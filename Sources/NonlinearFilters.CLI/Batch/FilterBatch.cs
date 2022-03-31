using NonlinearFilters.CLI.Extensions;
using NonlinearFilters.Filters.Interfaces;
using NonlinearFilters.Volume;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using System.Diagnostics;

namespace NonlinearFilters.CLI.Batch
{
	public class FilterBatch : BaseBatch
	{
		protected object GetFilter(object input, string[] args, Type filterType)
		{
			var filterCtor = filterType.GetConstructors().First();
			var paramCtor = GetParameterCtor(filterCtor);
			var @params = paramCtor.GetParameters();

			if (args.Length != @params.Length)
				throw new ArgumentException($"Wrong number of parameters (expected {@params.Length}, got {args.Length}) {filterType.Name}");

			//parsing arguments
			var paramArgs = new object[@params.Length];
			for (int i = 0; i < args.Length; i++)
			{
				var arg = Parse(args[i], @params[i].ParameterType);
				if (arg is null)
					throw new ArgumentException("Wrong constructor parameters");
				paramArgs[i] = arg;
			}

			var paramInstance = paramCtor.Invoke(paramArgs);
			var filterArgs = new object[] { input, paramInstance };

			var filterInstance = filterCtor.Invoke(filterArgs);
			return filterInstance;
		}

		public override void ApplyBatch(string inputFile, string outputFile, string[] args, Type filterType, int processCount)
		{
			object input;
			if (typeof(IFilter2Output).IsAssignableFrom(filterType))
				input = Image.Load<Rgba32>(inputFile);
			else if (typeof(IFilter3Output).IsAssignableFrom(filterType))
				input = VolumetricData.FromFile(inputFile);
			else
				throw new ArgumentException($"Wrong filter type '{filterType}' ...");

			var filterInstance = GetFilter(input, args, filterType);

			outputFile.PathEnsureCreated();

			Console.Write($"Applying filter {filterType.Name} [{processCount} threads]...");
			var watch = new Stopwatch();

			if (filterInstance is IFilter2Output filter2)
			{
				watch.Start();
				var imgOut = filter2.ApplyFilter(processCount);
				watch.Stop();

				Console.WriteLine("DONE");
				imgOut.Save(outputFile);
				imgOut.Dispose();
			}
			else if (filterInstance is IFilter3Output filter3)
			{
				watch.Start();
				var volOut = filter3.ApplyFilter(processCount);
				watch.Stop();

				Console.WriteLine("DONE");
				VolumetricData.SaveFile(volOut, outputFile);
			}

			Console.WriteLine($"File saved -> {outputFile}");
			Console.WriteLine($"Time elapsed: {watch.Elapsed}");

			if (input is Image<Rgba32> img)
				img.Dispose();
		}
	}
}
