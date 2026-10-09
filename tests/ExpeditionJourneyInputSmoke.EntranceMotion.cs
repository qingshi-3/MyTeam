using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Run;
using TowerAutobattler.UI;

public partial class ExpeditionJourneyInputSmoke
{
    private readonly List<object> _entranceClips = [];

    private async Task EntranceMotionJourney()
    {
        _step = "entrance / main menu to opening";
        await EntranceSettled();
        await Capture("01-main-menu.png");
        await EntranceClip("01-menu-opening", async () =>
        {
            await EntrancePause(.25);
            await Click(Screens.MainMenu.GetNode<Button>("Center/Panel/Menu/NewRunButton"));
            await Until(() => Screens.HeroSelection.IsVisibleInTree(), "开局选人进入");
            await EntranceSettled();
            await EntrancePause(.8); // The six authored card entrances finish after the page transition.
        });
        var cards = Descendants<HeroLibraryTile>(Screens.HeroSelection).Where(card => card.IsVisibleInTree()).ToArray();
        Require(cards.Length == 6, "开局显示六名正式候选。");
        var rows = cards.Select(card => Math.Round(card.GetGlobalRect().Position.Y / 12)).Distinct().Count();
        var columns = cards.Select(card => Math.Round(card.GetGlobalRect().Position.X / 12)).Distinct().Count();
        Require(rows == 2 && columns == 3, $"六名候选应排成两排三张，当前 {rows} 排 {columns} 列。");
        foreach (var card in cards)
            Require(_stage.Viewport.GetVisibleRect().Encloses(card.GetGlobalRect()), "开局整卡在视口内：" + card.StableId);
        await Capture("02-opening-six.png");

        _step = "entrance / two selections, cancel seat, replace";
        await EntranceClip("02-select-cancel-replace", async () =>
        {
            await Click(cards[0]); await EntrancePause(.28);
            await Click(cards[1]); await EntrancePause(.28);
            Require(State().OpeningRecruitment!.SelectedIds.Length == 2, "真实点选保存两人。");
            var seat = Descendants<OpeningHeroSeat>(Screens.HeroSelection).First(item => item.StableId == cards[0].StableId);
            await Click(seat); await EntrancePause(.28);
            Require(State().OpeningRecruitment!.SelectedIds.SequenceEqual(new[] { cards[1].StableId }), "席位取消只移除对应英雄。");
            await Click(cards[2]); await EntrancePause(.45);
            Require(State().OpeningRecruitment!.SelectedIds.SequenceEqual(new[] { cards[1].StableId, cards[2].StableId }), "补位顺序与正式开局事务一致。");
        });
        await Capture("03-opening-two-selected.png");
        Check("entrance-six-2x3-real-select-cancel-replace");

        _step = "entrance / depart to continent";
        await EntranceClip("03-depart-map", async () =>
        {
            await Click(Screens.HeroSelection.GetNode<Button>("%ConfirmOpening"));
            await Until(() => Screens.Expedition.IsVisibleInTree(), "出发进入大陆", 30);
            await EntranceSettled();
            await EntrancePause(.45);
        });
        Require(State().OpeningRecruitment is null && State().Roster.Count == 2, "出发成功后保存二人队伍。");
        await AssertInitialPartyVisible();
        await Capture("04-map-arrival.png");
        Check("entrance-confirm-to-formal-map");

        _step = "entrance / keyboard movement and fog reveal";
        var knownBefore = Map().Cells.Count(cell => cell.Revealed);
        await EntranceClip("04-walk-reveal", async () =>
        {
            var moves = 0;
            while (moves < 8)
            {
                var map = Map();
                var frontier = Frontier(map).FirstOrDefault(-1);
                Require(frontier >= 0, "地图存在已知可达探索边缘。");
                var route = KnownRoutes(map)[frontier];
                Require(route.Length > 0, "探索边缘需要真实行走。");
                foreach (var next in route.Take(8 - moves))
                {
                    await KeyboardStep(next);
                    moves++;
                    await EntrancePause(.08);
                }
                if (moves >= 3 && Map().Cells.Count(cell => cell.Revealed) > knownBefore) break;
            }
            await EntrancePause(.4);
        });
        Require(Map().Cells.Count(cell => cell.Revealed) > knownBefore, "键盘逐格移动实际揭开新迷雾。");
        await Capture("05-map-explored.png");
        Check("entrance-keyboard-steps-reveal-fog-without-action-cost");

        // Navigation is intentionally outside the short recording: use only
        // already revealed roads and reach one nearby service, never fight.
        _step = "entrance / reach camp";
        var camp = await KeyboardReachSite(TowerNodeType.Rest);
        await EntrancePause(.4);
        await Click(MapCell(camp.Index));
        await Capture("06-camp-detail.png");
        var beforeService = JsonSerializer.Serialize(State());
        var scroll = Screens.Expedition.GetNode<ScrollContainer>("%MapScroll");
        var cameraBefore = new Vector2I(scroll.ScrollHorizontal, scroll.ScrollVertical);
        var zoomBefore = Screens.Expedition.GetNode<Label>("%ZoomLabel").Text;
        _step = "entrance / camp preview and return";
        await EntranceClip("05-camp-return", async () =>
        {
            await Key(Godot.Key.Enter);
            await Until(() => Screens.Reward.IsVisibleInTree(), "回车打开营地预览");
            await EntranceSettled();
            await EntrancePause(.4);
            await Key(Godot.Key.W);
            Require(JsonSerializer.Serialize(State()) == beforeService, "服务页按键不能透传到底层地图。");
            await Click(Screens.Reward.GetNode<Button>("Center/Panel/Layout/ContinueButton"));
            await Until(() => Screens.Expedition.IsVisibleInTree(), "服务取消返回大陆");
            await EntranceSettled();
            await EntrancePause(.35);
        });
        Require(JsonSerializer.Serialize(State()) == beforeService, "营地预览和取消保持完整存档，不扣行动。");
        Require(new Vector2I(scroll.ScrollHorizontal, scroll.ScrollVertical) == cameraBefore &&
            Screens.Expedition.GetNode<Label>("%ZoomLabel").Text == zoomBefore,
            "返回地图保留原视角和缩放。");
        await Capture("07-map-after-service.png");
        Check("entrance-service-cancel-preserves-save-camera-zoom");

        _step = "entrance / menu continue";
        await EntranceClip("06-menu-continue", async () =>
        {
            await Click(Screens.Expedition.GetNode<Button>("%MenuButton"));
            await Until(() => Screens.MainMenu.IsVisibleInTree(), "地图返回主菜单");
            await EntranceSettled();
            await Key(Godot.Key.W);
            Require(JsonSerializer.Serialize(State()) == beforeService, "主菜单移动键不透传地图。");
            await Click(Screens.MainMenu.GetNode<Button>("Center/Panel/Menu/ContinueButton"));
            await Until(() => Screens.Expedition.IsVisibleInTree(), "继续返回原地图");
            await EntranceSettled();
            await EntrancePause(.35);
        });
        Require(JsonSerializer.Serialize(State()) == beforeService, "菜单继续保持完整存档。");
        await Capture("08-map-continued.png");
        Check("entrance-menu-continue-preserves-save");
    }

    private async Task EntranceSettled()
    {
        await Until(() => !Screens.IsTransitioning, "等待正式页面转场结束", 12);
        await Frames(3);
    }

    private async Task EntrancePause(double seconds)
    {
        var until = Time.GetTicksMsec() + (ulong)(seconds * 1000);
        while (Time.GetTicksMsec() < until) await Frames(1);
    }

    private async Task EntranceClip(string name, Func<Task> interaction)
    {
        var frames = new List<(Image Image, ulong Time)>();
        var folder = Path.Combine(_output, name);
        Directory.CreateDirectory(folder);
        var bus = AudioServer.GetBusIndex("Master");
        var slot = AudioServer.GetBusEffectCount(bus);
        var sound = new AudioEffectRecord { Format = AudioStreamWav.FormatEnum.Format16Bits };
        AudioServer.AddBusEffect(bus, sound);
        var start = Time.GetTicksMsec();
        sound.SetRecordingActive(true);
        string? failure = null;
        try
        {
            var action = interaction();
            ulong previous = 0;
            while (!action.IsCompleted)
            {
                await Frames(1);
                var now = Time.GetTicksMsec();
                if (now - previous < (_mapLightOnly || _recruitmentMotionOnly ? 33UL : 66UL) || frames.Count >= (_mapLightOnly || _recruitmentMotionOnly ? 360 : 180)) continue;
                previous = now;
                var image = _stage.Viewport.GetTexture().GetImage();
                image.Resize(960, 540, Image.Interpolation.Lanczos);
                frames.Add((image, now));
            }
            await action;
        }
        catch (Exception exception) { failure = exception.ToString(); throw; }
        finally
        {
            sound.SetRecordingActive(false);
            var end = Time.GetTicksMsec();
            var audio = sound.GetRecording();
            var audioResult = audio?.SaveToWav(Path.Combine(folder, "audio.wav"));
            AudioServer.RemoveBusEffect(bus, slot);
            var concat = new List<string> { "ffconcat version 1.0" };
            try
            {
                for (var index = 0; index < frames.Count; index++)
                {
                    var file = $"frame-{index:D3}.png";
                    frames[index].Image.SavePng(Path.Combine(folder, file));
                    // Account for every elapsed interval, including expensive
                    // texture readback. No speedup from assumed fixed FPS.
                    var stop = index + 1 < frames.Count ? frames[index + 1].Time : end;
                    var duration = Math.Max(.001, (stop - frames[index].Time) / 1000.0);
                    concat.Add($"file '{file}'");
                    concat.Add("duration " + duration.ToString("0.000000", CultureInfo.InvariantCulture));
                }
                if (frames.Count > 0) concat.Add($"file 'frame-{frames.Count - 1:D3}.png'");
                File.WriteAllLines(Path.Combine(folder, "frames.ffconcat"), concat);
                var evidence = new
                {
                    Name = name, Start = start, End = end, Frames = frames.Count,
                    FirstFrameDelayMs = frames.Count > 0 ? frames[0].Time - start : 0,
                    Timestamps = frames.Select(frame => frame.Time).ToArray(),
                    FrameCapReached = frames.Count >= (_mapLightOnly ? 360 : 180), Audio = audioResult?.ToString() ?? "unavailable",
                    Failure = failure,
                    Timing = "ffconcat preserves observed elapsed time; audio starts at Start; shift audio by FirstFrameDelayMs when muxing",
                    Input = "SubViewport.PushInput only", Host = "960x540", Content = "1600x900", Video = "960x540"
                };
                _entranceClips.Add(evidence);
                File.WriteAllText(Path.Combine(folder, "samples.json"), JsonSerializer.Serialize(evidence, new JsonSerializerOptions { WriteIndented = true }));
            }
            finally
            {
                foreach (var frame in frames) frame.Image.Dispose();
                audio?.Dispose();
                sound.Dispose();
            }
        }
    }
}
