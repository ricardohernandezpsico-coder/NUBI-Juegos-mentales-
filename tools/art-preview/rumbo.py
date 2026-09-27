"""Maqueta de Rumbo a Casa (disposición según HomingGameController.Layout a medio canvas). El viaje es real: lo genera
HomingContract.NextTrip vía ArtPreview (homing_trips.txt, con una vuelta de ejemplo evaluada por HomingContract.Evaluate);
el arte sale de los .raw (nave, base, cristales, faro, flecha, dial, niebla, marcas).

Cuatro momentos: la ida (señal del próximo cristal en el borde de la vista), apuntar hacia casa, avanzar, y la vista
desde arriba con la ruta de ida (lima), la vuelta (sol) y la vuelta justa (crema).

Uso: python3 tools/art-preview/rumbo.py <raw> [--out docs/previews]  ->  rumbo.png
"""
import argparse
import math
import os
import random

from PIL import Image, ImageDraw, ImageFilter, ImageFont

from juegos import INK, W, H, ROOT, FB, load, put, glow, clay_text, hexc
from piloto import hud, clay_box

K = 0.5  # medio canvas
SUN, LIME, CORAL, SKY, GRAPE, CREAM, PINK = (hexc(0xFFC93C), hexc(0x9BE564), hexc(0xFF6B4A), hexc(0x4CC9F0),
                                             hexc(0xB8A4FF), (255, 248, 236), hexc(0xFF7BC0))
CRYSTAL_COLORS = [LIME, SKY, GRAPE, PINK, SUN]
DEEP = (0x1B, 0x24, 0x66)
PLAY_H = 1920.0
SHIP = (W / 2, H / 2 + 0.04 * PLAY_H * K)          # la nave, un poco abajo del centro
FOG_CLEAR, FOG_GONE = 380.0, 520.0


def font(size):
    return ImageFont.truetype(FB, size)


def load_trips(raw):
    trips, cur = [], None
    for line in open(f'{raw}/homing_trips.txt'):
        t = line.split()
        if t[0] == 'T':
            v = list(map(float, t[2:]))
            cur = {'level': int(t[1]), 'crystals': [(v[i], v[i + 1]) for i in range(0, len(v), 2)]}
        else:
            e = list(map(float, t[1:]))
            cur.update(end=(e[0], e[1]), arrival=e[2], turn=e[3], angle_err=e[4], dist_ratio=e[5], err_ratio=e[6],
                       beacon=e[7])
            trips.append(cur)
    return trips


def space(seed):
    """Espacio profundo: el degradé de la app SIN estrellas de fondo (serían una brújula) y nebulosas muy tenues."""
    top, mid, bot = (0x10, 0x1A, 0x58), (0x08, 0x0E, 0x3A), (0x04, 0x06, 0x1C)
    im = Image.new('RGBA', (W, H))
    d = ImageDraw.Draw(im)
    for y in range(H):
        t = y / (H - 1)
        a, b, k = (top, mid, t / 0.5) if t < 0.5 else (mid, bot, (t - 0.5) / 0.5)
        d.line([(0, y), (W, y)], fill=tuple(int(a[i] + (b[i] - a[i]) * k) for i in range(3)) + (255,))
    glow(im, W * 0.5, H * 0.25, 360, SKY, 26)
    glow(im, W * 0.5, H * 0.8, 340, GRAPE, 36)
    return im


def rot(x, y, deg):
    """Gira (x, y) en sentido antihorario (como Unity con Euler z positivo)."""
    r = math.radians(deg)
    return x * math.cos(r) - y * math.sin(r), x * math.sin(r) + y * math.cos(r)


def cockpit(px, py, heading):
    """Del mapa a la maqueta en la vista de cabina (la nave fija, el mundo girado por su rumbo)."""
    def f(wx, wy):
        vx, vy = rot(wx - px, wy - py, heading)
        return SHIP[0] + vx * K, SHIP[1] - vy * K
    return f


def visibility(d):
    k = min(1.0, max(0.0, (d - FOG_CLEAR) / (FOG_GONE - FOG_CLEAR)))
    return 1.0 - k * k * (3 - 2 * k)


def with_alpha(im, a):
    im = im.copy()
    im.putalpha(im.getchannel('A').point(lambda v: int(v * a)))
    return im


def dust(im, px, py, heading, seed):
    rng = random.Random(seed)
    proj = cockpit(px, py, heading)
    layer = Image.new('RGBA', im.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    for _ in range(150):
        wx = px + (rng.random() - 0.5) * 2600
        wy = py + (rng.random() - 0.5) * 2600
        x, y = proj(wx, wy)
        r = (5 + rng.random() * 9) * K / 2
        d.ellipse((x - r, y - r, x + r, y + r), fill=(217, 224, 255, int(255 * (0.25 + 0.45 * rng.random()))))
    im.alpha_composite(layer)


def fog(im, raw):
    put(im, load(f'{raw}/homing_fog.raw'), SHIP[0], SHIP[1], 3000 * K)


def ship(im, raw, flame=False):
    if flame:
        glow(im, SHIP[0], SHIP[1] + 170 * K * 0.5, 30, SUN, 200)
    put(im, load(f'{raw}/homing_ship.raw'), SHIP[0], SHIP[1], 170 * K)


def beacon(im, raw, bearing, heading):
    rel = math.radians(bearing - heading)
    x, y = SHIP[0] + math.sin(rel) * 465 * K, SHIP[1] - math.cos(rel) * 465 * K
    glow(im, x, y, 60, SUN, 110)
    put(im, load(f'{raw}/homing_beacon.raw'), x, y, 120 * K)
    d = ImageDraw.Draw(im)
    d.text((x, y + 36), 'faro', fill=SUN, anchor='mm', font=font(18), stroke_width=2, stroke_fill=INK)


def texts(im, prompt, detail='', hint='', prompt_col=(255, 255, 255)):
    d = ImageDraw.Draw(im)
    if prompt:
        clay_text(d, (W / 2, 142), prompt, 31, prompt_col)
    if detail:
        d.text((W / 2, 180), detail, fill=(255, 255, 255), anchor='mm', font=font(21), stroke_width=2, stroke_fill=INK)
    if hint:
        d.text((W / 2, 810), hint, fill=(255, 255, 255), anchor='mm', font=font(20), stroke_width=2, stroke_fill=INK)


def button(im, label, col):
    d = ImageDraw.Draw(im)
    box = (W / 2 - 150, 885 - 42, W / 2 + 150, 885 + 42)
    clay_box(im, box, 32, col)
    d = ImageDraw.Draw(im)
    d.text((W / 2, 885), label, fill=INK, anchor='mm', font=font(32))


def toast(im, title, sub, color):
    d = ImageDraw.Draw(im)
    top, h, w = 4, 126, 225
    d.rounded_rectangle((W / 2 - w, top, W / 2 + w, top + h), 40, fill=DEEP + (255,), outline=INK, width=4)
    d.ellipse((W / 2 - w + 14, top + h / 2 - 8, W / 2 - w + 30, top + h / 2 + 8), fill=color)
    clay_text(d, (W / 2, top + h / 2 - 16), title, 22)
    d.text((W / 2, top + h / 2 + 20), sub, fill=(215, 220, 245), anchor='mm', font=font(16))


def base_frame(level, points, streak):
    return space(7)


def top_hud(im, level, points, streak):
    """El marcador va por encima de todo (la niebla y el mapa quedan debajo)."""
    hud(im, ImageDraw.Draw(im), level, points, streak, 'Rumbo a Casa')


# ---------------------------------------------------------------- momentos

def frame_outbound(raw, trip):
    """Ida: en el primer cristal, la señal del segundo aparece en el borde de la vista (a la izquierda)."""
    px, py = trip['crystals'][0]
    heading = 0.0
    im = base_frame(trip['level'], 20, 0)
    dust(im, px, py, heading, 3)
    proj = cockpit(px, py, heading)
    # La base quedó atrás, en la niebla (ya no se ve); el cristal siguiente, lejos: solo su señal.
    fog(im, raw)
    beacon(im, raw, trip['beacon'], heading)
    nx, ny = trip['crystals'][1]
    rel = math.atan2(nx - px, ny - py) - math.radians(heading)
    sx, sy = SHIP[0] + math.sin(rel) * 420 * K, SHIP[1] - math.cos(rel) * 420 * K
    glow(im, sx, sy, 50, CRYSTAL_COLORS[1], 120)
    d = ImageDraw.Draw(im)
    d.ellipse((sx - 37, sy - 37, sx + 37, sy + 37), outline=CRYSTAL_COLORS[1] + (235,), width=7)
    put(im, load(f'{raw}/homing_crystal_1.raw'), sx, sy, 84 * K)
    ship(im, raw)
    top_hud(im, trip['level'], 20, 0)
    texts(im, 'Sigue las señales', hint='Toca la señal para ir al cristal')
    return im


def frame_aim(raw, trip):
    px, py = trip['crystals'][-1]
    heading = trip['arrival']
    im = base_frame(trip['level'], 40, 0)
    dust(im, px, py, heading, 5)
    fog(im, raw)
    beacon(im, raw, trip['beacon'], heading)
    put(im, with_alpha(load(f'{raw}/homing_dial.raw'), 0.55), SHIP[0], SHIP[1], 500 * K)
    # Flecha: cola sobre la nave, apuntando donde la persona cree que está casa.
    arrow = load(f'{raw}/homing_arrow.raw').resize((int(300 * K), int(300 * K)), Image.LANCZOS)
    tail = (-0.92 / 1.08 + 1) * 0.5
    big = Image.new('RGBA', (arrow.width * 2, arrow.height * 2), (0, 0, 0, 0))
    big.alpha_composite(arrow, (arrow.width // 2, int(arrow.height * (0.5 + tail)) - arrow.height // 2 - int(arrow.height * 0.0)))
    # En "big" la cola quedó en el centro; se gira alrededor del centro (sentido horario = negativo en PIL).
    big = big.rotate(-trip['turn'], resample=Image.BICUBIC)
    im.alpha_composite(big, (int(SHIP[0] - big.width / 2), int(SHIP[1] - big.height / 2)))
    ship(im, raw)
    top_hud(im, trip['level'], 40, 0)
    texts(im, '¿Hacia dónde está casa?', prompt_col=SUN)
    button(im, 'FIJAR RUMBO', LIME)
    toast(im, '¡Hora de volver!', 'Combustible justo para llegar a casa', SUN)
    return im


def frame_advance(raw, trip):
    sx, sy = trip['crystals'][-1]
    heading = trip['arrival'] + trip['turn']
    ex, ey = trip['end']
    px, py = sx + (ex - sx) * 0.55, sy + (ey - sy) * 0.55
    im = base_frame(trip['level'], 40, 0)
    dust(im, px, py, heading, 9)
    proj = cockpit(px, py, heading)
    # La base asoma apenas entre la niebla si ya está cerca (aquí todavía no).
    bd = math.hypot(px, py)
    if visibility(bd) > 0.02:
        bx, by = proj(0, 0)
        put(im, with_alpha(load(f'{raw}/homing_base.raw'), visibility(bd)), bx, by, 270 * K)
    fog(im, raw)
    beacon(im, raw, trip['beacon'], heading)
    ship(im, raw, flame=True)
    top_hud(im, trip['level'], 40, 0)
    texts(im, 'Avanza hacia casa', 'Toca ¡AQUÍ! donde creas que está la base', prompt_col=SUN)
    button(im, '¡AQUÍ!', SUN)
    return im


def line(layer, pts, width, rgba):
    d = ImageDraw.Draw(layer)
    d.line(pts, fill=rgba, width=int(width), joint='curve')
    r = width / 2
    for x, y in (pts[0], pts[-1]):
        d.ellipse((x - r, y - r, x + r, y + r), fill=rgba)


def frame_reveal(raw, trip):
    im = base_frame(trip['level'], 40 + 150 + 30, 1)
    pts = [(0.0, 0.0)] + trip['crystals']
    ex, ey = trip['end']
    xs = [p[0] for p in pts] + [ex]
    ys = [p[1] for p in pts] + [ey]
    minx, maxx, miny, maxy = min(xs), max(xs), min(ys), max(ys)
    fit = min((1080 - 2 * 130) / max(1, maxx - minx), (0.5 * PLAY_H) / max(1, maxy - miny), 0.6)
    cx, cy = (minx + maxx) / 2, (miny + maxy) / 2
    center = (W / 2, H / 2 + 0.02 * PLAY_H * K)

    def m(x, y):
        return center[0] + (x - cx) * fit * K, center[1] - (y - cy) * fit * K

    def icon(size, min_screen):
        return max(1.0, min_screen / (size * fit))

    layer = Image.new('RGBA', im.size, (0, 0, 0, 0))
    out = [m(*p) for p in pts]
    glow_layer = Image.new('RGBA', im.size, (0, 0, 0, 0))
    line(glow_layer, out, 44 * K, LIME + (90,))
    im.alpha_composite(glow_layer.filter(ImageFilter.GaussianBlur(6)))
    line(layer, out, 12 * K, LIME + (255,))
    s = m(*trip['crystals'][-1])
    line(layer, [s, m(0, 0)], 7 * K, CREAM + (140,))
    line(layer, [s, m(ex, ey)], 12 * K, SUN + (255,))
    line(layer, [m(ex, ey), m(0, 0)], 7 * K, LIME + (255,))
    im.alpha_composite(layer)
    bx, by = m(0, 0)
    glow(im, bx, by, 70, SUN, 150)
    put(im, load(f'{raw}/homing_base.raw'), bx, by, 270 * icon(270, 170) * fit * K)
    for i, c in enumerate(trip['crystals']):
        x, y = m(*c)
        put(im, load(f'{raw}/homing_crystal_{i}.raw'), x, y, 124 * icon(124, 120) * fit * K)
    x, y = m(ex, ey)
    heading = trip['arrival'] + trip['turn']
    shp = load(f'{raw}/homing_ship_flat.raw').rotate(-heading, resample=Image.BICUBIC)
    put(im, shp, x, y, 136 * icon(136, 110) * fit * K)
    hit = trip['err_ratio'] <= 0.35
    put(im, load(f'{raw}/mark_check.raw' if hit else f'{raw}/mark_cross.raw'), x + 22, y - 22, 110 * icon(110, 72) * fit * K)
    a = trip['angle_err']
    rumbo = 'Rumbo justo' if abs(a) < 5 else f"Rumbo: {round(abs(a))}° a la {'derecha' if a > 0 else 'izquierda'}"
    dr = trip['dist_ratio']
    dist = 'te quedaste corto' if dr < 0.9 else 'te pasaste' if dr > 1.1 else 'distancia justa'
    top_hud(im, trip['level'], 220, 1)
    texts(im, '¡Llegaste a casa!' if hit else 'Esta vez quedaste lejos', f'{rumbo} · {dist}', prompt_col=LIME if hit else SUN)
    d = ImageDraw.Draw(im)
    # Leyenda suelta (texto y líneas finas, sin recuadro).
    ly = 800
    for i, (col, txt) in enumerate([(LIME, 'tu ida'), (SUN, 'tu vuelta'), (CREAM, 'vuelta justa')]):
        x0 = 70 + i * 150
        d.line((x0, ly, x0 + 30, ly), fill=col + (255,), width=5)
        d.text((x0 + 40, ly), txt, fill=(235, 238, 255), anchor='lm', font=font(18))
    return im


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('raw')
    ap.add_argument('--out', default=ROOT + '/docs/previews')
    a = ap.parse_args()
    trips = load_trips(a.raw)
    t = trips[0]
    panels = [frame_outbound(a.raw, t), frame_aim(a.raw, t), frame_advance(a.raw, t), frame_reveal(a.raw, t)]
    gap = 24
    sheet = Image.new('RGBA', (len(panels) * W + (len(panels) + 1) * gap, H + 2 * gap), (0x02, 0x03, 0x10, 255))
    for i, p in enumerate(panels):
        sheet.alpha_composite(p, (gap + i * (W + gap), gap))
    os.makedirs(a.out, exist_ok=True)
    sheet.convert('RGB').save(os.path.join(a.out, 'rumbo.png'))
    print('OK')


if __name__ == '__main__':
    main()
