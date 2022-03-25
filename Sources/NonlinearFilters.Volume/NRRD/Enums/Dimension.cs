using NonlinearFilters.Volume.NRRD.Attributes;

namespace NonlinearFilters.Volume.NRRD.Enums;

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
