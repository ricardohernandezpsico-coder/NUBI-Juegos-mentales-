"""Lámina de «Engranajes» (5-oct-2026): réplica EXACTA de la composición de la pantalla con los sprites REALES horneados (tools/art-preview) y las máquinas de muestra del generador real.

Uso:  python3 tools/art-preview/engranajes.py <raw> [--out docs/previews]
      (<raw> = la carpeta que vuelca ArtPreview: `dotnet run --project tools/art-preview -- <raw>`)
Genera docs/previews/engranajes.png con seis pantallas de 360 × 740 dp (nivel 3 mirando, nivel 5 con la carga arrancando, nivel 8 con correa, nivel 9 con la trampa, nivel 10 con el hueco y
nivel 12 con la correa cruzada elegida). Réplica de la disposición de EngranajesLayout + EngranajesGameController (ver el código Unity); no es una captura del juego.
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
LAV = (171, 165, 210)
TEXT = (237, 234, 251)


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


class Frame:
    def __init__(self, raw):
        self.raw = raw
        self.im = Image.new('RGBA', (W * S, H * S), (2, 3, 15, 255))
        d = ImageDraw.Draw(self.im)
        for y in range(H * S):
            k = y / (H * S)
            col = (int(2 + 8 * (1 - k)), int(3 + 12 * (1 - k)), int(15 + 36 * (1 - k)))
            d.line([(0, y), (W * S, y)], fill=col)
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
        if tint is not None:
            r, g, b, a = s.split()
            s = Image.merge('RGBA', (r.point(lambda _: tint[0]), g.point(lambda _: tint[1]), b.point(lambda _: tint[2]), a))
        if alpha < 1.0:
            r, g, b, a = s.split()
            s = Image.merge('RGBA', (r, g, b, a.point(lambda v: int(v * alpha))))
        if angle_cw:
            s = s.rotate(-math.degrees(angle_cw), resample=Image.BICUBIC, expand=True)
        x, y = self.L(bx, by) if scene else (bx, by)
        self.im.alpha_composite(s, (int(x * S - s.width / 2), int(y * S - s.height / 2)))

    def line(self, a, b, width, color):
        d = ImageDraw.Draw(self.im)
        pa, pb = self.L(*a), self.L(*b)
        k = self.m['scale']
        d.line([(pa[0] * S, pa[1] * S), (pb[0] * S, pb[1] * S)], fill=color, width=max(1, int(width * k * S)))
        r = width * k * S / 2
        for p in (pa, pb):
            d.ellipse([p[0] * S - r, p[1] * S - r, p[0] * S + r, p[1] * S + r], fill=color)

    def pill(self, bx, by, wdp, hdp, fill, text, fg, size, scene=True):
        d = ImageDraw.Draw(self.im)
        x, y = self.L(bx, by) if scene else (bx, by)
        k = self.m['scale'] if scene else 1.0
        w, h = wdp * k, hdp * k
        d.rounded_rectangle([(x - w / 2) * S, (y - h / 2) * S, (x + w / 2) * S, (y + h / 2) * S], radius=h / 2 * S, fill=fill)
        d.text((x * S, (y + 0.5) * S), text, font=font(size), fill=fg, anchor='mm')


def parse(raw, name):
    gears, links, buttons = [], [], []
    meta, slot, jam, question, explain, trick, path = None, None, None, '', '', '', []
    for line in open(os.path.join(raw, 'eng_machine_%s.txt' % name), encoding='utf8'):
        line = line.rstrip('\n')
        key, _, rest = line.partition(' ')
        if key == 'meta':
            meta = rest.split()
        elif key == 'question':
            question = rest
        elif key == 'button':
            buttons.append(rest.split('|'))
        elif key == 'gear':
            p = rest.split()
            gears.append(dict(i=int(p[0]), x=float(p[1]), y=float(p[2]), tip=float(p[3]), n=int(p[4]), a=float(p[5]), role=int(p[6]), removed=p[7] == '1',
                              station=int(p[8]), pitch=float(p[9]), depth=int(p[10]), speed=float(p[11])))
        elif key == 'link':
            p = rest.split()
            links.append((int(p[0]), int(p[1]), p[2]))
        elif key == 'slot':
            p = rest.split()
            slot = dict(x=float(p[0]), y=float(p[1]), tip=float(p[2]), n=int(p[3]), a=float(p[4]), frm=int(p[5]), to=int(p[6]))
        elif key == 'jam':
            jam = [int(v) for v in rest.split()]
        elif key == 'explain':
            explain = rest
        elif key == 'trick':
            trick = rest
        elif key == 'path':
            path = [int(v) for v in rest.split()]
    return dict(meta=meta, gears=gears, links=links, buttons=buttons, slot=slot, jam=jam, question=question, explain=explain, trick=trick, path=path)


STATION_LABEL = ['Antena', 'Compuerta', 'Carga', 'Turbina']
PART = [(306, 98), (312, 250), (312, 316), (306, 442)]
LABEL = [(306, 126), (304, 281), (304, 360), (306, 404)]


def gear_name(g, index):
    size = 'Big' if g['n'] >= 15 else 'Small' if g['n'] >= 10 else 'Jam'
    if index == 0:
        pal = 'Motor'
    elif g['role'] == 3:
        pal = 'Station'
    elif g['role'] == 4:
        pal = 'Jam'
    else:
        pal = 'Main'
    return 'eng_gear_%s_%s' % (size, pal), size


def draw_gear(f, name_sil, name_gear, g, angle, glow=0.0):
    box = 2 * (g['tip'] + 3)
    # soporte del eje
    d = ImageDraw.Draw(f.im)
    cx, cy = f.L(g['x'], g['y'])
    r = g['tip'] * 0.5 * f.m['scale'] * S
    d.ellipse([cx * S - r, cy * S - r, cx * S + r, cy * S + r], fill=(11, 16, 48))
    # la sombra va sobre el soporte, el cuerpo encima
    f.put(name_sil, g['x'], g['y'] + 4, box, box, angle_cw=angle, alpha=0.45, tint=(0, 0, 0))
    f.put(name_gear, g['x'], g['y'], box, box, angle_cw=angle)
    f.put('eng_hub', g['x'], g['y'], g['tip'] * 0.5, g['tip'] * 0.5)


def render(raw, name, state, title, note):
    m = parse(raw, name)
    f = Frame(raw)
    d = ImageDraw.Draw(f.im)
    meta = m['meta']
    level, q, tstation, mdir, truth, target, jam, placed = int(meta[0]), meta[1], int(meta[2]), int(meta[3]), meta[4], int(meta[5]), meta[6] == '1', int(meta[7])
    mm = f.m
    # marcador (réplica simple)
    d.rounded_rectangle([10 * S, 6 * S, 350 * S, 58 * S], radius=14 * S, fill=(20, 27, 58))
    d.text((24 * S, 32 * S), 'Engranajes', font=font(20), fill=(255, 255, 255), anchor='lm')
    d.text((340 * S, 32 * S), 'Nivel %d' % level, font=font(16), fill=GOLD, anchor='rm')
    # cabecera
    d.text((14 * S, (mm['strip_top'] + 14) * S), 'Cohete n.º 2', font=font(17), fill=TEXT, anchor='lm')
    d.text((14 * S, (mm['strip_top'] + 34) * S), '1 cohete en órbita', font=font(14, False), fill=LAV, anchor='lm')
    lit = 4
    for k in range(10):
        x, y = (150 + 19 * k) * S, (mm['strip_top'] + 14) * S
        col = GOLD if k < lit else (255, 255, 255, 41)
        d.ellipse([x - 6 * S, y - 6 * S, x + 6 * S, y + 6 * S], fill=col)
    # pregunta
    d.rounded_rectangle([8 * S, mm['q_top'] * S, 352 * S, (mm['q_top'] + 48) * S], radius=14 * S, fill=(20, 27, 58))
    words = m['question'].split(' ')
    if len(m['question']) <= 32:
        d.text((180 * S, (mm['q_top'] + 25) * S), m['question'], font=font(18), fill=TEXT, anchor='mm')
    else:
        best, bd = 1, 1e9
        for i in range(1, len(words)):
            dd = abs(len(' '.join(words[:i])) - len(' '.join(words[i:])))
            if dd < bd:
                bd, best = dd, i
        d.text((180 * S, (mm['q_top'] + 14) * S), ' '.join(words[:best]), font=font(16), fill=TEXT, anchor='mm')
        d.text((180 * S, (mm['q_top'] + 35) * S), ' '.join(words[best:]), font=font(16), fill=TEXT, anchor='mm')
    # escena: sala, casco, ventanillas
    f.put('eng_room', 129, 288, 242, 348)
    f.put('eng_hull', 306, 267, 116, 358)
    for k in range(10):
        y = 154 + k * ((428 - 142 - 90) / 9)
        cx, cy = f.L(338, y)
        r = 4.2 * mm['scale'] * S
        d = ImageDraw.Draw(f.im)
        d.ellipse([cx * S - r, cy * S - r, cx * S + r, cy * S + r], fill=GOLD if k < 4 else (58, 65, 112))
    active = {g['station']: g for g in m['gears'] if g['station'] >= 0 and not g['removed']}
    running = state in ('run', 'jam')
    # piezas
    for s in range(4):
        a = 1.0 if s in active else 0.45
        px, py = PART[s]
        if s == 0:
            gear = active.get(0)
            ang = (gear['a'] + (2.4 * gear['speed'] * 0.8 if running and gear['depth'] >= 0 and not jam else 0)) if gear else 0
            f.line((306, 98), (306, 112), 6, (74, 80, 128, int(255 * a)))
            f.put('eng_dish', 306, 98, 48, 24, angle_cw=ang, alpha=a)
        elif s == 1:
            off = 0
            if running and 1 in active and not jam:
                off = -40 if active[1]['speed'] < 0 else 16
            d = ImageDraw.Draw(f.im)
            x0, y0 = f.L(296, 220)
            x1, y1 = f.L(336, 268)
            d.rounded_rectangle([x0 * S, y0 * S, x1 * S, y1 * S], radius=6 * mm['scale'] * S, fill=(30, 37, 80, int(255 * a)))
            f.put('eng_door', 316, 244 + max(-44, min(10, off)), 44, 52, alpha=a)
        elif s == 2:
            off = 0
            if running and 2 in active and not jam:
                off = -26 if active[2]['speed'] < 0 else 16
            f.put('eng_cframe', 316, 316, 48, 68, alpha=a)
            f.put('eng_cplate', 316, 325 + max(-26, min(18, off)), 40, 10, alpha=a)
            f.put('eng_cbox', 316, 312 + max(-26, min(18, off)), 32, 24, alpha=a)
        else:
            gear = active.get(3)
            ang = (gear['a'] + (2.4 * gear['speed'] * 0.8 if running and gear['depth'] >= 0 and not jam else 0)) if gear else 0
            f.put('eng_fan', 306, 442, 40, 40, angle_cw=ang, alpha=a)
    # ejes
    for s, g in active.items():
        px, py = PART[s]
        a = (g['x'], g['y'])
        if s in (0, 3):
            segs = [(a, (px, g['y'])), ((px, g['y']), (px, py))]
        else:
            segs = [(a, (px - 14, g['y']))]
        for w, col in ((6, (59, 52, 112)), (2.5, (107, 95, 184))):
            for sa, sb in segs:
                f.line(sa, sb, w, col)
    gears = m['gears']
    by_i = {g['i']: g for g in gears}

    def belt(a, b, crossed):
        ang = math.atan2(b['y'] - a['y'], b['x'] - a['x']) + math.pi / 2
        nv = (math.cos(ang), math.sin(ang))
        for w, col in ((8, (42, 35, 80)), (4, (142, 131, 216))):
            for sg in (1, -1):
                p0 = (a['x'] + nv[0] * (a['pitch'] - 1) * sg, a['y'] + nv[1] * (a['pitch'] - 1) * sg)
                k = -sg if crossed else sg
                p1 = (b['x'] + nv[0] * (b['pitch'] - 1) * k, b['y'] + nv[1] * (b['pitch'] - 1) * k)
                f.line(p0, p1, w, col)

    for a, b, t in m['links']:
        if t in ('Straight', 'Crossed'):
            belt(by_i[a], by_i[b], t == 'Crossed')
    slot = m['slot']
    if slot and placed == 2:
        belt(by_i[slot['frm']], by_i[slot['to']], True)
    # engranajes
    for g in gears:
        if g['removed']:
            continue
        a = g['a']
        if running and not jam and g['depth'] >= 0:
            a += 2.4 * g['speed'] * 0.8
        gname, size = gear_name(g, g['i'])
        draw_gear(f, 'eng_sil_' + size, gname, g, a)
    if slot:
        if placed == 1:
            sa = slot['a'] + (2.4 * (-by_i[slot['frm']]['speed'] * by_i[slot['frm']]['pitch'] / (30 if slot['n'] >= 15 else 20)) * 0.8 if running else 0)
            g2 = dict(x=slot['x'], y=slot['y'], tip=slot['tip'], n=slot['n'], role=1)
            gname, size = gear_name(g2, 99)
            draw_gear(f, 'eng_sil_' + size, gname, g2, sa)
        elif placed == 0:
            r = slot['tip'] * 1.05
            f.put('eng_dring', slot['x'], slot['y'], r * 2, r * 2, tint=GOLD, alpha=0.8)
            d = ImageDraw.Draw(f.im)
            cx, cy = f.L(slot['x'], slot['y'] + 1)
            d.text((cx * S, cy * S), '?', font=font(22), fill=GOLD, anchor='mm')
    # correa: rótulos
    for a, b, t in m['links']:
        if t in ('Straight', 'Crossed'):
            A, B = by_i[a], by_i[b]
            mx, my = (A['x'] + B['x']) / 2, (A['y'] + B['y']) / 2
            vertical = abs(A['x'] - B['x']) < 2
            lx = (mx + 50 if mx + 50 < 240 else mx - 50) if vertical else mx
            ly = my if vertical else my - 30
            f.pill(lx, ly, 72, 22, (35, 43, 87), 'cruzada' if t == 'Crossed' else 'recta', TEXT, 14)
    # motor: flecha
    g0 = gears[0]
    r = g0['tip'] + 10
    arrow = 'eng_arrow_%d_%s' % (int(r), 'cw' if mdir > 0 else 'ccw')
    box = 2 * (r + 14)
    f.put(arrow, g0['x'], g0['y'], box, box, tint=CYAN)
    # trampa
    if jam and state == 'jam':
        tri = m['jam']
        pts = [by_i[i] for i in tri]
        for e in range(3):
            a, b = pts[e], pts[(e + 1) % 3]
            length = math.hypot(b['x'] - a['x'], b['y'] - a['y'])
            dx, dy = (b['x'] - a['x']) / length, (b['y'] - a['y']) / length
            t = 0.0
            while t < length - 2:
                p0 = (a['x'] + dx * t, a['y'] + dy * t)
                p1 = (a['x'] + dx * min(length, t + 6), a['y'] + dy * min(length, t + 6))
                f.line(p0, p1, 3, (255, 140, 107))
                t += 11
    # flechas del truco
    if state == 'trick':
        for j, gi in enumerate(m['path']):
            g = by_i[gi]
            r = g['tip'] + 7
            arrow = 'eng_arrow_%d_%s' % (int(r), 'cw' if g['speed'] >= 0 else 'ccw')
            f.put(arrow, g['x'], g['y'], 2 * (r + 14), 2 * (r + 14), tint=GOLD if j % 2 else MINT)
    # rótulos
    for s in range(4):
        tgt = s == tstation
        label = STATION_LABEL[s]
        size = 16 if tgt else 14
        w = len(label) * 0.6 * size + 16
        f.pill(LABEL[s][0], LABEL[s][1], w, 22, GOLD if tgt else (244, 241, 255, 224), label, INK if (tgt or s in active) else (26, 18, 64, 128), size)
    f.pill(g0['x'], g0['y'] + g0['tip'] * 0.55, 62, 22, (22, 58, 92), 'MOTOR', (191, 233, 255), 14)
    if state == 'play':
        px, py = PART[tstation]
        f.put('eng_tring', px, py, 64, 64, tint=GOLD, alpha=0.8)
    # botones
    d = ImageDraw.Draw(f.im)
    n = len(m['buttons'])
    bw = (W - 28 - 10 * (n - 1)) / n
    for k, (val, label, icon) in enumerate(m['buttons']):
        x0 = 14 + k * (bw + 10)
        y0 = mm['btn_top']
        chosen = state in ('run', 'trick', 'jam') and ((truth.lower() and k == 0))
        dim = state != 'play' and not chosen
        d.rounded_rectangle([x0 * S, (y0 + 6) * S, (x0 + bw) * S, (y0 + mm['btn_h'] + 6) * S], radius=22 * S, fill=(0, 0, 0, 115))
        d.rounded_rectangle([x0 * S, y0 * S, (x0 + bw) * S, (y0 + mm['btn_h']) * S], radius=22 * S, fill=GOLD if chosen else (35, 43, 87), outline=INK if chosen else (60, 68, 108), width=int(1.5 * S))
        col = INK if chosen else TEXT
        icon_img = load(raw, 'eng_icon_' + icon)
        r_, g_, b_, a_ = icon_img.split()
        icon_img = Image.merge('RGBA', (r_.point(lambda _: col[0]), g_.point(lambda _: col[1]), b_.point(lambda _: col[2]), a_))
        icon_img = icon_img.resize((56 * S, 56 * S), Image.LANCZOS)
        f.im.alpha_composite(icon_img, (int((x0 + bw / 2 - 28) * S), int((y0 + mm['btn_h'] * 0.36 - 28) * S)))
        size = 15 if bw < 110 else 17
        lines = label.split(' ')
        if bw < 110 and len(label) > 9:
            half = len(lines) // 2 + (len(lines) % 2)
            lines = [' '.join(lines[:half]), ' '.join(lines[half:])]
        else:
            lines = [label]
        ly = y0 + mm['btn_h'] - 24 - (len(lines) - 1) * 9
        for line in lines:
            d.text(((x0 + bw / 2) * S, ly * S), line, font=font(size), fill=col, anchor='mm')
            ly += 18
    # aviso
    if state in ('run', 'trick', 'jam'):
        ok = state == 'run'
        d.rounded_rectangle([10 * S, mm['say_top'] * S, 350 * S, (mm['say_top'] + mm['say_h']) * S], radius=14 * S, fill=(8, 10, 34), outline=(159, 245, 214) if ok else (255, 214, 160), width=int(1.5 * S))
        if ok:
            d.text((180 * S, (mm['say_top'] + mm['say_h'] / 2) * S), '¡Exacto!', font=font(16), fill=MINT, anchor='mm')
        else:
            title = m['explain']
            words = title.split(' ')
            mid = len(words) // 2
            if len(title) > 36:
                l1, l2 = ' '.join(words[:mid]), ' '.join(words[mid:])
            else:
                l1, l2 = title, ''
            y = mm['say_top'] + 18
            d.text((180 * S, y * S), l1, font=font(16), fill=(255, 214, 160), anchor='mm')
            if l2:
                y += 19
                d.text((180 * S, y * S), l2, font=font(16), fill=(255, 214, 160), anchor='mm')
            if m['trick']:
                d.text((180 * S, (mm['say_top'] + mm['say_h'] - 16) * S), m['trick'], font=font(14, False), fill=GOLD, anchor='mm')
    d.text((180 * S, (H - 6) * S), title, font=font(12, False), fill=(110, 106, 154), anchor='mm')
    return f.im


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('raw')
    ap.add_argument('--out', default=ROOT + '/docs/previews')
    a = ap.parse_args()
    frames = [
        ('l3', 'play', 'Nivel 3: una rama; el motor, la pieza que brilla y los botones'),
        ('l5', 'run', 'Nivel 5: la carga arranca (barra dentada)'),
        ('l8', 'run', 'Nivel 8: velocidad, con una correa'),
        ('l9', 'jam', 'Nivel 9: la trampa (triángulo punteado)'),
        ('l10', 'play', 'Nivel 10: «Arma tú»: el hueco con su «?»'),
        ('l12', 'trick', 'Nivel 12: correa cruzada elegida, con el truco de Nubi'),
    ]
    imgs = [render(a.raw, n, s, t, '') for n, s, t in frames]
    cols = 3
    gap = 16 * S
    rows = (len(imgs) + cols - 1) // cols
    sheet = Image.new('RGBA', (cols * (W * S + gap) + gap, rows * (H * S + gap) + gap), (3, 4, 20, 255))
    for i, im in enumerate(imgs):
        sheet.alpha_composite(im, (gap + (i % cols) * (W * S + gap), gap + (i // cols) * (H * S + gap)))
    os.makedirs(a.out, exist_ok=True)
    out = os.path.join(a.out, 'engranajes.png')
    sheet.convert('RGB').save(out)
    print('OK', out)


if __name__ == '__main__':
    main()
