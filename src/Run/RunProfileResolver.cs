using System;
using System.Collections.Generic;
using System.Text.Json;
using TowerAutobattler.Composition;
using TowerAutobattler.Project;

namespace TowerAutobattler.Run;

// This is a routing envelope, not a validated ActiveRunDto. Reading/resolving it
// neither migrates a save nor grants permission to publish it or initialize Meta.
public sealed record RunProfileHeader(
    int Version,
    string ProfileId,
    string ContentFingerprint,
    int ProfileCompatibilityVersion = 0,
    int ProgressionProtocolVersion = 0,
    int RewardProtocolVersion = 0,
    int IdentityProtocolVersion = 0);

public static class RunProfileResolver
{
    public const int FirstProfileSchemaVersion = 11;

    public static bool TryReadHeader(string json, out RunProfileHeader? header, out string error)
    {
        header = null;
        error = string.Empty;
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                throw new FormatException("存档必须是一个对象。");
            var fields = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in document.RootElement.EnumerateObject())
                if (IsEnvelopeField(property.Name) && !fields.TryAdd(property.Name, property.Value))
                    throw new FormatException($"存档头字段重复：{property.Name}。");
            if (!fields.ContainsKey("Version")) throw new FormatException("存档缺少版本字段。");
            header = new RunProfileHeader(ReadInt(fields, "Version"),
                ReadString(fields, "ProfileId"), ReadString(fields, "ContentFingerprint"),
                ReadInt(fields, "ProfileCompatibilityVersion"), ReadInt(fields, "ProgressionProtocolVersion"),
                ReadInt(fields, "RewardProtocolVersion"), ReadInt(fields, "IdentityProtocolVersion"));
            return true;
        }
        catch (Exception exception) when (exception is JsonException or FormatException or ArgumentException)
        {
            error = $"无法读取存档配置头，原文件应保留：{exception.Message}";
            return false;
        }
    }

    public static bool TryResolve(CompiledGamePackage package, string saveNamespace, RunProfileHeader header,
        out CompiledRunProfile? profile, out string error,
        IReadOnlyDictionary<string, string>? legacyNamespaceMappings = null)
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentNullException.ThrowIfNull(header);
        profile = null;
        error = string.Empty;
        if (header.Version < 2 || header.Version > FirstProfileSchemaVersion)
            return Reject($"不支持存档版本 {header.Version}，原文件应保留。", out error);

        if (header.Version < FirstProfileSchemaVersion)
        {
            // Only the original schemas without new envelope fields use namespace
            // routing. Their existing migration/validation boundary still decides
            // whether the body is supported (a version number alone never does).
            if (!string.IsNullOrEmpty(header.ProfileId) || !string.IsNullOrEmpty(header.ContentFingerprint) ||
                header.ProfileCompatibilityVersion != 0 || header.ProgressionProtocolVersion != 0 ||
                header.RewardProtocolVersion != 0 || header.IdentityProtocolVersion != 0)
                return Reject("旧版存档包含不匹配的配置头，原文件应保留。", out error);
            var space = string.IsNullOrWhiteSpace(saveNamespace) ? string.Empty : saveNamespace.Trim('/');
            string? legacyId = null;
            if (legacyNamespaceMappings is not null && legacyNamespaceMappings.TryGetValue(space, out var explicitId))
                legacyId = explicitId;
            else if (space == "growth-journey") legacyId = RunProfileIds.GrowthTowerLegacy;
            else if (space.Length == 0) legacyId = RunProfileIds.AlphaTowerLegacy;
            if (string.IsNullOrEmpty(legacyId))
                return Reject("此存档命名空间没有明确的旧版配置映射，原文件应保留。", out error);
            if (!package.ProfilesById.TryGetValue(legacyId, out var legacy))
                return Reject($"旧版配置 {legacyId} 未发布，原文件应保留。", out error);
            if (legacy.Project.Campaign.Progression is not CompiledTowerProgression)
                return Reject("旧版存档只能交给完整的旧爬塔配置迁移，原文件应保留。", out error);
            profile = legacy;
            return true;
        }

        if (string.IsNullOrWhiteSpace(header.ProfileId) || string.IsNullOrWhiteSpace(header.ContentFingerprint))
            return Reject("存档缺少配置身份或内容指纹，原文件应保留。", out error);
        if (!package.ProfilesById.TryGetValue(header.ProfileId, out var selected))
            return Reject($"存档配置 {header.ProfileId} 未发布，原文件应保留。", out error);
        if (!StringComparer.Ordinal.Equals(header.ContentFingerprint, selected.ContentFingerprint))
            return Reject("存档内容指纹与已发布配置不一致，原文件应保留。", out error);
        if (header.ProfileCompatibilityVersion != selected.CompatibilityVersion ||
            header.ProgressionProtocolVersion != selected.ProgressionProtocolVersion ||
            header.RewardProtocolVersion != selected.RewardProtocolVersion ||
            header.IdentityProtocolVersion != selected.IdentityProtocolVersion)
            return Reject("存档配置或处理协议版本不匹配，原文件应保留。", out error);
        profile = selected;
        return true;
    }

    private static bool IsEnvelopeField(string name) =>
        name.Equals("Version", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("ProfileId", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("ContentFingerprint", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("ProfileCompatibilityVersion", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("ProgressionProtocolVersion", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("RewardProtocolVersion", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("IdentityProtocolVersion", StringComparison.OrdinalIgnoreCase);

    private static int ReadInt(IReadOnlyDictionary<string, JsonElement> fields, string name)
    {
        if (!fields.TryGetValue(name, out var field)) return 0;
        if (field.ValueKind != JsonValueKind.Number || !field.TryGetInt32(out var value) || value < 0)
            throw new FormatException($"存档头字段 {name} 必须是非负整数。");
        return value;
    }

    private static string ReadString(IReadOnlyDictionary<string, JsonElement> fields, string name)
    {
        if (!fields.TryGetValue(name, out var field)) return string.Empty;
        if (field.ValueKind != JsonValueKind.String)
            throw new FormatException($"存档头字段 {name} 必须是字符串。");
        return field.GetString()!;
    }

    private static bool Reject(string message, out string error)
    { error = message; return false; }
}
