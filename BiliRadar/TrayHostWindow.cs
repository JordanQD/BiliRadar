using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace BiliRadar;

internal sealed class TrayHostWindow : Window
{
    private readonly AppWindow _appWindow;

    public TrayHostWindow()
    {
        var hwnd = WindowNative.GetWindowHandle(this);
        _appWindow = AppWindow.GetFromWindowId(Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd));
        _appWindow.IsShownInSwitchers = false;
    }

    public void InitializeVisible()
    {
        Activate();
    }

    public void InitializeHidden()
    {
        Activate();
        _appWindow.Hide();
    }
}
