# 单位动画音效配置

## 当前战斗混音

每个 `FeedbackSound.MixGroup` 引用 `mix/` 中的静态分组；发声帧不变，密集时只丢弃声音，不延迟动画或战斗结算。`MaximumDuration` 为短尾上限，0 保留原音频长度；末段最多 40ms 淡出。UI 声音不加入战斗分组。

| 分组 | 最小发声间隔 | 最多同时播放 | 用途 |
| --- | ---: | ---: | --- |
| attack | 55ms | 3 | 轻挥击／离弦；180ms 短尾 |
| impact | 140ms | 2 | 次层接触反馈；100–120ms 短尾 |
| support | 220ms | 2 | 治疗、获盾、冻结、召唤与普通死亡 |
| highlight | 100ms | 2 | 技能、爆炸、破盾 |

同音源 Cue 仍有自己的间隔／上限，与分组约束共同生效；组满时更高优先级可替换组内较低者。关键声被实际接受后，普通战斗声在约 25ms 内降低 7dB，短暂保持后约 200ms 平滑恢复；暂停冻结包络，清理恢复。参数是本项目首版试听值，不是云顶参数。普通命中当前为 -17／-19dB，挥击／离弦为 -12／-11dB；均为播放器增益，不能直接当感知响度比较。

## 编辑单位标记

在英雄／敌人／小兵的独立场景中选 `VisualRoot/UnitAnimationComponent`，展开 `FrameSounds`。
每条配置包含 `Animation`（逻辑动作）、`Frame`（从 1 开始）、`Sound`（音源及音量、并发规则）。可为同一动作添加多条；数组为空沿用旧事件反馈。
标记保存在本目录 units 下，每个单位独立可调；声音文件可以共享。不要修改 SpriteFrames 来调声音。
例如铁甲卫的 `units/hero_hc03_iron_guard_attack.tres` 中 `Animation = "attack"`、`Frame = 10`，表示进入普攻第 10 帧时开始播放。更换音源可给该标记指定独立的 `FeedbackSound`，再配置 `Stream` 和 `VolumeDb`；如要单独修改，先复制共享 Sound 资源。`Cue` 必须非空，用来分组限流；同组过密的声音会被丢弃，不排队补响。支持 `attack`、`skill_cast`、`move`、`idle`、`defeated`、`displaced`，动作回退时按实际动画帧数校验。
实验室选中场上单位或库中原型后，点“动画音效”，可以查看当前帧、前／后一帧静默检查、重播及临时修改标记试听。逐帧检查／修改标记后，继续会从头重播，避免检查位置与计时错位；永久值以这些 .tres 为准。

本批迁移普攻动作声；命中声仍读取实际命中事件，不因播放动画而制造命中。
HC03／同外观Boss和HC04已看逐帧图选择出手帧，HC01按实际离弦进度对应第13帧并检查姿势；其他近战暂置中段，未声称逐英雄完成主观试听。
投射物取离弦进度所在帧的开始，帧粒度与实际离弦可有不到一帧的差异，可继续逐单位调整。

| 单位ID | 名称 | 普攻总帧数 | 发声帧 | 声音 | 当前依据 |
| --- | --- | ---: | ---: | --- | --- |
| hero_hc01_crossbow | 连弩手 | 15 | 13 | arrow_release | 按离弦进度换算（帧边界） |
| hero_hc03_iron_guard | 铁甲卫 | 26 | 10 | melee_swing | 已看逐帧图：开始旋转卷起冰风 |
| hero_hc04_crit_duelist | 追刃客 | 10 | 8 | melee_swing | 已看逐帧图：刀光展开 |
| hero_hc05_tide_caster | 回潮术士 | 14 | 12 | arrow_release | 按离弦进度换算（帧边界） |
| hero_hc06_surge_conductor | 共鸣使 | 29 | 24 | arrow_release | 按离弦进度换算（帧边界） |
| hero_hc07_death_echo | 传灯狐 | 16 | 8 | melee_swing | 动作中段暂值，待逐单位试听 |
| hero_hc08_poison_keeper | 棘毒使 | 9 | 8 | arrow_release | 按离弦进度换算（帧边界） |
| hero_hc09_breach_scribe | 裂印使 | 24 | 20 | arrow_release | 按离弦进度换算（帧边界） |
| hero_hc10_ember_cashout | 引烬师 | 24 | 20 | arrow_release | 按离弦进度换算（帧边界） |
| hero_hc11_triad_weaver | 三相使 | 10 | 9 | arrow_release | 按离弦进度换算（帧边界） |
| hero_hc12_control_clock | 缚钟人 | 11 | 9 | arrow_release | 按离弦进度换算（帧边界） |
| hero_hc13_shield_crit | 晶盾剑士 | 26 | 13 | melee_swing | 动作中段暂值，待逐单位试听 |
| hero_hc14_stone_fist | 磐岩拳师 | 12 | 6 | melee_swing | 动作中段暂值，待逐单位试听 |
| hero_hc15_shield_grower | 接盾战士 | 19 | 10 | melee_swing | 动作中段暂值，待逐单位试听 |
| hero_hc16_shield_bomber | 碎盾轰卫 | 22 | 11 | melee_swing | 动作中段暂值，待逐单位试听 |
| hero_hc17_pain_vessel | 蓄痛卫 | 12 | 6 | melee_swing | 动作中段暂值，待逐单位试听 |
| hero_hc18_healing_reader | 济世医师 | 19 | 16 | arrow_release | 按离弦进度换算（帧边界） |
| hero_hc19_overheal_priest | 溢光祭司 | 29 | 24 | arrow_release | 按离弦进度换算（帧边界） |
| hero_hc20_blood_drummer | 血契鼓手 | 29 | 15 | melee_swing | 动作中段暂值，待逐单位试听 |
| hero_hc21_last_stand | 背水狂徒 | 17 | 9 | melee_swing | 动作中段暂值，待逐单位试听 |
| hero_hc22_dodge_assassin | 风步刺客 | 29 | 15 | melee_swing | 动作中段暂值，待逐单位试听 |
| hero_hc23_frost_historian | 霜羽猎手 | 24 | 20 | arrow_release | 按离弦进度换算（帧边界） |
| hero_hc24_swarm_keeper | 虫群牧者 | 10 | 9 | arrow_release | 按离弦进度换算（帧边界） |
| hero_hc25_death_provider | 送葬人 | 19 | 16 | arrow_release | 按离弦进度换算（帧边界） |
| hero_hc26_ash_returner | 灰烬行者 | 20 | 10 | melee_swing | 动作中段暂值，待逐单位试听 |
| hero_hc27_ally_consumer | 饕餮 | 37 | 19 | melee_swing | 动作中段暂值，待逐单位试听 |
| hero_hc28_corpse_raiser | 拾骸者 | 29 | 24 | arrow_release | 按离弦进度换算（帧边界） |
| hero_hc29_mind_binder | 缚心者 | 13 | 11 | arrow_release | 按离弦进度换算（帧边界） |
| hero_hc30_armor_broadcaster | 山岳旗手 | 23 | 12 | melee_swing | 动作中段暂值，待逐单位试听 |
| hero_hc31_breach_lancer | 破阵骑兵 | 26 | 13 | melee_swing | 动作中段暂值，待逐单位试听 |
| hero_hc32_sky_monk | 腾空武僧 | 13 | 7 | melee_swing | 动作中段暂值，待逐单位试听 |
| hero_hc33_rift_assassin | 影隙刺客 | 29 | 15 | melee_swing | 动作中段暂值，待逐单位试听 |
| hero_hc34_shockshield_guard | 震盾卫 | 19 | 10 | melee_swing | 动作中段暂值，待逐单位试听 |
| hero_hc35_hook_hunter | 铁钩猎手 | 9 | 5 | melee_swing | 动作中段暂值，待逐单位试听 |
| hero_hc36_cyclone_weaver | 风眼术士 | 14 | 12 | arrow_release | 按离弦进度换算（帧边界） |
| enemy_blood_reaver | 血潮掠夺者 | 29 | 15 | melee_swing | 动作中段暂值，待逐单位试听 |
| enemy_boreal_boss | 第一层主：北境重装 | 26 | 10 | melee_swing | 已看逐帧图：开始旋转卷起冰风 |
| enemy_carrion | 腐尸爬兽 | 10 | 5 | melee_swing | 动作中段暂值，待逐单位试听 |
| enemy_clockwork_boss | 塔顶主宰：欺诈机神 | 30 | 25 | arrow_release | 按离弦进度换算（帧边界） |
| enemy_crossbow | 塔垛弩手 | 15 | 13 | arrow_release | 按离弦进度换算（帧边界） |
| enemy_cutpurse | 沙路割喉者 | 10 | 8 | melee_swing | 已看逐帧图：刀光展开 |
| enemy_hexer | 流沙咒师 | 14 | 7 | skill | 动作中段暂值，待逐单位试听 |
| enemy_ice_blade | 冰誓刃卫 | 29 | 15 | melee_swing | 动作中段暂值，待逐单位试听 |
| enemy_ice_hawker | 冻原鹰手 | 24 | 20 | arrow_release | 按离弦进度换算（帧边界） |
| enemy_resistance_dummy | 抗性验证傀儡 | 12 | 6 | melee_swing | 动作中段暂值，待逐单位试听 |
| enemy_rust_guard | 锈甲守卫 | 19 | 10 | melee_swing | 动作中段暂值，待逐单位试听 |
| enemy_scale_brute | 青鳞蛮兽 | 12 | 6 | melee_swing | 动作中段暂值，待逐单位试听 |
| enemy_shadow_boss | 第二层主：暗影领主 | 23 | 12 | melee_swing | 动作中段暂值，待逐单位试听 |
| enemy_wyrm | 塔巢幼龙 | 20 | 17 | arrow_release | 按离弦进度换算（帧边界） |
| soldier_bc_insect | 虫卒 | 10 | 5 | melee_swing | 动作中段暂值，待逐单位试听 |
| soldier_bc_morsel | 食饵 | 37 | 19 | melee_swing | 动作中段暂值，待逐单位试听 |
| soldier_bc_raised | 复生骸骨 | 9 | 5 | melee_swing | 动作中段暂值，待逐单位试听 |
| soldier_bc_skeleton | 砂骨仆 | 27 | 14 | melee_swing | 动作中段暂值，待逐单位试听 |
| soldier_dummy_melee | 近战小兵假人 | 10 | 8 | melee_swing | 已看逐帧图：刀光展开 |
| soldier_dummy_ranged | 远程小兵假人 | 15 | 13 | arrow_release | 按离弦进度换算（帧边界） |
