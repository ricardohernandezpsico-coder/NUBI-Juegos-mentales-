"""Maqueta de Tráfico Estelar (disposición según TrafficGameController.Layout a medio canvas; las redes salen de
TrafficContract.BuildNetwork vía ArtPreview (traffic_net_N.txt) y el arte de los .raw: puertos, cápsulas, desvíos).

Uso: python3 tools/art-preview/trafico.py <raw> [--out docs/previews]  ->  trafico.png
"""
import argparse
import math
import os

from PIL import Image, ImageDraw

from juegos import INK, W, H, ROOT, load, night, put, glow, clay_text, hexc
from piloto import hud

LIME, SUN, CORAL, SKY, GRAPE, CREAM = hexc(0x9BE564), hexc(0xFFC93C), hexc(0xFF6B4A), hexc(0x4CC9F0), hexc(0xB8A4FF), (255, 248, 236)
COLORS = [0xFF6B4A, 0xFFC93C, 0x4CC9F0, 0x9BE564, 0xB8A4FF, 0xFF7BC0, 0x5FD68A, 0xFF8A3D]
FX, FY, FW, FH = 20, 115, 500, 825


def load_net(raw, ports):
    nodes = []
    for line in open(f'{raw}/traffic_net_{ports}.txt'):
        x, y, parent, color = line.split()
        nodes.append((float(x), float(y), int(parent), int(color)))
    children = {i: [] for i in range(len(nodes))}
    for i, (_, _, p, _) in enumerate(nodes):
        if p >= 0:
            children[p].append(i)
    return nodes, children


def ui(n):
    return FX + n[0] * FW, FY + n[1] * FH


def base(raw, seed, level, points, streak, prompt=''):
    im = night(seed)
    d = ImageDraw.Draw(im)
    hud(im, d, level, points, streak, 'Tráfico Estelar')
    d = ImageDraw.Draw(im)
    d.rounded_rectangle((30, 98, W - 30, 106), 4, fill=(255, 255, 255, 30))
    d.rounded_rectangle((30, 98, 30 + (W - 60) * 0.55, 106), 4, fill=LIME)
    if prompt:
        clay_text(d, (W / 2, FY + 0.24 * FH), prompt, 25)
    return im


def network(im, raw, nodes, children, states):
    layer = Image.new('RGBA', im.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    for n, ch in children.items():
        for idx, c in enumerate(ch):
            active = len(ch) != 2 or states.get(n, 0) == idx
            a, b = ui(nodes[n]), ui(nodes[c])
            d.line([(a[0], a[1] + 2.5), (b[0], b[1] + 2.5)], fill=INK + (230 if active else 100,), width=11)
            d.line([a, b], fill=CREAM + (230 if active else 56,), width=8)
            if active:
                d.line([a, b], fill=SKY + (240,), width=4)
    im.alpha_composite(layer)
    # Portal.
    sx, sy = ui(nodes[0])
    glow(im, sx, sy, 60, GRAPE, 190)
    d = ImageDraw.Draw(im)
    d.arc((sx - 36, sy - 36, sx + 36, sy + 36), 20, 310, fill=GRAPE, width=7)
    d.ellipse((sx - 17, sy - 17, sx + 17, sy + 17), fill=INK)
    knob = load(f'{raw}/traffic_knob.raw')
    for n, ch in children.items():
        x, y = ui(nodes[n])
        if len(ch) == 2:
            c = ch[states.get(n, 0)]
            cx, cy = ui(nodes[c])
            ang = math.degrees(math.atan2(-(cy - y), cx - x)) - 90
            k = knob.resize((64, 64), Image.LANCZOS).rotate(ang, resample=Image.BICUBIC, expand=True)
            im.alpha_composite(k, (int(x - k.width / 2), int(y - k.height / 2)))
        elif len(ch) == 0:
            put(im, load(f'{raw}/traffic_port_{nodes[n][3]}.raw'), x, y, 75)


def pod(im, raw, nodes, a, b, t, color):
    ax, ay = ui(nodes[a])
    bx, by = ui(nodes[b])
    x, y = ax + (bx - ax) * t, ay + (by - ay) * t
    glow(im, x, y, 30, hexc(COLORS[color]), 110)
    put(im, load(f'{raw}/traffic_pod_{color}.raw'), x, y, 43)


def port_of(nodes, color):
    return next(i for i, n in enumerate(nodes) if n[3] == color)


def frame_intro(raw):
    nodes, ch = load_net(raw, 4)
    im = base(raw, 81, 3, 420, 2)
    network(im, raw, nodes, ch, {})
    d = ImageDraw.Draw(im)
    d.rounded_rectangle((W / 2 - 190, 128, W / 2 + 190, 184), 24, fill=(0x1B, 0x24, 0x66, 255), outline=SKY, width=3)
    clay_text(d, (W / 2, 146), 'Lleva cada cápsula a su planeta', 17)
    d.text((W / 2, 170), 'Toca los desvíos para cambiar la ruta', fill=(215, 220, 245), anchor='mm', font=__import__('PIL.ImageFont', fromlist=['x']).truetype(__import__('juegos').FB, 14))
    root = ch[0][0]
    pod(im, raw, nodes, 0, root, 0.6, nodes[port_of(nodes, 1)][3])
    return im


def busy_states(nodes, ch, seed):
    st = {}
    for i, (n, c) in enumerate(ch.items()):
        if len(c) == 2:
            st[n] = (i + seed) % 2
    return st


def frame_busy(raw):
    nodes, ch = load_net(raw, 8)
    im = base(raw, 82, 11, 5320, 7)
    st = busy_states(nodes, ch, 1)
    network(im, raw, nodes, ch, st)
    # Cápsulas en distintos tramos.
    edges = [(p, c) for p, cs in ch.items() for c in cs]
    for k, (a, b) in enumerate(edges[1:12:2]):
        pod(im, raw, nodes, a, b, 0.35 + 0.1 * (k % 3), k % 8)
    return im


def frame_deliver(raw):
    nodes, ch = load_net(raw, 6)
    im = base(raw, 83, 7, 2480, 5)
    st = busy_states(nodes, ch, 0)
    network(im, raw, nodes, ch, st)
    target = port_of(nodes, 3)
    x, y = ui(nodes[target])
    glow(im, x, y, 70, LIME, 160)
    layer = Image.new('RGBA', im.size, (0, 0, 0, 0))
    ImageDraw.Draw(layer).ellipse((x - 62, y - 62, x + 62, y + 62), outline=LIME + (220,), width=5)
    im.alpha_composite(layer)
    put(im, load(f'{raw}/traffic_port_3.raw'), x, y, 90)
    d = ImageDraw.Draw(im)
    clay_text(d, (x, y - 70), '+140', 26, SUN)
    root = ch[0][0]
    pod(im, raw, nodes, 0, root, 0.4, 0)
    return im


def frame_wrong(raw):
    nodes, ch = load_net(raw, 6)
    im = base(raw, 84, 7, 2480, 0)
    st = busy_states(nodes, ch, 1)
    network(im, raw, nodes, ch, st)
    wrong = port_of(nodes, 1)
    x, y = ui(nodes[wrong])
    glow(im, x, y, 60, CORAL, 150)
    put(im, load(f'{raw}/traffic_port_1.raw'), x + 6, y, 75)
    put(im, load(f'{raw}/mark_cross.raw'), x + 28, y - 28, 40)
    root = ch[0][0]
    pod(im, raw, nodes, 0, root, 0.8, 4)
    toast_y = 150
    d = ImageDraw.Draw(im)
    d.rounded_rectangle((W / 2 - 150, toast_y - 22, W / 2 + 150, toast_y + 22), 22, fill=(0x1B, 0x24, 0x66, 255), outline=SUN, width=3)
    clay_text(d, (W / 2, toast_y), 'Mira el color y el símbolo', 17)
    return im


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('raw')
    ap.add_argument('--out', default=ROOT + '/docs/previews')
    a = ap.parse_args()
    panels = [frame_intro(a.raw), frame_busy(a.raw), frame_deliver(a.raw), frame_wrong(a.raw)]
    gap = 24
    sheet = Image.new('RGBA', (len(panels) * W + (len(panels) + 1) * gap, H + 2 * gap), (0x02, 0x03, 0x10, 255))
    for i, p in enumerate(panels):
        sheet.alpha_composite(p, (gap + i * (W + gap), gap))
    os.makedirs(a.out, exist_ok=True)
    sheet.convert('RGB').save(os.path.join(a.out, 'trafico.png'))
    print('OK')


if __name__ == '__main__':
    main()
