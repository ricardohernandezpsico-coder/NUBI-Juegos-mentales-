# -*- coding: utf-8 -*-
"""En la punta de la lengua en plasticina (Tarea 101): la ficha-letra (un disco de plasticina gris claro que el código tiñe: crema, fija por ayuda, y los cuatro metales de las encendidas), la casilla (el hueco donde va cada letra) y la perla del lucero del cielo (una esfera gris clara
que el código tiñe de oro, plata, cobre o azul). La tarjeta de la definición, los botones y el panel del final salen de las losas del kit (`comun`); lo que brilla (resplandores, estela del lucero que vuela, ondas de la antena) y el texto se quedan procedurales.

Uso: blender -b -P tools/juegos/punta.py -- [--solo ficha|casilla|perla] [--samples N] [--out DIR]
Salida (en --out, por defecto tools/juegos/out): `ficha.png`, `casilla.png`, `perla.png`; `empaqueta.py punta` las reduce al tamaño final.

Cada pieza sale en un lienzo cuadrado que deja aire para la sombra de contacto; el código la pone de modo que el CUERPO mida lo de siempre (la ficha, `_m.TileD`; la casilla, d; la perla, 14 dp): `FichaLienzo` / `CasillaLienzo` / `PerlaLienzo` en `PuntaSprites` dicen cuántas veces el cuerpo mide el lado de la imagen.
La ficha y la perla van en gris claro para teñir con `Image.color` (multiplica): la luz de plasticina (cúpula) queda; la tinta de la letra va encima, como `Text`.
"""
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from plasticina import *

SOLO = arg('--solo', 'todo')
GRIS = '#F2F2F4'

# ------------------------------------------------------------------ la ficha-letra: disco de radio 1 en un lienzo de ±1,25 (el cuerpo mide 0,8 del lado)
FIC_LIENZO = 2.5
FIC_PX = 384                                              # la maestra: se reduce a 192
FIC_H = 0.34

def ficha(r):
    disco(1.0, FIC_H, 0.30, GRIS, nombre='ficha', padre=r, verts=96)

def render_ficha():
    una('ficha', ficha, FIC_LIENZO, FIC_LIENZO, FIC_PX / FIC_LIENZO, centro=(0.0, sube(FIC_H) / 2))

# ------------------------------------------------------------------ la casilla: un cuenco oscuro con un aro celeste tenue; lienzo de ±1,2 (el cuerpo mide 0,83 del lado)
CAS_LIENZO = 2.4
CAS_PX = 256                                              # la maestra: se reduce a 128
CAS_H = 0.16

def casilla(r):
    disco(1.0, CAS_H, 0.07, nombre='cas_cuenco', padre=r, verts=96, material=mat_oscuro('m_j_casilla', '#14173F'))
    aro_plano(0.93, 0.045, loc=(0, 0, CAS_H), nombre='cas_aro', padre=r, material=mat_kit('m_j_casilla_aro', '#3A7FA0'))

def render_casilla():
    una('casilla', casilla, CAS_LIENZO, CAS_LIENZO, CAS_PX / CAS_LIENZO, centro=(0.0, sube(CAS_H) / 2))

# ------------------------------------------------------------------ la perla del lucero del cielo: una esfera; lienzo de ±1 (el cuerpo mide 0,94 del lado), sin sombra de contacto (flota)
PER_LIENZO = 2.0
PER_PX = 256                                              # la maestra: se reduce a 64
PER_R = 0.94

def perla(r):
    bpy.data.objects['juego_suelo'].hide_render = True
    esfera(PER_R, GRIS, loc=(0, 0, PER_R), nombre='perla', seg=64)

def render_perla():
    una('perla', perla, PER_LIENZO, PER_LIENZO, PER_PX / PER_LIENZO, centro=(0.0, PER_R * math.sin(math.radians(TILT))))

preparar()
if SOLO in ('todo', 'ficha'): render_ficha()
if SOLO in ('todo', 'casilla'): render_casilla()
if SOLO in ('todo', 'perla'): render_perla()
termina()
