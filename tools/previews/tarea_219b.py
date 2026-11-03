# -*- coding: utf-8 -*-
"""Lámina de la Tarea 219b (3-nov-2026): sin Nubilus Plus, con marca nueva y partidas que quedan, Hoy dice bajo «Otra vez» cuántas quedan (auditoría de Fable de la 219, §3; hueco de la tabla de `idea-pestana-contrarreloj.md` §2.2).
A TAMAÑO REAL (1 dp = 1 px). Fila de arriba: ANTES (solo el recuadro y «Otra vez»). Fila de abajo: DESPUÉS («Te quedan 2 partidas de hoy.» bajo «Otra vez», sin el récord, que ya está en el recuadro). Columnas: 393 dp con letra 1,0 y 1,3, y 360 dp con 1,0.
Las capturas salen de `Tarea219CapturasTest` (Roborazzi, `app/src/test/screenshots/t219/`; las de antes, en `t219/antes/`, fuera de git).
Uso:  python -I tools/previews/tarea_219b.py      → docs/previews/tarea-219b.png
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SHOTS = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots', 't219')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-219b.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
NAVY, SUN, DIM, LINE = (20, 18, 63), (255, 203, 69), (200, 196, 235), (90, 86, 160)
GAP, MARGIN, COLW = 36, 24, 393


def font(px, bold=True): return ImageFont.truetype(os.path.join(FONTS, 'fredoka.ttf' if bold else 'nunito_regular.ttf'), int(px))


def wrap(draw, text, f, width):
    lines, cur = [], ''
    for word in text.split(' '):
        t = (cur + ' ' + word).strip()
        if draw.textlength(t, font=f) <= width: cur = t
        else: lines.append(cur); cur = word
    if cur: lines.append(cur)
    return lines


def shot(name, dp_width=393, folder=None):
    path = os.path.join(folder or SHOTS, name + '.png')
    if not os.path.exists(path):
        im = Image.new('RGB', (dp_width, 300), (40, 36, 100)); ImageDraw.Draw(im).text((20, 120), 'falta ' + name, font=font(20), fill=SUN); return im
    im = Image.open(path).convert('RGB')
    return im.resize((dp_width, int(round(im.height * dp_width / im.width))), Image.LANCZOS)


ANTES = os.path.join(SHOTS, 'antes')
ROWS = [
    [('ANTES · 393 dp · letra 1,0', 'solo el recuadro y «Otra vez»: no dice cuántas partidas quedan (en «jugado» sí lo decía)', shot('hoy_sin_plus_marca_nueva_393_10', folder=ANTES)),
     ('ANTES · 393 dp · letra 1,3', 'lo último termina 8 dp antes de la barra', shot('hoy_sin_plus_marca_nueva_393_13', folder=ANTES)),
     ('ANTES · 360 dp · letra 1,0', 'a 360 dp Hoy ya se deslizaba (+71 dp)', shot('hoy_sin_plus_marca_nueva_360_10', 360, folder=ANTES))],
    [('DESPUÉS · 393 dp · letra 1,0', '«Te queda(n) N partidas de hoy.» bajo «Otra vez», en texto sobre el cielo y sin el récord (ya está en el recuadro); cabe con 28 dp de sobra', shot('hoy_sin_plus_marca_nueva_393_10')),
     ('DESPUÉS · 393 dp · letra 1,3', 'la línea suma unos 38 dp: lo último se pasa 30 dp de la barra y la línea queda bajo ella (se alcanza deslizando); la acción principal (el recuadro) sigue a la vista', shot('hoy_sin_plus_marca_nueva_393_13')),
     ('DESPUÉS · 360 dp · letra 1,0', 'se pasa 103 dp (antes 71): la línea queda bajo la barra y se alcanza deslizando; la acción principal (el recuadro) sigue a la vista', shot('hoy_sin_plus_marca_nueva_360_10', 360))],
]


def main():
    d0 = ImageDraw.Draw(Image.new('RGB', (10, 10)))
    sf = font(15, bold=False)
    cols = 3
    sheet_w = MARGIN * 2 + cols * COLW + (cols - 1) * GAP
    heights = []
    for row in ROWS:
        shots = max(im.height for _, _, im in row)
        cap = max(len(wrap(d0, t, sf, COLW)) * 20 for _, t, _ in row)
        heights.append(26 + shots + 12 + cap + 34)
    sheet = Image.new('RGB', (sheet_w, MARGIN + sum(heights)), NAVY)
    d = ImageDraw.Draw(sheet)
    y = MARGIN
    for row, hrow in zip(ROWS, heights):
        x = MARGIN
        for title, text, im in row:
            size = 17
            while size > 12 and d.textlength(title, font=font(size)) > COLW: size -= 1
            d.text((x, y), title, font=font(size), fill=SUN)
            sheet.paste(im, (x, y + 26))
            d.rectangle((x - 1, y + 25, x + im.width, y + 26 + im.height), outline=LINE)
            cy = y + 26 + im.height + 12
            for i, line in enumerate(wrap(d, text, sf, COLW)):
                d.text((x, cy + i * 20), line, font=sf, fill=DIM)
            x += COLW + GAP
        y += hrow
    sheet.save(OUT)
    print(OUT, sheet.size)


main()
