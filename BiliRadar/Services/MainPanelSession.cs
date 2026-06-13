using BiliRadar.Helpers;
using BiliRadar.Controls;
using BiliRadar.Models;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;

namespace BiliRadar.Services;

public sealed class MainPanelSession : IDisposable, INotifyPropertyChanged
{
    private const int MaxCachedRoundedImages = 48;

    private readonly UpdateMonitorService _updateMonitorService;
    private readonly CookieStore _cookieStore;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly HashSet<string> _loadedUpdateIds = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _loadedHistoryIds = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _loadedViewLaterIds = new(StringComparer.OrdinalIgnoreCase);

    // ── Loading state ──
    private bool _isLoading;
    private bool _isLoadingMore;
    private bool _isLoadingHistory;
    private bool _isLoadingMoreHistory;
    private bool _isLoadingViewLater;
    private bool _isLoadingMoreViewLater;
    private bool _refreshQueuedOnShow;
    private int _pendingImageLoads;
    private bool _hasMoreUpdates = true;
    private bool _hasMoreHistory = true;
    private bool _hasMoreViewLater = true;

    static MainPanelSession()
    {
        ImageHttpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(12) };
    }

    public MainPanelSession(CookieStore cookieStore, MainWindowSnapshot? snapshot = null)
    {
        _cookieStore = cookieStore;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _updateMonitorService = new UpdateMonitorService(new BiliWebDataProvider(cookieStore));

        if (snapshot is not null)
        {
            foreach (var update in snapshot.Updates.OrderByDescending(item => item.PublishedAt))
            {
                if (_loadedUpdateIds.Add(update.Id))
                {
                    var row = new VideoUpdateRow(update);
                    row.RefreshPublishedTip();
                    Updates.Add(row);
                }
            }

            foreach (var creator in snapshot.LiveCreators)
                LiveCreators.Add(new LiveCreatorRow(creator));

            foreach (var item in snapshot.HistoryItems.OrderByDescending(item => item.PublishedAt))
            {
                if (_loadedHistoryIds.Add(item.Id))
                    HistoryItems.Add(new VideoUpdateRow(item));
            }

            foreach (var item in snapshot.ViewLaterItems.OrderByDescending(item => item.PublishedAt))
            {
                if (_loadedViewLaterIds.Add(item.Id))
                    ViewLaterItems.Add(new VideoUpdateRow(item));
            }

            UnreadCount = Updates.Count(item => item.IsUnread);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    // ── Collections ──
    public ObservableCollection<VideoUpdateRow> Updates { get; } = [];
    public ObservableCollection<VideoUpdateRow> HistoryItems { get; } = [];
    public ObservableCollection<VideoUpdateRow> ViewLaterItems { get; } = [];
    public ObservableCollection<CreatorRow> Following { get; } = [];
    public ObservableCollection<LiveCreatorRow> LiveCreators { get; } = [];
    public ObservableCollection<StatusNotification> StatusNotifications { get; } = [];

    // ── Bound properties ──
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
    private int _unreadCount;

    public int FollowingCount
    {
        get => _followingCount;
        private set => SetProperty(ref _followingCount, value);
    }
    private int _followingCount;

    public string LastCheckedText
    {
        get => _lastCheckedText;
        private set => SetProperty(ref _lastCheckedText, value);
    }
    private string _lastCheckedText = LocalizationHelper.GetString("LastCheckedNotYet");

    public string FollowingListText
    {
        get => _followingListText;
        private set => SetProperty(ref _followingListText, value);
    }
    private string _followingListText = LocalizationHelper.GetString("NoFollowingData");

    public bool RefreshProgressIsActive => IsLoading || _isLoadingHistory || _isLoadingViewLater
        || _pendingImageLoads > 0
        || _isLoadingMoreHistory || _isLoadingMoreViewLater || _isLoadingMore;

    public double RefreshProgressOpacity => RefreshProgressIsActive ? 1.0 : 0.0;

    // ── Data loading ──

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        RefreshUpdateRelativeTimes();

        if (IsLoading) return;

        IsLoading = true;
        try
        {
            ClearStatusNotifications();
            if (!_cookieStore.HasCookie)
            {
                ClearSignedOutData();
                UpdatesRefreshed?.Invoke(this, EventArgs.Empty);
                return;
            }

            await RefreshFollowingListAsync(cancellationToken);

            IReadOnlyList<BiliVideoUpdate> updates = [];
            try
            {
                updates = await _updateMonitorService.RefreshAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
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
                    row.RefreshPublishedTip();
                    Updates.Insert(newRows.Count, row);
                    newRows.Add(row);
                }
                else
                {
                    var existing = Updates.FirstOrDefault(r => string.Equals(r.Id, update.Id, StringComparison.OrdinalIgnoreCase));
                    if (existing != null)
                        existing.RefreshPublishedTip();
                }
            }

            RemoveStaleVideoCards(updates);

            _hasMoreUpdates = Updates.Count > 0;
            UnreadCount = Updates.Count(item => item.IsUnread);
            LastCheckedText = _updateMonitorService.LastCheckedAt.ToString("HH:mm:ss");
            if (_cookieStore.HasCookie && Updates.Count == 0 && StatusNotifications.Count == 0)
                ShowStatus(LocalizationHelper.GetString("NoVideoUpdates"), InfoBarSeverity.Informational);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            ShowStatus(ex.Message, InfoBarSeverity.Error);
        }
        finally
        {
            RefreshUpdateRelativeTimes();
            IsLoading = false;
            UpdatesRefreshed?.Invoke(this, EventArgs.Empty);
        }
    }

    public async Task RefreshOnShowAsync(CancellationToken cancellationToken = default)
    {
        if (_refreshQueuedOnShow) return;

        _refreshQueuedOnShow = true;
        try
        {
            await RefreshAsync(cancellationToken);
        }
        finally
        {
            _refreshQueuedOnShow = false;
        }
    }

    public async Task LoadMoreUpdatesAsync(CancellationToken cancellationToken = default)
    {
        if (_isLoading || _isLoadingMore || !_hasMoreUpdates) return;

        _isLoadingMore = true;
        NotifyRefreshProgressChanged();
        try
        {
            var page = await _updateMonitorService.LoadMoreAsync(cancellationToken);
            _hasMoreUpdates = page.HasMore;

            foreach (var update in page.Items.OrderByDescending(item => item.PublishedAt))
            {
                if (AddUpdateIfNew(update))
                    CollectionAdded?.Invoke(this, Updates[^1]);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            ShowStatus(LocalizationHelper.Format("LoadEarlierFailed", ex.Message), InfoBarSeverity.Error);
        }
        finally
        {
            _isLoadingMore = false;
            NotifyRefreshProgressChanged();
        }
    }

    public async Task RefreshHistoryAsync(CancellationToken cancellationToken = default)
    {
        if (_isLoadingHistory) return;

        _isLoadingHistory = true;
        NotifyRefreshProgressChanged();
        try
        {
            ClearStatusNotifications();

            if (!_cookieStore.HasCookie)
            {
                HistoryItems.Clear();
                _loadedHistoryIds.Clear();
                _hasMoreHistory = false;
                HistoryRefreshed?.Invoke(this, EventArgs.Empty);
                return;
            }

            await RefreshFollowingListAsync(cancellationToken);

            var page = await _updateMonitorService.RefreshHistoryAsync(cancellationToken);
            _hasMoreHistory = page.HasMore;

            int removed = DiffRefreshHistory(page.Items);

            if (removed > page.Items.Count / 2 && _hasMoreHistory)
                _dispatcherQueue.TryEnqueue(() => _ = LoadMoreHistoryAsync(cancellationToken));

            if (HistoryItems.Count == 0 && StatusNotifications.Count == 0)
                ShowStatus(LocalizationHelper.GetString("NoHistory"), InfoBarSeverity.Informational);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            ShowStatus(LocalizationHelper.Format("HistoryLoadFailed", ex.Message), InfoBarSeverity.Error);
        }
        finally
        {
            _isLoadingHistory = false;
            NotifyRefreshProgressChanged();
            HistoryRefreshed?.Invoke(this, EventArgs.Empty);
        }
    }

    public async Task LoadMoreHistoryAsync(CancellationToken cancellationToken = default)
    {
        if (_isLoadingMoreHistory || !_hasMoreHistory) return;

        _isLoadingMoreHistory = true;
        NotifyRefreshProgressChanged();
        try
        {
            var page = await _updateMonitorService.LoadMoreHistoryAsync(cancellationToken);
            _hasMoreHistory = page.HasMore;
            foreach (var item in page.Items)
            {
                var change = AddOrUpdateHistoryItem(item);
                if (change.Kind == HistoryItemChangeKind.Inserted)
                    CollectionAdded?.Invoke(this, HistoryItems[^1]);
                else if (change.Kind == HistoryItemChangeKind.Updated)
                    CollectionUpdated?.Invoke(this, (change.NewIndex, HistoryItems[change.NewIndex]));
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            ShowStatus(LocalizationHelper.Format("LoadEarlierHistoryFailed", ex.Message), InfoBarSeverity.Error);
        }
        finally
        {
            _isLoadingMoreHistory = false;
            NotifyRefreshProgressChanged();
        }
    }

    public async Task RefreshViewLaterAsync(CancellationToken cancellationToken = default)
    {
        if (_isLoadingViewLater) return;

        _isLoadingViewLater = true;
        NotifyRefreshProgressChanged();
        try
        {
            ClearStatusNotifications();

            if (!_cookieStore.HasCookie)
            {
                ViewLaterItems.Clear();
                _loadedViewLaterIds.Clear();
                _hasMoreViewLater = false;
                ViewLaterRefreshed?.Invoke(this, EventArgs.Empty);
                return;
            }

            await RefreshFollowingListAsync(cancellationToken);

            var page = await _updateMonitorService.RefreshViewLaterAsync(cancellationToken);
            _hasMoreViewLater = page.HasMore;

            int removed = DiffRefreshViewLater(page.Items);

            if (removed > page.Items.Count / 2 && _hasMoreViewLater)
                _dispatcherQueue.TryEnqueue(() => _ = LoadMoreViewLaterAsync(cancellationToken));

            if (ViewLaterItems.Count == 0 && StatusNotifications.Count == 0)
                ShowStatus(LocalizationHelper.GetString("NoViewLater"), InfoBarSeverity.Informational);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            ShowStatus(LocalizationHelper.Format("ViewLaterLoadFailed", ex.Message), InfoBarSeverity.Error);
        }
        finally
        {
            _isLoadingViewLater = false;
            NotifyRefreshProgressChanged();
            ViewLaterRefreshed?.Invoke(this, EventArgs.Empty);
        }
    }

    public async Task LoadMoreViewLaterAsync(CancellationToken cancellationToken = default)
    {
        if (_isLoadingMoreViewLater || !_hasMoreViewLater) return;

        _isLoadingMoreViewLater = true;
        NotifyRefreshProgressChanged();
        try
        {
            var page = await _updateMonitorService.LoadMoreViewLaterAsync(cancellationToken);
            _hasMoreViewLater = page.HasMore;
            foreach (var item in page.Items)
            {
                if (AddViewLaterIfNew(item))
                    CollectionAdded?.Invoke(this, ViewLaterItems[^1]);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            ShowStatus(LocalizationHelper.Format("LoadMoreViewLaterFailed", ex.Message), InfoBarSeverity.Error);
        }
        finally
        {
            _isLoadingMoreViewLater = false;
            NotifyRefreshProgressChanged();
        }
    }

    // ── Business operations ──

    public async Task LaunchSelectedBrowserUriAsync(MainPanelSection section)
    {
        var url = section switch
        {
            MainPanelSection.History => "https://www.bilibili.com/history",
            MainPanelSection.ViewLater => "https://www.bilibili.com/watchlater/list",
            _ => "https://www.bilibili.com/",
        };
        await Windows.System.Launcher.LaunchUriAsync(new Uri(url));
    }

    public async Task AddToViewLaterAsync(long aid)
    {
        await _updateMonitorService.AddToViewLaterAsync(aid);
    }

    public async Task RemoveFromViewLaterAsync(long aid)
    {
        await _updateMonitorService.RemoveFromViewLaterAsync(aid);
    }

    public async Task FollowCreatorAsync(long mid)
    {
        await _updateMonitorService.FollowCreatorAsync(mid);
    }

    public async Task UnfollowCreatorAsync(long mid)
    {
        await _updateMonitorService.UnfollowCreatorAsync(mid);
    }

    public Task<bool> IsCreatorFollowedAsync(long mid, CancellationToken cancellationToken = default)
    {
        return _updateMonitorService.IsCreatorFollowedAsync(mid, cancellationToken);
    }

    public CreatorRelationActionMode GetCreatorRelationActionMode(VideoUpdateRow item)
    {
        return IsCreatorFollowed(item.CreatorMid)
            ? CreatorRelationActionMode.Unfollow
            : CreatorRelationActionMode.Follow;
    }

    public bool IsCreatorFollowed(long mid)
    {
        return mid > 0 && Following.Any(item => item.Mid == mid);
    }

    public void RemoveViewLaterItem(VideoUpdateRow item)
    {
        var index = ViewLaterItems.IndexOf(item);
        if (index < 0)
        {
            index = ViewLaterItems
                .Select((row, rowIndex) => new { row, rowIndex })
                .FirstOrDefault(match => match.row.Aid == item.Aid
                    || string.Equals(match.row.Id, item.Id, StringComparison.OrdinalIgnoreCase))
                ?.rowIndex ?? -1;
        }

        if (index >= 0)
        {
            var removed = ViewLaterItems[index];
            _loadedViewLaterIds.Remove(removed.Id);
            ViewLaterItems.RemoveAt(index);
        }
        else
        {
            _loadedViewLaterIds.Remove(item.Id);
        }
    }

    public void AddFollowingCreator(VideoUpdateRow item)
    {
        if (item.CreatorMid <= 0 || Following.Any(creator => creator.Mid == item.CreatorMid))
            return;

        Following.Add(new CreatorRow(new BiliCreator(item.CreatorMid, item.CreatorName, item.AvatarUrl)));
        FollowingCount = Following.Count;
        FollowingListText = Following.Count == 0
            ? LocalizationHelper.GetString("NoFollowingData")
            : string.Join(Environment.NewLine, Following.Select(creator => $"{creator.Name}  UID:{creator.Mid}"));
    }

    public void RemoveFollowingCreator(long mid)
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

    public IDisposable TrackImageLoad()
    {
        _pendingImageLoads++;
        NotifyRefreshProgressChanged();
        return new ImageLoadTracker(this);
    }

    public void ShowStatus(string message, InfoBarSeverity severity)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            ClearStatusNotifications();
            return;
        }

        var notification = new StatusNotification(message, severity);
        StatusNotifications.Add(notification);
        StatusAdded?.Invoke(this, notification);

        var timer = _dispatcherQueue.CreateTimer();
        timer.Interval = TimeSpan.FromSeconds(3);
        timer.IsRepeating = false;
        timer.Tick += (_, _) => DismissStatusNotification(notification);
        notification.AutoDismissTimer = timer;
        timer.Start();
    }

    public void DismissStatusNotification(StatusNotification notification)
    {
        if (!StatusNotifications.Contains(notification))
        {
            return;
        }

        notification.AutoDismissTimer?.Stop();
        notification.AutoDismissTimer = null;
        StatusNotifications.Remove(notification);
        StatusRemoved?.Invoke(this, notification);
    }

    public void ClearSignedOutData()
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
    }

    public void RefreshUpdateRelativeTimes()
    {
        foreach (var item in Updates)
        {
            item.RefreshPublishedTip();
        }
    }

    public MainWindowSnapshot CreateSnapshot()
    {
        return new MainWindowSnapshot(
            Updates.Select(item => item.ToModel()).ToList(),
            LiveCreators.Select(item => item.ToModel()).ToList(),
            HistoryItems.Select(item => item.ToModel()).ToList(),
            ViewLaterItems.Select(item => item.ToModel()).ToList());
    }

    // ── Events for UI to react to collection changes ──

    public event EventHandler<VideoUpdateRow>? CollectionAdded;
    public event EventHandler<(int index, VideoUpdateRow item)>? CollectionUpdated;
    public event EventHandler<StatusNotification>? StatusAdded;
    public event EventHandler<StatusNotification>? StatusRemoved;
    public event EventHandler? StatusCleared;
    public event EventHandler? UpdatesRefreshed;
    public event EventHandler? HistoryRefreshed;
    public event EventHandler? ViewLaterRefreshed;
    public event EventHandler? FollowingListRefreshed;

    // ── Image helpers (moved from MainWindow) ──

    public static readonly HttpClient ImageHttpClient;

    // ── IDisposable ──

    public void Dispose()
    {
        StopStatusNotificationTimers();
        _updateMonitorService.Dispose();
        Updates.Clear();
        HistoryItems.Clear();
        ViewLaterItems.Clear();
        Following.Clear();
        LiveCreators.Clear();
        _loadedUpdateIds.Clear();
        _loadedHistoryIds.Clear();
        _loadedViewLaterIds.Clear();
        CollectionAdded = null;
        CollectionUpdated = null;
        StatusAdded = null;
        StatusRemoved = null;
        StatusCleared = null;
        UpdatesRefreshed = null;
        HistoryRefreshed = null;
        ViewLaterRefreshed = null;
        FollowingListRefreshed = null;
        PropertyChanged = null;
    }

    // ── Private helpers ──

    private async Task RefreshFollowingListAsync(CancellationToken cancellationToken = default)
    {
        var following = await _updateMonitorService.GetFollowingAsync(cancellationToken);

        Following.Clear();
        foreach (var creator in following)
            Following.Add(new CreatorRow(creator));

        LiveCreators.Clear();
        try
        {
            var liveCreators = await _updateMonitorService.GetFollowingLiveCreatorsAsync(cancellationToken);
            foreach (var creator in liveCreators)
                LiveCreators.Add(new LiveCreatorRow(creator));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            LiveCreators.Clear();
        }

        FollowingCount = Following.Count;
        FollowingListText = Following.Count == 0
            ? LocalizationHelper.GetString("NoFollowingData")
            : string.Join(Environment.NewLine, Following.Select(item => $"{item.Name}  UID:{item.Mid}"));
        FollowingListRefreshed?.Invoke(this, EventArgs.Empty);
    }

    private bool AddUpdateIfNew(BiliVideoUpdate update)
    {
        if (!_loadedUpdateIds.Add(update.Id)) return false;
        Updates.Add(new VideoUpdateRow(update));
        return true;
    }

    private bool AddViewLaterIfNew(BiliVideoUpdate item)
    {
        if (!_loadedViewLaterIds.Add(item.Id)) return false;
        ViewLaterItems.Add(new VideoUpdateRow(item));
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
                return HistoryItemChange.None;

            HistoryItems.RemoveAt(existingIndex);
            var target = insertIndex ?? existingIndex;
            if (existingIndex < target) target--;
            target = Math.Clamp(target, 0, HistoryItems.Count);
            HistoryItems.Insert(target, row);
            return HistoryItemChange.Updated(existingIndex, target);
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

    private void RemoveStaleVideoCards(IReadOnlyList<BiliVideoUpdate> serverItems)
    {
        if (serverItems.Count == 0) return;
        var serverIds = new HashSet<string>(serverItems.Select(u => u.Id), StringComparer.OrdinalIgnoreCase);
        var oldestDate = serverItems.Min(u => u.PublishedAt);

        for (var i = Updates.Count - 1; i >= 0; i--)
        {
            var item = Updates[i];
            if (_loadedUpdateIds.Contains(item.Id) && !serverIds.Contains(item.Id) && item.PublishedAt >= oldestDate)
                Updates.RemoveAt(i);
        }
    }

    private int DiffRefreshHistory(IReadOnlyList<BiliVideoUpdate> serverItems)
    {
        if (serverItems.Count == 0) return 0;
        var serverIds = new HashSet<string>(serverItems.Select(i => i.Id), StringComparer.OrdinalIgnoreCase);
        var oldestDate = serverItems.Min(i => i.PublishedAt);
        int removed = 0;

        for (int i = HistoryItems.Count - 1; i >= 0; i--)
        {
            if (!serverIds.Contains(HistoryItems[i].Id) && HistoryItems[i].PublishedAt >= oldestDate)
            {
                HistoryItems.RemoveAt(i);
                removed++;
            }
        }

        int serverIdx = 0;
        foreach (var serverItem in serverItems)
        {
            var existingIdx = IndexById(HistoryItems, serverItem.Id);
            if (existingIdx >= 0)
            {
                if (existingIdx != serverIdx)
                    HistoryItems.Move(existingIdx, serverIdx);
            }
            else
            {
                _loadedHistoryIds.Add(serverItem.Id);
                HistoryItems.Insert(serverIdx, new VideoUpdateRow(serverItem));
            }
            serverIdx++;
        }

        return removed;
    }

    private int DiffRefreshViewLater(IReadOnlyList<BiliVideoUpdate> serverItems)
    {
        if (serverItems.Count == 0) return 0;
        var serverIds = new HashSet<string>(serverItems.Select(i => i.Id), StringComparer.OrdinalIgnoreCase);
        var oldestDate = serverItems.Min(i => i.PublishedAt);
        int removed = 0;

        for (int i = ViewLaterItems.Count - 1; i >= 0; i--)
        {
            if (!serverIds.Contains(ViewLaterItems[i].Id) && ViewLaterItems[i].PublishedAt >= oldestDate)
            {
                ViewLaterItems.RemoveAt(i);
                removed++;
            }
        }

        int serverIdx = 0;
        foreach (var serverItem in serverItems)
        {
            var existingIdx = IndexById(ViewLaterItems, serverItem.Id);
            if (existingIdx >= 0)
            {
                if (existingIdx != serverIdx)
                    ViewLaterItems.Move(existingIdx, serverIdx);
            }
            else
            {
                _loadedViewLaterIds.Add(serverItem.Id);
                ViewLaterItems.Insert(serverIdx, new VideoUpdateRow(serverItem));
            }
            serverIdx++;
        }

        return removed;
    }

    private static int IndexById(IList<VideoUpdateRow> list, string id)
    {
        for (int i = 0; i < list.Count; i++)
            if (string.Equals(list[i].Id, id, StringComparison.OrdinalIgnoreCase))
                return i;
        return -1;
    }

    private void RemoveStatusNotification(StatusNotification notification)
    {
        DismissStatusNotification(notification);
    }

    private void ClearStatusNotifications()
    {
        foreach (var n in StatusNotifications)
        {
            n.AutoDismissTimer?.Stop();
            n.AutoDismissTimer = null;
        }
        StatusNotifications.Clear();
        StatusCleared?.Invoke(this, EventArgs.Empty);
    }

    private void StopStatusNotificationTimers()
    {
        foreach (var n in StatusNotifications)
        {
            n.AutoDismissTimer?.Stop();
            n.AutoDismissTimer = null;
        }
        StatusNotifications.Clear();
    }

    private void SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(storage, value)) return;
        storage = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        if (propertyName == nameof(IsLoading))
            NotifyRefreshProgressChanged();
    }

    private void NotifyRefreshProgressChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RefreshProgressIsActive)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RefreshProgressOpacity)));
    }

    private void CompleteImageLoad()
    {
        if (!_dispatcherQueue.HasThreadAccess)
        {
            _dispatcherQueue.TryEnqueue(CompleteImageLoad);
            return;
        }

        if (_pendingImageLoads <= 0)
            return;

        _pendingImageLoads--;
        NotifyRefreshProgressChanged();
    }

    private readonly record struct HistoryItemChange(HistoryItemChangeKind Kind, int OldIndex, int NewIndex)
    {
        public static HistoryItemChange None { get; } = new(HistoryItemChangeKind.None, -1, -1);
        public static HistoryItemChange Inserted(int index) => new(HistoryItemChangeKind.Inserted, -1, index);
        public static HistoryItemChange Updated(int oldIndex, int newIndex) => new(HistoryItemChangeKind.Updated, oldIndex, newIndex);
    }

    private enum HistoryItemChangeKind { None, Inserted, Updated }

    private sealed class ImageLoadTracker(MainPanelSession owner) : IDisposable
    {
        private bool _isDisposed;

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            owner.CompleteImageLoad();
        }
    }
}
