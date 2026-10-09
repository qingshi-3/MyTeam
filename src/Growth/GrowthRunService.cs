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

// Owns growth projections. Player commands clone/validate/publish; action lifecycle
// projections edit only the coordinator's detached working copy and never save.
public sealed partial class GrowthRunService(
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
        if (GetPending(run) is { } pending)
            return GrowthCommandResult.Reject(pending.IsBattle ? "本场战斗已经开始，整备状态已冻结。" : "当前节点的生产状态已冻结。");
        if (run.Expedition is not null && (run.Action is not null || growth.PendingNode is not null))
            return GrowthCommandResult.Reject("请先完成当前行动和奖励领取。");
        return GrowthCommandResult.Success();
    }

    public GrowthCommandResult SetAssignment(ActiveRunDto? run, string producerId, string targetId, GrowthProductionMode mode)
    {
        var check = CheckAction(run);
        if (!check.Succeeded) return check;
        var producer = run!.Roster.SingleOrDefault(hero => hero.InstanceId == producerId);
        var target = run.Roster.SingleOrDefault(hero => hero.InstanceId == targetId);
        if (!Enum.IsDefined(mode) || producer is null || target is null || !rules!.Heroes.TryGetValue(producer.ContentId, out var definition) ||
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
        if (run.Expedition is null && (run.PendingOffer is not null || GetPending(run) is not null ||
            run.PendingNode && RunDecisionService.KindFor(run.SelectedNode) is not null))
            return GrowthCommandResult.Reject("升阶只能在未冻结的战斗整备阶段进行。");
        var hero = run.Roster.SingleOrDefault(candidate => candidate.InstanceId == heroId);
        if (hero is null || !rules!.Heroes.TryGetValue(hero.ContentId, out var definition) ||
            string.IsNullOrWhiteSpace(definition.AscensionId) ||
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
        if (run?.Expedition is not null)
            return GrowthCommandResult.Reject("地图战斗必须由行动事务统一提交。");
        if (rules is null) return GrowthCommandResult.Success();
        if (run is null) return GrowthCommandResult.Reject("当前没有可开始的征程。");
        var alreadyFrozen = GetPending(run) is not null;
        var working = persistence.CloneRun(run);
        var result = BeginBattleProjection(working, encounter);
        if (!result.Succeeded || alreadyFrozen) return result;
        return Commit(working, run, "战斗成长状态已冻结。", "保存失败，法术未消耗且战斗未开始。");
    }

    // The map coordinator authenticates the encounter and creates Action.Source
    // before calling this, then validates the final BattleFreeze and saves once.
    // Rejected projections do not consume inventory or install a partial snapshot.
    public GrowthCommandResult BeginBattleProjection(ActiveRunDto working, EncounterPlan encounter)
    {
        if (rules is null) return GrowthCommandResult.Success();
        if (working.Growth is not { } state || state.RulesId != rules.StableId)
            return GrowthCommandResult.Reject("当前征程没有可用的成长状态。");
        if (working.OpeningRecruitment is not null || !string.IsNullOrEmpty(working.TerminalCompletionId))
            return GrowthCommandResult.Reject("当前阶段不能开始战斗。");
        if (state.PendingDiscovery is not null) return GrowthCommandResult.Reject("请先领取发现奖励再开始下一场战斗。");
        if (working.Expedition is not null)
        {
            if (state.PendingNode is not null || !ValidMapSource(RunProgressionContext.Source(working)) ||
                working.Action is null || encounter.NodeType is not (TowerNodeType.Combat or TowerNodeType.Elite or TowerNodeType.Boss))
                return GrowthCommandResult.Reject("当前没有有效的地图战斗行动来源。");
        }
        else
        {
            if (!working.PendingNode || encounter.NodeType != working.SelectedNode || RunDecisionService.KindFor(working.SelectedNode) is not null)
                return GrowthCommandResult.Reject("当前没有可开始的战斗节点。");
            var expected = new TowerGenerator(project.Campaign).Encounter(working, working.SelectedNode);
            if (encounter.EncounterId != expected.EncounterId || encounter.Title != expected.Title ||
                encounter.FloorRuleId != expected.FloorRuleId || encounter.IsBoss != expected.IsBoss ||
                encounter.IsElite != expected.IsElite || !encounter.EnemyIds.SequenceEqual(expected.EnemyIds) ||
                encounter.CompositionId != expected.CompositionId || !encounter.EnemyCells.SequenceEqual(expected.EnemyCells))
                return GrowthCommandResult.Reject("战斗计划与当前节点不匹配。");
        }
        if (GetPending(working) is { } frozen)
            return frozen.IsBattle && MatchesCurrentAction(working, frozen) && frozen.BattleNumber == working.BattleNumber
                ? GrowthCommandResult.Success("本场战斗已冻结，可安全重试启动。")
                : GrowthCommandResult.Reject("当前节点已有不匹配的成长快照。");
        var snapshot = Freeze(working, true);
        if (working.Expedition is not null && AlreadySettled(working, snapshot))
            return GrowthCommandResult.Reject("当前行动已经结算，不能重复冻结成长或消耗法术。");
        var selected = state.EquippedSpellId;
        if (!string.IsNullOrEmpty(selected))
        {
            if (!rules.Spells.ContainsKey(selected) || state.SpellInventory.GetValueOrDefault(selected) <= 0 ||
                !snapshot.ParticipantInstanceIds.Contains(state.SpellTargetInstanceId))
                return GrowthCommandResult.Reject("法术库存或目标已失效，请重新选择。");
            state.SpellInventory[selected]--;
            snapshot.ConsumedSpellId = selected;
            snapshot.SpellTargetInstanceId = state.SpellTargetInstanceId;
            state.EquippedSpellId = string.Empty;
            state.SpellTargetInstanceId = string.Empty;
        }
        SetPending(working, snapshot);
        return GrowthCommandResult.Success("战斗成长状态已冻结。");
    }

    public bool FreezeNonCombat(ActiveRunDto working)
    {
        if (rules is null || working.Growth is null) return true;
        if (working.Growth.PendingDiscovery is not null || GetPending(working) is not null) return false;
        if (working.Expedition is not null && (working.Growth.PendingNode is not null ||
            working.Action is null || !ValidMapSource(RunProgressionContext.Source(working)))) return false;
        var snapshot = Freeze(working, false);
        if (working.Expedition is not null && AlreadySettled(working, snapshot)) return false;
        SetPending(working, snapshot);
        return true;
    }

    public bool SettleNode(ActiveRunDto working)
    {
        if (rules is null || working.Growth is null) return true;
        var snapshot = GetPending(working);
        if (snapshot is null || !MatchesCurrentAction(working, snapshot) || snapshot.BattleNumber > working.BattleNumber ||
            AlreadySettled(working, snapshot) || !ValidAssignments(working, snapshot) || !ValidBreedingSnapshot(working, snapshot)) return false;
        var breedingAttack = 0f;
        var breedingHealth = 0f;
        RosterHeroInstanceDto? breeder = null;
        CompiledGrowthHero? breedingRule = null;
        if (snapshot.Breeding is { } breeding)
        {
            breeder = working.Roster.Single(hero => hero.InstanceId == breeding.ProducerInstanceId);
            breedingRule = rules.Heroes[breeder.ContentId];
            if ((long)breeding.Points + breedingRule.BreedingYield > int.MaxValue) return false;
            breedingAttack = breeding.Points * breedingRule.AttackGainPerBreedingPoint;
            breedingHealth = breeding.Points * breedingRule.MaxHealthGainPerBreedingPoint;
            if (!float.IsFinite(breedingAttack) || !float.IsFinite(breedingHealth)) return false;
        }
        var settlement = new GrowthSettlementDto
        {
            Source = snapshot.Source,
            FloorIndex = snapshot.FloorIndex,
            BattleNumber = snapshot.BattleNumber,
            IsBattle = snapshot.IsBattle,
            MaterialsGranted = rules.MaterialsPerNode
        };
        var battleGainKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var gain in working.Roster.SelectMany(hero => hero.Growth.History).Where(gain =>
                     SameGainAction(working, gain, snapshot.Source, snapshot.FloorIndex, snapshot.BattleNumber) && gain.Source is "battle" or "remembered"))
        {
            var key = $"{gain.Source}:{gain.ProducerInstanceId}:{gain.TargetInstanceId}:{gain.AbilityId}:{gain.OperationKey}:{gain.GrantOrdinal}:{gain.Mode}:{gain.Amount:R}";
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
            // A cultivation assignment is one frozen action entitlement. Both fixed
            // attributes are projected into the same detached transaction and save.
            var (attack, health) = assignment.Mode switch
            {
                GrowthProductionMode.Attack => (definition.AttackDamage * producerRule.GrowthRate, 0f),
                GrowthProductionMode.Vitality => (0f, definition.MaxHealth * producerRule.GrowthRate),
                GrowthProductionMode.Cultivation => (producerRule.FixedAttackGain + breedingAttack, producerRule.FixedMaxHealthGain + breedingHealth),
                _ => throw new InvalidOperationException("不支持的成长生产方式。")
            };
            var nextAttack = target.Growth.AddedAttack + attack;
            var nextHealth = target.Growth.AddedMaxHealth + health;
            if (!float.IsFinite(nextAttack) || !float.IsFinite(nextHealth) || attack < 0 || health < 0) return false;
            target.Growth.AddedAttack = nextAttack;
            target.Growth.AddedMaxHealth = nextHealth;
            if (attack > 0) AddGain(target, settlement, snapshot, assignment, attack, GrowthProductionMode.Attack);
            if (health > 0) AddGain(target, settlement, snapshot, assignment, health, GrowthProductionMode.Vitality);
        }
        // The current seed uses the committed old points. Increasing breeding
        // happens after all assignments, in this same detached transaction.
        if (breeder is not null && breedingRule is not null)
        {
            settlement.Breeding = new GrowthBreedingGainDto
            {
                ProducerInstanceId = breeder.InstanceId, PreviousPoints = snapshot.Breeding!.Points,
                Amount = breedingRule.BreedingYield
            };
            breeder.Growth.BreedingPoints += breedingRule.BreedingYield;
        }
        working.Growth.Materials = checked(working.Growth.Materials + rules.MaterialsPerNode);
        if (working.Expedition is null) working.Growth.LastSettledFloorIndex = snapshot.FloorIndex;
        working.Growth.History.Add(settlement);
        SetPending(working, null);
        return true;
    }

    public bool ApplyBattlePermanentGains(ActiveRunDto working,
        ImmutableArray<BattlePermanentGain> gains, bool apply)
    {
        if (gains.IsDefaultOrEmpty) return true;
        if (rules is null || GetPending(working) is not { IsBattle: true } snapshot ||
            working.Expedition is not null && (!MatchesCurrentAction(working, snapshot) || AlreadySettled(working, snapshot))) return false;
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
            var loadout = definition.ResolveBattleLoadout(!string.IsNullOrEmpty(hero.Growth.AscensionId));
            if (!MatchesPermanentOperation(loadout, gain)) return false;
            if (!apply) continue;
            if (gain.Attribute == CombatAttribute.AttackDamage) hero.Growth.AddedAttack += gain.Amount;
            else hero.Growth.AddedMaxHealth += gain.Amount;
            hero.Growth.History.Add(new GrowthGainDto
            {
                ActionSource = snapshot.Source,
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
            !Enum.IsDefined(hero.Growth.ProductionMode) || hero.Growth.ProductionTargetInstanceId is null || hero.Growth.History is null || hero.Growth.History.Any(gain => gain is null) ||
            hero.Growth.BreedingPoints < 0 || !float.IsFinite(hero.Growth.AddedAttack) || hero.Growth.AddedAttack < 0 ||
            !float.IsFinite(hero.Growth.AddedMaxHealth) || hero.Growth.AddedMaxHealth < 0)) return false;
        if (run.Growth is null) return rules is null;
        var growth = run.Growth;
        var map = run.Expedition is not null;
        if (rules is null || growth.RulesId is null || growth.EquippedSpellId is null ||
            growth.SpellTargetInstanceId is null || growth.RulesId != rules.StableId || growth.Materials < 0 || growth.Research < 0 ||
            (map ? growth.PendingNode is not null || growth.LastSettledFloorIndex != -1 :
                growth.LastSettledFloorIndex < -1 || growth.LastSettledFloorIndex > run.FloorIndex) ||
            growth.CategoryMaterials is null || growth.SpellInventory is null || growth.History is null ||
            growth.History.Any(value => value is null || !ValidSettlement(run, value)) ||
            growth.CategoryMaterials.Any(pair => string.IsNullOrWhiteSpace(pair.Key) || pair.Value < 0) ||
            growth.SpellInventory.Any(pair => !rules.Spells.ContainsKey(pair.Key) || pair.Value < 0)) return false;
        if (map)
        {
            if (project.Campaign.Progression is not CompiledExpedition expedition ||
                growth.History.Any(value => value.Source!.StageIndex >= expedition.Stages.Length) ||
                growth.History.GroupBy(value => value.Source!.ActionId, StringComparer.Ordinal).Any(group => group.Count() != 1) ||
                growth.History.GroupBy(value => value.Source!.Sequence).Any(group => group.Count() != 1)) return false;
            for (var index = 1; index < growth.History.Count; index++)
                if (growth.History[index - 1].Source!.Sequence >= growth.History[index].Source!.Sequence ||
                    growth.History[index - 1].Source!.StageIndex > growth.History[index].Source!.StageIndex) return false;
            if (RunProgressionContext.Source(run) is { } current &&
                growth.History.Any(value => value.Source!.Sequence > current.Sequence || value.Source.StageIndex > current.StageIndex)) return false;
        }
        else if (growth.History.GroupBy(value => value.FloorIndex).Any(group => group.Count() != 1)) return false;

        if (!ValidBreedingHistory(run) || !ValidRememberedHistory(run)) return false;

        var ids = run.Roster.Select(hero => hero.InstanceId).ToHashSet(StringComparer.Ordinal);
        if (string.IsNullOrEmpty(growth.EquippedSpellId) != string.IsNullOrEmpty(growth.SpellTargetInstanceId) ||
            !string.IsNullOrEmpty(growth.EquippedSpellId) &&
             (!rules.Spells.ContainsKey(growth.EquippedSpellId) ||
             growth.SpellInventory.GetValueOrDefault(growth.EquippedSpellId) <= 0 ||
             !ids.Contains(growth.SpellTargetInstanceId) || !run.Deployment.Contains(growth.SpellTargetInstanceId))) return false;
        foreach (var hero in run.Roster)
        {
            if (hero.Growth.History.Any(gain => !ValidGain(run, gain) ||
                map && !growth.History.Any(settlement =>
                    SameGainAction(run, gain, settlement.Source, settlement.FloorIndex, settlement.BattleNumber) &&
                    settlement.Gains.Any(recorded => SameGain(recorded, gain))))) return false;
            if (!rules.Heroes.TryGetValue(hero.ContentId, out var definition))
            {
                if (!string.IsNullOrEmpty(hero.Growth.AscensionId)) return false;
                continue;
            }
            if (!string.IsNullOrEmpty(hero.Growth.AscensionId) && hero.Growth.AscensionId != definition.AscensionId) return false;
            if (!definition.ProductionModes.Contains(hero.Growth.ProductionMode) &&
                !string.IsNullOrEmpty(hero.Growth.ProductionTargetInstanceId)) return false;
            if (!string.IsNullOrEmpty(hero.Growth.ProductionTargetInstanceId) && !ids.Contains(hero.Growth.ProductionTargetInstanceId)) return false;
        }
        var correctPool = RunProgressionContext.IsAdvancedDiscovery(run, project) ? rules.AdvancedDiscoveryPool : rules.FirstDiscoveryPool;
        if (growth.PendingDiscovery is { } discovery &&
            (string.IsNullOrWhiteSpace(discovery.OfferId) || string.IsNullOrWhiteSpace(discovery.UpgradedHeroInstanceId) ||
             !ids.Contains(discovery.UpgradedHeroInstanceId) ||
             discovery.OfferId != DiscoveryIdentity(run, discovery.UpgradedHeroInstanceId) ||
             discovery.CandidateIds is null || discovery.CandidateIds.Count != 3 || discovery.CandidateIds.Distinct().Count() != 3 ||
             discovery.CandidateIds.Any(id => string.IsNullOrWhiteSpace(id) || !correctPool.Contains(id) ||
                 run.Roster.Any(hero => hero.ContentId == id)) ||
             (long)run.Roster.Count >= (long)run.CurrentPopulation + project.RunRules.ReserveCapacity ||
             run.Roster.Single(hero => hero.InstanceId == discovery.UpgradedHeroInstanceId).Growth.AscensionId.Length == 0)) return false;
        if (GetPending(run) is { } node &&
            (!ValidBreedingSnapshot(run, node) || node.ParticipantInstanceIds is null || node.Assignments is null || node.ConsumedSpellId is null ||
             node.SpellTargetInstanceId is null || !MatchesCurrentAction(run, node) || node.BattleNumber != run.BattleNumber ||
             map && AlreadySettled(run, node) ||
             !map && node.IsBattle != (RunDecisionService.KindFor(run.SelectedNode) is null) ||
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

    private static bool ValidGain(ActiveRunDto run, GrowthGainDto? gain) => gain is not null &&
        (run.Expedition is not null ? ValidMapSource(gain.ActionSource) : gain.FloorIndex >= 0 && gain.FloorIndex <= run.FloorIndex) &&
        gain.BattleNumber >= 0 && gain.BattleNumber <= run.BattleNumber && !string.IsNullOrWhiteSpace(gain.ProducerInstanceId) &&
        !string.IsNullOrWhiteSpace(gain.TargetInstanceId) &&
        (gain.Mode is GrowthProductionMode.Attack or GrowthProductionMode.Vitality or GrowthProductionMode.Research) &&
        gain.AbilityId is not null && gain.Source is not null && float.IsFinite(gain.Amount) && gain.Amount >= 0 &&
        (string.IsNullOrEmpty(gain.Source) && string.IsNullOrEmpty(gain.AbilityId) ||
         gain.Source is "battle" or "remembered" && !string.IsNullOrWhiteSpace(gain.AbilityId)) &&
        (gain.Source == "remembered" ? ValidRememberedMetadata(gain) :
            gain.OperationKey == "" && gain.RememberAbilityId == "" && gain.RememberOperationKey == "" &&
            gain.GrantOrdinal == 0 && gain.GrantedTick == 0);

    private static bool ValidSettlement(ActiveRunDto run, GrowthSettlementDto settlement) =>
        (run.Expedition is not null ? ValidMapSource(settlement.Source) : settlement.FloorIndex >= 0 && settlement.FloorIndex <= run.FloorIndex) &&
        settlement.BattleNumber >= 0 && settlement.BattleNumber <= run.BattleNumber &&
        settlement.MaterialsGranted >= 0 && settlement.Gains is not null && settlement.Gains.All(gain =>
            ValidGain(run, gain) && (run.Expedition is null ||
                SameGainAction(run, gain, settlement.Source, settlement.FloorIndex, settlement.BattleNumber) &&
                (gain.Source is not ("battle" or "remembered") || settlement.IsBattle)));

    private static bool SameGain(GrowthGainDto first, GrowthGainDto second) =>
        first.ActionSource == second.ActionSource && first.BattleNumber == second.BattleNumber &&
        first.ProducerInstanceId == second.ProducerInstanceId && first.TargetInstanceId == second.TargetInstanceId &&
        first.Mode == second.Mode && first.Amount == second.Amount && first.AbilityId == second.AbilityId && first.Source == second.Source &&
        first.OperationKey == second.OperationKey && first.RememberAbilityId == second.RememberAbilityId &&
        first.RememberOperationKey == second.RememberOperationKey && first.GrantOrdinal == second.GrantOrdinal &&
        first.GrantedTick == second.GrantedTick;

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

    public static GrowthNodeSnapshotDto? GetPending(ActiveRunDto run) =>
        run.Expedition is not null ? run.Action?.GrowthSnapshot : run.Growth?.PendingNode;

    public static void SetPending(ActiveRunDto run, GrowthNodeSnapshotDto? snapshot)
    {
        if (run.Expedition is null)
        {
            if (run.Growth is not null) run.Growth.PendingNode = snapshot;
            else if (snapshot is not null) throw new InvalidOperationException("A growth snapshot requires growth state.");
            return;
        }
        if (run.Growth?.PendingNode is not null)
            throw new InvalidOperationException("An expedition cannot own a legacy growth snapshot.");
        if (run.Action is { } action) action.GrowthSnapshot = snapshot;
        else if (snapshot is not null) throw new InvalidOperationException("An expedition growth snapshot requires an action owner.");
    }

    private static bool ValidMapSource(RunActionSource? source) => source is not null &&
        !string.IsNullOrWhiteSpace(source.ActionId) && source.Sequence > 0 && source.StageIndex >= 0 &&
        source.SiteId is not null && source.LegacyFloorIndex is null;

    private static bool MatchesCurrentAction(ActiveRunDto run, GrowthNodeSnapshotDto snapshot) =>
        run.Expedition is not null
            ? ValidMapSource(snapshot.Source) && snapshot.Source == RunProgressionContext.Source(run)
            : snapshot.FloorIndex == run.FloorIndex;

    private static bool AlreadySettled(ActiveRunDto run, GrowthNodeSnapshotDto snapshot) =>
        run.Expedition is not null
            ? snapshot.Source is not { } source || run.Growth!.History.Any(settlement =>
                settlement.Source is { } previous && (previous.ActionId == source.ActionId || previous.Sequence >= source.Sequence))
            : run.Growth!.LastSettledFloorIndex >= snapshot.FloorIndex;

    private static bool SameGainAction(ActiveRunDto run, GrowthGainDto gain, RunActionSource? source,
        int legacyFloorIndex, int battleNumber) => gain.BattleNumber == battleNumber &&
        (run.Expedition is not null ? source is not null && gain.ActionSource == source : gain.FloorIndex == legacyFloorIndex);

    private bool ValidAssignments(ActiveRunDto run, GrowthNodeSnapshotDto snapshot) =>
        snapshot.ParticipantInstanceIds is not null && snapshot.Assignments is not null &&
        snapshot.Assignments.All(assignment => assignment is not null && Enum.IsDefined(assignment.Mode) &&
            snapshot.ParticipantInstanceIds.Contains(assignment.ProducerInstanceId) &&
            snapshot.ParticipantInstanceIds.Contains(assignment.TargetInstanceId) &&
            run.Roster.FirstOrDefault(hero => hero.InstanceId == assignment.ProducerInstanceId) is { } producer &&
            rules!.Heroes.TryGetValue(producer.ContentId, out var definition) &&
            definition.ProductionModes.Contains(assignment.Mode) &&
            (assignment.Mode != GrowthProductionMode.Research || assignment.TargetInstanceId == assignment.ProducerInstanceId)) &&
        snapshot.Assignments.Select(assignment => assignment.ProducerInstanceId).Distinct(StringComparer.Ordinal).Count() == snapshot.Assignments.Count;

    private bool ValidBreedingSnapshot(ActiveRunDto run, GrowthNodeSnapshotDto snapshot)
    {
        if (snapshot.ParticipantInstanceIds is null) return false;
        var eligible = run.Roster.Where(hero => snapshot.ParticipantInstanceIds.Contains(hero.InstanceId) &&
            rules!.Heroes.TryGetValue(hero.ContentId, out var definition) && definition.BreedingYield > 0).ToArray();
        if (eligible.Length == 0) return snapshot.Breeding is null;
        return eligible.Length == 1 && snapshot.Breeding is { Points: >= 0 } breeding &&
            breeding.ProducerInstanceId == eligible[0].InstanceId && breeding.Points == eligible[0].Growth.BreedingPoints;
    }

    private bool ValidBreedingHistory(ActiveRunDto run)
    {
        var totals = new Dictionary<string, long>(StringComparer.Ordinal);
        foreach (var settlement in run.Growth!.History)
        {
            if (settlement.Breeding is not { } gain) continue;
            if (string.IsNullOrWhiteSpace(gain.ProducerInstanceId) || gain.PreviousPoints < 0 || gain.Amount <= 0 ||
                run.Roster.FirstOrDefault(hero => hero.InstanceId == gain.ProducerInstanceId) is not { } hero ||
                !rules!.Heroes.TryGetValue(hero.ContentId, out var rule) || rule.BreedingYield != gain.Amount ||
                totals.GetValueOrDefault(hero.InstanceId) != gain.PreviousPoints) return false;
            var next = (long)gain.PreviousPoints + gain.Amount;
            if (next > int.MaxValue) return false;
            totals[hero.InstanceId] = next;
        }
        return run.Roster.All(hero => hero.Growth.BreedingPoints == totals.GetValueOrDefault(hero.InstanceId) &&
            (hero.Growth.BreedingPoints == 0 || rules!.Heroes.TryGetValue(hero.ContentId, out var rule) && rule.BreedingYield > 0));
    }

    private GrowthNodeSnapshotDto Freeze(ActiveRunDto run, bool battle)
    {
        var participants = run.Deployment.Where(id => !string.IsNullOrEmpty(id)).Distinct(StringComparer.Ordinal).ToList();
        var snapshot = new GrowthNodeSnapshotDto
        {
            Source = RunProgressionContext.Source(run),
            FloorIndex = run.Expedition is null ? run.FloorIndex : -1,
            BattleNumber = run.BattleNumber,
            IsBattle = battle,
            ParticipantInstanceIds = participants
        };
        foreach (var id in participants)
        {
            var hero = run.Roster.Single(candidate => candidate.InstanceId == id);
            if (rules!.Heroes.TryGetValue(hero.ContentId, out var breedingRule) && breedingRule.BreedingYield > 0)
            {
                if (snapshot.Breeding is not null) throw new InvalidOperationException("一次行动不能冻结多个育种来源。");
                snapshot.Breeding = new GrowthBreedingSnapshotDto { ProducerInstanceId = id, Points = hero.Growth.BreedingPoints };
            }
            if (!rules!.Heroes.TryGetValue(hero.ContentId, out var definition) ||
                !definition.ProductionModes.Contains(hero.Growth.ProductionMode)) continue;
            var target = hero.Growth.ProductionMode == GrowthProductionMode.Research ? id : hero.Growth.ProductionTargetInstanceId;
            if (hero.Growth.ProductionMode != GrowthProductionMode.Research && !participants.Contains(target)) continue;
            snapshot.Assignments.Add(new GrowthAssignmentDto { ProducerInstanceId = id, TargetInstanceId = target, Mode = hero.Growth.ProductionMode });
        }
        return snapshot;
    }

    private List<string> DiscoveryCandidates(ActiveRunDto run, string heroId)
    {
        var pool = RunProgressionContext.IsAdvancedDiscovery(run, project) ? rules!.AdvancedDiscoveryPool : rules!.FirstDiscoveryPool;
        var owned = run.Roster.Select(hero => hero.ContentId).ToHashSet(StringComparer.Ordinal);
        return pool.Where(id => !owned.Contains(id)).Distinct(StringComparer.Ordinal)
            .OrderBy(id => StableRoll(DiscoveryIdentity(run, heroId) + ":" + id)).Take(3).ToList();
    }

    private static string DiscoveryIdentity(ActiveRunDto run, string heroId) =>
        run.Expedition is null ? $"growth:{run.Seed:x16}:{run.FloorIndex}:{run.BattleNumber}:{heroId}" :
        $"growth:{RunProgressionContext.Identity(run)}:{heroId}";

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
        GrowthNodeSnapshotDto snapshot, GrowthAssignmentDto assignment, float amount, GrowthProductionMode? gainMode = null)
    {
        var gain = new GrowthGainDto { ActionSource = snapshot.Source, FloorIndex = snapshot.FloorIndex, BattleNumber = snapshot.BattleNumber,
            ProducerInstanceId = assignment.ProducerInstanceId, TargetInstanceId = assignment.TargetInstanceId,
            Mode = gainMode ?? assignment.Mode, Amount = amount };
        target.Growth.History.Add(gain);
        settlement.Gains.Add(gain);
    }

    private GrowthCommandResult Commit(ActiveRunDto working, ActiveRunDto authoritative, string success, string failure)
    {
        if (!persistence.ValidateRun(working)) return GrowthCommandResult.Reject("成长结果不符合征程规则，未作修改。");
        return persistence.TryPublish(working, authoritative) ? GrowthCommandResult.Success(success) : GrowthCommandResult.Reject(failure);
    }
}
