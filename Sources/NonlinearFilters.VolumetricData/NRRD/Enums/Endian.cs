using NonlinearFilters.VolumetricData.NRRD.Attributes;

namespace NonlinearFilters.VolumetricData.NRRD.Enums;

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
