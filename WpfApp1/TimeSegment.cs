using System;
using System.Windows.Media;

namespace WpfApp1
{
    public class TimedElement : Notifiable
    {
        private DateTime? _Start;
        public DateTime? Start
        {
            get => _Start;
            set
            {
                if (_Start == value) return;
                _Start = value;
                NotifyPropertyChanged();
            }
        }

        private DateTime? _End;
        public virtual DateTime? End
        {
            get => _End;
            set
            {
                if (_End == value) return;
                _End = value;
                NotifyPropertyChanged();
            }
        }
    }

    public sealed class TimeSegment : TimedElement
    {
        private String _Title;
        public String Title
        {
            get => _Title;
            set
            {
                if (String.Equals(_Title, value, StringComparison.OrdinalIgnoreCase)) return;
                _Title = value;
                NotifyPropertyChanged();
            }
        }

        public new DateTime Start
        {
            get => base.Start.Value;
            private set => base.Start = value;
        }

        public new DateTime End
        {
            get => base.End.Value;
            private set => base.End = value;
        }

        public TimeSpan Duration => End - Start;

        private Brush _BackGround;
        public Brush BackGround
        {
            get => _BackGround;
            set
            {
                if (_BackGround == value) return;
                _BackGround = value;
                NotifyPropertyChanged();
            }
        }

        public TimeSegment(DateTime start, DateTime end)
        {
            Start = start;
            End = end;
        }

        public Boolean Intersect(TimeSegment other)
            => other.Start == Start
                || other.Start == End
                || other.End == Start
                || other.End == End
                || other.Start > Start && other.Start < End
                || other.End > Start && other.End < End
                || other.Start < Start && other.End > End;

        public override string ToString()
            => $"{Start}-{End}";

    }
}