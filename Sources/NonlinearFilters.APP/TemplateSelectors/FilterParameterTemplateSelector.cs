using NonlinearFilters.APP.Models;
using System.Windows.Controls;
using System.Windows;

namespace NonlinearFilters.APP.TemplateSelectors
{
	public class FilterParameterTemplateSelector : DataTemplateSelector
	{
		public override DataTemplate? SelectTemplate(object item, DependencyObject container)
		{
			var element = container as FrameworkElement;

			if (element is not null && item is FilterParameter parameter)
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
						_ => throw new ArgumentException()
					};
				}
			}

			return null;
		}
	}
}
