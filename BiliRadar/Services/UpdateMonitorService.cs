using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BiliRadar.Models;

namespace BiliRadar.Services;

public sealed class UpdateMonitorService
{
    private readonly IBiliDataProvider _dataProvider;

    public UpdateMonitorService(IBiliDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
    }

    public DateTimeOffset LastCheckedAt { get; private set; }

    public Task<IReadOnlyList<BiliCreator>> GetFollowingAsync(CancellationToken cancellationToken = default)
    {
        return _dataProvider.GetFollowingAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BiliVideoUpdate>> RefreshAsync(CancellationToken cancellationToken = default)
    {
        var updates = await _dataProvider.GetRecentVideoUpdatesAsync(cancellationToken);
        LastCheckedAt = DateTimeOffset.Now;
        return updates;
    }

    public Task<BiliVideoUpdatePage> LoadMoreAsync(CancellationToken cancellationToken = default)
    {
        return _dataProvider.GetMoreVideoUpdatesAsync(cancellationToken);
    }

    public Task AddToViewLaterAsync(long aid, CancellationToken cancellationToken = default)
    {
        return _dataProvider.AddToViewLaterAsync(aid, cancellationToken);
    }
}
