using System.Collections.Generic;

namespace BiliRadar.Models;

public sealed record MainWindowSnapshot(
    IReadOnlyList<BiliVideoUpdate> Updates,
    IReadOnlyList<BiliLiveCreator> LiveCreators,
    IReadOnlyList<BiliVideoUpdate> HistoryItems,
    IReadOnlyList<BiliVideoUpdate> ViewLaterItems)
{
    public MainWindowSnapshot(
        IReadOnlyList<BiliVideoUpdate> updates,
        IReadOnlyList<BiliLiveCreator> liveCreators)
        : this(updates, liveCreators, [], [])
    {
    }
}
