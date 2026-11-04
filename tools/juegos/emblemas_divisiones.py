# -*- coding: utf-8 -*-
"""Los emblemas de plasticina de las cinco divisiones del Contrarreloj (D3; diseño de Fable, 2-nov-2026; Tarea 212: Ricardo eligió la dirección A «Los cinco lugares»; el diseño completo está en `auditoria-fable/emblemas-divisiones.md`).

Dos direcciones, cinco emblemas cada una, con las MISMAS piezas y la misma luz y material que el kit y los juegos (`tools/juegos/plasticina.py`,
que toma `clay()`/`studio()` de `tools/isla/isla.py`; el cohete es `tools/juegos/cohete.py`, el de Freno y Piloto):
  A «Los cinco lugares»: un solo objeto grande por división, el lugar que dice el nombre.
     a1 plataforma (la losa lila con dos torres de Freno) con el cohete parado · a2 el cohete despegando con sus nubes · a3 el planeta con anillo (Constelaciones) ·
     a4 la luna llena con cráteres (una esfera; símbolos neutros: nada de media luna) · a5 tres luceros redondos (Rastro, Meteoros).
  B «El viaje del cohete» (descartada por Ricardo, queda como registro): el cohete en las cinco escenas (b1 = a1, b2 = a2, b3 en órbita del planeta, b4 posado en la luna, b5 volando entre luceros).
  C «Los cinco lugares de más de la liga de 10 divisiones» (Tarea 228, 4-nov-2026; `docs/liga-de-la-semana.md` §1.1; la dirección A con cinco lugares nuevos, sin tocar los cinco de arriba): c1 sobre las nubes (un banco de cinco bolas de nube) ·
     c2 en Marte (esfera terracota con casquete polar y cráteres bajos, sin anillo) · c3 entre asteroides (cuatro rocas irregulares pardas con cráteres chicos) · c4 en Júpiter (esfera con bandas horizontales y la mancha roja oval, sin anillo) ·
     c5 en otra galaxia (espiral de dos brazos de bolitas lila y sol que achican hacia afuera, con centro claro).

Cada emblema sale en un lienzo CUADRADO de 2 unidades de diseño a 220 px por unidad (440 px, la «maestra»); en la app (`ui/components/DivisionEmblem.kt`) el objeto ocupa el 86 % de una ventanilla noche de 52 dp en la cifra y de 28 dp en la escalera.
Uso:  blender -b -P tools/juegos/emblemas_divisiones.py -- [--solo a|b|c|a1..c5] [--samples N] [--out DIR]       (a 112 muestras, unos 40 s por emblema)
Después:  python -I tools/juegos/emblemas_divisiones_app.py     (reduce las cinco maestras y las deja en app/src/main/res/drawable-nodpi/division_*.webp)
"""
import math, os, sys
import bmesh
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
MARTE = '#BE4F38'                                           # terracota oscura (Tarea 228)
ROCA = ('#A8968A', '#9A8878', '#B5A395', '#8F7F70')         # gris cálido / pardo de las rocas
JUP_BASE, JUP_ARENA, JUP_NARANJA, JUP_NARANJA2, JUP_MANCHA = '#F6E7C8', '#E6C58F', '#E58B4C', '#D9753F', '#D0503A'

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

def cascara(r, cx, cy, R, color, margen, grosor=1.014, eps=0.035, nombre='cascara', seg=300, piso=0.975):
    """Una cáscara de plasticina pegada a una esfera de radio R centrada en (cx, cy): otra esfera un poco mayor ([grosor]) cuyos vértices se hunden bajo la esfera de abajo donde [margen](u) (u = vector unitario local; positivo = dentro de la zona) es negativo, con una
    transición suave de ancho [eps] (hasta el radio [piso] × R: más hondo que los cráteres de la esfera, para que no asomen): la costura es una curva lisa. Sirve para el casquete polar, las bandas y las manchas."""
    o = esfera(R * grosor, color, loc=(cx, cy - R * math.sin(T), R), nombre=nombre, padre=r, seg=seg)
    for v in o.data.vertices:
        u = Vector(v.co).normalized()
        m = min(margen(u), u.z - u.y * math.sin(T) + 0.10)           # fuera de la mitad que mira a la cámara, también se hunde
        w = min(1.0, max(0.0, 0.5 + m / (2.0 * eps)))
        v.co = u * (R * (piso + (grosor - piso) * w))
    o.data.update()
    return o

def altura(u):
    """La altura en la IMAGEN (-1 abajo, 1 arriba) de un punto de la esfera con vector unitario local u: sus bandas horizontales son los niveles de esta altura."""
    return (u.y + u.z * math.sin(T)) / math.sqrt(1.0 + math.sin(T) ** 2)

def de_frente(u):
    return u.z > u.y * math.sin(T) - 0.10                  # la mitad que mira a la cámara (con un poco de margen para que la cáscara llegue al borde)

def abolla(o, semilla, amp=0.20):
    """Deforma una esfera en una roca irregular: cada vértice se corre a lo largo de su radio según tres ondas que dependen de la dirección (mismo resultado en cada corrida)."""
    s = float(semilla)
    for v in o.data.vertices:
        p = Vector(v.co); u = p.normalized()
        f = 1.0 + amp * (0.50 * math.sin(2.6 * u.x + 1.7 * s) + 0.40 * math.sin(3.1 * u.y + 0.9 * s + 1.3 * u.z) + 0.35 * math.sin(3.9 * u.z + 2.3 * s - 1.1 * u.x))
        v.co = p * f
    o.data.update()

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

# ------------------------------------------------------------------ C · los cinco lugares de más de la liga de 10 divisiones (Tarea 228)
def c1(r):   # Sobre las nubes: un banco de cinco bolas de nube en dos filas, que llena el ancho (sin cohete: la nube ES el objeto)
    nubes(r, [(-0.58, -0.30, 0.38), (-0.02, -0.36, 0.46), (0.56, -0.30, 0.38), (-0.30, 0.20, 0.36), (0.26, 0.26, 0.40)])

def c2(r):   # En Marte: esfera terracota oscura con un casquete polar crema arriba y tres cráteres bajos; sin anillo
    R = 0.82
    o = bola(r, 0.0, 0.0, R, MARTE, 'marte', seg=200)
    crateres(o, R, [(-0.34, -0.10, 0.26, 0.11), (0.30, -0.38, 0.20, 0.09), (0.38, 0.12, 0.14, 0.07)])
    polo = Vector((0.0, 0.78, 0.625)).normalized()                      # el polo, inclinado hacia la cámara para que el casquete se vea
    cascara(r, 0.0, 0.0, R, CREMA, lambda u: (u.dot(polo) - 0.77) * 1.0, grosor=1.028, eps=0.03, piso=0.80, nombre='marte_casquete')

def c3(r):   # Entre asteroides: cuatro rocas irregulares (esferas abolladas) de tamaños distintos, gris cálido y pardo, con cráteres chicos
    rocas = [(-0.30, 0.20, 0.50, 0, (1.0, 0.86, 0.92), [(-0.14, 0.10, 0.14, 0.07), (0.18, -0.14, 0.10, 0.055)]),
             (0.46, -0.24, 0.38, 1, (0.92, 1.0, 0.86), [(-0.08, 0.06, 0.11, 0.06)]),
             (-0.44, -0.56, 0.26, 2, (1.0, 0.9, 0.9), [(0.05, 0.04, 0.08, 0.045)]),
             (0.54, 0.50, 0.20, 3, (0.9, 1.0, 0.9), [])]
    for (cx, cy, R, k, esc, cr) in rocas:
        o = bola(r, cx, cy, R, ROCA[k], 'roca%d' % k, seg=64); o.scale = esc
        if cr: crateres(o, R, cr)
        abolla(o, 1.3 + 2.1 * k, 0.22)

def c4(r):   # En Júpiter: esfera grande con bandas horizontales de plasticina (crema, arena y naranja) y la mancha roja oval; sin anillo
    R = 0.84
    bola(r, 0.0, 0.0, R, JUP_BASE, 'jupiter', seg=200)
    for (lo, hi, color) in [(0.72, 1.02, JUP_ARENA), (0.50, 0.64, JUP_NARANJA), (0.22, 0.40, JUP_ARENA), (-0.04, 0.12, JUP_NARANJA2), (-0.32, -0.14, JUP_ARENA), (-0.58, -0.44, JUP_NARANJA), (-1.02, -0.78, JUP_ARENA)]:
        cascara(r, 0.0, 0.0, R, color, lambda u, lo=lo, hi=hi: min(altura(u) - lo, hi - altura(u)), nombre='jupiter_banda')
    # la mancha roja: un óvalo sobre la banda de abajo, un poco a la derecha
    u0 = Vector((0.40, -0.655, 0.641)); s0 = altura(u0)
    cascara(r, 0.0, 0.0, R, JUP_MANCHA, lambda u: (1.0 - math.sqrt(((u.x - u0.x) / 0.21) ** 2 + ((altura(u) - s0) / 0.095) ** 2)) * 0.09, grosor=1.03, eps=0.02, nombre='jupiter_mancha')

def c5(r):   # En otra galaxia: dos brazos en espiral de bolitas redondas (lila y sol) que achican hacia afuera, alrededor de un centro claro
    bola(r, 0.0, 0.0, 0.25, CREMA, 'galaxia_centro', seg=64)
    for brazo, color in ((0, LILA), (1, SOL)):
        for i in range(8):
            t = i / 7.0
            rad = 0.38 + 0.50 * t
            ang = math.pi * brazo + 0.45 + 3.4 * t
            bola(r, rad * math.cos(ang), rad * math.sin(ang) * 0.95, 0.175 - 0.085 * t, color, 'galaxia_bola', seg=48)

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
            'b1': (b1, (0.0, -0.12)), 'b2': (b2, (0.0, 0.0)), 'b3': (b3, (0.0, 0.0)), 'b4': (b4, (0.0, 0.0)), 'b5': (b5, (0.0, 0.0)),
            'c1': (c1, (0.0, -0.06)), 'c2': (c2, (0.0, 0.0)), 'c3': (c3, (0.0, 0.0)), 'c4': (c4, (0.0, 0.0)), 'c5': (c5, (0.0, 0.0))}

preparar()
for nombre, (construir, centro) in EMBLEMAS.items():
    if SOLO == 'todo' or nombre in SOLO.split(',') or nombre[0] in SOLO.split(','):
        una('emblema_' + nombre, construir, LIENZO, LIENZO, PX / LIENZO, centro=centro)
termina()
