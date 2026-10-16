# -*- coding: utf-8 -*-
"""Mide los objetos y las piezas de Bodega de carga en plasticina (Tarea 107) contra lo que reemplazan y contra el fondo.
Lee el atlas ya empaquetado (`Assets/Resources/Plasticina/bodega`): mide lo MISMO que verá el teléfono. Deja `tools/juegos/out/bodega_medidas.md`.

  1. LOS 12 OBJETOS (192 px = 2,24 unidades de diseño; el objeto ocupa ±0,8): tamaño, contraste contra el interior oscuro de la escotilla, y qué tanto se parecen de a pares en luminancia (nada solo por el color).
  2. LA ESCOTILLA: tamaños de cada pieza, la hoja (solo la mitad de arriba), la hoja presionada (luminosidad, ΔE, dónde cae la huella) y cuánto resalta la hoja contra el interior y el marco.
  3. EL ROBOT: visor contra el cuerpo y ojos contra el visor; la caja (UNA cinta, sin «+»); la esclusa (el borde dorado) y los tres círculos del carro.
  4. Lo que pesa el atlas.
Uso: python -I tools/juegos/mide_bodega.py
"""
import json, math, os, sys
import numpy as np
from PIL import Image
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from huella import *
sys.stdout.reconfigure(encoding='utf-8')

AQUI = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.normpath(os.path.join(AQUI, '..', '..'))
BASE = os.path.join(REPO, 'unity', 'NeuroVidaCore', 'Assets', 'Resources', 'Plasticina', 'bodega')
NOMBRES = ('llave', 'campana', 'farol', 'manzana', 'hongo', 'taza', 'paraguas', 'libro', 'gema', 'reloj de arena', 'pluma', 'bellota')
man = json.load(open(os.path.join(BASE, 'manifest.json'), encoding='utf-8'))
atlas = Image.open(os.path.join(BASE, 'atlas.png')).convert('RGBA')

def recorte(n):
    e = next(s for s in man['sprites'] if s['n'] == n)
    return np.asarray(atlas.crop((e['x'], atlas.height - e['y'] - e['h'], e['x'] + e['w'], atlas.height - e['y']))).astype(float), e

def hexs(rgb): return '#%02X%02X%02X' % tuple(int(round(v)) for v in rgb)
def lab(rgb):
    c = np.asarray(rgb, float) / 255.0; c = np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)
    x = (0.4124 * c[0] + 0.3576 * c[1] + 0.1805 * c[2]) / 0.95047; y = 0.2126 * c[0] + 0.7152 * c[1] + 0.0722 * c[2]; z = (0.0193 * c[0] + 0.1192 * c[1] + 0.9505 * c[2]) / 1.08883
    f = lambda t: np.where(t > 0.008856, np.cbrt(t), 7.787 * t + 16 / 116)
    return np.array([116 * f(y) - 16, 500 * (f(x) - f(y)), 200 * (f(y) - f(z))])
def de(a, b): return float(np.linalg.norm(lab(a) - lab(b)))

def caja(a, umbral=40):
    m = a[..., 3] >= umbral; ys, xs = np.where(m); return xs.min(), xs.max(), ys.min(), ys.max()

def lum_img(a):
    c = a[..., :3] / 255.0; c = np.where(c <= 0.03928, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)
    return 0.2126 * c[..., 0] + 0.7152 * c[..., 1] + 0.0722 * c[..., 2]

def raiz_lum(a):
    c = a[..., :3] / 255.0; c = np.where(c <= 0.03928, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)
    return np.sqrt(0.2126 * c[..., 0] + 0.7152 * c[..., 1] + 0.0722 * c[..., 2])

def dif_gris(a, b):
    ma = np.abs(a[..., 3] - b[..., 3]) > 128
    mg = (a[..., 3] > 200) & (b[..., 3] > 200) & (np.abs(raiz_lum(a) - raiz_lum(b)) > 0.25)
    return int((ma | mg).sum())

out = []
def pr(s=''):
    print(s); out.append(s)

pr('# Bodega de carga en plasticina — medidas')
pr()
pr('## 1. Los 12 objetos (192 px = 2,24 unidades; el dibujo de siempre ocupa ±0,8: hasta ~1,7 de lado mayor; aquí se mide el cuerpo opaco, sin la sombra)')
pr()
interior0, _ = recorte('interior_0'); H, W = interior0.shape[:2]
fondo = interior0[H // 2, W // 2, :3]
pr(f'El fondo oscuro de una escotilla vacía (centro de `interior_0`): {hexs(fondo)}.')
pr()
pr('| Objeto | Silueta (unidades) | Margen mínimo (px) | Luminosidad media | Contraste contra el fondo oscuro |')
pr('|---|---|---:|---:|---:|')
imgs = []
for k in range(12):
    a, e = recorte(f'obj_{k}'); imgs.append(a); x0, x1, y0, y1 = caja(a, 240); ppu = e['w'] / 2.24
    cuerpo = a[..., 3] > 240; med = a[..., :3][cuerpo].mean(0)
    pr(f'| {NOMBRES[k]} | {(x1 - x0 + 1) / ppu:.2f} × {(y1 - y0 + 1) / ppu:.2f} | {min(x0, e["w"] - 1 - x1, y0, e["h"] - 1 - y1)} | {lum(med):.2f} | {contraste(med, fondo):.1f}:1 |')
pr()
peor = (10 ** 9, '', '')
for i in range(12):
    for j in range(i + 1, 12):
        d = dif_gris(imgs[i], imgs[j])
        if d < peor[0]: peor = (d, NOMBRES[i], NOMBRES[j])
pr(f'La pareja más parecida en luminancia: {peor[1]} y {peor[2]}, con {peor[0]} píxeles distintos de {192 * 192}.')
pr()
pr('## 2. La escotilla (4 px por dp)')
pr()
for n in ('marco', 'aro', 'interior_0', 'interior_1', 'interior_2', 'puerta', 'puerta_p', 'puerta_disco'):
    a, e = recorte(n); pr(f'- {n}: {e["w"]} × {e["h"]} px = {e["w"] / 4:.1f} × {e["h"] / 4:.1f} dp')
hoja, e = recorte('puerta'); hp, _ = recorte('puerta_p'); H, W = hoja.shape[:2]
arriba = (hoja[..., 3][:H // 2 - 2] > 128).sum(); abajo = (hoja[..., 3][H // 2 + 2:] > 128).sum()
pr(f'- La hoja llena la mitad de arriba ({arriba} px) y nada de la de abajo ({abajo} px); mide {(caja(hoja, 128)[1] - caja(hoja, 128)[0] + 1) / 4:.1f} dp de ancho (el radio de la escotilla, 27: la de siempre, 52).')
mh, mp = hoja[..., 3] >= 250, hp[..., 3] >= 250; ch, cp = hoja[..., :3][mh].mean(0), hp[..., :3][mp].mean(0)
pr(f'- La hoja presionada: color medio {hexs(ch)} → {hexs(cp)}, luminosidad {lum(cp) / lum(ch):.2f} de la normal, ΔE {de(ch, cp):.1f}.')
yy, xx = np.mgrid[0:H, 0:W]; X = (xx - W / 2) / 4; Y = (H / 2 - yy) / 4
rat = np.where(mh & mp, lum_img(hp) / np.maximum(lum_img(hoja), 1e-4), np.nan); mediana = np.nanmedian(rat)
def desvio(cx, cy, rad):
    d = (np.hypot(X - cx, Y - cy) < rad) & mh & mp; return float((np.abs(rat[d] - mediana) / mediana).mean())
pr(f'- La huella: la cara se oscurece pareja (×{mediana:.2f} de luminosidad) salvo donde el dedo hundió la plasticina, a (0, +14) dp (entre el tirador, a (0, +6), y el aro de la hoja): desvío medio {desvio(0, 14, 5) * 100:.1f} % ahí, contra {desvio(-10, 10, 5) * 100:.1f} % y {desvio(10, 10, 5) * 100:.1f} % en la cara de los lados.')
marco, _ = recorte('marco'); inter, _ = recorte('interior_2')
pr(f'- La hoja contra el interior oscuro: {contraste(ch, fondo):.1f}:1; contra el marco ({hexs(marco[marco.shape[0] // 2, marco.shape[1] // 2 + 30 * 4, :3])}): {contraste(ch, marco[marco.shape[0] // 2, marco.shape[1] // 2 + 30 * 4, :3]):.1f}:1.')
pr()
pr('## 3. El robot, la caja, la esclusa y el carro')
pr()
rob, e = recorte('robot'); vis, _ = recorte('visor'); ojo, _ = recorte('ojo')
cr = rob[rob.shape[0] // 2, 20, :3]; cv = vis[vis.shape[0] // 2, vis.shape[1] // 2, :3]; co = ojo[ojo.shape[0] // 2, ojo.shape[1] // 2, :3]
pr(f'- Robot: {e["w"] / 4:.1f} dp; el visor ({hexs(cv)}) contra el cuerpo ({hexs(cr)}): {contraste(cv, cr):.1f}:1; el ojo ({hexs(co)}) contra el visor: {contraste(co, cv):.1f}:1.')
cj, e = recorte('caja'); H, W = cj.shape[:2]
fila = lambda y: cj[int(H / 2 - y * 4), :, :3]
pr(f'- La caja: {e["w"] / 4:.1f} × {e["h"] / 4:.1f} dp. UNA cinta (a +2 dp): luminosidad en la cinta {lum(cj[int(H / 2 - 2 * 4), W // 2, :3]):.2f} contra encima ({lum(cj[int(H / 2 - 8.5 * 4), W // 2, :3]):.2f}) y debajo ({lum(cj[int(H / 2 + 6 * 4), W // 2, :3]):.2f}); sin banda vertical: centro de arriba {lum(cj[int(H / 2 - 8.5 * 4), W // 2, :3]):.2f} contra un costado {lum(cj[int(H / 2 - 8.5 * 4), W // 2 + 7 * 4, :3]):.2f}.')
es, e = recorte('esclusa'); H, W = es.shape[:2]; oro = ((es[..., 3] > 200) & (es[..., 0] > 200) & (es[..., 1] > 140) & (es[..., 2] < 150) & (es[..., 0] > es[..., 2] + 80)).sum()
pr(f'- La esclusa: {e["w"] / 4:.1f} dp; borde dorado: {int(oro)} px (la prueba de hoy pide más de 400).')
for k, n in enumerate(('pendiente', 'de memoria (menta + ✓)', 'con error (lila)')):
    c, e = recorte(f'carro_{k}'); H, W = c.shape[:2]; anillo = c[H // 2, int(W / 2 + 18 * 4), :3]
    pr(f'- Carro, {n}: {e["w"] / 4:.0f} dp; el borde {hexs(anillo)} contra el fondo del carro: {contraste(anillo, c[H // 2, W // 2, :3]):.1f}:1.')
pr()
pr('## 4. Lo que pesa')
pr()
p = man['peso']
pr(f'Atlas {man["ancho"]} × {man["alto"]} px, {len(man["sprites"])} imágenes: PNG {p["png_bytes"] / 1024:.0f} KB; en el APK sin compresión de textura ≈ {p["deflate_rgba32"] / 1048576:.2f} MB (tope por juego 1,5 MB); con ASTC 6×6 ≈ {p["astc6x6"] / 1048576:.2f} MB.')
open(os.path.join(AQUI, 'out', 'bodega_medidas.md'), 'w', encoding='utf-8').write('\n'.join(out) + '\n')
