using System.Collections.Generic;

namespace BiliRadar.Models;

public sealed record MainWindowSnapshot(
    IReadOnlyList<BiliVideoUpdate> Updates,
    IReadOnlyList<BiliLiveCreator> LiveCreators);
