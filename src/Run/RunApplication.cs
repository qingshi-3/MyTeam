using System;
using System.Collections.Generic;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.Equipment;
using TowerAutobattler.Project;
using TowerAutobattler.Traits;
using TowerAutobattler.Growth;
using TowerAutobattler.Composition;

namespace TowerAutobattler.Run;

// Stable use-case facade. Cohesive services own formation, node resolution,
// Battle preparation, reward/economy, and progression/persistence behavior.
public sealed partial class RunApplication
{
    private ContentRegistry _content;
    private CompiledGameProject _project;
    private TowerGenerator? _tower;
    private RunActionCoordinator? _actions;
    private CompiledGamePackage? _package;
    private IRunSaveService? _originalSave;
    private ActiveRunLoadDiagnostic? _profileDiagnostic;
    private RunProgressionPersistenceService _persistence;
    private RunRewardEconomyService _rewards;
    private RunEquipmentService _equipment;
    private RunFormationService _formation;
    private RunBattlePreparationService _battlePreparation;
    private RunNodeResolutionService _nodes;
    private RunDecisionService _decisions;
    private GrowthRunService _growth;

    public RunApplication(ContentRegistry content, IRunSaveService save, CompiledGameProject project, CompiledGrowthRules? growthRules = null,
        CompiledRunProfile? profile = null, bool loadExisting = true)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(save);
        ArgumentNullException.ThrowIfNull(project);
        if (!ReferenceEquals(content.Catalog, project.Content) &&
            content.Catalog.ResourcePath != project.Content.ResourcePath)
            throw new ArgumentException(
                "Run application content registry does not match the compiled project.",
                nameof(project));

        _content = content;
        _project = project;
        _tower = project.Campaign.Progression is CompiledTowerProgression ? new TowerGenerator(project.Campaign) : null;
        _persistence = new RunProgressionPersistenceService(content, save, project, growthRules, profile, initializeMeta: loadExisting);
        ActiveRun = loadExisting ? _persistence.LoadActiveRun() : null;
        _growth = new GrowthRunService(content, project, growthRules, _persistence);
        _rewards = new RunRewardEconomyService(content, project, _persistence, ActiveRun, _growth);
        _decisions = new RunDecisionService(content, project, _persistence, _growth);
        _equipment = new RunEquipmentService(content.Graph, project.RunRules, _persistence);
        _formation = new RunFormationService(project.RunRules, _persistence);
        var relics = new RunRelicService(content);
        _battlePreparation = new RunBattlePreparationService(content, project, relics, growthRules);
        _nodes = _tower is null ? null! : new RunNodeResolutionService(
            project,
            _tower,
            _persistence,
            _battlePreparation,
            _rewards,
            _decisions,
            _growth);
        if (project.Campaign.Progression is CompiledExpedition)
            _actions = new(content, project, _persistence, _growth, _rewards, _decisions, _battlePreparation);
        ApplyMasterVolume();
    }

    public RunApplication(CompiledGamePackage package, IRunSaveService save, string saveNamespace)
        : this(package, save, RunSessionFactory.ResolveProfile(package, save, saveNamespace)) { }

    private RunApplication(CompiledGamePackage package, IRunSaveService save, RunSessionResolution resolution)
        : this(package.Content, resolution.SaveService, (resolution.Profile ?? package.DefaultProfile).Project,
            (resolution.Profile ?? package.DefaultProfile).GrowthRules, resolution.Profile ?? package.DefaultProfile,
            loadExisting: resolution.IsSuccess)
    {
        _package = package; _originalSave = save; _profileDiagnostic = resolution.Diagnostic;
    }


    public MetaProgressDto Meta => _persistence.Meta;
    public SettingsDto Settings => _persistence.Settings;
    public ActiveRunLoadDiagnostic? ActiveRunLoadDiagnostic => _profileDiagnostic ?? _persistence.LastActiveRunLoadDiagnostic;
    internal ActiveRunDto? ActiveRun { get; private set; }
    public RunView? Run => ActiveRun is { } value ? RunView.For(value) : null;
    public ContentRegistry Content => _content;
    public CompiledGameProject Project => _project;
    public CompiledRunRules Rules => _project.RunRules;
    public TowerGenerator Tower => _tower ?? throw new InvalidOperationException("当前进程不是爬塔。");
    public CompiledGrowthRules? GrowthRules => _growth.Rules;

    // Compatibility fixture/tool entry point. Production navigation uses the saved
    // opening draft and ConfirmOpeningRecruitment; it never grants random companions.
    public bool StartNewRun(string heroId, ulong seed)
    {
        var run = _rewards.CreateNewRun(heroId, seed, Meta.UnlockedHeroIds);
        if (run is null) return false;
        _nodes?.ResetRunLifecycle();
        ActiveRun = run;
        EquipmentEditingLocked = false;
        return true;
    }

    public bool BeginOpeningRecruitment(ulong seed)
    {
        // Create the complete default-profile session before publication. Save failure
        // preserves both the previous run and every service bound to its rules.
        if (_package is { } package && _originalSave is { } save)
        {
            var profile = package.DefaultProfile;
            var candidate = new RunApplication(package.Content, save, profile.Project, profile.GrowthRules, profile, loadExisting: false);
            var draft = candidate._rewards.CreateOpening(seed);
            if (draft is null) return false;
            _project = candidate._project; _tower = candidate._tower; _persistence = candidate._persistence;
            _growth = candidate._growth; _rewards = candidate._rewards; _decisions = candidate._decisions;
            _equipment = candidate._equipment; _formation = candidate._formation;
            _battlePreparation = candidate._battlePreparation; _nodes = candidate._nodes; _actions = candidate._actions;
            ActiveRun = draft; _profileDiagnostic = null; _previewEncounter = null; _previewServiceSiteId = null; EquipmentEditingLocked = false;
            return true;
        }
        var run = _rewards.CreateOpening(seed);
        if (run is null) return false;
        _nodes?.ResetRunLifecycle();
        ActiveRun = run;
        EquipmentEditingLocked = false;
        return true;
    }

    public bool ToggleOpeningHero(string heroId) => _rewards.ToggleOpeningHero(ActiveRun, heroId);
    public bool ConfirmOpeningRecruitment() => _rewards.ConfirmOpening(ActiveRun, _actions is null ? null : _actions.Initialize);

    public void AbandonRun()
    {
        _nodes?.ResetRunLifecycle();
        ActiveRun = null;
        _previewEncounter = null; _previewServiceSiteId = null;
        EquipmentEditingLocked = false;
        _persistence.DeleteActiveRun();
    }

    public IReadOnlyList<TowerNodeOption> CurrentOptions() => _nodes?.CurrentOptions(ActiveRun) ?? [];

    // Only choices the player may make now; committed older choices resolve through CurrentEncounter.
    public IReadOnlyList<OpponentOption> CurrentOpponentOptions(TowerNodeType type) =>
        _nodes?.CurrentOpponentOptions(ActiveRun, type) ?? [];

    public EncounterPlan CurrentEncounter() => IsExpedition
        ? ActiveRun?.Action?.Encounter ?? _previewEncounter ?? throw new InvalidOperationException("没有已选择的遭遇。")
        : _nodes.CurrentEncounter(ActiveRun);

    public bool SelectNode(TowerNodeType type, string? opponentOptionId = null) =>
        _nodes?.SelectNode(ActiveRun, type, opponentOptionId) ?? false;
    public GrowthCommandResult CheckGrowthAction() => _growth.CheckAction(ActiveRun);
    public GrowthCommandResult SetGrowthAssignment(string producerInstanceId, string targetInstanceId, GrowthProductionMode mode) =>
        _growth.SetAssignment(ActiveRun, producerInstanceId, targetInstanceId, mode);
    public GrowthCommandResult AscendHero(string heroInstanceId) => _growth.Ascend(ActiveRun, heroInstanceId);
    public GrowthCommandResult ChooseGrowthHero(string candidateContentId) => _growth.ChooseHero(ActiveRun, candidateContentId);
    public GrowthCommandResult CraftGrowthSpell(string spellId) => _growth.CraftSpell(ActiveRun, spellId);
    public GrowthCommandResult EquipGrowthSpell(string spellId, string targetHeroInstanceId) =>
        _growth.EquipSpell(ActiveRun, spellId, targetHeroInstanceId);
    public GrowthCommandResult TryBeginGrowthBattle(EncounterPlan encounter)
    {
        if (_actions is not null && ActiveRun is { } expeditionRun) return _actions.BeginBattle(expeditionRun, encounter);
        if (GrowthRules is null) return GrowthCommandResult.Success();
        if (ActiveRun is not { } run) return GrowthCommandResult.Reject("没有活动征程。");
        try { _battlePreparation.Build(run, encounter, true); }
        catch (Exception exception) { return GrowthCommandResult.Reject("当前阵型或战斗配置无效：" + exception.Message); }
        return _growth.BeginBattle(run, encounter);
    }
    public PendingRunOffer? PendingOffer => ActiveRun?.PendingOffer is { } offer
        ? _decisions.RelicRules.PresentOffer(ActiveRun, RunItemSupplyPolicy.VisibleOffer(ActiveRun,
            RunRecruitmentPolicy.VisibleOffer(ActiveRun, IsExpedition ? offer : RunOfferDefaults.RefreshHealthChoices(offer, Rules)), _content))
        : ActiveRun is { } previewRun && _previewServiceSiteId is { } id ? _actions?.SiteOffer(previewRun, id) : null;
    public string RewardSkipText => ActiveRun is { } run && PendingOffer is { } offer
        ? _decisions.RelicRules.SkipText(run, offer) : "跳过";
    public RunDecisionResult ResolveOffer(string offerId, string? choiceId) => EquipmentEditingLocked ||
        ActiveRun is { } pendingRun && (GrowthRunService.GetPending(pendingRun)?.IsBattle == true || pendingRun.Growth?.PendingDiscovery is not null)
        ? RunDecisionResult.Reject(RunDecisionFailure.NotEligible, "战斗尚未完成结算。")
        : _actions is not null && ActiveRun is { } run ? ResolveExpeditionOffer(run, offerId, choiceId) : _decisions.Resolve(ActiveRun, offerId, choiceId);
    public RunDecisionResult CheckOfferChoice(CompiledRunChoice choice) => ActiveRun is null
        ? RunDecisionResult.Reject(RunDecisionFailure.NoOffer, "没有活动征程。")
        : _actions is not null ? _actions.CheckChoice(ActiveRun, choice, _previewServiceSiteId) : _decisions.Check(ActiveRun, choice);
    public bool ResumeTerminalCompletion()
    {
        if (ActiveRun is not { } run || string.IsNullOrEmpty(run.TerminalCompletionId) || !_persistence.TryCompleteTerminal(run)) return false;
        ActiveRun = null;
        return true;
    }

    public void FinishNonCombatNode() { if (PreparationEditingLocked) return; if (_actions is not null && ActiveRun is { } run) { _actions.LeaveService(run); _previewServiceSiteId = null; } else _nodes.FinishNonCombatNode(ActiveRun); }

    public IReadOnlyList<CatalogEntry> RecruitmentChoices(int salt = 0) =>
        PickEntries(_project.Campaign.RecruitmentPool, Rules.RecruitmentChoiceCount, salt);

    public IReadOnlyList<CatalogEntry> ItemChoices(int salt = 0) =>
        PickEntries(_project.Campaign.ItemRewardPool, Rules.ItemChoiceCount, salt);

    public IReadOnlyList<CatalogEntry> ShopChoices(int salt = 0) =>
        PickEntries(_project.Campaign.ShopPool, Rules.ItemChoiceCount, salt);

    public bool Recruit(string rosterHeroId) => !PreparationEditingLocked && _rewards.Recruit(ActiveRun, rosterHeroId);

    public RunPopulationFacts? Population => ActiveRun is null
        ? null
        : RunPopulationPolicy.Evaluate(ActiveRun, Rules);

    public bool GrantPopulation(int amount) => !PreparationEditingLocked && _rewards.GrantPopulation(ActiveRun, amount);

    public bool GrantPopulationFromSource(string sourceId, int populationAmount, int effectiveCapIncrease) =>
        !PreparationEditingLocked && _rewards.GrantPopulationFromSource(ActiveRun, sourceId, populationAmount, effectiveCapIncrease);

    public int ConvertRecruitToGold() => _rewards.ConvertRecruitToGold(ActiveRun);

    private bool _equipmentEditingLocked;
    public bool EquipmentEditingLocked
    {
        get => _equipmentEditingLocked || !string.IsNullOrEmpty(ActiveRun?.TerminalCompletionId);
        private set => _equipmentEditingLocked = value;
    }

    // The formal battle lifecycle holds this lock through settlement. A preview
    // BuildBattleConfig is deliberately read-only and never locks the run.
    public void SetEquipmentEditingLocked(bool locked) => EquipmentEditingLocked = locked;

    public bool BuyItem(string itemId) => !PreparationEditingLocked && _rewards.BuyItem(ActiveRun, itemId);

    public bool GrantItem(string itemId) => !PreparationEditingLocked && _rewards.GrantItem(ActiveRun, itemId);

    public bool EquipItem(string ownerHeroInstanceId, int slotIndex, string equipmentContentId) =>
        !PreparationEditingLocked && _equipment.Equip(ActiveRun, ownerHeroInstanceId, slotIndex, equipmentContentId);

    public bool EquipOwnedItem(string equipmentInstanceId, string ownerHeroInstanceId, int slotIndex) =>
        !PreparationEditingLocked && _equipment.EquipOwned(ActiveRun, equipmentInstanceId, ownerHeroInstanceId, slotIndex);

    public bool RemoveEquipment(string ownerHeroInstanceId, int slotIndex) =>
        !PreparationEditingLocked && _equipment.Remove(ActiveRun, ownerHeroInstanceId, slotIndex);

    public bool EquipDeployment(string instanceId, int slot) => MoveDeploymentUnit(instanceId, slot);

    public bool MoveDeploymentUnit(string instanceId, int slot) =>
        !PreparationEditingLocked && _formation.MoveDeploymentUnit(ActiveRun, instanceId, slot);

    public bool ApplyFormationCommand(FormationMoveCommand command, IBattleFloorRuleRuntime floorRule) =>
        !PreparationEditingLocked && _formation.Apply(ActiveRun, command, floorRule);

    public FormationEvaluation EvaluateFormationCommand(
        FormationMoveCommand command,
        IBattleFloorRuleRuntime floorRule) =>
        _formation.Evaluate(ActiveRun, command, floorRule);

    public bool WithdrawDeploymentUnit(string instanceId) => !PreparationEditingLocked && _formation.Withdraw(ActiveRun, instanceId);

    public void ClearDeploymentSlot(int slot) { if (!PreparationEditingLocked) _formation.ClearSlot(ActiveRun, slot); }

    private bool PreparationEditingLocked => EquipmentEditingLocked ||
        ActiveRun?.Growth is { PendingDiscovery: not null } ||
        ActiveRun is { } lockedRun && (GrowthRunService.GetPending(lockedRun)?.IsBattle == true || lockedRun.Action?.Phase == RunActionPhase.Rewards);

    public void Rest(bool takeGold) => _rewards.Rest(ActiveRun, takeGold);

    public void ResolveEvent(bool risky) => _rewards.ResolveEvent(ActiveRun, risky);

    public BattleConfig BuildBattleConfig(EncounterPlan encounter, bool requireLegalFormation = true)
    {
        var run = ActiveRun ?? throw new InvalidOperationException("No active run");
        return _actions is not null ? _actions.BuildBattlePreview(run, encounter, requireLegalFormation)
            : _battlePreparation.Build(run, encounter, requireLegalFormation);
    }

    public TraitSnapshot BuildTraitSnapshot(
        IEnumerable<TraitExplicitContribution>? explicitExtras = null)
    {
        var run = ActiveRun ?? throw new InvalidOperationException("No active run");
        return RunTraitSnapshotBuilder.Build(run, _content.Graph, explicitExtras);
    }

    public RunBattleResolution ResolveBattle(BattleResult result, EncounterPlan encounter)
    {
        var resolution = _actions is not null && ActiveRun is { } run
            ? _actions.Complete(run, result, encounter) : _nodes.CompleteBattle(ActiveRun, result, encounter);
        if (resolution.Accepted) ActiveRun = resolution.ActiveRun;
        return resolution;
    }

    // Compatibility facade for existing non-UI callers. Flow coordination must use
    // ResolveBattle so rejection is never conflated with an accepted defeat/timeout.
    public bool CompleteBattle(BattleResult result, EncounterPlan encounter) =>
        ResolveBattle(result, encounter).FacadeReturnValue;

    public void SaveSettings()
    {
        _persistence.SaveSettings();
        ApplyMasterVolume();
    }

    private IReadOnlyList<CatalogEntry> PickEntries(
        CompiledContentPool source,
        int count,
        int salt)
    {
        var run = ActiveRun ?? throw new InvalidOperationException("No active run");
        return _rewards.PickEntries(run, source, count, salt);
    }

    private void ApplyMasterVolume()
    {
        var master = AudioServer.GetBusIndex("Master");
        if (master >= 0)
        {
            AudioServer.SetBusMute(master, Settings.MasterVolume <= 0);
            AudioServer.SetBusVolumeDb(
                master,
                Mathf.LinearToDb(Mathf.Max(.001f, Settings.MasterVolume)));
        }
    }
}
