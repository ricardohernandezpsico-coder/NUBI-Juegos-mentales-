# -*- coding: utf-8 -*-
"""Hoja «antes | después» de la ronda final de pulido de la app (Tarea 143, 19-oct; revisión de Fable `auditoria-fable/revision-pulido-19-oct.md`, «Tarea A»): las tomas que cambian, lado a lado.
«ANTES» sale del commit `c9dff5e` (las capturas de la Tarea 141, `git show`) y «DESPUÉS» de `docs/previews/app-actual/` ya regenerado (`AppActualScreenshotTest` + `tools/previews/app_actual.py`): capturas REALES de Roborazzi a 393 dp.
Hoja de una sola vez: su «DESPUÉS» sale de la carpeta viva, que cambió en la Tarea 147 (la lámina ya no es una cuadrícula ni se desliza; `hoy-lamina-juegos*` se borró): no volver a correrla; la hoja ya hecha queda como registro.
Uso:  python -I tools/previews/pulido_final_app.py      → docs/previews/pulido-final-app.png
"""
import io, os, subprocess, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
AFTER = os.path.join(ROOT, 'docs', 'previews', 'app-actual'); OUT = os.path.join(ROOT, 'docs', 'previews', 'pulido-final-app.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
BEFORE_COMMIT = 'c9dff5e'
NAVY, SUN, DIM, CREAM, LIME = (20, 18, 63), (255, 203, 69), (200, 196, 235), (245, 235, 219), (155, 229, 100)
W, GAP, PAIR_GAP, MARGIN, COLS = 330, 10, 40, 30, 3


def font(px, bold=True): return ImageFont.truetype(os.path.join(FONTS, 'fredoka.ttf' if bold else 'nunito_regular.ttf'), int(px))


# (captura de ANTES, captura de DESPUÉS, título, qué cambió). Las dos de la lámina de un área se muestran con la lámina bajada hasta «Tus juegos aquí» (la lámina se desplaza).
PAIRS = [
    ('hoy-lamina', 'hoy-lamina-juegos', 'La lámina, «Tus juegos aquí»', 'cuadrícula de fichas con emblema: sin cifras, «Nubi sugiere» en su ficha'),
    ('hoy-lamina-letra13', 'hoy-lamina-juegos-letra13', 'La lámina con letra 1,3', 'sin semanas; dos columnas, nombres enteros'),
    ('hoy-letra13', 'hoy-letra13', 'Hoy con letra 1,3', '«Seguir la ruta» antes de la lista; farolas del kit; «Después»'),
    ('avance-area', 'avance-area', 'Detalle de un área', 'la pregunta en el globo, «Jugar …» y la flecha «Avance»'),
    ('avance-area-letra13', 'avance-area-letra13', 'Detalle con letra 1,3', 'la escala en una frase y un juego a la vista'),
    ('ajustes', 'ajustes', 'Ajustes', 'una cabecera, «Tu nombre», «Cómo juegas», notas a 16 sp'),
    ('juegos-memoria', 'juegos-memoria', 'La ventana de un área', '«Para volver a jugar»'),
    ('perfil', 'perfil', 'Perfil', 'Nubi con tu nombre; escudo chico en «Tu liga»; «Etapa» y «medallas»'),
    ('primer-vuelo-punto', 'primer-vuelo-punto', 'Primer vuelo: tu punto de partida', 'barras del kit; «Paso 6 de 6» a secas'),
    ('resultado', 'resultado', 'Primera partida de un juego', 'solo «Hoy» y «Primera partida: juega otra vez…»'),
]


def before(name):
    raw = subprocess.run(['git', '-C', ROOT, 'show', '%s:docs/previews/app-actual/%s.png' % (BEFORE_COMMIT, name)], capture_output=True, check=True).stdout
    return Image.open(io.BytesIO(raw)).convert('RGB')


def scaled(im, h):
    k = h / im.height
    return im.resize((round(im.width * k), h), Image.LANCZOS)


tiles = []
for name_a, name_b, title, sub in PAIRS:
    a = before(name_a); b = Image.open(os.path.join(AFTER, name_b + '.png')).convert('RGB')
    h = int(W * a.height / a.width)
    tiles.append((title, sub, scaled(a, h), scaled(b, h)))
rowsN = (len(tiles) + COLS - 1) // COLS
pair_w = 2 * W + GAP
H_IMG = max(t[2].height for t in tiles)
LAB = 90
width = MARGIN * 2 + COLS * pair_w + (COLS - 1) * PAIR_GAP
height = 160 + rowsN * (H_IMG + LAB + 30) + 20
sheet = Image.new('RGB', (width, height), NAVY); d = ImageDraw.Draw(sheet)
d.text((MARGIN, 22), 'La ronda final de pulido de la app (Tarea 143): antes | después', font=font(52), fill=SUN)
d.text((MARGIN, 88), 'Capturas reales de Roborazzi a 393 × 873 dp. ANTES = las del 19-oct (commit %s); DESPUÉS = las de hoy. Las dos primeras, con la lámina bajada hasta «Tus juegos aquí».' % BEFORE_COMMIT, font=font(22, False), fill=DIM)
d.text((MARGIN, 118), 'Lo que cambia: letra grande, escala de etapas única, textos que ya no eran ciertos, la pregunta de los colores del Primer vuelo (quitada) y «Tus juegos aquí» como cuadrícula de emblemas.', font=font(22, False), fill=DIM)
for i, (title, sub, a, b) in enumerate(tiles):
    r, c = divmod(i, COLS)
    x = MARGIN + c * (pair_w + PAIR_GAP); y = 160 + r * (H_IMG + LAB + 30)
    for k, (im, tag, col) in enumerate(((a, 'ANTES', DIM), (b, 'DESPUÉS', LIME))):
        xx = x + k * (W + GAP)
        sheet.paste(im, (xx, y)); d.rectangle((xx, y, xx + im.width - 1, y + im.height - 1), outline=(70, 74, 130), width=1)
        d.text((xx, y + H_IMG + 6), tag, font=font(22), fill=col)
    d.text((x, y + H_IMG + 34), title, font=font(28), fill=CREAM)
    d.text((x, y + H_IMG + 68), sub, font=font(19, False), fill=DIM)
sheet.save(OUT, optimize=True); print('hoja:', OUT, sheet.size, os.path.getsize(OUT) // 1024, 'KB')
