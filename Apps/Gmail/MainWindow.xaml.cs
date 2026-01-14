using System.Windows;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;

namespace WindowsPhoneNext.Gmail;

public partial class MainWindow : Window
{
    private bool _isInitialized;
    private const string GmailUrl = "https://mail.google.com";

    // Allowed domains for Gmail functionality
    private static readonly string[] AllowedDomains = new[]
    {
        "mail.google.com",
        "accounts.google.com",
        "www.google.com",
        "google.com",
        "googleapis.com",
        "gstatic.com",
        "googleusercontent.com",
        "gmail.com",
        "www.gmail.com"
    };

    public MainWindow()
    {
        InitializeComponent();
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

            // Initialize WebView2 with custom settings
            var env = await CoreWebView2Environment.CreateAsync();
            await WebView.EnsureCoreWebView2Async(env);

            // Configure WebView2 settings
            WebView.CoreWebView2.Settings.IsStatusBarEnabled = false;
            WebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
            WebView.CoreWebView2.Settings.IsZoomControlEnabled = false;
            WebView.CoreWebView2.Settings.AreDevToolsEnabled = false;

            // Set mobile user agent for better Gmail mobile experience
            WebView.CoreWebView2.Settings.UserAgent =
                "Mozilla/5.0 (Linux; Android 13; WindowsPhoneNext) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Mobile Safari/537.36";

            // Event handlers
            WebView.CoreWebView2.NavigationStarting += WebView_NavigationStarting;
            WebView.CoreWebView2.NavigationCompleted += WebView_NavigationCompleted;

            // Block navigation to non-Gmail domains
            WebView.CoreWebView2.NewWindowRequested += WebView_NewWindowRequested;

            // Inject CSS for mobile viewport
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

            // Navigate to Gmail
            Navigate(GmailUrl);
        }
        catch (Exception ex)
        {
            LoadingOverlay.Visibility = Visibility.Collapsed;
            ErrorText.Text = $"Failed to initialize: {ex.Message}";
            ErrorOverlay.Visibility = Visibility.Visible;
        }
    }

    private void Navigate(string url)
    {
        if (!_isInitialized) return;

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

    private bool IsAllowedDomain(string url)
    {
        try
        {
            var uri = new Uri(url);
            var host = uri.Host.ToLower();

            foreach (var domain in AllowedDomains)
            {
                if (host == domain || host.EndsWith("." + domain))
                {
                    return true;
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private void WebView_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        // Block navigation to non-allowed domains
        if (!IsAllowedDomain(e.Uri))
        {
            e.Cancel = true;
            return;
        }

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
        });
    }

    private void WebView_NewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
    {
        // Handle new window requests (e.g., from links)
        // Keep navigation in the same window if it's an allowed domain
        if (IsAllowedDomain(e.Uri))
        {
            e.Handled = true;
            Navigate(e.Uri);
        }
        else
        {
            // Block navigation to external sites
            e.Handled = true;
        }
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        WebView.CoreWebView2?.Reload();
    }

    private void RetryButton_Click(object sender, RoutedEventArgs e)
    {
        Navigate(GmailUrl);
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
        else if (e.Key == Key.F5)
        {
            WebView.CoreWebView2?.Reload();
            e.Handled = true;
        }
    }

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
