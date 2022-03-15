using NonlinearFilters.APP.Models;
using System.Windows.Controls;
using System.Windows;

namespace NonlinearFilters.APP.TemplateSelectors
{
	public class FilterParameterTemplateSelector : DataTemplateSelector
	{
		public override DataTemplate? SelectTemplate(object? item, DependencyObject? container)
		{
			if (container is FrameworkElement element && item is FilterParameter parameter)
			{
				if (parameter.Property.PropertyType.IsEnum)
				{
					return element.FindResource("DTEnumParam") as DataTemplate;
				}
				else
				{
					return parameter.Value switch
					{
						int => element.FindResource("DTIntParam") as DataTemplate,
						double => element.FindResource("DTDoubleParam") as DataTemplate,
						bool => element.FindResource("DTBoolParam") as DataTemplate,
						_ => null
					};
				}
			}

			return null;
		}
	}
}
