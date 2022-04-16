using NonlinearFilters.Filters;
using NonlinearFilters.Filters.Interfaces;
using NonlinearFilters.Filters.Parameters;
using NonlinearFilters.Mathematics;
using NonlinearFilters.Volume;

namespace NonlinearFilters.Filters3D
{
	public abstract class BaseFilter3<TParameters> : BaseFilter<TParameters>, IFilter3 where TParameters : BaseFilterParameters
	{
		public VolumetricData Input { get; }

		public BaseFilter3(ref VolumetricData input, TParameters parameters) : base(parameters, 100.0 / (input.Size.X * input.Size.Y * input.Size.Z))
		{
			Input = input;
		}

		public abstract VolumetricData ApplyFilter(int cpuCount = 1);

		protected VolumetricData FilterArea(int cpuCount, Action<Block, VolumetricData, VolumetricData, int> filterBlock)
		{
			cpuCount = Math.Clamp(cpuCount, 1, Environment.ProcessorCount);
			doneCounts = new int[cpuCount];

			if (!Initalized)
				Initalize();

			var output = Input.Create();
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

		protected virtual unsafe void PreCompute() { }

		protected virtual void BeforeFilter(VolumetricData input, VolumetricData output, int cpuCount) { }

		protected Block[] Split(int count)
		{
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
