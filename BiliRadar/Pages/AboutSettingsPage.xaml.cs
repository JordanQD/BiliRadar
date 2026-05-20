using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel;
using Windows.Storage;

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
        VersionText.Text = GetVersionText();
        PublisherText.Text = GetPublisherText();
        DataLocationText.Text = ApplicationData.Current.LocalFolder.Path;
        SettingsScrollViewer.ChangeView(null, 0, null, true);
    }

    private static string GetVersionText()
    {
        try
        {
            var version = Package.Current.Id.Version;
            return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }
        catch
        {
            return typeof(AboutSettingsPage).Assembly.GetName().Version?.ToString() ?? "未知";
        }
    }

    private static string GetPublisherText()
    {
        try
        {
            return Package.Current.PublisherDisplayName;
        }
        catch
        {
            return "Q";
        }
    }
}
