# -*- coding: utf-8 -*-
"""Mide la antena, los íconos y las barras del medidor de ¿Verdad o disparate? en plasticina contra lo que reemplazan y contra el fondo (Tarea 97).
Lee el atlas ya empaquetado (`Assets/Resources/Plasticina/disparate`): mide lo MISMO que verá el teléfono. Deja `tools/juegos/out/disparate_medidas.md`.

  1. LA ANTENA PARABÓLICA (corrección del 14-oct): su caja, la inclinación del plato, dónde cae la luz de la punta y el centro del plato contra `AntennaTip` y `AntennaDish`, y que cada par de piezas que se tocan (plato, mástil, brazo, receptor, luz) se separe por tono.
  2. LOS ÍCONOS: lo que ocupan del lienzo, su contraste de tinta contra los botones lima y coral y, en la onda cortada, que a 24, 28 y 32 dp se sigan viendo DOS tramos con un hueco claro entre ellos.
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
def _lab(rgb):
    c = np.asarray(rgb, float) / 255.0; c = np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)
    x = (0.4124 * c[0] + 0.3576 * c[1] + 0.1805 * c[2]) / 0.95047; y = 0.2126 * c[0] + 0.7152 * c[1] + 0.0722 * c[2]; z = (0.0193 * c[0] + 0.1192 * c[1] + 0.9505 * c[2]) / 1.08883
    f = lambda t: np.where(t > 0.008856, np.cbrt(t), 7.787 * t + 16 / 116)
    return np.array([116 * f(y) - 16, 500 * (f(x) - f(y)), 200 * (f(y) - f(z))])
def de(a, b): return float(np.linalg.norm(_lab(a) - _lab(b)))
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
pr('## 1. La antena parabólica (lienzo de ±1,2 unidades de diseño; corrección del 14-oct)')
pr()
ant = recorte('antena'); N = ant.shape[0]
m = ant[..., 3] >= 250
cx0, cy0, cx1, cy1 = (lambda ys, xs: (xs.min() / N, 1 - ys.max() / N, xs.max() / N, 1 - ys.min() / N))(*np.where(m))
pr(f'Cuerpo: caja ({cx0:.3f}–{cx1:.3f}) × ({cy0:.3f}–{cy1:.3f}) del lienzo, área {m.sum() / N / N:.3f} del lienzo.')
# el plato: el labio crema (un aro) da la forma de la boca (los ejes del aro = los de la elipse); el brazo lila (su tramo de arriba) da hacia dónde mira el plato
cielo = m & (ant[..., 2] > 200) & (ant[..., 0] < 140) & (ant[..., 1] > 150)
ys, xs = np.where(cielo)
altura = np.arange(N)[:, None] * np.ones((1, N))
labio = m & (ant[..., 0] > 235) & (ant[..., 1] > 225) & (ant[..., 2] > 200) & (altura < N * 0.55)
yl, xl = np.where(labio); cov = np.cov(np.vstack([xl - xl.mean(), -(yl - yl.mean())])); val, vec = np.linalg.eigh(cov)
ang_largo = np.degrees(np.arctan2(vec[1, 1], vec[0, 1])); ang_largo = (ang_largo + 90) % 180 - 90
brazo = m & (np.abs(ant[..., 0] - 142) < 40) & (np.abs(ant[..., 1] - 124) < 40) & (ant[..., 2] > 190) & (altura < N * 0.30)
ya, xa = np.where(brazo); cova = np.cov(np.vstack([xa - xa.mean(), -(ya - ya.mean())])); va, vea = np.linalg.eigh(cova)
dir_brazo = np.degrees(np.arctan2(vea[0, 1], vea[1, 1]));  dir_brazo = abs((dir_brazo + 90) % 180 - 90)
pr(f'El plato (interior celeste): centro ({xs.mean() / N:.3f}, {1 - ys.mean() / N:.3f}) del lienzo. La boca (el labio crema) es una elipse de {np.sqrt(val[1] / val[0]):.2f}:1 con el lado largo a {ang_largo:.0f}° de la horizontal; el brazo del receptor (el eje del plato) sale a {dir_brazo:.0f}° de la vertical hacia la derecha: el plato mira de lado y arriba, no de frente.')
sol = m & (ant[..., 0] > 220) & (ant[..., 1] > 150) & (ant[..., 2] < 130)
ys2, xs2 = np.where(sol)
pr(f'La luz de la punta (sol) cae en ({xs2.mean() / N:.3f}, {1 - ys2.mean() / N:.3f}); el código la espera en (0.678, 0.902) (`AntennaTip`). El centro del plato: el código espera (0.603, 0.648) (`AntennaDish`).')
crema = ant[..., :3][m & (lum(ant[..., :3]) > 0.7) & (ant[..., 0] > 235)].mean(0)
pr(f'El plato crema ({hexs(crema)}) contra el cielo {SKY}: {contraste(crema, hexrgb(SKY)):.1f}:1.')
# tono contra tono: cada par de piezas que se tocan se separa
lila = ant[..., :3][m & (np.abs(ant[..., 0] - 142) < 22) & (np.abs(ant[..., 1] - 124) < 22) & (np.abs(ant[..., 2] - 224) < 30)]
lila = lila.mean(0) if len(lila) else np.array([142, 124, 224.0])
rec = ant[..., :3][m & (lum(ant[..., :3]) < 0.08) & (ant[..., 2] > ant[..., 0]) & (np.arange(N)[:, None] < N // 4)]
rec = rec.mean(0) if len(rec) else hexrgb('#4A3F8C')
solc = ant[..., :3][sol].mean(0)
pr()
pr('| Par que se toca | Colores | Contraste de luminosidad | ΔE CIELAB |')
pr('|---|---|---:|---:|')
for n, c1, c2 in (('plato (crema) y mástil (lila)', crema, lila), ('brazo (lila) y receptor (tinta suave)', lila, rec), ('receptor y luz (sol)', rec, solc), ('brazo (lila) y luz (sol)', lila, solc), ('mástil (lila) y base (uva)', lila, hexrgb('#B8A4FF'))):
    pr(f'| {n} | {hexs(c1)} / {hexs(c2)} | {contraste(c1, c2):.1f}:1 | {de(c1, c2):.0f} |')
pr()
pr('La silueta (solo la forma, sin color): un plato oblicuo con su boca elíptica, un brazo que sale del centro hacia la luz y un mástil con su base (ver `tools/juegos/out/disp_final_prev.png`).')
pr()
pr('## 2. Los íconos de los botones (lienzo de ±1)')
pr()
pr('| Ícono | Ocupa (ancho × alto, fracción del lienzo) | Tinta | Contra VERDAD (lima) | Contra DISPARATE (coral) |')
pr('|---|---|---|---:|---:|')
for n in ('icono_visto', 'icono_ola'):
    a_ = recorte(n); h = a_.shape[0]; mo = a_[..., 3] >= 250
    tinta = a_[..., :3][mo & (lum(a_[..., :3]) < 0.04)].mean(0)
    pr(f'| {n} | {caja(mo)[0] / h:.2f} × {caja(mo)[1] / h:.2f} | {hexs(tinta)} | {contraste(tinta, hexrgb(LIME)):.1f}:1 | {contraste(tinta, hexrgb(CORAL)):.1f}:1 |')
pr()
# la onda cortada: ¿se siguen viendo DOS tramos con un hueco claro a 24, 28 y 32 dp?
o = recorte('icono_ola'); h = o.shape[0]; tinta_o = (o[..., 3] >= 128)
pr('La onda cortada a tamaño chico (la imagen cubre ±1 = 2 unidades; se reduce con promedio y se umbral a la mitad):')
pr()
pr('| Tamaño | Trazos separados | Hueco entre ellos | Grosor del trazo |')
pr('|---|---:|---:|---:|')
for dp in (24, 28, 32):
    k = h // dp                                                   # px de la maestra por px chico
    chico = tinta_o[: dp * k, : dp * k].reshape(dp, k, dp, k).mean(axis=(1, 3)) > 0.5
    cols = chico.any(axis=0); izq = np.where(cols)[0]
    hueco = 0; mejor = 0; run = 0
    for c in range(izq.min(), izq.max() + 1):
        if not cols[c]: run += 1; mejor = max(mejor, run)
        else: run = 0
    # componentes conexas (4 vecinos)
    visto = np.zeros_like(chico); comp = 0
    for y0 in range(dp):
        for x0 in range(dp):
            if chico[y0, x0] and not visto[y0, x0]:
                comp += 1; pila = [(y0, x0)]; visto[y0, x0] = True
                while pila:
                    y1, x1 = pila.pop()
                    for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                        y2, x2 = y1 + dy, x1 + dx
                        if 0 <= y2 < dp and 0 <= x2 < dp and chico[y2, x2] and not visto[y2, x2]: visto[y2, x2] = True; pila.append((y2, x2))
    pr(f'| {dp} dp | {comp} | {mejor} px de {dp} ({mejor / dp * 100:.0f} % del ancho) | {0.21 / 2 * dp:.1f} dp |')
pr()
pr('## 3. Las barras del medidor de señal')
pr()
e = recorte('medidor_encendida'); o = recorte('medidor_apagada')
ce = e[..., :3][(e[..., 3] >= 250) & (lum(e[..., :3]) > 0.05)].mean(0); co = o[..., :3][(o[..., 3] >= 250) & (lum(o[..., :3]) > 0.01)].mean(0)
pr(f'Encendida {hexs(ce)} y apagada {hexs(co)}: se distinguen {contraste(ce, co):.1f}:1 (por luminosidad, no solo por color); la apagada contra el cielo {contraste(co, hexrgb(SKY)):.1f}:1, la encendida {contraste(ce, hexrgb(SKY)):.1f}:1.')
pr(f'Cada barra mide 34 u de ancho (la imagen, {e.shape[1]} px con la sombra) y 50 a 154 u de alto; los extremos redondos no se estiran (nine-slice vertical).')
open(os.path.join(AQUI, 'out', 'disparate_medidas.md'), 'w', encoding='utf-8').write('\n'.join(out) + '\n')
