# Nonlinear Filters
**Bachelor's thesis**

Topic: Nonlinear filtering for large 3D image data (Bilateral filter, Non-local means filter). 

## Milestones
- 2D Bilateral filter (naive implementation) ✔
- Parallel processing ✔
- Fast 2D Bilateral filter (custom optimizations) ✔
- 2D Non-local means filter (patch wise implementation) ✔
- 2D Non-local means filter (pixel wise implementation) ✔
- Fast 2D Non-local means filter (integral image) ✔
- CLI for 2D ✔
- rendering 3D volumetric data ✔
- 3D Bilateral filter ✔
- 3D Non-local means filter ✔
- CLI for 3D ✔
- Piecewise processing for large 3D volumetric data
- Optimizations via parallel processing using SIMD

### Data samples

Image:
- House 620x620 (GrayScale) [source](https://www.ipol.im/pub/art/2011/bcm_nlm/article.pdf)

Volumetric data:
- CT Foot 183x255x125 (uint8) [source](https://web.cs.ucdavis.edu/~okreylos/PhDStudies/Spring2000/ECS277/DataSets.html)
- C60 64x64x64 (uint8) [source](https://web.cs.ucdavis.edu/~okreylos/PhDStudies/Spring2000/ECS277/DataSets.html)

## Bilateral filter

| Parameter   | Value |
|:------------|:-----:|
| Space sigma | 6     |
| Range sigma | 25.5  |

| Filter               | Windows [s] | Ubuntu [s] |
|:---------------------|------------:|-----------:|
| Bilateral (1 thread) | ~ 0.56      | ~ 0.51     |
| Bilateral            | ~ 0.16      | ~ 0.15     |
| OpenCV               | ~ 0.30      | ~ 0.32     |
| SimpleITK            | ~ 1.04      | ~ 0.70     |
| Itk                  | ~ 0.71      | ~ 0.78     |

Noisy vs Bilateral filter
![Bilateral filter](/Images/bl-noisy-vs-bilateral.png)

## Non-local means filter

| Parameter   | Value |
|:------------|:-----:|
| h           | 15    |
| Patch size  | 3x3   |
| Window size | 21x21 |
| Samples     | 500   |

| Filter                                   | Windows [s] | Ubuntu [s] |
|:-----------------------------------------|------------:|-----------:|
| Non-local means (1 thread)               | ~ 8.0       | ~ 6.26     |
| Non-local means (7 threads)              | ~ 2.25      | ~ 1.83     |
| Non-local means sampled (7 threads)      | ~ 1.58      | ~ 1.4      |
| Fast Non-local means (1 thread)          | ~ 3.36      | ~ 3.15     |
| Fast Non-local means (7 threads)         | ~ 2.1       | ~ 2.2      |
| Fast Non-local means sampled (7 threads) | ~ 1.5       | ~ 2        |
| OpenCV                                   | ~ 0.9       | ~ 0.4      |

*Note: Fast Non-local means filter complexity is independent to patch size due to integral image optimizations*

Noisy vs Non-local means filter
![Non-local means filter](/Images/nlm-vs-noisy.png)

Non-local means  filter vs OpenCV Non-local means filter
![Non-local means vs OpenCV filter](/Images/nlm-fast-vs-opencv.png)

Sampled Non-local means filter vs OpenCV Non-local means filter
![Sampled Non-local means vs OpenCV filter](/Images/nlm-sampled-vs-opencv.png)

## Bilateral vs Non-local means

![Comparison](/Images/2d-cmp.png)

## Rendering volumetric data
3D rendering of noisy volumetric data using ray casting

![noisy volumetric image](/Images/3drender.png)

## 3D Bilateral filter

| Parameter    | Value |
|:-------------|:-----:|
| Domain sigma | 5     |
| Range sigma  | 20    |

| Filter                     | Windows      | Ubuntu     |
|:---------------------------|-------------:|-----------:|
| Fast Bilateral (1 thread)  | ~ 1.3 min    | ~ 1.6 min  |
| Fast Bilateral (7 threads) | ~ 25 sec     | ~ 27 sec   |
| Simple Itk                 | ~ 14.9 min   | ~ 9.2 min  |
| Itk                        | ~ 12 min     | ~ 7.75 min |

Fast bilateral filter
![Fast 3D bilateral](/Images/3dbl.png)

*Note: There are no differences discernible to the eye between measured filters.*

## 3D Non-local means filter

| Parameter    | Value    |
|:-------------|:--------:|
| h            | 20       |
| Patch size   | 3x3x3    |
| Window size  | 15x15x15 |
| Samples      | 500      |

| Filter                                   | Windows      | Ubuntu       |
|:-----------------------------------------|-------------:|-------------:|
| Non-local means sampled (7 threads)      | ~ 10.6 min   | ~ 10.5 min   |
| Fast Non-local means sampled (7 threads) | ~ 4.56 min   | ~ 4.76 min   |
| scikit                                   | ~ 7 min      | ~ 4.9 min    |

*Note: Fast Non-local means filter complexity is independent to patch size due to integral image optimizations*

Non-local means sampled filter
![3D Non-local means sampled](/Images/3dnlm-foot.png)

scikit Non-local means filter
![3D Non-local scikit](/Images/3dnlm-scikit-foot.png)

## 3D Bilateral vs 3D Non-local means

![3D bl vs nlm](/Images/3d-cmp.png)

### Testing environment

| Tool         | Windows 10                         | Ubuntu 20.04 (WSL 2) |
|--------------|------------------------------------|----------------------|
| .NET runtime | 6.0.3                              | 6.0.3                |
| Python       | 3.10.2 [MSC v.1929 64 bit (AMD64)] | 3.8.10 [GCC 9.4.0]   |
| OpenCV       | 4.5.5                              | 4.5.5                |
| Itk          | 5.3.0                              | 5.2.1                |
| SimpleITK    | 2.2.0rc2.post35                    | 2.1.1                |
| scikit image | 0.19.2                             | 0.19.2               |

*Remainder:*

- Test script ([test.ps1](https://github.com/skrasekmichael/NonlinearFilters/blob/main/test.ps1)) requires 2 powershell scripts ([cmp-img.ps1](https://github.com/skrasekmichael/powershell/blob/main/scripts/cmp-img.ps1) and [join-img.ps1](https://github.com/skrasekmichael/powershell/blob/main/scripts/join-img.ps1) for Windows only) for full experience. 
- For comparing filters with python alternatives, it's required to install dependencies:
	- [Itk](https://pypi.org/project/itk/)
	- [SimpleItk](https://pypi.org/project/SimpleITK/)
	- [OpenCV](https://pypi.org/project/opencv-python/)
	- [scikit image](https://pypi.org/project/scikit-image/)

	...
