# -*- coding: utf-8 -*-
"""Bodega de carga en plasticina (Tarea 107): los 12 objetos de la carga, la escotilla (marco, aro, tres interiores y la hoja de la puerta con su estado PRESIONADO), el robot, la caja, la esclusa y el carro de reparto.
El casco, los remaches, el aro de sello, los resplandores, el haz, la cuerda y las chispas se quedan procedurales (decoración: pasada final de decoraciones); la tarjeta de arriba, el aviso y la tarjeta de entrada son losas del kit.

Uso: blender -b -P tools/juegos/bodega.py -- [--solo obj|obj_K|escotilla|robot|resto] [--samples N] [--out DIR]
Salida (en --out, por defecto tools/juegos/out): las «maestras» con fondo transparente (`obj_0..11`, `marco`, `aro`, `interior_0..2`, `puerta_d0..3`, `puerta_p_d0..3`, `puerta_disco_d0..3`, `robot`, `visor`, `ojo`, `tallo`, `bola`, `caja`, `esclusa`, `carro_0..2`);
`empaqueta.py bodega` promedia las cuatro de cada puerta y las reduce.

LOS OBJETOS son las siluetas de siempre (las de `BitacoraSprites` y la manzana y la taza de `BodegaSprites`: x a la derecha, y ARRIBA, el objeto ocupa ±0,8 en un lienzo de ±1,12), hechos de relieves de plasticina apilados con el canto blando. Sin borde de tinta:
se ven sobre fondos oscuros (el interior de la escotilla, la tarjeta, el carro, el espacio). 1 unidad de diseño = 1 unidad de Blender.
LAS PIEZAS DE LA ESCOTILLA (1 dp = 1/20 de unidad de Blender): la hoja de la puerta (media escotilla) GIRA por código al ángulo de su lugar en el anillo: se renderiza SIN luz direccional (se promedian cuatro renders con la luz girada 0°, 90°, 180° y 270°)
para que no tenga una sombra que gire con ella; `puerta_p` es la hoja presionada (un punto más oscura y con la huella de un dedo en el espacio libre de la cara); el aplastamiento (~8 % más baja, ~4 % más ancha) lo hace el juego escalando la escotilla entera.
"""
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from plasticina import *
from mathutils import Matrix

SOLO = arg('--solo', 'todo')
SUN = '#FFC93C'; ORANGE = '#FF8A3D'; GRAPE = '#B8A4FF'; SKY = '#4CC9F0'; CREAM = '#FFF8EC'; CORAL = '#FF6B4A'; MINT = '#5FD68A'; LIME = '#9BE564'; BROWN = '#9C6A4E'; APPLE = '#E2524A'; TINTA = '#1A1240'
OBJ_LIENZO = 2.24
OBJ_PX = 384                                                  # la maestra: 384 px (se reduce a 192)

# ------------------------------------------------------------------ geometría (y ARRIBA)
def ccw(pts):
    a = sum(pts[i][0] * pts[(i + 1) % len(pts)][1] - pts[(i + 1) % len(pts)][0] * pts[i][1] for i in range(len(pts)))
    return list(pts) if a > 0 else list(reversed(pts))

def girar(pts, grados, cx=0.0, cy=0.0):
    a = math.radians(grados); c, s = math.cos(a), math.sin(a)
    return [(cx + (x - cx) * c - (y - cy) * s, cy + (x - cx) * s + (y - cy) * c) for (x, y) in pts]

def circ(cx, cy, r, n=72): return [(cx + r * math.cos(2 * math.pi * i / n), cy + r * math.sin(2 * math.pi * i / n)) for i in range(n)]
def elip(cx, cy, rx, ry, n=72): return [(cx + rx * math.cos(2 * math.pi * i / n), cy + ry * math.sin(2 * math.pi * i / n)) for i in range(n)]

def rr(cx, cy, hx, hy, r=0.0, n=7):
    """Un rectángulo de medios hx × hy con esquinas redondeadas de radio r (mínimo: la plasticina nunca tiene la esquina viva)."""
    r = max(0.02, min(r, hx, hy)); out = []
    for (sx, sy, a0) in ((1, 1, 0.0), (-1, 1, 0.5 * math.pi), (-1, -1, math.pi), (1, -1, 1.5 * math.pi)):
        ccx, ccy = cx + sx * (hx - r), cy + sy * (hy - r)
        for k in range(n + 1):
            a = a0 + 0.5 * math.pi * k / n; out.append((ccx + r * math.cos(a), ccy + r * math.sin(a)))
    return ccw(out)

def trazo(pts, w0, w1=None, cap=8):
    """Una cinta de ancho [w0] a [w1] (completo) a lo largo de la polilínea [pts], con las puntas redondas."""
    w1 = w0 if w1 is None else w1; n = len(pts); izq = []; der = []; ns = []
    for i, p in enumerate(pts):
        t = i / (n - 1.0); w = (w0 + (w1 - w0) * t) / 2.0
        if i == 0: d = (pts[1][0] - p[0], pts[1][1] - p[1])
        elif i == n - 1: d = (p[0] - pts[i - 1][0], p[1] - pts[i - 1][1])
        else: d = (pts[i + 1][0] - pts[i - 1][0], pts[i + 1][1] - pts[i - 1][1])
        l = math.hypot(*d) or 1.0; nx, ny = -d[1] / l, d[0] / l; ns.append((nx, ny, w))
        izq.append((p[0] + nx * w, p[1] + ny * w)); der.append((p[0] - nx * w, p[1] - ny * w))
    fin = []; nx, ny, w = ns[-1]; a0 = math.atan2(ny, nx)
    for k in range(1, cap): a = a0 - math.pi * k / cap; fin.append((pts[-1][0] + w * math.cos(a), pts[-1][1] + w * math.sin(a)))
    ini = []; nx, ny, w = ns[0]; a0 = math.atan2(-ny, -nx)
    for k in range(1, cap): a = a0 - math.pi * k / cap; ini.append((pts[0][0] + w * math.cos(a), pts[0][1] + w * math.sin(a)))
    return ccw(izq + fin + list(reversed(der)) + ini)

def arco(cx, cy, r, a0, a1, n=24): return [(cx + r * math.cos(math.radians(a0 + (a1 - a0) * i / n)), cy + r * math.sin(math.radians(a0 + (a1 - a0) * i / n))) for i in range(n + 1)]

def recorta(pts, dentro, corte):
    """Sutherland-Hodgman contra UN semiplano: [dentro](p) dice si el punto queda; [corte](a, b) es donde la arista lo cruza."""
    out = []
    for i in range(len(pts)):
        a, b = pts[i - 1], pts[i]; da, db = dentro(a), dentro(b)
        if db:
            if not da: out.append(corte(a, b))
            out.append(b)
        elif da: out.append(corte(a, b))
    return out

def semiplano_y(pts, y0, arriba=True):
    cruza = lambda a, b: (a[0] + (b[0] - a[0]) * (y0 - a[1]) / (b[1] - a[1]), y0)
    return recorta(pts, (lambda p: p[1] >= y0) if arriba else (lambda p: p[1] <= y0), cruza)

def semiplano_x(pts, x0, derecha=True):
    cruza = lambda a, b: (x0, a[1] + (b[1] - a[1]) * (x0 - a[0]) / (b[0] - a[0]))
    return recorta(pts, (lambda p: p[0] >= x0) if derecha else (lambda p: p[0] <= x0), cruza)

# las distancias firmadas de ClayRaster (negativas adentro), para sacar el contorno de las siluetas que son uniones y diferencias
def d_circ(x, y, cx, cy, r): return math.hypot(x - cx, y - cy) - r
def d_elip(x, y, cx, cy, rx, ry): return (math.hypot((x - cx) / rx, (y - cy) / ry) - 1.0) * min(rx, ry)
def d_caja(x, y, cx, cy, hx, hy, r):
    qx, qy = abs(x - cx) - (hx - r), abs(y - cy) - (hy - r); return math.hypot(max(qx, 0.0), max(qy, 0.0)) + min(max(qx, qy), 0.0) - r

def contorno_sdf(f, cx, cy, n=200, rmax=1.6):
    """El contorno (antihorario) de la forma [f] <= 0 que se ve desde (cx, cy) (adentro): un rayo por ángulo, hasta donde [f] cruza 0."""
    out = []
    for i in range(n):
        a = 2 * math.pi * i / n; dx, dy = math.cos(a), math.sin(a); lo, hi = 0.0, rmax
        for _ in range(40):
            m = (lo + hi) / 2
            if f(cx + dx * m, cy + dy * m) <= 0: lo = m
            else: hi = m
        out.append((cx + dx * lo, cy + dy * lo))
    return out

# ------------------------------------------------------------------ las capas
_R = {}
def capa(cont, color, h, bis, nombre='p', z0=0.0, panza=0.0, material=None):
    return pieza(cont, h, min(bis, h * 0.48), color, nombre, z0=z0, panza=panza, material=material, padre=_R['r'])

def fina(cont, h, nombre='linea'): return capa(cont, None, h, 0.012, nombre, material=tinta())

def aro(cx, cy, R_, tubo, color, z=None, nombre='aro', material=None):
    return aro_plano(R_, tubo, color, loc=(cx, cy, tubo if z is None else z), nombre=nombre, material=material, padre=_R['r'])

# ------------------------------------------------------------------ los 12 objetos (id de BodegaContract.ObjectNames)
def llave(r):
    A = 35.0
    bx, by = girar([(-0.5, 0.0)], A)[0]
    aro(bx, by, 0.28, 0.12, SUN, nombre='llave_aro')                                                    # el ojo de la llave: una dona (anillo de 0,28 ± 0,12)
    capa(trazo(girar([(-0.22, 0.0), (0.78, 0.0)], A), 0.2), SUN, 0.17, 0.07, 'llave_caña')
    for (cx, cy, hx, hy) in ((0.5, -0.16, 0.06, 0.12), (0.7, -0.2, 0.07, 0.16)): capa(girar(rr(cx, cy, hx, hy, 0.03), A), SUN, 0.15, 0.05, 'llave_diente')

def campana(r):
    cont = ccw([(0.58, -0.4)] + [(0.36, 0.26)] + arco(0.0, 0.24, 0.36, 0, 180, 30)[1:-1] + [(-0.36, 0.26), (-0.58, -0.4)])
    capa(cont, ORANGE, 0.24, 0.11, 'campana', panza=0.03)
    capa(rr(0.0, -0.44, 0.66, 0.1, 0.08), SUN, 0.30, 0.07, 'campana_borde')
    capa(circ(0.0, -0.64, 0.13, 40), oscurece(SUN, 0.8), 0.26, 0.06, 'campana_badajo')
    aro(0.0, 0.68, 0.13, 0.05, SUN, nombre='campana_asa')

def farol(r):
    capa(rr(0.0, -0.08, 0.38, 0.46, 0.1), ORANGE, 0.22, 0.08, 'farol')
    capa(ccw([(-0.46, 0.36), (0.46, 0.36), (0.2, 0.58), (-0.2, 0.58)]), ORANGE, 0.22, 0.07, 'farol_tapa')
    capa(rr(0.0, -0.6, 0.46, 0.08, 0.05), ORANGE, 0.2, 0.05, 'farol_pie')
    aro(0.0, 0.62, 0.22, 0.05, ORANGE, nombre='farol_asa')                                                  # el asa: una dona (su mitad de abajo queda escondida bajo la tapa)
    capa(rr(0.0, -0.08, 0.295, 0.375, 0.07), TINTA, 0.25, 0.02, 'farol_marco', material=tinta())
    capa(rr(0.0, -0.08, 0.26, 0.34, 0.06), '#FFDE7A', 0.28, 0.03, 'farol_vidrio')
    capa(elip(0.0, -0.14, 0.1, 0.17, 36), '#FFF0B8', 0.31, 0.04, 'farol_llama')
    capa(trazo([(0.0, -0.42), (0.0, 0.26)], 0.05), None, 0.33, 0.01, 'farol_barra', material=tinta())

def manzana(r):
    f = lambda x, y: min(d_circ(x, y, -0.24, -0.06, 0.5), d_circ(x, y, 0.24, -0.06, 0.5), d_circ(x, y, 0.0, -0.22, 0.52))
    capa(contorno_sdf(f, 0.0, -0.1, 200), APPLE, 0.3, 0.14, 'manzana', panza=0.02)
    capa(trazo([(0.0, 0.42), (0.04, 0.82)], 0.12), BROWN, 0.14, 0.05, 'manzana_tallo', z0=0.18)
    hoja = girar(elip(0.0, 0.0, 0.2, 0.09, 36), -0.55 * 180 / math.pi); hoja = [(x + 0.3, y + 0.7) for (x, y) in hoja]
    capa(ccw(hoja), MINT, 0.12, 0.05, 'manzana_hoja', z0=0.12)

def hongo(r):
    capa(rr(0.0, -0.38, 0.22, 0.38, 0.14), CREAM, 0.2, 0.09, 'hongo_pie')
    cap = ccw(arco(0.0, 0.02, 1.0, 0, 180, 60)); cap = [(0.78 * x, 0.02 + 0.66 * (y - 0.02)) for (x, y) in cap]
    capa(cap, CORAL, 0.28, 0.13, 'hongo_sombrero', panza=0.03, z0=0.04)
    for (cx, cy, rad) in ((-0.36, 0.26, 0.12), (0.12, 0.46, 0.1), (0.44, 0.18, 0.09)): capa(circ(cx, cy, rad, 36), CREAM, 0.1, 0.045, 'hongo_punto', z0=0.27)

def taza(r):
    capa(rr(-0.04, -0.6, 0.68, 0.08, 0.06), GRAPE, 0.1, 0.04, 'taza_plato')
    capa(trazo(arco(0.5, 0.02, 0.27, -100, 100, 24), 0.18), SKY, 0.14, 0.06, 'taza_asa')
    cuerpo = rr(-0.08, 0.0, 0.5, 0.5, 0.16)
    capa(cuerpo, SKY, 0.24, 0.09, 'taza', z0=0.02)
    capa(semiplano_y(cuerpo, 0.34), CREAM, 0.045, 0.02, 'taza_borde', z0=0.255)
    capa(rr(-0.08, 0.36, 0.4, 0.03, 0.02), '#A97A58', 0.05, 0.015, 'taza_raya', z0=0.3)

def paraguas(r):
    f = lambda x, y: max(max(d_circ(x, y, 0.0, 0.0, 0.84), -y), -min(d_circ(x, y, -0.56, -0.02, 0.28), d_circ(x, y, 0.0, -0.02, 0.28), d_circ(x, y, 0.56, -0.02, 0.28)))
    capa(trazo([(0.0, 0.1), (0.0, -0.62)], 0.11), BROWN, 0.12, 0.05, 'paraguas_mango')
    capa(trazo(arco(0.16, -0.62, 0.16, 180, 360, 20), 0.11), BROWN, 0.12, 0.05, 'paraguas_gancho')
    cont = contorno_sdf(f, 0.0, 0.35, 260)
    capa(cont, LIME, 0.26, 0.11, 'paraguas', panza=0.03, z0=0.04)
    for (x0, x1) in ((0.18, 0.38), (-0.38, -0.18)):                                                           # dos franjas claras de la cúpula, recortadas a su silueta (con los festones)
        fr = semiplano_x(semiplano_x(cont, x0), x1, derecha=False)
        if len(fr) >= 3: capa(ccw(fr), '#C9F2A5', 0.05, 0.02, 'paraguas_franja', z0=0.29)
    capa(circ(0.0, 0.86, 0.07, 28), LIME, 0.12, 0.05, 'paraguas_punta', z0=0.04)

def libro(r):
    capa(rr(0.0, 0.0, 0.6, 0.74, 0.1), SKY, 0.2, 0.08, 'libro_tapa')
    capa(rr(-0.5, 0.0, 0.1, 0.74, 0.06), '#3B9FC0', 0.26, 0.05, 'libro_lomo')
    capa(rr(0.1, -0.02, 0.44, 0.64, 0.05), CREAM, 0.27, 0.05, 'libro_paginas')
    for i in range(4): capa(trazo([(-0.18, 0.36 - 0.2 * i), (0.4, 0.36 - 0.2 * i)], 0.045), '#A9B3C9', 0.3, 0.012, 'libro_linea')
    capa(ccw([(0.3, 0.74), (0.44, 0.74), (0.44, 0.36), (0.37, 0.44), (0.3, 0.36)]), CORAL, 0.31, 0.03, 'libro_marca')

def gema(r):
    A, B, C, D, E, F, P = (-0.66, 0.24, 0.10), (-0.34, 0.56, 0.22), (0.34, 0.56, 0.22), (0.66, 0.24, 0.10), (0.2, 0.24, 0.22), (-0.2, 0.24, 0.22), (0.0, -0.7, 0.05)
    me = bpy.data.meshes.new('gema'); bm = bmesh.new(); base = [A, B, C, D, P]; n = len(base)
    ring0 = [bm.verts.new((v[0], v[1], 0.0)) for v in base]; ring1 = [bm.verts.new((v[0], v[1], v[2] * 0.4)) for v in base]
    bm.faces.new(list(reversed(ring0)))
    for j in range(n): bm.faces.new((ring0[j], ring0[(j + 1) % n], ring1[(j + 1) % n], ring1[j]))
    vv = {k: bm.verts.new(v) for k, v in (('A', A), ('B', B), ('C', C), ('D', D), ('E', E), ('F', F), ('P', P))}
    for tri in (('A', 'B', 'F'), ('B', 'C', 'E', 'F'), ('C', 'D', 'E'), ('A', 'F', 'P'), ('F', 'E', 'P'), ('E', 'D', 'P')): bm.faces.new([vv[k] for k in tri])
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces)); bm.normal_update(); bm.to_mesh(me); bm.free(); me.update()
    o = bpy.data.objects.new('gema', me); scene.collection.objects.link(o); link(o, 'iconos')
    md = bevel(o, 0.02, 3, ang=25); md.limit_method = 'ANGLE'
    o.data.materials.append(mat_kit('m_j_gema', SKY)); o.parent = _R['r']

def reloj(r):
    vidrio = ccw([(-0.42, 0.62), (0.42, 0.62), (0.09, 0.0), (0.42, -0.62), (-0.42, -0.62), (-0.09, 0.0)])
    capa(rr(0.0, 0.7, 0.56, 0.1, 0.06), BROWN, 0.22, 0.06, 'reloj_tapa')
    capa(rr(0.0, -0.7, 0.56, 0.1, 0.06), BROWN, 0.22, 0.06, 'reloj_base')
    capa(vidrio, '#A6E1F8', 0.18, 0.06, 'reloj_vidrio', panza=0.02)
    abajo = semiplano_y(ccw([(0.09, 0.0), (0.42, -0.62), (-0.42, -0.62), (-0.09, 0.0)]), -0.22, arriba=False)
    arriba = semiplano_y(semiplano_y(ccw([(-0.42, 0.62), (-0.09, 0.0), (0.09, 0.0), (0.42, 0.62)]), 0.26), 0.5, arriba=False)
    capa(abajo, SUN, 0.1, 0.04, 'reloj_arena', z0=0.12)
    capa(arriba, SUN, 0.1, 0.04, 'reloj_arena_alta', z0=0.12)

def pluma(r):
    A = 38.0
    f = lambda x, y: max(d_elip(x, y, 0.0, 0.12, 0.3, 0.76), -min(d_circ(x, y, 0.36, 0.22, 0.12), d_circ(x, y, -0.36, -0.12, 0.12)))
    cont = contorno_sdf(f, 0.0, 0.12, 240)
    capa(girar(cont, A), GRAPE, 0.2, 0.09, 'pluma', panza=0.02)
    capa(girar(semiplano_x(cont, 0.0), A), '#DDD3FF', 0.045, 0.02, 'pluma_luz', z0=0.19)
    capa(girar(trazo([(0.0, -0.9), (0.0, 0.1)], 0.12), A), CREAM, 0.13, 0.05, 'pluma_cañon')
    capa(girar(trazo([(0.0, -0.55), (0.0, 0.8)], 0.04), A), None, 0.235, 0.01, 'pluma_nervio', material=tinta())

def bellota(r):
    capa(trazo([(0.0, 0.42), (0.08, 0.72)], 0.12), oscurece(BROWN, 0.8), 0.14, 0.05, 'bellota_tallo')
    capa(elip(0.0, -0.2, 0.45, 0.56, 72), ORANGE, 0.3, 0.14, 'bellota', panza=0.04)
    cap = ccw(arco(0.0, 0.22, 1.0, 0, 180, 40)); cap = [(0.56 * x, 0.22 + 0.3 * (y - 0.22)) for (x, y) in cap]
    cap = semiplano_y(cap, 0.1)
    capa(cap, BROWN, 0.26, 0.09, 'bellota_gorro', z0=0.1, panza=0.02)

OBJETOS = (llave, campana, farol, manzana, hongo, taza, paraguas, libro, gema, reloj, pluma, bellota)

def render_objetos(solo=None):
    for k, fn in enumerate(OBJETOS):
        nombre = f'obj_{k}'
        if solo is not None and nombre != solo: continue
        def construir(r, fn=fn):
            _R['r'] = r; fn(r)
        una(nombre, construir, OBJ_LIENZO, OBJ_LIENZO, OBJ_PX / OBJ_LIENZO, centro=(0.0, 0.04))

# ------------------------------------------------------------------ las piezas de la escotilla, el robot, la caja, la esclusa y el carro (1 dp = 1/20 de unidad de Blender; 160 px por unidad = 8 px por dp en la maestra, 4 en el atlas)
DP = 1.0 / 20.0
PX_U = 160.0                                              # la maestra: 160 px por unidad de Blender (la final, 80)

def dp(v): return v * DP

def luces_giradas(grados):
    """Gira todas las luces del estudio [grados] alrededor del eje vertical (para promediar renders sin dirección de luz: lo que gira por código no puede llevar una sombra pegada)."""
    Rz = Matrix.Rotation(math.radians(grados), 4, 'Z')
    for o in bpy.data.objects:
        if o.type == 'LIGHT': o.matrix_world = Rz @ o.matrix_world

def sin_direccion(nombre, construir, ancho, alto, centro=(0.0, 0.0)):
    for k in range(4):
        una(f'{nombre}_d{k}', construir, ancho, alto, PX_U, centro=centro); luces_giradas(90.0)

def sin_suelo():
    bpy.data.objects['juego_suelo'].hide_render = True

CAJA_HATCH = 2.85                                          # la caja de la hoja: 57 dp
PUERTA = '#D2CAF7'                                         # la hoja de la puerta (Door0 → Door1 del boceto, a la mitad)
HOJA_R = dp(26.0)                                          # HatchR − 1

def contorno_hoja(n=90):
    """La hoja: la mitad de arriba (y > 0) del disco de la escotilla."""
    return ccw(arco(0.0, 0.0, HOJA_R, 0, 180, n))

def hoja(r, presionada=False):
    _R['r'] = r; sin_suelo()
    col = oscurece(PUERTA, PRES_OSCURA) if presionada else PUERTA
    cont = contorno_hoja()
    if presionada:
        pieza_huella(cont, 0.1, 0.05, col, 'hoja_p', padre=r, huella=(0.0, 0.70, 0.20), anillos=56, n=180, fino=0.93, redondea=False)
    else:
        capa(cont, col, 0.1, 0.05, 'hoja')
    capa(trazo(arco(0.0, 0.0, dp(19.0), 14, 166, 24), 0.05), oscurece(col, 0.82), 0.13, 0.015, 'hoja_aro')
    esfera(0.13, TINTA, loc=(0.0, dp(6.0), 0.1), escala=(1, 1, 0.5), nombre='hoja_tirador', material=tinta(), padre=r)

def disco_puerta(r):
    _R['r'] = r; sin_suelo()
    capa(circ(0.0, 0.0, HOJA_R, 96), PUERTA, 0.1, 0.05, 'puerta')
    ar = circ(0.0, 0.0, dp(19.0), 60)
    capa(trazo(ar + [ar[0]], 0.05), oscurece(PUERTA, 0.82), 0.13, 0.015, 'puerta_aro')
    esfera(0.13, TINTA, loc=(0.0, dp(6.0), 0.1), escala=(1, 1, 0.5), nombre='puerta_tirador', material=tinta(), padre=r)

def render_escotilla():
    if SOLO in ('todo', 'escotilla', 'marco'):
        def marco(r):
            disco(dp(32.0), 0.12, 0.07, material=mat_oscuro('m_j_bod_marco', '#27306A'), nombre='marco', padre=r)
        una('marco', marco, dp(69.0), dp(69.0), PX_U, centro=(0.0, 0.03))
    if SOLO in ('todo', 'escotilla', 'aro'):
        def borde(r):
            sin_suelo(); aro_plano(dp(27.5), 0.085, material=mat_oscuro('m_j_bod_aro', '#4A5594'), loc=(0, 0, 0.1), nombre='aro', padre=r)
        una('aro', borde, dp(69.0), dp(69.0), PX_U, centro=(0.0, 0.05))
    if SOLO in ('todo', 'escotilla', 'interior'):
        for k, col in enumerate(('#171B3A', '#4C3E6E', '#27306A')):
            def interior(r, col=col):
                sin_suelo(); disco(dp(28.0), 0.04, 0.02, material=mat_oscuro('m_j_bod_int' + col.strip('#'), col), nombre='interior', padre=r)
            una(f'interior_{k}', interior, dp(56.0), dp(56.0), PX_U, centro=(0.0, 0.01))
    if SOLO in ('todo', 'escotilla', 'puerta'):
        sin_direccion('puerta', lambda r: hoja(r, False), CAJA_HATCH, CAJA_HATCH, centro=(0.0, 0.0))
        sin_direccion('puerta_p', lambda r: hoja(r, True), CAJA_HATCH, CAJA_HATCH, centro=(0.0, 0.0))
        sin_direccion('puerta_disco', disco_puerta, CAJA_HATCH, CAJA_HATCH, centro=(0.0, 0.0))

# ------------------------------------------------------------------ el robot
def render_robot():
    if SOLO not in ('todo', 'robot'): return
    def cuerpo(r): sin_suelo(); esfera(dp(26.0), '#E4DEFA', loc=(0, 0, dp(26.0)), nombre='robot', seg=96)
    una('robot', cuerpo, dp(59.0), dp(59.0), PX_U, centro=(0.0, dp(26.0) * math.sin(math.radians(TILT))))
    def visor(r):
        _R['r'] = r; sin_suelo(); capa(elip(0.0, 0.0, dp(15.0), dp(9.0), 64), '#163A5C', 0.14, 0.07, 'visor', panza=0.02, material=mat_oscuro('m_j_bod_visor', '#163A5C'))
    una('visor', visor, dp(32.0), dp(20.0), PX_U, centro=(0.0, 0.035))
    def ojo(r): sin_suelo(); esfera(dp(3.0), '#7FD8FF', loc=(0, 0, dp(3.0)), nombre='ojo', seg=40)
    una('ojo', ojo, dp(10.0), dp(10.0), PX_U, centro=(0.0, dp(3.0) * math.sin(math.radians(TILT))))
    def tallo(r):
        _R['r'] = r; sin_suelo(); capa(trazo([(0.0, -dp(3.6)), (0.0, dp(3.6))], dp(2.4)), None, 0.07, 0.025, 'tallo', material=tinta())
    una('tallo', tallo, dp(6.0), dp(14.0), PX_U, centro=(0.0, 0.02))
    def bola(r): sin_suelo(); esfera(dp(4.0), SUN, loc=(0, 0, dp(4.0)), nombre='bola', seg=40)
    una('bola', bola, dp(10.0), dp(10.0), PX_U, centro=(0.0, dp(4.0) * math.sin(math.radians(TILT))))

# ------------------------------------------------------------------ la caja, la esclusa y el carro de reparto
def render_resto():
    if SOLO in ('todo', 'resto', 'caja'):
        def caja(r):
            _R['r'] = r
            capa(rr(0.0, 0.0, dp(15.0), dp(12.0), dp(5.0)), '#C9A26B', 0.24, 0.1, 'caja', panza=0.01)
            capa(rr(0.0, dp(2.0), dp(15.0), dp(1.7), 0.03), '#8B6F47', 0.27, 0.03, 'caja_cinta')          # UNA cinta de lado a lado (la del dibujo de siempre cruzaba en «+»: se leía como cruz)
        una('caja', caja, dp(38.0), dp(32.0), PX_U, centro=(0.0, 0.05))
    if SOLO in ('todo', 'resto', 'esclusa'):
        def esclusa(r):
            _R['r'] = r; sin_suelo()
            aro_plano(dp(32.5), 0.1, '#FFC94A', loc=(0, 0, 0.1), nombre='esclusa_oro', padre=r)               # el borde dorado: de 30,5 a 34,5 dp (el del dibujo de siempre, de 31,5 a 34,5)
            disco(dp(31.5), 0.08, 0.03, material=mat_oscuro('m_j_bod_vidrio', '#2A2350'), nombre='esclusa_vidrio', padre=r)
            disco(dp(27.0), 0.1, 0.03, material=mat_oscuro('m_j_bod_cielo', '#0F1E52'), nombre='esclusa_cielo', padre=r)
            esfera(dp(13.0), '#7FD8FF', loc=(dp(8.0), dp(-8.0), 0.1 + dp(13.0)), nombre='esclusa_planeta', seg=64)
            a_ = aro_plano(dp(16.0), 0.04, '#E9F4FF', loc=(dp(8.0), dp(-8.0), 0.1 + dp(13.0)), nombre='esclusa_anillo', padre=r); a_.scale = (1.0, 0.22, 1.0); a_.rotation_euler = (0, 0, 0.4)
            for (sx, sy, sr) in ((-12.0, 10.0, 1.4), (-4.0, 16.0, 1.0), (-16.0, -4.0, 1.1), (14.0, 12.0, 0.9)):
                esfera(dp(sr) * 1.3, '#FFFFFF', loc=(dp(sx), dp(sy), 0.1 + dp(sr) * 1.3), nombre='esclusa_estrella', seg=24)
        una('esclusa', esclusa, dp(70.0), dp(70.0), PX_U, centro=(0.0, 0.05))
    if SOLO in ('todo', 'resto', 'carro'):
        for k, (anillo, w) in enumerate((('#3A4278', 2.0), ('#8FEDC8', 3.0), ('#8A82B8', 2.0))):
            def carro(r, anillo=anillo, w=w):
                sin_suelo(); disco(dp(19.0), 0.06, 0.03, material=mat_oscuro('m_j_bod_carro', '#141B3A'), nombre='carro', padre=r)
                aro_plano(dp(19.0 - w / 2.0), dp(w / 2.0), anillo, loc=(0, 0, 0.06 + dp(w / 2.0) * 0.5), nombre='carro_aro', padre=r)
            una(f'carro_{k}', carro, dp(40.0), dp(40.0), PX_U, centro=(0.0, 0.04))

preparar()
if SOLO in ('todo', 'obj'): render_objetos()
elif SOLO.startswith('obj_'): render_objetos(SOLO)
render_escotilla(); render_robot(); render_resto()
termina()
