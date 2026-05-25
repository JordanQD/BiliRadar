using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BiliRadar.Models;
using BiliRadar.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace BiliRadar.Pages;

public sealed partial class NotificationSettingsPage : Page
{
    private readonly BiliWebDataProvider _dataProvider = new(new CookieStore());
    private readonly UpdateMonitorService _updateMonitorService;
    private bool _isLoadingSettings;
    private Popup? _notificationPopup;
    private InfoBar? _notificationInfoBar;
    private Microsoft.UI.Dispatching.DispatcherQueueTimer? _notificationInfoTimer;

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
        SettingsScrollView.ScrollTo(0, 0);
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
        var dialogContentWidth = Math.Clamp((XamlRoot?.Size.Width ?? 0) * 0.45, 360, 480);
        var linkBox = new TextBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            MinWidth = 0,
            PlaceholderText = "https://space.bilibili.com/123456 或 UID",
        };
        var resolveButton = new Button
        {
            Width = 44,
            Height = 40,
            Padding = new Thickness(0),
            Content = new SymbolIcon(Symbol.Find),
        };
        var avatarPicture = new PersonPicture
        {
            Width = 56,
            Height = 56,
            Initials = "UP",
        };
        var creatorNameText = new TextBlock
        {
            Text = "未读取",
            Style = (Style)Application.Current.Resources["BodyStrongTextBlockStyle"],
        };
        var creatorDetailText = new TextBlock
        {
            Text = "输入链接或 UID 后读取 UP 信息。",
            Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            TextWrapping = TextWrapping.Wrap,
        };
        var videoSwitch = new ToggleSwitch
        {
            IsOn = true,
        };
        var liveSwitch = new ToggleSwitch
        {
            IsOn = true,
        };

        BiliCreator? resolvedCreator = null;

        var creatorInfoGrid = new Grid
        {
            ColumnSpacing = 12,
            Visibility = Visibility.Collapsed,
        };
        creatorInfoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        creatorInfoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        creatorInfoGrid.Children.Add(avatarPicture);
        var creatorTextPanel = new StackPanel
        {
            Spacing = 2,
            VerticalAlignment = VerticalAlignment.Center,
        };
        creatorTextPanel.Children.Add(creatorNameText);
        creatorTextPanel.Children.Add(creatorDetailText);
        Grid.SetColumn(creatorTextPanel, 1);
        creatorInfoGrid.Children.Add(creatorTextPanel);

        var notificationOptions = new StackPanel
        {
            Width = dialogContentWidth,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Spacing = 8,
        };
        notificationOptions.Children.Add(CreateDialogSwitchRow("视频更新通知", videoSwitch, dialogContentWidth));
        notificationOptions.Children.Add(CreateDialogSwitchRow("开播通知", liveSwitch, dialogContentWidth));

        var linkGrid = new Grid
        {
            Width = dialogContentWidth,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ColumnSpacing = 8,
        };
        linkGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        linkGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        linkGrid.Children.Add(linkBox);
        Grid.SetColumn(resolveButton, 1);
        linkGrid.Children.Add(resolveButton);

        var contentPanel = new StackPanel
        {
            Width = dialogContentWidth,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Spacing = 14,
        };
        contentPanel.Children.Add(linkGrid);
        contentPanel.Children.Add(creatorInfoGrid);
        contentPanel.Children.Add(notificationOptions);

        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = "添加UP主",
            Content = contentPanel,
            PrimaryButtonText = "保存",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Primary,
            IsPrimaryButtonEnabled = false,
        };

        linkBox.TextChanged += (_, _) =>
        {
            resolvedCreator = null;
            dialog.IsPrimaryButtonEnabled = false;
            creatorNameText.Text = "未读取";
            creatorDetailText.Text = "输入链接或 UID 后读取 UP 信息。";
            creatorInfoGrid.Visibility = Visibility.Collapsed;
            avatarPicture.ProfilePicture = null;
            avatarPicture.Initials = "UP";
        };

        void ShowDialogMessage(InfoBarSeverity severity, string title, string message)
        {
            ShowNotificationInfoBar(severity, title, message);
        }

        async Task ResolveCreatorForDialogAsync()
        {
            var mid = TryParseCreatorMid(linkBox.Text);
            if (mid <= 0)
            {
                ShowDialogMessage(InfoBarSeverity.Warning, "输入无效", "请输入有效的 UP 主页链接或 UID。");
                return;
            }

            if (CustomNotificationCreators.Any(item => item.Mid == mid))
            {
                ShowDialogMessage(InfoBarSeverity.Warning, "已存在", "这个 UP 已经在通知列表里。");
                return;
            }

            resolveButton.IsEnabled = false;
            ShowDialogMessage(InfoBarSeverity.Informational, "正在读取", "正在读取 UP 信息...");
            try
            {
                resolvedCreator = await TryResolveCreatorAsync(mid);
                creatorNameText.Text = resolvedCreator.Name;
                creatorDetailText.Text = $"UID {resolvedCreator.Mid}";
                creatorInfoGrid.Visibility = Visibility.Visible;
                avatarPicture.Initials = string.IsNullOrWhiteSpace(resolvedCreator.Name) ? "UP" : resolvedCreator.Name[..1];
                avatarPicture.ProfilePicture = string.IsNullOrWhiteSpace(resolvedCreator.AvatarUrl)
                    ? null
                    : new BitmapImage(new Uri(resolvedCreator.AvatarUrl));
                dialog.IsPrimaryButtonEnabled = true;
                ShowDialogMessage(InfoBarSeverity.Success, "读取完成", "已读取 UP 信息。");
            }
            catch (Exception ex)
            {
                ShowDialogMessage(InfoBarSeverity.Error, "读取失败", ex.Message);
            }
            finally
            {
                resolveButton.IsEnabled = true;
            }
        }

        resolveButton.Click += async (_, _) => await ResolveCreatorForDialogAsync();
        dialog.PrimaryButtonClick += async (_, args) =>
        {
            args.Cancel = true;
            if (resolvedCreator is null)
            {
                await ResolveCreatorForDialogAsync();
            }

            if (resolvedCreator is null)
            {
                return;
            }

            if (CustomNotificationCreators.Any(item => item.Mid == resolvedCreator.Mid))
            {
                ShowDialogMessage(InfoBarSeverity.Warning, "已存在", "这个 UP 已经在通知列表里。");
                return;
            }

            var subscription = new NotificationCreatorSubscription
            {
                Mid = resolvedCreator.Mid,
                Name = resolvedCreator.Name,
                AvatarUrl = resolvedCreator.AvatarUrl,
                VideoNotificationsEnabled = videoSwitch.IsOn,
                LiveNotificationsEnabled = liveSwitch.IsOn,
            };

            CustomNotificationCreators.Add(subscription);
            await SeedNotificationBaselineAsync(subscription);
            SaveCustomNotificationCreators();
            CustomNotificationStatusText.Text = $"已添加：{subscription.Name}";
            dialog.Hide();
        };

        await dialog.ShowAsync();
    }

    private static FrameworkElement CreateDialogSwitchRow(string label, ToggleSwitch toggleSwitch, double rowWidth)
    {
        toggleSwitch.OnContent = string.Empty;
        toggleSwitch.OffContent = string.Empty;
        toggleSwitch.Width = 64;
        toggleSwitch.MinWidth = 0;
        toggleSwitch.HorizontalAlignment = HorizontalAlignment.Right;
        toggleSwitch.RenderTransform = new TranslateTransform { X = 28 };

        var row = new Grid
        {
            Width = rowWidth,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ColumnSpacing = 12,
        };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        row.Children.Add(new TextBlock
        {
            Text = label,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center,
        });
        var stateText = new TextBlock
        {
            Width = 24,
            Text = toggleSwitch.IsOn ? "开" : "关",
            VerticalAlignment = VerticalAlignment.Center,
        };
        toggleSwitch.Toggled += (_, _) => stateText.Text = toggleSwitch.IsOn ? "开" : "关";
        Grid.SetColumn(stateText, 1);
        row.Children.Add(stateText);

        Grid.SetColumn(toggleSwitch, 2);
        row.Children.Add(toggleSwitch);

        return row;
    }

    private void ShowNotificationInfoBar(InfoBarSeverity severity, string title, string message)
    {
        _notificationInfoTimer ??= DispatcherQueue.CreateTimer();
        _notificationInfoTimer.Stop();
        _notificationInfoTimer.Interval = TimeSpan.FromSeconds(3);
        _notificationInfoTimer.Tick -= NotificationInfoTimer_Tick;
        _notificationInfoTimer.Tick += NotificationInfoTimer_Tick;

        if (_notificationInfoBar is null)
        {
            _notificationInfoBar = new InfoBar
            {
                Width = 380,
                IsClosable = false,
            };
            _notificationInfoBar.Loaded += (_, _) => RepositionNotificationInfoBar();
        }

        if (_notificationPopup is null)
        {
            _notificationPopup = new Popup
            {
                XamlRoot = XamlRoot,
                Child = _notificationInfoBar,
                IsLightDismissEnabled = false,
            };
        }

        _notificationInfoBar.Severity = severity;
        _notificationInfoBar.Title = title;
        _notificationInfoBar.Message = message;
        _notificationInfoBar.IsOpen = true;
        _notificationPopup.IsOpen = true;
        RepositionNotificationInfoBar();
        _notificationInfoTimer.Start();
    }

    private void RepositionNotificationInfoBar()
    {
        if (_notificationPopup is null || _notificationInfoBar is null)
        {
            return;
        }

        _notificationInfoBar.UpdateLayout();
        var width = _notificationInfoBar.ActualWidth > 0 ? _notificationInfoBar.ActualWidth : _notificationInfoBar.Width;
        var height = _notificationInfoBar.ActualHeight > 0 ? _notificationInfoBar.ActualHeight : 72;
        _notificationPopup.HorizontalOffset = Math.Max(24, XamlRoot.Size.Width - width - 24);
        _notificationPopup.VerticalOffset = Math.Max(24, XamlRoot.Size.Height - height - 24);
    }

    private void NotificationInfoTimer_Tick(Microsoft.UI.Dispatching.DispatcherQueueTimer sender, object args)
    {
        sender.Stop();
        if (_notificationInfoBar is not null)
        {
            _notificationInfoBar.IsOpen = false;
        }

        if (_notificationPopup is not null)
        {
            _notificationPopup.IsOpen = false;
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
            var allUpdates = await _updateMonitorService.GetCreatorVideoUpdatesAsync(subscription.Mid);
            AppSettings.KnownVideoUpdateIds = allUpdates
                .Select(item => item.Id)
                .Concat(AppSettings.KnownVideoUpdateIds)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            AppSettings.VideoNotificationBaselineInitialized = true;
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

            AppSettings.LiveNotificationBaselineInitialized = true;
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
