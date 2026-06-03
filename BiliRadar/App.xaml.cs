using BiliRadar.Controls;
using BiliRadar.Helpers;
using BiliRadar.Services;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using AppActivationArguments = Microsoft.Windows.AppLifecycle.AppActivationArguments;
using AppInstance = Microsoft.Windows.AppLifecycle.AppInstance;
using ExtendedActivationKind = Microsoft.Windows.AppLifecycle.ExtendedActivationKind;

namespace BiliRadar;

public partial class App : Application
{
    private const string MainInstanceKey = "BiliRadar.Main";
    private const string SignInAction = "signIn";

    private TrayHostWindow? _trayHostWindow;
    private SettingsWindow? _settingsWindow;
    private WebSignInWindow? _signInWindow;
    private TrayFlyoutService? _trayFlyoutService;
    private BackgroundNotificationMonitor? _backgroundNotificationMonitor;
    private AppNotificationManager? _notificationManager;
    private NotificationService.NotificationActivationRequest? _pendingNotificationRequest;
    private readonly CookieStore _cookieStore = new();
    private readonly DispatcherQueue _dispatcherQueue;

    public App()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        LocalizationHelper.SetLanguage(AppSettings.AppLanguage);
        InitializeComponent();
        InitializeNotifications();
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        var activatedArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
        if (await RedirectToMainInstanceIfNeededAsync(activatedArgs))
        {
            Exit();
            return;
        }

        AppInstance.GetCurrent().Activated += AppInstance_Activated;

        _trayHostWindow = new TrayHostWindow();
        _trayHostWindow.InitializeHidden();
        _trayHostWindow.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, InitializeTrayAndData);
        HandleActivation(activatedArgs, isRedirectedActivation: false);

        if (!_cookieStore.HasCookie)
        {
            ShowSignInNotification();
        }
    }

    private static async Task<bool> RedirectToMainInstanceIfNeededAsync(AppActivationArguments activatedArgs)
    {
        var mainInstance = AppInstance.FindOrRegisterForKey(MainInstanceKey);
        if (mainInstance.IsCurrent)
        {
            return false;
        }

        await mainInstance.RedirectActivationToAsync(activatedArgs);
        return true;
    }

    private void AppInstance_Activated(object? sender, AppActivationArguments args)
    {
        _dispatcherQueue.TryEnqueue(() => HandleActivation(args, isRedirectedActivation: true));
    }

    private void InitializeTrayAndData()
    {
        if (_trayHostWindow is null)
        {
            return;
        }

        if (_trayFlyoutService is null)
        {
            _trayFlyoutService = new TrayFlyoutService(
                _trayHostWindow,
                ShowSettingsWindow,
                ExitApplication,
                _backgroundNotificationMonitor?.GetSnapshot());
        }

        if (_backgroundNotificationMonitor is null)
        {
            _backgroundNotificationMonitor = new BackgroundNotificationMonitor(_cookieStore);
            _ = _backgroundNotificationMonitor.StartAsync();
            HandlePendingNotificationRequest();
        }
    }

    private void ShowSettingsWindow()
    {
        if (_settingsWindow is null)
        {
            _settingsWindow = new SettingsWindow();
            _settingsWindow.Closed += SettingsWindow_Closed;
        }

        _settingsWindow.ShowWindow();
    }

    private async void HandleRunningLaunchAction()
    {
        if (AppSettings.RunningLaunchAction == RunningLaunchAction.OpenCustomWebPage)
        {
            if (Uri.TryCreate(AppSettings.CustomLaunchWebPageUrl, UriKind.Absolute, out var uri))
            {
                await NotificationService.LaunchUriAsync(uri);
            }

            return;
        }

        ShowSettingsWindow();
    }

    private void ExitApplication()
    {
        _trayFlyoutService?.Dispose();
        _trayFlyoutService = null;
        _backgroundNotificationMonitor?.Dispose();
        _backgroundNotificationMonitor = null;
        _notificationManager?.Unregister();
        _notificationManager = null;
        _settingsWindow?.Close();
        _settingsWindow = null;
        _trayHostWindow?.Close();
        _trayHostWindow = null;
        Exit();
    }

    private void InitializeNotifications()
    {
        try
        {
            _notificationManager = AppNotificationManager.Default;
            _notificationManager.NotificationInvoked += AppNotificationManager_NotificationInvoked;
            if (IsPackaged())
            {
                _notificationManager.Register();
            }
            else
            {
                var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Square44x44Logo.targetsize-24_altform-unplated.png");
                _notificationManager.Register("BiliRadar", new Uri(iconPath));
            }
        }
        catch
        {
            _notificationManager = null;
        }
    }

    private void AppNotificationManager_NotificationInvoked(AppNotificationManager sender, AppNotificationActivatedEventArgs args)
    {
        if (args.Arguments.TryGetValue("action", out var action) && action == SignInAction)
        {
            _dispatcherQueue.TryEnqueue(OpenSignInWindow);
            return;
        }

        HandleNotificationActivation(args);
    }

    private void HandleActivation(AppActivationArguments activatedArgs, bool isRedirectedActivation)
    {
        try
        {
            if (activatedArgs.Kind == ExtendedActivationKind.AppNotification
                && activatedArgs.Data is AppNotificationActivatedEventArgs notificationArgs)
            {
                HandleNotificationActivation(notificationArgs);
                return;
            }

            if (isRedirectedActivation)
            {
                HandleRunningLaunchAction();
            }
        }
        catch
        {
        }
    }

    private void HandleNotificationActivation(AppNotificationActivatedEventArgs args)
    {
        if (args.Arguments.TryGetValue("action", out var action) && action == SignInAction)
        {
            _dispatcherQueue.TryEnqueue(OpenSignInWindow);
            return;
        }

        if (!NotificationService.TryGetActivationRequest(args, out var request))
        {
            return;
        }

        var monitor = _backgroundNotificationMonitor;
        if (monitor is null)
        {
            _pendingNotificationRequest = request;
            return;
        }

        _dispatcherQueue.TryEnqueue(async () => await monitor.HandleActivationAsync(request));
    }

    private void HandlePendingNotificationRequest()
    {
        if (_pendingNotificationRequest is null || _backgroundNotificationMonitor is null)
        {
            return;
        }

        var request = _pendingNotificationRequest;
        _pendingNotificationRequest = null;
        _dispatcherQueue.TryEnqueue(async () => await _backgroundNotificationMonitor.HandleActivationAsync(request));
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

    private static void ShowSignInNotification()
    {
        try
        {
            var builder = new AppNotificationBuilder()
                .AddArgument("action", SignInAction)
                .AddText(LocalizationHelper.GetString("SignInNotificationTitle"))
                .AddText(LocalizationHelper.GetString("SignInNotificationBody"));

            AppNotificationManager.Default.Show(builder.BuildNotification());
        }
        catch
        {
        }
    }

    private void OpenSignInWindow()
    {
        if (_signInWindow is not null)
        {
            _signInWindow.Activate();
            return;
        }

        _signInWindow = new WebSignInWindow(_cookieStore);
        _signInWindow.SignInSucceeded += OnSignInSucceeded;
        _signInWindow.Closed += OnSignInWindowClosed;
        _signInWindow.Activate();
    }

    private async void OnSignInSucceeded(object? sender, EventArgs e)
    {
        if (_backgroundNotificationMonitor is null)
        {
            InitializeTrayAndData();
        }

        if (_trayFlyoutService is not null)
        {
            await _trayFlyoutService.RefreshCurrentPanelPageAsync();
        }
    }

    private void OnSignInWindowClosed(object sender, WindowEventArgs args)
    {
        if (_signInWindow is not null)
        {
            _signInWindow.SignInSucceeded -= OnSignInSucceeded;
            _signInWindow.Closed -= OnSignInWindowClosed;
            _signInWindow = null;
        }
    }

    private void SettingsWindow_Closed(object sender, WindowEventArgs args)
    {
        if (_settingsWindow is not null)
        {
            _settingsWindow.Closed -= SettingsWindow_Closed;
            _settingsWindow = null;
        }
    }
}
