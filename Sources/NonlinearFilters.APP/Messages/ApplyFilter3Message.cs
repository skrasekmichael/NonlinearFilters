using NonlinearFilters.Filters.Interfaces;

namespace NonlinearFilters.APP.Messages
{
	public class ApplyFilter3Message : IMessage
	{
		public IFilter3Output Filter { get; }
		public int ProcessCount { get; }

		public ApplyFilter3Message(IFilter3Output filter, int processCount)
		{
			Filter = filter;
			ProcessCount = processCount;
		}
	}
}
