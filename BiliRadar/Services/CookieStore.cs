using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Storage;

namespace BiliRadar.Services;

public sealed class CookieStore
{
    private const string ContainerName = "BiliRadar";
    private const string CookieKey = "BiliCookie";

    public bool HasCookie => !string.IsNullOrWhiteSpace(GetCookieString());

    public string GetCookieString()
    {
        var container = GetContainer();
        return container.Values.TryGetValue(CookieKey, out var value)
            ? value?.ToString() ?? string.Empty
            : string.Empty;
    }

    public void SaveCookieString(string cookie)
    {
        var normalized = NormalizeCookie(cookie);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            Clear();
            return;
        }

        GetContainer().Values[CookieKey] = normalized;
    }

    public void Clear()
    {
        GetContainer().Values.Remove(CookieKey);
    }

    private static ApplicationDataContainer GetContainer()
        => ApplicationData.Current.LocalSettings.CreateContainer(ContainerName, ApplicationDataCreateDisposition.Always);

    private static string NormalizeCookie(string cookie)
    {
        var pairs = ParseCookiePairs(cookie);
        return string.Join("; ", pairs.Select(pair => $"{pair.Key}={pair.Value}"));
    }

    private static IDictionary<string, string> ParseCookiePairs(string cookie)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var segments = cookie
            .ReplaceLineEndings(";")
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var segment in segments)
        {
            var separatorIndex = segment.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = segment[..separatorIndex].Trim();
            var value = segment[(separatorIndex + 1)..].Trim();
            if (!string.IsNullOrWhiteSpace(key))
            {
                result[key] = value;
            }
        }

        return result;
    }
}
