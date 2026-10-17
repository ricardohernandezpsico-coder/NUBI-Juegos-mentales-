# -*- coding: utf-8 -*-
"""Acoplamiento v3 (Tarea 117, SOLO PROPUESTA: ningún juego lo usa): lo que sale de Blender con el material y la luz del kit para la hoja `propuesta_acoplamiento_v3.py`.
- los íconos de «Encaja» (tres bloques en L que bajan a su soporte en U) y «Espejo» (dos L espejadas con un eje de puntos): churros de TINTA, el mismo tratamiento de `ico_play` y de los íconos de VERDAD / DISPARATE;
- los 4 chevrones del puerto como un churro de plasticina en cuatro tonos (verde agua en reposo, lima al encajar, celeste al mostrar, apagado);
- las luces de la fila de avance: una bolita redonda de plasticina (sin puntas) apagada, encendida (sol) y completa (lima).
Uso: blender -b -P tools/juegos/acoplamiento_v3.py -- [--solo iconos|chevrones|luces] [--samples N] [--out DIR]   (salida por defecto: tools/juegos/out/acoplamiento_v3)"""
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from plasticina import *

SOLO = arg('--solo', 'todo')
ICO_LIENZO = 2.0
ICO_PX = 256                                              # la maestra: 128 px por unidad

def churro_curva(pts, r, z, material, nombre='trazo'):
    """Un churro que sigue una curva (puntos de DISEÑO, y arriba) a la altura [z]: UN tubo con las puntas redondas (la misma función de `disparate.py`)."""
    k = 1 / math.cos(math.radians(TILT))
    cu = bpy.data.curves.new(nombre, 'CURVE'); cu.dimensions = '3D'; cu.bevel_depth = r; cu.bevel_resolution = 8; cu.resolution_u = 4
    sp = cu.splines.new('POLY'); sp.points.add(len(pts) - 1)
    for p, (x, y) in zip(sp.points, pts): p.co = (x, y * k, z, 1.0)
    o = bpy.data.objects.new(nombre, cu); scene.collection.objects.link(o); link(o, 'iconos'); cu.materials.append(material)
    for (x, y) in (pts[0], pts[-1]):
        c = sphere(r=r, seg=24, loc=(x, y * k, z), coll='iconos', name=nombre + '_punta'); c.data.materials.append(material)

def bloque(r, cx, cy, medio=0.16, redondeo=0.06, h=0.20):
    """Un bloque de tinta (cuadrado redondeado de lado 2 × (medio + redondeo)) con el canto blando."""
    pieza(redondeado(rect(cx, cy, medio, medio), redondeo), h, 0.065, material=tinta(), nombre='ico_bloque', padre=r)

def ico_encaja(r):
    """Tres bloques (una L) que bajan a su soporte (una U): la pieza que encaja en el puerto."""
    for (cx, cy) in ((-0.27, 0.52), (0.27, 0.52), (0.27, -0.02)): bloque(r, cx, cy, 0.19, 0.06)
    churro_curva([(-0.66, -0.26), (-0.66, -0.64), (0.66, -0.64), (0.66, -0.26)], 0.10, 0.10, tinta(), 'ico_soporte')

def ico_espejo(r):
    """Dos L espejadas con un eje vertical de puntos: la pieza y su espejo."""
    for sx in (-1, 1):
        for (cx, cy) in ((0.80, 0.50), (0.80, 0.02), (0.38, 0.02)): bloque(r, sx * cx, cy, 0.12, 0.05, 0.17)
    for yy in (0.72, 0.38, 0.04, -0.30, -0.64):
        churro_c((0.0, yy), (0.0, yy + 0.001), 0.07, 0.07, tinta(), 'ico_eje')

def render_iconos():
    una('ico_encaja', ico_encaja, ICO_LIENZO, ICO_LIENZO, ICO_PX / ICO_LIENZO, centro=(0.0, sube(0.2) / 2))
    una('ico_espejo', ico_espejo, ICO_LIENZO, ICO_LIENZO, ICO_PX / ICO_LIENZO, centro=(0.0, sube(0.2) / 2))

# ------------------------------------------------------------------ chevrones (una «V» de churro: 1,24 × 0,44 unidades, trazo de 0,34)
CHEV = (('verde_agua', '#5CC8C2'), ('lima', '#A6E36B'), ('celeste', '#7FD8FF'), ('apagado', '#3A3680'))

def chevron(color):
    def construir(r):
        mat = clay('chev_' + color.strip('#'), color, **MAT_KIT)
        churro_curva([(-0.62, 0.20), (0.0, -0.20), (0.62, 0.20)], 0.17, 0.17, mat, 'chevron')
    return construir

def render_chevrones():
    for nombre, color in CHEV: una('chev_' + nombre, chevron(color), 2.0, 1.2, 96.0, centro=(0.0, sube(0.17) / 2))

# ------------------------------------------------------------------ luces de la fila de avance: una bolita redonda (disco bajo con el canto muy blando)
def luz(color):
    def construir(r): disco(0.8, 0.34, 0.17, color, loc=(0, 0, 0), nombre='luz', padre=r, verts=72)
    return construir

def render_luces():
    with material_kit(spec=0.05, sheen=0.0):
        una('luz_apagada', luz('#2A2566'), 2.2, 2.2, 128 / 2.2, centro=(0.0, sube(0.34) / 2))
    una('luz_oro', luz(KIT_SOL), 2.2, 2.2, 128 / 2.2, centro=(0.0, sube(0.34) / 2))
    una('luz_lima', luz(KIT_LIMA), 2.2, 2.2, 128 / 2.2, centro=(0.0, sube(0.34) / 2))

preparar()
if SOLO in ('todo', 'iconos'): render_iconos()
if SOLO in ('todo', 'chevrones'): render_chevrones()
if SOLO in ('todo', 'luces'): render_luces()
termina()
