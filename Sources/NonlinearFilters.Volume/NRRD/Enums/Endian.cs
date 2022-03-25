using NonlinearFilters.Volume.NRRD.Attributes;

namespace NonlinearFilters.Volume.NRRD.Enums;

[Identifiers("endian")]
public enum Endian
{
	[Supported]
	[Identifiers("little")]
	Little,

	[Supported(false)]
	[Identifiers("big")]
	Big
}
