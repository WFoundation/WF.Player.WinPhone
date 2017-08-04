using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Geowigo.Converters
{
	public class ItemSourceToStringConverter : IValueConverter
	{
		public Dictionary<object, string> Strings { get; set; }
		
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (Strings != null && Strings.ContainsKey(value))
			{
				return Strings[value];
			}

			return value == null ? "" : value.ToString();
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
