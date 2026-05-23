using System;
using System.Collections.Generic;

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
    int CommentCount,
    int Page = 0,
    int VideoCount = 0);

public sealed record BiliVideoUpdatePage(
    IReadOnlyList<BiliVideoUpdate> Items,
    string NextOffset,
    bool HasMore);

public sealed record BiliVideoHistoryPage(
    IReadOnlyList<BiliVideoUpdate> Items,
    long NextMax,
    long NextViewAt,
    bool HasMore);

public sealed record BiliViewLaterPage(
    IReadOnlyList<BiliVideoUpdate> Items,
    int TotalCount,
    int NextPageNumber,
    bool HasMore);
