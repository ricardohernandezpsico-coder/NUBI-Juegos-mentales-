# -*- coding: utf-8 -*-
"""El cohete de plasticina que comparten Piloto Estelar (la nave del jugador, acento coral) y Freno de Emergencia (los cohetes de cada carril, acento coral, cielo o lima): casco crema en forma de hoja (la intersección de dos círculos), punta y aletas del color de acento, ventanilla celeste
con aro de tinta y llama de sol con el centro coral. Mirando hacia ARRIBA (y arriba), en las unidades de diseño de `PilotShipSprite` (la nave mide de y = −0,90, la punta de la llama, a y = +0,62, la punta del casco).
Solo se importa desde scripts que corren dentro de Blender."""
import math, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from plasticina import *
from formas import engorda, recorta_banda

COHETE_H = 0.14                       # el alto del casco

def contorno_casco(n=40):
    """El casco: la intersección de dos círculos de radio 0,92 centrados en (±0,62, −0,06), cortada abajo en y = −0,58 (una hoja con la punta arriba)."""
    def x_de(y): return math.sqrt(max(0.0, 0.92 ** 2 - (y + 0.06) ** 2)) - 0.62
    ys = [-0.58 + (0.62 + 0.58) * i / n for i in range(n + 1)]
    return [(x_de(y), y) for y in ys] + [(-x_de(y), y) for y in reversed(ys[:-1])]

def ccw(pts):
    a = sum(pts[i][0] * pts[(i + 1) % len(pts)][1] - pts[(i + 1) % len(pts)][0] * pts[i][1] for i in range(len(pts)))
    return pts if a > 0 else list(reversed(pts))

def cohete(r, acento='#FF6B4A'):
    """Arma el cohete bajo la raíz [r]; [acento] es el color de la punta y las aletas."""
    # la llama (debajo de todo): sol con el centro coral
    pieza(casco_convexo([(0.0, -0.56, 0.17 + 0.055), (0.0, -0.90, 0.04 + 0.055)]), 0.05, 0.02, '#FFC93C', 'coh_llama', padre=r)
    pieza(casco_convexo([(0.0, -0.58, 0.08), (0.0, -0.76, 0.02)]), 0.04, 0.015, '#FF6B4A', 'coh_llama_centro', z0=0.04, padre=r)
    # las aletas
    for sx in (1, -1):
        pieza(redondeado(ccw([(sx * 0.20, -0.10), (sx * 0.52, -0.46), (sx * 0.52, -0.68), (sx * 0.20, -0.52)]), 0.075), 0.07, 0.03, acento, 'coh_aleta', padre=r)
    pieza(engorda(contorno_casco(), 0.07), COHETE_H, 0.06, '#FFF8EC', 'coh_casco', padre=r, panza=0.004)
    # la punta: el casco cortado por arriba de y = 0,26
    pieza(recorta_banda(engorda(contorno_casco(), 0.055), 0.26, 2.0), 0.04, 0.02, acento, 'coh_punta', z0=COHETE_H - 0.012, padre=r)
    # la ventanilla: aro de tinta, cristal celeste y un brillo
    aro_plano(0.165, 0.03, material=tinta(), loc=(0, 0.02, COHETE_H + 0.03), nombre='coh_aro', padre=r)
    disco(0.15, 0.05, 0.02, '#7FD8FF', loc=(0, 0.02, COHETE_H - 0.005), nombre='coh_cristal', padre=r)
    esfera(0.035, '#FFFFFF', loc=(-0.05, 0.07, COHETE_H + 0.075), escala=(1.2, 0.8, 0.6), nombre='coh_brillo', padre=r)
