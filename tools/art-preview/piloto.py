"""Maqueta de Piloto Estelar (disposición aproximada; arte exacto: nave, señales y marcas salen de los .raw que
vuelca ArtPreview; la ruta usa la misma fórmula que PilotContract.CenterAt).

Uso: python3 tools/art-preview/piloto.py <raw> [--out docs/previews]  ->  piloto-estelar.png
"""
import argparse
import math
import os

from PIL import Image, ImageDraw, ImageFont

from juegos import FB, INK, W, H, ROOT, load, night, put, glow, clay_text, hexc

SKY, CORAL, SUN, LIME, SURFACE = hexc(0x4CC9F0), hexc(0xFF6B4A), hexc(0xFFC93C), hexc(0x9BE564), hexc(0x1B2466)
S = 0.5  # medio canvas


def center_at(d, amp, half):
    wave = 0.62 * math.sin(d * 2.1) + 0.38 * math.sin(d * 3.7 + 1.3)
    return max(half + 0.04, min(1 - half - 0.04, 0.5 + amp * wave))


def rrect(d, box, r, fill, outline=None, width=0):
    d.rounded_rectangle(box, r, fill=fill, outline=outline, width=width)


def clay_box(im, box, r, fill):
    d = ImageDraw.Draw(im)
    x0, y0, x1, y1 = box
    rrect(d, (x0, y0 + 5, x1, y1 + 5), r, INK + (255,))
    rrect(d, box, r, fill + (255,), INK + (255,), 3)


def hud(im, d, level, points, streak):
    d.text((30, 38), 'Piloto Estelar', font=ImageFont.truetype(FB, 34), fill=(255, 255, 255), anchor='lm', stroke_width=2, stroke_fill=INK)
    f = ImageFont.truetype(FB, 20)
    clay_box(im, (30, 62, 132, 90), 14, SURFACE)
    d.text((81, 76), f'Nivel {level}', font=f, fill=SKY, anchor='mm')
    clay_box(im, (142, 62, 262, 90), 14, SURFACE)
    d.text((202, 76), f'{points:,} pts'.replace(',', '.'), font=f, fill=SUN, anchor='mm')
    clay_text(d, (482, 58), str(streak), 40, SUN if streak >= 3 else (255, 255, 255))
    d.text((482, 86), 'racha', font=ImageFont.truetype(FB, 15), fill=(200, 205, 240), anchor='mm')


def banner(im, raw, mission):
    d = ImageDraw.Draw(im)
    clay_box(im, (30, 104, W - 30, 176), 26, SURFACE)
    glow(im, 72, 140, 34, (255, 255, 255), 30)
    put(im, load(f'{raw}/{mission}.raw'), 72, 140, 64)
    d = ImageDraw.Draw(im)
    d.text((118, 128), 'MISIÓN: atrapa solo esta', font=ImageFont.truetype(FB, 23), fill=(255, 255, 255), anchor='lm', stroke_width=1, stroke_fill=INK)
    d.text((118, 156), 'Misma forma y mismo color. Ignora las demás.', font=ImageFont.truetype(FB, 15), fill=(215, 220, 245), anchor='lm')
    return d


def lane(im, traveled, ship_y, amp, half, out_of_lane):
    top, bottom = 200, H
    layer = Image.new('RGBA', im.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    spacing = 23
    dd = traveled - (bottom - ship_y) - 40
    while True:
        dd += spacing
        y = ship_y - (dd - traveled)
        if y < top - 20:
            break
        if y > bottom + 20:
            continue
        c = center_at(dd * 2 / 1900, amp, half)
        depth = (bottom - y) / (bottom - top)
        scale = 1.25 + (0.6 - 1.25) * depth
        alpha = int(255 * (1 + (0.45 - 1) * depth))
        near = abs(y - ship_y) < 80
        col = CORAL if (near and out_of_lane) else SKY
        lx, rx = (c - half) * W, (c + half) * W
        d.rectangle((lx, y - spacing / 2, rx, y + spacing / 2), fill=SKY + (int(14 * alpha / 255),))
        big = round(dd / spacing) % 3 == 0
        r = (13 if big else 8) * scale * S * 1.0
        for x in (lx, rx):
            d.ellipse((x - r, y - r, x + r, y + r), fill=col + (alpha,))
    im.alpha_composite(layer)
    return center_at(traveled * 2 / 1900, amp, half)


def signal(im, raw, name, cx, cy, left=0.6, mark=None, faded=False):
    size = 80
    glow(im, cx, cy, size * 0.8, (255, 255, 255), 40)
    d = ImageDraw.Draw(im)
    r = size * 0.59
    d.arc((cx - r, cy - r, cx + r, cy + r), -90, -90 + 360 * left, fill=(255, 255, 255, 200), width=6)
    icon = load(f'{raw}/{name}.raw')
    if faded:
        icon.putalpha(icon.getchannel('A').point(lambda v: v * 55 // 100))
    put(im, icon, cx, cy, size)
    if mark:
        put(im, load(f'{raw}/{mark}.raw'), cx + size * 0.34, cy - size * 0.34, size * 0.52)


def ship(im, raw, x, y, tilt):
    glow(im, x, y, 90, SKY, 70)
    for k in range(6):
        glow(im, x, y + 40 + k * 16, 16 - k * 2, CORAL, 120 - k * 18)
    s = load(f'{raw}/ship_0.raw').resize((88, 88), Image.LANCZOS).rotate(tilt, resample=Image.BICUBIC, expand=True)
    im.alpha_composite(s, (int(x - s.width / 2), int(y - s.height / 2)))


def control(im, d, text, pill, pill_col):
    band = Image.new('RGBA', im.size, (0, 0, 0, 0))
    rrect(ImageDraw.Draw(band), (15, 778, W - 15, 951), 32, (255, 255, 255, 16))
    im.alpha_composite(band)
    d = ImageDraw.Draw(im)
    d.text((W / 2, 880), text, font=ImageFont.truetype(FB, 20), fill=(255, 255, 255, 180), anchor='mm')
    f = ImageFont.truetype(FB, 17)
    w = d.textlength(pill, font=f) + 44
    clay_box(im, (W / 2 - w / 2, 800, W / 2 + w / 2, 834), 17, (0x16, 0x1D, 0x52))
    d = ImageDraw.Draw(im)
    d.ellipse((W / 2 - w / 2 + 12, 812, W / 2 - w / 2 + 22, 822), fill=pill_col)
    d.text((W / 2 + 6, 817), pill, font=f, fill=(255, 255, 255), anchor='mm')


def float_text(d, x, y, text, col):
    clay_text(d, (x, y), text, 28, col)


def frame_a(raw):
    """Nivel 4: a los mandos, en la ruta, una señal atrapada (✓, +140) y un distractor que se deja pasar."""
    im = night(21)
    glow(im, W / 2, 0, 260, SKY, 50)
    glow(im, W / 2, H, 240, CORAL, 35)
    d = ImageDraw.Draw(im)
    hud(im, d, 4, 1240, 5)
    banner(im, raw, 'sym_Comet_0')
    d = ImageDraw.Draw(im)
    d.rounded_rectangle((30, 186, W - 30, 194), 4, fill=(255, 255, 255, 30))
    d.rounded_rectangle((30, 186, 30 + (W - 60) * 0.62, 194), 4, fill=LIME)
    c = lane(im, 3520, 703, 0.16, 0.24, False)
    signal(im, raw, 'sym_Comet_0', 350, 330, left=0.45, mark='mark_check')
    d = ImageDraw.Draw(im)
    float_text(d, 350, 262, '+140', SUN)
    signal(im, raw, 'sym_Planet_1', 170, 470, left=0.7)
    ship(im, raw, c * W + 6, 703, -8)
    d = ImageDraw.Draw(im)
    control(im, d, '‹  Desliza aquí para guiar la nave  ›', 'En la ruta', LIME)
    return im


def frame_b(raw):
    """Nivel 7: señal en la periferia, un parecido (misma forma, otro color) tocado por error y la nave fuera de la ruta."""
    im = night(22)
    glow(im, W / 2, 0, 260, SKY, 50)
    glow(im, 0, 520, 200, CORAL, 90)
    glow(im, W, 520, 200, CORAL, 90)
    d = ImageDraw.Draw(im)
    hud(im, d, 7, 3860, 0)
    banner(im, raw, 'sym_Comet_0')
    d = ImageDraw.Draw(im)
    d.rounded_rectangle((30, 186, W - 30, 194), 4, fill=(255, 255, 255, 30))
    d.rounded_rectangle((30, 186, 30 + (W - 60) * 0.3, 194), 4, fill=SUN)
    c = lane(im, 5210, 703, 0.26, 0.18, True)
    signal(im, raw, 'sym_Comet_1', 300, 400, left=0.3, mark='mark_cross', faded=True)
    signal(im, raw, 'sym_Comet_0', 62, 290, left=0.8)
    signal(im, raw, 'sym_Star_2', 468, 520, left=0.55)
    ship(im, raw, min(W - 60, (c + 0.26) * W), 703, 14)
    d = ImageDraw.Draw(im)
    control(im, d, '', '¡Vuelve a la ruta!', CORAL)
    return im


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('raw')
    ap.add_argument('--out', default=ROOT + '/docs/previews')
    a = ap.parse_args()
    panels = [frame_a(a.raw), frame_b(a.raw)]
    gap = 24
    sheet = Image.new('RGBA', (len(panels) * W + (len(panels) + 1) * gap, H + 2 * gap), (0x02, 0x03, 0x10, 255))
    for i, p in enumerate(panels):
        sheet.alpha_composite(p, (gap + i * (W + gap), gap))
    os.makedirs(a.out, exist_ok=True)
    sheet.convert('RGB').save(os.path.join(a.out, 'piloto-estelar.png'))
    print('OK')


if __name__ == '__main__':
    main()
