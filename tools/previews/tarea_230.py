# -*- coding: utf-8 -*-
"""Lámina de la Tarea 230 (4-nov-2026): «Etapa» sale de la app. Antes (código de la Tarea 229, HEAD f6f183fc) y ahora, en cinco pantallas: la página de un área de Avance, la ficha de un juego, la pantalla de carga, Tú y el final de Bodega con «Nivel más alto»;
cada una a 393 dp con letra 1,0 y a 360 dp con letra 2,0.
Las capturas son REALES de Robolectric/Roborazzi (`Tarea230CapturasTest`, xxhdpi: 3 px por dp), grabadas una vez sobre el código de antes y otra sobre el de ahora, y están en `unity/test-results/t230/` (fuera de git: `antes_<escena>.png` y `despues_<escena>.png`).
Uso:  python -I tools/previews/tarea_230.py      → docs/previews/tarea-230.png
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SHOTS = os.path.join(ROOT, 'unity', 'test-results', 't230')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-230.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
NAVY, SUN, DIM, LINE, WHITE, LIME = (20, 18, 63), (255, 230, 166), (200, 196, 235), (90, 86, 160), (240, 244, 255), (176, 235, 110)
K = 3.0                                                       # los PNG son xxhdpi: 3 px por dp; en la lámina 1 dp = 1 px
COL, GAP, MARGIN = 393, 28, 24


def font(px, bold=True): return ImageFont.truetype(os.path.join(FONTS, 'fredoka.ttf' if bold else 'nunito_regular.ttf'), int(px))


def crop(prefix, scene, top, bottom):
    im = Image.open(os.path.join(SHOTS, '%s_%s.png' % (prefix, scene))).convert('RGB')
    c = im.crop((0, int(top * K), im.width, min(int(bottom * K), im.height)))
    return c.resize((int(round(c.width / K)), int(round(c.height / K))), Image.LANCZOS)


# (título, escena de 393 dp con letra 1,0, recorte, escena de 360 dp con letra 2,0, recorte, qué cambia)
ROWS = [
    ('1 · Página de un área de Avance', 'avance', (0, 520), 'avance', (0, 700),
     'Sale «· Etapa Aprendiz» junto a los puntos, la tira «Etapas: Inicio · Aprendiz · Hábil · Experto · Maestro. Vas en Aprendiz» y «Te faltan 67 puntos para Hábil.». Queda el número de puntos, la barra (sin las marcas de las etapas) y la frase de la semana.'),
    ('2 · Ficha de un juego', 'ficha', (0, 545), 'ficha', (0, 760),
     '«Etapa Experto · 180 de 250 puntos» pasa a «180 de 250 puntos»; la barra pierde sus marcas.'),
    ('3 · Pantalla de carga', 'carga', (400, 650), 'carga', (400, 620),
     '«Etapa Hábil · Con reloj» pasa a «Con reloj».'),
    ('4 · Tú: «N partidas»', 'tu', (0, 520), 'tu', (0, 900),
     'La persona de la captura tiene 10 partidas de siempre y 5 de Contrarreloj: Tú decía «10 partidas» y ahora dice «15». Las medallas por número de partidas cuentan igual.'),
    ('5 · Final de Bodega de carga (detalle abierto)', 'final_bodega', (600, 880), 'final_bodega', (1060, 1640),
     '«Etapa más alta: 3 de 5 (cajas que se mueven)» pasa a «Nivel más alto: 3 de 5 (cajas que se mueven)». Igual en Constelaciones, Engranajes y Correo.'),
]


def wrap(d, text, f, width):
    lines, cur = [], ''
    for word in text.split(' '):
        t = (cur + ' ' + word).strip()
        if d.textlength(t, font=f) <= width: cur = t
        else: lines.append(cur); cur = word
    lines.append(cur)
    return lines


def main():
    sf, tf, hf = font(15, bold=False), font(19), font(15)
    W = MARGIN * 2 + 2 * COL + GAP
    tmp = Image.new('RGB', (W, 10)); td = ImageDraw.Draw(tmp)
    blocks = []
    for title, s1, r1, s2, r2, note in ROWS:
        row = []
        for scene, r in ((s1 + '_393_10', r1), (s2 + '_360_20', r2)):
            row.append((crop('antes', scene, *r), crop('despues', scene, *r)))
        blocks.append((title, row, note))
    H = MARGIN + 84
    for title, row, note in blocks:
        n = len(wrap(td, note, sf, W - 2 * MARGIN))
        H += 34 + 24 + max(a.height for a, _ in [row[0]]) + 18 + 22 + 24 + max(a.height for a, _ in [row[1]]) + 14 + n * 20 + 30
    H += 330
    sheet = Image.new('RGB', (W, H), NAVY); d = ImageDraw.Draw(sheet)
    d.text((MARGIN, MARGIN), 'Tarea 230 · «Etapa» sale de la app', font=font(28), fill=SUN)
    sub = 'Capturas REALES de las pruebas (Roborazzi): a la izquierda el código de antes (Tarea 229), a la derecha el de ahora. Arriba 393 dp con letra 1,0; debajo 360 dp con letra 2,0.'
    yy = MARGIN + 40
    for line in wrap(d, sub, sf, W - 2 * MARGIN):
        d.text((MARGIN, yy), line, font=sf, fill=DIM); yy += 20
    y = MARGIN + 84
    for title, row, note in blocks:
        d.text((MARGIN, y), title, font=tf, fill=WHITE); y += 34
        for label, (a, b) in zip(('393 dp · letra 1,0', '360 dp · letra 2,0'), row):
            d.text((MARGIN, y), label, font=hf, fill=DIM); y += 24
            for i, (im, cap) in enumerate(((a, 'ANTES'), (b, 'AHORA'))):
                x = MARGIN + i * (COL + GAP)
                sheet.paste(im, (x + (COL - im.width) // 2, y)); d.rectangle((x + (COL - im.width) // 2 - 1, y - 1, x + (COL - im.width) // 2 + im.width, y + im.height), outline=LINE)
                d.text((x + 8, y + im.height + 4), cap, font=hf, fill=LIME if cap == 'AHORA' else DIM)
            y += a.height + 28
        for line in wrap(d, note, sf, W - 2 * MARGIN):
            d.text((MARGIN, y), line, font=sf, fill=WHITE); y += 20
        y += 30
    notes = ('Lo que no se ve en las capturas: sale la fila «etapa» del final antiguo y la celebración «¡Subiste de etapa!» (avisaba cada subida de nivel por debajo del 5; no se puso otra: queda «¡Tu maestría sube!» desde el nivel 5); '
             'el Primer vuelo ya no dice la palabra de la etapa (Hábil, Aprendiz…) junto a «tu fuerte hoy»; «Te mantuviste» pasa a «Tus puntos de Razonamiento no cambiaron» (resumen de la ruta) y «Esta semana, tus puntos de Memoria no cambiaron.», para no chocar con «te quedas» de la liga; '
             'y una medalla de constancia o de partidas ganada con un Contrarreloj se celebra al terminar ESE Contrarreloj. '
             'La guardia de textos falla si un texto visible vuelve a decir «etapa». En Unity solo queda un texto visible con «etapa»: «Etapa más alta» en el resumen del día de Correo (MailGameController.Scene.cs:871); no se tocó.')
    for line in wrap(d, notes, sf, W - 2 * MARGIN):
        d.text((MARGIN, y), line, font=sf, fill=WHITE); y += 20
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    sheet.crop((0, 0, W, y + MARGIN)).save(OUT, optimize=True)
    print('listo', OUT, sheet.crop((0, 0, W, y + MARGIN)).size)


main()
