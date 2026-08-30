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
    /// Code-behind managing playlist lists, track linkages, and play collections.
    /// </summary>
    public sealed partial class PlaylistsPage : Page
    {
        private readonly IPlaylistService _playlistService;
        private readonly IAudioPlaybackService _playbackService;
        private Playlist? _selectedPlaylist;

        public PlaylistsPage()
        {
            this.InitializeComponent();

            _playlistService = App.Current.Services.GetRequiredService<IPlaylistService>();
            _playbackService = App.Current.Services.GetRequiredService<IAudioPlaybackService>();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            RefreshPlaylistsList();
        }

        private void RefreshPlaylistsList()
        {
            PlaylistsListView.ItemsSource = null;
            PlaylistsListView.ItemsSource = _playlistService.GetPlaylists();
        }

        private void OnPlaylistSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PlaylistsListView.SelectedItem is Playlist playlist)
            {
                _selectedPlaylist = playlist;
                PlaylistDetailsPanel.Visibility = Visibility.Visible;
                SelectedPlaylistNameText.Text = playlist.Name.ToUpper();
                TracksCountText.Text = $"{playlist.Tracks.Count} треков";
                
                RefreshTracksList();
            }
            else
            {
                _selectedPlaylist = null;
                PlaylistDetailsPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void RefreshTracksList()
        {
            if (_selectedPlaylist != null)
            {
                PlaylistTracksListView.ItemsSource = null;
                PlaylistTracksListView.ItemsSource = _selectedPlaylist.Tracks;
                TracksCountText.Text = $"{_selectedPlaylist.Tracks.Count} треков";
            }
        }

        private async void OnCreatePlaylistClick(object sender, RoutedEventArgs e)
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
                RefreshPlaylistsList();
            }
        }

        private void OnPlayAllClick(object sender, RoutedEventArgs e)
        {
            if (_selectedPlaylist != null && _selectedPlaylist.Tracks.Count > 0)
            {
                _playbackService.SetQueue(_selectedPlaylist.Tracks);
            }
        }

        private void OnRemoveTrackClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Track track && _selectedPlaylist != null)
            {
                _playlistService.RemoveTrackFromPlaylist(_selectedPlaylist.Name, track);
                RefreshTracksList();
            }
        }
    }
}
