# Phase 5 内存测试计划

> 状态：进行中
> 参考：`docs/WINUI3_MEMORY_OPTIMIZATION.md`
> 目标：用同一套指标比较当前“Flyout 内容长期保留”策略与“Flyout 关闭后重建面板”策略，再决定是否进入生命周期改造。

## 1. 结论先行

Phase 5 不直接做 `ListView` 虚拟化，也不先清空图片缓存。当前已经接入关闭后销毁 `MainPanelControl` 的实验实现，下一步用同一套口径测它相对长期保留策略的收益。

需要回答的问题只有一个：

```text
Flyout 关闭后，继续保留 MainPanelControl + Frame 缓存页面 + MainPanelSession，
是否比关闭后销毁并用纯数据 snapshot 重建更合适？
```

判断标准同时看：

- 关闭后 `Private Working Set` 是否明显回落。
- `Commit Size` 是否稳定，避免只修剪 working set 造成“看起来降了”。
- 首次打开和重开是否出现明显空白、图片重载、直播区闪现或高度跳动。
- 连续打开/关闭后控件、图片缓存和句柄数量是否持续增长。

## 2. 当前实验实现

当前实现已经从保留策略切到关闭后重建策略：

```text
App 启动
  -> 创建 TrayHostWindow
  -> 创建 TrayFlyoutService

Flyout 打开
  -> 创建 MainPanelControl
  -> MainPanelControl 创建 MainPanelSession
  -> MainPanelControl.OnFlyoutOpened()
  -> 当前页面 ActivateAsync()

Flyout 关闭
  -> MainPanelControl.OnFlyoutClosed()
  -> 取消仍在运行的 UI 请求
  -> 导出 MainWindowSnapshot
  -> 清空 Flyout.Content
  -> Dispose MainPanelControl、Frame 缓存页面、MainPanelSession 和已创建卡片
  -> 低优先级 GC + working set trim

应用退出
  -> TrayFlyoutService.Dispose()
```

这个策略的预期收益是关闭后后台常驻内存回落。风险是重开时可能更慢，或出现图片重载、短暂空白、直播区状态闪动。

## 3. 测量指标

每个场景至少记录：

| 指标 | 说明 |
| --- | --- |
| Private Working Set | 用户最容易感知的后台常驻内存；任务管理器常见口径 |
| Commit Size | 判断是否只是 working set 被修剪，还是实际提交内存也稳定 |
| Managed heap size | 可选；用来区分托管对象增长和 WinUI/图片/Composition 资源 |
| GDI objects / USER objects | 可选；检查窗口、菜单、图片或控件资源是否随开关增长 |
| 首次打开耗时 | 从点击托盘到主面板可见并有首帧内容 |
| 重开耗时 | 关闭后再次打开到主面板可见并有首帧内容 |
| 图片缓存数量 | `VideoCard` 静态缓存是否有边界且不随开关无限增长 |

不要只记录一次数字。每个状态等待 10 到 30 秒后再记一次稳定值。

## 4. 基线测量流程

### 4.1 当前关闭后重建策略

1. 启动应用，不点击托盘，等待 10 到 30 秒。
2. 记录后台常驻内存。
3. 点击托盘打开主 Flyout，等待关注页刷新和图片加载稳定。
4. 记录打开状态内存和首次打开耗时。
5. 切换到历史页，等待加载稳定。
6. 切换到稍后再看页，等待加载稳定。
7. 回到关注页，关闭 Flyout，等待 10 到 30 秒。
8. 记录关闭后内存。
9. 重复打开/关闭 10 次，每次关闭后等待稳定再记录。
10. 检查是否出现空白、图片重载、折叠区闪现、滚动位置异常或任务管理器分组异常。

### 4.2 对照保留策略

如需对照，可临时回退到旧保留策略进行测量。对照目标是验证收益，不追求完整产品化。

对照行为：

```text
Flyout 关闭
  -> 取消请求
  -> 保留 MainPanelControl、Frame 页面缓存、MainPanelSession、已创建卡片和图片缓存
```

对照策略需要记录同样的指标和流程。

## 5. 决策阈值

建议按下面规则判断：

| 结果 | 决策 |
| --- | --- |
| 关闭后 working set 降幅明显，commit 也稳定下降，重开体验可接受 | 做关闭后重建 |
| working set 下降主要来自 trim，commit 基本不变，重开还更慢 | 保留当前策略 |
| 重建后明显闪白或图片重载，但内存收益很大 | 保留有界图片缓存，只销毁 UI 树 |
| 连续开关后内存持续增长 | 优先查事件订阅、计时器、异步图片回调和静态缓存 |
| 当前保留策略内存已经稳定且后台值可接受 | 不做生命周期改造，直接进入 Phase 6 |

这里不要追求“最低内存”。BiliRadar 是托盘弹窗应用，目标是后台常驻内存明显合理，同时重开不破坏体验。

## 6. 可能的实现方向

如果测量支持关闭后重建，推荐实现为：

1. `TrayFlyoutService` 不在构造时永久固定一个 `MainPanelControl`。
2. 保留一个轻量 `MainWindowSnapshot?` 或扩展后的 `MainPanelSnapshot?`。
3. Flyout 打开前确保内容存在。
4. Flyout 关闭后导出 snapshot，Dispose 内容并置空。
5. 低优先级执行一次 GC 和 working set trim。
6. 图片缓存采用平衡策略：保留有界缓存，不在每次关闭时无条件清空。

不要把 `Page`、`VideoCard`、`ImageSource`、`BitmapImage` 或 `DispatcherQueue` 放入 snapshot。snapshot 只保存纯模型数据。

## 7. Phase 6 边界

以下问题留给 Phase 6：

- 三个纵向列表迁移到虚拟化 `ListView`。
- `VideoCard` 适配虚拟化复用。
- 删除手动 `Panel.Children` 渲染方法。
- 关注页直播横向区域改为 `ItemsRepeater`。

Phase 5 只决定 Flyout 内容生命周期。列表性能优化是另一个变量，不能混在同一轮测量里。

## 8. 建议记录模板

```text
日期：
构建：
配置：Debug x64 / Release x64 / MSIX
策略：保留 MainPanelControl / 关闭后重建

启动后未打开：
首次打开耗时：
关注页稳定：
历史页稳定：
稍后再看稳定：
关闭后 10s：
关闭后 30s：
连续开关 10 次后：

Private Working Set：
Commit Size：
Managed heap：
GDI objects：
USER objects：
图片缓存数量：

UI 观察：
结论：
下一步：
```
