using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Data;
using WF.Player.Core;

namespace Geowigo.Converters
{
	public class PlayerZoneStateToStringConverter : IValueConverter
	{

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (!(value is PlayerZoneState))
			{
				return null;
			}

			switch ((PlayerZoneState)value)
			{
				case PlayerZoneState.Inside:
					return "Inside";

				case PlayerZoneState.Proximity:
					return "In proximity";

				case PlayerZoneState.Distant:
					return "Distant";

				case PlayerZoneState.NotInRange:
					return "Not in range";

				default:
					return null;
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
