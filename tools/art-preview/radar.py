"""Maqueta de Radar (disposición según RadarGameController.Layout a medio canvas; arte exacto: radar, haz,
interferencia, botones de dirección, íconos y marcas salen de los .raw que vuelca ArtPreview; las casillas usan
RadarContract.Position).

Uso: python3 tools/art-preview/radar.py <raw> [--out docs/previews]  ->  radar.png
"""
import argparse
import math
import os
import random

from PIL import Image, ImageDraw, ImageFont

from juegos import FB, INK, W, H, ROOT, load, night, put, glow, clay_text, hexc, tinted
from piloto import clay_box, hud

LIME, SUN, CORAL, CREAM = hexc(0x9BE564), hexc(0xFFC93C), hexc(0xFF6B4A), hexc(0xFFF8EC)
RINGS = [0.42, 0.65, 0.87]
SCOPE = 500                 # lado del radar (medio canvas)
CX, CY = W / 2, 166 + SCOPE / 2
GLASS = SCOPE / 2 * 0.86 / 1.06
ITEM = GLASS * 0.25
CW = 0.22 * GLASS


def slot(direction, ring, seed):
    a = direction * math.pi / 4
    r = RINGS[ring]
    j = random.Random(seed)
    jx, jy = (j.random() - 0.5) * 0.05, (j.random() - 0.5) * 0.05
    return CX + (r * math.sin(a) + jx) * GLASS, CY - (r * math.cos(a) + jy) * GLASS


def base(raw, seed, level, points, streak, prompt, prompt_col=(255, 255, 255), timer=0.7, rescued=3):
    im = night(seed)
    glow(im, W / 2, CY, 300, LIME, 22)
    d = ImageDraw.Draw(im)
    hud(im, d, level, points, streak, 'Radar')
    d = ImageDraw.Draw(im)
    d.rounded_rectangle((30, 98, W - 30, 106), 4, fill=(255, 255, 255, 30))
    d.rounded_rectangle((30, 98, 30 + (W - 60) * timer, 106), 4, fill=LIME if timer > 0.5 else SUN)
    clay_text(d, (W / 2, 138), prompt, 30, prompt_col)
    put(im, load(f'{raw}/radar_scope.raw'), CX, CY, SCOPE)
    # Fila de rescatados.
    d = ImageDraw.Draw(im)
    d.text((W / 2, 872), f'Rescatados: {rescued}', font=ImageFont.truetype(FB, 20), fill=(255, 255, 255, 190), anchor='mm')
    total = 10 * 40 + 9 * 6
    helmet = load(f'{raw}/sym_Helmet_1.raw')
    for i in range(10):
        x = W / 2 - total / 2 + 20 + i * 46
        if i < rescued:
            put(im, helmet, x, 916, 40)
        else:
            d.ellipse((x - 10, 906, x + 10, 926), fill=(255, 255, 255, 26))
    return im


def sweep(im, raw, angle):
    s = tinted(load(f'{raw}/radar_sweep.raw'), LIME)
    s.putalpha(s.getchannel('A').point(lambda v: v * 32 // 100))
    s = s.resize((int(GLASS * 2), int(GLASS * 2)), Image.LANCZOS).rotate(angle, resample=Image.BICUBIC)
    im.alpha_composite(s, (int(CX - s.width / 2), int(CY - s.height / 2)))


def stimuli(im, raw, center, helmet_slot, asteroids, trial_seed):
    rng = random.Random(trial_seed)
    for s in asteroids:
        x, y = slot(s % 8, s // 8, s + trial_seed)
        a = load(f'{raw}/sym_Asteroid_{rng.randrange(3)}.raw').resize((int(ITEM * 0.92), int(ITEM * 0.92)), Image.LANCZOS)
        a = a.rotate(rng.randrange(360), resample=Image.BICUBIC)
        im.alpha_composite(a, (int(x - a.width / 2), int(y - a.height / 2)))
    x, y = slot(helmet_slot[0], helmet_slot[1], helmet_slot[1] * 8 + helmet_slot[0] + trial_seed)
    put(im, load(f'{raw}/sym_Helmet_1.raw'), x, y, ITEM)
    put(im, load(f'{raw}/{center}.raw'), CX, CY, CW * 1.75)
    return x, y


def options(im, raw, a, b, chosen=None, ok=None):
    size = 145
    for i, name in enumerate((a, b)):
        x = W / 2 + (-1 if i == 0 else 1) * (size / 2 + 20)
        y = 756
        layer = Image.new('RGBA', im.size, (0, 0, 0, 0))
        clay_box(layer, (x - size / 2, y - size / 2, x + size / 2, y + size / 2), 30, CREAM)
        put(layer, load(f'{raw}/{name}.raw'), x, y, size * 0.76)
        if chosen is not None and i != chosen:
            layer.putalpha(layer.getchannel('A').point(lambda v: v * 35 // 100))
        im.alpha_composite(layer)
        if chosen == i:
            put(im, load(f'{raw}/mark_{"check" if ok else "cross"}.raw'), x + size * 0.4, y - size * 0.4, 55)


def pads(im, raw, only=None, mark=None, wrong=False):
    pad = load(f'{raw}/radar_pad.raw')
    for dct in range(8):
        if only is not None and dct != only:
            continue
        a = dct * math.pi / 4
        x, y = CX + RINGS[2] * math.sin(a) * GLASS, CY - RINGS[2] * math.cos(a) * GLASS
        p = pad.resize((66, 66), Image.LANCZOS).rotate(-45 * dct, resample=Image.BICUBIC)
        if wrong:
            p = tinted(p, (255, 158, 143))
        im.alpha_composite(p, (int(x - p.width / 2), int(y - p.height / 2)))
        if mark:
            put(im, load(f'{raw}/mark_{mark}.raw'), x + 24, y - 24, 36)


def frame_flash(raw):
    """Nivel 7, el destello: cohete en el centro, astronauta en el anillo de afuera, 15 asteroides."""
    im = base(raw, 31, 7, 1480, 4, 'Mira el centro')
    asteroids = [r * 8 + d for r in (1, 2) for d in range(8) if not (r == 2 and d == 3)]
    stimuli(im, raw, 'sym_Rocket_0', (3, 2), asteroids, 5)
    return im


def frame_mask(raw):
    """La interferencia que borra la imagen."""
    im = base(raw, 32, 7, 1480, 4, 'Mira el centro')
    m = load(f'{raw}/radar_mask_0.raw').resize((SCOPE, SCOPE), Image.LANCZOS).rotate(40, resample=Image.BICUBIC)
    im.alpha_composite(m, (int(CX - SCOPE / 2), int(CY - SCOPE / 2)))
    return im


def frame_answer(raw):
    """Paso 1 respondido (cohete ✓), paso 2: tocar la dirección del astronauta."""
    im = base(raw, 33, 7, 1480, 4, '¿Dónde estaba el astronauta?')
    sweep(im, raw, 60)
    pads(im, raw)
    d = ImageDraw.Draw(im)
    options(im, raw, 'sym_Rocket_0', 'sym_Planet_2', chosen=0, ok=True)
    return im


def frame_feedback(raw):
    """¡Rescatado!: la verdad a la vista con marcas ✓, puntos y el astronauta rumbo a la fila."""
    im = base(raw, 34, 7, 1780, 5, '¡Rescatado! · racha 5', LIME, rescued=4)
    sweep(im, raw, 200)
    a = 3 * math.pi / 4
    x, y = CX + RINGS[2] * math.sin(a) * GLASS, CY - RINGS[2] * math.cos(a) * GLASS
    glow(im, x, y, 60, LIME, 140)
    put(im, load(f'{raw}/sym_Helmet_1.raw'), x, y, ITEM)
    pads(im, raw, only=3, mark='check')
    put(im, load(f'{raw}/sym_Rocket_0.raw'), CX, CY, CW * 1.75)
    put(im, load(f'{raw}/mark_check.raw'), CX + CW * 0.85, CY - CW * 0.85, CW * 0.9)
    d = ImageDraw.Draw(im)
    clay_text(d, (x, y - 70), '+240', 28, SUN)
    options(im, raw, 'sym_Rocket_0', 'sym_Planet_2', chosen=0, ok=True)
    return im


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('raw')
    ap.add_argument('--out', default=ROOT + '/docs/previews')
    a = ap.parse_args()
    panels = [frame_flash(a.raw), frame_mask(a.raw), frame_answer(a.raw), frame_feedback(a.raw)]
    gap = 24
    sheet = Image.new('RGBA', (len(panels) * W + (len(panels) + 1) * gap, H + 2 * gap), (0x02, 0x03, 0x10, 255))
    for i, p in enumerate(panels):
        sheet.alpha_composite(p, (gap + i * (W + gap), gap))
    os.makedirs(a.out, exist_ok=True)
    sheet.convert('RGB').save(os.path.join(a.out, 'radar.png'))
    print('OK')


if __name__ == '__main__':
    main()
