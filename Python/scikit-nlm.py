import sys
import nrrd
from skimage import img_as_ubyte
from skimage.restoration import denoise_nl_means
from stopwatch import stopwatch

noisyFile = sys.argv[1]
outputFile = sys.argv[2]

patchRadius = int(sys.argv[3])
windowRadius = int(sys.argv[4])
h = float(sys.argv[5]) / 255

sw = stopwatch()

noisyData, header = nrrd.read(noisyFile)

print("Applying scikit 3D non-local means filter...", end="", flush=True)
sw.start()
data = denoise_nl_means(
	image=noisyData,
	patch_size=int(2 * patchRadius + 1),
	patch_distance=windowRadius,
	h=h, 
	preserve_range=False,
	channel_axis=None,
	fast_mode=True
)
sw.stop()
print("DONE")

outputData = img_as_ubyte(data)
nrrd.write(outputFile, outputData)
print("Scikit file saved ->", outputFile)
print("Time elapsed:", sw.elapsed())
