# -*- coding: utf-8 -*-
"""Los emblemas de plasticina de las cinco divisiones del Contrarreloj (D3; diseño de Fable, 2-nov-2026; Tarea 212: Ricardo eligió la dirección A «Los cinco lugares»; el diseño completo está en `auditoria-fable/emblemas-divisiones.md`).

Dos direcciones, cinco emblemas cada una, con las MISMAS piezas y la misma luz y material que el kit y los juegos (`tools/juegos/plasticina.py`,
que toma `clay()`/`studio()` de `tools/isla/isla.py`; el cohete es `tools/juegos/cohete.py`, el de Freno y Piloto):
  A «Los cinco lugares»: un solo objeto grande por división, el lugar que dice el nombre.
     a1 plataforma (la losa lila con dos torres de Freno) con el cohete parado · a2 el cohete despegando con sus nubes · a3 el planeta con anillo (Constelaciones) ·
     a4 la luna llena con cráteres (una esfera; símbolos neutros: nada de media luna) · a5 tres luceros redondos (Rastro, Meteoros).
  B «El viaje del cohete» (descartada por Ricardo, queda como registro): el cohete en las cinco escenas (b1 = a1, b2 = a2, b3 en órbita del planeta, b4 posado en la luna, b5 volando entre luceros).

Cada emblema sale en un lienzo CUADRADO de 2 unidades de diseño a 220 px por unidad (440 px, la «maestra»); en la app (`ui/components/DivisionEmblem.kt`) el objeto ocupa el 86 % de una ventanilla noche de 52 dp en la cifra y de 28 dp en la escalera.
Uso:  blender -b -P tools/juegos/emblemas_divisiones.py -- [--solo a|b|a1..b5] [--samples N] [--out DIR]       (a 112 muestras, unos 40 s por emblema)
Después:  python -I tools/juegos/emblemas_divisiones_app.py     (reduce las cinco maestras y las deja en app/src/main/res/drawable-nodpi/division_*.webp)
"""
import math, os, sys
AQUI = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, AQUI)                                    # plasticina.py y cohete.py viven al lado
from plasticina import *                                  # bpy, scene, TILT, preparar, una, termina, pieza, esfera, aro_plano, disco, churro_c, tinta, rect, redondeado, sube, mat_kit…
from mathutils import Vector, Matrix
from cohete import cohete

SOLO = arg('--solo', 'todo')
LIENZO = 2.0
PX = 440
T = math.radians(TILT)
VISTA = Vector((0.0, -math.sin(T), math.cos(T)))           # hacia la cámara
ARRIBA = Vector((0.0, math.cos(T), math.sin(T)))            # arriba en la imagen
DERECHA = Vector((1.0, 0.0, 0.0))

# colores del kit y de las piezas que ya existen
CORAL, SOL, LILA, CREMA = '#FF6B4A', '#FFC93C', '#B8A4FF', '#FFF8EC'
LILA_TORRE = '#9A86E6'
PLANETA, ANILLO = '#FF9F6B', '#FFD46B'                      # el planeta con anillo de Constelaciones
LUNA = '#C9CFE6'                                            # gris lavanda: se separa de la losa crema (#FFEBD4) y del planeta
NUBE = '#ECE8F3'

# ------------------------------------------------------------------ ayudas
def sub(r, x=0.0, y=0.0, z=0.0, escala=1.0, giro=0.0, nombre='sub'):
    """Un padre vacío bajo la raíz: lo que cuelga de él se mueve a (x, y) de diseño, se escala y gira en el plano de la imagen (giro en radianes, positivo = antihorario)."""
    e = bpy.data.objects.new(nombre, None); scene.collection.objects.link(e)
    e.location = (x, y - z * math.sin(T), z); e.scale = (escala, escala, escala); e.rotation_euler = (0.0, 0.0, giro); e.parent = r      # lo que está a altura z se ve z·sen(T) más arriba: se compensa
    return e

def cohete_en(r, x, y, escala=1.0, giro=0.0, z=0.0, llama=True, acento=CORAL):
    """El cohete de Freno/Piloto (`cohete.py`) en (x, y), a [escala] y con [giro]; sin [llama], el motor apagado (parado en la plataforma o posado en la luna)."""
    antes = set(bpy.data.objects)
    s = sub(r, x, y, z, escala, giro, 'cohete_raiz')
    cohete(s, acento)
    if not llama:
        for o in [o for o in bpy.data.objects if o not in antes and o.name.startswith('coh_llama')]: bpy.data.objects.remove(o, do_unlink=True)
    return s

def bola(r, cx, cy, R, color, nombre='bola', seg=96, material=None):
    """Una esfera apoyada en el suelo cuyo contorno se ve centrado en (cx, cy) de diseño (la inclinación de la cámara la sube R·sen(T): se compensa)."""
    return esfera(R, color, loc=(cx, cy - R * math.sin(T), R), nombre=nombre, padre=r, seg=seg, material=material)

def crateres(o, R, lista):
    """Hunde cráteres en la esfera [o] (radio R, centrada en su origen): [lista] de (sx, sy, rd, prof) en la cara que mira a la cámara, (sx, sy) desde el centro del contorno en unidades de diseño."""
    normales = []
    for (sx, sy, rd, prof) in lista:
        q = max(0.0, 1.0 - (sx * sx + sy * sy) / (R * R))
        normales.append(((DERECHA * (sx / R) + ARRIBA * (sy / R) + VISTA * math.sqrt(q)).normalized(), rd, prof))
    for v in o.data.vertices:
        p = Vector(v.co); u = p.normalized(); d = 0.0
        for (n, rd, prof) in normales:
            s = (u - n).length * R
            if s < rd:
                f = (1.0 - (s / rd) ** 2) ** 2                       # hondo al centro, sin esquina en el borde
                d += prof * f - 0.18 * prof * (s / rd) ** 2 * (1.0 - (s / rd) ** 2)       # un labio suave alrededor
        if d: v.co = u * (R - d)
    o.data.update()

def planeta_con_anillo(r, cx, cy, R, con_anillo=True):
    """El planeta de Constelaciones: esfera naranja con dos franjas y el anillo inclinado (0,35 rad; su plano visto con la razón 6,5 / 21)."""
    psi = 0.35; phi = math.asin(6.5 / 21.0)
    L = DERECHA * math.cos(psi) + ARRIBA * math.sin(psi)
    M = ARRIBA * math.cos(psi) - DERECHA * math.sin(psi)
    a = M * math.cos(phi) + VISTA * math.sin(phi)
    ey = a.cross(L)
    m = Matrix((L, ey, a)).transposed()
    e = bpy.data.objects.new('planeta', None); scene.collection.objects.link(e)
    e.location = (cx, (cy - R * math.sin(T)) / math.cos(T), R); e.rotation_euler = m.to_euler()
    s = esfera(R, PLANETA, nombre='planeta_esfera', seg=96); s.parent = e
    if con_anillo:
        an = aro_plano(R * 1.62, R * 0.23, ANILLO, nombre='planeta_anillo'); an.parent = e
    return e

def plataforma(r, y, ancho=0.86, z0=0.0):
    """La plataforma de lanzamiento de Freno, simplificada para 52 y 28 dp: la losa lila con el canto y dos torres más oscuras con dos travesaños de tinta cada una."""
    H = 0.16; TH = 0.22
    pieza(redondeado(rect(0.0, y, ancho, 0.09), 0.12), H, 0.06, LILA, 'pad_losa', padre=r, z0=z0)
    for sx in (-1, 1):
        x = sx * (ancho - 0.20)
        pieza(redondeado(rect(x, y + 0.09 + 0.27, 0.06, 0.27), 0.045), TH, 0.05, LILA_TORRE, 'pad_torre', padre=r, z0=z0 + H)
        for i in range(2):
            ty = y + 0.09 + 0.14 + i * 0.22
            churro_c((x + sx * 0.05, ty), (x - sx * 0.05, ty + 0.10), 0.022, z0 + H + TH, tinta(), 'pad_travesano')

def luceros(r, lista):
    for (cx, cy, R) in lista: bola(r, cx, cy, R, SOL, 'lucero', seg=64)

def nubes(r, lista):
    for (cx, cy, R) in lista: bola(r, cx, cy, R, NUBE, 'nube', seg=48)

# ------------------------------------------------------------------ A · los cinco lugares
def a1(r):   # En la plataforma: la plataforma con el cohete parado (sin llama)
    plataforma(r, -0.70)
    cohete_en(r, 0.0, -0.10, escala=0.85, giro=0.0, z=0.16, llama=False)

def a2(r):   # Despegando: el cohete inclinado, con su llama, y tres nubes de humo abajo a la izquierda
    nubes(r, [(-0.36, -0.74, 0.25), (-0.60, -0.56, 0.17), (-0.66, -0.86, 0.12)])
    cohete_en(r, 0.10, 0.10, escala=0.95, giro=-0.30, llama=True)

def a3(r):   # En órbita: el planeta con su anillo
    planeta_con_anillo(r, 0.0, 0.02, 0.56)

def a4(r):   # En la Luna: la luna llena con cráteres
    o = bola(r, 0.0, 0.0, 0.80, LUNA, 'luna', seg=200)
    crateres(o, 0.80, [(-0.30, 0.22, 0.27, 0.13), (0.36, -0.08, 0.22, 0.11), (-0.12, -0.46, 0.18, 0.10), (0.30, 0.44, 0.14, 0.08), (-0.54, -0.24, 0.12, 0.07), (0.04, 0.60, 0.10, 0.06)])

def a5(r):   # Entre estrellas: tres luceros redondos
    luceros(r, [(-0.36, -0.22, 0.42), (0.46, 0.42, 0.28), (0.44, -0.52, 0.19)])

# ------------------------------------------------------------------ B · el viaje del cohete
b1, b2 = a1, a2

def b3(r):   # En órbita: el cohete recorriendo el anillo del planeta
    planeta_con_anillo(r, -0.06, 0.06, 0.48)
    cohete_en(r, 0.58, -0.46, escala=0.42, giro=-0.95, z=0.95, llama=True)

def b4(r):   # En la Luna: el cohete posado sobre la luna (sin llama)
    o = bola(r, 0.0, -0.40, 0.52, LUNA, 'luna', seg=200)
    crateres(o, 0.52, [(-0.22, 0.08, 0.17, 0.08), (0.24, -0.16, 0.15, 0.07), (-0.06, -0.36, 0.11, 0.06), (0.28, 0.22, 0.09, 0.05)])
    cohete_en(r, 0.0, 0.43, escala=0.45, giro=0.0, z=0.80, llama=False)

def b5(r):   # Entre estrellas: el cohete volando entre luceros
    luceros(r, [(-0.62, 0.52, 0.22), (0.66, 0.26, 0.17), (-0.56, -0.60, 0.13)])
    cohete_en(r, 0.02, 0.0, escala=0.72, giro=-0.25, z=0.6, llama=True)

EMBLEMAS = {'a1': (a1, (0.0, -0.12)), 'a2': (a2, (0.0, 0.0)), 'a3': (a3, (0.0, 0.0)), 'a4': (a4, (0.0, 0.0)), 'a5': (a5, (0.0, 0.0)),
            'b1': (b1, (0.0, -0.12)), 'b2': (b2, (0.0, 0.0)), 'b3': (b3, (0.0, 0.0)), 'b4': (b4, (0.0, 0.0)), 'b5': (b5, (0.0, 0.0))}

preparar()
for nombre, (construir, centro) in EMBLEMAS.items():
    if SOLO == 'todo' or nombre in SOLO.split(',') or nombre[0] in SOLO.split(','):
        una('emblema_' + nombre, construir, LIENZO, LIENZO, PX / LIENZO, centro=centro)
termina()
