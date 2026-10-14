# -*- coding: utf-8 -*-
"""Mide el reactor de Carga exacta en plasticina contra lo que reemplaza y contra el fondo (Tarea 97).
Lee el atlas ya empaquetado (`Assets/Resources/Plasticina/calculo`): mide lo MISMO que verá el teléfono. Deja `tools/juegos/out/calculo_medidas.md`.

  1. TAMAÑO Y CENTRO: el aro (RingR = 84 dp) y el núcleo (CoreR = 72 dp) frente a los de hoy (`CargaLayout`), a 3 px/dp.
  2. EL NÚCLEO ES CLARO: el código lo tiñe con `Image.color` (azul noche → dorado): el color medio de su cara y qué rinde el número encima en los dos colores.
  3. CONTRASTE del aro y del núcleo apagado contra el cielo, y de las losas de las celdas (crema / cielo / sol) con el número de tinta.
Uso: python -I tools/juegos/mide_calculo.py
"""
import json, os, sys
import numpy as np
from PIL import Image
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from huella import *
sys.stdout.reconfigure(encoding='utf-8')

AQUI = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.normpath(os.path.join(AQUI, '..', '..'))
PLASTICINA = os.path.join(REPO, 'unity', 'NeuroVidaCore', 'Assets', 'Resources', 'Plasticina')
PX_DP = 3.0
SKY = '#05081D'; INK = '#1A1240'

def carga(grupo):
    d = os.path.join(PLASTICINA, grupo)
    return json.load(open(os.path.join(d, 'manifest.json'), encoding='utf-8')), Image.open(os.path.join(d, 'atlas.png')).convert('RGBA')

def recorte(grupo, nombre):
    m, at = carga(grupo); e = next(s for s in m['sprites'] if s['n'] == nombre)
    return np.asarray(at.crop((e['x'], at.height - e['y'] - e['h'], e['x'] + e['w'], at.height - e['y']))).astype(float)

def hexs(rgb): return '#%02X%02X%02X' % tuple(int(round(v)) for v in rgb)
out = []
def pr(s=''):
    print(s); out.append(s)

pr('# Carga exacta en plasticina — medidas')
pr()
aro = recorte('calculo', 'reactor_aro'); nuc = recorte('calculo', 'reactor_nucleo'); N = aro.shape[0]
pr('## 1. Tamaño y centro (lienzo cuadrado de ±92 dp, 3 px/dp)')
pr()
pr('| Pieza | Diámetro hoy (dp) | Diámetro plasticina (dp) | Centro (x, y) en px del lienzo |')
pr('|---|---:|---:|---|')
for nombre, a, hoy in (('reactor_aro', aro, 168.0), ('reactor_nucleo', nuc, 144.0)):
    m = a[..., 3] >= 250; ys, xs = np.where(m)
    pr(f'| {nombre} | {hoy:.0f} | {(xs.max() - xs.min() + 1) / PX_DP:.1f} | ({(xs.min() + xs.max()) / 2:.0f}, {(ys.min() + ys.max()) / 2:.0f}) de {N / 2:.0f} |')
pr()
pr(f'El hueco del aro está vacío (alfa {int(aro[N // 2, N // 2, 3])} en el centro): ahí va el núcleo.')

pr()
pr('## 2. El núcleo es claro (el código lo tiñe) y el número se lee en los dos colores')
pr()
yy, xx = np.mgrid[0:N, 0:N]; r = np.hypot(xx - N / 2, yy - N / 2)
cara = (nuc[..., 3] >= 250) & (r < 60 * PX_DP)
gris = nuc[..., :3][cara].mean(0)
pr(f'Color medio de la cara del núcleo: {hexs(gris)} (luminancia {lum(gris):.2f}).')
pr()
pr('| Estado | Teñido (CoreDark / CoreGold × gris) | Número | Contraste |')
pr('|---|---|---|---:|')
oscuro = np.array([13, 20, 53]) * gris / 255.0; dorado = np.array([233, 169, 58]) * gris / 255.0
pr(f'| apagado | {hexs(oscuro)} | blanco | {contraste(np.array([255, 255, 255]), oscuro):.1f}:1 |')
pr(f'| logrado | {hexs(dorado)} | tinta {INK} | {contraste(hexrgb(INK), dorado):.1f}:1 |')
pr(f'| apagado contra logrado | | | {contraste(oscuro, dorado):.1f}:1 |')

pr()
pr('## 3. Contraste contra el cielo (WCAG) y las losas de las celdas')
pr()
ring = aro[..., :3][aro[..., 3] >= 250].mean(0)
pr(f'- Aro (azul noche): {hexs(ring)} contra el cielo {SKY}: {contraste(ring, hexrgb(SKY)):.1f}:1 (el de hoy, #232B57 con borde de tinta; es el cuerpo del reactor, no un estímulo).')
pr(f'- Núcleo apagado ({hexs(oscuro)}) contra el aro: {contraste(oscuro, ring):.1f}:1.')
m, at = carga('comun')
pr()
pr('| Losa de la celda | Color de la cara | Número de tinta |')
pr('|---|---|---:|')
for nombre, etiqueta in (('losa_md_crema', 'normal (crema)'), ('losa_md_cielo', 'elegida (cielo)'), ('losa_md_sol', 'lograda (sol)'), ('losa_md_cielo_p', 'elegida hundida (cielo, presionada)')):
    a = recorte('comun', nombre); h, w = a.shape[:2]
    cara = a[h // 2 - 8:h // 2 + 8, w // 2 - 8:w // 2 + 8, :3].reshape(-1, 3).mean(0)
    pr(f'| {etiqueta} | {hexs(cara)} | {contraste(hexrgb(INK), cara):.1f}:1 |')
open(os.path.join(AQUI, 'out', 'calculo_medidas.md'), 'w', encoding='utf-8').write('\n'.join(out) + '\n')
