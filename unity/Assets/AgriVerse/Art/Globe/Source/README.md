# AgriVerse Globe Assets

Unity-ready equirectangular globe textures assembled from NASA source imagery. These assets are not from Tripo.

## Included files

| File | Size | Unity use |
|---|---:|---|
| `Textures/Earth/Earth_Color_8K.jpg` | 8192×4096 | Base Map / Albedo |
| `Textures/Earth/Earth_Bump_4K.png` | 4096×2048 | Height or parallax input |
| `Textures/Earth/Earth_NormalGL_4K.png` | 4096×2048 | Tangent-space normal map derived from the elevation map |
| `Textures/Clouds/Earth_Clouds_Transparent_4K.png` | 4096×2048 RGBA | Transparent cloud shell |
| `HDRI/Space_StarMap_8K.exr` | 8192×4096 OpenEXR | Equirectangular sky texture |

## Recommended Unity import settings

- **Earth color:** Texture Type `Default`; sRGB on; Wrap Mode `Repeat`; Max Size `8192`; Compression `High Quality`.
- **Earth bump:** Texture Type `Default`; sRGB off; Wrap Mode `Repeat`; Max Size `4096`.
- **Earth normal:** Texture Type `Normal map`; sRGB off; Wrap Mode `Repeat`; Max Size `4096`. It is OpenGL/Y+; if the material appears inverted, use Unity's Flip Green Channel option.
- **Clouds:** Texture Type `Default`; Alpha Source `Input Texture Alpha`; Alpha Is Transparency on; sRGB on; Wrap Mode `Repeat`; Max Size `4096`. Put it on a sphere approximately 1.005 times the Earth radius and use transparent blending.
- **Star map:** Texture Shape `Cube`; Mapping `Latitude-Longitude Layout`; sRGB on; use with a Skybox/Cubemap material. If Unity's pipeline rejects the EXR as a cubemap, import it as a 2D equirectangular texture and convert it to a cubemap inside Unity.

The maps use a latitude-longitude/equirectangular projection. Rotate the globe or adjust the material's horizontal offset to establish the desired opening-camera longitude; do not permanently alter the source textures.

See `SOURCE_AND_LICENSE.md` for provenance, credits, and redistribution notes. Verify file integrity using `SHA256SUMS.txt`.
