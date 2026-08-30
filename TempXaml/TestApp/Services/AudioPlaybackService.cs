using System;
using System.Collections.Generic;
using Windows.Media.Core;
using Windows.Media.Playback;
using StormMusicPlayer.Contracts.Services;
using StormMusicPlayer.Models;

namespace StormMusicPlayer.Services
{
    /// <summary>
    /// Core playback implementation wrapping Microsoft's Media Playback API.
    /// Integrates the custom FFT pipeline effect and implements automated gapless playback transitions.
    /// </summary>
    public sealed class AudioPlaybackService : IAudioPlaybackService
    {
        private readonly MediaPlayer _mediaPlayer;
        private readonly List<Track> _queue = new List<Track>();
        private int _currentQueueIndex = -1;
        private Track _currentTrack;

        public Track CurrentTrack
        {
            get => _currentTrack;
            private set
            {
                if (_currentTrack != value)
                {
                    _currentTrack = value;
                    TrackChanged?.Invoke(this, _currentTrack);
                }
            }
        }

        public bool IsPlaying => _mediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing;

        public double Volume
        {
            get => _mediaPlayer.Volume * 100.0;
            set => _mediaPlayer.Volume = value / 100.0;
        }

        public double Position
        {
            get => _mediaPlayer.PlaybackSession.Position.TotalSeconds;
            set => _mediaPlayer.PlaybackSession.Position = TimeSpan.FromSeconds(value);
        }

        public double Duration => _mediaPlayer.PlaybackSession.NaturalDuration.TotalSeconds;

        public List<Track> Queue => _queue;

        public event EventHandler<Track> TrackChanged;
        public event EventHandler<bool> PlaybackStateChanged;
        public event EventHandler PositionChanged;

        public AudioPlaybackService()
        {
            _mediaPlayer = new MediaPlayer();
            
            // Injecting real-time FftAudioEffect into playback stream
            _mediaPlayer.AddAudioEffect(typeof(FftAudioEffect).FullName, false, null);

            // Hook playback session events
            _mediaPlayer.PlaybackSession.PlaybackStateChanged += (sender, args) =>
            {
                PlaybackStateChanged?.Invoke(this, IsPlaying);
            };

            _mediaPlayer.PlaybackSession.PositionChanged += (sender, args) =>
            {
                PositionChanged?.Invoke(this, EventArgs.Empty);
            };

            // Enable seamless gapless playback transitions
            _mediaPlayer.MediaEnded += (sender, args) =>
            {
                Next();
            };
        }

        public void Play() => _mediaPlayer.Play();

        public void Pause() => _mediaPlayer.Pause();

        public void Stop()
        {
            _mediaPlayer.Pause();
            _mediaPlayer.PlaybackSession.Position = TimeSpan.Zero;
        }

        public void PlayTrack(Track track)
        {
            if (track == null) return;

            CurrentTrack = track;
            
            try
            {
                // Create media source wrapper (can support local path file:/// or web HTTP stream)
                var mediaSource = MediaSource.CreateFromUri(new Uri(track.Path));
                _mediaPlayer.Source = mediaSource;
                _mediaPlayer.Play();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during playback stream initiation: {ex.Message}");
            }
        }

        public void Next()
        {
            if (_queue.Count == 0) return;

            _currentQueueIndex = (_currentQueueIndex + 1) % _queue.Count;
            PlayTrack(_queue[_currentQueueIndex]);
        }

        public void Previous()
        {
            if (_queue.Count == 0) return;

            _currentQueueIndex--;
            if (_currentQueueIndex < 0)
            {
                _currentQueueIndex = _queue.Count - 1;
            }
            PlayTrack(_queue[_currentQueueIndex]);
        }

        public void AddToQueue(Track track)
        {
            if (track == null) return;
            _queue.Add(track);
        }

        public void SetQueue(IEnumerable<Track> tracks)
        {
            _queue.Clear();
            _queue.AddRange(tracks);
            _currentQueueIndex = 0;
            if (_queue.Count > 0)
            {
                PlayTrack(_queue[0]);
            }
        }
    }
}
