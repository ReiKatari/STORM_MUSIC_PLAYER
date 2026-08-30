using System.Collections.Generic;
using StormMusicPlayer.Models;

namespace StormMusicPlayer.Contracts.Services
{
    /// <summary>
    /// Contract managing 10-band equalizer levels and premium presets.
    /// Ensures preset headers and names are formatted in BOLD.
    /// </summary>
    public interface IEqualizerService
    {
        /// <summary>
        /// Gets or sets whether the equalizer DSP effect is active.
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// Gets or sets the overall Pre-Amplification level in dB (-12.0 to 12.0).
        /// </summary>
        double PreampGain { get; set; }

        /// <summary>
        /// Gets the standard 10 parametric frequency bands (Hz).
        /// </summary>
        List<EqualizerBand> Bands { get; }

        /// <summary>
        /// Gets list of standard and custom saved EqualizerPresets.
        /// </summary>
        List<EqualizerPreset> Presets { get; }

        /// <summary>
        /// Configures equalizer band gains to match selected preset.
        /// </summary>
        void ApplyPreset(EqualizerPreset preset);

        /// <summary>
        /// Dynamically alters gain values at targeted frequency band index.
        /// </summary>
        void SetBandGain(int index, double gain);
    }
}
