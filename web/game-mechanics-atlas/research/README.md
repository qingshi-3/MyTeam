# 自走棋与相邻机制研究路由

本目录是探索证据，不是正式玩法或系统权威。正式构筑语法仍位于 `gameplay-design/combat-build-framework.md`，核心玩家规则仍位于 `gameplay-design/tower-autobattler-core.md`。

## 两层材料

- `discovery/`：2026-09-02 的 65 游戏商店页宽搜。它只用于确认候选、发行状态和公开特性承诺；其中的机制记录与综合均是**待验证假设**，不得单独支撑后续设计结论。
- `deep/`：逐游戏检索规则、攻略、实战分析、版本和社区材料后形成的深证据。只有满足 `deep/schema.md` 证据包门槛的游戏和记录才进入这里。

## 不可降级的证据规则

1. 已发行游戏至少需要一条实质规则/机制来源和一条实质策略/构筑/实战来源，且二者不能只是重复同一文本。
2. 商店页、搜索摘要、榜单句子、镜像和自动生成数据库不满足深证据门槛。
3. 平衡、统治率、失败、不可读、重做、移除、玩家行为和因果判断需要第三种恰当来源；无法确认就保留为问题。
4. 社区材料只能证明被明确标注的观察或意见，不能替代官方规则。
5. 找不到证据的候选必须留下 exclusion dossier，记录检索路径和淘汰原因，不能静默消失。

## 更新顺序

1. 在 `deep/candidate-roster.json` 锁定候选和唯一 dossier id。
2. 在 `deep/source-index.md` 登记实际打开的非商店来源及其支持的 claim id。
3. 完成 `deep/game-dossiers/<id>.md`，先写来源和版本，再写构筑与反制。
4. 只有 dossier 达到门槛后，才向 `deep/mechanic-evidence.json` 添加记录。
5. 每批更新 `deep/coverage-report.md` 和活动任务的精确恢复点。
6. 最后才更新 `deep/synthesis.md`；综合结论不得引用 discovery-only 记录。

访问障碍、版本漂移和资料矛盾必须如实记录。不得绕过登录、付费墙、CAPTCHA、robots 或已删除内容。
