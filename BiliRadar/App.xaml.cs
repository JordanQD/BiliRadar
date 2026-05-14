using System;
using BiliRadar.Services;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace BiliRadar;

public partial class App : Application
{
    private MainWindow? _mainWindow;
    private TrayIconService? _trayIconService;
    private bool _isExiting;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _mainWindow = new MainWindow();
        _mainWindow.HideRequested += HideMainWindow;

        _mainWindow.InitializeHidden();
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
                RefreshUpdates,
                ExitApplication);
            _trayIconService.SetupTrayIcon();

            _ = _mainWindow.RefreshAsync();
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

    private async void RefreshUpdates()
    {
        if (_mainWindow is null)
        {
            return;
        }

        ShowMainWindow();
        await _mainWindow.RefreshAsync();
    }

    private void ExitApplication()
    {
        _isExiting = true;
        _trayIconService?.Destroy();
        _trayIconService = null;
            _mainWindow?.CloseForExit();
        Exit();
    }

}
