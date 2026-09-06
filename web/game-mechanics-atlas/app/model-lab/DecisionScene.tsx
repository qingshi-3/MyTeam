"use client";

import { useState } from "react";
import type { FoundationDecision, FoundationWorkspace } from "./model";
import { SUPPLY_SCENARIO, SUPPLY_OPTIONS, SUPPLY_EVIDENCE, decisionNeedsReview, supplyOutcome } from "./decisions";

export const DECISION_STATUS = {tentative:"暂定方向",confirmed:"已确认方向",deferred:"暂缓决定"};

export default function DecisionScene({workspace,onRecord,onInspect,onGraph,saveError,saved,readOnly}: {
  workspace:FoundationWorkspace;
  onRecord:(optionId:string,status:FoundationDecision["status"],note:string)=>void;
  onInspect:(id:string)=>void;
  onGraph:()=>void;
  saveError:string;
  saved:boolean;
  readOnly:boolean;
}) {
  const record=workspace.decisions?.[SUPPLY_SCENARIO.id];
  const [optionId,setOptionId]=useState(record?.optionId ?? "fallback");
  const [revealed,setRevealed]=useState(false);
  const [offerIndex,setOfferIndex]=useState<number|null>(null);
  const [note,setNote]=useState(record?.note??"");
  const [confirming,setConfirming]=useState(false);
  const option=SUPPLY_OPTIONS.find(o=>o.id===optionId)??SUPPLY_OPTIONS[1];
  const outcome=offerIndex===null?null:supplyOutcome(option.id,offerIndex);
  const stale=decisionNeedsReview(workspace);
  const missing=SUPPLY_SCENARIO.nodeIds.filter(id=>!workspace.nodes.some(n=>n.id===id));
  const recordOption=SUPPLY_OPTIONS.find(o=>o.id===record?.optionId);
  const choose=(id:string)=>{setOptionId(id);setRevealed(false);setOfferIndex(null);setConfirming(false);};
  const commit=(status:FoundationDecision["status"])=>{onRecord(option.id,status,note);setConfirming(false);};

  return <div className="decision-viewport">
    <div className="decision-page">
      <header className="decision-page-header"><span>玩法决策 <span className="decision-count">01 / 01</span></span><button type="button" onClick={onGraph}>打开基础关系图 <span aria-hidden="true">↗</span></button></header>
      <section className="decision-question" aria-labelledby="decision-title">
        <div className="decision-kicker">招募与补救 · 尚未写入游戏规则</div>
        <h1 id="decision-title">一直招不到需要的角色，<br className="decision-title-break"/>怎么办？</h1>
        <p>你已有一个依靠状态兑现效果的角色，却连续错过了状态施加者。<br/>先体验不同补救方式，再决定游戏应该给玩家多大的主动权。</p>
      </section>

      <div className="decision-workbench">
        <section className="decision-playground" aria-label="同一个局面的方案演示">
          <div className="decision-section-heading"><h2>先看这条还没接上的链</h2><span>固定情境 · 非战斗模拟</span></div>
          <div className={`decision-chain${outcome?.connects ? " is-connected" : ""}`} aria-label={outcome?.connects ? "职责链已补齐，效果尚未验证" : "缺少状态施加者，职责链断开"}>
            <div className="decision-chain-part missing"><span>{outcome?.connects ? "本次补位" : "还缺这一环"}</span><strong>{outcome?.connects ? outcome.name : "状态施加者"}</strong><small>{outcome?.connects ? "提供需要的状态" : "没有稳定的状态来源"}</small></div>
            <span className="decision-chain-arrow" aria-hidden="true">{outcome?.connects ? "→" : "⇢"}</span>
            <div className="decision-chain-part"><span>你已拥有</span><strong>读取与兑现者</strong><small>读取目标状态，兑现额外效果</small></div>
          </div>

          <fieldset className="decision-options"><legend>换一种补救规则，看看选择怎样变化</legend>
            {SUPPLY_OPTIONS.map(o=><label key={o.id} className={`decision-option${option.id===o.id ? " is-active" : ""}`}>
              <input type="radio" name="supply-direction" value={o.id} checked={option.id===o.id} onChange={()=>choose(o.id)} />
              <span><strong>{o.name}</strong><small>{o.subtitle}</small></span><em>{o.badge}</em>
            </label>)}
          </fieldset>

          <div className="decision-reveal">
            <p>{option.rule}</p>
            {!revealed ? <button type="button" className="decision-primary" onClick={()=>setRevealed(true)}>{option.action} <span aria-hidden="true">→</span></button>
              : <div className="decision-offers" aria-label="示例候选，选择一个查看后果">{option.offers.map((offer,i)=><button type="button" key={`${option.id}-${i}`} aria-pressed={offerIndex===i} onClick={()=>setOfferIndex(i)}>
                <span>{offer.fit ? "可补缺口" : "其他职责"}</span><strong>{offer.name}</strong><small>{offer.role}</small><b>{offerIndex===i ? "已试选 ✓" : "试选 →"}</b>
              </button>)}</div>}
            <div className="decision-demo-feedback" aria-live="polite">{outcome ? <><strong>{outcome.connects ? "链路接上了，但不是完整答案。" : "得到了过渡选择，缺口还在。"}</strong><p>{outcome.explanation}</p></> : revealed ? <><strong>现在你会怎么选？</strong><p>{option.actionResult}</p></> : <p>同一个缺口，三种不同的补救规则。切换方案只改变演示，不会保存决定。</p>}</div>
            <small className="decision-demo-limit">候选与落空顺序是为比较方案编排的示例，不是抽样结果；不表示真实概率、角色强度或胜率。</small>
          </div>
        </section>

        <aside className="decision-tradeoffs" aria-label="方案取舍与建议">
          <div className="decision-section-heading"><h2>这意味着什么</h2></div>
          <section><h3>玩家得到</h3><p>{option.gain}</p></section>
          <section><h3>要付出的代价</h3><p>{option.cost}</p><p className="decision-cost">{option.price}</p></section>
          <section className="decision-recommendation"><h3>我的建议：先试“有退路”</h3><p>我们已经要求每个英雄有明确的即时用途，也不把所有角色都当核心。先保护一条可用退路，比保证指定角色更符合这个方向。</p><p>这是待检验的设计判断，不是调研已经证明的最优答案。</p></section>
          <details><summary>后面还要决定什么</summary><p>{option.open}</p><p>本轮只决定补救方向，不锁定数值、全部供给规则或具体英雄。</p></details>
        </aside>
      </div>

      <section className="decision-record" aria-labelledby="decision-record-title">
        <div><h2 id="decision-record-title">你的判断</h2><p>可以先暂定，也可以留下不同意见。暂缓决定不会改动已确认的游戏规则。</p></div>
        {record && <div className={`decision-saved-record${stale ? " needs-review" : ""}`} role="status"><strong>{stale ? "关联规则已变化 · 需要复核" : DECISION_STATUS[record.status]}</strong><span>{recordOption?.name??"原方案已不可用"}</span><small>{saved ? "已保存到本地模型" : "本地模型已更新，等待保存"}</small></div>}
        <label className="decision-note">我更在意……<textarea rows={3} value={note} onChange={e=>{setNote(e.target.value);setConfirming(false);}} placeholder="例如：保留随机，但不要让玩家一直花资源却没有补救。"/><small>意见随决定保存，留给下一轮讨论；不会自动解释成规则修改。</small></label>
        {missing.length>0 && <p className="decision-warning" role="alert">关联基础域 {missing.map(id=>id.toUpperCase()).join("、")} 已被移除。先回关系图处理，不能据此确认方案。</p>}
        {saveError && <p className="decision-warning" role="alert">{saveError}</p>}
        <div className="decision-record-actions">
          <button type="button" className="decision-primary" disabled={readOnly||!!missing.length} onClick={()=>commit("tentative")}>暂定“{option.name}”</button>
          <button type="button" disabled={readOnly||!!missing.length} onClick={()=>setConfirming(true)}>确认这个方向</button>
          <button type="button" disabled={readOnly||!!missing.length} onClick={()=>commit("deferred")}>先放着，记录意见</button>
        </div>
        {confirming && <div className="decision-confirm" role="region" aria-label="确认方向范围"><p>确认“{option.name}”，并关联到供给、经济、信息和局内节奏。<strong>只确认这项补救方向，不会把 F06 等整个规则域标为已定案。</strong></p><button type="button" className="decision-primary" onClick={()=>commit("confirmed")}>确认并记录到模型</button><button type="button" onClick={()=>setConfirming(false)}>继续比较</button></div>}
      </section>

      <section className="decision-foundations" aria-labelledby="decision-foundations-title"><div><h2 id="decision-foundations-title">落到哪些基础规则</h2><p>标题与状态来自现有模型。点击后进入原来的关系图，继续检查上下游。</p></div><div className="decision-rule-links">{SUPPLY_SCENARIO.links.map(link=>{
        const node=workspace.nodes.find(n=>n.id===link.id);
        return <button type="button" key={link.id} disabled={!node} onClick={()=>onInspect(link.id)}><b>{node?.code??link.id.toUpperCase()}</b><span><strong>{node?.label??"规则已移除"}</strong><small>{link.question}</small></span><i aria-hidden="true">↗</i></button>;
      })}</div></section>

      <details className="decision-evidence"><summary>建议从哪里来 <span>3 条已有调研 · 查看依据与限制</span></summary><div>{SUPPLY_EVIDENCE.map(e=><article key={e.id}><h3>{e.game}</h3><p>{e.lesson}</p><p className="decision-evidence-limit">{e.limit}</p><code>{e.id}</code><a href={e.url} target="_blank" rel="noreferrer">查看原始来源 ↗</a></article>)}</div></details>
      <footer className="decision-footer">这是一项设计决策，不是游戏规则的全部。基础模型、关系图和已有调研都保留。</footer>
    </div>
  </div>;
}
