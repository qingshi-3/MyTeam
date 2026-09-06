import assert from "node:assert/strict";
import test from "node:test";
import { traceImpact, graphBounds, fitCamera, zoomCamera, edgePath, NODE_WIDTH } from "../app/model-lab/graph.ts";
import { createFoundationWorkspace, deriveDiagnostics } from "../app/model-lab/model.ts";

async function renderModelLab() {
  const workerUrl = new URL("../dist/server/index.js", import.meta.url);
  workerUrl.searchParams.set("test", `${process.pid}-${Date.now()}-model-lab`);
  const { default: worker } = await import(workerUrl.href);
  return worker.fetch(new Request("http://localhost/model-lab", { headers: { accept: "text/html" } }), {
    ASSETS: { fetch: async () => new Response("Not found", { status: 404 }) },
  }, { waitUntil() {}, passThroughOnException() {} });
}

test("foundation model contains all 17 gameplay domains and four outer guardrails", () => {
  const workspace = createFoundationWorkspace();
  const gameplay = workspace.nodes.filter((node) => node.group !== "guardrail");
  const guardrails = workspace.nodes.filter((node) => node.group === "guardrail");
  assert.equal(gameplay.length, 17);
  assert.equal(guardrails.length, 4);
  assert.deepEqual(gameplay.map((node) => node.code), Array.from({ length: 17 }, (_, index) => `F${String(index + 1).padStart(2, "0")}`));
  assert.deepEqual(guardrails.map((node) => node.code), ["G01", "G02", "G03", "G04"]);
  assert.equal(workspace.zones.length, 6);
  assert.ok(workspace.edges.every((edge) => workspace.nodes.some((node) => node.id === edge.from) && workspace.nodes.some((node) => node.id === edge.to)));
});

test("every gameplay domain is grounded and the baseline passes diagnostics", () => {
  const workspace = createFoundationWorkspace();
  const gameplay = workspace.nodes.filter((node) => node.group !== "guardrail");
  assert.ok(gameplay.every((node) => node.evidence.length >= 2 && node.evidence.length <= 4));
  const references = gameplay.flatMap((node) => node.evidence.map((item) => item.id));
  assert.equal(references.length, 54);
  assert.equal(new Set(references).size, 49);
  assert.deepEqual(deriveDiagnostics(workspace).map((item) => item.id), ["complete"]);
});

test("diagnostics expose missing foundation domains and broken relationships", () => {
  const workspace = createFoundationWorkspace();
  workspace.nodes = workspace.nodes.filter((node) => node.id !== "f11");
  workspace.edges.push({ id: "broken-test", from: "f03", to: "missing", relation: "feeds", label: "测试" });
  const ids = new Set(deriveDiagnostics(workspace).map((item) => item.id));
  assert.ok(ids.has("missing-foundation"));
  assert.ok(ids.has("broken-edges"));
});

test("dependency scope distinguishes direct, two-step and transitive review without revisiting cycles", () => {
  const w = createFoundationWorkspace();
  const direct = traceImpact(w, "f13", 1);
  assert.deepEqual(direct.direct, ["f11", "f12"]);
  assert.deepEqual(direct.propagated, []);
  assert.equal(direct.edgeIds.length, 2);
  const two = traceImpact(w, "f13", 2);
  assert.deepEqual(new Set(two.propagated), new Set(["f10", "f03"]));
  const all = traceImpact(w, "f13");
  assert.ok(all.propagated.includes("f17"));
  assert.ok(all.propagated.length > two.propagated.length);
  assert.equal(all.depths.f13, 0);
  assert.equal(new Set([all.origin, ...all.direct, ...all.propagated]).size, 1 + all.direct.length + all.propagated.length);
  assert.equal(new Set(all.edgeIds).size, all.edgeIds.length);
});

test("dependency analysis ignores dangling edges and preserves the foundation data", () => {
  const w = createFoundationWorkspace();
  const original = structuredClone(w);
  traceImpact(w, "f13");
  assert.deepEqual(w, original);
  w.edges.push({ id:"broken", from:"f13", to:"missing", relation:"feeds", label:"test" });
  const trace = traceImpact(w, "f13");
  assert.ok(!trace.edgeIds.includes("broken"));
  assert.ok(!trace.propagated.includes("missing"));
  assert.deepEqual(traceImpact(w,"missing").direct, []);
  assert.deepEqual(traceImpact(w,"missing").channels, []);
});

test("wide-screen fit fills available space without moving draft coordinates", () => {
  const w = createFoundationWorkspace();
  const bounds = graphBounds(w);
  const wide = fitCamera(bounds, 2200, 1250);
  const laptop = fitCamera(bounds, 1280, 800);
  assert.ok(wide.zoom > laptop.zoom);
  for (const [camera,width,height] of [[wide,2200,1250],[laptop,1280,800]]) {
    assert.ok(camera.x + bounds.x * camera.zoom >= 0);
    assert.ok(camera.y + bounds.y * camera.zoom >= 0);
    assert.ok(camera.x + (bounds.x + bounds.width) * camera.zoom <= width);
    assert.ok(camera.y + (bounds.y + bounds.height) * camera.zoom <= height);
  }
  assert.deepEqual(w,createFoundationWorkspace());
});

test("zoom keeps the world point under the pointer fixed, including zoom limits", () => {
  const camera = {x:120,y:-40,zoom:0.85}, anchor = {x:725,y:420};
  const world = {x:(anchor.x-camera.x)/camera.zoom,y:(anchor.y-camera.y)/camera.zoom};
  for (const target of [1.3,99,0.01]) {
    const next = zoomCamera(camera,target,anchor);
    assert.ok(Math.abs(next.x + world.x * next.zoom - anchor.x) < 0.001);
    assert.ok(Math.abs(next.y + world.y * next.zoom - anchor.y) < 0.001);
    assert.ok(next.zoom >= 0.2 && next.zoom <= 2);
  }
});

test("edge direction starts outside the card body instead of beneath its center", () => {
  const from = createFoundationWorkspace().nodes[0];
  const to = {...from,id:"target",x:from.x+400};
  const path = edgePath(from,to,"feeds");
  assert.ok(path.startsWith(`M ${from.x+NODE_WIDTH/2} ${from.y} C`));
  assert.ok(path.endsWith(`${to.x-NODE_WIDTH/2-5} ${to.y}`));
});

test("model lab route server-renders the foundation-rule product shell", async () => {
  const response = await renderModelLab();
  assert.equal(response.status, 200);
  const html = await response.text();
  assert.match(html, /游戏基础规则网络/);
  assert.match(html, /F01/);
  assert.match(html, /F17/);
  assert.doesNotMatch(html, /护盾 × 冰|冰盾|土盾/);
});
