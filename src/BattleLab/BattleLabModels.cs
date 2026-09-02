using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Godot;
using TowerAutobattler.Battle;

namespace TowerAutobattler.BattleLab;

public enum BattleLabSide { Player, Enemy }
public enum BattleLabPlacementMode { Formal, FreeExperiment }

public sealed record BattleLabEquipmentConfiguration(
    string InstanceId,
    string ContentId,
    int SlotIndex);

public sealed record BattleLabUnitConfiguration(
    string InstanceId,
    string ContentId,
    BattleLabSide Side,
    Vector2I Cell,
    ImmutableArray<BattleLabEquipmentConfiguration> Equipment);

public sealed record BattleLabRelicConfiguration(
    string InstanceId,
    string ContentId,
    int Stacks);

public sealed record BattleLabStartSnapshot(
    int SchemaVersion,
    BattleLabPlacementMode Mode,
    int CurrentPopulation,
    long Seed,
    string FloorRuleId,
    string PrimaryHeroInstanceId,
    ImmutableArray<BattleLabUnitConfiguration> Units,
    ImmutableArray<BattleLabRelicConfiguration> Relics,
    string CanonicalDigest);

public sealed record BattleLabPlacementResult(
    bool Succeeded,
    string RejectionReason,
    string InstanceId,
    string? SwappedInstanceId = null)
{
    public static BattleLabPlacementResult Reject(string instanceId, string reason) =>
        new(false, reason, instanceId);
}

public sealed class BattleLabSession
{
    public const int SchemaVersion = 1;
    private readonly Dictionary<string, BattleLabUnitConfiguration> _units = new(StringComparer.Ordinal);
    private readonly Dictionary<string, BattleLabRelicConfiguration> _relics = new(StringComparer.Ordinal);
    private int _nextInstanceSequence;

    public BattleLabSession(
        BattleLabContentIndex content,
        int currentPopulation,
        long seed = 20260901,
        BattleLabPlacementMode mode = BattleLabPlacementMode.Formal,
        string floorRuleId = "")
    {
        Content = content ?? throw new ArgumentNullException(nameof(content));
        ValidateModeAndPopulation(mode, currentPopulation);
        CurrentPopulation = currentPopulation;
        Seed = seed;
        Mode = mode;
        FloorRuleId = Content.ResolveFloorRuleId(floorRuleId);
        PrimaryHeroInstanceId = string.Empty;
    }

    public BattleLabContentIndex Content { get; }
    public int CurrentPopulation { get; private set; }
    public long Seed { get; private set; }
    public BattleLabPlacementMode Mode { get; private set; }
    public string FloorRuleId { get; private set; }
    public string PrimaryHeroInstanceId { get; private set; }
    public IReadOnlyCollection<BattleLabUnitConfiguration> Units => _units.Values;
    public IReadOnlyCollection<BattleLabRelicConfiguration> Relics => _relics.Values;

    public void SetRules(BattleLabPlacementMode mode, int currentPopulation, long seed, string floorRuleId)
    {
        ValidateModeAndPopulation(mode, currentPopulation);
        var resolvedFloorRuleId = Content.ResolveFloorRuleId(floorRuleId);
        ValidateUnitSet(_units.Values, mode, currentPopulation, resolvedFloorRuleId);
        Mode = mode;
        CurrentPopulation = currentPopulation;
        Seed = seed;
        FloorRuleId = resolvedFloorRuleId;
    }

    public BattleLabPlacementResult AddAndPlace(
        string contentId,
        BattleLabSide side,
        Vector2I cell,
        Func<Vector2I, bool>? canOccupy = null)
    {
        var nextSequence = checked(_nextInstanceSequence + 1);
        var id = $"lab-unit-{nextSequence}";
        var candidate = new BattleLabUnitConfiguration(id, contentId, side, cell, []);
        var result = BattleLabPlacementPolicy.Evaluate(this, candidate, cell, false, canOccupy);
        if (!result.Succeeded) return result;
        _nextInstanceSequence = nextSequence;
        ApplyPlacement(candidate, result);
        if (side == BattleLabSide.Player && string.IsNullOrWhiteSpace(PrimaryHeroInstanceId))
            PrimaryHeroInstanceId = candidate.InstanceId;
        return result;
    }

    public BattleLabPlacementResult Move(
        string instanceId,
        Vector2I cell,
        bool allowSwap = true,
        Func<Vector2I, bool>? canOccupy = null)
    {
        if (!_units.TryGetValue(instanceId, out var unit))
            return BattleLabPlacementResult.Reject(instanceId, "未找到要移动的单位实例。");
        var result = BattleLabPlacementPolicy.Evaluate(this, unit, cell, allowSwap, canOccupy);
        if (!result.Succeeded) return result;
        ApplyPlacement(unit with { Cell = cell }, result);
        return result;
    }

    public bool Recall(string instanceId)
    {
        if (!_units.Remove(instanceId)) return false;
        if (string.Equals(PrimaryHeroInstanceId, instanceId, StringComparison.Ordinal))
            PrimaryHeroInstanceId = _units.Values.Where(unit => unit.Side == BattleLabSide.Player)
                .OrderBy(unit => unit.InstanceId, StringComparer.Ordinal)
                .Select(unit => unit.InstanceId).FirstOrDefault() ?? string.Empty;
        return true;
    }

    public void Clear(BattleLabSide? side = null)
    {
        if (side is null)
        {
            _units.Clear();
            PrimaryHeroInstanceId = string.Empty;
        }
        else
            foreach (var id in _units.Values.Where(unit => unit.Side == side).Select(unit => unit.InstanceId).ToArray())
                _units.Remove(id);
        if (side == BattleLabSide.Player) PrimaryHeroInstanceId = string.Empty;
    }

    public bool SetPrimaryHero(string instanceId)
    {
        if (!_units.TryGetValue(instanceId, out var unit) || unit.Side != BattleLabSide.Player) return false;
        PrimaryHeroInstanceId = instanceId;
        return true;
    }

    public bool Equip(string ownerInstanceId, int slotIndex, string contentId)
    {
        if (!_units.TryGetValue(ownerInstanceId, out var owner) || owner.Side != BattleLabSide.Player ||
            slotIndex < 0 || slotIndex >= Content.Rules.EquipmentSlotCapacity ||
            !Content.Equipment.Any(entry => entry.StableId == contentId))
            return false;
        var equipment = owner.Equipment.Where(item => item.SlotIndex != slotIndex).ToList();
        equipment.Add(new BattleLabEquipmentConfiguration(NextId("equipment"), contentId, slotIndex));
        _units[ownerInstanceId] = owner with
        {
            Equipment = equipment.OrderBy(item => item.SlotIndex).ToImmutableArray()
        };
        return true;
    }

    public bool RemoveEquipment(string ownerInstanceId, int slotIndex)
    {
        if (!_units.TryGetValue(ownerInstanceId, out var owner)) return false;
        var equipment = owner.Equipment.Where(item => item.SlotIndex != slotIndex).ToImmutableArray();
        if (equipment.Length == owner.Equipment.Length) return false;
        _units[ownerInstanceId] = owner with { Equipment = equipment };
        return true;
    }

    public bool SetRelic(string contentId, int stacks)
    {
        if (stacks <= 0 || !Content.Relics.Any(entry => entry.StableId == contentId)) return false;
        var current = _relics.Values.FirstOrDefault(item => item.ContentId == contentId);
        if (current is null)
        {
            var created = new BattleLabRelicConfiguration(NextId("relic"), contentId, stacks);
            _relics.Add(created.InstanceId, created);
        }
        else _relics[current.InstanceId] = current with { Stacks = stacks };
        return true;
    }

    public bool RemoveRelic(string instanceId) => _relics.Remove(instanceId);

    public BattleLabStartSnapshot Freeze()
    {
        var units = _units.Values.OrderBy(unit => unit.InstanceId, StringComparer.Ordinal)
            .Select(unit => unit with { Equipment = unit.Equipment.ToImmutableArray() })
            .ToImmutableArray();
        var relics = _relics.Values.OrderBy(relic => relic.InstanceId, StringComparer.Ordinal).ToImmutableArray();
        return new BattleLabStartSnapshot(
            SchemaVersion,
            Mode,
            CurrentPopulation,
            Seed,
            FloorRuleId,
            PrimaryHeroInstanceId,
            units,
            relics,
            CanonicalDigest(Mode, CurrentPopulation, Seed, FloorRuleId, PrimaryHeroInstanceId, units, relics));
    }

    public void Restore(BattleLabStartSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.SchemaVersion != SchemaVersion) throw new InvalidOperationException("不支持的战斗实验室配置版本。");
        var expected = CanonicalDigest(snapshot.Mode, snapshot.CurrentPopulation, snapshot.Seed,
            snapshot.FloorRuleId, snapshot.PrimaryHeroInstanceId, snapshot.Units, snapshot.Relics);
        if (!string.Equals(expected, snapshot.CanonicalDigest, StringComparison.Ordinal))
            throw new InvalidOperationException("战斗实验室配置摘要不匹配。");
        ValidateModeAndPopulation(snapshot.Mode, snapshot.CurrentPopulation);
        var floorRuleId = Content.ResolveFloorRuleId(snapshot.FloorRuleId);
        if (!string.Equals(floorRuleId, snapshot.FloorRuleId, StringComparison.Ordinal))
            throw new InvalidOperationException("战斗实验室快照必须显式记录地形规则。");
        var ids = ValidateUnitSet(snapshot.Units, snapshot.Mode, snapshot.CurrentPopulation, floorRuleId);
        var playerCount = snapshot.Units.Count(unit => unit.Side == BattleLabSide.Player);
        if ((playerCount == 0 && !string.IsNullOrWhiteSpace(snapshot.PrimaryHeroInstanceId)) ||
            (playerCount > 0 && (string.IsNullOrWhiteSpace(snapshot.PrimaryHeroInstanceId) ||
                !snapshot.Units.Any(unit => unit.InstanceId == snapshot.PrimaryHeroInstanceId &&
                                            unit.Side == BattleLabSide.Player))))
            throw new InvalidOperationException("战斗实验室主英雄实例无效。");
        foreach (var relic in snapshot.Relics)
            if (relic is null || string.IsNullOrWhiteSpace(relic.InstanceId) ||
                string.IsNullOrWhiteSpace(relic.ContentId) || !ids.Add(relic.InstanceId) ||
                relic.Stacks <= 0 ||
                !Content.Relics.Any(item => item.StableId == relic.ContentId))
                throw new InvalidOperationException("战斗实验室遗物配置无效。");
        _units.Clear();
        _relics.Clear();
        foreach (var unit in snapshot.Units) _units.Add(unit.InstanceId, unit);
        foreach (var relic in snapshot.Relics) _relics.Add(relic.InstanceId, relic);
        Mode = snapshot.Mode;
        CurrentPopulation = snapshot.CurrentPopulation;
        Seed = snapshot.Seed;
        FloorRuleId = floorRuleId;
        PrimaryHeroInstanceId = snapshot.PrimaryHeroInstanceId;
        _nextInstanceSequence = ids.Select(ParseSequence).DefaultIfEmpty(0).Max();
    }

    public bool TryGet(string instanceId, out BattleLabUnitConfiguration unit) =>
        _units.TryGetValue(instanceId, out unit!);

    public BattleLabUnitConfiguration? At(Vector2I cell) =>
        _units.Values.FirstOrDefault(unit => unit.Cell == cell);

    private void ApplyPlacement(BattleLabUnitConfiguration candidate, BattleLabPlacementResult result)
    {
        if (result.SwappedInstanceId is { } swappedId && _units.TryGetValue(swappedId, out var swapped) &&
            _units.TryGetValue(candidate.InstanceId, out var original))
            _units[swappedId] = swapped with { Cell = original.Cell };
        _units[candidate.InstanceId] = candidate;
        if (_units.Values.Select(unit => unit.Cell).Distinct().Count() != _units.Count)
            throw new InvalidOperationException("战斗实验室放置事务破坏了单格唯一约束。");
    }

    private void ValidateModeAndPopulation(BattleLabPlacementMode mode, int currentPopulation)
    {
        if (!Enum.IsDefined(mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        var cap = mode == BattleLabPlacementMode.Formal
            ? Content.Rules.PhysicalDeploymentCeiling
            : BattlefieldLayout.Width * BattlefieldLayout.Height;
        if (currentPopulation <= 0 || currentPopulation > cap)
            throw new ArgumentOutOfRangeException(nameof(currentPopulation));
    }

    private HashSet<string> ValidateUnitSet(
        IEnumerable<BattleLabUnitConfiguration> units,
        BattleLabPlacementMode mode,
        int currentPopulation,
        string floorRuleId)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var cells = new HashSet<Vector2I>();
        var playerCount = 0;
        foreach (var unit in units)
        {
            if (unit is null || string.IsNullOrWhiteSpace(unit.InstanceId) ||
                string.IsNullOrWhiteSpace(unit.ContentId) || !Enum.IsDefined(unit.Side) ||
                !ids.Add(unit.InstanceId) || !cells.Add(unit.Cell) ||
                !BattlefieldLayout.IsInBounds(unit.Cell) || !Content.CanOccupy(floorRuleId, unit.Cell) ||
                !Content.TryGetUnit(unit.ContentId, out var published) ||
                !published.AllowedSides.Contains(unit.Side))
                throw new InvalidOperationException("战斗实验室单位配置无效、重复或位于禁格。");
            if (mode == BattleLabPlacementMode.Formal &&
                ((unit.Side == BattleLabSide.Player && !BattlefieldLayout.IsPlayerDeploymentCell(unit.Cell)) ||
                 (unit.Side == BattleLabSide.Enemy &&
                  unit.Cell.X < BattlefieldLayout.Width - BattlefieldLayout.PlayerDeploymentColumns)))
                throw new InvalidOperationException("战斗实验室单位不符合正式阵营区域。");
            if (unit.Side == BattleLabSide.Player) playerCount++;
            if (unit.Side == BattleLabSide.Enemy && unit.Equipment.Length > 0)
                throw new InvalidOperationException("敌方单位不能配置玩家装备。");
            if (unit.Equipment.Length > Content.Rules.EquipmentSlotCapacity)
                throw new InvalidOperationException("英雄装备槽数量无效。");
            var slots = new HashSet<int>();
            foreach (var equipment in unit.Equipment)
                if (equipment is null || string.IsNullOrWhiteSpace(equipment.InstanceId) ||
                    string.IsNullOrWhiteSpace(equipment.ContentId) || !ids.Add(equipment.InstanceId) ||
                    equipment.SlotIndex < 0 || equipment.SlotIndex >= Content.Rules.EquipmentSlotCapacity ||
                    !slots.Add(equipment.SlotIndex) ||
                    !Content.Equipment.Any(item => item.StableId == equipment.ContentId))
                    throw new InvalidOperationException("英雄装备实例或槽位无效。");
        }
        if (mode == BattleLabPlacementMode.Formal && playerCount > currentPopulation)
            throw new InvalidOperationException("我方部署数量超过正式人口。");
        return ids;
    }

    private string NextId(string kind) => $"lab-{kind}-{++_nextInstanceSequence}";

    private static int ParseSequence(string id)
    {
        var index = id.LastIndexOf('-');
        return index >= 0 && int.TryParse(id[(index + 1)..], out var value) ? value : 0;
    }

    public static string CanonicalDigest(
        BattleLabPlacementMode mode,
        int population,
        long seed,
        string floorRuleId,
        string primaryHeroInstanceId,
        IEnumerable<BattleLabUnitConfiguration> units,
        IEnumerable<BattleLabRelicConfiguration> relics)
    {
        var canonical = string.Join("|",
            ((int)mode).ToString(CultureInfo.InvariantCulture),
            population.ToString(CultureInfo.InvariantCulture),
            seed.ToString(CultureInfo.InvariantCulture),
            floorRuleId ?? string.Empty,
            primaryHeroInstanceId ?? string.Empty,
            string.Join(";", units.OrderBy(unit => unit.InstanceId, StringComparer.Ordinal).Select(unit =>
                $"{unit.InstanceId},{unit.ContentId},{(int)unit.Side},{unit.Cell.X},{unit.Cell.Y}," +
                string.Join(',', unit.Equipment.OrderBy(item => item.SlotIndex)
                    .Select(item => $"{item.InstanceId}:{item.ContentId}:{item.SlotIndex}")))),
            string.Join(";", relics.OrderBy(relic => relic.InstanceId, StringComparer.Ordinal).Select(relic =>
                $"{relic.InstanceId},{relic.ContentId},{relic.Stacks}")));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }
}
