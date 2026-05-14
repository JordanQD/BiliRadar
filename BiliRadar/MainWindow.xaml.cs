using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BiliRadar.Models;
using BiliRadar.Services;
using Microsoft.UI.Windowing;
using Windows.Graphics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using WinRT.Interop;
using Windows.System;
using Windows.Storage.Streams;

namespace BiliRadar;

public sealed partial class MainWindow : Window, INotifyPropertyChanged
{
    private const string CollectionsAddIconData = "M11.0656 8.00389L11.25 7.99875H18.75C20.483 7.99875 21.8992 9.3552 21.9949 11.0643L22 11.2487V12.8096C21.5557 12.3832 21.051 12.0194 20.5 11.7322V11.2487C20.5 10.2822 19.7165 9.49875 18.75 9.49875H11.25C10.3318 9.49875 9.57881 10.2059 9.5058 11.1052L9.5 11.2487V18.7487C9.5 19.6669 10.2071 20.4199 11.1065 20.4929L11.25 20.4987H11.7316C12.0186 21.0497 12.3822 21.5544 12.8084 21.9987H11.25C9.51697 21.9987 8.10075 20.6423 8.00514 18.9332L8 18.7487V11.2487C8 9.51571 9.35645 8.0995 11.0656 8.00389ZM15.5818 4.23284L15.6345 4.40964L16.327 6.998H14.774L14.1856 4.79787C13.9355 3.86431 12.9759 3.31029 12.0423 3.56044L4.79787 5.50158C3.91344 5.73856 3.36966 6.61227 3.52756 7.49737L3.56044 7.64488L5.50158 14.8893C5.69372 15.6064 6.30445 16.0996 7.00045 16.1764L7.00056 17.6816C5.69932 17.6051 4.52962 16.7445 4.10539 15.4544L4.05269 15.2776L2.11155 8.03311C1.66301 6.35913 2.6067 4.6401 4.23284 4.10539L4.40964 4.05269L11.6541 2.11155C13.3281 1.66301 15.0471 2.6067 15.5818 4.23284ZM23 17.5C23 14.4624 20.5376 12 17.5 12C14.4624 12 12 14.4624 12 17.5C12 20.5376 14.4624 23 17.5 23C20.5376 23 23 20.5376 23 17.5ZM17.4101 14.0073L17.5 13.9992L17.5899 14.0073C17.794 14.0443 17.9549 14.2053 17.9919 14.4094L18 14.4992L17.9996 16.9992L20.5046 17L20.5944 17.0081C20.7985 17.0451 20.9595 17.206 20.9965 17.4101L21.0046 17.5L20.9965 17.5899C20.9595 17.794 20.7985 17.9549 20.5944 17.9919L20.5046 18L18.0007 17.9992L18.0011 20.5035L17.9931 20.5934C17.956 20.7975 17.7951 20.9584 17.591 20.9954L17.5011 21.0035L17.4112 20.9954C17.2071 20.9584 17.0462 20.7975 17.0092 20.5934L17.0011 20.5035L17.0007 17.9992L14.4977 18L14.4078 17.9919C14.2037 17.9549 14.0427 17.794 14.0057 17.5899L13.9977 17.5L14.0057 17.4101C14.0427 17.206 14.2037 17.0451 14.4078 17.0081L14.4977 17L16.9996 16.9992L17 14.4992L17.0081 14.4094C17.0451 14.2053 17.206 14.0443 17.4101 14.0073Z";
    private const string PersonDeleteIconData = "M17.5 12C20.5375661 12 23 14.4624339 23 17.5C23 20.5375661 20.5375661 23 17.5 23C14.4624339 23 12 20.5375661 12 17.5C12 14.4624339 14.4624339 12 17.5 12ZM12.0222607 13.9993086C11.7255613 14.4626083 11.4860296 14.9660345 11.3136172 15.4996352L4.25354153 15.499921C3.83932796 15.499921 3.50354153 15.8357075 3.50354153 16.249921L3.50354153 17.1572408C3.50354153 17.8128951 3.78953221 18.4359296 4.28670709 18.8633654C5.5447918 19.9450082 7.44080155 20.5010712 10 20.5010712C10.598839 20.5010712 11.1614445 20.4706245 11.6881394 20.4101192C11.9370538 20.9102887 12.2508544 21.3740111 12.6170965 21.7904935C11.8149076 21.9312924 10.9419626 22.0010712 10 22.0010712C7.11050247 22.0010712 4.87168436 21.3444691 3.30881727 20.0007885C2.48019625 19.2883988 2.00354153 18.2500002 2.00354153 17.1572408L2.00354153 16.249921C2.00354153 15.0072804 3.01090084 13.999921 4.25354153 13.999921L12.0222607 13.9993086ZM15.0930472 14.9662824L15.0237993 15.0241379L14.9659438 15.0933858C14.8478223 15.2638954 14.8478223 15.4914871 14.9659438 15.6619968L15.0237993 15.7312446L16.7933527 17.5006913L15.0263884 19.2674911L14.968533 19.3367389C14.8504114 19.5072486 14.8504114 19.7348403 14.968533 19.9053499L15.0263884 19.9745978L15.0956363 20.0324533C15.2661459 20.1505748 15.4937377 20.1505748 15.6642473 20.0324533L15.7334952 19.9745978L17.5003527 18.2076913L19.2693951 19.9768405L19.338643 20.0346959C19.5091526 20.1528175 19.7367444 20.1528175 19.907254 20.0346959L19.9765019 19.9768405L20.0343574 19.9075926C20.1524789 19.737083 20.1524789 19.5094912 20.0343574 19.3389816L19.9765019 19.2697337L18.2073527 17.5006913L19.9792686 15.7312918L20.0371241 15.6620439C20.1552456 15.4915343 20.1552456 15.2639425 20.0371241 15.0934329L19.9792686 15.024185L19.9100208 14.9663296C19.7395111 14.848208 19.5119194 14.848208 19.3414098 14.9663296L19.2721619 15.024185L17.5003527 16.7936913L15.7309061 15.0241379L15.6616582 14.9662824C15.5155071 14.8650354 15.3274181 14.8505715 15.1692847 14.9228908L15.0930472 14.9662824ZM10 2.0046246C12.7614237 2.0046246 15 4.24320085 15 7.0046246C15 9.76604835 12.7614237 12.0046246 10 12.0046246C7.23857625 12.0046246 5 9.76604835 5 7.0046246C5 4.24320085 7.23857625 2.0046246 10 2.0046246ZM10 3.5046246C8.06700338 3.5046246 6.5 5.07162798 6.5 7.0046246C6.5 8.93762123 8.06700338 10.5046246 10 10.5046246C11.9329966 10.5046246 13.5 8.93762123 13.5 7.0046246C13.5 5.07162798 11.9329966 3.5046246 10 3.5046246Z";
    private const int WindowWidthDip = 420;
    private const int WindowMinHeightDip = 100;
    private const int WindowMaxHeightDip = 760;
    private const int WindowRightMarginDip = 12;
    private const int WindowBottomMarginDip = 12;
    private const double WindowMaxWorkAreaHeightRatio = 0.75;
    private const int DefaultDpi = 96;
    private const int ImageLoadMaxAttemptCount = 3;
    private const uint MdtEffectiveDpi = 0;
    private static readonly TimeSpan ImageLoadRetryDelay = TimeSpan.FromMilliseconds(450);
    private static readonly HttpClient ImageHttpClient = CreateImageHttpClient();
    private readonly CookieStore _cookieStore = new();
    private readonly UpdateMonitorService _updateMonitorService;
    private readonly HashSet<string> _loadedUpdateIds = new(StringComparer.OrdinalIgnoreCase);
    private readonly AppWindow _appWindow;
    private readonly nint _hwnd;
    private bool _allowClose;
    private bool _isLoading;
    private bool _refreshQueuedOnShow;
    private bool _isLoadingMore;
    private bool _isShowingWindow;
    private bool _isVisible;
    private bool _isStatusInfoOpen;
    private bool _hasMoreUpdates = true;
    private int _unreadCount;
    private int _followingCount;
    private string _lastCheckedText = "尚未检查";
    private string _statusText = string.Empty;
    private string _followingListText = "暂无关注数据";
    private InfoBarSeverity _statusSeverity = InfoBarSeverity.Informational;

    public MainWindow()
    {
        InitializeComponent();
        Title = "BiliRadar";

        Updates = [];
        Following = [];
        _updateMonitorService = new(new BiliWebDataProvider(_cookieStore));
        _hwnd = WindowNative.GetWindowHandle(this);
        _appWindow = AppWindow.GetFromWindowId(Microsoft.UI.Win32Interop.GetWindowIdFromWindow(_hwnd));
        _appWindow.Title = "BiliRadar";
        _appWindow.Resize(new SizeInt32(WindowWidthDip, WindowMinHeightDip));
        _appWindow.Closing += AppWindow_Closing;
        Activated += MainWindow_Activated;

        if (_appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.IsResizable = false;
            presenter.SetBorderAndTitleBar(false, false);
        }

        ConfigureTitleBar();
        DisableWindowMovingAndResizing();
        HideFromTaskbar();
        ContentSelectorBar.SelectedItem = FollowingSelectorItem;
        ShowSelectedPage(FollowingSelectorItem);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event Action? HideRequested;

    public ObservableCollection<VideoUpdateRow> Updates { get; }

    public ObservableCollection<CreatorRow> Following { get; }

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    public int UnreadCount
    {
        get => _unreadCount;
        private set => SetProperty(ref _unreadCount, value);
    }

    public int FollowingCount
    {
        get => _followingCount;
        private set => SetProperty(ref _followingCount, value);
    }

    public string LastCheckedText
    {
        get => _lastCheckedText;
        private set => SetProperty(ref _lastCheckedText, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public InfoBarSeverity StatusSeverity
    {
        get => _statusSeverity;
        private set => SetProperty(ref _statusSeverity, value);
    }

    public bool IsStatusInfoOpen
    {
        get => _isStatusInfoOpen;
        private set => SetProperty(ref _isStatusInfoOpen, value);
    }

    public string FollowingListText
    {
        get => _followingListText;
        private set => SetProperty(ref _followingListText, value);
    }

    public Visibility LoadingVisibility => IsLoading ? Visibility.Visible : Visibility.Collapsed;

    public bool IsVisible => _isVisible;

    public void InitializeHidden()
    {
        Activate();
        HideWindow();
    }

    public void ShowWindow()
    {
        _isShowingWindow = true;
        try
        {
            AdjustWindowSizeToContent();
            Activate();
            ShowWindowNative(_hwnd, SwShow);
            SetWindowPos(_hwnd, HwndTopmost, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpNoActivate | SwpShowWindow);
            SetForegroundWindow(_hwnd);
            _isVisible = true;
            _ = RefreshOnShowAsync();
        }
        finally
        {
            _isShowingWindow = false;
        }
    }

    public void HideWindow()
    {
        ShowWindowNative(_hwnd, SwHide);
        _appWindow.Hide();
        _isVisible = false;
    }

    public void CloseForExit()
    {
        _allowClose = true;
        Close();
    }

    public async Task RefreshAsync()
    {
        if (IsLoading)
        {
            return;
        }

        IsLoading = true;
        try
        {
            IsStatusInfoOpen = false;
            if (!_cookieStore.HasCookie)
            {
                ShowStatus("还没有保存 Cookie。", InfoBarSeverity.Warning);
            }

            var following = await _updateMonitorService.GetFollowingAsync();

            Following.Clear();
            foreach (var creator in following)
            {
                Following.Add(new CreatorRow(creator));
            }

            FollowingCount = Following.Count;
            FollowingListText = Following.Count == 0
                ? "暂无关注数据"
                : string.Join(Environment.NewLine, Following.Select(item => $"{item.Name}  UID:{item.Mid}"));

            IReadOnlyList<BiliVideoUpdate> updates = [];
            try
            {
                updates = await _updateMonitorService.RefreshAsync();
            }
            catch (Exception ex)
            {
                ShowStatus($"视频动态加载失败：{ex.Message}", InfoBarSeverity.Error);
            }

            var newRows = new List<VideoUpdateRow>();
            foreach (var update in updates.OrderByDescending(item => item.PublishedAt))
            {
                if (_loadedUpdateIds.Add(update.Id))
                {
                    var row = new VideoUpdateRow(update);
                    Updates.Insert(newRows.Count, row);
                    newRows.Add(row);
                }
            }

            if (VideoCardsPanel.Children.Count == 0)
            {
                RenderVideoCards();
            }
            else
            {
                for (var index = newRows.Count - 1; index >= 0; index--)
                {
                    VideoCardsPanel.Children.Insert(0, CreateVideoCard(newRows[index]));
                }
            }

            _hasMoreUpdates = Updates.Count > 0;
            UnreadCount = Updates.Count(item => item.IsUnread);
            LastCheckedText = _updateMonitorService.LastCheckedAt.ToString("HH:mm:ss");
            if (_cookieStore.HasCookie && Updates.Count == 0 && !IsStatusInfoOpen)
            {
                ShowStatus("暂无视频动态。", InfoBarSeverity.Informational);
            }

            AdjustWindowSizeToContent();
        }
        catch (Exception ex)
        {
            ShowStatus(ex.Message, InfoBarSeverity.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RefreshOnShowAsync()
    {
        if (_refreshQueuedOnShow)
        {
            return;
        }

        _refreshQueuedOnShow = true;
        try
        {
            await RefreshAsync();
        }
        finally
        {
            _refreshQueuedOnShow = false;
        }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await RefreshAsync();
    }

    private void HideButton_Click(object sender, RoutedEventArgs e)
    {
        HideRequested?.Invoke();
    }

    private void ContentSelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        ShowSelectedPage(sender.SelectedItem);
    }

    private void ShowSelectedPage(SelectorBarItem? selectedItem)
    {
        FollowingPagePanel.Visibility = selectedItem == FollowingSelectorItem ? Visibility.Visible : Visibility.Collapsed;
        HistoryPagePanel.Visibility = selectedItem == HistorySelectorItem ? Visibility.Visible : Visibility.Collapsed;
        ViewLaterPagePanel.Visibility = selectedItem == ViewLaterSelectorItem ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void VideoScrollViewer_ViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
    {
        if (e.IsIntermediate || ContentSelectorBar.SelectedItem != FollowingSelectorItem)
        {
            return;
        }

        var distanceToBottom = VideoScrollViewer.ScrollableHeight - VideoScrollViewer.VerticalOffset;
        if (distanceToBottom <= 40)
        {
            await LoadMoreUpdatesAsync();
        }
    }

    private async Task LoadMoreUpdatesAsync()
    {
        if (_isLoading || _isLoadingMore || !_hasMoreUpdates)
        {
            return;
        }

        _isLoadingMore = true;
        try
        {
            var page = await _updateMonitorService.LoadMoreAsync();
            _hasMoreUpdates = page.HasMore;

            var addedCount = 0;
            foreach (var update in page.Items.OrderByDescending(item => item.PublishedAt))
            {
                if (AddUpdateIfNew(update))
                {
                    addedCount++;
                    VideoCardsPanel.Children.Add(CreateVideoCard(Updates[^1]));
                }
            }
        }
        catch (Exception ex)
        {
            ShowStatus($"加载更早内容失败：{ex.Message}", InfoBarSeverity.Error);
        }
        finally
        {
            _isLoadingMore = false;
        }
    }

    private bool AddUpdateIfNew(BiliVideoUpdate update)
    {
        if (!_loadedUpdateIds.Add(update.Id))
        {
            return false;
        }

        Updates.Add(new VideoUpdateRow(update));
        return true;
    }

    private void ClearCookieButton_Click(object sender, RoutedEventArgs e)
    {
        _cookieStore.Clear();
        Following.Clear();
        Updates.Clear();
        _loadedUpdateIds.Clear();
        _hasMoreUpdates = false;
        FollowingCount = 0;
        UnreadCount = 0;
        VideoCardsPanel.Children.Clear();
        FollowingListText = "暂无关注数据";
        LastCheckedText = "尚未检查";
        ShowStatus("Cookie 已清除。", InfoBarSeverity.Informational);
        AdjustWindowSizeToContent();
    }

    private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState == WindowActivationState.Deactivated && !_isShowingWindow)
        {
            HideRequested?.Invoke();
        }
    }

    private void RenderVideoCards()
    {
        VideoCardsPanel.Children.Clear();
        if (Updates.Count == 0)
        {
            VideoCardsPanel.Children.Add(new TextBlock
            {
                Text = "暂无视频动态",
                FontSize = 13,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            });
            return;
        }

        foreach (var item in Updates)
        {
            VideoCardsPanel.Children.Add(CreateVideoCard(item));
        }
    }

    private FrameworkElement CreateVideoCard(VideoUpdateRow item)
    {
        var card = new Border
        {
            Padding = new Thickness(10, 8, 10, 8),
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(82, 0, 0, 0)),
            CornerRadius = new CornerRadius(6),
        };

        var root = new Grid
        {
            ColumnSpacing = 10,
            MinHeight = 92,
        };
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        root.ColumnDefinitions.Add(new ColumnDefinition());
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var avatar = CreateAvatar(item);
        root.Children.Add(avatar);

        var textPanel = CreateListTextPanel(item);
        Grid.SetColumn(textPanel, 1);
        root.Children.Add(textPanel);

        var cover = CreateCompactCover(item);
        Grid.SetColumn(cover, 2);
        root.Children.Add(cover);

        card.Child = root;
        return card;
    }

    private static FrameworkElement CreateAvatar(VideoUpdateRow item)
    {
        var avatarHost = new Border
        {
            Width = 38,
            Height = 38,
            Margin = new Thickness(0, 2, 0, 0),
            VerticalAlignment = VerticalAlignment.Top,
            Background = (Brush)Application.Current.Resources["CardBackgroundFillColorSecondaryBrush"],
            CornerRadius = new CornerRadius(19),
        };

        if (!string.IsNullOrWhiteSpace(item.AvatarUrl))
        {
            avatarHost.Child = CreateRemoteImage(item.AvatarUrl, Stretch.UniformToFill);
        }

        return avatarHost;
    }

    private static FrameworkElement CreateListTextPanel(VideoUpdateRow item)
    {
        var panel = new Grid
        {
            RowSpacing = 3,
            VerticalAlignment = VerticalAlignment.Center,
        };
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var creator = new TextBlock
        {
            Text = item.CreatorName,
            FontSize = 12,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            TextTrimming = TextTrimming.CharacterEllipsis,
            TextWrapping = TextWrapping.NoWrap,
        };
        panel.Children.Add(creator);

        var title = new TextBlock
        {
            Text = item.Title,
            FontSize = 13,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            MaxLines = 2,
            Foreground = (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"],
            TextTrimming = TextTrimming.CharacterEllipsis,
            TextWrapping = TextWrapping.Wrap,
        };
        Grid.SetRow(title, 1);
        panel.Children.Add(title);

        var description = new TextBlock
        {
            Text = item.Description,
            FontSize = 12,
            MaxLines = 1,
            Foreground = (Brush)Application.Current.Resources["TextFillColorTertiaryBrush"],
            TextTrimming = TextTrimming.CharacterEllipsis,
            TextWrapping = TextWrapping.NoWrap,
            Visibility = string.IsNullOrWhiteSpace(item.Description) ? Visibility.Collapsed : Visibility.Visible,
        };
        Grid.SetRow(description, 2);
        panel.Children.Add(description);

        var time = new TextBlock
        {
            Text = item.Tip,
            FontSize = 12,
            Foreground = (Brush)Application.Current.Resources["TextFillColorTertiaryBrush"],
            TextTrimming = TextTrimming.CharacterEllipsis,
            TextWrapping = TextWrapping.NoWrap,
        };
        Grid.SetRow(time, 3);
        panel.Children.Add(time);

        return panel;
    }

    private FrameworkElement CreateCompactCover(VideoUpdateRow item)
    {
        var root = new Grid
        {
            Width = 128,
            Height = 72,
            VerticalAlignment = VerticalAlignment.Center,
        };

        var coverHost = new Button
        {
            Width = 128,
            Height = 72,
            Padding = new Thickness(0),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch,
            Style = (Style)Application.Current.Resources["SubtleButtonStyle"],
            Tag = item,
        };
        coverHost.Click += CoverButton_Click;

        var coverFrame = new Border
        {
            Background = (Brush)Application.Current.Resources["CardBackgroundFillColorSecondaryBrush"],
            CornerRadius = new CornerRadius(7),
        };

        if (!string.IsNullOrWhiteSpace(item.CoverUrl))
        {
            coverFrame.Child = CreateRemoteImage(item.CoverUrl, Stretch.UniformToFill);
        }

        coverHost.Content = coverFrame;
        root.Children.Add(coverHost);

        var viewLaterButton = CreateViewLaterButton(item);
        viewLaterButton.Width = 28;
        viewLaterButton.Height = 28;
        viewLaterButton.Margin = new Thickness(5);
        viewLaterButton.Content = CreatePathIcon(CollectionsAddIconData, 15, "White");
        root.Children.Add(viewLaterButton);

        if (!string.IsNullOrWhiteSpace(item.DurationText))
        {
            var duration = new Border
            {
                Margin = new Thickness(5),
                Padding = new Thickness(4, 1, 4, 1),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(180, 0, 0, 0)),
                CornerRadius = new CornerRadius(4),
                Child = new TextBlock
                {
                    Text = item.DurationText,
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                },
            };
            root.Children.Add(duration);
        }

        return root;
    }

    private Button CreateViewLaterButton(VideoUpdateRow item)
    {
        var viewLaterButton = new Button
        {
            Width = 32,
            Height = 32,
            MinWidth = 0,
            Margin = new Thickness(8),
            Padding = new Thickness(0),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            IsEnabled = item.Aid > 0,
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
            Tag = item,
            Content = CreatePathIcon(CollectionsAddIconData, 18, "White"),
        };
        SetOverlayButtonResources(viewLaterButton);
        SetOverlayButtonState(viewLaterButton, OverlayButtonState.Normal);
        viewLaterButton.PointerEntered += (_, _) => SetOverlayButtonState(viewLaterButton, OverlayButtonState.Hover);
        viewLaterButton.PointerExited += (_, _) => SetOverlayButtonState(viewLaterButton, OverlayButtonState.Normal);
        viewLaterButton.PointerPressed += (_, _) => SetOverlayButtonState(viewLaterButton, OverlayButtonState.Pressed);
        viewLaterButton.PointerReleased += (_, _) => SetOverlayButtonState(viewLaterButton, OverlayButtonState.Hover);
        viewLaterButton.PointerCanceled += (_, _) => SetOverlayButtonState(viewLaterButton, OverlayButtonState.Normal);
        ToolTipService.SetToolTip(viewLaterButton, "添加到稍后再看");
        viewLaterButton.Click += AddToViewLaterButton_Click;
        return viewLaterButton;
    }

    private static MenuFlyout CreateCreatorMenuFlyout()
    {
        var flyout = new MenuFlyout();
        flyout.Items.Add(new MenuFlyoutItem
        {
            Text = "取消关注",
            FontFamily = new FontFamily("Microsoft YaHei UI"),
            Icon = CreateMenuPathIcon(PersonDeleteIconData, 24),
        });
        return flyout;
    }

    private FrameworkElement CreateCover(VideoUpdateRow item)
    {
        var root = new Grid();
        var coverHost = new Button
        {
            Height = 168,
            Padding = new Thickness(0),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch,
            Style = (Style)Application.Current.Resources["SubtleButtonStyle"],
            Tag = item,
        };
        coverHost.Click += CoverButton_Click;

        var coverFrame = new Border
        {
            Background = (Brush)Application.Current.Resources["CardBackgroundFillColorSecondaryBrush"],
            CornerRadius = new CornerRadius(7),
        };

        if (!string.IsNullOrWhiteSpace(item.CoverUrl))
        {
            coverFrame.Child = CreateRemoteImage(item.CoverUrl, Stretch.UniformToFill);
        }

        coverHost.Content = coverFrame;
        root.Children.Add(coverHost);

        if (!string.IsNullOrWhiteSpace(item.DurationText))
        {
            var duration = new Border
            {
                Margin = new Thickness(8),
                Padding = new Thickness(5, 2, 5, 2),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(153, 0, 0, 0)),
                CornerRadius = new CornerRadius(4),
                Child = new TextBlock
                {
                    Text = item.DurationText,
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                },
            };
            root.Children.Add(duration);
        }

        var viewLaterButton = new Button
        {
            Width = 32,
            Height = 32,
            MinWidth = 0,
            Margin = new Thickness(8),
            Padding = new Thickness(0),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            IsEnabled = item.Aid > 0,
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
            Tag = item,
            Content = CreatePathIcon(CollectionsAddIconData, 18, "White"),
        };
        SetOverlayButtonResources(viewLaterButton);
        SetOverlayButtonState(viewLaterButton, OverlayButtonState.Normal);
        viewLaterButton.PointerEntered += (_, _) => SetOverlayButtonState(viewLaterButton, OverlayButtonState.Hover);
        viewLaterButton.PointerExited += (_, _) => SetOverlayButtonState(viewLaterButton, OverlayButtonState.Normal);
        viewLaterButton.PointerPressed += (_, _) => SetOverlayButtonState(viewLaterButton, OverlayButtonState.Pressed);
        viewLaterButton.PointerReleased += (_, _) => SetOverlayButtonState(viewLaterButton, OverlayButtonState.Hover);
        viewLaterButton.PointerCanceled += (_, _) => SetOverlayButtonState(viewLaterButton, OverlayButtonState.Normal);
        ToolTipService.SetToolTip(viewLaterButton, "添加到稍后再看");
        viewLaterButton.Click += AddToViewLaterButton_Click;
        root.Children.Add(viewLaterButton);

        return root;
    }

    private static FrameworkElement CreatePathIcon(string data, double size, string fill)
    {
        return (FrameworkElement)XamlReader.Load($"""
            <Viewbox
                xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                Width="{size}"
                Height="{size}"
                Stretch="Uniform">
                <Path
                    Width="24"
                    Height="24"
                    Data="{data}"
                    Fill="{fill}"
                    Stretch="Uniform" />
            </Viewbox>
            """);
    }

    private static IconElement CreateMenuPathIcon(string data, double size)
    {
        return (IconElement)XamlReader.Load($$"""
            <PathIcon
                xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                Width="{{size}}"
                Height="{{size}}"
                Foreground="{ThemeResource TextFillColorPrimaryBrush}"
                Data="{{data}}" />
            """);
    }

    private static void SetOverlayButtonResources(Button button)
    {
        button.Resources["ButtonBackground"] = CreateOverlayBrush(210, 34, 34, 34);
        button.Resources["ButtonBackgroundPointerOver"] = CreateOverlayBrush(238, 92, 92, 92);
        button.Resources["ButtonBackgroundPressed"] = CreateOverlayBrush(255, 72, 72, 72);
        button.Resources["ButtonBackgroundDisabled"] = new SolidColorBrush(Windows.UI.Color.FromArgb(96, 0, 0, 0));
        button.Resources["ButtonForeground"] = new SolidColorBrush(Microsoft.UI.Colors.White);
        button.Resources["ButtonForegroundPointerOver"] = new SolidColorBrush(Microsoft.UI.Colors.White);
        button.Resources["ButtonForegroundPressed"] = new SolidColorBrush(Microsoft.UI.Colors.White);
        button.Resources["ButtonBorderBrush"] = new SolidColorBrush(Windows.UI.Color.FromArgb(80, 255, 255, 255));
        button.Resources["ButtonBorderBrushPointerOver"] = new SolidColorBrush(Windows.UI.Color.FromArgb(135, 255, 255, 255));
        button.Resources["ButtonBorderBrushPressed"] = new SolidColorBrush(Windows.UI.Color.FromArgb(180, 255, 255, 255));
    }

    private static void SetOverlayButtonState(Button button, OverlayButtonState state)
    {
        button.Background = state switch
        {
            OverlayButtonState.Hover => CreateOverlayBrush(238, 92, 92, 92),
            OverlayButtonState.Pressed => CreateOverlayBrush(255, 72, 72, 72),
            _ => CreateOverlayBrush(210, 34, 34, 34),
        };
    }

    private static SolidColorBrush CreateOverlayBrush(byte alpha, byte red, byte green, byte blue)
    {
        return new SolidColorBrush(Windows.UI.Color.FromArgb(alpha, red, green, blue));
    }

    private static Image CreateRemoteImage(string url, Stretch stretch)
    {
        var image = new Image
        {
            Stretch = stretch,
        };

        _ = LoadRemoteImageAsync(image, url);
        return image;
    }

    private static async Task LoadRemoteImageAsync(Image image, string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return;
        }

        for (var attempt = 1; attempt <= ImageLoadMaxAttemptCount; attempt++)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, uri);
                request.Headers.TryAddWithoutValidation("User-Agent", BiliWebDataProvider.BrowserUserAgent);
                request.Headers.TryAddWithoutValidation("Referer", "https://www.bilibili.com/");
                request.Headers.TryAddWithoutValidation("Accept", "image/avif,image/webp,image/apng,image/svg+xml,image/*,*/*;q=0.8");

                using var response = await ImageHttpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var bytes = await response.Content.ReadAsByteArrayAsync();

                SetImageSource(image, uri, bytes);
                return;
            }
            catch
            {
                if (attempt == ImageLoadMaxAttemptCount)
                {
                    SetFallbackImageSource(image, uri);
                    return;
                }

                await Task.Delay(ImageLoadRetryDelay * attempt);
            }
        }
    }

    private static void SetImageSource(Image image, Uri uri, byte[] bytes)
    {
        image.DispatcherQueue.TryEnqueue(async () =>
        {
            try
            {
                using var stream = new InMemoryRandomAccessStream();
                await stream.WriteAsync(bytes.AsBuffer());
                stream.Seek(0);

                var bitmap = new BitmapImage();
                await bitmap.SetSourceAsync(stream);
                image.Source = bitmap;
            }
            catch
            {
                image.Source = new BitmapImage(uri);
            }
        });
    }

    private static void SetFallbackImageSource(Image image, Uri uri)
    {
        image.DispatcherQueue.TryEnqueue(() =>
        {
            image.Source = new BitmapImage(uri);
        });
    }

    private static HttpClient CreateImageHttpClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(12),
        };
        return client;
    }

    private enum OverlayButtonState
    {
        Normal,
        Hover,
        Pressed,
    }

    private async void CoverButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: VideoUpdateRow item }
            || string.IsNullOrWhiteSpace(item.Url)
            || !Uri.TryCreate(item.Url, UriKind.Absolute, out var uri))
        {
            ShowStatus("当前视频链接无效。", InfoBarSeverity.Warning);
            return;
        }

        await Launcher.LaunchUriAsync(uri);
    }

    private async void AddToViewLaterButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: VideoUpdateRow item })
        {
            return;
        }

        try
        {
            await _updateMonitorService.AddToViewLaterAsync(item.Aid);
            ShowStatus($"已添加到稍后再看：{item.Title}", InfoBarSeverity.Success);
        }
        catch (Exception ex)
        {
            ShowStatus($"添加稍后再看失败：{ex.Message}", InfoBarSeverity.Error);
        }
    }

    private void ShowStatus(string message, InfoBarSeverity severity)
    {
        StatusText = message;
        StatusSeverity = severity;
        IsStatusInfoOpen = !string.IsNullOrWhiteSpace(message);
    }

    private static FrameworkElement CreateStats(VideoUpdateRow item)
    {
        var stats = new Grid { ColumnSpacing = 12 };
        stats.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        stats.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        stats.Children.Add(CreateStat("", item.LikeCountText));

        var comments = CreateStat("", item.CommentCountText);
        Grid.SetColumn(comments, 1);
        stats.Children.Add(comments);

        return stats;
    }

    private static FrameworkElement CreateStat(string glyph, string text)
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
        };
        panel.Children.Add(new FontIcon
        {
            Glyph = glyph,
            FontSize = 11,
        });
        panel.Children.Add(new TextBlock
        {
            Text = text,
            FontSize = 12,
            Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
        });
        return panel;
    }

    private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (_allowClose)
        {
            return;
        }

        args.Cancel = true;
        HideRequested?.Invoke();
    }

    private void SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(storage, value))
        {
            return;
        }

        storage = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        if (propertyName == nameof(IsLoading))
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LoadingVisibility)));
        }
    }

    private void ConfigureTitleBar()
    {
        var titleBar = _appWindow.TitleBar;
        if (titleBar is null)
        {
            return;
        }

        titleBar.ExtendsContentIntoTitleBar = true;
        titleBar.PreferredHeightOption = TitleBarHeightOption.Collapsed;
        titleBar.SetDragRectangles([]);
    }

    private void AdjustWindowSizeToContent()
    {
        RootGrid.UpdateLayout();
        MainContainer.Measure(new Windows.Foundation.Size(WindowWidthDip, double.PositiveInfinity));
        var contentHeight = (int)Math.Ceiling(MainContainer.DesiredSize.Height);
        var finalHeightDip = Math.Min(Math.Max(contentHeight, WindowMinHeightDip), GetAdaptiveWindowMaxHeightDip());
        PositionWindowBottomRight(WindowWidthDip, finalHeightDip, WindowRightMarginDip, WindowBottomMarginDip);
    }

    private static int GetAdaptiveWindowMaxHeightDip()
    {
        if (!TryGetDisplayAreaAtCursor(out var displayArea) || displayArea is null)
        {
            return WindowMaxHeightDip;
        }

        var dpiScale = GetDpiScale(displayArea);
        var workAreaHeightDip = ScaleToDip(displayArea.WorkArea.Height, dpiScale);
        return (int)Math.Floor(workAreaHeightDip * WindowMaxWorkAreaHeightRatio);
    }

    private void PositionWindowBottomRight(int widthDip, int heightDip, int rightMarginDip, int bottomMarginDip)
    {
        if (!TryGetDisplayAreaAtCursor(out var displayArea) || displayArea is null)
        {
            return;
        }

        var dpiScale = GetDpiScale(displayArea);
        var work = displayArea.WorkArea;

        var width = ScaleToPhysicalPixels(widthDip, dpiScale);
        var height = ScaleToPhysicalPixels(heightDip, dpiScale);
        var marginRight = ScaleToPhysicalPixels(rightMarginDip, dpiScale);
        var marginBottom = ScaleToPhysicalPixels(bottomMarginDip, dpiScale);

        width = Math.Min(width, Math.Max(0, work.Width - marginRight));
        height = Math.Min(height, Math.Max(0, work.Height - marginBottom));

        var x = work.X + work.Width - width - marginRight;
        var y = work.Y + work.Height - height - marginBottom;
        MoveAndResizeOnDisplay(displayArea, new RectInt32(x, y, width, height));
    }

    private void MoveAndResizeOnDisplay(DisplayArea targetDisplay, RectInt32 finalRect)
    {
        var currentDisplay = DisplayArea.GetFromWindowId(_appWindow.Id, DisplayAreaFallback.Nearest);
        var needsTeleport = currentDisplay is null || currentDisplay.DisplayId.Value != targetDisplay.DisplayId.Value;

        if (needsTeleport)
        {
            var work = targetDisplay.WorkArea;
            _appWindow.MoveAndResize(new RectInt32(work.X, work.Y, 1, 1));
        }

        _appWindow.MoveAndResize(finalRect);
    }

    private static bool TryGetDisplayAreaAtCursor(out DisplayArea? displayArea)
    {
        displayArea = null;
        if (!GetCursorPos(out var cursor))
        {
            return false;
        }

        displayArea = DisplayArea.GetFromPoint(new PointInt32(cursor.X, cursor.Y), DisplayAreaFallback.Nearest);
        return displayArea is not null;
    }

    private static double GetDpiScale(DisplayArea displayArea)
    {
        var monitor = Microsoft.UI.Win32Interop.GetMonitorFromDisplayId(displayArea.DisplayId);
        return (double)GetEffectiveDpi(monitor) / DefaultDpi;
    }

    private static int ScaleToPhysicalPixels(int dip, double dpiScale)
    {
        return (int)Math.Ceiling(dip * dpiScale);
    }

    private static int ScaleToDip(int physicalPixels, double dpiScale)
    {
        return (int)Math.Floor(physicalPixels / dpiScale);
    }

    private static int GetEffectiveDpi(nint monitor)
    {
        if (monitor == 0)
        {
            return DefaultDpi;
        }

        var hr = GetDpiForMonitor(monitor, MdtEffectiveDpi, out var dpiX, out _);
        return hr >= 0 && dpiX > 0 ? (int)dpiX : DefaultDpi;
    }

    private void HideFromTaskbar()
    {
        var exStyle = GetWindowLongPtr(_hwnd, GwlExStyle);
        SetWindowLongPtr(_hwnd, GwlExStyle, (nint)((long)exStyle | WsExToolWindow));
    }

    private void DisableWindowMovingAndResizing()
    {
        var style = GetWindowLongPtr(_hwnd, GwlStyle);
        style = (nint)((long)style & ~WsThickFrame & ~WsMaximizeBox & ~WsMinimizeBox & ~WsCaption & ~WsSysMenu);
        SetWindowLongPtr(_hwnd, GwlStyle, style);

        var exStyle = GetWindowLongPtr(_hwnd, GwlExStyle);
        exStyle = (nint)((long)exStyle & ~WsExDlgModalFrame & ~WsExWindowEdge & ~WsExClientEdge & ~WsExStaticEdge);
        SetWindowLongPtr(_hwnd, GwlExStyle, exStyle);

        SetWindowPos(_hwnd, 0, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpFrameChanged);
    }

    [DllImport("user32.dll", EntryPoint = "ShowWindow")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindowNative(nint hwnd, int command);

    [DllImport("user32.dll", EntryPoint = "SetForegroundWindow")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(nint hwnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out Point point);

    [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(nint hwnd, nint insertAfter, int x, int y, int width, int height, uint flags);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern nint GetWindowLongPtr(nint hwnd, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern nint SetWindowLongPtr(nint hwnd, int index, nint newLong);

    [DllImport("shcore.dll")]
    private static extern int GetDpiForMonitor(nint monitor, uint dpiType, out uint dpiX, out uint dpiY);

    private const int SwHide = 0;
    private const int SwShow = 5;
    private const int GwlStyle = -16;
    private const int GwlExStyle = -20;
    private const long WsCaption = 0x00C00000L;
    private const long WsThickFrame = 0x00040000L;
    private const long WsMinimizeBox = 0x00020000L;
    private const long WsMaximizeBox = 0x00010000L;
    private const long WsSysMenu = 0x00080000L;
    private const long WsExToolWindow = 0x00000080L;
    private const long WsExDlgModalFrame = 0x00000001L;
    private const long WsExWindowEdge = 0x00000100L;
    private const long WsExClientEdge = 0x00000200L;
    private const long WsExStaticEdge = 0x00020000L;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpShowWindow = 0x0040;
    private const uint SwpFrameChanged = 0x0020;
    private static readonly nint HwndTopmost = new(-1);

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }

}
