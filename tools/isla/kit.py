# -*- coding: utf-8 -*-
"""El kit de plasticina (Tarea 89): toma los PNG que renderiza `isla.py --kit` y deja lo que entra a la app:
   - `kit_losa_<sol|crema|lima>[_p].webp`: losas para NINE-SLICE (esquinas fijas, bordes estirados en un eje, centro estirado), normal y presionada, todas con el mismo lienzo y el cuerpo en el mismo sitio;
   - `kit_cerrar[_p].webp`: el botón redondo con la X de dos churros tinta; `kit_jugar.webp`: el triángulo de «Jugar» como un churrito tinta;
   - `kit.json`: la densidad del render, el lienzo, los márgenes (donde cae la sombra), el cuerpo y los insets del nine-slice de cada pieza.
   Mide que el CENTRO que se estira sea parejo (si no, el nine-slice deformaría las huellas): imprime la desviación a lo ancho y a lo alto.
Uso: python -I kit.py <carpeta_render> [<carpeta_salida>]     (la salida por defecto es <carpeta_render>/app; `kit.json` va a `app/src/main/res/raw/` y los WebP a `res/drawable-nodpi/`)"""
import sys, os, json
import numpy as np
sys.stdout.reconfigure(encoding='utf-8')
from PIL import Image

src = sys.argv[1]
dst = sys.argv[2] if len(sys.argv) > 2 else os.path.join(src, 'app')
os.makedirs(dst, exist_ok=True)
PX = 3                                   # px por dp del render (KIT_PX en isla.py)
R_DP = 22                                # radio de las esquinas de la losa
LOSA = {'margen': (8, 6, 12, 14), 'cuerpo': (68, 52), 'centro': (24, 8)}             # dp: margen (izq, arr, der, abajo), cuerpo (ancho, alto), centro que se estira (ancho, alto): igual que KIT_* de isla.py
CERRAR = {'margen': (6, 4, 10, 12), 'cuerpo': (48, 48)}
JUGAR = {'margen': (4, 3, 6, 7), 'cuerpo': (20, 22)}


def load(n):
    """El PNG con la bruma del «shadow catcher» recortada: el suelo invisible deja un velo de alfa 10-20 hasta los bordes y sobre crema se vería como un rectángulo gris. Se resta un piso de 12/255 y se hace llegar a 0 en los últimos 10 px de cada borde."""
    im = Image.open(os.path.join(src, n + '.png')).convert('RGBA'); a = np.asarray(im).astype(float).copy()
    al = np.clip((a[..., 3] - 12.0) * 255.0 / (255.0 - 12.0), 0, 255)
    h, w = al.shape; yy, xx = np.mgrid[0:h, 0:w]; d = np.minimum.reduce([xx, yy, w - 1 - xx, h - 1 - yy]).astype(float)
    al *= 0.5 - 0.5 * np.cos(np.pi * np.clip(d / 10.0, 0, 1))        # rampa suave hasta 0 en el borde
    a[..., 3] = al
    return Image.fromarray(a.astype(np.uint8), 'RGBA')


def body_bbox(im):
    a = np.asarray(im)
    m = (a[..., 3] >= 200) & (a[..., :3].max(axis=2) > 70)       # el cuerpo: opaco y con color (la sombra de contacto es negra y translúcida)
    ys, xs = np.where(m)
    return xs.min(), ys.min(), xs.max() + 1, ys.max() + 1


def normalize(ims, spec):
    """Corre todas las imágenes de una pieza para que el cuerpo de la PRIMERA caiga exacto en su margen izquierdo y superior; las demás se corren lo mismo (la huella en el suelo no cambia entre estados)."""
    ml, mt, mr, mb = spec['margen']; bw, bh = spec['cuerpo']
    W = (ml + bw + mr) * PX; H = (mt + bh + mb) * PX
    x0, y0, x1, y1 = body_bbox(ims[0])
    dx, dy = ml * PX - x0, mt * PX - y0
    out = []
    for im in ims:
        canvas = Image.new('RGBA', (W, H), (0, 0, 0, 0)); canvas.paste(im, (dx, dy))        # paste sin máscara copia los cuatro canales tal cual (también con corrimiento negativo)
        out.append(canvas)
    return out, (x1 - x0, y1 - y0)


def measured(im, w, h):
    """Los márgenes REALES (dp) del cuerpo dentro del lienzo y su tamaño, medidos en la imagen: los de diseño solo sirven para alinear; el JSON dice lo que se midió."""
    x0, y0, x1, y1 = body_bbox(im)
    return {'izq': round(x0 / PX, 2), 'arr': round(y0 / PX, 2), 'der': round((w - x1) / PX, 2), 'abajo': round((h - y1) / PX, 2)}, {'ancho': round((x1 - x0) / PX, 2), 'alto': round((y1 - y0) / PX, 2)}


def save(im, name):
    p = os.path.join(dst, name + '.webp'); im.save(p, quality=92, alpha_quality=100, method=6); return os.path.getsize(p)


report = {}; total = 0
kit = {'px_por_dp': PX, 'nota': 'Kit de plasticina (Tarea 89), renderizado en Blender con `tools/isla/isla.py --kit`; `tools/isla/kit.py` lo normaliza. Los WebP viven en drawable-nodpi (a 3 px por dp, sin que Android los reescale).'}

# ---- las losas (nine-slice)
cw, ch = LOSA['centro']
first = True
for color in ('sol', 'crema', 'lima'):
    ims, size = normalize([load('kit_losa_' + color), load('kit_losa_' + color + '_p')], LOSA)
    for im, suf in zip(ims, ('', '_p')):
        total += save(im, 'kit_losa_' + color + suf)
    a = np.asarray(ims[0]).astype(float)
    W, H = ims[0].size
    marg, cuerpo = measured(ims[0], W, H)
    insets = {'izq': round((marg['izq'] + R_DP) * PX), 'arr': round((marg['arr'] + R_DP) * PX), 'der': round((R_DP + marg['der']) * PX), 'abajo': round((R_DP + marg['abajo']) * PX)}
    cx0, cx1, cy0, cy1 = insets['izq'], W - insets['der'], insets['arr'], H - insets['abajo']
    band_x = a[cy0:cy1, cx0:cx1, :3]                    # el centro: lo que se estira
    row_std = band_x.std(axis=1).mean()                 # a lo ancho (lo que se estira en horizontal)
    col_std = band_x.std(axis=0).mean()                 # a lo alto
    bajas = [(body_bbox(ims[1])[1] - body_bbox(ims[0])[1]) / PX]
    report[color] = (size, row_std, col_std)
    baja_losa = bajas[0]
kit['losa'] = {
    'imagen_px': [W, H], 'margenes_dp': marg, 'cuerpo_dp': cuerpo, 'radio_dp': R_DP,
    'insets_px': insets, 'centro_px': [cx1 - cx0, cy1 - cy0],
    'presionada': {'texto_baja_dp': 2.0, 'borde_baja_dp': round(baja_losa, 2), 'nota': 'la losa presionada es más baja (la mitad de alto), más oscura y de sombra más chica; el texto y el contenido bajan `texto_baja_dp` al apoyar el dedo'},
    'estados': ['normal', '_p'], 'colores': {'sol': '#FFCB45', 'crema': '#F5EBDB', 'lima': '#9BE564'}
}

# ---- el botón redondo con X
ims, size = normalize([load('kit_cerrar'), load('kit_cerrar_p')], CERRAR)
for im, suf in zip(ims, ('', '_p')): total += save(im, 'kit_cerrar' + suf)
marg2, cuerpo2 = measured(ims[0], *ims[0].size)
kit['cerrar'] = {'imagen_px': list(ims[0].size), 'margenes_dp': marg2, 'cuerpo_dp': cuerpo2, 'presionada': {'texto_baja_dp': 2.0, 'borde_baja_dp': round((body_bbox(ims[1])[1] - body_bbox(ims[0])[1]) / PX, 2)}}

# ---- el triángulo de Jugar
ims, size = normalize([load('kit_jugar')], JUGAR)
total += save(ims[0], 'kit_jugar')
marg3, cuerpo3 = measured(ims[0], *ims[0].size)
kit['jugar'] = {'imagen_px': list(ims[0].size), 'margenes_dp': marg3, 'cuerpo_dp': cuerpo3}

json.dump(kit, open(os.path.join(dst, 'kit.json'), 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
print('losa: imagen %dx%d px; centro que se estira %dx%d px; insets %s' % (W, H, cx1 - cx0, cy1 - cy0, insets))
for k, (sz, rs, cs) in report.items():
    print('  %-6s cuerpo %dx%d px (%.1fx%.1f dp); desviación del centro: a lo ancho %.2f, a lo alto %.2f (de 255)' % (k, sz[0], sz[1], sz[0] / PX, sz[1] / PX, rs, cs))
print('peso total de los WebP: %.1f KB (%d archivos)' % (total / 1024, len(os.listdir(dst)) - 1))
