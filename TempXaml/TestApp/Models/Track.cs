namespace StormMusicPlayer.Models
{
    /// <summary>
    /// Represents an audio track within the Storm Music Player.
    /// All properties are strictly formatted for binding to bold Century Gothic typography.
    /// </summary>
    public sealed class Track
    {
        /// <summary>
        /// The name of the track. Should be displayed in BOLD.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// The artist of the track. Should be displayed in BOLD.
        /// </summary>
        public string Artist { get; set; }

        /// <summary>
        /// The album name. Should be displayed in BOLD.
        /// </summary>
        public string Album { get; set; }

        /// <summary>
        /// Total track duration in seconds.
        /// </summary>
        public double DurationInSeconds { get; set; }

        /// <summary>
        /// Absolute file or resource URI path.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Path to the cached cover image.
        /// </summary>
        public string CoverImagePath { get; set; }
    }
}
