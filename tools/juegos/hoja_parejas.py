# -*- coding: utf-8 -*-
"""Arma `docs/previews/plasticina/parejas-objetos.png`: los 12 objetos y los 4 gemelos de Constelaciones EN su luz abierta (como los ve el juego: el objeto a R × 1,32 / 40 por unidad sobre el disco claro), la luz dormida y la dormida presionada,
con los sprites REALES del atlas (`Assets/Resources/Plasticina/parejas`), sobre el cielo del juego.
Uso: python -I tools/juegos/hoja_parejas.py [salida.png]"""
import json, os, sys
from PIL import Image, ImageDraw
sys.stdout.reconfigure(encoding='utf-8')

AQUI = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.normpath(os.path.join(AQUI, '..', '..'))
BASE = os.path.join(REPO, 'unity', 'NeuroVidaCore', 'Assets', 'Resources', 'Plasticina', 'parejas')
SALIDA = sys.argv[1] if len(sys.argv) > 1 else os.path.join(REPO, 'docs', 'previews', 'plasticina', 'parejas-objetos.png')
NOMBRES = ('planeta', 'cohete', 'cometa', 'ovni', 'cristal', 'casco', 'luna', 'satélite', 'telescopio', 'asteroide', 'antena', 'brújula')
GEMELOS = (0, 1, 3, 5)
man = json.load(open(os.path.join(BASE, 'manifest.json'), encoding='utf-8'))
atlas = Image.open(os.path.join(BASE, 'atlas.png')).convert('RGBA')

def sprite(n):
    e = next(s for s in man['sprites'] if s['n'] == n)
    return atlas.crop((e['x'], atlas.height - e['y'] - e['h'], e['x'] + e['w'], atlas.height - e['y']))

def luz_con(obj):
    disco = sprite('abierta').copy(); o = sprite(obj)
    w = round(disco.width * 56.0 * (38.0 * 1.32 / 40.0) / 84.0)                    # el objeto mide 56 × R × 1,32 / 40 unidades (R = 38) y la caja de la luz, 84
    o = o.resize((w, w), Image.LANCZOS); disco.alpha_composite(o, ((disco.width - w) // 2, (disco.height - w) // 2)); return disco

celdas = []
for k in range(12):
    celdas.append((NOMBRES[k], luz_con(f'obj_{k}_0')))
    if k in GEMELOS: celdas.append((NOMBRES[k] + ' (gemelo)', luz_con(f'obj_{k}_1')))
celdas += [('dormida', sprite('dormante')), ('dormida presionada', sprite('dormante_p'))]
cols = 6; cw = 252 + 16; ch = 252 + 34; rows = (len(celdas) + cols - 1) // cols
hoja = Image.new('RGBA', (cw * cols, ch * rows), (0x0A, 0x0F, 0x33, 255)); d = ImageDraw.Draw(hoja)
for i, (n, im) in enumerate(celdas):
    x = (i % cols) * cw + 8; y = (i // cols) * ch + 4
    hoja.alpha_composite(im, (x, y)); d.text((x + 6, y + im.height + 6), n, fill=(220, 220, 240, 255))
hoja.convert('RGB').save(SALIDA, optimize=True); print('hoja', SALIDA, hoja.size)
