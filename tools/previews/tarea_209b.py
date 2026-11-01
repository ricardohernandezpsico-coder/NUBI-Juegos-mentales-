# -*- coding: utf-8 -*-
"""Lámina de la Tarea 209b (1-nov-2026): tres arreglos chicos tras la auditoría de Fable a la Tarea 209 (`auditoria-fable/t209-auditoria.md`).
A TAMAÑO REAL (1 dp = 1 px, 393 dp de ancho), todo del EMULADOR (`medium_phone`, API 36). Fila 1: la página Contrarreloj bajada hasta «De dónde sale la referencia ›», el panel y la vuelta (la página sigue donde estaba, no arriba) y la losa «Semana a semana» con su título nuevo en UNA línea.
Fila 2: lo mismo en Tú: «En qué se basa Nubi» abierto desde la lista bajada y la vuelta a la misma posición.
Los PNG van a `app/src/test/screenshots/t209b/` (fuera de git).
Uso:  python -I tools/previews/tarea_209b.py      → docs/previews/tarea-209b.png
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SHOTS = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots', 't209b')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-209b.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
NAVY, SUN, DIM, LINE = (20, 18, 63), (255, 203, 69), (200, 196, 235), (90, 86, 160)
GAP, MARGIN, COLW = 36, 24, 393

ROWS = [
    [('contrarreloj_antes_vuelta', 'antes: al volver, arriba · emulador',
      'la app de la Tarea 209: tras tocar «De dónde sale la referencia ›» a mitad de página, al volver Contrarreloj arrancaba en el tope y había que deslizar de nuevo'),
     ('contrarreloj_bajada', 'ahora: la página bajada · emulador',
      'Contrarreloj deslizada con la fila «De dónde sale la referencia ›» a la vista (la fila está a mitad de página)'),
     ('panel_referencia', 'el panel de los estudios · emulador',
      'la fila abre «De dónde sale la referencia» sobre el cielo y sin la barra de pestañas, como siempre; Atrás vuelve'),
     ('contrarreloj_vuelta', 'ahora: al volver, donde estaba · emulador',
      'la misma página, con la fila en el mismo lugar de la pantalla (y=1108 px antes y después; la app vieja saltaba de 909 a 1446)')],
    [('tu_bajada', 'Tú bajada hasta «En qué se basa Nubi» · emulador',
      'la lista de Tú deslizada hasta el botón (lo mismo vale para «Más ajustes» y «Preguntas frecuentes»)'),
     ('tu_panel', '«En qué se basa Nubi» · emulador',
      'el panel abierto desde ese botón'),
     ('tu_vuelta', 'al volver con Atrás · emulador',
      'Tú sigue donde estaba: el botón queda en el mismo lugar (y=1269 px antes y después de los tres paneles; la app vieja volvía arriba)'),
     ('semana_a_semana_393', '«Semana a semana» · 393 dp · emulador',
      'el título nuevo en UNA línea (15 letras); antes, «Tu lugar cada semana» se parecía a «Tu lugar esta semana», la losa de la misma página cuando no hay marca esta semana')],
    [('semana_a_semana_360', '«Semana a semana» · 360 dp · emulador',
      'lo mismo en lo más angosto: una línea, con la letra de siempre (1,0)')],
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
