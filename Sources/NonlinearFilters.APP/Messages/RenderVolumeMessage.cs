using NonlinearFilters.Volume;

namespace NonlinearFilters.APP.Messages
{
	public class RenderVolumeMessage : IMessage
	{
		public VolumetricData Volume { get; }

		public RenderVolumeMessage(VolumetricData volume)
		{
			Volume = volume;
		}
	}
}
