using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Expedition;
using TowerAutobattler.Project;
using TowerAutobattler.UI;

public partial class ExpeditionJourneyInputSmoke
{
    private bool _v1DefeatObserved;

    // A deliberately weak formation is edited through native controls. The
    // natural draft, seed, resources and actual battle result remain untouched.
    // The caller must report sample-unavailable separately from input PASS.
    private async Task V1DefeatJourney()
    {
        var attempts = new List<object>();
        var excluded = new HashSet<string>(StringComparer.Ordinal);
        var status = "sample-unavailable";
        await OpenNewJourney();
        await AssertInitialPartyVisible();
        V1DefeatSave("opening");
        for (var attempt = 1; attempt <= 2; attempt++)
        {
            var site = await V1DefeatReachGuard(excluded);
            if (site is null) break;
            excluded.Add(site.SiteId);
            var kind = Definition.Sites.Single(value => value.StableId == site.DefinitionId).Kind;
            Require(kind is TowerNodeType.Combat or TowerNodeType.Elite, "败战片段只处理可见普通或精英守关，不打Boss");
            _step = $"v1 nonterminal defeat / actual attempt {attempt}";
            await SelectCell(site.Index);
            await Click(Screens.Expedition.GetNode<Button>("%InspectButton"));
            await Until(() => Screens.Deployment.IsVisibleInTree() && !Screens.IsTransitioning, "真实守关整备");
            await Frames(8);
            var run = State();
            var keep = run.Roster.Where(hero => run.Deployment.Contains(hero.InstanceId))
                .OrderBy(hero => V1Unit(hero.ContentId).MaxHealth + V1Unit(hero.ContentId).Armor * 30).First();
            foreach (var id in run.Deployment.Where(id => id.Length > 0 && id != keep.InstanceId))
            {
                var cell = Descendants<DeploymentCell>(Screens.Deployment).Single(value => value.PieceId == id);
                await ItemDrag(cell, Screens.Deployment.GetNode<Control>("%ReserveBench"));
                Require(!State().Deployment.Contains(id) && Descendants<DeploymentUnitCard>(Screens.Deployment).Any(value => value.InstanceId == id),
                    "原生拖回后备已实际保存，不能用直接领域命令制造弱队");
            }
            var before = State();
            Require(before.Deployment.Count(id => id.Length > 0) == 1 && before.Deployment.Contains(keep.InstanceId), "正式阵型只保留一名英雄");
            V1DefeatSave($"attempt-{attempt}-before");
            await Capture($"v1-defeat-{attempt}-one-hero.png");
            await Click(Screens.Deployment.GetNode<Button>("%StartBattleButton"));
            await Until(() => Screens.Battle.IsVisibleInTree() || Screens.BattleReport.IsVisibleInTree(), "真实单英雄开战", 15);
            Require(Mathf.IsEqualApprox(Screens.Battle.SpeedScale, 1), "真实败战片段保持正常速度");
            await RecordBattle(4);
            await Until(() => Screens.BattleReport.IsVisibleInTree(), "真实败战或胜利战报", 180);
            Require(Screens.Battle.LastRuntimeFailure.Length == 0, "真实战斗无运行故障");
            var result = Screens.Battle.TerminalResult ?? throw new InvalidOperationException("真实守关无实际战果");
            _outcome = result.Outcome.ToString();
            File.WriteAllText(Path.Combine(_output, $"v1-defeat-{attempt}-result.json"), JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
            await Capture($"v1-defeat-{attempt}-report.png");
            var after = _save.LoadActiveRun();
            attempts.Add(new { Attempt = attempt, site.SiteId, Kind = kind.ToString(), Outcome = result.Outcome.ToString(),
                HealthBefore = before.CurrentRunHealth, HealthAfter = after?.CurrentRunHealth,
                ActionsBefore = before.Expedition!.RemainingActions, ActionsAfter = after?.Expedition?.RemainingActions,
                Consequence = after?.LastBattleConsequence, ActualResult = true, SeedEdited = false, DomainSetupCommands = false });
            if (after is not null) V1DefeatSave($"attempt-{attempt}-settled");
            await Click(Screens.BattleReport.GetNode<Button>("%ReportContinue"));
            if (after is null)
            {
                await Until(() => Screens.Result.IsVisibleInTree(), "自然终局如实留证");
                status = "actual-terminal-no-nonterminal-sample";
                break;
            }
            Require(after.Expedition!.RemainingActions == before.Expedition!.RemainingActions - 1 && after.BattleNumber == before.BattleNumber + 1,
                "每次真实守关恰扣一次行动、实际结算一次");
            for (var group = 0; group < 8 && !Screens.Expedition.IsVisibleInTree(); group++)
            {
                await Until(() => Screens.Reward.IsVisibleInTree() || Screens.Expedition.IsVisibleInTree(), "真实战后奖励或地图");
                if (Screens.Reward.IsVisibleInTree()) await Click(Screens.Reward.GetNode<Button>("Center/Panel/Layout/ContinueButton"));
            }
            await Until(() => Screens.Expedition.IsVisibleInTree(), "真实报告继续回地图");
            if (result.Outcome != BattleOutcome.PlayerDefeat) continue;
            var rules = _game!.ProjectDefinition!.RunRules!;
            var loss = kind == TowerNodeType.Elite ? rules.EliteDefeatHealthLoss : rules.CombatDefeatHealthLoss;
            var settled = State();
            Require(settled.CurrentRunHealth == before.CurrentRunHealth - loss && settled.CurrentRunHealth > 0 &&
                settled.LastBattleConsequence is { Outcome: BattleOutcome.PlayerDefeat, RunEnded: false, IsBoss: false } &&
                settled.Roster.All(hero => hero.HealthRatio == 1), "实际非终局败战扣正式全局生命，英雄保留且下场满血");
            V1DefeatSave("before-cold-continue");
            await VerifyResume();
            V1DefeatSave("after-cold-continue");
            _v1DefeatObserved = true;
            status = "actual-nonterminal-defeat-cold-continue";
            Check("v1-real-nonterminal-defeat-report-menu-cold-continue-exact-save");
            break;
        }
        File.WriteAllText(Path.Combine(_output, "v1-defeat-evidence.json"), JsonSerializer.Serialize(new
        {
            Status = status, SampleObtained = _v1DefeatObserved, Attempts = attempts, PaidAttemptLimit = 2,
            NaturalOpening = true, SeedSelected = false, SaveEdited = false, OutcomeOverridden = false,
            Limits = "A one-hero diagnostic formation is not an ordinary-play balance claim. Victories, timeouts or missing opportunities do not count as a defeat sample."
        }, new JsonSerializerOptions { WriteIndented = true }));
        GD.Print($"V1_NONTERMINAL_DEFEAT_INPUT_RECORDED sample={_v1DefeatObserved} attempts={attempts.Count} output={_output}");
    }

    private async Task<ExpeditionCellView?> V1DefeatReachGuard(HashSet<string> excluded)
    {
        for (var attempt = 0; attempt < 64 && State().Expedition!.RemainingActions > 0; attempt++)
        {
            var map = Map(); var routes = KnownRoutes(map);
            var guards = map.Cells.Where(cell => cell.Revealed && cell.SiteId.Length > 0 && !cell.Cleared && !cell.Handled && !excluded.Contains(cell.SiteId))
                .Where(cell => Definition.Sites.Single(site => site.StableId == cell.DefinitionId).Kind is TowerNodeType.Combat or TowerNodeType.Elite)
                .Select(cell => (Cell: cell, Approach: Neighbors(map.Size, cell.Index).Append(cell.Index).Where(routes.ContainsKey)
                    .OrderBy(index => routes[index].Length).Select(index => (int?)index).FirstOrDefault()))
                .Where(value => value.Approach is not null)
                .OrderByDescending(value => Definition.Sites.Single(site => site.StableId == value.Cell.DefinitionId).Kind == TowerNodeType.Elite)
                .ThenBy(value => routes[value.Approach!.Value].Length).FirstOrDefault();
            if (guards.Cell is not null) { await MoveTo(guards.Approach!.Value); return guards.Cell; }
            var frontier = Frontier(map).FirstOrDefault(-1);
            if (frontier < 0) return null;
            await MoveTo(frontier);
        }
        return null;
    }

    private void V1DefeatSave(string label) => File.WriteAllText(Path.Combine(_output, "v1-defeat-" + label + "-save.json"),
        JsonSerializer.Serialize(State(), new JsonSerializerOptions { WriteIndented = true }));
}
