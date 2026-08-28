# BiliRadar 托盘迁移：DesktopFlyouts.WinUI

> 分支：`codex/desktopflyouts-migration`  
> 状态：Phase 0–5 完成；Phase 6 实现与自动验证完成，待人工生命周期/交互验收
> 最近检查：2026-08-28
> 参考项目：[0x5bfa/DesktopFlyouts](https://github.com/0x5bfa/DesktopFlyouts)

## 目标与优先级

迁移目标是用 `DesktopFlyouts.WinUI` 重写 BiliRadar 的托盘主面板宿主，在不改变现有业务页面和 `MainPanelSession` 职责的前提下，优先改善面板的外观、系统融合度、弹出动画和交互质感。

本轮明确采用**美观优先**的验收口径。性能基线、工作集数据和内存回落不再作为迁移门槛；只有明显影响日常使用的卡顿、泄漏或稳定性问题才在后续阶段处理。

## Phase 0：迁移基线

状态：实现与自动验证完成，待人工生命周期/交互验收。

- 从 `main` 创建独立分支 `codex/desktopflyouts-migration`。
- 改动前的 Debug x64 构建通过，0 warning / 0 error。
- 保留现有 WinUIEx 托盘实现作为迁移期间的可运行路径；Phase 0–1 不切换托盘运行时。
- 不采集性能基线。

## Phase 1：平台与依赖准入

### 已落地改动

- 项目目标框架从 `net9.0-windows10.0.22621.0` 提升为 `net9.0-windows10.0.26100.0`。
- 两个 x64 发布配置同步使用 26100 TFM，避免发布配置覆盖项目目标框架。
- 项目、发布脚本和 CI 发布矩阵统一收敛为 x64-only；不再生成 ARM64 包。
- `TargetPlatformMinVersion` 继续保持 `10.0.17763.0`，没有把最低运行系统提升到 Windows 11 24H2。
- 引入 `DesktopFlyouts.WinUI` 1.4.0。
- Windows App SDK 继续使用 2.4.0；本阶段没有混入其他 SDK 升级或 XAML 行为变更。

### 验证矩阵

| 项目 | 结果 | 说明 |
|---|---|---|
| NuGet restore（x64） | 通过 | `DesktopFlyouts.WinUI` 1.4.0 正常解析 |
| 依赖过期检查 | 通过 | 没有可用更新 |
| 已知漏洞检查（含传递依赖） | 通过 | 没有已知漏洞 |
| Debug x64 build | 通过 | 0 warning / 0 error |
| Release x64 build | 通过 | 0 warning / 0 error |
| Release x64 MSIX | 通过 | 已生成并验证签名 |
| 隔离包安装与真实启动 | 通过 | 进程保持运行，隐藏宿主无可见主窗口，符合现有托盘生命周期 |

x64 打包时另有 `mspdbcmf.exe` 缺失警告，只影响符号包生成，不影响已签名 MSIX、本体安装或启动，不作为阻断项。

### 架构范围

`DesktopFlyouts.WinUI` 1.2.0、1.3.0 和 1.4.0 的官方包均包含 AMD64 二进制。BiliRadar 本次迁移明确采用 x64-only 产品范围，因此 ARM64 不再属于构建、打包或发布验收项，也不阻塞后续迁移。

## Phase 2 入口条件

- 保持右键菜单仅包含“设置”和“退出”，不恢复右键“打开”。
- 保持 `TrayHostWindow` 隐藏，以及现有 `MainPanelSession` / snapshot / Dispose 生命周期约束。
- 视觉验收优先关注：圆角与阴影、系统材质、托盘边缘定位、多显示器/DPI、打开/关闭动画、light-dismiss、暗色/亮色主题和内容布局协调性。

## Phase 2：托盘宿主迁移

状态：完成。

### 落地范围

- 托盘图标从 WinUIEx `TrayIcon` 迁移到 DesktopFlyouts `SystemTrayIcon`，使用稳定 GUID，保留随系统主题切换深浅色图标以及 Explorer 重启后的图标恢复能力。
- 主面板从 WinUI 原生 `Flyout` + 透明锚点窗口迁移到 `DesktopFlyout` + `DesktopFlyoutIsland`。
- 右键菜单迁移到 `DesktopMenuFlyout`，仍然只包含“设置”和“退出”，并补充 Fluent 图标。
- `TrayHostWindow` 继续隐藏并维持 WinUI Dispatcher/应用生命周期，但不再承载 Flyout 锚点、定位或激活职责。

### 视觉与交互选择

- 主面板使用 Desktop Acrylic、系统主题阴影、系统 Overlay 圆角和 1px 主题描边。
- 使用 DesktopFlyouts 自带的垂直方向弹出/关闭 transition；底部任务栏向上弹出，顶部任务栏向下弹出。
- 禁用旧 `MainPanelControl` 的整面板位移动画，避免与宿主动画叠加。
- 保持 `HideOnLostFocus=true` 和可激活模式，支持 light-dismiss、键盘焦点以及面板内控件交互。
- 不启用整面板按压缩放和纵向 swipe-to-dismiss，避免与视频列表滚动手势冲突。
- 每次打开根据托盘图标所在显示器的工作区与有效 DPI 计算面板高度；宽度保持 420 DIPs。

### 生命周期适配

DesktopFlyouts 1.4.0 没有 `Opened` / `Closed` 事件，`IsOpen` 在动画完成后变化。BiliRadar 对 `DesktopFlyout.IsOpenProperty` 注册属性回调：

- `false -> true`：调用 `MainPanelControl.OnFlyoutOpened(false)`，开始当前页面激活和刷新。
- `true -> false`：取消页面请求、导出 `MainWindowSnapshot`、Dispose 面板和 Session，并清空 island 内容。
- 下次打开按 snapshot 重建 `MainPanelControl`，继续遵守现有页面不缓存和 Session 会话边界。

托盘重复点击在打开 transition 期间会被合并；关闭后保留短暂的 light-dismiss 防重开窗口，防止同一次托盘点击先触发失焦关闭又立即重新打开。

### Phase 2 验证

- Debug x64：通过，0 warning / 0 error。
- Release x64 自包含 MSIX：生成成功并通过签名验证。
- 隔离测试包：`JordanQD.BiliRadar.DesktopFlyoutsPhase2` 安装成功。
- 真实打包启动：进程持续运行且响应正常，隐藏宿主保持 `MainWindowHandle=0`，应用事件日志无新增启动崩溃。
- 人工视觉/交互验收：2026-08-18 通过；主面板、重复开关、右键菜单及三页基本交互无阻断问题。
- `mspdbcmf.exe` 缺失仍只影响符号包生成，不影响 MSIX 本体。

### 恢复工作检查点

Phase 2 结束后暂停，不进入后续迁移。恢复时从 `codex/desktopflyouts-migration` 分支继续，先完成以下人工视觉验收，再开始 Phase 3：

1. 左键托盘图标打开/关闭主面板，确认 Acrylic、圆角、阴影和弹出方向符合预期。
2. 点击面板外部关闭，再快速点击托盘图标，确认不会闪退、重入或立刻重新打开。
3. 右键菜单确认只有“设置”和“退出”，并验证两个命令。
4. 在浅色/深色主题、不同 DPI 或第二显示器上各打开一次。

用于恢复验证的隔离包已安装，包名为 `JordanQD.BiliRadar.DesktopFlyoutsPhase2`；测试进程在本阶段结束时关闭，不占用关机前的后台会话。

## Phase 3：旧路径清理与依赖收口

状态：完成；自动验证通过，并于 2026-08-27 进入 Phase 4 收口。

### 清理范围

- 删除 `MainPanelControl` 中已经不再调用的整面板打开/关闭位移动画；状态通知自身的进入/退出动画继续保留。
- 将 `TrayHostWindow` 收敛为只负责 Dispatcher 和应用生命周期的 1×1 隐藏窗口，不再保留 Flyout 锚点、托盘消息监视或 WinUIEx 扩展。
- 使用最小 Win32 extended style + layered alpha 隐藏宿主窗口，移除 `WinUIEx` 2.9.3 依赖。
- 删除显示配置变化后的旧托盘图标重建计时器。DesktopFlyouts 负责 Flyout 的工作区/DPI 定位，主题变化仍通过 `UISettings.ColorValuesChanged` 刷新深浅色托盘图标。

### Phase 3 验收边界

Codex 负责依赖审计、Debug/Release 构建、MSIX 打包、签名、安装和启动检查。主面板外观、动画感受、托盘点击、右键菜单和页面交互由用户人工验收；Codex 不代替用户操作界面。

### Phase 3 自动验证

- `WinUIEx` 已从项目引用和解析后的依赖图中移除。
- Debug x64：通过，0 warning / 0 error。
- Release x64 自包含 MSIX：生成成功并通过签名验证；仅保留不影响本体的 `mspdbcmf.exe` 符号包警告。
- 隔离测试包 `JordanQD.BiliRadar.DesktopFlyoutsPhase3`：安装与真实启动通过；进程响应正常，隐藏宿主保持 `MainWindowHandle=0`，应用事件日志无新增启动崩溃。

## Phase 4：状态机加固与交付收口

状态：实现和自动验证完成；用户于 2026-08-28 确认手动运行正常，等待交互验收。

### 实施范围

- 串行化主 `DesktopFlyout` 与右键 `DesktopMenuFlyout`：如果右键发生在主面板打开或关闭 transition 期间，记录最新菜单位置，等待主面板完整关闭后再显示菜单，避免两个独立 host window 重叠。
- 恢复切页释放约束：当前页面在导航前 `Dispose`，再从会话内页面集合移除；继续保持 `Frame.CacheSize=0`。
- 三个页面的异步命令捕获当前 `MainPanelSession`，在 `await` 返回后校验页面/会话仍有效；关闭 Flyout 或切页导致失效时静默停止后续 UI 与 InfoBar 回写。
- 将解决方案、README、About credit 和代理上下文收敛到 DesktopFlyouts + x64-only 现状；旧 WinUIEx 设计文档保留为历史归档。
- 自动验证 Debug/Release x64、依赖图、MSIX 签名、隔离安装、真实启动和应用事件日志。

### Phase 4 验收边界

Codex 负责代码、依赖、构建、打包、安装、启动和日志验证。以下视觉/交互项目仍由用户人工验收：

1. 主面板打开/关闭 transition 中快速交错左键和右键，确认不会同时出现主面板与菜单。
2. 三页来回切换、滚动加载、稍后再看移除、关注/取消关注和直播卡片操作。
3. 浅色/深色主题、不同 DPI、第二显示器和 Explorer 重启后的托盘表现。

### Phase 4 自动验证

- Debug x64 项目与解决方案严格构建：通过，0 warning / 0 error。
- Release x64 自包含 MSIX：生成成功；仍只有不影响本体的 `mspdbcmf.exe` 符号包警告。
- 依赖检查：`DesktopFlyouts.WinUI` 保持 1.4.0，直接和传递依赖没有已知漏洞。`Microsoft.Windows.SDK.BuildTools` 有较新修订版，但不属于本阶段且没有安全告警，因此未混入升级。
- 修正生成 manifest 后处理目标，使开发包不再错误沿用正式 `JordanQD.BiliRadar` Identity；支持通过 `DevelopmentPackageIdentityName` 生成隔离测试包。
- 隔离包 Identity：`JordanQD.BiliRadar.DesktopFlyoutsPhase4`，x64，`Windows.FullTrustApplication`；包内包含 `BiliRadar.exe`、`DesktopFlyouts.Wasdk.dll` 和 `AppxSignature.p7x`。
- 隔离 MSIX SHA-256：`380DD3CC42CDAD1EC74362A3093A2269C207508352F04BAF8BAFBAE9057B16C3`。
- 当前机器尚未由 Codex 导入项目测试证书 `64B1137BFCCF4F831CE518E83C1E885401B35218`；证书信任边界保持不变。用户已确认手动运行正常，隔离 MSIX 的自动安装仍未执行。

## Phase 5：Shell 环境适配核查

状态：完成；只做核查，没有代码改动。

### 人工验证结果

用户确认深浅色托盘图标切换、显示器 DPI 变化和 Explorer 重启后的图标恢复均能正确响应，日常功能行为正常。

### 任务栏边缘适配结论

- DesktopFlyouts 1.4.0 使用 `SHAppBarMessage(ABM_GETTASKBARPOS)` 识别主任务栏的左、上、右、下四个边缘。
- `SystemTrayIcon` 将通知图标矩形中心作为物理屏幕坐标传给 `DesktopFlyout.Show(Point)`；Flyout 按该点选择显示器的 `rcWork`，因此会避开任务栏占用区域并适配对应显示器。
- 左/右任务栏时，面板贴工作区左/右边缘并以托盘图标为纵向中心；上/下任务栏时，面板贴工作区上/下边缘并以托盘图标为横向中心。最终矩形还会统一 clamp 到工作区内。
- BiliRadar 当前使用 `PopupDirection=Vertical`：上方任务栏向下动画、下方任务栏向上动画；左/右任务栏的定位正确，但动画仍是上下方向，不会从侧边水平滑入。
- 库对点击点所在显示器的工作区有适配，但 `ABM_GETTASKBARPOS` 只提供主任务栏信息；第三方 Shell 提供的副任务栏边缘不属于可靠保证范围。

本项目的托盘图标通常位于主任务栏，现有 `Show(args.Point)` 用法可以直接利用库的四边定位能力。按照本阶段范围，不为左右边缘动画或第三方副任务栏增加应用层补丁。

## Phase 6：移除 WinUIEx 和旧宿主

状态：完成。

### 清理结果

- 删除 `TrayHostWindow.cs`，应用启动时不再创建、激活或隐藏 1×1 WinUI 窗口。
- `App.xaml.cs` 直接使用 UI 线程的 `DispatcherQueue` 初始化托盘服务；`TrayFlyoutService` 只接收该队列，不再依赖任何宿主 `Window`。
- 运行时代码中不再存在 WinUI 原生 `Flyout.ShowAt(...)` 锚点、WinUIEx `TrayIcon`、`WindowMessageMonitor`、透明锚点或条件编译双路径。
- `WinUIEx` 已从直接引用和解析后的依赖图中移除；DesktopFlyouts 是唯一托盘/Flyout 实现。
- 旧整面板动画状态机已删除。保留 `_isMainFlyoutShowPending`、关闭后短时防重开和延迟右键菜单逻辑，因为它们处理的是 DesktopFlyouts transition 期间的当前重入问题，而非旧 WinUIEx 状态。
- 右键菜单仍只有“设置”和“退出”；左键托盘图标仍是主面板唯一入口。

### Phase 6 自动验证

- Debug x64 严格构建：通过，0 warning / 0 error。
- Release x64 严格编译：通过，0 warning / 0 error。
- Release x64 自包含 MSIX bundle：生成成功；仍只有不影响本体的 `mspdbcmf.exe` 符号包警告。
- 隔离包 Identity：`JordanQD.BiliRadar.DesktopFlyoutsPhase6`，x64，`Windows.FullTrustApplication`；包内包含 `BiliRadar.exe`、`DesktopFlyouts.Wasdk.dll` 和内外层签名。
- 隔离 MSIX bundle SHA-256：`DEDCC45525C2FAD9B56DF684B92CB49DCDA4F11AE62DAD77397F9974270D799C`。
- 直接和传递依赖中无 `WinUIEx`；运行时代码中无旧宿主、旧锚点、`WindowMessageMonitor` 或条件编译双路径。

### Phase 6 验收边界

Codex 负责代码残留检查、依赖检查和 x64 构建/打包验证。无隐藏宿主时的后台驻留、托盘左右键、设置/退出和视觉交互由用户在最终构建上人工验收。
