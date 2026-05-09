# RuntimeRefs 迁移规则

适用版本：v1.3 中控模式框架搭建

## 核心原则

`RuntimeRefs` 在 v1.3 中只作为兼容层保留，不再作为新增系统的默认接入方式。新增功能应走：

```text
服务接口 -> 命令 -> 功能系统 -> 事件 -> UI/其他系统
              -> 运行时状态 -> 存档
```

## 允许继续使用 RuntimeRefs 的情况

- 旧系统已有调用，且本版本不计划迁移。
- 场景启动阶段需要兼容现有注册流程。
- 传送、相机、玩家 Transform 等强场景对象仍依赖旧主链路。
- 为避免大规模回归风险，试点接入时需要从 `RuntimeRefs` 读取旧系统实例并包装成适配器。

## 禁止新增的情况

AI 新增功能时不得：

- 在 `RuntimeRefs` 新增具体系统字段，例如 `FishingSystem`、`CookingSystem`、`PetSystem`。
- 为 UI 新增 `RuntimeRefs.SomeFeatureUI` 这类全局 UI 引用。
- 通过 `RuntimeRefs` 让两个玩法系统互相直接调用。
- 绕过 `GameEventBus` 直接让 UI 订阅多个玩法对象事件。
- 绕过 `GameCommandBus` 直接让 UI 修改玩法数据。

## 迁移优先级

| 优先级 | 类型 | 处理方式 |
|---|---|---|
| P0 | 新增功能 | 不允许进 `RuntimeRefs`，必须走中控模板 |
| P1 | 时间、背包/钱包、商店/NPC 试点 | 用适配器包装旧对象，向中控注册服务、事件、命令 |
| P2 | UI 直接持有玩法对象 | 逐步改为监听事件或发送命令，旧 Inspector 引用可暂留 fallback |
| P3 | 相机、传送、玩家 Transform | 暂缓迁移，避免影响主控制链路 |
| P4 | 编辑器工具和修复脚本 | 本版本不强制迁移 |

## 新功能接入标准

AI 新增一个功能模块时，必须优先创建：

- `I<Feature>Service`
- `<Feature>Events`
- `<Feature>Commands`
- `<Feature>State` 或状态快照结构
- `<Feature>SaveParticipant`，如功能需要长期保存
- `<Feature>Adapter`，如需要接入旧系统
- 最小 UI 适配器或监听器

不得先修改：

- `RuntimeRefs.cs`
- `GameManager.cs`
- `SaveManager.cs` 存档格式
- 其他功能模块的核心代码

## 从 RuntimeRefs 迁移到中控的模式

### 读取型服务

旧模式：

```text
SomeSystem -> RuntimeRefs.TimeSystem
```

目标模式：

```text
SomeSystem -> GameServiceRegistry.TryGet<ITimeService>()
```

### 数据变化通知

旧模式：

```text
UI -> RuntimeRefs.PlayerWalletChanged
UI -> PlayerWallet.OnChanged
```

目标模式：

```text
PlayerWalletAdapter -> GameEventBus.Publish(WalletChangedEvent)
UI -> GameEventBus.Subscribe<WalletChangedEvent>()
```

### 玩法请求

旧模式：

```text
ShopUI -> MiniShop.Buy()
```

目标模式：

```text
ShopUI -> GameCommandBus.Execute(BuyItemCommand)
ShopCommandHandler -> Inventory / Wallet / ShopCatalog
```

## v1.3 允许的适配器策略

为了降低风险，适配器可以临时读取旧对象：

- 通过 `RuntimeRefs.TimeSystem` 建立 `TimeServiceAdapter`。
- 通过 `RuntimeRefs.PlayerWallet` 建立 `WalletServiceAdapter`。
- 通过 `RuntimeRefs.InventoryHolder` 建立 `InventoryServiceAdapter`。
- 通过 `RuntimeRefs.SimpleShopUI` 或现有商店系统建立命令处理器。

但适配器对外只能暴露接口、事件或命令，不把旧对象继续传播出去。

## 验收要求

每完成一个迁移或试点，必须检查：

- 是否新增了 `RuntimeRefs` 具体字段；如果有，验收失败。
- 是否提供接口或事件/命令替代直接对象访问。
- 是否保留旧场景兼容路径。
- 是否有取消事件订阅或注销服务的路径。
- 是否记录改动文件、验证方式和未迁移风险。

## 下一版本建议

v1.3 完成后，后续版本可以按以下顺序继续迁移：

1. UI 层从 `RuntimeRefs` 读取改为事件监听。
2. 商店购买/出售全面改为命令。
3. NPC 对话打开/关闭全面改为命令。
4. 背包和钱包暴露稳定服务接口，隐藏具体 MonoBehaviour。
5. 传送和场景系统建立独立 `IPortalService`。
