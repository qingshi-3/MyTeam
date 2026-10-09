using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using TowerAutobattler.Battle;
using TowerAutobattler.Domain;

namespace TowerAutobattler.Presentation;

// Explicit diagnostic export, not a save/replay contract. It contains value data only.
internal static class BattleStatisticsExport
{
    internal static string Serialize(BattleConfig config, string title, int ticks, BattleOutcome outcome,
        string? digest, ImmutableArray<BattleUnitReportSnapshot> reports)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        options.Converters.Add(new JsonStringEnumConverter());
        return JsonSerializer.Serialize(new
        {
            SchemaVersion = 1, ExportedAtUtc = DateTimeOffset.UtcNow, Title = title,
            config.Seed, config.CaptureDetailedStatistics, TickSeconds = BattleTiming.TickSeconds,
            Ticks = ticks, Outcome = outcome, Terminal = outcome != BattleOutcome.Running, Digest = digest,
            Notes = new[]
            {
                "伤害总量=实际扣血+护盾吸收；溢出独立记录。所有时间均为模拟时间。",
                "受到硬控为去重占用时间；来源覆盖和造成目标秒可能重叠，不代表额外延长时间。",
                "其他动作锁定包括自身蓄势。授予护盾不等于护盾实际吸收。召唤物独立统计。",
                "固定配置和种子用于同版本比较；此文件为诊断结果，不是可恢复存档。"
            },
            InitialRoster = config.Spawns.Select(spawn => new
            {
                spawn.InstanceId, spawn.Team, spawn.IsTemporary, spawn.Unit.ContentId, spawn.Unit.DisplayName,
                Cell = new { spawn.Cell.X, spawn.Cell.Y }, spawn.HealthRatio,
                spawn.Unit.MaxHealth, spawn.Unit.Damage, spawn.Unit.Armor, spawn.Unit.Range, spawn.Unit.AttackTicks,
                AbilityIds = spawn.Unit.AbilityLoadout?.Abilities.Select(ability => ability.StableId).ToArray() ?? [],
                TraitIds = spawn.Unit.TraitContributions.IsDefault ? [] : spawn.Unit.TraitContributions.Select(trait => trait.TraitId).ToArray()
            }),
            Units = reports.Select(unit => new
            {
                unit.RuntimeId, unit.SourceInstanceId, unit.ContentId, unit.DisplayName, unit.Team,
                unit.IsHero, unit.IsTemporary, unit.Alive, unit.FinalHealth, unit.MaxHealth, unit.FinalShield,
                unit.DamageDealt, unit.DamageTaken, unit.ShieldAbsorbed, unit.HealingDone, unit.Kills,
                unit.JoinTick, unit.DefeatTick, unit.AttackActions, unit.EffectiveHealingEvents, unit.ActiveTicks,
                unit.DetailedStatistics
            })
        }, options);
    }
}
