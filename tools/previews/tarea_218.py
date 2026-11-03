# -*- coding: utf-8 -*-
"""Lámina de la Tarea 218 (3-nov-2026): la regla de partidas del Contrarreloj (`auditoria-fable/idea-pestana-contrarreloj.md` §4, §5 y §7; tarea 3 de las tres de esa idea).
A TAMAÑO REAL (1 dp = 1 px). Fila 1: la ficha del juego de hoy con partidas («Te quedan 2 partidas de hoy.»), la de otro juego (gris con candado, abre la hoja), la del de hoy sin partidas (sin botón) y la hoja del candado con sus cuatro líneas.
Fila 2: la pestaña con y sin partidas, y Hoy sin partidas (la frase sobre el cielo, UNA fila gris con candado) también con marca nueva. Fila 3: el final sin «Otra vez» y, en el emulador con letra 2,0 a 393 dp, Hoy y la pestaña sin partidas y la ficha gris.
Fila 4: lo más angosto con la letra más grande: 360 dp con 2,0 (emulador) y la pestaña con partidas a 393/2,0. Las capturas salen de `Tarea218CapturasTest` (Roborazzi, `app/src/test/screenshots/t218/`, fuera de git) y del emulador `medium_phone` (`emu_*.png`).
Uso:  python -I tools/previews/tarea_218.py      → docs/previews/tarea-218.png
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SHOTS = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots', 't218')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-218.png')
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


def shot(name, dp_width=393, crop_dp=None):
    path = os.path.join(SHOTS, name + '.png')
    if not os.path.exists(path):
        im = Image.new('RGB', (dp_width, 300), (40, 36, 100)); ImageDraw.Draw(im).text((20, 120), 'falta ' + name, font=font(20), fill=SUN); return im
    im = Image.open(path).convert('RGB')
    im = im.resize((dp_width, int(round(im.height * dp_width / im.width))), Image.LANCZOS)
    return im.crop((0, 0, dp_width, crop_dp)) if crop_dp else im


ROWS = [
    [('la ficha del juego de hoy · con partidas', 'sin Plus, el botón «Contrarreloj» dice «Te quedan 2 partidas de hoy.» en su segunda línea; con Plus, «Tu récord: N aciertos.» o «2 minutos», como siempre', shot('ficha_hoy_con_partidas_393_10')),
     ('la ficha de otro juego · sin Plus', 'el botón va en GRIS con candado, «Contrarreloj» y «Con Nubilus Plus» en su segunda línea; no lanza nada: abre la hoja del candado', shot('ficha_gris_393_10')),
     ('la ficha del juego de hoy · sin partidas', 'el botón no sale y queda «Hoy ya jugaste tus 3 partidas.»; nada gris en la ficha (la fila gris es solo de Hoy y de la pestaña)', shot('ficha_sin_partidas_393_10')),
     ('la hoja del candado · cuatro líneas', 'abre con «Gratis: el Contrarreloj de cada día, con 3 partidas, tu récord y tu división de la semana.»; la primera línea de lo que agrega Plus: «Los cuatro Contrarrelojes, cualquier día, con partidas libres.»', shot('hoja_393_10'))],
    [('la pestaña · con partidas', '«Jugar · 2 minutos» lleva debajo, dentro de la losa gratis, «Te quedan 2 partidas de hoy.»', shot('pestana_con_partidas_393_10')),
     ('la pestaña · sin partidas', 'en lugar de «Jugar», «Hoy ya jugaste tus 3 partidas.»; el récord, la semana y la división siguen; y bajo la losa gratis UNA fila gris con candado', shot('pestana_sin_partidas_393_10')),
     ('Hoy · sin partidas', 'el botón «Contrarreloj» no sale: en texto sobre el cielo «Hoy ya jugaste tus 3 partidas. Mañana: ¿Verdad o disparate?», UNA fila gris con candado y «Todos los juegos»', shot('hoy_sin_partidas_393_10')),
     ('Hoy · marca nueva y sin partidas', 'el recuadro de la marca nueva queda arriba; sin «Otra vez»', shot('hoy_marca_nueva_sin_partidas_393_10'))],
    [('el final · sin partidas', '«Otra vez» no sale y va «Mañana hay otro Contrarreloj.»; nada gris en el final', shot('final_sin_partidas_393_10')),
     ('393 dp · letra 2,0 · Hoy sin partidas (emulador)', 'la frase pasa a tres renglones por palabras; la fila gris, a tres; «Todos los juegos» al final', shot('emu_hoy_393_20_c')),
     ('393 dp · letra 2,0 · pestaña sin partidas (emulador)', '«Hoy ya jugaste / 3 partidas.»: «3 partidas» queda junto y no sobra una palabra sola', shot('emu_tab_393_20_a')),
     ('393 dp · letra 2,0 · ficha gris (emulador)', 'el botón gris con candado, entero; «Con Nubilus Plus» en su segunda línea', shot('emu_ficha_gris_393_20_b'))],
    [('360 dp · letra 2,0 · Hoy sin partidas (emulador)', 'lo más angosto con la letra más grande: nada se corta ni se encima', shot('emu_hoy_360_20', 360)),
     ('360 dp · letra 2,0 · pestaña sin partidas (emulador)', 'la losa gratis con la frase y la fila gris debajo', shot('emu_tab_360_20', 360)),
     ('360 dp · letra 2,0 · ficha gris (emulador)', 'el botón gris con candado; abre la hoja, que se desliza y «Cerrar» se alcanza', shot('emu_ficha_gris_360_20', 360)),
     ('393 dp · letra 2,0 · pestaña con partidas (emulador)', '«Te quedan / 2 partidas de hoy.» bajo «Jugar»; «de hoy» no queda solo', shot('emu_tab_con_393_20'))],
]


def main():
    d0 = ImageDraw.Draw(Image.new('RGB', (10, 10)))
    sf = font(15, bold=False)
    sheet_w = MARGIN * 2 + 4 * COLW + 3 * GAP
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
