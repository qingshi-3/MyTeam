import type { FoundationNode, FoundationWorkspace, RelationKind } from "./model.ts";

export type ImpactTrace = {
  origin: string;
  direct: string[];
  propagated: string[];
  edgeIds: string[];
  channels: string[];
  depths: Record<string, number>;
};

// Reachability describes review scope, not simulated outcomes or numerical causation.
export function traceImpact(workspace: FoundationWorkspace, origin: string, maxDepth = Infinity): ImpactTrace {
  const validIds = new Set(workspace.nodes.map((node) => node.id));
  const depths: Record<string, number> = {};
  const edgeIds = new Set<string>();
  const direct: string[] = [], propagated: string[] = [];
  if (validIds.has(origin)) depths[origin] = 0;
  const queue = validIds.has(origin) ? [origin] : [];
  for (let index = 0; index < queue.length; index++) {
    const id = queue[index], depth = depths[id];
    if (depth >= maxDepth) continue;
    for (const edge of workspace.edges) {
      if (edge.from !== id || !validIds.has(edge.to) || edge.from === edge.to) continue;
      edgeIds.add(edge.id);
      if (depths[edge.to] !== undefined) continue;
      depths[edge.to] = depth + 1;
      (depth === 0 ? direct : propagated).push(edge.to);
      queue.push(edge.to);
    }
  }
  const channels = [...new Set(workspace.nodes.filter((node) => depths[node.id] !== undefined).flatMap((node) => node.channels))];
  return { origin, direct, propagated, edgeIds: [...edgeIds], channels, depths };
}

export type Camera = { x: number; y: number; zoom: number };
export type Bounds = { x: number; y: number; width: number; height: number };
export const NODE_WIDTH = 164;
export const nodeHeight = (node: FoundationNode) => node.group === "guardrail" ? 68 : 116;

export function graphBounds(workspace: FoundationWorkspace): Bounds {
  const boxes = [
    ...workspace.zones,
    ...workspace.nodes.map((node) => ({ x: node.x - NODE_WIDTH / 2, y: node.y - nodeHeight(node) / 2, width: NODE_WIDTH, height: nodeHeight(node) })),
  ];
  if (!boxes.length) return { x: 0, y: 0, width: 1500, height: 980 };
  const x = Math.min(...boxes.map((box) => box.x)), y = Math.min(...boxes.map((box) => box.y));
  return { x, y, width: Math.max(...boxes.map((box) => box.x + box.width)) - x, height: Math.max(...boxes.map((box) => box.y + box.height)) - y };
}

export function fitCamera(bounds: Bounds, width: number, height: number): Camera {
  const padding = width < 600 ? 20 : 44;
  const availableHeight = Math.max(80, height - 208);
  // Overview must include distant/negative coordinates too, without a fixed zoom floor.
  const zoom = Math.min(1.65, Math.max(1, width - padding * 2) / Math.max(1,bounds.width), availableHeight / Math.max(1,bounds.height));
  return { zoom, x: (width - bounds.width * zoom) / 2 - bounds.x * zoom, y: 108 + (availableHeight - bounds.height * zoom) / 2 - bounds.y * zoom };
}

export function zoomCamera(camera: Camera, nextZoom: number, anchor: { x: number; y: number }): Camera {
  const zoom = Math.max(Math.min(0.2,camera.zoom), Math.min(2, nextZoom)), ratio = zoom / camera.zoom;
  return { zoom, x: anchor.x - (anchor.x - camera.x) * ratio, y: anchor.y - (anchor.y - camera.y) * ratio };
}

// Terminate on the node perimeter, keeping direction arrows outside the card.
export function edgePath(from: FoundationNode, to: FoundationNode, relation: RelationKind) {
  const dx = to.x - from.x, dy = to.y - from.y;
  const horizontal = Math.abs(dx) / NODE_WIDTH > Math.abs(dy) / Math.max(nodeHeight(from), nodeHeight(to));
  const sign = (horizontal ? dx : dy) >= 0 ? 1 : -1;
  const start = { x: from.x + (horizontal ? sign * NODE_WIDTH / 2 : 0), y: from.y + (horizontal ? 0 : sign * nodeHeight(from) / 2) };
  const end = { x: to.x - (horizontal ? sign * (NODE_WIDTH / 2 + 5) : 0), y: to.y - (horizontal ? 0 : sign * (nodeHeight(to) / 2 + 5)) };
  const bend = Math.max(30, Math.abs(horizontal ? end.x - start.x : end.y - start.y) * 0.42) + (relation === "feedback" ? 25 : 0);
  return horizontal
    ? `M ${start.x} ${start.y} C ${start.x + bend * sign} ${start.y}, ${end.x - bend * sign} ${end.y}, ${end.x} ${end.y}`
    : `M ${start.x} ${start.y} C ${start.x} ${start.y + bend * sign}, ${end.x} ${end.y - bend * sign}, ${end.x} ${end.y}`;
}
