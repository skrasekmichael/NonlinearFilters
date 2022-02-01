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
| Space sigma | 15    |
| Range sigma | 25.5  |

| Filter variant | Time \[s\]|
|:---------------|----------:|
| Bilateral      | 1.8573116 |
| Fast Bilateral | 0.1328743 |

![Bilateral filter](/Images/bl-noisy-vs-bilateral.png)

## Non-local means filter

| Parameter   | Value |
|:------------|:-----:|
| h           | 6     |
| Patch size  | 3x3   |
| Window size | 21x21 |

| Filter variant | Time \[s\] |
|:---------------|-----------:|
| Pixel wise     | 20.6308629 |
| Patch wise     | 2.6464456  |
| Integral image | 2.8232714  |

Pixel wise
![Non-local means filter](/Images/nlm-noisy-vs-pixel.png)
Patch wise / Integral image
![Non-local means filter](/Images/nlm-noisy-vs-patch.png)
