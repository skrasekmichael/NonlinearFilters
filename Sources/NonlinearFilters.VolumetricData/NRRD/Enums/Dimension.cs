using NonlinearFilters.VolumetricData.NRRD.Attributes;

namespace NonlinearFilters.VolumetricData.NRRD.Enums;

[Required]
[Supported(false)]
[Identifiers("dimension")]
public enum Dimension
{
	[Identifiers("1")]
	D1,

	[Identifiers("2")]
	D2,

	[Supported]
	[Identifiers("3")]
	D3,

	[Identifiers("4")]
	D4
}
