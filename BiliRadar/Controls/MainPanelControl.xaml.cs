using BiliRadar.Models;
using BiliRadar.Pages;
using BiliRadar.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BiliRadar.Controls;

public sealed partial class MainPanelControl : UserControl, IDisposable
{
    private int _previousSelectedPageIndex = -1;
    private CancellationTokenSource? _flyoutCts;
    private bool _isFlyoutOpen;

    public MainPanelSession Session { get; }

    public MainPanelControl()
    {
        InitializeComponent();
        Session = new MainPanelSession(new CookieStore());
        ContentFrame.Navigated += OnContentFrameNavigated;
        SetDefaultPage();
    }

    public MainPanelControl(MainWindowSnapshot? snapshot)
    {
        InitializeComponent();
        Session = new MainPanelSession(new CookieStore(), snapshot);
        ContentFrame.Navigated += OnContentFrameNavigated;
        SetDefaultPage();
    }

    private void SetDefaultPage()
    {
        var selectedItem = AppSettings.DefaultOpenPage switch
        {
            DefaultOpenPage.History => HistorySelectorItem,
            DefaultOpenPage.ViewLater => ViewLaterSelectorItem,
            _ => FollowingSelectorItem,
        };
        ContentSelectorBar.SelectedItem = selectedItem;
    }

    private void OnContentFrameNavigated(object sender, NavigationEventArgs e)
    {
        if (e.Content is IMainPanelPage page)
        {
            page.Initialize(Session);
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

        if (ContentFrame.Content is IMainPanelPage page)
        {
            _ = page.ActivateAsync(_flyoutCts.Token);
        }
    }

    /// <summary>
    /// Flyout 关闭时调用：取消仍在运行的 UI 请求，保留 Session。
    /// Session 由后续测量（Phase 5）决定是保留还是重建。
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
        _flyoutCts?.Cancel();
        _flyoutCts?.Dispose();
        _flyoutCts = null;
        ContentFrame.Navigated -= OnContentFrameNavigated;
        Session.Dispose();
    }

    private void ContentSelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        var currentIndex = sender.Items.IndexOf(sender.SelectedItem);
        var pageType = currentIndex switch
        {
            0 => typeof(FollowingPage),
            1 => typeof(HistoryPage),
            _ => typeof(ViewLaterPage),
        };

        if (ContentFrame.Content?.GetType() == pageType)
        {
            ResetPageScrollPosition();
            if (_isFlyoutOpen && ContentFrame.Content is IMainPanelPage existingPage)
                _ = existingPage.ActivateAsync(_flyoutCts?.Token ?? CancellationToken.None);
            _previousSelectedPageIndex = currentIndex;
            return;
        }

        var effect = currentIndex > _previousSelectedPageIndex
            ? SlideNavigationTransitionEffect.FromRight
            : SlideNavigationTransitionEffect.FromLeft;

        ContentFrame.Navigate(
            pageType,
            null,
            new SlideNavigationTransitionInfo { Effect = effect });

        _previousSelectedPageIndex = currentIndex;
        ResetPageScrollPosition();

        if (_isFlyoutOpen && ContentFrame.Content is IMainPanelPage newPage)
            _ = newPage.ActivateAsync(_flyoutCts?.Token ?? CancellationToken.None);
    }

    private void ResetPageScrollPosition()
    {
        (ContentFrame.Content as FollowingPage)?.ResetScrollPosition();
        (ContentFrame.Content as HistoryPage)?.ResetScrollPosition();
        (ContentFrame.Content as ViewLaterPage)?.ResetScrollPosition();
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
