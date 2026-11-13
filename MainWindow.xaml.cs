using SadViewer.ViewModels;
using System.Windows;
using System.Windows.Threading;

namespace SadViewer
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Closed += (_, __) => (DataContext as MainViewModel)?.Shutdown();

            if (DataContext is MainViewModel vm)
            {
                vm.LogLines.CollectionChanged += (_, __) =>
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        LogScroll?.ScrollToEnd();
                    }), DispatcherPriority.Background);
                };
            }
        }
    }
}