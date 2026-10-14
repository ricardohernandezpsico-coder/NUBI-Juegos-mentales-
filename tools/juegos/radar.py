# -*- coding: utf-8 -*-
"""Rescate relámpago en plasticina (Tarea 94, el piloto): las seis cápsulas y sus íconos, la roca, el radar (aro y consola), la nave de rescate y su asiento.

Uso: blender -b -P tools/juegos/radar.py -- [--solo capsulas|iconos|roca|radar|nave] [--samples N] [--out DIR]
Salida (en --out, por defecto tools/juegos/out/radar): PNG con fondo transparente, que `empaqueta.py` normaliza y lleva a Unity.
Las piezas comunes (losas «noche» de las casillas, marcas ✓/✗, el anillo de la cuenta regresiva) salen de `comun.py`.

Cada imagen se dibuja en el MISMO lienzo y a la MISMA escala que el sprite procedural al que reemplaza (`RadarSprites`): así el código de Unity de Rescate
no cambia sus medidas (`CapsuleSideInSizes`, `RockSideInRadii`, `ScopeSideInBezelRadii`…). Las formas salen de las mismas SDF (los mismos polígonos y radios).
"""
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from plasticina import *

SOLO = arg('--solo', 'todo')

# ------------------------------------------------------------------ colores (los de RadarSprites.Colors: FIJOS, no cambian con el nivel)
TIPOS = ('hexagono', 'gota', 'circulo', 'cuadrado', 'triangulo', 'rombo')            # el orden de CapsuleType
COLORES = ('#A6E36B', '#7FD8FF', '#FF8A6B', '#FFC94A', '#B79BFF', '#FFB3D1')
BLANCO_VENTANA = '#F4F2FF'; VISOR = '#284A8E'

# ------------------------------------------------------------------ las siluetas (las SDF de RadarSprites.ShapeSdf, en unidades de diseño: y arriba)
def hexagono(): return [(0.9 * math.cos(math.pi / 6 + i * math.pi / 3), 0.9 * math.sin(math.pi / 6 + i * math.pi / 3)) for i in range(6)]
def triangulo(): return list(reversed([(0.0, 0.98), (0.98, -0.74), (-0.98, -0.74)]))
def rombo(): return list(reversed([(0.0, 1.0), (0.84, 0.0), (0.0, -1.0), (-0.84, 0.0)]))

EXTRA = 0.05           # la plasticina no lleva borde tinta (el de ClayRaster engordaba la silueta 0,075): se engorda un poco la forma para que la huella sea la misma

def contorno(tipo):
    """El contorno (antihorario) de cada cápsula, engordado EXTRA."""
    if tipo == 'hexagono': return redondeado(hexagono(), 0.05 + EXTRA)
    if tipo == 'gota': return casco_convexo([(0.0, -0.34, 0.66 + EXTRA), (0.0, 0.96, 0.07 + EXTRA)])
    if tipo == 'circulo': return circulo(0.92 + EXTRA, 120)
    if tipo == 'cuadrado': return redondeado([(0.52, 0.52), (-0.52, 0.52), (-0.52, -0.52), (0.52, -0.52)], 0.22 + EXTRA)      # caja de 0,74 con esquinas de 0,22
    if tipo == 'triangulo': return redondeado(triangulo(), 0.08 + EXTRA)
    return redondeado(rombo(), 0.06 + EXTRA)

def ventana_y(tipo): return -0.34 if tipo == 'gota' else (-0.28 if tipo == 'triangulo' else 0.0)

# ------------------------------------------------------------------ la cápsula
CAP_H = 0.30           # alto de la plasticina
CAP_LIENZO = 2.36      # el lienzo de CapsuleSprite: ±1,18 unidades de diseño

def capsula(tipo, color, con_ventana=True):
    def construir(r):
        pieza(contorno(tipo), CAP_H, 0.15, color, 'cap_' + tipo, padre=r, seg=8, panza=0.012)
        if not con_ventana: return
        wy = ventana_y(tipo); z = CAP_H
        aro_plano(0.335, 0.034, material=tinta(), loc=(0, wy, z + 0.012), nombre='cap_aro', padre=r)           # el aro de churro tinta de la ventanilla
        disco(0.30, 0.05, 0.02, BLANCO_VENTANA, loc=(0, wy, z - 0.005), nombre='cap_cristal', padre=r)
        esfera(0.20, VISOR, loc=(0, wy - 0.02, z + 0.05), escala=(1.1, 0.9, 0.34), nombre='cap_visor', padre=r)
        esfera(0.04, '#FFFFFF', loc=(-0.09, wy + 0.06, z + 0.085), escala=(1.3, 0.8, 0.6), nombre='cap_brillo', padre=r)
    return construir

def render_capsulas(prefijo, px, con_ventana=True):
    for tipo, color in zip(TIPOS, COLORES):
        una(prefijo + tipo, capsula(tipo, color, con_ventana), CAP_LIENZO, CAP_LIENZO, px / CAP_LIENZO, centro=(0.0, sube(CAP_H) / 2))

# ------------------------------------------------------------------ la roca (RadarSprites.Rock: 8 radios irregulares, polígono de radio 0,98 engordado 0,04)
ROCA_K = (1.0, 0.8, 0.95, 0.76, 1.0, 0.84, 0.93, 0.79)
ROCA_H = 0.34
ROCA_LIENZO = 2.36                                                    # ±1,18 como la cápsula

def roca_contorno(n=120):
    """El contorno suave que pasa por los ocho radios de la roca (interpolación periódica de Catmull-Rom sobre el radio en función del ángulo)."""
    ctl = sorted([((-(i * math.pi / 4 + 0.2)) % (2 * math.pi), 0.98 * k + 0.04) for i, k in enumerate(ROCA_K)])
    m = len(ctl); out = []
    for j in range(n):
        a = 2 * math.pi * j / n
        i = max([k for k in range(m) if ctl[k][0] <= a] or [m - 1])
        a0, r0 = ctl[i]; a1, r1 = ctl[(i + 1) % m]
        if a1 <= a0: a1 += 2 * math.pi
        aa = a if a >= a0 else a + 2 * math.pi
        t = (aa - a0) / (a1 - a0)
        rm, rp = ctl[(i - 1) % m][1], ctl[(i + 2) % m][1]
        r = 0.5 * ((2 * r0) + (-rm + r1) * t + (2 * rm - 5 * r0 + 4 * r1 - rp) * t * t + (-rm + 3 * r0 - 3 * r1 + rp) * t ** 3)
        out.append((r * math.cos(a), r * math.sin(a)))
    return out

def roca(r):
    pieza(roca_contorno(), ROCA_H, 0.16, '#76718F', 'roca', padre=r, panza=0.02, bultos=0.012)
    for (x, y, rad) in ((-0.30, 0.22, 0.22), (0.35, -0.26, 0.16), (0.17, 0.43, 0.11)):                # las manchas más oscuras de RockSpot, como huellas bajas
        disco(rad * 0.92, 0.03, 0.012, '#5C5875', loc=(x, y, ROCA_H - 0.012), nombre='roca_mancha', padre=r, verts=40)

# ------------------------------------------------------------------ el radar: el aro (la pantalla de vidrio va aparte, procedural: sigue oscura)
ARO_BEZEL = 0.95; ARO_GLASS = 0.95 * 128.0 / 141.0               # los radios del dibujo (RadarSprites.BezelR y GlassR)
ARO_LIENZO = 2.12                                                  # ±1,06 (ScopeZoom)
ARO_H = 0.11

def anillo(r_int, r_ext, h, rho, color, nombre='anillo', n=180, padre=None, material=None, seg=8):
    """Un anillo de plasticina de sección de rectángulo redondeado (esquinas de radio [rho]), por revolución."""
    perfil = []
    for (cx, cz, a0) in ((r_int + rho, rho, 180.0), (r_ext - rho, rho, 270.0), (r_ext - rho, h - rho, 0.0), (r_int + rho, h - rho, 90.0)):
        for k in range(seg + 1):
            a = math.radians(a0 + 90.0 * k / seg); perfil.append((cx + rho * math.cos(a), cz + rho * math.sin(a)))
    me = bpy.data.meshes.new(nombre); bm = bmesh.new(); m = len(perfil); filas = []
    for j in range(n):
        th = 2 * math.pi * j / n; c, s_ = math.cos(th), math.sin(th)
        filas.append([bm.verts.new((r_ * c, r_ * s_, z)) for (r_, z) in perfil])
    for j in range(n):
        for i in range(m): bm.faces.new((filas[j][i], filas[(j + 1) % n][i], filas[(j + 1) % n][(i + 1) % m], filas[j][(i + 1) % m]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces); bm.to_mesh(me); bm.free(); me.update()
    o = bpy.data.objects.new(nombre, me); scene.collection.objects.link(o); link(o, 'iconos'); smooth(o); organic(o, 0.002, 1.4, 1)
    o.data.materials.append(material if material is not None else mat_kit('m_j_' + nombre + '_' + str(color).strip('#'), color))
    if padre is not None: o.parent = padre
    return o

def ang_pos(grados, rad, z=0.0):
    a = math.radians(grados); return (rad * math.cos(a), rad * math.sin(a), z)

def aro_radar(r):
    anillo(ARO_GLASS - 0.012, ARO_BEZEL, ARO_H, 0.04, '#B8A4FF', 'aro', padre=r)                  # el aro de lila (el color «lila» del kit); entra 0,012 sobre el vidrio para taparle el borde
    rm = (ARO_GLASS + ARO_BEZEL) / 2
    for k in range(12):                                                                              # 12 tachuelas de crema (antes eran 48 marcas)
        g = 90 + k * 30
        if g % 360 in (120, 240, 300): continue                                                      # ahí van la antena y los dos botones
        x, y, _ = ang_pos(g, rm); esfera(0.017, KIT_CREMA, loc=(x, y, ARO_H - 0.004), escala=(1, 1, 0.7), nombre='aro_tachuela', padre=r, seg=24)
    for g, col in ((240, KIT_SOL), (300, KIT_CORAL)):                                                # la «consola»: dos botones
        x, y, _ = ang_pos(g, rm); disco(0.031, 0.04, 0.016, col, loc=(x, y, ARO_H - 0.01), nombre='aro_boton', padre=r, verts=40)
    x0, y0, _ = ang_pos(120, ARO_BEZEL - 0.02); x1, y1, _ = ang_pos(120, ARO_BEZEL + 0.062)           # la antena: un palito de crema que sale del aro con una bolita de sol
    pieza(casco_convexo([(x0, y0, 0.014), (x1, y1, 0.014)], 24), 0.05, 0.018, KIT_CREMA, 'aro_antena', padre=r, z0=ARO_H - 0.012, bultos=0)
    esfera(0.03, KIT_SOL, loc=(x1, y1, ARO_H + 0.03), nombre='aro_antena_bola', padre=r, seg=32)

# ------------------------------------------------------------------ la nave de rescate (RadarSprites.RenderShip, 1 unidad = 100 dp)
NAVE_LIENZO = (2.4, 1.0)            # 240 × 100 dp
NAVE_PX = 340.0                     # píxeles por unidad = 3,4 px por dp
NAVE_H = 0.12                       # alto del casco
ASIENTO_X0, ASIENTO_PASO, ASIENTO_Y, ASIENTO_R = -0.81, 0.18, 0.02, 0.072

def ccw(pts):
    a = sum(pts[i][0] * pts[(i + 1) % len(pts)][1] - pts[(i + 1) % len(pts)][0] * pts[i][1] for i in range(len(pts)))
    return pts if a > 0 else list(reversed(pts))

def nave(r, con_asientos=True):
    for sx in (1, -1):
        aleta = [(0.80, 0.04), (1.12, -0.14), (1.04, -0.24), (0.74, -0.17)]
        pieza(redondeado(ccw([(sx * x, y) for (x, y) in aleta]), 0.02), 0.05, 0.022, '#B79BFF', 'nave_aleta', padre=r)
        cx = sx * 0.86
        pieza(redondeado(ccw([(cx + 0.05, -0.08), (cx - 0.05, -0.08), (cx - 0.05, -0.12), (cx + 0.05, -0.12)]), 0.08), 0.07, 0.03, '#FF8A6B', 'nave_propulsor', padre=r)
    cab = [(0.2 * math.cos(a), 0.17 + 0.2 * math.sin(a)) for a in [math.pi * k / 24 for k in range(25)]]
    pieza(redondeado(ccw(cab), 0.02, arco=6), 0.14, 0.05, '#7FD8FF', 'nave_cabina', padre=r)
    pieza(casco_convexo([(-0.81, 0.0, 0.19 + 0.02), (0.81, 0.0, 0.19 + 0.02)]), NAVE_H, 0.06, '#E9E6FF', 'nave_casco', padre=r, panza=0.004)
    pieza(redondeado(ccw([(0.9, -0.103), (-0.9, -0.103), (-0.9, -0.127), (0.9, -0.127)]), 0.012), 0.022, 0.009, '#7FD8FF', 'nave_franja', padre=r, z0=NAVE_H - 0.006, bultos=0)
    esfera(0.085, '#F4F2FF', loc=(0, 0.25, 0.14 + 0.03), nombre='nave_casco_piloto', padre=r)
    esfera(0.056, VISOR, loc=(0, 0.245, 0.14 + 0.082), escala=(1, 0.75, 0.5), nombre='nave_visor', padre=r)
    if con_asientos:
        for i in range(10):
            x = ASIENTO_X0 + i * ASIENTO_PASO
            aro_plano(ASIENTO_R, 0.016, material=mat_kit('m_j_nave_aro', '#CFCBF0'), loc=(x, ASIENTO_Y, NAVE_H + 0.008), nombre='nave_asiento_aro', padre=r)
            disco(ASIENTO_R - 0.004, 0.018, 0.008, '#8F8AC4', loc=(x, ASIENTO_Y, NAVE_H - 0.01), nombre='nave_asiento', padre=r, verts=40)

def asiento_lleno(r):
    disco(0.056, 0.04, 0.02, '#FFFFFF', loc=(0, 0, 0), nombre='asiento_lleno', padre=r, verts=48)

# ------------------------------------------------------------------ main
def escribe_medidas():
    """Lo que `empaqueta.py` pone en el manifiesto y Unity lee (en dp): dónde cae el centro de cada asiento de la nave (los asientos están pintados en el casco) y el tamaño del lienzo de la nave."""
    import json
    t = math.radians(TILT)
    med = {'asiento_dy_dp': ((ASIENTO_Y + (NAVE_H + 0.008 + 0.02) * math.sin(t)) - sube(NAVE_H) / 2) * 100.0,      # cuánto sube el centro del asiento (y de la bolita que lo llena) sobre el centro de la nave
           'asiento_x0_dp': ASIENTO_X0 * 100.0, 'asiento_paso_dp': ASIENTO_PASO * 100.0,
           'nave_ancho_dp': NAVE_LIENZO[0] * 100.0, 'nave_alto_dp': NAVE_LIENZO[1] * 100.0, 'asiento_lleno_dp': 20.0, 'inclinacion_grados': TILT}
    json.dump(med, open(os.path.join(OUT, 'medidas.json'), 'w', encoding='utf-8'), indent=1)

preparar()
escribe_medidas()
if SOLO in ('todo', 'capsulas'): render_capsulas('capsula_', 256)
if SOLO in ('todo', 'iconos'): render_capsulas('icono_', 128)
if SOLO in ('todo', 'roca'): una('roca', roca, ROCA_LIENZO, ROCA_LIENZO, 192 / ROCA_LIENZO, centro=(0.0, sube(ROCA_H) / 2))
if SOLO in ('todo', 'radar'): una('radar_aro', aro_radar, ARO_LIENZO, ARO_LIENZO, 1024 / ARO_LIENZO, centro=(0.0, sube(ARO_H) / 2))
if SOLO in ('todo', 'nave'):
    una('nave', nave, NAVE_LIENZO[0], NAVE_LIENZO[1], NAVE_PX, centro=(0.0, sube(NAVE_H) / 2))
    una('asiento_lleno', asiento_lleno, 0.2, 0.2, NAVE_PX, centro=(0.0, sube(0.04) / 2))
termina()
