"use client";

import { useState, type CSSProperties } from "react";
import { getMechanicDiagram, type MechanicDiagramKind } from "../data/mechanicDiagrams";

type DiagramStyle = CSSProperties & Record<`--${string}`, string | number>;

function at(x: number, y: number): DiagramStyle {
  return { "--left": `${(x + 0.5) * (100 / 6)}%`, "--top": `${(y + 0.5) * 25}%` };
}

function Unit({ x, y, team, role = "soldier", ghost = false, defeated = false }: { x: number; y: number; team: "ally" | "enemy"; role?: string; ghost?: boolean; defeated?: boolean }) {
  return <span className={`diagram-unit ${team} ${role} ${ghost ? "ghost" : ""} ${defeated ? "defeated" : ""}`} style={at(x, y)}><i /><b /></span>;
}

function Cell({ x, y, tone = "range", motion = "" }: { x: number; y: number; tone?: string; motion?: string }) {
  return <span className={`diagram-cell ${tone} ${motion}`} style={at(x, y)} />;
}

function Path({ left, top, width, angle = 0, tone = "focus", className = "" }: { left: string; top: string; width: string; angle?: number; tone?: string; className?: string }) {
  return <span className={`diagram-path ${tone} ${className}`} style={{ left, top, width, "--angle": `${angle}deg` } as DiagramStyle} />;
}

function Mark({ x, y, kind, className = "" }: { x: number; y: number; kind: string; className?: string }) {
  return <span className={`diagram-mark ${kind} ${className}`} style={at(x, y)}><i /></span>;
}

function DiagramLabel({ x, y, children, tone = "neutral", className = "" }: { x: number; y: number; children: React.ReactNode; tone?: "neutral" | "danger" | "focus" | "muted"; className?: string }) {
  return <span className={`diagram-label ${tone} ${className}`} style={{ left: `${x}%`, top: `${y}%` }}>{children}</span>;
}

type StoryboardKind = "threat-redirection" | "charge-displacement" | "aoe-counter";

export const storyboardKinds: readonly StoryboardKind[] = [
  "threat-redirection",
  "charge-displacement",
  "aoe-counter",
];

function isStoryboardKind(kind: MechanicDiagramKind): kind is StoryboardKind {
  return storyboardKinds.includes(kind as StoryboardKind);
}

function StoryboardPanel({ title, phase, children }: { title: string; phase: "before" | "after"; children: React.ReactNode }) {
  return <section className={`storyboard-panel ${phase}`}>
    <header><span>{title}</span>{phase === "before" ? <small>01</small> : <small>02</small>}</header>
    <div className="storyboard-grid">{children}</div>
  </section>;
}

function Storyboard({ kind }: { kind: StoryboardKind }) {
  if (kind === "threat-redirection") return <div className="diagram-storyboard">
    <StoryboardPanel title="命中前" phase="before">
      <Unit x={1} y={2} team="ally" role="core" />
      <Unit x={2} y={1} team="ally" role="guard" />
      <Unit x={5} y={0} team="enemy" />
      <Unit x={5} y={3} team="enemy" />
      <Path left="28%" top="63%" width="66%" angle={-28} tone="danger" className="original reverse motion-1" />
      <Path left="28%" top="63%" width="66%" angle={22} tone="danger" className="original reverse motion-1" />
      <DiagramLabel x={28} y={82} tone="danger">原目标</DiagramLabel>
    </StoryboardPanel>
    <span className="storyboard-arrow" aria-hidden="true">→</span>
    <StoryboardPanel title="命中后" phase="after">
      <Unit x={1} y={2} team="ally" role="core" />
      <Unit x={2} y={1} team="ally" role="guard" />
      <Unit x={5} y={0} team="enemy" />
      <Unit x={5} y={3} team="enemy" />
      <Path left="41%" top="38%" width="53%" angle={-12} className="reverse motion-2" />
      <Path left="41%" top="38%" width="56%" angle={32} className="reverse motion-2" />
      <Mark x={2} y={1} kind="threat" className="motion-3" />
      <DiagramLabel x={43} y={14} tone="focus" className="motion-3">嘲讽后</DiagramLabel>
    </StoryboardPanel>
  </div>;

  if (kind === "charge-displacement") return <div className="diagram-storyboard">
    <StoryboardPanel title="命中前" phase="before">
      <Unit x={0} y={1} team="ally" role="charger" />
      <Unit x={3} y={1} team="enemy" />
      <Path left="13%" top="38%" width="50%" className="charge motion-1" />
      <Mark x={1} y={1} kind="momentum" />
      <Mark x={2} y={1} kind="momentum" />
      <Mark x={3} y={1} kind="impact" className="motion-2" />
      <DiagramLabel x={56} y={15} tone="danger">首次接敌</DiagramLabel>
    </StoryboardPanel>
    <span className="storyboard-arrow" aria-hidden="true">→</span>
    <StoryboardPanel title="命中后" phase="after">
      <Unit x={2} y={1} team="ally" role="charger" />
      <Unit x={3} y={1} team="enemy" ghost />
      <Unit x={4} y={1} team="enemy" />
      <Path left="59%" top="38%" width="18%" className="displace motion-3" />
      <DiagramLabel x={57} y={13} tone="focus">沿冲锋方向击退1格</DiagramLabel>
      <span className="diagram-branch-divider" />
      <Unit x={3} y={3} team="enemy" />
      <span className="diagram-obstacle" />
      <Mark x={3} y={3} kind="impact" className="motion-3" />
      <DiagramLabel x={25} y={65} tone="danger" className="blocked-branch-label">阻挡 / 边缘</DiagramLabel>
      <DiagramLabel x={24} y={86} tone="danger" className="blocked-outcome-label">不位移 · 碰撞 / 眩晕</DiagramLabel>
    </StoryboardPanel>
  </div>;

  return <div className="diagram-storyboard">
    <StoryboardPanel title="命中前" phase="before">
      <Unit x={0} y={2} team="ally" role="caster" />
      <span className="aoe-boundary motion-1" />
      <Unit x={2} y={1} team="enemy" />
      <Unit x={3} y={1} team="enemy" />
      <Unit x={4} y={1} team="enemy" />
      <Unit x={2} y={2} team="enemy" />
      <Unit x={3} y={2} team="enemy" />
      <Unit x={4} y={2} team="enemy" />
      <Unit x={3} y={3} team="enemy" />
      <DiagramLabel x={58} y={12} tone="muted">密集低生命敌群</DiagramLabel>
    </StoryboardPanel>
    <span className="storyboard-arrow" aria-hidden="true">→</span>
    <StoryboardPanel title="命中后" phase="after">
      <Unit x={0} y={2} team="ally" role="caster" />
      <span className="aoe-boundary resolved motion-2" />
      <Unit x={2} y={1} team="enemy" defeated />
      <Unit x={3} y={1} team="enemy" defeated />
      <Unit x={4} y={1} team="enemy" defeated />
      <Unit x={2} y={2} team="enemy" defeated />
      <Unit x={3} y={2} team="enemy" defeated />
      <Unit x={4} y={2} team="enemy" defeated />
      <Unit x={3} y={3} team="enemy" defeated />
      <Mark x={3} y={2} kind="blast" className="motion-3" />
      <DiagramLabel x={56} y={12} tone="danger">范围内同时结算</DiagramLabel>
    </StoryboardPanel>
  </div>;
}

function DiagramScene({ kind, mode }: { kind: MechanicDiagramKind; mode: "summary" | "detail" }) {
  switch (kind) {
    case "cone-coverage": return <><Unit x={0} y={2} team="ally" role="caster" /><Cell x={1} y={2} motion="motion-1" /><Cell x={2} y={1} motion="motion-2" /><Cell x={2} y={2} motion="motion-2" /><Cell x={2} y={3} motion="motion-2" /><Cell x={3} y={0} motion="motion-3" /><Cell x={3} y={1} motion="motion-3" /><Cell x={3} y={2} motion="motion-3" /><Cell x={3} y={3} motion="motion-3" /><Unit x={2} y={1} team="enemy" /><Unit x={3} y={2} team="enemy" /><Unit x={3} y={3} team="enemy" /><Path left="13%" top="63%" width="50%" className="motion-1" /><DiagramLabel x={72} y={15} tone="focus">扇形覆盖</DiagramLabel></>;
    case "line-pierce": return <><Unit x={0} y={2} team="ally" role="ranged" /><Unit x={2} y={2} team="enemy" /><Unit x={3} y={2} team="enemy" /><Unit x={5} y={2} team="enemy" /><Path left="13%" top="63%" width="78%" className="pierce motion-1" /><Mark x={2} y={2} kind="hit" className="motion-2" /><Mark x={3} y={2} kind="hit" className="motion-2" /><Mark x={5} y={2} kind="hit" className="motion-3" /><DiagramLabel x={51} y={23} tone="focus">穿透路径</DiagramLabel></>;
    case "guard-intercept": return <><Unit x={1} y={2} team="ally" role="core" /><Unit x={2} y={1} team="ally" role="guard" /><Unit x={4} y={2} team="enemy" ghost /><Unit x={3} y={2} team="enemy" /><Cell x={2} y={2} tone="threat-zone" /><Cell x={3} y={1} tone="threat-zone" /><Cell x={3} y={2} tone="threat-zone" /><Path left="80%" top="63%" width="60%" angle={180} tone="danger" className="original motion-1" /><Mark x={3} y={2} kind="stop" className="motion-2" /><Path left="44%" top="38%" width="22%" angle={38} className="motion-3" /><DiagramLabel x={64} y={17} tone="danger">进入威胁格 → 截停</DiagramLabel></>;
    case "shield-break": return <><Unit x={2} y={2} team="ally" role="shielded" /><Unit x={5} y={2} team="enemy" role="ranged" /><Path left="87%" top="63%" width="45%" angle={180} tone="danger" className="motion-1" /><span className="shield-ring motion-2" style={at(2, 2)} /><span className="health-bar full" style={at(2, 2)}><i /></span><Mark x={2} y={2} kind="break" className="motion-3" /><DiagramLabel x={36} y={18} tone="focus">护盾先承伤</DiagramLabel></>;
    case "corpse-conversion": return <><Unit x={0} y={2} team="ally" ghost defeated /><Mark x={2} y={2} kind="corpse" className="motion-1" /><Path left="42%" top="63%" width="33%" className="motion-2" /><Mark x={2} y={2} kind="consumed" className="motion-2" /><Unit x={4} y={2} team="ally" role="summon" /><Mark x={4} y={2} kind="summon" className="motion-3" /><DiagramLabel x={52} y={18} tone="focus">尸体 → 召唤</DiagramLabel></>;
    case "overheal-charge": return <><Unit x={0} y={2} team="ally" role="healer" /><Unit x={2} y={2} team="ally" role="core" /><Path left="13%" top="63%" width="28%" className="heal motion-1" /><span className="health-bar full" style={at(2, 2)}><i /></span><Mark x={2} y={1} kind="heal" className="motion-1" /><Path left="40%" top="53%" width="27%" angle={-22} className="overflow motion-2" /><span className="charge-meter" aria-hidden="true"><i /><i /><i /><i /></span><Mark x={5} y={1} kind="effect" className="motion-3" /><DiagramLabel x={55} y={84} tone="focus">过量治疗 → 有界充能</DiagramLabel></>;
    case "armor-break-counter": return <><Unit x={3} y={2} team="enemy" role="armored" /><span className="armor-segments" style={at(3, 2)}><i /><i /><i /><i /></span><Path left="12%" top="38%" width="42%" angle={18} className="small-hit motion-1" /><Path left="12%" top="63%" width="42%" className="small-hit motion-1" /><Path left="12%" top="85%" width="42%" angle={-18} className="small-hit motion-2" /><Mark x={3} y={2} kind="armor-crack" className="motion-2" /><Path left="12%" top="63%" width="47%" tone="danger" className="finisher motion-3" /><DiagramLabel x={69} y={19} tone="danger">先破甲 → 再重击</DiagramLabel></>;
    case "backline-counter": return <><Unit x={1} y={2} team="ally" role="core" /><Unit x={2} y={1} team="ally" role="guard" /><Mark x={3} y={3} kind="decoy" /><Unit x={5} y={0} team="enemy" role="hunter" /><Path left="24%" top="63%" width="72%" angle={-30} tone="danger" className="original reverse motion-1" /><Path left="42%" top="39%" width="50%" angle={-7} className="reverse motion-2" /><Path left="58.333%" top="87.5%" width={mode === "detail" ? "60.1%" : "55%"} angle={mode === "detail" ? -56.3 : -52.7} className="answer reverse motion-3" /><Cell x={2} y={2} tone="threat-zone" /><Cell x={3} y={1} tone="threat-zone" /><DiagramLabel x={63} y={18} tone="focus">护卫 / 诱饵改道</DiagramLabel></>;
    case "boss-timeline": return <><span className="timeline-line" /><Mark x={0} y={2} kind="telegraph" className="motion-1" /><span className="timeline-window motion-2" /><Mark x={3} y={2} kind="command" className="motion-3" /><Mark x={5} y={2} kind="resolve" className="motion-4" /><Unit x={5} y={0} team="enemy" role="boss" /><DiagramLabel x={48} y={18} tone="focus">预告 → 响应 → 结算</DiagramLabel></>;
  }
}

export function MechanicDiagram({ mechanicId, mode = "summary", onNavigate }: { mechanicId: string; mode?: "summary" | "detail"; onNavigate?: (id: string) => void }) {
  const definition = getMechanicDiagram(mechanicId);
  const [playing, setPlaying] = useState(false);
  if (!definition) return null;

  const storyboard = isStoryboardKind(definition.kind);

  return <div className={`mechanic-diagram-wrap ${mode}`} data-mechanic-diagram={mechanicId} data-visual-kind={definition.kind}>
    <div className={`mechanic-diagram ${storyboard ? "has-storyboard" : ""} ${playing ? "is-playing" : "is-paused"}`} role="img" aria-label={definition.ariaDescription}>
      <div aria-hidden="true">{storyboard ? <Storyboard kind={definition.kind} /> : <div className="diagram-grid"><DiagramScene kind={definition.kind} mode={mode} /></div>}</div>
    </div>
    {mode === "detail" && <div className="diagram-explanation">
      <div><span>图解语义</span><p>{definition.caption}</p><ol>{definition.steps.map((step) => <li key={step}>{step}</li>)}</ol></div>
      {definition.counter && <div className="diagram-counter"><span>{definition.counter.pressure}</span><b aria-hidden="true">→</b><span>{definition.counter.answer}</span>{onNavigate && <button onClick={() => onNavigate(definition.counter!.targetId)}>{definition.counter.linkLabel}<i aria-hidden="true">→</i></button>}</div>}
      <button className="diagram-playback" aria-pressed={playing} onClick={() => setPlaying((current) => !current)}>{playing ? "暂停演示" : "播放演示"}<span aria-hidden="true">{playing ? "Ⅱ" : "▶"}</span></button>
    </div>}
  </div>;
}
