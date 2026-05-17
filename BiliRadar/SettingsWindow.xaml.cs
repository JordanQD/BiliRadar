using System;
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
    private const int WindowWidth = 1280;
    private const int WindowHeight = 820;

    private readonly AppWindow _appWindow;

    public SettingsWindow()
    {
        InitializeComponent();

        var hwnd = WindowNative.GetWindowHandle(this);
        _appWindow = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(hwnd));
        _appWindow.Title = "BiliRadar 设置";
        _appWindow.Resize(new SizeInt32(WindowWidth, WindowHeight));
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
            "account" => typeof(AccountSettingsPage),
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
        appTitleBar.ButtonBackgroundColor = Colors.Transparent;
        appTitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
        appTitleBar.ButtonHoverBackgroundColor = Windows.UI.Color.FromArgb(28, 255, 255, 255);
        appTitleBar.ButtonPressedBackgroundColor = Windows.UI.Color.FromArgb(46, 255, 255, 255);
        appTitleBar.ButtonForegroundColor = Colors.White;
        appTitleBar.ButtonInactiveForegroundColor = Windows.UI.Color.FromArgb(160, 255, 255, 255);
        appTitleBar.ButtonHoverForegroundColor = Colors.White;
        appTitleBar.ButtonPressedForegroundColor = Colors.White;

        titleBar.Height = appTitleBar.Height;
    }
}
