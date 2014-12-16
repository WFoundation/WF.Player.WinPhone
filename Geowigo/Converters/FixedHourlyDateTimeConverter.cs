using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace Geowigo.Converters
{
    public class FixedHourlyDateTimeConverter : IValueConverter
    {
        private Microsoft.Phone.Controls.HourlyDateTimeConverter _wrappedConverter = new Microsoft.Phone.Controls.HourlyDateTimeConverter();

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // WP7 Toolkit's HourlyDateTimeConverter has a bug where dates of different time zones are not properly
            // converted, and also crash the app. This fixes it.
            // See https://github.com/WFoundation/WF.Player.WinPhone/issues/23

            if (value is DateTime)
            {
                // Fix the value date time by converting it to local time.
                DateTime fixedValue = ((DateTime)value).ToLocalTime();

                // The conversion should be fine now.
                return _wrappedConverter.Convert(fixedValue, targetType, parameter, culture);
            }

            // Let the wrapper converter handle other values.
            return _wrappedConverter.Convert(value, targetType, parameter, culture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
