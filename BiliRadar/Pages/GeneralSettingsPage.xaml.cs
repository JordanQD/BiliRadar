using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using BiliRadar.Helpers;
using BiliRadar.Models;
using BiliRadar.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Win32;
using Microsoft.Windows.Storage.Pickers;
using Windows.ApplicationModel;

namespace BiliRadar.Pages;

public sealed partial class GeneralSettingsPage : Page
{
    private const string StartupTaskId = "BiliRadarStartupTask";
    private const string StartupRegistryName = "BiliRadar";
    private const string PersonAddIconData = "M17.5 12C20.5375661 12 23 14.4624339 23 17.5C23 20.5375661 20.5375661 23 17.5 23C14.4624339 23 12 20.5375661 12 17.5C12 14.4624339 14.4624339 12 17.5 12ZM12.0222607 13.9993086C11.7255613 14.4626083 11.4860296 14.9660345 11.3136172 15.4996352L4.25354153 15.499921C3.83932796 15.499921 3.50354153 15.8357075 3.50354153 16.249921L3.50354153 17.1572408C3.50354153 17.8128951 3.78953221 18.4359296 4.28670709 18.8633654C5.5447918 19.9450082 7.44080155 20.5010712 10 20.5010712C10.598839 20.5010712 11.1614445 20.4706245 11.6881394 20.4101192C11.9370538 20.9102887 12.2508544 21.3740111 12.6170965 21.7904935C11.8149076 21.9312924 10.9419626 22.0010712 10 22.0010712C7.11050247 22.0010712 4.87168436 21.3444691 3.30881727 20.0007885C2.48019625 19.2883988 2.00354153 18.2500002 2.00354153 17.1572408L2.00354153 16.249921C2.00354153 15.0072804 3.01090084 13.999921 4.25354153 13.999921L12.0222607 13.9993086ZM17.5 14.25C17.9142 14.25 18.25 14.5858 18.25 15V16.75H20C20.4142 16.75 20.75 17.0858 20.75 17.5C20.75 17.9142 20.4142 18.25 20 18.25H18.25V20C18.25 20.4142 17.9142 20.75 17.5 20.75C17.0858 20.75 16.75 20.4142 16.75 20V18.25H15C14.5858 18.25 14.25 17.9142 14.25 17.5C14.25 17.0858 14.5858 16.75 15 16.75H16.75V15C16.75 14.5858 17.0858 14.25 17.5 14.25ZM10 2.0046246C12.7614237 2.0046246 15 4.24320085 15 7.0046246C15 9.76604835 12.7614237 12.0046246 10 12.0046246C7.23857625 12.0046246 5 9.76604835 5 7.0046246C5 4.24320085 7.23857625 2.0046246 10 2.0046246ZM10 3.5046246C8.06700338 3.5046246 6.5 5.07162798 6.5 7.0046246C6.5 8.93762123 8.06700338 10.5046246 10 10.5046246C11.9329966 10.5046246 13.5 8.93762123 13.5 7.0046246C13.5 5.07162798 11.9329966 3.5046246 10 3.5046246Z";
    private const string PersonDeleteIconData = "M17.5 12C20.5375661 12 23 14.4624339 23 17.5C23 20.5375661 20.5375661 23 17.5 23C14.4624339 23 12 20.5375661 12 17.5C12 14.4624339 14.4624339 12 17.5 12ZM12.0222607 13.9993086C11.7255613 14.4626083 11.4860296 14.9660345 11.3136172 15.4996352L4.25354153 15.499921C3.83932796 15.499921 3.50354153 15.8357075 3.50354153 16.249921L3.50354153 17.1572408C3.50354153 17.8128951 3.78953221 18.4359296 4.28670709 18.8633654C5.5447918 19.9450082 7.44080155 20.5010712 10 20.5010712C10.598839 20.5010712 11.1614445 20.4706245 11.6881394 20.4101192C11.9370538 20.9102887 12.2508544 21.3740111 12.6170965 21.7904935C11.8149076 21.9312924 10.9419626 22.0010712 10 22.0010712C7.11050247 22.0010712 4.87168436 21.3444691 3.30881727 20.0007885C2.48019625 19.2883988 2.00354153 18.2500002 2.00354153 17.1572408L2.00354153 16.249921C2.00354153 15.0072804 3.01090084 13.999921 4.25354153 13.999921L12.0222607 13.9993086ZM15.0930472 14.9662824L15.0237993 15.0241379L14.9659438 15.0933858C14.8478223 15.2638954 14.8478223 15.4914871 14.9659438 15.6619968L15.0237993 15.7312446L16.7933527 17.5006913L15.0263884 19.2674911L14.968533 19.3367389C14.8504114 19.5072486 14.8504114 19.7348403 14.968533 19.9053499L15.0263884 19.9745978L15.0956363 20.0324533C15.2661459 20.1505748 15.4937377 20.1505748 15.6642473 20.0324533L15.7334952 19.9745978L17.5003527 18.2076913L19.2693951 19.9768405L19.338643 20.0346959C19.5091526 20.1528175 19.7367444 20.1528175 19.907254 20.0346959L19.9765019 19.9768405L20.0343574 19.9075926C20.1524789 19.737083 20.1524789 19.5094912 20.0343574 19.3389816L19.9765019 19.2697337L18.2073527 17.5006913L19.9792686 15.7312918L20.0371241 15.6620439C20.1552456 15.4915343 20.1552456 15.2639425 20.0371241 15.0934329L19.9792686 15.024185L19.9100208 14.9663296C19.7395111 14.848208 19.5119194 14.848208 19.3414098 14.9663296L19.2721619 15.024185L17.5003527 16.7936913L15.7309061 15.0241379L15.6616582 14.9662824C15.5155071 14.8650354 15.3274181 14.8505715 15.1692847 14.9228908L15.0930472 14.9662824ZM10 2.0046246C12.7614237 2.0046246 15 4.24320085 15 7.0046246C15 9.76604835 12.7614237 12.0046246 10 12.0046246C7.23857625 12.0046246 5 9.76604835 5 7.0046246C5 4.24320085 7.23857625 2.0046246 10 2.0046246ZM10 3.5046246C8.06700338 3.5046246 6.5 5.07162798 6.5 7.0046246C6.5 8.93762123 8.06700338 10.5046246 10 10.5046246C11.9329966 10.5046246 13.5 8.93762123 13.5 7.0046246C13.5 5.07162798 11.9329966 3.5046246 10 3.5046246Z";

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
        InitLanguageSelector();
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
                StartupTaskState.DisabledByUser => LocalizationHelper.GetString("StartupDisabledByUser"),
                StartupTaskState.DisabledByPolicy => LocalizationHelper.GetString("StartupDisabledByPolicy"),
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
        StartupInfoBar.Message = LocalizationHelper.GetString("StartupTaskFallback");
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

    private async void LoginActionButton_Click(object sender, RoutedEventArgs e)
    {
        if (_cookieStore.HasCookie)
        {
            using var authService = new BiliKernelAuthService(_cookieStore);
            await authService.SignOutAsync();
            await RefreshAccountStatusAsync();
        }
        else
        {
            if (_signInWindow is null)
            {
                _signInWindow = new WebSignInWindow(_cookieStore);
                _signInWindow.SignInSucceeded += SignInWindow_SignInSucceeded;
                _signInWindow.Closed += SignInWindow_Closed;
            }

            _signInWindow.Activate();
        }
    }

    private void SignInWindow_SignInSucceeded(object? sender, EventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            _ = RefreshAccountStatusAsync();
        });
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
        if (!_cookieStore.HasCookie)
        {
            AccountNameText.Text = LocalizationHelper.GetString("NotLoggedInShort");
            SetAccountDetailText(string.Empty);
            AvatarPicture.ProfilePicture = null;
            AvatarPicture.Initials = "";
            UpdateLoginButton(false);
            return;
        }

        try
        {
            var profile = await _accountService.GetCurrentProfileAsync();
            if (profile is null)
            {
                AccountNameText.Text = LocalizationHelper.GetString("LoginStatusInvalid");
                SetAccountDetailText(LocalizationHelper.GetString("PleaseRelogin"));
                AvatarPicture.ProfilePicture = null;
                AvatarPicture.Initials = "";
                UpdateLoginButton(false);
                return;
            }

            AccountNameText.Text = profile.Name;
            SetAccountDetailText(string.Empty);
            AvatarPicture.Initials = string.IsNullOrWhiteSpace(profile.Name) ? "" : profile.Name[..1];
            AvatarPicture.ProfilePicture = string.IsNullOrWhiteSpace(profile.AvatarUrl)
                ? null
                : new BitmapImage(new Uri(profile.AvatarUrl));
            UpdateLoginButton(true);
        }
        catch (Exception)
        {
            AccountNameText.Text = LocalizationHelper.GetString("StatusUnknown");
            SetAccountDetailText(LocalizationHelper.GetString("CookieUnverifiable"));
            AvatarPicture.ProfilePicture = null;
            AvatarPicture.Initials = "";
            UpdateLoginButton(false);
        }
    }

    private void SetAccountDetailText(string text)
    {
        AccountDetailText.Text = text;
        AccountDetailText.Visibility = string.IsNullOrWhiteSpace(text) ? Visibility.Collapsed : Visibility.Visible;
    }

    private void UpdateLoginButton(bool isLoggedIn)
    {
        if (isLoggedIn)
        {
            LoginActionButton.Content = CreateButtonPathIcon(PersonDeleteIconData, 20);
            LoginActionButton.SetValue(ToolTipService.ToolTipProperty, "退出登录");
        }
        else
        {
            LoginActionButton.Content = CreateButtonPathIcon(PersonAddIconData, 20);
            LoginActionButton.SetValue(ToolTipService.ToolTipProperty, "网页登录");
        }
    }

    private static FrameworkElement CreateButtonPathIcon(string data, double size)
    {
        return (FrameworkElement)XamlReader.Load($$"""
            <Viewbox
                xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                Width="{{size}}"
                Height="{{size}}"
                Stretch="Uniform">
                <Path
                    Width="24"
                    Height="24"
                    Data="{{data}}"
                    Fill="{ThemeResource TextFillColorPrimaryBrush}"
                    Stretch="Uniform" />
            </Viewbox>
            """);
    }

    private async void ExportConfigButton_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileSavePicker(((FrameworkElement)sender).XamlRoot.ContentIslandEnvironment.AppWindowId)
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = $"BiliRadar-config-{DateTime.Now:yyyyMMdd-HHmmss}",
        };
        picker.FileTypeChoices.Add(LocalizationHelper.GetString("FilePickerJsonLabel"), [".json"]);

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
            SetConfigStatus(LocalizationHelper.Format("ExportedTo", result.Path));
        }
        catch (Exception ex)
        {
            SetConfigStatus(LocalizationHelper.Format("ExportFailed", ex.Message));
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
                ?? throw new InvalidOperationException(LocalizationHelper.GetString("InvalidConfigFile"));

            await ApplySettingsExportAsync(import);
            SetConfigStatus(LocalizationHelper.Format("ImportedFrom", result.Path));
        }
        catch (Exception ex)
        {
            SetConfigStatus(LocalizationHelper.Format("ImportFailed", ex.Message));
        }
    }

    private SettingsExport CreateSettingsExport()
    {
        return new SettingsExport
        {
            ExportedAt = DateTimeOffset.Now,
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

    private void InitLanguageSelector()
    {
        var current = AppSettings.AppLanguage;
        LanguageSelectorBox.SelectedIndex = current switch
        {
            "zh-HK" => 1,
            _ => 0,
        };
    }

    private void LanguageSelectorBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoadingSettings || LanguageSelectorBox.SelectedItem is not ComboBoxItem { Tag: string tag })
        {
            return;
        }

        if (string.Equals(AppSettings.AppLanguage, tag, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        AppSettings.AppLanguage = tag;
        LocalizationHelper.SetLanguage(tag);

        SetConfigStatus(LocalizationHelper.Format("LanguageRestartMessage", tag switch
        {
            "zh-HK" => "繁體中文（香港）",
            _ => "简体中文",
        }));
    }

    private void SetConfigStatus(string message)
    {
        ConfigStatusText.Text = message;
        ConfigStatusText.Visibility = string.IsNullOrWhiteSpace(message) ? Visibility.Collapsed : Visibility.Visible;
    }

    private sealed class LoginExport
    {
        public bool HasCookie { get; set; }

        public string Cookie { get; set; } = string.Empty;
    }

    private sealed class SettingsExport
    {
        public DateTimeOffset ExportedAt { get; set; }

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

        public LoginExport? Login { get; set; }
    }
}
