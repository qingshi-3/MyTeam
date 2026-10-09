using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.Run;

namespace TowerAutobattler.Growth;

public sealed partial class GrowthRunService
{
    // Receipts are bounded facts from the authoritative simulation. Reconstruct
    // eligibility from frozen inputs; do not trust receipt-supplied partner links.
    public bool ApplyBattleRememberedGains(ActiveRunDto active, ActiveRunDto working,
        BattleResult result, BattlePreparationSnapshot? opening, bool apply)
    {
        var gains = result.RememberedGains;
        if (gains.IsDefaultOrEmpty) return true;
        if (rules is null || opening is null || ReferenceEquals(active, working) ||
            GetPending(active) is not { IsBattle: true } frozen ||
            GetPending(working) is not { IsBattle: true } pending ||
            frozen.Source != pending.Source || frozen.BattleNumber != pending.BattleNumber ||
            !MatchesCurrentAction(active, frozen) || AlreadySettled(working, pending) ||
            result.Units.IsDefault || result.Ticks < 0 || result.Units.Any(report => report is null ||
                report.JoinTick < 0 || report.JoinTick > result.Ticks ||
                report.DefeatTick is { } death && (death < report.JoinTick || death > result.Ticks))) return false;
        var permanent = opening.Units.Where(unit => unit.Team == 0 && unit.Definition.IsHero &&
            !unit.IsTemporary && frozen.ParticipantInstanceIds.Contains(unit.SourceInstanceId)).ToArray();
        if (permanent.GroupBy(unit => unit.SourceInstanceId, StringComparer.Ordinal).Any(group => group.Count() != 1)) return false;
        var units = permanent.ToDictionary(unit => unit.SourceInstanceId, StringComparer.Ordinal);
        // At most two partners, each with AD/HP, per authored source group.
        var maximum = permanent.Sum(unit => MatrixOperations(unit.Definition.AbilityLoadout)
            .Count(operation => operation.Kind == MatrixOperationKind.GrantRememberableAttribute)) * 2;
        if (gains.Length > maximum || gains.Any(gain => gain is null ||
            string.IsNullOrWhiteSpace(gain.SourceInstanceId) || string.IsNullOrWhiteSpace(gain.TargetInstanceId) ||
            string.IsNullOrWhiteSpace(gain.AbilityId) || string.IsNullOrWhiteSpace(gain.RememberAbilityId))) return false;
        var validated = new List<(RosterHeroInstanceDto Hero, BattleRememberedGain Gain)>();
        foreach (var group in gains.GroupBy(gain => (gain.SourceInstanceId, gain.TargetInstanceId,
                     gain.AbilityId, gain.OperationKey, gain.GrantOrdinal)))
        {
            var pair = group.ToArray();
            if (pair.Length != 2 || pair.Count(gain => gain.Attribute == CombatAttribute.AttackDamage) != 1 ||
                pair.Count(gain => gain.Attribute == CombatAttribute.MaxHealth) != 1 ||
                pair[0].GrantedTick != pair[1].GrantedTick || pair[0].RememberAbilityId != pair[1].RememberAbilityId ||
                pair[0].RememberOperationKey != pair[1].RememberOperationKey) return false;
            foreach (var gain in pair)
            {
                if (gain.GrantOrdinal != 1 || gain.GrantedTick < 0 || gain.GrantedTick > result.Ticks ||
                    gain.SourceInstanceId == gain.TargetInstanceId || !float.IsFinite(gain.Amount) || gain.Amount <= 0 ||
                    !units.TryGetValue(gain.SourceInstanceId, out var source) ||
                    !units.TryGetValue(gain.TargetInstanceId, out var target) ||
                    !MatchesRememberedOperations(source.Definition.AbilityLoadout, target.Definition.AbilityLoadout, gain)) return false;
                // These links were rebuilt from the frozen config above, never
                // supplied by the result. Use binding-time positions: a full-mana
                // opener can move after binding but before the preview snapshot.
                var partners = opening.OpeningAllyLinks.IsDefault ? [] : opening.OpeningAllyLinks.Where(link =>
                    link.SourceRuntimeId == source.RuntimeId && link.SourceInstanceId == source.SourceInstanceId &&
                    link.GrantKey == gain.OperationKey).ToArray();
                if (partners.Length > 2 || partners.Count(link => link.TargetRuntimeId == target.RuntimeId &&
                        link.TargetInstanceId == gain.TargetInstanceId) != 1 || !ValidGrantTime(source, gain.GrantedTick) ||
                    !ValidGrantTime(target, gain.GrantedTick)) return false;
                var hero = working.Roster.SingleOrDefault(candidate => candidate.InstanceId == gain.TargetInstanceId);
                if (hero is null || hero.Growth.History.Any(old => old.Source == "remembered" &&
                    SameGainAction(working, old, pending.Source, pending.FloorIndex, pending.BattleNumber) &&
                    old.ProducerInstanceId == gain.SourceInstanceId && old.AbilityId == gain.AbilityId &&
                    old.OperationKey == gain.OperationKey && old.GrantOrdinal == gain.GrantOrdinal &&
                    old.Mode == ToGrowthMode(gain.Attribute))) return false;
                validated.Add((hero, gain));
            }
        }
        // Validate all additions before changing even the detached projection.
        foreach (var target in validated.GroupBy(value => value.Hero))
            if (!float.IsFinite(target.Key.Growth.AddedAttack + target.Where(value => value.Gain.Attribute == CombatAttribute.AttackDamage).Sum(value => value.Gain.Amount)) ||
                !float.IsFinite(target.Key.Growth.AddedMaxHealth + target.Where(value => value.Gain.Attribute == CombatAttribute.MaxHealth).Sum(value => value.Gain.Amount))) return false;
        if (!apply) return true;
        foreach (var (hero, gain) in validated)
        {
            if (gain.Attribute == CombatAttribute.AttackDamage) hero.Growth.AddedAttack += gain.Amount;
            else hero.Growth.AddedMaxHealth += gain.Amount;
            hero.Growth.History.Add(new GrowthGainDto
            {
                ActionSource = pending.Source, FloorIndex = pending.FloorIndex, BattleNumber = pending.BattleNumber,
                ProducerInstanceId = gain.SourceInstanceId, TargetInstanceId = gain.TargetInstanceId,
                Mode = ToGrowthMode(gain.Attribute), Amount = gain.Amount, AbilityId = gain.AbilityId,
                Source = "remembered", OperationKey = gain.OperationKey, RememberAbilityId = gain.RememberAbilityId,
                RememberOperationKey = gain.RememberOperationKey, GrantOrdinal = gain.GrantOrdinal, GrantedTick = gain.GrantedTick
            });
        }
        return true;

        bool ValidGrantTime(BattlePreparedUnit unit, int tick)
        {
            var matches = result.Units.Where(report => report.Team == 0 && report.IsHero && !report.IsTemporary &&
                report.RuntimeId == unit.RuntimeId && report.SourceInstanceId == unit.SourceInstanceId &&
                report.ContentId == unit.Definition.ContentId).ToArray();
            return matches.Length == 1 && matches[0].JoinTick <= tick &&
                (matches[0].DefeatTick is not { } defeat || tick <= defeat);
        }
    }

    private static GrowthProductionMode ToGrowthMode(CombatAttribute attribute) =>
        attribute == CombatAttribute.AttackDamage ? GrowthProductionMode.Attack : GrowthProductionMode.Vitality;

    private static IEnumerable<CompiledMatrixOperation> MatrixOperations(CompiledAbilityLoadout? loadout) =>
        loadout?.Abilities.SelectMany(ability => ability.Operations.OfType<CompiledMatrixOperation>()
            .SelectMany(MatrixAbilityCompiler.Flatten)) ?? [];

    private static bool MatchesRememberedOperations(CompiledAbilityLoadout? source, CompiledAbilityLoadout? target,
        BattleRememberedGain gain)
    {
        var grant = source?.Find(gain.AbilityId);
        var remember = target?.Find(gain.RememberAbilityId);
        return !string.IsNullOrWhiteSpace(gain.OperationKey) && !string.IsNullOrWhiteSpace(gain.RememberOperationKey) &&
            grant is { IsActiveSkill: true } && grant.Operations.OfType<CompiledMatrixOperation>()
                .SelectMany(MatrixAbilityCompiler.Flatten).Count(operation =>
                    operation.Kind == MatrixOperationKind.GrantRememberableAttribute && operation.Key == gain.OperationKey &&
                    operation.Attribute == gain.Attribute && operation.Amount == gain.Amount) == 1 &&
            source!.Abilities.Where(ability => ability.Trigger == AbilityTriggerKind.OwnerEnteredBattle)
                .SelectMany(ability => ability.Operations.OfType<CompiledMatrixOperation>()).SelectMany(MatrixAbilityCompiler.Flatten)
                .Any(operation => operation.Kind == MatrixOperationKind.BindOpeningAllies && operation.Key == gain.OperationKey) &&
            remember?.Trigger == AbilityTriggerKind.OwnerEnteredBattle &&
            remember.Operations.OfType<CompiledMatrixOperation>().SelectMany(MatrixAbilityCompiler.Flatten).Count(operation =>
                operation.Kind == MatrixOperationKind.RememberAllyFixedGains && operation.Key == gain.RememberOperationKey) == 1;
    }

    private static bool ValidRememberedMetadata(GrowthGainDto gain) => gain.ProducerInstanceId != gain.TargetInstanceId &&
        gain.Mode is GrowthProductionMode.Attack or GrowthProductionMode.Vitality && gain.Amount > 0 &&
        !string.IsNullOrWhiteSpace(gain.OperationKey) && !string.IsNullOrWhiteSpace(gain.RememberAbilityId) &&
        !string.IsNullOrWhiteSpace(gain.RememberOperationKey) && gain.GrantOrdinal == 1 && gain.GrantedTick >= 0;

    private bool ValidRememberedHistory(ActiveRunDto run)
    {
        foreach (var hero in run.Roster)
        {
            var gains = hero.Growth.History.Where(gain => gain.Source == "remembered").ToArray();
            if (gains.Any(gain => !ValidRememberedMetadata(gain) || gain.TargetInstanceId != hero.InstanceId)) return false;
            foreach (var group in gains.GroupBy(gain => (gain.ActionSource, gain.FloorIndex, gain.BattleNumber,
                         gain.ProducerInstanceId, gain.AbilityId, gain.OperationKey, gain.GrantOrdinal)))
            {
                var pair = group.ToArray();
                if (pair.Length != 2 || pair.Count(gain => gain.Mode == GrowthProductionMode.Attack) != 1 ||
                    pair.Count(gain => gain.Mode == GrowthProductionMode.Vitality) != 1 ||
                    pair[0].GrantedTick != pair[1].GrantedTick || pair[0].RememberAbilityId != pair[1].RememberAbilityId ||
                    pair[0].RememberOperationKey != pair[1].RememberOperationKey) return false;
                var source = run.Roster.SingleOrDefault(candidate => candidate.InstanceId == pair[0].ProducerInstanceId);
                if (source is null || !rules!.Heroes.TryGetValue(source.ContentId, out var sourceRule) ||
                    !rules.Heroes.TryGetValue(hero.ContentId, out var targetRule)) return false;
                foreach (var gain in pair)
                {
                    var receipt = new BattleRememberedGain(gain.ProducerInstanceId, gain.TargetInstanceId, gain.AbilityId,
                        gain.OperationKey, gain.RememberAbilityId, gain.RememberOperationKey,
                        gain.Mode == GrowthProductionMode.Attack ? CombatAttribute.AttackDamage : CombatAttribute.MaxHealth,
                        gain.Amount, gain.GrantOrdinal, gain.GrantedTick);
                    // History can precede ascension; either authored loadout is legal.
                    if (!new[] { sourceRule.BaseLoadout, sourceRule.AscensionBenefit?.BattleLoadout }.Any(sourceLoadout =>
                        new[] { targetRule.BaseLoadout, targetRule.AscensionBenefit?.BattleLoadout }.Any(targetLoadout =>
                            MatchesRememberedOperations(sourceLoadout, targetLoadout, receipt)))) return false;
                }
            }
            if (gains.Where(gain => gain.Mode == GrowthProductionMode.Attack).Sum(gain => (double)gain.Amount) > hero.Growth.AddedAttack ||
                gains.Where(gain => gain.Mode == GrowthProductionMode.Vitality).Sum(gain => (double)gain.Amount) > hero.Growth.AddedMaxHealth) return false;
        }
        return true;
    }
}
