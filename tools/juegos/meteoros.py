# -*- coding: utf-8 -*-
"""Lluvia de meteoros en plasticina (Tarea 105): la roca (nine-slice), su estado PRESIONADO (regla del 14-oct) y las dos estrellas de la constelación. La placa de la palabra y el panel del final salen de las losas del kit (`comun`).
La brasa del borde que va por delante y las grietas de la roca de polvo son capas aparte (`meteoros_capas.py`, con numpy): son luz y marca, no plasticina.

Uso: blender -b -P tools/juegos/meteoros.py -- [--solo rocas|presionadas|estrellas] [--samples N] [--out DIR]
Salida (en --out, por defecto tools/juegos/out): `roca_0..3.png`, `roca_p_0..3.png`, `estrella_encendida.png`, `estrella_apagada.png`, las «maestras» con fondo transparente; `empaqueta.py meteoros` las reduce.

LA ROCA: las 4 siluetas de `MeteorSprites.RockSdf` (una elipse de 0,9 × 0,62 con ondulaciones; cuerpo de ~2,0 × 1,3 unidades), de plasticina GRIS CLARA (el código la tiñe de lila, celeste, coral, sol o polvo con `Image.color`), con sus cráteres hundidos y unos bultos suaves.
Se dibuja como nine-slice (corners con los cráteres, fijos; el centro, liso, se estira: así la roca se alarga con la palabra sin deformarse). El lienzo (2,5 × 1,9 unidades) deja margen para la sombra y el resplandor de la brasa.
LA ROCA PRESIONADA (al tocarla): aplastada (~4 % más ancha), un punto más oscura y con la huella de un dedo en el espacio libre de una ESQUINA (el centro lo tapa la placa y se estira: una huella ahí se deformaría).
LAS ESTRELLAS de la constelación: esferas redondas (nada de puntas): sol la encendida y azul noche la apagada.
"""
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from plasticina import *

SOLO = arg('--solo', 'todo')
GRIS = '#E4E4EA'
FASES = ((0.3, 1.9, 4.1, 2.2), (2.2, 0.4, 1.3, 5.1), (4.6, 3.1, 0.7, 3.3), (1.1, 5.2, 2.6, 0.9))               # MeteorSprites.Phase
CRATERES = (                                                                                                   # MeteorSprites.Craters: (x, y, radio) de cada forma
    (-0.62, 0.34, 0.17, 0.60, -0.36, 0.14, 0.06, 0.46, 0.10, 0.80, 0.10, 0.08, -0.74, -0.22, 0.08),
    (-0.74, -0.20, 0.14, -0.30, 0.45, 0.12, 0.66, 0.30, 0.16, 0.34, -0.46, 0.10, 0.0, 0.5, 0.07),
    (-0.52, -0.40, 0.15, 0.14, 0.47, 0.11, 0.74, -0.10, 0.13, -0.78, 0.22, 0.09, 0.45, -0.45, 0.07),
    (0.52, 0.40, 0.14, -0.66, 0.26, 0.16, -0.12, -0.48, 0.11, 0.78, -0.26, 0.10, -0.4, 0.5, 0.07),
)
ROC_W, ROC_H = 2.5, 1.9                                                                                       # el lienzo (unidades de diseño)
ROC_PX_U = 240                                                                                                # la maestra: 240 px por unidad (se reduce a 120)
ROC_ALTO = 0.12
ROC_DOMO = 0.38                                                                                               # el centro se levanta como una cúpula (la altura del dibujo de hoy: ~0,55 en el centro)

def contorno_roca(s, n=240):
    """El contorno de la forma [s]: la elipse de 0,9 × 0,62 con las ondulaciones de `RockSdf` (el radio en cada ángulo)."""
    ph = FASES[s]; out = []
    for i in range(n):
        a = 2 * math.pi * i / n
        w = 1 + 0.07 * math.sin(3 * a + ph[0]) + 0.045 * math.sin(5 * a + ph[1]) + 0.03 * math.sin(9 * a + ph[2]) + 0.015 * math.sin(14 * a + ph[3])
        r = w / math.sqrt(math.cos(a) ** 2 / 0.81 + math.sin(a) ** 2 / 0.3844)
        out.append((r * math.cos(a), r * math.sin(a)))
    return out

def crateres(s):
    c = CRATERES[s]
    return [(c[i], c[i + 1], c[i + 2], 0.55 * c[i + 2], 0.0, 0.1, 0.0, 1.0) for i in range(0, len(c), 3)]       # hundidos y redondos, sin crestas

def roca(s, presionada=False):
    def construir(r):
        cont = contorno_roca(s); huellas = crateres(s)
        color = GRIS
        if presionada:
            aplasta(r, 1.04)
            color = oscurece(GRIS, PRES_OSCURA)
            # la huella va en el espacio libre más grande de UNA ESQUINA del nine-slice (el centro lo tapa la placa y se estira), lejos de los cráteres
            obst = [((c[0], c[1]), (c[0], c[1]), c[2] + 0.06) for c in huellas] + [((-1.3, 0.0), (1.3, 0.0), 0.25), ((0.0, -1.0), (0.0, 1.0), 0.37)]            # (la fila y la columna del centro del nine-slice: se estiran)
            cx, cy, d = espacio_libre(cont, obst)
            rd = max(0.1, min(0.22, (d - 0.08) / 1.3))
            print(f'[juegos] huella de la roca {s}: centro ({cx:.2f}, {cy:.2f}), espacio libre {d:.2f}, semieje {rd:.2f}')
            huellas = huellas + [(cx, cy, rd, 0.09, 0.012)]
        pieza_huella(cont, ROC_ALTO, 0.10, color, 'roca_p' if presionada else 'roca', padre=r, panza=0.02, huella=huellas, anillos=56, n=200, bultos=0.010, domo=ROC_DOMO, redondea=True, fino=0.93)
    return construir

SOLO_FORMA = arg('--forma', None)
def render_rocas(presionadas):
    for s in ([int(SOLO_FORMA)] if SOLO_FORMA is not None else range(4)):
        h = (ROC_ALTO + ROC_DOMO) * (PRES_ALTO if presionadas else 1.0)
        una(('roca_p_' if presionadas else 'roca_') + str(s), roca(s, presionadas), ROC_W, ROC_H, ROC_PX_U, centro=(0.0, sube(h) / 2))

# ------------------------------------------------------------------ las estrellas de la constelación (MeteorSprites.RenderStar: lienzo ±1,12, esfera de 0,56 + el borde de 0,06)
EST_LIENZO = 2.24
EST_PX = 192                                              # la maestra: se reduce a 96
EST_R = 0.60

def estrella(encendida):
    def construir(r):
        bpy.data.objects['juego_suelo'].hide_render = True        # flota
        if encendida: esfera(EST_R, '#FFC93C', loc=(0, 0, EST_R), nombre='estrella', seg=64)
        else: esfera(EST_R, material=mat_oscuro('m_j_estrella_apagada', '#292F6E'), loc=(0, 0, EST_R), nombre='estrella_apagada', seg=64)
    return construir

def render_estrellas():
    for nombre, enc in (('estrella_encendida', True), ('estrella_apagada', False)):
        una(nombre, estrella(enc), EST_LIENZO, EST_LIENZO, EST_PX / EST_LIENZO, centro=(0.0, EST_R * math.sin(math.radians(TILT))))

preparar()
if SOLO in ('todo', 'rocas'): render_rocas(False)
if SOLO in ('todo', 'presionadas'): render_rocas(True)
if SOLO in ('todo', 'estrellas'): render_estrellas()
termina()
