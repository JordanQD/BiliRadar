namespace BiliRadar.Models;

public sealed class NotificationCreatorSubscription
{
    public long Mid { get; set; }

    public string Name { get; set; } = string.Empty;

    public string AvatarUrl { get; set; } = string.Empty;

    public bool VideoNotificationsEnabled { get; set; } = true;

    public bool LiveNotificationsEnabled { get; set; } = true;
}
