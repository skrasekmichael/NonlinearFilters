using NonlinearFilters.Volume.NRRD.Attributes;

namespace NonlinearFilters.Volume.NRRD.Enums;

[Required]
[Supported(false)]
[Identifiers("type")]
public enum DataType
{
	[Identifiers("int8", "signed char", "int8_t")]
	Int8,

	[Supported]
	[Identifiers("uint8", "uchar", "unsigned char", "uint8_t")]
	UInt8,

	[Identifiers("short", "short int", "signed short", "signed short int", "int16", "int16_t")]
	Int16,

	[Identifiers("ushort", "unsigned short", "unsigned short int", "uint16", "uint16_t")]
	UInt16,

	[Identifiers("int", "signed int", "int32", "int32_t")]
	Int32,

	[Identifiers("uint", "unsigned int", "uint32", "uint32_t")]
	UInt32,

	[Identifiers("int64", "longlong", "long long", "long long int", "signed long long", "signed long long int", "int64_t")]
	Int64,

	[Identifiers("uint64", "ulonglong", "unsigned long long", "unsigned long long int", "uint64_t")]
	UInt64,

	[Identifiers("float")]
	Float,

	[Identifiers("double")]
	Double
}
