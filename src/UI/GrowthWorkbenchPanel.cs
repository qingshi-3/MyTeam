using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TowerAutobattler.Content;
using TowerAutobattler.Growth;
using TowerAutobattler.Run;

namespace TowerAutobattler.UI;

public partial class GrowthWorkbenchPanel : VBoxContainer
{
    [Signal] public delegate void ChangedEventHandler();
    // Optional animation observers receive only committed facts. No observer is required.
    [Signal] public delegate void AscensionCompletedEventHandler(string heroInstanceId, string ascensionId);

    private RunApplication? _application;
    private OptionButton _producer = null!;
    private OptionButton _mode = null!;
    private OptionButton _target = null!;
    private OptionButton _ascensionHero = null!;
    private OptionButton _spell = null!;
    private OptionButton _spellTarget = null!;
    private VBoxContainer _discovery = null!;
    private Label _message = null!;
    private PackedScene _discoveryChoice = null!;
    public bool IsGrowthEnabled => _application?.Run?.Growth is not null && _application.GrowthRules is not null;

    public override void _Ready()
    {
        _producer = GetNode<OptionButton>("%Producer");
        _mode = GetNode<OptionButton>("%ProductionMode");
        _target = GetNode<OptionButton>("%ProductionTarget");
        _ascensionHero = GetNode<OptionButton>("%AscensionHero");
        _spell = GetNode<OptionButton>("%Spell");
        _spellTarget = GetNode<OptionButton>("%SpellTarget");
        _discovery = GetNode<VBoxContainer>("%DiscoveryChoices");
        _message = GetNode<Label>("%Message");
        _discoveryChoice = GD.Load<PackedScene>("res://scenes/ui/components/GrowthDiscoveryChoice.tscn");
        _producer.ItemSelected += _ => RefreshAssignmentChoices();
        _mode.ItemSelected += _ => RefreshProductionPreview();
        _target.ItemSelected += _ => RefreshProductionPreview();
        _ascensionHero.ItemSelected += _ => RefreshAscensionPreview();
        _spell.ItemSelected += _ => RefreshSpellPreview();
        GetNode<Button>("%ApplyAssignment").Pressed += ApplyAssignment;
        GetNode<Button>("%Ascend").Pressed += Ascend;
        GetNode<Button>("%CraftSpell").Pressed += CraftSpell;
        GetNode<Button>("%EquipSpell").Pressed += EquipSpell;
        GetNode<Button>("%UnequipSpell").Pressed += UnequipSpell;
    }

    public void Bind(RunApplication application)
    {
        _application = application;
        Refresh();
    }

    public void Refresh()
    {
        var app = _application;
        var run = app?.Run;
        var rules = app?.GrowthRules;
        if (!IsGrowthEnabled || app is null || run is null || rules is null) return;

        var producerId = SelectedId(_producer);
        var targetId = SelectedId(_target);
        var ascensionId = SelectedId(_ascensionHero);
        var spellId = SelectedId(_spell);
        var spellTargetId = SelectedId(_spellTarget);
        FillHeroes(_producer, run, app.Content, hero => rules.Heroes.TryGetValue(hero.ContentId, out var rule) && !rule.ProductionModes.IsEmpty, producerId);
        FillHeroes(_target, run, app.Content, _ => true, targetId);
        FillHeroes(_ascensionHero, run, app.Content, hero => rules.Heroes.ContainsKey(hero.ContentId), ascensionId);
        FillHeroes(_spellTarget, run, app.Content, hero => run.Deployment.Contains(hero.InstanceId), spellTargetId);
        Fill(_spell, rules.Spells.Values.OrderBy(value => value.DisplayName)
            .Select(value => (value.StableId, $"{value.DisplayName} ×{run.Growth!.SpellInventory.GetValueOrDefault(value.StableId)}")), spellId);

        var growth = run.Growth!;
        GetNode<Label>("%Inventory").Text = $"通用材料 {growth.Materials} · 研究 {growth.Research}" +
            (growth.CategoryMaterials.Count == 0 ? "" : "\n类别材料：" + string.Join(" · ", growth.CategoryMaterials.OrderBy(pair => pair.Key).Select(pair => $"{pair.Key} {pair.Value}")));
        GetNode<Label>("%EquippedSpell").Text = string.IsNullOrEmpty(growth.EquippedSpellId)
            ? "当前未预设法术"
            : $"已预设：{SpellName(rules, growth.EquippedSpellId)} → {HeroName(run, app.Content, growth.SpellTargetInstanceId)}";
        if (run.PendingGrowthSnapshot is { IsBattle: true } pending)
            GetNode<Label>("%EquippedSpell").Text = string.IsNullOrEmpty(pending.ConsumedSpellId)
                ? "本场已开始，整备已锁定。"
                : $"本场已使用：{SpellName(rules, pending.ConsumedSpellId)} → {HeroName(run, app.Content, pending.SpellTargetInstanceId)}";
        var gains = run.Roster.Where(hero => hero.Growth.AddedAttack > 0 || hero.Growth.AddedMaxHealth > 0).ToArray();
        var ledger = GetNode<Label>("%GrowthLedger");
        ledger.Visible = gains.Length > 0;
        ledger.Text = gains.Length == 0 ? string.Empty : "本局累计培养\n" + string.Join("\n", gains.Select(hero =>
            $"{HeroName(run, app.Content, hero.InstanceId)}：攻击 +{hero.Growth.AddedAttack:0.##} · 生命 +{hero.Growth.AddedMaxHealth:0.##}"));
        foreach (var hero in gains)
        {
            var remembered = hero.Growth.History.Where(gain => gain.Source == "remembered").ToArray();
            if (remembered.Length == 0) continue;
            ledger.Text += $"\n其中 {HeroName(run, app.Content, hero.InstanceId)} 的已保存铭记：攻击 +" +
                $"{remembered.Where(gain => gain.Mode == GrowthProductionMode.Attack).Sum(gain => gain.Amount):0.##} · 生命 +" +
                $"{remembered.Where(gain => gain.Mode == GrowthProductionMode.Vitality).Sum(gain => gain.Amount):0.##}";
        }
        var breeders = run.Roster.Where(hero => rules.Heroes.TryGetValue(hero.ContentId, out var rule) && rule.BreedingYield > 0).ToArray();
        foreach (var breeder in breeders)
        {
            ledger.Visible = true;
            ledger.Text += (ledger.Text.Length == 0 ? string.Empty : "\n") +
                $"{HeroName(run, app.Content, breeder.InstanceId)}：育种 {breeder.Growth.BreedingPoints} · " +
                (run.Deployment.Contains(breeder.InstanceId) ? "上阵，放大下一份树种" : "后备，暂停育种与树种放大");
        }
        var editable = app.CheckGrowthAction();
        var phaseHint = GetNode<Label>("%PhaseHint");
        phaseHint.Visible = !editable.Succeeded;
        phaseHint.Text = editable.Succeeded ? string.Empty : editable.Message;
        foreach (var button in new[] { "ApplyAssignment", "Ascend", "CraftSpell", "EquipSpell", "UnequipSpell" })
            GetNode<Button>("%" + button).Disabled = !editable.Succeeded;
        GetNode<Button>("%ApplyAssignment").Disabled |= _producer.ItemCount == 0;
        _producer.GetParent<Control>().Visible = _producer.ItemCount > 0;
        GetNode<Control>("Scroll/Content/Production/Layout/SelectorLabels").Visible = _producer.ItemCount > 0;
        GetNode<Button>("%ApplyAssignment").Visible = _producer.ItemCount > 0;
        RefreshAssignmentChoices();
        RefreshAscensionPreview();
        RefreshSpellPreview();
        RefreshDiscovery(run, app.Content);
    }

    private void RefreshAssignmentChoices()
    {
        if (_application?.Run is not { } run || _application.GrowthRules is not { } rules) return;
        var producer = run.Roster.FirstOrDefault(hero => hero.InstanceId == SelectedId(_producer));
        var previousMode = SelectedId(_mode);
        _mode.Clear();
        if (producer is not null && rules.Heroes.TryGetValue(producer.ContentId, out var definition))
            foreach (var mode in definition.ProductionModes)
                Add(_mode, mode.ToString(), ModeName(mode));
        Select(_mode, previousMode);
        if (producer is not null)
        {
            Select(_mode, producer.Growth.ProductionMode.ToString());
            Select(_target, producer.Growth.ProductionTargetInstanceId);
        }
        RefreshProductionPreview();
    }

    private void RefreshProductionPreview()
    {
        if (_application?.Run is not { } run || _application.GrowthRules is not { } rules) return;
        var producer = run.Roster.FirstOrDefault(hero => hero.InstanceId == SelectedId(_producer));
        var target = run.Roster.FirstOrDefault(hero => hero.InstanceId == SelectedId(_target));
        if (producer is null || !rules.Heroes.TryGetValue(producer.ContentId, out var growthHero) || !Enum.TryParse(SelectedId(_mode), out GrowthProductionMode mode))
        {
            GetNode<Label>("%ProductionPreview").Text = "当前没有可安排生产的英雄。";
            return;
        }
        var bonusAttack = 0f;
        var bonusHealth = 0f;
        foreach (var breeder in run.Roster.Where(hero => run.Deployment.Contains(hero.InstanceId)))
            if (rules.Heroes.TryGetValue(breeder.ContentId, out var rule) && rule.BreedingYield > 0)
            {
                bonusAttack += breeder.Growth.BreedingPoints * rule.AttackGainPerBreedingPoint;
                bonusHealth += breeder.Growth.BreedingPoints * rule.MaxHealthGainPerBreedingPoint;
            }
        var output = mode == GrowthProductionMode.Research ? $"研究 +{growthHero.ResearchYield}" : target is null ? "请选择受益英雄" :
            mode == GrowthProductionMode.Cultivation ? $"{HeroName(run, _application.Content, target.InstanceId)} 下一份树种：攻击永久 +{growthHero.FixedAttackGain + bonusAttack:0.##} · 生命永久 +{growthHero.FixedMaxHealthGain + bonusHealth:0.##}" :
            mode == GrowthProductionMode.Attack ? $"{HeroName(run, _application.Content, target.InstanceId)} 攻击永久 +{Base(target.ContentId).AttackDamage * growthHero.GrowthRate:0.##}" :
            $"{HeroName(run, _application.Content, target.InstanceId)} 生命永久 +{Base(target.ContentId).MaxHealth * growthHero.GrowthRate:0.##}";
        var eligible = run.Deployment.Contains(producer.InstanceId) && (mode == GrowthProductionMode.Research || target is not null && run.Deployment.Contains(target.InstanceId));
        var assigned = string.IsNullOrEmpty(producer.Growth.ProductionTargetInstanceId)
            ? "当前未分配培养对象"
            : $"已分配：{ModeName(producer.Growth.ProductionMode)} → {HeroName(run, _application.Content, producer.Growth.ProductionTargetInstanceId)}";
        GetNode<Label>("%ProductionPreview").Text = $"{assigned}\n选项预览：{output}\n" +
            (eligible ? "满足上阵条件；保存分配后于有效非终局行动结算" : "未满足上阵条件，按此分配无产出");
        _target.Disabled = mode == GrowthProductionMode.Research;
        UnitDefinition Base(string id) => (UnitDefinition)Required(_application.Content, id).Definition;
    }

    private void RefreshAscensionPreview()
    {
        if (_application?.Run is not { } run || _application.GrowthRules is not { } rules) return;
        var hero = run.Roster.FirstOrDefault(value => value.InstanceId == SelectedId(_ascensionHero));
        if (hero is null || !rules.Heroes.TryGetValue(hero.ContentId, out var definition)) return;
        var categories = string.IsNullOrWhiteSpace(definition.MaterialCategory) ? "通用材料" : "对应类别材料优先抵扣，不足部分以通用材料补足";
        var ascended = hero.Growth.AscensionId.Length > 0;
        var status = GrowthPresentation.AscensionStatus(definition, ascended);
        var benefit = GrowthPresentation.AscensionBenefitText(definition);
        GetNode<Label>("%AscensionPreview").Text = ascended
            ? $"{status}\n{benefit}"
            : $"{status}\n{benefit}\n{categories} · 升阶后从三名高阶英雄中选择一名";
        var button = GetNode<Button>("%Ascend");
        button.Text = hero.Growth.AscensionId.Length > 0 ? "已升阶" : $"升阶 · {rules.AscensionCost}材料";
        button.Disabled = !_application.CheckGrowthAction().Succeeded || hero.Growth.AscensionId.Length > 0 ||
            run.Growth!.Materials + run.Growth.CategoryMaterials.GetValueOrDefault(definition.MaterialCategory) < rules.AscensionCost;
    }

    private void RefreshSpellPreview()
    {
        if (_application?.GrowthRules is not { } rules || !rules.Spells.TryGetValue(SelectedId(_spell), out var spell)) return;
        GetNode<Label>("%SpellPreview").Text = spell.Description;
        var run = _application.Run!;
        var editable = _application.CheckGrowthAction().Succeeded;
        var craft = GetNode<Button>("%CraftSpell");
        craft.Text = $"兑换 · {spell.ResearchCost}研究";
        craft.Disabled = !editable || run.Growth!.Research < spell.ResearchCost;
        GetNode<Button>("%EquipSpell").Disabled = !editable || run.Growth!.SpellInventory.GetValueOrDefault(spell.StableId) <= 0 || _spellTarget.ItemCount == 0;
        GetNode<Button>("%UnequipSpell").Disabled = !editable || string.IsNullOrEmpty(run.Growth!.EquippedSpellId);
    }

    private void RefreshDiscovery(RunView run, ContentRegistry content)
    {
        foreach (var child in _discovery.GetChildren()) { _discovery.RemoveChild(child); child.QueueFree(); }
        var pending = run.Growth?.PendingDiscovery;
        GetNode<Control>("%DiscoverySection").Visible = pending is not null;
        if (pending is null) return;
        GetNode<Label>("%DiscoveryPrompt").Text = "选择一名高阶英雄";
        foreach (var id in pending.CandidateIds)
        {
            var definition = (UnitDefinition)Required(content, id).Definition;
            var button = _discoveryChoice.Instantiate<Button>();
            button.Text = definition.DisplayName;
            button.TooltipText = definition.Description;
            button.Pressed += () => RunCommand(() => _application!.ChooseGrowthHero(id));
            _discovery.AddChild(button);
        }
    }

    private void ApplyAssignment()
    {
        if (!Enum.TryParse(SelectedId(_mode), out GrowthProductionMode mode)) return;
        RunCommand(() => _application!.SetGrowthAssignment(SelectedId(_producer),
            mode == GrowthProductionMode.Research ? SelectedId(_producer) : SelectedId(_target), mode));
    }
    private void Ascend()
    {
        var heroId = SelectedId(_ascensionHero);
        if (!RunCommand(() => _application!.AscendHero(heroId))) return;
        var hero = _application!.Run!.Roster.Single(value => value.InstanceId == heroId);
        EmitSignal(SignalName.AscensionCompleted, heroId, hero.Growth.AscensionId);
    }
    private void CraftSpell() => RunCommand(() => _application!.CraftGrowthSpell(SelectedId(_spell)));
    private void EquipSpell() => RunCommand(() => _application!.EquipGrowthSpell(SelectedId(_spell), SelectedId(_spellTarget)));
    private void UnequipSpell() => RunCommand(() => _application!.EquipGrowthSpell(string.Empty, string.Empty));

    private bool RunCommand(Func<GrowthCommandResult> command)
    {
        var result = command();
        _message.Text = result.Succeeded ? string.Empty : result.Message;
        _message.ThemeTypeVariation = "FeedbackFailure";
        _message.Visible = !result.Succeeded && !string.IsNullOrWhiteSpace(result.Message);
        if (!result.Succeeded) return false;
        Refresh();
        EmitSignal(SignalName.Changed);
        return true;
    }

    private static void FillHeroes(OptionButton menu, RunView run, ContentRegistry content, Func<RosterHeroView, bool> predicate, string selected) =>
        Fill(menu, run.Roster.Where(predicate).Select(hero => (hero.InstanceId, ((UnitDefinition)Required(content, hero.ContentId).Definition).DisplayName)), selected);
    private static void Fill(OptionButton menu, System.Collections.Generic.IEnumerable<(string Id, string Name)> values, string selected)
    { menu.Clear(); foreach (var (id, name) in values) Add(menu, id, name); Select(menu, selected); }
    private static void Add(OptionButton menu, string id, string name) { menu.AddItem(name); menu.SetItemMetadata(menu.ItemCount - 1, id); }
    private static void Select(OptionButton menu, string id) { for (var i = 0; i < menu.ItemCount; i++) if (menu.GetItemMetadata(i).AsString() == id) { menu.Select(i); return; } if (menu.ItemCount > 0) menu.Select(0); }
    private static string SelectedId(OptionButton menu) => menu.ItemCount == 0 || menu.Selected < 0 ? string.Empty : menu.GetItemMetadata(menu.Selected).AsString();
    private static string ModeName(GrowthProductionMode mode) => mode switch { GrowthProductionMode.Attack => "攻击培养", GrowthProductionMode.Vitality => "生命培养", GrowthProductionMode.Research => "研究", GrowthProductionMode.Cultivation => "树种培养", _ => "无效生产方式" };
    private static string HeroName(RunView run, ContentRegistry content, string instanceId) { var hero = run.Roster.FirstOrDefault(value => value.InstanceId == instanceId); return hero is null ? "未选择" : ((UnitDefinition)Required(content, hero.ContentId).Definition).DisplayName; }
    private static string SpellName(CompiledGrowthRules rules, string id) => rules.Spells.TryGetValue(id, out var spell) ? spell.DisplayName : id;
    private static CatalogEntry Required(ContentRegistry content, string id) => content.TryGet(id, out var entry) ? entry : throw new InvalidOperationException($"缺少内容 {id}");
}
