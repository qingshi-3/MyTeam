using System;
using System.Linq;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.BattleLab;
using TowerAutobattler.Components;
using TowerAutobattler.Composition;
using TowerAutobattler.Content;
using TowerAutobattler.Presentation;
using TowerAutobattler.Project;
using TowerAutobattler.UI;

// Real runtime projections and authored scene bindings, without graphical play or save writes.
public partial class SkillProgressContractSmoke : Node
{
    private ContentRegistry _content = null!;
    private BattleLabContentIndex _index = null!;
    private HealthViewComponent _view = null!;
    private SelectedUnitPanel _panel = null!;

    public override async void _Ready()
    {
        try
        {
            var gate = await GamePackagePublisher.CreateReadyAsync(this,
                GD.Load<GameProjectDefinition>("res://content/project/alpha_project.tres"));
            var package = gate.Package ?? throw new InvalidOperationException(string.Join(';', gate.Report.CoreErrors));
            _content = package.Content;
            _index = new(package);
            _view = GD.Load<PackedScene>("res://scenes/components/HealthViewComponent.tscn").Instantiate<HealthViewComponent>();
            _panel = GD.Load<PackedScene>("res://scenes/ui/components/SelectedUnitPanel.tscn").Instantiate<SelectedUnitPanel>();
            AddChild(_view); AddChild(_panel);
            CheckCatalog(); CheckMana(); CheckGrit(); CheckPeriodic(); CheckBrood(); CheckPhaseReplacement(); CheckLimitedTrigger();
            _panel.Free(); _view.Free();
            GD.Print("SKILL_PROGRESS_OK all-published-both-teams; real-mana/grit/cadence; queued-critical; expiry; brood/phase; uses; read-purity; authored-HUD/detail-bindings; lifecycle");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PrintErr("SKILL_PROGRESS_FAILED " + error); GetTree().Quit(1); }
    }

    private void CheckCatalog()
    {
        var adapter = new BattleLabPreparationAdapter(_index);
        var withSkills = 0;
        foreach (var published in _index.Units)
        {
            var session = new BattleLabSession(_index, 1);
            Require(session.AddAndPlace(published.StableId, BattleLabSide.Player, new(2, 2)).Succeeded, "place A");
            Require(session.AddAndPlace(published.StableId, BattleLabSide.Enemy, new(7, 2)).Succeeded, "place B");
            var config = adapter.Build(session.Freeze());
            using var battle = new BattleSimulation(config);
            foreach (var unit in battle.Units)
            {
                var skills = battle.ReadUnitSkills(unit.RuntimeId);
                var expected = config.ResolveBossTimeline(unit.Definition.ContentId)?.Phases[0].AbilityLoadout?.Abilities
                    ?? unit.Definition.AbilityLoadout?.Abilities ?? [];
                Require(skills.Abilities.Select(a => a.StableId).ToHashSet().SetEquals(expected.Select(a => a.StableId)),
                    "runtime phase loadout visible: " + published.StableId);
                foreach (var ability in expected)
                    Require(skills.Progress.Any(p => p.AbilityId == ability.StableId), "every skill projects: " + ability.StableId);
                if (published.Definition.IsTestDummy) Require(skills.Primary is null, "no fake resource on empty dummy");
                if (skills.Primary is not null) withSkills++;
                Bind(battle, unit);
                Require(_view.ManaBar.Visible == (skills.Primary is not null), "overhead visibility: " + published.StableId);
                Require(_panel.GetNode<ProgressBar>("%ManaBar").Visible == _view.ManaBar.Visible, "details agree with overhead");
                if (skills.Primary is { } primary)
                {
                    Require(_view.ManaBar.Value == primary.Current && _view.ManaBar.MaxValue == primary.Maximum, "exact bound values");
                    Require(_view.ResourceMarker.Visible && _view.ResourceMarker.Text.Length > 0, "noncolor resource identity");
                    Require(_view.ManaBar.GetThemeStylebox("fill") ==
                        _panel.GetNode<ProgressBar>("%ManaBar").GetThemeStylebox("fill"), "shared authored style");
                }
            }
        }
        GD.Print($"SKILL_PROGRESS_CATALOG units={_index.Units.Length} skill-bearing-instances={withSkills}");
    }

    private void CheckMana()
    {
        using var battle = new BattleSimulation(Config(Profile("enemy_ee03_trample_brute")));
        var owner = Caster(battle);
        owner.CurrentMana = 0;
        Bind(battle, owner);
        Require(_view.ManaBar.Visible && _view.ManaBar.Value == 0, "empty mana stays visible");
        owner.CurrentMana = owner.MaxMana; owner.DisabledTicks = 3;
        Step(battle, 2);
        Require(Primary(battle).State == SkillProgressState.Ready && Primary(battle).Current == owner.MaxMana,
            "full controlled mana stays ready");
        owner.DisabledTicks = 0; Step(battle, 1);
        Require(Primary(battle).State == SkillProgressState.Casting && Primary(battle).Current == 0, "real cast consumes mana");
        Bind(battle, owner);
        Require(_panel.GetNode<Button>("%UnitAbility").Visible, "enemy active skill shown");
        var snapshot = battle.ReadUnitSkills(owner.RuntimeId);
        battle.Dispose();
        Require(Primary(battle).State == SkillProgressState.Ended && Primary(battle).Current == snapshot.Primary!.Current,
            "terminal projection freezes before resource cleanup");
    }

    private void CheckGrit()
    {
        var profile = Profile("hero_hc38_grit_brawler");
        using (var battle = new BattleSimulation(Config(profile, new Probe(context =>
               {
                   if (context.Tick == 1) context.Damage("target", context.Units.Single(u => u.RuntimeId == "owner"), 250);
               }))))
        {
            var owner = Caster(battle); Normalize(owner); owner.DisabledTicks = 200;
            Require(Primary(battle).Kind == SkillResourceKind.Grit && Primary(battle).Current == 0, "empty grit is primary without mana");
            Step(battle, 2);
            Require(Primary(battle).Current == 250 && Primary(battle).Maximum == 500, "actual damage fills capped grit");
            Bind(battle, owner);
            Require(_panel.GetNode<Button>("%UnitAbility").Visible && _view.ResourceMarker.Text == "怒", "conditional punch is active in detail and grit in HUD");
            var current = Primary(battle);
            for (var i = 0; i < 10; i++) Require(Primary(battle) == current, "repeated reads do not advance grit");
            Step(battle, 65);
            Require(Primary(battle).Current == 0, "expired damage disappears from actual gauge");
        }
        using (var battle = new BattleSimulation(Config(profile)))
        {
            var owner = Caster(battle); Normalize(owner);
            owner.Health = 499; Step(battle, 1);
            Require(Primary(battle).State == SkillProgressState.Casting, "half-health cast visible without mana");
            owner.Health = 199; Step(battle, 1);
            Require(Primary(battle).State == SkillProgressState.Casting && Primary(battle).Detail.Contains("已排队"), "critical request waits for current action");
            Step(battle, 12);
            Require(Primary(battle).Detail.Contains("濒死机会已用"), "once-only entitlement is actual runtime state");
            owner.Health = 0; Bind(battle, owner);
            Require(!_view.ManaBar.Visible && !_view.ResourceMarker.Visible && !_view.ReadyMarker.Visible, "defeated overhead cleared");
            owner.Health = 300; Bind(battle, owner);
            Require(_view.ManaBar.Visible && Primary(battle).Detail.Contains("濒死机会已用"), "restored identity keeps used entitlement");
        }
    }

    private void CheckPeriodic()
    {
        var profile = Profile("enemy_es01_piercing_crossbow");
        var authored = profile.AbilityLoadout!.Abilities.Single();
        var line = authored.Operations.OfType<CompiledChargedLineOperation>().Single() with { ChargeTicks = 1 };
        var ability = authored with { IntervalTicks = 6, CooldownTicks = 10, Operations = [line] };
        profile = profile with { AbilityLoadout = new([ability]) };
        using var observed = new BattleSimulation(Config(profile));
        using var untouched = new BattleSimulation(Config(profile));
        Require(Primary(observed).Current == 0 && Primary(observed).Maximum == 6, "first interval starts empty");
        for (var tick = 1; tick <= 18; tick++)
        {
            observed.Step(); untouched.Step();
            for (var read = 0; read < 4; read++) Bind(observed, Caster(observed));
            if (tick == 6) Require(Primary(observed).Current == 0 && Primary(observed).Maximum == 12,
                "cooldown rounded to actual next cadence (tick 18)");
            if (tick == 17) Require(Primary(observed).Current == 11 && Primary(observed).State == SkillProgressState.Building,
                "does not claim ready at hidden cooldown tick 16");
        }
        Require(observed.PendingEvents.Count(e => e.Type == "line_charge") == 2, "casts match projected cadence");
        Require(observed.CreateResult().Digest == untouched.CreateResult().Digest, "HUD/detail reads never mutate simulation");
        using var blocked = new BattleSimulation(Config(profile));
        Caster(blocked).DisabledTicks = 100; Step(blocked, 15);
        Require(Primary(blocked).State == SkillProgressState.Ready && Primary(blocked).Current == Primary(blocked).Maximum,
            "missed cadence retains readiness while controlled");
    }

    private void CheckBrood()
    {
        var session = new BattleLabSession(_index, 3);
        Require(session.AddAndPlace("enemy_eb02_shell_matriarch", BattleLabSide.Player, new(2, 2)).Succeeded, "place timeline mother");
        Require(session.AddAndPlace("soldier_dummy_static", BattleLabSide.Enemy, new(7, 2)).Succeeded, "place brood target");
        using var battle = new BattleSimulation(new BattleLabPreparationAdapter(_index).Build(session.Freeze()));
        var mother = battle.Units.Single(u => u.Definition.ContentId == "enemy_eb02_shell_matriarch");
        Require(mother.Definition.AbilityLoadout is null && battle.ReadUnitSkills(mother.RuntimeId).Abilities.Length > 0,
            "read real phase registration, not stripped direct loadout");
        mother.Health = mother.MaxHealth * .49f; Step(battle, 1);
        Require(battle.ReadUnitSkills(mother.RuntimeId).Primary!.ResourceName == "破壳", "threshold switches real phase progress");
        Step(battle, 24);
        var primary = battle.ReadUnitSkills(mother.RuntimeId).Primary!;
        Require(primary.ResourceName == "产卵" && primary.Detail.Contains("1/"), "real timeline mother spawns and exposes batch progress: " + primary);
        Bind(battle, mother);
        Require(_panel.GetNode<Label>("%ManaText").Text.Contains("产卵"), "phase state bound to details");
    }

    private void CheckPhaseReplacement()
    {
        var profile = Profile("enemy_es01_piercing_crossbow") with { IsBoss = true };
        var first = profile.AbilityLoadout!.Abilities.Single() with { StableId = "test_first", IntervalTicks = 6 };
        var second = first with { StableId = "test_second" };
        var config = Config(profile with { AbilityLoadout = null });
        config = new BattleConfig { Seed = config.Seed, HeroRule = config.HeroRule, FloorRule = config.FloorRule,
            Spawns = config.Spawns, BossTimeline = new("test_phases", profile.ContentId,
                [new("one", "一", 1, new([first])), new("two", "二", .5f, new([second]))]) };
        using var battle = new BattleSimulation(config);
        Step(battle, 8); Caster(battle).Health = Caster(battle).MaxHealth * .4f; Step(battle, 1);
        var read = battle.ReadUnitSkills("owner");
        Require(read.Abilities.Single().StableId == "test_second" && read.Primary!.Current == 0 && read.Primary.Maximum == 3,
            "phase at tick 9 exposes new skill and next cadence at tick 12");
    }

    private void CheckLimitedTrigger()
    {
        var profile = Profile("hero_hc26_ash_returner");
        var ability = profile.AbilityLoadout!.Abilities.First(a => a.Operations.OfType<CompiledLifecycleOperation>()
            .Any(op => op.Kind == LifecycleAbilityKind.ReviveOwner));
        profile = profile with { AbilityLoadout = new([ability with { MaxUses = 1 }]) };
        using var battle = new BattleSimulation(Config(profile, new Probe(context =>
        {
            if (context.Tick == 1) context.Damage("target", context.Units.Single(u => u.RuntimeId == "owner"), 100000);
        })));
        Require(Primary(battle).State == SkillProgressState.Waiting, "limited passive waits on actual death event");
        Step(battle, 2);
        Require(Primary(battle).Detail.Contains("已用 1/1"), "used entitlement persists while defeated");
    }

    private void Bind(BattleSimulation battle, BattleUnitState unit)
    {
        var skills = battle.ReadUnitSkills(unit.RuntimeId);
        _view.SetCombatResources(unit.CurrentMana, unit.MaxMana, unit.Shield, unit.Statuses, unit.Alive, skills.Primary);
        _panel.Bind(BattleRuntimeUnitProjection.Build(unit, _content, null, battle.TraitSnapshot, "", skills));
    }
    private UnitSnapshot Profile(string id)
    {
        Require(_content.TryGet(id, out var entry), "published " + id);
        return BattleSetupFactory.Snapshot(entry, _content) with { Behavior = new(Stationary: true, DisableBasicAttacks: true) };
    }
    private BattleConfig Config(UnitSnapshot owner, IBattleFloorRuleRuntime? floor = null) => new()
    {
        Seed = 213, HeroRule = HeroRuleSnapshot.Neutral, FloorRule = floor ?? new ClearFloorRuleRuntime("clear", "常规", ""),
        Spawns = [new(owner, 0, new(2, 2), "owner"), new(Profile("soldier_dummy_static"), 1, new(4, 2), "target"),
            new(Profile("soldier_dummy_static"), 0, new(1, 5), "ally", IsPersistentRosterHero: true)]
    };
    private static void Normalize(BattleUnitState unit)
    { unit.MaxHealth = unit.Health = 1000; unit.Attributes.SetBaseValue(CombatAttribute.Armor, 0); }
    private static BattleUnitState Caster(BattleSimulation battle) => battle.Units.Single(unit => unit.RuntimeId == "owner");
    private static BattleSkillProgress Primary(BattleSimulation battle) => battle.ReadUnitSkills("owner").Primary!;
    private static void Step(BattleSimulation battle, int ticks) { for (var i = 0; i < ticks; i++) battle.Step(); }
    private sealed class Probe(Action<BattleRuleContext> action) : ClearFloorRuleRuntime("skill_probe", "测试", "")
    { public override void OnTick(BattleRuleContext context) => action(context); }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
