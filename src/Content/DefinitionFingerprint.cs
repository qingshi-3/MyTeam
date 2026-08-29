using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Godot;

namespace TowerAutobattler.Content;

public static class DefinitionFingerprint
{
    public static string Compute(Resource definition)
    {
        var text = definition switch
        {
            UnitDefinition unit => string.Join("|",
                unit.Id, unit.DisplayName, unit.Description, unit.Role, unit.Faction, unit.IsHero, unit.IsEnemy,
                unit.RecruitCost, F(unit.MaxHealth), F(unit.AttackDamage), F(unit.AttackRange), F(unit.AttackCooldown),
                F(unit.MoveInterval), F(unit.Armor), F(unit.HealPower), F(unit.SplashRadius), F(unit.LifeSteal),
                unit.Portrait?.ResourcePath ?? string.Empty, unit.Portrait?.StableId ?? string.Empty,
                unit.Portrait?.Frames?.ResourcePath ?? string.Empty,
                unit.Portrait?.AnimationName.ToString() ?? string.Empty, unit.Portrait?.FrameIndex ?? -1,
                unit.Portrait is null ? string.Empty : F(unit.Portrait.Zoom),
                unit.Portrait?.OffsetRatio.ToString() ?? string.Empty, unit.Portrait?.FlipHorizontal ?? false,
                string.Join(",", unit.Tags.Select(tag => tag.ToString()).OrderBy(tag => tag, StringComparer.Ordinal))),
            ItemDefinition item => string.Join("|", item.Id, item.DisplayName, item.Description, item.Rarity, item.Price,
                string.Join(",", item.Tags.Select(tag => tag.ToString()).OrderBy(tag => tag, StringComparer.Ordinal))),
            _ => throw new ArgumentException($"Unsupported definition type: {definition.GetType().Name}", nameof(definition))
        };
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
    }

    private static string F(float value) => value.ToString("R", CultureInfo.InvariantCulture);
}
