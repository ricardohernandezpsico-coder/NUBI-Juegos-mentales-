# -*- coding: utf-8 -*-
"""Lámina de la Tarea 212 (2-nov-2026): «Los emblemas de las cinco divisiones del Contrarreloj» (D3; dirección A «Los cinco lugares» de Fable, que eligió Ricardo).
A TAMAÑO REAL (1 dp = 1 px, 393 dp de ancho), del EMULADOR (`medium_phone`, API 36). Fila 1: HOY (la Tarea 211) frente a la T212 a 393 dp con letra 1,0 (la cifra con su emblema de 52 dp y la escalera con los cinco de 28 dp; con «En la plataforma» la fila lima se apila, como avisó Fable)
y a 360 dp con letra 2,0 (los textos parten por palabras al lado del emblema, que no crece). Fila 2: los cinco emblemas a 3 × (el objeto sobre su ventanilla noche de 52 dp y de 28 dp) y en los tamaños reales.
Los PNG del emulador van a `app/src/test/screenshots/t212/` (fuera de git); los emblemas salen de `app/src/main/res/drawable-nodpi/division_*.webp`.
Uso:  python -I tools/previews/tarea_212.py      → docs/previews/tarea-212.png
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SHOTS = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots', 't212')
RES = os.path.join(ROOT, 'app', 'src', 'main', 'res')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-212.png')
FONTS = os.path.join(RES, 'font')
NAVY, SUN, DIM, LINE, NIGHT, CREAM = (20, 18, 63), (255, 203, 69), (200, 196, 235), (90, 86, 160), (16, 15, 58), (255, 235, 212)
GAP, MARGIN, COLW = 36, 24, 393

ROWS = [
    [('hoy_393_10', 'hoy (T211) · 393 dp · letra 1,0 · emulador',
      'la cifra con el nombre de la división y su lugar, y la escalera con sus cinco divisiones solo en palabras (la fila lima, «En la plataforma · Tú · 46 aciertos», en una línea)'),
     ('t212_393_10', 'T212 · 393 dp · letra 1,0 · emulador',
      'el emblema de 52 dp junto a la división en la cifra y cinco de 28 dp en la escalera, cada uno antes de su nombre; las filas apagadas miden lo mismo (40 dp) y la lima, con el emblema, se apila: el nombre arriba y la marca abajo (+28 dp), como con letra grande'),
     ('hoy_360_20', 'hoy (T211) · 360 dp · letra 2,0 · emulador',
      'la letra más grande en lo más angosto: la cifra y la división parten por palabras'),
     ('t212_360_20', 'T212 · 360 dp · letra 2,0 · emulador',
      'el emblema no crece (52 dp, a la altura de la primera línea) y los textos parten por palabras a su lado; nada se encima ni se corta')],
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


def load(key):
    path = os.path.join(SHOTS, key + '.png')
    if not os.path.exists(path):
        im = Image.new('RGB', (COLW, 873), (40, 36, 100)); ImageDraw.Draw(im).text((20, 400), 'falta ' + key, font=font(20), fill=SUN); return im
    im = Image.open(path).convert('RGB')
    h = int(round(im.height * COLW / im.width))
    return im.resize((COLW, h), Image.LANCZOS)          # tamaño real: 1 dp = 1 px


NOMBRES = [('plataforma', 'En la plataforma'), ('despegando', 'Despegando'), ('orbita', 'En órbita'), ('luna', 'En la Luna'), ('estrellas', 'Entre estrellas')]


def tile(name, dp, scale):
    """El emblema tal como lo dibuja la app: ventanilla noche de [dp] con esquina de 0,23 y la imagen al 86 % del lado, a [scale] px por dp."""
    side = int(round(dp * scale)); obj = int(round(side * 0.86))
    bg = Image.new('RGBA', (side, side), (0, 0, 0, 0))
    mask = Image.new('L', (side * 4, side * 4), 0); ImageDraw.Draw(mask).rounded_rectangle((0, 0, side * 4 - 1, side * 4 - 1), radius=int(side * 4 * 0.23), fill=255)
    win = Image.new('RGBA', (side, side), NIGHT + (255,)); win.putalpha(mask.resize((side, side), Image.LANCZOS))
    bg.alpha_composite(win)
    im = Image.open(os.path.join(RES, 'drawable-nodpi', 'division_%s.webp' % name)).convert('RGBA').resize((obj, obj), Image.LANCZOS)
    bg.alpha_composite(im, ((side - obj) // 2, (side - obj) // 2))
    return bg


def strip(width):
    """Fila 2: los cinco a 3 × en sus dos tamaños, sobre la losa crema, y a tamaño real."""
    h = 330
    im = Image.new('RGB', (width, h), NAVY); d = ImageDraw.Draw(im)
    d.text((0, 0), 'los cinco emblemas · 52 dp y 28 dp a 3 px por dp (arriba) y a tamaño real (abajo) · sobre la losa crema', font=font(17), fill=SUN)
    card = Image.new('RGB', (width, h - 34), CREAM); im.paste(card, (0, 34))
    x0 = 24; step = (width - 48) // 5
    for i, (n, label) in enumerate(NOMBRES):
        x = x0 + i * step
        big = tile(n, 52, 3.0); small = tile(n, 28, 3.0)
        im.paste(big.convert('RGB'), (x, 46), big)
        im.paste(small.convert('RGB'), (x + 160, 46 + 156 - 84), small)
        d.text((x, 46 + 160), label, font=font(17), fill=(36, 31, 90))
        r1 = tile(n, 52, 1.0); r2 = tile(n, 28, 1.0)
        im.paste(r1.convert('RGB'), (x, 46 + 192), r1); im.paste(r2.convert('RGB'), (x + 70, 46 + 192 + 12), r2)
    return im


def main():
    d0 = ImageDraw.Draw(Image.new('RGB', (10, 10)))
    tf, sf = font(17), font(15, bold=False)
    sheet_w = MARGIN * 2 + 4 * COLW + 3 * GAP
    heights = []
    for row in ROWS:
        shots = max(load(k).height for k, _, _ in row)
        cap = max(len(wrap(d0, t, sf, COLW)) * 20 for _, _, t in row)
        heights.append(26 + shots + 12 + cap + 34)
    strip_im = strip(sheet_w - MARGIN * 2)
    sheet = Image.new('RGB', (sheet_w, MARGIN + sum(heights) + strip_im.height + MARGIN), NAVY)
    d = ImageDraw.Draw(sheet)
    y = MARGIN
    for row, hrow in zip(ROWS, heights):
        x = MARGIN
        for key, title, text in row:
            size = 17
            while size > 12 and d.textlength(title, font=font(size)) > COLW: size -= 1          # un título largo no pisa al de la columna de al lado
            d.text((x, y), title, font=font(size), fill=SUN)
            im = load(key)
            sheet.paste(im, (x, y + 26))
            d.rectangle((x - 1, y + 25, x + COLW, y + 26 + im.height), outline=LINE)
            cy = y + 26 + im.height + 12
            for i, line in enumerate(wrap(d, text, sf, COLW)):
                d.text((x, cy + i * 20), line, font=sf, fill=DIM)
            x += COLW + GAP
        y += hrow
    sheet.paste(strip_im, (MARGIN, y))
    sheet.save(OUT)
    print(OUT, sheet.size)


main()
