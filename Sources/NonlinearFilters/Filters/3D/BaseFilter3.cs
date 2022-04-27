using NonlinearFilters.Filters;
using NonlinearFilters.Filters.Interfaces;
using NonlinearFilters.Filters.Parameters;
using NonlinearFilters.Mathematics;
using NonlinearFilters.Volume;

namespace NonlinearFilters.Filters3D
{
	/// <summary>
	/// Base class for 3D filters
	/// </summary>
	/// <typeparam name="TParameters">Filter parameters</typeparam>
	public abstract class BaseFilter3<TParameters> : BaseFilter<TParameters>, IFilter3 where TParameters : BaseFilterParameters
	{
		/// <summary>
		/// Input volumetric data
		/// </summary>
		public VolumetricData Input { get; }

		/// <summary>
		/// Initializes new instance of the <see cref="BaseFilter3{TParameters}"/> class.
		/// </summary>
		/// <param name="input">Input volumetric data</param>
		/// <param name="parameters">Filter parameters</param>
		public BaseFilter3(ref VolumetricData input, TParameters parameters) : base(parameters, 100.0 / (input.Size.X * input.Size.Y * input.Size.Z))
		{
			Input = input;
		}

		/// <summary>
		/// Applies filter on input volumetric data.
		/// </summary>
		/// <param name="cpuCount">Number of processors for parallel filtering</param>
		/// <returns>Filtered volumetric data</returns>
		public abstract VolumetricData ApplyFilter(int cpuCount = 1);

		protected VolumetricData FilterArea(int cpuCount, Action<Block, VolumetricData, VolumetricData, int> filterBlock)
		{
			cpuCount = Math.Clamp(cpuCount, 1, Environment.ProcessorCount);
			doneCounts = new int[cpuCount];

			if (!Initalized)
				Initalize();

			var output = Input.Create(); //creates new volumetric data with same properties
			VolumetricData paddedInput = Input, paddedOutput = output;

			if (Padding > 0)
			{
				paddedInput = dataPadder.CreatePadding(Input, Padding);
				paddedOutput = dataPadder.CreatePadding(output, Padding);
			}

			if (!PreComputed)
			{
				PreCompute();
				PreComputed = true;
			}

			BeforeFilter(paddedInput, paddedOutput, cpuCount);
			if (cpuCount == 1)
			{
				filterBlock(new(new(Padding, Padding, Padding), Input.Size), paddedInput, paddedOutput, 0);
			}
			else
			{
				//parallel filtering
				int last = cpuCount - 1;
				var blocks = Split(cpuCount);
				var tasks = new Task[last];

				for (int i = 0; i < last; i++)
				{
					int index = i; //save index into task scope
					tasks[index] = Task.Factory.StartNew(() => filterBlock(blocks[index], paddedInput, paddedOutput, index), TaskCreationOptions.LongRunning);
				}

				filterBlock(blocks[last], paddedInput, paddedOutput, last);
				Task.WaitAll(tasks);
			}

			if (Padding > 0)
			{
				dataPadder.RemovePaddding(paddedOutput, output, Padding);
			}

			return output;
		}

		/// <summary>
		/// Precomputes algorithm parameters for filtering  (depending on input parameters).
		/// </summary>
		protected virtual unsafe void PreCompute() { }

		/// <summary>
		/// Runs synchronously before parallel filter.
		/// </summary>
		/// <param name="input">Input volumetric data</param>
		/// <param name="output">Output volumetric data</param>
		/// <param name="cpuCount">Number of processors for parallel filtering</param>
		protected virtual void BeforeFilter(VolumetricData input, VolumetricData output, int cpuCount) { }

		/// <summary>
		/// Splits input volumetric data for parallel filtering.
		/// </summary>
		/// <param name="count">Number of blocks</param>
		/// <returns>Array of blocks representing coordinates and sizes of block in volumetric data</returns>
		protected Block[] Split(int count)
		{
			//splitting alongside of least varying axis
			int windowSize = (int)Math.Floor((double)Input.Size.X / count);
			int last = count - 1;

			var blocks = new Block[count];
			for (int i = 0; i < last; i++)
				blocks[i] = new(Padding + i * windowSize, Padding, Padding, windowSize, Input.Size.Y, Input.Size.Z);

			int remaining = Input.Size.X % count;
			blocks[last] = new(Padding + last * windowSize, Padding, Padding, windowSize + remaining, Input.Size.Y, Input.Size.Z);

			return blocks;
		}
	}
}
