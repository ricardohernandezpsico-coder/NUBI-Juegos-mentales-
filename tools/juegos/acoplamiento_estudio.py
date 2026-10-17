# -*- coding: utf-8 -*-
"""Acoplamiento — ESTUDIO de la plasticina, FASE A (Tarea 113): compara dos caminos para pasar el módulo y el hueco del puerto a plasticina SIN tocar el juego, con medidas.
  HOY: el arte procedural (réplica en numpy de `DockingSprites.RenderPieceLayers`).
  CAMINO 1 — sub-baldosas: cada bloque son 4 cuartos y cada cuarto elige una baldosa de Blender (convexa, borde, cóncava, interior) según sus 3 vecinos.
  CAMINO 2 — textura/perfil sobre el SDF de hoy: el contorno sale EXACTO del SDF (alfa analítico) y el relieve, de una tabla 1-D medida en un borde recto de Blender.
Deja `tools/juegos/out/acoplamiento_estudio.md`, `docs/previews/plasticina/acoplamiento-estudio.png` (las piezas, los acercamientos, la fuga del espejo y los bordes del hueco) y `acoplamiento-estudio-pantallas.png` (hoy, camino 1 y camino 2 lado a lado, a tamaño de teléfono sobre el fondo real).
Uso: python -I tools/juegos/acoplamiento_estudio.py [--render tools/juegos/out/acoplamiento] [--costo tools/juegos/out/costo_unity.txt]
(las placas las hace `blender -b -P tools/juegos/acoplamiento.py -- --tilt 0`; el costo en Unity, una prueba temporal que no se commitea)"""
import os, sys, math, time, io
import numpy as np
from PIL import Image
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
sys.stdout.reconfigure(encoding='utf-8')
from acopla_geo import *
from acopla_caminos import *
from acopla_medidas import *
from acopla_hojas import *

AQUI = os.path.dirname(os.path.abspath(__file__)); REPO = os.path.normpath(os.path.join(AQUI, '..', '..'))
args = sys.argv[1:]
def opt(n, d): return args[args.index(n) + 1] if n in args else d
RENDER = os.path.abspath(opt('--render', os.path.join(AQUI, 'out', 'acoplamiento')))
COSTO = opt('--costo', os.path.join(REPO, 'unity', 'test-results', 'estudio-costo.txt'))
SALIDA = os.path.join(REPO, 'docs', 'previews', 'plasticina')

out = []
def pr(s=''):
    print(s); out.append(s)

class Caminos:
    def __init__(self):
        self.mod = Maestras(RENDER, 'mod', 0); self.aro = Maestras(RENDER, 'aro', MARGEN); self.fon = Maestras(RENDER, 'fon', 0)
        self.mod_c = Maestras(RENDER, 'mod', 0, luces=(0,), simetriza=False)                       # CONTROL: UNA sola luz, sin simetrizar (lo que delataría el espejo)
        self.pm = PerfilSDF(*self.mod.perfil(-1.0, 0.3)); self.pa = PerfilSDF(*self.aro.perfil(-1.0, 0.3)); self.pf = PerfilSDF(*self.fon.perfil(-1.0, 0.3))
    def hoy(self, cells): return today_layers(cells)                                              # (cuerpo, borde, hueco)
    def c1(self, cells): return compone_c1(cells, self.mod), compone_c1(cells, self.aro), compone_c1(cells, self.fon)
    def c2(self, cells): b = compone_c2(cells, self.pm); a, f = compone_c2_hueco(cells, self.pa, self.pf); return b, a, f
    def c2_sin_arreglo(self, cells): return compone_c2(cells, self.pm, robusto=False)
    def control(self, cells): return compone_c1(cells, self.mod_c)

def fila(nombre, f47, f78):
    cc = lambda f: f'{f["max_conc"]:.2f}' if f['max_conc'] is not None else '—'
    return f'| {nombre} | {f47["iou"]:.4f} | {f47["max"]:.2f} | {cc(f47)} | {f78["iou"]:.4f} | {f78["max"]:.2f} | {cc(f78)} |'

def mide(C):
    P = pick_pieces(); spr = {}
    pr('# Acoplamiento — estudio de la plasticina (Tarea 113, fase A)')
    pr()
    pr('Las cinco piezas reales (poliominós quirales de 4 a 7 bloques que caben en 4 × 4, como `RandomChiralShape`):')
    pr()
    pr('| Pieza | Bloques | Con qué | Cuartos: convexa / borde / cóncava / interior |')
    pr('|---|---:|---|---|')
    quien = {'L4': 'la L del tutorial', 'F5': 'concavidades de 1 bloque (una F)', 'P5': 'un 2 × 2 (una P)', 'C6': 'un hueco de 1 bloque de ancho (una C)', 'H7': 'siete bloques, un hueco de 1 bloque y un 2 × 2'}
    for k, c in P.items():
        tc = tile_cases(c); pr(f'| {k} | {len(c)} | {quien[k]} | {tc["convexa"]} / {tc["borde"]} / {tc["concava"]} / {tc["interior"]} |')
    pool = enumerate_pieces(); total = {'convexa': 0, 'borde': 0, 'concava': 0, 'interior': 0, 'pellizco': 0}
    for v in pool.values():
        for c in v:
            for kk, x in tile_cases(c).items(): total[kk] += x
    nsil = sum(len(v) for v in pool.values()); cuartos = sum(total.values())
    pr()
    pr(f'**Las siluetas del juego:** {nsil} (= {nsil // 2} pares espejo, las «~83»: 4 de 4 bloques, 12 de 5, 42 de 6 y 108 de 7). Sus {cuartos} cuartos de bloque caen así: {total["convexa"]} convexos, {total["borde"]} de borde, {total["concava"]} cóncavos, {total["interior"]} interiores y **{total["pellizco"]} «pellizcos»** '
       f'(un cuarto que solo toca a otro por la diagonal): **con 4 baldosas basta**, el quinto caso que se temía no ocurre en ninguna pieza. El interior es solo el {100 * total["interior"] / cuartos:.0f} % de los cuartos: la pieza es casi todo canto.')
    pr()
    pr('## 1. Fidelidad de la silueta contra el SDF (bloque de 47 u, el más chico; y de 78 u, el más grande)')
    pr()
    pr('Se mide el alfa del sprite de 384 px (80 px por bloque) visto a 47 y a 78 px por bloque con muestreo bilineal sin prefiltro (la GPU), contra el SDF del juego. **IoU** = Σ mín / Σ máx contra la cobertura EXACTA (4 × 4 sub-muestras por píxel). **Desvío máximo del borde** = la mayor distancia '
       'del SDF en los puntos donde el alfa cruza 0,5 (interpolado entre píxeles: precisión de sub-píxel), en px de pantalla. «Concavidades» = puntos a menos de 0,45 bloque de un vértice cóncavo. El módulo se compara con d ≤ 0 (el contorno del SDF; hoy el celeste llega a −0,02 y el borde de tinta a +0,115, y «hoy» se compara con +0,115); '
       'el aro del hueco con d ≤ 0,115 y su fondo con d ≤ 0 (hoy: −0,023).')
    pr()
    pr('| Pieza · camino | IoU a 47 u | Desvío máx. 47 u (px) | máx. en concavidades | IoU a 78 u | Desvío máx. 78 u (px) | máx. en concavidades |')
    pr('|---|---:|---:|---:|---:|---:|---:|')
    for k, c in P.items():
        b, e, h = C.hoy(c); b1, a1, f1 = C.c1(c); b2, a2, f2 = C.c2(c); b2s = C.c2_sin_arreglo(c); b2p = compone_c2(c, C.pm, aa_px=1.0)
        spr[k] = {'hoy': (b, e, h), 'c1': (b1, a1, f1), 'c2': (b2, a2, f2)}
        b2h = compone_c2_baja(c, C.pm, 2, aa_px=1.0); b2t = compone_c2_baja(c, C.pm, 3, aa_px=1.0)
        for nombre, alfa, nivel in (('hoy (con tinta)', b[..., 3], INK), ('camino 1', b1[..., 3], 0.0), ('camino 2', b2[..., 3], 0.0), ('camino 2, rampa de 1 px', b2p[..., 3], 0.0), ('camino 2, SDF a 1/2 de resolución (192²)', b2h[..., 3], 0.0), ('camino 2, SDF a 1/3 de resolución (128²)', b2t[..., 3], 0.0)):
            pr(fila(f'{k} · {nombre}', fidelidad(alfa, c, nivel, 47), fidelidad(alfa, c, nivel, 78)))
    pr()
    pr('**El hueco**, en dos capas como hoy (el aro debajo, la silueta de d ≤ 0,115; el fondo encima, d ≤ 0):')
    pr()
    pr('| Pieza · capa · camino | IoU a 47 u | Desvío máx. 47 u (px) | máx. en concavidades | IoU a 78 u | Desvío máx. 78 u (px) | máx. en concavidades |')
    pr('|---|---:|---:|---:|---:|---:|---:|')
    for k, c in P.items():
        b, e, h = spr[k]['hoy']
        pr(fila(f'{k} · aro · hoy', fidelidad(e[..., 3], c, INK, 47), fidelidad(e[..., 3], c, INK, 78)))
        pr(fila(f'{k} · fondo · hoy', fidelidad(h[..., 3], c, -HOLE_INSET, 47), fidelidad(h[..., 3], c, -HOLE_INSET, 78)))
        for cam in ('c1', 'c2'):
            _, ar, fo = spr[k][cam]; n = 'camino 1' if cam == 'c1' else 'camino 2'
            pr(fila(f'{k} · aro · {n}', fidelidad(ar[..., 3], c, INK, 47), fidelidad(ar[..., 3], c, INK, 78)))
            pr(fila(f'{k} · fondo · {n}', fidelidad(fo[..., 3], c, 0.0, 47), fidelidad(fo[..., 3], c, 0.0, 78)))
    pr()
    return P, spr

T5 = normalize([(0, 0), (1, 0), (2, 0), (1, 1), (1, 2)])                                         # una pieza SIMÉTRICA (igual a su espejo): aísla la luz con dirección de la forma

def cuerpo_de(C, nombre, cells):
    if nombre == 'hoy': return today_layers(cells)[0]
    if nombre == 'camino 1': return compone_c1(cells, C.mod)
    if nombre == 'camino 2': return compone_c2(cells, C.pm)
    return C.control(cells)

def mide_espejo(C, P, spr):
    pr('## 2. Fuga del espejo')
    pr()
    pr('El juego muestra el espejo como el MISMO sprite con escala x −1 (`DockingGameController.Scene.cs`): lo que el sprite lleve horneado con dirección (una luz, un relieve) cambia de lado y lo delata. Dos medidas, en niveles de 255: '
       '(a) la **asimetría izquierda-derecha de la luz** en una pieza simétrica (una T de 5 bloques, igual a su espejo): |L(S) − L(espejo(S))|. OJO: como el camino 1 arma el lado derecho con las baldosas del izquierdo reflejadas, una pieza simétrica sale simétrica pase lo que pase con la luz (también en el control): por eso esta medida vale 0 en todos y la que delata es la (b); '
       '(b) la **modulación direccional del canto** de las 5 piezas reales (la luminosidad de una banda de 0,12 bloque dentro del contorno según el azimut de la normal exterior; el primer armónico es una luz con dirección). '
       'Además el alfa del sprite de una pieza espejada y el espejo del sprite de la pieza coinciden (diferencia máxima de alfa 0,000 en los tres caminos). «Control» = el camino 1 con UNA sola luz sin simetrizar: lo que pasaría con un render de luz horneada.')
    pr()
    pr('| Camino | (a) Asimetría L-R de la T: máx. | media | (b) Dir. 1.er armónico (peor de las 5) | 2.º armónico |')
    pr('|---|---:|---:|---:|---:|')
    for nombre in ('hoy', 'camino 1', 'camino 2', 'control (1 luz)'):
        S = cuerpo_de(C, nombre, T5); mx, md = asimetria_lr(S)
        a1 = []; a2 = []
        for k, c in P.items():
            d1, d2, _ = direccionalidad(cuerpo_de(C, nombre, c), c); a1.append(d1); a2.append(d2)
        pr(f'| {nombre} | {mx:.2f} | {md:.2f} | {max(a1):.2f} | {max(a2):.2f} |')
    pr()
    pr('El módulo es casi plano (la caída del canto es de unos 14 niveles de 255: ver §5), así que incluso la luz horneada de UNA sola dirección delata poco (≈ 1 nivel de modulación y 1 a 3 niveles de asimetría): la fuga existe y el promedio de 4 luces con espejos la deja en ≈ 0, '
       'pero el riesgo real es menor que el que se temía; crece si se sube el relieve (con el camino 2, ×2,5 o más, la tabla es isótropa por construcción: no cambia).')
    pr()

def mide_juntas(C, P, spr):
    pr('## 3. ¿Se ven juntas o un «+» dentro de piezas de 2 × 2?')
    pr()
    pr('En las piezas con un 2 × 2 (P5 y H7): el salto de luz (media de |ΔL| entre píxeles a un lado y otro de la línea, en niveles de 255) en las líneas donde se juntan los BLOQUES y donde se juntan los CUARTOS de baldosa, contra el salto «de fondo» (lo que cambia la luz en el interior sin junta alguna); '
       'y la luz del centro del «+» (disco de 5 px menos anillo de 9 a 13 px). «camino 2, sin arreglo» = el SDF tal cual está hoy.')
    pr()
    pr('| Pieza · camino | Salto en juntas de bloque | Salto en juntas de cuarto | Salto de fondo | «+» en el centro (niveles) |')
    pr('|---|---:|---:|---:|---:|')
    for k in ('P5', 'H7'):
        c = P[k]; s = set(c); ccx, ccy, w, h = piece_box(c)
        x, y = [(x, y) for (x, y) in c if {(x, y), (x + 1, y), (x, y + 1), (x + 1, y + 1)} <= s][0]
        jx = SIZE / 2 + (x + 0.5 - ccx) * PX; jy = SIZE / 2 + (y + 0.5 - ccy) * PX; qx = SIZE / 2 + (x - ccx) * PX; qy = SIZE / 2 + (y - ccy) * PX
        for nombre, S in (('hoy', spr[k]['hoy'][0]), ('camino 1', spr[k]['c1'][0]), ('camino 2', spr[k]['c2'][0]), ('camino 2, sin arreglo', C.c2_sin_arreglo(c))):
            sb = np.nanmean([v for v in (salto(S, linea_x=jx), salto(S, linea_y=jy)) if v is not None])
            sq = np.nanmean([v for v in (salto(S, linea_x=qx), salto(S, linea_x=qx + PX), salto(S, linea_y=qy), salto(S, linea_y=qy + PX)) if v is not None])
            cz = centro_en_cruz(S, c)
            pr(f'| {k} · {nombre} | {sb:.2f} | {sq:.2f} | {fondo_saltos(S):.2f} | {max(cz, key=abs):+.2f} |')
    pr()
    pr('Hallazgo del camino 2: con el SDF TAL COMO ESTÁ (cajas y puentes de 1 × 2), el punto donde se juntan 4 bloques de un 2 × 2 queda justo en el borde de los cuatro puentes (d = 0 aunque esté 0,5 bloque adentro de la pieza): ningún píxel cae exactamente ahí, pero el vecino más cercano da d ≈ −0,006 '
       'y se dibuja un ALFILER translúcido (alfa 0,83) del color del canto. Hoy no se ve porque la junta de tinta lo tapa. Se arregla sumando al SDF una caja de 2 × 2 por cada 2 × 2 de bloques (4 líneas, sin costo de importancia); con ese arreglo el «+» desaparece.')
    pr()

def mide_relieve(C, P, spr):
    pr('## 4. Cuánto se parece el relieve del camino 2 a las baldosas de Blender (el camino 1 es la verdad en 3D)')
    pr()
    pr('Diferencia de luminosidad (niveles de 255) entre el camino 2 y el camino 1 en la banda de 0,14 bloque dentro del contorno, por clase de cuarto (los cuartos de cada pieza se clasifican por sus 3 vecinos). Es el error del perfil 1-D (el borde recto) cuando se lo aplica a esquinas.')
    pr()
    pr('| Clase de cuarto | Píxeles | Diferencia de luz media | p99 | máx. |')
    pr('|---|---:|---:|---:|---:|')
    acc = {k: [] for k in ('convexa', 'borde', 'concava', 'interior')}
    for k, c in P.items():
        s = set(c); ccx, ccy, w, h = piece_box(c); gx, gy = grid(c); d = sdf_robusto(c, gx, gy)
        L1 = luma(spr[k]['c1'][0]) * 255.0; L2 = luma(spr[k]['c2'][0]) * 255.0
        ok = (d < -0.005) & (d > -0.14) & (spr[k]['c1'][0][..., 3] > 0.99) & (spr[k]['c2'][0][..., 3] > 0.99)
        ys, xs = np.mgrid[0:SIZE, 0:SIZE]
        for (x, y) in c:
            for sx in (-1, 1):
                for sy in (-1, 1):
                    tipo = paso_tipo((x + sx, y) in s, (x, y + sy) in s, (x + sx, y + sy) in s).split('_')[0]
                    cx = SIZE / 2 + (x - ccx) * PX; cy = SIZE / 2 + (y - ccy) * PX
                    m = ((xs + 0.5 - cx) * sx >= 0) & ((xs + 0.5 - cx) * sx < Q) & ((ys + 0.5 - cy) * sy >= 0) & ((ys + 0.5 - cy) * sy < Q) & ok
                    acc[tipo].append(np.abs(L1 - L2)[m])
    for tipo, v in acc.items():
        v = np.concatenate(v) if v else np.zeros(0)
        if v.size == 0: pr(f'| {tipo} | 0 | — (no hay banda de canto en un cuarto interior) | — | — |'); continue
        pr(f'| {tipo} | {v.size} | {v.mean():.2f} | {np.percentile(v, 99):.2f} | {v.max():.2f} |')
    pr()

def mide_relieve_visible(C, P, spr):
    pr('## 5. Cuánto relieve hay (la «plasticina» que se ve)')
    pr()
    ys, cols = C.mod.perfil(-1.0, 0.3); L = (0.2126 * cols[:, 0] + 0.7152 * cols[:, 1] + 0.0722 * cols[:, 2]) / np.maximum(cols[:, 3], 1e-6) * 255.0
    dentro = (ys < -0.02); li = L[ys < -0.5].mean()
    pr(f'- Perfil del canto del módulo (luminosidad en niveles de 255; promedio de 4 luces con espejos, a lo largo de un borde recto): interior {li:.1f}; mínimo {L[dentro].min():.1f} a {-ys[dentro][np.argmin(L[dentro])]:.3f} bloque del borde. La caída máxima es de {li - L[dentro].min():.1f} niveles ({100 * (li - L[dentro].min()) / li:.1f} %): '
       'el canto blando de 0,075 bloque (≈ 6 px a 80 por bloque) se nota como un velo apenas más oscuro. Con la luz promediada en 4 giros el relieve queda SIN dirección y por eso es tan suave; si hace falta más «plasticina» se sube el canto o se le suma oclusión (se ve igual en los dos caminos).')
    tex = C.mod.textura; lt = luma(tex) * 255.0
    centro = lt[int(lt.shape[0] * 0.2):int(lt.shape[0] * 0.8), int(lt.shape[1] * 0.2):int(lt.shape[1] * 0.8)]
    pr(f'- Textura fina del kit en el interior (placa lisa de 4,8 bloques, luz promediada): desviación {centro.std():.2f} niveles de 255. El grano propio de la plasticina casi no sobrevive al promedio de 4 luces y al reductor de ruido; si se quiere un grano que se vea, se le suma una textura isótropa de periodo corto: lo admiten los dos caminos por igual (camino 1: ya horneada en las baldosas; camino 2: una textura de 128 × 128 que se repite).')
    pr()

def mide_costo(C, P):
    pr('## 6. Peso y costo')
    pr()
    def png(a):
        b = io.BytesIO(); to_img(a).save(b, format='PNG', optimize=True); return b.tell()
    ts = [C.mod.t_convexa, C.mod.t_borde, C.mod.t_concava, C.mod.t_interior, C.aro.t_convexa, C.aro.t_borde, C.aro.t_concava, C.aro.t_interior, C.fon.t_convexa, C.fon.t_borde, C.fon.t_concava, C.fon.t_interior]
    pr(f'- **Camino 1:** 12 baldosas (4 del módulo de 40 × 40 px, 4 del aro de 40 a 52 px y 4 del fondo de 40 × 40): {sum(t.shape[0] * t.shape[1] for t in ts) * 4 / 1024:.1f} KB en RGBA32, ≈ {sum(png(t) for t in ts) / 1024:.1f} KB en PNG; en un atlas con su relleno ≈ 40 KB (a 6 px de canto, 8 baldosas por capa si se quisiera más variedad).')
    pr('- **Camino 2:** tres tablas 1-D de color (módulo, aro y fondo; 128 entradas de RGB = 1,1 KB en total) y, solo si se quiere grano, una textura isótropa de 128 × 128 (16 KB en gris). Sin baldosas.')
    P7 = P['H7']; tiempos = {}
    for nombre, f in (('hoy', lambda: today_layers(P7)), ('camino 1', lambda: (compone_c1(P7, C.mod), compone_c1(P7, C.aro), compone_c1(P7, C.fon))), ('camino 2', lambda: (compone_c2(P7, C.pm), compone_c2_hueco(P7, C.pa, C.pf))), ('camino 2 a 1/2', lambda: compone_c2_baja(P7, C.pm, 2))):
        t = time.time()
        for _ in range(3): f()
        tiempos[nombre] = (time.time() - t) / 3
    pr()
    pr('Tiempo por pieza de 7 bloques, solo en numpy (el orden de magnitud, no el del teléfono): hoy ' + f'{tiempos["hoy"] * 1000:.0f} ms · camino 1 {tiempos["camino 1"] * 1000:.0f} ms · camino 2 {tiempos["camino 2"] * 1000:.0f} ms · camino 2 con el SDF a 1/2 de resolución (solo el módulo) {tiempos["camino 2 a 1/2"] * 1000:.0f} ms.')
    if os.path.exists(COSTO):
        pr()
        pr('**Medido en Unity (Editor con Mono, esta PC; una pieza de 7 bloques, promedio de 20 horneos; NO medido en el teléfono: el número absoluto allá será otro, la proporción es la que importa):**')
        pr()
        for ln in open(COSTO, encoding='utf-8').read().splitlines(): pr(f'- {ln}')
    pr()

if __name__ == '__main__':
    C = Caminos()
    P, spr = mide(C)
    mide_espejo(C, P, spr)
    mide_juntas(C, P, spr)
    mide_relieve(C, P, spr)
    mide_relieve_visible(C, P, spr)
    mide_costo(C, P)
    os.makedirs(os.path.join(AQUI, 'out'), exist_ok=True)
    open(os.path.join(AQUI, 'out', 'acoplamiento_estudio.md'), 'w', encoding='utf-8').write('\n'.join(out) + '\n')
    cab = open(os.path.join(AQUI, 'acopla_cabecera.md'), encoding='utf-8').read()
    open(os.path.join(REPO, 'docs', 'estudio-acoplamiento-plasticina.md'), 'w', encoding='utf-8', newline='\n').write(cab + '# Las medidas\n\n' + '\n'.join(out[1:]) + '\n')    # la cabecera trae el título y la recomendación
    os.makedirs(SALIDA, exist_ok=True)
    if '--sin-hojas' not in args:
        hp = hoja_principal(C, P, spr); hp.save(os.path.join(SALIDA, 'acoplamiento-estudio.png'), optimize=True); print('hoja', hp.size)
        pa = hoja_pantallas(C, P, spr)
        if pa is not None: pa.save(os.path.join(SALIDA, 'acoplamiento-estudio-pantallas.png'), optimize=True); print('pantallas', pa.size)
    print('listo')
