# -*- coding: utf-8 -*-
"""Satélites «enciende tu planeta» en plasticina (Tarea 98): el satélite (UNO solo: todos son iguales), el planeta a oscuras, el sobre del mensaje y la nube de polvo.

Uso: blender -b -P tools/juegos/satelites.py -- [--solo satelite|planeta|sobre|nube] [--samples N] [--out DIR]
Salida (en --out, por defecto tools/juegos/out): las «maestras» con fondo transparente; `empaqueta.py satelites` las reduce al tamaño final.

Cada pieza sale en el MISMO lienzo y a la MISMA escala que el sprite procedural de `SatelliteSprites` al que reemplaza (satélite ±1,25; planeta ±1,14; sobre ±1,3; nube ±1): el controlador no cambia ninguna medida. Donde el sprite de hoy lleva borde de tinta, la plasticina ocupa
la misma huella (el cuerpo más el borde), porque la plasticina del kit no lleva borde.
Reglas del juego que el arte respeta (docs/diseno-satelites.md): los satélites son IDÉNTICOS (la señal es el aro dorado y el sobre, nunca el dibujo: un solo sprite, vista plana, sin luz por instancia y sin antena vertical sobre el brazo: sería una cruz); la nube de polvo es
PLANA y OPACA (tapa de verdad a los satélites que pasan detrás); el planeta es oscuro y de bajo contraste (las luces doradas son lo que resalta).
"""
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from plasticina import *

SOLO = arg('--solo', 'todo')
L = 0.06                                                  # el borde de tinta de hoy del satélite

# ------------------------------------------------------------------ el satélite (SatelliteSprites.RenderBody: lienzo ±1,25; disco de 0,55, brazo y dos paneles)
SAT_LIENZO = 2.5
SAT_PX = 384                                              # la maestra: se reduce a 192 (el satélite mide 64 dp = 191 u en el juego)
SAT_H = 0.20

def satelite(r):
    cuerpo = '#E9E6FF'; brazo = '#9C94D6'; panel = '#7FB6FF'; nucleo = '#5A5296'; divisoria = '#587EB3'
    pieza(redondeado(rect(0, 0, 0.675, 0.025), 0.025 + L), 0.08, 0.03, brazo, 'sat_brazo', padre=r)
    for sx in (-1, 1):
        pieza(redondeado(rect(sx * 0.76, 0, 0.13, 0.10), 0.05 + L), 0.12, 0.04, panel, 'sat_panel', padre=r)
        churro_c((sx * 0.76, -0.13), (sx * 0.76, 0.13), 0.016, 0.12, mat_kit('m_j_sat_div', divisoria), 'sat_divisoria')
    disco(0.55 + L - 0.01, SAT_H, 0.09, cuerpo, nombre='sat_disco', padre=r, verts=96)
    disco(0.2, 0.03, 0.012, nucleo, loc=(0, 0, SAT_H - 0.012), nombre='sat_nucleo', padre=r, verts=64)

def render_satelite():
    una('satelite', satelite, SAT_LIENZO, SAT_LIENZO, SAT_PX / SAT_LIENZO, centro=(0.0, sube(SAT_H) / 2))

# ------------------------------------------------------------------ el planeta a oscuras (RenderPlanet: lienzo ±1,14; radio 0,9 + borde de 0,05)
PLA_LIENZO = 2.28
PLA_PX = 768                                              # la maestra: se reduce a 512 (el planeta mide 334 u en el juego y ×1,75 = 585 u en la pantalla final)
PLA_R = 0.93
PLA_COLOR = '#222762'                                     # azul noche: la luz del estudio lo aclara arriba a la izquierda (el procedural iba de #3B3F86 a #151843)

def planeta(r):
    # una esfera NO se mete en la raíz estirada: su silueta es un círculo de radio PLA_R desde cualquier ángulo; el centro (a PLA_R de alto) se ve PLA_R·sen(inclinación) más arriba
    # el planeta flota en el espacio: sin la sombra de contacto sobre el suelo (la de una esfera tan alta se salía del lienzo y quedaba cortada en seco, como una caja oscura)
    bpy.data.objects['juego_suelo'].hide_render = True
    # (sin «sss»: en una esfera la luz que se cuela por el borde la vuelve de vidrio)
    esfera(PLA_R, nombre='planeta', loc=(0, 0, PLA_R), material=clay('m_j_planeta', PLA_COLOR, **{**MAT_KIT, 'spec': 0.0, 'sheen': 0.0, 'sss': 0.0, 'ao': 0.0, 'coat': 0.0}), seg=96)

def render_planeta():
    una('planeta', planeta, PLA_LIENZO, PLA_LIENZO, PLA_PX / PLA_LIENZO, centro=(0.0, PLA_R * math.sin(math.radians(TILT))))

# ------------------------------------------------------------------ el sobre del mensaje (RenderEnvelope: lienzo ±1,3; caja de 0,78 × 0,55 con una solapa en V de tinta)
SOB_LIENZO = 2.6
SOB_PX = 256                                              # la maestra: se reduce a 128 (el sobre mide 110 u)
SOB_H = 0.12

def sobre(r):
    pieza(redondeado(rect(0, 0, 0.68, 0.45), 0.10 + 0.07), SOB_H, 0.05, '#FFF4D6', 'sob_caja', padre=r)
    for sx in (-1, 1): churro_c((sx * 0.68, 0.40), (0, -0.04), 0.045, SOB_H, tinta(), 'sob_solapa')

def render_sobre():
    una('sobre', sobre, SOB_LIENZO, SOB_LIENZO, SOB_PX / SOB_LIENZO, centro=(0.0, sube(SOB_H) / 2))

# ------------------------------------------------------------------ la nube de polvo (RenderCloud: lienzo ±1; la unión de cinco círculos; plana y OPACA)
NUB_LIENZO = 2.0
NUB_PX = 672                                              # la maestra: se reduce a 448 (la nube mide 600 u en el juego)
NUB_COLOR = '#4A408A'

def nube(r):
    bx = (-0.34, 0.0, 0.34, -0.16, 0.18); by = (-0.06, 0.16, -0.04, -0.22, -0.2); br = (0.44, 0.52, 0.44, 0.36, 0.38)
    mat = mat_oscuro('m_j_nube', NUB_COLOR)
    for i in range(5):                                    # cinco domos achatados que se funden: la silueta es la unión de los cinco círculos y el relieve es bajo (plana)
        esfera(br[i], loc=(bx[i], by[i], 0.0), escala=(1, 1, 0.30), nombre='nube_bulto', padre=r, material=mat, seg=64)

def render_nube():
    una('nube', nube, NUB_LIENZO, NUB_LIENZO, NUB_PX / NUB_LIENZO, centro=(0.0, sube(0.16) / 2))

preparar()
if SOLO in ('todo', 'satelite'): render_satelite()
if SOLO in ('todo', 'planeta'): render_planeta()
if SOLO in ('todo', 'sobre'): render_sobre()
if SOLO in ('todo', 'nube'): render_nube()
termina()
