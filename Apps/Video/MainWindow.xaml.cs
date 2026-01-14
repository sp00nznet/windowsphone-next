using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;

namespace WindowsPhoneNext.Video;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _progressTimer;
    private readonly DispatcherTimer _hideControlsTimer;
    private bool _isDragging;
    private bool _isPlaying;
    private bool _hasVideo;
    private bool _controlsVisible = true;

    public MainWindow()
    {
        InitializeComponent();

        _progressTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        _progressTimer.Tick += ProgressTimer_Tick;

        _hideControlsTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        };
        _hideControlsTimer.Tick += HideControlsTimer_Tick;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // Check for command line argument (file path)
        var args = Environment.GetCommandLineArgs();
        if (args.Length > 1 && File.Exists(args[1]))
        {
            LoadVideo(args[1]);
        }
    }

    private void LoadVideo(string filePath)
    {
        try
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            NoVideoPlaceholder.Visibility = Visibility.Collapsed;

            VideoPlayer.Source = new Uri(filePath);
            TitleText.Text = Path.GetFileNameWithoutExtension(filePath);

            _hasVideo = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load video: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            LoadingOverlay.Visibility = Visibility.Collapsed;
            NoVideoPlaceholder.Visibility = Visibility.Visible;
        }
    }

    private void VideoPlayer_MediaOpened(object sender, RoutedEventArgs e)
    {
        LoadingOverlay.Visibility = Visibility.Collapsed;

        if (VideoPlayer.NaturalDuration.HasTimeSpan)
        {
            var duration = VideoPlayer.NaturalDuration.TimeSpan;
            TotalTimeText.Text = FormatTime(duration);
            ProgressSlider.Maximum = duration.TotalSeconds;
        }

        // Auto-play
        VideoPlayer.Play();
        _isPlaying = true;
        PlayPauseIcon.Text = "\u23F8"; // Pause icon
        _progressTimer.Start();
        StartHideControlsTimer();
    }

    private void VideoPlayer_MediaEnded(object sender, RoutedEventArgs e)
    {
        _isPlaying = false;
        PlayPauseIcon.Text = "\u25B6"; // Play icon
        _progressTimer.Stop();
        VideoPlayer.Position = TimeSpan.Zero;
        ProgressSlider.Value = 0;
        CurrentTimeText.Text = "0:00";
        ShowControls();
    }

    private void VideoPlayer_MediaFailed(object sender, ExceptionRoutedEventArgs e)
    {
        LoadingOverlay.Visibility = Visibility.Collapsed;
        NoVideoPlaceholder.Visibility = Visibility.Visible;
        _hasVideo = false;

        MessageBox.Show($"Failed to play video: {e.ErrorException?.Message ?? "Unknown error"}",
            "Playback Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void ProgressTimer_Tick(object? sender, EventArgs e)
    {
        if (!_isDragging && VideoPlayer.NaturalDuration.HasTimeSpan)
        {
            ProgressSlider.Value = VideoPlayer.Position.TotalSeconds;
            CurrentTimeText.Text = FormatTime(VideoPlayer.Position);
        }
    }

    private void HideControlsTimer_Tick(object? sender, EventArgs e)
    {
        if (_isPlaying && _hasVideo)
        {
            HideControls();
        }
        _hideControlsTimer.Stop();
    }

    private void ShowControls()
    {
        _controlsVisible = true;
        ControlsOverlay.Visibility = Visibility.Visible;
        Mouse.OverrideCursor = null;
    }

    private void HideControls()
    {
        _controlsVisible = false;
        ControlsOverlay.Visibility = Visibility.Collapsed;
        Mouse.OverrideCursor = Cursors.None;
    }

    private void StartHideControlsTimer()
    {
        _hideControlsTimer.Stop();
        _hideControlsTimer.Start();
    }

    private void Window_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_controlsVisible)
        {
            ShowControls();
        }
        StartHideControlsTimer();
    }

    private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_hasVideo)
        {
            OpenFileButton_Click(sender, e);
            return;
        }

        TogglePlayPause();
    }

    private void TogglePlayPause()
    {
        if (_isPlaying)
        {
            VideoPlayer.Pause();
            _isPlaying = false;
            PlayPauseIcon.Text = "\u25B6"; // Play icon
            _progressTimer.Stop();
            ShowControls();
        }
        else
        {
            VideoPlayer.Play();
            _isPlaying = true;
            PlayPauseIcon.Text = "\u23F8"; // Pause icon
            _progressTimer.Start();
            StartHideControlsTimer();
        }
    }

    private void Rewind_Click(object sender, RoutedEventArgs e)
    {
        if (!_hasVideo) return;

        var newPosition = VideoPlayer.Position - TimeSpan.FromSeconds(10);
        if (newPosition < TimeSpan.Zero)
            newPosition = TimeSpan.Zero;

        VideoPlayer.Position = newPosition;
        StartHideControlsTimer();
    }

    private void Forward_Click(object sender, RoutedEventArgs e)
    {
        if (!_hasVideo || !VideoPlayer.NaturalDuration.HasTimeSpan) return;

        var newPosition = VideoPlayer.Position + TimeSpan.FromSeconds(10);
        if (newPosition > VideoPlayer.NaturalDuration.TimeSpan)
            newPosition = VideoPlayer.NaturalDuration.TimeSpan;

        VideoPlayer.Position = newPosition;
        StartHideControlsTimer();
    }

    private void FullscreenButton_Click(object sender, RoutedEventArgs e)
    {
        // Toggle between normal and maximized
        if (WindowState == WindowState.Maximized)
        {
            WindowState = WindowState.Normal;
            FullscreenIcon.Text = "\u26F6"; // Expand icon
        }
        else
        {
            WindowState = WindowState.Maximized;
            FullscreenIcon.Text = "\u2716"; // Close/minimize icon
        }
        StartHideControlsTimer();
    }

    private void ProgressSlider_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        _isDragging = true;
    }

    private void ProgressSlider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        _isDragging = false;
        if (_hasVideo)
        {
            VideoPlayer.Position = TimeSpan.FromSeconds(ProgressSlider.Value);
        }
    }

    private void ProgressSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isDragging && _hasVideo)
        {
            CurrentTimeText.Text = FormatTime(TimeSpan.FromSeconds(ProgressSlider.Value));
        }
        StartHideControlsTimer();
    }

    private void OpenFileButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Video Files|*.mp4;*.avi;*.mkv;*.mov;*.wmv;*.flv;*.webm|All Files|*.*",
            Title = "Open Video File"
        };

        if (dialog.ShowDialog() == true)
        {
            LoadVideo(dialog.FileName);
        }
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
                if (WindowState == WindowState.Maximized)
                {
                    WindowState = WindowState.Normal;
                }
                else
                {
                    Close();
                }
                e.Handled = true;
                break;

            case Key.Space:
                if (_hasVideo)
                    TogglePlayPause();
                e.Handled = true;
                break;

            case Key.Left:
                Rewind_Click(sender, e);
                e.Handled = true;
                break;

            case Key.Right:
                Forward_Click(sender, e);
                e.Handled = true;
                break;

            case Key.F:
            case Key.F11:
                FullscreenButton_Click(sender, e);
                e.Handled = true;
                break;

            case Key.O:
                if (Keyboard.Modifiers == ModifierKeys.Control)
                {
                    OpenFileButton_Click(sender, e);
                    e.Handled = true;
                }
                break;
        }

        ShowControls();
        StartHideControlsTimer();
    }

    private static string FormatTime(TimeSpan time)
    {
        if (time.TotalHours >= 1)
        {
            return $"{(int)time.TotalHours}:{time.Minutes:D2}:{time.Seconds:D2}";
        }
        return $"{(int)time.TotalMinutes}:{time.Seconds:D2}";
    }

    protected override void OnClosed(EventArgs e)
    {
        _progressTimer.Stop();
        _hideControlsTimer.Stop();
        VideoPlayer.Stop();
        VideoPlayer.Source = null;
        base.OnClosed(e);
    }
}
