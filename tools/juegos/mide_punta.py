# -*- coding: utf-8 -*-
"""Mide la ficha, la casilla y la perla de En la punta de la lengua en plasticina contra el diseño (Tarea 101).
Lee el atlas ya empaquetado (`Assets/Resources/Plasticina/punta`): mide lo MISMO que verá el teléfono. Deja `tools/juegos/out/punta_medidas.md`.

  1. TAMAÑO: cuánto del lado de la imagen mide el cuerpo de cada pieza (el código escala la imagen para que el cuerpo mida lo de siempre).
  2. LA LETRA sobre cada estado de la ficha (la ficha gris se tiñe con `Image.color`: multiplica): el color medio de la cara teñida y qué rinde la tinta (#1A1240) encima (meta ≥ 4,5:1).
  3. LOS CUATRO METALES (oro, plata, cobre, azul: el color ES el dato del lucero): qué tanto se distinguen de a pares con la perla teñida frente a como se distinguían de diseño (ΔE CIELAB).
  4. Contraste de la casilla y de las perlas contra el cielo.
Uso: python -I tools/juegos/mide_punta.py
"""
import itertools, json, os, sys
import numpy as np
from PIL import Image
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from huella import *
sys.stdout.reconfigure(encoding='utf-8')

AQUI = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.normpath(os.path.join(AQUI, '..', '..'))
ATLAS = os.path.join(REPO, 'unity', 'NeuroVidaCore', 'Assets', 'Resources', 'Plasticina', 'punta')
SKY = '#0A0F33'; TINTA = '#1A1240'
ESTADOS = (('normal', '#F4F1FF'), ('fija por ayuda', '#F7DD94'), ('oro (solo)', '#FFC94A'), ('plata (con pista)', '#D6DEF0'), ('cobre (con letras)', '#E6965A'), ('azul', '#7FAAFF'))
manifest = json.load(open(os.path.join(ATLAS, 'manifest.json'), encoding='utf-8'))
atlas = Image.open(os.path.join(ATLAS, 'atlas.png')).convert('RGBA')

def recorte(nombre):
    e = next(s for s in manifest['sprites'] if s['n'] == nombre)
    return np.asarray(atlas.crop((e['x'], atlas.height - e['y'] - e['h'], e['x'] + e['w'], atlas.height - e['y']))).astype(float)

def hexs(rgb): return '#%02X%02X%02X' % tuple(int(round(v)) for v in rgb)
def lab(rgb):
    c = np.asarray(rgb, float) / 255.0; c = np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)
    x = (0.4124 * c[0] + 0.3576 * c[1] + 0.1805 * c[2]) / 0.95047; y = 0.2126 * c[0] + 0.7152 * c[1] + 0.0722 * c[2]; z = (0.0193 * c[0] + 0.1192 * c[1] + 0.9505 * c[2]) / 1.08883
    f = lambda t: np.where(t > 0.008856, np.cbrt(t), 7.787 * t + 16 / 116)
    return np.array([116 * f(y) - 16, 500 * (f(x) - f(y)), 200 * (f(y) - f(z))])
def de(a, b): return float(np.linalg.norm(lab(a) - lab(b)))
def cuerpo(a): m = a[..., 3] >= 250; ys, xs = np.where(m); return xs.max() - xs.min() + 1, ys.max() - ys.min() + 1, a.shape[0]

out = []
def pr(s=''):
    print(s); out.append(s)

pr('# En la punta de la lengua en plasticina — medidas')
pr()
pr('## 1. Tamaño del cuerpo dentro de la imagen (el código escala la imagen con esto)')
pr()
pr('| Pieza | Cuerpo (px) | Lado de la imagen (px) | Cuerpo / lado | Esperado |')
pr('|---|---:|---:|---:|---:|')
for n, esp in (('ficha', 0.8), ('casilla', 0.833), ('perla', 0.94)):
    w, h, L = cuerpo(recorte(n)); pr(f'| {n} | {w}×{h} | {L} | {w / L:.3f} | {esp} |')

pr()
pr('## 2. La letra de tinta sobre cada estado de la ficha (la ficha gris se tiñe)')
pr()
f = recorte('ficha'); N = f.shape[0]; X, Y = malla(N, 2.5)
cara = (f[..., 3] >= 250) & (np.hypot(X, Y) < 0.55)               # el centro de la ficha, donde va la letra
gris = f[..., :3][cara].mean(0)
pr(f'Color medio de la cara sin teñir: {hexs(gris)} (luminancia {lum(gris):.2f}).')
pr()
pr('| Estado | Tinte de diseño | Cara teñida | Letra de tinta sobre ella |')
pr('|---|---|---|---:|')
teñidos = {}
for n, c in ESTADOS:
    t = gris * hexrgb(c) / 255.0; teñidos[n] = t
    pr(f'| {n} | {c} | {hexs(t)} | {contraste(hexrgb(TINTA), t):.1f}:1 |')

pr()
pr('## 3. Los cuatro metales de los luceros (la perla teñida; ΔE CIELAB entre pares)')
pr()
p = recorte('perla'); Np = p.shape[0]; Xp, Yp = malla(Np, 2.0)
caraP = (p[..., 3] >= 250) & (np.hypot(Xp, Yp) < 0.7); grisP = p[..., :3][caraP].mean(0)
metales = (('oro', '#FFC94A'), ('plata', '#D6DEF0'), ('cobre', '#E6965A'), ('azul', '#7FAAFF'))
pr('| Par | De diseño | Con la perla teñida |')
pr('|---|---:|---:|')
peor = 99.0
for (a, ca), (b, cb) in itertools.combinations(metales, 2):
    d0 = de(hexrgb(ca), hexrgb(cb)); d1 = de(grisP * hexrgb(ca) / 255.0, grisP * hexrgb(cb) / 255.0); peor = min(peor, d1)
    pr(f'| {a} y {b} | {d0:.1f} | {d1:.1f} |')
pr()
pr(f'**El par más cercano con plasticina: ΔE {peor:.1f}.**')

pr()
pr('## 4. Contra el cielo ' + SKY)
pr()
c = recorte('casilla'); caraC = (c[..., 3] >= 250)
pr(f'- Casilla (cuenco oscuro con aro celeste): color medio {hexs(c[..., :3][caraC].mean(0))}; el aro (el más claro) {contraste(c[..., :3][caraC][lum(c[..., :3][caraC]) > 0.05].mean(0), hexrgb(SKY)):.1f}:1 contra el cielo.')
pr('- Perlas teñidas: ' + ', '.join(f'{n} {contraste(grisP * hexrgb(col) / 255.0, hexrgb(SKY)):.1f}:1' for n, col in metales) + '.')
open(os.path.join(AQUI, 'out', 'punta_medidas.md'), 'w', encoding='utf-8').write('\n'.join(out) + '\n')
