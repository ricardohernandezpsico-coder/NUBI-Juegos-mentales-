# -*- coding: utf-8 -*-
"""Lámina de la Tarea 207 (1-nov-2026): el lugar aparece desde la tercera partida de cada juego, y el eje de la campana ya no pone un rótulo bajo la raya de la mitad ni bajo la barra «Tú».
Todo a TAMAÑO REAL (1 dp = 1 px, 393 dp de ancho). Fila 1: letra 1,0 (Roborazzi, `Tarea207CapturasTest`): la página de Tinta o Palabra (franja de 30 a 44 años, marca 58) y la puerta de Avance sin Plus con 1 y con 3 partidas.
Fila 2: lo mismo con letra 2,0, del EMULADOR (`medium_phone`, API 36, `font_scale 2.0`, `tools/emulador_ui.py`): la frase es larga y un `Dialog` no recibe la letra de una prueba. Fila 3: el eje de la campana, antes (Tarea 206) y ahora, a 393 y a 360 dp.
Las capturas están en `app/src/test/screenshots/t207/` y `t206/` (fuera de git). Uso:  python -I tools/previews/tarea_207.py      → docs/previews/tarea-207.png
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SHOTS = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-207.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
NAVY, SUN, DIM, LINE, HOYC = (20, 18, 63), (255, 203, 69), (200, 196, 235), (90, 86, 160), (170, 166, 210)
GAP, MARGIN, COLW = 36, 24, 393

# (carpeta, captura, ancho dp, alto recortado dp o None, título, qué se ve); cada fila trae hasta 4 columnas
ROWS = [
    [('t207', 'ahora_page_1_393_10', 393, 873, 'la página · 1 partida · letra 1,0',
      'Tinta o Palabra, 30 a 44 años, 1 partida guardada: no hay lugar y la frase va UNA vez bajo el selector, en una línea corrida sin negrita; la cabecera y la nota dicen «solo contigo»'),
     ('t207', 'ahora_page_3_393_10', 393, 873, 'la página · 3 partidas · letra 1,0',
      'con la tercera partida todo vuelve a ser como era: el rótulo, la referencia estimada, la cifra, la escalera y la campana'),
     ('t207', 'ahora_door_1_393_10', 393, 873, 'la puerta de Avance · 1 partida · letra 1,0',
      'sin Plus, con 1 partida del juego de hoy la puerta calla (récord y candado), como sin franja de edad'),
     ('t207', 'ahora_door_3_393_10', 393, 873, 'la puerta de Avance · 3 partidas · letra 1,0',
      'con 3 partidas la puerta dice la frase gratis de siempre: la división y lo que falta')],
    [('t207', 'emu_page_1_393_20_b', 393, 873, 'la página · 1 partida · letra 2,0 · emulador',
      'la página bajada hasta la frase: con letra 2,0 ocupa cuatro renglones, cabe entre el selector y las tres cifras y no toca nada'),
     ('t207', 'emu_page_3_393_20_b', 393, 873, 'la página · 3 partidas · letra 2,0 · emulador',
      'con la tercera partida, bajo el selector, el rótulo y la frase larga de la referencia estimada (la frase de la tercera partida ya no está)'),
     ('t207', 'emu_door_1_393_20', 393, 873, 'la puerta · 1 partida · letra 2,0 · emulador',
      'la puerta a letra 2,0 con 1 partida: callada y entera, con el candado'),
     ('t207', 'emu_door_3_393_20', 393, 873, 'la puerta · 3 partidas · letra 2,0 · emulador',
      'con 3 partidas la frase gratis ocupa seis renglones y cabe sin cortar palabras')],
    [('t206', 'ahora_stroop_bell_393_10', 393, 760, 'el eje · antes (T206) · 393 dp',
      'Tinta de 30 a 44 años, marca 58: el «63» queda justo bajo la raya de la mitad (65) y se lee «la mitad es 63»'),
     ('t207', 'ahora_axis_393_10', 393, 760, 'el eje · ahora (T207) · 393 dp',
      'un rótulo intermedio no se dibuja si la raya de la mitad o la barra «Tú» caen en su caja más 2 dp: queda «0 aciertos · 32 · 95 o más»'),
     ('t206', 'ahora_stroop_bell_360_10', 360, 760, 'el eje · antes (T206) · 360 dp',
      'a 360 dp el «63» está exactamente bajo la raya'),
     ('t207', 'ahora_axis_360_10', 360, 760, 'el eje · ahora (T207) · 360 dp',
      'a 360 dp igual: los extremos se dibujan siempre y el «63» ya no está')],
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


def load(folder, key, w, crop_h):
    im = Image.open(os.path.join(SHOTS, folder, key + '.png')).convert('RGB')
    h = int(round(im.height * w / im.width))
    im = im.resize((w, h), Image.LANCZOS)          # tamaño real: 1 dp = 1 px
    return im.crop((0, 0, w, min(h, crop_h))) if crop_h else im


def main():
    d0 = ImageDraw.Draw(Image.new('RGB', (10, 10)))
    tf, sf = font(17), font(15, bold=False)
    sheet_w = MARGIN * 2 + 4 * COLW + 3 * GAP
    heights = []
    for row in ROWS:
        shots = max(load(f, k, w, c).height for f, k, w, c, _, _ in row)
        cap = max(len(wrap(d0, t, sf, COLW)) * 20 for _, _, _, _, _, t in row)
        heights.append(26 + shots + 12 + cap + 34)
    sheet = Image.new('RGB', (sheet_w, MARGIN + sum(heights)), NAVY)
    d = ImageDraw.Draw(sheet)
    y = MARGIN
    for row, hrow in zip(ROWS, heights):
        x = MARGIN
        for folder, key, w, crop, title, text in row:
            d.text((x, y), title, font=tf, fill=HOYC if 'antes' in title else SUN)
            im = load(folder, key, w, crop)
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
