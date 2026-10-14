# -*- coding: utf-8 -*-
"""Mide el satélite, el planeta, el sobre y la nube de polvo de Satélites en plasticina contra lo que reemplazan y contra el fondo (Tarea 98).
Lee el atlas ya empaquetado (`Assets/Resources/Plasticina/satelites`): mide lo MISMO que verá el teléfono. Deja `tools/juegos/out/satelites_medidas.md`.

  1. HUELLA frente a la de hoy (la SDF de ClayRaster con su borde de tinta; la nube no lleva borde) en el mismo lienzo.
  2. EL SATÉLITE: un solo dibujo (sin antena vertical: el alto es mucho menor que el ancho) y su contraste contra el cielo.
  3. EL PLANETA: oscuro y de bajo contraste (luminancia media frente a la de hoy) y qué tanto resaltan las luces doradas.
  4. LA NUBE: opaca (el 100 % de su interior con alfa ≥ 250) y plana; contra el cielo.
Uso: python -I tools/juegos/mide_satelites.py
"""
import json, math, os, sys
import numpy as np
from PIL import Image
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from huella import *
sys.stdout.reconfigure(encoding='utf-8')

AQUI = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.normpath(os.path.join(AQUI, '..', '..'))
ATLAS = os.path.join(REPO, 'unity', 'NeuroVidaCore', 'Assets', 'Resources', 'Plasticina', 'satelites')
SKY = '#05081D'; LUZ = '#FFE7A8'
manifest = json.load(open(os.path.join(ATLAS, 'manifest.json'), encoding='utf-8'))
atlas = Image.open(os.path.join(ATLAS, 'atlas.png')).convert('RGBA')

def recorte(nombre):
    e = next(s for s in manifest['sprites'] if s['n'] == nombre)
    return np.asarray(atlas.crop((e['x'], atlas.height - e['y'] - e['h'], e['x'] + e['w'], atlas.height - e['y']))).astype(float)

def hexs(rgb): return '#%02X%02X%02X' % tuple(int(round(v)) for v in rgb)
def capsula(X, Y, a, b, r):
    pa = np.array(a); ba = np.array(b) - pa; t = np.clip(((X - pa[0]) * ba[0] + (Y - pa[1]) * ba[1]) / (ba @ ba), 0, 1)
    return np.hypot(X - pa[0] - ba[0] * t, Y - pa[1] - ba[1] * t) - r
def caja_redonda(X, Y, cx, cy, hx, hy, r):
    qx, qy = np.abs(X - cx) - hx + r, np.abs(Y - cy) - hy + r
    return np.hypot(np.maximum(qx, 0), np.maximum(qy, 0)) + np.minimum(np.maximum(qx, qy), 0) - r
def fila(nombre, a, hoy, umbral=250):
    m = a[..., 3] >= umbral
    return m, f'| {nombre} | {hoy.sum()} | {m.sum()} | {m.sum() / hoy.sum():.2f} | {caja(hoy)[0]}×{caja(hoy)[1]} | {caja(m)[0]}×{caja(m)[1]} | {(hoy & m).sum() / (hoy | m).sum():.2f} |'

out = []
def pr(s=''):
    print(s); out.append(s)

pr('# Satélites en plasticina — medidas')
pr()
pr('## 1. Huella (mismo lienzo cuadrado que el sprite de hoy; cuerpo opaco)')
pr()
pr('| Pieza | Área hoy (px²) | Área plasticina | Razón | Caja hoy | Caja plasticina | IoU |')
pr('|---|---:|---:|---:|---|---|---:|')
sat = recorte('satelite'); N = sat.shape[0]; X, Y = malla(N, 2.5)
hoy_sat = np.minimum.reduce([np.hypot(X, Y) - 0.55, caja_redonda(X, Y, 0, 0, 0.7, 0.05, 0.025), caja_redonda(X, Y, -0.76, 0, 0.18, 0.15, 0.05), caja_redonda(X, Y, 0.76, 0, 0.18, 0.15, 0.05)]) <= 0.06
msat, f = fila('satelite', sat, hoy_sat); pr(f)
pla = recorte('planeta'); N = pla.shape[0]; X, Y = malla(N, 2.28)
hoy_pla = (np.hypot(X, Y) - 0.9) <= 0.05
mpla, f = fila('planeta', pla, hoy_pla); pr(f)
sob = recorte('sobre'); N = sob.shape[0]; X, Y = malla(N, 2.6)
hoy_sob = caja_redonda(X, Y, 0, 0, 0.78, 0.55, 0.1) <= 0.07
msob, f = fila('sobre', sob, hoy_sob); pr(f)
nub = recorte('nube'); N = nub.shape[0]; X, Y = malla(N, 2.0)
bx = (-0.34, 0.0, 0.34, -0.16, 0.18); by = (-0.06, 0.16, -0.04, -0.22, -0.2); br = (0.44, 0.52, 0.44, 0.36, 0.38)
sdf_nub = np.minimum.reduce([np.hypot(X - bx[i], Y - by[i]) - br[i] for i in range(5)])
mnub, f = fila('nube', nub, sdf_nub <= 0, umbral=200); pr(f)

pr()
pr('## 2. El satélite (UNO solo, todos iguales)')
pr()
m = msat; ys, xs = np.where(m)
pr(f'Alto/ancho de la silueta: {(ys.max() - ys.min() + 1) / (xs.max() - xs.min() + 1):.2f} (disco con un brazo horizontal y dos paneles: sin nada vertical sobre el brazo, que sería una cruz).')
disco = sat[..., :3][(sat[..., 3] >= 250) & (lum(sat[..., :3]) > 0.55)].mean(0)
panel = sat[..., :3][(sat[..., 3] >= 250) & (lum(sat[..., :3]) > 0.25) & (lum(sat[..., :3]) <= 0.55)].mean(0)
pr(f'Disco {hexs(disco)} contra el cielo {SKY}: {contraste(disco, hexrgb(SKY)):.1f}:1; paneles {hexs(panel)}: {contraste(panel, hexrgb(SKY)):.1f}:1.')

pr()
pr('## 3. El planeta (oscuro y de bajo contraste)')
pr()
Xp, Yp = malla(256, 2.28)
d = np.clip(np.hypot(Xp + 0.28, Yp - 0.32) / 1.5, 0, 1); t = d * d * (3 - 2 * d)
hoy_col = (1 - t)[..., None] * np.array([0x3B, 0x3F, 0x86]) + t[..., None] * np.array([0x15, 0x18, 0x43])
medio_hoy = hoy_col[np.hypot(Xp, Yp) < 0.85].mean(0)
medio = pla[..., :3][mpla & (np.hypot(*malla(pla.shape[0], 2.28)) < 0.85)].mean(0)
pr(f'Color medio del planeta: hoy {hexs(medio_hoy)} (luminancia {lum(medio_hoy):.3f}), plasticina {hexs(medio)} ({lum(medio):.3f}).')
pr(f'Contra el cielo {SKY}: hoy {contraste(medio_hoy, hexrgb(SKY)):.1f}:1, plasticina {contraste(medio, hexrgb(SKY)):.1f}:1.')
pr(f'Las luces doradas ({LUZ}) sobre el planeta: hoy {contraste(hexrgb(LUZ), medio_hoy):.1f}:1, plasticina {contraste(hexrgb(LUZ), medio):.1f}:1.')

pr()
pr('## 4. La nube de polvo (plana y opaca)')
pr()
interior = sdf_nub < -0.05
pr(f'Interior de la nube (a más de 0,05 del borde): {(nub[..., 3][interior] >= 250).mean() * 100:.1f} % con alfa ≥ 250 (la de hoy, 97 %: opaca). Alfa mínimo del interior: {int(nub[..., 3][interior].min())}.')
col = nub[..., :3][(nub[..., 3] >= 250) & interior].mean(0)
pr(f'Color medio {hexs(col)} contra el cielo {SKY}: {contraste(col, hexrgb(SKY)):.1f}:1; contra el planeta: {contraste(col, medio):.1f}:1.')
open(os.path.join(AQUI, 'out', 'satelites_medidas.md'), 'w', encoding='utf-8').write('\n'.join(out) + '\n')
