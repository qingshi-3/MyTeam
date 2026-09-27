import fs from "node:fs";
import path from "node:path";

const validationDir = path.dirname(new URL(import.meta.url).pathname.replace(/^\/(?:([A-Za-z]:))/, "$1"));
const sourcePath = path.resolve(process.argv[2] ?? path.join(validationDir, "matchups.json"));
const outputStem = path.resolve(process.argv[3] ?? path.join(validationDir, "matchups-summary"));

function fail(message) {
  throw new Error(`matchup summary refused: ${message}`);
}

function finite(value, label) {
  if (typeof value !== "number" || !Number.isFinite(value)) fail(`${label} is not a finite number`);
  return value;
}

function median(values) {
  if (!values.length) return null;
  const ordered = [...values].sort((a, b) => a - b);
  const middle = Math.floor(ordered.length / 2);
  return ordered.length % 2 ? ordered[middle] : (ordered[middle - 1] + ordered[middle]) / 2;
}

function mean(values) {
  return values.length ? values.reduce((sum, value) => sum + value, 0) / values.length : null;
}

function stats(values) {
  return { Mean: mean(values), Median: median(values), Min: values.length ? Math.min(...values) : null,
    Max: values.length ? Math.max(...values) : null };
}

function groupBy(values, keyOf) {
  const groups = new Map();
  for (const value of values) {
    const key = keyOf(value);
    if (!groups.has(key)) groups.set(key, []);
    groups.get(key).push(value);
  }
  return groups;
}

function playerUnits(record) {
  return record.Battle.Units.filter(unit => unit.Team === 0);
}

function ratio(record) {
  const initial = playerUnits(record).filter(unit => !unit.Temporary);
  return initial.reduce((sum, unit) => sum + unit.FinalHealth, 0) /
    Math.max(1, initial.reduce((sum, unit) => sum + unit.MaximumHealth, 0));
}

function enemyRemaining(record) {
  const enemies = record.Battle.Units.filter(unit => unit.Team === 1 && !unit.Temporary);
  const maximumHealth = Math.max(1, enemies.reduce((sum, unit) => sum + unit.MaximumHealth, 0));
  return {
    HealthRatio: enemies.reduce((sum, unit) => sum + unit.FinalHealth, 0) / maximumHealth,
    ShieldToHealthRatio: enemies.reduce((sum, unit) => sum + unit.FinalShield, 0) / maximumHealth,
  };
}

function sumUnits(units, field) {
  return units.reduce((sum, unit) => sum + finite(unit[field], `${unit.RuntimeId}.${field}`), 0);
}

function summarizeTeamRows(rows) {
  const outcomes = new Set(["PlayerVictory", "PlayerDefeat", "Timeout"]);
  for (const row of rows) if (!outcomes.has(row.Battle.Outcome))
    fail(`non-terminal or unknown outcome ${row.Battle.Outcome} in ${row.TeamId}/${row.Kind}/${row.Seed}`);
  const totals = rows.map(row => {
    const units = playerUnits(row);
    const initial = units.filter(unit => !unit.Temporary);
    const summons = units.filter(unit => unit.Temporary);
    return {
      Damage: sumUnits(units, "DamageDealt"),
      InitialUnitDamage: sumUnits(initial, "DamageDealt"),
      SummonDamage: sumUnits(summons, "DamageDealt"),
      EffectiveHealing: sumUnits(units, "EffectiveHealing"),
      RequestedHealing: sumUnits(units, "RequestedHealing"),
      HealingOverflow: sumUnits(units, "HealingOverflow"),
      ShieldGranted: sumUnits(units, "ShieldGranted"),
      ShieldAbsorbed: sumUnits(units, "ShieldAbsorbed"),
      HealthDamageTaken: sumUnits(units, "HealthDamageTaken"),
      HardControlApplications: sumUnits(units, "HardControlApplications"),
      GrantedHardControlTicks: sumUnits(units, "GrantedHardControlTicks"),
      ActiveCasts: sumUnits(units, "ActiveCasts"),
      ManaCasts: sumUnits(units, "ManaCasts"),
      Attacks: sumUnits(units, "Attacks"),
    };
  });
  const metric = field => stats(totals.map(total => total[field]));
  const result = {
    Samples: rows.length,
    Wins: rows.filter(row => row.Battle.Outcome === "PlayerVictory").length,
    Losses: rows.filter(row => row.Battle.Outcome === "PlayerDefeat").length,
    Timeouts: rows.filter(row => row.Battle.Outcome === "Timeout").length,
    Ticks: stats(rows.map(row => finite(row.Battle.Ticks, "Battle.Ticks"))),
    RemainingHealthRatio: stats(rows.map(ratio)),
    EnemyRemainingHealthRatio: stats(rows.map(row => enemyRemaining(row).HealthRatio)),
    EnemyRemainingShieldToHealthRatio: stats(rows.map(row => enemyRemaining(row).ShieldToHealthRatio)),
    Damage: metric("Damage"), InitialUnitDamage: metric("InitialUnitDamage"), SummonDamage: metric("SummonDamage"),
    EffectiveHealing: metric("EffectiveHealing"), RequestedHealing: metric("RequestedHealing"), HealingOverflow: metric("HealingOverflow"),
    ShieldGranted: metric("ShieldGranted"), ShieldAbsorbed: metric("ShieldAbsorbed"), HealthDamageTaken: metric("HealthDamageTaken"),
    HardControlApplications: metric("HardControlApplications"), GrantedHardControlTicks: metric("GrantedHardControlTicks"),
    ActiveCasts: metric("ActiveCasts"), ManaCasts: metric("ManaCasts"), Attacks: metric("Attacks"),
  };
  if (result.Wins + result.Losses + result.Timeouts !== result.Samples)
    fail(`outcome totals do not close for ${rows[0].TeamId}/${rows[0].Kind}`);
  return result;
}

if (!fs.existsSync(sourcePath)) fail(`source does not exist: ${sourcePath}`);
const data = JSON.parse(fs.readFileSync(sourcePath, "utf8"));
if (!Array.isArray(data.Records)) fail("Records is missing");
if (!Array.isArray(data.Teams)) fail("Teams is missing");
if (data.RecordCount !== data.Records.length) fail(`RecordCount=${data.RecordCount}, actual=${data.Records.length}`);
if (data.Records.length !== 297) fail(`expected 297 records, got ${data.Records.length}`);
if (data.Teams.length !== 33) fail(`expected 33 teams, got ${data.Teams.length}`);
if (data.DeterminismReplays !== 3) fail(`expected 3 determinism replays, got ${data.DeterminismReplays}`);

const recordKeys = new Set();
for (const row of data.Records) {
  const key = `${row.TeamId}|${row.Kind}|${row.Seed}`;
  if (recordKeys.has(key)) fail(`duplicate record ${key}`);
  recordKeys.add(key);
  if (!row.Battle || !Array.isArray(row.Battle.Units)) fail(`missing battle units for ${key}`);
}
for (const team of data.Teams) {
  const rows = data.Records.filter(row => row.TeamId === team.Id);
  if (rows.length !== 9) fail(`${team.Id} has ${rows.length} records, expected 9`);
  if (new Set(rows.map(row => row.Kind)).size !== 3 || new Set(rows.map(row => row.Seed)).size !== 3)
    fail(`${team.Id} does not contain 3 kinds x 3 seeds`);
}
for (const rows of groupBy(data.Records, row => `${row.Progression}|${row.Kind}|${row.Seed}`).values()) {
  if (new Set(rows.map(row => row.Population)).size !== 1) fail(`unequal population in ${rows[0].Progression}/${rows[0].Kind}/${rows[0].Seed}`);
  if (new Set(rows.map(row => JSON.stringify(row.TierVector))).size !== 1) fail(`unequal tier vector in ${rows[0].Progression}/${rows[0].Kind}/${rows[0].Seed}`);
  if (new Set(rows.map(row => `${row.EncounterId}|${row.CompositionId}|${row.FloorRuleId}`)).size !== 1)
    fail(`enemy fixture mismatch in ${rows[0].Progression}/${rows[0].Kind}/${rows[0].Seed}`);
}

const teamById = new Map(data.Teams.map(team => [team.Id, team]));
const teamGroups = [...groupBy(data.Records, row => `${row.TeamId}|${row.Kind}`).entries()].map(([key, rows]) => {
  const [teamId, kind] = key.split("|");
  const team = teamById.get(teamId);
  return { TeamId: teamId, Direction: team.Direction, Progression: team.Progression, Kind: kind,
    Population: rows[0].Population, TierVector: rows[0].TierVector, ActualTraits: team.ActualTraits,
    ...summarizeTeamRows(rows) };
}).sort((a, b) => a.Progression.localeCompare(b.Progression) || a.Kind.localeCompare(b.Kind) || a.Direction.localeCompare(b.Direction));

const progressionGroups = [...groupBy(data.Records, row => `${row.Progression}|${row.Direction}`).entries()].map(([key, rows]) => {
  const [progression, direction] = key.split("|");
  return { Progression: progression, Direction: direction, ...summarizeTeamRows(rows) };
}).sort((a, b) => a.Progression.localeCompare(b.Progression) || a.Direction.localeCompare(b.Direction));

const unitRows = [];
for (const [key, units] of groupBy(data.Records.flatMap(record => playerUnits(record).map(unit => ({ record, unit }))),
  item => `${item.record.Progression}|${item.record.Direction}|${item.unit.ContentId}|${item.unit.Temporary}`).entries()) {
  const [progression, direction, contentId, temporaryText] = key.split("|");
  const values = units.map(item => item.unit);
  const numeric = field => stats(values.map(value => finite(value[field], `${contentId}.${field}`)));
  const present = field => values.map(value => value[field]).filter(value => typeof value === "number");
  unitRows.push({ Progression: progression, Direction: direction, ContentId: contentId, Temporary: temporaryText === "true",
    Samples: values.length, Damage: numeric("DamageDealt"), EffectiveHealing: numeric("EffectiveHealing"),
    RequestedHealing: numeric("RequestedHealing"), HealingOverflow: numeric("HealingOverflow"),
    ShieldGranted: numeric("ShieldGranted"), ShieldAbsorbed: numeric("ShieldAbsorbed"), HealthDamageTaken: numeric("HealthDamageTaken"),
    Attacks: numeric("Attacks"), ActiveCasts: numeric("ActiveCasts"), ManaCasts: numeric("ManaCasts"),
    FirstActiveCastTick: stats(present("FirstActiveCastTick")), FirstActiveCastRate: present("FirstActiveCastTick").length / values.length,
    FirstManaCastTick: stats(present("FirstManaCastTick")), FirstManaCastRate: present("FirstManaCastTick").length / values.length,
    HardControlApplications: numeric("HardControlApplications"), GrantedHardControlTicks: numeric("GrantedHardControlTicks"),
    HardControlledUnionTicks: numeric("HardControlledUnionTicks"), SlowedUnionTicks: numeric("SlowedUnionTicks"),
    OwnedSummonDamageAttribution: numeric("OwnedSummonDamage"),
  });
}
unitRows.sort((a, b) => a.Progression.localeCompare(b.Progression) || a.Direction.localeCompare(b.Direction) || a.ContentId.localeCompare(b.ContentId));

const summonOwnership = [...groupBy(data.Records.flatMap(record => playerUnits(record).filter(unit => unit.Temporary)
  .map(unit => ({ record, unit }))), item => `${item.record.Progression}|${item.record.Direction}|${item.unit.RootSummonerRuntimeId}`).entries()]
  .map(([key, rows]) => { const [progression, direction, root] = key.split("|"); return { Progression: progression,
    Direction: direction, RootSummonerRuntimeId: root, SummonEntities: rows.length,
    Damage: stats(rows.map(row => finite(row.unit.DamageDealt, `${row.unit.RuntimeId}.DamageDealt`))) }; });

const plannedMxIds = Array.from({ length: 49 }, (_, index) => `hero_mx${String(index + 1).padStart(2, "0")}`);
const initialPlayerAppearances = data.Records.flatMap(record => playerUnits(record).filter(unit => !unit.Temporary));
const appearanceCounts = Object.fromEntries(plannedMxIds.map(id => [id,
  initialPlayerAppearances.filter(unit => unit.ContentId === id).length]));
const zeroCastByProgression = [...groupBy(data.Records.flatMap(record => playerUnits(record).filter(unit => !unit.Temporary)
  .map(unit => ({ progression: record.Progression, unit }))), item => item.progression).entries()].map(([progression, rows]) => {
    const zero = rows.filter(row => row.unit.ActiveCasts === 0).length;
    return { Progression: progression, UnitBattleSamples: rows.length, ZeroActiveCastSamples: zero,
      ZeroActiveCastRatio: zero / rows.length };
  }).sort((a, b) => a.Progression.localeCompare(b.Progression));

const summary = {
  Source: path.basename(sourcePath), SourceFingerprint: data.SourceFingerprint, GeneratedUtc: new Date().toISOString(),
  Validation: { Records: data.Records.length, Teams: data.Teams.length, Seeds: data.Seeds,
    DeterminismReplays: data.DeterminismReplays, CartesianProductComplete: true, ComparisonBudgetsConsistent: true },
  RosterCoverage: { DistinctMxUnitsAppeared: plannedMxIds.filter(id => appearanceCounts[id] > 0).length,
    MissingMxUnits: plannedMxIds.filter(id => appearanceCounts[id] === 0), UnitBattleSamples: appearanceCounts,
    ZeroActiveCastsByProgression: zeroCastByProgression },
  ProgressionComparisons: progressionGroups, TeamByEncounterKind: teamGroups, Units: unitRows,
  SummonOwnership: summonOwnership,
  Accounting: "Team Damage sums each player runtime entity exactly once. OwnedSummonDamageAttribution is a separate root-owner view and is never added to team totals.",
  Limitations: [data.Limitation,
    "Tier vectors control the authored identity/supply budget. Base stats are generated from roleStats by profession, so equal tier vectors do not establish equal realized combat strength.",
    "The 49-unit contract proves each unit can cast in its dedicated fixture. Matchup coverage and zero-cast rates below show which units actually appeared and cast in roster diagnostics; these are separate claims."],
};
fs.writeFileSync(`${outputStem}.json`, `${JSON.stringify(summary, null, 2)}\n`, "utf8");

const fmt = value => value === null ? "—" : Number(value).toFixed(1);
const markdown = [
  "# 49MX 羁绊阵容诊断汇总",
  "",
  `来源：\`${path.basename(sourcePath)}\`；${data.Records.length} 场，${data.Teams.length} 队，固定种子 ${data.Seeds.join("、")}；确定性回放 ${data.DeterminismReplays} 场。`,
  "",
  "> 固定种子、同人口同阶位、零装备／遗物预算的逻辑诊断，不代表招募可得性、自然胜率、玩家体验或最终平衡认可。",
  "",
  "## 阶段与投入方向",
  "",
  "| 阶段/体系 | 方向 | 场次 | 胜 | 负 | 超时 | 中位 ticks | 我方余血 | 敌方余血 | 敌方余盾/最大生命 | 中位伤害 | 中位治疗 | 中位护盾授予 | 中位控制授予 |",
  "|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|",
  ...progressionGroups.map(row => `| ${row.Progression} | ${row.Direction} | ${row.Samples} | ${row.Wins} | ${row.Losses} | ${row.Timeouts} | ${fmt(row.Ticks.Median)} | ${fmt(row.RemainingHealthRatio.Median * 100)}% | ${fmt(row.EnemyRemainingHealthRatio.Median * 100)}% | ${fmt(row.EnemyRemainingShieldToHealthRatio.Median * 100)}% | ${fmt(row.Damage.Median)} | ${fmt(row.EffectiveHealing.Median)} | ${fmt(row.ShieldGranted.Median)} | ${fmt(row.GrantedHardControlTicks.Median)} |`),
  "",
  "## 报告阅读顺序",
  "",
  "1. 比较 early／middle／late 中 dispersed、shallow、concentrated、cross-system 的同投入结果。",
  "2. 比较七个 `system-*` 中 deep 对 dispersed／shallow；结合实际激活标签核实成熟体系是否兑现。",
  "3. 在 JSON 的 `Units` 中检查伤害、治疗、护盾、控制、首次施法、施法次数与承伤；首次 tick 仅对实际施放样本统计，并同时给出施放率。",
  "4. 在 `SummonOwnership` 检查召唤根归属。队伍总伤害已逐运行实体计一次，根归属字段不得再次相加。",
  "5. 在 `RosterCoverage` 检查实际出场 MX、未出场 ID、各单位样本数与分进度零主动施法比例；单体夹具可释放不等于全部 49 单位都已获得阵容平衡验证。",
  "6. 单列缺失、超时和异常；数值反常返回设计判断，不通过改口径消除。",
  "",
  "阶位向量控制的是设计身份／供给预算。基础属性按职业 `roleStats` 统一起测，因此同阶位向量不保证单位实际战力等价。",
  "",
];
fs.writeFileSync(`${outputStem}.md`, `${markdown.join("\n")}\n`, "utf8");
console.log(`wrote ${outputStem}.json and ${outputStem}.md`);
