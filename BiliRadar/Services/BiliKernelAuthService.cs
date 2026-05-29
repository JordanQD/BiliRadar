// Portions of this file are derived from Richasy/bili-kernel.
// Copyright (c) Richasy.
// Licensed under the GNU General Public License v3.0.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace BiliRadar.Services;

internal sealed class BiliKernelAuthService : IDisposable
{
    private const string QrCodeUrl = "https://passport.bilibili.com/x/passport-tv-login/qrcode/auth_code";
    private const string QrCodeConfirmUrl = "https://passport.bilibili.com/x/passport-tv-login/h5/qrcode/confirm";
    private const string QrCodePollUrl = "https://passport.bilibili.com/x/passport-tv-login/qrcode/poll";
    private const string AppKey = "27eb53fc9058f8c3";
    private const string AppSecret = "c2ed53a74eeefe3cf99fbd01d8c9c375";
    private const string BuildNumber = "80200100";
    private const string ContainerName = "BiliRadar";
    private const string TokenKey = "BiliKernelToken";
    private static readonly TimeSpan PollDelay = TimeSpan.FromSeconds(1);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly CookieStore _cookieStore;
    private readonly HttpClient _httpClient = new();

    public BiliKernelAuthService(CookieStore cookieStore)
    {
        _cookieStore = cookieStore;
    }

    public async Task SignInWithCookiesAsync(IReadOnlyDictionary<string, string> cookies, CancellationToken cancellationToken = default)
    {
        if (!HasRequiredWebCookies(cookies))
        {
            throw new InvalidOperationException("网页登录 Cookie 不完整，请重新登录。");
        }

        _cookieStore.SaveCookieString(ToCookieString(cookies));

        var localId = Guid.NewGuid().ToString("N");
        var qrCode = await GetQRCodeAsync(localId, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(qrCode.AuthCode))
        {
            throw new InvalidOperationException("B 站没有返回 TV 授权码，请重新登录。");
        }

        await ConfirmQRCodeAsync(qrCode.AuthCode, cancellationToken).ConfigureAwait(false);
        var token = await PollTokenAsync(qrCode.AuthCode, localId, cancellationToken).ConfigureAwait(false);
        SaveToken(token);
        SaveCookiesFromToken(token);
    }

    public Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        _cookieStore.Clear();
        RemoveToken();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    private Task<TvQRCode> GetQRCodeAsync(string localId, CancellationToken cancellationToken)
        => SendBiliRequestAsync<TvQRCode>(
            QrCodeUrl,
            new Dictionary<string, string>
            {
                ["local_id"] = localId,
            },
            cancellationToken);

    private Task ConfirmQRCodeAsync(string authCode, CancellationToken cancellationToken)
        => SendBiliStatusAsync(
            QrCodeConfirmUrl,
            new Dictionary<string, string>
            {
                ["auth_code"] = authCode,
            },
            cancellationToken,
            requireCookie: true,
            requireCsrf: true);

    private async Task<BiliToken> PollTokenAsync(string authCode, string localId, CancellationToken cancellationToken)
    {
        while (true)
        {
            var result = await SendBiliCoreRequestAsync<BiliToken>(
                QrCodePollUrl,
                new Dictionary<string, string>
                {
                    ["auth_code"] = authCode,
                    ["local_id"] = localId,
                    ["guid"] = Guid.NewGuid().ToString("N"),
                },
                cancellationToken,
                throwOnBiliError: false).ConfigureAwait(false);

            if (result.Response.Code == 0 && result.Data is not null)
            {
                return NormalizeTokenExpiration(result.Data);
            }

            switch (result.Response.Code)
            {
                case 86090:
                case 86039:
                    await Task.Delay(PollDelay, cancellationToken).ConfigureAwait(false);
                    break;
                case 86038:
                case -3:
                    throw new InvalidOperationException("登录二维码已过期，请重新登录。");
                default:
                    throw CreateBiliException(result.Response);
            }
        }
    }

    private async Task<T> SendBiliRequestAsync<T>(
        string url,
        Dictionary<string, string> parameters,
        CancellationToken cancellationToken,
        bool requireCookie = false,
        bool requireCsrf = false)
    {
        var result = await SendBiliCoreRequestAsync<T>(
            url,
            parameters,
            cancellationToken,
            requireCookie,
            requireCsrf,
            throwOnBiliError: true).ConfigureAwait(false);
        return result.Data ?? throw new InvalidOperationException("B 站接口没有返回有效数据。");
    }

    private async Task SendBiliStatusAsync(
        string url,
        Dictionary<string, string> parameters,
        CancellationToken cancellationToken,
        bool requireCookie = false,
        bool requireCsrf = false)
    {
        var result = await SendBiliCoreRequestAsync<JsonElement>(
            url,
            parameters,
            cancellationToken,
            requireCookie,
            requireCsrf,
            throwOnBiliError: true).ConfigureAwait(false);
        if (result.Response.Code != 0)
        {
            throw CreateBiliException(result.Response);
        }
    }

    private async Task<BiliRequestResult<T>> SendBiliCoreRequestAsync<T>(
        string url,
        Dictionary<string, string> parameters,
        CancellationToken cancellationToken,
        bool requireCookie = false,
        bool requireCsrf = false,
        bool throwOnBiliError = true)
    {
        AddAppParameters(parameters);
        if (requireCsrf)
        {
            var csrf = GetCookieValue(_cookieStore.GetCookieString(), "bili_jct");
            if (string.IsNullOrWhiteSpace(csrf))
            {
                throw new InvalidOperationException("Cookie 中缺少 bili_jct，无法完成登录确认。");
            }

            parameters["csrf"] = csrf;
        }

        parameters["sign"] = CreateSign(parameters);

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new FormUrlEncodedContent(parameters),
        };

        request.Headers.TryAddWithoutValidation("User-Agent", BiliWebDataProvider.BrowserUserAgent);
        request.Headers.TryAddWithoutValidation("Accept", "application/json, text/plain, */*");
        request.Headers.TryAddWithoutValidation("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8");
        request.Headers.TryAddWithoutValidation("Referer", "https://www.bilibili.com/");
        request.Headers.TryAddWithoutValidation("Origin", "https://www.bilibili.com");
        if (requireCookie)
        {
            request.Headers.TryAddWithoutValidation("Cookie", _cookieStore.GetCookieString());
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"HTTP {(int)response.StatusCode} {response.ReasonPhrase}: {TryGetBiliErrorMessage(body)}");
        }

        var biliResponse = JsonSerializer.Deserialize<BiliDataResponse<T>>(body, JsonOptions)
            ?? throw new InvalidOperationException("B 站接口返回内容为空。");
        if (throwOnBiliError && biliResponse.Code != 0)
        {
            throw CreateBiliException(biliResponse);
        }

        return new BiliRequestResult<T>(biliResponse, biliResponse.Data);
    }

    private static void AddAppParameters(IDictionary<string, string> parameters)
    {
        parameters.TryAdd("appkey", AppKey);
        parameters.TryAdd("build", BuildNumber);
        parameters.TryAdd("mobi_app", "iphone");
        parameters.TryAdd("platform", "ios");
        parameters.TryAdd("ts", DateTimeOffset.Now.ToUnixTimeSeconds().ToString());
    }

    private static string CreateSign(IDictionary<string, string> parameters)
    {
        var query = string.Join("&", parameters
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => $"{pair.Key}={pair.Value}"));
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(query + AppSecret));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static BiliToken NormalizeTokenExpiration(BiliToken token)
    {
        if (token.ExpiresIn > 0)
        {
            token.ExpiresIn = DateTimeOffset.Now.ToUnixTimeSeconds() + token.ExpiresIn;
        }

        return token;
    }

    private void SaveToken(BiliToken token)
    {
        GetContainer().Values[TokenKey] = JsonSerializer.Serialize(token, JsonOptions);
    }

    private void SaveCookiesFromToken(BiliToken token)
    {
        var cookies = token.CookieInfo?.Cookies?
            .Where(cookie => !string.IsNullOrWhiteSpace(cookie.Name))
            .ToDictionary(cookie => cookie.Name!, cookie => cookie.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase);
        if (cookies is null || cookies.Count == 0)
        {
            return;
        }

        _cookieStore.SaveCookieString(ToCookieString(cookies));
    }

    private static void RemoveToken()
    {
        GetContainer().Values.Remove(TokenKey);
    }

    private static ApplicationDataContainer GetContainer()
        => ApplicationData.Current.LocalSettings.CreateContainer(ContainerName, ApplicationDataCreateDisposition.Always);

    private static bool HasRequiredWebCookies(IReadOnlyDictionary<string, string> cookies)
        => cookies.ContainsKey("SESSDATA")
            && cookies.ContainsKey("bili_jct")
            && cookies.ContainsKey("DedeUserID");

    private static string ToCookieString(IReadOnlyDictionary<string, string> cookies)
        => string.Join("; ", cookies.Select(pair => $"{pair.Key}={pair.Value}"));

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

    private static Exception CreateBiliException(BiliResponse response)
        => new InvalidOperationException(string.IsNullOrWhiteSpace(response.Message)
            ? $"B 站接口返回错误：{response.Code}"
            : $"B 站接口返回错误：{response.Message} ({response.Code})");

    private static string TryGetBiliErrorMessage(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return "响应为空";
        }

        try
        {
            var response = JsonSerializer.Deserialize<BiliResponse>(body, JsonOptions);
            return string.IsNullOrWhiteSpace(response?.Message) ? body : response.Message;
        }
        catch
        {
            return body.Length > 160 ? body[..160] : body;
        }
    }

    private sealed record BiliRequestResult<T>(BiliResponse Response, T? Data);

    private class BiliResponse
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }

    private sealed class BiliDataResponse<T> : BiliResponse
    {
        [JsonPropertyName("data")]
        public T? Data { get; set; }
    }

    private sealed class TvQRCode
    {
        [JsonPropertyName("auth_code")]
        public string AuthCode { get; set; } = string.Empty;
    }

    private sealed class BiliToken
    {
        [JsonPropertyName("mid")]
        public long UserId { get; set; }

        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public long ExpiresIn { get; set; }

        [JsonPropertyName("cookie_info")]
        public BiliCookieInfo? CookieInfo { get; set; }
    }

    private sealed class BiliCookieInfo
    {
        [JsonPropertyName("cookies")]
        public List<BiliCookie>? Cookies { get; set; }
    }

    private sealed class BiliCookie
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("value")]
        public string? Value { get; set; }
    }
}
