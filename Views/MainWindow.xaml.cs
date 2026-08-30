using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using StormMusicPlayer.Views;

namespace StormMusicPlayer.Views
{
    /// <summary>
    /// Top-level shell hosting framework backdrops and primary view navigation targets.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private MicaController _micaController;
        private SystemBackdropConfiguration _backdropConfiguration;

        public MainWindow()
        {
            this.InitializeComponent();

            // Set up mica glass material background for Fluent styles on Windows 11
            TrySetMicaBackdrop();

            // Extend standard titlebar elements for unified custom top area
            ExtendsContentIntoTitleBar = true;

            // Direct route to the main layout control page
            RootFrame.Navigate(typeof(MainPage));
        }

        private void TrySetMicaBackdrop()
        {
            if (MicaController.IsSupported())
            {
                _backdropConfiguration = new SystemBackdropConfiguration();
                
                // Configure focus transitions
                this.Activated += (s, e) => _backdropConfiguration.IsInputActive = e.WindowActivationState != WindowActivationState.Deactivated;
                
                _micaController = new MicaController();
                
                // Retrieve the WinRT composition backdrop support interface
                var supportsBackdrop = this.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>();
                _micaController.AddSystemBackdropTarget(supportsBackdrop);
                _micaController.Configure(this, _backdropConfiguration);
            }
        }
    }

    /// <summary>
    /// Quick extension class to handle dynamic casting using COM or internal WinRT mappings.
    /// </summary>
    internal static class WindowExtensions
    {
        public static T As<T>(this Window window)
        {
            return (T)(object)window;
        }
    }
}
