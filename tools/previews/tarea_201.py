# -*- coding: utf-8 -*-
"""Lámina «hoy | ahora» de la Tarea 201 (31-oct-2026): la página «Contrarreloj» de Avance con el orden nuevo (récord, ritmo, semana, aciertos y errores, pistas anteriores), las líneas de separación, la ventanilla noche de los gráficos
de barras y la losa nueva «Tu ritmo en la última partida». Capturas REALES de Roborazzi (`Tarea201CapturasTest` = ahora; «hoy» son las capturas de la Tarea 199, `prop_*` de `t199/`, copiadas como `hoy_*` antes de tocar la página) a tamaño real
(1 px = 1 dp), con los datos de la Tarea 199 y, en la partida de hoy, el ritmo de la lámina de Fable (§2.3). Lo que no existía antes se marca «Todavía no existía».
Uso:  python -I tools/previews/tarea_201.py      → docs/previews/tarea-201.png
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SHOTS = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots', 't201')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-201.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
NAVY, SUN, DIM, CREAM, LINE = (20, 18, 63), (255, 203, 69), (200, 196, 235), (245, 235, 219), (90, 86, 160)
GAP, PAIR_GAP, MARGIN = 12, 40, 24

# (captura de «hoy» o None, captura de «ahora», título, ancho dp, alto dp, qué cambió)
ROW1 = [
    ('hoy_page_393_10_top', 'ahora_page_393_10_top', '1 · La página, arriba · 393 × 873 · letra 1,0', 393, 873,
     '«Tu récord» ahora separa con una línea fina (tinta al 14 %, 1 dp, 16 dp de aire arriba y abajo) la cifra con su fecha y su paso de «Tus últimas 10 marcas», y el gráfico va en una ventanilla noche (#100F3A, esquina y relleno de 12 dp): barras lila sin borde, '
     'el récord en sol con su moneda, cifras en crema (la del récord, en amarillo suave y negrita) y el eje en gris azulado. Abajo asoma la losa nueva. La cabecera, el selector y «Tu récord» siguen cabiendo en los 873 dp'),
    (None, 'ahora_page_393_10_rhythm', '2 · «Tu ritmo en la última partida» (nueva) · letra 1,0', 393, 873,
     'cuatro medios minutos (0 a 30, 30 a 60, 60 a 90 y 90 a 120 segundos) de la última partida, en la misma ventanilla: aciertos en barras lila y errores en barras coral (más angostas, a la MISMA escala), las cifras en UNA fila encima (el mejor medio minuto, en amarillo suave y negrita; '
     'los errores, solo donde hubo), el eje debajo con «segundos» escrito completo, la leyenda en palabras y la frase que lee el gráfico. Sin tiempos por ensayo y sin compararse con nadie'),
    ('hoy_page_393_10_bottom', 'ahora_page_393_10_bottom', '3 · La página, abajo · letra 1,0', 393, 873,
     '«Esta semana y la pasada» con una línea entre las dos semanas; «Aciertos y errores» con una línea tras «En tu récord» y otra tras «En tus últimas 5», antes de la regla. «Pistas anteriores» y «Tu ritmo» no llevan línea (una sola idea por losa). Las barras horizontales siguen en crema'),
]
ROW2 = [
    ('hoy_page_393_20_record', 'ahora_page_393_20_marks', '4 · Tus últimas marcas · letra 2,0', 393, 873,
     'con letra 2,0 el gráfico de las marcas dibuja las últimas 5 (las diez cifras de 16 sp no caben con 4 dp de aire), ahora dentro de la ventanilla noche; el eje dice la fecha de la primera barra que se ve y «hoy», y el récord, si queda fuera de esas 5, no lleva moneda'),
    (None, 'ahora_page_393_20_rhythm', '5 · Tu ritmo · letra 2,0', 393, 873,
     'el título y las frases siguen la letra de la persona; las cifras y el eje del gráfico no pasan de la escala 1,3, así que dos dígitos no tocan a la columna vecina; los rótulos parten la línea por palabras y «segundos» sigue escrito completo debajo'),
    (None, 'ahora_page_393_10_wait', '6 · Con marcas pero sin ritmo guardado', 393, 873,
     'las partidas de antes de la versión 17 no tienen ritmo: la losa solo dice «Tu ritmo aparece desde tu próxima partida del Contrarreloj.», sin gráfico, leyenda ni frase. Sin ninguna marca en el juego, la losa no aparece'),
]
ROW3 = [
    (None, 'ahora_page_393_13_rhythm', '7 · Tu ritmo · letra 1,3', 393, 873,
     'a 1,3 el título pasa a dos líneas y los rótulos del eje que no caben parten la línea por palabras («30 a / 60»); «segundos» sigue debajo, una sola vez'),
    (None, 'ahora_page_360_10_rhythm', '8 · Tu ritmo · 360 × 780 · letra 1,0', 360, 780,
     'a 360 dp cada columna mide un cuarto de la ventanilla menos 8 dp de aire (se calcula, no es fija): las barras y las cifras caben sin tocarse y los cuatro rótulos siguen en una línea'),
    (None, 'ahora_page_393_10_heavy', '9 · El peor caso: 48 aciertos y 18 errores en un medio minuto', 393, 873,
     'cifras de dos dígitos en las dos barras de una misma columna, y errores repartidos en dos pasos de la pista («en los pasos 1 y 4»); la prueba de cajas mide 0 choques así con letra 1,0, 1,3 y 2,0 a 393 dp y con 1,0 y 2,0 a 360 dp'),
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
            for j, (key, label) in enumerate(((before, 'HOY'), (after, 'AHORA'))):
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
