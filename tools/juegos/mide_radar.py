# -*- coding: utf-8 -*-
"""Mide las cápsulas de plasticina de Rescate contra lo que reemplazan y contra la pantalla del radar (Tarea 94).
Lee el atlas ya empaquetado (`Assets/Resources/Plasticina/radar`), así que mide lo MISMO que verá el teléfono. Imprime y deja `tools/juegos/out/radar_medidas.md`.

  1. HUELLA: el área y la caja de la silueta del cuerpo (alfa opaco, sin la sombra de contacto) frente a la de hoy (la SDF de `RadarSprites.ShapeSdf` con su borde tinta): misma figura y mismo tamaño.
  2. CONTRASTE contra el vidrio del radar (WCAG): el color medio de cada cápsula y su borde más oscuro contra el vidrio más claro (#11495C) y el más oscuro (#051522). Meta: ≥ 3:1.
  3. ESCALA DE GRISES: las seis siluetas se distinguen aunque no se vea el color (cuánto se parecen de a pares: IoU) y cuánto cambia la luminancia entre cápsulas.
Uso: python -I tools/juegos/mide_radar.py
"""
import json, math, os, sys
import numpy as np
from PIL import Image
sys.stdout.reconfigure(encoding='utf-8')

AQUI = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.normpath(os.path.join(AQUI, '..', '..'))
ATLAS = os.path.join(REPO, 'unity', 'NeuroVidaCore', 'Assets', 'Resources', 'Plasticina', 'radar')
TIPOS = ('hexagono', 'gota', 'circulo', 'cuadrado', 'triangulo', 'rombo')
COLORES = ('#A6E36B', '#7FD8FF', '#FF8A6B', '#FFC94A', '#B79BFF', '#FFB3D1')
VIDRIO_CLARO, VIDRIO_OSCURO = '#11495C', '#051522'
LIENZO = 2.36                                  # ±1,18 unidades de diseño
N = 224                                        # el ancho final de la cápsula (px)

def hexrgb(h): return np.array([int(h.lstrip('#')[i:i + 2], 16) for i in (0, 2, 4)], float)
def lum(rgb):
    c = np.asarray(rgb, float) / 255.0; c = np.where(c <= 0.03928, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)
    return 0.2126 * c[..., 0] + 0.7152 * c[..., 1] + 0.0722 * c[..., 2]
def contraste(a, b):
    la, lb = lum(a), lum(b); return (max(la, lb) + 0.05) / (min(la, lb) + 0.05)

# ------------------------------------------------------------------ la silueta de hoy (SDF de RadarSprites.ShapeSdf)
yy, xx = np.mgrid[0:N, 0:N]
X = ((xx + 0.5) / N * 2 - 1) * (LIENZO / 2); Y = -(((yy + 0.5) / N * 2 - 1) * (LIENZO / 2))      # y arriba

def sdf_poligono(pts):
    pts = np.array(pts, float); n = len(pts); d = np.full(X.shape, 1e9)
    for i in range(n):
        a, b = pts[i], pts[(i + 1) % n]; e = b - a
        t = np.clip(((X - a[0]) * e[0] + (Y - a[1]) * e[1]) / (e @ e), 0, 1)
        d = np.minimum(d, np.hypot(X - a[0] - e[0] * t, Y - a[1] - e[1] * t))
    dentro = np.zeros(X.shape, bool)                                                         # trazado de rayos
    for i in range(n):
        a, b = pts[i], pts[(i + 1) % n]
        cond = ((a[1] > Y) != (b[1] > Y)) & (X < (b[0] - a[0]) * (Y - a[1]) / (b[1] - a[1] + 1e-12) + a[0])
        dentro ^= cond
    return np.where(dentro, -d, d)

def silueta_hoy(tipo):
    ink = 0.075
    if tipo == 'hexagono': return sdf_poligono([(0.9 * math.cos(math.pi / 6 + i * math.pi / 3), 0.9 * math.sin(math.pi / 6 + i * math.pi / 3)) for i in range(6)]) - 0.05 <= ink
    if tipo == 'circulo': return np.hypot(X, Y) - 0.92 <= ink
    if tipo == 'cuadrado':
        qx, qy = np.abs(X) - 0.74 + 0.22, np.abs(Y) - 0.74 + 0.22
        return np.hypot(np.maximum(qx, 0), np.maximum(qy, 0)) + np.minimum(np.maximum(qx, qy), 0) - 0.22 <= ink
    if tipo == 'triangulo': return sdf_poligono([(0.0, 0.98), (0.98, -0.74), (-0.98, -0.74)]) - 0.08 <= ink
    if tipo == 'rombo': return sdf_poligono([(0.0, 1.0), (0.84, 0.0), (0.0, -1.0), (-0.84, 0.0)]) - 0.06 <= ink
    # gota: la envolvente de dos círculos
    c0 = (0.0, -0.34, 0.66); c1 = (0.0, 0.96, 0.07)
    t = np.clip((Y - c0[1]) / (c1[1] - c0[1]), 0, 1)                                       # aproximación: el radio varía lineal entre los centros
    r = c0[2] + (c1[2] - c0[2]) * t; cy = c0[1] + (c1[1] - c0[1]) * t
    return np.hypot(X, Y - cy) - r <= ink + 0.0

def carga(tipo):
    m = json.load(open(os.path.join(ATLAS, 'manifest.json'), encoding='utf-8'))
    e = next(s for s in m['sprites'] if s['n'] == 'capsula_' + tipo)
    atlas = Image.open(os.path.join(ATLAS, 'atlas.png')).convert('RGBA'); H = atlas.height
    box = (e['x'], H - e['y'] - e['h'], e['x'] + e['w'], H - e['y'])                         # Unity cuenta y desde abajo
    return np.asarray(atlas.crop(box)).astype(float)

out = []
def pr(s=''):
    print(s); out.append(s)

pr('# Cápsulas de plasticina de Rescate — medidas')
pr()
pr('## 1. Huella (la silueta frente a la de hoy, mismo lienzo de 2,36 unidades)')
pr()
pr('| Forma | Área hoy (px²) | Área plasticina | Razón | Caja ancho×alto hoy | Caja plasticina | IoU con la de hoy |')
pr('|---|---:|---:|---:|---|---|---:|')
nuevas = {}
for t in TIPOS:
    a = carga(t); nuevas[t] = a[..., 3] >= 250
    v = silueta_hoy(t); n_ = nuevas[t]
    def caja(m):
        ys, xs = np.where(m); return (xs.max() - xs.min() + 1, ys.max() - ys.min() + 1)
    iou = (v & n_).sum() / (v | n_).sum()
    pr(f'| {t} | {v.sum()} | {n_.sum()} | {n_.sum() / v.sum():.2f} | {caja(v)[0]}×{caja(v)[1]} | {caja(n_)[0]}×{caja(n_)[1]} | {iou:.2f} |')

pr()
pr('## 2. Contraste contra el vidrio del radar (WCAG; meta ≥ 3:1)')
pr()
pr('| Forma | Color de diseño | Color medio del cuerpo | Contraste vs vidrio claro #11495C | vs vidrio oscuro #051522 | Borde más oscuro (p10) vs vidrio claro |')
pr('|---|---|---|---:|---:|---:|')
medio = {}
for t, col in zip(TIPOS, COLORES):
    a = carga(t); al = a[..., 3]
    yy2, xx2 = np.mgrid[0:N, 0:N]; r = np.hypot(xx2 - N / 2 + 0.5, yy2 - N / 2 + 0.5) / (N / 2)
    cuerpo = (al >= 250) & (r > 0.40)                                                         # sin la ventanilla del centro
    rgb = a[..., :3][cuerpo]; medio[t] = rgb.mean(0)
    L = lum(rgb); borde = rgb[np.argsort(L)[:max(1, len(L) // 10)]].mean(0)                  # el 10 % más oscuro: el canto en sombra
    pr(f'| {t} | {col} | #{int(medio[t][0]):02X}{int(medio[t][1]):02X}{int(medio[t][2]):02X} | {contraste(medio[t], hexrgb(VIDRIO_CLARO)):.1f}:1 | {contraste(medio[t], hexrgb(VIDRIO_OSCURO)):.1f}:1 | {contraste(borde, hexrgb(VIDRIO_CLARO)):.1f}:1 |')

pr()
pr('## 3. Escala de grises')
pr()
pr('Luminancia del color medio (0-1) de cada cápsula: ' + ', '.join(f'{t} {lum(medio[t]):.2f}' for t in TIPOS))
pr()
pr('Parecido de a pares entre las SILUETAS (IoU, 0 = nada que ver; las seis se reconocen por su forma, el color es redundante). Se compara con las de hoy:')
pr()
pr('| Par | Plasticina | Hoy |')
pr('|---|---:|---:|')
mayor_n = mayor_h = 0
for i in range(6):
    for j in range(i + 1, 6):
        a, b = nuevas[TIPOS[i]], nuevas[TIPOS[j]]; ha, hb = silueta_hoy(TIPOS[i]), silueta_hoy(TIPOS[j])
        iou_n = (a & b).sum() / (a | b).sum(); iou_h = (ha & hb).sum() / (ha | hb).sum()
        mayor_n = max(mayor_n, iou_n); mayor_h = max(mayor_h, iou_h)
        if iou_n > 0.6 or iou_h > 0.6: pr(f'| {TIPOS[i]} / {TIPOS[j]} | {iou_n:.2f} | {iou_h:.2f} |')
pr(f'| **el par más parecido** | **{mayor_n:.2f}** | **{mayor_h:.2f}** |')
open(os.path.join(AQUI, 'out', 'radar_medidas.md'), 'w', encoding='utf-8').write('\n'.join(out) + '\n')
