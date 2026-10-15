# -*- coding: utf-8 -*-
"""Mide los estados PRESIONADOS de la Tarea 103 (regla del 14-oct: todo objeto de plasticina que se toca tiene su imagen presionada, aplastada, un punto más oscura y con la huella de un dedo)
contra su imagen normal: la señal de Piloto, el botón de Freno, el satélite de Satélites, la ficha-letra de Punta y la losa apagada de Carga exacta.
Lee los atlas ya empaquetados (`Assets/Resources/Plasticina/<grupo>`): mide lo MISMO que verá el teléfono. Deja `tools/juegos/out/presionados_medidas.md`.

  1. TAMAÑO: ancho y alto del cuerpo (alfa ≥ 250) normal → presionado, el margen que queda hasta el borde del lienzo (no se recorta) y cuánto se corre el centro (no «salta»).
  2. COLOR: el color medio del cuerpo normal → presionado (un punto más oscuro: luminosidad y ΔE CIELAB).
  3. EL DETALLE que es el estímulo (la señal de Piloto: el punto, el anillo o la franja de tinta): cuánta tinta queda (no se pisa).
  4. LA HUELLA: el detalle fino de la superficie (desviación de la luminosidad menos su desenfoque), normal → presionado.
Uso: python -I tools/juegos/mide_presionados.py
"""
import json, os, re, sys
import numpy as np
from PIL import Image
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from huella import *
sys.stdout.reconfigure(encoding='utf-8')

AQUI = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.normpath(os.path.join(AQUI, '..', '..'))
BASE = os.path.join(REPO, 'unity', 'NeuroVidaCore', 'Assets', 'Resources', 'Plasticina')
_cache = {}

def carga(grupo):
    if grupo not in _cache:
        _cache[grupo] = (json.load(open(os.path.join(BASE, grupo, 'manifest.json'), encoding='utf-8')), Image.open(os.path.join(BASE, grupo, 'atlas.png')).convert('RGBA'))
    return _cache[grupo]

def recorte(grupo, nombre):
    m, a = carga(grupo)
    e = next(s for s in m['sprites'] if s['n'] == nombre)
    return np.asarray(a.crop((e['x'], a.height - e['y'] - e['h'], e['x'] + e['w'], a.height - e['y']))).astype(float)

def lab(rgb):
    c = np.asarray(rgb, float) / 255.0; c = np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)
    x = (0.4124 * c[0] + 0.3576 * c[1] + 0.1805 * c[2]) / 0.95047; y = 0.2126 * c[0] + 0.7152 * c[1] + 0.0722 * c[2]; z = (0.0193 * c[0] + 0.1192 * c[1] + 0.9505 * c[2]) / 1.08883
    f = lambda t: np.where(t > 0.008856, np.cbrt(t), 7.787 * t + 16 / 116)
    return np.array([116 * f(y) - 16, 500 * (f(x) - f(y)), 200 * (f(y) - f(z))])
def de(a, b): return float(np.linalg.norm(lab(a) - lab(b)))
def hexs(rgb): return '#%02X%02X%02X' % tuple(int(round(v)) for v in rgb)

def desenfoca(L, r):
    k = int(r * 3) * 2 + 1; xs = np.arange(k) - k // 2; g = np.exp(-xs ** 2 / (2.0 * r * r)); g /= g.sum()
    conv = lambda v: np.convolve(np.pad(v, k // 2, mode='edge'), g, mode='valid')
    return np.apply_along_axis(conv, 1, np.apply_along_axis(conv, 0, L))

def cuerpo(a):
    m = a[..., 3] >= 250; ys, xs = np.where(m); N = a.shape[0]
    return m, (xs.max() - xs.min() + 1, ys.max() - ys.min() + 1, xs.min(), N - 1 - xs.max(), ys.min(), N - 1 - ys.max(), (ys.min() + ys.max()) / 2.0)

def detalle_fino(a, m):
    L = lum(a[..., :3]) * 255
    return float(np.std((L - desenfoca(L, 3))[m]))

def tinta(a, m): return int((m & (lum(a[..., :3]) < 0.04)).sum())

PARES = []
for f in ('hexagono', 'gota', 'circulo', 'cuadrado', 'triangulo'):
    for d in ('punto', 'anillo', 'franja'): PARES.append(('piloto', f'señal {f} {d}', f'senal_{f}_{d}', f'senal_p_{f}_{d}', True))
PARES += [('freno', 'botón encendido', 'boton_encendido', 'boton_encendido_p', False), ('freno', 'botón apagado', 'boton_apagado', 'boton_apagado_p', False),
          ('satelites', 'satélite', 'satelite', 'satelite_p', False), ('punta', 'ficha-letra', 'ficha', 'ficha_p', False)]

# ---- dónde cae la huella de cada pieza (lo que escribieron los scripts de Blender: «[juegos] huella de …: centro (x, y), …, semieje r»), para medir el detalle fino SOLO ahí
OUT = os.path.join(AQUI, 'out')
def lee_huellas(log, patron):
    d = {}
    if not os.path.exists(log): return d
    texto = open(log, encoding='utf-8', errors='replace').read()
    for m in re.finditer(patron + r': centro \((-?[0-9.]+), (-?[0-9.]+)\), espacio libre [0-9.]+, semieje ([0-9.]+)', texto):
        d[m.group(1).strip()] = (float(m.group(2)), float(m.group(3)), float(m.group(4)))
    return d
HUELLAS = {}
for k, v in lee_huellas(os.path.join(OUT, 'render3_piloto_p.log'), r'huella de ([a-z]+ [a-z]+)').items(): HUELLAS['senal ' + k] = (v, 2.36, 0.30, 0.30 * 0.92)
for k, v in lee_huellas(os.path.join(OUT, 'render4_freno_p.log'), r'huella del (botón)').items(): HUELLAS['boton'] = (v, 2.2, 0.34, 0.34 * 0.92)
for k, v in lee_huellas(os.path.join(OUT, 'render3_satelites_p.log'), r'huella del (satélite)').items(): HUELLAS['satelite'] = (v, 2.5, 0.20, 0.20 * 0.92)
HUELLAS['ficha'] = ((0.0, 0.0, 0.40), 2.5, 0.34, 0.34 * 0.92)

def desenfoca(L, r):
    k = int(r * 3) * 2 + 1; xs = np.arange(k) - k // 2; g = np.exp(-xs ** 2 / (2.0 * r * r)); g /= g.sum()
    conv = lambda v: np.convolve(np.pad(v, k // 2, mode='edge'), g, mode='valid')
    return np.apply_along_axis(conv, 1, np.apply_along_axis(conv, 0, L))

def detalle_local(a, m, clave, presionada):
    """El detalle fino alrededor de la huella (un círculo de 1,3 × el semieje): lo que suma la huella a la superficie."""
    if clave not in HUELLAS: return None
    (cx, cy, rd), lienzo, h0, h1 = HUELLAS[clave]
    N = a.shape[0]; ppu = N / lienzo
    z = h1 if presionada else h0
    col = (cx / lienzo + 0.5) * N
    fila = (0.5 - (cy + (z - z) * 0.5) / lienzo) * N                                     # la cara de arriba, vista desde el centro del lienzo (centrado en la pieza): sin corrimiento
    yy, xx = np.mgrid[0:N, 0:N]; zona = (np.hypot(xx - col, yy - fila) < 1.3 * rd * ppu) & m
    if zona.sum() < 30: return None
    L = lum(a[..., :3]) * 255
    return float(np.std((L - desenfoca(L, 3))[zona]))


out = []
def pr(s=''):
    print(s); out.append(s)

pr('# Estados presionados (Tarea 103) — medidas')
pr()
pr('Regla del 14-oct: la pieza aplastada (~8 % más baja, ~4 % más ancha), un punto más oscura y con la huella de un dedo; mismo lienzo y misma escala; sin «saltar» de lugar; el color dentro de lo medido; lo que es el estímulo (el detalle de la señal) no se pisa.')
pr()
pr('| Pieza | Ancho normal → presionado | Alto normal → presionado | Margen mínimo al borde (px) | Centro corrido (px) | Color normal → presionado | Luminosidad | ΔE | Tinta que queda | Detalle fino donde cae la huella, normal → presionado |')
pr('|---|---|---|---:|---:|---|---:|---:|---:|---|')
peor = {}
for grupo, nombre, n, p, es_senal in PARES:
    a = recorte(grupo, n); b = recorte(grupo, p)
    ma, (wa, ha, *_ , ca) = cuerpo(a); mb, (wb, hb, l, r, t, ab, cb) = cuerpo(b)
    cn = a[..., :3][ma].mean(0); cp = b[..., :3][mb].mean(0)
    marg = min(l, r, t, ab)
    k = tinta(b, mb) / max(1, tinta(a, ma)) if es_senal else None
    clave = ('senal ' + nombre.split(' ', 1)[1]) if es_senal else ('boton' if nombre.startswith('botón') else ('satelite' if nombre == 'satélite' else 'ficha'))
    la, lb = detalle_local(a, ma, clave, False), detalle_local(b, mb, clave, True)
    local = ('%.1f → %.1f' % (la, lb)) if la is not None and lb is not None else '—'
    pr(f'| {nombre} | {wa} → {wb} ({wb / wa:.3f}) | {ha} → {hb} ({hb / ha:.3f}) | {marg} | {cb - ca:+.1f} | {hexs(cn)} → {hexs(cp)} | {lum(cp) / lum(cn):.2f} | {de(cn, cp):.1f} | {("%.0f %%" % (k * 100)) if k is not None else "—"} | {local} |')
open(os.path.join(AQUI, 'out', 'presionados_medidas.md'), 'w', encoding='utf-8').write('\n'.join(out) + '\n')
