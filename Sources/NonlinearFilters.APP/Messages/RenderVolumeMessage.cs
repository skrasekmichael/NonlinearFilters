using NonlinearFilters.Mathematics;

namespace NonlinearFilters.APP.Messages
{
	public class RenderVolumeMessage : IMessage
	{
		public VolumetricImage Volume { get; }

		public RenderVolumeMessage(VolumetricImage volume)
		{
			Volume = volume;
		}
	}
}
