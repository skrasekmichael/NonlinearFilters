using NonlinearFilters.Filters;
using NonlinearFilters.Filters.Interfaces;
using NonlinearFilters.Filters.Parameters;
using NonlinearFilters.Mathematics;

namespace NonlinearFilters.Filters3D
{
	public abstract class BaseFilter3<TParameters> : BaseFilter<TParameters>, IFilter3 where TParameters : BaseFilterParameters
	{
		public VolumetricImage Target { get; }

		public BaseFilter3(ref VolumetricImage input, TParameters parameters) : base(parameters, 100.0 / (input.Size.X * input.Size.Y * input.Size.Z))
		{
			Target = input;
		}

		public abstract VolumetricImage ApplyFilter(int cpuCount = 1);

		protected VolumetricImage FilterArea(int cpuCount, Action<Block, VolumetricImage, VolumetricImage, int> filterBlock)
		{
			cpuCount = Math.Clamp(cpuCount, 1, Environment.ProcessorCount);
			doneCounts = new int[cpuCount];

			if (!Initalized)
				Initalize();

			var output = new VolumetricImage(Target.Size, Target.Ratio, Target.Border);

			if (!PreComputed)
			{
				PreCompute();
				PreComputed = true;
			}

			if (cpuCount == 1)
			{
				filterBlock(new(new(0, 0, 0), Target.Size), Target, output, 0);
			}
			else
			{
				var blocks = Split(cpuCount);
				var tasks = new Task[cpuCount - 1];

				for (int i = 0; i < cpuCount - 1; i++)
				{
					int index = i; //save index into task scope
					tasks[index] = Task.Factory.StartNew(() => filterBlock(blocks[index], Target, output, index));
				}

				filterBlock(blocks[cpuCount - 1], Target, output, cpuCount - 1);
				Task.WaitAll(tasks);
			}

			return output;
		}

		protected virtual unsafe void PreCompute() { }

		protected Block[] Split(int count)
		{
			int windowSize = (int)Math.Floor((double)Target.Size.X / count);
			int last = count - 1;

			var blocks = new Block[count];
			for (int i = 0; i < last; i++)
				blocks[i] = new(i * windowSize, 0, 0, windowSize, Target.Size.Y, Target.Size.Z);

			int remaining = Target.Size.X % count;
			blocks[last] = new(last * windowSize, 0, 0, windowSize + remaining, Target.Size.Y, Target.Size.Z);

			return blocks;
		}
	}
}
