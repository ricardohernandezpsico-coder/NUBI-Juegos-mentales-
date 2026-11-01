# -*- coding: utf-8 -*-
"""Lámina de la Tarea 208 (1-nov-2026): Rescate relámpago cuenta por CÁPSULA (cada cápsula bien marcada suma 1 acierto y cada cápsula marcada que no estaba resta 1 error; la escalera sigue por rondas logradas).
A TAMAÑO REAL (1 dp = 1 px, 393 dp de ancho). Fila 1: el final del Contrarreloj de Rescate con la marca nueva y el detalle abierto (la frase de Rescate en 16 sp y, al pie, el récord de la pista 2 como «pista anterior»), y la página de Rescate de 18 a 29 años (escalera de cinco peldaños y campana de 0 a 45).
Fila 2: la página de 75 años o más, y el final con letra 2,0. Fila 3: la página de Rescate con letra 2,0 en el EMULADOR (`medium_phone`, `font_scale 2.0`, `tools/emulador_ui.py`). Las de Roborazzi salen de `Tarea208CapturasTest`; todo en `app/src/test/screenshots/t208/` (fuera de git).
Uso:  python -I tools/previews/tarea_208.py      → docs/previews/tarea-208.png
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SHOTS = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots', 't208')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-208.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
NAVY, SUN, DIM, LINE = (20, 18, 63), (255, 203, 69), (200, 196, 235), (90, 86, 160)
GAP, MARGIN, COLW = 36, 24, 393

# (captura, título, qué se ve); cada fila trae hasta 4 columnas, todas de 393 x 873 dp
ROWS = [
    [('ahora_final_393_10', 'el final · marca nueva · letra 1,0',
      '34 aciertos menos 3 errores = 31, y el detalle abierto: «En Rescate relámpago, cada cápsula bien marcada es un acierto y cada cápsula marcada que no estaba, un error.» (16 sp) y, al pie, «Tu récord de la pista 2: 12 aciertos.», la pista anterior'),
     ('ahora_ladder_18_29_393_10', 'la escalera · 18 a 29 años · letra 1,0',
      'los CINCO peldaños de Rescate (antes faltaba uno de 18 a 44): Despegando desde 26, En órbita 29, En la Luna 32, Entre estrellas 35. Marca 26: «Despegando»'),
     ('ahora_bell_18_29_393_10', 'la campana · 18 a 29 años · letra 1,0',
      'tramos de a 3: el eje va de 0 a «45 o más» (rótulos 0 · 15 · 45; el «30» no se dibuja porque quedaría bajo la raya de la mitad, 29)'),
     ('ahora_older_393_10', 'la pista anterior · letra 1,0',
      'Rescate arranca sin la marca de la regla de rondas: «Tu récord de la pista 2: 12 aciertos. Las marcas de otra pista no se comparan.»')],
    [('ahora_ladder_75_393_10', 'la escalera · 75 años o más · letra 1,0',
      'cinco peldaños también aquí: Despegando desde 17, En órbita 21, En la Luna 24, Entre estrellas 27. Marca 19: «Despegando»'),
     ('ahora_bell_75_393_10', 'la campana · 75 años o más · letra 1,0',
      'la misma pista de 0 a 45, la campana corrida a la izquierda: la mitad en 21'),
     ('ahora_final_393_20', 'el final · letra 2,0 (la frase)',
      'con letra 2,0 el detalle se desliza y la frase de Rescate sigue entera, sin partir ninguna palabra'),
     ('ahora_final_393_20_pie', 'el final · letra 2,0 (el pie)',
      'y al pie del detalle, la duración y la pista anterior')],
    [('emu_ladder_393_20', 'la cifra y la escalera · letra 2,0 · emulador',
      'la página de Rescate en el emulador (393 dp, letra 2,0, 18 a 29 años, 3 partidas de la pista 3; la de la pista 2 no cuenta): marca 26, «27 de cada 100», Despegando'),
     ('emu_ladder2_393_20', 'la escalera · letra 2,0 · emulador',
      'los cinco peldaños en columna, cada uno con su «desde N aciertos», y «Te faltan 3 aciertos para En órbita.»'),
     ('emu_bell_393_20', 'la campana · letra 2,0 · emulador',
      'con letra 2,0 el eje conserva sus extremos, «0 aciertos» y «45 o más», y omite los dos del medio: el 15 no cabe y el 30 quedaría bajo la raya de la mitad')],
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


def load(key):
    im = Image.open(os.path.join(SHOTS, key + '.png')).convert('RGB')
    h = int(round(im.height * COLW / im.width))
    return im.resize((COLW, h), Image.LANCZOS)          # tamaño real: 1 dp = 1 px


def main():
    d0 = ImageDraw.Draw(Image.new('RGB', (10, 10)))
    tf, sf = font(17), font(15, bold=False)
    sheet_w = MARGIN * 2 + 4 * COLW + 3 * GAP
    heights = []
    for row in ROWS:
        shots = max(load(k).height for k, _, _ in row)
        cap = max(len(wrap(d0, t, sf, COLW)) * 20 for _, _, t in row)
        heights.append(26 + shots + 12 + cap + 34)
    sheet = Image.new('RGB', (sheet_w, MARGIN + sum(heights)), NAVY)
    d = ImageDraw.Draw(sheet)
    y = MARGIN
    for row, hrow in zip(ROWS, heights):
        x = MARGIN
        for key, title, text in row:
            d.text((x, y), title, font=tf, fill=SUN)
            im = load(key)
            sheet.paste(im, (x, y + 26))
            d.rectangle((x - 1, y + 25, x + COLW, y + 26 + im.height), outline=LINE)
            cy = y + 26 + im.height + 12
            for i, line in enumerate(wrap(d, text, sf, COLW)):
                d.text((x, cy + i * 20), line, font=sf, fill=DIM)
            x += COLW + GAP
        y += hrow
    sheet.save(OUT)
    print(OUT, sheet.size)


main()
