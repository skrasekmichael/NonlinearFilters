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
- CLI for 3D
- Piecewise processing for large 3D volumetric data

### Image

- Size: 620x620
- Format: GrayScale
- Source: https://www.ipol.im/pub/art/2011/bcm_nlm/article.pdf

## Bilateral filter

| Parameter   | Value | Filter         | Time \[s\]|
|:------------|:-----:|:---------------|----------:|
| Space sigma | 6     | Bilateral      | ~1.46     |
| Range sigma | 25.5  | Fast Bilateral | ~0.16     |

![Bilateral filter](/Images/bl-noisy-vs-bilateral.png)

## Non-local means filter

| Parameter   | Value | Filter         | Time \[s\] |
|:------------|:-----:|:---------------|-----------:|
| h           | 15    | Pixel wise     | ~20.4      |
| Patch size  | 3x3   | Patch wise     | ~2.5       |
| Window size | 21x21 | Integral image | ~2.5       |
|             |       | OpenCV         | ~1.05      |

Noisy vs Pixel wise
![Non-local means filter](/Images/nlm-noisy-vs-pixel.png)
Noisy vs Patch wise / Integral image
![Non-local means filter](/Images/nlm-noisy-vs-patch.png)
Integral Image vs OpenCV
![Non-local means filter](/Images/nlm-fast-vs-opencv.png)

## Bilateral vs Non-local means

![Edge preservation](/Images/edge-preservation.png)

### Volumetric data

- Size: 183x255x125
- Data type: uint8
- Source: https://web.cs.ucdavis.edu/~okreylos/PhDStudies/Spring2000/ECS277/DataSets.html

## Rendering volumetric data
3D rendering of noisy volumetric data using ray casting

![noisy volumetric image](/Images/3drender.png)

## 3D Bilateral filter

| Parameter    | Value | Filter              | Time         |
|:-------------|:-----:|:--------------------|-------------:|
| Domain sigma | 5     | Fast Bilateral      | ~ 37 sec     |
| Range sigma  | 15    | Itk (Python)        | ~ 12.5 min   |
|              |       | Simple Itk (Python) | ~ 14.5 min   |
|              |       | 3D Slicer           | 15+ min      |

Fast bilateral filter
![Fast 3D bilateral](/Images/3dbl.png)

*Note: There are no differences discernible to the eye between measured filters.*

## 3D Non-local means filter

| Parameter    | Value    | Filter              | Time         |
|:-------------|:--------:|:--------------------|-------------:|
| h            | 20       | Non-local means     | ~ 6.1 min    |
| Patch size   | 3x3x3    |                     |              |
| Window size  | 15x15x15 |                     |              |

![3D Non-local means](/Images/3dnlm-foot.png)

## 3D Bilateral vs 3D Non-local means

![3D bl vs nlm](/Images/3d-cmp.png)
