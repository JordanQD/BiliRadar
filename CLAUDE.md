# BiliRadar — WinUI 3 项目速览

## 项目概述

BiliRadar 是一个 Windows 桌面应用（WinUI 3 + Windows App SDK），提供 Bilibili 关注作者的视频更新追踪、历史记录和稍后再看管理。通过系统托盘图标驻留后台，点击弹出 Flyout 面板展示内容。

**技术栈**：C# / WinUI 3 / WinUIEx 2.9.0 / CommunityToolkit 8.2 / .NET 9

## 架构总览

项目正从 `MainWindow.xaml.cs`（~2500 行单体）向 WinUIEx `TrayIcon` + 页面化架构迁移。当前处在 Phase 2f 完成、计划 Phase 2g 的状态。

```
App.xaml.cs                    ← 应用入口，初始化托盘、数据监控
├── TrayHostWindow             ← 空窗口，承载 TrayIcon 生命周期
├── TrayFlyoutService          ← WinUIEx TrayIcon + Flyout/MenuFlyout 管理
│   └── MainPanelControl       ← Flyout 内容（420px 宽面板）
│       ├── SelectorBar        ← 关注/历史/稍后再看 三页切换
│       ├── Frame              ← 承载三个 Page（CacheSize=3, SlideNavigation）
│       ├── ProgressBar        ← 顶部刷新进度条
│       └── ItemsControl       ← 底部 StatusNotification InfoBar 列表
├── BackgroundNotificationMonitor ← 后台轮询推送通知
└── SettingsWindow             ← 独立设置窗口（常规/通知/关于 三个页面）
```

### 页面结构

```
Pages/
├── FollowingPage       ← 直播区域 + 视频卡片列表（最复杂页面）
├── HistoryPage         ← 历史记录卡片列表 + 空状态
├── ViewLaterPage       ← 稍后再看卡片列表 + 移除按钮 + 空状态
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
├── MainWindowSnapshot  ← (Updates[], LiveCreators[]) — 传给 MainPanelControl
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
├── TrayIconService      ← 旧架构托盘服务（将 Phase 4 删除）
└── TrayFlyoutService    ← 新架构 WinUIEx TrayIcon + Flyout
```

### SystemTray/ 旧基础设施（Phase 4 计划删除）

```
SystemTray/Core/SystemTrayManager.cs, WindowHelper.cs
SystemTray/UI/SystemTrayIcon.cs, SystemTrayContextMenuWindow.cs
SystemTray/Models/SystemTrayEventArgs.cs, SystemTrayContextMenuItem.cs
SystemTray/Interfaces/IIconFile.cs
```

当前通过 `#if USE_WINUIEX_FLYOUT` 编译符号切换新旧托盘路径（Debug 模式默认启用新路径）。

## 关键设计决策

1. **不使用 ListView** — 目前所有卡片通过代码手动创建 `VideoCard` 并添加到 `StackPanel`（Phase 6 才迁移到 `ListView` 数据绑定）
2. **不使用 MVVM 绑定数据** — 页面通过 `MainPanelSession` 的事件（而非绑定）触发重新渲染。社区工具包 MVVM 虽然安装了，但未在核心面板中使用
3. **NavigationCacheMode="Required"** — 三个面板 Page 都缓存，切换页面不重新创建
4. **VideoCard 是"智能控件"** — 它自己处理图片加载、缓存、主题切换，页面只负责创建和事件响应
5. **CommunityToolkit.WinUI.Animations** — 用于页面切换、直播区域展开/收起、状态通知进出动画
6. **WinUIEx 2.9.0** — 提供 `TrayIcon`（系统托盘）和 Flyout 的自定义定位
7. **Flyout 内容生命周期** — `MainPanelControl` 在 `TrayFlyoutService` 构造时创建，Flyout 关闭时不被销毁（内存策略待 Phase 5 测试决定）

## 构建

```bash
dotnet build BiliRadar/BiliRadar.csproj -p:Platform=x64
```

Debug 配置：框架依赖（`WinUISDKReferences=true`）；Release：自包含 MSIX 包。
`USE_WINUIEX_FLYOUT` 编译符号在 Debug 下默认定义。

## 当前迁移状态

| 阶段 | 状态 |
|------|------|
| Phase 1 (WinUIEx 原型) | ✅ |
| Phase 2a-2f (页面提取) | ✅ |
| Phase 2g (Flyout 集成) | 下一步 |
| Phase 3 (右键菜单迁移) | 待做 |
| Phase 4 (清理旧代码) | 待做 |
| Phase 5 (内存测试) | 待做 |
| Phase 6 (ListView 迁移) | 待做 |

详见 `docs/design/tray-flyout-migration.md` 和 `C:\Users\Q\.claude\plans\radiant-bouncing-quiche.md`。

## 本地化

资源文件在 `Strings/zh-CN/Resources.resw` 和 `zh-HK/Resources.resw`。通过 `LocalizationHelper.GetString(key)` 获取。

## Git 分支

当前分支 `architecture-update`，主分支 `main`。
