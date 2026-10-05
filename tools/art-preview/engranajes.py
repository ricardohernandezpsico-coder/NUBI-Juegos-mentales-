"""Lámina de «Engranajes: Taller de reparación» (5-oct-2026): réplica de la composición de la pantalla con los sprites REALES horneados (tools/art-preview) y las máquinas de muestra del generador real.

Uso:  python3 tools/art-preview/engranajes.py <raw> [--out docs/previews]
      (<raw> = la carpeta que vuelca ArtPreview: `dotnet run --project tools/art-preview -- <raw>`)
Genera docs/previews/engranajes.png con tres pantallas de 360 × 740 dp: etapa 3 recién armada (los carteles, la consigna, la cuenta de cambios y «Arrancar»), etapa 7 después de un cambio
equivocado (carteles verde y coral, en celeste lo que movió el cambio, en dorado lo que había que tocar y el aviso de dos líneas) y etapa 10 con sus dos cambios hechos y todo en verde.
Réplica de la disposición de EngranajesLayout + EngranajesGameController (ver el código Unity); no es una captura del juego.
"""
import argparse
import math
import os
import struct

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
FB = ROOT + '/unity/NeuroVidaCore/Assets/Resources/Fonts/Fredoka-Bold.ttf'
FS = ROOT + '/unity/NeuroVidaCore/Assets/Resources/Fonts/Fredoka-SemiBold.ttf'
S = 2            # píxeles por dp
W, H = 360, 740
GOLD = (255, 201, 74)
INK = (0x1A, 0x12, 0x40)
CYAN = (127, 216, 255)
MINT = (159, 245, 214)
CORAL = (255, 140, 107)
LAV = (171, 165, 210)
TEXT = (237, 234, 251)
BELT_LIGHT = (201, 193, 255)

NAMES = ['Antena', 'Compuerta', 'Carga', 'Turbina']
PART = [(306, 110), (312, 250), (312, 316), (306, 442)]
CARTEL_Y = [142, 294, 360, 398]
ACT_WORD = ['reloj', 'al revés', 'sube', 'baja', 'se abre', 'se cierra']    # Cw Ccw Up Down Open Close


def load(raw, name):
    b = open(os.path.join(raw, name + '.raw'), 'rb').read()
    n = struct.unpack('<i', b[:4])[0]
    if n < 0:
        w = -n
        h = struct.unpack('<i', b[4:8])[0]
        return Image.frombytes('RGBA', (w, h), b[8:8 + w * h * 4]).transpose(Image.FLIP_TOP_BOTTOM)
    return Image.frombytes('RGBA', (n, n), b[4:4 + n * n * 4]).transpose(Image.FLIP_TOP_BOTTOM)


def font(size, bold=True):
    return ImageFont.truetype(FB if bold else FS, int(size * S))


def compute(h):
    """Port de EngranajesLayout.Compute."""
    strip_h, q_h = 42.0, 48.0
    scene_h = 384.0
    button_h, say_h = 88.0, 84.0
    fixed_top = 66 + 2 + strip_h + 4 + q_h + 6
    fixed_bottom = 8 + button_h + 6 + say_h + 6
    avail = h - fixed_top - fixed_bottom
    scale = min(0.96, avail / scene_h)
    if scale < 0.55:
        button_h, say_h = 80.0, 72.0
        fixed_bottom = 8 + button_h + 6 + say_h + 6
        avail = h - fixed_top - fixed_bottom
        scale = max(0.55, min(0.96, avail / scene_h))
    scene = scene_h * scale
    free = max(0.0, avail - scene)
    grow = min(14.0, free * 0.4)
    button_h += grow
    free -= grow
    m = {'strip_top': 68.0}
    m['q_top'] = m['strip_top'] + strip_h + 4
    scene_top = m['q_top'] + q_h + 6 + free * 0.4
    m['scale'] = scale
    m['cy'] = scene_top + scene / 2
    m['btn_h'] = button_h
    m['btn_top'] = scene_top + scene + 8 + free * 0.3
    m['say_top'] = m['btn_top'] + button_h + 6
    m['say_h'] = say_h
    return m


def tinted(sp, color, alpha=1.0):
    r, g, b, a = sp.split()
    out = Image.merge('RGBA', (r.point(lambda _: color[0]), g.point(lambda _: color[1]), b.point(lambda _: color[2]), a))
    if alpha < 1.0:
        r, g, b, a = out.split()
        out = Image.merge('RGBA', (r, g, b, a.point(lambda v: int(v * alpha))))
    return out


class Frame:
    def __init__(self, raw):
        self.raw = raw
        self.im = Image.new('RGBA', (W * S, H * S), (2, 3, 15, 255))
        d = ImageDraw.Draw(self.im)
        for y in range(H * S):
            k = y / (H * S)
            d.line([(0, y), (W * S, y)], fill=(int(2 + 8 * (1 - k)), int(3 + 12 * (1 - k)), int(15 + 36 * (1 - k))))
        import random
        r = random.Random(3)
        for _ in range(110):
            x, y = r.random() * W * S, r.random() * H * S
            d.ellipse([x - 1, y - 1, x + 1, y + 1], fill=(255, 255, 255, int(60 + 120 * r.random())))
        self.m = compute(H)
        self.cache = {}

    def spr(self, name):
        if name not in self.cache:
            self.cache[name] = load(self.raw, name)
        return self.cache[name]

    def L(self, bx, by):
        m = self.m
        return (180 - 4.5 + (bx - 180) * m['scale'], m['cy'] + (by - 280) * m['scale'])

    def put(self, name, bx, by, wdp, hdp, angle_cw=0.0, alpha=1.0, tint=None, scene=True):
        sp = self.spr(name)
        k = self.m['scale'] if scene else 1.0
        w, h = max(1, int(wdp * k * S)), max(1, int(hdp * k * S))
        s = sp.resize((w, h), Image.LANCZOS)
        if tint is not None or alpha < 1.0:
            s = tinted(s, tint if tint is not None else (255, 255, 255), alpha) if tint is not None else tinted_alpha(s, alpha)
        if angle_cw:
            s = s.rotate(-math.degrees(angle_cw), resample=Image.BICUBIC, expand=True)
        x, y = self.L(bx, by) if scene else (bx, by)
        self.im.alpha_composite(s, (int(x * S - s.width / 2), int(y * S - s.height / 2)))

    def glow(self, bx, by, size, color, alpha):
        k = self.m['scale']
        n = max(8, int(size * k * S))
        mask = Image.radial_gradient('L').resize((n, n), Image.BICUBIC).point(lambda v: int(max(0, 255 - v * 1.2) * alpha))
        layer = Image.new('RGBA', (n, n), color + (0,))
        layer.putalpha(mask)
        x, y = self.L(bx, by)
        self.im.alpha_composite(layer, (int(x * S - n / 2), int(y * S - n / 2)))

    def line(self, a, b, width, color):
        d = ImageDraw.Draw(self.im, 'RGBA')
        pa, pb = self.L(*a), self.L(*b)
        k = self.m['scale']
        d.line([(pa[0] * S, pa[1] * S), (pb[0] * S, pb[1] * S)], fill=color, width=max(1, int(width * k * S)))
        r = width * k * S / 2
        for p in (pa, pb):
            d.ellipse([p[0] * S - r, p[1] * S - r, p[0] * S + r, p[1] * S + r], fill=color)


def tinted_alpha(sp, alpha):
    r, g, b, a = sp.split()
    return Image.merge('RGBA', (r, g, b, a.point(lambda v: int(v * alpha))))


def parse(raw, name):
    m = dict(gears=[], links=[], mission={}, result={}, changes=[], solution=[], downstream=[], targets=[])
    for line in open(os.path.join(raw, 'eng_machine_%s.txt' % name), encoding='utf8'):
        line = line.rstrip('\n')
        key, _, rest = line.partition(' ')
        p = rest.split()
        if key == 'meta':
            m['level'], m['mdir'], m['w'], m['mink'], m['keys'], m['hint'] = [int(v) for v in p]
        elif key == 'targets':
            m['targets'] = [int(v) for v in p]
        elif key == 'mission':
            m['mission'][int(p[0])] = int(p[1])
        elif key == 'result':
            m['result'][int(p[0])] = (int(p[1]), p[2] == '1')
        elif key == 'changes':
            m['changes'] = [int(v) for v in p]
        elif key == 'solution':
            m['solution'] = [int(v) for v in p]
        elif key == 'downstream':
            m['downstream'] = [int(v) for v in p]
        elif key == 'gear':
            m['gears'].append(dict(i=int(p[0]), x=float(p[1]), y=float(p[2]), tip=float(p[3]), n=int(p[4]), a=float(p[5]), role=int(p[6]),
                                   station=int(p[7]), pitch=float(p[8]), depth=int(p[9]), speed=float(p[10])))
        elif key == 'link':
            m['links'].append(dict(i=int(p[0]), a=int(p[1]), b=int(p[2]), type=p[3], crossed=p[4] == '1'))
        elif key in ('fail', 'hint', 'ok'):
            m['t_' + key] = rest
        elif key == 'consigna':
            m['consigna'] = rest.split('|')
    return m


def gear_name(g):
    size = 'Big' if g['n'] >= 15 else 'Small'
    pal = 'Motor' if g['i'] == 0 else 'Station' if g['role'] == 2 else 'Main'
    return 'eng_gear_%s_%s' % (size, pal), 'eng_sil_' + size


def draw_gear(f, g, angle, glow=None):
    box = 2 * (g['tip'] + 3)
    if glow:
        f.glow(g['x'], g['y'], g['tip'] * 2 + 24, glow, 0.5)
    d = ImageDraw.Draw(f.im)
    cx, cy = f.L(g['x'], g['y'])
    r = g['tip'] * 0.5 * f.m['scale'] * S
    d.ellipse([cx * S - r, cy * S - r, cx * S + r, cy * S + r], fill=(11, 16, 48))
    gname, sname = gear_name(g)
    f.put(sname, g['x'], g['y'] + 4, box, box, angle_cw=angle, alpha=0.45, tint=(0, 0, 0))
    f.put(gname, g['x'], g['y'], box, box, angle_cw=angle)
    f.put('eng_hub', g['x'], g['y'], g['tip'] * 0.5, g['tip'] * 0.5)


def draw_belt(f, a, b, k, hot, gold):
    ang = math.atan2(b['y'] - a['y'], b['x'] - a['x'])
    n = ang + math.pi / 2
    r = 14.0
    nv = (math.cos(n), math.sin(n))
    layers = [(16, (255, 201, 74, int(255 * hot))), (8, INK + (255,)), (4.5, (GOLD if gold else BELT_LIGHT) + (255,))]
    for w, col in layers:
        if w == 16 and hot <= 0.01:
            continue
        for s in (1, -1):
            sb = s * (1 - 2 * k)
            p0 = (a['x'] + nv[0] * r * s, a['y'] + nv[1] * r * s)
            p1 = (b['x'] + nv[0] * r * sb, b['y'] + nv[1] * r * sb)
            f.line(p0, p1, w, col)
        for end, rot in ((a, ang), (b, ang + math.pi)):
            cap = f.spr('eng_cap_%s' % ('%g' % w))
            box = 2 * (r + w / 2 + 2)
            # el sprite cubre la mitad de la izquierda; en pantalla (y hacia abajo) se gira ang grados en el sentido del reloj
            f.put('eng_cap_%s' % ('%g' % w), end['x'], end['y'], box, box, angle_cw=rot, tint=col[:3], alpha=col[3] / 255 if len(col) > 3 else 1.0)
    for end in (a, b):
        f.put('eng_pulley', end['x'], end['y'], 40, 40)


def text_w(txt, size, bold=True):
    return font(size, bold).getlength(txt) / S


def render(raw, name, state, title):
    m = parse(raw, name)
    f = Frame(raw)
    d = ImageDraw.Draw(f.im)
    mm = f.m
    gears = m['gears']
    by_i = {g['i']: g for g in gears}
    changes = m['changes']
    reveal = state in ('fail', 'ok')
    # marcador y cabecera
    d.rounded_rectangle([10 * S, 6 * S, 350 * S, 58 * S], radius=14 * S, fill=(20, 27, 58))
    d.text((24 * S, 32 * S), 'Engranajes', font=font(20), fill=(255, 255, 255), anchor='lm')
    d.text((340 * S, 32 * S), 'Nivel %d' % m['level'], font=font(16), fill=GOLD, anchor='rm')
    d.text((14 * S, (mm['strip_top'] + 14) * S), 'Cohete n.º 2', font=font(17), fill=TEXT, anchor='lm')
    d.text((14 * S, (mm['strip_top'] + 34) * S), '1 cohete en órbita', font=font(14, False), fill=LAV, anchor='lm')
    for k in range(10):
        x, y = (150 + 19 * k) * S, (mm['strip_top'] + 14) * S
        d.ellipse([x - 6 * S, y - 6 * S, x + 6 * S, y + 6 * S], fill=GOLD if k < 4 else (255, 255, 255, 41))
    # consigna
    d.rounded_rectangle([8 * S, mm['q_top'] * S, 352 * S, (mm['q_top'] + 48) * S], radius=14 * S, fill=(20, 27, 58))
    d.text((180 * S, (mm['q_top'] + 14) * S), m['consigna'][0], font=font(17), fill=TEXT, anchor='mm')
    d.text((180 * S, (mm['q_top'] + 35) * S), m['consigna'][1], font=font(14, False), fill=LAV, anchor='mm')
    # sala, casco y ventanillas
    f.put('eng_room', 129, 288, 242, 348)
    f.put('eng_hull', 306, 267, 116, 358)
    for k in range(10):
        y = 162 + k * ((428 - 150 - 90) / 9)
        cx, cy = f.L(338, y)
        r = 4.2 * mm['scale'] * S
        d = ImageDraw.Draw(f.im)
        d.ellipse([cx * S - r, cy * S - r, cx * S + r, cy * S + r], fill=GOLD if k < 4 else (58, 65, 112))
    active = {g['station']: g for g in gears if g['station'] >= 0}
    for s in range(4):
        a = 1.0 if s in active else 0.45
        if s == 0:
            ang = (active[0]['a'] + (2.4 * active[0]['speed'] * 0.8 if reveal else 0)) if 0 in active else 0
            f.line((306, 110), (306, 124), 6, (74, 80, 128, int(255 * a)))
            f.put('eng_dish', 306, 110, 48, 24, angle_cw=ang, alpha=a)
        elif s == 1:
            off = (-40 if active[1]['speed'] < 0 else 16) if reveal and 1 in active else 0
            d = ImageDraw.Draw(f.im)
            x0, y0 = f.L(296, 220)
            x1, y1 = f.L(336, 268)
            d.rounded_rectangle([x0 * S, y0 * S, x1 * S, y1 * S], radius=6 * mm['scale'] * S, fill=(30, 37, 80, int(255 * a)))
            f.put('eng_door', 316, 244 + max(-44, min(10, off)), 44, 52, alpha=a)
        elif s == 2:
            off = (-26 if active[2]['speed'] < 0 else 16) if reveal and 2 in active else 0
            f.put('eng_cframe', 316, 316, 48, 68, alpha=a)
            f.put('eng_cplate', 316, 325 + max(-26, min(18, off)), 40, 10, alpha=a)
            f.put('eng_cbox', 316, 312 + max(-26, min(18, off)), 32, 24, alpha=a)
        else:
            ang = (active[3]['a'] + (2.4 * active[3]['speed'] * 0.8 if reveal else 0)) if 3 in active else 0
            f.put('eng_fan', 306, 442, 40, 40, angle_cw=ang, alpha=a)
    # ejes de la antena y la turbina
    for s in (0, 3):
        if s not in active:
            continue
        g = active[s]
        px, py = PART[s]
        for w, col in ((6, (59, 52, 112)), (2.5, (107, 95, 184))):
            f.line((g['x'], g['y']), (px, g['y']), w, col)
            f.line((px, g['y']), (px, py), w, col)
    # engranajes (al equivocarse, en celeste los que movió el cambio)
    fixing = state == 'fail'
    hot_g = set(m['downstream']) if fixing else set()
    for g in gears:
        a = g['a'] + (2.4 * g['speed'] * 0.8 if reveal and g['depth'] >= 0 else 0)
        draw_gear(f, g, a, glow=CYAN if g['i'] in hot_g else None)
    # barras dentadas
    for s, idx in ((1, 'eng_rack'), (2, 'eng_rack')):
        if s in active:
            g = active[s]
            off = ((-40 if g['speed'] < 0 else 16) if reveal else 0)
            f.put(idx, g['x'] + g['pitch'] + 7.5, g['y'] + max(-40, min(18, off)), 20, 84)
    # correas
    for l in m['links']:
        if l['type'] != 'Belt':
            continue
        changed = l['i'] in changes
        k = 1.0 if (l['crossed'] != changed) else 0.0
        hot = 0.85 if fixing and l['i'] in m['solution'] else 0.0
        draw_belt(f, by_i[l['a']], by_i[l['b']], k, hot, changed)
    # motor: flecha y rótulo
    g0 = gears[0]
    md = m['mdir'] * (-1 if -1 in changes else 1)
    r = int(g0['tip'] + 8)
    f.put('eng_arrow_%d_%s' % (r, 'cw' if md > 0 else 'ccw'), g0['x'], g0['y'], 2 * (r + 14), 2 * (r + 14), tint=GOLD if -1 in changes else CYAN)
    changed = -1 in changes
    if fixing and -1 in m['solution']:
        f.glow(g0['x'], g0['y'] + 16, 90, GOLD, 0.9)
    cx, cy = f.L(g0['x'], g0['y'] + 16)
    k = mm['scale']
    d = ImageDraw.Draw(f.im)
    d.rounded_rectangle([(cx - 31 * k) * S, (cy - 12 * k) * S, (cx + 31 * k) * S, (cy + 12 * k) * S], radius=12 * k * S, fill=GOLD if changed else (35, 43, 87),
                        outline=INK if changed else (142, 131, 216), width=int(1.5 * S))
    d.text((cx * S, (cy + 0.5) * S), 'MOTOR', font=font(14), fill=INK if changed else TEXT, anchor='mm')
    # carteles
    for s in range(4):
        if s not in active:
            continue
        act = m['mission'][s]
        res = m['result'].get(s)
        done = reveal and res is not None
        ok = done and res[1]
        bad = done and not res[1]
        name_w = text_w(NAMES[s], 14)
        word_w = text_w(ACT_WORD[act], 14)
        w = max(96.0, max(name_w + 24, word_w + 18) / mm['scale'] + 16)       # el nombre deja lugar al ✓ de la derecha
        max_edge = 180 + (360 - 3 - (180 - 4.5)) / mm['scale']
        cxs = min(273 + w / 2, max_edge - w / 2)
        cx, cy = f.L(cxs, CARTEL_Y[s])
        ww, hh = w * k, 38 * k
        fill = (15, 59, 51) if ok else (74, 30, 34) if bad else (28, 35, 80)
        rim = MINT if ok else CORAL if bad else GOLD
        d = ImageDraw.Draw(f.im)
        d.rounded_rectangle([(cx - ww / 2) * S, (cy - hh / 2) * S, (cx + ww / 2) * S, (cy + hh / 2) * S], radius=12 * k * S, fill=fill, outline=rim, width=int(2.2 * S))
        d.text((cx * S, (cy - 9 * k) * S), NAMES[s], font=font(14), fill=TEXT, anchor='mm')
        col = MINT if ok else (255, 180, 140) if bad else GOLD
        grp = word_w + 18
        ix = cx - grp / 2 + 6
        iy = cy + 9.5 * k
        if s in (0, 3):
            sp = f.spr('eng_mini_cw' if act == 0 else 'eng_mini_ccw')
            im = tinted(sp.resize((22 * S, 22 * S), Image.LANCZOS), col)
            f.im.alpha_composite(im, (int((ix - 11) * S), int((iy - 11) * S)))
        else:
            sp = f.spr('eng_glyph_Up' if act in (2, 4) else 'eng_glyph_Down')
            im = tinted(sp.resize((26 * S, 26 * S), Image.LANCZOS), col)
            f.im.alpha_composite(im, (int((ix - 13) * S), int((iy - 13) * S)))
        d.text(((ix + 10) * S, iy * S), ACT_WORD[act], font=font(14), fill=col, anchor='lm')
        if ok:
            chk = tinted(f.spr('eng_check').resize((14 * S, 14 * S), Image.LANCZOS), MINT)
            f.im.alpha_composite(chk, (int((cx + ww / 2 - 18) * S), int((cy - 17 * k) * S)))
    # abajo: la cuenta de cambios y «Arrancar»
    d = ImageDraw.Draw(f.im)
    y0, bh = mm['btn_top'], mm['btn_h']
    can = state == 'play'
    d.rounded_rectangle([14 * S, y0 * S, 118 * S, (y0 + bh) * S], radius=20 * S, fill=(20, 27, 58))
    d.text((66 * S, (y0 + 20) * S), '1 cambio' if m['keys'] == 1 else '2 cambios', font=font(14, False), fill=LAV, anchor='mm')
    for kk in range(m['keys']):
        x = 66 + (0 if m['keys'] == 1 else (-18 if kk == 0 else 18))
        used = kk < len(changes)
        d.ellipse([(x - 16) * S, (y0 + 54 - 16) * S, (x + 16) * S, (y0 + 54 + 16) * S], fill=(255, 201, 74, 46) if used else (255, 255, 255, 15))
        wr = tinted(f.spr('eng_wrench').resize((30 * S, 30 * S), Image.LANCZOS), GOLD if used else (110, 106, 154))
        f.im.alpha_composite(wr, (int((x - 15) * S), int((y0 + 54 - 15) * S)))
    bx, bw = 130, 216
    d.rounded_rectangle([bx * S, (y0 + 6) * S, (bx + bw) * S, (y0 + bh + 6) * S], radius=24 * S, fill=(0, 0, 0, 115))
    d.rounded_rectangle([bx * S, y0 * S, (bx + bw) * S, (y0 + bh) * S], radius=24 * S, fill=CYAN, outline=INK, width=int(3 * S))
    play = tinted(f.spr('eng_glyph_Play').resize((28 * S, 28 * S), Image.LANCZOS), INK)
    f.im.alpha_composite(play, (int((bx + 63 - 14) * S), int((y0 + bh / 2 - 14) * S)))
    d.text(((bx + 88) * S, (y0 + bh / 2) * S), 'Arrancar', font=font(22), fill=INK, anchor='lm')
    if not can:
        veil = Image.new('RGBA', f.im.size, (0, 0, 0, 0))
        ImageDraw.Draw(veil).rounded_rectangle([14 * S, y0 * S, (bx + bw) * S, (y0 + bh + 6) * S], radius=20 * S, fill=(2, 3, 15, 110))
        f.im.alpha_composite(veil)
    # aviso
    d = ImageDraw.Draw(f.im)
    if reveal:
        good = state == 'ok'
        d.rounded_rectangle([10 * S, mm['say_top'] * S, 350 * S, (mm['say_top'] + mm['say_h']) * S], radius=14 * S, fill=(8, 10, 34), outline=MINT if good else (255, 214, 160), width=int(1.5 * S))
        if good:
            d.text((180 * S, (mm['say_top'] + mm['say_h'] / 2) * S), m['t_ok'], font=font(16), fill=MINT, anchor='mm')
        else:
            d.text((180 * S, (mm['say_top'] + 26) * S), m['t_fail'], font=font(16), fill=(255, 214, 160), anchor='mm')
            d.text((180 * S, (mm['say_top'] + mm['say_h'] - 24) * S), m['t_hint'], font=font(14, False), fill=GOLD, anchor='mm')
    d.text((180 * S, (H - 6) * S), title, font=font(12, False), fill=(110, 106, 154), anchor='mm')
    return f.im


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('raw')
    ap.add_argument('--out', default=ROOT + '/docs/previews')
    a = ap.parse_args()
    frames = [
        ('l3', 'play', 'Etapa 3: la máquina mal armada'),
        ('l7', 'fail', 'Etapa 7: un cambio equivocado'),
        ('l10', 'ok', 'Etapa 10: dos cambios, todo en verde'),
    ]
    imgs = [render(a.raw, n, s, t) for n, s, t in frames]
    gap = 16 * S
    sheet = Image.new('RGBA', (len(imgs) * (W * S + gap) + gap, H * S + 2 * gap), (3, 4, 20, 255))
    for i, im in enumerate(imgs):
        sheet.alpha_composite(im, (gap + i * (W * S + gap), gap))
    os.makedirs(a.out, exist_ok=True)
    out = os.path.join(a.out, 'engranajes.png')
    sheet.convert('RGB').save(out)
    print('OK', out)


if __name__ == '__main__':
    main()
