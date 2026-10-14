# -*- coding: utf-8 -*-
"""¿Verdad o disparate? en plasticina (Tarea 97): la antena de la sala de radio, los dos íconos de los botones (✓ y la onda rota) y las barras del medidor de señal. La placa de la transmisión, los botones VERDAD y DISPARATE y el panel del final salen de las
losas del kit (`comun`): no se renderiza nada para ellos.

Uso: blender -b -P tools/juegos/disparate.py -- [--solo antena|iconos|medidor] [--samples N] [--out DIR]
Salida (en --out, por defecto tools/juegos/out): las «maestras» con fondo transparente; `empaqueta.py disparate` las reduce al tamaño final.

Cada pieza sale en el MISMO lienzo y a la MISMA escala que el sprite procedural al que reemplaza (la antena, ±1,2: de ahí salen `DisparateSprites.AntennaTip`, `AntennaDish` y `AntennaFloor`, donde el código pone las ondas y la luz; los íconos, ±1), así el
controlador no cambia ninguna medida. La plasticina ocupa el cuerpo de hoy más su borde de tinta (la del kit no lleva borde).
  - `antena`: plataforma de acero, base uva, patas y mástil crema con remaches de tinta, plato crema con borde celeste, brazo y luz sol en la punta (la luz que pulsa es un resplandor procedural encima).
  - `icono_visto`, `icono_ola`: la marca ✓ y la onda rota, churros de tinta (se ponen sobre las losas lima y coral).
  - `medidor_encendida`, `medidor_apagada`: UNA barra con los extremos redondos que el código estira en vertical (nine-slice: 5 barras de 50 a 154 u de alto y 34 u de ancho). 1 unidad de Blender = 34 u del lienzo de 1080.
"""
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from plasticina import *

SOLO = arg('--solo', 'todo')
CREMA = '#FFF8EC'; CIELO = '#4CC9F0'; SOL = '#FFC93C'; UVA = '#B8A4FF'; ACERO = '#39408F'; APAGADA = '#38428F'
L = 0.05                                                  # el borde de tinta de hoy

# ------------------------------------------------------------------ la antena (DisparateSprites.RenderAntenna: lienzo ±1,2)
ANT_LIENZO = 2.4
ANT_PX = 640                                              # la maestra: se reduce a 420 (el tamaño en el juego)
ANT_H = 0.16

def antena(r):
    # plataforma y base
    pieza(redondeado(rect(0, -0.86, 0.84, 0.05), 0.08 + L), 0.10, 0.05, nombre='ant_plataforma', padre=r, material=mat_oscuro('m_j_ant_acero', ACERO))
    pieza(redondeado(rect(0, -0.62, 0.33, 0.08), 0.07 + L), 0.14, 0.06, UVA, 'ant_base', z0=0.05, padre=r)
    for sx in (-1, 1): disco(0.035, 0.03, 0.01, material=tinta(), loc=(sx * 0.28, -0.62, 0.19 - 0.012), nombre='ant_remache_base', padre=r)
    # patas y mástil crema
    for sx in (-1, 1): churro_c((sx * 0.30, -0.52), (sx * 0.05, -0.02), 0.07, 0.07, mat_kit('m_j_ant_pata', CREMA), 'ant_pata')
    pieza(redondeado(rect(0, 0.12, 0.035, 0.46), 0.04 + L), 0.14, 0.06, CREMA, 'ant_mastil', padre=r)
    for i in range(5): disco(0.03, 0.03, 0.01, material=tinta(), loc=(0, -0.3 + 0.18 * i, 0.14 - 0.012), nombre='ant_remache', padre=r)
    # el plato: un cuenco (la mitad de abajo de un círculo de 0,52), su borde elíptico crema y el interior celeste
    n = 40
    cuenco = [(0.52 * math.cos(math.pi + math.pi * i / n), 0.74 + 0.52 * math.sin(math.pi + math.pi * i / n)) for i in range(n + 1)]
    pieza(redondeado(cuenco, L), 0.16, 0.06, CREMA, 'ant_cuenco', z0=0.02, padre=r)
    pieza(elipse(0, 0.74, 0.52 + L * 0.6, 0.10 + L * 0.6), 0.05, 0.02, CREMA, 'ant_borde', z0=0.16, padre=r)
    pieza(elipse(0, 0.745, 0.45, 0.065), 0.03, 0.01, CIELO, 'ant_interior', z0=0.19, padre=r)
    # el brazo y la luz de la punta
    churro_c((0, 0.74), (0, 1.0), 0.05, 0.05, mat_kit('m_j_ant_brazo', CREMA), 'ant_brazo')
    disco(0.105, 0.10, 0.04, SOL, loc=(0, 1.02, 0.0), nombre='ant_luz', padre=r, verts=48)
    esfera(0.03, '#FFFFFF', loc=(-0.035, 1.05, 0.11), escala=(1.2, 0.8, 0.6), nombre='ant_luz_brillo', padre=r)

def render_antena():
    una('antena', antena, ANT_LIENZO, ANT_LIENZO, ANT_PX / ANT_LIENZO, centro=(0.0, sube(0.10) / 2))

# ------------------------------------------------------------------ los íconos de los botones (DisparateSprites.CheckIcon y BrokenWaveIcon: lienzo ±1)
ICO_LIENZO = 2.0
ICO_PX = 256                                              # la maestra: se reduce a 128 (el ícono mide 120 u en el juego)
ICO_R = 0.155

def icono_visto(r):
    for (a, b) in (((-0.55, 0.0), (-0.18, -0.4)), ((-0.18, -0.4), (0.6, 0.46))): churro_c(a, b, ICO_R, ICO_R, tinta(), 'ico_visto')

def icono_ola(r):
    pts_l = [(-0.85, 0.0), (-0.66, 0.38), (-0.46, -0.38), (-0.24, 0.2)]
    pts_r = [(0.2, -0.2), (0.4, 0.38), (0.6, -0.38), (0.85, 0.05)]
    for pts in (pts_l, pts_r):
        for a, b in zip(pts, pts[1:]): churro_c(a, b, 0.11, 0.11, tinta(), 'ico_ola')

def render_iconos():
    una('icono_visto', icono_visto, ICO_LIENZO, ICO_LIENZO, ICO_PX / ICO_LIENZO, centro=(0.0, sube(2 * ICO_R) / 2))
    una('icono_ola', icono_ola, ICO_LIENZO, ICO_LIENZO, ICO_PX / ICO_LIENZO, centro=(0.0, sube(0.22) / 2))

# ------------------------------------------------------------------ las barras del medidor (1 unidad = 34 u: ancho 1, largo 3,5; el lienzo deja 0,4 de sombra a cada lado)
MED_W, MED_H, MED_M = 1.0, 3.5, 0.4
MED_PX_U = 136.0                                           # la maestra: 136 px por unidad (= 4 px por u del lienzo de 1080); se reduce a 2 px por u
MED_RELIEVE = 0.30

def barra(color, oscura):
    def construir(r):
        cont = casco_convexo([(0.0, -(MED_H / 2 - MED_W / 2), MED_W / 2), (0.0, MED_H / 2 - MED_W / 2, MED_W / 2)])
        if oscura: pieza(cont, MED_RELIEVE, 0.15, nombre='medidor', padre=r, material=mat_oscuro('m_j_medidor_apagada', color))
        else: pieza(cont, MED_RELIEVE, 0.15, color, 'medidor', padre=r)
    return construir

def render_medidor():
    for nombre, color, oscura in (('medidor_encendida', SOL, False), ('medidor_apagada', APAGADA, True)):
        una(nombre, barra(color, oscura), MED_W + 2 * MED_M, MED_H + 2 * MED_M, MED_PX_U, centro=(0.0, sube(MED_RELIEVE) / 2))

preparar()
if SOLO in ('todo', 'antena'): render_antena()
if SOLO in ('todo', 'iconos'): render_iconos()
if SOLO in ('todo', 'medidor'): render_medidor()
termina()
