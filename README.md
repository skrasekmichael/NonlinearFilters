# Nonlinear Filters
**Bachelor's thesis**

Topic: Nonlinear denoising filters for 3D filtration (Bilateral filter, Non-local means filter). 

## Milestones
- 2D Bilateral filter (naive implementation) ✔
- Fast 2D Bilateral filter (custom optimizations) ✔
- Fast 2D Bilateral filter (aproximation, other optimizaions ...) ❌
- 2D Non-local means filter (patchwise implementation) ✔
- 2D Non-local means filter (pixelwise implementation) ✔
- Fast 2D Non-local means filter (custom optimizaions, integral image ...)

...
- 3D Bilateral filter
- 3D Non-local means filter

## Results

### Image

- Size: 620x620
- Depth: 32b grayscale
- Source: https://www.ipol.im/pub/art/2011/bcm_nlm/article.pdf

## Bilateral filter

| Parameter     | Value |
|:--------------|:-----:|
| Spatial sigma | 15    |
| Range sigma   | 0.1   |

![Bilateral filter](/Images/bilateral.png)

## Non-local means filter (patchwise)

| Parameter | Value |
|:----------|:-----:|
| Sigma     | 15    |

![Non-local means filter](/Images/nonlocal.png)
