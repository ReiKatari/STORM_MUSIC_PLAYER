using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using StormMusicPlayer.Contracts.Services;
using StormMusicPlayer.Models;

namespace StormMusicPlayer.Views
{
    /// <summary>
    /// Code-behind managing events, timeline progression sliders, volume updates, background visualizers, and responsive layouts.
    /// </summary>
    public sealed partial class NowPlayingPage : Page
    {
        private readonly IAudioPlaybackService _playbackService;
        private readonly DispatcherTimer _progressTimer;
        private bool _isUpdatingFromTimer = false;

        public NowPlayingPage()
        {
            this.InitializeComponent();

            _playbackService = App.Current.Services.GetRequiredService<IAudioPlaybackService>();

            // Setup smooth position timer
            _progressTimer = new DispatcherTimer();
            _progressTimer.Interval = TimeSpan.FromMilliseconds(250);
            _progressTimer.Tick += OnProgressTimerTick;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Subscribe to playback events
            _playbackService.TrackChanged += OnTrackChanged;
            _playbackService.PlaybackStateChanged += OnPlaybackStateChanged;

            // Sync initial states
            SyncTrackInfo(_playbackService.CurrentTrack);
            SyncPlaybackState(_playbackService.IsPlaying);
            SyncShuffleRepeatState();

            // Set initial volume sliders
            WideVolumeSlider.Value = _playbackService.Volume;
            NarrowVolumeSlider.Value = _playbackService.Volume;

            // Sync visualizer picker selection
            WideVisualizerPicker.SelectedIndex = ActiveVisualizer.EffectIndex;
            NarrowVisualizerPicker.SelectedIndex = ActiveVisualizer.EffectIndex;

            if (_playbackService.IsPlaying)
            {
                _progressTimer.Start();
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            // Unsubscribe to avoid memory leaks
            _playbackService.TrackChanged -= OnTrackChanged;
            _playbackService.PlaybackStateChanged -= OnPlaybackStateChanged;
            _progressTimer.Stop();
        }

        private void OnProgressTimerTick(object? sender, object e)
        {
            if (_playbackService.CurrentTrack != null)
            {
                double currentSec = _playbackService.Position;
                double totalSec = _playbackService.Duration;

                if (totalSec > 0)
                {
                    _isUpdatingFromTimer = true;
                    WideTimelineSlider.Maximum = totalSec;
                    NarrowTimelineSlider.Maximum = totalSec;
                    WideTimelineSlider.Value = currentSec;
                    NarrowTimelineSlider.Value = currentSec;
                    _isUpdatingFromTimer = false;
                }

                string formattedTime = FormatTime(currentSec);
                string formattedTotal = FormatTime(totalSec);
                
                WideCurrentTimeText.Text = formattedTime;
                NarrowCurrentTimeText.Text = formattedTime;
                WideTotalTimeText.Text = formattedTotal;
                NarrowTotalTimeText.Text = formattedTotal;
            }
        }

        private void SyncTrackInfo(Track? track)
        {
            if (track != null)
            {
                string titleUpper = track.Title.ToUpper();
                string artistUpper = track.Artist.ToUpper();

                WideTrackTitleText.Text = titleUpper;
                NarrowTrackTitleText.Text = titleUpper;

                WideArtistNameText.Text = artistUpper;
                NarrowArtistNameText.Text = artistUpper;

                double duration = track.DurationInSeconds > 0 ? track.DurationInSeconds : _playbackService.Duration;
                double maxVal = duration > 0 ? duration : 100;

                WideTimelineSlider.Maximum = maxVal;
                NarrowTimelineSlider.Maximum = maxVal;

                double currentPos = _playbackService.Position;
                WideTimelineSlider.Value = currentPos;
                NarrowTimelineSlider.Value = currentPos;

                string currentPosFormatted = FormatTime(currentPos);
                string durationFormatted = FormatTime(duration);

                WideCurrentTimeText.Text = currentPosFormatted;
                NarrowCurrentTimeText.Text = currentPosFormatted;
                WideTotalTimeText.Text = durationFormatted;
                NarrowTotalTimeText.Text = durationFormatted;

                // Dynamic cover art image loading with clean fallback mechanics
                if (!string.IsNullOrEmpty(track.CoverImagePath))
                {
                    try
                    {
                        var uri = new Uri(track.CoverImagePath, UriKind.RelativeOrAbsolute);
                        var bitmap = new BitmapImage(uri);
                        
                        WideCoverImage.Source = bitmap;
                        NarrowCoverImage.Source = bitmap;
                        
                        WideCoverImage.Visibility = Visibility.Visible;
                        NarrowCoverImage.Visibility = Visibility.Visible;
                        
                        WideDefaultCoverIcon.Visibility = Visibility.Collapsed;
                        NarrowDefaultCoverIcon.Visibility = Visibility.Collapsed;
                    }
                    catch
                    {
                        FallbackCover();
                    }
                }
                else
                {
                    FallbackCover();
                }
            }
            else
            {
                WideTrackTitleText.Text = "ШТОРМОВОЙ ВЕК";
                NarrowTrackTitleText.Text = "ШТОРМОВОЙ ВЕК";

                WideArtistNameText.Text = "STORM SYSTEM";
                NarrowArtistNameText.Text = "STORM SYSTEM";

                WideTimelineSlider.Value = 0;
                NarrowTimelineSlider.Value = 0;

                WideCurrentTimeText.Text = "0:00";
                NarrowCurrentTimeText.Text = "0:00";
                WideTotalTimeText.Text = "0:00";
                NarrowTotalTimeText.Text = "0:00";

                FallbackCover();
            }
        }

        private void FallbackCover()
        {
            WideCoverImage.Source = null;
            NarrowCoverImage.Source = null;

            WideCoverImage.Visibility = Visibility.Collapsed;
            NarrowCoverImage.Visibility = Visibility.Collapsed;

            WideDefaultCoverIcon.Visibility = Visibility.Visible;
            NarrowDefaultCoverIcon.Visibility = Visibility.Visible;
        }

        private void SyncPlaybackState(bool isPlaying)
        {
            string glyph = isPlaying ? "\uE103" : "\uE102"; // Pause vs Play MDL2 glyphs

            WidePlayPauseIcon.Glyph = glyph;
            NarrowPlayPauseIcon.Glyph = glyph;

            if (isPlaying)
            {
                _progressTimer.Start();
            }
            else
            {
                _progressTimer.Stop();
            }
        }

        private void SyncShuffleRepeatState()
        {
            var accentColor = (SolidColorBrush)Application.Current.Resources["AccentColorBrush"];
            var grayColor = new SolidColorBrush(Colors.Gray);

            Brush activeBrush = _playbackService.IsShuffleEnabled ? accentColor : grayColor;
            WideShuffleIcon.Foreground = activeBrush;
            NarrowShuffleIcon.Foreground = activeBrush;

            Brush repeatBrush = _playbackService.IsRepeatEnabled ? accentColor : grayColor;
            WideRepeatIcon.Foreground = repeatBrush;
            NarrowRepeatIcon.Foreground = repeatBrush;
        }

        private void OnTrackChanged(object? sender, Track? track)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                SyncTrackInfo(track);
            });
        }

        private void OnPlaybackStateChanged(object? sender, bool isPlaying)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                SyncPlaybackState(isPlaying);
            });
        }

        private string FormatTime(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0)
                return "0:00";

            TimeSpan t = TimeSpan.FromSeconds(seconds);
            return $"{(int)t.TotalMinutes}:{t.Seconds:D2}";
        }

        private void OnPlayPauseClick(object sender, RoutedEventArgs e)
        {
            if (_playbackService.IsPlaying)
            {
                _playbackService.Pause();
            }
            else
            {
                _playbackService.Play();
            }
        }

        private void OnPrevClick(object sender, RoutedEventArgs e)
        {
            _playbackService.Previous();
        }

        private void OnNextClick(object sender, RoutedEventArgs e)
        {
            _playbackService.Next();
        }

        private void OnShuffleClick(object sender, RoutedEventArgs e)
        {
            _playbackService.IsShuffleEnabled = !_playbackService.IsShuffleEnabled;
            SyncShuffleRepeatState();
        }

        private void OnRepeatClick(object sender, RoutedEventArgs e)
        {
            _playbackService.IsRepeatEnabled = !_playbackService.IsRepeatEnabled;
            SyncShuffleRepeatState();
        }

        private void OnTimelineSliderValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (!_isUpdatingFromTimer && _playbackService.CurrentTrack != null)
            {
                _playbackService.Position = e.NewValue;

                // Sync the alternate slider position
                _isUpdatingFromTimer = true;
                if (ReferenceEquals(sender, WideTimelineSlider))
                {
                    NarrowTimelineSlider.Value = e.NewValue;
                }
                else if (ReferenceEquals(sender, NarrowTimelineSlider))
                {
                    WideTimelineSlider.Value = e.NewValue;
                }
                _isUpdatingFromTimer = false;
            }
        }

        private void OnVolumeSliderValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (_playbackService != null)
            {
                _playbackService.Volume = e.NewValue;

                // Sync the alternate slider position
                if (ReferenceEquals(sender, WideVolumeSlider))
                {
                    NarrowVolumeSlider.Value = e.NewValue;
                }
                else if (ReferenceEquals(sender, NarrowVolumeSlider))
                {
                    WideVolumeSlider.Value = e.NewValue;
                }
            }
        }

        private void OnVisualizerPickerSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox picker && ActiveVisualizer != null)
            {
                int index = picker.SelectedIndex;
                if (index >= 0)
                {
                    ActiveVisualizer.EffectIndex = index;

                    // Synchronize selectors between wide and narrow cards
                    if (picker == WideVisualizerPicker && NarrowVisualizerPicker != null)
                    {
                        NarrowVisualizerPicker.SelectedIndex = index;
                    }
                    else if (picker == NarrowVisualizerPicker && WideVisualizerPicker != null)
                    {
                        WideVisualizerPicker.SelectedIndex = index;
                    }
                }
            }
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Switch layout cards based on responsive width limits (850px standard)
            if (e.NewSize.Width < 850)
            {
                WideContentGrid.Visibility = Visibility.Collapsed;
                NarrowContentGrid.Visibility = Visibility.Visible;
            }
            else
            {
                WideContentGrid.Visibility = Visibility.Visible;
                NarrowContentGrid.Visibility = Visibility.Collapsed;
            }
        }
    }
}
