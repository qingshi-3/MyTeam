import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const here = path.dirname(fileURLToPath(import.meta.url));
const config = JSON.parse(fs.readFileSync(path.join(here, "model-config.json"), "utf8"));
const round = (value, digits = 6) => Number(value.toFixed(digits));
const assert = (condition, message) => { if (!condition) throw new Error(message); };

function availableCount(floor, producerJoinAfterNode, targetJoinAfterNode) {
  return Math.max(0, floor - 1 - Math.max(producerJoinAfterNode, targetJoinAfterNode));
}

function growthTables() {
  const timing = [];
  for (const floor of config.growth.checkFloors) {
    for (const targetJoinAfterNode of [0, ...config.growth.lateJoinAfterNodes]) {
      for (const producerJoinAfterNode of [0, 2, 7]) {
        const n = availableCount(floor, producerJoinAfterNode, targetJoinAfterNode);
        for (const rate of config.growth.rates) {
          for (const sources of config.growth.producerCounts) {
            const targetAvailable = floor > targetJoinAfterNode;
            const producerAvailable = sources === 0 || floor > producerJoinAfterNode;
            const eligible = targetAvailable && producerAvailable;
            timing.push({ floor, targetJoinAfterNode, producerJoinAfterNode, n, rate,
              sources, targetAvailable, producerAvailable, eligible,
              eligibilityNote: eligible ? "deployable_prediction" : !targetAvailable
                ? "target_not_yet_joined" : "producer_not_yet_joined",
              additiveMultiplier: eligible ? round(1 + rate * sources * n) : null });
          }
        }
      }
    }
  }
  const namedCounts = Object.entries(config.growth.namedProducerJoinAfterNodes).map(([name, joinAfterNode]) => ({
    name, joinAfterNode, usableCountAtFloor15: availableCount(15, joinAfterNode, 0)
  }));
  const compoundingDanger = [];
  for (const rate of config.growth.rates) for (const n of [4, 9, 14]) {
    compoundingDanger.push({ rate, n,
      additiveOneSource: round(1 + rate * n),
      compoundOneSource: round((1 + rate) ** n),
      additiveThreeSources: round(1 + 3 * rate * n),
      compoundThreeSourcesCombinedPerRound: round((1 + 3 * rate) ** n),
      compoundThreeSourcesSequential: round((1 + rate) ** (3 * n))
    });
  }
  const fixedWindowDamageProxy = [];
  const N = config.growth.fixedWindowProxy.teamSize;
  for (const n of config.growth.fixedWindowProxy.completedWindows)
    for (const sources of [1, 2, 3])
      for (const q of config.growth.fixedWindowProxy.producerOutputFractions)
        for (const w of config.growth.fixedWindowProxy.coreWeights) {
          const D = (N - 1 - sources + sources * q + w * (1 + 0.04 * sources * n)) / (N - 1 + w);
          fixedWindowDamageProxy.push({ n, sources, producerOutputFraction: q, coreWeight: w, ratio: round(D) });
        }
  const lateJoinBaseRequirement = config.growth.lateJoinAfterNodes.map(joinAfterNode => ({
    joinAfterNode,
    requiredBaseRatio: round((1 + 0.04 * 14) / (1 + 0.04 * (14 - joinAfterNode)))
  }));
  return { timing, namedCounts, compoundingDanger, fixedWindowDamageProxy, lateJoinBaseRequirement };
}

function defenseTable() {
  const { maxHealth: hp, armor, armorCoefficient: c, increase } = config.defenseExample;
  const ehp = (health, defense) => health * (1 + c * defense);
  return [
    { case: "baseline", hp, armor, ehp: round(ehp(hp, armor)) },
    { case: "hp_plus_56_percent", hp: hp * (1 + increase), armor, ehp: round(ehp(hp * (1 + increase), armor)) },
    { case: "armor_plus_56_percent", hp, armor: armor * (1 + increase), ehp: round(ehp(hp, armor * (1 + increase))) },
    { case: "both_plus_56_percent", hp: hp * (1 + increase), armor: armor * (1 + increase), ehp: round(ehp(hp * (1 + increase), armor * (1 + increase))) }
  ];
}

const regularFloors = [1,2,3,4,6,7,8,9,11,12,13,14];
const kindAt = regularFloors.map((floor, i) => ({ floor, kind: config.route.regularPatternPerRegion[i % 4] }));

function enumerateRoutes() {
  const aggregate = new Map();
  const choices = [];
  let total = 0;
  function visit(index, recruits, optionalBattles) {
    if (index === kindAt.length) {
      total++;
      const battleCount = optionalBattles + 3;
      const usableBattleClock = optionalBattles + 2;
      const obtainableItemChoiceRewards = optionalBattles + 2;
      const key = `${recruits}|${battleCount}|${usableBattleClock}|${obtainableItemChoiceRewards}`;
      aggregate.set(key, (aggregate.get(key) ?? 0) + 1);
      return;
    }
    for (const choice of config.route[kindAt[index].kind]) {
      choices[index] = choice;
      visit(index + 1, recruits + (choice === "recruitment" ? 1 : 0),
        optionalBattles + (choice === "combat" || choice === "elite" ? 1 : 0));
    }
  }
  visit(0, 0, 0);
  const distribution = [...aggregate.entries()].map(([key, count]) => {
    const [recruitmentCount, battleCount, usableBattleClock, obtainableItemChoiceRewards] = key.split("|").map(Number);
    return { recruitmentCount, battleCount, usableBattleClock, obtainableItemChoiceRewards, itemChoiceKind: "equipment_or_relic", pathCount: count,
      pathRatio: round(count / total, 10) };
  }).sort((a,b) => a.recruitmentCount-b.recruitmentCount || a.battleCount-b.battleCount);
  return { totalPaths: total, distribution };
}

function materialSensitivity() {
  return config.materials.costSensitivity.map(cost => {
    let held = 0; const upgradeAfterFloors = [];
    for (let floor=1; floor<=config.usableCompletionFloors; floor++) {
      held += config.materials.grantPerUsableCompletedNode;
      if (held >= cost) { held -= cost; upgradeAfterFloors.push(floor); }
    }
    return { cost, upgradeCount: upgradeAfterFloors.length, upgradeAfterFloors, endingMaterial: held,
      grantedMaterial: config.usableCompletionFloors };
  });
}

function scenario(name, selector, pattern = config.route.regularPatternPerRegion) {
  let gold = config.route.startingGold, material = 0, roster = config.route.openingRoster;
  let itemChoiceRewards = 0, usableBattleClock = 0, growthOpportunities = 0;
  const rows = [];
  for (let floor=1; floor<=config.floors; floor++) {
    const terminal = floor === config.floors;
    let node = "boss";
    if (!config.route.bossFloors.includes(floor)) {
      const regularIndex = regularFloors.indexOf(floor);
      node = selector(floor, pattern[regularIndex % 4]);
    }
    let goldGain = 0, recruited = 0, upgraded = 0;
    if (!terminal) {
      if (node === "combat") { goldGain=7; usableBattleClock++; itemChoiceRewards++; }
      else if (node === "elite") { goldGain=12; usableBattleClock++; itemChoiceRewards++; }
      else if (node === "boss") { goldGain=18; usableBattleClock++; itemChoiceRewards++; }
      else if (node === "event") goldGain=6;
      else if (node === "rest") goldGain=8;
      else if (node === "recruitment") { recruited=1; roster++; }
      gold += goldGain;
      material++;
      growthOpportunities++;
      if (material >= 4) { material -= 4; roster++; upgraded=1; }
    }
    rows.push({ floor, node, terminalSettlementExcluded: terminal, goldGainUsable: goldGain, gold,
      material, recruited, upgradedHighTierUnits: upgraded, roster,
      deployed: Math.min(roster, config.route.deploymentCap), reserve: Math.max(0, roster-config.route.deploymentCap),
      itemChoiceRewards, itemChoiceKind: "equipment_or_relic", growthOpportunities, usableBattleClock });
  }
  return { name, rows };
}

function routeScenarios() {
  return [
    scenario("all_R_recruitment_B_combat", (_floor, kind) => kind === "B" ? "combat" : "recruitment"),
    scenario("miss_recruitment_at_floors_7_and_12", (floor, kind) => kind === "B" ? "combat" : ([7,12].includes(floor) ? "rest" : "recruitment")),
    scenario("all_R_elite_B_combat", (_floor, kind) => kind === "B" ? "combat" : "elite"),
    scenario("minimum_battles_B_safe_event_R_recruitment", (_floor, kind) => kind === "B" ? "event" : "recruitment")
  ];
}

function patternSensitivity() {
  return config.route.permutationSensitivityPatterns.map(pattern => {
    const allRecruit = scenario(`pattern_${pattern.join("")}_all_R_recruitment`, (_floor, kind) => kind === "B" ? "combat" : "recruitment", pattern);
    const allBattle = scenario(`pattern_${pattern.join("")}_all_R_elite`, (_floor, kind) => kind === "B" ? "combat" : "elite", pattern);
    const checkpoints = rows => [5,10,15].map(floor => {
      const row = rows[floor-2];
      return { beforeBossFloor: floor, benefitsThroughNode: floor-1, gold: row.gold, roster: row.roster,
        deployed: row.deployed, reserve: row.reserve, itemChoiceRewards: row.itemChoiceRewards,
        growthOpportunities: row.growthOpportunities, usableBattleClock: row.usableBattleClock };
    });
    return { pattern, allRecruitCheckpoints: checkpoints(allRecruit.rows), allBattleCheckpoints: checkpoints(allBattle.rows) };
  });
}

const results = {
  status: config.status,
  assumptions: {
    noStepRounding: true,
    benefitsApplyNextFloor: true,
    departureRosterFrozenBeforeNode: true,
    finalSettlementBenefitsExcluded: true,
    routeRatiosAreCombinationCountsNotPlayerProbabilities: true,
    fixedWindowProxyIsNotPowerOrWinRate: true
  },
  growth: growthTables(),
  defense: defenseTable(),
  routes: enumerateRoutes(),
  materials: materialSensitivity(),
  scenarios: routeScenarios(),
  patternSensitivity: patternSensitivity()
};

assert(results.routes.totalPaths === 3 ** 12, "route enumeration count mismatch");
assert(results.routes.distribution.reduce((s,x)=>s+x.pathCount,0) === results.routes.totalPaths, "route aggregation mismatch");
assert(Math.min(...results.routes.distribution.map(x=>x.battleCount)) === 3, "minimum battle count mismatch");
assert(Math.max(...results.routes.distribution.map(x=>x.battleCount)) === 15, "maximum battle count mismatch");
for (const m of results.materials) assert(m.upgradeCount*m.cost+m.endingMaterial===m.grantedMaterial,"material conservation failed");
for (const s of results.scenarios) for (const row of s.rows) assert(row.deployed<=10,"deployment cap exceeded");
for (const s of results.scenarios) assert(s.rows.at(-1).goldGainUsable===0 && s.rows.at(-1).terminalSettlementExcluded,"final benefit exclusion failed");
for (const s of results.scenarios) assert(s.rows.at(-1).growthOpportunities===14,"growth opportunity count mismatch");
const minimumBattleScenario = results.scenarios.find(s=>s.name.startsWith("minimum_battles"));
assert(minimumBattleScenario.rows.at(-1).gold===88 && minimumBattleScenario.rows.at(-1).itemChoiceRewards===2 && minimumBattleScenario.rows.at(-1).usableBattleClock===2,"minimum battle scenario mismatch");
for (const sensitivity of results.patternSensitivity) for (const checkpoint of [...sensitivity.allRecruitCheckpoints, ...sensitivity.allBattleCheckpoints])
  assert(checkpoint.benefitsThroughNode===checkpoint.beforeBossFloor-1,"Boss pre-fight checkpoint includes current Boss reward");
for (const row of results.growth.timing) {
  assert(row.eligible === (row.targetAvailable && row.producerAvailable), "growth timing eligibility mismatch");
  assert((row.eligible && row.additiveMultiplier !== null) || (!row.eligible && row.additiveMultiplier === null), "ineligible growth prediction leaked multiplier");
}

fs.writeFileSync(path.join(here,"model-results.json"), JSON.stringify(results,null,2)+"\n");

const dist = results.routes.distribution;
const countBy = key => Object.entries(Object.groupBy(dist, x=>x[key])).map(([value, rows]) => ({value:Number(value),count:rows.reduce((s,x)=>s+x.pathCount,0)}));
const scenarioLast = results.scenarios.map(s=>({name:s.name,...s.rows.at(-1)}));
const md = `# 成长与供给候选算例\n\n本报告由 \`model.mjs\` 根据 \`model-config.json\` 确定性生成。所有参数都是候选假设，不是已定规则、战力或平衡结论。原始宽表见 \`model-results.json\`。\n\n## 核心数值\n\n- 4% 原始基础项加法：完整源14次为 1.56 倍，join2早源12次为1.48倍，join7中源7次为1.28倍。0源或刚加入且无既往次数均为1倍。\n- 复利危险对照（4%、14轮、3源）：加法2.68倍；同轮合并 \`(1+3r)^n\`=${round((1.12)**14)}倍；逐源 \`(1+r)^(3n)\`=${round((1.04)**42)}倍。\n- 护卫基线EHP为${results.defense[0].ehp}；HP单加56%=${results.defense[1].ehp}，防御单加56%=${results.defense[2].ehp}，两者都加56%=${results.defense[3].ehp}。SpellPower默认0，百分比无法从0产生增量。\n- 晚入单位达到完整14次4%成长后基准所需倍率：join4 ${results.growth.lateJoinBaseRequirement[0].requiredBaseRatio}，join9 ${results.growth.lateJoinBaseRequirement[1].requiredBaseRatio}，join12 ${results.growth.lateJoinBaseRequirement[2].requiredBaseRatio}，join14 ${results.growth.lateJoinBaseRequirement[3].requiredBaseRatio}。\n\n固定窗口代理严格使用 \`D=(N-1-k+kq+w(1+rkn))/(N-1+w)\`，N=6；完整 n4/9/14、k1..3、q=.4/.6/.8、w=1/2/3 结果保存在JSON。它只是等6次取得选择的无交互伤害代理，不能称为战力或胜率，也不描述当前未满员换人。\n\n## 全路径枚举\n\n- 枚举 ${results.routes.totalPaths.toLocaleString("en-US")} 条路径；比例是组合计数占比，不是玩家行为概率。\n- 战斗总数3～15；前14层战斗钟2～14；可得装备奖励2～14（全胜且最终Boss无物品）。\n- 招募次数分布：${countBy("recruitmentCount").map(x=>`${x.value}次=${x.count}条`).join("；")}。\n- 战斗总数分布：${countBy("battleCount").map(x=>`${x.value}场=${x.count}条`).join("；")}。\n\n## 材料与三条合法路线\n\n每个前14层完成节点给1材料。cost4在节点4/8/12产生3名高阶三选一单位；cost5在5/10产生2名；cost6在6/12产生2名。升阶不花金币，每名英雄至多一次；本算例把每次结果作为新单位自动入队。\n\n| 路线 | 终局前可用金币 | 名册 | 部署 | 后备 |\n|---|---:|---:|---:|---:|\n${scenarioLast.map(x=>`| ${x.name} | ${x.gold} | ${x.roster} | ${x.deployed} | ${x.reserve} |`).join("\n")}\n\n逐层金币、材料、名册、部署和后备见JSON。最终Boss结算不计可用金币、材料或物品。路线只证明节点选择合法，不保证拿到指定身份、战斗胜利或成长曲线成立；不模拟随机商店价格、装备交互、角色能力、敌人或玩家选择。\n`;
const singleSourceSensitivity = config.growth.checkFloors.map(floor => {
  const n = floor - 1;
  return `| ${floor} | ${n} | ${config.growth.rates.map(rate => round(1 + rate*n)).join(" | ")} |`;
}).join("\n");
const finalMd = md
  .replace("固定窗口代理严格使用", `目标必须在该层战前已经加入；有生产源时，来源也必须已经加入。未来才加入的目标或来源在JSON中标为 \`eligible=false\`、倍率为 \`null\`，不计入正常比较；0来源不要求生产者资格。\n\n| 战斗层 | 有效次数 | 3% | 4% | 6% |\n|---:|---:|---:|---:|---:|\n${singleSourceSensitivity}\n\n固定窗口代理严格使用`)
  .replace("可得装备奖励2～14", "可得装备或遗物三选一奖励2～14")
  .replace("## 材料与三条合法路线", "## 材料与四条合法路线")
  .replace("## 材料与四条合法路线", "BRBR／RBRB 置换敏感性检查点均取 Boss 开战前状态：第5／10／15层分别只包含截至节点4／9／14的收益，不含当前Boss奖励；完整字段见JSON。\n\n## 材料与四条合法路线")
  .replace("| 路线 | 终局前可用金币 | 名册 | 部署 | 后备 |\n|---|---:|---:|---:|---:|\n" +
    scenarioLast.map(x=>`| ${x.name} | ${x.gold} | ${x.roster} | ${x.deployed} | ${x.reserve} |`).join("\n"),
    "| 路线 | 终局前可用金币 | 装备或遗物选择 | 成长机会 | 战斗钟 | 名册 | 部署 | 后备 |\n|---|---:|---:|---:|---:|---:|---:|---:|\n" +
    scenarioLast.map(x=>`| ${x.name} | ${x.gold} | ${x.itemChoiceRewards} | ${x.growthOpportunities} | ${x.usableBattleClock} | ${x.roster} | ${x.deployed} | ${x.reserve} |`).join("\n"))
  .replace("逐层金币、材料、名册、部署和后备见JSON。", "最少战斗路线终局前88金，对比全R招募/B普战路线94金只少6金；主要显式机会差是装备或遗物选择2次对8次，不能据此臆称金币损失巨大。逐层金币、材料、名册、部署和后备见JSON。");
fs.writeFileSync(path.join(here,"model-report.md"),finalMd);
console.log(JSON.stringify({totalPaths:results.routes.totalPaths,battleRange:[Math.min(...dist.map(x=>x.battleCount)),Math.max(...dist.map(x=>x.battleCount))],usableBattleClockRange:[Math.min(...dist.map(x=>x.usableBattleClock)),Math.max(...dist.map(x=>x.usableBattleClock))],itemChoiceRewardRange:[Math.min(...dist.map(x=>x.obtainableItemChoiceRewards)),Math.max(...dist.map(x=>x.obtainableItemChoiceRewards))],materials:results.materials,scenarioLast},null,2));
