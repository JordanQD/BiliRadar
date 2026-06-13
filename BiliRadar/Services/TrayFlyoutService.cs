using BiliRadar.Controls;
using BiliRadar.Helpers;
using BiliRadar.Models;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Graphics;
using Windows.UI.ViewManagement;
using WinUIEx;
using Flyout = Microsoft.UI.Xaml.Controls.Flyout;
using MenuFlyout = Microsoft.UI.Xaml.Controls.MenuFlyout;

namespace BiliRadar.Services;

internal sealed class TrayFlyoutService : IDisposable
{
    private const uint TrayIconId = 1;
    private static readonly TimeSpan TrayLightDismissReopenGuard = TimeSpan.FromMilliseconds(300);

    private readonly Flyout _mainFlyout;
    private readonly MenuFlyout _contextMenu;
    private readonly TrayHostWindow _containerWindow;
    private TrayIcon _trayIcon;
    private MainWindowSnapshot? _lastSnapshot;
    private UISettings? _uiSettings;
    private DateTime _lastMainFlyoutClosedAt = DateTime.MinValue;
    private CancellationTokenSource? _mainFlyoutCloseAnimationCts;
    private bool _isRunningMainFlyoutCloseAnimation;
    private bool _isMainFlyoutCloseAnimationComplete;
    private bool _isMainFlyoutOpen;
    private bool _isDisposed;

    public TrayFlyoutService(
        TrayHostWindow containerWindow,
        Action settingsAction,
        Action exitAction,
        MainWindowSnapshot? initialSnapshot = null)
    {
        _containerWindow = containerWindow;
        _lastSnapshot = initialSnapshot;

        _trayIcon = CreateTrayIcon();
        _containerWindow.TaskbarCreated += OnTaskbarCreated;

        _mainFlyout = new Flyout
        {
            LightDismissOverlayMode = LightDismissOverlayMode.On,
            ShouldConstrainToRootBounds = false,
            FlyoutPresenterStyle = CreateMainFlyoutPresenterStyle(),
        };
        _mainFlyout.AreOpenCloseAnimationsEnabled = true;
        _mainFlyout.SystemBackdrop = new MicaBackdrop();
        TraceFlyout($"_mainFlyout.AreOpenCloseAnimationsEnabled={_mainFlyout.AreOpenCloseAnimationsEnabled}");
        _mainFlyout.Closing += OnMainFlyoutClosing;
        _mainFlyout.Opened += OnMainFlyoutOpened;
        _mainFlyout.Closed += OnMainFlyoutClosed;

        _contextMenu = new MenuFlyout();
        _contextMenu.SystemBackdrop = new MicaBackdrop();
        _contextMenu.Placement = FlyoutPlacementMode.Top;
        _contextMenu.ShouldConstrainToRootBounds = false;
        _contextMenu.Closed += OnContextMenuClosed;
        _contextMenu.Items.Add(new MenuFlyoutItem
        {
            Text = LocalizationHelper.GetString("TraySettings"),
            Command = new DelegateCommand(settingsAction),
        });
        _contextMenu.Items.Add(new MenuFlyoutSeparator());
        _contextMenu.Items.Add(new MenuFlyoutItem
        {
            Text = LocalizationHelper.GetString("TrayExit"),
            Command = new DelegateCommand(() => RequestExit(exitAction)),
        });

        _uiSettings = new UISettings();
        TraceFlyout($"UISettings.AnimationsEnabled={_uiSettings.AnimationsEnabled}");
        _uiSettings.ColorValuesChanged += OnColorValuesChanged;
    }

    public Task RefreshCurrentPanelPageAsync()
    {
        return _mainFlyout.Content is MainPanelControl panel
            ? panel.RefreshCurrentPageAsync()
            : Task.CompletedTask;
    }

    private TrayIcon CreateTrayIcon()
    {
        var trayIcon = new TrayIcon(TrayIconId, GetIconPath(), "BiliRadar")
        {
            IsVisible = true,
        };

        trayIcon.Selected += OnTrayIconSelected;
        trayIcon.ContextMenu += OnTrayIconContextMenu;
        return trayIcon;
    }

    private void OnTaskbarCreated(object? sender, EventArgs e)
    {
        if (_isDisposed)
        {
            return;
        }

        TraceFlyout("TaskbarCreated received; recreating tray icon");
        RecreateTrayIcon();
    }

    private void RecreateTrayIcon()
    {
        _trayIcon.Selected -= OnTrayIconSelected;
        _trayIcon.ContextMenu -= OnTrayIconContextMenu;
        _trayIcon.Dispose();
        _trayIcon = CreateTrayIcon();
    }

    private void OnMainFlyoutClosing(FlyoutBase sender, FlyoutBaseClosingEventArgs args)
    {
        TraceFlyout("_mainFlyout.Closing");

        if (_isDisposed || _isMainFlyoutCloseAnimationComplete)
        {
            _isMainFlyoutCloseAnimationComplete = false;
            TraceFlyout("_mainFlyout.Closing allowed after close animation");
            return;
        }

        if (_uiSettings?.AnimationsEnabled != true || _mainFlyout.Content is not MainPanelControl panel)
        {
            return;
        }

        args.Cancel = true;

        if (_isRunningMainFlyoutCloseAnimation)
        {
            TraceFlyout("_mainFlyout.Closing ignored while close animation is already running");
            return;
        }

        _isRunningMainFlyoutCloseAnimation = true;
        _mainFlyoutCloseAnimationCts?.Cancel();
        _mainFlyoutCloseAnimationCts?.Dispose();
        _mainFlyoutCloseAnimationCts = new CancellationTokenSource();

        _ = CompleteMainFlyoutCloseAfterAnimationAsync(panel, _mainFlyoutCloseAnimationCts.Token);
    }

    private void OnMainFlyoutOpened(object? sender, object e)
    {
        _isMainFlyoutOpen = true;

        if (_mainFlyout.Content is MainPanelControl panel)
        {
            panel.OnFlyoutOpened(_uiSettings?.AnimationsEnabled == true);
        }
    }

    private async Task CompleteMainFlyoutCloseAfterAnimationAsync(
        MainPanelControl panel,
        CancellationToken cancellationToken)
    {
        try
        {
            TraceFlyout("MainPanelControl close animation started");
            await panel.PlayFlyoutCloseAnimationAsync(cancellationToken);
            TraceFlyout("MainPanelControl close animation completed");
        }
        catch (OperationCanceledException)
        {
            TraceFlyout("MainPanelControl close animation canceled");
            return;
        }
        catch (Exception ex)
        {
            TraceFlyout($"MainPanelControl close animation failed: {ex.GetType().Name}");
        }

        if (_isDisposed || cancellationToken.IsCancellationRequested)
        {
            return;
        }

        _isRunningMainFlyoutCloseAnimation = false;
        _isMainFlyoutCloseAnimationComplete = true;
        TraceFlyout("_mainFlyout.Hide() requested after close animation");
        _mainFlyout.Hide();
    }

    private void OnMainFlyoutClosed(object? sender, object e)
    {
        TraceFlyout("_mainFlyout.Closed");
        _isRunningMainFlyoutCloseAnimation = false;
        _isMainFlyoutCloseAnimationComplete = false;
        _isMainFlyoutOpen = false;
        _lastMainFlyoutClosedAt = DateTime.UtcNow;

        if (_mainFlyout.Content is MainPanelControl panel)
        {
            panel.OnFlyoutClosed();
            _lastSnapshot = panel.Session.CreateSnapshot();
        }

        TraceFlyout("TrayHostWindow.HideFlyoutHost() requested from _mainFlyout.Closed");
        _containerWindow.HideFlyoutHost();
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        if (_uiSettings is not null)
        {
            _uiSettings.ColorValuesChanged -= OnColorValuesChanged;
            _uiSettings = null;
        }

        _mainFlyoutCloseAnimationCts?.Cancel();
        _mainFlyoutCloseAnimationCts?.Dispose();
        _mainFlyoutCloseAnimationCts = null;
        _mainFlyout.Opened -= OnMainFlyoutOpened;
        _mainFlyout.Closing -= OnMainFlyoutClosing;
        _mainFlyout.Closed -= OnMainFlyoutClosed;
        _contextMenu.Closed -= OnContextMenuClosed;
        _containerWindow.TaskbarCreated -= OnTaskbarCreated;
        if (_mainFlyout.Content is IDisposable disposableContent)
        {
            disposableContent.Dispose();
        }

        _mainFlyout.Content = null;
        _trayIcon.Selected -= OnTrayIconSelected;
        _trayIcon.ContextMenu -= OnTrayIconContextMenu;
        _trayIcon.Dispose();
    }

    private void OnTrayIconSelected(object? sender, TrayIconEventArgs args)
    {
        TraceFlyout($"Tray icon selected. IsOpen={_mainFlyout.IsOpen}, trackedOpen={_isMainFlyoutOpen}");

        if (_isRunningMainFlyoutCloseAnimation)
        {
            args.Handled = true;
            TraceFlyout("Tray icon selected while close animation is running; ignored duplicate hide request");
            return;
        }

        if (_mainFlyout.IsOpen)
        {
            args.Handled = true;
            TraceFlyout("_mainFlyout.Hide() requested from tray icon selected");
            _mainFlyout.Hide();
            return;
        }

        if (_isMainFlyoutOpen)
        {
            args.Handled = true;
            TraceFlyout("Tray icon selected while _mainFlyout is closing; ignored duplicate hide request");
            return;
        }

        if (DateTime.UtcNow - _lastMainFlyoutClosedAt < TrayLightDismissReopenGuard)
        {
            args.Handled = true;
            return;
        }

        EnsureFlyoutContent();
        args.Handled = true;
        ShowMainFlyoutAtCursor();
    }

    private void EnsureFlyoutContent()
    {
        if (_mainFlyout.Content is not MainPanelControl)
        {
            _mainFlyout.FlyoutPresenterStyle = CreateMainFlyoutPresenterStyle();
            _mainFlyout.Content = new MainPanelControl(_lastSnapshot);
        }
    }

    private Style CreateMainFlyoutPresenterStyle()
    {
        var panelHeight = GetMainPanelHeight();

        return new Style(typeof(FlyoutPresenter))
        {
            Setters =
            {
                new Setter(FrameworkElement.WidthProperty, 420d),
                new Setter(FrameworkElement.HeightProperty, panelHeight),
                new Setter(FrameworkElement.MarginProperty, new Thickness(0, -8, 0, 0)),
                new Setter(FrameworkElement.MaxWidthProperty, 10000d),
                new Setter(FrameworkElement.MaxHeightProperty, panelHeight),
                new Setter(Control.BackgroundProperty, new SolidColorBrush(Colors.Transparent)),
                new Setter(Control.BorderThicknessProperty, new Thickness(0)),
                new Setter(FlyoutPresenter.PaddingProperty, new Thickness(0)),
                new Setter(FlyoutPresenter.CornerRadiusProperty, new CornerRadius(8)),
                new Setter(FlyoutPresenter.IsDefaultShadowEnabledProperty, true),
                new Setter(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled),
            },
        };
    }

    private double GetMainPanelHeight()
    {
        var scale = GetDpiForWindow(Win32Interop.GetWindowFromWindowId(_containerWindow.AppWindow.Id)) / 96.0;
        var workArea = DisplayArea.GetFromWindowId(_containerWindow.AppWindow.Id, DisplayAreaFallback.Primary).WorkArea;
        return Math.Min(workArea.Height / scale - 80, AppSettings.MainPanelHeight);
    }

    private void OnTrayIconContextMenu(object? sender, TrayIconEventArgs args)
    {
        args.Handled = true;
        ShowContextMenuAtCursor();
    }

    private void ShowMainFlyoutAtCursor()
    {
        var cursor = GetCursorPosition();
        _containerWindow.PrepareFlyoutHost(cursor);
        if (_mainFlyout.Content is MainPanelControl panel)
        {
            panel.PrepareForFlyoutOpenAnimation(_uiSettings?.AnimationsEnabled == true);
        }

        var options = new FlyoutShowOptions
        {
            Placement = FlyoutPlacementMode.Top,
            Position = new Point(0, 0),
        };

        _mainFlyout.ShowAt(_containerWindow.MainFlyoutAnchor, options);
    }

    private void ShowContextMenuAtCursor()
    {
        var cursor = GetCursorPosition();
        _containerWindow.PrepareFlyoutHost(cursor);

        var options = new FlyoutShowOptions
        {
            Position = new Point(0, 0),
        };

        _contextMenu.ShowAt(_containerWindow.ContextFlyoutAnchor, options);
    }

    private void OnContextMenuClosed(object? sender, object e)
    {
        if (!_mainFlyout.IsOpen && !_isMainFlyoutOpen)
        {
            TraceFlyout("TrayHostWindow.HideFlyoutHost() requested from context menu closed");
            _containerWindow.HideFlyoutHost();
        }
    }

    private void RequestExit(Action exitAction)
    {
        TraceFlyout("Exit requested from context menu");
        _contextMenu.Hide();
        _containerWindow.DispatcherQueue.TryEnqueue(
            Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,
            () =>
            {
                TraceFlyout("Exit action running after context menu dismissed");
                exitAction();
            });
    }

    private void OnColorValuesChanged(UISettings sender, object args)
    {
        _containerWindow.DispatcherQueue.TryEnqueue(() =>
        {
            _trayIcon.SetIcon(GetIconPath());
        });
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

    private static PointInt32 GetCursorPosition()
    {
        return GetCursorPos(out var point)
            ? new PointInt32(point.X, point.Y)
            : new PointInt32(0, 0);
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

    private static void CollectReleasedPanelResources()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        using var process = Process.GetCurrentProcess();
        SetProcessWorkingSetSize(process.Handle, new IntPtr(-1), new IntPtr(-1));
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetProcessWorkingSetSize(
        IntPtr process,
        IntPtr minimumWorkingSetSize,
        IntPtr maximumWorkingSetSize);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

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
