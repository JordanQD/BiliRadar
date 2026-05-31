namespace BiliRadar.Models;

using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Text.Json.Serialization;

public sealed class NotificationCreatorSubscription
{
    private ImageSource? _avatarImage;
    private string _avatarUrl = string.Empty;

    public long Mid { get; set; }

    public string Name { get; set; } = string.Empty;

    public string AvatarUrl
    {
        get => _avatarUrl;
        set
        {
            _avatarUrl = value ?? string.Empty;
            _avatarImage = null;
        }
    }

    public bool VideoNotificationsEnabled { get; set; } = true;

    public bool LiveNotificationsEnabled { get; set; } = true;

    [JsonIgnore]
    public ImageSource? AvatarImage
    {
        get
        {
            if (_avatarImage is not null)
            {
                return _avatarImage;
            }

            if (string.IsNullOrWhiteSpace(_avatarUrl))
            {
                return null;
            }

            _avatarImage = new BitmapImage(new Uri(_avatarUrl));
            return _avatarImage;
        }
    }
}
