using NonlinearFilters.Extensions;
using NonlinearFilters.Volume.NRRD.Attributes;
using NonlinearFilters.Volume.NRRD.Enums;
using OpenTK.Mathematics;
using System.IO.Compression;

namespace NonlinearFilters.Volume.NRRD
{
	public class NRRDFileHandler : BaseFileHandler
	{
		//http://teem.sourceforge.net/nrrd/format.html

		private const string PREFIX = "NRRD000";
		private const string NRRD0001 = $"{PREFIX}1";
		private const string NRRD0002 = $"{PREFIX}2";
		private const string NRRD0003 = $"{PREFIX}3";
		private const string NRRD0004 = $"{PREFIX}4";
		private const string NRRD0005 = $"{PREFIX}5";

		private static readonly string[] Header = {
				NRRD0001, NRRD0002, NRRD0003, NRRD0004, NRRD0005
		};

		public override string Extension => ".nrrd";

		public override VolumetricData Load(string path)
		{
			using var stream = File.OpenRead(path);
			using var br = new BinaryReader(stream);

			var firstLine = br.ReadLine();
			if (!Header.Contains(firstLine))
				throw new InvalidDataException($"Unsupported header - expected one of [{string.Join(", ", Header)}], '{firstLine}' was given.");

			//header fields
			var fields = new Dictionary<string, string>();
			while (true)
			{
				var line = br.ReadLine();
				if (line is null || line == string.Empty)
					break;

				if (line[0] == '#')
					continue; //skip comments

				int index = line.IndexOf(':');
				if (index > 0)
				{
					var span = line.AsSpan();
					fields.Add(span[..index].ToString().ToLower(), span[(index + 1)..].ToString().Trim());
				}
			}

			var endian = ParseField<Endian>(fields);
			var type = ParseField<DataType>(fields);
			var dimension = ParseField<Dimension>(fields);
			var encoding = ParseField<Encoding>(fields)!.Value;

			var sizes = fields["sizes"].Split(' ');
			var size = new Vector3i(int.Parse(sizes[2]), int.Parse(sizes[1]), int.Parse(sizes[0]));

			var len = size.X * size.Y * size.Z;
			byte[] data;

			if (fields.ContainsKey("data file"))
			{
				using var dataStream = File.OpenRead(fields["data file"]);
				using var dbr = new BinaryReader(dataStream);
				data = ReadData(dbr, len, encoding);
			}
			else
			{
				data = ReadData(br, len, encoding);
			}

			return new VolumetricData(new(size, Vector3d.One, 0), data);
		}

		private static T? ParseField<T>(Dictionary<string, string> fields) where T : struct
		{
			var type = typeof(T);
			var attrs = type.GetCustomAttributes(false);

			if (attrs.FirstOrDefault(attr => attr is IdentifiersAttribute) is not IdentifiersAttribute id)
				throw new Exception("IdentifiersAttribute wasn't detected.");

			var key = id.Identifiers.First();
			if (!fields.ContainsKey(key))
			{
				if (attrs.FirstOrDefault(attr => attr is RequiredAttribute) is RequiredAttribute)
					throw new InvalidDataException($"'{key}' is required field.");
				return Activator.CreateInstance(type) as T?;
			}

			var val = fields[key];
			var enumValues = type.GetFields();
			foreach (var enumValue in enumValues)
			{
				attrs = enumValue.GetCustomAttributes(false);
				var typeId = attrs.FirstOrDefault(attr => attr is IdentifiersAttribute) as IdentifiersAttribute;
				if (typeId?.Identifiers.Contains(val) == true)
				{
					var supportAttrs = enumValue.GetCustomAttributes(true).FirstOrDefault(attr => attr is SupportedAttribute) as SupportedAttribute;
					if (supportAttrs?.IsSupported != true)
						throw new InvalidDataException($"'{val}' is not supported by this application.");
					return Enum.ToObject(type, enumValue.GetRawConstantValue()!) as T?;
				}
			}

			throw new InvalidDataException($"Unsupported {key} value '{val}'.");
		}

		private static byte[] ReadData(BinaryReader br, int len, Encoding encoding)
		{
			byte[] data = null!;
			if (encoding == Encoding.Raw)
				data = br.ReadBytes(len);
			else if (encoding == Encoding.Gzip)
				data = ReadGZIP(br.BaseStream);

			if (data!.Length != len)
				throw new InvalidDataException($"File is missing data [{data.Length}/{len}]");
			return data;
		}

		private static byte[] ReadGZIP(Stream stream)
		{
			using var gzip = new GZipStream(stream, CompressionMode.Decompress, true);
			using var memory = new MemoryStream();
			gzip.CopyTo(memory);
			return memory.ToArray();
		}

		private static string EnumToString<T>(T @enum) where T : struct
		{
			var type = typeof(T);
			var field = type.GetField(Enum.GetName(type, @enum)!)!;
			var attrs = field.GetCustomAttributes(false);
			return attrs.FirstOrDefault(attr => attr is IdentifiersAttribute) switch
			{
				IdentifiersAttribute id => id.Identifiers.First(),
				_ => @enum.ToString() ?? type.Name
			};
		}

		private static string Set<T>(T val) where T : struct
		{
			var type = typeof(T);
			var attrs = type.GetCustomAttributes(false);
			var fieldName = attrs.FirstOrDefault(attr => attr is IdentifiersAttribute) switch
			{
				IdentifiersAttribute id => id.Identifiers.First(),
				_ => type.Name
			};
			return $"{fieldName}: {EnumToString(val)}";
		}

		private static string Set(ComplexField fieldVal, string val)
		{
			var type = typeof(ComplexField);
			var field = type.GetField(Enum.GetName(type, fieldVal)!)!;
			var attrs = field.GetCustomAttributes(false);
			var fieldName = attrs.FirstOrDefault(attr => attr is IdentifiersAttribute) switch
			{
				IdentifiersAttribute id => id.Identifiers.First(),
				_ => fieldVal.ToString()
			};
			return $"{fieldName}: {val}";
		}

		public override void Save(VolumetricData data, string path)
		{
			using var stream = File.OpenWrite(path);
			var sw = new StreamWriter(stream);

			sw.WriteLine(NRRD0001);
			sw.WriteLine(Set(DataType.UInt8));
			sw.WriteLine(Set(Dimension.D3));
			sw.WriteLine(Set(ComplexField.Size, $"{data.Size.Z} {data.Size.Y} {data.Size.X}"));
			sw.WriteLine(Set(Endian.Little));
			sw.WriteLine(Set(Encoding.Raw));
			sw.WriteLine();
			sw.Flush();

			using var bw = new BinaryWriter(stream);
			bw.Write(data.Data);
		}
	}
}
