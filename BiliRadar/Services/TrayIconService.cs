using System;
using System.Windows.Input;
using H.NotifyIcon;
using Microsoft.Win32;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace BiliRadar.Services;

internal sealed class TrayIconService : IDisposable
{
    private readonly string _tooltip;
    private readonly Action _toggleAction;
    private readonly Action _openAction;
    private readonly Action _settingsAction;
    private readonly Action _exitAction;
    private TaskbarIcon? _taskbarIcon;

    public TrayIconService(
        string tooltip,
        Action toggleAction,
        Action openAction,
        Action settingsAction,
        Action exitAction)
    {
        _tooltip = tooltip;
        _toggleAction = toggleAction;
        _openAction = openAction;
        _settingsAction = settingsAction;
        _exitAction = exitAction;
    }

    public void SetupTrayIcon()
    {
        if (_taskbarIcon is not null)
        {
            return;
        }

        _taskbarIcon = new TaskbarIcon
        {
            ToolTipText = _tooltip,
            IconSource = LoadTrayIconSource(),
            ContextFlyout = CreateContextMenu(),
            ContextMenuMode = ContextMenuMode.PopupMenu,
            LeftClickCommand = new DelegateCommand(_toggleAction),
            NoLeftClickDelay = true,
        };
        _taskbarIcon.ForceCreate();
    }

    public void Destroy()
    {
        Dispose();
    }

    public void Dispose()
    {
        _taskbarIcon?.Dispose();
        _taskbarIcon = null;
    }

    private MenuFlyout CreateContextMenu()
    {
        var menu = new MenuFlyout();

        menu.Items.Add(CreateMenuItem("打开 BiliRadar", "\uE8A7", _openAction));
        menu.Items.Add(CreateMenuItem("设置", "\uE713", _settingsAction));
        menu.Items.Add(new MenuFlyoutSeparator());
        menu.Items.Add(CreateMenuItem("退出", "\uE8BB", _exitAction));

        return menu;
    }

    private static MenuFlyoutItem CreateMenuItem(string text, string glyph, Action action)
    {
        return new MenuFlyoutItem
        {
            Text = text,
            Icon = new FontIcon { Glyph = glyph },
            Command = new DelegateCommand(action),
        };
    }

    private static BitmapImage LoadTrayIconSource()
    {
        var assetName = IsSystemUsingLightTheme() ? "TrayIconDark.ico" : "TrayIconLight.ico";
        return new BitmapImage(new Uri($"ms-appx:///Assets/{assetName}"));
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

        public bool CanExecute(object? parameter)
        {
            return true;
        }

        public void Execute(object? parameter)
        {
            _execute();
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
