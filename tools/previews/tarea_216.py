# -*- coding: utf-8 -*-
"""Lámina de la Tarea 216 (2-nov-2026): la app es desde los 14 años (`auditoria-fable/referencia-14-17.md` §3; decisión de Ricardo: «solo por temas legales, así nos resguardamos»).
A TAMAÑO REAL (1 dp = 1 px). Fila 1: la pregunta de edad del Primer vuelo con «14 a 17 años» como primera opción, la pestaña Contrarreloj de una persona de 14 a 17 sin Plus y con Plus (la comparación completa, la tabla de 18 a 29) y «Otras edades» sin fila de 14 a 17.
Fila 2: la hoja «De dónde sale la referencia» con la línea nueva y el estudio de Luna, «Preguntas frecuentes» con la respuesta de 14 a 17 y «Acerca de Nubi» con su línea de edad a 393 dp con letra 1,0. Fila 3: lo que va a letra 2,0 a 360 dp (Roborazzi; el emulador, en `docs/contrarreloj.md` §25).
Las capturas salen de `Tarea216CapturasTest` (`app/src/test/screenshots/t216/`, fuera de git).
Uso:  python -I tools/previews/tarea_216.py      → docs/previews/tarea-216.png
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SHOTS = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots', 't216')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-216.png')
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
    path = os.path.join(SHOTS, name + '.png')
    if not os.path.exists(path):
        im = Image.new('RGB', (dp_width, 300), (40, 36, 100)); ImageDraw.Draw(im).text((20, 120), 'falta ' + name, font=font(20), fill=SUN); return im
    im = Image.open(path).convert('RGB')
    return im.resize((dp_width, int(round(im.height * dp_width / im.width))), Image.LANCZOS)


ROWS = [
    [('la pregunta de edad · Primer vuelo', 'las siete opciones, la primera dice «14 a 17 años»; no hay opción «menos de 14», ni pantalla de edad, ni fecha de nacimiento (una barrera solo invita a mentir)', shot('vuelo_edad_393_10')),
     ('14 a 17 años · sin Plus · 393 dp', 'igual que de 18 a 29: lo gratis con la división y su lugar, las dos losas grises con candado y la fila «Con qué te comparas ›»; ya no hay línea de «De 13 a 17 años…»', shot('pestana_sin_plus_393_10')),
     ('14 a 17 años · con Plus · 393 dp', 'la comparación completa con la referencia de 18 a 29 años: la frase «superas al N %», la escalera, la campana, otras edades y semana a semana', shot('pestana_con_plus_393_10')),
     ('«Otras edades» · 14 a 17', 'las seis filas de 18 a 29 a 75 o más; la de 14 a 17 no tiene fila (sería la de 18 a 29 repetida) y quien tiene 14 a 17 no tiene fila «(tu franja)»', shot('otras_edades_393_10'))],
    [('De dónde sale la referencia · arriba', 'la línea nueva, justo bajo el párrafo de cómo se calcularon los números: «De 14 a 17 años la referencia es la misma que de 18 a 29: a esa edad la rapidez ya está cerca de la adulta.»', shot('referencia_393_10')),
     ('De dónde sale la referencia · Luna', 'el estudio nuevo, al final de «La rapidez con la edad»: Luna y otros, 2004 · 245 personas de 8 a 30 años; su DOI abre el artículo (PubMed y doi.org, 2-nov)', shot('referencia_luna_393_10')),
     ('Preguntas frecuentes · 14 a 17', '«¿Qué ve alguien de 14 a 17 años?» (abierta) termina con la misma frase de «Acerca de Nubi»: «Nubi es para personas de 14 años o más.»', shot('preguntas_393_10')),
     ('Acerca de Nubi · 393 dp · letra 1,0', 'justo bajo el título y antes de «Licencias y créditos», en una línea, en tinta y con la letra de lectura del panel; «14 años» no se parte', shot('acerca_393_10'))],
    [('Acerca de Nubi · 360 dp · letra 2,0', 'con letra 2,0 a 360 dp la línea parte por palabras en tres renglones y «14 años» queda junto; nada se corta', shot('acerca_360_20', 360)),
     ('Contrarreloj de 14 a 17 · 360 dp · letra 2,0', 'lo más angosto con la letra más grande (Roborazzi): el selector en una columna y lo gratis; sin choques', shot('pestana_sin_plus_360_20', 360)),
     ('De dónde sale la referencia · 360 dp · letra 2,0', 'la línea de 14 a 17 con letra 2,0: parte por palabras, y «14 a 17 años» queda junto', shot('referencia_360_20', 360))],
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
