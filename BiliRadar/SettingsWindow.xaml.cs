using System;
using System.Runtime.InteropServices;
using BiliRadar.Pages;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;
using WinRT.Interop;

namespace BiliRadar;

public sealed partial class SettingsWindow : Window
{
    private const int WindowWidth = 1360;
    private const int WindowHeight = 900;

    private readonly AppWindow _appWindow;

    public SettingsWindow()
    {
        InitializeComponent();

        var hwnd = WindowNative.GetWindowHandle(this);
        _appWindow = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(hwnd));
        _appWindow.Title = "BiliRadar 设置";
        ResizeForCurrentDpi(hwnd);
        ConfigureTitleBar();

        if (_appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMaximizable = true;
            presenter.IsMinimizable = true;
        }

        GeneralNavItem.IsSelected = true;
        navFrame.Navigate(typeof(GeneralSettingsPage));
    }

    public void ShowWindow()
    {
        Activate();
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItemContainer?.Tag is not string tag)
        {
            return;
        }

        var pageType = tag switch
        {
            "general" => typeof(GeneralSettingsPage),
            "notification" => typeof(NotificationSettingsPage),
            "about" => typeof(AboutSettingsPage),
            _ => typeof(GeneralSettingsPage),
        };

        if (navFrame.CurrentSourcePageType != pageType)
        {
            navFrame.Navigate(pageType);
        }
    }

    private void TitleBar_PaneToggleRequested(TitleBar sender, object args)
    {
        navView.IsPaneOpen = !navView.IsPaneOpen;
    }

    private void ConfigureTitleBar()
    {
        var appTitleBar = _appWindow.TitleBar;
        appTitleBar.ExtendsContentIntoTitleBar = true;
        appTitleBar.PreferredHeightOption = TitleBarHeightOption.Standard;

        titleBar.Height = appTitleBar.Height;
    }

    private void ResizeForCurrentDpi(IntPtr hwnd)
    {
        var scale = GetDpiForWindow(hwnd) / 96.0;
        _appWindow.Resize(new SizeInt32(
            (int)Math.Round(WindowWidth * scale),
            (int)Math.Round(WindowHeight * scale)));
    }

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);
}
