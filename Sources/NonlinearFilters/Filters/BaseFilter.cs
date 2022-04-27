using NonlinearFilters.Filters.Interfaces;
using NonlinearFilters.Filters.Parameters;
using NonlinearFilters.Mathematics;

namespace NonlinearFilters.Filters
{
	/// <summary>
	/// Base abstraction for filters
	/// </summary>
	/// <typeparam name="TParameters">Filter parameters</typeparam>
	public abstract class BaseFilter<TParameters> : IFilter, IFilterProgressChanged where TParameters : BaseFilterParameters
	{
		public event EventHandler<double>? OnProgressChanged;

		protected int[]? doneCounts = null;
		protected readonly double sizeCoeff;
		protected readonly DataPadder dataPadder = new();

		/// <summary>
		/// Filter parameters
		/// </summary>
		public TParameters Parameters { get; protected set; }
		/// <summary>
		/// True if filter was initialized.
		/// </summary>
		protected bool Initalized { get; set; } = false;
		/// <summary>
		/// True if filter was precomputed.
		/// </summary>
		protected bool PreComputed { get; set; } = false;
		/// <summary>
		/// True if filtering has been canceled.
		/// </summary>
		protected bool IsCanceled { get; private set; } = false;
		/// <summary>
		/// Padding around data
		/// </summary>
		protected int Padding { get; set; } = 0;

		/// <summary>
		/// Initializes new instance of the <see cref="BaseFilter{TParameters}"/> class.
		/// </summary>
		/// <param name="parameters"></param>
		/// <param name="sizeCoeff"></param>
		public BaseFilter(TParameters parameters, double sizeCoeff)
		{
			Parameters = parameters;
			this.sizeCoeff = sizeCoeff;
		}

		/// <summary>
		/// Initializes parameters and filter
		/// </summary>
		public void Initalize()
		{
			InitalizeParams();
			PreComputed = false;

			InitalizeFilter();
			Initalized = true;
		}

		/// <summary>
		/// Initializes filter.
		/// </summary>
		protected virtual void InitalizeFilter() { }

		/// <summary>
		/// Updates filter parameters.
		/// </summary>
		/// <param name="parameters">New parameters</param>
		public void UpdateParameters(TParameters parameters)
		{
			Parameters = parameters;
			InitalizeParams();
			PreComputed = false;
		}

		/// <summary>
		/// Initializes parameters.
		/// </summary>
		protected abstract void InitalizeParams();

		/// <summary>
		/// Cancels filtering.
		/// </summary>
		public void Cancel()
		{
			IsCanceled = true;
		}

		protected void ChangeProgress(double percentage) => OnProgressChanged?.Invoke(this, percentage);

		/// <summary>
		/// Update progress from all threads.
		/// </summary>
		protected virtual void UpdateProgress()
		{
			int sum = doneCounts![0];
			for (int i = 1; i < doneCounts.Length; i++)
				sum += doneCounts[i];
			ChangeProgress(sum * sizeCoeff);
		}
	}
}
