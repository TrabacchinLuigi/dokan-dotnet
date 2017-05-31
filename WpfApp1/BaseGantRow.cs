using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace WpfApp1
{
    public abstract class BaseGanttRow : Notifiable
    {
        public String Title { get; set; }
        public ObservableCollection<TimeSegment> Segments { get; private set; }
        public Object Context { get; set; }

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

        public BaseGanttRow()
        {
            Segments = new ObservableCollection<TimeSegment>();
            Segments.CollectionChanged += Segments_CollectionChanged;
        }

        private void Segments_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                    CalculateMinMax();
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                default:
                    break;
            }
        }

        private void CalculateMinMax()
        {
            MinDate = Segments.Select(x => (DateTime?)x.Start).Min();
            MaxDate = Segments.Select(x => (DateTime?)x.End).Max();
        }
    }

    public class GanttRow : BaseGanttRow { }

    public class ExpandableGanttRow : BaseGanttRow
    {
        public new ReadOnlyObservableCollection<TimeSegment> Segments { get; private set; }
        public ObservableCollection<BaseGanttRow> SubRows { get; private set; }
        private Boolean _IsExpanded;
        public Boolean IsExpanded
        {
            get => _IsExpanded;
            set
            {
                if (_IsExpanded == value) return;
                _IsExpanded = value;
                NotifyPropertyChanged();
            }
        }

        public ExpandableGanttRow()
        {
            Segments = new ReadOnlyObservableCollection<TimeSegment>(base.Segments);
            SubRows = new ObservableCollection<BaseGanttRow>();
            SubRows.CollectionChanged += SubRows_CollectionChanged;
        }

        private void SubRows_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                    CalculateSegments();
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                default:
                    break;
            }
        }

        private void CalculateSegments()
        {
            var tempSegments = new List<TimeSegment>();
            foreach (var subRow in SubRows)
            {
                foreach (var segment in subRow.Segments)
                {
                    var existingIntersectingSegments = tempSegments.Where(x => x.Intersect(segment)).ToArray();
                    if (!existingIntersectingSegments.Any())
                    {
                        tempSegments.Add(new TimeSegment(segment.Start, segment.End));
                    }
                    else
                    {
                        foreach (var existingIntersectingSegment in existingIntersectingSegments)
                        {
                            tempSegments.Remove(existingIntersectingSegment);
                            var mergedStart = existingIntersectingSegment.Start < segment.Start
                                ? existingIntersectingSegment.Start
                                : segment.Start;
                            var mergedEnd = existingIntersectingSegment.End > segment.End
                                ? existingIntersectingSegment.End
                                : segment.End;

                            tempSegments.Add(new TimeSegment(mergedStart, mergedEnd));
                        }
                    }
                }
            }
            base.Segments.Clear();
            foreach (var segment in tempSegments)
                base.Segments.Add(segment);
        }

    }
}