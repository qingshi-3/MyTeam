using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.BattleLab;
using TowerAutobattler.Composition;
using TowerAutobattler.Project;
using TowerAutobattler.Run;
using TowerAutobattler.Statuses;

public partial class DesignedContentContractSmoke : Node
{
    private const string Crossbow = "hero_hc01_crossbow", Guard = "hero_hc03_iron_guard";
    private const string Static = "soldier_dummy_static", Melee = "soldier_dummy_melee", Ranged = "soldier_dummy_ranged";
    private BattleLabContentIndex _index = null!;

    public override async void _Ready()
    {
        var code = 0;
        try
        {
            var gate = await GamePackagePublisher.CreateReadyAsync(this,
                GD.Load<GameProjectDefinition>("res://content/project/alpha_project.tres"));
            var package = gate.Package ?? throw new InvalidOperationException(string.Join('\n', gate.Report.CoreErrors));
            _index = new BattleLabContentIndex(package);
            Require(_index.PlayerHeroes.Select(unit => unit.StableId).Order().SequenceEqual(new[] { Crossbow, Guard }.Order()), "only designed heroes published");
            Require(package.Content.Catalog.Soldiers.Count == 3 &&
                package.Content.Catalog.Soldiers.All(entry => ((TowerAutobattler.Content.UnitDefinition)entry.Definition).IsTestDummy), "old recruits removed");
            foreach (var id in new[] { Static, Melee, Ranged })
                Require(_index.TryGetUnit(id, out var dummy) && dummy.AllowedSides.Length == 2, "dummy deploys on either side");
            ValidatePresets();
            ValidateNewRuns(package);
            ValidateExperienceSlice(package);
            ValidateGrowth(false);
            ValidateGrowth(true);
            ValidateVolley();
            ValidateWindup();
            ValidateTank();
            ValidateSkillHitAndRollback();
            ValidateDummies();
            ValidateMeleeContact();
            GD.Print("DESIGNED_CONTENT_OK publication presets run-start dummies growth target-switch upgrade reset volley cadence taunt reflection");
        }
        catch (Exception e) { GD.PrintErr("DESIGNED_CONTENT_FAILED: " + e); code = 1; }
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        GetTree().Quit(code);
    }

    private BattleLabSession Session(params (string Content, int Team, int X, int Y)[] units)
    {
        var session = new BattleLabSession(_index, 6, 912, BattleLabPlacementMode.FreeExperiment, "rule_clear");
        foreach (var (content, team, x, y) in units)
            Require(session.AddAndPlace(content, (BattleLabSide)team, new Vector2I(x, y)).Succeeded, "fixture placement");
        return session;
    }

    private BattleSimulation Battle(BattleLabSession session, bool noAbilities = false)
    {
        var config = new BattleLabPreparationAdapter(_index).Build(session.Freeze());
        if (noAbilities)
            for (var i = 0; i < config.Spawns.Count; i++)
                config.Spawns[i] = config.Spawns[i] with { Unit = config.Spawns[i].Unit with { AbilityLoadout = null } };
        return new BattleSimulation(config);
    }

    private void ValidatePresets()
    {
        var presets = new BattleLabPresetStore(GD.Load<BattleLabPresetCatalog>("res://content/battle-lab/battle_lab_presets.tres"));
        foreach (var (name, dto) in presets.BuiltIns)
        {
            var session = new BattleLabSession(_index, 6);
            session.Restore(BattleLabPresetStore.ToSnapshot(dto));
            Require(BattleLabDerivedProjectionBuilder.Build(session).IsReady, "preset ready " + name);
            using var battle = Battle(session);
            for (var i = 0; i < 50 && battle.Outcome == BattleOutcome.Running; i++) battle.Step();
        }
    }

    private static void ValidateNewRuns(CompiledGamePackage package)
    {
        foreach (var hero in new[] { Crossbow, Guard })
        {
            var save = new MemorySave();
            var run = new RunApplication(package.Content, save, package.Project);
            Require(run.StartNewRun(hero, 912), "new run starts " + hero);
            Require(run.ActiveRun!.Roster.Count == 2 && run.ActiveRun.Roster.Select(unit => unit.ContentId).Distinct().Count() == 2,
                "selected hero plus the other designed companion");
            Require(!run.Recruit(Melee), "dummy cannot be recruited");
            Require(run.RecruitmentChoices().All(entry => entry.StableId is Crossbow or Guard), "recruitment contains designed heroes only");
        }
    }

    private void ValidateGrowth(bool upgrade)
    {
        var session = Session((Crossbow, 0, 2, 2), (Static, 1, 5, 2), (Static, 1, 7, 3));
        var heroId = session.Units.First(unit => unit.ContentId == Crossbow).InstanceId;
        if (upgrade) Require(session.SetRetentionUpgrade(heroId, true), "upgrade selectable");
        var restored = BattleLabPresetStore.ToSnapshot(BattleLabPresetStore.ToDto(session.Freeze()));
        Require(restored.CanonicalDigest == session.Freeze().CanonicalDigest && restored.Units[0].RetainAttackStacks == upgrade, "upgrade preset round trip");
        using (var battle = Battle(session, noAbilities: true))
        {
            var hero = battle.Units.Single(unit => unit.Team == 0);
            for (var i = 0; i < 65; i++) battle.Step();
            var before = hero.AttackHitStacks;
            Require(before >= 5, "basic hit stacks accumulate");
            Near(hero.Attributes.GetValue(CombatAttribute.AttackSpeed), 1 + before * .04f, "additive speed growth");
            var previousTarget = hero.AttackChainTargetId;
            battle.Units.Single(unit => unit.RuntimeId == previousTarget).Health = 0;
            for (var i = 0; i < 30 && hero.AttackChainTargetId == previousTarget; i++) battle.Step();
            Require(hero.AttackChainTargetId != previousTarget, "switches target");
            Require(upgrade ? hero.AttackHitStacks >= before : hero.AttackHitStacks < before, "target-switch stack policy");
        }
        using var fresh = Battle(session, noAbilities: true);
        var freshHero = fresh.Units.Single(unit => unit.Team == 0);
        Require(freshHero.AttackHitStacks == 0, "new battle starts at zero");
        freshHero.Attributes.SetBaseValue(CombatAttribute.AttackSpeed, 1200);
        Require(freshHero.Attributes.GetValue(CombatAttribute.AttackSpeed) > 1000, "no legacy speed clamp");
        freshHero.Attributes.SetBaseValue(CombatAttribute.AttackSpeed, 36);
        for (var i = 0; i < 4; i++) fresh.Step();
        Require(fresh.PendingEvents.Where(e => e.Type == "attack").GroupBy(e => e.Tick).Any(group => group.Count() > 1), "no one-attack-per-tick cap");
    }

    private static void ValidateExperienceSlice(CompiledGamePackage package)
    {
        var definition = GD.Load<TowerAutobattler.Experience.ExperienceSliceDefinition>("res://content/experience/reward_loop.tres");
        var session = new TowerAutobattler.Experience.ExperienceSliceSession(package, definition);
        Require(session.Run.Roster.Count == 2, "experience slice uses two designed heroes");
        Require(session.ChooseRelic(session.Definition.RelicIds[0]), "experience relic choice");
        Require(session.ChooseRecruit(session.Definition.RecruitIds[0]), "experience recruitment accepts designed hero");
        using var battle = new BattleSimulation(session.StartBattle());
        battle.Step();
        Require(battle.Units.Any(unit => unit.Team == 0), "experience battle starts");
    }

    private void ValidateVolley()
    {
        int Duration(float speed)
        {
            using var battle = Battle(Session((Crossbow, 0, 2, 2), (Static, 1, 5, 2)));
            var hero = battle.Units.Single(unit => unit.Team == 0);
            hero.Attributes.SetBaseValue(CombatAttribute.ManaPerSecond, 0);
            hero.Attributes.SetBaseValue(CombatAttribute.AttackSpeed, speed);
            hero.CurrentMana = hero.MaxMana;
            battle.Step();
            Require(hero.ProjectileSequence is not null && hero.CurrentMana == 0, "volley starts and spends mana");
            for (var i = 0; i < 40 && (hero.ProjectileSequence is not null || !hero.ProjectileWindups.IsEmpty); i++) battle.Step();
            var shots = battle.PendingEvents.Where(e => e.Type == "skill_arrow").ToArray();
            Require(shots.Length == 3, "three sequential skill projectiles");
            Require(hero.CurrentMana == 0 && !battle.PendingEvents.Any(e => e.Type == "attack"), "skill arrows are not mana-generating basic attacks");
            Require(hero.AttackHitStacks > 0, "skill impacts grow attack speed");
            return shots[^1].Tick - shots[0].Tick;
        }
        Require(Duration(2) < Duration(1), "volley cadence scales with attack speed");
    }

    private void ValidateTank()
    {
        using var battle = Battle(Session((Guard, 0, 4, 2), (Crossbow, 0, 4, 4), (Melee, 1, 5, 3), (Ranged, 1, 6, 3)));
        var tank = battle.Units.Single(unit => unit.Definition.ContentId == Guard);
        tank.CurrentMana = tank.MaxMana;
        tank.Shield = 1000;
        battle.Step();
        Near(tank.Armor, 30, "active adds defense");
        var taunted = battle.Units.Where(unit => unit.Team == 1).ToArray();
        Require(taunted.All(unit => unit.Statuses.Any(status => status.Behavior == StatusBehaviorKind.Taunt)), "nearby enemies taunted");
        for (var i = 0; i < 20; i++) battle.Step();
        Require(taunted.All(unit => unit.ActionTargetRuntimeId == tank.RuntimeId), "taunt controls real targeting");
        Require(taunted.Any(unit => unit.Health < unit.MaxHealth), "reflection damages attackers");
        Require(tank.Shield < 1000 && tank.Health == tank.MaxHealth, "shielded hits still trigger reflection");
        tank.Attributes.SetBaseValue(CombatAttribute.ManaPerSecond, 0);
        tank.Attributes.SetBaseValue(CombatAttribute.ManaPerDamageRatio, 0);
        tank.Attributes.SetBaseValue(CombatAttribute.ManaPerAttack, 0);
        tank.CurrentMana = 0;
        for (var i = 0; i < 20; i++) battle.Step();
        Near(tank.Armor, 12, "defense expires");
        Require(taunted.All(unit => unit.Statuses.All(status => status.Behavior != StatusBehaviorKind.Taunt)), "taunt expires");

        using var duel = Battle(Session((Guard, 0, 4, 2), (Melee, 1, 5, 2)));
        var guard = duel.Units.Single(unit => unit.Team == 0);
        var attacker = duel.Units.Single(unit => unit.Team == 1);
        guard.CurrentMana = 0;
        guard.AttackCooldown = 1000;
        guard.Shield = 100;
        guard.Attributes.SetBaseValue(CombatAttribute.Armor, 25);
        // Contact now requires approaching the opponent before the first ordinary hit.
        for (int i = 0; i < 20 && attacker.Health == attacker.MaxHealth; i++) duel.Step();
        Near(attacker.MaxHealth - attacker.Health, 25, "reflect reads current defense exactly once");
        // Two tanks retain passive grants but no attack damage. Reflections must terminate.
        var session = Session((Guard, 0, 4, 2), (Melee, 1, 5, 2));
        var config = new BattleLabPreparationAdapter(_index).Build(session.Freeze());
        config.Spawns[1] = config.Spawns[1] with { Unit = config.Spawns[0].Unit };
        using var mirrors = new BattleSimulation(config);
        foreach (var unit in mirrors.Units) { unit.CurrentMana = 0; unit.Damage = 0; }
        for (int i = 0; i < 10; i++) mirrors.Step();
        Require(mirrors.PendingEvents.Any(fact => fact.Type == "attack"), "reflection duel reached melee contact");
        Require(mirrors.Units.All(unit => unit.Alive), "reflection does not recurse as basic attack");
    }

    private void ValidateWindup()
    {
        BattleSimulation NewBattle() => Battle(Session((Crossbow, 0, 2, 2), (Static, 1, 6, 2)), noAbilities: true);
        using (var battle = NewBattle())
        {
            var hero = battle.Units.Single(unit => unit.Team == 0);
            var target = battle.Units.Single(unit => unit.Team == 1);
            battle.Step();
            Require(battle.Projectiles.IsEmpty && !battle.PendingEvents.Any(e => e.Type == "attack"), "windup does not launch or declare a completed attack");
            var timing = battle.PendingEvents.Single(e => e.Type == "attack_windup").AttackTiming!;
            var release = hero.ProjectileWindups.Single().ReleaseTick;
            Near(timing.PlaybackSeconds * timing.ReleaseProgress, (release - battle.TickIndex) * .1f, "animation release matches authoritative windup");
            while (battle.TickIndex < release - 1) battle.Step();
            Require(battle.Projectiles.IsEmpty && target.Health == target.MaxHealth, "no early projectile or damage during draw");
            battle.Step();
            Require(battle.Projectiles.Length == 1 && target.Health == target.MaxHealth, "release creates projectile without launch-time damage");
            for (var i = 0; i < 6; i++) battle.Step();
            Require(target.Health < target.MaxHealth, "released projectile damages only on collision");
        }
        using (var controlled = NewBattle())
        {
            var hero = controlled.Units.Single(unit => unit.Team == 0);
            controlled.Step();
            hero.DisabledTicks = 10;
            for (var i = 0; i < 7; i++) controlled.Step();
            Require(hero.ProjectileWindups.IsEmpty && controlled.Projectiles.IsEmpty &&
                !controlled.PendingEvents.Any(e => e.Type == "attack"), "control cancels an unreleased arrow");
        }
        using (var moved = NewBattle())
        {
            moved.Step();
            moved.Units.Single(unit => unit.Team == 1).Position = new Vector2(9.5f, 5.5f);
            for (var i = 0; i < 6; i++) moved.Step();
            Require(!moved.PendingEvents.Any(e => e.Type == "projectile_spawn") &&
                moved.PendingEvents.Any(e => e.Type == "attack_cancel"), "invalid target cancels release without ghost arrow");
        }
    }

    private void ValidateDummies()
    {
        using var battle = Battle(Session((Static, 0, 2, 2), (Static, 1, 7, 2)));
        var positions = battle.Units.Select(unit => unit.Position).ToArray();
        for (var i = 0; i < 40; i++) battle.Step();
        Require(battle.Units.Select(unit => unit.Position).SequenceEqual(positions) &&
            battle.Units.All(unit => unit.Health == unit.MaxHealth) && !battle.PendingEvents.Any(e => e.Type == "attack"), "static dummies remain inert");
    }

    private void ValidateMeleeContact()
    {
        foreach (var opponent in new[] { Melee, "enemy_cutpurse", "enemy_boreal_boss", "enemy_shadow_boss" })
        foreach (var diagonal in new[] { false, true })
        {
            using var battle = Battle(Session((Guard, 0, 3, 2), (opponent, 1, 5, diagonal ? 4 : 2)), noAbilities: true);
            var attackers = new HashSet<string>();
            for (int tick = 0; tick < 80 && attackers.Count < 2; tick++)
            {
                battle.DrainEvents();
                var positions = battle.Units.ToDictionary(unit => unit.RuntimeId, unit => unit.Position);
                battle.Step();
                foreach (var hit in battle.DrainEvents().Where(fact => fact.Type == "attack"))
                {
                    var separation = positions[hit.SourceRuntimeId].DistanceTo(positions[hit.TargetRuntimeId]);
                    Require(separation <= 1.01f, $"melee must approach within one cell pitch before swinging: {opponent}, distance={separation}");
                    attackers.Add(hit.SourceRuntimeId);
                }
                var first = battle.Units[0]; var second = battle.Units[1];
                Require(first.Position.DistanceTo(second.Position) + .001f >= first.BodyRadius + second.BodyRadius,
                    "short reach must not make melee bodies overlap");
            }
            Require(attackers.Count == 2, "both melee units can reach and attack without stalling");
        }
    }

    private void ValidateSkillHitAndRollback()
    {
        var session = Session((Crossbow, 0, 2, 2), (Melee, 1, 5, 2));
        var config = new BattleLabPreparationAdapter(_index).Build(session.Freeze());
        var tankConfig = new BattleLabPreparationAdapter(_index).Build(Session((Guard, 0, 2, 2), (Melee, 1, 5, 2)).Freeze());
        config.Spawns[1] = config.Spawns[1] with { Unit = tankConfig.Spawns[0].Unit };
        using (var battle = new BattleSimulation(config))
        {
            var archer = battle.Units.Single(unit => unit.Team == 0);
            var tank = battle.Units.Single(unit => unit.Team == 1);
            archer.CurrentMana = archer.MaxMana;
            archer.AttackCooldown = tank.AttackCooldown = 1000;
            tank.CurrentMana = 0;
            for (var i = 0; i < 22; i++) battle.Step();
            Require(archer.Health == archer.MaxHealth && tank.Health < tank.MaxHealth, "skill projectiles never trigger ordinary-attack reflection");
            Require(archer.AttackHitStacks == 3, "one growth event per skill impact");
        }
        var rollbackConfig = new BattleConfig
        {
            Seed = config.Seed, FloorRule = config.FloorRule, HeroRule = config.HeroRule, Spawns = config.Spawns,
            ConfigureCombatBindings = bindings => bindings.Subscribe(BattleCombatEventKind.AbilityResolved,
                CombatSourceRef.System("sequence-rollback-probe"), 0, (fact, sink) =>
                {
                    if (fact.SubjectStableId != "ability_hc01_volley") return;
                    sink.Enqueue(CombatSourceRef.System("sequence-rollback-probe"), 0,
                        _ => throw new InvalidOperationException("injected sequence commit failure"));
                })
        };
        using var failed = new BattleSimulation(rollbackConfig);
        var caster = failed.Units.Single(unit => unit.Team == 0);
        caster.CurrentMana = caster.MaxMana;
        caster.AttackCooldown = 1000;
        failed.Step();
        Require(caster.CurrentMana == caster.MaxMana && caster.ProjectileSequence is null && caster.ProjectileWindups.IsEmpty && failed.Projectiles.IsEmpty,
            "failed cast restores mana, sequence and projectile state");
    }

    private static void Near(float actual, float expected, string message) => Require(Math.Abs(actual - expected) < .01f, $"{message}: {actual} != {expected}");
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private sealed class MemorySave : IRunSaveService
    {
        public MetaProgressDto LoadMeta() => new();
        public SettingsDto LoadSettings() => new();
        public ActiveRunDto? LoadActiveRun() => null;
        public bool SaveMeta(MetaProgressDto value) => true;
        public bool SaveSettings(SettingsDto value) => true;
        public bool SaveActiveRun(ActiveRunDto value) => true;
        public void DeleteActiveRun() { }
    }
}
