import type { FoundationDecision, FoundationWorkspace } from "./model.ts";

export const SUPPLY_SCENARIO = {
  id: "recruitment-recovery", revision: 1,
  title: "一直招不到需要的角色，怎么办？",
  nodeIds: ["f06", "f15", "f05", "f02"],
  links: [
    {id:"f06", question:"补救怎样改变候选供给？"},
    {id:"f15", question:"为补救付出什么机会成本？"},
    {id:"f05", question:"玩家事先能知道什么？"},
    {id:"f02", question:"还剩多少次调整机会？"},
  ],
};

export const SUPPLY_OPTIONS = [
  {id:"reroll", name:"花资源，再找一次", subtitle:"把补救交给玩家的预算", badge:"更强调冒险",
    rule:"允许付费刷新，但不保证出现缺失角色，也不保证补齐职责。",
    gain:"保留意外收获与临场转型；预算分配本身成为选择。",
    cost:"连续落空仍可能发生；投入越深，越容易陷入不愿转型的局面。",
    action:"支付资源，试一次刷新", actionResult:"这次示例刷新仍未出现施加者。你可以接受过渡角色，也可以停止投入。",
    price:"刷新占用原本可用于招募或强化的资源。", open:"刷新价格、次数限制，以及何时提示停止追逐。",
    offers:[{name:"稳定前排",role:"改善承伤",fit:false},{name:"过渡输出",role:"直接造成伤害",fit:false},{name:"通用辅助",role:"提供一般增益",fit:false}]},
  {id:"fallback", name:"给一条能走的退路", subtitle:"保证有补救，不保证理想构筑", badge:"建议先试",
    rule:"补救机会中至少提供一个可立即承担缺失职责的过渡选项；不保证指定角色或最终核心。",
    gain:"玩家仍要适应随机供给，但已投入的思路不必因为缺一环就被迫废弃。",
    cost:"需要定义什么才算有效补位；补位过强会降低转型和取舍的价值。",
    action:"查看这次的补救选项", actionResult:"多出一个过渡施加者。它能提供所需状态，但没有理想角色的其他能力。是否值得占用位置，由你决定。",
    price:"选择过渡位会占用一次招募机会和一个英雄位置；没有免费完成构筑。", open:"何时开放补救、怎样识别缺失职责，以及过渡选项的能力边界。",
    offers:[{name:"稳定前排",role:"改善承伤",fit:false},{name:"过渡施加者",role:"提供缺失状态",fit:true},{name:"过渡输出",role:"直接造成伤害",fit:false}]},
  {id:"targeted", name:"指定职责，再招募", subtitle:"用成本换取更明确的方向", badge:"更强调规划",
    rule:"玩家主动选择要寻找的职责，从对应候选范围招募；具体角色仍不指定。",
    gain:"目标和行动关系明确，玩家能为缺失环节主动安排路线与资源。",
    cost:"如果定向获得过于便利，重复路线可能替代临场适应；还要处理候选池为空。",
    action:"指定寻找“状态施加者”", actionResult:"示例进入施加者候选池。三个选项承担同一职责，但作用范围和代价不同。",
    price:"使用定向渠道需要额外资源或路线机会；具体费用尚未决定。", open:"定向渠道的成本、开放阶段、空池处理，以及能否重复使用。",
    offers:[{name:"单体施加者",role:"集中作用于单个目标",fit:true},{name:"范围施加者",role:"覆盖多个目标",fit:true},{name:"间歇施加者",role:"间隔一段时间提供状态",fit:true}]},
] as const;

export const SUPPLY_EVIDENCE = [
  {id:"ev-sap-001-shop-gold-replacement",game:"Super Auto Pets",lesson:"付费刷新和保留候选让“继续搜索还是先变强”成为资源取舍。",limit:"非官方、同作者攻略；不移植价格、每轮收入或不结转金币的规则。",url:"https://www.twoaveragegamers.com/ultimate-guide-to-super-auto-pets-game-mechanics/"},
  {id:"ev-gods-vs-horrors-017-pool-pivot-reachability",game:"Gods vs Horrors",lesson:"组合数量不等于实际可达性；过渡选项和剩余转型时间决定一条退路能不能用。",limit:"攻略与玩家体验有分歧，没有概率或胜率数据；不能据此声称某种纠偏一定更好。",url:"https://steamcommunity.com/sharedfiles/filedetails/?id=3493071391"},
  {id:"ev-girls-of-the-tower-017-pool-reachability-pivot",game:"Girls of The Tower",lesson:"筛选与刷新提供主动获取手段，但还要考虑成本、组件缺口和获取期限。",limit:"官方更新未公开完整保底模型；这里的职责定向方案是我们的提案，不是原作规则复刻。",url:"https://steamstore-a.akamaihd.net/news/externalpost/steam_community_announcements/5746109972543686415"},
];

export function validDecisions(value: unknown): boolean {
  if (value === undefined) return true; // Existing v2 drafts have no decision layer.
  if (!value || typeof value !== "object" || Array.isArray(value)) return false;
  return Object.entries(value).every(([id,v]) => v && typeof v === "object"
    && v.scenarioId === id && ["optionId","note","basis","updatedAt"].every(key=>typeof v[key] === "string")
    && ["tentative","confirmed","deferred"].includes(v.status));
}

export function decisionBasis(workspace: FoundationWorkspace): string {
  const ids = new Set(SUPPLY_SCENARIO.nodeIds);
  return JSON.stringify({revision:SUPPLY_SCENARIO.revision,
    nodes:SUPPLY_SCENARIO.nodeIds.map(id=> {
      const n=workspace.nodes.find(node=>node.id===id);
      if(!n) return {id,missing:true};
      return {id:n.id,label:n.label,status:n.status,summary:n.summary,responsibility:n.responsibility,
        inputs:n.inputs,outputs:n.outputs,channels:n.channels,openQuestion:n.openQuestion,evidence:n.evidence};
    }),
    edges:workspace.edges.filter(e=>ids.has(e.from)||ids.has(e.to)).toSorted((a,b)=>a.id.localeCompare(b.id)),
  });
}

export function decisionNeedsReview(workspace: FoundationWorkspace): boolean {
  const record=workspace.decisions?.[SUPPLY_SCENARIO.id];
  return !!record && (record.basis!==decisionBasis(workspace) || !SUPPLY_OPTIONS.some(o=>o.id===record.optionId));
}

export function recordSupplyDecision(workspace: FoundationWorkspace, optionId: string, status: FoundationDecision["status"], note: string, updatedAt: string): FoundationWorkspace {
  if(!SUPPLY_OPTIONS.some(o=>o.id===optionId)) throw new Error("未知决策方案");
  if(SUPPLY_SCENARIO.nodeIds.some(id=>!workspace.nodes.some(n=>n.id===id))) throw new Error("关联规则缺失，请先恢复或讨论模型结构");
  return {...workspace,decisions:{...workspace.decisions,[SUPPLY_SCENARIO.id]:{
    scenarioId:SUPPLY_SCENARIO.id,optionId,status,note,basis:decisionBasis(workspace),updatedAt,
  }}};
}

export function supplyOutcome(optionId: string, offerIndex: number) {
  const option=SUPPLY_OPTIONS.find(o=>o.id===optionId);
  const offer=option?.offers[offerIndex];
  if(!offer) return null;
  return {name:offer.name,connects:offer.fit,
    explanation:offer.fit ? "职责链可以接上：施加状态 → 读取状态 → 兑现效果。但强度、费用与战斗结果仍需后续验证。"
      : "缺失环节仍未补齐。这个选项可以承担过渡职责，但你仍需决定继续寻找，还是调整原计划。"};
}
