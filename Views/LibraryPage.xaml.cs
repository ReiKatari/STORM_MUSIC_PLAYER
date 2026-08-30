using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using StormMusicPlayer.Contracts.Services;
using StormMusicPlayer.Models;

namespace StormMusicPlayer.Views
{
    /// <summary>
    /// Code-behind managing track lists, custom file pickups, and playlist context attachments.
    /// </summary>
    public sealed partial class LibraryPage : Page
    {
        private readonly IAudioPlaybackService _playbackService;
        private readonly IPlaylistService _playlistService;

        public LibraryPage()
        {
            this.InitializeComponent();

            _playbackService = App.Current.Services.GetRequiredService<IAudioPlaybackService>();
            _playlistService = App.Current.Services.GetRequiredService<IPlaylistService>();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            RefreshTracksList();
        }

        private void RefreshTracksList()
        {
            TracksListView.ItemsSource = null;
            TracksListView.ItemsSource = _playbackService.Queue;
        }

        private void OnPlayTrackClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Track track)
            {
                _playbackService.PlayTrack(track);
            }
        }

        private void OnAddToPlaylistClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Track track)
            {
                var playlists = _playlistService.GetPlaylists();
                var flyout = new MenuFlyout();

                foreach (var playlist in playlists)
                {
                    var item = new MenuFlyoutItem 
                    { 
                        Text = playlist.Name, 
                        FontFamily = new FontFamily("Century Gothic"), 
                        FontWeight = Microsoft.UI.Text.FontWeights.Bold 
                    };
                    item.Click += (s, args) =>
                    {
                        _playlistService.AddTrackToPlaylist(playlist.Name, track);
                    };
                    flyout.Items.Add(item);
                }

                var createNewItem = new MenuFlyoutItem 
                { 
                    Text = "+ Создать плейлист...", 
                    FontFamily = new FontFamily("Century Gothic"), 
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold 
                };
                createNewItem.Click += async (s, args) =>
                {
                    await ShowCreatePlaylistDialogAsync();
                };

                flyout.Items.Add(new MenuFlyoutSeparator());
                flyout.Items.Add(createNewItem);

                button.Flyout = flyout;
                flyout.ShowAt(button);
            }
        }

        private async System.Threading.Tasks.Task ShowCreatePlaylistDialogAsync()
        {
            var inputTextBox = new TextBox 
            { 
                PlaceholderText = "Имя плейлиста", 
                FontFamily = new FontFamily("Century Gothic"), 
                FontWeight = Microsoft.UI.Text.FontWeights.Bold 
            };
            
            var dialog = new ContentDialog
            {
                Title = new TextBlock 
                { 
                    Text = "НОВЫЙ ПЛЕЙЛИСТ", 
                    FontFamily = new FontFamily("Century Gothic"), 
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold 
                },
                Content = inputTextBox,
                PrimaryButtonText = "Создать",
                CloseButtonText = "Отмена",
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(inputTextBox.Text))
            {
                _playlistService.CreatePlaylist(inputTextBox.Text);
            }
        }

        private async void OnImportFilesClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var picker = new Windows.Storage.Pickers.FileOpenPicker();
                
                // Mandatory WinUI 3 HWND association
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindowInstance);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.List;
                picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.MusicLibrary;
                picker.FileTypeFilter.Add(".mp3");
                picker.FileTypeFilter.Add(".wav");

                var files = await picker.PickMultipleFilesAsync();
                if (files != null && files.Count > 0)
                {
                    foreach (var file in files)
                    {
                        var newTrack = new Track
                        {
                            Title = file.DisplayName,
                            Artist = "Локальный файл",
                            Album = "Импортированные",
                            Path = file.Path,
                            DurationInSeconds = 240, // Standard fallback
                            CoverImagePath = ""
                        };
                        _playbackService.AddToQueue(newTrack);
                    }

                    RefreshTracksList();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"File pick failed: {ex.Message}");
            }
        }
    }
}
