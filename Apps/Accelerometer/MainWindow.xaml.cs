using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WindowsPhoneNext.AccelerometerLib;

namespace WindowsPhoneNext.Accelerometer
{
    public partial class MainWindow : Window
    {
        private AccelerometerController _accelerometer;
        private const double BAR_MAX_WIDTH = 200;  // Max width for the bar visualization

        public MainWindow()
        {
            InitializeComponent();
            _accelerometer = new AccelerometerController();
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Connect to accelerometer
            var connected = await _accelerometer.AutoConnectAsync();

            if (connected)
            {
                if (_accelerometer.IsDemoMode)
                {
                    StatusText.Text = "Demo Mode";
                    StatusText.Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD4));
                    StatusIndicator.Fill = new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD4));
                    DemoModeLabel.Visibility = Visibility.Visible;
                }
                else
                {
                    StatusText.Text = "Connected";
                    StatusText.Foreground = new SolidColorBrush(Color.FromRgb(0x44, 0xFF, 0x44));
                    StatusIndicator.Fill = new SolidColorBrush(Color.FromRgb(0x44, 0xFF, 0x44));
                    DemoModeLabel.Visibility = Visibility.Collapsed;
                }

                // Subscribe to events
                _accelerometer.AccelerationDataReceived += OnAccelerationDataReceived;
                _accelerometer.OrientationChanged += OnOrientationChanged;

                // Start reading
                _accelerometer.StartReading(50);  // 20Hz updates

                // Also start the orientation service as server
                await OrientationService.Instance.StartAsServerAsync();
            }
            else
            {
                StatusText.Text = "Not Found";
                StatusText.Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x44, 0x44));
            }

            // Update initial orientation display
            UpdateOrientationDisplay(_accelerometer.CurrentOrientation);
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _accelerometer.StopReading();
            _accelerometer.Dispose();
        }

        private void OnAccelerationDataReceived(object? sender, AccelerationEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                var data = e.Data;

                // Update text values
                XValue.Text = $"{data.X:F2}";
                YValue.Text = $"{data.Y:F2}";
                ZValue.Text = $"{data.Z:F2}";
                MagnitudeValue.Text = $"{data.Magnitude:F2} m/s²";

                // Update bar visualizations
                // Scale: -15 to +15 m/s² maps to bar width
                UpdateAxisBar(XBar, data.X);
                UpdateAxisBar(YBar, data.Y);
                UpdateAxisBar(ZBar, data.Z);
            });
        }

        private void UpdateAxisBar(System.Windows.Shapes.Rectangle bar, double value)
        {
            // Clamp value to -15 to 15
            value = Math.Clamp(value, -15, 15);

            // Calculate bar position and width
            // Center is at BAR_MAX_WIDTH/2, positive goes right, negative goes left
            double center = BAR_MAX_WIDTH / 2;
            double barWidth = Math.Abs(value) / 15.0 * center;

            if (value >= 0)
            {
                Canvas.SetLeft(bar, center);
                bar.Width = barWidth;
            }
            else
            {
                Canvas.SetLeft(bar, center - barWidth);
                bar.Width = barWidth;
            }
        }

        private void OnOrientationChanged(object? sender, OrientationChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateOrientationDisplay(e.NewOrientation);
            });
        }

        private void UpdateOrientationDisplay(ScreenOrientation orientation)
        {
            // Update text
            OrientationText.Text = orientation switch
            {
                ScreenOrientation.Portrait => "Portrait",
                ScreenOrientation.PortraitFlipped => "Portrait (Flipped)",
                ScreenOrientation.LandscapeLeft => "Landscape Left",
                ScreenOrientation.LandscapeRight => "Landscape Right",
                ScreenOrientation.FaceUp => "Face Up",
                ScreenOrientation.FaceDown => "Face Down",
                _ => "Unknown"
            };

            // Update icon and rotation
            double angle = orientation switch
            {
                ScreenOrientation.Portrait => 0,
                ScreenOrientation.LandscapeRight => 90,
                ScreenOrientation.PortraitFlipped => 180,
                ScreenOrientation.LandscapeLeft => -90,
                _ => 0
            };

            // Update phone visualization rotation
            PhoneRotation.Angle = angle;
            ScreenRotation.Angle = angle;
            NotchRotation.Angle = angle;

            // Adjust the center of rotation for phone outline
            PhoneRotation.CenterX = 40;
            PhoneRotation.CenterY = 80;

            // Update icon
            OrientationIcon.Text = orientation switch
            {
                ScreenOrientation.Portrait or ScreenOrientation.PortraitFlipped => "\U0001F4F1",
                ScreenOrientation.LandscapeLeft or ScreenOrientation.LandscapeRight => "\U0001F4F2",
                ScreenOrientation.FaceUp => "\U0001F4F3",
                ScreenOrientation.FaceDown => "\U0001F4F4",
                _ => "\U0001F4F1"
            };

            // Apply rotation transform to icon
            OrientationIcon.RenderTransform = new RotateTransform(angle, 32, 32);
        }

        private void OrientationButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string orientationStr)
            {
                if (Enum.TryParse<ScreenOrientation>(orientationStr, out var orientation))
                {
                    // Use the orientation service to broadcast change
                    OrientationService.Instance.SetOrientation(orientation);
                    UpdateOrientationDisplay(orientation);
                }
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
