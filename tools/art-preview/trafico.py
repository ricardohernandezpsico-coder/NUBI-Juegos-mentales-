"""Maqueta de Tráfico Estelar (disposición según TrafficGameController.Layout a medio canvas). Las redes y sus
recorridos curvos salen de TrafficContract.BuildNetwork vía ArtPreview (traffic_net_<puertos>_<semilla>.txt) y el arte
de los .raw: estación, puertos, cápsulas, desvíos. Los rieles imitan a RailLine (resplandor, sombra, riel de arcilla,
línea de luz) y las luces que corren por las rutas activas.

Uso: python3 tools/art-preview/trafico.py <raw> [--out docs/previews]  ->  trafico.png
"""
import argparse
import math
import os

from PIL import Image, ImageDraw, ImageFilter, ImageFont

from juegos import INK, W, H, ROOT, FB, load, night, put, glow, clay_text, hexc
from piloto import hud

LIME, SUN, CORAL, SKY, GRAPE, CREAM = hexc(0x9BE564), hexc(0xFFC93C), hexc(0xFF6B4A), hexc(0x4CC9F0), hexc(0xB8A4FF), (255, 248, 236)
IDLE = hexc(0x4B4F9A)
COLORS = [0xFF6B4A, 0xFFC93C, 0x4CC9F0, 0x9BE564, 0xB8A4FF, 0xFF7BC0, 0x5FD68A, 0xFF8A3D]
FX, FY, FW, FH = 20, 115, 500, 825
K = 0.5  # medio canvas


def load_net(raw, name):
    nodes, paths = [], {}
    for line in open(f'{raw}/traffic_net_{name}.txt'):
        t = line.split()
        if t[0] == 'N':
            nodes.append((float(t[1]), float(t[2]), int(t[3]), int(t[4])))
        else:
            v = list(map(float, t[2:]))
            paths[int(t[1])] = [ui((v[i], v[i + 1])) for i in range(0, len(v), 2)]
    children = {i: [] for i in range(len(nodes))}
    for i, (_, _, p, _) in enumerate(nodes):
        if p >= 0:
            children[p].append(i)
    return nodes, children, paths


def ui(n):
    return FX + n[0] * FW, FY + n[1] * FH


def base(seed, level, points, streak):
    im = night(seed)
    d = ImageDraw.Draw(im)
    hud(im, d, level, points, streak, 'Tráfico Estelar')
    d = ImageDraw.Draw(im)
    d.rounded_rectangle((30, 98, W - 30, 106), 4, fill=(255, 255, 255, 30))
    d.rounded_rectangle((30, 98, 30 + (W - 60) * 0.55, 106), 4, fill=LIME)
    return im


def active_edges(children, states):
    act = set()
    for n, ch in children.items():
        for idx, c in enumerate(ch):
            if len(ch) != 2 or states.get(n, 0) == idx:
                act.add(c)
    return act


def along(pts, dist):
    """Punto a una distancia (px) desde el inicio de la polilínea."""
    for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
        seg = math.hypot(x1 - x0, y1 - y0)
        if dist <= seg and seg > 0:
            t = dist / seg
            return x0 + (x1 - x0) * t, y0 + (y1 - y0) * t
        dist -= seg
    return pts[-1]


def length(pts):
    return sum(math.hypot(b[0] - a[0], b[1] - a[1]) for a, b in zip(pts, pts[1:]))


def network(im, raw, nodes, children, paths, states, phase=0.0):
    act = active_edges(children, states)
    # Resplandor de las rutas activas.
    halo = Image.new('RGBA', im.size, (0, 0, 0, 0))
    hd = ImageDraw.Draw(halo)
    for n in act:
        hd.line(paths[n], fill=SKY + (120,), width=int(70 * K), joint='curve')
    im.alpha_composite(halo.filter(ImageFilter.GaussianBlur(7)))
    layer = Image.new('RGBA', im.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    w = int(30 * K)
    for n, pts in paths.items():  # sombras
        d.line([(x, y + 3.5) for x, y in pts], fill=INK + (230 if n in act else 140,), width=w, joint='curve')
    for n, pts in paths.items():  # borde tinta del riel
        d.line(pts, fill=INK + (255,), width=w, joint='curve')
    for n, pts in paths.items():  # cuerpo del riel
        d.line(pts, fill=(SKY if n in act else IDLE) + (255,), width=int(w * 0.56), joint='curve')
    for n in act:  # línea de luz
        d.line(paths[n], fill=(255, 255, 255, 128), width=3, joint='curve')
    im.alpha_composite(layer)
    # Luces que corren por las rutas activas.
    dots = ImageDraw.Draw(im)
    for n in act:
        pts = paths[n]
        L = length(pts)
        s = phase * 40
        while s < L:
            x, y = along(pts, s)
            dots.ellipse((x - 3, y - 3, x + 3, y + 3), fill=CREAM + (242,))
            s += 40
    # Estación (la compuerta queda sobre el inicio de la ruta).
    sx, sy = ui(nodes[0])
    glow(im, sx, sy - 22, 90, GRAPE, 90)
    glow(im, sx, sy - 22 - 0.72 / 2.24 * 110, 22, CORAL, 150)
    put(im, load(f'{raw}/traffic_station.raw'), sx, sy - 0.2 * 110, 110)
    knob = load(f'{raw}/traffic_knob.raw')
    for n, ch in children.items():
        x, y = ui(nodes[n])
        if len(ch) == 2:
            c = ch[states.get(n, 0)]
            cx, cy = along(paths[c], min(22, length(paths[c]) / 2))
            ang = math.degrees(math.atan2(-(cy - y), cx - x)) - 90
            k = knob.resize((64, 64), Image.LANCZOS).rotate(ang, resample=Image.BICUBIC, expand=True)
            im.alpha_composite(k, (int(x - k.width / 2), int(y - k.height / 2)))
        elif len(ch) == 0:
            put(im, load(f'{raw}/traffic_port_{nodes[n][3]}.raw'), x, y, 75)


def pod(im, raw, paths, edge, t, color):
    pts = paths[edge]
    L = length(pts)
    x, y = along(pts, L * t)
    tint = hexc(COLORS[color])
    trail = Image.new('RGBA', im.size, (0, 0, 0, 0))
    td = ImageDraw.Draw(trail)
    for i in range(3):
        bx, by = along(pts, max(0.0, L * t - 13 * (i + 1)))
        r = (30 - 7 * i) * K / 2 + 1
        td.ellipse((bx - r, by - r, bx + r, by + r), fill=tint + (int(255 * (0.5 - 0.14 * i)),))
    im.alpha_composite(trail)
    glow(im, x, y, 30, tint, 110)
    put(im, load(f'{raw}/traffic_pod_{color}.raw'), x, y, 43)


def port_of(nodes, color):
    return next(i for i, n in enumerate(nodes) if n[3] == color)


def states_for(children, seed):
    st = {}
    for i, (n, c) in enumerate(children.items()):
        if len(c) == 2:
            st[n] = (i + seed) % 2
    return st


def route_to(nodes, children, port):
    """Estados de los desvíos que llevan del portal al puerto."""
    parent = {c: n for n, ch in children.items() for c in ch}
    st, n = {}, port
    while n in parent:
        p = parent[n]
        if len(children[p]) == 2:
            st[p] = children[p].index(n)
        n = p
    return st


def toast(im, text, sub, color):
    d = ImageDraw.Draw(im)
    w = 190 if sub else 150
    h = 28 if sub else 22
    y = 150
    d.rounded_rectangle((W / 2 - w, y - h, W / 2 + w, y + h), 24, fill=(0x1B, 0x24, 0x66, 255), outline=color, width=3)
    if sub:
        clay_text(d, (W / 2, y - 8), text, 17)
        d.text((W / 2, y + 14), sub, fill=(215, 220, 245), anchor='mm', font=ImageFont.truetype(FB, 14))
    else:
        clay_text(d, (W / 2, y), text, 17)


def frame_intro(raw):
    nodes, ch, paths = load_net(raw, '4_3')
    im = base(81, 3, 420, 2)
    network(im, raw, nodes, ch, paths, {}, 0.3)
    pod(im, raw, paths, ch[0][0], 0.35, nodes[port_of(nodes, 1)][3])
    toast(im, 'Lleva cada cápsula a su planeta', 'Toca los desvíos para cambiar la ruta', SKY)
    return im


def frame_busy(raw):
    nodes, ch, paths = load_net(raw, '8_5')
    im = base(82, 11, 5320, 7)
    st = states_for(ch, 1)
    network(im, raw, nodes, ch, paths, st, 0.6)
    edges = sorted(paths)
    trunk = ch[0][0]
    pod(im, raw, paths, trunk, 0.22, 2)
    pod(im, raw, paths, trunk, 0.62, 5)
    for k, e in enumerate([e for e in edges if e != trunk][1:12:3]):
        pod(im, raw, paths, e, 0.4 + 0.12 * (k % 3), (k * 3) % 8)
    return im


def frame_deliver(raw):
    nodes, ch, paths = load_net(raw, '6_11')
    im = base(83, 7, 2480, 5)
    target = port_of(nodes, 3)
    st = route_to(nodes, ch, port_of(nodes, 0))
    network(im, raw, nodes, ch, paths, st, 0.1)
    x, y = ui(nodes[target])
    glow(im, x, y, 70, LIME, 160)
    layer = Image.new('RGBA', im.size, (0, 0, 0, 0))
    ImageDraw.Draw(layer).ellipse((x - 62, y - 62, x + 62, y + 62), outline=LIME + (220,), width=5)
    im.alpha_composite(layer)
    put(im, load(f'{raw}/traffic_port_3.raw'), x, y, 90)
    d = ImageDraw.Draw(im)
    clay_text(d, (x, y - 70), '+140', 26, SUN)
    pod(im, raw, paths, ch[0][0], 0.7, 0)
    return im


def frame_wrong(raw):
    nodes, ch, paths = load_net(raw, '6_21')
    im = base(84, 7, 2480, 0)
    wrong = port_of(nodes, 1)
    st = route_to(nodes, ch, port_of(nodes, 4))
    network(im, raw, nodes, ch, paths, st, 0.8)
    x, y = ui(nodes[wrong])
    glow(im, x, y, 60, CORAL, 150)
    put(im, load(f'{raw}/traffic_port_1.raw'), x + 6, y, 75)
    put(im, load(f'{raw}/mark_cross.raw'), x + 28, y - 28, 40)
    pod(im, raw, paths, ch[0][0], 0.5, 4)
    toast(im, 'Mira el color y el símbolo', None, SUN)
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
