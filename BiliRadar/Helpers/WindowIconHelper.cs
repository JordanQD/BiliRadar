using Microsoft.UI.Windowing;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace BiliRadar.Helpers;

internal static class WindowIconHelper
{
    private const string AppIconRelativePath = @"Assets\AppIcon.ico";
    private const int ImageIcon = 1;
    private const int IconSmall = 0;
    private const int IconBig = 1;
    private const int IconSmall2 = 2;
    private const int WmSetIcon = 0x0080;
    private const uint LrLoadFromFile = 0x00000010;
    private const uint LrDefaultSize = 0x00000040;
    private const uint LrShared = 0x00008000;

    public static void ApplyTo(AppWindow appWindow, nint hwnd)
    {
        var iconPath = Path.Combine(AppContext.BaseDirectory, AppIconRelativePath);
        if (!File.Exists(iconPath))
        {
            return;
        }

        try
        {
            appWindow.SetIcon(iconPath);
        }
        catch
        {
        }

        TrySetNativeWindowIcons(hwnd, iconPath);
    }

    private static void TrySetNativeWindowIcons(nint hwnd, string iconPath)
    {
        try
        {
            var icon = LoadImage(0, iconPath, ImageIcon, 0, 0, LrLoadFromFile | LrDefaultSize | LrShared);
            if (icon == 0)
            {
                return;
            }

            SendMessage(hwnd, WmSetIcon, IconSmall, icon);
            SendMessage(hwnd, WmSetIcon, IconBig, icon);
            SendMessage(hwnd, WmSetIcon, IconSmall2, icon);
        }
        catch
        {
        }
    }

    [DllImport("user32.dll", EntryPoint = "LoadImageW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint LoadImage(nint instance, string name, int type, int desiredWidth, int desiredHeight, uint load);

    [DllImport("user32.dll", EntryPoint = "SendMessageW")]
    private static extern nint SendMessage(nint hwnd, int message, nint wParam, nint lParam);
}
