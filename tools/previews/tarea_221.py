# -*- coding: utf-8 -*-
"""Lámina de la Tarea 221 (3-nov-2026): «Tus últimas partidas» en Hoy, la opción A que eligió Ricardo (la tarjeta de alto propio, medido en filas, con la lista que se desliza por dentro).
A TAMAÑO REAL (1 dp = 1 px). Fila 1: la primera pantalla a 393 con letra 1,0 (ruta a medias y ruta completa) y Hoy deslizada hasta la tarjeta. Fila 2: la lista deslizada hasta el final, la primera pantalla y Hoy deslizada con letra 1,3. Fila 3: a 360 dp (primera pantalla, Hoy deslizada y la lista al final)
y con letra 2,0 en el emulador (`medium_phone`, la lista al principio y al final).
Las capturas salen de `Tarea221CapturasTest` (Roborazzi, `app/src/test/screenshots/t221/`) y del emulador (`emu_*`); todas fuera de git.
Uso:  python -I tools/previews/tarea_221.py      → docs/previews/tarea-221.png
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
T221 = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots', 't221')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-221.png')
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


def shot(name, dp_width=393):
    path = os.path.join(T221, name + '.png')
    if not os.path.exists(path):
        im = Image.new('RGB', (dp_width, 300), (40, 36, 100)); ImageDraw.Draw(im).text((20, 120), 'falta ' + name, font=font(20), fill=SUN); return im
    im = Image.open(path).convert('RGB')
    return im.resize((dp_width, int(round(im.height * dp_width / im.width))), Image.LANCZOS)


ROWS = [
    [('1 · la primera pantalla · ruta a medias', '393 × 873 · letra 1,0. Bajo el Contrarreloj asoma el título de la tarjeta y el borde de la primera fila: se ve que hay un historial y cómo se llama. Nada de arriba cambió.', shot('primera_ruta_a_medias_393_10')),
     ('1 · la primera pantalla · ruta completa', 'El estado de todos los días: el título y una fila entera y media. La tarjeta mide tres filas y MEDIA (el alto se mide en filas, nunca en dp fijos).', shot('primera_ruta_completa_393_10')),
     ('2 · el dedo sube: Hoy se desliza primero', 'Hoy se desliza hasta que la tarjeta queda ENTERA sobre la barra: tres filas enteras y la mitad de la cuarta (la señal de «hay más»). La lista de adentro todavía no se movió.', shot('deslizada_ruta_completa_393_10'))],
    [('3 · el dedo sigue subiendo: se mueve la lista', 'Con Hoy en su final, el mismo gesto desliza las filas por dentro hasta la décima, entera y con su aire. Al volver: primero la lista, después Hoy. La lista nunca se mueve con la tarjeta a medias.', shot('final_ruta_completa_393_10')),
     ('Ruta a medias · Hoy deslizada', 'La tarjeta entera sobre la barra, tras deslizar Hoy: la mitad de la cuarta fila cortada por el borde de abajo.', shot('deslizada_ruta_a_medias_393_10')),
     ('Con 1 a 3 partidas · alto natural', 'Sin cuarta fila no hay qué asomar: la tarjeta mide lo que miden sus filas, sin recorte ni deslizado por dentro. Sin partidas no hay tarjeta.', shot('dos_partidas_393_10'))],
    [('Letra 1,3 · primera pantalla', '393 × 873 · ruta a medias. La tarjeta crece con la letra (las filas pasan a tres renglones) y sigue mostrando tres filas y media.', shot('primera_ruta_a_medias_393_13')),
     ('Letra 1,3 · Hoy deslizada', 'La tarjeta, entera sobre la barra: más alta (unos 308 dp de lista contra 224), las mismas tres filas y media.', shot('deslizada_ruta_a_medias_393_13')),
     ('360 dp · letra 1,0 · primera pantalla', 'A 360 dp Hoy ya se deslizaba: el título baja de la barra y la tarjeta se alcanza deslizando.', shot('primera_ruta_a_medias_360_10', 360))],
    [('360 dp · Hoy deslizada', 'La tarjeta entera sobre la barra: la cuarta fila queda cortada a la mitad.', shot('deslizada_ruta_a_medias_360_10', 360)),
     ('360 dp · la lista al final', 'Deslizada hasta la décima fila.', shot('final_ruta_a_medias_360_10', 360)),
     ('Emulador · 393 dp · letra 2,0', 'Con letra 2,0 la tarjeta mide más de 480 dp y la ventana sigue mostrando tres filas y media: ninguna palabra se parte ni se encima.', shot('emu_hoy_393_20_lista_inicio'))],
    [('Emulador · 393 dp · letra 2,0 · el final', 'La lista deslizada hasta su final con letra 2,0.', shot('emu_hoy_393_20_lista_final')),
     ('Emulador · 360 dp · letra 2,0', 'A 360 dp con letra 2,0, la lista al principio.', shot('emu_hoy_360_20_lista_inicio', 360)),
     ('Emulador · 360 dp · letra 2,0 · el final', 'Y deslizada hasta el final.', shot('emu_hoy_360_20_lista_final', 360))],
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
    head = 70
    sheet = Image.new('RGB', (sheet_w, MARGIN + head + sum(heights)), NAVY)
    d = ImageDraw.Draw(sheet)
    d.text((MARGIN, MARGIN), 'Tarea 221 · «Tus últimas partidas» en Hoy (la opción A)', font=font(26), fill=SUN)
    sub = 'La tarjeta muestra tres filas y media y se desliza por dentro. Hacia arriba el dedo mueve Hoy primero y después la lista; hacia abajo, la lista primero y después Hoy. A tamaño real (1 dp = 1 px).'
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
