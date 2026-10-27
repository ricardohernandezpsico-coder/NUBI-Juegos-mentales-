# -*- coding: utf-8 -*-
"""Lámina de la Tarea 180, «Hoy: Modos de juego» (`docs/previews/tarea-180.png`), con CAPTURAS REALES; no son maquetas.
  1. Hoy | propuesta con letra 1,0 y 1,3 (Roborazzi a 393 × 873 dp, `Tarea180CapturasTest`: «hoy» se grabó con la disposición anterior —`git stash`—, «propuesta» con la nueva).
  2. La letra 2,0: Roborazzi y el emulador (`medium_phone`), hoy | propuesta.
  3. El reloj de plasticina (Blender, `kit_i_reloj_clay`) ampliado sobre lila, sol y crema, y el ícono de «Todos los juegos» (2 × 2 con los colores de las áreas).
  4. Cuánto mide Hoy en cada estado (dp que se pasa de la barra; negativo = sobra).
Entradas (fuera de git): `app/src/test/screenshots/tarea-180/{hoy,propuesta}/`, `tools/isla/out/kit-reloj/kit_i_reloj_clay.png`, `unity/test-results/t180-emu-*.png`, `tools/previews/tarea_180_medidas.txt`.
Uso:  python -I tools/previews/tarea_180.py
"""
import os, sys, textwrap
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SHOTS = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots', 'tarea-180')
EMU = os.path.join(ROOT, 'unity', 'test-results')
RELOJ = os.path.join(ROOT, 'tools', 'isla', 'out', 'kit-reloj', 'kit_i_reloj_clay.png')
MEDIDAS = os.path.join(HERE, 'tarea_180_medidas.txt')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-180.png')
NAVY, SUN, DIM, CREAM = (20, 18, 63), (255, 203, 69), (200, 196, 235), (245, 235, 219)
LILA = (184, 164, 255)
GAP = 36
W = 2400
ESTADOS = {'media-ruta': 'La ruta sigue (lila)', 'ruta-completa': 'Ruta completa (sol)', 'jugado': 'Ya jugado (crema)', 'marca-nueva': 'Marca nueva', 'pausa': 'Partida en pausa (lila)'}


def font(px, bold=True, rubik=False):
    return ImageFont.truetype(os.path.join(FONTS, 'rubik_bold.ttf' if rubik else ('fredoka.ttf' if bold else 'nunito_regular.ttf')), int(px))


def load(path): return Image.open(path).convert('RGB') if path and os.path.exists(path) else None


def fit_w(im, w): return im.resize((w, round(im.height * w / im.width)), Image.LANCZOS)


def wrap(d, x, y, text, f, fill, width_chars, step):
    for line in textwrap.wrap(text, width_chars):
        d.text((x, y), line, font=f, fill=fill); y += step
    return y


def title(d, y, text):
    d.text((GAP, y), text, font=font(46, rubik=True), fill=SUN); return y + 74


def row(sheet, d, y, items, w, caption_chars=36, crop_h=None):
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


def pares(sheet, d, y, fs, estados, nota_hoy, w=520, crop_h=1500):
    """Hoy | propuesta de cada estado, de a dos pares por fila (cuatro teléfonos)."""
    for k in range(0, len(estados), 2):
        items = []
        for e in estados[k:k + 2]:
            items.append((load(os.path.join(SHOTS, 'hoy', '%s-%s.png' % (e, fs))), 'HOY · ' + ESTADOS[e], nota_hoy.get(e, '')))
            items.append((load(os.path.join(SHOTS, 'propuesta', '%s-%s.png' % (e, fs))), 'PROPUESTA · ' + ESTADOS[e], ''))
        y = row(sheet, d, y, items, w, 34, crop_h)
    return y


def reloj_ampliado():
    im = Image.open(RELOJ).convert('RGBA') if os.path.exists(RELOJ) else None
    if im is None: return None
    k = 4; S = (im.width * k, im.height * k)
    sh = Image.new('RGB', (S[0] * 3, S[1]), (0, 0, 0))
    for i, f in enumerate((LILA, SUN, CREAM)):
        bg = Image.new('RGBA', im.size, f + (255,)); bg.alpha_composite(im); sh.paste(bg.convert('RGB').resize(S, Image.LANCZOS), (i * S[0], 0))
    return sh


def build():
    sheet = Image.new('RGB', (W, 14000), NAVY); d = ImageDraw.Draw(sheet)
    d.text((GAP, 24), 'Hoy: Modos de juego (Tarea 180)', font=font(66, rubik=True), fill=SUN)
    y = 118
    for line in ('Bajo la tarjeta de la ruta, Hoy ahora tiene una sección con título, «Modos de juego», y DOS botones a todo el ancho, uno bajo el otro: «Contrarreloj» (con un reloj de plasticina de ~42 dp hecho en Blender; abajo, solo «Hoy: Tinta o Palabra») y «Todos los juegos» (con las cuatro casillas de los colores de las áreas). El Contrarreloj es lila mientras la ruta sigue, sol con la ruta completa y por jugar, y crema ya jugado; un solo sol a la vez. «Tu récord» sigue como línea plana bajo el botón; con marca nueva, el recuadro plano lila claro y, debajo, el botón crema «Otra vez».',
                 'Todo son capturas reales (Roborazzi a 393 × 873 dp con el fondo y las barras del sistema simuladas por margen: 24 dp arriba y 163 abajo; el reloj es el render real de Blender). «Hoy» se grabó con el código de antes de la tarea. Con letra 1,3 y 2,0 y a 360 dp ninguna palabra se parte; lo que no cabe en la pantalla se desliza (los números, abajo).'):
        y = wrap(d, GAP, y, line, font(27, False), DIM, 150, 38)
    y += 16
    y = title(d, y, '1 · Letra 1,0 (393 × 873 dp)')
    y = pares(sheet, d, y, '1_0', ['media-ruta', 'ruta-completa', 'jugado', 'marca-nueva'], {'media-ruta': 'Dos botones pequeños en una fila, con «Contrarreloj de hoy» partido en dos renglones.', 'ruta-completa': 'Un botón sol con dos líneas: el juego y «2 minutos».'})
    y = title(d, y, '2 · Letra 1,3')
    y = pares(sheet, d, y, '1_3', ['media-ruta', 'ruta-completa', 'jugado', 'marca-nueva', 'pausa'], {})
    y = title(d, y, '3 · Letra 2,0 (Roborazzi) y en el emulador')
    items = []
    for e in ('media-ruta', 'ruta-completa'):
        items.append((load(os.path.join(SHOTS, 'hoy', '%s-2_0.png' % e)), 'HOY · ' + ESTADOS[e], ''))
        items.append((load(os.path.join(SHOTS, 'propuesta', '%s-2_0.png' % e)), 'PROPUESTA · ' + ESTADOS[e], ''))
    y = row(sheet, d, y, items, 520, 34, 1500)
    emu = [load(os.path.join(EMU, f)) for f in ('t180-emu-2_0-hoy.png', 't180-emu-2_0-abajo.png')]
    if any(e is not None for e in emu):
        y = row(sheet, d, y, [(emu[0], 'EMULADOR · letra 2,0', 'La app instalada en el emulador con la letra al 200 %: «Contrarreloj» en una línea, nada partido.'), (emu[1], 'EMULADOR · deslizando hasta el final', 'Todo se alcanza deslizando.')], 520, 34)
    y = title(d, y, '4 · El reloj de plasticina, ampliado sobre lila, sol y crema, y las casillas de las áreas')
    r = reloj_ampliado()
    if r is not None:
        sheet.paste(r, (GAP, y)); y += r.height + 12
        d.text((GAP, y), 'Aro de tinta y cara crema: se lee igual sobre los tres colores del botón. 38 × 42 dp medidos; se dibuja a 42 dp (3 px por dp, como el resto del kit).', font=font(24, False), fill=DIM); y += 60
    y = title(d, y, '5 · Cuánto mide Hoy (dp que se pasa de la barra de pestañas; negativo = sobra)')
    if os.path.exists(MEDIDAS):
        for line in open(MEDIDAS, encoding='utf-8').read().splitlines():
            if line.strip(): y = wrap(d, GAP, y, line, font(25, False), CREAM, 175, 34)
    y += 20
    sheet = sheet.crop((0, 0, W, y + 20)); sheet.save(OUT, optimize=True)
    print('lámina:', OUT, sheet.size)


if __name__ == '__main__':
    build()
