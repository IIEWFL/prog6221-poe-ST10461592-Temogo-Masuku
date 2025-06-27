using System.Windows;
using System.Windows.Threading;

namespace CybersecurityChatbot
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Show splash screen
            var splash = new SplashWindow();
            splash.Show();

            // Use a timer to close splash and show main window
            var timer = new DispatcherTimer();
            timer.Interval = System.TimeSpan.FromSeconds(4);
            timer.Tick += (s, args) => {
                timer.Stop();
                splash.Close();

                // Show main window
                var mainWindow = new MainWindow();
                this.MainWindow = mainWindow;
                mainWindow.Show();
            };
            timer.Start();
        }
    }
}