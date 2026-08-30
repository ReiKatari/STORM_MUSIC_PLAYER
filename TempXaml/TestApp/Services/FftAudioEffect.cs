using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.Media.Effects;
using Windows.Media.MediaProperties;
using StormMusicPlayer.Common;

namespace StormMusicPlayer.Services
{
    /// <summary>
    /// Captures raw audio frames directly inside the Windows Media Player pipeline.
    /// Performs Hann windowing and FFT to feed visualizers with minimal latency.
    /// </summary>
    [Guid("8F77B5C3-CE30-4C3D-B1CD-AFCD7603B6FF")]
    public sealed class FftAudioEffect : IBasicAudioEffect
    {
        private const int FftSize = 512; // Bins size must be a power of 2. Exposes 256 frequency bands.
        private readonly float[] _fftReal = new float[FftSize];
        private readonly float[] _fftImag = new float[FftSize];
        private readonly float[] _sampleBuffer = new float[FftSize];
        private int _sampleCount = 0;

        /// <summary>
        /// Global thread-safe spectrum buffer read by the VisualizerControl.
        /// Elements range from 0.0f (silent) to 1.0f (full amplitude at specific band).
        /// </summary>
        public static readonly float[] SpectrumData = new float[FftSize / 2];
        private static readonly object BufferLock = new object();

        public bool UseIndependentDelay => false;
        public bool UseInputFrameForOutput => true;

        public IReadOnlyList<AudioEncodingProperties> SupportedEncodingProperties => new List<AudioEncodingProperties>
        {
            CreateFloatEncodingProperties(2, 44100),
            CreateFloatEncodingProperties(2, 48000)
        };

        private AudioEncodingProperties CreateFloatEncodingProperties(uint channels, uint sampleRate)
        {
            var props = AudioEncodingProperties.CreatePcm(sampleRate, channels, 32);
            props.Subtype = MediaEncodingSubtypes.Float;
            return props;
        }

        public void SetEncodingProperties(AudioEncodingProperties encodingProperties)
        {
            // Can be used to read samplerate or channel counts dynamically
        }

        /// <summary>
        /// Callback executed by Windows Media Session containing PCM audio frames.
        /// Extracts samples, averages stereo channels, computes FFT, and writes results.
        /// </summary>
        public void ProcessFrame(ProcessAudioFrameContext context)
        {
            AudioFrame inputFrame = context.InputFrame;
            using (AudioBuffer inputBuffer = inputFrame.LockBuffer(AudioBufferAccessMode.Read))
            using (IMemoryBufferReference reference = inputBuffer.CreateReference())
            {
                unsafe
                {
                    byte* dataInBytes;
                    uint capacity;
                    ((IMemoryBufferByteAccess)reference).GetBuffer(out dataInBytes, out capacity);

                    float* dataInFloat = (float*)dataInBytes;
                    int samplesAvailable = (int)(inputBuffer.Length / sizeof(float));

                    // Samples in IEEE Float format are interleaved: Left, Right, Left, Right
                    for (int i = 0; i < samplesAvailable; i += 2)
                    {
                        if (i >= samplesAvailable) break;

                        float monoSample = dataInFloat[i]; // Left Channel
                        if (i + 1 < samplesAvailable)
                        {
                            monoSample = (monoSample + dataInFloat[i + 1]) * 0.5f; // Average channels to Mono
                        }

                        _sampleBuffer[_sampleCount] = monoSample;
                        _sampleCount++;

                        if (_sampleCount >= FftSize)
                        {
                            Array.Copy(_sampleBuffer, _fftReal, FftSize);
                            Array.Clear(_fftImag, 0, FftSize);

                            // Apply Hann Window to reduce spectral leakage
                            for (int w = 0; w < FftSize; w++)
                            {
                                float hann = 0.5f * (1.0f - (float)Math.Cos(2.0 * Math.PI * w / (FftSize - 1)));
                                _fftReal[w] *= hann;
                            }

                            // Compute Radix-2 FFT
                            FftHelper.FFT(_fftReal, _fftImag);

                            // Update SpectrumData
                            lock (BufferLock)
                            {
                                for (int k = 0; k < FftSize / 2; k++)
                                {
                                    float mag = (float)Math.Sqrt(_fftReal[k] * _fftReal[k] + _fftImag[k] * _fftImag[k]);
                                    
                                    // Smooth scaling using logarithmic decibels
                                    float db = 20.0f * (float)Math.Log10(mag + 1e-5f);
                                    float normalized = Math.Max(0.0f, (db + 60.0f) / 60.0f); // Map range [-60dB, 0dB] to [0.0, 1.0]

                                    // Apply simple lowpass temporal smoothing (decay)
                                    SpectrumData[k] = (SpectrumData[k] * 0.35f) + (normalized * 0.65f);
                                }
                            }

                            _sampleCount = 0;
                        }
                    }
                }
            }
        }

        public void Close(MediaEffectClosedReason reason) { }
        public void DiscardQueuedFrames() { _sampleCount = 0; }
        public void SetProperties(IPropertySet configuration) { }
    }

    /// <summary>
    /// Provides access to the underlying unsafe byte array backing WinRT Memory Buffers.
    /// </summary>
    [ComImport]
    [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMemoryBufferByteAccess
    {
        unsafe void GetBuffer(out byte* buffer, out uint capacity);
    }
}
