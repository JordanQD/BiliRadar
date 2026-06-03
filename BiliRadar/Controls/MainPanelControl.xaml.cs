using BiliRadar.Models;
using BiliRadar.Pages;
using BiliRadar.Services;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace BiliRadar.Controls;

public sealed partial class MainPanelControl : UserControl, IDisposable
{
    private int _previousSelectedPageIndex = -1;
    private readonly HashSet<IDisposable> _initializedPages = [];
    private CancellationTokenSource? _flyoutCts;
    private CancellationTokenSource? _pageSwitchCleanupCts;
    private bool _isFlyoutOpen;
    private bool _isDisposed;
    private bool _isSettingDefaultPage;

    public MainPanelSession Session { get; }

    public MainPanelControl()
    {
        InitializeComponent();
        ApplyPanelHeight();
        Loaded += MainPanelControl_Loaded;
        Session = new MainPanelSession(new CookieStore());
        ContentFrame.Navigated += OnContentFrameNavigated;
        SetDefaultPage();
    }

    public MainPanelControl(MainWindowSnapshot? snapshot)
    {
        InitializeComponent();
        ApplyPanelHeight();
        Loaded += MainPanelControl_Loaded;
        Session = new MainPanelSession(new CookieStore(), snapshot);
        ContentFrame.Navigated += OnContentFrameNavigated;
        SetDefaultPage();
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

    private void SetDefaultPage()
    {
        SetDefaultPage(resetScrollPosition: false);
    }

    private void SetDefaultPage(bool resetScrollPosition)
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

        NavigateToSelectedPage(resetScrollPosition);
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
    public void OnFlyoutOpened()
    {
        _isFlyoutOpen = true;
        _flyoutCts?.Cancel();
        _flyoutCts?.Dispose();
        _flyoutCts = new CancellationTokenSource();

        if (!AppSettings.SaveMainPanelPosition)
        {
            SetDefaultPage(resetScrollPosition: true);
            ApplyCurrentPageOpenSettings();
        }

        if (ContentFrame.Content is IMainPanelPage page)
        {
            _ = page.ActivateAsync(_flyoutCts.Token);
        }
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
        ContentFrame.Navigated -= OnContentFrameNavigated;
        foreach (var page in _initializedPages)
        {
            page.Dispose();
        }

        _initializedPages.Clear();
        StatusItemsControl.ItemsSource = null;
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

    private void StatusNotificationInfoBar_CloseButtonClick(InfoBar sender, object args)
    {
        if (sender.DataContext is StatusNotification notification)
        {
            notification.AutoDismissTimer?.Stop();
            notification.AutoDismissTimer = null;
            Session.StatusNotifications.Remove(notification);
        }
    }
}
