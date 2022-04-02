using McMaster.Extensions.CommandLineUtils;
using NonlinearFilters.CLI.Batch;
using NonlinearFilters.Filters2D;
using NonlinearFilters.Filters3D;
using System.ComponentModel.DataAnnotations;

namespace NonlinearFilters.CLI
{
	public enum InputType { Default = 0, Param = 1, Image = 2 }

	public sealed class Startup
	{
		[Required]
		[FileExists]
		[Option(ShortName = "i", LongName = "input")]
		public string Input { get; } = null!;

		[Required]
		[Option(ShortName = "o", LongName = "output")]
		public string Output { get; } = null!;

		[Required]
		[AllowedValues("bf", "fbf", "nlmf", "nlmpf", "fnlmf", "fbf3", "fnlmf3", IgnoreCase = true)]
		[Option(ShortName = "f", LongName = "filter")]
		public string Filter { get; set; } = null!;

		[Required]
		[Option(ShortName = "p", LongName = "params")]
		public string Params { get; } = string.Empty;

		[Option(ShortName = "t", LongName = "type")]
		public InputType InputType { get; } = InputType.Default;
		
		[Option(ShortName = "tc", LongName = "threads")]
		public int ThreadCount { get; } = Environment.ProcessorCount - 1;

		private static readonly Dictionary<string, Type> filters = new()
		{
			{ "bf", typeof(BilateralFilter) },
			{ "fbf", typeof(FastBilateralFilter) },
			{ "nlmf", typeof(NonLocalMeansPixelFilter) },
			{ "nlmpf", typeof(NonLocalMeansPatchFilter) },
			{ "fnlmf", typeof(FastNonLocalMeansFilter) },
			{ "fbf3", typeof(FastBilateralFilter3) },
			{ "fnlmf3", typeof(FastNonLocalMeansFilter3) }
		};

		private void OnExecute()
		{
			Filter = Filter.ToLower();
			if (!filters.ContainsKey(Filter))
				throw new ArgumentException($"Invalid filter '{Filter}'");

			var filterType = filters[Filter];
			BaseBatch filter = InputType switch
			{
				InputType.Default => new FilterBatch(),
				InputType.Param => new ParamBatch(),
				_ => throw new NotImplementedException()
			};

			filter.ApplyBatch(Input, Output, Params.Split(','), filterType, ThreadCount);
		}
	}
}
