import sys
import cv2 as cv
from stopwatch import stopwatch

noisyFile = sys.argv[1]
outputFile = sys.argv[2]

patchRadius = int(sys.argv[3])
windowsRadius = int(sys.argv[4])
h = float(sys.argv[5])

sw = stopwatch()

img = cv.imread(noisyFile)

print("Applying OpenCV Non-local means filter...", end="")
sw.start()
dst = cv.fastNlMeansDenoising(img, h=h, block_size=int(patchRadius * 2 + 1), searchWindowSize=(windowsRadius * 2 + 1))
sw.stop()
print("DONE")

cv.imwrite(outputFile, dst)
print("OpenCV file saved -> " + outputFile)
print("Time elapsed:", sw.elapsed())
