using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using BiliRadar.Models;
using BiliRadar.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Win32;
using Microsoft.Windows.Storage.Pickers;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;

namespace BiliRadar.Pages;

public sealed partial class GeneralSettingsPage : Page
{
    private const string StartupTaskId = "BiliRadarStartupTask";
    private const string StartupRegistryName = "BiliRadar";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly CookieStore _cookieStore = new();
    private readonly BiliAccountService _accountService;
    private WebSignInWindow? _signInWindow;
    private bool _isLoadingSettings;
    private bool _useRegistryStartup;
    private StartupTask? _startupTask;

    public GeneralSettingsPage()
    {
        _accountService = new BiliAccountService(_cookieStore);
        InitializeComponent();
        Loaded += GeneralSettingsPage_Loaded;
        Unloaded += GeneralSettingsPage_Unloaded;
    }

    private async void GeneralSettingsPage_Loaded(object sender, RoutedEventArgs e)
    {
        _isLoadingSettings = true;
        RunningLaunchActionBox.SelectedIndex = AppSettings.RunningLaunchAction == RunningLaunchAction.OpenCustomWebPage ? 1 : 0;
        CustomLaunchWebPageUrlBox.Text = AppSettings.CustomLaunchWebPageUrl;
        DefaultOpenPageBox.SelectedIndex = (int)AppSettings.DefaultOpenPage;
        LiveSectionDisplayModeBox.SelectedIndex = (int)AppSettings.LiveSectionDisplayMode;
        UpdateCustomLaunchWebPageUrlBoxState();
        await LoadStartupStateAsync();
        _isLoadingSettings = false;

        SettingsScrollView.ScrollTo(0, 0);
        await RefreshAccountStatusAsync();
    }

    private void GeneralSettingsPage_Unloaded(object sender, RoutedEventArgs e)
    {
        Loaded -= GeneralSettingsPage_Loaded;
        Unloaded -= GeneralSettingsPage_Unloaded;
        _accountService.Dispose();
        if (_signInWindow is not null)
        {
            _signInWindow.SignInSucceeded -= SignInWindow_SignInSucceeded;
            _signInWindow.Closed -= SignInWindow_Closed;
            _signInWindow = null;
        }
    }

    private async System.Threading.Tasks.Task LoadStartupStateAsync()
    {
        try
        {
            _startupTask = await StartupTask.GetAsync(StartupTaskId);
            _useRegistryStartup = false;
            AutoStartSwitch.IsOn = _startupTask.State is StartupTaskState.Enabled;
            AutoStartSwitch.IsEnabled = _startupTask.State is StartupTaskState.Disabled or StartupTaskState.Enabled;
            StartupInfoBar.IsOpen = _startupTask.State is StartupTaskState.DisabledByUser or StartupTaskState.DisabledByPolicy;
            StartupInfoBar.Severity = InfoBarSeverity.Informational;
            StartupInfoBar.Message = _startupTask.State switch
            {
                StartupTaskState.DisabledByUser => "自启已被系统设置关闭，请在 Windows 启动应用设置中重新允许。",
                StartupTaskState.DisabledByPolicy => "自启已被系统策略禁用。",
                _ => string.Empty,
            };
            return;
        }
        catch
        {
        }

        _startupTask = null;
        _useRegistryStartup = true;
        AutoStartSwitch.IsEnabled = true;
        AutoStartSwitch.IsOn = IsRegistryStartupEnabled();
        StartupInfoBar.IsOpen = true;
        StartupInfoBar.Severity = InfoBarSeverity.Informational;
        StartupInfoBar.Message = "当前运行方式不支持应用启动任务，已改用当前用户启动项。";
    }

    private async void AutoStartSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isLoadingSettings)
        {
            return;
        }

        await SetAutoStartEnabledAsync(AutoStartSwitch.IsOn);
    }

    private async System.Threading.Tasks.Task SetAutoStartEnabledAsync(bool isEnabled)
    {
        if (_useRegistryStartup)
        {
            SetRegistryStartupEnabled(isEnabled);
            return;
        }

        if (_startupTask is null)
        {
            _isLoadingSettings = true;
            AutoStartSwitch.IsOn = false;
            _isLoadingSettings = false;
            return;
        }

        try
        {
            if (isEnabled)
            {
                var state = await _startupTask.RequestEnableAsync();
                if (state is not StartupTaskState.Enabled)
                {
                    _isLoadingSettings = true;
                    AutoStartSwitch.IsOn = false;
                    _isLoadingSettings = false;
                }
            }
            else if (_startupTask.State is StartupTaskState.Enabled)
            {
                _startupTask.Disable();
            }
        }
        catch
        {
            _isLoadingSettings = true;
            AutoStartSwitch.IsOn = _startupTask.State is StartupTaskState.Enabled;
            _isLoadingSettings = false;
        }
    }

    private void RunningLaunchActionBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoadingSettings)
        {
            return;
        }

        AppSettings.RunningLaunchAction = RunningLaunchActionBox.SelectedIndex == 1
            ? RunningLaunchAction.OpenCustomWebPage
            : RunningLaunchAction.OpenSettings;
        UpdateCustomLaunchWebPageUrlBoxState();
    }

    private void CustomLaunchWebPageUrlBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isLoadingSettings)
        {
            return;
        }

        AppSettings.CustomLaunchWebPageUrl = CustomLaunchWebPageUrlBox.Text;
    }

    private void CustomLaunchWebPageUrlBox_LostFocus(object sender, RoutedEventArgs e)
    {
        _isLoadingSettings = true;
        CustomLaunchWebPageUrlBox.Text = AppSettings.CustomLaunchWebPageUrl;
        _isLoadingSettings = false;
    }

    private void DefaultOpenPageBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoadingSettings)
        {
            return;
        }

        AppSettings.DefaultOpenPage = DefaultOpenPageBox.SelectedIndex switch
        {
            1 => DefaultOpenPage.History,
            2 => DefaultOpenPage.ViewLater,
            _ => DefaultOpenPage.Following,
        };
    }

    private void UpdateCustomLaunchWebPageUrlBoxState()
    {
        CustomLaunchWebPageUrlBox.IsEnabled = RunningLaunchActionBox.SelectedIndex == 1;
    }

    private void LiveSectionDisplayModeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoadingSettings)
        {
            return;
        }

        AppSettings.LiveSectionDisplayMode = LiveSectionDisplayModeBox.SelectedIndex switch
        {
            1 => LiveSectionDisplayMode.Collapsed,
            2 => LiveSectionDisplayMode.Hidden,
            _ => LiveSectionDisplayMode.Expanded,
        };
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
            AccountDetailText.Text = "网页登录后会保存登录状态，用于刷新关注动态。";
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

    private async void ExportConfigButton_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileSavePicker(((FrameworkElement)sender).XamlRoot.ContentIslandEnvironment.AppWindowId)
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = $"BiliRadar-config-{DateTime.Now:yyyyMMdd-HHmmss}",
        };
        picker.FileTypeChoices.Add("JSON 配置", [".json"]);

        var result = await picker.PickSaveFileAsync();
        if (result is null)
        {
            return;
        }

        try
        {
            var export = CreateSettingsExport();
            var json = JsonSerializer.Serialize(export, JsonOptions);
            await File.WriteAllTextAsync(result.Path, json);
            ConfigStatusText.Text = $"已导出到：{result.Path}";
        }
        catch (Exception ex)
        {
            ConfigStatusText.Text = $"导出失败：{ex.Message}";
        }
    }

    private async void ImportConfigButton_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker(((FrameworkElement)sender).XamlRoot.ContentIslandEnvironment.AppWindowId)
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
        };
        picker.FileTypeFilter.Add(".json");

        var result = await picker.PickSingleFileAsync();
        if (result is null)
        {
            return;
        }

        try
        {
            var json = await File.ReadAllTextAsync(result.Path);
            var import = JsonSerializer.Deserialize<SettingsExport>(json, JsonOptions)
                ?? throw new InvalidOperationException("配置文件为空或格式不正确。");

            await ApplySettingsExportAsync(import);
            ConfigStatusText.Text = $"已导入：{result.Path}";
        }
        catch (Exception ex)
        {
            ConfigStatusText.Text = $"导入失败：{ex.Message}";
        }
    }

    private SettingsExport CreateSettingsExport()
    {
        return new SettingsExport
        {
            ExportedAt = DateTimeOffset.Now,
            IsPackaged = IsPackaged(),
            AutoStartEnabled = AutoStartSwitch.IsOn,
            RunningLaunchActionValue = AppSettings.RunningLaunchAction.ToString(),
            CustomLaunchWebPageUrl = AppSettings.CustomLaunchWebPageUrl,
            DefaultOpenPageValue = AppSettings.DefaultOpenPage.ToString(),
            NotificationCheckIntervalMinutes = AppSettings.NotificationCheckIntervalMinutes,
            NotificationTargetMode = AppSettings.NotificationTargetMode.ToString(),
            LiveSectionDisplayMode = AppSettings.LiveSectionDisplayMode.ToString(),
            VideoNotificationsEnabled = AppSettings.VideoNotificationsEnabled,
            LiveNotificationsEnabled = AppSettings.LiveNotificationsEnabled,
            CustomNotificationCreators = AppSettings.CustomNotificationCreators.ToList(),
            KnownVideoUpdateIds = AppSettings.KnownVideoUpdateIds.ToList(),
            KnownLiveRoomIds = AppSettings.KnownLiveRoomIds.ToList(),
            Login = new LoginExport
            {
                HasCookie = _cookieStore.HasCookie,
                Cookie = _cookieStore.GetCookieString(),
            },
        };
    }

    private async System.Threading.Tasks.Task ApplySettingsExportAsync(SettingsExport import)
    {
        AppSettings.RunningLaunchAction = ParseRunningLaunchAction(import.RunningLaunchActionValue);
        AppSettings.CustomLaunchWebPageUrl = import.CustomLaunchWebPageUrl;
        AppSettings.DefaultOpenPage = ParseEnum(import.DefaultOpenPageValue, DefaultOpenPage.Following);
        AppSettings.NotificationCheckIntervalMinutes = Math.Max(1, import.NotificationCheckIntervalMinutes);
        AppSettings.NotificationTargetMode = ParseEnum(import.NotificationTargetMode, NotificationTargetMode.AllFollowing);
        AppSettings.VideoNotificationsEnabled = import.VideoNotificationsEnabled;
        AppSettings.LiveNotificationsEnabled = import.LiveNotificationsEnabled;
        AppSettings.LiveSectionDisplayMode = ParseEnum(import.LiveSectionDisplayMode, LiveSectionDisplayMode.Expanded);
        AppSettings.CustomNotificationCreators = import.CustomNotificationCreators ?? [];
        AppSettings.KnownVideoUpdateIds = import.KnownVideoUpdateIds ?? [];
        AppSettings.KnownLiveRoomIds = import.KnownLiveRoomIds ?? [];

        if (import.Login?.HasCookie == true && !string.IsNullOrWhiteSpace(import.Login.Cookie))
        {
            _cookieStore.SaveCookieString(import.Login.Cookie);
        }
        else
        {
            _cookieStore.Clear();
        }

        _isLoadingSettings = true;
        RunningLaunchActionBox.SelectedIndex = AppSettings.RunningLaunchAction == RunningLaunchAction.OpenCustomWebPage ? 1 : 0;
        CustomLaunchWebPageUrlBox.Text = AppSettings.CustomLaunchWebPageUrl;
        DefaultOpenPageBox.SelectedIndex = (int)AppSettings.DefaultOpenPage;
        LiveSectionDisplayModeBox.SelectedIndex = (int)AppSettings.LiveSectionDisplayMode;
        UpdateCustomLaunchWebPageUrlBoxState();
        AutoStartSwitch.IsOn = import.AutoStartEnabled;
        _isLoadingSettings = false;

        await SetAutoStartEnabledAsync(import.AutoStartEnabled);
        await RefreshAccountStatusAsync();
    }

    private static bool IsRegistryStartupEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", writable: false);
        return key?.GetValue(StartupRegistryName) is string value
            && string.Equals(value, GetRegistryStartupCommand(), StringComparison.OrdinalIgnoreCase);
    }

    private static void SetRegistryStartupEnabled(bool isEnabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
        if (isEnabled)
        {
            key?.SetValue(StartupRegistryName, GetRegistryStartupCommand());
        }
        else
        {
            key?.DeleteValue(StartupRegistryName, throwOnMissingValue: false);
        }
    }

    private static string GetRegistryStartupCommand()
    {
        var path = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            path = Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
        }

        return $"\"{path}\"";
    }

    private static TEnum ParseEnum<TEnum>(string? value, TEnum fallback)
        where TEnum : struct, Enum
    {
        return Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed)
            ? parsed
            : fallback;
    }

    private static RunningLaunchAction ParseRunningLaunchAction(string? value)
    {
        if (string.Equals(value, "OpenBilibiliWebPage", StringComparison.OrdinalIgnoreCase))
        {
            return RunningLaunchAction.OpenCustomWebPage;
        }

        return ParseEnum(value, RunningLaunchAction.OpenSettings);
    }

    private static bool IsPackaged()
    {
        try
        {
            _ = Package.Current.Id.Name;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private sealed class LoginExport
    {
        public bool HasCookie { get; set; }

        public string Cookie { get; set; } = string.Empty;
    }

    private sealed class SettingsExport
    {
        public DateTimeOffset ExportedAt { get; set; }

        public bool IsPackaged { get; set; }

        public bool AutoStartEnabled { get; set; }

        [JsonPropertyName("RunningLaunchAction")]
        public string RunningLaunchActionValue { get; set; } = nameof(Services.RunningLaunchAction.OpenSettings);

        public string CustomLaunchWebPageUrl { get; set; } = "https://www.bilibili.com/";

        [JsonPropertyName("DefaultOpenPage")]
        public string DefaultOpenPageValue { get; set; } = nameof(Services.DefaultOpenPage.Following);

        public int NotificationCheckIntervalMinutes { get; set; } = 15;

        public string NotificationTargetMode { get; set; } = nameof(Services.NotificationTargetMode.AllFollowing);

        public string LiveSectionDisplayMode { get; set; } = nameof(Services.LiveSectionDisplayMode.Expanded);

        public bool VideoNotificationsEnabled { get; set; } = true;

        public bool LiveNotificationsEnabled { get; set; } = true;

        public List<NotificationCreatorSubscription> CustomNotificationCreators { get; set; } = [];

        public List<string> KnownVideoUpdateIds { get; set; } = [];

        public List<string> KnownLiveRoomIds { get; set; } = [];

        public LoginExport? Login { get; set; }
    }
}
