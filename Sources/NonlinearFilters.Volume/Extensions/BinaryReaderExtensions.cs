namespace NonlinearFilters.Extensions
{
	public static class BinaryReaderExtensions
	{
		public static string ReadLine(this BinaryReader sr)
		{
			var bufferList = new List<char>();

			while (true)
			{
				var c = sr.Read();
				if (c == -1 || c == '\n')
					break;
				else if (c == '\r' && sr.PeekChar() == '\n')
				{
					sr.Read();
					break;
				}

				bufferList.Add((char)c);
			}

			return new string(bufferList.ToArray());
		}
	}
}
