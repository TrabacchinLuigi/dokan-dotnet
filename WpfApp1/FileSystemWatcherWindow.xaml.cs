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
        public FileSystemWatcherWindowViewModel ViewModel { get; private set; }

        public FileSystemWatcherWindow(Mirror fs)
        {
            ViewModel = new FileSystemWatcherWindowViewModel(Dispatcher, fs);

            InitializeComponent();
        }

        private void CurrentControl_Closed(object sender, EventArgs e)
        {
            ViewModel.Dispose();
        }
    }

    public class FileSystemWatcherWindowViewModel : Notifiable, IDisposable
    {
        private System.Windows.Threading.Dispatcher _Dispatcher;
        private Mirror _fs;
        public ObservableCollection<MirrorContext> Contexts { get; private set; }

        private Int32 _Delay = 5;
        public Int32 Delay
        {
            get => _Delay;
            set
            {
                if (_Delay == value) return;
                _Delay = value;
                NotifyPropertyChanged(nameof(Delay));
            }
        }

        public FileSystemWatcherWindowViewModel(System.Windows.Threading.Dispatcher dispatcher, Mirror fs)
        {
            _Dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            _fs = fs ?? throw new ArgumentNullException(nameof(fs));

            Contexts = new ObservableCollection<MirrorContext>();
            Contexts.CollectionChanged += Contexts_CollectionChanged;
            _fs.ContextCreated += _fs_ContextCreated;
        }

        private void Contexts_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
                foreach (var mc in e.OldItems.OfType<MirrorContext>())
                    mc.Dispose();
        }

        private void _fs_ContextCreated(Mirror x, MirrorContext mirrorContext)
        {
            if (disposedValue) return;

            mirrorContext.Closed += MirrorContext_Closed;
            _Dispatcher.InvokeAsync(async () =>
            {
                Contexts.Add(mirrorContext);
                await Task.Delay(TimeSpan.FromSeconds(Delay));

                if (!mirrorContext.HaveErrors && mirrorContext.FileStream == null)
                    Contexts.Remove(mirrorContext);
            });

        }

        private void MirrorContext_Closed(MirrorContext mirrorContext)
        {
            _Dispatcher.InvokeAsync(async () =>
            {
                if (!mirrorContext.HaveErrors)
                {
                    await Task.Delay(TimeSpan.FromSeconds(Delay));
                    Contexts.Remove(mirrorContext);
                }
            });
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    _fs.ContextCreated -= _fs_ContextCreated;
                    _fs = null;
                    foreach (var c in Contexts.ToArray())
                        c.Dispose();
                    Contexts.Clear();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~FileSystemWatcherWindowViewModel() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
