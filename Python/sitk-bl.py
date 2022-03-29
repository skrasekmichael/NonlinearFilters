import os
import sys
import SimpleITK as sitk
from stopwatch import stopwatch

noisyFile = sys.argv[1]
outputFile = sys.argv[2]

sigmaRange = float(sys.argv[3])
sigmaSpace = float(sys.argv[4])

threadCount = os.cpu_count() - 1

sw = stopwatch()

bl = sitk.BilateralImageFilter()
bl.SetNumberOfThreads(threadCount)
bl.SetDomainSigma(sigmaSpace)
bl.SetRangeSigma(sigmaRange)
bl.SetNumberOfRangeGaussianSamples(1)

reader = sitk.ImageFileReader()
reader.SetFileName(noisyFile)
noisyImage = reader.Execute()

print("Applying SimpleItk bilateral filter [" + str(threadCount) + " threads]...", end="", flush=True)
sw.start()
image = bl.Execute(noisyImage)
sw.stop()
print("DONE")

writer = sitk.ImageFileWriter()
writer.SetFileName(outputFile)
writer.Execute(image)
print("SimpleItk file saved ->", outputFile)
print("Time elapsed:", sw.elapsed())
