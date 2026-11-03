# -*- coding: utf-8 -*-
"""Lámina de la Tarea 217 (2-nov-2026): la app se llama «Nubilus» (`auditoria-fable/nombre-app.md` §7; la mascota sigue siendo Nubi).
A TAMAÑO REAL (1 dp = 1 px). Fila 1: el nombre bajo el ícono (réplica: el lanzador lo dibuja Android con `app_name`) y la marca de la tarjeta de compartir, Tú con las secciones renombradas, la hoja del candado con «Nubilus Plus» y «Acerca de Nubilus» a 393 dp con letra 1,0.
Fila 2: lo mismo con letra 2,0 a 393 dp en el emulador `medium_phone` (la barra, la pestaña Contrarreloj, Tú y Más ajustes › Acerca de Nubilus). Fila 3: lo más angosto con la letra más grande: 360 dp con 2,0 (emulador).
Las capturas salen de `Tarea217CapturasTest` (Roborazzi, `app/src/test/screenshots/t217/`, fuera de git) y del emulador (`emu_*.png`).
Uso:  python -I tools/previews/tarea_217.py      → docs/previews/tarea-217.png
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SHOTS = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots', 't217')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-217.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
ICON = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'mipmap-xxxhdpi', 'ic_launcher_round.webp')
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


def shot(name, dp_width=393, crop_dp=None):
    path = os.path.join(SHOTS, name + '.png')
    if not os.path.exists(path):
        im = Image.new('RGB', (dp_width, 300), (40, 36, 100)); ImageDraw.Draw(im).text((20, 120), 'falta ' + name, font=font(20), fill=SUN); return im
    im = Image.open(path).convert('RGB')
    im = im.resize((dp_width, int(round(im.height * dp_width / im.width))), Image.LANCZOS)
    return im.crop((0, 0, dp_width, crop_dp)) if crop_dp else im


def icon_and_card():
    """El nombre bajo el ícono (réplica con el ícono real de la app y la letra del sistema aproximada) con letra 1,0 y 1,3, y arriba de la tarjeta de compartir la marca nueva."""
    out = Image.new('RGB', (COLW, 400), (28, 26, 80)); d = ImageDraw.Draw(out)
    if os.path.exists(ICON):
        ic = Image.open(ICON).convert('RGBA').resize((64, 64), Image.LANCZOS)
        for i, (scale, label) in enumerate([(1.0, 'letra 1,0'), (1.3, 'letra 1,3')]):
            x = 40 + i * 180
            out.paste(ic, (x + 40, 24), ic)
            f = font(12 * scale, bold=False); w = d.textlength('Nubilus', font=f)
            d.text((x + 72 - w / 2, 96), 'Nubilus', font=f, fill=(255, 255, 255))
            d.text((x + 40, 124), label, font=font(12, bold=False), fill=DIM)
    else:
        d.text((20, 40), 'falta el ícono', font=font(18), fill=SUN)
    d.text((20, 160), 'la marca de la tarjeta de compartir', font=font(13, bold=False), fill=DIM)
    card_path = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots', 'compartir-medalla.png')
    if os.path.exists(card_path):
        c = Image.open(card_path).convert('RGB').crop((0, 0, 1080, 380)).resize((COLW, int(380 * COLW / 1080)), Image.LANCZOS)
        out.paste(c, (0, 184))
    return out.crop((0, 0, COLW, 184 + int(380 * COLW / 1080)))


ROWS = [
    [('el nombre bajo el ícono y la tarjeta de compartir', '«Nubilus» cabe entero bajo el ícono con letra 1,0 y 1,3 (54 y 70 dp medidos con la letra del proyecto; con 2,0 el lanzador lo recorta con puntos suspensivos, como a cualquier nombre largo). La marca de la tarjeta lleva el destello pegado a la «s», medido', icon_and_card()),
     ('Tú · secciones renombradas · 393 dp', '«En qué se basa Nubilus» (botón), «Quién hace Nubilus» y Contacto con «Escribir a Nubilus» y «Nubilus 1.0 · …»; la mascota sigue siendo Nubi en Hoy y en la guía', shot('tu_393_10')),
     ('la hoja del candado · «Nubilus Plus»', 'el título, la losa gris y «Con Nubilus Plus, además:»; el nombre del plan cambia en los doce textos; la mascota no se toca', shot('hoja_393_10')),
     ('Acerca de Nubilus · 393 dp · letra 1,0', '«Nubilus es para personas de 14 años o más.» pasa a dos renglones parejos (con «Nubi» cabía en uno); «14 años» queda junto y no sobra una palabra sola', shot('acerca_393_10'))],
    [('393 dp · letra 2,0 · la barra (emulador)', 'las cuatro columnas caben con el rótulo topado en 1,3: «Contrarreloj» entero; esta pantalla no lleva el nombre de la app', shot('emu_hoy_393_20')),
     ('393 dp · letra 2,0 · Contrarreloj (emulador)', 'abajo del selector, lo gratis y «Con qué te comparas ›»; la losa gris dice «Con Nubilus Plus: tu lugar en detalle.» y pasa a dos renglones por palabras', shot('emu_tab_393_20_c')),
     ('393 dp · letra 2,0 · Tú (emulador)', '«En qué se basa Nubilus» en dos renglones por palabras dentro de su botón; «Preguntas frecuentes» y «Quién hace Nubilus» en una línea', shot('emu_tu_base_393_20')),
     ('393 dp · letra 2,0 · Acerca de Nubilus', 'el título en una línea y la edad mínima en dos renglones; ninguna palabra partida (emulador)', shot('emu_acerca_393_20'))],
    [('360 dp · letra 2,0 · la barra (emulador)', 'lo más angosto con la letra más grande: las cuatro columnas siguen cabiendo', shot('emu_hoy_360_20', 360)),
     ('360 dp · letra 2,0 · Contrarreloj (emulador)', 'la losa gris con «Con Nubilus Plus: tu lugar en detalle.» pasa a dos renglones por palabras y nada se encima', shot('emu_tab_360_20_b', 360)),
     ('360 dp · letra 2,0 · Tú y Contacto (emulador)', '«Quién hace Nubilus» pasa a dos renglones («Quién hace» / «Nubilus»); «Escribir a Nubilus» cabe en una línea', shot('emu_tu_360_20', 360)),
     ('360 dp · letra 2,0 · Acerca de Nubilus', 'el título de «Acerca de Nubilus» cabe en una línea dentro de su losa (emulador)', shot('emu_acerca_360_20', 360))],
]


def main():
    d0 = ImageDraw.Draw(Image.new('RGB', (10, 10)))
    sf = font(15, bold=False)
    sheet_w = MARGIN * 2 + 4 * COLW + 3 * GAP
    heights = []
    for row in ROWS:
        shots = max(im.height for _, _, im in row)
        cap = max(len(wrap(d0, t, sf, COLW)) * 20 for _, t, _ in row)
        heights.append(26 + shots + 12 + cap + 34)
    sheet = Image.new('RGB', (sheet_w, MARGIN + sum(heights)), NAVY)
    d = ImageDraw.Draw(sheet)
    y = MARGIN
    for row, hrow in zip(ROWS, heights):
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
