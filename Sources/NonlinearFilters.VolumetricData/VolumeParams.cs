using OpenTK.Mathematics;

namespace NonlinearFilters.VolumetricData;

public class VolumeParams
{
	public Vector3i Size { get; }
	public Vector3d Ratio { get; }
	public int Border { get; }

	public VolumeParams(Vector3i size, Vector3d ratio, int border)
	{
		Size = size;
		Ratio = ratio;
		Border = border;
	}
}
