"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import Link from "next/link";
import { bossTests, capabilityLabels, catalogVersion, domains, engines, mechanics, type DecisionState, type Mechanic } from "../data/atlas";
import { getMechanicDiagram } from "../data/mechanicDiagrams";
import { createInitialState, createWorkspace, findOrphanIds, makeId, mergeImported, migrateState, parseImport, STATE_SCHEMA_VERSION, STORAGE_KEY, touchState, type AtlasState, type EngineWorkspaceChoice, type MechanicChoice, type Workspace } from "../lib/persistence";
import { MechanicDiagram } from "./MechanicDiagram";

const decisionMeta: Record<DecisionState, { label: string; short: string }> = {
  unreviewed: { label: "未评估", short: "未" }, interested: { label: "感兴趣", short: "趣" },
  candidate: { label: "候选", short: "选" }, confirmed: { label: "图谱内确认", short: "确" },
  deferred: { label: "暂缓", short: "缓" }, excluded: { label: "排除", short: "排" },
};
const viewMeta: Array<{ id: AtlasState["ui"]["view"]; label: string; hint: string }> = [
  { id: "overview", label: "总览", hint: "坐标系" }, { id: "dimensions", label: "维度树", hint: "拆分空间" },
  { id: "mechanics", label: "机制库", hint: "筛选比较" }, { id: "engines", label: "构筑引擎", hint: "组合循环" },
  { id: "bosses", label: "Boss 试卷", hint: "压力与答案" }, { id: "decisions", label: "决策台", hint: "工作区与恢复" },
];
const agencyLabels: Record<string, string> = { prepare: "战前准备", automatic: "自动触发", command: "有限指令", movement: "移动干预" };
const dependencyLabels: Record<string, string> = { baseline: "空白网格基线", "special-map": "需要特殊地图", "movement-intervention": "需要移动干预" };
const complexityLabels: Record<string, string> = { low: "低复杂度", medium: "中复杂度", high: "高复杂度" };
const depthLabels: Record<string, string> = { space: "空间", time: "时间", resource: "资源", build: "构筑", information: "信息" };

function choiceFor(workspace: Workspace, id: string): MechanicChoice {
  return workspace.mechanics[id] ?? { state: "unreviewed", notes: "", tags: [], priority: 0, updatedAt: new Date(0).toISOString() };
}

function engineChoiceFor(workspace: Workspace, id: string): EngineWorkspaceChoice {
  return workspace.engines[id] ?? { inPlan: false, inComparison: false, updatedAt: new Date(0).toISOString() };
}

function formatTime(value: string) {
  try { return new Intl.DateTimeFormat("zh-CN", { month: "2-digit", day: "2-digit", hour: "2-digit", minute: "2-digit" }).format(new Date(value)); }
  catch { return "—"; }
}

export default function AtlasApp() {
  const [state, setState] = useState<AtlasState>(() => createInitialState(catalogVersion));
  const [hydrated, setHydrated] = useState(false);
  const [saveStatus, setSaveStatus] = useState<"loading" | "saving" | "saved" | "error">("loading");
  const [external, setExternal] = useState<AtlasState | null>(null);
  const [pendingImport, setPendingImport] = useState<AtlasState | null>(null);
  const [toast, setToast] = useState("");
  const fileInput = useRef<HTMLInputElement>(null);
  const saveTimer = useRef<ReturnType<typeof setTimeout> | null>(null);
  const catalogIds = useMemo(() => new Set(mechanics.map((mechanic) => mechanic.id)), []);
  const activeWorkspace = state.workspaces.find((workspace) => workspace.id === state.activeWorkspaceId) ?? state.workspaces[0];

  useEffect(() => {
    queueMicrotask(() => {
      try {
        const stored = window.localStorage.getItem(STORAGE_KEY);
        if (stored) setState(migrateState(JSON.parse(stored), catalogVersion));
      } catch { setToast("本地数据读取失败，已打开安全的空白工作区。可通过 JSON 备份恢复。"); }
      finally { setHydrated(true); setSaveStatus("saved"); }
    });
  }, []);

  useEffect(() => {
    if (!hydrated) return;
    queueMicrotask(() => setSaveStatus("saving"));
    if (saveTimer.current) clearTimeout(saveTimer.current);
    saveTimer.current = setTimeout(() => {
      try {
        window.localStorage.setItem(STORAGE_KEY, JSON.stringify(state));
        setSaveStatus("saved");
      } catch { setSaveStatus("error"); }
    }, 320);
    return () => { if (saveTimer.current) clearTimeout(saveTimer.current); };
  }, [state, hydrated]);

  useEffect(() => {
    if (!hydrated) return;
    const onStorage = (event: StorageEvent) => {
      if (event.key !== STORAGE_KEY || !event.newValue) return;
      try {
        const incoming = migrateState(JSON.parse(event.newValue), catalogVersion);
        if (incoming.revision > state.revision && incoming.updatedAt !== state.updatedAt) setExternal(incoming);
      } catch { /* another tab wrote invalid state; keep current */ }
    };
    window.addEventListener("storage", onStorage);
    return () => window.removeEventListener("storage", onStorage);
  }, [hydrated, state.revision, state.updatedAt]);

  useEffect(() => {
    if (!hydrated) return;
    const target = state.ui.scrollY;
    requestAnimationFrame(() => window.scrollTo({ top: target, behavior: "instant" }));
    // Resume position is restored once; subsequent navigation intentionally starts at the top.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [hydrated]);

  useEffect(() => {
    if (!hydrated) return;
    let timer: ReturnType<typeof setTimeout> | null = null;
    const onScroll = () => {
      if (timer) clearTimeout(timer);
      timer = setTimeout(() => setState((current) => Math.abs(current.ui.scrollY - window.scrollY) < 80 ? current : touchState({ ...current, ui: { ...current.ui, scrollY: Math.round(window.scrollY) } })), 220);
    };
    window.addEventListener("scroll", onScroll, { passive: true });
    return () => { window.removeEventListener("scroll", onScroll); if (timer) clearTimeout(timer); };
  }, [hydrated]);

  useEffect(() => {
    if (!toast) return;
    const timer = setTimeout(() => setToast(""), 5200);
    return () => clearTimeout(timer);
  }, [toast]);

  const mutate = (recipe: (current: AtlasState) => AtlasState) => setState((current) => touchState(recipe(current)));
  const setView = (view: AtlasState["ui"]["view"]) => {
    mutate((current) => ({ ...current, ui: { ...current.ui, view, scrollY: 0 } }));
    window.scrollTo({ top: 0, behavior: "smooth" });
  };
  const updateChoice = (mechanicId: string, patch: Partial<MechanicChoice>) => mutate((current) => ({
    ...current,
    workspaces: current.workspaces.map((workspace) => workspace.id !== current.activeWorkspaceId ? workspace : {
      ...workspace, updatedAt: new Date().toISOString(), mechanics: {
        ...workspace.mechanics,
        [mechanicId]: { ...choiceFor(workspace, mechanicId), ...patch, updatedAt: new Date().toISOString() },
      },
    }),
  }));
  const updateEngineChoice = (engineId: string, patch: Partial<EngineWorkspaceChoice>) => mutate((current) => ({
    ...current,
    workspaces: current.workspaces.map((workspace) => workspace.id !== current.activeWorkspaceId ? workspace : {
      ...workspace,
      updatedAt: new Date().toISOString(),
      engines: {
        ...workspace.engines,
        [engineId]: { ...engineChoiceFor(workspace, engineId), ...patch, updatedAt: new Date().toISOString() },
      },
    }),
  }));
  const focusEngine = (engineId: string) => mutate((current) => ({ ...current, ui: { ...current.ui, focusedEngineId: engineId } }));
  const openMechanic = (mechanicId: string) => mutate((current) => ({ ...current, ui: { ...current.ui, view: "mechanics", selectedMechanicId: mechanicId, scrollY: 0 } }));
  const closeMechanic = () => mutate((current) => ({ ...current, ui: { ...current.ui, selectedMechanicId: null } }));
  const updateFilter = (key: keyof AtlasState["ui"]["filters"], value: string) => mutate((current) => ({ ...current, ui: { ...current.ui, filters: { ...current.ui.filters, [key]: value }, scrollY: 0 } }));
  const clearFilters = () => mutate((current) => ({ ...current, ui: { ...current.ui, filters: { search: "", domain: "", decision: "", dependency: "", agency: "", complexity: "", depth: "" } } }));

  const filteredMechanics = useMemo(() => mechanics.filter((mechanic) => {
    const filter = state.ui.filters;
    const choice = choiceFor(activeWorkspace, mechanic.id);
    const haystack = `${mechanic.title} ${mechanic.id} ${mechanic.subdimension} ${mechanic.premise} ${mechanic.rule} ${mechanic.decision}`.toLowerCase();
    return (!filter.search || haystack.includes(filter.search.toLowerCase()))
      && (!filter.domain || mechanic.domainId === filter.domain)
      && (!filter.decision || choice.state === filter.decision)
      && (!filter.dependency || mechanic.dependency === filter.dependency)
      && (!filter.agency || mechanic.agency === filter.agency)
      && (!filter.complexity || mechanic.complexity === filter.complexity)
      && (!filter.depth || mechanic.depthTypes.includes(filter.depth as never));
  }), [state.ui.filters, activeWorkspace]);

  const selectedMechanic = mechanics.find((mechanic) => mechanic.id === state.ui.selectedMechanicId) ?? null;
  const counts = useMemo(() => Object.keys(decisionMeta).reduce((acc, key) => {
    acc[key as DecisionState] = mechanics.filter((mechanic) => choiceFor(activeWorkspace, mechanic.id).state === key).length;
    return acc;
  }, {} as Record<DecisionState, number>), [activeWorkspace]);
  const evaluated = mechanics.length - counts.unreviewed;
  const orphans = findOrphanIds(state, catalogIds);

  const createNewWorkspace = () => {
    const name = window.prompt("新工作区名称", `方案 ${state.workspaces.length + 1}`)?.trim();
    if (!name) return;
    const workspace = createWorkspace(name);
    mutate((current) => ({ ...current, workspaces: [...current.workspaces, workspace], activeWorkspaceId: workspace.id }));
  };
  const duplicateWorkspace = () => {
    const name = window.prompt("副本名称", `${activeWorkspace.name}（副本）`)?.trim();
    if (!name) return;
    const now = new Date().toISOString();
    const workspace: Workspace = { ...structuredClone(activeWorkspace), id: makeId("workspace"), name, createdAt: now, updatedAt: now };
    mutate((current) => ({ ...current, workspaces: [...current.workspaces, workspace], activeWorkspaceId: workspace.id }));
  };
  const renameWorkspace = () => {
    const name = window.prompt("工作区新名称", activeWorkspace.name)?.trim();
    if (!name || name === activeWorkspace.name) return;
    mutate((current) => ({ ...current, workspaces: current.workspaces.map((workspace) => workspace.id === activeWorkspace.id ? { ...workspace, name, updatedAt: new Date().toISOString() } : workspace) }));
  };
  const deleteWorkspace = () => {
    if (state.workspaces.length === 1) { setToast("至少保留一个工作区；如需清空，请使用“重置本机数据”。"); return; }
    if (!window.confirm(`删除工作区“${activeWorkspace.name}”？该操作不可撤销，建议先导出备份。`)) return;
    mutate((current) => {
      const workspaces = current.workspaces.filter((workspace) => workspace.id !== activeWorkspace.id);
      return { ...current, workspaces, activeWorkspaceId: workspaces[0].id };
    });
  };
  const createSnapshot = () => {
    const name = window.prompt("快照名称", `${activeWorkspace.name} · ${new Date().toLocaleDateString("zh-CN")}`)?.trim();
    if (!name) return;
    mutate((current) => ({ ...current, snapshots: [{ id: makeId("snapshot"), name, createdAt: new Date().toISOString(), sourceWorkspaceId: activeWorkspace.id, workspace: structuredClone(activeWorkspace) }, ...current.snapshots] }));
    setToast("快照已保存。它不会随当前工作区继续变化。");
  };
  const restoreSnapshot = (snapshotId: string) => {
    const snapshot = state.snapshots.find((item) => item.id === snapshotId);
    if (!snapshot || !window.confirm(`将快照“${snapshot.name}”恢复为一个新工作区？当前工作区不会被覆盖。`)) return;
    const now = new Date().toISOString();
    const workspace = { ...structuredClone(snapshot.workspace), id: makeId("workspace"), name: `${snapshot.name}（恢复）`, createdAt: now, updatedAt: now };
    mutate((current) => ({ ...current, workspaces: [...current.workspaces, workspace], activeWorkspaceId: workspace.id }));
  };
  const deleteSnapshot = (snapshotId: string) => {
    const snapshot = state.snapshots.find((item) => item.id === snapshotId);
    if (!snapshot || !window.confirm(`永久删除快照“${snapshot.name}”？`)) return;
    mutate((current) => ({ ...current, snapshots: current.snapshots.filter((item) => item.id !== snapshotId) }));
  };
  const exportJson = () => {
    const blob = new Blob([JSON.stringify(state, null, 2)], { type: "application/json" });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement("a");
    anchor.href = url; anchor.download = `tower-mechanics-atlas-${new Date().toISOString().slice(0, 10)}.json`; anchor.click();
    URL.revokeObjectURL(url); setToast("完整工作区已导出。请把文件作为跨设备与灾难恢复备份。");
  };
  const importFile = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0]; event.target.value = "";
    if (!file) return;
    try { setPendingImport(parseImport(await file.text(), catalogVersion)); }
    catch (error) { setToast(error instanceof Error ? error.message : "导入失败，当前数据未发生变化。"); }
  };
  const applyImport = (mode: "merge" | "overwrite") => {
    if (!pendingImport) return;
    if (mode === "overwrite" && !window.confirm("覆盖导入会替换本机全部工作区、快照和界面状态。确认继续？")) return;
    setState(mode === "merge" ? mergeImported(state, pendingImport) : touchState({ ...pendingImport, catalogVersion }));
    setPendingImport(null); setToast(mode === "merge" ? "导入内容已作为独立工作区合并。" : "本机数据已由备份完整恢复。");
  };
  const resetLocal = () => {
    if (!window.confirm("重置会清除本机全部工作区与快照。请确认已导出需要保留的 JSON 备份。")) return;
    if (!window.confirm("再次确认：这是不可撤销的本机数据清除操作。")) return;
    const fresh = createInitialState(catalogVersion); window.localStorage.removeItem(STORAGE_KEY); setState(fresh); setToast("本机数据已重置。");
  };

  if (!hydrated) return <main className="loading-screen" aria-live="polite"><span className="sigil">策</span><p>正在恢复你的设计工作区…</p></main>;

  return (
    <div className="atlas-shell">
      <header className="topbar">
        <button className="brand" onClick={() => setView("overview")} aria-label="返回设计图谱总览">
          <span className="brand-mark">塔</span><span><strong>机制设计图谱</strong><small>TOWER SYSTEMS ATLAS · CATALOG {catalogVersion}</small></span>
        </button>
        <div className="workspace-strip">
          <label><span>当前工作区</span><select value={activeWorkspace.id} onChange={(event) => mutate((current) => ({ ...current, activeWorkspaceId: event.target.value }))} aria-label="切换工作区">
            {state.workspaces.map((workspace) => <option key={workspace.id} value={workspace.id}>{workspace.name}</option>)}
          </select></label>
          <details className="workspace-menu">
            <summary>管理工作区</summary>
            <div role="menu" aria-label="工作区管理">
              <button role="menuitem" onClick={createNewWorkspace}>新建工作区</button>
              <button role="menuitem" onClick={duplicateWorkspace}>复制当前工作区</button>
              <button role="menuitem" onClick={renameWorkspace}>重命名当前工作区</button>
              <button role="menuitem" className="danger" onClick={deleteWorkspace}>删除当前工作区</button>
            </div>
          </details>
          <span className={`save-state ${saveStatus}`} aria-live="polite" title={saveStatus === "saved" ? `最后保存：${formatTime(state.updatedAt)}` : undefined}><i />{saveStatus === "saving" ? "保存中" : saveStatus === "error" ? "保存失败" : "已保存"}</span>
        </div>
      </header>

      {external && <aside className="alert-banner" role="alert"><span>检测到另一标签页在 {formatTime(external.updatedAt)} 保存了较新版本。</span><div><button onClick={() => { setState(external); setExternal(null); }}>载入较新版本</button><button onClick={() => {
        const now = new Date().toISOString();
        const copy = { ...structuredClone(activeWorkspace), id: makeId("workspace"), name: `${activeWorkspace.name}（冲突保留）`, createdAt: now, updatedAt: now };
        setState(touchState({ ...external, workspaces: [...external.workspaces, copy], activeWorkspaceId: copy.id })); setExternal(null);
      }}>保留当前为副本</button><button onClick={() => setExternal(null)}>稍后处理</button></div></aside>}
      {pendingImport && <aside className="alert-banner import-banner" role="alert"><span>备份校验通过：{pendingImport.workspaces.length} 个工作区、{pendingImport.snapshots.length} 个快照，结构版本 {pendingImport.schemaVersion}。</span><div><button onClick={() => applyImport("merge")}>安全合并</button><button className="danger" onClick={() => applyImport("overwrite")}>覆盖本机</button><button onClick={() => setPendingImport(null)}>取消</button></div></aside>}

      <aside className="side-nav" aria-label="图谱主导航">
        <nav>{viewMeta.map((view) => <button key={view.id} className={state.ui.view === view.id ? "active" : ""} onClick={() => setView(view.id)} aria-current={state.ui.view === view.id ? "page" : undefined}><span>{view.label}</span><small>{view.hint}</small></button>)}<Link href="/run-progression"><span>局内进程</span><small>10 套框架</small></Link></nav>
        <div className="authority-note"><strong>探索 ≠ 权威</strong><p>工作区中的“确认”仅代表当前设计筛选，不会修改正式玩法契约。</p><code>gameplay-design/</code></div>
      </aside>

      <main className="main-canvas">
        {state.ui.view === "overview" && <Overview evaluated={evaluated} counts={counts} onView={setView} />}
        {state.ui.view === "dimensions" && <DimensionTree state={state} mutate={mutate} openMechanic={openMechanic} activeWorkspace={activeWorkspace} />}
        {state.ui.view === "mechanics" && <MechanicLibrary filtered={filteredMechanics} state={state} workspace={activeWorkspace} updateFilter={updateFilter} clearFilters={clearFilters} openMechanic={openMechanic} updateChoice={updateChoice} />}
        {state.ui.view === "engines" && <EngineView key={activeWorkspace.id} focusedEngineId={state.ui.focusedEngineId} workspace={activeWorkspace} focusEngine={focusEngine} updateEngineChoice={updateEngineChoice} openMechanic={openMechanic} />}
        {state.ui.view === "bosses" && <BossView />}
        {state.ui.view === "decisions" && <DecisionDesk counts={counts} evaluated={evaluated} state={state} workspace={activeWorkspace} orphans={orphans} createSnapshot={createSnapshot} restoreSnapshot={restoreSnapshot} deleteSnapshot={deleteSnapshot} exportJson={exportJson} importClick={() => fileInput.current?.click()} resetLocal={resetLocal} />}
      </main>

      {selectedMechanic && <MechanicDetail mechanic={selectedMechanic} choice={choiceFor(activeWorkspace, selectedMechanic.id)} close={closeMechanic} navigate={openMechanic} updateChoice={updateChoice} />}
      <input ref={fileInput} type="file" accept="application/json,.json" onChange={importFile} hidden aria-hidden="true" />
      {toast && <div className="toast" role="status">{toast}</div>}
    </div>
  );
}

function PageHead({ eyebrow, title, copy, meta }: { eyebrow: string; title: string; copy: string; meta?: string }) {
  return <header className="page-head"><div><p className="eyebrow">{eyebrow}</p><h1>{title}</h1><p>{copy}</p></div>{meta && <span className="page-meta">{meta}</span>}</header>;
}

function Overview({ evaluated, counts, onView }: { evaluated: number; counts: Record<DecisionState, number>; onView: (view: AtlasState["ui"]["view"]) => void }) {
  return <>
    <section className="hero-panel">
      <div className="hero-copy"><p className="eyebrow">塔军自走构筑 · 设计空间坐标系</p><h1>先拆维度，<br /><em>再组合玩法。</em></h1><p>这不是一份把脑暴写成结论的长文章，而是一张持续生长的机制地图：从空白 10×6 棋盘出发，观察空间、时间、资源与构筑如何交叉，最终生成军团流派与 Boss 试卷。</p><div className="hero-actions"><button className="primary" onClick={() => onView("dimensions")}>进入维度树 <span>→</span></button><button onClick={() => onView("mechanics")}>浏览 {mechanics.length} 个机制</button></div></div>
      <div className="coordinate-card" aria-label="设计层级示意"><span className="axis axis-a">空间</span><span className="axis axis-b">时间</span><span className="axis axis-c">资源</span><span className="axis axis-d">构筑</span><div className="core"><small>基线</small><strong>空白网格</strong><span>自动战斗 · 战前布阵<br />有限英雄指令</span></div><i className="ring one" /><i className="ring two" /><i className="ring three" /></div>
    </section>
    <section className="stats-row"><div><strong>{domains.length}</strong><span>顶层领域</span></div><div><strong>{mechanics.length}</strong><span>结构化机制</span></div><div><strong>{engines.length}</strong><span>代表引擎</span></div><div><strong>{bossTests.length}</strong><span>Boss 试卷</span></div><div><strong>{evaluated}</strong><span>当前已评估</span></div></section>
    <section className="section-block"><div className="section-title"><div><p className="eyebrow">DESIGN DOMAINS</p><h2>八个维度，不混成一团</h2></div><p>每个领域回答不同问题；机制通过稳定 ID 建立协同、克制和依赖。</p></div><div className="domain-grid">{domains.map((domain) => <button key={domain.id} className="domain-card" style={{ "--accent": domain.color } as React.CSSProperties} onClick={() => onView("dimensions")}><span className="domain-index">{domain.index}</span><h3>{domain.title}</h3><p>{domain.summary}</p><small>{mechanics.filter((mechanic) => mechanic.domainId === domain.id).length} 个机制 · 查看分支 <b>↗</b></small></button>)}</div></section>
    <section className="authority-band"><div><span>规则边界</span><h2>图谱负责探索，权威文档负责承诺。</h2></div><p>“感兴趣、候选、图谱内确认、暂缓、排除”都是当前工作区的设计判断。只有另一次明确确认并同步到玩法文档，才会成为游戏规则。</p><div className="mini-counts"><span>候选 <b>{counts.candidate}</b></span><span>确认 <b>{counts.confirmed}</b></span><span>暂缓 <b>{counts.deferred}</b></span></div></section>
  </>;
}

function DimensionTree({ state, mutate, openMechanic, activeWorkspace }: { state: AtlasState; mutate: (recipe: (current: AtlasState) => AtlasState) => void; openMechanic: (id: string) => void; activeWorkspace: Workspace }) {
  const toggle = (id: string) => mutate((current) => ({ ...current, ui: { ...current.ui, expandedDomains: current.ui.expandedDomains.includes(id) ? current.ui.expandedDomains.filter((item) => item !== id) : [...current.ui.expandedDomains, id] } }));
  return <><PageHead eyebrow="维度树" title="把设计空间拆到可以讨论" copy="从领域进入子维度，再落到具体机制。这里展示边界与分叉，不要求现在就做取舍。" meta={`${domains.length} 领域 · ${new Set(mechanics.map((m) => `${m.domainId}.${m.subdimension}`)).size} 子维度`} />
    <div className="dimension-list">{domains.map((domain) => {
      const expanded = state.ui.expandedDomains.includes(domain.id);
      const domainMechanics = mechanics.filter((mechanic) => mechanic.domainId === domain.id);
      const subs = [...new Set(domainMechanics.map((mechanic) => mechanic.subdimension))];
      return <section key={domain.id} className={`dimension-node ${expanded ? "expanded" : ""}`} style={{ "--accent": domain.color } as React.CSSProperties}>
        <button className="dimension-head" onClick={() => toggle(domain.id)} aria-expanded={expanded}><span className="domain-index">{domain.index}</span><span><small>{domain.question}</small><strong>{domain.title}</strong><p>{domain.summary}</p></span><span className="dimension-count">{subs.length} 分支<br />{domainMechanics.length} 机制</span><b>{expanded ? "−" : "＋"}</b></button>
        {expanded && <div className="subdimension-grid">{subs.map((sub) => <div className="subdimension" key={sub}><h3>{sub}</h3>{domainMechanics.filter((mechanic) => mechanic.subdimension === sub).map((mechanic) => { const choice = choiceFor(activeWorkspace, mechanic.id); return <button key={mechanic.id} onClick={() => openMechanic(mechanic.id)}><span className={`state-dot ${choice.state}`} title={decisionMeta[choice.state].label} /><span><strong>{mechanic.title}</strong><small>{mechanic.premise}</small></span><b>→</b></button>; })}</div>)}</div>}
      </section>;
    })}</div></>;
}

function FilterSelect({ label, value, onChange, children }: { label: string; value: string; onChange: (value: string) => void; children: React.ReactNode }) {
  return <label className="filter-control"><span>{label}</span><select value={value} onChange={(event) => onChange(event.target.value)}><option value="">全部</option>{children}</select></label>;
}

function MechanicCard({ mechanic, workspace, openMechanic, updateChoice }: { mechanic: Mechanic; workspace: Workspace; openMechanic: (id: string) => void; updateChoice: (id: string, patch: Partial<MechanicChoice>) => void }) {
  const domain = domains.find((item) => item.id === mechanic.domainId)!;
  const choice = choiceFor(workspace, mechanic.id);
  const illustrated = Boolean(getMechanicDiagram(mechanic.id));
  return <article className={`mechanic-card ${illustrated ? "illustrated" : "text-only"}`} style={{ "--accent": domain.color } as React.CSSProperties}>
    <div className="card-top"><span>{domain.title} / {mechanic.subdimension}</span><button className={`decision-pill ${choice.state}`} onClick={() => updateChoice(mechanic.id, { state: choice.state === "unreviewed" ? "interested" : choice.state === "interested" ? "candidate" : "unreviewed" })} title="快速轮换：未评估 / 感兴趣 / 候选">{decisionMeta[choice.state].label}</button></div>
    <button className={`card-body ${illustrated ? "illustrated" : ""}`} onClick={() => openMechanic(mechanic.id)}>
      {illustrated && <MechanicDiagram mechanicId={mechanic.id} />}
      <h2>{mechanic.title}</h2>
      <p>{mechanic.premise}</p>
      {!illustrated && <blockquote>{mechanic.decision}</blockquote>}
      <div className="tag-line"><span>{dependencyLabels[mechanic.dependency]}</span><span>{agencyLabels[mechanic.agency]}</span><span>{complexityLabels[mechanic.complexity]}</span></div>
      {!illustrated && <code>{mechanic.id}</code>}
    </button>
    {choice.notes && <p className="note-preview">备注：{choice.notes}</p>}
  </article>;
}

function MechanicLibrary({ filtered, state, workspace, updateFilter, clearFilters, openMechanic, updateChoice }: { filtered: Mechanic[]; state: AtlasState; workspace: Workspace; updateFilter: (key: keyof AtlasState["ui"]["filters"], value: string) => void; clearFilters: () => void; openMechanic: (id: string) => void; updateChoice: (id: string, patch: Partial<MechanicChoice>) => void }) {
  const f = state.ui.filters;
  const activeFilterCount = Object.values(f).filter(Boolean).length;
  const illustrated = filtered.filter((mechanic) => Boolean(getMechanicDiagram(mechanic.id)));
  const textOnly = filtered.filter((mechanic) => !getMechanicDiagram(mechanic.id));
  return <><header className="library-head"><div><p className="eyebrow">机制库</p><h1>机制卡片库</h1><p>搜索规则、组合与问题；筛选、选择与备注会随当前工作区自动恢复。</p></div><span className="page-meta">显示 {filtered.length} / {mechanics.length}</span></header>
    <section className="filter-panel" aria-label="机制筛选">
      <label className="search-box"><span>⌕</span><input value={f.search} onChange={(event) => updateFilter("search", event.target.value)} placeholder="搜索机制、规则、玩家判断或稳定 ID…" aria-label="搜索机制" />{f.search && <button onClick={() => updateFilter("search", "")} aria-label="清除搜索">×</button>}</label>
      <div className="filter-primary-row">
        <FilterSelect label="领域" value={f.domain} onChange={(v) => updateFilter("domain", v)}>{domains.map((d) => <option key={d.id} value={d.id}>{d.title}</option>)}</FilterSelect>
        <FilterSelect label="决策状态" value={f.decision} onChange={(v) => updateFilter("decision", v)}>{Object.entries(decisionMeta).map(([id, meta]) => <option key={id} value={id}>{meta.label}</option>)}</FilterSelect>
        <details className="advanced-filters"><summary>更多筛选{[f.dependency, f.agency, f.complexity, f.depth].filter(Boolean).length ? `（${[f.dependency, f.agency, f.complexity, f.depth].filter(Boolean).length}）` : ""}</summary><div className="filter-row">
          <FilterSelect label="实现依赖" value={f.dependency} onChange={(v) => updateFilter("dependency", v)}>{Object.entries(dependencyLabels).map(([id, label]) => <option key={id} value={id}>{label}</option>)}</FilterSelect>
          <FilterSelect label="玩家作用" value={f.agency} onChange={(v) => updateFilter("agency", v)}>{Object.entries(agencyLabels).map(([id, label]) => <option key={id} value={id}>{label}</option>)}</FilterSelect>
          <FilterSelect label="复杂度" value={f.complexity} onChange={(v) => updateFilter("complexity", v)}>{Object.entries(complexityLabels).map(([id, label]) => <option key={id} value={id}>{label}</option>)}</FilterSelect>
          <FilterSelect label="深度类型" value={f.depth} onChange={(v) => updateFilter("depth", v)}>{Object.entries(depthLabels).map(([id, label]) => <option key={id} value={id}>{label}</option>)}</FilterSelect>
        </div></details>
        <button className="clear-filters" onClick={clearFilters} disabled={!activeFilterCount}>清除 {activeFilterCount ? `${activeFilterCount} 项` : "筛选"}</button>
      </div>
    </section>
    {filtered.length ? <div className="mechanic-results">
      {illustrated.length > 0 && <section className="mechanic-result-section illustrated-results" aria-labelledby="illustrated-results-title"><header><div><p className="eyebrow">VISUAL RULES</p><h2 id="illustrated-results-title">图解机制</h2></div><span>{illustrated.length} 项 · 先看空间与结算</span></header><div className="mechanic-grid illustrated-grid">{illustrated.map((mechanic) => <MechanicCard key={mechanic.id} mechanic={mechanic} workspace={workspace} openMechanic={openMechanic} updateChoice={updateChoice} />)}</div></section>}
      {textOnly.length > 0 && <section className="mechanic-result-section text-results" aria-labelledby="text-results-title"><header><div><p className="eyebrow">DESIGN RECORDS</p><h2 id="text-results-title">文字机制</h2></div><span>{textOnly.length} 项 · 紧凑浏览与比较</span></header><div className="mechanic-grid text-grid">{textOnly.map((mechanic) => <MechanicCard key={mechanic.id} mechanic={mechanic} workspace={workspace} openMechanic={openMechanic} updateChoice={updateChoice} />)}</div></section>}
    </div> : <div className="empty-state"><span>∅</span><h2>没有符合全部条件的机制</h2><p>筛选条件会叠加生效。清除一部分条件，或者保留它作为“当前设计缺口”。</p><button onClick={clearFilters}>清除全部筛选</button></div>}
  </>;
}

function RelationGroup({ title, ids, navigate }: { title: string; ids: string[]; navigate: (id: string) => void }) {
  if (!ids.length) return null;
  return <div className="relation-group"><h4>{title}</h4><div>{ids.map((id) => { const target = mechanics.find((mechanic) => mechanic.id === id); return <button key={id} onClick={() => navigate(id)}>{target?.title ?? id}<span>→</span></button>; })}</div></div>;
}

function MechanicDetail({ mechanic, choice, close, navigate, updateChoice }: { mechanic: Mechanic; choice: MechanicChoice; close: () => void; navigate: (id: string) => void; updateChoice: (id: string, patch: Partial<MechanicChoice>) => void }) {
  const domain = domains.find((item) => item.id === mechanic.domainId)!;
  return <div className="drawer-layer" role="presentation" onMouseDown={(event) => { if (event.target === event.currentTarget) close(); }}><aside className="detail-drawer" role="dialog" aria-modal="true" aria-labelledby="mechanic-title" style={{ "--accent": domain.color } as React.CSSProperties}><header><div><p>{domain.title} / {mechanic.subdimension}</p><h2 id="mechanic-title">{mechanic.title}</h2><code>{mechanic.id}</code></div><button onClick={close} aria-label="关闭机制详情">×</button></header><div className="detail-scroll">{getMechanicDiagram(mechanic.id) && <MechanicDiagram mechanicId={mechanic.id} mode="detail" onNavigate={navigate} />}<section className="premise"><span>核心命题</span><p>{mechanic.premise}</p></section><section><h3>规则想法</h3><p>{mechanic.rule}</p></section><div className="decision-pleasure"><section><h3>玩家会判断什么</h3><p>{mechanic.decision}</p></section><section><h3>预期乐趣</h3><p>{mechanic.pleasure}</p></section></div><section><h3>四轴影响</h3><div className="consequence-grid">{Object.entries(mechanic.consequences).map(([key, text]) => <div key={key}><span>{depthLabels[key]}</span><p>{text.replace(/^(空间|时间|资源|构筑)：/, "")}</p></div>)}</div></section><div className="relations"><RelationGroup title="协同" ids={mechanic.synergies} navigate={navigate} /><RelationGroup title="克制 / 回答" ids={mechanic.counters} navigate={navigate} /><RelationGroup title="依赖" ids={mechanic.dependencies} navigate={navigate} /></div><section className="design-audit"><h3>设计审计</h3><dl><div><dt>可读性</dt><dd>{mechanic.readability}</dd></div><div><dt>平衡风险</dt><dd>{mechanic.risks}</dd></div><div><dt>模拟 / 实现</dt><dd>{mechanic.implementation}</dd></div><div><dt>适用内容</dt><dd>{mechanic.suitable.join(" · ")}</dd></div></dl></section></div><footer><label><span>当前工作区状态</span><select value={choice.state} onChange={(event) => updateChoice(mechanic.id, { state: event.target.value as DecisionState })}>{Object.entries(decisionMeta).map(([id, meta]) => <option value={id} key={id}>{meta.label}</option>)}</select></label><label><span>优先级</span><select value={choice.priority} onChange={(event) => updateChoice(mechanic.id, { priority: Number(event.target.value) as 0 | 1 | 2 | 3 })}><option value="0">未设置</option><option value="1">低</option><option value="2">中</option><option value="3">高</option></select></label><label className="notes"><span>设计备注（自动保存）</span><textarea value={choice.notes} onChange={(event) => updateChoice(mechanic.id, { notes: event.target.value })} placeholder="记录判断、疑问、组合设想或否决原因…" /></label></footer></aside></div>;
}

function EngineView({ focusedEngineId, workspace, focusEngine, updateEngineChoice, openMechanic }: {
  focusedEngineId: string | null;
  workspace: Workspace;
  focusEngine: (id: string) => void;
  updateEngineChoice: (id: string, patch: Partial<EngineWorkspaceChoice>) => void;
  openMechanic: (id: string) => void;
}) {
  const [compareOpen, setCompareOpen] = useState(false);
  const compareHeading = useRef<HTMLHeadingElement>(null);
  const compareTrigger = useRef<HTMLButtonElement>(null);
  const focused = engines.find((engine) => engine.id === focusedEngineId) ?? engines[0];
  const planEngines = engines.filter((engine) => engineChoiceFor(workspace, engine.id).inPlan);
  const comparisonEngines = engines.filter((engine) => engineChoiceFor(workspace, engine.id).inComparison);
  const focusedChoice = engineChoiceFor(workspace, focused.id);

  useEffect(() => {
    if (compareOpen) compareHeading.current?.focus();
  }, [compareOpen]);

  const closeComparison = () => {
    setCompareOpen(false);
    requestAnimationFrame(() => compareTrigger.current?.focus());
  };
  const removeFromComparison = (engineId: string) => {
    updateEngineChoice(engineId, { inComparison: false });
    if (comparisonEngines.length <= 2) closeComparison();
  };
  const focusFromComparison = (engineId: string) => {
    focusEngine(engineId);
    closeComparison();
  };

  return <>
    <PageHead eyebrow="构筑实验台" title="从机制到会运转的构筑" copy="先扫视所有引擎，再把一个方向放到桌面中央。完整链路、断链风险与模拟边界只在聚焦详情中展开。" meta={`${engines.length} 条代表链路`} />

    {(planEngines.length > 0 || comparisonEngines.length > 0) && <section className="engine-selection-tray" aria-label="当前引擎选择">
      {planEngines.length > 0 && <div><strong>当前方案</strong><div>{planEngines.map((engine) => <button key={engine.id} onClick={() => focusEngine(engine.id)}>{engine.title}<span>查看</span></button>)}</div></div>}
      {comparisonEngines.length > 0 && <div className="comparison-group"><strong>比较栏</strong><div>{comparisonEngines.map((engine) => <button key={engine.id} onClick={() => focusEngine(engine.id)}>{engine.title}<span>查看</span></button>)}</div>{comparisonEngines.length >= 2 && <button ref={compareTrigger} className="open-comparison" onClick={() => setCompareOpen(true)} aria-expanded={compareOpen} aria-controls="engine-comparison-panel">打开比较（{comparisonEngines.length}）</button>}</div>}
    </section>}

    {compareOpen && comparisonEngines.length >= 2 ? <section id="engine-comparison-panel" className="engine-comparison-panel" aria-labelledby="engine-comparison-title">
      <header>
        <div><span>多引擎比较</span><h2 id="engine-comparison-title" ref={compareHeading} tabIndex={-1}>同时检查 {comparisonEngines.length} 条构筑链路</h2><p>并排核对核心命题、成型组件、断链压力和模拟边界，再回到单项详情继续深挖。</p></div>
        <button onClick={closeComparison}>关闭比较，返回实验台</button>
      </header>
      <div className="engine-comparison-grid">{comparisonEngines.map((engine) => <article key={engine.id} className="engine-comparison-card">
        <header><div><span>{engine.id}</span><h3>{engine.title}</h3><p>{engine.thesis}</p></div><div><button onClick={() => focusFromComparison(engine.id)}>聚焦查看完整详情</button><button className="remove" onClick={() => removeFromComparison(engine.id)}>从比较中移除</button></div></header>
        <div className="comparison-fields">
          <section><h4>成型组件</h4><ul>{engine.requirements.map((item) => <li key={item}>{item}</li>)}</ul></section>
          <section className="risk"><h4>断链风险</h4><ul>{engine.breaks.map((item) => <li key={item}>{item}</li>)}</ul></section>
          <section className="safe"><h4>模拟安全边界</h4><ul>{engine.safeguards.map((item) => <li key={item}>{item}</li>)}</ul></section>
        </div>
      </article>)}</div>
      <footer><button onClick={closeComparison}>关闭比较，返回实验台</button></footer>
    </section> : <div className="engine-workbench">
      <aside className="engine-browser" aria-label="构筑引擎列表">
        <header><h2>引擎概览</h2><span>{engines.length} 个方向</span></header>
        <div className="engine-summary-list">{engines.map((engine) => {
          const choice = engineChoiceFor(workspace, engine.id);
          const isFocused = engine.id === focused.id;
          return <article key={engine.id} className={`engine-summary ${isFocused ? "focused" : ""}`}>
            <button className="engine-summary-main" onClick={() => focusEngine(engine.id)} aria-current={isFocused ? "true" : undefined}>
              <span className="engine-summary-title"><strong>{engine.title}</strong>{choice.inPlan && <i>已加入方案</i>}{choice.inComparison && <i className="compare">比较中</i>}</span>
              <p>{engine.thesis}</p>
              <span className="engine-summary-tags"><i>{engine.requirements.length} 个组件</i><i>{engine.breaks.length} 个断链点</i><i>{engine.related.length} 个关联</i></span>
              <b>查看详情 <span>→</span></b>
            </button>
            <div className="engine-summary-actions">
              <button className={choice.inPlan ? "selected" : ""} aria-pressed={choice.inPlan} onClick={() => updateEngineChoice(engine.id, { inPlan: !choice.inPlan })}>{choice.inPlan ? "移出当前方案" : "加入当前方案"}</button>
              <button className={choice.inComparison ? "comparing" : ""} aria-pressed={choice.inComparison} onClick={() => updateEngineChoice(engine.id, { inComparison: !choice.inComparison })}>{choice.inComparison ? "移出比较" : "加入比较"}</button>
            </div>
          </article>;
        })}</div>
      </aside>

      <article className="engine-focus" aria-live="polite">
        <header>
          <div><span>当前聚焦</span><h2>{focused.title}</h2><p>{focused.thesis}</p></div>
          <div className="engine-focus-actions">
            <button className={focusedChoice.inPlan ? "selected" : ""} aria-pressed={focusedChoice.inPlan} onClick={() => updateEngineChoice(focused.id, { inPlan: !focusedChoice.inPlan })}>{focusedChoice.inPlan ? "已加入当前方案" : "加入当前方案"}</button>
            <button className={focusedChoice.inComparison ? "comparing" : ""} aria-pressed={focusedChoice.inComparison} onClick={() => updateEngineChoice(focused.id, { inComparison: !focusedChoice.inComparison })}>{focusedChoice.inComparison ? "已加入比较" : "加入比较"}</button>
          </div>
        </header>

        <section className="engine-detail-section"><div className="engine-section-heading"><span>01</span><div><h3>触发顺序</h3><p>从启动条件到反馈闭环，顺序决定引擎是否成立。</p></div></div><ol className="engine-flow">{focused.steps.map((step, stepIndex) => <li key={step}><span>{stepIndex + 1}</span><p>{step}</p>{stepIndex < focused.steps.length - 1 && <b aria-hidden="true">→</b>}</li>)}</ol></section>

        <div className="engine-detail-grid">
          <section className="engine-detail-section"><div className="engine-section-heading"><span>02</span><div><h3>成型组件</h3><p>引擎稳定启动所需的拼图。</p></div></div><ul>{focused.requirements.map((item) => <li key={item}>{item}</li>)}</ul></section>
          <section className="engine-detail-section risk"><div className="engine-section-heading"><span>03</span><div><h3>断链风险</h3><p>这些压力会降低效率或直接打断循环。</p></div></div><ul>{focused.breaks.map((item) => <li key={item}>{item}</li>)}</ul></section>
          <section className="engine-detail-section safe"><div className="engine-section-heading"><span>04</span><div><h3>模拟安全边界</h3><p>允许强循环，但必须保持确定、可解释、可结算。</p></div></div><ul>{focused.safeguards.map((item) => <li key={item}>{item}</li>)}</ul></section>
        </div>

        <section className="engine-related"><div><h3>相关机制</h3><p>打开机制详情，继续检查规则、风险与依赖。</p></div><div>{focused.related.map((id) => <button key={id} onClick={() => openMechanic(id)}>{mechanics.find((mechanic) => mechanic.id === id)?.title ?? id}<span>→</span></button>)}</div></section>
      </article>
    </div>}
  </>;
}

function BossView() {
  return <><PageHead eyebrow="Boss 能力矩阵" title="Boss 是试卷，不是钥匙孔" copy="每个主要压力都需要稳定答案、替代答案和高风险答案。矩阵检验能力覆盖，不绑定某个具体单位或遗物。" meta={`${bossTests.length} 试卷 · ${capabilityLabels.length} 能力`} /><div className="matrix-wrap"><table className="boss-matrix"><thead><tr><th>Boss / 核心压力</th>{capabilityLabels.map((label) => <th key={label}>{label}</th>)}</tr></thead><tbody>{bossTests.map((boss) => <tr key={boss.id}><th><strong>{boss.title}</strong><span>{boss.pressure}</span></th>{capabilityLabels.map((capability) => { const level = boss.primary.includes(capability) ? "primary" : boss.alternative.includes(capability) ? "alternative" : boss.risky.includes(capability) ? "risky" : "none"; return <td key={capability} data-level={level}><span aria-label={`${capability}：${level === "primary" ? "稳定答案" : level === "alternative" ? "替代答案" : level === "risky" ? "高风险答案" : "不是主要答案"}`}>{level === "primary" ? "●" : level === "alternative" ? "◐" : level === "risky" ? "△" : "·"}</span></td>; })}</tr>)}</tbody></table></div><div className="matrix-legend"><span><i className="primary">●</i> 稳定答案</span><span><i className="alternative">◐</i> 替代答案</span><span><i className="risky">△</i> 高风险答案</span><p>空白不等于“完全无效”，只表示它不是这道题的主要设计答案。</p></div><div className="boss-briefs">{bossTests.map((boss) => <article key={boss.id}><span>{boss.id}</span><h2>{boss.title}</h2><p>{boss.pressure}</p><dl><div><dt>公平预告</dt><dd>{boss.warning}</dd></div><div><dt>禁止设计</dt><dd>{boss.forbidden}</dd></div></dl></article>)}</div></>;
}

function DecisionDesk({ counts, evaluated, state, workspace, orphans, createSnapshot, restoreSnapshot, deleteSnapshot, exportJson, importClick, resetLocal }: { counts: Record<DecisionState, number>; evaluated: number; state: AtlasState; workspace: Workspace; orphans: string[]; createSnapshot: () => void; restoreSnapshot: (id: string) => void; deleteSnapshot: (id: string) => void; exportJson: () => void; importClick: () => void; resetLocal: () => void }) {
  return <><PageHead eyebrow="决策台" title="把探索保存成长期工作记忆" copy="状态、备注、筛选、展开位置和阅读进度都保存在这台设备。JSON 是跨浏览器、跨设备与清理缓存后的恢复路径。" meta={`结构 v${STATE_SCHEMA_VERSION} · 图谱 ${catalogVersion}`} /><section className="progress-card"><div className="progress-ring" style={{ "--progress": `${Math.round(evaluated / mechanics.length * 100)}%` } as React.CSSProperties}><strong>{Math.round(evaluated / mechanics.length * 100)}%</strong><span>已评估</span></div><div className="status-bars">{Object.entries(decisionMeta).map(([id, meta]) => <div key={id}><span>{meta.label}</span><div><i className={id} style={{ width: `${Math.max(2, counts[id as DecisionState] / mechanics.length * 100)}%` }} /></div><strong>{counts[id as DecisionState]}</strong></div>)}</div><div className="workspace-summary"><span>当前方案</span><h2>{workspace.name}</h2><p>{Object.keys(workspace.mechanics).filter((id) => choiceFor(workspace, id).notes).length} 条备注 · 更新于 {formatTime(workspace.updatedAt)}</p><code>{workspace.id}</code></div></section>
    <div className="desk-grid"><section className="desk-card"><header><div><p className="eyebrow">工作区</p><h2>命名工作区</h2></div><span>{state.workspaces.length}</span></header><p>不同方案的机制、引擎选择与备注完全分离。切换使用顶部选择器；新建、复制、重命名和删除统一收纳在“管理工作区”。</p><div className="workspace-list">{state.workspaces.map((item) => <div key={item.id} className={item.id === workspace.id ? "active" : ""}><span>{item.id === workspace.id ? "当前" : "方案"}</span><strong>{item.name}</strong><small>{Object.keys(item.mechanics).length} 个机制 · {Object.values(item.engines).filter((choice) => choice.inPlan).length} 个引擎</small></div>)}</div></section>
      <section className="desk-card"><header><div><p className="eyebrow">检查点</p><h2>命名快照</h2></div><button onClick={createSnapshot}>＋ 新建快照</button></header><p>快照冻结当前方案。恢复时创建新工作区，不覆盖正在编辑的版本。</p><div className="snapshot-list">{state.snapshots.length ? state.snapshots.map((snapshot) => <div key={snapshot.id}><span><strong>{snapshot.name}</strong><small>{formatTime(snapshot.createdAt)} · {Object.keys(snapshot.workspace.mechanics).length} 个记录</small></span><button onClick={() => restoreSnapshot(snapshot.id)}>恢复</button><button className="danger ghost" onClick={() => deleteSnapshot(snapshot.id)}>删除</button></div>) : <div className="quiet-empty">还没有快照。建议在一次重要筛选完成后创建。</div>}</div></section>
      <section className="desk-card backup-card"><header><div><p className="eyebrow">恢复</p><h2>备份与恢复</h2></div></header><p>浏览器本地数据可能因清理站点数据、隐私模式或更换设备而丢失。JSON 备份包含所有工作区、快照、备注和界面状态。</p><div className="backup-actions"><button className="primary" onClick={exportJson}>导出完整 JSON</button><button onClick={importClick}>导入 JSON</button></div><small>导入会先验证结构。你可以安全合并为新工作区，或经二次确认覆盖本机数据。</small></section>
      <section className="desk-card orphan-card"><header><div><p className="eyebrow">迁移</p><h2>版本与孤儿记录</h2></div><span>{orphans.length}</span></header>{orphans.length ? <><p>这些稳定 ID 来自旧版或外部目录，当前图谱无法匹配。数据仍被完整保留，不会静默删除。</p><div className="orphan-list">{orphans.map((id) => <code key={id}>{id}</code>)}</div></> : <p className="success-copy">当前所有已保存机制都能与图谱稳定 ID 匹配。新增目录内容不会覆盖已有选择。</p>}<dl><div><dt>用户数据结构</dt><dd>v{state.schemaVersion}</dd></div><div><dt>保存时图谱</dt><dd>{state.catalogVersion}</dd></div><div><dt>当前图谱</dt><dd>{catalogVersion}</dd></div></dl></section>
    </div><section className="danger-zone"><div><span>本机数据管理</span><p>重置仅影响此浏览器中的图谱工作区，不会修改游戏项目或正式设计文档。</p></div><button className="danger" onClick={resetLocal}>重置本机全部数据</button></section></>;
}
