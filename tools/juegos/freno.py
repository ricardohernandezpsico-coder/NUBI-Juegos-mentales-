# -*- coding: utf-8 -*-
"""Freno de Emergencia en plasticina (Tarea 96): el botón de lanzamiento (apagado y encendido), los cohetes de los carriles, la plataforma y la señal de ALTO.

Uso: blender -b -P tools/juegos/freno.py -- [--solo botones|cohetes|plataforma|alto] [--samples N] [--out DIR]
Salida (en --out, por defecto tools/juegos/out): las «maestras» con fondo transparente; `empaqueta.py freno` las reduce al tamaño final.

Cada pieza sale en el MISMO lienzo y a la MISMA escala que el sprite procedural de `BrakeSprites` al que reemplaza (botón y alto: ±1,1; plataforma: ±1,05): el controlador no cambia ninguna medida. Donde el sprite de hoy tiene borde de tinta, la plasticina
ocupa la misma huella (el cuerpo más el borde), porque la plasticina del kit no lleva borde.
El cohete es el de Piloto (`cohete.py`) con el acento del carril (coral, cielo o lima): el casco crema domina, así el «lima = ir» y el «coral = alto» no se confunden con el cohete. Los cohetes de Freno NO son los de Constelaciones (`SymbolSprite.Rocket`): ese se
queda hasta que le toque a ese juego. La baliza (el disco de luz sobre cada cohete) se queda procedural: es luz, no un objeto (igual que las balizas de Piloto).
"""
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from plasticina import *
from formas import circulo, redondeado
from cohete import cohete, COHETE_H, ccw

SOLO = arg('--solo', 'todo')
CREMA = '#FFF8EC'; LIMA = '#9BE564'; CORAL = '#FF6B4A'; SOL = '#FFC93C'; CIELO = '#4CC9F0'

# ------------------------------------------------------------------ el botón de lanzamiento (BrakeSprites.RenderButton: disco de radio 0,8 + borde de 0,08, con una flecha hacia arriba)
BOT_LIENZO = 2.2
BOT_PX = 384
BOT_H = 0.34
BOT_APAGADO = '#222B78'          # el azul de hoy (#2A3590) un poco más oscuro: sin el borde de tinta, el botón se separa mejor del suelo de la luna (#595890) y la flecha crema resalta más

def boton(encendido):
    def construir(r):
        # apagado: azul oscuro con la flecha crema; encendido: lima con la flecha de tinta (la diferencia de luminosidad es lo que lo hace legible sin color)
        if encendido: pieza(circulo(0.88, 120), BOT_H, 0.17, LIMA, 'bot', padre=r, panza=0.01)
        else: pieza(circulo(0.88, 120), BOT_H, 0.17, nombre='bot', padre=r, panza=0.01, material=mat_oscuro('m_j_boton_apagado', BOT_APAGADO))
        flecha = tinta() if encendido else mat_kit('m_j_flecha_crema', CREMA)
        for (a, b) in (((0.0, -0.36), (0.0, 0.30)), ((-0.27, 0.04), (0.0, 0.34)), ((0.27, 0.04), (0.0, 0.34))):
            churro_c(a, b, 0.09, BOT_H, flecha, 'bot_flecha')
    return construir

def render_botones():
    for nombre, enc in (('boton_apagado', False), ('boton_encendido', True)):
        una(nombre, boton(enc), BOT_LIENZO, BOT_LIENZO, BOT_PX / BOT_LIENZO, centro=(0.0, sube(BOT_H) / 2))

# ------------------------------------------------------------------ los cohetes de los carriles (el de Piloto con el acento del carril)
COH_LIENZO = 1.9
COH_PX = 384
ACENTOS = (('coral', CORAL), ('cielo', CIELO), ('lima', LIMA))

def render_cohetes():
    for nombre, acento in ACENTOS:
        una('cohete_' + nombre, lambda r, a=acento: cohete(r, a), COH_LIENZO, COH_LIENZO, COH_PX / COH_LIENZO, centro=(0.0, -0.15 + sube(COHETE_H) / 2))

# ------------------------------------------------------------------ la plataforma (BrakeSprites.RenderLaunchPad: losa lila con franjas de precaución y dos torres con travesaños)
PAD_LIENZO = 2.1
PAD_PX = 512
LILA_PAD = '#9C94D6'; LILA_TORRE = '#847EB6'           # la torre de hoy: la losa × 0,85
SLAB_H = 0.14; TORRE_H = 0.30

def recorta_x(pts, x0, x1):
    """El polígono convexo [pts] recortado a la franja x0 ≤ x ≤ x1 (Sutherland-Hodgman)."""
    def corta(p, dentro, inter):
        out = []
        for i in range(len(p)):
            a, b = p[i - 1], p[i]
            if dentro(b):
                if not dentro(a): out.append(inter(a, b))
                out.append(b)
            elif dentro(a): out.append(inter(a, b))
        return out
    def en_x(xc): return lambda a, b: (xc, a[1] + (b[1] - a[1]) * (xc - a[0]) / (b[0] - a[0]))
    p = corta(pts, lambda q: q[0] >= x0, en_x(x0))
    return corta(p, lambda q: q[0] <= x1, en_x(x1)) if p else []

def plataforma(r):
    # la losa: la RoundBox de hoy (centro (0, −0,62), medios 0,9 × 0,16, esquina 0,08) más su borde de 0,05
    pieza(redondeado(rect(0.0, -0.62, 0.82, 0.08), 0.13), SLAB_H, 0.06, LILA_PAD, 'pad_losa', padre=r)
    # las franjas de precaución: la banda de y ∈ [−0,72, −0,64] con rayas de 45° ((x − y)·4,5: sol de 0 a 0,5, tinta de 0,5 a 1)
    y0, y1, X = -0.72, -0.64, 0.84
    pieza(rect(0.0, (y0 + y1) / 2, X, (y1 - y0) / 2), 0.02, 0.008, nombre='pad_franja_base', z0=SLAB_H - 0.012, padre=r, material=tinta())
    k = -9
    while k < 9:
        c0, c1 = k / 4.5, (k + 0.5) / 4.5                 # x − y ∈ [c0, c1)
        pts = recorta_x([(c0 + y0, y0), (c1 + y0, y0), (c1 + y1, y1), (c0 + y1, y1)], -X, X)
        if len(pts) >= 3: pieza(ccw(pts), 0.03, 0.01, SOL, 'pad_franja_sol', z0=SLAB_H - 0.012, padre=r)
        k += 1
    # las torres: más oscuras (la losa × 0,85), de 0,14 × 0,6 con la esquina de 0,04 (más 0,02 de borde), y tres travesaños de tinta en cada una
    for sx in (-1, 1):
        x = sx * 0.62
        pieza(redondeado(rect(x, -0.2, 0.05, 0.26), 0.04), TORRE_H, 0.04, LILA_TORRE, 'pad_torre', padre=r)
        for i in range(3):
            ty = -0.36 + i * 0.14
            churro_c((x + sx * 0.04, ty), (x - sx * 0.04, ty + 0.09), 0.02, TORRE_H, tinta(), 'pad_travesano')

def render_plataforma():
    una('plataforma', plataforma, PAD_LIENZO, PAD_LIENZO, PAD_PX / PAD_LIENZO, centro=(0.0, sube(SLAB_H) / 2))

# ------------------------------------------------------------------ la señal de ALTO (BrakeSprites.RenderStopSign: octágono coral de circunradio 0,82 con un aro crema; el texto «ALTO» lo pone Unity)
ALTO_LIENZO = 2.2
ALTO_PX = 512
ALTO_H = 0.30

def octagono(rc): return [(rc * math.cos(math.pi / 8 + i * math.pi / 4), rc * math.sin(math.pi / 8 + i * math.pi / 4)) for i in range(8)]

def alto(r):
    # el cuerpo: el octágono de hoy más su borde de 0,07 (esquinas redondas, como la SDF desplazada)
    pieza(redondeado(octagono(0.82), 0.07), ALTO_H, 0.13, CORAL, 'alto_cuerpo', padre=r, panza=0.01)
    # el aro crema (un churro de 0,07 de ancho sobre el octágono de circunradio 0,68): incrustado, queda la cara coral lisa para el texto
    crema = mat_kit('m_j_alto_aro', CREMA)
    pts = octagono(0.68)
    for i in range(8): churro_c(pts[i], pts[(i + 1) % 8], 0.035, ALTO_H, crema, 'alto_aro')

def render_alto():
    una('alto', alto, ALTO_LIENZO, ALTO_LIENZO, ALTO_PX / ALTO_LIENZO, centro=(0.0, sube(ALTO_H) / 2))

preparar()
if SOLO in ('todo', 'botones'): render_botones()
if SOLO in ('todo', 'cohetes'): render_cohetes()
if SOLO in ('todo', 'plataforma'): render_plataforma()
if SOLO in ('todo', 'alto'): render_alto()
termina()
