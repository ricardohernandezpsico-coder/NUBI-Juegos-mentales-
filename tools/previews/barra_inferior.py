# -*- coding: utf-8 -*-
"""Hoja «hoy | propuesta» de la Tarea 156 (21-oct-2026; `auditoria-fable/spec-barra-inferior.md`, opción C: discos del kit con un glifo de churro tinta y la elegida sobre una losa sol).
CAPTURAS REALES de Roborazzi a 393 dp (`AppActualScreenshotTest`: Hoy, Juegos y Avance con la barra puesta como en la app, sobre los 48 dp de la barra del teléfono). «HOY» = el commit `a7032b1` (`git show`: la bandeja crema con tres cubos y objetos);
«PROPUESTA» = `docs/previews/app-actual/` ya regenerado. De cada toma se muestra el PIE (lo de abajo, donde está la barra) a letra 1,0 y 1,3.
Uso:  python -I tools/previews/barra_inferior.py      → docs/previews/tarea-156.png
"""
import io, os, subprocess, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
PREV = os.path.join(ROOT, 'docs', 'previews'); OUT = os.path.join(PREV, 'tarea-156.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
BEFORE_COMMIT = 'a7032b1'
NAVY, SUN, DIM, CREAM, LIME = (20, 18, 63), (255, 203, 69), (200, 196, 235), (245, 235, 219), (155, 229, 100)
W, GAP, MARGIN = 640, 18, 40
FOOT = 1000                                   # px de la toma (1080 de ancho = 393 dp) que se muestran desde abajo: la barra y lo que tiene encima


def font(px, bold=True): return ImageFont.truetype(os.path.join(FONTS, 'fredoka.ttf' if bold else 'nunito_regular.ttf'), int(px))


def hoy(name):
    raw = subprocess.run(['git', '-C', ROOT, 'show', '%s:docs/previews/app-actual/%s.png' % (BEFORE_COMMIT, name)], capture_output=True, check=True).stdout
    return Image.open(io.BytesIO(raw)).convert('RGB')


def nuevo(name): return Image.open(os.path.join(PREV, 'app-actual', name + '.png')).convert('RGB')


def foot(im):
    im = im.crop((0, max(0, im.height - FOOT), im.width, im.height))
    return im.resize((W, round(im.height * W / im.width)), Image.LANCZOS)


def wrap(d, text, fnt, width):
    lines, cur = [], ''
    for word in text.split(' '):
        t = (cur + ' ' + word).strip()
        if d.textlength(t, font=fnt) <= width: cur = t
        else: lines.append(cur); cur = word
    if cur: lines.append(cur)
    return lines


FILAS = [
    ('HOY · letra 1,0 · la barra mide ~115 dp', DIM, [(hoy('hoy'), 'Hoy'), (hoy('juegos'), 'Juegos'), (hoy('avance'), 'Avance')]),
    ('PROPUESTA · letra 1,0 · la barra mide 88 dp', LIME, [(nuevo('hoy'), 'Hoy'), (nuevo('juegos'), 'Juegos'), (nuevo('avance'), 'Avance')]),
    ('HOY · letra 1,3 · ~121 dp', DIM, [(hoy('hoy-letra13'), 'Hoy'), (hoy('juegos-letra13'), 'Juegos'), (hoy('avance-letra13'), 'Avance')]),
    ('PROPUESTA · letra 1,3 · 94 dp (crece solo lo que crece el rótulo)', LIME, [(nuevo('hoy-letra13'), 'Hoy'), (nuevo('juegos-letra13'), 'Juegos'), (nuevo('avance-letra13'), 'Avance')]),
]
INTRO = ('La queja de Ricardo: «los íconos casi no se ven y su base parece un sándwich». Se va la bandeja crema y los cubos con objetos de ~30 dp (claros sobre crema). Ahora: una franja del cielo un tono más claro con un filo arriba, tres discos crema de 48 dp con un glifo '
         'de churro tinta de 32 dp (la isla, un cohete, una farola: el mismo lenguaje que la X, la flecha de volver y los íconos de Ajustes) y el rótulo de 16 sp debajo. La pestaña elegida se apoya en una losa sol con el rótulo dentro, en tinta y negrita: se ve por forma, por peso y por «seleccionado» '
         'para el lector de pantalla, nunca solo por color. Cada pestaña es TODA su columna (un tercio del ancho, todo el alto: ≥ 48 dp). La barra baja de ~115 dp a 88; el fundido de 150 ms y «quitar animaciones» se conservan.')

probe = ImageDraw.Draw(Image.new('RGB', (10, 10)))
SUBF = font(22, False); ROWF = font(32)
cols = 3; width = MARGIN * 2 + cols * W + (cols - 1) * GAP
intro_lines = wrap(probe, INTRO, SUBF, width - 2 * MARGIN)
heights = [50 + max(foot(im).height for im, _ in tiles) + 8 + 30 + 18 for _, _, tiles in FILAS]
H = 130 + 30 * len(intro_lines) + 26 + sum(heights) + 20
sheet = Image.new('RGB', (width, H), NAVY); d = ImageDraw.Draw(sheet)
d.text((MARGIN, 22), 'La barra inferior del kit (Tarea 156): hoy | propuesta', font=font(50), fill=SUN)
d.text((MARGIN, 88), 'Capturas reales de Roborazzi a 393 dp, el pie de cada pantalla. HOY = el commit %s (Tarea 148); PROPUESTA = lo de ahora.' % BEFORE_COMMIT, font=font(22, False), fill=DIM)
y = 128
for line in intro_lines:
    d.text((MARGIN, y), line, font=SUBF, fill=CREAM); y += 30
y += 26
for (label, col, tiles), h in zip(FILAS, heights):
    d.text((MARGIN, y), label, font=ROWF, fill=col)
    x = MARGIN; top = y + 50
    for im, cap in tiles:
        f = foot(im); sheet.paste(f, (x, top)); d.rectangle((x, top, x + f.width - 1, top + f.height - 1), outline=(70, 74, 130), width=1)
        d.text((x, top + f.height + 6), cap, font=SUBF, fill=DIM)
        x += W + GAP
    y += h
sheet.save(OUT)
print('ok', OUT, sheet.size)
