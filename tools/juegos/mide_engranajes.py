# -*- coding: utf-8 -*-
"""Mide Engranajes «Taller de reparación» en plasticina (Tarea 112) contra lo que reemplaza. Lee el atlas ya empaquetado (`Assets/Resources/Plasticina/engranajes`) y el dibujo de ANTES (los sprites procedurales que deja la prueba
`TheProceduralDrawing_IsSavedForTheMeasureScript` en `unity/test-results/engranajes-antes/*.png`; sin esa carpeta se mide solo lo de después). Deja `tools/juegos/out/engranajes_medidas.md`.

  1. LOS DIENTES SON LA MECÁNICA: ancho de la punta del diente y del hueco, a 1 y a 3 dp de la punta, el trapecio exacto (punta ±0,17 · raíz ±0,32 del paso), el de antes y el de después (pide ±0,3 dp).
  2. EL ENGRANE: el par grande-chico en reposo (MeshPhase) con holgura cero: cuánto se pisan y cuánto hueco se ve, antes y después, y mal encajado.
  3. LO QUE GIRA: los rayos (5 y 3) y su hondura de luminosidad, la asimetría (espejo y 180°) que muestra el sentido, la luz de las aspas de la turbina y del radar (sin una luz que gire), y el punto coral del radar.
  4. LA TURBINA de 5 aspas (no una cruz «+»), el cruce de la correa y el aspa del cartel.
  5. LAS FLECHAS de giro: espejo, y en escala de grises a tamaño real cuántos píxeles de la cabeza cambian de lado.
  6. EL ESTADO PRESIONADO (el motor y la polea), y lo que pesa el atlas.
Uso: python -I tools/juegos/mide_engranajes.py [--antes carpeta]
"""
import json, math, os, sys
import numpy as np
from PIL import Image
sys.stdout.reconfigure(encoding='utf-8')

AQUI = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.normpath(os.path.join(AQUI, '..', '..'))
BASE = os.path.join(REPO, 'unity', 'NeuroVidaCore', 'Assets', 'Resources', 'Plasticina', 'engranajes')
args = sys.argv[1:]
def opt(n, d): return args[args.index(n) + 1] if n in args else d
ANTES = opt('--antes', os.path.join(REPO, 'unity', 'test-results', 'engranajes-antes'))
man = json.load(open(os.path.join(BASE, 'manifest.json'), encoding='utf-8'))
atlas = Image.open(os.path.join(BASE, 'atlas.png')).convert('RGBA')

def recorte(n):
    e = next(s for s in man['sprites'] if s['n'] == n)
    return np.asarray(atlas.crop((e['x'], atlas.height - e['y'] - e['h'], e['x'] + e['w'], atlas.height - e['y']))).astype(float), e['ppu']

def antes(n):
    ruta = os.path.join(ANTES, n + '.png')
    if not os.path.exists(ruta): return None
    ppu = float(open(os.path.join(ANTES, n + '.ppu')).read()) if os.path.exists(os.path.join(ANTES, n + '.ppu')) else 3.5
    return np.asarray(Image.open(ruta).convert('RGBA')).astype(float), ppu

def luma(a): return (0.2126 * a[..., 0] + 0.7152 * a[..., 1] + 0.0722 * a[..., 2]) / 255.0

def sil(a): return a[..., 3] / 255.0                                                              # la silueta (el alfa), con la tinta de antes si la trae
def relleno(a):                                                                                   # el relleno CLARO: el alfa por una rampa de luz (la tinta oscura de antes vale 0): lo mismo que EngranajesPlasticTests.FillValue
    return a[..., 3] / 255.0 * np.clip((luma(a) - 0.12) / 0.2, 0.0, 1.0)

def campo(m, ppu, x, y):
    """La máscara [m] (alto × ancho, fila 0 arriba) interpolada en (x, y) dp del centro (y hacia ARRIBA), con precisión de sub-píxel."""
    H, W = m.shape
    fx = W / 2.0 + np.asarray(x) * ppu - 0.5; fy = H / 2.0 - np.asarray(y) * ppu - 0.5
    x0 = np.floor(fx).astype(int); y0 = np.floor(fy).astype(int); tx = fx - x0; ty = fy - y0
    def g(xx, yy): return m[np.clip(yy, 0, H - 1), np.clip(xx, 0, W - 1)]
    return (g(x0, y0) * (1 - tx) + g(x0 + 1, y0) * tx) * (1 - ty) + (g(x0, y0 + 1) * (1 - tx) + g(x0 + 1, y0 + 1) * tx) * ty

def diente_hueco(a, ppu, n, r, campo_fn=None):
    p = 2 * math.pi / n; t = np.linspace(-p / 2, p / 2, 4001)
    on = campo((campo_fn or sil)(a), ppu, np.cos(t) * r, np.sin(t) * r) >= 0.5
    if not on.any(): return None, None
    lo, hi = t[on].min(), t[on].max()
    return (hi - lo) * r, (p - (hi - lo)) * r

def analitico(n, tip, r):
    p = 2 * math.pi / n; root = tip - 6.0
    A = np.array([math.cos(0.17 * p) * tip, math.sin(0.17 * p) * tip]); B = np.array([math.cos(0.32 * p) * root, math.sin(0.32 * p) * root])
    lo, hi = 0.0, 1.0
    for _ in range(50):
        mid = (lo + hi) / 2; q = A + (B - A) * mid
        if np.hypot(*q) > r: lo = mid
        else: hi = mid
    q = A + (B - A) * (lo + hi) / 2; half = math.atan2(q[1], q[0])
    return 2 * half * r, (p - 2 * half) * r

out = []
def pr(s=''):
    print(s); out.append(s)

TIP = {0: 33.0, 1: 23.0}; TEETH = {0: 15, 1: 10}; NOMBRE = {0: 'grande (15 dientes)', 1: 'chico (10 dientes)'}
tiene_antes = os.path.isdir(ANTES)

pr('# Engranajes «Taller de reparación» en plasticina — medidas')
pr()
pr('## 1. Los dientes son la mecánica: el trapecio exacto, y el ancho de la punta y del hueco de antes y de después')
pr()
pr('Se recorre el círculo de radio r entre −medio paso y +medio paso del diente 0 (a la derecha) y se mide el arco de la SILUETA que rodea al diente y el del hueco, con el alfa interpolado (sub-píxel). «Esperado» = el trapecio exacto de `GearPolygon` (punta ±0,17 y raíz ±0,32 del paso angular). '
   'El engranaje de ANTES lleva un borde de tinta de 2,4 dp CENTRADO en ese contorno (1,2 dp hacia adentro y 1,2 hacia afuera): su contorno es la silueta con tinta menos 2,4 dp (× la inclinación del flanco); lo que se ve de él es solo el relleno claro (la tinta casi no se distingue del fondo). Tolerancia pedida: ±0,3 dp.')
pr()
pr('| Engranaje | r (dp) | Diente esperado | Diente antes (contorno) | Diente después | Δ | Hueco esperado | Hueco antes (contorno) | Hueco después | Δ | Hueco que se veía antes (relleno claro) |')
pr('|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|')
peor_d = peor_h = peor_e = 0.0
for size in (0, 1):
    despues, ppu = recorte(f'gear_{size}_0'); ant = antes(f'gear_{size}_0')
    root = TIP[size] - 6.0; tang = root * 0.15 * (2 * math.pi / TEETH[size]); inclina = math.sqrt(1 + (tang / 6.0) ** 2)
    for r in (TIP[size] - 0.5, TIP[size] - 3.0, TIP[size] - 4.5, TIP[size] - 5.5):                    # junto a la punta (ancho de la punta del diente), a media altura, cerca de la raíz y pegado a ella (a 0,5 dp: la tinta de antes llena el hueco, sin contorno que comparar)
        ed, eh = analitico(TEETH[size], TIP[size], r); dd, dh = diente_hueco(despues, ppu, TEETH[size], r)
        peor_e = max(peor_e, abs(dd - ed), abs(dh - eh))
        if ant:
            sd, sh = diente_hueco(ant[0], ant[1], TEETH[size], r); ad, ah = sd - 2 * 1.2 * inclina, sh + 2 * 1.2 * inclina
            if r > TIP[size] - 5.0: peor_d = max(peor_d, abs(dd - ad)); peor_h = max(peor_h, abs(dh - ah))
            else: ad = ah = None
            vd, vh = diente_hueco(ant[0], ant[1], TEETH[size], r, relleno) if r < TIP[size] - 1.5 else (None, None)
        else: ad = ah = vh = None
        f = lambda v: f'{v:.2f}' if v is not None else '—'
        pr(f'| {NOMBRE[size]} | {r:.1f} | {ed:.2f} | {f(ad)} | {dd:.2f} | ' + (f'{dd - ad:+.2f}' if ad is not None else '—') + f' | {eh:.2f} | {f(ah)} | {dh:.2f} | ' + (f'{dh - ah:+.2f}' if ah is not None else '—') + f' | {f(vh)} |')
pr()
pr(f'**Contra el trapecio exacto, la mayor diferencia es {peor_e:.2f} dp** (el bisel solo redondea los cantos de arriba y de abajo; las esquinas verticales de los dientes quedan vivas: con el límite de 40° de `pieza` redondeaban la punta y se perdían 0,3 dp de ancho). ' + (f'**Contra el contorno de antes: {peor_d:.2f} dp en el diente y {peor_h:.2f} dp en el hueco** (se pide ±0,3).' if tiene_antes else ''))
pr()
pr('Lo que se ve cambia en una cosa, y a favor de la mecánica: el engranaje de antes mostraba el relleno claro 1,2 dp por DENTRO del contorno (los dientes se veían más finos y el hueco, 2,5 dp más ancho: última columna); el de plasticina llena hasta el contorno exacto, así que el hueco que se ve es el hueco real.')
pr()
# los huecos transparentes: el disco oscuro de apoyo se ve a través (como hoy)
for size in (0, 1):
    a, ppu = recorte(f'gear_{size}_0'); p = 2 * math.pi / TEETH[size]
    al = [campo(a[..., 3] / 255.0, ppu, math.cos(k * p + p / 2) * (TIP[size] - 1.5), math.sin(k * p + p / 2) * (TIP[size] - 1.5)) for k in range(TEETH[size])]
    pr(f'- {NOMBRE[size]}: alfa en el medio de cada hueco (a 1,5 dp de la punta): máximo {max(al):.3f} (0 = transparente: se ve el disco oscuro de atrás, como hoy).')
pr()

# ------------------------------------------------------------------ 2. el engrane en reposo
pr('## 2. El engrane del par grande-chico en reposo (holgura cero)')
pr()
pr('Los dos engranajes a 50 dp con las fases de `MeshPhase` (cada diente de uno frente al hueco del otro), en la zona de contacto (x de 14 a 36 dp, y de −9 a 9) con una rejilla de 0,25 dp: **se pisan** = dp² con relleno de los DOS; **hueco que se ve** = dp² sin relleno de ninguno (se ve el disco oscuro de atrás). «Rellenos»: lo que se ve de hoy (el relleno claro); «siluetas»: con la tinta.')
pr()
def rot_val(m, ppu, x, ydown, ang):
    c, s_ = math.cos(ang), math.sin(ang)
    return campo(m, ppu, x * c + ydown * s_, x * s_ - ydown * c) >= 0.5
def mesh(a_img, b_img, fn, ang_b_extra=0.0):
    (ia, pa), (ib, pb) = a_img, b_img; ma, mb = fn(ia), fn(ib)
    A_ = 0.37; th = 0.0; pi_ = 2 * math.pi / 15; pj = 2 * math.pi / 10
    fi = (((th - A_) / pi_) % 1.0 + 1.0) % 1.0; fj = ((0.5 - fi) % 1.0 + 1.0) % 1.0; B_ = th + math.pi - fj * pj
    xs, ys = np.meshgrid(np.arange(14.0, 36.0001, 0.25), np.arange(-9.0, 9.0001, 0.25))
    fa = rot_val(ma, pa, xs, ys, A_); fb = rot_val(mb, pb, xs - 50.0, ys, B_ + ang_b_extra)
    cell = 0.0625
    return (fa & fb).sum() * cell, (~fa & ~fb).sum() * cell
nA, nB = recorte('gear_0_0'), recorte('gear_1_2'); oA, oB = antes('gear_0_0'), antes('gear_1_2')
d_p, d_h = mesh(nA, nB, sil); d_c, _ = mesh(nA, nB, sil, math.pi / 10)
pr('| | Se pisan (dp²) | Hueco que se ve (dp²) |')
pr('|---|---:|---:|')
if oA and oB:
    a_p, a_h = mesh(oA, oB, relleno); t_p, t_h = mesh(oA, oB, sil)
    pr(f'| Antes: rellenos claros (lo que se veía) | {a_p:.2f} | {a_h:.1f} |')
    pr(f'| Antes: siluetas con tinta | {t_p:.1f} | {t_h:.1f} |')
pr(f'| Después (plasticina), en reposo | {d_p:.2f} | {d_h:.1f} |')
pr(f'| Después, mal encajado (medio paso) | {d_c:.2f} | — |')
pr()

# ------------------------------------------------------------------ 3. lo que gira
pr('## 3. Lo que gira: los rayos, el sentido y ninguna luz que gire con la pieza')
pr()
def luma_alrededor(a, ppu, r, n=720):
    t = 2 * math.pi * np.arange(n) / n; H, W = a.shape[:2]
    px = np.clip(np.round(W / 2 + np.cos(t) * r * ppu - 0.5).astype(int), 0, W - 1); py = np.clip(np.round(H / 2 - np.sin(t) * r * ppu - 0.5).astype(int), 0, H - 1)
    return luma(a)[py, px]
def hondonadas(v, ventana, hondura):
    """Cuántas hondonadas (tramos continuos de la luminosidad circular por debajo del promedio de ±[ventana] muestras en más de [hondura]) hay, y la más honda."""
    n = len(v); alta = np.array([v[i] - np.mean([v[(i + k) % n] for k in range(-ventana, ventana + 1)]) for i in range(n)])
    bajo = alta < -hondura
    if bajo.all() or not bajo.any(): return 0, 0.0
    ini = int(np.argmin(bajo)); cuenta = 0; k = 0
    while k < n:
        if bajo[(ini + k) % n]:
            cuenta += 1
            while k < n and bajo[(ini + k) % n]: k += 1
        else: k += 1
    return cuenta, float(alta.min())
pr('| Engranaje | Paleta | Rayos contados | Hondura del rayo (luminosidad) | Rayos de antes | Hondura antes | Espejo (dif. media) | Giro de 180° (dif. media) |')
pr('|---|---|---:|---:|---:|---:|---:|---:|')
for size in (0, 1):
    root = TIP[size] - 6.0; rayos = 5 if size == 0 else 3
    for pal, nom in ((0, 'motor (celeste)'), (1, 'camino (madera)'), (2, 'pieza (uva)')):
        a, ppu = recorte(f'gear_{size}_{pal}'); c, h = hondonadas(luma_alrededor(a, ppu, root * 0.5), 720 // rayos // 3, 0.035)
        ant = antes(f'gear_{size}_{pal}'); ca = ha = None
        if ant: ca, ha = hondonadas(luma_alrededor(ant[0], ant[1], root * 0.5), 720 // rayos // 3, 0.02)
        gx, gy = np.meshgrid(np.arange(-root, root, 0.5), np.arange(-root, root, 0.5)); dentro = gx ** 2 + gy ** 2 <= root * root * 0.9
        la = luma(a); Hh, Ww = la.shape
        def L(x, y): return la[np.clip(np.round(Hh / 2 - y * ppu - 0.5).astype(int), 0, Hh - 1), np.clip(np.round(Ww / 2 + x * ppu - 0.5).astype(int), 0, Ww - 1)]
        base = L(gx, gy); esp = np.abs(base - L(-gx, gy))[dentro].mean(); giro = np.abs(base - L(-gx, -gy))[dentro].mean()
        pr(f'| {NOMBRE[size]} | {nom} | {c} | {h:.3f} | ' + (f'{ca}' if ca is not None else '—') + ' | ' + (f'{ha:.3f}' if ha is not None else '—') + f' | {esp:.3f} | {giro:.3f} |')
pr()
pr('(Los rayos se cuentan a la mitad de la raíz como hondonadas de luminosidad respecto del entorno cercano; la prueba pide exactamente 5 y 3 y una hondura de al menos 0,035, y que el espejo y el giro de 180° difieran más de 0,02: el engranaje NO es simétrico, así que se lee su sentido.)')
pr()
# la turbina y el radar
def aspas(a, ppu, r, n=1440):
    t = 2 * math.pi * np.arange(n) / n; H, W = a.shape[:2]
    px = np.clip(np.round(W / 2 + np.cos(t) * r * ppu - 0.5).astype(int), 0, W - 1); py = np.clip(np.round(H / 2 - np.sin(t) * r * ppu - 0.5).astype(int), 0, H - 1)
    pix = a[py, px]; oro = (pix[:, 3] > 200) & (pix[:, 0] > 215) & (pix[:, 1] > 150) & (pix[:, 1] < 235) & (pix[:, 2] < 150)
    if oro.all() or not oro.any(): return [], []
    ini = int(np.argmin(oro)); ang = []; lum = []; k = 0
    while k < n:
        i = (ini + k) % n
        if not oro[i]: k += 1; continue
        j = k; ls = []
        while j < n and oro[(ini + j) % n]: ls.append(luma(pix[(ini + j) % n][None, None, :])[0, 0]); j += 1
        ang.append(((ini + k + (j - k - 1) / 2.0) % n) * 360.0 / n); lum.append(float(np.mean(ls))); k = j
    return ang, lum
fan, ppu_f = recorte('turbina'); an, lu = aspas(fan, ppu_f, 12.0); an = sorted(an)
pr(f'- **Turbina**: {len(an)} aspas a 12 dp del centro, en {", ".join(f"{v:.0f}°" for v in an)}; pasos de ' + ', '.join(f'{(an[(i + 1) % len(an)] - an[i]) % 360:.0f}°' for i in range(len(an))) + f'; la luz de cada aspa varía {max(lu) - min(lu):.3f} (luminosidad; ninguna luz gira con la pieza: se pide < 0,05).')
pr('- La turbina de ANTES tenía CUATRO aspas a 90° (una cruz «+»); el dibujo procedural de respaldo ya tiene cinco (la prueba cuenta cinco con y sin atlas).')
radar, ppu_r = recorte('radar')
coral = (radar[..., 3] > 200) & (radar[..., 0] > 200) & (radar[..., 1] > 90) & (radar[..., 1] < 190) & (radar[..., 2] < 150) & (radar[..., 0] > radar[..., 1] + 40)
ys_, xs_ = np.where(coral); cx = (xs_.mean() + 0.5 - radar.shape[1] / 2) / ppu_r; cy = -(ys_.mean() + 0.5 - radar.shape[0] / 2) / ppu_r
pr(f'- **Radar**: el punto coral ({coral.sum()} px) está en x = {cx:+.1f} dp, y = {cy:+.1f} dp (la punta derecha: sigue ASIMÉTRICO); a la izquierda del centro no hay nada coral ({int(coral[:, :radar.shape[1] // 2].sum())} px).')
H, W = radar.shape[:2]; gx, gy = np.meshgrid(np.arange(-16.0, 12.0, 0.4), np.arange(-6.0, 6.0, 0.4))
def Lr(x, y): return luma(radar)[np.clip(np.round(H / 2 - y * ppu_r - 0.5).astype(int), 0, H - 1), np.clip(np.round(W / 2 + x * ppu_r - 0.5).astype(int), 0, W - 1)]
al = radar[np.clip(np.round(H / 2 - gy * ppu_r - 0.5).astype(int), 0, H - 1), np.clip(np.round(W / 2 + gx * ppu_r - 0.5).astype(int), 0, W - 1)][..., 3]
ok = (al > 250) & (gx ** 2 / 18.0 ** 2 + gy ** 2 / 5.5 ** 2 <= 1.0)
pr(f'- El radar sin el punto: izquierda {Lr(gx, gy)[ok & (gx < -2)].mean():.3f} · derecha {Lr(gx, gy)[ok & (gx > 2) & (gx < 10)].mean():.3f} · arriba {Lr(gx, gy)[ok & (gy > 1)].mean():.3f} · abajo {Lr(gx, gy)[ok & (gy < -1)].mean():.3f} (luminosidad media: iguales, ninguna luz gira con el radar).')
pr()

# ------------------------------------------------------------------ 4. el cruce de la correa y el aspa
pr('## 4. Nada de «+»: el cruce de la correa «ocho» y el aspa del cartel')
pr()
ang = 2 * math.degrees(math.atan(2 * 14.0 / 100.0))
pr(f'- La correa cruzada une dos engranajes a dos pasos de la cuadrícula (100 u) y sale del borde de las poleas (r = 14): las dos tiras se cruzan a 2·atan(28/100) = **{ang:.1f}°**, un ocho tendido, nunca una cruz «+» recta (90°). La prueba `TheCrossedBelt_CrossesAtAbout31Degrees_NeverAStraightPlus` revisa las correas de las 12 etapas.')
pr()

# ------------------------------------------------------------------ 5. las flechas de giro
pr('## 5. Las flechas de giro: la cabeza muestra el sentido, también en escala de grises y a tamaño real')
pr()
pr('Escala de grises = luminosidad × alfa (lo que se ve sobre el fondo oscuro). «Espejo»: píxeles de la SILUETA de la flecha a favor del reloj que no coinciden con la de en contra ESPEJADA, sobre el total (la luz del render no se espeja con la geometría, por eso se exige a la silueta). «Sentido»: lo mismo SIN espejar. «Cabeza»: dp² de píxeles que cambian más de 0,3 de gris entre las dos.')
pr()
pr('| Flecha | px/dp | Espejo (silueta) | Sentido (silueta) | Cabeza en grises (dp²) |')
pr('|---|---:|---:|---:|---:|')
for n in ('flecha_grande', 'flecha_chico', 'miniflecha'):
    cw, ppu = recorte(n + '_cw'); ccw, _ = recorte(n + '_ccw')
    g = lambda a: luma(a) * a[..., 3] / 255.0
    sa, sb = cw[..., 3] > 128, ccw[..., 3] > 128
    pr(f'| {n} | {ppu:.0f} | {(sa != sb[:, ::-1]).sum() / sa.sum() * 100:.2f} % | {(sa != sb).sum() / sa.sum() * 100:.1f} % | {(np.abs(g(cw) - g(ccw)) > 0.3).sum() / ppu ** 2:.1f} |')
pr()

# ------------------------------------------------------------------ 6. presionados y peso
pr('## 6. El estado presionado y lo que pesa')
pr()
pr('| Pieza | Luminosidad suelta | Luminosidad presionada | Diferencia | Silueta que cambia |')
pr('|---|---:|---:|---:|---:|')
for suelta, presionada in (('gear_0_0', 'gear_0_0_p'), ('gear_1_0', 'gear_1_0_p'), ('polea', 'polea_p')):
    a, _ = recorte(suelta); b, _ = recorte(presionada); m = (a[..., 3] > 250) & (b[..., 3] > 250)
    sil = ((a[..., 3] > 128) != (b[..., 3] > 128)).sum() / max(1, ((a[..., 3] > 128) | (b[..., 3] > 128)).sum())
    pr(f'| {suelta} → {presionada} | {luma(a)[m].mean():.3f} | {luma(b)[m].mean():.3f} | {luma(b)[m].mean() - luma(a)[m].mean():+.3f} | {sil * 100:.2f} % |')
pr()
p = man['peso']
pr(f'Atlas {man["ancho"]} × {man["alto"]} px, {len(man["sprites"])} imágenes: PNG {p["png_bytes"] / 1024:.0f} KB; en el APK sin compresión de textura ≈ {p["deflate_rgba32"] / 1048576:.2f} MB (tope por juego 1,5 MB); con ASTC 6×6 ≈ {p["astc6x6"] / 1048576:.2f} MB.')
open(os.path.join(AQUI, 'out', 'engranajes_medidas.md'), 'w', encoding='utf-8').write('\n'.join(out) + '\n')
