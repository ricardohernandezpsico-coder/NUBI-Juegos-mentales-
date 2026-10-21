# -*- coding: utf-8 -*-
"""Hoja «hoy | propuesta» de las Tareas 150 y 151 (21-oct-2026; `auditoria-fable/auditoria-coherencia-20-oct.md`, B1, B2, B4 y B5): una palabra por concepto y números que cuadren.
«HOY» sale del commit `ffd2439` (`git show`: las tomas de la Tarea 147; la carga del juego, que antes no se capturaba, de `docs/previews/tarea-150-hoy/`, grabada con el código de ese commit)
y «PROPUESTA» de `docs/previews/app-actual/` ya regenerado (`AppActualScreenshotTest` + `tools/previews/app_actual.py`): capturas REALES de Roborazzi a 393 dp.
Uso:  python -I tools/previews/tarea_150_151.py      → docs/previews/tarea-150-151.png
"""
import io, os, subprocess, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
PREV = os.path.join(ROOT, 'docs', 'previews'); OUT = os.path.join(PREV, 'tarea-150-151.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
BEFORE_COMMIT = 'ffd2439'
NAVY, SUN, DIM, CREAM, LIME = (20, 18, 63), (255, 203, 69), (200, 196, 235), (245, 235, 219), (155, 229, 100)
W, GAP, PAIR_GAP, MARGIN, COLS = 380, 10, 44, 30, 3


def font(px, bold=True): return ImageFont.truetype(os.path.join(FONTS, 'fredoka.ttf' if bold else 'nunito_regular.ttf'), int(px))


# (clave, archivo «hoy» (None = el de la misma clave), recorte (x0, y0, x1, y1) sobre el original o None, título, qué cambió)
PAIRS = [
    ('app-actual/hoy', None, None, 'Hoy · el enlace en una línea', '«Encuentra tu punto de partida ›» en una línea (antes «· 4 juegos cortos», que bajaba a dos); «Tu ruta de hoy · 1 de 3 · unos 4 minutos»: una sola cifra de minutos'),
    ('app-actual/juegos', None, None, 'B2 · Juegos: una sola sugerencia', 'el globo dice «Te sugiero Razonamiento: sigue Carga exacta en tu ruta de hoy» (la que sigue en la ruta; antes «es el área que más puede crecer») y el chip del edificio se quitó: globo O chip, no los dos (la lámina conserva el chip «Nubi sugiere»)'),
    ('app-actual/hoy-lamina', None, None, 'B1 · B2 · La lámina', 'el globo dice «Esta semana avanzó de 18 a 22» (antes «avanzaste»: en Avance decía «avanzó»); «Hábil · 47 de 100»; el chip «Nubi sugiere» solo en el área de la sugerencia'),
    ('app-actual/avance-area', None, None, 'B1 · B2 · El detalle de un área', 'el globo de 6 líneas pasó a DOS frases: «¿Le damos un empujón? Te sugiero Constelaciones: sigue en tu ruta de hoy.» (la línea de avance de abajo ya dice cuánto avanzó); «Hábil · 52 de 100» y «Te faltan 8 para Experto»'),
    ('app-actual/juegos-memoria', None, None, 'B4.5 · La ventana de Juegos', 'un solo visto: el sello del emblema es «jugado hoy»; la parada hecha de la ruta se dice con la palabra «Hecho» (antes un segundo círculo con ✓)'),
    ('app-actual/juegos-ficha-desplegada', None, None, 'B1.3 · B4.4 · B5 · La ficha', '«Al límite» (antes «Experto», que es una etapa), cada modo con una frase corta (antes «9 de 10 aciertos»), «Con reloj» y un subtítulo que se entiende («Gira la pieza en tu mente»)'),
    ('app-actual/avance-completa', None, (0, 1900, 1080, 3700), 'B1.8 · Avance, «Tus áreas»', 'la tarjeta de cada área dice lo mismo que el detalle y la lámina: «Esta semana avanzó de 37 a 47» (antes «Subió 10 puntos esta semana»); «esta semana» cuenta desde el lunes en toda la pantalla'),
    ('app-actual/avance-completa', None, (0, 3700, 1080, 5050), 'B1.5 · B1.11 · Avance, «Tus medidas»', '«Tus medidas» (antes «Lo que descubriste» y «Tus marcas») y las medallas dicen «5 de 8 · siguiente: 30 días» (antes «Nivel 5 de 8», cuando la app dice «etapa»)'),
    ('app-actual/perfil', None, (0, 0, 1080, 2400), 'B1.6 · Perfil', '«Etapa general: Hábil» (la etapa de toda la persona, no de un área) y las medallas con «N de M»'),
    ('app-actual/primer-vuelo-2', None, None, 'B4.3 · B1.9 · Primer vuelo, las metas', '«Paso 2 de 6 · una pregunta» (el mismo «de 6» que las farolas; antes la edad y las metas eran las dos «Paso 2 de 6») y las áreas con las mismas palabras que Juegos («recordar y ubicar», no «recordar, retener»)'),
    ('app-actual/primer-vuelo-3', None, None, 'B1.1 · Primer vuelo, el cierre', '«ruta» en todo: «Tu primer vuelo fue tu ruta de hoy» y «Empezar mi ruta» (antes «camino»)'),
    ('app-actual/carga-juego', 'tarea-150-hoy/carga-juego', None, 'B1.2 · La carga de un juego', '«Etapa Hábil · Con reloj» (antes «Reto»; sin tiempo decía «Precisión», ahora «Sin reloj»), el mismo nombre que en la ficha'),
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
    a = fit(before(hoy_name or name), crop); b = fit(Image.open(os.path.join(PREV, name + '.png')).convert('RGB'), crop)
    tiles.append((title, sub, a, b))
probe = ImageDraw.Draw(Image.new('RGB', (10, 10)))
SUBF = font(19, False); pair_w = 2 * W + GAP
rows = [tiles[i:i + COLS] for i in range(0, len(tiles), COLS)]
row_h = [max(max(t[2].height, t[3].height) + 34 + 40 + 26 * len(wrap(probe, t[1], SUBF, pair_w)) + 24 for t in row) for row in rows]
width = MARGIN * 2 + COLS * pair_w + (COLS - 1) * PAIR_GAP
height = 150 + sum(row_h) + 20
sheet = Image.new('RGB', (width, height), NAVY); d = ImageDraw.Draw(sheet)
d.text((MARGIN, 22), 'Una palabra por concepto y números que cuadren (Tareas 150 y 151): hoy | propuesta', font=font(50), fill=SUN)
d.text((MARGIN, 88), 'Capturas reales de Roborazzi a 393 dp (la fecha y los datos de las tomas son de días distintos). HOY = el commit %s (Tarea 147); PROPUESTA = las de ahora.' % BEFORE_COMMIT, font=font(20, False), fill=DIM)
d.text((MARGIN, 114), '«ruta» en todo · «Con reloj / Sin reloj» · modos Suave, A tu medida, Desafío, Al límite · «Hábil · 47 de 100» y «Te faltan N para X» · UNA sola sugerencia de Nubi · una sola frase para lo que cambió y para el descanso · «esta semana» desde el lunes · un solo visto.', font=font(20, False), fill=DIM)
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
