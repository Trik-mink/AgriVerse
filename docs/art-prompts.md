# AgriVerse - Art Direction & Image-Generation Prompts (Flat Editorial Style)

Goal: a coherent, DESIGNED flat-illustration world that reads as intentional graphic design,
NOT AI-generated painterly art. Matches the clean flat aesthetic of the landing page.
Generate every asset with the SAME model and SAME style anchor appended, for one cohesive set.

## Locked art direction

- **Style:** flat modern editorial vector illustration. Bold simple shapes, limited harmonious
  palette, matte finish, minimal detail, clean geometric stylization, subtle screen-print/grain
  texture. Designed poster art. Explicitly NOT realistic, NOT painterly, NOT 3D render, NOT glossy.
- **Setting:** Vietnamese Mekong Delta - rice paddies, green-brown water channels, stilt houses,
  sampan boats, coconut palms, banana leaves.
- **Palette:** rice green, warm teal water, terracotta/clay red, soft sun yellow, cream. Flat
  color blocks, gentle warm light suggested through color, not rendered shadows.
- **Mood:** calm, hopeful, dignified, warm. Authentic Vietnamese rural life, no caricature.
- Original art only; never depict real Disney characters or scenes.

### Style anchor (append to EVERY prompt, verbatim)
> "...in a flat modern editorial vector illustration style: bold simple shapes, limited
> harmonious palette of rice green, warm teal, terracotta, sun yellow and cream, matte finish,
> minimal detail, clean geometric stylization, subtle screen-print grain, designed poster art,
> warm and hopeful. NOT realistic, NOT painterly, NOT 3D render, NOT glossy. No text, no
> watermark, original artwork."

### Technical settings
- **Backgrounds:** 16:9 landscape (1536x1024). Focal subject center/upper; keep lower-left and
  right thirds calmer for UI overlays. No real text/signage in the image.
- **Character portraits:** portrait/upper-body, on a soft SOLID neutral background (easy to mask),
  subject clearly separated from background, minimal facial detail, friendly, facing viewer.
- Generate 2-3 variations each; pick the best. Same model across the whole set. Use the first
  good character as a style reference for the rest so faces match.

---

## Station backgrounds (save to public/assets/scenes/)

- `hero.png`, `paddy.png`, `research-post.png`, `district-office.png`, `planning-dock.png`,
  `future-fields.png`, `reflection-pavilion.png`

Prompts and character prompts are given fully-assembled in the chat handoff. Base descriptions:

- HERO: sweeping Mekong Delta farming community - paddies, wide channel, stilt houses, palms, a
  sampan boat, distant fields.
- PADDY: lush rice paddy beside a channel, small wooden field shelter and sampling posts at the
  water's edge, young rice shoots, a conical hat and tools resting nearby.
- RESEARCH POST: open-sided riverside field lab, wooden posts, hanging maps/charts, glass water
  sample jars on a table, potted plants, notebooks.
- DISTRICT OFFICE: small tidy rural civic office by the channel, plaster walls, wooden desk with
  papers and a fan, window onto rice fields.
- PLANNING DOCK: shaded wooden planning table on a dock overlooking wide rice fields and channel,
  rolled maps and simple markers.
- FUTURE FIELDS: wide panorama of rice fields to the horizon under a big sky, some lush and some
  salt-stressed, suggesting passing seasons.
- REFLECTION PAVILION: serene waterside pavilion at warm sunset with soft lanterns and banners,
  peaceful review space, gently celebratory.

## Characters (save to public/assets/characters/)

- `mr-ba.png` (farmer, 50s, weathered kind face, conical hat, worn shirt)
- `dr-linh.png` (researcher, late 30s, glasses, hair tied back, field vest, notebook)
- `ms-hoa.png` (official, mid 40s, neat blouse, tidy hair, warm authority)
- `guide.png` (cheerful young field companion, welcoming, scarf)
- `player-1.png`..`player-4.png` (four diverse young student-investigators, field bags)

## Attribution
Save finals and log each in `public/assets/ATTRIBUTIONS.md` as
"AI-generated, original, [model], 2026-07-17".
