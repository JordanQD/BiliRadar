using System;

namespace BiliRadar.Models;

public sealed record BiliVideoUpdate(
    string Id,
    long Aid,
    long CreatorMid,
    string CreatorName,
    string Title,
    DateTimeOffset PublishedAt,
    string Url,
    bool IsUnread,
    string CoverUrl,
    string AvatarUrl,
    string Tip,
    string DurationText,
    string Description,
    int LikeCount,
    int CommentCount);
