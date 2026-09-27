"""Maqueta de Constelación de Palabras (disposición según ConstellationGameController.Layout a medio canvas). La ronda es
real: las frases de la prueba de voz de Ricardo (28-sep) leídas por FluencyContract vía ArtPreview (const_round.txt:
palabra, constelación, lugar en ella y nombre del grupo). Las estrellas se ubican con el mismo criterio que el juego
(junto a la anterior si sigue su constelación; si no, en el lugar más libre del cielo).

Cuatro momentos: antes de empezar (micrófono), a mitad de la ronda (estrellas que nacen mientras se habla), el cielo
de la ronda con su resumen, y la ronda de letra escribiendo con el teclado.

Uso: python3 tools/art-preview/constelacion.py <raw> [--out docs/previews]  ->  constelacion.png
"""
import argparse
import math
import os
import random

from PIL import Image, ImageDraw, ImageFilter, ImageFont

from juegos import INK, W, H, ROOT, FB, load, put, glow, clay_text, hexc
from piloto import hud, clay_box

K = 0.5
SUN, LIME, CORAL, SKY, GRAPE, CREAM, PINK = (hexc(0xFFC93C), hexc(0x9BE564), hexc(0xFF6B4A), hexc(0x4CC9F0),
                                             hexc(0xB8A4FF), (255, 248, 236), hexc(0xFF7BC0))
COLORS = [SKY, SUN, LIME, PINK, GRAPE, CORAL, hexc(0x5FD68A), hexc(0xFF8A3D)]
DEEP = (0x1B, 0x24, 0x66)
TOP = 960.0
Y0 = TOP - (190 + 40)
SKY_TOP, SKY_BOTTOM = Y0 - 230, -960 + 470
MIC_Y = -960 + 230
HALF_W = 540 - 110


def font(size):
    return ImageFont.truetype(FB, size)


def m(x, y):
    return W / 2 + x * K, H / 2 - y * K


def load_round(raw):
    words, summary = [], None
    for line in open(f'{raw}/const_round.txt', encoding='utf-8'):
        t = line.rstrip('\n').split('|')
        if t[0] == 'W':
            words.append({'text': t[1], 'color': int(t[2]), 'step': int(t[3]), 'group': t[4]})
        elif t[0] == 'S':
            summary = t[1:]
    return words, summary


def place(words, seed):
    """El mismo criterio que PlaceFree / PlaceNear del juego."""
    rng = random.Random(seed)
    pos = []

    def room(p):
        best = 1e9
        for q in pos:
            dx, dy = p[0] - q[0], p[1] - q[1]
            best = min(best, math.hypot(dx * 0.8, dy * 1.5))
        return best

    def free():
        best, br = (0, 0), -1
        for _ in range(48):
            p = (-HALF_W + rng.random() * 2 * HALF_W, SKY_BOTTOM + 50 + rng.random() * (SKY_TOP - 70 - SKY_BOTTOM - 50))
            r = 1000 - math.hypot(*p) * 0.3 if not pos else room(p)
            if r > br:
                br, best = r, p
        return best

    for w in words:
        if w['step'] > 0:
            f = pos[-1]
            best, br = None, -1
            for _ in range(24):
                a = rng.random() * math.tau
                r = 135 + rng.random() * 50
                p = (f[0] + math.cos(a) * r, f[1] + math.sin(a) * r)
                if abs(p[0]) > HALF_W or p[1] > SKY_TOP - 70 or p[1] < SKY_BOTTOM + 50:
                    continue
                rr = min(room(p), 170)
                if rr > br:
                    br, best = rr, p
            pos.append(best if best is not None and br >= 70 else free())
        else:
            pos.append(free())
    return pos


def base(level, points, round_label, title, prompt, timer):
    im = Image.new('RGBA', (W, H))
    d = ImageDraw.Draw(im)
    top, mid, bot = (0x10, 0x1A, 0x58), (0x08, 0x0E, 0x3A), (0x04, 0x06, 0x1C)
    for y in range(H):
        t = y / (H - 1)
        a, b, k = (top, mid, t / 0.5) if t < 0.5 else (mid, bot, (t - 0.5) / 0.5)
        d.line([(0, y), (W, y)], fill=tuple(int(a[i] + (b[i] - a[i]) * k) for i in range(3)) + (255,))
    rng = random.Random(4)
    for _ in range(30):
        x, y, r = rng.random() * W, rng.random() * H, rng.choice([1, 1, 1.5])
        d.ellipse((x - r, y - r, x + r, y + r), fill=(255, 255, 255, rng.randint(70, 170)))
    glow(im, W * 0.25, H * 0.3, 300, GRAPE, 40)
    glow(im, W * 0.8, H * 0.65, 280, SKY, 30)
    d = ImageDraw.Draw(im)
    hud(im, d, level, points, 0, 'Constelación de Palabras')
    d = ImageDraw.Draw(im)
    # Barra de tiempo.
    d.rounded_rectangle((30, 97, W - 30, 105), 4, fill=(255, 255, 255, 30))
    col = LIME if timer > 0.5 else SUN if timer > 0.25 else CORAL
    d.rounded_rectangle((30, 97, 30 + (W - 60) * timer, 105), 4, fill=col + (255,))
    x, y = m(0, Y0 - 25)
    d.text((x, y), round_label, fill=(255, 255, 255, 190), anchor='mm', font=font(18))
    clay_text(d, m(0, Y0 - 100), title, 46, SUN)
    d.text(m(0, Y0 - 180), prompt, fill=CREAM, anchor='mm', font=font(20), stroke_width=2, stroke_fill=INK)
    return im


def mic(im, raw, listening, pulse=0.3):
    x, y = m(0, MIC_Y)
    layer = Image.new('RGBA', im.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    for i, k in enumerate([pulse, (pulse + 0.33) % 1, (pulse + 0.66) % 1]):
        r = (100 * 1.05 + 100 * 0.85 * k) * K * 1.0
        col = CORAL if listening else SKY
        d.ellipse((x - r, y - r, x + r, y + r), outline=col + (int(255 * (1 - k) * 0.55),), width=5)
    im.alpha_composite(layer)
    d = ImageDraw.Draw(im)
    d.ellipse((x - 50, y - 50 + 5, x + 50, y + 50 + 5), fill=INK)
    d.ellipse((x - 50, y - 50, x + 50, y + 50), fill=DEEP + (255,), outline=INK, width=4)
    put(im, load(f'{raw}/const_mic.raw'), x, y, 78)


def stars(im, raw, words, pos, count, live=0, names=True):
    layer = Image.new('RGBA', im.size, (0, 0, 0, 0))
    glow_l = Image.new('RGBA', im.size, (0, 0, 0, 0))
    # Constelaciones (líneas detrás), solo entre las estrellas ya nacidas.
    groups = {}
    for i in range(count):
        c = words[i]['color']
        if c >= 0:
            groups.setdefault(c, []).append(i)
    for c, idx in groups.items():
        if len(idx) < 2:
            continue
        pts = [m(*pos[i]) for i in idx]
        ImageDraw.Draw(glow_l).line(pts, fill=COLORS[c % 8] + (110,), width=14, joint='curve')
        ImageDraw.Draw(layer).line(pts, fill=COLORS[c % 8] + (200,), width=3, joint='curve')
    im.alpha_composite(glow_l.filter(ImageFilter.GaussianBlur(5)))
    im.alpha_composite(layer)
    for i in range(count):
        w = words[i]
        is_live = i >= count - live
        c = w['color']
        in_group = c >= 0 and len(groups.get(c, [])) >= 2
        spr = load(f"{raw}/const_star_{c % 8}.raw") if in_group else load(f'{raw}/const_star_neutral.raw')
        if is_live:
            spr.putalpha(spr.getchannel('A').point(lambda v: int(v * 0.7)))
        x, y = m(*pos[i])
        put(im, spr, x, y, 70 * K)
        d = ImageDraw.Draw(im)
        d.text((x, y + 19), w['text'], fill=(CREAM if not is_live else (255, 255, 255)) + ((255,) if not is_live else (180,)),
               anchor='mt', font=font(17), stroke_width=2, stroke_fill=INK)
    if names:
        d = ImageDraw.Draw(im)
        for c, idx in groups.items():
            if len(idx) < 2:
                continue
            x, y = m(*max((pos[i] for i in idx), key=lambda p: p[1]))  # sobre la estrella más alta
            d.text((x, y - 18 - 13), words[idx[0]]['group'], fill=COLORS[c % 8], anchor='mb', font=font(16),
                   stroke_width=2, stroke_fill=INK)


def text_button(im, label):
    d = ImageDraw.Draw(im)
    x, y = m(0, -960 + 70)
    d.text((x, y), label, fill=SKY, anchor='mm', font=font(21), stroke_width=1, stroke_fill=INK)
    d.line((x - 82, y + 15, x + 82, y + 15), fill=SKY + (180,), width=2)


def toast(im, title, sub, color):
    d = ImageDraw.Draw(im)
    top, h, w = 4, 126, 225
    d.rounded_rectangle((W / 2 - w, top, W / 2 + w, top + h), 40, fill=DEEP + (255,), outline=INK, width=4)
    d.ellipse((W / 2 - w + 14, top + h / 2 - 8, W / 2 - w + 30, top + h / 2 + 8), fill=color)
    clay_text(d, (W / 2, top + h / 2 - 16), title, 22)
    d.text((W / 2, top + h / 2 + 20), sub, fill=(215, 220, 245), anchor='mm', font=font(16))


# ---------------------------------------------------------------- momentos

def frame_intro(raw):
    im = base(1, 0, 'Ronda 1 de 2', 'Animales', 'Di todos los animales que puedas', 1.0)
    mic(im, raw, listening=False)
    d = ImageDraw.Draw(im)
    d.text(m(0, MIC_Y + 170), 'Toca el micrófono y di en voz alta todas las que puedas', fill=(255, 255, 255),
           anchor='mm', font=font(18), stroke_width=2, stroke_fill=INK)
    text_button(im, 'Prefiero escribir')
    toast(im, 'Un minuto', 'No es un examen: di lo que se te ocurra', SKY)
    return im


def frame_live(raw, words, pos):
    count = 16
    im = base(1, 260, 'Ronda 1 de 2', 'Animales', 'Di todos los animales que puedas', 0.37)
    stars(im, raw, words, pos, count, live=1)
    mic(im, raw, listening=True, pulse=0.15)
    d = ImageDraw.Draw(im)
    d.text(m(0, MIC_Y + 170), '…tiburón tiburón blanco tiburón ballena', fill=(255, 255, 255, 205), anchor='mm', font=font(20))
    return im


def frame_reveal(raw, words, pos, summary):
    im = base(1, 470, 'Ronda 1 de 2', 'Animales', 'Di todos los animales que puedas', 0.0)
    stars(im, raw, words, pos, len(words))
    n, mean, switches = summary[0], summary[1], summary[2]
    constellations = len({w['color'] for w in words if w['color'] >= 0})
    clay_text(ImageDraw.Draw(im), m(0, MIC_Y + 110), f'{n} estrellas · {constellations} constelaciones · {switches} saltos', 24, SUN)
    toast(im, '¡Tu constelación más grande!', 'felinos: 5 estrellas', SUN)
    return im


def frame_keyboard(raw):
    words = [{'text': t, 'color': c, 'step': s, 'group': g} for t, c, s, g in [
        ('pato', 0, 0, 'empiezan con «pa»'), ('pala', 0, 1, ''), ('pan', 0, 2, ''), ('pino', -1, 0, ''),
        ('perro', 1, 0, 'empiezan con «pe»'), ('pera', 1, 1, ''), ('pelota', 1, 2, ''), ('puma', -1, 0, ''),
        ('plato', -1, 0, '')]]
    pos = place(words, 11)
    im = base(1, 690, 'Ronda 2 de 2', 'Palabras con P', 'Palabras que empiecen con P · sin nombres propios', 0.55)
    stars(im, raw, words, pos, len(words))
    # Teclado: campo de arcilla crema + "Agregar".
    x, y = m(-150, MIC_Y)
    clay_box(im, (x - 155, y - 32, x + 155, y + 32), 24, CREAM)
    d = ImageDraw.Draw(im)
    d.text((x - 138, y), 'papa', fill=INK, anchor='lm', font=font(25))
    bx, by = m(330, MIC_Y)
    clay_box(im, (bx - 75, by - 32, bx + 75, by + 32), 24, LIME)
    d = ImageDraw.Draw(im)
    d.text((bx, by), 'Agregar', fill=INK, anchor='mm', font=font(26))
    return im


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('raw')
    ap.add_argument('--out', default=ROOT + '/docs/previews')
    a = ap.parse_args()
    words, summary = load_round(a.raw)
    pos = place(words, 7)
    panels = [frame_intro(a.raw), frame_live(a.raw, words, pos), frame_reveal(a.raw, words, pos, summary), frame_keyboard(a.raw)]
    gap = 24
    sheet = Image.new('RGBA', (len(panels) * W + (len(panels) + 1) * gap, H + 2 * gap), (0x02, 0x03, 0x10, 255))
    for i, p in enumerate(panels):
        sheet.alpha_composite(p, (gap + i * (W + gap), gap))
    os.makedirs(a.out, exist_ok=True)
    sheet.convert('RGB').save(os.path.join(a.out, 'constelacion.png'))
    print('OK')


if __name__ == '__main__':
    main()
