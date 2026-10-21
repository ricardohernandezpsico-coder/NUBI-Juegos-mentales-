# -*- coding: utf-8 -*-
"""Hoja «hoy | propuesta» de la Tarea C de la revisión del 19-oct (Tarea 145, 20-oct; `auditoria-fable/revision-pulido-19-oct.md` §4): las costuras «de antes» de la app.
«HOY» sale del commit `bce4c27` (las capturas de la Tarea 143, `git show`) y «PROPUESTA» de `docs/previews/app-actual/` ya regenerado (`AppActualScreenshotTest` + `tools/previews/app_actual.py`): capturas REALES de Roborazzi a 393 dp.
Uso:  python -I tools/previews/tarea_c_app.py      → docs/previews/tarea-c-app.png
"""
import io, os, subprocess, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
AFTER = os.path.join(ROOT, 'docs', 'previews', 'app-actual'); OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-c-app.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
BEFORE_COMMIT = 'bce4c27'
NAVY, SUN, DIM, CREAM, LIME = (20, 18, 63), (255, 203, 69), (200, 196, 235), (245, 235, 219), (155, 229, 100)
W, GAP, PAIR_GAP, MARGIN, COLS = 380, 10, 44, 30, 3


def font(px, bold=True): return ImageFont.truetype(os.path.join(FONTS, 'fredoka.ttf' if bold else 'nunito_regular.ttf'), int(px))


# (archivo, recorte (x0, y0, x1, y1) sobre 1080 de ancho o None, título, qué cambió)
PAIRS = [
    ('medalla-con-pieza', None, 'La celebración, con pieza', 'la medalla de plasticina de su familia (la de Avance y del Perfil) con el número de su nivel; los rayos, la luz y «Medalla nueva» se quedan'),
    ('medalla-sin-pieza', None, 'La celebración, sin pieza', 'lo mismo sin la línea «Y en tu isla aparece»'),
    ('licencias', None, 'Licencias y créditos', 'el texto largo sobre una losa crema con la letra en tinta: los luceros ya no quedan detrás de las palabras'),
    ('primer-vuelo-3', None, 'Cierre del Primer vuelo', 'la isla de Hoy (con Nubi) en el lugar de Nubi sola y la mitad vacía; «primer vuelo» en minúscula'),
    ('perfil', (0, 560, 1080, 1180), 'La fila de cifras del Perfil', '«Medallas 9» (los niveles ganados: 5 + 4 + 0, el mismo número de «Tus medallas») en lugar de «Mejor»'),
    ('primer-vuelo-1', (0, 0, 1080, 480), 'La bienvenida del Primer vuelo', '«Empezamos juntos» en lugar de «Ya diste el primer paso», que afirmaba algo que todavía no se había hecho'),
]


def before(name, crop):
    raw = subprocess.run(['git', '-C', ROOT, 'show', '%s:docs/previews/app-actual/%s.png' % (BEFORE_COMMIT, name)], capture_output=True, check=True).stdout
    return fit(Image.open(io.BytesIO(raw)).convert('RGB'), crop)


def fit(im, crop):
    if crop: im = im.crop(crop)
    k = W / im.width
    return im.resize((W, round(im.height * k)), Image.LANCZOS)


def wrap(d, text, fnt, width):
    lines, cur = [], ''
    for word in text.split(' '):
        t = (cur + ' ' + word).strip()
        if d.textlength(t, font=fnt) <= width: cur = t
        else: lines.append(cur); cur = word
    if cur: lines.append(cur)
    return lines


tiles = []
for name, crop, title, sub in PAIRS:
    a = before(name, crop); b = fit(Image.open(os.path.join(AFTER, name + '.png')).convert('RGB'), crop)
    tiles.append((title, sub, a, b))
probe = ImageDraw.Draw(Image.new('RGB', (10, 10)))
SUBF = font(19, False); pair_w = 2 * W + GAP
rows = [tiles[i:i + COLS] for i in range(0, len(tiles), COLS)]
row_h = [max(t[2].height for t in row) + 34 + 40 + 26 * max(len(wrap(probe, t[1], SUBF, pair_w)) for t in row) + 24 for row in rows]
width = MARGIN * 2 + COLS * pair_w + (COLS - 1) * PAIR_GAP
height = 150 + sum(row_h) + 20
sheet = Image.new('RGB', (width, height), NAVY); d = ImageDraw.Draw(sheet)
d.text((MARGIN, 22), 'Las costuras «de antes» de la app (Tarea 145): hoy | propuesta', font=font(50), fill=SUN)
d.text((MARGIN, 88), 'Capturas reales de Roborazzi a 393 × 873 dp. HOY = las del 20-oct (commit %s); PROPUESTA = las de ahora. Cambian la medalla de la celebración, Licencias, el cierre del Primer vuelo y tres textos; el escudo de liga espera al modelo de negocio.' % BEFORE_COMMIT, font=font(20, False), fill=DIM)
y = 150
for r, row in enumerate(rows):
    for c, (title, sub, a, b) in enumerate(row):
        x = MARGIN + c * (pair_w + PAIR_GAP)
        for k, (im, tag, col) in enumerate(((a, 'HOY', DIM), (b, 'PROPUESTA', LIME))):
            xx = x + k * (W + GAP)
            sheet.paste(im, (xx, y)); d.rectangle((xx, y, xx + im.width - 1, y + im.height - 1), outline=(70, 74, 130), width=1)
            d.text((xx, y + max(a.height, b.height) + 4), tag, font=font(22), fill=col)
        ty = y + max(a.height, b.height) + 34
        d.text((x, ty), title, font=font(28), fill=CREAM)
        ty += 40
        for line in wrap(d, sub, SUBF, pair_w): d.text((x, ty), line, font=SUBF, fill=DIM); ty += 26
    y += row_h[r]
sheet.save(OUT, optimize=True); print('hoja:', OUT, sheet.size, os.path.getsize(OUT) // 1024, 'KB')
