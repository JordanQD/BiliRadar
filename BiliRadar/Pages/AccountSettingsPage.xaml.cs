using System;
using BiliRadar.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace BiliRadar.Pages;

public sealed partial class AccountSettingsPage : Page
{
    private readonly CookieStore _cookieStore = new();
    private readonly BiliAccountService _accountService;
    private WebSignInWindow? _signInWindow;

    public AccountSettingsPage()
    {
        _accountService = new BiliAccountService(_cookieStore);
        InitializeComponent();
        Loaded += AccountSettingsPage_Loaded;
        Unloaded += AccountSettingsPage_Unloaded;
    }

    private async void AccountSettingsPage_Loaded(object sender, RoutedEventArgs e)
    {
        SettingsScrollViewer.ChangeView(null, 0, null, true);
        await RefreshAccountStatusAsync();
    }

    private void AccountSettingsPage_Unloaded(object sender, RoutedEventArgs e)
    {
        Loaded -= AccountSettingsPage_Loaded;
        Unloaded -= AccountSettingsPage_Unloaded;
        _accountService.Dispose();
        if (_signInWindow is not null)
        {
            _signInWindow.SignInSucceeded -= SignInWindow_SignInSucceeded;
            _signInWindow = null;
        }
    }

    private void SignInButton_Click(object sender, RoutedEventArgs e)
    {
        if (_signInWindow is null)
        {
            _signInWindow = new WebSignInWindow(_cookieStore);
            _signInWindow.SignInSucceeded += SignInWindow_SignInSucceeded;
            _signInWindow.Closed += SignInWindow_Closed;
        }

        _signInWindow.Activate();
    }

    private async void RefreshStatusButton_Click(object sender, RoutedEventArgs e)
    {
        await RefreshAccountStatusAsync();
    }

    private async void SignOutButton_Click(object sender, RoutedEventArgs e)
    {
        using var authService = new BiliKernelAuthService(_cookieStore);
        await authService.SignOutAsync();
        await RefreshAccountStatusAsync();
    }

    private async void SignInWindow_SignInSucceeded(object? sender, EventArgs e)
    {
        await RefreshAccountStatusAsync();
    }

    private void SignInWindow_Closed(object sender, WindowEventArgs args)
    {
        if (_signInWindow is not null)
        {
            _signInWindow.SignInSucceeded -= SignInWindow_SignInSucceeded;
            _signInWindow.Closed -= SignInWindow_Closed;
            _signInWindow = null;
        }
    }

    private async System.Threading.Tasks.Task RefreshAccountStatusAsync()
    {
        SignOutButton.IsEnabled = _cookieStore.HasCookie;

        if (!_cookieStore.HasCookie)
        {
            AccountInfoBar.Severity = InfoBarSeverity.Informational;
            AccountInfoBar.Message = "尚未登录。";
            AccountNameText.Text = "未登录";
            AccountDetailText.Text = "网页登录后会自动换取更稳定的登录凭据，用于刷新关注动态。";
            AvatarPicture.ProfilePicture = null;
            AvatarPicture.Initials = "BR";
            return;
        }

        try
        {
            AccountInfoBar.Severity = InfoBarSeverity.Informational;
            AccountInfoBar.Message = "正在检查登录状态...";

            var profile = await _accountService.GetCurrentProfileAsync();
            if (profile is null)
            {
                AccountInfoBar.Severity = InfoBarSeverity.Warning;
                AccountInfoBar.Message = "已保存 Cookie，但登录状态无效或已过期。";
                AccountNameText.Text = "登录状态失效";
                AccountDetailText.Text = "请重新登录。";
                AvatarPicture.ProfilePicture = null;
                AvatarPicture.Initials = "BR";
                return;
            }

            AccountInfoBar.Severity = InfoBarSeverity.Success;
            AccountInfoBar.Message = "已登录。";
            AccountNameText.Text = profile.Name;
            AccountDetailText.Text = $"UID {profile.Mid}";
            AvatarPicture.Initials = string.IsNullOrWhiteSpace(profile.Name) ? "BR" : profile.Name[..1];
            AvatarPicture.ProfilePicture = string.IsNullOrWhiteSpace(profile.AvatarUrl)
                ? null
                : new BitmapImage(new Uri(profile.AvatarUrl));
        }
        catch (Exception ex)
        {
            AccountInfoBar.Severity = InfoBarSeverity.Warning;
            AccountInfoBar.Message = $"登录状态检查失败：{ex.Message}";
            AccountNameText.Text = "状态未知";
            AccountDetailText.Text = "Cookie 已保存，但暂时无法验证。";
            AvatarPicture.ProfilePicture = null;
            AvatarPicture.Initials = "BR";
        }
    }
}
