# -*- coding: utf-8 -*-
"""La Nubi 3D para la app y el ícono (Tarea 92): toma el render `isla.py --solo-nubi --mirada frente` (PNG con fondo transparente) y deja lo que entra a `app/src/main/res/`:
   - `drawable-nodpi/nubi_3d.webp`: Nubi de frente recortada a su caja (720 px de ancho: a 3 px por dp sirve hasta 240 dp), para todas las pantallas (reemplaza a los `nubi_hola/mira/celebra/cientifica.webp` planos);
   - ícono adaptativo (`mipmap-*/ic_launcher_foreground.png` y `ic_launcher_monochrome.png`, lienzo de 108 dp × densidad): Nubi encuadrada en la zona segura del 66 % (círculo de 66 dp) con una sombra suave debajo; el monocromo es su silueta sólida con dos ojos redondos y la sonrisa (un arco de grosor parejo, 6 % del ancho) calados (Tarea 93);
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

# ---- el monocromo (Tarea 93: simplificado; el sistema lo tiñe): la silueta SÓLIDA de Nubi con tres calados limpios: dos ojos redondos (sin el aro crema ni el brillo) y la sonrisa como un arco de grosor parejo (6 % del ancho de la silueta).
# Antes se calaba por luminosidad (ojos con aro y brillo, mejillas, sombra de abajo) y se leía como ruido. Ahora la posición y el tamaño de los rasgos se MIDEN en el render (lo oscuro de la cara: el iris y la sonrisa) y los calados se dibujan geométricos.
SONRISA_GROSOR = 0.06                                                          # grosor del arco, como fracción del ancho de la silueta
OJO_K = 0.75                                                                    # el ojo calado mide esta fracción del iris medido (el iris entero se comía la silueta y tocaba la sonrisa)
SONRISA_CURVA = 0.70                                                            # cuánto se conserva de la curvatura medida (más plana se lee mejor a tamaño chico)
SONRISA_LARGO = 0.14                                                            # largo de la cuerda de la sonrisa, como fracción del ancho de la silueta
rgbf = np.asarray(nubi.convert('RGB')).astype(float) / 255.0
lumf = 0.2126 * rgbf[..., 0] + 0.7152 * rgbf[..., 1] + 0.0722 * rgbf[..., 2]
oscuro = (lumf < 0.25) & (a0 > 200)                                            # el iris (lum ~0,21) y la sonrisa; con 0,30 se colaba la sombra del aro
Hn, Wn = oscuro.shape
yy, xx = np.mgrid[0:Hn, 0:Wn]
cara = (yy > 0.50 * Hn) & (yy < 0.88 * Hn)
izq = oscuro & cara & (xx < 0.43 * Wn); der = oscuro & cara & (xx > 0.60 * Wn); sonr = oscuro & cara & (xx >= 0.43 * Wn) & (xx <= 0.57 * Wn) & (yy > 0.64 * Hn)
ext = []
for m in (izq, der):
    ys_, xs_ = np.nonzero(m); ext.append((xs_.min(), xs_.max(), ys_.min()))
r_ = np.mean([(x1 - x0) / 2 for x0, x1, _ in ext])                              # un solo radio para los dos ojos: sale del ancho del iris (abajo el iris se aclara y el umbral lo corta)
cy_ojos = np.mean([y0 for _, _, y0 in ext]) + r_                                 # y los dos a la misma altura (el borde de arriba del iris + el radio): más limpio que seguir la perspectiva del render
ojos = [((x0 + x1) / 2, cy_ojos, r_) for x0, x1, _ in ext]                       # centro y radio del iris, en px del recorte
ys_, xs_ = np.nonzero(sonr)
cols = np.unique(xs_); cy = np.array([ys_[xs_ == c].mean() for c in cols])
cf = np.polyfit(cols, cy, 2)                                                  # la línea central de la sonrisa: una parábola
sx0, sx1 = cols.min(), cols.max()
sxm = (sx0 + sx1) / 2; sym = np.polyval(cf, sxm)                                # el centro de la sonrisa medida
forma = (np.polyval(cf, sx0) + np.polyval(cf, sx1)) / 2 - sym                    # cuánto suben sus puntas respecto del medio (negativo: sonríe); se conserva la proporción con el largo
forma /= (sx1 - sx0) / 2


def punto(pts, t):
    """El punto de la polilínea `pts` a la fracción `t` de su recorrido (interpolación lineal entre sus vértices: son 60, la curva sale suave)."""
    u = t * (len(pts) - 1); i = min(int(u), len(pts) - 2); f = u - i
    return pts[i][0] * (1 - f) + pts[i + 1][0] * f, pts[i][1] * (1 - f) + pts[i + 1][1] * f


def calados():
    """La máscara (0..255) de lo que se calla de la silueta, al tamaño de `body`: dibujada en 4× y reducida (bordes limpios)."""
    f = 4; kk = sw / W0 * f
    m = Image.new('L', (sw * f, sh * f), 0); dr = ImageDraw.Draw(m)
    for cx, cy_, r in ojos:
        r = r * OJO_K; dr.ellipse([(cx - r) * kk, (cy_ - r) * kk, (cx + r) * kk, (cy_ + r) * kk], fill=255)
    gr = SONRISA_GROSOR * sw * f                                                # grosor del arco: 6 % del ancho de la silueta
    mitad = SONRISA_LARGO * W0 / 2                                              # media cuerda de la sonrisa dibujada, en px del recorte
    pts = [((sxm + t) * kk, (sym + SONRISA_CURVA * forma * mitad * (t / mitad) ** 2) * kk) for t in np.linspace(-mitad, mitad, 60)]
    for t in np.linspace(0, 1, 400):                                            # el trazo de grosor parejo y puntas redondas: un círculo por punto del arco (ImageDraw.line con ancho grande deja huecos entre tramos)
        x, y = punto(pts, t); dr.ellipse([x - gr / 2, y - gr / 2, x + gr / 2, y + gr / 2], fill=255)
    return m.resize((sw, sh), Image.LANCZOS)


hueco = np.asarray(calados()).astype(float) / 255.0
alpha = np.asarray(body.getchannel('A')).astype(float)
alpha[alpha < 24] = 0                                                           # sin la bruma del borde
alpha = alpha * (1.0 - hueco)
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

print('rasgos medidos (px del recorte de %dx%d): ojos %s; sonrisa de x %d a %d, y medio %.0f' % (W0, H0, [(round(x), round(y), round(r)) for x, y, r in ojos], sx0, sx1, sym))
print('Nubi 3D %dx%d; escala del ícono %.3f (Nubi %dx%d en un lienzo de %d); zona segura: radio %.0f px' % (w, h, k, sw, sh, S, SAFE * S / 2))
