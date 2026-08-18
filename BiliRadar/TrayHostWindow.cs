using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Runtime.InteropServices;
using WinRT.Interop;

namespace BiliRadar;

internal sealed class TrayHostWindow : Window
{
    private readonly AppWindow _appWindow;
    private readonly nint _hwnd;

    public TrayHostWindow()
    {
        _hwnd = WindowNative.GetWindowHandle(this);
        _appWindow = AppWindow.GetFromWindowId(Microsoft.UI.Win32Interop.GetWindowIdFromWindow(_hwnd));

        _appWindow.IsShownInSwitchers = false;
        _appWindow.Resize(new Windows.Graphics.SizeInt32(1, 1));
        ConfigureInvisibleNativeHost();
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
        Activate();
        _appWindow.Hide();
    }

    private void ConfigureInvisibleNativeHost()
    {
        var style = GetWindowLongPtr(_hwnd, GwlExStyle);
        SetWindowLongPtr(
            _hwnd,
            GwlExStyle,
            style | WsExToolWindow | WsExLayered | WsExTransparent);
        SetLayeredWindowAttributes(_hwnd, 0, 0, LwaAlpha);
    }

    private const int GwlExStyle = -20;
    private const nint WsExTransparent = 0x00000020;
    private const nint WsExToolWindow = 0x00000080;
    private const nint WsExLayered = 0x00080000;
    private const uint LwaAlpha = 0x00000002;

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern nint GetWindowLongPtr(nint hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern nint SetWindowLongPtr(nint hWnd, int nIndex, nint dwNewLong);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetLayeredWindowAttributes(
        nint hWnd,
        uint colorKey,
        byte alpha,
        uint flags);

}
