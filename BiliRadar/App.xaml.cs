using BiliRadar.Helpers;
using BiliRadar.Services;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
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

    private MainWindow? _mainWindow;
    private TrayHostWindow? _trayHostWindow;
    private SettingsWindow? _settingsWindow;
    private WebSignInWindow? _signInWindow;
    private TrayIconService? _trayIconService;
    private BackgroundNotificationMonitor? _backgroundNotificationMonitor;
    private AppNotificationManager? _notificationManager;
    private NotificationService.NotificationActivationRequest? _pendingNotificationRequest;
    private readonly CookieStore _cookieStore = new();
    private readonly DispatcherQueue _dispatcherQueue;
    private bool _isExiting;

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
        HandleActivation(activatedArgs, isRedirectedActivation: false);
        _trayHostWindow.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, InitializeTrayAndData);

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

        if (_trayIconService is null)
        {
            _trayIconService = new TrayIconService(
                _trayHostWindow,
                "BiliRadar",
                ToggleMainWindow,
                ShowMainWindow,
                ShowSettingsWindow,
                ExitApplication);
            _trayIconService.SetupTrayIcon();
        }

        if (_backgroundNotificationMonitor is null)
        {
            _backgroundNotificationMonitor = new BackgroundNotificationMonitor(_cookieStore);
            _ = _backgroundNotificationMonitor.StartAsync();
            HandlePendingNotificationRequest();
        }
    }

    private void ShowMainWindow()
    {
        var window = EnsureMainWindow();
        window.ShowDefaultOpenPage();
        window.ShowWindow();
    }

    private MainWindow EnsureMainWindow()
    {
        if (_mainWindow is not null)
        {
            return _mainWindow;
        }

        _mainWindow = new MainWindow();
        _mainWindow.HideRequested += HideMainWindow;
        return _mainWindow;
    }

    private void ToggleMainWindow()
    {
        if (_mainWindow?.IsVisible == true)
        {
            HideMainWindow();
        }
        else
        {
            ShowMainWindow();
        }
    }

    private void HideMainWindow()
    {
        if (_isExiting)
        {
            return;
        }

        var window = _mainWindow;
        _mainWindow = null;
        window?.CloseForRecycle();
        _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, CollectReleasedWindowResources);
    }

    private static void CollectReleasedWindowResources()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        using var process = Process.GetCurrentProcess();
        SetProcessWorkingSetSize(process.Handle, new nint(-1), new nint(-1));
    }

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetProcessWorkingSetSize(nint process, nint minimumWorkingSetSize, nint maximumWorkingSetSize);

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
        _isExiting = true;
        _trayIconService?.Destroy();
        _trayIconService = null;
        _backgroundNotificationMonitor?.Dispose();
        _backgroundNotificationMonitor = null;
        _notificationManager?.Unregister();
        _notificationManager = null;
        _settingsWindow?.Close();
        _settingsWindow = null;
        _mainWindow?.CloseForExit();
        _mainWindow = null;
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
        if (_trayIconService is null || _backgroundNotificationMonitor is null)
        {
            InitializeTrayAndData();
        }

        if (_mainWindow is not null)
        {
            await _mainWindow.RefreshAsync();
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
