using System.Reflection;

namespace NonlinearFilters.APP.Models
{
	public class FilterParameter
	{
		public string Name { get; set; }
		public object Value { get; set; }
		public PropertyInfo Property { get; }

		public FilterParameter(string name, object value, PropertyInfo property)
		{
			Name = name;
			Value = value;
			Property = property;
		}
	}
}
