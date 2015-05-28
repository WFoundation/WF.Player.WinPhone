using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using Geowigo.Utils;

namespace Geowigo.Converters
{
    public class SafeDateTimeConverter : IValueConverter
    {
        public IValueConverter WrappedConverter { get; set; }
        
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // WP7 Toolkit's time converters have a bug where dates of different time zones are not properly
            // converted, and also crash the app. This fixes it.
            // See https://github.com/WFoundation/WF.Player.WinPhone/issues/23

            if (WrappedConverter == null)
            {
                return null;
            }

            return WrappedConverter.ConvertSafe(value, targetType, parameter, culture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
