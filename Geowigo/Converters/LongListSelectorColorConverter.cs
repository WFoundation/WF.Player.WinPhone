using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Media;

namespace Geowigo.Converters
{
    public class LongListSelectorColorConverter : IValueConverter
    {
        public Brush EmptyBrush { get; set; }

        public Brush NotEmptyBrush { get; set; }
        
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            IEnumerable item = value as IEnumerable;

            if (value == null)
            {
                return null;
            }

            int cnt = 0;
            foreach (object i in item)
            {
                cnt++;
            }

            return cnt > 0 ? NotEmptyBrush : EmptyBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
