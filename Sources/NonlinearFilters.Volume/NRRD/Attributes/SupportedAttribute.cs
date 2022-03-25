namespace NonlinearFilters.Volume.NRRD.Attributes;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Enum)]
public class SupportedAttribute : Attribute
{
	public bool IsSupported { get; }

	public SupportedAttribute(bool isSupported = true)
	{
		IsSupported = isSupported;
	}
}
