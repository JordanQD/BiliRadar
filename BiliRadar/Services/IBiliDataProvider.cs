using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BiliRadar.Models;

namespace BiliRadar.Services;

public interface IBiliDataProvider
{
    Task<IReadOnlyList<BiliCreator>> GetFollowingAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BiliVideoUpdate>> GetRecentVideoUpdatesAsync(CancellationToken cancellationToken = default);

    Task<BiliVideoUpdatePage> GetMoreVideoUpdatesAsync(CancellationToken cancellationToken = default);

    Task AddToViewLaterAsync(long aid, CancellationToken cancellationToken = default);
}
