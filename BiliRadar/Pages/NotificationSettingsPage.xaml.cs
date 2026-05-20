using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BiliRadar.Models;
using BiliRadar.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BiliRadar.Pages;

public sealed partial class NotificationSettingsPage : Page
{
    private readonly BiliWebDataProvider _dataProvider = new(new CookieStore());
    private readonly UpdateMonitorService _updateMonitorService;
    private bool _isLoadingSettings;

    public ObservableCollection<NotificationCreatorSubscription> CustomNotificationCreators { get; } = [];

    public NotificationSettingsPage()
    {
        _updateMonitorService = new UpdateMonitorService(_dataProvider);
        InitializeComponent();
        CustomNotificationCreatorsList.ItemsSource = CustomNotificationCreators;
        Loaded += NotificationSettingsPage_Loaded;
        Unloaded += NotificationSettingsPage_Unloaded;
    }

    private void NotificationSettingsPage_Loaded(object sender, RoutedEventArgs e)
    {
        _isLoadingSettings = true;
        NotificationCheckIntervalBox.Value = AppSettings.NotificationCheckIntervalMinutes;
        NotificationTargetModeBox.SelectedIndex = AppSettings.NotificationTargetMode == NotificationTargetMode.CustomCreators ? 1 : 0;
        VideoNotificationSwitch.IsOn = AppSettings.VideoNotificationsEnabled;
        LiveNotificationSwitch.IsOn = AppSettings.LiveNotificationsEnabled;
        CustomNotificationCreators.Clear();
        foreach (var creator in AppSettings.CustomNotificationCreators)
        {
            CustomNotificationCreators.Add(creator);
        }

        UpdateNotificationModeVisibility();
        _isLoadingSettings = false;
        SettingsScrollViewer.ChangeView(null, 0, null, true);
    }

    private void NotificationSettingsPage_Unloaded(object sender, RoutedEventArgs e)
    {
        _dataProvider.Dispose();
    }

    private void VideoNotificationSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isLoadingSettings)
        {
            return;
        }

        AppSettings.VideoNotificationsEnabled = VideoNotificationSwitch.IsOn;
    }

    private void LiveNotificationSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isLoadingSettings)
        {
            return;
        }

        AppSettings.LiveNotificationsEnabled = LiveNotificationSwitch.IsOn;
    }

    private void NotificationCheckIntervalBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (_isLoadingSettings || double.IsNaN(args.NewValue))
        {
            return;
        }

        AppSettings.NotificationCheckIntervalMinutes = Math.Max(1, (int)Math.Round(args.NewValue));
    }

    private void NotificationTargetModeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoadingSettings)
        {
            return;
        }

        AppSettings.NotificationTargetMode = NotificationTargetModeBox.SelectedIndex == 1
            ? NotificationTargetMode.CustomCreators
            : NotificationTargetMode.AllFollowing;
        UpdateNotificationModeVisibility();
    }

    private async void AddCreatorButton_Click(object sender, RoutedEventArgs e)
    {
        var mid = TryParseCreatorMid(CreatorInputBox.Text);
        if (mid <= 0)
        {
            CustomNotificationStatusText.Text = "请输入有效的 UP 主页链接或 UID。";
            return;
        }

        if (CustomNotificationCreators.Any(item => item.Mid == mid))
        {
            CustomNotificationStatusText.Text = "这个 UP 已经在通知列表里。";
            return;
        }

        AddCreatorButton.IsEnabled = false;
        CustomNotificationStatusText.Text = "正在读取 UP 信息...";
        try
        {
            var creator = await TryResolveCreatorAsync(mid);
            var subscription = new NotificationCreatorSubscription
            {
                Mid = creator.Mid,
                Name = creator.Name,
                AvatarUrl = creator.AvatarUrl,
                VideoNotificationsEnabled = true,
                LiveNotificationsEnabled = true,
            };

            CustomNotificationCreators.Add(subscription);
            SaveCustomNotificationCreators();
            await SeedNotificationBaselineAsync(subscription);
            CreatorInputBox.Text = string.Empty;
            CustomNotificationStatusText.Text = $"已添加：{subscription.Name}";
        }
        catch (Exception ex)
        {
            CustomNotificationStatusText.Text = $"添加失败：{ex.Message}";
        }
        finally
        {
            AddCreatorButton.IsEnabled = true;
        }
    }

    private async Task<BiliCreator> TryResolveCreatorAsync(long mid)
    {
        try
        {
            return await _updateMonitorService.GetCreatorAsync(mid)
                ?? new BiliCreator(mid, $"UID {mid}", string.Empty);
        }
        catch
        {
            return new BiliCreator(mid, $"UID {mid}", string.Empty);
        }
    }

    private void CustomVideoNotificationSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isLoadingSettings || sender is not ToggleSwitch { Tag: NotificationCreatorSubscription subscription })
        {
            return;
        }

        subscription.VideoNotificationsEnabled = ((ToggleSwitch)sender).IsOn;
        SaveCustomNotificationCreators();
    }

    private void CustomLiveNotificationSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isLoadingSettings || sender is not ToggleSwitch { Tag: NotificationCreatorSubscription subscription })
        {
            return;
        }

        subscription.LiveNotificationsEnabled = ((ToggleSwitch)sender).IsOn;
        SaveCustomNotificationCreators();
    }

    private void RemoveCreatorButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: NotificationCreatorSubscription subscription })
        {
            return;
        }

        CustomNotificationCreators.Remove(subscription);
        SaveCustomNotificationCreators();
        CustomNotificationStatusText.Text = $"已删除：{subscription.Name}";
    }

    private void UpdateNotificationModeVisibility()
    {
        var isCustomMode = AppSettings.NotificationTargetMode == NotificationTargetMode.CustomCreators;
        var globalVisibility = isCustomMode ? Visibility.Collapsed : Visibility.Visible;

        GlobalVideoNotificationCard.Visibility = globalVisibility;
        GlobalLiveNotificationCard.Visibility = globalVisibility;
        CustomNotificationPanel.Visibility = isCustomMode ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SaveCustomNotificationCreators()
    {
        AppSettings.CustomNotificationCreators = CustomNotificationCreators.ToList();
    }

    private async Task SeedNotificationBaselineAsync(NotificationCreatorSubscription subscription)
    {
        try
        {
            var updates = await _updateMonitorService.GetCreatorVideoUpdatesAsync(subscription.Mid);
            AppSettings.KnownVideoUpdateIds = updates
                .Select(item => item.Id)
                .Concat(AppSettings.KnownVideoUpdateIds)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch
        {
        }

        try
        {
            var liveCreator = await _updateMonitorService.GetCreatorLiveAsync(subscription.Mid);
            if (liveCreator is not null)
            {
                AppSettings.KnownLiveRoomIds = new[] { liveCreator.RoomId.ToString() }
                    .Concat(AppSettings.KnownLiveRoomIds)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
        }
        catch
        {
        }
    }

    private static long TryParseCreatorMid(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        var match = Regex.Match(text, @"space\.bilibili\.com/(\d+)", RegexOptions.IgnoreCase);
        if (match.Success && long.TryParse(match.Groups[1].Value, out var urlMid))
        {
            return urlMid;
        }

        match = Regex.Match(text, @"\d+");
        return match.Success && long.TryParse(match.Value, out var mid) ? mid : 0;
    }
}
