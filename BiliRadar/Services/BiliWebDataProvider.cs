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
    private const string FollowingLiveUrl = "https://api.live.bilibili.com/xlive/web-ucenter/v1/xfetter/GetWebList";
    private const string RelationUrl = "https://api.bilibili.com/x/relation";
    private const string ModifyRelationUrl = "https://api.bilibili.com/x/relation/modify";
    private const string AllMomentsUrl = "https://api.bilibili.com/x/polymer/web-dynamic/v1/feed/all";
    private const string HistoryCursorUrl = "https://api.bilibili.com/x/web-interface/history/cursor";
    private const string ViewLaterListUrl = "https://api.bilibili.com/x/v2/history/toview";
    private const string AddToViewLaterUrl = "https://api.bilibili.com/x/v2/history/toview/add";
    private const string RemoveFromViewLaterUrl = "https://api.bilibili.com/x/v2/history/toview/del";
    internal const string BrowserUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36";
    private const int RequestMaxAttemptCount = 3;
    private static readonly TimeSpan RequestRetryDelay = TimeSpan.FromMilliseconds(500);
    private readonly CookieStore _cookieStore;
    private readonly HttpClient _httpClient;
    private string _nextOffset = string.Empty;
    private bool _hasMoreUpdates;
    private long _historyNextMax;
    private long _historyNextViewAt;
    private bool _hasMoreHistory;
    private int _viewLaterNextPageNumber = 1;
    private int _viewLaterLoadedCount;
    private int _viewLaterTotalCount;
    private bool _hasMoreViewLater;

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

    public async Task<IReadOnlyList<BiliLiveCreator>> GetFollowingLiveCreatorsAsync(CancellationToken cancellationToken = default)
    {
        var cookie = _cookieStore.GetCookieString();
        if (string.IsNullOrWhiteSpace(cookie))
        {
            return Array.Empty<BiliLiveCreator>();
        }

        var url = $"{FollowingLiveUrl}?page=1&page_size=50";
        using var document = await SendJsonAsync(url, cookie, cancellationToken).ConfigureAwait(false);
        EnsureBiliSuccess(document.RootElement);

        if (!document.RootElement.TryGetProperty("data", out var data)
            || !TryGetLiveList(data, out var list))
        {
            return Array.Empty<BiliLiveCreator>();
        }

        var creators = new List<BiliLiveCreator>();
        foreach (var item in list.EnumerateArray())
        {
            var creator = TryCreateLiveCreator(item);
            if (creator is not null)
            {
                creators.Add(creator);
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

        var page = await TryGetVideoUpdatesAsync(CreateAllMomentsUrl(), cookie, cancellationToken).ConfigureAwait(false);
        _nextOffset = page.NextOffset;
        _hasMoreUpdates = page.HasMore;
        return page.Items;
    }

    public async Task<BiliVideoUpdatePage> GetMoreVideoUpdatesAsync(CancellationToken cancellationToken = default)
    {
        var cookie = _cookieStore.GetCookieString();
        if (string.IsNullOrWhiteSpace(cookie) || !_hasMoreUpdates || string.IsNullOrWhiteSpace(_nextOffset))
        {
            return new BiliVideoUpdatePage(Array.Empty<BiliVideoUpdate>(), _nextOffset, false);
        }

        var page = await TryGetVideoUpdatesAsync(CreateAllMomentsUrl(_nextOffset), cookie, cancellationToken).ConfigureAwait(false);
        _nextOffset = page.NextOffset;
        _hasMoreUpdates = page.HasMore;
        return page;
    }

    public async Task<BiliVideoHistoryPage> GetRecentVideoHistoryAsync(CancellationToken cancellationToken = default)
    {
        var cookie = _cookieStore.GetCookieString();
        if (string.IsNullOrWhiteSpace(cookie))
        {
            return new BiliVideoHistoryPage(Array.Empty<BiliVideoUpdate>(), 0, 0, false);
        }

        var page = await TryGetVideoHistoryAsync(CreateHistoryCursorUrl(), cookie, cancellationToken).ConfigureAwait(false);
        _historyNextMax = page.NextMax;
        _historyNextViewAt = page.NextViewAt;
        _hasMoreHistory = page.HasMore;
        return page;
    }

    public async Task<BiliVideoHistoryPage> GetMoreVideoHistoryAsync(CancellationToken cancellationToken = default)
    {
        var cookie = _cookieStore.GetCookieString();
        if (string.IsNullOrWhiteSpace(cookie) || !_hasMoreHistory || _historyNextMax <= 0)
        {
            return new BiliVideoHistoryPage(Array.Empty<BiliVideoUpdate>(), _historyNextMax, _historyNextViewAt, false);
        }

        var page = await TryGetVideoHistoryAsync(CreateHistoryCursorUrl(_historyNextMax, _historyNextViewAt), cookie, cancellationToken).ConfigureAwait(false);
        _historyNextMax = page.NextMax;
        _historyNextViewAt = page.NextViewAt;
        _hasMoreHistory = page.HasMore;
        return page;
    }

    public async Task<BiliViewLaterPage> GetRecentViewLaterAsync(CancellationToken cancellationToken = default)
    {
        var cookie = _cookieStore.GetCookieString();
        if (string.IsNullOrWhiteSpace(cookie))
        {
            return new BiliViewLaterPage(Array.Empty<BiliVideoUpdate>(), 0, 1, false);
        }

        var page = await TryGetViewLaterAsync(CreateViewLaterListUrl(1), cookie, 0, cancellationToken).ConfigureAwait(false);
        _viewLaterNextPageNumber = page.NextPageNumber;
        _viewLaterLoadedCount = page.Items.Count;
        _viewLaterTotalCount = page.TotalCount;
        _hasMoreViewLater = page.HasMore;
        return page;
    }

    public async Task<BiliViewLaterPage> GetMoreViewLaterAsync(CancellationToken cancellationToken = default)
    {
        var cookie = _cookieStore.GetCookieString();
        if (string.IsNullOrWhiteSpace(cookie) || !_hasMoreViewLater)
        {
            return new BiliViewLaterPage(Array.Empty<BiliVideoUpdate>(), _viewLaterTotalCount, _viewLaterNextPageNumber, false);
        }

        var page = await TryGetViewLaterAsync(CreateViewLaterListUrl(_viewLaterNextPageNumber), cookie, _viewLaterLoadedCount, cancellationToken).ConfigureAwait(false);
        _viewLaterNextPageNumber = page.NextPageNumber;
        _viewLaterLoadedCount += page.Items.Count;
        _viewLaterTotalCount = page.TotalCount;
        _hasMoreViewLater = page.HasMore;
        return page;
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

    public async Task RemoveFromViewLaterAsync(long aid, CancellationToken cancellationToken = default)
    {
        if (aid <= 0)
        {
            throw new InvalidOperationException("当前视频缺少 avid，暂时无法移出稍后再看。");
        }

        var cookie = _cookieStore.GetCookieString();
        if (string.IsNullOrWhiteSpace(cookie))
        {
            throw new InvalidOperationException("请先保存 B 站 Cookie。");
        }

        var csrf = GetCookieValue(cookie, "bili_jct");
        if (string.IsNullOrWhiteSpace(csrf))
        {
            throw new InvalidOperationException("Cookie 中缺少 bili_jct，无法移出稍后再看。");
        }

        using var request = CreateWebRequest(HttpMethod.Post, RemoveFromViewLaterUrl, cookie);
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

    public Task FollowCreatorAsync(long mid, CancellationToken cancellationToken = default)
    {
        return ModifyCreatorRelationAsync(mid, isFollow: true, cancellationToken);
    }

    public Task UnfollowCreatorAsync(long mid, CancellationToken cancellationToken = default)
    {
        return ModifyCreatorRelationAsync(mid, isFollow: false, cancellationToken);
    }

    public async Task<bool> IsCreatorFollowedAsync(long mid, CancellationToken cancellationToken = default)
    {
        if (mid <= 0)
        {
            return false;
        }

        var cookie = _cookieStore.GetCookieString();
        if (string.IsNullOrWhiteSpace(cookie))
        {
            return false;
        }

        using var document = await SendJsonAsync($"{RelationUrl}?fid={mid}", cookie, cancellationToken).ConfigureAwait(false);
        EnsureBiliSuccess(document.RootElement);

        if (!document.RootElement.TryGetProperty("data", out var data))
        {
            return false;
        }

        var attribute = GetInt32(data, "attribute");
        var special = GetInt32(data, "special");
        return special == 1 || attribute is 2 or 6;
    }

    private async Task ModifyCreatorRelationAsync(long mid, bool isFollow, CancellationToken cancellationToken)
    {
        if (mid <= 0)
        {
            throw new InvalidOperationException(isFollow ? "当前 UP 主缺少 mid，暂时无法关注。" : "当前 UP 主缺少 mid，暂时无法取消关注。");
        }

        var cookie = _cookieStore.GetCookieString();
        if (string.IsNullOrWhiteSpace(cookie))
        {
            throw new InvalidOperationException("请先保存 B 站 Cookie。");
        }

        var csrf = GetCookieValue(cookie, "bili_jct");
        if (string.IsNullOrWhiteSpace(csrf))
        {
            throw new InvalidOperationException(isFollow ? "Cookie 中缺少 bili_jct，无法关注。" : "Cookie 中缺少 bili_jct，无法取消关注。");
        }

        using var request = CreateWebRequest(HttpMethod.Post, ModifyRelationUrl, cookie);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["fid"] = mid.ToString(),
            ["act"] = isFollow ? "1" : "2",
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

    private async Task<BiliVideoUpdatePage> TryGetVideoUpdatesAsync(string url, string cookie, CancellationToken cancellationToken)
    {
        using var document = await SendJsonAsync(url, cookie, cancellationToken).ConfigureAwait(false);
        EnsureBiliSuccess(document.RootElement);

        if (!document.RootElement.TryGetProperty("data", out var data)
            || !data.TryGetProperty("items", out var items)
            || items.ValueKind != JsonValueKind.Array)
        {
            return new BiliVideoUpdatePage(Array.Empty<BiliVideoUpdate>(), string.Empty, false);
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

        var nextOffset = GetString(data, "offset");
        var hasMore = GetBool(data, "has_more");
        return new BiliVideoUpdatePage(updates, nextOffset, hasMore);
    }

    private static string CreateAllMomentsUrl(string offset = "")
    {
        var url = $"{AllMomentsUrl}?type=video&timezone_offset=-480";
        return string.IsNullOrWhiteSpace(offset)
            ? url
            : $"{url}&offset={Uri.EscapeDataString(offset)}";
    }

    private static string CreateHistoryCursorUrl(long max = 0, long viewAt = 0)
    {
        return $"{HistoryCursorUrl}?max={max}&view_at={viewAt}&business=archive";
    }

    private static string CreateViewLaterListUrl(int pageNumber)
    {
        return $"{ViewLaterListUrl}?pn={pageNumber}&ps=40";
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

    private async Task<BiliVideoHistoryPage> TryGetVideoHistoryAsync(string url, string cookie, CancellationToken cancellationToken)
    {
        using var document = await SendJsonAsync(url, cookie, cancellationToken).ConfigureAwait(false);
        EnsureBiliSuccess(document.RootElement);

        if (!document.RootElement.TryGetProperty("data", out var data)
            || !data.TryGetProperty("list", out var list)
            || list.ValueKind != JsonValueKind.Array)
        {
            return new BiliVideoHistoryPage(Array.Empty<BiliVideoUpdate>(), 0, 0, false);
        }

        var items = new List<BiliVideoUpdate>();
        foreach (var item in list.EnumerateArray())
        {
            var historyItem = TryCreateVideoHistoryItem(item);
            if (historyItem is not null)
            {
                items.Add(historyItem);
            }
        }

        var cursor = data.TryGetProperty("cursor", out var cursorElement) ? cursorElement : default;
        var nextMax = GetInt64(cursor, "max");
        var nextViewAt = GetInt64(cursor, "view_at");
        var hasMore = GetBool(data, "has_more")
            || GetBool(cursor, "has_more")
            || (items.Count > 0 && nextMax > 0 && nextMax != _historyNextMax);

        return new BiliVideoHistoryPage(items, nextMax, nextViewAt, hasMore);
    }

    private static BiliVideoUpdate? TryCreateVideoHistoryItem(JsonElement item)
    {
        if (!item.TryGetProperty("history", out var history))
        {
            return null;
        }

        var business = GetString(history, "business");
        if (!string.IsNullOrWhiteSpace(business) && !string.Equals(business, "archive", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var title = GetString(item, "title");
        var bvid = GetString(history, "bvid");
        var aid = GetInt64(history, "oid");
        if (aid <= 0)
        {
            aid = GetInt64(item, "aid");
        }

        var id = !string.IsNullOrWhiteSpace(bvid) ? bvid : aid.ToString();
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(title))
        {
            return null;
        }

        var creatorName = GetString(item, "author_name");
        var creatorMid = GetInt64(item, "author_mid");
        var avatar = NormalizeUrl(GetString(item, "author_face"));
        var cover = NormalizeUrl(GetString(item, "cover"));
        var durationSeconds = GetInt32(item, "duration");
        var progressSeconds = GetInt32(item, "progress");
        var viewAtTimestamp = GetInt64(item, "view_at");
        var viewedAt = viewAtTimestamp > 0
            ? DateTimeOffset.FromUnixTimeSeconds(viewAtTimestamp).ToLocalTime()
            : DateTimeOffset.Now;
        var url = NormalizeUrl(GetString(item, "uri"));
        if (string.IsNullOrWhiteSpace(url) || !url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            url = !string.IsNullOrWhiteSpace(bvid)
                ? $"https://www.bilibili.com/video/{bvid}"
                : $"https://www.bilibili.com/av{aid}";
        }

        return new BiliVideoUpdate(
            id,
            aid,
            creatorMid,
            creatorName,
            title,
            viewedAt,
            url,
            false,
            cover,
            avatar,
            $"{FormatRelativeTime(viewedAt)}观看",
            FormatDuration(durationSeconds),
            FormatProgress(progressSeconds, durationSeconds),
            0,
            0);
    }

    private async Task<BiliViewLaterPage> TryGetViewLaterAsync(string url, string cookie, int loadedCount, CancellationToken cancellationToken)
    {
        using var document = await SendJsonAsync(url, cookie, cancellationToken).ConfigureAwait(false);
        EnsureBiliSuccess(document.RootElement);

        if (!document.RootElement.TryGetProperty("data", out var data)
            || !data.TryGetProperty("list", out var list)
            || list.ValueKind != JsonValueKind.Array)
        {
            return new BiliViewLaterPage(Array.Empty<BiliVideoUpdate>(), 0, _viewLaterNextPageNumber, false);
        }

        var items = new List<BiliVideoUpdate>();
        foreach (var item in list.EnumerateArray())
        {
            var video = TryCreateViewLaterItem(item);
            if (video is not null)
            {
                items.Add(video);
            }
        }

        var totalCount = GetInt32(data, "count");
        var nextPageNumber = _viewLaterNextPageNumber + 1;
        var hasMore = items.Count > 0 && loadedCount + items.Count < totalCount;
        return new BiliViewLaterPage(items, totalCount, nextPageNumber, hasMore);
    }

    private static BiliVideoUpdate? TryCreateViewLaterItem(JsonElement item)
    {
        var title = GetString(item, "title");
        var bvid = GetString(item, "bvid");
        var aid = GetInt64(item, "aid");
        var id = !string.IsNullOrWhiteSpace(bvid) ? bvid : aid.ToString();
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(title))
        {
            return null;
        }

        var owner = item.TryGetProperty("owner", out var ownerElement) ? ownerElement : default;
        var creatorName = GetString(owner, "name");
        var creatorMid = GetInt64(owner, "mid");
        var avatar = NormalizeUrl(GetString(owner, "face"));
        var cover = NormalizeUrl(GetString(item, "pic"));
        var durationSeconds = GetInt32(item, "duration");
        var progressSeconds = GetInt32(item, "progress");
        var addAtTimestamp = GetInt64(item, "add_at");
        var addedAt = addAtTimestamp > 0
            ? DateTimeOffset.FromUnixTimeSeconds(addAtTimestamp).ToLocalTime()
            : DateTimeOffset.Now;
        var url = NormalizeUrl(GetString(item, "redirect_url"));
        if (string.IsNullOrWhiteSpace(url) || !url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            url = !string.IsNullOrWhiteSpace(bvid)
                ? $"https://www.bilibili.com/video/{bvid}"
                : $"https://www.bilibili.com/av{aid}";
        }

        var stat = item.TryGetProperty("stat", out var statElement) ? statElement : default;
        return new BiliVideoUpdate(
            id,
            aid,
            creatorMid,
            creatorName,
            title,
            addedAt,
            url,
            false,
            cover,
            avatar,
            $"{FormatRelativeTime(addedAt)}添加",
            FormatDuration(durationSeconds),
            FormatProgress(progressSeconds, durationSeconds),
            GetInt32(stat, "like"),
            GetInt32(stat, "reply"));
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
        for (var attempt = 1; attempt <= RequestMaxAttemptCount; attempt++)
        {
            try
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
            catch (HttpRequestException) when (attempt < RequestMaxAttemptCount)
            {
                await Task.Delay(RequestRetryDelay * attempt, cancellationToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested && attempt < RequestMaxAttemptCount)
            {
                await Task.Delay(RequestRetryDelay * attempt, cancellationToken).ConfigureAwait(false);
            }
        }

        throw new InvalidOperationException("B 站接口请求失败，请稍后重试。");
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
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(propertyName, out var value))
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
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(propertyName, out var value))
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
        => element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out var value) ? value.GetString() ?? string.Empty : string.Empty;

    private static bool GetBool(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(propertyName, out var value))
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

    private static bool TryGetLiveList(JsonElement data, out JsonElement list)
    {
        list = default;
        if (data.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        foreach (var propertyName in new[] { "rooms", "list", "items" })
        {
            if (data.TryGetProperty(propertyName, out list) && list.ValueKind == JsonValueKind.Array)
            {
                return true;
            }
        }

        return false;
    }

    private static BiliLiveCreator? TryCreateLiveCreator(JsonElement item)
    {
        var room = GetObject(item, "room_info");
        var user = GetObject(item, "uinfo");
        if (user.ValueKind == JsonValueKind.Undefined)
        {
            user = GetObject(item, "user");
        }

        var liveStatus = GetFirstInt32(item, room, "live_status", "status");
        if (liveStatus == 0
            && (HasProperty(item, "live_status") || HasProperty(room, "live_status") || HasProperty(item, "status") || HasProperty(room, "status")))
        {
            return null;
        }

        var isLiving = GetFirstBool(item, room, "is_live", "is_living");
        if (isLiving == false)
        {
            return null;
        }

        var roomId = GetFirstInt64(item, room, "room_id", "roomid", "id");
        var mid = GetFirstInt64(item, user, "uid", "mid", "uname_mid");
        var name = GetFirstString(item, user, "uname", "name", "username");
        var avatar = NormalizeUrl(GetFirstString(item, user, "face", "uface", "avatar"));
        var title = GetFirstString(item, room, "title", "room_title");

        if (roomId <= 0 || string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var url = $"https://live.bilibili.com/{roomId}";
        return new BiliLiveCreator(mid, roomId, name, avatar, title, url);
    }

    private static JsonElement GetObject(JsonElement element, string propertyName)
        => element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(propertyName, out var value)
            && value.ValueKind == JsonValueKind.Object
                ? value
                : default;

    private static bool HasProperty(JsonElement element, string propertyName)
        => element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out _);

    private static int GetFirstInt32(JsonElement first, JsonElement second, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            var value = GetInt32(first, propertyName);
            if (value != 0)
            {
                return value;
            }

            value = GetInt32(second, propertyName);
            if (value != 0)
            {
                return value;
            }
        }

        return 0;
    }

    private static long GetFirstInt64(JsonElement first, JsonElement second, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            var value = GetInt64(first, propertyName);
            if (value != 0)
            {
                return value;
            }

            value = GetInt64(second, propertyName);
            if (value != 0)
            {
                return value;
            }
        }

        return 0;
    }

    private static bool? GetFirstBool(JsonElement first, JsonElement second, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (HasProperty(first, propertyName))
            {
                return GetBool(first, propertyName);
            }

            if (HasProperty(second, propertyName))
            {
                return GetBool(second, propertyName);
            }
        }

        return null;
    }

    private static string GetFirstString(JsonElement first, JsonElement second, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            var value = GetString(first, propertyName);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            value = GetString(second, propertyName);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return string.Empty;
    }

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
        if (author.ValueKind != JsonValueKind.Object
            || !author.TryGetProperty("pub_ts", out var value)
            || !value.TryGetInt64(out var timestamp)
            || timestamp <= 0)
        {
            return false;
        }

        publishedAt = DateTimeOffset.FromUnixTimeSeconds(timestamp).ToLocalTime();
        return true;
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

    private static string FormatDuration(int seconds)
    {
        if (seconds <= 0)
        {
            return string.Empty;
        }

        var duration = TimeSpan.FromSeconds(seconds);
        return duration.TotalHours >= 1
            ? $"{(int)duration.TotalHours}:{duration.Minutes:00}:{duration.Seconds:00}"
            : $"{duration.Minutes}:{duration.Seconds:00}";
    }

    private static string FormatProgress(int progressSeconds, int durationSeconds)
    {
        if (progressSeconds < 0)
        {
            return "已看完";
        }

        if (progressSeconds <= 0)
        {
            return string.Empty;
        }

        if (durationSeconds > 0 && progressSeconds >= durationSeconds)
        {
            return "已看完";
        }

        return $"看到 {FormatDuration(progressSeconds)}";
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
