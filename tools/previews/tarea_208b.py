# -*- coding: utf-8 -*-
"""Lámina de la Tarea 208b (1-nov-2026): el final del Contrarreloj de INVITADO trae la regla de Rescate relámpago.
A TAMAÑO REAL (1 dp = 1 px, 393 dp de ancho). Fila única: el final de invitado de Rescate con letra 1,0 y con letra 2,0 (la frase a 16 sp, justo después de «cómo se cuenta», sin partir palabras) y, para comparar, el de Tinta o Palabra, que NO lleva la frase.
Las tres salen de `Tarea208bCapturasTest` (Roborazzi); todo en `app/src/test/screenshots/t208b/` (fuera de git).
Uso:  python -I tools/previews/tarea_208b.py      → docs/previews/tarea-208b.png
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SHOTS = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots', 't208b')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-208b.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
NAVY, SUN, DIM, LINE = (20, 18, 63), (255, 203, 69), (200, 196, 235), (90, 86, 160)
GAP, MARGIN, COLW = 36, 24, 393

# (captura, título, qué se ve); todas de 393 x 873 dp
ROW = [
    ('ahora_rescate_393_10', 'Rescate relámpago · letra 1,0',
     '«16 aciertos menos 2 errores, en 2 minutos de Contrarreloj.» y, justo después, la regla de Rescate (16 sp): «En Rescate relámpago, cada cápsula bien marcada es un acierto y cada cápsula marcada que no estaba, un error.» Luego la edad elegida y el aviso de que no se guardó'),
    ('ahora_rescate_393_20', 'Rescate relámpago · letra 2,0',
     'con letra 2,0 la frase sigue entera y en su sitio, sin partir ninguna palabra; la pantalla se desliza para llegar a los botones'),
    ('ahora_tinta_393_10', 'Tinta o Palabra · letra 1,0 (sin la frase)',
     'otro juego, el mismo final de siempre: «cómo se cuenta», la edad y el aviso; la regla de cápsulas es solo de Rescate relámpago'),
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
    im = Image.open(os.path.join(SHOTS, key + '.png')).convert('RGB')
    h = int(round(im.height * COLW / im.width))
    return im.resize((COLW, h), Image.LANCZOS)          # tamaño real: 1 dp = 1 px


def main():
    d0 = ImageDraw.Draw(Image.new('RGB', (10, 10)))
    tf, sf = font(17), font(15, bold=False)
    sheet_w = MARGIN * 2 + len(ROW) * COLW + (len(ROW) - 1) * GAP
    shots = max(load(k).height for k, _, _ in ROW)
    cap = max(len(wrap(d0, t, sf, COLW)) * 20 for _, _, t in ROW)
    sheet = Image.new('RGB', (sheet_w, MARGIN + 26 + shots + 12 + cap + 34), NAVY)
    d = ImageDraw.Draw(sheet)
    x, y = MARGIN, MARGIN
    for key, title, text in ROW:
        d.text((x, y), title, font=tf, fill=SUN)
        im = load(key)
        sheet.paste(im, (x, y + 26))
        d.rectangle((x - 1, y + 25, x + COLW, y + 26 + im.height), outline=LINE)
        cy = y + 26 + im.height + 12
        for i, line in enumerate(wrap(d, text, sf, COLW)):
            d.text((x, cy + i * 20), line, font=sf, fill=DIM)
        x += COLW + GAP
    sheet.save(OUT)
    print(OUT, sheet.size)


main()
