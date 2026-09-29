"""Maqueta de Radar / Rescate relámpago (disposición según RadarGameController.Layout a medio canvas; arte exacto:
radar, haz, interferencia, robot, baliza, aros de lugar, cascos y marcas salen de los .raw que vuelca ArtPreview; los
lugares usan RadarContract.Position: 8 direcciones x 2 anillos).

Uso: python3 tools/art-preview/radar.py <raw> [--out docs/previews]  ->  radar.png
"""
import argparse
import math
import os
import random

from PIL import Image, ImageDraw, ImageFont

from juegos import FB, INK, W, H, ROOT, load, night, put, glow, clay_text, hexc, tinted
from piloto import clay_box, hud

LIME, SUN, CORAL, CREAM, SKY = hexc(0x9BE564), hexc(0xFFC93C), hexc(0xFF6B4A), hexc(0xFFF8EC), hexc(0x4CC9F0)
RINGS = [0.46, 0.80]
SCOPE = 500                 # lado del radar (medio canvas)
CX, CY = W / 2, 166 + SCOPE / 2
GLASS = SCOPE / 2 * 0.86 / 1.06
ITEM = GLASS * 0.26
CW = 0.16 * GLASS


def slot_center(s):
    a = (s % 8) * math.pi / 4
    r = RINGS[s // 8]
    return CX + r * math.sin(a) * GLASS, CY - r * math.cos(a) * GLASS


def slot(s, seed):
    a = (s % 8) * math.pi / 4
    r = RINGS[s // 8]
    j = random.Random(s * 7919 + seed)
    jx, jy = (j.random() - 0.5) * 0.05, (j.random() - 0.5) * 0.05
    return CX + (r * math.sin(a) + jx) * GLASS, CY - (r * math.cos(a) + jy) * GLASS


def base(raw, seed, level, points, streak, prompt, prompt_col=(255, 255, 255), timer=0.7, rescued=3):
    im = night(seed)
    glow(im, W / 2, CY, 300, LIME, 22)
    d = ImageDraw.Draw(im)
    hud(im, d, level, points, streak, 'Radar')
    d = ImageDraw.Draw(im)
    d.rounded_rectangle((30, 98, W - 30, 106), 4, fill=(255, 255, 255, 30))
    d.rounded_rectangle((30, 98, 30 + (W - 60) * timer, 106), 4, fill=LIME if timer > 0.5 else SUN)
    clay_text(d, (W / 2, 138), prompt, 30, prompt_col)
    put(im, load(f'{raw}/radar_scope.raw'), CX, CY, SCOPE)
    # Fila de rescatados.
    d = ImageDraw.Draw(im)
    d.text((W / 2, 872), f'Rescatados: {rescued}', font=ImageFont.truetype(FB, 20), fill=(255, 255, 255, 190), anchor='mm')
    total = 10 * 40 + 9 * 6
    helmet = load(f'{raw}/sym_Helmet_1.raw')
    for i in range(10):
        x = W / 2 - total / 2 + 20 + i * 46
        if i < rescued:
            put(im, helmet, x, 916, 40)
        else:
            d.ellipse((x - 10, 906, x + 10, 926), fill=(255, 255, 255, 26))
    return im


def sweep(im, raw, angle):
    s = tinted(load(f'{raw}/radar_sweep.raw'), LIME)
    s.putalpha(s.getchannel('A').point(lambda v: v * 32 // 100))
    s = s.resize((int(GLASS * 2), int(GLASS * 2)), Image.LANCZOS).rotate(angle, resample=Image.BICUBIC)
    im.alpha_composite(s, (int(CX - s.width / 2), int(CY - s.height / 2)))


def hint(im, text):
    d = ImageDraw.Draw(im)
    d.text((W / 2, CY + SCOPE / 2 + 26), text, font=ImageFont.truetype(FB, 19), fill=(255, 255, 255, 205), anchor='mm')


def rescue_button(im):
    y = CY + SCOPE / 2 + 92
    layer = Image.new('RGBA', im.size, (0, 0, 0, 0))
    clay_box(layer, (W / 2 - 140, y - 35, W / 2 + 140, y + 35), 30, SUN)
    im.alpha_composite(layer)
    ImageDraw.Draw(im).text((W / 2, y), '¡RESCATAR!', font=ImageFont.truetype(FB, 30), fill=INK, anchor='mm')


def fixation(im, raw):
    d = ImageDraw.Draw(im)
    r = CW * 0.45
    d.ellipse((CX - r, CY - r, CX + r, CY + r), outline=SUN + (180,), width=3)


def stimuli(im, raw, targets, robots, seed):
    for s in targets:
        x, y = slot(s, seed)
        put(im, load(f'{raw}/sym_Helmet_1.raw'), x, y, ITEM)
    for s in robots:
        x, y = slot(s, seed)
        put(im, load(f'{raw}/radar_robot.raw'), x, y, ITEM * 1.25)


def slots(im, raw, beacons=(), skip=()):
    ring = tinted(load(f'{raw}/radar_slot.raw'), CREAM)
    ring.putalpha(ring.getchannel('A').point(lambda v: v // 2))
    for s in range(16):
        if s in skip:
            continue
        x, y = slot_center(s)
        if s in beacons:
            put(im, load(f'{raw}/radar_beacon.raw'), x, y, ITEM * 0.95)
        else:
            put(im, ring, x, y, ITEM * 1.2)


TARGETS, ROBOTS, SEED = [9, 3, 13, 6], [0], 5


def frame_wait(raw):
    """Atento: el haz gira; el destello llega sin aviso."""
    im = base(raw, 30, 6, 1480, 4, 'Atento al radar...')
    sweep(im, raw, 60)
    fixation(im, raw)
    hint(im, 'El destello llega en cualquier momento.')
    return im


def frame_flash(raw):
    """Nivel 6, el destello: 4 astronautas y un robot (desde el nivel 5)."""
    im = base(raw, 31, 6, 1480, 4, 'Atento al radar...')
    stimuli(im, raw, TARGETS, ROBOTS, SEED)
    return im


def frame_mask(raw):
    """La interferencia que borra la imagen."""
    im = base(raw, 32, 6, 1480, 4, 'Atento al radar...')
    m = load(f'{raw}/radar_mask_0.raw').resize((SCOPE, SCOPE), Image.LANCZOS).rotate(40, resample=Image.BICUBIC)
    im.alpha_composite(m, (int(CX - SCOPE / 2), int(CY - SCOPE / 2)))
    return im


def frame_answer(raw):
    """¿Dónde estaban los 4? Tres balizas puestas (una en un lugar vacío)."""
    im = base(raw, 33, 6, 1480, 4, '¿Dónde estaban los 4?')
    slots(im, raw, beacons={9, 3, 6, 10})
    hint(im, '4 de 4 balizas · toca de nuevo para sacar')
    rescue_button(im)
    return im


def frame_feedback(raw):
    """Revelación: 3 rescatados (✓), uno se escapó (aro sol), una baliza de más (✗) y el robot."""
    im = base(raw, 34, 6, 1780, 5, 'Rescataste 3 de 4', SUN, rescued=6)
    sweep(im, raw, 200)
    hits, missed, extra = [9, 3, 6], [13], [10]
    for s in hits:
        x, y = slot(s, SEED)
        glow(im, x, y, 44, LIME, 140)
        put(im, load(f'{raw}/sym_Helmet_1.raw'), x, y, ITEM)
        cx, cy = slot_center(s)
        put(im, load(f'{raw}/mark_check.raw'), cx + ITEM * 0.45, cy - ITEM * 0.45, ITEM * 0.6)
    for s in missed:
        x, y = slot(s, SEED)
        ring = tinted(load(f'{raw}/radar_slot.raw'), SUN)
        cx, cy = slot_center(s)
        put(im, ring, cx, cy, ITEM * 1.45)
        h = load(f'{raw}/sym_Helmet_1.raw')
        h.putalpha(h.getchannel('A').point(lambda v: v * 6 // 10))
        put(im, h, x, y, ITEM)
    for s in extra:
        cx, cy = slot_center(s)
        put(im, load(f'{raw}/radar_beacon.raw'), cx, cy, ITEM * 0.95)
        put(im, load(f'{raw}/mark_cross.raw'), cx + ITEM * 0.45, cy - ITEM * 0.45, ITEM * 0.6)
    for s in ROBOTS:
        x, y = slot(s, SEED)
        put(im, load(f'{raw}/radar_robot.raw'), x, y, ITEM * 1.25)
    d = ImageDraw.Draw(im)
    clay_text(d, (CX, CY - GLASS - 4), '+168', 26, SUN)
    hint(im, 'Uno se escapó (aro sol)')
    return im


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('raw')
    ap.add_argument('--out', default=ROOT + '/docs/previews')
    a = ap.parse_args()
    panels = [frame_wait(a.raw), frame_flash(a.raw), frame_mask(a.raw), frame_answer(a.raw), frame_feedback(a.raw)]
    gap = 24
    sheet = Image.new('RGBA', (len(panels) * W + (len(panels) + 1) * gap, H + 2 * gap), (0x02, 0x03, 0x10, 255))
    for i, p in enumerate(panels):
        sheet.alpha_composite(p, (gap + i * (W + gap), gap))
    os.makedirs(a.out, exist_ok=True)
    sheet.convert('RGB').save(os.path.join(a.out, 'radar.png'))
    print('OK')


if __name__ == '__main__':
    main()
