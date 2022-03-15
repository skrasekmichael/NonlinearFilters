using System.Globalization;
using System.Windows.Data;

namespace NonlinearFilters.APP.Converters
{
	public class IntToStringConverter : IValueConverter
	{
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			if (value is int num)
				return num.ToString();
			return "0";
		}

		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			if (value is not null)
			{
				if (int.TryParse((string)value, out var num))
					return num;
			}
			return 0;
		}
	}
}
