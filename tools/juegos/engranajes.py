# -*- coding: utf-8 -*-
"""Engranajes «Taller de reparación» en plasticina (Tarea 112): los engranajes (grande de 15 dientes y chico de 10, en las tres paletas: motor azul, camino lila y pieza dorada) con su aro y sus 5 o 3 rayos, el radar y la turbina (que giran con su engranaje),
la polea, el portón, el marco, la plataforma y la caja de la carga, la barra dentada, el casco del cohete y las flechas de giro (las del motor, las del cartel) y los triángulos de los carteles y de «Arrancar». Los dibujos son los de `EngranajesSprites` (dp, 1 dp = 1/20 de unidad de Blender).

Uso: blender -b -P tools/juegos/engranajes.py -- --solo giran|engranajes|fijas [--tilt 0 para lo que GIRA] [--samples N] [--out DIR]
  - `--solo giran --tilt 0`: lo que GIRA por código (los 6 engranajes, el estado presionado del motor, el radar y la turbina): visto exactamente desde arriba (sin cara de adelante que gire con la pieza), SIN suelo (nada de sombra que gire) y promediando CUATRO renders con la luz del estudio
    girada 0°, 90°, 180° y 270° (`empaqueta.py engranajes`: `promedia_cuatro`): ni la luz ni la sombra giran con la pieza, como la hoja de la puerta de Bodega.
  - `--solo fijas` (inclinación 30° del kit): lo que no gira (polea, portón, marco y caja de la carga, barra dentada, casco, flechas y triángulos).
LOS DIENTES SON LA MECÁNICA: el contorno es el MISMO trapecio de `EngranajesSprites.GearPolygon` (punta ±0,17 y raíz ±0,32 del paso angular, holgura cero contra el vecino). La plasticina es una placa de relieve con el canto blando HACIA ADENTRO, vista desde arriba: el contorno
exterior de los dientes es el de siempre, así que no puede ensancharlos. Los huecos entre dientes quedan transparentes (detrás va el disco oscuro de apoyo, como hoy).
"""
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from plasticina import *
from mathutils import Matrix, Vector

SOLO = arg('--solo', 'todo')
UNO = arg('--uno', None)                                  # solo las piezas cuyo nombre empieza así (para probar una sola: `--uno gear_0_0`)
DP = 1.0 / 20.0
PX_U = 160.0                                              # 8 píxeles por dp en la maestra (el atlas, 3,5)
PX_G = 112.0                                              # los engranajes (32 renders) a 5,6 píxeles por dp: de ahí se baja a 3,5 con precisión de sub-píxel y tarda la mitad
T = math.radians(TILT)
TINTA = '#1A1240'

# ------------------------------------------------------------------ geometría: los dibujos vienen en dp con y HACIA ARRIBA (como los sprites de EngranajesSprites); se pasan a unidades de Blender sin voltear
def ccw(pts):
    a = sum(pts[i][0] * pts[(i + 1) % len(pts)][1] - pts[(i + 1) % len(pts)][0] * pts[i][1] for i in range(len(pts)))
    return list(pts) if a > 0 else list(reversed(pts))

def U(pts): return ccw([(x * DP, y * DP) for (x, y) in pts])                              # dp (y arriba) → unidades de Blender

def circ_dp(cx, cy, r, n=72): return [(cx + r * math.cos(2 * math.pi * i / n), cy + r * math.sin(2 * math.pi * i / n)) for i in range(n)]

def elip_dp(cx, cy, rx, ry, ang=0.0, n=48):
    c, s = math.cos(ang), math.sin(ang)
    return [(cx + rx * math.cos(2 * math.pi * i / n) * c - ry * math.sin(2 * math.pi * i / n) * s, cy + rx * math.cos(2 * math.pi * i / n) * s + ry * math.sin(2 * math.pi * i / n) * c) for i in range(n)]

def rr_dp(cx, cy, hx, hy, r=0.0, n=7):
    r = max(0.5, min(r, hx, hy)); out = []
    for (sx, sy, a0) in ((1, 1, 0.0), (-1, 1, 0.5 * math.pi), (-1, -1, math.pi), (1, -1, 1.5 * math.pi)):
        ccx, ccy = cx + sx * (hx - r), cy + sy * (hy - r)
        for k in range(n + 1):
            a = a0 + 0.5 * math.pi * k / n; out.append((ccx + r * math.cos(a), ccy + r * math.sin(a)))
    return out

def trazo(pts, w0, w1=None, cap=8):
    """Una cinta de ancho [w0] a [w1] (completo, en unidades de Blender) a lo largo de la polilínea [pts] (unidades de Blender), con las puntas redondas."""
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

def hex_rgb(h): return tuple(int(h.lstrip('#')[i:i + 2], 16) for i in (0, 2, 4))
def mezcla(a, b, t):
    ra, rb = hex_rgb(a), hex_rgb(b); return '#%02X%02X%02X' % tuple(round(ra[i] + (rb[i] - ra[i]) * t) for i in range(3))

_R = {}
def capa(cont, color, h, bis, nombre='p', z0=0.0, material=None):
    return pieza(cont, h, min(bis, h * 0.48), color, nombre, z0=z0, material=material, padre=_R['r'])

def sin_suelo(): bpy.data.objects['juego_suelo'].hide_render = True

def luces_giradas(grados):
    Rz = Matrix.Rotation(math.radians(grados), 4, 'Z')
    for o in bpy.data.objects:
        if o.type == 'LIGHT': o.matrix_world = Rz @ o.matrix_world

def promedio_cuatro(nombre, construir, ancho, alto, centro=(0.0, 0.0), px=None):
    """La pieza renderizada con la luz del estudio girada 0°, 90°, 180° y 270° (`<nombre>_d0..3`): lo que GIRA por código no lleva una luz ni una sombra que giren con ella."""
    if UNO and not nombre.startswith(UNO): return
    for k in range(4):
        una(f'{nombre}_d{k}', construir, ancho, alto, px or PX_U, centro=centro); luces_giradas(90.0)

# ------------------------------------------------------------------ los engranajes (EngranajesSprites.GearPolygon: el MISMO trapecio; el diente 0 apunta a la derecha)
TIP = {0: 33.0, 1: 23.0}                                  # GearSize.Big / Small
TEETH = {0: 15, 1: 10}
DEPTH = 6.0
PAL = {0: ('#CFEFFF', '#5FA3E0'), 1: ('#F1EDFF', '#A49BE0'), 2: ('#FFF1C2', '#E2B24A')}        # motor, camino y ramas, piezas del cohete

def contorno_gear(size):
    tip = TIP[size]; root = tip - DEPTH; n = TEETH[size]; p = 2.0 * math.pi / n; pts = []
    for k in range(n):
        t = k * p
        for (a, r) in ((t - 0.32 * p, root), (t - 0.17 * p, tip), (t + 0.17 * p, tip), (t + 0.32 * p, root)): pts.append((math.cos(a) * r, math.sin(a) * r))
    return pts

def gear(size, pal, presionado=False):
    def construir(r):
        _R['r'] = r; sin_suelo()
        tip = TIP[size]; root = tip - DEPTH; spokes = 5 if size == 0 else 3; ring_r = root * 0.72
        base = mezcla(PAL[pal][0], PAL[pal][1], 0.45)                                        # el color medio: el degradé del boceto va aparte (empaqueta.py), no es luz
        relieve = mezcla(base, TINTA, 0.22)
        if presionado: base = oscurece(base, PRES_OSCURA); relieve = oscurece(relieve, PRES_OSCURA)
        h = 0.14 if presionado else 0.17
        placa = capa(U(contorno_gear(size)), base, h, 0.05, 'gear')                          # la placa: el contorno EXACTO de los dientes; el canto blando va hacia adentro
        for md in placa.modifiers:                                                           # el bisel solo en los cantos de ARRIBA y de abajo (90°): las esquinas verticales de los dientes (74°) quedan vivas. Con el límite de 40° de `pieza` redondeaban la punta
            if md.type == 'BEVEL': md.angle_limit = math.radians(82.0)                       # (−0,3 dp de ancho junto a la punta) y rellenaban la raíz (+0,25 dp): el trapecio exacto manda
        aro_plano(ring_r * DP, 0.9 * DP, relieve, loc=(0, 0, h + 0.012), nombre='gear_aro', padre=r)
        for j in range(spokes):                                                              # los rayos (3 o 5): lo que muestra el giro; un poco más altos que el aro
            a = 2.0 * math.pi * j / spokes
            capa(trazo([(math.cos(a) * tip * 0.2 * DP, math.sin(a) * tip * 0.2 * DP), (math.cos(a) * ring_r * DP, math.sin(a) * ring_r * DP)], 3.2 * DP), relieve, 0.05, 0.02, 'gear_rayo', z0=h)
    return construir

def render_giran():
    for size, box in ((0, 72.0), (1, 52.0)):
        for pal in (0, 1, 2): promedio_cuatro(f'gear_{size}_{pal}', gear(size, pal), box * DP, box * DP, px=PX_G)
        promedio_cuatro(f'gear_{size}_0_p', gear(size, 0, presionado=True), box * DP, box * DP, px=PX_G)           # el motor se toca: su estado presionado (más oscuro y más plano; el contorno no cambia)

    if SOLO == 'engranajes': return                                                                     # solo los engranajes (para rehacerlos sin el radar ni la turbina)

    # el radar de la antena (EngranajesSprites.Dish: 48 × 24; elipse celeste con un punto coral en la punta: ASIMÉTRICO)
    def radar(r):
        _R['r'] = r; sin_suelo()
        capa(U(elip_dp(0.0, 0.0, 20.0, 7.0)), '#BFE9FF', 0.15, 0.06, 'radar')
        capa(U(circ_dp(18.0, 0.0, 3.5, 40)), '#FF8C6B', 0.20, 0.06, 'radar_punto', z0=0.0)
    promedio_cuatro('radar', radar, 48.0 * DP, 24.0 * DP)

    # la turbina (EngranajesSprites.Fan: 40 × 40): CINCO aspas doradas inclinadas y un cubo (con cuatro formaban una cruz «+»; el número de aspas no es una pista del juego)
    def turbina(r):
        _R['r'] = r; sin_suelo()
        for j in range(5):
            th = 2.0 * math.pi * j / 5.0 + math.pi / 2.0
            capa(U(elip_dp(math.cos(th) * 7.2, math.sin(th) * 7.2, 4.5, 8.0, ang=th - math.pi / 2.0 - 0.4)), '#FFC94A', 0.12, 0.05, 'aspa', z0=0.012 * j)
        capa(U(circ_dp(0.0, 0.0, 3.2, 40)), TINTA, 0.22, 0.06, 'turbina_cubo', z0=0.0)
    promedio_cuatro('turbina', turbina, 40.0 * DP, 40.0 * DP)

# ------------------------------------------------------------------ lo que no gira
def polea(presionada=False):
    def construir(r):
        _R['r'] = r; sin_suelo()
        c0, c1 = '#2A2350', '#4A4390'
        if presionada: c0, c1 = oscurece(c0, PRES_OSCURA), oscurece(c1, PRES_OSCURA)
        h = 0.08 if presionada else 0.10
        capa(U(circ_dp(0.0, 0.0, 17.0, 72)), c0, h, 0.04, 'polea_borde')
        capa(U(circ_dp(0.0, 0.0, 12.0, 72)), c1, h + 0.06, 0.04, 'polea_disco', z0=0.0)
        capa(U(circ_dp(0.0, 0.0, 4.0, 40)), TINTA, h + 0.10, 0.03, 'polea_eje', z0=0.0)
    return construir

def porton(r):
    _R['r'] = r; sin_suelo()
    capa(U(rr_dp(0.0, 0.0, 20.0, 24.0, 6.0)), '#8C86B8', 0.10, 0.05, 'porton')
    for ly in (14.0, 5.0, -4.0, -13.0): capa(U(rr_dp(0.0, ly, 14.0, 1.0, 0.8)), '#6F6A9E', 0.14, 0.03, 'porton_raya', z0=0.0)

def marco_carga(r):
    _R['r'] = r; sin_suelo(); pts = rr_dp(0.0, 0.0, 22.0, 32.0, 6.0, n=9); m = pts[:] + [pts[0]]
    mat = mat_kit('m_j_cframe', '#6B5FB8')
    for i in range(len(m) - 1): churro_c((m[i][0] * DP, m[i][1] * DP), (m[i + 1][0] * DP, m[i + 1][1] * DP), 1.0 * DP, 0.08, mat, 'marco_tubo')

def plataforma(r):
    _R['r'] = r; sin_suelo(); capa(U(rr_dp(0.0, 0.0, 18.0, 3.0, 2.0)), '#E9C46A', 0.10, 0.04, 'plataforma')

def caja_carga(r):
    _R['r'] = r; sin_suelo(); capa(U(rr_dp(0.0, 0.0, 14.0, 10.0, 4.0)), '#7FD8FF', 0.12, 0.05, 'caja')

def barra(r):
    _R['r'] = r; sin_suelo()
    capa(U(rr_dp(0.0, 0.0, 4.5, 40.0, 3.0)), '#8E83D8', 0.10, 0.04, 'barra')
    for ty in range(-36, 40, 8): capa(U(rr_dp(-5.5, -(ty + 2.0), 2.0, 2.0, 0.4)), '#4B438F', 0.12, 0.03, 'barra_diente', z0=0.0)

def casco(r):
    _R['r'] = r; sin_suelo()
    HX, HY = 306.0, 267.0
    pts = []
    def add(bx, by): pts.append((bx - HX, HY - by))                                          # coordenadas del boceto (y abajo) → locales con y arriba
    add(262, 428); add(262, 150)
    for i in range(1, 15):
        t = i / 14.0; u = 1 - t; add(u * u * 262 + 2 * u * t * 262 + t * t * 306, u * u * 150 + 2 * u * t * 122 + t * t * 104)
    for i in range(1, 15):
        t = i / 14.0; u = 1 - t; add(u * u * 306 + 2 * u * t * 350 + t * t * 350, u * u * 104 + 2 * u * t * 122 + t * t * 150)
    add(350, 428)
    capa(U(pts), '#E6E1FB', 0.20, 0.08, 'casco')
    for (a, b, c) in (((262, 368), (252, 440), (262, 428)), ((350, 368), (360, 440), (350, 428))):                    # las aletas rojas (se ven por fuera del casco)
        capa(U([(a[0] - HX, HY - a[1]), (b[0] - HX, HY - b[1]), (c[0] - HX, HY - c[1])]), '#C93C3C', 0.18, 0.06, 'aleta', z0=0.0)
    capa(U(rr_dp(0.0, HY - 433.0, 18.0, 5.0, 3.0)), '#4A5080', 0.16, 0.05, 'tobera', z0=0.0)

# ------------------------------------------------------------------ las flechas de giro y los triángulos (se tiñen con Image.color: plasticina gris claro)
CLARO = '#F2F1FA'

def punta_flecha(r, a1, size):
    tx, ty = math.cos(a1) * r, math.sin(a1) * r; tan = a1 + math.pi / 2.0
    return [(tx + math.cos(tan) * size * 1.5, ty + math.sin(tan) * size * 1.5), (tx + math.cos(tan + 2.4) * size * 1.35, ty + math.sin(tan + 2.4) * size * 1.35), (tx + math.cos(tan - 2.4) * size * 1.35, ty + math.sin(tan - 2.4) * size * 1.35)]

def flecha(radio, ancho, a0, a1, tam_punta, horario):
    """El arco con punta de EngranajesSprites.Arrow / MiniArrow (dibujado con y hacia abajo; aquí se pasa a y arriba). [horario] = como el reloj; la otra es su ESPEJO (la punta va al otro lado)."""
    def construir(r):
        _R['r'] = r; sin_suelo()
        arco = [(radio * math.cos(a0 + (a1 - a0) * i / 40.0), radio * math.sin(a0 + (a1 - a0) * i / 40.0)) for i in range(41)]
        punta = punta_flecha(radio, a1, tam_punta)
        voltea = (lambda p: (p[0], -p[1])) if horario else (lambda p: (-p[0], -p[1]))
        capa(trazo([(voltea(p)[0] * DP, voltea(p)[1] * DP) for p in arco], ancho * DP), CLARO, 0.09, 0.035, 'flecha_arco')
        capa(U([voltea(p) for p in punta]), CLARO, 0.09, 0.035, 'flecha_punta', z0=0.0)
    return construir

def triangulo(pts):
    def construir(r):
        _R['r'] = r; sin_suelo(); capa(U(pts), CLARO, 0.10, 0.04, 'triangulo')
    return construir

ARC0, ARC1 = -math.pi * 0.775, -math.pi * 0.225

def render_fijas():
    una('polea', polea(False), 40.0 * DP, 40.0 * DP, PX_U, centro=(0.0, 0.04))
    una('polea_p', polea(True), 40.0 * DP, 40.0 * DP, PX_U, centro=(0.0, 0.035))
    una('porton', porton, 44.0 * DP, 52.0 * DP, PX_U, centro=(0.0, 0.04))
    una('marco_carga', marco_carga, 48.0 * DP, 68.0 * DP, PX_U, centro=(0.0, 0.04))
    una('plataforma', plataforma, 40.0 * DP, 10.0 * DP, PX_U, centro=(0.0, 0.04))
    una('caja_carga', caja_carga, 32.0 * DP, 24.0 * DP, PX_U, centro=(0.0, 0.05))
    una('barra', barra, 20.0 * DP, 84.0 * DP, PX_U, centro=(0.0, 0.04))
    una('casco', casco, 116.0 * DP, 358.0 * DP, PX_U, centro=(0.0, 0.05))
    for nombre, radio in (('grande', TIP[0] + 8.0), ('chico', TIP[1] + 8.0)):                       # las flechas del motor: una por tamaño de engranaje y sentido
        caja = 2.0 * (radio + 14.0)
        for horario, suf in ((True, 'cw'), (False, 'ccw')):
            una(f'flecha_{nombre}_{suf}', flecha(radio, 4.0, ARC0, ARC1, 6.0, horario), caja * DP, caja * DP, PX_U / 2.0, centro=(0.0, 0.04))
    for horario, suf in ((True, 'cw'), (False, 'ccw')):                                              # la flechita del cartel (casi una vuelta entera)
        una(f'miniflecha_{suf}', flecha(6.5, 2.6, -math.pi * 1.15, math.pi * 0.15, 4.0, horario), 22.0 * DP, 22.0 * DP, PX_U, centro=(0.0, 0.04))
    una('tri_arriba', triangulo([(0.0, 14.0), (12.0, -6.0), (-12.0, -6.0)]), 56.0 * DP, 56.0 * DP, PX_U / 2.0, centro=(0.0, 0.04))
    una('tri_abajo', triangulo([(0.0, -14.0), (-12.0, 6.0), (12.0, 6.0)]), 56.0 * DP, 56.0 * DP, PX_U / 2.0, centro=(0.0, 0.04))
    una('tri_play', triangulo([(-9.0, 14.0), (-9.0, -14.0), (13.0, 0.0)]), 56.0 * DP, 56.0 * DP, PX_U / 2.0, centro=(0.0, 0.04))

preparar()
if SOLO in ('todo', 'giran', 'engranajes'): render_giran()
if SOLO in ('todo', 'fijas'): render_fijas()
termina()
