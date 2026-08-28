# BiliRadar — WinUI 3 项目速览

## 项目概述

BiliRadar 是一个 Windows 桌面应用（WinUI 3 + Windows App SDK），提供 Bilibili 关注作者的视频更新追踪、历史记录和稍后再看管理。通过系统托盘图标驻留后台，点击弹出 Flyout 面板展示内容。

**技术栈**：C# / WinUI 3 / DesktopFlyouts.WinUI 1.4.0 / CommunityToolkit 8.2 / .NET 9

## 架构总览

项目已从旧 `MainWindow.xaml.cs` 单体窗口迁移到 DesktopFlyouts `SystemTrayIcon` + `DesktopFlyout` + 页面化架构。Flyout 内容关闭后重建，三页纵向视频列表使用虚拟化 `ListView`，关注页直播横向区域使用 `ItemsRepeater`。

```
App.xaml.cs                    ← 应用入口，初始化托盘、数据监控
├── TrayFlyoutService          ← DesktopFlyouts SystemTrayIcon + DesktopFlyout/DesktopMenuFlyout
│   └── MainPanelControl       ← Flyout 内容（420px 宽面板）
│       ├── SelectorBar        ← 关注/历史/稍后再看 三页切换
│       ├── Frame              ← 承载三个 Page（CacheSize=0, 切页释放旧页）
│       ├── ProgressBar        ← 顶部刷新进度条
│       └── ItemsControl       ← 底部 StatusNotification InfoBar 列表
├── BackgroundNotificationMonitor ← 后台轮询推送通知
└── SettingsWindow             ← 独立设置窗口（常规/通知/关于 三个页面）
```

### 页面结构

```
Pages/
├── FollowingPage       ← 直播 ItemsRepeater + 最新视频 ListView
├── HistoryPage         ← 历史记录 ListView + 空状态
├── ViewLaterPage       ← 稍后再看 ListView + 移除按钮 + 空状态
├── GeneralSettingsPage ← 通用设置（语言、开机自启等）
├── NotificationSettingsPage ← 通知设置
└── AboutSettingsPage   ← 关于页面

Controls/
├── VideoCard        ← 视频卡片控件：封面图、标题、描述、UP主头像、时长标签、
│                      追番/取消追番按钮。ViewLaterButtonMode 控制右侧按钮行为。
├── MainPanelControl ← Flyout 外壳：SelectorBar + Frame + ProgressBar + 通知区
└── (未来可能) LiveCreatorCard

Models/
├── BiliVideoUpdate / BiliVideoUpdatePage / BiliVideoHistoryPage / BiliViewLaterPage
├── VideoUpdateRow      ← UI 绑定的视频行（BiliVideoUpdate → VideoUpdateRow）
├── CreatorRow / LiveCreatorRow ← UP主 / 直播UP主 UI 行
├── BiliCreator / BiliLiveCreator / BiliAccountProfile
├── MainPanelSection    ← enum { Following, History, ViewLater }
├── MainWindowSnapshot  ← (Updates[], LiveCreators[], HistoryItems[], ViewLaterItems[]) — 传给 MainPanelControl
└── StatusNotification  ← InfoBar 消息
```

### 服务层

```
Services/
├── MainPanelSession     ← **核心**：数据容器 + 所有 API 调用
│   暴露 ObservableCollection<T>、刷新/加载更多/关注/取消关注/稍后再看等方法
│   通过事件 (UpdatesRefreshed, HistoryRefreshed, ViewLaterRefreshed,
│   FollowingListRefreshed, CollectionAdded, CollectionUpdated, StatusAdded)
│   通知 UI 更新
├── UpdateMonitorService ← 封装 IBiliDataProvider 的分页逻辑
├── BiliWebDataProvider  ← 实际 HTTP 请求（Bilibili API）
├── IBiliDataProvider    ← 数据提供者接口
├── MockBiliDataProvider ← 测试用 mock
├── AppSettings          ← ApplicationData.LocalSettings 持久化
├── CookieStore          ← Bilibili Cookie 管理
├── BiliAccountService / BiliKernelAuthService / NotificationService
└── TrayFlyoutService    ← DesktopFlyouts 托盘图标、主面板和右键菜单
```

## 关键设计决策

1. **纵向视频列表使用 ListView** — 关注、历史、稍后再看三页的视频卡片都由 `ObservableCollection<VideoUpdateRow>` 驱动虚拟化 `ListView`，不再手动维护 `Panel.Children`。
2. **直播横向区域使用 ItemsRepeater** — 关注页直播 UP 主区域使用官方 `ItemsRepeater + StackLayout(Orientation=Horizontal)`，外层仍由横向 `ScrollViewer` 承载。
3. **页面不缓存 UI 树** — `Frame.CacheSize=0`，三个 Page 不使用 `NavigationCacheMode="Required"`。切页前主动 Dispose 当前页，避免已访问页面的 UI 内存叠加。
4. **数据保留在 MainPanelSession** — 页面 UI 可销毁，`MainPanelSession` 持有 `Updates`、`HistoryItems`、`ViewLaterItems`、`LiveCreators` 等集合，切页后新页面重新绑定集合。
5. **VideoCard 适配虚拟化复用** — `Item` 是依赖属性，`Loaded` 幂等，`Unloaded` 释放当前图片引用，异步图片回写有版本校验；右键菜单通过 `CardMenuFlyoutFactory` 按当前 item 重建。
6. **切页后延迟资源回收** — 页面切换后延迟、低优先级执行 GC + working set trim，用于提前触发 WinUI/图片资源释放后的工作集回落。
7. **Flyout 内容按需创建** — `MainPanelControl` 在托盘左键打开时创建，Flyout 关闭后导出 `MainWindowSnapshot`、Dispose 面板并低优先级修剪 working set。
8. **右键菜单不提供“打开”** — 左键托盘图标负责打开/关闭主 Flyout；右键菜单只保留“设置”和“退出”。不要重新加入右键“打开”，之前尝试在 MenuFlyout 命令中主动 `ShowAt(...)` 会造成状态重入和卡死风险。
9. **无旧宿主窗口** — DesktopFlyouts 使用独立 XAML island 窗口；应用在 `OnLaunched` 中将 `DispatcherShutdownMode` 设为 `OnExplicitShutdown`，关闭最后一个设置/登录窗口后仍由托盘驻留，只有显式退出才停止 UI 消息循环。不要把设置移到 `App` 构造函数，`Application.Start` 会在随后覆盖它。不再创建透明锚点或隐藏 `TrayHostWindow`。

## 构建

```bash
dotnet build BiliRadar/BiliRadar.csproj -p:Platform=x64
```

Debug 配置：框架依赖（`WinUISDKReferences=true`）；Release：自包含 MSIX 包。

### Codex 环境中的 NuGet 还原

本机普通终端或 Visual Studio 能访问 NuGet，但 Codex 受限沙箱内的 Windows Schannel 可能报以下错误：

- `SEC_E_NO_CREDENTIALS (0x8009030e)` / “安全包中没有可用的凭据”
- `NU1301`，内层错误为 “The SSL connection could not be established”

这表示沙箱进程无法取得 TLS credential handle，不是 NuGet 用户名/密码错误，也不是项目包源配置错误。不要改用非 HTTPS 包源，也不要设置或保留 `disableTLSCertificateValidation="true"` 绕过证书校验。

正确做法是在沙箱外运行同一条还原命令；Codex 工具调用应使用 `sandbox_permissions: require_escalated`：

```powershell
dotnet restore BiliRadar/BiliRadar.csproj -p:Platform=x64
```

下载期间偶发 `unexpected EOF or 0 bytes` 通常是代理或 CDN 瞬时断流，先让 NuGet 自己重试。若持续发生，再降低并发：

```powershell
dotnet restore BiliRadar/BiliRadar.csproj -p:Platform=x64 --disable-parallel
```

还原成功后，编译时避免再次触发网络请求：

```powershell
dotnet build BiliRadar/BiliRadar.csproj -p:Platform=x64 --no-restore
```

项目根目录的 `NuGet.Config` 是本项目包源配置的唯一依据，当前通过 `<clear />` 后只启用官方 `https://api.nuget.org/v3/index.json`。若沙箱内失败而沙箱外访问该地址返回 HTTP 200，应直接按上述方式在沙箱外还原，不要继续修改项目配置。

## Windows App SDK 升级准入

不要只根据“能编译”决定是否升级。候选版本必须是 Stable 通道，并按以下顺序评测：

1. **依赖审计** — 用 `dotnet list BiliRadar/BiliRadar.csproj package --outdated` 确认候选版本；检查直接和传递依赖是否出现降级、冲突、弃用或安全警告，并确认 DesktopFlyouts 版本支持该 Windows App SDK。
2. **构建与打包矩阵** — 验证 Debug x64 框架依赖构建和 Release x64 自包含 MSIX。项目当前为 x64-only，不再构建或发布 ARM64。还原成功后构建统一使用 `--no-restore`。
3. **启动与生命周期** — 安装/注册并真实启动 MSIX，验证二次启动的单实例重定向、开机启动、通知激活、设置窗口和 WebView2 登录链路，不能只检查进程存在。
4. **托盘关键路径** — 验证托盘左键 Flyout、右键仅“设置/退出”、Explorer 重启后的图标恢复、切换显示器/DPI 后图标清晰，以及连续开关 Flyout 不出现重入或卡死。
5. **WinUI 关键路径** — 验证三页切换、`ListView`/`ItemsRepeater` 虚拟化与滚动加载、稍后再看移除、关注/取消关注、直播卡片操作；刷新或加载中关闭 Flyout 时，取消请求不能产生错误 InfoBar。
6. **升级门槛** — 构建/打包零错误，关键路径无功能回归，事件日志无新的应用崩溃。性能数字不作为当前托盘迁移门槛；若出现肉眼可见卡顿、泄漏或稳定性问题再单独处理。

评测时应单独建分支，只修改 Windows App SDK 版本，先验证默认行为；不要一开始同时启用新的 XAML 可选行为或 `XamlChangeId`，这些应作为第二阶段独立基准测试。

## 当前迁移状态

### DesktopFlyouts 迁移

| 阶段 | 状态 |
|------|------|
| Phase 0 (分支与构建基线) | ✅ |
| Phase 1 (26100 TFM、依赖和 x64 打包准入) | ✅ |
| Phase 2 (SystemTrayIcon + DesktopFlyout + DesktopMenuFlyout) | ✅ 已人工验收 |
| Phase 3 (旧路径清理与 WinUIEx 依赖移除) | ✅ |
| Phase 4 (状态机加固与交付收口) | ✅ |
| Phase 5 (Shell 环境适配核查) | ✅ |
| Phase 6 (移除 WinUIEx 和旧宿主) | 🧪 自动验证通过，待人工生命周期/交互验收 |

### 旧 WinUIEx 架构迁移（历史）

| 阶段 | 状态 |
|------|------|
| Phase 1 (WinUIEx 原型) | ✅ |
| Phase 2a-2f (页面提取) | ✅ |
| Phase 2g (Flyout 集成) | ✅ 已手动验证 |
| Phase 3 (右键菜单迁移) | ✅ 新路径原生 MenuFlyout，仅保留设置/退出 |
| Phase 4 (清理旧代码) | ✅ 删除 MainWindow、TrayIconService、SystemTray/，#if 双路径已移除 |
| Phase 5 (内存测试) | ✅ 已接入关闭后重建面板、切页 Dispose 和延迟 working set trim；仍建议记录最终数值 |
| Phase 6 (ListView / ItemsRepeater 迁移) | ✅ 三个纵向列表已改 ListView，直播横向区已改 ItemsRepeater |

DesktopFlyouts 迁移详见 `docs/design/desktopflyouts-migration.md`；旧架构记录见 `docs/design/tray-flyout-migration.md`。

## 后续迁移约束

1. **右键菜单范围** — 右键菜单只做设置和退出。左键托盘图标是唯一主面板入口，不要重新加入右键“打开”；之前尝试在 `MenuFlyout` 命令中主动打开主 Flyout，出现过状态重入和卡死。
2. **不恢复旧宿主** — DesktopFlyouts 自己创建 XAML island 窗口，应用通过 `DispatcherShutdownMode.OnExplicitShutdown` 维持无窗口时的托盘生命周期；不要重新添加 `TrayHostWindow`、透明锚点、原生 `Flyout.ShowAt(...)` 或 WinUIEx 托盘路径。
3. **Session 生命周期** — Flyout 关闭后 Dispose `MainPanelControl` 和 `MainPanelSession`，并用纯数据 `MainWindowSnapshot` 支撑下次重建。切页只 Dispose 当前 Page，`MainPanelSession` 在当前 Flyout 会话内保留。
4. **页面缓存约束** — 当前以内存优先，`Frame.CacheSize=0`。不要重新启用 `NavigationCacheMode="Required"`，否则三页 UI 稳定内存会再次叠加。
5. **取消请求链路** — 页面刷新和加载更多继续通过 `CancellationToken` 传到 `MainPanelSession`/`UpdateMonitorService`。Flyout 关闭触发取消时，不应显示错误 InfoBar。

## 建议下一步

1. 在 Phase 6 最终构建上验证无隐藏宿主时进程可持续驻留。
2. 验证托盘左键主面板、右键仅“设置/退出”、关闭设置/登录窗口后继续驻留，以及右键“退出”能完整结束进程。

## 本地化

资源文件在 `Strings/zh-CN/Resources.resw` 和 `zh-HK/Resources.resw`。通过 `LocalizationHelper.GetString(key)` 获取。

## Git 分支

当前迁移分支 `codex/desktopflyouts-migration`，主分支 `main`。
