export const mechanicDiagramIds = [
  "battlefield.targeting.threat-lock",
  "battlefield.shape.cone",
  "battlefield.shape.line-pierce",
  "battlefield.engagement.intercept",
  "battlefield.engagement.charge",
  "combat.defense.shield",
  "combat.summon.corpse",
  "combat.trigger.overheal",
  "army.role.aoe-casters",
  "combat.damage.armor-break",
  "encounter.template.backline-hunter",
  "encounter.boss.timeline",
] as const;

export type MechanicDiagramId = typeof mechanicDiagramIds[number];
export type MechanicDiagramKind =
  | "threat-redirection"
  | "cone-coverage"
  | "line-pierce"
  | "guard-intercept"
  | "charge-displacement"
  | "shield-break"
  | "corpse-conversion"
  | "overheal-charge"
  | "aoe-counter"
  | "armor-break-counter"
  | "backline-counter"
  | "boss-timeline";

export type DiagramAudit = {
  actor: string;
  target: string;
  affected: string;
  path: string;
  beforeAfter: string;
};

export type DiagramCounter = {
  pressure: string;
  answer: string;
  targetId: string;
  linkLabel: string;
};

export type MechanicDiagramDefinition = {
  id: MechanicDiagramId;
  kind: MechanicDiagramKind;
  ariaDescription: string;
  caption: string;
  steps: [string, string, string];
  audit: DiagramAudit;
  counter?: DiagramCounter;
};

export const mechanicDiagramDefinitions: Record<MechanicDiagramId, MechanicDiagramDefinition> = {
  "battlefield.targeting.threat-lock": {
    id: "battlefield.targeting.threat-lock",
    kind: "threat-redirection",
    ariaDescription: "命中前面板中，两名敌人的虚线原目标指向脆弱后排；命中后面板中，两条实线箭头汇聚到带菱形威胁标记的护卫者，并直标嘲讽后。",
    caption: "前后态并列显示目标关系的改变：敌人原本追索后排核心；护卫者制造更高威胁后，多条目标线稳定转向同一承压单位。",
    steps: ["命中前：后排核心暴露在两条标为原目标的虚线上。", "前方单位累积威胁并越过锁定阈值。", "命中后：敌方火力汇聚到标为嘲讽后的护卫者。"],
    audit: { actor: "两名敌方攻击者与威胁持有者", target: "脆弱后排核心", affected: "目标锁定关系", path: "原始虚线与重定向实线", beforeAfter: "锁定后排转为锁定威胁持有者" },
  },
  "battlefield.shape.cone": {
    id: "battlefield.shape.cone",
    kind: "cone-coverage",
    ariaDescription: "左侧施法者朝右释放逐列扩张的扇形覆盖格，近端窄、远端宽，多名敌人位于不同覆盖层。",
    caption: "施法者朝向决定扇形轴线；覆盖从近端一格逐层扩张，位置越远，横向影响面越宽。",
    steps: ["施法者从当前格确定朝向。", "覆盖沿轴线逐层展开为扇面。", "扇面内多个目标同时受到伤害或控制。"],
    audit: { actor: "左侧施法者", target: "扇面中的多名敌人", affected: "逐层扩张的八个格子", path: "由近及远的扇形轴线", beforeAfter: "未覆盖转为多格同时命中" },
  },
  "battlefield.shape.line-pierce": {
    id: "battlefield.shape.line-pierce",
    kind: "line-pierce",
    ariaDescription: "左侧射手沿同一行发射带箭头的直线路径，依次穿过并命中三名对齐敌人。",
    caption: "一条确定弹道沿目标方向推进，按空间顺序命中同线单位；对齐越完整，一次攻击覆盖越多目标。",
    steps: ["射手选择一条合法直线。", "弹道按格位顺序穿过目标。", "三名对齐敌人依次出现命中标记。"],
    audit: { actor: "左侧穿透射手", target: "同一行的三名敌人", affected: "整条射击走廊", path: "贯穿六列的单一实线箭头", beforeAfter: "完整敌列转为连续三次命中" },
  },
  "battlefield.engagement.intercept": {
    id: "battlefield.engagement.intercept",
    kind: "guard-intercept",
    ariaDescription: "敌人沿虚线路径向左移动，进入带斜纹边界的守卫威胁格后在八角停止标记处停步，守卫转向该敌人。",
    caption: "移动者进入守卫相邻威胁格时，截击消耗一次资源，把剩余移动路径截断并建立临时接敌。",
    steps: ["敌人沿预定路径逼近后排。", "路径首次进入守卫威胁格。", "停止标记截断后续路径，守卫接住突破者。"],
    audit: { actor: "移动中的敌人与守卫", target: "后方核心", affected: "守卫相邻威胁格", path: "被停止标记截断的虚线路径", beforeAfter: "继续突破转为在威胁格停步" },
  },
  "battlefield.engagement.charge": {
    id: "battlefield.engagement.charge",
    kind: "charge-displacement",
    ariaDescription: "命中前面板中，冲锋者沿直线到首次接敌；命中后面板显示目标沿冲锋方向右移一格，并在阻挡或边缘分支中明确目标不位移、改为碰撞或眩晕。",
    caption: "冲锋者直线前进到首次接敌。正常结果只把该目标沿冲锋方向击退一格；目的格被占或位于棋盘外时不位移，以碰撞伤害或眩晕提示结算。",
    steps: ["命中前：直线路径在首次接敌处停止。", "命中后：前线目标沿冲锋方向右移一格，不发生侧抛或连锁推移。", "阻挡 / 边缘：目标不位移，只显示碰撞 / 眩晕结果，不把单位移出棋盘。"],
    audit: { actor: "左侧冲锋单位", target: "前线敌人", affected: "冲锋走廊与目标相邻格", path: "带三段动量刻度的长箭头", beforeAfter: "目标原位轮廓转为右移一格" },
  },
  "combat.defense.shield": {
    id: "combat.defense.shield",
    kind: "shield-break",
    ariaDescription: "敌方伤害箭头击中带钢色外环的友军，外环分裂并出现破盾星形标记，内部绿色生命条保持完整。",
    caption: "伤害先接触独立护盾层；护盾归零触发破裂事件，而生命层仍以完整形状保留在内侧。",
    steps: ["钢色外环表示可消耗护盾层。", "伤害箭头先削减外环而非生命。", "破盾星形出现，内侧生命条仍未受损。"],
    audit: { actor: "右侧敌方攻击者", target: "带护盾友军", affected: "护盾层与生命层", path: "从敌人到护盾的伤害箭头", beforeAfter: "完整护盾转为破裂，生命保持完整" },
  },
  "combat.summon.corpse": {
    id: "combat.summon.corpse",
    kind: "corpse-conversion",
    ariaDescription: "左侧倒下单位留下带归属环的骨形尸体标记，尸体沿转换箭头被划除，右侧出现一名新的召唤单位。",
    caption: "一次终结只生成一个带归属的尸体；消费标记使其不可再次使用，并在另一格创建一个召唤物。",
    steps: ["败亡虚影表明尸体来源。", "归属环中的骨形标记代表唯一尸体资源。", "消费斜线关闭尸体，右侧召唤单位成形。"],
    audit: { actor: "败亡单位与召唤者", target: "带归属的尸体标记", affected: "尸体格与召唤落点", path: "尸体到召唤物的转换箭头", beforeAfter: "一个尸体转为一个召唤物且不可重复消费" },
  },
  "combat.trigger.overheal": {
    id: "combat.trigger.overheal",
    kind: "overheal-charge",
    ariaDescription: "治疗者把目标生命条填满，溢出的十字脉冲沿弯折路径进入四格有上限的金色充能槽，充满后点亮效果环。",
    caption: "治疗先填补真实生命缺口；只有超过上限的授权部分进入有明确容量的充能槽，满槽再驱动一次效果。",
    steps: ["治疗脉冲先把绿色生命条补满。", "溢出部分改道进入四段容量槽。", "容量达到上限后，右侧效果环被点亮。"],
    audit: { actor: "治疗者", target: "满血友军", affected: "生命条、四段充能槽与效果环", path: "治疗到生命再到充能的弯折路径", beforeAfter: "治疗缺口转为满血与有界充能" },
  },
  "army.role.aoe-casters": {
    id: "army.role.aoe-casters",
    kind: "aoe-counter",
    ariaDescription: "命中前面板显示一个清楚边界内的密集低生命敌群；命中后面板保留同一边界，范围内多个低生命目标同时变为叉形败亡标记，并直标范围内同时结算。",
    caption: "前后态保留同一个范围边界：压力来自同一区域内快速增长的低生命敌群；一次范围结算覆盖边界内多具低生命单位，把数量优势转成受击密度。",
    steps: ["命中前：密集低生命敌群集中在同一范围边界内。", "范围模板锁定边界内的多个低生命目标。", "命中后：边界内多个低生命目标同时受击并败亡。"],
    audit: { actor: "左侧范围施法者", target: "密集低生命敌群", affected: "圆形模板覆盖的九格区域", path: "低生命敌群进入模板的压力箭头", beforeAfter: "完整低生命敌群转为多具叉形败亡标记" },
    counter: { pressure: "密集低生命敌群压力", answer: "范围清场答案", targetId: "encounter.template.swarm", linkLabel: "查看敌群压力机制" },
  },
  "combat.damage.armor-break": {
    id: "combat.damage.armor-break",
    kind: "armor-break-counter",
    ariaDescription: "重甲敌人外侧有四块钢色护甲片，三次小型命中依次击碎护甲片，最后一支粗箭头穿过缺口完成重击。",
    caption: "压力来自能抵消多段小伤害的分段护甲；持续命中先拆除钢色甲片，粗重击再利用暴露缺口收尾。",
    steps: ["四块钢色甲片显示重甲压力仍完整。", "连续命中逐段移除护甲并留下裂口。", "粗重击箭头穿过裂口，完成消费层数的终结。"],
    audit: { actor: "持续攻击者与重击者", target: "分段重甲敌人", affected: "四块护甲片与内层目标", path: "三次小命中后接一支粗重击箭头", beforeAfter: "完整护甲转为裂口暴露并被重击" },
    counter: { pressure: "分段护甲压力", answer: "破甲叠层答案", targetId: "combat.defense.armor", linkLabel: "查看分段护甲机制" },
  },
  "encounter.template.backline-hunter": {
    id: "encounter.template.backline-hunter",
    kind: "backline-counter",
    ariaDescription: "敌方猎手的红色虚线路径绕向后排核心，带盾形标记的守卫与菱形诱饵在中途建立分叉箭头，把猎手重定向到安全落点。",
    caption: "猎手压力绕过普通前线直指后排；护卫威胁或诱饵答案在路径中插入分叉，改变落点与接敌对象。",
    steps: ["红色虚线显示猎手原本绕向后排核心。", "守卫威胁格与诱饵菱形构成可读答案。", "实线分叉把猎手引向可承受的接敌点。"],
    audit: { actor: "敌方后排猎手与护卫/诱饵", target: "后排核心", affected: "绕后路径、威胁格与替代落点", path: "原始红色虚线与答案分叉实线", beforeAfter: "直达核心转为被护卫或诱饵截住" },
    counter: { pressure: "绕后猎杀压力", answer: "护卫 / 诱饵答案", targetId: "battlefield.formation.guard-pocket", linkLabel: "查看护卫口袋机制" },
  },
  "encounter.boss.timeline": {
    id: "encounter.boss.timeline",
    kind: "boss-timeline",
    ariaDescription: "横向时间线从三角预告开始，经过斜纹响应窗口与金色菱形指令时点，最后连接到Boss技能爆发的八角结算标记。",
    caption: "主要技能不是突然发生：预告先出现，开放有限响应窗口，玩家在金色节点投入指令，随后 Boss 按确定时点结算。",
    steps: ["三角节点公开下一次主要威胁。", "斜纹区间表示可以部署答案或使用指令的窗口。", "金色指令节点落在结算前，八角节点显示最终结果。"],
    audit: { actor: "Boss 与玩家指令", target: "即将受技能影响的军团", affected: "预告、响应窗口、指令点和结算点", path: "从左至右的短时间轴", beforeAfter: "未知威胁转为预告后应对并确定结算" },
  },
};

export function getMechanicDiagram(id: string): MechanicDiagramDefinition | null {
  return mechanicDiagramDefinitions[id as MechanicDiagramId] ?? null;
}
