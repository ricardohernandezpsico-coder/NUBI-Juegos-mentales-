# -*- coding: utf-8 -*-
"""Engranajes con la plasticina de los botones aprobados (Tarea 116): lo tocable y lo que rotula se hace con el MISMO kit que ¿Verdad o disparate? y Rescate, no con una variante propia.
- las losas CHICAS (ch, radio 14 dp) en los tres colores de los carteles: noche (neutro), acierto y error. Mismas luz, material y márgenes que las del kit; los colores son los de las casillas de Rescate (`comun.py`);
  la pastilla dorada del rótulo MOTOR es la `losa_ch_sol` del kit (grupo `comun`), no se vuelve a hacer;
- `ico_play`: el triángulo de «Arrancar», una cuña de plasticina de tinta con la sombra suave en el suelo, como los íconos de VERDAD y DISPARATE;
- `llave_libre` y `llave_usada`: la llave de la cuenta cambios sobre una moneda de plasticina (lila = libre, sol = usada), el mismo formato de las marcas ✓ / ✗ de lo común (disco de radio 0,8 con el glifo de churro de tinta).
Los engranajes NO se tocan (`engranajes_volumen.py`).

Uso: blender -b -P tools/juegos/engranajes_botones.py -- [--solo losas|iconos] [--samples N] [--out DIR]   (salida por defecto: tools/juegos/out/engranajes_botones)
Después: `python -I tools/juegos/empaqueta.py engranajes` (las pasa al atlas con el nine-slice y los márgenes de la clase ch del kit).
"""
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from plasticina import *

SOLO = arg('--solo', 'todo')
# (nombre, color de DISEÑO). Un color oscuro con el brillo normal del kit sale lechoso: poco especular y sin «sheen» (igual que las casillas «noche» de `comun.py`).
LOSAS = (('losa_ch_noche', '#2A2566'), ('losa_ch_acierto', '#3D6A2C'), ('losa_ch_error', '#6B2E3A'))

def render_losas():
    with material_kit(spec=0.05, sheen=0.0):
        for nombre, color in LOSAS: losa(nombre, color, radio=14.0)

# ------------------------------------------------------------------ íconos (lienzo ±1, 128 px por unidad en la maestra de 256 y se reduce a 128)
ICO_LIENZO = 2.0
ICO_PX = 256

def ico_play(r):
    """Una cuña de plasticina (triángulo redondeado) de tinta, con el borde blando y la cara de arriba plana."""
    tri = [(-0.40, -0.52), (0.52, 0.0), (-0.40, 0.52)]
    pieza(redondeado(tri, 0.20), 0.20, 0.085, material=tinta(), nombre='ico_play', padre=r)

def churro_curva(pts, r, z, material, nombre='trazo'):
    """Un churro que sigue una curva (puntos de DISEÑO, y arriba) a la altura [z]: UN tubo con las puntas redondas (la misma función de `disparate.py`)."""
    k = 1 / math.cos(math.radians(TILT))
    cu = bpy.data.curves.new(nombre, 'CURVE'); cu.dimensions = '3D'; cu.bevel_depth = r; cu.bevel_resolution = 8; cu.resolution_u = 4
    sp = cu.splines.new('POLY'); sp.points.add(len(pts) - 1)
    for p, (x, y) in zip(sp.points, pts): p.co = (x, y * k, z, 1.0)
    o = bpy.data.objects.new(nombre, cu); scene.collection.objects.link(o); link(o, 'iconos'); cu.materials.append(material)
    for (x, y) in (pts[0], pts[-1]):
        c = sphere(r=r, seg=24, loc=(x, y * k, z), coll='iconos', name=nombre + '_punta'); c.data.materials.append(material)

def llave(color):
    """La moneda (disco de radio 0,8, como las marcas de lo común) con una llave inglesa de churro de tinta: la cabeza abierta hacia arriba a la derecha y el mango hacia abajo a la izquierda."""
    H = 0.16
    def construir(r):
        disco(0.8, H, 0.07, color, loc=(0, 0, 0), nombre='llave_disco', padre=r, verts=72)
        z = H + 0.07
        cx, cy, rr = 0.20, 0.20, 0.26                                                       # el centro de la cabeza (todo el dibujo se corre -0,03 para que quede centrado)
        arco = [(cx + rr * math.cos(math.radians(a)) - 0.03, cy + rr * math.sin(math.radians(a)) - 0.03) for a in range(83, 368, 12)]
        churro_curva(arco, 0.105, z, tinta(), 'llave_cabeza')
        churro_c((cx + rr * math.cos(math.radians(225)) - 0.03, cy + rr * math.sin(math.radians(225)) - 0.03), (-0.45, -0.45), 0.105, z, tinta(), 'llave_mango')
    return construir

def render_iconos():
    una('ico_play', ico_play, ICO_LIENZO, ICO_LIENZO, ICO_PX / ICO_LIENZO, centro=(0.0, sube(0.2) / 2))
    una('llave_libre', llave(KIT_LILA), 2.2, 2.2, 128 / 2.2, centro=(0.0, sube(0.16) / 2))
    una('llave_usada', llave(KIT_SOL), 2.2, 2.2, 128 / 2.2, centro=(0.0, sube(0.16) / 2))

preparar()
if SOLO in ('todo', 'losas'): render_losas()
if SOLO in ('todo', 'iconos'): render_iconos()
termina()
