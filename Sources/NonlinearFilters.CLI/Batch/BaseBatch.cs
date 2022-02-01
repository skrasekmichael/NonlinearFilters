using System.ComponentModel;
using System.Reflection;

namespace NonlinearFilters.CLI.Batch
{
	public abstract class BaseBatch
	{
		protected object? Parse(string value, Type type)
		{
			value = value.Trim();
			if (string.IsNullOrEmpty(value))
				return null;

			if (type.IsEnum)
			{
				if (!int.TryParse(value, out var parsedEnum))
					return null;
				return Enum.ToObject(type, parsedEnum);
			}

			var converter = TypeDescriptor.GetConverter(type);
			if (converter is not null && converter.IsValid(value))
				return converter.ConvertFromString(value);
			return null;
		}

		protected ConstructorInfo GetParameterCtor(ConstructorInfo ctor) => ctor.GetParameters()[1].ParameterType.GetConstructors().First();

		public abstract void ApplyBatch(string input, string output, string[] args, Type filterType);
	}
}
