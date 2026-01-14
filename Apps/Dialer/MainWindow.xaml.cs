using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using WindowsPhoneNext.ModemLib;
using WindowsPhone.Shared;

namespace WindowsPhoneNext.Dialer;

public partial class MainWindow : Window
{
    private readonly ModemController _modem;
    private readonly StringBuilder _phoneNumber = new();
    private readonly DispatcherTimer _callTimer;
    private DateTime _callStartTime;
    private bool _isMuted;
    private bool _isSpeakerOn;
    private bool _isInCall;
    private bool _isDemoMode;
    private readonly List<CallRecord> _recentCalls = new();

    public MainWindow()
    {
        InitializeComponent();

        _modem = new ModemController();

        _callTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _callTimer.Tick += CallTimer_Tick;

        // Subscribe to modem events
        _modem.IncomingCall += Modem_IncomingCall;
        _modem.CallStateChanged += Modem_CallStateChanged;

        LoadRecentCalls();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        await InitializeModemAsync();
    }

    private async Task InitializeModemAsync()
    {
        try
        {
            if (await _modem.AutoConnectAsync())
            {
                await _modem.InitializeAsync();
                _isDemoMode = false;
            }
            else
            {
                _isDemoMode = true;
            }
        }
        catch
        {
            // Modem initialization failed, continue in demo mode
            _isDemoMode = true;
        }
    }

    private void LoadRecentCalls()
    {
        // Load from storage - for now use sample data
        _recentCalls.Add(new CallRecord { PhoneNumber = "+1 555 123 4567", CallType = "Outgoing", Time = "2m ago" });
        _recentCalls.Add(new CallRecord { PhoneNumber = "+1 555 987 6543", CallType = "Incoming", Time = "1h ago" });
        _recentCalls.Add(new CallRecord { PhoneNumber = "+1 555 456 7890", CallType = "Missed", Time = "Yesterday" });

        RecentsList.ItemsSource = _recentCalls;
    }

    #region Dialpad

    private void DialpadButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string digit)
        {
            _phoneNumber.Append(digit);
            UpdatePhoneDisplay();
        }
    }

    private void ZeroButton_Hold(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // Long press on 0 adds +
        _phoneNumber.Append('+');
        UpdatePhoneDisplay();
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_phoneNumber.Length > 0)
        {
            _phoneNumber.Remove(_phoneNumber.Length - 1, 1);
            UpdatePhoneDisplay();
        }
    }

    private void UpdatePhoneDisplay()
    {
        PhoneNumberDisplay.Text = FormatPhoneNumber(_phoneNumber.ToString());
        DeleteButton.Visibility = _phoneNumber.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private static string FormatPhoneNumber(string number)
    {
        // Simple formatting for display
        if (number.Length <= 3) return number;
        if (number.Length <= 6) return $"{number[..3]} {number[3..]}";
        if (number.Length <= 10) return $"{number[..3]} {number[3..6]} {number[6..]}";
        return $"+{number[1..2]} {number[2..5]} {number[5..8]} {number[8..]}";
    }

    #endregion

    #region Tab Navigation

    private void KeypadTab_Click(object sender, RoutedEventArgs e)
    {
        SetActiveTab("Keypad");
        DialpadGrid.Visibility = Visibility.Visible;
        RecentsList.Visibility = Visibility.Collapsed;
    }

    private void RecentsTab_Click(object sender, RoutedEventArgs e)
    {
        SetActiveTab("Recents");
        DialpadGrid.Visibility = Visibility.Collapsed;
        RecentsList.Visibility = Visibility.Visible;
    }

    private void ContactsTab_Click(object sender, RoutedEventArgs e)
    {
        SetActiveTab("Contacts");
        // Would show contacts list
    }

    private void SetActiveTab(string tabName)
    {
        var activeBrush = FindResource("PrimaryBrush") as Brush;
        var inactiveBrush = FindResource("TextSecondaryBrush") as Brush;

        KeypadTab.Foreground = tabName == "Keypad" ? activeBrush : inactiveBrush;
        RecentsTab.Foreground = tabName == "Recents" ? activeBrush : inactiveBrush;
        ContactsTab.Foreground = tabName == "Contacts" ? activeBrush : inactiveBrush;
    }

    #endregion

    #region Call Handling

    private async void CallButton_Click(object sender, RoutedEventArgs e)
    {
        var number = _phoneNumber.ToString();
        if (string.IsNullOrWhiteSpace(number))
        {
            // If no number, show recent calls
            RecentsTab_Click(sender, e);
            return;
        }

        await MakeCallAsync(number);
    }

    private async Task MakeCallAsync(string number)
    {
        // Check if number is blocked
        if (BlockingService.Instance.IsBlockedForCalls(number))
        {
            MessageBox.Show(
                "This number is blocked. Unblock them from Contacts to call.",
                "Number Blocked",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        ShowActiveCallView(number);
        CallStatusText.Text = "Calling...";

        if (_isDemoMode)
        {
            // Demo mode: simulate call flow
            await SimulateDemoCallAsync(number);
            return;
        }

        try
        {
            var success = await _modem.DialAsync(number);

            if (success)
            {
                CallStatusText.Text = "Ringing...";

                // Wait for call to connect and start timer
                _ = MonitorCallStatusAsync();
            }
            else
            {
                CallStatusText.Text = "Call Failed";
                await Task.Delay(2000);
                ShowDialpadView();
            }
        }
        catch
        {
            CallStatusText.Text = "Call Failed";
            await Task.Delay(2000);
            ShowDialpadView();
        }
    }

    private async Task SimulateDemoCallAsync(string number)
    {
        // Simulate dialing
        CallStatusText.Text = "Dialing... (Demo)";
        await Task.Delay(1500);

        if (!_isInCall) return;

        // Simulate ringing
        CallStatusText.Text = "Ringing... (Demo)";
        await Task.Delay(2500);

        if (!_isInCall) return;

        // Simulate connection
        CallStatusText.Text = "Connected (Demo)";
        _callStartTime = DateTime.Now;
        _callTimer.Start();

        // Let the demo call run until user ends it
    }

    private async Task MonitorCallStatusAsync()
    {
        while (_isInCall)
        {
            try
            {
                var status = await _modem.GetCallStatusAsync();

                await Dispatcher.InvokeAsync(() =>
                {
                    switch (status)
                    {
                        case CallStatus.Active:
                            if (!_callTimer.IsEnabled)
                            {
                                CallStatusText.Text = "Connected";
                                _callStartTime = DateTime.Now;
                                _callTimer.Start();
                            }
                            break;
                        case CallStatus.Idle:
                            EndCall();
                            break;
                        case CallStatus.Dialing:
                            CallStatusText.Text = "Dialing...";
                            break;
                        case CallStatus.Alerting:
                            CallStatusText.Text = "Ringing...";
                            break;
                    }
                });

                await Task.Delay(1000);
            }
            catch
            {
                break;
            }
        }
    }

    private void CallTimer_Tick(object? sender, EventArgs e)
    {
        var duration = DateTime.Now - _callStartTime;
        CallDurationText.Text = duration.ToString(@"mm\:ss");
    }

    private async void EndCallButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _modem.HangUpAsync();
        }
        catch { }

        EndCall();
    }

    private void EndCall()
    {
        _isInCall = false;
        _callTimer.Stop();

        // Add to recent calls
        var number = ActiveCallNumber.Text;
        _recentCalls.Insert(0, new CallRecord
        {
            PhoneNumber = number,
            CallType = "Outgoing",
            Time = "Just now"
        });

        ShowDialpadView();
    }

    private void ShowActiveCallView(string number)
    {
        _isInCall = true;
        ActiveCallNumber.Text = number;
        CallDurationText.Text = "00:00";

        DialpadView.Visibility = Visibility.Collapsed;
        ActiveCallView.Visibility = Visibility.Visible;
    }

    private void ShowDialpadView()
    {
        _isInCall = false;
        _phoneNumber.Clear();
        UpdatePhoneDisplay();

        DialpadView.Visibility = Visibility.Visible;
        ActiveCallView.Visibility = Visibility.Collapsed;
        InCallDialpadOverlay.Visibility = Visibility.Collapsed;
    }

    #endregion

    #region In-Call Actions

    private void MuteButton_Click(object sender, RoutedEventArgs e)
    {
        _isMuted = !_isMuted;
        MuteIcon.Foreground = _isMuted
            ? FindResource("ErrorBrush") as Brush
            : FindResource("TextPrimaryBrush") as Brush;

        // Would send AT command to mute microphone
    }

    private void SpeakerButton_Click(object sender, RoutedEventArgs e)
    {
        _isSpeakerOn = !_isSpeakerOn;
        SpeakerIcon.Foreground = _isSpeakerOn
            ? FindResource("PrimaryBrush") as Brush
            : FindResource("TextPrimaryBrush") as Brush;

        // Would send AT command to enable speaker
    }

    private void KeypadButton_Click(object sender, RoutedEventArgs e)
    {
        InCallDialpadOverlay.Visibility = Visibility.Visible;
    }

    private void CloseInCallDialpad_Click(object sender, RoutedEventArgs e)
    {
        InCallDialpadOverlay.Visibility = Visibility.Collapsed;
    }

    private async void DtmfButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string digit)
        {
            try
            {
                await _modem.SendDtmfAsync(digit[0]);
            }
            catch { }
        }
    }

    #endregion

    #region Modem Events

    private async void Modem_IncomingCall(object? sender, IncomingCallEventArgs e)
    {
        // Check if caller is blocked
        if (BlockingService.Instance.IsBlockedForCalls(e.CallerId))
        {
            // Reject the call silently
            try
            {
                await _modem.HangUpAsync();
            }
            catch { }

            // Add to recent calls as blocked
            Dispatcher.Invoke(() =>
            {
                _recentCalls.Insert(0, new CallRecord
                {
                    PhoneNumber = e.CallerId,
                    CallType = "Blocked",
                    Time = "Just now"
                });
            });
            return;
        }

        Dispatcher.Invoke(() =>
        {
            ShowActiveCallView(e.CallerId);
            CallStatusText.Text = "Incoming Call";
        });
    }

    private void Modem_CallStateChanged(object? sender, CallStateChangedEventArgs e)
    {
        if (e.Status == CallStatus.Idle)
        {
            Dispatcher.Invoke(() => EndCall());
        }
    }

    #endregion

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            if (_isInCall)
            {
                EndCallButton_Click(sender, e);
            }
            else
            {
                Close();
            }
            e.Handled = true;
        }
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        _callTimer.Stop();
        _modem.Dispose();
        base.OnClosed(e);
    }
}

public class CallRecord
{
    public string PhoneNumber { get; set; } = "";
    public string CallType { get; set; } = "";
    public string Time { get; set; } = "";
}
