using McMaster.Extensions.CommandLineUtils;
using NonlinearFilters.CLI.Batch;
using NonlinearFilters.Filters2;
using System.ComponentModel.DataAnnotations;

namespace NonlinearFilters.CLI
{
	public enum InputType { Default = 0, Param = 1, Image = 2 }

	public sealed class Startup
	{
		[Required]
		[FileExists]
		[Option(ShortName = "i", LongName = "input")]
		public string Input { get; set; } = null!;

		[Required]
		[Option(ShortName = "o", LongName = "output")]
		public string Output { get; set; } = null!;

		[Required]
		[AllowedValues("nlmf", "bf", "fbf", "fnlmf", IgnoreCase = true)]
		[Option(ShortName = "f", LongName = "filter")]
		public string Filter { get; set; } = null!;

		[Required]
		[Option(ShortName = "p", LongName = "params")]
		public string Params { get; set; } = null!;

		[Option(ShortName = "t", LongName = "type")]
		public InputType InputType { get; } = InputType.Default;

		private void OnExecute()
		{
			Filter = Filter.ToLower();
			var filters = new Dictionary<string, Type>()
			{
				{ "bf", typeof(BilateralFilter) },
				{ "fbf", typeof(FastBilateralFilter) },
				{ "nlmf", typeof(NonLocalMeansFilter) },
				{ "fnlmf", typeof(FastNonLocalMeansFilter) }
			};

			var filterType = filters[Filter];
			BaseBatch filter = InputType switch
			{
				InputType.Default => new FilterBatch(),
				InputType.Param => new ParamBatch(),
				_ => throw new NotImplementedException()
			};

			filter.ApplyBatch(Input, Output, Params.Split(','), filterType);
		}
	}
}
