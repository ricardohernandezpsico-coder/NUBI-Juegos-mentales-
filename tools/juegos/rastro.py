# -*- coding: utf-8 -*-
"""Rastro de luz en plasticina (Tarea 99): los 9 luceros. Las píldoras del marcador y el panel del final salen de las losas del kit (`comun`); los íconos de los modos, la ✗, los aros, el disco punteado y la viñeta se quedan procedurales (se tiñen o giran).

Uso: blender -b -P tools/juegos/rastro.py -- [--solo 0..8|p|p0..p8] [--samples N] [--out DIR]
Salida (en --out, por defecto tools/juegos/out): `lucero_0.png` … `lucero_8.png` y sus estados presionados `lucero_p_0.png` … `lucero_p_8.png` (la esfera aplastada ~8 % más baja y ~4 % más ancha, un punto más oscura y con una huella de dedo hundida), las «maestras» con fondo transparente; `empaqueta.py rastro` las reduce a 192 px.

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

# ------------------------------------------------------------------ el lucero PRESIONADO (regla del 14-oct: todo objeto de plasticina que se toca tiene su estado presionado, aplastado y con huella)
PRES_ALTO, PRES_ANCHO = 0.92, 1.04                         # la esfera aplastada: ~8 % más baja y ~4 % más ancha
PRES_OSCURA = 0.93                                         # un punto más oscura (el mismo `oscurece` de las losas presionadas del kit)

def huella(o, radio):
    """Hunde en la esfera [o] una huella de dedo: un óvalo suave y poco profundo en la cara que mira a la cámara, con unas crestas finas como las de la yema. Se aplica a los vértices ANTES de que el objeto se aplaste (la escala va después)."""
    t = math.radians(TILT)
    n = Vector((0.0, -math.sin(t) / PRES_ANCHO, math.cos(t) / PRES_ALTO)).normalized()      # hacia la cámara, en el espacio del objeto sin aplastar
    e1 = (Vector((1.0, 0.0, 0.0)) - n * n.x).normalized(); e2 = n.cross(e1)
    ang = math.radians(18); ca, sa = math.cos(ang), math.sin(ang)
    rd, prof, cresta, lam = 0.55 * radio, 0.17 * radio, 0.008 * radio, 0.14 * radio
    for v in o.data.vertices:
        p = Vector(v.co)
        if p.dot(n) < 0.5 * radio: continue
        u, w = p.dot(e1), p.dot(e2)
        uu, ww = u * ca + w * sa, (-u * sa + w * ca) / 1.3                                    # el óvalo: más alto que ancho y un poco girado, como la yema
        s_ = math.hypot(uu, ww)
        if s_ >= rd: continue
        f = (1.0 - (s_ / rd) ** 4) ** 2                                                       # 1 en el fondo (plano) y 0, sin esquina, en el borde: una pared que se ve
        d = prof * f - cresta * f * math.cos(2 * math.pi * s_ / lam)                          # hundido, con crestas finas
        v.co = p.normalized() * (p.length - d)
    o.data.update()

def lucero_presionado(color):
    def construir(r):
        bpy.data.objects['juego_suelo'].hide_render = True
        o = esfera(R, oscurece('#%06X' % color, PRES_OSCURA), loc=(0, 0, R * PRES_ALTO), escala=(PRES_ANCHO, PRES_ANCHO, PRES_ALTO), nombre='lucero_p', seg=320)
        huella(o, R)
    return construir

preparar()
for i, c in enumerate(COLORES):
    if SOLO in ('todo', str(i)): una(f'lucero_{i}', lucero(c), LIENZO, LIENZO, PX / LIENZO, centro=(0.0, R * math.sin(math.radians(TILT))))
    if SOLO in ('todo', 'p', f'p{i}'): una(f'lucero_p_{i}', lucero_presionado(c), LIENZO, LIENZO, PX / LIENZO, centro=(0.0, R * PRES_ALTO * math.sin(math.radians(TILT))))      # el lienzo centrado en la esfera aplastada: la imagen no «salta» al cambiar y no se pega al borde de abajo
termina()
