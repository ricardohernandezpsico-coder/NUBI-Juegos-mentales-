# -*- coding: utf-8 -*-
"""Hoja «hoy | propuesta» de la Tarea 153 (21-oct-2026; `auditoria-fable/auditoria-coherencia-20-oct.md`, B7 y C6): la letra de 16 sp, más el nombre del Perfil con «Cambiar» (pedido de Opus).
«HOY» sale del commit `cdb571e` (`git show`: las tomas de la Tarea 152)
y «PROPUESTA» de `docs/previews/app-actual/` ya regenerado (`AppActualScreenshotTest` + `tools/previews/app_actual.py`): capturas REALES de Roborazzi a 393 dp.
Uso:  python -I tools/previews/tarea_153.py      → docs/previews/tarea-153.png
"""
import io, os, subprocess, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
PREV = os.path.join(ROOT, 'docs', 'previews'); OUT = os.path.join(PREV, 'tarea-153.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
BEFORE_COMMIT = 'cdb571e'
NAVY, SUN, DIM, CREAM, LIME = (20, 18, 63), (255, 203, 69), (200, 196, 235), (245, 235, 219), (155, 229, 100)
W, GAP, PAIR_GAP, MARGIN, COLS = 380, 10, 44, 30, 3


def font(px, bold=True): return ImageFont.truetype(os.path.join(FONTS, 'fredoka.ttf' if bold else 'nunito_regular.ttf'), int(px))


# (clave, archivo «hoy» (None = el de la misma clave), recorte (x0, y0, x1, y1) sobre el original —o un par (hoy, propuesta) cuando cambió el largo de la pantalla—, título, qué cambió)
PAIRS = [
    ('app-actual/perfil', None, (0, 0, 1080, 1750), 'Perfil · el nombre con «Cambiar»', 'el nombre otra vez en grande, como texto, con un botón chico del kit «Cambiar» (antes, un campo siempre abierto que se leía como formulario y sin rótulo); el lector de pantalla dice «Tu nombre, Ricardo, botón Cambiar»'),
    ('app-actual/perfil-editando', 'app-actual/perfil', (0, 0, 1080, 1750), 'Perfil · al tocar «Cambiar»', 'el nombre pasa a un campo con el rótulo «Tu nombre» y el teclado abierto; se guarda con «Listo» del teclado, al salir del campo o del Perfil, y vuelve a texto'),
    ('app-actual/hoy', None, None, 'B7 · Hoy a 16 sp', 'la fecha y la meta («Miércoles 21 oct. · 2 de 4 días de tu meta») y «1 de 3 · unos 4 minutos» pasan de 15 a 16 sp; con el mes abreviado la fecha sigue en UNA línea'),
    ('app-actual/avance-area', None, (0, 0, 1080, 2000), 'B7 · C6 · El detalle de un área', '«Te faltan 8 para Experto» a 16 sp; el gráfico de las 4 semanas a 14 sp (rótulo de gráfico, antes 13) y con «sem.» («hace 3 sem. · sem. pasada · esta sem.»)'),
    ('app-actual/ajustes-completa', None, (0, 700, 1080, 3200), 'B7 · Ajustes', 'las opciones de edad, del nivel de estudios y las horas del aviso a 16 sp (antes 15) y «Un aviso al día» a 16 (antes 13)'),
    ('app-actual/licencias-completa', None, (0, 400, 1080, 2400), 'B7 · Licencias y créditos', 'todo el cuerpo a 16 sp (antes 15): es la pantalla de lectura larga, la que más lo necesita'),
    ('app-actual/juegos-letra13-abajo', 'app-actual/juegos-letra13', None, 'C10 · Juegos con letra 1,3', 'a la izquierda, la toma de siempre (la segunda fila queda bajo la barra hasta deslizar); a la derecha, deslizada hasta el final: la rejilla termina con 16 dp de aire y la segunda fila se ve entera sobre la barra'),
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
d.text((MARGIN, 22), 'La letra de 16 sp y el nombre del Perfil (Tarea 153): hoy | propuesta', font=font(50), fill=SUN)
d.text((MARGIN, 88), 'Capturas reales de Roborazzi a 393 dp. HOY = el commit %s (Tarea 152); PROPUESTA = las de ahora.' % BEFORE_COMMIT, font=font(20, False), fill=DIM)
d.text((MARGIN, 114), 'El texto que informa va a 16 sp o más en toda la app; 14 sp solo en los rótulos de gráfico (TamanosDeLetraTest lo vigila). Ya no hay texto de 13 sp.', font=font(20, False), fill=DIM)
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
