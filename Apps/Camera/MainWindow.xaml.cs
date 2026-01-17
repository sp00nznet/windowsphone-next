using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace WindowsPhoneCamera;

public partial class MainWindow : Window
{
    private enum CameraMode { Photo, Video, Portrait }
    private enum FlashMode { Auto, On, Off }

    private CameraMode _currentMode = CameraMode.Photo;
    private FlashMode _flashMode = FlashMode.Auto;
    private bool _isRecording;
    private bool _isFrontCamera;
    private readonly DispatcherTimer _recordingTimer;
    private TimeSpan _recordingDuration;
    private readonly string _photosPath;
    private readonly string _videosPath;

    public MainWindow()
    {
        InitializeComponent();

        // Set up paths
        _photosPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            "Camera");
        _videosPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
            "Camera");

        Directory.CreateDirectory(_photosPath);
        Directory.CreateDirectory(_videosPath);

        // Recording timer
        _recordingTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _recordingTimer.Tick += RecordingTimer_Tick;

        // Initialize camera
        InitializeCamera();

        // Load last photo thumbnail
        LoadLastPhotoThumbnail();
    }

    private async void InitializeCamera()
    {
        CameraStatus.Text = "Looking for cameras...";

        await Task.Delay(500); // Simulate initialization

        // Check for camera devices
        // In a real implementation, this would use Windows.Media.Capture or similar
        var hasCameras = CheckForCameras();

        if (hasCameras)
        {
            CameraStatus.Text = "Camera ready";
            // In real implementation: start camera preview
            // For demo, we'll show a placeholder
        }
        else
        {
            CameraStatus.Text = "No camera detected\nTap to take simulated photo";
        }
    }

    private bool CheckForCameras()
    {
        // In real implementation, enumerate video capture devices
        // For demo purposes, return false to show placeholder
        return false;
    }

    private void LoadLastPhotoThumbnail()
    {
        try
        {
            var files = Directory.GetFiles(_photosPath, "*.jpg")
                .OrderByDescending(f => File.GetCreationTime(f))
                .FirstOrDefault();

            if (files != null)
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(files);
                bitmap.DecodePixelWidth = 100;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                LastPhotoThumbnail.Source = bitmap;
            }
        }
        catch { }
    }

    #region Mode Selection

    private void PhotoMode_Click(object sender, RoutedEventArgs e)
    {
        SetMode(CameraMode.Photo);
    }

    private void VideoMode_Click(object sender, RoutedEventArgs e)
    {
        SetMode(CameraMode.Video);
    }

    private void PortraitMode_Click(object sender, RoutedEventArgs e)
    {
        SetMode(CameraMode.Portrait);
    }

    private void SetMode(CameraMode mode)
    {
        _currentMode = mode;

        // Update button styles
        PhotoModeBtn.Foreground = mode == CameraMode.Photo
            ? (Brush)FindResource("TextPrimaryBrush")
            : (Brush)FindResource("TextSecondaryBrush");
        PhotoModeBtn.FontWeight = mode == CameraMode.Photo ? FontWeights.SemiBold : FontWeights.Normal;

        VideoModeBtn.Foreground = mode == CameraMode.Video
            ? (Brush)FindResource("TextPrimaryBrush")
            : (Brush)FindResource("TextSecondaryBrush");
        VideoModeBtn.FontWeight = mode == CameraMode.Video ? FontWeights.SemiBold : FontWeights.Normal;

        PortraitModeBtn.Foreground = mode == CameraMode.Portrait
            ? (Brush)FindResource("TextPrimaryBrush")
            : (Brush)FindResource("TextSecondaryBrush");
        PortraitModeBtn.FontWeight = mode == CameraMode.Portrait ? FontWeights.SemiBold : FontWeights.Normal;

        // Update capture button style
        if (mode == CameraMode.Video)
        {
            CaptureButton.Style = (Style)FindResource("RecordButtonStyle");
        }
        else
        {
            CaptureButton.Style = (Style)FindResource("CaptureButtonStyle");
        }
    }

    #endregion

    #region Capture

    private async void Capture_Click(object sender, RoutedEventArgs e)
    {
        if (_currentMode == CameraMode.Video)
        {
            ToggleRecording();
        }
        else
        {
            await CapturePhoto();
        }
    }

    private async Task CapturePhoto()
    {
        // Check for timer
        var timerSeconds = GetTimerSeconds();

        if (timerSeconds > 0)
        {
            await RunCountdown(timerSeconds);
        }

        // Flash effect
        await PlayFlashEffect();

        // Save photo (simulated)
        var filename = $"IMG_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
        var filepath = Path.Combine(_photosPath, filename);

        // In real implementation, capture from camera
        // For demo, create a placeholder image
        CreatePlaceholderPhoto(filepath);

        // Update thumbnail
        LoadLastPhotoThumbnail();

        // Show confirmation
        CameraStatus.Text = $"Photo saved: {filename}";
        await Task.Delay(2000);
        CameraStatus.Text = "Camera ready";
    }

    private void CreatePlaceholderPhoto(string filepath)
    {
        try
        {
            // Create a simple placeholder image
            var visual = new DrawingVisual();
            using (var context = visual.RenderOpen())
            {
                context.DrawRectangle(
                    new LinearGradientBrush(
                        Color.FromRgb(26, 26, 46),
                        Color.FromRgb(22, 33, 62),
                        90),
                    null,
                    new Rect(0, 0, 1920, 1080));

                var formattedText = new FormattedText(
                    $"Photo captured\n{DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Segoe UI"),
                    48,
                    Brushes.White,
                    96);

                context.DrawText(formattedText,
                    new Point(960 - formattedText.Width / 2, 540 - formattedText.Height / 2));
            }

            var bitmap = new RenderTargetBitmap(1920, 1080, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(visual);

            var encoder = new JpegBitmapEncoder { QualityLevel = 90 };
            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            using var stream = File.Create(filepath);
            encoder.Save(stream);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error creating photo: {ex.Message}");
        }
    }

    private void ToggleRecording()
    {
        if (_isRecording)
        {
            StopRecording();
        }
        else
        {
            StartRecording();
        }
    }

    private void StartRecording()
    {
        _isRecording = true;
        _recordingDuration = TimeSpan.Zero;

        RecordingIndicator.Visibility = Visibility.Visible;
        RecordingTime.Text = "00:00";
        _recordingTimer.Start();

        CameraStatus.Text = "Recording...";
    }

    private void StopRecording()
    {
        _isRecording = false;
        _recordingTimer.Stop();

        RecordingIndicator.Visibility = Visibility.Collapsed;

        // Save video (simulated)
        var filename = $"VID_{DateTime.Now:yyyyMMdd_HHmmss}.mp4";
        var filepath = Path.Combine(_videosPath, filename);

        // In real implementation, stop recording and save
        CameraStatus.Text = $"Video saved: {filename} ({_recordingDuration:mm\\:ss})";
    }

    private void RecordingTimer_Tick(object? sender, EventArgs e)
    {
        _recordingDuration = _recordingDuration.Add(TimeSpan.FromSeconds(1));
        RecordingTime.Text = _recordingDuration.ToString(@"mm\:ss");
    }

    private int GetTimerSeconds()
    {
        if (Timer3s.IsChecked == true) return 3;
        if (Timer10s.IsChecked == true) return 10;
        return 0;
    }

    private async Task RunCountdown(int seconds)
    {
        CountdownOverlay.Visibility = Visibility.Visible;

        for (int i = seconds; i > 0; i--)
        {
            CountdownText.Text = i.ToString();
            await Task.Delay(1000);
        }

        CountdownOverlay.Visibility = Visibility.Collapsed;
    }

    private async Task PlayFlashEffect()
    {
        FlashOverlay.Opacity = 1;
        await Task.Delay(100);

        var animation = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200));
        FlashOverlay.BeginAnimation(OpacityProperty, animation);
    }

    #endregion

    #region Controls

    private void Flash_Click(object sender, RoutedEventArgs e)
    {
        _flashMode = _flashMode switch
        {
            FlashMode.Auto => FlashMode.On,
            FlashMode.On => FlashMode.Off,
            FlashMode.Off => FlashMode.Auto,
            _ => FlashMode.Auto
        };

        FlashButton.Content = _flashMode switch
        {
            FlashMode.Auto => "⚡A",
            FlashMode.On => "⚡",
            FlashMode.Off => "⚡✕",
            _ => "⚡"
        };

        FlashButton.Foreground = _flashMode == FlashMode.Off
            ? (Brush)FindResource("TextSecondaryBrush")
            : (Brush)FindResource("TextPrimaryBrush");
    }

    private void SwitchCamera_Click(object sender, RoutedEventArgs e)
    {
        _isFrontCamera = !_isFrontCamera;
        CameraStatus.Text = _isFrontCamera ? "Front camera" : "Rear camera";

        // In real implementation, switch camera source
        // Animation effect
        var scaleTransform = new ScaleTransform(1, 1);
        CameraPlaceholder.RenderTransform = scaleTransform;
        CameraPlaceholder.RenderTransformOrigin = new Point(0.5, 0.5);

        var animation = new DoubleAnimation(1, -1, TimeSpan.FromMilliseconds(150));
        animation.AutoReverse = true;
        scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
    }

    private void Gallery_Click(object sender, RoutedEventArgs e)
    {
        // Open gallery/photos app
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = _photosPath,
                UseShellExecute = true
            });
        }
        catch { }
    }

    private void GridLines_Changed(object sender, RoutedEventArgs e)
    {
        GridLines.Visibility = GridLinesToggle.IsChecked == true
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void CloseSettings_Click(object sender, RoutedEventArgs e)
    {
        SettingsPanel.Visibility = Visibility.Collapsed;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        if (_isRecording)
        {
            StopRecording();
        }
        Close();
    }

    #endregion
}
