# WorldKit.Streaming.SaveKitAdapter

这个 Adapter 使用 SaveKit Section 持久化 WorldKit Runtime Delta，不直接序列化 `IWorldDelta` 多态对象。

## 显式 Codec

每种 Delta TypeId 必须显式注册 `IWorldDeltaCodec`：

- `TryEncode(IWorldDelta -> string payload)`
- `TryDecode(Target + Version + payload -> IWorldDelta)`

Snapshot 保存 Stable TypeId、Version、World/Region/Chunk Target 和 payload。没有反射扫描、CLR `$type` 元数据或运行时类型名依赖。

## 原子 Restore

`WorldDeltaPersistenceState` 先把完整 Snapshot 解码到候选 `WorldDeltaSet`。任一 entry 的 TypeId、Version、Target、codec 或 payload 非法时全部失败，旧 DeltaSet 保持不变；只有完整验证后才替换当前状态。

真实 SaveKit `Save -> Clear -> Load` 已纳入 EditMode 验证。

## 使用案例：持久化玩家改造的 Chunk

游戏项目为自己的 `IWorldDelta` 类型实现一个稳定 TypeId 的 `IWorldDeltaCodec`，在启动时注册 codec，再把状态装入 SaveKit Section：

```csharp
using System;
using StellarFramework.WorldKit;
using StellarFramework.WorldKit.Streaming.SaveKitAdapter;

var codecs = new WorldDeltaCodecRegistry();
if (!codecs.TryRegister(new PlayerHeightDeltaCodec(), out string error))
    throw new InvalidOperationException(error);

var state = new WorldDeltaPersistenceState(WorldId.From("world.main"), codecs);
state.DeltaSet.Append(playerHeightDelta); // IWorldDelta, targeted at one Chunk
WorldDeltaSnapshot snapshot = state.CaptureSnapshot();
if (!state.ValidateSnapshot(snapshot, out error))
    throw new InvalidOperationException(error);
state.RestoreSnapshot(snapshot);
```

正式接入时，在 SaveKit 初始化后注册 `new WorldDeltaSaveSection(state)`，然后调用 `SaveKit.SaveAsync(slot)` / `LoadAsync(slot)`。流送重建时先按 seed 重建未修改的基础 Chunk，再把已加载 Delta 应用到对应 Chunk；SaveKit Section 只负责原子保存和恢复 Delta Snapshot。
