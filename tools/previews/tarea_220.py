# -*- coding: utf-8 -*-
"""Lámina de la Tarea 220 (3-nov-2026): la pestaña «Juegos» y la barra de cinco (Hoy · Juegos · Contrarreloj · Avance · Tú); «Todos los juegos» sale de Hoy.
A TAMAÑO REAL (1 dp = 1 px). Filas 1 y 2: la barra a 393 y 360 dp con letra 1,0 y 1,3 (Roborazzi) y con letra 2,0 (emulador `medium_phone`, el recorte de abajo). Fila 3: la pestaña Juegos, Hoy ANTES (con la losa) y AHORA (ruta a medias). Fila 4: Hoy ANTES y AHORA con la ruta completa y el paso 2 de la guía.
Las capturas salen de `Tarea220CapturasTest` (Roborazzi, `app/src/test/screenshots/t220/`), de `Tarea219CapturasTest` (las de ANTES, `t219/`) y del emulador (`emu_*`); todas fuera de git.
Uso:  python -I tools/previews/tarea_220.py      → docs/previews/tarea-220.png
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SHOTS = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots')
T220 = os.path.join(SHOTS, 't220'); T219 = os.path.join(SHOTS, 't219')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-220.png')
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


def shot(folder, name, dp_width=393, crop_bottom_dp=None, density=None):
    """La captura a su ancho en dp. `crop_bottom_dp`: se queda con los últimos N dp de arriba hacia abajo (la barra), sin la franja de gestos de Android (`gesture_dp`)."""
    path = os.path.join(folder, name + '.png')
    if not os.path.exists(path):
        im = Image.new('RGB', (dp_width, 300), (40, 36, 100)); ImageDraw.Draw(im).text((20, 120), 'falta ' + name, font=font(20), fill=SUN); return im
    im = Image.open(path).convert('RGB')
    im = im.resize((dp_width, int(round(im.height * dp_width / im.width))), Image.LANCZOS)
    if crop_bottom_dp:
        gesture = 24 if name.startswith('emu_') else 0                       # la franja de gestos del sistema (el emulador la dibuja; Roborazzi no)
        im = im.crop((0, im.height - crop_bottom_dp - gesture, im.width, im.height - gesture))
    return im


ROWS = [
    [('393 dp · letra 1,0', 'Hoy · Juegos · Contrarreloj · Avance · Tú: los cinco rótulos (16 sp, negrita) caben con su aire de 12 dp y ninguna columna baja de 48 dp. Juegos lleva el ícono de las cuatro casillas.', shot(T220, 'barra_393_10', 393)),
     ('393 dp · letra 1,3', 'Con las medidas de este teléfono simulado los cinco necesitan 390 dp de 393: el tope de los rótulos queda en 1,3. Con las de un Pixel 8 necesitan 394 dp y el tope baja solo a 1,2: siempre el mayor con que quepan.', shot(T220, 'barra_393_13', 393)),
     ('360 dp · letra 1,0', 'Los cinco necesitan unos 338 dp de 360: caben con 22 dp de sobra.', shot(T220, 'barra_360_10', 360))],
    [('360 dp · letra 1,3 (tope 1,1)', 'Con 1,3 los cinco pedirían 390 dp y no caben en 360. Sin tope, Hoy y Tú bajarían a 45 dp de ancho (menos que el toque) y «Contrarreloj» quedaría sin aire: el tope de los rótulos cae a 1,1 (necesitan 354 dp) y todo conserva su aire y sus 48 dp.', shot(T220, 'barra_360_13', 360)),
     ('Emulador · 393 dp · letra 2,0', 'Con letra 2,0 los rótulos no pasan del tope (1,3 aquí): una sola línea, ninguna palabra partida ni recortada.', shot(T220, 'emu_hoy_393_20', 393, crop_bottom_dp=100)),
     ('Emulador · 360 dp · letra 2,0', 'A 360 dp el tope es 1,1: los cinco rótulos en una línea y la elegida (Juegos) cabe en su losa.', shot(T220, 'emu_juegos_360_20', 360, crop_bottom_dp=100))],
    [('La pestaña Juegos · 393 dp', 'Título «Juegos» (26 sp, como «Avance» y «Tú»), sin la X ni Atrás propio, y la franja de la barra libre abajo. Buscador, chips de área y filas, sin cambios. La búsqueda, el chip y el deslizado se quedan al cambiar de pestaña.', shot(T220, 'juegos_393_10', 393)),
     ('Hoy ANTES · ruta a medias', 'La losa crema «Todos los juegos» (56 dp) entre la tarjeta y el Contrarreloj.', shot(T219, 'hoy_ruta_a_medias_393_10', 393)),
     ('Hoy AHORA · ruta a medias', 'La losa se fue: el Contrarreloj sube 70 dp (56 de cuerpo + 6 de sombra + 8 de aire; no 64). Nada más cambia.', shot(T220, 'hoy_ruta_a_medias_393_10', 393))],
    [('Hoy ANTES · ruta completa', 'Con la ruta hecha, «Todos los juegos» y el Contrarreloj bajo la tarjeta.', shot(T219, 'hoy_ruta_completa_393_10', 393)),
     ('Hoy AHORA · ruta completa', 'El Contrarreloj sube 70 dp: sobran 173 dp entre él y la barra (el lugar de «Tus últimas partidas», Tarea 221).', shot(T220, 'hoy_ruta_completa_393_10', 393)),
     ('La guía · paso 2 de 3', 'El hueco ilumina la COLUMNA Juegos de la barra, no un botón de Hoy. El globo: «Aquí están los 17 juegos: eliges el que quieras.»', shot(T220, 'guia_paso2_393_10', 393))],
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
    d.text((MARGIN, MARGIN), 'Tarea 220 · la pestaña Juegos y la barra de cinco', font=font(26), fill=SUN)
    d.text((MARGIN, MARGIN + 38), '«Todos los juegos» sale de Hoy y vuelve a ser pestaña. A tamaño real (1 dp = 1 px). El tope de la letra de los rótulos es el mayor de 1,3 · 1,2 · 1,1 · 1,0 con que quepan los cinco.', font=sf, fill=DIM)
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
