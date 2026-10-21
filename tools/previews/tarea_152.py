# -*- coding: utf-8 -*-
"""Hoja «hoy | propuesta» de la Tarea 152 (21-oct-2026; `auditoria-fable/auditoria-coherencia-20-oct.md`, B5, B6 y C): lo innecesario.
«HOY» sale del commit `3525c81` (`git show`: las tomas de las Tareas 150 y 151)
y «PROPUESTA» de `docs/previews/app-actual/` ya regenerado (`AppActualScreenshotTest` + `tools/previews/app_actual.py`): capturas REALES de Roborazzi a 393 dp.
Uso:  python -I tools/previews/tarea_152.py      → docs/previews/tarea-152.png
"""
import io, os, subprocess, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
PREV = os.path.join(ROOT, 'docs', 'previews'); OUT = os.path.join(PREV, 'tarea-152.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
BEFORE_COMMIT = '3525c81'
NAVY, SUN, DIM, CREAM, LIME = (20, 18, 63), (255, 203, 69), (200, 196, 235), (245, 235, 219), (155, 229, 100)
W, GAP, PAIR_GAP, MARGIN, COLS = 380, 10, 44, 30, 3


def font(px, bold=True): return ImageFont.truetype(os.path.join(FONTS, 'fredoka.ttf' if bold else 'nunito_regular.ttf'), int(px))


# (clave, archivo «hoy» (None = el de la misma clave), recorte (x0, y0, x1, y1) sobre el original —o un par (hoy, propuesta) cuando cambió el largo de la pantalla—, título, qué cambió)
PAIRS = [
    ('app-actual/ajustes-completa', None, ((0, 0, 1080, 3500), (0, 0, 1080, 3500)), 'B5.1 · B5.2 · B6.5 · Ajustes', 'sin «Tu nombre» y «Guardar» (el nombre se edita en el Perfil), sin la pregunta de los colores (no hacía nada) y con «Tu nivel de estudios» y para qué sirve, bajo «Cómo juegas»'),
    ('app-actual/ajustes-completa', None, ((0, 4100, 1080, 4950), (0, 3950, 1080, 4900)), 'C3 · Ajustes, la hora del aviso', '«Tarde 15:00 · Al atardecer 19:00 · Noche 21:00» (antes «Noche 19:00» y «Más tarde 21:00»: «Más tarde» no es una hora)'),
    ('app-actual/perfil', None, (0, 0, 1080, 2400), 'B6.3 · B6.5 · Perfil', 'el nombre se edita aquí y se guarda al salir del campo (sin botón); el nivel de estudios se fue a Ajustes'),
    ('app-actual/avance-completa', None, ((0, 3500, 1080, 5330), (0, 3500, 1080, 5050)), 'B6.1 · Avance, las medallas', 'las tres tarjetas viven solo en el Perfil; aquí una línea, «9 medallas · ver en tu perfil», que lleva allá'),
    ('app-actual/hoy', None, None, 'B6.2 · Hoy', 'la invitación «Encuentra tu punto de partida» dura hasta 5 partidas o 7 días: con 41 partidas ya no sale (sigue en Avance, «Tu punto de partida»)'),
    ('app-actual/juegos-ficha', None, None, 'C5 · La ficha plegada', 'el botón dice «Jugar» a secas: la línea de arriba («A tu medida · con reloj (2 min)») ya dice lo elegido (desplegada, el botón lleva lo elegido)'),
    ('app-actual/licencias-completa', None, (0, 0, 1080, 2600), 'C2 · Licencias y créditos', 'sin «Cosecha de palabras», que está retirada (la licencia de SPALEX sigue valiendo para los otros dos juegos)'),
]


def before(name):
    if name.startswith('tarea-150-hoy/'):
        return Image.open(os.path.join(PREV, name + '.png')).convert('RGB')
    raw = subprocess.run(['git', '-C', ROOT, 'show', '%s:docs/previews/%s.png' % (BEFORE_COMMIT, name)], capture_output=True, check=True).stdout
    return Image.open(io.BytesIO(raw)).convert('RGB')


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
for name, hoy_name, crop, title, sub in PAIRS:
    ca, cb = crop if (crop and isinstance(crop[0], tuple)) else (crop, crop)
    a = fit(before(hoy_name or name), ca); b = fit(Image.open(os.path.join(PREV, name + '.png')).convert('RGB'), cb)
    tiles.append((title, sub, a, b))
probe = ImageDraw.Draw(Image.new('RGB', (10, 10)))
SUBF = font(19, False); pair_w = 2 * W + GAP
rows = [tiles[i:i + COLS] for i in range(0, len(tiles), COLS)]
row_h = [max(max(t[2].height, t[3].height) + 34 + 40 + 26 * len(wrap(probe, t[1], SUBF, pair_w)) + 24 for t in row) for row in rows]
width = MARGIN * 2 + COLS * pair_w + (COLS - 1) * PAIR_GAP
height = 150 + sum(row_h) + 20
sheet = Image.new('RGB', (width, height), NAVY); d = ImageDraw.Draw(sheet)
d.text((MARGIN, 22), 'Lo innecesario se va (Tarea 152): hoy | propuesta', font=font(50), fill=SUN)
d.text((MARGIN, 88), 'Capturas reales de Roborazzi a 393 dp. HOY = el commit %s (Tareas 150 y 151); PROPUESTA = las de ahora.' % BEFORE_COMMIT, font=font(20, False), fill=DIM)
d.text((MARGIN, 114), 'Ajustes es de cómo juega la app, el Perfil es de quién eres y Avance de cómo vas: cada cosa vive en un solo sitio, sin preguntas que no hacen nada y sin repetir.', font=font(20, False), fill=DIM)
y = 150
for r, row in enumerate(rows):
    for c, (title, sub, a, b) in enumerate(row):
        x = MARGIN + c * (pair_w + PAIR_GAP)
        top = max(a.height, b.height)                                   # los rótulos van bajo SU propia pantalla (cada una con su alto)
        for k, (im, tag, col) in enumerate(((a, 'HOY', DIM), (b, 'PROPUESTA', LIME))):
            xx = x + k * (W + GAP)
            sheet.paste(im, (xx, y)); d.rectangle((xx, y, xx + im.width - 1, y + im.height - 1), outline=(70, 74, 130), width=1)
            d.text((xx, y + top + 4), tag, font=font(22), fill=col)
        ty = y + top + 34
        d.text((x, ty), title, font=font(28), fill=CREAM)
        ty += 40
        for line in wrap(d, sub, SUBF, pair_w): d.text((x, ty), line, font=SUBF, fill=DIM); ty += 26
    y += row_h[r]
sheet.save(OUT, optimize=True); print('hoja:', OUT, sheet.size, os.path.getsize(OUT) // 1024, 'KB')
