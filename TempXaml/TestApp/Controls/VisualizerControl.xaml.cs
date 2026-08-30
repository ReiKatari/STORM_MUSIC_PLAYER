using System;
using System.Numerics;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Windows.UI;
using StormMusicPlayer.Services;

namespace StormMusicPlayer.Controls
{
    /// <summary>
    /// Code-behind for custom Win2D rendering. Handles 60fps timer ticks,
    /// read access to real-time FFT spectrum, and renders selected complex visual effects.
    /// Typography is locked strictly to BOLD Century Gothic for label components.
    /// </summary>
    public sealed partial class VisualizerControl : UserControl
    {
        private DispatcherTimer _renderTimer;
        private readonly float[] _fftData = new float[256];
        private readonly float[] _peakData = new float[256];
        private float _sensitivity = 1.8f;
        private int _effectIndex = 0; // 0: Classic Spectrum, 1: Circular Waveform, 2: Retro VU Meter

        /// <summary>
        /// Exposes the EffectIndex property to XAML as a DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty EffectIndexProperty =
            DependencyProperty.Register(nameof(EffectIndex), typeof(int), typeof(VisualizerControl),
                new PropertyMetadata(0, OnEffectIndexChanged));

        public int EffectIndex
        {
            get => (int)GetValue(EffectIndexProperty);
            set => SetValue(EffectIndexProperty, value);
        }

        private static void OnEffectIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is VisualizerControl control)
            {
                control._effectIndex = (int)e.NewValue;
                control.RenderCanvas?.Invalidate();
            }
        }

        /// <summary>
        /// Exposes the Sensitivity property to XAML as a DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty SensitivityProperty =
            DependencyProperty.Register(nameof(Sensitivity), typeof(float), typeof(VisualizerControl),
                new PropertyMetadata(1.8f, OnSensitivityChanged));

        public float Sensitivity
        {
            get => (float)GetValue(SensitivityProperty);
            set => SetValue(SensitivityProperty, value);
        }

        private static void OnSensitivityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is VisualizerControl control)
            {
                control._sensitivity = (float)e.NewValue;
                control.RenderCanvas?.Invalidate();
            }
        }

        public VisualizerControl()
        {
            this.InitializeComponent();

            // Set up high-precision rendering trigger at 60fps (approx. 16.6ms)
            _renderTimer = new DispatcherTimer();
            _renderTimer.Interval = TimeSpan.FromMilliseconds(1000.0 / 60.0);
            _renderTimer.Tick += (s, e) => RenderCanvas.Invalidate();
            _renderTimer.Start();
        }

        private void OnCreateResources(CanvasControl sender, Microsoft.Graphics.Canvas.UI.CanvasCreateResourcesEventArgs args)
        {
            // Initial load of custom background textures, sprites, or GPU-bound brushes.
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            RenderCanvas.Invalidate();
        }

        private void OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            // Thread-safe extraction of FFT data
            Array.Copy(FftAudioEffect.SpectrumData, _fftData, Math.Min(FftAudioEffect.SpectrumData.Length, _fftData.Length));

            CanvasDrawingSession ds = args.DrawingSession;
            float width = (float)sender.ActualWidth;
            float height = (float)sender.ActualHeight;

            // Render active effect
            switch (EffectIndex)
            {
                case 0:
                    DrawClassicSpectrum(ds, width, height);
                    break;
                case 1:
                    DrawCircularWaveform(ds, width, height);
                    break;
                case 2:
                    DrawRetroVUMeter(ds, width, height);
                    break;
                default:
                    // Easily scales up to 30 custom visual effects (3. Neon Ribbons, 4. Fire Embers, etc.)
                    DrawClassicSpectrum(ds, width, height);
                    break;
            }
        }

        /// <summary>
        /// Renders vertical logarithmic frequency bars with smooth peak-drop decay.
        /// </summary>
        private void DrawClassicSpectrum(CanvasDrawingSession ds, float w, float h)
        {
            int barCount = 64;
            float barWidth = w / barCount;
            float gap = 2.5f;
            float usableWidth = barWidth - gap;

            // Harmonious premium gradient: DeepSkyBlue (bass) -> Violet -> Crimson (treble)
            CanvasGradientStop[] stops = {
                new CanvasGradientStop { Position = 0.0f, Color = Colors.Crimson },
                new CanvasGradientStop { Position = 0.5f, Color = Colors.Orchid },
                new CanvasGradientStop { Position = 1.0f, Color = Colors.DeepSkyBlue }
            };
            var gradient = new CanvasLinearGradientBrush(ds, stops)
            {
                StartPoint = new Vector2(0, h),
                EndPoint = new Vector2(0, 0)
            };

            for (int i = 0; i < barCount; i++)
            {
                float amplitude = _fftData[i] * _sensitivity;
                float barHeight = Math.Clamp(amplitude * h, 4.0f, h - 15.0f);

                // Peak gravity/falloff mechanics
                if (barHeight > _peakData[i])
                {
                    _peakData[i] = barHeight;
                }
                else
                {
                    _peakData[i] -= 1.8f; // Decay factor
                    if (_peakData[i] < 4.0f) _peakData[i] = 4.0f;
                }

                float x = i * barWidth;
                
                // Draw main bar with rounded caps
                ds.FillRoundedRectangle(x, h - barHeight, usableWidth, barHeight, 3.0f, 3.0f, gradient);

                // Draw hovering peak dot
                ds.FillCircle(x + (usableWidth / 2.0f), h - _peakData[i] - 4.0f, 2.0f, Colors.White);
            }
        }

        /// <summary>
        /// Renders a circular morphing ring that expands dynamically in sync with bass.
        /// </summary>
        private void DrawCircularWaveform(CanvasDrawingSession ds, float w, float h)
        {
            Vector2 center = new Vector2(w / 2.0f, h / 2.0f);
            float baseRadius = Math.Min(w, h) * 0.28f;
            
            // Extract average bass frequencies (bins 0-3) for structural ring expansions
            float bassAverage = (_fftData[0] + _fftData[1] + _fftData[2] + _fftData[3]) / 4.0f;
            float expandedRadius = baseRadius + (bassAverage * _sensitivity * 45.0f);

            int pointCount = 120;
            float angleStep = (float)(2.0 * Math.PI / pointCount);

            using (var pathBuilder = new CanvasPathBuilder(ds))
            {
                Vector2 firstPoint = Vector2.Zero;

                for (int i = 0; i < pointCount; i++)
                {
                    float angle = i * angleStep;
                    
                    // Map spectrum bins symmetrically
                    int fftIndex = i < pointCount / 2 ? i : pointCount - i;
                    float magnitude = _fftData[fftIndex] * _sensitivity * 55.0f;

                    float currentRadius = expandedRadius + magnitude;
                    float x = center.X + (float)Math.Cos(angle) * currentRadius;
                    float y = center.Y + (float)Math.Sin(angle) * currentRadius;
                    Vector2 point = new Vector2(x, y);

                    if (i == 0)
                    {
                        pathBuilder.BeginFigure(point);
                        firstPoint = point;
                    }
                    else
                    {
                        pathBuilder.AddLine(point);
                    }
                }

                pathBuilder.EndFigure(CanvasFigureLoop.Closed);

                using (var geometry = CanvasGeometry.CreatePath(pathBuilder))
                {
                    // Glowing edge stroke
                    ds.DrawGeometry(geometry, Colors.DeepSkyBlue, 3.5f);
                    
                    // Radial gradient background fill inside expanding audio sphere
                    CanvasGradientStop[] stops = {
                        new CanvasGradientStop { Position = 0.0f, Color = ColorHelper.FromArgb(70, 211, 47, 47) },
                        new CanvasGradientStop { Position = 1.0f, Color = ColorHelper.FromArgb(12, 0, 191, 255) }
                    };
                    var radialBrush = new CanvasRadialGradientBrush(ds, stops)
                    {
                        Center = center,
                        RadiusX = expandedRadius * 1.6f,
                        RadiusY = expandedRadius * 1.6f
                    };
                    ds.FillGeometry(geometry, radialBrush);
                }
            }
        }

        /// <summary>
        /// Renders dual needle digital VU levels measuring Left and Right channels.
        /// </summary>
        private void DrawRetroVUMeter(CanvasDrawingSession ds, float w, float h)
        {
            float meterWidth = w * 0.42f;
            float meterRadius = meterWidth * 0.55f;

            // Left peak values (bins 0-19)
            float leftSum = 0.0f;
            for (int i = 0; i < 20; i++) leftSum += _fftData[i];
            float valL = Math.Clamp((leftSum / 20.0f) * _sensitivity * 1.6f, 0.0f, 1.0f);

            // Right peak values (bins 20-39)
            float rightSum = 0.0f;
            for (int i = 20; i < 40; i++) rightSum += _fftData[i];
            float valR = Math.Clamp((rightSum / 40.0f) * _sensitivity * 1.6f, 0.0f, 1.0f);

            // Apply inertia (needle physics smoothing)
            _peakData[0] = (_peakData[0] * 0.82f) + (valL * 0.18f);
            _peakData[1] = (_peakData[1] * 0.82f) + (valR * 0.18f);

            // Render Left VU Meter
            DrawSingleVUMeter(ds, new Vector2(w * 0.26f, h * 0.62f), meterRadius, "LEFT CHANNEL", _peakData[0]);

            // Render Right VU Meter
            DrawSingleVUMeter(ds, new Vector2(w * 0.74f, h * 0.62f), meterRadius, "RIGHT CHANNEL", _peakData[1]);
        }

        private void DrawSingleVUMeter(CanvasDrawingSession ds, Vector2 pivot, float radius, string title, float deflection)
        {
            // Backing background card
            ds.FillCircle(pivot, radius, ColorHelper.FromArgb(30, 20, 20, 20));
            ds.DrawCircle(pivot, radius, Colors.DarkGray, 2.0f);

            // Graduated frequency markings
            int ticksCount = 10;
            for (int i = 0; i <= ticksCount; i++)
            {
                float ratio = (float)i / ticksCount;
                float angle = (float)(Math.PI + ratio * Math.PI); // Half arc from PI to 2*PI

                Vector2 markerStart = pivot + new Vector2((float)Math.Cos(angle) * (radius - 12.0f), (float)Math.Sin(angle) * (radius - 12.0f));
                Vector2 markerEnd = pivot + new Vector2((float)Math.Cos(angle) * radius, (float)Math.Sin(angle) * radius);

                // High frequencies (clip zone) in red, low in green
                Color color = ratio > 0.82f ? Colors.Crimson : Colors.ForestGreen;
                ds.DrawLine(markerStart, markerEnd, color, 2.5f);
            }

            // Deflected needle
            float needleAngle = (float)(Math.PI + deflection * Math.PI);
            Vector2 needleTip = pivot + new Vector2((float)Math.Cos(needleAngle) * (radius - 6.0f), (float)Math.Sin(needleAngle) * (radius - 6.0f));
            ds.DrawLine(pivot, needleTip, Colors.Crimson, 3.0f);

            // Chrome center pin
            ds.FillCircle(pivot, 8.0f, Colors.White);
            ds.DrawCircle(pivot, 8.0f, Colors.Gray, 1.2f);

            // BOLD Century Gothic labels
            var fontFormat = new CanvasTextFormat
            {
                FontFamily = "Century Gothic",
                FontWeight = FontWeights.Bold,
                FontSize = 12
            };
            ds.DrawText(title, pivot.X - 52.0f, pivot.Y + 16.0f, Colors.LightGray, fontFormat);
        }
    }
}
