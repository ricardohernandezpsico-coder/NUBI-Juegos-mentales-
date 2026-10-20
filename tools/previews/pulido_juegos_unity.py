# -*- coding: utf-8 -*-
"""Hoja «hoy | propuesta» de la ronda de pulido de los juegos de Unity (Tarea 144, 20-oct; revisión de Fable `auditoria-fable/revision-pulido-19-oct.md`, «Tarea B»):
la primera pantalla de Freno, Punta y Disparate y los marcadores de Bodega y Acoplamiento, con CAPTURAS REALES a 1080x2400 (la forma de teléfono 20:9), a buen tamaño.
«HOY» sale de `docs/previews/pulido-juegos-unity-antes/` (las capturas del 19-oct, reducidas a 540 px de ancho: las originales de `unity/test-results/capturas/` se regraban en cada corrida) y
«PROPUESTA» de `unity/test-results/capturas/<juego>/` (`bash tools/verificar-todo.sh --capturas Freno,Anagramas,Disparate,Bodega,Acoplamiento`).
Punta lleva tres imágenes: hoy, la propuesta mientras se piensa (bandeja tenue) y la propuesta con «¡La tengo!» (fichas caídas, bandeja firme; toma «fichas»).
Uso:  python -I tools/previews/pulido_juegos_unity.py      → docs/previews/pulido-juegos-unity.png
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
BEFORE = os.path.join(ROOT, 'docs', 'previews', 'pulido-juegos-unity-antes')
AFTER = os.path.join(ROOT, 'unity', 'test-results', 'capturas')
OUT = os.path.join(ROOT, 'docs', 'previews', 'pulido-juegos-unity.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
NAVY, SUN, DIM, CREAM, LIME = (20, 18, 63), (255, 203, 69), (200, 196, 235), (245, 235, 219), (155, 229, 100)
W, GAP, MARGIN, CELL_GAP = 480, 12, 34, 56


def font(px, bold=True): return ImageFont.truetype(os.path.join(FONTS, 'fredoka.ttf' if bold else 'nunito_regular.ttf'), int(px))


B = lambda name: os.path.join(BEFORE, name)
A = lambda name: os.path.join(AFTER, name)
TOP = (0, 0, 1080, 1000)
# una celda: (imágenes [(archivo, recorte, rótulo, color)], título, qué cambió, nota)
FRENO = ([(B('freno_02-primera.png'), None, 'HOY', DIM), (A('freno/02-primera.png'), None, 'PROPUESTA', LIME)], 'Freno de Emergencia',
         'el conjunto botón-plataforma-cohete sube y crece ×1,3, el botón pasa de 210 a 240 u y la luna sube con él (de 0,20 a 0,30 del alto); la señal ALTO (el estímulo) sube con el conjunto: ≈ +60 dp en 20:9 y ≈ +25 dp en 16:9; mismo tamaño, momento y sonido', '')
DISPARATE = ([(B('disparate_02-primera.png'), None, 'HOY', DIM), (A('disparate/02-primera.png'), None, 'PROPUESTA', LIME)], '¿Verdad o disparate?',
             'la placa centrada entre la antena y los botones; la frase de 24 a 28 sp; «Verdad» y «Disparate» sin mayúsculas sostenidas', '')
PUNTA = ([(B('anagramas_02-primera.png'), None, 'HOY', DIM), (A('anagramas/02-primera.png'), None, 'PROPUESTA · mientras se piensa', LIME), (A('anagramas/03-fichas.png'), None, 'PROPUESTA · con «¡La tengo!»', LIME)],
         'En la punta de la lengua', '«¿Cuál es la palabra?» pegada a 16 dp bajo la tarjeta y, desde el primer cuadro, una bandeja de 15 pozos hundidos donde caerán las fichas: tenue (≈45 %) mientras se piensa, firme en 200 ms con «¡La tengo!», y los pozos que sobran se desvanecen',
         'pozos fijos: no revelan el largo')
BODEGA = ([(B('bodega_08-momento.png'), TOP, 'HOY', DIM), (A('bodega/08-momento.png'), TOP, 'PROPUESTA', LIME)], 'Bodega: «¡Pedido perfecto!»',
          'el chip «×2 seguidos» ya no tapa el final de «perfecto!»: se pone bajo la tarjeta del pedido', '')
ACOPLAMIENTO = ([(B('acoplamiento_06-anillo-completo.png'), TOP, 'HOY', DIM), (A('acoplamiento/06-anillo-completo.png'), TOP, 'PROPUESTA', LIME)], 'Acoplamiento: el marcador',
                'un solo marcador: «Módulo 2 · 8 a bordo» y el chip común «×8 seguidos» (antes se montaban sobre «Módulo N» y sobre la consigna)', '')
ROWS = [[FRENO, DISPARATE], [PUNTA], [BODEGA, ACOPLAMIENTO]]


def load(path, crop):
    im = Image.open(path).convert('RGB')
    if im.width != 1080:                                  # las de HOY vienen reducidas a 540 px de ancho
        k = 1080 / im.width
        im = im.resize((1080, round(im.height * k)), Image.LANCZOS)
    if crop: im = im.crop(crop)
    k = W / im.width
    return im.resize((W, round(im.height * k)), Image.LANCZOS)


def wrap(d, text, fnt, width):
    lines, cur = [], ''
    for word in text.split(' '):
        t = (cur + ' ' + word).strip()
        if d.textlength(t, font=fnt) <= width: cur = t
        else: lines.append(cur); cur = word
    if cur: lines.append(cur)
    return lines


probe = ImageDraw.Draw(Image.new('RGB', (10, 10)))
SUBF = font(19, False)


def cell_size(cell):
    imgs, title, sub, note = cell
    n = len(imgs)
    width = n * W + (n - 1) * GAP
    loaded = [load(p, c) for p, c, _, _ in imgs]
    h = max(i.height for i in loaded) + 34 + 40 + 26 * len(wrap(probe, sub, SUBF, width)) + (34 if note else 0) + 14
    return width, h, loaded


sizes = [[cell_size(c) for c in row] for row in ROWS]
row_w = [sum(s[0] for s in row) + CELL_GAP * (len(row) - 1) for row in sizes]
row_h = [max(s[1] for s in row) for row in sizes]
HEAD = 170
width = MARGIN * 2 + max(row_w)
height = HEAD + sum(row_h) + CELL_GAP * len(ROWS) + 10
sheet = Image.new('RGB', (width, height), NAVY); d = ImageDraw.Draw(sheet)
d.text((MARGIN, 22), 'Pulido de los juegos de Unity (Tarea 144): hoy | propuesta', font=font(50), fill=SUN)
d.text((MARGIN, 88), 'Capturas reales a 1080 × 2400 (forma 20:9), a la mitad de tamaño. HOY = las del 19-oct; PROPUESTA = las de hoy. Cambia solo el lugar y el tamaño de las cosas (y dos textos): ninguna regla, tiempo ni medida.', font=font(21, False), fill=DIM)
d.text((MARGIN, 118), 'Freno puede mover unos milisegundos el tiempo de lanzar y de frenar (botón más grande y más cerca del pulgar; señal ALTO en otro lugar): queda anotado con la fecha en docs/medidas-juegos-estrella.md.', font=font(21, False), fill=DIM)
y = HEAD
for r, row in enumerate(ROWS):
    x = MARGIN
    for c, cell in enumerate(row):
        imgs, title, sub, note = cell
        cw, ch, loaded = sizes[r][c]
        for k, (im, (_, _, tag, col)) in enumerate(zip(loaded, imgs)):
            xx = x + k * (W + GAP)
            sheet.paste(im, (xx, y)); d.rectangle((xx, y, xx + im.width - 1, y + im.height - 1), outline=(70, 74, 130), width=1)
            d.text((xx, y + im.height + 4), tag, font=font(22), fill=col)
        ty = y + max(i.height for i in loaded) + 34
        d.text((x, ty), title, font=font(28), fill=CREAM)
        ty += 40
        for line in wrap(d, sub, SUBF, cw):
            d.text((x, ty), line, font=SUBF, fill=DIM); ty += 26
        if note: d.text((x, ty + 2), note, font=font(24), fill=SUN)
        x += cw + CELL_GAP
    y += row_h[r] + CELL_GAP
sheet.save(OUT, optimize=True); print('hoja:', OUT, sheet.size, os.path.getsize(OUT) // 1024, 'KB')
