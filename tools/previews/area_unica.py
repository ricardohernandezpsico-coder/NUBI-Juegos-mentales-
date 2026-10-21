# -*- coding: utf-8 -*-
"""Hoja «hoy | propuesta» de la Tarea 155 (21-oct-2026; `auditoria-fable/spec-area-unica.md`, opción 1: un área, una sola vista): tocar un área abre LA MISMA lámina en Hoy, Juegos y Avance.
CAPTURAS REALES de Roborazzi a 393 dp (`AppActualScreenshotTest` y `AreaSheetEnLasTresTest`). «HOY» = el commit `a7032b1` (la Tarea 148, `git show`: la lámina de Hoy, la ventana de Juegos y el detalle de Avance de antes);
«PROPUESTA» = `docs/previews/app-actual/` ya regenerado. Arriba las tres de hoy y abajo las tres nuevas, a letra 1,0 y 1,3.
Uso:  python -I tools/previews/area_unica.py      → docs/previews/tarea-155.png
"""
import io, os, subprocess, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
PREV = os.path.join(ROOT, 'docs', 'previews'); OUT = os.path.join(PREV, 'tarea-155.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
BEFORE_COMMIT = 'a7032b1'
NAVY, SUN, DIM, CREAM, LIME = (20, 18, 63), (255, 203, 69), (200, 196, 235), (245, 235, 219), (155, 229, 100)
W, GAP, MARGIN = 430, 14, 40


def font(px, bold=True): return ImageFont.truetype(os.path.join(FONTS, 'fredoka.ttf' if bold else 'nunito_regular.ttf'), int(px))


def hoy(name):
    raw = subprocess.run(['git', '-C', ROOT, 'show', '%s:docs/previews/app-actual/%s.png' % (BEFORE_COMMIT, name)], capture_output=True, check=True).stdout
    return Image.open(io.BytesIO(raw)).convert('RGB')


def nuevo(name): return Image.open(os.path.join(PREV, 'app-actual', name + '.png')).convert('RGB')


def fit(im): return im.resize((W, round(im.height * W / im.width)), Image.LANCZOS)


def wrap(d, text, fnt, width):
    lines, cur = [], ''
    for word in text.split(' '):
        t = (cur + ' ' + word).strip()
        if d.textlength(t, font=fnt) <= width: cur = t
        else: lines.append(cur); cur = word
    if cur: lines.append(cur)
    return lines


# (fila, rótulo de la fila, [(imagen, rótulo de la columna)])
FILAS = [
    ('HOY · letra 1,0', DIM, [(hoy('hoy-lamina'), 'Hoy · tocar un edificio'), (hoy('juegos-memoria'), 'Juegos · la ventana de un área'), (hoy('avance-area'), 'Avance · el detalle de un área')]),
    ('PROPUESTA · letra 1,0', LIME, [(nuevo('hoy-lamina'), 'Hoy · la lámina (igual; sus filas dicen «hoy, en tu ruta» / «hecho hoy»)'), (nuevo('juegos-lamina'), 'Juegos · LA MISMA lámina'), (nuevo('avance-lamina'), 'Avance · la misma lámina + el gráfico de las 4 semanas')]),
    ('HOY · letra 1,3', DIM, [(hoy('hoy-lamina-letra13'), 'Hoy'), (hoy('juegos-memoria-letra13'), 'Juegos'), (hoy('avance-area-letra13'), 'Avance')]),
    ('PROPUESTA · letra 1,3', LIME, [(nuevo('hoy-lamina-letra13'), 'Hoy'), (nuevo('juegos-lamina-letra13'), 'Juegos'), (nuevo('avance-lamina-letra13'), 'Avance (cede el globo y los nombres de las etapas; los juegos y el botón no)')]),
]
INTRO = ('Tocar un área abre SIEMPRE lo mismo: la lámina de Hoy (velo, lámina que sube, Nubi que vuela hasta ella). En Juegos Nubi sale de «¿Qué jugamos hoy?»; en Avance, de «Tu semana». X, Atrás o el velo la cierran; tocar un juego abre su ficha encima. '
         'El botón juega lo mismo en las tres. Se pierden las secciones «En tu ruta de hoy» / «Para volver a jugar» de Juegos (las filas lo dicen: «hoy, en tu ruta», «hecho hoy»; las paradas van primero), la cifra por juego en la lista (queda en la ficha) y las flechas ‹ › de Avance.')

probe = ImageDraw.Draw(Image.new('RGB', (10, 10)))
SUBF = font(20, False); TAGF = font(21); ROWF = font(30)
cols = 3; width = MARGIN * 2 + cols * W + (cols - 1) * GAP
intro_lines = wrap(probe, INTRO, SUBF, width - 2 * MARGIN)
heights = []
for _, _, tiles in FILAS:
    tile_h = max(fit(im).height for im, _ in tiles)
    cap_h = max(len(wrap(probe, cap, SUBF, W)) for _, cap in tiles) * 26
    heights.append(46 + tile_h + 8 + cap_h + 20)
H = 120 + 28 * len(intro_lines) + 30 + sum(heights) + 20
sheet = Image.new('RGB', (width, H), NAVY); d = ImageDraw.Draw(sheet)
d.text((MARGIN, 22), 'Un área, una sola vista (Tarea 155): hoy | propuesta', font=font(48), fill=SUN)
d.text((MARGIN, 84), 'Capturas reales de Roborazzi a 393 dp. HOY = el commit %s (Tarea 148); PROPUESTA = lo de ahora.' % BEFORE_COMMIT, font=font(20, False), fill=DIM)
y = 118
for line in intro_lines:
    d.text((MARGIN, y), line, font=SUBF, fill=CREAM); y += 28
y += 20
for (label, col, tiles), h in zip(FILAS, heights):
    d.text((MARGIN, y), label, font=ROWF, fill=col)
    x = MARGIN; top = y + 46
    for im, cap in tiles:
        f = fit(im); sheet.paste(f, (x, top)); d.rectangle((x, top, x + f.width - 1, top + f.height - 1), outline=(70, 74, 130), width=1)
        for i, line in enumerate(wrap(d, cap, SUBF, W)): d.text((x, top + f.height + 6 + 26 * i), line, font=SUBF, fill=DIM)
        x += W + GAP
    y += h
sheet.save(OUT)
print('ok', OUT, sheet.size)
