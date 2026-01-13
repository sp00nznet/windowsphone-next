using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace WindowsPhoneNext.Gallery;

public partial class MainWindow : Window
{
    private List<string> _imagePaths = new();
    private int _currentIndex = -1;
    private string _currentFolder = string.Empty;
    private readonly string _settingsPath;
    private GallerySettings _settings;

    private static readonly string[] SupportedExtensions =
        { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff", ".tif" };

    public MainWindow()
    {
        InitializeComponent();

        // Initialize settings path
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var settingsFolder = Path.Combine(appData, "WindowsPhoneNext");
        Directory.CreateDirectory(settingsFolder);
        _settingsPath = Path.Combine(settingsFolder, "gallery_settings.json");

        _settings = new GallerySettings();
        LoadSettings();
    }

    private void LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                _settings = JsonSerializer.Deserialize<GallerySettings>(json) ?? new GallerySettings();
            }
        }
        catch { }
    }

    private void SaveSettings()
    {
        try
        {
            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);
        }
        catch { }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // Check for configured gallery path in settings
        if (!string.IsNullOrEmpty(_settings.DefaultPath) && Directory.Exists(_settings.DefaultPath))
        {
            LoadFolder(_settings.DefaultPath);
        }
        else
        {
            // Show empty state - user needs to select a folder
            ShowEmptyState();
        }
    }

    private void ShowEmptyState()
    {
        NoImagesPanel.Visibility = Visibility.Visible;
        MainImage.Source = null;
        ImageCountText.Text = "No folder";
        TitleText.Text = "Gallery";
        ImageInfoOverlay.Visibility = Visibility.Collapsed;
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (SettingsOverlay.Visibility == Visibility.Visible)
        {
            if (e.Key == Key.Escape)
            {
                SettingsOverlay.Visibility = Visibility.Collapsed;
                e.Handled = true;
            }
            return;
        }

        switch (e.Key)
        {
            case Key.Left:
                NavigatePrev();
                e.Handled = true;
                break;
            case Key.Right:
                NavigateNext();
                e.Handled = true;
                break;
            case Key.Escape:
                Close();
                e.Handled = true;
                break;
            case Key.Home:
                if (_imagePaths.Count > 0)
                {
                    SelectImage(0);
                    e.Handled = true;
                }
                break;
            case Key.End:
                if (_imagePaths.Count > 0)
                {
                    SelectImage(_imagePaths.Count - 1);
                    e.Handled = true;
                }
                break;
        }
    }

    private void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select Image Folder"
        };

        if (dialog.ShowDialog() == true)
        {
            LoadFolder(dialog.FolderName);
        }
    }

    private void LoadFolder(string folderPath)
    {
        _currentFolder = folderPath;
        _imagePaths.Clear();
        ThumbnailPanel.Children.Clear();
        _currentIndex = -1;

        try
        {
            var files = Directory.GetFiles(folderPath)
                .Where(f => SupportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .OrderBy(f => f)
                .ToList();

            _imagePaths = files;

            if (_imagePaths.Count > 0)
            {
                NoImagesPanel.Visibility = Visibility.Collapsed;
                ImageCountText.Text = $"{_imagePaths.Count} images";
                TitleText.Text = Path.GetFileName(folderPath);

                // Create thumbnails
                for (int i = 0; i < _imagePaths.Count; i++)
                {
                    CreateThumbnail(i);
                }

                // Select first image
                SelectImage(0);
            }
            else
            {
                NoImagesPanel.Visibility = Visibility.Visible;
                MainImage.Source = null;
                ImageCountText.Text = "0 images";
                TitleText.Text = "Gallery";
                ImageInfoOverlay.Visibility = Visibility.Collapsed;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading folder: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CreateThumbnail(int index)
    {
        var imagePath = _imagePaths[index];

        var border = new Border
        {
            Width = 100,
            Height = 100,
            Margin = new Thickness(4),
            CornerRadius = new CornerRadius(8),
            Cursor = Cursors.Hand,
            Tag = index,
            ClipToBounds = true,
            Background = (SolidColorBrush)FindResource("CardBrush")
        };

        var image = new Image
        {
            Stretch = Stretch.UniformToFill,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        // Load thumbnail asynchronously
        Task.Run(() =>
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imagePath);
                bitmap.DecodePixelWidth = 150; // Thumbnail size
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();

                Dispatcher.Invoke(() =>
                {
                    image.Source = bitmap;
                });
            }
            catch { }
        });

        border.Child = image;
        border.MouseLeftButtonUp += Thumbnail_Click;
        border.MouseEnter += (s, e) =>
        {
            if ((int)border.Tag != _currentIndex)
            {
                border.BorderBrush = (SolidColorBrush)FindResource("TextSecondaryBrush");
                border.BorderThickness = new Thickness(2);
            }
        };
        border.MouseLeave += (s, e) =>
        {
            if ((int)border.Tag != _currentIndex)
            {
                border.BorderBrush = null;
                border.BorderThickness = new Thickness(0);
            }
        };

        ThumbnailPanel.Children.Add(border);
    }

    private void Thumbnail_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.Tag is int index)
        {
            SelectImage(index);
        }
    }

    private void SelectImage(int index)
    {
        if (index < 0 || index >= _imagePaths.Count) return;

        // Update thumbnail selection visual
        if (_currentIndex >= 0 && _currentIndex < ThumbnailPanel.Children.Count)
        {
            var oldThumb = ThumbnailPanel.Children[_currentIndex] as Border;
            if (oldThumb != null)
            {
                oldThumb.BorderBrush = null;
                oldThumb.BorderThickness = new Thickness(0);
            }
        }

        _currentIndex = index;

        if (_currentIndex < ThumbnailPanel.Children.Count)
        {
            var newThumb = ThumbnailPanel.Children[_currentIndex] as Border;
            if (newThumb != null)
            {
                newThumb.BorderBrush = (SolidColorBrush)FindResource("PrimaryBrush");
                newThumb.BorderThickness = new Thickness(3);

                // Scroll to thumbnail
                var scrollViewer = ThumbnailScroller;
                var thumbLeft = index * 108; // 100 width + 8 margin
                var viewportWidth = scrollViewer.ViewportWidth;
                var currentOffset = scrollViewer.HorizontalOffset;

                if (thumbLeft < currentOffset)
                {
                    scrollViewer.ScrollToHorizontalOffset(thumbLeft);
                }
                else if (thumbLeft + 108 > currentOffset + viewportWidth)
                {
                    scrollViewer.ScrollToHorizontalOffset(thumbLeft + 108 - viewportWidth);
                }
            }
        }

        // Load full image
        LoadFullImage(_imagePaths[index]);

        // Update navigation buttons
        PrevButton.Visibility = _currentIndex > 0 ? Visibility.Visible : Visibility.Collapsed;
        NextButton.Visibility = _currentIndex < _imagePaths.Count - 1 ? Visibility.Visible : Visibility.Collapsed;

        // Show info overlay
        ImageInfoOverlay.Visibility = Visibility.Visible;
        ImageIndex.Text = $"{_currentIndex + 1} / {_imagePaths.Count}";
    }

    private void LoadFullImage(string imagePath)
    {
        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(imagePath);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();

            MainImage.Source = bitmap;

            // Update info
            var fileInfo = new FileInfo(imagePath);
            ImageFileName.Text = fileInfo.Name;
            ImageDetails.Text = $"{bitmap.PixelWidth} x {bitmap.PixelHeight}  |  {FormatFileSize(fileInfo.Length)}";
        }
        catch (Exception ex)
        {
            MainImage.Source = null;
            ImageFileName.Text = Path.GetFileName(imagePath);
            ImageDetails.Text = $"Error: {ex.Message}";
        }
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:0.##} {sizes[order]}";
    }

    private void NavigatePrev()
    {
        if (_currentIndex > 0)
        {
            SelectImage(_currentIndex - 1);
        }
    }

    private void NavigateNext()
    {
        if (_currentIndex < _imagePaths.Count - 1)
        {
            SelectImage(_currentIndex + 1);
        }
    }

    private void PrevImage_Click(object sender, RoutedEventArgs e)
    {
        NavigatePrev();
    }

    private void NextImage_Click(object sender, RoutedEventArgs e)
    {
        NavigateNext();
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        DefaultPathBox.Text = string.IsNullOrEmpty(_settings.DefaultPath)
            ? "(Not set)"
            : _settings.DefaultPath;
        SettingsOverlay.Visibility = Visibility.Visible;
    }

    private void CloseSettings_Click(object sender, RoutedEventArgs e)
    {
        SettingsOverlay.Visibility = Visibility.Collapsed;
    }

    private void BrowseDefaultPath_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select Default Image Folder"
        };

        if (dialog.ShowDialog() == true)
        {
            _settings.DefaultPath = dialog.FolderName;
            SaveSettings();
            DefaultPathBox.Text = _settings.DefaultPath;
        }
    }

    private void ClearDefaultPath_Click(object sender, RoutedEventArgs e)
    {
        _settings.DefaultPath = "";
        SaveSettings();
        DefaultPathBox.Text = "(Not set)";
    }
}

public class GallerySettings
{
    public string DefaultPath { get; set; } = "";
}
