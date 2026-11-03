# -*- coding: utf-8 -*-
"""Lámina de la Tarea 221b (3-nov-2026): «Tus últimas partidas» con la segunda línea toda en minúscula salvo los nombres propios (`RetoChoice.modeWords`: «en tu ruta», «suave», «a tu medida», «en Desafío», «al límite»; «Contrarreloj» con mayúscula) y Freno de Emergencia SIN marca
(su medida es un promedio, no una partida: la línea termina en el modo). A 393 dp con letra 1,0, A TAMAÑO REAL (1 dp = 1 px), antes (columna izquierda) y después (derecha), con la lista al principio (arriba) y al final (abajo): una fila de cada modo (ruta, Suave, Desafío, Al límite y Contrarreloj) y una de Freno.
Las capturas salen de `Tarea221CapturasTest` (`modos_*`, Roborazzi, `app/src/test/screenshots/t221/`; fuera de git).
Uso:  python -I tools/previews/tarea_221b.py      → docs/previews/tarea-221b.png
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
T221 = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots', 't221')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-221b.png')
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


def shot(name):
    """La captura a su ancho en dp, recortada a la tarjeta (de abajo del Contrarreloj hasta la barra)."""
    path = os.path.join(T221, name + '.png')
    if not os.path.exists(path):
        im = Image.new('RGB', (COLW, 300), (40, 36, 100)); ImageDraw.Draw(im).text((20, 120), 'falta ' + name, font=font(20), fill=SUN); return im
    im = Image.open(path).convert('RGB')
    im = im.resize((COLW, int(round(im.height * COLW / im.width))), Image.LANCZOS)
    return im.crop((0, int(im.height * 0.40), COLW, int(im.height * 0.90)))


ROWS = [
    [('ANTES · la lista al principio', 'El modo con mayúscula («En tu ruta», «Desafío»): la línea mezclaba la voz del botón «Jugar · en Desafío» con la de una etiqueta.', shot('modos_antes_inicio_393_10')),
     ('DESPUÉS · la lista al principio', 'Toda la línea en minúscula salvo los nombres propios: «hoy · en tu ruta · 70 % de las cargas, sin pista», «ayer · en Desafío · …», «hoy · Contrarreloj · 38 aciertos». Es la forma del botón «Jugar · en Desafío · con reloj» de la ficha (`RetoChoice.modeWords`).', shot('modos_despues_inicio_393_10'))],
    [('ANTES · la lista al final', 'Freno de Emergencia con la zona de ESA partida («zona firme»): la app siempre dice el freno como el promedio de las últimas partidas, y una partida suelta podía contradecir su propio final.', shot('modos_antes_final_393_10')),
     ('DESPUÉS · la lista al final', 'Freno sin marca: la línea termina en el modo («hace 2 días · a tu medida»). Una fila de cada modo: ruta, Suave, Desafío, Al límite y Contrarreloj. Lo que lee el lector dice lo mismo que la pantalla.', shot('modos_despues_final_393_10'))],
]


def main():
    d0 = ImageDraw.Draw(Image.new('RGB', (10, 10)))
    sf = font(15, bold=False)
    cols = 2
    sheet_w = MARGIN * 2 + cols * COLW + (cols - 1) * GAP
    heights = []
    for row in ROWS:
        shots = max(im.height for _, _, im in row)
        cap = max(len(wrap(d0, t, sf, COLW)) * 20 for _, t, _ in row)
        heights.append(26 + shots + 12 + cap + 34)
    head = 70
    sheet = Image.new('RGB', (sheet_w, MARGIN + head + sum(heights)), NAVY)
    d = ImageDraw.Draw(sheet)
    d.text((MARGIN, MARGIN), 'Tarea 221b · «Tus últimas partidas»: la línea en minúscula y Freno sin marca', font=font(24), fill=SUN)
    sub = '393 dp · letra 1,0 · a tamaño real (1 dp = 1 px). Antes a la izquierda, después a la derecha; arriba la lista al principio, abajo al final.'
    for i, line in enumerate(wrap(d, sub, sf, sheet_w - 2 * MARGIN)): d.text((MARGIN, MARGIN + 38 + i * 20), line, font=sf, fill=DIM)
    y = MARGIN + head
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
