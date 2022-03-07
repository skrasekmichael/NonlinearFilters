using NonlinearFilters.Filters.Parameters;

namespace NonlinearFilters.Filters
{
	public delegate void ProgressChanged(double percentage, object sender);

	public abstract class BaseFilter<TParameters> where TParameters : BaseFilterParameters
	{
		public event ProgressChanged? OnProgressChanged;

		protected int[]? doneCounts = null;
		protected readonly double sizeCoeff;

		public TParameters Parameters { get; protected set; }
		protected bool Initalized { get; set; } = false;
		protected bool PreComputed { get; set; } = false;

		public BaseFilter(TParameters parameters, double sizeCoeff)
		{
			Parameters = parameters;
			InitalizeParams();
			this.sizeCoeff = sizeCoeff;
		}

		public void Initalize()
		{
			InitalizeFilter();
			Initalized = true;
		}

		protected virtual void InitalizeFilter() { }

		public void UpdateParameters(TParameters parameters)
		{
			Parameters = parameters;
			InitalizeParams();
			PreComputed = false;
		}

		protected abstract void InitalizeParams();

		protected void ChangeProgress(double percentage) => OnProgressChanged?.Invoke(percentage, this);

		protected virtual void UpdateProgress()
		{
			int sum = doneCounts![0];
			for (int i = 1; i < doneCounts.Length; i++)
				sum += doneCounts[i];
			ChangeProgress(sum * sizeCoeff);
		}
	}
}
