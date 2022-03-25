using NonlinearFilters.VolumetricData;

namespace NonlinearFilters.APP.Messages
{
	public class RenderVolumeMessage : IMessage
	{
		public VolumetricData.VolumetricData Volume { get; }

		public RenderVolumeMessage(VolumetricData.VolumetricData volume)
		{
			Volume = volume;
		}
	}
}
