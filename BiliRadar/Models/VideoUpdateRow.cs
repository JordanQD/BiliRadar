using Microsoft.UI.Xaml.Media.Imaging;
using System;

namespace BiliRadar.Models;

public sealed class VideoUpdateRow
{
    public VideoUpdateRow(BiliVideoUpdate update)
    {
        Id = update.Id;
        Aid = update.Aid;
        CreatorMid = update.CreatorMid;
        CreatorName = update.CreatorName;
        Title = update.Title;
        PublishedAt = update.PublishedAt;
        IsUnread = update.IsUnread;
        IsUnreadText = update.IsUnread ? "未读" : "已读";
        Url = update.Url;
        CoverUrl = update.CoverUrl;
        AvatarUrl = update.AvatarUrl;
        Tip = update.Tip;
        DurationText = update.DurationText;
        Description = update.Description;
        LikeCountText = FormatCount(update.LikeCount);
        CommentCountText = FormatCount(update.CommentCount);
        CoverImage = CreateImage(update.CoverUrl);
        AvatarImage = CreateImage(update.AvatarUrl);
        Page = update.Page;
        PageText = update.Page > 0 ? $"P{update.Page}" : string.Empty;
        VideoCount = update.VideoCount;
    }

    public string Id { get; }

    public long Aid { get; }

    public long CreatorMid { get; }

    public string CreatorName { get; }

    public string Title { get; }

    public DateTimeOffset PublishedAt { get; }

    public bool IsUnread { get; }

    public string IsUnreadText { get; }

    public string Url { get; }

    public string CoverUrl { get; }

    public string AvatarUrl { get; }

    public string Tip { get; set; }

    public string DurationText { get; }

    public string Description { get; }

    public string LikeCountText { get; }

    public string CommentCountText { get; }

    public int Page { get; }

    public string PageText { get; }

    public int VideoCount { get; }

    public BitmapImage? CoverImage { get; }

    public BitmapImage? AvatarImage { get; }

    private static BitmapImage? CreateImage(string url)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return null;
        }

        return new BitmapImage(uri);
    }

    private static string FormatCount(int count)
    {
        return count >= 10000
            ? $"{count / 10000d:0.#}万"
            : count.ToString();
    }
}
