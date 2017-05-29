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
}
