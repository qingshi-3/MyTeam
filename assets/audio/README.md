# 首版反馈音效

当前单位动作声已迁入独立场景的 `UnitAnimationComponent.FrameSounds`：按动作和从 1 开始的帧号配置，详见 [配置说明](../../content/audio/README.md)。下文的事件路由仍负责真实命中等结果反馈，以及没有配置帧标记的动作回退；已配置的普攻不再由 attack／projectile_spawn 重复播放动作声。

`feedback/` 来源为 Counterplay Games / OpenDuelyst 官方仓库 `app/resources/sfx/`，仓库 README 明确以 CC0 1.0 发布；本目录保存完整许可。`source-manifest.json` 固定下载 commit、每个上游文件 URL、原件与加工件 SHA-256 及加工参数。未使用来源未核实的旧 donor 音频，运行时不依赖外部路径。

`feedback/*.ogg` 为选取并加工的 17 个短反馈：离弦、箭命中、挥击、近战命中、冻结、获盾、破盾、爆炸、召唤、恢复、施法、阵亡、胜利、失败、选择、拒绝、购买。破盾使用冰柱溶裂的短暂质感作首版候选，阵亡使用非人声魔像碎裂，失败使用暗种法术声作首版提示；这些属于项目适配，并非原作的同名用途。没有配乐、角色配音。

加工：去除开头低于 −48 dB 的静音、按用途截成 0.16–1.4 秒、响度目标 −22 LUFS / 峰值 −5 dB、单声道 44.1 kHz、尾部淡出、Vorbis quality 4。短脉冲 LUFS 不保证感知完全等响；最终听感仍需实际试听和玩家反馈。`rebuild_audio.py` 可按清单重建；传 cue 名只重建对应文件。需要 `imageio-ffmpeg`，不在运行时执行。

运行接线：`scenes/audio/FeedbackAudio.tscn` 每实例固定 12 声部，按事件优先级、同类间隔及同类并发限流，普通高频攻击较轻，UI 结果优先于按钮选择。轻微音高变化只使用表现层递增计数，不消费战斗 RNG。战斗与 UI 各自归属，暂停只暂停战斗声部；战斗更换、异常清理及树退出停止旧声音。Master 音量沿用设置，0 会真正静音，恢复正值会解除静音。

声音只消费事实：projectile_spawn 离弦，projectile_impact 碰撞，AttackLanded 近战成功命中，有效 ShieldResolved / HealingResolved，首次获得 state.frozen，真实 summoned / revived / defeated，VFX 时序与明确殉爆 AbilityResolved。不会把轨迹更新、状态刷新、临时单位清理或特效消失误当声音事件。尸爆映射在 authored 音频场景内，不更改英雄或模拟器。UI 仅首页菜单可交互按钮在 MouseEntered 或非悬停的 FocusEntered 时播放，点击不叠加；首页范围由 GameRoot 显式传入 MainMenu。内部按钮悬停／焦点移动静音，仅接受点击或键盘／手柄确认后的 Pressed 播放；动态按钮遵循同一规则。购买／拒绝结果音优先，单次操作不叠加普通选择音。

验证入口：`tests/AudioFeedbackSmoke.tscn`，需要窗口与音频驱动。它使用独立测试存档和 authored SubViewport 渲染正式 GameRoot，通过视口 GUI 鼠标／键盘输入检查首页悬停、停留、重新进入、禁用、焦点及点击去重；设置页检查内部悬停／焦点静音、鼠标／键盘确认播放及立即切屏的操作；动态商店按钮检查点击和购买结果去重，再经过部署／开战／暂停／恢复。单次首页悬停、内部点击的混音 PCM 分别保存为 `.godot/ui-review/ui-hover-input.wav`、`ui-internal-click-input.wav`，完整混音为 `audio-feedback-main-flow.wav`。隔离视口只接受 Viewport.PushInput 经正式命中与焦点路径输入，外层显示其渲染纹理，不转发桌面鼠标事件，避免用户同时使用桌面时污染检查；不直接发射按钮信号。Master 捕获器位于最终主音量前，不可将该录音误当最终硬件输出的静音证据；测试另核对保存音量 0 / 正值的总线静音状态。运行结果由活动任务记录，自动输入与混音检查不代表主观体验验收。

## 清脆 UI 点击试听（2026-09-15）

用户已确认单独 A 轻鼠标点击的音色可用，并将触发规则收窄为首页 hover、内部点击。A 全长 65 毫秒，单声道 48 kHz PCM16，峰值 −9 dBFS。`ui-click-study/` 保留 A、B 硬质按键、C 双段卡扣与 D（B→A）四份原创程序合成声音作为可恢复试听；用户已否定 B→A 组合。它们不使用外部录音或生成式音频模型，不属于上述 OpenDuelyst 来源清单。`generate.py` 用 Python 标准库确定性重建；`manifest.json` 保存设计意图、格式、时长、峰值和 SHA-256。每版包含单次 WAV 与 3.5 秒试听（慢点三下，再连点四下）。

试听目录用 `.gdignore` 排除导入；A 的副本 `ui/ui_hover.wav` 已绑定正式场景的 `ui_select`，来源与校验值见 `ui/source-manifest.json`，Godot 无压缩、不裁剪、不归一化、不循环。现有场景增益 −4 dB、65 毫秒最小间隔和单声部限制沿用，避免快速操作时堆叠。旧 `feedback/ui_select.ogg` 作为可恢复素材保留，CC0 重建脚本不覆盖新 WAV。格式、端点与无削波检查通过；音色认可仅针对 A，不扩大为对其他首版音效的认可。
