# -*- coding: utf-8 -*-
"""Hoja «hoy | propuesta» de la Tarea 149 (20-oct-2026; `auditoria-fable/auditoria-coherencia-20-oct.md` A1): las ligas fuera de la vista (`Features.LEAGUES = false`).
«HOY» sale del commit `2492b64` (`git show`: las tomas de la app y la tarjeta de compartir de antes) y «PROPUESTA» de `docs/previews/app-actual/` ya regenerado (`AppActualScreenshotTest` + `tools/previews/app_actual.py`) y de `docs/previews/compartir-medalla.png`: capturas REALES de Roborazzi a 393 dp.
Uso:  python -I tools/previews/tarea_149_ligas.py      → docs/previews/tarea-149-ligas.png
"""
import io, os, subprocess, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
PREV = os.path.join(ROOT, 'docs', 'previews'); AFTER = os.path.join(PREV, 'app-actual'); OUT = os.path.join(PREV, 'tarea-149-ligas.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
BEFORE_COMMIT = '2492b64'
NAVY, SUN, DIM, CREAM, LIME = (20, 18, 63), (255, 203, 69), (200, 196, 235), (245, 235, 219), (155, 229, 100)
W, GAP, PAIR_GAP, MARGIN, COLS = 380, 10, 44, 30, 3


def font(px, bold=True): return ImageFont.truetype(os.path.join(FONTS, 'fredoka.ttf' if bold else 'nunito_regular.ttf'), int(px))


# (archivo en docs/previews, recorte (x0, y0, x1, y1) sobre el original o None, título, qué cambió)
PAIRS = [
    ('app-actual/hoy', (0, 0, 1080, 330), 'Hoy: el avatar', 'sin el escudo de bronce: el avatar va solo con su inicial, y el lector dice «Tu perfil» (antes «Tu perfil, liga Bronce»)'),
    ('app-actual/avance-completa', (0, 3480, 1080, 5330), 'Avance: de «Tus medallas» al final', 'sin «Tu liga · Liga Bronce · 37 trofeos» ni su botón Compartir'),
    ('app-actual/perfil-completa', (0, 1180, 1080, 2150), 'Perfil', 'sin la sección «Tu liga»: queda Nubi, las cifras, las medallas y el nivel de estudios'),
    ('compartir-medalla', (0, 1130, 1080, 1920), 'La tarjeta que se comparte (abajo)', 'el pie dice «Juega. Avanza. Cada día un poco.» (antes «Sube de liga») y el archivo se llama nubi-medalla.png (antes neurovida-liga.png)'),
]
NOTICES = (
    ['«Tu camino te espera: 3 juegos cortos y vuelves a sumar trofeos.»', '«Cada partida suma trofeos para tu liga.»'],
    ['«Te esperan 3 juegos cortos y sigues tu ruta.»', '«Cada partida suma a tu avance.»'],
)


def before(name):
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


def notices_tile(height):
    """Los dos avisos que hablaban de trofeos y liga: texto contra texto (no hay captura: salen en la bandeja del sistema)."""
    tiles = []
    for lines, tag in zip(NOTICES, ('HOY', 'PROPUESTA')):
        im = Image.new('RGB', (W, height), (30, 28, 82)); d = ImageDraw.Draw(im); f = font(21, False); y = 24
        d.text((16, y), 'Avisos del día', font=font(22), fill=SUN); y += 44
        for line in lines:
            for part in wrap(d, line, f, W - 32): d.text((16, y), part, font=f, fill=CREAM); y += 29
            y += 16
        tiles.append(im)
    return tiles


tiles = []
for name, crop, title, sub in PAIRS:
    a = fit(before(name), crop); b = fit(Image.open(os.path.join(PREV, name + '.png')).convert('RGB'), crop)
    tiles.append((title, sub, a, b))
h = max(max(t[2].height, t[3].height) for t in tiles)
na, nb = notices_tile(300)
tiles.append(('Los avisos', 'ya no hablan de trofeos ni de ligas; el de «vuelves después de varios días» dice «sigues tu ruta»', na, nb))
probe = ImageDraw.Draw(Image.new('RGB', (10, 10)))
SUBF = font(19, False); pair_w = 2 * W + GAP
rows = [tiles[i:i + COLS] for i in range(0, len(tiles), COLS)]
row_h = [max(max(t[2].height, t[3].height) + 34 + 40 + 26 * len(wrap(probe, t[1], SUBF, pair_w)) + 24 for t in row) for row in rows]
width = MARGIN * 2 + COLS * pair_w + (COLS - 1) * PAIR_GAP
height = 150 + sum(row_h) + 20
sheet = Image.new('RGB', (width, height), NAVY); d = ImageDraw.Draw(sheet)
d.text((MARGIN, 22), 'Las ligas fuera de la vista (Tarea 149): hoy | propuesta', font=font(50), fill=SUN)
d.text((MARGIN, 88), 'Capturas reales de Roborazzi a 393 dp (la fecha de Hoy cambia solo porque las tomas son de días distintos). HOY = el commit %s; PROPUESTA = las de ahora.' % BEFORE_COMMIT, font=font(20, False), fill=DIM)
d.text((MARGIN, 114), '`Features.LEAGUES = false`: no se ve NADA de liga ni de trofeos; el dato (RankTier, rating, trofeos) se sigue guardando y la celebración de ascenso no sale.', font=font(20, False), fill=DIM)
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
