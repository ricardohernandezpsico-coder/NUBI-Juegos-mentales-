# -*- coding: utf-8 -*-
"""Lámina «hoy | propuesta» de la Tarea 199 (31-oct-2026): la página «Contrarreloj» de Avance (solo con Nubi Plus), su puerta en Avance y el atajo del final. Capturas REALES de Roborazzi (`Tarea199CapturasTest` = propuesta; `Tarea199HoyCapturasTest` = «hoy», con el código de antes) a tamaño real
(1 px = 1 dp), con los datos de la lámina de Fable (§11.3: 31, 35, 39, 36, 44 —el récord, 17 de octubre—, 38, 41, 37, 42 y 43 hoy). Lo que no existía antes se marca «Todavía no existía».
Uso:  python -I tools/previews/tarea_199.py      → docs/previews/tarea-199.png
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SHOTS = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots', 't199')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-199.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
NAVY, SUN, DIM, CREAM, LINE = (20, 18, 63), (255, 203, 69), (200, 196, 235), (245, 235, 219), (90, 86, 160)
GAP, PAIR_GAP, MARGIN = 12, 40, 24

# (captura de «hoy» o None, captura de la propuesta, título, ancho dp, alto dp, qué cambió)
ROW1 = [
    ('hoy_avance_plus', 'prop_avance_plus', '1 · Avance, con Nubi Plus', 393, 873,
     'entre «Tus áreas» y «Tus mejores marcas», UNA losa crema sin título de sección ni línea de ayuda sobre el cielo (los textos sueltos se leían mal con los luceros): el reloj de plasticina de Hoy, «Tus marcas del Contrarreloj» y «Tu récord de hoy: 44 aciertos» (el juego del día), con el chevron del kit; 24 dp de aire arriba y abajo. Toca la losa entera (64 dp de alto)'),
    ('hoy_avance_plus', 'prop_avance_sinplus', '2 · Avance, SIN Nubi Plus', 393, 873,
     'la misma losa sigue diciendo el récord de hoy (es tuyo y Hoy ya lo dice) y suma una tercera línea con el candado «Con Nubi Plus»; sin Plus no hay chevron. Sin ninguna partida de Contrarreloj dice «Todavía sin marcas»'),
    (None, 'prop_hoja_candado', '3 · La hoja del candado', 393, 873,
     'al tocar la losa sin Plus se abre una hoja corta: qué trae la página (cinco líneas) y UN solo botón, «Entendido» (único sol de la hoja). El botón de compra llega con Google Play Billing (Etapa 4): hoy la hoja solo cuenta qué hay detrás, sin precios ni promesas'),
]
ROW2 = [
    (None, 'prop_page_393_10_top', '4 · La página, arriba · 393 × 873 · letra 1,0', 393, 873,
     'cabecera sobre el cielo (disco de volver, «Contrarreloj» en Rubik y «Tus marcas, comparadas solo contigo.» con su orilla); el cielo se desvanece en 24 dp al noche plano (#050823) y de ahí para abajo todo va en losas crema, sin luceros entre ellas. El selector de los cuatro juegos en dos filas parejas (el elegido en lima con el visto sobre su emblema); «Tu récord»: «44 aciertos», «el 17 de octubre, en 2 minutos de Contrarreloj», el paso, y las diez barras con la moneda del récord en sol, las cifras en UNA fila, base común y solo dos rótulos (la fecha de la primera barra y «hoy»). A 1,0 la cabecera, el selector y «Tu récord» entero caben en los 873 dp'),
    (None, 'prop_page_393_10_bottom', '5 · La página, abajo · 393 × 873 · letra 1,0', 393, 873,
     '«Esta semana y la pasada» como filas «rótulo … cifra» con la cifra a la derecha; «Aciertos y errores» en tu récord y en tus últimas 5 (barra lila con «46 aciertos», coral con «2 errores», a la misma escala y con su palabra); «Pistas anteriores» con el récord de la pista 1 y la nota de siempre. La página entera mide 1656 dp y se desliza'),
    ('hoy_final_393_10', 'prop_final_393_10', '6 · El final del Contrarreloj, con Nubi Plus', 393, 873,
     'bajo «La mejor de esta semana», la losa chica «Todas tus marcas ›» (a la derecha, 48 dp de toque) abre la MISMA página con el juego recién jugado elegido (no es otra puerta). Solo con Plus; sin Plus el final queda como «hoy»'),
]
ROW3 = [
    (None, 'prop_page_393_13_top', '7 · Arriba · letra 1,3', 393, 873,
     'el selector pasa a UNA columna (con dos, «Acoplamiento» no cabe entero en media fila: una palabra nunca se parte); el subtítulo parte la línea por palabras; el título y la cifra grande no pasan de la escala 1,3. Las diez barras siguen con sus cifras a 4 dp o más. La página mide 2061 dp'),
    (None, 'prop_page_393_13_bottom', '8 · Abajo · letra 1,3', 393, 873,
     'las filas «rótulo … cifra» siguen en una línea; las barras de aciertos y errores quedan a la misma escala; todo dentro del margen de 16 dp'),
    (None, 'prop_page_393_20_record', '9 · «Tu récord» · letra 2,0', 393, 873,
     'con letra 2,0 se dibujan las últimas 5 marcas (las diez cifras de 16 sp no caben con 4 dp de aire); el eje empieza en la fecha de la PRIMERA de esas cinco, y como el récord (la 5.ª partida) queda fuera de las últimas 5, no hay moneda. La cifra grande no pasa de 44 sp. La página mide 2935 dp'),
]
ROW4 = [
    (None, 'prop_page_360_10_top', '10 · Arriba · 360 × 780 · letra 1,0', 360, 780,
     'a 360 dp «Acoplamiento» no cabe en media fila: el selector pasa a una columna; lo demás, igual. La página mide 1844 dp'),
    (None, 'prop_page_single', '11 · Con UNA sola marca', 393, 873,
     'en vez de las barras, «Tu primera marca. Desde la segunda, se dibujan aquí.»; sin promedios; la semana pasada dice «ninguna partida»; los otros juegos, «sin marca aún»'),
    (None, 'prop_page_393_20_top', '12 · Arriba · letra 2,0', 393, 873,
     'una columna; el título, con tope de escala 1,3, no se sale del margen derecho; el subtítulo parte la línea en tres'),
]
ROWS = [ROW1, ROW2, ROW3, ROW4]


def font(px, bold=True): return ImageFont.truetype(os.path.join(FONTS, 'fredoka.ttf' if bold else 'nunito_regular.ttf'), int(px))


def wrap(draw, text, f, width):
    lines, cur = [], ''
    for word in text.split(' '):
        t = (cur + ' ' + word).strip()
        if draw.textlength(t, font=f) <= width: cur = t
        else: lines.append(cur); cur = word
    if cur: lines.append(cur)
    return lines


def main():
    d0 = ImageDraw.Draw(Image.new('RGB', (10, 10)))
    tf, sf = font(17), font(15, bold=False)
    pair_w = lambda w: 2 * w + GAP
    sheet_w = MARGIN * 2 + sum(pair_w(w) for _, _, _, w, _, _ in ROW1) + PAIR_GAP * (len(ROW1) - 1)
    heights = []
    for row in ROWS:
        cap = max(len(wrap(d0, t, sf, pair_w(w) - 4)) * 20 for _, _, _, w, _, t in row)
        heights.append(26 + 24 + max(h for _, _, _, _, h, _ in row) + 12 + cap + 30)
    sheet = Image.new('RGB', (sheet_w, MARGIN + sum(heights)), NAVY)
    d = ImageDraw.Draw(sheet)
    y = MARGIN
    for row, hrow in zip(ROWS, heights):
        x0 = MARGIN
        for before, after, title, w, h, text in row:
            d.text((x0, y), title, font=tf, fill=SUN)
            for j, (key, label) in enumerate(((before, 'HOY'), (after, 'PROPUESTA'))):
                x = x0 + j * (w + GAP)
                d.text((x, y + 26), label, font=font(15), fill=CREAM if j else DIM)
                if key is None:
                    d.rectangle((x, y + 50, x + w - 1, y + 50 + h - 1), outline=LINE)
                    msg = 'Todavía no existía'
                    f = font(18, bold=False)
                    d.text((x + (w - d.textlength(msg, font=f)) / 2, y + 50 + h / 2 - 12), msg, font=f, fill=DIM)
                    continue
                im = Image.open(os.path.join(SHOTS, key + '.png')).convert('RGB')
                sheet.paste(im.resize((w, h), Image.LANCZOS), (x, y + 50))
                d.rectangle((x - 1, y + 49, x + w, y + 50 + h), outline=LINE)
            cy = y + 50 + max(hh for _, _, _, _, hh, _ in row) + 12
            for i, line in enumerate(wrap(d, text, sf, pair_w(w) - 4)):
                d.text((x0, cy + i * 20), line, font=sf, fill=DIM)
            x0 += pair_w(w) + PAIR_GAP
        y += hrow
    sheet.save(OUT)
    print(OUT, sheet.size)


main()
