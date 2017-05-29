using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp1
{
    public class GanttRowPanel : Panel
    {
        public static readonly DependencyProperty StartDateProperty = DependencyProperty.RegisterAttached(
            "StartDate",
            typeof(DateTime),
            typeof(GanttRowPanel),
            new FrameworkPropertyMetadata(DateTime.MinValue, FrameworkPropertyMetadataOptions.AffectsParentArrange)
        );

        public static readonly DependencyProperty EndDateProperty = DependencyProperty.RegisterAttached(
            "EndDate",
            typeof(DateTime),
            typeof(GanttRowPanel),
            new FrameworkPropertyMetadata(DateTime.MaxValue, FrameworkPropertyMetadataOptions.AffectsParentArrange)
        );

        public static readonly DependencyProperty MaxDateProperty = DependencyProperty.Register(
            nameof(MaxDate),
            typeof(DateTime),
            typeof(GanttRowPanel),
            new FrameworkPropertyMetadata(DateTime.MaxValue, FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public static readonly DependencyProperty MinDateProperty = DependencyProperty.Register(
            nameof(MinDate),
            typeof(DateTime),
            typeof(GanttRowPanel),
            new FrameworkPropertyMetadata(DateTime.MaxValue, FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public DateTime MaxDate
        {
            get => (DateTime)GetValue(MaxDateProperty);
            set => SetValue(MaxDateProperty, value);
        }
        public DateTime MinDate
        {
            get => (DateTime)GetValue(MinDateProperty);
            set => SetValue(MinDateProperty, value);
        }

        public static DateTime GetStartDate(DependencyObject obj)
            => (DateTime)obj.GetValue(StartDateProperty);

        public static void SetStartDate(DependencyObject obj, DateTime value)
            => obj.SetValue(StartDateProperty, value);

        public static DateTime GetEndDate(DependencyObject obj)
            => (DateTime)obj.GetValue(EndDateProperty);

        public static void SetEndDate(DependencyObject obj, DateTime value)
            => obj.SetValue(EndDateProperty, value);


        protected override Size MeasureOverride(Size availableSize)
        {
            foreach (var child in Children.OfType<UIElement>())
            {
                child.Measure(availableSize);
            }

            return new Size(0, 0);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var childs = Children.OfType<UIElement>().ToArray();

            if (MaxDate == MinDate) return finalSize;
            if (childs.Any())
            {
                double range = (MaxDate - MinDate).Ticks;
                double pixelsPerTick = finalSize.Width / range;
                if (pixelsPerTick < 0.00001) pixelsPerTick = 0.00001;
                if (pixelsPerTick > 10) pixelsPerTick = 10;

                pixelsPerTick = 0.0005;

                var childRects = childs.Select(c => ArrangeChild(c, MinDate, pixelsPerTick, finalSize.Height)).ToArray();
                var maxWidth = childRects.Select(x => x.Right).Max();
                if (finalSize.Width < maxWidth)
                {
                    Width = maxWidth;
                    return new Size(maxWidth, finalSize.Height);
                }

            }

            return finalSize;
        }

        private Rect ArrangeChild(UIElement child, DateTime minDate, double pixelsPerTick, double elementHeight)
        {
            DateTime childStartDate = GetStartDate(child);
            DateTime childEndDate = GetEndDate(child);
            TimeSpan childDuration = childEndDate - childStartDate;


            double offset = (childStartDate - minDate).Ticks * pixelsPerTick;
            double width = childDuration == TimeSpan.Zero ? 1 : childDuration.Ticks * pixelsPerTick;
            if (double.IsInfinity(width) || double.IsInfinity(offset)) { System.Diagnostics.Debugger.Break(); }
            var childSize = new Rect(offset, 0, width, elementHeight);
            child.Arrange(childSize);
            return childSize;
        }
    }
}