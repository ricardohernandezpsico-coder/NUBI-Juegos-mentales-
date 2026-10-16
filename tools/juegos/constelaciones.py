# -*- coding: utf-8 -*-
"""Constelaciones en plasticina (Tarea 106): los 12 objetos del espacio y los 4 gemelos, la luz dormida (esfera de arcilla oscura con un núcleo), su estado PRESIONADO (regla del 14-oct) y la luz abierta (un disco claro).
Los aros de las parejas hechas, las líneas, los puntos de «De memoria» y las chispas se quedan procedurales (se tiñen, giran o cambian de largo).

Uso: blender -b -P tools/juegos/constelaciones.py -- [--solo obj|luces|obj_K_V|dormante|dormante_p|abierta] [--samples N] [--out DIR]
Salida (en --out, por defecto tools/juegos/out): `obj_K_V.png` (K = 0..11, V = 0, y 1 solo en los cuatro que tienen gemelo), `dormante.png`, `dormante_p.png` y `abierta.png`, las «maestras» con fondo transparente;
`empaqueta.py parejas` les pone el contorno de tinta a los objetos, el brillo del núcleo a las luces dormidas, y las reduce.

LOS OBJETOS son el dibujo del boceto aprobado (`ConstelacionSprites.PaintObject`, x a la derecha, y HACIA ABAJO, ±22 unidades en una caja de 56) hecho de plasticina: piezas planas apiladas con el canto blando (como el cohete de Piloto), y
una esfera de verdad con su anillo inclinado (el planeta, que flota: sin sombra de contacto). La silueta, el tamaño y el detalle que distingue a cada gemelo (anillo / llama grande / patas / antena) son los de siempre.
1 unidad del boceto = 1/20 de unidad de Blender. El contorno de tinta de los objetos (el «borde de tinta» del boceto, que los separa del disco claro de la luz abierta) lo agrega `empaqueta.py` a partir de la silueta.
LA LUZ: la dormida es una esfera de arcilla azul noche (radio 38 u) con un grano claro al centro (núcleo); la presionada es la misma aplastada (~8 % más baja y ~4 % más ancha), un punto más oscura y con la huella de un dedo
HUNDIDA lejos del núcleo (la huella no pisa el estímulo). La abierta es un disco claro de arcilla.
"""
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from plasticina import *
from formas import engorda
from mathutils import Matrix

SOLO = arg('--solo', 'todo')
S = 1.0 / 20.0                                   # 1 unidad del boceto en unidades de Blender
CAJA = 2.8                                       # el lienzo de un objeto: 56 unidades del boceto
OBJ_PX_U = 160                                   # la maestra: 448 px (se reduce a 224)
TINTA = '#1A1240'
H0, DH = 0.05, 0.035                             # alto de la primera capa y lo que sube cada capa siguiente
T = math.radians(TILT)

# ------------------------------------------------------------------ geometría en las coordenadas del boceto (y hacia abajo)
def K(p): return (p[0] * S, -p[1] * S)

def ccw(pts):
    a = sum(pts[i][0] * pts[(i + 1) % len(pts)][1] - pts[(i + 1) % len(pts)][0] * pts[i][1] for i in range(len(pts)))
    return list(pts) if a > 0 else list(reversed(pts))

def KP(pts): return ccw([K(p) for p in pts])

class Ruta:
    """El camino del boceto: rectas y curvas de segundo grado (aplanadas), como `Path` de ConstelacionSprites."""
    def __init__(s): s.p = []; s.cx = s.cy = 0.0
    def mover(s, x, y): s.p.append((x, y)); s.cx, s.cy = x, y; return s
    def linea(s, x, y): s.p.append((x, y)); s.cx, s.cy = x, y; return s
    def quad(s, qx, qy, x, y):
        sx, sy = s.cx, s.cy
        for i in range(1, 13):
            t = i / 12.0; u = 1 - t; s.p.append((u * u * sx + 2 * u * t * qx + t * t * x, u * u * sy + 2 * u * t * qy + t * t * y))
        s.cx, s.cy = x, y; return s
    def arco(s, cx, cy, r, a0, a1, pasos=16):
        for i in range(pasos + 1):
            a = a0 + (a1 - a0) * i / pasos; s.p.append((cx + r * math.cos(a), cy + r * math.sin(a)))
        s.cx, s.cy = s.p[-1]; return s

def girar(pts, ang, tx=0.0, ty=0.0):
    """Del marco local de una pieza girada (rx = x·c − y·s, ry = x·s + y·c en el boceto) al dibujo."""
    c, s = math.cos(ang), math.sin(ang)
    return [(x * c + y * s + tx, -x * s + y * c + ty) for (x, y) in pts]

def mueve(pts, dx, dy): return [(x + dx, y + dy) for (x, y) in pts]

def circ_sk(cx, cy, r, n=72): return [(cx + r * math.cos(2 * math.pi * i / n), cy + r * math.sin(2 * math.pi * i / n)) for i in range(n)]
def el_sk(cx, cy, rx, ry, n=72): return [(cx + rx * math.cos(2 * math.pi * i / n), cy + ry * math.sin(2 * math.pi * i / n)) for i in range(n)]

def caja_sk(cx, cy, hx, hy, r=0.0, n=7):
    """Un rectángulo de medios hx × hy con las esquinas redondeadas con radio r (el `RoundBox` del boceto; con r = 0, un redondeo mínimo: la plasticina nunca tiene la esquina viva)."""
    r = max(0.6, min(r, hx, hy)); out = []
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

def segmento(p0, p1, medio_ancho): return trazo([K(p0), K(p1)], medio_ancho * 2 * S)

# ------------------------------------------------------------------ las capas de plasticina
_R = {}
def capa(cont, color, nivel, nombre, bis=None, panza=0.0, material=None, z0=0.0):
    h = H0 + DH * nivel
    bis = min(0.03, h * 0.5) if bis is None else bis
    return pieza(cont, h, bis, color, nombre, z0=z0, panza=panza, material=material, padre=_R['raiz'])

def tinta_capa(cont, nivel, nombre): return capa(cont, None, nivel, nombre, bis=0.02, material=tinta())

def clava(r_, nombre, **kw):
    _R['raiz'] = r_

def mat_(nombre, color): return mat_kit('m_j_' + nombre + '_' + color.strip('#'), color)

def bola(cx, cy, R_, color=None, material=None, nombre='bola', seg=64):
    """Una esfera de plasticina apoyada en el suelo cuyo contorno en la imagen tiene el centro en (cx, cy) de diseño (en unidades de Blender)."""
    return esfera(R_, color, loc=(cx, (cy - R_ * math.sin(T)) / math.cos(T), R_), nombre=nombre, material=material, seg=seg)

VISTA = Vector((0.0, -math.sin(T), math.cos(T)))              # hacia la cámara
ARRIBA = Vector((0.0, math.cos(T), math.sin(T)))               # arriba en la imagen
DERECHA = Vector((1.0, 0.0, 0.0))

def punto_esfera(cx, cy, R_, sx, sy, dentro=0.0):
    """El punto de la superficie de la bola (ver `bola`) que se ve a (sx, sy) del centro de su contorno, en unidades de Blender."""
    q = max(0.0, 1.0 - (sx * sx + sy * sy) / (R_ * R_)); n = DERECHA * (sx / R_) + ARRIBA * (sy / R_) + VISTA * math.sqrt(q)
    c = Vector((cx, (cy - R_ * math.sin(T)) / math.cos(T), R_)); return c + n * (R_ - dentro)

# ------------------------------------------------------------------ los objetos
def planeta(v):
    def construir(r):
        bpy.data.objects['juego_suelo'].hide_render = True        # flota (la sombra de una esfera tan alta se saldría del lienzo)
        R_ = 0.65; psi = 0.35; phi = math.asin(6.5 / 21.0)
        L = DERECHA * math.cos(psi) + ARRIBA * math.sin(psi)                                    # el eje largo del anillo, inclinado 0,35 rad como el del boceto
        M = ARRIBA * math.cos(psi) - DERECHA * math.sin(psi)
        a = M * math.cos(phi) + VISTA * math.sin(phi)                                           # el eje del planeta: el anillo (su plano) se ve con la razón 6,5 / 21 del boceto
        ey = a.cross(L)
        m = Matrix((L, ey, a)).transposed()
        e = bpy.data.objects.new('planeta', None); scene.collection.objects.link(e); e.location = (0.0, -R_ * math.sin(T) / math.cos(T), R_); e.rotation_euler = m.to_euler()
        s = esfera(R_, '#FF9F6B', nombre='planeta_esfera', seg=96); s.parent = e
        # las dos franjas: cintas esféricas (más naranja) en las latitudes donde el boceto las pinta (y −4…0 y 5…7,5)
        def lat(sy):
            th = math.asin(max(-1.0, min(1.0, sy / R_))); n = VISTA * math.cos(th) + ARRIBA * math.sin(th); return n.dot(a)
        for (y_lo, y_hi, nombre) in ((0.0, 0.2, 'franja_a'), (-0.375, -0.25, 'franja_b')):
            z1, z2 = lat(y_lo) * R_, lat(y_hi) * R_
            if z1 > z2: z1, z2 = z2, z1
            cinta_esfera(e, R_, z1, z2, '#E9764A', nombre)
        if v == 0:
            a_ = aro_plano(1.05, 0.15, '#FFD46B', nombre='planeta_anillo'); a_.parent = e
    return construir

def cinta_esfera(padre, R_, z1, z2, color, nombre, N=10, seg=128, grosor=0.022):
    me = bpy.data.meshes.new(nombre); bm = bmesh.new(); Rb = R_ + grosor
    zs = [z1 + (z2 - z1) * k / N for k in range(N + 1)]
    ext = [[bm.verts.new((math.sqrt(max(Rb * Rb - z * z, 1e-6)) * math.cos(2 * math.pi * j / seg), math.sqrt(max(Rb * Rb - z * z, 1e-6)) * math.sin(2 * math.pi * j / seg), z)) for j in range(seg)] for z in zs]
    for k in (0, N):
        z = zs[k]; ints = [bm.verts.new((math.sqrt(max(R_ * R_ - z * z, 1e-6)) * math.cos(2 * math.pi * j / seg), math.sqrt(max(R_ * R_ - z * z, 1e-6)) * math.sin(2 * math.pi * j / seg), z * 0.98)) for j in range(seg)]
        for j in range(seg):
            f = (ext[k][j], ext[k][(j + 1) % seg], ints[(j + 1) % seg], ints[j]); bm.faces.new(f if k == N else tuple(reversed(f)))
    for k in range(N):
        for j in range(seg): bm.faces.new((ext[k][j], ext[k][(j + 1) % seg], ext[k + 1][(j + 1) % seg], ext[k + 1][j]))
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces)); bm.normal_update(); bm.to_mesh(me); bm.free(); me.update()
    o = bpy.data.objects.new(nombre, me); scene.collection.objects.link(o); link(o, 'iconos'); smooth(o)
    o.data.materials.append(mat_kit('m_j_' + nombre + '_' + color.strip('#'), color)); o.parent = padre; return o

def cohete(v):
    def construir(r):
        clava(r, 'cohete'); dy = -3.0 if v == 1 else 0.0
        def P(pts): return KP(mueve(pts, 0.0, dy))
        if v == 1:
            capa(P(Ruta().mover(-8, 10).quad(-6, 22, 0, 30).quad(6, 22, 8, 10).p), '#FF8A3D', 0, 'llama_grande')
            capa(P(Ruta().mover(-4, 10).quad(-3, 18, 0, 23).quad(3, 18, 4, 10).p), '#FFE27A', 1, 'llama_centro')
        capa(P([(-8, 3), (-15, 13), (-8, 11)]), '#E2524A', 1, 'aleta_i')
        capa(P([(8, 3), (15, 13), (8, 11)]), '#E2524A', 1, 'aleta_d')
        capa(P(Ruta().mover(0, -19).quad(9, -10, 8, -2).linea(8, 11).linea(-8, 11).linea(-8, -2).quad(-9, -10, 0, -19).p), '#EDEAFB', 2, 'casco', panza=0.004)
        capa(P(Ruta().mover(0, -19).quad(6.5, -13, 7, -9).linea(-7, -9).quad(-6.5, -13, 0, -19).p), '#E2524A', 3, 'punta')
        cx, cy = K((0, dy)); h = H0 + DH * 4
        capa(ccw([(cx + 0.21 * math.cos(2 * math.pi * i / 48), cy + 0.21 * math.sin(2 * math.pi * i / 48)) for i in range(48)]), '#7FD8FF', 4, 'ventana')
        aro_plano(0.225, 0.028, material=tinta(), loc=(cx, cy, h - 0.005), nombre='ventana_aro', padre=r)
    return construir

def cometa(v):
    def construir(r):
        clava(r, 'cometa')
        for dy, w, col, niv in ((-5.0, 5.0, '#82D7FF', 0), (5.0, 5.0, '#82D7FF', 0), (0.0, 6.0, '#C4EBFF', 1)):
            ruta = Ruta().mover(4, -4 + dy * 0.2).quad(-6, 4 + dy, -18, 14 + dy * 0.6).p
            capa(trazo([K(p) for p in ruta], w * S, w * S * 0.45), col, niv, 'cola', bis=0.02)
        capa(KP(circ_sk(7, -7, 8, 72)), '#BFE9FF', 3, 'cabeza', panza=0.03, bis=0.075)
        capa(KP(circ_sk(5, -9, 2.4, 32)), '#FFFFFF', 4, 'brillo', bis=0.03)
    return construir

def ovni(v):
    def construir(r):
        clava(r, 'ovni')
        if v == 1:
            for (a, b) in (((-9, 4), (-13, 15)), ((-17, 15), (-9, 15)), ((9, 4), (13, 15)), ((9, 15), (17, 15))):
                tinta_capa(segmento(a, b, 1.6), 0, 'pata')
        capa(KP(Ruta().arco(0, -3, 9, math.pi, 2 * math.pi, 24).p), '#9FF5D6', 1, 'cupula', panza=0.02, bis=0.06)
        capa(KP(el_sk(0, 1, 19, 6.5)), '#A99FEA', 2, 'plato', panza=0.01, bis=0.05)
        for dx in (-10, 0, 10): capa(KP(circ_sk(dx, 2, 2.4, 32)), '#FFC94A', 3, 'luz', bis=0.025)
    return construir

def cristal(v):
    def construir(r):
        clava(r, 'cristal')
        base = KP([(0, -18), (10, -8), (10, 8), (0, 18), (-10, 8), (-10, -8)]); n = len(base)
        me = bpy.data.meshes.new('cristal'); bm = bmesh.new()
        ap = bm.verts.new((0.0, 0.1, 0.30))                                                        # la punta del cristal: donde se cruzan las aristas del boceto (0, −2)
        b0 = [bm.verts.new((x, y, 0.0)) for (x, y) in base]; b1 = [bm.verts.new((x, y, 0.05)) for (x, y) in base]
        bm.faces.new(list(reversed(b0)))
        for j in range(n): bm.faces.new((b0[j], b0[(j + 1) % n], b1[(j + 1) % n], b1[j]))
        for j in range(n): bm.faces.new((b1[j], b1[(j + 1) % n], ap))
        bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces)); bm.normal_update(); bm.to_mesh(me); bm.free(); me.update()
        o = bpy.data.objects.new('cristal', me); scene.collection.objects.link(o); link(o, 'iconos')
        md = bevel(o, 0.022, 3, ang=25); md.limit_method = 'ANGLE'
        wn = o.modifiers.new('Normales', 'WEIGHTED_NORMAL'); wn.keep_sharp = True; wn.weight = 60
        o.data.materials.append(mat_kit('m_j_cristal_C084FC', '#C084FC')); o.parent = r
    return construir

def casco(v):
    def construir(r):
        clava(r, 'casco')
        if v == 1:
            tinta_capa(segmento((6, -12), (10, -21), 1.5), 0, 'antena')
            capa(KP(circ_sk(10, -22, 3.9, 40)), '#FFC94A', 1, 'bolita', bis=0.04)
        capa(KP(caja_sk(0, 14, 11, 3, 0)), '#A99FEA', 1, 'collar', bis=0.025)
        capa(KP(circ_sk(0, 0, 14.5, 96)), '#F4F1FF', 2, 'casco', panza=0.02, bis=0.09)
        capa(KP(caja_sk(0, -0.5, 10, 6.5, 6)), '#163A5C', 3, 'visera', bis=0.035, material=mat_oscuro('m_j_visera', '#163A5C'))
        capa(trazo([K(p) for p in Ruta().arco(-3, -1, 5, math.pi * 1.1, math.pi * 1.5, 10).p], 0.09, 0.09), '#7FD8FF', 4, 'brillo', bis=0.015)
    return construir

def luna(v):
    def construir(r):
        clava(r, 'luna')
        cr = [(-5, -5, 4.0), (6, 3, 3.2), (-2, 8, 2.4), (7, -8, 2.0)]
        huellas = [(x * S, -y * S, rd * S, 0.55 * rd * S, 0.0, 0.1, 0.0, 1.0) for (x, y, rd) in cr]
        pieza_huella(circulo(15 * S, 160), 0.13, 0.07, '#E6DFB8', 'luna', padre=r, huella=huellas, anillos=48, n=160, domo=0.07, redondea=True, fino=0.93)
    return construir

def satelite(v):
    def construir(r):
        clava(r, 'satelite')
        tinta_capa(segmento((-8, 0), (8, 0), 1.4), 0, 'brazo')
        for sx in (-1, 1):
            cx = 14.5 * sx
            capa(KP(caja_sk(cx, 0, 6.5, 6, 0)), '#5B8DEF', 1, 'panel', bis=0.025)
            capa(KP(caja_sk(cx, 0, 0.7, 5.2, 0)), '#B7CDFF', 2, 'rejilla_v', bis=0.012)
            capa(KP(caja_sk(cx, 0, 5.6, 0.7, 0)), '#B7CDFF', 2, 'rejilla_h', bis=0.012)
        capa(KP(caja_sk(0, 0, 6, 8, 3)), '#FFC94A', 3, 'cuerpo', bis=0.04)
        tinta_capa(segmento((0, -8), (0, -14), 1.2), 1, 'mastil')
        capa(KP(circ_sk(0, -15, 2.7, 32)), '#E2524A', 3, 'bolita', bis=0.04)
    return construir

def telescopio(v):
    def construir(r):
        clava(r, 'telescopio'); a = 0.55
        for (a_, b_) in (((0, 3), (-9, 18)), ((0, 3), (0, 18)), ((0, 3), (9, 18))): tinta_capa(segmento(a_, b_, 1.5), 0, 'pata')
        capa(KP(girar(caja_sk(-2.5, 0, 13.5, 5.5, 3), a)), '#7FB2FF', 1, 'tubo', bis=0.04, panza=0.01)
        capa(KP(girar(caja_sk(12.5, 0, 2.5, 7, 0), a)), '#3E5BA9', 2, 'lente', bis=0.03, material=mat_oscuro('m_j_telesc_azul', '#3E5BA9'))
        capa(KP(girar(caja_sk(-17, 0, 2, 3.5, 0), a)), '#3E5BA9', 2, 'ocular', bis=0.025, material=mat_oscuro('m_j_telesc_azul', '#3E5BA9'))
    return construir

def asteroide(v):
    def construir(r):
        clava(r, 'asteroide')
        pts = [(0, -15), (9, -11), (15, -2), (12, 9), (3, 15), (-8, 13), (-15, 4), (-13, -8)]
        cont = redondeado(KP(pts), 0.05)
        cr = [(-4, -4, 3.4), (6, 4, 2.6), (-5, 7, 2.0)]
        huellas = [(x * S, -y * S, rd * S, 0.55 * rd * S, 0.0, 0.1, 0.0, 1.0) for (x, y, rd) in cr]
        pieza_huella(cont, 0.14, 0.07, '#A08C7A', 'asteroide', padre=r, huella=huellas, anillos=48, n=160, domo=0.06, redondea=True, fino=0.93)
    return construir

def antena(v):
    def construir(r):
        clava(r, 'antena'); a = 0.55
        capa(KP(caja_sk(0, 16, 9, 2, 0)), '#ABA5D2', 0, 'base', bis=0.025)
        tinta_capa(segmento((0, 14), (0, 4), 1.6), 1, 'mastil')
        plato = [(x, y) for (x, y) in Ruta().arco(0, 0, 1.0, 0.0, math.pi, 40).p]
        plato = [(15 * x, 7.5 * y) for (x, y) in plato]                                        # la media elipse (hacia abajo en el marco local, y positiva), 15 × 7,5
        capa(KP(girar(plato, a)), '#DCD8F0', 2, 'plato', bis=0.045, panza=0.02)
        tinta_capa(segmento(girar([(0, 4)], a)[0], girar([(0, -11)], a)[0], 1.1), 3, 'alimentador')
        px, py = girar([(0, -12)], a)[0]
        capa(KP(circ_sk(px, py, 2.9, 32)), '#E2524A', 4, 'bolita', bis=0.04)
    return construir

def brujula(v):
    def construir(r):
        clava(r, 'brujula')
        capa(KP(circ_sk(0, 0, 15, 96)), '#F6EBD2', 0, 'cara', panza=0.01, bis=0.05)
        aro_plano(11 * S, 0.02, material=mat_('aro_brujula', '#C8B98E'), loc=(0, 0, H0 + 0.01), nombre='brujula_aro', padre=r)
        capa(KP([(0, -11), (4, 0), (-4, 0)]), '#E2524A', 1, 'aguja_n', bis=0.02)
        capa(KP([(0, 11), (4, 0), (-4, 0)]), '#6E6A9A', 1, 'aguja_s', bis=0.02, material=mat_oscuro('m_j_aguja_s', '#6E6A9A'))
        capa(KP(circ_sk(0, 0, 2.4, 32)), TINTA, 3, 'eje', bis=0.03, material=tinta())
    return construir

OBJETOS = (planeta, cohete, cometa, ovni, cristal, casco, luna, satelite, telescopio, asteroide, antena, brujula)
GEMELOS = (0, 1, 3, 5)

def render_objetos(solo=None):
    for k, fn in enumerate(OBJETOS):
        for v in ((0, 1) if k in GEMELOS else (0,)):
            nombre = f'obj_{k}_{v}'
            if solo is not None and nombre != solo: continue
            una(nombre, fn(v), CAJA, CAJA, OBJ_PX_U, centro=(0.0, 0.0))

# ------------------------------------------------------------------ las luces
R_LUZ = 0.94                                       # la esfera ocupa el radio 38 del boceto (la caja de 84 unidades mide 2,0789)
LIENZO_LUZ = 84.0 / 38.0 * R_LUZ
LUZ_PX = 504                                       # la maestra: 504 px (se reduce a 252 = 84 unidades a 3 px por unidad)
AZUL = '#2E3478'
NUCLEO = '#CFD9FF'
RB = 0.115                                         # el radio del grano del núcleo

def luz_dormida(r_unused=None):
    def construir(r):
        bpy.data.objects['juego_suelo'].hide_render = True
        esfera(R_LUZ, material=mat_oscuro('m_j_dormante', AZUL), loc=(0, 0, R_LUZ), nombre='dormante', seg=128)
        p = Vector((0.0, -R_LUZ * math.sin(T), R_LUZ)) + VISTA * (R_LUZ - 0.01) + Vector((0.0, 0.0, 0.0))
        p = Vector((0.0, 0.0, R_LUZ)) + VISTA * (R_LUZ - 0.012)
        b = esfera(RB, NUCLEO, loc=tuple(p), escala=(1.0, 1.0, 0.45), nombre='nucleo'); b.rotation_euler = (T, 0, 0)
    return construir

PRES_ALTO_, PRES_ANCHO_ = 0.92, 1.04

def huella_luz(o, radio, ox=0.40, oy=-0.34, rd_k=0.34, prof_k=0.15):
    """Hunde en la esfera [o] la huella de un dedo, lejos del núcleo: centrada a (ox, oy) radios de la mitad de la cara que mira a la cámara (más cerca del borde de abajo a la derecha)."""
    n = Vector((0.0, -math.sin(T) / PRES_ANCHO_, math.cos(T) / PRES_ALTO_)).normalized()
    e1 = (Vector((1.0, 0.0, 0.0)) - n * n.x).normalized(); e2 = n.cross(e1)
    ang = math.radians(18); ca, sa = math.cos(ang), math.sin(ang)
    rd, prof, cresta, lam = rd_k * radio, prof_k * radio, 0.008 * radio, 0.14 * radio
    for v in o.data.vertices:
        p = Vector(v.co)
        if p.dot(n) < 0.35 * radio: continue
        u, w = p.dot(e1) - ox * radio, p.dot(e2) - oy * radio
        uu, ww = u * ca + w * sa, (-u * sa + w * ca) / 1.3
        s_ = math.hypot(uu, ww)
        if s_ >= rd: continue
        f = (1.0 - (s_ / rd) ** 4) ** 2
        d = prof * f - cresta * f * math.cos(2 * math.pi * s_ / lam)
        v.co = p.normalized() * (p.length - d)
    o.data.update()

def luz_presionada():
    def construir(r):
        bpy.data.objects['juego_suelo'].hide_render = True
        o = esfera(R_LUZ, material=mat_oscuro('m_j_dormante_p', oscurece(AZUL, PRES_OSCURA)), loc=(0, 0, R_LUZ * PRES_ALTO_), escala=(PRES_ANCHO_, PRES_ANCHO_, PRES_ALTO_), nombre='dormante_p', seg=320)
        huella_luz(o, R_LUZ)
        a, b, c = PRES_ANCHO_, PRES_ANCHO_, PRES_ALTO_; vx, vy, vz = VISTA.x, VISTA.y, VISTA.z
        k = math.sqrt(a * a * vx * vx + b * b * vy * vy + c * c * vz * vz)
        sup = Vector((a * a * vx, b * b * vy, c * c * vz)) / k * R_LUZ                       # el punto de la esfera aplastada que mira a la cámara
        p = Vector((0.0, 0.0, R_LUZ * PRES_ALTO_)) + sup * (1.0 - 0.012 / R_LUZ)
        g = esfera(RB, oscurece(NUCLEO, PRES_OSCURA), loc=tuple(p), escala=(PRES_ANCHO_, PRES_ANCHO_, 0.45 * PRES_ALTO_), nombre='nucleo_p'); g.rotation_euler = (T, 0, 0)
    return construir

def luz_abierta():
    def construir(r):
        disco(R_LUZ, 0.07, 0.04, '#CFC6F4', loc=(0, 0, 0), nombre='abierta', padre=r)
    return construir

def render_luces(solo=None):
    if solo in (None, 'dormante'): una('dormante', luz_dormida(), LIENZO_LUZ, LIENZO_LUZ, LUZ_PX / LIENZO_LUZ, centro=(0.0, R_LUZ * math.sin(T)))
    if solo in (None, 'dormante_p'): una('dormante_p', luz_presionada(), LIENZO_LUZ, LIENZO_LUZ, LUZ_PX / LIENZO_LUZ, centro=(0.0, R_LUZ * PRES_ALTO_ * math.sin(T)))
    if solo in (None, 'abierta'): una('abierta', luz_abierta(), LIENZO_LUZ, LIENZO_LUZ, LUZ_PX / LIENZO_LUZ, centro=(0.0, 0.035 * math.sin(T)))

preparar()
if SOLO in ('todo', 'obj'): render_objetos()
elif SOLO.startswith('obj_'): render_objetos(SOLO)
if SOLO in ('todo', 'luces'): render_luces()
elif SOLO in ('dormante', 'dormante_p', 'abierta'): render_luces(SOLO)
termina()
