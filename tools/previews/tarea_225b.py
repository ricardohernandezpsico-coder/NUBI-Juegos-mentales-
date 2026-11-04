# -*- coding: utf-8 -*-
"""Lámina de la Tarea 225b (4-nov-2026): el botón «Tú» pasa a ser parte de la cabecera de cada pestaña (se va con la página), la «R» ya no toca el buscador de Juegos y la guía ilumina el botón en su paso 3 con «Saltar la guía» a la izquierda.
Capturas REALES del emulador (`medium_phone`) con `tools/capturar_225b.py`. «Antes» = la Tarea 225 (`unity/test-results/t225/despues_*` y `unity/test-results/t225b/antes_hoy200_*`); «después» = `unity/test-results/t225b/despues_*`.
Uso:  python -I tools/previews/tarea_225b.py      → docs/previews/tarea-225b.png
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
OLD = os.path.join(ROOT, 'unity', 'test-results', 't225')
NEW = os.path.join(ROOT, 'unity', 'test-results', 't225b')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-225b.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
NAVY, SUN, DIM, LINE, WHITE, LIME = (20, 18, 63), (255, 203, 69), (200, 196, 235), (90, 86, 160), (240, 244, 255), (164, 230, 74)
GAP, MARGIN = 28, 24
CELL = 393
SHEET_W = MARGIN * 2 + 4 * CELL + 3 * GAP


def font(px, bold=True): return ImageFont.truetype(os.path.join(FONTS, 'fredoka.ttf' if bold else 'nunito_regular.ttf'), int(px))


def wrap(draw, text, f, width):
    lines = []
    for para in text.split('\n'):
        cur = ''
        for word in para.split(' '):
            t = (cur + ' ' + word).strip()
            if draw.textlength(t, font=f) <= width: cur = t
            else: lines.append(cur); cur = word
        lines.append(cur)
    return lines


def load(folder, name):
    path = os.path.join(folder, name + '.png')
    if not os.path.exists(path):
        im = Image.new('RGB', (393, 200), (40, 36, 100)); ImageDraw.Draw(im).text((10, 80), 'falta ' + name, font=font(18), fill=SUN); return im
    return Image.open(path).convert('RGB')


def dp_crop(folder, name, w, y0, y1):
    """Una franja de la captura (`w` dp de ancho; 2,625 px por dp) a 1 dp = 1 px."""
    im = load(folder, name); k = im.width / float(w)
    c = im.crop((0, int(y0 * k), im.width, int(y1 * k)))
    return c.resize((w, int(round(c.height / k))), Image.LANCZOS)


def full(folder, name, w):
    im = load(folder, name); return im.resize((w, int(round(im.height * w / im.width))), Image.LANCZOS)


def block(d, sheet, x, y, title, im, text, sf):
    """Un recuadro: título, la imagen con su marco y un pie; devuelve el alto que ocupó."""
    d.text((x, y), title, font=font(15), fill=WHITE)
    sheet.paste(im, (x, y + 24)); d.rectangle((x - 1, y + 23, x + im.width, y + 24 + im.height), outline=LINE)
    lines = wrap(d, text, sf, max(im.width, 300)) if text else []
    for j, line in enumerate(lines): d.text((x, y + 24 + im.height + 10 + j * 20), line, font=sf, fill=DIM)
    return 24 + im.height + 10 + len(lines) * 20


def main():
    sf = font(15, bold=False)
    d0 = ImageDraw.Draw(Image.new('RGB', (10, 10)))
    CROP = 132                                                              # dp de alto de las cabeceras

    notes = [
        ('LO QUE PIDIÓ OPUS, Y QUÉ SE HIZO',
         '1) El botón «Tú» ya NO flota encima de todo: es parte de la cabecera de cada pestaña (`TuHeaderRow`: una fila de 48 dp con el título a la izquierda y el botón arriba a la derecha, centro a 24 dp, margen de 16 dp, toque de 48 × 48). En Hoy va en la fila del logotipo y se va hacia arriba con la página; en Contrarreloj va en la cabecera, dentro de la lista; en Juegos y Avance, en la fila del título (en Juegos el título y el buscador quedan fijos y el botón con ellos). '
         'En la página de un área de Avance no hay botón: allí manda su propia flecha de volver. MainActivity ya no dibuja ningún botón.\n'
         '2) El aire: la sombra del botón termina a los 48 dp, así que lo de abajo empieza a 56 dp o más. Juegos: el buscador pasa de 46 a 56 dp (+10); Contrarreloj: el subtítulo de 44 a 56 dp (+12; todo lo de abajo baja 12); Avance: las llamitas de 54 a 56 dp (+2). `TuFlowTest` mide el aire en las 6 configuraciones (360 y 393 dp × letra 1,0 · 1,3 · 2,0) en las tres pestañas.\n'
         '3) La guía, paso 3: «Saltar la guía» va arriba a la IZQUIERDA (sobre el logotipo, que la guía nunca ilumina) y el paso 3 ilumina también el botón «Tú» con un hueco REDONDO y aro lima (64 dp: el botón más 8 dp de aire), además de Contrarreloj y Avance. Antes del paso, la guía sube Hoy hasta arriba (`bringIntoView`) para que el botón se vea entero junto con la barra. Tocar el botón iluminado es «Listo» y NO abre Tú. El texto del paso 3 no cambia.'),
    ]

    # ---------- armado
    items = []
    # 1) Juegos arriba: antes (T225) y después, 393 y 360 dp, letra 1,0 · 1,3 · 2,0
    juegos = []
    for w in (393, 360):
        for sc, lab in (('10', '1,0'), ('13', '1,3'), ('20', '2,0')):
            juegos.append((w, lab, dp_crop(OLD, 'despues_%d_%s_Juegos' % (w, sc), w, 0, CROP), dp_crop(NEW, 'despues_%d_%s_Juegos' % (w, sc), w, 0, CROP)))
    # 2) Contrarreloj y Avance arriba (antes de la 225b = el botón flotante) a 393/1,0 y 360/2,0
    otras = []
    for w, sc, lab in ((393, '10', '1,0'), (360, '20', '2,0')):
        for t in ('Contrarreloj', 'Avance'):
            otras.append(('%s · %d dp · letra %s' % (t, w, lab), dp_crop(OLD, 'despues_%d_%s_%s' % (w, sc, t), w, 0, CROP), dp_crop(NEW, 'despues_%d_%s_%s' % (w, sc, t), w, 0, CROP)))
    # 3) Hoy deslizada ~200 dp
    hoy = [('Hoy deslizada unos 200 dp · 393 dp', full(NEW, 'antes_hoy200_393_10', 393), full(NEW, 'despues_hoy200_393_10', 393)),
           ('Hoy deslizada unos 200 dp · 360 dp', full(NEW, 'antes_hoy200_360_10', 360), full(NEW, 'despues_hoy200_360_10', 360))]
    # 4) guía, paso 3
    guia = [('Guía, paso 3 · 393 dp · letra 1,0', full(OLD, 'despues_guia3_393_10', 393), full(NEW, 'despues_guia3_393_10', 393)),
            ('Guía, paso 3 · 360 dp · letra 2,0', full(OLD, 'despues_guia3_360_20', 360), full(NEW, 'despues_guia3_360_20', 360))]

    # ---------- alturas
    head = 74
    note_h = sum(30 + len(wrap(d0, b, sf, SHEET_W - 2 * MARGIN)) * 20 + 14 for _, b in notes)
    juegos_h = 30 + 2 * (20 + (CROP + 12) * 2 + 22)
    otras_h = 30 + 2 * (20 + (CROP + 12) * 2 + 22)
    hoy_h = 30 + 24 + max(i.height for _, a, b in hoy for i in (a, b)) + 14 + 30
    guia_h = 30 + 24 + max(i.height for _, a, b in guia for i in (a, b)) + 14 + 30
    total = MARGIN + head + juegos_h + otras_h + hoy_h + guia_h + note_h + MARGIN + 700          # holgado: al final se recorta
    sheet = Image.new('RGB', (SHEET_W, total), NAVY)
    d = ImageDraw.Draw(sheet)
    d.text((MARGIN, MARGIN), 'Tarea 225b · el botón «Tú» va en la cabecera de cada pestaña y se mueve con la página', font=font(26), fill=SUN)
    sub = 'Capturas REALES del emulador, a tamaño real (1 dp = 1 px; las cabeceras, recortadas a %d dp de alto). Antes: la Tarea 225 (el botón flotaba encima de todo). Después: el botón dentro de la cabecera de cada pestaña.' % CROP
    for i, line in enumerate(wrap(d, sub, sf, SHEET_W - 2 * MARGIN)): d.text((MARGIN, MARGIN + 38 + i * 20), line, font=sf, fill=DIM)
    y = MARGIN + head

    d.text((MARGIN, y), 'JUEGOS ARRIBA · la «R» tocaba el buscador; ahora hay 8 dp de aire (arriba, antes; abajo, después)', font=font(17), fill=SUN); y += 30
    for g in range(2):
        grupo = juegos[g * 3:(g + 1) * 3]
        d.text((MARGIN, y), '%d dp de ancho · letra 1,0 · 1,3 · 2,0' % grupo[0][0], font=font(15), fill=WHITE); y += 20
        for col in (2, 3):
            x = MARGIN
            for item in grupo:
                im = item[col]; sheet.paste(im, (x, y)); d.rectangle((x - 1, y - 1, x + im.width, y + im.height), outline=LINE); x += item[0] + GAP
            y += CROP + 12
        y += 10

    d.text((MARGIN, y), 'CONTRARRELOJ Y AVANCE ARRIBA · el botón dentro de la cabecera (arriba, antes; abajo, después)', font=font(17), fill=SUN); y += 30
    for g in range(2):
        grupo = otras[g * 2:(g + 1) * 2]
        for col in (1, 2):
            x = MARGIN
            for item in grupo:
                if col == 1: d.text((x, y), item[0] + (' · antes' if col == 1 else ''), font=font(15), fill=WHITE)
                x += item[1].width + GAP
            x = MARGIN
            for item in grupo:
                im = item[col]; sheet.paste(im, (x, y + 20)); d.rectangle((x - 1, y + 19, x + im.width, y + 20 + im.height), outline=LINE); x += im.width + GAP
            y += 20 + CROP + 12
        y += 10

    d.text((MARGIN, y), 'HOY DESLIZADA · antes el botón se quedaba sobre la tarjeta; ahora se va con el logotipo', font=font(17), fill=SUN); y += 30
    x = MARGIN; hmax = 0
    for title, a, b in hoy:
        for lab, im in (('Antes · ' + title, a), ('Después · ' + title, b)):
            h = block(d, sheet, x, y, lab, im, '', sf); hmax = max(hmax, h); x += im.width + GAP
    y += hmax + 14 + 30 - 30

    d.text((MARGIN, y), 'LA GUÍA, PASO 3 · «Saltar la guía» a la izquierda; el botón «Tú» iluminado con su hueco redondo', font=font(17), fill=SUN); y += 30
    x = MARGIN; hmax = 0
    for title, a, b in guia:
        for lab, im in (('Antes · ' + title, a), ('Después · ' + title, b)):
            h = block(d, sheet, x, y, lab, im, '', sf); hmax = max(hmax, h); x += im.width + GAP
    y += hmax + 14 + 30 - 30

    for title, body in notes:
        d.text((MARGIN, y), title, font=font(17), fill=SUN); y += 28
        for line in wrap(d, body, sf, SHEET_W - 2 * MARGIN): d.text((MARGIN, y), line, font=sf, fill=WHITE); y += 20
        y += 14
    sheet = sheet.crop((0, 0, SHEET_W, min(total, y + MARGIN)))
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    sheet.save(OUT, optimize=True)
    print('listo', OUT, sheet.size)


main()
