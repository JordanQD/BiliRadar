using System;

namespace BiliRadar.Models;

public sealed class VideoUpdateRow
{
    public VideoUpdateRow(BiliVideoUpdate update)
    {
        Id = update.Id;
        CreatorName = update.CreatorName;
        Title = update.Title;
        PublishedAt = update.PublishedAt;
        IsUnread = update.IsUnread;
        IsUnreadText = update.IsUnread ? "未读" : "已读";
    }

    public string Id { get; }

    public string CreatorName { get; }

    public string Title { get; }

    public DateTimeOffset PublishedAt { get; }

    public bool IsUnread { get; }

    public string IsUnreadText { get; }
}
