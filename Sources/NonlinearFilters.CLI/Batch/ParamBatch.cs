using NonlinearFilters.CLI.Extensions;
using NonlinearFilters.Filters.Interfaces;
using NonlinearFilters.Volume;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using System.Diagnostics;

namespace NonlinearFilters.CLI.Batch
{
	public class ParamBatch : BaseBatch
	{
		public override void ApplyBatch(string inputFile, string outputFiles, string[] args, Type filterType, int processCount)
		{
			var updateParamsMethod = filterType.GetMethod("UpdateParameters");
			if (updateParamsMethod is null)
				return;

			var filterCtor = filterType.GetConstructors().First();
			var paramCtor = GetParameterCtor(filterCtor);
			var paramParams = paramCtor.GetParameters();
			var paramCount = paramParams.Length;

			if (args.Length % paramCount != 0)
				throw new ArgumentException("Wrong parameter count");

			int iterations = args.Length / paramCount;

			object input;
			if (typeof(IFilter2Output).IsAssignableFrom(filterType))
				input = Image.Load<Rgba32>(inputFile);
			else if (typeof(IFilter3Output).IsAssignableFrom(filterType))
				input = VolumetricData.FromFile(inputFile);
			else
				throw new ArgumentException($"Wrong filter type '{filterType}' ...");

			var @params = new object[iterations];
			for (int i = 0; i < iterations; i++)
			{
				var paramArgs = new object[paramCount];
				for (int j = 0; j < paramCount; j++)
				{
					var arg = Parse(args[i * paramCount + j], paramParams[j].ParameterType);
					if (arg is null)
						throw new ArgumentException("Wrong constructor parameters");
					paramArgs[j] = arg;
				}
				@params[i] = paramCtor.Invoke(paramArgs);
			}

			var outputPaths = outputFiles.Split(',').Select(e => e.Trim()).ToArray();
			foreach (var path in outputPaths)
				path.PathEnsureCreated();

			var filterInstance = filterCtor.Invoke(new object[] { input, @params.First() });

			var watch = new Stopwatch();
			for (int i = 0; i < iterations; i++)
			{
				updateParamsMethod.Invoke(filterInstance, new object[] { @params[i] });
				Console.Write($"{i + 1}. Applying filter [{processCount} threads]...");

				if (filterInstance is IFilter2Output filter2)
				{
					watch.Restart();
					var imgOut = filter2.ApplyFilter(processCount);
					watch.Stop();

					Console.WriteLine("DONE");
					imgOut.Save(outputPaths[i]);
					imgOut.Dispose();
				}
				else if (filterInstance is IFilter3Output filter3)
				{
					watch.Restart();
					var volOut = filter3.ApplyFilter(processCount);
					watch.Stop();

					Console.WriteLine("DONE");
					VolumetricData.SaveFile(volOut, outputPaths[i]);
				}

				Console.WriteLine($"File saved -> {outputPaths[i]}");
				Console.WriteLine($"Time elapsed: {watch.Elapsed}\n");
			}

			if (input is Image<Rgba32> img)
				img.Dispose();
		}
	}
}
