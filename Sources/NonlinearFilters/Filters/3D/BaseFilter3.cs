using NonlinearFilters.Filters;
using NonlinearFilters.Filters.Interfaces;
using NonlinearFilters.Filters.Parameters;
using NonlinearFilters.Mathematics;
using NonlinearFilters.VolumetricData;

namespace NonlinearFilters.Filters3D
{
	public abstract class BaseFilter3<TParameters> : BaseFilter<TParameters>, IFilter3 where TParameters : BaseFilterParameters
	{
		public BaseVolumetricData Input { get; }

		public BaseFilter3(ref BaseVolumetricData input, TParameters parameters) : base(parameters, 100.0 / (input.Size.X * input.Size.Y * input.Size.Z))
		{
			Input = input;
		}

		public abstract BaseVolumetricData ApplyFilter(int cpuCount = 1);

		protected BaseVolumetricData FilterArea(int cpuCount, Action<Block, BaseVolumetricData, BaseVolumetricData, int> filterBlock)
		{
			cpuCount = Math.Clamp(cpuCount, 1, Environment.ProcessorCount);
			doneCounts = new int[cpuCount];

			if (!Initalized)
				Initalize();

			var output = Input.Create();

			if (!PreComputed)
			{
				PreCompute();
				PreComputed = true;
			}

			if (cpuCount == 1)
			{
				filterBlock(new(new(0, 0, 0), Input.Size), Input, output, 0);
			}
			else
			{
				var blocks = Split(cpuCount);
				var tasks = new Task[cpuCount - 1];

				for (int i = 0; i < cpuCount - 1; i++)
				{
					int index = i; //save index into task scope
					tasks[index] = Task.Factory.StartNew(() => filterBlock(blocks[index], Input, output, index));
				}

				filterBlock(blocks[cpuCount - 1], Input, output, cpuCount - 1);
				Task.WaitAll(tasks);
			}

			return output;
		}

		protected virtual unsafe void PreCompute() { }

		protected Block[] Split(int count)
		{
			int windowSize = (int)Math.Floor((double)Input.Size.X / count);
			int last = count - 1;

			var blocks = new Block[count];
			for (int i = 0; i < last; i++)
				blocks[i] = new(i * windowSize, 0, 0, windowSize, Input.Size.Y, Input.Size.Z);

			int remaining = Input.Size.X % count;
			blocks[last] = new(last * windowSize, 0, 0, windowSize + remaining, Input.Size.Y, Input.Size.Z);

			return blocks;
		}
	}
}
