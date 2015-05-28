using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace Geowigo.Utils
{
    public static class ConverterExtensions
    {
        /// <summary>
        /// Converts a value and handles typical bugs of Toolkit and official IValueConverters.
        /// </summary>
        /// <param name="vc"></param>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public static object ConvertSafe(this IValueConverter vc, object value, Type targetType, object parameter, CultureInfo culture) 
        {
            // WP7 Toolkit's time converters have a bug where dates of different time zones are not properly
            // converted, and also crash the app. This fixes it.
            // See https://github.com/WFoundation/WF.Player.WinPhone/issues/23
            
            if (value is DateTime)
            {
                DateTime dtValue = (DateTime)value;

                // Lets the converter try converting the raw value.
                try
                {
                    vc.Convert(dtValue, targetType, parameter, culture);
                }
                catch (NotSupportedException)
                {
                    // Fix the value date time by converting it to local time.
                    DateTime fixedValue = dtValue.ToLocalTime();

                    // The conversion should be fine now.
                    try
                    {
                        return vc.Convert(fixedValue, targetType, parameter, culture);
                    }
                    catch (NotSupportedException)
                    {
                        // Despite all precautions, if the conversion still doesn't work, this probably means that 
                        // the date is still considered to be "in the future". Let's assume it's not too far from the present...
                        try
                        {
                            return vc.Convert(DateTime.Now, targetType, parameter, culture);
                        }
                        catch (Exception)
                        {
                            // Great despair, and nothing much to be done...
                            return null;
                        }
                    }    
                }   
            }

            // Let the wrapper converter handle other values.
            return vc.Convert(value, targetType, parameter, culture);
        }
    }
}
