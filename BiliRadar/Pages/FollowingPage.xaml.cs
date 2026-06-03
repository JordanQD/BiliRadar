using BiliRadar.Controls;
using BiliRadar.Helpers;
using BiliRadar.Models;
using BiliRadar.Services;
using CommunityToolkit.WinUI.Animations;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.System;

namespace BiliRadar.Pages;

public sealed partial class FollowingPage : Page, IMainPanelPage, IDisposable
{
    private const string PersonAddIconData = "M10 2C12.7614 2 15 4.23858 15 7C15 9.76142 12.7614 12 10 12C7.23858 12 5 9.76142 5 7C5 4.23858 7.23858 2 10 2ZM10 3.5C8.067 3.5 6.5 5.067 6.5 7C6.5 8.933 8.067 10.5 10 10.5C11.933 10.5 13.5 8.933 13.5 7C13.5 5.067 11.933 3.5 10 3.5ZM4.25 14H11.25C11.6642 14 12 14.3358 12 14.75C12 15.1642 11.6642 15.5 11.25 15.5H4.25C3.83579 15.5 3.5 15.8358 3.5 16.25V17.16C3.5 17.82 3.79 18.44 4.29 18.86C5.54 19.94 7.44 20.5 10 20.5C10.58 20.5 11.13 20.47 11.65 20.41C12.0615 20.3626 12.4337 20.6579 12.4811 21.0694C12.5285 21.4808 12.2332 21.853 11.8218 21.9004C11.2493 21.9664 10.642 22 10 22C7.11 22 4.87 21.34 3.31 20C2.48 19.29 2 18.25 2 17.16V16.25C2 15.0074 3.00736 14 4.25 14ZM18 12C18.4142 12 18.75 12.3358 18.75 12.75V16.25H22.25C22.6642 16.25 23 16.5858 23 17C23 17.4142 22.6642 17.75 22.25 17.75H18.75V21.25C18.75 21.6642 18.4142 22 18 22C17.5858 22 17.25 21.6642 17.25 21.25V17.75H13.75C13.3358 17.75 13 17.4142 13 17C13 16.5858 13.3358 16.25 13.75 16.25H17.25V12.75C17.25 12.3358 17.5858 12 18 12Z";
    private const string PersonDeleteIconData = "M17.5 12C20.5375661 12 23 14.4624339 23 17.5C23 20.5375661 20.5375661 23 17.5 23C14.4624339 23 12 20.5375661 12 17.5C12 14.4624339 14.4624339 12 17.5 12ZM12.0222607 13.9993086C11.7255613 14.4626083 11.4860296 14.9660345 11.3136172 15.4996352L4.25354153 15.499921C3.83932796 15.499921 3.50354153 15.8357075 3.50354153 16.249921L3.50354153 17.1572408C3.50354153 17.8128951 3.78953221 18.4359296 4.28670709 18.8633654C5.5447918 19.9450082 7.44080155 20.5010712 10 20.5010712C10.598839 20.5010712 11.1614445 20.4706245 11.6881394 20.4101192C11.9370538 20.9102887 12.2508544 21.3740111 12.6170965 21.7904935C11.8149076 21.9312924 10.9419626 22.0010712 10 22.0010712C7.11050247 22.0010712 4.87168436 21.3444691 3.30881727 20.0007885C2.48019625 19.2883988 2.00354153 18.2500002 2.00354153 17.1572408L2.00354153 16.249921C2.00354153 15.0072804 3.01090084 13.999921 4.25354153 13.999921L12.0222607 13.9993086ZM15.0930472 14.9662824L15.0237993 15.0241379L14.9659438 15.0933858C14.8478223 15.2638954 14.8478223 15.4914871 14.9659438 15.6619968L15.0237993 15.7312446L16.7933527 17.5006913L15.0263884 19.2674911L14.968533 19.3367389C14.8504114 19.5072486 14.8504114 19.7348403 14.968533 19.9053499L15.0263884 19.9745978L15.0956363 20.0324533C15.2661459 20.1505748 15.4937377 20.1505748 15.6642473 20.0324533L15.7334952 19.9745978L17.5003527 18.2076913L19.2693951 19.9768405L19.338643 20.0346959C19.5091526 20.1528175 19.7367444 20.1528175 19.907254 20.0346959L19.9765019 19.9768405L20.0343574 19.9075926C20.1524789 19.737083 20.1524789 19.5094912 20.0343574 19.3389816L19.9765019 19.2697337L18.2073527 17.5006913L19.9792686 15.7312918L20.0371241 15.6620439C20.1552456 15.4915343 20.1552456 15.2639425 20.0371241 15.0934329L19.9792686 15.024185L19.9100208 14.9663296C19.7395111 14.848208 19.5119194 14.848208 19.3414098 14.9663296L19.2721619 15.024185L17.5003527 16.7936913L15.7309061 15.0241379L15.6616582 14.9662824C15.5155071 14.8650354 15.3274181 14.8505715 15.1692847 14.9228908L15.0930472 14.9662824ZM10 2.0046246C12.7614237 2.0046246 15 4.24320085 15 7.0046246C15 9.76604835 12.7614237 12.0046246 10 12.0046246C7.23857625 12.0046246 5 9.76604835 5 7.0046246C5 4.24320085 7.23857625 2.0046246 10 2.0046246ZM10 3.5046246C8.06700338 3.5046246 6.5 5.07162798 6.5 7.0046246C6.5 8.93762123 8.06700338 10.5046246 10 10.5046246C11.9329966 10.5046246 13.5 8.93762123 13.5 7.0046246C13.5 5.07162798 11.9329966 3.5046246 10 3.5046246Z";
    private const double RemoteRoundedImageSize = 44;
    private const double RemoteRoundedImageCornerRadius = 22;

    private MainPanelSession? _session;
    private CancellationToken _flyoutCancellationToken;
    private bool _isResettingScrollPosition;
    private bool _isLiveSectionExpanded;
    private int _liveSectionAnimationVersion;
    private bool _isDisposed;

    public FollowingPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    public void Initialize(MainPanelSession session)
    {
        if (_session is not null)
        {
            _session.UpdatesRefreshed -= OnUpdatesRefreshed;
            _session.FollowingListRefreshed -= OnFollowingListRefreshed;
            _session.CollectionAdded -= OnCollectionAdded;
        }

        _session = session;
        _session.UpdatesRefreshed += OnUpdatesRefreshed;
        _session.FollowingListRefreshed += OnFollowingListRefreshed;
        _session.CollectionAdded += OnCollectionAdded;
    }

    public Task ActivateAsync(CancellationToken cancellationToken = default)
    {
        _flyoutCancellationToken = cancellationToken;
        return _session is not null
            ? _session.RefreshOnShowAsync(cancellationToken)
            : Task.CompletedTask;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        ActualThemeChanged += OnActualThemeChanged;
        ApplyLiveSectionDisplayMode(AppSettings.LiveSectionDisplayMode);
    }

    private void OnActualThemeChanged(FrameworkElement sender, object args)
    {
        if (_isDisposed) return;
        DispatcherQueue.TryEnqueue(() =>
        {
            if (_isDisposed) return;
            RenderLiveCreators();
        });
    }

    private void OnUpdatesRefreshed(object? sender, EventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (!_isDisposed) RenderVideoCards();
        });
    }

    private void OnFollowingListRefreshed(object? sender, EventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (!_isDisposed) RenderLiveCreators();
        });
    }

    private void OnCollectionAdded(object? sender, VideoUpdateRow row)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (_isDisposed) return;
            VideoCardsPanel.Children.Add(CreateVideoCardControl(row));
        });
    }

    private void ApplyLiveSectionDisplayMode(LiveSectionDisplayMode mode)
    {
        _isLiveSectionExpanded = mode != LiveSectionDisplayMode.Collapsed;

        if (mode == LiveSectionDisplayMode.Hidden)
        {
            LiveCreatorsSection.Visibility = Visibility.Collapsed;
            LiveCreatorsSection.Opacity = 1;
            LiveCreatorsSection.Translation = Vector3.Zero;
            LatestVideosHeader.Visibility = Visibility.Collapsed;
            return;
        }

        LatestVideosHeader.Visibility = Visibility.Visible;
        ApplyLiveSectionExpandedStateImmediately(_isLiveSectionExpanded);

        if (_session is not null && _session.LiveCreators.Count > 0)
        {
            LiveCreatorsSection.Visibility = Visibility.Visible;
            LiveCreatorsSection.Opacity = 1;
            LiveCreatorsSection.Translation = Vector3.Zero;
        }
        else
        {
            LiveCreatorsSection.Visibility = Visibility.Collapsed;
        }
    }

    private void ApplyLiveSectionExpandedStateImmediately(bool expanded)
    {
        _liveSectionAnimationVersion++;
        LiveCardsScrollViewer.Visibility = expanded ? Visibility.Visible : Visibility.Collapsed;
        LiveCardsScrollViewer.Opacity = expanded ? 1 : 0;
        LiveSectionChevronCollapsed.Visibility = expanded ? Visibility.Collapsed : Visibility.Visible;
        LiveSectionChevronExpanded.Visibility = expanded ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void LiveSectionToggleButton_Click(object sender, RoutedEventArgs e)
    {
        _isLiveSectionExpanded = !_isLiveSectionExpanded;
        await ApplyLiveSectionExpandedStateAsync(_isLiveSectionExpanded);
    }

    private async Task ApplyLiveSectionExpandedStateAsync(bool expanded)
    {
        var animationVersion = ++_liveSectionAnimationVersion;
        if (expanded)
        {
            LiveCardsScrollViewer.Visibility = Visibility.Visible;
            LiveCardsScrollViewer.Opacity = 1;
            await AnimationBuilder.Create()
                .Opacity(from: 0, to: 1, duration: TimeSpan.FromMilliseconds(200))
                .Translation(Axis.Y, from: -10, to: 0, duration: TimeSpan.FromMilliseconds(200))
                .StartAsync(LiveCardsScrollViewer);
        }
        else
        {
            await AnimationBuilder.Create()
                .Opacity(from: 1, to: 0, duration: TimeSpan.FromMilliseconds(150))
                .Translation(Axis.Y, from: 0, to: -10, duration: TimeSpan.FromMilliseconds(150))
                .StartAsync(LiveCardsScrollViewer);
            if (animationVersion != _liveSectionAnimationVersion) return;
            LiveCardsScrollViewer.Opacity = 0;
            LiveCardsScrollViewer.Visibility = Visibility.Collapsed;
        }

        if (animationVersion != _liveSectionAnimationVersion) return;
        LiveSectionChevronCollapsed.Visibility = expanded ? Visibility.Collapsed : Visibility.Visible;
        LiveSectionChevronExpanded.Visibility = expanded ? Visibility.Visible : Visibility.Collapsed;
    }

    private void RenderVideoCards()
    {
        if (_session is null || _isDisposed) return;
        ClearPanelChildren(VideoCardsPanel);
        if (_session.Updates.Count == 0)
        {
            VideoCardsPanel.Children.Add(new TextBlock
            {
                Text = LocalizationHelper.GetString("NoVideoUpdates"),
                FontSize = 13,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            });
            return;
        }

        foreach (var item in _session.Updates)
            VideoCardsPanel.Children.Add(CreateVideoCardControl(item));
    }

    private VideoCard CreateVideoCardControl(
        VideoUpdateRow item,
        ViewLaterButtonMode viewLaterButtonMode = ViewLaterButtonMode.Add,
        CreatorRelationActionMode relationActionMode = CreatorRelationActionMode.Unfollow)
    {
        var card = new VideoCard
        {
            Item = item,
            ViewLaterButtonMode = viewLaterButtonMode,
            ShowMetaTime = true,
            IsCreatorFollowedAsync = _session is not null
                ? mid => _session.IsCreatorFollowedAsync(mid)
                : null,
        };
        card.CardMenuFlyout = CreateVideoCardMenuFlyout(item, relationActionMode);
        card.CoverTapped += (_, row) => _ = LaunchVideoAsync(row);
        card.CreatorAvatarClicked += (_, row) => _ = LaunchCreatorSpaceAsync(row);
        card.ViewLaterClicked += viewLaterButtonMode == ViewLaterButtonMode.Remove
            ? (_, row) => HandleRemoveViewLaterClick(row)
            : (_, row) => HandleAddToViewLaterClick(row);
        return card;
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

    private async void HandleRemoveViewLaterClick(VideoUpdateRow item)
    {
        if (_session is null) return;
        try
        {
            await _session.RemoveFromViewLaterAsync(item.Aid);
            _session.RemoveViewLaterItem(item);
            _session.ShowStatus(LocalizationHelper.GetString("RemovedFromViewLater"), InfoBarSeverity.Success);
        }
        catch (Exception ex)
        {
            _session.ShowStatus(LocalizationHelper.Format("RemoveFromViewLaterFailed", ex.Message), InfoBarSeverity.Error);
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

    private void RenderLiveCreators()
    {
        if (_session is null || _isDisposed) return;

        ApplyLiveSectionExpandedStateImmediately(_isLiveSectionExpanded);
        SyncLiveCreatorCards();

        var hasLiveCreators = _session.LiveCreators.Count > 0;
        if (AppSettings.LiveSectionDisplayMode == LiveSectionDisplayMode.Hidden)
        {
            LiveCreatorsSection.Visibility = Visibility.Collapsed;
            LiveCreatorsSection.Opacity = 1;
            LiveCreatorsSection.Translation = Vector3.Zero;
            LatestVideosHeader.Visibility = Visibility.Collapsed;
        }
        else
        {
            LatestVideosHeader.Visibility = Visibility.Visible;
            if (hasLiveCreators)
            {
                if (LiveCreatorsSection.Visibility != Visibility.Visible)
                {
                    LiveCreatorsSection.Visibility = Visibility.Visible;
                    _ = AnimationBuilder.Create()
                        .Opacity(from: 0, to: 1, duration: TimeSpan.FromMilliseconds(180), easingType: EasingType.Cubic, easingMode: EasingMode.EaseOut)
                        .Translation(Axis.Y, from: -8, to: 0, duration: TimeSpan.FromMilliseconds(180), easingType: EasingType.Cubic, easingMode: EasingMode.EaseOut)
                        .StartAsync(LiveCreatorsSection);
                }
            }
            else if (LiveCreatorsSection.Visibility == Visibility.Visible)
            {
                _ = AnimationBuilder.Create()
                    .Opacity(from: 1, to: 0, duration: TimeSpan.FromMilliseconds(180), easingType: EasingType.Cubic, easingMode: EasingMode.EaseIn)
                    .Translation(Axis.Y, from: 0, to: -8, duration: TimeSpan.FromMilliseconds(180), easingType: EasingType.Cubic, easingMode: EasingMode.EaseIn)
                    .StartAsync(LiveCreatorsSection);
            }
        }
    }

    private void SyncLiveCreatorCards()
    {
        if (_session is null) return;

        for (var index = LiveCreatorCardsPanel.Children.Count - 1; index >= 0; index--)
        {
            if (LiveCreatorCardsPanel.Children[index] is not FrameworkElement { Tag: LiveCreatorRow existingItem }
                || _session.LiveCreators.All(item => !IsSameLiveCreator(item, existingItem)))
            {
                LiveCreatorCardsPanel.Children.RemoveAt(index);
            }
        }

        for (var targetIndex = 0; targetIndex < _session.LiveCreators.Count; targetIndex++)
        {
            var item = _session.LiveCreators[targetIndex];
            var existingIndex = FindLiveCreatorCardIndex(item);
            if (existingIndex < 0)
            {
                LiveCreatorCardsPanel.Children.Insert(targetIndex, CreateLiveCreatorItem(item));
            }
            else if (existingIndex != targetIndex)
            {
                var existingCard = LiveCreatorCardsPanel.Children[existingIndex];
                LiveCreatorCardsPanel.Children.RemoveAt(existingIndex);
                LiveCreatorCardsPanel.Children.Insert(targetIndex, existingCard);
            }
        }
    }

    private int FindLiveCreatorCardIndex(LiveCreatorRow item)
    {
        for (var index = 0; index < LiveCreatorCardsPanel.Children.Count; index++)
        {
            if (LiveCreatorCardsPanel.Children[index] is FrameworkElement { Tag: LiveCreatorRow existingItem }
                && IsSameLiveCreator(item, existingItem))
            {
                return index;
            }
        }

        return -1;
    }

    private static bool IsSameLiveCreator(LiveCreatorRow left, LiveCreatorRow right)
    {
        return left.Mid > 0 && right.Mid > 0
            ? left.Mid == right.Mid
            : left.RoomId == right.RoomId;
    }

    private FrameworkElement CreateLiveCreatorItem(LiveCreatorRow item)
    {
        var button = new Button
        {
            Width = 68,
            Height = 92,
            MinWidth = 0,
            Padding = new Thickness(4, 4, 4, 8),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            VerticalContentAlignment = VerticalAlignment.Top,
            Style = (Style)Application.Current.Resources["SubtleButtonStyle"],
            Tag = item,
        };
        button.Click += LiveCreatorButton_Click;
        var liveNotifyFlyout = new MenuFlyout();
        var liveNotifyItem = new MenuFlyoutItem
        {
            FontFamily = new FontFamily("Microsoft YaHei UI"),
            DataContext = item,
            Visibility = Visibility.Collapsed,
        };
        ConfigureLiveNotificationMenuItem(liveNotifyItem);
        liveNotifyItem.Click += LiveNotificationMenuItem_Click;
        liveNotifyFlyout.Items.Add(liveNotifyItem);
        liveNotifyFlyout.Opening += (_, _) => ConfigureLiveNotificationMenuItem(liveNotifyItem);
        button.ContextFlyout = liveNotifyFlyout;
        ToolTipService.SetToolTip(button, string.IsNullOrWhiteSpace(item.Title)
            ? LocalizationHelper.Format("OpenLiveRoomTooltip", item.Name)
            : item.Title);

        var panel = new StackPanel
        {
            Spacing = 4,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
        };

        var avatarHost = new Grid
        {
            Width = 44,
            Height = 44,
            HorizontalAlignment = HorizontalAlignment.Center,
        };

        avatarHost.Children.Add(CreateRemoteRoundedImage(item.AvatarUrl, RemoteRoundedImageSize, RemoteRoundedImageCornerRadius));
        panel.Children.Add(avatarHost);

        panel.Children.Add(new TextBlock
        {
            Text = item.Name,
            Width = 60,
            Height = 32,
            FontSize = 11,
            LineHeight = 16,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            TextAlignment = TextAlignment.Center,
            Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            MaxLines = 2,
            TextTrimming = TextTrimming.CharacterEllipsis,
            TextWrapping = TextWrapping.Wrap,
        });

        button.Content = panel;
        return button;
    }

    private static void ConfigureLiveNotificationMenuItem(MenuFlyoutItem item)
    {
        if (item.DataContext is not LiveCreatorRow row || row.Mid <= 0
            || AppSettings.NotificationTargetMode != NotificationTargetMode.CustomCreators)
        {
            item.Visibility = Visibility.Collapsed;
            return;
        }

        item.Visibility = Visibility.Visible;
        var isInList = AppSettings.CustomNotificationCreators.Any(c => c.Mid == row.Mid);
        item.Text = isInList
            ? LocalizationHelper.GetString("RemoveFromNotificationListMenuItem")
            : LocalizationHelper.GetString("AddToNotificationListMenuItem");
        item.Icon = isInList
            ? new FontIcon { Glyph = "", FontSize = 16 }
            : new FontIcon { Glyph = "", FontSize = 16 };
        item.Tag = isInList ? "remove" : "add";
    }

    private void LiveNotificationMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuFlyoutItem { DataContext: LiveCreatorRow item, Tag: string action })
            return;

        if (action == "remove")
            RemoveCreatorFromNotificationList(item.Mid, item.Name);
        else
            AddCreatorToNotificationList(item.Mid, item.Name, item.AvatarUrl);
    }

    private async void LiveCreatorButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: LiveCreatorRow item }) return;

        if (string.IsNullOrWhiteSpace(item.Url) || !Uri.TryCreate(item.Url, UriKind.Absolute, out var uri))
        {
            _session?.ShowStatus(LocalizationHelper.GetString("InvalidLiveRoomLink"), InfoBarSeverity.Warning);
            return;
        }

        await Launcher.LaunchUriAsync(uri);
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

    private async void VideoScrollViewer_ViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
    {
        if (e.IsIntermediate || _isResettingScrollPosition) return;

        var distanceToBottom = VideoScrollViewer.ScrollableHeight - VideoScrollViewer.VerticalOffset;
        if (distanceToBottom <= 40 && _session is not null)
            await _session.LoadMoreUpdatesAsync(_flyoutCancellationToken);
    }

    public void ResetScrollPosition()
    {
        _isResettingScrollPosition = true;
        VideoScrollViewer.ChangeView(null, 0, null, true);
        VideoScrollViewer.DispatcherQueue.TryEnqueue(() => _isResettingScrollPosition = false);
    }

    private static FrameworkElement CreateRemoteRoundedImage(string url, double size, double cornerRadius)
    {
        var imageBrush = new ImageBrush
        {
            Stretch = Stretch.UniformToFill,
        };
        var imageFrame = new Border
        {
            Width = size,
            Height = size,
            CornerRadius = new CornerRadius(cornerRadius),
            Background = imageBrush,
        };

        _ = LoadRemoteImageBrushAsync(imageBrush, url);
        return imageFrame;
    }

    private static async Task LoadRemoteImageBrushAsync(ImageBrush imageBrush, string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return;

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                using var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, uri);
                request.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0");
                request.Headers.TryAddWithoutValidation("Referer", "https://www.bilibili.com/");

                using var response = await MainPanelSession.ImageHttpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var bytes = await response.Content.ReadAsByteArrayAsync();

                imageBrush.DispatcherQueue.TryEnqueue(async () =>
                {
                    try
                    {
                        using var stream = new InMemoryRandomAccessStream();
                        await stream.WriteAsync(bytes.AsBuffer());
                        stream.Seek(0);
                        var bitmap = new BitmapImage();
                        await bitmap.SetSourceAsync(stream);
                        imageBrush.ImageSource = bitmap;
                    }
                    catch
                    {
                        imageBrush.ImageSource = new BitmapImage(uri);
                    }
                });
                return;
            }
            catch
            {
                if (attempt == 3)
                {
                    imageBrush.DispatcherQueue.TryEnqueue(() =>
                        imageBrush.ImageSource = new BitmapImage(uri));
                }
                else
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(450 * attempt));
                }
            }
        }
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

        Loaded -= OnLoaded;
        ActualThemeChanged -= OnActualThemeChanged;
        _liveSectionAnimationVersion++;

        if (_session is not null)
        {
            _session.UpdatesRefreshed -= OnUpdatesRefreshed;
            _session.FollowingListRefreshed -= OnFollowingListRefreshed;
            _session.CollectionAdded -= OnCollectionAdded;
            _session = null;
        }

        ClearPanelChildren(VideoCardsPanel);
        LiveCreatorCardsPanel.Children.Clear();
    }

    private static void ClearPanelChildren(Panel panel)
    {
        foreach (var child in panel.Children.OfType<IDisposable>().ToList())
        {
            child.Dispose();
        }

        panel.Children.Clear();
    }
}
