# -*- coding: utf-8 -*-
"""Lámina «hoy | propuesta» de la Tarea 193 (30-oct-2026; `auditoria-fable/guia-primer-ingreso.md`, propuesta A, con las decisiones de Ricardo y de Opus): la guía de Nubi en la primera entrada a Hoy, solo la primera medalla y la tarjeta del día del vuelo con los juegos de verdad.
Todo son capturas REALES a 393 × 873 dp y tamaño real (1 px = 1 dp): las de Roborazzi salen de `HoyGuideScreenshotTest` (`./gradlew.bat :app:recordRoborazziDebug --tests "*HoyGuideScreenshotTest*"` → `app/src/test/screenshots/guia/`; el Hoy de verdad, la barra real y el velo) y las de letra 2,0
del emulador (`medium_phone`, API 36) de `unity/test-results/capturas-193/` (fuera de git; si no están, esa fila se omite). «HOY» = lo de antes (3 filas de la cola del día, dos frases); «PROPUESTA» = lo nuevo.
Uso:  python -I tools/previews/tarea_193.py      → docs/previews/tarea-193.png
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SHOTS = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots', 'guia')
EMU = os.path.join(ROOT, 'unity', 'test-results', 'capturas-193')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-193.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
NAVY, SUN, DIM, CREAM = (20, 18, 63), (255, 203, 69), (200, 196, 235), (245, 235, 219)
W, H, GAP, MARGIN = 393, 873, 14, 24


def font(px, bold=True): return ImageFont.truetype(os.path.join(FONTS, 'fredoka.ttf' if bold else 'nunito_regular.ttf'), int(px))


def wrap(draw, text, f, width):
    lines, cur = [], ''
    for word in text.split(' '):
        t = (cur + ' ' + word).strip()
        if draw.textlength(t, font=f) <= width: cur = t
        else: lines.append(cur); cur = word
    if cur: lines.append(cur)
    return lines


# (título de la sección, [(archivo, dónde, rótulo, qué cambió)])
SECTIONS = [
    ('LA TARJETA DEL DÍA DEL VUELO · hoy | propuesta, letra 1,0 y 1,3', [
        ('hoy_viejo_10', SHOTS, 'HOY · letra 1,0', '«3 de 3» con tres juegos de la cola del día (Bodega, Carga, Estrella intrusa), aunque nadie los jugó; la línea, dos frases'),
        ('propuesta_10', SHOTS, 'PROPUESTA · letra 1,0', 'los 4 juegos que se jugaron en el vuelo, en su orden, con «Hecho»; «4 de 4 · ruta completa»; la línea, UNA frase. Cabe sin deslizar'),
        ('hoy_viejo_13', SHOTS, 'HOY · letra 1,3', 'con la regla conservadora de la prueba (la barra y 75 dp de aire) ya se pasa 29 dp; con la barra real cabe'),
        ('propuesta_13', SHOTS, 'PROPUESTA · letra 1,3', 'con 4 filas, la regla conservadora de la prueba se pasa 90 dp; en el emulador (barra real de 88 dp) cabe justo. Si algo no cabe, se desliza y la guía lo trae a la vista'),
        ('medalla_unica', SHOTS, 'LA MEDALLA ÚNICA', 'al terminar el vuelo se celebra SOLO «Primer paso»; «Día completo» y «Mente completa» se ganan igual y quedan en Tú › Medallas'),
    ]),
    ('LA GUÍA DE NUBI · letra 1,0 (tres pasos y Hoy después)', [
        ('paso1_10', SHOTS, 'Paso 1 de 3', 'el hueco con aro lima sobre «Contrarreloj»; Nubi M y su globo; «Seguir ›» lila y «Saltar la guía» crema'),
        ('paso2_10', SHOTS, 'Paso 2 de 3', 'el hueco baja a «Todos los juegos»'),
        ('paso3_10', SHOTS, 'Paso 3 de 3', 'las pestañas Avance y Tú; el chip dice «Listo», sin flecha'),
        ('propuesta_10', SHOTS, 'Después', 'Hoy queda como estaba; sin rastro de la guía'),
    ]),
    ('LA GUÍA · letra 1,3 (Nubi pasa a S; Hoy se desliza y trae el blanco entero sobre la barra)', [
        ('paso1_13', SHOTS, 'Paso 1 de 3', 'Hoy ya se trajo a la vista: el botón entero y sobre la barra'),
        ('paso2_13', SHOTS, 'Paso 2 de 3', 'el botón se trae a la vista, entero y fuera de la barra, ANTES de dibujar el hueco'),
        ('paso3_13', SHOTS, 'Paso 3 de 3', 'la barra'),
        ('propuesta_13', SHOTS, 'Después', 'Hoy sin guía (sin deslizar)'),
    ]),
    ('LA GUÍA · letra 2,0 en el emulador (medium_phone, API 36)', [
        ('letra20-paso1', EMU, 'Paso 1 · letra 2,0', 'Hoy se desliza y trae el botón a la vista; el contador pasa bajo el texto y el chip bajo el contador (nada se parte)'),
        ('letra20-paso2', EMU, 'Paso 2 · letra 2,0', '«Todos los juegos» entero, sobre la barra, con su hueco'),
        ('letra20-paso3', EMU, 'Paso 3 · letra 2,0', 'el hueco coincide con la barra real y el globo queda encima'),
    ]),
]


def main():
    d0 = ImageDraw.Draw(Image.new('RGB', (10, 10)))
    tf, cf, sf = font(26), font(17), font(15, bold=False)
    frames_per_row = max(len(items) for _, items in SECTIONS)
    sheet_w = MARGIN * 2 + frames_per_row * W + (frames_per_row - 1) * GAP
    rows = []
    for title, items in SECTIONS:
        have = [(n, p, c, t) for n, p, c, t in items if os.path.exists(os.path.join(p, n + '.png'))]
        if have: rows.append((title, have))
    # alto de cada fila: título + imágenes + los rótulos más largos
    heights = []
    for title, items in rows:
        cap_h = 0
        for n, p, cap, text in items:
            lines = wrap(d0, text, sf, W - 4)
            cap_h = max(cap_h, 26 + len(lines) * 20)
        heights.append(44 + H + 10 + cap_h + 34)
    sheet = Image.new('RGB', (sheet_w, MARGIN + sum(heights) + 10), NAVY)
    d = ImageDraw.Draw(sheet)
    y = MARGIN
    for (title, items), hrow in zip(rows, heights):
        d.text((MARGIN, y), title, font=cf, fill=SUN)
        y0 = y + 44
        for k, (n, p, cap, text) in enumerate(items):
            x = MARGIN + k * (W + GAP)
            im = Image.open(os.path.join(p, n + '.png')).convert('RGB')
            sheet.paste(im.resize((W, H), Image.LANCZOS), (x, y0))
            d.rectangle((x - 1, y0 - 1, x + W, y0 + H), outline=(90, 86, 160))
            d.text((x, y0 + H + 8), cap, font=font(17), fill=CREAM)
            for i, line in enumerate(wrap(d, text, sf, W - 4)):
                d.text((x, y0 + H + 34 + i * 20), line, font=sf, fill=DIM)
        y += hrow
    sheet.save(OUT)
    print(OUT, sheet.size)


main()
