# -*- coding: utf-8 -*-
"""Mide la antena, los íconos y las barras del medidor de ¿Verdad o disparate? en plasticina contra lo que reemplazan y contra el fondo (Tarea 97).
Lee el atlas ya empaquetado (`Assets/Resources/Plasticina/disparate`): mide lo MISMO que verá el teléfono. Deja `tools/juegos/out/disparate_medidas.md`.

  1. HUELLA de la antena frente a la de hoy (la SDF de ClayRaster con su borde de tinta, lienzo ±1,2) y dónde cae la luz de la punta contra `AntennaTip`.
  2. LOS ÍCONOS: lo que ocupan del lienzo frente a los de hoy y su contraste de tinta contra los botones lima y coral.
  3. LAS BARRAS DEL MEDIDOR: encendida y apagada (luminosidad) y su contraste contra el cielo.
  4. CONTRASTE de la antena contra el cielo.
Uso: python -I tools/juegos/mide_disparate.py
"""
import json, os, sys
import numpy as np
from PIL import Image
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from huella import *
sys.stdout.reconfigure(encoding='utf-8')

AQUI = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.normpath(os.path.join(AQUI, '..', '..'))
ATLAS = os.path.join(REPO, 'unity', 'NeuroVidaCore', 'Assets', 'Resources', 'Plasticina', 'disparate')
SKY = '#070A28'; LIME = '#9BE564'; CORAL = '#FF6B4A'
manifest = json.load(open(os.path.join(ATLAS, 'manifest.json'), encoding='utf-8'))
atlas = Image.open(os.path.join(ATLAS, 'atlas.png')).convert('RGBA')

def recorte(nombre):
    e = next(s for s in manifest['sprites'] if s['n'] == nombre)
    return np.asarray(atlas.crop((e['x'], atlas.height - e['y'] - e['h'], e['x'] + e['w'], atlas.height - e['y']))).astype(float)

def hexs(rgb): return '#%02X%02X%02X' % tuple(int(round(v)) for v in rgb)
def capsula(X, Y, a, b, r):
    pa = np.array(a); ba = np.array(b) - pa; t = np.clip(((X - pa[0]) * ba[0] + (Y - pa[1]) * ba[1]) / (ba @ ba), 0, 1)
    return np.hypot(X - pa[0] - ba[0] * t, Y - pa[1] - ba[1] * t) - r
def caja_redonda(X, Y, cx, cy, hx, hy, r):
    qx, qy = np.abs(X - cx) - hx + r, np.abs(Y - cy) - hy + r
    return np.hypot(np.maximum(qx, 0), np.maximum(qy, 0)) + np.minimum(np.maximum(qx, qy), 0) - r

out = []
def pr(s=''):
    print(s); out.append(s)

pr('# ¿Verdad o disparate? en plasticina — medidas')
pr()
pr('## 1. La antena (lienzo de ±1,2 unidades de diseño)')
pr()
ant = recorte('antena'); N = ant.shape[0]; X, Y = malla(N, 2.4)
cuerpo = np.minimum.reduce([caja_redonda(X, Y, 0, -0.86, 0.92, 0.13, 0.08), caja_redonda(X, Y, 0, -0.62, 0.40, 0.15, 0.07),
                            capsula(X, Y, (-0.3, -0.52), (-0.05, -0.02), 0.05), capsula(X, Y, (0.3, -0.52), (0.05, -0.02), 0.05),
                            caja_redonda(X, Y, 0, 0.12, 0.075, 0.5, 0.04), np.maximum(np.hypot(X, Y - 0.74) - 0.52, Y - 0.74),
                            np.hypot(X / 0.52, (Y - 0.74) / 0.10) * 0.10 - 0.10, capsula(X, Y, (0, 0.74), (0, 1.0), 0.035), np.hypot(X, Y - 1.02) - 0.085])
hoy = cuerpo <= 0.05
m = ant[..., 3] >= 250
iou = (hoy & m).sum() / (hoy | m).sum()
pr(f'Huella: área {m.sum()} px² contra {hoy.sum()} de hoy (razón {m.sum() / hoy.sum():.2f}); caja {caja(m)[0]}×{caja(m)[1]} contra {caja(hoy)[0]}×{caja(hoy)[1]}; IoU {iou:.2f}.')
sol = (ant[..., 3] >= 250) & (ant[..., 0] > 220) & (ant[..., 1] > 150) & (ant[..., 2] < 130)
ys, xs = np.where(sol[: N // 4])
pr(f'La luz de la punta (sol) cae en ({xs.mean() / N:.3f}, {1 - ys.mean() / N:.3f}) del lienzo; el código la espera en ({0.5:.3f}, {(1.02 / 1.2 + 1) * 0.5:.3f}) (`AntennaTip`).')
crema = ant[..., :3][(ant[..., 3] >= 250) & (lum(ant[..., :3]) > 0.6)].mean(0)
pr(f'Mástil y plato crema ({hexs(crema)}) contra el cielo {SKY}: {contraste(crema, hexrgb(SKY)):.1f}:1.')
acero = ant[..., :3][(ant[..., 3] >= 250)][:0]
pr()
pr('## 2. Los íconos de los botones (lienzo de ±1)')
pr()
pr('| Ícono | Ocupa hoy (ancho × alto, fracción del lienzo) | Ocupa plasticina | Tinta | Contra VERDAD (lima) | Contra DISPARATE (coral) |')
pr('|---|---|---|---|---:|---:|')
Xi, Yi = malla(96, 2.0)
formas = {'icono_visto': np.minimum(capsula(Xi, Yi, (-0.55, 0.0), (-0.18, -0.4), 0.17), capsula(Xi, Yi, (-0.18, -0.4), (0.6, 0.46), 0.17)),
          'icono_ola': np.minimum.reduce([capsula(Xi, Yi, (-0.85, 0.0), (-0.66, 0.38), 0.11), capsula(Xi, Yi, (-0.66, 0.38), (-0.46, -0.38), 0.11), capsula(Xi, Yi, (-0.46, -0.38), (-0.24, 0.2), 0.11),
                                          capsula(Xi, Yi, (0.2, -0.2), (0.4, 0.38), 0.11), capsula(Xi, Yi, (0.4, 0.38), (0.6, -0.38), 0.11), capsula(Xi, Yi, (0.6, -0.38), (0.85, 0.05), 0.11)])}
for n, sdf in formas.items():
    a = recorte(n); h = a.shape[0]; mo = a[..., 3] >= 250; mh = sdf <= 0
    tinta = a[..., :3][mo & (lum(a[..., :3]) < 0.04)].mean(0)
    pr(f'| {n} | {caja(mh)[0] / 96:.2f} × {caja(mh)[1] / 96:.2f} | {caja(mo)[0] / h:.2f} × {caja(mo)[1] / h:.2f} | {hexs(tinta)} | {contraste(tinta, hexrgb(LIME)):.1f}:1 | {contraste(tinta, hexrgb(CORAL)):.1f}:1 |')
pr()
pr('## 3. Las barras del medidor de señal')
pr()
e = recorte('medidor_encendida'); o = recorte('medidor_apagada')
ce = e[..., :3][(e[..., 3] >= 250) & (lum(e[..., :3]) > 0.05)].mean(0); co = o[..., :3][(o[..., 3] >= 250) & (lum(o[..., :3]) > 0.01)].mean(0)
pr(f'Encendida {hexs(ce)} y apagada {hexs(co)}: se distinguen {contraste(ce, co):.1f}:1 (por luminosidad, no solo por color); la apagada contra el cielo {contraste(co, hexrgb(SKY)):.1f}:1, la encendida {contraste(ce, hexrgb(SKY)):.1f}:1.')
pr(f'Cada barra mide 34 u de ancho (la imagen, {e.shape[1]} px con la sombra) y 50 a 154 u de alto; los extremos redondos no se estiran (nine-slice vertical).')
open(os.path.join(AQUI, 'out', 'disparate_medidas.md'), 'w', encoding='utf-8').write('\n'.join(out) + '\n')
