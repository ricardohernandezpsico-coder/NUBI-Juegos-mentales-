# -*- coding: utf-8 -*-
"""Lámina «antes | ahora» de la Tarea 205b (1-nov-2026): la raya de «la mitad» que la barra «Tú» tapaba en la campana (Rescate relámpago con la marca en la mitad a 393 y 360 dp; Tinta o Palabra con la mitad + 1 a 360 dp) y la línea que dice dónde elegir
la edad cuando no hay franja guardada (393 y 360 dp, letra 1,0 y 2,0). Las capturas salen de `Tarea205bCapturasTest` (`app/src/test/screenshots/t205b/`): «antes» = la T205 (7c5ead1f), «ahora» = la T205b; las dos a tamaño real y recortadas en la zona que cambió.
Uso:  python -I tools/previews/tarea_205b.py      → docs/previews/tarea-205b.png
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SHOTS = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots', 't205b')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-205b.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
NAVY, SUN, DIM, LINE, HOYC = (20, 18, 63), (255, 203, 69), (200, 196, 235), (90, 86, 160), (170, 166, 210)
GAP, MARGIN, COLW = 40, 24, 393

# (captura, ancho dp, alto recortado dp, título, qué se ve); cada fila es (antes, ahora)
ROWS = [
    [('antes_radar_393', 393, 400, 'antes · T205 · Rescate, marca en la mitad · 393 dp',
      'la marca (14) y la mitad (14) caen en el mismo punto: la barra sol de 6 dp tapa la raya de 1,5 dp y la frase dice «La línea marca la mitad de la referencia estimada.» de una raya que no se ve'),
     ('ahora_radar_393', 393, 400, 'ahora · T205b · Rescate, marca en la mitad · 393 dp',
      'la raya no se dibuja y la frase dice «Tu marca está en la mitad de la referencia estimada.»; el lector de pantalla termina con «…, está en la mitad.» en vez de «…, está marcada.»')],
    [('antes_radar_360', 360, 400, 'antes · T205 · lo mismo a 360 dp',
      'mismo problema a 360 dp'),
     ('ahora_radar_360', 360, 400, 'ahora · T205b · a 360 dp',
      'la misma regla: la distancia entre centros (en dp) menor que medio ancho de la barra + medio de la raya + 1 dp; en Rescate (11 dp por acierto) solo la misma marca tapa la raya')],
    [('antes_stroop_360', 360, 400, 'antes · T205 · Tinta o Palabra, mitad + 1 · 360 dp',
      'a 360 dp un acierto mide ~3,4 dp: con la mitad ± 1 la barra también tapa la raya, y la frase decía que había una línea'),
     ('ahora_stroop_360', 360, 400, 'ahora · T205b · Tinta o Palabra, mitad + 1 · 360 dp',
      'la frase coincide con lo que se ve: «Tu marca está en la mitad de la referencia estimada.»; con la mitad ± 2 (7 dp) la raya sí se ve y la frase de siempre vuelve (`ContrarrelojPageTest`)')],
    [('antes_nofranja_393_10', 393, 620, 'antes · T205 · sin franja guardada · 393 dp · letra 1,0',
      'la página queda callada: solo «Tus marcas»; quien instaló antes de la Tarea 195 (sin franja de edad) no entiende por qué no hay comparación'),
     ('ahora_nofranja_393_10', 393, 620, 'ahora · T205b · sin franja guardada · 393 dp · letra 1,0',
      'UNA línea bajo el selector, donde iría el rótulo: «Para compararte con una referencia estimada para tu edad, elige tu edad en Tú › Más ajustes › Juego › Rango de edad.» (frase corrida, sin negrita)')],
    [('antes_nofranja_393_20', 393, 873, 'antes · T205 · letra 2,0',
      'sin aviso (la página, bajada 300 dp, igual que en «ahora»)'),
     ('ahora_nofranja_393_20', 393, 873, 'ahora · T205b · letra 2,0',
      'con letra 2,0 la frase parte por palabras sin salirse de los márgenes de 16 dp')],
    [('antes_nofranja_360_10', 360, 700, 'antes · T205 · sin franja guardada · 360 dp · letra 1,0',
      'sin aviso'),
     ('ahora_nofranja_360_10', 360, 700, 'ahora · T205b · 360 dp · letra 1,0',
      'la misma línea a 360 dp')],
    [('antes_nofranja_360_20', 360, 780, 'antes · T205 · 360 dp · letra 2,0',
      'sin aviso'),
     ('ahora_nofranja_360_20', 360, 780, 'ahora · T205b · 360 dp · letra 2,0',
      'letra 2,0 a 360 dp: la línea cabe y no toca el selector ni la losa de abajo (`ContrarrelojPageTest`)')],
]


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
    sheet_w = MARGIN * 2 + 2 * COLW + GAP
    def load(key, w, crop_h):
        im = Image.open(os.path.join(SHOTS, key + '.png')).convert('RGB')
        h = int(round(im.height * w / im.width))
        im = im.resize((w, h), Image.LANCZOS)
        return im.crop((0, 0, w, min(h, crop_h)))
    heights = []
    for row in ROWS:
        shots = max(min(crop, int(round(Image.open(os.path.join(SHOTS, k + '.png')).height * w / Image.open(os.path.join(SHOTS, k + '.png')).width))) for k, w, crop, _, _ in row)
        cap = max(len(wrap(d0, t, sf, COLW)) * 20 for _, _, _, _, t in row)
        heights.append(26 + shots + 12 + cap + 34)
    sheet = Image.new('RGB', (sheet_w, MARGIN + sum(heights)), NAVY)
    d = ImageDraw.Draw(sheet)
    y = MARGIN
    for row, hrow in zip(ROWS, heights):
        x = MARGIN
        for key, w, crop, title, text in row:
            d.text((x, y), title, font=tf, fill=HOYC if title.startswith('antes') else SUN)
            im = load(key, w, crop)
            sheet.paste(im, (x, y + 26))
            d.rectangle((x - 1, y + 25, x + w, y + 26 + im.height), outline=LINE)
            cy = y + 26 + im.height + 12
            for i, line in enumerate(wrap(d, text, sf, COLW)):
                d.text((x, cy + i * 20), line, font=sf, fill=DIM)
            x += COLW + GAP
        y += hrow
    sheet.save(OUT)
    print(OUT, sheet.size)


main()
