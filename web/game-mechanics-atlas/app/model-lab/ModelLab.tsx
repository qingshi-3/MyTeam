"use client";

import Link from "next/link";
import { useEffect, useMemo, useRef, useState } from "react";
import DecisionScene, { DECISION_STATUS } from "./DecisionScene";
import { SUPPLY_SCENARIO, SUPPLY_OPTIONS, validDecisions, recordSupplyDecision, decisionNeedsReview } from "./decisions";
import { edgePath, fitCamera, graphBounds, traceImpact, zoomCamera, type Camera, type ImpactTrace } from "./graph";
import { ZONE_HEADER_HEIGHT, normalizeLayout, moveNodeInLayout, moveZoneInLayout, resizeZoneInLayout, type LayoutResult } from "./layout";
import { gsap } from "gsap";
import { useGSAP } from "@gsap/react";
import {
  CONTENT_CHANNELS,
  GRAMMAR_TERMS,
  GROUP_LABELS,
  RELATION_LABELS,
  STATUS_LABELS,
  createCustomNode,
  createFoundationWorkspace,
  deriveDiagnostics,
  type DomainStatus,
  type FoundationEdge,
  type FoundationDecision,
  type FoundationNode,
  type FoundationWorkspace,
  type FoundationZone,
  type RelationKind,
} from "./model";

gsap.registerPlugin(useGSAP);

const STORAGE_KEY = "tower-foundation-model-lab:v2";
const SCHEMA_VERSION = 2;

// Do not overwrite drafts the current UI cannot safely render.
function validWorkspace(value: unknown): value is FoundationWorkspace {
  if (!value || typeof value !== "object") return false;
  const w = value as FoundationWorkspace;
  const strings = (v: unknown) => Array.isArray(v) && v.every((item) => typeof item === "string");
  return Array.isArray(w.nodes) && Array.isArray(w.edges) && Array.isArray(w.zones)
    && validDecisions(w.decisions)
    && w.nodes.every((n) => n && ["id","code","label","summary","responsibility","openQuestion","evidenceSummary"].every((key) => typeof n[key as keyof FoundationNode] === "string")
      && n.group in GROUP_LABELS && n.status in STATUS_LABELS && Number.isFinite(n.x) && Number.isFinite(n.y)
      && strings(n.inputs) && strings(n.outputs) && strings(n.channels)
      && Array.isArray(n.evidence) && n.evidence.every((e) => e && typeof e.id === "string" && typeof e.game === "string" && typeof e.domain === "string"))
    && w.edges.every((e) => e && typeof e.id === "string" && typeof e.from === "string" && typeof e.to === "string" && typeof e.label === "string" && e.relation in RELATION_LABELS)
    && w.zones.every((z) => z && typeof z.id === "string" && typeof z.label === "string" && typeof z.summary === "string" && z.group in GROUP_LABELS && [z.x,z.y,z.width,z.height].every(Number.isFinite));
}

type ViewMode = "decisions" | "graph" | "matrix" | "grammar" | "evidence";
type Selection =
  | { type: "node"; id: string }
  | { type: "edge"; id: string }
  | { type: "zone"; id: string }
  | null;
type NodeDrag = {
  pointerId: number;
  clientX: number;
  clientY: number;
  x: number;
  y: number;
  moved: boolean;
};
type ZoneDrag = {
  pointerId: number;
  mode: "move" | "resize";
  clientX: number;
  clientY: number;
  x: number;
  y: number;
  width: number;
  height: number;
  moved: boolean;
};
type StoredLab = { schemaVersion: number; workspace: FoundationWorkspace };


function snap(value: number) {
  return Math.round(value / 5) * 5;
}

function lines(value: string) {
  return value.split("\n").map((item) => item.trim()).filter(Boolean);
}


function NodeCard({
  node, selected, impact, dimmed, wiring, zoom, onSelect, onMove, onWire,
}: {
  node: FoundationNode;
  selected: boolean;
  impact: "origin" | "direct" | "propagated" | "upstream" | null;
  dimmed: boolean;
  wiring: boolean;
  zoom: number;
  onSelect: () => void;
  onMove: (x: number, y: number) => void;
  onWire: () => void;
}) {
  const drag = useRef<NodeDrag | null>(null);
  const suppressClick = useRef(false);
  const start = (event: React.PointerEvent<HTMLButtonElement>) => {
    if (event.button !== 0) return;
    event.currentTarget.setPointerCapture(event.pointerId);
    drag.current = { pointerId: event.pointerId, clientX: event.clientX, clientY: event.clientY, x: node.x, y: node.y, moved: false };
  };
  const move = (event: React.PointerEvent<HTMLButtonElement>) => {
    const state = drag.current;
    if (!state || state.pointerId !== event.pointerId) return;
    const dx = (event.clientX - state.clientX) / zoom;
    const dy = (event.clientY - state.clientY) / zoom;
    if (Math.abs(dx) + Math.abs(dy) > 3) state.moved = true;
    if (!state.moved) return;
    onMove(state.x + snap(dx), state.y + snap(dy));
  };
  const finish = () => {
    suppressClick.current = Boolean(drag.current?.moved);
    drag.current = null;
  };
  const key = (event: React.KeyboardEvent<HTMLButtonElement>) => {
    const step = event.shiftKey ? 25 : 5;
    if (event.key === "Enter" || event.key === " ") {
      event.preventDefault();
      event.stopPropagation();
      onSelect();
      return;
    }
    const delta = event.key === "ArrowLeft" ? [-step, 0] : event.key === "ArrowRight" ? [step, 0] : event.key === "ArrowUp" ? [0, -step] : event.key === "ArrowDown" ? [0, step] : null;
    if (!delta) return;
    event.preventDefault();
    event.stopPropagation();
    onMove(node.x + delta[0], node.y + delta[1]);
  };

  return (
    <div className={`foundation-node group-${node.group}${selected ? " is-selected" : ""}${impact ? ` impact-${impact}` : ""}${dimmed ? " is-dimmed" : ""}${wiring ? " is-wiring" : ""}`} style={{ left: node.x, top: node.y }} data-node-id={node.id} data-impact={impact ?? undefined}>
      <button type="button" className="foundation-node__body" aria-pressed={selected} aria-label={`${node.code} ${node.label}，拖动可调整位置，方向键可微调`}
        onClick={() => { if (!suppressClick.current) onSelect(); suppressClick.current = false; }}
        onPointerDown={start} onPointerMove={move} onPointerUp={finish} onPointerCancel={() => { drag.current = null; suppressClick.current = true; }} onKeyDown={key}>
        <span className="foundation-node__meta"><b>{node.code}</b><i>{STATUS_LABELS[node.status]}</i></span>
        <strong>{node.label}</strong>
        <small>{node.summary}</small>
        {impact && <span className={`node-relationship role-${impact}`}>{({origin:"当前规则",direct:"直接影响",propagated:"后续传播",upstream:"上游依赖"})[impact]}</span>}
        <span className="foundation-node__signal" aria-hidden="true" />
      </button>
      <button type="button" className="foundation-node__port" aria-label={`从 ${node.label} 建立关系`} title="建立关系" onClick={(event) => { event.stopPropagation(); onWire(); }} />
    </div>
  );
}

function ZoneFrame({ zone, selected, blocked, zoom, onSelect, onMove, onResize }: {
  zone: FoundationZone;
  selected: boolean;
  blocked: boolean;
  zoom: number;
  onSelect: () => void;
  onMove: (x: number, y: number) => void;
  onResize: (width: number, height: number) => void;
}) {
  const drag = useRef<ZoneDrag | null>(null);
  const suppressClick = useRef(false);
  // Keep the title readable until its reserved world-space lane is the limiting size.
  const labelScale = Math.min(1 / zoom, (ZONE_HEADER_HEIGHT - 24) / 30);
  const start = (mode: "move" | "resize", event: React.PointerEvent<HTMLButtonElement>) => {
    if (event.button !== 0) return;
    event.stopPropagation();
    event.currentTarget.setPointerCapture(event.pointerId);
    onSelect();
    drag.current = { pointerId: event.pointerId, mode, clientX: event.clientX, clientY: event.clientY, x: zone.x, y: zone.y, width: zone.width, height: zone.height, moved:false };
  };
  const move = (event: React.PointerEvent<HTMLButtonElement>) => {
    const state = drag.current;
    if (!state || state.pointerId !== event.pointerId) return;
    const dx = (event.clientX - state.clientX) / zoom;
    const dy = (event.clientY - state.clientY) / zoom;
    if (Math.abs(dx) + Math.abs(dy) > 3) state.moved = true;
    if (!state.moved) return;
    if (state.mode === "move") onMove(state.x + snap(dx), state.y + snap(dy));
    else onResize(state.width + snap(dx), state.height + snap(dy));
  };
  const finish = () => { suppressClick.current = Boolean(drag.current?.moved); drag.current = null; };

  return (
    <section data-zone-id={zone.id} data-group={zone.group} className={`foundation-zone group-${zone.group}${selected ? " is-selected" : ""}${blocked ? " is-blocked" : ""}`} style={{ left: zone.x, top: zone.y, width: zone.width, height: zone.height, "--zone-scale": 1 / zoom, "--zone-label-scale": labelScale } as React.CSSProperties}>
      <button type="button" className="foundation-zone__label" title={zone.label} style={{maxWidth:(zone.width-36)/labelScale}} onClick={() => {if (!suppressClick.current) onSelect(); suppressClick.current = false;}} onKeyDown={(event) => {
        const step = event.shiftKey ? 25 : 5;
        const d = event.key === "ArrowLeft" ? [-step, 0] : event.key === "ArrowRight" ? [step, 0] : event.key === "ArrowUp" ? [0, -step] : event.key === "ArrowDown" ? [0, step] : null;
        if (d) { event.preventDefault(); event.stopPropagation(); onMove(zone.x + d[0], zone.y + d[1]); }
      }} onPointerDown={(event) => start("move", event)} onPointerMove={move} onPointerUp={finish} onPointerCancel={finish}>
        <strong>{zone.label}</strong><span>{zone.summary}</span>
      </button>
      {blocked && <span className="foundation-zone__boundary" role="status">已到边界</span>}
      <button type="button" className="foundation-zone__resize" aria-label={`调整 ${zone.label} 分区大小`} onKeyDown={(event) => {
        const d = event.key === "ArrowRight" ? [10, 0] : event.key === "ArrowLeft" ? [-10, 0] : event.key === "ArrowDown" ? [0, 10] : event.key === "ArrowUp" ? [0, -10] : null;
        if (d) { event.preventDefault(); event.stopPropagation(); onResize(zone.width + d[0], zone.height + d[1]); }
      }} onPointerDown={(event) => start("resize", event)} onPointerMove={move} onPointerUp={finish} onPointerCancel={finish} />
    </section>
  );
}

type CameraRequest = { sequence: number; kind: "fit" | "focus" | "zoom"; zoom?: number };
function RelationshipGraph({ workspace, selection, impact, pulse, blockedZone, cameraRequest, onZoom, onSelection, onMoveNode, onMoveZone, onResizeZone, onConnect }: {
  workspace: FoundationWorkspace;
  selection: Selection;
  impact: ImpactTrace | null;
  pulse: number;
  blockedZone: string | null;
  cameraRequest: CameraRequest;
  onZoom: (zoom: number) => void;
  onSelection: (value: Selection) => void;
  onMoveNode: (id: string, x: number, y: number) => void;
  onMoveZone: (id: string, x: number, y: number) => void;
  onResizeZone: (id: string, width: number, height: number) => void;
  onConnect: (from: string, to: string) => void;
}) {
  const root = useRef<HTMLDivElement>(null);
  const [camera, setCamera] = useState<Camera>({ x: 0, y: 0, zoom: 1 });
  const [wireSource, setWireSource] = useState<string | null>(null);
  const [panning, setPanning] = useState(false);
  const pan = useRef<{ id: number; x: number; y: number; camera: Camera; moved: boolean } | null>(null);
  const autoFit = useRef(true);
  const bounds = useMemo(() => graphBounds(workspace), [workspace]);
  const boundsRef = useRef(bounds);
  useEffect(() => { boundsRef.current = bounds; }, [bounds]);
  const byId = useMemo(() => new Map(workspace.nodes.map((node) => [node.id, node])), [workspace.nodes]);
  const effectiveWireSource = wireSource && byId.has(wireSource) ? wireSource : null;
  const impactIds = new Set(impact ? [impact.origin, ...impact.direct, ...impact.propagated] : []);
  const upstream = new Set(impact ? workspace.edges.filter((edge) => edge.to === impact.origin && byId.get(edge.from)?.group !== "guardrail").map((edge) => edge.from) : []);

  useEffect(() => {
    const element = root.current;
    if (!element) return;
    let previousWidth = 0, previousHeight = 0;
    const observer = new ResizeObserver(() => {
      const { width, height } = element.getBoundingClientRect();
      if (!width || !height) return; // Switching to decisions must not reset the hidden graph's camera.
      if (autoFit.current) {
        // A phone opens on readable nodes; explicit 总览 still fits the entire map.
        const fitted = width < 600
          ? { zoom: 0.85, x: 20 - boundsRef.current.x * 0.85, y: 162 - boundsRef.current.y * 0.85 }
          : fitCamera(boundsRef.current, width, height);
        setCamera(fitted);
      }
      else if (previousWidth) setCamera((current) => ({ ...current, x: current.x + (width - previousWidth) / 2, y: current.y + (height - previousHeight) / 2 }));
      previousWidth = width; previousHeight = height;
    });
    observer.observe(element);
    const wheel = (event: WheelEvent) => {
      if ((event.target as Element).closest("button")) return;
      event.preventDefault();
      autoFit.current = false;
      const rect = element.getBoundingClientRect();
      if (event.ctrlKey || event.metaKey) {
        setCamera((current) => zoomCamera(current, current.zoom * Math.exp(-event.deltaY * 0.008), { x: event.clientX - rect.left, y: event.clientY - rect.top }));
      } else {
        setCamera((current) => ({ ...current, x: current.x - (event.shiftKey ? event.deltaY : event.deltaX), y: current.y - (event.shiftKey ? 0 : event.deltaY) }));
      }
    };
    element.addEventListener("wheel", wheel, { passive: false });
    return () => { observer.disconnect(); element.removeEventListener("wheel", wheel); };
  }, []);
  useEffect(() => { onZoom(camera.zoom); }, [camera.zoom, onZoom]);

  const lastRequest = useRef(0);
  useEffect(() => {
    if (!root.current || cameraRequest.sequence === lastRequest.current) return;
    const { clientWidth: width, clientHeight: height } = root.current;
    const request = window.requestAnimationFrame(() => {
      lastRequest.current = cameraRequest.sequence;
      if (cameraRequest.kind === "fit") {
        autoFit.current = true;
        setCamera(fitCamera(boundsRef.current, width, height));
      } else if (cameraRequest.kind === "focus" && selection?.type === "node") {
        const node = byId.get(selection.id);
        if (!node) return;
        autoFit.current = false;
        setCamera((current) => { const zoom = Math.max(1.1, current.zoom); return { zoom, x: width / 2 - node.x * zoom, y: height / 2 - node.y * zoom }; });
      } else if (cameraRequest.kind === "zoom" && cameraRequest.zoom) {
        autoFit.current = false;
        setCamera((current) => zoomCamera(current, cameraRequest.zoom!, { x: width / 2, y: height / 2 }));
      }
    });
    return () => window.cancelAnimationFrame(request);
  }, [cameraRequest, selection, byId]);

  useGSAP(() => {
    if (!impact || !root.current || !pulse) return;
    const media = gsap.matchMedia();
    media.add("(prefers-reduced-motion: no-preference)", () => {
      const timeline = gsap.timeline();
      timeline.fromTo("[data-impact='origin'] .foundation-node__body", { scale: 1 }, { scale: 1.025, duration: 0.12, yoyo: true, repeat: 1 });
      const direct = root.current!.querySelectorAll("[data-impact='direct'] .foundation-node__body");
      if (direct.length) timeline.fromTo(direct, { scale: 0.975 }, { scale: 1, duration: 0.22, stagger: 0.045, ease: "power2.out" }, 0.12);
      const propagated = root.current!.querySelectorAll("[data-impact='propagated'] .foundation-node__signal");
      if (propagated.length) timeline.fromTo(propagated, { opacity: 0.2 }, { opacity: 1, duration: 0.25, stagger: 0.045 }, 0.28);
    }, root);
    return () => media.revert();
  }, { scope: root, dependencies: [pulse, impact?.origin], revertOnUpdate: true });

  const selectNode = (node: FoundationNode) => {
    if (effectiveWireSource && effectiveWireSource !== node.id) { onConnect(effectiveWireSource, node.id); setWireSource(null); }
    else onSelection({ type: "node", id: node.id });
  };
  const fit = () => {
    if (!root.current) return;
    autoFit.current = true;
    setCamera(fitCamera(bounds, root.current.clientWidth, root.current.clientHeight));
  };

  return <div className={`graph-scroll${panning ? " is-panning" : ""}`} ref={root} tabIndex={0} aria-label="关系画布，拖动空白平移，按 Home 总览，方向键平移"
    onKeyDown={(event) => {
      if (event.target !== event.currentTarget && event.key !== "Escape") return;
      if (event.key === "Escape") { setWireSource(null); onSelection(null); }
      if (event.key === "Home") { event.preventDefault(); fit(); }
      const d = event.key === "ArrowLeft" ? [70, 0] : event.key === "ArrowRight" ? [-70, 0] : event.key === "ArrowUp" ? [0, 70] : event.key === "ArrowDown" ? [0, -70] : null;
      if (d) { event.preventDefault(); autoFit.current = false; setCamera((c) => ({ ...c, x: c.x + d[0], y: c.y + d[1] })); }
    }}
    onPointerDown={(event) => {
      if ((event.target as Element).closest("button, .foundation-edge__hit") || event.button !== 0) return;
      // The canvas owns this gesture from pointer-down, before native text selection starts.
      event.preventDefault();
      event.currentTarget.focus({preventScroll:true});
      window.getSelection()?.removeAllRanges();
      event.currentTarget.setPointerCapture(event.pointerId);
      pan.current = { id: event.pointerId, x: event.clientX, y: event.clientY, camera, moved: false };
    }}
    onPointerMove={(event) => {
      const state = pan.current;
      if (!state || state.id !== event.pointerId) return;
      const dx = event.clientX - state.x, dy = event.clientY - state.y;
      if (Math.abs(dx) + Math.abs(dy) < 4 && !state.moved) return;
      state.moved = true; autoFit.current = false; setPanning(true);
      setCamera({ ...state.camera, x: state.camera.x + dx, y: state.camera.y + dy });
    }}
    onPointerUp={() => { if (pan.current && !pan.current.moved) { setWireSource(null); onSelection(null); } pan.current = null; setPanning(false); }}
    onPointerCancel={() => { pan.current = null; setPanning(false); }}>
    <div className="graph-canvas" style={{ transform: `translate(${camera.x}px, ${camera.y}px) scale(${camera.zoom})` }}>
      {workspace.zones.map((zone) => <ZoneFrame key={zone.id} zone={zone} zoom={camera.zoom} blocked={blockedZone === zone.id} selected={selection?.type === "zone" && selection.id === zone.id}
        onSelect={() => onSelection({ type: "zone", id: zone.id })} onMove={(x,y) => onMoveZone(zone.id,x,y)} onResize={(w,h) => onResizeZone(zone.id,w,h)} />)}
      <svg className="foundation-edges" style={{left:bounds.x-100,top:bounds.y-100,width:bounds.width+200,height:bounds.height+200}} viewBox={`${bounds.x-100} ${bounds.y-100} ${bounds.width+200} ${bounds.height+200}`} aria-label="基础规则关系">
        <defs><marker id="foundation-arrow" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="6" markerHeight="6" orient="auto-start-reverse"><path d="M0,0 L10,5 L0,10 z" fill="context-stroke" /></marker></defs>
        {workspace.edges.map((edge) => {
          const from = byId.get(edge.from), to = byId.get(edge.to);
          if (!from || !to) return null;
          const active = impact?.edgeIds.includes(edge.id) ?? false;
          const incoming = impact?.origin === edge.to && from.group !== "guardrail";
          const selected = selection?.type === "edge" && selection.id === edge.id;
          const quiet = !selected && !active && !incoming && (Boolean(impact) || from.group === "guardrail");
          return <g key={edge.id} className={`foundation-edge relation-${edge.relation}${active ? " is-impact" : ""}${active && edge.from !== impact?.origin ? " is-propagated" : ""}${incoming ? " is-upstream" : ""}${selected ? " is-selected" : ""}${quiet ? " is-quiet" : ""}`} data-edge-id={edge.id}>
            <path className="foundation-edge__line" d={edgePath(from,to,edge.relation)} markerEnd="url(#foundation-arrow)" />
            <path className="foundation-edge__hit" d={edgePath(from,to,edge.relation)} role="button" tabIndex={quiet ? -1 : 0} aria-label={`${from.label} ${RELATION_LABELS[edge.relation]} ${to.label}`}
              onClick={(event) => { event.stopPropagation(); onSelection({ type:"edge", id:edge.id }); }}
              onKeyDown={(event) => { if (event.key === "Enter" || event.key === " ") { event.preventDefault(); onSelection({ type:"edge", id:edge.id }); } }}><title>{`${from.code} → ${to.code}：${edge.label}`}</title></path>
          </g>;
        })}
      </svg>
      {workspace.nodes.map((node) => {
        const role = impact?.origin === node.id ? "origin" : impact?.direct.includes(node.id) ? "direct" : impact?.propagated.includes(node.id) ? "propagated" : upstream.has(node.id) ? "upstream" : null;
        return <NodeCard key={node.id} node={node} zoom={camera.zoom} selected={selection?.type === "node" && selection.id === node.id} impact={role} dimmed={Boolean(impact && !impactIds.has(node.id) && !upstream.has(node.id))} wiring={wireSource === node.id}
          onSelect={() => selectNode(node)} onMove={(x,y) => onMoveNode(node.id,x,y)} onWire={() => setWireSource((current) => current === node.id ? null : node.id)} />;
      })}
    </div>
    {effectiveWireSource && <div className="wire-notice" role="status"><b>{byId.get(effectiveWireSource)?.code}</b><span>选择目标节点建立关系</span><button type="button" onClick={() => setWireSource(null)}>取消</button></div>}
  </div>;
}

function TagEditor({ label, values, onChange }: { label: string; values: string[]; onChange: (value: string[]) => void }) {
  // Preserve unfinished lines and cursor position while the shared model gets normalized values.
  const [text, setText] = useState(() => values.join("\n"));
  return <label className="inspector-field"><span>{label}<small>每行一项</small></span><textarea value={text} rows={Math.min(7, Math.max(3, text.split("\n").length))} onChange={(event) => { setText(event.target.value); onChange(lines(event.target.value)); }} /></label>;
}

function NodeInspector({ node, impact, workspace, onUpdate, onDelete, onNavigate, onReviewDecision }: {
  node: FoundationNode; impact: ImpactTrace; workspace: FoundationWorkspace;
  onUpdate: (patch: Partial<FoundationNode>) => void; onDelete: () => void;
  onNavigate: (selection: Selection) => void;
  onReviewDecision: () => void;
}) {
  const [tab, setTab] = useState<"connections" | "edit" | "evidence">("connections");
  const incoming = workspace.edges.filter((edge) => edge.to === node.id);
  const outgoing = workspace.edges.filter((edge) => edge.from === node.id);
  const byId = new Map(workspace.nodes.map((item) => [item.id,item]));
  const decision = workspace.decisions?.[SUPPLY_SCENARIO.id];
  const relations = (edges: FoundationEdge[], direction: "in" | "out") => edges.map((edge) => {
    const other = byId.get(direction === "in" ? edge.from : edge.to);
    if (!other) return null;
    return <div className="dependency-row" key={edge.id}>
      <button type="button" onClick={() => onNavigate({type:"node", id:other.id})}><b>{other.code}</b><span>{other.label}</span><i aria-hidden="true">{direction === "in" ? "↘" : "↗"}</i></button>
      <button type="button" className="dependency-edit" aria-label={`编辑关系 ${edge.from.toUpperCase()} 到 ${edge.to.toUpperCase()}`} onClick={() => onNavigate({type:"edge",id:edge.id})}>{RELATION_LABELS[edge.relation]} · 编辑关系</button>
    </div>;
  });
  return <div className="inspector-content">
    <div className="inspector-title"><span>{node.code} / {GROUP_LABELS[node.group]}</span><h2>{node.label}</h2><p>{node.summary}</p><span className={`rule-status status-${node.status}`}>{STATUS_LABELS[node.status]}</span></div>
    {SUPPLY_SCENARIO.nodeIds.includes(node.id) && <section className="decision-backlink" aria-label="关联设计决策"><strong>招募不到需要的角色，如何补救？</strong><p>{decision ? `${decisionNeedsReview(workspace) ? "关联规则变化，待复核" : DECISION_STATUS[decision.status]} · ${SUPPLY_OPTIONS.find(o=>o.id===decision.optionId)?.name??"原方案不可用"}` : "还没有记录决定，可先从玩家情境比较方案。"}</p><small>这项决定不等于整个规则域已定案。</small><p><button type="button" onClick={onReviewDecision}>回到决策场景</button></p></section>}
    <nav className="inspector-tabs" aria-label="规则详情">{([["connections","影响关系"],["edit","编辑规则"],["evidence",`证据 ${node.evidence.length}`]] as const).map(([key,label]) => <button type="button" key={key} aria-pressed={tab === key} onClick={() => setTab(key)}>{label}</button>)}</nav>
    {tab === "connections" && <>
      <section className="dependency-section downstream"><h3>会影响谁 <span>{outgoing.length}</span></h3>{outgoing.length ? relations(outgoing,"out") : <p className="inspector-note">尚无下游关系。可用节点右侧连接点建立。</p>}</section>
      <section className="dependency-section upstream"><h3>依赖什么 <span>{incoming.length}</span></h3>{incoming.length ? relations(incoming,"in") : <p className="inspector-note">暂无上游依赖。</p>}</section>
      <details className="inspector-disclosure"><summary>后续传播 <span>{impact.propagated.length} 个规则域</span></summary><p className="inspector-note">沿现有有向关系可达；包含反馈回路，不表示修改必然导致数值变化。</p><div className="propagation-links">{impact.propagated.map((id) => <button type="button" key={id} onClick={() => onNavigate({type:"node",id})}><b>{byId.get(id)?.code}</b>{byId.get(id)?.label}</button>)}</div></details>
      <details className="inspector-disclosure"><summary>关联内容渠道 <span>{impact.channels.length}</span></summary><div className="channel-tags">{impact.channels.map((channel) => <span key={channel}>{channel}</span>)}</div></details>
      {node.openQuestion && <section className="open-question"><h3>还需要决定</h3><p>{node.openQuestion}</p><button type="button" onClick={() => setTab("edit")}>调整这条规则</button></section>}
    </>}
    {tab === "edit" && <>
      <p className="inspector-note">修改的是本地设计草案。高亮显示需要复核的依赖，不是战斗模拟。</p>
      <label className="inspector-field"><span>规则域名称</span><textarea rows={2} value={node.label} onChange={(event) => onUpdate({label:event.target.value})} /></label>
      <label className="inspector-field"><span>规则域摘要</span><textarea rows={3} value={node.summary} onChange={(event) => onUpdate({summary:event.target.value})} /></label>
      <label className="inspector-field"><span>状态</span><select value={node.status} onChange={(event) => onUpdate({status:event.target.value as DomainStatus})}>{Object.entries(STATUS_LABELS).filter(([key]) => key !== "guardrail" || node.group === "guardrail").map(([key,label]) => <option key={key} value={key}>{label}</option>)}</select></label>
      <label className="inspector-field"><span>唯一职责</span><textarea rows={4} value={node.responsibility} onChange={(event) => onUpdate({responsibility:event.target.value})} /></label>
      <TagEditor label="输入" values={node.inputs} onChange={(inputs) => onUpdate({inputs})} />
      <TagEditor label="输出" values={node.outputs} onChange={(outputs) => onUpdate({outputs})} />
      <TagEditor label="内容渠道" values={node.channels} onChange={(channels) => onUpdate({channels})} />
      <label className="inspector-field"><span>待决问题</span><textarea rows={3} value={node.openQuestion} onChange={(event) => onUpdate({openQuestion:event.target.value})} /></label>
      <button type="button" className="danger-action" onClick={onDelete}>{node.custom ? "删除这个自定义节点" : "从当前草案移除该基础域"}</button>
    </>}
    {tab === "evidence" && <section className="evidence-block"><p>{node.evidenceSummary}</p>{node.evidence.map((evidence) => <article key={evidence.id}><strong>{evidence.game}</strong><span>{evidence.domain}</span><code>{evidence.id}</code></article>)}{!node.evidence.length && <p>该节点尚未绑定调研证据。</p>}<p className="inspector-note">研究证据支持设计讨论，不代表已确认的游戏规则。</p></section>}
  </div>;
}

function EdgeInspector({ edge, workspace, onUpdate, onDelete }: { edge: FoundationEdge; workspace: FoundationWorkspace; onUpdate: (patch: Partial<FoundationEdge>) => void; onDelete: () => void }) {
  return <div className="inspector-content" key={edge.id}>
    <div className="inspector-title"><span>RELATION · 可重定向</span><h2>编辑规则关系</h2><p>关系只表达依赖方向；它不会替代目标规则域自己的结算定义。</p></div>
    <label className="inspector-field"><span>起点</span><select value={edge.from} onChange={(event) => onUpdate({ from: event.target.value })}>{workspace.nodes.map((node) => <option key={node.id} value={node.id}>{node.code} · {node.label}</option>)}</select></label>
    <label className="inspector-field"><span>关系</span><select value={edge.relation} onChange={(event) => { const relation = event.target.value as RelationKind; onUpdate({ relation, label: RELATION_LABELS[relation] }); }}>{Object.entries(RELATION_LABELS).map(([id, label]) => <option key={id} value={id}>{label}</option>)}</select></label>
    <label className="inspector-field"><span>终点</span><select value={edge.to} onChange={(event) => onUpdate({ to: event.target.value })}>{workspace.nodes.map((node) => <option key={node.id} value={node.id}>{node.code} · {node.label}</option>)}</select></label>
    <label className="inspector-field"><span>关系说明</span><input value={edge.label} onChange={(event) => onUpdate({ label: event.target.value })} /></label>
    <div className="relation-sentence"><b>{workspace.nodes.find((node) => node.id === edge.from)?.label}</b><span>{edge.label}</span><b>{workspace.nodes.find((node) => node.id === edge.to)?.label}</b></div>
    <button type="button" className="danger-action" onClick={onDelete}>删除这条关系</button>
  </div>;
}

function ZoneInspector({ zone, workspace, onUpdate }: { zone: FoundationZone; workspace: FoundationWorkspace; onUpdate: (patch: Partial<FoundationZone>) => void }) {
  const members = workspace.nodes.filter((node) => node.group === zone.group);
  return <div className="inspector-content" key={zone.id}>
    <div className="inspector-title"><span>REGION · {members.length} 个节点</span><input value={zone.label} onChange={(event) => onUpdate({ label: event.target.value })} /><textarea rows={2} value={zone.summary} onChange={(event) => onUpdate({ summary: event.target.value })} /></div>
    <div className="zone-members">{members.map((node) => <span key={node.id}><b>{node.code}</b>{node.label}</span>)}</div><p className="inspector-note">拖动节点时分区自动包裹同组内容；拖动标题会整组移动。分区之间保留间距，右下角缩放不会裁掉节点。</p>
  </div>;
}

function Inspector({ workspace, selection, impact, collapsed, onToggle, onUpdateNode, onUpdateEdge, onUpdateZone, onDeleteNode, onDeleteEdge, onNavigate, onReviewDecision }: {
  workspace: FoundationWorkspace;
  selection: Selection;
  impact: ImpactTrace | null;
  collapsed: boolean;
  onToggle: () => void;
  onUpdateNode: (id: string, patch: Partial<FoundationNode>) => void;
  onUpdateEdge: (id: string, patch: Partial<FoundationEdge>) => void;
  onUpdateZone: (id: string, patch: Partial<FoundationZone>) => void;
  onDeleteNode: (id: string) => void;
  onDeleteEdge: (id: string) => void;
  onNavigate: (selection: Selection) => void;
  onReviewDecision: () => void;
}) {
  const node = selection?.type === "node" ? workspace.nodes.find((item) => item.id === selection.id) : undefined;
  const edge = selection?.type === "edge" ? workspace.edges.find((item) => item.id === selection.id) : undefined;
  const zone = selection?.type === "zone" ? workspace.zones.find((item) => item.id === selection.id) : undefined;
  const railLabel = node?.code ?? (edge ? "关系" : zone ? "分区" : "详情");
  return <aside className={`foundation-inspector${collapsed ? " is-collapsed" : ""}`} aria-label="规则检查器">
    <button type="button" className="inspector-toggle" aria-expanded={!collapsed} aria-label={collapsed ? "展开规则检查器" : "收起规则检查器"} onClick={onToggle}><span aria-hidden="true">{collapsed ? "‹" : "›"}</span></button>
    {collapsed && <button type="button" className="inspector-rail" onClick={onToggle} aria-label={`展开 ${railLabel} 详情`}><b>{railLabel}</b>{impact && <span>{impact.direct.length}<i>+</i>{impact.propagated.length}</span>}<small>详情</small></button>}
    {!collapsed && <div className="inspector-surface">
      {node && impact && <NodeInspector key={node.id} onNavigate={onNavigate} onReviewDecision={onReviewDecision} node={node} impact={impact} workspace={workspace} onUpdate={(patch) => onUpdateNode(node.id, patch)} onDelete={() => onDeleteNode(node.id)} />}
      {edge && <EdgeInspector edge={edge} workspace={workspace} onUpdate={(patch) => onUpdateEdge(edge.id, patch)} onDelete={() => onDeleteEdge(edge.id)} />}
      {zone && <ZoneInspector zone={zone} workspace={workspace} onUpdate={(patch) => onUpdateZone(zone.id, patch)} />}
      {!node && !edge && !zone && <div className="inspector-empty"><span>INSPECTOR</span><h2>选择一个规则域</h2><p>这里会同步显示它对其他规则、内容渠道和调研证据产生的影响。</p></div>}
    </div>}
  </aside>;
}

function ImpactMatrix({ workspace, onSelect, onCreate }: { workspace: FoundationWorkspace; onSelect: (selection: Selection) => void; onCreate: (from: string, to: string) => void }) {
  return <section className="projection-panel matrix-panel">
    <header><span>RELATION MATRIX</span><h2>影响矩阵</h2><p>行是影响源，列是被影响域。点击已有关系可编辑；点击空白格可建立一条“影响”关系。</p></header>
    <div className="matrix-scroll"><table><thead><tr><th>源 ↓ / 目标 →</th>{workspace.nodes.map((node) => <th key={node.id} title={node.label}>{node.code}</th>)}</tr></thead><tbody>
      {workspace.nodes.map((from) => <tr key={from.id}><th><button type="button" onClick={() => onSelect({ type: "node", id: from.id })}><b>{from.code}</b><span>{from.label}</span></button></th>{workspace.nodes.map((to) => {
        const edge = workspace.edges.find((item) => item.from === from.id && item.to === to.id);
        return <td key={to.id} className={edge ? `has-relation relation-${edge.relation}` : ""}>{from.id === to.id ? <i>—</i> : <button type="button" title={edge ? `${from.label} ${edge.label} ${to.label}` : `建立 ${from.label} → ${to.label}`} onClick={() => edge ? onSelect({ type: "edge", id: edge.id }) : onCreate(from.id, to.id)}>{edge ? edge.relation === "feeds" ? "→" : edge.relation === "constrains" ? "⊣" : "↺" : "+"}</button>}</td>;
      })}</tr>)}
    </tbody></table></div>
  </section>;
}

function GrammarView({ workspace, onSelect }: { workspace: FoundationWorkspace; onSelect: (selection: Selection) => void }) {
  return <section className="projection-panel grammar-panel">
    <header><span>GENERIC RULE GRAMMAR</span><h2>通用规则语法</h2><p>这不是一条具体玩法，而是所有单位、能力、装备、遗物、指令和环境规则进入游戏时共用的描述骨架。</p></header>
    <div className="grammar-chain">{GRAMMAR_TERMS.map(([term, explanation], index) => <article key={term}><span>{String(index + 1).padStart(2, "0")}</span><h3>{term}</h3><p>{explanation}</p>{index < GRAMMAR_TERMS.length - 1 && <i aria-hidden="true">→</i>}</article>)}</div>
    <div className="channel-map"><header><span>CONTENT INJECTION</span><h2>内容接入面</h2></header>{CONTENT_CHANNELS.map(([name, description, ids]) => <article key={name}><div><h3>{name}</h3><p>{description}</p></div><div>{ids.map((id) => { const node = workspace.nodes.find((item) => item.id === id); return node && <button type="button" key={id} onClick={() => onSelect({ type: "node", id })}><b>{node.code}</b>{node.label}</button>; })}</div></article>)}</div>
  </section>;
}

function EvidenceIndex({ workspace, onSelect }: { workspace: FoundationWorkspace; onSelect: (selection: Selection) => void }) {
  const [query, setQuery] = useState("");
  const entries = useMemo(() => workspace.nodes.flatMap((node) => node.evidence.map((evidence) => ({ ...evidence, node }))), [workspace.nodes]);
  const games = useMemo(() => [...new Set(entries.map((item) => item.game))].sort((a, b) => a.localeCompare(b)), [entries]);
  const filtered = entries.filter((item) => `${item.id} ${item.game} ${item.domain} ${item.node.code} ${item.node.label}`.toLowerCase().includes(query.toLowerCase()));
  return <section className="projection-panel evidence-panel">
    <header><span>RESEARCH INDEX</span><h2>证据索引</h2><p>{entries.length} 次引用 · {new Set(entries.map((item) => item.id)).size} 条独立证据 · {games.length} 款游戏。研究资产保持只读，此处只建立到规则域的映射。</p><label><span>筛选证据</span><input value={query} placeholder="游戏、证据 ID 或规则域" onChange={(event) => setQuery(event.target.value)} /></label></header>
    <div className="evidence-grid">{filtered.map((entry) => <button type="button" key={`${entry.node.id}-${entry.id}`} onClick={() => onSelect({ type: "node", id: entry.node.id })}><span>{entry.node.code} · {entry.node.label}</span><strong>{entry.game}</strong><p>{entry.domain}</p><code>{entry.id}</code></button>)}</div>
    {!filtered.length && <div className="empty-search">没有符合当前筛选的证据。</div>}
  </section>;
}

function ConfirmChange({ reset, onCancel, onConfirm }: { reset: boolean; onCancel: () => void; onConfirm: () => void }) {
  const dialog = useRef<HTMLDialogElement>(null);
  useEffect(() => {
    const previousFocus = document.activeElement as HTMLElement | null;
    const element = dialog.current;
    element?.showModal();
    element?.querySelector<HTMLButtonElement>("button")?.focus();
    return () => { element?.close(); if (previousFocus?.isConnected) previousFocus.focus(); };
  }, []);
  return <dialog ref={dialog} className="confirm-change" aria-labelledby="confirm-change-title" aria-describedby="confirm-change-description" onCancel={(event) => {event.preventDefault();onCancel();}}>
    <h2 id="confirm-change-title">{reset ? "恢复 V2 基线？" : "从草案中移除？"}</h2>
    <p id="confirm-change-description">{reset ? "当前本地修改将被基线替换。" : "规则域被移除时，它的关联关系也会移除。"}操作后可立即撤销；继续编辑后撤销入口会关闭。</p>
    <div><button type="button" onClick={onCancel}>保留草案</button><button type="button" className="confirm-destructive" onClick={onConfirm}>{reset ? "确认恢复" : "确认移除"}</button></div>
  </dialog>;
}

export default function ModelLab() {
  const [workspace, setWorkspace] = useState<FoundationWorkspace>(() => normalizeLayout(createFoundationWorkspace())!);
  const [blockedZone, setBlockedZone] = useState<string | null>(null);
  const [view, setView] = useState<ViewMode>("decisions");
  const [selection, setSelection] = useState<Selection>(null);
  const [soundEnabled, setSoundEnabled] = useState(true);
  const [pulse, setPulse] = useState(0);
  const [saved, setSaved] = useState(false);
  const [topTrayOpen, setTopTrayOpen] = useState(false);
  const [inspectorCollapsed, setInspectorCollapsed] = useState(true);
  const [zoom, setZoom] = useState(1);
  const [cameraRequest, setCameraRequest] = useState<CameraRequest>({sequence:0,kind:"fit"});
  const [impactDepth, setImpactDepth] = useState(1);
  const [saveError, setSaveError] = useState("");
  const [canRecord, setCanRecord] = useState(false);
  const [changeNotice, setChangeNotice] = useState("");
  const [undoWorkspace, setUndoWorkspace] = useState<FoundationWorkspace | null>(null);
  const [pendingAction, setPendingAction] = useState<{type:"node" | "edge" | "reset"; id?:string} | null>(null);
  const readOnlyDraft = useRef(false);
  const requestCamera = (kind: CameraRequest["kind"], nextZoom?: number) => setCameraRequest((current) => ({ sequence:current.sequence + 1, kind, zoom:nextZoom }));
  const loaded = useRef(false);
  const audioContext = useRef<AudioContext | null>(null);

  const impact = useMemo(() => selection?.type === "node" ? traceImpact(workspace, selection.id) : null, [workspace, selection]);
  const visibleImpact = useMemo(() => selection?.type === "node" ? traceImpact(workspace, selection.id, impactDepth) : null, [workspace, selection, impactDepth]);
  const diagnostics = useMemo(() => deriveDiagnostics(workspace), [workspace]);
  const selectedNode = selection?.type === "node" ? workspace.nodes.find((node) => node.id === selection.id) : undefined;
  const counts = useMemo(() => ({
    gameplay: workspace.nodes.filter((node) => node.group !== "guardrail").length,
    guardrails: workspace.nodes.filter((node) => node.group === "guardrail").length,
    open: workspace.nodes.filter((node) => node.status === "open").length,
    evidence: new Set(workspace.nodes.flatMap((node) => node.evidence.map((item) => item.id))).size,
  }), [workspace.nodes]);

  useEffect(() => {
    let restored: FoundationWorkspace | null = null;
    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      if (stored) {
        const parsed = JSON.parse(stored) as StoredLab;
        if (parsed.schemaVersion === SCHEMA_VERSION && validWorkspace(parsed.workspace)) {
          restored = normalizeLayout(parsed.workspace);
          if (!restored) readOnlyDraft.current = true;
        }
        else { readOnlyDraft.current = true; }
      }
    } catch { readOnlyDraft.current = true; }
    const handle = window.setTimeout(() => {
      if (restored) setWorkspace(restored);
      if (readOnlyDraft.current) setSaveError("原草案无法读取，已保留原件；当前修改不会覆盖它。");
      setCanRecord(!readOnlyDraft.current);
      loaded.current = true;
    }, 0);
    return () => window.clearTimeout(handle);
  }, []);

  useEffect(() => {
    if (!loaded.current || readOnlyDraft.current) return;
    const dirtyHandle = window.setTimeout(() => setSaved(false), 0);
    const handle = window.setTimeout(() => {
      try {
        localStorage.setItem(STORAGE_KEY, JSON.stringify({ schemaVersion: SCHEMA_VERSION, workspace } satisfies StoredLab));
        setSaved(true); setSaveError("");
      } catch { setSaved(false); setSaveError("本地保存失败，请先保留此页面；不要刷新。"); }
    }, 220);
    return () => { window.clearTimeout(dirtyHandle); window.clearTimeout(handle); };
  }, [workspace]);

  const playImpact = (trace: ImpactTrace | null) => {
    if (!soundEnabled || !trace) return;
    try {
      const context = audioContext.current ?? new AudioContext();
      audioContext.current = context;
      const now = context.currentTime;
      const notes = [196, 261.63, 329.63];
      const amounts = [1, trace.direct.length, trace.propagated.length];
      notes.forEach((frequency, index) => {
        if (!amounts[index] && index > 0) return;
        const oscillator = context.createOscillator();
        const gain = context.createGain();
        oscillator.type = "sine";
        oscillator.frequency.setValueAtTime(frequency, now + index * 0.055);
        gain.gain.setValueAtTime(0.0001, now + index * 0.055);
        gain.gain.exponentialRampToValueAtTime(0.035, now + index * 0.055 + 0.008);
        gain.gain.exponentialRampToValueAtTime(0.0001, now + index * 0.055 + 0.11);
        oscillator.connect(gain).connect(context.destination);
        oscillator.start(now + index * 0.055);
        oscillator.stop(now + index * 0.055 + 0.12);
      });
    } catch { /* Audio is reinforcement only. */ }
  };

  const select = (next: Selection) => {
    setSelection(next);
    setBlockedZone(null);
    setChangeNotice("");
    if (next?.type === "node") {
      const trace = traceImpact(workspace, next.id, impactDepth);
      setPulse((value) => value + 1);
      playImpact(trace);
    }
    if (view !== "graph" && next) { setView("graph"); setInspectorCollapsed(false); }
  };

  const updateNode = (id: string, patch: Partial<FoundationNode>) => {
    setUndoWorkspace(null);
    setSaved(false);
    if (patch.x === undefined && patch.y === undefined) {
      setChangeNotice(`${id.toUpperCase()} 已修改 · 请复核高亮依赖`);
      setPulse((value) => value + 1);
    }
    setWorkspace((current) => ({ ...current, nodes: current.nodes.map((node) => node.id === id ? { ...node, ...patch } : node) }));
  };
  const updateEdge = (id: string, patch: Partial<FoundationEdge>) => {
    setUndoWorkspace(null);
    setSaved(false);
    setChangeNotice("关系已更新 · 下游依赖已重新计算");
    setWorkspace((current) => ({ ...current, edges: current.edges.map((edge) => edge.id === id ? { ...edge, ...patch } : edge) }));
  };
  const updateZone = (id: string, patch: Partial<FoundationZone>) => {
    setUndoWorkspace(null);
    setWorkspace((current) => ({ ...current, zones: current.zones.map((zone) => zone.id === id ? { ...zone, ...patch } : zone) }));
  };
  const applyLayout = (result: LayoutResult) => {
    setUndoWorkspace(null);
    setSaved(false);
    setBlockedZone(result.blocked ? result.zoneId ?? null : null);
    setWorkspace(result.workspace);
  };
  const recordDecision = (optionId: string, status: FoundationDecision["status"], note: string) => {
    if (readOnlyDraft.current) return;
    try {
      const next = recordSupplyDecision(workspace,optionId,status,note,new Date().toISOString());
      setUndoWorkspace(null); setSaved(false); setWorkspace(next);
    } catch (error) {setSaveError(error instanceof Error ? error.message : "决策未保存，请检查关联规则。");}
  };
  const connect = (from: string, to: string) => {
    if (from === to || !workspace.nodes.some((node) => node.id === from) || !workspace.nodes.some((node) => node.id === to)) {
      setChangeNotice("连接已取消：请选择两个仍在草案中的不同规则域。");
      return;
    }
    setView("graph");
    setInspectorCollapsed(false);
    const existing = workspace.edges.find((edge) => edge.from === from && edge.to === to);
    if (existing) { setSelection({ type: "edge", id: existing.id }); return; }
    setUndoWorkspace(null);
    const edge: FoundationEdge = { id: `edge-custom-${Date.now()}`, from, to, relation: "feeds", label: "影响" };
    setWorkspace((current) => ({ ...current, edges: [...current.edges, edge] }));
    setSelection({ type: "edge", id: edge.id });
  };
  const addNode = () => {
    setUndoWorkspace(null);
    setView("graph");
    const customCount = workspace.nodes.filter((node) => node.custom).length + 1;
    const node = createCustomNode(customCount, 1320, 330 + ((customCount - 1) % 3) * 120);
    setWorkspace((current) => normalizeLayout({ ...current, nodes: [...current.nodes, node] }) ?? current);
    setSelection({ type: "node", id: node.id });
    setInspectorCollapsed(false);
    requestCamera("focus");
    setPulse((value) => value + 1);
  };
  const deleteNode = (id: string) => {
    setPendingAction({type:"node",id});
  };
  const reset = () => {
    if (readOnlyDraft.current) { setSaveError("已保留无法读取的原草案。请先导出或备份浏览器草案，再处理重置。"); return; }
    setPendingAction({type:"reset"});
  };

  const selectedLabel = selectedNode ? `${selectedNode.code} · ${selectedNode.label}` : selection?.type === "edge" ? "规则关系" : selection?.type === "zone" ? "规则分区" : "未选择";
  const leadingDiagnostic = diagnostics[0];
  const deleteEdge = (id: string) => {
    setPendingAction({type:"edge",id});
  };
  const confirmChange = () => {
    if (!pendingAction) return;
    setUndoWorkspace(workspace);
    const {type,id} = pendingAction;
    if (type === "reset") { setWorkspace(normalizeLayout(createFoundationWorkspace())!); requestCamera("fit"); }
    else if (type === "node") setWorkspace((current) => ({...current,nodes:current.nodes.filter((node) => node.id !== id),edges:current.edges.filter((edge) => edge.from !== id && edge.to !== id)}));
    else setWorkspace((current) => ({...current,edges:current.edges.filter((edge) => edge.id !== id)}));
    setSelection(null);
    setChangeNotice(type === "reset" ? "已恢复 V2 基线" : "已从本地草案移除");
    setPendingAction(null);
  };

  return <main className="model-lab">
    {pendingAction && <ConfirmChange reset={pendingAction.type === "reset"} onCancel={() => setPendingAction(null)} onConfirm={confirmChange} />}
    <header className={`lab-toptray${topTrayOpen ? " is-open" : ""}`}>
      <div className="toptray-surface">
        <Link href="/" className="lab-brand" aria-label="返回机制图谱首页"><span aria-hidden="true">◎</span><div><strong>游戏基础规则网络</strong><small>V2 · 本地草案</small></div></Link>
        <nav aria-label="模型视图">{([['decisions', '决策场景'], ['graph', '基础关系图'], ['matrix', '影响矩阵'], ['grammar', '通用规则语法'], ['evidence', '证据索引']] as Array<[ViewMode, string]>).map(([id, label]) => <button type="button" key={id} className={view === id ? "is-active" : ""} onClick={() => { setView(id); setTopTrayOpen(false); }}>{label}</button>)}</nav>
        <div className="tray-metrics" aria-label="模型概览"><span><b>{counts.gameplay}</b>玩法域</span><span><b>{counts.guardrails}</b>护栏</span><span><b>{counts.open}</b>待决</span><span><b>{counts.evidence}</b>证据</span></div>
        <div className="tray-actions">
          <span className={`save-state${saved ? " is-saved" : ""}`} role="status">{saved ? "已保存" : "本地保存"}</span>
          <button type="button" className={`sound-toggle${soundEnabled ? " is-on" : ""}`} aria-pressed={soundEnabled} onClick={() => setSoundEnabled((value) => !value)}><span aria-hidden="true">{soundEnabled ? "◖))" : "◖×"}</span>音效</button>
          <button type="button" onClick={addNode}>新增规则</button>
          <button type="button" onClick={reset}>恢复基线</button>
        </div>
        {leadingDiagnostic && <button type="button" className={`tray-diagnostic diagnostic-${leadingDiagnostic.severity}`} disabled={!leadingDiagnostic.nodeId} onClick={() => leadingDiagnostic.nodeId && select({ type: "node", id: leadingDiagnostic.nodeId })}><i />{leadingDiagnostic.title}</button>}
      </div>
      <button type="button" className="toptray-handle" aria-expanded={topTrayOpen} aria-label={topTrayOpen ? "收起顶部工具" : "展开顶部工具"} onClick={() => setTopTrayOpen((value) => !value)}><span />{topTrayOpen ? "收起" : "视图与工具"}<span /></button>
    </header>

    <div className="decision-layer" hidden={view!=="decisions"}><DecisionScene key={workspace.decisions?.[SUPPLY_SCENARIO.id]?.updatedAt??"unrecorded"} workspace={workspace} onRecord={recordDecision} onInspect={(id)=>{select({type:"node",id});requestCamera("focus");}} onGraph={()=>setView("graph")} saveError={saveError} saved={saved} readOnly={!canRecord} /></div>

    <section hidden={view!=="graph"} className={`graph-stage${inspectorCollapsed ? " inspector-is-collapsed" : ""}`} aria-label="游戏基础规则网络工作台">
      <div className="graph-panel"><RelationshipGraph workspace={workspace} selection={selection} impact={visibleImpact} pulse={pulse} blockedZone={blockedZone} cameraRequest={cameraRequest} onZoom={setZoom} onSelection={select} onMoveNode={(id, x, y) => applyLayout(moveNodeInLayout(workspace,id,x,y))} onMoveZone={(id,x,y) => applyLayout(moveZoneInLayout(workspace,id,x,y))} onResizeZone={(id,width,height) => applyLayout(resizeZoneInLayout(workspace,id,width,height))} onConnect={connect} /></div>

      <div className="canvas-heading"><h1>游戏基础规则网络</h1><p>{counts.gameplay} 个玩法域 <span>/</span> {counts.guardrails} 个工程护栏</p><button type="button" className="decision-return" onClick={()=>setView("decisions")}>回到决策场景</button></div>
      <div className="canvas-mode" aria-label="影响展示范围">
        <span>追踪范围</span>
        {([[1,"直接影响"],[2,"两层关系"],[Infinity,"完整传播"]] as const).map(([depth,label]) => <button type="button" key={label} aria-pressed={impactDepth === depth} onClick={() => {setImpactDepth(depth); setPulse((v) => v+1);}}>{label}</button>)}
      </div>
      <div className="canvas-bottom">
        <div className="graph-readout" aria-live="polite">{selectedNode ? <><span>{selectedNode.code}</span><strong>{selectedNode.label}</strong><small>{visibleImpact?.direct.length} 直接 / {visibleImpact?.propagated.length} 后续</small></> : <><strong>{selection ? selectedLabel : "选择规则，查看它的上下游"}</strong><small>拖动空白平移 · Ctrl + 滚轮缩放</small></>}</div>
        <div className="graph-controls" aria-label="画布控制">
          <button type="button" aria-label="缩小画布" onClick={() => requestCamera("zoom",zoom-0.15)}>−</button><output aria-label="当前缩放比例">{Math.round(zoom*100)}%</output><button type="button" aria-label="放大画布" onClick={() => requestCamera("zoom",zoom+0.15)}>＋</button>
          <button type="button" onClick={() => requestCamera("fit")}>总览</button><button type="button" disabled={selection?.type !== "node"} onClick={() => requestCamera("focus")}>聚焦</button>
        </div>
      </div>
      <div className="canvas-legend"><span><i className="legend-upstream" />上游依赖</span><span><i className="legend-direct" />直接影响</span><span><i className="legend-propagated" />后续传播</span><small>结构依赖 · 非战斗模拟</small></div>
      {(saveError || changeNotice || undoWorkspace) && <div className={`change-notice${saveError ? " is-error" : ""}`} role="status"><span>{saveError || changeNotice || "已更新本地草案"}</span>{undoWorkspace && <button type="button" onClick={() => {setWorkspace(undoWorkspace);setUndoWorkspace(null);setSelection(null);setChangeNotice("已撤销上次移除或重置");}}>撤销移除 / 重置</button>}{!saveError && <button type="button" aria-label="关闭修改提示" onClick={() => {setChangeNotice("");setUndoWorkspace(null);}}>×</button>}</div>}

      <Inspector onNavigate={select} onReviewDecision={()=>setView("decisions")} workspace={workspace} selection={selection} impact={impact} collapsed={inspectorCollapsed} onToggle={() => setInspectorCollapsed((value) => !value)} onUpdateNode={updateNode} onUpdateEdge={updateEdge} onUpdateZone={updateZone} onDeleteNode={deleteNode} onDeleteEdge={deleteEdge} />
    </section>

    {view !== "graph" && view !== "decisions" && <div className="projection-viewport">
      {view === "matrix" && <ImpactMatrix workspace={workspace} onSelect={select} onCreate={connect} />}
      {view === "grammar" && <GrammarView workspace={workspace} onSelect={select} />}
      {view === "evidence" && <EvidenceIndex workspace={workspace} onSelect={select} />}
      <footer className="lab-footer"><span>修改和决策保存在当前设备；各视图共享同一份模型。</span><button type="button" onClick={reset}>清除修改并恢复 V2 基线</button></footer>
    </div>}
  </main>;
}
