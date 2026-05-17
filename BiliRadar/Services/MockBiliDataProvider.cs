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

    public Task<IReadOnlyList<BiliLiveCreator>> GetFollowingLiveCreatorsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<BiliLiveCreator> creators =
        [
            new(123456, 101, "影视飓风", string.Empty, "模拟直播间：今天聊聊影像", "https://live.bilibili.com/101"),
            new(567890, 102, "老师好我叫何同学", string.Empty, "模拟直播间：桌面小实验", "https://live.bilibili.com/102"),
        ];

        return Task.FromResult(creators);
    }

    public Task AddToViewLaterAsync(long aid, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task RemoveFromViewLaterAsync(long aid, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task FollowCreatorAsync(long mid, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task UnfollowCreatorAsync(long mid, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<bool> IsCreatorFollowedAsync(long mid, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(mid is 123456 or 345678 or 567890);
    }

    public Task<IReadOnlyList<BiliVideoUpdate>> GetRecentVideoUpdatesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.Now;
        IReadOnlyList<BiliVideoUpdate> updates =
        [
            new("BV1FRAME01", 1001, 123456, "影视飓风", "一条模拟的新视频更新，之后由 bili-kernel provider 替换", now.AddMinutes(-18), "https://www.bilibili.com", true, string.Empty, string.Empty, "18 分钟前投稿了视频", string.Empty, string.Empty, 0, 0),
            new("BV1FRAME02", 1002, 345678, "巫师财经", "关注列表轮询框架已经就位", now.AddHours(-2), "https://www.bilibili.com", true, string.Empty, string.Empty, "2 小时前投稿了视频", string.Empty, string.Empty, 0, 0),
            new("BV1FRAME03", 1003, 567890, "老师好我叫何同学", "这里会显示已读/未读和发布时间", now.AddDays(-1), "https://www.bilibili.com", false, string.Empty, string.Empty, "昨天投稿了视频", string.Empty, string.Empty, 0, 0),
        ];

        return Task.FromResult(updates);
    }

    public Task<BiliVideoUpdatePage> GetMoreVideoUpdatesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new BiliVideoUpdatePage([], string.Empty, false));
    }

    public Task<BiliVideoHistoryPage> GetRecentVideoHistoryAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.Now;
        IReadOnlyList<BiliVideoUpdate> items =
        [
            new("BV1HISTORY01", 2001, 123456, "影视飓风", "历史记录会复用关注页的紧凑卡片", now.AddMinutes(-12), "https://www.bilibili.com", false, string.Empty, string.Empty, "12 分钟前观看", "12:34", "看到 03:21", 0, 0),
            new("BV1HISTORY02", 2002, 345678, "巫师财经", "滚动到底部会继续加载更早记录", now.AddHours(-3), "https://www.bilibili.com", false, string.Empty, string.Empty, "3 小时前观看", "08:08", "已看完", 0, 0),
        ];

        return Task.FromResult(new BiliVideoHistoryPage(items, 0, 0, false));
    }

    public Task<BiliVideoHistoryPage> GetMoreVideoHistoryAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new BiliVideoHistoryPage([], 0, 0, false));
    }

    public Task<BiliViewLaterPage> GetRecentViewLaterAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.Now;
        IReadOnlyList<BiliVideoUpdate> items =
        [
            new("BV1VIEWLATER01", 3001, 123456, "影视飓风", "稍后再看也使用同一套卡片", now.AddHours(-5), "https://www.bilibili.com", false, string.Empty, string.Empty, "5 小时前添加", "15:20", "看到 02:10", 0, 0),
            new("BV1VIEWLATER02", 3002, 567890, "老师好我叫何同学", "这里会展示待看的视频列表", now.AddDays(-2), "https://www.bilibili.com", false, string.Empty, string.Empty, "2 天前添加", "06:30", string.Empty, 0, 0),
        ];

        return Task.FromResult(new BiliViewLaterPage(items, items.Count, 2, false));
    }

    public Task<BiliViewLaterPage> GetMoreViewLaterAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new BiliViewLaterPage([], 0, 2, false));
    }
}
