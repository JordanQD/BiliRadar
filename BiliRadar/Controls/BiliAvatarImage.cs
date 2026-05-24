using System;
using System.Collections.Generic;
using System.Net.Http;
using BiliRadar.Services;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Richasy.WinUIKernel.Share.Base;
using Windows.Foundation;

namespace BiliRadar.Controls;

/// <summary>
/// A <see cref="ImageExBase"/> subclass that adds Bilibili referer / user-agent
/// headers so avatar images load correctly from Bilibili CDN.
/// </summary>
public sealed class BiliAvatarImage : ImageExBase
{
    public BiliAvatarImage()
    {
        DecodeWidth = 48;
        IsShimmerEnabled = false;
    }

    protected override HttpClient? GetCustomHttpClient()
    {
        var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = true });
        client.DefaultRequestHeaders.Add("User-Agent", BiliWebDataProvider.BrowserUserAgent);
        client.DefaultRequestHeaders.Add("Referer", "https://www.bilibili.com/");
        return client;
    }

    protected override Dictionary<string, string> GetHeaders()
    {
        return new Dictionary<string, string>
        {
            ["User-Agent"] = BiliWebDataProvider.BrowserUserAgent,
            ["Referer"] = "https://www.bilibili.com/",
        };
    }

    protected override string GetCacheSubFolder() => "AvatarCache";

    /// <inheritdoc/>
    protected override void DrawImage(CanvasBitmap canvasBitmap)
    {
        var width = canvasBitmap.Size.Width;
        var height = canvasBitmap.Size.Height;
        var aspectRatio = width / height;
        var actualHeight = Math.Round(DecodeWidth / aspectRatio);

        if (Math.Abs(DecodeHeight - actualHeight) > 1)
        {
            DecodeHeight = actualHeight;
            CanvasImageSource = new CanvasImageSource(
                resourceCreator: CanvasDevice.GetSharedDevice(),
                width: (float)DecodeWidth,
                height: (float)DecodeHeight,
                dpi: 96,
                CanvasAlphaMode.Premultiplied);
        }

        // Center-crop: draw the image filling the destination rect
        var destRect = new Rect(0, 0, DecodeWidth, DecodeHeight);
        var sourceRect = GetCenterCropRect(
            new Rect(0, 0, width, height),
            new Rect(0, 0, DecodeWidth, DecodeHeight));

        using var ds = CanvasImageSource!.CreateDrawingSession(Colors.Transparent);
        ds.DrawImage(canvasBitmap, destRect, sourceRect);
    }
}
