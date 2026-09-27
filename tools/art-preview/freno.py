"""Maqueta de Freno de Emergencia (disposición según BrakeGameController.Layout/SetupLanes a medio canvas; arte exacto:
señal de alto, plataformas, botones, cohetes, superficie lunar y marcas salen de los .raw que vuelca ArtPreview).

Uso: python3 tools/art-preview/freno.py <raw> [--out docs/previews]  ->  freno.png
"""
import argparse
import os
import random

from PIL import Image, ImageDraw, ImageFont

from juegos import FB, W, H, ROOT, load, night, put, glow, clay_text, hexc, tinted
from piloto import hud

LIME, SUN, CORAL, SKY = hexc(0x9BE564), hexc(0xFFC93C), hexc(0xFF6B4A), hexc(0x4CC9F0)
LANES = [94, 270, 446]
BUTTON_Y, PAD_Y, ROCKET_Y, BEACON_Y = 878, 780, 774, 703


def base(raw, seed, points, streak, gauge_ms, best_ms, prompt='', prompt_col=(255, 255, 255), stars=6, lit=None, beacons=None, hidden=None):
    im = night(seed)
    surf = load(f'{raw}/surface.raw')
    surf = surf.resize((W, int(H * 0.22)), Image.LANCZOS)
    im.alpha_composite(surf, (0, H - surf.height))
    d = ImageDraw.Draw(im)
    hud(im, d, 3, points, streak, 'Freno de Emergencia')
    d = ImageDraw.Draw(im)
    d.rounded_rectangle((30, 98, W - 30, 106), 4, fill=(255, 255, 255, 30))
    d.rounded_rectangle((30, 98, 30 + (W - 60) * 0.62, 106), 4, fill=LIME)
    # Medidor "Límite del freno".
    d.text((30, 125), 'Límite del freno', font=ImageFont.truetype(FB, 18), fill=(255, 255, 255, 205), anchor='lm')
    clay_text(d, (W - 60, 125), f'{gauge_ms} ms', 20, SKY)
    d.rounded_rectangle((30, 145, W - 30, 155), 5, fill=(255, 255, 255, 30))
    d.rounded_rectangle((30, 145, 30 + (W - 60) * gauge_ms / 900, 155), 5, fill=SKY)
    if best_ms:
        x = 30 + (W - 60) * best_ms / 900
        d.rounded_rectangle((x - 3, 140, x + 3, 160), 3, fill=SUN, outline=(0x1A, 0x12, 0x40), width=1)
    # Estrellas de los lanzamientos anteriores.
    rng = random.Random(seed)
    spark = load(f'{raw}/sym_Star_0.raw')
    for _ in range(stars):
        put(im, spark, rng.uniform(40, W - 40), rng.uniform(200, 610), rng.uniform(15, 26))
    if prompt:
        clay_text(d, (W / 2, 470), prompt, 33, prompt_col)
    for i, x in enumerate(LANES):
        put(im, load(f'{raw}/brake_pad.raw'), x, PAD_Y, 150)
        if lit == i:
            glow(im, x, ROCKET_Y, 115, LIME, 150)
        if hidden != i:
            r = load(f'{raw}/sym_Rocket_{i % 3}.raw').resize((115, 115), Image.LANCZOS).rotate(45, resample=Image.BICUBIC)
            im.alpha_composite(r, (int(x - r.width / 2), int(ROCKET_Y - r.height / 2)))
        col = (beacons or {}).get(i, (255, 255, 255, 46))
        d = ImageDraw.Draw(im)
        d.ellipse((x - 11.5, BEACON_Y - 11.5, x + 11.5, BEACON_Y + 11.5), fill=col)
        put(im, load(f'{raw}/brake_button{"_lit" if lit == i else ""}.raw'), x, BUTTON_Y, 105)
    return im


def frame_go(raw):
    """Se enciende el cohete del medio: hay que lanzarlo."""
    return base(raw, 51, 1240, 4, 300, 350, '', stars=7, lit=1, beacons={1: LIME + (255,)})


def frame_stop(raw):
    """Un instante después: ¡ALTO! (no tocar)."""
    im = base(raw, 52, 1240, 4, 300, 350, stars=7, lit=1, beacons={0: CORAL + (255,), 1: CORAL + (255,), 2: CORAL + (255,)})
    glow(im, W / 2, 370, 170, CORAL, 90)
    put(im, load(f'{raw}/brake_stop.raw'), W / 2, 370, 200)
    d = ImageDraw.Draw(im)
    clay_text(d, (W / 2, 364), 'ALTO', 56)
    return im


def frame_launch(raw):
    """Despegue: el cohete sale con llama y humo; suma puntos y una estrella."""
    x = LANES[2]
    # La plataforma queda vacía hasta que llega el cohete nuevo.
    im = base(raw, 53, 1360, 5, 300, 350, '¡Despegue relámpago!', LIME, stars=8, hidden=2)
    for k, (dx, s, a) in enumerate([(-40, 34, 110), (40, 34, 110), (-72, 46, 80), (72, 46, 80), (-100, 55, 50), (100, 55, 50)]):
        o = Image.new('RGBA', im.size, (0, 0, 0, 0))
        ImageDraw.Draw(o).ellipse((x + dx - s / 2, 800 - s / 2, x + dx + s / 2, 800 + s / 2), fill=(255, 255, 255, a))
        im.alpha_composite(o)
    glow(im, x, 560, 60, SUN, 220)
    glow(im, x, 600, 40, CORAL, 180)
    r = load(f'{raw}/sym_Rocket_2.raw').resize((115, 115), Image.LANCZOS).rotate(45, resample=Image.BICUBIC)
    im.alpha_composite(r, (int(x - r.width / 2), int(500 - r.height / 2)))
    d = ImageDraw.Draw(im)
    clay_text(d, (x, 400), '+118', 29, SUN)
    return im


def frame_brake(raw):
    """¡Freno perfecto!: vapor de los frenos, ✓, más puntos y el medidor sube (nuevo récord)."""
    im = base(raw, 54, 1590, 6, 400, 400, '¡FRENO PERFECTO!', LIME, stars=8, beacons={0: CORAL + (255,), 1: CORAL + (255,), 2: CORAL + (255,)})
    x = LANES[0]
    for dx, s, a in [(-44, 36, 120), (44, 36, 120), (-80, 50, 80), (80, 50, 80)]:
        o = Image.new('RGBA', im.size, (0, 0, 0, 0))
        ImageDraw.Draw(o).ellipse((x + dx - s / 2, 790 - s / 2, x + dx + s / 2, 790 + s / 2), fill=(255, 255, 255, a))
        im.alpha_composite(o)
    put(im, load(f'{raw}/mark_check.raw'), x + 45, ROCKET_Y - 45, 50)
    layer = Image.new('RGBA', im.size, (0, 0, 0, 0))
    ImageDraw.Draw(layer).ellipse((W / 2 - 150, 220, W / 2 + 150, 520), outline=LIME + (200,), width=6)
    im.alpha_composite(layer)
    put(im, load(f'{raw}/brake_stop.raw'), W / 2, 370, 200)
    d = ImageDraw.Draw(im)
    clay_text(d, (W / 2, 364), 'ALTO', 56)
    clay_text(d, (W / 2, 240), '+230', 29, SUN)
    # Aviso de nuevo récord (toast).
    d.rounded_rectangle((W / 2 - 170, 170, W / 2 + 170, 214), 22, fill=(0x1B, 0x24, 0x66, 255), outline=SUN, width=3)
    d.text((W / 2, 192), '¡Nuevo límite! 400 ms', font=ImageFont.truetype(FB, 19), fill=(255, 255, 255), anchor='mm')
    return im


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('raw')
    ap.add_argument('--out', default=ROOT + '/docs/previews')
    a = ap.parse_args()
    panels = [frame_go(a.raw), frame_stop(a.raw), frame_launch(a.raw), frame_brake(a.raw)]
    gap = 24
    sheet = Image.new('RGBA', (len(panels) * W + (len(panels) + 1) * gap, H + 2 * gap), (0x02, 0x03, 0x10, 255))
    for i, p in enumerate(panels):
        sheet.alpha_composite(p, (gap + i * (W + gap), gap))
    os.makedirs(a.out, exist_ok=True)
    sheet.convert('RGB').save(os.path.join(a.out, 'freno.png'))
    print('OK')


if __name__ == '__main__':
    main()
