import assert from "node:assert/strict";
import test from "node:test";
import {createFoundationWorkspace, createCustomNode} from "../app/model-lab/model.ts";
import {ZONE_HEADER_HEIGHT,normalizeLayout,moveNodeInLayout,moveZoneInLayout,resizeZoneInLayout,zonesOverlap,containsNode} from "../app/model-lab/layout.ts";
import {graphBounds,fitCamera,zoomCamera} from "../app/model-lab/graph.ts";

function valid(w) {
  assert.ok(w);
  for (const n of w.nodes) assert.ok(containsNode(w.zones.find(z=>z.group===n.group),n), `${n.id} escaped its group`);
  for (const [i,z] of w.zones.entries()) {
    assert.ok([z.x,z.y,z.width,z.height].every(Number.isFinite));
    for (const other of w.zones.slice(i+1)) assert.ok(!zonesOverlap(z,other), `${z.id} collided with ${other.id}`);
  }
}
const seed = () => normalizeLayout(createFoundationWorkspace());
const zoneFor = (w,group) => w.zones.find(z=>z.group===group);

test("header migration keeps sibling spacing and reserves space through upward drag and resize",()=>{
  const raw=createFoundationWorkspace();
  const legacy={nodes:raw.nodes.filter(n=>n.id==='f01'||n.id==='f02'),zones:[raw.zones.find(z=>z.group==='loop')],edges:[]};
  const w=normalizeLayout(legacy);
  valid(w);
  assert.deepEqual(w.nodes,legacy.nodes);
  assert.ok(w.zones[0].y<legacy.zones[0].y);
  assert.deepEqual(normalizeLayout(w),w);
  const moved=moveNodeInLayout(w,'f01',130,-200).workspace;
  const resized=resizeZoneInLayout(moved,moved.zones[0].id,220,1).workspace;
  valid(resized);
  for(const n of resized.nodes) assert.ok(n.y-58>=resized.zones[0].y+ZONE_HEADER_HEIGHT);
  assert.deepEqual(normalizeLayout(JSON.parse(JSON.stringify(resized))),resized);
});

test("baseline and legacy/new-node layouts preserve content while repairing containment",()=>{
  const raw=createFoundationWorkspace(), original=structuredClone(raw), w=seed();
  valid(w);
  assert.deepEqual(raw,original);
  assert.deepEqual(w.edges,raw.edges);
  const content=n=>Object.fromEntries(Object.entries(n).filter(([key])=>key!=="x" && key!=="y"));
  assert.deepEqual(w.nodes.map(content),raw.nodes.map(content));
  assert.deepEqual(normalizeLayout(w),w);
  const legacy={...w,nodes:[...w.nodes.map(n=>n.id==="f01"?{...n,x:1400,y:100}:n),createCustomNode(1,1320,330)]};
  const repaired=normalizeLayout(legacy);
  valid(repaired);
  assert.equal(repaired.nodes[0].group,"loop");
  assert.equal(repaired.nodes.length,w.nodes.length+1);
  valid(normalizeLayout({...w,zones:w.zones.map(z=>({...z,x:10,y:10,width:1480,height:960}))}));
  const invalid={...w,zones:[...w.zones,w.zones[0]]};
  assert.equal(normalizeLayout(invalid),null);
});

test("world coordinates and frame size have no invisible outer boundary",()=>{
  const base=seed(), node=base.nodes[0], zone=zoneFor(base,node.group);
  // Isolated group: all four directions are free of real neighbouring obstacles.
  const w={...base,nodes:base.nodes.filter(n=>n.group===zone.group),zones:[zone],edges:[]};
  for (const [x,y] of [[-5000,130],[6000,130],[130,-5000],[130,6000]]) {
    const moved=moveNodeInLayout(w,node.id,x,y);
    valid(moved.workspace);
    assert.equal(moved.blocked,false);
    assert.equal(moved.workspace.nodes[0].x,x);
    assert.equal(moved.workspace.nodes[0].y,y);
    assert.deepEqual(normalizeLayout(JSON.parse(JSON.stringify(moved.workspace))),moved.workspace);
    const groupMove=moveZoneInLayout(w,zone.id,x,y);
    valid(groupMove.workspace);
    assert.equal(groupMove.blocked,false);
    assert.equal(groupMove.workspace.zones[0].x,x);
    assert.equal(groupMove.workspace.zones[0].y,y);
  }
  const expanded=resizeZoneInLayout(w,zone.id,20000,15000);
  valid(expanded.workspace);
  assert.equal(expanded.blocked,false);
  assert.deepEqual(normalizeLayout(expanded.workspace),expanded.workspace);
  const bounds=graphBounds(expanded.workspace),camera=fitCamera(bounds,1280,720);
  assert.ok(camera.zoom<0.15);
  assert.ok(camera.x+bounds.x*camera.zoom>=0);
  assert.ok(camera.y+bounds.y*camera.zoom>=0);
  assert.ok(camera.x+(bounds.x+bounds.width)*camera.zoom<=1280);
  assert.ok(camera.y+(bounds.y+bounds.height)*camera.zoom<=720);
  assert.equal(zoomCamera(camera,camera.zoom,{x:640,y:360}).zoom,camera.zoom);
});

test("node motion expands and contracts only its stable owning frame",()=>{
  const w=seed(), original=structuredClone(w), node=w.nodes.find(n=>n.id==="f01");
  const moved=moveNodeInLayout(w,node.id,node.x,node.y-15);
  valid(moved.workspace);
  assert.equal(moved.blocked,false);
  const expanded=zoneFor(moved.workspace,"loop");
  assert.ok(expanded.y<zoneFor(w,"loop").y);
  const returned=moveNodeInLayout(moved.workspace,node.id,node.x,node.y);
  valid(returned.workspace);
  assert.ok(zoneFor(returned.workspace,"loop").height<expanded.height);
  assert.deepEqual(w,original);
  assert.deepEqual(moved.workspace.zones.filter(z=>z.group!=="loop"),w.zones.filter(z=>z.group!=="loop"));
  assert.deepEqual(moved.workspace.nodes.filter(n=>n.id!==node.id),w.nodes.filter(n=>n.id!==node.id));
});

test("large node jumps stop at a neighbour and reverse motion remains possible",()=>{
  const w=seed(), node=w.nodes.find(n=>n.id==="f03");
  const moved=moveNodeInLayout(w,node.id,1450,node.y);
  valid(moved.workspace);
  assert.equal(moved.blocked,true);
  assert.ok(moved.workspace.nodes.find(n=>n.id===node.id).x<350);
  const reverse=moveNodeInLayout(moved.workspace,node.id,node.x-20,node.y);
  valid(reverse.workspace);
  assert.equal(reverse.blocked,false);
});

test("a single-node frame cannot tunnel through a neighbour with a clear endpoint",()=>{
  const raw=createFoundationWorkspace();
  const w={nodes:[{...raw.nodes[0],x:150,y:150},{...raw.nodes[3],x:500,y:150}],edges:[],zones:[
    {...raw.zones[0],x:50,y:-4,width:220,height:226},
    {...raw.zones[1],x:400,y:-4,width:220,height:226}]};
  valid(w);
  const moved=moveNodeInLayout(w,w.nodes[0].id,1000,150);
  valid(moved.workspace);
  assert.equal(moved.blocked,true);
  assert.ok(moved.workspace.nodes[0].x<300);
});

test("group drag carries only members and resize never clips content or crosses frames",()=>{
  const w=seed(), zone=zoneFor(w,"decision");
  const moved=moveZoneInLayout(w,zone.id,zone.x+900,zone.y);
  valid(moved.workspace);
  assert.equal(moved.blocked,true);
  const dx=zoneFor(moved.workspace,"decision").x-zone.x;
  for (const [i,n] of moved.workspace.nodes.entries()) assert.equal(n.x,w.nodes[i].x+(n.group==="decision"?dx:0));
  for (const dimensions of [[1,1],[1800,1500],[zone.width+3,zone.height+3]]) {
    valid(resizeZoneInLayout(w,zone.id,...dimensions).workspace);
  }
});

test("repeated pointer-sized and keyboard-sized edits maintain layout invariants",()=>{
  let w=seed();
  let state=17;
  const random=()=>{state=(state*1664525+1013904223)>>>0;return state/2**32;};
  for(let i=0;i<1500;i++){
    const node=w.nodes[Math.floor(random()*w.nodes.length)], zone=w.zones[Math.floor(random()*w.zones.length)];
    const dx=(random()-.5)*(i%2?50:1800),dy=(random()-.5)*(i%2?50:1100);
    w=i%3===0?moveNodeInLayout(w,node.id,node.x+dx,node.y+dy).workspace
      :i%3===1?moveZoneInLayout(w,zone.id,zone.x+dx,zone.y+dy).workspace
      :resizeZoneInLayout(w,zone.id,zone.width+dx,zone.height+dy).workspace;
    valid(w);
  }
});
