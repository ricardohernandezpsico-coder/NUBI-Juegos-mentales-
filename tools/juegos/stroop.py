# -*- coding: utf-8 -*-
"""Tinta o Palabra en plasticina (Tarea 100): las losas con el COLOR EXACTO de cada tinta, de la tarjeta y de las orillas, y los dos íconos de las reglas (la gota y el libro). Todo lo demás sale de las losas del kit (`comun`) o se queda procedural (los haces de luz, el aro,
los puntos, el reloj, la palabra, que sigue siendo `Text`).

Uso: blender -b -P tools/juegos/stroop.py -- [--solo losas|iconos] [--samples N] [--out DIR]
Salida (en --out, por defecto tools/juegos/out): las losas (`losa_<clase>_<nombre>.png`, el mismo formato que las del kit: cuerpo con los márgenes de la sombra) y los íconos; `empaqueta.py stroop` las alinea al lienzo del kit y arma el atlas.

EL COLOR DE LA TINTA ES EL ESTÍMULO: los 4 botones de respuesta (ROJO C93C3C, AZUL 4A86E8, AMARILLO F2CC1D, BLANCO F4F4F4: validados con `tools/paleta_daltonismo.py`, separación ≥ 20 también en protanopía y deuteranopía) y el fondo de la tarjeta (#141B36: sobre él se midieron los
contrastes de las 4 tintas) se renderizan con `material_kit(spec=0.05, sheen=0.0)`: sin el brillo normal del kit, que le suma un gris de ~45/255 a la cara plana y movería el tono. `mide_stroop.py` comprueba que el color medio de cada cara quede a ΔE ≤ 5 del de diseño y que la separación
entre tintas se mantenga. Las losas de las orillas (violeta 7C5CE0, turquesa 159A8C) y de la cinta no son tintas de respuesta: siempre van con ícono y texto.
"""
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from plasticina import *

SOLO = arg('--solo', 'todo')
# (nombre, color de DISEÑO, color que se le da a Blender, radio en dp, con versión presionada). La luz cálida del kit sube el rojo y baja el azul de la cara (hasta ±13/255): el color que se le da a Blender es el de diseño CORREGIDO por lo que se midió en una primera pasada
# (diseño − diferencia), así la cara sale a ΔE ≤ 5 del de diseño (`mide_stroop.py` lo comprueba sobre el atlas empaquetado). Para recalibrar: renderizar con el color de diseño en las dos columnas, medir y volver a corregir.
LOSAS = (
    ('md_boton_rojo', '#C93C3C', '#C23C3E', 22.0, True), ('md_boton_azul', '#4A86E8', '#478AF4', 22.0, True), ('md_boton_amarillo', '#F2CC1D', '#E5CD1B', 22.0, True), ('md_boton_blanco', '#F4F4F4', '#E9F4FC', 22.0, True),
    ('md_cinta_tinta', '#4D3F93', '#4A409D', 22.0, False), ('md_cinta_palabra', '#156165', '#0F646C', 22.0, False),
    ('gr_tarjeta_fondo', '#141B36', '#0D1838', 30.0, False), ('gr_tarjeta_tinta', '#7C5CE0', '#785FEC', 30.0, False), ('gr_tarjeta_palabra', '#159A8C', '#0E9F95', 30.0, False),
    ('ch_chip_tinta', '#7C5CE0', '#795FEC', 14.0, False), ('ch_chip_palabra', '#159A8C', '#0F9F95', 14.0, False),
)

def render_losas():
    with material_kit(spec=0.05, sheen=0.0):
        for nombre, diseno, color, radio, presionada in LOSAS:
            losa('losa_' + nombre, color, radio=radio)
            if presionada: losa('losa_' + nombre + '_p', color, presionada=True, radio=radio)

# ------------------------------------------------------------------ los íconos de las reglas (StroopSprites.RenderDrop y RenderBook: lienzo ±1,1, en blanco para teñir con Image.color; con el borde de tinta de 0,085)
ICO_LIENZO = 2.2
ICO_PX = 256                                              # la maestra: se reduce a 128
L = 0.085
BLANCO = '#F4F4F6'
ICO_H = 0.22

def gota(r):
    pieza(casco_convexo([(0.0, -0.2, 0.42 + L), (0.0, 0.58, 0.05 + L)]), ICO_H, 0.10, BLANCO, 'gota', padre=r)

def libro(r):
    for sx in (-1, 1): pieza(redondeado(rect(sx * 0.42, 0, 0.37, 0.55), 0.07 + L), ICO_H, 0.08, BLANCO, 'libro_pagina', padre=r)
    z = ICO_H
    churro_c((0, -0.58), (0, 0.58), 0.045, z, tinta(), 'libro_lomo')
    for k in range(3):
        yy = 0.30 - k * 0.26
        churro_c((-0.78, yy), (-0.16, yy), 0.04, z, tinta(), 'libro_renglon')
        churro_c((0.16, yy), (0.78, yy), 0.04, z, tinta(), 'libro_renglon')

def render_iconos():
    una('gota', gota, ICO_LIENZO, ICO_LIENZO, ICO_PX / ICO_LIENZO, centro=(0.0, sube(ICO_H) / 2))
    una('libro', libro, ICO_LIENZO, ICO_LIENZO, ICO_PX / ICO_LIENZO, centro=(0.0, sube(ICO_H) / 2))

preparar()
if SOLO in ('todo', 'losas'): render_losas()
if SOLO in ('todo', 'iconos'): render_iconos()
termina()
