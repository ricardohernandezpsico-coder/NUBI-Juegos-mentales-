# -*- coding: utf-8 -*-
"""Lámina de la Tarea 206 (1-nov-2026): la escala de la referencia estimada calibrada con la primera semana de las 5 personas de Ricardo (Tinta 0,89 · ¿Verdad o disparate? 0,92 · Acoplamiento 0,90) y los 20 tramos de Tinta y de Acoplamiento.
La página «Contrarreloj» de Tinta o Palabra y de Acoplamiento en la franja de 30 a 44 años, a TAMAÑO REAL (sin reducir): la escalera de cinco peldaños y la campana con su eje («95 o más», «38 o más») con letra 1,0 y 2,0, y la campana a 360 dp (el ancho más angosto).
Las capturas salen de `Tarea206CapturasTest` (Roborazzi; la página no es un Dialog, así que la letra 2,0 sí se aplica) en `app/src/test/screenshots/t206/`. Uso:  python -I tools/previews/tarea_206.py      → docs/previews/tarea-206.png
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SHOTS = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots', 't206')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-206.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
NAVY, SUN, DIM, LINE = (20, 18, 63), (255, 203, 69), (200, 196, 235), (90, 86, 160)
GAP, MARGIN, COLW = 36, 24, 393

# (captura, ancho dp, alto recortado dp o None, título, qué se ve); cada fila trae hasta 4 columnas
ROWS = [
    [('ahora_stroop_ladder_393_10', 393, 873, 'Tinta o Palabra · escalera · letra 1,0',
      '30 a 44 años, marca 58 (la de P1): «Despegando», percentil 25. Los CINCO peldaños, «Entre estrellas» desde 78 aciertos (con 16 tramos no tenía peldaño)'),
     ('ahora_stroop_bell_393_10', 393, 873, 'Tinta o Palabra · campana · letra 1,0',
      '20 tramos de a 5: el eje termina en «95 o más»; el último tramo junta 0,3 % de los robots, así que el percentil 90 se alcanza con margen'),
     ('ahora_stroop_ladder_393_20', 393, 873, 'Tinta o Palabra · escalera · letra 2,0',
      'la escalera conserva sus cinco peldaños con letra 2,0'),
     ('ahora_stroop_bell_393_20', 393, 873, 'Tinta o Palabra · campana · letra 2,0',
      'con letra 2,0 el dibujo omite el rótulo del medio («32») porque no cabe y quedan 0 · 63 · 95 o más: nada choca')],
    [('ahora_acop_ladder_393_10', 393, 873, 'Acoplamiento · escalera · letra 1,0',
      '30 a 44 años, marca 24 (la de P1): «Despegando», percentil 42. Los CINCO peldaños; «Entre estrellas» desde 31 aciertos'),
     ('ahora_acop_bell_393_10', 393, 873, 'Acoplamiento · campana · letra 1,0',
      '20 tramos de a 2: el eje termina en «38 o más»; el último tramo ya no junta a nadie'),
     ('ahora_acop_ladder_393_20', 393, 873, 'Acoplamiento · escalera · letra 2,0',
      'cinco peldaños con letra 2,0'),
     ('ahora_acop_bell_393_20', 393, 873, 'Acoplamiento · campana · letra 2,0',
      'con letra 2,0 se omite el «13» y queda 0 · 25 · 38 o más; nada choca')],
    [('ahora_stroop_bell_360_10', 360, 780, 'Tinta o Palabra · campana · 360 dp',
      'el ancho más angosto: los cuatro rótulos del eje (0 · 32 · 63 · 95 o más) con letra 1,0'),
     ('ahora_acop_bell_360_10', 360, 780, 'Acoplamiento · campana · 360 dp',
      'el eje (0 · 13 · 25 · 38 o más) a 360 dp')],
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


def load(key, w, crop_h):
    im = Image.open(os.path.join(SHOTS, key + '.png')).convert('RGB')
    h = int(round(im.height * w / im.width))
    im = im.resize((w, h), Image.LANCZOS)          # tamaño real: 1 dp = 1 px
    return im.crop((0, 0, w, min(h, crop_h))) if crop_h else im


def main():
    d0 = ImageDraw.Draw(Image.new('RGB', (10, 10)))
    tf, sf = font(17), font(15, bold=False)
    sheet_w = MARGIN * 2 + 4 * COLW + 3 * GAP
    heights = []
    for row in ROWS:
        shots = max(load(k, w, c).height for k, w, c, _, _ in row)
        cap = max(len(wrap(d0, t, sf, COLW)) * 20 for _, _, _, _, t in row)
        heights.append(26 + shots + 12 + cap + 34)
    sheet = Image.new('RGB', (sheet_w, MARGIN + sum(heights)), NAVY)
    d = ImageDraw.Draw(sheet)
    y = MARGIN
    for row, hrow in zip(ROWS, heights):
        x = MARGIN
        for key, w, crop, title, text in row:
            d.text((x, y), title, font=tf, fill=SUN)
            im = load(key, w, crop)
            sheet.paste(im, (x, y + 26))
            d.rectangle((x - 1, y + 25, x + w, y + 26 + im.height), outline=LINE)
            cy = y + 26 + im.height + 12
            for i, line in enumerate(wrap(d, text, sf, COLW)):
                d.text((x, cy + i * 20), line, font=sf, fill=DIM)
            x += COLW + GAP
        y += hrow
    sheet.save(OUT)
    print(OUT, sheet.size)


main()
