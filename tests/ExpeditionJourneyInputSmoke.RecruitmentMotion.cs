using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Run;
using TowerAutobattler.UI;

public partial class ExpeditionJourneyInputSmoke
{
    private async Task RecruitmentMotionJourney()
    {
        await EntranceSettled();
        _step = "recruitment motion / real keyboard discovery";
        var site = await KeyboardReachSite(TowerNodeType.Recruitment);
        await EntrancePause(.4);
        var before = State();
        var savedBefore = SavedRunText();
        var stock = before.Expedition!.Sites.Single(value => value.SiteId == site.SiteId).ServiceOffer!;
        await EntranceClip("01-enter-and-inspect", async () =>
        {
            await Key(Godot.Key.Enter);
            await Until(() => Screens.Recruitment.IsVisibleInTree(), "打开正式招募");
            await EntranceSettled();
            await EntrancePause(.55);
            var cards = RecruitmentCards(Screens.Recruitment);
            Require(cards.Length == 3 && cards.All(card => card.RecruitmentPresentation), "真实三名候选启用招募专属呈现。");
            Require(cards.Select(card => card.StableId).SequenceEqual(stock.Choices.Select(choice => choice.StableId)), "显示真实冻结库存。");
            AssertRecruitmentLayout(Screens.Recruitment, 3, "motion-three");
            await Click(cards[0]);
            await EntrancePause(.22);
            Require(Screens.Recruitment.SelectedRecruitmentChoiceId == cards[0].StableId && SavedRunText() == savedBefore,
                "卡面选择只检视，不招募不保存。");
            await Click(cards[1]);
            await EntrancePause(.4);
            Require(Screens.Recruitment.SelectedRecruitmentChoiceId == cards[1].StableId && SavedRunText() == savedBefore,
                "可更换所选卡且不提交。");
            var corePlate = cards[1].GetNode<Control>("Layout/Artwork/CoreReadingSurface").GetGlobalRect();
            foreach (var path in new[] { "Left/LifeSteal", "Left/Growth", "Right/Traits" })
            {
                var entry = cards[1].Orbit.GetNode<Control>(path);
                if (!entry.IsVisibleInTree()) continue;
                Require(entry.GetGlobalRect().End.Y <= corePlate.Position.Y,
                    $"完整详情的 {path} 底行不得被核心属性铭牌遮住：{entry.GetGlobalRect()} / {corePlate}。");
            }
        });
        await Capture("recruitment-selected.png");
        var roster = Screens.Recruitment.GetNode<Control>("Center/Panel/Layout/RecruitmentRoster");
        Require(roster.IsVisibleInTree() && _stage.Viewport.GetVisibleRect().Encloses(roster.GetGlobalRect()), "真实队伍承接条完整可见。");
        Check("recruitment-three-visible-real-roster-card-selection-read-only");
        await Click(Screens.Recruitment.GetNode<Button>("Center/Panel/Layout/ContinueButton"));
        await EntranceSettled();
        Require(Screens.Expedition.IsVisibleInTree() && SavedRunText() == savedBefore, "免费退出不改存档。");
        await Key(Godot.Key.Enter);
        await EntranceSettled();
        await EntrancePause(.45);
        var chosenCard = RecruitmentCards(Screens.Recruitment)[1];
        var contentId = stock.Choices.Single(choice => choice.StableId == chosenCard.StableId).ContentId;
        Require(SavedRunText() == savedBefore, "重开不改变库存与保存。");
        _step = "recruitment motion / committed receipt and repeated input";
        await EntranceClip("02-confirm-and-return", async () =>
        {
            await EntrancePause(.15);
            await Click(chosenCard.ConfirmButton);
            Require(Screens.Recruitment.IsRecruitmentReceiptPlaying, "成功后存在可观察归队动画。");
            var committed = State();
            var hero = committed.Roster.Single(value => !before.Roster.Any(old => old.InstanceId == value.InstanceId));
            Require(hero.ContentId == contentId && Screens.Recruitment.RecruitmentReceiptInstanceId == hero.InstanceId
                && committed.Expedition!.RemainingActions == before.Expedition.RemainingActions - 1,
                "动画期间真实新英雄已保存，费用已支付一次，目标对应新实例。");
            var savedCommitted = SavedRunText();
            // Deliberately bypass the helper's enabled-control and transition waits.
            var point = chosenCard.ConfirmButton.GetGlobalRect().GetCenter();
            _inputs.Add(new { kind = "receipt-repeat-click-and-enter", x = point.X, y = point.Y, ticks = Time.GetTicksMsec() });
            _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = true });
            _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = false });
            _stage.Push(new InputEventKey { Keycode = Godot.Key.Enter, PhysicalKeycode = Godot.Key.Enter, Pressed = true });
            _stage.Push(new InputEventKey { Keycode = Godot.Key.Enter, PhysicalKeycode = Godot.Key.Enter, Pressed = false });
            await Frames(2);
            Require(SavedRunText() == savedCommitted, "动画期间重复输入不重复扣费或招募。");
            await Until(() => Screens.Expedition.IsVisibleInTree() && !Screens.IsTransitioning, "归队结束返回地图");
            await EntrancePause(.4);
            Require(SavedRunText() == savedCommitted && !Screens.Recruitment.GetNode<OpeningSelectionLight>("ReceiptLight").IsPlaying,
                "返回保留已提交状态，离页清理全部归队粒子。");
        });
        await Capture("recruitment-after-return.png");
        Check("recruitment-committed-before-motion-real-instance-one-cost-repeat-blocked-clean-return");
        await VerifyResume();
        Require(State().Roster.Any(hero => hero.ContentId == contentId), "继续恢复保留正式招募英雄。");
    }
}
