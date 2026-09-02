"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import Link from "next/link";
import { runProgressionConcepts, type RunProgressionConcept } from "../data/runProgression";

const fieldRows: Array<{ key: keyof RunProgressionConcept; label: string; mark: string }> = [
  { key: "randomizedProblem", label: "随机题目", mark: "◇" },
  { key: "playerAction", label: "玩家行动", mark: "→" },
  { key: "buildPayoff", label: "构筑收益", mark: "↑" },
  { key: "progressionPressure", label: "进程压力", mark: "!" },
  { key: "terminalCondition", label: "终局条件", mark: "◆" },
  { key: "principalRisk", label: "主要风险", mark: "△" },
];

type EnlargedImage = { src: string; alt: string; label: string };

export default function RunProgressionBrowser() {
  const [selectedIndex, setSelectedIndex] = useState(0);
  const [comparisonOpen, setComparisonOpen] = useState(false);
  const [enlarged, setEnlarged] = useState<EnlargedImage | null>(null);
  const closeButton = useRef<HTMLButtonElement>(null);
  const enlargeTrigger = useRef<HTMLButtonElement | null>(null);
  const selected = runProgressionConcepts[selectedIndex];

  const select = (index: number) => {
    setSelectedIndex(index);
    setComparisonOpen(false);
    document.getElementById("selected-framework")?.scrollIntoView({ behavior: "auto", block: "start" });
  };

  const move = (direction: -1 | 1) => {
    const next = (selectedIndex + direction + runProgressionConcepts.length) % runProgressionConcepts.length;
    select(next);
  };

  const openImage = (image: EnlargedImage, trigger: HTMLButtonElement) => {
    enlargeTrigger.current = trigger;
    setEnlarged(image);
  };

  const closeImage = useCallback(() => {
    setEnlarged(null);
    requestAnimationFrame(() => enlargeTrigger.current?.focus());
  }, []);

  useEffect(() => {
    if (!enlarged) return;
    closeButton.current?.focus();
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") closeImage();
      if (event.key === "Tab") {
        event.preventDefault();
        closeButton.current?.focus();
      }
    };
    document.body.style.overflow = "hidden";
    window.addEventListener("keydown", onKeyDown);
    return () => {
      document.body.style.overflow = "";
      window.removeEventListener("keydown", onKeyDown);
    };
  }, [enlarged, closeImage]);

  return (
    <main className="run-page">
      <header className="run-hero">
        <nav aria-label="页面路径">
          <Link href="/">机制设计图谱</Link><span aria-hidden="true">/</span><strong>局内进程</strong>
        </nav>
        <div className="run-hero-copy">
          <div><p className="run-kicker">RUN PROGRESSION · 10 FRAMEWORKS</p><h1>十种完整局内进程</h1></div>
          <p>随机局势提出问题，玩家主动追求构筑，自动战斗兑现成长。这里比较的是一局如何运转，不是十张地图皮肤。</p>
        </div>
        <div className="run-legend" aria-label="图例">
          <span><b>◇</b> 随机生成题目</span><span><b>→</b> 玩家作出选择</span><span><b>↑</b> 战斗反馈成长</span><span><b>◆</b> 汇入终局</span>
        </div>
      </header>

      <section className="framework-picker" aria-labelledby="framework-picker-title">
        <div className="picker-head"><div><p>选择框架</p><h2 id="framework-picker-title">同一套战斗核心，十种推进方式</h2></div><button className="compare-toggle" onClick={() => setComparisonOpen((open) => !open)} aria-expanded={comparisonOpen} aria-controls="framework-comparison">{comparisonOpen ? "收起快速比较" : "展开快速比较"}<span aria-hidden="true">{comparisonOpen ? "−" : "+"}</span></button></div>
        <div className="framework-tabs" role="tablist" aria-label="十种局内进程框架">
          {runProgressionConcepts.map((concept, index) => <button key={concept.id} role="tab" aria-selected={index === selectedIndex} aria-controls="selected-framework" className={index === selectedIndex ? "active" : ""} onClick={() => select(index)}><small>{concept.index}</small><span>{concept.shortTitle}</span><i style={{ background: concept.accent }} aria-hidden="true" /></button>)}
        </div>
        {comparisonOpen && <div id="framework-comparison" className="framework-comparison"><div className="comparison-labels" aria-hidden="true"><span>框架</span><span>构筑如何被追求</span><span>主要压力</span></div>{runProgressionConcepts.map((concept, index) => <button key={concept.id} onClick={() => select(index)}><span><small>{concept.index}</small><strong>{concept.shortTitle}</strong></span><p>{concept.buildPayoff}</p><p>{concept.progressionPressure}</p><b aria-hidden="true">→</b></button>)}</div>}
      </section>

      <article id="selected-framework" className="selected-framework" style={{ "--run-accent": selected.accent } as React.CSSProperties} aria-labelledby="selected-title">
        <header className="selected-head">
          <div className="selected-index"><span>{selected.index}</span><small>/ 10</small></div>
          <div><p>当前框架</p><h2 id="selected-title">{selected.title}</h2><strong>{selected.thesis}</strong></div>
          <div className="step-navigation" aria-label="切换框架"><button onClick={() => move(-1)} aria-label="上一套局内进程">←<span>上一套</span></button><button onClick={() => move(1)} aria-label="下一套局内进程"><span>下一套</span>→</button></div>
        </header>

        <section className="image-board" aria-label={`${selected.title}机制图`}>
          <figure className="run-figure overview-figure"><button onClick={(event) => openImage({ src: selected.overviewImage, alt: selected.overviewAlt, label: `${selected.title} · 整局骨架` }, event.currentTarget)} aria-label={`放大查看${selected.title}整局骨架`}>
            {/* Accepted local raster diagrams are served directly so only the selected pair reaches the browser. */}
            {/* eslint-disable-next-line @next/next/no-img-element */}
            <img key={selected.overviewImage} src={selected.overviewImage} alt={selected.overviewAlt} width="1536" height="1024" fetchPriority="high" />
          </button><figcaption><span>01</span><div><strong>整局骨架</strong><small>随机开局 → 发展压力 → 终局</small></div><b aria-hidden="true">放大 ↗</b></figcaption></figure>
          <figure className="run-figure loop-figure"><button onClick={(event) => openImage({ src: selected.loopImage, alt: selected.loopAlt, label: `${selected.title} · 决策闭环` }, event.currentTarget)} aria-label={`放大查看${selected.title}决策闭环`}>
            {/* eslint-disable-next-line @next/next/no-img-element */}
            <img key={selected.loopImage} src={selected.loopImage} alt={selected.loopAlt} loading="lazy" decoding="async" />
          </button><figcaption><span>02</span><div><strong>关键决策闭环</strong><small>主动选择 → 自动战斗 → 构筑反馈</small></div><b aria-hidden="true">放大 ↗</b></figcaption></figure>
        </section>

        <section className="mechanism-fields" aria-label={`${selected.title}六项机制摘要`}>
          {fieldRows.map((field) => <div key={field.key} className={field.key === "principalRisk" ? "risk-field" : ""}><span aria-hidden="true">{field.mark}</span><section><h3>{field.label}</h3><p>{selected[field.key]}</p></section></div>)}
        </section>
      </article>

      <footer className="run-footer"><p>这十套仍是探索材料，不代表项目已选定正式进程结构。</p><Link href="/">返回机制设计图谱 <span aria-hidden="true">→</span></Link></footer>

      {enlarged && <div className="image-lightbox" role="dialog" aria-modal="true" aria-label={enlarged.label} onMouseDown={(event) => { if (event.target === event.currentTarget) closeImage(); }}><div><header><span>{enlarged.label}</span><button ref={closeButton} onClick={closeImage} aria-label="关闭大图">关闭 ×</button></header>
        {/* eslint-disable-next-line @next/next/no-img-element */}
        <img src={enlarged.src} alt={enlarged.alt} />
      </div></div>}
    </main>
  );
}
