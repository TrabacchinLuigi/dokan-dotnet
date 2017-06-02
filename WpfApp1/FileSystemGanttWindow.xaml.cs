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
using System.Windows.Shapes;
using DokanNetMirror;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class FileSystemGanttWindow : Window
    {
        public FileSystemGanttWindowViewModel ViewModel { get; private set; }
        public FileSystemGanttWindow(Mirror fs)
        {
            ViewModel = new FileSystemGanttWindowViewModel(Dispatcher, fs);
            InitializeComponent();
        }

        public class FileSystemGanttWindowViewModel : Notifiable
        {
            private System.Windows.Threading.Dispatcher _Dispatcher;
            private Mirror _fs;
            private ObservableCollection<BaseGanttRow> _GanttRows = new ObservableCollection<BaseGanttRow>();
            public ReadOnlyObservableCollection<BaseGanttRow> GanttRows { get; private set; }

            public FileSystemGanttWindowViewModel(System.Windows.Threading.Dispatcher dispatcher, Mirror fs)
            {
                _Dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
                _fs = fs ?? throw new ArgumentNullException(nameof(fs));
                GanttRows = new ReadOnlyObservableCollection<BaseGanttRow>(_GanttRows);
                _fs.ContextCreated += _fs_ContextCreated;

                //var now = DateTime.Now;
                //var midnight = new DateTime(now.Year, now.Month, now.Day);

                //var row1 = new ExpandableGanttRow() { Title = "riga1" };
                //var subrow11 = new GanttRow() { Title = "sottoriga1" };
                //subrow11.Segments.Add(new TimeSegment(midnight.AddHours(1), midnight.AddHours(3)));
                //subrow11.Segments.Add(new TimeSegment(midnight.AddHours(3), midnight.AddHours(6)));
                //row1.SubRows.Add(subrow11);
                //var subrow12 = new GanttRow() { Title = "sottoriga2" };
                //subrow12.Segments.Add(new TimeSegment(midnight.AddHours(5).AddMinutes(30), midnight.AddHours(8)));
                //subrow12.Segments.Add(new TimeSegment(midnight.AddHours(8), midnight.AddHours(11).AddMinutes(30)));
                //row1.SubRows.Add(subrow12);
                //_GanttRows.Add(row1);

                //var row2 = new ExpandableGanttRow() { Title = "riga2" };
                //var subrow21 = new GanttRow() { Title = "sottoriga1" };
                //subrow21.Segments.Add(new TimeSegment(midnight.AddHours(5), midnight.AddHours(9)));
                //subrow21.Segments.Add(new TimeSegment(midnight.AddHours(9), midnight.AddHours(13)));
                //row2.SubRows.Add(subrow21);
                //var subrow22 = new GanttRow() { Title = "sottoriga2" };
                //subrow22.Segments.Add(new TimeSegment(midnight.AddHours(12).AddMinutes(30), midnight.AddHours(14)));
                //subrow22.Segments.Add(new TimeSegment(midnight.AddHours(14), midnight.AddHours(18).AddMinutes(30)));
                //row2.SubRows.Add(subrow22);

                //_GanttRows.Add(row2);
            }

            private void _fs_ContextCreated(Mirror fs, MirrorContext context)
            {
                _Dispatcher.InvokeAsync(() =>
                {
                    context.CallResultAdded += Context_CallResultAdded;
                    var existingRow = GanttRows.OfType<ExpandableGanttRow>().SingleOrDefault(x => String.Equals(x.Title, context.FileName, StringComparison.OrdinalIgnoreCase));
                    if (existingRow == null)
                    {
                        existingRow = new ExpandableGanttRow() { Title = context.FileName, Context = context.FileName };
                        _GanttRows.Add(existingRow);
                    }
                    var contextRow = new GanttRow() { Title = context.Id.ToString(), Context = context };
                    existingRow.SubRows.Add(contextRow);
                });
            }

            private void Context_CallResultAdded(MirrorContext mc, CallResult cr)
            {
                _Dispatcher.InvokeAsync(() =>
                {
                    var existingParentRow = _GanttRows.OfType<ExpandableGanttRow>().SingleOrDefault(x => String.Equals(x.Title, mc.FileName));
                    if (existingParentRow == null) return;
                    var existingRow = existingParentRow.SubRows.SingleOrDefault(x => x.Context == mc);
                    if (existingRow == null) return;
                    existingRow.Segments.Add(new TimeSegment(cr.Created.DateTime, cr.Ended.DateTime) { Title = cr.Method, BackGround = GetColorForCallResult(cr) });
                });
            }

            private Brush GetColorForCallResult(CallResult cr)
            {
                switch (cr.Method)
                {
                    case nameof(Mirror.Cleanup):
                        return Brushes.AliceBlue;
                    case nameof(Mirror.CloseFile):
                        return Brushes.AntiqueWhite;
                    case nameof(Mirror.CreateFile):
                        return Brushes.Aqua;
                    case nameof(Mirror.DeleteDirectory):
                        return Brushes.Aquamarine;
                    case nameof(Mirror.DeleteFile):
                        return Brushes.Azure;
                    case nameof(Mirror.FindFiles):
                        return Brushes.Beige;
                    case nameof(Mirror.FindFilesWithPattern):
                        return Brushes.Bisque;
                    case nameof(Mirror.FindStreams):
                        return Brushes.Black;
                    case nameof(Mirror.FlushFileBuffers):
                        return Brushes.BlanchedAlmond;
                    case nameof(Mirror.GetDiskFreeSpace):
                        return Brushes.Blue;
                    case nameof(Mirror.GetFileInformation):
                        return Brushes.BlueViolet;
                    case nameof(Mirror.GetFileSecurity):
                        return Brushes.Brown;
                    case nameof(Mirror.GetVolumeInformation):
                        return Brushes.BurlyWood;
                    case nameof(Mirror.LockFile):
                        return Brushes.CadetBlue;
                    case nameof(Mirror.MoveFile):
                        return Brushes.Chartreuse;
                    case nameof(Mirror.ReadFile):
                        return Brushes.Chocolate;
                    case nameof(Mirror.SetFileAttributes):
                        return Brushes.Coral;
                    case nameof(Mirror.SetFileSecurity):
                        return Brushes.CornflowerBlue;
                    case nameof(Mirror.SetFileTime):
                        return Brushes.Cornsilk;
                    case nameof(Mirror.UnlockFile):
                        return Brushes.Crimson;
                    case nameof(Mirror.WriteFile):
                        return Brushes.Cyan;
                    default:
                        return Brushes.Transparent;
                }
            }
        }
    }
}
