using BiliRadar.Models;
using BiliRadar.Pages;
using BiliRadar.Services;
using Microsoft.UI.Composition;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace BiliRadar.Controls;

public sealed partial class MainPanelControl : UserControl, IDisposable
{
    private static readonly TimeSpan FlyoutOpenAnimationDuration = TimeSpan.FromMilliseconds(267);
    private static readonly TimeSpan FlyoutCloseAnimationDuration = TimeSpan.FromMilliseconds(200);
    private static readonly Vector2 FlyoutOpenAnimationControlPoint1 = new(0.1f, 0.9f);
    private static readonly Vector2 FlyoutOpenAnimationControlPoint2 = new(0.4f, 1.0f);
    private static readonly Vector2 FlyoutCloseAnimationControlPoint1 = new(0.2f, 0.0f);
    private static readonly Vector2 FlyoutCloseAnimationControlPoint2 = new(0.9f, 0.0f);
    private static readonly TimeSpan StatusNotificationEnterAnimationDuration = TimeSpan.FromMilliseconds(180);
    private static readonly TimeSpan StatusNotificationExitAnimationDuration = TimeSpan.FromMilliseconds(160);

    private int _previousSelectedPageIndex = -1;
    private readonly HashSet<IDisposable> _initializedPages = [];
    private CancellationTokenSource? _flyoutCts;
    private CancellationTokenSource? _pageSwitchCleanupCts;
    private StatusNotification? _currentStatusNotification;
    private int _statusNotificationAnimationVersion;
    private bool _isFlyoutOpen;
    private bool _isDisposed;
    private bool _isSettingDefaultPage;
    private bool _isSchedulingOpenActivation;

    public MainPanelSession Session { get; }

    public MainPanelControl()
    {
        InitializeComponent();
        ApplyPanelHeight();
        Loaded += MainPanelControl_Loaded;
        Session = new MainPanelSession(new CookieStore());
        Session.StatusAdded += Session_StatusAdded;
        Session.StatusRemoved += Session_StatusRemoved;
        Session.StatusCleared += Session_StatusCleared;
        ContentFrame.Navigated += OnContentFrameNavigated;
        SelectDefaultPage();
    }

    public MainPanelControl(MainWindowSnapshot? snapshot)
    {
        InitializeComponent();
        ApplyPanelHeight();
        Loaded += MainPanelControl_Loaded;
        Session = new MainPanelSession(new CookieStore(), snapshot);
        Session.StatusAdded += Session_StatusAdded;
        Session.StatusRemoved += Session_StatusRemoved;
        Session.StatusCleared += Session_StatusCleared;
        ContentFrame.Navigated += OnContentFrameNavigated;
        SelectDefaultPage();
    }

    private void MainPanelControl_Loaded(object sender, RoutedEventArgs e)
    {
        ApplyPanelHeight();
    }

    private void ApplyPanelHeight()
    {
        var scale = XamlRoot?.RasterizationScale ?? 1.0;
        var workAreaHeight = DisplayArea.Primary.WorkArea.Height / scale;
        RootGrid.Height = Math.Min(workAreaHeight - 80, AppSettings.MainPanelHeight);
    }

    private void SelectDefaultPage()
    {
        var selectedItem = AppSettings.DefaultOpenPage switch
        {
            DefaultOpenPage.History => HistorySelectorItem,
            DefaultOpenPage.ViewLater => ViewLaterSelectorItem,
            _ => FollowingSelectorItem,
        };

        _isSettingDefaultPage = true;
        ContentSelectorBar.SelectedItem = selectedItem;
        selectedItem.IsSelected = true;
        _isSettingDefaultPage = false;
    }

    private void OnContentFrameNavigated(object sender, NavigationEventArgs e)
    {
        if (e.Content is IMainPanelPage page)
        {
            page.Initialize(Session);
            if (page is IDisposable disposablePage)
                _initializedPages.Add(disposablePage);
        }
    }

    /// <summary>
    /// Flyout 打开时调用：创建新的 CancellationTokenSource，触发当前可见页面的数据刷新。
    /// </summary>
    public void OnFlyoutOpened(bool playOpenAnimation)
    {
        _isFlyoutOpen = true;
        _flyoutCts?.Cancel();
        _flyoutCts?.Dispose();
        _flyoutCts = new CancellationTokenSource();

        if (playOpenAnimation)
        {
            _ = PlayFlyoutOpenAnimationAsync(_flyoutCts.Token);
        }
        else
        {
            ResetFlyoutVisual();
        }

        if (!AppSettings.SaveMainPanelPosition)
        {
            SelectDefaultPage();
        }

        ScheduleCurrentPageActivation(resetScrollPosition: !AppSettings.SaveMainPanelPosition);
    }

    /// <summary>
    /// Flyout 关闭时调用：取消仍在运行的 UI 请求。
    /// 外层 TrayFlyoutService 负责保存 snapshot 并决定是否销毁整个面板。
    /// </summary>
    public void OnFlyoutClosed()
    {
        _isFlyoutOpen = false;
        _flyoutCts?.Cancel();
    }

    public Task RefreshCurrentPageAsync()
    {
        if (!_isFlyoutOpen || ContentFrame.Content is not IMainPanelPage page)
        {
            return Task.CompletedTask;
        }

        return page.ActivateAsync(_flyoutCts?.Token ?? CancellationToken.None);
    }

    public void PrepareForFlyoutOpenAnimation(bool playOpenAnimation)
    {
        var visual = ElementCompositionPreview.GetElementVisual(RootGrid);
        visual.Opacity = 1f;
        visual.Offset = playOpenAnimation
            ? new Vector3(0f, GetFlyoutTransitionOffset(), 0f)
            : Vector3.Zero;
    }

    private async Task PlayFlyoutOpenAnimationAsync(CancellationToken cancellationToken)
    {
        try
        {
            await AnimateFlyoutOffsetAsync(
                GetFlyoutTransitionOffset(),
                0f,
                FlyoutOpenAnimationDuration,
                FlyoutOpenAnimationControlPoint1,
                FlyoutOpenAnimationControlPoint2,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
    }

    public async Task PlayFlyoutCloseAnimationAsync(CancellationToken cancellationToken)
    {
        var visual = ElementCompositionPreview.GetElementVisual(RootGrid);
        visual.Opacity = 1f;
        visual.Offset = Vector3.Zero;

        await AnimateFlyoutOffsetAsync(
            0f,
            GetFlyoutTransitionOffset(),
            FlyoutCloseAnimationDuration,
            FlyoutCloseAnimationControlPoint1,
            FlyoutCloseAnimationControlPoint2,
            cancellationToken);
    }

    private async Task AnimateFlyoutOffsetAsync(
        float from,
        float to,
        TimeSpan duration,
        Vector2 controlPoint1,
        Vector2 controlPoint2,
        CancellationToken cancellationToken)
    {
        var visual = ElementCompositionPreview.GetElementVisual(RootGrid);
        visual.Opacity = 1f;
        visual.Offset = new Vector3(0f, from, 0f);

        var compositor = visual.Compositor;
        var easing = compositor.CreateCubicBezierEasingFunction(controlPoint1, controlPoint2);
        var offsetAnimation = compositor.CreateScalarKeyFrameAnimation();
        offsetAnimation.Duration = duration;
        offsetAnimation.InsertKeyFrame(0f, from);
        offsetAnimation.InsertKeyFrame(1f, to, easing);

        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));

        var batch = compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
        batch.Completed += (_, _) => completion.TrySetResult();
        visual.StartAnimation("Offset.Y", offsetAnimation);
        batch.End();

        await completion.Task;
        visual.Offset = new Vector3(0f, to, 0f);
    }

    private void ResetFlyoutVisual()
    {
        var visual = ElementCompositionPreview.GetElementVisual(RootGrid);
        visual.Opacity = 1f;
        visual.Offset = Vector3.Zero;
    }

    private float GetFlyoutTransitionOffset()
    {
        var height = RootGrid.ActualHeight > 0
            ? RootGrid.ActualHeight
            : RootGrid.Height;

        if (double.IsNaN(height) || height <= 0)
        {
            height = AppSettings.MainPanelHeight;
        }

        return (float)Math.Max(1d, height);
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _flyoutCts?.Cancel();
        _flyoutCts?.Dispose();
        _flyoutCts = null;
        _pageSwitchCleanupCts?.Cancel();
        _pageSwitchCleanupCts?.Dispose();
        _pageSwitchCleanupCts = null;
        Loaded -= MainPanelControl_Loaded;
        Session.StatusAdded -= Session_StatusAdded;
        Session.StatusRemoved -= Session_StatusRemoved;
        Session.StatusCleared -= Session_StatusCleared;
        ContentFrame.Navigated -= OnContentFrameNavigated;
        foreach (var page in _initializedPages)
        {
            page.Dispose();
        }

        _initializedPages.Clear();
        _currentStatusNotification = null;
        _statusNotificationAnimationVersion++;
        StatusInfoBar.IsOpen = false;
        StatusNotificationLayer.Visibility = Visibility.Collapsed;
        StatusInfoBarText.Text = string.Empty;
        ResetStatusNotificationVisual();
        ContentFrame.Content = null;
        Session.Dispose();
    }

    private void ContentSelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        if (_isSettingDefaultPage)
        {
            return;
        }

        NavigateToSelectedPage(resetScrollPosition: true);
    }

    private void ScheduleCurrentPageActivation(bool resetScrollPosition)
    {
        if (_isSchedulingOpenActivation)
        {
            return;
        }

        _isSchedulingOpenActivation = true;
        DispatcherQueue.TryEnqueue(
            Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,
            () =>
            {
                _isSchedulingOpenActivation = false;
                if (_isDisposed || !_isFlyoutOpen)
                {
                    return;
                }

                EnsureSelectedPageNavigated(resetScrollPosition);
            });
    }

    private void NavigateToSelectedPage(bool resetScrollPosition)
    {
        var currentIndex = ContentSelectorBar.Items.IndexOf(ContentSelectorBar.SelectedItem);
        if (currentIndex < 0)
        {
            currentIndex = 0;
        }

        var pageType = currentIndex switch
        {
            0 => typeof(FollowingPage),
            1 => typeof(HistoryPage),
            _ => typeof(ViewLaterPage),
        };

        if (ContentFrame.Content?.GetType() == pageType)
        {
            if (resetScrollPosition)
                ResetPageScrollPosition();

            if (_isFlyoutOpen && ContentFrame.Content is IMainPanelPage existingPage)
                _ = existingPage.ActivateAsync(_flyoutCts?.Token ?? CancellationToken.None);
            _previousSelectedPageIndex = currentIndex;
            return;
        }

        var effect = currentIndex > _previousSelectedPageIndex
            ? SlideNavigationTransitionEffect.FromRight
            : SlideNavigationTransitionEffect.FromLeft;

        DeactivateCurrentPage();

        ContentFrame.Navigate(
            pageType,
            null,
            new SlideNavigationTransitionInfo { Effect = effect });

        _previousSelectedPageIndex = currentIndex;
        if (resetScrollPosition)
            ResetPageScrollPosition();

        if (_isFlyoutOpen && ContentFrame.Content is IMainPanelPage newPage)
            _ = newPage.ActivateAsync(_flyoutCts?.Token ?? CancellationToken.None);

        SchedulePageSwitchCleanup();
    }

    private void EnsureSelectedPageNavigated(bool resetScrollPosition)
    {
        NavigateToSelectedPage(resetScrollPosition);
        ApplyCurrentPageOpenSettings();
    }

    private void DeactivateCurrentPage()
    {
        if (ContentFrame.Content is IMainPanelPage page)
        {
            page.Deactivate();
            if (page is IDisposable disposablePage)
                _initializedPages.Remove(disposablePage);
        }
    }

    private void SchedulePageSwitchCleanup()
    {
        _pageSwitchCleanupCts?.Cancel();
        _pageSwitchCleanupCts?.Dispose();
        _pageSwitchCleanupCts = new CancellationTokenSource();
        var token = _pageSwitchCleanupCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(2), token);
                DispatcherQueue.TryEnqueue(
                    Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,
                    CollectReleasedPageResources);
            }
            catch (OperationCanceledException)
            {
            }
        }, token);
    }

    private static void CollectReleasedPageResources()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        using var process = Process.GetCurrentProcess();
        SetProcessWorkingSetSize(process.Handle, new IntPtr(-1), new IntPtr(-1));
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetProcessWorkingSetSize(
        IntPtr process,
        IntPtr minimumWorkingSetSize,
        IntPtr maximumWorkingSetSize);

    private void ResetPageScrollPosition()
    {
        (ContentFrame.Content as FollowingPage)?.ResetScrollPosition();
        (ContentFrame.Content as HistoryPage)?.ResetScrollPosition();
        (ContentFrame.Content as ViewLaterPage)?.ResetScrollPosition();
    }

    private void ApplyCurrentPageOpenSettings()
    {
        (ContentFrame.Content as FollowingPage)?.ApplyOpenSettings();
    }

    private async void OpenBrowserButton_Click(object sender, RoutedEventArgs e)
    {
        var section = ContentSelectorBar.SelectedItem switch
        {
            SelectorBarItem item when item == HistorySelectorItem => MainPanelSection.History,
            SelectorBarItem item when item == ViewLaterSelectorItem => MainPanelSection.ViewLater,
            _ => MainPanelSection.Following,
        };
        await Session.LaunchSelectedBrowserUriAsync(section);
    }

    private void Session_StatusAdded(object? sender, StatusNotification notification)
    {
        var isReplacingVisibleNotification = StatusInfoBar.IsOpen && _currentStatusNotification is not null;
        _currentStatusNotification = notification;
        StatusNotificationLayer.Visibility = Visibility.Visible;
        StatusInfoBarText.Text = notification.Message;
        StatusInfoBar.Severity = notification.Severity;
        StatusInfoBar.IsOpen = true;

        if (isReplacingVisibleNotification)
        {
            _statusNotificationAnimationVersion++;
            ResetStatusNotificationVisual();
            return;
        }

        PlayStatusNotificationEnterAnimation(++_statusNotificationAnimationVersion);
    }

    private void Session_StatusRemoved(object? sender, StatusNotification notification)
    {
        if (!ReferenceEquals(_currentStatusNotification, notification))
        {
            return;
        }

        _currentStatusNotification = null;
        _ = PlayStatusNotificationExitAnimationAsync(++_statusNotificationAnimationVersion);
    }

    private void Session_StatusCleared(object? sender, EventArgs e)
    {
        _currentStatusNotification = null;
        _statusNotificationAnimationVersion++;
        StatusInfoBar.IsOpen = false;
        StatusNotificationLayer.Visibility = Visibility.Collapsed;
        StatusInfoBarText.Text = string.Empty;
        ResetStatusNotificationVisual();
    }

    private void StatusNotificationInfoBar_CloseButtonClick(InfoBar sender, object args)
    {
        if (_currentStatusNotification is StatusNotification notification)
        {
            Session.DismissStatusNotification(notification);
            return;
        }

        StatusInfoBar.IsOpen = false;
        StatusNotificationLayer.Visibility = Visibility.Collapsed;
        StatusInfoBarText.Text = string.Empty;
        ResetStatusNotificationVisual();
    }

    private void PlayStatusNotificationEnterAnimation(int version)
    {
        var visual = ElementCompositionPreview.GetElementVisual(StatusNotificationLayer);
        visual.Opacity = 0f;
        visual.Offset = new Vector3(0f, 12f, 0f);

        var compositor = visual.Compositor;
        var easing = compositor.CreateCubicBezierEasingFunction(
            FlyoutOpenAnimationControlPoint1,
            FlyoutOpenAnimationControlPoint2);

        var opacityAnimation = compositor.CreateScalarKeyFrameAnimation();
        opacityAnimation.Duration = StatusNotificationEnterAnimationDuration;
        opacityAnimation.InsertKeyFrame(0f, 0f);
        opacityAnimation.InsertKeyFrame(1f, 1f, easing);

        var offsetAnimation = compositor.CreateScalarKeyFrameAnimation();
        offsetAnimation.Duration = StatusNotificationEnterAnimationDuration;
        offsetAnimation.InsertKeyFrame(0f, 12f);
        offsetAnimation.InsertKeyFrame(1f, 0f, easing);

        var batch = compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
        batch.Completed += (_, _) =>
        {
            if (version != _statusNotificationAnimationVersion)
            {
                return;
            }

            visual.Opacity = 1f;
            visual.Offset = Vector3.Zero;
        };

        visual.StartAnimation("Opacity", opacityAnimation);
        visual.StartAnimation("Offset.Y", offsetAnimation);
        batch.End();
    }

    private async Task PlayStatusNotificationExitAnimationAsync(int version)
    {
        var visual = ElementCompositionPreview.GetElementVisual(StatusNotificationLayer);
        visual.Opacity = 1f;
        visual.Offset = Vector3.Zero;

        var compositor = visual.Compositor;
        var easing = compositor.CreateCubicBezierEasingFunction(
            FlyoutCloseAnimationControlPoint1,
            FlyoutCloseAnimationControlPoint2);

        var opacityAnimation = compositor.CreateScalarKeyFrameAnimation();
        opacityAnimation.Duration = StatusNotificationExitAnimationDuration;
        opacityAnimation.InsertKeyFrame(0f, 1f);
        opacityAnimation.InsertKeyFrame(1f, 0f, easing);

        var offsetAnimation = compositor.CreateScalarKeyFrameAnimation();
        offsetAnimation.Duration = StatusNotificationExitAnimationDuration;
        offsetAnimation.InsertKeyFrame(0f, 0f);
        offsetAnimation.InsertKeyFrame(1f, 12f, easing);

        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var batch = compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
        batch.Completed += (_, _) => completion.TrySetResult();
        visual.StartAnimation("Opacity", opacityAnimation);
        visual.StartAnimation("Offset.Y", offsetAnimation);
        batch.End();

        await completion.Task;
        if (version != _statusNotificationAnimationVersion)
        {
            return;
        }

        StatusInfoBar.IsOpen = false;
        StatusNotificationLayer.Visibility = Visibility.Collapsed;
        StatusInfoBarText.Text = string.Empty;
        ResetStatusNotificationVisual();
    }

    private void ResetStatusNotificationVisual()
    {
        var visual = ElementCompositionPreview.GetElementVisual(StatusNotificationLayer);
        visual.Opacity = 1f;
        visual.Offset = Vector3.Zero;
    }
}
