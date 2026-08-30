using System;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace StormMusicPlayer.Views
{
    /// <summary>
    /// Code-behind managing theme switches, color picker selections, and sensitivities.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            this.InitializeComponent();

            if (App.MainWindowInstance != null && App.MainWindowInstance.Content is FrameworkElement root)
            {
                if (root.RequestedTheme == ElementTheme.Dark)
                    ThemeDarkRadio.IsChecked = true;
                else if (root.RequestedTheme == ElementTheme.Light)
                    ThemeLightRadio.IsChecked = true;
                else
                    ThemeDefaultRadio.IsChecked = true;
            }
        }

        private void OnThemeRadioChecked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radio && App.MainWindowInstance != null)
            {
                string? theme = radio.Tag?.ToString();
                if (App.MainWindowInstance.Content is FrameworkElement root)
                {
                    switch (theme)
                    {
                        case "Dark":
                            root.RequestedTheme = ElementTheme.Dark;
                            break;
                        case "Light":
                            root.RequestedTheme = ElementTheme.Light;
                            break;
                        case "Default":
                            root.RequestedTheme = ElementTheme.Default;
                            break;
                    }
                }
            }
        }

        private void OnAccentColorClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string hexColor)
            {
                try
                {
                    Color color = ParseHexColor(hexColor);
                    var brush = new SolidColorBrush(color);

                    // Update primary resources dynamically
                    Application.Current.Resources["AccentColorBrush"] = brush;

                    // Update all ThemeDictionaries dynamically so ThemeResource bindings re-evaluate immediately
                    var dictionaries = Application.Current.Resources.ThemeDictionaries;
                    foreach (var key in dictionaries.Keys)
                    {
                        if (dictionaries[key] is ResourceDictionary dict)
                        {
                            dict["AccentColorBrush"] = brush;
                        }
                    }
                    
                    // Force re-evaluation of ThemeResource bindings in visual tree by flipping theme briefly
                    if (App.MainWindowInstance != null && App.MainWindowInstance.Content is FrameworkElement root)
                    {
                        var theme = root.RequestedTheme;
                        root.RequestedTheme = theme == ElementTheme.Light ? ElementTheme.Dark : ElementTheme.Light;
                        root.RequestedTheme = theme;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to set accent color: {ex.Message}");
                }
            }
        }

        private Color ParseHexColor(string hex)
        {
            hex = hex.Replace("#", "");
            byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            return Color.FromArgb(255, r, g, b);
        }

        private void OnSensitivityChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            // Changes standard session defaults dynamically if desired
        }
    }
}
