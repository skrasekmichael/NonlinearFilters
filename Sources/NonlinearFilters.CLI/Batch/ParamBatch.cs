using NonlinearFilters.CLI.Extensions;
using System.Diagnostics;
using System.Drawing;

namespace NonlinearFilters.CLI.Batch
{
	public class ParamBatch : BaseBatch
	{
		public override void ApplyBatch(string input, string output, string[] args, Type filterType)
		{
			var updateParamsMethod = filterType.GetMethod("UpdateParameters");
			var applyFilterMethod = filterType.GetMethod("ApplyFilter");

			if (updateParamsMethod is null || applyFilterMethod is null)
				return;

			var filterCtor = filterType.GetConstructors().First();
			var paramCtor = GetParameterCtor(filterCtor);
			var paramParams = paramCtor.GetParameters();
			var paramCount = paramParams.Length;

			if (args.Length % paramCount != 0)
				throw new ArgumentException("Wrong parameter count");

			int iterations = args.Length / paramCount;
			var bmp = new Bitmap(Image.FromFile(input));

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

			var outputPaths = output.Split(',').Select(e => e.Trim()).ToArray();
			foreach (var path in outputPaths)
				path.PathEnsureCreated();

			var filterInstance = filterCtor.Invoke(new object[] { bmp, @params.First() });
			var watch = new Stopwatch();

			for (int i = 0; i < iterations; i++)
			{
				updateParamsMethod.Invoke(filterInstance, new object[] { @params[i] });

				Console.Write($"{i + 1}. Applying filter...");
				watch.Start();
				var bmpOut = applyFilterMethod.Invoke(filterInstance, new object[] { Environment.ProcessorCount - 1 }) as Bitmap;
				watch.Stop();
				Console.WriteLine("DONE");

				bmpOut!.Save(outputPaths[i]);
				Console.WriteLine($"File saved -> {outputPaths[i]}");
				Console.WriteLine($"Time elapsed: {watch.Elapsed}\n");
				watch.Restart();
			}
		}
	}
}
