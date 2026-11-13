using SadViewer.ViewModels;
using System.Windows;

namespace SadViewer
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            DispatcherUnhandledException += (s, ex) =>
            {
                try { (MainWindow?.DataContext as MainViewModel)?.Shutdown(); } catch { }
                MessageBox.Show(ex.Exception.ToString(), "Error");
                ex.Handled = true;
            };
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try { (MainWindow?.DataContext as MainViewModel)?.Shutdown(); } catch { }
            base.OnExit(e);
        }
    }
}