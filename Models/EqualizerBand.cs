using CommunityToolkit.Mvvm.ComponentModel;

namespace StormMusicPlayer.Models
{
    /// <summary>
    /// Represents a single frequency band in the parametric equalizer.
    /// Uses CommunityToolkit.Mvvm for real-time slider bindings.
    /// </summary>
    public sealed class EqualizerBand : ObservableObject
    {
        private double _gain;

        /// <summary>
        /// Gets the frequency of this band in Hz.
        /// </summary>
        public int Frequency { get; init; }

        /// <summary>
        /// Gets or sets the gain of this band in decibels (typically -12.0dB to +12.0dB).
        /// </summary>
        public double Gain
        {
            get => _gain;
            set => SetProperty(ref _gain, value);
        }
    }
}
