using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BiliRadar.Helpers;
using BiliRadar.Models;
using BiliRadar.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;

namespace BiliRadar.Pages;

public sealed partial class NotificationSettingsPage : Page
{
    private BiliWebDataProvider? _dataProvider;
    private UpdateMonitorService? _updateMonitorService;
    private bool _isLoadingSettings;

    private BiliWebDataProvider DataProvider => _dataProvider ??= new(new CookieStore());
    private UpdateMonitorService UpdateMonitorService => _updateMonitorService ??= new(DataProvider);

    public ObservableCollection<NotificationCreatorSubscription> CustomNotificationCreators { get; } = [];

    public NotificationSettingsPage()
    {
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
        _dataProvider?.Dispose();
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
        const double dialogContentMaxWidth = 480;
        const double dialogVerticalSpacing = 12;
        var availableDialogWidth = Math.Max(280, (XamlRoot?.Size.Width ?? dialogContentMaxWidth) - 96);
        var dialogContentMinWidth = Math.Min(360, availableDialogWidth);

        // --- link input row ---
        var linkBox = new AutoSuggestBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            MinWidth = 0,
            PlaceholderText = LocalizationHelper.GetString("CreatorLinkPlaceholder"),
            QueryIcon = new SymbolIcon(Symbol.Find),
        };
        AutomationProperties.SetName(linkBox, LocalizationHelper.GetString("CreatorLinkAutomationName"));

        // --- creator info (always visible, placeholder initially) ---
        var avatarPicture = new PersonPicture
        {
            Width = 40,
            Height = 40,
            Initials = "UP",
        };
        var creatorNameText = new TextBlock
        {
            Text = LocalizationHelper.GetString("EnterLinkOrUid"),
            Style = (Style)Application.Current.Resources["BodyStrongTextBlockStyle"],
            TextTrimming = TextTrimming.CharacterEllipsis,
        };
        var creatorDetailText = new TextBlock
        {
            Text = LocalizationHelper.GetString("ClickQueryToFetch"),
            Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            TextWrapping = TextWrapping.Wrap,
        };

        var creatorInfoGrid = new Grid
        {
            ColumnSpacing = 12,
            VerticalAlignment = VerticalAlignment.Center,
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

        // --- error InfoBar (overlays creator info, animated) ---
        var dialogInfoBar = new InfoBar
        {
            IsClosable = false,
            IsOpen = false,
            Opacity = 0,
        };

        var fadeInAnimation = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = TimeSpan.FromMilliseconds(200),
        };
        var fadeOutAnimation = new DoubleAnimation
        {
            From = 1,
            To = 0,
            Duration = TimeSpan.FromMilliseconds(300),
        };

        var fadeInStoryboard = new Storyboard();
        fadeInStoryboard.Children.Add(fadeInAnimation);
        Storyboard.SetTarget(fadeInAnimation, dialogInfoBar);
        Storyboard.SetTargetProperty(fadeInAnimation, "Opacity");

        var fadeOutStoryboard = new Storyboard();
        fadeOutStoryboard.Children.Add(fadeOutAnimation);
        Storyboard.SetTarget(fadeOutAnimation, dialogInfoBar);
        Storyboard.SetTargetProperty(fadeOutAnimation, "Opacity");

        var dialogInfoTimer = DispatcherQueue.CreateTimer();
        dialogInfoTimer.Interval = TimeSpan.FromSeconds(3);
        dialogInfoTimer.Tick += (_, _) =>
        {
            dialogInfoTimer.Stop();
            fadeOutStoryboard.Begin();
        };
        fadeOutStoryboard.Completed += (_, _) =>
        {
            dialogInfoBar.IsOpen = false;
        };

        // overlay: InfoBar sits on top of creator info, same height
        var infoOverlayGrid = new Grid
        {
            MinHeight = 52,
        };
        infoOverlayGrid.Children.Add(creatorInfoGrid);
        infoOverlayGrid.Children.Add(dialogInfoBar);

        // --- notification toggles ---
        var videoSwitch = new ToggleSwitch { IsOn = true };
        var liveSwitch = new ToggleSwitch { IsOn = true };
        var notificationOptions = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Spacing = dialogVerticalSpacing,
        };
        notificationOptions.Children.Add(CreateDialogSwitchRow(LocalizationHelper.GetString("NotificationSettings.VideoNotifyCard.Header"), videoSwitch));
        notificationOptions.Children.Add(CreateDialogSwitchRow(LocalizationHelper.GetString("NotificationSettings.LiveNotifyCard.Header"), liveSwitch));

        // --- content panel ---
        var contentPanel = new StackPanel
        {
            MinWidth = dialogContentMinWidth,
            MaxWidth = dialogContentMaxWidth,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Spacing = dialogVerticalSpacing,
        };
        contentPanel.Children.Add(linkBox);
        contentPanel.Children.Add(infoOverlayGrid);
        contentPanel.Children.Add(notificationOptions);

        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = LocalizationHelper.GetString("AddCreatorDialogTitle"),
            Content = contentPanel,
            PrimaryButtonText = LocalizationHelper.GetString("SaveButton"),
            CloseButtonText = LocalizationHelper.GetString("CancelButton"),
            DefaultButton = ContentDialogButton.Primary,
            IsPrimaryButtonEnabled = false,
        };

        BiliCreator? resolvedCreator = null;

        // --- reset to placeholder ---
        void ResetToPlaceholder()
        {
            resolvedCreator = null;
            dialog.IsPrimaryButtonEnabled = false;
            avatarPicture.ProfilePicture = null;
            avatarPicture.Initials = "UP";
            creatorNameText.Text = LocalizationHelper.GetString("EnterLinkOrUid");
            creatorDetailText.Text = LocalizationHelper.GetString("ClickQueryToFetch");
            DismissInfoBar();
        }

        // --- show error overlay ---
        void ShowError(InfoBarSeverity severity, string title, string message)
        {
            dialogInfoBar.Severity = severity;
            dialogInfoBar.Title = title;
            dialogInfoBar.Message = message;
            dialogInfoBar.IsOpen = true;
            fadeInStoryboard.Begin();
            dialogInfoTimer.Stop();
            dialogInfoTimer.Start();
        }

        // --- dismiss info bar immediately ---
        void DismissInfoBar()
        {
            dialogInfoTimer.Stop();
            if (dialogInfoBar.IsOpen)
            {
                dialogInfoBar.IsOpen = false;
                dialogInfoBar.Opacity = 0;
            }
        }

        // --- resolve creator ---
        async Task ResolveCreatorForDialogAsync()
        {
            var mid = TryParseCreatorMid(linkBox.Text);
            if (mid <= 0)
            {
                ShowError(InfoBarSeverity.Warning, LocalizationHelper.GetString("InputInvalid"), LocalizationHelper.GetString("InputInvalidDetail"));
                return;
            }

            if (CustomNotificationCreators.Any(item => item.Mid == mid))
            {
                ShowError(InfoBarSeverity.Warning, LocalizationHelper.GetString("AlreadyExists"), LocalizationHelper.GetString("AlreadyExistsDetail"));
                return;
            }

            linkBox.IsEnabled = false;
            creatorDetailText.Text = LocalizationHelper.GetString("FetchingCreatorInfo");
            DismissInfoBar();
            try
            {
                resolvedCreator = await TryResolveCreatorAsync(mid);
                avatarPicture.Initials = string.IsNullOrWhiteSpace(resolvedCreator.Name) ? "UP" : resolvedCreator.Name[..1];
                avatarPicture.ProfilePicture = string.IsNullOrWhiteSpace(resolvedCreator.AvatarUrl)
                    ? null
                    : new BitmapImage(new Uri(resolvedCreator.AvatarUrl));
                creatorNameText.Text = resolvedCreator.Name;
                creatorDetailText.Text = $"UID {resolvedCreator.Mid}";
                dialog.IsPrimaryButtonEnabled = true;
            }
            catch (Exception ex)
            {
                ShowError(InfoBarSeverity.Error, LocalizationHelper.GetString("FetchFailed"), ex.Message);
            }
            finally
            {
                linkBox.IsEnabled = true;
            }
        }

        linkBox.TextChanged += (_, args) =>
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                ResetToPlaceholder();
            }
        };
        linkBox.QuerySubmitted += async (_, _) => await ResolveCreatorForDialogAsync();

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
                ShowError(InfoBarSeverity.Warning, LocalizationHelper.GetString("AlreadyExists"), LocalizationHelper.GetString("AlreadyExistsDetail"));
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
            CustomNotificationStatusText.Text = LocalizationHelper.Format("AddedCreator", subscription.Name);
            dialog.Hide();
        };

        await dialog.ShowAsync();
    }

    private static FrameworkElement CreateDialogSwitchRow(string label, ToggleSwitch toggleSwitch)
    {
        toggleSwitch.OnContent = null;
        toggleSwitch.OffContent = null;
        toggleSwitch.Width = 48;
        toggleSwitch.MinWidth = 0;
        toggleSwitch.HorizontalAlignment = HorizontalAlignment.Right;
        AutomationProperties.SetName(toggleSwitch, label);

        var row = new Grid
        {
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
            Text = toggleSwitch.IsOn ? LocalizationHelper.GetString("On") : LocalizationHelper.GetString("Off"),
            VerticalAlignment = VerticalAlignment.Center,
        };
        toggleSwitch.Toggled += (_, _) => stateText.Text = toggleSwitch.IsOn ? LocalizationHelper.GetString("On") : LocalizationHelper.GetString("Off");
        Grid.SetColumn(stateText, 1);
        row.Children.Add(stateText);

        Grid.SetColumn(toggleSwitch, 2);
        row.Children.Add(toggleSwitch);

        return row;
    }

    private async Task<BiliCreator> TryResolveCreatorAsync(long mid)
    {
        try
        {
            return await UpdateMonitorService.GetCreatorAsync(mid)
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
        CustomNotificationStatusText.Text = LocalizationHelper.Format("RemovedCreator", subscription.Name);
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
            var allUpdates = await UpdateMonitorService.GetCreatorVideoUpdatesAsync(subscription.Mid);
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
            var liveCreator = await UpdateMonitorService.GetCreatorLiveAsync(subscription.Mid);
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
