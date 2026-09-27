using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using TowerAutobattler.Content;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.Project;
using TowerAutobattler.Run;

namespace TowerAutobattler.Growth;

// Owns mutable growth state. Every public command edits a detached Run and
// publishes only after the whole command validates and saves successfully.
public sealed class GrowthRunService(
    ContentRegistry content,
    CompiledGameProject project,
    CompiledGrowthRules? rules,
    RunProgressionPersistenceService persistence)
{
    public CompiledGrowthRules? Rules => rules;

    public void InitializeRun(ActiveRunDto run)
    {
        if (rules is null) return;
        run.Growth = new GrowthRunDto { RulesId = rules.StableId };
        foreach (var hero in run.Roster) InitializeHero(hero);
    }

    public void InitializeHero(RosterHeroInstanceDto hero) => hero.Growth ??= new HeroGrowthDto();

    public GrowthCommandResult CheckAction(ActiveRunDto? run)
    {
        if (rules is null) return GrowthCommandResult.Reject("当前征程未启用成长规则。");
        if (run?.Growth is not { } growth || growth.RulesId != rules.StableId)
            return GrowthCommandResult.Reject("当前征程没有可用的成长状态。");
        if (run.OpeningRecruitment is not null || !string.IsNullOrEmpty(run.TerminalCompletionId))
            return GrowthCommandResult.Reject("当前阶段不能操作工坊。");
        if (growth.PendingDiscovery is not null)
            return GrowthCommandResult.Reject("请先领取已经支付的发现奖励。");
        if (growth.PendingNode is not null)
            return GrowthCommandResult.Reject(growth.PendingNode.IsBattle ? "本场战斗已经开始，整备状态已冻结。" : "当前节点的生产状态已冻结。");
        return GrowthCommandResult.Success();
    }

    public GrowthCommandResult SetAssignment(ActiveRunDto? run, string producerId, string targetId, GrowthProductionMode mode)
    {
        var check = CheckAction(run);
        if (!check.Succeeded) return check;
        var producer = run!.Roster.SingleOrDefault(hero => hero.InstanceId == producerId);
        var target = run.Roster.SingleOrDefault(hero => hero.InstanceId == targetId);
        if (producer is null || target is null || !rules!.Heroes.TryGetValue(producer.ContentId, out var definition) ||
            !definition.ProductionModes.Contains(mode))
            return GrowthCommandResult.Reject("生产者、受益者或生产方式无效。");
        if (mode == GrowthProductionMode.Research && !string.IsNullOrEmpty(targetId) && producerId != targetId)
            return GrowthCommandResult.Reject("研究生产不需要指定其他受益者。");
        var working = persistence.CloneRun(run);
        var edited = working.Roster.Single(hero => hero.InstanceId == producerId);
        edited.Growth.ProductionMode = mode;
        edited.Growth.ProductionTargetInstanceId = mode == GrowthProductionMode.Research ? producerId : targetId;
        return Commit(working, run, "生产分配已更新。", "保存失败，原生产分配已保留。");
    }

    public GrowthCommandResult Ascend(ActiveRunDto? run, string heroId)
    {
        var check = CheckAction(run);
        if (!check.Succeeded) return check;
        var growth = run!.Growth!;
        if (run.PendingOffer is not null || growth.PendingNode is not null ||
            run.PendingNode && RunDecisionService.KindFor(run.SelectedNode) is not null)
            return GrowthCommandResult.Reject("升阶只能在未冻结的战斗整备阶段进行。");
        var hero = run.Roster.SingleOrDefault(candidate => candidate.InstanceId == heroId);
        if (hero is null || !rules!.Heroes.TryGetValue(hero.ContentId, out var definition) ||
            string.IsNullOrWhiteSpace(definition.AscensionId) || definition.AscendedLoadout is null ||
            !string.IsNullOrEmpty(hero.Growth.AscensionId))
            return GrowthCommandResult.Reject("该英雄没有可用的升阶配置，或已经升阶。");
        var candidates = DiscoveryCandidates(run, heroId);
        if (candidates.Count != 3) return GrowthCommandResult.Reject("当前发现池不足三名不同且未拥有的候选，未扣除材料。");
        if (run.Roster.Count >= run.CurrentPopulation + project.RunRules.ReserveCapacity)
            return GrowthCommandResult.Reject("队伍与后备已满，无法保留发现奖励，未扣除材料。");
        var category = definition.MaterialCategory;
        var categoryCount = string.IsNullOrEmpty(category) ? 0 : growth.CategoryMaterials.GetValueOrDefault(category);
        if ((long)growth.Materials + categoryCount < rules.AscensionCost)
            return GrowthCommandResult.Reject("升阶材料不足。");
        var working = persistence.CloneRun(run);
        var editedGrowth = working.Growth!;
        var useCategory = Math.Min(categoryCount, rules.AscensionCost);
        if (useCategory > 0) editedGrowth.CategoryMaterials[category] -= useCategory;
        editedGrowth.Materials -= rules.AscensionCost - useCategory;
        working.Roster.Single(candidate => candidate.InstanceId == heroId).Growth.AscensionId = definition.AscensionId;
        editedGrowth.PendingDiscovery = new GrowthDiscoveryDto
        {
            OfferId = DiscoveryIdentity(run, heroId),
            UpgradedHeroInstanceId = heroId,
            CandidateIds = candidates
        };
        return Commit(working, run, "升阶完成，请选择一名新英雄。", "保存失败，材料、升阶与发现奖励均未提交。");
    }

    public GrowthCommandResult ChooseHero(ActiveRunDto? run, string contentId)
    {
        if (rules is null || run?.Growth is not { } state || state.RulesId != rules.StableId ||
            run.OpeningRecruitment is not null || !string.IsNullOrEmpty(run.TerminalCompletionId))
            return GrowthCommandResult.Reject("当前征程没有可领取的成长发现。");
        var discovery = run!.Growth!.PendingDiscovery;
        if (discovery is null || !discovery.CandidateIds.Contains(contentId, StringComparer.Ordinal))
            return GrowthCommandResult.Reject("候选不属于当前发现奖励。");
        if (run.Roster.Any(hero => hero.ContentId == contentId)) return GrowthCommandResult.Reject("该英雄已在队伍中。");
        if (!content.TryGet(contentId, out var entry) || entry.Definition is not UnitDefinition { IsHero: true, IsEnemy: false, IsTestDummy: false })
            return GrowthCommandResult.Reject("候选英雄内容无效。");
        if (run.Roster.Count >= run.CurrentPopulation + project.RunRules.ReserveCapacity)
            return GrowthCommandResult.Reject("队伍与后备已满。");
        var working = persistence.CloneRun(run);
        var instanceId = NextHeroInstanceId(working);
        if (instanceId is null) return GrowthCommandResult.Reject("无法分配新的英雄实例身份。");
        var hero = new RosterHeroInstanceDto { InstanceId = instanceId, ContentId = contentId };
        InitializeHero(hero);
        working.Roster.Add(hero);
        working.Growth!.PendingDiscovery = null;
        return Commit(working, run, "发现英雄已加入后备。", "保存失败，发现奖励仍可领取。");
    }

    public GrowthCommandResult CraftSpell(ActiveRunDto? run, string spellId)
    {
        var check = CheckAction(run);
        if (!check.Succeeded) return check;
        if (!rules!.Spells.TryGetValue(spellId, out var spell) || spell.ResearchCost <= 0)
            return GrowthCommandResult.Reject("未知的研究法术。");
        if (run!.Growth!.Research < spell.ResearchCost) return GrowthCommandResult.Reject("研究点不足。");
        var working = persistence.CloneRun(run);
        working.Growth!.Research -= spell.ResearchCost;
        working.Growth.SpellInventory[spellId] = checked(working.Growth.SpellInventory.GetValueOrDefault(spellId) + 1);
        return Commit(working, run, "法术已制作并放入库存。", "保存失败，研究点与库存均未改变。");
    }

    public GrowthCommandResult EquipSpell(ActiveRunDto? run, string spellId, string targetId)
    {
        var check = CheckAction(run);
        if (!check.Succeeded) return check;
        var active = run!;
        if (string.IsNullOrEmpty(spellId))
        {
            var working = persistence.CloneRun(active);
            working.Growth!.EquippedSpellId = string.Empty;
            working.Growth.SpellTargetInstanceId = string.Empty;
            return Commit(working, active, "已卸下开战法术。", "保存失败，原法术预设已保留。");
        }
        if (!rules!.Spells.ContainsKey(spellId) || active.Growth!.SpellInventory.GetValueOrDefault(spellId) <= 0 ||
            active.Roster.All(hero => hero.InstanceId != targetId) || !active.Deployment.Contains(targetId))
            return GrowthCommandResult.Reject("法术库存不足，或目标不是当前已部署英雄。");
        var edited = persistence.CloneRun(active);
        edited.Growth!.EquippedSpellId = spellId;
        edited.Growth.SpellTargetInstanceId = targetId;
        return Commit(edited, active, "开战法术预设已更新。", "保存失败，原法术预设已保留。");
    }

    public GrowthCommandResult BeginBattle(ActiveRunDto? run, EncounterPlan encounter)
    {
        if (rules is null) return GrowthCommandResult.Success();
        if (run?.Growth is not { } state || state.RulesId != rules.StableId)
            return GrowthCommandResult.Reject("当前征程没有可用的成长状态。");
        if (run.OpeningRecruitment is not null || !string.IsNullOrEmpty(run.TerminalCompletionId))
            return GrowthCommandResult.Reject("当前阶段不能开始战斗。");
        if (run!.Growth!.PendingDiscovery is not null) return GrowthCommandResult.Reject("请先领取发现奖励再开始下一场战斗。");
        if (!run.PendingNode || encounter.NodeType != run.SelectedNode || RunDecisionService.KindFor(run.SelectedNode) is not null)
            return GrowthCommandResult.Reject("当前没有可开始的战斗节点。");
        var expected = new TowerGenerator(project.Campaign).Encounter(run, run.SelectedNode);
        if (encounter.EncounterId != expected.EncounterId || encounter.Title != expected.Title ||
            encounter.FloorRuleId != expected.FloorRuleId || encounter.IsBoss != expected.IsBoss ||
            encounter.IsElite != expected.IsElite || !encounter.EnemyIds.SequenceEqual(expected.EnemyIds) ||
            encounter.CompositionId != expected.CompositionId || !encounter.EnemyCells.SequenceEqual(expected.EnemyCells))
            return GrowthCommandResult.Reject("战斗计划与当前节点不匹配。");
        if (state.PendingNode is { } frozen)
            return frozen.IsBattle && frozen.FloorIndex == run.FloorIndex && frozen.BattleNumber == run.BattleNumber
                ? GrowthCommandResult.Success("本场战斗已冻结，可安全重试启动。")
                : GrowthCommandResult.Reject("当前节点已有不匹配的成长快照。");
        var working = persistence.CloneRun(run);
        var snapshot = Freeze(working, true);
        var selected = working.Growth!.EquippedSpellId;
        if (!string.IsNullOrEmpty(selected))
        {
            if (!rules!.Spells.ContainsKey(selected) || working.Growth.SpellInventory.GetValueOrDefault(selected) <= 0 ||
                !snapshot.ParticipantInstanceIds.Contains(working.Growth.SpellTargetInstanceId))
                return GrowthCommandResult.Reject("法术库存或目标已失效，请重新选择。");
            working.Growth.SpellInventory[selected]--;
            snapshot.ConsumedSpellId = selected;
            snapshot.SpellTargetInstanceId = working.Growth.SpellTargetInstanceId;
            working.Growth.EquippedSpellId = string.Empty;
            working.Growth.SpellTargetInstanceId = string.Empty;
        }
        return Commit(working, run, "战斗成长状态已冻结。", "保存失败，法术未消耗且战斗未开始。");
    }

    public bool FreezeNonCombat(ActiveRunDto working)
    {
        if (rules is null || working.Growth is null) return true;
        if (working.Growth.PendingDiscovery is not null || working.Growth.PendingNode is not null) return false;
        Freeze(working, false);
        return true;
    }

    public bool SettleNode(ActiveRunDto working)
    {
        if (rules is null || working.Growth is null) return true;
        var snapshot = working.Growth.PendingNode;
        if (snapshot is null || snapshot.FloorIndex != working.FloorIndex || snapshot.BattleNumber > working.BattleNumber ||
            working.Growth.LastSettledFloorIndex >= snapshot.FloorIndex) return false;
        var settlement = new GrowthSettlementDto
        {
            FloorIndex = snapshot.FloorIndex,
            BattleNumber = snapshot.BattleNumber,
            IsBattle = snapshot.IsBattle,
            MaterialsGranted = rules.MaterialsPerNode
        };
        var battleGainKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var gain in working.Roster.SelectMany(hero => hero.Growth.History).Where(gain =>
                     gain.FloorIndex == snapshot.FloorIndex && gain.BattleNumber == snapshot.BattleNumber && gain.Source == "battle"))
        {
            var key = $"{gain.ProducerInstanceId}:{gain.TargetInstanceId}:{gain.AbilityId}:{gain.Mode}:{gain.Amount:R}";
            if (battleGainKeys.Add(key)) settlement.Gains.Add(gain);
        }
        foreach (var assignment in snapshot.Assignments)
        {
            var producer = working.Roster.SingleOrDefault(hero => hero.InstanceId == assignment.ProducerInstanceId);
            if (producer is null || !rules.Heroes.TryGetValue(producer.ContentId, out var producerRule)) continue;
            if (assignment.Mode == GrowthProductionMode.Research)
            {
                working.Growth.Research = checked(working.Growth.Research + producerRule.ResearchYield);
                AddGain(producer, settlement, snapshot, assignment, producerRule.ResearchYield);
                continue;
            }
            var target = working.Roster.SingleOrDefault(hero => hero.InstanceId == assignment.TargetInstanceId);
            if (target is null || !snapshot.ParticipantInstanceIds.Contains(target.InstanceId) ||
                !content.TryGet(target.ContentId, out var targetEntry) || targetEntry.Definition is not UnitDefinition definition) continue;
            var amount = (assignment.Mode == GrowthProductionMode.Attack ? definition.AttackDamage : definition.MaxHealth) * producerRule.GrowthRate;
            if (!float.IsFinite(amount) || amount <= 0) continue;
            if (assignment.Mode == GrowthProductionMode.Attack) target.Growth.AddedAttack += amount;
            else target.Growth.AddedMaxHealth += amount;
            AddGain(target, settlement, snapshot, assignment, amount);
        }
        working.Growth.Materials = checked(working.Growth.Materials + rules.MaterialsPerNode);
        working.Growth.LastSettledFloorIndex = snapshot.FloorIndex;
        working.Growth.History.Add(settlement);
        working.Growth.PendingNode = null;
        return true;
    }

    public bool ApplyBattlePermanentGains(ActiveRunDto working,
        ImmutableArray<BattlePermanentGain> gains, bool apply)
    {
        if (gains.IsDefaultOrEmpty) return true;
        if (rules is null || working.Growth?.PendingNode is not { IsBattle: true } snapshot) return false;
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var gain in gains)
        {
            if (gain is null || string.IsNullOrWhiteSpace(gain.SourceInstanceId) ||
                gain.SourceInstanceId != gain.TargetInstanceId || string.IsNullOrWhiteSpace(gain.AbilityId) ||
                string.IsNullOrWhiteSpace(gain.Slot) || !float.IsFinite(gain.Amount) || gain.Amount <= 0 ||
                gain.Attribute is not (CombatAttribute.AttackDamage or CombatAttribute.MaxHealth) ||
                !snapshot.ParticipantInstanceIds.Contains(gain.SourceInstanceId) ||
                !keys.Add($"{gain.SourceInstanceId}:{gain.AbilityId}:{gain.Slot}")) return false;
            var hero = working.Roster.SingleOrDefault(candidate => candidate.InstanceId == gain.SourceInstanceId);
            if (hero is null || !rules.Heroes.TryGetValue(hero.ContentId, out var definition)) return false;
            var loadout = string.IsNullOrEmpty(hero.Growth.AscensionId) ? definition.BaseLoadout : definition.AscendedLoadout;
            if (!MatchesPermanentOperation(loadout, gain)) return false;
            if (!apply) continue;
            if (gain.Attribute == CombatAttribute.AttackDamage) hero.Growth.AddedAttack += gain.Amount;
            else hero.Growth.AddedMaxHealth += gain.Amount;
            hero.Growth.History.Add(new GrowthGainDto
            {
                FloorIndex = snapshot.FloorIndex,
                BattleNumber = snapshot.BattleNumber,
                ProducerInstanceId = gain.SourceInstanceId,
                TargetInstanceId = gain.TargetInstanceId,
                Mode = gain.Attribute == CombatAttribute.AttackDamage ? GrowthProductionMode.Attack : GrowthProductionMode.Vitality,
                Amount = gain.Amount,
                AbilityId = gain.AbilityId,
                Source = "battle"
            });
        }
        return true;
    }

    public bool Validate(ActiveRunDto run)
    {
        if (run.Roster.Any(hero => hero?.Growth is null || hero.Growth.AscensionId is null ||
            hero.Growth.ProductionTargetInstanceId is null || hero.Growth.History is null || hero.Growth.History.Any(gain => gain is null) ||
            !float.IsFinite(hero.Growth.AddedAttack) || hero.Growth.AddedAttack < 0 ||
            !float.IsFinite(hero.Growth.AddedMaxHealth) || hero.Growth.AddedMaxHealth < 0)) return false;
        if (run.Growth is null) return rules is null;
        if (rules is null || run.Growth.RulesId is null || run.Growth.EquippedSpellId is null ||
            run.Growth.SpellTargetInstanceId is null || run.Growth.RulesId != rules.StableId || run.Growth.Materials < 0 || run.Growth.Research < 0 ||
            run.Growth.LastSettledFloorIndex < -1 || run.Growth.LastSettledFloorIndex > run.FloorIndex ||
            run.Growth.CategoryMaterials is null || run.Growth.SpellInventory is null || run.Growth.History is null ||
            run.Growth.History.Any(value => value is null || value.FloorIndex < 0 || value.FloorIndex > run.FloorIndex ||
                value.BattleNumber < 0 || value.BattleNumber > run.BattleNumber ||
                value.MaterialsGranted < 0 || value.Gains is null || value.Gains.Any(gain => !ValidGain(gain))) ||
            run.Growth.History.GroupBy(value => value.FloorIndex).Any(group => group.Count() != 1) ||
            run.Growth.CategoryMaterials.Any(pair => string.IsNullOrWhiteSpace(pair.Key) || pair.Value < 0) ||
            run.Growth.SpellInventory.Any(pair => !rules.Spells.ContainsKey(pair.Key) || pair.Value < 0)) return false;
        var ids = run.Roster.Select(hero => hero.InstanceId).ToHashSet(StringComparer.Ordinal);
        if (string.IsNullOrEmpty(run.Growth.EquippedSpellId) != string.IsNullOrEmpty(run.Growth.SpellTargetInstanceId) ||
            !string.IsNullOrEmpty(run.Growth.EquippedSpellId) &&
             (!rules.Spells.ContainsKey(run.Growth.EquippedSpellId) ||
             run.Growth.SpellInventory.GetValueOrDefault(run.Growth.EquippedSpellId) <= 0 ||
             !ids.Contains(run.Growth.SpellTargetInstanceId) || !run.Deployment.Contains(run.Growth.SpellTargetInstanceId))) return false;
        foreach (var hero in run.Roster)
        {
            if (!rules.Heroes.TryGetValue(hero.ContentId, out var definition))
            {
                if (!string.IsNullOrEmpty(hero.Growth.AscensionId)) return false;
                continue;
            }
            if (!string.IsNullOrEmpty(hero.Growth.AscensionId) && hero.Growth.AscensionId != definition.AscensionId) return false;
            if (!definition.ProductionModes.Contains(hero.Growth.ProductionMode) &&
                !string.IsNullOrEmpty(hero.Growth.ProductionTargetInstanceId)) return false;
            if (!string.IsNullOrEmpty(hero.Growth.ProductionTargetInstanceId) && !ids.Contains(hero.Growth.ProductionTargetInstanceId)) return false;
            if (hero.Growth.History.Any(gain => !ValidGain(gain) || gain.FloorIndex > run.FloorIndex || gain.BattleNumber > run.BattleNumber)) return false;
        }
        var correctPool = run.FloorIndex < project.Campaign.FloorsPerRegion ? rules.FirstDiscoveryPool : rules.AdvancedDiscoveryPool;
        if (run.Growth.PendingDiscovery is { } discovery &&
            (string.IsNullOrWhiteSpace(discovery.OfferId) || string.IsNullOrWhiteSpace(discovery.UpgradedHeroInstanceId) ||
             !ids.Contains(discovery.UpgradedHeroInstanceId) ||
             discovery.OfferId != DiscoveryIdentity(run, discovery.UpgradedHeroInstanceId) ||
             discovery.CandidateIds is null || discovery.CandidateIds.Count != 3 || discovery.CandidateIds.Distinct().Count() != 3 ||
             discovery.CandidateIds.Any(id => string.IsNullOrWhiteSpace(id) || !correctPool.Contains(id) ||
                 run.Roster.Any(hero => hero.ContentId == id)) ||
             (long)run.Roster.Count >= (long)run.CurrentPopulation + project.RunRules.ReserveCapacity ||
             run.Roster.Single(hero => hero.InstanceId == discovery.UpgradedHeroInstanceId).Growth.AscensionId.Length == 0)) return false;
        if (run.Growth.PendingNode is { } node &&
            (node.ParticipantInstanceIds is null || node.Assignments is null || node.ConsumedSpellId is null ||
             node.SpellTargetInstanceId is null || node.FloorIndex != run.FloorIndex || node.BattleNumber != run.BattleNumber ||
             node.IsBattle != (RunDecisionService.KindFor(run.SelectedNode) is null) ||
             node.ParticipantInstanceIds.Distinct().Count() != node.ParticipantInstanceIds.Count ||
             node.ParticipantInstanceIds.Any(id => string.IsNullOrWhiteSpace(id) || !ids.Contains(id)) ||
             node.Assignments.GroupBy(value => value?.ProducerInstanceId).Any(group => group.Key is null || group.Count() != 1) ||
             node.Assignments.Any(value => value is null || string.IsNullOrWhiteSpace(value.ProducerInstanceId) ||
                 string.IsNullOrWhiteSpace(value.TargetInstanceId) || !node.ParticipantInstanceIds.Contains(value.ProducerInstanceId) ||
                 !node.ParticipantInstanceIds.Contains(value.TargetInstanceId) ||
                 !rules.Heroes.TryGetValue(run.Roster.Single(hero => hero.InstanceId == value.ProducerInstanceId).ContentId, out var producerRule) ||
                 !producerRule.ProductionModes.Contains(value.Mode) ||
                 value.Mode == GrowthProductionMode.Research && value.TargetInstanceId != value.ProducerInstanceId) ||
             string.IsNullOrEmpty(node.ConsumedSpellId) != string.IsNullOrEmpty(node.SpellTargetInstanceId) ||
             !string.IsNullOrEmpty(node.ConsumedSpellId) && (!node.IsBattle || !rules.Spells.ContainsKey(node.ConsumedSpellId) ||
                 !node.ParticipantInstanceIds.Contains(node.SpellTargetInstanceId)))) return false;
        return true;
    }

    private static bool ValidGain(GrowthGainDto? gain) => gain is not null && gain.FloorIndex >= 0 &&
        gain.BattleNumber >= 0 && !string.IsNullOrWhiteSpace(gain.ProducerInstanceId) &&
        !string.IsNullOrWhiteSpace(gain.TargetInstanceId) && Enum.IsDefined(gain.Mode) &&
        gain.AbilityId is not null && gain.Source is not null && float.IsFinite(gain.Amount) && gain.Amount >= 0 &&
        (string.IsNullOrEmpty(gain.Source) && string.IsNullOrEmpty(gain.AbilityId) ||
         gain.Source == "battle" && !string.IsNullOrWhiteSpace(gain.AbilityId));

    private static bool MatchesPermanentOperation(CompiledAbilityLoadout? loadout, BattlePermanentGain gain)
    {
        var ability = loadout?.Find(gain.AbilityId);
        var prefix = gain.AbilityId + ":";
        if (ability is null || !gain.Slot.StartsWith(prefix, StringComparison.Ordinal))
            return false;
        var path = gain.Slot[prefix.Length..].Split(':');
        if (path.Length == 0 || path.Any(part => !int.TryParse(part, out _)) ||
            !int.TryParse(path[0], out var rootIndex) || rootIndex < 0 || rootIndex >= ability.Operations.Length ||
            ability.Operations[rootIndex] is not CompiledMatrixOperation current) return false;
        for (var depth = 1; depth < path.Length; depth++)
        {
            var childIndex = int.Parse(path[depth], System.Globalization.CultureInfo.InvariantCulture);
            if (childIndex < 0 || childIndex >= current.Effects.Length) return false;
            current = current.Effects[childIndex];
        }
        // Sequence executes descendants with its own slot, while registered reactions
        // append concrete child indices. Flatten supports the former after path traversal.
        return MatrixAbilityCompiler.Flatten(current).Any(operation => operation.Kind == MatrixOperationKind.PermanentAttribute &&
            operation.Attribute == gain.Attribute && operation.Amount == gain.Amount);
    }

    private GrowthNodeSnapshotDto Freeze(ActiveRunDto run, bool battle)
    {
        var participants = run.Deployment.Where(id => !string.IsNullOrEmpty(id)).Distinct(StringComparer.Ordinal).ToList();
        var snapshot = new GrowthNodeSnapshotDto
        {
            FloorIndex = run.FloorIndex,
            BattleNumber = run.BattleNumber,
            IsBattle = battle,
            ParticipantInstanceIds = participants
        };
        foreach (var id in participants)
        {
            var hero = run.Roster.Single(candidate => candidate.InstanceId == id);
            if (!rules!.Heroes.TryGetValue(hero.ContentId, out var definition) ||
                !definition.ProductionModes.Contains(hero.Growth.ProductionMode)) continue;
            var target = hero.Growth.ProductionMode == GrowthProductionMode.Research ? id : hero.Growth.ProductionTargetInstanceId;
            if (hero.Growth.ProductionMode != GrowthProductionMode.Research && !participants.Contains(target)) continue;
            snapshot.Assignments.Add(new GrowthAssignmentDto { ProducerInstanceId = id, TargetInstanceId = target, Mode = hero.Growth.ProductionMode });
        }
        run.Growth!.PendingNode = snapshot;
        return snapshot;
    }

    private List<string> DiscoveryCandidates(ActiveRunDto run, string heroId)
    {
        var pool = run.FloorIndex < project.Campaign.FloorsPerRegion ? rules!.FirstDiscoveryPool : rules!.AdvancedDiscoveryPool;
        var owned = run.Roster.Select(hero => hero.ContentId).ToHashSet(StringComparer.Ordinal);
        return pool.Where(id => !owned.Contains(id)).Distinct(StringComparer.Ordinal)
            .OrderBy(id => StableRoll(DiscoveryIdentity(run, heroId) + ":" + id)).Take(3).ToList();
    }

    private static string DiscoveryIdentity(ActiveRunDto run, string heroId) =>
        $"growth:{run.Seed:x16}:{run.FloorIndex}:{run.BattleNumber}:{heroId}";

    private static uint StableRoll(string value) =>
        System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static string? NextHeroInstanceId(ActiveRunDto run)
    {
        for (var sequence = 1; sequence < int.MaxValue; sequence++)
        {
            var id = $"roster-hero-{sequence}";
            if (run.Roster.All(hero => hero.InstanceId != id)) return id;
        }
        return null;
    }

    private static void AddGain(RosterHeroInstanceDto target, GrowthSettlementDto settlement,
        GrowthNodeSnapshotDto snapshot, GrowthAssignmentDto assignment, float amount)
    {
        var gain = new GrowthGainDto { FloorIndex = snapshot.FloorIndex, BattleNumber = snapshot.BattleNumber,
            ProducerInstanceId = assignment.ProducerInstanceId, TargetInstanceId = assignment.TargetInstanceId,
            Mode = assignment.Mode, Amount = amount };
        target.Growth.History.Add(gain);
        settlement.Gains.Add(gain);
    }

    private GrowthCommandResult Commit(ActiveRunDto working, ActiveRunDto authoritative, string success, string failure)
    {
        if (!persistence.ValidateRun(working)) return GrowthCommandResult.Reject("成长结果不符合征程规则，未作修改。");
        return persistence.TryPublish(working, authoritative) ? GrowthCommandResult.Success(success) : GrowthCommandResult.Reject(failure);
    }
}
