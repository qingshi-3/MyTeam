using System.Collections.Generic;

namespace TowerAutobattler.Content;

public sealed class ValidationReport
{
    private readonly List<string> _coreErrors = [];
    private readonly List<string> _warnings = [];

    public IReadOnlyList<string> CoreErrors => _coreErrors;
    public IReadOnlyList<string> Warnings => _warnings;
    public bool HasCoreErrors => _coreErrors.Count > 0;

    public void Error(string message) => _coreErrors.Add(message);
    public void Warn(string message) => _warnings.Add(message);
    public void Merge(ValidationReport other)
    {
        _coreErrors.AddRange(other._coreErrors);
        _warnings.AddRange(other._warnings);
    }
}
