# -*- coding: utf-8 -*-
"""Mide los 9 luceros de Rastro de luz en plasticina contra los de hoy y contra el fondo (Tarea 99).
Lee el atlas ya empaquetado (`Assets/Resources/Plasticina/rastro`): mide lo MISMO que verá el teléfono. Deja `tools/juegos/out/rastro_medidas.md`.

  1. HUELLA: el área y la caja de cada lucero frente a la de hoy (esfera de radio 0,94 del lienzo de ±1).
  2. EL COLOR ES LA IDENTIDAD (con la posición y la nota): el color medio de la cara de cada lucero frente a su color de diseño (distancia en CIELAB), y qué tanto se distinguen los pares cercanos frente a como se distinguían de diseño.
  3. CONTRASTE de cada lucero contra el cielo del juego (WCAG).
  4. EL ESTADO PRESIONADO (Tarea 99b, 14-oct): ancho y alto, color (la regla de ΔE sigue valiendo en los dos estados), pares cercanos y cuánto se ve la huella.
Uso: python -I tools/juegos/mide_rastro.py
"""
import json, math, os, sys
import numpy as np
from PIL import Image
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from huella import *
sys.stdout.reconfigure(encoding='utf-8')

AQUI = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.normpath(os.path.join(AQUI, '..', '..'))
ATLAS = os.path.join(REPO, 'unity', 'NeuroVidaCore', 'Assets', 'Resources', 'Plasticina', 'rastro')
COLORES = (0x7FD8FF, 0xB8A4FF, 0xFFC94A, 0x8EE07A, 0xFF9F7A, 0x9FF5D6, 0xF7A8E0, 0xFFE27A, 0xA9C7FF)
SKY = '#0A0F33'
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
def desenfoca(L, r):
    """Un desenfoque gaussiano separable de radio [r] px (solo numpy)."""
    k = int(r * 3) * 2 + 1; xs = np.arange(k) - k // 2; g = np.exp(-xs ** 2 / (2.0 * r * r)); g /= g.sum()
    conv = lambda v: np.convolve(np.pad(v, k // 2, mode='edge'), g, mode='valid')
    return np.apply_along_axis(conv, 1, np.apply_along_axis(conv, 0, L))

out = []
def pr(s=''):
    print(s); out.append(s)

pr('# Rastro de luz en plasticina — medidas')
pr()
pr('## 1. Huella (lienzo de ±1; la esfera ocupa el 94 %)')
pr()
pr('| Lucero | Color de diseño | Área hoy (px²) | Área plasticina | Razón | Caja plasticina |')
pr('|---|---|---:|---:|---:|---|')
medios = []; razones = []
for i, c in enumerate(COLORES):
    a = recorte(f'lucero_{i}'); N = a.shape[0]; X, Y = malla(N, 2.0)
    hoy = np.hypot(X, Y) <= 0.94; m = a[..., 3] >= 250
    razones.append(m.sum() / hoy.sum())
    cara = m & (np.hypot(X, Y) < 0.7)
    medios.append(a[..., :3][cara].mean(0))
    pr(f'| {i} | #{c:06X} | {hoy.sum()} | {m.sum()} | {m.sum() / hoy.sum():.2f} | {caja(m)[0]}×{caja(m)[1]} |')
pr()
pr(f'**Razón de áreas: {min(razones):.2f} a {max(razones):.2f}.**')

pr()
pr('## 2. El color es la identidad')
pr()
pr('| Lucero | Color de diseño | Color medio de la cara | Distancia (ΔE CIELAB) |')
pr('|---|---|---|---:|')
des = [np.array([(c >> 16) & 255, (c >> 8) & 255, c & 255], float) for c in COLORES]
for i in range(9): pr(f'| {i} | #{COLORES[i]:06X} | {hexs(medios[i])} | {de(medios[i], des[i]):.1f} |')
pr()
pr('Los pares cercanos (cuánto se distinguen de diseño y en plasticina, ΔE; menor = más parecidos):')
pr()
pr('| Par | De diseño | Plasticina |')
pr('|---|---:|---:|')
pares = sorted(((de(des[i], des[j]), i, j) for i in range(9) for j in range(i + 1, 9)))[:6]
peor = 99.0
for d0, i, j in pares:
    d1 = de(medios[i], medios[j]); peor = min(peor, d1)
    pr(f'| {i} (#{COLORES[i]:06X}) y {j} (#{COLORES[j]:06X}) | {d0:.1f} | {d1:.1f} |')
pr()
pr(f'**El par más cercano en plasticina: ΔE {peor:.1f}.**')

pr()
pr('## 3. Contraste contra el cielo del juego (WCAG)')
pr()
pr('Cara de cada lucero contra el cielo ' + SKY + ': ' + ', '.join(f'{i}: {contraste(medios[i], hexrgb(SKY)):.1f}:1' for i in range(9)) + f'. El más bajo: {min(contraste(m, hexrgb(SKY)) for m in medios):.1f}:1.')
pr()
pr('La ✗ coral (#FF6B4A) que se pone sobre un lucero errado, contra la cara de cada uno: ' + ', '.join(f'{i}: {contraste(hexrgb("#FF6B4A"), medios[i]):.1f}:1' for i in range(9)) + ' (igual que hoy: el color de los luceros no cambia; la ✗ pierde contraste sobre los claros y se lee por su forma).')
pr()
pr('## 4. El estado presionado (regla del 14-oct: todo objeto de plasticina que se toca tiene su estado presionado, aplastado y con huella)')
pr()
pr('El lucero apoyado en el dedo: la esfera aplastada (~8 % más baja y ~4 % más ancha), un punto más oscura y con una huella de dedo hundida en la cara. Mismo lienzo y misma escala que el normal. Se muestra SOLO al tocar (el que el juego enciende en la secuencia sigue siendo luz y halo).')
pr()
pr('| Lucero | Ancho (px) normal → presionado | Alto (px) normal → presionado | Cara presionada | ΔE contra su diseño | ΔE contra el normal | Detalle fino en la cara (la huella), normal → presionado |')
pr('|---|---|---|---|---:|---:|---|')
medios_p = []
for i, c in enumerate(COLORES):
    n_ = recorte(f'lucero_{i}'); a_ = recorte(f'lucero_p_{i}'); N = a_.shape[0]; X, Y = malla(N, 2.0)
    mn = n_[..., 3] >= 250; mp = a_[..., 3] >= 250
    cara = mp & (np.hypot(X, Y) < 0.7); medios_p.append(a_[..., :3][cara].mean(0))
    centro = mp & (np.hypot(X, Y - 0.05) < 0.5)
    centro_n = mn & (np.hypot(X, Y - 0.05) < 0.5)
    dsv = lambda im, m: float(np.std((lum(im[..., :3]) * 255 - desenfoca(lum(im[..., :3]) * 255, 3))[m]))             # el detalle fino (lo que la huella suma), sin el degradé de la luz
    pr(f'| {i} | {caja(mn)[0]} → {caja(mp)[0]} | {caja(mn)[1]} → {caja(mp)[1]} | {hexs(medios_p[-1])} | {de(medios_p[-1], des[i]):.1f} | {de(medios_p[-1], medios[i]):.1f} | {dsv(n_, centro_n):.1f} → {dsv(a_, centro):.1f} |')
pr()
pr('Los pares cercanos con el lucero presionado (cuánto se distinguen de diseño y presionados, ΔE):')
pr()
pr('| Par | De diseño | Presionados |')
pr('|---|---:|---:|')
peor_p = 99.0
for d0, i, j in pares:
    d1 = de(medios_p[i], medios_p[j]); peor_p = min(peor_p, d1)
    pr(f'| {i} y {j} | {d0:.1f} | {d1:.1f} |')
pr()
pr(f'**El par más cercano presionado: ΔE {peor_p:.1f}.** Contraste de los presionados contra el cielo: el más bajo {min(contraste(m, hexrgb(SKY)) for m in medios_p):.1f}:1.')
open(os.path.join(AQUI, 'out', 'rastro_medidas.md'), 'w', encoding='utf-8').write('\n'.join(out) + '\n')
