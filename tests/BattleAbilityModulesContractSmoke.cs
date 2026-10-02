using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;

public partial class BattleAbilityModulesContractSmoke : Node
{
    public override void _Ready()
    {
        try
        {
            VerifyBoundaries();
            VerifySummonReservationsAndBonus();
            VerifyStatisticsRollbackAndFreeze();
            GD.Print("BATTLE_ABILITY_MODULES_CONTRACT_OK independent-modules reservations preflight bonus-order cap-consumption statistics-rollback frozen-reports checkpoint-owner");
            GetTree().Quit();
        }
        catch (Exception e) { GD.PrintErr("BATTLE_ABILITY_MODULES_CONTRACT_FAILED " + e); GetTree().Quit(1); }
    }

    private static void VerifyBoundaries()
    {
        foreach (var type in new[] { typeof(BattleAbilityExecutor), typeof(BattleAbilityEffectOperations), typeof(BattleAbilitySummons), typeof(BattleStatistics), typeof(BattleEnemyActionScope) })
        {
            Require(type.DeclaringType is null, type.Name + " is still nested in the root");
            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                Require(field.FieldType != typeof(BattleSimulation) && field.FieldType != typeof(BattleUnitState), type.Name + " captures a mutable root/unit");
        }
        foreach (var port in new[] { typeof(IBattleAbilityHost), typeof(IBattleAbilityEffectWorld), typeof(IBattleAbilitySummonWorld), typeof(IBattleEnemyQueries), typeof(IBattleEnemyCommands) })
            foreach (var method in port.GetMethods())
                Require(method.ReturnType != typeof(BattleSimulation) && method.ReturnType != typeof(BattleUnitState), "port exposes mutable authority");
    }

    private static void VerifySummonReservationsAndBonus()
    {
        using var world = new World();
        var module = new BattleAbilitySummons(world);
        var summon = new CompiledSummonAbilityOperation(AbilitySummonProfile.Mercenary, 1, 1, 1, 0, true, "");
        var ability = Ability(summon, summon);
        var invocation = new BattleAbilityInvocation(ability, "owner", "owner", "", 0,
            new(0, ImmutableDictionary<string, AbilityEntitySnapshot>.Empty));
        var reservations = new BattleAbilitySummons.Reservations();
        Require(module.Prepare(summon, invocation, 0, reservations).Error == "", "first summon preparation");
        Require(module.Prepare(summon, invocation, 1, reservations).Error == "", "second summon preparation");
        Require(world.Trace.Count == 0 && world.Units.Count == 1, "preparation generated bodies or consumed bonus");
        Require(reservations.Items.Select(r => r.PositionX).Distinct().Count() == 2, "operations reserved the same space");
        var operations = ImmutableArray.Create(new ResolvedAbilityOperation(0, summon, [], 0), new ResolvedAbilityOperation(1, summon, [], 0));
        var plan = new AbilityExecutionPlan(ability, "owner", "owner", 0, operations, reservations.Items.ToImmutableArray(), 0);
        world.Blocked = true;
        Require(module.Validate(plan).Length > 0 && world.Trace.Count == 0, "blocked landing accepted or wrote state");
        world.Blocked = false;
        Require(module.Validate(plan) == "", "legal reservations rejected");
        var facts = ImmutableArray.CreateBuilder<string>();
        foreach (var operation in operations) Require(module.Execute(summon, operation, plan, facts), "reserved summon did not materialize");
        module.CompleteBonuses(plan, new[] { 0, 1 });
        Require(world.Trace.Take(4).SequenceEqual(new[] { "spawn", "spawn", "consume", "bonus" }), "trait bonus occupied authored reservation early");
        Require(facts.Count == 2 && world.Units.Count == 4, "wrong authored/bonus count");

        using var capped = new World();
        var cappedModule = new BattleAbilitySummons(capped);
        capped.Spawn(capped.Profile, 0, new(2, 2), 1, 1, "owner");
        var limited = summon with { MaximumLivingTemporaryUnits = 1 };
        var cappedPlan = plan with { Ability = Ability(limited, limited) };
        cappedModule.CompleteBonuses(cappedPlan, new[] { 0 });
        Require(!capped.BonusAvailable && !capped.Trace.Contains("bonus"), "living cap failed to consume first-success eligibility");
        var full = new BattleAbilitySummons.Reservations();
        Require(cappedModule.Prepare(limited, invocation, 0, full).Error.Length > 0 && full.Items.Count == 0, "living cap ignored during preparation");
    }

    private static void VerifyStatisticsRollbackAndFreeze()
    {
        using var world = new World();
        world.Spawn(world.Profile, 0, new(3, 2), 1, 1, "owner");
        var target = world.Units[1];
        var stats = new BattleStatistics();
        stats.Join("owner", 0); stats.Join(target.RuntimeId, 3);
        var checkpoint = stats.Capture();
        stats.Attack("owner");
        stats.Damage("owner", target.RuntimeId, 40, 10, true);
        stats.Damage("floor", target.RuntimeId, 5, 0, false);
        stats.Heal("owner", 8); stats.Heal("owner", 0); stats.Heal("missing", 20);
        stats.Defeat(target.RuntimeId, 7); stats.Defeat(target.RuntimeId, 9);
        var reports = stats.ReadReports(world.Units, _ => 6);
        Require(reports[0].DamageDealt == 40 && reports[0].Kills == 1 && reports[0].HealingDone == 8 && reports[0].EffectiveHealingEvents == 1, "source credit");
        Require(reports[1].DamageTaken == 45 && reports[1].ShieldAbsorbed == 10 && reports[1].JoinTick == 3 && reports[1].DefeatTick == 7, "target accounting");
        stats.Freeze(world.Units, _ => 6);
        stats.Attack("owner");
        world.Owner.Health = 1;
        Require(stats.ReadReports(world.Units, _ => 99).SequenceEqual(reports), "terminal report changed after freeze");
        stats.Restore(checkpoint);
        reports = stats.ReadReports(world.Units, _ => 6);
        Require(stats.AttackActions("owner") == 0 && reports[0].FinalHealth == 1 && reports[0].DamageDealt == 0 && reports[1].DefeatTick is null, "rollback retained statistics or terminal freeze");
        var rejected = false;
        try { new BattleStatistics().Restore(checkpoint); } catch (InvalidOperationException) { rejected = true; }
        Require(rejected, "cross-battle statistics checkpoint accepted");
        stats.Join("rolled_back_spawn", 20);
        stats.Restore(checkpoint);
        stats.Join("rolled_back_spawn", 20); // Rollback must release newly generated identities too.
    }

    private static CompiledAbilityDefinition Ability(params CompiledAbilityOperation[] operations) =>
        new("module_probe", "测试", "", AbilityActivationKind.ManualCommand, AbilityTriggerKind.None, 0, 0, 0, 0, 0, operations.ToImmutableArray(), null);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

    private sealed class World : IBattleAbilitySummonWorld, IDisposable
    {
        private readonly BattleAttributeScope _attributes = new("ability-module-probe");
        private readonly List<BattleUnitView> _units = [];
        public readonly List<string> Trace = [];
        public readonly UnitSnapshot Profile = new("probe", "测试", UnitRole.Fighter, true, false, 100, 10, 1, 10, 1, 0, 0, 0, 0, [], new UnitBehaviorSnapshot());
        public readonly BattleUnitState Owner;
        public bool Blocked;
        public bool BonusAvailable = true;
        public IReadOnlyList<BattleUnitView> Units => _units;
        public World() { Owner = Add("owner", false, Vector2.Zero); }
        private BattleUnitState Add(string id, bool temporary, Vector2 position)
        {
            var state = new BattleUnitState
            {
                RuntimeId = id, SourceInstanceId = id, Definition = Profile, Team = 0, InitialTeam = 0,
                Attributes = _attributes.CreateSet(id, new CompiledAttributeSetDefinition([new(CombatAttribute.MaxHealth, 100, 0, 1000), new(CombatAttribute.AttackDamage, 10, 0, 1000)], id)),
                Cell = Vector2I.Zero, Health = 100, IsTemporary = temporary, SummonerRuntimeId = temporary ? "owner" : "", Position = position
            };
            _units.Add(BattleUnitView.For(state));
            return state;
        }
        public UnitSnapshot? ResolveProfile(CompiledSummonAbilityOperation summon, string ownerId) => Profile;
        public bool TryFindOpenNear(Vector2 origin, int team, float radius, IReadOnlyList<(Vector2 Position, float Radius)> reserved, out Vector2 position)
        { position = new Vector2(4 + reserved.Count, 2); return !Blocked; }
        public bool CanOccupy(Vector2 position, float radius, IReadOnlyList<(Vector2 Position, float Radius)> reserved) =>
            !Blocked && reserved.All(space => space.Position != position);
        public bool Spawn(UnitSnapshot profile, int team, Vector2 position, float health, float damage, string sourceId)
        { Trace.Add("spawn"); Add("summon_" + _units.Count, true, position); return true; }
        public void SpawnNear(UnitSnapshot profile, int team, Vector2 origin, float health, float damage, string sourceId)
        { Trace.Add("bonus"); Add("bonus_" + _units.Count, true, new(10, 2)); }
        public bool ConsumeExtraSummon(string ownerId)
        { Trace.Add("consume"); var available = BonusAvailable; BonusAvailable = false; return available; }
        public void Dispose() => _attributes.Dispose();
    }
}
