# -*- coding: utf-8 -*-
"""La hoja de la propuesta de volumen (Tarea 114, parte 2): Engranajes y Acoplamiento, «hoy | propuesta» en pantallas del tamaño de un teléfono (1080 × 2400 reducidas a 0,6) y recortes al 100 %.
NO está integrada a ningún juego: es una propuesta para que Ricardo apruebe; después vienen la Tarea 115 (Engranajes) y la 116 (Acoplamiento, fase B).
Entradas: tools/juegos/out/propuesta/{eng_n12,eng_t08,aco}_{hoy,propuesta}.png (las sacan propuesta_engranajes.py y propuesta_acoplamiento.py); salida: docs/previews/plasticina/propuesta-volumen.png (paleta reducida con tramado para que pese poco).
Uso: python -I tools/juegos/propuesta_volumen.py"""
import os, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFont
AQUI = os.path.dirname(os.path.abspath(__file__)); REPO = os.path.normpath(os.path.join(AQUI, '..', '..'))
sys.stdout.reconfigure(encoding='utf-8')
PROP = os.path.join(AQUI, 'out', 'propuesta')
SALIDA = os.path.join(REPO, 'docs', 'previews', 'plasticina', 'propuesta-volumen.png')
FONTS = os.path.join(REPO, 'unity', 'NeuroVidaCore', 'Assets', 'Resources', 'Fonts')
def fuente(nombre, px): return ImageFont.truetype(os.path.join(FONTS, nombre), px)
TITULO = fuente('Fredoka-Bold.ttf', 40); SUB = fuente('Fredoka-SemiBold.ttf', 26); ETQ = fuente('Fredoka-SemiBold.ttf', 24); TXT = fuente('AtkinsonHyperlegible-Bold.ttf', 21)
FONDO = (16, 16, 24); BLANCO = (240, 238, 250); GRIS = (168, 164, 196); CORAL = (255, 128, 112); LIMA = (166, 227, 107)
W = 1380; M = 30; GAP = 24; ESC = 0.6

def carga(n): return Image.open(os.path.join(PROP, n)).convert('RGB')

def envuelve(texto, fuente_, ancho):
    """El [texto] partido en renglones que caben en [ancho] px."""
    palabras = texto.split(' '); lineas = []; actual = ''
    for p in palabras:
        prueba = (actual + ' ' + p).strip()
        if fuente_.getlength(prueba) <= ancho: actual = prueba
        else: lineas.append(actual); actual = p
    if actual: lineas.append(actual)
    return lineas

def parrafos(textos, ancho=W - 2 * M, fuente_=None, interlinea=28):
    """(renglones, alto) de una lista de párrafos envueltos."""
    fuente_ = fuente_ or TXT; ren = []
    for t in textos: ren += envuelve(t, fuente_, ancho)
    return ren, len(ren) * interlinea

def hoja():
    secciones = []                                                   # cada una: (alto, función que dibuja sobre (hoja, dr, y0))
    def titulo(h, dr, y):
        dr.text((M, y + 10), 'Propuesta de volumen: Engranajes y Acoplamiento', font=TITULO, fill=BLANCO)
        for i, l in enumerate(envuelve('hoy (captura real) | propuesta (rehecha sobre la misma captura). Solo propuesta: no está en ningún juego. Pantallas a 0,6 del tamaño de un teléfono; los recortes, al 100 %.', TXT, W - 2 * M)): dr.text((M, y + 62 + i * 28), l, font=TXT, fill=GRIS)
    secciones.append((140, titulo))
    def pantallas(nombre, hoy, prop, pie):
        a = carga(hoy); b = carga(prop); w, h = int(a.size[0] * ESC), int(a.size[1] * ESC); ren, alto_pie = parrafos([' '.join(pie)])
        def f(hj, dr, y):
            dr.text((M, y + 2), nombre, font=SUB, fill=BLANCO)
            dr.text((M, y + 40), 'HOY', font=ETQ, fill=GRIS); dr.text((M + w + GAP, y + 40), 'PROPUESTA', font=ETQ, fill=LIMA)
            hj.paste(a.resize((w, h), Image.LANCZOS), (M, y + 76)); hj.paste(b.resize((w, h), Image.LANCZOS), (M + w + GAP, y + 76))
            for i, linea in enumerate(ren): dr.text((M, y + 76 + h + 10 + i * 28), linea, font=TXT, fill=GRIS)
        return (76 + h + 20 + alto_pie + 24, f)
    secciones.append(pantallas('ENGRANAJES (nivel 12: correas, barra, 3 cartels, 2 llaves)', 'eng_n12_hoy.png', 'eng_n12_propuesta.png',
        ['Cada objeto con CANTO (su silueta más oscura, 4,5 dp hacia ABAJO en la pantalla: no gira), sombra de contacto, cara mate con bisel por distancia al borde y color por rol:',
         'motor azul, camino LILA, pieza dorada. Letras y bordes de los cartels en relieve; cohete con cuerpo redondo, remaches y luces con aro; «Arrancar» y las llaves, losas del kit.']))
    secciones.append(pantallas('ACOPLAMIENTO (camino 2 del estudio)', 'aco_hoy.png', 'aco_propuesta.png',
        ['Módulo con la MISMA silueta, cara mate (sin franja de brillo), bisel ×3, variación a mano, celeste más cálido y canto 5 dp hacia abajo; hueco como cavidad (pared de arriba y fondo),',
         'panel del puerto como losa gruesa, luces del puerto como churros y botones del kit. La luz, el canto y el grano son de la pantalla y sin dirección: no delatan el espejo.']))
    def recortes(titulo_r, pares, nota=None):
        # pares: lista de (hoy, prop, caja) ; se apilan (una fila por par) al 100 %
        filas = []
        for (hn, pn, caja) in pares:
            a = carga(hn).crop(caja); b = carga(pn).crop(caja); filas.append((a, b))
        alto = 40 + sum(a.size[1] + 40 for a, b in filas) + (30 if nota else 0)
        def f(hj, dr, y):
            dr.text((M, y + 2), titulo_r, font=SUB, fill=BLANCO)
            yy = y + 44
            for a, b in filas:
                dr.text((M, yy - 4), 'HOY', font=ETQ, fill=GRIS); dr.text((M + a.size[0] + GAP, yy - 4), 'PROPUESTA', font=ETQ, fill=LIMA)
                hj.paste(a, (M, yy + 26)); hj.paste(b, (M + a.size[0] + GAP, yy + 26)); yy += a.size[1] + 40
            if nota: dr.text((M, yy - 10), nota, font=TXT, fill=GRIS)
        return (alto, f)
    secciones.append(recortes('AL 100 %: engranajes, correas y poleas (Engranajes)', [('eng_n12_hoy.png', 'eng_n12_propuesta.png', (40, 660, 640, 1160))]))
    secciones.append(recortes('AL 100 %: un cartel que falla, con su moneda «no» del kit (Engranajes)', [('eng_t08_hoy.png', 'eng_t08_propuesta.png', (640, 620, 1060, 900))]))
    secciones.append(recortes('AL 100 %: cartels, remaches y luces con aro del cohete (Engranajes)', [('eng_n12_hoy.png', 'eng_n12_propuesta.png', (600, 700, 1060, 1100))]))
    secciones.append(recortes('AL 100 %: la cuenta de llaves y «Arrancar» con losas del kit (Engranajes)', [('eng_n12_hoy.png', 'eng_n12_propuesta.png', (30, 1700, 640, 2080))]))
    secciones.append(recortes('AL 100 %: el módulo (Acoplamiento)', [('aco_hoy.png', 'aco_propuesta.png', (300, 900, 780, 1300))]))
    secciones.append(recortes('AL 100 %: el puerto, el hueco como cavidad y las luces (Acoplamiento)', [('aco_hoy.png', 'aco_propuesta.png', (240, 1580, 840, 1880))]))
    medidas = [
        'MEDIDAS (tools/juegos/propuesta_medidas.py): la precisión manda; la silueta y las medidas no cambian.',
        'ACOPLAMIENTO: silueta de la cara = la exacta (IoU 0,998 contra el SDF analítico); espejo con canto y sombra incluidos: la pieza espejada en pantalla y la de celdas reflejadas',
        'dan EXACTAMENTE lo mismo (diferencia menor que 1e-11 sobre 255, o sea 0: el compositor solo ve la silueta); contraste pieza/fondo: cara 11,5 : 1 (igual que hoy); canto 2,1 : 1 contra el fondo y 5,4 : 1 contra la cara.',
        'ENGRANAJES: las siluetas son las exactas de cada objeto (alfa de la capa suelta). Hueco entre dientes con el canto detrás (razón de contraste diente/hueco, mediana de 12 engranajes):',
        'mitad de arriba: de 8,2 a 5,9 : 1; mitad de abajo, donde cae el canto: de 9,5 a 3,1 : 1 (sobre el 3 : 1 de lo que no es texto); 11 de 12 engranajes conservan ≥ 83 % del hueco legible; el peor es uno casi tapado por una polea.',
    ]
    ren_m = []
    for i, t in enumerate(medidas): ren_m += [(l, i == 0) for l in envuelve(t, TXT, W - 2 * M)]
    secciones.append((len(ren_m) * 28 + 50, lambda hj, dr, y: [dr.text((M, y + 14 + i * 28), l, font=TXT, fill=BLANCO if tit else GRIS) for i, (l, tit) in enumerate(ren_m)]))
    alto = sum(s[0] for s in secciones) + M
    hj = Image.new('RGB', (W, alto), FONDO); dr = ImageDraw.Draw(hj); y = 0
    for h, f in secciones:
        f(hj, dr, y); y += h
    return hj

if __name__ == '__main__':
    hj = hoja(); print('hoja', hj.size)
    q = hj.quantize(colors=256, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.FLOYDSTEINBERG)
    os.makedirs(os.path.dirname(SALIDA), exist_ok=True)
    q.save(SALIDA, optimize=True)
    print('guardada', SALIDA, os.path.getsize(SALIDA) // 1024, 'KB')
