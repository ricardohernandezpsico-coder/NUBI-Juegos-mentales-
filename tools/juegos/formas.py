# -*- coding: utf-8 -*-
"""Las formas de los juegos con símbolos de silueta (Rescate relámpago y Piloto Estelar): las MISMAS SDF de `RadarSprites.ShapeSdf` y `PilotSignalSprites.ShapeSdf`, como contornos para `plasticina.pieza()`.
Seis tipos (los cinco de Piloto más el rombo de Rescate) con su color FIJO (la silueta es lo que se reconoce; el color es redundante). Unidades de diseño: y arriba, la forma ocupa ±0,92 dentro de un lienzo de ±1,18.
Solo se importa desde los scripts que corren dentro de Blender (usa `plasticina`)."""
import math
import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from plasticina import redondeado, casco_convexo, circulo

TIPOS = ('hexagono', 'gota', 'circulo', 'cuadrado', 'triangulo', 'rombo')            # el orden de CapsuleType (Rescate); Piloto usa los cinco primeros (SignalShape)
COLORES = ('#A6E36B', '#7FD8FF', '#FF8A6B', '#FFC94A', '#B79BFF', '#FFB3D1')
EXTRA = 0.05           # la plasticina no lleva borde tinta (el de ClayRaster engordaba la silueta 0,075): se engorda un poco la forma para que la huella sea la misma

def hexagono(): return [(0.9 * math.cos(math.pi / 6 + i * math.pi / 3), 0.9 * math.sin(math.pi / 6 + i * math.pi / 3)) for i in range(6)]
def triangulo(): return list(reversed([(0.0, 0.98), (0.98, -0.74), (-0.98, -0.74)]))
def rombo(): return list(reversed([(0.0, 1.0), (0.84, 0.0), (0.0, -1.0), (-0.84, 0.0)]))

def contorno_forma(tipo, extra=EXTRA):
    """El contorno (antihorario) de cada forma, engordado [extra] (negativo = encogido: la huella interior donde caben los detalles)."""
    if tipo == 'hexagono': return redondeado(hexagono(), max(0.0, 0.05 + extra))
    if tipo == 'gota': return casco_convexo([(0.0, -0.34, max(0.02, 0.66 + extra)), (0.0, 0.96, max(0.02, 0.07 + extra))])
    if tipo == 'circulo': return circulo(max(0.05, 0.92 + extra), 120)
    if tipo == 'cuadrado': return redondeado([(0.52, 0.52), (-0.52, 0.52), (-0.52, -0.52), (0.52, -0.52)], max(0.0, 0.22 + extra))      # caja de 0,74 con esquinas de 0,22
    if tipo == 'triangulo': return redondeado(triangulo(), max(0.0, 0.08 + extra))
    return redondeado(rombo(), max(0.0, 0.06 + extra))

def recorta_banda(contorno, y0, y1):
    """Recorta un contorno CONVEXO a la banda y0 ≤ y ≤ y1 (Sutherland-Hodgman contra dos semiplanos): la franja de Piloto."""
    def corta(pts, dentro, corte):
        out = []
        for i in range(len(pts)):
            a, b = pts[i - 1], pts[i]
            da, db = dentro(a), dentro(b)
            if db:
                if not da: out.append(corte(a, b))
                out.append(b)
            elif da: out.append(corte(a, b))
        return out
    def en_y(y):
        return lambda a, b: (a[0] + (b[0] - a[0]) * (y - a[1]) / (b[1] - a[1]), y)
    pts = corta(list(contorno), lambda p: p[1] >= y0, en_y(y0))
    pts = corta(pts, lambda p: p[1] <= y1, en_y(y1))
    return pts

def engorda(pts, d):
    """Desplaza un contorno ANTIHORARIO [d] hacia afuera por la normal de cada vértice (el promedio de sus dos aristas, con la corrección de esquina limitada: una punta aguda no se alarga de más)."""
    n = len(pts); out = []
    for i in range(n):
        p0, p1, p2 = pts[i - 1], pts[i], pts[(i + 1) % n]
        n0 = ((p1[1] - p0[1]), -(p1[0] - p0[0])); n1 = ((p2[1] - p1[1]), -(p2[0] - p1[0]))
        l0 = math.hypot(*n0) or 1.0; l1 = math.hypot(*n1) or 1.0
        nx = n0[0] / l0 + n1[0] / l1; ny = n0[1] / l0 + n1[1] / l1
        l = math.hypot(nx, ny) or 1.0
        c = max(0.6, (nx / l) * (n0[0] / l0) + (ny / l) * (n0[1] / l0))
        out.append((p1[0] + nx / l * d / c, p1[1] + ny / l * d / c))
    return out
