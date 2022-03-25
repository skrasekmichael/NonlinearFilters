using NonlinearFilters.CLI.Extensions;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using System.Diagnostics;

namespace NonlinearFilters.CLI.Batch
{
	public class FilterBatch : BaseBatch
	{
		protected object GetFilter(ref Image<Rgba32> input, string[] args, Type filterType)
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

		public override void ApplyBatch(string input, string output, string[] args, Type filterType)
		{
			var img = Image.Load<Rgba32>(input);
			var filterInstance = GetFilter(ref img, args, filterType);

			output.PathEnsureCreated();

			var applyFilter = filterType.GetMethod("ApplyFilter");
			if (applyFilter is null)
				throw new Exception("method ApplyFilter not found");

			Console.Write($"Applying filter [{filterType.Name}]...");
			var watch = new Stopwatch();

			watch.Start();
			var imgOut = applyFilter!.Invoke(filterInstance, new object[] { Environment.ProcessorCount - 1 }) as Image<Rgba32>;
			watch.Stop();

			Console.WriteLine("DONE");
			imgOut!.Save(output);
			Console.WriteLine($"File saved -> {output}");
			Console.WriteLine($"Time elapsed: {watch.Elapsed}");
		}
	}
}
