using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WindowsPhoneFiles;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<FileItem> _files = new();
    private readonly Stack<string> _navigationHistory = new();
    private string _currentPath = "";
    private FileItem? _clipboardItem;
    private bool _isCut;
    private FileItem? _contextMenuItem;

    public MainWindow()
    {
        InitializeComponent();
        FileList.ItemsSource = _files;
        ShowQuickAccess();
        UpdateStorageInfo();
    }

    private void ShowQuickAccess()
    {
        QuickAccessPanel.Visibility = Visibility.Visible;
        FileListPanel.Visibility = Visibility.Collapsed;
        PathText.Text = "Quick Access";
        BackButton.IsEnabled = false;
        _currentPath = "";
    }

    private void NavigateTo(string path)
    {
        if (!string.IsNullOrEmpty(_currentPath))
        {
            _navigationHistory.Push(_currentPath);
        }

        _currentPath = path;
        LoadDirectory(path);
    }

    private void LoadDirectory(string path)
    {
        _files.Clear();
        QuickAccessPanel.Visibility = Visibility.Collapsed;
        FileListPanel.Visibility = Visibility.Visible;
        BackButton.IsEnabled = true;

        try
        {
            PathText.Text = path;

            var dirInfo = new DirectoryInfo(path);

            // Add directories first
            foreach (var dir in dirInfo.GetDirectories().OrderBy(d => d.Name))
            {
                try
                {
                    var itemCount = dir.GetFileSystemInfos().Length;
                    _files.Add(new FileItem
                    {
                        Name = dir.Name,
                        FullPath = dir.FullName,
                        IsDirectory = true,
                        Icon = "📁",
                        Info = $"{itemCount} items",
                        LastModified = dir.LastWriteTime
                    });
                }
                catch
                {
                    _files.Add(new FileItem
                    {
                        Name = dir.Name,
                        FullPath = dir.FullName,
                        IsDirectory = true,
                        Icon = "📁",
                        Info = "Access denied",
                        LastModified = dir.LastWriteTime
                    });
                }
            }

            // Add files
            foreach (var file in dirInfo.GetFiles().OrderBy(f => f.Name))
            {
                _files.Add(new FileItem
                {
                    Name = file.Name,
                    FullPath = file.FullName,
                    IsDirectory = false,
                    Icon = GetFileIcon(file.Extension),
                    Info = FormatFileSize(file.Length),
                    Size = file.Length,
                    LastModified = file.LastWriteTime
                });
            }

            EmptyState.Visibility = _files.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        catch (UnauthorizedAccessException)
        {
            MessageBox.Show("Access to this folder is denied.", "Access Denied",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            GoBack();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading directory: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            GoBack();
        }
    }

    private void GoBack()
    {
        if (_navigationHistory.Count > 0)
        {
            _currentPath = _navigationHistory.Pop();
            LoadDirectory(_currentPath);
            if (_navigationHistory.Count == 0)
            {
                BackButton.IsEnabled = true; // Still can go to quick access
            }
        }
        else
        {
            ShowQuickAccess();
        }
    }

    private string GetFileIcon(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".txt" or ".md" or ".log" => "📄",
            ".pdf" => "📕",
            ".doc" or ".docx" => "📘",
            ".xls" or ".xlsx" => "📗",
            ".ppt" or ".pptx" => "📙",
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp" => "🖼️",
            ".mp3" or ".wav" or ".flac" or ".aac" or ".ogg" => "🎵",
            ".mp4" or ".avi" or ".mkv" or ".mov" or ".wmv" => "🎬",
            ".zip" or ".rar" or ".7z" or ".tar" or ".gz" => "📦",
            ".exe" or ".msi" => "⚙️",
            ".dll" => "🔧",
            ".cs" or ".js" or ".py" or ".java" or ".cpp" or ".h" => "💻",
            ".html" or ".htm" or ".css" => "🌐",
            ".json" or ".xml" or ".yaml" or ".yml" => "📋",
            ".apk" => "📱",
            _ => "📄"
        };
    }

    private string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = bytes;

        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:0.##} {sizes[order]}";
    }

    private void UpdateStorageInfo()
    {
        try
        {
            var drive = DriveInfo.GetDrives().FirstOrDefault(d =>
                d.IsReady && d.DriveType == DriveType.Fixed);

            if (drive != null)
            {
                var used = drive.TotalSize - drive.AvailableFreeSpace;
                var total = drive.TotalSize;
                InternalStorageInfo.Text = $"{FormatFileSize(used)} / {FormatFileSize(total)}";
            }

            // Check for removable drives (SD Card)
            var removable = DriveInfo.GetDrives().FirstOrDefault(d =>
                d.IsReady && d.DriveType == DriveType.Removable);

            if (removable != null)
            {
                SDCardButton.Visibility = Visibility.Visible;
                var used = removable.TotalSize - removable.AvailableFreeSpace;
                var total = removable.TotalSize;
                SDCardInfo.Text = $"{FormatFileSize(used)} / {FormatFileSize(total)}";
            }
        }
        catch { }
    }

    #region Event Handlers

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        GoBack();
    }

    private void HomeButton_Click(object sender, RoutedEventArgs e)
    {
        _navigationHistory.Clear();
        ShowQuickAccess();
    }

    private void MenuButton_Click(object sender, RoutedEventArgs e)
    {
        // Toggle sort/view options - simplified for now
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var searchText = SearchBox.Text.ToLowerInvariant();

        if (string.IsNullOrEmpty(_currentPath))
            return;

        if (string.IsNullOrWhiteSpace(searchText))
        {
            LoadDirectory(_currentPath);
            return;
        }

        var filtered = _files.Where(f =>
            f.Name.ToLowerInvariant().Contains(searchText)).ToList();

        _files.Clear();
        foreach (var file in filtered)
        {
            _files.Add(file);
        }

        EmptyState.Visibility = _files.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void QuickAccess_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string location)
        {
            string path = location switch
            {
                "Documents" => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Downloads" => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
                "Pictures" => Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                "Music" => Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
                "Videos" => Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
                "Internal" => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "SDCard" => DriveInfo.GetDrives().FirstOrDefault(d => d.DriveType == DriveType.Removable)?.RootDirectory.FullName ?? "",
                _ => ""
            };

            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
            {
                NavigateTo(path);
            }
        }
    }

    private void FileItem_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.Tag is FileItem item)
        {
            if (item.IsDirectory)
            {
                NavigateTo(item.FullPath);
            }
            else
            {
                // Open file with default application
                try
                {
                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = item.FullPath,
                        UseShellExecute = true
                    };
                    System.Diagnostics.Process.Start(psi);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Cannot open file: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    private void FileMenu_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is FileItem item)
        {
            e.Handled = true;
            _contextMenuItem = item;
            ContextMenu.Visibility = Visibility.Visible;
        }
    }

    private void NewFolder_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_currentPath))
        {
            MessageBox.Show("Navigate to a folder first.", "Info",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new InputDialog("New Folder", "Enter folder name:");
        if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.InputText))
        {
            try
            {
                var newPath = Path.Combine(_currentPath, dialog.InputText);
                Directory.CreateDirectory(newPath);
                LoadDirectory(_currentPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating folder: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void Paste_Click(object sender, RoutedEventArgs e)
    {
        if (_clipboardItem == null || string.IsNullOrEmpty(_currentPath))
            return;

        try
        {
            var destPath = Path.Combine(_currentPath, _clipboardItem.Name);

            if (_clipboardItem.IsDirectory)
            {
                if (_isCut)
                {
                    Directory.Move(_clipboardItem.FullPath, destPath);
                }
                else
                {
                    CopyDirectory(_clipboardItem.FullPath, destPath);
                }
            }
            else
            {
                if (_isCut)
                {
                    File.Move(_clipboardItem.FullPath, destPath);
                }
                else
                {
                    File.Copy(_clipboardItem.FullPath, destPath);
                }
            }

            if (_isCut)
            {
                _clipboardItem = null;
                PasteButton.IsEnabled = false;
            }

            LoadDirectory(_currentPath);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Paste Failed",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CopyDirectory(string source, string dest)
    {
        Directory.CreateDirectory(dest);

        foreach (var file in Directory.GetFiles(source))
        {
            File.Copy(file, Path.Combine(dest, Path.GetFileName(file)));
        }

        foreach (var dir in Directory.GetDirectories(source))
        {
            CopyDirectory(dir, Path.Combine(dest, Path.GetFileName(dir)));
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    #endregion

    #region Context Menu

    private void ContextMenu_Copy(object sender, RoutedEventArgs e)
    {
        if (_contextMenuItem != null)
        {
            _clipboardItem = _contextMenuItem;
            _isCut = false;
            PasteButton.IsEnabled = true;
        }
        HideContextMenu();
    }

    private void ContextMenu_Cut(object sender, RoutedEventArgs e)
    {
        if (_contextMenuItem != null)
        {
            _clipboardItem = _contextMenuItem;
            _isCut = true;
            PasteButton.IsEnabled = true;
        }
        HideContextMenu();
    }

    private void ContextMenu_Rename(object sender, RoutedEventArgs e)
    {
        if (_contextMenuItem == null)
        {
            HideContextMenu();
            return;
        }

        var dialog = new InputDialog("Rename", "Enter new name:", _contextMenuItem.Name);
        if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.InputText))
        {
            try
            {
                var newPath = Path.Combine(Path.GetDirectoryName(_contextMenuItem.FullPath)!, dialog.InputText);

                if (_contextMenuItem.IsDirectory)
                {
                    Directory.Move(_contextMenuItem.FullPath, newPath);
                }
                else
                {
                    File.Move(_contextMenuItem.FullPath, newPath);
                }

                LoadDirectory(_currentPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error renaming: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        HideContextMenu();
    }

    private void ContextMenu_Delete(object sender, RoutedEventArgs e)
    {
        if (_contextMenuItem == null)
        {
            HideContextMenu();
            return;
        }

        var result = MessageBox.Show(
            $"Are you sure you want to delete '{_contextMenuItem.Name}'?",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                if (_contextMenuItem.IsDirectory)
                {
                    Directory.Delete(_contextMenuItem.FullPath, true);
                }
                else
                {
                    File.Delete(_contextMenuItem.FullPath);
                }

                LoadDirectory(_currentPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        HideContextMenu();
    }

    private void ContextMenu_Properties(object sender, RoutedEventArgs e)
    {
        if (_contextMenuItem == null)
        {
            HideContextMenu();
            return;
        }

        var info = _contextMenuItem.IsDirectory
            ? $"Type: Folder\nPath: {_contextMenuItem.FullPath}\nModified: {_contextMenuItem.LastModified}"
            : $"Type: File\nSize: {_contextMenuItem.Info}\nPath: {_contextMenuItem.FullPath}\nModified: {_contextMenuItem.LastModified}";

        MessageBox.Show(info, _contextMenuItem.Name, MessageBoxButton.OK, MessageBoxImage.Information);
        HideContextMenu();
    }

    private void ContextMenu_Cancel(object sender, RoutedEventArgs e)
    {
        HideContextMenu();
    }

    private void HideContextMenu()
    {
        ContextMenu.Visibility = Visibility.Collapsed;
        _contextMenuItem = null;
    }

    #endregion
}

public class FileItem
{
    public string Name { get; set; } = "";
    public string FullPath { get; set; } = "";
    public bool IsDirectory { get; set; }
    public string Icon { get; set; } = "📄";
    public string Info { get; set; } = "";
    public long Size { get; set; }
    public DateTime LastModified { get; set; }
}

/// <summary>
/// Simple input dialog for rename/new folder operations.
/// </summary>
public class InputDialog : Window
{
    private readonly TextBox _textBox;

    public string InputText => _textBox.Text;

    public InputDialog(string title, string prompt, string defaultValue = "")
    {
        Title = title;
        Width = 400;
        Height = 200;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new System.Windows.Media.SolidColorBrush(
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1A1A2E"));

        var grid = new Grid { Margin = new Thickness(20) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var label = new TextBlock
        {
            Text = prompt,
            Foreground = System.Windows.Media.Brushes.White,
            FontSize = 16,
            Margin = new Thickness(0, 0, 0, 12)
        };
        Grid.SetRow(label, 0);

        _textBox = new TextBox
        {
            Text = defaultValue,
            FontSize = 16,
            Padding = new Thickness(12, 10, 12, 10),
            Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#16213E")),
            Foreground = System.Windows.Media.Brushes.White,
            BorderBrush = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#374151")),
            Margin = new Thickness(0, 0, 0, 16)
        };
        Grid.SetRow(_textBox, 1);

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        Grid.SetRow(buttonPanel, 2);

        var cancelBtn = new Button
        {
            Content = "Cancel",
            Padding = new Thickness(20, 10, 20, 10),
            Margin = new Thickness(0, 0, 8, 0),
            Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#374151")),
            Foreground = System.Windows.Media.Brushes.White,
            BorderThickness = new Thickness(0)
        };
        cancelBtn.Click += (s, e) => { DialogResult = false; Close(); };

        var okBtn = new Button
        {
            Content = "OK",
            Padding = new Thickness(20, 10, 20, 10),
            Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F39C12")),
            Foreground = System.Windows.Media.Brushes.White,
            BorderThickness = new Thickness(0)
        };
        okBtn.Click += (s, e) => { DialogResult = true; Close(); };

        buttonPanel.Children.Add(cancelBtn);
        buttonPanel.Children.Add(okBtn);

        grid.Children.Add(label);
        grid.Children.Add(_textBox);
        grid.Children.Add(buttonPanel);

        Content = grid;
    }
}
