# BiliRadar — WinUI 3 项目速览

## 项目概述

BiliRadar 是一个 Windows 桌面应用（WinUI 3 + Windows App SDK），提供 Bilibili 关注作者的视频更新追踪、历史记录和稍后再看管理。通过系统托盘图标驻留后台，点击弹出 Flyout 面板展示内容。

**技术栈**：C# / WinUI 3 / WinUIEx 2.9.0 / CommunityToolkit 8.2 / .NET 9

## 架构总览

项目已从旧 `MainWindow.xaml.cs` 单体窗口迁移到 WinUIEx `TrayIcon` + 原生 Flyout + 页面化架构。Phase 5/6 已进入内存和列表性能优化阶段：Flyout 内容关闭后重建，三页纵向视频列表已迁移到虚拟化 `ListView`，关注页直播横向区域已迁移到 `ItemsRepeater`。

```
App.xaml.cs                    ← 应用入口，初始化托盘、数据监控
├── TrayHostWindow             ← 空窗口，承载 TrayIcon 生命周期
├── TrayFlyoutService          ← WinUIEx TrayIcon + Flyout/MenuFlyout 管理
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
└── TrayFlyoutService    ← WinUIEx TrayIcon + Flyout
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
9. **隐藏 TrayHostWindow** — 新 WinUIEx 路径也使用 `TrayHostWindow.InitializeHidden()`，不要再用 `InitializeVisible()`。启动后应保持后台进程分组，不应因为宿主窗口可见而出现在任务管理器“应用”分组。

## 构建

```bash
dotnet build BiliRadar/BiliRadar.csproj -p:Platform=x64
```

Debug 配置：框架依赖（`WinUISDKReferences=true`）；Release：自包含 MSIX 包。

## 当前迁移状态

| 阶段 | 状态 |
|------|------|
| Phase 1 (WinUIEx 原型) | ✅ |
| Phase 2a-2f (页面提取) | ✅ |
| Phase 2g (Flyout 集成) | ✅ 已手动验证 |
| Phase 3 (右键菜单迁移) | ✅ 新路径原生 MenuFlyout，仅保留设置/退出 |
| Phase 4 (清理旧代码) | ✅ 删除 MainWindow、TrayIconService、SystemTray/，#if 双路径已移除 |
| Phase 5 (内存测试) | ✅ 已接入关闭后重建面板、切页 Dispose 和延迟 working set trim；仍建议记录最终数值 |
| Phase 6 (ListView / ItemsRepeater 迁移) | ✅ 三个纵向列表已改 ListView，直播横向区已改 ItemsRepeater |

详见 `docs/design/tray-flyout-migration.md` 和 `C:\Users\Q\.Codex\plans\radiant-bouncing-quiche.md`。

## 后续迁移约束

1. **右键菜单范围** — 右键菜单只做设置和退出。左键托盘图标是唯一主面板入口，不要重新加入右键“打开”；之前尝试在 `MenuFlyout` 命令中主动打开主 Flyout，出现过状态重入和卡死。
2. **隐藏宿主窗口** — 新 WinUIEx 路径使用 `TrayHostWindow.InitializeHidden()`。启动后应保持后台进程分组；主 Flyout 打开期间 WinUIEx/WinUI 可能仍会让进程进入任务管理器“应用”分组，目前接受这个行为，不要为此手写枚举窗口或改 Win32 样式。
3. **Session 生命周期** — Flyout 关闭后 Dispose `MainPanelControl` 和 `MainPanelSession`，并用纯数据 `MainWindowSnapshot` 支撑下次重建。切页只 Dispose 当前 Page，`MainPanelSession` 在当前 Flyout 会话内保留。
4. **页面缓存约束** — 当前以内存优先，`Frame.CacheSize=0`。不要重新启用 `NavigationCacheMode="Required"`，否则三页 UI 稳定内存会再次叠加。
5. **取消请求链路** — 页面刷新和加载更多继续通过 `CancellationToken` 传到 `MainPanelSession`/`UpdateMonitorService`。Flyout 关闭触发取消时，不应显示错误 InfoBar。

## 建议下一步

1. 完整手测三页切换、滚动加载、稍后再看移除、关注/取消关注、直播卡片点击和右键菜单。
2. 记录最终内存数据：后台未打开、三页分别加载稳定、连续切页后 2-3 秒、Flyout 关闭后 10-30 秒。

## 本地化

资源文件在 `Strings/zh-CN/Resources.resw` 和 `zh-HK/Resources.resw`。通过 `LocalizationHelper.GetString(key)` 获取。

## Git 分支

当前分支 `architecture-update`，主分支 `main`。
