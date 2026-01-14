using System.Text.Json;
using System.Text.RegularExpressions;

namespace WindowsPhone.Shared;

/// <summary>
/// Represents a blocked contact with associated metadata.
/// </summary>
public class BlockedContact
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string PhoneNumber { get; set; } = "";
    public string Name { get; set; } = "";
    public string Reason { get; set; } = "";
    public DateTime DateBlocked { get; set; } = DateTime.Now;
    public bool BlockCalls { get; set; } = true;
    public bool BlockMessages { get; set; } = true;
}

/// <summary>
/// Event args for when a blocked contact list changes.
/// </summary>
public class BlockListChangedEventArgs : EventArgs
{
    public BlockedContact? Contact { get; set; }
    public bool WasAdded { get; set; }
}

/// <summary>
/// Shared service for managing blocked contacts across all apps.
/// Singleton pattern ensures all apps share the same block list.
/// </summary>
public sealed class BlockingService
{
    private static readonly Lazy<BlockingService> _instance = new(() => new BlockingService());
    public static BlockingService Instance => _instance.Value;

    private readonly string _blockListPath;
    private List<BlockedContact> _blockedContacts = new();
    private readonly object _lock = new();

    public event EventHandler<BlockListChangedEventArgs>? BlockListChanged;

    public IReadOnlyList<BlockedContact> BlockedContacts
    {
        get
        {
            lock (_lock)
            {
                return _blockedContacts.AsReadOnly();
            }
        }
    }

    private BlockingService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WindowsPhoneNext");

        Directory.CreateDirectory(appDataPath);
        _blockListPath = Path.Combine(appDataPath, "blocklist.json");

        Load();
    }

    /// <summary>
    /// Check if a phone number is blocked.
    /// </summary>
    public bool IsBlocked(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        var normalized = NormalizePhoneNumber(phoneNumber);

        lock (_lock)
        {
            return _blockedContacts.Any(c =>
                NormalizePhoneNumber(c.PhoneNumber) == normalized);
        }
    }

    /// <summary>
    /// Check if a phone number is blocked for calls specifically.
    /// </summary>
    public bool IsBlockedForCalls(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        var normalized = NormalizePhoneNumber(phoneNumber);

        lock (_lock)
        {
            return _blockedContacts.Any(c =>
                NormalizePhoneNumber(c.PhoneNumber) == normalized && c.BlockCalls);
        }
    }

    /// <summary>
    /// Check if a phone number is blocked for messages specifically.
    /// </summary>
    public bool IsBlockedForMessages(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        var normalized = NormalizePhoneNumber(phoneNumber);

        lock (_lock)
        {
            return _blockedContacts.Any(c =>
                NormalizePhoneNumber(c.PhoneNumber) == normalized && c.BlockMessages);
        }
    }

    /// <summary>
    /// Get a blocked contact by phone number.
    /// </summary>
    public BlockedContact? GetBlockedContact(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return null;

        var normalized = NormalizePhoneNumber(phoneNumber);

        lock (_lock)
        {
            return _blockedContacts.FirstOrDefault(c =>
                NormalizePhoneNumber(c.PhoneNumber) == normalized);
        }
    }

    /// <summary>
    /// Block a phone number.
    /// </summary>
    public void Block(string phoneNumber, string name = "", string reason = "",
        bool blockCalls = true, bool blockMessages = true)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return;

        var normalized = NormalizePhoneNumber(phoneNumber);

        lock (_lock)
        {
            // Check if already blocked
            var existing = _blockedContacts.FirstOrDefault(c =>
                NormalizePhoneNumber(c.PhoneNumber) == normalized);

            if (existing != null)
            {
                // Update existing entry
                existing.Name = name;
                existing.Reason = reason;
                existing.BlockCalls = blockCalls;
                existing.BlockMessages = blockMessages;
            }
            else
            {
                // Add new entry
                var blocked = new BlockedContact
                {
                    PhoneNumber = phoneNumber,
                    Name = name,
                    Reason = reason,
                    BlockCalls = blockCalls,
                    BlockMessages = blockMessages,
                    DateBlocked = DateTime.Now
                };
                _blockedContacts.Add(blocked);

                BlockListChanged?.Invoke(this, new BlockListChangedEventArgs
                {
                    Contact = blocked,
                    WasAdded = true
                });
            }
        }

        Save();
    }

    /// <summary>
    /// Unblock a phone number.
    /// </summary>
    public void Unblock(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return;

        var normalized = NormalizePhoneNumber(phoneNumber);
        BlockedContact? removed = null;

        lock (_lock)
        {
            removed = _blockedContacts.FirstOrDefault(c =>
                NormalizePhoneNumber(c.PhoneNumber) == normalized);

            if (removed != null)
            {
                _blockedContacts.Remove(removed);
            }
        }

        if (removed != null)
        {
            Save();
            BlockListChanged?.Invoke(this, new BlockListChangedEventArgs
            {
                Contact = removed,
                WasAdded = false
            });
        }
    }

    /// <summary>
    /// Unblock by ID.
    /// </summary>
    public void UnblockById(Guid id)
    {
        BlockedContact? removed = null;

        lock (_lock)
        {
            removed = _blockedContacts.FirstOrDefault(c => c.Id == id);

            if (removed != null)
            {
                _blockedContacts.Remove(removed);
            }
        }

        if (removed != null)
        {
            Save();
            BlockListChanged?.Invoke(this, new BlockListChangedEventArgs
            {
                Contact = removed,
                WasAdded = false
            });
        }
    }

    /// <summary>
    /// Normalize a phone number for comparison (remove spaces, dashes, etc.).
    /// </summary>
    public static string NormalizePhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return "";

        // Remove all non-digit characters except leading +
        var normalized = Regex.Replace(phoneNumber, @"[^\d+]", "");

        // Remove leading + if present and normalize
        if (normalized.StartsWith("+1"))
            normalized = normalized.Substring(2);
        else if (normalized.StartsWith("1") && normalized.Length == 11)
            normalized = normalized.Substring(1);
        else if (normalized.StartsWith("+"))
            normalized = normalized.Substring(1);

        return normalized;
    }

    private void Load()
    {
        try
        {
            if (File.Exists(_blockListPath))
            {
                var json = File.ReadAllText(_blockListPath);
                _blockedContacts = JsonSerializer.Deserialize<List<BlockedContact>>(json) ?? new();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load block list: {ex.Message}");
            _blockedContacts = new();
        }
    }

    private void Save()
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(_blockedContacts, options);
            File.WriteAllText(_blockListPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save block list: {ex.Message}");
        }
    }

    /// <summary>
    /// Force reload from disk (useful if another app modified the list).
    /// </summary>
    public void Reload()
    {
        lock (_lock)
        {
            Load();
        }
    }
}
