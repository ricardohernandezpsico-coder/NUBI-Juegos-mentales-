# -*- coding: utf-8 -*-
"""Lámina de la Tarea 215b (2-nov-2026): lo que Fable pidió corregir en la auditoría de la 215 (`auditoria-fable/contrarreloj-t215-auditoria.md` §3.1: A1, A3, A4 b y c, M1, M2 y M5).
A TAMAÑO REAL (1 dp = 1 px). Fila 1: la hoja del candado con la referencia a 393 dp con letra 1,0 (ya no promete lo que la losa crema de la pestaña da gratis), sin comparación y a 360 dp con letra 2,0.
Fila 2: la losa gratis en tres casos (la mejor marca de la semana ES el récord, la mejor es otra, el juego sin marcas) y la pestaña sin Plus antes de la tercera partida.
Las capturas salen de `Tarea215bCapturasTest` (`app/src/test/screenshots/t215b/`, fuera de git).
Uso:  python -I tools/previews/tarea_215b.py      → docs/previews/tarea-215b.png
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SHOTS = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots', 't215b')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-215b.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
NAVY, SUN, DIM, LINE = (20, 18, 63), (255, 203, 69), (200, 196, 235), (90, 86, 160)
GAP, MARGIN, COLW = 36, 24, 393
TAB_CROP = 736   # dp: la pestaña sin la barra de abajo


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
    [('la hoja del candado · con la referencia · 393 dp', 'ya no promete jugar ni el récord ni la mejor de la semana: lo dice la losa crema, 200 dp más arriba; la hoja abre con «Gratis: jugar, tu récord y tu división de la semana.» y sigue con lo que de verdad agrega Plus', shot('hoja_referencia_393_10')),
     ('la hoja · sin comparación · 393 dp', 'un juego sin tabla: «Gratis: jugar, tu récord y tu semana.» y, primera de la lista, las últimas 10 marcas en barras «con la fecha y el paso de tu récord», no «tu récord, tu mejor marca»', shot('hoja_sin_comparacion_393_10')),
     ('la hoja · con la referencia · 360 dp · letra 2,0', 'con letra 2,0 la hoja llena la pantalla y se desliza (la captura la muestra cortada abajo: no se desliza en Roborazzi); «Cerrar» se alcanza al final', shot('hoja_referencia_360_20', 360))],
    [('lo gratis · la mejor semana ES el récord', '«Tu récord: 52 aciertos.» y «Esta semana jugaste 3 partidas y la mejor fue tu récord.»: el número no se repite (ya lo dicen el récord y la losa del selector)', shot('gratis_record_es_la_mejor_393_10', crop_dp=TAB_CROP)),
     ('lo gratis · la mejor semana es otra', '«Tu récord: 60 aciertos.» y «…y la mejor fue de 48 aciertos.», con «y» en vez del punto y coma', shot('gratis_mejor_distinta_393_10', crop_dp=TAB_CROP)),
     ('lo gratis · el juego sin marcas', 'UNA línea, «Todavía no tienes marcas en este juego.»: sin «récord» ni la semana, que eran tres negaciones seguidas, y sin la losa vacía de antes', shot('gratis_sin_marcas_393_10', crop_dp=TAB_CROP)),
     ('sin Plus · antes de la tercera partida', 'la línea «Tu lugar aparece desde tu tercera partida…» y lo gratis con récord y semana (con 2 partidas, la mejor es el récord); el lector dice «Jugar 2 minutos de Tinta o Palabra» y las losas grises, su título y su frase', shot('antes_de_la_tercera_393_10', crop_dp=TAB_CROP))],
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
