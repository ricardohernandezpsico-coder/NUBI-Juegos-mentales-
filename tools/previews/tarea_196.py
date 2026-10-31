# -*- coding: utf-8 -*-
"""Lámina «hoy | propuesta» de la Tarea 196 (30-oct-2026): los chips de área de «Todos los juegos» (Ricardo los veía «desordenados»; eligió la variante «B ajustada, con color»). Capturas REALES de Roborazzi (`Tarea196CapturasTest`; `hoy` = el código de antes, `prop` = el nuevo) de la pantalla ENTERA a tamaño real
(1 px = 1 dp): 393 × 873 con letra 1,0 («Todas» y «Lenguaje» elegidas), 360 × 780 con 1,0, y 393 con 1,3 y con 2,0.
Uso:  python -I tools/previews/tarea_196.py      → docs/previews/tarea-196.png
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SHOTS = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots', 't196')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-196.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
NAVY, SUN, DIM, CREAM = (20, 18, 63), (255, 203, 69), (200, 196, 235), (245, 235, 219)
GAP, PAIR_GAP, MARGIN = 12, 40, 24

# (clave de la captura, título, ancho dp, alto dp, qué cambió)
CELLS = [
    ('393_10_todas', '1 · 393 × 873 · letra 1,0 · «Todas» elegida', 393, 873,
     'antes: cinco anchos distintos (de 72 a 132 dp), borde derecho dentado, sin color y la elegida en sol (dos soles con «Nubi sugiere»). Ahora: filas parejas (tres de 115 dp y dos de 176 dp, borde recto a 16 dp), un punto de color de 12 dp en cada área y las cuatro casillas en «Todas»; la elegida en lima, con el visto en el hueco de su color y en negrita. Dos filas en los dos: el bloque mide 104 dp'),
    ('393_10_lenguaje', '2 · 393 × 873 · letra 1,0 · «Lenguaje» elegida', 393, 873,
     'lo mismo con otro chip elegido: la elegida cambia su punto por el visto (lima, visto y negrita); los otros cuatro conservan su color. Elegir no mueve ni cambia de tamaño a ningún chip (probado con los cinco)'),
    ('360_10_todas', '3 · 360 × 780 · letra 1,0 · «Todas» elegida', 360, 780,
     'a 360 dp la parte de tres sería de 104 dp y «Atención» con su hueco pide 112,6 dp: no cabe, así que salen tres filas (2 + 2 + 1, bloque de 157 dp: 53 dp más que antes) y la fila de un solo chip (Lenguaje) toma todo el ancho. Nada se corta'),
    ('393_13_atencion', '4 · 393 × 873 · letra 1,3 · «Atención» elegida', 393, 873,
     'con letra 1,3 ningún chip con su hueco cabe en una parte de tres ni de dos «Razonamiento», así que salen cuatro filas (2 + 1 + 1 + 1, bloque de 209 dp: 105 dp más que antes, con tres chips sueltos a todo el ancho). Nada se corta; antes eran 3 + 2'),
    ('393_20_todas', '5 · 393 × 873 · letra 2,0 · «Todas» elegida', 393, 873,
     'con letra 2,0 los controles se deslizan con la lista (como antes) y los chips quedan en filas sueltas a todo el ancho: nada se corta ni pasa a dos líneas'),
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
    rows = [CELLS[0:3], CELLS[3:5]]
    pair_w = lambda w: 2 * w + GAP
    sheet_w = MARGIN * 2 + sum(pair_w(w) for _, _, w, _, _ in rows[0]) + PAIR_GAP * 2
    heights = []
    for row in rows:
        cap = max(len(wrap(d0, t, sf, pair_w(w) - 4)) * 20 for _, _, w, _, t in row)
        heights.append(26 + 24 + max(h for _, _, _, h, _ in row) + 12 + cap + 28)
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
