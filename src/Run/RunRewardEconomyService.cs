using System;
using System.Collections.Generic;
using System.Linq;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.Equipment;
using TowerAutobattler.Project;
using TowerAutobattler.Relics;

namespace TowerAutobattler.Run;

public sealed class RunRewardEconomyService
{
    private readonly ContentRegistry _content;
    private readonly CompiledGameProject _project;
    private readonly CompiledRunRules _rules;
    private readonly RunProgressionPersistenceService _persistence;

    public RunRewardEconomyService(
        ContentRegistry content,
        CompiledGameProject project,
        RunProgressionPersistenceService persistence,
        ActiveRunDto? loadedRun)
    {
        _content = content ?? throw new ArgumentNullException(nameof(content));
        _project = project ?? throw new ArgumentNullException(nameof(project));
        _rules = project.RunRules;
        _persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
        _ = loadedRun;
    }

    public ActiveRunDto? CreateNewRun(string heroId, ulong seed, IReadOnlyCollection<string> unlockedHeroIds)
    {
        if (!unlockedHeroIds.Contains(heroId) || !_content.TryGet(heroId, out var heroEntry) ||
            heroEntry.Definition is not UnitDefinition { IsHero: true })
            return null;
        var run = new ActiveRunDto
        {
            Seed = seed == 0 ? 1UL : seed,
            Gold = _rules.StartingGold
        };
        ActiveRunFormationSchema.InitializeVersion4(run, _rules);
        var startingHero = AddRosterHero(run, heroId, "player-hero");
        if (startingHero is null) return null;
        Place(run, startingHero.InstanceId, BattlefieldLayout.Version2HeroCell);
        var random = new DeterministicRandom(run.Seed);
        var starters = _project.Campaign.StarterPool.ContentIds
            .Select(Required)
            .OrderBy(_ => random.NextInt(0, int.MaxValue))
            .Take(_rules.StarterRosterHeroCount)
            .ToArray();
        for (var index = 0; index < starters.Length; index++)
        {
            var instance = AddRosterHero(run, starters[index].StableId);
            if (instance is null) return null;
            Place(run, instance.InstanceId, BattlefieldLayout.Version2SoldierCells[index]);
        }
        // Population is authored independently from the number of heroes granted
        // at run start; production keeps the legacy hero-plus-six deployment capacity.
        run.CurrentPopulation = _rules.InitialPopulation;
        return _persistence.SaveActiveRun(run) ? run : null;
    }

    public IReadOnlyList<CatalogEntry> PickEntries(
        ActiveRunDto run,
        CompiledContentPool source,
        int count,
        int salt)
    {
        var random = new DeterministicRandom(
            run.Seed ^ (ulong)(run.FloorIndex + 1 + salt) * 0x94D049BB133111EBUL);
        return source.ContentIds.Select(Required)
            .OrderBy(_ => random.NextInt(0, int.MaxValue))
            .Take(count)
            .ToArray();
    }

    public bool Recruit(ActiveRunDto? run, string rosterHeroId)
    {
        if (run is null || !_persistence.ValidateRun(run) ||
            !_content.TryGet(rosterHeroId, out var entry) ||
            entry.Definition is not UnitDefinition { IsEnemy: false })
            return false;
        var working = _persistence.CloneRun(run);
        if (working.Roster.Count >= working.CurrentPopulation + _rules.ReserveCapacity) return false;
        if (AddRosterHero(working, rosterHeroId) is null) return false;
        return _persistence.ValidateRun(working) && _persistence.TryPublish(working, run);
    }

    public bool GrantPopulation(ActiveRunDto? run, int amount)
    {
        if (run is null || amount <= 0 || !_persistence.ValidateRun(run)) return false;
        var working = _persistence.CloneRun(run);
        var facts = RunPopulationPolicy.Evaluate(working, _rules);
        if (working.CurrentPopulation > facts.EffectivePopulationCap - amount) return false;
        working.CurrentPopulation += amount;
        return _persistence.ValidateRun(working) && _persistence.TryPublish(working, run);
    }

    public bool GrantPopulationFromSource(
        ActiveRunDto? run,
        string sourceId,
        int populationAmount,
        int effectiveCapIncrease)
    {
        if (run is null || !_persistence.ValidateRun(run) || string.IsNullOrWhiteSpace(sourceId) ||
            populationAmount < 0 ||
            effectiveCapIncrease <= 0 ||
            effectiveCapIncrease > _rules.PhysicalDeploymentCeiling - _rules.OrdinaryPopulationCap ||
            run.PopulationCapSources.Any(source => source.SourceId == sourceId))
            return false;
        var working = _persistence.CloneRun(run);
        working.PopulationCapSources.Add(new PopulationCapSourceDto
        {
            SourceId = sourceId,
            Amount = effectiveCapIncrease
        });
        var facts = RunPopulationPolicy.Evaluate(working, _rules);
        if (working.CurrentPopulation > facts.EffectivePopulationCap - populationAmount) return false;
        working.CurrentPopulation += populationAmount;
        return _persistence.ValidateRun(working) && _persistence.TryPublish(working, run);
    }

    public int ConvertRecruitToGold(ActiveRunDto? run)
    {
        if (run is null || run.Roster.Count == 0 || !_content.TryGet(run.Roster[0].ContentId, out var heroEntry))
            return 0;
        var root = heroEntry.Scene.Instantiate<UnitContentRoot>();
        int amount;
        try { amount = root.HeroRule?.RecruitConversionGold ?? 0; }
        finally { root.Free(); }
        if (amount <= 0) return 0;
        run.Gold += amount;
        _persistence.SaveActiveRun(run);
        return amount;
    }

    public bool BuyItem(ActiveRunDto? run, string itemId)
    {
        if (run is null || !_content.TryGet(itemId, out var entry) ||
            entry.Definition is not ItemDefinition { ProductKind: ItemProductKind.Relic } definition ||
            run.Gold < definition.Price)
            return false;
        if (!TryNextInstanceSequence(run, out var sequence)) return false;
        run.Gold -= definition.Price;
        AddItem(run, itemId, sequence);
        return _persistence.SaveActiveRun(run);
    }

    public bool GrantItem(ActiveRunDto? run, string itemId)
    {
        if (run is null || !_content.TryGet(itemId, out var entry) ||
            entry.Definition is not ItemDefinition { ProductKind: ItemProductKind.Relic })
            return false;
        if (!TryNextInstanceSequence(run, out var sequence)) return false;
        AddItem(run, itemId, sequence);
        return _persistence.SaveActiveRun(run);
    }

    public void Rest(ActiveRunDto? run, bool takeGold)
    {
        if (run is null) return;
        if (takeGold) run.Gold += _rules.RestGold;
        else
        {
            foreach (var unit in run.Roster)
                unit.HealthRatio = Math.Min(1, unit.HealthRatio + LegacyRecovery(
                    unit,
                    _rules.RestHeroHealing,
                    _rules.RestSoldierHealing));
        }
        _persistence.SaveActiveRun(run);
    }

    public void ResolveEvent(ActiveRunDto? run, bool risky)
    {
        if (run is null) return;
        var random = new DeterministicRandom(run.Seed ^ (ulong)(run.FloorIndex + 23));
        if (risky && random.NextFloat() > 1f - _rules.RiskyEventSuccessChance)
            run.Gold += _rules.RiskyEventSuccessGold;
        else if (risky)
            foreach (var unit in run.Roster)
                unit.HealthRatio = Math.Max(
                    _rules.RiskyEventMinimumHealth,
                    unit.HealthRatio - _rules.RiskyEventHealthLoss);
        else
            run.Gold += _rules.SafeEventGold;
        _persistence.SaveActiveRun(run);
    }

    public void ApplyBattleVictory(
        ActiveRunDto run,
        BattleResult result,
        EncounterPlan encounter,
        int relicGoldDelta)
    {
        foreach (var state in result.Units.Where(unit => unit.Team == 0 && !unit.IsTemporary))
        {
            var ratio = state.FinalHealth / state.MaxHealth;
            var instance = run.Roster.FirstOrDefault(unit => unit.InstanceId == state.SourceInstanceId);
            if (instance is null) continue;
            var legacyHero = IsLegacyHeroContent(instance.ContentId);
            instance.HealthRatio = state.Alive
                ? Math.Max(
                    legacyHero ? _rules.MinimumVictoryHeroHealth : _rules.MinimumLivingSoldierHealth,
                    ratio)
                : _rules.DefeatedSoldierHealth;
            if (!state.Alive)
                for (var slot = 0; slot < run.Deployment.Count; slot++)
                    if (run.Deployment[slot] == instance.InstanceId)
                        run.Deployment[slot] = string.Empty;
        }

        // A victory includes a short regroup. Casualties remain wounded and leave deployment.
        foreach (var unit in run.Roster)
            unit.HealthRatio = Math.Min(1f, unit.HealthRatio + LegacyRecovery(
                unit,
                _rules.VictoryHeroRecovery,
                _rules.VictorySoldierRecovery));
        run.BattleNumber++;
        run.Gold = Math.Max(0, run.Gold - result.GoldSpent);
        run.Gold += encounter.IsBoss
            ? _rules.BossBattleGold
            : encounter.IsElite ? _rules.EliteBattleGold : _rules.NormalBattleGold;
        if (run.Roster.Count > 0 && _content.TryGet(run.Roster[0].ContentId, out var heroEntry))
        {
            var hero = heroEntry.Scene.Instantiate<UnitContentRoot>();
            try { run.Gold += hero.HeroRule?.BattleGoldBonus ?? 0; }
            finally { hero.Free(); }
        }
        run.Gold += relicGoldDelta;
    }

    private RosterHeroInstanceDto? AddRosterHero(
        ActiveRunDto run,
        string contentId,
        string? fixedInstanceId = null)
    {
        var sequence = 0;
        if (fixedInstanceId is null && !TryNextInstanceSequence(run, out sequence)) return null;
        var instance = new RosterHeroInstanceDto
        {
            InstanceId = fixedInstanceId ?? $"roster-hero-{sequence}",
            ContentId = contentId
        };
        run.Roster.Add(instance);
        return instance;
    }

    private void AddItem(ActiveRunDto run, string itemId, int sequence)
    {
        var definition = _content.Graph.ResolveRelic(itemId);
        run.Items.Add(new ItemInstanceDto
        {
            InstanceId = $"item-{sequence}",
            ContentId = itemId,
            Stacks = 1,
            Counters = RelicRunScope.InitialRunCounters(definition)
                .Select(counter => new RelicCounterStateDto
                {
                    CounterId = counter.CounterId,
                    Value = counter.Value
                }).ToList()
        });
    }

    private CatalogEntry Required(string id) => _content.TryGet(id, out var entry)
        ? entry
        : throw new InvalidOperationException($"Missing content: {id}");

    private static bool TryNextInstanceSequence(ActiveRunDto run, out int sequence)
    {
        sequence = 0;
        if (run.Roster is null || run.Items is null || run.Roster.Any(hero => hero?.Equipment is null) ||
            run.Items.Any(item => item is null) || run.Roster.SelectMany(hero => hero.Equipment).Any(item => item is null))
            return false;
        var maximum = run.Roster.Select(hero => hero.InstanceId)
            .Concat(run.Items.Select(item => item.InstanceId))
            .Concat(run.Roster.SelectMany(hero => hero.Equipment).Select(item => item.InstanceId))
            .Select(ParseInstanceSuffix)
            .DefaultIfEmpty(0)
            .Max();
        if (maximum == int.MaxValue) return false;
        sequence = maximum + 1;
        return true;
    }

    private static int ParseInstanceSuffix(string? instanceId)
    {
        if (string.IsNullOrEmpty(instanceId)) return 0;
        var separator = instanceId.LastIndexOf('-');
        return separator >= 0 && int.TryParse(instanceId[(separator + 1)..], out var value) ? value : 0;
    }

    private static void Place(ActiveRunDto run, string instanceId, Godot.Vector2I cell)
    {
        var slot = BattlefieldLayout.PlayerDeploymentSlot(cell);
        if (slot < 0 || !string.IsNullOrEmpty(run.Deployment[slot]))
            throw new InvalidOperationException($"Cannot place roster hero at {cell}.");
        run.Deployment[slot] = instanceId;
    }

    private float LegacyRecovery(RosterHeroInstanceDto unit, float heroValue, float rosterValue) =>
        IsLegacyHeroContent(unit.ContentId) ? heroValue : rosterValue;

    private bool IsLegacyHeroContent(string contentId) =>
        _content.TryGet(contentId, out var entry) && entry.Definition is UnitDefinition { IsHero: true };
}
