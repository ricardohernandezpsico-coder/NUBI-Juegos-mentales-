# -*- coding: utf-8 -*-
"""Hoja «hoy | propuesta» del pulido visual de la Tarea 147 (21-oct-2026; `auditoria-fable/propuestas-diseno-visual.md`: P2 un solo cielo, P3 Nubi en tres tamaños y un solo globo, P6 barras sin bolita, P7 farolas y un solo sol en Hoy,
P8 estados vacíos). La lámina del lugar tiene su propia hoja (`tools/previews/lamina_lugar.py` → `docs/previews/lamina-lugar.png`).
«HOY» sale del commit `630fc96` (`git show`: las tomas de la app y la tarjeta de compartir de antes; el Avance sin partidas, que antes no se capturaba, de `docs/previews/tarea-147-hoy/`, grabado con el código de ese commit)
y «PROPUESTA» de `docs/previews/app-actual/` ya regenerado (`AppActualScreenshotTest` + `tools/previews/app_actual.py`) y de `docs/previews/compartir-medalla.png`: capturas REALES de Roborazzi a 393 dp.
Uso:  python -I tools/previews/tarea_147_pulido.py      → docs/previews/tarea-147-pulido.png
"""
import io, os, subprocess, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
PREV = os.path.join(ROOT, 'docs', 'previews'); OUT = os.path.join(PREV, 'tarea-147-pulido.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
BEFORE_COMMIT = '630fc96'
NAVY, SUN, DIM, CREAM, LIME = (20, 18, 63), (255, 203, 69), (200, 196, 235), (245, 235, 219), (155, 229, 100)
W, GAP, PAIR_GAP, MARGIN, COLS = 380, 10, 44, 30, 3


def font(px, bold=True): return ImageFont.truetype(os.path.join(FONTS, 'fredoka.ttf' if bold else 'nunito_regular.ttf'), int(px))


# (clave, archivo «hoy» (None = el de la misma clave), recorte (x0, y0, x1, y1) sobre el original o None, título, qué cambió)
PAIRS = [
    ('app-actual/hoy', None, None, 'P7 · Hoy', 'las 7 farolas de la semana a 36 dp en UNA fila (con letra grande se parte en dos) y UN solo sol: el botón grande; «Encuentra tu punto de partida» pasa a enlace crema tenue con su flecha'),
    ('app-actual/primer-vuelo-1', None, None, 'P2 · Primer vuelo, Hola', 'el mismo cielo «Espacio profundo» de Hoy en vez del fondo aparte (nebulosas y estrellas que titilan); Nubi grande (L, 200 dp)'),
    ('app-actual/primer-vuelo-3', None, None, 'P2 · Primer vuelo, el camino de hoy', 'el mismo cielo de Hoy detrás de la isla; el texto sobre el cielo va en crema con su sombra de siempre (`OnBoardText`)'),
    ('compartir-medalla', None, None, 'P2 · La tarjeta que se comparte', 'dibuja el MISMO tablero y sus luceros (antes: un degradado con nebulosas y 170 estrellas al azar); el pie y el nombre del archivo, como en la Tarea 149'),
    ('app-actual/juegos', None, None, 'P3 · Juegos', 'Nubi en su tamaño M (120 dp) y UN solo globo con la sugerencia («Te sugiero Atención: …»), en vez de otro tamaño y otro globo'),
    ('app-actual/perfil', None, (0, 0, 1080, 1750), 'P3 · Perfil', 'arriba, la isla en miniatura con Nubi encima (la isla es el espacio de la persona), no un Nubi suelto de otro tamaño'),
    ('app-actual/avance-completa', None, (0, 1900, 1080, 3700), 'P6 · Avance, «Tus áreas»', 'las barras sin la bolita: el churro termina en su punta; lo de esta semana es un tramo más claro y, si bajó, una punta punteada'),
    ('app-actual/avance-area', None, None, 'P3 · P6 · El detalle de un área', 'Nubi con su globo (el mismo del resto de la app), la barra de las 5 etapas y la frase de la semana con los dos números («avanzó de 46 a 52»)'),
    ('app-actual/avance-vacio', 'tarea-147-hoy/avance-vacio', None, 'P8 · Avance sin ninguna partida', 'Nubi con UNA línea («Todavía no hay nada aquí. Después de tu primera ruta, esto se llena.») en vez de cuatro tarjetas de áreas vacías y «Lo que descubriste» sin nada'),
]


def before(name):
    if name.startswith('tarea-147-hoy/'):
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
    a = fit(before(hoy_name or name), crop); b = fit(Image.open(os.path.join(PREV, name + '.png')).convert('RGB'), crop)
    tiles.append((title, sub, a, b))
probe = ImageDraw.Draw(Image.new('RGB', (10, 10)))
SUBF = font(19, False); pair_w = 2 * W + GAP
rows = [tiles[i:i + COLS] for i in range(0, len(tiles), COLS)]
row_h = [max(max(t[2].height, t[3].height) + 34 + 40 + 26 * len(wrap(probe, t[1], SUBF, pair_w)) + 24 for t in row) for row in rows]
width = MARGIN * 2 + COLS * pair_w + (COLS - 1) * PAIR_GAP
height = 150 + sum(row_h) + 20
sheet = Image.new('RGB', (width, height), NAVY); d = ImageDraw.Draw(sheet)
d.text((MARGIN, 22), 'El pulido visual (Tarea 147: P2, P3, P6, P7, P8): hoy | propuesta', font=font(50), fill=SUN)
d.text((MARGIN, 88), 'Capturas reales de Roborazzi a 393 dp (la fecha de Hoy cambia solo porque las tomas son de días distintos). HOY = el commit %s; PROPUESTA = las de ahora. La lámina del lugar tiene su propia hoja (lamina-lugar.png).' % BEFORE_COMMIT, font=font(20, False), fill=DIM)
d.text((MARGIN, 114), 'Un solo cielo, Nubi en tres tamaños (S 72 · M 120 · L 200) y un solo globo, barras del kit sin bolita, un solo sol en Hoy y un Avance vacío con una línea de Nubi. Lo de Unity y Blender de P3 va en la Tarea 148.', font=font(20, False), fill=DIM)
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
