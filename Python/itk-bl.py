import sys
import itk
from stopwatch import stopwatch

noisyFile = sys.argv[1]
outputFile = sys.argv[2]

dim = int(sys.argv[3])
sigmaRange = float(sys.argv[4])
sigmaSpace = float(sys.argv[5])

sw = stopwatch()

imageType = itk.Image[itk.UC, dim]

reader = itk.ImageFileReader[imageType].New()
reader.SetFileName(noisyFile)

print("Applying Itk bilateral filter...", end="", flush=True)
sw.start()

bl = itk.BilateralImageFilter[imageType, imageType].New()
bl.SetInput(reader.GetOutput())
bl.SetDomainSigma(sigmaSpace)
bl.SetRangeSigma(sigmaRange)

writer = itk.ImageFileWriter[imageType].New()
writer.SetFileName(outputFile)
writer.SetInput(bl.GetOutput())
writer.Update()

sw.stop()
print("DONE")

print("Itk file saved ->", outputFile)
print("Time elapsed:", sw.elapsed())
