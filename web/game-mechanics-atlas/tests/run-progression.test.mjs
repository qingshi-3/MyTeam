import assert from "node:assert/strict";
import { access, readFile } from "node:fs/promises";
import test from "node:test";
import { runProgressionConcepts } from "../app/data/runProgression.ts";

const mechanismFields = [
  "randomizedProblem",
  "playerAction",
  "buildPayoff",
  "progressionPressure",
  "terminalCondition",
  "principalRisk",
];

async function render(pathname) {
  const workerUrl = new URL("../dist/server/index.js", import.meta.url);
  workerUrl.searchParams.set("test", `${process.pid}-${Date.now()}-${pathname}`);
  const { default: worker } = await import(workerUrl.href);
  return worker.fetch(new Request(`http://localhost${pathname}`, { headers: { accept: "text/html" } }), {
    ASSETS: { fetch: async () => new Response("Not found", { status: 404 }) },
  }, { waitUntil() {}, passThroughOnException() {} });
}

test("run progression data contains ten complete frameworks and twenty unique local images", async () => {
  assert.equal(runProgressionConcepts.length, 10);
  assert.equal(new Set(runProgressionConcepts.map((concept) => concept.id)).size, 10);

  const images = runProgressionConcepts.flatMap((concept) => [concept.overviewImage, concept.loopImage]);
  assert.equal(images.length, 20);
  assert.equal(new Set(images).size, 20);

  for (const concept of runProgressionConcepts) {
    for (const field of mechanismFields) {
      assert.ok(concept[field].length >= 20, `${concept.id}.${field} must remain a complete concise explanation`);
    }
    for (const image of [concept.overviewImage, concept.loopImage]) {
      assert.match(image, /^\/run-progression\/[a-z0-9-]+\.png$/);
      await access(new URL(`../public${image}`, import.meta.url));
    }
  }
});

test("run progression route server-renders its standalone product shell", async () => {
  const response = await render("/run-progression");
  assert.equal(response.status, 200);
  assert.match(response.headers.get("content-type") ?? "", /^text\/html\b/i);
  const html = await response.text();
  assert.match(html, /<title>十种局内进程机制｜塔军机制设计图谱<\/title>/i);
  assert.match(html, /十种完整局内进程/);
  assert.match(html, /同一套战斗核心，十种推进方式/);
  assert.match(html, /领土攻伐战役/);
  assert.match(html, /随机题目/);
  assert.match(html, /构筑收益/);
  assert.match(html, /主要风险/);
});

test("browser mounts only the selected framework images and exposes accessible navigation", async () => {
  const [browser, atlas] = await Promise.all([
    readFile(new URL("../app/run-progression/RunProgressionBrowser.tsx", import.meta.url), "utf8"),
    readFile(new URL("../app/components/AtlasApp.tsx", import.meta.url), "utf8"),
  ]);
  assert.match(browser, /src=\{selected\.overviewImage\}/);
  assert.match(browser, /src=\{selected\.loopImage\}/);
  assert.doesNotMatch(browser, /runProgressionConcepts\.map[\s\S]{0,500}<img\b/);
  assert.match(browser, /loading="lazy"/);
  assert.match(browser, /role="tablist"/);
  assert.match(browser, /aria-selected=\{index === selectedIndex\}/);
  assert.match(browser, /aria-label="上一套局内进程"/);
  assert.match(browser, /aria-label="下一套局内进程"/);
  assert.match(browser, /role="dialog" aria-modal="true"/);
  assert.match(browser, /event\.key === "Escape"/);
  assert.match(atlas, /href="\/run-progression"/);
});
