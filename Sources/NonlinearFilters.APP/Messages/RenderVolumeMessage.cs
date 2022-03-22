using NonlinearFilters.VolumetricData;

namespace NonlinearFilters.APP.Messages
{
	public class RenderVolumeMessage : IMessage
	{
		public BaseVolumetricData Volume { get; }

		public RenderVolumeMessage(BaseVolumetricData volume)
		{
			Volume = volume;
		}
	}
}
