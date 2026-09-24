using System;
using System.Collections.Generic;
using System.Linq;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.Statuses;

namespace TowerAutobattler.Audio;

// Routing is based on published events. Flight positions and repeating status
// snapshots never produce sound, and an attack windup is not an arrow release.
public static class BattleAudioFeedback
{
    public static string EventCue(BattleEvent fact, AttackDelivery delivery = AttackDelivery.Melee)
    {
        if (fact.Vfx is { } vfx)
        {
            if (vfx.Phase == BattleVfxPhase.ShieldDepleted) return "shield_break";
            if (vfx.Phase is BattleVfxPhase.Burst or BattleVfxPhase.SequenceFired)
            {
                var effectCue = EffectCue(vfx.EffectId);
                if (!string.IsNullOrEmpty(effectCue)) return effectCue;
            }
        }
        return fact.Type switch
        {
            "projectile_spawn" => "arrow_release",
            "projectile_impact" => "arrow_hit",
            "attack" when delivery == AttackDelivery.Melee => "melee_swing",
            "beam" => "skill",
            "summoned" or "revived" => "summon",
            "defeated" => "death",
            _ => ""
        };
    }

    public static string CombatCue(BattleCombatEvent fact, AttackDelivery delivery) => fact.Kind switch
    {
        BattleCombatEventKind.AttackLanded when delivery == AttackDelivery.Melee => "melee_hit",
        BattleCombatEventKind.ShieldResolved when fact.EffectiveValue > 0 => "shield",
        BattleCombatEventKind.HealingResolved when fact.EffectiveValue > 0 => "heal",
        _ => ""
    };

    public static string StatusCue(StatusPresentationCue fact) =>
        fact.Lifecycle == StatusPresentationCueLifecycle.OnActive && fact.Status.GrantedTags.Contains("state.frozen")
            ? "freeze" : "";

    private static string EffectCue(string effect) => effect switch
    {
        "burst" or "firefall" or "meteor_rain" or "ground_spikes" => "explosion",
        "shield" => "", // Effective shield receipts supply the authoritative cue.
        "summon" or "teleport" => "summon",
        "heal" or "healing_field" => "", // Effective restoration supplies the cue.
        "frozen" => "", // First active status supplies the cue; no sound on refresh.
        "taunt_call" or "stun" or "silence" or "root_bind" or "lightning" or
            "arc_lightning" or "thunderstorm" or "poison" or "mana_restore" or
            "empower" or "cleave" or "piercing_thrust" or "shockwave" => "skill",
        _ => ""
    };

    public static void Present(FeedbackAudio audio, IReadOnlyList<BattleEvent> facts,
        Func<string, AttackDelivery> delivery, Func<string, string, bool>? animationOwns = null)
    {
        var specialized = facts.Where(fact => fact.Vfx is not null && EventCue(fact) != "")
            .Select(fact => fact.SourceRuntimeId).ToHashSet(StringComparer.Ordinal);
        foreach (var fact in facts)
        {
            var cue = EventCue(fact, delivery(fact.SourceRuntimeId));
            if (fact.Type == "ability" && !specialized.Contains(fact.SourceRuntimeId)) cue = "skill";
            var action = fact.Type switch
            {
                "attack" or "projectile_spawn" or "beam" => "attack",
                "ability" => "skill_cast",
                "defeated" => "defeated",
                _ when cue == "skill" => "skill_cast",
                _ => ""
            };
            var actionOwner = fact.Type == "defeated" ? fact.TargetRuntimeId : fact.SourceRuntimeId;
            if (action.Length > 0 && animationOwns?.Invoke(actionOwner, action) == true) continue;
            audio.Play(cue);
        }
    }
}
