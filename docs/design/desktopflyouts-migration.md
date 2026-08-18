# BiliRadar 托盘迁移：DesktopFlyouts.WinUI

> 分支：`codex/desktopflyouts-migration`  
> 状态：Phase 0–1 完成
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
