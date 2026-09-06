using System;
using System.Linq;
using Godot;
using TowerAutobattler.Content;
using TowerAutobattler.UI;

namespace TowerAutobattler.Experience;

public partial class ExperienceSlicePanel : Control
{
    public event Action<string>? ChoiceCommitted;
    public event Action? SkipRequested;
    public event Action? PrepareRequested;
    public event Action<bool>? RetryRequested;
    public event Action<string, string?, int>? EquipmentMoveRequested;
    public event Action? ExitRequested;
    private ExperienceSliceSession? _session;
    private ExperienceStage? _boundStage;
    private string _selected = "";
    private string[] _itemIds = [], _ownerIds = [];
    private Label _step = null!, _title = null!, _hint = null!, _team = null!, _preview = null!, _inventory = null!, _history = null!, _feedback = null!;
    private Container _items = null!, _recruits = null!, _equipment = null!;
    private OptionButton _itemPicker = null!, _ownerPicker = null!, _slotPicker = null!;
    private Button _primary = null!, _skip = null!, _retry = null!, _retryAfter = null!, _exit = null!, _apply = null!, _bag = null!;
    private PackedScene _choiceTemplate = null!, _unitTemplate = null!;
    private SemanticIconCatalog _icons = null!;

    public override void _Ready()
    {
        _step = GetNode<Label>("%Step"); _title = GetNode<Label>("%Title");
        _hint = GetNode<Label>("%Hint"); _team = GetNode<Label>("%Team");
        _preview = GetNode<Label>("%Preview"); _inventory = GetNode<Label>("%Inventory");
        _history = GetNode<Label>("%History"); _feedback = GetNode<Label>("%Feedback");
        _items = GetNode<Container>("%ItemChoices"); _recruits = GetNode<Container>("%RecruitChoices");
        _equipment = GetNode<Container>("%EquipmentControls");
        _itemPicker = GetNode<OptionButton>("%ItemPicker"); _ownerPicker = GetNode<OptionButton>("%OwnerPicker");
        _slotPicker = GetNode<OptionButton>("%SlotPicker");
        _primary = GetNode<Button>("%Primary"); _skip = GetNode<Button>("%Skip");
        _retry = GetNode<Button>("%Retry"); _retryAfter = GetNode<Button>("%RetryAfter");
        _exit = GetNode<Button>("%Exit"); _apply = GetNode<Button>("%Equip"); _bag = GetNode<Button>("%Unequip");
        _primary.Pressed += Primary; _skip.Pressed += Skip; _retry.Pressed += Retry;
        _retryAfter.Pressed += RetryAfter; _exit.Pressed += Exit;
        _apply.Pressed += Equip; _bag.Pressed += Unequip;
    }

    public override void _ExitTree()
    {
        _primary.Pressed -= Primary; _skip.Pressed -= Skip; _retry.Pressed -= Retry;
        _retryAfter.Pressed -= RetryAfter; _exit.Pressed -= Exit;
        _apply.Pressed -= Equip; _bag.Pressed -= Unequip;
    }

    public void Bind(ExperienceSliceSession session)
    {
        _session = session;
        var app = session.Application;
        _choiceTemplate = app.Project.Presentation.ChoiceCard;
        _unitTemplate = app.Project.Presentation.UnitChoiceCard;
        _icons = app.Project.Presentation.SemanticIcons;
        if (_boundStage != session.Stage) _selected = "";
        _boundStage = session.Stage;
        _feedback.Text = "";
        _step.Text = $"玩法试验 01  /  第 {session.AttemptNumber} 次尝试  /  配置 v{session.Definition.Revision}";
        (_title.Text, _hint.Text) = session.Stage switch
        {
            ExperienceStage.Relic => ("先给队伍一个方向", "从预置休整点开始，没有省略或伪造上一场战报。三件遗物只取一件；点击先看实际受益者。"),
            ExperienceStage.Recruit => ("补上你想要的职责", "单体火力、范围处理，还是更多治疗？新英雄进入候命，下一步由你安排上场。"),
            ExperienceStage.PrepareFirst => ("让刚才的选择上场", "第一战：一名守卫保护远程弩手。先布阵，再观察你的队伍如何接敌。"),
            ExperienceStage.Equipment => ("看过第一战，再选一件装备", "第二战只增加远程敌人的数量，场地和敌人种类不变。生命与伤势沿用实际战后恢复结果。"),
            ExperienceStage.PrepareSecond => ("这件装备，给谁更合适？", "已有装备可以无损转移。替换槽位时，旧装备回背包；候命英雄的装备不会在本场生效。"),
            _ => (session.LastResult?.Outcome == TowerAutobattler.Battle.BattleOutcome.PlayerVictory ? "这一段体验完成了" : "这次没有通过，换个选择再试", "先看真实结果，再决定换奖励、换人还是换站位。固定样本不代表随机局胜率。")
        };
        _team.Text = app.ActiveRun is { } run ? string.Join("  /  ", run.Roster.Select(h =>
            $"{session.Name(h.ContentId)} {h.HealthRatio:P0}{(run.Deployment.Contains(h.InstanceId) ? "" : "·候命")}")) : "本次队伍已在战斗中失败；可从保留的检查点重试。";
        var choiceStage = session.Stage is ExperienceStage.Relic or ExperienceStage.Equipment;
        _items.Visible = choiceStage; _recruits.Visible = session.Stage == ExperienceStage.Recruit;
        _equipment.Visible = session.Stage == ExperienceStage.PrepareSecond;
        _inventory.Visible = _equipment.Visible;
        _skip.Visible = session.Stage == ExperienceStage.Recruit;
        _primary.Visible = session.Stage != ExperienceStage.Complete;
        _primary.Disabled = !session.IsPreparing && string.IsNullOrEmpty(_selected);
        _primary.Text = session.IsPreparing ? "进入布阵 →" : "确认选择 →";
        _retryAfter.Disabled = !session.CanRetryAfterFirst;
        _retry.Disabled = false;
        _preview.Text = "";
        if (choiceStage)
        {
            var ids = session.Stage == ExperienceStage.Relic ? session.Definition.RelicIds : session.Definition.EquipmentIds;
            ChoiceCardListBinder.SyncChoices(_items, ids.Select(id =>
            {
                var item = (ItemDefinition)Required(id).Definition;
                return new ChoiceCardViewModel(id, item.DisplayName, item.Description, "点击比较", Icon: item.Icon);
            }).ToArray(), _choiceTemplate, SelectChoice);
        }
        if (_recruits.Visible)
            ChoiceCardListBinder.SyncUnits(_recruits, session.Definition.RecruitIds.Select(id =>
            {
                var unit = (UnitDefinition)Required(id).Definition;
                return new UnitChoiceCardViewModel(id, unit, unit.Description, "点击选择职责");
            }).ToArray(), _unitTemplate, _icons, SelectChoice);
        if (_equipment.Visible) BindEquipment();
        if (!string.IsNullOrEmpty(_selected) && !session.IsPreparing) SelectChoice(_selected);
        _history.Text = string.Join("\n\n", session.History.Select(a =>
            $"尝试 {a.Number} · {a.Origin}\n" + string.Join("\n", a.Choices) + "\n" + Results(a.Results))) +
            $"\n\n当前尝试 {session.AttemptNumber}\n" + string.Join("\n", session.Choices) + "\n" + Results(session.Results) +
            "\n\n这里只比较实际记录。操作或敌人不同不能算单一变量收益；同种子也可能因行动顺序改变而改变随机调用。";

        CatalogEntry Required(string id) => app.Content.TryGet(id, out var entry) ? entry : throw new InvalidOperationException(id);
    }

    private static string Results(System.Collections.Generic.IEnumerable<TowerAutobattler.Battle.BattleResult> results) =>
        string.Join("\n", results.Select((r, i) => $"第 {i + 1} 战：{PlayerFacingText.DescribeBattleOutcome(r.Outcome)} / {r.Ticks * .1:0.0} 秒 / 战术成功 {r.SuccessfulTacticalCommandUses} 次 / 摘要 {r.Digest[..12]}"));

    private void BindEquipment()
    {
        var session = _session!;
        var previousItem = _itemPicker.Selected >= 0 && _itemPicker.Selected < _itemIds.Length ? _itemIds[_itemPicker.Selected] : "";
        var previousOwner = _ownerPicker.Selected >= 0 && _ownerPicker.Selected < _ownerIds.Length ? _ownerIds[_ownerPicker.Selected] : "";
        _itemIds = session.Inventory.Select(i => i.InstanceId).ToArray();
        _ownerIds = session.Run.Roster.Select(h => h.InstanceId).ToArray();
        _itemPicker.Clear(); _ownerPicker.Clear(); _slotPicker.Clear();
        foreach (var item in session.Inventory) _itemPicker.AddItem(session.Name(item.ContentId) + " · " + item.Origin);
        foreach (var hero in session.Run.Roster) _ownerPicker.AddItem(session.Name(hero.ContentId) + (session.Run.Deployment.Contains(hero.InstanceId) ? "" : "（候命）"));
        for (var i = 0; i < session.Application.Rules.EquipmentSlotCapacity; i++) _slotPicker.AddItem($"槽 {i + 1}");
        _itemPicker.Select(Array.IndexOf(_itemIds, previousItem) is var itemIndex && itemIndex >= 0 ? itemIndex : _itemIds.Length - 1);
        _ownerPicker.Select(Math.Max(0, Array.IndexOf(_ownerIds, previousOwner)));
        _inventory.Text = string.Join("\n", session.Inventory.Select(item =>
        {
            var owner = session.Run.Roster.FirstOrDefault(h => h.Equipment.Any(e => e.InstanceId == item.InstanceId));
            var slot = owner?.Equipment.Single(e => e.InstanceId == item.InstanceId).SlotIndex;
            return session.Name(item.ContentId) + " → " + (owner is null ? "背包" : session.Name(owner.ContentId) + $" / 槽 {slot + 1}");
        }));
    }

    private void SelectChoice(string id)
    {
        _selected = id;
        _primary.Disabled = false;
        _primary.Text = "选择「" + _session!.Name(id) + "」 →";
        _preview.Text = _session.Stage == ExperienceStage.Relic ? "当前已部署队伍的开战变化\n" + _session.PreviewRelic(id) :
            _session.Stage == ExperienceStage.Recruit ? "选后进入候命；布阵时可以部署、替换或继续保留后备。" : "选择后获得一件实例，再指定持有者；不会自动装给起始英雄。";
    }
    public void ShowFeedback(string message) => _feedback.Text = message;
    private void Primary() { if (_session?.IsPreparing == true) PrepareRequested?.Invoke(); else if (!string.IsNullOrEmpty(_selected)) ChoiceCommitted?.Invoke(_selected); }
    private void Skip() => SkipRequested?.Invoke();
    private void Retry() { _selected = ""; RetryRequested?.Invoke(false); }
    private void RetryAfter() { _selected = ""; RetryRequested?.Invoke(true); }
    private void Exit() => ExitRequested?.Invoke();
    private void Equip() { if (_itemPicker.Selected >= 0 && _ownerPicker.Selected >= 0) EquipmentMoveRequested?.Invoke(_itemIds[_itemPicker.Selected], _ownerIds[_ownerPicker.Selected], _slotPicker.Selected); }
    private void Unequip() { if (_itemPicker.Selected >= 0) EquipmentMoveRequested?.Invoke(_itemIds[_itemPicker.Selected], null, 0); }
}
