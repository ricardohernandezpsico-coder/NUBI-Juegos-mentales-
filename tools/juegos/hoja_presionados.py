# -*- coding: utf-8 -*-
"""Arma la hoja `docs/previews/plasticina/presionados.png` (Tarea 104): para cada juego, un cuadro NORMAL y uno PRESIONADO de lo que se toca, con los sprites reales de los atlas
(la herramienta de capturas no puede simular un dedo apoyado). Losas del kit (Rescate, Carga exacta, Disparate, Intrusa, Punta), las señales de Piloto, el botón de Freno, el satélite y la ficha-letra de Punta, y los luceros de Rastro.
Uso: python -I tools/juegos/hoja_presionados.py
"""
import json, os, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFont
sys.stdout.reconfigure(encoding='utf-8')

AQUI = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.normpath(os.path.join(AQUI, '..', '..'))
BASE = os.path.join(REPO, 'unity', 'NeuroVidaCore', 'Assets', 'Resources', 'Plasticina')
SALIDA = os.path.join(REPO, 'docs', 'previews', 'plasticina', 'presionados.png')
CIELO = (10, 15, 51, 255); LUNA = (89, 88, 144, 255)
_c = {}

def sprite(grupo, nombre):
    if grupo not in _c: _c[grupo] = (json.load(open(os.path.join(BASE, grupo, 'manifest.json'), encoding='utf-8')), Image.open(os.path.join(BASE, grupo, 'atlas.png')).convert('RGBA'))
    m, a = _c[grupo]
    e = next(s for s in m['sprites'] if s['n'] == nombre)
    return a.crop((e['x'], a.height - e['y'] - e['h'], e['x'] + e['w'], a.height - e['y']))

def losa(nombre, ancho):
    """Una losa del kit estirada (nine-slice a mano) a [ancho] px de cuerpo: se recortan los bordes de la imagen y se estira el centro."""
    im = sprite('comun', nombre); m, _ = _c['comun']; e = next(s for s in m['sprites'] if s['n'] == nombre)
    izq, abajo, der, arr = [int(v) for v in e['borde']]
    w, h = im.size; nw = ancho + izq + der
    cuerpo = im.crop((izq, arr, w - der, h - abajo)).resize((max(1, nw - izq - der), h - arr - abajo), Image.LANCZOS)
    out = Image.new('RGBA', (nw, h), (0, 0, 0, 0))
    out.alpha_composite(im.crop((0, 0, izq, h)), (0, 0)); out.alpha_composite(im.crop((w - der, 0, w, h)), (nw - der, 0))
    sup = im.crop((izq, 0, w - der, arr)).resize((nw - izq - der, arr), Image.LANCZOS); inf = im.crop((izq, h - abajo, w - der, h)).resize((nw - izq - der, abajo), Image.LANCZOS)
    out.alpha_composite(sup, (izq, 0)); out.alpha_composite(cuerpo, (izq, arr)); out.alpha_composite(inf, (izq, h - abajo))
    return out

try:
    F = ImageFont.truetype('arialbd.ttf', 17); F2 = ImageFont.truetype('arial.ttf', 14); F3 = ImageFont.truetype('arialbd.ttf', 14)
except Exception:
    F = F2 = F3 = ImageFont.load_default()

W = 1320
filas = []        # (título, [(rótulo, imagen)], fondo)
def losas(titulo, pares, ancho=150, fondo=CIELO, texto=True):
    items = []
    for etiqueta, base in pares:
        for suf, rot in (('', 'normal'), ('_p', 'presionada')):
            im = losa(base + suf, ancho); items.append((etiqueta + '\n' + rot, im.resize((int(im.width * 0.6), int(im.height * 0.6)), Image.LANCZOS)))
    filas.append((titulo, items, fondo))

losas('Rescate relámpago: casilla de respuesta y «¡Rescatar!»', [('casilla', 'losa_md_noche'), ('¡Rescatar!', 'losa_md_sol')])
losas('Carga exacta: celda, opción y herramienta', [('celda', 'losa_md_crema'), ('opción', 'losa_md_noche'), ('herramienta', 'losa_md_apagada')], 110)
losas('¿Verdad o disparate?: VERDAD y DISPARATE', [('VERDAD', 'losa_md_lima'), ('DISPARATE', 'losa_md_coral')])
losas('La estrella intrusa: opción y «Seguir»', [('opción', 'losa_md_crema'), ('acierta', 'losa_md_lima'), ('falla', 'losa_md_coral')], 120)
losas('En la punta de la lengua: botones', [('principal', 'losa_md_cielo'), ('secundario', 'losa_md_noche')])

def fila_pieza(titulo, pares, fondo, escala=1.0):
    items = []
    for etiqueta, a, b in pares:
        items.append((etiqueta + '\nnormal', a)); items.append((etiqueta + '\npresionada', b))
    filas.append((titulo, items, fondo))

sen = []
for f in ('hexagono', 'gota', 'circulo', 'cuadrado', 'triangulo'):
    sen.append((f, sprite('piloto', f'senal_{f}_anillo').resize((120, 120), Image.LANCZOS), sprite('piloto', f'senal_p_{f}_anillo').resize((120, 120), Image.LANCZOS)))
fila_pieza('Piloto Estelar: las 5 señales (detalle «anillo»); la huella va donde no pisa el detalle', sen, CIELO)
fila_pieza('Freno de Emergencia: el botón del carril, encendido y apagado', [('encendido', sprite('freno', 'boton_encendido').resize((150, 150), Image.LANCZOS), sprite('freno', 'boton_encendido_p').resize((150, 150), Image.LANCZOS)),
                                                                             ('apagado', sprite('freno', 'boton_apagado').resize((150, 150), Image.LANCZOS), sprite('freno', 'boton_apagado_p').resize((150, 150), Image.LANCZOS))], LUNA)
fila_pieza('Satélites: el satélite al marcarlo', [('satélite', sprite('satelites', 'satelite').resize((210, 210), Image.LANCZOS), sprite('satelites', 'satelite_p').resize((210, 210), Image.LANCZOS))], CIELO)

def tinta(im, rgb):
    a = np.asarray(im).astype(float); a[..., 0] *= rgb[0] / 255; a[..., 1] *= rgb[1] / 255; a[..., 2] *= rgb[2] / 255
    return Image.fromarray(a.astype('uint8'), 'RGBA')
fic = []
for nombre, rgb in (('crema', (244, 241, 255)), ('oro', (255, 201, 74)), ('cobre', (230, 150, 90)), ('azul', (127, 170, 255))):
    fic.append((nombre, tinta(sprite('punta', 'ficha'), rgb).resize((120, 120), Image.LANCZOS), tinta(sprite('punta', 'ficha_p'), rgb).resize((120, 120), Image.LANCZOS)))
fila_pieza('En la punta de la lengua: la ficha-letra (el código la tiñe)', fic, CIELO)
luc = [(f'lucero {i}', sprite('rastro', f'lucero_{i}').resize((110, 110), Image.LANCZOS), sprite('rastro', f'lucero_p_{i}').resize((110, 110), Image.LANCZOS)) for i in (0, 2, 5, 8)]
fila_pieza('Rastro de luz: los luceros (4 de los 9)', luc, CIELO)

# ---- composición: cada fila, sus cuadros en renglones de ancho W
margen = 16; lienzos = []
for titulo, items, fondo in filas:
    x = margen; y = 34; renglones = [[]]; ancho_ren = margen
    for etiqueta, im in items:
        w = max(im.width, 130) + 14
        if ancho_ren + w > W - margen: renglones.append([]); ancho_ren = margen
        renglones[-1].append((etiqueta, im)); ancho_ren += w
    alto = 34 + sum(max(im.height for _, im in r) + 40 for r in renglones) + 8
    lienzo = Image.new('RGBA', (W, alto), fondo); d = ImageDraw.Draw(lienzo); d.text((margen, 8), titulo, font=F, fill=(255, 255, 255, 255))
    y = 36
    for r in renglones:
        x = margen; hmax = max(im.height for _, im in r)
        for etiqueta, im in r:
            w = max(im.width, 130) + 14
            lienzo.alpha_composite(im, (x + (max(im.width, 130) - im.width) // 2, y + (hmax - im.height) // 2))
            d.text((x, y + hmax + 4), etiqueta, font=F2, fill=(210, 215, 255, 255))
            x += w
        y += hmax + 40
    lienzos.append(lienzo)
total = Image.new('RGBA', (W, sum(l.height for l in lienzos) + 4 * len(lienzos)), (20, 20, 40, 255))
y = 0
for l in lienzos: total.alpha_composite(l, (0, y)); y += l.height + 4
os.makedirs(os.path.dirname(SALIDA), exist_ok=True)
total.convert('RGB').save(SALIDA); print(SALIDA, total.size)
