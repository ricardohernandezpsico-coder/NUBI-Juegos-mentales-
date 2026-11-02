# -*- coding: utf-8 -*-
"""Lámina de la Tarea 214 (2-nov-2026): arreglos chicos tras la auditoría de la Tarea 213 (`auditoria-fable/t213-auditoria.md`; decisiones de Ricardo): tres frases de juegos más cortas para que la ficha lleve el nombre y UNA línea
también a 360 dp, y «porcentaje» en la hoja del candado. A TAMAÑO REAL (1 dp = 1 px) y del EMULADOR (`medium_phone`, API 36; 440 de densidad = 393 dp y 480 = 360 dp). «antes» = el APK de la Tarea 213; «ahora» = el de esta tarea.
Fila 1: las fichas de ¿Verdad o disparate?, Piloto Estelar y Aterrizaje Lunar «antes | ahora» a 360 dp con letra 1,0. Fila 2: ¿Verdad o disparate? a 360 dp con letra 1,3, y la hoja del candado a 393 dp con letra 1,0 y a 360 dp con letra 2,0 (sin Plus, con la línea nueva).
Fila 3: las tres fichas a 360 dp con letra 2,0. Las capturas salen de `emu214.py`, que maneja SOLO el emulador, y van a `app/src/test/screenshots/t214/` (fuera de git).
Uso:  python -I tools/previews/tarea_214.py      → docs/previews/tarea-214.png
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SHOTS = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots', 't214')
RES = os.path.join(ROOT, 'app', 'src', 'main', 'res')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-214.png')
FONTS = os.path.join(RES, 'font')
NAVY, SUN, DIM, LINE = (20, 18, 63), (255, 230, 166), (200, 196, 235), (90, 86, 160)
GAP, MARGIN, COLW = 30, 24, 393


def pair(scene, cfg, dp, what, before, after):
    return [('antes_%s_%s' % (scene, cfg), dp, 'antes (T213) · %s' % what, before), ('ahora_%s_%s' % (scene, cfg), dp, 'ahora (T214) · %s' % what, after)]


ROWS = [
    pair('ficha_disparate', '360_10', 360, '¿Verdad o disparate? · 360 dp · letra 1,0', '«Lee la frase y di si tiene sentido»: ya cabía en una línea con letra 1,0', '«Di si la frase tiene sentido»: 198 dp de 296, una línea')
    + pair('ficha_piloto', '360_10', 360, 'Piloto Estelar · 360 dp · letra 1,0', '«Guía la nave y atiende las señales a la vez» pasa a dos renglones', '«Pilota y atiende las señales a la vez»: 270 dp de 296, una línea')
    + pair('ficha_aterrizaje', '360_10', 360, 'Aterrizaje Lunar · 360 dp · letra 1,0', '«Calcula dónde va cada número en la regla» pasa a dos renglones', '«Ubica cada número en la regla»: 230 dp de 296, una línea'),
    pair('ficha_disparate', '360_13', 360, '¿Verdad o disparate? · 360 dp · letra 1,3', 'dos renglones: la ficha se pasaba 69 dp del marco', 'un renglón (252 dp de 296): la ficha vuelve a pasarse 43 dp, como las otras tres del Contrarreloj')
    + pair('sheet', '393_10', 393, 'hoja del candado · 393 dp · letra 1,0', '«…qué % de ella superas y en qué división estás.»', '«…qué porcentaje de ella superas y en qué división estás.»')
    + pair('sheet', '360_20', 360, 'hoja del candado · 360 dp · letra 2,0', 'la línea con «%»', 'la línea con «porcentaje», por palabras'),
    pair('ficha_disparate', '360_20', 360, '¿Verdad o disparate? · 360 dp · letra 2,0', '«Lee la frase y di si / tiene sentido»: dos renglones, por palabras', '«Di si la frase tiene / sentido»: también dos renglones, por palabras; a esta letra ninguna frase se pide de una línea')
    + pair('ficha_piloto', '360_20', 360, 'Piloto Estelar · 360 dp · letra 2,0', 'tres renglones', 'dos renglones: «Pilota y atiende las / señales a la vez»')
    + pair('ficha_aterrizaje', '360_20', 360, 'Aterrizaje Lunar · 360 dp · letra 2,0', 'tres renglones', 'dos renglones: «Ubica cada número / en la regla»'),
]


def font(px, bold=True): return ImageFont.truetype(os.path.join(FONTS, 'fredoka.ttf' if bold else 'nunito_regular.ttf'), int(px))


def wrap(draw, text, f, width):
    lines, cur = [], ''
    for word in text.split(' '):
        t = (cur + ' ' + word).strip()
        if draw.textlength(t, font=f) <= width: cur = t
        else: lines.append(cur); cur = word
    if cur: lines.append(cur)
    return lines


def load(key, dp):
    path = os.path.join(SHOTS, key + '.png')
    if not os.path.exists(path):
        im = Image.new('RGB', (dp, 873), (40, 36, 100)); ImageDraw.Draw(im).text((20, 400), 'falta ' + key, font=font(20), fill=SUN); return im
    im = Image.open(path).convert('RGB')
    return im.resize((dp, int(round(im.height * dp / im.width))), Image.LANCZOS)          # tamaño real: 1 dp = 1 px


def main():
    d0 = ImageDraw.Draw(Image.new('RGB', (10, 10)))
    sf = font(15, bold=False)
    ncols = max(len(r) for r in ROWS)
    sheet_w = MARGIN * 2 + ncols * COLW + (ncols - 1) * GAP
    head = 96
    heights = []
    for row in ROWS:
        shots = max(load(k, dp).height for k, dp, _, _ in row)
        cap = max(len(wrap(d0, t, sf, COLW)) * 20 for _, _, _, t in row)
        heights.append(26 + shots + 12 + cap + 34)
    sheet = Image.new('RGB', (sheet_w, MARGIN + head + sum(heights) + MARGIN), NAVY)
    d = ImageDraw.Draw(sheet)
    d.text((MARGIN, MARGIN), 'Tarea 214 · antes | ahora · arreglos chicos tras la auditoría de la Tarea 213', font=font(26), fill=SUN)
    intro = ('Tres frases de juegos más cortas, para que la ficha lleve el nombre y UNA línea también en los teléfonos de 360 dp (Ricardo, 2-nov), y «porcentaje» en la hoja del candado (TalkBack leía «qué por ciento»). '
             'Capturas del emulador a tamaño real (1 dp = 1 px); «antes» es el APK de la Tarea 213.')
    for i, line in enumerate(wrap(d, intro, sf, sheet_w - 2 * MARGIN)):
        d.text((MARGIN, MARGIN + 40 + i * 20), line, font=sf, fill=DIM)
    y = MARGIN + head
    for row, hrow in zip(ROWS, heights):
        x = MARGIN
        for key, dp, title, text in row:
            size = 17
            while size > 11 and d.textlength(title, font=font(size)) > COLW: size -= 1
            d.text((x, y), title, font=font(size), fill=SUN)
            im = load(key, dp)
            sheet.paste(im, (x, y + 26))
            d.rectangle((x - 1, y + 25, x + im.width, y + 26 + im.height), outline=LINE)
            cy = y + 26 + im.height + 12
            for i, line in enumerate(wrap(d, text, sf, COLW)):
                d.text((x, cy + i * 20), line, font=sf, fill=DIM)
            x += COLW + GAP
        y += hrow
    sheet.save(OUT)
    print(OUT, sheet.size)


main()
