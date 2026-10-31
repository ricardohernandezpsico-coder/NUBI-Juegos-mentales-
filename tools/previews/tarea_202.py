# -*- coding: utf-8 -*-
"""Lámina de la Tarea 202 (31-oct-2026): la comparación con personas de tu edad en la página «Contrarreloj» de Avance, con DATOS DE MUESTRA y solo en depuración. Capturas REALES de Roborazzi (`Tarea202CapturasTest`) a tamaño real (1 px = 1 dp): «Tu lugar esta semana» a 1,0 y 2,0, la campana, las 7 semanas,
otras edades, «Todavía somos pocos», sin marca esta semana, 360 dp y la página entera con las dos secciones. Datos: la persona de 45 a 54 años de la lámina de Fable (128 personas esta semana, 43 aciertos supera a 63 de cada 100, la mitad 39 y la cuarta parte más alta 47) con las marcas de la Tarea 199.
Uso:  python -I tools/previews/tarea_202.py      → docs/previews/tarea-202.png
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SHOTS = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots', 't202')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-202.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
NAVY, SUN, DIM, CREAM, LINE = (20, 18, 63), (255, 203, 69), (200, 196, 235), (245, 235, 219), (90, 86, 160)
GAP, MARGIN = 40, 24

# (captura, título, ancho dp, qué se ve)
ROW1 = [
    ('ahora_section_393_10', '1 · «Tu lugar esta semana» · letra 1,0', 393,
     'bajo el rótulo «Comparado con personas de tu edad» (con su línea) va la frase «Estos son datos de muestra: solo se ven en la versión de prueba.»; la losa trae la frase principal en el recuadro plano lila de «Marca nueva» (la franja, el juego y la semana a la vista), '
     '«Es el percentil 63: la división En órbita.» en tinta apagada y una línea de separación antes de la campana'),
    ('ahora_place_393_10_bell', '2 · La campana real', 393,
     'el histograma de las 128 personas suavizado 1-2-1 solo al dibujar (el percentil sale de los tramos sin suavizar), en la ventanilla noche: la línea fina de la mitad (39 aciertos), «Tú» como barra sol hasta la curva con la moneda del visto y la palabra encima, y el eje «0 aciertos · 25 · 50 · 75 o más». '
     'Debajo, la mitad y la cuarta parte más alta, dichas, y «Hecha con las marcas de las 128 personas tal como son, no con una curva teórica.»'),
    ('ahora_weeks_393_10', '3 · Tu percentil en las últimas 7 semanas', 393,
     'solo las semanas con marca propia y con 50 personas o más en la franja (aquí cuatro: las otras tres no tienen partida); la de esta semana en sol y negrita; la frase cuenta las que hay («En 3 de las 4 semanas…»). El eje dice «hace 7 semanas» y «esta semana»'),
]
ROW2 = [
    ('ahora_others_393_10', '4 · Comparado con otras edades', 393,
     'seis franjas (sin la de 13 a 17 años) con «N de cada 100» a la derecha, la propia en negrita con «(tu franja)»; la de 75 años o más tiene 38 personas y dice «todavía somos pocos» (apagado). Una línea y «Todas las edades»; al pie, «De 13 a 17 años no hay comparación por ahora.»'),
    ('ahora_place_393_20', '5 · «Tu lugar» · letra 2,0', 393,
     'la frase parte la línea por palabras y el recuadro crece con ella; el rótulo de la sección y su línea siguen la letra'),
    ('ahora_place_393_20_bell', '6 · La campana · letra 2,0', 393,
     'la campana y «Tú» no cambian de tamaño; el eje omite el «25» porque no deja 8 dp al «0 aciertos» (se guardan los dos extremos y el «50»); las dos frases de abajo siguen la letra'),
]
ROW3 = [
    ('ahora_weeks_393_20', '7 · Las 7 semanas · letra 2,0', 393,
     'el título parte la línea por palabras («7 semanas» siempre junto); las cifras y el eje, con tope de escala 1,3'),
    ('ahora_others_393_20', '8 · Otras edades · letra 2,0', 393,
     'cada fila pasa su cifra a una línea propia cuando no caben juntas, como «Esta semana y la pasada»'),
    ('ahora_few_393_10', '9 · «Todavía somos pocos» · letra 1,0', 393,
     'con menos de 50 personas en la franja esta semana (aquí 23): la frase enmarcada con la cuenta real, la barra «23 de 50 personas» y, tras una línea, «Mientras tanto, comparada con todas las edades de las últimas 4 semanas…». Ya no hay campana ni semanas con 50 personas'),
]
ROW4 = [
    ('ahora_few_393_20', '10 · «Pocos» · letra 2,0', 393,
     'la barra pasa su texto debajo cuando no caben juntos; la frase y «Mientras tanto» siguen la letra'),
    ('ahora_nomark_393_10', '11 · Sin marca esta semana', 393,
     'la losa dice qué hacer («Juega su Contrarreloj para ver tu lugar…») y no inventa números; las semanas anteriores con marca siguen contándose y «otras edades» no aparece'),
    ('ahora_place_360_10', '12 · «Tu lugar» · 360 × 780', 360,
     'a 360 dp la campana y su eje caben sin tocarse; la moneda de «Tú» y la línea de la mitad dejan más de 3 dp entre sí'),
]
ROW5 = [
    ('ahora_full_393_10', '13 · La página entera · 393 dp · letra 1,0', 393,
     'las dos secciones con su rótulo: «Tus marcas» (todo lo de la Tarea 201) y «Comparado con personas de tu edad» (Tu lugar, las 7 semanas y otras edades). Sin la comparación (la build de la tienda, o una persona de 13 a 17 años) la página queda como en la Tarea 201, sin rótulos'),
    ('ahora_full_few_393_10', '14 · La página entera con «Todavía somos pocos»', 393,
     'el mismo orden con el mundo de «pocos» de los datos de muestra (el botón «[Debug] Comparación: muchos / pocos» de Ajustes lo cambia)'),
]
ROWS = [ROW1, ROW2, ROW3, ROW4, ROW5]


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
    sheet_w = MARGIN * 2 + 3 * 393 + 2 * GAP
    # alto real de cada captura a 1 px = 1 dp
    def real_h(key, w):
        im = Image.open(os.path.join(SHOTS, key + '.png'))
        return int(round(im.height * w / im.width))
    heights = []
    for row in ROWS:
        shots = max(real_h(k, w) for k, _, w, _ in row)
        cap = max(len(wrap(d0, t, sf, w)) * 20 for _, _, w, t in row)
        heights.append(26 + shots + 12 + cap + 34)
    sheet = Image.new('RGB', (sheet_w, MARGIN + sum(heights)), NAVY)
    d = ImageDraw.Draw(sheet)
    y = MARGIN
    for row, hrow in zip(ROWS, heights):
        x = MARGIN
        for key, title, w, text in row:
            d.text((x, y), title, font=tf, fill=SUN)
            im = Image.open(os.path.join(SHOTS, key + '.png')).convert('RGB')
            h = int(round(im.height * w / im.width))
            sheet.paste(im.resize((w, h), Image.LANCZOS), (x, y + 26))
            d.rectangle((x - 1, y + 25, x + w, y + 26 + h), outline=LINE)
            cy = y + 26 + h + 12
            for i, line in enumerate(wrap(d, text, sf, w)):
                d.text((x, cy + i * 20), line, font=sf, fill=DIM)
            x += 393 + GAP
        y += hrow
    sheet.save(OUT)
    print(OUT, sheet.size)


main()
