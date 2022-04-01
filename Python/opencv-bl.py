import sys
import cv2 as cv
from stopwatch import stopwatch

noisyFile = sys.argv[1]
outputFile = sys.argv[2]

radius = float(sys.argv[3])
sigmaSpace = float(sys.argv[4])
sigmaRange = float(sys.argv[5])

sw = stopwatch()

img = cv.imread(noisyFile)

print("Applying OpenCV bilateral filter...", end="")
sw.start()
dst = cv.bilateralFilter(img, d=int(2 * radius + 1), sigmaColor=sigmaRange, sigmaSpace=sigmaSpace, borderType=cv.BORDER_ISOLATED)
sw.stop()
print("DONE")

cv.imwrite(outputFile, dst)
print("OpenCV file saved ->", outputFile)
print("Time elapsed:", sw.elapsed())
