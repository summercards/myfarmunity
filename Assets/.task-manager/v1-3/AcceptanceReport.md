# v1.3 验收报告

更新时间：2026-04-29

## 当前结论

v1.3 已完成中控核心骨架和 3 个试点系统的代码接入，但尚未完成 Play Mode 场景回归。当前状态应判定为：

```text
代码施工完成，运行时总体验收待执行。
```

## 已完成项

| 项目 | 结果 |
|---|---|
| 读取目标文档和施工手册 | 已完成 |
| 中控基线审计 | 已完成 |
| RuntimeRefs 迁移规则 | 已完成 |
| `GameRuntimeContext` | 已实现 |
| `GameServiceRegistry` | 已实现 |
| `GameEventBus` | 已实现 |
| `GameCommandBus` | 已实现 |
| `GameStateService` | 已实现 |
| 时间系统试点 | 已实现代码接入 |
| 钱包系统试点 | 已实现代码接入 |
| 商店购买命令试点 | 已实现代码接入 |
| 商店金币文本中控事件刷新 | 已补齐 `MiniShop` 与 `SimpleShopUI` |
| AI 新功能接入模板 | 已输出 |

## 编译验证

执行命令：

```text
dotnet build .\FarmGame.Gameplay.csproj --no-restore -v:minimal
```

结果：

- 成功生成 `FarmGame.Gameplay.dll`。
- 0 个 C# 错误。
- 4 个 warning，均为 Unity/包引用中的既有程序集版本冲突：
  - `System.Net.Http`
  - `System.Security.Cryptography.Algorithms`

## 中控核心行为静态验收

| 能力 | 验收结果 |
|---|---|
| 服务注册、获取、注销 | 代码具备，外部编译通过 |
| 事件订阅、取消、发布 | 代码具备，外部编译通过 |
| 命令注册、取消、执行 | 代码具备，外部编译通过 |
| 未注册命令失败返回 | 代码具备，返回 `CommandResult.Fail` |
| 状态服务不依赖 UI | 代码满足 |
| 中控随启动创建 | 代码通过 `RuntimeInitializeOnLoadMethod` 自动创建 |
| 挂到 `AppRoot` | 代码通过 `AppRoot.AttachPersistent` 挂接 |

## 未完成运行时验收

以下项目尚未在 Play Mode 执行：

- 玩家移动回归。
- 时间推进事件回归。
- 存档保存/读取回归。
- 背包刷新回归。
- 商店购买命令回归。
- NPC 对话回归。
- 传送主链路回归。

原因：

- 当前桌面团结编辑器占用了同一个工程，batchmode 无法打开项目。
- Unity MCP Console 读取返回 `Unity session not available`。

## 版本完成判定

当前不能标记 v1.3 全量完成。阻塞项是：

- 需要在团结编辑器中刷新项目，确认新脚本和 `.meta` 导入状态。
- 需要执行 Play Mode 回归。
- 需要确认商店购买命令在真实场景中不重复扣费、不误触发 fallback。

## 下一步验收步骤

1. 在团结编辑器中等待脚本编译完成。
2. 打开 Console，确认没有 C# error。
3. 进入主场景 Play Mode。
4. 验证时间推进时是否发布事件。
5. 验证钱包变化时商店 UI 金币文本刷新。
6. 验证商店购买走 `BuyShopItemCommand`，失败时返回可读提示。
7. 回归玩家移动、背包、NPC、传送、存档。

## 剩余风险

- `RuntimeRefs.InventoryBridge` 仍参与商店购买命令，后续应抽象为 `IInventoryService`。
- 旧商店购买逻辑和命令购买逻辑短期共存，需要通过真实场景测试确认不会重复执行。
- 新建脚本已配套 `.meta`，仍需 Unity 刷新后确认导入状态。
