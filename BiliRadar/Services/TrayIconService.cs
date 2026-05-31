using BiliRadar.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Windows.Input;
using SystemTray.Core;
using Windows.UI.ViewManagement;

namespace BiliRadar.Services;

internal sealed class TrayIconService : IDisposable
{
    private readonly Window _mainWindow;
    private readonly string _tooltip;
    private readonly Action _toggleAction;
    private readonly Action _openAction;
    private readonly Action _settingsAction;
    private readonly Action _exitAction;
    private SystemTrayManager? _systemTrayManager;
    private UISettings? _uiSettings;

    public TrayIconService(
        Window mainWindow,
        string tooltip,
        Action toggleAction,
        Action openAction,
        Action settingsAction,
        Action exitAction)
    {
        _mainWindow = mainWindow;
        _tooltip = tooltip;
        _toggleAction = toggleAction;
        _openAction = openAction;
        _settingsAction = settingsAction;
        _exitAction = exitAction;
    }

    public void SetupTrayIcon()
    {
        if (_systemTrayManager is not null)
            return;

        var windowHelper = new WindowHelper(_mainWindow);
        var iconPath = GetIconPath();

        var menuItems = new List<SystemTrayManager.MenuItemConfig>
        {
            new(LocalizationHelper.GetString("TrayOpenBiliRadar"), new DelegateCommand(_openAction)),
            new(LocalizationHelper.GetString("TraySettings"), new DelegateCommand(_settingsAction)),
            new("--", null, IsSeparator: true),
            new(LocalizationHelper.GetString("TrayExit"), new DelegateCommand(_exitAction)),
        };

        _systemTrayManager = new SystemTrayManager(
            windowHelper,
            iconPath,
            _tooltip,
            menuItems,
            leftClickAction: _toggleAction)
        {
            MinimizeToTray = true,
            CloseButtonMinimizesToTray = true,
        };

        _uiSettings = new UISettings();
        _uiSettings.ColorValuesChanged += OnColorValuesChanged;
    }

    public void RefreshIconForTheme()
    {
        if (_systemTrayManager is null) return;
        _systemTrayManager.RefreshIcon(GetIconPath());
    }

    public void Destroy()
    {
        Dispose();
    }

    public void Dispose()
    {
        if (_uiSettings is not null)
        {
            _uiSettings.ColorValuesChanged -= OnColorValuesChanged;
            _uiSettings = null;
        }

        _systemTrayManager?.Dispose();
        _systemTrayManager = null;
    }

    private void OnColorValuesChanged(UISettings sender, object args)
    {
        _mainWindow.DispatcherQueue.TryEnqueue(RefreshIconForTheme);
    }

    private static string GetIconPath()
    {
        var assetName = IsSystemUsingLightTheme() ? "Assets/TrayIconDark.ico" : "Assets/TrayIconLight.ico";
        return assetName;
    }

    private static bool IsSystemUsingLightTheme()
    {
        const string personalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        using var key = Registry.CurrentUser.OpenSubKey(personalizeKey);
        return key?.GetValue("SystemUsesLightTheme") is int value ? value != 0 : true;
    }

    private sealed class DelegateCommand : ICommand
    {
        private readonly Action _execute;

        public DelegateCommand(Action execute)
        {
            _execute = execute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => _execute();

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
