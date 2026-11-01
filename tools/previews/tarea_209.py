# -*- coding: utf-8 -*-
"""Lámina de la Tarea 209 (1-nov-2026): «Claridad de Plus y ayuda» (`auditoria-fable/claridad-plus-y-ayuda.md`, ideas 3 a 6; decisiones de Ricardo: «percentil» fuera, «un psicólogo chileno» sin nombre, la IA solo en la pregunta frecuente, el segundo párrafo de «De dónde sale la referencia» con la redacción de Opus).
A TAMAÑO REAL (1 dp = 1 px, 393 dp de ancho). Fila 1: la comparación (frase nueva, enlace y cifra con el lugar de la división), la puerta de Avance sin Plus (frase gratis con el lugar y candado largo), la hoja del candado (lo gratis y lo de Plus, la razón y el enlace) y la página de 13 a 17 años con su línea.
Fila 2: «Preguntas frecuentes» (dos desplegadas), «De dónde sale la referencia» (arriba y el final) y Tú en el EMULADOR (el botón y «Quién hace Nubi»). Filas 3 a 5: lo que va a letra 2,0 en el EMULADOR (`medium_phone`, `font_scale 2.0`, `wm density 440` = 393 dp y 480 = 360 dp, `tools/emulador_ui.py`): la frase de la comparación con su enlace, la puerta, la hoja real del candado, la página de 13 a 17 años, las preguntas y «De dónde sale la referencia» a 393 y a 360 dp.
Las de Roborazzi salen de `Tarea209CapturasTest`; las del emulador de `emu209.py`; todo en `app/src/test/screenshots/t209/` (fuera de git).
Uso:  python -I tools/previews/tarea_209.py      → docs/previews/tarea-209.png
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SHOTS = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots', 't209')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-209.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
NAVY, SUN, DIM, LINE = (20, 18, 63), (255, 203, 69), (200, 196, 235), (90, 86, 160)
GAP, MARGIN, COLW = 36, 24, 393

# (captura, título, qué se ve); cada fila trae hasta 4 columnas, todas de 393 x 873 dp
ROWS = [
    [('ahora_comparacion_393_10', 'la comparación · letra 1,0',
      'la razón primero: «No son otras personas: Nubi no junta datos de nadie. Por eso la referencia… es estimada…», la fila «De dónde sale la referencia ›» (48 dp) y la cifra con su lugar: «En órbita» y «La tercera de cinco divisiones.», sin «Percentil»'),
     ('ahora_puerta_393_10', 'la puerta de Avance sin Plus · letra 1,0',
      'la frase gratis dice el lugar («…En órbita, la tercera de cinco divisiones de la referencia estimada para tu edad. Te faltan N aciertos…») y el candado dice qué agrega Plus: «Con Nubi Plus: además, dónde queda tu marca frente a tu edad y a otras edades.»'),
     ('ahora_hoja_393_10', 'la hoja del candado · letra 1,0',
      '«Gratis: tu división de la semana. Con Nubi Plus, además:», las tres líneas nuevas, «La referencia no son personas: Nubi no junta datos de nadie.» y la fila que abre la hoja de los estudios; «Cerrar» sigue siendo el único botón'),
     ('ahora_menores_393_10', 'la página de 13 a 17 años · letra 1,0',
      'ya no calla: una línea corrida, «De 13 a 17 años, el Contrarreloj muestra solo tus propias marcas.», y el subtítulo sigue diciendo que se compara solo contigo')],
    [('ahora_preguntas_393_10', 'Preguntas frecuentes · letra 1,0',
      'seis losas plegables (aquí abiertas la del teléfono y la del Contrarreloj, que lleva «De dónde sale la referencia ›»); las dos del cobro quedan apagadas con Features.BILLING'),
     ('ahora_referencia_393_10', 'De dónde sale la referencia · letra 1,0',
      'primero «No son otras personas…» y qué es la referencia, el segundo párrafo con la redacción de Opus y los estudios con «Autor, año · N personas», su línea y «Ver el estudio ›»'),
     ('ahora_referencia_fin_393_10', 'De dónde sale la referencia · el final',
      'el último grupo, ¿Verdad o disparate? y Acoplamiento, no tiene losas: solo la línea «Sus tiempos salen de los mismos estudios de rapidez…»'),
     ('emu_tu_393_10', 'Tú · letra 1,0 · emulador',
      '«Preguntas frecuentes» (tercer botón crema, con el glifo de información del kit) y la sección «Quién hace Nubi»: «Un psicólogo chileno decide cada juego y cada texto.», antes de Contacto')],
    [('emu_nota_393_20', 'la comparación · 393 dp · letra 2,0 · emulador',
      'la frase («No son otras personas…») parte por palabras, «45 a 54 años» no se separa, la fila «De dónde sale la referencia ›» pasa a dos líneas y mide más de 48 dp, y la cifra sigue debajo'),
     ('emu_nota_360_20', 'la comparación · 360 dp · letra 2,0 · emulador',
      'lo mismo en lo más angosto con la letra más grande: nada cortado ni encimado'),
     ('emu_puerta_fin_393_20', 'la puerta · 393 dp · letra 2,0 · emulador',
      'la puerta con letra 2,0 mide 670 dp (485 antes de la Tarea 209): la frase gratis y el candado largo parten por palabras; Avance se desliza'),
     ('emu_puerta_fin_360_20', 'la puerta · 360 dp · letra 2,0 · emulador',
      'a 360 dp mide 744 dp con 2,0 (485 antes): casi una pantalla, y todo se lee sin cortes')],
    [('emu_hoja_393_20', 'la hoja del candado · 393 dp · letra 2,0 · emulador',
      'la hoja REAL (un Dialog) con letra 2,0: se desliza'),
     ('emu_hoja_fin_393_20', 'la hoja · 393 dp · letra 2,0 · el final',
      'la razón, el enlace «De dónde sale la referencia ›» y «Cerrar», que se alcanza deslizando'),
     ('emu_hoja_fin_360_20', 'la hoja · 360 dp · letra 2,0 · el final',
      'lo mismo a 360 dp'),
     ('emu_menores_360_20', 'la página de 13 a 17 años · 360 dp · letra 2,0 · emulador',
      'la línea de 13 a 17 años con letra 2,0 en lo más angosto')],
    [('emu_preguntas_393_20', 'Preguntas frecuentes · 393 dp · letra 2,0 · emulador',
      'la pregunta pasa a dos líneas por palabras, el chevron queda a su lado y la respuesta se lee entera'),
     ('emu_preguntas_360_20', 'Preguntas frecuentes · 360 dp · letra 2,0 · emulador',
      'a 360 dp con 2,0: tres líneas de pregunta y nada se corta'),
     ('emu_referencia_360_20', 'De dónde sale la referencia · 360 dp · letra 2,0 · emulador',
      'el título del panel pasa a dos líneas por palabras («De dónde sale / la referencia») y el texto no se parte'),
     ('ahora_puerta_393_20', 'la puerta · letra 2,0 · Roborazzi',
      'la misma puerta fuera del emulador (Roborazzi mide la letra de verdad porque no es un Dialog)')],
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
    path = os.path.join(SHOTS, key + '.png')
    if not os.path.exists(path):
        im = Image.new('RGB', (COLW, 873), (40, 36, 100)); ImageDraw.Draw(im).text((20, 400), 'falta ' + key, font=font(20), fill=SUN); return im
    im = Image.open(path).convert('RGB')
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
            size = 17
            while size > 12 and d.textlength(title, font=font(size)) > COLW: size -= 1          # un título largo no pisa al de la columna de al lado
            d.text((x, y), title, font=font(size), fill=SUN)
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
