using NonlinearFilters.Volume.NRRD.Attributes;

namespace NonlinearFilters.Volume.NRRD.Enums;

[Required]
[Supported(false)]
[Identifiers("encoding")]
public enum Encoding
{
	[Supported]
	[Identifiers("raw")]
	Raw,

	[Identifiers("ascii", "txt", "text")]
	Text,

	[Identifiers("hex")]
	Hex,

	[Supported]
	[Identifiers("gzip", "gz")]
	Gzip,

	[Identifiers("bzip2", "bz2")]
	Bzip2
}
