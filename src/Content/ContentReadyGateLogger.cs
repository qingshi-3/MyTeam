using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Battle;
using TowerAutobattler.Equipment;
using TowerAutobattler.Relics;
using TowerAutobattler.Statuses;
using TowerAutobattler.TacticalCommands;
using TowerAutobattler.Traits;

namespace TowerAutobattler.Content;

internal sealed partial class ContentReadyGateLogger : Logger
{
    private readonly object _lock = new();
    private readonly List<string> _errors = [];
    public IReadOnlyList<string> Errors
    {
        get { lock (_lock) return _errors.ToArray(); }
    }

    public override void _LogError(
        string function, string file, int line, string code, string rationale,
        bool editorNotify, int errorType, Godot.Collections.Array<ScriptBacktrace> scriptBacktraces)
    {
        if (errorType == (int)ErrorType.Warning) return;
        var message = string.IsNullOrWhiteSpace(rationale) ? code : rationale;
        lock (_lock) _errors.Add($"{message} ({file}:{line} {function})");
    }
}
