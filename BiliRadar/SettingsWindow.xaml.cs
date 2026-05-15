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
    private readonly AppWindow _appWindow;

    public SettingsWindow()
    {
        InitializeComponent();

        var hwnd = WindowNative.GetWindowHandle(this);
        _appWindow = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(hwnd));
        _appWindow.Title = "BiliRadar 设置";
        _appWindow.Resize(new SizeInt32(920, 640));

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

    private void NavFrame_Navigated(object sender, Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        titleBar.IsBackButtonVisible = navFrame.CanGoBack;
    }

    private void TitleBar_BackRequested(TitleBar sender, object args)
    {
        if (navFrame.CanGoBack)
        {
            navFrame.GoBack();
        }
    }

    private void TitleBar_PaneToggleRequested(TitleBar sender, object args)
    {
        navView.IsPaneOpen = !navView.IsPaneOpen;
    }
}
