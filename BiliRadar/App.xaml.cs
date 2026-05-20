using System;
using System.IO;
using BiliRadar.Services;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.AppNotifications;
using Windows.ApplicationModel;

namespace BiliRadar;

public partial class App : Application
{
    private MainWindow? _mainWindow;
    private SettingsWindow? _settingsWindow;
    private TrayIconService? _trayIconService;
    private AppNotificationManager? _notificationManager;
    private NotificationService.NotificationActivationRequest? _pendingNotificationRequest;
    private bool _isExiting;

    public App()
    {
        InitializeComponent();
        InitializeNotifications();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _mainWindow = new MainWindow();
        _mainWindow.HideRequested += HideMainWindow;

        _mainWindow.InitializeHidden();
        HandleLaunchActivation();
        HandlePendingNotificationRequest();
        _mainWindow.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, InitializeTrayAndData);
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

    private void HandleLaunchActivation()
    {
        try
        {
            var activatedArgs = Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().GetActivatedEventArgs();
            if (activatedArgs.Kind == ExtendedActivationKind.AppNotification
                && activatedArgs.Data is AppNotificationActivatedEventArgs notificationArgs)
            {
                HandleNotificationActivation(notificationArgs);
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
