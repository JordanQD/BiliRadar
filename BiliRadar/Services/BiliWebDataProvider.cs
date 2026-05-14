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
    private const string AllMomentsUrl = "https://api.bilibili.com/x/polymer/web-dynamic/v1/feed/all?type=video&timezone_offset=-480";
    private const string AddToViewLaterUrl = "https://api.bilibili.com/x/v2/history/toview/add";
    private const string BrowserUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36";
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
        using var document = await SendJsonAsync(url, cookie, cancellationToken).ConfigureAwait(false);
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

    public async Task<IReadOnlyList<BiliVideoUpdate>> GetRecentVideoUpdatesAsync(CancellationToken cancellationToken = default)
    {
        var cookie = _cookieStore.GetCookieString();
        if (string.IsNullOrWhiteSpace(cookie))
        {
            return Array.Empty<BiliVideoUpdate>();
        }

        return await TryGetVideoUpdatesAsync(AllMomentsUrl, cookie, cancellationToken).ConfigureAwait(false);
    }

    public async Task AddToViewLaterAsync(long aid, CancellationToken cancellationToken = default)
    {
        if (aid <= 0)
        {
            throw new InvalidOperationException("当前视频缺少 avid，暂时无法添加到稍后再看。");
        }

        var cookie = _cookieStore.GetCookieString();
        if (string.IsNullOrWhiteSpace(cookie))
        {
            throw new InvalidOperationException("请先保存 B 站 Cookie。");
        }

        var csrf = GetCookieValue(cookie, "bili_jct");
        if (string.IsNullOrWhiteSpace(csrf))
        {
            throw new InvalidOperationException("Cookie 中缺少 bili_jct，无法添加到稍后再看。");
        }

        using var request = CreateWebRequest(HttpMethod.Post, AddToViewLaterUrl, cookie);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["aid"] = aid.ToString(),
            ["csrf"] = csrf,
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var message = TryGetBiliErrorMessage(body);
            throw new InvalidOperationException($"HTTP {(int)response.StatusCode} {response.ReasonPhrase}: {message}");
        }

        using var document = JsonDocument.Parse(body);
        EnsureBiliSuccess(document.RootElement);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    private async Task<long> GetMyMidAsync(string cookie, CancellationToken cancellationToken)
    {
        using var document = await SendJsonAsync(NavUrl, cookie, cancellationToken).ConfigureAwait(false);
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

    private async Task<IReadOnlyList<BiliVideoUpdate>> TryGetVideoUpdatesAsync(string url, string cookie, CancellationToken cancellationToken)
    {
        using var document = await SendJsonAsync(url, cookie, cancellationToken).ConfigureAwait(false);
        EnsureBiliSuccess(document.RootElement);

        if (!document.RootElement.TryGetProperty("data", out var data)
            || !data.TryGetProperty("items", out var items)
            || items.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<BiliVideoUpdate>();
        }

        var updates = new List<BiliVideoUpdate>();
        foreach (var item in items.EnumerateArray())
        {
            var update = TryCreateVideoUpdate(item);
            if (update is not null)
            {
                updates.Add(update);
            }
        }

        return updates;
    }

    private static BiliVideoUpdate? TryCreateVideoUpdate(JsonElement item)
    {
        if (!item.TryGetProperty("modules", out var modules)
            || !modules.TryGetProperty("module_dynamic", out var dynamicModule)
            || !dynamicModule.TryGetProperty("major", out var major))
        {
            return null;
        }

        JsonElement video;
        if (major.TryGetProperty("archive", out var archive))
        {
            video = archive;
        }
        else if (major.TryGetProperty("pgc", out var pgc))
        {
            video = pgc;
        }
        else
        {
            return null;
        }

        var bvid = GetString(video, "bvid");
        var title = GetString(video, "title");
        var cover = NormalizeUrl(GetString(video, "cover"));
        var duration = GetString(video, "duration_text");
        var description = GetString(video, "desc");
        var jumpUrl = NormalizeUrl(GetString(video, "jump_url"));
        var aid = GetInt64(video, "aid");
        var id = !string.IsNullOrWhiteSpace(bvid) ? bvid : aid.ToString();
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(title))
        {
            return null;
        }

        var author = modules.TryGetProperty("module_author", out var authorModule) ? authorModule : default;
        var creatorMid = GetInt64(author, "mid");
        var creatorName = GetString(author, "name");
        var avatar = NormalizeUrl(GetString(author, "face"));
        var tip = GetString(author, "pub_time");
        var publishedAt = TryGetPublishedAt(author, out var time) ? time : DateTimeOffset.Now;

        var likeCount = 0;
        var commentCount = 0;
        if (modules.TryGetProperty("module_stat", out var stat))
        {
            likeCount = GetNestedCount(stat, "like");
            commentCount = GetNestedCount(stat, "comment");
        }

        if (video.TryGetProperty("stat", out var videoStat))
        {
            likeCount = likeCount == 0 ? GetInt32(videoStat, "like") : likeCount;
            commentCount = commentCount == 0 ? GetInt32(videoStat, "reply") : commentCount;
        }

        if (string.IsNullOrWhiteSpace(jumpUrl))
        {
            jumpUrl = !string.IsNullOrWhiteSpace(bvid)
                ? $"https://www.bilibili.com/video/{bvid}"
                : $"https://www.bilibili.com/av{aid}";
        }

        return new BiliVideoUpdate(
            id,
            aid,
            creatorMid,
            creatorName,
            title,
            publishedAt,
            jumpUrl,
            true,
            cover,
            avatar,
            tip,
            duration,
            description,
            likeCount,
            commentCount);
    }

    private static HttpRequestMessage CreateWebRequest(HttpMethod method, string url, string cookie)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.TryAddWithoutValidation("Cookie", cookie);
        request.Headers.TryAddWithoutValidation("User-Agent", BrowserUserAgent);
        request.Headers.TryAddWithoutValidation("Accept", "application/json, text/plain, */*");
        request.Headers.TryAddWithoutValidation("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8");
        request.Headers.TryAddWithoutValidation("Referer", "https://www.bilibili.com/");
        request.Headers.TryAddWithoutValidation("Origin", "https://www.bilibili.com");
        return request;
    }

    private async Task<JsonDocument> SendJsonAsync(string url, string cookie, CancellationToken cancellationToken)
    {
        using var request = CreateWebRequest(HttpMethod.Get, url, cookie);
        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var message = TryGetBiliErrorMessage(body);
            throw new InvalidOperationException(
                $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}: {message}");
        }

        return JsonDocument.Parse(body);
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

    private static string TryGetBiliErrorMessage(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return "响应为空";
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            var message = GetString(document.RootElement, "message");
            return string.IsNullOrWhiteSpace(message) ? body : message;
        }
        catch
        {
            if (body.TrimStart().StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase)
                || body.TrimStart().StartsWith("<html", StringComparison.OrdinalIgnoreCase))
            {
                return "返回的是 HTML 页面，接口地址可能已失效或被重定向。";
            }

            return body.Length > 160 ? body[..160] : body;
        }
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

    private static string GetString(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var value) ? value.GetString() ?? string.Empty : string.Empty;

    private static string GetCookieValue(string cookie, string key)
    {
        var segments = cookie.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var segment in segments)
        {
            var separatorIndex = segment.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            if (string.Equals(segment[..separatorIndex].Trim(), key, StringComparison.OrdinalIgnoreCase))
            {
                return segment[(separatorIndex + 1)..].Trim();
            }
        }

        return string.Empty;
    }

    private static int GetNestedCount(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return 0;
        }

        if (value.ValueKind == JsonValueKind.Object)
        {
            return GetInt32(value, "count");
        }

        return value.TryGetInt32(out var count) ? count : 0;
    }

    private static bool TryGetPublishedAt(JsonElement author, out DateTimeOffset publishedAt)
    {
        publishedAt = default;
        if (!author.TryGetProperty("pub_ts", out var value) || !value.TryGetInt64(out var timestamp) || timestamp <= 0)
        {
            return false;
        }

        publishedAt = DateTimeOffset.FromUnixTimeSeconds(timestamp).ToLocalTime();
        return true;
    }

    private static string NormalizeUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return string.Empty;
        }

        return url.StartsWith("//", StringComparison.Ordinal) ? $"https:{url}" : url;
    }
}
