"""Maqueta de Primer Contacto (disposición según ContactGameController.Layout, a medio canvas). Arte exacto de los .raw
que vuelca ArtPreview (el nuri, los íconos del cielo en los colores de la paleta, la punta del globo, las marcas); la
escena del medio es REAL: la arma ContactContract.MakeScene (contact_scene.txt).

Tres momentos: la primera lección (una palabra, tres cosas, el diccionario con "?"), una lección de colores (frase de dos
palabras; la que suena va en coral) y una palabra descifrada (vuela al diccionario).

Uso: python3 tools/art-preview/contacto.py <raw> [--out docs/previews]  ->  contacto.png
"""
import argparse
import os

from PIL import Image, ImageDraw, ImageFont

from juegos import INK, W, H, ROOT, FB, load, put, glow, night, hexc
from piloto import clay_box

K = 0.5
SUN, LIME, CORAL, SKY, GRAPE, CREAM = (hexc(0xFFC93C), hexc(0x9BE564), hexc(0xFF6B4A), hexc(0x4CC9F0), hexc(0xB8A4FF),
                                       (255, 248, 236))
PALETTE = [CORAL, SUN, SKY, GRAPE, LIME]
DEEP = (0x1B, 0x24, 0x66)
WORDS = ["ZOBA", "KITU", "FEDI", "NUPA", "LIRO", "GAMU", "TEBI", "SUKE", "MOVA", "DAKO", "PIFO", "NELI",
         "RA", "KI", "LU", "ZO", "PE"]
MEANING = {12: 'coral', 13: 'amarillo', 14: 'celeste'}

# Disposición (unidades del canvas 1080 x 1920, centro = 0,0), igual que Layout().
ALIEN_Y, BUBBLE_Y = 635, 420
DICT_Y, CAPTION_Y, THINGS_Y = -750, -470, -32
THING, SLOT = 210, 104


def font(size):
    return ImageFont.truetype(FB, size)


def sx(x):
    return W / 2 + x * K


def sy(y):
    return H / 2 - y * K


def background(seed):
    im = night(seed)
    glow(im, W * 0.5, H * 0.2, 260, LIME, 34)
    glow(im, W * 0.15, H * 0.8, 240, GRAPE, 44)
    # Planeta uva asomando a la derecha y una luna lila arriba a la derecha (como GameWorld.FirstContact).
    d = ImageDraw.Draw(im)
    cx, cy, r = W * 1.1, H * 0.5, 85
    glow(im, cx, cy, r * 1.6, GRAPE, 40)
    d = ImageDraw.Draw(im)
    d.ellipse((cx - r, cy - r, cx + r, cy + r), fill=GRAPE + (255,), outline=INK + (255,), width=4)
    mx, my, mr = W * 0.88, H * 0.1, 22
    glow(im, mx, my, mr * 3, (0xE4, 0xDC, 0xFF), 60)
    d = ImageDraw.Draw(im)
    d.ellipse((mx - mr, my - mr, mx + mr, my + mr), fill=(0xE4, 0xDC, 0xFF, 255))
    return im


def hud(im, level, info):
    d = ImageDraw.Draw(im)
    d.text((30, 38), 'Primer Contacto', font=font(34), fill=(255, 255, 255), anchor='lm', stroke_width=2, stroke_fill=INK)
    clay_box(im, (30, 62, 132, 90), 14, DEEP)
    d = ImageDraw.Draw(im)
    d.text((81, 76), f'Nivel {level}', font=font(20), fill=SKY, anchor='mm')
    wbox = 60 + 11 * len(info)
    clay_box(im, (142, 62, 142 + wbox, 90), 14, DEEP)
    d = ImageDraw.Draw(im)
    d.text((142 + wbox / 2, 76), info, font=font(20), fill=SUN, anchor='mm')


def toast(im, title, sub, accent):
    clay_box(im, (60, 108, W - 60, 176), 22, DEEP)
    d = ImageDraw.Draw(im)
    d.rounded_rectangle((60, 108, 72, 176), 6, fill=accent + (255,))
    d.text((W / 2, 130), title, font=font(24), fill=accent, anchor='mm')
    d.text((W / 2, 157), sub, font=font(17), fill=(230, 232, 250), anchor='mm')


def alien(im, raw, mouth_open):
    glow(im, sx(0), sy(ALIEN_Y), 130, LIME, 50)
    put(im, load(f'{raw}/contact_alien_{"open" if mouth_open else "closed"}.raw'), sx(0), sy(ALIEN_Y), 250 * K)


def bubble(im, raw, words, current):
    d = ImageDraw.Draw(im)
    f = font(38)
    parts = []
    for i, w in enumerate(words):
        parts.append((w, CORAL if i == current else INK))
    text_w = sum(d.textlength(w, font=f) for w, _ in parts) + d.textlength('  ', font=f) * (len(parts) - 1)
    bw = max(130, text_w + 45)
    x0, x1 = W / 2 - bw / 2, W / 2 + bw / 2
    y0, y1 = sy(BUBBLE_Y) - 32, sy(BUBBLE_Y) + 32
    d.rounded_rectangle((x0 - 3, y0 + 2, x1 + 3, y1 + 8), 28, fill=INK + (255,))
    d.rounded_rectangle((x0 - 3, y0 - 3, x1 + 3, y1 + 3), 28, fill=INK + (255,))
    put(im, load(f'{raw}/contact_tail.raw'), W / 2, y0 - 8, 32)
    d = ImageDraw.Draw(im)
    d.rounded_rectangle((x0, y0, x1, y1), 26, fill=CREAM + (255,))
    x = W / 2 - text_w / 2
    for i, (w, col) in enumerate(parts):
        d.text((x, sy(BUBBLE_Y) + 1), w, font=f, fill=col + (255,), anchor='lm')
        x += d.textlength(w + '  ', font=f)


def thing_sprite(raw, obj, color):
    if obj >= 12:
        return load(f'{raw}/bit_find_{obj - 12}.raw')
    if color < 0:
        return load(f'{raw}/sym_{SHAPES[obj]}_0.raw')
    return load(f'{raw}/contact_obj_{obj}_{color}.raw')


SHAPES = ['Planet', 'Rocket', 'Comet', 'Star', 'Moon', 'Ufo', 'Satellite', 'Sun', 'Helmet', 'Asteroid', 'Telescope', 'Crystal']


def thing_positions(n):
    gap = 270
    if n <= 3:
        return [((i - (n - 1) / 2) * gap, THINGS_Y) for i in range(n)]
    first = 2 if n == 4 else 3
    out = []
    for i in range(n):
        row = 0 if i < first else 1
        col = i if row == 0 else i - first
        in_row = first if row == 0 else n - first
        out.append(((col - (in_row - 1) / 2) * gap, THINGS_Y + (125 if row == 0 else -125)))
    return out


def draw_thing(im, raw, x, y, obj, color, count, chosen=False, dim=False, mark=None):
    cx, cy = sx(x), sy(y)
    glow(im, cx, cy + 0.4 * THING * K, 60, SKY, 50 if not dim else 18)
    d = ImageDraw.Draw(im)
    bw, bh = 0.9 * THING * K / 2, 0.26 * THING * K / 2
    by = cy + 0.4 * THING * K
    d.ellipse((cx - bw, by - bh, cx + bw, by + bh), outline=SKY + (210,), width=4)
    spr = thing_sprite(raw, obj, color)
    if dim:
        a = spr.split()[3].point(lambda v: int(v * 0.45))
        spr.putalpha(a)
    if count == 1:
        put(im, spr, cx, cy - 14 * K, 170 * K)
    elif count == 2:
        for dx in (-52, 52):
            put(im, spr, cx + dx * K, cy - 10 * K, 120 * K)
    else:
        for dx, dy in ((-54, -6), (54, -6), (0, 62)):
            put(im, spr, cx + dx * K, cy - dy * K, 104 * K)
    if chosen:
        d = ImageDraw.Draw(im)
        r = 1.08 * THING * K / 2
        d.ellipse((cx - r, cy - 8 * K - r, cx + r, cy - 8 * K + r), outline=SUN + (255,), width=5)
    if mark:
        put(im, load(f'{raw}/mark_{mark}.raw'), cx + 0.4 * THING * K, cy - 0.4 * THING * K, 66 * K)


def dictionary(im, raw, slots):
    """slots: lista de (palabra, estado, icono) con estado '?', 'lit' o 'ok'; icono = ('obj', obj) o ('color', c)."""
    d = ImageDraw.Draw(im)
    n = len(slots)
    gap = SLOT + 34
    d.text((W / 2, sy(DICT_Y + 97)), 'diccionario nuri', font=font(15), fill=(255, 248, 236, 180), anchor='mm')
    for i, (word, state, icon) in enumerate(slots):
        x = (i - (n - 1) / 2) * gap
        cx, cy = sx(x), sy(DICT_Y - 10)
        r = SLOT * K / 2
        d = ImageDraw.Draw(im)
        d.ellipse((cx - r, cy - r, cx + r, cy + r), fill=DEEP + (255,))
        ring = {'?': (255, 248, 236, 80), 'lit': SUN + (255,), 'ok': LIME + (255,)}[state]
        d.ellipse((cx - r, cy - r, cx + r, cy + r), outline=ring, width=3)
        if state == 'ok' and icon:
            if icon[0] == 'color':
                rr = r * 0.62
                d.ellipse((cx - rr, cy - rr, cx + rr, cy + rr), fill=PALETTE[icon[1]] + (255,))
            else:
                put(im, thing_sprite(raw, icon[1], -1), cx, cy, SLOT * K * 0.78)
        else:
            d.text((cx, cy), '?', font=font(28), fill=(255, 248, 236, 140), anchor='mm')
        d = ImageDraw.Draw(im)
        d.text((cx, cy + r + 12), word, font=font(16), fill=CREAM + (255,), anchor='mm')


def caption(im, main, sub):
    d = ImageDraw.Draw(im)
    if main:
        d.text((W / 2, sy(CAPTION_Y + 26)), main, font=font(23), fill=(255, 255, 255), anchor='mm', stroke_width=2, stroke_fill=INK)
    if sub:
        d.text((W / 2, sy(CAPTION_Y - 34)), sub, font=font(17), fill=(255, 248, 236, 205), anchor='mm')


def frame_first(raw):
    im = background(3)
    hud(im, 2, 'Descifradas 0 de 3')
    alien(im, raw, True)
    bubble(im, raw, ['KITU'], 0)
    for (x, y), obj in zip(thing_positions(3), [4, 1, 13]):
        draw_thing(im, raw, x, y, obj, -1, 1)
    caption(im, '', 'Toca lo que crees que nombró')
    dictionary(im, raw, [('ZOBA', '?', None), ('KITU', 'lit', None), ('FEDI', '?', None)])
    return im


def load_scene(raw):
    things, slots, phrase, target = [], [], [], 0
    for line in open(f'{raw}/contact_scene.txt'):
        t = line.split()
        if t[0] == 'PHRASE':
            phrase = t[1:]
        elif t[0] == 'TARGET':
            target = int(t[1])
        elif t[0] == 'THING':
            things.append(tuple(map(int, t[1:])))
        elif t[0] == 'SLOT':
            slots.append((int(t[1]), t[2]))
    return phrase, target, things, slots


def frame_colors(raw):
    phrase, target, things, slots = load_scene(raw)
    im = background(5)
    hud(im, 5, 'Descifradas 1 de 5')
    alien(im, raw, True)
    bubble(im, raw, phrase, 1)
    for (x, y), (obj, color, count) in zip(thing_positions(len(things)), things):
        draw_thing(im, raw, x, y, obj, color, count)
    caption(im, '', 'Toca al nuri para oírlo de nuevo')
    dictionary(im, raw, [(w, 'ok' if wid == 12 else 'lit' if w in phrase else '?', ('color', 0) if wid == 12 else None)
                         for wid, w in slots])
    return im


def frame_decoded(raw):
    phrase, target, things, slots = load_scene(raw)
    im = background(7)
    hud(im, 5, 'Descifradas 2 de 5')
    alien(im, raw, False)
    bubble(im, raw, phrase, -1)
    for i, ((x, y), (obj, color, count)) in enumerate(zip(thing_positions(len(things)), things)):
        draw_thing(im, raw, x, y, obj, color, count, chosen=i == target, dim=i != target, mark='check' if i == target else None)
    toast(im, '¡Descifrada!', 'KI = amarillo', LIME)
    caption(im, 'KI es amarillo', '¡A la primera deducción!')
    dictionary(im, raw, [(w, 'ok' if wid in (12, 13) else '?', ('color', wid - 12) if wid in (12, 13) else None)
                         for wid, w in slots])
    return im


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('raw')
    ap.add_argument('--out', default=ROOT + '/docs/previews')
    a = ap.parse_args()
    panels = [frame_first(a.raw), frame_colors(a.raw), frame_decoded(a.raw)]
    gap = 24
    sheet = Image.new('RGBA', (len(panels) * W + (len(panels) + 1) * gap, H + 2 * gap), (0x02, 0x03, 0x10, 255))
    for i, p in enumerate(panels):
        sheet.alpha_composite(p, (gap + i * (W + gap), gap))
    os.makedirs(a.out, exist_ok=True)
    sheet.convert('RGB').save(os.path.join(a.out, 'contacto.png'))
    print('OK')


if __name__ == '__main__':
    main()
