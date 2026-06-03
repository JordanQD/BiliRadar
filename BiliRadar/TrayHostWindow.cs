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

        // 保留一个最小 XAML 根节点，给 WinUIEx 托盘对象提供宿主线程和 XamlRoot。
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

    private void HideFromAltTabAndTaskbar()
    {
        var style = GetWindowLongPtr(_hwnd, GwlExStyle);
        SetWindowLongPtr(_hwnd, GwlExStyle, style | WsExToolWindow);
    }

    private const int GwlExStyle = -20;
    private const nint WsExToolWindow = 0x00000080;

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern nint GetWindowLongPtr(nint hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern nint SetWindowLongPtr(nint hWnd, int nIndex, nint dwNewLong);
}
