# -*- coding: utf-8 -*-
"""Hoja «La ficha con una decisión visible» (Tarea 140, A3 de `auditoria-fable/spec-adopcion-A1-A7.md`): lo que de verdad dibuja la app, no una maqueta. Junta las capturas REALES de Roborazzi
(`:app:recordRoborazziDebug --tests "com.example.GameSheetFitScreenshotTest"`, que dejan los PNG en `app/src/test/screenshots/`) de la ficha de Rescate relámpago a 393 × 873 dp:
HOY (la ficha de «A · Compacta» con todo a la vista; `ficha-hoy-*.png` se grabaron ANTES del cambio y se guardan con ese nombre), PLEGADA (por defecto), DESPLEGADA («Listo») y PLEGADA con la letra del sistema al 130 %.
Uso:  python -I tools/previews/ficha_una_decision.py      → docs/previews/ficha-una-decision.png
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SHOTS = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots'); OUT = os.path.join(ROOT, 'docs', 'previews', 'ficha-una-decision.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
NAVY, SUN, DIM, CREAM = (20, 18, 63), (255, 203, 69), (200, 196, 235), (245, 235, 219)
PH = 1500                                                   # alto de cada panel en la hoja
GAP = 30


def font(px, bold=True): return ImageFont.truetype(os.path.join(FONTS, 'fredoka.ttf' if bold else 'nunito_regular.ttf'), int(px))


PANELS = [
    ('ficha-hoy-rescate-393x873.png', 'HOY', 'todo a la vista, pegada abajo'),
    ('ficha-rescate-393x873.png', 'PLEGADA (por defecto)', 'más arriba: centrada en el espacio libre'),
    ('ficha-rescate-393x873-desplegada.png', 'DESPLEGADA', 'crece arriba y abajo; «Listo» la pliega'),
    ('ficha-rescate-393x873-letra130.png', 'PLEGADA con letra al 130 %', '«Jugar» en dos líneas, cada una entera'),
]
ims = []
for f, _, _ in PANELS:
    im = Image.open(os.path.join(SHOTS, f)).convert('RGB'); k = PH / im.height; ims.append(im.resize((round(im.width * k), PH), Image.LANCZOS))
W = GAP + sum(i.width + GAP for i in ims); H = 150 + PH + GAP + 150
sheet = Image.new('RGB', (W, H), NAVY); d = ImageDraw.Draw(sheet)
d.text((GAP, 24), 'La ficha del juego con UNA decisión visible (Tarea 140)', font=font(52), fill=SUN)
d.text((GAP, 92), 'Capturas reales de Roborazzi a 393 × 873 dp (Rescate relámpago, A tu medida · con reloj). Plegada por defecto y CENTRADA (Ricardo, 19-oct: «un poco más arriba»): lo elegido en una línea, «Cambiar cómo jugar» y «Jugar · …»; desplegada es la ficha de hoy y crece hacia arriba y hacia abajo.', font=font(26, False), fill=DIM)
x = GAP
for (f, t, s), im in zip(PANELS, ims):
    sheet.paste(im, (x, 150))
    d.text((x, 150 + PH + 10), t, font=font(34), fill=CREAM); d.text((x, 150 + PH + 54), s, font=font(24, False), fill=DIM)
    x += im.width + GAP
d.text((GAP, 150 + PH + 100), 'El cambio de tamaño dura 200 ms (NubiMotion; de un salto con «quitar animaciones»). El modo de cada juego se recuerda (el resumen ya es su elección); la ventana recuerda «desplegada» solo mientras está abierta. Lector de pantalla: botón con lo elegido, estado «opciones ocultas / visibles» y foco en la casilla elegida.', font=font(24, False), fill=DIM)
sheet.save(OUT, optimize=True); print('hoja:', OUT, sheet.size, os.path.getsize(OUT) // 1024, 'KB')
