using System;
using System.Collections.Generic;
using System.Linq;
using TowerAutobattler.Battle;

namespace TowerAutobattler.BattleLab;

internal sealed record BattleLabEditResult(bool Succeeded, string Message);

// Screen-independent edit history, preset identity and prepared read projections.
internal sealed class BattleLabEditorSession
{
    private sealed record History(BattleLabStartSnapshot Snapshot, string Label, string PresetName, string PresetDigest);
    private readonly List<History> _history = [];
    private readonly Dictionary<string, UnitSnapshot> _unitSnapshots = new(StringComparer.Ordinal);
    private BattleLabDerivedProjection? _derived;
    private string _derivedDigest = "";
    public BattleLabContentIndex Content { get; }
    public BattleLabSession Session { get; }
    public BattleLabPresetStore? Presets { get; }
    public string PresetName { get; private set; }
    public string PresetDigest { get; private set; }
    public int UndoCount => _history.Count;
    public string UndoLabel => _history.Count == 0 ? "" : _history[^1].Label;

    public BattleLabEditorSession(BattleLabContentIndex content, BattleLabSession session, BattleLabPresetStore? presets)
    {
        Content = content;
        Session = session;
        Presets = presets;
        PresetDigest = session.Freeze().CanonicalDigest;
        PresetName = presets?.BuiltIns.FirstOrDefault(entry =>
            BattleLabPresetStore.ToSnapshot(entry.Value).CanonicalDigest == PresetDigest).Key ?? "";
    }

    public BattleLabDerivedProjection Prepared()
    {
        var digest = Session.Freeze().CanonicalDigest;
        if (_derived is null || _derivedDigest != digest)
        {
            var prepared = BattleLabDerivedProjectionBuilder.Build(Session);
            _derived = prepared;
            _derivedDigest = digest;
        }
        return _derived;
    }

    public UnitSnapshot UnitSnapshotFor(BattleLabPublishedUnit content)
    {
        if (!_unitSnapshots.TryGetValue(content.StableId, out var snapshot))
        {
            snapshot = BattleSetupFactory.Snapshot(content.Entry, Content.Package.Content);
            _unitSnapshots.Add(content.StableId, snapshot);
        }
        return snapshot;
    }

    public void RecordChange(BattleLabStartSnapshot before, string label)
    {
        if (before.CanonicalDigest == Session.Freeze().CanonicalDigest) return;
        _history.Add(new(before, label, PresetName, PresetDigest));
        if (_history.Count > 32) _history.RemoveAt(0);
    }

    public BattleLabEditResult Edit(string label, Func<bool> mutation, string success, string failure)
    {
        var before = Session.Freeze();
        try
        {
            var succeeded = mutation();
            if (succeeded) RecordChange(before, label);
            return new(succeeded, succeeded ? success : failure);
        }
        catch (Exception exception) { return new(false, failure + "：" + exception.Message); }
    }

    public BattleLabEditResult Undo()
    {
        if (_history.Count == 0) return new(false, "暂无可撤销操作");
        var entry = _history[^1];
        try
        {
            Session.Restore(entry.Snapshot);
            _history.RemoveAt(_history.Count - 1);
            PresetName = entry.PresetName;
            PresetDigest = entry.PresetDigest;
            return new(true, $"已撤销：{entry.Label}。");
        }
        catch (Exception exception) { return new(false, "无法撤销：" + exception.Message); }
    }

    public BattleLabEditResult LoadPreset(string name)
    {
        if (Presets is null || !Presets.TryLoad(name, out var preset)) return new(false, "预设读取失败，当前配置未改变。");
        var before = Session.Freeze();
        try
        {
            Session.Restore(BattleLabPresetStore.ToSnapshot(preset));
            RecordChange(before, "载入预设");
            PresetName = name;
            PresetDigest = Session.Freeze().CanonicalDigest;
            return new(true, $"已载入“{name}”。");
        }
        catch (Exception exception) { return new(false, "无法载入：" + exception.Message); }
    }

    public string ValidatePresetName(string name)
    {
        if (Presets?.BuiltIns.ContainsKey(name) == true) return "这是内置预设名称，请为自己的配置取一个新名字。";
        return name.Length is < 1 or > 64 || name.Any(character => !(char.IsLetterOrDigit(character) || character is '-' or '_' || character > 127))
            ? "名称需为 1–64 个字，支持中文、字母、数字、横线和下划线；不含空格。" : "";
    }

    public BattleLabEditResult SavePreset(string name)
    {
        var invalid = ValidatePresetName(name);
        if (invalid.Length > 0) return new(false, invalid);
        if (Presets is null || !Presets.Save(name, Session.Freeze())) return new(false, "保存失败：实验室预设文件不可写。");
        PresetName = name;
        PresetDigest = Session.Freeze().CanonicalDigest;
        return new(true, $"已保存“{name}”。");
    }
}
