import assert from "node:assert/strict";
import test from "node:test";
import { readFileSync } from "node:fs";
import { createFoundationWorkspace } from "../app/model-lab/model.ts";
import { normalizeLayout } from "../app/model-lab/layout.ts";
import {SUPPLY_SCENARIO,SUPPLY_OPTIONS,SUPPLY_EVIDENCE,validDecisions,recordSupplyDecision,decisionNeedsReview,supplyOutcome} from "../app/model-lab/decisions.ts";

test("a scenario records a scoped decision without accepting or replacing foundation rules",()=>{
  const w=createFoundationWorkspace(), original=structuredClone(w);
  const next=recordSupplyDecision(w,"fallback","confirmed","保留随机，但给退路","2026-09-05T10:00:00Z");
  assert.deepEqual(w,original);
  assert.equal(next.nodes,w.nodes);
  assert.equal(next.edges,w.edges);
  assert.equal(next.zones,w.zones);
  assert.equal(next.nodes.find(n=>n.id==='f06').status,'open');
  assert.equal(next.decisions[SUPPLY_SCENARIO.id].status,'confirmed');
  assert.equal(decisionNeedsReview(next),false);
  assert.equal(decisionNeedsReview(normalizeLayout(next)),false);
  assert.equal(validDecisions(JSON.parse(JSON.stringify(next.decisions))),true);
});

test("semantic edits and missing links require review; geometry and unrelated domains do not",()=>{
  const w=recordSupplyDecision(createFoundationWorkspace(),"reroll","tentative","","now");
  const change=(id,patch)=>({...w,nodes:w.nodes.map(n=>n.id===id?{...n,...patch}:n)});
  assert.equal(decisionNeedsReview(change('f06',{x:9999,y:-1000})),false);
  assert.equal(decisionNeedsReview(change('f12',{summary:'其他域的细节修改'})),false);
  assert.equal(decisionNeedsReview(change('f06',{summary:'供给契约变了'})),true);
  assert.equal(decisionNeedsReview({...w,edges:[...w.edges,{id:'new',from:'f06',to:'f03',relation:'feeds',label:'new'}]}),true);
  assert.equal(decisionNeedsReview({...w,nodes:w.nodes.filter(n=>n.id!=='f05')}),true);
  assert.throws(()=>recordSupplyDecision({...w,nodes:[]},'fallback','confirmed','','now'));
  assert.throws(()=>recordSupplyDecision(w,'invented','confirmed','','now'));
});

test("v2 drafts without decisions remain valid and malformed decision records are rejected",()=>{
  assert.equal(validDecisions(undefined),true);
  for(const value of [null,[],{a:{}},{a:{scenarioId:'a',optionId:'x',status:'confirmed',note:3,basis:'',updatedAt:''}}]) assert.equal(validDecisions(value),false);
  const w=recordSupplyDecision(createFoundationWorkspace(),'targeted','deferred','稍后讨论','now');
  assert.equal(validDecisions(w.decisions),true);
  assert.equal(decisionNeedsReview({...w,decisions:{...w.decisions,[SUPPLY_SCENARIO.id]:{...w.decisions[SUPPLY_SCENARIO.id],optionId:'unavailable'}}}),true);
});

test("illustrative choices expose distinct outcomes without claiming probabilities",()=>{
  assert.equal(supplyOutcome('reroll',0).connects,false);
  assert.equal(supplyOutcome('fallback',0).connects,false);
  assert.equal(supplyOutcome('fallback',1).connects,true);
  for(let i=0;i<3;i++) assert.equal(supplyOutcome('targeted',i).connects,true);
  assert.equal(supplyOutcome('unknown',0),null);
  assert.equal(supplyOutcome('fallback',99),null);
  assert.equal(new Set(SUPPLY_OPTIONS.map(o=>o.id)).size,SUPPLY_OPTIONS.length);
});

test("every scenario citation resolves into the protected research corpus",()=>{
  const corpus=JSON.parse(readFileSync(new URL('../research/deep/mechanic-evidence.json',import.meta.url),'utf8'));
  for(const reference of SUPPLY_EVIDENCE) {
    const evidence=corpus.records.find(r=>r.id===reference.id);
    assert.ok(evidence);
    assert.equal(evidence.game,reference.game);
    assert.ok(reference.limit && reference.lesson);
  }
});
