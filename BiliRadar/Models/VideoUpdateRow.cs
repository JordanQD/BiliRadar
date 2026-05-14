using System;
using Microsoft.UI.Xaml.Media.Imaging;

namespace BiliRadar.Models;

public sealed class VideoUpdateRow
{
    public VideoUpdateRow(BiliVideoUpdate update)
    {
        Id = update.Id;
        Aid = update.Aid;
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
    }

    public string Id { get; }

    public long Aid { get; }

    public string CreatorName { get; }

    public string Title { get; }

    public DateTimeOffset PublishedAt { get; }

    public bool IsUnread { get; }

    public string IsUnreadText { get; }

    public string Url { get; }

    public string CoverUrl { get; }

    public string AvatarUrl { get; }

    public string Tip { get; }

    public string DurationText { get; }

    public string Description { get; }

    public string LikeCountText { get; }

    public string CommentCountText { get; }

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
