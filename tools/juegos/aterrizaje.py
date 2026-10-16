# -*- coding: utf-8 -*-
"""Aterrizaje Lunar en plasticina (Tarea 111): la nave, el gallardete de la bandera, la Tierra, el satélite, la cúpula del marcador (encendida y apagada) y la regla (una losa crema de lado a lado, sin marcas). Las marcas de extremo y del medio, los números y
el asta de la bandera NO se tocan: se quedan procedurales, en tinta y exactamente en el mismo x (la regla manda: docs/diseno-aterrizaje.md y docs/plasticina-juegos.md §13). La luna y las cordilleras también (están pegadas a la regla y nada decorativo puede ir junto a ella).

Uso: blender -b -P tools/juegos/aterrizaje.py -- [--solo nave|gallardete|tierra|satelite|domo|regla] [--samples N] [--out DIR]
Salida (en --out, por defecto tools/juegos/out): las «maestras» con fondo transparente (`nave`, `gallardete`, `tierra`, `satelite`, `domo_on`, `domo_off`, `regla`); `empaqueta.py aterrizaje` las reduce, SIMETRIZA la nave (su centro en x es la respuesta) y arma el atlas.

Cada pieza sale en el MISMO lienzo y a la MISMA escala que el dibujo procedural al que reemplaza (nave: cuadro de 72 × 72 dp; gallardete 36 × 24; Tierra 128 × 128 con la esfera de 116; satélite 32 × 12; cúpula 28 × 20): el controlador no cambia ninguna medida.
Todo se modela en dp (1 dp = 1/20 de unidad de Blender), con los dibujos del boceto (x a la derecha, y HACIA ABAJO, origen en el centro de cada cuadro).
"""
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from plasticina import *
from mathutils import Vector, Matrix
import bmesh

SOLO = arg('--solo', 'todo')
DP = 1.0 / 20.0
PX_U = 160.0                                              # 8 píxeles por dp en la maestra (el atlas, 4)
CREMA_REGLA = '#F3EBD3'                                   # LandingSprites.RulerColor
T = math.radians(TILT)
VISTA = Vector((0.0, -math.sin(T), math.cos(T)))          # de la esfera hacia la cámara
ARRIBA = Vector((0.0, math.cos(T), math.sin(T)))          # «arriba» de la pantalla, en el mundo

# ------------------------------------------------------------------ geometría: los dibujos vienen en dp con y HACIA ABAJO; aquí se pasan a unidades de Blender con y arriba
def ccw(pts):
    a = sum(pts[i][0] * pts[(i + 1) % len(pts)][1] - pts[(i + 1) % len(pts)][0] * pts[i][1] for i in range(len(pts)))
    return list(pts) if a > 0 else list(reversed(pts))

def K(pts): return ccw([(x * DP, -y * DP) for (x, y) in pts])

def circ_dp(cx, cy, r, n=72): return [(cx + r * math.cos(2 * math.pi * i / n), cy + r * math.sin(2 * math.pi * i / n)) for i in range(n)]

def rr_dp(cx, cy, hx, hy, r=0.0, n=7):
    r = max(0.5, min(r, hx, hy)); out = []
    for (sx, sy, a0) in ((1, 1, 0.0), (-1, 1, 0.5 * math.pi), (-1, -1, math.pi), (1, -1, 1.5 * math.pi)):
        ccx, ccy = cx + sx * (hx - r), cy + sy * (hy - r)
        for k in range(n + 1):
            a = a0 + 0.5 * math.pi * k / n; out.append((ccx + r * math.cos(a), ccy + r * math.sin(a)))
    return out

def trazo(pts, w0, w1=None, cap=8):
    """Una cinta de ancho [w0] a [w1] (completo, en unidades de Blender) a lo largo de la polilínea [pts] (unidades de Blender, y arriba), con las puntas redondas."""
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

_R = {}
def capa(cont, color, h, bis, nombre='p', z0=0.0, panza=0.0, material=None):
    return pieza(cont, h, min(bis, h * 0.48), color, nombre, z0=z0, panza=panza, material=material, padre=_R['r'])

def sin_suelo(): bpy.data.objects['juego_suelo'].hide_render = True

# ------------------------------------------------------------------ la nave (LandingSprites.RenderLander: cuadro de 72 × 72 dp; SIMÉTRICA respecto de su eje vertical: su centro en x ES la respuesta)
def nave(r):
    """El módulo lunar de arriba: cabina (media circunferencia de 17 dp de radio y un tramo recto), ventanilla celeste, base sol y cuatro patas abiertas. Todo en dp con y hacia abajo, origen en el centro del cuadro. Flota (sin sombra de contacto)."""
    _R['r'] = r; sin_suelo()
    for s in (-1, 1):                                                                    # las patas: de la base hacia afuera y un pie horizontal (como la nave de siempre, en gris lavanda claro para que se vea sobre el cielo)
        capa(trazo([(s * 12.0 * DP, -8.0 * DP), (s * 24.0 * DP, -24.0 * DP)], 3.4 * DP), '#C9C4EA', 0.10, 0.04, 'pata')
        capa(trazo([(s * 30.0 * DP, -24.0 * DP), (s * 18.0 * DP, -24.0 * DP)], 3.4 * DP), '#C9C4EA', 0.10, 0.04, 'pie')
    capa(K(rr_dp(0.0, 6.0, 22.0, 8.0, 6.0)), '#FFC94A', 0.18, 0.07, 'base', z0=0.0)
    cab = [(-17.0, 0.0), (17.0, 0.0)] + [(17.0 * math.cos(a), -6.0 - 17.0 * math.sin(a)) for a in [math.pi * k / 48.0 for k in range(0, 49)]]
    capa(K(cab), '#E9E6FF', 0.20, 0.08, 'cabina', z0=0.0)
    capa(K(circ_dp(0.0, -9.0, 8.6, 60)), '#BDB8E8', 0.215, 0.04, 'bisel', z0=0.0)                # el aro de la ventanilla
    capa(K(circ_dp(0.0, -9.0, 7.0, 60)), '#7FD8FF', 0.24, 0.05, 'ventanilla', z0=0.0)

def render_nave():
    una('nave', nave, 72.0 * DP, 72.0 * DP, PX_U, centro=(0.0, 0.05))

# ------------------------------------------------------------------ el gallardete (LandingSprites.RenderPennant: cuadro de 36 × 24 dp, vértice de arriba a la izquierda en (3, 3), triángulo de 26 × 15)
def gallardete(r):
    _R['r'] = r; sin_suelo()
    capa(K([(3.0 - 18.0, 3.0 - 12.0), (29.0 - 18.0, 10.0 - 12.0), (3.0 - 18.0, 18.0 - 12.0)]), '#FFC94A', 0.14, 0.05, 'gallardete')

def render_gallardete():
    una('gallardete', gallardete, 36.0 * DP, 24.0 * DP, PX_U, centro=(0.0, 0.04))

# ------------------------------------------------------------------ la Tierra (LandingSprites.RenderEarth: cuadro de 128 dp, esfera de radio 58)
TIERRA_R = 58.0
TIERRAS = ((-0.40, -0.30, 0.30, 0.20), (-0.24, 0.02, 0.15, 0.20), (-0.52, 0.14, 0.17, 0.20), (0.36, 0.30, 0.24, 0.22))           # (x, y hacia abajo, rx, ry) en fracciones del radio
NUBES = ((-0.7, 0.45, -0.1, 0.38, 4.5), (0.05, -0.62, 0.55, -0.5, 4.0), (0.3, -0.05, 0.75, 0.02, 3.5), (-0.35, -0.78, -0.05, -0.82, 3.0))   # (x0, y0, x1, y1 en fracciones del radio con y HACIA ARRIBA; grosor en dp)

def sobre_esfera(centro, R_, xs, ys):
    """El punto de la esfera [R_] (centro [centro], en unidades de Blender) que se ve a (xs, ys) de su centro en la pantalla (ys hacia arriba), y la normal ahí."""
    a, b = xs / R_, ys / R_; c = math.sqrt(max(0.0, 1.0 - a * a - b * b))
    n = Vector((1.0, 0.0, 0.0)) * a + ARRIBA * b + VISTA * c
    return centro + n * R_, n

def orienta(o, normal, tangente):
    """Gira [o] para que su eje Z mire a lo largo de [normal] y su eje X siga [tangente] (aplanada sobre la superficie)."""
    z = normal.normalized(); x = (tangente - z * tangente.dot(z)).normalized(); y = z.cross(x)
    m = Matrix(((x.x, y.x, z.x), (x.y, y.y, z.y), (x.z, y.z, z.z)))
    o.rotation_euler = m.to_euler()

def tierra(r):
    sin_suelo(); R_ = TIERRA_R * DP; centro = Vector((0.0, 0.0, R_))
    esfera(R_, '#4A98E0', loc=tuple(centro), nombre='tierra_oceano', seg=128)
    verde = mat_kit('m_j_tierra_verde', '#96D68C'); blanco = mat_kit('m_j_tierra_nube', '#FFFFFF')
    derecha = Vector((1.0, 0.0, 0.0))
    for (x, y, rx, ry) in TIERRAS:                                                       # las tierras: casquetes achatados pegados a la superficie (medio hundidos)
        pos, n = sobre_esfera(centro, R_, x * R_, -y * R_)
        o = esfera(1.0, nombre='tierra_continente', loc=tuple(pos - n * 0.03), escala=(rx * R_, ry * R_, 0.09), material=verde, seg=48)
        orienta(o, n, derecha)
    for (x0, y0, x1, y1, g) in NUBES:                                                    # las nubes: trazos blancos cortos y achatados
        pos0, n0 = sobre_esfera(centro, R_, x0 * R_, y0 * R_); pos1, n1 = sobre_esfera(centro, R_, x1 * R_, y1 * R_)
        mid = (pos0 + pos1) / 2.0; nm = (n0 + n1).normalized(); largo = (pos1 - pos0).length
        o = esfera(1.0, nombre='tierra_nube', loc=tuple(mid + nm * 0.005), escala=(largo / 2.0 + g * DP, g * DP * 0.9, 0.07), material=blanco, seg=32)
        orienta(o, nm, pos1 - pos0)

def render_tierra():
    una('tierra', tierra, 128.0 * DP, 128.0 * DP, PX_U, centro=(0.0, TIERRA_R * DP * math.sin(T)))

# ------------------------------------------------------------------ el satélite (LandingSprites.RenderSatellite: cuadro de 32 × 12 dp; paneles de 9 × 6 a ±9,5 y un cuerpo de 8 × 8; nada vertical sobre el cuerpo: sin «+»)
def satelite(r):
    _R['r'] = r; sin_suelo()
    capa(K(rr_dp(0.0, 0.0, 6.0, 1.0, 0.5)), '#9C94D6', 0.08, 0.03, 'sat_brazo')
    for sx in (-1, 1):
        capa(K(rr_dp(sx * 9.5, 0.0, 4.5, 3.0, 0.6)), '#3E5BA8', 0.12, 0.04, 'sat_panel')
        capa(trazo([(sx * 9.5 * DP, -2.4 * DP), (sx * 9.5 * DP, 2.4 * DP)], 0.5 * DP), '#2F4A8F', 0.14, 0.01, 'sat_divisoria', z0=0.0)
    capa(K(rr_dp(0.0, 0.0, 4.0, 4.0, 0.9)), '#E9E6FF', 0.16, 0.06, 'sat_cuerpo')

def render_satelite():
    una('satelite', satelite, 32.0 * DP, 12.0 * DP, PX_U, centro=(0.0, 0.04))

# ------------------------------------------------------------------ la cúpula del marcador (LandingSprites.RenderDome: cuadro de 28 × 20 dp; media esfera de 11 dp de radio con centro en (0, 4) y una base recta de 22 × 2 dp en y = 5)
def hemisferio(R_, color, loc, nombre='domo', material=None, seg=64):
    o = esfera(R_, color, loc=loc, nombre=nombre, material=material, seg=seg)
    bm = bmesh.new(); bm.from_mesh(o.data)
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if v.co.z < -1e-5], context='VERTS')
    bm.to_mesh(o.data); bm.free(); o.data.update()
    return o

def domo(color):
    def construir(r):
        _R['r'] = r; sin_suelo()
        capa(K(rr_dp(0.0, 5.0, 11.0, 1.0, 0.8)), '#B9B2E4', 0.10, 0.04, 'domo_base')
        # el dibujo de siempre es una media esfera VISTA DE LADO (un semicírculo sobre una base recta): el hemisferio se acuesta con su polo hacia «arriba de la pantalla» para que su silueta sea ese semicírculo desde la cámara inclinada;
        # su centro (en el borde de arriba de la base, a 4 dp bajo el centro del cuadro) se corrige a mano por la inclinación (las esferas no van dentro de la raíz estirada)
        zc = 0.12; y_up = -4.0 * DP
        h = hemisferio(11.0 * DP, color, (0.0, (y_up - zc * math.sin(T)) / math.cos(T), zc), nombre='domo_cupula')
        h.rotation_euler = (T - math.pi / 2.0, 0.0, 0.0)
    return construir

def render_domo():
    una('domo_on', domo('#7FD8FF'), 28.0 * DP, 20.0 * DP, PX_U, centro=(0.0, 0.04))
    una('domo_off', domo('#3A3486'), 28.0 * DP, 20.0 * DP, PX_U, centro=(0.0, 0.04))

# ------------------------------------------------------------------ la regla (una losa crema de las del kit: radio 7,5 dp para que el cuerpo mida 20 dp de alto; se estira a lo ancho como nine-slice). SIN marcas, rayas ni cráteres: las marcas van en tinta, por código
def render_regla():
    losa('regla', CREMA_REGLA, presionada=False, radio=7.5)

preparar()
if SOLO in ('todo', 'nave'): render_nave()
if SOLO in ('todo', 'gallardete'): render_gallardete()
if SOLO in ('todo', 'tierra'): render_tierra()
if SOLO in ('todo', 'satelite'): render_satelite()
if SOLO in ('todo', 'domo'): render_domo()
if SOLO in ('todo', 'regla'): render_regla()
termina()
