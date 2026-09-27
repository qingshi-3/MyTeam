# MX49 与扩展53候选供给抽样

> 状态：Agent 候选设计测试；不预测现行 17 人池，不代表确认、实现、发布或平衡结论。来源：[原 trait matrix 计划](../trait-matrix/plan.json)。完整数据见 [supply-results.json](supply-results.json)，可用 [supply-model.mjs](supply-model.mjs)复现。

## 失败与对照假设

- `original49` 原样保留完整 MX49。原池只有 5 个 T4，且棘毒、构装没有 T4；严格 T4 发现会出现不足 3 个合法候选，也会天然偏向已有 T4 的体系。这个失败不通过补写原结果掩盖。
- `expanded53` 仅为供给覆盖测试加入四个 T4 身份：GX01 棘毒术士、GX02 棘毒统御者、GX03 构装射手、GX04 构装护卫。技能完全未模拟，未加入原 plan、content-map 或运行资源；它不承诺发布四张卡，也不证明战斗平衡。
- 两池均按相同开局、普通招募、发现顺序、阶位权重、策略和固定 seed 各运行 70,000 次。发现合法候选不足时缩短候选，绝不伪造第三项。A/B 或池差异改变已拥有身份后，后续 offer 不保证相同。
- 中档取原 plan 各体系第二门槛，深档取最后门槛。所有比率只属于当前算法与测试池。

## 5／10／15 战前简表

| 池 | 体系 | 策略 | 属性源率 5／10／15 | 中档率 5／10／15 | 深档率@15 | 无源 runs@15 | 发现不足事件率 | 发现不足 run率 |
|---|---|---|---|---|---:|---:|---:|---:|
| original49 | 霜羽 | A | 53.1%／65.6%／71.1% | 5.5%／44.3%／73.2% | 11.6% | 1443 | 6.5% | 19.6% |
| original49 | 霜羽 | B | 62.5%／75.8%／80.7% | 3.9%／40.2%／70.2% | 9.1% | 964 | 6.5% | 19.6% |
| original49 | 棘毒 | A | 56.0%／68.1%／73.6% | 10.3%／33.2%／54.3% | 4.9% | 1321 | 5.9% | 17.8% |
| original49 | 棘毒 | B | 61.1%／74.4%／79.4% | 9.0%／30.3%／51.0% | 4.4% | 1030 | 5.9% | 17.8% |
| original49 | 灰烬 | A | 53.3%／65.9%／72.2% | 23.4%／67.9%／87.3% | 23.2% | 1391 | 7.6% | 22.9% |
| original49 | 灰烬 | B | 61.8%／74.9%／79.8% | 20.2%／64.6%／85.5% | 19.7% | 1009 | 7.5% | 22.6% |
| original49 | 亡契 | A | 54.9%／66.3%／71.4% | 4.0%／46.4%／86.3% | 21.3% | 1432 | 8.6% | 25.7% |
| original49 | 亡契 | B | 62.9%／75.5%／80.4% | 2.6%／41.8%／84.1% | 17.6% | 981 | 8.3% | 25.0% |
| original49 | 构装 | A | 57.3%／68.9%／73.7% | 7.4%／31.4%／56.6% | 5.5% | 1313 | 5.7% | 17.0% |
| original49 | 构装 | B | 61.9%／74.6%／79.5% | 6.5%／29.2%／53.9% | 4.8% | 1026 | 5.7% | 17.0% |
| original49 | 血誓 | A | 57.3%／69.7%／75.3% | 23.4%／66.5%／86.9% | 23.8% | 1236 | 7.7% | 23.1% |
| original49 | 血誓 | B | 61.4%／74.0%／79.1% | 21.8%／64.6%／85.9% | 22.1% | 1045 | 7.7% | 23.1% |
| original49 | 星辉 | A | 53.1%／64.8%／69.6% | 23.4%／79.4%／98.0% | 55.3% | 1519 | 8.3% | 25.0% |
| original49 | 星辉 | B | 62.0%／74.5%／79.4% | 19.6%／76.7%／97.7% | 50.6% | 1028 | 8.1% | 24.4% |
| expanded53 | 霜羽 | A | 53.1%／65.6%／70.9% | 5.5%／34.9%／64.4% | 9.0% | 1457 | 0.0% | 0.0% |
| expanded53 | 霜羽 | B | 62.5%／75.8%／80.5% | 3.9%／30.4%／60.9% | 7.0% | 974 | 0.0% | 0.0% |
| expanded53 | 棘毒 | A | 56.0%／67.7%／72.8% | 10.3%／56.2%／88.2% | 32.8% | 1359 | 0.0% | 0.0% |
| expanded53 | 棘毒 | B | 61.1%／74.4%／79.2% | 9.0%／53.3%／86.7% | 30.3% | 1038 | 0.0% | 0.0% |
| expanded53 | 灰烬 | A | 53.3%／66.0%／72.0% | 23.4%／58.3%／80.0% | 18.6% | 1398 | 0.0% | 0.0% |
| expanded53 | 灰烬 | B | 61.8%／74.9%／79.7% | 20.2%／54.5%／77.7% | 15.6% | 1014 | 0.0% | 0.0% |
| expanded53 | 亡契 | A | 54.9%／66.9%／71.7% | 4.0%／34.7%／70.5% | 12.2% | 1415 | 0.0% | 0.0% |
| expanded53 | 亡契 | B | 62.9%／75.5%／80.1% | 2.6%／30.6%／67.3% | 9.8% | 994 | 0.0% | 0.0% |
| expanded53 | 构装 | A | 57.3%／68.6%／72.9% | 7.4%／56.4%／88.6% | 34.6% | 1353 | 0.0% | 0.0% |
| expanded53 | 构装 | B | 61.9%／74.6%／79.3% | 6.5%／53.7%／87.3% | 32.4% | 1034 | 0.0% | 0.0% |
| expanded53 | 血誓 | A | 57.3%／69.8%／75.1% | 23.4%／56.3%／79.4% | 18.9% | 1246 | 0.0% | 0.0% |
| expanded53 | 血誓 | B | 61.4%／74.0%／78.9% | 21.8%／54.5%／78.1% | 17.4% | 1055 | 0.0% | 0.0% |
| expanded53 | 星辉 | A | 53.1%／65.3%／70.3% | 23.4%／67.8%／92.0% | 37.6% | 1484 | 0.0% | 0.0% |
| expanded53 | 星辉 | B | 62.0%／74.6%／79.2% | 19.6%／64.2%／90.7% | 33.6% | 1038 | 0.0% | 0.0% |

## 原池与扩展池对照

| 体系 | 策略 | 中档@15 原→扩 | 深档@15 原→扩 | 属性源@15 原→扩 | 发现不足事件 原→扩 | 发现不足runs 原→扩 |
|---|---|---|---|---|---|---|
| 霜羽 | A | 73.2%→64.4% | 11.6%→9.0% | 71.1%→70.9% | 6.5%→0.0% | 19.6%→0.0% |
| 霜羽 | B | 70.2%→60.9% | 9.1%→7.0% | 80.7%→80.5% | 6.5%→0.0% | 19.6%→0.0% |
| 棘毒 | A | 54.3%→88.2% | 4.9%→32.8% | 73.6%→72.8% | 5.9%→0.0% | 17.8%→0.0% |
| 棘毒 | B | 51.0%→86.7% | 4.4%→30.3% | 79.4%→79.2% | 5.9%→0.0% | 17.8%→0.0% |
| 灰烬 | A | 87.3%→80.0% | 23.2%→18.6% | 72.2%→72.0% | 7.6%→0.0% | 22.9%→0.0% |
| 灰烬 | B | 85.5%→77.7% | 19.7%→15.6% | 79.8%→79.7% | 7.5%→0.0% | 22.6%→0.0% |
| 亡契 | A | 86.3%→70.5% | 21.3%→12.2% | 71.4%→71.7% | 8.6%→0.0% | 25.7%→0.0% |
| 亡契 | B | 84.1%→67.3% | 17.6%→9.8% | 80.4%→80.1% | 8.3%→0.0% | 25.0%→0.0% |
| 构装 | A | 56.6%→88.6% | 5.5%→34.6% | 73.7%→72.9% | 5.7%→0.0% | 17.0%→0.0% |
| 构装 | B | 53.9%→87.3% | 4.8%→32.4% | 79.5%→79.3% | 5.7%→0.0% | 17.0%→0.0% |
| 血誓 | A | 86.9%→79.4% | 23.8%→18.9% | 75.3%→75.1% | 7.7%→0.0% | 23.1%→0.0% |
| 血誓 | B | 85.9%→78.1% | 22.1%→17.4% | 79.1%→78.9% | 7.7%→0.0% | 23.1%→0.0% |
| 星辉 | A | 98.0%→92.0% | 55.3%→37.6% | 69.6%→70.3% | 8.3%→0.0% | 25.0%→0.0% |
| 星辉 | B | 97.7%→90.7% | 50.6%→33.6% | 79.4%→79.2% | 8.1%→0.0% | 24.4%→0.0% |

原池发现不足事件率为 5.7%–8.6%，受影响 run 率为 17.0%–25.7%；全部发生在节点 12 的第二次 T4 发现。扩展池本轮的不足事件和受影响 runs 都是 0，因此这四个占位身份在本抽样规则下修复了“三选不足”，但这不证明占位角色的技能、平衡或发布合理。


## 完整示例轨迹

每个池、体系和策略各保留一条 run 0 轨迹；节点 4／12 的普通招募先于发现。

### original49 · 霜羽 · A（seed 2147850067）

- opening@0（FloorIndex 0 weighted tiers）：MX26、MX20、MX07、MX27、MX49、MX03 → MX03、MX27
- recruit@2（FloorIndex 1 weighted tiers）：MX48、MX19、MX30 → MX30
- recruit@4（FloorIndex 3 weighted tiers）：MX42、MX31、MX49 → MX42
- discovery@4（T3 only）：MX43、MX36、MX29 → MX36
- recruit@7（FloorIndex 6 weighted tiers）：MX40、MX18、MX04 → MX04
- discovery@8（T4 only）：MX38、MX44、MX45 → MX44
- recruit@9（FloorIndex 8 weighted tiers）：MX29、MX14、MX08 → MX08
- recruit@12（FloorIndex 11 weighted tiers）：MX43、MX37、MX49 → MX37
- discovery@12（T4 only）：MX41、MX45、MX38 → MX38
- recruit@14（FloorIndex 13 weighted tiers）：MX34、MX45、MX40 → MX45

终局名册：MX03@0、MX27@0、MX30@2、MX42@4、MX36@4、MX04@7、MX44@8、MX08@9、MX37@12、MX38@12、MX45@14

### original49 · 霜羽 · B（seed 2147850067）

- opening@0（FloorIndex 0 weighted tiers）：MX26、MX20、MX07、MX27、MX49、MX03 → MX03、MX27
- recruit@2（FloorIndex 1 weighted tiers）：MX48、MX19、MX30 → MX30
- recruit@4（FloorIndex 3 weighted tiers）：MX42、MX31、MX49 → MX42
- discovery@4（T3 only）：MX43、MX36、MX29 → MX36
- recruit@7（FloorIndex 6 weighted tiers）：MX40、MX18、MX04 → MX04
- discovery@8（T4 only）：MX38、MX44、MX45 → MX44
- recruit@9（FloorIndex 8 weighted tiers）：MX29、MX14、MX08 → MX08
- recruit@12（FloorIndex 11 weighted tiers）：MX43、MX37、MX49 → MX37
- discovery@12（T4 only）：MX41、MX45、MX38 → MX38
- recruit@14（FloorIndex 13 weighted tiers）：MX34、MX45、MX40 → MX45

终局名册：MX03@0、MX27@0、MX30@2、MX42@4、MX36@4、MX04@7、MX44@8、MX08@9、MX37@12、MX38@12、MX45@14

### original49 · 棘毒 · A（seed 3095923618）

- opening@0（FloorIndex 0 weighted tiers）：MX22、MX16、MX31、MX15、MX26、MX27 → MX22、MX27
- recruit@2（FloorIndex 1 weighted tiers）：MX16、MX01、MX28 → MX01
- recruit@4（FloorIndex 3 weighted tiers）：MX02、MX49、MX09 → MX09
- discovery@4（T3 only）：MX18、MX39、MX43 → MX18
- recruit@7（FloorIndex 6 weighted tiers）：MX43、MX34、MX42 → MX42
- discovery@8（T4 only）：MX45、MX44、MX41 → MX41
- recruit@9（FloorIndex 8 weighted tiers）：MX30、MX16、MX36 → MX36
- recruit@12（FloorIndex 11 weighted tiers）：MX02、MX47、MX45 → MX45
- discovery@12（T4 only）：MX44、MX47、MX38 → MX38
- recruit@14（FloorIndex 13 weighted tiers）：MX44、MX29、MX47 → MX44

终局名册：MX22@0、MX27@0、MX01@2、MX09@4、MX18@4、MX42@7、MX41@8、MX36@9、MX45@12、MX38@12、MX44@14

### original49 · 棘毒 · B（seed 3095923618）

- opening@0（FloorIndex 0 weighted tiers）：MX22、MX16、MX31、MX15、MX26、MX27 → MX22、MX27
- recruit@2（FloorIndex 1 weighted tiers）：MX16、MX01、MX28 → MX01
- recruit@4（FloorIndex 3 weighted tiers）：MX02、MX49、MX09 → MX09
- discovery@4（T3 only）：MX18、MX39、MX43 → MX18
- recruit@7（FloorIndex 6 weighted tiers）：MX43、MX34、MX42 → MX42
- discovery@8（T4 only）：MX45、MX44、MX41 → MX41
- recruit@9（FloorIndex 8 weighted tiers）：MX30、MX16、MX36 → MX30
- recruit@12（FloorIndex 11 weighted tiers）：MX02、MX47、MX45 → MX45
- discovery@12（T4 only）：MX44、MX47、MX38 → MX38
- recruit@14（FloorIndex 13 weighted tiers）：MX44、MX29、MX47 → MX44

终局名册：MX22@0、MX27@0、MX01@2、MX09@4、MX18@4、MX42@7、MX41@8、MX30@9、MX45@12、MX38@12、MX44@14

### original49 · 灰烬 · A（seed 3975270158）

- opening@0（FloorIndex 0 weighted tiers）：MX19、MX16、MX21、MX07、MX33、MX32 → MX19、MX32
- recruit@2（FloorIndex 1 weighted tiers）：MX30、MX33、MX23 → MX30
- recruit@4（FloorIndex 3 weighted tiers）：MX10、MX13、MX04 → MX13
- discovery@4（T3 only）：MX42、MX29、MX35 → MX29
- recruit@7（FloorIndex 6 weighted tiers）：MX25、MX08、MX39 → MX25
- discovery@8（T4 only）：MX44、MX47、MX45 → MX44
- recruit@9（FloorIndex 8 weighted tiers）：MX12、MX09、MX18 → MX12
- recruit@12（FloorIndex 11 weighted tiers）：MX24、MX39、MX42 → MX24
- discovery@12（T4 only）：MX47、MX41、MX45 → MX41
- recruit@14（FloorIndex 13 weighted tiers）：MX09、MX47、MX38 → MX38

终局名册：MX19@0、MX32@0、MX30@2、MX13@4、MX29@4、MX25@7、MX44@8、MX12@9、MX24@12、MX41@12、MX38@14

### original49 · 灰烬 · B（seed 3975270158）

- opening@0（FloorIndex 0 weighted tiers）：MX19、MX16、MX21、MX07、MX33、MX32 → MX19、MX32
- recruit@2（FloorIndex 1 weighted tiers）：MX30、MX33、MX23 → MX30
- recruit@4（FloorIndex 3 weighted tiers）：MX10、MX13、MX04 → MX13
- discovery@4（T3 only）：MX42、MX29、MX35 → MX29
- recruit@7（FloorIndex 6 weighted tiers）：MX25、MX08、MX39 → MX25
- discovery@8（T4 only）：MX44、MX47、MX45 → MX44
- recruit@9（FloorIndex 8 weighted tiers）：MX12、MX09、MX18 → MX12
- recruit@12（FloorIndex 11 weighted tiers）：MX24、MX39、MX42 → MX24
- discovery@12（T4 only）：MX47、MX41、MX45 → MX41
- recruit@14（FloorIndex 13 weighted tiers）：MX09、MX47、MX38 → MX38

终局名册：MX19@0、MX32@0、MX30@2、MX13@4、MX29@4、MX25@7、MX44@8、MX12@9、MX24@12、MX41@12、MX38@14

### original49 · 亡契 · A（seed 368585221）

- opening@0（FloorIndex 0 weighted tiers）：MX16、MX21、MX09、MX31、MX02、MX07 → MX16、MX02
- recruit@2（FloorIndex 1 weighted tiers）：MX19、MX46、MX13 → MX19
- recruit@4（FloorIndex 3 weighted tiers）：MX21、MX27、MX11 → MX27
- discovery@4（T3 only）：MX43、MX42、MX24 → MX24
- recruit@7（FloorIndex 6 weighted tiers）：MX18、MX46、MX10 → MX18
- discovery@8（T4 only）：MX44、MX45、MX38 → MX38
- recruit@9（FloorIndex 8 weighted tiers）：MX49、MX30、MX35 → MX30
- recruit@12（FloorIndex 11 weighted tiers）：MX10、MX13、MX42 → MX10
- discovery@12（T4 only）：MX44、MX41、MX47 → MX41
- recruit@14（FloorIndex 13 weighted tiers）：MX47、MX04、MX45 → MX45

终局名册：MX16@0、MX02@0、MX19@2、MX27@4、MX24@4、MX18@7、MX38@8、MX30@9、MX10@12、MX41@12、MX45@14

### original49 · 亡契 · B（seed 368585221）

- opening@0（FloorIndex 0 weighted tiers）：MX16、MX21、MX09、MX31、MX02、MX07 → MX16、MX02
- recruit@2（FloorIndex 1 weighted tiers）：MX19、MX46、MX13 → MX19
- recruit@4（FloorIndex 3 weighted tiers）：MX21、MX27、MX11 → MX27
- discovery@4（T3 only）：MX43、MX42、MX24 → MX24
- recruit@7（FloorIndex 6 weighted tiers）：MX18、MX46、MX10 → MX10
- discovery@8（T4 only）：MX44、MX45、MX38 → MX38
- recruit@9（FloorIndex 8 weighted tiers）：MX49、MX30、MX35 → MX30
- recruit@12（FloorIndex 11 weighted tiers）：MX12、MX17、MX42 → MX17
- discovery@12（T4 only）：MX44、MX41、MX47 → MX41
- recruit@14（FloorIndex 13 weighted tiers）：MX47、MX04、MX45 → MX45

终局名册：MX16@0、MX02@0、MX19@2、MX27@4、MX24@4、MX10@7、MX38@8、MX30@9、MX17@12、MX41@12、MX45@14

### original49 · 构装 · A（seed 1110356618）

- opening@0（FloorIndex 0 weighted tiers）：MX15、MX16、MX23、MX03、MX17、MX33 → MX23、MX17
- recruit@2（FloorIndex 1 weighted tiers）：MX15、MX03、MX33 → MX03
- recruit@4（FloorIndex 3 weighted tiers）：MX16、MX36、MX24 → MX24
- discovery@4（T3 only）：MX43、MX39、MX04 → MX39
- recruit@7（FloorIndex 6 weighted tiers）：MX42、MX04、MX14 → MX04
- discovery@8（T4 only）：MX47、MX44、MX38 → MX38
- recruit@9（FloorIndex 8 weighted tiers）：MX35、MX42、MX07 → MX35
- recruit@12（FloorIndex 11 weighted tiers）：MX36、MX18、MX29 → MX18
- discovery@12（T4 only）：MX45、MX44、MX47 → MX44
- recruit@14（FloorIndex 13 weighted tiers）：MX45、MX40、MX42 → MX40

终局名册：MX23@0、MX17@0、MX03@2、MX24@4、MX39@4、MX04@7、MX38@8、MX35@9、MX18@12、MX44@12、MX40@14

### original49 · 构装 · B（seed 1110356618）

- opening@0（FloorIndex 0 weighted tiers）：MX15、MX16、MX23、MX03、MX17、MX33 → MX23、MX17
- recruit@2（FloorIndex 1 weighted tiers）：MX15、MX03、MX33 → MX03
- recruit@4（FloorIndex 3 weighted tiers）：MX16、MX36、MX24 → MX24
- discovery@4（T3 only）：MX43、MX39、MX04 → MX39
- recruit@7（FloorIndex 6 weighted tiers）：MX42、MX04、MX14 → MX04
- discovery@8（T4 only）：MX47、MX44、MX38 → MX38
- recruit@9（FloorIndex 8 weighted tiers）：MX35、MX42、MX07 → MX35
- recruit@12（FloorIndex 11 weighted tiers）：MX36、MX18、MX29 → MX18
- discovery@12（T4 only）：MX45、MX44、MX47 → MX44
- recruit@14（FloorIndex 13 weighted tiers）：MX45、MX40、MX42 → MX40

终局名册：MX23@0、MX17@0、MX03@2、MX24@4、MX39@4、MX04@7、MX38@8、MX35@9、MX18@12、MX44@12、MX40@14

### original49 · 血誓 · A（seed 3767747687）

- opening@0（FloorIndex 0 weighted tiers）：MX11、MX17、MX25、MX26、MX16、MX48 → MX26、MX25
- recruit@2（FloorIndex 1 weighted tiers）：MX15、MX13、MX05 → MX05
- recruit@4（FloorIndex 3 weighted tiers）：MX03、MX22、MX12 → MX12
- discovery@4（T3 only）：MX40、MX18、MX37 → MX40
- recruit@7（FloorIndex 6 weighted tiers）：MX36、MX38、MX39 → MX38
- discovery@8（T4 only）：MX47、MX45、MX44 → MX44
- recruit@9（FloorIndex 8 weighted tiers）：MX14、MX49、MX19 → MX14
- recruit@12（FloorIndex 11 weighted tiers）：MX47、MX42、MX13 → MX47
- discovery@12（T4 only）：MX45、MX41 → MX41
- recruit@14（FloorIndex 13 weighted tiers）：MX45、MX42、MX17 → MX45

终局名册：MX26@0、MX25@0、MX05@2、MX12@4、MX40@4、MX38@7、MX44@8、MX14@9、MX47@12、MX41@12、MX45@14

### original49 · 血誓 · B（seed 3767747687）

- opening@0（FloorIndex 0 weighted tiers）：MX11、MX17、MX25、MX26、MX16、MX48 → MX25、MX26
- recruit@2（FloorIndex 1 weighted tiers）：MX15、MX13、MX05 → MX05
- recruit@4（FloorIndex 3 weighted tiers）：MX03、MX22、MX12 → MX12
- discovery@4（T3 only）：MX40、MX18、MX37 → MX40
- recruit@7（FloorIndex 6 weighted tiers）：MX36、MX38、MX39 → MX38
- discovery@8（T4 only）：MX47、MX45、MX44 → MX44
- recruit@9（FloorIndex 8 weighted tiers）：MX14、MX49、MX19 → MX14
- recruit@12（FloorIndex 11 weighted tiers）：MX47、MX42、MX13 → MX47
- discovery@12（T4 only）：MX45、MX41 → MX41
- recruit@14（FloorIndex 13 weighted tiers）：MX45、MX42、MX17 → MX45

终局名册：MX25@0、MX26@0、MX05@2、MX12@4、MX40@4、MX38@7、MX44@8、MX14@9、MX47@12、MX41@12、MX45@14

### original49 · 星辉 · A（seed 3795372346）

- opening@0（FloorIndex 0 weighted tiers）：MX07、MX28、MX11、MX33、MX15、MX31 → MX31、MX33
- recruit@2（FloorIndex 1 weighted tiers）：MX21、MX25、MX13 → MX25
- recruit@4（FloorIndex 3 weighted tiers）：MX03、MX18、MX20 → MX18
- discovery@4（T3 only）：MX39、MX35、MX14 → MX35
- recruit@7（FloorIndex 6 weighted tiers）：MX17、MX45、MX06 → MX45
- discovery@8（T4 only）：MX44、MX47、MX38 → MX47
- recruit@9（FloorIndex 8 weighted tiers）：MX11、MX24、MX26 → MX24
- recruit@12（FloorIndex 11 weighted tiers）：MX36、MX44、MX38 → MX38
- discovery@12（T4 only）：MX44、MX41 → MX41
- recruit@14（FloorIndex 13 weighted tiers）：MX43、MX10、MX29 → MX10

终局名册：MX31@0、MX33@0、MX25@2、MX18@4、MX35@4、MX45@7、MX47@8、MX24@9、MX38@12、MX41@12、MX10@14

### original49 · 星辉 · B（seed 3795372346）

- opening@0（FloorIndex 0 weighted tiers）：MX07、MX28、MX11、MX33、MX15、MX31 → MX31、MX33
- recruit@2（FloorIndex 1 weighted tiers）：MX21、MX25、MX13 → MX25
- recruit@4（FloorIndex 3 weighted tiers）：MX03、MX18、MX20 → MX18
- discovery@4（T3 only）：MX39、MX35、MX14 → MX35
- recruit@7（FloorIndex 6 weighted tiers）：MX17、MX45、MX06 → MX45
- discovery@8（T4 only）：MX44、MX47、MX38 → MX47
- recruit@9（FloorIndex 8 weighted tiers）：MX11、MX24、MX26 → MX24
- recruit@12（FloorIndex 11 weighted tiers）：MX36、MX44、MX38 → MX38
- discovery@12（T4 only）：MX44、MX41 → MX41
- recruit@14（FloorIndex 13 weighted tiers）：MX43、MX10、MX29 → MX10

终局名册：MX31@0、MX33@0、MX25@2、MX18@4、MX35@4、MX45@7、MX47@8、MX24@9、MX38@12、MX41@12、MX10@14

### expanded53 · 霜羽 · A（seed 2147850067）

- opening@0（FloorIndex 0 weighted tiers）：MX26、MX20、MX07、MX27、MX49、MX03 → MX03、MX27
- recruit@2（FloorIndex 1 weighted tiers）：MX48、MX19、MX30 → MX30
- recruit@4（FloorIndex 3 weighted tiers）：MX42、MX31、MX49 → MX42
- discovery@4（T3 only）：MX43、MX36、MX29 → MX36
- recruit@7（FloorIndex 6 weighted tiers）：MX40、MX18、MX04 → MX04
- discovery@8（T4 only）：MX41、MX45、MX47 → MX41
- recruit@9（FloorIndex 8 weighted tiers）：MX29、MX14、MX08 → MX08
- recruit@12（FloorIndex 11 weighted tiers）：MX43、MX37、MX49 → MX37
- discovery@12（T4 only）：MX45、GX02、MX47 → GX02
- recruit@14（FloorIndex 13 weighted tiers）：MX34、MX47、MX40 → MX47

终局名册：MX03@0、MX27@0、MX30@2、MX42@4、MX36@4、MX04@7、MX41@8、MX08@9、MX37@12、GX02@12、MX47@14

### expanded53 · 霜羽 · B（seed 2147850067）

- opening@0（FloorIndex 0 weighted tiers）：MX26、MX20、MX07、MX27、MX49、MX03 → MX03、MX27
- recruit@2（FloorIndex 1 weighted tiers）：MX48、MX19、MX30 → MX30
- recruit@4（FloorIndex 3 weighted tiers）：MX42、MX31、MX49 → MX42
- discovery@4（T3 only）：MX43、MX36、MX29 → MX36
- recruit@7（FloorIndex 6 weighted tiers）：MX40、MX18、MX04 → MX04
- discovery@8（T4 only）：MX41、MX45、MX47 → MX41
- recruit@9（FloorIndex 8 weighted tiers）：MX29、MX14、MX08 → MX08
- recruit@12（FloorIndex 11 weighted tiers）：MX43、MX37、MX49 → MX37
- discovery@12（T4 only）：MX45、GX02、MX47 → GX02
- recruit@14（FloorIndex 13 weighted tiers）：MX34、MX47、MX40 → MX47

终局名册：MX03@0、MX27@0、MX30@2、MX42@4、MX36@4、MX04@7、MX41@8、MX08@9、MX37@12、GX02@12、MX47@14

### expanded53 · 棘毒 · A（seed 3095923618）

- opening@0（FloorIndex 0 weighted tiers）：MX22、MX16、MX31、MX15、MX26、MX27 → MX22、MX27
- recruit@2（FloorIndex 1 weighted tiers）：MX16、MX01、MX28 → MX01
- recruit@4（FloorIndex 3 weighted tiers）：MX02、MX49、MX09 → MX09
- discovery@4（T3 only）：MX18、MX39、MX43 → MX18
- recruit@7（FloorIndex 6 weighted tiers）：MX43、MX34、MX42 → MX42
- discovery@8（T4 only）：GX03、GX01、MX44 → GX01
- recruit@9（FloorIndex 8 weighted tiers）：MX30、MX16、MX36 → MX36
- recruit@12（FloorIndex 11 weighted tiers）：MX02、GX04、GX03 → GX03
- discovery@12（T4 only）：MX45、GX04、MX44 → GX04
- recruit@14（FloorIndex 13 weighted tiers）：MX38、MX29、MX41 → MX38

终局名册：MX22@0、MX27@0、MX01@2、MX09@4、MX18@4、MX42@7、GX01@8、MX36@9、GX03@12、GX04@12、MX38@14

### expanded53 · 棘毒 · B（seed 3095923618）

- opening@0（FloorIndex 0 weighted tiers）：MX22、MX16、MX31、MX15、MX26、MX27 → MX22、MX27
- recruit@2（FloorIndex 1 weighted tiers）：MX16、MX01、MX28 → MX01
- recruit@4（FloorIndex 3 weighted tiers）：MX02、MX49、MX09 → MX09
- discovery@4（T3 only）：MX18、MX39、MX43 → MX18
- recruit@7（FloorIndex 6 weighted tiers）：MX43、MX34、MX42 → MX42
- discovery@8（T4 only）：GX03、GX01、MX44 → GX01
- recruit@9（FloorIndex 8 weighted tiers）：MX30、MX16、MX36 → MX30
- recruit@12（FloorIndex 11 weighted tiers）：MX02、GX04、GX03 → GX03
- discovery@12（T4 only）：MX45、GX04、MX44 → GX04
- recruit@14（FloorIndex 13 weighted tiers）：MX38、MX29、MX41 → MX38

终局名册：MX22@0、MX27@0、MX01@2、MX09@4、MX18@4、MX42@7、GX01@8、MX30@9、GX03@12、GX04@12、MX38@14

### expanded53 · 灰烬 · A（seed 3975270158）

- opening@0（FloorIndex 0 weighted tiers）：MX19、MX16、MX21、MX07、MX33、MX32 → MX19、MX32
- recruit@2（FloorIndex 1 weighted tiers）：MX30、MX33、MX23 → MX30
- recruit@4（FloorIndex 3 weighted tiers）：MX10、MX13、MX04 → MX13
- discovery@4（T3 only）：MX42、MX29、MX35 → MX29
- recruit@7（FloorIndex 6 weighted tiers）：MX25、MX08、MX39 → MX25
- discovery@8（T4 only）：MX47、GX03、GX01 → GX01
- recruit@9（FloorIndex 8 weighted tiers）：MX12、MX09、MX18 → MX12
- recruit@12（FloorIndex 11 weighted tiers）：MX24、MX39、MX42 → MX24
- discovery@12（T4 only）：GX04、MX44、GX03 → GX03
- recruit@14（FloorIndex 13 weighted tiers）：MX09、GX02、MX38 → MX38

终局名册：MX19@0、MX32@0、MX30@2、MX13@4、MX29@4、MX25@7、GX01@8、MX12@9、MX24@12、GX03@12、MX38@14

### expanded53 · 灰烬 · B（seed 3975270158）

- opening@0（FloorIndex 0 weighted tiers）：MX19、MX16、MX21、MX07、MX33、MX32 → MX19、MX32
- recruit@2（FloorIndex 1 weighted tiers）：MX30、MX33、MX23 → MX30
- recruit@4（FloorIndex 3 weighted tiers）：MX10、MX13、MX04 → MX13
- discovery@4（T3 only）：MX42、MX29、MX35 → MX29
- recruit@7（FloorIndex 6 weighted tiers）：MX25、MX08、MX39 → MX25
- discovery@8（T4 only）：MX47、GX03、GX01 → GX01
- recruit@9（FloorIndex 8 weighted tiers）：MX12、MX09、MX18 → MX12
- recruit@12（FloorIndex 11 weighted tiers）：MX24、MX39、MX42 → MX24
- discovery@12（T4 only）：GX04、MX44、GX03 → GX03
- recruit@14（FloorIndex 13 weighted tiers）：MX09、GX02、MX38 → MX38

终局名册：MX19@0、MX32@0、MX30@2、MX13@4、MX29@4、MX25@7、GX01@8、MX12@9、MX24@12、GX03@12、MX38@14

### expanded53 · 亡契 · A（seed 368585221）

- opening@0（FloorIndex 0 weighted tiers）：MX16、MX21、MX09、MX31、MX02、MX07 → MX16、MX02
- recruit@2（FloorIndex 1 weighted tiers）：MX19、MX46、MX13 → MX19
- recruit@4（FloorIndex 3 weighted tiers）：MX21、MX27、MX11 → MX27
- discovery@4（T3 only）：MX43、MX42、MX24 → MX24
- recruit@7（FloorIndex 6 weighted tiers）：MX18、MX46、MX10 → MX18
- discovery@8（T4 only）：MX47、GX01、MX44 → GX01
- recruit@9（FloorIndex 8 weighted tiers）：MX49、MX30、MX35 → MX30
- recruit@12（FloorIndex 11 weighted tiers）：MX10、MX13、MX42 → MX10
- discovery@12（T4 only）：MX44、MX38、GX04 → MX38
- recruit@14（FloorIndex 13 weighted tiers）：GX04、MX04、GX03 → GX03

终局名册：MX16@0、MX02@0、MX19@2、MX27@4、MX24@4、MX18@7、GX01@8、MX30@9、MX10@12、MX38@12、GX03@14

### expanded53 · 亡契 · B（seed 368585221）

- opening@0（FloorIndex 0 weighted tiers）：MX16、MX21、MX09、MX31、MX02、MX07 → MX16、MX02
- recruit@2（FloorIndex 1 weighted tiers）：MX19、MX46、MX13 → MX19
- recruit@4（FloorIndex 3 weighted tiers）：MX21、MX27、MX11 → MX27
- discovery@4（T3 only）：MX43、MX42、MX24 → MX24
- recruit@7（FloorIndex 6 weighted tiers）：MX18、MX46、MX10 → MX10
- discovery@8（T4 only）：MX47、GX01、MX44 → GX01
- recruit@9（FloorIndex 8 weighted tiers）：MX49、MX30、MX35 → MX30
- recruit@12（FloorIndex 11 weighted tiers）：MX12、MX17、MX42 → MX17
- discovery@12（T4 only）：MX44、MX38、GX04 → MX38
- recruit@14（FloorIndex 13 weighted tiers）：GX04、MX04、GX03 → GX03

终局名册：MX16@0、MX02@0、MX19@2、MX27@4、MX24@4、MX10@7、GX01@8、MX30@9、MX17@12、MX38@12、GX03@14

### expanded53 · 构装 · A（seed 1110356618）

- opening@0（FloorIndex 0 weighted tiers）：MX15、MX16、MX23、MX03、MX17、MX33 → MX23、MX17
- recruit@2（FloorIndex 1 weighted tiers）：MX15、MX03、MX33 → MX03
- recruit@4（FloorIndex 3 weighted tiers）：MX16、MX36、MX24 → MX24
- discovery@4（T3 only）：MX43、MX39、MX04 → MX39
- recruit@7（FloorIndex 6 weighted tiers）：MX42、MX04、MX14 → MX04
- discovery@8（T4 only）：GX03、GX01、MX41 → GX03
- recruit@9（FloorIndex 8 weighted tiers）：MX35、MX42、MX07 → MX35
- recruit@12（FloorIndex 11 weighted tiers）：MX36、MX18、MX29 → MX18
- discovery@12（T4 only）：MX47、GX01、MX45 → GX01
- recruit@14（FloorIndex 13 weighted tiers）：MX45、MX40、MX42 → MX40

终局名册：MX23@0、MX17@0、MX03@2、MX24@4、MX39@4、MX04@7、GX03@8、MX35@9、MX18@12、GX01@12、MX40@14

### expanded53 · 构装 · B（seed 1110356618）

- opening@0（FloorIndex 0 weighted tiers）：MX15、MX16、MX23、MX03、MX17、MX33 → MX23、MX17
- recruit@2（FloorIndex 1 weighted tiers）：MX15、MX03、MX33 → MX03
- recruit@4（FloorIndex 3 weighted tiers）：MX16、MX36、MX24 → MX24
- discovery@4（T3 only）：MX43、MX39、MX04 → MX39
- recruit@7（FloorIndex 6 weighted tiers）：MX42、MX04、MX14 → MX04
- discovery@8（T4 only）：GX03、GX01、MX41 → GX03
- recruit@9（FloorIndex 8 weighted tiers）：MX35、MX42、MX07 → MX35
- recruit@12（FloorIndex 11 weighted tiers）：MX36、MX18、MX29 → MX18
- discovery@12（T4 only）：MX47、GX01、MX45 → GX01
- recruit@14（FloorIndex 13 weighted tiers）：MX45、MX40、MX42 → MX40

终局名册：MX23@0、MX17@0、MX03@2、MX24@4、MX39@4、MX04@7、GX03@8、MX35@9、MX18@12、GX01@12、MX40@14

### expanded53 · 血誓 · A（seed 3767747687）

- opening@0（FloorIndex 0 weighted tiers）：MX11、MX17、MX25、MX26、MX16、MX48 → MX26、MX25
- recruit@2（FloorIndex 1 weighted tiers）：MX15、MX13、MX05 → MX05
- recruit@4（FloorIndex 3 weighted tiers）：MX03、MX22、MX12 → MX12
- discovery@4（T3 only）：MX40、MX18、MX37 → MX40
- recruit@7（FloorIndex 6 weighted tiers）：MX36、MX41、MX39 → MX41
- discovery@8（T4 only）：GX04、GX01、GX03 → GX01
- recruit@9（FloorIndex 8 weighted tiers）：MX14、MX49、MX19 → MX14
- recruit@12（FloorIndex 11 weighted tiers）：GX02、MX42、MX13 → GX02
- discovery@12（T4 only）：MX47、GX03、GX04 → GX03
- recruit@14（FloorIndex 13 weighted tiers）：GX04、MX38、MX39 → GX04

终局名册：MX26@0、MX25@0、MX05@2、MX12@4、MX40@4、MX41@7、GX01@8、MX14@9、GX02@12、GX03@12、GX04@14

### expanded53 · 血誓 · B（seed 3767747687）

- opening@0（FloorIndex 0 weighted tiers）：MX11、MX17、MX25、MX26、MX16、MX48 → MX25、MX26
- recruit@2（FloorIndex 1 weighted tiers）：MX15、MX13、MX05 → MX05
- recruit@4（FloorIndex 3 weighted tiers）：MX03、MX22、MX12 → MX12
- discovery@4（T3 only）：MX40、MX18、MX37 → MX40
- recruit@7（FloorIndex 6 weighted tiers）：MX36、MX41、MX39 → MX41
- discovery@8（T4 only）：GX04、GX01、GX03 → GX01
- recruit@9（FloorIndex 8 weighted tiers）：MX14、MX49、MX19 → MX14
- recruit@12（FloorIndex 11 weighted tiers）：GX02、MX42、MX13 → GX02
- discovery@12（T4 only）：MX47、GX03、GX04 → GX03
- recruit@14（FloorIndex 13 weighted tiers）：GX04、MX38、MX39 → GX04

终局名册：MX25@0、MX26@0、MX05@2、MX12@4、MX40@4、MX41@7、GX01@8、MX14@9、GX02@12、GX03@12、GX04@14

### expanded53 · 星辉 · A（seed 3795372346）

- opening@0（FloorIndex 0 weighted tiers）：MX07、MX28、MX11、MX33、MX15、MX31 → MX31、MX33
- recruit@2（FloorIndex 1 weighted tiers）：MX21、MX25、MX13 → MX25
- recruit@4（FloorIndex 3 weighted tiers）：MX03、MX18、MX20 → MX18
- discovery@4（T3 only）：MX39、MX35、MX14 → MX35
- recruit@7（FloorIndex 6 weighted tiers）：MX17、GX01、MX06 → GX01
- discovery@8（T4 only）：GX02、GX03、MX38 → GX02
- recruit@9（FloorIndex 8 weighted tiers）：MX11、MX24、MX26 → MX24
- recruit@12（FloorIndex 11 weighted tiers）：MX36、GX03、MX38 → GX03
- discovery@12（T4 only）：MX45、GX04、MX41 → MX41
- recruit@14（FloorIndex 13 weighted tiers）：MX38、MX39、MX14 → MX38

终局名册：MX31@0、MX33@0、MX25@2、MX18@4、MX35@4、GX01@7、GX02@8、MX24@9、GX03@12、MX41@12、MX38@14

### expanded53 · 星辉 · B（seed 3795372346）

- opening@0（FloorIndex 0 weighted tiers）：MX07、MX28、MX11、MX33、MX15、MX31 → MX31、MX33
- recruit@2（FloorIndex 1 weighted tiers）：MX21、MX25、MX13 → MX25
- recruit@4（FloorIndex 3 weighted tiers）：MX03、MX18、MX20 → MX18
- discovery@4（T3 only）：MX39、MX35、MX14 → MX35
- recruit@7（FloorIndex 6 weighted tiers）：MX17、GX01、MX06 → GX01
- discovery@8（T4 only）：GX02、GX03、MX38 → GX02
- recruit@9（FloorIndex 8 weighted tiers）：MX11、MX24、MX26 → MX24
- recruit@12（FloorIndex 11 weighted tiers）：MX36、GX03、MX38 → GX03
- discovery@12（T4 only）：MX45、GX04、MX41 → MX41
- recruit@14（FloorIndex 13 weighted tiers）：MX38、MX39、MX14 → MX38

终局名册：MX31@0、MX33@0、MX25@2、MX18@4、MX35@4、GX01@7、GX02@8、MX24@9、GX03@12、MX41@12、MX38@14

## 限制

- original49 和 expanded53 都不是正式供给；expanded53 只是检验高阶池覆盖补洞的候选。
- 标签贪心不是最优阵容、玩家行为或 AI；不保证坦克、治疗、输出齐全。
- 不模拟技能、战斗、装备、材料支付、存活、阵型、部署取舍或运营机会成本。
- 原 7 职业＋7 体系及其 14 羁绊没有修改；比率只属于此算法、池和假设。
- 终局 11 人名册另有 10 人可部署最大主体系人数分布，不能把全名册直接当作激活羁绊。

## 自检

共执行 140,000 runs。候选去重、已拥有排除、发现阶位、取得来自当次候选、终局 11 人与机会不提前均在每个 run 的每个事件即时断言，不依赖是否保存示例轨迹；本次无断言失败。
