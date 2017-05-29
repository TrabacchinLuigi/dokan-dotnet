using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
        private ObservableCollection<MirrorContextViewModel> _Contexts = new ObservableCollection<MirrorContextViewModel>();
        private readonly ReadOnlyObservableCollection<MirrorContextViewModel> _ROContexts;
        public ReadOnlyObservableCollection<MirrorContextViewModel> Contexts => _ROContexts;

        private Int32 _Delay = 5;
        public Int32 Delay
        {
            get => _Delay;
            set
            {
                if (_Delay == value) return;
                _Delay = value;
                NotifyPropertyChanged();
            }
        }

        private Boolean _KeepForever;
        public Boolean KeepForever
        {
            get => _KeepForever;
            set
            {
                if (_KeepForever == value) return;
                _KeepForever = value;
                NotifyPropertyChanged();
            }
        }

        public FileSystemWatcherWindowViewModel(System.Windows.Threading.Dispatcher dispatcher, Mirror fs)
        {
            _Dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            _fs = fs ?? throw new ArgumentNullException(nameof(fs));

            _ROContexts = new ReadOnlyObservableCollection<MirrorContextViewModel>(_Contexts);
            _fs.ContextCreated += _fs_ContextCreated;
        }

        private void _fs_ContextCreated(Mirror x, MirrorContext mc)
        {
            if (disposedValue) return;

            var mirrorContext = new MirrorContextViewModel(_Dispatcher, mc);

            mirrorContext.Closed += MirrorContext_Closed;
            _Dispatcher.InvokeAsync(async () =>
            {
                _Contexts.Add(mirrorContext);
                if (KeepForever) return;
                await Task.Delay(TimeSpan.FromSeconds(Delay));

                if (!mirrorContext.HaveErrors)
                    _Contexts.Remove(mirrorContext);
            });
        }

        private void MirrorContext_Closed(MirrorContextViewModel mirrorContext)
        {
            mirrorContext.Closed -= MirrorContext_Closed;
            _Dispatcher.InvokeAsync(async () =>
            {
                if (!mirrorContext.HaveErrors)
                {
                    await Task.Delay(TimeSpan.FromSeconds(Delay));
                    _Contexts.Remove(mirrorContext);
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
                    _Contexts.Clear();
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

    public class MirrorContextViewModel : Notifiable
    {
        public event Action<MirrorContextViewModel> Closed;

        private readonly System.Windows.Threading.Dispatcher _Dispatcher;
        private ObservableCollection<CallResultViewModel> _Calls = new ObservableCollection<CallResultViewModel>();
        private readonly ReadOnlyObservableCollection<CallResultViewModel> _ROCalls;
        public ReadOnlyObservableCollection<CallResultViewModel> Calls => _ROCalls;

        public Int32 ProcessId { get; private set; }
        public String IdentityName { get; private set; }
        public String FileName { get; private set; }
        public DokanNet.FileAccess Access { get; private set; }
        public FileShare Share { get; private set; }
        public FileMode Mode { get; private set; }
        public DateTimeOffset? Start => _Calls.FirstOrDefault()?.Created;
        public DateTimeOffset? End => _Calls.LastOrDefault()?.Created;

        private Boolean _HaveErrors;
        public Boolean HaveErrors
        {
            get => _HaveErrors;
            private set
            {
                if (_HaveErrors == value) return;
                _HaveErrors = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(BackGroundColor));
            }
        }

        private Boolean _IsClosed;
        public Boolean IsClosed
        {
            get => _IsClosed;
            private set
            {
                if (_IsClosed == value) return;
                _IsClosed = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(BackGroundColor));
                if (_IsClosed) Closed.Invoke(this);
            }
        }

        private Boolean _FileExisted;
        public Boolean FileExisted
        {
            get => _FileExisted;
            private set
            {
                if (_FileExisted == value) return;
                _FileExisted = value;
                NotifyPropertyChanged();
            }
        }

        private Boolean _BeenRead;
        public Boolean BeenRead
        {
            get => _BeenRead;
            private set
            {
                if (_BeenRead == value) return;
                _BeenRead = value;
                NotifyPropertyChanged();
            }
        }

        private Boolean _BeenWritten;
        public Boolean BeenWritten
        {
            get => _BeenWritten;
            private set
            {
                if (_BeenWritten == value) return;
                _BeenWritten = value;
                NotifyPropertyChanged();
            }
        }

        public SolidColorBrush BackGroundColor
        {
            get
            {
                if (!HaveErrors && !IsClosed) return Brushes.Transparent;
                else if (HaveErrors && !IsClosed) return Brushes.Red;
                else if (HaveErrors && IsClosed) return Brushes.Orange;
                else if (!HaveErrors && IsClosed) return Brushes.Yellow;
                else return Brushes.HotPink;
            }
        }

        public MirrorContextViewModel()
        {
            _ROCalls = new ReadOnlyObservableCollection<CallResultViewModel>(_Calls);
        }

        public MirrorContextViewModel(System.Windows.Threading.Dispatcher dispatcher, MirrorContext mc) : this()
        {
            _Dispatcher = dispatcher;
            ProcessId = mc.ProcessId;
            IdentityName = mc.IdentityName;
            FileName = mc.FileName;

            Mode = mc.Mode;
            Access = mc.Access;
            Share = mc.Share;
            mc.CallResultAdded += Mc_CallResultAdded;
        }

        private void Mc_CallResultAdded(MirrorContext sender, CallResult cr)
        {
            _Dispatcher.Invoke(() =>
            {
                var lastCall = _Calls.LastOrDefault();

                if (lastCall != null && lastCall.Method == cr.Method && lastCall.Result == cr.Result && lastCall.Exception?.GetType() == cr.Exception?.GetType())
                {
                    lastCall.Times++;
                }
                else
                {
                    _Calls.Add(new CallResultViewModel(cr));
                    if (cr.Exception != null) HaveErrors = true;

                    if (
                        cr.Method == nameof(Mirror.CloseFile) ||
                         (cr.Method == nameof(Mirror.CreateFile) && (cr.Result == DokanNet.DokanResult.FileNotFound || cr.Result == DokanNet.DokanResult.AccessDenied))
                    )
                    {
                        IsClosed = true;
                        sender.CallResultAdded -= Mc_CallResultAdded;
                    };

                    if (cr.Method == nameof(Mirror.CreateFile) && cr.Result != DokanNet.DokanResult.FileNotFound)
                    {
                        FileExisted = true;
                    }

                    if (cr.Method == nameof(Mirror.WriteFile)) BeenWritten = true;
                    if (cr.Method == nameof(Mirror.ReadFile)) BeenRead = true;
                }
            });
        }
    }

    public class CallResultViewModel : Notifiable
    {
        public DateTimeOffset Created { get; private set; }
        public string Method { get; set; }
        public DokanNet.NtStatus? Result { get; set; }
        public Exception Exception { get; set; }

        private UInt32 _Times = 1;
        public UInt32 Times
        {
            get => _Times;
            set
            {
                if (Times == value) return;
                _Times = value;
                NotifyPropertyChanged();
            }
        }

        public CallResultViewModel() { }

        public CallResultViewModel(CallResult cr) : this()
        {
            Created = cr.Created;
            Method = cr.Method;
            Result = cr.Result;
            Exception = cr.Exception;
        }
    }
}
