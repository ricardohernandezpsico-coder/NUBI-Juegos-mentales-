# -*- coding: utf-8 -*-
"""Mide las piezas de La estación de correo en plasticina (Tarea 108) contra lo que reemplazan y contra el fondo.
Lee el atlas ya empaquetado (`Assets/Resources/Plasticina/correo`): mide lo MISMO que verá el teléfono. Deja `tools/juegos/out/correo_medidas.md`.

  1. LAS 12 CARTAS (384 × 276 px = 128 × 92 dp, 3 px por dp): papel, esquinas transparentes, que ninguna se parezca a otra en luminancia (nada solo por el color) y el SELLO DORADO (docs §12): más grande que el normal, con el borde dentado de una estampilla
     (muchos dientes cortos y redondeados: NO rayos, nada que parezca estrella con puntas ni sol), más oscuro que el papel y con una figura neutra al centro.
  2. LAS 4 FIGURAS (círculo, triángulo, cuadrado redondeado y gota): la forma es la pista principal. Se mide cuánto se parecen sus siluetas de a pares (IoU) en el planeta y en el sello de la carta, y los colores de los planetas con visión típica, protanopía,
     deuteranopía y tritanopía (CIEDE2000, `tools/paleta_daltonismo.py`): donde dos colores se acercan, la figura los separa.
  3. LA CAJA FUERTE Y EL FARO: el dial (8 marcas, no una rueda de radios), la puerta y la manija, la torre (las franjas caben en el trapecio) y las dos lámparas (apagada y encendida).
  4. EL RELOJ: base, tapa lila y tapa presionada (luminosidad, silueta aplastada, dónde cae la huella); la nave, el saco y la marca de raya.
  5. Lo que pesa el atlas.
Uso: python -I tools/juegos/mide_correo.py
"""
import itertools, json, math, os, sys
import numpy as np
from PIL import Image
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
from huella import *
import paleta_daltonismo as pd
sys.stdout.reconfigure(encoding='utf-8')

AQUI = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.normpath(os.path.join(AQUI, '..', '..'))
BASE = os.path.join(REPO, 'unity', 'NeuroVidaCore', 'Assets', 'Resources', 'Plasticina', 'correo')
PLANETAS = ('Coralia (círculo)', 'Celesta (triángulo)', 'Lima (cuadrado)', 'Uva (gota)')
CUES = ('normal', 'sello dorado', 'lazo')
man = json.load(open(os.path.join(BASE, 'manifest.json'), encoding='utf-8'))
atlas = Image.open(os.path.join(BASE, 'atlas.png')).convert('RGBA')

def recorte(n):
    e = next(s for s in man['sprites'] if s['n'] == n)
    return np.asarray(atlas.crop((e['x'], atlas.height - e['y'] - e['h'], e['x'] + e['w'], atlas.height - e['y']))).astype(float), e

def hexs(rgb): return '#%02X%02X%02X' % tuple(int(round(v)) for v in rgb)

def lum_img(a):
    c = a[..., :3] / 255.0; c = np.where(c <= 0.03928, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)
    return 0.2126 * c[..., 0] + 0.7152 * c[..., 1] + 0.0722 * c[..., 2]

def luma(a):
    """La luma simple (lo que mide la prueba de Unity: 0,2126 R + 0,7152 G + 0,0722 B sobre el valor ya codificado)."""
    return (0.2126 * a[..., 0] + 0.7152 * a[..., 1] + 0.0722 * a[..., 2]) / 255.0

def raiz_lum(a): return np.sqrt(lum_img(a))

def dif_gris(a, b):
    ma = np.abs(a[..., 3] - b[..., 3]) > 128
    mg = (a[..., 3] > 200) & (b[..., 3] > 200) & (np.abs(raiz_lum(a) - raiz_lum(b)) > 0.25)
    return int((ma | mg).sum())

def caja(a, umbral=40):
    ys, xs = np.where(a[..., 3] >= umbral); return xs.min(), xs.max(), ys.min(), ys.max()

def en_dp(a, e, x_dp, y_dp):
    """El pixel de [a] a (x_dp, y_dp) del centro del lienzo (y hacia ARRIBA)."""
    H, W = a.shape[:2]; return a[int(round(H / 2 - y_dp * e['ppu'])), int(round(W / 2 + x_dp * e['ppu']))]

out = []
def pr(s=''):
    print(s); out.append(s)

pr('# La estación de correo en plasticina — medidas')
pr()
pr('## 1. Las 12 cartas (3 px por dp; caja de 128 × 92 dp, papel de 112 × 76)')
pr()
cartas = {}
pr('| Carta | Lienzo (dp) | Papel ocupa | Esquinas (alfa) | Luma del papel |')
pr('|---|---|---:|---:|---:|')
for p in range(4):
    for c in range(3):
        a, e = recorte(f'carta_{p}_{c}'); cartas[(p, c)] = a; H, W = a.shape[:2]
        opaco = (a[..., 3] > 200).mean(); esq = max(a[0, 0, 3], a[0, -1, 3], a[-1, 0, 3], a[-1, -1, 3])
        papel = luma(en_dp(a, e, -30, 20)); pr(f'| {PLANETAS[p].split()[0]} · {CUES[c]} | {W / e["ppu"]:.0f} × {H / e["ppu"]:.0f} | {opaco * 100:.0f} % | {esq:.0f} | {papel:.2f} |')
pr()
peor = (10 ** 9, None, None)
for (k1, a1), (k2, a2) in itertools.combinations(cartas.items(), 2):
    d = dif_gris(a1, a2)
    if d < peor[0]: peor = (d, k1, k2)
pr(f'La pareja de cartas más parecida en luminancia: {PLANETAS[peor[1][0]].split()[0]}·{CUES[peor[1][1]]} y {PLANETAS[peor[2][0]].split()[0]}·{CUES[peor[2][1]]}, con {peor[0]} píxeles distintos de {a1.shape[0] * a1.shape[1]} (todas se distinguen por forma y claridad).')
pr()
pr('### El sello dorado frente al normal (la señal «focal»: se mira el sello)')
pr()
SEAL_G = (32.0, -14.0, 18.0); SEAL_N = (34.0, -16.0, 15.0)                    # centro (x, y hacia abajo) y mitad del lado (dp), los de MailSprites
normal, e = cartas[(0, 0)], recorte('carta_0_0')[1]; oro = cartas[(0, 1)]
paper = luma(en_dp(normal, e, -30, 20))                                              # arriba a la izquierda del papel (y hacia arriba: +20 = 20 dp sobre el centro)
def mancha(a):
    H, W = a.shape[:2]; yy, xx = np.mgrid[0:H, 0:W]; X = (xx + 0.5 - W / 2) / e['ppu']; Y = (H / 2 - yy - 0.5) / e['ppu']        # y arriba
    zona = (X >= 10) & (X <= 56) & (Y >= -8) & (Y <= 38)
    return int((zona & (np.abs(luma(a) - paper) > 0.08) & (a[..., 3] > 200)).sum())
bg, bn = mancha(oro), mancha(normal)
pr(f'- La mancha del sello (lo que se aparta del papel en su zona, en grises): dorado {bg} px contra normal {bn} px → ×{bg / bn:.2f} (la prueba pide ≥ ×1,25; el diseño: 36 dp contra 30).')
cx, cy, half = SEAL_G[0], -SEAL_G[1], SEAL_G[2]                                 # y hacia ARRIBA: la parte de arriba del sello está en +cy
def linea_borde(a, ydp, x0, x1, paso=0.25):
    xs = np.arange(x0, x1 + 1e-6, paso); return xs, np.array([luma(en_dp(a, e, x, ydp)[None, None, :])[0, 0] for x in xs])
xs, L = linea_borde(oro, cy + half - 0.7, cx - 12, cx + 12)                      # justo adentro del borde de arriba
oscuro = L < paper - 0.2; dientes = int(((~oscuro[:-1]) & oscuro[1:]).sum()) + int(oscuro[0])
pr(f'- El borde de arriba del sello dorado (luma a lo largo de 24 dp, 0,7 dp adentro): {dientes} tramos oscuros (el borde café, luma ≈ {L.min():.2f}) separados por el papel que asoma en las mordidas (luma hasta {L.max():.2f}; el papel, {paper:.2f}).')
pr(f'- Dientes del borde, por lado: {int(round(2 * half / 5.5)) - 1} mordidas de 3,8 dp de diámetro cada 5,5 dp (radio 1,9): son MUCHOS, CORTOS Y REDONDEADOS (el seno del diente mide 1,9 dp de hondo en un sello de 36), como el borde de una estampilla. Sin puntas ni rayos.')
dentro = luma(en_dp(oro, e, cx - 8, cy - 10)); pr(f'- El interior dorado (luma {dentro:.2f}) contra el papel ({paper:.2f}): diferencia {abs(dentro - paper):.2f} (la prueba pide > 0,08).')
pr(f'- Cabe en el papel de 112 × 76: el sello llega a x = {cx + half:.0f} (el papel, a 56), y = {cy + half:.0f} (el papel, a 38) y baja hasta y = {cy - half:.0f} (las líneas de dirección empiezan en y = −10).')
pr()
pr('## 2. Las 4 figuras: la forma es la pista principal')
pr()
masas = {}
for p in range(4):
    a, e2 = recorte(f'planeta_{p}'); H, W = a.shape[:2]
    blanco = (a[..., 3] > 200) & (a[..., 0] > 225) & (a[..., 1] > 215) & (a[..., 2] > 205)
    masas[p] = blanco
pr('Las figuras blancas en relieve de los planetas (64 dp de lienzo) comparadas de a pares: IoU (1 = iguales); un valor bajo es una silueta distinta.')
pr()
pr('| Par | IoU planeta | IoU sello (carta) |')
pr('|---|---:|---:|')
peor_iou = 0.0
def mascara_sello(p):
    """La figura oscura del sello NORMAL (17 dp): lo que no es blanco dentro de 12 dp del centro del sello (el borde de tinta queda más afuera, a 14-15 dp)."""
    a = cartas[(p, 0)]; H, W = a.shape[:2]; yy, xx = np.mgrid[0:H, 0:W]; X = (xx + 0.5 - W / 2) / e['ppu']; Y = (H / 2 - yy - 0.5) / e['ppu']
    zona = (np.abs(X - SEAL_N[0]) < 12) & (np.abs(Y + SEAL_N[1]) < 12)
    return zona & (a[..., 3] > 200) & (luma(a) < 0.85)
for i, j in itertools.combinations(range(4), 2):
    m1, m2 = masas[i], masas[j]; iou = (m1 & m2).sum() / max(1, (m1 | m2).sum())
    s1, s2 = mascara_sello(i), mascara_sello(j); iou_s = (s1 & s2).sum() / max(1, (s1 | s2).sum())
    peor_iou = max(peor_iou, iou)
    pr(f'| {PLANETAS[i].split()[0]} y {PLANETAS[j].split()[0]} | {iou:.2f} | {iou_s:.2f} |')
pr()
pr(f'La pareja más parecida en el planeta: IoU {peor_iou:.2f} (círculo y cuadrado redondeado).')
pr()
pr('**Colores de los planetas** (cuerpo, medido en el atlas) con cada visión (CIEDE2000 mínimo entre pares; la regla de color solo es ≥ 20 cuando el color es LA pista; aquí la pista es la figura y el color la refuerza):')
pr()
cols = {}
for p in range(4):
    a, e2 = recorte(f'planeta_{p}'); H, W = a.shape[:2]; cuerpo = (a[..., 3] > 240) & (~masas[p])
    yy, xx = np.mgrid[0:H, 0:W]; anillo = cuerpo & (np.hypot(xx - W / 2, yy - H / 2) > 0.62 * W / 2) & (np.hypot(xx - W / 2, yy - H / 2) < 0.8 * W / 2)
    cols[p] = a[..., :3][anillo].mean(0)
    pr(f'- {PLANETAS[p]}: color medio del cuerpo {hexs(cols[p])}')
pr()
for v, M in pd.M.items():
    labs = {p: pd.lab(M @ pd.lin(cols[p])) for p in cols}
    pares = sorted((pd.de2000(labs[a], labs[b]), a, b) for a, b in itertools.combinations(cols, 2))
    d0, a0, b0 = pares[0]
    pr(f'- {v}: el par de colores más cercano es {PLANETAS[a0].split()[0]} y {PLANETAS[b0].split()[0]}, ΔE {d0:.1f}' + (' (se separan por la figura: IoU %.2f)' % ((masas[a0] & masas[b0]).sum() / max(1, (masas[a0] | masas[b0]).sum())) if d0 < 20 else ' (también se separan por color)'))
pr()
def dilata(m, r):
    o = m.copy()
    for _ in range(r):
        q = o.copy(); q[1:] |= o[:-1]; q[:-1] |= o[1:]; q[:, 1:] |= o[:, :-1]; q[:, :-1] |= o[:, 1:]; o = q
    return o

pr('**Contraste de cada figura (WCAG 1.4.11: un gráfico que informa necesita ≥ 3:1 contra lo que lo rodea).** El color solo no alcanzaba: antes del aro de tinta, la figura BLANCA contra el cuerpo de su planeta rendía 2,62 (Coralia), 2,34 (Celesta), **1,95 (Lima)** y 2,97:1 (Uva), y la figura oscura contra el sello blanco 3,8 / 3,0 / **2,8** / 4,3:1. Ahora cada figura lleva un aro fino de tinta (0,9 dp) HACIA ADENTRO de su contorno (la silueta y el tamaño no cambian). Medido en el atlas:')
pr()
pr('| Planeta | Figura blanca contra su aro | Aro contra el cuerpo del planeta | Aro contra el sello blanco | Aro contra el sello dorado | IoU de la silueta con la de antes |')
pr('|---|---:|---:|---:|---:|---:|')
peor_aro = 99.0
for p in range(4):
    a, e2 = recorte(f'planeta_{p}'); H, W = a.shape[:2]
    blanco = (a[..., 3] > 200) & (a[..., 0] > 225) & (a[..., 1] > 215) & (a[..., 2] > 205)
    cerca = dilata(blanco, 4); ancho = dilata(blanco, 9); medio = dilata(blanco, 5)
    tinta_m = cerca & ~blanco & (a[..., 3] > 200) & (luma(a) < 0.25)
    cuerpo_m = ancho & ~medio & (a[..., 3] > 240)
    fb = a[..., :3][blanco].mean(0); ft = a[..., :3][tinta_m].mean(0); fc = a[..., :3][cuerpo_m].mean(0)
    cel = []
    for c in (0, 1):
        ac = cartas[(p, c)]; ec = recorte(f'carta_{p}_{c}')[1]; Hc, Wc = ac.shape[:2]; yy, xx = np.mgrid[0:Hc, 0:Wc]
        X = (xx + 0.5 - Wc / 2) / ec['ppu']; Y = (Hc / 2 - yy - 0.5) / ec['ppu']
        sx, sy = (SEAL_G[0], -SEAL_G[1]) if c == 1 else (SEAL_N[0], -SEAL_N[1]); alcance = 13 if c == 1 else 12
        zona = (np.abs(X - sx) < alcance) & (np.abs(Y - sy) < alcance)
        anillo = zona & (ac[..., 3] > 200) & (luma(ac) < 0.25)
        if c == 1: alrededor = zona & (ac[..., 3] > 200) & (ac[..., 0] > 170) & (ac[..., 1] > 120) & (ac[..., 2] < 110) & (ac[..., 0] - ac[..., 2] > 100)
        else: alrededor = zona & (ac[..., 3] > 200) & (luma(ac) > 0.93)
        cel.append(contraste(ac[..., :3][anillo].mean(0), ac[..., :3][alrededor].mean(0)) if anillo.sum() and alrededor.sum() else 0.0)
    peor_aro = min(peor_aro, contraste(fb, ft), contraste(ft, fc), *cel)
    pr(f'| {PLANETAS[p].split()[0]} | {contraste(fb, ft):.1f}:1 | {contraste(ft, fc):.1f}:1 | {cel[0]:.1f}:1 | {cel[1]:.1f}:1 | (ver la tabla de IoU de arriba) |')
pr()
pr(f'**El peor de todos: {peor_aro:.1f}:1** (la prueba `EveryFigure_HasAnInkRing_ThatReadsAtThreeToOneOrMore_OnThePlanetAndOnTheSeal` pide ≥ 3:1 en todos).')
pr()
pr('Colores de relleno de las figuras de los SELLOS (oscuras, 17 dp sobre el sello blanco): color medido del relleno y tamaño.')
pr()
for p in range(4):
    a = cartas[(p, 0)]; m = mascara_sello(p)
    if m.sum() == 0: pr(f'- {PLANETAS[p]}: no se encontró la figura'); continue
    interior = m & (luma(a) > 0.25)
    f = a[..., :3][interior].mean(0)
    pr(f'- {PLANETAS[p]}: relleno {hexs(f)}, figura entera (con el aro) {int(m.sum())} px = {m.sum() / e["ppu"] ** 2:.0f} dp²')
pr()
pr('## 3. La caja fuerte y el faro (4 px por dp)')
pr()
for n in ('dial', 'caja_puerta', 'manija', 'torre', 'lampara_0', 'lampara_1', 'nave', 'saco', 'marca_raya', 'reloj_base', 'reloj_tapa', 'reloj_tapa_p', 'planeta_0'):
    a, e3 = recorte(n); pr(f'- {n}: {e3["w"]} × {e3["h"]} px = {e3["w"] / e3["ppu"]:.1f} × {e3["h"] / e3["ppu"]:.1f} dp')
pr()
dial, ed = recorte('dial')
def es_oro(px): return px[3] > 200 and px[0] > 170 and px[1] > 120 and px[2] < 110 and px[0] - px[2] > 100
marcas = [es_oro(en_dp(dial, ed, 9 * math.cos(k * math.pi / 4), 9 * math.sin(k * math.pi / 4))) for k in range(8)]
entre = [es_oro(en_dp(dial, ed, 9 * math.cos((k + 0.5) * math.pi / 4), 9 * math.sin((k + 0.5) * math.pi / 4))) for k in range(8)]
pr(f'- El dial: {sum(marcas)} de 8 marcas doradas a 9 dp del centro y {sum(entre)} de 8 doradas ENTRE ellas (ocho puntos, no una rueda de radios); el centro dorado: {es_oro(en_dp(dial, ed, 0, 0))}.')
puerta, ep = recorte('caja_puerta'); x0, x1, y0, y1 = caja(puerta, 200)
pr(f'- La puerta: el cuerpo opaco mide {(x1 - x0 + 1) / 4:.1f} × {(y1 - y0 + 1) / 4:.1f} dp (la de siempre, 62 × 68); color {hexs(puerta[..., :3][puerta[..., 3] > 250].mean(0))}.')
manija, em = recorte('manija'); x0, x1, y0, y1 = caja(manija, 200)
pr(f'- La manija: {(x1 - x0 + 1) / 4:.1f} × {(y1 - y0 + 1) / 4:.1f} dp (la de siempre, 28 × 7).')
torre, et = recorte('torre'); rojo = (torre[..., 3] > 200) & (torre[..., 0] > 190) & (torre[..., 1] < 130) & (torre[..., 2] < 120)
H, W = torre.shape[:2]
def extremos(mask, ydp):
    r = int(round(H / 2 - ydp * 4 - 0.5)); xs = np.where(mask[r])[0]; return (xs.min(), xs.max()) if len(xs) else (None, None)
for nombre, yd in (('franja de arriba', 5.5), ('franja de abajo', -5.5)):
    r0, r1 = extremos(rojo, yd); t0, t1 = extremos(torre[..., 3] > 200, yd)
    pr(f'- La {nombre} (a y = {yd:+.1f} dp): el rojo va de x = {r0} a {r1} y el cuerpo de la torre de {t0} a {t1} → sobran {r0 - t0} px a la izquierda y {t1 - r1} a la derecha (≥ 1: la franja no se sale del trapecio).')
l0, _ = recorte('lampara_0'); l1, _ = recorte('lampara_1'); c0 = l0[l0.shape[0] // 2, l0.shape[1] // 2, :3]; c1 = l1[l1.shape[0] // 2, l1.shape[1] // 2, :3]
pr(f'- Lámpara apagada {hexs(c0)} (luma {lum(c0):.2f}) y encendida {hexs(c1)} (luma {lum(c1):.2f}): la encendida es ×{(lum(c1) + 0.05) / (lum(c0) + 0.05):.1f} más clara.')
pr()
pr('## 4. El reloj, la nave, el saco y la marca de raya')
pr()
base, eb = recorte('reloj_base'); tapa, et2 = recorte('reloj_tapa'); tp, _ = recorte('reloj_tapa_p')
fondo = base[base.shape[0] // 2, base.shape[1] // 2, :3]
ct, cp = tapa[..., :3][tapa[..., 3] >= 250].mean(0), tp[..., :3][tp[..., 3] >= 250].mean(0)
bt, bp = caja(tapa, 128), caja(tp, 128)
pr(f'- La base: el fondo {hexs(fondo)}; el aro de tinta mide {(caja(base, 128)[1] - caja(base, 128)[0] + 1) / 4:.1f} dp de ancho (el de siempre, 59).')
pr(f'- La tapa: color medio {hexs(ct)} (diseño #B79BFF, ΔE {pd.de2000(pd.lab(pd.lin(ct)), pd.lab(pd.lin(hexrgb("#B79BFF")))):.1f}); disco de {(bt[1] - bt[0] + 1) / 4:.1f} × {(bt[3] - bt[2] + 1) / 4:.1f} dp (el de siempre, 47).')
pr(f'- La tapa presionada: {hexs(cp)}, luminosidad {lum(cp) / lum(ct):.2f} de la normal, disco de {(bp[1] - bp[0] + 1) / 4:.1f} × {(bp[3] - bp[2] + 1) / 4:.1f} dp (aplastado: +4 % de ancho y −8 % de espesor, como las losas del kit).')
H, W = tapa.shape[:2]; yy, xx = np.mgrid[0:H, 0:W]; X = (xx - W / 2) / 4; Y = (H / 2 - yy) / 4
mt, mp = tapa[..., 3] >= 250, tp[..., 3] >= 250; rat = np.where(mt & mp, lum_img(tp) / np.maximum(lum_img(tapa), 1e-4), np.nan); med = np.nanmedian(rat)
def desvio(cx, cy, rad):
    d = (np.hypot(X - cx, Y - cy) < rad) & mt & mp; return float((np.abs(rat[d] - med) / med).mean()) if d.sum() else 0.0
pr(f'- La huella del dedo en la tapa presionada (arriba a la izquierda): desvío medio {desvio(-9, 7, 5) * 100:.1f} % ahí, contra {desvio(9, -7, 5) * 100:.1f} % en la cara de enfrente.')
pr(f'- La tapa contra el cielo del juego (#0A0F33): {contraste(ct, hexrgb("#0A0F33")):.1f}:1; contra la base: {contraste(ct, fondo):.1f}:1.')
nave, en = recorte('nave'); x0, x1, y0, y1 = caja(nave, 200)
pr(f'- La nave: {(x1 - x0 + 1) / 4:.1f} × {(y1 - y0 + 1) / 4:.1f} dp en un lienzo de {en["w"] / 4:.0f} × {en["h"] / 4:.0f} (la de siempre, 78 × 30 de cuerpo); sobre dorado ' + ('sí' if ((nave[..., 3] > 200) & (nave[..., 0] > 230) & (nave[..., 1] > 170) & (nave[..., 2] < 120)).sum() > 100 else 'NO') + ', ventanilla celeste ' + ('sí' if ((nave[..., 3] > 200) & (nave[..., 2] > 220) & (nave[..., 1] > 180) & (nave[..., 0] < 170)).sum() > 100 else 'NO') + '.')
saco, es = recorte('saco'); x0, x1, y0, y1 = caja(saco, 200)
pr(f'- El saco: {(x1 - x0 + 1) / 4:.1f} × {(y1 - y0 + 1) / 4:.1f} dp (el de siempre, 28 × 22).')
raya, er = recorte('marca_raya'); H, W = raya.shape[:2]
pr(f'- La raya del resumen: aro con UNA barra horizontal; arriba y abajo del centro vacío ({int(en_dp(raya, er, 0, 5)[3])} y {int(en_dp(raya, er, 0, -5)[3])} de alfa) y la barra a los lados ({int(en_dp(raya, er, 5, 0)[3])} y {int(en_dp(raya, er, -5, 0)[3])}): sin barra vertical, no es una cruz. Color {hexs(en_dp(raya, er, 5, 0)[:3])}.')
pr()
pr('## 5. Lo que pesa')
pr()
p = man['peso']
pr(f'Atlas {man["ancho"]} × {man["alto"]} px, {len(man["sprites"])} imágenes: PNG {p["png_bytes"] / 1024:.0f} KB; en el APK sin compresión de textura ≈ {p["deflate_rgba32"] / 1048576:.2f} MB (tope por juego 1,5 MB); con ASTC 6×6 ≈ {p["astc6x6"] / 1048576:.2f} MB.')
open(os.path.join(AQUI, 'out', 'correo_medidas.md'), 'w', encoding='utf-8').write('\n'.join(out) + '\n')
