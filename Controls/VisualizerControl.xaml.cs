using System;
using System.Numerics;
using Microsoft.UI;
using Microsoft.UI.Text;
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
        private double _sensitivity = 1.8;
        private int _effectIndex = 0;

        // Persistent High-Fidelity Physics & Particle States
        private struct VisualizerParticle
        {
            public float Angle;
            public float Radius;
            public float Speed;
            public float Size;
            public Color Color;
        }

        private VisualizerParticle[]? _orbitParticles;
        private VisualizerParticle[]? _vortexParticles;

        private float _needlePosL = 0.0f;
        private float _needlePosR = 0.0f;
        private float _needleVelL = 0.0f;
        private float _needleVelR = 0.0f;
        private float _peakLedL = 0.0f;
        private float _peakLedR = 0.0f;

        private float _rotationAngle = 0.0f;
        private float _ribbonTime = 0.0f;

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
            DependencyProperty.Register(nameof(Sensitivity), typeof(double), typeof(VisualizerControl),
                new PropertyMetadata(1.8, OnSensitivityChanged));

        public double Sensitivity
        {
            get => (double)GetValue(SensitivityProperty);
            set => SetValue(SensitivityProperty, value);
        }

        private static void OnSensitivityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is VisualizerControl control)
            {
                control._sensitivity = (double)e.NewValue;
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
                case 3:
                    DrawNeonRibbons(ds, width, height);
                    break;
                case 4:
                    DrawParticleVortex(ds, width, height);
                    break;
                default:
                    DrawClassicSpectrum(ds, width, height);
                    break;
            }
        }

        /// <summary>
        /// Renders vertical logarithmic frequency bars with beautiful glass reflections and peak gravity indicators.
        /// </summary>
        private void DrawClassicSpectrum(CanvasDrawingSession ds, float w, float h)
        {
            int barCount = 64;
            float barWidth = w / barCount;
            float gap = 2.0f;
            float usableWidth = Math.Max(1.0f, barWidth - gap);

            // Logarithmic bin layout (maps 256 bins to 64 bars)
            float[] logData = new float[barCount];
            for (int i = 0; i < barCount; i++)
            {
                int srcIndex = (int)(Math.Pow(i / (float)barCount, 1.5) * 192); // Logarithmic curve mapping
                logData[i] = _fftData[Math.Clamp(srcIndex, 0, 255)];
            }

            // Harmonious premium neon gradient: Crimson (left/highs) -> Pink -> Neon Blue (right/bass)
            CanvasGradientStop[] stops = {
                new CanvasGradientStop { Position = 0.0f, Color = Colors.Crimson },
                new CanvasGradientStop { Position = 0.5f, Color = Colors.Magenta },
                new CanvasGradientStop { Position = 1.0f, Color = Colors.DeepSkyBlue }
            };

            var gradient = new CanvasLinearGradientBrush(ds, stops)
            {
                StartPoint = new Vector2(0, h * 0.8f),
                EndPoint = new Vector2(0, 0)
            };

            float centerY = h * 0.8f; // Baseline for mirror reflections

            for (int i = 0; i < barCount; i++)
            {
                float amplitude = logData[i] * (float)_sensitivity;
                float barHeight = Math.Clamp(amplitude * centerY, 4.0f, centerY - 15.0f);

                // Peak gravity falloff mechanics
                if (barHeight > _peakData[i])
                {
                    _peakData[i] = barHeight;
                }
                else
                {
                    _peakData[i] -= 1.4f; // Falling gravity acceleration
                    if (_peakData[i] < 4.0f) _peakData[i] = 4.0f;
                }

                float x = i * barWidth;

                // 1. Draw main bar with rounded caps
                ds.FillRoundedRectangle(x, centerY - barHeight, usableWidth, barHeight, 3.0f, 3.0f, gradient);

                // 2. Draw semi-transparent glass mirror reflection
                float reflectionHeight = barHeight * 0.35f;
                var reflectionColor = ColorHelper.FromArgb((byte)(45 * (1.0f - (reflectionHeight / centerY))), Colors.Magenta.R, Colors.Magenta.G, Colors.Magenta.B);
                ds.FillRoundedRectangle(x, centerY, usableWidth, reflectionHeight, 3.0f, 3.0f, reflectionColor);

                // 3. Draw hovering glowing peak indicator
                ds.FillCircle(x + (usableWidth / 2.0f), centerY - _peakData[i] - 5.0f, 1.8f, Colors.White);
            }
        }

        /// <summary>
        /// Renders a circular breathing neon ring pulsing with bass, rotating, and surrounded by space dust.
        /// </summary>
        private void DrawCircularWaveform(CanvasDrawingSession ds, float w, float h)
        {
            Vector2 center = new Vector2(w / 2.0f, h / 2.0f);
            float baseRadius = Math.Min(w, h) * 0.26f;
            
            // Extract average bass frequencies (bins 0-3) for structural ring expansions
            float bassAverage = (_fftData[0] + _fftData[1] + _fftData[2] + _fftData[3]) / 4.0f;
            float expandedRadius = baseRadius + (bassAverage * (float)_sensitivity * 48.0f);

            // Dynamic rotation driven by bass pulse
            _rotationAngle += 0.003f + bassAverage * 0.03f;

            // Initialize orbital space dust particles lazily
            if (_orbitParticles == null)
            {
                var rand = new Random();
                _orbitParticles = new VisualizerParticle[40];
                for (int i = 0; i < _orbitParticles.Length; i++)
                {
                    _orbitParticles[i] = new VisualizerParticle
                    {
                        Angle = (float)(rand.NextDouble() * Math.PI * 2.0),
                        Radius = (float)(rand.NextDouble() * 100.0 + 130.0),
                        Speed = (float)(rand.NextDouble() * 0.015 + 0.005),
                        Size = (float)(rand.NextDouble() * 2.5 + 1.2),
                        Color = ColorHelper.FromArgb(200, 0, 191, 255)
                    };
                }
            }

            // Draw orbiting cosmic dust stars
            float trebleAverage = (_fftData[15] + _fftData[16] + _fftData[17]) / 3.0f;
            foreach (var p in _orbitParticles)
            {
                // Update speed dynamically based on high frequencies
                float angle = p.Angle + p.Speed * (1.0f + trebleAverage * 6.0f);
                float radius = p.Radius + (bassAverage * (float)_sensitivity * 25.0f);
                Vector2 pos = center + new Vector2((float)Math.Cos(angle) * radius, (float)Math.Sin(angle) * radius);
                ds.FillCircle(pos, p.Size, Colors.DeepSkyBlue);
            }

            int pointCount = 120;
            float angleStep = (float)(2.0 * Math.PI / pointCount);

            // Draw double outer waving frequency ring
            using (var pathBuilder = new CanvasPathBuilder(ds))
            {
                for (int i = 0; i < pointCount; i++)
                {
                    float angle = i * angleStep + _rotationAngle;
                    int fftIndex = i < pointCount / 2 ? i : pointCount - i;
                    float magnitude = _fftData[Math.Clamp(fftIndex, 0, 255)] * (float)_sensitivity * 55.0f;

                    float currentRadius = expandedRadius + magnitude;
                    Vector2 point = center + new Vector2((float)Math.Cos(angle) * currentRadius, (float)Math.Sin(angle) * currentRadius);

                    if (i == 0) pathBuilder.BeginFigure(point);
                    else pathBuilder.AddLine(point);
                }
                pathBuilder.EndFigure(CanvasFigureLoop.Closed);

                using (var geometry = CanvasGeometry.CreatePath(pathBuilder))
                {
                    ds.DrawGeometry(geometry, Colors.DeepSkyBlue, 3.5f);

                    // Glowing backdrop interior fill
                    CanvasGradientStop[] stops = {
                        new CanvasGradientStop { Position = 0.0f, Color = ColorHelper.FromArgb(60, 211, 47, 47) }, // pulsing red core
                        new CanvasGradientStop { Position = 1.0f, Color = ColorHelper.FromArgb(10, 0, 191, 255) }  // neon blue edge
                    };
                    var radialBrush = new CanvasRadialGradientBrush(ds, stops)
                    {
                        Center = center,
                        RadiusX = expandedRadius * 1.5f,
                        RadiusY = expandedRadius * 1.5f
                    };
                    ds.FillGeometry(geometry, radialBrush);
                }
            }

            // Draw pulsing inner solid orb
            float innerRadius = baseRadius * 0.45f + (bassAverage * (float)_sensitivity * 12.0f);
            ds.FillCircle(center, innerRadius, ColorHelper.FromArgb(200, 211, 47, 47));
            ds.DrawCircle(center, innerRadius, Colors.White, 1.5f);
        }

        /// <summary>
        /// Renders dual needle VU meters on a beautiful dark carbon-fiber backplate with reactive overload LEDs.
        /// </summary>
        private void DrawRetroVUMeter(CanvasDrawingSession ds, float w, float h)
        {
            // Carbon-fiber texture backplate (fine dark lines)
            float gridSize = 12.0f;
            for (float x = 0; x < w; x += gridSize)
            {
                ds.DrawLine(new Vector2(x, 0), new Vector2(x, h), ColorHelper.FromArgb(30, 255, 255, 255), 1.0f);
            }
            for (float y = 0; y < h; y += gridSize)
            {
                ds.DrawLine(new Vector2(0, y), new Vector2(w, y), ColorHelper.FromArgb(30, 255, 255, 255), 1.0f);
            }

            float meterWidth = w * 0.40f;
            float meterRadius = meterWidth * 0.52f;

            // Left peak values (bins 0-19)
            float leftSum = 0.0f;
            for (int i = 0; i < 20; i++) leftSum += _fftData[i];
            float valL = Math.Clamp((leftSum / 20.0f) * (float)_sensitivity * 1.8f, 0.0f, 1.0f);

            // Right peak values (bins 20-39)
            float rightSum = 0.0f;
            for (int i = 20; i < 40; i++) rightSum += _fftData[i];
            float valR = Math.Clamp((rightSum / 40.0f) * (float)_sensitivity * 1.8f, 0.0f, 1.0f);

            // Needle Physics (spring mechanics with wobbly inertia)
            float forceL = (valL - _needlePosL) * 0.40f;
            _needleVelL = (_needleVelL + forceL) * 0.82f;
            _needlePosL += _needleVelL;

            float forceR = (valR - _needlePosR) * 0.40f;
            _needleVelR = (_needleVelR + forceR) * 0.82f;
            _needlePosR += _needleVelR;

            // Overload peak LED timers
            if (valL > 0.82f) _peakLedL = 1.0f;
            else _peakLedL *= 0.90f; // slow fading

            if (valR > 0.82f) _peakLedR = 1.0f;
            else _peakLedR *= 0.90f;

            // Render Left VU Meter
            DrawSingleVUMeter(ds, new Vector2(w * 0.26f, h * 0.58f), meterRadius, "LEFT CHANNEL", _needlePosL, _peakLedL);

            // Render Right VU Meter
            DrawSingleVUMeter(ds, new Vector2(w * 0.74f, h * 0.58f), meterRadius, "RIGHT CHANNEL", _needlePosR, _peakLedR);
        }

        private void DrawSingleVUMeter(CanvasDrawingSession ds, Vector2 pivot, float radius, string title, float deflection, float ledIntensity)
        {
            // Backing background card
            ds.FillCircle(pivot, radius, ColorHelper.FromArgb(60, 20, 20, 20));
            ds.DrawCircle(pivot, radius, Colors.DarkGray, 2.5f);

            // Graduated frequency markings
            int ticksCount = 10;
            for (int i = 0; i <= ticksCount; i++)
            {
                float ratio = (float)i / ticksCount;
                float angle = (float)(Math.PI + ratio * Math.PI); // Half arc from PI to 2*PI

                Vector2 markerStart = pivot + new Vector2((float)Math.Cos(angle) * (radius - 12.0f), (float)Math.Sin(angle) * (radius - 12.0f));
                Vector2 markerEnd = pivot + new Vector2((float)Math.Cos(angle) * radius, (float)Math.Sin(angle) * radius);

                Color color = ratio > 0.80f ? Colors.Crimson : Colors.ForestGreen;
                ds.DrawLine(markerStart, markerEnd, color, 3.0f);
            }

            // Deflected needle
            float needleAngle = (float)(Math.PI + deflection * Math.PI);
            Vector2 needleTip = pivot + new Vector2((float)Math.Cos(needleAngle) * (radius - 8.0f), (float)Math.Sin(needleAngle) * (radius - 8.0f));
            ds.DrawLine(pivot, needleTip, Colors.Crimson, 3.5f);

            // Overload LED Indicator
            Vector2 ledPos = pivot + new Vector2(0, -radius * 0.4f);
            Color dimColor = ColorHelper.FromArgb(80, 100, 0, 0);
            Color litColor = Colors.Red;
            Color activeColor = ColorHelper.FromArgb(
                255,
                (byte)(dimColor.R + (litColor.R - dimColor.R) * ledIntensity),
                (byte)(dimColor.G + (litColor.G - dimColor.G) * ledIntensity),
                (byte)(dimColor.B + (litColor.B - dimColor.B) * ledIntensity)
            );
            ds.FillCircle(ledPos, 6.0f, activeColor);
            ds.DrawCircle(ledPos, 6.0f, Colors.Black, 1.0f);

            // Chrome center pin
            ds.FillCircle(pivot, 10.0f, Colors.White);
            ds.DrawCircle(pivot, 10.0f, Colors.Gray, 1.5f);

            // Bold Century Gothic labels
            var fontFormat = new CanvasTextFormat
            {
                FontFamily = "Century Gothic",
                FontWeight = FontWeights.Bold,
                FontSize = 12
            };
            ds.DrawText(title, pivot.X - 52.0f, pivot.Y + 16.0f, Colors.LightGray, fontFormat);
        }

        /// <summary>
        /// Renders three neon waves (Cyan, Magenta, Gold) snake-dancing dynamically in response to frequencies.
        /// </summary>
        private void DrawNeonRibbons(CanvasDrawingSession ds, float w, float h)
        {
            float centerY = h / 2.0f;
            float bass = (_fftData[0] + _fftData[1] + _fftData[2]) / 3.0f;
            float mids = (_fftData[10] + _fftData[11] + _fftData[12]) / 3.0f;
            float highs = (_fftData[30] + _fftData[31] + _fftData[32]) / 3.0f;

            _ribbonTime += 0.015f + bass * 0.03f;

            Color[] ribbonColors = { Colors.Cyan, Colors.Magenta, Colors.Gold };
            float[] modulations = { bass, mids, highs };
            float[] frequencies = { 0.008f, 0.012f, 0.016f };

            for (int r = 0; r < 3; r++)
            {
                using (var pathBuilder = new CanvasPathBuilder(ds))
                {
                    float currentMod = modulations[r] * (float)_sensitivity;
                    float freq = frequencies[r];
                    float phaseOffset = r * 2.0f;

                    for (float x = 0; x < w; x += 6.0f)
                    {
                        float wave = (float)Math.Sin(x * freq + _ribbonTime + phaseOffset) * (currentMod * 130.0f + 25.0f);
                        Vector2 pt = new Vector2(x, centerY + wave);

                        if (x == 0) pathBuilder.BeginFigure(pt);
                        else pathBuilder.AddLine(pt);
                    }
                    pathBuilder.EndFigure(CanvasFigureLoop.Open);

                    using (var geometry = CanvasGeometry.CreatePath(pathBuilder))
                    {
                        ds.DrawGeometry(geometry, ribbonColors[r], 2.8f);
                    }
                }
            }
        }

        /// <summary>
        /// Renders 120+ colorful stellar particles swirling around a central vortex, pulsing reactively.
        /// </summary>
        private void DrawParticleVortex(CanvasDrawingSession ds, float w, float h)
        {
            Vector2 center = new Vector2(w / 2.0f, h / 2.0f);
            float bass = (_fftData[0] + _fftData[1] + _fftData[2] + _fftData[3]) / 4.0f;

            // Initialize vortex particles lazily
            if (_vortexParticles == null)
            {
                var rand = new Random();
                _vortexParticles = new VisualizerParticle[120];
                for (int i = 0; i < _vortexParticles.Length; i++)
                {
                    _vortexParticles[i] = new VisualizerParticle
                    {
                        Angle = (float)(rand.NextDouble() * Math.PI * 2.0),
                        Radius = (float)(rand.NextDouble() * 320.0 + 15.0),
                        Speed = (float)(rand.NextDouble() * 0.018 + 0.004),
                        Size = (float)(rand.NextDouble() * 3.0 + 1.2),
                        Color = rand.Next(3) == 0 ? Colors.Cyan : (rand.Next(2) == 0 ? Colors.Magenta : Colors.Yellow)
                    };
                }
            }

            // Draw center gravity core (black hole)
            float coreRadius = 15.0f + bass * (float)_sensitivity * 18.0f;
            ds.FillCircle(center, coreRadius, ColorHelper.FromArgb(200, 15, 15, 15));
            ds.DrawCircle(center, coreRadius, Colors.White, 1.2f);

            // Draw and update swirling particles
            for (int i = 0; i < _vortexParticles.Length; i++)
            {
                var p = _vortexParticles[i];

                // Swirl speed modulated by bass
                p.Angle += p.Speed * (1.0f + bass * 4.0f);
                _vortexParticles[i].Angle = p.Angle;

                // Pulsate orbit size
                float currentRadius = p.Radius * (1.0f + bass * 0.12f);
                Vector2 pos = center + new Vector2((float)Math.Cos(p.Angle) * currentRadius, (float)Math.Sin(p.Angle) * currentRadius);

                // Draw main particle
                ds.FillCircle(pos, p.Size, p.Color);

                // Draw secondary trailing spark
                if (p.Radius > 100.0f)
                {
                    float trailAngle = p.Angle - 0.08f;
                    Vector2 trailPos = center + new Vector2((float)Math.Cos(trailAngle) * currentRadius, (float)Math.Sin(trailAngle) * currentRadius);
                    ds.FillCircle(trailPos, p.Size * 0.6f, ColorHelper.FromArgb(100, p.Color.R, p.Color.G, p.Color.B));
                }
            }
        }
    }
}
