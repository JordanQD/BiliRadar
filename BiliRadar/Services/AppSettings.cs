using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using BiliRadar.Models;
using Windows.Storage;

namespace BiliRadar.Services;

public static class AppSettings
{
    public const string SystemLanguage = "system";

    private const int DefaultNotificationCheckIntervalMinutes = 15;
    private const string VideoNotificationsEnabledKey = "VideoNotificationsEnabled";
    private const string LiveNotificationsEnabledKey = "LiveNotificationsEnabled";
    private const string NotificationCheckIntervalMinutesKey = "NotificationCheckIntervalMinutes";
    private const string KnownVideoUpdateIdsKey = "KnownVideoUpdateIds";
    private const string KnownLiveRoomIdsKey = "KnownLiveRoomIds";
    private const string VideoNotificationBaselineInitializedKey = "VideoNotificationBaselineInitialized";
    private const string LiveNotificationBaselineInitializedKey = "LiveNotificationBaselineInitialized";
    private const string NotificationTargetModeKey = "NotificationTargetMode";
    private const string RunningLaunchActionKey = "RunningLaunchAction";
    private const string CustomLaunchWebPageUrlKey = "CustomLaunchWebPageUrl";
    private const string DefaultOpenPageKey = "DefaultOpenPage";
    private const string LiveSectionDisplayModeKey = "LiveSectionDisplayMode";
    private const string CustomNotificationCreatorsKey = "CustomNotificationCreators";
    private const string AppLanguageKey = "AppLanguage";
    private const string DefaultCustomLaunchWebPageUrl = "https://www.bilibili.com/";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static ApplicationDataContainer LocalSettings => ApplicationData.Current.LocalSettings;

    public static event EventHandler? NotificationSettingsChanged;

    public static bool VideoNotificationsEnabled
    {
        get => ReadBool(VideoNotificationsEnabledKey, true);
        set
        {
            if (VideoNotificationsEnabled == value)
            {
                return;
            }

            LocalSettings.Values[VideoNotificationsEnabledKey] = value;
            NotificationSettingsChanged?.Invoke(null, EventArgs.Empty);
        }
    }

    public static bool LiveNotificationsEnabled
    {
        get => ReadBool(LiveNotificationsEnabledKey, true);
        set
        {
            if (LiveNotificationsEnabled == value)
            {
                return;
            }

            LocalSettings.Values[LiveNotificationsEnabledKey] = value;
            NotificationSettingsChanged?.Invoke(null, EventArgs.Empty);
        }
    }

    public static int NotificationCheckIntervalMinutes
    {
        get => ReadInt(NotificationCheckIntervalMinutesKey, DefaultNotificationCheckIntervalMinutes);
        set
        {
            var normalizedValue = Math.Max(1, value);
            if (NotificationCheckIntervalMinutes == normalizedValue)
            {
                return;
            }

            LocalSettings.Values[NotificationCheckIntervalMinutesKey] = normalizedValue;
            NotificationSettingsChanged?.Invoke(null, EventArgs.Empty);
        }
    }

    public static NotificationTargetMode NotificationTargetMode
    {
        get
        {
            var value = ReadRawInt(NotificationTargetModeKey, (int)NotificationTargetMode.None);
            return Enum.IsDefined(typeof(NotificationTargetMode), value)
                ? (NotificationTargetMode)value
                : NotificationTargetMode.AllFollowing;
        }
        set
        {
            if (NotificationTargetMode == value)
            {
                return;
            }

            LocalSettings.Values[NotificationTargetModeKey] = (int)value;
            NotificationSettingsChanged?.Invoke(null, EventArgs.Empty);
        }
    }

    public static RunningLaunchAction RunningLaunchAction
    {
        get
        {
            var value = ReadRawInt(RunningLaunchActionKey, (int)RunningLaunchAction.OpenSettings);
            return Enum.IsDefined(typeof(RunningLaunchAction), value)
                ? (RunningLaunchAction)value
                : RunningLaunchAction.OpenSettings;
        }
        set
        {
            if (RunningLaunchAction == value)
            {
                return;
            }

            LocalSettings.Values[RunningLaunchActionKey] = (int)value;
        }
    }

    public static string CustomLaunchWebPageUrl
    {
        get => ReadString(CustomLaunchWebPageUrlKey, DefaultCustomLaunchWebPageUrl);
        set => LocalSettings.Values[CustomLaunchWebPageUrlKey] = NormalizeWebPageUrl(value);
    }

    public static DefaultOpenPage DefaultOpenPage
    {
        get
        {
            var value = ReadRawInt(DefaultOpenPageKey, (int)DefaultOpenPage.Following);
            return Enum.IsDefined(typeof(DefaultOpenPage), value)
                ? (DefaultOpenPage)value
                : DefaultOpenPage.Following;
        }
        set => LocalSettings.Values[DefaultOpenPageKey] = (int)value;
    }

    public static LiveSectionDisplayMode LiveSectionDisplayMode
    {
        get
        {
            var value = ReadRawInt(LiveSectionDisplayModeKey, (int)LiveSectionDisplayMode.Expanded);
            return Enum.IsDefined(typeof(LiveSectionDisplayMode), value)
                ? (LiveSectionDisplayMode)value
                : LiveSectionDisplayMode.Expanded;
        }
        set => LocalSettings.Values[LiveSectionDisplayModeKey] = (int)value;
    }

    public static bool VideoNotificationBaselineInitialized
    {
        get => ReadBool(VideoNotificationBaselineInitializedKey, false);
        set => LocalSettings.Values[VideoNotificationBaselineInitializedKey] = value;
    }

    public static bool LiveNotificationBaselineInitialized
    {
        get => ReadBool(LiveNotificationBaselineInitializedKey, false);
        set => LocalSettings.Values[LiveNotificationBaselineInitializedKey] = value;
    }

    public static IReadOnlyList<string> KnownVideoUpdateIds
    {
        get => ReadStringList(KnownVideoUpdateIdsKey);
        set => LocalSettings.Values[KnownVideoUpdateIdsKey] = string.Join('\n', value);
    }

    public static IReadOnlyList<string> KnownLiveRoomIds
    {
        get => ReadStringList(KnownLiveRoomIdsKey);
        set => LocalSettings.Values[KnownLiveRoomIdsKey] = string.Join('\n', value);
    }

    public static IReadOnlyList<NotificationCreatorSubscription> CustomNotificationCreators
    {
        get => ReadCustomNotificationCreators();
        set
        {
            var creators = value
                .Where(item => item.Mid > 0)
                .GroupBy(item => item.Mid)
                .Select(group => group.First())
                .ToList();

            LocalSettings.Values[CustomNotificationCreatorsKey] = JsonSerializer.Serialize(creators, JsonOptions);
            NotificationSettingsChanged?.Invoke(null, EventArgs.Empty);
        }
    }

    public static string AppLanguage
    {
        get => ReadString(AppLanguageKey, SystemLanguage);
        set => LocalSettings.Values[AppLanguageKey] = value;
    }

    private static bool ReadBool(string key, bool defaultValue)
    {
        return LocalSettings.Values.TryGetValue(key, out var value) && value is bool boolValue
            ? boolValue
            : defaultValue;
    }

    private static int ReadInt(string key, int defaultValue)
    {
        if (!LocalSettings.Values.TryGetValue(key, out var value))
        {
            return defaultValue;
        }

        return value switch
        {
            int intValue => Math.Max(1, intValue),
            long longValue => (int)Math.Max(1, longValue),
            double doubleValue => Math.Max(1, (int)Math.Round(doubleValue)),
            _ => defaultValue,
        };
    }

    private static int ReadRawInt(string key, int defaultValue)
    {
        if (!LocalSettings.Values.TryGetValue(key, out var value))
        {
            return defaultValue;
        }

        return value switch
        {
            int intValue => intValue,
            long longValue => (int)longValue,
            double doubleValue => (int)Math.Round(doubleValue),
            _ => defaultValue,
        };
    }

    private static string ReadString(string key, string defaultValue)
    {
        return LocalSettings.Values.TryGetValue(key, out var value)
            && value is string text
            && !string.IsNullOrWhiteSpace(text)
                ? text
                : defaultValue;
    }

    private static string NormalizeWebPageUrl(string? value)
    {
        var text = string.IsNullOrWhiteSpace(value)
            ? DefaultCustomLaunchWebPageUrl
            : value.Trim();

        if (!text.Contains("://", StringComparison.Ordinal))
        {
            text = $"https://{text}";
        }

        return Uri.TryCreate(text, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                ? uri.AbsoluteUri
                : DefaultCustomLaunchWebPageUrl;
    }

    private static IReadOnlyList<string> ReadStringList(string key)
    {
        if (!LocalSettings.Values.TryGetValue(key, out var value)
            || value is not string text
            || string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        return text
            .Split('\n')
            .Select(item => item.Trim())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IReadOnlyList<NotificationCreatorSubscription> ReadCustomNotificationCreators()
    {
        if (!LocalSettings.Values.TryGetValue(CustomNotificationCreatorsKey, out var value)
            || value is not string text
            || string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<NotificationCreatorSubscription>>(text, JsonOptions)?
                .Where(item => item.Mid > 0)
                .GroupBy(item => item.Mid)
                .Select(group => group.First())
                .ToList() ?? [];
        }
        catch
        {
            return [];
        }
    }
}

public enum NotificationTargetMode
{
    None = 0,
    AllFollowing = 1,
    CustomCreators = 2,
}

public enum RunningLaunchAction
{
    OpenSettings = 0,
    OpenCustomWebPage = 1,
}

public enum DefaultOpenPage
{
    Following = 0,
    History = 1,
    ViewLater = 2,
}

public enum LiveSectionDisplayMode
{
    Expanded = 0,
    Collapsed = 1,
    Hidden = 2,
}
