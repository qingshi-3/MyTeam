using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Godot;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.Composition;
using TowerAutobattler.Content;
using TowerAutobattler.Project;
using TowerAutobattler.Run;
using TowerAutobattler.Statuses;
using TowerAutobattler.Traits;

public partial class BuildMatchupDiagnostics : Node
{
    private sealed record Gear(int Owner, string Id);
    private sealed record TeamCase(string Id, string Category, string[] Heroes, Gear[] Gear, string Relic,
        int MinimumFloor, string Note);
    private sealed record UnitTelemetry(string RuntimeId, string SourceInstanceId, string ContentId, string Name,
        int Team, bool Temporary, bool Alive, float FinalHealth, float MaxHealth, float FinalShield,
        float DamageDealt, float DamageTaken, float HealthDamageTaken, float ShieldAbsorbed, float HealingDone,
        float HealingRequested, float HealingApplied, float HealingOverflow, float ShieldGranted,
        int Kills, int JoinTick, int? DefeatTick, int ActiveTicks, int AttackActions, int EffectiveHealingEvents,
        int ManaCasts, int? FirstManaCastTick, int GritPunches, int HardControlApplications,
        float HardControlGrantedTicks, int SlowApplications, int HardControlReceivedTicks, int SlowReceivedTicks,
        string SummonerRuntimeId, string RootSummonerRuntimeId, float OwnedSummonDamage, float OwnedSummonHealing,
        object[] AbilityCasts, object[] DamageSources);
    private sealed record BattleRecord(string CaseId, string Category, string Note, int Floor, string Kind, ulong Seed,
        string PositionVariant, string BaselineKey,
        string EncounterId, string EncounterTitle, string CompositionId, string FloorRuleId,
        float EnemyHealthMultiplier, float EnemyDamageMultiplier, string Outcome, int Ticks, string Digest,
        string[] Heroes, object[] HeroEconomy, object[] Equipment, object? Relic, object[] Traits,
        object[] Placements, object[] InitialStats, UnitTelemetry[] Units);

    private static readonly ulong[] Seeds = [1776, 2026, 9173, 4109, 8128];
    private static string H(string id) => "hero_" + id;
    private static string E(string id) => "equipment_" + id;

    public override async void _Ready()
    {
        try
        {
            var gate = await GamePackagePublisher.CreateReadyAsync(this,
                GD.Load<GameProjectDefinition>("res://content/project/alpha_project.tres"));
            var package = gate.Package ?? throw new InvalidOperationException(string.Join(';', gate.Report.CoreErrors));
            var cases = Cases().ToArray();
            var records = new List<BattleRecord>();
            var quick = System.Environment.GetEnvironmentVariable("MATCHUP_DIAGNOSTICS_QUICK") == "1";
            var mode = System.Environment.GetEnvironmentVariable("MATCHUP_DIAGNOSTICS_MODE") ?? (quick ? "quick" : "main");
            var selectedCases = System.Environment.GetEnvironmentVariable("MATCHUP_DIAGNOSTICS_CASES")?.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (selectedCases is not null) cases = cases.Where(x => selectedCases.Contains(x.Id)).ToArray();
            var seeds = quick ? Seeds.Take(1).ToArray() : Seeds;
            var work = mode == "followup" ? Followup(cases).ToArray()
                : mode == "corrected" ? Corrected(cases).ToArray()
                : (quick ? cases.Select(x => (Team:x,Floor:8,Kind:TowerNodeType.Elite,OldPlacement:false))
                    : cases.SelectMany(x => Stages().Where(s => s.Floor >= x.MinimumFloor).Select(s => (Team:x,s.Floor,s.Kind,OldPlacement:false)))).ToArray();
            foreach (var item in work)
            foreach (var seed in seeds)
            {
                records.Add(Run(package, item.Team, item.Floor, item.Kind, seed, item.OldPlacement));
                if (records.Count % 20 == 0) { GD.Print($"MATCHUP_PROGRESS {records.Count}"); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
            }
            Validate(records, quick);
            var first=work.First();
            var repeat=Run(package,first.Team,first.Floor,first.Kind,seeds[0],first.OldPlacement);
            if(repeat.Digest!=records.First().Digest)throw new InvalidOperationException("Determinism replay digest mismatch.");
            var directory = ProjectSettings.GlobalizePath("res://design-discussion/04-content-validation/artifacts/player-balance/matchup-diagnostics");
            Directory.CreateDirectory(directory);
            var name = mode == "followup" ? "raw-followup.json" : mode == "corrected" ? "raw-corrected.json" : quick ? "raw-quick.json" : "raw.json";
            File.WriteAllText(Path.Combine(directory, name), JsonSerializer.Serialize(new {
                GeneratedUtc = DateTime.UtcNow.ToString("O"), Quick = quick, Seeds = seeds,
                SourceFingerprint = Fingerprint(), DeterminismReplays=1, DeterminismPassed=true, RecordCount = records.Count, Records = records
            }, new JsonSerializerOptions { WriteIndented = true }));
            GD.Print($"BUILD_MATCHUP_DIAGNOSTICS_OK records={records.Count} output={name}");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PrintErr("BUILD_MATCHUP_DIAGNOSTICS_FAILED " + error); GetTree().Quit(1); }
    }

    private static (int Floor, TowerNodeType Kind)[] Stages() => [
        (0, TowerNodeType.Combat), (3, TowerNodeType.Combat), (3, TowerNodeType.Elite), (4, TowerNodeType.Boss),
        (8, TowerNodeType.Combat), (8, TowerNodeType.Elite), (9, TowerNodeType.Boss),
        (13, TowerNodeType.Combat), (13, TowerNodeType.Elite), (14, TowerNodeType.Boss)];

    private static IEnumerable<(TeamCase Team,int Floor,TowerNodeType Kind,bool OldPlacement)> Followup(TeamCase[] cases)
    {
        foreach(var id in new[]{"general-duty","poison-complete","summon-complete","winter-formed"})
            yield return (cases.Single(x=>x.Id==id),10,TowerNodeType.Combat,false);
        foreach(var id in new[]{"poison-complete","winter-formed"})
        foreach(var stage in new[]{(13,TowerNodeType.Elite),(14,TowerNodeType.Boss)})
            yield return (cases.Single(x=>x.Id==id) with { Id=id+"-old-placement", Note=cases.Single(x=>x.Id==id).Note+"；旧校准站位敏感性追测" },stage.Item1,stage.Item2,true);
    }
    private static IEnumerable<(TeamCase Team,int Floor,TowerNodeType Kind,bool OldPlacement)> Corrected(TeamCase[] cases)
    {
        foreach(var id in new[]{"lab-shield-chain","lab-crit-attack"})
        foreach(var stage in new[]{(13,TowerNodeType.Combat),(13,TowerNodeType.Elite),(14,TowerNodeType.Boss)})
            yield return (cases.Single(x=>x.Id==id),stage.Item1,stage.Item2,false);
    }

    private static IEnumerable<TeamCase> Cases()
    {
        yield return new("general-duty", "formal-pool", ["hc03_iron_guard","hc01_crossbow","hc18_healing_reader","hc09_breach_scribe","hc39_hook_machine"],
            [new(0,"vanguard_insignia"),new(2,"ne01_mana_charm"),new(1,"ne10_swift_gloves"),new(3,"field_focus")], "item_ne01_opening_bulwark", 0, "前排+远程+治疗通用队；按阶段截取人数与物品");
        yield return new("poison-complete", "formal-pool", ["hc03_iron_guard","hc08_poison_keeper","hc18_healing_reader","hc15_shield_grower","hc09_breach_scribe"],
            [new(1,"ne01_mana_charm"),new(2,"ne01_mana_charm"),new(0,"vanguard_insignia"),new(1,"field_focus")], "item_ne01_opening_bulwark", 5, "完整棘毒");
        yield return new("poison-no-healer", "formal-pool", ["hc03_iron_guard","hc08_poison_keeper","hc20_blood_drummer","hc15_shield_grower","hc09_breach_scribe"],
            [new(1,"ne01_mana_charm"),new(2,"ne01_mana_charm"),new(0,"vanguard_insignia"),new(1,"field_focus")], "item_ne01_opening_bulwark", 5, "同预算替换持续治疗；远程治疗换近战鼓手并按职责重排，属于复合替补对照");
        yield return new("summon-complete", "formal-pool", ["hc03_iron_guard","hc25_death_provider","hc24_swarm_keeper","hc18_healing_reader","hc07_death_echo"],
            [new(1,"ne01_mana_charm"),new(2,"ne01_mana_charm"),new(3,"ne01_mana_charm"),new(4,"ne01_mana_charm")], "item_ne02_bone_vigor", 5, "完整召唤");
        yield return new("summon-no-death-core", "formal-pool", ["hc03_iron_guard","hc38_grit_brawler","hc24_swarm_keeper","hc18_healing_reader","hc07_death_echo"],
            [new(1,"ne01_mana_charm"),new(2,"ne01_mana_charm"),new(3,"ne01_mana_charm"),new(4,"ne01_mana_charm")], "item_ne02_bone_vigor", 5, "远程死亡供给换近战怒拳并按职责重排；怒拳持法力装备无收益，属于复合替补压力样本，可能仍有有效配合");
        yield return new("winter-formed", "formal-pool", ["hc03_iron_guard","hc23_frost_historian","hc01_crossbow","hc18_healing_reader","hc30_armor_broadcaster"],
            [new(2,"ne10_winter_badge"),new(3,"ne10_winter_badge"),new(1,"ne10_swift_gloves"),new(0,"vanguard_insignia")], "item_ne10_control_shelter", 5, "四成员霜羽与前排防护");
        yield return new("winter-same-heroes-missing", "formal-pool", ["hc03_iron_guard","hc23_frost_historian","hc01_crossbow","hc18_healing_reader","hc30_armor_broadcaster"],
            [new(2,"field_focus"),new(3,"ne01_mana_charm"),new(1,"ne10_swift_gloves"),new(0,"vanguard_insignia")], "item_ne10_control_shelter", 5, "英雄不变，仅用同价装备替换两枚徽章");
        yield return new("lab-shield-chain", "laboratory-extension", ["hc13_shield_crit","hc15_shield_grower","hc16_shield_bomber","hc18_healing_reader","hc30_armor_broadcaster"],
            [new(0,"field_focus"),new(1,"ne01_mana_charm"),new(2,"ne10_swift_gloves"),new(4,"vanguard_insignia")], "item_ne01_opening_bulwark", 8, "含正式17人池外英雄；非自然获取声明");
        yield return new("lab-crit-attack", "laboratory-extension", ["hc03_iron_guard","hc04_crit_duelist","hc13_shield_crit","hc01_crossbow","hc20_blood_drummer"],
            [new(1,"ne10_swift_gloves"),new(2,"field_focus"),new(3,"ne10_swift_gloves"),new(0,"vanguard_insignia")], "item_ne01_opening_bulwark", 8, "暴击/普攻实验扩展；非自然获取声明");
        yield return new("lab-grit-sustain", "laboratory-extension", ["hc03_iron_guard","hc38_grit_brawler","hc18_healing_reader","hc19_overheal_priest","hc30_armor_broadcaster"],
            [new(1,"vanguard_insignia"),new(2,"ne01_mana_charm"),new(3,"ne01_mana_charm"),new(4,"field_focus")], "item_ne01_opening_bulwark", 8, "怒拳承伤+治疗实验扩展；非自然获取声明");
    }

    private static BattleRecord Run(CompiledGamePackage package, TeamCase team, int floor, TowerNodeType kind, ulong seed, bool oldPlacement=false)
    {
        var count = floor == 0 ? Math.Min(2, team.Heroes.Length) : floor < 5 ? Math.Min(3, team.Heroes.Length) : floor < 10 ? Math.Min(4, team.Heroes.Length) : team.Heroes.Length;
        var gearCount = floor == 0 ? 0 : floor < 5 ? 1 : floor < 10 ? 2 : 4;
        var heroes = team.Heroes.Take(count).ToArray();
        var gear = team.Gear.Where(x => x.Owner < count).Take(gearCount).ToArray();
        var relic = floor >= 5 ? team.Relic : "";
        var run = new ActiveRunDto { Seed=seed, FloorIndex=floor, BattleNumber=floor, CurrentPopulation=count,
            CurrentRunHealth=100, MaximumRunHealth=100, Gold=0,
            EquippedTacticalCommandIds=package.Project.RunRules.StarterTacticalCommandIds.ToList() };
        var melee=0; var ranged=0;
        var meleeCells=new[]{new Vector2I(2,0),new Vector2I(2,2),new Vector2I(2,4),new Vector2I(1,0),new Vector2I(1,2),new Vector2I(1,4)};
        var rangedCells=new[]{new Vector2I(0,1),new Vector2I(0,3),new Vector2I(1,1),new Vector2I(1,3),new Vector2I(0,0),new Vector2I(0,4)};
        for (var i=0;i<heroes.Length;i++)
        {
            var id=H(heroes[i]); var instance=new RosterHeroInstanceDto { InstanceId="diag-"+i, ContentId=id }; run.Roster.Add(instance);
            var definition=(UnitDefinition)package.Content.Catalog.Heroes.Single(x=>x.StableId==id).Definition;
            var cell=oldPlacement ? (i==0 ? new Vector2I(2,2) : definition.AttackRange<2
                    ? new Vector2I(2,i<3?i-1:i+1) : new Vector2I(i%2,i<3?i+1:i-2))
                : definition.AttackRange<2 ? meleeCells[melee++] : rangedCells[ranged++];
            run.Deployment[BattlefieldLayout.PlayerDeploymentSlot(cell)]=instance.InstanceId;
        }
        var deployedIds=run.Deployment.Where(x=>!string.IsNullOrWhiteSpace(x)).ToArray();
        if(deployedIds.Length!=heroes.Length||deployedIds.Distinct(StringComparer.Ordinal).Count()!=heroes.Length)
            throw new InvalidOperationException($"Deployment lost a hero for {team.Id}: heroes={heroes.Length} deployed={deployedIds.Length}");
        foreach(var g in gear) { var owner=run.Roster[g.Owner]; owner.Equipment.Add(new(){ InstanceId=$"gear-{g.Owner}-{owner.Equipment.Count}", ContentId=E(g.Id), OwnerHeroInstanceId=owner.InstanceId, SlotIndex=owner.Equipment.Count }); }
        if(relic.Length>0) run.Items.Add(new(){InstanceId="relic",ContentId=relic});
        var encounter=new TowerGenerator(package.Project.Campaign).Encounter(run,kind);
        var config=new RunBattlePreparationService(package.Content,package.Project,new RunRelicService(package.Content)).Build(run,encounter,true);
        var prepared=config.Spawns.Where(x=>x.Team==0&&!x.IsTemporary).Select(x=>x.InstanceId).ToHashSet(StringComparer.Ordinal);
        var expected=run.Roster.Select(x=>x.InstanceId).ToHashSet(StringComparer.Ordinal);
        if(!prepared.SetEquals(expected))throw new InvalidOperationException($"Prepared hero identity mismatch for {team.Id}");
        using var battle=new BattleSimulation(config);
        var hardObserved=new Dictionary<string,int>(StringComparer.Ordinal); var slowObserved=new Dictionary<string,int>(StringComparer.Ordinal);
        while(battle.Outcome==BattleOutcome.Running)
        {
            battle.Step();
            foreach(var u in battle.Units.Where(x=>x.Alive))
            {
                if(u.DisabledTicks>0||u.Statuses.Any(s=>IsHard(package,s.StableId))) hardObserved[u.RuntimeId]=hardObserved.GetValueOrDefault(u.RuntimeId)+1;
                if(u.Statuses.Any(s=>IsSlow(package,s.StableId))) slowObserved[u.RuntimeId]=slowObserved.GetValueOrDefault(u.RuntimeId)+1;
            }
        }
        var result=battle.CreateResult(); var events=battle.CombatEvents.ToArray();
        var reported=result.Units.Where(x=>x.Team==0&&!x.IsTemporary).Select(x=>x.SourceInstanceId).ToHashSet(StringComparer.Ordinal);
        if(!reported.SetEquals(expected))throw new InvalidOperationException($"Reported hero identity mismatch for {team.Id}");
        var summonParent=events.Where(x=>x.Kind==BattleCombatEventKind.UnitSummoned).GroupBy(x=>x.TargetRuntimeId).ToDictionary(x=>x.Key,x=>x.First().SourceRuntimeId);
        foreach(var summoned in battle.Units.Where(x=>x.IsTemporary&&!string.IsNullOrEmpty(x.SummonerRuntimeId)))
            if(!summonParent.TryGetValue(summoned.RuntimeId,out var eventParent)||eventParent!=summoned.SummonerRuntimeId)
                throw new InvalidOperationException($"Summon parent mismatch {summoned.RuntimeId}: state={summoned.SummonerRuntimeId} event={eventParent}");
        string Root(string id) { var seen=new HashSet<string>(); while(summonParent.TryGetValue(id,out var p)&&seen.Add(id)) id=p; return id; }
        var reportById=result.Units.ToDictionary(x=>x.RuntimeId,StringComparer.Ordinal);
        var units=result.Units.Select(u=>{
            var ev=events.Where(x=>x.SourceRuntimeId==u.RuntimeId).ToArray();
            var heal=ev.Where(x=>x.Kind==BattleCombatEventKind.HealingResolved).ToArray(); var shield=ev.Where(x=>x.Kind==BattleCombatEventKind.ShieldResolved).ToArray();
            var casts=ev.Where(x=>x.Kind==BattleCombatEventKind.ManaSkillResolved).ToArray(); var hard=ev.Where(x=>x.Kind==BattleCombatEventKind.ControlApplied).ToArray();
            var slow=ev.Where(x=>x.Kind==BattleCombatEventKind.StatusApplied&&IsSlow(package,x.SubjectStableId)).ToArray();
            var owned=result.Units.Where(x=>x.IsTemporary&&Root(x.RuntimeId)==u.RuntimeId).ToArray();
            var abilityCasts=casts.GroupBy(x=>x.SubjectStableId).Select(x=>(object)new{AbilityId=x.Key,Count=x.Count(),FirstTick=x.Min(e=>e.Tick)}).ToArray();
            var damageSources=events.Where(x=>x.SourceRuntimeId==u.RuntimeId&&x.Kind==BattleCombatEventKind.DamageResolved)
                .GroupBy(x=>new{x.Source.Kind,x.Source.StableId}).Select(x=>(object)new{Kind=x.Key.Kind.ToString(),StableId=x.Key.StableId,Damage=x.Sum(e=>e.EffectiveValue),Hits=x.Count()}).ToArray();
            return new UnitTelemetry(u.RuntimeId,u.SourceInstanceId,u.ContentId,u.DisplayName,u.Team,u.IsTemporary,u.Alive,u.FinalHealth,u.MaxHealth,u.FinalShield,
                u.DamageDealt,u.DamageTaken,Math.Max(0,u.DamageTaken-u.ShieldAbsorbed),u.ShieldAbsorbed,u.HealingDone,
                heal.Sum(x=>x.RequestedValue),heal.Sum(x=>x.AppliedValue),heal.Sum(x=>Math.Max(0,x.AppliedValue-x.EffectiveValue)),shield.Sum(x=>x.EffectiveValue),
                u.Kills,u.JoinTick,u.DefeatTick,u.ActiveTicks,u.AttackActions,u.EffectiveHealingEvents,casts.Length,casts.Length==0?null:casts.Min(x=>x.Tick),
                battle.PendingEvents.Count(x=>x.SourceRuntimeId==u.RuntimeId&&x.Type=="line_release"),hard.Length,hard.Sum(x=>x.EffectiveValue),slow.Length,
                hardObserved.GetValueOrDefault(u.RuntimeId),slowObserved.GetValueOrDefault(u.RuntimeId),summonParent.GetValueOrDefault(u.RuntimeId,""),u.IsTemporary?Root(u.RuntimeId):"",
                owned.Sum(x=>x.DamageDealt),owned.Sum(x=>x.HealingDone),abilityCasts,damageSources);
        }).ToArray();
        var compiled=package.Project.Campaign.Regions[floor/package.Project.Campaign.FloorsPerRegion].Encounters[kind];
        var local=floor%package.Project.Campaign.FloorsPerRegion;
        float Mul(ImmutableArray<float> xs)=>xs.IsDefaultOrEmpty?1:xs[Math.Min(local,xs.Length-1)];
        var traitSnapshot=TraitSnapshotBuilder.Build(config.Traits.Definitions,config.Traits.Contributions);
        var economy=heroes.Select(id=>{var contentId=H(id);var definition=(UnitDefinition)package.Content.Catalog.Heroes.Single(x=>x.StableId==contentId).Definition;return (object)new { ContentId=contentId, Tier=package.Project.Campaign.RecruitmentSupply?.TierOf(contentId)??0, AuthoredRecruitCost=definition.RecruitCost, RecruitmentPrice=(int?)null, InFormalPool=package.Project.Campaign.RecruitmentPool.ContentIds.Contains(contentId) };}).ToArray();
        var equipment=gear.Select(g=>{var id=E(g.Id);var def=(ItemDefinition)package.Content.Catalog.Items.Single(x=>x.StableId==id).Definition;return (object)new{Owner=g.Owner,ContentId=id,def.Price};}).ToArray();
        object? relicInfo=null; if(relic.Length>0){var def=(ItemDefinition)package.Content.Catalog.Items.Single(x=>x.StableId==relic).Definition;relicInfo=new{ContentId=relic,def.Price};}
        var baseId=team.Id.EndsWith("-old-placement",StringComparison.Ordinal)?team.Id[..^14]:team.Id;
        return new(team.Id,team.Category,team.Note,floor+1,kind.ToString(),seed,oldPlacement?"enemy-calibration-legacy":"diagnostic-default",
            $"{baseId}|{floor+1}|{kind}|{seed}",encounter.EncounterId,encounter.Title,encounter.CompositionId,encounter.FloorRuleId,
            compiled.EnemyHealthMultiplier*Mul(compiled.LocalHealthMultipliers),compiled.EnemyDamageMultiplier*Mul(compiled.LocalDamageMultipliers),result.Outcome.ToString(),result.Ticks,result.Digest,
            heroes,economy,equipment,relicInfo,traitSnapshot.Values.Where(x=>x.Team==0).Select(x=>(object)new{x.TraitId,x.Value,ActiveMin=x.ActiveBreakpoint?.MinValue}).ToArray(),
            config.Spawns.Select(x=>(object)new{x.InstanceId,ContentId=x.Unit.ContentId,x.Team,x.Cell}).ToArray(),config.Spawns.Select(x=>(object)new{x.InstanceId,ContentId=x.Unit.ContentId,x.Team,Health=x.Unit.MaxHealth*x.HealthRatio,Attack=x.Unit.Damage,x.Unit.Armor,Range=x.Unit.Range,x.Unit.AttackTicks,x.Unit.MoveTicks}).ToArray(),units);
    }
    private static bool IsHard(CompiledGamePackage p,string id)=>id.Length>0&&p.Content.Graph.TryGetStatus(id,out var s)&&s.Behavior==StatusBehaviorKind.DisableActions;
    private static bool IsSlow(CompiledGamePackage p,string id)=>id.Length>0&&p.Content.Graph.TryGetStatus(id,out var s)&&s.Disposition==StatusDisposition.Harmful&&s.AttributeModifiers.Any(m=>m.Attribute is CombatAttribute.MoveSpeed or CombatAttribute.AttackSpeed);
    private static void Validate(List<BattleRecord> records,bool quick)
    {
        if(records.Count==0)throw new InvalidOperationException("No diagnostic records.");
        foreach(var r in records)foreach(var u in r.Units)
        {
            if(u.HealthDamageTaken < -.01f || Math.Abs(u.DamageTaken-u.ShieldAbsorbed-u.HealthDamageTaken)>.05f)throw new InvalidOperationException("Damage telemetry conservation failed.");
            if(u.HealingOverflow < -.01f)throw new InvalidOperationException("Healing telemetry conservation failed.");
            var damageEvents=u.DamageSources.Sum(x=>(float)((JsonElement)JsonSerializer.SerializeToElement(x)).GetProperty("Damage").GetDouble());
            if(Math.Abs(damageEvents-u.DamageDealt)>.1f)throw new InvalidOperationException($"Damage event/report mismatch {r.CaseId}/{u.RuntimeId}: {damageEvents} != {u.DamageDealt}");
            if(Math.Abs(u.HealingDone-(u.HealingApplied-u.HealingOverflow))>.1f)throw new InvalidOperationException($"Healing event/report mismatch {r.CaseId}/{u.RuntimeId}");
        }
        if(!quick&&records.GroupBy(x=>new{x.CaseId,x.Floor,x.Kind}).Any(x=>x.Count()!=Seeds.Length))throw new InvalidOperationException("Seed matrix incomplete.");
    }
    private static object Fingerprint()
    {
        var root=ProjectSettings.GlobalizePath("res://");
        var files=Directory.EnumerateFiles(Path.Combine(root,"src"),"*.cs",SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(Path.Combine(root,"content"),"*.tres",SearchOption.AllDirectories))
            .Concat(Directory.EnumerateFiles(Path.Combine(root,"content"),"*.tscn",SearchOption.AllDirectories))
            .Append(Path.Combine(root,"tests","BuildMatchupDiagnostics.cs")).OrderBy(x=>x,StringComparer.Ordinal).ToArray();
        var manifest=new List<object>();using var total=SHA256.Create();
        foreach(var file in files){var bytes=File.ReadAllBytes(file);var relative=Path.GetRelativePath(root,file).Replace('\\','/');var hash=Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();var line=Encoding.UTF8.GetBytes(relative+"\0"+hash+"\n");total.TransformBlock(line,0,line.Length,null,0);manifest.Add(new{Path=relative,Sha256=hash});}
        total.TransformFinalBlock([],0,0);return new{TotalSha256=Convert.ToHexString(total.Hash!).ToLowerInvariant(),FileCount=files.Length,Files=manifest};
    }
}
