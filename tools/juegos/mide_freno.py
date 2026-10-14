# -*- coding: utf-8 -*-
"""Mide el botón, los cohetes, la plataforma y la señal de ALTO de Freno de Emergencia en plasticina contra lo que reemplazan y contra el fondo (Tarea 96).
Lee el atlas ya empaquetado (`Assets/Resources/Plasticina/freno`): mide lo MISMO que verá el teléfono. Deja `tools/juegos/out/freno_medidas.md`.

  1. HUELLA: el área y la caja de la silueta (alfa opaco, sin la sombra) frente a la de hoy (la SDF de ClayRaster con su borde de tinta).
     El cohete de hoy sale de `SymbolSprite.Rocket` (dibujado en diagonal): la referencia es la nave de Piloto (el mismo dibujo, de pie) en el lienzo del cohete, y además cuánto del alto del lienzo llena (hoy, ~0,89 medido en la captura).
  2. CONTRASTE contra el fondo REAL del juego (WCAG): el suelo de la luna (#595890, medido en la captura) y el cielo de arriba (#070A28).
  3. LA FLECHA contra el botón, y la señal de ALTO contra el cielo.
Uso: python -I tools/juegos/mide_freno.py
"""
import json, math, os, sys
import numpy as np
from PIL import Image
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from huella import *
sys.stdout.reconfigure(encoding='utf-8')

AQUI = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.normpath(os.path.join(AQUI, '..', '..'))
ATLAS = os.path.join(REPO, 'unity', 'NeuroVidaCore', 'Assets', 'Resources', 'Plasticina', 'freno')
FONDOS = (('suelo de la luna', '#595890'), ('cielo de arriba', '#070A28'))

manifest = json.load(open(os.path.join(ATLAS, 'manifest.json'), encoding='utf-8'))
atlas = Image.open(os.path.join(ATLAS, 'atlas.png')).convert('RGBA')

def recorte(nombre):
    e = next(s for s in manifest['sprites'] if s['n'] == nombre)
    return np.asarray(atlas.crop((e['x'], atlas.height - e['y'] - e['h'], e['x'] + e['w'], atlas.height - e['y']))).astype(float)

def round_box(X, Y, cx, cy, hx, hy, r):
    qx, qy = np.abs(X - cx) - hx + r, np.abs(Y - cy) - hy + r
    return np.hypot(np.maximum(qx, 0), np.maximum(qy, 0)) + np.minimum(np.maximum(qx, qy), 0) - r

def hexs(rgb): return '#%02X%02X%02X' % tuple(int(round(v)) for v in rgb)
def huella(nombre, hoy):
    a = recorte(nombre); m = a[..., 3] >= 250
    return a, m, f'| {nombre} | {hoy.sum()} | {m.sum()} | {m.sum() / hoy.sum():.2f} | {caja(hoy)[0]}×{caja(hoy)[1]} | {caja(m)[0]}×{caja(m)[1]} | {(hoy & m).sum() / (hoy | m).sum():.2f} |'

out = []
def pr(s=''):
    print(s); out.append(s)

pr('# Freno de Emergencia en plasticina — medidas')
pr()
pr('## 1. Huella (mismo lienzo cuadrado que el sprite de hoy; cuerpo opaco)')
pr()
pr('| Pieza | Área hoy (px²) | Área plasticina | Razón | Caja hoy | Caja plasticina | IoU |')
pr('|---|---:|---:|---:|---|---|---:|')
razones = []; ious = []; partes = {}
# botón: círculo de 0,8 + borde de 0,08, lienzo de 2,2
for n in ('boton_apagado', 'boton_encendido'):
    a = recorte(n); N = a.shape[0]; X, Y = malla(N, 2.2); hoy = np.hypot(X, Y) <= 0.88
    a, m, fila = huella(n, hoy); partes[n] = a; pr(fila); razones.append(m.sum() / hoy.sum()); ious.append((hoy & m).sum() / (hoy | m).sum())
# señal de ALTO: octágono de circunradio 0,82 + borde de 0,07, lienzo de 2,2
a = recorte('alto'); N = a.shape[0]; X, Y = malla(N, 2.2)
octa =[(0.82 * math.cos(math.pi / 8 + i * math.pi / 4), 0.82 * math.sin(math.pi / 8 + i * math.pi / 4)) for i in range(8)]
hoy = sdf_poligono(octa, X, Y) <= 0.07
a, m, fila = huella('alto', hoy); partes['alto'] = a; pr(fila); razones.append(m.sum() / hoy.sum()); ious.append((hoy & m).sum() / (hoy | m).sum())
# plataforma: losa y torres (RoundBox) + borde de 0,05, lienzo de 2,1
a = recorte('plataforma'); N = a.shape[0]; X, Y = malla(N, 2.1)
cuerpo = np.minimum(round_box(X, Y, 0, -0.62, 0.9, 0.16, 0.08), np.minimum(round_box(X, Y, -0.62, -0.2, 0.07, 0.3, 0.04), round_box(X, Y, 0.62, -0.2, 0.07, 0.3, 0.04)))
hoy = cuerpo <= 0.05
a, m, fila = huella('plataforma', hoy); partes['plataforma'] = a; pr(fila); razones.append(m.sum() / hoy.sum()); ious.append((hoy & m).sum() / (hoy | m).sum())
# cohete: la referencia es la nave de Piloto (el mismo dibujo) en el lienzo de 1,9 centrado en y = −0,15
for n in ('cohete_coral', 'cohete_cielo', 'cohete_lima'):
    a = recorte(n); N = a.shape[0]; X, Y = malla(N, 1.9); Y = Y - 0.15
    hull = np.maximum(np.maximum(np.hypot(X + 0.62, Y + 0.06) - 0.92, np.hypot(X - 0.62, Y + 0.06) - 0.92), -0.58 - Y)
    aleta = sdf_poligono([(0.20, -0.10), (0.52, -0.46), (0.52, -0.68), (0.20, -0.52)], np.abs(X), Y)
    llama = sdf_capsula(X, Y, (0.0, -0.56, 0.17), (0.0, -0.90, 0.04))
    hoy = np.minimum(np.minimum(hull, aleta), llama) <= 0.075
    a, m, fila = huella(n, hoy); partes[n] = a; pr(fila); razones.append(m.sum() / hoy.sum()); ious.append((hoy & m).sum() / (hoy | m).sum())
pr()
pr(f'**Razón de áreas: {min(razones):.2f} a {max(razones):.2f}; IoU: {min(ious):.2f} a {max(ious):.2f}.**')
m = partes['cohete_coral'][..., 3] >= 250; ys = np.where(m.any(1))[0]
pr(f'El cohete llena el {(ys.max() - ys.min() + 1) / m.shape[0] * 100:.0f} % del alto del lienzo (el de hoy, ~89 % medido en la captura del juego; la llama y la punta incluidas).')

pr()
pr('## 2. Contraste contra el fondo del juego (WCAG)')
pr()
pr('| Pieza | Color medio | ' + ' | '.join(n + ' ' + c for n, c in FONDOS) + ' |')
pr('|---|---|' + '---:|' * len(FONDOS))
def cuerpo_medio(a, clase):
    """El color medio de las partes opacas de [clase]: 'claro' (L > 0.06) o 'oscuro' (la tinta, L < 0.03)."""
    l = lum(a[..., :3]); op = a[..., 3] >= 250
    sel = op & ((l > 0.06) if clase == 'claro' else (l < 0.03))
    return a[..., :3][sel].mean(0)
medios = {}
for n in ('boton_apagado', 'boton_encendido', 'alto', 'plataforma', 'cohete_coral', 'cohete_cielo', 'cohete_lima'):
    a = partes[n]
    # el cuerpo del botón apagado es oscuro: su «color medio» sale de las partes opacas lejos del borde (L > 0.01) y lejos de la flecha crema (L < 0.5)
    if n == 'boton_apagado':
        l = lum(a[..., :3]); sel = (a[..., 3] >= 250) & (l > 0.01) & (l < 0.3); med = a[..., :3][sel].mean(0)
    elif n.startswith('cohete'):
        l = lum(a[..., :3]); sel = (a[..., 3] >= 250) & (l > 0.8); med = a[..., :3][sel].mean(0)          # el casco crema (el acento lima, de L ≈ 0,62, queda afuera)
    else: med = cuerpo_medio(a, 'claro')
    medios[n] = med
    pr(f'| {n}{" (casco crema)" if n.startswith("cohete") else ""} | {hexs(med)} | ' + ' | '.join(f'{contraste(med, hexrgb(c)):.1f}:1' for _, c in FONDOS) + ' |')
# el acento de cada cohete (punta y aletas) contra el suelo, solo informativo
pr()
pr('Acentos de los cohetes (punta y aletas) contra el suelo de la luna: ' + ', '.join(f'{n[7:]} {contraste(hexrgb(c), hexrgb(FONDOS[0][1])):.1f}:1' for n, c in (('cohete_coral', '#FF6B4A'), ('cohete_cielo', '#4CC9F0'), ('cohete_lima', '#9BE564'))) + '.')

pr()
pr('## 3. La flecha contra el botón, y el borde de cada botón contra el suelo')
pr()
for n in ('boton_apagado', 'boton_encendido'):
    a = partes[n]; l = lum(a[..., :3]); op = a[..., 3] >= 250
    h, w = l.shape; yy, xx = np.mgrid[0:h, 0:w]
    centro = (np.hypot(xx - w / 2, yy - h / 2) < w * 0.2) & op                         # dentro de la flecha o del cuerpo cerca del centro
    if n == 'boton_apagado': flecha = a[..., :3][centro & (l > 0.5)]; cuerpo = medios[n]
    else: flecha = a[..., :3][centro & (l < 0.03)]; cuerpo = medios[n]
    pr(f'- {n}: flecha {hexs(flecha.mean(0))} contra el cuerpo {hexs(cuerpo)}: {contraste(flecha.mean(0), cuerpo):.1f}:1 ({len(flecha)} px de flecha).')
a = partes['alto']
pr(f'- alto: el cuerpo coral contra el cielo {contraste(medios["alto"], hexrgb(FONDOS[1][1])):.1f}:1; el aro crema contra el coral: {contraste(a[..., :3][(a[..., 3] >= 250) & (lum(a[..., :3]) > 0.5)].mean(0), medios["alto"]):.1f}:1.')
pr()
pr('Para el borde del botón apagado: el de hoy lleva un borde de tinta (#1A1240, 2,6:1 contra el suelo); el de plasticina no lleva borde, solo sombra de contacto.')
open(os.path.join(AQUI, 'out', 'freno_medidas.md'), 'w', encoding='utf-8').write('\n'.join(out) + '\n')
