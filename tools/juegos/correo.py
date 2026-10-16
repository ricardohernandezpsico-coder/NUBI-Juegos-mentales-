# -*- coding: utf-8 -*-
"""La estación de correo en plasticina (Tarea 108): las 12 cartas (4 planetas × {normal, sello dorado, lazo}), los 4 planetas de los buzones con su figura, el dial de la caja fuerte (gira por código), la puerta y la manija de la caja fuerte, la torre y la lámpara del faro
(apagada y encendida), la nave del correo y el saco dorado, la tapa del reloj (y la presionada), el fondo del reloj y la marca de «raya» del resumen. Los buzones, la caja fuerte, el faro y las tarjetas son losas del kit; el marco punteado de la cinta, la barra del día,
los haces, los resplandores y las chispas se quedan procedurales.

Uso: blender -b -P tools/juegos/correo.py -- [--solo cartas|planetas|piezas|reloj|carta_P_C] [--samples N] [--out DIR]
Salida (en --out, por defecto tools/juegos/out): las «maestras» con fondo transparente (`carta_P_C`, `planeta_P`, `dial_d0..3`, `caja_puerta`, `manija`, `torre`, `lampara_0/1`, `nave`, `saco`, `reloj_base`, `reloj_tapa`, `reloj_tapa_p`, `marca_raya`); `empaqueta.py correo` las reduce y promedia las cuatro del dial.

LAS CARTAS son el dibujo del boceto (`MailSprites.PaintLetter`; dp, y HACIA ABAJO, origen en el centro de la carta de 112 × 76 dentro de una caja de 128 × 92): papel crema, la doblez, las dos líneas de dirección, el sello y, en la del lazo, la cinta roja con su moño.
CLASIFICAR depende de la FORMA del sello (círculo, triángulo, cuadrado redondeado, gota) a 17 dp: es plana, grande y en el color oscuro del planeta sobre el sello blanco. El sello DORADO es la señal «focal»: 1,2 veces más grande (36 dp), con la misma silueta dentada de 5 mordidas por lado
(`SealShape`, la que fijan las pruebas) y el borde café; el LAZO es la señal «no focal» (no está en el sello). El render no vuelve más ni menos saliente a ninguno: mismas medidas, mismos lugares. 1 dp = 1/20 de unidad de Blender.
"""
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from plasticina import *
from mathutils import Matrix

SOLO = arg('--solo', 'todo')
DP = 1.0 / 20.0
PX_U = 160.0                                              # las piezas chicas: 160 px por unidad de Blender (8 por dp en la maestra, 4 en el atlas)
PX_U_CARTA = 120.0                                        # las cartas: 6 px por dp en la maestra, 3 en el atlas
CREMA = '#F6EFDC'
ROJO = '#E2524A'
ORO = '#FFC94A'
PLANETA = ('#FF8A6B', '#7FD8FF', '#A6E36B', '#B79BFF')           # MailSprites.PlanetCol
OSCURO = ('#C9533A', '#3E9BC4', '#6AA63A', '#7B5FD6')            # MailSprites.PlanetDark

# ------------------------------------------------------------------ geometría: los dibujos vienen en dp con y HACIA ABAJO; aquí se pasan a unidades de Blender con y arriba
def ccw(pts):
    a = sum(pts[i][0] * pts[(i + 1) % len(pts)][1] - pts[(i + 1) % len(pts)][0] * pts[i][1] for i in range(len(pts)))
    return list(pts) if a > 0 else list(reversed(pts))

def K(pts): return ccw([(x * DP, -y * DP) for (x, y) in pts])

def circ_dp(cx, cy, r, n=72): return [(cx + r * math.cos(2 * math.pi * i / n), cy + r * math.sin(2 * math.pi * i / n)) for i in range(n)]
def elip_dp(cx, cy, rx, ry, n=72): return [(cx + rx * math.cos(2 * math.pi * i / n), cy + ry * math.sin(2 * math.pi * i / n)) for i in range(n)]

def rr_dp(cx, cy, hx, hy, r=0.0, n=7):
    r = max(0.5, min(r, hx, hy)); out = []
    for (sx, sy, a0) in ((1, 1, 0.0), (-1, 1, 0.5 * math.pi), (-1, -1, math.pi), (1, -1, 1.5 * math.pi)):
        ccx, ccy = cx + sx * (hx - r), cy + sy * (hy - r)
        for k in range(n + 1):
            a = a0 + 0.5 * math.pi * k / n; out.append((ccx + r * math.cos(a), ccy + r * math.sin(a)))
    return out

def rota_dp(pts, ang, cx=0.0, cy=0.0):
    """Gira [pts] alrededor de (cx, cy) el ángulo [ang] (rad) en el sentido del dibujo (y hacia abajo: positivo = horario en pantalla)."""
    c, s = math.cos(ang), math.sin(ang)
    return [(cx + (x - cx) * c - (y - cy) * s, cy + (x - cx) * s + (y - cy) * c) for (x, y) in pts]

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

def linea_dp(p0, p1, medio_ancho): return trazo([(p0[0] * DP, -p0[1] * DP), (p1[0] * DP, -p1[1] * DP)], medio_ancho * 2 * DP)

def contorno_sdf(f, cx, cy, n=400, rmax=40.0):
    """El contorno de la forma [f] <= 0 (dp, y abajo) que se ve desde (cx, cy): un rayo por ángulo, hasta donde [f] cruza 0. Devuelve los puntos en dp (y abajo)."""
    out = []
    for i in range(n):
        a = 2 * math.pi * i / n; dx, dy = math.cos(a), math.sin(a); lo, hi = 0.0, rmax
        for _ in range(40):
            m = (lo + hi) / 2
            if f(cx + dx * m, cy + dy * m) <= 0: lo = m
            else: hi = m
        out.append((cx + dx * lo, cy + dy * lo))
    return out

# ------------------------------------------------------------------ el sello (MailSprites.SealShape, SealSize = 30, GoldSealScale = 1.2, PerfRadius = 1.9, PerfPitch = 5.5, PerfPerSide = 5)
SEAL, GOLD_K, PERF_R, PERF_P, PERF_N = 30.0, 1.2, 1.9, 5.5, 5

def d_caja(x, y, cx, cy, hx, hy, r):
    qx, qy = abs(x - cx) - (hx - r), abs(y - cy) - (hy - r); return math.hypot(max(qx, 0.0), max(qy, 0.0)) + min(max(qx, qy), 0.0) - r

def sello_mitad(oro): return SEAL * (GOLD_K if oro else 1.0) / 2.0
def sello_cx(oro): return 32.0 if oro else 34.0
def sello_cy(oro): return -14.0 if oro else -16.0

def sello_sdf(oro, x, y, cx, cy):
    half = sello_mitad(oro); d = d_caja(x, y, cx, cy, half, half, 5.0)
    if not oro: return d
    lx, ly = x - cx, y - cy; maxo = PERF_P * (PERF_N // 2)
    ax = max(-maxo, min(maxo, round(lx / PERF_P) * PERF_P)); ay = max(-maxo, min(maxo, round(ly / PERF_P) * PERF_P))
    bite = min(math.hypot(lx - ax, ly + half) - PERF_R, math.hypot(lx - ax, ly - half) - PERF_R, math.hypot(lx + half, ly - ay) - PERF_R, math.hypot(lx - half, ly - ay) - PERF_R)
    return max(d, -bite)

# ------------------------------------------------------------------ las figuras de los planetas (MailSprites.Symbol): 0 círculo, 1 triángulo, 2 cuadrado redondeado, 3 gota
def bezier(p0, p1, p2, p3, n=14): return [((1 - t) ** 3 * p0[0] + 3 * (1 - t) ** 2 * t * p1[0] + 3 * (1 - t) * t * t * p2[0] + t ** 3 * p3[0], (1 - t) ** 3 * p0[1] + 3 * (1 - t) ** 2 * t * p1[1] + 3 * (1 - t) * t * t * p2[1] + t ** 3 * p3[1]) for t in [i / n for i in range(1, n + 1)]]

def figura_dp(planeta, s, cx=0.0, cy=0.0):
    """El contorno (dp, y abajo) de la figura [planeta] de tamaño [s] centrada en (cx, cy)."""
    if planeta == 0: return circ_dp(cx, cy, s * 0.5, 60)
    if planeta == 1: return [(cx, cy - s * 0.55), (cx + s * 0.52, cy + s * 0.4), (cx - s * 0.52, cy + s * 0.4)]
    if planeta == 2: return rr_dp(cx, cy, s * 0.42, s * 0.42, s * 0.16)
    pts = [(0.0, -0.6)] + bezier((0.0, -0.6), (0.35, -0.15), (0.5, 0.1), (0.5, 0.22))
    pts += [(0.0 + 0.5 * math.cos(a * math.pi / 14), 0.22 + 0.5 * math.sin(a * math.pi / 14)) for a in range(1, 15)]
    pts += bezier((-0.5, 0.22), (-0.5, 0.1), (-0.35, -0.15), (0.0, -0.6))
    return [(cx + x * s, cy + y * s) for (x, y) in pts]

# ------------------------------------------------------------------ las capas
_R = {}
def capa(cont, color, h, bis, nombre='p', z0=0.0, panza=0.0, material=None):
    return pieza(cont, h, min(bis, h * 0.48), color, nombre, z0=z0, panza=panza, material=material, padre=_R['r'])

def sin_suelo(): bpy.data.objects['juego_suelo'].hide_render = True

def luces_giradas(grados):
    Rz = Matrix.Rotation(math.radians(grados), 4, 'Z')
    for o in bpy.data.objects:
        if o.type == 'LIGHT': o.matrix_world = Rz @ o.matrix_world

def sin_direccion(nombre, construir, ancho, alto, centro=(0.0, 0.0)):
    for k in range(4):
        una(f'{nombre}_d{k}', construir, ancho, alto, PX_U, centro=centro); luces_giradas(90.0)

VISTA = Vector((0.0, -math.sin(math.radians(TILT)), math.cos(math.radians(TILT))))
T = math.radians(TILT)

# ------------------------------------------------------------------ las cartas
def carta(planeta, cue):
    """cue: 0 normal, 1 sello dorado, 2 lazo."""
    def construir(r):
        _R['r'] = r; oro = cue == 1
        capa(K(rr_dp(0.0, 0.0, 56.0, 38.0, 10.0)), CREMA, 0.10, 0.045, 'papel')                                       # el papel de 112 × 76 dp
        capa(trazo([(x * DP, -y * DP) for (x, y) in ((-50.0, -32.0), (0.0, 4.0), (50.0, -32.0))], 0.05), '#D4CBB0', 0.115, 0.012, 'doblez')
        x0 = -56.0 + 14.0 + (24.0 if cue == 2 else 0.0)
        for (y, largo) in ((10.0, 42.0), (20.0, 32.0)): capa(linea_dp((x0, y), (x0 + largo, y), 1.5), '#A9A4BC', 0.115, 0.012, 'direccion')
        if cue == 2:                                                                                                     # el lazo: la cinta vertical, dos lazadas inclinadas y el nudo (un moño, nunca un «+»)
            bx = -56.0 * 0.36
            capa(K(rr_dp(bx, 0.0, 5.0, 38.0, 1.0)), ROJO, 0.15, 0.05, 'cinta')
            for s in (-1, 1): capa(K(rota_dp(elip_dp(bx + s * 9.0, -76.0 * 0.05, 9.0, 6.0, 48), -s * 0.4, bx + s * 9.0, -76.0 * 0.05)), ROJO, 0.2, 0.07, 'lazada')
            capa(K(circ_dp(bx, -76.0 * 0.05, 4.0, 40)), oscurece(ROJO, 0.85), 0.26, 0.07, 'nudo')
        cx, cy, half = sello_cx(oro), sello_cy(oro), sello_mitad(oro)
        if oro:
            borde = contorno_sdf(lambda x, y: sello_sdf(True, x, y, cx, cy), cx, cy, 720, 30.0)
            dentro = contorno_sdf(lambda x, y: sello_sdf(True, x, y, cx, cy) + 2.5, cx, cy, 720, 30.0)
            capa(K(borde), '#B07A12', 0.08, 0.025, 'sello_borde', z0=0.09)                                              # el borde café va todo hacia adentro: las mordidas dejan ver el papel
            capa(K(dentro), ORO, 0.08, 0.03, 'sello', z0=0.15)
            z_fig = 0.22
        else:
            capa(K(rr_dp(cx, cy, half + 1.0, half + 1.0, 6.0)), None, 0.04, 0.015, 'sello_tinta', z0=0.09, material=tinta())    # el borde de tinta de 1 dp
            capa(K(rr_dp(cx, cy, half, half, 5.0)), '#FFFFFF', 0.09, 0.03, 'sello', z0=0.09)
            z_fig = 0.17
        capa(K(figura_dp(planeta, 17.0 * half / sello_mitad(False), cx, cy)), OSCURO[planeta], 0.06, 0.02, 'figura', z0=z_fig, material=mat_oscuro(f'm_j_cor_figura_{planeta}', OSCURO[planeta]))        # color oscuro: sin el velo gris del brillo normal (la figura se lee por la forma y el contraste contra el sello blanco)
    return construir

def render_cartas(solo=None):
    for p in range(4):
        for c in range(3):
            nombre = f'carta_{p}_{c}'
            if solo is not None and nombre != solo: continue
            una(nombre, carta(p, c), 128.0 * DP, 92.0 * DP, PX_U_CARTA, centro=(0.0, 0.05))

# ------------------------------------------------------------------ los planetas de los buzones (una esfera de 28 dp de radio con su figura en blanco, en relieve)
def planeta(p):
    def construir(r):
        sin_suelo(); R_ = 28.0 * DP
        esfera(R_, PLANETA[p], loc=(0, 0, R_), nombre='planeta', seg=96)
        cont = K(figura_dp(p, 28.0 * 0.9))
        o = pieza(cont, 0.22, 0.05, '#FFFFFF', 'figura', z0=0.0, material=None, padre=None)                              # la placa se pone mirando a la cámara, a ras de la esfera
        o.rotation_euler = (T, 0, 0); o.location = tuple(Vector((0.0, 0.0, R_)) + VISTA * (R_ - 0.16))
    return construir

def render_planetas():
    for p in range(4):
        una(f'planeta_{p}', planeta(p), 64.0 * DP, 64.0 * DP, PX_U, centro=(0.0, 28.0 * DP * math.sin(T)))

# ------------------------------------------------------------------ las piezas de la estación
def render_piezas():
    if SOLO in ('todo', 'piezas', 'dial'):
        def dial(r):
            _R['r'] = r; sin_suelo()
            disco(13.0 * DP, 0.06, 0.03, material=mat_oscuro('m_j_cor_dial', '#2A2350'), nombre='dial', padre=r)
            aro_plano(12.4 * DP, 1.25 * DP, ORO, loc=(0, 0, 0.06 + 1.25 * DP * 0.4), nombre='dial_aro', padre=r)
            zm = 0.06 + 1.4 * DP * 1.2                                                                                   # las marcas son esferas (no van dentro de la raíz estirada): se corrigen a mano la inclinación y la altura para que caigan sobre el círculo de 9 dp
            for k in range(8):
                a = k * math.pi / 4
                esfera(1.4 * DP * 1.25, ORO, loc=(math.cos(a) * 9.0 * DP, (math.sin(a) * 9.0 * DP - zm * math.sin(T)) / math.cos(T), zm), nombre='dial_marca', seg=20)
            disco(3.5 * DP, 0.1, 0.04, ORO, loc=(0, 0, 0.04), nombre='dial_centro', padre=r)
        sin_direccion('dial', dial, 32.0 * DP, 32.0 * DP, centro=(0.0, 0.02))
    if SOLO in ('todo', 'piezas', 'puerta'):
        def puerta(r):
            _R['r'] = r; capa(K(rr_dp(0.0, 0.0, 31.0, 34.0, 11.5)), '#5A5280', 0.1, 0.05, 'puerta', material=mat_oscuro('m_j_cor_puerta', '#5A5280'))
        una('caja_puerta', puerta, 66.0 * DP, 72.0 * DP, PX_U, centro=(0.0, 0.03))
        def manija(r):
            _R['r'] = r; sin_suelo(); capa(K(rr_dp(0.0, 0.0, 14.0, 3.5, 3.5)), ORO, 0.08, 0.035, 'manija')
        una('manija', manija, 32.0 * DP, 12.0 * DP, PX_U, centro=(0.0, 0.02))
    if SOLO in ('todo', 'piezas', 'faro'):
        def torre(r):
            _R['r'] = r
            capa(K([(-12.0, 26.0), (-8.0, -12.0), (8.0, -12.0), (12.0, 26.0)]), '#EDEAFB', 0.14, 0.06, 'torre', panza=0.01)
            capa(K(rr_dp(0.0, 5.5, 9.2, 3.5, 0.5)), ROJO, 0.17, 0.04, 'franja_a', z0=0.0)
            capa(K(rr_dp(0.0, -5.5, 8.2, 2.5, 0.5)), ROJO, 0.17, 0.04, 'franja_b', z0=0.0)
        una('torre', torre, 44.0 * DP, 66.0 * DP, PX_U, centro=(0.0, 0.04))
        for k, col in enumerate(('#6E6A9A', '#FFE27A')):
            def lampara(r, col=col):
                sin_suelo(); R_ = 9.0 * DP; esfera(R_, col, loc=(0, 0, R_), nombre='lampara', seg=48)
            una(f'lampara_{k}', lampara, 28.0 * DP, 28.0 * DP, PX_U, centro=(0.0, 9.0 * DP * math.sin(T)))
    if SOLO in ('todo', 'piezas', 'nave'):
        def nave(r):
            _R['r'] = r; sin_suelo()
            pts = [(-38.0, 4.0)]
            def quad(p0, q, p1, n=12): return [((1 - t) ** 2 * p0[0] + 2 * (1 - t) * t * q[0] + t * t * p1[0], (1 - t) ** 2 * p0[1] + 2 * (1 - t) * t * q[1] + t * t * p1[1]) for t in [i / n for i in range(1, n + 1)]]
            pts += quad((-38.0, 4.0), (-36.0, -16.0), (0.0, -16.0)); pts += quad((0.0, -16.0), (30.0, -16.0), (40.0, 0.0)); pts += quad((40.0, 0.0), (30.0, 14.0), (0.0, 14.0)); pts += quad((0.0, 14.0), (-36.0, 14.0), (-38.0, 4.0))
            capa(K(pts), '#ECE8FF', 0.16, 0.07, 'nave', panza=0.02)
            capa(K(circ_dp(20.0, -3.0, 6.0, 40)), '#7FD8FF', 0.2, 0.05, 'ventanilla', z0=0.0)
            capa(K(rr_dp(-7.0, -0.5, 11.0, 7.5, 3.0)), ORO, 0.2, 0.05, 'sobre', z0=0.04)
            capa(trazo([(x * DP, -y * DP) for (x, y) in ((-18.0, -8.0), (-7.0, 0.0), (4.0, -8.0))], 0.05), None, 0.215, 0.012, 'solapa', material=tinta(), z0=0.06)
        una('nave', nave, 100.0 * DP, 44.0 * DP, PX_U, centro=(0.0, 0.05))
        def saco(r):
            _R['r'] = r; sin_suelo()
            def quad(p0, q, p1, n=12): return [((1 - t) ** 2 * p0[0] + 2 * (1 - t) * t * q[0] + t * t * p1[0], (1 - t) ** 2 * p0[1] + 2 * (1 - t) * t * q[1] + t * t * p1[1]) for t in [i / n for i in range(1, n + 1)]]
            pts = [(-14.0, -6.0)] + quad((-14.0, -6.0), (-18.0, 16.0), (0.0, 16.0)) + quad((0.0, 16.0), (18.0, 16.0), (14.0, -6.0))
            capa(K(pts), '#E8B85A', 0.2, 0.09, 'saco', panza=0.03)
            capa(K(rr_dp(0.0, -7.5, 9.0, 2.5, 1.0)), '#C98A2E', 0.26, 0.05, 'saco_cinta', z0=0.0)
        una('saco', saco, 40.0 * DP, 40.0 * DP, PX_U, centro=(0.0, 0.05))
    if SOLO in ('todo', 'piezas', 'marca'):
        def raya(r):
            _R['r'] = r; sin_suelo()
            aro_plano(12.0 * DP, 1.5 * DP * 1.3, '#FFB4A0', loc=(0, 0, 1.5 * DP * 1.3), nombre='marca_aro', padre=r)
            capa(trazo([(-6.0 * DP, 0.0), (6.0 * DP, 0.0)], 3.5 * DP), '#FFB4A0', 0.1, 0.04, 'marca_raya')
        una('marca_raya', raya, 32.0 * DP, 32.0 * DP, PX_U, centro=(0.0, 0.03))

# ------------------------------------------------------------------ el reloj tapado
PRES_ALTO_ = 0.92

def render_reloj():
    if SOLO not in ('todo', 'reloj'): return
    def base(r):
        _R['r'] = r
        capa(K(circ_dp(0.0, 0.0, 29.5, 96)), None, 0.06, 0.03, 'reloj_borde', material=tinta())
        capa(K(circ_dp(0.0, 0.0, 26.5, 96)), '#2B3170', 0.1, 0.04, 'reloj_fondo', material=mat_oscuro('m_j_cor_relojfondo', '#2B3170'))
    una('reloj_base', base, 64.0 * DP, 64.0 * DP, PX_U, centro=(0.0, 0.04))
    def tapa(r, presionada=False):
        _R['r'] = r
        col = oscurece('#B79BFF', PRES_OSCURA) if presionada else '#B79BFF'
        cont = K(circ_dp(0.0, 0.0, 23.5, 120))
        if presionada:
            aplasta(r, 1.04)
            pieza_huella(cont, 0.14, 0.07, col, 'reloj_tapa_p', padre=r, huella=(-9.0 * DP, 7.0 * DP, 5.0 * DP), anillos=56, n=180, fino=0.93, domo=0.05, redondea=True)
        else:
            pieza_huella(cont, 0.14, 0.07, col, 'reloj_tapa', padre=r, huella=None, anillos=56, n=180, fino=0.93, domo=0.05, redondea=True)
    una('reloj_tapa', lambda r: tapa(r, False), 50.0 * DP, 50.0 * DP, PX_U, centro=(0.0, 0.1 * 0.5))
    una('reloj_tapa_p', lambda r: tapa(r, True), 50.0 * DP, 50.0 * DP, PX_U, centro=(0.0, 0.1 * 0.5 * PRES_ALTO_))

preparar()
if SOLO in ('todo', 'cartas'): render_cartas()
elif SOLO.startswith('carta_'): render_cartas(SOLO)
if SOLO in ('todo', 'planetas'): render_planetas()
render_piezas(); render_reloj()
termina()
