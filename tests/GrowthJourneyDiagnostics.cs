using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Composition;
using TowerAutobattler.Domain;
using TowerAutobattler.Growth;
using TowerAutobattler.Project;
using TowerAutobattler.Run;

public partial class GrowthJourneyDiagnostics : Node
{
    private const string Output = "res://design-discussion/04-content-validation/artifacts/growth-route/runtime";
    private static readonly ulong[] Seeds = [1776, 2026, 9173];
    private static readonly TowerNodeType[] CommonPriority =
        [TowerNodeType.Recruitment, TowerNodeType.Combat, TowerNodeType.Event, TowerNodeType.Rest, TowerNodeType.Shop, TowerNodeType.Elite, TowerNodeType.Boss];
    private const string GatherSpell = "gather_momentum";
    private const string GuardSpell = "guard_formation";
    private const string GatherBattleAbility = "growth_spell_gather_momentum";
    private const string GuardBattleAbility = "growth_spell_guard_formation";
    private const int GuardDurationTicks = 180;
    private sealed record Strategy(string Id, string Label, string[] PreferredHeroes, GrowthProductionMode Mode,
        TowerNodeType[] NodePriority, string SpellId = "", bool AvoidProducers = false, bool LateProducer = false,
        bool SkipInvestment = false, bool Scatter = false);
    private sealed record DifficultyProfile(string Id,
        float RegionTwoNormalHealth, float RegionTwoNormalDamage, float RegionTwoBossHealth, float RegionTwoBossDamage,
        float RegionThreeNormalHealth, float RegionThreeNormalDamage, float RegionThreeBossHealth, float RegionThreeBossDamage);
    private static readonly DifficultyProfile[] Profiles =
    [
        new("authored", 1, 1, 1, 1, 1, 1, 1, 1),
        new("gentle", 1.1f, 1.05f, 1.5f, 1.25f, 1.25f, 1.15f, 1.3f, 1.2f),
        new("standard", 1.25f, 1.15f, 2f, 1.4f, 1.6f, 1.3f, 1.7f, 1.4f),
        new("pressure", 1.5f, 1.25f, 2.5f, 1.5f, 2f, 1.45f, 2.1f, 1.55f)
    ];
    private static readonly Strategy[] Strategies =
    [
        new("frost-battle", "霜系战斗路线", ["hero_mx01", "hero_mx02", "hero_mx03", "hero_mx04", "hero_mx05"],
            GrowthProductionMode.Attack, CommonPriority),
        new("construct-growth", "构装生命生产", ["hero_mx25", "hero_mx21", "hero_mx22", "hero_mx23", "hero_mx24"],
            GrowthProductionMode.Vitality, CommonPriority),
        new("construct-research", "构装研究蓄势", ["hero_mx25", "hero_mx21", "hero_mx22", "hero_mx23", "hero_mx24"],
            GrowthProductionMode.Research, CommonPriority, GatherSpell),
        new("construct-no-growth", "构装无生产者", ["hero_mx21", "hero_mx22", "hero_mx23", "hero_mx24", "hero_mx43", "hero_mx40"],
            GrowthProductionMode.Attack, CommonPriority, AvoidProducers: true),
        new("construct-late-growth", "构装后期启用工坊", ["hero_mx25", "hero_mx21", "hero_mx22", "hero_mx23", "hero_mx24"],
            GrowthProductionMode.Attack, CommonPriority, LateProducer: true),
        new("scattered-no-investment", "低重叠偏好无主动培养", [], GrowthProductionMode.Attack, CommonPriority,
            AvoidProducers: true, SkipInvestment: true, Scatter: true),
        new("construct-concentrated", "多生产者集中核心", ["hero_mx25", "hero_mx10", "hero_mx30", "hero_mx23", "hero_mx21", "hero_mx22"],
            GrowthProductionMode.Attack, CommonPriority),
        new("frost-elite", "霜系精英优先", ["hero_mx01", "hero_mx02", "hero_mx03", "hero_mx04", "hero_mx05"],
            GrowthProductionMode.Attack, CommonPriority),
        new("growth-elite", "构装成长精英优先", ["hero_mx25", "hero_mx21", "hero_mx22", "hero_mx23", "hero_mx24"],
            GrowthProductionMode.Vitality, CommonPriority)
    ];

    public override async void _Ready()
    {
        try
        {
            var publication = await GrowthContentPackage.CreateReadyAsync(this);
            var package = publication.Package ?? throw new InvalidOperationException(string.Join(';', publication.Report.CoreErrors));
            var rules = GrowthContentPackage.LoadRules(package.Content);
            foreach (var strategy in Strategies.Where(strategy => !string.IsNullOrEmpty(strategy.SpellId)))
                Require(rules.Spells.ContainsKey(strategy.SpellId), $"strategy {strategy.Id} references unknown run spell {strategy.SpellId}");
            Require(rules.Spells.ContainsKey(GatherSpell) && rules.Spells.ContainsKey(GuardSpell),
                "checkpoint spell comparison requires gather_momentum and guard_formation");
            var journeys = new List<object>();
            var profileTimings = new List<object>();
            var defaultSuites = Strategies.Where(strategy => !strategy.Id.EndsWith("-elite", StringComparison.Ordinal)).ToArray();
            var selectedStrategies = SelectByArgument(Strategies, defaultSuites, "suite", strategy => strategy.Id);
            var selectedProfiles = SelectByArgument(Profiles, [Profiles.Single(profile => profile.Id == "authored")], "profile", profile => profile.Id);
            var selectedSeeds = SelectSeeds();
            var outputBase = Argument("output") ?? "growth-authored-journeys";
            foreach (var profile in selectedProfiles)
            {
                var timer = Stopwatch.StartNew();
                var profiledPackage = package with { Project = package.Project with { Campaign = ApplyProfile(package.Project.Campaign, profile) } };
                foreach (var strategy in selectedStrategies)
                foreach (var seed in selectedSeeds)
                {
                    journeys.Add(RunJourney(profiledPackage, rules, strategy, profile, seed));
                    GD.Print($"GROWTH_JOURNEY_PROGRESS profile={profile.Id} strategy={strategy.Id} seed={seed}");
                    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                }
                timer.Stop();
                profileTimings.Add(new { profile.Id, ElapsedSeconds = timer.Elapsed.TotalSeconds });
                GD.Print($"GROWTH_PROFILE_COMPLETE profile={profile.Id} elapsed_seconds={timer.Elapsed.TotalSeconds:F2}");
            }
            Write(outputBase + ".json", new
            {
                GeneratedUtc = DateTime.UtcNow,
                Seeds = selectedSeeds,
                Profiles = selectedProfiles,
                EffectiveEncounterMultipliers = selectedProfiles.Select(profile => new
                {
                    profile.Id,
                    Encounters = ApplyProfile(package.Project.Campaign, profile).Regions.SelectMany(region => region.Encounters.Values)
                        .Select(encounter => new { encounter.StableId, encounter.NodeType, encounter.EnemyHealthMultiplier, encounter.EnemyDamageMultiplier })
                }),
                ProfileTimings = profileTimings,
                Strategies = selectedStrategies.Select(x => new { x.Id, x.Label, x.Mode, x.PreferredHeroes, x.NodePriority,
                    x.SpellId, x.AvoidProducers, x.LateProducer, x.SkipInvestment, x.Scatter,
                    ScatterThreshold = x.Scatter ? "每次招募最小化当前已拥有角色重复×10 + 羁绊贡献重复数；不保证零重叠。" : "不适用" }),
                Journeys = journeys,
                TelemetryCoverage = new[] { "伤害", "治疗", "控制施加次数与有效量", "承伤与护盾吸收", "普攻次数", "技能逐ID释放次数" },
                MissingTelemetry = new[] { "控制来源没有独立的持续时间归因；仅保存ControlApplied事件的次数与有效量", "非战斗节点没有战斗遥测" },
                Interpretation = "固定种子测试 profile 诊断；不注入金币、材料或成长，不保证通关，也不代表总体胜率。"
            });
            WriteMarkdown(outputBase, journeys.Count);
            GD.Print($"GROWTH_JOURNEY_DIAGNOSTICS_OK profiles={selectedProfiles.Length} strategies={selectedStrategies.Length} journeys={journeys.Count}");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PrintErr("GROWTH_JOURNEY_DIAGNOSTICS_FAILED " + error); GetTree().Quit(1); }
    }

    private static object RunJourney(CompiledGamePackage package, CompiledGrowthRules rules, Strategy strategy, DifficultyProfile profile, ulong seed)
    {
        var save = new MemorySave();
        var app = new RunApplication(package.Content, save, package.Project, rules);
        Require(app.BeginOpeningRecruitment(seed), "opening recruitment did not start");
        var opening = app.ActiveRun!.OpeningRecruitment ?? throw new InvalidOperationException("opening offer missing");
        foreach (var id in OpeningChoices(app, opening.CandidateIds, strategy, 2))
            Require(app.ToggleOpeningHero(id), "opening selection rejected: " + id);
        Require(app.ConfirmOpeningRecruitment(), "opening confirmation rejected");
        var steps = new List<object>();
        object? spellCheckpoint = null;
        var earlyWorkshopSeen = false;
        var eliteNodesEntered = 0;
        var availableEliteOptions = new int[package.Project.Campaign.Regions.Length];
        var eliteNodesByRegion = new int[package.Project.Campaign.Regions.Length];
        var stopped = "floor-limit";

        while (app.ActiveRun is { } run && run.FloorIndex < package.Project.Campaign.TotalFloors)
        {
            ResolvePendingOffer(app, strategy);
            ResolveDiscovery(app, strategy);
            if (app.ActiveRun is null) break;
            run = app.ActiveRun;
            PrepareRoster(app, strategy);
            ConfigureGrowth(app, rules, strategy);
            var wasEarly = run.FloorIndex < package.Project.Campaign.FloorsPerRegion;
            TowerNodeType selectedType;
            if (run.PendingNode) selectedType = run.SelectedNode;
            else
            {
                var options = app.CurrentOptions();
                if (options.Count == 0) { stopped = "no-options"; break; }
                var regionIndex = run.FloorIndex / package.Project.Campaign.FloorsPerRegion;
                var localFloor = run.FloorIndex % package.Project.Campaign.FloorsPerRegion;
                var elite = options.FirstOrDefault(option => option.Type == TowerNodeType.Elite);
                if (elite is not null) availableEliteOptions[regionIndex]++;
                var selected = strategy.Id.EndsWith("-elite", StringComparison.Ordinal) && localFloor >= 2 &&
                               eliteNodesByRegion[regionIndex] == 0 && elite is not null
                    ? elite
                    : strategy.NodePriority.Select(kind => options.FirstOrDefault(x => x.Type == kind))
                    .FirstOrDefault(x => x is not null) ?? throw new InvalidOperationException("node priority did not match an option");
                Require(app.SelectNode(selected.Type), "node selection rejected");
                selectedType = selected.Type;
            }
            if (selectedType == TowerNodeType.Elite)
            {
                eliteNodesEntered++;
                eliteNodesByRegion[run.FloorIndex / package.Project.Campaign.FloorsPerRegion]++;
            }

            object? battle = null;
            if (selectedType is TowerNodeType.Combat or TowerNodeType.Elite or TowerNodeType.Boss)
            {
                var encounter = app.CurrentEncounter();
                PrepareRoster(app, strategy);
                if (spellCheckpoint is null && strategy.Id == "construct-research" && app.ActiveRun!.Growth!.Research >= 4)
                    spellCheckpoint = RunSpellCheckpoint(package, rules, save, encounter);
                if (selectedType is TowerNodeType.Elite or TowerNodeType.Boss)
                    ConfigureSpell(app, rules, strategy.SpellId);
                var begin = app.TryBeginGrowthBattle(encounter);
                Require(begin.Succeeded, "growth battle begin rejected: " + begin.Message);
                var observed = Observe(app.BuildBattleConfig(encounter), app, selectedType);
                battle = observed.Record;
                var completed = app.ResolveBattle(observed.Result, encounter);
                Require(completed.Accepted, "battle settlement rejected: " + completed.Failure);
                if (observed.Result.Outcome == BattleOutcome.PlayerDefeat && selectedType == TowerNodeType.Boss)
                    stopped = "boss-defeat";
            }
            if (app.ActiveRun is not null) ResolvePendingOffer(app, strategy);
            if (app.ActiveRun is not null) ResolveDiscovery(app, strategy);
            earlyWorkshopSeen |= wasEarly && (app.ActiveRun ?? run).Roster.Any(hero => hero.ContentId == "hero_mx25");
            steps.Add(Snapshot(app.ActiveRun ?? run, selectedType, battle));
            if (app.ActiveRun is null) { stopped = stopped == "boss-defeat" ? stopped : "terminal"; break; }

            // Reload after every settled node through the same serialized save service.
            app = new RunApplication(package.Content, save, package.Project, rules);
            Require(app.ActiveRun is not null, "saved journey did not reload");
        }
        return new { Profile = profile.Id, strategy.Id, strategy.Label, Seed = seed, Stopped = stopped, EarlyWorkshopSeen = earlyWorkshopSeen,
            EarlyWorkshopNote = earlyWorkshopSeen ? "首区实际取得过构装工坊。" : "首区供应未出现或没有选到构装工坊；未补造机会。",
            SaveWrites = save.Writes, AvailableEliteOptions = availableEliteOptions, EliteNodesEntered = eliteNodesEntered,
            EliteNodesByRegion = eliteNodesByRegion,
            SpellCheckpoint = spellCheckpoint, Steps = steps };
    }

    private static void ConfigureGrowth(RunApplication app, CompiledGrowthRules rules, Strategy strategy)
    {
        var run = app.ActiveRun!;
        if (strategy.LateProducer && run.FloorIndex < app.Project.Campaign.FloorsPerRegion * 2) return;
        var sharedTarget = GrowthTarget(run, strategy);
        foreach (var producer in run.Roster.Where(hero => rules.Heroes.ContainsKey(hero.ContentId)))
        {
            var definition = rules.Heroes[producer.ContentId];
            if (definition.ProductionModes.IsEmpty || strategy.AvoidProducers || strategy.SkipInvestment) continue;
            var mode = definition.ProductionModes.Contains(strategy.Mode) ? strategy.Mode : definition.ProductionModes.First();
            var targetInstance = mode == GrowthProductionMode.Research ? producer.InstanceId : run.Roster.First(x => x.ContentId == sharedTarget).InstanceId;
            var assignment = app.SetGrowthAssignment(producer.InstanceId, targetInstance, mode);
            Require(assignment.Succeeded, "assignment rejected: " + assignment.Message);
        }
        run = app.ActiveRun!;
        if (run.Growth?.PendingDiscovery is not null) ResolveDiscovery(app, strategy);
        run = app.ActiveRun!;
        if (strategy.SkipInvestment) return;
        foreach (var hero in Rank(run.Roster.Select(x => x.ContentId), strategy).Select(id => run.Roster.First(x => x.ContentId == id)))
        {
            if (hero.Growth.AscensionId.Length > 0 || !rules.Heroes.ContainsKey(hero.ContentId)) continue;
            var result = app.AscendHero(hero.InstanceId);
            if (result.Succeeded) { ResolveDiscovery(app, strategy); break; }
        }
        run = app.ActiveRun!;
    }

    private static void ConfigureSpell(RunApplication app, CompiledGrowthRules rules, string spellId)
    {
        if (string.IsNullOrEmpty(spellId)) return;
        if (!rules.Spells.TryGetValue(spellId, out var spell))
            throw new InvalidOperationException("unknown configured run spell: " + spellId);
        var run = app.ActiveRun!;
        if (run.Growth!.Research >= spell.ResearchCost)
        {
            var crafted = app.CraftGrowthSpell(spell.StableId);
            Require(crafted.Succeeded, "spell craft rejected: " + crafted.Message);
        }
        run = app.ActiveRun!;
        if (run.Growth!.SpellInventory.GetValueOrDefault(spellId) > 0)
        {
            var target = SpellTarget(app, rules, spellId);
            Require(!string.IsNullOrEmpty(target), "no legal deployed target for spell " + spellId);
            var equipped = app.EquipGrowthSpell(spellId, target);
            Require(equipped.Succeeded, "spell equip rejected: " + equipped.Message);
        }
        else if (!string.IsNullOrEmpty(run.Growth.EquippedSpellId))
        {
            var unequipped = app.EquipGrowthSpell(string.Empty, string.Empty);
            Require(unequipped.Succeeded, "spell unequip rejected: " + unequipped.Message);
        }
    }

    private static string SpellTarget(RunApplication app, CompiledGrowthRules rules, string spellId)
    {
        var run = app.ActiveRun!;
        var deployed = run.Roster.Where(hero => run.Deployment.Contains(hero.InstanceId)).ToArray();
        if (spellId == GuardSpell)
            return deployed.OrderBy(hero => Backline(app, hero.ContentId))
                .ThenBy(hero => Array.IndexOf(new[] { "hero_mx21", "hero_mx01", "hero_mx22", "hero_mx24" }, hero.ContentId) is var rank && rank >= 0 ? rank : int.MaxValue)
                .Select(hero => hero.InstanceId).FirstOrDefault() ?? string.Empty;
        if (spellId != GatherSpell) return deployed.FirstOrDefault()?.InstanceId ?? string.Empty;
        return deployed.Where(hero => rules.Heroes.TryGetValue(hero.ContentId, out var growthHero) &&
                    EffectiveLoadout(hero, growthHero)?.Abilities.Any(ability => ability.ManaCost > 0) == true)
                .OrderBy(hero => Array.IndexOf(new[] { "hero_mx23", "hero_mx22", "hero_mx03", "hero_mx05", "hero_mx25" }, hero.ContentId) is var rank && rank >= 0 ? rank : int.MaxValue)
                .ThenByDescending(hero => EffectiveLoadout(hero, rules.Heroes[hero.ContentId])!.Abilities.Max(ability => ability.ManaCost))
                .Select(hero => hero.InstanceId).FirstOrDefault() ?? string.Empty;
    }

    private static TowerAutobattler.Abilities.CompiledAbilityLoadout? EffectiveLoadout(
        RosterHeroInstanceDto hero, CompiledGrowthHero definition) =>
        !string.IsNullOrEmpty(hero.Growth.AscensionId) ? definition.AscendedLoadout : definition.BaseLoadout;

    private static string GrowthTarget(ActiveRunDto run, Strategy strategy)
    {
        var preferred = strategy.Mode == GrowthProductionMode.Attack
            ? new[] { "hero_mx23", "hero_mx03", "hero_mx22", "hero_mx21" }
            : new[] { "hero_mx21", "hero_mx01", "hero_mx23", "hero_mx22" };
        return preferred.FirstOrDefault(id => run.Roster.Any(hero => hero.ContentId == id))
            ?? Rank(run.Roster.Where(hero => hero.ContentId != "hero_mx25").Select(hero => hero.ContentId), strategy).FirstOrDefault()
            ?? run.Roster[0].ContentId;
    }

    private static void PrepareRoster(RunApplication app, Strategy strategy)
    {
        var run = app.ActiveRun ?? throw new InvalidOperationException("active run missing during preparation");
        var capacity = Math.Min(run.CurrentPopulation, app.Rules.OrdinaryPopulationCap);
        var eligible = strategy.LateProducer && run.FloorIndex < app.Project.Campaign.FloorsPerRegion * 2
            ? run.Roster.Where(hero => !IsProducer(hero.ContentId))
            : run.Roster;
        var ordered = eligible.OrderBy(hero => Array.IndexOf(strategy.PreferredHeroes, hero.ContentId) is var rank && rank >= 0 ? rank : int.MaxValue)
            .ThenBy(hero => Backline(app, hero.ContentId))
            .ThenBy(hero => hero.InstanceId, StringComparer.Ordinal).Take(capacity).ToArray();
        var selectedIds = ordered.Select(hero => hero.InstanceId).ToHashSet(StringComparer.Ordinal);
        foreach (var deployed in run.Deployment.Where(id => !string.IsNullOrEmpty(id) && !selectedIds.Contains(id)).Distinct(StringComparer.Ordinal).ToArray())
            Require(app.WithdrawDeploymentUnit(deployed), "reserve hero could not be withdrawn for deterministic formation: " + deployed);
        var claimedSlots = run.Deployment.Select((id, slot) => (id, slot))
            .Where(x => selectedIds.Contains(x.id)).Select(x => x.slot).ToHashSet();
        foreach (var hero in ordered)
        {
            if (app.ActiveRun!.Deployment.Contains(hero.InstanceId)) continue;
            var front = !Backline(app, hero.ContentId);
            var slots = BattlefieldLayout.PlayerDeploymentCells.Select((cell, slot) => (cell, slot))
                .Where(x => front ? x.cell.X == 2 : x.cell.X == 0)
                .OrderBy(x => Math.Abs(x.cell.Y - 2.5f)).Select(x => x.slot)
                .Concat(BattlefieldLayout.PlayerDeploymentCells.Select((_, slot) => slot));
            foreach (var slot in slots)
            {
                if (claimedSlots.Contains(slot)) continue;
                if (!app.MoveDeploymentUnit(hero.InstanceId, slot)) continue;
                claimedSlots.Add(slot);
                break;
            }
        }
        Require(ordered.Length <= capacity && ordered.All(hero => app.ActiveRun!.Deployment.Contains(hero.InstanceId)),
            "selected roster was not fully deployed within population capacity");
        run = app.ActiveRun!;
        foreach (var item in run.EquipmentInventory.ToArray())
        {
            var current = app.ActiveRun!;
            var target = ordered.Select(hero => current.Roster.SingleOrDefault(candidate => candidate.InstanceId == hero.InstanceId))
                .FirstOrDefault(hero => hero is not null && hero.Equipment.Count < app.Rules.EquipmentSlotCapacity);
            if (target is null) break;
            var free = Enumerable.Range(0, app.Rules.EquipmentSlotCapacity).First(slot => target.Equipment.All(x => x.SlotIndex != slot));
            Require(app.EquipOwnedItem(item.InstanceId, target.InstanceId, free), "owned equipment could not be equipped: " + item.InstanceId);
        }
    }

    private static bool Backline(RunApplication app, string contentId) =>
        app.Content.TryGet(contentId, out var entry) && entry.Definition is TowerAutobattler.Content.UnitDefinition unit &&
        unit.AttackRange > 2.5f;

    private static void ResolveDiscovery(RunApplication app, Strategy strategy)
    {
        var candidates = app.ActiveRun?.Growth?.PendingDiscovery?.CandidateIds;
        if (candidates is null) return;
        var result = app.ChooseGrowthHero(Rank(candidates, strategy).First());
        Require(result.Succeeded, "growth discovery rejected: " + result.Message);
    }

    private static void ResolvePendingOffer(RunApplication app, Strategy strategy)
    {
        var offer = app.PendingOffer;
        if (offer is null) return;
        var eligible = offer.Choices.Where(choice => app.CheckOfferChoice(choice).Succeeded).ToArray();
        CompiledRunChoice? chosen = eligible.OrderByDescending(choice => ChoiceScore(app, choice, strategy)).ThenBy(choice => choice.StableId, StringComparer.Ordinal).FirstOrDefault();
        if (offer.AllowSkip && chosen is not null && ChoiceScore(app, chosen, strategy) <= -10000) chosen = null;
        var result = app.ResolveOffer(offer.OfferId, chosen?.StableId);
        if (!result.Succeeded && offer.AllowSkip) result = app.ResolveOffer(offer.OfferId, null);
        Require(result.Succeeded, "offer resolution rejected: " + result.Message);
    }

    private static int ChoiceScore(RunApplication app, CompiledRunChoice choice, Strategy strategy)
    {
        var recruit = choice.Operations.FirstOrDefault(x => x.Kind == RunOperationKind.Recruit)?.ContentId ?? choice.ContentId;
        if (!string.IsNullOrEmpty(recruit) && strategy.AvoidProducers && IsProducer(recruit)) return -10000;
        if (!string.IsNullOrEmpty(recruit) && strategy.Scatter && app.Content.TryGet(recruit, out var candidate) &&
            candidate.Definition is TowerAutobattler.Content.UnitDefinition unit)
        {
            var owned = app.ActiveRun!.Roster.Select(hero => app.Content.TryGet(hero.ContentId, out var entry)
                ? entry.Definition as TowerAutobattler.Content.UnitDefinition : null).Where(definition => definition is not null).ToArray();
            var overlap = owned.Count(definition => definition!.Role == unit.Role) * 10 +
                unit.TraitContributions.Sum(trait => owned.Count(definition => definition!.TraitContributions.Any(x => x.TraitId == trait.TraitId)));
            return 5000 - overlap;
        }
        var rank = Array.IndexOf(strategy.PreferredHeroes, recruit);
        return rank >= 0 ? 10000 - rank * 100 : !string.IsNullOrEmpty(recruit) ? 1000 :
            choice.Operations.Any(x => x.Kind == RunOperationKind.GrantItem) ? 500 : 0;
    }

    private static IEnumerable<string> OpeningChoices(RunApplication app, IEnumerable<string> candidates, Strategy strategy, int count)
    {
        var remaining = candidates.Where(id => !strategy.AvoidProducers || !IsProducer(id)).ToList();
        var chosen = new List<string>();
        while (chosen.Count < count && remaining.Count > 0)
        {
            var next = strategy.Scatter
                ? remaining.OrderBy(id => OpeningOverlap(app, id, chosen)).ThenBy(id => id, StringComparer.Ordinal).First()
                : Rank(remaining, strategy).First();
            chosen.Add(next);
            remaining.Remove(next);
        }
        return chosen;
    }

    private static int OpeningOverlap(RunApplication app, string candidateId, IReadOnlyCollection<string> chosen)
    {
        if (!app.Content.TryGet(candidateId, out var entry) || entry.Definition is not TowerAutobattler.Content.UnitDefinition candidate)
            return int.MaxValue;
        var selected = chosen.Select(id => app.Content.TryGet(id, out var selectedEntry)
            ? selectedEntry.Definition as TowerAutobattler.Content.UnitDefinition : null).Where(unit => unit is not null).ToArray();
        return selected.Count(unit => unit!.Role == candidate.Role) * 10 +
            candidate.TraitContributions.Sum(trait => selected.Count(unit => unit!.TraitContributions.Any(x => x.TraitId == trait.TraitId)));
    }

    private static IEnumerable<string> Rank(IEnumerable<string> ids, Strategy strategy) => ids
        .OrderBy(id => strategy.AvoidProducers && IsProducer(id) ? 1 : 0)
        .ThenBy(id => { var rank = Array.IndexOf(strategy.PreferredHeroes, id); return rank < 0 ? int.MaxValue : rank; })
        .ThenBy(id => id, StringComparer.Ordinal);

    private static bool IsProducer(string id) => id is "hero_mx25" or "hero_mx10" or "hero_mx30";

    private static string? Argument(string name)
    {
        var prefix = $"--{name}=";
        return OS.GetCmdlineUserArgs().FirstOrDefault(argument => argument.StartsWith(prefix, StringComparison.Ordinal))?[prefix.Length..];
    }

    private static T[] SelectByArgument<T>(T[] values, T[] defaults, string name, Func<T, string> id)
    {
        var value = Argument(name);
        if (string.IsNullOrEmpty(value)) return defaults;
        if (value == "all") return values;
        var requested = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.Ordinal);
        var selected = values.Where(value => requested.Contains(id(value))).ToArray();
        if (selected.Length != requested.Count)
            throw new InvalidOperationException($"Unknown {name}: {string.Join(',', requested.Except(selected.Select(id), StringComparer.Ordinal))}");
        return selected;
    }

    private static ulong[] SelectSeeds()
    {
        var value = Argument("seeds");
        if (string.IsNullOrEmpty(value)) return Seeds;
        return value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(seed => ulong.TryParse(seed, out var parsed) ? parsed : throw new InvalidOperationException("invalid seed: " + seed))
            .ToArray();
    }

    private static CompiledCampaign ApplyProfile(CompiledCampaign campaign, DifficultyProfile profile)
    {
        if (profile.Id == "authored") return campaign;
        var regions = campaign.Regions.Select((region, index) => region with
        {
            Encounters = region.Encounters.ToImmutableDictionary(pair => pair.Key,
                pair => ScaleEncounter(pair.Value, profile, index, pair.Key))
        }).ToImmutableArray();
        return campaign with { StableId = $"{campaign.StableId}:diagnostic:{profile.Id}", Regions = regions };
    }

    private static CompiledEncounter ScaleEncounter(CompiledEncounter encounter, DifficultyProfile profile,
        int regionIndex, TowerNodeType node)
    {
        if (regionIndex == 0 || node is not (TowerNodeType.Combat or TowerNodeType.Elite or TowerNodeType.Boss)) return encounter;
        var boss = node == TowerNodeType.Boss;
        var health = regionIndex == 1
            ? boss ? profile.RegionTwoBossHealth : profile.RegionTwoNormalHealth
            : boss ? profile.RegionThreeBossHealth : profile.RegionThreeNormalHealth;
        var damage = regionIndex == 1
            ? boss ? profile.RegionTwoBossDamage : profile.RegionTwoNormalDamage
            : boss ? profile.RegionThreeBossDamage : profile.RegionThreeNormalDamage;
        var alpha = GD.Load<EncounterDefinition>($"res://content/project/encounters/{encounter.StableId}.tres")
            ?? throw new InvalidOperationException("missing alpha encounter base: " + encounter.StableId);
        return encounter with
        {
            EnemyHealthMultiplier = alpha.EnemyHealthMultiplier * health,
            EnemyDamageMultiplier = alpha.EnemyDamageMultiplier * damage
        };
    }

    private static object RunSpellCheckpoint(CompiledGamePackage package, CompiledGrowthRules rules,
        MemorySave checkpoint, EncounterPlan encounter)
    {
        Require(checkpoint.Serialized is not null, "spell checkpoint save is missing");
        return new
        {
            FloorIndex = checkpoint.LoadActiveRun()!.FloorIndex,
            BattleNumber = checkpoint.LoadActiveRun()!.BattleNumber,
            Encounter = encounter.EncounterId,
            Research = checkpoint.LoadActiveRun()!.Growth!.Research,
            Variants = new[]
            {
                RunSpellVariant(package, rules, checkpoint.Serialized!, encounter, "none", string.Empty),
                RunSpellVariant(package, rules, checkpoint.Serialized!, encounter, "guard-front", GuardSpell),
                RunSpellVariant(package, rules, checkpoint.Serialized!, encounter, "gather-mana-core", GatherSpell)
            }
        };
    }

    private static object RunSpellVariant(CompiledGamePackage package, CompiledGrowthRules rules,
        string serializedRun, EncounterPlan encounter, string variant, string spellId)
    {
        var save = new MemorySave(serializedRun);
        var app = new RunApplication(package.Content, save, package.Project, rules);
        var active = app.ActiveRun ?? throw new InvalidOperationException("spell replay did not restore active run");
        Require(active.PendingNode, "spell replay did not restore selected battle node");
        if (string.IsNullOrEmpty(spellId))
        {
            if (!string.IsNullOrEmpty(active.Growth!.EquippedSpellId))
            {
                var unequipped = app.EquipGrowthSpell(string.Empty, string.Empty);
                Require(unequipped.Succeeded, "none replay could not clear equipped spell: " + unequipped.Message);
            }
        }
        else ConfigureSpell(app, rules, spellId);
        var targetInstance = string.IsNullOrEmpty(spellId)
            ? SpellTarget(app, rules, GatherSpell)
            : app.ActiveRun!.Growth!.SpellTargetInstanceId;
        var consumedSpell = app.ActiveRun!.Growth!.EquippedSpellId;
        var begin = app.TryBeginGrowthBattle(encounter);
        Require(begin.Succeeded, "spell replay battle begin rejected: " + begin.Message);
        var config = app.BuildBattleConfig(encounter);
        using var simulation = new BattleSimulation(config);
        while (simulation.Outcome == BattleOutcome.Running && simulation.TickIndex < BattleSimulation.MaxTicks) simulation.Step();
        var result = simulation.CreateResult();
        var events = simulation.CombatEvents.ToArray();
        var targetRuntime = result.Units.FirstOrDefault(unit => unit.SourceInstanceId == targetInstance)?.RuntimeId ?? string.Empty;
        var battleAbility = spellId == GuardSpell ? GuardBattleAbility : spellId == GatherSpell ? GatherBattleAbility : string.Empty;
        var spellEvents = string.IsNullOrEmpty(battleAbility) ? [] : events
            .Where(combatEvent => combatEvent.Source.StableId == battleAbility || combatEvent.SubjectStableId == battleAbility).ToArray();
        var shieldAppliedTick = spellEvents.Where(combatEvent => combatEvent.Kind == BattleCombatEventKind.ShieldResolved)
            .Select(combatEvent => (int?)combatEvent.Tick).Min();
        var firstDamageTick = events.Where(combatEvent => !string.IsNullOrEmpty(targetRuntime) && combatEvent.TargetRuntimeId == targetRuntime &&
                combatEvent.Kind == BattleCombatEventKind.DamageResolved && combatEvent.RequestedValue > 0)
            .Select(combatEvent => (int?)combatEvent.Tick).Min();
        var firstManaSkillTick = events.Where(combatEvent => combatEvent.SourceRuntimeId == targetRuntime &&
                combatEvent.Kind == BattleCombatEventKind.ManaSkillResolved)
            .Select(combatEvent => (int?)combatEvent.Tick).Min();
        return new
        {
            Variant = variant,
            RunSpellId = consumedSpell,
            BattleAbilityId = battleAbility,
            TargetInstanceId = targetInstance,
            TargetContentId = config.Spawns.FirstOrDefault(spawn => spawn.InstanceId == targetInstance)?.Unit.ContentId ?? string.Empty,
            ShieldAppliedTick = shieldAppliedTick,
            ShieldExpiresTick = shieldAppliedTick is null || spellId != GuardSpell ? (int?)null : shieldAppliedTick.Value + GuardDurationTicks,
            FirstTargetDamageTick = firstDamageTick,
            FirstTargetManaSkillTick = firstManaSkillTick,
            GuardExpiredBeforeFirstTargetDamage = shieldAppliedTick is not null && spellId == GuardSpell &&
                (firstDamageTick is null || shieldAppliedTick.Value + GuardDurationTicks <= firstDamageTick.Value),
            ShieldApplied = spellEvents.Where(combatEvent => combatEvent.Kind == BattleCombatEventKind.ShieldResolved)
                .Sum(combatEvent => combatEvent.EffectiveValue),
            Outcome = result.Outcome.ToString(),
            result.Ticks,
            result.Digest,
            PlayerSurvivors = result.Units.Count(unit => unit.Team == 0 && !unit.IsTemporary && unit.Alive)
        };
    }

    private static (BattleResult Result, object Record) Observe(BattleConfig config, RunApplication app, TowerNodeType node)
    {
        using var simulation = new BattleSimulation(config);
        while (simulation.Outcome == BattleOutcome.Running && simulation.TickIndex < BattleSimulation.MaxTicks) simulation.Step();
        var result = simulation.CreateResult();
        var events = simulation.CombatEvents.ToArray();
        var units = result.Units.Select(unit =>
        {
            var own = events.Where(x => x.SourceRuntimeId == unit.RuntimeId).ToArray();
            return new
            {
                unit.RuntimeId, unit.SourceInstanceId, unit.ContentId, unit.Team, unit.IsTemporary, unit.Alive,
                unit.FinalHealth, unit.MaxHealth, unit.FinalShield, unit.DamageDealt, unit.HealingDone, unit.DamageTaken,
                unit.ShieldAbsorbed, unit.AttackActions, unit.Kills,
                ControlApplications = own.Count(x => x.Kind == BattleCombatEventKind.ControlApplied),
                ControlEffectiveValue = own.Where(x => x.Kind == BattleCombatEventKind.ControlApplied).Sum(x => x.EffectiveValue),
                AbilityCasts = own.Where(x => x.Kind == BattleCombatEventKind.AbilityResolved).GroupBy(x => x.SubjectStableId)
                    .Select(group => new { AbilityId = group.Key, Count = group.Count() }).ToArray()
            };
        }).ToArray();
        var run = app.ActiveRun!;
        var traits = app.BuildTraitSnapshot().Values.Where(value => value.Team == 0 && value.Value > 0)
            .Select(value => new { value.TraitId, value.Value, ActiveMin = value.ActiveBreakpoint?.MinValue }).ToArray();
        var productionGains = run.Growth?.History.SelectMany(entry => entry.Gains).Count(gain => gain.Source != "battle") ?? 0;
        var battleGains = run.Growth?.History.SelectMany(entry => entry.Gains).Count(gain => gain.Source == "battle") ?? 0;
        var materialsProduced = run.Growth?.History.Sum(entry => entry.MaterialsGranted) ?? 0;
        var researchProduced = run.Growth?.History.SelectMany(entry => entry.Gains)
            .Where(gain => gain.Source != "battle" && gain.Mode == GrowthProductionMode.Research).Sum(gain => gain.Amount) ?? 0;
        var ascensionSpend = run.Roster.Count(hero => !string.IsNullOrEmpty(hero.Growth.AscensionId)) * app.GrowthRules!.AscensionCost;
        var consumedSpell = run.Growth?.PendingNode?.ConsumedSpellId ?? string.Empty;
        var consumedSpellTarget = run.Growth?.PendingNode?.SpellTargetInstanceId ?? string.Empty;
        var record = new { Node = node.ToString(), IsBoss = node == TowerNodeType.Boss,
            Outcome = result.Outcome.ToString(), result.Ticks, result.Digest, Units = units, Traits = traits,
            DeployedCount = config.Spawns.Count(x => x.Team == 0 && !x.IsTemporary),
            ResourcesBeforeBattle = new { run.Growth?.Materials, run.Growth?.CategoryMaterials, run.Growth?.Research,
                MaterialsProduced = materialsProduced, MaterialsSpentOnAscension = ascensionSpend,
                ResearchProduced = researchProduced, ResearchSpent = researchProduced - (run.Growth?.Research ?? 0),
                ProductionGains = productionGains, BattleGains = battleGains, ConsumedSpellId = consumedSpell,
                ConsumedSpellTargetInstanceId = consumedSpellTarget },
            PermanentGains = result.PermanentGains.Select(gain => new { gain.SourceInstanceId, gain.TargetInstanceId,
                gain.AbilityId, gain.Slot, Attribute = gain.Attribute.ToString(), gain.Amount }).ToArray(),
            Player = config.Spawns.Where(x => x.Team == 0).Select(x =>
            {
                var hero = run.Roster.SingleOrDefault(hero => hero.InstanceId == x.InstanceId);
                var definition = app.Content.TryGet(x.Unit.ContentId, out var content) ? content.Definition as TowerAutobattler.Content.UnitDefinition : null;
                var baseHealth = definition?.MaxHealth ?? 0;
                var baseAttack = definition?.AttackDamage ?? 0;
                return new { x.InstanceId, x.Unit.ContentId, Cell = new { X = x.Cell.X, Y = x.Cell.Y },
                AuthoredMaxHealth = baseHealth, AuthoredAttack = baseAttack,
                AddedMaxHealth = hero?.Growth.AddedMaxHealth ?? 0, AddedAttack = hero?.Growth.AddedAttack ?? 0,
                MaxHealthGrowthPercent = baseHealth > 0 ? (hero?.Growth.AddedMaxHealth ?? 0) / baseHealth * 100 : 0,
                AttackGrowthPercent = baseAttack > 0 ? (hero?.Growth.AddedAttack ?? 0) / baseAttack * 100 : 0,
                StartingMaxHealth = x.Unit.MaxHealth, StartingDamage = x.Unit.Damage,
                AbilityIds = x.Unit.AbilityLoadout?.Abilities.Select(ability => ability.StableId).ToArray() ?? [] };
            }).ToArray(),
            Enemy = config.Spawns.Where(x => x.Team == 1).Select(x => new { x.InstanceId, x.Unit.ContentId }).ToArray() };
        return (result, record);
    }

    private static object Snapshot(ActiveRunDto run, TowerNodeType node, object? battle) => new
    {
        run.FloorIndex, run.BattleNumber, Node = node.ToString(), run.CurrentRunHealth, run.MaximumRunHealth, run.Gold,
        Roster = run.Roster.Select(hero => new { hero.InstanceId, hero.ContentId, hero.Growth.AddedAttack, hero.Growth.AddedMaxHealth,
            hero.Growth.AscensionId, hero.Growth.ProductionMode, hero.Growth.ProductionTargetInstanceId,
            Equipment = hero.Equipment.Select(item => new { item.ContentId, item.SlotIndex }).ToArray() }).ToArray(),
        Growth = run.Growth is null ? null : new { run.Growth.Materials, run.Growth.CategoryMaterials, run.Growth.Research,
            run.Growth.SpellInventory, run.Growth.EquippedSpellId, run.Growth.SpellTargetInstanceId,
            Ledger = run.Growth.History.Select(entry => new { entry.FloorIndex, entry.BattleNumber, entry.IsBattle, entry.MaterialsGranted,
                Gains = entry.Gains.Select(gain => new { gain.ProducerInstanceId, gain.TargetInstanceId, gain.Mode, gain.Amount,
                    gain.Source, gain.AbilityId }).ToArray() }).ToArray() },
        Battle = battle
    };

    private static void Write(string name, object value)
    {
        var folder = ProjectSettings.GlobalizePath(Output); Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, name), JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true }));
    }
    private static void WriteMarkdown(string outputBase, int count)
    {
        var folder = ProjectSettings.GlobalizePath(Output); Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, outputBase + ".md"), $"# 成长征程运行诊断\n\n本次生成 {count} 条真实征程。逐节点数据、成长账本与战斗单位遥测见 `{outputBase}.json`。\n\n诊断不注入金币、材料或成长，不伪造胜利。\n");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }

    private sealed class MemorySave : IRunSaveService
    {
        private string? _run;
        public MemorySave() { }
        public MemorySave(string serializedRun) => _run = serializedRun;
        public string? Serialized => _run;
        public int Writes { get; private set; }
        public MetaProgressDto LoadMeta() => new();
        public SettingsDto LoadSettings() => new();
        public ActiveRunDto? LoadActiveRun() => _run is null ? null : JsonSerializer.Deserialize<ActiveRunDto>(_run);
        public bool SaveMeta(MetaProgressDto value) => true;
        public bool SaveSettings(SettingsDto value) => true;
        public bool SaveActiveRun(ActiveRunDto value) { Writes++; _run = JsonSerializer.Serialize(value); return true; }
        public void DeleteActiveRun() => _run = null;
    }
}
