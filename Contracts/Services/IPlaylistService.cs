using System.Collections.Generic;
using StormMusicPlayer.Models;

namespace StormMusicPlayer.Contracts.Services
{
    /// <summary>
    /// Contract defining operations for playlist creation and track additions.
    /// </summary>
    public interface IPlaylistService
    {
        /// <summary>
        /// Gets all active playlists.
        /// </summary>
        List<Playlist> GetPlaylists();

        /// <summary>
        /// Creates a new playlist with the specified name.
        /// </summary>
        void CreatePlaylist(string name);

        /// <summary>
        /// Adds a track to a playlist.
        /// </summary>
        void AddTrackToPlaylist(string playlistName, Track track);

        /// <summary>
        /// Removes a track from a playlist.
        /// </summary>
        void RemoveTrackFromPlaylist(string playlistName, Track track);
    }
}
