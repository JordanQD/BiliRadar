using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

namespace BiliRadar.Services;

internal sealed class TrayIconService : IDisposable
{
    private readonly string _tooltip;
    private readonly Action _openAction;
    private readonly Action _settingsAction;
    private readonly Action _refreshAction;
    private readonly Action _exitAction;
    private NotifyIcon? _notifyIcon;
    private ContextMenuStrip? _contextMenu;

    public TrayIconService(
        string tooltip,
        Action openAction,
        Action settingsAction,
        Action refreshAction,
        Action exitAction)
    {
        _tooltip = tooltip;
        _openAction = openAction;
        _settingsAction = settingsAction;
        _refreshAction = refreshAction;
        _exitAction = exitAction;
    }

    public void SetupTrayIcon()
    {
        if (_notifyIcon is not null)
        {
            _notifyIcon.Visible = true;
            return;
        }

        _contextMenu = new ContextMenuStrip();
        _contextMenu.Items.Add("打开 BiliRadar", null, (_, _) => _openAction());
        _contextMenu.Items.Add("设置", null, (_, _) => _settingsAction());
        _contextMenu.Items.Add("立即刷新", null, (_, _) => _refreshAction());
        _contextMenu.Items.Add(new ToolStripSeparator());
        _contextMenu.Items.Add("退出", null, (_, _) => _exitAction());

        _notifyIcon = new NotifyIcon
        {
            Text = _tooltip,
            Icon = LoadTrayIcon(),
            ContextMenuStrip = _contextMenu,
            Visible = true,
        };
        _notifyIcon.MouseUp += NotifyIcon_MouseUp;
    }

    public void Destroy()
    {
        Dispose();
    }

    public void Dispose()
    {
        if (_notifyIcon is not null)
        {
            _notifyIcon.MouseUp -= NotifyIcon_MouseUp;
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }

        _contextMenu?.Dispose();
        _contextMenu = null;
    }

    private static Icon LoadTrayIcon()
    {
        var assetName = IsSystemUsingLightTheme() ? "TrayIconDark.ico" : "TrayIconLight.ico";
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", assetName);
        return File.Exists(path)
            ? new Icon(path)
            : SystemIcons.Application;
    }

    private static bool IsSystemUsingLightTheme()
    {
        const string personalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        using var key = Registry.CurrentUser.OpenSubKey(personalizeKey);
        return key?.GetValue("SystemUsesLightTheme") is int value ? value != 0 : true;
    }

    private void NotifyIcon_MouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _openAction();
        }
    }
}
