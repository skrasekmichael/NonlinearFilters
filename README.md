# Nonlinear Filters
**Bachelor's thesis**

Topic: Nonlinear filtering for large 3D image data (Bilateral filter, Non-local means filter). 

## Milestones
- 2D Bilateral filter (naive implementation) ✔
- Fast 2D Bilateral filter (custom optimizations) ✔
- Fast 2D Bilateral filter (approximation, other optimization's ...) ❌
- 2D Non-local means filter (patch wise implementation) ✔
- 2D Non-local means filter (pixel wise implementation) ✔
- Fast 2D Non-local means filter (integral image) ✔
- CLI ✔
- rendering 3D volumetric data ✔

...
- 3D Bilateral filter
- 3D Non-local means filter

## Results

### Image

- Size: 620x620
- Depth: 32b GrayScale
- Source: https://www.ipol.im/pub/art/2011/bcm_nlm/article.pdf

## Bilateral filter

| Parameter   | Value |
|:------------|:-----:|
| Space sigma | 6     |
| Range sigma | 25.5  |

| Filter variant | Time \[s\]|
|:---------------|----------:|
| Bilateral      | 1.8596089 |
| Fast Bilateral | 0.2009760 |

![Bilateral filter](/Images/bl-noisy-vs-bilateral.png)

## Non-local means filter

| Parameter   | Value |
|:------------|:-----:|
| h           | 15    |
| Patch size  | 3x3   |
| Window size | 21x21 |

| Filter variant | Time \[s\] |
|:---------------|-----------:|
| Pixel wise     | 20.2834070 |
| Patch wise     | 3.2618740  |
| Integral image | 2.6450633  |

Pixel wise
![Non-local means filter](/Images/nlm-noisy-vs-pixel.png)
Patch wise / Integral image
![Non-local means filter](/Images/nlm-noisy-vs-patch.png)

## Bilateral vs Non-local means

Edge preservation
![Edge preservation](/Images/edge-preservation.png)


## Rendering volumetric data
3D rendering volumetric data using ray casting

![Volumetric image](/Images/3drender.png)
