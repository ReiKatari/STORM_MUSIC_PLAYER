using System.Collections.Generic;

namespace StormMusicPlayer.Models
{
    /// <summary>
    /// Represents a custom user playlist within Storm Music Player.
    /// </summary>
    public sealed class Playlist
    {
        /// <summary>
        /// The name of the playlist.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The list of tracks associated with this playlist.
        /// </summary>
        public List<Track> Tracks { get; set; } = new List<Track>();
    }
}
