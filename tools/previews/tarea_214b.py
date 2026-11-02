# -*- coding: utf-8 -*-
"""Lámina de la Tarea 214b (2-nov-2026, carril rápido): la frase de Bodega de carga, «Recuerda dónde guardó cada cosa el robot» → «Recuerda dónde guardó cada cosa» (decisión de Ricardo), para que la ficha lleve el nombre y UNA línea a 360 dp.
La ficha de Bodega de carga «antes | ahora» a 360 dp con letra 1,0, A TAMAÑO REAL (1 dp = 1 px) y del EMULADOR (`medium_phone`, API 36, wm density 480 = 360 dp). «antes» = el APK de la Tarea 214; «ahora» = el de esta tarea.
Las capturas salen de `emu214.py` (escena `ficha_bodega`, que maneja SOLO el emulador) y van a `app/src/test/screenshots/t214/` (fuera de git).
Uso:  python -I tools/previews/tarea_214b.py      → docs/previews/tarea-214b.png
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SHOTS = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots', 't214')
RES = os.path.join(ROOT, 'app', 'src', 'main', 'res')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-214b.png')
FONTS = os.path.join(RES, 'font')
NAVY, SUN, DIM, LINE = (20, 18, 63), (255, 230, 166), (200, 196, 235), (90, 86, 160)
GAP, MARGIN, DP = 30, 24, 360

COLS = [('antes_ficha_bodega_360_10', 'antes (T214) · Bodega de carga · 360 dp · letra 1,0', '«Recuerda dónde guardó cada cosa el robot»: unos 321 dp contra 296, pasa a dos renglones'),
        ('ahora_ficha_bodega_360_10', 'ahora (T214b) · Bodega de carga · 360 dp · letra 1,0', '«Recuerda dónde guardó cada cosa»: 259 dp de 296, una línea (el nombre y UNA línea, como los otros 16 juegos)')]


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
        im = Image.new('RGB', (DP, 800), (40, 36, 100)); ImageDraw.Draw(im).text((20, 400), 'falta ' + key, font=font(20), fill=SUN); return im
    im = Image.open(path).convert('RGB')
    return im.resize((DP, int(round(im.height * DP / im.width))), Image.LANCZOS)          # tamaño real: 1 dp = 1 px


def main():
    d0 = ImageDraw.Draw(Image.new('RGB', (10, 10)))
    sf = font(15, bold=False)
    shots = [load(k) for k, _, _ in COLS]
    cap_h = max(len(wrap(d0, t, sf, DP)) * 20 for _, _, t in COLS)
    width = MARGIN * 2 + len(COLS) * DP + (len(COLS) - 1) * GAP
    sheet = Image.new('RGB', (width, MARGIN + 60 + 26 + max(s.height for s in shots) + 12 + cap_h + 34 + MARGIN), NAVY)
    d = ImageDraw.Draw(sheet)
    d.text((MARGIN, MARGIN), 'Tarea 214b · antes | ahora · Bodega de carga', font=font(24), fill=SUN)
    x, y = MARGIN, MARGIN + 60
    for (key, title, text), im in zip(COLS, shots):
        size = 17
        while size > 11 and d.textlength(title, font=font(size)) > DP: size -= 1
        d.text((x, y), title, font=font(size), fill=SUN)
        sheet.paste(im, (x, y + 26))
        d.rectangle((x - 1, y + 25, x + im.width, y + 26 + im.height), outline=LINE)
        for i, line in enumerate(wrap(d, text, sf, DP)):
            d.text((x, y + 26 + im.height + 12 + i * 20), line, font=sf, fill=DIM)
        x += DP + GAP
    sheet.save(OUT)
    print(OUT, sheet.size)


main()
