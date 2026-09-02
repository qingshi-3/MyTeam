import assert from "node:assert/strict";
import test from "node:test";
import { readFile } from "node:fs/promises";

async function render() {
  const workerUrl = new URL("../dist/server/index.js", import.meta.url);
  workerUrl.searchParams.set("test", `${process.pid}-${Date.now()}`);
  const { default: worker } = await import(workerUrl.href);
  return worker.fetch(new Request("http://localhost/", { headers: { accept: "text/html" } }), {
    ASSETS: { fetch: async () => new Response("Not found", { status: 404 }) },
  }, { waitUntil() {}, passThroughOnException() {} });
}

test("server renders the product shell and Chinese metadata", async () => {
  const response = await render();
  assert.equal(response.status, 200);
  assert.match(response.headers.get("content-type") ?? "", /^text\/html\b/i);
  const html = await response.text();
  assert.match(html, /<html[^>]+lang="zh-CN"/i);
  assert.match(html, /<title>塔军机制设计图谱<\/title>/i);
  assert.match(html, /<meta(?=[^>]*name="robots")(?=[^>]*content="noindex, nofollow, nocache")[^>]*>/i);
  assert.match(html, /<meta(?=[^>]*name="googlebot")(?=[^>]*content="noindex, nofollow, noimageindex")[^>]*>/i);
  assert.match(html, /正在恢复你的设计工作区/);
  assert.doesNotMatch(html, /codex-preview|Your site is taking shape|react-loading-skeleton/i);
});

test("build-engine source exposes master-detail and full comparison actions", async () => {
  const source = await readFile(new URL("../app/components/AtlasApp.tsx", import.meta.url), "utf8");
  for (const label of ["引擎概览", "查看详情", "加入当前方案", "加入比较", "打开比较", "关闭比较，返回实验台", "聚焦查看完整详情", "从比较中移除", "成型组件", "断链风险", "模拟安全边界"]) {
    assert.match(source, new RegExp(label));
  }
  assert.match(source, /aria-pressed=\{choice\.inPlan\}/);
  assert.match(source, /aria-pressed=\{choice\.inComparison\}/);
  assert.match(source, /engine\.requirements\.map/);
  assert.match(source, /engine\.breaks\.map/);
  assert.match(source, /engine\.safeguards\.map/);
});
