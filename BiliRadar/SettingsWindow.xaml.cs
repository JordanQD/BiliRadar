using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using BiliRadar.Helpers;
using BiliRadar.Pages;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;
using Windows.UI;
using WinRT.Interop;

namespace BiliRadar;

public sealed partial class SettingsWindow : Window
{
    private const int WindowWidth = 1360;
    private const int WindowHeight = 900;
    private const double TallTitleBarHeight = 48;

    private readonly AppWindow _appWindow;

    public SettingsWindow()
    {
        InitializeComponent();

        var hwnd = WindowNative.GetWindowHandle(this);
        _appWindow = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(hwnd));
        _appWindow.Title = LocalizationHelper.GetString("SettingsWindow.Title", "BiliRadar 设置");
        ResizeForCurrentDpi(hwnd);
        ConfigureTitleBar(hwnd);
        titleBar.Loaded += TitleBar_Loaded;

        ApplySystemThemeToCaptionButtons();
        RootGrid.ActualThemeChanged += (_, _) => ApplySystemThemeToCaptionButtons();

        if (_appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMaximizable = true;
            presenter.IsMinimizable = true;
        }

        GeneralNavItem.IsSelected = true;
        navFrame.Navigate(typeof(GeneralSettingsPage));
        RootGrid.SizeChanged += OnRootGridSizeChanged;
    }

    private void OnRootGridSizeChanged(object sender, SizeChangedEventArgs e)
    {
        navView.PaneDisplayMode = e.NewSize.Width switch
        {
            < 640 => NavigationViewPaneDisplayMode.LeftMinimal,
            < 1008 => NavigationViewPaneDisplayMode.LeftCompact,
            _ => NavigationViewPaneDisplayMode.Left,
        };
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

        if (navView.PaneDisplayMode == NavigationViewPaneDisplayMode.LeftMinimal)
        {
            navView.IsPaneOpen = false;
        }
    }

    private void TitleBar_PaneToggleRequested(TitleBar sender, object args)
    {
        navView.IsPaneOpen = !navView.IsPaneOpen;
    }

    private void ConfigureTitleBar(IntPtr hwnd)
    {
        this.ExtendsContentIntoTitleBar = true;

        var appTitleBar = _appWindow.TitleBar;
        appTitleBar.ExtendsContentIntoTitleBar = true;
        appTitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;

        var scale = GetDpiForWindow(hwnd) / 96.0;
        var titleBarHeight = appTitleBar.Height > 0
            ? appTitleBar.Height / scale
            : TallTitleBarHeight;

        titleBar.Height = titleBarHeight;
        titleBar.MinHeight = titleBarHeight;
        this.SetTitleBar(titleBar);
    }

    private void TitleBar_Loaded(object sender, RoutedEventArgs e)
    {
        titleBar.Loaded -= TitleBar_Loaded;
        RemovePaneToggleButtonInset();
    }

    private void RemovePaneToggleButtonInset()
    {
        foreach (var element in EnumerateDescendants(titleBar))
        {
            if (element is Button { Name: "PART_PaneToggleButton" } button)
            {
                button.Margin = new Thickness(0);
                break;
            }
        }
    }

    private static IEnumerable<DependencyObject> EnumerateDescendants(DependencyObject parent)
    {
        var childCount = VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < childCount; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            yield return child;

            foreach (var descendant in EnumerateDescendants(child))
            {
                yield return descendant;
            }
        }
    }

    private void ApplySystemThemeToCaptionButtons()
    {
        var foregroundColor = RootGrid.ActualTheme == ElementTheme.Dark ? Colors.White : Colors.Black;
        _appWindow.TitleBar.ButtonForegroundColor = foregroundColor;
        _appWindow.TitleBar.ButtonHoverForegroundColor = foregroundColor;
        _appWindow.TitleBar.ButtonHoverBackgroundColor = RootGrid.ActualTheme == ElementTheme.Dark
            ? Color.FromArgb(24, 255, 255, 255)
            : Color.FromArgb(24, 0, 0, 0);
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
