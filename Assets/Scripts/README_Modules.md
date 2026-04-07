# FarmGame 模块与命名空间约定

## 当前 asmdef 边界

- `FarmGame.Core`
  - 路径：`Assets/Scripts/Core`
  - 作用：跨场景生命周期与基础运行时服务（`AppRoot`、`RuntimeService`）。
- `FarmGame.Gameplay`
  - 路径：`Assets/Scripts`（排除嵌套 asmdef）
  - 作用：现阶段主要运行时业务逻辑（角色、种植、建造、商店、时间、存档等）。
- `FarmGame.ActorSystem`
  - 路径：`Assets/Scripts/ActorSystem`
  - 作用：NPC Actor 主线模块（Actor/Dialogue/Quest/Gift/Shop）。
- `FarmGame.NPCSystem`
  - 路径：`Assets/Scripts/NPCSystem`
  - 作用：旧 NPC 数据与兼容桥接层（`NPCDefinition/NPCFunction/NPCInteractable`）。
- `FarmGame.Editor`
  - 路径：`Assets/Scripts/Editor`
  - 作用：编辑器工具脚本（仅 Editor 平台）。
- `FarmGame.SceneFixers.Editor`
  - 路径：`Assets/Scripts/SceneFixers`
  - 作用：场景修复与模板工具（仅 Editor 平台）。

## 命名空间策略（统一到 `FarmGame.*`）

- 核心基础设施：`FarmGame.Core`
- 玩法层：`FarmGame.Gameplay.*`
- 交互 UI：`FarmGame.UI`
- Actor 体系：`FarmGame.ActorSystem`
- 旧 NPC 数据/兼容：`FarmGame.NPCSystem`
- 编辑器工具：`FarmGame.Editor.*`

> 说明：当前仓库存在历史全局命名空间脚本。本轮已完成 `Core + Gameplay + ActorSystem + NPCSystem + Editor + SceneFixers(Editor)` 边界；并新增 `FarmGame.Core.Contracts`（`IDialogSubject/IDialogUI/IDialogWorldBridge/IHintHUD`）用于削弱运行时对具体 UI 类的依赖。`UI` 与 Gameplay 仍有双向引用，建议下一轮继续抽取 Shop/Inventory UI 合约后再独立 `FarmGame.UI.asmdef`。
