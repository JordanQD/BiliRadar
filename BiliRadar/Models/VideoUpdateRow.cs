using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BiliRadar.Models;

public sealed class VideoUpdateRow : INotifyPropertyChanged
{
    private readonly BiliVideoUpdate _source;
    private string _tip;

    public VideoUpdateRow(BiliVideoUpdate update)
    {
        _source = update;
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
        _tip = update.Tip;
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

    public string Tip
    {
        get => _tip;
        set => SetProperty(ref _tip, value);
    }

    public string DurationText { get; }

    public string Description { get; }

    public string LikeCountText { get; }

    public string CommentCountText { get; }

    public int Page { get; }

    public string PageText { get; }

    public int VideoCount { get; }

    public BitmapImage? CoverImage { get; }

    public BitmapImage? AvatarImage { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public BiliVideoUpdate ToModel()
    {
        return _source with { Tip = Tip };
    }

    public void RefreshPublishedTip()
    {
        Tip = FormatRelativeTime(PublishedAt);
    }

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

    private static string FormatRelativeTime(DateTimeOffset time)
    {
        var now = DateTimeOffset.Now;
        var delta = now - time;
        if (delta.TotalMinutes < 1)
        {
            return "刚刚";
        }

        if (delta.TotalHours < 1)
        {
            return $"{Math.Max(1, (int)delta.TotalMinutes)} 分钟前";
        }

        if (delta.TotalDays < 1)
        {
            return $"{Math.Max(1, (int)delta.TotalHours)} 小时前";
        }

        if (time.Date == now.AddDays(-1).Date)
        {
            return $"昨天 {time:HH:mm}";
        }

        return time.Year == now.Year ? time.ToString("M-d HH:mm") : time.ToString("yyyy-M-d HH:mm");
    }

    private void SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(storage, value)) return;
        storage = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
