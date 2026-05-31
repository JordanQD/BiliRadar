using BiliRadar.Helpers;
using BiliRadar.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BiliRadar.Services;

internal sealed class BackgroundNotificationMonitor : IDisposable
{
    private readonly BiliWebDataProvider _dataProvider;
    private readonly UpdateMonitorService _updateMonitorService;
    private readonly NotificationService _notificationService = new();

    public BackgroundNotificationMonitor(CookieStore cookieStore)
    {
        _dataProvider = new BiliWebDataProvider(cookieStore);
        _updateMonitorService = new UpdateMonitorService(_dataProvider);
    }

    public Task StartAsync()
    {
        return _notificationService.TryStartAsync(RefreshNotificationDataAsync);
    }

    public async Task HandleActivationAsync(NotificationService.NotificationActivationRequest request)
    {
        if (string.Equals(request.Action, NotificationService.WatchLaterAction, StringComparison.OrdinalIgnoreCase))
        {
            await AddToViewLaterFromNotificationAsync(request.Aid);
            return;
        }

        if (request.Uri is not null)
        {
            await NotificationService.LaunchUriAsync(request.Uri);
        }
    }

    public void Dispose()
    {
        _notificationService.Stop();
        _updateMonitorService.Dispose();
    }

    private async Task RefreshNotificationDataAsync()
    {
        if (AppSettings.NotificationTargetMode == NotificationTargetMode.CustomCreators)
        {
            await RefreshCustomNotificationDataAsync();
            return;
        }

        IReadOnlyList<BiliVideoUpdate> updates = [];
        try
        {
            updates = await _updateMonitorService.GetRecentVideoUpdatesForNotificationAsync();
        }
        catch
        {
        }

        var filteredUpdates = updates
            .Where(update => update.PublishedAt > _notificationService.ServiceStartedAt)
            .ToList();
        await _notificationService.NotifyVideoUpdatesAsync(filteredUpdates);

        IReadOnlyList<BiliLiveCreator> liveCreators = [];
        try
        {
            liveCreators = await _updateMonitorService.GetFollowingLiveCreatorsAsync();
        }
        catch
        {
        }

        await _notificationService.NotifyLiveStartsAsync(liveCreators);
    }

    private async Task RefreshCustomNotificationDataAsync()
    {
        var subscriptions = AppSettings.CustomNotificationCreators;
        IReadOnlyList<BiliVideoUpdate> allUpdates = [];
        try
        {
            allUpdates = await _updateMonitorService.GetRecentVideoUpdatesForNotificationAsync();
        }
        catch
        {
        }

        var cutoff = _updateMonitorService.LastCheckedAt > _notificationService.ServiceStartedAt
            ? _updateMonitorService.LastCheckedAt
            : _notificationService.ServiceStartedAt;
        var subscribedMids = subscriptions
            .Where(item => item.VideoNotificationsEnabled)
            .Select(item => item.Mid)
            .ToHashSet();
        var videoUpdates = allUpdates
            .Where(update => update.PublishedAt > cutoff && subscribedMids.Contains(update.CreatorMid))
            .ToList();

        await _notificationService.NotifyVideoUpdatesAsync(videoUpdates);

        IReadOnlyList<BiliLiveCreator> allLiveCreators = [];
        try
        {
            allLiveCreators = await _updateMonitorService.GetFollowingLiveCreatorsAsync();
        }
        catch
        {
        }

        var subscribedLiveMids = subscriptions
            .Where(item => item.LiveNotificationsEnabled)
            .Select(item => item.Mid)
            .ToHashSet();
        var liveCreators = allLiveCreators
            .Where(creator => subscribedLiveMids.Contains(creator.Mid))
            .ToList();

        await _notificationService.NotifyLiveStartsAsync(liveCreators);
    }

    private async Task AddToViewLaterFromNotificationAsync(long aid)
    {
        if (aid <= 0)
        {
            return;
        }

        try
        {
            await _updateMonitorService.AddToViewLaterAsync(aid);
            NotificationService.ShowStatusNotification(
                LocalizationHelper.GetString("AddedToViewLater"),
                LocalizationHelper.GetString("AddedToViewLaterDetail"));
        }
        catch (Exception ex)
        {
            NotificationService.ShowStatusNotification(
                LocalizationHelper.GetString("AddToViewLaterFailed"),
                ex.Message);
        }
    }
}
