# 用 Timeline 修改机器人带路

这版演出约 6.5 秒：机器人招手 → 转身带路 → 双门打开 → 镜头跟随 → 亮光盖住画面 → 主题选择。

三维机器人已经保存在 MainMenu 场景中，取代原来的平面头像。原机器人和九个动作资产保持原样。

## 打开与预览

1. 打开 `Assets/_Project/Scenes/MainMenu.unity`。
2. 在 Hierarchy 选择 `Entrance Timeline`，它的 Playable Director 绑定了 `LaboratoryEntrance.playable`。
3. 菜单 `Window > Sequencing > Timeline` 打开时间轴；也可以使用 `Calculus Lab > Main menu > Open entrance Timeline`。
4. 拖动播放头预览机器人、门、镜头和界面的关键帧；在 Play 模式点击 Start，检查完整页面切换。

页面切换 Signal 只在游戏运行时执行，编辑器预览不会打开主题选择页。

## 时间轴里有什么

| 轨道 | 控制什么 | 想改时看哪里 |
|---|---|---|
| Robot • wave, then floating walk | 招手和漂浮前进的身体动作 | Come with me / Lead the learner 两段动画及交叠过渡 |
| Robot • turn and lead through the door | 转身、向门内移动、短暂侧身回望 | Guide-Route.anim 的位置和旋转关键帧 |
| Left leaf / Right leaf | 两扇门绕边缘的门轴打开 | Door-Left.anim / Door-Right.anim |
| Centre seam | 闭门时中央的亮线 | Activation Track 的激活片段 |
| Camera • follow behind the guide | 等待机器人，再跟着向前 | Camera-FollowGuide.anim 的位置、旋转和 Lens Shift |
| UI • clear the view, then reveal Topics | 菜单退场、走廊铺满屏幕、亮光进出 | UI-Entrance.anim 的 Canvas Group Alpha 和 Rect Transform |
| SIGNAL • open Topics under full light | 亮光盖住画面后进入主题选择 | Signal 标记与 Signal Receiver 的事件绑定 |

点击动画片段，在 Animation 窗口编辑关键帧。Timeline 负责安排片段的起点、时长和混合。

信号放在亮光完全盖住画面之后。流程组件还会确保完整遮盖帧已渲染，才切换页面。

## GameManager 负责流程

`Start Button On Click → ProductionCanvas.OnHomeStart → GameManager.EnterLaboratory → PlayableDirector.Play()`。

Playable Director 使用 Unscaled Game Time，所以游戏时间暂停时演出也能继续。

`MainMenuEntrance` 负责输入锁定、接收 Signal、调用已有页面导航，以及在 Timeline 停止后恢复原始位置。
门、机器人路线、镜头、UI 和亮光的主要动画保存在 Timeline / Animation Clip 中。

返回主菜单时会恢复闭门、镜头和机器人的起点。再次点击 Start 可以重新播放。

## 资产位置

- 时间轴及动画：`Assets/_Project/Art/UI/MainMenu/Timeline/`
- 主时间轴：`LaboratoryEntrance.playable`
- 主题选择信号：`EnterTopics.signal`
- 机器人 prefab：`Assets/_Project/Prefabs/Resources/Production/MainMenuGuide.prefab`
- 场景中的机器人：`Main Menu Lobby > Laboratory guide`
- 原始机器人动作：`Assets/_Project/Art/LaboratoryAI/Animations/`；这些原文件没有修改。

正式关卡仍通过原有 GameManager 和场景转场进入。

[Unity Timeline 官方说明](https://docs.unity.com/en-us/engine/6000.3/manual/packages-list/packages-all/pack-safe/com-unity-timeline)
