using System;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using StormMusicPlayer.Contracts.Services;
using StormMusicPlayer.Services;
using StormMusicPlayer.Views;

namespace StormMusicPlayer
{
    /// <summary>
    /// Represents the main Entry Point class of the STORM MUSIC PLAYER application.
    /// Operates DI service bindings and triggers shell window launch.
    /// </summary>
    public sealed partial class App : Application
    {
        private Window? _mainWindow;

        public static MainWindow? MainWindowInstance { get; private set; }

        public Window? MainWindow => _mainWindow;

        /// <summary>
        /// Gets the current App instance cast to the specific Application implementation.
        /// </summary>
        public static new App Current => (App)Application.Current;

        /// <summary>
        /// Gets the service locator provider for DI resolved instances.
        /// </summary>
        public IServiceProvider Services { get; }

        public App()
        {
            try
            {
                System.IO.File.AppendAllText("E:\\STORM MUSIC PLAYER\\StormMusicPlayer\\StormMusicPlayer\\test.txt", "App constructor entered\n");
                this.InitializeComponent();
                System.IO.File.AppendAllText("E:\\STORM MUSIC PLAYER\\StormMusicPlayer\\StormMusicPlayer\\test.txt", "App.InitializeComponent completed\n");

                // Configure DI container
                var services = new ServiceCollection();

                // Core Playback & Equalizer services registered as Singletons
                services.AddSingleton<IAudioPlaybackService, AudioPlaybackService>();
                services.AddSingleton<IEqualizerService, EqualizerService>();
                services.AddSingleton<IPlaylistService, PlaylistService>();

                Services = services.BuildServiceProvider();
                System.IO.File.AppendAllText("E:\\STORM MUSIC PLAYER\\StormMusicPlayer\\StormMusicPlayer\\test.txt", "App DI Services built\n");
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText("E:\\STORM MUSIC PLAYER\\StormMusicPlayer\\StormMusicPlayer\\test.txt", $"Exception in App constructor: {ex}\n");
                throw;
            }
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            try
            {
                var queue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
                System.IO.File.AppendAllText("E:\\STORM MUSIC PLAYER\\StormMusicPlayer\\StormMusicPlayer\\test.txt", $"OnLaunched entered. Queue Null: {queue == null}\n");
                _mainWindow = new MainWindow();
                MainWindowInstance = (MainWindow)_mainWindow;
                System.IO.File.AppendAllText("E:\\STORM MUSIC PLAYER\\StormMusicPlayer\\StormMusicPlayer\\test.txt", "MainWindow created\n");
                _mainWindow.Activate();
                System.IO.File.AppendAllText("E:\\STORM MUSIC PLAYER\\StormMusicPlayer\\StormMusicPlayer\\test.txt", "MainWindow activated\n");
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText("E:\\STORM MUSIC PLAYER\\StormMusicPlayer\\StormMusicPlayer\\test.txt", $"Exception in OnLaunched: {ex}\n");
                throw;
            }
        }
    }
}
