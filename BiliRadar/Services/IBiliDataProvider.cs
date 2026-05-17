using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BiliRadar.Models;

namespace BiliRadar.Services;

public interface IBiliDataProvider
{
    Task<IReadOnlyList<BiliCreator>> GetFollowingAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BiliLiveCreator>> GetFollowingLiveCreatorsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BiliVideoUpdate>> GetRecentVideoUpdatesAsync(CancellationToken cancellationToken = default);

    Task<BiliVideoUpdatePage> GetMoreVideoUpdatesAsync(CancellationToken cancellationToken = default);

    Task<BiliVideoHistoryPage> GetRecentVideoHistoryAsync(CancellationToken cancellationToken = default);

    Task<BiliVideoHistoryPage> GetMoreVideoHistoryAsync(CancellationToken cancellationToken = default);

    Task<BiliViewLaterPage> GetRecentViewLaterAsync(CancellationToken cancellationToken = default);

    Task<BiliViewLaterPage> GetMoreViewLaterAsync(CancellationToken cancellationToken = default);

    Task AddToViewLaterAsync(long aid, CancellationToken cancellationToken = default);

    Task RemoveFromViewLaterAsync(long aid, CancellationToken cancellationToken = default);

    Task FollowCreatorAsync(long mid, CancellationToken cancellationToken = default);

    Task UnfollowCreatorAsync(long mid, CancellationToken cancellationToken = default);

    Task<bool> IsCreatorFollowedAsync(long mid, CancellationToken cancellationToken = default);
}
