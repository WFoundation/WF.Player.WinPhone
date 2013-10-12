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
	public class ObjectTypeToVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			//Type targetValueType = Type.GetType(parameter.ToString(), true);
			string targetValueTypeString = parameter.ToString();
			Type testType = value.GetType();

			// Returns Visible if the target type is equal to the test type.
			//if (testType.Equals(targetValueType) || testType.IsSubclassOf(targetValueType))
			if (String.Equals(testType.FullName, targetValueTypeString))
			{
				return Visibility.Visible;
			}

			// Returns Visible if a certain set of types are matching.
			if (targetValueTypeString == "Thing" && value is WF.Player.Core.Thing)
			{
				return Visibility.Visible;
			}

			return Visibility.Collapsed;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
