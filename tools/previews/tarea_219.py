# -*- coding: utf-8 -*-
"""Lámina de la Tarea 219 (3-nov-2026): Hoy reordenada y el Contrarreloj en celeste (`auditoria-fable/idea-pestana-contrarreloj.md` §2.2, §9 punto 2 y §10; tarea 2 de las tres de esa idea).
A TAMAÑO REAL (1 dp = 1 px). Fila 1: Hoy ANTES (Tarea 180: título «Modos de juego», el Contrarreloj lila o sol sobre «Todos los juegos»). Fila 2: Hoy DESPUÉS con Nubilus Plus (ruta · «Todos los juegos» de 56 dp · losa celeste de 80 dp con el reloj a 56 dp):
a mitad de ruta, ruta completa, jugado («Otra vez» en crema) y marca nueva (recuadro celeste claro + «Otra vez»). Fila 3: sin Plus («Te quedan 3 partidas de hoy.», jugado con partidas y récord, sin partidas con la fila gris y marca nueva sin partidas en UN solo recuadro).
Fila 4: la ficha con el botón celeste (antes lila), con y sin Plus, el gris con candado que no cambia, y el reloj de plasticina ANTES y DESPUÉS a 42 dp (la ficha) y a 56 dp (Hoy). Fila 5: el emulador con letra 2,0 a 393 dp. Fila 6: a 360 dp (letra 1,0 y 2,0).
Las capturas salen de `Tarea219CapturasTest` (Roborazzi, `app/src/test/screenshots/t219/`, fuera de git), de las de antes (`t219/antes/`) y del emulador `medium_phone` (`emu_*.png`); el reloj, de `tools/isla/out/kit-reloj/` (antes) y `kit-reloj-219/` (después).
Uso:  python -I tools/previews/tarea_219.py      → docs/previews/tarea-219.png
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SHOTS = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots', 't219')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-219.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
RELOJ_ANTES = os.path.join(ROOT, 'tools', 'isla', 'out', 'kit-reloj', 'kit_i_reloj_clay.png')
RELOJ_DESPUES = os.path.join(ROOT, 'tools', 'isla', 'out', 'kit-reloj-219', 'kit_i_reloj_clay.png')
NAVY, SUN, DIM, LINE, CREAM = (20, 18, 63), (255, 203, 69), (200, 196, 235), (90, 86, 160), (245, 235, 219)
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


def shot(name, dp_width=393, crop_dp=None, folder=None):
    path = os.path.join(folder or SHOTS, name + '.png')
    if not os.path.exists(path):
        im = Image.new('RGB', (dp_width, 300), (40, 36, 100)); ImageDraw.Draw(im).text((20, 120), 'falta ' + name, font=font(20), fill=SUN); return im
    im = Image.open(path).convert('RGB')
    im = im.resize((dp_width, int(round(im.height * dp_width / im.width))), Image.LANCZOS)
    return im.crop((0, 0, dp_width, crop_dp)) if crop_dp else im


def antes(name, **kw): return shot(name, folder=os.path.join(SHOTS, 'antes'), **kw)


def relojes():
    """El reloj de plasticina ANTES (162 px, cuerpo de 42 dp) y DESPUÉS (216 px, cuerpo de ~57 dp) sobre crema: a 42 dp (la ficha: mismo tamaño y encuadre) y a 56 dp (la losa de Hoy: antes ampliado 1,32×, después a su tamaño). 3 px por dp (la imagen en xxhdpi); abajo, cada uno con su lado en dp."""
    if not (os.path.exists(RELOJ_ANTES) and os.path.exists(RELOJ_DESPUES)):
        im = Image.new('RGB', (COLW, 300), (40, 36, 100)); ImageDraw.Draw(im).text((20, 120), 'falta el reloj', font=font(20), fill=SUN); return im
    a = Image.open(RELOJ_ANTES).convert('RGBA'); d = Image.open(RELOJ_DESPUES).convert('RGBA')
    PX = 3
    body_a, body_d = 42.33, 57.33                                              # dp del cuerpo, de `kit.json` (`reloj_clay`)

    def dibuja(im, body_dp, objetivo_dp):
        k = objetivo_dp / body_dp                                              # dp de pantalla por dp de la imagen
        w = int(round(im.width * k)); h = int(round(im.height * k))
        return im.resize((w, h), Image.LANCZOS)
    cols = []
    for objetivo in (42.0, 56.0):
        cols.append((dibuja(a, body_a, objetivo), dibuja(d, body_d, objetivo), objetivo))
    W, H = COLW, 620
    sheet = Image.new('RGB', (W, H), CREAM)
    dr = ImageDraw.Draw(sheet)
    f = font(15, bold=False)
    y = 14
    for ia, idd, obj in cols:
        dr.text((12, y), 'cuerpo de %d dp' % obj, font=f, fill=(26, 18, 64))
        y += 22
        sheet.paste(ia, (24, y), ia); sheet.paste(idd, (24 + ia.width + 40, y), idd)
        dr.text((24, y + ia.height + 2), 'antes (162 px)', font=f, fill=(81, 75, 120)); dr.text((24 + ia.width + 40, y + idd.height + 2), 'después (216 px)', font=f, fill=(81, 75, 120))
        y += max(ia.height, idd.height) + 30
    return sheet.crop((0, 0, W, min(H, y)))


ROWS = [
    [('ANTES · a mitad de ruta (Tarea 180)', 'el título «Modos de juego», el Contrarreloj lila de 64 dp y «Todos los juegos» de 48 dp, abajo del todo', antes('hoy-tarjeta-393')),
     ('ANTES · ruta completa', 'el sol pasaba al Contrarreloj, con «Tu récord» bajo él', antes('hoy-tarjeta-completa')),
     ('ANTES · jugado', 'el Contrarreloj crema «Otra vez» y su récord', antes('hoy-tarjeta-contrarreloj-jugado')),
     ('ANTES · marca nueva', 'el recuadro lila claro y «Otra vez» en crema', antes('hoy-tarjeta-contrarreloj-marca-nueva'))],
    [('DESPUÉS · a mitad de ruta (con Plus)', 'ruta · «Todos los juegos» (56 dp, letra 18, casillas de 36) · losa celeste de 80 dp con el reloj a 56 dp; «Tu récord: 41 aciertos.» debajo; sin título', shot('hoy_ruta_a_medias_393_10')),
     ('DESPUÉS · ruta completa', 'la tarjeta se cierra y la losa SIGUE celeste: el sol vive solo en la ruta', shot('hoy_ruta_completa_393_10')),
     ('DESPUÉS · jugado', '«Otra vez» en crema con «Hoy: <juego>»; el récord debajo', shot('hoy_jugado_393_10')),
     ('DESPUÉS · marca nueva', 'el recuadro pasa de lila claro a celeste claro (#A5E4F7, tinta 12,5:1) con el visto; «Otra vez» debajo', shot('hoy_marca_nueva_393_10'))],
    [('sin Plus · a mitad de ruta', '«Te quedan 3 partidas de hoy.» (sin récord: antes de jugar lo que cuenta son las partidas)', shot('hoy_sin_plus_ruta_a_medias_393_10')),
     ('sin Plus · jugado', '«Otra vez» y, debajo, «Te quedan 2 partidas de hoy. Tu récord: 41 aciertos.»', shot('hoy_sin_plus_jugado_393_10')),
     ('sin Plus · sin partidas', 'sin losa: el recuadro celeste claro con lo de mañana y la fila gris con candado (sigue GRIS)', shot('hoy_sin_partidas_393_10')),
     ('sin Plus · marca nueva y sin partidas', 'UN solo recuadro: la marca arriba y, tras una línea fina, lo de mañana; después la fila gris', shot('hoy_marca_nueva_sin_partidas_393_10'))],
    [('ANTES · la ficha del juego de hoy', 'el botón «Contrarreloj» en lila', antes('ficha_hoy_con_partidas_393_10')),
     ('DESPUÉS · la ficha del juego de hoy', 'celeste: lila = Desafío; celeste = Contrarreloj; crema = opciones e información; único sol = «Jugar»', shot('ficha_hoy_con_partidas_393_10')),
     ('DESPUÉS · la ficha con Plus', 'con Plus, el botón celeste en los cuatro juegos; sin Plus, los otros tres siguen con el botón GRIS con candado (no cambia: la crema sin saturación)', shot('ficha_con_plus_393_10')),
     ('el reloj de plasticina · antes y después', 'a 42 dp (la ficha) mismo tamaño y encuadre; a 56 dp (Hoy) el de antes se ampliaba 1,32× y el nuevo se dibuja a su tamaño', relojes())],
    [('393 dp · letra 2,0 · a mitad de ruta (emulador)', 'ruta, «Todos los juegos» y la losa celeste; nada se corta', shot('emu_hoy_393_20_a')),
     ('393 dp · letra 2,0 · ruta completa (emulador)', 'la losa celeste; «Contrarreloj» en una línea y «Hoy: …» en dos por palabras', shot('emu_hoy_393_20_b')),
     ('393 dp · letra 2,0 · marca nueva (emulador)', 'el recuadro, «Otra vez» y su línea de estado', shot('emu_hoy_393_20_c')),
     ('393 dp · letra 2,0 · sin partidas (emulador)', 'el recuadro con lo de mañana y la fila gris', shot('emu_hoy_393_20_d'))],
    [('360 dp · letra 1,0 · a mitad de ruta', 'lo último se desliza (como todo Hoy a 360 dp): se alcanza deslizando', shot('hoy_ruta_a_medias_360_10', 360)),
     ('360 dp · letra 2,0 · a mitad de ruta (emulador)', 'lo más angosto con la letra más grande: nada se corta ni se encima', shot('emu_hoy_360_20_a', 360)),
     ('360 dp · letra 2,0 · sin partidas (emulador)', 'el recuadro y la fila gris por palabras', shot('emu_hoy_360_20_d', 360)),
     ('393 dp · letra 2,0 · la ficha celeste (emulador)', 'el botón celeste entero; «Te quedan 2 partidas de hoy.» en su segunda línea', shot('emu_ficha_393_20'))],
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
