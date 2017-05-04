using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace WpfApp1
{
    public class MirrorContextColorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var booleans = values.OfType<Boolean>().ToArray();
            if (booleans.Length >= 2)
            {
                if (!booleans[0] && !booleans[1]) return System.Windows.Media.Brushes.White;
                else if (!booleans[0] && booleans[1]) return System.Windows.Media.Brushes.Yellow;
                else if (booleans[0] && !booleans[1]) return System.Windows.Media.Brushes.Red;
                else if (booleans[0] && booleans[1]) return System.Windows.Media.Brushes.OrangeRed;
                else return System.Windows.Media.Brushes.Violet;
            }
            else return System.Windows.Media.Brushes.HotPink;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
