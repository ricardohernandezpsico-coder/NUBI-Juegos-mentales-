# -*- coding: utf-8 -*-
"""Lámina «hoy | propuesta» de la Tarea 194 (30-oct-2026): arreglos de lectura que pidió Ricardo tras probar la T193b en el teléfono. Capturas REALES de Roborazzi (`LecturaScreenshotTest`; `hoy` = el código de antes, `prop` = el nuevo) a 393 × 873 dp y tamaño real (1 px = 1 dp);
bajo «¿Sabías que…?», un recorte ampliado ×2 (2 px = 1 dp) de «poco y seguido» en las dos versiones, para comparar el borde de las letras.
Uso:  python -I tools/previews/tarea_194.py      → docs/previews/tarea-194.png
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SHOTS = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots', 'lectura')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-194.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
NAVY, SUN, DIM, CREAM = (20, 18, 63), (255, 203, 69), (200, 196, 235), (245, 235, 219)
W, H, GAP, PAIR_GAP, MARGIN = 393, 873, 12, 40, 24
SCALE_PX_PER_DP = 3                          # las capturas son xxhdpi (3 px por dp)
ZOOM_BOX = (350, 1340, 830, 1470)           # «poco y seguido» en la captura completa (px): 160 × 43 dp
ZOOM = 2                                     # 2 px por dp


def font(px, bold=True): return ImageFont.truetype(os.path.join(FONTS, 'fredoka.ttf' if bold else 'nunito_regular.ttf'), int(px))


def wrap(draw, text, f, width):
    lines, cur = [], ''
    for word in text.split(' '):
        t = (cur + ' ' + word).strip()
        if draw.textlength(t, font=f) <= width: cur = t
        else: lines.append(cur); cur = word
    if cur: lines.append(cur)
    return lines


# (clave de la captura, título del par, qué cambió, ¿con recorte ampliado?)
ROWS = [
    [('dato', '1 · «¿Sabías que…?» del Primer vuelo', 'el rótulo y «poco y seguido» pasan del sol saturado (#FFC93C) a un amarillo mantequilla (#FFE6A6). Y «poco y seguido» ya no sale inflado: su orilla navy se dibujaba en AMARILLO (el color de la palabra), ahora en navy; abajo, ×2', True),
     ('pregunta', '1 · una pregunta del vuelo', 'el mismo amarillo en el rótulo de TODAS las páginas del vuelo (un solo estilo); los botones sol no cambian', False)],
    [('medalla', '2 · la medalla única del vuelo', '«Medalla nueva» y «Primer paso» en el amarillo suave; el botón sol y el dibujo no cambian', False),
     ('coleccion', '2 · un final de partida con «Tu colección»', 'los títulos del detalle («Por grupo de palabras», «Tu reconocimiento», «Tu filtro», «Tu colección») en el amarillo suave; la cifra en lima no cambia', False)],
    [('tarjeta_10', '3 · la tarjeta del día del vuelo · letra 1,0', '«Tu primer vuelo fue tu ruta de hoy.» queda pegada a las filas: un espacio de 6 dp bajo la última (antes 18)', False),
     ('paso1_10', '4 · la guía, paso 1 · letra 1,0', 'el texto del globo en tinta, 18 sp y SemiBold (Fredoka, como los botones); «Paso 1 de 3» en tinta (antes tinta apagada)', False)],
    [('paso1_13', '4 · la guía, paso 1 · letra 1,3', 'con letra 1,3 el globo sigue cabiendo: el texto más firme no suma líneas', False)],
]


def zoom_of(pre):
    im = Image.open(os.path.join(SHOTS, '%s_dato.png' % pre)).convert('RGB').crop(ZOOM_BOX)
    w = (ZOOM_BOX[2] - ZOOM_BOX[0]) * ZOOM // SCALE_PX_PER_DP; h = (ZOOM_BOX[3] - ZOOM_BOX[1]) * ZOOM // SCALE_PX_PER_DP
    return im.resize((w, h), Image.LANCZOS)


def main():
    d0 = ImageDraw.Draw(Image.new('RGB', (10, 10)))
    tf, sf = font(17), font(15, bold=False)
    zw = (ZOOM_BOX[2] - ZOOM_BOX[0]) * ZOOM // SCALE_PX_PER_DP; zh = (ZOOM_BOX[3] - ZOOM_BOX[1]) * ZOOM // SCALE_PX_PER_DP
    pair_w = 2 * W + GAP
    per_row = max(len(r) for r in ROWS)
    sheet_w = MARGIN * 2 + per_row * pair_w + (per_row - 1) * PAIR_GAP
    heights = []
    for row in ROWS:
        extra = max((zh + 44 if z else 0) for _, _, _, z in row)
        cap_h = max(26 + len(wrap(d0, t, sf, pair_w - 4)) * 20 for _, _, t, _ in row)
        heights.append(40 + 28 + H + extra + 10 + cap_h + 30)
    sheet = Image.new('RGB', (sheet_w, MARGIN + sum(heights)), NAVY)
    d = ImageDraw.Draw(sheet)
    y = MARGIN
    for row, hrow in zip(ROWS, heights):
        for k, (key, title, text, z) in enumerate(row):
            x0 = MARGIN + k * (pair_w + PAIR_GAP)
            d.text((x0, y), title, font=tf, fill=SUN)
            for j, (pre, label) in enumerate((('hoy', 'HOY'), ('prop', 'PROPUESTA'))):
                x = x0 + j * (W + GAP)
                d.text((x, y + 26), label, font=font(15), fill=CREAM if pre == 'prop' else DIM)
                im = Image.open(os.path.join(SHOTS, '%s_%s.png' % (pre, key))).convert('RGB')
                sheet.paste(im.resize((W, H), Image.LANCZOS), (x, y + 50))
                d.rectangle((x - 1, y + 49, x + W, y + 50 + H), outline=(90, 86, 160))
            cy = y + 50 + H + 10
            if z:
                for j, (pre, label) in enumerate((('hoy', 'HOY · ampliado ×2'), ('prop', 'PROPUESTA · ampliado ×2'))):
                    x = x0 + j * (W + GAP)
                    d.text((x, cy), label, font=font(14), fill=DIM)
                    sheet.paste(zoom_of(pre), (x, cy + 22))
                    d.rectangle((x - 1, cy + 21, x + zw, cy + 22 + zh), outline=(90, 86, 160))
                cy += zh + 44
            for i, line in enumerate(wrap(d, text, sf, pair_w - 4)):
                d.text((x0, cy + i * 20), line, font=sf, fill=DIM)
        y += hrow
    sheet.save(OUT)
    print(OUT, sheet.size)


main()
