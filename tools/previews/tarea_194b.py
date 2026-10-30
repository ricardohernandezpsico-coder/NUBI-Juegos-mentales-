# -*- coding: utf-8 -*-
"""Lámina «hoy | propuesta» de la Tarea 194b (30-oct-2026): el cierre del Primer vuelo («Paso 6 de 6», «Tu primer vuelo fue tu ruta de hoy») armado como «¿Sabías que…?». Capturas REALES de Roborazzi (`CierreScreenshotTest`;
`hoy` = el código de antes, `prop` = el nuevo) a 393 × 873 dp y tamaño real (1 px = 1 dp), con letra 1,0, 1,3 y 2,0; sobre cada par, cuántos dp quedan entre la frase y el botón (se miden de las capturas).
Uso:  python -I tools/previews/tarea_194b.py      → docs/previews/tarea-194b.png
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SHOTS = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots', 'cierre')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-194b.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
NAVY, SUN, DIM, CREAM = (20, 18, 63), (255, 203, 69), (200, 196, 235), (245, 235, 219)
W, H, GAP, PAIR_GAP, MARGIN = 393, 873, 12, 40, 24

# (clave de la captura, título del par, qué cambió)
COLS = [
    ('10', 'letra 1,0', 'el bloque (Nubi, título y frase) queda centrado en el alto que hay entre «Paso 6 de 6» y el botón, como «¿Sabías que…?»: el título empieza en 446 dp (antes, en unos 641 dp) y queda aire sobre el botón; antes Nubi se estiraba y empujaba las dos líneas hasta pegarlas a él'),
    ('13', 'letra 1,3', 'igual: el bloque centrado; con la letra 1,3 la frase pasa a tres líneas y nada se corta ni lo tapa el botón'),
    ('20', 'letra 2,0', 'con la letra 2,0 el bloque entra entero sin deslizar (antes la frase se veía cortada contra el botón, y había que deslizar para leerla); si en otra pantalla no entra, la página se desliza como las demás y el botón queda siempre abajo'),
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
    sheet_w = MARGIN * 2 + len(COLS) * pair_w + (len(COLS) - 1) * PAIR_GAP
    cap_h = max(len(wrap(d0, t, sf, pair_w - 4)) * 20 for _, _, t in COLS)
    sheet_h = MARGIN + 26 + 24 + H + 12 + cap_h + 24
    sheet = Image.new('RGB', (sheet_w, sheet_h), NAVY)
    d = ImageDraw.Draw(sheet)
    for k, (key, title, text) in enumerate(COLS):
        x0 = MARGIN + k * (pair_w + PAIR_GAP)
        y = MARGIN
        d.text((x0, y), '6 · el cierre del Primer vuelo · ' + title, font=tf, fill=SUN)
        for j, (pre, label) in enumerate((('hoy', 'HOY'), ('prop', 'PROPUESTA'))):
            x = x0 + j * (W + GAP)
            d.text((x, y + 26), label, font=font(15), fill=CREAM if pre == 'prop' else DIM)
            im = Image.open(os.path.join(SHOTS, '%s_%s.png' % (pre, key))).convert('RGB')
            sheet.paste(im.resize((W, H), Image.LANCZOS), (x, y + 50))
            d.rectangle((x - 1, y + 49, x + W, y + 50 + H), outline=(90, 86, 160))
        cy = y + 50 + H + 12
        for i, line in enumerate(wrap(d, text, sf, pair_w - 4)):
            d.text((x0, cy + i * 20), line, font=sf, fill=DIM)
    sheet.save(OUT)
    print(OUT, sheet.size)


main()
