# -*- coding: utf-8 -*-
"""Hoja «Tu isla crece» (Tarea 136, A1.3 de `auditoria-fable/spec-adopcion-A1-A7.md`): lo que de verdad dibuja la app, no una maqueta. Junta las capturas REALES de Roborazzi
(`:app:recordRoborazziDebug --tests "com.example.HoyIslaScreenshotTest" --tests "com.example.AchievementOverlayScreenshotTest"`, que dejan los PNG en `app/src/test/screenshots/`):
Hoy a 393 × 873 dp con 0 piezas, con 3 (arbustos, banderines y muelle: las primeras) y con las 10, y la celebración de la medalla «Una semana» con la línea nueva «Y en tu isla aparece: dos arbustos».
Uso:  python -I tools/previews/isla_crece.py      → docs/previews/isla-crece.png
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SHOTS = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots'); OUT = os.path.join(ROOT, 'docs', 'previews', 'isla-crece.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
NAVY, SUN, DIM, CREAM = (20, 18, 63), (255, 203, 69), (200, 196, 235), (245, 235, 219)
PH = 1500                                                   # alto de cada panel en la hoja
GAP = 30


def font(px, bold=True): return ImageFont.truetype(os.path.join(FONTS, 'fredoka.ttf' if bold else 'nunito_regular.ttf'), int(px))


PANELS = [
    ('hoy-isla-393-piezas-0.png', 'Hoy · sin piezas', 'así empieza la isla'),
    ('hoy-isla-393-piezas-3.png', 'Con 3 piezas', 'dos arbustos · una cuerda de banderines · el muelle'),
    ('hoy-isla-393-piezas-10.png', 'Con las 10 piezas', 'jardín, banderines y muelle con su nave'),
    ('logro-con-pieza.png', 'Al ganar una medalla con pieza', '«Una semana»: una línea, la pieza y el sonido Nuevo'),
]
ims = []
for f, _, _ in PANELS:
    im = Image.open(os.path.join(SHOTS, f)).convert('RGB'); k = PH / im.height; ims.append(im.resize((round(im.width * k), PH), Image.LANCZOS))
W = GAP + sum(i.width + GAP for i in ims); H = 150 + PH + GAP + 150
sheet = Image.new('RGB', (W, H), NAVY); d = ImageDraw.Draw(sheet)
d.text((GAP, 24), 'Tu isla crece: las 10 piezas por medallas, en la app (Tarea 136)', font=font(52), fill=SUN)
d.text((GAP, 92), 'Capturas reales de Roborazzi a 393 × 873 dp (la última, la celebración de la medalla). Las piezas se DERIVAN de las medallas de conducta: Constancia (jardín), Variedad (banderines) y Volver (muelle); nunca se quitan.', font=font(26, False), fill=DIM)
x = GAP
for (f, t, s), im in zip(PANELS, ims):
    sheet.paste(im, (x, 150))
    d.text((x, 150 + PH + 10), t, font=font(34), fill=CREAM); d.text((x, 150 + PH + 54), s, font=font(24, False), fill=DIM)
    x += im.width + GAP
d.text((GAP, 150 + PH + 100), 'Hoy no vuelve a festejar: la pieza nueva entra en Hoy una sola vez (600 ms, de a una), sin sonido ni vibración; con «quitar animaciones» nace puesta. Quien ya tenía medallas ve su isla con las piezas puestas.', font=font(24, False), fill=DIM)
sheet.save(OUT, optimize=True); print('hoja:', OUT, sheet.size, os.path.getsize(OUT) // 1024, 'KB')
