# -*- coding: utf-8 -*-
"""Lámina «hoy | propuesta» de la Tarea 198 (31-oct-2026): «¡Ruta de hoy completa!». Capturas REALES de Roborazzi (`Tarea198CapturasTest`; `hoy` = la pantalla de antes, `prop` = la nueva: UNA losa crema y la semana de Hoy debajo) de la pantalla ENTERA a tamaño real (1 px = 1 dp), sobre el cielo de la app y con
los datos de Ricardo (Bodega · Freno · Aterrizaje, racha de 1 día; Aterrizaje no sumó puntos): 393 × 873 con letra 1,0, 1,3 y 2,0; 360 × 780 con 1,0; y 393 con 1,0 con Nubi en lo alto de su saltito.
Uso:  python -I tools/previews/tarea_198.py      → docs/previews/tarea-198.png
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SHOTS = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots', 't198')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-198.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
NAVY, SUN, DIM, CREAM = (20, 18, 63), (255, 203, 69), (200, 196, 235), (245, 235, 219)
GAP, PAIR_GAP, MARGIN = 12, 40, 24

# (captura, título, ancho dp, alto dp, qué cambió)
ROW1 = [
    ('393_10', '1 · 393 × 873 · letra 1,0', 393, 873,
     'todo cabe sin deslizar: el contenido termina en 679 dp de 759 dp visibles (80 dp de aire). Nubi con 20 dp de aire arriba; el título y la frase sobre el cielo con su orilla (ya no se ven los luceros entre las letras); UNA losa crema de 475 dp con «Tus juegos» y «Tu avance» (en tinta, caja normal, sin mayúsculas); '
     'cada número UNA vez; la barra de los cuadros de Avance (sin brillo) con lo de hoy a media tinta; la semana de Hoy con su racha, debajo de la losa; el único sol es «Volver a Hoy». Aterrizaje no sumó puntos y dice «Te mantuviste en Razonamiento», en paralelo con «12 puntos en Atención». Sin desvanecido: todo cabe'),
    ('393_13', '2 · 393 × 873 · letra 1,3', 393, 873,
     'la losa mide 572 dp; el nombre y los puntos de «Razonamiento» no caben en una línea y quedan uno bajo el otro; la semana se alcanza deslizando y el pie («Nubi te espera…» y «Volver a Hoy») queda fijo. El borde de abajo del área que se desliza se DESVANECE (24 dp, hacia el cielo) mientras queda algo por ver, en vez de cortarse en seco, y hay 16 dp hasta la frase del pie (antes unos 12 dp)'),
    ('393_20', '3 · 393 × 873 · letra 2,0', 393, 873,
     'los nombres de los juegos pasan a dos líneas por palabras (ninguna se parte); la losa mide 924 dp y se desliza; el pie queda fijo. El borde de abajo se desvanece en lugar de partir un texto a la mitad (en «hoy» «Hoy te mantuviste» quedaba cortado contra el pie) y hay 16 dp hasta el pie. Una prueba mide cada caja con letra 1,0, 1,3 y 2,0 (4 dp o más entre las cajas; nada se pisa ni se corta)'),
]
ROW2 = [
    ('360_10', '4 · 360 × 780 · letra 1,0', 360, 780,
     'a 360 dp la losa mide lo mismo (475 dp); el contenido pasa un poco del área visible y la semana se alcanza deslizando; el borde de abajo se desvanece mientras queda algo por ver y hay 16 dp hasta el pie, que queda fijo'),
    ('393_10_salto', '5 · 393 × 873 · letra 1,0 · Nubi en lo alto del saltito', 393, 873,
     'antes, 8 dp de aire y un salto de 12 dp dejaban el borde de arriba de Nubi 4 dp fuera del área que se desliza (se cortaba: es lo que se ve en la captura de Ricardo); ahora Nubi queda 5 dp DENTRO en lo más alto del salto. Lo mide una prueba que compone con el reloj en lo alto del salto'),
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


def main():
    d0 = ImageDraw.Draw(Image.new('RGB', (10, 10)))
    tf, sf = font(17), font(15, bold=False)
    pair_w = lambda w: 2 * w + GAP
    rows = [ROW1, ROW2]
    sheet_w = MARGIN * 2 + sum(pair_w(w) for _, _, w, _, _ in ROW1) + PAIR_GAP * (len(ROW1) - 1)
    heights = []
    for row in rows:
        cap = max(len(wrap(d0, t, sf, pair_w(w) - 4)) * 20 for _, _, w, _, t in row)
        heights.append(26 + 24 + max(h for _, _, _, h, _ in row) + 12 + cap + 30)
    sheet = Image.new('RGB', (sheet_w, MARGIN + sum(heights)), NAVY)
    d = ImageDraw.Draw(sheet)
    y = MARGIN
    for row, hrow in zip(rows, heights):
        x0 = MARGIN
        for key, title, w, h, text in row:
            d.text((x0, y), title, font=tf, fill=SUN)
            for j, (pre, label) in enumerate((('hoy', 'HOY'), ('prop', 'PROPUESTA'))):
                x = x0 + j * (w + GAP)
                d.text((x, y + 26), label, font=font(15), fill=CREAM if pre == 'prop' else DIM)
                im = Image.open(os.path.join(SHOTS, '%s_%s.png' % (pre, key))).convert('RGB')
                sheet.paste(im.resize((w, h), Image.LANCZOS), (x, y + 50))
                d.rectangle((x - 1, y + 49, x + w, y + 50 + h), outline=(90, 86, 160))
            cy = y + 50 + max(hh for _, _, _, hh, _ in row) + 12
            for i, line in enumerate(wrap(d, text, sf, pair_w(w) - 4)):
                d.text((x0, cy + i * 20), line, font=sf, fill=DIM)
            x0 += pair_w(w) + PAIR_GAP
        y += hrow
    sheet.save(OUT)
    print(OUT, sheet.size)


main()
