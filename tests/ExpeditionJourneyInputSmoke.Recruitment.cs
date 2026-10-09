using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.Growth;
using TowerAutobattler.Project;
using TowerAutobattler.Run;
using TowerAutobattler.UI;

public partial class ExpeditionJourneyInputSmoke
{
    private readonly List<object> _recruitmentLayouts = [];

    private string SavedRunText() => File.ReadAllText(ProjectSettings.GlobalizePath($"user://{_namespace}/active_run.json"));

    private async Task RecruitmentJourney()
    {
        _step = "recruitment / keyboard discovery and Enter";
        var openingHeroes = State().Roster.Select(hero => hero.ContentId).ToHashSet(StringComparer.Ordinal);
        var site = await KeyboardReachSite(TowerNodeType.Recruitment);
        Require(Map().PositionIndex == site.Index, "真实键盘移动到招募地点，不能用命令直接打开招募。");
        var before = State();
        var savedBefore = SavedRunText();
        var stock = before.Expedition!.Sites.Single(value => value.SiteId == site.SiteId).ServiceOffer!;
        var candidateIds = stock.Choices.Select(choice =>
            choice.Operations.Single(operation => operation.Kind == RunOperationKind.Recruit).ContentId).ToArray();
        Require(candidateIds.Length == 3 && candidateIds.Distinct(StringComparer.Ordinal).Count() == 3 &&
            candidateIds.All(id => !openingHeroes.Contains(id)), "新局正式招募冻结三个不同且未在开局队伍中的英雄身份。");
        await Key(Godot.Key.Enter);
        await Until(() => Screens.Recruitment.IsVisibleInTree(), "当前位置 Enter 打开正式招募页面");
        await Frames(24);
        var cards = RecruitmentCards(Screens.Recruitment);
        Require(cards.Select(card => card.StableId).SequenceEqual(stock.Choices.Select(choice => choice.StableId)) &&
            cards.All(card => card.IsUnit && !card.ConfirmButton.Disabled), "实际三张英雄卡与冻结的可取得候选逐一一致。");
        var naturalWidth = AssertRecruitmentLayout(Screens.Recruitment, 3, "natural-three");
        var naturalScreenRect = Screens.Recruitment.GetRect();
        await ParkRecruitmentPointer();
        await Capture("recruitment-natural-three.png");
        Require(SavedRunText() == savedBefore, "Enter 预览不得写入存档或支付行动。");
        if (_recruitmentStyleOnly)
        {
            await InspectRecruitmentCard(cards[0], candidateIds[0], savedBefore);
            await RecruitmentSparseLayoutFixture(candidateIds, naturalWidth, naturalScreenRect);
            Require(SavedRunText() == savedBefore, "局部样式与阅读检查不招募、不改写自然存档。");
            Check("recruitment-style-only-no-acquisition-or-resume-replay");
            return;
        }
        var cancel = Screens.Recruitment.GetNode<Button>("Center/Panel/Layout/ContinueButton");
        Require(cancel.Text == "返回地图（不消耗行动）", "招募取消入口明确说明无行动费用。");
        await Click(cancel);
        await Until(() => Screens.Expedition.IsVisibleInTree(), "取消招募返回大陆");
        Require(SavedRunText() == savedBefore, "取消招募保留完整存档原文，包括行动、成长和冻结库存。");
        Check("recruitment-keyboard-enter-three-unique-unowned-cancel-byte-exact-save");

        _step = "recruitment / reopen same stock and recruit once";
        await Key(Godot.Key.Enter);
        await Until(() => Screens.Recruitment.IsVisibleInTree(), "Enter 重进同一招募地点");
        await Frames(12);
        cards = RecruitmentCards(Screens.Recruitment);
        Require(cards.Select(card => card.StableId).SequenceEqual(stock.Choices.Select(choice => choice.StableId)) &&
            JsonSerializer.Serialize(State().Expedition!.Sites.Single(value => value.SiteId == site.SiteId).ServiceOffer) ==
            JsonSerializer.Serialize(stock) && SavedRunText() == savedBefore,
            "重进的实际候选和冻结库存完全一致，不按查看次数重抽。");
        var chosen = stock.Choices.Single(choice => choice.StableId == cards[0].StableId);
        var recruitedId = chosen.Operations.Single(operation => operation.Kind == RunOperationKind.Recruit).ContentId;
        await InspectRecruitmentCard(cards[0], recruitedId, savedBefore);
        await Click(cards[0].ConfirmButton);
        await Until(() => Screens.Expedition.IsVisibleInTree(), "真实招募按钮确认后返回大陆");
        var after = State();
        Require(after.Expedition!.RemainingActions == before.Expedition.RemainingActions - 1 &&
            after.Roster.Count == before.Roster.Count + 1 && after.Roster.Count(hero => hero.ContentId == recruitedId) == 1 &&
            before.Roster.All(hero => after.Roster.Any(remaining => remaining.InstanceId == hero.InstanceId)) &&
            after.Expedition.Sites.Single(value => value.SiteId == site.SiteId).Handled && after.Action is null,
            "成功招募只增加选中的一名英雄，扣一次行动并关闭地点事务。");
        await ParkRecruitmentPointer();
        await Capture("recruitment-after-confirm.png");
        Check("recruitment-real-confirm-one-hero-one-action-return-map");
        await VerifyResume();
        Require(State().Roster.Any(hero => hero.ContentId == recruitedId) &&
            JsonSerializer.Serialize(State()) == JsonSerializer.Serialize(after), "重新启动并继续，招募后的英雄、库存和行动精确恢复。");

        // Separate visual fixture: the natural persisted run stays untouched.
        // Reduced candidate counts remain valid for a scarce pool or frozen old
        // stock; this never refills, migrates or edits that inventory.
        await RecruitmentSparseLayoutFixture(candidateIds, naturalWidth, naturalScreenRect);
    }

    private async Task RecruitmentSparseLayoutFixture(string[] candidateIds, float naturalWidth, Rect2 naturalScreenRect)
    {
        _step = "recruitment / controlled sparse-candidate UI fixture";
        var savedBefore = SavedRunText();
        var content = _game!.Content!;
        var compiled = GameProjectCompiler.Compile(_game.ProjectDefinition!, content.Graph).Project
            ?? throw new InvalidOperationException("共享招募布局夹具需要正式编译内容。");
        // Model binding only: no active run, acquisition or real save service.
        var modelApp = new RunApplication(content, new ExpeditionRunMemorySave(), compiled, loadExisting: false);
        var fixture = GD.Load<PackedScene>("res://scenes/ui/RecruitmentScreen.tscn").Instantiate<RewardScreenController>();
        fixture.Name = "SparseRecruitmentLayoutFixture";
        _game.AddChild(fixture);
        // Match the formal router's header/traits insets without invoking a
        // second gameplay router or enlarging the sparse fixture's canvas.
        fixture.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        fixture.Position = naturalScreenRect.Position;
        fixture.Size = naturalScreenRect.Size;
        Screens.Hide();
        try
        {
            fixture.GetNode<Control>("Center/Panel/Layout/ConvertButton").Hide();
            fixture.GetNode<Label>("Center/Panel/Layout/Hint").Text = "独立布局检查：不修改征程存档，不代表实际地点库存。";
            fixture.GetNode<Button>("Center/Panel/Layout/ContinueButton").Text = "布局检查";
            var grid = fixture.GetNode<RunOfferChoiceGrid>("Center/Panel/Layout/OfferBody/ChoiceScroll/CardCanvas/Choices");
            var threeWidth = 0f;
            foreach (var count in new[] { 3, 2, 1 })
            {
                fixture.GetNode<Label>("Center/Panel/Layout/Title").Text = $"共享招募布局 · {count} 名候选（受控夹具）";
                RunOfferCardBinder.SyncContent(grid, modelApp, candidateIds.Take(count), SemanticIcons.Catalog,
                    _ => throw new InvalidOperationException("布局夹具不能发出招募命令。"), "招募");
                await Frames(16);
                var width = AssertRecruitmentLayout(fixture, count, "controlled-" + count);
                if (count == 3)
                {
                    threeWidth = width;
                    Require(Math.Abs(width - naturalWidth) <= 2, "受控夹具与自然三选一共用正式卡片尺寸。");
                }
                else
                {
                    Require(Math.Abs(width - threeWidth) <= 2, "仅剩一、两名候选时不填满整行而横向拉伸。");
                    await ParkRecruitmentPointer();
                    await Capture($"recruitment-fixture-{count}-candidate.png");
                }
            }
            Require(SavedRunText() == savedBefore, "独立稀缺候选布局夹具不改写正式自然存档。");
            Check("controlled-one-two-candidate-layout-visible-centered-same-card-width-no-save-writes");
        }
        finally
        {
            fixture.QueueFree();
            Screens.Show();
        }
        await Frames(3);
    }

    private async Task InspectRecruitmentCard(RunOfferChoiceCard card, string contentId, string savedBefore)
    {
        _step = "recruitment / buttonless hover and keyboard inspection";
        Require(!card.HasNode("UnitDetailsButton"), "招募单位卡不再有独立详情按钮。");
        await ParkRecruitmentPointer();
        await HoverRecruitmentCard(card);
        Require(!card.Disclosure.IsPinned && SavedRunText() == savedBefore,
            "悬浮展开无按钮候选卡，仅阅读、不固定或招募。");
        await Capture("recruitment-hover-expanded.png");
        await Key(Godot.Key.Escape);
        await Until(() => !card.Disclosure.IsOpen, "Escape 关闭悬浮阅读");
        await ParkRecruitmentPointer();
        // Reach the authored focusable card through real navigation, without
        // GrabFocus or directly invoking disclosure methods from the test.
        await Key(Godot.Key.Tab);
        for (var index = 0; index < 80 && _stage.Viewport.GuiGetFocusOwner() != card; index++)
            await Key(Godot.Key.Tab);
        Require(_stage.Viewport.GuiGetFocusOwner() == card, "真实 Tab 能到达候选卡本体。");
        await Until(() => card.Disclosure.IsOpen, "键盘焦点展开候选属性与技能");
        await Key(Godot.Key.Enter);
        Require(Screens.Recruitment.IsVisibleInTree() && SavedRunText() == savedBefore,
            "卡片本体的键盘确认不会替代独立招募按钮或误招募。");
        await Capture("recruitment-keyboard-expanded.png");
        for (var index = 0; index < 80 && _stage.Viewport.GuiGetFocusOwner() != card.ConfirmButton; index++)
            await Key(Godot.Key.Tab);
        Require(_stage.Viewport.GuiGetFocusOwner() == card.ConfirmButton, "真实 Tab 到达该候选的招募按钮。");
        await ToSignal(GetTree().CreateTimer(.4), SceneTreeTimer.SignalName.Timeout);
        await Frames(2);
        Require(card.Disclosure.IsOpen && SavedRunText() == savedBefore,
            "焦点移到卡内招募按钮并经过关闭宽限后，属性仍展开且未误招募。");
        var keyboardEntry = Descendants<CompactDetailFact>(card.Orbit).First(fact => fact.IsVisibleInTree());
        for (var index = 0; index < 80 && _stage.Viewport.GuiGetFocusOwner() != keyboardEntry; index++)
            await Key(Godot.Key.Tab);
        Require(_stage.Viewport.GuiGetFocusOwner() == keyboardEntry && card.Disclosure.IsOpen && SavedRunText() == savedBefore,
            "从招募按钮继续 Tab 能到达可见属性条目，后代焦点保持展开且不改存档。");
        Check("recruitment-descendant-keyboard-focus-confirm-delay-orbit-preserves-disclosure");
        var growthButton = card.Orbit.GetNode<Button>("Left/Growth");
        var traitsButton = card.Orbit.GetNode<Button>("Right/Traits");
        Require(growthButton.IsVisibleInTree() && traitsButton.IsVisibleInTree(), "展开后保留正式成长与羁绊入口。");
        var content = _game!.Content!;
        Require(content.TryGet(contentId, out var entry), "候选英雄仍在正式已发布内容中。");
        var definition = (UnitDefinition)entry.Definition;
        var growth = GrowthPresentation.Project(contentId, BattleSetupFactory.Snapshot(entry, content), GrowthContentPackage.LoadRules(content));
        Require(growth.HasGrowthPlan, "当前正式候选存在可核对的成长规则。");
        var expectedTraits = UnitSemanticFacts.Traits(definition.Faction, definition.Tags).Select(fact => fact.Text).ToArray();
        Require(expectedTraits.Length > 0, "当前正式候选存在可核对的羁绊贡献。");
        var growthHint = await HoverRecruitmentEntry(growthButton);
        Require(growthHint.GetNode<Label>("%Title").Text == growth.Title &&
            growthHint.GetNode<CombatRichText>("%SingleEntry").Text.Contains(growth.Description, StringComparison.Ordinal),
            "成长提示对应正式内容中的生产与升阶规则。");
        var traitsHint = await HoverRecruitmentEntry(traitsButton);
        Require(expectedTraits.All(trait => traitsHint.GetNode<CombatRichText>("%SingleEntry").Text.Contains(trait, StringComparison.Ordinal)),
            "羁绊提示包含该英雄实际阵营与标签的贡献。");
        // Stay over the artwork while hints close; moving to the background
        // would correctly close this transient, unpinned disclosure too.
        await HoverRecruitmentCard(card);
        await Until(() => !Descendants<BattleLabTooltip>(_stage.Viewport).Any(RecruitmentTooltipVisible), "离开条目但保持卡片悬浮，提示关闭", 3);
        await Capture("recruitment-details-expanded.png");
        var skill = card.Orbit.GetNode<Control>("TopDock/Skills").GetChildren().OfType<Button>().First(button => button.IsVisibleInTree());
        var skillHint = await HoverRecruitmentEntry(skill);
        var expectedSkill = new UnitInformation(contentId, definition, growth.Snapshot).Skills.First();
        Require(skillHint.GetNode<Label>("%Title").Text == expectedSkill.Name &&
            !string.IsNullOrWhiteSpace(skillHint.GetNode<CombatRichText>("%SingleEntry").Text),
            "真实悬浮技能图标显示对应技能名称与规则。");
        await Capture("recruitment-skill-tooltip.png");
        await Key(Godot.Key.Escape);
        await ParkRecruitmentPointer();
        await Until(() => !card.Disclosure.IsOpen, "Escape 关闭详情后再招募");
        Require(SavedRunText() == savedBefore, "展开、悬浮阅读和关闭详情不招募、不修改存档。");
        Check("recruitment-buttonless-hover-tab-enter-no-recruit-growth-traits-skill-escape");
    }

    private async Task HoverRecruitmentCard(RunOfferChoiceCard card)
    {
        var point = card.GetNode<Control>("Layout/Artwork").GetGlobalRect().GetCenter();
        _inputs.Add(new { kind = "hover-card", target = card.GetPath().ToString(), x = point.X, y = point.Y, ticks = Time.GetTicksMsec() });
        _stage.Push(new InputEventMouseMotion { Position = point, GlobalPosition = point });
        await Until(() => card.Disclosure.IsOpen, "真实悬浮卡片展开", 3);
        await Frames(16);
    }

    private async Task<BattleLabTooltip> HoverRecruitmentEntry(Control target)
    {
        var point = target.GetGlobalRect().GetCenter();
        Require(target.IsVisibleInTree() && _stage.Viewport.GetVisibleRect().HasPoint(point), "实际悬浮目标位于可用屏内。");
        _inputs.Add(new { kind = "hover", target = target.GetPath().ToString(), x = point.X, y = point.Y, ticks = Time.GetTicksMsec() });
        _stage.Push(new InputEventMouseMotion { Position = point, GlobalPosition = point });
        await Until(() => Descendants<BattleLabTooltip>(target).Any(RecruitmentTooltipVisible), "真实鼠标悬浮显示条目提示", 3);
        await Frames(3);
        return Descendants<BattleLabTooltip>(target).Single(RecruitmentTooltipVisible);
    }

    private async Task ParkRecruitmentPointer()
    {
        var point = _stage.Viewport.GetVisibleRect().End - new Vector2(8, 8);
        _inputs.Add(new { kind = "click-blank", x = point.X, y = point.Y, ticks = Time.GetTicksMsec() });
        _stage.Push(new InputEventMouseMotion { Position = point, GlobalPosition = point });
        await Frames(1);
        // Keyboard-focused hints legitimately survive pointer leave. A real
        // background click dismisses them through the production input handler.
        _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point,
            ButtonIndex = MouseButton.Left, ButtonMask = MouseButtonMask.Left, Pressed = true });
        await Frames(1);
        _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point,
            ButtonIndex = MouseButton.Left, Pressed = false });
        await Frames(24);
        await Until(() => !Descendants<BattleLabTooltip>(_stage.Viewport).Any(RecruitmentTooltipVisible), "真实空白点击后可见提示关闭", 3);
    }

    private static bool RecruitmentTooltipVisible(BattleLabTooltip hint) =>
        hint.Visible && hint.GetNode<Control>("%TooltipPanel").IsVisibleInTree();

    private float AssertRecruitmentLayout(Control screen, int count, string scope)
    {
        var cards = RecruitmentCards(screen);
        Require(cards.Length == count, scope + " 的实际可见候选数量正确。");
        var scroll = screen.GetNode<ScrollContainer>("Center/Panel/Layout/OfferBody/ChoiceScroll");
        var visible = scroll.GetGlobalRect();
        var window = _stage.Viewport.GetVisibleRect();
        Require(scroll.ScrollHorizontal == 0 && scroll.ScrollVertical == 0, scope + " 无需测试自动滚动修复可见区域。");
        foreach (var card in cards)
        {
            var rect = card.GetGlobalRect();
            var frame = card.GetNode<Control>("CardFrame").GetGlobalRect();
            // Recruitment lift moves its empty outer gutters; assert the
            // authored visible frame/action instead of invisible card padding.
            var paintedRect = card.RecruitmentPresentation ? frame : rect;
            Require(visible.Grow(2).Encloses(paintedRect) && window.Encloses(paintedRect) &&
                visible.Grow(2).Encloses(card.ConfirmButton.GetGlobalRect()), scope + " 的完整卡片和招募按钮均在当前可用屏内。");
            Require(rect.Size.X > 300 && rect.Size.Y > rect.Size.X &&
                Math.Abs(frame.Size.X / frame.Size.Y - 2f / 3f) < .04f,
                scope + " 保持竖版灰石框约 2:3，不变成横向宽面板。");
            _recruitmentLayouts.Add(new
            {
                Scope = scope, CandidateCount = count, card.StableId,
                X = rect.Position.X, Y = rect.Position.Y, Width = rect.Size.X, Height = rect.Size.Y,
                FrameWidth = frame.Size.X, FrameHeight = frame.Size.Y,
                ScrollX = visible.Position.X, ScrollY = visible.Position.Y,
                ScrollWidth = visible.Size.X, ScrollHeight = visible.Size.Y
            });
        }
        for (var index = 1; index < cards.Length; index++)
            Require(Math.Abs(cards[index].Position.Y - cards[0].Position.Y) < 2 &&
                cards[index].GetNode<Control>("CardFrame").GetGlobalRect().Position.X >=
                cards[index - 1].GetNode<Control>("CardFrame").GetGlobalRect().End.X,
                scope + " 候选并排显示，无换行遮挡或相互重叠。");
        var layoutTransform = cards[0].GetParent<Control>().GetGlobalTransform();
        var groupLeft = (layoutTransform * cards[0].Position).X;
        var groupRight = (layoutTransform * (cards[^1].Position + new Vector2(cards[^1].Size.X, 0))).X;
        Require(Math.Abs((groupLeft + groupRight) / 2 - visible.GetCenter().X) < 3,
            scope + " 候选整体在可用区域内居中。");
        var cancel = screen.GetNode<Control>("Center/Panel/Layout/ContinueButton");
        Require(cancel.IsVisibleInTree() && window.Encloses(cancel.GetGlobalRect()), scope + " 返回入口在屏内。");
        return cards.Average(card => card.Size.X);
    }

    private static RunOfferChoiceCard[] RecruitmentCards(Node screen) => Descendants<RunOfferChoiceCard>(screen)
        .Where(card => card.IsVisibleInTree() && !card.IsQueuedForDeletion())
        .OrderBy(card => card.GlobalPosition.X).ToArray();
}
