using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.BattleLab;
using TowerAutobattler.Composition;
using TowerAutobattler.Content;
using TowerAutobattler.Statuses;

// Isolated validation data only. This harness never loads or writes a player save.
public static class TraitMatrixValidationSupport
{
    public const string ProjectPath = "res://content/validation_matrix/matrix_project.tres";
    public const string PlanPath = "res://design-discussion/04-content-validation/artifacts/trait-matrix/plan.json";
    public const string OutputPath = "res://design-discussion/04-content-validation/artifacts/trait-matrix/validation";
    public sealed record PlanUnit(string Id, string Name, int Tier, string[] Classes, string[] Systems)
    {
        public string ContentId => "hero_" + Id.ToLowerInvariant();
        public string[] Tags => Classes.Concat(Systems).ToArray();
    }
    public sealed record PlanTrait(string Id, string Name, bool System, int[] Thresholds);
    public sealed record Plan(PlanUnit[] Units, PlanTrait[] Traits);
    public sealed record Telemetry(string RuntimeId, string SourceInstanceId, string ContentId, int Team,
        bool Temporary, bool Alive, float FinalHealth, float MaximumHealth, float FinalShield,
        float DamageDealt, float DamageTaken, float HealthDamageTaken, float ShieldAbsorbed,
        float EffectiveHealing, float RequestedHealing, float HealingOverflow, float ShieldGranted,
        int Attacks, int ActiveCasts, int? FirstActiveCastTick, int ManaCasts, int? FirstManaCastTick,
        int HardControlApplications, float GrantedHardControlTicks, int HardControlledUnionTicks,
        int SlowedUnionTicks, int Kills, int JoinTick, int? DefeatTick,
        string SummonerRuntimeId, string RootSummonerRuntimeId, float OwnedSummonDamage,
        object[] AbilityCasts, object[] DamageSources);
    public sealed record Observation(string Outcome, int Ticks, string Digest, Telemetry[] Units,
        object[] Traits, object[] InitialStats, object[] Placements, object[] EventCounts);

    public static Plan ReadPlan()
    {
        using var doc = JsonDocument.Parse(Godot.FileAccess.GetFileAsString(PlanPath));
        var root = doc.RootElement;
        string[] Strings(JsonElement row, string name) => row.GetProperty(name).EnumerateArray().Select(x => x.GetString()!).ToArray();
        var units = root.GetProperty("units").EnumerateArray().Select(x => new PlanUnit(
            x.GetProperty("id").GetString()!, x.GetProperty("name").GetString()!, x.GetProperty("tier").GetInt32(),
            Strings(x, "classes"), Strings(x, "systems"))).ToArray();
        var traits = new[] { ("professions", false), ("systems", true) }.SelectMany(section =>
            root.GetProperty(section.Item1).EnumerateArray().Select(x => new PlanTrait(x.GetProperty("id").GetString()!,
                x.GetProperty("name").GetString()!, section.Item2,
                x.GetProperty("tiers").EnumerateArray().Select(t => t.GetProperty("count").GetInt32()).ToArray()))).ToArray();
        Require(units.Length == 49 && units.Select(x => x.Id).Distinct().Count() == 49, "plan must contain 49 unique units");
        Require(traits.Length == 14 && traits.Sum(x => x.Thresholds.Length) == 39, "plan must contain 14 traits / 39 tiers");
        return new(units, traits);
    }

    public static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    public static BattleConfig Laboratory(BattleLabContentIndex index, IEnumerable<string> allies,
        IEnumerable<string> enemies, ulong seed)
    {
        var a = allies.ToArray(); var b = enemies.ToArray();
        var session = new BattleLabSession(index, Math.Max(1, a.Length), unchecked((long)seed), floorRuleId: "rule_clear");
        foreach (var (ids, side) in new[] { (a, BattleLabSide.Player), (b, BattleLabSide.Enemy) })
        {
            var cells = Enumerable.Range(0, 3).SelectMany(depth => Enumerable.Range(0, 5)
                .Select(y => new Vector2I(side == BattleLabSide.Player ? 2 - depth : 6 + depth, y))).ToArray();
            for (var i = 0; i < ids.Length; i++)
                Require(session.AddAndPlace(ids[i], side, cells[i]).Succeeded, $"fixture placement failed: {ids[i]}/{side}/{i}");
        }
        var config = new BattleLabPreparationAdapter(index).Build(session.Freeze());
        Require(config.Spawns.Count == a.Length + b.Length, "laboratory preparation lost a unit");
        return config;
    }

    public static Observation Observe(CompiledGamePackage package, BattleConfig config, int limit = BattleSimulation.MaxTicks,
        Action<BattleSimulation>? setup = null, Action<BattleSimulation>? afterStep = null)
    {
        var expected = config.Spawns.Select(x => x.InstanceId).ToHashSet(StringComparer.Ordinal);
        Require(expected.Count == config.Spawns.Count, "duplicate prepared identity");
        Require(config.Spawns.Select(x => x.Cell).Distinct().Count() == config.Spawns.Count, "overlapping fixture cells");
        using var battle = new BattleSimulation(config);
        setup?.Invoke(battle);
        var initial = battle.Units.Select(x => (object)new { x.RuntimeId, x.SourceInstanceId, x.Definition.ContentId,
            x.Team, x.MaxHealth, x.Health, x.Shield, x.Damage, x.Armor, x.AttackRange, x.Definition.AttackTicks,
            BaseHealth = x.Definition.MaxHealth, BaseDamage = x.Definition.Damage }).ToArray();
        var hard = new Dictionary<string, int>(); var slow = new Dictionary<string, int>();
        var activeIds = new Dictionary<string, HashSet<string>>();
        void Capture()
        {
            foreach (var u in battle.Units)
            {
                if (!activeIds.TryGetValue(u.RuntimeId, out var ids)) activeIds[u.RuntimeId] = ids = new();
                ids.UnionWith(battle.ReadUnitSkills(u.RuntimeId).Abilities.Where(x => x.IsDisplayedActiveSkill).Select(x => x.StableId));
            }
        }
        Capture();
        while (battle.Outcome == BattleOutcome.Running && battle.TickIndex < limit)
        {
            battle.Step(); Capture(); afterStep?.Invoke(battle);
            foreach (var u in battle.Units.Where(x => x.Alive))
            {
                if (u.DisabledTicks > 0 || u.Statuses.Any(s => s.Behavior == StatusBehaviorKind.DisableActions)) hard[u.RuntimeId] = hard.GetValueOrDefault(u.RuntimeId) + 1;
                if (u.Statuses.Any(s => IsSlow(package, s.StableId))) slow[u.RuntimeId] = slow.GetValueOrDefault(u.RuntimeId) + 1;
            }
        }
        var result = battle.CreateResult(); var events = battle.CombatEvents.ToArray();
        Require(expected.SetEquals(result.Units.Where(x => !x.IsTemporary).Select(x => x.SourceInstanceId)), "report lost initial unit identity");
        var parents = battle.Units.Where(x => x.IsTemporary && x.SummonerRuntimeId.Length > 0).ToDictionary(x => x.RuntimeId, x => x.SummonerRuntimeId);
        string Root(string id) { var seen = new HashSet<string>(); while (parents.TryGetValue(id, out var parent)) { Require(seen.Add(id), "summon ancestry cycle"); id = parent; } return id; }
        var units = result.Units.Select(u =>
        {
            var own = events.Where(e => e.SourceRuntimeId == u.RuntimeId).ToArray();
            var healing = own.Where(e => e.Kind == BattleCombatEventKind.HealingResolved).ToArray();
            var active = own.Where(e => e.Kind == BattleCombatEventKind.AbilityResolved && activeIds[u.RuntimeId].Contains(e.SubjectStableId)).ToArray();
            var mana = own.Where(e => e.Kind == BattleCombatEventKind.ManaSkillResolved).ToArray();
            var controls = own.Where(e => e.Kind == BattleCombatEventKind.ControlApplied).ToArray();
            var damage = own.Where(e => e.Kind == BattleCombatEventKind.DamageResolved).ToArray();
            Require(Math.Abs(damage.Sum(e => e.EffectiveValue) - u.DamageDealt) < .2f, $"damage event/report mismatch {u.RuntimeId}");
            Require(Math.Abs(healing.Sum(e => e.EffectiveValue) - u.HealingDone) < .2f, $"healing event/report mismatch {u.RuntimeId}");
            Require(u.DamageTaken + .01f >= u.ShieldAbsorbed, "negative health damage");
            return new Telemetry(u.RuntimeId, u.SourceInstanceId, u.ContentId, u.Team, u.IsTemporary, u.Alive,
                u.FinalHealth, u.MaxHealth, u.FinalShield, u.DamageDealt, u.DamageTaken, u.DamageTaken - u.ShieldAbsorbed,
                u.ShieldAbsorbed, u.HealingDone, healing.Sum(e => e.RequestedValue), healing.Sum(e => Math.Max(0, e.AppliedValue - e.EffectiveValue)),
                own.Where(e => e.Kind == BattleCombatEventKind.ShieldResolved).Sum(e => e.EffectiveValue), u.AttackActions,
                active.Length, active.Length == 0 ? null : active.Min(e => e.Tick), mana.Length, mana.Length == 0 ? null : mana.Min(e => e.Tick),
                controls.Length, controls.Sum(e => e.EffectiveValue), hard.GetValueOrDefault(u.RuntimeId), slow.GetValueOrDefault(u.RuntimeId),
                u.Kills, u.JoinTick, u.DefeatTick, parents.GetValueOrDefault(u.RuntimeId, ""), u.IsTemporary ? Root(u.RuntimeId) : "",
                result.Units.Where(x => x.IsTemporary && Root(x.RuntimeId) == u.RuntimeId).Sum(x => x.DamageDealt),
                own.Where(e => e.Kind == BattleCombatEventKind.AbilityResolved).GroupBy(e => e.SubjectStableId)
                    .Select(g => (object)new { AbilityId = g.Key, Count = g.Count(), FirstTick = g.Min(e => e.Tick), Active = activeIds[u.RuntimeId].Contains(g.Key) }).ToArray(),
                damage.GroupBy(e => (e.Source.Kind, e.Source.StableId)).Select(g => (object)new { Kind = g.Key.Kind.ToString(), g.Key.StableId, Damage = g.Sum(e => e.EffectiveValue), Hits = g.Count() }).ToArray());
        }).ToArray();
        return new(result.Outcome.ToString(), result.Ticks, result.Digest, units,
            battle.TraitSnapshot.Values.Where(x => x.Team == 0).Select(x => (object)new { x.TraitId, x.Value, ActiveMin = x.ActiveBreakpoint?.MinValue }).ToArray(), initial,
            config.Spawns.Select(x => (object)new { x.InstanceId, x.Unit.ContentId, x.Team, X = x.Cell.X, Y = x.Cell.Y }).ToArray(),
            events.GroupBy(e => (e.Kind, e.SubjectStableId)).Select(g => (object)new { Kind = g.Key.Kind.ToString(), g.Key.SubjectStableId, Count = g.Count() }).ToArray());
    }

    private static bool IsSlow(CompiledGamePackage p, string id) => id == "matrix_chill" || id.Length > 0 && p.Content.Graph.TryGetStatus(id, out var s) && s.Disposition == StatusDisposition.Harmful && s.AttributeModifiers.Any(m => m.Attribute is TowerAutobattler.Attributes.CombatAttribute.MoveSpeed or TowerAutobattler.Attributes.CombatAttribute.AttackSpeed);

    public static void Write(string name, object value)
    {
        var folder = ProjectSettings.GlobalizePath(OutputPath); Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, name), JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true }));
    }
    public static string Fingerprint()
    {
        var root = ProjectSettings.GlobalizePath("res://");
        var paths = new[] { "src", "content", "tests" }.SelectMany(folder => Directory.EnumerateFiles(Path.Combine(root, folder), "*", SearchOption.AllDirectories))
            .Where(p => p.EndsWith(".cs") || p.EndsWith(".tres") || p.EndsWith(".tscn")).Append(ProjectSettings.GlobalizePath(PlanPath)).Order(StringComparer.Ordinal);
        var manifest = string.Join("\n", paths.Select(p => Path.GetRelativePath(root, p).Replace('\\', '/') + ":" + Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p)))));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(manifest)));
    }
}
