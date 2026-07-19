# Source and license record

Generated on 2026-07-19 for the AgriVerse Unity project. No asset in this bundle came from Tripo.

NASA imagery is generally not subject to copyright in the United States when produced solely by NASA, but NASA names, insignia, identifiers, and some third-party material have separate restrictions. Keep the credits below, do not imply NASA endorsement, and follow NASA's current media guidelines: https://www.nasa.gov/nasa-brand-center/images-and-media/

## Earth color

- Output: `Textures/Earth/Earth_Color_8K.jpg`
- Source: NASA Earth Observatory, Blue Marble Next Generation — Base Topography and Bathymetry, July 2004.
- Source page: https://science.nasa.gov/earth/earth-observatory/blue-marble-next-generation/base-topography-bathymetry/
- Direct source used: https://assets.science.nasa.gov/content/dam/science/esd/eo/images/bmng/bmng-topography-bathymetry/july/world.topo.bathy.200407.3x21600x10800.jpg
- Transformation: resized from 21600×10800 to 8192×4096 and saved as high-quality JPEG.
- Credit: NASA Earth Observatory; Blue Marble Next Generation. Visualization by Reto Stöckli and Robert Simmon, based on NASA data.

## Earth bump and derived normal map

- Outputs: `Textures/Earth/Earth_Bump_4K.png`, `Textures/Earth/Earth_NormalGL_4K.png`
- Source: NASA Earth Observatory / GEBCO 08 global elevation data.
- Source record: https://visibleearth.nasa.gov/images/73934/topography
- Direct source used: https://eoimages.gsfc.nasa.gov/images/imagerecords/73000/73934/gebco_08_rev_elev_21600x10800.png
- Transformations: grayscale elevation resized from 21600×10800 to 4096×2048; the OpenGL tangent-space normal map was computed from wrapped horizontal and clamped vertical height gradients.
- Credit: NASA Earth Observatory, using GEBCO 08 elevation data.

## Transparent clouds

- Output: `Textures/Clouds/Earth_Clouds_Transparent_4K.png`
- Source: NASA Earth Observatory, Blue Marble cloud map.
- Source record: https://visibleearth.nasa.gov/images/57747/blue-marble-clouds
- Direct source used: https://eoimages.gsfc.nasa.gov/images/imagerecords/57000/57747/cloud_combined_8192.tif
- Transformation: resized from 8192×4096 to 4096×2048; source luminance was placed in the alpha channel over white RGB to create a transparent cloud-shell texture.
- Credit: NASA Earth Observatory / Blue Marble.

## Space star map / HDRI

- Output: `HDRI/Space_StarMap_8K.exr`
- Source: NASA Scientific Visualization Studio, Deep Star Maps 2020.
- Source page: https://svs.gsfc.nasa.gov/4851/
- Direct source used: https://svs.gsfc.nasa.gov/vis/a000000/a004800/a004851/starmap_2020_8k.exr
- Transformation: none; original 8192×4096 OpenEXR retained.
- Credit: NASA/Goddard Space Flight Center Scientific Visualization Studio. The visualization incorporates Gaia DR2 data from ESA/Gaia/DPAC and catalog data identified on the source page; retain those acknowledgements when publishing credits.

## Redistribution statement

This bundle records source URLs and modifications for repository review. It is not labelled CC0. Redistribution must follow NASA's media guidelines and preserve applicable third-party catalogue acknowledgements. NASA logos and insignia are not included.
