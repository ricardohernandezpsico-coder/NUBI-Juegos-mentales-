# -*- coding: utf-8 -*-
"""Mide la roca, la roca presionada y las estrellas de Lluvia de meteoros en plasticina (Tarea 105) contra lo que reemplazan y contra el fondo.
Lee el atlas ya empaquetado (`Assets/Resources/Plasticina/meteoros`): mide lo MISMO que verá el teléfono. Deja `tools/juegos/out/meteoros_medidas.md`.

  1. EL CUERPO de cada roca (120 px por unidad de diseño): ancho y alto contra el dibujo de hoy (la elipse de 0,9 × 0,62 con ondulaciones) y el margen que deja a la sombra y la brasa.
  2. LA ROCA PRESIONADA: ancho, margen, centro corrido y color contra la normal.
  3. EL COLOR: la roca gris teñida con cada color del juego contra su color de diseño (ΔE CIELAB); el dorado (la bonificación) y los otros.
  4. LA PLACA con la palabra (tinta sobre la losa chica crema del kit): contraste de la palabra y de la placa contra cada roca teñida.
  5. LAS ESTRELLAS de la constelación: redondez, tamaño, contraste contra el cielo y entre sí.
Uso: python -I tools/juegos/mide_meteoros.py
"""
import json, math, os, sys
import numpy as np
from PIL import Image
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from huella import *
sys.stdout.reconfigure(encoding='utf-8')

AQUI = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.normpath(os.path.join(AQUI, '..', '..'))
BASE = os.path.join(REPO, 'unity', 'NeuroVidaCore', 'Assets', 'Resources', 'Plasticina')
SKY = '#0A0F33'; TINTA = '#1A1240'
TINTES = (('lila', '#B8A4FF'), ('celeste', '#4CC9F0'), ('coral', '#FF6B4A'), ('sol (dorado)', '#FFC93C'), ('polvo', '#A29FBE'))
_c = {}

def carga(g):
    if g not in _c: _c[g] = (json.load(open(os.path.join(BASE, g, 'manifest.json'), encoding='utf-8')), Image.open(os.path.join(BASE, g, 'atlas.png')).convert('RGBA'))
    return _c[g]

def recorte(g, n):
    m, a = carga(g); e = next(s for s in m['sprites'] if s['n'] == n)
    return np.asarray(a.crop((e['x'], a.height - e['y'] - e['h'], e['x'] + e['w'], a.height - e['y']))).astype(float)

def hexs(rgb): return '#%02X%02X%02X' % tuple(int(round(v)) for v in rgb)
def lab(rgb):
    c = np.asarray(rgb, float) / 255.0; c = np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)
    x = (0.4124 * c[0] + 0.3576 * c[1] + 0.1805 * c[2]) / 0.95047; y = 0.2126 * c[0] + 0.7152 * c[1] + 0.0722 * c[2]; z = (0.0193 * c[0] + 0.1192 * c[1] + 0.9505 * c[2]) / 1.08883
    f = lambda t: np.where(t > 0.008856, np.cbrt(t), 7.787 * t + 16 / 116)
    return np.array([116 * f(y) - 16, 500 * (f(x) - f(y)), 200 * (f(y) - f(z))])
def de(a, b): return float(np.linalg.norm(lab(a) - lab(b)))

def cuerpo(a):
    m = a[..., 3] >= 250; ys, xs = np.where(m); H, W = m.shape
    return m, (xs.max() - xs.min() + 1, ys.max() - ys.min() + 1, xs.min(), W - 1 - xs.max(), ys.min(), H - 1 - ys.max(), (ys.min() + ys.max()) / 2.0)

out = []
def pr(s=''):
    print(s); out.append(s)

pr('# Lluvia de meteoros en plasticina — medidas')
pr()
pr('## 1. El cuerpo de la roca (120 px por unidad de diseño; el de hoy: una elipse de 0,9 × 0,62 con ondulaciones, ~2,0 × 1,3)')
pr()
pr('| Forma | Cuerpo (px) | En unidades | Margen izq/der/arr/abajo (px) | Color medio (gris claro) |')
pr('|---|---|---|---|---|')
medios = []
for i in range(4):
    a = recorte('meteoros', f'roca_{i}'); m, (w, h, l, r, t, b, c) = cuerpo(a); medios.append(a[..., :3][m].mean(0))
    pr(f'| {i} | {w}×{h} | {w / 120:.2f} × {h / 120:.2f} | {l} / {r} / {t} / {b} | {hexs(medios[-1])} |')
pr()
pr('## 2. La roca presionada (aplastada, un punto más oscura, con la huella de un dedo en una esquina)')
pr()
pr('| Forma | Ancho normal → presionada | Alto normal → presionada | Margen mínimo (px) | Centro corrido (px) | Color | Luminosidad | ΔE |')
pr('|---|---|---|---:|---:|---|---:|---:|')
for i in range(4):
    a = recorte('meteoros', f'roca_{i}'); b = recorte('meteoros', f'roca_p_{i}'); ma, (wa, ha, *_, ca) = cuerpo(a); mb, (wb, hb, l, r, t, bb, cb) = cuerpo(b)
    cn, cp = a[..., :3][ma].mean(0), b[..., :3][mb].mean(0)
    pr(f'| {i} | {wa} → {wb} ({wb / wa:.3f}) | {ha} → {hb} ({hb / ha:.3f}) | {min(l, r, t, bb)} | {cb - ca:+.1f} | {hexs(cn)} → {hexs(cp)} | {lum(cp) / lum(cn):.2f} | {de(cn, cp):.1f} |')
pr()
pr('## 3. El color: la roca gris teñida contra el color de diseño (ΔE CIELAB)')
pr()
pr('| Teñida de | Color de diseño | La roca teñida (forma 0) | ΔE |')
pr('|---|---|---|---:|')
for nombre, hx in TINTES:
    t = hexrgb(hx); tint = medios[0] * t / 255.0
    pr(f'| {nombre} | {hx} | {hexs(tint)} | {de(tint, t):.1f} |')
pr()
pr('El dorado (bonificación) frente a los demás colores de la roca (ΔE entre las rocas teñidas): ' + ', '.join(f'{n} {de(medios[0] * hexrgb(hx) / 255.0, medios[0] * hexrgb("#FFC93C") / 255.0):.0f}' for n, hx in TINTES if 'sol' not in n) + '.')
pr()
pr('## 4. La placa de la palabra (losa chica crema del kit)')
pr()
ch = recorte('comun', 'losa_ch_crema'); H, W = ch.shape[:2]
cara = ch[int(H * 0.4):int(H * 0.6), int(W * 0.4):int(W * 0.6), :3].reshape(-1, 3).mean(0)
pr(f'La palabra de tinta ({TINTA}) sobre la losa crema ({hexs(cara)}): **{contraste(hexrgb(TINTA), cara):.1f}:1** (en el dibujo de hoy, sobre crema #FFF8EC: {contraste(hexrgb(TINTA), hexrgb("#FFF8EC")):.1f}:1).')
pr()
pr('La placa crema contra cada roca teñida (luminosidad; además la separa su sombra, que ya no es el borde de tinta de hoy): ' + ', '.join(f'{n} {contraste(cara, medios[0] * hexrgb(hx) / 255.0):.2f}:1' for n, hx in TINTES) + '.')
pr()
pr('## 5. Las estrellas de la constelación')
pr()
est = {}
for n in ('estrella_encendida', 'estrella_apagada'):
    a = recorte('meteoros', n); m, (w, h, *_) = cuerpo(a); N = a.shape[0]; est[n] = a[..., :3][m].mean(0)
    pr(f'- {n}: cuerpo {w}×{h} de {N} px (redondez {m.sum() / (math.pi * (w / 2) ** 2):.2f}), color medio {hexs(est[n])}, contra el cielo {contraste(est[n], hexrgb(SKY)):.1f}:1.')
pr(f'- Encendida contra apagada: {contraste(est["estrella_encendida"], est["estrella_apagada"]):.1f}:1 (por luminosidad, no solo por color).')
open(os.path.join(AQUI, 'out', 'meteoros_medidas.md'), 'w', encoding='utf-8').write('\n'.join(out) + '\n')
