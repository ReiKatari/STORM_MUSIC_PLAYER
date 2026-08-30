using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace StormMusicPlayer.Views
{
    /// <summary>
    /// Serves as the navigation controller container containing the sidebar panel.
    /// Routes frames to target views upon selection clicks.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            // Set default view target
            ShellNavigationView.SelectedItem = ShellNavigationView.MenuItems[0];
            ContentFrame.Navigate(typeof(NowPlayingPage));
        }

        private void OnNavigationSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                // Future expansion: SettingsPage route
                return;
            }

            if (args.SelectedItemContainer is NavigationViewItem selectedItem)
            {
                string destinationTag = selectedItem.Tag?.ToString();
                switch (destinationTag)
                {
                    case "NowPlaying":
                        ContentFrame.Navigate(typeof(NowPlayingPage));
                        break;
                    case "Equalizer":
                        ContentFrame.Navigate(typeof(EqualizerPage));
                        break;
                    case "Visualizer":
                        ContentFrame.Navigate(typeof(VisualizerPage));
                        break;
                    default:
                        // Default Fallback
                        ContentFrame.Navigate(typeof(NowPlayingPage));
                        break;
                }
            }
        }
    }
}
