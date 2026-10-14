# -*- coding: utf-8 -*-
"""Piloto Estelar en plasticina (Tarea 95): las 15 señales (5 formas × 3 detalles) y la nave del jugador.

Uso: blender -b -P tools/juegos/piloto.py -- [--solo senales|nave] [--samples N] [--out DIR]
Salida (en --out, por defecto tools/juegos/out/piloto): PNG con fondo transparente (las «maestras» a mayor resolución; `empaqueta.py` las reduce al tamaño final y saca de la misma maestra la versión chica de la tarjeta de misión).

Las señales son las MISMAS formas y colores FIJOS que las cápsulas de Rescate (`formas.py`: las SDF de `PilotSignalSprites.ShapeSdf` son las de `RadarSprites.ShapeSdf`), en el mismo lienzo de ±1,18 y con el mismo grosor de plasticina, SIN la ventanilla: en su lugar,
cada una lleva su detalle de churro de tinta incrustado (la jugabilidad de las «parecidas» es la misma forma y el mismo color con OTRO detalle: tienen que verse planos, grandes y de alto contraste):
  - punto: un disco de tinta de radio 0,27;  - anillo: un aro de tinta de radio 0,42 y 0,15 de ancho;  - franja: una banda de tinta de 0,30 de alto que cruza la forma.
La nave sale de `PilotShipSprite` (mirando hacia arriba): casco en forma de hoja (la intersección de dos círculos, cortada abajo), punta y aletas coral, ventanilla celeste y llama de sol.
"""
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from plasticina import *
from formas import TIPOS, COLORES, EXTRA, contorno_forma, recorta_banda, engorda

SOLO = arg('--solo', 'todo')
FORMAS = TIPOS[:5]                                         # hexágono, gota, círculo, cuadrado, triángulo (Piloto no usa el rombo)
DETALLES = ('punto', 'anillo', 'franja')
SEN_H = 0.30                                               # el mismo grosor que las cápsulas
SEN_LIENZO = 2.36                                          # ±1,18 como PilotSignalSprites (Zoom 1,18)
SEN_PX = 384                                               # la maestra: se reduce a 192 (juego y aviso) y 96 (tarjeta de misión)

def detalle_y(tipo):
    """El centro del detalle dentro de cada forma (PilotSignalSprites.DetailY)."""
    return -0.30 if tipo == 'gota' else (-0.18 if tipo == 'triangulo' else 0.0)

def senal(tipo, color, detalle):
    def construir(r):
        pieza(contorno_forma(tipo), SEN_H, 0.15, color, 'sen_' + tipo, padre=r, panza=0.012)
        cy = detalle_y(tipo); z = SEN_H
        if detalle == 'punto':
            disco(0.27, 0.07, 0.03, material=tinta(), loc=(0, cy, z - 0.012), nombre='sen_punto', padre=r, verts=64)
        elif detalle == 'anillo':
            aro_plano(0.42, 0.075, material=tinta(), loc=(0, cy, z + 0.03), nombre='sen_anillo', padre=r)
        else:
            banda = recorta_banda(contorno_forma(tipo, EXTRA - 0.11), cy - 0.15, cy + 0.15)
            pieza(banda, 0.07, 0.03, material=tinta(), nombre='sen_franja', z0=z - 0.012, padre=r)
    return construir

def render_senales():
    for tipo, color in zip(FORMAS, COLORES):
        for det in DETALLES:
            una(f'senal_{tipo}_{det}', senal(tipo, color, det), SEN_LIENZO, SEN_LIENZO, SEN_PX / SEN_LIENZO, centro=(0.0, sube(SEN_H) / 2))

# ------------------------------------------------------------------ la nave (PilotShipSprite: lienzo de ±1,1, mirando hacia arriba)
NAV_LIENZO = 2.2
NAV_PX = 384
NAV_H = 0.14

def contorno_casco(n=40):
    """El casco: la intersección de dos círculos de radio 0,92 centrados en (±0,62, −0,06), cortada abajo en y = −0,58 (una hoja con la punta arriba)."""
    def x_de(y): return math.sqrt(max(0.0, 0.92 ** 2 - (y + 0.06) ** 2)) - 0.62
    ys = [-0.58 + (0.62 + 0.58) * i / n for i in range(n + 1)]
    return [(x_de(y), y) for y in ys] + [(-x_de(y), y) for y in reversed(ys[:-1])]

def ccw(pts):
    a = sum(pts[i][0] * pts[(i + 1) % len(pts)][1] - pts[(i + 1) % len(pts)][0] * pts[i][1] for i in range(len(pts)))
    return pts if a > 0 else list(reversed(pts))

def nave(r):
    # la llama (debajo de todo): sol con el centro coral
    pieza(casco_convexo([(0.0, -0.56, 0.17 + 0.055), (0.0, -0.90, 0.04 + 0.055)]), 0.05, 0.02, '#FFC93C', 'nav_llama', padre=r)
    pieza(casco_convexo([(0.0, -0.58, 0.08), (0.0, -0.76, 0.02)]), 0.04, 0.015, '#FF6B4A', 'nav_llama_centro', z0=0.04, padre=r)
    # las aletas (coral)
    for sx in (1, -1):
        pieza(redondeado(ccw([(sx * 0.20, -0.10), (sx * 0.52, -0.46), (sx * 0.52, -0.68), (sx * 0.20, -0.52)]), 0.075), 0.07, 0.03, '#FF6B4A', 'nav_aleta', padre=r)
    casco = engorda(contorno_casco(), 0.07)
    pieza(casco, NAV_H, 0.06, '#FFF8EC', 'nav_casco', padre=r, panza=0.004)
    # la punta (coral): el casco cortado por arriba de y = 0,26
    punta = recorta_banda(engorda(contorno_casco(), 0.055), 0.26, 2.0)
    pieza(punta, 0.04, 0.02, '#FF6B4A', 'nav_punta', z0=NAV_H - 0.012, padre=r)
    # la ventanilla: aro de tinta, cristal celeste y un brillo
    aro_plano(0.165, 0.03, material=tinta(), loc=(0, 0.02, NAV_H + 0.03), nombre='nav_aro', padre=r)
    disco(0.15, 0.05, 0.02, '#7FD8FF', loc=(0, 0.02, NAV_H - 0.005), nombre='nav_cristal', padre=r)
    esfera(0.035, '#FFFFFF', loc=(-0.05, 0.07, NAV_H + 0.075), escala=(1.2, 0.8, 0.6), nombre='nav_brillo', padre=r)

def render_nave():
    una('nave', nave, NAV_LIENZO, NAV_LIENZO, NAV_PX / NAV_LIENZO, centro=(0.0, sube(NAV_H) / 2))

preparar()
if SOLO in ('todo', 'senales'): render_senales()
if SOLO in ('todo', 'nave'): render_nave()
termina()
