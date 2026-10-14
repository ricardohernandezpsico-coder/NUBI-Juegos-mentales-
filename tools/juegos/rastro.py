# -*- coding: utf-8 -*-
"""Rastro de luz en plasticina (Tarea 99): los 9 luceros. Las píldoras del marcador y el panel del final salen de las losas del kit (`comun`); los íconos de los modos, la ✗, los aros, el disco punteado y la viñeta se quedan procedurales (se tiñen o giran).

Uso: blender -b -P tools/juegos/rastro.py -- [--solo 0..8] [--samples N] [--out DIR]
Salida (en --out, por defecto tools/juegos/out): `lucero_0.png` … `lucero_8.png`, las «maestras» con fondo transparente; `empaqueta.py rastro` las reduce a 192 px.

Cada lucero sale en el MISMO lienzo y a la MISMA escala que el de hoy (`RastroSprites.OrbPixels`: lienzo de ±1 y la esfera ocupa el 94 %): el controlador no cambia ninguna medida. La identidad de un lucero es SOLO su posición, su color y su nota: las 9 esferas son idénticas
(mismo tamaño, misma luz, sin grumos ni marcas propias) y llevan el color de diseño de `RastroBoard.Colors` (los pares cercanos —7FD8FF / A9C7FF, FFC94A / FFE27A y 9FF5D6 / 8EE07A— tienen que seguir distinguiéndose: `mide_rastro.py`). Redondos, SIN puntas ni símbolos.
Los luceros flotan: sin la sombra de contacto sobre el suelo (la de una esfera tan alta se saldría del lienzo y quedaría cortada en seco); el halo que respira va detrás, procedural.
"""
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from plasticina import *

SOLO = arg('--solo', 'todo')
COLORES = (0x7FD8FF, 0xB8A4FF, 0xFFC94A, 0x8EE07A, 0xFF9F7A, 0x9FF5D6, 0xF7A8E0, 0xFFE27A, 0xA9C7FF)     # RastroBoard.Colors
LIENZO = 2.0
PX = 384                                                  # la maestra: se reduce a 192 (el lucero mide 56 dp = 168 u; el sprite 179 u)
R = 0.94                                                  # la esfera ocupa el 94 % del lienzo

def lucero(color):
    def construir(r):
        bpy.data.objects['juego_suelo'].hide_render = True          # flota: sin sombra de contacto
        esfera(R, '#%06X' % color, loc=(0, 0, R), nombre='lucero', seg=96)
    return construir

preparar()
for i, c in enumerate(COLORES):
    if SOLO in ('todo', str(i)): una(f'lucero_{i}', lucero(c), LIENZO, LIENZO, PX / LIENZO, centro=(0.0, R * math.sin(math.radians(TILT))))
termina()
