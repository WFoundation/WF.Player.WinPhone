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
	/// <summary>
	/// A converter from ListBox item count to Visibility.
	/// </summary>
	/// <remarks>
	/// Visible if the count of items is greater than 0, Collapsed otherwise.
	/// </remarks>
	public class ListBoxToVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			ListBox lb = value as ListBox;

			if (lb == null)
			{
				return null;
			}

			return lb.Items.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
