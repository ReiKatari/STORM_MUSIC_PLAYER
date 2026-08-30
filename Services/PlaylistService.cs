using System.Collections.Generic;
using System.Linq;
using StormMusicPlayer.Contracts.Services;
using StormMusicPlayer.Models;

namespace StormMusicPlayer.Services
{
    /// <summary>
    /// Service managing custom user playlists and track links.
    /// </summary>
    public sealed class PlaylistService : IPlaylistService
    {
        private readonly List<Playlist> _playlists = new List<Playlist>();

        public PlaylistService()
        {
            // Pre-populate with beautiful default playlists
            var stormMix = new Playlist { Name = "Штормовой Микс" };
            stormMix.Tracks.Add(new Track 
            { 
                Title = "Storm Winds", 
                Artist = "Storm System", 
                Album = "SoundHelix Vol. 1", 
                DurationInSeconds = 372, 
                Path = "https://www.soundhelix.com/examples/mp3/SoundHelix-Song-1.mp3", 
                CoverImagePath = "Assets/StormWinds.png" 
            });
            stormMix.Tracks.Add(new Track 
            { 
                Title = "Thunder Force", 
                Artist = "Storm System", 
                Album = "SoundHelix Vol. 1", 
                DurationInSeconds = 423, 
                Path = "https://www.soundhelix.com/examples/mp3/SoundHelix-Song-2.mp3", 
                CoverImagePath = "Assets/ThunderForce.png" 
            });
            _playlists.Add(stormMix);

            var chillMix = new Playlist { Name = "Релакс" };
            chillMix.Tracks.Add(new Track 
            { 
                Title = "Raindrops", 
                Artist = "Storm System", 
                Album = "SoundHelix Vol. 1", 
                DurationInSeconds = 302, 
                Path = "https://www.soundhelix.com/examples/mp3/SoundHelix-Song-3.mp3", 
                CoverImagePath = "Assets/Raindrops.png" 
            });
            _playlists.Add(chillMix);
        }

        public List<Playlist> GetPlaylists() => _playlists;

        public void CreatePlaylist(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            if (!_playlists.Any(p => p.Name == name))
            {
                _playlists.Add(new Playlist { Name = name });
            }
        }

        public void AddTrackToPlaylist(string playlistName, Track track)
        {
            if (track == null) return;
            var playlist = _playlists.FirstOrDefault(p => p.Name == playlistName);
            if (playlist != null)
            {
                if (!playlist.Tracks.Any(t => t.Path == track.Path))
                {
                    playlist.Tracks.Add(track);
                }
            }
        }

        public void RemoveTrackFromPlaylist(string playlistName, Track track)
        {
            if (track == null) return;
            var playlist = _playlists.FirstOrDefault(p => p.Name == playlistName);
            if (playlist != null)
            {
                var existing = playlist.Tracks.FirstOrDefault(t => t.Path == track.Path);
                if (existing != null)
                {
                    playlist.Tracks.Remove(existing);
                }
            }
        }
    }
}
