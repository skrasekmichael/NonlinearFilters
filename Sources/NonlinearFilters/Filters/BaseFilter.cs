using NonlinearFilters.Filters.Interfaces;
using NonlinearFilters.Filters.Parameters;
using NonlinearFilters.Mathematics;

namespace NonlinearFilters.Filters
{
	public abstract class BaseFilter<TParameters> : IFilter, IFilterProgressChanged where TParameters : BaseFilterParameters
	{
		public event EventHandler<double>? OnProgressChanged;

		protected int[]? doneCounts = null;
		protected readonly double sizeCoeff;
		protected readonly DataPadder dataPadder = new();

		public TParameters Parameters { get; protected set; }
		protected bool Initalized { get; set; } = false;
		protected bool PreComputed { get; set; } = false;
		protected bool IsCanceled { get; private set; } = false;
		protected int Padding { get; set; } = 0;

		public BaseFilter(TParameters parameters, double sizeCoeff)
		{
			Parameters = parameters;
			this.sizeCoeff = sizeCoeff;
		}

		public void Initalize()
		{
			InitalizeParams();
			PreComputed = false;

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

		public void Cancel()
		{
			IsCanceled = true;
		}

		protected void ChangeProgress(double percentage) => OnProgressChanged?.Invoke(this, percentage);

		protected virtual void UpdateProgress()
		{
			int sum = doneCounts![0];
			for (int i = 1; i < doneCounts.Length; i++)
				sum += doneCounts[i];
			ChangeProgress(sum * sizeCoeff);
		}
	}
}
