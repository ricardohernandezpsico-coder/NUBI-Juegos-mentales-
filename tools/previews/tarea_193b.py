# -*- coding: utf-8 -*-
"""Lámina de la Tarea 193b (30-oct-2026): la guía de Nubi también sale cuando el Primer vuelo termina SIN jugar ningún juego («Terminar después» en todo). Capturas REALES de Roborazzi (`HoyGuideScreenshotTest`, las `cero_*`) a 393 × 873 dp y tamaño real (1 px = 1 dp), con letra 1,0 y 1,3: el Hoy de verdad
en ese estado (la ruta de hoy normal sin empezar, con el sol en la tarjeta y el Contrarreloj en lila; el enlace «Encuentra tu punto de partida» bajo los botones), la barra real y el velo.
Uso:  python -I tools/previews/tarea_193b.py      → docs/previews/tarea-193b.png
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SHOTS = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots', 'guia')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-193b.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
NAVY, SUN, DIM, CREAM = (20, 18, 63), (255, 203, 69), (200, 196, 235), (245, 235, 219)
W, H, GAP, MARGIN = 393, 873, 14, 24


def font(px, bold=True): return ImageFont.truetype(os.path.join(FONTS, 'fredoka.ttf' if bold else 'nunito_regular.ttf'), int(px))


def wrap(draw, text, f, width):
    lines, cur = [], ''
    for word in text.split(' '):
        t = (cur + ' ' + word).strip()
        if draw.textlength(t, font=f) <= width: cur = t
        else: lines.append(cur); cur = word
    if cur: lines.append(cur)
    return lines


SECTIONS = [
    ('LA GUÍA CUANDO EL VUELO TERMINÓ SIN JUGAR NINGÚN JUEGO · letra 1,0', [
        ('cero_paso1_10', 'Paso 1 de 3', 'la ruta de hoy sigue normal (el sol está en la tarjeta); el Contrarreloj va en lila y la guía no suma ningún sol'),
        ('cero_paso2_10', 'Paso 2 de 3', 'el hueco sobre «Todos los juegos»; bajo él, el enlace «Encuentra tu punto de partida» queda libre'),
        ('cero_paso3_10', 'Paso 3 de 3', 'sin partidas, Avance no muestra un punto de partida: «En Avance ves cómo vas en cada área»'),
        ('cero_despues_10', 'Después', 'Hoy como estaba: la ruta de hoy para jugarla, sin medallas'),
    ]),
    ('LA MISMA GUÍA · letra 1,3 (Nubi pasa a S; Hoy se desliza y trae el blanco entero sobre la barra)', [
        ('cero_paso1_13', 'Paso 1 de 3', ''),
        ('cero_paso2_13', 'Paso 2 de 3', 'el botón entero y fuera de la barra; el enlace del punto de partida queda debajo, bajo el velo'),
        ('cero_paso3_13', 'Paso 3 de 3', ''),
        ('cero_despues_13', 'Después', 'Hoy sin guía'),
    ]),
]


def main():
    d0 = ImageDraw.Draw(Image.new('RGB', (10, 10)))
    cf, sf = font(17), font(15, bold=False)
    per_row = max(len(items) for _, items in SECTIONS)
    sheet_w = MARGIN * 2 + per_row * W + (per_row - 1) * GAP
    heights = []
    for title, items in SECTIONS:
        cap_h = max(26 + len(wrap(d0, t, sf, W - 4)) * 20 for _, _, t in items)
        heights.append(44 + H + 10 + cap_h + 34)
    sheet = Image.new('RGB', (sheet_w, MARGIN + sum(heights) + 10), NAVY)
    d = ImageDraw.Draw(sheet)
    y = MARGIN
    for (title, items), hrow in zip(SECTIONS, heights):
        d.text((MARGIN, y), title, font=cf, fill=SUN)
        y0 = y + 44
        for k, (n, cap, text) in enumerate(items):
            x = MARGIN + k * (W + GAP)
            im = Image.open(os.path.join(SHOTS, n + '.png')).convert('RGB')
            sheet.paste(im.resize((W, H), Image.LANCZOS), (x, y0))
            d.rectangle((x - 1, y0 - 1, x + W, y0 + H), outline=(90, 86, 160))
            d.text((x, y0 + H + 8), cap, font=font(17), fill=CREAM)
            for i, line in enumerate(wrap(d, text, sf, W - 4)):
                d.text((x, y0 + H + 34 + i * 20), line, font=sf, fill=DIM)
        y += hrow
    sheet.save(OUT)
    print(OUT, sheet.size)


main()
