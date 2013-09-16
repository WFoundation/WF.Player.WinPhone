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

namespace Geowigo.Converters
{
	public class ObjectToVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			// If isStrict is true, the convertor only checks for null/non-null objects.
			// If it is false, it checks for object values as well.
			bool isStrict = String.Equals(parameter, "strict");
			
			Visibility target = Visibility.Visible;

			if (value == null)
			{
				// Null values are always collapsed.
				target = Visibility.Collapsed;
			}
			else
			{
				// Non-strict mode
				if (!isStrict)
				{
					// Booleans give collapsed only if they are false.
					if (value is bool && !(bool)value)
					{
						target = Visibility.Collapsed;
					}
				}
			}

			return target;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
