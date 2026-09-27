"""Maqueta de Aterrizaje Lunar (disposición según LandingGameController.Layout a medio canvas; arte exacto: módulo
lunar, bandera, superficie y marcas salen de los .raw que vuelca ArtPreview).

Uso: python3 tools/art-preview/aterrizaje.py <raw> [--out docs/previews]  ->  aterrizaje.png
"""
import argparse
import os

from PIL import Image, ImageDraw, ImageFont

from juegos import FB, INK, W, H, ROOT, load, night, put, glow, clay_text, hexc
from piloto import hud

LIME, SUN, CORAL, SKY, CREAM = hexc(0x9BE564), hexc(0xFFC93C), hexc(0xFF6B4A), hexc(0x4CC9F0), hexc(0xFFF8EC)
RULER_Y, RULER_L, RULER_R = 710, 45, 495
GROUND = 658
LANDER = 115


def rx(f):
    return RULER_L + (RULER_R - RULER_L) * f


def base(raw, seed, level, points, streak, small, mission, lo, hi, mid=False, prompt='', prompt_col=(255, 255, 255), timer=0.7):
    im = night(seed)
    d = ImageDraw.Draw(im)
    d.ellipse((W - 70, 385, W + 100, 555), fill=hexc(0xB8A4FF), outline=INK, width=4)
    surf = load(f'{raw}/surface.raw').resize((W, int(H * 0.28)), Image.LANCZOS)
    im.alpha_composite(surf, (0, H - surf.height))
    d = ImageDraw.Draw(im)
    hud(im, d, level, points, streak, 'Aterrizaje Lunar')
    d = ImageDraw.Draw(im)
    d.rounded_rectangle((30, 98, W - 30, 106), 4, fill=(255, 255, 255, 30))
    d.rounded_rectangle((30, 98, 30 + (W - 60) * timer, 106), 4, fill=LIME if timer > 0.5 else SUN)
    d.text((W / 2, 130), small, font=ImageFont.truetype(FB, 22), fill=(255, 255, 255, 205), anchor='mm')
    clay_text(d, (W / 2, 190), mission, 72, SUN)
    if prompt:
        clay_text(d, (W / 2, 262), prompt, 30, prompt_col)
    # Regla.
    d.rounded_rectangle((RULER_L, RULER_Y - 7 + 4, RULER_R, RULER_Y + 7 + 4), 6, fill=INK)
    d.rounded_rectangle((RULER_L, RULER_Y - 7, RULER_R, RULER_Y + 7), 6, fill=CREAM, outline=INK, width=2)
    for x in (RULER_L, RULER_R):
        d.rounded_rectangle((x - 3, RULER_Y - 19, x + 3, RULER_Y + 19), 3, fill=INK)
    if mid:
        d.rounded_rectangle((W / 2 - 2, RULER_Y - 12, W / 2 + 2, RULER_Y + 12), 2, fill=INK + (180,))
    clay_text(d, (RULER_L, RULER_Y + 40), lo, 28)
    clay_text(d, (RULER_R, RULER_Y + 40), hi, 28)
    return im


def lander(im, raw, x, y, flame=0.0, beam=False):
    if beam:
        layer = Image.new('RGBA', im.size, (0, 0, 0, 0))
        dd = ImageDraw.Draw(layer)
        top, bottom = y + 25, RULER_Y - 8
        for i in range(24):
            k = (i + 0.5) / 24
            yy = top + (bottom - top) * k
            dd.ellipse((x - 3, yy - 3, x + 3, yy + 3), fill=LIME + (int(255 * (0.25 + 0.5 * k)),))
        im.alpha_composite(layer)
    if flame > 0:
        glow(im, x, y + 45, 28 * flame, SUN, int(230 * flame))
    put(im, load(f'{raw}/land_lander.raw'), x, y, LANDER)


def flag(im, raw, x, label):
    f = load(f'{raw}/land_flag.raw').resize((95, 95), Image.LANCZOS)
    # Pie del asta en (x, regla): el asta está al 26% del ancho y su pie al 5% del alto.
    im.alpha_composite(f, (int(x - 95 * 0.262), int(RULER_Y - 95 * (1 - 0.048))))
    d = ImageDraw.Draw(im)
    clay_text(d, (x, RULER_Y - 110), label, 29, SUN)


def gap(im, x1, x2, col, label):
    d = ImageDraw.Draw(im)
    l, r = min(x1, x2), max(x1, x2)
    d.rounded_rectangle((l, RULER_Y - 26, r, RULER_Y - 20), 3, fill=col)
    clay_text(d, ((l + r) / 2, RULER_Y + 72), label, 22, col)


def frame_fly(raw):
    """Nivel 3: "Aterriza en 37" en una regla de 0 a 100 con la marca del medio; el haz marca dónde se posará."""
    im = base(raw, 61, 3, 820, 2, 'Aterriza en', '37', '0', '100', mid=True)
    lander(im, raw, rx(0.43), 470, flame=0.5, beam=True)
    d = ImageDraw.Draw(im)
    d.text((W / 2, 845), 'Arrastra para mover la nave · suelta para aterrizar', font=ImageFont.truetype(FB, 17), fill=(255, 255, 255, 220), anchor='mm')
    return im


def frame_fraction(raw):
    """Nivel 7: fracciones en una regla de 0 a 1, cerca del suelo (retrocohetes a fondo)."""
    im = base(raw, 62, 7, 2140, 4, 'Aterriza en', '3/4', '0', '1')
    lander(im, raw, rx(0.71), GROUND - 60, flame=1.0, beam=True)
    return im


def frame_hit(raw):
    """Aterrizó en 40, el blanco era 37: bandera en su lugar, tramo lima "a 3" y ✓."""
    im = base(raw, 63, 3, 940, 3, 'Aterriza en', '37', '0', '100', mid=True, prompt='¡Buen aterrizaje!', prompt_col=LIME)
    gap(im, rx(0.37), rx(0.40), LIME, 'a 3')
    lander(im, raw, rx(0.40), GROUND)
    flag(im, raw, rx(0.37), '37')
    put(im, load(f'{raw}/mark_check.raw'), rx(0.40) + 42, GROUND - 42, 40)
    d = ImageDraw.Draw(im)
    clay_text(d, (rx(0.40), GROUND - 95), '+103', 26, SUN)
    return im


def frame_bull(raw):
    """Nivel 10: resolver "250 + 130" y aterrizar en 380 de 1000: ¡diana lunar!"""
    im = base(raw, 64, 10, 4210, 6, 'Aterriza en', '250 + 130', '0', '1000', prompt='¡DIANA LUNAR! · racha 6', prompt_col=SUN)
    glow(im, rx(0.38), RULER_Y - 40, 90, SUN, 150)
    layer = Image.new('RGBA', im.size, (0, 0, 0, 0))
    ImageDraw.Draw(layer).ellipse((rx(0.38) - 95, RULER_Y - 130, rx(0.38) + 95, RULER_Y + 60), outline=SUN + (220,), width=5)
    im.alpha_composite(layer)
    lander(im, raw, rx(0.38), GROUND)
    flag(im, raw, rx(0.38), '380')
    spark = load(f'{raw}/sym_Star_0.raw')
    for dx, dy, s in [(-80, -120, 26), (70, -140, 22), (-120, -40, 18), (110, -60, 20), (0, -170, 24)]:
        put(im, spark, rx(0.38) + dx, RULER_Y + dy, s)
    d = ImageDraw.Draw(im)
    clay_text(d, (rx(0.38), GROUND - 150), '+310', 28, SUN)
    return im


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('raw')
    ap.add_argument('--out', default=ROOT + '/docs/previews')
    a = ap.parse_args()
    panels = [frame_fly(a.raw), frame_fraction(a.raw), frame_hit(a.raw), frame_bull(a.raw)]
    gap_ = 24
    sheet = Image.new('RGBA', (len(panels) * W + (len(panels) + 1) * gap_, H + 2 * gap_), (0x02, 0x03, 0x10, 255))
    for i, p in enumerate(panels):
        sheet.alpha_composite(p, (gap_ + i * (W + gap_), gap_))
    os.makedirs(a.out, exist_ok=True)
    sheet.convert('RGB').save(os.path.join(a.out, 'aterrizaje.png'))
    print('OK')


if __name__ == '__main__':
    main()
