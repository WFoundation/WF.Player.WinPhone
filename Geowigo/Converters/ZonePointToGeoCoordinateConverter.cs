using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using WF.Player.Core;
using Geowigo.Utils;

namespace Geowigo.Converters
{
    public class ZonePointToGeoCoordinateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            ZonePoint zp = value as ZonePoint;
            if (zp == null)
            {
                if (!(parameter is string) || (string)parameter != "NoNull")
                {
                    return null;
                }
                else
                {
                    zp = new ZonePoint(0, 0, 0);
                }
            } 

            return zp.ToGeoCoordinate();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
