# -*- coding: utf-8 -*-
"""Arma `docs/previews/plasticina/bodega-piezas.png`: los 12 objetos DENTRO de su escotilla abierta (marco, interior con algo, el objeto a 40 dp y el aro), la escotilla cerrada, la escotilla presionada (la hoja con la huella de un dedo, aplastada
+4 % de ancho y −8 % de alto, un punto más oscura), el robot armado (cuerpo, visor, ojos y antena), la caja, la esclusa y los tres círculos del carro, con los sprites REALES del atlas (`Assets/Resources/Plasticina/bodega`), sobre el cielo del juego.
Uso: python -I tools/juegos/hoja_bodega.py [salida.png]"""
import json, math, os, sys
import numpy as np
from PIL import Image, ImageDraw
sys.stdout.reconfigure(encoding='utf-8')

AQUI = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.normpath(os.path.join(AQUI, '..', '..'))
BASE = os.path.join(REPO, 'unity', 'NeuroVidaCore', 'Assets', 'Resources', 'Plasticina', 'bodega')
SALIDA = sys.argv[1] if len(sys.argv) > 1 else os.path.join(REPO, 'docs', 'previews', 'plasticina', 'bodega-piezas.png')
NOMBRES = ('llave', 'campana', 'farol', 'manzana', 'hongo', 'taza', 'paraguas', 'libro', 'gema', 'reloj de arena', 'pluma', 'bellota')
man = json.load(open(os.path.join(BASE, 'manifest.json'), encoding='utf-8'))
atlas = Image.open(os.path.join(BASE, 'atlas.png')).convert('RGBA')
CIELO = (0x0A, 0x0F, 0x33, 255)
PX = 4                                                       # píxeles por dp de los sprites

def sprite(n):
    e = next(s for s in man['sprites'] if s['n'] == n)
    return atlas.crop((e['x'], atlas.height - e['y'] - e['h'], e['x'] + e['w'], atlas.height - e['y']))

def pega(base, im, cx, cy, ancho_dp=None, alto_dp=None, giro=0.0):
    """Pega [im] centrado en (cx, cy) píxeles; si se da [ancho_dp], se escala a ese ancho en dp (a 4 px por dp), y [alto_dp] el alto."""
    if ancho_dp is not None or alto_dp is not None:
        w = round((ancho_dp if ancho_dp is not None else im.width / PX) * PX); h = round((alto_dp if alto_dp is not None else im.height / PX) * PX)
        im = im.resize((w, h), Image.LANCZOS)
    if giro: im = im.rotate(giro, resample=Image.BICUBIC, expand=False)
    base.alpha_composite(im, (int(cx - im.width / 2), int(cy - im.height / 2)))

def oscuro(im, k):
    a = np.asarray(im).astype(np.float32); a[..., :3] *= k
    return Image.fromarray(a.astype(np.uint8), 'RGBA')

def escotilla(abierta=None, presionada=False, giro=0.0):
    """Una escotilla como la arma el juego (marco, interior, [objeto | hojas], aro) en un lienzo de 76 × 76 dp."""
    W = 76 * PX; lienzo = Image.new('RGBA', (W, W), (0, 0, 0, 0)); c = W / 2
    k = 0.93 if presionada else 1.0
    pega(lienzo, oscuro(sprite('marco'), k), c, c)
    pega(lienzo, oscuro(sprite('interior_1' if abierta is not None else 'interior_2'), k), c, c)
    if abierta is not None: pega(lienzo, sprite(f'obj_{abierta}'), c, c, 40, 40)
    else:
        hoja = sprite('puerta_p' if presionada else 'puerta'); otra = oscuro(sprite('puerta'), k).rotate(180, resample=Image.BICUBIC)
        a = Image.new('RGBA', (W, W), (0, 0, 0, 0)); b = Image.new('RGBA', (W, W), (0, 0, 0, 0))
        pega(a, hoja, c, c); pega(b, otra, c, c); a.alpha_composite(b)
        if giro: a = a.rotate(giro, resample=Image.BICUBIC)
        lienzo.alpha_composite(a)
    pega(lienzo, oscuro(sprite('aro'), k), c, c)
    if presionada:
        w = round(W * 1.04); h = round(W * 0.92); lienzo = lienzo.resize((w, h), Image.LANCZOS)
        base = Image.new('RGBA', (W, W), (0, 0, 0, 0)); base.alpha_composite(lienzo, ((W - w) // 2, (W - h) // 2)); lienzo = base
    return lienzo

def robot():
    W = 76 * PX; im = Image.new('RGBA', (W, W), (0, 0, 0, 0)); c = W / 2
    pega(im, sprite('robot'), c, c + 4 * PX)
    pega(im, sprite('visor'), c, c + 4 * PX, 32, 20)
    for dx in (-5, 5): pega(im, sprite('ojo'), c + dx * PX, c + 4 * PX, 10, 10)
    pega(im, sprite('tallo'), c, c - 20 * PX + 4 * PX, 6, 14)
    pega(im, sprite('bola'), c, c - 31 * PX + 4 * PX, 10, 10)
    return im

celdas = []
for k in range(12): celdas.append((NOMBRES[k], escotilla(abierta=k)))
celdas += [('escotilla cerrada', escotilla()), ('presionada', escotilla(presionada=True)), ('cerrada, de lado', escotilla(giro=-35)), ('presionada, de lado', escotilla(presionada=True, giro=-35)),
           ('robot', robot()), ('caja', None), ('esclusa', None), ('carro: pendiente', None), ('de memoria', None), ('con error', None)]
cols = 6; cw = 76 * PX + 16; ch = 76 * PX + 34; rows = (len(celdas) + cols - 1) // cols
hoja = Image.new('RGBA', (cw * cols, ch * rows), CIELO); d = ImageDraw.Draw(hoja)
for i, (n, im) in enumerate(celdas):
    x = (i % cols) * cw + 8; y = (i // cols) * ch + 4
    if im is None:
        im = Image.new('RGBA', (76 * PX, 76 * PX), (0, 0, 0, 0)); cc = 76 * PX / 2
        if n == 'caja': pega(im, sprite('caja'), cc, cc)
        elif n == 'esclusa': pega(im, sprite('esclusa'), cc, cc)
        else: pega(im, sprite('carro_' + {'carro: pendiente': '0', 'de memoria': '1', 'con error': '2'}[n]), cc, cc)
    hoja.alpha_composite(im, (x, y)); d.text((x + 6, y + im.height + 6), n, fill=(220, 220, 240, 255))
hoja.convert('RGB').save(SALIDA, optimize=True); print('hoja', SALIDA, hoja.size)
