using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using BiliRadar.Helpers;
using BiliRadar.Services;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppNotifications;
using Windows.ApplicationModel;
using AppActivationArguments = Microsoft.Windows.AppLifecycle.AppActivationArguments;
using AppInstance = Microsoft.Windows.AppLifecycle.AppInstance;
using ExtendedActivationKind = Microsoft.Windows.AppLifecycle.ExtendedActivationKind;

namespace BiliRadar;

public partial class App : Application
{
    private const string MainInstanceKey = "BiliRadar.Main";

    private MainWindow? _mainWindow;
    private SettingsWindow? _settingsWindow;
    private TrayIconService? _trayIconService;
    private AppNotificationManager? _notificationManager;
    private NotificationService.NotificationActivationRequest? _pendingNotificationRequest;
    private bool _isExiting;

    public App()
    {
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

        _mainWindow = new MainWindow();
        _mainWindow.HideRequested += HideMainWindow;

        _mainWindow.InitializeHidden();
        HandleActivation(activatedArgs, isRedirectedActivation: false);
        HandlePendingNotificationRequest();
        _mainWindow.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, InitializeTrayAndData);
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
        if (_mainWindow is null)
        {
            return;
        }

        _mainWindow.DispatcherQueue.TryEnqueue(() => HandleActivation(args, isRedirectedActivation: true));
    }

    private void InitializeTrayAndData()
    {
        if (_mainWindow is null)
        {
            return;
        }

        try
        {
            _trayIconService = new TrayIconService(
                "BiliRadar",
                ToggleMainWindow,
                ShowMainWindow,
                ShowSettingsWindow,
                ExitApplication);
            _trayIconService.SetupTrayIcon();

            _ = _mainWindow.StartNotificationMonitorAsync();
        }
        catch
        {
        }
    }

    private void ShowMainWindow()
    {
        _mainWindow?.ShowDefaultOpenPage();
        _mainWindow?.ShowWindow();
    }

    private void ToggleMainWindow()
    {
        if (_mainWindow is null)
        {
            return;
        }

        if (_mainWindow.IsVisible)
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

        _mainWindow?.HideWindow();
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
            if (_mainWindow is not null)
            {
                await _mainWindow.LaunchCustomWebPageUriAsync();
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
        _notificationManager?.Unregister();
        _notificationManager = null;
        _settingsWindow?.Close();
        _settingsWindow = null;
        _mainWindow?.CloseForExit();
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
        if (!NotificationService.TryGetActivationRequest(args, out var request))
        {
            return;
        }

        var window = _mainWindow;
        if (window is null)
        {
            _pendingNotificationRequest = request;
            return;
        }

        window.DispatcherQueue.TryEnqueue(async () => await window.HandleNotificationActivationAsync(request));
    }

    private void HandlePendingNotificationRequest()
    {
        if (_pendingNotificationRequest is null || _mainWindow is null)
        {
            return;
        }

        var request = _pendingNotificationRequest;
        _pendingNotificationRequest = null;
        _mainWindow.DispatcherQueue.TryEnqueue(async () => await _mainWindow.HandleNotificationActivationAsync(request));
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

    private void SettingsWindow_Closed(object sender, WindowEventArgs args)
    {
        if (_settingsWindow is not null)
        {
            _settingsWindow.Closed -= SettingsWindow_Closed;
            _settingsWindow = null;
        }
    }
}
