using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BiliRadar.Models;
using BiliRadar.Services;
using Microsoft.UI.Windowing;
using Windows.Graphics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinRT.Interop;

namespace BiliRadar;

public sealed partial class MainWindow : Window, INotifyPropertyChanged
{
    private const int WindowWidthDip = 348;
    private const int WindowMinHeightDip = 100;
    private const int WindowMaxHeightDip = 650;
    private const int WindowRightMarginDip = 12;
    private const int WindowBottomMarginDip = 12;
    private const double WindowMaxWorkAreaHeightRatio = 0.75;
    private const int DefaultDpi = 96;
    private const uint MdtEffectiveDpi = 0;
    private readonly CookieStore _cookieStore = new();
    private readonly UpdateMonitorService _updateMonitorService;
    private readonly AppWindow _appWindow;
    private readonly nint _hwnd;
    private bool _allowClose;
    private bool _isLoading;
    private bool _isShowingWindow;
    private bool _isVisible;
    private int _unreadCount;
    private int _followingCount;
    private string _lastCheckedText = "尚未检查";
    private string _statusText = "粘贴 B 站 Cookie 后保存并刷新。";

    public MainWindow()
    {
        InitializeComponent();
        Title = "BiliRadar";

        Updates = [];
        Following = [];
        _updateMonitorService = new(new BiliWebDataProvider(_cookieStore));
        _hwnd = WindowNative.GetWindowHandle(this);
        _appWindow = AppWindow.GetFromWindowId(Microsoft.UI.Win32Interop.GetWindowIdFromWindow(_hwnd));
        _appWindow.Title = "BiliRadar";
        _appWindow.Resize(new SizeInt32(WindowWidthDip, WindowMinHeightDip));
        _appWindow.Closing += AppWindow_Closing;
        Activated += MainWindow_Activated;

        if (_appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.IsResizable = false;
            presenter.SetBorderAndTitleBar(false, false);
        }

        ConfigureTitleBar();
        DisableWindowMovingAndResizing();
        HideFromTaskbar();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event Action? HideRequested;

    public ObservableCollection<VideoUpdateRow> Updates { get; }

    public ObservableCollection<CreatorRow> Following { get; }

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    public int UnreadCount
    {
        get => _unreadCount;
        private set => SetProperty(ref _unreadCount, value);
    }

    public int FollowingCount
    {
        get => _followingCount;
        private set => SetProperty(ref _followingCount, value);
    }

    public string LastCheckedText
    {
        get => _lastCheckedText;
        private set => SetProperty(ref _lastCheckedText, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public Visibility LoadingVisibility => IsLoading ? Visibility.Visible : Visibility.Collapsed;

    public bool IsVisible => _isVisible;

    public void InitializeHidden()
    {
        Activate();
        HideWindow();
    }

    public void ShowWindow()
    {
        _isShowingWindow = true;
        try
        {
            AdjustWindowSizeToContent();
            Activate();
            ShowWindowNative(_hwnd, SwShow);
            SetWindowPos(_hwnd, HwndTopmost, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpNoActivate | SwpShowWindow);
            SetForegroundWindow(_hwnd);
            _isVisible = true;
        }
        finally
        {
            _isShowingWindow = false;
        }
    }

    public void HideWindow()
    {
        ShowWindowNative(_hwnd, SwHide);
        _appWindow.Hide();
        _isVisible = false;
    }

    public void CloseForExit()
    {
        _allowClose = true;
        Close();
    }

    public async Task RefreshAsync()
    {
        IsLoading = true;
        try
        {
            StatusText = _cookieStore.HasCookie ? "正在刷新..." : "还没有保存 Cookie。";
            var following = await _updateMonitorService.GetFollowingAsync();
            var updates = await _updateMonitorService.RefreshAsync();

            Following.Clear();
            foreach (var creator in following)
            {
                Following.Add(new CreatorRow(creator));
            }

            Updates.Clear();
            foreach (var update in updates.OrderByDescending(item => item.PublishedAt))
            {
                Updates.Add(new VideoUpdateRow(update));
            }

            FollowingCount = Following.Count;
            UnreadCount = Updates.Count(item => item.IsUnread);
            LastCheckedText = _updateMonitorService.LastCheckedAt.ToString("HH:mm:ss");
            StatusText = _cookieStore.HasCookie
                ? $"已获取 {FollowingCount} 个关注用户。"
                : "粘贴 B 站 Cookie 后保存并刷新。";
            AdjustWindowSizeToContent();
        }
        catch (Exception ex)
        {
            StatusText = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await RefreshAsync();
    }

    private void HideButton_Click(object sender, RoutedEventArgs e)
    {
        HideRequested?.Invoke();
    }

    private async void SaveCookieButton_Click(object sender, RoutedEventArgs e)
    {
        _cookieStore.SaveCookieString(CookieBox.Password);
        CookieBox.Password = string.Empty;
        StatusText = "Cookie 已保存。";
        await RefreshAsync();
    }

    private void ClearCookieButton_Click(object sender, RoutedEventArgs e)
    {
        _cookieStore.Clear();
        Following.Clear();
        Updates.Clear();
        FollowingCount = 0;
        UnreadCount = 0;
        LastCheckedText = "尚未检查";
        StatusText = "Cookie 已清除。";
        AdjustWindowSizeToContent();
    }

    private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState == WindowActivationState.Deactivated && !_isShowingWindow)
        {
            HideRequested?.Invoke();
        }
    }

    private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (_allowClose)
        {
            return;
        }

        args.Cancel = true;
        HideRequested?.Invoke();
    }

    private void SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(storage, value))
        {
            return;
        }

        storage = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        if (propertyName == nameof(IsLoading))
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LoadingVisibility)));
        }
    }

    private void ConfigureTitleBar()
    {
        var titleBar = _appWindow.TitleBar;
        if (titleBar is null)
        {
            return;
        }

        titleBar.ExtendsContentIntoTitleBar = true;
        titleBar.PreferredHeightOption = TitleBarHeightOption.Collapsed;
        titleBar.SetDragRectangles([]);
    }

    private void AdjustWindowSizeToContent()
    {
        RootGrid.UpdateLayout();
        MainContainer.Measure(new Windows.Foundation.Size(WindowWidthDip, double.PositiveInfinity));
        var contentHeight = (int)Math.Ceiling(MainContainer.DesiredSize.Height);
        var finalHeightDip = Math.Min(Math.Max(contentHeight, WindowMinHeightDip), GetAdaptiveWindowMaxHeightDip());
        PositionWindowBottomRight(WindowWidthDip, finalHeightDip, WindowRightMarginDip, WindowBottomMarginDip);
    }

    private static int GetAdaptiveWindowMaxHeightDip()
    {
        if (!TryGetDisplayAreaAtCursor(out var displayArea) || displayArea is null)
        {
            return WindowMaxHeightDip;
        }

        var dpiScale = GetDpiScale(displayArea);
        var workAreaHeightDip = ScaleToDip(displayArea.WorkArea.Height, dpiScale);
        return (int)Math.Floor(workAreaHeightDip * WindowMaxWorkAreaHeightRatio);
    }

    private void PositionWindowBottomRight(int widthDip, int heightDip, int rightMarginDip, int bottomMarginDip)
    {
        if (!TryGetDisplayAreaAtCursor(out var displayArea) || displayArea is null)
        {
            return;
        }

        var dpiScale = GetDpiScale(displayArea);
        var work = displayArea.WorkArea;

        var width = ScaleToPhysicalPixels(widthDip, dpiScale);
        var height = ScaleToPhysicalPixels(heightDip, dpiScale);
        var marginRight = ScaleToPhysicalPixels(rightMarginDip, dpiScale);
        var marginBottom = ScaleToPhysicalPixels(bottomMarginDip, dpiScale);

        width = Math.Min(width, Math.Max(0, work.Width - marginRight));
        height = Math.Min(height, Math.Max(0, work.Height - marginBottom));

        var x = work.X + work.Width - width - marginRight;
        var y = work.Y + work.Height - height - marginBottom;
        MoveAndResizeOnDisplay(displayArea, new RectInt32(x, y, width, height));
    }

    private void MoveAndResizeOnDisplay(DisplayArea targetDisplay, RectInt32 finalRect)
    {
        var currentDisplay = DisplayArea.GetFromWindowId(_appWindow.Id, DisplayAreaFallback.Nearest);
        var needsTeleport = currentDisplay is null || currentDisplay.DisplayId.Value != targetDisplay.DisplayId.Value;

        if (needsTeleport)
        {
            var work = targetDisplay.WorkArea;
            _appWindow.MoveAndResize(new RectInt32(work.X, work.Y, 1, 1));
        }

        _appWindow.MoveAndResize(finalRect);
    }

    private static bool TryGetDisplayAreaAtCursor(out DisplayArea? displayArea)
    {
        displayArea = null;
        if (!GetCursorPos(out var cursor))
        {
            return false;
        }

        displayArea = DisplayArea.GetFromPoint(new PointInt32(cursor.X, cursor.Y), DisplayAreaFallback.Nearest);
        return displayArea is not null;
    }

    private static double GetDpiScale(DisplayArea displayArea)
    {
        var monitor = Microsoft.UI.Win32Interop.GetMonitorFromDisplayId(displayArea.DisplayId);
        return (double)GetEffectiveDpi(monitor) / DefaultDpi;
    }

    private static int ScaleToPhysicalPixels(int dip, double dpiScale)
    {
        return (int)Math.Ceiling(dip * dpiScale);
    }

    private static int ScaleToDip(int physicalPixels, double dpiScale)
    {
        return (int)Math.Floor(physicalPixels / dpiScale);
    }

    private static int GetEffectiveDpi(nint monitor)
    {
        if (monitor == 0)
        {
            return DefaultDpi;
        }

        var hr = GetDpiForMonitor(monitor, MdtEffectiveDpi, out var dpiX, out _);
        return hr >= 0 && dpiX > 0 ? (int)dpiX : DefaultDpi;
    }

    private void HideFromTaskbar()
    {
        var exStyle = GetWindowLongPtr(_hwnd, GwlExStyle);
        SetWindowLongPtr(_hwnd, GwlExStyle, (nint)((long)exStyle | WsExToolWindow));
    }

    private void DisableWindowMovingAndResizing()
    {
        var style = GetWindowLongPtr(_hwnd, GwlStyle);
        style = (nint)((long)style & ~WsThickFrame & ~WsMaximizeBox & ~WsMinimizeBox & ~WsCaption & ~WsSysMenu);
        SetWindowLongPtr(_hwnd, GwlStyle, style);

        var exStyle = GetWindowLongPtr(_hwnd, GwlExStyle);
        exStyle = (nint)((long)exStyle & ~WsExDlgModalFrame & ~WsExWindowEdge & ~WsExClientEdge & ~WsExStaticEdge);
        SetWindowLongPtr(_hwnd, GwlExStyle, exStyle);

        SetWindowPos(_hwnd, 0, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpFrameChanged);
    }

    [DllImport("user32.dll", EntryPoint = "ShowWindow")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindowNative(nint hwnd, int command);

    [DllImport("user32.dll", EntryPoint = "SetForegroundWindow")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(nint hwnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out Point point);

    [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(nint hwnd, nint insertAfter, int x, int y, int width, int height, uint flags);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern nint GetWindowLongPtr(nint hwnd, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern nint SetWindowLongPtr(nint hwnd, int index, nint newLong);

    [DllImport("shcore.dll")]
    private static extern int GetDpiForMonitor(nint monitor, uint dpiType, out uint dpiX, out uint dpiY);

    private const int SwHide = 0;
    private const int SwShow = 5;
    private const int GwlStyle = -16;
    private const int GwlExStyle = -20;
    private const long WsCaption = 0x00C00000L;
    private const long WsThickFrame = 0x00040000L;
    private const long WsMinimizeBox = 0x00020000L;
    private const long WsMaximizeBox = 0x00010000L;
    private const long WsSysMenu = 0x00080000L;
    private const long WsExToolWindow = 0x00000080L;
    private const long WsExDlgModalFrame = 0x00000001L;
    private const long WsExWindowEdge = 0x00000100L;
    private const long WsExClientEdge = 0x00000200L;
    private const long WsExStaticEdge = 0x00020000L;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpShowWindow = 0x0040;
    private const uint SwpFrameChanged = 0x0020;
    private static readonly nint HwndTopmost = new(-1);

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }

}
