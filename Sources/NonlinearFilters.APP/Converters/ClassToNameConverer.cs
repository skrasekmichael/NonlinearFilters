using System.Globalization;
using System.Windows.Data;

namespace NonlinearFilters.APP.Converters
{
	public class ClassToNameConverer : IValueConverter
	{
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			return value is null ? "ClassName" : value.GetType().Name;
		}

		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
