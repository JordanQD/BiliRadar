namespace BiliRadar.Models;

using System;
using System.Text.Json.Serialization;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

public sealed class NotificationCreatorSubscription
{
    public long Mid { get; set; }

    public string Name { get; set; } = string.Empty;

    public string AvatarUrl { get; set; } = string.Empty;

    public bool VideoNotificationsEnabled { get; set; } = true;

    public bool LiveNotificationsEnabled { get; set; } = true;

    [JsonIgnore]
    public ImageSource? AvatarImage => string.IsNullOrWhiteSpace(AvatarUrl)
        ? null
        : new BitmapImage(new Uri(AvatarUrl));
}
