using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Runtime.InteropServices;
using WinUIEx;
using WinUIEx.Messaging;
using WinRT.Interop;

namespace BiliRadar;

internal sealed class TrayHostWindow : Window
{
    private readonly AppWindow _appWindow;
    private readonly WindowMessageMonitor _messageMonitor;
    private readonly nint _hwnd;

    public event EventHandler? DisplayConfigurationChanged;

    public TrayHostWindow()
    {
        _hwnd = WindowNative.GetWindowHandle(this);
        _appWindow = AppWindow.GetFromWindowId(Microsoft.UI.Win32Interop.GetWindowIdFromWindow(_hwnd));
        _messageMonitor = new WindowMessageMonitor(_hwnd);
        _messageMonitor.WindowMessageReceived += OnWindowMessageReceived;
        Closed += TrayHostWindow_Closed;

        _appWindow.IsShownInSwitchers = false;
        _appWindow.Resize(new Windows.Graphics.SizeInt32(1, 1));
        this.SetExtendedWindowStyle(ExtendedWindowStyle.Transparent);
        this.SetWindowOpacity(0);
        ExtendsContentIntoTitleBar = true;

        // DesktopFlyouts creates independent XAML island windows. This hidden root only
        // keeps the WinUI dispatcher and application lifetime alive.
        Content = new Grid();

        if (_appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.SetBorderAndTitleBar(false, false);
            presenter.IsMinimizable = false;
            presenter.IsMaximizable = false;
            presenter.IsResizable = false;
        }
    }

    public void InitializeHidden()
    {
        HideFromAltTabAndTaskbar();
        Activate();
        _appWindow.Hide();
    }

    private void OnWindowMessageReceived(object? sender, WindowMessageEventArgs e)
    {
        if (e.Message.MessageId is WmDisplayChange or WmDpiChanged)
        {
            DisplayConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void TrayHostWindow_Closed(object sender, WindowEventArgs args)
    {
        Closed -= TrayHostWindow_Closed;
        _messageMonitor.WindowMessageReceived -= OnWindowMessageReceived;
        _messageMonitor.Dispose();
    }

    private void HideFromAltTabAndTaskbar()
    {
        var style = GetWindowLongPtr(_hwnd, GwlExStyle);
        SetWindowLongPtr(_hwnd, GwlExStyle, style | WsExToolWindow);
    }

    private const int GwlExStyle = -20;
    private const nint WsExToolWindow = 0x00000080;
    private const uint WmDisplayChange = 0x007E;
    private const uint WmDpiChanged = 0x02E0;

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern nint GetWindowLongPtr(nint hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern nint SetWindowLongPtr(nint hWnd, int nIndex, nint dwNewLong);

}
