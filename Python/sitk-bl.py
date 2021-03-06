import os
import sys
import SimpleITK as sitk
from stopwatch import stopwatch

noisyFile = sys.argv[1]
outputFile = sys.argv[2]

sigmaSpace = float(sys.argv[3])
sigmaRange = float(sys.argv[4])

threadCount = os.cpu_count() - 1

sw = stopwatch()

bl = sitk.BilateralImageFilter()
bl.SetNumberOfThreads(threadCount)
bl.SetDomainSigma(sigmaSpace)
bl.SetRangeSigma(sigmaRange)

reader = sitk.ImageFileReader()
reader.SetFileName(noisyFile)
noisyImage = reader.Execute()

print("Applying SimpleITK bilateral filter [" + str(threadCount) + " threads]...", end="", flush=True)
sw.start()
image = bl.Execute(noisyImage)
sw.stop()
print("DONE")

writer = sitk.ImageFileWriter()
writer.SetFileName(outputFile)
writer.Execute(image)
print("SimpleITK file saved ->", outputFile)
print("Time elapsed:", sw.elapsed())
