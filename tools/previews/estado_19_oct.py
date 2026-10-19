# -*- coding: utf-8 -*-
"""Lámina índice «El estado de Nubi al 19-oct» (Tarea 141): una VISTA GENERAL para la revisión final del pulido (la hace Fable). Una fila con lo más representativo de la APP (capturas REALES de Roborazzi, `AppActualScreenshotTest`,
ya copiadas a `docs/previews/app-actual/` por `tools/previews/app_actual.py`) y una fila de 6 JUEGOS (capturas REALES de Unity, `bash tools/verificar-todo.sh --capturas todos`, de `unity/test-results/capturas/`).
Uso:  python -I tools/previews/estado_19_oct.py      → docs/previews/estado-19-oct.png
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
APP = os.path.join(ROOT, 'docs', 'previews', 'app-actual'); GAMES = os.path.join(ROOT, 'unity', 'test-results', 'capturas')
OUT = os.path.join(ROOT, 'docs', 'previews', 'estado-19-oct.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
NAVY, SUN, DIM, CREAM = (20, 18, 63), (255, 203, 69), (200, 196, 235), (245, 235, 219)
W, GAP, MARGIN = 300, 18, 30


def font(px, bold=True): return ImageFont.truetype(os.path.join(FONTS, 'fredoka.ttf' if bold else 'nunito_regular.ttf'), int(px))


APP_ROW = [
    ('hoy-isla-10', 'Hoy', 'la isla con sus 10 piezas y la ruta'),
    ('hoy-lamina', 'Un área en Hoy', 'la lámina con su avance'),
    ('juegos', 'Juegos', 'las cuatro áreas'),
    ('juegos-ficha', 'La ficha del juego', 'plegada, centrada: una decisión'),
    ('avance', 'Avance', 'tu semana y Nubi'),
    ('ajustes', 'Ajustes', 'con las farolas de la meta'),
    ('resultado-con-record', 'El final con sentido', 'con récord'),
    ('medalla-con-pieza', 'Una medalla', 'con su pieza de la isla'),
]
# (carpeta, parte del nombre de la toma, título, qué se ve)
GAME_ROW = [
    ('parejas', '04-', 'Constelaciones', 'a los 20 s de juego'),
    ('radar', '02-destello', 'Rescate relámpago', 'el destello'),
    ('piloto', '02-ruta', 'Piloto Estelar', 'la ruta de balizas'),
    ('calculo', '02-', 'Carga exacta', 'la primera pantalla jugable'),
    ('engranajes', '02-', 'Engranajes', 'la primera pantalla jugable'),
    ('meteoros', '02-', 'Lluvia de meteoros', 'la primera pantalla jugable'),
]


def load(path):
    im = Image.open(path).convert('RGB'); k = W / im.width
    return im.resize((W, int(im.height * k)), Image.LANCZOS)


def pick(folder, part):
    d = os.path.join(GAMES, folder)
    for f in sorted(os.listdir(d)):
        if f.endswith('.png') and part in f: return os.path.join(d, f)
    raise SystemExit('falta una toma de %s con «%s»' % (folder, part))


app = [(t, s, load(os.path.join(APP, f + '.png'))) for f, t, s in APP_ROW]
games = [(t, sub, load(pick(f, p))) for f, p, t, sub in GAME_ROW]
cols = max(len(app), len(games))
width = MARGIN * 2 + cols * W + (cols - 1) * GAP
h1 = max(i.height for _, _, i in app); h2 = max(i.height for _, _, i in games)
HEAD, LAB = 170, 74
height = HEAD + h1 + LAB + 60 + h2 + LAB + 20
sheet = Image.new('RGB', (width, height), NAVY); d = ImageDraw.Draw(sheet)
d.text((MARGIN, 24), 'Nubi · el estado al 19-oct (Tareas 121-142)', font=font(54), fill=SUN)
d.text((MARGIN, 88), 'Arriba, la APP (capturas reales de Roborazzi a 393 × 873 dp, con unos 20 días de uso de ejemplo); abajo, 6 de los 19 JUEGOS (capturas reales de Unity, 1080 × 2400). Detalle: docs/previews/app-actual/ y docs/previews/capturas/.', font=font(22, False), fill=DIM)
y = HEAD
d.text((MARGIN, y - 28), 'LA APP', font=font(26), fill=CREAM)
for i, (t, s, im) in enumerate(app):
    x = MARGIN + i * (W + GAP)
    sheet.paste(im, (x, y)); d.rectangle((x, y, x + W - 1, y + im.height - 1), outline=(70, 74, 130), width=1)
    d.text((x, y + h1 + 8), t, font=font(26), fill=CREAM); d.text((x, y + h1 + 40), s, font=font(20, False), fill=DIM)
y += h1 + LAB + 40
d.text((MARGIN, y - 28), 'LOS JUEGOS', font=font(26), fill=CREAM)
for i, (t, s, im) in enumerate(games):
    x = MARGIN + i * (W + GAP)
    sheet.paste(im, (x, y)); d.rectangle((x, y, x + W - 1, y + im.height - 1), outline=(70, 74, 130), width=1)
    d.text((x, y + h2 + 8), t, font=font(26), fill=CREAM); d.text((x, y + h2 + 40), s, font=font(20, False), fill=DIM)
sheet.save(OUT, optimize=True); print('hoja:', OUT, sheet.size, os.path.getsize(OUT) // 1024, 'KB')
