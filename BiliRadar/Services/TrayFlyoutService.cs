using BiliRadar.Controls;
using BiliRadar.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel;
using Windows.UI.ViewManagement;
using WinUIEx;
using Flyout = Microsoft.UI.Xaml.Controls.Flyout;
using MenuFlyout = Microsoft.UI.Xaml.Controls.MenuFlyout;

namespace BiliRadar.Services;

internal sealed class TrayFlyoutService : IDisposable
{
    private const uint TrayIconId = 1;

    private readonly TrayIcon _trayIcon;
    private readonly Flyout _mainFlyout;
    private readonly MenuFlyout _contextMenu;
    private readonly Window _containerWindow;
    private UISettings? _uiSettings;
    private bool _isDisposed;

    public TrayFlyoutService(
        Window containerWindow,
        Action settingsAction,
        Action exitAction)
    {
        _containerWindow = containerWindow;

        _trayIcon = new TrayIcon(TrayIconId, GetIconPath(), "BiliRadar")
        {
            IsVisible = true,
        };

        _mainFlyout = new Flyout
        {
            LightDismissOverlayMode = LightDismissOverlayMode.On,
            ShouldConstrainToRootBounds = false,
            FlyoutPresenterStyle = new Style(typeof(FlyoutPresenter))
            {
                Setters =
                {
                    new Setter(FlyoutPresenter.PaddingProperty, new Thickness(0)),
                    new Setter(FlyoutPresenter.CornerRadiusProperty, new CornerRadius(8)),
                },
            },
        };
        _mainFlyout.SystemBackdrop = new MicaBackdrop();
        _mainFlyout.Opened += OnMainFlyoutOpened;
        _mainFlyout.Closed += OnMainFlyoutClosed;

        _contextMenu = new MenuFlyout();
        _contextMenu.Items.Add(new MenuFlyoutItem
        {
            Text = LocalizationHelper.GetString("TraySettings"),
            Command = new DelegateCommand(settingsAction),
        });
        _contextMenu.Items.Add(new MenuFlyoutSeparator());
        _contextMenu.Items.Add(new MenuFlyoutItem
        {
            Text = LocalizationHelper.GetString("TrayExit"),
            Command = new DelegateCommand(exitAction),
        });

        _trayIcon.Selected += OnTrayIconSelected;
        _trayIcon.ContextMenu += OnTrayIconContextMenu;

        _uiSettings = new UISettings();
        _uiSettings.ColorValuesChanged += OnColorValuesChanged;
    }

    public void SetFlyoutContent(UIElement content)
    {
        _mainFlyout.Content = content;
    }

    public Task RefreshCurrentPanelPageAsync()
    {
        return _mainFlyout.Content is MainPanelControl panel
            ? panel.RefreshCurrentPageAsync()
            : Task.CompletedTask;
    }

    private void OnMainFlyoutOpened(object? sender, object e)
    {
        if (_mainFlyout.Content is MainPanelControl panel)
        {
            panel.OnFlyoutOpened();
        }
    }

    private void OnMainFlyoutClosed(object? sender, object e)
    {
        if (_mainFlyout.Content is MainPanelControl panel)
        {
            panel.OnFlyoutClosed();
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        if (_uiSettings is not null)
        {
            _uiSettings.ColorValuesChanged -= OnColorValuesChanged;
            _uiSettings = null;
        }

        _mainFlyout.Opened -= OnMainFlyoutOpened;
        _mainFlyout.Closed -= OnMainFlyoutClosed;
        if (_mainFlyout.Content is IDisposable disposableContent)
        {
            disposableContent.Dispose();
        }

        _mainFlyout.Content = null;
        _trayIcon.Selected -= OnTrayIconSelected;
        _trayIcon.ContextMenu -= OnTrayIconContextMenu;
        _trayIcon.Dispose();
    }

    private void OnTrayIconSelected(object? sender, TrayIconEventArgs args)
    {
        if (_mainFlyout.IsOpen)
        {
            _trayIcon.CloseFlyout();
            args.Handled = true;
            return;
        }

        args.Flyout = _mainFlyout;
    }

    private void OnTrayIconContextMenu(object? sender, TrayIconEventArgs args)
    {
        args.Flyout = _contextMenu;
    }

    private void OnColorValuesChanged(UISettings sender, object args)
    {
        _containerWindow.DispatcherQueue.TryEnqueue(() =>
        {
            _trayIcon.SetIcon(GetIconPath());
        });
    }

    private static string GetIconPath()
    {
        var assetName = IsSystemUsingLightTheme()
            ? "Assets/TrayIconDark.ico"
            : "Assets/TrayIconLight.ico";

        try
        {
            return Path.Combine(Package.Current.InstalledLocation.Path, assetName);
        }
        catch
        {
            return Path.Combine(AppContext.BaseDirectory, assetName);
        }
    }

    private static bool IsSystemUsingLightTheme()
    {
        const string personalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        using var key = Registry.CurrentUser.OpenSubKey(personalizeKey);
        return key?.GetValue("SystemUsesLightTheme") is int value ? value != 0 : true;
    }

    private sealed class DelegateCommand : ICommand
    {
        private readonly Action _execute;

        public DelegateCommand(Action execute) => _execute = execute;

        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => _execute();
    }
}
