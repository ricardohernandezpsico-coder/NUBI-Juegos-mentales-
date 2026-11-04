# -*- coding: utf-8 -*-
"""Captura de la Tarea 227 (4-nov-2026): Hoy y Avance con una persona que esta semana solo jugó Contrarreloj. Antes, Hoy decía «2 de 4 días de tu meta» y Avance «Tu semana empieza hoy» con las siete llamitas apagadas; después, las dos dicen lo mismo.
Las capturas son REALES del emulador (`tools/capturar_227.py`, 393 dp, letra 1,0) y están en `unity/test-results/t227/` (fuera de git: `antes_Hoy.png`, `antes_Avance.png`, `despues_Hoy.png`, `despues_Avance.png`).
Uso:  python -I tools/previews/tarea_227.py      → docs/previews/tarea-227.png
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SHOTS = os.path.join(ROOT, 'unity', 'test-results', 't227')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-227.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
NAVY, SUN, DIM, LINE, WHITE = (20, 18, 63), (255, 203, 69), (200, 196, 235), (90, 86, 160), (240, 244, 255)
CROP_DP, W_DP, GAP, MARGIN = 245, 393, 24, 24                      # 1 dp = 1 px (393 dp de ancho; las cabeceras, recortadas a 245 dp de alto)


def font(px, bold=True): return ImageFont.truetype(os.path.join(FONTS, 'fredoka.ttf' if bold else 'nunito_regular.ttf'), int(px))


def crop(name):
    im = Image.open(os.path.join(SHOTS, name + '.png')).convert('RGB'); k = im.width / float(W_DP)
    c = im.crop((0, 0, im.width, int(CROP_DP * k)))
    return c.resize((W_DP, CROP_DP), Image.LANCZOS)


def main():
    sf = font(15, bold=False)
    w = MARGIN * 2 + 2 * W_DP + GAP
    h = MARGIN + 78 + 2 * (24 + CROP_DP + 34) + 150 + MARGIN
    sheet = Image.new('RGB', (w, h), NAVY); d = ImageDraw.Draw(sheet)
    d.text((MARGIN, MARGIN), 'Tarea 227 · Avance cuenta la semana igual que Hoy', font=font(26), fill=SUN)
    d.text((MARGIN, MARGIN + 38), 'Capturas REALES del emulador (393 dp, letra 1,0): una persona que esta semana solo jugó Contrarreloj (lunes y martes).', font=sf, fill=DIM)
    y = MARGIN + 78
    rows = [('ANTES · Tarea 225b: Hoy dice «2 de 4 días» con dos llamitas; Avance, «Tu semana empieza hoy»', 'antes'),
            ('DESPUÉS · Tarea 227: las dos pantallas dicen lo mismo', 'despues')]
    for title, label in rows:
        d.text((MARGIN, y), title, font=font(15), fill=WHITE); y += 24
        for i, tab in enumerate(('Hoy', 'Avance')):
            im = crop('%s_%s' % (label, tab)); x = MARGIN + i * (W_DP + GAP)
            sheet.paste(im, (x, y)); d.rectangle((x - 1, y - 1, x + im.width, y + im.height), outline=LINE)
        y += CROP_DP + 34
    notes = ('Causa: la línea de la semana de Avance se armaba con `history` (las partidas de siempre), que no trae el Contrarreloj; Hoy usa `activityTimestamps` (las partidas y el Contrarreloj). Ahora las dos leen `activityTimestamps`, y «Tu semana empieza hoy» solo sale si no se jugó NADA. '
             'Además, Avance renueva su «ahora» al cambiar el día (con el reloj de 30 s de Hoy) y las medallas de constancia y «Tu mejor racha» de Tú cuentan los días de Contrarreloj como la racha de Hoy; el recordatorio también.')
    cur = ''; yy = y
    for word in notes.split(' '):
        t = (cur + ' ' + word).strip()
        if d.textlength(t, font=sf) <= w - 2 * MARGIN: cur = t
        else: d.text((MARGIN, yy), cur, font=sf, fill=WHITE); yy += 20; cur = word
    d.text((MARGIN, yy), cur, font=sf, fill=WHITE)
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    sheet.crop((0, 0, w, yy + 20 + MARGIN)).save(OUT, optimize=True)
    print('listo', OUT)


main()
