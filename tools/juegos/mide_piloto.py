# -*- coding: utf-8 -*-
"""Mide las 15 señales y la nave de Piloto Estelar en plasticina contra lo que reemplazan y contra el fondo (Tarea 95).
Lee el atlas ya empaquetado (`Assets/Resources/Plasticina/piloto`): mide lo MISMO que verá el teléfono. Deja `tools/juegos/out/piloto_medidas.md`.

  1. HUELLA: el área y la caja de la silueta del cuerpo (alfa opaco, sin la sombra) frente a la de hoy (la SDF de ClayRaster con su borde tinta).
  2. CONTRASTE de cada forma contra el cielo (WCAG): el color medio del cuerpo contra tres fondos del hiperespacio (el cielo de arriba, una nebulosa azul y una coral). Meta ≥ 3:1.
  3. DETALLES: el contraste de la tinta contra el cuerpo (lo que separa a las «parecidas»: misma forma y color, otro detalle), cuánto se distinguen de a pares y cuánto miden a los tamaños reales (juego, aviso y tarjeta de misión).
  4. NAVE: huella frente a la de hoy y contraste.
Uso: python -I tools/juegos/mide_piloto.py
"""
import json, os, sys
import numpy as np
from PIL import Image
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from huella import *
sys.stdout.reconfigure(encoding='utf-8')

AQUI = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.normpath(os.path.join(AQUI, '..', '..'))
ATLAS = os.path.join(REPO, 'unity', 'NeuroVidaCore', 'Assets', 'Resources', 'Plasticina', 'piloto')
FORMAS = ('hexagono', 'gota', 'circulo', 'cuadrado', 'triangulo')
DETALLES = ('punto', 'anillo', 'franja')
COLORES = {'hexagono': '#A6E36B', 'gota': '#7FD8FF', 'circulo': '#FF8A6B', 'cuadrado': '#FFC94A', 'triangulo': '#B79BFF'}
FONDOS = (('cielo de arriba', '#0A0F33'), ('nebulosa azul', '#1B2B66'), ('nebulosa coral', '#5A3340'))
N = 192
CENTROS = {'gota': -0.30, 'triangulo': -0.18}

manifest = json.load(open(os.path.join(ATLAS, 'manifest.json'), encoding='utf-8'))
atlas = Image.open(os.path.join(ATLAS, 'atlas.png')).convert('RGBA')

def recorte(nombre):
    e = next(s for s in manifest['sprites'] if s['n'] == nombre)
    return np.asarray(atlas.crop((e['x'], atlas.height - e['y'] - e['h'], e['x'] + e['w'], atlas.height - e['y']))).astype(float)

out = []
def pr(s=''):
    print(s); out.append(s)

X, Y = malla(N)
pr('# Piloto Estelar en plasticina — medidas')
pr()
pr('## 1. Huella de las señales (mismo lienzo de 2,36 unidades; cuerpo opaco)')
pr()
pr('| Forma + detalle | Área hoy (px²) | Área plasticina | Razón | Caja hoy | Caja plasticina | IoU |')
pr('|---|---:|---:|---:|---|---|---:|')
razones = []; ious = []; cuerpos = {}
for f in FORMAS:
    hoy = silueta_hoy(f, X, Y)
    for d in DETALLES:
        a = recorte(f'senal_{f}_{d}'); m = a[..., 3] >= 250; cuerpos[(f, d)] = a
        iou = (hoy & m).sum() / (hoy | m).sum(); razones.append(m.sum() / hoy.sum()); ious.append(iou)
        pr(f'| {f} {d} | {hoy.sum()} | {m.sum()} | {m.sum() / hoy.sum():.2f} | {caja(hoy)[0]}×{caja(hoy)[1]} | {caja(m)[0]}×{caja(m)[1]} | {iou:.2f} |')
pr()
pr(f'**Razón de áreas: {min(razones):.2f} a {max(razones):.2f}; IoU: {min(ious):.2f} a {max(ious):.2f}.**')

pr()
pr('## 2. Contraste de la forma contra el cielo (WCAG; meta ≥ 3:1)')
pr()
pr('| Forma | Color de diseño | Color medio del cuerpo | ' + ' | '.join(n for n, _ in FONDOS) + ' |')
pr('|---|---|---|' + '---:|' * len(FONDOS))
medio = {}
peor = 99.0
for f in FORMAS:
    a = cuerpos[(f, 'punto')]; al = a[..., 3]
    claro = (al >= 250) & (lum(a[..., :3]) > 0.06)                                         # el cuerpo (sin la tinta del detalle ni el canto en sombra)
    medio[f] = a[..., :3][claro].mean(0)
    cs = [contraste(medio[f], hexrgb(c)) for _, c in FONDOS]; peor = min(peor, min(cs))
    pr(f'| {f} | {COLORES[f]} | #{int(medio[f][0]):02X}{int(medio[f][1]):02X}{int(medio[f][2]):02X} | ' + ' | '.join(f'{c:.1f}:1' for c in cs) + ' |')
pr()
pr(f'**El más bajo: {peor:.1f}:1.**')

pr()
pr('## 3. Los detalles (lo que separa a las «parecidas»: misma forma y mismo color, otro detalle)')
pr()
pr('| Forma | Tinta contra el cuerpo (punto / anillo / franja) | Se distinguen de a pares (% de la forma que cambia: punto-anillo / punto-franja / anillo-franja) |')
pr('|---|---|---|')
minimo = 99.0; peor_par = 1.0
for f in FORMAS:
    cont = []
    for d in DETALLES:
        a = cuerpos[(f, d)]; tinta = (a[..., 3] >= 250) & (lum(a[..., :3]) < 0.03)
        cont.append(contraste(medio[f], a[..., :3][tinta].mean(0))); minimo = min(minimo, cont[-1])
    pares = []
    for (d1, d2) in (('punto', 'anillo'), ('punto', 'franja'), ('anillo', 'franja')):
        a, b = cuerpos[(f, d1)], cuerpos[(f, d2)]
        ambos = (a[..., 3] >= 250) & (b[..., 3] >= 250)
        cambia = (np.abs(lum(a[..., :3]) - lum(b[..., :3])) > 0.2) & ambos
        pares.append(cambia.sum() / ambos.sum()); peor_par = min(peor_par, pares[-1])
    pr(f'| {f} | ' + ' / '.join(f'{c:.1f}:1' for c in cont) + ' | ' + ' / '.join(f'{p * 100:.0f} %' for p in pares) + ' |')
pr()
pr(f'**Tinta contra el cuerpo: mínimo {minimo:.1f}:1. Par de detalles más parecido: cambia el {peor_par * 100:.0f} % de la forma.**')
pr()
u = 192 / 2.36
pr('Lo que miden los detalles a los tamaños reales (1 unidad del lienzo de 1080 = 1 px en el teléfono):')
pr()
pr('| Dónde | Forma (u) | Grosor del anillo | Diámetro del punto | Alto de la franja |')
pr('|---|---:|---:|---:|---:|')
for donde, lado in (('en el juego (34 dp de forma)', 102), ('en la tarjeta de misión (28 dp de lado)', 66), ('en el aviso de misión nueva (52 dp)', 156)):
    k = lado / 1.84
    pr(f'| {donde} | {lado} | {0.15 * k:.1f} px | {0.54 * k:.1f} px | {0.30 * k:.1f} px |')

pr()
pr('## 4. La nave')
pr()
Xn, Yn = malla(N, 2.2)
hull = np.maximum(np.maximum(np.hypot(Xn + 0.62, Yn + 0.06) - 0.92, np.hypot(Xn - 0.62, Yn + 0.06) - 0.92), -0.58 - Yn)
aleta = sdf_poligono([(0.20, -0.10), (0.52, -0.46), (0.52, -0.68), (0.20, -0.52)], np.abs(Xn), Yn)
llama = sdf_capsula(Xn, Yn, (0.0, -0.56, 0.17), (0.0, -0.90, 0.04))
hoy = np.minimum(np.minimum(hull, aleta), llama) <= 0.075
a = recorte('nave'); m = a[..., 3] >= 250
iou = (hoy & m).sum() / (hoy | m).sum()
pr(f'Huella: área {m.sum()} px² contra {hoy.sum()} de hoy (razón {m.sum() / hoy.sum():.2f}); caja {caja(m)[0]}×{caja(m)[1]} contra {caja(hoy)[0]}×{caja(hoy)[1]}; IoU {iou:.2f}.')
cuerpo = a[..., :3][(a[..., 3] >= 250) & (lum(a[..., :3]) > 0.5)]
pr(f'Casco crema contra el cielo: ' + ', '.join(f'{n} {contraste(cuerpo.mean(0), hexrgb(c)):.1f}:1' for n, c in FONDOS) + '.')
open(os.path.join(AQUI, 'out', 'piloto_medidas.md'), 'w', encoding='utf-8').write('\n'.join(out) + '\n')
