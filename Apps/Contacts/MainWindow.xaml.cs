using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WindowsPhone.Shared;

namespace WindowsPhoneNext.Contacts;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<Contact> _contacts = new();
    private readonly string _contactsFilePath;
    private Contact? _selectedContact;
    private bool _isEditing;

    public MainWindow()
    {
        InitializeComponent();

        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WindowsPhoneNext");
        Directory.CreateDirectory(appData);
        _contactsFilePath = Path.Combine(appData, "contacts.json");

        LoadContacts();
        ContactList.ItemsSource = _contacts;
    }

    private void LoadContacts()
    {
        _contacts.Clear();

        if (File.Exists(_contactsFilePath))
        {
            try
            {
                var json = File.ReadAllText(_contactsFilePath);
                var contacts = JsonSerializer.Deserialize<List<Contact>>(json);
                if (contacts != null)
                {
                    foreach (var contact in contacts.OrderBy(c => c.Name))
                    {
                        _contacts.Add(contact);
                    }
                }
            }
            catch
            {
                // Failed to load, start fresh
            }
        }

        // Add sample contacts if empty
        if (_contacts.Count == 0)
        {
            AddSampleContacts();
        }
    }

    private void AddSampleContacts()
    {
        var samples = new[]
        {
            new Contact { FirstName = "Alice", LastName = "Smith", Phone = "+1 555 123 4567", Email = "alice@example.com" },
            new Contact { FirstName = "Bob", LastName = "Johnson", Phone = "+1 555 234 5678", Email = "bob@example.com" },
            new Contact { FirstName = "Carol", LastName = "Williams", Phone = "+1 555 345 6789" },
            new Contact { FirstName = "David", LastName = "Brown", Phone = "+1 555 456 7890", Email = "david@example.com" },
            new Contact { FirstName = "Eve", LastName = "Davis", Phone = "+1 555 567 8901" }
        };

        foreach (var contact in samples)
        {
            _contacts.Add(contact);
        }

        SaveContacts();
    }

    private void SaveContacts()
    {
        try
        {
            var json = JsonSerializer.Serialize(_contacts.ToList(), new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_contactsFilePath, json);
        }
        catch
        {
            // Failed to save
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var searchText = SearchBox.Text.ToLower();

        if (string.IsNullOrWhiteSpace(searchText))
        {
            ContactList.ItemsSource = _contacts;
        }
        else
        {
            ContactList.ItemsSource = _contacts
                .Where(c => c.Name.ToLower().Contains(searchText) ||
                           c.Phone.Contains(searchText))
                .ToList();
        }
    }

    private void ContactItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Guid id)
        {
            var contact = _contacts.FirstOrDefault(c => c.Id == id);
            if (contact != null)
            {
                ShowContactDetail(contact);
            }
        }
    }

    private void ShowContactDetail(Contact contact)
    {
        _selectedContact = contact;

        DetailInitial.Text = contact.Initial;
        DetailName.Text = contact.Name;
        DetailPhone.Text = contact.Phone;

        if (!string.IsNullOrWhiteSpace(contact.Email))
        {
            DetailEmail.Text = contact.Email;
            EmailSection.Visibility = Visibility.Visible;
        }
        else
        {
            EmailSection.Visibility = Visibility.Collapsed;
        }

        // Update block status UI
        UpdateBlockStatusUI(contact.Phone);

        ContactListView.Visibility = Visibility.Collapsed;
        ContactDetailView.Visibility = Visibility.Visible;
    }

    private void UpdateBlockStatusUI(string phoneNumber)
    {
        var isBlocked = BlockingService.Instance.IsBlocked(phoneNumber);

        BlockedBanner.Visibility = isBlocked ? Visibility.Visible : Visibility.Collapsed;

        if (isBlocked)
        {
            BlockButton.Content = "✓ Unblock Contact";
            BlockButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#166534"));
        }
        else
        {
            BlockButton.Content = "🚫 Block Contact";
            BlockButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#991B1B"));
        }
    }

    private void BlockContact_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedContact == null) return;

        var isBlocked = BlockingService.Instance.IsBlocked(_selectedContact.Phone);

        if (isBlocked)
        {
            // Unblock
            BlockingService.Instance.Unblock(_selectedContact.Phone);
        }
        else
        {
            // Block
            var result = MessageBox.Show(
                $"Block {_selectedContact.Name}?\n\nThey won't be able to call or message you.",
                "Block Contact",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            BlockingService.Instance.Block(
                _selectedContact.Phone,
                _selectedContact.Name,
                "Blocked from Contacts app");
        }

        UpdateBlockStatusUI(_selectedContact.Phone);
    }

    private void BackToList_Click(object sender, RoutedEventArgs e)
    {
        ContactDetailView.Visibility = Visibility.Collapsed;
        ContactListView.Visibility = Visibility.Visible;
        _selectedContact = null;
    }

    private void AddContact_Click(object sender, RoutedEventArgs e)
    {
        _isEditing = false;
        _selectedContact = null;

        EditTitle.Text = "New Contact";
        EditFirstName.Text = "";
        EditLastName.Text = "";
        EditPhone.Text = "";
        EditEmail.Text = "";
        EditInitial.Text = "?";

        ContactListView.Visibility = Visibility.Collapsed;
        EditContactView.Visibility = Visibility.Visible;

        EditFirstName.Focus();
    }

    private void EditContact_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedContact == null) return;

        _isEditing = true;

        EditTitle.Text = "Edit Contact";
        EditFirstName.Text = _selectedContact.FirstName;
        EditLastName.Text = _selectedContact.LastName;
        EditPhone.Text = _selectedContact.Phone;
        EditEmail.Text = _selectedContact.Email;
        UpdateEditInitial();

        ContactDetailView.Visibility = Visibility.Collapsed;
        EditContactView.Visibility = Visibility.Visible;
    }

    private void CancelEdit_Click(object sender, RoutedEventArgs e)
    {
        EditContactView.Visibility = Visibility.Collapsed;

        if (_isEditing && _selectedContact != null)
        {
            ContactDetailView.Visibility = Visibility.Visible;
        }
        else
        {
            ContactListView.Visibility = Visibility.Visible;
        }
    }

    private void SaveContact_Click(object sender, RoutedEventArgs e)
    {
        var firstName = EditFirstName.Text.Trim();
        var lastName = EditLastName.Text.Trim();
        var phone = EditPhone.Text.Trim();
        var email = EditEmail.Text.Trim();

        if (string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastName))
        {
            MessageBox.Show("Please enter a name.", "Validation Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(phone))
        {
            MessageBox.Show("Please enter a phone number.", "Validation Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_isEditing && _selectedContact != null)
        {
            _selectedContact.FirstName = firstName;
            _selectedContact.LastName = lastName;
            _selectedContact.Phone = phone;
            _selectedContact.Email = email;
        }
        else
        {
            var newContact = new Contact
            {
                FirstName = firstName,
                LastName = lastName,
                Phone = phone,
                Email = email
            };
            _contacts.Add(newContact);
            _selectedContact = newContact;
        }

        SaveContacts();

        // Refresh the list
        var sorted = _contacts.OrderBy(c => c.Name).ToList();
        _contacts.Clear();
        foreach (var c in sorted)
        {
            _contacts.Add(c);
        }

        EditContactView.Visibility = Visibility.Collapsed;

        if (_selectedContact != null)
        {
            ShowContactDetail(_selectedContact);
        }
        else
        {
            ContactListView.Visibility = Visibility.Visible;
        }
    }

    private void DeleteContact_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedContact == null) return;

        var result = MessageBox.Show(
            $"Delete {_selectedContact.Name}?",
            "Delete Contact",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            _contacts.Remove(_selectedContact);
            SaveContacts();

            ContactDetailView.Visibility = Visibility.Collapsed;
            ContactListView.Visibility = Visibility.Visible;
            _selectedContact = null;
        }
    }

    private void EditName_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateEditInitial();
    }

    private void UpdateEditInitial()
    {
        var first = EditFirstName.Text.Trim();
        var last = EditLastName.Text.Trim();

        if (!string.IsNullOrEmpty(first) || !string.IsNullOrEmpty(last))
        {
            var initial = "";
            if (!string.IsNullOrEmpty(first)) initial += first[0];
            if (!string.IsNullOrEmpty(last)) initial += last[0];
            EditInitial.Text = initial.ToUpper();
        }
        else
        {
            EditInitial.Text = "?";
        }
    }

    private void CallContact_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedContact == null) return;
        LaunchDialer(_selectedContact.Phone);
    }

    private void MessageContact_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedContact == null) return;
        LaunchMessaging(_selectedContact.Phone);
    }

    private void LaunchDialer(string phoneNumber)
    {
        try
        {
            var appsBasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..");
            var appPath = Path.Combine(appsBasePath, "Dialer", "WindowsPhoneDialer.exe");

            if (File.Exists(appPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = appPath,
                    Arguments = phoneNumber,
                    UseShellExecute = true
                });
            }
        }
        catch
        {
            // Failed to launch dialer
        }
    }

    private void LaunchMessaging(string phoneNumber)
    {
        try
        {
            var appsBasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..");
            var appPath = Path.Combine(appsBasePath, "Messaging", "WindowsPhoneMessaging.exe");

            if (File.Exists(appPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = appPath,
                    Arguments = phoneNumber,
                    UseShellExecute = true
                });
            }
        }
        catch
        {
            // Failed to launch messaging
        }
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            if (EditContactView.Visibility == Visibility.Visible)
            {
                CancelEdit_Click(sender, e);
            }
            else if (ContactDetailView.Visibility == Visibility.Visible)
            {
                BackToList_Click(sender, e);
            }
            else
            {
                Close();
            }
            e.Handled = true;
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

public class Contact
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";

    public string Name => $"{FirstName} {LastName}".Trim();

    public string Initial
    {
        get
        {
            var initial = "";
            if (!string.IsNullOrEmpty(FirstName)) initial += FirstName[0];
            if (!string.IsNullOrEmpty(LastName)) initial += LastName[0];
            return initial.ToUpper();
        }
    }
}
