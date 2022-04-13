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

### Image

- Size: 620x620
- Format: GrayScale
- Source: https://www.ipol.im/pub/art/2011/bcm_nlm/article.pdf

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

![Bilateral filter](/Images/bl-noisy-vs-bilateral.png)

## Non-local means filter

| Parameter   | Value |
|:------------|:-----:|
| h           | 15    |
| Patch size  | 3x3   |
| Window size | 21x21 |

| Filter                    | Windows [s] | Ubuntu [s] |
|:--------------------------|------------:|-----------:|
| Pixel wise                | ~ 2.1       | ~ 2.2      |
| Patch wise                | ~ 2.5       | ~ 1.9      |
| Patch wise sampled        | ~ 0.8       | ~ 0.93     |
| Integral image (1 thread) | ~ 11.63     | ~ 9.15     |
| Integral image            | ~ 2.46      | ~ 2.2      |
| Integral image sampled    | ~ 0.73      | ~ 0.77     |
| OpenCV                    | ~ 0.84      | ~ 0.47     |

Noisy vs Pixel wise
![Non-local means filter](/Images/nlm-noisy-vs-pixel.png)
Noisy vs Patch wise / Integral image
![Non-local means filter](/Images/nlm-noisy-vs-patch.png)
Integral Image vs OpenCV
![Non-local means filter](/Images/nlm-fast-vs-opencv.png)

## Bilateral vs Non-local means

![Comparison](/Images/2d-cmp.png)

### Volumetric data

- Size: 183x255x125
- Data type: uint8
- Source: https://web.cs.ucdavis.edu/~okreylos/PhDStudies/Spring2000/ECS277/DataSets.html

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

| Filter                  | Windows      | Ubuntu       |
|:------------------------|-------------:|-------------:|
| Non-local means         | ~ 5.0 min    | ~ 4.0 min    |
| Non-local means sampled | ~ 1.7 min    | ~ 1.7 min    |
| scikit                  | ~ 7 min      | ~ 4.9 min    |

![3D Non-local means sampled](/Images/3dnlm-foot.png)

## 3D Bilateral vs 3D Non-local means

![3D bl vs nlm](/Images/3d-cmp.png)

### Testing environment

| Tool         | Windows 10                         | Ubuntu 20.04 (WSL 2) |
|--------------|-----------------------------------:|---------------------:|
| .NET runtime | 6.0.3                              | 6.0.3                |
| Python       | 3.10.2 [MSC v.1929 64 bit (AMD64)] | 3.8.10 [GCC 9.4.0]   |
| OpenCV       | 4.5.5                              | 4.5.5                |
| Itk          | 5.3.0                              | 5.2.1                |
| SimpleITK    | 2.2.0rc2.post35                    | 2.1.1                |
| scikit image | 0.19.2                             | 0.19.2               |
