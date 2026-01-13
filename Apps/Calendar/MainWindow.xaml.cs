using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WindowsPhoneNext.Calendar;

public partial class MainWindow : Window
{
    private DateTime _currentDate;
    private DateTime _selectedDate;
    private DateTime _viewDate;
    private HashSet<DateTime> _markedDates = new();
    private string _dataFilePath;

    private enum ViewMode { Year, Month, Day }
    private ViewMode _currentView = ViewMode.Month;

    public MainWindow()
    {
        InitializeComponent();

        _currentDate = DateTime.Today;
        _selectedDate = _currentDate;
        _viewDate = _currentDate;

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var calendarFolder = System.IO.Path.Combine(appData, "WindowsPhoneNext", "Calendar");
        Directory.CreateDirectory(calendarFolder);
        _dataFilePath = System.IO.Path.Combine(calendarFolder, "marked_dates.json");
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        LoadMarkedDates();
        UpdateView();
    }

    private void LoadMarkedDates()
    {
        try
        {
            if (File.Exists(_dataFilePath))
            {
                var json = File.ReadAllText(_dataFilePath);
                var dates = JsonSerializer.Deserialize<List<string>>(json);
                _markedDates = dates?.Select(d => DateTime.Parse(d)).ToHashSet() ?? new();
            }
        }
        catch { }
    }

    private void SaveMarkedDates()
    {
        try
        {
            var dates = _markedDates.Select(d => d.ToString("yyyy-MM-dd")).ToList();
            var json = JsonSerializer.Serialize(dates);
            File.WriteAllText(_dataFilePath, json);
        }
        catch { }
    }

    private void UpdateView()
    {
        switch (_currentView)
        {
            case ViewMode.Year:
                YearView.Visibility = Visibility.Visible;
                MonthView.Visibility = Visibility.Collapsed;
                DayView.Visibility = Visibility.Collapsed;
                TitleText.Text = _viewDate.Year.ToString();
                BuildYearView();
                break;
            case ViewMode.Month:
                YearView.Visibility = Visibility.Collapsed;
                MonthView.Visibility = Visibility.Visible;
                DayView.Visibility = Visibility.Collapsed;
                TitleText.Text = _viewDate.ToString("MMMM yyyy");
                MonthYearText.Text = _viewDate.ToString("MMMM yyyy");
                BuildMonthView();
                break;
            case ViewMode.Day:
                YearView.Visibility = Visibility.Collapsed;
                MonthView.Visibility = Visibility.Collapsed;
                DayView.Visibility = Visibility.Visible;
                TitleText.Text = _selectedDate.ToString("MMM d, yyyy");
                DayHeaderText.Text = _selectedDate.ToString("dddd, MMMM d");
                BuildDayView();
                break;
        }
    }

    private void BuildYearView()
    {
        YearMonthGrid.Children.Clear();
        YearText.Text = _viewDate.Year.ToString();

        var monthNames = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun",
                                  "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

        for (int i = 0; i < 12; i++)
        {
            var monthDate = new DateTime(_viewDate.Year, i + 1, 1);
            var btn = new Button
            {
                Style = (Style)FindResource("MonthCellStyle"),
                Margin = new Thickness(4),
                Tag = monthDate
            };

            var stack = new StackPanel();
            stack.Children.Add(new TextBlock
            {
                Text = monthNames[i],
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = (SolidColorBrush)FindResource("TextPrimaryBrush")
            });

            // Show marked dates count for this month
            var markedCount = _markedDates.Count(d => d.Year == monthDate.Year && d.Month == monthDate.Month);
            if (markedCount > 0)
            {
                stack.Children.Add(new TextBlock
                {
                    Text = $"{markedCount} marked",
                    FontSize = 10,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Foreground = (SolidColorBrush)FindResource("MarkedBrush")
                });
            }

            // Highlight current month
            if (monthDate.Year == _currentDate.Year && monthDate.Month == _currentDate.Month)
            {
                btn.Background = (SolidColorBrush)FindResource("PrimaryBrush");
            }

            btn.Content = stack;
            btn.Click += MonthCell_Click;
            YearMonthGrid.Children.Add(btn);
        }
    }

    private void BuildMonthView()
    {
        MonthDayGrid.Children.Clear();

        var firstDay = new DateTime(_viewDate.Year, _viewDate.Month, 1);
        var daysInMonth = DateTime.DaysInMonth(_viewDate.Year, _viewDate.Month);
        var startDayOfWeek = (int)firstDay.DayOfWeek;

        // Previous month's days
        var prevMonth = firstDay.AddMonths(-1);
        var daysInPrevMonth = DateTime.DaysInMonth(prevMonth.Year, prevMonth.Month);

        for (int i = 0; i < startDayOfWeek; i++)
        {
            var dayNum = daysInPrevMonth - startDayOfWeek + i + 1;
            var dayDate = new DateTime(prevMonth.Year, prevMonth.Month, dayNum);
            AddDayCell(dayDate, isCurrentMonth: false);
        }

        // Current month's days
        for (int i = 1; i <= daysInMonth; i++)
        {
            var dayDate = new DateTime(_viewDate.Year, _viewDate.Month, i);
            AddDayCell(dayDate, isCurrentMonth: true);
        }

        // Next month's days
        var totalCells = MonthDayGrid.Children.Count;
        var remaining = 42 - totalCells; // 6 rows * 7 columns
        var nextMonth = firstDay.AddMonths(1);

        for (int i = 1; i <= remaining; i++)
        {
            var dayDate = new DateTime(nextMonth.Year, nextMonth.Month, i);
            AddDayCell(dayDate, isCurrentMonth: false);
        }
    }

    private void AddDayCell(DateTime date, bool isCurrentMonth)
    {
        var btn = new Button
        {
            Style = (Style)FindResource("DayCellStyle"),
            Tag = date,
            Margin = new Thickness(2)
        };

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var dayText = new TextBlock
        {
            Text = date.Day.ToString(),
            FontSize = 16,
            HorizontalAlignment = HorizontalAlignment.Center,
            Foreground = isCurrentMonth
                ? (SolidColorBrush)FindResource("TextPrimaryBrush")
                : (SolidColorBrush)FindResource("TextSecondaryBrush")
        };

        // Today highlight
        if (date.Date == _currentDate.Date)
        {
            btn.Background = (SolidColorBrush)FindResource("PrimaryBrush");
            dayText.FontWeight = FontWeights.Bold;
        }

        // Selected highlight
        if (date.Date == _selectedDate.Date && date.Date != _currentDate.Date)
        {
            btn.Background = (SolidColorBrush)FindResource("CardBrush");
        }

        grid.Children.Add(dayText);

        // Marked indicator
        if (_markedDates.Contains(date.Date))
        {
            var marker = new Ellipse
            {
                Width = 6,
                Height = 6,
                Fill = (SolidColorBrush)FindResource("MarkedBrush"),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 2, 0, 0)
            };
            Grid.SetRow(marker, 1);
            grid.Children.Add(marker);
        }

        btn.Content = grid;
        btn.Click += DayCell_Click;
        MonthDayGrid.Children.Add(btn);
    }

    private void BuildDayView()
    {
        HourSlotsPanel.Children.Clear();

        // Update mark button
        var isMarked = _markedDates.Contains(_selectedDate.Date);
        MarkDayText.Text = isMarked ? "\u2605 Marked as Important" : "\u2606 Mark as Important";
        MarkDayButton.Background = isMarked
            ? (SolidColorBrush)FindResource("MarkedBrush")
            : (SolidColorBrush)FindResource("CardBrush");

        // Build 24 hour slots
        for (int hour = 0; hour < 24; hour++)
        {
            var slot = new Border
            {
                Style = (Style)FindResource("HourSlotStyle"),
                Height = 50
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var timeText = new TextBlock
            {
                Text = DateTime.Today.AddHours(hour).ToString("h tt"),
                FontSize = 14,
                Foreground = (SolidColorBrush)FindResource("TextSecondaryBrush"),
                VerticalAlignment = VerticalAlignment.Center
            };

            var eventArea = new Border
            {
                Background = Brushes.Transparent,
                CornerRadius = new CornerRadius(4)
            };

            // Highlight current hour if viewing today
            if (_selectedDate.Date == _currentDate.Date && hour == DateTime.Now.Hour)
            {
                slot.Background = new SolidColorBrush(Color.FromArgb(30, 0, 120, 212));
                timeText.Foreground = (SolidColorBrush)FindResource("PrimaryBrush");
                timeText.FontWeight = FontWeights.SemiBold;
            }

            Grid.SetColumn(timeText, 0);
            Grid.SetColumn(eventArea, 1);
            grid.Children.Add(timeText);
            grid.Children.Add(eventArea);

            slot.Child = grid;
            HourSlotsPanel.Children.Add(slot);
        }

        // Scroll to current hour if viewing today
        if (_selectedDate.Date == _currentDate.Date)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var scrollViewer = HourSlotsPanel.Parent as ScrollViewer;
                var targetOffset = Math.Max(0, (DateTime.Now.Hour - 2) * 50);
                scrollViewer?.ScrollToVerticalOffset(targetOffset);
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }
    }

    private void MonthCell_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is DateTime monthDate)
        {
            _viewDate = monthDate;
            _currentView = ViewMode.Month;
            UpdateView();
        }
    }

    private void DayCell_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is DateTime dayDate)
        {
            _selectedDate = dayDate;
            _currentView = ViewMode.Day;
            UpdateView();
        }
    }

    private void ShowYearView_Click(object sender, RoutedEventArgs e)
    {
        _currentView = ViewMode.Year;
        UpdateView();
    }

    private void ShowMonthView_Click(object sender, RoutedEventArgs e)
    {
        _viewDate = _selectedDate;
        _currentView = ViewMode.Month;
        UpdateView();
    }

    private void PrevYear_Click(object sender, RoutedEventArgs e)
    {
        _viewDate = _viewDate.AddYears(-1);
        UpdateView();
    }

    private void NextYear_Click(object sender, RoutedEventArgs e)
    {
        _viewDate = _viewDate.AddYears(1);
        UpdateView();
    }

    private void PrevMonth_Click(object sender, RoutedEventArgs e)
    {
        _viewDate = _viewDate.AddMonths(-1);
        UpdateView();
    }

    private void NextMonth_Click(object sender, RoutedEventArgs e)
    {
        _viewDate = _viewDate.AddMonths(1);
        UpdateView();
    }

    private void PrevDay_Click(object sender, RoutedEventArgs e)
    {
        _selectedDate = _selectedDate.AddDays(-1);
        UpdateView();
    }

    private void NextDay_Click(object sender, RoutedEventArgs e)
    {
        _selectedDate = _selectedDate.AddDays(1);
        UpdateView();
    }

    private void TodayButton_Click(object sender, RoutedEventArgs e)
    {
        _selectedDate = _currentDate;
        _viewDate = _currentDate;
        _currentView = ViewMode.Month;
        UpdateView();
    }

    private void MarkDay_Click(object sender, RoutedEventArgs e)
    {
        if (_markedDates.Contains(_selectedDate.Date))
        {
            _markedDates.Remove(_selectedDate.Date);
        }
        else
        {
            _markedDates.Add(_selectedDate.Date);
        }

        SaveMarkedDates();
        UpdateView();
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentView == ViewMode.Day)
        {
            _viewDate = _selectedDate;
            _currentView = ViewMode.Month;
            UpdateView();
        }
        else if (_currentView == ViewMode.Year)
        {
            _currentView = ViewMode.Month;
            UpdateView();
        }
        else
        {
            Close();
        }
    }
}
