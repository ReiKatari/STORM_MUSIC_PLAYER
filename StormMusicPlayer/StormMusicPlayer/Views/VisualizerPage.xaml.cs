using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace StormMusicPlayer.Views
{
    /// <summary>
    /// Code-behind managing real-time graphics controls, sensitivity tuning, and effect selections.
    /// </summary>
    public sealed partial class VisualizerPage : Page
    {
        public VisualizerPage()
        {
            this.InitializeComponent();
        }

        private void OnEffectSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PageVisualizer != null && EffectListView != null)
            {
                PageVisualizer.EffectIndex = EffectListView.SelectedIndex;
            }
        }

        private void OnSensitivityChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (PageVisualizer != null && SensitivitySlider != null)
            {
                PageVisualizer.Sensitivity = (float)SensitivitySlider.Value;
            }
        }
    }
}
