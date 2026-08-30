namespace StormMusicPlayer.Models
{
    /// <summary>
    /// Represents an equalizer profile configuration (e.g. Rock, Jazz, Bass Boost).
    /// All preset titles must be rendered in BOLD Century Gothic.
    /// </summary>
    public sealed class EqualizerPreset
    {
        /// <summary>
        /// Gets the name of the preset. Must be displayed in BOLD.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the gains for the 10 frequency bands (-12.0 to 12.0 dB).
        /// </summary>
        public double[] Gains { get; }

        public EqualizerPreset(string name, double[] gains)
        {
            Name = name;
            Gains = gains;
        }
    }
}
