using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Composition;
using TowerAutobattler.Equipment;
using TowerAutobattler.Expedition;
using TowerAutobattler.Project;
using TowerAutobattler.Relics;
using TowerAutobattler.Run;

public partial class ExpeditionPveCalibration
{
    private sealed record V1EffectCase(string Item, bool Equipment, string Context, string Group, int Stage,
        int Owner, string Profile, string Expectation);
    // Context labels are hypotheses, never classifications inferred from a winning result.
    private static readonly V1EffectCase[] V1EffectCases =
    [
        new("e02",true,"normal","b_remembered",1,11,"gloam_combat_profile","初蓝与原始攻击回蓝可提前首次换形；之后零蓝不再受益。"),
        new("e02",true,"weak","a_b_mixed",1,7,"gloam_combat_profile","无蓝怒劲英雄只能取得攻击属性；回蓝部分应无效。"),
        new("e03",true,"normal","b_remembered",2,21,"crown_boss_profile","长局雷云法强应落实为实际技能伤害；记录启动与施法，不能用过杀冒充收益。"),
        new("e02",true,"long-cast","b_remembered",2,21,"crown_boss_profile","长启动雷云初蓝与攻击回蓝的作用；与同源E03单件分别比较，不假称两件已取得。"),
        new("e03",true,"weak","a_melee",1,2,"gloam_elite_profile","攻击主核不应把所有攻击系数误当法强收益。"),
        new("e06",true,"normal","a_ranged",2,19,"crown_boss_profile","原始攻击和合格追加可能受益，技能箭不能被虚构为暴击。"),
        new("e06",true,"weak","a_melee",1,16,"gloam_elite_profile","低攻击占比支援者预期弱于攻击核心；治疗不应被本件放大。"),
        new("e07",true,"normal","a_melee",1,2,"gloam_elite_profile","前排多生命可能延长培养核输出窗口。"),
        new("e07",true,"weak","a_ranged",1,19,"gloam_elite_profile","后排未受威胁时多生命可能不改变结果；应按实际承伤判断。"),
        new("e10",true,"normal","a_melee",1,2,"gloam_elite_profile","实际扣血和受伤形成吸血窗口；范围效率与过疗限制回报。"),
        new("e10",true,"weak","a_melee",1,16,"gloam_elite_profile","低伤害支援者吸血回报预期较弱，不能按理论伤害记治疗。"),
        new("e11",true,"normal","a_ranged",1,19,"gloam_elite_profile","正式命中建立破甲与续航，观察长局收益。"),
        new("e11",true,"weak","a_melee",1,16,"gloam_elite_profile","低攻击频率支援者触发机会少；仍可能有辅助破甲价值。"),
        new("r06",false,"normal","assassin_support",1,5,"gloam_combat_profile","较高首击伤害可利用严格90%血线窗口。"),
        new("r06",false,"weak","a_ranged",1,19,"gloam_elite_profile","持续输出多数命中落在低血线，不能整段追溯增幅。"),
        new("r07",false,"normal","a_melee",1,2,"gloam_elite_profile","近战核心身体边缘距离内输出应受益。"),
        new("r07",false,"weak","a_ranged",1,19,"gloam_elite_profile","远程保持距离时弱，实际贴近后受益不能按职业禁用。"),
        new("r08",false,"normal","a_ranged",1,19,"gloam_boss_profile","仅正式熔炉Boss受增幅，护卫不受影响。"),
        new("r08",false,"weak","a_ranged",1,19,"gloam_elite_profile","精英没有正式Boss身份，应完全无直接效果。"),
        new("r09",false,"normal","a_ranged",1,19,"gloam_elite_profile","攻击与合格追加暴击增加触发，小额治疗受实际缺血限制。"),
        new("r09",false,"weak","b_remembered",0,11,"ember_combat_profile","早期低攻击次数和过疗可能弱；全队效果仍须看实际记录。"),
        new("r10",false,"normal","a_melee",1,2,"gloam_elite_profile","超过10秒且仍存活受伤时应有续战价值。"),
        new("r10",false,"weak","c_immediate",0,2,"ember_combat_profile","10秒前速胜或阵亡者无治疗，若实战并非速胜则情境假设待改。")
    ];

    // Four preregistered cells only. The previous normal E10 owner never lost health;
    // this existing U04 is a melee frontline member that did take health damage.
    private async Task CalibrateV1SustainProbe(CompiledGamePackage package,CompiledExpedition expedition,Options options)
    {
        Directory.CreateDirectory(options.Output);
        var source=options.V1Replay!;
        var directory=source.StartsWith("res://")||source.StartsWith("user://")?ProjectSettings.GlobalizePath(source):Path.GetFullPath(source);
        var path=Path.Combine(directory,"a_ranged-stage3.checkpoint.json");
        var rows=new List<JsonElement>();
        const string expectation="已上阵近战U04在末期Boss的有效吸血与近距增伤：源阵型/永久投入不改。R07作用全队实际近距来源，非仅U04；伤害与实治/过疗共同判断，不要求两件优于每个单件。";
        var plan=new {Group="a_ranged",Stage=3,Owner="prototype_u04",Profile="crown_boss_profile",Seed=options.Seeds[0],
            Equipment="e10",Relic="r07",ExpectedCells=4,Expectation=expectation,
            ExperimentalAcquisition=true,AcquisitionCostUnpaid=true,CommittedToRun=false,
            SelectionReason="Previous final-version evidence showed this existing deployed melee U04 took real health damage. No source search by treatment win or output is performed."};
        File.WriteAllText(Path.Combine(options.Output,"sustain-plan.json"),JsonSerializer.Serialize(plan,PrettyJson));
        if(!File.Exists(path))rows.Add(B3Json(new {Status="checkpoint-unavailable",Path=path}));
        else
        {
            var checkpoint=JsonSerializer.Deserialize<V1Checkpoint>(File.ReadAllText(path),PrettyJson)!;
            ValidateV1Checkpoint(package,checkpoint,V1Probes.Single(p=>p.Id=="a_ranged"),2);
            var owner=checkpoint.Run.Roster.SingleOrDefault(h=>h.ContentId=="prototype_u04");
            if(owner is null||!checkpoint.Run.Deployment.Contains(owner.InstanceId))
                rows.Add(B3Json(new {Status="owner-unavailable",Source=ProbeSource(checkpoint)}));
            else foreach(var gear in new[]{false,true})foreach(var relic in new[]{false,true})
            {
                rows.Add(RunV1Effect(package,expedition,checkpoint,"crown_boss_profile",options.Seeds[0],"r07-e10-sustain-u04",
                    $"equipment-{gear}-relic-{relic}","e10","r07",owner.InstanceId,gear,relic,expectation));
                await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            }
        }
        File.WriteAllLines(Path.Combine(options.Output,"sustain-battles.jsonl"),rows.Select(r=>r.GetRawText()));
        var completed=rows.Count(r=>r.GetProperty("Status").GetString()=="completed");
        File.WriteAllText(Path.Combine(options.Output,"sustain-summary.json"),JsonSerializer.Serialize(new {
            package.DefaultProfile.ContentFingerprint,Plan=plan,Completed=completed,Expected=4,
            CoverageStatus=completed==4?"all-requested-cells-completed":"incomplete",Rows=rows,
            Limits="One seed, four compiled-effect isolation cells from one validated real source. Target equipment is removed everywhere and owner slot0 cleared on both sides. No cost/route or natural combination claim. Zero effective healing remains a reported result, never replaced."},PrettyJson));
        GD.Print($"V1_SUSTAIN_PROBE_{(completed==4?"RECORDED":"INCOMPLETE")} completed={completed}/4 output={options.Output}");
    }

    private async Task CalibrateV1Probes(CompiledGamePackage package, CompiledExpedition expedition, Options options)
    {
        Directory.CreateDirectory(options.Output);
        var replay = options.V1Replay!;
        var directory = replay.StartsWith("res://") || replay.StartsWith("user://") ? ProjectSettings.GlobalizePath(replay) : Path.GetFullPath(replay);
        var checkpoints = new Dictionary<(string,int),V1Checkpoint>();
        foreach(var probe in V1Probes)
        for(var stage=0;stage<3;stage++)
        {
            var path=Path.Combine(directory,$"{probe.Id}-stage{stage+1}.checkpoint.json");
            if(!File.Exists(path))continue;
            var value=JsonSerializer.Deserialize<V1Checkpoint>(File.ReadAllText(path),PrettyJson)!;
            ValidateV1Checkpoint(package,value,probe,stage);checkpoints.Add((probe.Id,stage),value);
        }
        var cases=V1EffectCases.Where(c=>options.Probe is null||c.Group==options.Probe).ToArray();
        File.WriteAllText(Path.Combine(options.Output,"v1-probes-plan.json"),JsonSerializer.Serialize(new {
            SchemaVersion=1,ContentFingerprint=package.DefaultProfile.ContentFingerprint,Seed=options.Seeds[0],Cases=cases,
            Mode="compiled_effect_isolation_from_validated_checkpoint",
            BudgetPolicy="Run ledger is unchanged. Experimental item activation is not paid/acquired, and experimental labeling appears only in output metadata while business fingerprints are recomputed from exact instances. No equal-cost superiority claim.",
            EquipmentPolicy="Both variants remove the tested equipment everywhere and clear slot0 on the named owner; treatment adds one compiled item in that slot. All other content stays fixed.",
            RelicPolicy="Both variants remove the target relic; treatment adds one stack with initial counters. Only R06-R10 use this mode.",
            OtherCoverage=new[]{"two preregistered 2x2 interactions","stage1 and stage2 Boss with respective-stage checkpoints","one final snapshot statistics toggle"},
            Pending=new[]{"R01-R05/R11 matched formal-path acquisition branches","equal-budget service versus combat opportunities","old-core versus secondary-core growth allocation"}
        },PrettyJson));
        var rows=new List<JsonElement>();
        using var writer=new StreamWriter(Path.Combine(options.Output,"probe-battles.jsonl"),false);
        async Task EmitRow(JsonElement row)
        { rows.Add(row);writer.WriteLine(row.GetRawText());writer.Flush();await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame); }
        foreach(var item in cases)
        {
            var key=$"{item.Item}-{item.Context}";
            if(!checkpoints.TryGetValue((item.Group,item.Stage),out var checkpoint))
            {await EmitRow(B3Json(new {Pair=key,Status="checkpoint-unavailable",Case=item}));continue;}
            var owner=checkpoint.Run.Roster.SingleOrDefault(h=>h.ContentId==IdV1(item.Owner));
            if(owner is null||!checkpoint.Run.Deployment.Contains(owner.InstanceId))
            {await EmitRow(B3Json(new {Pair=key,Status="owner-unavailable",Case=item,Source=ProbeSource(checkpoint)}));continue;}
            foreach(var enabled in new[]{false,true})
                await EmitRow(RunV1Effect(package,expedition,checkpoint,item.Profile,options.Seeds[0],key,
                    enabled?"effect":"baseline",item.Equipment?item.Item:null,item.Equipment?null:item.Item,
                    owner.InstanceId,enabled&&item.Equipment,enabled&&!item.Equipment,item.Expectation));
        }
        foreach(var interaction in new[]{(Id:"r09-e06",Group:"a_ranged",Stage:2,Profile:"crown_boss_profile",Owner:19,Equipment:"e06",Relic:"r09"),
            (Id:"r07-e10",Group:"a_melee",Stage:1,Profile:"gloam_elite_profile",Owner:2,Equipment:"e10",Relic:"r07")})
        {
            if(options.Probe is not null&&interaction.Group!=options.Probe)continue;
            if(!checkpoints.TryGetValue((interaction.Group,interaction.Stage),out var checkpoint))
            {await EmitRow(B3Json(new {Pair=interaction.Id,Status="checkpoint-unavailable"}));continue;}
            var owner=checkpoint.Run.Roster.Single(h=>h.ContentId==IdV1(interaction.Owner));
            if(!checkpoint.Run.Deployment.Contains(owner.InstanceId))
            {await EmitRow(B3Json(new {Pair=interaction.Id,Status="owner-unavailable"}));continue;}
            foreach(var gear in new[]{false,true})foreach(var relic in new[]{false,true})
                await EmitRow(RunV1Effect(package,expedition,checkpoint,interaction.Profile,options.Seeds[0],interaction.Id,
                    $"equipment-{gear}-relic-{relic}",interaction.Equipment,interaction.Relic,owner.InstanceId,gear,relic,
                    "四格隔离：比较装备、遗物和共同效果；交互不能用来源表相加推断，不代表同价替代。"));
        }
        // These are current-stage pressure probes, not late teams beating previous bosses.
        foreach(var group in new[]{"a_melee","b_remembered","c_immediate"}.Where(g=>options.Probe is null||g==options.Probe))
        for(var stage=0;stage<2;stage++)
        {
            var key=$"boss-stage{stage+1}-{group}";
            if(!checkpoints.TryGetValue((group,stage),out var checkpoint))
            {await EmitRow(B3Json(new {Pair=key,Status="checkpoint-unavailable"}));continue;}
            await EmitRow(RunV1Effect(package,expedition,checkpoint,stage==0?"ember_boss_profile":"gloam_boss_profile",
                options.Seeds[0],key,"unmodified",null,null,"",false,false,
                "同阶段真实已达投入对Boss压力；此快照尚余2行动，不能当作已合法挑战Boss或等预算A/B/C排名。"));
        }
        var final=checkpoints.Values.Where(c=>c.Stage==2&&(options.Probe is null||c.Group==options.Probe))
            .OrderBy(c=>c.Group=="a_melee"?0:1).ThenBy(c=>c.Group,StringComparer.Ordinal).FirstOrDefault();
        JsonElement performance;
        if(final is null)performance=B3Json(new {Status="checkpoint-unavailable",Mode="statistics-toggle"});
        else
        {
            var measurements=new List<JsonElement>();string? signature=null;var consistent=true;
            // AB then BA bounds order bias; samples are diagnostics, not a benchmark distribution.
            foreach(var detailed in new[]{false,true,true,false})
            {
                var config=BuildV1ProbeConfig(package,expedition,final,"crown_boss_profile",options.Seeds[0],null,null,"",false,false,detailed);
                var allocated=GC.GetAllocatedBytesForCurrentThread();var clock=Stopwatch.StartNew();
                using var battle=new BattleSimulation(config);var result=battle.RunToEnd();clock.Stop();
                var current=JsonSerializer.Serialize(new {result.Outcome,result.Ticks,result.Digest,result.GoldSpent,
                    Units=result.Units.Select(u=>new {u.RuntimeId,u.Alive,u.FinalHealth,u.FinalShield,u.FinalPosition,u.DamageDealt,u.HealingDone,u.Kills}),result.PermanentGains,result.RememberedGains});
                signature??=current;consistent&=current==signature;
                measurements.Add(B3Json(new {DetailedStatistics=detailed,Milliseconds=clock.Elapsed.TotalMilliseconds,
                    ManagedAllocatedBytes=GC.GetAllocatedBytesForCurrentThread()-allocated,result.Outcome,result.Ticks,result.Digest}));
                await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            }
            performance=B3Json(new {Status=consistent?"consistent":"mismatch",Mode="statistics-toggle",Source=ProbeSource(final),Measurements=measurements,
                Limits="Four same-input serial samples; includes construction/simulation, excludes file I/O; managed-thread allocations are not resident memory or a general performance guarantee."});
        }
        File.WriteAllText(Path.Combine(options.Output,"statistics-toggle.json"),performance.GetRawText());
        // Provenance inventory only. Formal economic paired routes remain a distinct unfinished gate.
        var economic=new[]{"r01","r02","r03","r04","r05","r11"}.Select(id=>new {
            ContentId="v1_relic_"+id,Status="formal-paired-path-pending",
            Evidence=checkpoints.Values.Where(c=>c.Run.Items.Any(i=>i.ContentId=="v1_relic_"+id))
                .Select(c=>new {Source=ProbeSource(c),Instances=c.Run.Items.Where(i=>i.ContentId=="v1_relic_"+id),Ledger=c.Ledger}),
            Required="Actual offered acquire-versus-decline branches, successful expenses/rewards and remaining opportunities; no injected inventory/gold or theoretical savings."});
        File.WriteAllText(Path.Combine(options.Output,"economic-path-evidence.json"),JsonSerializer.Serialize(economic,PrettyJson));
        File.WriteAllText(Path.Combine(options.Output,"probe-summary.json"),JsonSerializer.Serialize(new {
            SchemaVersion=1,ContentFingerprint=package.DefaultProfile.ContentFingerprint,
            CoverageStatus=rows.All(r=>r.GetProperty("Status").GetString()=="completed")?"all-requested-cells-completed":rows.Any(r=>r.GetProperty("Status").GetString()=="completed")?"partial":"blocked",
            Rows=rows,Comparisons=CompareV1Effects(rows),Performance=performance,
            Pending="Economy matched routes, equal-budget alternatives and secondary-core allocation are not passed by this mode.",
            Interpretation="normal/weak are preregistered hypotheses; report failed assumptions and unavailable owners. One fixed seed is mechanism evidence, not completion rate."
        },PrettyJson));
        GD.Print($"V1_EFFECT_PROBES_RECORDED battles={rows.Count(r=>r.GetProperty("Status").GetString()=="completed")} unavailable={rows.Count(r=>r.GetProperty("Status").GetString()!="completed")} output={options.Output}");
    }

    private static object[] CompareV1Effects(List<JsonElement> rows)=>rows.Where(r=>r.GetProperty("Status").GetString()=="completed")
        .GroupBy(r=>r.GetProperty("Pair").GetString()).Where(g=>g.Any(r=>r.GetProperty("Variant").GetString()=="baseline")&&g.Any(r=>r.GetProperty("Variant").GetString()=="effect"))
        .Select(g=>
        {
            var a=g.Single(r=>r.GetProperty("Variant").GetString()=="baseline");var b=g.Single(r=>r.GetProperty("Variant").GetString()=="effect");
            double Delta(string metric)=>b.GetProperty("Metrics").GetProperty(metric).GetDouble()-a.GetProperty("Metrics").GetProperty(metric).GetDouble();
            var neutral=a.GetProperty("Ticks").GetInt32()==b.GetProperty("Ticks").GetInt32()&&a.GetProperty("Outcome").GetString()==b.GetProperty("Outcome").GetString()&&
                new[]{"Survivors","Health","Damage","Healing"}.All(m=>Math.Abs(Delta(m))<.001);
            return (object)new {Pair=g.Key,OutcomeBefore=a.GetProperty("Outcome"),OutcomeAfter=b.GetProperty("Outcome"),
                TickDelta=b.GetProperty("Ticks").GetInt32()-a.GetProperty("Ticks").GetInt32(),SurvivorDelta=Delta("Survivors"),HealthDelta=Delta("Health"),DamageDelta=Delta("Damage"),HealingDelta=Delta("Healing"),
                NeutralMetrics=neutral,HypothesisStatus=g.Key=="r08-weak"?(neutral?"expected-no-boss-effect":"unexpected-effect-review-required"):"context-and-role-review-required",
                Interpretation="Negative time delta is not sufficient: examine survival, effective healing, activation windows and opportunity cost; no equal-win-rate target."};
        }).ToArray();

    private static object ProbeSource(V1Checkpoint checkpoint)=>new {
        checkpoint.Group,Stage=checkpoint.Stage+1,checkpoint.ContentFingerprint,checkpoint.Run.RunId,checkpoint.Draft,checkpoint.RouteAttempt,
        CheckpointHash=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(checkpoint,PrettyJson)))),Budget=V1Budget(checkpoint.Run)};
    private static BattleConfig BuildV1ProbeConfig(CompiledGamePackage package,CompiledExpedition expedition,V1Checkpoint checkpoint,
        string profile,ulong seed,string? gear,string? relic,string owner,bool enableGear,bool enableRelic,bool detailed)
    {
        ValidateV1Checkpoint(package,checkpoint,V1Probes.Single(p=>p.Id==checkpoint.Group),checkpoint.Stage);
        var preparation=new RunBattlePreparationService(package.Content,package.Project,new RunRelicService(package.Content),package.GrowthRules);
        var source=CaptureConfiguration(preparation.Build(checkpoint.Run,CreatePlan(expedition.Encounters[profile]),true),seed);
        var equipment=source.Equipment;
        if(gear is not null)
        {
            var id="v1_equipment_"+gear;
            var instances=equipment.Instances.Where(i=>i.ContentId!=id&&!(i.OwnerHeroInstanceId==owner&&i.SlotIndex==0)).ToImmutableArray();
            if(enableGear)instances=instances.Add(new("experiment-equipment",id,owner,0,package.Content.Graph.ResolveEquipment(id)));
            equipment=new(EquipmentStateFingerprint.Compute(instances),instances);
        }
        var relics=source.Relics;
        if(relic is not null)
        {
            Require(relics is not null,"formal preparation has relic run context");
            var id="v1_relic_"+relic;var definition=package.Content.Graph.ResolveRelic(id);
            var instances=relics!.Instances.Where(i=>i.ContentId!=id).ToImmutableArray();
            if(enableRelic)instances=instances.Add(new("experiment-relic",id,1,0,0,RelicRunScope.InitialRunCounters(definition),definition));
            var projection=instances.Select(i=>new RelicRunInstanceSnapshot(i.InstanceId,i.ContentId,i.Stacks,i.Charges,i.Roll,
                i.Counters.Where(counter=>i.Definition.ReactiveCounters.Any(rule=>rule.CounterId==counter.CounterId&&rule.Scope==RelicCounterScope.Run)).ToImmutableArray()));
            var fingerprint=RelicRunScope.Fingerprint(projection);
            relics=relics with {SourceFingerprint=fingerprint,TransitionId=RelicRunScope.TransitionIdentity(relics.RunKey,fingerprint),
                Instances=instances,Modifiers=RelicRunScope.AggregateModifiers(instances)};
        }
        return new() {CaptureDetailedStatistics=detailed,Seed=source.Seed,Identity=source.Identity,MatrixStatusRules=source.MatrixStatusRules,
            FloorRule=source.FloorRule,Spawns=source.Spawns,HeroRule=source.HeroRule,Modifiers=source.Modifiers,Summons=source.Summons,
            EmptyDeploymentSlots=source.EmptyDeploymentSlots,StartingGold=source.StartingGold,Relics=relics,RelicSummons=source.RelicSummons,
            Equipment=equipment,Traits=source.Traits,TacticalCommands=source.TacticalCommands,TacticalSummons=source.TacticalSummons,
            BossTimeline=source.BossTimeline,AdditionalBossTimelines=source.AdditionalBossTimelines,ConfigureCombatBindings=source.ConfigureCombatBindings};
    }
    private static JsonElement RunV1Effect(CompiledGamePackage package,CompiledExpedition expedition,V1Checkpoint checkpoint,
        string profile,ulong seed,string pair,string variant,string? gear,string? relic,string owner,bool enableGear,bool enableRelic,string expectation)
    {
        try
        {
            var config=BuildV1ProbeConfig(package,expedition,checkpoint,profile,seed,gear,relic,owner,enableGear,enableRelic,true);
            using var battle=new BattleSimulation(config);var initial=BattleSimulation.CapturePreparation(battle);
            var result=battle.RunToEnd();VerifyStatistics(result);
            var observed=initial.Units.Where(u=>u.SourceInstanceId==owner&&u.Definition.ContentId=="prototype_u21").Select(u=>u.RuntimeId).ToHashSet();
            var startup=battle.PendingEvents.Where(e=>
                (observed.Contains(e.SourceRuntimeId)&&(e.Type=="ability"||e.Type.StartsWith("storm_channel_",StringComparison.Ordinal)))||
                (observed.Contains(e.TargetRuntimeId)&&e.Type=="defeated"))
                .Select(e=>new {e.Tick,e.Type,e.SourceRuntimeId,e.TargetRuntimeId,e.Value}).ToArray();
            return B3Json(new {Pair=pair,Variant=variant,Status="completed",Profile=profile,Seed=seed,Expectation=expectation,Source=ProbeSource(checkpoint),
                ExperimentalEffect=gear is not null||relic is not null,Equipment=gear,Relic=relic,Owner=owner,enableGear,enableRelic,
                PaidExperimentalCost=false,CommittedToRun=false,EquipmentBindings=config.Equipment.Instances.Select(i=>new {i.ContentId,i.OwnerHeroInstanceId,i.SlotIndex}),
                RelicBindings=config.Relics?.Instances.Select(i=>new {i.ContentId,i.Stacks}),
                InitialUnits=initial.Units.Select(u=>InitialUnit(package.Content,u)),result.Outcome,result.Ticks,result.Digest,
                Player=Summarize(result,initial,0),Enemy=Summarize(result,initial,1),Units=result.Units.Select(ReportUnit),
                StartupEvents=startup,StartupEventNote="Existing U21 ability/channel/death events only; start Value is planned bolts, each hit Value is remaining before that hit. Cancel or battle end can leave unreleased bolts. No per-frame collection.",
                Metrics=new {Survivors=result.Units.Count(u=>u.Team==0&&!u.IsTemporary&&u.Alive),
                    Health=result.Units.Where(u=>u.Team==0&&!u.IsTemporary).Sum(u=>u.FinalHealth),
                    Damage=result.Units.Where(u=>u.Team==0).Sum(u=>u.DamageDealt),Healing=result.Units.Where(u=>u.Team==0).Sum(u=>u.HealingDone)},
                HypothesisReview="pending: compare pair actual damage, core survival, effective healing/shields, control and startup; never infer isolated item damage from source attribution alone"});
        }
        catch(Exception error) when(error is InvalidOperationException or ArgumentException)
        {return B3Json(new {Pair=pair,Variant=variant,Status="blocked",Profile=profile,Seed=seed,error.Message,Source=ProbeSource(checkpoint)});}
    }
}
