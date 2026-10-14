# -*- coding: utf-8 -*-
"""La Nubi 3D para la app y el ícono (Tarea 92): toma el render `isla.py --solo-nubi --mirada frente` (PNG con fondo transparente) y deja lo que entra a `app/src/main/res/`:
   - `drawable-nodpi/nubi_3d.webp`: Nubi de frente recortada a su caja (720 px de ancho: a 3 px por dp sirve hasta 240 dp), para todas las pantallas (reemplaza a los `nubi_hola/mira/celebra/cientifica.webp` planos);
   - ícono adaptativo (`mipmap-*/ic_launcher_foreground.png` y `ic_launcher_monochrome.png`, lienzo de 108 dp × densidad): Nubi encuadrada en la zona segura del 66 % (círculo de 66 dp) con una sombra suave debajo; el monocromo es su silueta con los ojos (aro y pupila) y la sonrisa calados;
   - íconos de Android 7 (`mipmap-*/ic_launcher.webp` cuadrado redondeado y `ic_launcher_round.webp` circular, 48 dp × densidad): el fondo azul marino de la lámina más Nubi;
   - ícono de notificación (`drawable-*/ic_stat_neurovida.png`, 24 dp × densidad): la silueta blanca de Nubi, recortada.
   El fondo del ícono adaptativo es un vector (`res/drawable/ic_launcher_background.xml`): el azul marino de la lámina B (`#23255A`, un poco más claro arriba).
Uso: python -I icono.py <render_png> <carpeta_salida_res>        p. ej.  python -I tools/isla/icono.py tools/isla/out/nubi3d/capa-nubi-frente.png app/src/main/res
(Si el render cambia, volver a correr esto; `nubi_recursos.py` ya NO genera a Nubi: solo los planetas de las áreas.)"""
import os, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

src = sys.argv[1]
res = sys.argv[2]
DENS = [('mdpi', 1.0), ('hdpi', 1.5), ('xhdpi', 2.0), ('xxhdpi', 3.0), ('xxxhdpi', 4.0)]
NAVY_TOP = (0x2F, 0x31, 0x67); NAVY_BOTTOM = (0x23, 0x25, 0x5A)          # el azul marino de la lámina B: #23255A, apenas más claro arriba
SAFE = 66 / 108                                                           # zona segura del ícono adaptativo: un círculo de 66 dp en un lienzo de 108


def path(rel): p = os.path.join(res, rel); os.makedirs(os.path.dirname(p), exist_ok=True); return p


im = Image.open(src).convert('RGBA')
nubi = im.crop(im.getbbox())                                              # Nubi recortada a su caja (con el render a 3000 px: ~860 × 568)
W0, H0 = nubi.size
a0 = np.asarray(nubi.getchannel('A'))

# ---- Nubi para las pantallas
w = 720; h = round(H0 * w / W0)
nubi.resize((w, h), Image.LANCZOS).save(path('drawable-nodpi/nubi_3d.webp'), 'WEBP', quality=92, alpha_quality=100, method=6)

# ---- ícono: Nubi dentro de la zona segura
ys, xs = np.nonzero(a0 > 24)
cx0, cy0 = (xs.min() + xs.max()) / 2, (ys.min() + ys.max()) / 2
r1 = np.hypot(xs - cx0, ys - cy0).max()                                  # radio (en px del recorte) que contiene a toda Nubi
S = 432                                                                   # el lienzo a 4× (xxxhdpi); las demás densidades se reducen de acá
R = SAFE * S / 2 * 0.96                                                  # radio disponible, con un 4 % de aire dentro de la zona segura
k = R / r1
sw, sh = round(W0 * k), round(H0 * k)
body = nubi.resize((sw, sh), Image.LANCZOS)
DY = -6                                                                   # Nubi un poco arriba del centro: debajo va su sombra
px, py = round(S / 2 - cx0 * k), round(S / 2 - cy0 * k) + DY


def sombra(canvas_size):
    """Una elipse suave de tinta debajo de Nubi, como la de la isla."""
    sh_ = Image.new('RGBA', (canvas_size, canvas_size), (0, 0, 0, 0))
    ex, ey = canvas_size / 2, py + (ys.max() * k) + 10
    rx = (xs.max() - xs.min()) * k * 0.46; ry = rx * 0.17
    ImageDraw.Draw(sh_).ellipse([ex - rx, ey - ry, ex + rx, ey + ry], fill=(12, 10, 40, 150))
    return sh_.filter(ImageFilter.GaussianBlur(canvas_size * 0.012))


fg = Image.new('RGBA', (S, S), (0, 0, 0, 0))
fg.alpha_composite(sombra(S))
fg.alpha_composite(body, (px, py))

# ---- el monocromo: la silueta de Nubi con los ojos y la sonrisa calados (lo oscuro de la cara); el sistema la tiñe
rgb = np.asarray(body.convert('RGB')).astype(float) / 255.0
lum = 0.2126 * rgb[..., 0] + 0.7152 * rgb[..., 1] + 0.0722 * rgb[..., 2]
alpha = np.asarray(body.getchannel('A')).astype(float)
anillo = (rgb[..., 0] - rgb[..., 2] > 0.06) & (lum > 0.65)                    # el aro crema de cada ojo (el cuerpo es lila: azul más alto que rojo)
alpha[((lum < 0.20) | anillo) & (alpha > 200)] = 0                          # ojos (aro y pupila) y sonrisa calados
mono_body = Image.new('RGBA', body.size, (255, 255, 255, 0)); mono_body.putalpha(Image.fromarray(alpha.astype(np.uint8)))
mono = Image.new('RGBA', (S, S), (0, 0, 0, 0)); mono.alpha_composite(mono_body, (px, py))

for name, d in DENS:
    s = int(108 * d)
    fg.resize((s, s), Image.LANCZOS).save(path(f'mipmap-{name}/ic_launcher_foreground.png'))
    mono.resize((s, s), Image.LANCZOS).save(path(f'mipmap-{name}/ic_launcher_monochrome.png'))

# ---- íconos de Android 7: el fondo azul marino (con su degradado) + Nubi, en cuadrado redondeado y en círculo
def fondo(side):
    g = np.zeros((side, side, 3), dtype=np.uint8)
    for c in range(3): g[..., c] = np.linspace(NAVY_TOP[c], NAVY_BOTTOM[c], side)[:, None].astype(np.uint8)
    return Image.fromarray(g, 'RGB').convert('RGBA')


def completo(side):
    big = side * 4
    base = fondo(big); full = fg.resize((big, big), Image.LANCZOS)                    # el lienzo del adaptativo (108 dp) cubre todo el ícono: se escala a 1,5× para llenar el de 72 dp visibles
    k2 = 1.38
    z = full.resize((round(big * k2), round(big * k2)), Image.LANCZOS); off = (z.width - big) // 2
    base.alpha_composite(z.crop((off, off, off + big, off + big)), (0, 0))
    return base


def mascara(side, forma):
    m = Image.new('L', (side * 4, side * 4), 0); d = ImageDraw.Draw(m)
    if forma == 'circle': d.ellipse([0, 0, side * 4 - 1, side * 4 - 1], fill=255)
    else: d.rounded_rectangle([0, 0, side * 4 - 1, side * 4 - 1], radius=side * 4 * 0.22, fill=255)
    return m.resize((side, side), Image.LANCZOS)


for name, d in DENS:
    side = int(48 * d)
    full = completo(side)
    for forma, fn in (('squircle', 'ic_launcher'), ('circle', 'ic_launcher_round')):
        out = full.resize((side, side), Image.LANCZOS); out.putalpha(mascara(side, forma))
        out.save(path(f'mipmap-{name}/{fn}.webp'), 'WEBP', lossless=True)

# ---- ícono de notificación: silueta blanca recortada
sil = Image.new('RGBA', body.size, (255, 255, 255, 0)); sil.putalpha(Image.fromarray(alpha.astype(np.uint8)))
box = sil.getchannel('A').getbbox(); sil = sil.crop(box)
side0 = int(max(sil.size) * 1.12); st = Image.new('RGBA', (side0, side0), (0, 0, 0, 0)); st.paste(sil, ((side0 - sil.width) // 2, (side0 - sil.height) // 2))
for name, d in DENS:
    st.resize((int(24 * d), int(24 * d)), Image.LANCZOS).save(path(f'drawable-{name}/ic_stat_neurovida.png'))

print('Nubi 3D %dx%d; escala del ícono %.3f (Nubi %dx%d en un lienzo de %d); zona segura: radio %.0f px' % (w, h, k, sw, sh, S, SAFE * S / 2))
