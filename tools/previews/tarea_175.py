# -*- coding: utf-8 -*-
"""Lámina de la Tarea 175, «Rubik en los títulos de toda la app» (`docs/previews/tarea-175.png`), con CAPTURAS REALES del emulador (API 36, 1080 × 2400) y de Roborazzi (393 × 873 dp); no son maquetas.
  1. Con letra 1,0, «hoy | propuesta» de cada pantalla: Hoy, Todos los juegos, la ficha de un juego, «Sobre este juego», Tú, Ajustes, el final de una partida, el resumen de la ruta y el Primer vuelo («hoy» es la app de la Tarea 174, con los títulos en Fredoka).
  2. Hoy y la ficha con letra 1,3 y 2,0: ningún título se parte por la mitad ni empuja algo fuera de la pantalla.
Entradas: `app/src/test/screenshots/tarea-175-emulador/*.png` (`tools/emulador_ui.py`, solo el emulador) y `app/src/test/screenshots/tarea-175/*.png` (final y resumen, Roborazzi; «hoy» se sacó con `TitleFamily` apuntando un momento a Fredoka).
Uso:  python -I tools/previews/tarea_175.py
"""
import os, sys, textwrap
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
EMU = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots', 'tarea-175-emulador')
ROB = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots', 'tarea-175')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-175.png')
NAVY, SUN, DIM, CREAM = (20, 18, 63), (255, 203, 69), (200, 196, 235), (245, 235, 219)
GAP = 36
W_PH = 300                                   # ancho de cada captura
H_PH = round(2400 * W_PH / 1080)             # alto de una captura del emulador (1080 × 2400)
PAIR = 2 * W_PH + 14


def font(px, bold=True, rubik=False):
    return ImageFont.truetype(os.path.join(FONTS, 'rubik_bold.ttf' if rubik else ('fredoka.ttf' if bold else 'nunito_regular.ttf')), int(px))


def load(path): return Image.open(path).convert('RGB') if os.path.exists(path) else None


def fit_w(im, w): return im.resize((w, round(im.height * w / im.width)), Image.LANCZOS)


def wrap(d, x, y, text, f, fill, width_chars, step):
    for line in textwrap.wrap(text, width_chars):
        d.text((x, y), line, font=f, fill=fill); y += step
    return y


def shot(sheet, d, x, y, hoy, prop, caption):
    """Una pareja «hoy | propuesta»; las capturas altas (el final, 6000 px) se recortan arriba."""
    for j, p in enumerate((hoy, prop)):
        im = load(p)
        if im is None: continue
        if im.height > 2600: im = im.crop((0, 0, im.width, round(im.width * 2400 / 1080)))
        sheet.paste(fit_w(im, W_PH), (x + j * (W_PH + 14), y))
    d.text((x, y + H_PH + 6), caption + ' · hoy', font=font(24), fill=DIM)
    d.text((x + W_PH + 14, y + H_PH + 6), 'propuesta', font=font(24), fill=CREAM)


def build():
    cols = 4
    W = 2 * GAP + cols * PAIR + (cols - 1) * 40
    ROW = H_PH + 70
    H = 330 + 3 * ROW + 90 + 2 * 20 + (ROW + 60)
    sheet = Image.new('RGB', (W, H), NAVY); d = ImageDraw.Draw(sheet)
    d.text((GAP, 24), 'Rubik en los títulos de toda la app (Tarea 175)', font=font(64, rubik=True), fill=SUN)
    y = 112
    for line in ('Capturas reales (emulador API 36 y Roborazzi). Ricardo probó Rubik Bold en Avance (Tarea 174, «me convence») y la aprobó para el resto de la app: un TÍTULO es el texto que nombra una pantalla, una sección, una tarjeta o una lámina.',
                 'No cambian los botones de arcilla ni su segunda línea (Fredoka), las cifras grandes («668», «5 luces»), los chips, las etiquetas, las pestañas ni el cuerpo (Nunito). Los juegos de Unity no se tocan.'):
        d.text((GAP, y), line, font=font(24, False), fill=DIM); y += 34
    y = 212
    d.text((GAP, y), '1 · Con letra 1,0, hoy | propuesta', font=font(40, rubik=True), fill=SUN)
    y += 62
    e = lambda pre, n: os.path.join(EMU, f'{pre}-{n}.png')
    r = lambda pre, n: os.path.join(ROB, f'{pre}-{n}.png')
    fila1 = [('hoy-1_0', 'Hoy'), ('todos-1_0', 'Todos los juegos'), ('ficha-1_0', 'Ficha'), ('sobre-1_0', 'Sobre este juego')]
    for k, (n, cap) in enumerate(fila1):
        shot(sheet, d, GAP + k * (PAIR + 40), y, e('hoy', n), e('propuesta', n), cap)
    y += ROW
    fila2 = [(e('hoy', 'tu-1_0'), e('propuesta', 'tu-1_0'), 'Tú'), (e('hoy', 'ajustes-1_0'), e('propuesta', 'ajustes-1_0'), 'Ajustes'),
             (r('hoy', 'final'), r('propuesta', 'final'), 'Final de partida'), (r('hoy', 'resumen'), r('propuesta', 'resumen'), 'Resumen de la ruta')]
    for k, (a, b, cap) in enumerate(fila2):
        shot(sheet, d, GAP + k * (PAIR + 40), y, a, b, cap)
    y += ROW
    shot(sheet, d, GAP, y, e('hoy', 'vuelo-1_0'), e('propuesta', 'vuelo-1_0'), 'Primer vuelo')
    xt = GAP + PAIR + 70
    d.text((xt, y), 'Qué pasa a Rubik Bold', font=font(32, rubik=True), fill=SUN); yy = y + 50
    for linea in ('El saludo de Hoy y «Tu ruta de hoy»; «Todos los juegos» y el nombre de cada área; el nombre del juego en la ficha, en «Sobre este juego» y en el final, con sus títulos de sección («Qué pones en juego», «Truco para la próxima»).',
                  'Los títulos de Tú, de Ajustes y de Licencias; «¡Ruta de hoy completa!», «Tus juegos» y «Tu avance»; los del Primer vuelo, los diálogos y la medalla nueva; los de Avance, que ya lo tenían.'):
        yy = wrap(d, xt, yy, '·  ' + linea, font(25, False), CREAM, 64, 33) + 12
    xt2 = xt + 1000
    d.text((xt2, y), 'Qué se queda como estaba', font=font(32, rubik=True), fill=SUN); yy = y + 50
    for linea in ('Los botones de arcilla («Jugar», «Desafío», «Empezar la ruta») y su segunda línea, en Fredoka.',
                  'Las cifras grandes («5 luces», «668», los puntos) y los números de las medallas.',
                  'Chips, etiquetas, pestañas y el cuerpo, en Nunito. Los nombres de juego DENTRO de una fila (la ruta de Hoy, la lista de Todos los juegos) son etiquetas de fila y siguen en Fredoka.'):
        yy = wrap(d, xt2, yy, '·  ' + linea, font(25, False), CREAM, 60, 33) + 12
    y += ROW + 8
    d.text((GAP, y), '2 · Hoy y la ficha con letra 1,3 y 2,0', font=font(40, rubik=True), fill=SUN)
    y += 62
    for k, (n, cap) in enumerate((('hoy-1_3', 'Hoy · letra 1,3'), ('ficha-1_3', 'Ficha · letra 1,3'), ('hoy-2_0', 'Hoy · letra 2,0'), ('ficha-2_0', 'Ficha · letra 2,0'))):
        shot(sheet, d, GAP + k * (PAIR + 40), y, e('hoy', n), e('propuesta', n), cap)
    y += ROW
    yy = y - 20
    for linea in ('Con letra 1,3, Hoy y la ficha siguen entrando sin deslizar (`HoyTarjetaTest` y `GameSheetFitScreenshotTest`, verdes). Rubik es algo más ancha que Fredoka: con 2,0 el saludo y «Tu ruta de hoy» pasan a dos líneas y la ficha pone el nombre en dos («Rescate / relámpago»), pero ninguna palabra se parte y Hoy se desliza como antes.',):
        yy = wrap(d, GAP, yy, linea, font(25, False), CREAM, 200, 33)
    yy = wrap(d, GAP, yy + 8, 'Los datos de la cuenta de prueba (racha, días de la meta) cambian de una captura a otra porque se sacaron en momentos distintos; lo que se compara es la letra de los títulos.', font(25, False), DIM, 200, 33)
    sheet = sheet.crop((0, 0, W, yy + 24))
    sheet.save(OUT, optimize=True); print('lámina:', OUT, sheet.size, os.path.getsize(OUT) // 1024, 'KB')


if __name__ == '__main__':
    build()
