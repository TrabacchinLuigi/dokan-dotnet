using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DokanNetMirror;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class FileSystemWatcherWindow : Window
    {
        private Mirror _fs;
        public ObservableCollection<MirrorContext> Contexts { get; } = new ObservableCollection<MirrorContext>();
        public FileSystemWatcherWindow(Mirror fs)
        {
            _fs = fs ?? throw new ArgumentNullException(nameof(fs));

            Contexts.CollectionChanged += Contexts_CollectionChanged;

            _fs.ContextCreated += (x, y) =>
            {
                y.Closed += z =>
                {
                    Dispatcher.InvokeAsync(async () =>
                    {
                        if (!z.HaveErrors)
                        {
                            await Task.Delay(TimeSpan.FromSeconds(5));
                            Contexts.Remove(z);
                        }
                    });
                };
                Dispatcher.InvokeAsync(async () =>
                {
                    Contexts.Add(y);
                    await Task.Delay(TimeSpan.FromSeconds(10));

                    if (!y.HaveErrors && y.FileStream == null) Contexts.Remove(y);
                });
            };

            InitializeComponent();
        }

        private void Contexts_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
                foreach (var mc in e.OldItems.OfType<MirrorContext>())
                    mc.Dispose();
        }
    }
}
