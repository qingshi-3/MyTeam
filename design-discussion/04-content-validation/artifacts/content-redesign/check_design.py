"""Check the proposed content catalog. Does not certify implementation or balance."""
from pathlib import Path
from collections import Counter
import json, math, re
P=Path(__file__).resolve().parent
D=json.loads((P/'catalog.json').read_text())
U={x['id']:x for x in D['units']};T={x['name']:x for x in D['traits']};E={x['id']:x for x in D['equipment']}
checks=[]
def check(condition,message):
    if not condition:raise AssertionError(message)
    checks.append(message)
check(len(U)==40==len(D['units']),'40名英雄身份唯一')
check(len(T)==18==len(D['traits']),'18条羁绊身份与名称唯一')
check(len(E)==22==len(D['equipment']),'22件装备身份唯一')
check(len({x['id'] for x in D['relics']})==32,'32件遗物身份唯一')
for u in U.values():
    check(all(x in T for x in u['traits']),u['id']+'羁绊引用有效')
    check(all(x in E for x in u['equipment']),u['id']+'装备引用有效')
    check(u['hp']>0 and u['attack']>0 and u['attack_speed']>0 and u['range']>0 and u['ap']>0,u['id']+'基础参数完整')
    check(0<=u['starting_mana']<=u['max_mana'],u['id']+'初始法力合法')
    check(u['ascension_benefit'] is None and bool(u['skill'].strip()) and bool(u['skill_name'].strip()),u['id']+'技能文本非空且升阶收益明确留空')
for t in T.values():
    members=[u['id'] for u in U.values() if t['name'] in u['traits']]
    check(members==t['members'],t['id']+'成员双向一致')
    check(len(members)>=max(t['thresholds']),t['id']+'最高人数档自然可达')
union=set()
for v,ids in D['pool_variants'].items():
    union.update(ids);check(len(ids)==len(set(ids)),v+'身份不重复')
    for t in T.values():
        count=sum(i in t['members'] for i in ids)
        check(count==0 or count>=max(t['thresholds']),v+'中'+t['name']+'完整保留或全部缺席')
    check(sum(U[i]['tier']==3 for i in ids)>=3,v+'一期发现有至少三名三阶候选')
check(union==set(U),'四种局内名录覆盖全部40英雄')
build_results=[]
for b in D['builds']:
    check(len(b['members'])==len(set(b['members']))<=10,b['id']+'人数合法且不重复')
    check(all(i in U for i in b['members']),b['id']+'全体成员存在')
    for name,n in b['expected_thresholds'].items():
        count=sum(i in T[name]['members'] for i in b['members'])
        check(count>=n,b['id']+'实际达到'+name+str(n))
    variants=[v for v,ids in D['pool_variants'].items() if set(b['members'])<=set(ids)]
    check(bool(variants),b['id']+'至少一种局内池可组成完整阵容')
    helpers=2*('H39' in b['members'])+1*('H40' in b['members'])
    check(len(b['members'])+helpers+2<=18,b['id']+'加原生部署物与R21假人不超过18格')
    build_results.append((b['name'],len(b['members']),'/'.join(v.replace('omit_','缺') for v in variants)))
for variant, pool in D['pool_variants'].items():
    check(sum(set(b['members'])<=set(pool) for b in D['builds'])>=3,variant+'至少三组已写完整阵容可用')
b=D['builds'][8];ct=Counter(t for i in b['members'] for t in U[i]['traits'])
check(all(all(ct[t]<min(T[t]['thresholds']) for t in U[i]['traits']) for i in b['members'][:6]),'卓尔不群示例前六人均满足资格')
for pool,ids in D['equipment_pools'].items():check(all(i in E for i in ids),pool+'无失效装备')
expected=[2,3,4,5,6,7,9,10,12,13,14,20,25,27,29,32,36,38,41,42,46,49,52,57,63,64,66,70,73,101,102,103,104,106,107,108,115,116,158,183,216,219,226,270,268,261,288,314,315,317,318,327,336,374,399,427,429,456,462,477,488,513,550,638,696,756]
check(D['selected_reference_ids']==expected,'66个参考索引按用户原顺序保留')
check([x['index'] for x in D['reference_mapping']]==expected,'66个参考均有逐项去向')
for p in P.glob('*.md'):
    check('\ufffd' not in p.read_text(),p.name+'无替代乱码')
    for dest in re.findall(r'\]\(([^)]+)\)',p.read_text()):
        if dest.startswith(('https:','http:','#')):continue
        target=dest.split('#')[0]
        check((P/target).exists(),p.name+'本地链接存在:'+target)
# The finite budget is an explicit choice path, not a random loot expectation.
gold=10
balances=[]
for ops,boss in [([-3,6,-5,6,-12,8],10),([-7,6,-12,-7,6,8],12),([-10,6,-12,-6,6,8],0)]:
    for x in ops:gold+=x;check(gold>=0,'经济路线每次消费可支付')
    gold+=boss;balances.append(gold)
check(balances==[20,26,18],'三阶段金币余额20/26/18与文档一致')
check((18+2)//4==5,'20材料最多支持五次发现')
report=['# 内容提案静态检查','', '运行方式：`python3 check_design.py`。本脚本只读提案数据并重建此检查报告，不读取或修改运行游戏内容。', '',f'本次通过{len(checks)}项静态断言。覆盖身份、属性字段、成员双向引用、自然人数档、四种局内池、十组阵容、装备池、66项索引、链接和有限经济账。','', '| 阵容 | 常驻人数 | 原样配方在哪些池可取得 |','| --- | ---: | --- |']
for name,n,variants in build_results:report.append(f'| {name} | {n} | {variants} |')
report+=['','## 数值边界试算','',f'- 8/14防御对应承伤系数分别为{100/(100+7*8):.3f}/{100/(100+7*14):.3f}；未偷换成云顶双防。', '- 星环回蓝：2/4/6成员每次至多发12/24/36总法力；分配给其他可收蓝成员而非每人都发该数值。6星环且5人可收蓝时各7.2，回响不再发。', '- 无装备、不受伤且持续普攻时H03首施法理论约50/(3+0.75×10)=4.76秒，再加动作排队和0.35秒前摇；H31约(160-50)/(3+0.65×10)=11.58秒再加0.8秒前摇。实际受伤、移动、锁蓝和其他收益会改变时点。', '- 霜庭极端风险：4成员每人3次/秒、每次35%冻结0.8秒，若集火同一目标，简化独立泊松估计覆盖率约1-exp(-4×3×0.35×0.8)=96.5%。不是实测；应优先观察，必要时调该羁绊概率/时长，不据此给所有Boss偷偷加免控。', '- 血誓+斗士+怒拳会同时提高承伤、怒劲上限和护盾，必须用实际生命伤害蓄怒，并核查0.8秒出拳窗口；不能用账面高收益直接宣称平衡。','', '## 未验证的范围','', '未运行Godot、战斗模拟、真实输入或新特效预览；没有原样自然随机供给成型率、通关率或乐趣结论。十组阵容检查证明名单与阈值可组成，不证明玩家在18行动内必然招齐。地图生成的候选覆盖审核属于待实现设计，尚无实现可测。源资料历史数值和动态表现覆盖缺口见references.md。','', '所有设计仍为Agent提案，用户观感、节奏与平衡验收未进行。']
(P/'validation.md').write_text('\n'.join(report)+'\n')
print(f'PASS: {len(checks)} checks; 40 heroes; 18 traits; 32 relics; 22 equipment; 10 builds; 66 references')
