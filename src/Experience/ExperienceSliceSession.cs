using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using TowerAutobattler.Battle;
using TowerAutobattler.Composition;
using TowerAutobattler.Content;
using TowerAutobattler.Equipment;
using TowerAutobattler.Run;

namespace TowerAutobattler.Experience;

public enum ExperienceStage { Relic, Recruit, PrepareFirst, BattleFirst, ReportFirst, Equipment, PrepareSecond, BattleSecond, ReportSecond, Complete }
public sealed record ExperienceItem(string InstanceId, string ContentId, string Origin);
public sealed record ExperienceCheckpoint(string RunJson, ImmutableArray<ExperienceItem> Inventory,
    ImmutableArray<string> Choices, ImmutableArray<BattleResult> Results, ExperienceStage Stage);
public sealed record ExperienceAttempt(int Number, string Origin, ImmutableArray<string> Choices,
    ImmutableArray<BattleResult> Results, ExperienceCheckpoint? AfterFirst);
public sealed record ExperienceStats(string InstanceId, string Name, float Health, float Damage, float Armor, float Heal, float LifeSteal);

// Owns the experiment, not a second combat engine. All battles, formation rules and
// cross-floor recovery use production Run services with an in-memory persistence adapter.
public sealed class ExperienceSliceSession
{
    private readonly ContentRegistry _content;
    private ExperienceMemoryStore _store = null!;
    private readonly List<ExperienceItem> _inventory = [];
    private readonly List<string> _choices = [];
    private readonly List<BattleResult> _results = [];
    private readonly List<ExperienceAttempt> _history = [];
    private ExperienceCheckpoint _start = null!;
    private ExperienceCheckpoint? _afterFirst;
    private string _origin = "初始休整点";

    public ExperienceSliceSession(CompiledGamePackage package, ExperienceSliceDefinition definition)
    {
        _content = package.Content;
        Definition = definition.Compile(package);
        NewApplication(null);
        Application.Meta.UnlockedHeroIds.Add(Definition.StartingHeroId);
        if (!Application.StartNewRun(Definition.StartingHeroId, Definition.Seed))
            throw new InvalidOperationException("无法创建隔离试验队伍。");
        _inventory.Add(new("slice-start-1", Definition.StartingEquipmentId, "初始装备"));
        var run = ExperienceMemoryStore.Clone(Run);
        run.Roster[0].Equipment.Add(new EquipmentInstanceState
        {
            InstanceId = _inventory[0].InstanceId, ContentId = Definition.StartingEquipmentId,
            OwnerHeroInstanceId = run.Roster[0].InstanceId, SlotIndex = 0
        });
        if (!Publish(run)) throw new InvalidOperationException("试验初始装备不合法。");
        Stage = ExperienceStage.Relic;
        _start = Checkpoint();
    }

    public CompiledExperienceSlice Definition { get; }
    public RunApplication Application { get; private set; } = null!;
    public ExperienceStage Stage { get; private set; }
    public int AttemptNumber { get; private set; } = 1;
    public IReadOnlyList<ExperienceItem> Inventory => _inventory.AsReadOnly();
    public IReadOnlyList<string> Choices => _choices.AsReadOnly();
    public IReadOnlyList<BattleResult> Results => _results.AsReadOnly();
    public IReadOnlyList<ExperienceAttempt> History => _history.AsReadOnly();
    public bool CanRetryAfterFirst => _afterFirst is not null;
    public bool IsPreparing => Stage is ExperienceStage.PrepareFirst or ExperienceStage.PrepareSecond;
    public bool IsBattle => Stage is ExperienceStage.BattleFirst or ExperienceStage.BattleSecond;
    public ActiveRunDto Run => Application.ActiveRun ?? throw new InvalidOperationException("试验队伍已结束。");
    public BattleResult? LastResult => _results.LastOrDefault();
    public string Name(string id) => _content.TryGet(id, out var entry) ? entry.Definition switch
    { UnitDefinition unit => unit.DisplayName, ItemDefinition item => item.DisplayName, _ => id } : id;
    public EncounterPlan Encounter => Application.Tower.Encounter(Run,
        Stage is ExperienceStage.Equipment or ExperienceStage.PrepareSecond or ExperienceStage.BattleSecond or ExperienceStage.ReportSecond or ExperienceStage.Complete
            ? TowerNodeType.Elite : TowerNodeType.Combat);

    public bool ChooseRelic(string id)
    {
        if (Stage != ExperienceStage.Relic || !Definition.RelicIds.Contains(id) || !Application.GrantItem(id)) return false;
        _choices.Add("遗物：" + Name(id));
        Stage = ExperienceStage.Recruit;
        return true;
    }

    public bool ChooseRecruit(string? id)
    {
        if (Stage != ExperienceStage.Recruit || (id is not null &&
            (!Definition.RecruitIds.Contains(id) || !Application.Recruit(id)))) return false;
        _choices.Add(id is null ? "跳过招募" : "招募：" + Name(id) + "（进入候命，需手动部署）");
        if (!Application.SelectNode(TowerNodeType.Combat)) throw new InvalidOperationException("试验第一战不可选。");
        Stage = ExperienceStage.PrepareFirst;
        return true;
    }

    public bool ChooseEquipment(string id)
    {
        if (Stage != ExperienceStage.Equipment || !Definition.EquipmentIds.Contains(id)) return false;
        // The pending offer can be claimed exactly once. It creates owned inventory,
        // not a free content-id equip command. Initial gear remains owned when displaced.
        _inventory.Add(new("slice-reward-1", id, "第一战奖励"));
        _choices.Add("装备奖励：" + Name(id));
        if (!Application.SelectNode(TowerNodeType.Elite)) throw new InvalidOperationException("试验第二战不可选。");
        Stage = ExperienceStage.PrepareSecond;
        return true;
    }

    public bool MoveEquipment(string itemInstanceId, string? ownerId, int slot)
    {
        if (Stage != ExperienceStage.PrepareSecond || slot < 0 || slot >= Application.Rules.EquipmentSlotCapacity) return false;
        var item = _inventory.SingleOrDefault(i => i.InstanceId == itemInstanceId);
        if (item is null || ownerId is not null && !Run.Roster.Any(h => h.InstanceId == ownerId)) return false;
        var run = ExperienceMemoryStore.Clone(Run);
        foreach (var hero in run.Roster) hero.Equipment.RemoveAll(e => e.InstanceId == itemInstanceId);
        if (ownerId is not null)
        {
            var owner = run.Roster.Single(h => h.InstanceId == ownerId);
            owner.Equipment.RemoveAll(e => e.SlotIndex == slot); // Displaced copy stays in _inventory.
            owner.Equipment.Add(new EquipmentInstanceState
            { InstanceId = item.InstanceId, ContentId = item.ContentId, OwnerHeroInstanceId = ownerId, SlotIndex = slot });
        }
        if (!Publish(run)) return false;
        _choices.Add(ownerId is null ? $"收回背包：{Name(item.ContentId)}" :
            $"装备：{Name(item.ContentId)} → {Name(run.Roster.Single(h => h.InstanceId == ownerId).ContentId)} / 槽 {slot + 1}");
        return true;
    }

    public BattleConfig StartBattle()
    {
        if (!IsPreparing) throw new InvalidOperationException("当前不在战前准备。");
        if (!Run.Deployment.Any(id => !string.IsNullOrEmpty(id))) throw new InvalidOperationException("至少部署一位英雄后才能开始。");
        var config = Application.BuildBattleConfig(Encounter);
        _choices.Add($"第 {_results.Count + 1} 战阵型：" + string.Join("；", Run.Deployment.Select((id, index) => (id, index))
            .Where(p => !string.IsNullOrEmpty(p.id)).Select(p => $"{Name(Run.Roster.Single(h => h.InstanceId == p.id).ContentId)}@{p.index}")));
        Stage = Stage == ExperienceStage.PrepareFirst ? ExperienceStage.BattleFirst : ExperienceStage.BattleSecond;
        return config;
    }

    public void CancelFailedStart()
    {
        if (IsBattle) Stage = Stage == ExperienceStage.BattleFirst ? ExperienceStage.PrepareFirst : ExperienceStage.PrepareSecond;
    }

    public bool AcceptResult(BattleResult result)
    {
        if (!IsBattle || result.Outcome == BattleOutcome.Running) return false;
        var first = Stage == ExperienceStage.BattleFirst;
        var resolution = Application.ResolveBattle(result, Encounter);
        if (!resolution.Accepted) return false;
        _results.Add(result);
        Stage = first ? ExperienceStage.ReportFirst : ExperienceStage.ReportSecond;
        return true;
    }

    public bool ContinueReport()
    {
        if (Stage is not (ExperienceStage.ReportFirst or ExperienceStage.ReportSecond)) return false;
        // The isolated slice owns its own reward sequence; consume the formal opportunity
        // through the same exactly-once command before advancing, without creating a second reward.
        if (Application.PendingOffer is { } offer && !Application.ResolveOffer(offer.OfferId, null).Succeeded)
            return false;
        if (LastResult?.Outcome != BattleOutcome.PlayerVictory || Stage == ExperienceStage.ReportSecond)
            Stage = ExperienceStage.Complete;
        else
        {
            Stage = ExperienceStage.Equipment;
            _afterFirst = Checkpoint();
        }
        return true;
    }

    public bool Retry(bool afterFirst)
    {
        if (IsBattle || Stage is ExperienceStage.ReportFirst or ExperienceStage.ReportSecond) return false;
        var checkpoint = afterFirst ? _afterFirst : _start;
        if (checkpoint is null) return false;
        _history.Add(new(AttemptNumber, _origin, _choices.ToImmutableArray(), _results.ToImmutableArray(), _afterFirst));
        AttemptNumber++;
        _origin = afterFirst ? "第一战后检查点" : "初始休整点";
        NewApplication(JsonSerializer.Deserialize<ActiveRunDto>(checkpoint.RunJson)!);
        _inventory.Clear(); _inventory.AddRange(checkpoint.Inventory);
        _choices.Clear(); _choices.AddRange(checkpoint.Choices);
        _results.Clear(); _results.AddRange(checkpoint.Results);
        Stage = checkpoint.Stage;
        if (!afterFirst) _afterFirst = null;
        return true;
    }

    public IReadOnlyList<ExperienceStats> PreparedStats() => Stats(Application, Encounter);

    public void RecordCommand(int tick, int slot, string target, bool succeeded, string reason)
    {
        if (IsBattle) _choices.Add($"战术 {tick * .1:0.0}s / 槽 {slot + 1} / 目标 {target} / " + (succeeded ? "成功" : "未发动：" + reason));
    }

    public string PreviewRelic(string id)
    {
        if (Stage != ExperienceStage.Relic || !Definition.RelicIds.Contains(id)) return "";
        var baseline = PreparedStats();
        var memory = new ExperienceMemoryStore(); memory.SaveActiveRun(Run);
        var preview = new RunApplication(_content, memory, Definition.Project);
        if (!preview.GrantItem(id)) return "当前队伍无法获得该遗物。";
        var changed = Stats(preview, Encounter);
        return DescribeChanges(baseline, changed);
    }

    public static string DescribeChanges(IReadOnlyList<ExperienceStats> before, IReadOnlyList<ExperienceStats> after)
    {
        var lines = new List<string>();
        foreach (var current in after)
        {
            var previous = before.FirstOrDefault(p => p.InstanceId == current.InstanceId);
            if (previous is null) continue;
            var changes = new List<string>();
            Change("生命上限", previous.Health, current.Health);
            Change("攻击", previous.Damage, current.Damage);
            Change("护甲", previous.Armor, current.Armor);
            Change("治疗", previous.Heal, current.Heal);
            Change("吸血", previous.LifeSteal * 100, current.LifeSteal * 100, "%");
            if (changes.Count > 0) lines.Add(current.Name + "：" + string.Join("，", changes));
            void Change(string label, float a, float b, string suffix = "")
            { if (Math.Abs(a - b) > .001f) changes.Add($"{label} {a:0.#}{suffix} → {b:0.#}{suffix}"); }
        }
        return lines.Count == 0 ? "开战属性无静态变化；命中、条件和状态效果在真实战斗中结算。" : string.Join("\n", lines);
    }

    private static IReadOnlyList<ExperienceStats> Stats(RunApplication app, EncounterPlan encounter)
    {
        using var preview = new BattleSimulation(app.BuildBattleConfig(encounter, false));
        return preview.Units.Where(u => u.Team == 0 && !u.IsTemporary).Select(u =>
            new ExperienceStats(u.SourceInstanceId, u.Definition.DisplayName, u.MaxHealth, u.Damage, u.Armor, u.HealingPower, u.LifeSteal)).ToArray();
    }

    private ExperienceCheckpoint Checkpoint() => new(JsonSerializer.Serialize(Run), _inventory.ToImmutableArray(),
        _choices.ToImmutableArray(), _results.ToImmutableArray(), Stage);

    private void NewApplication(ActiveRunDto? run)
    {
        _store = new ExperienceMemoryStore();
        if (run is not null) _store.SaveActiveRun(run);
        Application = new RunApplication(_content, _store, Definition.Project);
        if (run is not null && Application.ActiveRun is null) throw new InvalidOperationException("试验检查点不合法，已拒绝载入。");
    }

    private bool Publish(ActiveRunDto run)
    {
        if (!RunFormationPolicy.Validate(run, Definition.Project.RunRules) ||
            !ActiveRunConfigurationValidator.Validate(run, _content, Definition.Project)) return false;
        NewApplication(run);
        return true;
    }
}
