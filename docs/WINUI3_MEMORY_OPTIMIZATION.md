# WinUI 3 托盘弹窗内存优化复盘

本文记录一次针对 WinUI 3 + C# 托盘弹窗应用的内存优化思路。目标场景是：应用长期驻留后台，窗口只在点击托盘图标时短暂显示，关闭或失焦后应尽量回到低内存占用状态。

这份文档不依赖某一次具体代码结构，适合作为以后重构或新项目的优化清单。

## 背景问题

原始现象大致是：

- 应用刚启动但未打开窗口时，后台常驻内存偏高。
- 打开弹窗后，占用进一步升高。
- 弹窗失焦关闭后，内存只下降一小部分，远高于“刚启动且未唤醒窗口”的状态。
- 弹窗重新打开时，如果完全销毁 UI，容易出现短暂空白、卡片重新加载、窗口高度跳动、折叠区闪现等体验问题。

WinUI 3 的窗口、XAML 视觉树、图片资源、Composition 资源和 Web/网络数据模型一旦创建，通常不会在窗口隐藏时立刻释放。仅仅 `Hide()` 或设置 `Visibility`，一般只能让窗口不可见，不能显著降低工作集。

## 核心原则

这次优化的核心不是“把窗口藏起来”，而是把应用拆成两层：

1. **后台常驻层**：尽量轻，只保留托盘图标、定时任务、通知监控、纯数据缓存。
2. **前台弹窗层**：首次点击托盘时才创建；失焦关闭后可以销毁窗口和页面 UI。

这样可以让后台常驻成本接近一个小型 tray app，而不是一个已经完整初始化的 WinUI 主窗口。

## 优化策略

### 1. 主窗口延迟创建

启动时不要立刻 `new MainWindow()`。

推荐做法：

- `App.OnLaunched` 中只初始化托盘宿主、后台服务和必要配置。
- 用户第一次点击托盘图标时，再创建 `MainWindow`。
- 窗口关闭后，将 `App` 中保存的主窗口引用置空。

伪代码：

```csharp
private MainWindow? _mainWindow;
private TrayHostWindow? _trayHostWindow;

protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    _trayHostWindow = new TrayHostWindow();
    _trayHostWindow.InitializeTray();

    _backgroundMonitor = new BackgroundNotificationMonitor();
    _backgroundMonitor.Start();
}

public void ShowMainWindow()
{
    _mainWindow ??= new MainWindow(_lastSnapshot);
    _mainWindow.Activate();
}
```

收益：

- 未打开窗口时，不创建完整 XAML 视觉树。
- 避免图片控件、卡片控件、页面资源、动画资源在启动阶段占用内存。

### 2. 用轻量托盘宿主替代完整主窗口

WinUI 3 应用通常需要窗口句柄承载托盘、消息分发或 Win32 interop。不要让完整主窗口承担这个职责。

推荐做法：

- 新增一个极小的 `TrayHostWindow`。
- 该窗口只用于托盘图标、消息循环、右键菜单宿主等。
- 主 UI 弹窗与托盘宿主分离。

要点：

- 托盘右键菜单也可以延迟创建，而不是启动时就构建完整菜单窗口。
- 托盘图标、菜单窗口、事件订阅都需要明确释放。

### 3. 关闭弹窗时销毁窗口，而不是只隐藏

如果目标是明显降低内存，`Hide()` 往往不够。

推荐流程：

1. 弹窗失焦或用户关闭时，先导出纯数据快照。
2. 停止定时器、取消事件订阅。
3. 释放页面控件持有的图片、集合、服务。
4. 清空窗口内容。
5. `Close()` 窗口。
6. 将主窗口引用置为 `null`。
7. 低优先级触发一次资源回收和工作集修剪。

伪代码：

```csharp
public void HideMainWindow()
{
    if (_mainWindow is null)
    {
        return;
    }

    _lastSnapshot = _mainWindow.ExportSnapshot();

    var window = _mainWindow;
    _mainWindow = null;

    window.DisposeAndClose();
    CollectReleasedWindowResources();
}
```

页面释放伪代码：

```csharp
public void DisposeAndClose()
{
    StopTimers();
    UnsubscribeEvents();

    foreach (var card in VideoCardsPanel.Children.OfType<IDisposable>())
    {
        card.Dispose();
    }

    VideoCardsPanel.Children.Clear();
    LiveCreatorCardsPanel.Children.Clear();
    StatusNotifications.Clear();

    _updateMonitorService?.Dispose();
    _updateMonitorService = null;

    Content = null;
    Close();
}
```

注意：

- 销毁窗口比隐藏窗口更“猛”，但对于短暂弹窗类应用是合理的。
- 如果窗口打开频率很高，可以改成延迟销毁，例如关闭后 10 到 30 秒内重新打开则复用，超时再销毁。

### 4. 显式回收和修剪工作集

.NET GC 回收托管对象，但任务管理器看到的工作集不一定马上下降。WinUI、图片和 Composition 资源也可能延迟释放。

在窗口销毁后，可以做一次保守的资源回收：

```csharp
private static void CollectReleasedWindowResources()
{
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();

    using var process = Process.GetCurrentProcess();
    SetProcessWorkingSetSize(process.Handle, -1, -1);
}
```

其中 `SetProcessWorkingSetSize` 需要 P/Invoke：

```csharp
[DllImport("kernel32.dll")]
private static extern bool SetProcessWorkingSetSize(
    IntPtr process,
    int minimumWorkingSetSize,
    int maximumWorkingSetSize);
```

使用建议：

- 不要在高频路径上调用。
- 只在完整窗口销毁后调用。
- 可以通过 `DispatcherQueue.TryEnqueue` 放到低优先级执行，减少 UI 卡顿感。

### 5. 保存纯数据快照，避免重建时空白

完全销毁窗口会降低内存，但重新打开时容易短暂空白。解决办法不是保留整棵 UI 树，而是保留轻量数据。

推荐新增 `MainWindowSnapshot`：

```csharp
public sealed record MainWindowSnapshot(
    IReadOnlyList<VideoUpdateRow> Videos,
    IReadOnlyList<LiveCreatorRow> LiveCreators,
    IReadOnlyList<VideoUpdateRow> History,
    IReadOnlyList<VideoUpdateRow> ViewLater,
    DateTimeOffset CapturedAt);
```

关闭窗口前：

- 从当前页面导出纯模型。
- 不保存控件对象。
- 不保存 `ImageSource`、`BitmapImage`、`UIElement`、`DispatcherQueue` 等 UI 资源。

重新打开时：

- 先用 snapshot 同步渲染第一帧。
- 后台再刷新网络数据。
- 刷新完成后替换为最新数据。

这样能同时保留：

- 关闭后的低内存。
- 再打开时不明显空白。

### 6. 图片缓存要有边界

图片是此类应用最容易“悄悄吃内存”的部分。

优化前常见问题：

- 每次打开都重新创建图片。
- 图片控件释放了，但静态缓存无限增长。
- 圆角处理、缩略图转换结果没有上限。

推荐：

- 使用小型 LRU 或简单有界缓存。
- 给视频封面、头像、圆角处理结果分别设上限。
- 关闭窗口时可以选择是否清空缓存。

两种策略：

- **最低内存策略**：窗口关闭时清空图片缓存，内存下降更多，但重新打开会重新加载图片，容易空白。
- **平衡策略**：保留有界图片缓存，关闭后内存略高，但重新打开体验更好。

本次最终更偏向平衡策略：不在每次关闭时删除所有视频卡片文字和图片，而是保留有界缓存，减少重新打开时的闪白和重载。

### 7. 折叠区初始状态要先应用，再创建内容

当设置里默认折叠“正在直播”列表时，如果先创建头像卡片，再异步折叠，就会看到头像闪一下。

推荐顺序：

1. 读取设置。
2. 同步设置折叠区 `Visibility`、`Opacity`、箭头状态。
3. 再创建直播卡片。
4. 用户手动展开时再播放动画。

同时给展开/折叠动画加版本号，避免旧动画回调覆盖新状态：

```csharp
private int _liveSectionAnimationVersion;

private void ApplyLiveSectionExpandedStateImmediately(bool expanded)
{
    _liveSectionAnimationVersion++;

    LiveCardsScrollViewer.Visibility = expanded
        ? Visibility.Visible
        : Visibility.Collapsed;

    LiveCardsScrollViewer.Opacity = expanded ? 1 : 0;
    UpdateLiveSectionChevron(expanded);
}
```

### 8. 固定窗口尺寸时，内部布局不要再写死高度

窗口防止高度跳动时，可以固定外层窗口大小，例如 `420 x 607 DIP` 或 `420 x 800 DIP`。

但内部根容器和滚动区域应尽量自适应：

```xml
<Grid
    x:Name="RootGrid"
    Width="420"
    Background="{ThemeResource LayerOnAcrylicFillColorDefaultBrush}">

    <Grid.RowDefinitions>
        <RowDefinition Height="Auto" />
        <RowDefinition Height="*" />
    </Grid.RowDefinitions>

    <ScrollViewer
        Grid.Row="1"
        VerticalScrollBarVisibility="Auto">
        <!-- content -->
    </ScrollViewer>
</Grid>
```

避免：

```xml
<ScrollViewer MaxHeight="560" />
```

否则窗口变高后，内容区域仍被旧高度限制，底部会出现大块空白。

## 推荐实现清单

可以按这个顺序做：

1. 启动时只创建托盘宿主，不创建主窗口。
2. 点击托盘时首次创建主窗口。
3. 主窗口失焦关闭时导出纯数据快照。
4. 主窗口关闭时释放事件、定时器、服务、UI 子元素。
5. `App` 清空主窗口引用。
6. 窗口销毁后低优先级执行 GC 和工作集修剪。
7. 重新打开时优先用 snapshot 渲染第一帧。
8. 图片资源改为有界缓存。
9. 默认折叠区域先同步状态，再创建子内容。
10. 固定外层窗口尺寸，内部布局使用 `*` 和自适应高度。

## 验证方法

建议不要只看一次任务管理器数字，而是看稳定后的趋势：

1. 启动应用，不点击托盘，等待 10 到 30 秒，记录后台常驻内存。
2. 点击托盘打开窗口，等待图片加载完成，记录打开状态内存。
3. 关闭或让弹窗失焦，等待 10 到 30 秒，记录关闭后内存。
4. 重复打开/关闭两到三次，观察是否稳定回落。
5. 检查 UI 是否出现空白、图片重载、折叠区闪现、窗口跳高。

更细的指标：

- Private Working Set
- Commit Size
- .NET managed heap size
- GDI/User object count
- 图片缓存数量
- XAML 控件数量是否随开关次数增长

## 取舍

### 最低内存

关闭窗口时：

- 清空所有 UI。
- 清空图片缓存。
- 清空圆角图缓存。
- 销毁窗口。
- 修剪工作集。

优点是关闭后内存最低。缺点是再打开时更容易空白，图片需要重新解码和下载。

### 平衡体验

关闭窗口时：

- 销毁窗口和 UI 控件。
- 保留纯数据 snapshot。
- 保留有界图片缓存。
- 修剪工作集。

优点是关闭后仍能明显降内存，再打开也更顺。缺点是内存不会降到绝对最低。

对托盘弹窗应用来说，平衡体验通常更合适。

## 常见坑

- 只调用 `Hide()`，窗口其实还活着。
- `App`、托盘服务或静态事件仍持有 `MainWindow` 引用。
- 定时器、事件、异步任务闭包捕获了窗口或控件。
- `ObservableCollection` 清了，但卡片控件里的图片资源没有释放。
- 静态图片缓存没有容量上限。
- 关闭窗口后立刻看任务管理器，GC 和工作集还没稳定。
- 固定了外层窗口高度，却忘了移除内部 `MaxHeight`。
- 折叠区先创建内容再折叠，导致首帧闪现。

## 可复用判断标准

如果一个 WinUI 3 应用满足这些条件，就适合使用这套策略：

- 长期驻留后台。
- 前台窗口只是临时弹窗。
- 点击托盘、快捷键或通知才需要显示 UI。
- UI 中包含图片、列表、卡片、动画或较多 XAML 控件。
- 用户更关心后台常驻内存，而不是每次打开都零延迟。

如果是主窗口长期打开的生产力应用，则不一定适合频繁销毁窗口，应更多关注虚拟化、分页、图片解码尺寸、缓存策略和泄漏排查。

