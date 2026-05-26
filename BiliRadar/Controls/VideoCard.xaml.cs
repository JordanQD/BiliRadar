using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading.Tasks;
using BiliRadar.Helpers;
using BiliRadar.Models;
using BiliRadar.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage.Streams;

namespace BiliRadar.Controls;

public enum ViewLaterButtonMode { None, Add, Remove }
public enum CreatorRelationActionMode { Follow, Unfollow }

public sealed partial class VideoCard : UserControl
{
    private const string AddIcon = "M11.0656 8.00389L11.25 7.99875H18.75C20.483 7.99875 21.8992 9.3552 21.9949 11.0643L22 11.2487V12.8096C21.5557 12.3832 21.051 12.0194 20.5 11.7322V11.2487C20.5 10.2822 19.7165 9.49875 18.75 9.49875H11.25C10.3318 9.49875 9.57881 10.2059 9.5058 11.1052L9.5 11.2487V18.7487C9.5 19.6669 10.2071 20.4199 11.1065 20.4929L11.25 20.4987H11.7316C12.0186 21.0497 12.3822 21.5544 12.8084 21.9987H11.25C9.51697 21.9987 8.10075 20.6423 8.00514 18.9332L8 18.7487V11.2487C8 9.51571 9.35645 8.0995 11.0656 8.00389ZM15.5818 4.23284L15.6345 4.40964L16.327 6.998H14.774L14.1856 4.79787C13.9355 3.86431 12.9759 3.31029 12.0423 3.56044L4.79787 5.50158C3.91344 5.73856 3.36966 6.61227 3.52756 7.49737L3.56044 7.64488L5.50158 14.8893C5.69372 15.6064 6.30445 16.0996 7.00045 16.1764L7.00056 17.6816C5.69932 17.6051 4.52962 16.7445 4.10539 15.4544L4.05269 15.2776L2.11155 8.03311C1.66301 6.35913 2.6067 4.6401 4.23284 4.10539L4.40964 4.05269L11.6541 2.11155C13.3281 1.66301 15.0471 2.6067 15.5818 4.23284ZM23 17.5C23 14.4624 20.5376 12 17.5 12C14.4624 12 12 14.4624 12 17.5C12 20.5376 14.4624 23 17.5 23C20.5376 23 23 20.5376 23 17.5ZM17.4101 14.0073L17.5 13.9992L17.5899 14.0073C17.794 14.0443 17.9549 14.2053 17.9919 14.4094L18 14.4992L17.9996 16.9992L20.5046 17L20.5944 17.0081C20.7985 17.0451 20.9595 17.206 20.9965 17.4101L21.0046 17.5L20.9965 17.5899C20.9595 17.794 20.7985 17.9549 20.5944 17.9919L20.5046 18L18.0007 17.9992L18.0011 20.5035L17.9931 20.5934C17.956 20.7975 17.7951 20.9584 17.591 20.9954L17.5011 21.0035L17.4112 20.9954C17.2071 20.9584 17.0462 20.7975 17.0092 20.5934L17.0011 20.5035L17.0007 17.9992L14.4977 18L14.4078 17.9919C14.2037 17.9549 14.0427 17.794 14.0057 17.5899L13.9977 17.5L14.0057 17.4101C14.0427 17.206 14.2037 17.0451 14.4078 17.0081L14.4977 17L16.9996 16.9992L17 14.4992L17.0081 14.4094C17.0451 14.2053 17.206 14.0443 17.4101 14.0073Z";
    private const string RemoveIcon = "M10 2.25C9.0335 2.25 8.25 3.0335 8.25 4V4.75H5C4.58579 4.75 4.25 5.08579 4.25 5.5C4.25 5.91421 4.58579 6.25 5 6.25H19C19.4142 6.25 19.75 5.91421 19.75 5.5C19.75 5.08579 19.4142 4.75 19 4.75H15.75V4C15.75 3.0335 14.9665 2.25 14 2.25H10ZM9.75 4C9.75 3.86193 9.86193 3.75 10 3.75H14C14.1381 3.75 14.25 3.86193 14.25 4V4.75H9.75V4ZM6.75 8C6.75 7.58579 7.08579 7.25 7.5 7.25H16.5C16.9142 7.25 17.25 7.58579 17.25 8V18.5C17.25 20.2949 15.7949 21.75 14 21.75H10C8.20507 21.75 6.75 20.2949 6.75 18.5V8ZM8.25 8.75V18.5C8.25 19.4665 9.0335 20.25 10 20.25H14C14.9665 20.25 15.75 19.4665 15.75 18.5V8.75H8.25ZM10.5 10.75C10.9142 10.75 11.25 11.0858 11.25 11.5V17.5C11.25 17.9142 10.9142 18.25 10.5 18.25C10.0858 18.25 9.75 17.9142 9.75 17.5V11.5C9.75 11.0858 10.0858 10.75 10.5 10.75ZM14.25 11.5C14.25 11.0858 13.9142 10.75 13.5 10.75C13.0858 10.75 12.75 11.0858 12.75 11.5V17.5C12.75 17.9142 13.0858 18.25 13.5 18.25C13.9142 18.25 14.25 17.9142 14.25 17.5V11.5Z";
    private const string FollowIcon = "M10 2C12.7614 2 15 4.23858 15 7C15 9.76142 12.7614 12 10 12C7.23858 12 5 9.76142 5 7C5 4.23858 7.23858 2 10 2ZM10 3.5C8.067 3.5 6.5 5.067 6.5 7C6.5 8.933 8.067 10.5 10 10.5C11.933 10.5 13.5 8.933 13.5 7C13.5 5.067 11.933 3.5 10 3.5ZM4.25 14H11.25C11.6642 14 12 14.3358 12 14.75C12 15.1642 11.6642 15.5 11.25 15.5H4.25C3.83579 15.5 3.5 15.8358 3.5 16.25V17.16C3.5 17.82 3.79 18.44 4.29 18.86C5.54 19.94 7.44 20.5 10 20.5C10.58 20.5 11.13 20.47 11.65 20.41C12.0615 20.3626 12.4337 20.6579 12.4811 21.0694C12.5285 21.4808 12.2332 21.853 11.8218 21.9004C11.2493 21.9664 10.642 22 10 22C7.11 22 4.87 21.34 3.31 20C2.48 19.29 2 18.25 2 17.16V16.25C2 15.0074 3.00736 14 4.25 14ZM18 12C18.4142 12 18.75 12.3358 18.75 12.75V16.25H22.25C22.6642 16.25 23 16.5858 23 17C23 17.4142 22.6642 17.75 22.25 17.75H18.75V21.25C18.75 21.6642 18.4142 22 18 22C17.5858 22 17.25 21.6642 17.25 21.25V17.75H13.75C13.3358 17.75 13 17.4142 13 17C13 16.5858 13.3358 16.25 13.75 16.25H17.25V12.75C17.25 12.3358 17.5858 12 18 12Z";
    private const string UnfollowIcon = "M17.5 12C20.5376 12 23 14.4624 23 17.5C23 20.5376 20.5376 23 17.5 23C14.4624 23 12 20.5376 12 17.5C12 14.4624 14.4624 12 17.5 12ZM12.0223 13.9993C11.7256 14.4626 11.486 14.966 11.3136 15.4996L4.2535 15.4999C3.8393 15.4999 3.5035 15.8357 3.5035 16.2499V17.1572C3.5035 17.8129 3.7895 18.4359 4.2867 18.8634C5.5448 19.945 7.4408 20.5011 10 20.5011C10.5988 20.5011 11.1614 20.4706 11.6881 20.4101C11.9371 20.9103 12.2509 21.374 12.6171 21.7905C11.8149 21.9313 10.942 22.0011 10 22.0011C7.1105 22.0011 4.8717 21.3445 3.3088 20.0008C2.4802 19.2884 2.0035 18.25 2.0035 17.1572V16.2499C2.0035 15.0073 3.0109 13.9999 4.2535 13.9999L12.0223 13.9993ZM15.093 14.9663L15.0238 15.0241L14.9659 15.0934C14.8478 15.2639 14.8478 15.4915 14.9659 15.662L15.0238 15.7312L16.7934 17.5007L15.0264 19.2675L14.9685 19.3367C14.8504 19.5072 14.8504 19.7348 14.9685 19.9053L15.0264 19.9746L15.0956 20.0325C15.2661 20.1506 15.4937 20.1506 15.6643 20.0325L15.7335 19.9746L17.5004 18.2077L19.2694 19.9768L19.3386 20.0347C19.5092 20.1528 19.7367 20.1528 19.9073 20.0347L19.9765 19.9768L20.0344 19.9076C20.1525 19.7371 20.1525 19.5095 20.0344 19.339L19.9765 19.2697L18.2074 17.5007L19.9793 15.7313L20.0371 15.662C20.1552 15.4915 20.1552 15.2639 20.0371 15.0934L19.9793 15.0242L19.9101 14.9663C19.7395 14.8482 19.5119 14.8482 19.3414 14.9663L19.2722 15.0242L17.5004 16.7937L15.7309 15.0241L15.6617 14.9663C15.5155 14.865 15.3274 14.8506 15.1693 14.9229L15.093 14.9663ZM10 2.0046C12.7614 2.0046 15 4.2432 15 7.0046C15 9.766 12.7614 12.0046 10 12.0046C7.2386 12.0046 5 9.766 5 7.0046C5 4.2432 7.2386 2.0046 10 2.0046ZM10 3.5046C8.067 3.5046 6.5 5.0716 6.5 7.0046C6.5 8.9376 8.067 10.5046 10 10.5046C11.933 10.5046 13.5 8.9376 13.5 7.0046C13.5 5.0716 11.933 3.5046 10 3.5046Z";
    private const int MaxRetries = 3;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(450);
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(12) };
    private static readonly ConcurrentDictionary<string, ImageSource> ImgCache = new(StringComparer.OrdinalIgnoreCase);

    // Dynamically built text panel elements
    private TextBlock _titleText = null!, _descText = null!, _creatorText = null!, _timeText = null!;
    private Button _avatarBtn = null!;
    private PersonPicture _avatarPic = null!;
    private MenuFlyout? _flyout;
    private MenuFlyoutItem? _relItem;
    private VideoUpdateRow? _item;

    private bool _loaded;

    public VideoCard() { InitializeComponent(); Loaded += OnLoaded; }

    public VideoUpdateRow? Item { get => _item; set { _item = value; if (_loaded) ApplyData(); } }

    private ViewLaterButtonMode _vlMode = ViewLaterButtonMode.Add;
    public ViewLaterButtonMode ViewLaterButtonMode { get => _vlMode; set { _vlMode = value; if (_loaded) ApplyVLMode(); } }

    private bool _showTime = true;
    public bool ShowMetaTime { get => _showTime; set { _showTime = value; if (_loaded) { _timeText.Visibility = value ? Visibility.Visible : Visibility.Collapsed; _creatorText.MaxWidth = value ? 120 : 240; } } }

    public Func<long, Task<bool>>? IsCreatorFollowedAsync { get; set; }

    public event EventHandler<VideoUpdateRow>? CoverTapped;
    public event EventHandler<VideoUpdateRow>? ViewLaterClicked;
    public event EventHandler<VideoUpdateRow>? CreatorAvatarClicked;
    public event EventHandler<(VideoUpdateRow Item, CreatorRelationActionMode Mode)>? CreatorRelationActionRequested;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        BuildTextPanel();
        BuildFlyout();
        StyleFromResources();
        ApplyData();
        ApplyVLMode();
        // Apply ShowMetaTime without going through the setter (already have refs)
        _timeText.Visibility = _showTime ? Visibility.Visible : Visibility.Collapsed;
        _creatorText.MaxWidth = _showTime ? 120 : 240;
        _loaded = true;
    }

    private void BuildTextPanel()
    {
        var app = Application.Current;
        var textPanel = new Grid { RowSpacing = 4, VerticalAlignment = VerticalAlignment.Stretch };
        textPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        textPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        textPanel.RowDefinitions.Add(new RowDefinition());

        _titleText = new TextBlock { FontSize = 13, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, MaxLines = 2, TextTrimming = TextTrimming.CharacterEllipsis, TextWrapping = TextWrapping.Wrap, Foreground = (Brush)app.Resources["TextFillColorPrimaryBrush"] };
        textPanel.Children.Add(_titleText);

        _descText = new TextBlock { FontSize = 12, MaxLines = 1, Foreground = (Brush)app.Resources["TextFillColorTertiaryBrush"], TextTrimming = TextTrimming.CharacterEllipsis, TextWrapping = TextWrapping.NoWrap, Visibility = Visibility.Collapsed };
        Grid.SetRow(_descText, 1);
        textPanel.Children.Add(_descText);

        var creatorRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 4, 0, 0) };
        Grid.SetRow(creatorRow, 2);
        textPanel.Children.Add(creatorRow);

        _avatarPic = new PersonPicture { Width = 24, Height = 24 };

        _avatarBtn = new Button { Width = 24, Height = 24, MinWidth = 0, Padding = new Thickness(0), BorderThickness = new Thickness(0), BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Transparent), Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center, Content = _avatarPic };
        _avatarBtn.Template = XamlReader.Load("""
            <ControlTemplate
                xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                TargetType="Button">
                <Border x:Name="Root"
                        Background="{TemplateBinding Background}"
                        BorderBrush="{TemplateBinding BorderBrush}"
                        BorderThickness="{TemplateBinding BorderThickness}"
                        CornerRadius="12">
                    <VisualStateManager.VisualStateGroups>
                        <VisualStateGroup x:Name="CommonStates">
                            <VisualState x:Name="Normal" />
                            <VisualState x:Name="PointerOver">
                                <VisualState.Setters>
                                    <Setter Target="Root.Background" Value="{ThemeResource SubtleFillColorSecondaryBrush}" />
                                </VisualState.Setters>
                            </VisualState>
                            <VisualState x:Name="Pressed">
                                <VisualState.Setters>
                                    <Setter Target="Root.Background" Value="{ThemeResource SubtleFillColorTertiaryBrush}" />
                                </VisualState.Setters>
                            </VisualState>
                            <VisualState x:Name="Disabled">
                                <VisualState.Setters>
                                    <Setter Target="Root.Opacity" Value="0.5" />
                                </VisualState.Setters>
                            </VisualState>
                        </VisualStateGroup>
                    </VisualStateManager.VisualStateGroups>
                    <ContentPresenter Padding="{TemplateBinding Padding}"
                                      HorizontalAlignment="{TemplateBinding HorizontalContentAlignment}"
                                      VerticalAlignment="{TemplateBinding VerticalContentAlignment}"
                                      HorizontalContentAlignment="{TemplateBinding HorizontalContentAlignment}"
                                      VerticalContentAlignment="{TemplateBinding VerticalContentAlignment}"
                                      Content="{TemplateBinding Content}"
                                      ContentTemplate="{TemplateBinding ContentTemplate}"
                                      ContentTransitions="{TemplateBinding ContentTransitions}" />
                </Border>
            </ControlTemplate>
            """) as ControlTemplate;
        _avatarBtn.Click += (_, _) => { if (_item is not null) CreatorAvatarClicked?.Invoke(this, _item); };
        ToolTipService.SetToolTip(_avatarBtn, LocalizationHelper.GetString("OpenCreatorHomeTooltip"));
        creatorRow.Children.Add(_avatarBtn);

        _creatorText = new TextBlock { FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = (Brush)app.Resources["TextFillColorSecondaryBrush"], MaxWidth = 120, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis, TextWrapping = TextWrapping.NoWrap };
        creatorRow.Children.Add(_creatorText);

        _timeText = new TextBlock { FontSize = 12, Foreground = (Brush)app.Resources["TextFillColorTertiaryBrush"], HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis, TextWrapping = TextWrapping.NoWrap };
        creatorRow.Children.Add(_timeText);

        RootGrid.Children.Insert(0, textPanel);
    }

    private void StyleFromResources()
    {
        CardBorder.Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"];
        CardBorder.CornerRadius = new CornerRadius(8); // OverlayCornerRadius
        CoverFrame.Background = (Brush)Application.Current.Resources["CardBackgroundFillColorSecondaryBrush"];
        CoverFrame.CornerRadius = new CornerRadius(8);
        DurationBadge.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(180, 0, 0, 0));
        DurationBadge.CornerRadius = new CornerRadius(4); // ControlCornerRadius
        DurationText.Foreground = new SolidColorBrush(Microsoft.UI.Colors.White);
        ViewLaterBtn.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(210, 34, 34, 34));
        ViewLaterBtn.Foreground = new SolidColorBrush(Microsoft.UI.Colors.White);
        ViewLaterBtn.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(80, 255, 255, 255));
        CoverBtn.Style = (Style)Application.Current.Resources["SubtleButtonStyle"];
    }

    private void ApplyData()
    {
        var item = _item; if (item is null) return;
        _titleText.Text = item.Title;
        _creatorText.Text = item.CreatorName;
        _timeText.Text = item.Tip;

        if (string.IsNullOrWhiteSpace(item.Description))
            _descText.Visibility = Visibility.Collapsed;
        else { _descText.Visibility = Visibility.Visible; _descText.Text = item.Description; }

        if (string.IsNullOrWhiteSpace(item.DurationText))
            DurationBadge.Visibility = Visibility.Collapsed;
        else { DurationBadge.Visibility = Visibility.Visible; DurationText.Text = item.DurationText; }

        if (!string.IsNullOrWhiteSpace(item.CoverUrl)) _ = LoadImg(CoverImage, item.CoverUrl);
        if (!string.IsNullOrWhiteSpace(item.AvatarUrl)) _ = LoadAvatar(_avatarPic, item.AvatarUrl);

        ApplyVLMode();
    }

    private void ApplyVLMode()
    {
        if (_vlMode == ViewLaterButtonMode.None) { ViewLaterBtn.Visibility = Visibility.Collapsed; return; }
        ViewLaterBtn.Visibility = Visibility.Visible;
        ViewLaterIcon.Glyph = _vlMode == ViewLaterButtonMode.Remove ? "" : "";
        ToolTipService.SetToolTip(ViewLaterBtn, _vlMode == ViewLaterButtonMode.Remove
            ? LocalizationHelper.GetString("RemoveFromViewLaterTooltip")
            : LocalizationHelper.GetString("AddToViewLaterTooltip"));
    }

    private void BuildFlyout()
    {
        _relItem = new MenuFlyoutItem { FontFamily = new FontFamily("Microsoft YaHei UI") };
        _relItem.Click += (_, _) => { if (_item is not null && _relItem?.Tag is CreatorRelationActionMode m) CreatorRelationActionRequested?.Invoke(this, (_item, m)); };
        _flyout = new MenuFlyout();
        _flyout.Opening += async (_, _) =>
        {
            var it = _item; if (it is null || it.CreatorMid <= 0) { if (_relItem is not null) _relItem.IsEnabled = false; return; }
            _relItem!.IsEnabled = false;
            try { var f = IsCreatorFollowedAsync?.Invoke(it.CreatorMid) is Task<bool> t && await t; SetRelMenu(f ? CreatorRelationActionMode.Unfollow : CreatorRelationActionMode.Follow); }
            catch { }
            finally { _relItem.IsEnabled = true; }
        };
        _flyout.Items.Add(_relItem);
        CardBorder.ContextFlyout = _flyout;
        _avatarBtn.ContextFlyout = _flyout;
    }

    private void SetRelMenu(CreatorRelationActionMode m)
    {
        if (_relItem is null)
        {
            return;
        }

        _relItem.Tag = m;
        _relItem.Text = m == CreatorRelationActionMode.Follow
            ? LocalizationHelper.GetString("FollowCreatorMenuItem")
            : LocalizationHelper.GetString("UnfollowCreatorMenuItem");
        _relItem.Icon = MakeMenuIcon(m == CreatorRelationActionMode.Follow ? FollowIcon : UnfollowIcon);
    }

    private void CardBorder_Tapped(object sender, TappedRoutedEventArgs e) { if (IsInteractive(e.OriginalSource as DependencyObject)) return; if (_item is not null) { e.Handled = true; CoverTapped?.Invoke(this, _item); } }
    private void CoverBtn_Click(object sender, RoutedEventArgs e) { if (_item is not null) CoverTapped?.Invoke(this, _item); }
    private void ViewLaterBtn_Click(object sender, RoutedEventArgs e) { if (_item is not null) ViewLaterClicked?.Invoke(this, _item); }

    private static bool IsInteractive(DependencyObject? s) { while (s is not null) { if (s is Button or MenuFlyoutItem) return true; s = VisualTreeHelper.GetParent(s); } return false; }

    // ── Image loading ──
    private static async Task LoadImg(Image img, string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var u)) return;
        if (ImgCache.TryGetValue(u.AbsoluteUri, out var c)) { img.DispatcherQueue.TryEnqueue(() => img.Source = c); return; }
        for (int i = 1; i <= MaxRetries; i++)
            try
            {
                using var r = new HttpRequestMessage(HttpMethod.Get, u);
                r.Headers.TryAddWithoutValidation("User-Agent", BiliWebDataProvider.BrowserUserAgent);
                r.Headers.TryAddWithoutValidation("Referer", "https://www.bilibili.com/");
                using var res = await Http.SendAsync(r); res.EnsureSuccessStatusCode();
                var b = await res.Content.ReadAsByteArrayAsync();
                img.DispatcherQueue.TryEnqueue(async () => { try { using var s = new InMemoryRandomAccessStream(); await s.WriteAsync(b.AsBuffer()); s.Seek(0); var bmp = new BitmapImage(); await bmp.SetSourceAsync(s); ImgCache[u.AbsoluteUri] = bmp; img.Source = bmp; } catch { var bmp = new BitmapImage(u); ImgCache[u.AbsoluteUri] = bmp; img.Source = bmp; } });
                return;
            }
            catch { if (i == MaxRetries) img.DispatcherQueue.TryEnqueue(() => { var bmp = new BitmapImage(u); ImgCache[u.AbsoluteUri] = bmp; img.Source = bmp; }); else await Task.Delay(RetryDelay * i); }
    }

    private static async Task LoadImgBrush(ImageBrush br, string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var u)) return;
        if (ImgCache.TryGetValue(u.AbsoluteUri, out var c)) { br.DispatcherQueue.TryEnqueue(() => br.ImageSource = c); return; }
        for (int i = 1; i <= MaxRetries; i++)
            try
            {
                using var r = new HttpRequestMessage(HttpMethod.Get, u);
                r.Headers.TryAddWithoutValidation("User-Agent", BiliWebDataProvider.BrowserUserAgent);
                r.Headers.TryAddWithoutValidation("Referer", "https://www.bilibili.com/");
                using var res = await Http.SendAsync(r); res.EnsureSuccessStatusCode();
                var b = await res.Content.ReadAsByteArrayAsync();
                br.DispatcherQueue.TryEnqueue(async () => { try { using var s = new InMemoryRandomAccessStream(); await s.WriteAsync(b.AsBuffer()); s.Seek(0); var bmp = new BitmapImage(); await bmp.SetSourceAsync(s); ImgCache[u.AbsoluteUri] = bmp; br.ImageSource = bmp; } catch { var bmp = new BitmapImage(u); ImgCache[u.AbsoluteUri] = bmp; br.ImageSource = bmp; } });
                return;
            }
            catch { if (i == MaxRetries) br.DispatcherQueue.TryEnqueue(() => { var bmp = new BitmapImage(u); ImgCache[u.AbsoluteUri] = bmp; br.ImageSource = bmp; }); else await Task.Delay(RetryDelay * i); }
    }

    private static async Task LoadAvatar(PersonPicture personPic, string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var u)) return;
        if (ImgCache.TryGetValue(u.AbsoluteUri, out var c)) { personPic.DispatcherQueue.TryEnqueue(() => personPic.ProfilePicture = c); return; }
        for (int i = 1; i <= MaxRetries; i++)
            try
            {
                using var r = new HttpRequestMessage(HttpMethod.Get, u);
                r.Headers.TryAddWithoutValidation("User-Agent", BiliWebDataProvider.BrowserUserAgent);
                r.Headers.TryAddWithoutValidation("Referer", "https://www.bilibili.com/");
                using var res = await Http.SendAsync(r); res.EnsureSuccessStatusCode();
                var b = await res.Content.ReadAsByteArrayAsync();
                personPic.DispatcherQueue.TryEnqueue(async () => { try { using var s = new InMemoryRandomAccessStream(); await s.WriteAsync(b.AsBuffer()); s.Seek(0); var bmp = new BitmapImage(); await bmp.SetSourceAsync(s); ImgCache[u.AbsoluteUri] = bmp; personPic.ProfilePicture = bmp; } catch { var bmp = new BitmapImage(u); ImgCache[u.AbsoluteUri] = bmp; personPic.ProfilePicture = bmp; } });
                return;
            }
            catch { if (i == MaxRetries) personPic.DispatcherQueue.TryEnqueue(() => { var bmp = new BitmapImage(u); ImgCache[u.AbsoluteUri] = bmp; personPic.ProfilePicture = bmp; }); else await Task.Delay(RetryDelay * i); }
    }

    private static IconElement MakeMenuIcon(string data) => (IconElement)XamlReader.Load($$"""<PathIcon xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" Width="24" Height="24" Data="{{data}}" />""");
}
