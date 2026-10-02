using System;
using TowerAutobattler.Battle;
using TowerAutobattler.BattleLab;
using TowerAutobattler.Composition;
using TowerAutobattler.Presentation;
using TowerAutobattler.Run;
using TowerAutobattler.UI;

namespace TowerAutobattler.App;

// Owns only the disposable laboratory session and its frozen reset configuration.
// It never settles a Run or holds the campaign's pending battle result.
internal sealed class BattleLabFlow : IDisposable
{
    private readonly Func<RunApplication?> _application;
    private readonly BattleLabScreenController _lab;
    private readonly BattleScreenController _battle;
    private readonly BattleReportScreen _report;
    private readonly Action<AppScreenId> _show;
    private readonly Action _showMainMenu;
    private readonly Action<string, string> _showResult;
    private BattleLabContentIndex? _battleLabIndex;
    private BattleLabPresetStore? _battleLabPresets;
    private BattleLabSession? _battleLabSession;
    private BattleLabStartSnapshot? _battleLabStartSnapshot;
    private BattleResult? _battleLabResult;
    private bool _battleLabBattleActive;
    private bool _battleLabReportShown;

    internal BattleLabFlow(Func<RunApplication?> application, BattleLabScreenController lab,
        BattleScreenController battle, BattleReportScreen report, Action<AppScreenId> show,
        Action showMainMenu, Action<string, string> showResult)
    {
        _application = application;
        _lab = lab;
        _battle = battle;
        _report = report;
        _show = show;
        _showMainMenu = showMainMenu;
        _showResult = showResult;
    }
    private RunApplication App => _application() ?? throw new InvalidOperationException("Run is not ready.");
    internal bool BattleActive => _battleLabBattleActive;
    internal void AcceptBattleResult(BattleResult result)
    {
        if (_battleLabBattleActive) _battleLabResult ??= result;
    }
    internal void ShowBattleReport()
    {
        if (!_battleLabBattleActive || _battleLabReportShown || _battleLabResult is null) return;
        _battleLabReportShown = true;
        _battle.StopBattle(replacement: false);
        _show(AppScreenId.BattleReport);
        _report.Bind(_battleLabResult, "战斗实验室", App.Content, "返回原配置");
    }
    public void Dispose()
    {
        if (_battleLabBattleActive) _battle.StopBattle();
        _battleLabBattleActive = false;
        _battleLabResult = null;
        _battleLabStartSnapshot = null;
        _battleLabSession = null;
        _battleLabIndex = null;
        _battleLabPresets = null;
    }

    internal void ShowBattleLab()
    {
        try
        {
            EnsureBattleLabSession();
            _lab.Bind(_battleLabIndex!, _battleLabSession!, _battleLabPresets);
            _show(AppScreenId.BattleLab);
        }
        catch (Exception exception)
        {
            _showResult("战斗实验室不可用", exception.Message);
        }
    }

    private void EnsureBattleLabSession()
    {
        if (_battleLabSession is not null) return;
        _battleLabIndex = new BattleLabContentIndex(new CompiledGamePackage(
            App.Content,
            App.Project,
            App.Content.PublicationVersion));
        _battleLabPresets = new BattleLabPresetStore(_lab.PresetCatalog);
        if (!_battleLabPresets.TryLoad(_battleLabPresets.DefaultPresetName, out var preset))
            throw new InvalidOperationException("默认战斗实验室预设无法读取。");
        var snapshot = BattleLabPresetStore.ToSnapshot(preset);
        _battleLabSession = new BattleLabSession(
            _battleLabIndex,
            snapshot.CurrentPopulation,
            snapshot.Seed,
            snapshot.Mode,
            snapshot.FloorRuleId);
        _battleLabSession.Restore(snapshot);
    }

    internal void StartBattleLabBattle()
    {
        if (_battleLabIndex is null || _battleLabSession is null) return;
        try
        {
            _battleLabStartSnapshot = _battleLabSession.Freeze();
            _battleLabResult = null;
            _battleLabReportShown = false;
            _battleLabBattleActive = true;
            var config = new BattleLabPreparationAdapter(_battleLabIndex).Build(_battleLabStartSnapshot);
            _show(AppScreenId.Battle);
            _battle.StartBattle(App.Content, config, "战斗实验室", 1f);
            _battle.SetLabControlsVisible(true);
        }
        catch (Exception exception)
        {
            _battle.StopBattle();
            _battleLabBattleActive = false;
            _lab.ShowFeedback("无法开始实验：" + exception.Message, false);
            _show(AppScreenId.BattleLab);
        }
    }

    internal void ResetBattleLabBattle()
    {
        if (!_battleLabBattleActive || _battleLabIndex is null || _battleLabStartSnapshot is null) return;
        try
        {
            _battleLabResult = null;
            _battleLabReportShown = false;
            var config = new BattleLabPreparationAdapter(_battleLabIndex).Build(_battleLabStartSnapshot);
            _battle.StartBattle(App.Content, config, "战斗实验室", 1f);
            _battle.SetLabControlsVisible(true);
        }
        catch (Exception exception)
        {
            _battle.StopBattle();
            ReturnToBattleLabConfiguration();
            _lab.ShowFeedback("重置失败：" + exception.Message, false);
        }
    }

    internal void ReturnToBattleLabConfiguration()
    {
        if (!_battleLabBattleActive && _battleLabSession is null) return;
        _battle.StopBattle();
        _battleLabBattleActive = false;
        _battleLabResult = null;
        _battleLabReportShown = false;
        _battleLabStartSnapshot = null;
        if (_battleLabIndex is null || _battleLabSession is null) { _showMainMenu(); return; }
        _lab.Bind(_battleLabIndex, _battleLabSession, _battleLabPresets);
        _show(AppScreenId.BattleLab);
    }
}
