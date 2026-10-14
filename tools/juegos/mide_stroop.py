# -*- coding: utf-8 -*-
"""Mide las losas y los íconos de Tinta o Palabra en plasticina contra el color de diseño (Tarea 100).
Lee el atlas ya empaquetado (`Assets/Resources/Plasticina/stroop`): mide lo MISMO que verá el teléfono. Deja `tools/juegos/out/stroop_medidas.md`.

  EL COLOR DE LA TINTA ES EL ESTÍMULO:
  1. FIDELIDAD: el color medio de la cara de cada losa frente a su color de diseño (ΔE CIELAB; meta ≤ 5), y también de la versión presionada.
  2. SEPARACIÓN de las 4 tintas (CIEDE2000, `tools/paleta_daltonismo.py`: regla ≥ 20 en visión típica, protanopía y deuteranopía) con los colores MEDIDOS de las caras.
  3. EL NOMBRE sobre cada botón (blanco sobre ROJO; tinta sobre las otras tres) frente a lo que rendía el diseño (5,0 / 4,9 / 11,2 / 15,9:1).
  4. EL FONDO DE LA TARJETA (#141B36) con las 4 tintas encima: el contraste de cada palabra contra el fondo MEDIDO frente a lo del diseño (3,4 / 4,7 / 10,8 / 15,4:1).
  5. Orillas, cinta y esquina: color medio frente al de diseño. 6. Los íconos de las reglas (gota y libro): huella frente a la de hoy.
Uso: python -I tools/juegos/mide_stroop.py
"""
import itertools, json, os, sys
import numpy as np
from PIL import Image
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
from huella import *
import paleta_daltonismo as pd
sys.stdout.reconfigure(encoding='utf-8')

AQUI = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.normpath(os.path.join(AQUI, '..', '..'))
ATLAS = os.path.join(REPO, 'unity', 'NeuroVidaCore', 'Assets', 'Resources', 'Plasticina', 'stroop')
manifest = json.load(open(os.path.join(ATLAS, 'manifest.json'), encoding='utf-8'))
atlas = Image.open(os.path.join(ATLAS, 'atlas.png')).convert('RGBA')
FONDO = '#141B36'; TINTA = '#1A1240'
BOTONES = (('rojo', '#C93C3C', '#FFFFFF', 5.0), ('azul', '#4A86E8', TINTA, 4.9), ('amarillo', '#F2CC1D', TINTA, 11.2), ('blanco', '#F4F4F4', TINTA, 15.9))
OTRAS = (('md_cinta_tinta', '#4D3F93'), ('md_cinta_palabra', '#156165'), ('gr_tarjeta_tinta', '#7C5CE0'), ('gr_tarjeta_palabra', '#159A8C'), ('ch_chip_tinta', '#7C5CE0'), ('ch_chip_palabra', '#159A8C'))

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
def cara(nombre):
    """El color medio de la cara de una losa: el centro de su cuerpo (los márgenes de la sombra no cuentan)."""
    a = recorte('losa_' + nombre); m = a[..., 3] >= 250; ys, xs = np.where(m); cy, cx = (ys.min() + ys.max()) // 2, (xs.min() + xs.max()) // 2
    return a[cy - 10:cy + 10, cx - 10:cx + 10, :3].reshape(-1, 3).mean(0)

out = []
def pr(s=''):
    print(s); out.append(s)

pr('# Tinta o Palabra en plasticina — medidas')
pr()
pr('## 1. Fidelidad del color (ΔE CIELAB; meta ≤ 5)')
pr()
pr('| Losa | Color de diseño | Cara medida | ΔE | Presionada | ΔE |')
pr('|---|---|---|---:|---|---:|')
medidos = {}; peor = 0.0
for nombre, color, _, _ in BOTONES:
    c = cara('md_boton_' + nombre); p = cara('md_boton_' + nombre + '_p'); medidos[nombre] = c
    d = de(c, hexrgb(color)); peor = max(peor, d)
    pr(f'| botón {nombre} | {color} | {hexs(c)} | {d:.1f} | {hexs(p)} | {de(p, hexrgb(color)):.1f} (más oscura: se hunde) |')
c = cara('gr_tarjeta_fondo'); fondo = c; d = de(c, hexrgb(FONDO)); peor = max(peor, d)
pr(f'| fondo de la tarjeta | {FONDO} | {hexs(c)} | {d:.1f} | | |')
pr()
pr(f'**El peor: ΔE {peor:.1f}.**')

pr()
pr('## 2. Las 4 tintas se siguen distinguiendo (CIEDE2000, mínimo entre pares; regla ≥ 20)')
pr()
for v, M in pd.M.items():
    labs = {n: pd.lab(M @ pd.lin(medidos[n])) for n in medidos}
    dis = {n: pd.lab(M @ pd.lin(hexrgb(c))) for n, c, _, _ in BOTONES}
    d_med = min(pd.de2000(labs[a], labs[b]) for a, b in itertools.combinations(medidos, 2))
    d_dis = min(pd.de2000(dis[a], dis[b]) for a, b in itertools.combinations(dis, 2))
    pr(f'- {v}: medidas {d_med:.1f} (de diseño {d_dis:.1f})' + ('' if v != 'tritan' else ' (la regla no exige tritanopía)'))

pr()
pr('## 3. El nombre sobre cada botón (contraste WCAG)')
pr()
pr('| Botón | Nombre | Diseño | Medido sobre la cara de plasticina |')
pr('|---|---|---:|---:|')
for nombre, _, txt, dis in BOTONES: pr(f'| {nombre} | {"blanco" if txt == "#FFFFFF" else "tinta"} | {dis:.1f}:1 | {contraste(hexrgb(txt), medidos[nombre]):.1f}:1 |')

pr()
pr('## 4. El fondo de la tarjeta con las 4 tintas encima (contraste WCAG de la palabra)')
pr()
pr('| Tinta de la palabra | Diseño (sobre #141B36) | Medido (sobre el fondo de plasticina) |')
pr('|---|---:|---:|')
for nombre, color, _, _ in BOTONES: pr(f'| {nombre} {color} | {contraste(hexrgb(color), hexrgb(FONDO)):.1f}:1 | {contraste(hexrgb(color), fondo):.1f}:1 |')

pr()
pr('## 5. Orillas, cinta y esquina (color medio contra el de diseño)')
pr()
for nombre, color in OTRAS: pr(f'- {nombre}: {color} → {hexs(cara(nombre))} (ΔE {de(cara(nombre), hexrgb(color)):.1f})')

pr()
pr('## 6. Los íconos de las reglas (lienzo de ±1,1; gris claro para teñir)')
pr()
Xi, Yi = malla(128, 2.2)
def capsula(X, Y, a, b, r):
    pa = np.array(a); ba = np.array(b) - pa; t = np.clip(((X - pa[0]) * ba[0] + (Y - pa[1]) * ba[1]) / (ba @ ba), 0, 1)
    return np.hypot(X - pa[0] - ba[0] * t, Y - pa[1] - ba[1] * t) - r
def caja_redonda(X, Y, cx, cy, hx, hy, r):
    qx, qy = np.abs(X - cx) - hx + r, np.abs(Y - cy) - hy + r
    return np.hypot(np.maximum(qx, 0), np.maximum(qy, 0)) + np.minimum(np.maximum(qx, qy), 0) - r
t = np.clip((Yi + 0.2) / 0.78, 0, 1); gota = np.hypot(Xi, Yi + 0.2 - 0.78 * t) - (0.42 + (0.05 - 0.42) * t)       # UnevenCapsule(x, y + 0,2; 0,42 → 0,05; largo 0,78)
libro = np.minimum(caja_redonda(Xi, Yi, -0.42, 0, 0.44, 0.62, 0.07), caja_redonda(Xi, Yi, 0.42, 0, 0.44, 0.62, 0.07))
for n, sdf in (('gota', gota), ('libro', libro)):
    a = recorte(n); hoy = sdf <= 0.085; m = a[..., 3] >= 250
    pr(f'- {n}: área {m.sum()} px² contra {hoy.sum()} de hoy (razón {m.sum() / hoy.sum():.2f}); caja {caja(m)[0]}×{caja(m)[1]} contra {caja(hoy)[0]}×{caja(hoy)[1]}; IoU {(hoy & m).sum() / (hoy | m).sum():.2f}; color medio {hexs(a[..., :3][m & (lum(a[..., :3]) > 0.5)].mean(0))}.')
open(os.path.join(AQUI, 'out', 'stroop_medidas.md'), 'w', encoding='utf-8').write('\n'.join(out) + '\n')
