using System;
using System.Collections.Generic;
using StormMusicPlayer.Contracts.Services;
using StormMusicPlayer.Models;

namespace StormMusicPlayer.Services
{
    /// <summary>
    /// Implements standard ISO 10-band parametric equalization controls.
    /// Provides beautiful default preset profiles with bold headings.
    /// </summary>
    public sealed class EqualizerService : IEqualizerService
    {
        public bool IsEnabled { get; set; } = true;
        public double PreampGain { get; set; } = 0.0;
        
        public List<EqualizerBand> Bands { get; } = new List<EqualizerBand>();
        public List<EqualizerPreset> Presets { get; } = new List<EqualizerPreset>();

        public EqualizerService()
        {
            InitializeBands();
            InitializePresets();
        }

        private void InitializeBands()
        {
            // Set standard ISO frequencies in Hz:
            int[] frequencies = { 31, 62, 125, 250, 500, 1000, 2000, 4000, 8000, 16000 };
            foreach (var freq in frequencies)
            {
                Bands.Add(new EqualizerBand { Frequency = freq, Gain = 0.0 });
            }
        }

        private void InitializePresets()
        {
            Presets.Add(new EqualizerPreset("Flat", new double[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }));
            Presets.Add(new EqualizerPreset("Рок", new double[] { 4.5, 3.0, 1.5, 0.0, -1.0, -0.5, 1.5, 3.0, 4.0, 5.0 }));
            Presets.Add(new EqualizerPreset("Поп", new double[] { -1.5, -0.5, 1.5, 3.0, 4.0, 3.5, 1.5, -0.5, -1.0, -1.5 }));
            Presets.Add(new EqualizerPreset("Джаз", new double[] { 3.0, 2.0, 1.0, 1.5, -1.0, -1.0, 0.0, 1.5, 2.5, 3.5 }));
            Presets.Add(new EqualizerPreset("Классика", new double[] { 4.0, 3.0, 2.0, 1.5, -1.0, -1.0, 0.0, 2.0, 3.0, 4.0 }));
            Presets.Add(new EqualizerPreset("Bass Boost", new double[] { 7.5, 6.0, 4.5, 2.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 }));
        }

        public void ApplyPreset(EqualizerPreset preset)
        {
            if (preset == null || preset.Gains.Length != Bands.Count) return;

            for (int i = 0; i < Bands.Count; i++)
            {
                Bands[i].Gain = preset.Gains[i];
            }

            // In actual DSP implementation, these gain values are parsed and loaded into 
            // the low-level WASAPI or AudioGraph BiquadFilterEffectDefinition nodes.
        }

        public void SetBandGain(int index, double gain)
        {
            if (index < 0 || index >= Bands.Count) return;
            Bands[index].Gain = Math.Clamp(gain, -12.0, 12.0);

            // Dynamically updates individual filter parameters on the active DSP stream.
        }
    }
}
