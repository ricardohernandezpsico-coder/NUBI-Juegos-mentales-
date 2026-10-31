# -*- coding: utf-8 -*-
"""Lámina «hoy | propuesta» de la Tarea 197 (30-oct-2026): el velo detrás de la tarjeta «Nuevo» de Bodega, Engranajes y Constelaciones. Capturas REALES de los juegos en Unity (`ScreenshotRunner`, modo NUBI_SHOTS_NUEVO=1: el juego real, a 1080 × 2400, la pantalla entera con el velo, la tarjeta,
el tablero y el cielo); `hoy` = el velo de antes (un disco estirado a toda la pantalla: una elipse oscura), `prop` = el velo liso sobre toda la pantalla. Las capturas viven en `unity/test-results/capturas/tarea-197/{hoy,prop}/` (fuera de git; se sacan con
`NUBI_SHOTS_NUEVO=1 NUBI_SHOTS_GAMES=bodega,engranajes,parejas NUBI_SHOTS_DIR=<carpeta> Unity -batchmode -executeMethod NeuroVida.Bridge.EditorTools.ScreenshotRunner.Run`, sin -nographics).
Uso:  python -I tools/previews/tarea_197.py      → docs/previews/tarea-197.png
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SHOTS = os.path.join(ROOT, 'unity', 'test-results', 'capturas', 'tarea-197')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-197.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
NAVY, SUN, DIM, CREAM = (20, 18, 63), (255, 203, 69), (200, 196, 235), (245, 235, 219)
W, H, GAP, PAIR_GAP, MARGIN = 360, 800, 12, 40, 24       # la captura es de 1080 × 2400: aquí a un tercio

# (captura, título, qué se ve)
CELLS = [
    ('bodega_nuevo-2', 'Bodega de carga · «Ahora el robot cambia una caja de lugar»',
     'la tarjeta que vio Ricardo en su teléfono. Antes, un óvalo oscuro enorme detrás de la tarjeta dejaba ver el tablero y el cielo claros en las cuatro esquinas; ahora el mismo tono liso cubre toda la pantalla, de borde a borde. Misma tarjeta, mismo texto, mismo «Seguir»'),
    ('engranajes_nuevo-1', 'Engranajes · «La máquina del cohete viene mal armada»',
     'el mismo defecto: el óvalo dejaba ver el cohete y los botones de abajo en las esquinas con otra luz que en el centro. Ahora el velo es parejo. La tarjeta no cambió'),
    ('parejas_nuevo-1', 'Constelaciones · «¡Llegan gemelos parecidos!»',
     'igual: antes, esquinas claras y centro oscuro; ahora, un solo tono sobre toda la pantalla. La tarjeta no cambió'),
]


def font(px, bold=True): return ImageFont.truetype(os.path.join(FONTS, 'fredoka.ttf' if bold else 'nunito_regular.ttf'), int(px))


def wrap(draw, text, f, width):
    lines, cur = [], ''
    for word in text.split(' '):
        t = (cur + ' ' + word).strip()
        if draw.textlength(t, font=f) <= width: cur = t
        else: lines.append(cur); cur = word
    if cur: lines.append(cur)
    return lines


def main():
    d0 = ImageDraw.Draw(Image.new('RGB', (10, 10)))
    tf, sf = font(17), font(15, bold=False)
    pair_w = 2 * W + GAP
    sheet_w = MARGIN * 2 + len(CELLS) * pair_w + PAIR_GAP * (len(CELLS) - 1)
    cap = max(len(wrap(d0, t, sf, pair_w - 4)) * 20 for _, _, t in CELLS)
    title = max(len(wrap(d0, tt, tf, pair_w - 4)) * 22 for _, tt, _ in CELLS)
    sheet = Image.new('RGB', (sheet_w, MARGIN + title + 26 + 24 + H + 12 + cap + 24), NAVY)
    d = ImageDraw.Draw(sheet)
    x0 = MARGIN
    for key, tt, text in CELLS:
        for i, line in enumerate(wrap(d, tt, tf, pair_w - 4)): d.text((x0, MARGIN + i * 22), line, font=tf, fill=SUN)
        y = MARGIN + title
        for j, (pre, label) in enumerate((('hoy', 'HOY'), ('prop', 'PROPUESTA'))):
            x = x0 + j * (W + GAP)
            d.text((x, y), label, font=font(15), fill=CREAM if pre == 'prop' else DIM)
            im = Image.open(os.path.join(SHOTS, pre, key + '.png')).convert('RGB')
            sheet.paste(im.resize((W, H), Image.LANCZOS), (x, y + 24))
            d.rectangle((x - 1, y + 23, x + W, y + 24 + H), outline=(90, 86, 160))
        for i, line in enumerate(wrap(d, text, sf, pair_w - 4)): d.text((x0, y + 24 + H + 12 + i * 20), line, font=sf, fill=DIM)
        x0 += pair_w + PAIR_GAP
    sheet.save(OUT)
    print(OUT, sheet.size)


main()
