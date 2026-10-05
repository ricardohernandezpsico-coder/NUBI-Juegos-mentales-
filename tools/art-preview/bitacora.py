"""Maqueta de Bitácora de Misión (disposición según BitacoraGameController.Layout a medio canvas). La misión es real:
la genera BitacoraContract.Generate vía ArtPreview (bit_mission.txt); el arte sale de los .raw (planetas-puerto
Estelar, hallazgos, sonda, marcas).

Uso: python3 tools/art-preview/bitacora.py <raw> [--out docs/previews]  ->  bitacora.png
"""
import argparse
import math
import os

from PIL import Image, ImageDraw, ImageFont

from juegos import INK, W, H, ROOT, FB, load, night, put, glow, clay_text, hexc
from piloto import hud

K = 0.5
SUN, LIME, CORAL, SKY, GRAPE, CREAM = hexc(0xFFC93C), hexc(0x9BE564), hexc(0xFF6B4A), hexc(0x4CC9F0), hexc(0xB8A4FF), (255, 248, 236)
DEEP = (0x1B, 0x24, 0x66)
PLANET_COLORS = [0xFF6B4A, 0xFFC93C, 0x4CC9F0, 0x9BE564, 0xB8A4FF, 0xFF7BC0, 0x5FD68A, 0xFF8A3D]
PLANET_NAMES = ["Coral", "Sol", "Cielo", "Lima", "Uva", "Rosa", "Menta", "Naranja"]
FIND_NAMES = ["una llave", "una campana", "una pluma", "una concha", "un reloj de arena", "una brújula", "un farol",
              "una corona", "una bellota", "un libro", "una copa", "una gema", "un hongo", "un ancla",
              "una flor", "un paraguas"]

# Layout (en px de la maqueta, medio canvas)
MAP_X0, MAP_W = 43, 455
MAP_Y0, MAP_H = 145, 403
CAPTION_Y, SUB_Y, MID_Y = 623, 654, 807
PLANET, FIND, PROBE, SLOT, OPTION = 75, 62, 48, 46, 75


def load_mission(raw):
    planets, route, finds, drawer = [], [], [], []
    for line in open(f'{raw}/bit_mission.txt'):
        t = line.split()
        if t[0] == 'P':
            planets.append((int(t[1]), float(t[2]), float(t[3])))
        elif t[0] == 'R':
            route = list(map(int, t[1:]))
        elif t[0] == 'F':
            finds = list(map(int, t[1:]))
        elif t[0] == 'D':
            drawer = list(map(int, t[1:]))
    return planets, route, finds, drawer


def pos(planet):
    _, x, y = planet
    return MAP_X0 + x * MAP_W, MAP_Y0 + y * MAP_H


def font(size):
    return ImageFont.truetype(FB, size)


def base(seed, level, info):
    im = night(seed)
    d = ImageDraw.Draw(im)
    hud(im, d, level, 0, 2, 'Bitácora de Misión')
    d = ImageDraw.Draw(im)
    # El chip de puntos se tapa con el texto de la fase (como hace SetInfo).
    d.rounded_rectangle((150, 78, 150 + 16 + 8 * len(info), 100), 11, fill=DEEP)
    d.text((158, 89), info, fill=SUN, anchor='lm', font=font(13))
    return im


def draw_planets(im, raw, planets, active=None, dim=False):
    d = ImageDraw.Draw(im)
    for i, p in enumerate(planets):
        x, y = pos(p)
        if i == active:
            glow(im, x, y, 70, hexc(PLANET_COLORS[p[0]]), 150)
        spr = load(f'{raw}/port_{p[0]}.raw')
        if dim:
            spr.putalpha(spr.getchannel('A').point(lambda v: int(v * 0.35)))
        put(im, spr, x, y, PLANET)
        d = ImageDraw.Draw(im)
        d.text((x, y + 47), PLANET_NAMES[p[0]], fill=CREAM + (215,), anchor='mm', font=font(15))


def find_on(im, raw, planets, idx, fid, scale=1.0, alpha=1.0):
    x, y = pos(planets[idx])
    spr = load(f'{raw}/bit_find_{fid}.raw')
    if alpha < 1:
        spr.putalpha(spr.getchannel('A').point(lambda v: int(v * alpha)))
    put(im, spr, x, y - 46, FIND * scale)


def ring(im, x, y, r, col, width=4):
    layer = Image.new('RGBA', im.size, (0, 0, 0, 0))
    ImageDraw.Draw(layer).ellipse((x - r, y - r, x + r, y + r), outline=col + (235,), width=width)
    im.alpha_composite(layer)


def sparks(im, x, y, n, reach, col):
    layer = Image.new('RGBA', im.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    for k in range(n):
        a = 2 * math.pi * k / n + 0.3
        r = reach * (0.55 + 0.45 * ((k * 37) % 10) / 10)
        px, py = x + r * math.cos(a), y + r * math.sin(a)
        s = 3 + (k % 3)
        d.polygon([(px, py - s * 2), (px + s * 0.6, py), (px, py + s * 2), (px - s * 0.6, py)], fill=col + (230,))
        d.polygon([(px - s * 2, py), (px, py + s * 0.6), (px + s * 2, py), (px, py - s * 0.6)], fill=col + (230,))
    im.alpha_composite(layer)


def caption(im, main, sub='', color_name=None):
    d = ImageDraw.Draw(im)
    if color_name:
        # "Planeta <Nombre> · hallazgo" con el nombre en su color.
        pre, name, post = color_name
        f = font(25)
        total = d.textlength(pre + name + post, font=f)
        x = W / 2 - total / 2
        for text, col in ((pre, (255, 255, 255)), (name, None), (post, (255, 255, 255))):
            c = col if col else hexc(PLANET_COLORS[PLANET_NAMES.index(name)])
            for ox, oy in ((0, 3), (-2, 0), (2, 0), (0, -2), (0, 2)):
                d.text((x + ox, CAPTION_Y + oy), text, fill=INK, anchor='lm', font=f)
            d.text((x, CAPTION_Y), text, fill=c, anchor='lm', font=f)
            x += d.textlength(text, font=f)
    else:
        clay_text(d, (W / 2, CAPTION_Y), main, 25)
    if sub:
        d.text((W / 2, SUB_Y), sub, fill=CREAM + (205,), anchor='mm', font=font(16))


def log_strip(im, raw, items, saved):
    d = ImageDraw.Draw(im)
    d.text((W / 2, MID_Y - 40), 'bitácora', fill=CREAM + (180,), anchor='mm', font=font(15))
    gap = 54
    for i in range(items):
        x = W / 2 + (i - (items - 1) / 2) * gap
        d.ellipse((x - SLOT / 2, MID_Y - SLOT / 2, x + SLOT / 2, MID_Y + SLOT / 2), fill=DEEP, outline=CREAM + (70,), width=2)
        if i < len(saved) and saved[i] is not None:
            put(im, load(f'{raw}/bit_find_{saved[i]}.raw'), x, MID_Y, SLOT * 0.95)
            d = ImageDraw.Draw(im)


def drawer(im, raw, ids):
    n = len(ids)
    cols = 4 if n <= 8 else 5
    rows = (n + cols - 1) // cols
    gap = 86
    d = ImageDraw.Draw(im)
    for i, fid in enumerate(ids):
        row, col = divmod(i, cols)
        in_row = min(cols, n - row * cols)
        x = W / 2 + (col - (in_row - 1) / 2) * gap
        y = MID_Y + (row - (rows - 1) / 2) * gap
        d.ellipse((x - OPTION / 2, y - OPTION / 2, x + OPTION / 2, y + OPTION / 2), fill=DEEP, outline=SKY + (115,), width=3)
        put(im, load(f'{raw}/bit_find_{fid}.raw'), x, y, OPTION * 0.8)
        d = ImageDraw.Draw(im)


def badge(im, planets, idx, n, col):
    x, y = pos(planets[idx])
    bx, by = x + 33, y + 21
    d = ImageDraw.Draw(im)
    d.ellipse((bx - 15, by - 15 + 2, bx + 15, by + 15 + 2), fill=INK)
    d.ellipse((bx - 15, by - 15, bx + 15, by + 15), fill=col, outline=INK, width=3)
    d.text((bx, by), str(n), fill=INK, anchor='mm', font=font(17))


def mark(im, raw, planets, idx, ok):
    x, y = pos(planets[idx])
    put(im, load(f'{raw}/mark_{"check" if ok else "cross"}.raw'), x + 39, y - 39, 31)


def trail(im, a, b, n=16, bend=40):
    layer = Image.new('RGBA', im.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    dx, dy = b[0] - a[0], b[1] - a[1]
    L = math.hypot(dx, dy) or 1
    nx, ny = -dy / L * bend, dx / L * bend
    for k in range(n):
        t = k / (n - 1)
        x = a[0] + dx * t + nx * math.sin(math.pi * t)
        y = a[1] + dy * t + ny * math.sin(math.pi * t)
        r = 2.5 + 1.5 * t
        d.ellipse((x - r, y - r, x + r, y + r), fill=CREAM + (int(40 + 170 * t),))
    im.alpha_composite(layer)


def toast(im, title, sub, color):
    d = ImageDraw.Draw(im)
    top, h, w = 4, 126, 225
    d.rounded_rectangle((W / 2 - w, top, W / 2 + w, top + h), 40, fill=DEEP + (255,), outline=INK, width=4)
    d.ellipse((W / 2 - w + 14, top + h / 2 - 8, W / 2 - w + 30, top + h / 2 + 8), fill=color)
    clay_text(d, (W / 2, top + h / 2 - 16), title, 22)
    d.text((W / 2, top + h / 2 + 20), sub, fill=(215, 220, 245), anchor='mm', font=font(16))


def frame_transmission(raw, m):
    planets, route, finds, _ = m
    im = base(91, 6, 'Parada 3 de 5')
    draw_planets(im, raw, planets, active=route[2])
    a = pos(planets[route[1]])
    b = pos(planets[route[2]])
    trail(im, (a[0], a[1] - 40), (b[0], b[1] - 40))
    x, y = b
    glow(im, x, y - 36, 55, SUN, 150)
    find_on(im, raw, planets, route[2], finds[2])
    sparks(im, x, y - 36, 10, 58, SUN)
    ring(im, x, y - 36, 42, SUN, 4)
    put(im, load(f'{raw}/bit_probe.raw'), x + 42, y - 78, PROBE)
    name = PLANET_NAMES[planets[route[2]][0]]
    caption(im, '', 'Tócalo para guardarlo', ('Planeta ', name, ' · ' + FIND_NAMES[finds[2]]))
    log_strip(im, raw, len(route), [finds[0], finds[1]])
    return im


def frame_review(raw, m):
    planets, route, finds, draw = m
    im = base(92, 6, 'Primer repaso')
    target = route[3]
    draw_planets(im, raw, planets, active=target)
    x, y = pos(planets[target])
    ring(im, x, y, 48, SUN, 5)
    name = PLANET_NAMES[planets[target][0]]
    caption(im, '', 'Elige el hallazgo', ('¿Qué había en el planeta ', name, '?'))
    drawer(im, raw, draw)
    return im


def frame_route(raw, m):
    planets, route, finds, _ = m
    im = base(93, 6, 'La ruta')
    draw_planets(im, raw, planets)
    chosen = {route[0]: finds[0], route[1]: finds[1], route[2]: finds[2], route[3]: 11, route[4]: finds[4]}
    for p, f in chosen.items():
        find_on(im, raw, planets, p, f, scale=0.8)
    for n, p in enumerate([route[0], route[1], route[3]]):
        badge(im, planets, p, n + 1, SUN)
    caption(im, 'La ruta', 'Toca los planetas en el orden en que pasó la sonda')
    return im


def frame_reveal(raw, m):
    planets, route, finds, _ = m
    im = base(94, 6, 'Informe')
    draw_planets(im, raw, planets)
    for i, p in enumerate(route):
        ok = i != 3
        if ok:
            pass
        else:
            find_on(im, raw, planets, p, finds[i], alpha=0.55)
        mark(im, raw, planets, p, ok)
        badge(im, planets, p, i + 1, LIME if i in (0, 1, 4) else CREAM)
    for i in (0, 1, 2, 4):
        x, y = pos(planets[route[i]])
        sparks(im, x, y - 30, 6, 40, SUN)
    caption(im, 'Recordaste 4 de 5', '')
    log_strip(im, raw, len(route), [finds[0], finds[1], finds[2], None, finds[4]])
    toast(im, '¡A tu bitácora!', '4 hallazgos archivados', SUN)
    return im


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('raw')
    ap.add_argument('--out', default=ROOT + '/docs/previews')
    a = ap.parse_args()
    m = load_mission(a.raw)
    panels = [frame_transmission(a.raw, m), frame_review(a.raw, m), frame_route(a.raw, m), frame_reveal(a.raw, m)]
    gap = 24
    sheet = Image.new('RGBA', (len(panels) * W + (len(panels) + 1) * gap, H + 2 * gap), (0x02, 0x03, 0x10, 255))
    for i, p in enumerate(panels):
        sheet.alpha_composite(p, (gap + i * (W + gap), gap))
    os.makedirs(a.out, exist_ok=True)
    sheet.convert('RGB').save(os.path.join(a.out, 'bitacora.png'))
    print('OK')


if __name__ == '__main__':
    main()
