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
                try
                {
                    return _wrappedConverter.Convert(fixedValue, targetType, parameter, culture);
                }
                catch (NotSupportedException)
                {
                    // Despite all precautions, if the conversion still doesn't work, this probably means that 
                    // the date is still considered to be "in the future". Let's assume it's not too far from the present...
                    try
                    {
                        return _wrappedConverter.Convert(DateTime.Now, targetType, parameter, culture);
                    }
                    catch (Exception)
                    {
                        // Great despair, and nothing much to be done...
                        return null;
                    }
                }
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
