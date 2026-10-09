using System;
using TowerAutobattler.Expedition;
using TowerAutobattler.Project;

namespace TowerAutobattler.Run;

public sealed partial class RunApplication
{
    private EncounterPlan? _previewEncounter;
    private string? _previewServiceSiteId;
    public bool IsExpedition => _project.Campaign.Progression is CompiledExpedition;
    public ExpeditionMapView? Expedition => ActiveRun?.Expedition is { } map ? ExpeditionMapProjection.Create(map) : null;
    public int ExpeditionStageIndex => ActiveRun?.Expedition?.StageIndex ?? 0;
    public int ExpeditionRemainingActions => ActiveRun?.Expedition?.RemainingActions ?? 0;
    public bool IsFinalExpeditionStage => IsExpedition && ExpeditionStageIndex == ((CompiledExpedition)_project.Campaign.Progression).Stages.Length - 1;
    public bool ExpeditionBattleCommitted => ActiveRun?.Action is { Phase: RunActionPhase.Battle, BattleFreeze: not null };
    public bool ExpeditionInteractionLocked => ActiveRun is not { } run || _actions?.Locked(run) != false;
    public bool CanChallengeExpeditionBoss => ActiveRun is { } run && _actions?.CanBoss(run) == true;
    public bool CanMoveExpedition(int cellIndex) => ActiveRun is { } run && _actions?.CanMove(run, cellIndex) == true;
    public bool CanInteractExpeditionSite(string siteId) => ActiveRun is { } run && _actions?.CanInteract(run, siteId) == true;
    public bool MoveExpedition(int cellIndex) { _previewServiceSiteId = null; return ActiveRun is { } run && _actions?.Move(run, cellIndex) == true; }
    public bool InspectExpeditionSite(string siteId)
    {
        if (ActiveRun is not { } run || _actions?.Inspect(run, siteId) != true) return false;
        _previewServiceSiteId = _actions.SiteOffer(run, siteId) is not null ? siteId : null;
        return true;
    }
    private RunDecisionResult ResolveExpeditionOffer(ActiveRunDto run, string offerId, string? choiceId)
    {
        var result = _actions!.Resolve(run, offerId, choiceId, _previewServiceSiteId);
        if (result.Succeeded && run.Action is null) _previewServiceSiteId = null;
        return result;
    }
    public PendingRunOffer? PreviewExpeditionSiteOffer(string siteId) => ActiveRun is { } run ? _actions?.SiteOffer(run, siteId) : null;
    public EncounterPlan PreviewExpeditionEncounter(string siteId)
    {
        _previewServiceSiteId = null;
        return _previewEncounter = _actions!.Preview(ActiveRun ?? throw new InvalidOperationException("没有征程。"), siteId);
    }
    public EncounterPlan PreviewExpeditionBoss()
    {
        _previewServiceSiteId = null;
        return _previewEncounter = _actions!.Preview(ActiveRun ?? throw new InvalidOperationException("没有征程。"), "", true);
    }
}
