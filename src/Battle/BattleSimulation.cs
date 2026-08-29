using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Godot;
using TowerAutobattler.Components;

namespace TowerAutobattler.Battle;

public sealed class BattleSimulation : IDisposable
{
    public const int Width = BattlefieldLayout.Width;
    public const int Height = BattlefieldLayout.Height;
    public const float TickSeconds = 0.1f;
    public const int MaxTicks = 1800;

    private readonly BattleConfig _config;
    private readonly DeterministicRandom _random;
    private readonly List<BattleUnitState> _units = [];
    private readonly List<BattleEvent> _events = [];
    private readonly StringBuilder _digest = new();
    private readonly HashSet<string> _deathProcUnits = new(StringComparer.Ordinal);
    private readonly Dictionary<string, BattleUnitStatistics> _statistics = new(StringComparer.Ordinal);
    private IGridMovementService? _movement;
    private int _summonCounter;
    private bool _floorRuleEnded;

    public int TickIndex { get; private set; }
    public BattleOutcome Outcome { get; private set; } = BattleOutcome.Running;
    public int MaxMana => _config.HeroRule.MaxMana;
    public int CurrentMana { get; private set; }
    public int CommandManaCost => _config.HeroRule.CommandManaCost;
    public int CommandCharges => CommandManaCost <= 0 ? 0 : CurrentMana / CommandManaCost;
    public int GoldSpent { get; private set; }
    public int SuccessfulHeroCommandUses { get; private set; }
    public string CommandName => _config.HeroRule.CommandName;
    public string CommandDescription => _config.HeroRule.CommandDescription;
    public int CommandGoldCost => _config.HeroRule.CommandGoldCost;
    public int RemainingGold => _config.StartingGold - GoldSpent;
    public IReadOnlyList<BattleUnitState> Units => _units;
    public IReadOnlyList<BattleEvent> PendingEvents => _events;

    public BattleSimulation(BattleConfig config)
    {
        _config = config;
        _random = new DeterministicRandom(config.Seed);
        CurrentMana = Math.Max(0, config.HeroRule.MaxMana);
        var index = 0;
        foreach (var spawn in config.Spawns)
        {
            var unit = spawn.Unit;
            var taggedForHero = !string.IsNullOrWhiteSpace(config.HeroRule.RequiredSoldierTag) && unit.Tags.Contains(config.HeroRule.RequiredSoldierTag);
            var healthMultiplier = spawn.Team == 0
                ? (unit.IsHero ? config.Modifiers.HeroHealthMultiplier : config.Modifiers.ArmyHealthMultiplier * config.HeroRule.SoldierHealthMultiplier)
                : 1f;
            if (spawn.Team == 0 && !unit.IsHero && taggedForHero) healthMultiplier *= config.HeroRule.TaggedSoldierHealthMultiplier;
            var damageMultiplier = spawn.Team == 0
                ? (unit.IsHero
                    ? config.Modifiers.HeroDamageMultiplier * config.HeroRule.HeroDamageMultiplier *
                      (1f + config.EmptyDeploymentSlots * (config.HeroRule.EmptySlotHeroBonus + config.Modifiers.EmptySlotPower / 100f))
                    : config.Modifiers.ArmyDamageMultiplier * config.HeroRule.SoldierDamageMultiplier)
                : 1f;
            if (spawn.Team == 0 && !unit.IsHero && taggedForHero) damageMultiplier *= config.HeroRule.TaggedSoldierDamageMultiplier;
            var requestedCell = ClampCell(spawn.Cell);
            var resolvedCell = CanOccupy(requestedCell) ? requestedCell : FindOpenNear(requestedCell, spawn.Team);
            var maxHealth = unit.MaxHealth * healthMultiplier;
            var state = new BattleUnitState
            {
                RuntimeId = string.IsNullOrWhiteSpace(spawn.InstanceId) ? $"{(spawn.Team == 0 ? "p" : "e")}-{index}" : spawn.InstanceId,
                SourceInstanceId = spawn.InstanceId,
                Definition = unit,
                Team = spawn.Team,
                Cell = resolvedCell,
                MaxHealth = maxHealth,
                Health = maxHealth * Mathf.Clamp(spawn.HealthRatio, .05f, 1f),
                Damage = unit.Damage * damageMultiplier,
                LifeSteal = Mathf.Clamp(unit.LifeSteal + (spawn.Team == 0
                    ? (unit.IsHero ? config.Modifiers.HeroLifeStealBonus + config.HeroRule.HeroLifeStealBonus : config.Modifiers.ArmyLifeStealBonus)
                    : 0), 0, .8f),
                Shield = spawn.Team == 0 ? config.Modifiers.StartShield +
                    (unit.IsHero ? maxHealth * config.EmptyDeploymentSlots * config.HeroRule.EmptySlotStartShield : 0) : 0,
                IsTemporary = spawn.IsTemporary,
                BehaviorSummon = spawn.BehaviorSummon
            };
            _units.Add(state);
            _statistics.Add(state.RuntimeId, new BattleUnitStatistics { JoinTick = 0 });
            index++;
        }
        AddConfiguredSummons();
        try
        {
            _config.FloorRule.OnBattleStarted(CreateRuleContext());
        }
        catch
        {
            try { EndFloorRule(BattleOutcome.Timeout); }
            catch { }
            throw;
        }
        _movement = new DeterministicGridMovementService(
            Width, Height, () => _units, cell => _config.FloorRule.CanOccupy(cell), HasLineAccess, config.Seed);
        Emit("battle_started", "", "", 0, new Vector2I(Width / 2, Height / 2), "idle");
    }

    public IReadOnlyList<BattleEvent> DrainEvents()
    {
        var copy = _events.ToArray();
        _events.Clear();
        return copy;
    }

    public BattleOutcome Step()
    {
        if (Outcome != BattleOutcome.Running) return Outcome;
        TickIndex++;
        try
        {
            _movement!.BeginTick();
            ApplyFloorRule();
            foreach (var unit in _units.Where(unit => unit.Alive).OrderBy(unit => unit.RuntimeId, StringComparer.Ordinal).ToArray())
                Act(unit);
            _movement.ResolveIntents((unit, cell) => Emit("move", unit.RuntimeId, "", 0, cell, "move"));
            ResolveOutcome();
        }
        catch
        {
            try { Abort(); }
            catch { }
            throw;
        }
        return Outcome;
    }

    public BattleResult RunToEnd()
    {
        while (Outcome == BattleOutcome.Running) Step();
        return CreateResult();
    }

    public void Abort()
    {
        if (Outcome == BattleOutcome.Running) Outcome = BattleOutcome.Timeout;
        EndFloorRule(Outcome);
    }

    public void Dispose()
    {
        Abort();
        foreach (var unit in _units) ClearActionTarget(unit);
        _events.Clear();
        _movement?.Dispose();
        _movement = null;
    }

    public bool UseHeroCommand() => TryUseHeroCommand().Succeeded;

    public HeroCommandUseResult TryUseHeroCommand()
    {
        if (Outcome != BattleOutcome.Running) return new HeroCommandUseResult(false, "战斗已经结束。");
        if (CommandManaCost <= 0) return new HeroCommandUseResult(false, "指令法力消耗配置无效。");
        if (CurrentMana < CommandManaCost) return new HeroCommandUseResult(false, "法力不足。");
        var hero = _units.FirstOrDefault(unit => unit.Team == 0 && unit.Definition.IsHero && unit.Alive);
        if (hero is null) return new HeroCommandUseResult(false, "英雄已无法发动指令。");
        var pendingGold = 0;
        var pendingSpawns = new List<PendingCommandSpawn>();
        var pendingCues = new List<string>();
        var reservedCells = new HashSet<Vector2I>();
        var context = new BattleCommandContext(
            hero,
            Allies(0).ToArray(),
            Allies(1).ToArray(),
            _config.Summons,
            (source, target, amount) => HealLiving(source.RuntimeId, target, amount),
            (profile, near, healthScale, damageScale) =>
            {
                if (profile is null || !TryFindOpenNear(near, 0, reservedCells, out var cell)) return false;
                reservedCells.Add(cell);
                pendingSpawns.Add(new PendingCommandSpawn(profile, cell, healthScale, damageScale));
                return true;
            },
            amount =>
            {
                if (amount < 0 || RemainingGold - pendingGold < amount) return false;
                pendingGold += amount;
                return true;
            },
            cue => pendingCues.Add(cue));
        if (!_config.HeroRule.Command.TryExecute(context))
            return new HeroCommandUseResult(false, string.IsNullOrWhiteSpace(context.FailureReason) ? "当前没有合法的指令目标。" : context.FailureReason);
        foreach (var spawn in pendingSpawns)
            if (!SpawnTemporary(spawn.Profile, 0, spawn.Cell, spawn.HealthScale, spawn.DamageScale))
                throw new InvalidOperationException("A validated command spawn failed during transaction commit.");
        GoldSpent += pendingGold;
        CurrentMana -= CommandManaCost;
        SuccessfulHeroCommandUses++;
        foreach (var cue in pendingCues) Emit("hero_command", hero.RuntimeId, "", 0, hero.Cell, cue);
        Emit("hero_command", hero.RuntimeId, "", CurrentMana, hero.Cell, "skill_cast");
        return HeroCommandUseResult.Success;
    }


    public BattleResult CreateResult()
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(_digest.ToString()))).ToLowerInvariant();
        var units = _units.Select(unit =>
        {
            var statistics = _statistics[unit.RuntimeId];
            return new BattleUnitReportSnapshot(
                unit.RuntimeId,
                unit.SourceInstanceId,
                unit.Definition.ContentId,
                unit.Definition.DisplayName,
                unit.Definition.Role,
                unit.Team,
                unit.Definition.IsHero,
                unit.IsTemporary,
                unit.Alive,
                unit.Cell,
                unit.Health,
                unit.MaxHealth,
                unit.Shield,
                unit.Damage,
                statistics.DamageDealt,
                statistics.DamageTaken,
                statistics.ShieldAbsorbed,
                statistics.HealingDone,
                statistics.Kills,
                statistics.JoinTick,
                statistics.DefeatTick,
                statistics.AttackActions,
                statistics.EffectiveHealingEvents);
        }).ToImmutableArray();
        return new BattleResult(Outcome, TickIndex, hash, units, GoldSpent, SuccessfulHeroCommandUses);
    }

    private void AddConfiguredSummons()
    {
        var hero = _units.FirstOrDefault(unit => unit.Team == 0 && unit.Definition.IsHero);
        if (hero is null) return;
        if (_config.HeroRule.AddBattleConstruct || _config.Modifiers.SummonToken)
        {
            if (_config.HeroRule.AddBattleConstruct)
                SpawnTemporary(_config.Summons.HeroConstruct, 0, FindOpenNear(hero.Cell, 0), .85f, .9f);
            if (_config.Modifiers.SummonToken)
                SpawnTemporary(_config.Summons.ItemToken, 0, FindOpenNear(hero.Cell, 0), .85f, .9f);
        }
    }

    private void Act(BattleUnitState unit)
    {
        if (!unit.Alive) return;
        ApplyPeriodicBehavior(unit);
        if (!unit.Alive) return;
        if (unit.DisabledTicks > 0)
        {
            unit.DisabledTicks--;
            unit.Mode = BattleUnitMode.Disabled;
            unit.WaitingTicks = 0;
            _movement!.ReleaseGoal(unit.RuntimeId);
            return;
        }
        if (unit.AttackCooldown > 0) unit.AttackCooldown--;
        if (unit.MoveCooldown > 0) unit.MoveCooldown--;

        if (unit.Definition.HealPower > 0)
        {
            var wounded = Allies(unit.Team).Where(ally => ally != unit && ally.Health < ally.MaxHealth)
                .OrderBy(ally => ally.Health / ally.MaxHealth).ThenBy(ally => ally.RuntimeId, StringComparer.Ordinal).ToArray();
            var protectedAlly = _movement!.SelectTarget(unit, wounded);
            if (protectedAlly is not null)
            {
                SetActionTarget(unit, protectedAlly);
                unit.LastActionKind = BattleActionKind.Heal;
                if (Distance(unit.Cell, protectedAlly.Cell) <= unit.Definition.Range && HasLineAccess(unit, protectedAlly))
                {
                    _movement.ReleaseGoal(unit.RuntimeId);
                    if (unit.AttackCooldown == 0)
                    {
                        HealLiving(unit.RuntimeId, protectedAlly, unit.Definition.HealPower);
                        unit.AttackCooldown = unit.Definition.AttackTicks;
                        unit.Mode = BattleUnitMode.Casting;
                        unit.WaitingTicks = 0;
                        Emit("heal", unit.RuntimeId, protectedAlly.RuntimeId, unit.Definition.HealPower, protectedAlly.Cell, "skill_cast");
                    }
                    else unit.Mode = BattleUnitMode.Recovering;
                }
                else if (unit.MoveCooldown == 0) _movement.QueueMove(unit);
                else unit.Mode = BattleUnitMode.Seeking;
                return;
            }
        }

        var target = SelectTarget(unit);
        if (target is null)
        {
            ClearActionTarget(unit);
            if (Allies(1 - unit.Team).Any())
            {
                unit.Mode = BattleUnitMode.Waiting;
                unit.WaitingTicks++;
            }
            return;
        }
        SetActionTarget(unit, target);
        unit.LastActionKind = BattleActionKind.Attack;
        if (Distance(unit.Cell, target.Cell) <= unit.Definition.Range && HasLineAccess(unit, target))
        {
            _movement!.ReleaseGoal(unit.RuntimeId);
            if (unit.AttackCooldown == 0) Attack(unit, target);
            else unit.Mode = BattleUnitMode.Recovering;
            return;
        }
        if (unit.MoveCooldown == 0) _movement!.QueueMove(unit);
        else unit.Mode = BattleUnitMode.Seeking;
    }

    private BattleUnitState? SelectTarget(BattleUnitState unit)
    {
        var enemies = Allies(1 - unit.Team).ToList();
        if (enemies.Count == 0)
        {
            _movement!.ClearTarget(unit.RuntimeId);
            return null;
        }
        IEnumerable<BattleUnitState> ordered;
        if (unit.Team == 0 && unit.Definition.IsHero && _config.HeroRule.PreferBossTargets)
        {
            ordered = enemies.OrderByDescending(enemy => enemy.Definition.IsBoss && Distance(unit.Cell, enemy.Cell) <= 3f)
                .ThenBy(enemy => Distance(unit.Cell, enemy.Cell)).ThenBy(enemy => enemy.RuntimeId, StringComparer.Ordinal);
        }
        else if (unit.Definition.Behavior.PreferBacklineTargets)
            ordered = enemies.OrderByDescending(enemy => enemy.Definition.Range + enemy.Definition.HealPower)
                .ThenBy(enemy => Distance(unit.Cell, enemy.Cell)).ThenBy(enemy => enemy.RuntimeId, StringComparer.Ordinal);
        else if (unit.Definition.Role == Content.UnitRole.Assassin)
            ordered = enemies.OrderByDescending(enemy => enemy.Definition.Range).ThenBy(enemy => Distance(unit.Cell, enemy.Cell))
                .ThenBy(enemy => enemy.RuntimeId, StringComparer.Ordinal);
        else
            ordered = enemies.OrderBy(enemy => Distance(unit.Cell, enemy.Cell)).ThenBy(enemy => enemy.RuntimeId, StringComparer.Ordinal);
        return _movement!.SelectTarget(unit, ordered.ToArray());
    }

    private void Attack(BattleUnitState attacker, BattleUnitState target)
    {
        _statistics[attacker.RuntimeId].AttackActions++;
        attacker.AttackCooldown = attacker.Definition.AttackTicks;
        attacker.Mode = BattleUnitMode.Attacking;
        attacker.LastActionKind = BattleActionKind.Attack;
        attacker.WaitingTicks = 0;
        SetActionTarget(attacker, target);
        var rawDamage = EffectiveDamage(attacker);
        if (attacker.Definition.Behavior.LowHealthDamageBonus > 0 && attacker.Health / attacker.MaxHealth <= .4f)
            rawDamage *= 1f + attacker.Definition.Behavior.LowHealthDamageBonus;
        var damage = ApplyDamage(attacker.RuntimeId, attacker, target, rawDamage);
        if (attacker.Alive && attacker.LifeSteal > 0)
            HealLiving(attacker.RuntimeId, attacker, damage * attacker.LifeSteal);
        Emit("attack", attacker.RuntimeId, target.RuntimeId, damage, target.Cell, "attack");
        if (attacker.Definition.SplashRadius > 0)
            foreach (var splash in Allies(target.Team).Where(other => other != target && Distance(other.Cell, target.Cell) <= attacker.Definition.SplashRadius).ToArray())
                ApplyDamage(attacker.RuntimeId, attacker, splash, rawDamage * .45f);
        if (attacker.Definition.Behavior.PiercingLine)
        {
            var behind = Allies(target.Team).FirstOrDefault(other => other != target && other.Cell.Y == target.Cell.Y &&
                Math.Sign(other.Cell.X - target.Cell.X) == Math.Sign(target.Cell.X - attacker.Cell.X));
            if (behind is not null) ApplyDamage(attacker.RuntimeId, attacker, behind, rawDamage * .35f);
        }
        if (attacker.Definition.Behavior.SlowOnHitTicks > 0 && target.Alive)
        {
            target.AttackCooldown += attacker.Definition.Behavior.SlowOnHitTicks / 2;
            target.MoveCooldown += attacker.Definition.Behavior.SlowOnHitTicks;
        }
    }

    private float ApplyDamage(string sourceRuntimeId, BattleUnitState? source, BattleUnitState target, float raw)
    {
        var healthBefore = target.Health;
        var wasAlive = target.Alive;
        var context = CreateRuleContext();
        if (target.Team == 0 && target.Definition.IsHero && _config.HeroRule.EmptySlotHeroDefense > 0)
            raw *= Math.Max(.25f, 1f - _config.EmptyDeploymentSlots * _config.HeroRule.EmptySlotHeroDefense);
        raw = _config.FloorRule.ModifyIncomingDamage(context, target, raw);
        if (source is not null && source.Definition.Behavior.ExecuteHealthThreshold > 0 && target.Health / target.MaxHealth <= source.Definition.Behavior.ExecuteHealthThreshold)
            raw *= 1.5f;
        var armor = EffectiveArmor(target);
        var damage = Math.Max(1f, raw * 100f / (100f + armor * 7f));
        var absorbed = Math.Min(target.Shield, damage);
        target.Shield -= absorbed;
        damage -= absorbed;
        target.Health = Math.Max(0, target.Health - damage);
        var healthRemoved = Math.Min(healthBefore, damage);
        var effectiveDamage = absorbed + healthRemoved;
        var targetStatistics = _statistics[target.RuntimeId];
        targetStatistics.DamageTaken += effectiveDamage;
        targetStatistics.ShieldAbsorbed += absorbed;
        if (_statistics.TryGetValue(sourceRuntimeId, out var sourceStatistics))
        {
            sourceStatistics.DamageDealt += effectiveDamage;
            if (wasAlive && !target.Alive) sourceStatistics.Kills++;
        }
        if (!target.Alive)
        {
            targetStatistics.DefeatTick ??= TickIndex;
            target.Mode = BattleUnitMode.Defeated;
            target.LastActionKind = BattleActionKind.None;
            ClearActionTarget(target);
            target.WaitingTicks = 0;
            Emit("defeated", sourceRuntimeId, target.RuntimeId, damage, target.Cell, "defeated");
            HandleDeath(source, target);
        }
        else Emit("damage", sourceRuntimeId, target.RuntimeId, damage, target.Cell, "hit");
        return damage;
    }

    private void ApplyFloorRule()
    {
        _config.FloorRule.OnTick(CreateRuleContext());
    }

    private bool BeaconControlled(int team)
    {
        var center = new Vector2I(Width / 2, Height / 2);
        var friendly = Allies(team).Count(unit => Distance(unit.Cell, center) <= 1.5f);
        var enemy = Allies(1 - team).Count(unit => Distance(unit.Cell, center) <= 1.5f);
        return friendly > enemy && friendly > 0;
    }

    private bool HasLineAccess(BattleUnitState source, BattleUnitState target)
        => HasLineAccess(source.Cell, source.Definition, target.Cell);

    private bool HasLineAccess(Vector2I sourceCell, UnitSnapshot sourceDefinition, Vector2I targetCell)
    {
        var delta = targetCell - sourceCell;
        var steps = Math.Max(Math.Abs(delta.X), Math.Abs(delta.Y));
        for (var step = 1; step < steps; step++)
        {
            var x = sourceCell.X + Mathf.RoundToInt(delta.X * (step / (float)steps));
            var y = sourceCell.Y + Mathf.RoundToInt(delta.Y * (step / (float)steps));
            if (!_config.FloorRule.CanOccupy(new Vector2I(x, y))) return false;
        }
        return true;
    }

    private bool CanOccupy(Vector2I cell)
    {
        if (cell.X < 0 || cell.X >= Width || cell.Y < 0 || cell.Y >= Height) return false;
        if (!_config.FloorRule.CanOccupy(cell)) return false;
        return _movement?.IsReserved(cell) != true && _units.All(unit => !unit.Alive || unit.Cell != cell);
    }

    private Vector2I FindOpenNear(Vector2I origin, int team)
    {
        var directions = new[] { Vector2I.Down, Vector2I.Up, Vector2I.Left, Vector2I.Right };
        foreach (var direction in directions)
        {
            var cell = origin + direction;
            if (CanOccupy(cell)) return cell;
        }
        for (var y = 0; y < Height; y++)
            for (var x = team == 0 ? 0 : Width - 1; x >= 0 && x < Width; x += team == 0 ? 1 : -1)
                if (CanOccupy(new Vector2I(x, y))) return new Vector2I(x, y);
        return origin;
    }

    private bool TryFindOpenNear(Vector2I origin, int team, IReadOnlySet<Vector2I> reserved, out Vector2I result)
    {
        var directions = new[] { Vector2I.Down, Vector2I.Up, Vector2I.Left, Vector2I.Right };
        foreach (var direction in directions)
        {
            var cell = origin + direction;
            if (!reserved.Contains(cell) && CanOccupy(cell))
            {
                result = cell;
                return true;
            }
        }
        for (var y = 0; y < Height; y++)
        for (var x = team == 0 ? 0 : Width - 1; x >= 0 && x < Width; x += team == 0 ? 1 : -1)
        {
            var cell = new Vector2I(x, y);
            if (reserved.Contains(cell) || !CanOccupy(cell)) continue;
            result = cell;
            return true;
        }
        result = default;
        return false;
    }

    private bool SpawnTemporary(UnitSnapshot? snapshot, int team, Vector2I cell, float healthScale, float damageScale)
    {
        if (snapshot is null || !CanOccupy(cell)) return false;
        var unit = new BattleUnitState
        {
            RuntimeId = $"s-{team}-{_summonCounter++}", SourceInstanceId = string.Empty, Definition = snapshot, Team = team, Cell = cell,
            MaxHealth = snapshot.MaxHealth * healthScale, Health = snapshot.MaxHealth * healthScale,
            Damage = snapshot.Damage * damageScale, LifeSteal = snapshot.LifeSteal, IsTemporary = true
        };
        _units.Add(unit);
        _statistics.Add(unit.RuntimeId, new BattleUnitStatistics { JoinTick = TickIndex });
        Emit("summoned", unit.RuntimeId, "", 0, cell, "skill_cast");
        return true;
    }

    private void ApplyPeriodicBehavior(BattleUnitState unit)
    {
        var behavior = unit.Definition.Behavior;
        if (behavior.PeriodicShieldTicks > 0 && TickIndex % behavior.PeriodicShieldTicks == 0)
        {
            unit.Shield += behavior.PeriodicShieldAmount;
            Emit("shield", unit.RuntimeId, unit.RuntimeId, behavior.PeriodicShieldAmount, unit.Cell, "skill_cast");
        }
        if (behavior.PeriodicSummonTicks > 0 && TickIndex % behavior.PeriodicSummonTicks == 0 &&
            (behavior.PeriodicSummonLimit <= 0 || _units.Count(other => other.Team == unit.Team && other.IsTemporary && other.Alive) < behavior.PeriodicSummonLimit))
            SpawnTemporary(unit.BehaviorSummon, unit.Team, FindOpenNear(unit.Cell, unit.Team), .65f, .7f);
    }

    private BattleRuleContext CreateRuleContext() => new(
        TickIndex, _units, Allies,
        (source, target, amount) => ApplyDamage(source, null, target, amount),
        (target, amount) => HealLiving(string.Empty, target, amount),
        Emit, BeaconControlled);

    private float HealLiving(string sourceRuntimeId, BattleUnitState target, float amount)
    {
        if (!target.Alive || amount <= 0) return 0;
        var before = target.Health;
        target.Health = Math.Min(target.MaxHealth, target.Health + amount);
        var effectiveHealing = target.Health - before;
        if (effectiveHealing > 0 && _statistics.TryGetValue(sourceRuntimeId, out var sourceStatistics))
        {
            sourceStatistics.HealingDone += effectiveHealing;
            sourceStatistics.EffectiveHealingEvents++;
        }
        return effectiveHealing;
    }

    private void HandleDeath(BattleUnitState? source, BattleUnitState target)
    {
        if (!_deathProcUnits.Add(target.RuntimeId)) return;
        foreach (var unit in _units.Where(unit => unit.ActionTargetRuntimeId == target.RuntimeId))
            ClearActionTarget(unit);
        _movement?.ReleaseUnit(target.RuntimeId);
        if (target.Definition.Behavior.OnDeathDamage > 0)
            foreach (var enemy in Allies(1 - target.Team).Where(enemy => Distance(enemy.Cell, target.Cell) <= 1.5f).ToArray())
                ApplyDamage(target.RuntimeId, null, enemy, target.Definition.Behavior.OnDeathDamage);
        if (target.Team == 0 && _config.HeroRule.SummonOnAllyDeath && !target.Definition.IsHero && !target.IsTemporary)
            SpawnTemporary(_config.Summons.DeathSummon, 0, target.Cell, .6f, .65f);
        if (source is { Team: 0 } && _config.HeroRule.KillGrowth > 0 &&
            (string.IsNullOrWhiteSpace(_config.HeroRule.RequiredSoldierTag) || source.Definition.Tags.Contains(_config.HeroRule.RequiredSoldierTag)))
            foreach (var ally in Allies(0).Where(ally => string.IsNullOrWhiteSpace(_config.HeroRule.RequiredSoldierTag) || ally.Definition.Tags.Contains(_config.HeroRule.RequiredSoldierTag)))
                ally.Damage *= 1f + _config.HeroRule.KillGrowth;
    }

    private float EffectiveDamage(BattleUnitState unit)
    {
        var multiplier = 1f;
        var adjacent = Allies(unit.Team).Where(ally => ally != unit && Distance(ally.Cell, unit.Cell) <= 1.5f).ToArray();
        if (adjacent.Length > 0)
        {
            if (unit.Team == 0)
            {
                multiplier *= 1f + _config.HeroRule.FormationDamageBonus;
                multiplier *= _config.Modifiers.FormationAdjacentDamageMultiplier;
            }
            multiplier *= 1f + adjacent.Sum(ally => ally.Definition.Behavior.AdjacentDamageAura);
        }
        return unit.Damage * multiplier;
    }

    private float EffectiveArmor(BattleUnitState unit)
    {
        var armor = unit.Definition.Armor;
        var adjacent = Allies(unit.Team).Where(ally => ally != unit && Distance(ally.Cell, unit.Cell) <= 1.5f).ToArray();
        if (adjacent.Length > 0)
        {
            if (unit.Team == 0)
                armor += _config.HeroRule.FormationArmorBonus + _config.Modifiers.FormationAdjacentArmor;
            armor += adjacent.Sum(ally => ally.Definition.Behavior.AdjacentArmorAura);
        }
        return armor;
    }

    private IEnumerable<BattleUnitState> Allies(int team) => _units.Where(unit => unit.Team == team && unit.Alive);

    private static void SetActionTarget(BattleUnitState unit, BattleUnitState target)
    {
        unit.ActionTargetRuntimeId = target.RuntimeId;
        unit.ActionTargetName = target.Definition.DisplayName;
    }

    private static void ClearActionTarget(BattleUnitState unit)
    {
        unit.ActionTargetRuntimeId = string.Empty;
        unit.ActionTargetName = string.Empty;
    }

    private void ResolveOutcome()
    {
        var playerHeroAlive = _units.Any(unit => unit.Team == 0 && unit.Definition.IsHero && unit.Alive);
        var enemyAlive = _units.Any(unit => unit.Team == 1 && unit.Alive);
        if (!playerHeroAlive) Outcome = BattleOutcome.PlayerDefeat;
        else if (!enemyAlive) Outcome = BattleOutcome.PlayerVictory;
        else if (TickIndex >= MaxTicks) Outcome = BattleOutcome.Timeout;
        if (Outcome != BattleOutcome.Running)
        {
            EndFloorRule(Outcome);
            Emit("battle_finished", "", "", (float)Outcome, new Vector2I(), "idle");
        }
    }

    private void EndFloorRule(BattleOutcome outcome)
    {
        if (_floorRuleEnded) return;
        _floorRuleEnded = true;
        _config.FloorRule.OnBattleEnded(CreateRuleContext(), outcome);
    }

    private void Emit(string type, string source, string target, float value, Vector2I cell, string cue)
    {
        var battleEvent = new BattleEvent(TickIndex, type, source, target, value, cell, cue);
        _events.Add(battleEvent);
        _digest.Append(TickIndex).Append('|').Append(type).Append('|').Append(source).Append('|').Append(target).Append('|')
            .Append(value.ToString("0.###", CultureInfo.InvariantCulture)).Append('|').Append(cell.X).Append(',').Append(cell.Y).Append(';');
    }

    private static float Distance(Vector2I a, Vector2I b) => a.DistanceTo(b);
    private static Vector2I ClampCell(Vector2I cell) => new(Math.Clamp(cell.X, 0, Width - 1), Math.Clamp(cell.Y, 0, Height - 1));

    private sealed record PendingCommandSpawn(UnitSnapshot Profile, Vector2I Cell, float HealthScale, float DamageScale);

    private sealed class BattleUnitStatistics
    {
        public float DamageDealt { get; set; }
        public float DamageTaken { get; set; }
        public float ShieldAbsorbed { get; set; }
        public float HealingDone { get; set; }
        public int Kills { get; set; }
        public int JoinTick { get; init; }
        public int? DefeatTick { get; set; }
        public int AttackActions { get; set; }
        public int EffectiveHealingEvents { get; set; }
    }
}
