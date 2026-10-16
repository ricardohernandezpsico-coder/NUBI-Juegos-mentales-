# -*- coding: utf-8 -*-
"""Mide los objetos y las luces de Constelaciones en plasticina (Tarea 106) contra lo que reemplazan y contra el fondo.
Lee el atlas ya empaquetado (`Assets/Resources/Plasticina/parejas`): mide lo MISMO que verá el teléfono. Deja `tools/juegos/out/parejas_medidas.md`.

  1. LOS 16 OBJETOS (4 px por unidad del boceto): tamaño de la silueta (más de 24 y hasta 54 unidades: cabe en su luz), el borde de tinta que los separa del disco claro y su contraste contra él.
  2. LOS GEMELOS: cuántos píxeles distintos hay contra su original (> 700) y cuántos solo por luminancia (> 500): el detalle es de FORMA (anillo, llama, patas, antena), nunca de tono.
  3. NINGÚN PAR de los 12 objetos se parece en luminancia (la peor pareja y cuántos píxeles las separan).
  4. LAS LUCES (3 px por unidad): la dormida contra el cielo del juego, el núcleo, la abierta (la cara contra la tinta); la presionada contra la normal (tamaño, centro, luminosidad, ΔE) y la huella lejos del núcleo.
  5. Lo que pesa el atlas.
Uso: python -I tools/juegos/mide_parejas.py
"""
import json, math, os, sys
import numpy as np
from PIL import Image
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from huella import *
sys.stdout.reconfigure(encoding='utf-8')

AQUI = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.normpath(os.path.join(AQUI, '..', '..'))
BASE = os.path.join(REPO, 'unity', 'NeuroVidaCore', 'Assets', 'Resources', 'Plasticina', 'parejas')
SKY = '#0A0F33'; TINTA = '#1A1240'
NOMBRES = ('planeta', 'cohete', 'cometa', 'ovni', 'cristal', 'casco', 'luna', 'satélite', 'telescopio', 'asteroide', 'antena', 'brújula')
GEMELOS = (0, 1, 3, 5)
man = json.load(open(os.path.join(BASE, 'manifest.json'), encoding='utf-8'))
atlas = Image.open(os.path.join(BASE, 'atlas.png')).convert('RGBA')

def recorte(n):
    e = next(s for s in man['sprites'] if s['n'] == n)
    return np.asarray(atlas.crop((e['x'], atlas.height - e['y'] - e['h'], e['x'] + e['w'], atlas.height - e['y']))).astype(float), e['ppu']

def hexs(rgb): return '#%02X%02X%02X' % tuple(int(round(v)) for v in rgb)
def lab(rgb):
    c = np.asarray(rgb, float) / 255.0; c = np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)
    x = (0.4124 * c[0] + 0.3576 * c[1] + 0.1805 * c[2]) / 0.95047; y = 0.2126 * c[0] + 0.7152 * c[1] + 0.0722 * c[2]; z = (0.0193 * c[0] + 0.1192 * c[1] + 0.9505 * c[2]) / 1.08883
    f = lambda t: np.where(t > 0.008856, np.cbrt(t), 7.787 * t + 16 / 116)
    return np.array([116 * f(y) - 16, 500 * (f(x) - f(y)), 200 * (f(y) - f(z))])
def de(a, b): return float(np.linalg.norm(lab(a) - lab(b)))
def lumv(a): return np.vectorize(lambda r, g, b: lum(np.array([r, g, b])))(a[..., 0], a[..., 1], a[..., 2])

def caja(a, umbral):
    m = a[..., 3] >= umbral; ys, xs = np.where(m); H, W = m.shape
    return xs.min(), xs.max(), ys.min(), ys.max(), W, H

def diferencias(a, b):
    ma = np.abs(a[..., 3] - b[..., 3]) > 80
    mc = (a[..., 3] > 200) & (b[..., 3] > 200) & ((np.abs(a[..., :3] - b[..., :3]).sum(-1)) > 120)
    return int((ma | mc).sum())

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

pr('# Constelaciones en plasticina — medidas')
pr()
pr('## 1. Los 16 objetos (4 px por unidad del boceto; el dibujo ocupa ±22 y entra en una caja de 56)')
pr()
pr('| Objeto | Silueta (unidades) | Píxeles de tinta | Tinta contra el disco claro |')
pr('|---|---|---:|---:|')
disco, ppu_d = recorte('abierta'); H, W = disco.shape[:2]
cara = disco[H // 2 - round(12 * ppu_d) - 3:H // 2 - round(12 * ppu_d) + 4, W // 2 - round(12 * ppu_d) - 3:W // 2 - round(12 * ppu_d) + 4, :3].reshape(-1, 3).mean(0)
imgs = {}
for k in range(12):
    for v in ((0, 1) if k in GEMELOS else (0,)):
        a, ppu = recorte(f'obj_{k}_{v}'); imgs[(k, v)] = a
        x0, x1, y0, y1, w, h = caja(a, 40); ancho, alto = (x1 - x0 + 1) / ppu, (y1 - y0 + 1) / ppu
        tinta = hexrgb(TINTA); ink = int(((a[..., 3] > 240) & (np.abs(a[..., :3] - tinta).sum(-1) < 60)).sum())
        pr(f'| {NOMBRES[k]}{" (gemelo)" if v else ""} | {ancho:.1f} × {alto:.1f} | {ink} | {contraste(tinta, cara):.1f}:1 |')
pr()
pr(f'El disco abierto (cara a (−12, +12) unidades del centro): {hexs(cara)}.')
pr()
pr('## 2. Los gemelos: un detalle GRANDE de forma, nunca un tono')
pr()
pr('| Gemelo | Píxeles distintos | Solo por luminancia |')
pr('|---|---:|---:|')
for k in GEMELOS:
    pr(f'| {NOMBRES[k]} | {diferencias(imgs[(k, 0)], imgs[(k, 1)])} | {dif_gris(imgs[(k, 0)], imgs[(k, 1)])} |')
pr()
pr('## 3. Ningún par de objetos se parece en luminancia')
pr()
peor = (10 ** 9, '', '')
for i in range(12):
    for j in range(i + 1, 12):
        d = dif_gris(imgs[(i, 0)], imgs[(j, 0)])
        if d < peor[0]: peor = (d, NOMBRES[i], NOMBRES[j])
pr(f'La pareja más parecida en luminancia: {peor[1]} y {peor[2]}, con {peor[0]} píxeles distintos de {224 * 224} (umbral de la prueba: 900).')
pr()
pr('## 4. Las luces (3 px por unidad)')
pr()
dor, ppu = recorte('dormante'); pres, _ = recorte('dormante_p'); H, W = dor.shape[:2]
x0, x1, y0, y1, _, _ = caja(dor, 240); px0, px1, py0, py1, _, _ = caja(pres, 240)
pr(f'- La dormida: cuerpo {(x1 - x0 + 1) / ppu:.1f} × {(y1 - y0 + 1) / ppu:.1f} unidades (la de hoy: Ø76); a 33 unidades del centro (el brillo del borde) rinde {max(contraste(dor[H // 2 + round(33 * ppu * math.sin(a / 18 * math.pi)), W // 2 + round(33 * ppu * math.cos(a / 18 * math.pi)), :3], hexrgb(SKY)) for a in range(36)):.1f}:1 contra el cielo.')
nu = dor[H // 2, W // 2, :3]; borde = dor[H // 2, W // 2 + round(30 * ppu), :3]
pr(f'- El núcleo (centro) {hexs(nu)} contra la esfera a 30 unidades {hexs(borde)}: diferencia de azul {(nu[2] - borde[2]) / 255:.2f} (mínimo de la prueba: 0,2).')
oc = recorte('abierta')[0]
pr(f'- La abierta: cuerpo {(caja(oc, 240)[1] - caja(oc, 240)[0] + 1) / ppu:.1f} unidades; luminosidad de la cara {lum(cara):.2f} (suma de canales {cara.sum() / 255:.2f}, mínimo de la prueba 2,2).')
mn, mp = dor[..., 3] >= 250, pres[..., 3] >= 250; cn, cp = dor[..., :3][mn].mean(0), pres[..., :3][mp].mean(0)
pr(f'- La presionada: {(px1 - px0 + 1) / (x1 - x0 + 1):.3f} del ancho y {(py1 - py0 + 1) / (y1 - y0 + 1):.3f} del alto de la normal; centro corrido ({(px0 + px1) / 2 - (x0 + x1) / 2:+.1f}, {(py0 + py1) / 2 - (y0 + y1) / 2:+.1f}) px; margen mínimo al borde del lienzo {min(px0, W - 1 - px1, py0, H - 1 - py1)} px; color medio {hexs(cn)} → {hexs(cp)}, luminosidad {lum(cp) / lum(cn):.2f} de la normal, ΔE {de(cn, cp):.1f}.')
claro = (pres[..., :3].mean(-1) > 150) & (pres[..., 3] > 250); claro[:H // 2 - 40] = False; claro[H // 2 + 40:] = False; claro[:, :W // 2 - 40] = False; claro[:, W // 2 + 40:] = False
ys, xs = np.nonzero(claro); by, bx = ys.mean(), xs.mean()
yy, xx = np.mgrid[0:H, 0:W]; d = np.hypot(xx - bx, yy - by) / ppu
cambia = (np.abs(lumv(dor) - lumv(pres)) > 0.012) & mn & mp
pr(f'- El núcleo de la presionada cae a ({bx - W / 2:+.1f}, {by - H / 2:+.1f}) px del centro; lo que cambia con la huella: {int((cambia & (d > 14)).sum())} px a más de 14 unidades del núcleo y {int((cambia & (d <= 14)).sum())} a menos.')
pr()
pr('## 5. Lo que pesa')
pr()
p = man['peso']
pr(f'Atlas {man["ancho"]} × {man["alto"]} px, {len(man["sprites"])} imágenes: PNG {p["png_bytes"] / 1024:.0f} KB; en el APK sin compresión de textura ≈ {p["deflate_rgba32"] / 1048576:.2f} MB (tope por juego 1,5 MB); con ASTC 6×6 ≈ {p["astc6x6"] / 1048576:.2f} MB.')
open(os.path.join(AQUI, 'out', 'parejas_medidas.md'), 'w', encoding='utf-8').write('\n'.join(out) + '\n')
