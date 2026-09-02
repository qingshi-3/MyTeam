import type { DecisionState } from "../data/atlas";

export const STATE_SCHEMA_VERSION = 3;
export const STORAGE_KEY = "tower-atlas.workspace.v2";

export type MechanicChoice = {
  state: DecisionState;
  notes: string;
  tags: string[];
  priority: 0 | 1 | 2 | 3;
  updatedAt: string;
};

export type EngineWorkspaceChoice = {
  inPlan: boolean;
  inComparison: boolean;
  updatedAt: string;
};

export type Workspace = {
  id: string;
  name: string;
  createdAt: string;
  updatedAt: string;
  mechanics: Record<string, MechanicChoice>;
  engines: Record<string, EngineWorkspaceChoice>;
};

export type Filters = {
  search: string;
  domain: string;
  decision: string;
  dependency: string;
  agency: string;
  complexity: string;
  depth: string;
};

export type AtlasState = {
  schemaVersion: number;
  catalogVersion: string;
  revision: number;
  updatedAt: string;
  activeWorkspaceId: string;
  workspaces: Workspace[];
  snapshots: Array<{ id: string; name: string; createdAt: string; sourceWorkspaceId: string; workspace: Workspace }>;
  ui: {
    view: "overview" | "dimensions" | "mechanics" | "engines" | "bosses" | "decisions";
    filters: Filters;
    expandedDomains: string[];
    selectedMechanicId: string | null;
    focusedEngineId: string | null;
    scrollY: number;
  };
};

const isoNow = () => new Date().toISOString();
export const makeId = (prefix: string) => `${prefix}-${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 8)}`;

export function createWorkspace(name = "基础方案"): Workspace {
  const now = isoNow();
  return { id: makeId("workspace"), name, createdAt: now, updatedAt: now, mechanics: {}, engines: {} };
}

export function createInitialState(catalogVersion: string): AtlasState {
  const workspace = createWorkspace();
  return {
    schemaVersion: STATE_SCHEMA_VERSION,
    catalogVersion,
    revision: 1,
    updatedAt: isoNow(),
    activeWorkspaceId: workspace.id,
    workspaces: [workspace],
    snapshots: [],
    ui: {
      view: "overview",
      filters: { search: "", domain: "", decision: "", dependency: "", agency: "", complexity: "", depth: "" },
      expandedDomains: [],
      selectedMechanicId: null,
      focusedEngineId: null,
      scrollY: 0,
    },
  };
}

function isObject(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function normalizeChoice(raw: unknown): MechanicChoice | null {
  if (!isObject(raw)) return null;
  const allowed = ["unreviewed", "interested", "candidate", "confirmed", "deferred", "excluded"];
  const state = allowed.includes(String(raw.state)) ? raw.state as DecisionState : "unreviewed";
  return {
    state,
    notes: typeof raw.notes === "string" ? raw.notes.slice(0, 20000) : "",
    tags: Array.isArray(raw.tags) ? raw.tags.filter((tag): tag is string => typeof tag === "string").slice(0, 20) : [],
    priority: [0, 1, 2, 3].includes(Number(raw.priority)) ? Number(raw.priority) as 0 | 1 | 2 | 3 : 0,
    updatedAt: typeof raw.updatedAt === "string" ? raw.updatedAt : isoNow(),
  };
}

function normalizeEngineChoice(raw: unknown): EngineWorkspaceChoice | null {
  if (!isObject(raw)) return null;
  return {
    inPlan: raw.inPlan === true,
    inComparison: raw.inComparison === true,
    updatedAt: typeof raw.updatedAt === "string" ? raw.updatedAt : isoNow(),
  };
}

function normalizeWorkspace(raw: unknown, fallbackName: string): Workspace | null {
  if (!isObject(raw)) return null;
  const mechanics: Record<string, MechanicChoice> = {};
  const engines: Record<string, EngineWorkspaceChoice> = {};
  if (isObject(raw.mechanics)) {
    for (const [id, value] of Object.entries(raw.mechanics)) {
      if (!/^[a-z0-9][a-z0-9.-]+$/.test(id)) continue;
      const choice = normalizeChoice(value);
      if (choice) mechanics[id] = choice;
    }
  }
  if (isObject(raw.engines)) {
    for (const [id, value] of Object.entries(raw.engines)) {
      if (!/^[a-z0-9][a-z0-9.-]+$/.test(id)) continue;
      const choice = normalizeEngineChoice(value);
      if (choice) engines[id] = choice;
    }
  }
  const now = isoNow();
  return {
    id: typeof raw.id === "string" && raw.id ? raw.id : makeId("workspace"),
    name: typeof raw.name === "string" && raw.name.trim() ? raw.name.trim().slice(0, 80) : fallbackName,
    createdAt: typeof raw.createdAt === "string" ? raw.createdAt : now,
    updatedAt: typeof raw.updatedAt === "string" ? raw.updatedAt : now,
    mechanics,
    engines,
  };
}

/** Migrate historical device-local exports without discarding unknown mechanic ids. */
export function migrateState(raw: unknown, catalogVersion: string): AtlasState {
  if (!isObject(raw)) throw new Error("文件不是有效的设计图谱数据。");
  const version = Number(raw.schemaVersion ?? 1);
  if (!Number.isInteger(version) || version < 1 || version > STATE_SCHEMA_VERSION) {
    throw new Error(`不支持的数据结构版本：${String(raw.schemaVersion)}`);
  }
  const workspacesRaw = Array.isArray(raw.workspaces) ? raw.workspaces : [];
  const workspaces = workspacesRaw.map((item, index) => normalizeWorkspace(item, `迁移方案 ${index + 1}`)).filter((item): item is Workspace => Boolean(item));
  if (workspaces.length === 0) workspaces.push(createWorkspace());
  const activeWorkspaceId = typeof raw.activeWorkspaceId === "string" && workspaces.some((workspace) => workspace.id === raw.activeWorkspaceId)
    ? raw.activeWorkspaceId : workspaces[0].id;
  const uiRaw = isObject(raw.ui) ? raw.ui : {};
  const filtersRaw = isObject(uiRaw.filters) ? uiRaw.filters : {};
  const views = ["overview", "dimensions", "mechanics", "engines", "bosses", "decisions"];
  const snapshotsRaw = Array.isArray(raw.snapshots) ? raw.snapshots : [];
  const snapshots: AtlasState["snapshots"] = snapshotsRaw.flatMap((item, index) => {
    if (!isObject(item)) return [];
    const workspace = normalizeWorkspace(item.workspace, `快照 ${index + 1}`);
    if (!workspace) return [];
    return [{
      id: typeof item.id === "string" ? item.id : makeId("snapshot"),
      name: typeof item.name === "string" && item.name.trim() ? item.name.trim().slice(0, 80) : `快照 ${index + 1}`,
      createdAt: typeof item.createdAt === "string" ? item.createdAt : isoNow(),
      sourceWorkspaceId: typeof item.sourceWorkspaceId === "string" ? item.sourceWorkspaceId : workspace.id,
      workspace,
    }];
  });
  return {
    schemaVersion: STATE_SCHEMA_VERSION,
    catalogVersion: typeof raw.catalogVersion === "string" ? raw.catalogVersion : catalogVersion,
    revision: Number.isFinite(Number(raw.revision)) ? Math.max(1, Number(raw.revision)) : 1,
    updatedAt: typeof raw.updatedAt === "string" ? raw.updatedAt : isoNow(),
    activeWorkspaceId,
    workspaces,
    snapshots,
    ui: {
      view: views.includes(String(uiRaw.view)) ? uiRaw.view as AtlasState["ui"]["view"] : "overview",
      filters: {
        search: typeof filtersRaw.search === "string" ? filtersRaw.search.slice(0, 200) : "",
        domain: typeof filtersRaw.domain === "string" ? filtersRaw.domain : "",
        decision: typeof filtersRaw.decision === "string" ? filtersRaw.decision : "",
        dependency: typeof filtersRaw.dependency === "string" ? filtersRaw.dependency : "",
        agency: typeof filtersRaw.agency === "string" ? filtersRaw.agency : "",
        complexity: typeof filtersRaw.complexity === "string" ? filtersRaw.complexity : "",
        depth: typeof filtersRaw.depth === "string" ? filtersRaw.depth : "",
      },
      expandedDomains: Array.isArray(uiRaw.expandedDomains) ? uiRaw.expandedDomains.filter((id): id is string => typeof id === "string") : [],
      selectedMechanicId: typeof uiRaw.selectedMechanicId === "string" ? uiRaw.selectedMechanicId : null,
      focusedEngineId: typeof uiRaw.focusedEngineId === "string" ? uiRaw.focusedEngineId : null,
      scrollY: Number.isFinite(Number(uiRaw.scrollY)) ? Math.max(0, Number(uiRaw.scrollY)) : 0,
    },
  };
}

export function parseImport(text: string, catalogVersion: string): AtlasState {
  if (text.length > 8_000_000) throw new Error("导入文件过大，已停止处理。");
  let raw: unknown;
  try { raw = JSON.parse(text); } catch { throw new Error("JSON 格式无效，请检查文件是否完整。"); }
  return migrateState(raw, catalogVersion);
}

export function mergeImported(current: AtlasState, incoming: AtlasState): AtlasState {
  const existingIds = new Set(current.workspaces.map((workspace) => workspace.id));
  const imported = incoming.workspaces.map((workspace) => existingIds.has(workspace.id)
    ? { ...workspace, id: makeId("workspace"), name: `${workspace.name}（导入）` }
    : workspace);
  return {
    ...current,
    workspaces: [...current.workspaces, ...imported],
    snapshots: [...current.snapshots, ...incoming.snapshots.map((snapshot) => ({ ...snapshot, id: makeId("snapshot") }))],
    activeWorkspaceId: imported[0]?.id ?? current.activeWorkspaceId,
    revision: current.revision + 1,
    updatedAt: isoNow(),
  };
}

export function findOrphanIds(state: AtlasState, catalogIds: Set<string>): string[] {
  const ids = new Set<string>();
  for (const workspace of state.workspaces) {
    for (const id of Object.keys(workspace.mechanics)) if (!catalogIds.has(id)) ids.add(id);
  }
  return [...ids].sort();
}

export function touchState(state: AtlasState): AtlasState {
  return { ...state, revision: state.revision + 1, updatedAt: isoNow(), schemaVersion: STATE_SCHEMA_VERSION };
}
