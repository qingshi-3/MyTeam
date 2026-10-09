using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Audio;
using TowerAutobattler.Battle;
using TowerAutobattler.Composition;
using TowerAutobattler.Content;
using TowerAutobattler.Run;
using TowerAutobattler.UI;

// Explicit presentation fixture. The authored UI and motion resources are real;
// selection, position and fog below are disposable view DTOs, never a Run/save.
public partial class JourneyPresentationPreview : Node
{
    private Control _world = null!;
    private MainMenuScreenController _menu = null!;
    private HeroSelectScreen _heroes = null!;
    private ExpeditionScreenController _map = null!;
    private RestScreenController _camp = null!;
    private UiJourneyTransition _motion = null!;
    private FeedbackAudio _audio = null!;
    private UiMotionBinding? _motionBinding;
    private UiFeedbackBinding? _feedbackBinding;
    private UiInputStage? _stage;
    private Control? _active;
    private HeroSelectionViewModel[] _models = [];
    private OpeningRecruitmentDto _opening = new([], []);
    private readonly HashSet<int> _revealed = [];
    private readonly List<string> _checks = [];
    private readonly List<object> _inputs = [];
    private readonly Dictionary<string, Image> _screenshots = [];
    private bool _reduced, _departed;
    private int _position = 7 * 16 + 6, _selected = 7 * 16 + 6;
    private const int CampIndex = 7 * 16 + 9;
    private static bool SelectionLightOnly => OS.GetCmdlineUserArgs().Contains("--selection-light-only");
    private static string Output => OS.GetCmdlineUserArgs().FirstOrDefault(arg => arg.StartsWith("--output=", StringComparison.Ordinal))?[9..] ?? (SelectionLightOnly
        ? "res://outputs/opening-selection-light-20261004"
        : "res://outputs/journey-presentation-preview-20261004");

    public override async void _Ready()
    {
        try
        {
            _world = GetNode<Control>("World");
            var capture = OS.GetCmdlineUserArgs().Contains("--capture");
            if (capture)
            {
                Require(DisplayServer.GetName() != "headless", "动态预览需要实际渲染驱动。");
                _stage = new UiInputStage(this, true);
                _stage.Viewport.GuiEmbedSubwindows = true;
                // The authored subtree has already entered once. Its controllers
                // disconnect on ExitTree, so request Ready before isolated reentry.
                foreach (var node in Descendants<Node>(_world).Prepend(_world))
                    if (node is not FeedbackAudio) node.RequestReady();
                RemoveChild(_world);
                _stage.AddChild(_world);
            }
            _menu = _world.GetNode<MainMenuScreenController>("Menu");
            _heroes = _world.GetNode<HeroSelectScreen>("Heroes");
            _map = _world.GetNode<ExpeditionScreenController>("Map");
            _camp = _world.GetNode<RestScreenController>("Camp");
            _motion = _world.GetNode<UiJourneyTransition>("Transition");
            _audio = _world.GetNode<FeedbackAudio>("UiAudio");
            _motion.PlaySound = _audio.RequestUi;
            _motionBinding = new UiMotionBinding(_world, () => _reduced);
            _feedbackBinding = new UiFeedbackBinding(_world, _audio, _menu);
            _world.GetNode<CheckButton>("PreviewBanner/Reduced").Toggled += value => _reduced = value;
            var content = await SkillSampleContent.LoadAsync(this);
            SemanticIcons.Configure(GD.Load<SemanticIconCatalog>("res://content/ui/semantic_icon_catalog.tres"));
            var ids = new[] { "hero_hc38_grit_brawler", "enemy_ee03_trample_brute", "enemy_ee07_furnace_lizard", "enemy_ee09_swap_envoy", "soldier_dummy_melee", "soldier_dummy_static" };
            _models = ids.Select(id =>
            {
                Require(content.TryGet(id, out var entry), "保留样板缺失：" + id);
                var definition = (UnitDefinition)entry.Definition;
                return new HeroSelectionViewModel(id, definition, true, "保留内容展示",
                    id.StartsWith("soldier_dummy", StringComparison.Ordinal) ? "测试假人，仅用于展示卡片布局。" : "保留技能样板，仅用于展示 UI 动线。",
                    BattleSetupFactory.Snapshot(entry, content), 1);
            }).ToArray();
            _opening = new(ids.ToImmutableArray(), []);
            _menu.BindJourney(true);
            _menu.Bind(false);
            _menu.GetNode<Button>("Center/Panel/Menu/SettingsButton").Hide();
            _menu.GetNode<Button>("Center/Panel/Menu/ToolsButton").Hide();
            _menu.NewRunRequested += OpenDraft;
            _menu.ContinueRequested += () => { if (_departed) ShowPage(_map); };
            _menu.QuitRequested += () => GetTree().Quit();
            _heroes.BackRequested += () => ShowPage(_menu);
            _heroes.OpeningHeroToggled += ToggleHero;
            _heroes.OpeningConfirmed += Depart;
            _map.StepRequested += Step;
            _map.CellSelected += index => { if (_motion.IsPlaying) return; _selected = index; BindMap(); };
            _map.MoveRequested += index => { if (!_motion.IsPlaying && _revealed.Contains(index)) { _position = _selected = index; Reveal(); BindMap(); } };
            _map.InteractRequested += OpenCamp;
            _map.InspectRequested += _ => OpenCamp();
            _map.MenuRequested += () => { _menu.Bind(_departed); HideDevelopmentButtons(); ShowPage(_menu); };
            _camp.GetNode<Button>("Center/Panel/Layout/PreviewBack").Pressed += () => ShowPage(_map);
            _camp.GetNode<Label>("Center/Panel/Layout/Title").Text = "林间营地";
            _camp.GetNode<Label>("Center/Panel/Layout/Description").Text = "交互展示：在这里驻足，查看后返回原地图。\n本预览没有资源结算或存档。";
            _camp.GetNode<Control>("Center/Panel/Layout/GoldButton").Hide();
            _camp.GetNode<Control>("Center/Panel/Layout/RecoverButton").Hide();
            Reveal(); BindMap(); ShowPage(_menu);
            if (capture)
            {
                await Settle();
                await RecordPreview();
                if (SelectionLightOnly) await CheckSelectionLightInterrupts();
                else await CheckReducedMotion();
                GD.Print("JOURNEY_PRESENTATION_PREVIEW_CAPTURED fixture=true formal-run=false output=" + ProjectSettings.GlobalizePath(Output));
                GetTree().Quit();
            }
        }
        catch (Exception exception)
        {
            GD.PrintErr("JOURNEY_PRESENTATION_PREVIEW_FAILED " + exception);
            if (OS.GetCmdlineUserArgs().Contains("--capture")) GetTree().Quit(1);
        }
    }

    private void HideDevelopmentButtons()
    {
        _menu.GetNode<Button>("Center/Panel/Menu/SettingsButton").Hide();
        _menu.GetNode<Button>("Center/Panel/Menu/ToolsButton").Hide();
    }

    private void OpenDraft()
    {
        if (_motion.IsPlaying) return;
        _opening = _opening with { SelectedIds = [] };
        _heroes.BindOpening(_models, _opening);
        ShowPage(_heroes);
    }
    private void ToggleHero(string id)
    {
        if (_motion.IsPlaying) return;
        var selected = _opening.SelectedIds.ToList();
        if (!selected.Remove(id) && selected.Count < 2) selected.Add(id);
        _opening = _opening with { SelectedIds = selected.ToImmutableArray() };
        _heroes.RefreshOpening(_opening);
        _audio.RequestUi("ui_select");
    }
    private void Depart()
    {
        if (_motion.IsPlaying || _opening.SelectedIds.Length != 2) return;
        _departed = true;
        _map.PrepareForJourneyEntry();
        BindMap(); ShowPage(_map, true);
    }
    private void ShowPage(Control target, bool departure = false)
    {
        if (_motion.IsPlaying || target == _active) return;
        var previous = _active;
        foreach (var page in new Control[] { _menu, _heroes, _map, _camp })
            page.Visible = page == target || page == previous;
        _active = target;
        _motion.Begin(previous, target, _map, _world.GetNode<Control>("ShadowHeader"), departure, () =>
        {
            if (target == _map) _map.FocusParty();
        });
    }
    private void Step(Vector2I direction)
    {
        if (_motion.IsPlaying || _active != _map) return;
        var x = Math.Clamp(_position % 16 + direction.X, 0, 15);
        var y = Math.Clamp(_position / 16 + direction.Y, 0, 15);
        _position = _selected = y * 16 + x;
        Reveal(); BindMap();
    }
    private void Reveal()
    {
        for (var y = Math.Max(0, _position / 16 - 1); y <= Math.Min(15, _position / 16 + 1); y++)
            for (var x = Math.Max(0, _position % 16 - 1); x <= Math.Min(15, _position % 16 + 1); x++) _revealed.Add(y * 16 + x);
    }
    private void BindMap()
    {
        _map.Bind(new ExpeditionScreenModel
        {
            MapId = "presentation-fixture", Title = "雾林边境", StageText = "UI 展示地图", PlayerCellIndex = _position,
            SelectedCellIndex = _selected, RemainingActions = 8, ActionBudget = 8, CanOpenArmy = false,
            BossName = "远方的守望者", BossSummary = "界面展示占位，不包含首领流程。", BossDisabledReason = "独立 UI 预览",
            Cells = Enumerable.Range(0, 256).Select(index => new ExpeditionCellModel
            {
                Index = index, IsRevealed = _revealed.Contains(index), Terrain = index % 5 == 0 ? ExpeditionTerrainAppearance.Forest : ExpeditionTerrainAppearance.Grassland,
                TerrainName = index % 5 == 0 ? "树林" : "草地", RegionName = "雾林边境", IsReachable = _revealed.Contains(index),
                CanMove = _revealed.Contains(index), SiteAppearance = index == CampIndex ? ExpeditionSiteAppearance.Camp : ExpeditionSiteAppearance.None,
                SiteId = index == CampIndex ? "preview_camp" : "", SiteName = index == CampIndex ? "林间营地" : ""
            }).ToArray(),
            SelectedDetails = _selected == CampIndex && _revealed.Contains(CampIndex) ? new ExpeditionSiteDetails
            {
                CellIndex = CampIndex, SiteId = "preview_camp", Title = "林间营地", Subtitle = "雾林边境",
                Description = "沿着林间小径发现的营地。", InspectLabel = "查看营地", CanInspect = _position == CampIndex,
                AvailabilityText = _position == CampIndex ? "回车查看" : "走近后查看"
            } : null
        });
    }
    private void OpenCamp()
    {
        if (!_motion.IsPlaying && _active == _map && _position == CampIndex) ShowPage(_camp);
    }

    private async Task ScriptedInput()
    {
        await Pause(.5); await Screenshot("01-menu.png");
        await Click(_menu.GetNode<Button>("Center/Panel/Menu/NewRunButton")); await Settle(); await Pause(.65);
        var cards = Descendants<HeroLibraryTile>(_heroes).ToArray();
        Require(cards.Length == 6, "展示开局应为六卡。");
        Require(cards.Select(card => card.Position.Y).Distinct().Count() == 2, "六卡应为两排。");
        await Screenshot("02-opening.png");
        await Click(cards[0]); await Pause(SelectionLightOnly ? .8 : .25);
        await Click(cards[1]); await Pause(SelectionLightOnly ? .8 : .25);
        await Click(Descendants<OpeningHeroSeat>(_heroes).First(seat => seat.StableId == cards[0].StableId));
        await Pause(SelectionLightOnly ? .8 : .25); await Click(cards[2]); await Pause(SelectionLightOnly ? .8 : .35);
        Require(_opening.SelectedIds.SequenceEqual(new[] { cards[1].StableId, cards[2].StableId }), "展示选择／取消／补位顺序正确。");
        await Screenshot("03-two-selected.png");
        if (SelectionLightOnly)
        {
            Require(Descendants<OpeningSelectionLight>(_heroes).All(light => !light.IsPlaying && !light.Visible), "入槽光效在余辉结束后收净。");
            _checks.Add("actual selection, cancellation, refill and settled light cleanup");
            return;
        }
        await Click(_heroes.GetNode<Button>("%ConfirmOpening")); await Settle(); await Pause(.4);
        await Screenshot("04-map.png");
        var known = _revealed.Count;
        var scroll = _map.GetNode<ScrollContainer>("%MapScroll");
        var initialScroll = scroll.ScrollVertical;
        foreach (var key in new[] { Godot.Key.D, Godot.Key.S, Godot.Key.S })
        { await Key(key); await Pause(.18); }
        await Pause(.2);
        Require(scroll.ScrollVertical > initialScroll, "镜头跟随实际移动向下推进。");
        foreach (var key in new[] { Godot.Key.D, Godot.Key.D, Godot.Key.W, Godot.Key.W })
        { await Key(key); await Pause(.18); }
        Require(_position == CampIndex && _revealed.Count > known, "真实键盘输入推进展示位置与迷雾。");
        await Pause(.3); await Screenshot("05-camp-map.png");
        var view = new Vector2I(scroll.ScrollHorizontal, scroll.ScrollVertical);
        await Key(Godot.Key.Enter); await Settle(); await Pause(.35);
        Require(_active == _camp, "回车打开展示营地。");
        await Screenshot("06-camp.png");
        await Key(Godot.Key.W); Require(_position == CampIndex, "营地按键不透传地图。");
        await Click(_camp.GetNode<Button>("Center/Panel/Layout/PreviewBack")); await Settle(); await Pause(.5);
        Require(new Vector2I(scroll.ScrollHorizontal, scroll.ScrollVertical) == view, "营地关闭返回相同地图视角。");
        await Screenshot("07-map-return.png");
        _checks.Add("authored-menu-opening-2x3-selection-cancel-departure-keyboard-fog-camp-return");
    }

    private async Task CheckSelectionLightInterrupts()
    {
        var cards = Descendants<HeroLibraryTile>(_heroes).ToArray();
        var seats = Descendants<OpeningHeroSeat>(_heroes).ToArray();
        var reduced = _world.GetNode<CheckButton>("PreviewBanner/Reduced");
        await Click(seats.First(seat => seat.StableId == cards[2].StableId)); await Pause(.8);
        await Click(cards[0]);
        Require(Descendants<OpeningSelectionLight>(_heroes).Any(light => light.IsPlaying), "真实点选触发入槽粒子。");
        await Click(cards[0]); await Frames(2);
        Require(Descendants<OpeningSelectionLight>(_heroes).All(light => !light.IsPlaying), "飞行中取消清理旧粒子。");
        await Click(cards[0]); await Click(reduced); await Frames(2);
        Require(_reduced && Descendants<OpeningSelectionLight>(_heroes).All(light => !light.IsPlaying), "飞行中启用减弱动效清理粒子。");
        await Click(seats.First(seat => seat.StableId == cards[0].StableId));
        await Click(cards[0]); await Frames(2);
        Require(Descendants<OpeningSelectionLight>(_heroes).All(light => !light.IsPlaying), "减弱动效保留选择结果且不生成粒子。");
        await Click(seats.First(seat => seat.StableId == cards[0].StableId));
        await Click(reduced); await Click(cards[0]);
        await Click(_heroes.GetNode<Button>("%BackButton")); await Settle();
        Require(_active == _menu && Descendants<OpeningSelectionLight>(_heroes).All(light => !light.IsPlaying), "离开页面清理入槽粒子。");
        File.WriteAllText(Path.Combine(ProjectSettings.GlobalizePath(Output), "selection-light-check.txt"),
            "PASS: actual selection, mid-flight cancel, reduced-motion interruption, reduced selection, page exit; fixture only; no save.\n");
    }

    private async Task CheckReducedMotion()
    {
        await Click(_world.GetNode<CheckButton>("PreviewBanner/Reduced"));
        Require(_reduced, "真实输入启用减弱动效。");
        await Click(_map.GetNode<Button>("%MenuButton")); await Settle();
        Require(_active == _menu && !_motion.IsPlaying, "减弱动效立即回到菜单。");
        await Click(_menu.GetNode<Button>("Center/Panel/Menu/ContinueButton")); await Settle();
        Require(_active == _map && !_motion.IsPlaying, "减弱动效立即返回地图。");
        var origin = _position;
        foreach (var key in new[] { Godot.Key.D, Godot.Key.S, Godot.Key.A, Godot.Key.W }) await Key(key);
        await Frames(3);
        Require(_position == origin, "快速连续转向保持正确终点。");
        var marker = _map.GetNode<Control>("%PartyMarker");
        var cell = _map.GetNode<GridContainer>("%MapGrid").GetChild<Control>(_position);
        Require(marker.GetGlobalRect().GetCenter().DistanceTo(cell.GetGlobalRect().GetCenter()) < 2, "减弱动效标记立即落在真实终点。");
        File.WriteAllText(Path.Combine(ProjectSettings.GlobalizePath(Output), "reduced-motion-check.txt"),
            "PASS: real checkbox, menu/continue, rapid D/S/A/W and settled marker; fixture only; no save.\n");
    }

    private async Task RecordPreview()
    {
        var folder = ProjectSettings.GlobalizePath(Output); Directory.CreateDirectory(folder);
        var frames = new List<(Image Image, ulong Time)>();
        var bus = AudioServer.GetBusIndex("Master"); var slot = AudioServer.GetBusEffectCount(bus);
        var sound = new AudioEffectRecord { Format = AudioStreamWav.FormatEnum.Format16Bits };
        AudioServer.AddBusEffect(bus, sound); sound.SetRecordingActive(true);
        var start = Time.GetTicksMsec(); string? failure = null;
        try
        {
            var action = ScriptedInput(); ulong previous = 0;
            while (!action.IsCompleted)
            {
                await Frames(1); var now = Time.GetTicksMsec();
                if (now - previous < 30 || frames.Count >= 600) continue;
                previous = now; var image = _stage!.Viewport.GetTexture().GetImage();
                image.Resize(960, 540, Image.Interpolation.Lanczos); frames.Add((image, now));
            }
            await action;
        }
        catch (Exception exception) { failure = exception.ToString(); throw; }
        finally
        {
            var end = Time.GetTicksMsec(); sound.SetRecordingActive(false);
            var audio = sound.GetRecording(); var audioResult = audio?.SaveToWav(Path.Combine(folder, "audio.wav"));
            AudioServer.RemoveBusEffect(bus, slot);
            foreach (var shot in _screenshots)
            {
                Require(shot.Value.SavePng(Path.Combine(folder, shot.Key)) == Error.Ok, "保存截图。");
                shot.Value.Dispose();
            }
            _screenshots.Clear();
            var concat = new List<string> { "ffconcat version 1.0" };
            for (var index = 0; index < frames.Count; index++)
            {
                var file = $"frame-{index:D3}.png"; frames[index].Image.SavePng(Path.Combine(folder, file));
                var stop = index + 1 < frames.Count ? frames[index + 1].Time : end;
                concat.Add($"file '{file}'"); concat.Add("duration " + Math.Max(.001, (stop - frames[index].Time) / 1000.0).ToString("0.000000", CultureInfo.InvariantCulture));
            }
            if (frames.Count > 0) concat.Add($"file 'frame-{frames.Count - 1:D3}.png'");
            File.WriteAllLines(Path.Combine(folder, "frames.ffconcat"), concat);
            File.WriteAllText(Path.Combine(folder, "samples.json"), JsonSerializer.Serialize(new
            {
                Preview = true, FormalRunAcceptance = false, SavedState = "none", Start = start, End = end,
                Frames = frames.Count, FirstFrameDelayMs = frames.Count > 0 ? frames[0].Time - start : 0,
                FrameCapReached = frames.Count >= 600, Timestamps = frames.Select(frame => frame.Time).ToArray(),
                Audio = audioResult?.ToString(), Checks = _checks, Inputs = _inputs, Failure = failure,
                Fixture = "4 retained skill samples and 2 authored dummies; fixed 16x16 map; disposable view DTOs",
                Host = "960x540", InternalViewport = "1600x900", Video = "960x540", Input = "SubViewport.PushInput; no desktop cursor",
                Timing = "ffconcat observed time; trim FirstFrameDelayMs from audio before mux"
            }, new JsonSerializerOptions { WriteIndented = true }));
            foreach (var frame in frames) frame.Image.Dispose(); audio?.Dispose(); sound.Dispose();
        }
    }
    private async Task Screenshot(string file)
    {
        await Frames(2);
        // PNG compression runs after the interaction so recording never pauses
        // at a screenshot checkpoint.
        _screenshots.Add(file, _stage!.Viewport.GetTexture().GetImage());
    }
    private async Task Settle() { while (_motion.IsPlaying) await Frames(1); await Frames(3); }
    private async Task Pause(double seconds)
    { var end = Time.GetTicksMsec() + (ulong)(seconds * 1000); while (Time.GetTicksMsec() < end) await Frames(1); }
    private async Task Frames(int count)
    { for (var index = 0; index < count; index++) { await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); RenderingServer.ForceDraw(false); } }
    private async Task Click(Control target)
    {
        Require(target.IsVisibleInTree(), "点击目标可见。");
        var point = target.GetGlobalRect().GetCenter();
        _inputs.Add(new { Kind = "click", Target = target.GetPath().ToString(), Time = Time.GetTicksMsec() });
        _stage!.Push(new InputEventMouseMotion { Position = point, GlobalPosition = point }); await Frames(1);
        _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, ButtonMask = MouseButtonMask.Left, Pressed = true }); await Frames(1);
        _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left }); await Frames(3);
    }
    private async Task Key(Key key)
    {
        _inputs.Add(new { Kind = "key", Key = key.ToString(), Time = Time.GetTicksMsec() });
        _stage!.Push(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = true }); await Frames(1);
        _stage.Push(new InputEventKey { Keycode = key, PhysicalKeycode = key }); await Frames(3);
    }
    private static IEnumerable<T> Descendants<T>(Node node) where T : Node
    { foreach (var child in node.GetChildren()) { if (child is T match) yield return match; foreach (var nested in Descendants<T>(child)) yield return nested; } }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    public override void _ExitTree() { _motionBinding?.Dispose(); _feedbackBinding?.Dispose(); }
}
