using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for Gantt.xaml
    /// </summary>
    public partial class Gantt : UserControl
    {
        public static readonly DependencyProperty RowsProperty = DependencyProperty.Register(
          nameof(Rows),
          typeof(ReadOnlyObservableCollection<BaseGanttRow>),
          typeof(Gantt),
          new FrameworkPropertyMetadata((x, y) =>
          {
              var ganttControl = x as Gantt;
              var newValue = y.NewValue as ReadOnlyObservableCollection<BaseGanttRow>;
              ganttControl.ViewModel.Rows = newValue;
          })
      );

        public ReadOnlyObservableCollection<BaseGanttRow> Rows
        {
            get => (ReadOnlyObservableCollection<BaseGanttRow>)GetValue(RowsProperty);
            set => SetValue(RowsProperty, value);
        }

        public GanttViewModel ViewModel { get; private set; }
        public Gantt()
        {
            ViewModel = new GanttViewModel();
            ViewModel.FilterWithSelection();
            InitializeComponent();
        }

        private void SelectionPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (!ViewModel.MinDate.HasValue || !ViewModel.MaxDate.HasValue) return;
            var range = ViewModel.MaxDate.Value - ViewModel.MinDate.Value;
            var panel = sender as FrameworkElement;
            var pixelPerTicks = range.Ticks / panel.ActualWidth;
            var mousex = e.GetPosition(panel).X;
            var ticks = mousex * pixelPerTicks;
            var timespan = TimeSpan.FromTicks((long)ticks);

            ViewModel.MouseDate = ViewModel.MinDate + timespan;
            SetNewSelection();
        }

        private void SetNewSelection()
        {
            if (ViewModel.SelectionStartDate.HasValue)
            {
                if (ViewModel.MouseDate > ViewModel.SelectionStartDate)
                {
                    ViewModel.SelectionMinDate = ViewModel.SelectionStartDate;
                    ViewModel.SelectionMaxDate = ViewModel.MouseDate;
                }
                else
                {
                    ViewModel.SelectionMinDate = ViewModel.MouseDate;
                    ViewModel.SelectionMaxDate = ViewModel.SelectionStartDate;
                }
            }
        }

        private void SelectionPanel_MouseEnter(object sender, MouseEventArgs e)
        {
            ViewModel.SelectionPreviewVisibility = Visibility.Visible;
        }

        private void SelectionPanel_MouseLeave(object sender, MouseEventArgs e)
        {
            ViewModel.SelectionPreviewVisibility = Visibility.Collapsed;
        }

        private void SmallGantt_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ViewModel.SelectionMinDate = null;
            ViewModel.SelectionMaxDate = null;
            if (e.LeftButton == MouseButtonState.Pressed)
                ViewModel.SelectionStartDate = ViewModel.MouseDate;
        }

        private void SmallGantt_MouseUp(object sender, MouseButtonEventArgs e)
        {
            SetNewSelection();
            ViewModel.SelectionStartDate = null;

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.FilterWithSelection();
        }
    }

    public class GanttViewModel : Notifiable
    {
        private ReadOnlyObservableCollection<BaseGanttRow> _Rows;
        public ReadOnlyObservableCollection<BaseGanttRow> Rows
        {
            get => _Rows;
            set
            {
                if (_Rows == value) return;
                var oldValue = _Rows;
                if (oldValue != null) (oldValue as INotifyCollectionChanged).CollectionChanged -= Rows_CollectionChanged;
                _Rows = value;
                (value as INotifyCollectionChanged).CollectionChanged += Rows_CollectionChanged;
                NotifyPropertyChanged();
                RefreshTimeRange();
            }
        }

        private DateTime? _MinDate;
        public DateTime? MinDate
        {
            get => _MinDate;
            private set
            {
                if (_MinDate == value) return;
                _MinDate = value;
                NotifyPropertyChanged();
                if (!_SelectionMinDate.HasValue) NotifyPropertyChanged(nameof(SelectionMinDate));
                CalculateGrid();
            }
        }

        private DateTime? _MaxDate;
        public DateTime? MaxDate
        {
            get => _MaxDate;
            private set
            {
                if (_MaxDate == value) return;
                _MaxDate = value;
                NotifyPropertyChanged();
                if (!_SelectionMaxDate.HasValue) NotifyPropertyChanged(nameof(SelectionMaxDate));
                CalculateGrid();
            }
        }

        public ObservableCollection<GridElement> GridElements { get; private set; } = new ObservableCollection<GridElement>();

        private void CalculateGrid()
        {
            if (!MaxDate.HasValue || !MinDate.HasValue) return;
            GridElements.Clear();
            var range = MaxDate.Value - MinDate.Value;
            var onefifth = TimeSpan.FromTicks(range.Ticks / 5);
            if (onefifth.TotalDays >= 1) onefifth = TimeSpan.FromDays(Math.Round(onefifth.TotalDays));
            else if (onefifth.TotalHours >= 1) onefifth = TimeSpan.FromHours(Math.Round(onefifth.TotalHours));
            else if (onefifth.TotalMinutes >= 1) onefifth = TimeSpan.FromMinutes(Math.Round(onefifth.TotalMinutes));
            else if (onefifth.TotalSeconds >= 1) onefifth = TimeSpan.FromSeconds(Math.Round(onefifth.TotalSeconds));
            else if (onefifth.TotalMilliseconds >= 1) onefifth = TimeSpan.FromMilliseconds(Math.Round(onefifth.TotalMilliseconds));

            for (var i = 1L; i <= 5L; i++)
            {
                var end = MinDate.Value + TimeSpan.FromTicks(i * onefifth.Ticks);
                if (end >= MaxDate) return;
                GridElements.Add(new GridLine(end));
                GridElements.Add(new GridLabel(end, end - MinDate.Value));
            }
        }

        public DateTime? SelectionStartDate { get; internal set; }

        private DateTime? _SelectionMinDate;
        public DateTime? SelectionMinDate
        {
            get => _SelectionMinDate.HasValue ? _SelectionMinDate : _MinDate;
            set
            {
                if (_SelectionMinDate == value) return;
                _SelectionMinDate = value;
                NotifyPropertyChanged();
                FilterWithSelection();
            }
        }

        private DateTime? _SelectionMaxDate;
        public DateTime? SelectionMaxDate
        {
            get => _SelectionMaxDate.HasValue ? _SelectionMaxDate : _MaxDate;
            set
            {
                if (_SelectionMaxDate == value) return;
                _SelectionMaxDate = value;
                NotifyPropertyChanged();
                FilterWithSelection();
            }
        }

        private ReadOnlyObservableCollection<BaseGanttRow> _FilteredRows;
        public ReadOnlyObservableCollection<BaseGanttRow> FilteredRows
        {
            get => _FilteredRows; set
            {
                if (_FilteredRows == value) return;
                _FilteredRows = value;
                NotifyPropertyChanged();
            }
        }

        public void FilterWithSelection()
        {
            if (!SelectionMinDate.HasValue || !SelectionMaxDate.HasValue)
            {
                FilteredRows = _Rows;
            }
            else
            {
                var newFilteredRows = new ObservableCollection<BaseGanttRow>();
                FilteredRows = new ReadOnlyObservableCollection<BaseGanttRow>(newFilteredRows);

                var selectionRange = new TimeSegment(SelectionMinDate.Value, SelectionMaxDate.Value);

                foreach (var exRow in _Rows.OfType<ExpandableGanttRow>().ToArray())
                {
                    var newExRow = new ExpandableGanttRow() { Title = exRow.Title, Context = exRow.Context, IsExpanded = exRow.IsExpanded };
                    
                    foreach (var row in exRow.SubRows)
                    {
                        var intersectingSegments = row.Segments.Where(s => s.Intersect(selectionRange)).ToArray();
                        if (intersectingSegments.Any())
                        {
                            var newRow = new GanttRow() { Title = row.Title, Context = row.Context };
                            newExRow.SubRows.Add(newRow);
                            foreach (var intersectingSeg in intersectingSegments)
                                newRow.Segments.Add(intersectingSeg);
                        }
                    }
                    if(newExRow.SubRows.Any())
                       newFilteredRows.Add(newExRow);
                }
            }
        }

        private Visibility _SelectionPreviewVisibility;
        public Visibility SelectionPreviewVisibility
        {
            get => _SelectionPreviewVisibility;
            set
            {
                if (_SelectionPreviewVisibility == value) return;
                _SelectionPreviewVisibility = value;
                NotifyPropertyChanged();
            }
        }

        private DateTime? _MouseDate;
        public DateTime? MouseDate
        {
            get => _MouseDate;
            set
            {
                if (_MouseDate == value) return;
                _MouseDate = value;
                NotifyPropertyChanged();
            }
        }

        private void Rows_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (var ganttRow in e.NewItems.OfType<BaseGanttRow>())
                {
                    ReadMinMaxDates(ganttRow);
                    ganttRow.PropertyChanged += GanttRow_PropertyChanged;
                }
            }
            if (e.OldItems != null)
            {
                foreach (var ganttRow in e.OldItems.OfType<BaseGanttRow>())
                {
                    ganttRow.PropertyChanged -= GanttRow_PropertyChanged;
                    if (ganttRow.MinDate == MinDate || ganttRow.MaxDate == MaxDate)
                    {
                        RefreshTimeRange();
                    }
                }
            }
        }

        private void RefreshTimeRange()
        {
            MinDate = Rows.Select(x => x.MinDate).Min();
            MaxDate = Rows.Select(x => x.MaxDate).Max();
        }

        private void ReadMinMaxDates(BaseGanttRow ganttRow)
        {
            if (ganttRow.MinDate.HasValue && !MinDate.HasValue || ganttRow.MinDate < MinDate) MinDate = ganttRow.MinDate;
            if (ganttRow.MaxDate.HasValue && !MaxDate.HasValue || ganttRow.MaxDate > MaxDate) MaxDate = ganttRow.MaxDate;
        }

        private void GanttRow_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(BaseGanttRow.MinDate) || e.PropertyName == nameof(BaseGanttRow.MaxDate))
                ReadMinMaxDates(sender as BaseGanttRow);
        }
    }

    public class GridElement : TimedElement
    {
        public new DateTime End
        {
            get => base.End.Value;
            set => base.End = value;
        }

        public GridElement(DateTime end)
        {
            End = end;
        }
    }

    public sealed class GridLine : GridElement
    {
        public GridLine(DateTime end) : base(end) { }
    }

    public sealed class GridLabel : GridElement
    {
        public TimeSpan Elapsed { get; private set; }
        public GridLabel(DateTime end, TimeSpan elapsed) : base(end)
        {
            Elapsed = elapsed;
        }
    }
}
