# -*- coding: utf-8 -*-
"""Lámina de la Tarea 229 (4-nov-2026): la liga de la semana (`docs/liga-de-la-semana.md` §1 a §5). A TAMAÑO REAL (1 dp = 1 px).
Fila 1 y 2: la losa de la liga en sus seis estados (sube, se queda, baja, sin %, en la más alta, primera semana) a 393 dp con letra 1,0 y a 360 dp con 1,3. Fila 3: letra 2,0 (393 y 360 dp). Fila 4 y 5: la escalera de las diez divisiones con la persona abajo, al medio y arriba. Fila 6: los cuatro cuadros de «Tus juegos de esta semana», con y sin Plus.
Fila 7: la línea de Hoy bajo la losa del Contrarreloj en sus estados. Fila 8 y 9: la hoja del lunes en sus casos (sube, se queda, baja, sin %, la más alta). Fila 10: Tú. Al final, los textos de la hoja del candado de Plus, antes y ahora.
Las capturas salen de `Tarea229CapturasTest` (Roborazzi, `app/src/test/screenshots/t229/`, fuera de git).
Uso:  python -I tools/previews/tarea_229.py      → docs/previews/tarea-229.png
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SHOTS = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots', 't229')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-229.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
NAVY, SUN, DIM, LINE, CREAM, INK = (20, 18, 63), (255, 203, 69), (200, 196, 235), (90, 86, 160), (245, 235, 219), (26, 18, 64)
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
    """La captura a su tamaño en dp (xxhdpi: 3 px por dp); [dp_width] es el ancho en dp de la captura."""
    path = os.path.join(SHOTS, name + '.png')
    if not os.path.exists(path):
        im = Image.new('RGB', (dp_width, 300), (40, 36, 100)); ImageDraw.Draw(im).text((20, 120), 'falta ' + name, font=font(20), fill=SUN); return im
    im = Image.open(path).convert('RGB')
    im = im.resize((dp_width, int(round(im.height * dp_width / im.width))), Image.LANCZOS)
    return im.crop((0, 0, dp_width, crop_dp)) if crop_dp else im


def tall(name, dp_width=393, crop=None): return shot(name, dp_width, crop)


def texto(lineas, alto=None):
    """Un panel crema con los textos de la hoja del candado (antes / ahora)."""
    d0 = ImageDraw.Draw(Image.new('RGB', (10, 10)))
    f = font(15, bold=False); fb = font(16)
    filas = []
    for tipo, t in lineas:
        ff = fb if tipo == 'h' else f
        for l in wrap(d0, t, ff, COLW - 24): filas.append((tipo, l))
        filas.append(('s', ''))
    h = alto or (24 + 22 * len(filas))
    im = Image.new('RGB', (COLW, h), CREAM); d = ImageDraw.Draw(im)
    y = 12
    for tipo, l in filas:
        d.text((12, y), l, font=fb if tipo == 'h' else f, fill=INK if tipo != 'v' else (140, 40, 40)); y += 22
    return im


ROWS = [
    [('losa de la liga · SUBE (393 · 1,0)', 'Despegando con 25 %: «el lunes subirías a Sobre las nubes». La barra marca la línea de la de arriba', shot('liga_sube_393_10')),
     ('losa · SE QUEDA', '14 %: «te quedarías en Despegando» y lo que falta para subir', shot('liga_queda_393_10')),
     ('losa · BAJA', '4 %: «bajarías a En la plataforma. Para quedarte, te falta llegar al 10 %»', shot('liga_baja_393_10')),
     ('losa · SIN %', 'sin partidas esta semana: no hay cifra ni barra, solo la división y qué hacer', shot('liga_sin_porcentaje_393_10'))],
    [('losa · EN LA MÁS ALTA', 'En otra galaxia con 95 %: «te quedarías En otra galaxia, la división más alta»', shot('liga_en_la_mas_alta_393_10')),
     ('losa · PRIMERA SEMANA', 'todos parten En la plataforma; sin partidas', shot('liga_primera_semana_393_10')),
     ('360 dp · letra 1,3 · sube', '', shot('liga_sube_360_13', 360)),
     ('360 dp · letra 1,3 · baja', '', shot('liga_baja_360_13', 360))],
    [('360 dp · letra 1,3 · En la Luna', 'el nombre más largo de la mitad de arriba, pegado donde cabe', shot('liga_queda_en_la_luna_360_13', 360)),
     ('393 dp · letra 2,0 · sube', '', shot('liga_sube_393_20', 393)),
     ('360 dp · letra 2,0 · sin %', '', shot('liga_sin_porcentaje_360_20', 360)),
     ('360 dp · letra 2,0 · la más alta', '', shot('liga_en_la_mas_alta_360_20', 360))],
    [('escalera de las diez · persona ABAJO', 'En la plataforma: la fila lima de 52 dp con «Tú» y las nueve de arriba apagadas', shot('escalera_abajo_393_10')),
     ('escalera · AL MEDIO', 'En la Luna', shot('escalera_medio_393_10')),
     ('escalera · ARRIBA', 'En otra galaxia', shot('escalera_arriba_393_10')),
     ('escalera · 360 dp · letra 1,3', 'Sobre las nubes', shot('escalera_medio_360_13', 360))],
    [('escalera · 360 dp · letra 2,0', 'abajo: En la plataforma', shot('escalera_abajo_360_20', 360)),
     ('«Tus juegos de esta semana» · con Plus', 'tres juegos jugados y uno sin partidas; el elegido va en lima con su visto', shot('cuadros_con_plus_393_10')),
     ('cuadros · sin Plus', 'los tres juegos que no son de hoy van en gris con candado, pero muestran igual su marca y su %', shot('cuadros_sin_plus_393_10')),
     ('cuadros · sin Plus · 360 dp · letra 1,3', '', shot('cuadros_sin_plus_360_13', 360))],
    [('cuadros · con Plus · 360 dp · letra 2,0', 'una columna cuando la palabra más larga no cabe en media fila', shot('cuadros_con_plus_360_20', 360)),
     ('Hoy · SUBE', 'bajo la losa celeste: el emblema de 24 dp y «Despegando · vas para subir a Sobre las nubes»', shot('hoy_sube_393_10')),
     ('Hoy · SE QUEDA', '«vas para quedarte»', shot('hoy_queda_393_10')),
     ('Hoy · BAJA', '«juega para quedarte»', shot('hoy_baja_393_10'))],
    [('Hoy · SIN %', '«Despegando · juega para quedarte»', shot('hoy_sin_porcentaje_393_10')),
     ('Hoy · En la plataforma', '«En la plataforma · juega para subir»', shot('hoy_plataforma_393_10')),
     ('Hoy · la más alta', '«En otra galaxia · vas para quedarte»', shot('hoy_la_mas_alta_393_10')),
     ('Hoy · sin liga', 'sin franja de edad o sin referencia no hay línea (ni «Te quedan N partidas»: quedó en la pestaña y en la ficha)', shot('hoy_sin_liga_393_10'))],
    [('Hoy · sin partidas', 'la línea de la liga va tras el recuadro y la fila gris', shot('hoy_sin_partidas_393_10')),
     ('Hoy · 360 dp · letra 1,3', '', shot('hoy_larga_360_13', 360)),
     ('Hoy · 360 dp · letra 2,0', 'el nombre más largo de la mitad de arriba', shot('hoy_larga_360_20', 360)),
     ('hoja del lunes · SUBIÓ', '«¡Subiste a Sobre las nubes!», el emblema, la semana pasada, «Alcanzaba para En Marte…» y lo de esta semana', shot('hoja_sube_393_10'))],
    [('hoja · SE QUEDÓ', '«Sigues en Despegando», sin celebración', shot('hoja_queda_393_10')),
     ('hoja · BAJÓ', '«Esta semana estás En la plataforma», sin «bajaste»', shot('hoja_baja_393_10')),
     ('hoja · SEMANA SIN %', '«Tu semana pasada: no jugaste el Contrarreloj.»', shot('hoja_sin_porcentaje_393_10')),
     ('hoja · En la plataforma sin %', 'se queda: no se baja más', shot('hoja_plataforma_sin_porcentaje_393_10'))],
    [('hoja · la más alta', '', shot('hoja_la_mas_alta_393_10')),
     ('hoja · 360 dp · letra 1,3', '', shot('hoja_sube_360_13', 360)),
     ('hoja · 393 dp · letra 2,0', '', shot('hoja_sube_393_20', 393)),
     ('hoja · 360 dp · letra 2,0', '', shot('hoja_baja_360_20', 360))],
    [('Tú · Despegando', '«Tu división: Despegando» con el emblema de 24 dp (reemplaza a «Etapa general»)', shot('tu_despegando_393_10')),
     ('Tú · En la plataforma', '', shot('tu_en_la_plataforma_393_10')),
     ('Tú · 360 dp · letra 1,3', '', shot('tu_entre_asteroides_360_13', 360)),
     ('Tú · 360 dp · letra 2,0', '', shot('tu_en_otra_galaxia_360_20', 360))],
    [('hoja del candado · ANTES', '', texto([
        ('h', 'Con comparación (referencia estimada) · lo gratis:'),
        ('v', 'Gratis: el Contrarreloj de cada día, con 3 partidas, tu récord y tu división de la semana. Con Nubilus Plus, además:'),
        ('h', 'lo que trae Plus:'),
        ('v', '• Los cuatro Contrarrelojes, cualquier día, con partidas libres.'),
        ('v', '• Qué porcentaje de la referencia estimada para tu edad superas, y la escalera de las cinco divisiones.'),
        ('v', '• Tu lugar en otras edades y semana a semana, con tus semanas seguidas arriba de la mitad.'),
        ('v', '• Todas tus marcas: las últimas 10 en barras, tu ritmo en los 2 minutos y el récord de las pistas anteriores.'),
        ('h', 'la losa gris de la pestaña:'),
        ('v', 'Con Nubilus Plus: tu lugar en detalle.')])),
     ('hoja del candado · AHORA', '', texto([
        ('h', 'Con comparación (referencia estimada) · lo gratis:'),
        ('t', 'Gratis: el Contrarreloj de cada día, con 3 partidas, tu récord y tu división de la semana, con tu porcentaje en cada juego. Con Nubilus Plus, además:'),
        ('h', 'lo que trae Plus:'),
        ('t', '• Los cuatro Contrarrelojes, cualquier día, con partidas libres.'),
        ('t', '• La campana de tu edad: dónde queda tu marca de cada juego frente a la referencia estimada.'),
        ('t', '• Tu lugar en otras edades y semana a semana, con tus semanas seguidas en la mitad de arriba.'),
        ('t', '• Todas tus marcas: las últimas 10 en barras, tu ritmo en los 2 minutos y el récord de las pistas anteriores.'),
        ('h', 'la losa gris de la pestaña:'),
        ('t', 'Con Nubilus Plus: la campana, otras edades y semana a semana.')])),
     ('preguntas frecuentes · ANTES', '', texto([('v', '«¿Con qué me compara el Contrarreloj?» … Gratis ves tu división de la semana; con Nubilus Plus, además, dónde queda tu marca frente a tu edad y a otras edades.')])),
     ('preguntas frecuentes · AHORA', '', texto([('t', '«¿Con qué me compara el Contrarreloj?» … Gratis ves tu división de la semana y tu porcentaje en cada juego; con Nubilus Plus, además, la campana de tu edad, otras edades y semana a semana.')]))],
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
