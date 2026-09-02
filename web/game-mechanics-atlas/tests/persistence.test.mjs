import assert from "node:assert/strict";
import test from "node:test";
import { createInitialState, findOrphanIds, mergeImported, migrateState, parseImport, STATE_SCHEMA_VERSION } from "../app/lib/persistence.ts";

test("migrates v1 state and preserves unknown stable ids", () => {
  const raw = {
    schemaVersion: 1,
    catalogVersion: "0.7.0",
    activeWorkspaceId: "legacy",
    workspaces: [{ id: "legacy", name: "旧方案", mechanics: {
      "legacy.removed-mechanic": { state: "candidate", notes: "必须保留", priority: 3 },
    } }],
    ui: { filters: { search: "护盾" } },
  };
  const migrated = migrateState(raw, "1.0.0");
  assert.equal(migrated.schemaVersion, STATE_SCHEMA_VERSION);
  assert.equal(migrated.workspaces[0].mechanics["legacy.removed-mechanic"].notes, "必须保留");
  assert.deepEqual(findOrphanIds(migrated, new Set(["combat.defense.shield"])), ["legacy.removed-mechanic"]);
  assert.equal(migrated.ui.filters.search, "护盾");
});

test("migrates existing v2 workspaces without inventing engine selections", () => {
  const migrated = migrateState({
    schemaVersion: 2,
    catalogVersion: "1.0.0",
    activeWorkspaceId: "workspace-a",
    workspaces: [{ id: "workspace-a", name: "旧工作区", mechanics: {} }],
    ui: { view: "engines" },
  }, "1.0.0");

  assert.deepEqual(migrated.workspaces[0].engines, {});
  assert.equal(migrated.ui.focusedEngineId, null);
});

test("preserves v3 engine choices independently in each workspace", () => {
  const migrated = migrateState({
    schemaVersion: 3,
    catalogVersion: "1.0.0",
    activeWorkspaceId: "workspace-a",
    workspaces: [
      { id: "workspace-a", name: "方案 A", mechanics: {}, engines: {
        "engine.shield-cast-loop": { inPlan: true, inComparison: false, updatedAt: "2026-08-30T00:00:00.000Z" },
      } },
      { id: "workspace-b", name: "方案 B", mechanics: {}, engines: {
        "engine.shield-cast-loop": { inPlan: false, inComparison: true, updatedAt: "2026-08-30T01:00:00.000Z" },
      } },
    ],
    ui: { view: "engines", focusedEngineId: "engine.shield-cast-loop" },
  }, "1.0.0");

  assert.deepEqual(migrated.workspaces[0].engines["engine.shield-cast-loop"], {
    inPlan: true,
    inComparison: false,
    updatedAt: "2026-08-30T00:00:00.000Z",
  });
  assert.deepEqual(migrated.workspaces[1].engines["engine.shield-cast-loop"], {
    inPlan: false,
    inComparison: true,
    updatedAt: "2026-08-30T01:00:00.000Z",
  });
  assert.equal(migrated.ui.focusedEngineId, "engine.shield-cast-loop");
});

test("snapshot migration and JSON import retain engine state", () => {
  const raw = {
    schemaVersion: 3,
    catalogVersion: "1.0.0",
    activeWorkspaceId: "workspace-a",
    workspaces: [{ id: "workspace-a", name: "当前方案", mechanics: {}, engines: {
      "engine.command-burst": { inPlan: true, inComparison: true, updatedAt: "2026-08-30T02:00:00.000Z" },
    } }],
    snapshots: [{
      id: "snapshot-a",
      name: "爆发方案快照",
      createdAt: "2026-08-30T03:00:00.000Z",
      sourceWorkspaceId: "workspace-a",
      workspace: { id: "workspace-a", name: "当前方案", mechanics: {}, engines: {
        "engine.command-burst": { inPlan: false, inComparison: true, updatedAt: "2026-08-30T01:00:00.000Z" },
      } },
    }],
    ui: { view: "engines" },
  };

  const imported = parseImport(JSON.stringify(raw), "1.0.0");
  assert.equal(imported.workspaces[0].engines["engine.command-burst"].inPlan, true);
  assert.equal(imported.workspaces[0].engines["engine.command-burst"].inComparison, true);
  assert.equal(imported.snapshots[0].workspace.engines["engine.command-burst"].inPlan, false);
  assert.equal(imported.snapshots[0].workspace.engines["engine.command-burst"].inComparison, true);
});

test("rejects malformed and future-version imports without returning partial data", () => {
  assert.throws(() => parseImport("{broken", "1.0.0"), /JSON 格式无效/);
  assert.throws(() => parseImport(JSON.stringify({ schemaVersion: 999 }), "1.0.0"), /不支持的数据结构版本/);
});

test("merge import keeps current workspaces and resolves duplicate ids", () => {
  const current = createInitialState("1.0.0");
  const incoming = structuredClone(current);
  incoming.workspaces[0].name = "导入方案";
  const merged = mergeImported(current, incoming);
  assert.equal(merged.workspaces.length, 2);
  assert.notEqual(merged.workspaces[0].id, merged.workspaces[1].id);
  assert.match(merged.workspaces[1].name, /导入/);
});
