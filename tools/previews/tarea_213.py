# -*- coding: utf-8 -*-
"""Lámina de la Tarea 213 (2-nov-2026): «hoy | T213», los arreglos tras las simulaciones de MiroFish del 2-nov (`auditoria-fable/mirofish-2nov-arreglos.md`; decisiones de Ricardo: la cifra en UNA frase con «referencia estimada»,
la fila «Con qué te comparas ›» DEBAJO de la losa de la cifra y sin el pie largo, la hoja de la referencia con su primera frase nueva, las frases de cinco juegos y la pregunta frecuente del cambio de teléfono).
A TAMAÑO REAL (1 dp = 1 px) y del EMULADOR (`medium_phone`, API 36; 440 de densidad = 393 dp y 480 = 360 dp). «hoy» = el APK de la Tarea 212 (instalado antes); «T213» = el APK de esta tarea.
Fila 1: a 393 dp con letra 1,0, la cifra con su fila y la hoja «De dónde sale la referencia», hoy frente a T213. Fila 2: lo mismo a 360 dp con letra 2,0. Fila 3: la pregunta frecuente «¿Qué pasa si cambio de teléfono?», a 393 dp con 1,0 y a 360 dp con 2,0.
Filas 4 y 5: las cinco fichas con su frase nueva (solo T213: lo que cambió es el texto de una línea; las frases de antes van en cada título) a 393 dp con 1,0 y a 360 dp con 2,0.
Las capturas (`tools/previews/` no las guarda) salen de `emu213.py`, que maneja SOLO el emulador, y van a `app/src/test/screenshots/t213/` (fuera de git).
Uso:  python -I tools/previews/tarea_213.py      → docs/previews/tarea-213.png
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SHOTS = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots', 't213')
RES = os.path.join(ROOT, 'app', 'src', 'main', 'res')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-213.png')
FONTS = os.path.join(RES, 'font')
NAVY, SUN, DIM, LINE = (20, 18, 63), (255, 230, 166), (200, 196, 235), (90, 86, 160)
GAP, MARGIN, COLW = 36, 24, 393

ROWS = [
    # (clave de la captura, ancho en dp, título, texto)
    [('hoy_hero_393_10', 393, 'hoy (T212) · 393 dp · letra 1,0 · emulador', 'la cifra es «5 de cada 100» con una frase de tres renglones debajo («marcas de la referencia estimada … quedan detrás de tu mejor marca…»), y la fuente se explica al final de la página'),
     ('t213_hero_393_10', 393, 'T213 · 393 dp · letra 1,0 · emulador', 'UNA frase, «Con 46 aciertos, superas al 5 % de la referencia estimada para tu edad.», en Rubik 24; debajo, igual, la división con su emblema y su lugar. La fila «Con qué te comparas ›» va DEBAJO de la losa de la cifra, en sol sobre el noche'),
     ('hoy_sheet_393_10', 393, 'hoy (T212) · hoja de la referencia · 393 dp · 1,0', 'abre con «No son otras personas: Nubi no junta datos de nadie…» y un solo párrafo largo; después van los estudios'),
     ('t213_sheet_393_10', 393, 'T213 · hoja de la referencia · 393 dp · 1,0', 'tres párrafos: primero «Te comparas con una referencia estimada para tu edad, no con otras personas. Nubi no junta datos de nadie: tus marcas se quedan en tu teléfono.», después cómo se armó y cómo se calcularon los números')],
    [('hoy_hero_360_20', 360, 'hoy (T212) · 360 dp · letra 2,0 · emulador', 'la cifra de 40 sp con su frase, a la letra más grande en lo más angosto'),
     ('t213_hero_360_20', 360, 'T213 · 360 dp · letra 2,0 · emulador', 'la frase de la cifra crece hasta el tope de 1,3 (como las demás cifras) y parte por palabras; la fila queda debajo de la losa y se toca entera'),
     ('hoy_sheet_360_20', 360, 'hoy (T212) · hoja de la referencia · 360 dp · 2,0', 'el primer párrafo, con la letra grande'),
     ('t213_sheet_360_20', 360, 'T213 · hoja de la referencia · 360 dp · 2,0', 'la primera frase nueva y los párrafos siguientes, con la letra grande')],
    [('hoy_faq_393_10', 393, 'hoy (T212) · «¿Qué pasa si cambio de teléfono?» · 393 dp · 1,0', 'la respuesta de antes'),
     ('t213_faq_393_10', 393, 'T213 · «¿Qué pasa si cambio de teléfono?» · 393 dp · 1,0', 'primero dónde vive el avance y que Nubi no lo envía; la copia es de Android, en tu cuenta de Google, y Nubi no la ve; vuelven partidas, avance por juego, racha, medallas y semana; sin copia, queda en este teléfono'),
     ('hoy_faq_360_20', 360, 'hoy (T212) · la misma pregunta · 360 dp · 2,0', 'la respuesta de antes con la letra grande'),
     ('t213_faq_360_20', 360, 'T213 · la misma pregunta · 360 dp · 2,0', 'la respuesta nueva, completa, con la letra grande; la tarjeta crece y la página se desliza')],
]

FICHAS = [('freno', 'Freno de Emergencia', 'Lanza cuando toca y espera cuando no', 'Frena cuando suena la alarma'),
          ('meteoros', 'Lluvia de meteoros', 'Reconoce las palabras de verdad', 'Toca solo las palabras que existen'),
          ('disparate', '¿Verdad o disparate?', 'Lee la frase y di si tiene sentido', 'Lee y decide rápido'),
          ('piloto', 'Piloto Estelar', 'Guía la nave y atiende las señales a la vez', 'Pilota y atrapa las señales de tu misión'),
          ('aterrizaje', 'Aterrizaje Lunar', 'Calcula dónde va cada número en la regla', 'Posa la nave en el número')]


def font(px, bold=True): return ImageFont.truetype(os.path.join(FONTS, 'fredoka.ttf' if bold else 'nunito_regular.ttf'), int(px))


def wrap(draw, text, f, width):
    lines, cur = [], ''
    for word in text.split(' '):
        t = (cur + ' ' + word).strip()
        if draw.textlength(t, font=f) <= width: cur = t
        else: lines.append(cur); cur = word
    if cur: lines.append(cur)
    return lines


def load(key, dp):
    path = os.path.join(SHOTS, key + '.png')
    if not os.path.exists(path):
        im = Image.new('RGB', (dp, 873), (40, 36, 100)); ImageDraw.Draw(im).text((20, 400), 'falta ' + key, font=font(20), fill=SUN); return im
    im = Image.open(path).convert('RGB')
    return im.resize((dp, int(round(im.height * dp / im.width))), Image.LANCZOS)          # tamaño real: 1 dp = 1 px


def main():
    d0 = ImageDraw.Draw(Image.new('RGB', (10, 10)))
    tf, sf = font(17), font(15, bold=False)
    fichas = [[('t213_ficha_%s_393_10' % g, 393, '%s · 393 dp · letra 1,0' % n, '«%s» (antes «%s»)' % (new, old)) for g, n, new, old in FICHAS],
              [('t213_ficha_%s_360_20' % g, 360, '%s · 360 dp · letra 2,0' % n, '«%s» (antes «%s»)' % (new, old)) for g, n, new, old in FICHAS]]
    all_rows = ROWS + fichas
    ncols = max(len(r) for r in all_rows)
    sheet_w = MARGIN * 2 + ncols * COLW + (ncols - 1) * GAP
    head = 96
    heights = []
    for row in all_rows:
        shots = max(load(k, dp).height for k, dp, _, _ in row)
        cap = max(len(wrap(d0, t, sf, COLW)) * 20 for _, _, _, t in row)
        heights.append(26 + shots + 12 + cap + 34)
    sheet = Image.new('RGB', (sheet_w, MARGIN + head + sum(heights) + MARGIN), NAVY)
    d = ImageDraw.Draw(sheet)
    d.text((MARGIN, MARGIN), 'Tarea 213 · hoy | T213 · arreglos tras las simulaciones de MiroFish del 2-nov', font=font(26), fill=SUN)
    intro = ('Lo que cambió: la cifra del Contrarreloj, ahora UNA frase con «referencia estimada»; la fila «Con qué te comparas ›», debajo de la losa de la cifra (el pie largo de la página ya no está); la primera frase nueva de la hoja de la referencia; '
             'las frases de cinco juegos; y la respuesta del cambio de teléfono. Capturas del emulador a tamaño real (1 dp = 1 px).')
    for i, line in enumerate(wrap(d, intro, sf, sheet_w - 2 * MARGIN)):
        d.text((MARGIN, MARGIN + 40 + i * 20), line, font=sf, fill=DIM)
    y = MARGIN + head
    for row, hrow in zip(all_rows, heights):
        x = MARGIN
        for key, dp, title, text in row:
            size = 17
            while size > 11 and d.textlength(title, font=font(size)) > COLW: size -= 1          # un título largo no pisa al de la columna de al lado
            d.text((x, y), title, font=font(size), fill=SUN)
            im = load(key, dp)
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
