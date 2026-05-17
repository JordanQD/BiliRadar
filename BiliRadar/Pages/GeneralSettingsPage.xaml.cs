using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BiliRadar.Pages;

public sealed partial class GeneralSettingsPage : Page
{
    public GeneralSettingsPage()
    {
        InitializeComponent();
        Loaded += GeneralSettingsPage_Loaded;
    }

    private void GeneralSettingsPage_Loaded(object sender, RoutedEventArgs e)
    {
        SettingsScrollViewer.ChangeView(null, 0, null, true);
    }
}
