using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
using Godot;
using TowerAutobattler.Battle;

namespace TowerAutobattler.BattleLab;

public sealed class BattleLabPresetDto
{
    public int SchemaVersion { get; set; } = BattleLabSession.SchemaVersion;
    public BattleLabPlacementMode Mode { get; set; }
    public int CurrentPopulation { get; set; }
    public long Seed { get; set; }
    public string FloorRuleId { get; set; } = string.Empty;
    public string PrimaryHeroInstanceId { get; set; } = string.Empty;
    public List<BattleLabPresetUnitDto> Units { get; set; } = [];
    public List<BattleLabPresetRelicDto> Relics { get; set; } = [];
}

public sealed class BattleLabPresetUnitDto
{
    public string InstanceId { get; set; } = string.Empty;
    public string ContentId { get; set; } = string.Empty;
    public BattleLabSide Side { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public List<BattleLabPresetEquipmentDto> Equipment { get; set; } = [];
}

public sealed class BattleLabPresetEquipmentDto
{
    public string InstanceId { get; set; } = string.Empty;
    public string ContentId { get; set; } = string.Empty;
    public int SlotIndex { get; set; }
}

public sealed class BattleLabPresetRelicDto
{
    public string InstanceId { get; set; } = string.Empty;
    public string ContentId { get; set; } = string.Empty;
    public int Stacks { get; set; }
}

public sealed class BattleLabPresetStore
{
    public const string UserNamespace = "user://battle_lab/";
    private readonly JsonSerializerOptions _options = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
    private readonly IReadOnlyDictionary<string, BattleLabPresetDto> _builtIns;

    public BattleLabPresetStore(IReadOnlyDictionary<string, BattleLabPresetDto>? builtIns = null)
    {
        _builtIns = builtIns ?? new Dictionary<string, BattleLabPresetDto>(StringComparer.Ordinal);
        DefaultPresetName = _builtIns.Keys.OrderBy(name => name, StringComparer.Ordinal).FirstOrDefault() ?? string.Empty;
    }

    public BattleLabPresetStore(BattleLabPresetCatalog? catalog)
    {
        var builtIns = new Dictionary<string, BattleLabPresetDto>(StringComparer.Ordinal);
        foreach (var definition in catalog?.Presets ?? [])
        {
            if (definition is null || string.IsNullOrWhiteSpace(definition.DisplayName) ||
                string.IsNullOrWhiteSpace(definition.PresetJson)) continue;
            var preset = JsonSerializer.Deserialize<BattleLabPresetDto>(definition.PresetJson, _options);
            if (!ValidateShape(preset))
                throw new InvalidOperationException($"内置战斗实验室预设无效：{definition.DisplayName}");
            builtIns.Add(definition.DisplayName, preset!);
        }
        _builtIns = builtIns;
        DefaultPresetName = catalog?.DefaultPresetName ?? string.Empty;
        if (string.IsNullOrWhiteSpace(DefaultPresetName) || !_builtIns.ContainsKey(DefaultPresetName))
            throw new InvalidOperationException("战斗实验室默认预设未配置或不存在。");
    }

    public IReadOnlyDictionary<string, BattleLabPresetDto> BuiltIns => _builtIns;
    public string DefaultPresetName { get; }

    public IReadOnlyList<string> ListNames()
    {
        var names = new HashSet<string>(_builtIns.Keys, StringComparer.Ordinal);
        try
        {
            var directory = ProjectSettings.GlobalizePath(UserNamespace);
            if (Directory.Exists(directory))
                foreach (var path in Directory.EnumerateFiles(directory, "*.json", SearchOption.TopDirectoryOnly))
                {
                    var name = Path.GetFileNameWithoutExtension(path);
                    if (TrySafeName(name, out _)) names.Add(name);
                }
        }
        catch (Exception exception) { GD.PushWarning("战斗实验室预设列表读取失败：" + exception.Message); }
        return names.OrderBy(name => name, StringComparer.Ordinal).ToArray();
    }

    public bool Save(string name, BattleLabStartSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (!TrySafeName(name, out var safe)) return false;
        try
        {
            var directory = ProjectSettings.GlobalizePath(UserNamespace);
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, safe + ".json");
            var temporary = path + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(ToDto(snapshot), _options));
            File.Move(temporary, path, true);
            return true;
        }
        catch (Exception exception)
        {
            GD.PushWarning("战斗实验室预设保存失败：" + exception.Message);
            return false;
        }
    }

    public bool TryLoad(string name, out BattleLabPresetDto preset)
    {
        preset = null!;
        if (_builtIns.TryGetValue(name, out var builtIn))
        {
            preset = Clone(builtIn);
            return ValidateShape(preset);
        }
        if (!TrySafeName(name, out var safe)) return false;
        try
        {
            var path = Path.Combine(ProjectSettings.GlobalizePath(UserNamespace), safe + ".json");
            if (!File.Exists(path)) return false;
            preset = JsonSerializer.Deserialize<BattleLabPresetDto>(File.ReadAllText(path), _options)!;
            return ValidateShape(preset);
        }
        catch (Exception exception)
        {
            GD.PushWarning("战斗实验室预设读取失败：" + exception.Message);
            preset = null!;
            return false;
        }
    }

    public static BattleLabPresetDto ToDto(BattleLabStartSnapshot snapshot) => new()
    {
        SchemaVersion = snapshot.SchemaVersion,
        Mode = snapshot.Mode,
        CurrentPopulation = snapshot.CurrentPopulation,
        Seed = snapshot.Seed,
        FloorRuleId = snapshot.FloorRuleId,
        PrimaryHeroInstanceId = snapshot.PrimaryHeroInstanceId,
        Units = snapshot.Units.Select(unit => new BattleLabPresetUnitDto
        {
            InstanceId = unit.InstanceId,
            ContentId = unit.ContentId,
            Side = unit.Side,
            X = unit.Cell.X,
            Y = unit.Cell.Y,
            Equipment = unit.Equipment.Select(item => new BattleLabPresetEquipmentDto
            {
                InstanceId = item.InstanceId,
                ContentId = item.ContentId,
                SlotIndex = item.SlotIndex
            }).ToList()
        }).ToList(),
        Relics = snapshot.Relics.Select(relic => new BattleLabPresetRelicDto
        {
            InstanceId = relic.InstanceId,
            ContentId = relic.ContentId,
            Stacks = relic.Stacks
        }).ToList()
    };

    public static BattleLabStartSnapshot ToSnapshot(BattleLabPresetDto preset)
    {
        if (!ValidateShape(preset)) throw new InvalidOperationException("战斗实验室预设结构无效。");
        var units = preset.Units.Select(unit => new BattleLabUnitConfiguration(
            unit.InstanceId,
            unit.ContentId,
            unit.Side,
            new Vector2I(unit.X, unit.Y),
            unit.Equipment.Select(item => new BattleLabEquipmentConfiguration(
                item.InstanceId, item.ContentId, item.SlotIndex)).ToImmutableArray())).ToImmutableArray();
        var relics = preset.Relics.Select(relic => new BattleLabRelicConfiguration(
            relic.InstanceId, relic.ContentId, relic.Stacks)).ToImmutableArray();
        return new BattleLabStartSnapshot(
            preset.SchemaVersion,
            preset.Mode,
            preset.CurrentPopulation,
            preset.Seed,
            preset.FloorRuleId,
            preset.PrimaryHeroInstanceId,
            units,
            relics,
            BattleLabSession.CanonicalDigest(preset.Mode, preset.CurrentPopulation, preset.Seed,
                preset.FloorRuleId, preset.PrimaryHeroInstanceId, units, relics));
    }

    public static bool ValidateShape(BattleLabPresetDto? preset)
    {
        if (preset is null || preset.SchemaVersion != BattleLabSession.SchemaVersion ||
            !Enum.IsDefined(preset.Mode) || preset.CurrentPopulation <= 0 ||
            (preset.Mode == BattleLabPlacementMode.Formal &&
             preset.CurrentPopulation > BattlefieldLayout.PlayerDeploymentCells.Length) ||
            (preset.Mode == BattleLabPlacementMode.FreeExperiment &&
             preset.CurrentPopulation > BattlefieldLayout.Width * BattlefieldLayout.Height) ||
            string.IsNullOrWhiteSpace(preset.FloorRuleId) ||
            preset.Units is null || preset.Relics is null)
            return false;
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var cells = new HashSet<(int X, int Y)>();
        foreach (var unit in preset.Units)
        {
            if (unit is null || string.IsNullOrWhiteSpace(unit.InstanceId) ||
                string.IsNullOrWhiteSpace(unit.ContentId) || !Enum.IsDefined(unit.Side) ||
                !ids.Add(unit.InstanceId) || !BattlefieldLayout.IsInBounds(new Vector2I(unit.X, unit.Y)) ||
                !cells.Add((unit.X, unit.Y)) || unit.Equipment is null ||
                (preset.Mode == BattleLabPlacementMode.Formal &&
                 ((unit.Side == BattleLabSide.Player &&
                   !BattlefieldLayout.IsPlayerDeploymentCell(new Vector2I(unit.X, unit.Y))) ||
                  (unit.Side == BattleLabSide.Enemy &&
                   unit.X < BattlefieldLayout.Width - BattlefieldLayout.PlayerDeploymentColumns))))
                return false;
            var slots = new HashSet<int>();
            foreach (var item in unit.Equipment)
                if (item is null || string.IsNullOrWhiteSpace(item.InstanceId) ||
                    string.IsNullOrWhiteSpace(item.ContentId) || !ids.Add(item.InstanceId) ||
                    item.SlotIndex < 0 || !slots.Add(item.SlotIndex))
                    return false;
        }
        var playerCount = preset.Units.Count(unit => unit.Side == BattleLabSide.Player);
        if ((playerCount == 0 && !string.IsNullOrWhiteSpace(preset.PrimaryHeroInstanceId)) ||
            (playerCount > 0 && (string.IsNullOrWhiteSpace(preset.PrimaryHeroInstanceId) ||
                !preset.Units.Any(unit => unit.InstanceId == preset.PrimaryHeroInstanceId &&
                                          unit.Side == BattleLabSide.Player))) ||
            (preset.Mode == BattleLabPlacementMode.Formal &&
             playerCount > preset.CurrentPopulation))
            return false;
        foreach (var relic in preset.Relics)
            if (relic is null || string.IsNullOrWhiteSpace(relic.InstanceId) ||
                string.IsNullOrWhiteSpace(relic.ContentId) || relic.Stacks <= 0 ||
                !ids.Add(relic.InstanceId))
                return false;
        return true;
    }

    private static bool TrySafeName(string name, out string safe)
    {
        safe = (name ?? string.Empty).Trim();
        return safe.Length is > 0 and <= 64 && safe.All(character =>
            char.IsLetterOrDigit(character) || character is '-' or '_' || character > 127);
    }

    private BattleLabPresetDto Clone(BattleLabPresetDto value) =>
        JsonSerializer.Deserialize<BattleLabPresetDto>(JsonSerializer.Serialize(value, _options), _options)!;
}
