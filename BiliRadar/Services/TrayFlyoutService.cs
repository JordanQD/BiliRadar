using BiliRadar.Controls;
using BiliRadar.Helpers;
using BiliRadar.Models;
using DesktopFlyouts;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel;
using Windows.UI.ViewManagement;

namespace BiliRadar.Services;

internal sealed class TrayFlyoutService : IDisposable
{
    private const double MainPanelWidth = 420d;
    private const double MinimumPanelHeight = 320d;
    private const double FlyoutWorkAreaInset = 24d;
    private static readonly Guid TrayIconGuid = new("F0D7B8D0-7063-4F3D-A962-9E81BD766431");
    private static readonly TimeSpan TrayLightDismissReopenGuard = TimeSpan.FromMilliseconds(300);

    private readonly TrayHostWindow _containerWindow;
    private readonly DesktopFlyout _mainFlyout;
    private readonly DesktopFlyoutIsland _mainFlyoutIsland;
    private readonly DesktopMenuFlyout _contextMenu;
    private readonly DispatcherQueueTimer _trayIconRefreshTimer;
    private readonly long _mainFlyoutIsOpenCallbackToken;
    private readonly SystemTrayIcon _trayIcon;
    private MainWindowSnapshot? _lastSnapshot;
    private UISettings? _uiSettings;
    private DateTime _lastMainFlyoutClosedAt = DateTime.MinValue;
    private bool _isMainFlyoutShowPending;
    private bool _isDisposed;

    public TrayFlyoutService(
        TrayHostWindow containerWindow,
        Action settingsAction,
        Action exitAction,
        MainWindowSnapshot? initialSnapshot = null)
    {
        _containerWindow = containerWindow;
        _lastSnapshot = initialSnapshot;

        _uiSettings = new UISettings();
        _uiSettings.ColorValuesChanged += OnColorValuesChanged;

        _mainFlyoutIsland = new DesktopFlyoutIsland
        {
            IslandHeight = new GridLength(AppSettings.MainPanelHeight),
        };

        _mainFlyout = new DesktopFlyout
        {
            FlyoutWidth = new GridLength(MainPanelWidth),
            FlyoutHeight = new GridLength(AppSettings.MainPanelHeight),
            PopupDirection = DesktopFlyoutPopupDirection.Vertical,
            Placement = DesktopFlyoutPlacementMode.BottomRight,
            ActivationMode = DesktopFlyoutActivationMode.Activate,
            HideOnLostFocus = true,
            IsBackdropEnabled = true,
            BackdropKind = DesktopFlyoutBackdropKind.DesktopAcrylic,
            IsTransitionAnimationEnabled = _uiSettings.AnimationsEnabled,
            PressedScale = 1d,
            IsSwipeToDismissEnabled = false,
        };
        _mainFlyout.Islands.Add(_mainFlyoutIsland);
        _mainFlyoutIsOpenCallbackToken = _mainFlyout.RegisterPropertyChangedCallback(
            DesktopFlyout.IsOpenProperty,
            OnMainFlyoutIsOpenChanged);

        _contextMenu = CreateContextMenu(settingsAction, exitAction);

        _trayIcon = new SystemTrayIcon(GetIconPath(), "BiliRadar", TrayIconGuid);
        _trayIcon.LeftClicked += OnTrayIconLeftClicked;
        _trayIcon.RightClicked += OnTrayIconRightClicked;
        _trayIcon.Show();

        _containerWindow.DisplayConfigurationChanged += OnDisplayConfigurationChanged;
        _trayIconRefreshTimer = _containerWindow.DispatcherQueue.CreateTimer();
        _trayIconRefreshTimer.Interval = TimeSpan.FromMilliseconds(500);
        _trayIconRefreshTimer.IsRepeating = false;
        _trayIconRefreshTimer.Tick += OnTrayIconRefreshTimerTick;

        TraceFlyout($"DesktopFlyouts initialized. AnimationsEnabled={_uiSettings.AnimationsEnabled}");
    }

    public Task RefreshCurrentPanelPageAsync()
    {
        return _mainFlyoutIsland.Content is MainPanelControl panel
            ? panel.RefreshCurrentPageAsync()
            : Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        if (_uiSettings is not null)
        {
            _uiSettings.ColorValuesChanged -= OnColorValuesChanged;
            _uiSettings = null;
        }

        _containerWindow.DisplayConfigurationChanged -= OnDisplayConfigurationChanged;
        _trayIconRefreshTimer.Stop();
        _trayIconRefreshTimer.Tick -= OnTrayIconRefreshTimerTick;

        _trayIcon.LeftClicked -= OnTrayIconLeftClicked;
        _trayIcon.RightClicked -= OnTrayIconRightClicked;
        _trayIcon.Dispose();

        _mainFlyout.UnregisterPropertyChangedCallback(
            DesktopFlyout.IsOpenProperty,
            _mainFlyoutIsOpenCallbackToken);

        if (_mainFlyoutIsland.Content is MainPanelControl panel)
        {
            panel.OnFlyoutClosed();
            panel.Dispose();
        }

        _mainFlyoutIsland.Content = null;
        _mainFlyout.Dispose();
        _contextMenu.Dispose();
    }

    private DesktopMenuFlyout CreateContextMenu(Action settingsAction, Action exitAction)
    {
        var menu = new DesktopMenuFlyout();
        menu.Items.Add(new MenuFlyoutItem
        {
            Text = LocalizationHelper.GetString("TraySettings"),
            Icon = new FontIcon { Glyph = "\uE713" },
            Command = new DelegateCommand(settingsAction),
        });
        menu.Items.Add(new MenuFlyoutSeparator());
        menu.Items.Add(new MenuFlyoutItem
        {
            Text = LocalizationHelper.GetString("TrayExit"),
            Icon = new FontIcon { Glyph = "\uE8BB" },
            Command = new DelegateCommand(() => RequestExit(exitAction)),
        });
        return menu;
    }

    private void OnTrayIconLeftClicked(object? sender, MouseEventReceivedEventArgs args)
    {
        if (_isDisposed)
        {
            return;
        }

        TraceFlyout($"Tray icon left clicked. IsOpen={_mainFlyout.IsOpen}, pending={_isMainFlyoutShowPending}");

        if (_contextMenu.IsOpen)
        {
            _contextMenu.Hide();
        }

        if (_mainFlyout.IsOpen)
        {
            _mainFlyout.Hide();
            return;
        }

        if (_isMainFlyoutShowPending
            || DateTime.UtcNow - _lastMainFlyoutClosedAt < TrayLightDismissReopenGuard)
        {
            return;
        }

        var panelHeight = GetMainPanelHeight(args.Point);
        EnsureFlyoutContent(panelHeight);
        ConfigureMainFlyout(panelHeight);

        _mainFlyout.IsTransitionAnimationEnabled = _uiSettings?.AnimationsEnabled == true;
        _isMainFlyoutShowPending = true;
        _mainFlyout.Show(args.Point);
    }

    private void OnTrayIconRightClicked(object? sender, MouseEventReceivedEventArgs args)
    {
        if (_isDisposed)
        {
            return;
        }

        if (_mainFlyout.IsOpen)
        {
            _mainFlyout.Hide();
        }

        if (_contextMenu.IsOpen)
        {
            _contextMenu.Hide();
        }

        _contextMenu.Show(GetContextMenuPoint(args.Point));
    }

    private void OnMainFlyoutIsOpenChanged(DependencyObject sender, DependencyProperty dependencyProperty)
    {
        if (_isDisposed)
        {
            return;
        }

        if (_mainFlyout.IsOpen)
        {
            _isMainFlyoutShowPending = false;
            TraceFlyout("DesktopFlyout opened");

            if (_mainFlyoutIsland.Content is MainPanelControl panel)
            {
                // DesktopFlyouts owns the shell transition; suppress the legacy whole-panel animation.
                panel.OnFlyoutOpened(playOpenAnimation: false);
            }

            return;
        }

        _isMainFlyoutShowPending = false;
        _lastMainFlyoutClosedAt = DateTime.UtcNow;
        TraceFlyout("DesktopFlyout closed");

        if (_mainFlyoutIsland.Content is not MainPanelControl closedPanel)
        {
            return;
        }

        closedPanel.OnFlyoutClosed();
        _lastSnapshot = closedPanel.Session.CreateSnapshot();
        closedPanel.Dispose();
        _mainFlyoutIsland.Content = null;
    }

    private void EnsureFlyoutContent(double panelHeight)
    {
        if (_mainFlyoutIsland.Content is MainPanelControl existingPanel)
        {
            existingPanel.SetHostHeight(panelHeight);
            return;
        }

        var panel = new MainPanelControl(_lastSnapshot);
        panel.SetHostHeight(panelHeight);
        _mainFlyoutIsland.Content = panel;
    }

    private void ConfigureMainFlyout(double panelHeight)
    {
        var height = new GridLength(panelHeight);
        _mainFlyout.FlyoutWidth = new GridLength(MainPanelWidth);
        _mainFlyout.FlyoutHeight = height;
        _mainFlyoutIsland.IslandHeight = height;
    }

    private void RequestExit(Action exitAction)
    {
        TraceFlyout("Exit requested from context menu");
        _contextMenu.Hide();
        _containerWindow.DispatcherQueue.TryEnqueue(
            DispatcherQueuePriority.Low,
            () => exitAction());
    }

    private void OnDisplayConfigurationChanged(object? sender, EventArgs e)
    {
        if (_isDisposed)
        {
            return;
        }

        _trayIconRefreshTimer.Stop();
        _trayIconRefreshTimer.Start();
    }

    private void OnTrayIconRefreshTimerTick(DispatcherQueueTimer sender, object args)
    {
        sender.Stop();

        if (!_isDisposed)
        {
            _trayIcon.SetIcon(GetIconPath());
        }
    }

    private void OnColorValuesChanged(UISettings sender, object args)
    {
        _containerWindow.DispatcherQueue.TryEnqueue(() =>
        {
            if (!_isDisposed)
            {
                _trayIcon.SetIcon(GetIconPath());
            }
        });
    }

    private static double GetMainPanelHeight(Point anchorPoint)
    {
        var monitor = MonitorFromPoint(
            new NativePoint(anchorPoint.X, anchorPoint.Y),
            MonitorDefaultToNearest);
        var monitorInfo = new MonitorInfo
        {
            Size = Marshal.SizeOf<MonitorInfo>(),
        };

        if (monitor == nint.Zero || !GetMonitorInfo(monitor, ref monitorInfo))
        {
            return AppSettings.MainPanelHeight;
        }

        var dpiY = 96u;
        _ = GetDpiForMonitor(monitor, MonitorDpiType.Effective, out _, out dpiY);

        var workAreaHeightInPixels = monitorInfo.WorkArea.Bottom - monitorInfo.WorkArea.Top;
        var workAreaHeightInDips = workAreaHeightInPixels * 96d / Math.Max(96u, dpiY);
        return Math.Max(
            MinimumPanelHeight,
            Math.Min(workAreaHeightInDips - FlyoutWorkAreaInset, AppSettings.MainPanelHeight));
    }

    private static Point GetContextMenuPoint(Point anchorPoint)
    {
        var monitor = MonitorFromPoint(
            new NativePoint(anchorPoint.X, anchorPoint.Y),
            MonitorDefaultToNearest);
        var monitorInfo = new MonitorInfo
        {
            Size = Marshal.SizeOf<MonitorInfo>(),
        };

        if (monitor == nint.Zero || !GetMonitorInfo(monitor, ref monitorInfo))
        {
            return new Point(anchorPoint.X, anchorPoint.Y - 32);
        }

        var workAreaMiddle = monitorInfo.WorkArea.Top
            + ((monitorInfo.WorkArea.Bottom - monitorInfo.WorkArea.Top) / 2);
        var verticalOffset = anchorPoint.Y >= workAreaMiddle ? -32 : 16;
        return new Point(anchorPoint.X, anchorPoint.Y + verticalOffset);
    }

    private static string GetIconPath()
    {
        var assetName = IsSystemUsingLightTheme()
            ? "Assets/TrayIconDark.ico"
            : "Assets/TrayIconLight.ico";

        try
        {
            return Path.Combine(Package.Current.InstalledLocation.Path, assetName);
        }
        catch
        {
            return Path.Combine(AppContext.BaseDirectory, assetName);
        }
    }

    private static bool IsSystemUsingLightTheme()
    {
        const string personalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        using var key = Registry.CurrentUser.OpenSubKey(personalizeKey);
        return key?.GetValue("SystemUsesLightTheme") is int value ? value != 0 : true;
    }

    private static void TraceFlyout(string message)
    {
        Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [TrayFlyout] {message}");
    }

    private const uint MonitorDefaultToNearest = 2;

    private enum MonitorDpiType
    {
        Effective = 0,
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct NativePoint(int x, int y)
    {
        public readonly int X = x;
        public readonly int Y = y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MonitorInfo
    {
        public int Size;
        public NativeRect Monitor;
        public NativeRect WorkArea;
        public uint Flags;
    }

    [DllImport("user32.dll")]
    private static extern nint MonitorFromPoint(NativePoint point, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(nint monitor, ref MonitorInfo monitorInfo);

    [DllImport("shcore.dll")]
    private static extern int GetDpiForMonitor(
        nint monitor,
        MonitorDpiType dpiType,
        out uint dpiX,
        out uint dpiY);

    private sealed class DelegateCommand : ICommand
    {
        private readonly Action _execute;

        public DelegateCommand(Action execute) => _execute = execute;

        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => _execute();
    }
}
