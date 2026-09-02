using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Godot;

internal static class ResourceGraphFingerprint
{
    public static string Compute(IEnumerable<Resource?> roots)
    {
        ArgumentNullException.ThrowIfNull(roots);
        var writer = new Writer();
        var index = 0;
        foreach (var root in roots)
        {
            writer.Append($"root[{index++}]=");
            writer.WriteResource(root);
        }
        return Hash(writer.Text);
    }

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private sealed class Writer
    {
        private readonly StringBuilder _text = new();
        private readonly Dictionary<Resource, int> _visited = new(ReferenceEqualityComparer.Instance);

        public string Text => _text.ToString();
        public void Append(string value) => _text.Append(value);

        public void WriteResource(Resource? resource)
        {
            if (resource is null)
            {
                _text.Append("null;");
                return;
            }
            if (_visited.TryGetValue(resource, out var existing))
            {
                _text.Append("ref:").Append(existing).Append(';');
                return;
            }

            var ordinal = _visited.Count;
            _visited.Add(resource, ordinal);
            _text.Append("resource:").Append(ordinal).Append(':')
                .Append(resource.GetType().FullName).Append(':')
                .Append(resource.ResourcePath).Append('{');
            if (IsOpaqueAsset(resource))
            {
                _text.Append("opaque};");
                return;
            }

            var properties = resource.GetPropertyList()
                .Where(property => property.ContainsKey("name") && property.ContainsKey("usage"))
                .Select(property => (
                    Name: property["name"].AsStringName().ToString(),
                    Usage: (PropertyUsageFlags)property["usage"].AsInt64()))
                .Where(property => (property.Usage & PropertyUsageFlags.Storage) != 0 &&
                                   property.Name is not "script")
                .OrderBy(property => property.Name, StringComparer.Ordinal);
            foreach (var property in properties)
            {
                _text.Append(property.Name).Append('=');
                WriteVariant(resource.Get(property.Name));
            }
            _text.Append("};");
        }

        private void WriteVariant(Variant value)
        {
            _text.Append('[').Append((int)value.VariantType).Append(':');
            switch (value.VariantType)
            {
                case Variant.Type.Nil:
                    _text.Append("null");
                    break;
                case Variant.Type.Object:
                    if (value.AsGodotObject() is Resource resource)
                        WriteResource(resource);
                    else
                        _text.Append(value.AsGodotObject()?.GetType().FullName ?? "null");
                    break;
                case Variant.Type.Array:
                    var array = value.AsGodotArray();
                    _text.Append("count=").Append(array.Count).Append('{');
                    foreach (var item in array) WriteVariant(item);
                    _text.Append('}');
                    break;
                case Variant.Type.Dictionary:
                    var dictionary = value.AsGodotDictionary();
                    var keys = dictionary.Keys.OrderBy(KeyLabel, StringComparer.Ordinal).ToArray();
                    _text.Append("count=").Append(keys.Length).Append('{');
                    foreach (var key in keys)
                    {
                        WriteVariant(key);
                        WriteVariant(dictionary[key]);
                    }
                    _text.Append('}');
                    break;
                default:
                    _text.Append(GD.VarToStr(value));
                    break;
            }
            _text.Append("]; ");
        }

        private static string KeyLabel(Variant key) =>
            $"{(int)key.VariantType}:{GD.VarToStr(key)}";

        private static bool IsOpaqueAsset(Resource resource) => resource is
            PackedScene or Script or Texture2D or SpriteFrames or Theme or Font or Shader or Material;
    }
}
