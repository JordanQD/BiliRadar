using BiliRadar.Helpers;
using BiliRadar.Services;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Graphics;
using WinRT.Interop;

namespace BiliRadar;

public sealed partial class WebSignInWindow : Window
{
    private const double StandardTitleBarHeight = 32;

    private readonly CookieStore _cookieStore;
    private readonly BiliKernelAuthService _authService;
    private readonly AppWindow _appWindow;
    private bool _isCookieSaved;

    public WebSignInWindow(CookieStore cookieStore)
    {
        _cookieStore = cookieStore;
        _authService = new BiliKernelAuthService(_cookieStore);
        InitializeComponent();

        var hwnd = WindowNative.GetWindowHandle(this);
        _appWindow = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(hwnd));
        _appWindow.Title = LocalizationHelper.GetString("WebSignInWindow.Title", "BiliRadar 登录");
        WindowIconHelper.ApplyTo(_appWindow, hwnd);
        _appWindow.Resize(new SizeInt32(1200, 720));
        ConfigureTitleBar(hwnd);

        mainView.Loaded += MainView_Loaded;
        Closed += WebSignInWindow_Closed;
    }

    public event EventHandler? SignInSucceeded;

    private async void MainView_Loaded(object sender, RoutedEventArgs e)
    {
        mainView.Loaded -= MainView_Loaded;
        try
        {
            await mainView.EnsureCoreWebView2Async();
            mainView.CoreWebView2.CookieManager.DeleteAllCookies();
            mainView.CoreWebView2.Navigate("https://passport.bilibili.com/login");
        }
        catch (Exception ex)
        {
            ShowStatus(LocalizationHelper.Format("LoginWindowInitFailed", ex.Message), InfoBarSeverity.Error);
        }
    }

    private async void MainView_NavigationCompleted(WebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs args)
    {
        if (!args.IsSuccess || sender.CoreWebView2 is null)
        {
            return;
        }

        var source = sender.CoreWebView2.Source ?? string.Empty;
        if (source.StartsWith("https://passport.bilibili.com", StringComparison.OrdinalIgnoreCase))
        {
            await TryCleanPassportPageAsync(sender);
            return;
        }

        if (!source.StartsWith("https://www.bilibili.com", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await TrySaveCookiesAsync(sender, source);
    }

    private async Task TrySaveCookiesAsync(WebView2 sender, string source)
    {
        if (_isCookieSaved || sender.CoreWebView2 is null)
        {
            return;
        }

        var cookies = await sender.CoreWebView2.CookieManager.GetCookiesAsync(source);
        var cookiePairs = cookies
            .Where(cookie => !string.IsNullOrWhiteSpace(cookie.Name))
            .GroupBy(cookie => cookie.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Last().Value ?? string.Empty, StringComparer.OrdinalIgnoreCase);

        if (!HasLoginCookies(cookiePairs))
        {
            return;
        }

        ShowStatus(LocalizationHelper.GetString("LoginSuccessExchanging"), InfoBarSeverity.Informational);
        await _authService.SignInWithCookiesAsync(cookiePairs);
        _isCookieSaved = true;
        ShowStatus(LocalizationHelper.GetString("LoginSuccessSaved"), InfoBarSeverity.Success);
        SignInSucceeded?.Invoke(this, EventArgs.Empty);
        Close();
    }

    private static bool HasLoginCookies(IReadOnlyDictionary<string, string> cookies)
        => cookies.ContainsKey("SESSDATA")
            && cookies.ContainsKey("bili_jct")
            && cookies.ContainsKey("DedeUserID");

    private static async Task TryCleanPassportPageAsync(WebView2 sender)
    {
        const string script = """
            document.querySelector('.international-footer')?.remove();
            document.querySelector('.top-header')?.remove();
            """;

        try
        {
            await sender.CoreWebView2.ExecuteScriptAsync(script);
        }
        catch
        {
        }
    }

    private void ConfigureTitleBar(IntPtr hwnd)
    {
        this.ExtendsContentIntoTitleBar = true;

        var appTitleBar = _appWindow.TitleBar;
        appTitleBar.ExtendsContentIntoTitleBar = true;
        appTitleBar.PreferredHeightOption = TitleBarHeightOption.Standard;

        var scale = GetDpiForWindow(hwnd) / 96.0;
        var titleBarHeight = appTitleBar.Height > 0
            ? appTitleBar.Height / scale
            : StandardTitleBarHeight;

        titleBar.Height = titleBarHeight;
        titleBar.MinHeight = titleBarHeight;
        this.SetTitleBar(titleBar);
    }

    private void ShowStatus(string message, InfoBarSeverity severity)
    {
        statusInfoBar.Message = message;
        statusInfoBar.Severity = severity;
        statusInfoBar.IsOpen = true;
    }

    private void WebSignInWindow_Closed(object sender, WindowEventArgs args)
    {
        Closed -= WebSignInWindow_Closed;
        _authService.Dispose();
    }

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);
}
