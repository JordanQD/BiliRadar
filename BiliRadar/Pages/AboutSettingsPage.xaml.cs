using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;

namespace BiliRadar.Pages;

public sealed partial class AboutSettingsPage : Page
{
    private const string IssuesUrl = "https://github.com/JordanQD/BiliRadar/issues";

    public string Version => GetVersionText();

    public AboutSettingsPage()
    {
        InitializeComponent();
        Loaded += AboutSettingsPage_Loaded;
    }

    private void AboutSettingsPage_Loaded(object sender, RoutedEventArgs e)
    {
        SettingsScrollViewer.ChangeView(null, 0, null, true);
    }

    private void CloneRepositoryCard_Click(object sender, RoutedEventArgs e)
    {
        var dataPackage = new DataPackage();
        dataPackage.SetText(GitCloneTextBlock.Text);
        Clipboard.SetContent(dataPackage);
        AboutStatusText.Text = "已复制克隆命令。";
    }

    private async void IssueRequestCard_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await Launcher.LaunchUriAsync(new Uri(IssuesUrl));
            AboutStatusText.Text = $"已打开：{IssuesUrl}";
        }
        catch (Exception ex)
        {
            AboutStatusText.Text = $"打开链接失败：{ex.Message}";
        }
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
}
