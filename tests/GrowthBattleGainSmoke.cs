using System;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.BattleLab;
using TowerAutobattler.Content;
using TowerAutobattler.Growth;
using static TraitMatrixValidationSupport;

public partial class GrowthBattleGainSmoke : Node
{
    public override async void _Ready()
    {
        try
        {
            var publication = await GrowthContentPackage.CreateReadyAsync(this);
            var package = publication.Package ?? throw new InvalidOperationException(string.Join(';', publication.Report.CoreErrors));
            var index = new BattleLabContentIndex(package);
            var rules = GrowthContentPackage.LoadRules(package.Content);
            VerifyCommittedAndDeduplicated(index, rules);
            VerifyNoExecutionNoGain(index, rules);
            VerifyRollback(index);
            VerifyMx03AscensionRetainsChillChain(index, rules);
            VerifyGuardFormationDistribution(index, rules);
            GD.Print("GROWTH_BATTLE_GAIN_SMOKE_OK");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    private static CompiledMatrixOperation Compile(MatrixAbilityOperationSpec spec)
    {
        var report = new ValidationReport();
        using (spec)
        {
            var result = MatrixAbilityCompiler.Compile(spec, "growth gain smoke", report);
            Require(result is not null && !report.HasCoreErrors, string.Join(';', report.CoreErrors));
            return result!;
        }
    }

    private static CompiledAbilityDefinition FixtureAbility(string id)
    {
        var gain = Compile(new MatrixAbilityOperationSpec
        {
            Kind = MatrixOperationKind.PermanentAttribute, Target = MatrixTargetKind.Self,
            Attribute = CombatAttribute.MaxHealth, Amount = 16
        });
        var sequence = Compile(new MatrixAbilityOperationSpec
        {
            Kind = MatrixOperationKind.Sequence, Target = MatrixTargetKind.Self,
            Effects = [ToSpec(gain), ToSpec(gain)]
        });
        return new(id, "永久成长夹具", "同槽重复执行只记一次", AbilityActivationKind.Automatic,
            AbilityTriggerKind.BattleStarted, 0, 0, 1, 1, 0, [sequence], null);
    }

    private static MatrixAbilityOperationSpec ToSpec(CompiledMatrixOperation operation) => new()
    {
        Kind = operation.Kind, Target = operation.Target, Attribute = operation.Attribute, Amount = operation.Amount
    };

    private static BattleConfig Config(BattleLabContentIndex index, CompiledAbilityDefinition? ability,
        Action<BattleCombatBindingRegistry>? bindings = null)
    {
        var config = Laboratory(index, ["hero_mx01"], ["soldier_dummy_static"], 20260927);
        config.Spawns[0] = config.Spawns[0] with
        {
            Unit = config.Spawns[0].Unit with { AbilityLoadout = ability is null ? null : new([ability]) }
        };
        return new BattleConfig
        {
            Seed = config.Seed, FloorRule = config.FloorRule, HeroRule = config.HeroRule,
            Spawns = config.Spawns, ConfigureCombatBindings = bindings
        };
    }

    private static BattleConfig ActualMx01Config(BattleLabContentIndex index, CompiledGrowthRules rules, bool allowFreeze)
    {
        var allies = allowFreeze
            ? new[] { "hero_mx01", "hero_mx02", "hero_mx03", "hero_mx04" }
            : new[] { "hero_mx01", "hero_mx02" };
        var config = Laboratory(index, allies, ["soldier_dummy_static"], 20260927);
        var mx01 = rules.Heroes["hero_mx01"].BaseLoadout ?? throw new InvalidOperationException("MX01 growth base loadout missing");
        for (var i = 0; i < config.Spawns.Count; i++)
        {
            var spawn = config.Spawns[i];
            config.Spawns[i] = spawn with
            {
                Cell = spawn.Team == 0
                    ? new Vector2I(2, spawn.Unit.ContentId == "hero_mx01" ? 2 : i < 2 ? i : i + 1)
                    : new Vector2I(3, 2),
                Unit = spawn.Unit with
                {
                    AbilityLoadout = spawn.Unit.ContentId == "hero_mx01" ? mx01 : spawn.Unit.AbilityLoadout,
                    MaxHealth = spawn.Team == 1 ? 100000 : spawn.Unit.MaxHealth,
                    Damage = spawn.Team == 1 ? 2 : spawn.Unit.Damage,
                    Behavior = spawn.Unit.Behavior with { Stationary = !allowFreeze && spawn.Team == 1 }
                }
            };
        }
        return config;
    }

    private static void VerifyCommittedAndDeduplicated(BattleLabContentIndex index, CompiledGrowthRules rules)
    {
        using var battle = new BattleSimulation(ActualMx01Config(index, rules, true));
        var owner = battle.Units.Single(unit => unit.Definition.ContentId == "hero_mx01");
        var before = owner.MaxHealth;
        for (var tick = 0; tick < 800 && battle.CreateResult().PermanentGains.IsEmpty; tick++) battle.Step();
        Require(Math.Abs(owner.MaxHealth - before - 16) < .01f, "permanent gain did not affect the current battle");
        for (var tick = 0; tick < 300 && battle.Outcome == BattleOutcome.Running; tick++) battle.Step();
        var gains = battle.CreateResult().PermanentGains;
        Require(gains.Length == 1 && gains[0].Amount == 16 && gains[0].TargetInstanceId == owner.SourceInstanceId &&
            gains[0].AbilityId == "hero_mx01_permanent_frost_growth" &&
            battle.CombatEvents.Any(e => e.Kind == BattleCombatEventKind.ControlApplied && e.SourceRuntimeId == owner.RuntimeId),
            "real MX01 freeze did not produce exactly one permanent growth ledger entry");
    }

    private static void VerifyNoExecutionNoGain(BattleLabContentIndex index, CompiledGrowthRules rules)
    {
        using var battle = new BattleSimulation(ActualMx01Config(index, rules, false));
        for (var tick = 0; tick < 200 && battle.Outcome == BattleOutcome.Running; tick++) battle.Step();
        Require(!battle.CombatEvents.Any(e => e.Kind == BattleCombatEventKind.ControlApplied) && battle.CreateResult().PermanentGains.IsEmpty,
            "MX01 battle without a real freeze produced permanent growth");
    }

    private static void VerifyRollback(BattleLabContentIndex index)
    {
        var ability = FixtureAbility("growth_gain_rollback");
        var injected = 0;
        using var battle = new BattleSimulation(Config(index, ability, bindings => bindings.Subscribe(
            BattleCombatEventKind.AbilityResolved, CombatSourceRef.System("growth-gain-rollback"), 0, (fact, sink) =>
            {
                if (fact.SubjectStableId != ability.StableId) return;
                injected++;
                sink.Enqueue(CombatSourceRef.System("growth-gain-rollback"), 0,
                    _ => throw new InvalidOperationException("intentional permanent gain rollback"));
            })));
        var owner = battle.Units.Single(unit => unit.Team == 0);
        var before = owner.MaxHealth;
        battle.Step();
        Require(injected == 1 && Math.Abs(owner.MaxHealth - before) < .01f && battle.CreateResult().PermanentGains.IsEmpty,
            "failed transaction leaked permanent battle growth or its ledger");
    }

    private static void VerifyMx03AscensionRetainsChillChain(BattleLabContentIndex index, CompiledGrowthRules rules)
    {
        var growth = rules.Heroes["hero_mx03"];
        var basePassive = growth.BaseLoadout?.Find("hero_mx03_p")
            ?? throw new InvalidOperationException("MX03 base passive missing");
        var ascendedPassive = growth.AscendedLoadout?.Find("hero_mx03_growth_ascended_p")
            ?? throw new InvalidOperationException("MX03 ascended passive missing");

        Require(!AlternatingTargetsApplyChill(index, basePassive),
            "MX03 base passive retained chill-chain across alternating targets");
        Require(AlternatingTargetsApplyChill(index, ascendedPassive),
            "MX03 ascended passive reset chill-chain across alternating targets");
    }

    private static void VerifyGuardFormationDistribution(BattleLabContentIndex index, CompiledGrowthRules rules)
    {
        var guard = rules.Spells["guard_formation"].BattleLoadout;
        var config = Laboratory(index, ["hero_mx01", "hero_mx02", "hero_mx03", "hero_mx04"],
            ["soldier_dummy_static"], 20260927);
        for (var i = 0; i < config.Spawns.Count; i++)
        {
            var spawn = config.Spawns[i];
            config.Spawns[i] = spawn with
            {
                Cell = spawn.Unit.ContentId switch
                {
                    "hero_mx01" => new Vector2I(1, 2),
                    "hero_mx02" => new Vector2I(2, 2),
                    "hero_mx03" => new Vector2I(1, 3),
                    "hero_mx04" => new Vector2I(5, 2),
                    _ => new Vector2I(8, 2)
                },
                Unit = spawn.Unit with
                {
                    AbilityLoadout = spawn.Unit.ContentId == "hero_mx01" ? guard : null,
                    Damage = spawn.Team == 1 ? 1 : spawn.Unit.Damage,
                    Behavior = spawn.Unit.Behavior with { Stationary = true }
                }
            };
        }

        using var battle = new BattleSimulation(new BattleConfig
        {
            Seed = config.Seed, FloorRule = config.FloorRule, HeroRule = config.HeroRule,
            Spawns = config.Spawns
        });
        var owner = battle.Units.Single(unit => unit.Definition.ContentId == "hero_mx01");
        var near = battle.Units.Where(unit => unit.Definition.ContentId is "hero_mx02" or "hero_mx03").ToArray();
        var far = battle.Units.Single(unit => unit.Definition.ContentId == "hero_mx04");

        var expected = owner.MaxHealth * .15f;
        Require(Math.Abs(owner.Shield - expected) < .01f && near.All(unit => Math.Abs(unit.Shield - expected) < .01f) &&
            Math.Abs(far.Shield) < .01f,
            $"guard formation distribution mismatch: owner={owner.Shield}, near={string.Join(",", near.Select(unit => unit.Shield))}, far={far.Shield}");
    }

    private static bool AlternatingTargetsApplyChill(BattleLabContentIndex index, CompiledAbilityDefinition passive)
    {
        var config = Laboratory(index, ["hero_mx03"],
            ["soldier_dummy_static", "soldier_dummy_static", "soldier_dummy_static"], 20260927);
        for (var i = 0; i < config.Spawns.Count; i++)
        {
            var spawn = config.Spawns[i];
            config.Spawns[i] = spawn with
            {
                Cell = spawn.Team == 0 ? new Vector2I(2, 2) : i switch
                {
                    1 => new Vector2I(4, 2),
                    2 => new Vector2I(5, 2),
                    _ => new Vector2I(5, 1)
                },
                Unit = spawn.Unit with
                {
                    AbilityLoadout = spawn.Team == 0 ? new([passive]) : null,
                    MaxHealth = spawn.Team == 1 ? i < 3 ? 1 : 100000 : spawn.Unit.MaxHealth,
                    Damage = spawn.Team == 1 ? 1 : spawn.Unit.Damage,
                    Behavior = spawn.Unit.Behavior with { Stationary = true }
                }
            };
        }

        using var battle = new BattleSimulation(config);
        var owner = battle.Units.Single(unit => unit.Team == 0);
        var observedHits = 0;
        owner.AttackDeliveryOverride = AttackDelivery.Beam;
        while (observedHits < 3 && battle.TickIndex < 300)
        {
            battle.Step();
            var hits = battle.CombatEvents.Count(e => e.Kind == BattleCombatEventKind.AttackLanded && e.SourceRuntimeId == owner.RuntimeId);
            if (hits <= observedHits) continue;
            observedHits = hits;
        }

        Require(observedHits == 3, "MX03 alternating-target fixture did not produce three real basic attacks");
        var hitTargets = battle.CombatEvents
            .Where(e => e.Kind == BattleCombatEventKind.AttackLanded && e.SourceRuntimeId == owner.RuntimeId)
            .Take(3).Select(e => e.TargetRuntimeId).ToArray();
        Require(hitTargets.Distinct(StringComparer.Ordinal).Count() == 3 &&
            battle.Units.Single(unit => unit.RuntimeId == hitTargets[2]).Alive,
            "MX03 alternating-target fixture did not hit three different enemies and leave the payoff target alive");
        return battle.CombatEvents.Any(e => e.Kind == BattleCombatEventKind.StatusApplied &&
            e.SubjectStableId == "matrix_chill" && e.SourceRuntimeId == owner.RuntimeId);
    }
}
