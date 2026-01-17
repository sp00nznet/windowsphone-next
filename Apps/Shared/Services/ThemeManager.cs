using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;

namespace WindowsPhoneNext.Shared.Services
{
    /// <summary>
    /// Manages application themes across all Windows Phone Next apps.
    /// Provides theme persistence and dynamic theme switching.
    /// </summary>
    public static class ThemeManager
    {
        private static readonly string SettingsPath;
        private static ThemeSettings _settings;

        public static event EventHandler<string> ThemeChanged;

        public static readonly Dictionary<string, ThemeDefinition> AvailableThemes = new()
        {
            ["Dark"] = new ThemeDefinition
            {
                Name = "Dark",
                DisplayName = "Dark",
                Description = "Default dark theme",
                BackgroundColor = "#1A1A2E",
                SurfaceColor = "#16213E",
                CardColor = "#1F2937",
                PrimaryColor = "#0078D4",
                AccentColor = "#00B4D8",
                TextPrimaryColor = "#FFFFFF",
                TextSecondaryColor = "#9CA3AF",
                BorderColor = "#374151",
                SuccessColor = "#10B981",
                ErrorColor = "#EF4444",
                WarningColor = "#F59E0B"
            },
            ["Light"] = new ThemeDefinition
            {
                Name = "Light",
                DisplayName = "Light",
                Description = "Clean light theme",
                BackgroundColor = "#F5F5F5",
                SurfaceColor = "#FFFFFF",
                CardColor = "#E5E7EB",
                PrimaryColor = "#0078D4",
                AccentColor = "#0078D4",
                TextPrimaryColor = "#1F2937",
                TextSecondaryColor = "#6B7280",
                BorderColor = "#D1D5DB",
                SuccessColor = "#059669",
                ErrorColor = "#DC2626",
                WarningColor = "#D97706"
            },
            ["MidnightBlue"] = new ThemeDefinition
            {
                Name = "MidnightBlue",
                DisplayName = "Midnight Blue",
                Description = "Deep blue night theme",
                BackgroundColor = "#0F172A",
                SurfaceColor = "#1E293B",
                CardColor = "#334155",
                PrimaryColor = "#3B82F6",
                AccentColor = "#60A5FA",
                TextPrimaryColor = "#F8FAFC",
                TextSecondaryColor = "#94A3B8",
                BorderColor = "#475569",
                SuccessColor = "#22C55E",
                ErrorColor = "#EF4444",
                WarningColor = "#F59E0B"
            },
            ["Forest"] = new ThemeDefinition
            {
                Name = "Forest",
                DisplayName = "Forest Green",
                Description = "Natural green theme",
                BackgroundColor = "#14231A",
                SurfaceColor = "#1C3326",
                CardColor = "#264534",
                PrimaryColor = "#22C55E",
                AccentColor = "#4ADE80",
                TextPrimaryColor = "#F0FDF4",
                TextSecondaryColor = "#86EFAC",
                BorderColor = "#166534",
                SuccessColor = "#4ADE80",
                ErrorColor = "#F87171",
                WarningColor = "#FBBF24"
            },
            ["Purple"] = new ThemeDefinition
            {
                Name = "Purple",
                DisplayName = "Purple Night",
                Description = "Rich purple theme",
                BackgroundColor = "#1E1B2E",
                SurfaceColor = "#2D2640",
                CardColor = "#3D3455",
                PrimaryColor = "#A855F7",
                AccentColor = "#C084FC",
                TextPrimaryColor = "#FAF5FF",
                TextSecondaryColor = "#C4B5FD",
                BorderColor = "#6B21A8",
                SuccessColor = "#34D399",
                ErrorColor = "#F87171",
                WarningColor = "#FBBF24"
            },
            ["Sunset"] = new ThemeDefinition
            {
                Name = "Sunset",
                DisplayName = "Sunset Orange",
                Description = "Warm sunset theme",
                BackgroundColor = "#1C1410",
                SurfaceColor = "#2D1F18",
                CardColor = "#3D2C22",
                PrimaryColor = "#F97316",
                AccentColor = "#FB923C",
                TextPrimaryColor = "#FFF7ED",
                TextSecondaryColor = "#FDBA74",
                BorderColor = "#C2410C",
                SuccessColor = "#4ADE80",
                ErrorColor = "#F87171",
                WarningColor = "#FCD34D"
            },
            ["Rose"] = new ThemeDefinition
            {
                Name = "Rose",
                DisplayName = "Rose Pink",
                Description = "Elegant pink theme",
                BackgroundColor = "#1C1418",
                SurfaceColor = "#2D1F26",
                CardColor = "#3D2C35",
                PrimaryColor = "#EC4899",
                AccentColor = "#F472B6",
                TextPrimaryColor = "#FDF2F8",
                TextSecondaryColor = "#F9A8D4",
                BorderColor = "#BE185D",
                SuccessColor = "#4ADE80",
                ErrorColor = "#FCA5A5",
                WarningColor = "#FBBF24"
            },
            ["Ocean"] = new ThemeDefinition
            {
                Name = "Ocean",
                DisplayName = "Ocean Teal",
                Description = "Calm ocean theme",
                BackgroundColor = "#0F1419",
                SurfaceColor = "#162025",
                CardColor = "#1E2D33",
                PrimaryColor = "#14B8A6",
                AccentColor = "#2DD4BF",
                TextPrimaryColor = "#F0FDFA",
                TextSecondaryColor = "#5EEAD4",
                BorderColor = "#0F766E",
                SuccessColor = "#4ADE80",
                ErrorColor = "#F87171",
                WarningColor = "#FBBF24"
            },
            ["HighContrast"] = new ThemeDefinition
            {
                Name = "HighContrast",
                DisplayName = "High Contrast",
                Description = "Accessibility theme",
                BackgroundColor = "#000000",
                SurfaceColor = "#1A1A1A",
                CardColor = "#2A2A2A",
                PrimaryColor = "#00FF00",
                AccentColor = "#00FFFF",
                TextPrimaryColor = "#FFFFFF",
                TextSecondaryColor = "#CCCCCC",
                BorderColor = "#FFFFFF",
                SuccessColor = "#00FF00",
                ErrorColor = "#FF0000",
                WarningColor = "#FFFF00"
            }
        };

        static ThemeManager()
        {
            var appData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "WindowsPhoneNext");

            if (!Directory.Exists(appData))
            {
                Directory.CreateDirectory(appData);
            }

            SettingsPath = Path.Combine(appData, "theme_settings.json");
            LoadSettings();
        }

        public static string CurrentTheme => _settings.CurrentTheme;

        public static ThemeDefinition GetCurrentThemeDefinition()
        {
            if (AvailableThemes.TryGetValue(_settings.CurrentTheme, out var theme))
            {
                return theme;
            }
            return AvailableThemes["Dark"];
        }

        public static void SetTheme(string themeName)
        {
            if (!AvailableThemes.ContainsKey(themeName))
            {
                themeName = "Dark";
            }

            _settings.CurrentTheme = themeName;
            SaveSettings();
            ThemeChanged?.Invoke(null, themeName);
        }

        public static void ApplyTheme(ResourceDictionary resources, string appAccentOverride = null)
        {
            var theme = GetCurrentThemeDefinition();

            // Apply colors
            resources["BackgroundColor"] = ColorFromHex(theme.BackgroundColor);
            resources["SurfaceColor"] = ColorFromHex(theme.SurfaceColor);
            resources["CardColor"] = ColorFromHex(theme.CardColor);
            resources["PrimaryColor"] = ColorFromHex(theme.PrimaryColor);
            resources["AccentColor"] = string.IsNullOrEmpty(appAccentOverride)
                ? ColorFromHex(theme.AccentColor)
                : ColorFromHex(appAccentOverride);
            resources["TextPrimaryColor"] = ColorFromHex(theme.TextPrimaryColor);
            resources["TextSecondaryColor"] = ColorFromHex(theme.TextSecondaryColor);
            resources["BorderColor"] = ColorFromHex(theme.BorderColor);
            resources["SuccessColor"] = ColorFromHex(theme.SuccessColor);
            resources["ErrorColor"] = ColorFromHex(theme.ErrorColor);
            resources["WarningColor"] = ColorFromHex(theme.WarningColor);

            // Apply brushes
            resources["BackgroundBrush"] = new SolidColorBrush(ColorFromHex(theme.BackgroundColor));
            resources["SurfaceBrush"] = new SolidColorBrush(ColorFromHex(theme.SurfaceColor));
            resources["CardBrush"] = new SolidColorBrush(ColorFromHex(theme.CardColor));
            resources["PrimaryBrush"] = new SolidColorBrush(ColorFromHex(theme.PrimaryColor));
            resources["AccentBrush"] = string.IsNullOrEmpty(appAccentOverride)
                ? new SolidColorBrush(ColorFromHex(theme.AccentColor))
                : new SolidColorBrush(ColorFromHex(appAccentOverride));
            resources["TextPrimaryBrush"] = new SolidColorBrush(ColorFromHex(theme.TextPrimaryColor));
            resources["TextSecondaryBrush"] = new SolidColorBrush(ColorFromHex(theme.TextSecondaryColor));
            resources["BorderBrush"] = new SolidColorBrush(ColorFromHex(theme.BorderColor));
            resources["SuccessBrush"] = new SolidColorBrush(ColorFromHex(theme.SuccessColor));
            resources["ErrorBrush"] = new SolidColorBrush(ColorFromHex(theme.ErrorColor));
            resources["WarningBrush"] = new SolidColorBrush(ColorFromHex(theme.WarningColor));
        }

        private static Color ColorFromHex(string hex)
        {
            if (hex.StartsWith("#"))
            {
                hex = hex.Substring(1);
            }

            if (hex.Length == 6)
            {
                return Color.FromRgb(
                    Convert.ToByte(hex.Substring(0, 2), 16),
                    Convert.ToByte(hex.Substring(2, 2), 16),
                    Convert.ToByte(hex.Substring(4, 2), 16));
            }
            else if (hex.Length == 8)
            {
                return Color.FromArgb(
                    Convert.ToByte(hex.Substring(0, 2), 16),
                    Convert.ToByte(hex.Substring(2, 2), 16),
                    Convert.ToByte(hex.Substring(4, 2), 16),
                    Convert.ToByte(hex.Substring(6, 2), 16));
            }

            return Colors.White;
        }

        private static void LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    _settings = JsonSerializer.Deserialize<ThemeSettings>(json) ?? new ThemeSettings();
                }
                else
                {
                    _settings = new ThemeSettings();
                }
            }
            catch
            {
                _settings = new ThemeSettings();
            }
        }

        private static void SaveSettings()
        {
            try
            {
                var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsPath, json);
            }
            catch
            {
                // Silently fail if we can't save settings
            }
        }
    }

    public class ThemeSettings
    {
        public string CurrentTheme { get; set; } = "Dark";
    }

    public class ThemeDefinition
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string BackgroundColor { get; set; }
        public string SurfaceColor { get; set; }
        public string CardColor { get; set; }
        public string PrimaryColor { get; set; }
        public string AccentColor { get; set; }
        public string TextPrimaryColor { get; set; }
        public string TextSecondaryColor { get; set; }
        public string BorderColor { get; set; }
        public string SuccessColor { get; set; }
        public string ErrorColor { get; set; }
        public string WarningColor { get; set; }
    }
}
