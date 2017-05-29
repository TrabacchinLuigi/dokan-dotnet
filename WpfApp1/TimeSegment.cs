using System;
using System.Windows.Media;

namespace WpfApp1
{
    public sealed class TimeSegment : Notifiable
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

        public DateTime Start { get; private set; }
        public DateTime End { get; private set; }

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
                || other.Start > Start
                || other.End > Start;

        public override string ToString()
            => $"{Start}-{End}";

    }
}