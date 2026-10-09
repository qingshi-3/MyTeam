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
using TowerAutobattler.Growth;
using TowerAutobattler.Project;
using TowerAutobattler.Run;
using TowerAutobattler.Relics;

public partial class ExpeditionPveCalibration
{
    private sealed record V1RouteCommand(string SiteId,string? ChoiceId);
    private sealed record V1BranchResult(string Name,string Status,ActiveRunDto? Run,List<JsonElement> Ledger,List<V1RouteCommand> Commands);
    private static readonly string[] V1EconomyIds=["v1_relic_r01","v1_relic_r02","v1_relic_r03","v1_relic_r04","v1_relic_r05","v1_relic_r11"];

    // Entry consumes one explicitly selected, verified main-matrix checkpoint file.
    // No seed search and no injected hero, money, material, or item is part of this runner.
    private async Task CalibrateV1Economy(CompiledGamePackage package,CompiledExpedition expedition,string checkpointPath,string output)
    {
        Directory.CreateDirectory(output);
        var checkpoint=JsonSerializer.Deserialize<V1Checkpoint>(File.ReadAllText(checkpointPath),PrettyJson)!;
        var probe=V1Probes.Single(p=>p.Id==checkpoint.Group);
        ValidateV1Checkpoint(package,checkpoint,probe,checkpoint.Stage);
        var root=new B3Save {Json=JsonSerializer.Serialize(checkpoint.Run),Meta=checkpoint.Meta};
        var rows=new List<JsonElement>();
        rows.AddRange(V1PreparationEconomyEffects(package,expedition,checkpoint));
        // One source determines a shared, finite itinerary; other branches replay those exact sites/choices.
        var core=checkpoint.Run.Roster.Single(h=>h.ContentId==IdV1(probe.Core));
        var producers=checkpoint.Run.Roster.Where(h=>package.GrowthRules!.Heroes.TryGetValue(h.ContentId,out var rule)&&rule.ProductionModes.Contains(GrowthProductionMode.Cultivation))
            .Where(h=>checkpoint.Run.Deployment.Contains(h.InstanceId)).ToArray();
        var secondary=checkpoint.Run.Roster.Where(h=>h.InstanceId!=core.InstanceId&&checkpoint.Run.Deployment.Contains(h.InstanceId)&&
            h.ContentId is not ("prototype_u16" or "prototype_u25" or "prototype_u13"))
            .OrderBy(h=>h.ContentId==IdV1(18)?0:h.ContentId==IdV1(19)?1:RankV1(probe,h.ContentId)+2).FirstOrDefault();
        if(checkpoint.Stage!=1||checkpoint.Run.Expedition!.RemainingActions<2||producers.Length==0||secondary is null)
            rows.Add(B3Json(new {Case="two-action-allocation",Status="source-ineligible",Reason="requires actual middle-stage two-action budget, deployed producer and an already-owned deployed secondary"}));
        else
        {
            var old=await ContinueV1Branch(package,expedition,probe,root.Fork(),"old-core",null,app=>AssignV1Future(app,producers.Select(h=>h.InstanceId),core.InstanceId),output);
            rows.Add(BranchEvidence(old));
            var second=await ContinueV1Branch(package,expedition,probe,root.Fork(),"owned-secondary",old.Commands,app=>AssignV1Future(app,producers.Select(h=>h.InstanceId),secondary.InstanceId),output);
            rows.Add(BranchEvidence(second));
            var tree=checkpoint.Run.Roster.SingleOrDefault(h=>h.ContentId=="prototype_u25"&&checkpoint.Run.Deployment.Contains(h.InstanceId));
            if(tree is not null)
            {
                var absent=await ContinueV1Branch(package,expedition,probe,root.Fork(),"tree-benched",old.Commands,app=>
                {AssignV1Future(app,producers.Select(h=>h.InstanceId),core.InstanceId);BenchV1Owned(app,tree.InstanceId);},output);
                rows.Add(BranchEvidence(absent));
                var engines=producers.Select(h=>h.InstanceId).Append(tree.InstanceId).Distinct().ToDictionary(id=>id,id=>checkpoint.Run.Deployment.IndexOf(id));
                var serviceOnly=await ContinueV1Branch(package,expedition,probe,root.Fork(),"service-only-engines",old.Commands,
                    app=>AssignV1Future(app,producers.Select(h=>h.InstanceId),core.InstanceId),output,
                    prepareOpportunity:(app,isBattle)=>SetV1EnginePresence(app,engines,!isBattle),
                    beforePressure:app=>SetV1EnginePresence(app,engines,false));
                rows.Add(BranchEvidence(serviceOnly));
            }
            else rows.Add(B3Json(new {Case="tree-present-versus-benched",Status="owner-unavailable"}));
            if(old.Run is { } oldRun&&second.Run is { } secondRun)
                rows.Add(B3Json(new {Case="allocation-budget-check",Status="recorded",
                    SameMaterialProduction=oldRun.Growth!.History.Sum(h=>h.MaterialsGranted)==secondRun.Growth!.History.Sum(h=>h.MaterialsGranted),
                    SameGold=oldRun.Gold==secondRun.Gold,SameRemainingActions=oldRun.Expedition!.RemainingActions==secondRun.Expedition!.RemainingActions,
                    Note="Same source and actual itinerary; any failed route or different spending/rewards invalidates an equal-budget conclusion."}));
        }
        var dragon=checkpoint.Run.Roster.SingleOrDefault(h=>h.ContentId==IdV1(13));
        var hunter=checkpoint.Run.Roster.SingleOrDefault(h=>h.ContentId==IdV1(11));
        if(checkpoint.Stage==1&&dragon is not null&&hunter is not null&&checkpoint.Run.Deployment.Contains(dragon.InstanceId))
        {
            var early=await ContinueV1Branch(package,expedition,probe,root.Fork(),"dragon-first-opportunity",null,null,output,preferCombat:true);rows.Add(BranchEvidence(early));
            var late=await ContinueV1Branch(package,expedition,probe,root.Fork(),"dragon-second-opportunity",early.Commands,app=>BenchV1Owned(app,dragon.InstanceId),output,
                beforeSecond:app=>Require(app.MoveDeploymentUnit(dragon.InstanceId,checkpoint.Run.Deployment.IndexOf(dragon.InstanceId)),"formally redeploy existing dragon"),preferCombat:true);
            rows.Add(BranchEvidence(late));
            var absent=await ContinueV1Branch(package,expedition,probe,root.Fork(),"dragon-no-future-opportunity",early.Commands,app=>BenchV1Owned(app,dragon.InstanceId),output,preferCombat:true);rows.Add(BranchEvidence(absent));
        }
        else rows.Add(B3Json(new {Case="future-early-late-missing-dragon",Status="source-ineligible",Note="Does not recreate early-game acquisition or remove previously earned remembrance."}));
        // Seek at most one actual choice fork, across at most four forward commands.
        var search=root.Fork();var appSearch=OpenV1Branch(package,search);var searchLedger=new List<JsonElement>();var forked=false;
        var ignored=new HashSet<string>();
        try
        {
            for(var attempt=0;attempt<4&&!forked&&appSearch.ActiveRun?.Expedition?.RemainingActions>0;attempt++)
            {
                if(appSearch.PendingOffer is null)await ReachV1Opportunity(appSearch,expedition,null,ignored,searchLedger);
                if(appSearch.PendingOffer is { } offer)
                {
                    var eligible=offer.Choices.Where(c=>appSearch.CheckOfferChoice(c).Succeeded).ToArray();
                    var preferred=eligible.OrderBy(c=>V1EconomyIds.Contains(c.ContentId)?0:c.ContentId is "prototype_u25" or "prototype_u13"?1:2)
                        .ThenBy(c=>c.StableId,StringComparer.Ordinal).FirstOrDefault();
                    var alternative=eligible.FirstOrDefault(c=>c.StableId!=preferred?.StableId);
                    if(preferred is not null&&(alternative is not null||offer.AllowSkip))
                    {
                        var previewSite=appSearch.ActiveRun!.Expedition!.Sites.SingleOrDefault(site=>site.ServiceOffer?.OfferId==offer.OfferId)?.SiteId;
                        var frozen=search.Fork();
                        File.WriteAllText(Path.Combine(output,"actual-offer-source.json"),JsonSerializer.Serialize(new {checkpoint.ContentFingerprint,Run=frozen.LoadActiveRun(),Meta=frozen.LoadMeta(),Source=ProbeSource(checkpoint),Search=searchLedger,Offer=offer},PrettyJson));
                        V1BranchResult? first=null;
                        foreach(var take in new[]{true,false})
                        {
                            var save=frozen.Fork();var branch=OpenV1Branch(package,save);var ledger=new List<JsonElement>(searchLedger);
                            // Service inspection is presentation-only and is intentionally absent from saves.
                            // Reopen the same reached site; never silently resolve an absent offer.
                            if(branch.PendingOffer is null&&previewSite is not null)
                                Require(branch.InspectExpeditionSite(previewSite),"restore the actual reached service preview");
                            Require(branch.PendingOffer?.OfferId==offer.OfferId,"restored fork must expose the exact frozen offer");
                            var chosen=take?preferred.StableId:alternative?.StableId;
                            ResolveV1ActualOffer(branch,chosen,ledger);
                            Require(ledger.Any(e=>e.TryGetProperty("Kind",out var k)&&k.GetString()=="actual-choice"&&
                                e.GetProperty("Selected").GetString()==chosen),"fork must commit its requested different choice");
                            var run=await ContinueV1Branch(package,expedition,probe,save,"actual-offer-"+(take?"preferred":"alternative"),first?.Commands,null,output,initialLedger:ledger,settlementTarget:Math.Min(2,branch.ActiveRun!.Expedition!.RemainingActions));
                            first??=run;rows.Add(BranchEvidence(run));
                        }
                        forked=true;
                    }
                    else ResolveV1ActualOffer(appSearch,preferred?.StableId,searchLedger);
                }
                await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            }
        }
        catch(V1Unavailable error){searchLedger.Add(B3Json(new {Kind="search-stopped",error.Message}));}
        rows.Add(B3Json(new {Case="one-actual-offer-fork",Status=forked?"recorded":"bounded-search-unavailable",Search=searchLedger}));
        foreach(var id in V1EconomyIds)
            rows.Add(B3Json(new {Case=id,Status="normal-and-late-path-coverage-review-required",OwnedAtSource=checkpoint.Run.Items.Where(i=>i.ContentId==id),
                Required=id switch {"v1_relic_r01"=>"actual shop price and successful payment; no later affordable shop is weak",
                    "v1_relic_r02"=>"actual first entries, +2 receipts, then paid shop terminates; no remaining entries is weak",
                    "v1_relic_r03"=>"full BattlePreparation at actual gold; low remaining balance is weak",
                    "v1_relic_r04"=>"full BattlePreparation on elite and non-elite; remaining elite opportunity matters",
                    "v1_relic_r05"=>"actual eligible elite victory and unique extra relic claim; exhausted/no elite remaining is weak",
                    _=>"post-acquisition native-kill receipts, each six actual awarded equipment and remainder; no future six-kill threshold is weak"}}));
        File.WriteAllText(Path.Combine(output,"economy-route-summary.json"),JsonSerializer.Serialize(new {SchemaVersion=1,Source=ProbeSource(checkpoint),Rows=rows,
            Limits="Conditional source, no rate claim. At most two future production settlements per branch and six route commands; skipped/unaffordable choices do not count as successful opportunities. No unearned permanent gains are injected. Complete per-item normal/late coverage is not inferred from a single fork."},PrettyJson));
        GD.Print($"V1_ECONOMY_ROUTE_RECORDED rows={rows.Count} actualFork={forked} output={output}");
    }
    // Four preparation-only R03 cells from two already-reached, immutable source budgets.
    // The stage difference is explicit and is never used as a causal gold comparison.
    private async Task CalibrateV1TreasurySources(CompiledGamePackage package,CompiledExpedition expedition,string sourceDirectory,string output)
    {
        Directory.CreateDirectory(output);
        var directory=sourceDirectory.StartsWith("res://")||sourceDirectory.StartsWith("user://")
            ?ProjectSettings.GlobalizePath(sourceDirectory):Path.GetFullPath(sourceDirectory);
        var rows=new List<JsonElement>();var sources=new List<object>();
        foreach(var sample in new[]{(Stage:0,ExpectedGold:15,Label:"lower-existing-balance"),(Stage:2,ExpectedGold:94,Label:"higher-existing-balance")})
        {
            var path=Path.Combine(directory,$"a_ranged-stage{sample.Stage+1}.checkpoint.json");
            if(!File.Exists(path)){rows.Add(B3Json(new {Status="checkpoint-unavailable",Path=path,Context=sample.Label}));continue;}
            try
            {
                var checkpoint=JsonSerializer.Deserialize<V1Checkpoint>(File.ReadAllText(path),PrettyJson)!;
                ValidateV1Checkpoint(package,checkpoint,V1Probes.Single(p=>p.Id=="a_ranged"),sample.Stage);
                sources.Add(new {Context=sample.Label,Path=path,ExpectedGold=sample.ExpectedGold,ActualGold=checkpoint.Run.Gold,
                    Expectation="R03 adds 1.5 maximum health per actual coin at formal preparation; smaller balance grants less per-unit health. Survival or faster victory is not guaranteed.",Source=ProbeSource(checkpoint)});
                foreach(var row in V1PreparationEconomyEffects(package,expedition,checkpoint,"v1_relic_r03"))
                {
                    rows.Add(row);
                    await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                }
            }
            catch(Exception error) when(error is InvalidOperationException or ArgumentException)
            {rows.Add(B3Json(new {Status="blocked",Path=path,Context=sample.Label,error.Message}));}
        }
        var completed=rows.Count(r=>r.GetProperty("Status").GetString()=="experimental-completed");
        File.WriteAllText(Path.Combine(output,"treasury-source-probes.json"),JsonSerializer.Serialize(new {
            SchemaVersion=1,package.DefaultProfile.ContentFingerprint,Item="v1_relic_r03",Seed=20261006,
            Expected=4,Completed=completed,CoverageStatus=completed==4?"all-requested-cells-completed":"incomplete",Sources=sources,Rows=rows,
            AcquisitionCostUnpaid=true,NaturalAcquisitionEvidence=false,CommittedToRun=false,
            ComparisonPolicy="Compare absent/present only within each identical source and same-stage elite profile. Do not compare early and late outcomes causally: roster, growth, enemy and balance differ. Actual gold is never edited.",
            SeparateEvidence="The existing 32-gold same-source preparation pair and actual offer/spending branches ending at 30/42 gold remain separate evidence. These four cells do not pay acquisition cost, consume a shopping opportunity, or prove saving dominates spending.",
            Limits="One seed and two preregistered existing sources; no search by outcome, no R04 or forward economy branches. Missing sources or blocked cells remain explicit gaps."},PrettyJson));
        GD.Print($"V1_TREASURY_SOURCE_PROBES_{(completed==4?"RECORDED":"INCOMPLETE")} completed={completed}/4 output={output}");
    }

    private static IEnumerable<JsonElement> V1PreparationEconomyEffects(CompiledGamePackage package,CompiledExpedition expedition,V1Checkpoint source,string? onlyItem=null)
    {
        var region=new[]{"ember","gloam","crown"}[source.Stage];
        foreach(var item in new[]{"v1_relic_r03","v1_relic_r04"}.Where(id=>onlyItem is null||id==onlyItem))
        foreach(var elite in item.EndsWith("r04",StringComparison.Ordinal)?new[]{false,true}:new[]{true})
        foreach(var present in new[]{false,true})
        {
            // Explicit preparation-only counterfactual, never persisted or called reachable.
            // Gold, heroes, growth and all other inventory remain exactly the real source.
            var run=JsonSerializer.Deserialize<ActiveRunDto>(JsonSerializer.Serialize(source.Run))!;
            run.Items.RemoveAll(i=>i.ContentId==item);
            if(present)
            {
                var definition=package.Content.Graph.ResolveRelic(item);
                run.Items.Add(new ItemInstanceDto {InstanceId="experiment-preparation-"+item,ContentId=item,Stacks=1,
                    Counters=RelicRunScope.InitialRunCounters(definition).Select(c=>new RelicCounterStateDto {CounterId=c.CounterId,Value=c.Value}).ToList()});
            }
            var profile=region+(elite?"_elite_profile":"_combat_profile");
            var config=new RunBattlePreparationService(package.Content,package.Project,new RunRelicService(package.Content),package.GrowthRules)
                .Build(run,CreatePlan(expedition.Encounters[profile]),true);
            using var battle=new BattleSimulation(CaptureConfiguration(config,20261006));var initial=BattleSimulation.CapturePreparation(battle);var result=battle.RunToEnd();VerifyStatistics(result);
            yield return B3Json(new {Case=item+"-preparation-"+(elite?"elite":"normal"),Status="experimental-completed",Present=present,
                Source=ProbeSource(source),Profile=profile,Gold=run.Gold,Seed=20261006,Initial=initial.Units.Select(u=>InitialUnit(package.Content,u)),
                result.Outcome,result.Ticks,result.Digest,Units=result.Units.Select(ReportUnit),
                PaidOrNaturallyAcquired=false,CommittedToRun=false,ChangedOnly="one relic in a discarded BattlePreparation input clone; no gold or permanent gains injected",
                RemainingCoverage=item.EndsWith("r03",StringComparison.Ordinal)?"Lower/higher actual-balance pairs live in treasury-source-probes.json; formal spending alternatives remain separate and no hypothetical gold is credited.":"Elite initial health ratio versus ordinary battle effect is isolated; future reachable elite opportunities still need formal path evidence."});
        }
    }

    private static RunApplication OpenV1Branch(CompiledGamePackage package,B3Save save)
    {
        var run=save.LoadActiveRun()??throw new V1Unavailable("branch has no active run");
        var persistence=new RunProgressionPersistenceService(package.Content,save,package.Project,package.GrowthRules,package.DefaultProfile);
        Require(persistence.ValidateRun(run),"formal branch ledger validates before cold restoration");
        var app=new RunApplication(package,save,"v1-economy-route");Require(app.ActiveRun is not null,"formal branch restores");return app;
    }
    private static void AssignV1Future(RunApplication app,IEnumerable<string> producers,string target)
    {foreach(var id in producers){var result=app.SetGrowthAssignment(id,target,GrowthProductionMode.Cultivation);Require(result.Succeeded,$"formal future cultivation assignment: producer={id} target={target} reason={result.Message}");}}
    private static void SetV1EnginePresence(RunApplication app,IReadOnlyDictionary<string,int> engines,bool present)
    {
        foreach(var (id,slot) in engines)
        {
            if(present&&app.ActiveRun!.Deployment.IndexOf(id)!=slot) Require(app.MoveDeploymentUnit(id,slot),"restore an already-owned engine to its original slot");
            else if(!present&&app.ActiveRun!.Deployment.Contains(id)) Require(app.WithdrawDeploymentUnit(id),"legally bench engine before battle; no extra reserve capacity");
        }
    }
    private static void BenchV1Owned(RunApplication app,string id)
    {
        var run=app.ActiveRun!;var slot=run.Deployment.IndexOf(id);if(slot<0)return;
        var replacement=run.Roster.FirstOrDefault(h=>!run.Deployment.Contains(h.InstanceId));
        Require(replacement is not null?app.MoveDeploymentUnit(replacement.InstanceId,slot):app.WithdrawDeploymentUnit(id),"legal bench/replacement using only existing roster capacity");
    }
    private async Task<V1BranchResult> ContinueV1Branch(CompiledGamePackage package,CompiledExpedition expedition,V1Probe probe,B3Save save,
        string name,IReadOnlyList<V1RouteCommand>? replay,Action<RunApplication>? setup,string output,Action<RunApplication>? beforeSecond=null,List<JsonElement>? initialLedger=null,bool preferCombat=false,int settlementTarget=2,
        Action<RunApplication,bool>? prepareOpportunity=null,Action<RunApplication>? beforePressure=null)
    {
        var ledger=initialLedger??[];var commands=new List<V1RouteCommand>();var app=OpenV1Branch(package,save);var start=app.ActiveRun!.Growth!.History.Count;
        var ignored=new HashSet<string>();var status="incomplete";var activatedSecond=false;
        try
        {
            setup?.Invoke(app);ledger.Add(B3Json(new {Kind="branch-start",name,Budget=V1Budget(app.ActiveRun!)}));
            for(var step=0;step<6&&app.ActiveRun is { } run&&run.Growth!.History.Count-start<settlementTarget;step++)
            {
                if(run.Growth!.History.Count-start==1&&!activatedSecond){beforeSecond?.Invoke(app);activatedSecond=true;}
                if(run.Expedition!.RemainingActions<=0)break;
                if(replay is not null&&step>=replay.Count)break;
                var planned=replay is null?null:replay[step];
                var site=await ReachV1Opportunity(app,expedition,planned?.SiteId,ignored,ledger,preferCombat,prepareOpportunity);
                string? choiceId=null;
                if(app.PendingOffer is { } offer)
                {
                    choiceId=planned?.ChoiceId??ChooseV1Offer(app,offer,probe)?.StableId;
                    if(planned is not null)choiceId=planned.ChoiceId;
                    ResolveV1ActualOffer(app,choiceId,ledger);
                }
                commands.Add(new(site,choiceId));
                if(app.ActiveRun is null)throw new V1Unavailable("actual battle ended run");
                await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            }
            beforePressure?.Invoke(app);
            if(beforePressure is not null)ledger.Add(B3Json(new {Kind="formation-before-shared-pressure",Budget=V1Budget(app.ActiveRun!)}));
            var final=save.LoadActiveRun()??throw new V1Unavailable("actual branch terminal");
            OpenV1Branch(package,save);status=final.Growth!.History.Count-start==settlementTarget?(settlementTarget==2?"two-settlements-completed":settlementTarget==1?"one-future-settlement-completed":"no-future-settlement-available"):"bounded-opportunities-unavailable";
            File.WriteAllText(Path.Combine(output,name+".run.json"),JsonSerializer.Serialize(new {Run=final,Meta=save.LoadMeta(),Ledger=ledger,Commands=commands},PrettyJson));
            // Snapshot no longer obeys main matrix's fixed remaining-action boundary; validate Run, not a forged V1Checkpoint.
            var config=new RunBattlePreparationService(package.Content,package.Project,new RunRelicService(package.Content),package.GrowthRules)
                .Build(final,CreatePlan(expedition.Encounters["gloam_elite_profile"]),true);
            using var battle=new BattleSimulation(CaptureConfiguration(config,20261006));var result=battle.RunToEnd();VerifyStatistics(result);
            ledger.Add(B3Json(new {Kind="uncommitted-shared-pressure",Profile="gloam_elite_profile",Seed=20261006,result.Outcome,result.Ticks,result.Digest,
                Units=result.Units.Select(ReportUnit),Budget=V1Budget(final),Note="No route battle result substituted; this common pressure battle is never committed."}));
            return new(name,status,final,ledger,commands);
        }
        catch(Exception error) when(error is V1Unavailable or InvalidOperationException){ledger.Add(B3Json(new {Kind="branch-stopped",error.Message}));return new(name,"unavailable",save.LoadActiveRun(),ledger,commands);}
    }
    private static JsonElement BranchEvidence(V1BranchResult branch)=>B3Json(new {branch.Name,branch.Status,Budget=branch.Run is null?null:V1Budget(branch.Run),branch.Commands,branch.Ledger});
    private async Task<string> ReachV1Opportunity(RunApplication app,CompiledExpedition expedition,string? planned,HashSet<string> ignored,List<JsonElement> ledger,bool preferCombat=false,Action<RunApplication,bool>? prepareOpportunity=null)
    {
        var map=app.ActiveRun!.Expedition!;
        var site=planned is not null?map.Sites.Single(s=>s.SiteId==planned):map.Sites.Where(s=>!s.Cleared&&!s.Handled&&!ignored.Contains(s.SiteId))
            .Select(s=>new {Site=s,Path=V1Route(map,s.CellIndex),Kind=expedition.Sites.Single(d=>d.StableId==s.DefinitionId).Kind})
            .Where(s=>s.Path is not null).OrderBy(s=>s.Path!.Count(c=>ExpeditionNavigation.IsBlocked(map,c))*20+s.Path!.Count+
                (preferCombat?(s.Kind==TowerNodeType.Elite?-100:s.Kind==TowerNodeType.Combat?0:1000):(s.Kind is TowerNodeType.Event or TowerNodeType.Shop?0:s.Kind==TowerNodeType.Elite?10:15))).ThenBy(s=>s.Site.SiteId,StringComparer.Ordinal).FirstOrDefault()?.Site
                ??throw new V1Unavailable("no remaining opportunity");
        if(preferCombat&&site.EncounterProfileId.Length==0)throw new V1Unavailable("no bounded battle opportunity; service cannot stand in for remembered-growth comparison");
        var path=V1Route(map,site.CellIndex)??throw new V1Unavailable("unreachable actual site");
        var guard=path.Select(c=>ExpeditionNavigation.SiteAt(map,c)).FirstOrDefault(s=>s is {Cleared:false,EncounterProfileId.Length:>0});
        if(guard is not null&&guard.SiteId!=site.SiteId)
        {if(planned is not null)throw new V1Unavailable("paired itinerary unexpectedly needs a different guard");site=guard;path=V1Route(map,site.CellIndex)!;}
        foreach(var cell in path.Take(Math.Max(0,path.Count-1)))Require(app.MoveExpedition(cell),"formal adjacent movement");
        if(!app.CanInteractExpeditionSite(site.SiteId))throw new V1Unavailable("actual site interaction rejected");
        prepareOpportunity?.Invoke(app,site.EncounterProfileId.Length>0);
        if(prepareOpportunity is not null)ledger.Add(B3Json(new {Kind="formation-for-opportunity",site.SiteId,IsBattle=site.EncounterProfileId.Length>0,Budget=V1Budget(app.ActiveRun!)}));
        var before=V1Budget(app.ActiveRun!);
        if(site.EncounterProfileId.Length>0)
        {
            var encounter=app.PreviewExpeditionEncounter(site.SiteId);var begun=app.TryBeginGrowthBattle(encounter);
            if(!begun.Succeeded)throw new V1Unavailable(begun.Message);
            BattleResult result;using(var battle=new BattleSimulation(app.BuildBattleConfig(encounter)))result=battle.RunToEnd();
            Require(app.ResolveBattle(result,encounter).Accepted,"actual path result accepted");
            ledger.Add(B3Json(new {Kind="actual-battle",site.SiteId,site.EncounterProfileId,result.Outcome,result.Ticks,result.Digest,result.RememberedGains,result.PermanentGains,
                Before=before,After=app.ActiveRun is null?null:V1Budget(app.ActiveRun)}));
            if(result.Outcome!=BattleOutcome.PlayerVictory||app.ActiveRun is null)throw new V1Unavailable("actual path lost; never override or retry outcome");
        }
        else Require(app.InspectExpeditionSite(site.SiteId),"formal reached service");
        ignored.Add(site.SiteId);await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);return site.SiteId;
    }
    private static void ResolveV1ActualOffer(RunApplication app,string? choiceId,List<JsonElement> ledger)
    {
        if(app.PendingOffer is null)throw new V1Unavailable("no actual pending offer to resolve; restore reached service preview first");
        for(var group=0;group<8&&app.PendingOffer is { } offer;group++)
        {
            if(group>0)choiceId=offer.Choices.Where(c=>app.CheckOfferChoice(c).Succeeded).OrderBy(c=>c.StableId,StringComparer.Ordinal).FirstOrDefault()?.StableId;
            var before=V1Budget(app.ActiveRun!);var frozen=app.ActiveRun!.PendingOffer;
            var result=app.ResolveOffer(offer.OfferId,choiceId);
            if(!result.Succeeded)throw new V1Unavailable("formal choice rejected: "+result.Message);
            ledger.Add(B3Json(new {Kind="actual-choice",offer.OfferId,offer.RecipeId,OfferKind=offer.Kind,offer.Choices,OriginalFrozenChoices=frozen?.Choices,
                Selected=choiceId,result.Changes,result.ChanceSucceeded,Before=before,After=V1Budget(app.ActiveRun!)}));
            if(offer.Kind!=RunOfferKind.CombatReward){if(app.ActiveRun!.Action is not null)app.FinishNonCombatNode();break;}
        }
        if(app.PendingOffer is not null)throw new V1Unavailable("bounded reward chain not finished");
    }
}
