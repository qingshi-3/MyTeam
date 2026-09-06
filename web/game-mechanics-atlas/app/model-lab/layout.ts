import { NODE_WIDTH, nodeHeight } from "./graph.ts";
import type { FoundationNode, FoundationWorkspace, FoundationZone } from "./model.ts";

export const ZONE_GAP = 20;
const LEFT = NODE_WIDTH / 2 + 18;
const RIGHT = NODE_WIDTH / 2 + 38; // Includes the connection port and resize affordance.
// A real header lane, shared by containment, fitting and the rendered title.
export const ZONE_HEADER_HEIGHT = 96;
const TOP = ZONE_HEADER_HEIGHT;
const BOTTOM = 14;
const clamp = (v: number, lo: number, hi: number) => Math.min(hi, Math.max(lo, v));
const minHeight = (zone: FoundationZone) => (zone.group === "guardrail" ? 68 : 116) + TOP + BOTTOM;

export function zonesOverlap(a: FoundationZone, b: FoundationZone) {
  return a.x < b.x + b.width + ZONE_GAP - 1e-7 && a.x + a.width + ZONE_GAP > b.x + 1e-7
    && a.y < b.y + b.height + ZONE_GAP - 1e-7 && a.y + a.height + ZONE_GAP > b.y + 1e-7;
}

function legalZone(zone: FoundationZone, others: FoundationZone[]) {
  return others.every((other) => !zonesOverlap(zone, other));
}

export function containsNode(zone: FoundationZone, node: FoundationNode) {
  const half = nodeHeight(node) / 2;
  return node.x >= zone.x + LEFT - 1e-7 && node.x <= zone.x + zone.width - RIGHT + 1e-7
    && node.y >= zone.y + TOP + half - 1e-7 && node.y <= zone.y + zone.height - BOTTOM - half + 1e-7;
}

function fitMembers(zone: FoundationZone, nodes: FoundationNode[]): FoundationZone {
  if (!nodes.length) return zone;
  const x = Math.min(...nodes.map((n) => n.x - LEFT));
  const y = Math.min(...nodes.map((n) => n.y - nodeHeight(n) / 2 - TOP));
  return { ...zone, x, y,
    width: Math.max(220, Math.max(...nodes.map((n) => n.x + RIGHT)) - x),
    height: Math.max(minHeight(zone), Math.max(...nodes.map((n) => n.y + nodeHeight(n) / 2 + BOTTOM)) - y) };
}

/** The world is unbounded: negative and distant positions are valid draft geometry.
 * Repair only containment/overlap; ambiguous membership preserves the stored original. */
export function normalizeLayout(workspace: FoundationWorkspace): FoundationWorkspace | null {
  if (new Set(workspace.zones.map((z) => z.group)).size !== workspace.zones.length
    || workspace.nodes.some((n) => !workspace.zones.some((z) => z.group === n.group))) return null;
  const zones: FoundationZone[] = [];
  const offsets = new Map<string, {x: number; y: number}>();
  for (const old of workspace.zones) {
    const members = workspace.nodes.filter((n) => n.group === old.group);
    // Grow upwards first so adding header space does not squash existing cards together.
    const y = Math.min(old.y, ...members.map((n) => n.y-nodeHeight(n)/2-TOP));
    const width = Math.max(old.width, 220);
    const height = Math.max(old.height+old.y-y, minHeight(old));
    const candidate = { ...old, y, width, height };
    const xs = [candidate.x, ...zones.flatMap((z) => [z.x - width - ZONE_GAP, z.x + z.width + ZONE_GAP])];
    const ys = [candidate.y, ...zones.flatMap((z) => [z.y - height - ZONE_GAP, z.y + z.height + ZONE_GAP])];
    const placed = xs.flatMap((x) => ys.map((y) => ({...candidate,x,y})))
      .filter((z) => legalZone(z, zones))
      .sort((a,b) => Math.hypot(a.x-candidate.x,a.y-candidate.y) - Math.hypot(b.x-candidate.x,b.y-candidate.y))[0];
    if (!placed) return null;
    zones.push(placed);
    offsets.set(old.group, {x: placed.x-candidate.x,y: placed.y-candidate.y});
  }
  const nodes = workspace.nodes.map((node) => {
    const zone = zones.find((z) => z.group === node.group)!;
    const offset = offsets.get(node.group)!;
    return { ...node,
      x: clamp(node.x + offset.x, zone.x + LEFT, zone.x + zone.width - RIGHT),
      y: clamp(node.y + offset.y, zone.y + TOP + nodeHeight(node)/2, zone.y + zone.height - BOTTOM - nodeHeight(node)/2) };
  });
  return { ...workspace, zones, nodes };
}

// Rectangle edges change linearly between breakpoints. Find the first interval in
// which all four separation inequalities fail; endpoint-only checks permit tunnelling.
function collisionTime(a: FoundationZone, b: FoundationZone, obstacle: FoundationZone): number {
  const start = [a.x + a.width + ZONE_GAP - obstacle.x, obstacle.x + obstacle.width + ZONE_GAP - a.x,
    a.y + a.height + ZONE_GAP - obstacle.y, obstacle.y + obstacle.height + ZONE_GAP - a.y];
  const end = [b.x + b.width + ZONE_GAP - obstacle.x, obstacle.x + obstacle.width + ZONE_GAP - b.x,
    b.y + b.height + ZONE_GAP - obstacle.y, obstacle.y + obstacle.height + ZONE_GAP - b.y];
  let enter = 0, leave = 1;
  for (let i=0;i<4;i++) {
    const delta = end[i] - start[i];
    if (Math.abs(delta) < 1e-9) { if (start[i] <= 1e-7) return 1; }
    else if (delta > 0) enter = Math.max(enter, -start[i]/delta);
    else leave = Math.min(leave, -start[i]/delta);
  }
  return enter < leave - 1e-9 ? clamp(enter,0,1) : 1;
}

function safeFraction(a: FoundationZone, b: FoundationZone, others: FoundationZone[]) {
  let limit = 1;
  for (const other of others) limit = Math.min(limit, collisionTime(a,b,other));
  return clamp(limit,0,1);
}

export type LayoutResult = { workspace: FoundationWorkspace; blocked: boolean; zoneId?: string };
export function moveNodeInLayout(workspace: FoundationWorkspace, id: string, x: number, y: number): LayoutResult {
  const node = workspace.nodes.find((n) => n.id === id);
  const zone = workspace.zones.find((z) => z.group === node?.group);
  if (!node || !zone || !Number.isFinite(x) || !Number.isFinite(y)) return {workspace,blocked:false};
  const others = workspace.zones.filter((z) => z.id !== zone.id);
  const members = workspace.nodes.filter((n) => n.group === zone.group);
  const dx = x-node.x, dy = y-node.y;
  const frameAt = (t: number) => fitMembers(zone, members.map((n) => n.id === id ? {...n,x:node.x+dx*t,y:node.y+dy*t} : n));
  // Extrema and minimum sizes are piecewise linear. Include every point at which
  // the moving card trades a bounding edge with a stationary card.
  const times = new Set([0,1]);
  const add = (t: number) => {if(t>0 && t<1) times.add(t);};
  for (const other of members) if (other.id !== id) {
    if (dx) for (const offset of [0,220-LEFT-RIGHT,LEFT+RIGHT-220]) add((other.x+offset-node.x)/dx);
    if (dy) for (const offset of [0,minHeight(zone)-nodeHeight(node)-TOP-BOTTOM,nodeHeight(node)+TOP+BOTTOM-minHeight(zone)]) add((other.y+offset-node.y)/dy);
  }
  const stops = [...times].sort((a,b) => a-b);
  let accepted = 0;
  for (let i=1;i<stops.length;i++) {
    const start = stops[i-1], end = stops[i];
    const fraction = safeFraction(frameAt(start),frameAt(end),others);
    accepted = start + (end-start)*fraction;
    if (fraction < 1-1e-9) break;
  }
  const next = { ...node, x:node.x+dx*accepted,y:node.y+dy*accepted };
  const frame = frameAt(accepted);
  return { workspace: {...workspace, nodes: workspace.nodes.map((n) => n.id === id ? next : n), zones: workspace.zones.map((z) => z.id === zone.id ? frame : z)}, blocked:accepted < 1-1e-7, zoneId:zone.id };
}

export function moveZoneInLayout(workspace: FoundationWorkspace, id: string, x: number, y: number): LayoutResult {
  const zone = workspace.zones.find((z) => z.id === id);
  if (!zone) return {workspace,blocked:false};
  const fraction = safeFraction(zone,{...zone,x,y},workspace.zones.filter((z) => z.id !== id));
  const dx = (x-zone.x)*fraction, dy = (y-zone.y)*fraction;
  return {workspace:{...workspace,zones:workspace.zones.map((z) => z.id === id ? {...z,x:z.x+dx,y:z.y+dy} : z),
    nodes:workspace.nodes.map((n) => n.group === zone.group ? {...n,x:n.x+dx,y:n.y+dy} : n)},blocked:fraction<1-1e-7,zoneId:id};
}

export function resizeZoneInLayout(workspace: FoundationWorkspace, id: string, width: number, height: number): LayoutResult {
  const zone = workspace.zones.find((z) => z.id === id);
  if (!zone) return {workspace,blocked:false};
  const members = workspace.nodes.filter((n) => n.group === zone.group);
  const target = {...zone,width:Math.max(width,220,...members.map((n) => n.x+RIGHT-zone.x)),
    height:Math.max(height,minHeight(zone),...members.map((n) => n.y+nodeHeight(n)/2+BOTTOM-zone.y))};
  const fraction = safeFraction(zone,target,workspace.zones.filter((z) => z.id !== id));
  return {workspace:{...workspace,zones:workspace.zones.map((z) => z.id === id ? {...z,width:z.width+(target.width-z.width)*fraction,height:z.height+(target.height-z.height)*fraction} : z)},
    blocked:fraction<1-1e-7 || target.width!==width || target.height!==height,zoneId:id};
}
