using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Composition;
using TowerAutobattler.Expedition;
using TowerAutobattler.Equipment;
using TowerAutobattler.Project;
using TowerAutobattler.Run;

public partial class ExpeditionPveCalibration
{
    private static readonly string[] V1RunItemsIds = ["v1_relic_r01","v1_relic_r02","v1_relic_r05","v1_relic_r11"];
    private sealed record V1RunItemsStep(string Name,string SiteId,bool Boss=false);
    private sealed record V1RunItemsBranch(string Item,string Timing,bool Present,string Status,List<JsonElement> Ledger,
        Dictionary<string,string[]> Choices,ActiveRunDto? Run,MetaProgressDto Meta);

    // This is an explicit experimental acquisition, never a natural offer fork:
    // exactly one unpaid relic is granted through the formal API in a detached save.
    // No money, materials, growth, roster, encounter, random seed or result is edited.
    private async Task CalibrateV1RunItems(CompiledGamePackage package,CompiledExpedition expedition,string sourceDirectory,string output,string? onlyItem=null,string? onlyTiming=null)
    {
        Directory.CreateDirectory(output);
        var sources=Directory.GetFiles(sourceDirectory,"*.checkpoint.json").Order(StringComparer.Ordinal)
            .Select(path=>JsonSerializer.Deserialize<V1Checkpoint>(File.ReadAllText(path),PrettyJson)!).ToArray();
        Require(sources.Length==18,"run-item evaluation consumes the eighteen existing published checkpoints");
        foreach(var source in sources)ValidateV1Checkpoint(package,source,V1Probes.Single(p=>p.Id==source.Group),source.Stage);
        var rows=new List<JsonElement>();
        foreach(var item in V1RunItemsIds.Where(item=>onlyItem is null||item==onlyItem))
        foreach(var timing in new[]{"normal","late-zero-actions"}.Where(timing=>onlyTiming is null||timing==onlyTiming))
        {
            var stage=timing=="late-zero-actions"?2:item is "v1_relic_r05" or "v1_relic_r11"?1:0;
            var preferred=timing=="normal"&&item=="v1_relic_r02"?"a_b_mixed":"a_melee";
            V1Checkpoint? source=null;List<V1RunItemsStep>? plan=null;var rejected=new List<object>();
            foreach(var candidate in sources.Where(c=>c.Stage==stage&&c.Run.Items.All(i=>i.ContentId!=item))
                .OrderBy(c=>c.Group==preferred?0:1).ThenBy(c=>c.Group,StringComparer.Ordinal))
            {
                try{plan=V1RunItemsPlan(expedition,candidate,item,timing);source=candidate;break;}
                catch(V1Unavailable error){rejected.Add(new{candidate.Group,Stage=candidate.Stage+1,error.Message});}
            }
            if(source is null||plan is null)
            {rows.Add(B3Json(new{Item=item,Timing=timing,Status="source-opportunity-unavailable",RejectedSources=rejected,
                Reason="No existing current-fingerprint source is both target-free and has the required actual window; no source or inventory is fabricated."}));continue;}
            var absent=await V1RunItemsForward(package,expedition,source,item,timing,false,plan,null,output);
            var present=await V1RunItemsForward(package,expedition,source,item,timing,true,plan,absent.Choices,output);
            rows.Add(B3Json(new{Item=item,Timing=timing,Source=ProbeSource(source),Plan=plan,
                Absent=V1CompactRunItemsBranch(absent),Present=V1CompactRunItemsBranch(present),Comparison=V1RunItemsCompare(item,timing,absent,present),
                ExperimentalAcquisition=true,AcquisitionCostUnpaid=true,NaturalAcquisitionEvidence=false,
                EqualOpportunityCost=false,Note="Shared real source and itinerary isolate the unpaid relic effect. Real additional items and their downstream effects are retained; outcomes are never replaced."}));
        }
        File.WriteAllText(Path.Combine(output,"run-item-economy.json"),JsonSerializer.Serialize(new{SchemaVersion=1,
            ContentFingerprint=package.DefaultProfile.ContentFingerprint,SourceCount=sources.Length,Rows=rows,
            EvidenceColumns=new{Natural="Only the existing source acquisition ledger is natural/formal route evidence; these four relic acquisitions are experimental.",
                Experimental="RunApplication.GrantItem on validated detached save, unpaid acquisition. Then formal movement, node submission, exact real battle, purchasing, rewards and equipment use.",
                Boundary="Late sources have zero paid actions. The final boss is actually submitted; no future stage or shop is invented. Unavailable or losing branches remain gaps."},
            Limits=new[]{"Not acquisition rates, an equal-budget ranking, or overall balance.","Normal R02 requires a real boss win to reach its first purchase/post-purchase window; failure is retained.",
                "R05 extra relic is delivered by real settlement, not a fabricated extra offer.","R11 credits only its persisted post-acquisition native-kill receipts; boss kills are excluded.",
                "Acquired equipment is formally worn before the next actual battle; showing wear is not proof of a favorable marginal damage delta."}},PrettyJson));
        GD.Print($"V1_RUN_ITEMS_ECONOMY_RECORDED pairs={rows.Count} output={output}");
    }

    private static List<V1RunItemsStep> V1RunItemsPlan(CompiledExpedition expedition,V1Checkpoint source,string item,string timing)
    {
        if(timing=="late-zero-actions")return [new("final-boss-no-future-spend-window","",true)];
        var map=source.Run.Expedition!;var used=new HashSet<string>();
        string Find(TowerNodeType kind,int minimumNative=0,string? desired=null)
        {
            var site=map.Sites.Where(s=>!s.Cleared&&!s.Handled&&!used.Contains(s.SiteId)&&
                expedition.Sites.Single(d=>d.StableId==s.DefinitionId).Kind==kind&&
                (desired is null||s.SiteId==desired)&&
                (minimumNative==0||expedition.Encounters[s.EncounterProfileId].Composition.EnemyIds.Length>=minimumNative))
                .Select(s=>(Site:s,Path:V1Route(map,s.CellIndex)))
                .Where(x=>x.Path is not null&&!x.Path.Take(Math.Max(0,x.Path.Count-1)).Any(c=>ExpeditionNavigation.IsBlocked(map,c)))
                .OrderBy(x=>x.Path!.Count).ThenBy(x=>x.Site.SiteId,StringComparer.Ordinal).FirstOrDefault();
            if(site.Site is null)throw new V1Unavailable($"no guard-free actual {kind} opportunity for {item}");
            used.Add(site.Site.SiteId);return site.Site.SiteId;
        }
        return item switch
        {
            "v1_relic_r01"=>[new("paid-shop",Find(TowerNodeType.Shop)),new("use-purchased-equipment",Find(TowerNodeType.Combat))],
            "v1_relic_r02"=>[new("node-before-purchase-1",Find(TowerNodeType.Rest)),new("node-before-purchase-2",Find(TowerNodeType.Rest)),
                new("real-boss-to-next-actions","",true),new("first-paid-shop",Find(TowerNodeType.Shop)),new("node-after-purchase",Find(TowerNodeType.Event))],
            "v1_relic_r05"=>[new("elite-extra-relic",Find(TowerNodeType.Elite)),new("use-earned-relic",Find(TowerNodeType.Combat))],
            "v1_relic_r11"=>[new("native-kills-1",Find(TowerNodeType.Combat,3)),new("native-kills-2",Find(TowerNodeType.Combat,3)),new("use-bounty-equipment-in-actual-boss","",true)],
            _=>throw new V1Unavailable("unknown run-item experiment")
        };
    }

    private async Task<V1RunItemsBranch> V1RunItemsForward(CompiledGamePackage package,CompiledExpedition expedition,V1Checkpoint source,
        string item,string timing,bool present,List<V1RunItemsStep> plan,Dictionary<string,string[]>? replay,string output)
    {
        var save=new B3Save{Json=JsonSerializer.Serialize(source.Run),Meta=source.Meta};var app=OpenV1Branch(package,save);
        var ledger=new List<JsonElement>();var selections=new Dictionary<string,string[]>(StringComparer.Ordinal);
        var sourceEquipment=V1RunItemsEquipment(source.Run).Select(e=>e.InstanceId).ToHashSet(StringComparer.Ordinal);
        var probe=V1Probes.Single(p=>p.Id==source.Group);var status="completed";
        try
        {
            if(present)
            {
                var before=V1Budget(app.ActiveRun!);Require(app.GrantItem(item),"formal unpaid experimental relic grant");
                Require(app.ActiveRun!.Items.Count(i=>i.ContentId==item)==1,"exactly one experimental relic");
                ledger.Add(B3Json(new{Kind="experimental-grant",item,AcquisitionCostUnpaid=true,Before=before,After=V1Budget(app.ActiveRun)}));
            }
            ledger.Add(B3Json(new{Kind="start",Budget=V1Budget(app.ActiveRun!),Target=V1RunItemsTarget(app.ActiveRun!,item),
                CurrentlyInteractablePaidSites=app.ActiveRun!.Expedition!.Sites.Count(s=>app.CanInteractExpeditionSite(s.SiteId)),
                RemainingPaidActions=app.ActiveRun!.Expedition!.RemainingActions}));
            foreach(var step in plan)
            {
                if(app.ActiveRun is null)throw new V1Unavailable("real branch already terminal");
                V1RunItemsEquip(app,probe,sourceEquipment,ledger);
                if(step.Boss)
                {
                    await V1RunItemsBattle(app,app.PreviewExpeditionBoss(),step.Name,item,ledger,selections,replay);
                    continue;
                }
                var map=app.ActiveRun!.Expedition!;
                if(map.RemainingActions<=0)throw new V1Unavailable("no remaining paid-action window; no boss bypass");
                var site=map.Sites.Single(s=>s.SiteId==step.SiteId);var path=V1Route(map,site.CellIndex)??throw new V1Unavailable("actual paired site no longer reachable");
                if(path.Take(Math.Max(0,path.Count-1)).Any(c=>ExpeditionNavigation.IsBlocked(map,c)))throw new V1Unavailable("paired itinerary would require an unplanned intervening guard");
                foreach(var cell in path.Take(Math.Max(0,path.Count-1)))Require(app.MoveExpedition(cell),"formal adjacent movement along paired actual route");
                Require(app.CanInteractExpeditionSite(site.SiteId),"formal paired site interaction");
                if(site.EncounterProfileId.Length>0)
                    await V1RunItemsBattle(app,app.PreviewExpeditionEncounter(site.SiteId),step.Name,item,ledger,selections,replay);
                else
                {
                    Require(app.InspectExpeditionSite(site.SiteId),"inspect actual frozen service");
                    var before=app.ActiveRun!;var beforeGold=before.Gold;var beforeActions=before.Expedition!.RemainingActions;
                    var offer=app.PendingOffer??throw new V1Unavailable("no current service offer");
                    var choice=V1RunItemsChoice(app,offer,replay?.GetValueOrDefault(step.Name)?.FirstOrDefault());
                    var price=choice.Costs.Where(c=>c.Kind==RunOperationKind.SpendGold).Sum(c=>c.Amount);
                    var beforeEquipment=V1RunItemsEquipment(before).Select(e=>e.InstanceId).ToHashSet(StringComparer.Ordinal);
                    var result=app.ResolveOffer(offer.OfferId,choice.StableId);
                    if(!result.Succeeded)throw new V1Unavailable("actual service/purchase rejected: "+result.Message);
                    selections[step.Name]=[choice.StableId];
                    var after=app.ActiveRun??throw new V1Unavailable("service ended actual run");
                    ledger.Add(B3Json(new{Kind="service",Step=step.Name,site.SiteId,OfferKind=offer.Kind,choice.StableId,choice.ContentId,
                        PricePaid=price,BeforeGold=beforeGold,AfterGold=after.Gold,GoldDelta=after.Gold-beforeGold,
                        BeforeActions=beforeActions,AfterActions=after.Expedition!.RemainingActions,result.Changes,result.ChanceSucceeded,
                        NewEquipment=V1RunItemsEquipment(after).Where(e=>!beforeEquipment.Contains(e.InstanceId)),Target=V1RunItemsTarget(after,item)}));
                    if(present && item=="v1_relic_r02")
                    {
                        var target=after.Items.Single(i=>i.ContentId==item);
                        Require(target.Charges==(step.Name is "first-paid-shop" or "node-after-purchase"?1:0),"actual first paid purchase freezes R02 charges; later entry cannot resume income");
                    }
                    if(after.Action is not null)app.FinishNonCombatNode();
                }
                // Validate the exact published ledger again; no hand-edited counter state is accepted.
                if(app.ActiveRun is not null)OpenV1Branch(package,save);
                await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            }
            if(timing=="normal"&&item=="v1_relic_r11"&&present)
            {
                var state=app.ActiveRun?.Items.Single(i=>i.ContentId==item).RunRuleStates.Single();
                if(state is null||state.ClearedSites.Sum(c=>c.NativeKills)<6)throw new V1Unavailable("actual post-acquisition kills did not earn the six-kill equipment threshold");
            }
        }
        catch(V1Unavailable error){status="opportunity-unavailable";ledger.Add(B3Json(new{Kind="stopped",error.Message}));}
        catch(InvalidOperationException error){status="contract-failed";ledger.Add(B3Json(new{Kind="contract-failed",error.Message}));}
        var final=save.LoadActiveRun();var branch=new V1RunItemsBranch(item,timing,present,status,ledger,selections,final,save.LoadMeta());
        File.WriteAllText(Path.Combine(output,item+"-"+timing+"-"+(present?"present":"absent")+".json"),JsonSerializer.Serialize(branch,PrettyJson));
        return branch;
    }

    private static CompiledRunChoice V1RunItemsChoice(RunApplication app,PendingRunOffer offer,string? planned)
    {
        var eligible=offer.Choices.Where(c=>app.CheckOfferChoice(c).Succeeded&&!V1RunItemsIds.Contains(c.ContentId)).ToArray();
        if(planned is not null)return eligible.SingleOrDefault(c=>c.StableId==planned)??throw new V1Unavailable("the exact paired actual choice is no longer eligible");
        return eligible.OrderBy(c=>c.SuccessChance<1).ThenByDescending(c=>c.Operations.Any(o=>o.Kind==RunOperationKind.GainGold))
            .ThenBy(c=>c.Costs.Where(o=>o.Kind==RunOperationKind.SpendGold).Sum(o=>o.Amount)).ThenBy(c=>c.StableId,StringComparer.Ordinal).FirstOrDefault()
            ??throw new V1Unavailable("no actual eligible non-contaminating choice; experimental targets cannot be naturally added to control");
    }

    private async Task V1RunItemsBattle(RunApplication app,EncounterPlan encounter,string label,string item,List<JsonElement> ledger,
        Dictionary<string,string[]> selections,Dictionary<string,string[]>? replay)
    {
        var before=app.ActiveRun??throw new V1Unavailable("actual battle has no run");var beforeBudget=V1Budget(before);var beforeGold=before.Gold;
        var beforeEquipment=V1RunItemsEquipment(before).Select(e=>e.InstanceId).ToHashSet(StringComparer.Ordinal);
        var beforeItems=before.Items.Select(i=>i.InstanceId).ToHashSet(StringComparer.Ordinal);
        var begun=app.TryBeginGrowthBattle(encounter);if(!begun.Succeeded)throw new V1Unavailable("actual battle preparation refused: "+begun.Message);
        var enteredGold=app.ActiveRun!.Gold;
        var config=app.BuildBattleConfig(encounter);
        // Only diagnostics are enabled; the formal seed, signed identity and all combat inputs remain unchanged.
        config=CaptureConfiguration(config,config.Seed);
        BattleResult result; BattlePreparationSnapshot initial;
        using(var battle=new BattleSimulation(config)){initial=BattleSimulation.CapturePreparation(battle);result=battle.RunToEnd();}
        VerifyStatistics(result);var accepted=app.ResolveBattle(result,encounter);Require(accepted.Accepted,"actual unmodified battle result accepted");
        var settled=app.ActiveRun;EquipmentInstanceState[] addedEquipment=settled is null?[]:V1RunItemsEquipment(settled).Where(e=>!beforeEquipment.Contains(e.InstanceId)).ToArray();
        var addedRelics=settled?.Items.Where(i=>!beforeItems.Contains(i.InstanceId)).ToArray()??[];
        var nativeDefeated=result.Units.Where(u=>u.Team==1&&!u.IsTemporary&&u.JoinTick==0&&u.SourceInstanceId.StartsWith("enemy-",StringComparison.Ordinal)&&!u.Alive&&u.DefeatTick is not null).ToArray();
        ledger.Add(B3Json(new{Kind="battle",Step=label,encounter.ExpeditionSiteId,encounter.ExpeditionProfileId,encounter.IsBoss,encounter.IsElite,
            config.Seed,BeforeGold=beforeGold,EnteredGold=enteredGold,EntryGoldDelta=enteredGold-beforeGold,
            ExpectedBossEntryGoldDelta=item=="v1_relic_r02"&&(encounter.IsBoss||encounter.ExpeditionBoss)?0:(int?)null,result.Outcome,result.Ticks,result.Digest,
            NativeDefeated=nativeDefeated.Select(u=>new{u.SourceInstanceId,u.ContentId,u.JoinTick,u.DefeatTick}),
            InitialEquipment=config.Equipment.Instances.Select(e=>new{e.ContentId,e.OwnerHeroInstanceId,e.SlotIndex}),
            InitialRelics=config.Relics?.Instances.Select(i=>i.ContentId),InitialUnits=initial.Units.Select(u=>new{u.SourceInstanceId,ContentId=u.Definition.ContentId,u.Health,u.MaxHealth,u.Damage}),
            Units=result.Units.Select(ReportUnit),SettlementEquipmentBeforeRewardChoices=addedEquipment,SettlementRelicsBeforeRewardChoices=addedRelics,
            BudgetBefore=beforeBudget,BudgetAfter=settled is null?null:V1Budget(settled),Target=settled is null?null:V1RunItemsTarget(settled,item),
            ActualBattleCommitted=true,ResultOverridden=false,SeedOverridden=false,DiagnosticsOnly=true}));
        if(item=="v1_relic_r02"&&(encounter.IsBoss||encounter.ExpeditionBoss))
            Require(enteredGold==beforeGold,$"R02 actual Boss entry must grant zero gold ({label}): before={beforeGold}, entered={enteredGold}");
        if(result.Outcome!=BattleOutcome.PlayerVictory)throw new V1Unavailable("actual battle lost/timed out; no retry or substituted result");
        if(settled is null)
        {
            selections[label]=[];
            if(!encounter.IsBoss)throw new V1Unavailable("non-boss victory unexpectedly terminal");return;
        }
        var chosen=new List<string>();
        for(var group=0;group<8&&app.PendingOffer is { } offer;group++)
        {
            var expected=replay?.GetValueOrDefault(label)?.ElementAtOrDefault(group);
            var choice=V1RunItemsChoice(app,offer,expected);var rewardBefore=V1Budget(app.ActiveRun!);
            var resolution=app.ResolveOffer(offer.OfferId,choice.StableId);if(!resolution.Succeeded)throw new V1Unavailable("actual earned reward refused: "+resolution.Message);
            chosen.Add(choice.StableId);ledger.Add(B3Json(new{Kind="earned-reward",Step=label,offer.OfferId,offer.RewardCategory,choice.StableId,choice.ContentId,
                resolution.Changes,Before=rewardBefore,After=V1Budget(app.ActiveRun!)}));
        }
        selections[label]=chosen.ToArray();Require(app.PendingOffer is null&&app.ActiveRun!.Action is null,"real reward transaction completes once");
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
    }

    private static IEnumerable<EquipmentInstanceState> V1RunItemsEquipment(ActiveRunDto run)=>run.EquipmentInventory.Concat(run.Roster.SelectMany(h=>h.Equipment));
    private static object? V1RunItemsTarget(ActiveRunDto run,string item)=>run.Items.SingleOrDefault(i=>i.ContentId==item);
    private static void V1RunItemsEquip(RunApplication app,V1Probe probe,HashSet<string> sourceEquipment,List<JsonElement> ledger)
    {
        var run=app.ActiveRun!;var core=run.Roster.Single(h=>h.ContentId==IdV1(probe.Core));
        foreach(var gear in run.EquipmentInventory.Where(e=>!sourceEquipment.Contains(e.InstanceId)).ToArray())
        {
            var hero=app.ActiveRun!.Roster.Single(h=>h.InstanceId==core.InstanceId);
            var slot=Enumerable.Range(0,app.Rules.EquipmentSlotCapacity).FirstOrDefault(s=>hero.Equipment.All(e=>e.SlotIndex!=s),0);
            var displaced=hero.Equipment.SingleOrDefault(e=>e.SlotIndex==slot);
            Require(app.EquipOwnedItem(gear.InstanceId,hero.InstanceId,slot),"formally wear actual purchased/earned equipment");
            ledger.Add(B3Json(new{Kind="wear-actual-equipment",gear.InstanceId,gear.ContentId,Owner=hero.InstanceId,Slot=slot,Displaced=displaced?.ContentId,
                AcquisitionWasFormalForwardPayoff=true,Equipment=app.ActiveRun!.Roster.Single(h=>h.InstanceId==hero.InstanceId).Equipment}));
            // Returned equipment cannot loop back in later calls and displace the new item again.
            if(displaced is not null)sourceEquipment.Add(displaced.InstanceId);
            sourceEquipment.Add(gear.InstanceId);
        }
    }

    private static object V1CompactRunItemsBranch(V1RunItemsBranch branch)
    {
        var fields=new HashSet<string>(["Kind","Step","item","Message","SiteId","ExpeditionSiteId","ExpeditionProfileId","IsBoss","IsElite","Seed",
            "StableId","ContentId","PricePaid","BeforeGold","EnteredGold","EntryGoldDelta","ExpectedBossEntryGoldDelta","AfterGold","GoldDelta","BeforeActions","AfterActions","ChanceSucceeded",
            "NativeDefeated","InitialEquipment","InitialRelics","SettlementEquipmentBeforeRewardChoices","SettlementRelicsBeforeRewardChoices","Target",
            "Owner","Slot","Displaced","InstanceId","CurrentlyInteractablePaidSites","RemainingPaidActions"],StringComparer.Ordinal);
        return new{branch.Status,branch.Present,branch.Choices,FinalGold=branch.Run?.Gold,FinalActions=branch.Run?.Expedition?.RemainingActions,
            FinalTarget=branch.Run is null?null:V1RunItemsTarget(branch.Run,branch.Item),
            Entries=branch.Ledger.Select(row=>row.EnumerateObject().Where(p=>fields.Contains(p.Name)).ToDictionary(p=>p.Name,p=>p.Value)),
            FullEvidence=branch.Item+"-"+branch.Timing+"-"+(branch.Present?"present":"absent")+".json"};
    }

    private static object V1RunItemsCompare(string item,string timing,V1RunItemsBranch absent,V1RunItemsBranch present)
    {
        var matched=absent.Choices.Count==present.Choices.Count&&absent.Choices.All(p=>present.Choices.TryGetValue(p.Key,out var values)&&values.SequenceEqual(p.Value));
        var baseline=absent.Ledger.Where(r=>r.GetProperty("Kind").GetString()=="service").ToArray();
        var services=present.Ledger.Where(r=>r.GetProperty("Kind").GetString()=="service").Select(row=>
        {
            var step=row.GetProperty("Step").GetString();var control=baseline.SingleOrDefault(r=>r.GetProperty("Step").GetString()==step);
            return new{Step=step,Paired=control.ValueKind!=JsonValueKind.Undefined && control.GetProperty("StableId").GetString()==row.GetProperty("StableId").GetString(),
                BasePrice=control.ValueKind==JsonValueKind.Undefined?(int?)null:control.GetProperty("PricePaid").GetInt32(),
                PresentPrice=row.GetProperty("PricePaid").GetInt32(),
                NetGoldDeltaDifference=control.ValueKind==JsonValueKind.Undefined?(int?)null:row.GetProperty("GoldDelta").GetInt32()-control.GetProperty("GoldDelta").GetInt32(),
                ExpectedNetGoldDelta=item=="v1_relic_r02" && (step is "node-before-purchase-1" or "node-before-purchase-2" or "first-paid-shop")?2:item=="v1_relic_r02"&&step=="node-after-purchase"?0:(int?)null,
                Interpretation=step is "node-before-purchase-1" or "node-before-purchase-2"?"Actual +2 only if same selected payoff; boss/shop entries are separate receipts.":
                    step=="node-after-purchase"?"Actual post-payment entry: expected zero new +2; accumulated gold is not new income.":"Actual frozen price and payment, including all independently earned downstream rules."};
        }).ToArray();
        return new{MatchedActualChoices=matched,AbsentStatus=absent.Status,PresentStatus=present.Status,ServicePayments=services,
            FinalTarget=present.Run is null?null:V1RunItemsTarget(present.Run,item),
            RemainingPaidActions=present.Run?.Expedition?.RemainingActions,
            Interpretation=timing=="late-zero-actions"?"No normal-site spending window at acquisition. Final boss outcome is actual; R02 grants zero Boss entry gold and no future stage or purchase is invented.":
                item=="v1_relic_r05"?"Compare SettlementRelicsBeforeRewardChoices, which is the direct real unique-relic draw; later regular reward choices and new relic interactions remain separate.":
                item=="v1_relic_r11"?"Compare persisted ClearedSites native kills, remainder and SettlementEquipmentBeforeRewardChoices; only actually earned/worn equipment is counted.":
                "Price/entry receipts describe the unpaid experimental effect, not equal opportunity cost or natural acquisition value."};
    }
}
