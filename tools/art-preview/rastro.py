"""Lámina del arte de «Rastro de luz» (3-oct), armada con los .raw REALES que vuelca ArtPreview (los generadores de Unity, sin Unity):
los 9 luceros de cristal, los 4 íconos de modo dibujados, la ✗, el aro de ayuda, el disco punteado y Nubi maestra del tutorial; y una
pantalla de ejemplo (el cielo, el tablero con la chispa y su rastro, un error y la cinta del dedo). Es una maqueta de la disposición, NO la app.

Uso: python3 tools/art-preview/rastro.py <raw> [--out docs/previews]  ->  rastro-arte.png
"""
import argparse
import math
import os
import random

from PIL import Image, ImageDraw, ImageFont

import juegos as J
from juegos import load, put, glow, hexc, tinted

FB, FR = J.FB, J.ROOT + '/unity/NeuroVidaCore/Assets/Resources/Fonts/Fredoka-SemiBold.ttf'
INK = J.INK
SKY_TINT = [(76, 201, 240), (184, 164, 255), (120, 240, 200), (255, 159, 122)]
TRAIL = [(255, 226, 122), (200, 170, 255), (160, 250, 210), (255, 190, 150)]
SUN, CORAL = (255, 201, 74), (255, 122, 92)
S = 2  # px por dp de la pantalla de ejemplo


def sky(w, h, seed=7):
    im = Image.new('RGBA', (w, h))
    d = ImageDraw.Draw(im)
    top, mid, bot = (0x02, 0x03, 0x0F), (0x05, 0x08, 0x23), (0x0A, 0x0F, 0x33)
    for y in range(h):
        t = y / (h - 1)
        a, b, k = (top, mid, t / 0.55) if t < 0.55 else (mid, bot, (t - 0.55) / 0.45)
        d.line([(0, y), (w, y)], fill=tuple(int(a[i] + (b[i] - a[i]) * min(1, k)) for i in range(3)) + (255,))
    rng = random.Random(seed)
    for _ in range(240):
        x, y, r = rng.random() * w, rng.random() * h, 0.4 + rng.random() ** 3 * 1.4
        d.ellipse((x - r, y - r, x + r, y + r), fill=(255, 255, 255, rng.randint(60, 200)))
    return im


def read_board(raw):
    pts = []
    for line in open(os.path.join(raw, 'rastro_board.txt')):
        x, y, c = line.split()
        pts.append((float(x), float(y), int(c)))
    return pts


def line(img, a, b, w, rgb, alpha):
    lay = Image.new('RGBA', img.size, (0, 0, 0, 0))
    ImageDraw.Draw(lay).line([a, b], fill=rgb + (alpha,), width=int(w))
    img.alpha_composite(lay)


def screen(raw, mode=0):
    """Una pantalla de 360 x 640 dp (a 2 px por dp) con el tablero, la chispa, su rastro y los marcadores."""
    w, h = 360 * S, 640 * S
    im = sky(w, h, 7 + mode)
    glow(im, 180 * S, 352 * S, 150 * S, SKY_TINT[mode], 40)
    board = read_board(raw)
    disc = load(os.path.join(raw, 'rastro_disc.raw'))
    put(im, tinted(disc, (120, 110, 200)), 180 * S, 352 * S, 300 * S)
    path = [(0, 1), (1, 2), (2, 3)] if mode != 3 else [(0, 1), (1, 2), (2, 3), (3, 4)]
    for a, b in path:
        line(im, (board[a][0] * S, board[a][1] * S), (board[b][0] * S, board[b][1] * S), 5 * S, TRAIL[mode], 150)
    for i, (x, y, c) in enumerate(board):
        glow(im, x * S, y * S, 38 * S, hexc(c), 70)
        put(im, load(os.path.join(raw, f'rastro_orb_{i}.raw')), x * S, y * S, 56 * S / 0.94)
    # error: el tocado con aro coral y ✗, el correcto con aro sol; y en el modo 0 la chispa
    if mode == 1:
        for idx, rgb in ((5, CORAL), (4, SUN)):
            x, y, _ = board[idx]
            ring = Image.new('RGBA', im.size, (0, 0, 0, 0))
            ImageDraw.Draw(ring).ellipse((x * S - 36 * S, y * S - 36 * S, x * S + 36 * S, y * S + 36 * S), outline=rgb + (255,), width=4 * S)
            im.alpha_composite(ring)
        x, y, _ = board[5]
        put(im, tinted(load(os.path.join(raw, 'rastro_x.raw')), CORAL), x * S, y * S, 26 * S)
    if mode == 0:
        x, y, _ = board[3]
        glow(im, x * S, y * S, 26 * S, TRAIL[0], 160)
        put(im, load(os.path.join(raw, 'rastro_orb_3.raw')), x * S, y * S, 1)  # (sin efecto: solo asegura la carga)
        d = ImageDraw.Draw(im)
        d.ellipse((x * S - 9 * S, y * S - 9 * S, x * S + 9 * S, y * S + 9 * S), fill=(255, 255, 255, 255))
    if mode == 2:
        x, y, _ = board[6]
        dash = load(os.path.join(raw, 'rastro_dashed.raw'))
        put(im, tinted(dash, SUN), x * S, y * S, 78 * S)
    d = ImageDraw.Draw(im)
    # marcador: modo + cuántas luces, vidas y contador
    names = ['El rastro', 'Al revés', 'El cielo gira', 'En marcha']
    counts = ['3 luces', '4 luces', '4 luces', 'Últimas 3 luces']
    f15 = ImageFont.truetype(FB, 15 * S)
    px = 16 * S
    tw = d.textlength(names[mode], font=f15)
    d.rounded_rectangle((px, 10 * S, px + 24 * S + 22 * S + tw + 6 * S, 46 * S), radius=18 * S, fill=(27, 36, 102, 230), outline=INK, width=3)
    put(im, tinted(load(os.path.join(raw, f'rastro_icon_{mode}.raw')), SUN), px + 24 * S, 28 * S, 22 * S)
    d.text((px + 46 * S, 28 * S), names[mode], font=f15, fill=SUN, anchor='lm')
    tw2 = d.textlength(counts[mode], font=f15)
    d.rounded_rectangle((px, 52 * S, px + 24 * S + tw2, 88 * S), radius=18 * S, fill=(27, 36, 102, 230), outline=INK, width=3)
    d.text((px + 12 * S, 70 * S), counts[mode], font=f15, fill=(127, 216, 255), anchor='lm')
    for i in range(3):
        cx = (360 - 16 - 6 - (2 - i) * 18) * S
        d.ellipse((cx - 6 * S, 28 * S - 6 * S, cx + 6 * S, 28 * S + 6 * S), fill=(255, 246, 224, 255) if i < 3 - (mode == 1) else (255, 255, 255, 46))
    d.text(((360 - 16) * S, 70 * S), str(5 + mode), font=ImageFont.truetype(FB, 32 * S), fill=(255, 255, 255), anchor='rm')
    d.text(((360 - 16) * S, 100 * S), 'luces recordadas', font=ImageFont.truetype(FR, 14 * S), fill=(171, 165, 210), anchor='rm')
    rules = ['Mira el rastro', 'Esa no era: la correcta tiene el aro amarillo', '¡El cielo gira!', 'Repite las últimas 3']
    d.text((180 * S, 566 * S), rules[mode], font=ImageFont.truetype(FB, 20 * S), fill=(255, 255, 255), anchor='mm')
    d.text((180 * S, 596 * S), names[mode], font=ImageFont.truetype(FR, 15 * S), fill=(171, 165, 210), anchor='mm')
    return im


def parts(raw):
    """Las piezas sueltas sobre el cielo."""
    w, h = 1080, 520
    im = sky(w, h, 3)
    d = ImageDraw.Draw(im)
    d.text((24, 22), 'Rastro de luz: arte horneado por código (los generadores reales de Unity)', font=ImageFont.truetype(FB, 24), fill=(255, 255, 255))
    board = read_board(raw)
    for i, (_, _, c) in enumerate(board):
        x = 70 + i * 112
        glow(im, x, 120, 54, hexc(c), 80)
        put(im, load(os.path.join(raw, f'rastro_orb_{i}.raw')), x, 120, 96)
    names = ['El rastro', 'Al revés', 'El cielo gira', 'En marcha']
    for m in range(4):
        x = 90 + m * 150
        glow(im, x, 290, 70, SKY_TINT[m], 60)
        put(im, load(os.path.join(raw, f'rastro_icon_{m}.raw')), x, 280, 100)
        d.text((x, 360), names[m], font=ImageFont.truetype(FB, 18), fill=SUN, anchor='mm')
    x = 720
    put(im, tinted(load(os.path.join(raw, 'rastro_x.raw')), CORAL), x, 270, 80)
    d.text((x, 340), 'error (✗ dibujada)', font=ImageFont.truetype(FR, 15), fill=(217, 212, 245), anchor='mm')
    put(im, tinted(load(os.path.join(raw, 'rastro_dashed.raw')), SUN), x + 120, 270, 90)
    d.text((x + 120, 340), 'aro de ayuda', font=ImageFont.truetype(FR, 15), fill=(217, 212, 245), anchor='mm')
    put(im, tinted(load(os.path.join(raw, 'rastro_disc.raw')), (160, 150, 230)), x + 260, 270, 130)
    d.text((x + 260, 350), 'disco del tablero', font=ImageFont.truetype(FR, 15), fill=(217, 212, 245), anchor='mm')
    put(im, load(os.path.join(raw, 'nubi_maestra.raw')), 200, 430, 160)
    d.text((300, 430), 'Nubi maestra: la tarjeta de entrada del tutorial guiado', font=ImageFont.truetype(FR, 18), fill=(217, 212, 245), anchor='lm')
    return im


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('raw')
    ap.add_argument('--out', default=J.ROOT + '/docs/previews')
    a = ap.parse_args()
    shots = [screen(a.raw, m) for m in range(4)]
    gap = 24
    pw = parts(a.raw)
    width = max(pw.width, len(shots) * shots[0].width + (len(shots) + 1) * gap)
    sheet = Image.new('RGBA', (width, pw.height + shots[0].height + 3 * gap), (0x02, 0x03, 0x10, 255))
    sheet.alpha_composite(pw, (gap, gap))
    for i, s in enumerate(shots):
        sheet.alpha_composite(s, (gap + i * (s.width + gap), pw.height + 2 * gap))
    os.makedirs(a.out, exist_ok=True)
    sheet.convert('RGB').save(os.path.join(a.out, 'rastro-arte.png'))
    print('OK')


if __name__ == '__main__':
    main()
