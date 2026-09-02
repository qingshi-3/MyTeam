using System;
using System.Collections.Generic;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.Equipment;
using TowerAutobattler.Project;
using TowerAutobattler.Traits;

namespace TowerAutobattler.Run;

// Stable use-case facade. Cohesive services own formation, node resolution,
// Battle preparation, reward/economy, and progression/persistence behavior.
public sealed class RunApplication
{
    private readonly ContentRegistry _content;
    private readonly CompiledGameProject _project;
    private readonly TowerGenerator _tower;
    private readonly RunProgressionPersistenceService _persistence;
    private readonly RunRewardEconomyService _rewards;
    private readonly RunEquipmentService _equipment;
    private readonly RunFormationService _formation;
    private readonly RunBattlePreparationService _battlePreparation;
    private readonly RunNodeResolutionService _nodes;

    public RunApplication(ContentRegistry content, IRunSaveService save, CompiledGameProject project)
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
        _tower = new TowerGenerator(project.Campaign);
        _persistence = new RunProgressionPersistenceService(content, save, project);
        ActiveRun = _persistence.LoadActiveRun();
        _rewards = new RunRewardEconomyService(content, project, _persistence, ActiveRun);
        _equipment = new RunEquipmentService(content.Graph, project.RunRules, _persistence);
        _formation = new RunFormationService(project.RunRules, _persistence);
        var relics = new RunRelicService(content);
        _battlePreparation = new RunBattlePreparationService(content, project, relics);
        _nodes = new RunNodeResolutionService(
            project,
            _tower,
            _persistence,
            _battlePreparation,
            _rewards);
        ApplyMasterVolume();
    }

    public MetaProgressDto Meta => _persistence.Meta;
    public SettingsDto Settings => _persistence.Settings;
    public ActiveRunLoadDiagnostic? ActiveRunLoadDiagnostic => _persistence.LastActiveRunLoadDiagnostic;
    public ActiveRunDto? ActiveRun { get; private set; }
    public ContentRegistry Content => _content;
    public CompiledGameProject Project => _project;
    public CompiledRunRules Rules => _project.RunRules;
    public TowerGenerator Tower => _tower;

    public bool StartNewRun(string heroId, ulong seed)
    {
        var run = _rewards.CreateNewRun(heroId, seed, Meta.UnlockedHeroIds);
        if (run is null) return false;
        _nodes.ResetRunLifecycle();
        ActiveRun = run;
        return true;
    }

    public void AbandonRun()
    {
        _nodes.ResetRunLifecycle();
        ActiveRun = null;
        _persistence.DeleteActiveRun();
    }

    public IReadOnlyList<TowerNodeOption> CurrentOptions() => _nodes.CurrentOptions(ActiveRun);

    public EncounterPlan CurrentEncounter() => _nodes.CurrentEncounter(ActiveRun);

    public bool SelectNode(TowerNodeType type) => _nodes.SelectNode(ActiveRun, type);

    public void FinishNonCombatNode() => _nodes.FinishNonCombatNode(ActiveRun);

    public IReadOnlyList<CatalogEntry> RecruitmentChoices(int salt = 0) =>
        PickEntries(_project.Campaign.RecruitmentPool, Rules.RecruitmentChoiceCount, salt);

    public IReadOnlyList<CatalogEntry> ItemChoices(int salt = 0) =>
        PickEntries(_project.Campaign.ItemRewardPool, Rules.ItemChoiceCount, salt);

    public IReadOnlyList<CatalogEntry> ShopChoices(int salt = 0) =>
        PickEntries(_project.Campaign.ShopPool, Rules.ItemChoiceCount, salt);

    public bool Recruit(string rosterHeroId) => _rewards.Recruit(ActiveRun, rosterHeroId);

    public RunPopulationFacts? Population => ActiveRun is null
        ? null
        : RunPopulationPolicy.Evaluate(ActiveRun, Rules);

    public bool GrantPopulation(int amount) => _rewards.GrantPopulation(ActiveRun, amount);

    public bool GrantPopulationFromSource(string sourceId, int populationAmount, int effectiveCapIncrease) =>
        _rewards.GrantPopulationFromSource(ActiveRun, sourceId, populationAmount, effectiveCapIncrease);

    public int ConvertRecruitToGold() => _rewards.ConvertRecruitToGold(ActiveRun);

    public bool BuyItem(string itemId) => _rewards.BuyItem(ActiveRun, itemId);

    public bool GrantItem(string itemId) => _rewards.GrantItem(ActiveRun, itemId);

    public bool EquipItem(string ownerHeroInstanceId, int slotIndex, string equipmentContentId) =>
        _equipment.Equip(ActiveRun, ownerHeroInstanceId, slotIndex, equipmentContentId);

    public bool RemoveEquipment(string ownerHeroInstanceId, int slotIndex) =>
        _equipment.Remove(ActiveRun, ownerHeroInstanceId, slotIndex);

    public bool EquipDeployment(string instanceId, int slot) => MoveDeploymentUnit(instanceId, slot);

    public bool MoveDeploymentUnit(string instanceId, int slot) =>
        _formation.MoveDeploymentUnit(ActiveRun, instanceId, slot);

    public bool ApplyFormationCommand(FormationMoveCommand command, IBattleFloorRuleRuntime floorRule) =>
        _formation.Apply(ActiveRun, command, floorRule);

    public FormationEvaluation EvaluateFormationCommand(
        FormationMoveCommand command,
        IBattleFloorRuleRuntime floorRule) =>
        _formation.Evaluate(ActiveRun, command, floorRule);

    public bool WithdrawDeploymentUnit(string instanceId) => _formation.Withdraw(ActiveRun, instanceId);

    public void ClearDeploymentSlot(int slot) => _formation.ClearSlot(ActiveRun, slot);

    public void Rest(bool takeGold) => _rewards.Rest(ActiveRun, takeGold);

    public void ResolveEvent(bool risky) => _rewards.ResolveEvent(ActiveRun, risky);

    public BattleConfig BuildBattleConfig(EncounterPlan encounter, bool requireLegalFormation = true)
    {
        var run = ActiveRun ?? throw new InvalidOperationException("No active run");
        return _battlePreparation.Build(run, encounter, requireLegalFormation);
    }

    public TraitSnapshot BuildTraitSnapshot(
        IEnumerable<TraitExplicitContribution>? explicitExtras = null)
    {
        var run = ActiveRun ?? throw new InvalidOperationException("No active run");
        return RunTraitSnapshotBuilder.Build(run, _content.Graph, explicitExtras);
    }

    public RunBattleResolution ResolveBattle(BattleResult result, EncounterPlan encounter)
    {
        var resolution = _nodes.CompleteBattle(ActiveRun, result, encounter);
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
            AudioServer.SetBusVolumeDb(
                master,
                Mathf.LinearToDb(Mathf.Max(.001f, Settings.MasterVolume)));
    }
}
