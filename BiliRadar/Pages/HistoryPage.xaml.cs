using BiliRadar.Controls;
using BiliRadar.Helpers;
using BiliRadar.Models;
using BiliRadar.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.System;

namespace BiliRadar.Pages;

public sealed partial class HistoryPage : Page, IMainPanelPage, IDisposable
{
    private const string PersonAddIconData = "M10 2C12.7614 2 15 4.23858 15 7C15 9.76142 12.7614 12 10 12C7.23858 12 5 9.76142 5 7C5 4.23858 7.23858 2 10 2ZM10 3.5C8.067 3.5 6.5 5.067 6.5 7C6.5 8.933 8.067 10.5 10 10.5C11.933 10.5 13.5 8.933 13.5 7C13.5 5.067 11.933 3.5 10 3.5ZM4.25 14H11.25C11.6642 14 12 14.3358 12 14.75C12 15.1642 11.6642 15.5 11.25 15.5H4.25C3.83579 15.5 3.5 15.8358 3.5 16.25V17.16C3.5 17.82 3.79 18.44 4.29 18.86C5.54 19.94 7.44 20.5 10 20.5C10.58 20.5 11.13 20.47 11.65 20.41C12.0615 20.3626 12.4337 20.6579 12.4811 21.0694C12.5285 21.4808 12.2332 21.853 11.8218 21.9004C11.2493 21.9664 10.642 22 10 22C7.11 22 4.87 21.34 3.31 20C2.48 19.29 2 18.25 2 17.16V16.25C2 15.0074 3.00736 14 4.25 14ZM18 12C18.4142 12 18.75 12.3358 18.75 12.75V16.25H22.25C22.6642 16.25 23 16.5858 23 17C23 17.4142 22.6642 17.75 22.25 17.75H18.75V21.25C18.75 21.6642 18.4142 22 18 22C17.5858 22 17.25 21.6642 17.25 21.25V17.75H13.75C13.3358 17.75 13 17.4142 13 17C13 16.5858 13.3358 16.25 13.75 16.25H17.25V12.75C17.25 12.3358 17.5858 12 18 12Z";
    private const string PersonDeleteIconData = "M17.5 12C20.5375661 12 23 14.4624339 23 17.5C23 20.5375661 20.5375661 23 17.5 23C14.4624339 23 12 20.5375661 12 17.5C12 14.4624339 14.4624339 12 17.5 12ZM12.0222607 13.9993086C11.7255613 14.4626083 11.4860296 14.9660345 11.3136172 15.4996352L4.25354153 15.499921C3.83932796 15.499921 3.50354153 15.8357075 3.50354153 16.249921L3.50354153 17.1572408C3.50354153 17.8128951 3.78953221 18.4359296 4.28670709 18.8633654C5.5447918 19.9450082 7.44080155 20.5010712 10 20.5010712C10.598839 20.5010712 11.1614445 20.4706245 11.6881394 20.4101192C11.9370538 20.9102887 12.2508544 21.3740111 12.6170965 21.7904935C11.8149076 21.9312924 10.9419626 22.0010712 10 22.0010712C7.11050247 22.0010712 4.87168436 21.3444691 3.30881727 20.0007885C2.48019625 19.2883988 2.00354153 18.2500002 2.00354153 17.1572408L2.00354153 16.249921C2.00354153 15.0072804 3.01090084 13.999921 4.25354153 13.999921L12.0222607 13.9993086ZM15.0930472 14.9662824L15.0237993 15.0241379L14.9659438 15.0933858C14.8478223 15.2638954 14.8478223 15.4914871 14.9659438 15.6619968L15.0237993 15.7312446L16.7933527 17.5006913L15.0263884 19.2674911L14.968533 19.3367389C14.8504114 19.5072486 14.8504114 19.7348403 14.968533 19.9053499L15.0263884 19.9745978L15.0956363 20.0324533C15.2661459 20.1505748 15.4937377 20.1505748 15.6642473 20.0324533L15.7334952 19.9745978L17.5003527 18.2076913L19.2693951 19.9768405L19.338643 20.0346959C19.5091526 20.1528175 19.7367444 20.1528175 19.907254 20.0346959L19.9765019 19.9768405L20.0343574 19.9075926C20.1524789 19.737083 20.1524789 19.5094912 20.0343574 19.3389816L19.9765019 19.2697337L18.2073527 17.5006913L19.9792686 15.7312918L20.0371241 15.6620439C20.1552456 15.4915343 20.1552456 15.2639425 20.0371241 15.0934329L19.9792686 15.024185L19.9100208 14.9663296C19.7395111 14.848208 19.5119194 14.848208 19.3414098 14.9663296L19.2721619 15.024185L17.5003527 16.7936913L15.7309061 15.0241379L15.6616582 14.9662824C15.5155071 14.8650354 15.3274181 14.8505715 15.1692847 14.9228908L15.0930472 14.9662824ZM10 2.0046246C12.7614237 2.0046246 15 4.24320085 15 7.0046246C15 9.76604835 12.7614237 12.0046246 10 12.0046246C7.23857625 12.0046246 5 9.76604835 5 7.0046246C5 4.24320085 7.23857625 2.0046246 10 2.0046246ZM10 3.5046246C8.06700338 3.5046246 6.5 5.07162798 6.5 7.0046246C6.5 8.93762123 8.06700338 10.5046246 10 10.5046246C11.9329966 10.5046246 13.5 8.93762123 13.5 7.0046246C13.5 5.07162798 11.9329966 3.5046246 10 3.5046246Z";

    private MainPanelSession? _session;
    private ScrollViewer? _historyScrollViewer;
    private CancellationToken _flyoutCancellationToken;
    private bool _isResettingScrollPosition;
    private bool _hasHistoryLoadCompleted;
    private bool _isDisposed;

    public HistoryPage()
    {
        InitializeComponent();
    }

    public void Initialize(MainPanelSession session)
    {
        if (_session is not null)
        {
            _session.HistoryRefreshed -= OnHistoryRefreshed;
            _session.HistoryItems.CollectionChanged -= OnHistoryItemsCollectionChanged;
        }

        _session = session;
        _session.HistoryRefreshed += OnHistoryRefreshed;
        _session.HistoryItems.CollectionChanged += OnHistoryItemsCollectionChanged;
        HistoryListView.ItemsSource = _session.HistoryItems;
        UpdateEmptyState();
    }

    public Task ActivateAsync(CancellationToken cancellationToken = default)
    {
        _flyoutCancellationToken = cancellationToken;
        return _session is not null
            ? _session.RefreshHistoryAsync(cancellationToken)
            : Task.CompletedTask;
    }

    private void OnHistoryRefreshed(object? sender, EventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (_isDisposed) return;
            _hasHistoryLoadCompleted = true;
            RenderHistoryCards();
        });
    }

    private void OnHistoryItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (!_isDisposed) UpdateEmptyState();
        });
    }

    private void RenderHistoryCards()
    {
        UpdateEmptyState();
    }

    private void UpdateEmptyState()
    {
        if (_session is null || _isDisposed) return;
        HistoryEmptyPanel.Visibility = _hasHistoryLoadCompleted && _session.HistoryItems.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void HistoryVideoCard_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not VideoCard card) return;

        card.IsCreatorFollowedAsync = _session is not null
            ? mid => _session.IsCreatorFollowedAsync(mid)
            : null;
        card.CardMenuFlyoutFactory = item =>
        {
            var relationActionMode = _session?.GetCreatorRelationActionMode(item)
                ?? CreatorRelationActionMode.Unfollow;
            return CreateVideoCardMenuFlyout(item, relationActionMode);
        };
    }

    private void VideoCard_CoverTapped(object sender, VideoUpdateRow row)
    {
        _ = LaunchVideoAsync(row);
    }

    private void VideoCard_CreatorAvatarClicked(object sender, VideoUpdateRow row)
    {
        _ = LaunchCreatorSpaceAsync(row);
    }

    private void VideoCard_ViewLaterClicked(object sender, VideoUpdateRow row)
    {
        HandleAddToViewLaterClick(row);
    }

    private async void HandleAddToViewLaterClick(VideoUpdateRow item)
    {
        if (_session is null) return;
        try
        {
            await _session.AddToViewLaterAsync(item.Aid);
            _session.ShowStatus(LocalizationHelper.GetString("AddedToViewLaterToast"), InfoBarSeverity.Success);
        }
        catch (Exception ex)
        {
            _session.ShowStatus(LocalizationHelper.Format("AddToViewLaterFailedToast", ex.Message), InfoBarSeverity.Error);
        }
    }

    private MenuFlyout CreateVideoCardMenuFlyout(VideoUpdateRow item, CreatorRelationActionMode relationActionMode)
    {
        var flyout = new MenuFlyout();
        var relationItem = new MenuFlyoutItem
        {
            FontFamily = new FontFamily("Microsoft YaHei UI"),
            IsEnabled = item.CreatorMid > 0,
            DataContext = item,
        };
        ConfigureCreatorRelationMenuItem(relationItem, relationActionMode);
        relationItem.Click += CreatorRelationMenuItem_Click;

        MenuFlyoutItem? notifyItem = null;
        if (item.CreatorMid > 0)
        {
            notifyItem = new MenuFlyoutItem
            {
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                DataContext = item,
                Visibility = Visibility.Collapsed,
            };
            ConfigureNotificationListMenuItem(notifyItem);
            notifyItem.Click += NotificationListMenuItem_Click;
        }

        flyout.Opening += async (_, _) =>
        {
            if (_session is null) return;
            if (notifyItem is not null)
            {
                try
                {
                    var followed = await _session.IsCreatorFollowedAsync(item.CreatorMid);
                    if (!followed)
                    {
                        notifyItem.Visibility = Visibility.Collapsed;
                        notifyItem.IsEnabled = false;
                        await RefreshCreatorRelationMenuItemAsync(relationItem);
                        return;
                    }
                }
                catch
                {
                    notifyItem.Visibility = Visibility.Collapsed;
                    notifyItem.IsEnabled = false;
                }

                RefreshNotificationListMenuItem(notifyItem);
            }

            await RefreshCreatorRelationMenuItemAsync(relationItem);
        };

        flyout.Items.Add(relationItem);
        if (notifyItem is not null)
            flyout.Items.Add(notifyItem);
        return flyout;
    }

    private async Task RefreshCreatorRelationMenuItemAsync(MenuFlyoutItem menuItem)
    {
        if (_session is null) return;
        if (menuItem.DataContext is not VideoUpdateRow item || item.CreatorMid <= 0)
        {
            menuItem.IsEnabled = false;
            return;
        }

        menuItem.IsEnabled = false;
        try
        {
            var isFollowed = await _session.IsCreatorFollowedAsync(item.CreatorMid);
            if (isFollowed)
                _session.AddFollowingCreator(item);
            else
                _session.RemoveFollowingCreator(item.CreatorMid);

            ConfigureCreatorRelationMenuItem(
                menuItem,
                isFollowed ? CreatorRelationActionMode.Unfollow : CreatorRelationActionMode.Follow);
        }
        catch
        {
            ConfigureCreatorRelationMenuItem(menuItem, _session.GetCreatorRelationActionMode(item));
        }
        finally
        {
            menuItem.IsEnabled = true;
        }
    }

    private static void ConfigureCreatorRelationMenuItem(MenuFlyoutItem item, CreatorRelationActionMode mode)
    {
        item.Tag = mode;
        item.Text = mode == CreatorRelationActionMode.Follow
            ? LocalizationHelper.GetString("FollowCreatorMenuItem")
            : LocalizationHelper.GetString("UnfollowCreatorMenuItem");
        item.Icon = CreatePathIcon(mode == CreatorRelationActionMode.Follow ? PersonAddIconData : PersonDeleteIconData);
    }

    private static void ConfigureNotificationListMenuItem(MenuFlyoutItem item)
    {
        if (item.DataContext is not VideoUpdateRow row || row.CreatorMid <= 0
            || AppSettings.NotificationTargetMode != NotificationTargetMode.CustomCreators)
        {
            item.Visibility = Visibility.Collapsed;
            return;
        }

        item.Visibility = Visibility.Visible;
        var isInList = AppSettings.CustomNotificationCreators.Any(c => c.Mid == row.CreatorMid);
        item.Text = isInList
            ? LocalizationHelper.GetString("RemoveFromNotificationListMenuItem")
            : LocalizationHelper.GetString("AddToNotificationListMenuItem");
        item.Icon = isInList
            ? new FontIcon { Glyph = "", FontSize = 16 }
            : new FontIcon { Glyph = "", FontSize = 16 };
        item.Tag = isInList ? "remove" : "add";
    }

    private static void RefreshNotificationListMenuItem(MenuFlyoutItem item)
    {
        ConfigureNotificationListMenuItem(item);
    }

    private async void CreatorRelationMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuFlyoutItem { DataContext: VideoUpdateRow item, Tag: CreatorRelationActionMode mode })
            return;
        if (_session is null) return;

        if (mode == CreatorRelationActionMode.Follow)
            await FollowCreatorAsync(item);
        else
            await UnfollowCreatorAsync(item);
    }

    private async Task FollowCreatorAsync(VideoUpdateRow item)
    {
        if (_session is null) return;
        await _session.FollowCreatorAsync(item.CreatorMid);
        _session.AddFollowingCreator(item);
        _session.ShowStatus(LocalizationHelper.Format("Followed", item.CreatorName), InfoBarSeverity.Success);
    }

    private async Task UnfollowCreatorAsync(VideoUpdateRow item)
    {
        if (_session is null) return;
        await _session.UnfollowCreatorAsync(item.CreatorMid);
        _session.RemoveFollowingCreator(item.CreatorMid);
        _session.ShowStatus(LocalizationHelper.Format("Unfollowed", item.CreatorName), InfoBarSeverity.Success);
    }

    private void NotificationListMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuFlyoutItem { DataContext: VideoUpdateRow item, Tag: string action })
            return;

        if (action == "remove")
            RemoveCreatorFromNotificationList(item.CreatorMid, item.CreatorName);
        else
            AddCreatorToNotificationList(item.CreatorMid, item.CreatorName, item.AvatarUrl);
    }

    private void AddCreatorToNotificationList(long mid, string name, string avatarUrl)
    {
        var subscriptions = AppSettings.CustomNotificationCreators.ToList();
        if (subscriptions.Any(c => c.Mid == mid)) return;

        subscriptions.Add(new NotificationCreatorSubscription
        {
            Mid = mid,
            Name = name,
            AvatarUrl = avatarUrl,
            VideoNotificationsEnabled = true,
            LiveNotificationsEnabled = true,
        });
        AppSettings.CustomNotificationCreators = subscriptions;
        _session?.ShowStatus(LocalizationHelper.Format("AddedToNotificationList", name), InfoBarSeverity.Success);
    }

    private void RemoveCreatorFromNotificationList(long mid, string name)
    {
        var subscriptions = AppSettings.CustomNotificationCreators.ToList();
        subscriptions.RemoveAll(c => c.Mid == mid);
        AppSettings.CustomNotificationCreators = subscriptions;
        _session?.ShowStatus(LocalizationHelper.Format("RemovedFromNotificationList", name), InfoBarSeverity.Success);
    }

    private async Task LaunchVideoAsync(VideoUpdateRow item)
    {
        if (string.IsNullOrWhiteSpace(item.Url) || !Uri.TryCreate(item.Url, UriKind.Absolute, out var uri))
        {
            _session?.ShowStatus(LocalizationHelper.GetString("InvalidVideoLink"), InfoBarSeverity.Warning);
            return;
        }

        await Launcher.LaunchUriAsync(uri);
    }

    private async Task LaunchCreatorSpaceAsync(VideoUpdateRow item)
    {
        if (item.CreatorMid <= 0)
        {
            _session?.ShowStatus(LocalizationHelper.GetString("InvalidCreatorLink"), InfoBarSeverity.Warning);
            return;
        }

        await Launcher.LaunchUriAsync(new Uri($"https://space.bilibili.com/{item.CreatorMid}"));
    }

    private void HistoryListView_Loaded(object sender, RoutedEventArgs e)
    {
        if (_historyScrollViewer is not null) return;

        _historyScrollViewer = FindDescendant<ScrollViewer>(HistoryListView);
        if (_historyScrollViewer is not null)
        {
            _historyScrollViewer.ViewChanged += HistoryScrollViewer_ViewChanged;
        }
    }

    private async void HistoryScrollViewer_ViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
    {
        if (_isResettingScrollPosition) return;
        if (_historyScrollViewer is null) return;

        var distanceToBottom = _historyScrollViewer.ScrollableHeight - _historyScrollViewer.VerticalOffset;
        if (distanceToBottom <= 120 && _session is not null)
            await _session.LoadMoreHistoryAsync(_flyoutCancellationToken);
    }

    public void ResetScrollPosition()
    {
        _isResettingScrollPosition = true;
        _historyScrollViewer?.ChangeView(null, 0, null, true);
        HistoryListView.DispatcherQueue.TryEnqueue(() => _isResettingScrollPosition = false);
    }

    private static IconElement CreatePathIcon(string data)
    {
        return (IconElement)XamlReader.Load($$"""
            <PathIcon
                xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                Width="24"
                Height="24"
                Data="{{data}}" />
            """);
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        Deactivate();
        DisposeVideoCards(HistoryListView);
    }

    public void Deactivate()
    {
        if (_session is not null)
        {
            _session.HistoryRefreshed -= OnHistoryRefreshed;
            _session.HistoryItems.CollectionChanged -= OnHistoryItemsCollectionChanged;
            _session = null;
        }

        if (_historyScrollViewer is not null)
        {
            _historyScrollViewer.ViewChanged -= HistoryScrollViewer_ViewChanged;
            _historyScrollViewer = null;
        }

        HistoryListView.ItemsSource = null;
    }

    private static void DisposeVideoCards(DependencyObject root)
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is VideoCard card)
                card.Dispose();

            DisposeVideoCards(child);
        }
    }

    private static T? FindDescendant<T>(DependencyObject root)
        where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is T match)
                return match;

            var descendant = FindDescendant<T>(child);
            if (descendant is not null)
                return descendant;
        }

        return null;
    }
}
