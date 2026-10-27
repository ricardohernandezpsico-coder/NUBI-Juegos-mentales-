# -*- coding: utf-8 -*-
"""Lámina de la Tarea 181, «El Contrarreloj en la ficha» (`docs/previews/tarea-181.png`), con CAPTURAS REALES; no son maquetas.
  1. La ficha de Tinta o Palabra con letra 1,0: hoy | propuesta, sin récord y con récord (Roborazzi a 393 × 873 dp, `Tarea181CapturasTest`: «hoy» se grabó con la ficha de antes —`git stash`—, «propuesta» con la nueva).
  2. El botón «Contrarreloj» en lila y en crema (sin y con récord), para elegir el color.
  3. Letra 1,3 (también con los dos nombres más largos: ¿Verdad o disparate? y Rescate relámpago).
  4. «Más opciones» desplegado en Tinta: ya sin «¿Con reloj?».
  5. Freno de Emergencia, sin cambios, con su «¿Con reloj?».
  6. El final de un Contrarreloj jugado desde la ficha de un juego que NO es el del día: dice «Contrarreloj», sin «de hoy».
  7. La letra 2,0 (Roborazzi y el emulador `medium_phone`).
  8. Cuánto se pasa la ficha del marco (los números).
Entradas (fuera de git): `app/src/test/screenshots/tarea-181/{hoy,propuesta}/`, `unity/test-results/t181-emu-*.png`, `tools/previews/tarea_181_medidas.txt`.
Uso:  python -I tools/previews/tarea_181.py
"""
import os, sys, textwrap
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SHOTS = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots', 'tarea-181')
EMU = os.path.join(ROOT, 'unity', 'test-results')
MEDIDAS = os.path.join(HERE, 'tarea_181_medidas.txt')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-181.png')
NAVY, SUN, DIM, CREAM = (20, 18, 63), (255, 203, 69), (200, 196, 235), (245, 235, 219)
FRAME_BG = (5, 8, 35)
GAP = 36
W = 2400


def font(px, bold=True, rubik=False):
    return ImageFont.truetype(os.path.join(FONTS, 'rubik_bold.ttf' if rubik else ('fredoka.ttf' if bold else 'nunito_regular.ttf')), int(px))


def load(path): return Image.open(path).convert('RGB') if path and os.path.exists(path) else None


def autocrop(im, pad=18):
    """Recorta el marco de la ventana (el fondo noche liso de arriba y abajo): se queda con la tarjeta de la ficha."""
    if im is None: return None
    px = im.load(); w, h = im.size
    def is_bg(x, y):
        r, g, b = px[x, y]; return abs(r - FRAME_BG[0]) < 6 and abs(g - FRAME_BG[1]) < 6 and abs(b - FRAME_BG[2]) < 8
    top = next((y for y in range(h) if any(not is_bg(x, y) for x in range(0, w, 7))), 0)
    bottom = next((y for y in range(h - 1, -1, -1) if any(not is_bg(x, y) for x in range(0, w, 7))), h - 1)
    return im.crop((0, max(0, top - pad), w, min(h, bottom + pad)))


def fit_w(im, w): return im.resize((w, round(im.height * w / im.width)), Image.LANCZOS)


def wrap(d, x, y, text, f, fill, width_chars, step):
    for line in textwrap.wrap(text, width_chars):
        d.text((x, y), line, font=f, fill=fill); y += step
    return y


def title(d, y, text):
    d.text((GAP, y), text, font=font(46, rubik=True), fill=SUN); return y + 74


def row(sheet, d, y, items, w, caption_chars=34, crop_h=None):
    """items = [(imagen, rótulo, nota)]; todas a la misma anchura, alineadas por arriba."""
    n = len(items); gap = (W - 2 * GAP - n * w) // max(1, n - 1) if n > 1 else 0
    x = GAP; tallest = 0
    for im, label, note in items:
        if im is not None:
            fi = fit_w(im, w)
            if crop_h: fi = fi.crop((0, 0, fi.width, min(fi.height, crop_h)))
            sheet.paste(fi, (x, y)); tallest = max(tallest, fi.height)
            d.rectangle((x - 1, y - 1, x + fi.width, y + fi.height), outline=(70, 66, 120), width=2)
        else:
            d.rectangle((x, y, x + w, y + 300), outline=DIM); d.text((x + 16, y + 16), 'sin captura', font=font(26, False), fill=DIM); tallest = max(tallest, 300)
        x += w + gap
    x = GAP; ly = y + tallest + 10; end = ly
    for im, label, note in items:
        d.text((x, ly), label, font=font(28), fill=SUN)
        end = max(end, wrap(d, x, ly + 38, note, font(23, False), DIM, caption_chars, 29)); x += w + gap
    return end + 40


def hoy(name): return autocrop(load(os.path.join(SHOTS, 'hoy', name + '.png')))
def prop(name): return autocrop(load(os.path.join(SHOTS, 'propuesta', name + '.png')))
def final(folder, name): return load(os.path.join(SHOTS, folder, name + '.png'))


def build():
    sheet = Image.new('RGB', (W, 16000), NAVY); d = ImageDraw.Draw(sheet)
    d.text((GAP, 24), 'El Contrarreloj en la ficha (Tarea 181)', font=font(66, rubik=True), fill=SUN)
    y = 118
    for line in ('En la ficha de Tinta o Palabra, ¿Verdad o disparate?, Rescate relámpago y Acoplamiento hay un botón nuevo, «Contrarreloj», a todo el ancho BAJO «Desafío | Más opciones»: el reloj de plasticina de Hoy (42 dp), el nombre y, debajo, «Tu récord: N aciertos.» (el récord de ESE juego con la pista vigente) o «2 minutos». Lanza el Contrarreloj de ese juego con la pista del día (la misma semilla que usaría Hoy), se guarda igual que desde Hoy y no pide nada más: gratis y sin tope. «Jugar» sigue siendo el ÚNICO sol de la ficha.',
                 '«¿Con reloj?» se va de «Más opciones» solo en esos cuatro, y la segunda línea de «Jugar» ya no dice «· sin reloj». Los otros 13 juegos quedan IGUAL (Freno, abajo). Todo son capturas reales (Roborazzi a 393 × 873 dp, con las barras del sistema simuladas por margen); «hoy» se grabó con la ficha de antes. El color del botón, lila o crema, lo elige Opus: las dos formas están en la fila 2.'):
        y = wrap(d, GAP, y, line, font(27, False), DIM, 150, 38)
    y += 16

    y = title(d, y, '1 · La ficha de Tinta o Palabra, letra 1,0')
    y = row(sheet, d, y, [
        (hoy('tinta-1_0'), 'HOY', 'Jugar dice «a tu medida · sin reloj»; «¿Con reloj?» vive en «Más opciones».'),
        (prop('tinta-1_0'), 'PROPUESTA · sin récord', 'El botón lila, con el reloj y «2 minutos». Jugar: «a tu medida».'),
        (prop('tinta-record-1_0'), 'PROPUESTA · con récord', 'Con récord: «Tu récord: 41 aciertos.» en lugar de «2 minutos».'),
    ], 700, 40)

    y = title(d, y, '2 · El botón en lila y en crema (letra 1,0)')
    y = row(sheet, d, y, [
        (prop('tinta-1_0'), 'LILA · sin récord', 'El mismo lila que el Contrarreloj de Hoy. «Desafío» también es lila.'),
        (prop('tinta-crema-1_0'), 'CREMA · sin récord', 'El mismo crema que «Más opciones» y «Sobre este juego».'),
        (prop('tinta-record-1_0'), 'LILA · con récord', ''),
        (prop('tinta-crema-record-1_0'), 'CREMA · con récord', ''),
    ], 520, 34)

    y = title(d, y, '3 · Letra 1,3: sigue cabiendo entera sin deslizar a 393 × 873')
    y = row(sheet, d, y, [
        (hoy('tinta-1_3'), 'HOY · Tinta', ''),
        (prop('tinta-1_3'), 'PROPUESTA · Tinta', 'Plegada cabe entera.'),
        (prop('tinta-record-1_3'), 'PROPUESTA · Tinta con récord', ''),
        (prop('disparate-record-1_3'), 'PROPUESTA · ¿Verdad o disparate?', 'Uno de los nombres más largos.'),
    ], 520, 34)
    y = row(sheet, d, y, [
        (prop('rescate-record-1_3'), 'PROPUESTA · Rescate relámpago', 'El otro nombre largo.'),
        (prop('acoplamiento-1_0'), 'PROPUESTA · Acoplamiento (1,0)', 'El cuarto juego.'),
    ], 520, 34)

    y = title(d, y, '4 · «Más opciones» desplegado en Tinta: ya sin «¿Con reloj?»')
    y = row(sheet, d, y, [
        (hoy('tinta-mas-1_0'), 'HOY · 1,0', 'Los cuatro modos y, abajo, «¿Con reloj?».'),
        (prop('tinta-mas-1_0'), 'PROPUESTA · 1,0', 'El botón queda entre la fila y los modos; «¿Con reloj?» ya no está.'),
        (hoy('tinta-mas-1_3'), 'HOY · 1,3', ''),
        (prop('tinta-mas-1_3'), 'PROPUESTA · 1,3', ''),
    ], 520, 34)

    y = title(d, y, '5 · Freno de Emergencia: IGUAL que antes, con su «¿Con reloj?»')
    y = row(sheet, d, y, [
        (hoy('freno-1_0'), 'HOY', ''),
        (prop('freno-1_0'), 'PROPUESTA', 'Sin botón; «Jugar» sigue diciendo «a tu medida · con reloj».'),
        (hoy('freno-mas-1_0'), 'HOY · desplegada', ''),
        (prop('freno-mas-1_0'), 'PROPUESTA · desplegada', 'Idéntica.'),
    ], 520, 34)

    y = title(d, y, '6 · El final de un Contrarreloj jugado desde la ficha')
    y = row(sheet, d, y, [
        (final('hoy', 'final-27'), 'HOY · Tinta el 27-oct', 'Decía «Contrarreloj de hoy» aunque Tinta no era el juego del día (el del 27 es ¿Verdad o disparate?).'),
        (final('propuesta', 'final-27'), 'PROPUESTA · NO es el del día', 'Dice «Contrarreloj», sin «de hoy». Lo demás, igual.'),
        (final('propuesta', 'final-26'), 'PROPUESTA · SÍ es el del día', 'Tinta el 26-oct: «Contrarreloj de hoy».'),
    ], 640, 40, crop_h=1100)
    d.text((GAP, y - 20), 'Lo mismo vale para la pantalla de carga («Contrarreloj de hoy» o «Contrarreloj», en lugar de «Etapa … · Sin reloj») y para el lector de pantalla del botón y del final.', font=font(24, False), fill=DIM); y += 50

    y = title(d, y, '7 · Letra 2,0 (Roborazzi y emulador)')
    emu = [autocrop(load(os.path.join(EMU, f))) for f in ('t181-emu-2_0-ficha.png', 't181-emu-2_0-desplegada.png')]
    y = row(sheet, d, y, [
        (hoy('tinta-2_0'), 'HOY · Roborazzi', ''),
        (prop('tinta-2_0'), 'PROPUESTA · Roborazzi', 'Ninguna palabra partida.'),
        (emu[0], 'EMULADOR · letra 2,0', 'La app instalada en el emulador con la letra al 200 %.'),
        (emu[1], 'EMULADOR · desplegada', 'Sin «¿Con reloj?»; la tarjeta se desliza por dentro y todo se alcanza.'),
    ], 520, 34)

    y = title(d, y, '8 · Cuánto se pasa la ficha del marco (dp; 0 = cabe entera sin deslizar)')
    if os.path.exists(MEDIDAS):
        for line in open(MEDIDAS, encoding='utf-8').read().splitlines():
            if line.strip(): y = wrap(d, GAP, y, line, font(25, False), CREAM, 175, 34)
    y += 20
    sheet = sheet.crop((0, 0, W, y + 20)); sheet.save(OUT, optimize=True)
    print('lámina:', OUT, sheet.size)


if __name__ == '__main__':
    build()
