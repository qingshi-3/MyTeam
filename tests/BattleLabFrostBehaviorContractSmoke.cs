using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.BattleLab;
using TowerAutobattler.Content;
using TowerAutobattler.Presentation;
using TowerAutobattler.Statuses;

public partial class BattleLabFrostBehaviorContractSmoke : Node
{
    private const string PresetName = "冰霜体系验证";
    private const string EquipmentId = "equipment_rimebrand";
    private const string TraitId = "trait_winterbound";
    private const string FrostId = "status_frost";
    private const string FreezeId = "status_freeze";
    private const string NormalTargetId = "enemy_scale_brute";
    private const string ResistantTargetId = "enemy_resistance_dummy";

    public override async void _Ready()
    {
        var exitCode = await RunAsync();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        GetTree().Quit(exitCode);
    }

    private async Task<int> RunAsync()
    {
        try
        {
            var gate = await TestProjectFixture.PublishAsync(this);
            var package = gate.Package ?? throw new InvalidOperationException(
                "Battle Lab Frost package: " + string.Join(';', gate.Report.CoreErrors));
            var index = new BattleLabContentIndex(package);
            var catalog = GD.Load<BattleLabPresetCatalog>("res://content/battle-lab/battle_lab_presets.tres") ??
                          throw new InvalidOperationException("Battle Lab preset catalog missing");
            var store = new BattleLabPresetStore(catalog);
            Require(store.TryLoad(PresetName, out var preset), "Frost built-in preset load");
            var snapshot = BattleLabPresetStore.ToSnapshot(preset);
            var session = new BattleLabSession(index, snapshot.CurrentPopulation,
                snapshot.Seed, snapshot.Mode, snapshot.FloorRuleId);
            session.Restore(snapshot);
            var config = new BattleLabPreparationAdapter(index).Build(session.Freeze());

            var frostEquipment = config.Equipment.Instances
                .Where(item => item.ContentId == EquipmentId)
                .OrderBy(item => item.InstanceId, StringComparer.Ordinal)
                .ToArray();
            Require(frostEquipment.Length == 2 &&
                    frostEquipment.Select(item => item.InstanceId).Distinct(StringComparer.Ordinal).Count() == 2 &&
                    frostEquipment.Select(item => item.OwnerHeroInstanceId).Distinct(StringComparer.Ordinal).Count() == 2,
                "Frost preset owns two independent Rimebrand instances");

            var ownerIds = frostEquipment.Select(item => item.OwnerHeroInstanceId).ToHashSet(StringComparer.Ordinal);
            using (var battle = new BattleSimulation(config))
            {
                var trait = battle.TraitSnapshot.Resolve(TraitId, 0);
                Require(trait.Value == 2 && trait.ActiveBreakpoint is not null,
                    "Frost preset activates Winterbound tier");
                foreach (var owner in battle.Units.Where(unit => ownerIds.Contains(unit.SourceInstanceId)))
                {
                    var baseAttackSpeed = owner.Definition.AttributeDefinition?.Find(CombatAttribute.AttackSpeed).BaseValue ?? 1f;
                    Require(owner.Attributes.GetValue(CombatAttribute.AttackSpeed) > baseAttackSpeed,
                        $"Winterbound raises AttackSpeed for {owner.SourceInstanceId}");
                }

                var ownerMomentumObserved = new HashSet<string>(StringComparer.Ordinal);
                var twoSourceFrostObserved = false;
                for (var guard = 0; guard < 3000 && battle.Outcome == BattleOutcome.Running; guard++)
                {
                    battle.Step();
                    foreach (var owner in battle.Units.Where(unit => ownerIds.Contains(unit.SourceInstanceId)))
                        if (owner.Statuses.Any(status => status.StableId == "status_rime_momentum") &&
                            owner.Attributes.GetValue(CombatAttribute.AttackSpeed) > 1.15f)
                            ownerMomentumObserved.Add(owner.SourceInstanceId);
                    twoSourceFrostObserved |= battle.Units.Any(unit => unit.Team == 1 && unit.Statuses.Any(status =>
                        status.StableId == FrostId && status.SourceContributions
                            .Select(source => source.SourceId).Distinct(StringComparer.Ordinal).Count() >= 2));
                }

                var normalRuntimeId = RuntimeId(battle, NormalTargetId);
                var resistantRuntimeId = RuntimeId(battle, ResistantTargetId);
                var normalFreeze = FreezeActivation(battle, normalRuntimeId);
                var resistantFreeze = FreezeActivation(battle, resistantRuntimeId);
                Require(ownerMomentumObserved.SetEquals(ownerIds),
                    "both Rimebrand owners gain on-hit AttackSpeed momentum");
                Require(twoSourceFrostObserved || battle.StatusPresentationCues.Any(cue =>
                        cue.Status.StableId == FrostId && cue.Status.SourceContributions
                            .Select(source => source.SourceId).Distinct(StringComparer.Ordinal).Count() >= 2),
                    "Frost aggregation exposes two independent Equipment sources");
                Require(normalFreeze.Status.RemainingTicks == 6,
                    "normal target receives six-tick Freeze");
                Require(resistantFreeze.Status.RemainingTicks == 3,
                    "0.5 control-resistance target receives three-tick Freeze");

                foreach (var targetRuntimeId in new[] { normalRuntimeId, resistantRuntimeId })
                {
                    var conversion = battle.StatusPresentationCues.FirstOrDefault(cue =>
                        cue.Status.OwnerId == targetRuntimeId && cue.Status.StableId == FrostId &&
                        cue.Lifecycle == StatusPresentationCueLifecycle.Removed &&
                        cue.RemovalReason == StatusRemovalReason.OverflowConsumed && cue.Status.Stacks == 3);
                    Require(conversion is not null, $"three-stack Frost converts on {targetRuntimeId}");
                }
                Require(battle.StatusPresentationCues.Any(cue => cue.Lifecycle == StatusPresentationCueLifecycle.OnActive) &&
                        battle.StatusPresentationCues.Any(cue => cue.Lifecycle == StatusPresentationCueLifecycle.Executed &&
                                                                   cue.Status.Stacks > 1) &&
                        battle.StatusPresentationCues.Any(cue => cue.Lifecycle == StatusPresentationCueLifecycle.Removed),
                    "Frost preset emits apply, stack, and remove presentation cues");
            }

            await VerifySourceAwareBattleInspection(package.Content,
                new BattleLabPreparationAdapter(index).Build(session.Freeze()),
                frostEquipment.Select(item => item.InstanceId).ToHashSet(StringComparer.Ordinal));

            GD.Print("BATTLE_LAB_FROST_BEHAVIOR_CONTRACT_OK equipment=two-instance trait=winterbound " +
                     "frost=two-source conversion=three-stack freeze=normal-6-resistant-3 cues=apply-stack-remove " +
                     "inspection=real-click-source-aware-ui");
            return 0;
        }
        catch (Exception exception)
        {
            GD.PrintErr("BATTLE_LAB_FROST_BEHAVIOR_CONTRACT_FAILED: " + exception);
            return 1;
        }
    }

    private static string RuntimeId(BattleSimulation battle, string contentId) => battle.Units
        .Single(unit => unit.Definition.ContentId == contentId && !unit.IsTemporary)
        .RuntimeId;

    private static StatusPresentationCue FreezeActivation(BattleSimulation battle, string ownerId) =>
        battle.StatusPresentationCues.FirstOrDefault(cue => cue.Status.OwnerId == ownerId &&
            cue.Status.StableId == FreezeId && cue.Lifecycle == StatusPresentationCueLifecycle.OnActive) ??
        throw new InvalidOperationException(
            $"Freeze activation missing for {ownerId}; cues=" + string.Join(';', battle.StatusPresentationCues
                .Where(cue => cue.Status.OwnerId == ownerId)
                .Select(cue => $"{cue.Tick}:{cue.Lifecycle}:{cue.Status.StableId}:{cue.Status.Stacks}:" +
                               $"{cue.Status.RemainingTicks}:{cue.RemovalReason}")));

    private async Task VerifySourceAwareBattleInspection(
        ContentRegistry content,
        BattleConfig config,
        IReadOnlySet<string> equipmentSourceIds)
    {
        BattleScreenController? screen = null;
        try
        {
            GetWindow().Size = new Vector2I(1600, 900);
            var scene = GD.Load<PackedScene>("res://scenes/ui/BattleScreen.tscn") ??
                        throw new InvalidOperationException("BattleScreen scene missing");
            screen = scene.Instantiate<BattleScreenController>();
            AddChild(screen);
            await Frame(3);
            screen.StartBattle(content, config, "冰霜体系验证");
            screen.SetLabControlsVisible(true);
            screen.SetPaused(true);

            BattleScreenRuntimeUnitSnapshot? target = null;
            for (var guard = 0; guard < 400 && screen.Outcome == BattleOutcome.Running; guard++)
            {
                Require(screen.StepOneTick(), "paused Frost inspection fixed step");
                target = screen.ReadRuntimeUnits().FirstOrDefault(unit => unit.Team == 1 && unit.Statuses.Any(status =>
                    status.StableId == FrostId && status.Stacks >= 2 &&
                    equipmentSourceIds.All(sourceId => status.SourceContributions.Any(source =>
                        source.SourceId == sourceId))));
                if (target is not null) break;
            }
            Require(target is not null, "production BattleScreen exposes two-source Frost snapshot");
            var selectedTarget = target ?? throw new InvalidOperationException(
                "production BattleScreen exposes two-source Frost snapshot");

            var presenter = Descendants<UnitContentRoot>(screen)
                .Single(unit => unit.RuntimeId == selectedTarget.RuntimeId);
            var click = presenter.GlobalPosition;
            GetViewport().PushInput(Mouse(click, true));
            await Frame();
            GetViewport().PushInput(Mouse(click, false));
            await Frame(2);

            var statusText = screen.GetNode<Label>(
                "Margin/Layout/BattleArea/InspectorRegion/SelectedUnitPanel/Layout/UnitStatuses").Text;
            Require(equipmentSourceIds.All(sourceId => statusText.Contains(sourceId, StringComparison.Ordinal)) &&
                    statusText.Contains("×2", StringComparison.Ordinal) &&
                    statusText.Contains("秒", StringComparison.Ordinal),
                "real BattleScreen selection renders both Equipment source ids, stacks, and duration: " + statusText);
        }
        finally
        {
            screen?.StopBattle();
            screen?.QueueFree();
            await Frame(2);
        }
    }

    private static IEnumerable<T> Descendants<T>(Node root) where T : Node
    {
        foreach (var child in root.GetChildren())
        {
            if (child is T match) yield return match;
            foreach (var nested in Descendants<T>(child)) yield return nested;
        }
    }

    private static InputEventMouseButton Mouse(Vector2 position, bool pressed) => new()
    {
        Position = position,
        GlobalPosition = position,
        ButtonIndex = MouseButton.Left,
        ButtonMask = pressed ? MouseButtonMask.Left : 0,
        Pressed = pressed
    };

    private async Task Frame(int count = 1)
    {
        for (var index = 0; index < count; index++)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private static void Require(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException(label);
    }
}
