"""Maqueta de Satélites (disposición según SatelliteGameController.Layout a medio canvas; arte exacto: satélites,
anillos y marcas salen de los .raw que vuelca ArtPreview; el movimiento imita SatelliteSwarm: velocidad constante,
rumbo que deriva, rebotes).

Uso: python3 tools/art-preview/satelites.py <raw> [--out docs/previews]  ->  satelites.png
"""
import argparse
import math
import os
import random

from PIL import Image, ImageDraw, ImageFont

from juegos import FB, W, H, ROOT, load, night, put, glow, clay_text, hexc
from piloto import hud

LIME, SUN, CORAL, SKY = hexc(0x9BE564), hexc(0xFFC93C), hexc(0xFF6B4A), hexc(0x4CC9F0)
AW, AH = 490, 717
AX, AY = W / 2, 168 + AH / 2
R = 0.06
FIELD_H = AH / AW
SAT = R * 2 * AW * 1.25


def ui(x, y):
    return AX + (x - 0.5) * AW, AY - (y - FIELD_H / 2) * AW


def simulate(n, seconds, speed, seed):
    rng = random.Random(seed)
    pos = []
    for i in range(n):
        while True:
            p = [R + rng.random() * (1 - 2 * R), R + rng.random() * (FIELD_H - 2 * R), rng.random() * 6.283]
            if all(math.dist(p[:2], q[:2]) > R * 3.2 for q in pos):
                break
        pos.append(p)
    paths = [[(p[0], p[1])] for p in pos]
    dt = 1 / 60
    for step in range(int(seconds * 60)):
        for p in pos:
            p[2] += (rng.random() - 0.5) * 3.6 * dt
            p[0] += math.cos(p[2]) * speed * dt
            p[1] += math.sin(p[2]) * speed * dt
            if p[0] < R or p[0] > 1 - R:
                p[0] = min(max(p[0], R), 1 - R); p[2] = math.pi - p[2]
            if p[1] < R or p[1] > FIELD_H - R:
                p[1] = min(max(p[1], R), FIELD_H - R); p[2] = -p[2]
        for i in range(n):
            for j in range(i + 1, n):
                dx, dy = pos[j][0] - pos[i][0], pos[j][1] - pos[i][1]
                d = math.hypot(dx, dy)
                if 0 < d < R * 2.1:
                    nx, ny = dx / d, dy / d
                    push = (R * 2.1 - d) / 2
                    pos[i][0] -= nx * push; pos[i][1] -= ny * push
                    pos[j][0] += nx * push; pos[j][1] += ny * push
                    pos[i][2] = math.atan2(-ny, -nx) + (rng.random() - 0.5)
                    pos[j][2] = math.atan2(ny, nx) + (rng.random() - 0.5)
        if step % 6 == 0:
            for i, p in enumerate(pos):
                paths[i].append((p[0], p[1]))
    return pos, paths


def base(raw, seed, level, points, streak, prompt, prompt_col=(255, 255, 255), timer=0.6, picks=0, targets=3):
    im = night(seed)
    d = ImageDraw.Draw(im)
    hud(im, d, level, points, streak, 'Satélites')
    d = ImageDraw.Draw(im)
    d.rounded_rectangle((30, 98, W - 30, 106), 4, fill=(255, 255, 255, 30))
    d.rounded_rectangle((30, 98, 30 + (W - 60) * timer, 106), 4, fill=LIME if timer > 0.5 else SUN)
    clay_text(d, (W / 2, 138), prompt, 30, prompt_col)
    layer = Image.new('RGBA', im.size, (0, 0, 0, 0))
    ImageDraw.Draw(layer).rounded_rectangle((AX - AW / 2, AY - AH / 2, AX + AW / 2, AY + AH / 2), 32,
                                            fill=(255, 255, 255, 9), outline=SKY + (64,), width=1)
    im.alpha_composite(layer)
    d = ImageDraw.Draw(im)
    for i in range(targets):
        x = W / 2 + (i - (targets - 1) / 2) * 29
        d.ellipse((x - 9.5, 922 - 9.5, x + 9.5, 922 + 9.5), fill=SKY if i < picks else (255, 255, 255, 40))
    return im


def sat(im, raw, x, y, alpha=1.0, angle=5):
    layer = Image.new('RGBA', im.size, (0, 0, 0, 0))
    hr = R * AW * 1.1
    ImageDraw.Draw(layer).ellipse((x - hr, y - hr, x + hr, y + hr), fill=SKY + (int(33 * alpha),))
    im.alpha_composite(layer)
    s = load(f'{raw}/sym_Satellite_0.raw').resize((int(SAT), int(SAT)), Image.LANCZOS).rotate(angle, resample=Image.BICUBIC)
    if alpha < 1:
        s.putalpha(s.getchannel('A').point(lambda v: int(v * alpha)))
    im.alpha_composite(s, (int(x - s.width / 2), int(y - s.height / 2)))


def ring(im, x, y, col, scale=1.0, width=4):
    r = R * AW * 1.35 * scale
    layer = Image.new('RGBA', im.size, (0, 0, 0, 0))
    ImageDraw.Draw(layer).ellipse((x - r, y - r, x + r, y + r), outline=col + (255,), width=width)
    im.alpha_composite(layer)


def mark(im, raw, x, y, kind):
    put(im, load(f'{raw}/mark_{kind}.raw'), x + SAT * 0.38, y - SAT * 0.38, SAT * 0.5)


TARGETS = {1, 4, 6}


def frame_cue(raw, start):
    im = base(raw, 41, 3, 620, 2, 'Memoriza los 3 que brillan')
    for i, p in enumerate(start):
        x, y = ui(p[0], p[1])
        if i in TARGETS:
            glow(im, x, y, SAT * 0.9, SUN, 190)
            ring(im, x, y, SUN, 1.08)
        sat(im, raw, x, y)
    return im


def frame_track(raw, mid):
    im = base(raw, 42, 3, 620, 2, '¡Síguelos con la vista!', SUN, timer=0.55)
    for p in mid:
        x, y = ui(p[0], p[1])
        sat(im, raw, x, y, angle=-6)
    return im


def frame_answer(raw, end):
    im = base(raw, 43, 3, 620, 2, 'Toca los 3 que brillaban', picks=2, timer=0.5)
    for i, p in enumerate(end):
        x, y = ui(p[0], p[1])
        sat(im, raw, x, y, angle=2)
        if i in (1, 3):
            ring(im, x, y, SKY)
    return im


def frame_reveal(raw, end, paths):
    im = base(raw, 44, 3, 700, 0, '2 de 3', SUN, picks=3, timer=0.46)
    layer = Image.new('RGBA', im.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    for i in TARGETS:
        pts = paths[i]
        for k in range(0, len(pts), 2):
            age = k / max(1, len(pts) - 1)
            x, y = ui(*pts[k])
            r = (3 + 3.5 * age)
            d.ellipse((x - r, y - r, x + r, y + r), fill=SUN + (int(255 * (0.18 + 0.42 * age)),))
    im.alpha_composite(layer)
    picked = {1, 4, 3}
    for i, p in enumerate(end):
        x, y = ui(p[0], p[1])
        if i in picked and i in TARGETS:
            sat(im, raw, x, y); ring(im, x, y, LIME); mark(im, raw, x, y, 'check')
        elif i in picked:
            sat(im, raw, x, y, 0.55); ring(im, x, y, CORAL); mark(im, raw, x, y, 'cross')
        elif i in TARGETS:
            glow(im, x, y, SAT * 0.9, SUN, 170); sat(im, raw, x, y); ring(im, x, y, SUN, 1.05)
        else:
            sat(im, raw, x, y, 0.45)
    # "¿Aquí se cruzaron?": el punto en que el que faltó (6) pasó más cerca del elegido por error (3).
    a, b = paths[6], paths[3]
    k = min(range(min(len(a), len(b))), key=lambda i: math.dist(a[i], b[i]))
    mx, my = ui((a[k][0] + b[k][0]) / 2, (a[k][1] + b[k][1]) / 2)
    ring(im, mx, my, CORAL, 1.3, 5)
    d = ImageDraw.Draw(im)
    clay_text(d, (mx, my + 60), '¿Aquí se cruzaron?', 19)
    clay_text(d, (W / 2, 900), '+180', 28, SUN)
    return im


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('raw')
    ap.add_argument('--out', default=ROOT + '/docs/previews')
    a = ap.parse_args()
    start, _ = simulate(7, 0, 0.19, 5)
    start = [p[:] for p in start]
    mid, _ = simulate(7, 2.5, 0.19, 5)
    end, paths = simulate(7, 5.0, 0.19, 5)
    panels = [frame_cue(a.raw, start), frame_track(a.raw, mid), frame_answer(a.raw, end), frame_reveal(a.raw, end, paths)]
    gap = 24
    sheet = Image.new('RGBA', (len(panels) * W + (len(panels) + 1) * gap, H + 2 * gap), (0x02, 0x03, 0x10, 255))
    for i, p in enumerate(panels):
        sheet.alpha_composite(p, (gap + i * (W + gap), gap))
    os.makedirs(a.out, exist_ok=True)
    sheet.convert('RGB').save(os.path.join(a.out, 'satelites.png'))
    print('OK')


if __name__ == '__main__':
    main()
