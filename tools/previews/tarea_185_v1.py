# -*- coding: utf-8 -*-
"""Recorte de la Tarea 185 con los cuatro cuadros de Tú YA corregidos (`docs/previews/tarea-185-v1.png`), con CAPTURAS REALES.
Ricardo eligió el 28-oct la variante V1 «Centrado» (la otra se borró) y pidió tres cosas que aquí se ven: los cuatro cuadros de la misma altura con su
contenido centrado también en altura (el de medallas, sin hueco abajo), y el número NUNCA separado de su unidad («21 días», «8 de octubre», «de 15 medallas»).
Izquierda: letra 1,0. Derecha: letra 1,3 (en un marco más alto, para que la barra de pestañas no tape los cuadros de abajo).
`docs/previews/tarea-185.png` queda como registro de la elección V1 | V2 (se grabó antes de borrar V2).
Entradas (fuera de git): `app/src/test/screenshots/tarea-185/propuesta/{tu-1_0,tu-1_3-alta}.png` (`Tarea185CapturasTest`, solo al grabar).
Uso:  python -I tools/previews/tarea_185_v1.py
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
PROP = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots', 'tarea-185', 'propuesta')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-185-v1.png')
NAVY, SUN, DIM = (20, 18, 63), (255, 203, 69), (200, 196, 235)
GAP = 36
DENSITY = 2.75                                  # px por dp de las capturas de Roborazzi (440 dpi)
COL_W = 1080


def font(px, rubik=False):
    return ImageFont.truetype(os.path.join(FONTS, 'rubik_bold.ttf' if rubik else 'fredoka.ttf'), int(px))


def crop_dp(path, top_dp, bottom_dp):
    im = Image.open(path).convert('RGB')
    return im.crop((0, round(top_dp * DENSITY), im.width, min(im.height, round(bottom_dp * DENSITY))))


def main():
    left = crop_dp(os.path.join(PROP, 'tu-1_0.png'), 265, 700)
    right = crop_dp(os.path.join(PROP, 'tu-1_3-alta.png'), 265, 745)
    w = 2 * COL_W + 3 * GAP
    head = 150; foot = 170
    h = head + max(left.height, right.height) + foot
    sheet = Image.new('RGB', (w, h), NAVY)
    d = ImageDraw.Draw(sheet)
    d.text((GAP, 30), 'Los cuatro cuadros de Tú, versión elegida (V1 «Centrado»)', font=font(46, rubik=True), fill=SUN)
    d.text((GAP, 92), 'Todos de la misma altura, con el contenido centrado y el número siempre pegado a su unidad.', font=font(34), fill=DIM)
    for i, (im, label) in enumerate([(left, 'Letra 1,0'), (right, 'Letra 1,3')]):
        x = GAP + i * (COL_W + GAP)
        sheet.paste(im, (x, head))
        d.text((x, head + max(left.height, right.height) + 28), label, font=font(40), fill=SUN)
    d.text((GAP, h - 70), 'Capturas reales (Roborazzi, 393 dp de ancho). El número no se separa de su unidad: «21 días», «8 de octubre», «15 medallas» llevan espacio duro.', font=font(30), fill=DIM)
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    sheet.save(OUT, optimize=True)
    print('ok', OUT, sheet.size)


main()
