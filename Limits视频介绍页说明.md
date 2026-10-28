# Limits 视频介绍页

现已扩展为三关共用播放器，最新入口与检查记录见 [三关视频介绍页说明](三关视频介绍页说明.md)。

选择 Limits 后，原来的文字介绍页会显示机器人英文教学视频。右侧保留任务说明、播放或暂停、重播、Start mission 和 Back to menu。

视频结束后保持最后画面，等待玩家点击 Start mission。观看不是通关条件，播放或准备过程中都可以直接开始或返回。介绍页不累计考核时间、不提交答案，也不消耗 Support Hearts。

## 在 Unity 中查看和编辑

- 菜单 `Calculus Lab / Limits / Use robot video on introduction page` 是一次性编辑工具，会重新应用介绍页布局。修改过布局后不要随意重新执行。
- 保存的页面位于 `Assets/_Project/Prefabs/Resources/Production/LimitsUI.prefab` 的 Brief 下。
- 播放器组件和按钮位于同目录的 `LimitsBriefingVideo.prefab`。
- Game Manager 的 `Mission — saved introduction video` 引用指向该关卡的播放器。
- 在播放器 Inspector 的 Video Player / Video Clip 替换视频；封面在 BriefVideoPoster 的 Texture 中替换。
- 本地视频和封面位于 `Assets/_Project/Art/Video/LimitsBriefing/`。
- Play/Pause 与 Replay 按钮的 Inspector On Click 已保存。Start 和 Back 沿用原页面按钮。

## 播放行为

场景转场结束、介绍页显示后自动准备并播放。音轨通过二维 Audio Source 输出，跟随现有主音量。点击 Start 或返回会立即停止视频和声音；存档恢复直接进入探索时不播放介绍。

如果视频准备失败，页面显示说明，Start 仍然可用，Replay 可以重试。视频、封面、按钮和地图都在运行前保存；运行时只创建私有播放纹理，不生成地图或界面。

## 资源来源

视频和封面复制自本机独立工程 `C:/Users/Huawei/Documents/ChatGPT/Calculus Limits Robot Teacher/output/`。机器人教学视频为英文，Sonia 英式女声，90 秒。游戏播放使用项目内的副本，不依赖原工程、网络服务或 AI 语音接口。
