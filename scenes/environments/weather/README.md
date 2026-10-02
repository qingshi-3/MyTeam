# 庭院天气展示资源

这四份 `.tres` 是静态美术参数，由 `WeatherPresentationStyle` 定义。时间、暂停和材质副本属于 `BattleEnvironment` 实例；运行时不改写或保存这些资源。

| 资源 | 表现 | 独立场景 |
|---|---|---|
| `clear.tres` | 已有云影、平行日光及浮尘 | `../SunlitCourtyard.tscn` |
| `rain.tres` | 阴天色调、三层斜雨、同落点水花、湿石反光和薄雾 | `../RainyCourtyard.tscn` |
| `snow.tres` | 冷色环境光、三层雪片、石路薄雪 | `../SnowyCourtyard.tscn` |
| `storm.tres` | 强风雨、较暗环境光、照亮地表／降水／雾的云闪 | `../StormCourtyard.tscn` |

独立打开对应 `.tscn` 即可运行。共享背景的 `WeatherStyle` 导出字段可引用任一预设；已有实例可通过显式的 `SetWeatherStyle(style)` 展示命令切换，传 `null` 恢复晴天。`Advance(delta)` 继续只接收表现时间，不接收任何局内、关卡或战斗对象。

主菜单 → 练习与预览 → 天气预览，可用真实按钮切换、暂停和减弱动效。预览直接复用生产背景和上述预设，不加载局内内容包，也不修改存档。游戏默认仍是晴天；此轮没有加入天气随机选择、跨页面天气偏好或按关卡切换规则。

天气层输入穿透；与太阳空气层使用相同 `AtmosphereZIndex`，由既有界面层级保证 HUD 在上方。减弱动效冻结表现时间，隐藏雨雪并关闭云闪，保留色调、湿地和薄雪。预览中的“暂停”保留当前帧。

所有新雨线、雪片、水花、覆雪和闪光形状由本仓库 shader 生成，无新增外部位图依赖。底图仍使用原庭院资产。来源、作者说明与项目适配分别记录在 [P01-S72](../../../design-discussion/02-foundation-models/combat-presentation/evidence.md)：Dante 的雨粒终点水花、NVIDIA Rain 示例、Lumine 多层雪代码（CC0）和 NWS 云内闪电说明；没有复制这些页面中的图片或视频到游戏。

地表润湿／积雪是针对这张庭院底图的二维颜色及纹理近似，不是三维积雪模拟。水花只在预设的庭院地面区域出现，不查询碰撞、英雄或寻路。
