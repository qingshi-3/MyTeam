using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Godot;
using TowerAutobattler.Content;
using TowerAutobattler.Growth;
using TowerAutobattler.Project;

namespace TowerAutobattler.Composition;

// Stable content/configuration identity, separate from the ReadyGate publication
// counter. Data records use stored fields (never calculated getters such as Tower).
// Native art/script resources contribute their authored paths, not instance IDs or
// pixel buffers. Execution-code compatibility is owned by the protocol versions.
internal static class RunProfileContentFingerprint
{
    public static string Compute(string profileId, int compatibilityVersion, int progressionProtocolVersion,
        int rewardProtocolVersion, int identityProtocolVersion, CompiledGameProject project,
        CompiledGrowthRules? growthRules, CompiledContentGraph content)
    {
        var writer = new CanonicalWriter();
        return writer.Hash(new object?[]
        {
            "run-profile-fingerprint-v1", profileId, compatibilityVersion, progressionProtocolVersion,
            rewardProtocolVersion, identityProtocolVersion, project, growthRules, content
        });
    }

    private sealed class CanonicalWriter
    {
        // Reference identity is only a traversal optimization; it never enters the digest.
        private readonly Dictionary<object, string> _cache = new(ReferenceEqualityComparer.Instance);
        private readonly HashSet<object> _active = new(ReferenceEqualityComparer.Instance);

        public string Hash(object? value)
        {
            if (value is Variant variant) return Hash(variant.Obj);
            if (value is null) return Digest("null");
            var type = value.GetType();
            var reference = !type.IsValueType && value is not string;
            if (reference && _cache.TryGetValue(value, out var cached)) return cached;
            if (reference && !_active.Add(value))
            {
                // Resource back-references can occur in authored presentation scenes.
                // Gameplay record cycles have no finite configuration and are rejected.
                if (value is Resource resource && !string.IsNullOrEmpty(resource.ResourcePath))
                    return Digest("resource-back-reference:" + type.FullName + ":" + resource.ResourcePath);
                throw new InvalidOperationException($"Cyclic profile fingerprint input: {type.FullName}.");
            }
            try
            {
                var text = new StringBuilder();
                Token(text, type.FullName ?? type.Name);
                switch (value)
                {
                    case string textValue: Token(text, textValue); break;
                    case StringName name: Token(text, name.ToString()); break;
                    case NodePath path: Token(text, path.ToString()); break;
                    case Enum enumeration: Token(text, Convert.ToInt64(enumeration, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture)); break;
                    case IFormattable number when type.IsPrimitive || value is decimal:
                        Token(text, number.ToString(value is float or double ? "R" : null, CultureInfo.InvariantCulture)); break;
                    case bool boolean: Token(text, boolean ? "true" : "false"); break;
                    case Resource resource: Resource(text, resource); break;
                    case Godot.Collections.Dictionary dictionary:
                        Dictionary(text, dictionary.Select(pair => ((object?)pair.Key, (object?)pair.Value))); break;
                    case IDictionary dictionary:
                        Dictionary(text, DictionaryEntries(dictionary)); break;
                    case IEnumerable sequence:
                        if (IsDictionary(type))
                            Dictionary(text, sequence.Cast<object>().Select(pair =>
                                (pair.GetType().GetProperty("Key")!.GetValue(pair), pair.GetType().GetProperty("Value")!.GetValue(pair))));
                        else if (!(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(System.Collections.Immutable.ImmutableArray<>) &&
                                   (bool)type.GetProperty("IsDefault")!.GetValue(value)!))
                            foreach (var item in sequence) Token(text, Hash(item));
                        break;
                    default:
                        if (type.Namespace == "Godot" && type.IsValueType)
                        {
                            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public).OrderBy(field => field.Name, StringComparer.Ordinal))
                            { Token(text, field.Name); Token(text, Hash(field.GetValue(value))); }
                        }
                        else if (type.Namespace?.StartsWith("TowerAutobattler.", StringComparison.Ordinal) == true)
                        {
                            foreach (var field in DataFields(type).OrderBy(field => field.Name, StringComparer.Ordinal))
                            { Token(text, field.Name); Token(text, Hash(field.GetValue(value))); }
                        }
                        else throw new InvalidOperationException($"Unsupported profile fingerprint input: {type.FullName}.");
                        break;
                }
                var result = Digest(text.ToString());
                if (reference) _cache[value] = result;
                return result;
            }
            finally { if (reference) _active.Remove(value); }
        }

        private void Resource(StringBuilder text, Resource resource)
        {
            Token(text, resource.ResourcePath);
            if (resource is PackedScene scene)
            {
                Scene(text, scene.GetState());
                return;
            }
            // Only exported authoring values enter a Resource fingerprint. Engine
            // handles, transient caches and native wrapper properties are excluded.
            foreach (var property in resource.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
                         .Where(property => property.IsDefined(typeof(ExportAttribute), true))
                         .OrderBy(property => property.Name, StringComparer.Ordinal))
            { Token(text, property.Name); Token(text, Hash(property.GetValue(resource))); }
            foreach (var field in resource.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public)
                         .Where(field => field.IsDefined(typeof(ExportAttribute), true))
                         .OrderBy(field => field.Name, StringComparer.Ordinal))
            { Token(text, field.Name); Token(text, Hash(field.GetValue(resource))); }
        }

        private void Scene(StringBuilder text, SceneState state)
        {
            if (state.GetBaseSceneState() is { } inherited)
            { Token(text, "base"); Scene(text, inherited); }
            Token(text, state.GetNodeCount().ToString(CultureInfo.InvariantCulture));
            for (var node = 0; node < state.GetNodeCount(); node++)
            {
                Token(text, state.GetNodePath(node).ToString());
                Token(text, state.GetNodeType(node).ToString());
                Token(text, Hash(state.GetNodeInstance(node)));
                foreach (var index in Enumerable.Range(0, state.GetNodePropertyCount(node))
                             .OrderBy(index => state.GetNodePropertyName(node, index).ToString(), StringComparer.Ordinal))
                {
                    Token(text, state.GetNodePropertyName(node, index).ToString());
                    Token(text, Hash(state.GetNodePropertyValue(node, index)));
                }
            }
        }

        private void Dictionary(StringBuilder text, IEnumerable<(object? Key, object? Value)> pairs)
        {
            // Hash values only after sorting keys, so traversal is independent of
            // dictionary insertion order even when authored resources are shared.
            foreach (var pair in pairs.Select(pair => (Key: Hash(pair.Key), pair.Value)).OrderBy(pair => pair.Key, StringComparer.Ordinal))
            { Token(text, pair.Key); Token(text, Hash(pair.Value)); }
        }

        private static IEnumerable<FieldInfo> DataFields(Type type)
        {
            for (var current = type; current is not null && current != typeof(object); current = current.BaseType)
                foreach (var field in current.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    if (field.Name.EndsWith("k__BackingField", StringComparison.Ordinal)) yield return field;
        }

        private static bool IsDictionary(Type type) => type.GetInterfaces().Any(contract => contract.IsGenericType &&
            (contract.GetGenericTypeDefinition() == typeof(IDictionary<,>) || contract.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>)));
        private static IEnumerable<(object? Key, object? Value)> DictionaryEntries(IDictionary dictionary)
        {
            var iterator = dictionary.GetEnumerator();
            while (iterator.MoveNext()) yield return (iterator.Key, iterator.Value);
        }
        private static void Token(StringBuilder text, string? token) =>
            text.Append((token?.Length ?? -1).ToString(CultureInfo.InvariantCulture)).Append(':').Append(token);
        private static string Digest(string text) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
    }
}
