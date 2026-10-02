using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.App;
using TowerAutobattler.Composition;
using TowerAutobattler.Project;
using TowerAutobattler.Run;
using TowerAutobattler.UI;

// Production package, detached memory saves. Rendered mode also drives the real coordinator by input.
public partial class RecruitmentOpeningContractSmoke : Control
{
    private UiInputStage? _stage;
    private bool OpeningOnly => OS.GetCmdlineUserArgs().Contains("--opening-only");

    public override async void _Ready()
    {
        var exit = 0;
        try
        {
            var gate = await GamePackagePublisher.CreateReadyAsync(this,
                GD.Load<GameProjectDefinition>("res://content/project/alpha_project.tres"));
            var package = gate.Package ?? throw new InvalidOperationException(string.Join(';', gate.Report.CoreErrors));
            VerifyOpening(package);
            if (!OpeningOnly)
            {
                VerifySupply(package);
                VerifyInvalidAuthoring(package);
            }
            GD.Print("RECRUITMENT_OPENING_CONTRACT_OK new-draft reload persistence atomicity uniqueness legacy-load");
            if (DisplayServer.GetName() != "headless") await VerifyInput(package);
        }
        catch (Exception exception)
        {
            exit = 1; GD.PrintErr("RECRUITMENT_OPENING_FAILED: " + exception);
            if (DisplayServer.GetName() != "headless") await Capture("failure");
        }
        GetTree().Quit(exit);
    }

    private static void VerifyOpening(CompiledGamePackage package)
    {
        var save = new MemorySave();
        var app = new RunApplication(package.Content, save, package.Project);
        var supply = app.Project.Campaign.RecruitmentSupply!;
        Require(app.Meta.UnlockedHeroIds.Count == 2, "legacy account fixture has only two unlocks");
        var batches = new HashSet<string>();
        for (ulong seed = 1; seed <= 128; seed++)
        {
            var seeded = new RunApplication(package.Content, new MemorySave(), package.Project);
            Require(seeded.BeginOpeningRecruitment(seed), "opening independent of legacy account unlocks");
            var opening = seeded.ActiveRun!.OpeningRecruitment!;
            Require(opening.CandidateIds.Length == 6 && opening.CandidateIds.Distinct().Count() == 6 &&
                opening.CandidateIds.All(id => supply.TierOf(id) is 1 or 2), "six unique low-tier opening candidates");
            batches.Add(string.Join(',', opening.CandidateIds));
        }
        Require(batches.Count > 80, "seeds produce varied opening offers");
        save.Fail = true;
        Require(!app.BeginOpeningRecruitment(5) && app.ActiveRun is null && save.Json is null, "failed opening creates no active run");
        save.Fail = false;
        Require(app.BeginOpeningRecruitment(5), "begin saved draft");
        var run = app.ActiveRun!;
        var candidates = run.OpeningRecruitment!.CandidateIds;
        var before = Snapshot(run); var stored = save.Json;
        Require(!app.ConfirmOpeningRecruitment() && !app.ToggleOpeningHero("unknown") &&
            !app.SelectNode(TowerNodeType.Combat) && !app.Recruit(candidates[0]) && app.CurrentOptions().Count == 0 &&
            Snapshot(run) == before && save.Json == stored, "draft cannot bypass selection or enter run operations");
        save.Fail = true;
        Require(!app.ToggleOpeningHero(candidates[0]) && Snapshot(run) == before && save.Json == stored, "toggle save failure is atomic");
        save.Fail = false;
        Require(app.ToggleOpeningHero(candidates[0]) && !app.ConfirmOpeningRecruitment(), "one selected is insufficient");
        before = Snapshot(run); stored = save.Json;
        save.Fail = true;
        Require(!app.BeginOpeningRecruitment(999) && ReferenceEquals(app.ActiveRun, run) &&
            Snapshot(run) == before && save.Json == stored, "failed new opening preserves the selected draft and its save");
        save.Fail = false;
        Require(app.BeginOpeningRecruitment(999) && !ReferenceEquals(app.ActiveRun, run) &&
            app.ActiveRun is { Seed: 999, OpeningRecruitment.SelectedIds.IsEmpty: true } &&
            Snapshot(run) == before, "new game replaces a pending draft and clears its selections");
        var expected = new RunApplication(package.Content, new MemorySave(), package.Project);
        Require(expected.BeginOpeningRecruitment(999) &&
            app.ActiveRun!.OpeningRecruitment!.CandidateIds.SequenceEqual(expected.ActiveRun!.OpeningRecruitment!.CandidateIds) &&
            save.Json == Snapshot(app.ActiveRun), "replacement uses the new seed and is saved before publication");
        candidates = app.ActiveRun!.OpeningRecruitment!.CandidateIds;
        Require(app.ToggleOpeningHero(candidates[0]), "select a hero in the new draft");
        app = new RunApplication(package.Content, save, package.Project);
        run = app.ActiveRun!;
        Require(run.OpeningRecruitment!.CandidateIds.SequenceEqual(candidates) &&
            run.OpeningRecruitment.SelectedIds.SequenceEqual([candidates[0]]), "reload preserves candidates and selected hero");
        Require(app.ToggleOpeningHero(candidates[0]) && run.OpeningRecruitment!.SelectedIds.IsEmpty, "unselect saved hero");
        Require(app.ToggleOpeningHero(candidates[1]) && app.ToggleOpeningHero(candidates[2]) &&
            !app.ToggleOpeningHero(candidates[3]), "exact two-person limit");
        before = Snapshot(run); stored = save.Json;
        save.Fail = true;
        Require(!app.ConfirmOpeningRecruitment() && Snapshot(run) == before && save.Json == stored, "failed confirmation preserves all draft state");
        save.Fail = false;
        Require(app.ConfirmOpeningRecruitment() && run.OpeningRecruitment is null && run.Roster.Count == 2 &&
            run.Roster.Select(hero => hero.ContentId).SequenceEqual(candidates.Skip(1).Take(2)) &&
            run.Deployment.Count(id => id.Length > 0) == 2 && run.PendingNode && run.SelectedNode == TowerNodeType.Combat &&
            !app.ConfirmOpeningRecruitment(), "confirmation grants precisely two and reaches first combat preparation exactly once");
        var reloaded = new RunApplication(package.Content, save, package.Project);
        Require(reloaded.ActiveRun is { OpeningRecruitment: null, PendingNode: true, Roster.Count: 2 }, "confirmed run resumes deployment");
        before = Snapshot(run); stored = save.Json;
        save.Fail = true;
        Require(!app.BeginOpeningRecruitment(12) && Snapshot(app.ActiveRun!) == before && save.Json == stored, "new opening failure preserves prior run");
        save.Fail = false;
        // A corrupt draft is rejected without a write, reroll, or reset.
        var bad = new MemorySave();
        var badApp = new RunApplication(package.Content, bad, package.Project);
        Require(badApp.BeginOpeningRecruitment(3), "corrupt fixture");
        badApp.ActiveRun!.OpeningRecruitment = new([candidates[0], candidates[0], .. candidates.Take(4)], []);
        bad.SaveActiveRun(badApp.ActiveRun); stored = bad.Json;
        var rejected = new RunApplication(package.Content, bad, package.Project);
        Require(rejected.ActiveRun is null && rejected.ActiveRunLoadDiagnostic is not null && bad.Json == stored, "invalid draft preserves saved original");
        var legacySave = new MemorySave();
        var legacy = new RunApplication(package.Content, legacySave, package.Project);
        Require(legacy.StartNewRun(legacy.Meta.UnlockedHeroIds[0], 15), "legacy start fixture");
        var excluded = package.Content.Catalog.Heroes.First(hero => !supply.TierByHero.ContainsKey(hero.StableId)).StableId;
        Require(legacy.Recruit(excluded), "pre-existing excluded roster member fixture");
        Require(new RunApplication(package.Content, legacySave, package.Project).ActiveRun!.Roster.Any(hero => hero.ContentId == excluded),
            "run-pool simplification preserves old rosters and laboratory content");
    }

    private static void VerifySupply(CompiledGamePackage package)
    {
        var supply = package.Project.Campaign.RecruitmentSupply!;
        var pool = package.Project.Campaign.RecruitmentPool.ContentIds;
        Require(pool.Length == 14 && Enumerable.Range(1, 4).Select(tier => pool.Count(id => supply.TierOf(id) == tier))
            .SequenceEqual([4, 4, 4, 2]), "four authored tier groups");
        foreach (var floor in new[] { 0, 2, 3, 5, 6, 9, 10, 14 })
        {
            var expected = floor < 3 ? new[] {65,35,0,0} : floor < 6 ? [35,45,20,0] : floor < 10 ? [15,30,45,10] : [0,15,50,35];
            Require(supply.WeightsAt(floor).SequenceEqual(expected), "absolute-floor stage boundary");
            var observed = new int[4];
            for (var sample = 0; sample < 10000; sample++)
            {
                var draw = RecruitmentSupplySampler.Draw(supply, pool, supply.WeightsAt(floor), 1, $"stats:{floor}:{sample}");
                observed[supply.TierOf(draw[0]) - 1]++;
            }
            for (var index = 0; index < 4; index++) Require(expected[index] == 0 ? observed[index] == 0 :
                Math.Abs(observed[index] / 10000d - expected[index] / 100d) < .025, "tier odds independent of number of heroes");
            GD.Print($"RECRUITMENT_ODDS floor={floor + 1} counts={string.Join(',', observed)} n=10000");
        }
        var noHigh = pool.Where(id => supply.TierOf(id) == 1).ToArray();
        Require(RecruitmentSupplySampler.Draw(supply, noHigh, supply.WeightsAt(14), 3, "empty").Count == 0,
            "late empty pool never backfills prohibited first-tier heroes");
        var oneHigh = pool.Where(id => supply.TierOf(id) == 4).Take(1).Concat(noHigh).ToArray();
        Require(RecruitmentSupplySampler.Draw(supply, oneHigh, supply.WeightsAt(14), 3, "short").Count == 1,
            "short high-tier pool is not padded");
        Require(RecruitmentSupplySampler.Draw(supply, pool.Where(id => supply.TierOf(id) > 2), supply.WeightsAt(0), 3, "early-empty").Count == 0,
            "early exhaustion cannot unlock high tiers");
        var save = new MemorySave();
        var app = new RunApplication(package.Content, save, package.Project);
        Require(app.BeginOpeningRecruitment(45), "offer fixture draft");
        var opening = app.ActiveRun!.OpeningRecruitment!;
        app.ToggleOpeningHero(opening.CandidateIds[0]); app.ToggleOpeningHero(opening.CandidateIds[1]); app.ConfirmOpeningRecruitment();
        var run = app.ActiveRun!;
        var reserve = pool.First(id => run.Roster.All(hero => hero.ContentId != id));
        Require(app.Recruit(reserve), "reserve fixture");
        var persistence = new RunProgressionPersistenceService(package.Content, save, package.Project);
        var decisions = new RunDecisionService(package.Content, package.Project, persistence);
        foreach (var floor in new[] { 0, 3, 6, 10, 14 })
        for (ulong seed = 1; seed <= 64; seed++)
        {
            run.FloorIndex = floor; run.Seed = seed;
            var offer = decisions.CreateOffer(run, RunOfferKind.Recruitment);
            var ids = offer.Choices.SelectMany(choice => choice.Operations).Where(op => op.Kind == RunOperationKind.Recruit).Select(op => op.ContentId).ToArray();
            Require(ids.Length == 3 && ids.Distinct().Count() == ids.Length &&
                ids.All(id => !RunRecruitmentPolicy.IsOwned(run, id) && supply.WeightsAt(floor)[supply.TierOf(id) - 1] > 0),
                "production offers apply stage gates, reserve ownership and batch uniqueness");
            Require(JsonSerializer.Serialize(offer) == JsonSerializer.Serialize(decisions.CreateOffer(run, RunOfferKind.Recruitment)), "deterministic opportunity");
            Require(app.RecruitmentChoices().All(entry => supply.WeightsAt(floor)[supply.TierOf(entry.StableId) - 1] > 0), "compatibility choice query uses stage policy");
        }
        run.FloorIndex = 10; run.SelectedNode = TowerNodeType.Recruitment;
        run.PendingOffer = decisions.CreateOffer(run, RunOfferKind.Recruitment);
        Require(persistence.ValidateRun(run) && save.SaveActiveRun(run), "persist generated stage offer");
        var original = JsonSerializer.Serialize(run.PendingOffer);
        var restored = new RunApplication(package.Content, save, package.Project);
        Require(JsonSerializer.Serialize(restored.PendingOffer) == original, "stage offer reload never rerolls");
        var choice = restored.PendingOffer!.Choices.First(choice => choice.Operations.Any(op => op.Kind == RunOperationKind.Recruit));
        Require(restored.ResolveOffer(restored.PendingOffer.OfferId, choice.StableId).Succeeded &&
            !restored.Recruit(choice.Operations[0].ContentId), "offered recruit is acquired once");

        run.FloorIndex = 0; run.PendingNode = false; run.PendingOffer = null;
        foreach (var id in pool.Where(id => supply.TierOf(id) <= 2 && !RunRecruitmentPolicy.IsOwned(run, id)))
            Require(app.Recruit(id), "exhaust early eligible tiers fixture");
        run.PendingNode = true; run.SelectedNode = TowerNodeType.Recruitment;
        run.PendingOffer = decisions.CreateOffer(run, RunOfferKind.Recruitment);
        Require(run.PendingOffer is { Choices.IsEmpty: true, AllowSkip: true } && persistence.ValidateRun(run) && save.SaveActiveRun(run),
            "early exhausted production offer persists without leaking locked tiers");
        restored = new RunApplication(package.Content, save, package.Project);
        Require(restored.PendingOffer is { Choices.IsEmpty: true, AllowSkip: true } &&
            restored.ResolveOffer(restored.PendingOffer.OfferId, null).Succeeded && restored.ActiveRun!.FloorIndex == 1,
            "empty stage offer reloads and advances exactly once");
    }

    private static void VerifyInvalidAuthoring(CompiledGamePackage package)
    {
        var source = GD.Load<GameProjectDefinition>("res://content/project/alpha_project.tres");
        var copy = (GameProjectDefinition)source.Duplicate();
        copy.Campaign = (CampaignDefinition)source.Campaign!.Duplicate();
        var supply = (RecruitmentSupplyDefinition)source.Campaign.RecruitmentSupply!.Duplicate();
        copy.Campaign.RecruitmentSupply = supply;
        supply.OpeningTierWeights = [1, 0, 0, 0];
        Require(GameProjectCompiler.Compile(copy, package.Content.Graph).Report.HasCoreErrors, "compile rejects fewer than six legal opening heroes");
        supply.OpeningTierWeights = [-1, 1, 0, 0];
        Require(GameProjectCompiler.Compile(copy, package.Content.Graph).Report.HasCoreErrors, "compile rejects negative weights");
    }

    private async Task VerifyInput(CompiledGamePackage package)
    {
        _stage = new UiInputStage(this, true);
        var game = GD.Load<PackedScene>("res://scenes/app/GameRoot.tscn").Instantiate<GameRoot>();
        game.SaveNamespace = $"tests/recruitment-opening/{Guid.NewGuid():N}";
        _stage.AddChild(game);
        for (var frame = 0; frame < 180 && game.Content is null; frame++) await Frames(1);
        Require(game.Content is not null, "production root ready");
        game.Flow.Dispose();
        var screens = game.GetNode<AppScreenHost>(game.ScreenHostPath);
        var save = new MemorySave();
        var app = new RunApplication(package.Content, save, package.Project);
        using var flow = new GameFlowCoordinator(() => app, screens, package.Project.Presentation, () => { });
        flow.Start();
        await Frames(3);
        await Click(screens.MainMenu.GetNode<Button>("Center/Panel/Menu/NewRunButton"));
        var screen = screens.HeroSelection;
        Require(screen.IsVisibleInTree(), "new game enters opening screen");
        var tiles = screen.GetNode<GridContainer>("%HeroLibrary").GetChildren().OfType<HeroLibraryTile>().ToArray();
        var confirm = screen.GetNode<Button>("%ConfirmOpening");
        Require(tiles.Length == 6 && confirm.Disabled, "six candidates rendered and confirmation disabled");
        var libraryScroll = screen.GetNode<ScrollContainer>("%LibraryScroll");
        Require(tiles.All(tile => libraryScroll.GetGlobalRect().Encloses(tile.GetGlobalRect())) &&
            screen.GetGlobalRect().Encloses(confirm.GetGlobalRect()), "all six candidates and confirmation fit without scrolling");
        await Capture("candidates");
        await Click(tiles[0]);
        Require(app.ActiveRun!.OpeningRecruitment!.SelectedIds.SequenceEqual([tiles[0].StableId]) && confirm.Disabled, "click selects one");
        var firstSeat = screen.GetNode<OpeningHeroSeat>("%FirstSeat");
        save.Fail = true;
        await Click(firstSeat);
        Require(firstSeat.StableId == tiles[0].StableId && !firstSeat.Disabled &&
            app.ActiveRun.OpeningRecruitment.SelectedIds.SequenceEqual([tiles[0].StableId]) &&
            screen.GetNode<Label>("%OpeningStatus").Visible,
            "failed seat cancellation preserves the saved card and visible seat");
        save.Fail = false;
        await Click(tiles[1]);
        Require(app.ActiveRun.OpeningRecruitment.SelectedIds.Length == 2 && !confirm.Disabled, "click selects second");
        await Capture("selected");
        var point = tiles[1].GetGlobalRect().GetCenter();
        Inject(new InputEventMouseMotion { Position = point, GlobalPosition = point });
        await Frames(30);
        Require(tiles[1].Disclosure.IsOpen && !tiles[1].HasNode("DetailsButton"), "hover opens production card without a details button");
        await Capture("details");
        await Click(tiles[2]);
        Require(app.ActiveRun.OpeningRecruitment.SelectedIds.Length == 2 && screen.PreviewStableId == tiles[2].StableId, "third click previews without adding");
        await Click(tiles[0]);
        Require(confirm.Disabled && app.ActiveRun.OpeningRecruitment.SelectedIds.Length == 1, "click deselects");
        // Leave mouse inspection, then actually navigate with the keyboard even
        // when the previous click already focused the first candidate.
        Inject(new InputEventMouseMotion { Position = new Vector2(800, 150), GlobalPosition = new Vector2(800, 150) });
        await ToSignal(GetTree().CreateTimer(.5), SceneTreeTimer.SignalName.Timeout);
        await KeyPress(Key.Tab);
        for (var step = 0; step < 25 && _stage.Viewport.GuiGetFocusOwner() != tiles[0]; step++) await KeyPress(Key.Tab);
        Require(_stage.Viewport.GuiGetFocusOwner() == tiles[0], "keyboard tab reaches candidate");
        await ToSignal(GetTree().CreateTimer(.4), SceneTreeTimer.SignalName.Timeout);
        Require(tiles[0].Disclosure.IsOpen, "keyboard focus reveals buttonless card details");
        await KeyPress(Key.Enter);
        Require(app.ActiveRun.OpeningRecruitment.SelectedIds.Length == 2 && !confirm.Disabled, "keyboard selection works");
        var snapshot = Snapshot(app.ActiveRun);
        await Click(screen.GetNode<Button>("%BackButton"));
        Require(screens.MainMenu.IsVisibleInTree(), "back reaches menu");
        app = new RunApplication(package.Content, save, package.Project);
        await Click(screens.MainMenu.GetNode<Button>("Center/Panel/Menu/ContinueButton"));
        Require(screen.IsVisibleInTree() && Snapshot(app.ActiveRun!) == snapshot && !confirm.Disabled, "continue after reload restores entire draft");
        await Capture("selected");
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var previous = app.ActiveRun!;
            await Click(screen.GetNode<Button>("%BackButton"));
            await Click(screens.MainMenu.GetNode<Button>("Center/Panel/Menu/NewRunButton"));
            Require(screen.IsVisibleInTree() && !ReferenceEquals(app.ActiveRun, previous) &&
                app.ActiveRun!.Seed != previous.Seed && app.ActiveRun.OpeningRecruitment!.SelectedIds.IsEmpty && confirm.Disabled,
                "each new-game click starts a fresh draft with cleared selections");
            tiles = screen.GetNode<GridContainer>("%HeroLibrary").GetChildren().OfType<HeroLibraryTile>().ToArray();
            Require(tiles.Length == 6 && tiles.Select(tile => tile.StableId).SequenceEqual(app.ActiveRun!.OpeningRecruitment!.CandidateIds),
                "six rendered cards match the newly saved candidates");
            GD.Print($"RECRUITMENT_NEW_GAME_INPUT seed={app.ActiveRun!.Seed} candidates={string.Join(',', app.ActiveRun.OpeningRecruitment!.CandidateIds)}");
            if (attempt == 0) await Capture("rerolled");
            await Click(tiles[0]);
        }
        await Click(tiles[1]);
        snapshot = Snapshot(app.ActiveRun!);
        await Click(screen.GetNode<Button>("%BackButton"));
        app = new RunApplication(package.Content, save, package.Project);
        await Click(screens.MainMenu.GetNode<Button>("Center/Panel/Menu/ContinueButton"));
        Require(Snapshot(app.ActiveRun!) == snapshot && !confirm.Disabled, "continue restores the replacement draft after reload");
        save.Fail = true;
        await Click(confirm);
        Require(screen.IsVisibleInTree() && Snapshot(app.ActiveRun!) == snapshot, "failed UI confirmation remains retryable");
        save.Fail = false;
        await Click(confirm);
        Require(screens.Deployment.IsVisibleInTree() && app.ActiveRun is { OpeningRecruitment: null, Roster.Count: 2 }, "confirmation opens first deployment");
        Require(screens.Deployment.GetNode<Control>("%ReserveBench").IsVisibleInTree(), "persistent reserve bench remains visible");
        await Capture("deployment");

        if (OpeningOnly)
        {
            GD.Print("RECRUITMENT_OPENING_INPUT_OK new-game-reroll cleared-selection continue-reload mouse keyboard failed-save confirm deployment isolated-pointer");
            game.QueueFree();
            await Frames(3);
            return;
        }

        // Inspect a mid-run opportunity without simulating a full run or touching player saves.
        var run = app.ActiveRun!;
        run.FloorIndex = 6; run.SelectedNode = TowerNodeType.Recruitment;
        var persistence = new RunProgressionPersistenceService(package.Content, save, package.Project);
        run.PendingOffer = new RunDecisionService(package.Content, package.Project, persistence).CreateOffer(run, RunOfferKind.Recruitment);
        Require(persistence.ValidateRun(run) && save.SaveActiveRun(run), "mid-stage UI fixture valid");
        flow.ShowTower(); await Frames(3);
        var cards = screens.Recruitment.FindChildren("*", "", true, false).OfType<RunOfferChoiceCard>().ToArray();
        Require(cards.Length == 3 && cards.All(card => card.GetNode<Label>("Layout/Identity").Text.Contains(" 阶 · ")),
            "ordinary recruitment cards visibly show acquisition tier");
        await Capture("mid-stage");
        var picked = cards[0].StableId;
        await Click(cards[0].ConfirmButton);
        Require(screens.Tower.IsVisibleInTree() && run.FloorIndex == 7 && run.Roster.Count == 3 &&
            run.Roster.Count(hero => hero.ContentId == picked) == 1, "native recruitment button acquires one hero and advances");
        GD.Print("RECRUITMENT_OPENING_INPUT_OK mouse keyboard deselect limit back reload failed-save confirm deployment tier-card recruit");
    }

    private async Task Click(Control target)
    {
        var point = target.GetGlobalRect().GetCenter();
        Require(target.IsVisibleInTree() && _stage!.Viewport.GetVisibleRect().HasPoint(point), "click target is visible in isolated viewport");
        Inject(new InputEventMouseMotion { Position = point, GlobalPosition = point });
        await Frames(1);
        Inject(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, ButtonMask = MouseButtonMask.Left, Pressed = true });
        Inject(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = false });
        await Frames(3);
    }
    private void Inject(InputEvent input) => _stage!.Push(input);
    private async Task KeyPress(Key key)
    {
        Inject(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = true });
        Inject(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = false });
        await Frames(2);
    }
    private async Task Frames(int count) { for (var i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
    private async Task Capture(string name)
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        RenderingServer.ForceDraw(false);
        var folder = ProjectSettings.GlobalizePath("res://.godot/ui-review");
        System.IO.Directory.CreateDirectory(folder);
        (_stage?.Viewport ?? GetViewport()).GetTexture().GetImage().SavePng($"{folder}/recruitment-opening-{name}.png");
    }
    private static string Snapshot(ActiveRunDto run) => JsonSerializer.Serialize(run);
    private static void Require(bool condition, string label) { if (!condition) throw new InvalidOperationException(label); }
    private sealed class MemorySave : IRunSaveService
    {
        public string? Json;
        public bool Fail;
        public MetaProgressDto LoadMeta() => new();
        public SettingsDto LoadSettings() => new();
        public ActiveRunDto? LoadActiveRun() => Json is null ? null : JsonSerializer.Deserialize<ActiveRunDto>(Json);
        public bool SaveMeta(MetaProgressDto value) => true;
        public bool SaveSettings(SettingsDto value) => true;
        public bool SaveActiveRun(ActiveRunDto value) { if (Fail) return false; Json = Snapshot(value); return true; }
        public void DeleteActiveRun() => Json = null;
    }
}
