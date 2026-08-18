# BiliRadar 托盘迁移：DesktopFlyouts.WinUI

> 分支：`codex/desktopflyouts-migration`  
> 状态：Phase 0–2 完成
> 最近检查：2026-08-18  
> 参考项目：[0x5bfa/DesktopFlyouts](https://github.com/0x5bfa/DesktopFlyouts)

## 目标与优先级

迁移目标是用 `DesktopFlyouts.WinUI` 重写 BiliRadar 的托盘主面板宿主，在不改变现有业务页面和 `MainPanelSession` 职责的前提下，优先改善面板的外观、系统融合度、弹出动画和交互质感。

本轮明确采用**美观优先**的验收口径。性能基线、工作集数据和内存回落不再作为迁移门槛；只有明显影响日常使用的卡顿、泄漏或稳定性问题才在后续阶段处理。

## Phase 0：迁移基线

状态：完成。

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
- `mspdbcmf.exe` 缺失仍只影响符号包生成，不影响 MSIX 本体。

### 恢复工作检查点

Phase 2 结束后暂停，不进入后续迁移。恢复时从 `codex/desktopflyouts-migration` 分支继续，先完成以下人工视觉验收，再开始 Phase 3：

1. 左键托盘图标打开/关闭主面板，确认 Acrylic、圆角、阴影和弹出方向符合预期。
2. 点击面板外部关闭，再快速点击托盘图标，确认不会闪退、重入或立刻重新打开。
3. 右键菜单确认只有“设置”和“退出”，并验证两个命令。
4. 在浅色/深色主题、不同 DPI 或第二显示器上各打开一次。

用于恢复验证的隔离包已安装，包名为 `JordanQD.BiliRadar.DesktopFlyoutsPhase2`；测试进程在本阶段结束时关闭，不占用关机前的后台会话。
