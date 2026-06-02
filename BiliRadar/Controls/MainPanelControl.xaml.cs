using BiliRadar.Models;
using BiliRadar.Pages;
using BiliRadar.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Linq;

namespace BiliRadar.Controls;

public sealed partial class MainPanelControl : UserControl
{
    private int _previousSelectedPageIndex = -1;

    public MainPanelSession Session { get; }

    public MainPanelControl()
    {
        InitializeComponent();
        Session = new MainPanelSession(new CookieStore());
        SetDefaultPage();
        ContentFrame.Navigated += OnContentFrameNavigated;
    }

    public MainPanelControl(MainWindowSnapshot? snapshot)
    {
        InitializeComponent();
        Session = new MainPanelSession(new CookieStore(), snapshot);
        SetDefaultPage();
        ContentFrame.Navigated += OnContentFrameNavigated;
    }

    private void SetDefaultPage()
    {
        var selectedItem = AppSettings.DefaultOpenPage switch
        {
            DefaultOpenPage.History => HistorySelectorItem,
            DefaultOpenPage.ViewLater => ViewLaterSelectorItem,
            _ => FollowingSelectorItem,
        };
        ContentSelectorBar.SelectedItem = selectedItem;
    }

    private void OnContentFrameNavigated(object sender, NavigationEventArgs e)
    {
        if (e.Content is IMainPanelPage page)
        {
            page.Initialize(Session);
        }
    }

    private void ContentSelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        var currentIndex = sender.Items.IndexOf(sender.SelectedItem);
        var pageType = currentIndex switch
        {
            0 => typeof(FollowingPage),
            1 => typeof(HistoryPage),
            _ => typeof(ViewLaterPage),
        };

        if (ContentFrame.Content?.GetType() == pageType)
        {
            ResetPageScrollPosition();
            if (ContentFrame.Content is IMainPanelPage existingPage)
                _ = existingPage.ActivateAsync();
            _previousSelectedPageIndex = currentIndex;
            return;
        }

        var effect = currentIndex > _previousSelectedPageIndex
            ? SlideNavigationTransitionEffect.FromRight
            : SlideNavigationTransitionEffect.FromLeft;

        ContentFrame.Navigate(
            pageType,
            null,
            new SlideNavigationTransitionInfo { Effect = effect });

        _previousSelectedPageIndex = currentIndex;
        ResetPageScrollPosition();

        if (ContentFrame.Content is IMainPanelPage newPage)
            _ = newPage.ActivateAsync();
    }

    private void ResetPageScrollPosition()
    {
        (ContentFrame.Content as FollowingPage)?.ResetScrollPosition();
        (ContentFrame.Content as HistoryPage)?.ResetScrollPosition();
        (ContentFrame.Content as ViewLaterPage)?.ResetScrollPosition();
    }

    private async void OpenBrowserButton_Click(object sender, RoutedEventArgs e)
    {
        var section = ContentSelectorBar.SelectedItem switch
        {
            SelectorBarItem item when item == HistorySelectorItem => MainPanelSection.History,
            SelectorBarItem item when item == ViewLaterSelectorItem => MainPanelSection.ViewLater,
            _ => MainPanelSection.Following,
        };
        await Session.LaunchSelectedBrowserUriAsync(section);
    }

    private void StatusNotificationInfoBar_CloseButtonClick(InfoBar sender, object args)
    {
        if (sender.DataContext is StatusNotification notification)
        {
            notification.AutoDismissTimer?.Stop();
            notification.AutoDismissTimer = null;
            Session.StatusNotifications.Remove(notification);
        }
    }
}
