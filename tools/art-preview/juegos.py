"""Maquetas de pantalla (aproximadas en la disposición, exactas en el arte) de Ruta del Tesoro, Secuencia,
Comparación y Series, armadas con los .raw que vuelca ArtPreview.

Uso: python3 tools/art-preview/juegos.py <raw> [--out docs/previews]
     ->  arte-juegos.png (Ruta del Tesoro, Secuencia, Comparación, Series)
         arte-juegos-2.png (Tinta o Palabra, Cambio de Chip, Cálculo, Anagramas)
"""
import argparse
import os
import random
import struct

from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
FB = ROOT + '/unity/NeuroVidaCore/Assets/Resources/Fonts/Fredoka-Bold.ttf'
INK = (0x1A, 0x12, 0x40)
W, H = 540, 960  # medio canvas de 1080 x 1920


def load(path):
    b = open(path, 'rb').read()
    w = struct.unpack('<i', b[:4])[0]
    if w < 0:
        w = -w
        h = struct.unpack('<i', b[4:8])[0]
        data = b[8:8 + w * h * 4]
    else:
        h = w
        data = b[4:4 + w * h * 4]
    return Image.frombytes('RGBA', (w, h), data).transpose(Image.FLIP_TOP_BOTTOM)


def tinted(im, rgb):
    r, g, b, a = im.split()
    r = r.point(lambda v: v * rgb[0] // 255)
    g = g.point(lambda v: v * rgb[1] // 255)
    b = b.point(lambda v: v * rgb[2] // 255)
    return Image.merge('RGBA', (r, g, b, a))


def hexc(h):
    return ((h >> 16) & 255, (h >> 8) & 255, h & 255)


def night(seed):
    top, mid, bot = (0x10, 0x1A, 0x58), (0x08, 0x0E, 0x3A), (0x04, 0x06, 0x1C)
    im = Image.new('RGBA', (W, H))
    d = ImageDraw.Draw(im)
    for y in range(H):
        t = y / (H - 1)
        a, b, k = (top, mid, t / 0.5) if t < 0.5 else (mid, bot, (t - 0.5) / 0.5)
        d.line([(0, y), (W, y)], fill=tuple(int(a[i] + (b[i] - a[i]) * k) for i in range(3)) + (255,))
    rng = random.Random(seed)
    for _ in range(55):
        x, y, r = rng.random() * W, rng.random() * H, rng.choice([1, 1, 1.5, 2])
        d.ellipse((x - r, y - r, x + r, y + r), fill=(255, 255, 255, rng.randint(90, 220)))
    return im


def put(dst, im, cx, cy, size):
    im = im.resize((int(size), int(size * im.height / im.width)), Image.LANCZOS)
    dst.alpha_composite(im, (int(cx - im.width / 2), int(cy - im.height / 2)))


def glow(dst, cx, cy, r, rgb, alpha):
    g = Image.new('RGBA', dst.size, (0, 0, 0, 0))
    ImageDraw.Draw(g).ellipse((cx - r, cy - r, cx + r, cy + r), fill=rgb + (alpha,))
    dst.alpha_composite(g.filter(ImageFilter.GaussianBlur(r * 0.45)))


def clay_text(d, xy, text, size, fill=(255, 255, 255)):
    f = ImageFont.truetype(FB, size)
    x, y = xy
    d.text((x, y + max(3, size // 18)), text, font=f, fill=INK, anchor='mm', stroke_width=max(2, size // 22), stroke_fill=INK)
    d.text((x, y), text, font=f, fill=fill, anchor='mm', stroke_width=max(2, size // 22), stroke_fill=INK)


def title(d, text):
    d.text((24, 40), text, font=ImageFont.truetype(FB, 38), fill=(255, 255, 255), anchor='lm',
           stroke_width=2, stroke_fill=INK)


def ruta(raw):
    im = night(1)
    glow(im, 440, 330, 160, (0xB8, 0xA4, 0xFF), 60)
    for (x, y, s, v) in [(0.02, 0.58, 120, 1), (0.985, 0.40, 96, 0), (0.90, 0.18, 64, 1)]:
        a = tinted(load(f'{raw}/sym_Asteroid_{v}.raw'), (158, 158, 199))
        put(im, a, x * W, H - y * H, s / 2)
    surf = load(f'{raw}/surface.raw').resize((W, int(H * 0.17)), Image.LANCZOS)
    im.alpha_composite(surf, (0, H - surf.height))
    d = ImageDraw.Draw(im)
    title(d, 'Ruta del Tesoro')
    tile = load(f'{raw}/tile.raw')
    rock, reveal, found = hexc(0xC9C3EE), hexc(0x7DD3FC), hexc(0x2DD4BF)
    n, cell = 4, 112
    x0, y0 = (W - n * cell) / 2, 250
    shown = {1: 0, 6: 1, 11: 2, 12: 3}
    for i in range(n * n):
        cx, cy = x0 + (i % n + 0.5) * cell, y0 + (i // n + 0.5) * cell
        col = found if i == 12 else reveal if i in shown else rock
        put(im, tinted(tile, col), cx, cy, cell)
        if i in shown:
            put(im, load(f'{raw}/treasure_{shown[i] if i != 12 else 5}.raw'), cx, cy - 4, cell * 0.66)
    return im


def secuencia(raw):
    im = night(2)
    d = ImageDraw.Draw(im)
    title(d, 'Secuencia Lumínica')
    pal = [tuple(float(v) for v in l.split()) for l in open(f'{raw}/palette.txt')]
    tile = load(f'{raw}/tile.raw')
    n, cell = 3, 150
    x0, y0 = (W - n * cell) / 2, 300
    lit = 4
    for i in range(n * n):
        cx, cy = x0 + (i % n + 0.5) * cell, y0 + (i // n + 0.5) * cell
        c = pal[i]
        rgb = tuple(int(v * 255) for v in (c[3:] if i == lit else c[:3]))
        if i == lit:
            glow(im, cx, cy, cell * 0.62, rgb, 150)
        put(im, tinted(tile, rgb), cx, cy, cell)
        put(im, load(f'{raw}/glyph_{i}.raw'), cx, cy - cell * 0.03, cell * 0.46)
    return im


def comparacion(raw):
    im = night(3)
    glow(im, 20, 900, 200, (0x4C, 0xC9, 0xF0), 50)
    glow(im, 520, 80, 200, (0xFF, 0x6B, 0x4A), 45)
    d = ImageDraw.Draw(im)
    title(d, 'Comparación')
    tile = load(f'{raw}/tile.raw')
    star = load(f'{raw}/count_star.raw')
    cards = [((0x3B, 0x82, 0xF6), 7, 150), ((0xA8, 0x55, 0xF7), 5, 390)]
    for col, count, cx in cards:
        cy, size = 400, 250
        put(im, tinted(tile, col), cx, cy, size)
        dot = size * 0.86 * 0.19
        step = dot * 1.32
        cols = 4
        rows = (count + cols - 1) // cols
        for k in range(count):
            r, c = k // cols, k % cols
            in_row = min(cols, count - r * cols)
            put(im, star, cx + (c - (in_row - 1) / 2) * step, cy - 8 - ((rows - 1) / 2 - r) * step, dot)
    for col, text, cx in [((0x3B, 0x82, 0xF6), '37 - 11', 150), ((0xA8, 0x55, 0xF7), '24', 390)]:
        cy, size = 700, 250
        put(im, tinted(tile, col), cx, cy, size)
        clay_text(ImageDraw.Draw(im), (cx, cy - 10), text, 56 if len(text) > 3 else 96)
    return im


def series(raw):
    im = night(4)
    d = ImageDraw.Draw(im)
    title(d, 'Detective de Series')
    tile = load(f'{raw}/tile.raw')
    terms = ['3', '6', '12', '24', '?']
    step = (W - 40) / 5
    size = step / 0.86 * 0.92
    cy = 420
    for i, t in enumerate(terms):
        cx = 20 + (i + 0.5) * step
        col = (0xFB, 0xBF, 0x24) if t == '?' else (0x5B, 0x6C, 0xF0)
        if t == '?':
            glow(im, cx, cy, size * 0.6, col, 120)
        put(im, tinted(tile, col), cx, cy, size)
        clay_text(ImageDraw.Draw(im), (cx, cy - 6), t, 40)
        if t == '?':
            mag = load(f'{raw}/magnifier.raw').rotate(-6, resample=Image.BICUBIC)
            put(im, mag, cx + size * 0.34, cy - size * 0.38, size * 0.66)
    opts = [('48', (0x3B, 0x82, 0xF6)), ('36', (0xEC, 0x48, 0x99)), ('30', (0x14, 0xB8, 0xA6)), ('44', (0xF9, 0x73, 0x16))]
    for i, (t, col) in enumerate(opts):
        cx, cy2 = 150 + (i % 2) * 240, 640 + (i // 2) * 200
        put(im, tinted(tile, col), cx, cy2, 190)
        clay_text(ImageDraw.Draw(im), (cx, cy2 - 8), t, 64)
    return im


def clay_rect(im, box, fill, r=28, border=5, drop=10):
    d = ImageDraw.Draw(im)
    x0, y0, x1, y1 = box
    d.rounded_rectangle((x0 - border, y0 - border + drop, x1 + border, y1 + border + drop), r + border, fill=INK)
    d.rounded_rectangle((x0 - border, y0 - border, x1 + border, y1 + border), r + border, fill=INK)
    d.rounded_rectangle(box, r, fill=fill)


def banner(im, raw, badge, word, sub, accent):
    base = (0x17, 0x21, 0x3D)
    col = tuple(int(base[i] + (accent[i] - base[i]) * 0.32) for i in range(3))
    clay_rect(im, (30, 110, W - 30, 205), col, r=30, border=3, drop=6)
    put(im, load(f'{raw}/badge_{badge}.raw'), 30 + 38, 157, 58)
    d = ImageDraw.Draw(im)
    d.text((110, 142), word, font=ImageFont.truetype(FB, 40), fill=(255, 255, 255), anchor='lm', stroke_width=1, stroke_fill=INK)
    d.text((110, 182), sub, font=ImageFont.truetype(FB, 17), fill=(235, 238, 255), anchor='lm')


def stroop(raw):
    im = night(5)
    glow(im, 80, 300, 180, (0xFF, 0x6B, 0x4A), 40)
    glow(im, 470, 700, 180, (0xB8, 0xA4, 0xFF), 45)
    d = ImageDraw.Draw(im)
    title(d, 'Tinta o Palabra')
    banner(im, raw, 'Ink', 'TINTA', 'Toca el color con que está escrita', (0x60, 0xA5, 0xFA))
    accent = (0x60, 0xA5, 0xFA)
    clay_rect(im, (40, 280, W - 40, 520), accent, r=30, border=3, drop=6)
    ImageDraw.Draw(im).rounded_rectangle((45, 285, W - 45, 515), 26, fill=(0x1B, 0x27, 0x40))
    for cx, cy in [(62, 302), (W - 62, 302), (62, 498), (W - 62, 498)]:
        put(im, load(f'{raw}/screw.raw'), cx, cy, 20)
    glow(im, W / 2, 400, 150, (0xEF, 0x44, 0x44), 90)
    d = ImageDraw.Draw(im)
    d.text((W / 2, 400), 'VERDE', font=ImageFont.truetype(FB, 92), fill=(0xFF, 0x6B, 0x6B), anchor='mm')
    clay_rect(im, (W / 2 - 80, 262, W / 2 + 80, 300), accent, r=19, border=2, drop=4)
    ImageDraw.Draw(im).text((W / 2, 281), 'TINTA', font=ImageFont.truetype(FB, 24), fill=INK, anchor='mm')
    tile = load(f'{raw}/tile.raw')
    cols = [((0xEF, 0x44, 0x44), 'ROJO'), ((0x3B, 0x82, 0xF6), 'AZUL'), ((0x22, 0xC5, 0x5E), 'VERDE'), ((0xF5, 0x9E, 0x0B), 'AMARILLO'), ((0xA8, 0x55, 0xF7), 'MORADO')]
    for i, (c, n) in enumerate(cols):
        cx = W / 2 + (i - 1) * 180 if i < 3 else W / 2 + (i - 3.5) * 180
        cy = 640 if i < 3 else 790
        put(im, tinted(tile, c), cx, cy, 160)
        ImageDraw.Draw(im).text((cx, cy - 6), n, font=ImageFont.truetype(FB, 24 if len(n) > 5 else 30), fill=(255, 255, 255), anchor='mm', stroke_width=1, stroke_fill=INK)
    return im


def chip(raw):
    im = night(6)
    d = ImageDraw.Draw(im)
    for rr, a in [(420, 30), (560, 22)]:
        d.ellipse((W / 2 - rr / 2, 420 - rr / 2, W / 2 + rr / 2, 420 + rr / 2), outline=(255, 248, 236, a), width=2)
    title(d, 'Cambio de Chip')
    banner(im, raw, 'Direction', 'DIRECCIÓN', 'Toca hacia dónde apunta la nave', (0x38, 0xBD, 0xF8))
    accent = (0x38, 0xBD, 0xF8)
    clay_rect(im, (90, 260, W - 90, 620), accent, r=30, border=3, drop=6)
    ImageDraw.Draw(im).rounded_rectangle((95, 265, W - 95, 615), 26, fill=(0x1B, 0x27, 0x40))
    clay_rect(im, (W / 2 - 95, 242, W / 2 + 95, 280), accent, r=19, border=2, drop=4)
    ImageDraw.Draw(im).text((W / 2, 261), 'DIRECCIÓN', font=ImageFont.truetype(FB, 22), fill=INK, anchor='mm')
    tile = load(f'{raw}/tile.raw')
    cx, cy = W / 2 + 105, 440
    put(im, tinted(tile, (0xF8, 0xFA, 0xFC)), cx, cy, 130)
    put(im, load(f'{raw}/ship_2.raw'), cx, cy - 4, 99)
    pads = [((0x38, 0xBD, 0xF8), 0, W / 2, 700), ((0xF4, 0x72, 0xB6), 1, W / 2, 880), ((0xA3, 0xE6, 0x35), 2, W / 2 - 150, 790), ((0xFB, 0xBF, 0x24), 3, W / 2 + 150, 790)]
    for c, dirn, px, py in pads:
        put(im, tinted(tile, c), px, py, 140)
        put(im, load(f'{raw}/arrow_{dirn}.raw'), px, py - 6, 72)
    return im


def calculo(raw):
    im = night(7)
    glow(im, 100, 180, 150, (0xFF, 0xF4, 0xD6), 70)
    d = ImageDraw.Draw(im)
    d.ellipse((60, 110, 150, 200), fill=(0xFF, 0xF4, 0xD6))
    title(d, 'Cálculo Sereno')
    clay_rect(im, (120, 250, W - 120, 360), (0xE0, 0xF2, 0xFE), r=50, border=4, drop=8)
    d = ImageDraw.Draw(im)
    d.ellipse((150, 262, 200, 280), fill=(255, 255, 255, 200))
    d.text((W / 2, 305), '7 × 8', font=ImageFont.truetype(FB, 64), fill=(0x0B, 0x2A, 0x3F), anchor='mm')
    water = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    ImageDraw.Draw(water).rounded_rectangle((30, 480, W - 30, 610), 28, fill=(26, 92, 158, 200), outline=INK, width=4)
    im.alpha_composite(water)
    ImageDraw.Draw(im).rounded_rectangle((45, 488, W - 45, 492), 2, fill=(205, 242, 255))
    put(im, load(f'{raw}/lily_0.raw'), 0.13 * W, 470, 76)
    put(im, load(f'{raw}/lily_1.raw'), 0.87 * W, 466, 90)
    tile = load(f'{raw}/tile.raw')
    opts = [('54', (0x7D, 0xD3, 0xFC)), ('56', (0xA7, 0xF3, 0xD0)), ('58', (0xFD, 0xE6, 0x8A)), ('48', (0xF9, 0xA8, 0xD4))]
    for i, (t, c) in enumerate(opts):
        cx, cy = 150 + (i % 2) * 240, 710 + (i // 2) * 170
        put(im, tinted(tile, c), cx, cy, 170)
        ImageDraw.Draw(im).text((cx, cy - 8), t, font=ImageFont.truetype(FB, 58), fill=(0x0B, 0x2A, 0x3F), anchor='mm')
    return im


def anagramas(raw):
    im = night(8)
    d = ImageDraw.Draw(im)
    rng = random.Random(9)
    for ch in 'AEMNORSL':
        d.text((rng.random() * W, 150 + rng.random() * 700), ch, font=ImageFont.truetype(FB, rng.randint(40, 80)), fill=(0xB8, 0xA4, 0xFF, 40))
    title(d, 'Anagramas')
    clay_rect(im, (30, 110, W - 30, 200), (0x5A, 0x2E, 0x6E), r=30, border=3, drop=6)
    ImageDraw.Draw(im).text((W / 2, 155), 'Pista: se ve de noche', font=ImageFont.truetype(FB, 26), fill=(255, 255, 255), anchor='mm')
    slot = load(f'{raw}/slot.raw')
    tile = load(f'{raw}/tile.raw')
    word, placed = 'LUNA', 2
    n, size = 4, 100
    x0 = (W - n * (size + 12)) / 2 + size / 2
    for i in range(n):
        cx = x0 + i * (size + 12)
        put(im, slot, cx, 330, size)
        if i < placed:
            put(im, tinted(tile, (0xFF, 0xD1, 0x7A)), cx, 326, size / 0.86)
            ImageDraw.Draw(im).text((cx, 320), word[i], font=ImageFont.truetype(FB, 58), fill=(0x2A, 0x17, 0x40), anchor='mm')
    for i, ch in enumerate('NA'):
        cx = W / 2 - 60 + i * 120
        put(im, tinted(tile, (0xFD, 0xE9, 0xC8)), cx, 520, size / 0.86)
        ImageDraw.Draw(im).text((cx, 514), ch, font=ImageFont.truetype(FB, 58), fill=(0x2A, 0x17, 0x40), anchor='mm')
    acts = [('Borrar', (0x5C, 0x4D, 0x9E), 'Backspace'), ('Pista', (0xD9, 0x8C, 0x1A), 'Hint'), ('Pasar', (0xC7, 0x47, 0x80), 'Skip')]
    bw = (W - 60 - 20) / 3
    for i, (t, c, ic) in enumerate(acts):
        x = 30 + i * (bw + 10)
        clay_rect(im, (x, 860, x + bw, 925), c, r=24, border=3, drop=5)
        put(im, load(f'{raw}/icon_{ic}.raw'), x + bw * 0.2, 892, 44)
        ImageDraw.Draw(im).text((x + bw * 0.63, 892), t, font=ImageFont.truetype(FB, 26), fill=(255, 255, 255), anchor='mm', stroke_width=1, stroke_fill=INK)
    return im


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('raw')
    ap.add_argument('--out', default=ROOT + '/docs/previews')
    a = ap.parse_args()
    panels = [ruta(a.raw), secuencia(a.raw), comparacion(a.raw), series(a.raw)]
    gap = 24
    sheet = Image.new('RGBA', (len(panels) * W + (len(panels) + 1) * gap, H + 2 * gap), (0x02, 0x03, 0x10, 255))
    for i, p in enumerate(panels):
        sheet.alpha_composite(p, (gap + i * (W + gap), gap))
    os.makedirs(a.out, exist_ok=True)
    sheet.convert('RGB').save(os.path.join(a.out, 'arte-juegos.png'))
    panels = [stroop(a.raw), chip(a.raw), calculo(a.raw), anagramas(a.raw)]
    sheet = Image.new('RGBA', (len(panels) * W + (len(panels) + 1) * gap, H + 2 * gap), (0x02, 0x03, 0x10, 255))
    for i, p in enumerate(panels):
        sheet.alpha_composite(p, (gap + i * (W + gap), gap))
    sheet.convert('RGB').save(os.path.join(a.out, 'arte-juegos-2.png'))
    print('OK')


if __name__ == '__main__':
    main()
