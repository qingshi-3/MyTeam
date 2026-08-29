using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Components;
using TowerAutobattler.Content;

namespace TowerAutobattler.Run;

public sealed class RunApplication
{
    public const int DeploymentCapacity = 6;
    public const int ReserveCapacity = 3;

    private readonly ContentRegistry _content;
    private readonly IRunSaveService _save;
    private readonly TowerGenerator _tower;
    private int _instanceCounter;

    public MetaProgressDto Meta { get; }
    public SettingsDto Settings { get; }
    public ActiveRunDto? ActiveRun { get; private set; }
    public ContentRegistry Content => _content;
    public TowerGenerator Tower => _tower;

    public RunApplication(ContentRegistry content, IRunSaveService save, IReadOnlyList<TowerRegionDefinition> regions)
    {
        _content = content;
        _save = save;
        _tower = new TowerGenerator(regions);
        Meta = save.LoadMeta();
        Settings = save.LoadSettings();
        ApplyMasterVolume();
        EnsureMetaDefaults();
        var loaded = save.LoadActiveRun();
        ActiveRun = loaded is not null && ValidateRun(loaded) ? loaded : null;
        _instanceCounter = ActiveRun is null ? 0 : ActiveRun.Roster.Select(unit => unit.InstanceId)
            .Concat(ActiveRun.Items.Select(item => item.InstanceId)).Select(ParseInstanceSuffix).DefaultIfEmpty(0).Max();
    }

    public bool StartNewRun(string heroId, ulong seed)
    {
        if (!Meta.UnlockedHeroIds.Contains(heroId) || !_content.TryGet(heroId, out var heroEntry) || heroEntry.Definition is not UnitDefinition { IsHero: true })
            return false;
        var run = new ActiveRunDto { Seed = seed == 0 ? 1UL : seed, HeroId = heroId };
        var random = new DeterministicRandom(run.Seed);
        var starters = _content.Catalog.Soldiers.OrderBy(_ => random.NextInt(0, int.MaxValue)).Take(3).ToArray();
        foreach (var entry in starters)
        {
            var instance = AddRosterUnit(run, entry.StableId);
            var open = run.Deployment.FindIndex(string.IsNullOrEmpty);
            if (open >= 0) run.Deployment[open] = instance.InstanceId;
        }
        ActiveRun = run;
        _save.SaveActiveRun(run);
        return true;
    }

    public void AbandonRun()
    {
        ActiveRun = null;
        _save.DeleteActiveRun();
    }

    public IReadOnlyList<TowerNodeOption> CurrentOptions() => ActiveRun is null ? [] : _tower.Options(ActiveRun);

    public EncounterPlan CurrentEncounter() => ActiveRun is null
        ? throw new InvalidOperationException("No active run")
        : _tower.Encounter(ActiveRun, ActiveRun.SelectedNode);

    public bool SelectNode(TowerNodeType type)
    {
        if (ActiveRun is null || ActiveRun.PendingNode) return false;
        if (!_tower.Options(ActiveRun).Any(option => option.Type == type)) return false;
        ActiveRun.SelectedNode = type;
        ActiveRun.PendingNode = true;
        return _save.SaveActiveRun(ActiveRun);
    }

    public void FinishNonCombatNode()
    {
        if (ActiveRun is null) return;
        AdvanceFloor();
    }

    public IReadOnlyList<CatalogEntry> RecruitmentChoices(int salt = 0) => PickEntries(_content.Catalog.Soldiers, 3, salt);
    public IReadOnlyList<CatalogEntry> ItemChoices(int salt = 0) => PickEntries(_content.Catalog.Items, 3, salt);

    public bool Recruit(string soldierId)
    {
        if (ActiveRun is null || ActiveRun.Roster.Count >= 9 || !_content.TryGet(soldierId, out var entry) || entry.Definition is not UnitDefinition { IsHero: false, IsEnemy: false })
            return false;
        AddRosterUnit(ActiveRun, soldierId);
        return _save.SaveActiveRun(ActiveRun);
    }

    public int ConvertRecruitToGold()
    {
        if (ActiveRun is null || !_content.TryGet(ActiveRun.HeroId, out var heroEntry)) return 0;
        var root = heroEntry.Scene.Instantiate<UnitContentRoot>();
        int amount;
        try { amount = root.HeroRule?.RecruitConversionGold ?? 0; }
        finally { root.Free(); }
        if (amount <= 0) return 0;
        ActiveRun.Gold += amount;
        _save.SaveActiveRun(ActiveRun);
        return amount;
    }

    public bool BuyItem(string itemId)
    {
        if (ActiveRun is null || !_content.TryGet(itemId, out var entry) || entry.Definition is not ItemDefinition definition || ActiveRun.Gold < definition.Price)
            return false;
        ActiveRun.Gold -= definition.Price;
        ActiveRun.Items.Add(new ItemInstanceDto
        {
            InstanceId = $"item-{++_instanceCounter}",
            ContentId = itemId,
            Stacks = 1
        });
        return _save.SaveActiveRun(ActiveRun);
    }

    public bool GrantItem(string itemId)
    {
        if (ActiveRun is null || !_content.TryGet(itemId, out var entry) || entry.Definition is not ItemDefinition)
            return false;
        ActiveRun.Items.Add(new ItemInstanceDto
        {
            InstanceId = $"item-{++_instanceCounter}",
            ContentId = itemId,
            Stacks = 1
        });
        return _save.SaveActiveRun(ActiveRun);
    }

    public bool EquipDeployment(string instanceId, int slot)
        => MoveDeploymentUnit(instanceId, slot);

    public bool MoveDeploymentUnit(string instanceId, int slot)
    {
        if (ActiveRun is null || slot < 0 || slot >= DeploymentCapacity || ActiveRun.Roster.All(unit => unit.InstanceId != instanceId)) return false;
        var sourceSlot = ActiveRun.Deployment.IndexOf(instanceId);
        if (sourceSlot == slot) return false;
        var displaced = ActiveRun.Deployment[slot];
        ActiveRun.Deployment[slot] = instanceId;
        if (sourceSlot >= 0) ActiveRun.Deployment[sourceSlot] = displaced;
        if (_save.SaveActiveRun(ActiveRun)) return true;
        ActiveRun.Deployment[slot] = displaced;
        if (sourceSlot >= 0) ActiveRun.Deployment[sourceSlot] = instanceId;
        return false;
    }

    public bool WithdrawDeploymentUnit(string instanceId)
    {
        if (ActiveRun is null) return false;
        var slot = ActiveRun.Deployment.IndexOf(instanceId);
        if (slot < 0 || ReserveCount(ActiveRun) >= ReserveCapacity) return false;
        ActiveRun.Deployment[slot] = string.Empty;
        if (_save.SaveActiveRun(ActiveRun)) return true;
        ActiveRun.Deployment[slot] = instanceId;
        return false;
    }

    public void ClearDeploymentSlot(int slot)
    {
        if (ActiveRun is null || slot < 0 || slot >= DeploymentCapacity || string.IsNullOrEmpty(ActiveRun.Deployment[slot])) return;
        ActiveRun.Deployment[slot] = string.Empty;
        _save.SaveActiveRun(ActiveRun);
    }

    public void Rest(bool takeGold)
    {
        if (ActiveRun is null) return;
        if (takeGold) ActiveRun.Gold += 8;
        else
        {
            ActiveRun.HeroHealthRatio = Math.Min(1, ActiveRun.HeroHealthRatio + .35f);
            foreach (var unit in ActiveRun.Roster) unit.HealthRatio = Math.Min(1, unit.HealthRatio + .45f);
        }
        _save.SaveActiveRun(ActiveRun);
    }

    public void ResolveEvent(bool risky)
    {
        if (ActiveRun is null) return;
        var random = new DeterministicRandom(ActiveRun.Seed ^ (ulong)(ActiveRun.FloorIndex + 23));
        if (risky && random.NextFloat() > .35f) ActiveRun.Gold += 18;
        else if (risky) ActiveRun.HeroHealthRatio = Math.Max(.25f, ActiveRun.HeroHealthRatio - .25f);
        else ActiveRun.Gold += 6;
        _save.SaveActiveRun(ActiveRun);
    }

    public BattleConfig BuildBattleConfig(EncounterPlan encounter)
    {
        var run = ActiveRun ?? throw new InvalidOperationException("No active run");
        var spawns = new List<BattleSpawn>();
        var heroEntry = Required(run.HeroId);
        var heroRoot = heroEntry.Scene.Instantiate<UnitContentRoot>();
        FloorRuleContentRoot? rule = null;
        try
        {
            var heroDefinition = (UnitDefinition)heroEntry.Definition;
            spawns.Add(new BattleSpawn(
                BattleSetupFactory.Snapshot(heroDefinition, heroRoot.Behavior), 0, BattlefieldLayout.HeroCell, "player-hero",
                run.HeroHealthRatio, BehaviorSummon: SnapshotOptional(heroRoot.Behavior?.SummonContentId ?? string.Empty)));
            for (var index = 0; index < run.Deployment.Count; index++)
            {
                var instanceId = run.Deployment[index];
                var instance = run.Roster.FirstOrDefault(unit => unit.InstanceId == instanceId);
                if (instance is null) continue;
                var entry = Required(instance.ContentId);
                var unitRoot = entry.Scene.Instantiate<UnitContentRoot>();
                try
                {
                    spawns.Add(new BattleSpawn(
                        BattleSetupFactory.Snapshot((UnitDefinition)entry.Definition, unitRoot.Behavior), 0, BattlefieldLayout.SoldierCells[index], instance.InstanceId,
                        instance.HealthRatio, BehaviorSummon: SnapshotOptional(unitRoot.Behavior?.SummonContentId ?? string.Empty)));
                }
                finally { unitRoot.Free(); }
            }
            for (var index = 0; index < encounter.EnemyIds.Count; index++)
            {
                var entry = Required(encounter.EnemyIds[index]);
                var enemyRoot = entry.Scene.Instantiate<UnitContentRoot>();
                try
                {
                    spawns.Add(new BattleSpawn(
                        BattleSetupFactory.Snapshot((UnitDefinition)entry.Definition, enemyRoot.Behavior), 1, BattlefieldLayout.EnemyCells[index % BattlefieldLayout.EnemyCells.Length], $"enemy-{index}",
                        BehaviorSummon: SnapshotOptional(enemyRoot.Behavior?.SummonContentId ?? string.Empty)));
                }
                finally { enemyRoot.Free(); }
            }
            var itemBindings = run.Items.Select(item => new BattleSetupFactory.ItemBindingSpec(
                Required(item.ContentId),
                new ItemInstanceState
                {
                    InstanceId = item.InstanceId,
                    Stacks = item.Stacks,
                    Charges = item.Charges,
                    Roll = item.Roll
                })).ToArray();
            var modifiers = BattleSetupFactory.AggregateItems(itemBindings);
            var ruleScene = _content.Catalog.FloorRules.First(scene =>
            {
                var node = scene.Instantiate<FloorRuleContentRoot>();
                try { return node.Id == encounter.FloorRuleId; }
                finally { node.Free(); }
            });
            rule = ruleScene.Instantiate<FloorRuleContentRoot>();
            var heroRule = heroRoot.HeroRule ?? throw new InvalidOperationException($"Hero {run.HeroId} has no rule component.");
            var heroCommand = heroRoot.HeroCommand ?? throw new InvalidOperationException($"Hero {run.HeroId} has no command component.");
            var heroRuleSnapshot = BattleSetupFactory.Snapshot(heroRule, heroCommand);
            var heroSummon = SnapshotOptional(heroRuleSnapshot.SummonContentId);
            var itemSummon = SnapshotOptional(modifiers.SummonContentId);
            return new BattleConfig
            {
                Seed = run.Seed ^ (ulong)(run.BattleNumber + 1) * 0xD1B54A32D192ED03UL,
                FloorRule = rule.CreateRuntime(),
                Spawns = spawns,
                HeroRule = heroRuleSnapshot,
                Modifiers = modifiers,
                Summons = new SummonProfiles(heroSummon, heroSummon, heroSummon, itemSummon),
                EmptyDeploymentSlots = run.Deployment.Count(string.IsNullOrEmpty),
                StartingGold = run.Gold
            };
        }
        finally
        {
            rule?.Free();
            heroRoot.Free();
        }
    }

    public bool CompleteBattle(BattleResult result, EncounterPlan encounter)
    {
        var run = ActiveRun ?? throw new InvalidOperationException("No active run");
        if (result.Outcome != BattleOutcome.PlayerVictory)
        {
            ActiveRun = null;
            _save.DeleteActiveRun();
            return false;
        }
        foreach (var state in result.Units.Where(unit => unit.Team == 0 && !unit.IsTemporary))
        {
            var ratio = state.FinalHealth / state.MaxHealth;
            if (state.IsHero) run.HeroHealthRatio = Math.Max(.15f, ratio);
            else
            {
                var instance = run.Roster.FirstOrDefault(unit => unit.InstanceId == state.SourceInstanceId);
                if (instance is null) continue;
                instance.HealthRatio = state.Alive ? Math.Max(.1f, ratio) : .25f;
                if (!state.Alive)
                    for (var slot = 0; slot < run.Deployment.Count; slot++)
                        if (run.Deployment[slot] == instance.InstanceId) run.Deployment[slot] = string.Empty;
            }
        }
        // A victory includes a short regroup. Casualties still lose deployment and return wounded,
        // but ordinary chip damage does not make one early fight invalidate the whole run.
        run.HeroHealthRatio = Math.Min(1f, run.HeroHealthRatio + .12f);
        foreach (var unit in run.Roster) unit.HealthRatio = Math.Min(1f, unit.HealthRatio + .15f);
        run.BattleNumber++;
        run.Gold = Math.Max(0, run.Gold - result.GoldSpent);
        run.Gold += encounter.IsBoss ? 18 : encounter.IsElite ? 12 : 7;
        if (_content.TryGet(run.HeroId, out var heroEntry))
        {
            var hero = heroEntry.Scene.Instantiate<UnitContentRoot>();
            try { run.Gold += hero.HeroRule?.BattleGoldBonus ?? 0; }
            finally { hero.Free(); }
        }
        var itemBindings = run.Items.Select(item => new BattleSetupFactory.ItemBindingSpec(
            Required(item.ContentId), new ItemInstanceState { InstanceId = item.InstanceId, Stacks = item.Stacks, Charges = item.Charges, Roll = item.Roll }));
        run.Gold += BattleSetupFactory.AggregateItems(itemBindings).GoldPerBattle;

        var finalVictory = run.FloorIndex == 14 && encounter.IsBoss;
        if (finalVictory)
        {
            Meta.Victories++;
            Meta.HighestRegion = 3;
            UnlockNextHero();
            _save.SaveMeta(Meta);
            ActiveRun = null;
            _save.DeleteActiveRun();
            return true;
        }
        AdvanceFloor();
        return true;
    }

    public void SaveSettings()
    {
        _save.SaveSettings(Settings);
        ApplyMasterVolume();
    }

    private void ApplyMasterVolume()
    {
        var master = AudioServer.GetBusIndex("Master");
        if (master >= 0) AudioServer.SetBusVolumeDb(master, Mathf.LinearToDb(Mathf.Max(.001f, Settings.MasterVolume)));
    }

    private void AdvanceFloor()
    {
        if (ActiveRun is null) return;
        ActiveRun.FloorIndex++;
        ActiveRun.PendingNode = false;
        Meta.HighestRegion = Math.Max(Meta.HighestRegion, Math.Min(3, ActiveRun.FloorIndex / 5 + 1));
        _save.SaveMeta(Meta);
        _save.SaveActiveRun(ActiveRun);
    }

    private UnitInstanceDto AddRosterUnit(ActiveRunDto run, string contentId)
    {
        var instance = new UnitInstanceDto { InstanceId = $"unit-{++_instanceCounter}", ContentId = contentId };
        run.Roster.Add(instance);
        return instance;
    }

    private IReadOnlyList<CatalogEntry> PickEntries(Godot.Collections.Array<CatalogEntry> source, int count, int salt)
    {
        var run = ActiveRun ?? throw new InvalidOperationException("No active run");
        var random = new DeterministicRandom(run.Seed ^ (ulong)(run.FloorIndex + 1 + salt) * 0x94D049BB133111EBUL);
        return source.OrderBy(_ => random.NextInt(0, int.MaxValue)).Take(count).ToArray();
    }

    private CatalogEntry Required(string id) => _content.TryGet(id, out var entry) ? entry : throw new InvalidOperationException($"Missing content: {id}");

    private bool ValidateRun(ActiveRunDto run)
    {
        if (run.Version != 2 || run.FloorIndex is < 0 or > 14 || !_content.TryGet(run.HeroId, out var hero) || hero.Definition is not UnitDefinition { IsHero: true }) return false;
        if (run.Roster.Count > DeploymentCapacity + ReserveCapacity || run.Deployment.Count != DeploymentCapacity) return false;
        var instanceIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var unit in run.Roster)
            if (string.IsNullOrWhiteSpace(unit.InstanceId) || !instanceIds.Add(unit.InstanceId) || !_content.TryGet(unit.ContentId, out var entry) || entry.Definition is not UnitDefinition { IsHero: false, IsEnemy: false }) return false;
        if (run.Deployment.Any(id => !string.IsNullOrEmpty(id) && !instanceIds.Contains(id))) return false;
        var deployedIds = run.Deployment.Where(id => !string.IsNullOrEmpty(id)).ToArray();
        if (deployedIds.Distinct(StringComparer.Ordinal).Count() != deployedIds.Length) return false;
        var itemInstanceIds = new HashSet<string>(StringComparer.Ordinal);
        return run.Items.All(item => item.Stacks > 0 && !string.IsNullOrWhiteSpace(item.InstanceId) && itemInstanceIds.Add(item.InstanceId) &&
            _content.TryGet(item.ContentId, out var entry) && entry.Definition is ItemDefinition);
    }

    private UnitSnapshot? SnapshotOptional(string contentId)
    {
        if (string.IsNullOrWhiteSpace(contentId) || !_content.TryGet(contentId, out var entry) || entry.Definition is not UnitDefinition definition)
            return null;
        var root = entry.Scene.Instantiate<UnitContentRoot>();
        try { return BattleSetupFactory.Snapshot(definition, root.Behavior); }
        finally { root.Free(); }
    }

    private static int ParseInstanceSuffix(string instanceId)
    {
        var separator = instanceId.LastIndexOf('-');
        return separator >= 0 && int.TryParse(instanceId[(separator + 1)..], out var value) ? value : 0;
    }

    private static int ReserveCount(ActiveRunDto run) =>
        run.Roster.Count - run.Deployment.Count(id => !string.IsNullOrEmpty(id));

    private void EnsureMetaDefaults()
    {
        Meta.UnlockedHeroIds.RemoveAll(id => !_content.TryGet(id, out var entry) || entry.Definition is not UnitDefinition { IsHero: true });
        if (Meta.UnlockedHeroIds.Count == 0)
            Meta.UnlockedHeroIds.AddRange(_content.Catalog.Heroes.Take(3).Select(entry => entry.StableId));
        _save.SaveMeta(Meta);
    }

    private void UnlockNextHero()
    {
        var locked = _content.Catalog.Heroes.FirstOrDefault(entry => !Meta.UnlockedHeroIds.Contains(entry.StableId));
        if (locked is not null) Meta.UnlockedHeroIds.Add(locked.StableId);
    }
}
