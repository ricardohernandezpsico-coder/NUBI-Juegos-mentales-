# -*- coding: utf-8 -*-
"""Lámina corta de la Tarea 202b (31-oct-2026): la cabecera y la nota de la página «Contrarreloj» con y sin la comparación con personas de tu edad (con ella ya no dicen «solo contigo») y «Todavía somos pocos» con «Mientras tanto» y la fila «Todas las edades»: con todas las edades de ESTA semana el número es
el mismo en las dos; si esta semana no llegan a 50, «Mientras tanto» usa las últimas 4 semanas, lo dice, y la fila dice «todavía somos pocos». Capturas REALES de Roborazzi (`Tarea202bCapturasTest`) a tamaño real (1 px = 1 dp), con datos de muestra de depuración.
Uso:  python -I tools/previews/tarea_202b.py      → docs/previews/tarea-202b.png
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SHOTS = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots', 't202b')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-202b.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
NAVY, SUN, DIM, CREAM, LINE = (20, 18, 63), (255, 203, 69), (200, 196, 235), (245, 235, 219), (90, 86, 160)
GAP, MARGIN, W = 40, 24, 393

ROW1 = [
    ('ahora_header_sin', '1 · La cabecera SIN comparación', 'la tienda, o una persona de 13 a 17 años: queda como hoy, «Tus marcas, comparadas solo contigo.»'),
    ('ahora_header_con', '2 · La cabecera CON comparación', 'con la sección «Comparado con personas de tu edad» en la página: «Tus marcas, comparadas contigo y con personas de tu edad.»'),
    ('ahora_note_sin', '3 · La nota SIN comparación', '«Es el resultado de un juego, comparado solo contigo. No es una medida de capacidad.», como hoy (y como en el final de la partida, que no compara con nadie)'),
]
ROW2 = [
    ('ahora_note_con', '4 · La nota CON comparación', '«Es el resultado de un juego. No es una medida de capacidad.»: ya no dice «solo contigo»'),
    ('ahora_few_place', '5 · «Pocos»: Mientras tanto', 'todas las edades llegan a 50 esta semana (124 personas): «Mientras tanto, comparada con todas las edades de esta semana, tu mejor marca, 43 aciertos, supera a 51 de cada 100 personas.»'),
    ('ahora_few_others', '6 · «Pocos»: la fila «Todas las edades»', 'la misma semana, el MISMO número: «51 de cada 100». Las franjas con menos de 50 personas dicen «todavía somos pocos»'),
]
ROW3 = [
    ('ahora_veryfew_place', '7 · «Muy pocos»: Mientras tanto', 'esta semana todas las edades no llegan a 50 (45 personas): «Mientras tanto» usa las últimas 4 semanas y lo dice («de las últimas 4 semanas»)'),
    ('ahora_veryfew_others', '8 · «Muy pocos»: la fila «Todas las edades»', 'y la fila de esta semana dice «todavía somos pocos»: ningún número del mismo grupo cambia de una frase a otra'),
]
ROWS = [ROW1, ROW2, ROW3]


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
    sheet_w = MARGIN * 2 + 3 * W + 2 * GAP
    def real_h(key):
        im = Image.open(os.path.join(SHOTS, key + '.png'))
        return int(round(im.height * W / im.width))
    heights = []
    for row in ROWS:
        shots = max(real_h(k) for k, _, _ in row)
        cap = max(len(wrap(d0, t, sf, W)) * 20 for _, _, t in row)
        heights.append(26 + shots + 12 + cap + 34)
    sheet = Image.new('RGB', (sheet_w, MARGIN + sum(heights)), NAVY)
    d = ImageDraw.Draw(sheet)
    y = MARGIN
    for row, hrow in zip(ROWS, heights):
        x = MARGIN
        for key, title, text in row:
            d.text((x, y), title, font=tf, fill=SUN)
            im = Image.open(os.path.join(SHOTS, key + '.png')).convert('RGB')
            h = int(round(im.height * W / im.width))
            sheet.paste(im.resize((W, h), Image.LANCZOS), (x, y + 26))
            d.rectangle((x - 1, y + 25, x + W, y + 26 + h), outline=LINE)
            cy = y + 26 + h + 12
            for i, line in enumerate(wrap(d, text, sf, W)):
                d.text((x, cy + i * 20), line, font=sf, fill=DIM)
            x += W + GAP
        y += hrow
    sheet.save(OUT)
    print(OUT, sheet.size)


main()
