# -*- coding: utf-8 -*-
"""Nubi VIVA (Tarea 148, P1 y P3 de `auditoria-fable/propuestas-diseno-visual.md`): arma las imágenes del reposo de Nubi (ojos abiertos y ojos cerrados) con el MISMO recorte, para que el parpadeo sea un fundido sin saltos.
Solo corre lo de siempre: los renders salen de `isla.py` sin tocarlo (misma cámara y misma luz):

  # la Nubi de la isla (cámara de la isla, 1400 px): la de ojos abiertos ya es `capa-nubi.png` de las capas; solo hace falta la cerrada
  blender -b -P tools/isla/isla.py -- --engine CYCLES --samples 112 --width 1400 --solo-nubi --mirada frente --nubi descansa --out tools/isla/out/n148/isla-descansa
  # la Nubi de frente para las pantallas (3800 px: ~1080 px de Nubi; antes 3000 px → 720 px), abiertos y cerrados
  blender -b -P tools/isla/isla.py -- --engine CYCLES --samples 112 --width 3800 --solo-nubi --mirada frente --nubi hola --out tools/isla/out/n148/hola
  blender -b -P tools/isla/isla.py -- --engine CYCLES --samples 112 --width 3800 --solo-nubi --mirada frente --nubi descansa --out tools/isla/out/n148/descansa

Uso:  python -I tools/isla/nubi_reposo.py <isla_hola.png> <isla_descansa.png> <frente_hola.png> <frente_descansa.png> <carpeta_res>
  p. ej.  python -I tools/isla/nubi_reposo.py tools/isla/out/t134/capa-nubi.png tools/isla/out/n148/isla-descansa/capa-nubi-frente.png tools/isla/out/n148/hola/capa-nubi-frente.png tools/isla/out/n148/descansa/capa-nubi-frente.png app/src/main/res
Deja en `drawable-nodpi/`:
  - `isla_nubi_descansa.webp`: la de la isla con los ojos cerrados, recortada con la caja de `isla_nubi_hola` (la de los ojos abiertos, +8 px por lado como en `componer.py`);
  - `nubi_3d.webp` y `nubi_3d_descansa.webp`: Nubi de frente a 1080 px de ancho (antes 720), las dos con la caja de la de ojos abiertos. El ícono de la app no se toca (sigue saliendo de `icono.py`).
Imprime el tamaño y la relación ancho/alto (`NUBI_ASPECT` en `ui/components/NubiHome.kt` debe coincidir con la de `nubi_3d`).
"""
import os, sys
import numpy as np
from PIL import Image

isla_hola, isla_descansa, frente_hola, frente_descansa, res = sys.argv[1:6]
out = os.path.join(res, 'drawable-nodpi')
os.makedirs(out, exist_ok=True)


def bbox(im, umbral=8):
    return im.getchannel('A').point(lambda v: 255 if v > umbral else 0).getbbox()


def guarda(im, nombre, calidad):
    p = os.path.join(out, nombre)
    im.save(p, 'WEBP', quality=calidad, alpha_quality=100, method=6)
    print('%-26s %4d x %-4d %5d KB   ancho/alto = %.4f' % (nombre, im.width, im.height, os.path.getsize(p) // 1024, im.width / im.height))


# ---- la de la isla: la caja sale de la de ojos abiertos (la misma cámara: el cuerpo no cambia, solo los ojos)
a = Image.open(isla_hola).convert('RGBA'); b = Image.open(isla_descansa).convert('RGBA')
assert a.size == b.size, 'los dos renders de la isla deben tener el mismo tamaño: %s y %s' % (a.size, b.size)
bb = bbox(a); caja = (max(0, bb[0] - 8), max(0, bb[1] - 8), min(a.width, bb[2] + 8), min(a.height, bb[3] + 8))
bd = bbox(b)
print('isla: caja de los ojos abiertos', bb, '· de los cerrados', bd, '(deben coincidir a 1-2 px)')
guarda(b.crop(caja), 'isla_nubi_descansa.webp', 88)

# ---- la de frente: la caja y la escala salen de la de ojos abiertos
c = Image.open(frente_hola).convert('RGBA'); d = Image.open(frente_descansa).convert('RGBA')
assert c.size == d.size, 'los dos renders de frente deben tener el mismo tamaño: %s y %s' % (c.size, d.size)
bc = bbox(c, 0)
print('frente: caja de los ojos abiertos', bc, '· de los cerrados', bbox(d, 0))
W = 1080
H = round((bc[3] - bc[1]) * W / (bc[2] - bc[0]))
for im, nombre in ((c, 'nubi_3d.webp'), (d, 'nubi_3d_descansa.webp')):
    guarda(im.crop(bc).resize((W, H), Image.LANCZOS), nombre, 90)
print('NUBI_ASPECT debe ser %d / %d = %.4f' % (W, H, W / H))
