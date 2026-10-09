using System;
using System.Linq;
using System.Collections.Generic;
using Godot;
using TowerAutobattler.Components;
using TowerAutobattler.Content;
using TowerAutobattler.Project;
using TowerAutobattler.Run;

namespace TowerAutobattler.UI;

public partial class RewardScreenController : Control, IUiJourneySurface, IUiMotionHost
{
    // This page has no outer frame. Follow the nearest actual offer instead of
    // outlining its transparent full-page layout container.
    public Control? ResolveJourneySurface(Vector2 source) => _choices.GetChildren().OfType<Control>()
        .Where(card => card.IsVisibleInTree())
        .OrderBy(card => card.GetGlobalRect().GetCenter().DistanceSquaredTo(source)).FirstOrDefault();
    public event Action<string>? ChoiceRequested;
    public event Action? ConvertRequested;
    public event Action? ContinueRequested;

    private Label _title = null!;
    private Label _hint = null!;
    private Container _choices = null!;
    private Button _convert = null!;
    private Button _continue = null!;
    private Control _offerBody = null!;
    private bool _claimed;
    public Func<bool> ReduceUiMotion { get; set; } = () => false;
    public Action<string>? PlaySound { get; set; }
    [Export] public PackedScene RecruitmentRosterPortraitScene { get; set; } = null!;
    public bool IsRecruitmentReceiptPlaying { get; private set; }
    public string SelectedRecruitmentChoiceId { get; private set; } = string.Empty;
    public string RecruitmentReceiptInstanceId { get; private set; } = string.Empty;
    private bool _recruitment;
    private HashSet<string> _rosterBefore = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _recruitmentContent = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Control> _rosterPortraits = new(StringComparer.Ordinal);
    private HBoxContainer _roster = null!;
    private HBoxContainer _rosterRow = null!;
    private ScrollContainer _rosterScroll = null!;
    private Label _rosterCounts = null!;
    private Control _receiptBlocker = null!;
    private UnitPortrait _receiptPortrait = null!;
    private OpeningSelectionLight _receiptLight = null!;
    private Control? _receiptDestination;
    private Tween? _receipt;
    private Action? _receiptFinished;
    private int _receiptGeneration;


    public override void _Ready()
    {
        _title = GetNode<Label>("Center/Panel/Layout/Title");
        _hint = GetNode<Label>("Center/Panel/Layout/Hint");
        _choices = GetNode<Container>("Center/Panel/Layout/OfferBody/ChoiceScroll/CardCanvas/Choices");
        _offerBody = GetNode<Control>("Center/Panel/Layout/OfferBody");
        _convert = GetNode<Button>("Center/Panel/Layout/ConvertButton");
        _continue = GetNode<Button>("Center/Panel/Layout/ContinueButton");
        _roster = GetNode<HBoxContainer>("Center/Panel/Layout/RecruitmentRoster");
        _rosterRow = GetNode<HBoxContainer>("Center/Panel/Layout/RecruitmentRoster/Scroll/Portraits");
        _rosterScroll = GetNode<ScrollContainer>("Center/Panel/Layout/RecruitmentRoster/Scroll");
        _rosterCounts = GetNode<Label>("Center/Panel/Layout/RecruitmentRoster/Summary/Counts");
        _receiptBlocker = GetNode<Control>("ReceiptBlocker");
        _receiptPortrait = GetNode<UnitPortrait>("ReceiptPortrait");
        _receiptLight = GetNode<OpeningSelectionLight>("ReceiptLight");
        VisibilityChanged += ReceiptVisibility;
        _convert.Pressed += OnConvert;
        _continue.Pressed += OnContinue;
    }

    public override void _ExitTree()
    {
        CancelRecruitmentReceipt();
        VisibilityChanged -= ReceiptVisibility;
        _convert.Pressed -= OnConvert;
        _continue.Pressed -= OnContinue;
    }

    public void BindRecruitment(
        RunApplication app,
        PackedScene unitTemplate,
        SemanticIconCatalog icons)
    {
        ResetPresentation();
        _claimed = false;
        _offerBody.Visible = true;
        _continue.Visible = true;
        _title.Text = "征募新兵";
        _hint.Text = "选择一位加入队伍。";
        RunOfferCardBinder.SyncContent(_choices, app, app.RecruitmentChoices().Select(entry => entry.StableId),
            icons, OnChoice, "招募");
        var conversion = CurrentHeroConversion(app);
        _convert.Visible = conversion > 0;
        _convert.Text = $"转为 {conversion} 金币";
        _continue.Text = "跳过征募";
        ConfigureRecruitment(app, true);
    }

    public void BindCombatReward(
        RunApplication app,
        PackedScene choiceTemplate,
        PackedScene itemTemplate,
        SemanticIconCatalog icons)
    {
        ResetPresentation();
        _claimed = false;
        _offerBody.Visible = true;
        _title.Text = "战斗胜利";
        _continue.Visible = true;
        _hint.Text = "选择一件战利品，补强你的队伍。";
        RunOfferCardBinder.SyncContent(_choices, app, app.ItemChoices(37).Select(entry => entry.StableId),
            icons, OnChoice, "领取");
        _convert.Visible = false;
        _continue.Text = "放弃战利品";
    }

    public void ShowClaimFailure() => _hint.Text = "战利品未能保存，尚未领取，请重试。";
    public void ShowDecisionMessage(string message) => _hint.Text = message;

    public void ShowTerminalFailure(string message)
    {
        ResetPresentation();
        _claimed = true;
        _title.Text = "征程结算待保存";
        _hint.Text = message;
        _offerBody.Visible = false;
        _convert.Visible = false;
        _continue.Visible = true;
        _continue.Text = "重试保存";
    }

    public void BindOffer(RunApplication app, PackedScene choiceTemplate, PackedScene itemTemplate, SemanticIconCatalog icons)
    {
        ResetPresentation();
        var offer = app.PendingOffer ?? throw new InvalidOperationException("No pending Run offer.");
        var expeditionService = app.IsExpedition && offer.Kind != RunOfferKind.CombatReward;
        _claimed = false;
        _offerBody.Visible = true;
        _title.Text = offer.DisplayName;
        _hint.Text = "选择一项带走。";
        if (offer.RewardCategory is RunRewardCategory.Equipment or RunRewardCategory.Relic)
        {
            var category = offer.RewardCategory == RunRewardCategory.Equipment ? "装备" : "遗物";
            var remaining = app.Run!.PendingRewardGroups.Length;
            _hint.Text = $"本组{category}选择一件；基础收益已到账。"
                + (remaining > 0 ? $"领取或跳过后，还有 {remaining} 组独立奖励。" : "这是本场最后一组奖励。");
        }
        if (offer.Kind == TowerAutobattler.Project.RunOfferKind.Rest)
            _hint.Text = $"全局生命 {app.Run!.CurrentRunHealth}/{app.Run.MaximumRunHealth} · 恢复生命与领取金币只能选择一项。";
        if (offer.Kind == TowerAutobattler.Project.RunOfferKind.Recruitment)
            _hint.Text = offer.Choices.IsEmpty ? (app.IsExpedition
                ? "该地点已无可取得的新伙伴，可以继续探索。"
                : "本阶段已无可招募的新英雄，可以继续前进。") :
                "选择一名英雄加入队伍。";
        RunOfferCardBinder.Sync(_choices, app, choiceTemplate, itemTemplate, icons, OnChoice);
        _convert.Visible = false;
        _continue.Visible = expeditionService || offer.AllowSkip;
        _continue.Text = expeditionService ? "返回地图（不消耗行动）" : app.RewardSkipText == "跳过"
            ? (offer.RewardCategory == RunRewardCategory.None ? "放弃此次机会，继续" : "跳过本组奖励")
            : app.RewardSkipText;
        _recruitmentContent.Clear();
        if (offer.Kind == RunOfferKind.Recruitment)
            foreach (var choice in offer.Choices) _recruitmentContent[choice.StableId] = choice.ContentId;
        ConfigureRecruitment(app, offer.Kind == RunOfferKind.Recruitment);
    }

    private void ResetPresentation()
    {
        CancelRecruitmentReceipt();
        _recruitment = false;
        SelectedRecruitmentChoiceId = RecruitmentReceiptInstanceId = string.Empty;
        if (_roster is not null) _roster.Hide();
        if (_choices is RunOfferChoiceGrid grid) grid.RecruitmentPresentation = false;
        foreach (var card in _choices.GetChildren().OfType<RunOfferChoiceCard>())
            card.ConfigureRecruitment(false);
        _continue.Disabled = _convert.Disabled = false;
    }

    private void ConfigureRecruitment(RunApplication app, bool enabled)
    {
        _recruitment = enabled;
        _roster.Visible = enabled;
        if (_choices is RunOfferChoiceGrid grid)
        { grid.RecruitmentPresentation = enabled; grid.RefreshLayout(); }
        foreach (var card in _choices.GetChildren().OfType<RunOfferChoiceCard>())
        { card.ReduceUiMotion = () => ReduceUiMotion(); card.ConfigureRecruitment(enabled, InspectRecruitment); }
        if (!enabled) return;
        _rosterBefore = app.Run is { } run
            ? run.Roster.Select(hero => hero.InstanceId).ToHashSet(StringComparer.Ordinal) : [];
        BindRoster(app);
        Callable.From(RefreshRecruitmentLayout).CallDeferred();
    }

    private void RefreshRecruitmentLayout()
    {
        if (_choices is RunOfferChoiceGrid grid) grid.RefreshLayout();
    }

    private void InspectRecruitment(string id)
    {
        if (!_recruitment || _claimed || IsRecruitmentReceiptPlaying) return;
        SelectedRecruitmentChoiceId = id;
        foreach (var card in _choices.GetChildren().OfType<RunOfferChoiceCard>())
            card.SetRecruitmentSelected(card.StableId == id);
    }

    private void BindRoster(RunApplication app)
    {
        var run = app.Run;
        if (run is null || RecruitmentRosterPortraitScene is null) return;
        var deployed = run.Deployment.Where(id => !string.IsNullOrEmpty(id)).ToHashSet(StringComparer.Ordinal);
        var deployedCount = run.Roster.Count(hero => deployed.Contains(hero.InstanceId));
        _rosterCounts.Text = $"上阵 {deployedCount} · 后备 {run.Roster.Count - deployedCount}";
        var keep = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < run.Roster.Count; index++)
        {
            var hero = run.Roster[index];
            keep.Add(hero.InstanceId);
            if (!_rosterPortraits.TryGetValue(hero.InstanceId, out var seat))
            {
                seat = RecruitmentRosterPortraitScene.Instantiate<Control>();
                _rosterRow.AddChild(seat);
                _rosterPortraits[hero.InstanceId] = seat;
            }
            _rosterRow.MoveChild(seat, index);
            var unit = app.Content.TryGet(hero.ContentId, out var entry) ? entry.Definition as UnitDefinition : null;
            var state = deployed.Contains(hero.InstanceId) ? "上阵" : "后备";
            seat.SetMeta("hero_instance_id", hero.InstanceId);
            seat.GetNode<UnitPortrait>("Portrait").Bind(unit?.Portrait, unit?.Icon);
            seat.GetNode<Label>("Name").Text = unit?.DisplayName ?? hero.ContentId;
            seat.GetNode<Label>("State").Text = state;
            seat.AccessibilityName = (unit?.DisplayName ?? hero.ContentId) + " · " + state;
        }
        foreach (var id in _rosterPortraits.Keys.Where(id => !keep.Contains(id)).ToArray())
        {
            var stale = _rosterPortraits[id]; _rosterPortraits.Remove(id);
            _rosterRow.RemoveChild(stale); stale.QueueFree();
        }
    }

    // Called only after the application has successfully committed its offer.
    // Business state already includes the new hero; this receipt never changes it.
    public void PresentRecruitmentAccepted(string choiceId, RunApplication app, Action finished)
    {
        CancelRecruitmentReceipt();
        _claimed = true;
        _receiptFinished = finished;
        IsRecruitmentReceiptPlaying = true;
        _continue.Disabled = _convert.Disabled = true;
        _receiptBlocker.Show();
        var source = _choices.GetChildren().OfType<RunOfferChoiceCard>()
            .FirstOrDefault(card => card.StableId == choiceId);
        var contentId = _recruitmentContent.GetValueOrDefault(choiceId, choiceId);
        var hero = app.Run?.Roster.FirstOrDefault(hero => !_rosterBefore.Contains(hero.InstanceId) && hero.ContentId == contentId);
        var sourceRect = source?.Portrait.GetGlobalRect() ?? default;
        foreach (var card in _choices.GetChildren().OfType<RunOfferChoiceCard>())
            card.PresentRecruitmentDeparture(card == source);
        BindRoster(app);
        if (!_recruitment || source is null || hero is null || !IsVisibleInTree() || ReduceUiMotion() ||
            !_rosterPortraits.TryGetValue(hero.InstanceId, out var destination) ||
            !app.Content.TryGet(hero.ContentId, out var entry) || entry.Definition is not UnitDefinition unit)
        { FinishRecruitmentReceipt(); return; }
        RecruitmentReceiptInstanceId = hero.InstanceId;
        _receiptDestination = destination.GetNode<Control>("Portrait");
        _receiptPortrait.Bind(unit.Portrait, unit.Icon);
        var sourceCenter = sourceRect.GetCenter();
        var generation = _receiptGeneration;
        // The authored roster container must settle before its new actual slot is sampled.
        Callable.From(() => StartRecruitmentReceipt(sourceCenter, sourceRect.Size, destination, generation)).CallDeferred();
    }

    private void StartRecruitmentReceipt(Vector2 sourceCenter, Vector2 sourceSize, Control destination, int generation)
    {
        if (generation != _receiptGeneration || !IsRecruitmentReceiptPlaying) return;
        if (!IsVisibleInTree()) { CancelRecruitmentReceipt(); return; }
        if (ReduceUiMotion()) { FinishRecruitmentReceipt(); return; }
        _rosterScroll.EnsureControlVisible(destination);
        // EnsureControlVisible updates scroll before deriving the destination in canvas space.
        var inverse = GetGlobalTransformWithCanvas().AffineInverse();
        var start = inverse * sourceCenter;
        var end = inverse * _receiptDestination!.GetGlobalRect().GetCenter();
        var bend = (start + end) * .5f + new Vector2(0, -Mathf.Min(80, start.DistanceTo(end) * .18f));
        _receiptDestination.Hide();
        _receiptPortrait.Size = _receiptDestination.Size;
        // Start as a substantial cutout of the actual card portrait, then settle
        // into the real roster medallion instead of moving an already tiny icon.
        var sourceLocalSize = inverse.BasisXform(sourceSize).Abs();
        var startScale = Mathf.Clamp(Mathf.Min(sourceLocalSize.X, sourceLocalSize.Y) * .68f /
            Mathf.Max(1, _receiptPortrait.Size.X), 1.12f, 2.4f);
        _receiptPortrait.Position = start - _receiptPortrait.Size * .5f;
        _receiptPortrait.PivotOffset = _receiptPortrait.Size * .5f;
        _receiptPortrait.Scale = Vector2.One * startScale;
        _receiptPortrait.Show();
        _receiptLight.Begin(start);
        PlaySound?.Invoke("journey_depart");
        _hint.Text = destination.AccessibilityName + " · 已加入队伍";
        _receipt = CreateTween().SetIgnoreTimeScale(true).SetPauseMode(Tween.TweenPauseMode.Process);
        _receipt.TweenMethod(Callable.From<float>(progress =>
        {
            var remaining = 1 - progress;
            var center = remaining * remaining * start + 2 * remaining * progress * bend + progress * progress * end;
            _receiptPortrait.Position = center - _receiptPortrait.Size * .5f;
            _receiptPortrait.Scale = Vector2.One * Mathf.Lerp(startScale, .94f, progress);
            _receiptLight.Track(center);
        }), 0f, 1f, .38).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        _receipt.TweenCallback(Callable.From(() =>
        {
            _receiptLight.Land(end);
            PlaySound?.Invoke("journey_land");
        }));
        _receipt.TweenProperty(_receiptPortrait, "scale", Vector2.One, .18)
            .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        _receipt.TweenInterval(.14);
        _receipt.TweenCallback(Callable.From(FinishRecruitmentReceipt));
    }

    public override void _Process(double delta)
    {
        if (IsRecruitmentReceiptPlaying && ReduceUiMotion()) FinishRecruitmentReceipt();
    }

    private void FinishRecruitmentReceipt()
    {
        var finished = _receiptFinished;
        var canFinish = IsInsideTree() && IsVisibleInTree();
        CancelRecruitmentReceipt();
        if (canFinish) finished?.Invoke();
    }

    private void CancelRecruitmentReceipt()
    {
        _receiptGeneration++;
        _receipt?.Kill(); _receipt = null;
        _receiptFinished = null;
        IsRecruitmentReceiptPlaying = false;
        _receiptLight?.Clear();
        _receiptPortrait?.Hide();
        if (_receiptDestination is not null && IsInstanceValid(_receiptDestination)) _receiptDestination.Show();
        _receiptDestination = null;
        _receiptBlocker?.Hide();
    }

    private void ReceiptVisibility()
    {
        if (!IsVisibleInTree()) CancelRecruitmentReceipt();
    }

    private static int CurrentHeroConversion(RunApplication app)
    {
        if (app.Run is not { Roster.Count: > 0 } run ||
            !app.Content.TryGet(run.Roster[0].ContentId, out var entry))
            return 0;
        var root = entry.Scene.Instantiate<UnitContentRoot>();
        try { return root.HeroRule?.RecruitConversionGold ?? 0; }
        finally { root.Free(); }
    }

    private void OnChoice(string stableId)
    {
        if (_claimed || IsRecruitmentReceiptPlaying) return;
        if (_recruitment) InspectRecruitment(stableId);
        ChoiceRequested?.Invoke(stableId);
    }
    private void OnConvert() { if (!IsRecruitmentReceiptPlaying && (!_recruitment || !_claimed)) ConvertRequested?.Invoke(); }
    private void OnContinue() { if (!IsRecruitmentReceiptPlaying && (!_recruitment || !_claimed)) ContinueRequested?.Invoke(); }
}
