using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BiliRadar.Models;

namespace BiliRadar.Services;

public sealed class MockBiliDataProvider : IBiliDataProvider
{
    private static readonly IReadOnlyList<BiliCreator> Creators =
    [
        new(123456, "影视飓风", string.Empty),
        new(345678, "巫师财经", string.Empty),
        new(567890, "老师好我叫何同学", string.Empty),
    ];

    public Task<IReadOnlyList<BiliCreator>> GetFollowingAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Creators);
    }

    public Task<IReadOnlyList<BiliVideoUpdate>> GetRecentVideoUpdatesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.Now;
        IReadOnlyList<BiliVideoUpdate> updates =
        [
            new("BV1FRAME01", 123456, "影视飓风", "一条模拟的新视频更新，之后由 bili-kernel provider 替换", now.AddMinutes(-18), "https://www.bilibili.com", true),
            new("BV1FRAME02", 345678, "巫师财经", "关注列表轮询框架已经就位", now.AddHours(-2), "https://www.bilibili.com", true),
            new("BV1FRAME03", 567890, "老师好我叫何同学", "这里会显示已读/未读和发布时间", now.AddDays(-1), "https://www.bilibili.com", false),
        ];

        return Task.FromResult(updates);
    }
}
