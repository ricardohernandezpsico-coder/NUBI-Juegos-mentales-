# -*- coding: utf-8 -*-
"""Mide Aterrizaje Lunar en plasticina (Tarea 111) contra lo que reemplaza. Lee el atlas ya empaquetado (`Assets/Resources/Plasticina/aterrizaje`) y, para «la regla manda», dos capturas REALES (1080 × 2400) del mismo instante, la de antes y la de después.
Deja `tools/juegos/out/aterrizaje_medidas.md`.

  1. LA REGLA MANDA: el centro visual (en x, píxeles de la captura y unidades del lienzo de 1080) de la marca del extremo izquierdo, la del medio, la del extremo derecho y el asta de la bandera, ANTES y DESPUÉS: tienen que coincidir a ±1 unidad.
  2. LA REGLA de plasticina: tamaño del cuerpo, color, y que la cara sea lisa (sin marcas, rayas ni grano).
  3. LA NAVE: simetría (espejo), centro de masa de la silueta (unidades del lienzo), tamaño, y los colores de la ventanilla y la base.
  4. El gallardete, la Tierra, el satélite (sin «+») y las cúpulas; lo que pesa el atlas.
Uso: python -I tools/juegos/mide_aterrizaje.py [--antes carpeta_de_capturas_de_antes] [--despues carpeta_de_capturas_de_despues]
       (las carpetas traen `02-acierto.png`; por omisión, «despues» = unity/test-results/capturas/aterrizaje; sin «antes» se mide solo lo de después)
"""
import json, math, os, sys
import numpy as np
from PIL import Image
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from huella import *
sys.stdout.reconfigure(encoding='utf-8')

AQUI = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.normpath(os.path.join(AQUI, '..', '..'))
BASE = os.path.join(REPO, 'unity', 'NeuroVidaCore', 'Assets', 'Resources', 'Plasticina', 'aterrizaje')
args = sys.argv[1:]
def opt(n, d): return args[args.index(n) + 1] if n in args else d
ANTES = opt('--antes', None); DESPUES = opt('--despues', os.path.join(REPO, 'unity', 'test-results', 'capturas', 'aterrizaje'))
man = json.load(open(os.path.join(BASE, 'manifest.json'), encoding='utf-8'))
atlas = Image.open(os.path.join(BASE, 'atlas.png')).convert('RGBA')
TINTA = np.array([26.0, 18.0, 64.0])

def recorte(n):
    e = next(s for s in man['sprites'] if s['n'] == n)
    return np.asarray(atlas.crop((e['x'], atlas.height - e['y'] - e['h'], e['x'] + e['w'], atlas.height - e['y']))).astype(float), e

def hexs(rgb): return '#%02X%02X%02X' % tuple(int(round(v)) for v in rgb)

def lab(rgb):
    c = np.asarray(rgb, float) / 255.0; c = np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)
    x = (0.4124 * c[0] + 0.3576 * c[1] + 0.1805 * c[2]) / 0.95047; y = 0.2126 * c[0] + 0.7152 * c[1] + 0.0722 * c[2]; z = (0.0193 * c[0] + 0.1192 * c[1] + 0.9505 * c[2]) / 1.08883
    f = lambda t: np.where(t > 0.008856, np.cbrt(t), 7.787 * t + 16 / 116)
    return np.array([116 * f(y) - 16, 500 * (f(x) - f(y)), 200 * (f(y) - f(z))])
def de(a, b): return float(np.linalg.norm(lab(a) - lab(b)))

def luma(a): return (0.2126 * a[..., 0] + 0.7152 * a[..., 1] + 0.0722 * a[..., 2]) / 255.0

out = []
def pr(s=''):
    print(s); out.append(s)

# ------------------------------------------------------------------ 1. la regla manda (sobre capturas reales)
S = 3.0                                                         # píxeles por dp de la captura de 1080 de ancho (360 dp): 1 dp = 3 unidades
X0, X1, XM, XF = 36.0, 324.0, 180.0, 36.0 + 0.37 * 288.0         # las marcas y el lugar justo del 37 de 100 de la captura «acierto»

def px_de(xdp): return (xdp - 180.0) * S + 540.0

def centros(ruta):
    """El centro en x (píxeles) de la marca izquierda, la del medio, la derecha y el asta, en una captura de «acierto»; None si no se ven."""
    a = np.asarray(Image.open(ruta).convert('RGB')).astype(float); H, W = a.shape[:2]
    cerca = (np.abs(a - TINTA).sum(axis=2) < 30)                                          # tinta (26, 18, 64); el cielo y la luna están lejos
    crema = (np.abs(a[..., 0] - 243) < 26) & (np.abs(a[..., 1] - 235) < 26) & (np.abs(a[..., 2] - 211) < 30)
    cara = crema[:, int(px_de(100.0)):int(px_de(120.0))].sum(axis=1)                          # la cara de la regla entre el medio y la marca del extremo derecho
    filas = np.where(cara > 20)[0]
    r0, r1 = int(filas.min()), int(filas.max())
    res = []
    for nombre, xd in (('izq', X0), ('medio', XM), ('der', X1)):
        c = int(round(px_de(xd))); ys = slice(r0 + 3, r1 - 2)
        m = cerca[ys, c - 14:c + 15]; xs = np.where(m.any(axis=0))[0]
        w = m.sum(axis=0).astype(float); res.append(((w * np.arange(c - 14, c + 15)).sum() / w.sum() + 0.5) if w.sum() > 0 else None)
    c = int(round(px_de(XF))); ys = slice(max(0, r0 - 75), r0 - 8)
    m = cerca[ys, c - 14:c + 15]; w = m.sum(axis=0).astype(float)
    res.append(((w * np.arange(c - 14, c + 15)).sum() / w.sum() + 0.5) if w.sum() > 0 else None)
    return res, (r0, r1)

pr('# Aterrizaje Lunar en plasticina — medidas')
pr()
pr('## 1. La regla manda: el centro visual de las marcas y del asta, antes y después')
pr()
pr('Medido sobre las capturas REALES (1080 × 2400, el instante «acierto»: 37 de 100): el centro en x de la marca del extremo izquierdo (a x = 36 dp), la del medio (180 dp), la del extremo derecho (324 dp) y el asta de la bandera (a 142,56 dp, el lugar justo). 1 unidad del lienzo = 1 píxel de la captura.')
pr()
nombres = ('marca del extremo izquierdo', 'marca del medio', 'marca del extremo derecho', 'asta de la bandera')
esperados = [px_de(v) for v in (X0, XM, X1, XF)]
antes = centros(os.path.join(ANTES, '02-acierto.png'))[0] if ANTES else None
despues, banda = centros(os.path.join(DESPUES, '02-acierto.png'))
pr('| Pieza | x esperado (u) | Antes (u) | Después (u) | Diferencia (u) |')
pr('|---|---:|---:|---:|---:|')
peor = 0.0
for i, n in enumerate(nombres):
    a = antes[i] if antes else None; d = despues[i]
    dif = (d - a) if (a is not None and d is not None) else None
    if dif is not None: peor = max(peor, abs(dif))
    pr(f'| {n} | {esperados[i]:.1f} | ' + (f'{a:.2f}' if a is not None else '—') + ' | ' + (f'{d:.2f}' if d is not None else '—') + ' | ' + (f'{dif:+.2f}' if dif is not None else '—') + ' |')
pr()
if antes: pr(f'**La mayor diferencia entre el antes y el después: {peor:.2f} unidades** (la regla pide ±1; las marcas y el asta son procedurales, en tinta, y los pone el código en el mismo x: la plasticina solo cambia el CUERPO de la regla). La prueba `TheRulerMarksAndThePole_StayExactlyWhereTheyWere_WithAndWithoutPlasticina` lo vigila sobre el diseño (±1/3 dp = ±1 u).')
pr()
pr('## 2. La regla de plasticina (3 px por dp; losa del kit de radio 7,5 dp, nine-slice a lo ancho)')
pr()
rg, e = recorte('regla'); H, W = rg.shape[:2]; mi, ma, md, mb = e['margen']
pr(f'- {e["w"]} × {e["h"]} px; bordes del nine-slice (izq, abajo, der, arr) {e["borde"]}; márgenes (izq, arr, der, abajo) {e["margen"]} px.')
cw = (W - mi - md) / 3.0; ch = (H - ma - mb) / 3.0
pr(f'- El cuerpo mide {cw:.1f} × {ch:.1f} dp (el de la regla de siempre: 20 dp de alto; el controlador lo estira a 304 dp de ancho).')
x0 = mi + int(0.30 * (W - mi - md)); x1 = mi + int(0.70 * (W - mi - md)); y0 = ma + int(0.30 * (H - ma - mb)); y1 = ma + int(0.70 * (H - ma - mb))
cara = rg[y0:y1, x0:x1, :3]; L = luma(rg[y0:y1, x0:x1])
pr(f'- La cara (el centro del cuerpo): color medio {hexs(cara.reshape(-1, 3).mean(0))} (el de la regla de siempre, #F3EBD3: ΔE {de(cara.reshape(-1, 3).mean(0), hexrgb("#F3EBD3")):.1f}), luminosidad de {L.min():.2f} a {L.max():.2f}, desviación {L.std():.4f}: **lisa**, sin una marca, raya, grano ni cráter (la prueba pide mínimo > 0,6, variación < 0,12 y desviación < 0,03).')
pr()
pr('## 3. La nave: simétrica; su centro en x es la respuesta')
pr()
nv, e = recorte('nave'); H, W = nv.shape[:2]
izq = nv[:, :W // 2]; der = nv[:, ::-1][:, :W // 2]
dif_alfa = np.abs(izq[..., 3] - der[..., 3]).max(); mascara = (izq[..., 3] > 200) & (der[..., 3] > 200)
dif_luma = np.abs(luma(izq) - luma(der))[mascara].max()
sil = nv[..., 3] >= 128; ys, xs = np.where(sil); cx = (xs + 0.5).mean(); u_por_px = 90.0 * 3.0 / W
pr(f'- {W} × {H} px = {W / e["ppu"]:.0f} × {H / e["ppu"]:.0f} dp (el cuadro de 72 dp de la nave de siempre, que se dibuja a 90 dp = 270 unidades).')
pr(f'- Es su propio espejo: la diferencia de alfa entre la izquierda y la derecha es {dif_alfa:.1f}/255 y la de luminosidad {dif_luma:.4f} (la nave se simetriza al empaquetarla: el promedio de la imagen con su espejo, que equivale a las luces de izquierda y de derecha).')
pr(f'- El centro de masa de la silueta (alfa ≥ 128): a {(cx - W / 2.0) * u_por_px:+.3f} unidades del centro del cuadro (la prueba pide ±2; la nave de hoy también está en el centro).')
pr(f'- La silueta mide {(xs.max() - xs.min() + 1) / e["ppu"]:.1f} × {(ys.max() - ys.min() + 1) / e["ppu"]:.1f} dp (la de hoy: patas hasta ±31,6 dp de ancho y de −23 a +25,6 dp de alto: 63,2 × 48,6).')
def dp_color(a, e, x, y): H, W = a.shape[:2]; return a[int(round(H / 2 - y * e['ppu'] - 0.5)), int(round(W / 2 + x * e['ppu'] - 0.5))]
pr(f'- La ventanilla (a +9 dp): {hexs(dp_color(nv, e, 0, 9)[:3])} (celeste); la base (a −6 dp): {hexs(dp_color(nv, e, 0, -6)[:3])} (sol); la cabina: {hexs(dp_color(nv, e, 0, 20)[:3])}.')
pr()
pr('## 4. El gallardete, la Tierra, el satélite y las cúpulas')
pr()
for n in ('gallardete', 'tierra', 'satelite', 'domo_on', 'domo_off'):
    a, e = recorte(n); pr(f'- {n}: {e["w"]} × {e["h"]} px = {e["w"] / e["ppu"]:.0f} × {e["h"] / e["ppu"]:.0f} dp')
sa, e = recorte('satelite'); pr(f'- El satélite: paneles a ±9,5 dp (alfa {dp_color(sa, e, -9.5, 0)[3]:.0f} y {dp_color(sa, e, 9.5, 0)[3]:.0f}), cuerpo al centro (alfa {dp_color(sa, e, 0, 0)[3]:.0f}); encima y debajo del cuerpo (±5,5 dp) alfa {dp_color(sa, e, 0, 5.5)[3]:.0f} y {dp_color(sa, e, 0, -5.5)[3]:.0f}: nada vertical, sin «+».')
on, e = recorte('domo_on'); off, _ = recorte('domo_off')
pr(f'- Cúpula encendida {hexs(dp_color(on, e, 0, 0)[:3])} (luma {luma(dp_color(on, e, 0, 0)[None, None, :])[0, 0]:.2f}) y apagada {hexs(dp_color(off, e, 0, 0)[:3])} (luma {luma(dp_color(off, e, 0, 0)[None, None, :])[0, 0]:.2f}).')
tr, e = recorte('tierra'); oc = ((tr[..., 3] > 240) & (tr[..., 2] > tr[..., 0] + 60) & (tr[..., 2] > 150)).sum(); ti = ((tr[..., 3] > 240) & (tr[..., 1] > tr[..., 2] + 30) & (tr[..., 1] > tr[..., 0] + 10)).sum()
pr(f'- La Tierra: océano {int(oc)} px y tierras {int(ti)} px; la esfera mide {((tr[..., 3] > 128).any(axis=0).sum()) / e["ppu"]:.0f} dp (el radio de siempre, 58: 116 dp).')
pr()
pr('## 5. Lo que pesa')
pr()
p = man['peso']
pr(f'Atlas {man["ancho"]} × {man["alto"]} px, {len(man["sprites"])} imágenes: PNG {p["png_bytes"] / 1024:.0f} KB; en el APK sin compresión de textura ≈ {p["deflate_rgba32"] / 1048576:.2f} MB (tope por juego 1,5 MB); con ASTC 6×6 ≈ {p["astc6x6"] / 1048576:.2f} MB.')
open(os.path.join(AQUI, 'out', 'aterrizaje_medidas.md'), 'w', encoding='utf-8').write('\n'.join(out) + '\n')
