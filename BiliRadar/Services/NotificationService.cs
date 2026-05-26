using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using BiliRadar.Models;
using BiliRadar.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Windows.System;

namespace BiliRadar.Services;

public sealed class NotificationService
{
    public const string OpenAction = "open";
    public const string WatchLaterAction = "watchLater";
    private const int MaxStoredVideoUpdateIds = 80;
    private const int MaxStoredLiveRoomIds = 80;
    private const int MaxNotificationsPerRefresh = 3;
    private static readonly TimeSpan NotificationSpacing = TimeSpan.FromMilliseconds(750);
    private DispatcherTimer? _timer;
    private Func<Task>? _refreshAction;
    private bool _isRefreshing;

    public async Task TryStartAsync(Func<Task> refreshAction)
    {
        _refreshAction = refreshAction;
        if (_timer is null)
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(AppSettings.NotificationCheckIntervalMinutes),
            };
            _timer.Tick += OnTimerTick;
            AppSettings.NotificationSettingsChanged += AppSettings_NotificationSettingsChanged;
        }

        UpdateTimerState();

        if (_timer.IsEnabled)
        {
            await RefreshForNotificationsAsync();
        }
    }

    public void Stop()
    {
        if (_timer is null)
        {
            return;
        }

        _timer.Stop();
        _timer.Tick -= OnTimerTick;
        AppSettings.NotificationSettingsChanged -= AppSettings_NotificationSettingsChanged;
        _timer = null;
        _refreshAction = null;
    }

    public async Task NotifyVideoUpdatesAsync(IReadOnlyList<BiliVideoUpdate> updates, bool showNotifications = true)
    {
        var orderedUpdates = updates
            .Where(item => !string.IsNullOrWhiteSpace(item.Id))
            .OrderByDescending(item => item.PublishedAt)
            .ToList();

        if (!AppSettings.VideoNotificationBaselineInitialized)
        {
            SaveKnownVideoUpdates(orderedUpdates.Select(item => item.Id));
            AppSettings.VideoNotificationBaselineInitialized = true;
            return;
        }

        var knownIds = AppSettings.KnownVideoUpdateIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var newUpdates = orderedUpdates
            .Where(item => !knownIds.Contains(item.Id))
            .Take(MaxNotificationsPerRefresh)
            .ToList();

        SaveKnownVideoUpdates(orderedUpdates.Select(item => item.Id).Concat(knownIds));
        if (!showNotifications || !IsVideoNotificationEnabled() || newUpdates.Count == 0)
        {
            return;
        }

        foreach (var update in newUpdates.OrderBy(item => item.PublishedAt))
        {
            ShowVideoNotification(update);
            await Task.Delay(NotificationSpacing);
        }
    }

    public async Task NotifyLiveStartsAsync(IReadOnlyList<BiliLiveCreator> liveCreators, bool showNotifications = true)
    {
        var currentLiveCreators = liveCreators
            .Where(item => item.RoomId > 0)
            .GroupBy(item => item.RoomId)
            .Select(group => group.First())
            .ToList();

        if (!AppSettings.LiveNotificationBaselineInitialized)
        {
            SaveKnownLiveRooms(currentLiveCreators.Select(item => item.RoomId.ToString()));
            AppSettings.LiveNotificationBaselineInitialized = true;
            return;
        }

        var knownRoomIds = AppSettings.KnownLiveRoomIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var newLiveCreators = currentLiveCreators
            .Where(item => !knownRoomIds.Contains(item.RoomId.ToString()))
            .Take(MaxNotificationsPerRefresh)
            .ToList();

        SaveKnownLiveRooms(currentLiveCreators.Select(item => item.RoomId.ToString()));
        if (!showNotifications || !IsLiveNotificationEnabled() || newLiveCreators.Count == 0)
        {
            return;
        }

        foreach (var creator in newLiveCreators)
        {
            ShowLiveNotification(creator);
            await Task.Delay(NotificationSpacing);
        }
    }

    public static bool TryGetActivationRequest(
        AppNotificationActivatedEventArgs args,
        [NotNullWhen(true)] out NotificationActivationRequest? request)
    {
        request = null;
        var action = args.Arguments.TryGetValue("action", out var actionValue)
            ? actionValue
            : OpenAction;

        if (string.Equals(action, WatchLaterAction, StringComparison.OrdinalIgnoreCase))
        {
            if (!args.Arguments.TryGetValue("aid", out var aidText)
                || !long.TryParse(aidText, out var aid)
                || aid <= 0)
            {
                return false;
            }

            request = NotificationActivationRequest.ForWatchLater(aid);
            return true;
        }

        if (!args.Arguments.TryGetValue("url", out var url)
            || string.IsNullOrWhiteSpace(url)
            || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        request = NotificationActivationRequest.ForOpen(uri);
        return true;
    }

    public static Task LaunchUriAsync(Uri uri)
    {
        return Launcher.LaunchUriAsync(uri).AsTask();
    }

    public static void ShowStatusNotification(string title, string message)
    {
        TryShow(new AppNotificationBuilder()
            .AddText(title)
            .AddText(message)
            .BuildNotification());
    }

    private void OnTimerTick(object? sender, object e)
    {
        _ = RefreshForNotificationsAsync();
    }

    private void AppSettings_NotificationSettingsChanged(object? sender, EventArgs e)
    {
        if (_timer is null)
        {
            return;
        }

        UpdateTimerState();
        if (_timer.IsEnabled)
        {
            _ = RefreshForNotificationsAsync();
        }
    }

    private void UpdateTimerState()
    {
        if (_timer is null)
        {
            return;
        }

        _timer.Interval = TimeSpan.FromMinutes(AppSettings.NotificationCheckIntervalMinutes);
        if (IsAnyNotificationEnabled())
        {
            if (!_timer.IsEnabled)
            {
                _timer.Start();
            }
        }
        else
        {
            _timer.Stop();
        }
    }

    private async Task RefreshForNotificationsAsync()
    {
        if (_refreshAction is null || _isRefreshing)
        {
            return;
        }

        _isRefreshing = true;
        try
        {
            await _refreshAction();
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    private static void ShowVideoNotification(BiliVideoUpdate update)
    {
        var builder = new AppNotificationBuilder()
            .AddArgument("kind", "video")
            .AddArgument("action", OpenAction)
            .AddArgument("url", update.Url)
            .AddText(LocalizationHelper.Format("VideoNotificationTitle", update.CreatorName))
            .AddText(update.Title)
            .AddButton(new AppNotificationButton(LocalizationHelper.GetString("AddToViewLaterButton"))
                .AddArgument("kind", "video")
                .AddArgument("action", WatchLaterAction)
                .AddArgument("aid", update.Aid.ToString()))
            .AddButton(new AppNotificationButton(LocalizationHelper.GetString("OpenVideoButton"))
                .AddArgument("kind", "video")
                .AddArgument("action", OpenAction)
                .AddArgument("url", update.Url));

        AddImages(builder, update.AvatarUrl, update.CoverUrl);
        TryShow(builder.BuildNotification());
    }

    private static void ShowLiveNotification(BiliLiveCreator creator)
    {
        var title = string.IsNullOrWhiteSpace(creator.Title)
            ? LocalizationHelper.GetString("LiveNotificationFallbackTitle")
            : creator.Title;

        var builder = new AppNotificationBuilder()
            .AddArgument("kind", "live")
            .AddArgument("action", OpenAction)
            .AddArgument("url", creator.Url)
            .AddText(LocalizationHelper.Format("LiveNotificationTitle", creator.Name))
            .AddText(title);

        AddImages(builder, creator.AvatarUrl, string.Empty);
        TryShow(builder.BuildNotification());
    }

    private static void AddImages(AppNotificationBuilder builder, string avatarUrl, string coverUrl)
    {
        if (Uri.TryCreate(avatarUrl, UriKind.Absolute, out var avatarUri))
        {
            builder.SetAppLogoOverride(avatarUri, AppNotificationImageCrop.Circle);
        }

        if (Uri.TryCreate(coverUrl, UriKind.Absolute, out var coverUri))
        {
            builder.SetHeroImage(coverUri);
        }
    }

    private static void TryShow(AppNotification notification)
    {
        try
        {
            AppNotificationManager.Default.Show(notification);
        }
        catch
        {
        }
    }

    private static bool IsAnyNotificationEnabled()
    {
        if (AppSettings.NotificationTargetMode == NotificationTargetMode.CustomCreators)
        {
            return AppSettings.CustomNotificationCreators.Any(item =>
                item.VideoNotificationsEnabled || item.LiveNotificationsEnabled);
        }

        return AppSettings.VideoNotificationsEnabled || AppSettings.LiveNotificationsEnabled;
    }

    private static bool IsVideoNotificationEnabled()
    {
        if (AppSettings.NotificationTargetMode == NotificationTargetMode.CustomCreators)
        {
            return AppSettings.CustomNotificationCreators.Any(item => item.VideoNotificationsEnabled);
        }

        return AppSettings.VideoNotificationsEnabled;
    }

    private static bool IsLiveNotificationEnabled()
    {
        if (AppSettings.NotificationTargetMode == NotificationTargetMode.CustomCreators)
        {
            return AppSettings.CustomNotificationCreators.Any(item => item.LiveNotificationsEnabled);
        }

        return AppSettings.LiveNotificationsEnabled;
    }

    private static void SaveKnownVideoUpdates(IEnumerable<string> ids)
    {
        AppSettings.KnownVideoUpdateIds = ids
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(MaxStoredVideoUpdateIds)
            .ToList();
    }

    private static void SaveKnownLiveRooms(IEnumerable<string> ids)
    {
        AppSettings.KnownLiveRoomIds = ids
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(MaxStoredLiveRoomIds)
            .ToList();
    }

    public sealed record NotificationActivationRequest(string Action, Uri? Uri, long Aid)
    {
        public static NotificationActivationRequest ForOpen(Uri uri)
            => new(OpenAction, uri, 0);

        public static NotificationActivationRequest ForWatchLater(long aid)
            => new(WatchLaterAction, null, aid);
    }
}
