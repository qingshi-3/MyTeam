using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.Composition;
using TowerAutobattler.Content;
using TowerAutobattler.Project;
using TowerAutobattler.UI;

// Real Control input against independent authored consumers; no Run or player persistence.
public partial class UnitInformationInputSmoke : Control
{
    private string _step = "publication";
    public override async void _Ready()
    {
        var code = 0;
        try
        {
            Require(DisplayServer.GetName() != "headless", "rendered validation required");
            GetWindow().Mode = Window.ModeEnum.Windowed;
            GetWindow().Size = new Vector2I(1600, 900);
            var gate = await GamePackagePublisher.CreateReadyAsync(this,
                GD.Load<GameProjectDefinition>("res://content/project/alpha_project.tres"));
            var package = gate.Package ?? throw new InvalidOperationException(string.Join(';', gate.Report.CoreErrors));
            Require(package.Content.TryGet("hero_hc01_crossbow", out var entry), "published hero available");
            var definition = (UnitDefinition)entry.Definition;
            var snapshot = BattleSetupFactory.Snapshot(entry, package.Content);
            var attributes = snapshot.AttributeDefinition!.Attributes.ToDictionary(value => value.Attribute, value => value.BaseValue);
            var baseDamage = attributes[CombatAttribute.AttackDamage];
            attributes[CombatAttribute.AttackDamage] = baseDamage + 17;
            attributes[CombatAttribute.MaxHealth] += 100;
            attributes[CombatAttribute.MaxMana] = 80;
            attributes[CombatAttribute.StartingMana] = 25;
            var effective = snapshot with { AttackHitGrowth = new AttackHitGrowthSnapshot(.03f, false, true) };
            var prepared = new PreparedUnitDetails(effective, attributes, 73, 12);
            var model = new UnitInformation("prepared", definition, snapshot, prepared);
            var compact = GetNode<UnitInfoCard>("Margin/Columns/Compact/Information");
            var opening = GetNode<HeroDetailPanel>("Margin/Columns/Opening");
            var host = GetNode<PreparedUnitDetailPanel>("Margin/Columns/Prepared");
            compact.Bind(model, "出战 · 2 阶");
            opening.Bind(new(definition.Id, definition, true, "", "", snapshot));
            host.Bind("prepared", definition, snapshot, model.Context, prepared);
            var detail = host.GetNode<UnitDetailView>("%UnitDetails");
            var baseline = opening.GetNode<UnitDetailView>("%UnitDetails");
            attributes[CombatAttribute.AttackDamage] = 9999;
            Require(model.Read(CombatAttribute.AttackDamage) == baseDamage + 17, "presentation copies mutable preparation input");
            await Frames(4);
            _step = "base and prepared facts";
            Require(Damage(compact).GetNode<Label>("%FactValue").Text == $"{baseDamage + 17:0.#}" &&
                Damage(detail).GetNode<Label>("%FactValue").Text == $"{baseDamage + 17:0.#}", "compact and detail show the same prepared damage");
            Require(Damage(baseline).GetNode<Label>("%FactValue").Text == $"{baseDamage:0.#}", "opening keeps base damage");
            Require(compact.GetNode<Label>("%AttributeContext").Text.Contains("准备") &&
                baseline.GetNode<Label>("%Context").Text.Contains("基础"), "value contexts stay explicit");
            var vitals = detail.GetNode<UnitVitals>("%Vitals");
            Require(vitals.GetNode<ProgressBar>("%HealthGauge").Value == 73 &&
                vitals.GetNode<ProgressBar>("%ManaGauge").Value == 25 &&
                vitals.GetNode<Label>("%HealthValue").Text.Contains("+盾 12"), "actual preparation health, mana and shield are preserved");
            Require(model.Skills.Count(skill => skill.Name == "连续命中成长") == 1 &&
                model.Skills.Single(skill => skill.Name == "连续命中成长").Body.Contains("换目标保层"), "component skill uses effective snapshot and is not duplicated");
            await Capture("shared-views");

            _step = "compact keyboard details";
            var selections = 0;
            compact.Activated += () => selections++;
            await Click(Damage(compact));
            Require(selections == 1, "compact fact delegates one selection to its host");
            await PressKey(Key.Tab);
            var focused = GetViewport().GuiGetFocusOwner();
            Require(focused is CompactDetailFact && compact.IsAncestorOf(focused), "Tab follows compact attribute facts");
            await ToSignal(GetTree().CreateTimer(.6), SceneTreeTimer.SignalName.Timeout);
            await Frames(2);
            var hint = focused!.GetNode<BattleLabHoverHint>("BattleLabHoverHint").GetChildren().OfType<BattleLabTooltip>().SingleOrDefault();
            Require(hint is { Visible: true } && hint.GetNode<CombatRichText>("%Stats").Text.Length > 0,
                "compact keyboard focus exposes the complete attribute explanation");

            _step = "mouse and keyboard explanations";
            var damage = Damage(detail);
            await Click(damage);
            Require(detail.GetNode<Control>("%Explanation").Visible &&
                detail.GetNode<CombatRichText>("%ExplanationText").Text == damage.ExplanationText, "mouse activates shared attribute explanation");
            Require(damage.ExplanationText.Contains("+17"), "explanation keeps net preparation delta");
            var close = detail.GetNode<Button>("%CloseExplanation");
            await Click(close);
            Require(damage.HasFocus(), "close returns focus to fact inside the shared scene");
            await PressKey(Key.Enter);
            Require(detail.GetNode<Control>("%Explanation").Visible, "keyboard activation opens the same explanation");
            Require(!baseline.GetNode<Control>("%Explanation").Visible, "independent detail instances do not share reading state");
            await Click(close);
            var skill = detail.GetNode<Control>("%SkillList").GetChildren().OfType<UnitSkillSummary>().First();
            await Click(skill.GetNode<Button>("%SkillHeader"));
            Require(detail.GetNode<Label>("%ExplanationTitle").Text.Length > 0 &&
                detail.GetNode<Control>("%Explanation").Visible, "skill explanation travels through the shared view");
            await Capture("explanation");

            _step = "host action remains separate";
            var actions = new List<string>();
            opening.DeployRequested += actions.Add;
            await Click(opening.GetNode<Button>("%DeployButton"));
            Require(actions.SequenceEqual(new[] { definition.Id }), "opening host submits exactly one selected identity");
            opening.Bind(new(definition.Id, definition, false, "", "", snapshot));
            await Click(opening.GetNode<Button>("%DeployButton"));
            Require(actions.Count == 1, "locked hero does not submit");

            _step = "rebind and independent lifecycle";
            var noMana = new Dictionary<CombatAttribute, float> { [CombatAttribute.MaxMana] = 0, [CombatAttribute.StartingMana] = 0 };
            var noSkills = snapshot with { AbilityLoadout = null, AttackHitGrowth = null };
            var other = new UnitInformation("different", definition, snapshot, new PreparedUnitDetails(noSkills, noMana, 10, 0));
            compact.Bind(other, "后备 · 1 阶");
            detail.Bind(other);
            await Frames(3);
            Require(!compact.GetNode<UnitVitals>("%Vitals").GetNode<Control>("%ManaFact").Visible &&
                !detail.GetNode<UnitVitals>("%Vitals").GetNode<Control>("%ManaFact").Visible, "no-mana rebind hides old mana in both layouts");
            Require(compact.GetNode<Button>("%Active").Text.EndsWith("无") && compact.GetNode<Button>("%Passive").Text.EndsWith("无"), "rebind clears old compact skills");
            Require(!detail.GetNode<Control>("%Explanation").Visible && detail.GetNode<FoldableContainer>("%SecondaryAttributes").Folded &&
                detail.GetNode<ScrollContainer>("%DetailsScroll").ScrollVertical == 0, "new identity resets detail reading state");
            Require(baseline.GetNode<UnitVitals>("%Vitals").GetNode<Control>("%ManaFact").Visible, "another instance retains its mana");
            compact.QueueFree(); host.QueueFree(); opening.QueueFree();
            await Frames(3);
            GD.Print("UNIT_INFORMATION_INPUT_OK base-prepared-copy skills health-mana-shield mouse keyboard focus independent-state host-action rebind teardown");
        }
        catch (Exception error)
        {
            code = 1;
            GD.PrintErr($"UNIT_INFORMATION_INPUT_FAILED step={_step}: {error}");
            if (DisplayServer.GetName() != "headless") await Capture("failure");
        }
        GetTree().Quit(code);
    }

    private static CompactDetailFact Damage(Node view) => view.GetNode<UnitCoreStats>("%CoreStats").GetNode<CompactDetailFact>("%DamageFact");
    private async Task Click(Control target)
    {
        for (var parent = target.GetParent(); parent is not null; parent = parent.GetParent())
        {
            if (parent is not ScrollContainer scroll) continue;
            for (var attempt = 0; attempt < 80 && !scroll.GetGlobalRect().Encloses(target.GetGlobalRect()); attempt++)
            {
                var point = scroll.GetGlobalRect().GetCenter();
                var button = target.GlobalPosition.Y < scroll.GlobalPosition.Y ? MouseButton.WheelUp : MouseButton.WheelDown;
                Input.WarpMouse(point);
                Input.ParseInputEvent(new InputEventMouseMotion { Position = point, GlobalPosition = point });
                Input.ParseInputEvent(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = button, Pressed = true });
                Input.ParseInputEvent(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = button, Pressed = false });
                await Frames(2);
            }
        }
        var at = target.GetGlobalRect().GetCenter();
        Input.WarpMouse(at);
        Input.ParseInputEvent(new InputEventMouseMotion { Position = at, GlobalPosition = at });
        Input.ParseInputEvent(new InputEventMouseButton { Position = at, GlobalPosition = at, ButtonIndex = MouseButton.Left, ButtonMask = MouseButtonMask.Left, Pressed = true });
        Input.ParseInputEvent(new InputEventMouseButton { Position = at, GlobalPosition = at, ButtonIndex = MouseButton.Left, Pressed = false });
        await Frames(3);
    }
    private async Task PressKey(Key key)
    {
        Input.ParseInputEvent(new InputEventKey { Keycode = key, Pressed = true });
        Input.ParseInputEvent(new InputEventKey { Keycode = key, Pressed = false });
        await Frames(3);
    }
    private async Task Frames(int count)
    {
        for (var i = 0; i < count; i++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        }
    }
    private async Task Capture(string name)
    {
        await Frames(2);
        DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath("res://.godot/unit-information"));
        GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath($"res://.godot/unit-information/{name}.png"));
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
