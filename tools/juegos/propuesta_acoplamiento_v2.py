# -*- coding: utf-8 -*-
"""La hoja de Acoplamiento v2 (Tarea 115, parte 2): «hoy | v1 | v2» en pantallas de teléfono (0,5 del tamaño real) y recortes al 100 %: el módulo, el módulo con el bloque más chico (47 u) y otras piezas (con un 2 × 2). NO está integrada a ningún juego.
Ricardo aprobó la idea de las bolitas pero la quiere ver antes de integrarla. Entradas: tools/juegos/out/propuesta/aco_{hoy,propuesta,v2}.png (las saca `propuesta_acoplamiento.py`); salida: docs/previews/plasticina/propuesta-acoplamiento-v2.png.
Uso: python -I tools/juegos/propuesta_acoplamiento_v2.py"""
import os, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFont
AQUI = os.path.dirname(os.path.abspath(__file__)); REPO = os.path.normpath(os.path.join(AQUI, '..', '..'))
sys.path.insert(0, AQUI); sys.stdout.reconfigure(encoding='utf-8')
import propuesta_acoplamiento as pa
from acopla_geo import normalize, pick_pieces
PROP = os.path.join(AQUI, 'out', 'propuesta')
SALIDA = os.path.join(REPO, 'docs', 'previews', 'plasticina', 'propuesta-acoplamiento-v2.png')
FONTS = os.path.join(REPO, 'unity', 'NeuroVidaCore', 'Assets', 'Resources', 'Fonts')
def fuente(n, px): return ImageFont.truetype(os.path.join(FONTS, n), px)
TITULO = fuente('Fredoka-Bold.ttf', 40); SUB = fuente('Fredoka-SemiBold.ttf', 26); ETQ = fuente('Fredoka-SemiBold.ttf', 24); TXT = fuente('AtkinsonHyperlegible-Bold.ttf', 21)
FONDO = (16, 16, 24); BLANCO = (240, 238, 250); GRIS = (168, 164, 196); LIMA = (166, 227, 107)
W = 1728; M = 30; GAP = 24; ESC = 0.5

def carga(n): return Image.open(os.path.join(PROP, n)).convert('RGB')
def a_pil(a): return Image.fromarray((np.clip(a, 0, 1) * 255 + 0.5).astype(np.uint8))

def envuelve(texto, f, ancho):
    palabras = texto.split(' '); lineas = []; actual = ''
    for p in palabras:
        prueba = (actual + ' ' + p).strip()
        if f.getlength(prueba) <= ancho: actual = prueba
        else: lineas.append(actual); actual = p
    if actual: lineas.append(actual)
    return lineas

def parrafos(textos, ancho=W - 2 * M):
    ren = []
    for t in textos: ren += envuelve(t, TXT, ancho)
    return ren

def hoja():
    secciones = []
    nota = 'hoy (captura real) | v1 (la hoja anterior) | v2 (bolitas). Solo propuesta: no está en ningún juego. Pantallas a 0,5 del tamaño de un teléfono; los recortes, al 100 %.'
    def titulo(hj, dr, y):
        dr.text((M, y + 8), 'Acoplamiento v2: bolitas de plasticina en cada cuadrito', font=TITULO, fill=BLANCO)
        for i, l in enumerate(envuelve(nota, TXT, W - 2 * M)): dr.text((M, y + 60 + i * 28), l, font=TXT, fill=GRIS)
    secciones.append((120, titulo))

    a, b, c = carga('aco_hoy.png'), carga('aco_propuesta.png'), carga('aco_v2.png'); w, h = int(1080 * ESC), int(2400 * ESC)
    def pantallas(hj, dr, y):
        for i, (t, col, im) in enumerate((('HOY', GRIS, a), ('V1', GRIS, b), ('V2: bolitas y almohada', LIMA, c))):
            x = M + i * (w + GAP); dr.text((x, y + 2), t, font=ETQ, fill=col); hj.paste(im.resize((w, h), Image.LANCZOS), (x, y + 36))
    secciones.append((36 + h + 16, pantallas))

    pie = ['V2: una bolita de plasticina (un domo de 0,33 del bloque) en el centro de cada cuadrito, con sombreado ISÓTROPO (brillo centrado y borde parejo: ninguna luz de un lado que gire con la pieza y delate el espejo) y su sombrita de contacto en la pantalla, hacia abajo, como el canto. '
           'El módulo, más inflado (almohada): el sombreado sale de la distancia al borde y se oscurece hacia él; canto de 5 dp hacia abajo y sombra; variación de color a mano; mate y celeste cálido. Sin juntas ni «+». El hueco del puerto queda como en la v1: cavidad hundida, lisa, sin bolitas.']
    ren = parrafos(pie)
    secciones.append((len(ren) * 28 + 20, lambda hj, dr, y: [dr.text((M, y + 4 + i * 28), l, font=TXT, fill=GRIS) for i, l in enumerate(ren)]))

    box = (300, 900, 780, 1300)
    def recorte3(hj, dr, y):
        dr.text((M, y + 2), 'AL 100 %: el módulo', font=SUB, fill=BLANCO)
        for i, (t, col, im) in enumerate((('HOY', GRIS, a), ('V1', GRIS, b), ('V2', LIMA, c))):
            x = M + i * (480 + GAP); dr.text((x, y + 40), t, font=ETQ, fill=col); hj.paste(im.crop(box), (x, y + 70))
    secciones.append((70 + 400 + 20, recorte3))

    # el bloque chico (47 u), v1 | v2 a 100 % y a 2 × para leerlas
    L4 = pick_pieces()['L4']; P = normalize([(-x, -y) for x, y in L4]); cara = pa.perfil_cara(P)
    def recorta(im, margen=22):
        arr = np.asarray(im).astype(int); d = np.abs(arr - np.array([8, 8, 28])[None, None, :]).sum(axis=2) > 12; ys, xs = np.where(d)
        return im.crop((max(0, xs.min() - margen), max(0, ys.min() - margen), min(im.size[0], xs.max() + margen), min(im.size[1], ys.max() + margen)))
    chicos = [(v, recorta(a_pil(pa.modulo_suelto(v, 47, cara=cara)))) for v in (1, 2)]
    # las dos del mismo tamaño de lienzo para que queden alineadas
    cw, chh = max(im.size[0] for v, im in chicos), max(im.size[1] for v, im in chicos)
    chicos = [(v, im) for v, im in chicos]
    def chico(hj, dr, y):
        dr.text((M, y + 2), 'AL 100 % con el bloque más chico (47 u por bloque: la bolita mide 31 u, unos 10 dp de diámetro): v1 | v2, y las dos a 2 × para leerlas', font=SUB, fill=BLANCO)
        x = M
        for v, im in chicos:
            dr.text((x, y + 40), 'V1' if v == 1 else 'V2', font=ETQ, fill=GRIS if v == 1 else LIMA); hj.paste(im, (x, y + 70)); x += im.size[0] + GAP
        for v, im in chicos:
            big = im.resize((im.size[0] * 2, im.size[1] * 2), Image.LANCZOS); hj.paste(big, (x, y + 40)); x += big.size[0] + GAP
    secciones.append((70 + chicos[0][1].size[1] * 2 + 20, chico))

    # otras piezas (con un 2 × 2) a 78 u: las 4 bolitas de un 2 × 2 quedan como 4 puntos sueltos
    piezas = pick_pieces(); ims = []
    for nombre in ('L4', 'P5', 'F5', 'H7'):
        Q = normalize(piezas[nombre]); cq = pa.perfil_cara(Q)
        ims.append((nombre, a_pil(pa.modulo_suelto(2, 78, cara=cq, P=Q, angulo=0.0, flip=False))))
    def otras(hj, dr, y):
        dr.text((M, y + 2), 'Otras piezas en v2 (a 78 u): el P5 trae un 2 × 2; sus 4 bolitas quedan como 4 puntos sueltos, sin juntas ni «+»', font=SUB, fill=BLANCO)
        x = M
        for nombre, im in ims:
            iw, ih = int(im.size[0] * 0.8), int(im.size[1] * 0.8); hj.paste(im.resize((iw, ih), Image.LANCZOS), (x, y + 44)); x += iw + 10
    secciones.append((44 + int(ims[0][1].size[1] * 0.8) + 20, otras))

    medidas = ['MEDIDAS (tools/juegos/propuesta_medidas.py aco2): la precisión manda; la silueta y la respuesta no cambian.',
               'SILUETA: la cara de la v2 es EXACTAMENTE la silueta de la v1 (diferencia del alfa 0) y esa es la del SDF (IoU 0,998): las bolitas y la almohada van adentro y el contorno no se toca.',
               'FUGA DEL ESPEJO con bolitas, canto y sombras: la pieza espejada en pantalla y la de celdas reflejadas sin espejar dan lo mismo (diferencia menor que 1e-11 sobre 255, o sea 0) y los centros de las bolitas coinciden; el compositor solo ve la silueta de pantalla: la luz, el canto, el anillo de la bolita y el grano no tienen dirección.',
               'CONTRASTE de la bolita contra la cara (razón de luminancia): a 78 u por bloque, centro/cara 1,47 : 1 y filo/cara 1,66 : 1; a 47 u (bolita de 31 u, unos 10 dp), centro/cara 1,72 : 1 y filo/cara 1,43 : 1; todas sobre el 1,3 : 1 pedido. Se lee a 47 u: no hace falta una alternativa.',
               'CONTRASTE de la pieza contra el fondo: la cara no cambia de color medio (11,5 : 1 contra el cielo, igual que hoy y que la v1); el canto separa la pieza por abajo.']
    ren_m = []
    for i, t in enumerate(medidas): ren_m += [(l, i == 0) for l in envuelve(t, TXT, W - 2 * M)]
    secciones.append((len(ren_m) * 28 + 50, lambda hj, dr, y: [dr.text((M, y + 14 + i * 28), l, font=TXT, fill=BLANCO if tit else GRIS) for i, (l, tit) in enumerate(ren_m)]))

    alto = sum(s[0] for s in secciones) + M
    hj = Image.new('RGB', (W, alto), FONDO); dr = ImageDraw.Draw(hj); y = 0
    for hh, f in secciones:
        f(hj, dr, y); y += hh
    return hj

if __name__ == '__main__':
    hj = hoja(); print('hoja', hj.size)
    q = hj.quantize(colors=256, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.FLOYDSTEINBERG)
    os.makedirs(os.path.dirname(SALIDA), exist_ok=True); q.save(SALIDA, optimize=True)
    print('guardada', SALIDA, os.path.getsize(SALIDA) // 1024, 'KB')
