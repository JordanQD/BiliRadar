using System;
using System.Collections.Concurrent;
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
    private const string DeleteIconData = "M10 2.25C9.0335 2.25 8.25 3.0335 8.25 4V4.75H5C4.58579 4.75 4.25 5.08579 4.25 5.5C4.25 5.91421 4.58579 6.25 5 6.25H19C19.4142 6.25 19.75 5.91421 19.75 5.5C19.75 5.08579 19.4142 4.75 19 4.75H15.75V4C15.75 3.0335 14.9665 2.25 14 2.25H10ZM9.75 4C9.75 3.86193 9.86193 3.75 10 3.75H14C14.1381 3.75 14.25 3.86193 14.25 4V4.75H9.75V4ZM6.75 8C6.75 7.58579 7.08579 7.25 7.5 7.25H16.5C16.9142 7.25 17.25 7.58579 17.25 8V18.5C17.25 20.2949 15.7949 21.75 14 21.75H10C8.20507 21.75 6.75 20.2949 6.75 18.5V8ZM8.25 8.75V18.5C8.25 19.4665 9.0335 20.25 10 20.25H14C14.9665 20.25 15.75 19.4665 15.75 18.5V8.75H8.25ZM10.5 10.75C10.9142 10.75 11.25 11.0858 11.25 11.5V17.5C11.25 17.9142 10.9142 18.25 10.5 18.25C10.0858 18.25 9.75 17.9142 9.75 17.5V11.5C9.75 11.0858 10.0858 10.75 10.5 10.75ZM14.25 11.5C14.25 11.0858 13.9142 10.75 13.5 10.75C13.0858 10.75 12.75 11.0858 12.75 11.5V17.5C12.75 17.9142 13.0858 18.25 13.5 18.25C13.9142 18.25 14.25 17.9142 14.25 17.5V11.5Z";
    private const string PersonAddIconData = "M10 2C12.7614 2 15 4.23858 15 7C15 9.76142 12.7614 12 10 12C7.23858 12 5 9.76142 5 7C5 4.23858 7.23858 2 10 2ZM10 3.5C8.067 3.5 6.5 5.067 6.5 7C6.5 8.933 8.067 10.5 10 10.5C11.933 10.5 13.5 8.933 13.5 7C13.5 5.067 11.933 3.5 10 3.5ZM4.25 14H11.25C11.6642 14 12 14.3358 12 14.75C12 15.1642 11.6642 15.5 11.25 15.5H4.25C3.83579 15.5 3.5 15.8358 3.5 16.25V17.16C3.5 17.82 3.79 18.44 4.29 18.86C5.54 19.94 7.44 20.5 10 20.5C10.58 20.5 11.13 20.47 11.65 20.41C12.0615 20.3626 12.4337 20.6579 12.4811 21.0694C12.5285 21.4808 12.2332 21.853 11.8218 21.9004C11.2493 21.9664 10.642 22 10 22C7.11 22 4.87 21.34 3.31 20C2.48 19.29 2 18.25 2 17.16V16.25C2 15.0074 3.00736 14 4.25 14ZM18 12C18.4142 12 18.75 12.3358 18.75 12.75V16.25H22.25C22.6642 16.25 23 16.5858 23 17C23 17.4142 22.6642 17.75 22.25 17.75H18.75V21.25C18.75 21.6642 18.4142 22 18 22C17.5858 22 17.25 21.6642 17.25 21.25V17.75H13.75C13.3358 17.75 13 17.4142 13 17C13 16.5858 13.3358 16.25 13.75 16.25H17.25V12.75C17.25 12.3358 17.5858 12 18 12Z";
    private const string PersonDeleteIconData = "M17.5 12C20.5375661 12 23 14.4624339 23 17.5C23 20.5375661 20.5375661 23 17.5 23C14.4624339 23 12 20.5375661 12 17.5C12 14.4624339 14.4624339 12 17.5 12ZM12.0222607 13.9993086C11.7255613 14.4626083 11.4860296 14.9660345 11.3136172 15.4996352L4.25354153 15.499921C3.83932796 15.499921 3.50354153 15.8357075 3.50354153 16.249921L3.50354153 17.1572408C3.50354153 17.8128951 3.78953221 18.4359296 4.28670709 18.8633654C5.5447918 19.9450082 7.44080155 20.5010712 10 20.5010712C10.598839 20.5010712 11.1614445 20.4706245 11.6881394 20.4101192C11.9370538 20.9102887 12.2508544 21.3740111 12.6170965 21.7904935C11.8149076 21.9312924 10.9419626 22.0010712 10 22.0010712C7.11050247 22.0010712 4.87168436 21.3444691 3.30881727 20.0007885C2.48019625 19.2883988 2.00354153 18.2500002 2.00354153 17.1572408L2.00354153 16.249921C2.00354153 15.0072804 3.01090084 13.999921 4.25354153 13.999921L12.0222607 13.9993086ZM15.0930472 14.9662824L15.0237993 15.0241379L14.9659438 15.0933858C14.8478223 15.2638954 14.8478223 15.4914871 14.9659438 15.6619968L15.0237993 15.7312446L16.7933527 17.5006913L15.0263884 19.2674911L14.968533 19.3367389C14.8504114 19.5072486 14.8504114 19.7348403 14.968533 19.9053499L15.0263884 19.9745978L15.0956363 20.0324533C15.2661459 20.1505748 15.4937377 20.1505748 15.6642473 20.0324533L15.7334952 19.9745978L17.5003527 18.2076913L19.2693951 19.9768405L19.338643 20.0346959C19.5091526 20.1528175 19.7367444 20.1528175 19.907254 20.0346959L19.9765019 19.9768405L20.0343574 19.9075926C20.1524789 19.737083 20.1524789 19.5094912 20.0343574 19.3389816L19.9765019 19.2697337L18.2073527 17.5006913L19.9792686 15.7312918L20.0371241 15.6620439C20.1552456 15.4915343 20.1552456 15.2639425 20.0371241 15.0934329L19.9792686 15.024185L19.9100208 14.9663296C19.7395111 14.848208 19.5119194 14.848208 19.3414098 14.9663296L19.2721619 15.024185L17.5003527 16.7936913L15.7309061 15.0241379L15.6616582 14.9662824C15.5155071 14.8650354 15.3274181 14.8505715 15.1692847 14.9228908L15.0930472 14.9662824ZM10 2.0046246C12.7614237 2.0046246 15 4.24320085 15 7.0046246C15 9.76604835 12.7614237 12.0046246 10 12.0046246C7.23857625 12.0046246 5 9.76604835 5 7.0046246C5 4.24320085 7.23857625 2.0046246 10 2.0046246ZM10 3.5046246C8.06700338 3.5046246 6.5 5.07162798 6.5 7.0046246C6.5 8.93762123 8.06700338 10.5046246 10 10.5046246C11.9329966 10.5046246 13.5 8.93762123 13.5 7.0046246C13.5 5.07162798 11.9329966 3.5046246 10 3.5046246Z";
    private const int WindowWidthDip = 420;
    private const int WindowMinHeightDip = 100;
    private const int WindowMaxHeightDip = 760;
    private const int WindowRightMarginDip = 12;
    private const int WindowBottomMarginDip = 12;
    private const double WindowMaxWorkAreaHeightRatio = 0.75;
    private const int DefaultDpi = 96;
    private const int ImageLoadMaxAttemptCount = 3;
    private static readonly TimeSpan TransientStatusDuration = TimeSpan.FromSeconds(3);
    private const uint MdtEffectiveDpi = 0;
    private static readonly TimeSpan ImageLoadRetryDelay = TimeSpan.FromMilliseconds(450);
    private static readonly HttpClient ImageHttpClient = CreateImageHttpClient();
    private static readonly ConcurrentDictionary<string, ImageSource> RoundedImageCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly CookieStore _cookieStore = new();
    private readonly UpdateMonitorService _updateMonitorService;
    private readonly NotificationService _notificationService = new();
    private readonly HashSet<string> _loadedUpdateIds = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _loadedHistoryIds = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _loadedViewLaterIds = new(StringComparer.OrdinalIgnoreCase);
    private readonly AppWindow _appWindow;
    private readonly nint _hwnd;
    private bool _allowClose;
    private bool _isLoading;
    private bool _refreshQueuedOnShow;
    private bool _isLoadingMore;
    private bool _isLoadingHistory;
    private bool _isLoadingMoreHistory;
    private bool _isLoadingViewLater;
    private bool _isLoadingMoreViewLater;
    private bool _isResettingScrollPosition;
    private bool _isShowingWindow;
    private bool _isVisible;
    private bool _hasMoreUpdates = true;
    private bool _hasMoreHistory = true;
    private bool _hasMoreViewLater = true;
    private int _unreadCount;
    private int _followingCount;
    private string _lastCheckedText = "尚未检查";
    private string _followingListText = "暂无关注数据";

    public MainWindow()
    {
        InitializeComponent();
        Title = "BiliRadar";

        Updates = [];
        HistoryItems = [];
        ViewLaterItems = [];
        Following = [];
        LiveCreators = [];
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

    public ObservableCollection<VideoUpdateRow> HistoryItems { get; }

    public ObservableCollection<VideoUpdateRow> ViewLaterItems { get; }

    public ObservableCollection<CreatorRow> Following { get; }

    public ObservableCollection<LiveCreatorRow> LiveCreators { get; }

    public ObservableCollection<StatusNotification> StatusNotifications { get; } = [];

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

    public string FollowingListText
    {
        get => _followingListText;
        private set => SetProperty(ref _followingListText, value);
    }

    public Visibility LoadingVisibility => IsLoading ? Visibility.Visible : Visibility.Collapsed;

    public Visibility RefreshProgressVisibility => IsLoading || _isLoadingHistory || _isLoadingViewLater
        ? Visibility.Visible
        : Visibility.Collapsed;

    public bool RefreshProgressIsActive => IsLoading || _isLoadingHistory || _isLoadingViewLater;

    public double RefreshProgressOpacity => RefreshProgressIsActive ? 1.0 : 0.0;

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
            ResetCurrentPageScrollPosition();
            ShowWindowNative(_hwnd, SwShow);
            SetWindowPos(_hwnd, HwndTopmost, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpNoActivate | SwpShowWindow);
            SetForegroundWindow(_hwnd);
            _isVisible = true;
            _ = RefreshSelectedPageOnShowAsync();
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
        _notificationService.Stop();
        Close();
    }

    public Task StartNotificationMonitorAsync()
    {
        return _notificationService.TryStartAsync(RefreshNotificationDataAsync);
    }

    public async Task HandleNotificationActivationAsync(NotificationService.NotificationActivationRequest request)
    {
        if (string.Equals(request.Action, NotificationService.WatchLaterAction, StringComparison.OrdinalIgnoreCase))
        {
            await AddToViewLaterFromNotificationAsync(request.Aid);
            return;
        }

        if (request.Uri is not null)
        {
            await NotificationService.LaunchUriAsync(request.Uri);
        }
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
            ClearStatusNotifications();
            if (!_cookieStore.HasCookie)
            {
                ClearSignedOutData();
                RenderVideoCards();
                AdjustWindowSizeToContent();
                return;
            }

            await RefreshFollowingListAsync();

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
            if (_cookieStore.HasCookie && Updates.Count == 0 && StatusNotifications.Count == 0)
            {
                ShowStatus("暂无视频动态。", InfoBarSeverity.Informational);
            }

            _ = _notificationService.NotifyVideoUpdatesAsync(updates, showNotifications: false);
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

    private async Task RefreshFollowingListAsync()
    {
        var following = await _updateMonitorService.GetFollowingAsync();

        Following.Clear();
        foreach (var creator in following)
        {
            Following.Add(new CreatorRow(creator));
        }

        LiveCreators.Clear();
        try
        {
            var liveCreators = await _updateMonitorService.GetFollowingLiveCreatorsAsync();
            foreach (var creator in liveCreators)
            {
                LiveCreators.Add(new LiveCreatorRow(creator));
            }

            _ = _notificationService.NotifyLiveStartsAsync(liveCreators, showNotifications: false);
        }
        catch
        {
            LiveCreators.Clear();
        }

        FollowingCount = Following.Count;
        FollowingListText = Following.Count == 0
            ? "暂无关注数据"
            : string.Join(Environment.NewLine, Following.Select(item => $"{item.Name}  UID:{item.Mid}"));
        RenderLiveCreators();
    }

    private async Task RefreshNotificationDataAsync()
    {
        if (!_cookieStore.HasCookie)
        {
            return;
        }

        if (AppSettings.NotificationTargetMode == NotificationTargetMode.CustomCreators)
        {
            await RefreshCustomNotificationDataAsync();
            return;
        }

        IReadOnlyList<BiliVideoUpdate> updates = [];
        try
        {
            updates = await _updateMonitorService.RefreshAsync();
        }
        catch
        {
        }

        await _notificationService.NotifyVideoUpdatesAsync(updates);

        IReadOnlyList<BiliLiveCreator> liveCreators = [];
        try
        {
            liveCreators = await _updateMonitorService.GetFollowingLiveCreatorsAsync();
        }
        catch
        {
        }

        await _notificationService.NotifyLiveStartsAsync(liveCreators);
    }

    private async Task RefreshCustomNotificationDataAsync()
    {
        var subscriptions = AppSettings.CustomNotificationCreators;
        var videoUpdates = new List<BiliVideoUpdate>();
        foreach (var subscription in subscriptions.Where(item => item.VideoNotificationsEnabled))
        {
            try
            {
                videoUpdates.AddRange(await _updateMonitorService.GetCreatorVideoUpdatesAsync(subscription.Mid));
            }
            catch
            {
            }
        }

        await _notificationService.NotifyVideoUpdatesAsync(videoUpdates);

        var liveCreators = new List<BiliLiveCreator>();
        foreach (var subscription in subscriptions.Where(item => item.LiveNotificationsEnabled))
        {
            try
            {
                var liveCreator = await _updateMonitorService.GetCreatorLiveAsync(subscription.Mid);
                if (liveCreator is not null)
                {
                    liveCreators.Add(liveCreator);
                }
            }
            catch
            {
            }
        }

        await _notificationService.NotifyLiveStartsAsync(liveCreators);
    }

    private async Task AddToViewLaterFromNotificationAsync(long aid)
    {
        if (aid <= 0)
        {
            return;
        }

        try
        {
            await _updateMonitorService.AddToViewLaterAsync(aid);
            NotificationService.ShowStatusNotification("已添加到稍后再看", "可以稍后在 B 站继续观看。");
        }
        catch (Exception ex)
        {
            NotificationService.ShowStatusNotification("添加到稍后再看失败", ex.Message);
        }
    }

    private void ClearSignedOutData()
    {
        Following.Clear();
        LiveCreators.Clear();
        Updates.Clear();
        _loadedUpdateIds.Clear();
        _hasMoreUpdates = false;
        FollowingCount = 0;
        UnreadCount = 0;
        FollowingListText = "暂无关注数据";
        LastCheckedText = "尚未检查";
        VideoCardsPanel.Children.Clear();
        RenderLiveCreators();
    }

    private async Task RefreshSelectedPageOnShowAsync()
    {
        if (ContentSelectorBar.SelectedItem == HistorySelectorItem)
        {
            await RefreshHistoryAsync();
            return;
        }

        if (ContentSelectorBar.SelectedItem == ViewLaterSelectorItem)
        {
            await RefreshViewLaterAsync();
            return;
        }

        await RefreshOnShowAsync();
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await RefreshAsync();
    }

    private async void OpenBrowserButton_Click(object sender, RoutedEventArgs e)
    {
        await Launcher.LaunchUriAsync(GetSelectedBrowserUri());
    }

    private Uri GetSelectedBrowserUri()
    {
        if (ContentSelectorBar.SelectedItem == HistorySelectorItem)
        {
            return new Uri("https://www.bilibili.com/history");
        }

        if (ContentSelectorBar.SelectedItem == ViewLaterSelectorItem)
        {
            return new Uri("https://www.bilibili.com/watchlater/list");
        }

        return new Uri("https://www.bilibili.com/");
    }

    private void HideButton_Click(object sender, RoutedEventArgs e)
    {
        HideRequested?.Invoke();
    }

    private async void ContentSelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        ShowSelectedPage(sender.SelectedItem);
        if (sender.SelectedItem == HistorySelectorItem)
        {
            await RefreshHistoryAsync();
        }
        else if (sender.SelectedItem == ViewLaterSelectorItem)
        {
            await RefreshViewLaterAsync();
        }
        else
        {
            await RefreshOnShowAsync();
        }
    }

    private void ShowSelectedPage(SelectorBarItem? selectedItem)
    {
        FollowingPagePanel.Visibility = selectedItem == FollowingSelectorItem ? Visibility.Visible : Visibility.Collapsed;
        HistoryPagePanel.Visibility = selectedItem == HistorySelectorItem ? Visibility.Visible : Visibility.Collapsed;
        ViewLaterPagePanel.Visibility = selectedItem == ViewLaterSelectorItem ? Visibility.Visible : Visibility.Collapsed;
        ResetCurrentPageScrollPosition();
    }

    private void ResetCurrentPageScrollPosition()
    {
        if (ContentSelectorBar.SelectedItem == FollowingSelectorItem)
        {
            _isResettingScrollPosition = true;
            VideoScrollViewer.ChangeView(null, 0, null, true);
            VideoScrollViewer.DispatcherQueue.TryEnqueue(() => _isResettingScrollPosition = false);
            return;
        }

        if (ContentSelectorBar.SelectedItem == HistorySelectorItem)
        {
            _isResettingScrollPosition = true;
            HistoryScrollViewer.ChangeView(null, 0, null, true);
            HistoryScrollViewer.DispatcherQueue.TryEnqueue(() => _isResettingScrollPosition = false);
            return;
        }

        if (ContentSelectorBar.SelectedItem == ViewLaterSelectorItem)
        {
            _isResettingScrollPosition = true;
            ViewLaterScrollViewer.ChangeView(null, 0, null, true);
            ViewLaterScrollViewer.DispatcherQueue.TryEnqueue(() => _isResettingScrollPosition = false);
        }
    }

    private async void VideoScrollViewer_ViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
    {
        if (e.IsIntermediate || _isResettingScrollPosition || ContentSelectorBar.SelectedItem != FollowingSelectorItem)
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

    private async Task RefreshHistoryAsync()
    {
        if (_isLoadingHistory)
        {
            return;
        }

        _isLoadingHistory = true;
        NotifyRefreshProgressVisibilityChanged();
        try
        {
            ClearStatusNotifications();
            HistoryEmptyPanel.Visibility = Visibility.Collapsed;

            if (!_cookieStore.HasCookie)
            {
                HistoryItems.Clear();
                _loadedHistoryIds.Clear();
                HistoryCardsPanel.Children.Clear();
                _hasMoreHistory = false;
                RenderHistoryCards();
                return;
            }

            await RefreshFollowingListAsync();

            var page = await _updateMonitorService.RefreshHistoryAsync();
            _hasMoreHistory = page.HasMore;
            var insertIndex = 0;
            foreach (var item in page.Items)
            {
                var historyChange = AddOrUpdateHistoryItem(item, insertIndex);
                if (historyChange.Kind == HistoryItemChangeKind.Inserted)
                {
                    HistoryCardsPanel.Children.Insert(insertIndex, CreateVideoCard(HistoryItems[insertIndex], showMetaTime: false, relationActionMode: GetCreatorRelationActionMode(HistoryItems[insertIndex])));
                    insertIndex++;
                }
                else if (historyChange.Kind == HistoryItemChangeKind.Updated)
                {
                    if (historyChange.OldIndex >= 0 && historyChange.OldIndex < HistoryCardsPanel.Children.Count)
                    {
                        HistoryCardsPanel.Children.RemoveAt(historyChange.OldIndex);
                    }

                    HistoryCardsPanel.Children.Insert(insertIndex, CreateVideoCard(HistoryItems[insertIndex], showMetaTime: false, relationActionMode: GetCreatorRelationActionMode(HistoryItems[insertIndex])));
                    insertIndex++;
                }
            }

            HistoryEmptyPanel.Visibility = HistoryItems.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            if (HistoryItems.Count == 0 && StatusNotifications.Count == 0)
            {
                ShowStatus("暂无历史记录。", InfoBarSeverity.Informational);
            }

            AdjustWindowSizeToContent();
        }
        catch (Exception ex)
        {
            ShowStatus($"历史记录加载失败：{ex.Message}", InfoBarSeverity.Error);
        }
        finally
        {
            _isLoadingHistory = false;
            NotifyRefreshProgressVisibilityChanged();
        }
    }

    private async void HistoryScrollViewer_ViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
    {
        if (e.IsIntermediate || _isResettingScrollPosition || ContentSelectorBar.SelectedItem != HistorySelectorItem)
        {
            return;
        }

        var distanceToBottom = HistoryScrollViewer.ScrollableHeight - HistoryScrollViewer.VerticalOffset;
        if (distanceToBottom <= 40)
        {
            await LoadMoreHistoryAsync();
        }
    }

    private async Task LoadMoreHistoryAsync()
    {
        if (_isLoadingHistory || _isLoadingMoreHistory || !_hasMoreHistory)
        {
            return;
        }

        _isLoadingMoreHistory = true;
        try
        {
            var page = await _updateMonitorService.LoadMoreHistoryAsync();
            _hasMoreHistory = page.HasMore;
            foreach (var item in page.Items)
            {
                var historyChange = AddOrUpdateHistoryItem(item);
                if (historyChange.Kind == HistoryItemChangeKind.Inserted)
                {
                    HistoryCardsPanel.Children.Add(CreateVideoCard(HistoryItems[^1], showMetaTime: false, relationActionMode: GetCreatorRelationActionMode(HistoryItems[^1])));
                }
                else if (historyChange.Kind == HistoryItemChangeKind.Updated)
                {
                    ReplaceHistoryCard(historyChange.NewIndex);
                }
            }

            HistoryEmptyPanel.Visibility = HistoryItems.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            AdjustWindowSizeToContent();
        }
        catch (Exception ex)
        {
            ShowStatus($"加载更早历史失败：{ex.Message}", InfoBarSeverity.Error);
        }
        finally
        {
            _isLoadingMoreHistory = false;
        }
    }

    private async Task RefreshViewLaterAsync()
    {
        if (_isLoadingViewLater)
        {
            return;
        }

        _isLoadingViewLater = true;
        NotifyRefreshProgressVisibilityChanged();
        try
        {
            ClearStatusNotifications();
            ViewLaterEmptyPanel.Visibility = Visibility.Collapsed;

            if (!_cookieStore.HasCookie)
            {
                ViewLaterItems.Clear();
                _loadedViewLaterIds.Clear();
                ViewLaterCardsPanel.Children.Clear();
                _hasMoreViewLater = false;
                RenderViewLaterCards();
                return;
            }

            await RefreshFollowingListAsync();

            var page = await _updateMonitorService.RefreshViewLaterAsync();
            _hasMoreViewLater = page.HasMore;
            var insertIndex = 0;
            foreach (var item in page.Items)
            {
                if (AddViewLaterIfNew(item, insertIndex))
                {
                    ViewLaterCardsPanel.Children.Insert(insertIndex, CreateVideoCard(ViewLaterItems[insertIndex], ViewLaterButtonMode.Remove, showMetaTime: false, relationActionMode: GetCreatorRelationActionMode(ViewLaterItems[insertIndex])));
                    insertIndex++;
                }
            }

            ViewLaterEmptyPanel.Visibility = ViewLaterItems.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            if (ViewLaterItems.Count == 0 && StatusNotifications.Count == 0)
            {
                ShowStatus("暂无稍后再看。", InfoBarSeverity.Informational);
            }

            AdjustWindowSizeToContent();
        }
        catch (Exception ex)
        {
            ShowStatus($"稍后再看加载失败：{ex.Message}", InfoBarSeverity.Error);
        }
        finally
        {
            _isLoadingViewLater = false;
            NotifyRefreshProgressVisibilityChanged();
        }
    }

    private async void ViewLaterScrollViewer_ViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
    {
        if (e.IsIntermediate || _isResettingScrollPosition || ContentSelectorBar.SelectedItem != ViewLaterSelectorItem)
        {
            return;
        }

        var distanceToBottom = ViewLaterScrollViewer.ScrollableHeight - ViewLaterScrollViewer.VerticalOffset;
        if (distanceToBottom <= 40)
        {
            await LoadMoreViewLaterAsync();
        }
    }

    private async Task LoadMoreViewLaterAsync()
    {
        if (_isLoadingViewLater || _isLoadingMoreViewLater || !_hasMoreViewLater)
        {
            return;
        }

        _isLoadingMoreViewLater = true;
        try
        {
            var page = await _updateMonitorService.LoadMoreViewLaterAsync();
            _hasMoreViewLater = page.HasMore;
            foreach (var item in page.Items)
            {
                if (AddViewLaterIfNew(item))
                {
                    ViewLaterCardsPanel.Children.Add(CreateVideoCard(ViewLaterItems[^1], ViewLaterButtonMode.Remove, showMetaTime: false, relationActionMode: GetCreatorRelationActionMode(ViewLaterItems[^1])));
                }
            }

            ViewLaterEmptyPanel.Visibility = ViewLaterItems.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            AdjustWindowSizeToContent();
        }
        catch (Exception ex)
        {
            ShowStatus($"加载更多稍后再看失败：{ex.Message}", InfoBarSeverity.Error);
        }
        finally
        {
            _isLoadingMoreViewLater = false;
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

    private HistoryItemChange AddOrUpdateHistoryItem(BiliVideoUpdate item, int? insertIndex = null)
    {
        var existingIndex = HistoryItems
            .Select((row, index) => new { row, index })
            .FirstOrDefault(match => string.Equals(match.row.Id, item.Id, StringComparison.OrdinalIgnoreCase))
            ?.index ?? -1;
        var row = new VideoUpdateRow(item);
        if (existingIndex >= 0)
        {
            if (HistoryItems[existingIndex].PublishedAt >= row.PublishedAt)
            {
                return HistoryItemChange.None;
            }

            HistoryItems.RemoveAt(existingIndex);
            var targetIndex = insertIndex ?? existingIndex;
            if (existingIndex < targetIndex)
            {
                targetIndex--;
            }

            targetIndex = Math.Clamp(targetIndex, 0, HistoryItems.Count);
            HistoryItems.Insert(targetIndex, row);
            return HistoryItemChange.Updated(existingIndex, targetIndex);
        }

        _loadedHistoryIds.Add(item.Id);
        if (insertIndex.HasValue)
        {
            HistoryItems.Insert(insertIndex.Value, row);
            return HistoryItemChange.Inserted(insertIndex.Value);
        }

        HistoryItems.Add(row);
        return HistoryItemChange.Inserted(HistoryItems.Count - 1);
    }

    private bool AddViewLaterIfNew(BiliVideoUpdate item, int? insertIndex = null)
    {
        if (!_loadedViewLaterIds.Add(item.Id))
        {
            return false;
        }

        var row = new VideoUpdateRow(item);
        if (insertIndex.HasValue)
        {
            ViewLaterItems.Insert(insertIndex.Value, row);
        }
        else
        {
            ViewLaterItems.Add(row);
        }

        return true;
    }

    private void ReplaceHistoryCard(int index)
    {
        if (index < 0 || index >= HistoryItems.Count)
        {
            return;
        }

        var card = CreateVideoCard(HistoryItems[index], showMetaTime: false, relationActionMode: GetCreatorRelationActionMode(HistoryItems[index]));
        if (index < HistoryCardsPanel.Children.Count)
        {
            HistoryCardsPanel.Children[index] = card;
        }
        else
        {
            HistoryCardsPanel.Children.Add(card);
        }
    }

    private void ClearCookieButton_Click(object sender, RoutedEventArgs e)
    {
        _cookieStore.Clear();
        Following.Clear();
        LiveCreators.Clear();
        Updates.Clear();
        HistoryItems.Clear();
        ViewLaterItems.Clear();
        _loadedUpdateIds.Clear();
        _loadedHistoryIds.Clear();
        _loadedViewLaterIds.Clear();
        _hasMoreUpdates = false;
        _hasMoreHistory = false;
        _hasMoreViewLater = false;
        FollowingCount = 0;
        UnreadCount = 0;
        VideoCardsPanel.Children.Clear();
        RenderLiveCreators();
        HistoryCardsPanel.Children.Clear();
        ViewLaterCardsPanel.Children.Clear();
        HistoryEmptyPanel.Visibility = Visibility.Visible;
        ViewLaterEmptyPanel.Visibility = Visibility.Visible;
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

    private void RenderLiveCreators()
    {
        LiveCreatorCardsPanel.Children.Clear();
        LiveCreatorsSection.Visibility = LiveCreators.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        foreach (var item in LiveCreators)
        {
            LiveCreatorCardsPanel.Children.Add(CreateLiveCreatorItem(item));
        }
    }

    private FrameworkElement CreateLiveCreatorItem(LiveCreatorRow item)
    {
        var button = new Button
        {
            Width = 60,
            MinWidth = 0,
            Padding = new Thickness(4, 4, 4, 2),
            HorizontalAlignment = HorizontalAlignment.Left,
            Style = (Style)Application.Current.Resources["SubtleButtonStyle"],
            Tag = item,
        };
        button.Click += LiveCreatorButton_Click;
        ToolTipService.SetToolTip(button, string.IsNullOrWhiteSpace(item.Title) ? $"打开 {item.Name} 的直播间" : item.Title);

        var panel = new StackPanel
        {
            Spacing = 4,
            HorizontalAlignment = HorizontalAlignment.Center,
        };

        var avatarHost = new Grid
        {
            Width = 44,
            Height = 44,
            HorizontalAlignment = HorizontalAlignment.Center,
        };

        avatarHost.Children.Add(CreateAvatarFrame(item.AvatarUrl, 44));
        panel.Children.Add(avatarHost);

        panel.Children.Add(new TextBlock
        {
            Text = item.Name,
            Width = 54,
            FontSize = 11,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            MaxLines = 1,
            TextTrimming = TextTrimming.CharacterEllipsis,
            TextWrapping = TextWrapping.NoWrap,
        });

        button.Content = panel;
        return button;
    }

    private void RenderHistoryCards()
    {
        HistoryCardsPanel.Children.Clear();
        HistoryEmptyPanel.Visibility = HistoryItems.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        foreach (var item in HistoryItems)
        {
            HistoryCardsPanel.Children.Add(CreateVideoCard(item, showMetaTime: false, relationActionMode: GetCreatorRelationActionMode(item)));
        }
    }

    private void RenderViewLaterCards()
    {
        ViewLaterCardsPanel.Children.Clear();
        ViewLaterEmptyPanel.Visibility = ViewLaterItems.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        foreach (var item in ViewLaterItems)
        {
            ViewLaterCardsPanel.Children.Add(CreateVideoCard(item, ViewLaterButtonMode.Remove, showMetaTime: false, relationActionMode: GetCreatorRelationActionMode(item)));
        }
    }

    private FrameworkElement CreateVideoCard(
        VideoUpdateRow item,
        ViewLaterButtonMode viewLaterButtonMode = ViewLaterButtonMode.Add,
        bool showMetaTime = true,
        CreatorRelationActionMode relationActionMode = CreatorRelationActionMode.Unfollow)
    {
        var card = new Border
        {
            Padding = new Thickness(10, 8, 10, 8),
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(82, 0, 0, 0)),
            CornerRadius = new CornerRadius(6),
            Tag = item,
        };
        card.Tapped += VideoCard_Tapped;
        card.ContextFlyout = CreateVideoCardMenuFlyout(item, relationActionMode);

        var root = new Grid
        {
            ColumnSpacing = 12,
            MinHeight = 92,
        };
        root.ColumnDefinitions.Add(new ColumnDefinition());
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var textPanel = CreateListTextPanel(item, showMetaTime, relationActionMode);
        root.Children.Add(textPanel);

        var cover = CreateCompactCover(item, viewLaterButtonMode);
        Grid.SetColumn(cover, 1);
        root.Children.Add(cover);

        card.Child = root;
        return card;
    }

    private static FrameworkElement CreateAvatar(VideoUpdateRow item)
    {
        return CreateAvatarFrame(item.AvatarUrl, 24);
    }

    private FrameworkElement CreateListTextPanel(VideoUpdateRow item, bool showMetaTime, CreatorRelationActionMode relationActionMode)
    {
        var panel = new Grid
        {
            RowSpacing = 4,
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panel.RowDefinitions.Add(new RowDefinition());

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
        Grid.SetRow(description, 1);
        panel.Children.Add(description);

        var creatorPanel = CreateCreatorMetaPanel(item, showMetaTime, relationActionMode);
        Grid.SetRow(creatorPanel, 2);
        panel.Children.Add(creatorPanel);

        return panel;
    }

    private FrameworkElement CreateCreatorMetaPanel(VideoUpdateRow item, bool showMetaTime, CreatorRelationActionMode relationActionMode)
    {
        var panel = new Grid
        {
            ColumnSpacing = 6,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 4, 0, 0),
        };
        panel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        panel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        panel.ColumnDefinitions.Add(new ColumnDefinition());

        var avatar = CreateCreatorAvatarButton(item, relationActionMode);
        panel.Children.Add(avatar);

        var creator = new TextBlock
        {
            Text = item.CreatorName,
            FontSize = 12,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            MaxWidth = showMetaTime ? 120 : 240,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            TextWrapping = TextWrapping.NoWrap,
        };
        Grid.SetColumn(creator, 1);
        panel.Children.Add(creator);

        if (showMetaTime)
        {
            var time = new TextBlock
            {
                Text = item.Tip,
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = (Brush)Application.Current.Resources["TextFillColorTertiaryBrush"],
                TextTrimming = TextTrimming.CharacterEllipsis,
                TextWrapping = TextWrapping.NoWrap,
            };
            Grid.SetColumn(time, 2);
            panel.Children.Add(time);
        }

        return panel;
    }

    private FrameworkElement CreateCompactCover(VideoUpdateRow item, ViewLaterButtonMode viewLaterButtonMode = ViewLaterButtonMode.Add)
    {
        var root = new Grid
        {
            Width = 128,
            Height = 72,
            VerticalAlignment = VerticalAlignment.Center,
        };

        var coverFrame = new Border
        {
            Width = 128,
            Height = 72,
            Background = (Brush)Application.Current.Resources["CardBackgroundFillColorSecondaryBrush"],
            CornerRadius = new CornerRadius(7),
        };

        if (!string.IsNullOrWhiteSpace(item.CoverUrl))
        {
            coverFrame.Child = CreateRemoteImage(item.CoverUrl, Stretch.UniformToFill);
        }

        root.Children.Add(coverFrame);

        if (viewLaterButtonMode != ViewLaterButtonMode.None)
        {
            var viewLaterButton = CreateViewLaterButton(item, viewLaterButtonMode);
            viewLaterButton.Width = 28;
            viewLaterButton.Height = 28;
            viewLaterButton.Margin = new Thickness(5);
            viewLaterButton.Content = CreatePathIcon(GetViewLaterButtonIconData(viewLaterButtonMode), 15, "White");
            root.Children.Add(viewLaterButton);
        }

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

    private Button CreateViewLaterButton(VideoUpdateRow item, ViewLaterButtonMode mode = ViewLaterButtonMode.Add)
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
            Content = CreatePathIcon(GetViewLaterButtonIconData(mode), 18, "White"),
        };
        SetOverlayButtonResources(viewLaterButton);
        SetOverlayButtonState(viewLaterButton, OverlayButtonState.Normal);
        viewLaterButton.PointerEntered += (_, _) => SetOverlayButtonState(viewLaterButton, OverlayButtonState.Hover);
        viewLaterButton.PointerExited += (_, _) => SetOverlayButtonState(viewLaterButton, OverlayButtonState.Normal);
        viewLaterButton.PointerPressed += (_, _) => SetOverlayButtonState(viewLaterButton, OverlayButtonState.Pressed);
        viewLaterButton.PointerReleased += (_, _) => SetOverlayButtonState(viewLaterButton, OverlayButtonState.Hover);
        viewLaterButton.PointerCanceled += (_, _) => SetOverlayButtonState(viewLaterButton, OverlayButtonState.Normal);
        ToolTipService.SetToolTip(viewLaterButton, mode == ViewLaterButtonMode.Remove ? "移出稍后再看" : "添加到稍后再看");
        if (mode == ViewLaterButtonMode.Remove)
        {
            viewLaterButton.Click += RemoveFromViewLaterButton_Click;
        }
        else
        {
            viewLaterButton.Click += AddToViewLaterButton_Click;
        }

        return viewLaterButton;
    }

    private static string GetViewLaterButtonIconData(ViewLaterButtonMode mode)
    {
        return mode == ViewLaterButtonMode.Remove ? DeleteIconData : CollectionsAddIconData;
    }

    private CreatorRelationActionMode GetCreatorRelationActionMode(VideoUpdateRow item)
    {
        return IsCreatorFollowed(item.CreatorMid)
            ? CreatorRelationActionMode.Unfollow
            : CreatorRelationActionMode.Follow;
    }

    private bool IsCreatorFollowed(long mid)
    {
        return mid > 0 && Following.Any(item => item.Mid == mid);
    }

    private Button CreateCreatorAvatarButton(VideoUpdateRow item, CreatorRelationActionMode relationActionMode)
    {
        var avatarButton = new Button
        {
            Width = 24,
            Height = 24,
            MinWidth = 0,
            Padding = new Thickness(0),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            IsEnabled = item.CreatorMid > 0,
            Style = (Style)Application.Current.Resources["SubtleButtonStyle"],
            Tag = item,
            Content = CreateAvatar(item),
        };
        ToolTipService.SetToolTip(avatarButton, "打开 UP 主主页");
        avatarButton.Click += CreatorAvatarButton_Click;
        avatarButton.ContextFlyout = CreateVideoCardMenuFlyout(item, relationActionMode);
        return avatarButton;
    }

    private MenuFlyout CreateVideoCardMenuFlyout(VideoUpdateRow item, CreatorRelationActionMode relationActionMode)
    {
        var flyout = new MenuFlyout();
        var relationItem = new MenuFlyoutItem
        {
            FontFamily = new FontFamily("Microsoft YaHei UI"),
            IsEnabled = item.CreatorMid > 0,
            DataContext = item,
        };
        ConfigureCreatorRelationMenuItem(relationItem, relationActionMode);
        relationItem.Click += CreatorRelationMenuItem_Click;
        flyout.Opening += async (_, _) => await RefreshCreatorRelationMenuItemAsync(relationItem);
        flyout.Items.Add(relationItem);
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

    private static Border CreateRemoteRoundedImage(string url, double size, double cornerRadius)
    {
        var imageBrush = new ImageBrush
        {
            Stretch = Stretch.UniformToFill,
        };
        var imageFrame = new Border
        {
            Width = size,
            Height = size,
            CornerRadius = new CornerRadius(cornerRadius),
            Background = imageBrush,
        };

        _ = LoadRemoteImageBrushAsync(imageBrush, url);
        return imageFrame;
    }

    private static Border CreateAvatarFrame(string avatarUrl, double size)
    {
        var cornerRadius = size / 2;
        var avatarFrame = new Border
        {
            Width = size,
            Height = size,
            VerticalAlignment = VerticalAlignment.Center,
            Background = (Brush)Application.Current.Resources["CardBackgroundFillColorSecondaryBrush"],
            CornerRadius = new CornerRadius(cornerRadius),
        };

        if (!string.IsNullOrWhiteSpace(avatarUrl))
        {
            avatarFrame.Child = CreateRemoteRoundedImage(avatarUrl, size, cornerRadius);
        }

        return avatarFrame;
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

    private static async Task LoadRemoteImageBrushAsync(ImageBrush imageBrush, string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return;
        }

        if (RoundedImageCache.TryGetValue(uri.AbsoluteUri, out var cachedSource))
        {
            imageBrush.DispatcherQueue.TryEnqueue(() => imageBrush.ImageSource = cachedSource);
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

                SetImageBrushSource(imageBrush, uri, bytes);
                return;
            }
            catch
            {
                if (attempt == ImageLoadMaxAttemptCount)
                {
                    SetFallbackImageBrushSource(imageBrush, uri);
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

    private static void SetImageBrushSource(ImageBrush imageBrush, Uri uri, byte[] bytes)
    {
        imageBrush.DispatcherQueue.TryEnqueue(async () =>
        {
            try
            {
                using var stream = new InMemoryRandomAccessStream();
                await stream.WriteAsync(bytes.AsBuffer());
                stream.Seek(0);

                var bitmap = new BitmapImage();
                await bitmap.SetSourceAsync(stream);
                RoundedImageCache[uri.AbsoluteUri] = bitmap;
                imageBrush.ImageSource = bitmap;
            }
            catch
            {
                SetFallbackImageBrushSource(imageBrush, uri);
            }
        });
    }

    private static void SetFallbackImageBrushSource(ImageBrush imageBrush, Uri uri)
    {
        imageBrush.DispatcherQueue.TryEnqueue(() =>
        {
            var bitmap = new BitmapImage(uri);
            RoundedImageCache[uri.AbsoluteUri] = bitmap;
            imageBrush.ImageSource = bitmap;
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

    private enum ViewLaterButtonMode
    {
        None,
        Add,
        Remove,
    }

    private enum CreatorRelationActionMode
    {
        Follow,
        Unfollow,
    }

    private enum HistoryItemChangeKind
    {
        None,
        Inserted,
        Updated,
    }

    private readonly record struct HistoryItemChange(HistoryItemChangeKind Kind, int OldIndex, int NewIndex)
    {
        public static HistoryItemChange None { get; } = new(HistoryItemChangeKind.None, -1, -1);

        public static HistoryItemChange Inserted(int index) => new(HistoryItemChangeKind.Inserted, -1, index);

        public static HistoryItemChange Updated(int oldIndex, int newIndex) => new(HistoryItemChangeKind.Updated, oldIndex, newIndex);
    }

    private async void VideoCard_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        if (IsFromInteractiveElement(e.OriginalSource as DependencyObject))
        {
            return;
        }

        if (sender is FrameworkElement { Tag: VideoUpdateRow item })
        {
            e.Handled = true;
            await LaunchVideoAsync(item);
        }
    }

    private async void CoverButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: VideoUpdateRow item })
        {
            await LaunchVideoAsync(item);
        }
    }

    private async Task LaunchVideoAsync(VideoUpdateRow item)
    {
        if (string.IsNullOrWhiteSpace(item.Url)
            || !Uri.TryCreate(item.Url, UriKind.Absolute, out var uri))
        {
            ShowStatus("当前视频链接无效。", InfoBarSeverity.Warning);
            return;
        }

        await Launcher.LaunchUriAsync(uri);
    }

    private async void CreatorAvatarButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: VideoUpdateRow item })
        {
            return;
        }

        await LaunchCreatorSpaceAsync(item);
    }

    private async void LiveCreatorButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: LiveCreatorRow item })
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(item.Url)
            || !Uri.TryCreate(item.Url, UriKind.Absolute, out var uri))
        {
            ShowStatus("当前直播间链接无效。", InfoBarSeverity.Warning);
            return;
        }

        await Launcher.LaunchUriAsync(uri);
    }

    private async Task LaunchCreatorSpaceAsync(VideoUpdateRow item)
    {
        if (item.CreatorMid <= 0)
        {
            ShowStatus("当前 UP 主主页链接无效。", InfoBarSeverity.Warning);
            return;
        }

        await Launcher.LaunchUriAsync(new Uri($"https://space.bilibili.com/{item.CreatorMid}"));
    }

    private static void ConfigureCreatorRelationMenuItem(MenuFlyoutItem item, CreatorRelationActionMode mode)
    {
        item.Tag = mode;
        item.Text = mode == CreatorRelationActionMode.Follow ? "关注" : "取消关注";
        item.Icon = CreateMenuPathIcon(mode == CreatorRelationActionMode.Follow ? PersonAddIconData : PersonDeleteIconData, 24);
    }

    private async Task RefreshCreatorRelationMenuItemAsync(MenuFlyoutItem menuItem)
    {
        if (menuItem.DataContext is not VideoUpdateRow item || item.CreatorMid <= 0)
        {
            menuItem.IsEnabled = false;
            return;
        }

        menuItem.IsEnabled = false;
        try
        {
            var isFollowed = await _updateMonitorService.IsCreatorFollowedAsync(item.CreatorMid);
            if (isFollowed)
            {
                AddFollowingCreator(item);
            }
            else
            {
                RemoveFollowingCreator(item.CreatorMid);
            }

            ConfigureCreatorRelationMenuItem(
                menuItem,
                isFollowed ? CreatorRelationActionMode.Unfollow : CreatorRelationActionMode.Follow);
        }
        catch
        {
            ConfigureCreatorRelationMenuItem(menuItem, GetCreatorRelationActionMode(item));
        }
        finally
        {
            menuItem.IsEnabled = true;
        }
    }

    private async void CreatorRelationMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuFlyoutItem { DataContext: VideoUpdateRow item, Tag: CreatorRelationActionMode mode })
        {
            return;
        }

        if (mode == CreatorRelationActionMode.Follow)
        {
            await FollowCreatorAsync(item);
        }
        else
        {
            await UnfollowCreatorAsync(item);
        }
    }

    private async void UnfollowCreatorMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: VideoUpdateRow item })
        {
            return;
        }

        try
        {
            await UnfollowCreatorAsync(item);
        }
        catch (Exception ex)
        {
            ShowStatus($"取消关注失败：{ex.Message}", InfoBarSeverity.Error);
        }
    }

    private async void FollowCreatorMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: VideoUpdateRow item })
        {
            return;
        }

        try
        {
            await FollowCreatorAsync(item);
        }
        catch (Exception ex)
        {
            ShowStatus($"关注失败：{ex.Message}", InfoBarSeverity.Error);
        }
    }

    private async Task FollowCreatorAsync(VideoUpdateRow item)
    {
        await _updateMonitorService.FollowCreatorAsync(item.CreatorMid);
        AddFollowingCreator(item);
        ShowStatus($"已关注：{item.CreatorName}", InfoBarSeverity.Success);
    }

    private async Task UnfollowCreatorAsync(VideoUpdateRow item)
    {
        await _updateMonitorService.UnfollowCreatorAsync(item.CreatorMid);
        RemoveFollowingCreator(item.CreatorMid);
        ShowStatus($"已取消关注：{item.CreatorName}", InfoBarSeverity.Success);
    }

    private void AddFollowingCreator(VideoUpdateRow item)
    {
        if (item.CreatorMid <= 0 || Following.Any(creator => creator.Mid == item.CreatorMid))
        {
            return;
        }

        Following.Add(new CreatorRow(new BiliCreator(item.CreatorMid, item.CreatorName, item.AvatarUrl)));
        FollowingCount = Following.Count;
        FollowingListText = Following.Count == 0
            ? "暂无关注数据"
            : string.Join(Environment.NewLine, Following.Select(creator => $"{creator.Name}  UID:{creator.Mid}"));
    }

    private static bool IsFromInteractiveElement(DependencyObject? source)
    {
        var current = source;
        while (current is not null)
        {
            if (current is Button or MenuFlyoutItem)
            {
                return true;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return false;
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
            ShowStatus("已添加到稍后再看。", InfoBarSeverity.Success);
        }
        catch (Exception ex)
        {
            ShowStatus($"添加稍后再看失败：{ex.Message}", InfoBarSeverity.Error);
        }
    }

    private async void RemoveFromViewLaterButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: VideoUpdateRow item })
        {
            return;
        }

        try
        {
            await _updateMonitorService.RemoveFromViewLaterAsync(item.Aid);
            RemoveViewLaterCard(item);
            ShowStatus("已移出稍后再看。", InfoBarSeverity.Success);
        }
        catch (Exception ex)
        {
            ShowStatus($"移出稍后再看失败：{ex.Message}", InfoBarSeverity.Error);
        }
    }

    private void RemoveViewLaterCard(VideoUpdateRow item)
    {
        var index = ViewLaterItems.IndexOf(item);
        if (index < 0)
        {
            index = ViewLaterItems
                .Select((row, rowIndex) => new { row, rowIndex })
                .FirstOrDefault(match => match.row.Aid == item.Aid || string.Equals(match.row.Id, item.Id, StringComparison.OrdinalIgnoreCase))
                ?.rowIndex ?? -1;
        }

        if (index >= 0)
        {
            var removed = ViewLaterItems[index];
            _loadedViewLaterIds.Remove(removed.Id);
            ViewLaterItems.RemoveAt(index);
            if (index < ViewLaterCardsPanel.Children.Count)
            {
                ViewLaterCardsPanel.Children.RemoveAt(index);
            }
        }
        else
        {
            _loadedViewLaterIds.Remove(item.Id);
        }

        ViewLaterEmptyPanel.Visibility = ViewLaterItems.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        AdjustWindowSizeToContent();
    }

    private void RemoveFollowingCreator(long mid)
    {
        var creator = Following.FirstOrDefault(item => item.Mid == mid);
        if (creator is not null)
        {
            Following.Remove(creator);
            FollowingCount = Following.Count;
            FollowingListText = Following.Count == 0
                ? "暂无关注数据"
                : string.Join(Environment.NewLine, Following.Select(item => $"{item.Name}  UID:{item.Mid}"));
        }
    }

    private void ShowStatus(string message, InfoBarSeverity severity)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            ClearStatusNotifications();
            return;
        }

        var notification = new StatusNotification(message, severity);
        StatusNotifications.Add(notification);

        if (IsTransientStatus(severity))
        {
            var timer = DispatcherQueue.CreateTimer();
            timer.Interval = TransientStatusDuration;
            timer.IsRepeating = false;
            timer.Tick += (_, _) => RemoveStatusNotification(notification);
            notification.AutoDismissTimer = timer;
            timer.Start();
        }
    }

    private void StatusNotificationInfoBar_CloseButtonClick(InfoBar sender, object args)
    {
        if (sender.DataContext is StatusNotification notification)
        {
            RemoveStatusNotification(notification);
        }
    }

    private void RemoveStatusNotification(StatusNotification notification)
    {
        notification.AutoDismissTimer?.Stop();
        notification.AutoDismissTimer = null;
        StatusNotifications.Remove(notification);
    }

    private void ClearStatusNotifications()
    {
        foreach (var notification in StatusNotifications)
        {
            notification.AutoDismissTimer?.Stop();
            notification.AutoDismissTimer = null;
        }

        StatusNotifications.Clear();
    }

    private static bool IsTransientStatus(InfoBarSeverity severity)
    {
        return severity is InfoBarSeverity.Informational or InfoBarSeverity.Success;
    }

    public sealed class StatusNotification
    {
        public StatusNotification(string message, InfoBarSeverity severity)
        {
            Message = message;
            Severity = severity;
        }

        public string Message { get; }

        public InfoBarSeverity Severity { get; }

        internal Microsoft.UI.Dispatching.DispatcherQueueTimer? AutoDismissTimer { get; set; }
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
            NotifyRefreshProgressVisibilityChanged();
        }
    }

    private void NotifyRefreshProgressVisibilityChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RefreshProgressVisibility)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RefreshProgressIsActive)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RefreshProgressOpacity)));
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
