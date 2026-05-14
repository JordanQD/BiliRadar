using System;

namespace BiliRadar.Models;

public sealed record BiliVideoUpdate(
    string Id,
    long CreatorMid,
    string CreatorName,
    string Title,
    DateTimeOffset PublishedAt,
    string Url,
    bool IsUnread);
