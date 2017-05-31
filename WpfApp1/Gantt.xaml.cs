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
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (!ViewModel.MinDate.HasValue || !ViewModel.MaxDate.HasValue) return;

            var elapsedTime = ViewModel.MaxDate.Value - ViewModel.MinDate.Value;
            var onefourth = new TimeSpan(elapsedTime.Ticks / 4);
            ViewModel.SelectionMinDate = ViewModel.MinDate + onefourth;
            ViewModel.SelectionMaxDate = ViewModel.MaxDate - onefourth;
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
                if (_IsSelectedEverything) NotifyPropertyChanged(nameof(SelectionMinDate));
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
                if (_IsSelectedEverything) NotifyPropertyChanged(nameof(SelectionMaxDate));
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
                GridElements.Add(new GridLine(end));
                GridElements.Add(new GridLabel(end));
            }
        }

        private Boolean _IsSelectedEverything = true;
        public Boolean IsSelectedEverything
        {
            get => _IsSelectedEverything;
            set
            {
                if (_IsSelectedEverything == value) return;
                _IsSelectedEverything = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(SelectionMaxDate));
                NotifyPropertyChanged(nameof(SelectionMinDate));
            }
        }

        private DateTime? _SelectionMaxDate;
        public DateTime? SelectionMaxDate
        {
            get => _IsSelectedEverything ? _MaxDate : _SelectionMaxDate;
            set
            {
                if (_SelectionMaxDate == value) return;
                _SelectionMaxDate = value;
                _IsSelectedEverything = false;
                NotifyPropertyChanged();
            }
        }

        private DateTime? _SelectionMinDate;
        public DateTime? SelectionMinDate
        {
            get => _IsSelectedEverything ? _MinDate : _SelectionMinDate;
            set
            {
                if (_SelectionMinDate == value) return;
                _SelectionMinDate = value;
                _IsSelectedEverything = false;
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
        public GridLabel(DateTime end) : base(end) { }
    }
}
