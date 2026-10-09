using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using TowerAutobattler.Domain;
using TowerAutobattler.Equipment;
using TowerAutobattler.Growth;

namespace TowerAutobattler.Run;

// Live read ports over committed Run state. Wrappers have stable identity and expose
// neither DTOs nor mutable nested collections. Save data and write paths stay unchanged.

public sealed class RunView
{
    private static readonly ConditionalWeakTable<ActiveRunDto, RunView> Views = new();
    private readonly ActiveRunDto _source;
    private RunView(ActiveRunDto source)
    {
        _source = source;
        Roster = new(() => _source.Roster.Count, index => _source.Roster[index] is { } value ? RosterHeroView.For(value) : null!);
        PopulationCapSources = new(() => _source.PopulationCapSources.Count, index => _source.PopulationCapSources[index] is { } value ? PopulationCapSourceView.For(value) : null!);
        Deployment = new(() => _source.Deployment.Count, index => _source.Deployment[index]);
        Items = new(() => _source.Items.Count, index => _source.Items[index] is { } value ? RunItemView.For(value) : null!);
        EquipmentInventory = new(() => _source.EquipmentInventory.Count, index => _source.EquipmentInventory[index] is { } value ? RunEquipmentView.For(value) : null!);
        EquippedTacticalCommandIds = new(() => _source.EquippedTacticalCommandIds.Count, index => _source.EquippedTacticalCommandIds[index]);
    }
    internal static RunView For(ActiveRunDto source) => Views.GetValue(source, static value => new(value));
    public int Version => _source.Version;
    public string RunId => _source.RunId;
    public bool IsExpedition => _source.Expedition is not null;
    public int? ExpeditionStageIndex => _source.Expedition?.StageIndex;
    public int? ExpeditionRemainingActions => _source.Expedition?.RemainingActions;
    public RunActionSource? CurrentActionSource => _source.Action?.Source;
    public GrowthNodeSnapshotView? PendingGrowthSnapshot => GrowthRunService.GetPending(_source) is { } value
        ? GrowthNodeSnapshotView.For(value) : null;
    public ulong Seed => _source.Seed;
    public int CurrentRunHealth => _source.CurrentRunHealth;
    public int MaximumRunHealth => _source.MaximumRunHealth;
    public RunBattleConsequence? LastBattleConsequence => _source.LastBattleConsequence;
    public OpeningRecruitmentDto? OpeningRecruitment => _source.OpeningRecruitment;
    public ReadOnlyListView<RosterHeroView> Roster { get; }
    public int CurrentPopulation => _source.CurrentPopulation;
    public ReadOnlyListView<PopulationCapSourceView> PopulationCapSources { get; }
    public ReadOnlyListView<string> Deployment { get; }
    public ReadOnlyListView<RunItemView> Items { get; }
    public ReadOnlyListView<RunEquipmentView> EquipmentInventory { get; }
    public ReadOnlyListView<string> EquippedTacticalCommandIds { get; }
    public int Gold => _source.Gold;
    public int FloorIndex => _source.FloorIndex;
    public int BattleNumber => _source.BattleNumber;
    public bool PendingNode => _source.PendingNode;
    public TowerNodeType SelectedNode => _source.SelectedNode;
    public string SelectedOpponentOptionId => _source.SelectedOpponentOptionId;
    public PendingRunOffer? PendingOffer => _source.PendingOffer;
    public ImmutableArray<PendingRunOffer> PendingRewardGroups
    {
        get
        {
            if (_source.Expedition is null) return _source.PendingRewardGroups;
            if (_source.Action is not { Phase: RunActionPhase.Rewards } action || action.RewardGroups.IsDefaultOrEmpty)
                return [];
            var next = action.RewardCursor + 1;
            return next > 0 && next < action.RewardGroups.Length
                ? action.RewardGroups.Slice(next, action.RewardGroups.Length - next)
                : [];
        }
    }
    public string TerminalCompletionId => _source.TerminalCompletionId;
    public bool TerminalVictory => _source.TerminalVictory;
    public GrowthRunView? Growth => _source.Growth is { } value ? GrowthRunView.For(value) : null;
}

public sealed class RosterHeroView
{
    private static readonly ConditionalWeakTable<RosterHeroInstanceDto, RosterHeroView> Views = new();
    private readonly RosterHeroInstanceDto _source;
    private RosterHeroView(RosterHeroInstanceDto source)
    {
        _source = source;
        Equipment = new(() => _source.Equipment.Count, index => _source.Equipment[index] is { } value ? RunEquipmentView.For(value) : null!);
    }
    internal static RosterHeroView For(RosterHeroInstanceDto source) => Views.GetValue(source, static value => new(value));
    public string InstanceId => _source.InstanceId;
    public string ContentId => _source.ContentId;
    public float HealthRatio => _source.HealthRatio;
    public int Rank => _source.Rank;
    public ReadOnlyListView<RunEquipmentView> Equipment { get; }
    public HeroGrowthView Growth => HeroGrowthView.For(_source.Growth);
}

public sealed class RunItemView
{
    private static readonly ConditionalWeakTable<ItemInstanceDto, RunItemView> Views = new();
    private readonly ItemInstanceDto _source;
    private RunItemView(ItemInstanceDto source)
    {
        _source = source;
        Counters = new(() => _source.Counters.Count, index => _source.Counters[index] is { } value ? RunRelicCounterView.For(value) : null!);
        RunRuleStates = new(() => _source.RunRuleStates.Count, index => _source.RunRuleStates[index] is { } value ? RunRelicRunRuleView.For(value) : null!);
    }
    internal static RunItemView For(ItemInstanceDto source) => Views.GetValue(source, static value => new(value));
    public string InstanceId => _source.InstanceId;
    public string ContentId => _source.ContentId;
    public int Stacks => _source.Stacks;
    public int Charges => _source.Charges;
    public int Roll => _source.Roll;
    public ReadOnlyListView<RunRelicCounterView> Counters { get; }
    public ReadOnlyListView<RunRelicRunRuleView> RunRuleStates { get; }
}

public sealed class RunRelicRunRuleView
{
    private static readonly ConditionalWeakTable<RelicRunRuleStateDto, RunRelicRunRuleView> Views = new();
    private readonly RelicRunRuleStateDto _source;
    private RunRelicRunRuleView(RelicRunRuleStateDto source) => _source = source;
    internal static RunRelicRunRuleView For(RelicRunRuleStateDto source) => Views.GetValue(source, static value => new(value));
    public string RuleId => _source.RuleId;
    public long GrantedAtSequence => _source.GrantedAtSequence;
    public int Remainder => _source.Remainder;
    public int ClearedSiteCount => _source.ClearedSites.Count;
}

public sealed class RunRelicCounterView
{
    private static readonly ConditionalWeakTable<RelicCounterStateDto, RunRelicCounterView> Views = new();
    private readonly RelicCounterStateDto _source;
    private RunRelicCounterView(RelicCounterStateDto source)
    {
        _source = source;
    }
    internal static RunRelicCounterView For(RelicCounterStateDto source) => Views.GetValue(source, static value => new(value));
    public string CounterId => _source.CounterId;
    public int Value => _source.Value;
}

public sealed class PopulationCapSourceView
{
    private static readonly ConditionalWeakTable<PopulationCapSourceDto, PopulationCapSourceView> Views = new();
    private readonly PopulationCapSourceDto _source;
    private PopulationCapSourceView(PopulationCapSourceDto source)
    {
        _source = source;
    }
    internal static PopulationCapSourceView For(PopulationCapSourceDto source) => Views.GetValue(source, static value => new(value));
    public string SourceId => _source.SourceId;
    public int Amount => _source.Amount;
}

public sealed class RunEquipmentView
{
    private static readonly ConditionalWeakTable<EquipmentInstanceState, RunEquipmentView> Views = new();
    private readonly EquipmentInstanceState _source;
    private RunEquipmentView(EquipmentInstanceState source)
    {
        _source = source;
    }
    internal static RunEquipmentView For(EquipmentInstanceState source) => Views.GetValue(source, static value => new(value));
    public string InstanceId => _source.InstanceId;
    public string ContentId => _source.ContentId;
    public string OwnerHeroInstanceId => _source.OwnerHeroInstanceId;
    public int SlotIndex => _source.SlotIndex;
}

public sealed class HeroGrowthView
{
    private static readonly ConditionalWeakTable<HeroGrowthDto, HeroGrowthView> Views = new();
    private readonly HeroGrowthDto _source;
    private HeroGrowthView(HeroGrowthDto source)
    {
        _source = source;
        History = new(() => _source.History.Count, index => _source.History[index] is { } value ? GrowthGainView.For(value) : null!);
    }
    internal static HeroGrowthView For(HeroGrowthDto source) => Views.GetValue(source, static value => new(value));
    public float AddedAttack => _source.AddedAttack;
    public int BreedingPoints => _source.BreedingPoints;
    public float AddedMaxHealth => _source.AddedMaxHealth;
    public string AscensionId => _source.AscensionId;
    public GrowthProductionMode ProductionMode => _source.ProductionMode;
    public string ProductionTargetInstanceId => _source.ProductionTargetInstanceId;
    public ReadOnlyListView<GrowthGainView> History { get; }
}

public sealed class GrowthGainView
{
    private static readonly ConditionalWeakTable<GrowthGainDto, GrowthGainView> Views = new();
    private readonly GrowthGainDto _source;
    private GrowthGainView(GrowthGainDto source)
    {
        _source = source;
    }
    internal static GrowthGainView For(GrowthGainDto source) => Views.GetValue(source, static value => new(value));
    public RunActionSource? ActionSource => _source.ActionSource;
    public int FloorIndex => _source.FloorIndex;
    public int BattleNumber => _source.BattleNumber;
    public string ProducerInstanceId => _source.ProducerInstanceId;
    public string TargetInstanceId => _source.TargetInstanceId;
    public GrowthProductionMode Mode => _source.Mode;
    public float Amount => _source.Amount;
    public string AbilityId => _source.AbilityId;
    public string Source => _source.Source;
    public string OperationKey => _source.OperationKey;
    public string RememberAbilityId => _source.RememberAbilityId;
    public string RememberOperationKey => _source.RememberOperationKey;
    public int GrantOrdinal => _source.GrantOrdinal;
    public int GrantedTick => _source.GrantedTick;
}

public sealed class GrowthRunView
{
    private static readonly ConditionalWeakTable<GrowthRunDto, GrowthRunView> Views = new();
    private readonly GrowthRunDto _source;
    private GrowthRunView(GrowthRunDto source)
    {
        _source = source;
        History = new(() => _source.History.Count, index => _source.History[index] is { } value ? GrowthSettlementView.For(value) : null!);
    }
    internal static GrowthRunView For(GrowthRunDto source) => Views.GetValue(source, static value => new(value));
    public string RulesId => _source.RulesId;
    public int Materials => _source.Materials;
    public IReadOnlyDictionary<string, int> CategoryMaterials => RunReadDictionaries.For(_source.CategoryMaterials);
    public int Research => _source.Research;
    public IReadOnlyDictionary<string, int> SpellInventory => RunReadDictionaries.For(_source.SpellInventory);
    public string EquippedSpellId => _source.EquippedSpellId;
    public string SpellTargetInstanceId => _source.SpellTargetInstanceId;
    public GrowthDiscoveryView? PendingDiscovery => _source.PendingDiscovery is { } value ? GrowthDiscoveryView.For(value) : null;
    // Tower compatibility slot. Cross-progression presentation uses RunView.PendingGrowthSnapshot.
    public GrowthNodeSnapshotView? PendingNode => _source.PendingNode is { } value ? GrowthNodeSnapshotView.For(value) : null;
    public int LastSettledFloorIndex => _source.LastSettledFloorIndex;
    public ReadOnlyListView<GrowthSettlementView> History { get; }
}

public sealed class GrowthDiscoveryView
{
    private static readonly ConditionalWeakTable<GrowthDiscoveryDto, GrowthDiscoveryView> Views = new();
    private readonly GrowthDiscoveryDto _source;
    private GrowthDiscoveryView(GrowthDiscoveryDto source)
    {
        _source = source;
        CandidateIds = new(() => _source.CandidateIds.Count, index => _source.CandidateIds[index]);
    }
    internal static GrowthDiscoveryView For(GrowthDiscoveryDto source) => Views.GetValue(source, static value => new(value));
    public string OfferId => _source.OfferId;
    public string UpgradedHeroInstanceId => _source.UpgradedHeroInstanceId;
    public ReadOnlyListView<string> CandidateIds { get; }
}

public sealed class GrowthNodeSnapshotView
{
    private static readonly ConditionalWeakTable<GrowthNodeSnapshotDto, GrowthNodeSnapshotView> Views = new();
    private readonly GrowthNodeSnapshotDto _source;
    private GrowthNodeSnapshotView(GrowthNodeSnapshotDto source)
    {
        _source = source;
        ParticipantInstanceIds = new(() => _source.ParticipantInstanceIds.Count, index => _source.ParticipantInstanceIds[index]);
        Assignments = new(() => _source.Assignments.Count, index => _source.Assignments[index] is { } value ? GrowthAssignmentView.For(value) : null!);
    }
    internal static GrowthNodeSnapshotView For(GrowthNodeSnapshotDto source) => Views.GetValue(source, static value => new(value));
    public RunActionSource? ActionSource => _source.Source;
    public int FloorIndex => _source.FloorIndex;
    public int BattleNumber => _source.BattleNumber;
    public bool IsBattle => _source.IsBattle;
    public ReadOnlyListView<string> ParticipantInstanceIds { get; }
    public ReadOnlyListView<GrowthAssignmentView> Assignments { get; }
    public GrowthBreedingSnapshotView? Breeding => _source.Breeding is { } value
        ? new(value.ProducerInstanceId, value.Points) : null;
    public string ConsumedSpellId => _source.ConsumedSpellId;
    public string SpellTargetInstanceId => _source.SpellTargetInstanceId;
}

public sealed class GrowthAssignmentView
{
    private static readonly ConditionalWeakTable<GrowthAssignmentDto, GrowthAssignmentView> Views = new();
    private readonly GrowthAssignmentDto _source;
    private GrowthAssignmentView(GrowthAssignmentDto source)
    {
        _source = source;
    }
    internal static GrowthAssignmentView For(GrowthAssignmentDto source) => Views.GetValue(source, static value => new(value));
    public string ProducerInstanceId => _source.ProducerInstanceId;
    public string TargetInstanceId => _source.TargetInstanceId;
    public GrowthProductionMode Mode => _source.Mode;
}

public sealed class GrowthSettlementView
{
    private static readonly ConditionalWeakTable<GrowthSettlementDto, GrowthSettlementView> Views = new();
    private readonly GrowthSettlementDto _source;
    private GrowthSettlementView(GrowthSettlementDto source)
    {
        _source = source;
        Gains = new(() => _source.Gains.Count, index => _source.Gains[index] is { } value ? GrowthGainView.For(value) : null!);
    }
    internal static GrowthSettlementView For(GrowthSettlementDto source) => Views.GetValue(source, static value => new(value));
    public RunActionSource? ActionSource => _source.Source;
    public int FloorIndex => _source.FloorIndex;
    public int BattleNumber => _source.BattleNumber;
    public bool IsBattle => _source.IsBattle;
    public ReadOnlyListView<GrowthGainView> Gains { get; }
    public int MaterialsGranted => _source.MaterialsGranted;
    public GrowthBreedingGainView? Breeding => _source.Breeding is { } value
        ? new(value.ProducerInstanceId, value.PreviousPoints, value.Amount) : null;
}

public sealed record GrowthBreedingSnapshotView(string ProducerInstanceId, int Points);
public sealed record GrowthBreedingGainView(string ProducerInstanceId, int PreviousPoints, int Amount);

internal static class RunReadDictionaries
{
    private static readonly ConditionalWeakTable<Dictionary<string, int>, ReadOnlyDictionary<string, int>> Views = new();
    internal static IReadOnlyDictionary<string, int> For(Dictionary<string, int> source) =>
        Views.GetValue(source, static value => new(value));
}
