# -*- coding: utf-8 -*-
"""Los tres glifos de la barra de pestañas (Tarea 156, opción C de `auditoria-fable/spec-barra-inferior.md`): `kit_i_isla` (Hoy: la isla con su banderita), `kit_i_cohete` (Juegos) y `kit_i_farola` (Avance), churros tinta de 24 dp como los demás íconos del kit.
Toma los PNG que renderiza `isla.py --kit --kit-solo barra` y hace con ellos lo mismo que `kit.py` con los otros íconos (quita la bruma del suelo, guarda el WebP a 3 px por dp) y suma sus tres piezas a `res/raw/kit.json`, sin tocar el resto del kit.
Uso:
  blender -b -P tools/isla/isla.py -- --engine CYCLES --samples 112 --kit --kit-solo barra --out tools/isla/out/kit-barra
  python -I tools/isla/kit_barra.py tools/isla/out/kit-barra"""
import sys, os, json
import numpy as np
from PIL import Image

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SRC = os.path.abspath(sys.argv[1]) if len(sys.argv) > 1 else os.path.join(HERE, 'out', 'kit-barra')
RES = os.path.join(ROOT, 'app', 'src', 'main', 'res'); PX = 3
NOMBRES = ('isla', 'cohete', 'farola')


def carga(n):
    """El PNG sin la bruma del «shadow catcher» (igual que `kit.py`): se resta un piso de 12/255 y el alfa llega a 0 en los últimos 10 px de cada borde."""
    a = np.asarray(Image.open(os.path.join(SRC, n + '.png')).convert('RGBA')).astype(float).copy()
    al = np.clip((a[..., 3] - 12.0) * 255.0 / (255.0 - 12.0), 0, 255); h, w = al.shape; yy, xx = np.mgrid[0:h, 0:w]
    d = np.minimum.reduce([xx, yy, w - 1 - xx, h - 1 - yy]).astype(float); al *= 0.5 - 0.5 * np.cos(np.pi * np.clip(d / 10.0, 0, 1)); a[..., 3] = al
    return Image.fromarray(a.astype(np.uint8), 'RGBA')


def mide(im):
    """Los márgenes y el cuerpo REALES (dp) de un ícono de tinta (lo muy opaco), medidos en la imagen."""
    a = np.asarray(im); ys, xs = np.where(a[..., 3] >= 235); w, h = im.size; x0, y0, x1, y1 = xs.min(), ys.min(), xs.max() + 1, ys.max() + 1
    return ({'izq': round(float(x0) / PX, 2), 'arr': round(float(y0) / PX, 2), 'der': round(float(w - x1) / PX, 2), 'abajo': round(float(h - y1) / PX, 2)},
            {'ancho': round(float(x1 - x0) / PX, 2), 'alto': round(float(y1 - y0) / PX, 2)})


ruta = os.path.join(RES, 'raw', 'kit.json'); kit = json.load(open(ruta, encoding='utf-8'))
for n in NOMBRES:
    im = carga('kit_i_' + n); p = os.path.join(RES, 'drawable-nodpi', 'kit_i_%s.webp' % n)
    im.save(p, quality=92, alpha_quality=100, method=6)
    marg, cuerpo = mide(im)
    kit['piezas'][n] = {'imagen_px': list(im.size), 'margenes_dp': marg, 'cuerpo_dp': cuerpo}
    print('kit_i_%s: %s px, cuerpo %s, márgenes %s, %d KB' % (n, im.size, cuerpo, marg, os.path.getsize(p) // 1024))
json.dump(kit, open(ruta, 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
