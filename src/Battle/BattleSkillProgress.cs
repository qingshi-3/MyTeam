using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using TowerAutobattler.Abilities;

namespace TowerAutobattler.Battle;

public enum SkillResourceKind { Mana, Grit, Timer, Condition, Passive, Counter }
public enum SkillProgressState { Building, Ready, Casting, Queued, Recovering, Waiting, Active, Spent, Defeated, Ended }

public sealed record BattleSkillProgress(string AbilityId, string DisplayName, SkillResourceKind Kind,
    float Current, float Maximum, string ResourceName, SkillProgressState State, string Detail = "", int Priority = 2);

public sealed record BattleUnitSkillsSnapshot(ImmutableArray<CompiledAbilityDefinition> Abilities,
    ImmutableArray<BattleSkillProgress> Progress)
{
    public static BattleUnitSkillsSnapshot Empty { get; } = new([], []);
    public BattleSkillProgress? Primary => Progress.IsDefaultOrEmpty ? null : Progress[0];
}

public sealed partial class BattleSimulation
{
    private ImmutableDictionary<string, BattleUnitSkillsSnapshot>? _terminalSkillProgress;

    public BattleUnitSkillsSnapshot ReadUnitSkills(string runtimeId)
    {
        if (_terminalSkillProgress is not null)
            return _terminalSkillProgress.GetValueOrDefault(runtimeId, BattleUnitSkillsSnapshot.Empty);
        var owner = _units.FirstOrDefault(unit => unit.RuntimeId == runtimeId);
        if (owner is null) return BattleUnitSkillsSnapshot.Empty;
        var runtime = _abilityScope?.ReadStates(runtimeId) ?? [];
        var progress = new List<BattleSkillProgress>();
        foreach (var ability in runtime) AddSkillProgress(owner, ability, runtime, progress);
        AddBehaviorProgress(owner, progress);
        return new(runtime.Select(state => state.Definition).ToImmutableArray(), progress
            .OrderBy(value => value.Priority).ThenBy(value => value.AbilityId, StringComparer.Ordinal)
            .Select(value => owner.Alive ? value : value with { State = SkillProgressState.Defeated })
            .ToImmutableArray());
    }

    private void CaptureTerminalSkillProgress()
    {
        if (_terminalSkillProgress is not null) return;
        _terminalSkillProgress = _units.ToImmutableDictionary(unit => unit.RuntimeId, unit =>
        {
            var snapshot = ReadUnitSkills(unit.RuntimeId);
            return snapshot with { Progress = snapshot.Progress.Select(value => value with
                { State = unit.Alive ? SkillProgressState.Ended : SkillProgressState.Defeated }).ToImmutableArray() };
        }, StringComparer.Ordinal);
    }

    private void AddSkillProgress(BattleUnitState owner, AbilityRuntimeStatus runtime,
        ImmutableArray<AbilityRuntimeStatus> all, List<BattleSkillProgress> result)
    {
        var skill = runtime.Definition;
        var priority = skill.IsDisplayedActiveSkill ? 0 : 2;
        BattleSkillProgress Make(SkillResourceKind kind, float current, float maximum, string name,
            SkillProgressState state, string detail = "") => new(skill.StableId, skill.DisplayName, kind,
                Math.Clamp(current, 0, Math.Max(1, maximum)), Math.Max(1, maximum), name, state, detail, priority);

        var punch = skill.Operations.OfType<CompiledGritPunchOperation>().FirstOrDefault();
        var storage = skill.Operations.OfType<CompiledGritStorageOperation>().FirstOrDefault();
        if (punch is not null || storage is not null)
        {
            storage ??= all.SelectMany(item => item.Definition.Operations).OfType<CompiledGritStorageOperation>()
                .FirstOrDefault(item => item.CounterKey == punch!.CounterKey);
            if (storage is not null)
            {
                var maximum = owner.MaxHealth * storage.MaximumHealthRatio;
                var current = CurrentGrit(owner, storage.CounterKey);
                var latch = _techniques.GritTriggers.GetValueOrDefault(CounterAddress(owner, storage.CounterKey, false), new GritTriggerLatch());
                var state = IsSkillExecuting(owner, skill) ? SkillProgressState.Casting :
                    UnitActionRecovering(owner) ? SkillProgressState.Recovering :
                    HasQueuedUnitAction(owner, skill.StableId) ? SkillProgressState.Queued :
                    current >= maximum ? SkillProgressState.Ready : SkillProgressState.Building;
                var detail = $"受伤储存，{storage.WindowTicks * BattleTiming.TickSeconds:0.#} 秒后逐笔衰减。";
                if (punch is not null)
                    detail += $"满怒 / 生命跌破 {punch.LowHealthRatio:P0} / 生命低于 {punch.CriticalHealthRatio:P0} 触发；濒死机会{(latch.CriticalUsed ? "已用" : "未用")}（每战一次）。" +
                        (HasQueuedUnitAction(owner, skill.StableId) ? "释放已排队，等待当前动作结束。" : "");
                result.Add(Make(SkillResourceKind.Grit, current, maximum, "怒劲", state, detail));
                return;
            }
        }

        if (skill.Operations.OfType<CompiledBroodPhase>().FirstOrDefault() is { } brood)
        {
            if (_enemyActions.Broods.TryGetValue(owner.RuntimeId, out var phase))
            {
                if (!phase.Transformed)
                    result.Add(Make(SkillResourceKind.Timer, TickIndex - phase.BreakStart, brood.BreakTicks,
                        "破壳", SkillProgressState.Casting));
                else
                {
                    var spent = phase.Batches >= brood.MaximumBatches;
                    result.Add(Make(SkillResourceKind.Timer, spent ? 0 : brood.SpawnCycleTicks - Math.Max(0, phase.NextSpawn - TickIndex),
                        brood.SpawnCycleTicks, "产卵", spent ? SkillProgressState.Spent : TickIndex >= phase.NextSpawn
                            ? SkillProgressState.Waiting : SkillProgressState.Building,
                        $"已产卵 {phase.Batches}/{brood.MaximumBatches} 批；存活上限 {brood.MaximumLiving}。"));
                }
                return;
            }
            result.Add(Make(SkillResourceKind.Condition, owner.MaxHealth - owner.Health,
                owner.MaxHealth * (1 - brood.HealthThreshold), "破壳", SkillProgressState.Waiting,
                $"生命降至 {brood.HealthThreshold:P0} 时破壳并改为产卵。") with { AbilityId = skill.StableId + ":phase", Priority = 1 });
        }

        if (skill.Trigger == AbilityTriggerKind.ManaFull)
        {
            result.Add(Make(SkillResourceKind.Mana, owner.CurrentMana, owner.MaxMana, "法力",
                IsSkillExecuting(owner, skill) ? SkillProgressState.Casting : owner.CurrentMana >= owner.MaxMana
                    ? SkillProgressState.Ready : SkillProgressState.Building,
                "满蓝且满足目标与行动条件时释放。"));
            return;
        }
        if (skill.MaxUses > 0 && runtime.Uses >= skill.MaxUses)
        {
            result.Add(Make(SkillResourceKind.Condition, 0, 1, "次数", SkillProgressState.Spent,
                $"已用 {runtime.Uses}/{skill.MaxUses} 次。"));
            return;
        }
        if (skill.Trigger == AbilityTriggerKind.PeriodicTick)
        {
            // Automatic skills use the global tick cadence, rounded after their real
            // successful commit. Missed/blocked attempts never spend readiness.
            var start = runtime.Uses > 0 ? runtime.ReadyTick - skill.CooldownTicks : runtime.RegisteredTick;
            var interval = Math.Max(1, skill.IntervalTicks);
            var earliest = Math.Max(start + 1, runtime.ReadyTick);
            var due = ((earliest + interval - 1) / interval) * interval;
            result.Add(Make(SkillResourceKind.Timer, TickIndex - start, due - start, "周期",
                IsSkillExecuting(owner, skill) ? SkillProgressState.Casting : TickIndex >= due
                    ? SkillProgressState.Ready : SkillProgressState.Building,
                "周期就绪后，仍需满足目标与行动条件。"));
            return;
        }
        if (skill.Operations.OfType<CompiledCounterattackOperation>().FirstOrDefault() is { } counter)
        {
            var hits = ReadCounter(owner, "_counter_" + skill.StableId);
            result.Add(Make(SkillResourceKind.Counter, hits, counter.HitsRequired, "受击",
                hits >= counter.HitsRequired ? SkillProgressState.Ready : SkillProgressState.Building,
                "累计受击次数，反击后清零。"));
            return;
        }
        if (runtime.ReadyTick > TickIndex)
        {
            result.Add(Make(SkillResourceKind.Timer, skill.CooldownTicks - (runtime.ReadyTick - TickIndex),
                skill.CooldownTicks, "冷却", SkillProgressState.Building, TriggerDescription(skill.Trigger)));
            return;
        }
        var constant = skill.ActivationKind == AbilityActivationKind.Passive;
        var openingUsed = skill.Trigger == AbilityTriggerKind.BattleStarted && runtime.Uses > 0;
        result.Add(Make(constant ? SkillResourceKind.Passive : SkillResourceKind.Condition,
            constant && runtime.Uses > 0 ? 1 : 0, 1, constant ? "常驻" : "触发",
            constant && runtime.Uses > 0 ? SkillProgressState.Active : openingUsed ? SkillProgressState.Spent : SkillProgressState.Waiting,
            TriggerDescription(skill.Trigger) + (constant ? "" : $"；已触发 {runtime.Uses} 次" +
                (skill.MaxUses > 0 ? $"，上限 {skill.MaxUses} 次" : ""))));
    }

    private bool IsSkillExecuting(BattleUnitState owner, CompiledAbilityDefinition skill) =>
        owner.Trample?.Origin.StableId == skill.StableId || owner.ChargedLine?.Origin.StableId == skill.StableId ||
        owner.ProjectileSequence?.Origin.StableId == skill.StableId ||
        (_techniques.Punches.TryGetValue(owner.RuntimeId, out var punch) && punch.Origin.StableId == skill.StableId) ||
        (_techniques.Hooks.TryGetValue(owner.RuntimeId, out var hook) && hook.Origin.StableId == skill.StableId) ||
        (_enemyActions.Casts.TryGetValue(owner.RuntimeId, out var cast) && cast.Origin.StableId == skill.StableId) ||
        _enemyActions.Blades.Values.Any(blade => blade.OwnerId == owner.RuntimeId && blade.Origin.StableId == skill.StableId);

    private static string TriggerDescription(AbilityTriggerKind trigger) => trigger switch
    {
        AbilityTriggerKind.None => "常驻效果",
        AbilityTriggerKind.BattleStarted => "开战时触发",
        AbilityTriggerKind.AttackHit => "普攻命中时触发",
        AbilityTriggerKind.OwnerDefeated => "阵亡时触发",
        AbilityTriggerKind.CriticalHit => "暴击时触发",
        AbilityTriggerKind.DodgedAttack => "闪避时触发",
        AbilityTriggerKind.ReceivedAttack => "受到普攻时触发",
        AbilityTriggerKind.HealthDamaged => "生命受损时触发",
        AbilityTriggerKind.ShieldReceived => "获得护盾时触发",
        AbilityTriggerKind.HealingDone => "造成治疗时触发",
        AbilityTriggerKind.OverhealReceived => "受到过量治疗时触发",
        AbilityTriggerKind.AllyManaCast => "友军施放法力技能时触发",
        AbilityTriggerKind.OwnerManaCast => "自身施放法力技能时触发",
        AbilityTriggerKind.AllyAttackHit => "友军普攻命中时触发",
        AbilityTriggerKind.AllyDefeated => "友军阵亡时触发",
        AbilityTriggerKind.EnemyDefeated => "敌军阵亡时触发",
        AbilityTriggerKind.ControlApplied => "施加控制时触发",
        AbilityTriggerKind.SummonAttackHit => "召唤物普攻命中时触发",
        AbilityTriggerKind.OwnerRevived => "返场时触发",
        AbilityTriggerKind.OwnerAttackOrSkillHit => "自身普攻或技能命中时触发",
        AbilityTriggerKind.AllyTemporaryDefeated => "友方临时单位阵亡时触发",
        AbilityTriggerKind.ActionQueued => "满足条件后排队释放",
        _ => "满足技能条件时触发"
    };

    private void AddBehaviorProgress(BattleUnitState owner, List<BattleSkillProgress> result)
    {
        var behavior = owner.Definition.Behavior;
        void Add(string id, string name, string detail, bool active = true) => result.Add(new("behavior:" + id,
            name, SkillResourceKind.Passive, active ? 1 : 0, 1, "被动",
            active ? SkillProgressState.Active : SkillProgressState.Waiting, detail, 3));
        void Timer(string id, string name, int interval)
        {
            if (interval <= 0) return;
            result.Add(new("behavior:" + id, name, SkillResourceKind.Timer, TickIndex % interval, interval,
                "周期", SkillProgressState.Building, "按战斗周期检查触发条件。", 1));
        }
        Timer("shield", "周期护盾", behavior.PeriodicShieldTicks);
        Timer("summon", "周期召唤", behavior.PeriodicSummonTicks);
        if (behavior.SlowOnHitTicks > 0) Add("slow", "命中减速", $"普攻命中时减速 {behavior.SlowOnHitTicks * BattleTiming.TickSeconds:0.#} 秒。");
        if (behavior.AdjacentArmorAura != 0) Add("armor_aura", "防御光环", $"相邻友军防御 {behavior.AdjacentArmorAura:+0.#;-0.#}。");
        if (behavior.AdjacentDamageAura != 0) Add("damage_aura", "攻击光环", "增强相邻友军攻击。");
        if (behavior.ExecuteHealthThreshold > 0) Add("execute", "斩杀", $"命中生命不高于 {behavior.ExecuteHealthThreshold:P0} 的目标时触发。");
        if (behavior.LowHealthDamageBonus > 0) Add("low_health", "低血增伤", "生命不高于 40% 时生效。", owner.Health <= owner.MaxHealth * .4f);
        if (behavior.OnDeathDamage > 0) Add("death", "亡语伤害", "阵亡时触发。", false);
        if (behavior.PiercingLine) Add("pierce", "穿透攻击", "普攻穿透直线目标。");
        if (behavior.PreferBacklineTargets) Add("backline", "后排优先", "优先选择后排目标。");
        if (owner.Definition.AttackHitGrowth is not null) Add("growth", "连续命中成长", $"当前 {owner.AttackHitStacks} 层。");
    }
}
