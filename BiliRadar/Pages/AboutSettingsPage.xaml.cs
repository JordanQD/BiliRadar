using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BiliRadar.Pages;

public sealed partial class AboutSettingsPage : Page
{
    public AboutSettingsPage()
    {
        InitializeComponent();
        Loaded += AboutSettingsPage_Loaded;
    }

    private void AboutSettingsPage_Loaded(object sender, RoutedEventArgs e)
    {
        SettingsScrollViewer.ChangeView(null, 0, null, true);
    }
}
