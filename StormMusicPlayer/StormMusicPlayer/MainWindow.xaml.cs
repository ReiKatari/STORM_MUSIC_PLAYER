using Microsoft.UI.Xaml;
using StormMusicPlayer.Views;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace StormMusicPlayer;

/// <summary>
/// The application window. This hosts a Frame that displays pages. Add your
/// UI and logic to MainPage.xaml / MainPage.xaml.cs instead of here so you
/// can use Page features such as navigation events and the Loaded lifecycle.
/// </summary>
public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        try
        {
            System.IO.File.AppendAllText("E:\\STORM MUSIC PLAYER\\StormMusicPlayer\\StormMusicPlayer\\test.txt", "MainWindow constructor entered\n");
            InitializeComponent();
            System.IO.File.AppendAllText("E:\\STORM MUSIC PLAYER\\StormMusicPlayer\\StormMusicPlayer\\test.txt", "MainWindow InitializeComponent completed\n");

            ExtendsContentIntoTitleBar = true;
            System.IO.File.AppendAllText("E:\\STORM MUSIC PLAYER\\StormMusicPlayer\\StormMusicPlayer\\test.txt", "MainWindow ExtendsContentIntoTitleBar completed\n");
            
            SetTitleBar(AppTitleBar);
            System.IO.File.AppendAllText("E:\\STORM MUSIC PLAYER\\StormMusicPlayer\\StormMusicPlayer\\test.txt", "MainWindow SetTitleBar completed\n");

            try
            {
                AppWindow.Resize(new Windows.Graphics.SizeInt32(980, 680));
                var displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(AppWindow.Id, Microsoft.UI.Windowing.DisplayAreaFallback.Primary);
                if (displayArea != null)
                {
                    var centX = (displayArea.WorkArea.Width - AppWindow.Size.Width) / 2;
                    var centY = (displayArea.WorkArea.Height - AppWindow.Size.Height) / 2;
                    AppWindow.Move(new Windows.Graphics.PointInt32(centX, centY));
                }
            }
            catch (Exception sizeEx)
            {
                System.IO.File.AppendAllText("E:\\STORM MUSIC PLAYER\\StormMusicPlayer\\StormMusicPlayer\\test.txt", $"Exception resizing window: {sizeEx}\n");
            }

            try
            {
                AppWindow.SetIcon("Assets/AppIcon.ico");
                System.IO.File.AppendAllText("E:\\STORM MUSIC PLAYER\\StormMusicPlayer\\StormMusicPlayer\\test.txt", "MainWindow SetIcon completed\n");
            }
            catch (Exception iconEx)
            {
                System.IO.File.AppendAllText("E:\\STORM MUSIC PLAYER\\StormMusicPlayer\\StormMusicPlayer\\test.txt", $"Exception setting icon: {iconEx}\n");
            }

            // Navigate the root frame to the main page on startup.
            RootFrame.Navigate(typeof(MainPage));
            System.IO.File.AppendAllText("E:\\STORM MUSIC PLAYER\\StormMusicPlayer\\StormMusicPlayer\\test.txt", "MainWindow Navigate completed\n");
        }
        catch (Exception ex)
        {
            System.IO.File.AppendAllText("E:\\STORM MUSIC PLAYER\\StormMusicPlayer\\StormMusicPlayer\\test.txt", $"Exception in MainWindow constructor: {ex}\n");
            throw;
        }
    }
}

