using NonlinearFilters.CLI.Extensions;
using System.Diagnostics;
using System.Drawing;

namespace NonlinearFilters.CLI.Batch
{
	public class FilterBatch : BaseBatch
	{
		protected object GetFilter(ref Bitmap input, string[] args, Type filterType)
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
			var bmp = new Bitmap(Image.FromFile(input));
			var filterInstance = GetFilter(ref bmp, args, filterType);

			output.PathEnsureCreated();

			var applyFilter = filterType.GetMethod("ApplyFilter");
			if (applyFilter is null)
				throw new Exception("method ApplyFilter not found");

			Console.Write($"Applying filter [{filterType.Name}]...");
			var watch = new Stopwatch();

			watch.Start();
			var outBmp = applyFilter!.Invoke(filterInstance, new object[] { Environment.ProcessorCount - 1 }) as Bitmap;
			watch.Stop();

			Console.WriteLine("DONE");
			outBmp!.Save(output);
			Console.WriteLine($"File saved -> {output}");
			Console.WriteLine($"Time elapsed: {watch.Elapsed}");
		}
	}
}
