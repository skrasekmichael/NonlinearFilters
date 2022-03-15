using NonlinearFilters.APP.Models;

namespace NonlinearFilters.APP.Messages
{
	public class SaveMessage : IMessage
	{
		public DataInput Data { get; }
		public SaveMessage(DataInput data)
		{
			Data = data;
		}
	}
}
