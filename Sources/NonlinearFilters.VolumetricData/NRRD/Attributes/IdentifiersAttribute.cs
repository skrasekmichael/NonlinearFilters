namespace NonlinearFilters.VolumetricData.NRRD.Attributes;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Enum)]
public class IdentifiersAttribute : Attribute
{
	public string[] Identifiers { get; set; }

	public IdentifiersAttribute(params string[] identifiers)
	{
		Identifiers = identifiers;
	}
}
