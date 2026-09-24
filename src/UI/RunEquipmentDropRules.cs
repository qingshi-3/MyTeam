using System.Linq;
using System;
using System.Runtime.CompilerServices;
using Godot;
using TowerAutobattler.Run;

namespace TowerAutobattler.UI;

public readonly record struct EquipmentDropEvaluation(bool Allowed, string ItemId, int SlotIndex, string Reason)
{
    public static EquipmentDropEvaluation Reject(string reason) => new(false, string.Empty, -1, reason);
}

// Drag previews are not ownership: resolve the current run again at hover and at release.
public static class RunEquipmentDropRules
{
    private sealed class ContextToken { public string Id { get; } = Guid.NewGuid().ToString("N"); }
    private static readonly ConditionalWeakTable<ActiveRunDto, ContextToken> Contexts = new();
    public static string ContextFor(ActiveRunDto run) => Contexts.GetOrCreateValue(run).Id;
    public static EquipmentDropEvaluation Evaluate(RunApplication? app, string heroId, Variant data,
        bool editing = true)
    {
        if (!EquipmentSlotButton.TryEquipmentId(data, out var itemId) ||
            !data.AsGodotDictionary().TryGetValue("equipment-scope", out var scope) ||
            scope.VariantType != Variant.Type.String || scope.AsString() != "run")
            return EquipmentDropEvaluation.Reject("只能拖入本局拥有的装备。");
        if (!editing || app?.ActiveRun is not { } run || app.EquipmentEditingLocked)
            return EquipmentDropEvaluation.Reject("当前不能更换装备。");
        if (!EquipmentSlotButton.HasContext(data, ContextFor(run)))
            return EquipmentDropEvaluation.Reject("装备来自另一局或已失效的拖动，请重新拿起。");
        var hero = run.Roster.FirstOrDefault(candidate => candidate.InstanceId == heroId);
        if (hero is null) return EquipmentDropEvaluation.Reject("请拖到己方英雄，不能放到空格或敌人身上。");
        var item = run.EquipmentInventory.Concat(run.Roster.SelectMany(owner => owner.Equipment))
            .FirstOrDefault(candidate => candidate.InstanceId == itemId);
        if (item is null) return EquipmentDropEvaluation.Reject("这件装备已不在当前背包或英雄身上。");
        if (hero.Equipment.Any(candidate => candidate.InstanceId == itemId))
            return EquipmentDropEvaluation.Reject("该英雄已穿戴这件装备；调整位置请拖到具体装备槽。");
        for (var slot = 0; slot < app.Rules.EquipmentSlotCapacity; slot++)
            if (hero.Equipment.All(candidate => candidate.SlotIndex != slot))
                return new EquipmentDropEvaluation(true, itemId, slot, $"松手装入第 {slot + 1} 个空槽。");
        return EquipmentDropEvaluation.Reject("该英雄装备已满，请拖到具体装备槽替换。");
    }
}
