using NonlinearFilters.Filters.Interfaces;

namespace NonlinearFilters.APP.Messages
{
	public class ApplyFilter2Message : IMessage
	{
		public IFilter2Output Filter { get; }
		public int ProcessCount { get; }

		public ApplyFilter2Message(IFilter2Output filter, int processCount)
		{
			Filter = filter;
			ProcessCount = processCount;
		}
	}
}
