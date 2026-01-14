using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using WindowsPhoneNext.ModemLib;
using WindowsPhone.Shared;

namespace WindowsPhoneNext.Messaging;

public partial class MainWindow : Window
{
    private readonly ModemController _modem;
    private readonly ObservableCollection<Conversation> _conversations = new();
    private readonly ObservableCollection<Message> _messages = new();
    private readonly DispatcherTimer _refreshTimer;
    private Conversation? _currentConversation;
    private ViewMode _currentView = ViewMode.Conversations;

    private enum ViewMode
    {
        Conversations,
        Chat,
        NewMessage
    }

    public MainWindow()
    {
        InitializeComponent();

        _modem = new ModemController();
        _modem.SmsReceived += Modem_SmsReceived;

        ConversationsList.ItemsSource = _conversations;
        MessagesList.ItemsSource = _messages;

        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(30)
        };
        _refreshTimer.Tick += RefreshTimer_Tick;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        await InitializeModemAsync();
        await LoadMessagesAsync();
        _refreshTimer.Start();
    }

    private async Task InitializeModemAsync()
    {
        try
        {
            if (await _modem.AutoConnectAsync())
            {
                await _modem.InitializeAsync();
            }
        }
        catch
        {
            // Continue in demo mode
        }
    }

    private async Task LoadMessagesAsync()
    {
        try
        {
            if (_modem.IsConnected)
            {
                var smsMessages = await _modem.ReadAllSmsAsync();
                ProcessSmsMessages(smsMessages);
            }
            else
            {
                LoadDemoData();
            }

            UpdateEmptyState();
        }
        catch
        {
            LoadDemoData();
            UpdateEmptyState();
        }
    }

    private void ProcessSmsMessages(List<SmsMessage> smsMessages)
    {
        var groupedMessages = smsMessages.GroupBy(m => m.Sender);

        foreach (var group in groupedMessages)
        {
            // Skip blocked senders
            if (BlockingService.Instance.IsBlockedForMessages(group.Key))
            {
                continue;
            }

            var existingConv = _conversations.FirstOrDefault(c => c.PhoneNumber == group.Key);

            if (existingConv == null)
            {
                existingConv = new Conversation
                {
                    PhoneNumber = group.Key,
                    ContactName = group.Key,
                    Initial = GetInitial(group.Key)
                };
                _conversations.Add(existingConv);
            }

            foreach (var sms in group)
            {
                if (!existingConv.Messages.Any(m => m.Timestamp == sms.Timestamp && m.Body == sms.Body))
                {
                    existingConv.Messages.Add(new Message
                    {
                        Body = sms.Body,
                        Timestamp = sms.Timestamp,
                        IsSent = false,
                        SmsIndex = sms.Index
                    });
                }
            }

            var lastMsg = existingConv.Messages.OrderByDescending(m => m.Timestamp).FirstOrDefault();
            if (lastMsg != null)
            {
                existingConv.LastMessage = lastMsg.Body;
                existingConv.Time = FormatTime(lastMsg.Timestamp);
            }
        }

        var sorted = _conversations.OrderByDescending(c =>
            c.Messages.Any() ? c.Messages.Max(m => m.Timestamp) : DateTime.MinValue).ToList();

        _conversations.Clear();
        foreach (var conv in sorted)
        {
            _conversations.Add(conv);
        }
    }

    private void LoadDemoData()
    {
        if (_conversations.Count > 0) return;

        _conversations.Add(new Conversation
        {
            PhoneNumber = "+1 555 123 4567",
            ContactName = "John Doe",
            Initial = "J",
            LastMessage = "Hey, how are you doing?",
            Time = "2:30 PM",
            UnreadCount = 2,
            Messages = new List<Message>
            {
                new Message { Body = "Hi there!", Timestamp = DateTime.Now.AddHours(-2), IsSent = true },
                new Message { Body = "Hey! How's it going?", Timestamp = DateTime.Now.AddHours(-1.5), IsSent = false },
                new Message { Body = "Pretty good, thanks!", Timestamp = DateTime.Now.AddHours(-1), IsSent = true },
                new Message { Body = "Hey, how are you doing?", Timestamp = DateTime.Now.AddMinutes(-30), IsSent = false }
            }
        });

        _conversations.Add(new Conversation
        {
            PhoneNumber = "+1 555 987 6543",
            ContactName = "Jane Smith",
            Initial = "J",
            LastMessage = "See you tomorrow!",
            Time = "Yesterday"
        });

        _conversations.Add(new Conversation
        {
            PhoneNumber = "+1 555 456 7890",
            ContactName = "Work",
            Initial = "W",
            LastMessage = "Meeting at 3pm confirmed",
            Time = "Mon"
        });
    }

    private void UpdateEmptyState()
    {
        EmptyState.Visibility = _conversations.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private static string GetInitial(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "?";
        if (name.StartsWith("+") || char.IsDigit(name[0]))
            return "#";
        return name[0].ToString().ToUpper();
    }

    private static string FormatTime(DateTime time)
    {
        var now = DateTime.Now;

        if (time.Date == now.Date)
            return time.ToString("h:mm tt");

        if (time.Date == now.Date.AddDays(-1))
            return "Yesterday";

        if ((now - time).TotalDays < 7)
            return time.ToString("ddd");

        return time.ToString("MMM d");
    }

    #region Navigation

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        switch (_currentView)
        {
            case ViewMode.Chat:
            case ViewMode.NewMessage:
                ShowConversationsView();
                break;
            case ViewMode.Conversations:
                Close();
                break;
        }
    }

    private void ShowConversationsView()
    {
        _currentView = ViewMode.Conversations;
        TitleText.Text = "Messages";
        ConversationsView.Visibility = Visibility.Visible;
        ChatView.Visibility = Visibility.Collapsed;
        NewMessageView.Visibility = Visibility.Collapsed;
        _currentConversation = null;
    }

    private void ShowChatView(Conversation conversation)
    {
        _currentView = ViewMode.Chat;
        _currentConversation = conversation;
        TitleText.Text = conversation.ContactName;

        conversation.UnreadCount = 0;

        _messages.Clear();
        foreach (var msg in conversation.Messages.OrderBy(m => m.Timestamp))
        {
            _messages.Add(msg);
        }

        ConversationsView.Visibility = Visibility.Collapsed;
        ChatView.Visibility = Visibility.Visible;
        NewMessageView.Visibility = Visibility.Collapsed;
    }

    private void ShowNewMessageView()
    {
        _currentView = ViewMode.NewMessage;
        TitleText.Text = "New Message";

        ConversationsView.Visibility = Visibility.Collapsed;
        ChatView.Visibility = Visibility.Collapsed;
        NewMessageView.Visibility = Visibility.Visible;

        RecipientInput.Text = "";
        NewMessageInput.Text = "";
        RecipientInput.Focus();
    }

    #endregion

    #region Event Handlers

    private void ConversationsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ConversationsList.SelectedItem is Conversation conversation)
        {
            ShowChatView(conversation);
            ConversationsList.SelectedItem = null;
        }
    }

    private void NewMessageButton_Click(object sender, RoutedEventArgs e)
    {
        ShowNewMessageView();
    }

    private void MessageInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        SendButton.IsEnabled = !string.IsNullOrWhiteSpace(MessageInput.Text);
    }

    private void MessageInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && SendButton.IsEnabled)
        {
            SendMessage();
        }
    }

    private void SendButton_Click(object sender, RoutedEventArgs e)
    {
        SendMessage();
    }

    private async void SendMessage()
    {
        if (_currentConversation == null) return;

        // Check if contact is blocked
        if (BlockingService.Instance.IsBlockedForMessages(_currentConversation.PhoneNumber))
        {
            MessageBox.Show(
                "This contact is blocked. Unblock them from Contacts to send messages.",
                "Contact Blocked",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var messageText = MessageInput.Text.Trim();
        if (string.IsNullOrEmpty(messageText)) return;

        var message = new Message
        {
            Body = messageText,
            Timestamp = DateTime.Now,
            IsSent = true,
            Status = MessageStatus.Sending
        };

        _messages.Add(message);
        _currentConversation.Messages.Add(message);
        _currentConversation.LastMessage = messageText;
        _currentConversation.Time = "Just now";

        MessageInput.Text = "";

        try
        {
            if (_modem.IsConnected)
            {
                var success = await _modem.SendSmsAsync(_currentConversation.PhoneNumber, messageText);
                message.Status = success ? MessageStatus.Sent : MessageStatus.Failed;
            }
            else
            {
                await Task.Delay(500);
                message.Status = MessageStatus.Sent;
            }
        }
        catch
        {
            message.Status = MessageStatus.Failed;
        }
    }

    private void NewMessageInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        NewSendButton.IsEnabled = !string.IsNullOrWhiteSpace(NewMessageInput.Text) &&
                                   !string.IsNullOrWhiteSpace(RecipientInput.Text);
    }

    private async void NewSendButton_Click(object sender, RoutedEventArgs e)
    {
        var recipient = RecipientInput.Text.Trim();
        var messageText = NewMessageInput.Text.Trim();

        if (string.IsNullOrEmpty(recipient) || string.IsNullOrEmpty(messageText))
            return;

        // Check if recipient is blocked
        if (BlockingService.Instance.IsBlockedForMessages(recipient))
        {
            MessageBox.Show(
                "This number is blocked. Unblock them from Contacts to send messages.",
                "Number Blocked",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var conversation = _conversations.FirstOrDefault(c => c.PhoneNumber == recipient);
        if (conversation == null)
        {
            conversation = new Conversation
            {
                PhoneNumber = recipient,
                ContactName = recipient,
                Initial = GetInitial(recipient)
            };
            _conversations.Insert(0, conversation);
        }

        var message = new Message
        {
            Body = messageText,
            Timestamp = DateTime.Now,
            IsSent = true,
            Status = MessageStatus.Sending
        };

        conversation.Messages.Add(message);
        conversation.LastMessage = messageText;
        conversation.Time = "Just now";

        try
        {
            if (_modem.IsConnected)
            {
                var success = await _modem.SendSmsAsync(recipient, messageText);
                message.Status = success ? MessageStatus.Sent : MessageStatus.Failed;
            }
            else
            {
                await Task.Delay(500);
                message.Status = MessageStatus.Sent;
            }
        }
        catch
        {
            message.Status = MessageStatus.Failed;
        }

        ShowChatView(conversation);
    }

    private async void RefreshTimer_Tick(object? sender, EventArgs e)
    {
        if (_modem.IsConnected)
        {
            await LoadMessagesAsync();
        }
    }

    private void Modem_SmsReceived(object? sender, SmsReceivedEventArgs e)
    {
        // Check if sender is blocked - silently ignore if so
        if (BlockingService.Instance.IsBlockedForMessages(e.Sender))
        {
            return;
        }

        Dispatcher.Invoke(async () =>
        {
            await LoadMessagesAsync();
        });
    }

    #endregion

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            BackButton_Click(sender, e);
            e.Handled = true;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _refreshTimer.Stop();
        _modem.Dispose();
        base.OnClosed(e);
    }
}

#region Data Classes

public class Conversation
{
    public string PhoneNumber { get; set; } = "";
    public string ContactName { get; set; } = "";
    public string Initial { get; set; } = "";
    public string LastMessage { get; set; } = "";
    public string Time { get; set; } = "";
    public int UnreadCount { get; set; }
    public Visibility HasUnread => UnreadCount > 0 ? Visibility.Visible : Visibility.Collapsed;
    public List<Message> Messages { get; set; } = new();
}

public class Message
{
    private static readonly SolidColorBrush SentBrush = new(Color.FromRgb(0x00, 0x78, 0xD4));
    private static readonly SolidColorBrush ReceivedBrush = new(Color.FromRgb(0x37, 0x41, 0x51));

    public string Body { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public bool IsSent { get; set; }
    public MessageStatus Status { get; set; } = MessageStatus.Sent;
    public int SmsIndex { get; set; }
    public string TimeString => Timestamp.ToString("h:mm tt");

    // Properties for XAML binding without converters
    public HorizontalAlignment Alignment => IsSent ? HorizontalAlignment.Right : HorizontalAlignment.Left;
    public SolidColorBrush BubbleColor => IsSent ? SentBrush : ReceivedBrush;
}

public enum MessageStatus
{
    Sending,
    Sent,
    Delivered,
    Failed
}

#endregion
