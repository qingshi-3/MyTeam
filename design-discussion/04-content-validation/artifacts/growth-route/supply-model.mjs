import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const here = path.dirname(fileURLToPath(import.meta.url));
const planPath = path.resolve(here, '../trait-matrix/plan.json');
const outPath = path.join(here, 'supply-results.json');
const reportPath = path.join(here, 'supply-report.md');
const plan = JSON.parse(fs.readFileSync(planPath, 'utf8'));

const RUNS = 5000;
const SYSTEMS = plan.systems.map(({ id, name, tiers }) => ({ id, name, thresholds: tiers.map(t => t.count) }));
const ORIGINAL_HEROES = plan.units.map(u => ({ id: u.id, name: u.name, tier: u.tier, systems: u.systems }));
const SUPPLY_PLACEHOLDERS = [
  { id: 'GX01', name: '棘毒术士占位', tier: 4, systems: ['poison'], class: 'mage' },
  { id: 'GX02', name: '棘毒统御者占位', tier: 4, systems: ['poison'], class: 'summoner' },
  { id: 'GX03', name: '构装射手占位', tier: 4, systems: ['construct'], class: 'ranger' },
  { id: 'GX04', name: '构装护卫占位', tier: 4, systems: ['construct'], class: 'guard' },
];
const POOLS = [
  { id: 'original49', label: '原始49池', heroes: ORIGINAL_HEROES },
  { id: 'expanded53', label: '扩展53供给测试池', heroes: [...ORIGINAL_HEROES, ...SUPPLY_PLACEHOLDERS] },
];
const HERO_BY_ID = new Map(POOLS[1].heroes.map(h => [h.id, h]));
const PRODUCERS = new Set(['MX10', 'MX25', 'MX30']);
const SPELL_SOURCE = 'MX15';
const CHECKPOINTS = [5, 10, 15];
const RECRUIT_NODES = new Set([2, 4, 7, 9, 12, 14]);
const DISCOVERY_NODES = new Map([[4, 3], [8, 4], [12, 4]]);
const WEIGHTS = [
  { min: 0, max: 2, weights: [65, 35, 0, 0] },
  { min: 3, max: 5, weights: [35, 45, 20, 0] },
  { min: 6, max: 9, weights: [15, 30, 45, 10] },
  { min: 10, max: 14, weights: [0, 15, 50, 35] },
];

function rng32(seed) {
  let state = seed >>> 0;
  return () => {
    state = (state + 0x6D2B79F5) >>> 0;
    let t = state;
    t = Math.imul(t ^ (t >>> 15), t | 1);
    t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

function hashSeed(systemIndex, runIndex) {
  let x = (0x9E3779B9 ^ Math.imul(systemIndex + 1, 0x85EBCA6B) ^ Math.imul(runIndex + 1, 0xC2B2AE35)) >>> 0;
  x ^= x >>> 16; x = Math.imul(x, 0x7FEB352D); x ^= x >>> 15; x = Math.imul(x, 0x846CA68B); x ^= x >>> 16;
  return x >>> 0;
}

function tierWeights(floorIndex) {
  const row = WEIGHTS.find(w => floorIndex >= w.min && floorIndex <= w.max);
  if (!row) throw new Error(`No tier weights for FloorIndex ${floorIndex}`);
  return row.weights;
}

function weightedIndex(entries, random) {
  const total = entries.reduce((s, e) => s + e.weight, 0);
  if (total <= 0) return -1;
  let roll = random() * total;
  for (let i = 0; i < entries.length; i++) {
    roll -= entries[i].weight;
    if (roll < 0) return i;
  }
  return entries.length - 1;
}

function offerWeighted(heroes, count, floorIndex, owned, random) {
  const offered = new Set();
  const result = [];
  const weights = tierWeights(floorIndex);
  for (let slot = 0; slot < count; slot++) {
    const available = [1, 2, 3, 4].map(tier => ({
      tier,
      weight: weights[tier - 1],
      heroes: heroes.filter(h => h.tier === tier && !owned.has(h.id) && !offered.has(h.id)),
    })).filter(x => x.weight > 0 && x.heroes.length > 0);
    const pickedTierIndex = weightedIndex(available, random);
    if (pickedTierIndex < 0) break;
    const bucket = available[pickedTierIndex].heroes;
    const hero = bucket[Math.floor(random() * bucket.length)];
    result.push(hero.id);
    offered.add(hero.id);
  }
  return result;
}

function offerFixedTier(heroes, count, tier, owned, random) {
  const pool = heroes.filter(h => h.tier === tier && !owned.has(h.id));
  const result = [];
  while (result.length < count && pool.length > 0) {
    const index = Math.floor(random() * pool.length);
    result.push(pool[index].id);
    pool.splice(index, 1);
  }
  return result;
}

function choose(offers, owned, systemId, strategy) {
  if (offers.length === 0) return null;
  const hasProducer = [...owned].some(id => PRODUCERS.has(id));
  const rank = id => {
    const hero = HERO_BY_ID.get(id);
    const main = hero.systems.includes(systemId) ? 1 : 0;
    const producer = PRODUCERS.has(id) ? 1 : 0;
    let primary;
    if (strategy === 'B' && !hasProducer) primary = [producer, main];
    else primary = [main, producer];
    return { primary, tier: hero.tier, id };
  };
  return [...offers].sort((a, b) => {
    const ra = rank(a), rb = rank(b);
    if (ra.primary[0] !== rb.primary[0]) return rb.primary[0] - ra.primary[0];
    if (ra.primary[1] !== rb.primary[1]) return rb.primary[1] - ra.primary[1];
    if (ra.tier !== rb.tier) return rb.tier - ra.tier;
    return ra.id.localeCompare(rb.id);
  })[0];
}

function mainCount(owned, systemId, checkpoint) {
  return owned.filter(a => a.joinNode <= checkpoint - 1 && HERO_BY_ID.get(a.id).systems.includes(systemId)).length;
}

function producerCount(owned, checkpoint) {
  return owned.filter(a => a.joinNode <= checkpoint - 1 && PRODUCERS.has(a.id)).length;
}

function effectiveProduction(owned, checkpoint) {
  return owned.filter(a => PRODUCERS.has(a.id) && a.joinNode <= checkpoint - 1)
    .reduce((sum, a) => sum + (checkpoint - 1 - a.joinNode), 0);
}

function deployableMainCount(owned, systemId, limit = 10) {
  const main = owned.filter(a => HERO_BY_ID.get(a.id).systems.includes(systemId)).length;
  return Math.min(limit, main);
}

function simulate(pool, systemId, strategy, seed, captureTrace = false) {
  const random = rng32(seed);
  const owned = [];
  const ownedIds = new Set();
  const trace = [];
  const discoveryShortfalls = [];
  const acquire = (id, joinNode, source) => {
    if (!id) return;
    if (ownedIds.has(id)) throw new Error(`Duplicate acquisition ${id}`);
    ownedIds.add(id);
    owned.push({ id, joinNode, source });
  };
  const record = (event, node, tierRule, offers, picks) => {
    if (new Set(offers).size !== offers.length) throw new Error(`Duplicate offer in ${event}@${node}`);
    if (offers.some(id => ownedIds.has(id))) throw new Error(`Owned identity offered in ${event}@${node}`);
    if (picks.some(id => !offers.includes(id))) throw new Error(`Acquisition outside offers at ${event}@${node}`);
    if (event === 'discovery') {
      const tier = Number(tierRule.slice(1, 2));
      if (offers.some(id => HERO_BY_ID.get(id).tier !== tier)) throw new Error(`Wrong discovery tier at node ${node}`);
      if (offers.length < 3) discoveryShortfalls.push({ node, tier, offered: offers.length });
    }
    if (captureTrace) trace.push({ event, node, floorIndex: event === 'recruit' ? node - 1 : null, tierRule, offers, acquisitions: picks });
  };

  const openingOffers = offerWeighted(pool.heroes, 6, 0, ownedIds, random);
  const openingPicks = [];
  for (let i = 0; i < 2; i++) {
    const remaining = openingOffers.filter(id => !openingPicks.includes(id));
    const pick = choose(remaining, new Set([...ownedIds, ...openingPicks]), systemId, strategy);
    if (!pick) break;
    openingPicks.push(pick);
  }
  record('opening', 0, 'FloorIndex 0 weighted tiers', openingOffers, openingPicks);
  for (const id of openingPicks) acquire(id, 0, 'opening');

  for (let node = 1; node <= 14; node++) {
    if (RECRUIT_NODES.has(node)) {
      const offers = offerWeighted(pool.heroes, 3, node - 1, ownedIds, random);
      const pick = choose(offers, ownedIds, systemId, strategy);
      record('recruit', node, `FloorIndex ${node - 1} weighted tiers`, offers, pick ? [pick] : []);
      acquire(pick, node, 'recruit');
    }
    if (DISCOVERY_NODES.has(node)) {
      const tier = DISCOVERY_NODES.get(node);
      const offers = offerFixedTier(pool.heroes, 3, tier, ownedIds, random);
      const pick = choose(offers, ownedIds, systemId, strategy);
      record('discovery', node, `T${tier} only`, offers, pick ? [pick] : []);
      acquire(pick, node, 'discovery');
    }
  }

  if (owned.length !== 11) throw new Error(`Expected 11 acquisitions, got ${owned.length}`);
  return { owned, trace, discoveryShortfalls };
}

function distribution(values, denominator = RUNS) {
  const counts = {};
  for (const v of values) counts[v] = (counts[v] ?? 0) + 1;
  return Object.fromEntries(Object.entries(counts).sort((a, b) => Number(a[0]) - Number(b[0])).map(([k, v]) => [k, { count: v, rate: v / denominator }]));
}

const results = {
  title: 'MX49与扩展53候选供给抽样',
  status: 'Agent候选设计测试；不预测现行17人池，不代表确认、实现、发布或平衡结论',
  source: '../trait-matrix/plan.json',
  assumptions: {
    runs_per_pool_system_strategy: RUNS,
    pools: {
      original49: '原plan完整MX49，原结果保留在此池。',
      expanded53: '仅为供给覆盖测试增加GX01棘毒术士、GX02棘毒统御者、GX03构装射手、GX04构装护卫四个T4身份；技能完全未模拟。',
    },
    opening: 'FloorIndex 0按权重6选2', recruit_nodes: [...RECRUIT_NODES], recruit_offer: '普通节点3选1；FloorIndex=节点号-1',
    tier_weights: WEIGHTS, discoveries: [{ node: 4, tier: 3, offer: 3 }, { node: 8, tier: 4, offer: 3 }, { node: 12, tier: 4, offer: 3 }],
    order: '节点4/12先普通招募后升阶发现',
    strategies: { A: '主体系优先；无本系候选时优先MX10/MX25/MX30；再按阶位高、id小。', B: '尚无属性生产者时优先MX10/MX25/MX30；取得一名后与A相同。' },
    identity_rules: '拥有身份排除；同份候选无重复；未选者可后续再见；合法候选不足不伪造。',
    attribute_producers: [...PRODUCERS], spell_source_separate: SPELL_SOURCE, checkpoints: CHECKPOINTS,
    effective_production_formula: '检查点k前仅计joinNode<=k-1的属性源；每名贡献k-1-joinNode，取得前不可追溯。',
    deployment: '终局名册11名；另算最多部署10人时的主体系成员上限。',
    prng: '自实现Mulberry32 uint32；同体系同run、两个池和A/B使用同seed，但合法池差异会改变后续offers。',
  },
  supply_placeholders: SUPPLY_PLACEHOLDERS,
  pools: {},
  self_checks: { total_runs: 0, assertions_apply_to: '所有样本、所有事件；不是仅示例轨迹', failures: 0 },
};

for (const pool of POOLS) {
  const poolResult = results.pools[pool.id] = { label: pool.label, size: pool.heroes.length, tier_counts: distribution(pool.heroes.map(h => h.tier), pool.heroes.length), systems: {} };
  for (let si = 0; si < SYSTEMS.length; si++) {
    const system = SYSTEMS[si];
    poolResult.systems[system.id] = { name: system.name, thresholds: system.thresholds, strategies: {} };
    for (const strategy of ['A', 'B']) {
      const accum = Object.fromEntries(CHECKPOINTS.map(k => [k, { main: [], producer: [], production: [], thresholds: Object.fromEntries(system.thresholds.map(t => [t, 0])) }]));
      const firstProducerNodes = [], finalRosterMain = [], deployableMain = [];
      let noProducer = 0, spellSource = 0, shortfallEvents = 0, shortfallRuns = 0;
      const shortfallByNode = { 4: 0, 8: 0, 12: 0 };
      let example = null;
      for (let run = 0; run < RUNS; run++) {
        const seed = hashSeed(si, run);
        const sim = simulate(pool, system.id, strategy, seed, run === 0);
        if (run === 0) example = { seed, offers_and_acquisitions: sim.trace, final_roster: sim.owned };
        results.self_checks.total_runs++;
        if (sim.discoveryShortfalls.length) shortfallRuns++;
        shortfallEvents += sim.discoveryShortfalls.length;
        for (const e of sim.discoveryShortfalls) shortfallByNode[e.node]++;
        const producers = sim.owned.filter(a => PRODUCERS.has(a.id));
        if (producers.length) firstProducerNodes.push(Math.min(...producers.map(a => a.joinNode))); else noProducer++;
        if (sim.owned.some(a => a.id === SPELL_SOURCE)) spellSource++;
        for (const k of CHECKPOINTS) {
          const mc = mainCount(sim.owned, system.id, k), pc = producerCount(sim.owned, k), ep = effectiveProduction(sim.owned, k);
          if (ep < 0) throw new Error(`Early production opportunity in ${pool.id}/${system.id}/${strategy}/${run}`);
          accum[k].main.push(mc); accum[k].producer.push(pc); accum[k].production.push(ep);
          for (const t of system.thresholds) if (mc >= t) accum[k].thresholds[t]++;
        }
        finalRosterMain.push(mainCount(sim.owned, system.id, 15));
        deployableMain.push(deployableMainCount(sim.owned, system.id));
      }
      const checkpoints = {};
      for (const k of CHECKPOINTS) checkpoints[k] = {
        main_system_count_distribution: distribution(accum[k].main),
        threshold_rates: Object.fromEntries(system.thresholds.map(t => [t, accum[k].thresholds[t] / RUNS])),
        middle_threshold: system.thresholds[Math.min(1, system.thresholds.length - 1)],
        middle_threshold_rate: accum[k].thresholds[system.thresholds[Math.min(1, system.thresholds.length - 1)]] / RUNS,
        deep_threshold: system.thresholds.at(-1), deep_threshold_rate: accum[k].thresholds[system.thresholds.at(-1)] / RUNS,
        attribute_producer_rate: accum[k].producer.filter(x => x > 0).length / RUNS,
        attribute_producer_count_distribution: distribution(accum[k].producer),
        effective_attribute_production: { average: accum[k].production.reduce((a, b) => a + b, 0) / RUNS, distribution: distribution(accum[k].production) },
      };
      poolResult.systems[system.id].strategies[strategy] = {
        checkpoints,
        discovery_offer_shortfall: { event_count: shortfallEvents, event_rate: shortfallEvents / (RUNS * DISCOVERY_NODES.size), run_count: shortfallRuns, run_rate: shortfallRuns / RUNS, by_node_count: shortfallByNode },
        first_attribute_producer: { acquired_rate: 1 - noProducer / RUNS, average_join_node_among_acquired: firstProducerNodes.length ? firstProducerNodes.reduce((a, b) => a + b, 0) / firstProducerNodes.length : null, no_source_runs: noProducer, join_node_distribution_among_all_runs: distribution(firstProducerNodes) },
        spell_source_MX15_acquired_rate: spellSource / RUNS,
        final_roster_size: 11,
        final_roster_main_system_count_distribution: distribution(finalRosterMain),
        final_deployable_10_max_main_system_count_distribution: distribution(deployableMain),
        example,
      };
    }
  }
}

fs.writeFileSync(outPath, JSON.stringify(results, null, 2) + '\n');

const pct = x => `${(x * 100).toFixed(1)}%`, avg = x => x == null ? '—' : x.toFixed(2);
const lines = ['# MX49 与扩展53候选供给抽样', '', '> 状态：Agent 候选设计测试；不预测现行 17 人池，不代表确认、实现、发布或平衡结论。来源：[原 trait matrix 计划](../trait-matrix/plan.json)。完整数据见 [supply-results.json](supply-results.json)，可用 [supply-model.mjs](supply-model.mjs)复现。', ''];
lines.push('## 失败与对照假设', '');
lines.push('- `original49` 原样保留完整 MX49。原池只有 5 个 T4，且棘毒、构装没有 T4；严格 T4 发现会出现不足 3 个合法候选，也会天然偏向已有 T4 的体系。这个失败不通过补写原结果掩盖。');
lines.push('- `expanded53` 仅为供给覆盖测试加入四个 T4 身份：GX01 棘毒术士、GX02 棘毒统御者、GX03 构装射手、GX04 构装护卫。技能完全未模拟，未加入原 plan、content-map 或运行资源；它不承诺发布四张卡，也不证明战斗平衡。');
lines.push('- 两池均按相同开局、普通招募、发现顺序、阶位权重、策略和固定 seed 各运行 70,000 次。发现合法候选不足时缩短候选，绝不伪造第三项。A/B 或池差异改变已拥有身份后，后续 offer 不保证相同。');
lines.push('- 中档取原 plan 各体系第二门槛，深档取最后门槛。所有比率只属于当前算法与测试池。', '');
lines.push('## 5／10／15 战前简表', '');
lines.push('| 池 | 体系 | 策略 | 属性源率 5／10／15 | 中档率 5／10／15 | 深档率@15 | 无源 runs@15 | 发现不足事件率 | 发现不足 run率 |');
lines.push('|---|---|---|---|---|---:|---:|---:|---:|');
for (const pool of POOLS) for (const system of SYSTEMS) for (const strategy of ['A', 'B']) {
  const s = results.pools[pool.id].systems[system.id].strategies[strategy];
  const source = CHECKPOINTS.map(k => pct(s.checkpoints[k].attribute_producer_rate)).join('／');
  const middle = CHECKPOINTS.map(k => pct(s.checkpoints[k].middle_threshold_rate)).join('／');
  lines.push(`| ${pool.id} | ${system.name} | ${strategy} | ${source} | ${middle} | ${pct(s.checkpoints[15].deep_threshold_rate)} | ${s.first_attribute_producer.no_source_runs} | ${pct(s.discovery_offer_shortfall.event_rate)} | ${pct(s.discovery_offer_shortfall.run_rate)} |`);
}
lines.push('', '## 原池与扩展池对照', '');
lines.push('| 体系 | 策略 | 中档@15 原→扩 | 深档@15 原→扩 | 属性源@15 原→扩 | 发现不足事件 原→扩 | 发现不足runs 原→扩 |');
lines.push('|---|---|---|---|---|---|---|');
for (const system of SYSTEMS) for (const strategy of ['A', 'B']) {
  const a = results.pools.original49.systems[system.id].strategies[strategy], b = results.pools.expanded53.systems[system.id].strategies[strategy];
  lines.push(`| ${system.name} | ${strategy} | ${pct(a.checkpoints[15].middle_threshold_rate)}→${pct(b.checkpoints[15].middle_threshold_rate)} | ${pct(a.checkpoints[15].deep_threshold_rate)}→${pct(b.checkpoints[15].deep_threshold_rate)} | ${pct(a.checkpoints[15].attribute_producer_rate)}→${pct(b.checkpoints[15].attribute_producer_rate)} | ${pct(a.discovery_offer_shortfall.event_rate)}→${pct(b.discovery_offer_shortfall.event_rate)} | ${pct(a.discovery_offer_shortfall.run_rate)}→${pct(b.discovery_offer_shortfall.run_rate)} |`);
}
const originalShortfalls = [], expandedShortfalls = [];
for (const system of SYSTEMS) for (const strategy of ['A', 'B']) {
  originalShortfalls.push(results.pools.original49.systems[system.id].strategies[strategy].discovery_offer_shortfall);
  expandedShortfalls.push(results.pools.expanded53.systems[system.id].strategies[strategy].discovery_offer_shortfall);
}
lines.push('');
lines.push(`原池发现不足事件率为 ${pct(Math.min(...originalShortfalls.map(x => x.event_rate)))}–${pct(Math.max(...originalShortfalls.map(x => x.event_rate)))}，受影响 run 率为 ${pct(Math.min(...originalShortfalls.map(x => x.run_rate)))}–${pct(Math.max(...originalShortfalls.map(x => x.run_rate)))}；全部发生在节点 12 的第二次 T4 发现。扩展池本轮的不足事件和受影响 runs 都是 ${expandedShortfalls.reduce((s, x) => s + x.event_count, 0)}，因此这四个占位身份在本抽样规则下修复了“三选不足”，但这不证明占位角色的技能、平衡或发布合理。`, '');
lines.push('', '## 完整示例轨迹', '');
lines.push('每个池、体系和策略各保留一条 run 0 轨迹；节点 4／12 的普通招募先于发现。', '');
for (const pool of POOLS) for (const system of SYSTEMS) for (const strategy of ['A', 'B']) {
  const e = results.pools[pool.id].systems[system.id].strategies[strategy].example;
  lines.push(`### ${pool.id} · ${system.name} · ${strategy}（seed ${e.seed}）`, '');
  for (const x of e.offers_and_acquisitions) lines.push(`- ${x.event}@${x.node}（${x.tierRule}）：${x.offers.join('、') || '无合法候选'} → ${x.acquisitions.join('、') || '未取得'}`);
  lines.push('', `终局名册：${e.final_roster.map(x => `${x.id}@${x.joinNode}`).join('、')}`, '');
}
lines.push('## 限制', '');
lines.push('- original49 和 expanded53 都不是正式供给；expanded53 只是检验高阶池覆盖补洞的候选。');
lines.push('- 标签贪心不是最优阵容、玩家行为或 AI；不保证坦克、治疗、输出齐全。');
lines.push('- 不模拟技能、战斗、装备、材料支付、存活、阵型、部署取舍或运营机会成本。');
lines.push('- 原 7 职业＋7 体系及其 14 羁绊没有修改；比率只属于此算法、池和假设。');
lines.push('- 终局 11 人名册另有 10 人可部署最大主体系人数分布，不能把全名册直接当作激活羁绊。', '');
lines.push('## 自检', '');
lines.push(`共执行 ${results.self_checks.total_runs.toLocaleString('en-US')} runs。候选去重、已拥有排除、发现阶位、取得来自当次候选、终局 11 人与机会不提前均在每个 run 的每个事件即时断言，不依赖是否保存示例轨迹；本次无断言失败。`);
fs.writeFileSync(reportPath, lines.join('\n') + '\n');
console.log(JSON.stringify({ runs: results.self_checks.total_runs, pools: POOLS.map(p => p.id), output: path.basename(outPath), report: path.basename(reportPath) }));
