using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BiliRadar.Models;

namespace BiliRadar.Services;

public sealed class BiliWebDataProvider : IBiliDataProvider, IDisposable
{
    private const string NavUrl = "https://api.bilibili.com/x/web-interface/nav";
    private const string FollowingsUrl = "https://api.bilibili.com/x/relation/followings";
    private readonly CookieStore _cookieStore;
    private readonly HttpClient _httpClient;

    public BiliWebDataProvider(CookieStore cookieStore)
    {
        _cookieStore = cookieStore;
        _httpClient = new HttpClient();
    }

    public async Task<IReadOnlyList<BiliCreator>> GetFollowingAsync(CancellationToken cancellationToken = default)
    {
        var cookie = _cookieStore.GetCookieString();
        if (string.IsNullOrWhiteSpace(cookie))
        {
            return Array.Empty<BiliCreator>();
        }

        var myMid = await GetMyMidAsync(cookie, cancellationToken).ConfigureAwait(false);
        var url = $"{FollowingsUrl}?vmid={myMid}&pn=1&ps=50&order=desc&order_type=attention";
        using var request = CreateWebRequest(HttpMethod.Get, url, cookie);
        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        EnsureBiliSuccess(document.RootElement);

        if (!document.RootElement.TryGetProperty("data", out var data)
            || !data.TryGetProperty("list", out var list)
            || list.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<BiliCreator>();
        }

        var creators = new List<BiliCreator>();
        foreach (var item in list.EnumerateArray())
        {
            var mid = GetInt64(item, "mid");
            var name = GetString(item, "uname");
            var face = GetString(item, "face");
            if (mid > 0 && !string.IsNullOrWhiteSpace(name))
            {
                creators.Add(new BiliCreator(mid, name, face));
            }
        }

        return creators;
    }

    public Task<IReadOnlyList<BiliVideoUpdate>> GetRecentVideoUpdatesAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<BiliVideoUpdate> updates = Array.Empty<BiliVideoUpdate>();
        return Task.FromResult(updates);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    private async Task<long> GetMyMidAsync(string cookie, CancellationToken cancellationToken)
    {
        using var request = CreateWebRequest(HttpMethod.Get, NavUrl, cookie);
        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        EnsureBiliSuccess(document.RootElement);

        if (document.RootElement.TryGetProperty("data", out var data))
        {
            var mid = GetInt64(data, "mid");
            if (mid > 0)
            {
                return mid;
            }
        }

        throw new InvalidOperationException("Cookie 已保存，但没有拿到登录用户 UID。");
    }

    private static HttpRequestMessage CreateWebRequest(HttpMethod method, string url, string cookie)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.TryAddWithoutValidation("Cookie", cookie);
        request.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 BiliRadar/0.1");
        request.Headers.TryAddWithoutValidation("Referer", "https://www.bilibili.com/");
        return request;
    }

    private static void EnsureBiliSuccess(JsonElement root)
    {
        var code = GetInt32(root, "code");
        if (code == 0)
        {
            return;
        }

        var message = GetString(root, "message");
        throw new InvalidOperationException(string.IsNullOrWhiteSpace(message)
            ? $"B 站接口返回错误：{code}"
            : $"B 站接口返回错误：{message} ({code})");
    }

    private static int GetInt32(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var value) && value.TryGetInt32(out var number) ? number : 0;

    private static long GetInt64(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var value) && value.TryGetInt64(out var number) ? number : 0;

    private static string GetString(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var value) ? value.GetString() ?? string.Empty : string.Empty;
}
