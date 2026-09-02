using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.Domain;
using TowerAutobattler.Equipment;
using TowerAutobattler.Presentation;

public partial class BattleFloatingCueContractSmoke : Node
{
    public override async void _Ready()
    {
        var code = await RunAsync();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        GetTree().Quit(code);
    }

    private async Task<int> RunAsync()
    {
        BattleScreenController? screen = null;
        try
        {
            var publication = await TestProjectFixture.PublishAsync(this);
            var content = publication.Package?.Content ?? throw new InvalidOperationException(
                "production content publication failed: " + string.Join(" | ", publication.Report.CoreErrors));
            var template = GD.Load<PackedScene>("res://scenes/ui/components/BattleFloatingCue.tscn") ??
                           throw new InvalidOperationException("authored Battle floating-cue template is missing");
            var templateRoot = template.Instantiate<Label>();
            try
            {
                if (templateRoot.MouseFilter != Control.MouseFilterEnum.Ignore)
                    throw new InvalidOperationException("Battle floating-cue template intercepts pointer input");
            }
            finally { templateRoot.Free(); }

            screen = GD.Load<PackedScene>("res://scenes/ui/BattleScreen.tscn")
                .Instantiate<BattleScreenController>();
            screen.Theme = GD.Load<Theme>("res://content/ui/RealmTheme.tres");
            AddChild(screen);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            var overlay = screen.GetNodeOrNull<Control>("%FloatingCueOverlay") ??
                          throw new InvalidOperationException("BattleScreen has no authored floating-cue overlay");
            if (overlay.MouseFilter != Control.MouseFilterEnum.Ignore || overlay.GetParent() is Container)
                throw new InvalidOperationException(
                    "Battle floating-cue overlay is interactive or owned by a position-overriding Container");

            screen.StartBattle(content, CueConfig(content), "浮动战斗提示契约");
            screen.SetProcess(false);
            for (var tick = 0; tick < 3; tick++) screen._Process(.16);

            var labels = overlay.GetChildren().OfType<Label>().ToArray();
            var variations = labels.Select(label => label.ThemeTypeVariation.ToString())
                .ToHashSet(StringComparer.Ordinal);
            if (!variations.Contains("FloatingDamageLabel") ||
                !variations.Contains("FloatingHealingLabel") ||
                !variations.Contains("FloatingStatusActiveLabel") ||
                !variations.Contains("FloatingStatusStackLabel") ||
                !variations.Contains("FloatingStatusRemovedLabel"))
                throw new InvalidOperationException(
                    "production BattleScreen did not render damage/heal and active/stack/removed Status cues: " +
                    string.Join(',', variations.Order(StringComparer.Ordinal)));
            if (!labels.Any(label => label.Text.StartsWith("-", StringComparison.Ordinal)) ||
                !labels.Any(label => label.Text.StartsWith("+", StringComparison.Ordinal)) ||
                !labels.Any(label => label.Text.Contains("生效", StringComparison.Ordinal)) ||
                !labels.Any(label => label.Text.Contains('×')) ||
                !labels.Any(label => label.Text.Contains("消退", StringComparison.Ordinal)))
                throw new InvalidOperationException("floating cue text lacks numeric signs or non-color lifecycle wording");
            var maximum = CueMetric(screen, "MaximumFloatingCueCount");
            if (maximum <= 0 || overlay.GetChildCount() > maximum ||
                CueMetric(screen, "ActiveFloatingCueCount") != overlay.GetChildCount() ||
                CueMetric(screen, "ActiveFloatingTweenCount") != overlay.GetChildCount())
                throw new InvalidOperationException("floating cue node/Tween ownership is unbounded or inconsistent");

            var board = screen.GetNode<BattleBoard>("%BattleBoard");
            var targetPosition = board.GlobalPosition + board.CellToLocal(new Vector2I(3, 2));
            GetViewport().PushInput(new InputEventMouseButton
            {
                ButtonIndex = MouseButton.Left,
                Pressed = true,
                Position = targetPosition,
                GlobalPosition = targetPosition
            }, true);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            var selected = screen.GetNode<Control>("%SelectedUnitPanel");
            var selectedText = selected.GetNode<Label>("Layout/UnitAction").Text;
            if (!selected.Visible || !selectedText.Contains("冻结", StringComparison.Ordinal) ||
                !selectedText.Contains("秒", StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "floating-cue overlay blocked real selection or replaced exact selected-unit Status details");

            screen.StartBattle(content, DensityCueConfig(content), "密度提示上限");
            screen.SetProcess(false);
            screen._Process(.16);
            screen._Process(.16);
            maximum = CueMetric(screen, "MaximumFloatingCueCount");
            if (overlay.GetChildCount() != maximum ||
                CueMetric(screen, "ActiveFloatingCueCount") != maximum ||
                CueMetric(screen, "ActiveFloatingTweenCount") != maximum)
                throw new InvalidOperationException(
                    $"full-board Battle did not exercise or enforce the floating cue node/Tween cap: " +
                    $"children={overlay.GetChildCount()}, active={CueMetric(screen, "ActiveFloatingCueCount")}, " +
                    $"tweens={CueMetric(screen, "ActiveFloatingTweenCount")}, maximum={maximum}");

            screen.StartBattle(content, QuietConfig(), "替换清理");
            screen.SetProcess(false);
            AssertCueCleanup(screen, overlay, "replacement");

            screen.StartBattle(content, TerminalConfig(), "终局清理");
            screen.SetProcess(false);
            screen._Process(.16);
            if (!screen.IsEnding) throw new InvalidOperationException("terminal cue fixture did not resolve");
            AssertCueCleanup(screen, overlay, "battle completion");

            screen.StartBattle(content, CueConfig(content), "退树清理");
            screen.SetProcess(false);
            screen._Process(.16);
            if (overlay.GetChildCount() == 0)
                throw new InvalidOperationException("exit-tree cue fixture did not create an active cue");
            RemoveChild(screen);
            AssertCueCleanup(screen, overlay, "exit tree");
            screen.Free();
            screen = null;

            GD.Print("BATTLE_FLOATING_CUE_CONTRACT_OK facts=damage,heal status=active,stack,removed " +
                     "input=overlay-ignore-selected-detail density=bounded lifecycle=replacement,terminal,exit-zero");
            return 0;
        }
        catch (Exception exception)
        {
            GD.PrintErr("BATTLE_FLOATING_CUE_CONTRACT_FAILED: " + exception);
            return 1;
        }
        finally
        {
            if (screen is not null)
            {
                if (screen.GetParent() is not null) screen.GetParent().RemoveChild(screen);
                screen.Free();
            }
        }
    }

    private static BattleConfig DensityCueConfig(ContentRegistry content)
    {
        if (!content.Graph.TryGetEquipment("equipment_rimebrand", out var equipment))
            throw new InvalidOperationException("production Frost Equipment is missing for density cues");
        var spawns = new List<BattleSpawn>(BattlefieldLayout.Width * BattlefieldLayout.Height);
        var equipmentInstances = ImmutableArray.CreateBuilder<EquipmentBattleInstanceSnapshot>(18);
        for (var index = 0; index < 18; index++)
        {
            var ownerId = $"density_cue_player_{index:D2}";
            spawns.Add(new BattleSpawn(
                Unit("density_cue_player", "密度英雄", true, 1_000, 2, 10),
                0,
                BattlefieldLayout.PlayerDeploymentCells[index],
                ownerId,
                IsPersistentRosterHero: true));
            equipmentInstances.Add(new EquipmentBattleInstanceSnapshot(
                $"density_cue_rime_{index:D2}", equipment.StableId, ownerId, 0, equipment));
        }
        var temporaryIndex = 0;
        for (var y = 0; y < BattlefieldLayout.Height; y++)
        for (var x = BattlefieldLayout.PlayerDeploymentColumns; x < BattlefieldLayout.Width - 2; x++)
        {
            var team = x <= 5 ? 0 : 1;
            spawns.Add(new BattleSpawn(
                Unit("density_cue_temporary", "密度临时单位", false, 1_000, 2, 10),
                team,
                new Vector2I(x, y),
                $"density_cue_temporary_{temporaryIndex++:D2}",
                IsTemporary: true,
                IsPersistentRosterHero: false));
        }
        var enemyIndex = 0;
        for (var y = 0; y < BattlefieldLayout.Height; y++)
        for (var x = BattlefieldLayout.Width - 2; x < BattlefieldLayout.Width; x++)
            spawns.Add(new BattleSpawn(
                Unit("density_cue_enemy", "密度敌人", false, 1_000, 2, 10),
                1,
                new Vector2I(x, y),
                $"density_cue_enemy_{enemyIndex++:D2}"));
        if (spawns.Count != BattlefieldLayout.Width * BattlefieldLayout.Height ||
            spawns.Select(spawn => spawn.Cell).Distinct().Count() != spawns.Count)
            throw new InvalidOperationException("floating cue density fixture does not fill the board exactly once");
        var snapshots = equipmentInstances.MoveToImmutable();
        return new BattleConfig
        {
            Seed = 0xC0E8UL,
            FloorRule = new ClearFloorRuleRuntime("floating_cue_density", "常规", "密度提示"),
            HeroRule = Rule(),
            Equipment = new EquipmentBattlePreparation(
                EquipmentStateFingerprint.Compute(snapshots),
                snapshots),
            Spawns = spawns
        };
    }

    private static BattleConfig CueConfig(ContentRegistry content)
    {
        if (!content.Graph.TryGetEquipment("equipment_rimebrand", out var equipment))
            throw new InvalidOperationException("production Frost Equipment is missing");
        var equipmentInstance = new EquipmentBattleInstanceSnapshot(
            "cue-rime",
            equipment.StableId,
            "a_cue_owner",
            0,
            equipment);
        return new BattleConfig
        {
            Seed = 0xC0E5UL,
            Identity = new BattleIdentity("floating_cue_contract", TowerNodeType.Combat, 0xC0E5UL, 5, 1),
            FloorRule = new ClearFloorRuleRuntime("floating_cue", "常规", "浮动提示"),
            HeroRule = Rule(),
            Equipment = new EquipmentBattlePreparation(
                EquipmentStateFingerprint.Compute([equipmentInstance]),
                [equipmentInstance]),
            Spawns =
            [
                new BattleSpawn(Unit("hero_banner_marshal", "霜痕持有者", true, 300, 10, 10), 0,
                    new Vector2I(1, 2), "a_cue_owner", IsPersistentRosterHero: true),
                new BattleSpawn(Unit("hero_hour_arbiter", "治疗者", true, 300, 0, 10, 12), 0,
                    new Vector2I(0, 4), "b_cue_healer", IsPersistentRosterHero: true),
                new BattleSpawn(Unit("soldier_aegis_guard", "负伤友军", false, 300, 0, 10), 0,
                    new Vector2I(1, 4), "c_cue_wounded", .5f, IsPersistentRosterHero: true),
                new BattleSpawn(Unit("enemy_crossbow", "状态目标", false, 1_000, 0, 10), 1,
                    new Vector2I(3, 2), "z_cue_target"),
                new BattleSpawn(Unit("enemy_rust_guard", "后备目标", false, 1_000, 0, 1), 1,
                    new Vector2I(9, 5), "zz_cue_reserve")
            ]
        };
    }

    private static BattleConfig QuietConfig() => new()
    {
        Seed = 0xC0E6UL,
        FloorRule = new ClearFloorRuleRuntime("floating_cue_quiet", "常规", "替换清理"),
        HeroRule = Rule(),
        Spawns =
        [
            new BattleSpawn(Unit("hero_banner_marshal", "安静英雄", true, 1_000, 0, .5f), 0,
                Vector2I.Zero, "quiet_player", IsPersistentRosterHero: true),
            new BattleSpawn(Unit("enemy_rust_guard", "安静敌人", false, 1_000, 0, .5f), 1,
                new Vector2I(9, 5), "quiet_enemy")
        ]
    };

    private static BattleConfig TerminalConfig() => new()
    {
        Seed = 0xC0E7UL,
        FloorRule = new ClearFloorRuleRuntime("floating_cue_terminal", "常规", "终局清理"),
        HeroRule = Rule(),
        Spawns =
        [
            new BattleSpawn(Unit("hero_banner_marshal", "终结者", true, 100, 100, 10), 0,
                new Vector2I(1, 2), "terminal_player", IsPersistentRosterHero: true),
            new BattleSpawn(Unit("enemy_rust_guard", "终局敌人", false, 1, 0, 1), 1,
                new Vector2I(2, 2), "terminal_enemy")
        ]
    };

    private static UnitSnapshot Unit(
        string contentId,
        string displayName,
        bool hero,
        float health,
        float damage,
        float range,
        float healing = 0)
    {
        var attributes = AttributeDefinitionCompiler.Legacy(new Dictionary<CombatAttribute, float>
        {
            [CombatAttribute.MaxHealth] = health,
            [CombatAttribute.AttackDamage] = damage,
            [CombatAttribute.SpellPower] = 0,
            [CombatAttribute.AttackSpeed] = 1,
            [CombatAttribute.Armor] = 0,
            [CombatAttribute.MagicResistance] = 0,
            [CombatAttribute.AttackRange] = range,
            [CombatAttribute.MoveSpeed] = 1,
            [CombatAttribute.CriticalChance] = 0,
            [CombatAttribute.CriticalDamage] = 1.5f,
            [CombatAttribute.MaxMana] = 0,
            [CombatAttribute.StartingMana] = 0,
            [CombatAttribute.HealingPower] = healing,
            [CombatAttribute.LifeSteal] = 0,
            [CombatAttribute.ControlResistance] = 0
        });
        return new UnitSnapshot(
            contentId,
            displayName,
            UnitRole.Fighter,
            hero,
            false,
            health,
            damage,
            range,
            1,
            1,
            0,
            healing,
            0,
            0,
            Array.Empty<string>(),
            new UnitBehaviorSnapshot(),
            AttributeDefinition: attributes,
            TraitContributions: ImmutableArray<TowerAutobattler.Traits.CompiledTraitContribution>.Empty);
    }

    private static HeroRuleSnapshot Rule() => new(
        1, 1, 1, 0, 0, 0, false,
        string.Empty, 1, 1, 0, 0, 0, 0,
        false, false, 0, 0, string.Empty);

    private static int CueMetric(BattleScreenController screen, string property)
    {
        var value = typeof(BattleScreenController).GetProperty(
                property,
                BindingFlags.Instance | BindingFlags.Public)?.GetValue(screen) ??
            throw new InvalidOperationException("BattleScreen omitted cue metric: " + property);
        return Convert.ToInt32(value);
    }

    private static void AssertCueCleanup(BattleScreenController screen, Control overlay, string label)
    {
        if (overlay.GetChildCount() != 0 || CueMetric(screen, "ActiveFloatingCueCount") != 0 ||
            CueMetric(screen, "ActiveFloatingTweenCount") != 0)
            throw new InvalidOperationException(label + " retained floating cue nodes or Tweens");
    }
}
