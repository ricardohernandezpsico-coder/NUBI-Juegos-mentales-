# -*- coding: utf-8 -*-
"""Lámina de la Tarea 215 (2-nov-2026): la barra de CUATRO pestañas y la pestaña «Contrarreloj» (`auditoria-fable/idea-pestana-contrarreloj.md` §1.3, §3 y §7; las 9 decisiones de Ricardo en su opción recomendada).
A TAMAÑO REAL (1 dp = 1 px). Fila 1: la barra ANTES (tres tercios, la de la Tarea 156) | AHORA (Hoy · Contrarreloj · Avance · Tú, columnas de ancho desigual medidas con el rótulo en negrita, losa sol al ancho de su columna menos 8 dp, rótulos con tope de letra 1,3) a 360 y 393 dp con letra 1,0 y 1,3.
Fila 2: la pestaña SIN Nubi Plus (lo gratis a color arriba; abajo, las dos losas grises con candado) y CON Nubi Plus (lo gratis y, debajo, «tu lugar en detalle»). Fila 3: con Plus «todas tus marcas», sin franja de edad, de 13 a 17 años y Avance sin la puerta del Contrarreloj. Fila 4: la pestaña sin Plus a 360 dp con letra 1,3.
La letra 2,0 se mira en el EMULADOR (no está en esta lámina; ver `docs/contrarreloj.md` §24). Las capturas salen de `Tarea215CapturasTest` (Roborazzi, `app/src/test/screenshots/t215/`, fuera de git) y de `AppActualScreenshotTest.avance` (`app-actual/avance.png`).
Uso:  python -I tools/previews/tarea_215.py      → docs/previews/tarea-215.png
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SHOTS = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots', 't215')
ACTUAL = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots', 'app-actual')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-215.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
NAVY, SUN, DIM, LINE = (20, 18, 63), (255, 203, 69), (200, 196, 235), (90, 86, 160)
GAP, MARGIN, COLW = 36, 24, 393


def font(px, bold=True): return ImageFont.truetype(os.path.join(FONTS, 'fredoka.ttf' if bold else 'nunito_regular.ttf'), int(px))


def wrap(draw, text, f, width):
    lines, cur = [], ''
    for word in text.split(' '):
        t = (cur + ' ' + word).strip()
        if draw.textlength(t, font=f) <= width: cur = t
        else: lines.append(cur); cur = word
    if cur: lines.append(cur)
    return lines


def load(path, dp_width):
    """La captura a tamaño real: 1 dp = 1 px (`dp_width` es el ancho de la captura en dp)."""
    if not os.path.exists(path):
        im = Image.new('RGB', (dp_width, 300), (40, 36, 100)); ImageDraw.Draw(im).text((20, 120), 'falta ' + os.path.basename(path), font=font(20), fill=SUN); return im
    im = Image.open(path).convert('RGB')
    return im.resize((dp_width, int(round(im.height * dp_width / im.width))), Image.LANCZOS)


def shot(name, w=393): return load(os.path.join(SHOTS, name + '.png'), w)


def bar_pair(w, scale):
    """La barra antes y ahora, una sobre otra (100 dp de alto cada una)."""
    s = '10' if scale == 1 else '13'
    a = shot('antes_barra_%d_%s_hoy' % (w, s), w); b = shot('ahora_barra_%d_%s_contrarreloj' % (w, s), w)
    out = Image.new('RGB', (COLW, a.height + 34 + b.height + 8), NAVY)
    d = ImageDraw.Draw(out)
    d.text((0, 0), 'antes (tres tercios)', font=font(14), fill=DIM); out.paste(a, (0, 18))
    d.text((0, 18 + a.height + 8), 'ahora (cuatro columnas)', font=font(14), fill=SUN); out.paste(b, (0, 18 + a.height + 8 + 18))
    return out


# cada fila: (titulo, texto, imagen)
def rows():
    return [
        [('la barra · 360 dp · letra 1,0', 'las columnas ya no son tercios: Contrarreloj mide lo que su rótulo en negrita, y la losa sol, su columna menos 8 dp', bar_pair(360, 1)),
         ('la barra · 393 dp · letra 1,0', 'la elegida no mueve nada (el ancho se mide siempre con la negrita); «Tú» queda con ~60 dp y toca 48 dp o más', bar_pair(393, 1)),
         ('la barra · 360 dp · letra 1,3', 'con letra 1,3 todo cabe en una línea; con 2,0 los rótulos no pasan de 1,3 (20,8 sp) y se mira en el emulador', bar_pair(360, 1.3)),
         ('la barra · 393 dp · letra 1,3', 'los rótulos topados en 1,3: la barra mide 95 dp y de ahí no sube', bar_pair(393, 1.3))],
        [('Contrarreloj sin Nubi Plus · 393 dp', 'arriba, a color y nunca en gris: la pestaña, «Jugar», el récord, la semana, la división y «Con qué te comparas ›». Los otros tres juegos, en gris con candado y su récord a la vista; «Hoy» marca el del día', shot('ahora_sin_plus_393_10')),
         ('Contrarreloj sin Plus · abajo', 'las DOS losas grises (la imagen crema del kit sin saturación y 12 % más oscura): título legible, candado, «Con Nubi Plus: …» en negrita y una silueta decorativa; toda la losa abre la hoja del candado', shot('ahora_sin_plus_abajo_393_10')),
         ('Contrarreloj con Nubi Plus · 393 dp', 'lo gratis arriba, igual; nada en gris y el selector se toca entero. Antes de la puerta, esta página vivía dentro de Avance', shot('ahora_con_plus_393_10')),
         ('con Plus · tu lugar en detalle', 'la frase «superas al N %», la escalera de divisiones (sin repetir lo que falta ni el domingo, que ya están arriba), la campana, otras edades y semana a semana', shot('ahora_con_plus_detalle_393_10'))],
        [('con Plus · todas tus marcas', 'las últimas 10, el ritmo y las notas; las tres cifras salieron porque lo gratis ya dice el récord y la semana', shot('ahora_con_plus_marcas_393_10')),
         ('sin franja de edad · sin Plus', 'no hay división ni losa gris de «tu lugar» (Plus tampoco la daría); solo «todas tus marcas» en gris y la línea que dice dónde elegir la edad', shot('ahora_sin_franja_393_10')),
         ('de 13 a 17 años · sin Plus', 'igual: la línea «De 13 a 17 años, el Contrarreloj muestra solo tus propias marcas.» y en gris solo «todas tus marcas»; el subtítulo vuelve a «solo contigo»', shot('ahora_menores_393_10')),
         ('Avance sin la puerta', 'la losa «Tus marcas del Contrarreloj» y su hoja se fueron: quedan las cuatro áreas, «Tus mejores marcas» y el punto de partida', load(os.path.join(ACTUAL, 'avance.png'), 393))],
        [('Contrarreloj sin Plus · 360 dp · letra 1,3', 'lo más angosto con letra grande: el selector pasa a una columna y nada se corta ni se encima', shot('ahora_sin_plus_360_13', 360))],
    ]


def main():
    d0 = ImageDraw.Draw(Image.new('RGB', (10, 10)))
    tf, sf = font(17), font(15, bold=False)
    R = rows()
    sheet_w = MARGIN * 2 + 4 * COLW + 3 * GAP
    heights = []
    for row in R:
        shots = max(im.height for _, _, im in row)
        cap = max(len(wrap(d0, t, sf, COLW)) * 20 for _, t, _ in row)
        heights.append(26 + shots + 12 + cap + 34)
    sheet = Image.new('RGB', (sheet_w, MARGIN + sum(heights)), NAVY)
    d = ImageDraw.Draw(sheet)
    y = MARGIN
    for row, hrow in zip(R, heights):
        x = MARGIN
        for title, text, im in row:
            size = 17
            while size > 12 and d.textlength(title, font=font(size)) > COLW: size -= 1
            d.text((x, y), title, font=font(size), fill=SUN)
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
