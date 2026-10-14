# -*- coding: utf-8 -*-
"""Carga exacta en plasticina (Tarea 97): el aro del reactor y su núcleo. Las celdas, los botones de operación, «Deshacer» / «Empezar de nuevo» y el panel del final salen de las losas del kit (`comun`): no se renderiza nada para ellos.

Uso: blender -b -P tools/juegos/calculo.py -- [--solo aro|nucleo] [--samples N] [--out DIR]
Salida (en --out, por defecto tools/juegos/out): `reactor_aro.png` y `reactor_nucleo.png`, las «maestras» con fondo transparente; `empaqueta.py calculo` las reduce al tamaño final.

El diseño está en los dp lógicos de `CargaLayout` (360 de ancho; 1 dp = 3 u del lienzo de 1080): el reactor mide RingR = 84 dp de radio (aro con luces) y su núcleo CoreR = 72 dp. Acá se modela con 1 unidad de Blender = 84 dp (la luz y la cámara del estudio están hechas para
objetos de ~1 unidad: a escala de dp el plano cercano de la cámara cortaba la imagen). Las dos imágenes comparten el mismo lienzo cuadrado de ±92 dp centrado en el reactor, así el código las pone en el mismo rectángulo y no cambia ninguna medida.
  - `reactor_aro`: el aro de plasticina (radios 70 a 84 dp), azul noche (#232B57, el cuerpo de hoy). Las 12 luces que giran, el resplandor y el brillo del núcleo se quedan procedurales (son luz).
  - `reactor_nucleo`: el núcleo, un domo de plasticina GRIS CLARO de 72 dp de radio: el código lo tiñe (azul noche → dorado al lograr la carga) con `Image.color`.
"""
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from plasticina import *

SOLO = arg('--solo', 'todo')
DP = 84.0                                     # 1 unidad = 84 dp (el radio del reactor entero)
LIENZO = 184.0 / DP                           # ±92 dp
PX_U = 828.0 / LIENZO                         # la maestra: 828 px (4,5 px/dp); el empaquetador la reduce a 3 px/dp (552 px, el lienzo de 1080)
RING_R, INNER_R, CORE_R = 84.0 / DP, 70.0 / DP, 72.0 / DP
H_ARO = 14.0 / DP
H_NUCLEO = 10.0 / DP
AZUL_NOCHE = '#232B57'

def aro(r):
    R = (RING_R + INNER_R) / 2; t = (RING_R - INNER_R) / 2           # una dona de radios 70 a 84 dp
    bpy.ops.mesh.primitive_torus_add(major_radius=R, minor_radius=t, major_segments=192, minor_segments=24)         # (el `torus` del kit trae 48 facetas: a este tamaño se ven en el borde)
    o = bpy.context.object; smooth(o); place(o, loc=(0, 0, t), coll='iconos', name='reactor_aro'); o.data.materials.append(mat_oscuro('m_j_reactor_aro', AZUL_NOCHE)); o.parent = r

def nucleo(r):
    disco(CORE_R, H_NUCLEO, H_NUCLEO * 0.6, '#F2F2F8', nombre='reactor_nucleo', padre=r, verts=96)

preparar()
if SOLO in ('todo', 'aro'): una('reactor_aro', aro, LIENZO, LIENZO, PX_U, centro=(0.0, sube(H_ARO) / 2))
if SOLO in ('todo', 'nucleo'): una('reactor_nucleo', nucleo, LIENZO, LIENZO, PX_U, centro=(0.0, sube(H_NUCLEO) / 2))
termina()
