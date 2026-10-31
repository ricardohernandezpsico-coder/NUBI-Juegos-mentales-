# -*- coding: utf-8 -*-
"""Lámina «hoy | propuesta» de la Tarea 195 (30-oct-2026): las siete franjas de edad en el Primer vuelo y en Ajustes. Capturas REALES de Roborazzi (`Tarea195CapturasTest`; `hoy` = el código de antes, con las tres bandas; `prop` = el nuevo, con las siete franjas)
a 393 × 873 dp y tamaño real (1 px = 1 dp): la pregunta del vuelo con letra 1,0 y 1,3, y la sección «Rango de edad» de Ajustes con letra 1,0.
Uso:  python -I tools/previews/tarea_195.py      → docs/previews/tarea-195.png
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SHOTS = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots', 't195')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-195.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
NAVY, SUN, DIM, CREAM = (20, 18, 63), (255, 203, 69), (200, 196, 235), (245, 235, 219)
W, H, GAP, PAIR_GAP, MARGIN = 393, 873, 12, 40, 24

# (clave de la captura, título del par, qué cambió)
COLS = [
    ('edad_10', '1 · la pregunta del vuelo · letra 1,0',
     'siete opciones («13 a 17 años» … «75 años o más») en lugar de tres, de 52 dp cada una (el piso de toque es 48 dp), con la misma pregunta y la misma línea de ayuda. Con letra 1,0 todo cabe sin deslizar: la última opción termina en 678 dp, la ayuda termina en 773 dp, 16 dp antes del borde del área que se desliza (789 dp), y «Continuar» está a la vista. Sin elegir, «Continuar» sigue atenuado'),
    ('edad_13', '1 · la pregunta del vuelo · letra 1,3',
     'con letra 1,3 la página se desliza: en la captura, sin deslizar todavía, el borde del área corta la última línea de la ayuda; al deslizar se llega a ella y nada queda tapado por «Continuar» (lo mide una prueba, también con letra 2,0 y a 360 × 640)'),
    ('ajustes_10', '2 · Ajustes · «Rango de edad» · letra 1,0',
     'la misma lista de siete en Ajustes (2 por fila; aquí, una persona que ya eligió «30 a 44 años»). Quien solo tenía la banda de tres de antes no ve ninguna marcada hasta que elija. Al elegir se guardan las dos cosas: la franja y la banda del DDA que sale de ella; sin ventanas ni avisos nuevos'),
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
    pair_w = 2 * W + GAP
    sheet_w = MARGIN * 2 + len(COLS) * pair_w + (len(COLS) - 1) * PAIR_GAP
    cap_h = max(len(wrap(d0, t, sf, pair_w - 4)) * 20 for _, _, t in COLS)
    sheet_h = MARGIN + 26 + 24 + H + 12 + cap_h + 24
    sheet = Image.new('RGB', (sheet_w, sheet_h), NAVY)
    d = ImageDraw.Draw(sheet)
    for k, (key, title, text) in enumerate(COLS):
        x0 = MARGIN + k * (pair_w + PAIR_GAP)
        y = MARGIN
        d.text((x0, y), title, font=tf, fill=SUN)
        for j, (pre, label) in enumerate((('hoy', 'HOY'), ('prop', 'PROPUESTA'))):
            x = x0 + j * (W + GAP)
            d.text((x, y + 26), label, font=font(15), fill=CREAM if pre == 'prop' else DIM)
            im = Image.open(os.path.join(SHOTS, '%s_%s.png' % (pre, key))).convert('RGB')
            sheet.paste(im.resize((W, H), Image.LANCZOS), (x, y + 50))
            d.rectangle((x - 1, y + 49, x + W, y + 50 + H), outline=(90, 86, 160))
        cy = y + 50 + H + 12
        for i, line in enumerate(wrap(d, text, sf, pair_w - 4)):
            d.text((x0, cy + i * 20), line, font=sf, fill=DIM)
    sheet.save(OUT)
    print(OUT, sheet.size)


main()
