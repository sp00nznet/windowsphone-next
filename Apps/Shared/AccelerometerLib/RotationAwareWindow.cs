using System;
using System.Windows;
using System.Windows.Media;

namespace WindowsPhoneNext.AccelerometerLib
{
    /// <summary>
    /// Base class for windows that need to respond to screen rotation.
    /// Handles window resizing and provides hooks for custom rotation handling.
    /// </summary>
    public class RotationAwareWindow : Window
    {
        // Portrait dimensions: 720x1560
        // Landscape dimensions: 1560x720
        public const int PortraitWidth = 720;
        public const int PortraitHeight = 1560;
        public const int LandscapeWidth = 1560;
        public const int LandscapeHeight = 720;

        private bool _isSubscribed = false;

        public RotationAwareWindow()
        {
            // Default to portrait
            Width = PortraitWidth;
            Height = PortraitHeight;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;

            Loaded += OnWindowLoaded;
            Closing += OnWindowClosing;
        }

        /// <summary>
        /// Whether the window is currently in landscape orientation
        /// </summary>
        public bool IsLandscape => OrientationService.Instance.IsLandscape;

        /// <summary>
        /// Whether the window is currently in portrait orientation
        /// </summary>
        public bool IsPortrait => OrientationService.Instance.IsPortrait;

        /// <summary>
        /// Current screen orientation
        /// </summary>
        public ScreenOrientation CurrentOrientation => OrientationService.Instance.CurrentOrientation;

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            // Subscribe to orientation changes
            if (!_isSubscribed)
            {
                OrientationService.Instance.OrientationChanged += OnOrientationChanged;
                OrientationService.Instance.StartAsClient();
                _isSubscribed = true;
            }

            // Apply current orientation
            ApplyOrientation(OrientationService.Instance.CurrentOrientation);
        }

        private void OnWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_isSubscribed)
            {
                OrientationService.Instance.OrientationChanged -= OnOrientationChanged;
                _isSubscribed = false;
            }
        }

        private void OnOrientationChanged(object? sender, OrientationChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                ApplyOrientation(e.NewOrientation);
            });
        }

        /// <summary>
        /// Apply the given orientation to the window
        /// </summary>
        protected virtual void ApplyOrientation(ScreenOrientation orientation)
        {
            bool isLandscape = orientation == ScreenOrientation.LandscapeLeft ||
                              orientation == ScreenOrientation.LandscapeRight;

            // Resize window
            if (isLandscape)
            {
                Width = LandscapeWidth;
                Height = LandscapeHeight;
            }
            else
            {
                Width = PortraitWidth;
                Height = PortraitHeight;
            }

            // Call virtual method for custom handling
            OnOrientationApplied(orientation, isLandscape);
        }

        /// <summary>
        /// Override this method to handle orientation changes in derived classes
        /// </summary>
        /// <param name="orientation">The new orientation</param>
        /// <param name="isLandscape">True if landscape, false if portrait</param>
        protected virtual void OnOrientationApplied(ScreenOrientation orientation, bool isLandscape)
        {
            // Override in derived classes
        }

        /// <summary>
        /// Helper method to rotate a FrameworkElement based on orientation
        /// </summary>
        protected void RotateElement(FrameworkElement element, ScreenOrientation orientation)
        {
            double angle = orientation switch
            {
                ScreenOrientation.Portrait => 0,
                ScreenOrientation.LandscapeRight => 90,
                ScreenOrientation.PortraitFlipped => 180,
                ScreenOrientation.LandscapeLeft => 270,
                _ => 0
            };

            element.RenderTransformOrigin = new Point(0.5, 0.5);
            element.RenderTransform = new RotateTransform(angle);
        }
    }
}
