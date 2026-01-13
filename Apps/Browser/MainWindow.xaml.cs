using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;

namespace WindowsPhoneNext.Browser;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<Bookmark> _bookmarks = new();
    private bool _isInitialized;

    public MainWindow()
    {
        InitializeComponent();
        BookmarksList.ItemsSource = _bookmarks;
        LoadBookmarks();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        await InitializeWebViewAsync();
    }

    private async Task InitializeWebViewAsync()
    {
        try
        {
            LoadingOverlay.Visibility = Visibility.Visible;

            // Initialize WebView2 with custom settings for 720x720 display
            var env = await CoreWebView2Environment.CreateAsync();
            await WebView.EnsureCoreWebView2Async(env);

            // Configure WebView2 settings
            WebView.CoreWebView2.Settings.IsStatusBarEnabled = false;
            WebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
            WebView.CoreWebView2.Settings.IsZoomControlEnabled = false;
            WebView.CoreWebView2.Settings.AreDevToolsEnabled = false;

            // Set user agent to indicate mobile device for better 720x720 rendering
            WebView.CoreWebView2.Settings.UserAgent =
                "Mozilla/5.0 (Linux; Android 13; WindowsPhoneNext) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Mobile Safari/537.36";

            // Event handlers
            WebView.CoreWebView2.NavigationStarting += WebView_NavigationStarting;
            WebView.CoreWebView2.NavigationCompleted += WebView_NavigationCompleted;
            WebView.CoreWebView2.SourceChanged += WebView_SourceChanged;

            // Inject CSS to enforce viewport sizing
            await WebView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(@"
                (function() {
                    // Add viewport meta tag if not present
                    if (!document.querySelector('meta[name=viewport]')) {
                        var meta = document.createElement('meta');
                        meta.name = 'viewport';
                        meta.content = 'width=720, initial-scale=1.0, maximum-scale=1.0, user-scalable=no';
                        document.head.appendChild(meta);
                    }
                })();
            ");

            _isInitialized = true;

            // Navigate to home page
            Navigate(UrlBar.Text);
        }
        catch (Exception ex)
        {
            LoadingOverlay.Visibility = Visibility.Collapsed;
            ErrorText.Text = $"Failed to initialize browser: {ex.Message}";
            ErrorOverlay.Visibility = Visibility.Visible;
        }
    }

    private void Navigate(string url)
    {
        if (!_isInitialized) return;

        // Add https:// if no protocol specified
        if (!url.StartsWith("http://") && !url.StartsWith("https://"))
        {
            // Check if it looks like a URL
            if (url.Contains(".") && !url.Contains(" "))
            {
                url = "https://" + url;
            }
            else
            {
                // Treat as search query
                url = $"https://www.google.com/search?q={Uri.EscapeDataString(url)}";
            }
        }

        UrlBar.Text = url;
        ErrorOverlay.Visibility = Visibility.Collapsed;

        try
        {
            WebView.CoreWebView2.Navigate(url);
        }
        catch (Exception ex)
        {
            ErrorText.Text = $"Navigation error: {ex.Message}";
            ErrorOverlay.Visibility = Visibility.Visible;
        }
    }

    private void WebView_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            ErrorOverlay.Visibility = Visibility.Collapsed;
        });
    }

    private void WebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            LoadingOverlay.Visibility = Visibility.Collapsed;

            if (!e.IsSuccess)
            {
                ErrorText.Text = $"Failed to load page (Error: {e.WebErrorStatus})";
                ErrorOverlay.Visibility = Visibility.Visible;
            }

            UpdateNavigationButtons();
        });
    }

    private void WebView_SourceChanged(object? sender, CoreWebView2SourceChangedEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            UrlBar.Text = WebView.CoreWebView2.Source;
            UpdateNavigationButtons();
        });
    }

    private void UpdateNavigationButtons()
    {
        BackButton.IsEnabled = WebView.CoreWebView2?.CanGoBack ?? false;
        ForwardButton.IsEnabled = WebView.CoreWebView2?.CanGoForward ?? false;
    }

    #region Navigation

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (WebView.CoreWebView2?.CanGoBack == true)
        {
            WebView.CoreWebView2.GoBack();
        }
    }

    private void ForwardButton_Click(object sender, RoutedEventArgs e)
    {
        if (WebView.CoreWebView2?.CanGoForward == true)
        {
            WebView.CoreWebView2.GoForward();
        }
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        WebView.CoreWebView2?.Reload();
    }

    private void GoButton_Click(object sender, RoutedEventArgs e)
    {
        Navigate(UrlBar.Text);
    }

    private void UrlBar_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Navigate(UrlBar.Text);
        }
    }

    private void RetryButton_Click(object sender, RoutedEventArgs e)
    {
        Navigate(UrlBar.Text);
    }

    #endregion

    #region Bookmarks

    private void LoadBookmarks()
    {
        // Default bookmarks
        _bookmarks.Add(new Bookmark { Title = "Google", Url = "https://www.google.com" });
        _bookmarks.Add(new Bookmark { Title = "Wikipedia", Url = "https://www.wikipedia.org" });
        _bookmarks.Add(new Bookmark { Title = "Weather", Url = "https://weather.com" });
        _bookmarks.Add(new Bookmark { Title = "News", Url = "https://news.google.com" });
    }

    private void CloseBookmarks_Click(object sender, RoutedEventArgs e)
    {
        BookmarksPanel.Visibility = Visibility.Collapsed;
    }

    private void BookmarksList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (BookmarksList.SelectedItem is Bookmark bookmark)
        {
            Navigate(bookmark.Url);
            BookmarksPanel.Visibility = Visibility.Collapsed;
            BookmarksList.SelectedItem = null;
        }
    }

    #endregion

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        WebView?.Dispose();
        base.OnClosed(e);
    }
}

public class Bookmark
{
    public string Title { get; set; } = "";
    public string Url { get; set; } = "";
}
