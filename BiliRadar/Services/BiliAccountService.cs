using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BiliRadar.Models;

namespace BiliRadar.Services;

public sealed class BiliAccountService : IDisposable
{
    private const string NavUrl = "https://api.bilibili.com/x/web-interface/nav";
    private readonly CookieStore _cookieStore;
    private readonly HttpClient _httpClient = new();

    public BiliAccountService(CookieStore cookieStore)
    {
        _cookieStore = cookieStore;
    }

    public async Task<BiliAccountProfile?> GetCurrentProfileAsync(CancellationToken cancellationToken = default)
    {
        var cookie = _cookieStore.GetCookieString();
        if (string.IsNullOrWhiteSpace(cookie))
        {
            return null;
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, NavUrl);
        request.Headers.TryAddWithoutValidation("Cookie", cookie);
        request.Headers.TryAddWithoutValidation("User-Agent", BiliWebDataProvider.BrowserUserAgent);
        request.Headers.TryAddWithoutValidation("Accept", "application/json, text/plain, */*");
        request.Headers.TryAddWithoutValidation("Referer", "https://www.bilibili.com/");

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        if (GetInt32(root, "code") != 0 || !root.TryGetProperty("data", out var data))
        {
            return null;
        }

        var isLogin = GetBool(data, "isLogin");
        var mid = GetInt64(data, "mid");
        if (!isLogin || mid <= 0)
        {
            return null;
        }

        return new BiliAccountProfile(
            mid,
            GetString(data, "uname"),
            NormalizeUrl(GetString(data, "face")));
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    private static int GetInt32(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return 0;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetInt32(out var number) => number,
            JsonValueKind.String when int.TryParse(value.GetString(), out var number) => number,
            _ => 0,
        };
    }

    private static long GetInt64(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return 0;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetInt64(out var number) => number,
            JsonValueKind.String when long.TryParse(value.GetString(), out var number) => number,
            _ => 0,
        };
    }

    private static bool GetBool(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return false;
        }

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number when value.TryGetInt32(out var number) => number != 0,
            JsonValueKind.String when bool.TryParse(value.GetString(), out var flag) => flag,
            JsonValueKind.String when int.TryParse(value.GetString(), out var number) => number != 0,
            _ => false,
        };
    }

    private static string GetString(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var value) ? value.GetString() ?? string.Empty : string.Empty;

    private static string NormalizeUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return string.Empty;
        }

        return url.StartsWith("//", StringComparison.Ordinal) ? $"https:{url}" : url;
    }
}
