using BiliRadar.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BiliRadar.Services;

public sealed class UpdateMonitorService : IDisposable
{
    private readonly IBiliDataProvider _dataProvider;

    public UpdateMonitorService(IBiliDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
        LastCheckedAt = DateTimeOffset.Now;
    }

    public DateTimeOffset LastCheckedAt { get; private set; }

    public Task<IReadOnlyList<BiliCreator>> GetFollowingAsync(CancellationToken cancellationToken = default)
    {
        return _dataProvider.GetFollowingAsync(cancellationToken);
    }

    public Task<IReadOnlyList<BiliLiveCreator>> GetFollowingLiveCreatorsAsync(CancellationToken cancellationToken = default)
    {
        return _dataProvider.GetFollowingLiveCreatorsAsync(cancellationToken);
    }

    public Task<BiliCreator?> GetCreatorAsync(long mid, CancellationToken cancellationToken = default)
    {
        return _dataProvider.GetCreatorAsync(mid, cancellationToken);
    }

    public Task<IReadOnlyList<BiliVideoUpdate>> GetCreatorVideoUpdatesAsync(long mid, CancellationToken cancellationToken = default)
    {
        return _dataProvider.GetCreatorVideoUpdatesAsync(mid, cancellationToken);
    }

    public Task<BiliLiveCreator?> GetCreatorLiveAsync(long mid, CancellationToken cancellationToken = default)
    {
        return _dataProvider.GetCreatorLiveAsync(mid, cancellationToken);
    }

    public async Task<IReadOnlyList<BiliVideoUpdate>> RefreshAsync(CancellationToken cancellationToken = default)
    {
        var updates = await _dataProvider.GetRecentVideoUpdatesAsync(cancellationToken);
        LastCheckedAt = DateTimeOffset.Now;
        return updates;
    }

    public Task<IReadOnlyList<BiliVideoUpdate>> GetRecentVideoUpdatesForNotificationAsync(CancellationToken cancellationToken = default)
    {
        return _dataProvider.GetRecentVideoUpdatesAsync(cancellationToken);
    }

    public Task<BiliVideoUpdatePage> LoadMoreAsync(CancellationToken cancellationToken = default)
    {
        return _dataProvider.GetMoreVideoUpdatesAsync(cancellationToken);
    }

    public Task<BiliVideoHistoryPage> RefreshHistoryAsync(CancellationToken cancellationToken = default)
    {
        return _dataProvider.GetRecentVideoHistoryAsync(cancellationToken);
    }

    public Task<BiliVideoHistoryPage> LoadMoreHistoryAsync(CancellationToken cancellationToken = default)
    {
        return _dataProvider.GetMoreVideoHistoryAsync(cancellationToken);
    }

    public Task<BiliViewLaterPage> RefreshViewLaterAsync(CancellationToken cancellationToken = default)
    {
        return _dataProvider.GetRecentViewLaterAsync(cancellationToken);
    }

    public Task<BiliViewLaterPage> LoadMoreViewLaterAsync(CancellationToken cancellationToken = default)
    {
        return _dataProvider.GetMoreViewLaterAsync(cancellationToken);
    }

    public Task AddToViewLaterAsync(long aid, CancellationToken cancellationToken = default)
    {
        return _dataProvider.AddToViewLaterAsync(aid, cancellationToken);
    }

    public Task RemoveFromViewLaterAsync(long aid, CancellationToken cancellationToken = default)
    {
        return _dataProvider.RemoveFromViewLaterAsync(aid, cancellationToken);
    }

    public Task FollowCreatorAsync(long mid, CancellationToken cancellationToken = default)
    {
        return _dataProvider.FollowCreatorAsync(mid, cancellationToken);
    }

    public Task UnfollowCreatorAsync(long mid, CancellationToken cancellationToken = default)
    {
        return _dataProvider.UnfollowCreatorAsync(mid, cancellationToken);
    }

    public Task<bool> IsCreatorFollowedAsync(long mid, CancellationToken cancellationToken = default)
    {
        return _dataProvider.IsCreatorFollowedAsync(mid, cancellationToken);
    }

    public void Dispose()
    {
        if (_dataProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
