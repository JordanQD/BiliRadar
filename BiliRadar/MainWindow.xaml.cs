using BiliRadar.Controls;
using BiliRadar.Helpers;
using BiliRadar.Models;
using BiliRadar.Services;
using CommunityToolkit.WinUI.Animations;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Graphics;
using Windows.Storage.Streams;
using Windows.System;
using WinRT.Interop;

namespace BiliRadar;

public sealed partial class MainWindow : Window, INotifyPropertyChanged
{
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
    private const int MaxCachedRoundedImages = 48;
    private static readonly TimeSpan TransientStatusDuration = TimeSpan.FromSeconds(3);
    private const uint MdtEffectiveDpi = 0;
    private static readonly TimeSpan ImageLoadRetryDelay = TimeSpan.FromMilliseconds(450);
    private static readonly HttpClient ImageHttpClient = CreateImageHttpClient();
    private static readonly ConcurrentDictionary<string, ImageSource> RoundedImageCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentQueue<string> RoundedImageCacheOrder = new();
    private readonly CookieStore _cookieStore = new();
    private readonly UpdateMonitorService _updateMonitorService;
    private readonly HashSet<string> _loadedUpdateIds = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _loadedHistoryIds = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _loadedViewLaterIds = new(StringComparer.OrdinalIgnoreCase);
    private readonly AppWindow _appWindow;
    private readonly nint _hwnd;
    private bool _allowClose;
    private bool _isDisposed;
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
    private bool _isLiveSectionExpanded;
    private int _unreadCount;
    private int _followingCount;
    private string _lastCheckedText = LocalizationHelper.GetString("LastCheckedNotYet");
    private string _followingListText = LocalizationHelper.GetString("NoFollowingData");

    public MainWindow()
    {
        InitializeComponent();
        Title = LocalizationHelper.GetString("MainWindow.Title", "BiliRadar");

        Updates = [];
        HistoryItems = [];
        ViewLaterItems = [];
        Following = [];
        LiveCreators = [];
        _updateMonitorService = new(new BiliWebDataProvider(_cookieStore));
        _hwnd = WindowNative.GetWindowHandle(this);
        _appWindow = AppWindow.GetFromWindowId(Microsoft.UI.Win32Interop.GetWindowIdFromWindow(_hwnd));
        _appWindow.Title = LocalizationHelper.GetString("MainWindow.Title", "BiliRadar");
        WindowIconHelper.ApplyTo(_appWindow, _hwnd);
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
        ApplyLiveSectionDisplayMode(AppSettings.LiveSectionDisplayMode);

        RootGrid.ActualThemeChanged += OnRootGridActualThemeChanged;
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

    public bool RefreshProgressIsActive => IsLoading || _isLoadingHistory || _isLoadingViewLater
        || _isLoadingMoreHistory || _isLoadingMoreViewLater || _isLoadingMore;

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
            SystemBackdrop ??= new DesktopAcrylicBackdrop();
            AdjustWindowSizeToContent();
            Activate();
            ResetCurrentPageScrollPosition();
            ApplyLiveSectionDisplayMode(AppSettings.LiveSectionDisplayMode);
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
        SystemBackdrop = null;
        _isVisible = false;
    }

    public void CloseForExit()
    {
        DisposeAndClose();
    }

    public void CloseForRecycle()
    {
        DisposeAndClose();
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
                ShowStatus(LocalizationHelper.Format("VideoLoadFailed", ex.Message), InfoBarSeverity.Error);
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
                else
                {
                    var existing = Updates.FirstOrDefault(r => string.Equals(r.Id, update.Id, StringComparison.OrdinalIgnoreCase));
                    if (existing != null)
                    {
                        existing.Tip = update.Tip;
                    }
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
                    VideoCardsPanel.Children.Insert(0, CreateVideoCardControl(newRows[index]));
                }

                SyncCardTimeTexts();
            }

            RemoveStaleVideoCards(updates);

            _hasMoreUpdates = Updates.Count > 0;
            UnreadCount = Updates.Count(item => item.IsUnread);
            LastCheckedText = _updateMonitorService.LastCheckedAt.ToString("HH:mm:ss");
            if (_cookieStore.HasCookie && Updates.Count == 0 && StatusNotifications.Count == 0)
            {
                ShowStatus(LocalizationHelper.GetString("NoVideoUpdates"), InfoBarSeverity.Informational);
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

    private void RemoveStaleVideoCards(IReadOnlyList<BiliVideoUpdate> serverItems)
    {
        RemoveStaleItems(serverItems, Updates, VideoCardsPanel, _loadedUpdateIds);
    }

    private static void RemoveStaleItems(
        IReadOnlyList<BiliVideoUpdate> serverItems,
        ObservableCollection<VideoUpdateRow> collection,
        Panel panel,
        HashSet<string> loadedIds)
    {
        if (serverItems.Count == 0) return;
        var serverIds = new HashSet<string>(serverItems.Select(u => u.Id), StringComparer.OrdinalIgnoreCase);
        var oldestDate = serverItems.Min(u => u.PublishedAt);

        for (var i = collection.Count - 1; i >= 0; i--)
        {
            var item = collection[i];
            if (loadedIds.Contains(item.Id) && !serverIds.Contains(item.Id) && item.PublishedAt >= oldestDate)
            {
                if (i < panel.Children.Count)
                    panel.Children.RemoveAt(i);
                collection.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Diff-based panel refresh: removes items deleted from server, inserts new items at correct positions,
    /// and moves existing items to their server-defined order. Triggers transitions for actual changes only.
    /// Returns the number of items removed.
    /// </summary>
    private static int DiffRefreshPanel(
        IReadOnlyList<BiliVideoUpdate> serverItems,
        ObservableCollection<VideoUpdateRow> collection,
        Panel panel,
        HashSet<string> loadedIds,
        Func<VideoUpdateRow, UIElement> createCard)
    {
        if (serverItems.Count == 0) return 0;
        var serverIds = new HashSet<string>(serverItems.Select(i => i.Id), StringComparer.OrdinalIgnoreCase);
        var oldestDate = serverItems.Min(i => i.PublishedAt);
        int removedCount = 0;

        // 1. Remove stale items (deleted from server, within page-1 date range)
        for (int i = collection.Count - 1; i >= 0; i--)
        {
            if (!serverIds.Contains(collection[i].Id) && collection[i].PublishedAt >= oldestDate)
            {
                panel.Children.RemoveAt(i);
                collection.RemoveAt(i);
                removedCount++;
            }
        }

        // 2. Insert new items and move existing ones to correct server order
        int serverIdx = 0;
        foreach (var serverItem in serverItems)
        {
            var existingIdx = IndexById(collection, serverItem.Id);
            if (existingIdx >= 0)
            {
                // Move to correct position if needed
                if (existingIdx != serverIdx)
                {
                    collection.Move(existingIdx, serverIdx);
                    var card = panel.Children[existingIdx];
                    panel.Children.RemoveAt(existingIdx);
                    panel.Children.Insert(serverIdx, card);
                }
            }
            else
            {
                loadedIds.Add(serverItem.Id);
                var row = new VideoUpdateRow(serverItem);
                collection.Insert(serverIdx, row);
                panel.Children.Insert(serverIdx, createCard(row));
            }
            serverIdx++;
        }

        return removedCount;
    }

    private static int IndexById(IList<VideoUpdateRow> list, string id)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (string.Equals(list[i].Id, id, StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
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

        }
        catch
        {
            LiveCreators.Clear();
        }

        FollowingCount = Following.Count;
        FollowingListText = Following.Count == 0
            ? LocalizationHelper.GetString("NoFollowingData")
            : string.Join(Environment.NewLine, Following.Select(item => $"{item.Name}  UID:{item.Mid}"));
        RenderLiveCreators();
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
        FollowingListText = LocalizationHelper.GetString("NoFollowingData");
        LastCheckedText = LocalizationHelper.GetString("LastCheckedNotYet");
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
        await LaunchSelectedBrowserUriAsync();
    }

    public async Task LaunchSelectedBrowserUriAsync()
    {
        await Launcher.LaunchUriAsync(GetSelectedBrowserUri());
    }

    public async Task LaunchCustomWebPageUriAsync()
    {
        await Launcher.LaunchUriAsync(GetCustomWebPageUri());
    }

    public void ShowDefaultOpenPage()
    {
        var selectedItem = GetDefaultOpenPageSelectorItem();
        if (ContentSelectorBar.SelectedItem == selectedItem)
        {
            ShowSelectedPage(selectedItem);
            return;
        }

        ContentSelectorBar.SelectedItem = selectedItem;
    }

    private Uri GetCustomWebPageUri()
    {
        return Uri.TryCreate(AppSettings.CustomLaunchWebPageUrl, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                ? uri
                : new Uri("https://www.bilibili.com/");
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

    private SelectorBarItem GetDefaultOpenPageSelectorItem()
    {
        return AppSettings.DefaultOpenPage switch
        {
            DefaultOpenPage.History => HistorySelectorItem,
            DefaultOpenPage.ViewLater => ViewLaterSelectorItem,
            _ => FollowingSelectorItem,
        };
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

        if (selectedItem == FollowingSelectorItem)
        {
            ApplyLiveSectionDisplayMode(AppSettings.LiveSectionDisplayMode);
        }
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
        NotifyRefreshProgressVisibilityChanged();
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
                    VideoCardsPanel.Children.Add(CreateVideoCardControl(Updates[^1]));
                }
            }
        }
        catch (Exception ex)
        {
            ShowStatus(LocalizationHelper.Format("LoadEarlierFailed", ex.Message), InfoBarSeverity.Error);
        }
        finally
        {
            _isLoadingMore = false;
            NotifyRefreshProgressVisibilityChanged();
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

            int removed = DiffRefreshPanel(
                page.Items, HistoryItems, HistoryCardsPanel, _loadedHistoryIds,
                row => CreateVideoCardControl(row, showMetaTime: false, relationActionMode: GetCreatorRelationActionMode(row)));

            if (removed > page.Items.Count / 2 && _hasMoreHistory)
                DispatcherQueue.TryEnqueue(() => _ = LoadMoreHistoryAsync());

            HistoryEmptyPanel.Visibility = HistoryItems.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            if (HistoryItems.Count == 0 && StatusNotifications.Count == 0)
            {
                ShowStatus(LocalizationHelper.GetString("NoHistory"), InfoBarSeverity.Informational);
            }

            AdjustWindowSizeToContent();
        }
        catch (Exception ex)
        {
            ShowStatus(LocalizationHelper.Format("HistoryLoadFailed", ex.Message), InfoBarSeverity.Error);
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
        if (_isLoadingMoreHistory || !_hasMoreHistory)
        {
            return;
        }

        _isLoadingMoreHistory = true;
        NotifyRefreshProgressVisibilityChanged();
        try
        {
            var page = await _updateMonitorService.LoadMoreHistoryAsync();
            _hasMoreHistory = page.HasMore;
            foreach (var item in page.Items)
            {
                var historyChange = AddOrUpdateHistoryItem(item);
                if (historyChange.Kind == HistoryItemChangeKind.Inserted)
                {
                    HistoryCardsPanel.Children.Add(CreateVideoCardControl(HistoryItems[^1], showMetaTime: false, relationActionMode: GetCreatorRelationActionMode(HistoryItems[^1])));
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
            ShowStatus(LocalizationHelper.Format("LoadEarlierHistoryFailed", ex.Message), InfoBarSeverity.Error);
        }
        finally
        {
            _isLoadingMoreHistory = false;
            NotifyRefreshProgressVisibilityChanged();
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

            int removed = DiffRefreshPanel(
                page.Items, ViewLaterItems, ViewLaterCardsPanel, _loadedViewLaterIds,
                row => CreateVideoCardControl(row, ViewLaterButtonMode.Remove, showMetaTime: false, relationActionMode: GetCreatorRelationActionMode(row)));

            if (removed > page.Items.Count / 2 && _hasMoreViewLater)
                DispatcherQueue.TryEnqueue(() => _ = LoadMoreViewLaterAsync());

            ViewLaterEmptyPanel.Visibility = ViewLaterItems.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            if (ViewLaterItems.Count == 0 && StatusNotifications.Count == 0)
            {
                ShowStatus(LocalizationHelper.GetString("NoViewLater"), InfoBarSeverity.Informational);
            }

            AdjustWindowSizeToContent();
        }
        catch (Exception ex)
        {
            ShowStatus(LocalizationHelper.Format("ViewLaterLoadFailed", ex.Message), InfoBarSeverity.Error);
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
        if (_isLoadingMoreViewLater || !_hasMoreViewLater)
        {
            return;
        }

        _isLoadingMoreViewLater = true;
        NotifyRefreshProgressVisibilityChanged();
        try
        {
            var page = await _updateMonitorService.LoadMoreViewLaterAsync();
            _hasMoreViewLater = page.HasMore;
            foreach (var item in page.Items)
            {
                if (AddViewLaterIfNew(item))
                {
                    ViewLaterCardsPanel.Children.Add(CreateVideoCardControl(ViewLaterItems[^1], ViewLaterButtonMode.Remove, showMetaTime: false, relationActionMode: GetCreatorRelationActionMode(ViewLaterItems[^1])));
                }
            }

            ViewLaterEmptyPanel.Visibility = ViewLaterItems.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            AdjustWindowSizeToContent();
        }
        catch (Exception ex)
        {
            ShowStatus(LocalizationHelper.Format("LoadMoreViewLaterFailed", ex.Message), InfoBarSeverity.Error);
        }
        finally
        {
            _isLoadingMoreViewLater = false;
            NotifyRefreshProgressVisibilityChanged();
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

        var card = CreateVideoCardControl(HistoryItems[index], showMetaTime: false, relationActionMode: GetCreatorRelationActionMode(HistoryItems[index]));
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
        FollowingListText = LocalizationHelper.GetString("NoFollowingData");
        LastCheckedText = LocalizationHelper.GetString("LastCheckedNotYet");
        ShowStatus(LocalizationHelper.GetString("CookieCleared"), InfoBarSeverity.Informational);
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
                Text = LocalizationHelper.GetString("NoVideoUpdates"),
                FontSize = 13,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            });
            return;
        }

        foreach (var item in Updates)
        {
            VideoCardsPanel.Children.Add(CreateVideoCardControl(item));
        }
    }
    private VideoCard CreateVideoCardControl(
        VideoUpdateRow item,
        ViewLaterButtonMode viewLaterButtonMode = ViewLaterButtonMode.Add,
        bool showMetaTime = true,
        CreatorRelationActionMode relationActionMode = CreatorRelationActionMode.Unfollow)
    {
        var card = new VideoCard
        {
            Item = item,
            ViewLaterButtonMode = viewLaterButtonMode,
            ShowMetaTime = showMetaTime,
            IsCreatorFollowedAsync = mid => _updateMonitorService.IsCreatorFollowedAsync(mid),
        };
        card.CardMenuFlyout = CreateVideoCardMenuFlyout(item, relationActionMode);
        card.CoverTapped += (_, row) => _ = LaunchVideoAsync(row);
        card.CreatorAvatarClicked += (_, row) => _ = LaunchCreatorSpaceAsync(row);
        card.ViewLaterClicked += viewLaterButtonMode == ViewLaterButtonMode.Remove
            ? (_, row) => HandleViewLaterCardClick(row)
            : (_, row) => HandleAddToViewLaterClick(row);
        return card;
    }

    private async void HandleViewLaterCardClick(VideoUpdateRow item)
    {
        try
        {
            await _updateMonitorService.RemoveFromViewLaterAsync(item.Aid);
            RemoveViewLaterCard(item);
            ShowStatus(LocalizationHelper.GetString("RemovedFromViewLater"), InfoBarSeverity.Success);
        }
        catch (Exception ex)
        {
            ShowStatus(LocalizationHelper.Format("RemoveFromViewLaterFailed", ex.Message), InfoBarSeverity.Error);
        }
    }

    private async void HandleAddToViewLaterClick(VideoUpdateRow item)
    {
        try
        {
            await _updateMonitorService.AddToViewLaterAsync(item.Aid);
            ShowStatus(LocalizationHelper.GetString("AddedToViewLaterToast"), InfoBarSeverity.Success);
        }
        catch (Exception ex)
        {
            ShowStatus(LocalizationHelper.Format("AddToViewLaterFailedToast", ex.Message), InfoBarSeverity.Error);
        }
    }

    private void RenderLiveCreators()
    {
        LiveCreatorCardsPanel.Children.Clear();
        foreach (var item in LiveCreators)
        {
            LiveCreatorCardsPanel.Children.Add(CreateLiveCreatorItem(item));
        }

        var hasLiveCreators = LiveCreators.Count > 0;
        if (AppSettings.LiveSectionDisplayMode == LiveSectionDisplayMode.Hidden)
        {
            LiveCreatorsSection.Visibility = Visibility.Collapsed;
            LatestVideosHeader.Visibility = Visibility.Collapsed;
        }
        else
        {
            LiveCreatorsSection.Visibility = hasLiveCreators ? Visibility.Visible : Visibility.Collapsed;
            LatestVideosHeader.Visibility = Visibility.Visible;
            if (hasLiveCreators)
            {
                ApplyLiveSectionExpandedState(_isLiveSectionExpanded);
            }
        }
    }

    private void ApplyLiveSectionDisplayMode(LiveSectionDisplayMode mode)
    {
        if (mode == LiveSectionDisplayMode.Hidden)
        {
            _isLiveSectionExpanded = false;
            LiveCreatorsSection.Visibility = Visibility.Collapsed;
            LatestVideosHeader.Visibility = Visibility.Collapsed;
        }
        else
        {
            _isLiveSectionExpanded = mode == LiveSectionDisplayMode.Expanded;
            LatestVideosHeader.Visibility = Visibility.Visible;
            if (LiveCreators.Count > 0)
            {
                LiveCreatorsSection.Visibility = Visibility.Visible;
                ApplyLiveSectionExpandedState(_isLiveSectionExpanded);
            }
        }
    }

    private async void LiveSectionToggleButton_Click(object sender, RoutedEventArgs e)
    {
        _isLiveSectionExpanded = !_isLiveSectionExpanded;
        await ApplyLiveSectionExpandedStateAsync(_isLiveSectionExpanded);
    }

    private void ApplyLiveSectionExpandedState(bool expanded)
    {
        _ = ApplyLiveSectionExpandedStateAsync(expanded);
    }

    private async Task ApplyLiveSectionExpandedStateAsync(bool expanded)
    {
        if (expanded)
        {
            LiveCardsScrollViewer.Visibility = Visibility.Visible;
            await AnimationBuilder.Create()
                .Opacity(from: 0, to: 1, duration: TimeSpan.FromMilliseconds(200))
                .Translation(Axis.Y, from: -10, to: 0, duration: TimeSpan.FromMilliseconds(200))
                .StartAsync(LiveCardsScrollViewer);
        }
        else
        {
            await AnimationBuilder.Create()
                .Opacity(from: 1, to: 0, duration: TimeSpan.FromMilliseconds(150))
                .Translation(Axis.Y, from: 0, to: -10, duration: TimeSpan.FromMilliseconds(150))
                .StartAsync(LiveCardsScrollViewer);
            LiveCardsScrollViewer.Visibility = Visibility.Collapsed;
        }

        LiveSectionChevronCollapsed.Visibility = expanded ? Visibility.Collapsed : Visibility.Visible;
        LiveSectionChevronExpanded.Visibility = expanded ? Visibility.Visible : Visibility.Collapsed;
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
        var liveNotifyFlyout = new MenuFlyout();
        var liveNotifyItem = new MenuFlyoutItem
        {
            FontFamily = new FontFamily("Microsoft YaHei UI"),
            DataContext = item,
            Visibility = Visibility.Collapsed,
        };
        ConfigureLiveNotificationMenuItem(liveNotifyItem);
        liveNotifyItem.Click += LiveNotificationMenuItem_Click;
        liveNotifyFlyout.Items.Add(liveNotifyItem);
        liveNotifyFlyout.Opening += (_, _) => ConfigureLiveNotificationMenuItem(liveNotifyItem);
        button.ContextFlyout = liveNotifyFlyout;
        ToolTipService.SetToolTip(button, string.IsNullOrWhiteSpace(item.Title) ? LocalizationHelper.Format("OpenLiveRoomTooltip", item.Name) : item.Title);

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
            HistoryCardsPanel.Children.Add(CreateVideoCardControl(item, showMetaTime: false, relationActionMode: GetCreatorRelationActionMode(item)));
        }
    }

    private void RenderViewLaterCards()
    {
        ViewLaterCardsPanel.Children.Clear();
        ViewLaterEmptyPanel.Visibility = ViewLaterItems.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        foreach (var item in ViewLaterItems)
        {
            ViewLaterCardsPanel.Children.Add(CreateVideoCardControl(item, ViewLaterButtonMode.Remove, showMetaTime: false, relationActionMode: GetCreatorRelationActionMode(item)));
        }
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

        MenuFlyoutItem? notifyItem = null;
        if (item.CreatorMid > 0)
        {
            notifyItem = new MenuFlyoutItem
            {
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                DataContext = item,
                Visibility = Visibility.Collapsed,
            };
            ConfigureNotificationListMenuItem(notifyItem);
            notifyItem.Click += NotificationListMenuItem_Click;
        }

        flyout.Opening += async (_, _) =>
        {
            if (notifyItem is not null)
            {
                try
                {
                    var followed = await _updateMonitorService.IsCreatorFollowedAsync(item.CreatorMid);
                    if (!followed)
                    {
                        notifyItem.Visibility = Visibility.Collapsed;
                        notifyItem.IsEnabled = false;
                        await RefreshCreatorRelationMenuItemAsync(relationItem);
                        return;
                    }
                }
                catch
                {
                    notifyItem.Visibility = Visibility.Collapsed;
                    notifyItem.IsEnabled = false;
                }

                RefreshNotificationListMenuItem(notifyItem);
            }

            await RefreshCreatorRelationMenuItemAsync(relationItem);
        };
        flyout.Items.Add(relationItem);
        if (notifyItem is not null)
            flyout.Items.Add(notifyItem);
        return flyout;
    }

    private static void ConfigureNotificationListMenuItem(MenuFlyoutItem item)
    {
        if (item.DataContext is not VideoUpdateRow row || row.CreatorMid <= 0)
        {
            item.Visibility = Visibility.Collapsed;
            return;
        }

        if (AppSettings.NotificationTargetMode != NotificationTargetMode.CustomCreators)
        {
            item.Visibility = Visibility.Collapsed;
            return;
        }

        item.Visibility = Visibility.Visible;
        var isInList = AppSettings.CustomNotificationCreators.Any(c => c.Mid == row.CreatorMid);
        item.Text = isInList
            ? LocalizationHelper.GetString("RemoveFromNotificationListMenuItem")
            : LocalizationHelper.GetString("AddToNotificationListMenuItem");
        item.Icon = isInList
            ? new FontIcon { Glyph = "", FontSize = 16 }
            : new FontIcon { Glyph = "", FontSize = 16 };
        item.Tag = isInList ? "remove" : "add";
    }

    private static void RefreshNotificationListMenuItem(MenuFlyoutItem item)
    {
        ConfigureNotificationListMenuItem(item);
    }

    private void NotificationListMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuFlyoutItem { DataContext: VideoUpdateRow item, Tag: string action })
            return;

        if (action == "remove")
            RemoveCreatorFromNotificationList(item.CreatorMid, item.CreatorName);
        else
            AddCreatorToNotificationList(item.CreatorMid, item.CreatorName, item.AvatarUrl);
    }

    private void AddCreatorToNotificationList(long mid, string name, string avatarUrl)
    {
        var subscriptions = AppSettings.CustomNotificationCreators.ToList();
        if (subscriptions.Any(c => c.Mid == mid))
            return;

        subscriptions.Add(new NotificationCreatorSubscription
        {
            Mid = mid,
            Name = name,
            AvatarUrl = avatarUrl,
            VideoNotificationsEnabled = true,
            LiveNotificationsEnabled = true,
        });
        AppSettings.CustomNotificationCreators = subscriptions;
        ShowStatus(LocalizationHelper.Format("AddedToNotificationList", name), InfoBarSeverity.Success);
    }

    private void RemoveCreatorFromNotificationList(long mid, string name)
    {
        var subscriptions = AppSettings.CustomNotificationCreators.ToList();
        subscriptions.RemoveAll(c => c.Mid == mid);
        AppSettings.CustomNotificationCreators = subscriptions;
        ShowStatus(LocalizationHelper.Format("RemovedFromNotificationList", name), InfoBarSeverity.Success);
    }

    private static void ConfigureLiveNotificationMenuItem(MenuFlyoutItem item)
    {
        if (item.DataContext is not LiveCreatorRow row || row.Mid <= 0)
        {
            item.Visibility = Visibility.Collapsed;
            return;
        }

        if (AppSettings.NotificationTargetMode != NotificationTargetMode.CustomCreators)
        {
            item.Visibility = Visibility.Collapsed;
            return;
        }

        item.Visibility = Visibility.Visible;
        var isInList = AppSettings.CustomNotificationCreators.Any(c => c.Mid == row.Mid);
        item.Text = isInList
            ? LocalizationHelper.GetString("RemoveFromNotificationListMenuItem")
            : LocalizationHelper.GetString("AddToNotificationListMenuItem");
        item.Icon = isInList
            ? new FontIcon { Glyph = "", FontSize = 16 }
            : new FontIcon { Glyph = "", FontSize = 16 };
        item.Tag = isInList ? "remove" : "add";
    }

    private void LiveNotificationMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuFlyoutItem { DataContext: LiveCreatorRow item, Tag: string action })
            return;

        if (action == "remove")
            RemoveCreatorFromNotificationList(item.Mid, item.Name);
        else
            AddCreatorToNotificationList(item.Mid, item.Name, item.AvatarUrl);
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
                CacheRoundedImage(uri.AbsoluteUri, bitmap);
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
            CacheRoundedImage(uri.AbsoluteUri, bitmap);
            imageBrush.ImageSource = bitmap;
        });
    }

    private static void CacheRoundedImage(string url, ImageSource image)
    {
        if (!RoundedImageCache.TryAdd(url, image))
        {
            RoundedImageCache[url] = image;
            return;
        }

        RoundedImageCacheOrder.Enqueue(url);
        while (RoundedImageCache.Count > MaxCachedRoundedImages
            && RoundedImageCacheOrder.TryDequeue(out var oldestUrl))
        {
            RoundedImageCache.TryRemove(oldestUrl, out _);
        }
    }

    private void DisposeAndClose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _allowClose = true;
        Activated -= MainWindow_Activated;
        _appWindow.Closing -= AppWindow_Closing;
        RootGrid.ActualThemeChanged -= OnRootGridActualThemeChanged;
        StopStatusNotificationTimers();
        DisposeVideoCards(VideoCardsPanel);
        DisposeVideoCards(HistoryCardsPanel);
        DisposeVideoCards(ViewLaterCardsPanel);
        LiveCreatorCardsPanel.Children.Clear();
        Updates.Clear();
        HistoryItems.Clear();
        ViewLaterItems.Clear();
        Following.Clear();
        LiveCreators.Clear();
        RoundedImageCache.Clear();
        while (RoundedImageCacheOrder.TryDequeue(out _))
        {
        }

        VideoCard.ClearImageCache();
        _updateMonitorService.Dispose();
        Content = null;
        Close();
    }

    private static void DisposeVideoCards(Panel panel)
    {
        foreach (var card in panel.Children.OfType<VideoCard>())
        {
            card.Dispose();
        }

        panel.Children.Clear();
    }

    private void StopStatusNotificationTimers()
    {
        foreach (var notification in StatusNotifications)
        {
            notification.AutoDismissTimer?.Stop();
            notification.AutoDismissTimer = null;
        }

        StatusNotifications.Clear();
    }

    private static HttpClient CreateImageHttpClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(12),
        };
        return client;
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



    private async Task LaunchVideoAsync(VideoUpdateRow item)
    {
        if (string.IsNullOrWhiteSpace(item.Url)
            || !Uri.TryCreate(item.Url, UriKind.Absolute, out var uri))
        {
            ShowStatus(LocalizationHelper.GetString("InvalidVideoLink"), InfoBarSeverity.Warning);
            return;
        }

        await Launcher.LaunchUriAsync(uri);
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
            ShowStatus(LocalizationHelper.GetString("InvalidLiveRoomLink"), InfoBarSeverity.Warning);
            return;
        }

        await Launcher.LaunchUriAsync(uri);
    }

    private async Task LaunchCreatorSpaceAsync(VideoUpdateRow item)
    {
        if (item.CreatorMid <= 0)
        {
            ShowStatus(LocalizationHelper.GetString("InvalidCreatorLink"), InfoBarSeverity.Warning);
            return;
        }

        await Launcher.LaunchUriAsync(new Uri($"https://space.bilibili.com/{item.CreatorMid}"));
    }

    private static void ConfigureCreatorRelationMenuItem(MenuFlyoutItem item, CreatorRelationActionMode mode)
    {
        item.Tag = mode;
        item.Text = mode == CreatorRelationActionMode.Follow
            ? LocalizationHelper.GetString("FollowCreatorMenuItem")
            : LocalizationHelper.GetString("UnfollowCreatorMenuItem");
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
            ShowStatus(LocalizationHelper.Format("UnfollowFailed", ex.Message), InfoBarSeverity.Error);
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
            ShowStatus(LocalizationHelper.Format("FollowFailed", ex.Message), InfoBarSeverity.Error);
        }
    }

    private async Task FollowCreatorAsync(VideoUpdateRow item)
    {
        await _updateMonitorService.FollowCreatorAsync(item.CreatorMid);
        AddFollowingCreator(item);
        ShowStatus(LocalizationHelper.Format("Followed", item.CreatorName), InfoBarSeverity.Success);
    }

    private async Task UnfollowCreatorAsync(VideoUpdateRow item)
    {
        await _updateMonitorService.UnfollowCreatorAsync(item.CreatorMid);
        RemoveFollowingCreator(item.CreatorMid);
        ShowStatus(LocalizationHelper.Format("Unfollowed", item.CreatorName), InfoBarSeverity.Success);
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
            ? LocalizationHelper.GetString("NoFollowingData")
            : string.Join(Environment.NewLine, Following.Select(creator => $"{creator.Name}  UID:{creator.Mid}"));
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
                ? LocalizationHelper.GetString("NoFollowingData")
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

    private void StatusNotificationInfoBar_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            AnimationBuilder.Create()
                .Opacity(to: 1, from: 0, duration: TimeSpan.FromMilliseconds(250), easingType: EasingType.Cubic, easingMode: EasingMode.EaseOut)
                .Translation(Axis.Y, to: 0, from: 20, duration: TimeSpan.FromMilliseconds(250), easingType: EasingType.Cubic, easingMode: EasingMode.EaseOut)
                .Start(element);
        }
    }

    private void StatusNotificationInfoBar_CloseButtonClick(InfoBar sender, object args)
    {
        if (sender.DataContext is StatusNotification notification)
        {
            RemoveStatusNotification(notification);
        }
    }

    private async void RemoveStatusNotification(StatusNotification notification)
    {
        if (notification.IsRemoving)
        {
            return;
        }

        notification.AutoDismissTimer?.Stop();
        notification.AutoDismissTimer = null;

        await AnimateOutAsync(notification);
        StatusNotifications.Remove(notification);
    }

    private void ClearStatusNotifications()
    {
        foreach (var notification in StatusNotifications)
        {
            notification.AutoDismissTimer?.Stop();
            notification.AutoDismissTimer = null;
        }

        _ = ClearStatusNotificationsWithAnimationAsync();
    }

    private async Task ClearStatusNotificationsWithAnimationAsync()
    {
        var items = StatusNotifications.ToList();
        foreach (var notification in items)
        {
            await AnimateOutAsync(notification);
            StatusNotifications.Remove(notification);
        }
    }

    private Task AnimateOutAsync(StatusNotification notification)
    {
        if (notification.IsRemoving)
        {
            return Task.CompletedTask;
        }

        notification.IsRemoving = true;

        var container = StatusItemsControl.ContainerFromItem(notification) as ContentPresenter;
        if (container is null
            || VisualTreeHelper.GetChildrenCount(container) == 0
            || VisualTreeHelper.GetChild(container, 0) is not InfoBar infoBar)
        {
            return Task.CompletedTask;
        }

        var tcs = new TaskCompletionSource<bool>();
        AnimationBuilder.Create()
            .Opacity(to: 0, from: 1, duration: TimeSpan.FromMilliseconds(150), easingType: EasingType.Cubic, easingMode: EasingMode.EaseIn)
            .Translation(Axis.Y, to: 20, from: 0, duration: TimeSpan.FromMilliseconds(150), easingType: EasingType.Cubic, easingMode: EasingMode.EaseIn)
            .Start(infoBar, () =>
            {
                infoBar.IsOpen = false;
                tcs.TrySetResult(true);
            });

        return tcs.Task;
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

        internal bool IsRemoving { get; set; }
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

    private void SyncCardTimeTexts()
    {
        var count = Math.Min(Updates.Count, VideoCardsPanel.Children.Count);
        for (var i = 0; i < count; i++)
        {
            if (VideoCardsPanel.Children[i] is VideoCard card)
            {
                card.TryUpdateTimeText(Updates[i].Tip);
            }
        }
    }


    // ── Theme-aware brush lookup ──

    private void OnRootGridActualThemeChanged(FrameworkElement sender, object args)
    {
        DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
        {
            RenderLiveCreators();
        });
    }

}
