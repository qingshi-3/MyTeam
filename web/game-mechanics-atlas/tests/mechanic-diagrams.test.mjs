import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";
import { mechanics } from "../app/data/atlas.ts";
import { mechanicDiagramDefinitions, mechanicDiagramIds } from "../app/data/mechanicDiagrams.ts";
import { STATE_SCHEMA_VERSION, STORAGE_KEY } from "../app/lib/persistence.ts";

const expectedIds = [
  "battlefield.targeting.threat-lock",
  "battlefield.shape.cone",
  "battlefield.shape.line-pierce",
  "battlefield.engagement.intercept",
  "battlefield.engagement.charge",
  "combat.defense.shield",
  "combat.summon.corpse",
  "combat.trigger.overheal",
  "army.role.aoe-casters",
  "combat.damage.armor-break",
  "encounter.template.backline-hunter",
  "encounter.boss.timeline",
];

test("semantic diagrams cover the twelve exact catalog ids with unique visual kinds", () => {
  assert.equal(mechanics.length, 72, "the authored catalog must remain unchanged");
  assert.deepEqual([...mechanicDiagramIds], expectedIds);
  assert.equal(Object.keys(mechanicDiagramDefinitions).length, 12);
  assert.equal(new Set(Object.values(mechanicDiagramDefinitions).map((item) => item.kind)).size, 12);
  const catalogIds = new Set(mechanics.map((mechanic) => mechanic.id));
  for (const id of expectedIds) {
    assert.ok(catalogIds.has(id), `catalog is missing ${id}`);
    assert.equal(mechanicDiagramDefinitions[id].id, id);
  }
});

test("every prototype documents actor, target, affected space, path, and before-after semantics", () => {
  for (const definition of Object.values(mechanicDiagramDefinitions)) {
    assert.ok(definition.ariaDescription.length > 20, `${definition.id} needs an accessible description`);
    assert.ok(definition.caption.length > 20, `${definition.id} needs a semantic caption`);
    assert.equal(definition.steps.length, 3);
    for (const [key, value] of Object.entries(definition.audit)) {
      assert.ok(value.length > 0, `${definition.id} is missing audit.${key}`);
    }
  }
});

test("the three counter prototypes point pressure to real answer mechanics", () => {
  const counterIds = ["army.role.aoe-casters", "combat.damage.armor-break", "encounter.template.backline-hunter"];
  const catalogIds = new Set(mechanics.map((mechanic) => mechanic.id));
  for (const id of counterIds) {
    const counter = mechanicDiagramDefinitions[id].counter;
    assert.ok(counter, `${id} needs a counter direction`);
    assert.ok(counter.pressure && counter.answer);
    assert.ok(catalogIds.has(counter.targetId), `${counter.targetId} must be a real relationship target`);
  }
});

test("diagram component is code-native, accessible, controllable, and reduced-motion safe", async () => {
  const [component, atlasApp, css] = await Promise.all([
    readFile(new URL("../app/components/MechanicDiagram.tsx", import.meta.url), "utf8"),
    readFile(new URL("../app/components/AtlasApp.tsx", import.meta.url), "utf8"),
    readFile(new URL("../app/globals.css", import.meta.url), "utf8"),
  ]);
  assert.match(component, /role="img" aria-label=\{definition\.ariaDescription\}/);
  assert.match(component, /aria-pressed=\{playing\}/);
  assert.match(component, /播放演示/);
  assert.match(component, /暂停演示/);
  assert.match(component, /left="58\.333%" top="87\.5%" width=\{mode === "detail" \? "60\.1%" : "55%"\} angle=\{mode === "detail" \? -56\.3 : -52\.7\} className="answer reverse motion-3"/);
  assert.doesNotMatch(component, /width="82%" angle=\{-63\}/);
  assert.doesNotMatch(component, /<(?:svg|canvas|img)\b/i);
  assert.match(atlasApp, /<MechanicDiagram mechanicId=\{mechanic\.id\} \/>/);
  assert.match(atlasApp, /<MechanicDiagram mechanicId=\{mechanic\.id\} mode="detail" onNavigate=\{navigate\} \/>/);
  assert.match(atlasApp, /!illustrated && <blockquote>/);
  for (const detailLabel of ["规则想法", "玩家会判断什么", "平衡风险", "模拟 / 实现", "克制 / 回答"]) assert.match(atlasApp, new RegExp(detailLabel));
  assert.match(css, /@media \(prefers-reduced-motion: reduce\)/);
  assert.match(css, /\.mechanic-diagram\.is-playing \.motion-1/);
  assert.match(css, /animation: none !important/);
});

test("mechanic library uses compact filters, separated results, readable type, and content-sized cards", async () => {
  const [atlasApp, css] = await Promise.all([
    readFile(new URL("../app/components/AtlasApp.tsx", import.meta.url), "utf8"),
    readFile(new URL("../app/globals.css", import.meta.url), "utf8"),
  ]);
  assert.match(atlasApp, /<header className="library-head">/);
  assert.match(atlasApp, /<details className="advanced-filters"><summary>更多筛选/);
  assert.match(atlasApp, /id="illustrated-results-title">图解机制/);
  assert.match(atlasApp, /id="text-results-title">文字机制/);
  assert.doesNotMatch(css, /\.card-body[^{}]*min-height/i);
  assert.doesNotMatch(css, /\.card-body\s*>\s*p[^{}]*min-height/i);
  assert.doesNotMatch(css, /\.card-body\s+blockquote[^{}]*min-height/i);
  assert.match(css, /\.card-body\s*>\s*p\s*\{[^{}]*font-size:\s*14px/i);
  assert.match(css, /\.card-top\s*\{[^{}]*font-size:\s*11px/i);
  assert.match(css, /\.tag-line span\s*\{[^{}]*font-size:\s*11px/i);
  assert.match(css, /\.card-body h2\s*\{[^{}]*22px/i);
});

test("exactly three representative diagrams use explicit before-after storyboards", async () => {
  const component = await readFile(new URL("../app/components/MechanicDiagram.tsx", import.meta.url), "utf8");
  const storyboardDeclaration = component.match(/export const storyboardKinds:[\s\S]*?\];/);
  assert.ok(storyboardDeclaration, "storyboard registry must be explicit");
  const kinds = [...storyboardDeclaration[0].matchAll(/"([a-z-]+)"/g)].map((match) => match[1]);
  assert.deepEqual(kinds, ["threat-redirection", "charge-displacement", "aoe-counter"]);
  for (const label of ["命中前", "命中后", "原目标", "嘲讽后", "首次接敌", "沿冲锋方向击退1格", "阻挡 / 边缘", "不位移 · 碰撞 / 眩晕", "范围内同时结算"]) {
    assert.match(component, new RegExp(label));
  }
  assert.match(component, /<DiagramLabel x=\{25\} y=\{65\} tone="danger" className="blocked-branch-label">阻挡 \/ 边缘<\/DiagramLabel>/);
  assert.match(component, /<DiagramLabel x=\{24\} y=\{86\} tone="danger" className="blocked-outcome-label">不位移 · 碰撞 \/ 眩晕<\/DiagramLabel>/);
  assert.doesNotMatch(component, /<DiagramLabel x=\{8\} y=\{69\}[^>]*>阻挡 \/ 边缘/);
  assert.doesNotMatch(component, /<DiagramLabel x=\{50\} y=\{89\}[^>]*>不位移 · 碰撞 \/ 眩晕/);
  assert.doesNotMatch(component, /侧抛|出界移除|连锁推移/);
});

test("AOE defeat storyboard is explicitly limited to dense low-health enemies", async () => {
  const component = await readFile(new URL("../app/components/MechanicDiagram.tsx", import.meta.url), "utf8");
  const definition = mechanicDiagramDefinitions["army.role.aoe-casters"];
  assert.match(component, />密集低生命敌群<\/DiagramLabel>/);
  assert.doesNotMatch(component, />密集敌群<\/DiagramLabel>/);
  assert.match(definition.ariaDescription, /密集低生命敌群/);
  assert.match(definition.caption, /低生命敌群/);
  for (const step of definition.steps) assert.match(step, /低生命/);
  assert.match(definition.audit.target, /密集低生命敌群/);
  assert.match(definition.audit.beforeAfter, /低生命敌群/);
  assert.match(definition.counter.pressure, /密集低生命敌群压力/);
});

test("README preserves the exploratory visual-grammar boundary", async () => {
  const readme = await readFile(new URL("../README.md", import.meta.url), "utf8");
  assert.match(readme, /机制图解的视觉语法边界/);
  assert.match(readme, /前态与后态/);
  assert.match(readme, /未确认的规则分支不得在图中静默发明/);
  assert.match(readme, /不会把候选想法提升/);
  assert.match(readme, /其余九类原型只继承共享/);
});

test("semantic diagrams do not change the device-local persistence contract", () => {
  assert.equal(STATE_SCHEMA_VERSION, 3);
  assert.equal(STORAGE_KEY, "tower-atlas.workspace.v2");
});
