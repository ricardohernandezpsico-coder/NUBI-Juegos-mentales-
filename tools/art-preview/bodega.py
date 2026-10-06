"""Lámina de «Bodega de carga» (5-oct-2026): réplica de la composición de la pantalla con los sprites REALES horneados (tools/art-preview) y la disposición de BodegaLayout.

Uso:  python tools/art-preview/bodega.py <raw> [--out docs/previews]
      (<raw> = la carpeta que vuelca ArtPreview: `dotnet run --project tools/art-preview -- <raw>`; con solo el runtime 10 de .NET: DOTNET_ROLL_FORWARD=LatestMajor)
Genera docs/previews/bodega.png con tres pantallas de 360 × 740 dp:
  1. etapa 1 «guardando»: la carga llega por la esclusa (destello y sello dorado), el robot la toma con su haz y la lleva por dentro del anillo; la escotilla de destino ya se abre;
  2. etapa 7: la caja CERRADA cuelga del robot (se mece) mientras vuela por el anillo hacia la escotilla vacía; la escotilla de donde salió brilla;
  3. etapa 10: la bodega ya giró (la esclusa y las escotillas en otro lugar), se tocó una escotilla equivocada (muestra qué había) y la correcta brilla en dorado.
No es una captura del juego: son los sprites reales en las posiciones que calcula el código (BodegaLayout / BodegaGameController); el movimiento (resortes) no se ve en una imagen quieta.
"""
import argparse
import math
import os
import struct

from PIL import Image, ImageDraw, ImageFont, ImageChops

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

CX, CY, RING, HR, HULL = 180.0, 335.0, 125.0, 27.0, 169.0
OBJ = ['la llave', 'la campana', 'el farol', 'la manzana', 'el hongo', 'la taza', 'el paraguas', 'el libro', 'la gema', 'el reloj de arena', 'la pluma', 'la bellota']


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
    """Port de BodegaLayout.Compute."""
    card_h, tray_circle, tray_label_h, toast_h, hud = 66.0, 38.0, 20.0, 58.0, 66.0
    tray_block = tray_label_h + 6 + tray_circle
    full = hud + 2 + card_h + 4 + 2 * HULL + 6 + tray_block + 6 + toast_h + 6
    fixed = full - 2 * HULL
    scale = max(0.8, min(1.0, (h - fixed - 36) / (2 * HULL)))
    board_h = 2 * HULL * scale
    free = max(0.0, h - fixed - board_h)
    m = {'scale': scale, 'card_top': hud + 2}
    board_top = m['card_top'] + card_h + 4 + free * 0.4
    m['cy'] = board_top + board_h / 2
    tray_top = board_top + board_h + 6 + free * 0.3
    m['tray_label_y'] = tray_top + tray_label_h / 2
    m['tray_y'] = tray_top + tray_label_h + 6 + tray_circle / 2
    m['toast_top'] = tray_top + tray_block + 6 + free * 0.3
    return m


def tint_alpha(sp, alpha):
    r, g, b, a = sp.split()
    return Image.merge('RGBA', (r, g, b, a.point(lambda v: int(v * alpha))))


def tinted(sp, color, alpha=1.0):
    r, g, b, a = sp.split()
    out = Image.merge('RGBA', (r.point(lambda _: color[0]), g.point(lambda _: color[1]), b.point(lambda _: color[2]), a))
    return tint_alpha(out, alpha) if alpha < 1.0 else out


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

    # de un punto del boceto (centro de la bodega en 180, 335) a dp lógicos
    def L(self, bx, by):
        return (180 + (bx - CX) * self.m['scale'], self.m['cy'] + (by - CY) * self.m['scale'])

    def put_logical(self, name, lx, ly, wdp, hdp, rot_ccw=0.0, alpha=1.0, tint=None, im=None):
        sp = self.spr(name) if isinstance(name, str) else name
        w, h = max(1, int(wdp * S)), max(1, int(hdp * S))
        s = sp.resize((w, h), Image.LANCZOS)
        if tint is not None:
            s = tinted(s, tint, alpha)
        elif alpha < 1.0:
            s = tint_alpha(s, alpha)
        if rot_ccw:
            s = s.rotate(rot_ccw, resample=Image.BICUBIC, expand=True)
        (im or self.im).alpha_composite(s, (int(lx * S - s.width / 2), int(ly * S - s.height / 2)))

    def put(self, name, bx, by, wdp, hdp, rot_ccw=0.0, alpha=1.0, tint=None):
        k = self.m['scale']
        lx, ly = self.L(bx, by)
        self.put_logical(name, lx, ly, wdp * k, hdp * k, rot_ccw, alpha, tint)

    def glow(self, bx, by, size, color, alpha):
        k = self.m['scale']
        n = max(8, int(size * k * S))
        mask = Image.radial_gradient('L').resize((n, n), Image.BICUBIC).point(lambda v: int(max(0, 255 - v * 1.2) * alpha))
        layer = Image.new('RGBA', (n, n), color + (0,))
        layer.putalpha(mask)
        x, y = self.L(bx, by)
        self.im.alpha_composite(layer, (int(x * S - n / 2), int(y * S - n / 2)))

    def glow_logical(self, lx, ly, size, color, alpha):
        n = max(8, int(size * S))
        mask = Image.radial_gradient('L').resize((n, n), Image.BICUBIC).point(lambda v: int(max(0, 255 - v * 1.2) * alpha))
        layer = Image.new('RGBA', (n, n), color + (0,))
        layer.putalpha(mask)
        self.im.alpha_composite(layer, (int(lx * S - n / 2), int(ly * S - n / 2)))

    def line(self, a, b, width, color):
        d = ImageDraw.Draw(self.im, 'RGBA')
        pa, pb = self.L(*a), self.L(*b)
        k = self.m['scale']
        d.line([(pa[0] * S, pa[1] * S), (pb[0] * S, pb[1] * S)], fill=color, width=max(1, int(width * k * S)))
        r = width * k * S / 2
        for p in (pa, pb):
            d.ellipse([p[0] * S - r, p[1] * S - r, p[0] * S + r, p[1] * S + r], fill=color)

    def text(self, s, x, y, size, color, bold=True, anchor='mm'):
        d = ImageDraw.Draw(self.im, 'RGBA')
        d.text((x * S, y * S), s, font=font(size, bold), fill=color, anchor=anchor)

    def rrect(self, x, y, w, h, r, fill, outline=None, width=2):
        d = ImageDraw.Draw(self.im, 'RGBA')
        d.rounded_rectangle([x * S, y * S, (x + w) * S, (y + h) * S], radius=r * S, fill=fill, outline=outline, width=int(width * S))


def ring_point(k, places, rot, radius=RING):
    a = -math.pi / 2 + k * 2 * math.pi / places + rot
    return CX + math.cos(a) * radius, CY + math.sin(a) * radius, a


def draw_hull(f, places, rot):
    f.put('bod_hull', CX, CY, 2 * (HULL + 3), 2 * (HULL + 3))
    for k in range(places):
        a = -math.pi / 2 + (k + .5) * 2 * math.pi / places + rot
        f.put('bod_obj_0', 0, 0, 0, 0) if False else None
        d = ImageDraw.Draw(f.im, 'RGBA')
        x, y = f.L(CX + math.cos(a) * (RING + 37), CY + math.sin(a) * (RING + 37))
        r = 3.2 * f.m['scale'] * S
        d.ellipse([x * S - r, y * S - r, x * S + r, y * S + r], fill=(44, 53, 102, 255))


def draw_airlock(f, places, rot, flash=0.0, peek=None):
    x, y, a = ring_point(0, places, rot)
    if flash > 0:
        f.glow(x, y, 2 * (HR + 26), GOLD, flash)
    f.put('bod_seal', x, y, 2 * (HR + 13), 2 * (HR + 13), rot_ccw=-math.degrees(rot))
    f.put('bod_airlock', x, y, 2 * (33 + 2), 2 * (33 + 2))
    if peek is not None:
        f.put('bod_obj_%d' % peek[0], x, y, 34 * peek[1], 34 * peek[1])


def draw_hatch(f, places, rot, i, obj, k=0.0, glow=0.0, show=False, reveal_empty=False, shake=0.0):
    x, y, a = ring_point(i + 1, places, rot)
    x += shake
    sc = f.m['scale']
    if glow > 0:
        f.glow(x, y, 2 * (HR + 24), GOLD, glow)
    f.put('bod_hframe', x, y, 2 * (32 + 2.5), 2 * (32 + 2.5))
    # el interior con máscara redonda
    n = int(2 * (HR + 2) * sc * S)
    layer = Image.new('RGBA', (n, n), (0, 0, 0, 0))
    c = n / 2
    kind = 2 if k <= 0.001 else (1 if (obj is not None or show) else 0)
    inner = f.spr('bod_hin%d' % kind).resize((int(2 * (HR + 1) * sc * S),) * 2, Image.LANCZOS)
    layer.alpha_composite(inner, (int(c - inner.width / 2), int(c - inner.height / 2)))
    if k > 0 and obj is not None:
        osz = int(40 * (1.08 if show else 1.0) * min(1.0, max(0.01, k)) * sc * S)
        o = f.spr('bod_obj_%d' % obj).resize((osz, osz), Image.LANCZOS)
        layer.alpha_composite(o, (int(c - o.width / 2), int(c - o.height / 2)))
    if k > 0.6 and obj is None and reveal_empty:
        d = ImageDraw.Draw(layer, 'RGBA')
        d.text((c, c), 'vacía', font=ImageFont.truetype(FS, int(14 * sc * S)), fill=(171, 165, 210, 217), anchor='mm')
    if k < 1.12:
        off = max(0.0, k) * HR * 1.04 * sc * S
        ux, uy = math.sin(a), -math.cos(a)        # el eje «tangente» del contenedor (y arriba en el sprite), en coordenadas de imagen
        door = f.spr('bod_door').resize((int(2 * (HR + 1.5) * sc * S),) * 2, Image.LANCZOS)
        ang_ccw = -math.degrees(a)
        da = door.rotate(ang_ccw, resample=Image.BICUBIC, expand=True)
        db = door.rotate(ang_ccw + 180, resample=Image.BICUBIC, expand=True)
        layer.alpha_composite(da, (int(c + ux * off - da.width / 2), int(c + uy * off - da.height / 2)))
        layer.alpha_composite(db, (int(c - ux * off - db.width / 2), int(c - uy * off - db.height / 2)))
        if 0.02 < k < 0.35:
            d = ImageDraw.Draw(layer, 'RGBA')
            d.line([(c - math.cos(a) * HR * sc * S, c - math.sin(a) * HR * sc * S), (c + math.cos(a) * HR * sc * S, c + math.sin(a) * HR * sc * S)], fill=(255, 231, 168, int(255 * (1 - k / .35))), width=max(1, int(3 * sc * S)))
    mask = Image.new('L', (n, n), 0)
    ImageDraw.Draw(mask).ellipse([c - HR * sc * S, c - HR * sc * S, c + HR * sc * S, c + HR * sc * S], fill=255)
    layer.putalpha(ImageChops.multiply(layer.split()[3], mask))
    lx, ly = f.L(x, y)
    f.im.alpha_composite(layer, (int(lx * S - n / 2), int(ly * S - n / 2)))
    f.put('bod_hrim', x, y, 2 * (32 + 2.5), 2 * (32 + 2.5))


def draw_robot(f, rx, ry, look, lean=0.0, ant=0.0, bob=0.0, squash=0.0):
    sc = f.m['scale']
    lx_, ly_ = f.L(rx, ry + 32)
    d = ImageDraw.Draw(f.im, 'RGBA')
    ew, eh = 22 * sc * S, 5 * sc * S
    d.ellipse([lx_ * S - ew, ly_ * S - eh, lx_ * S + ew, ly_ * S + eh], fill=(0, 0, 0, 90))
    # cuerpo + visor + ojos en una capa que se inclina
    n = int(2 * 60 * sc * S)
    layer = Image.new('RGBA', (n, n), (0, 0, 0, 0))
    c = n / 2
    body = f.spr('bod_robot').resize((int(2 * (26 + 3.5) * sc * S),) * 2, Image.LANCZOS)
    layer.alpha_composite(body, (int(c - body.width / 2), int(c - body.height / 2)))
    lk = look - lean
    lx, ly = math.cos(lk) * 9, math.sin(lk) * 7
    vx, vy = lx * .6, ly * .6 - 2
    dl = ImageDraw.Draw(layer, 'RGBA')
    vw, vh = (15 - abs(lx) * .25) * sc * S, 9 * sc * S
    dl.ellipse([c + vx * sc * S - vw, c + vy * sc * S - vh, c + vx * sc * S + vw, c + vy * sc * S + vh], fill=(22, 58, 92, 255))
    for ex in (-5, 5):
        er = 3 * sc * S
        ex_, ey_ = c + (vx + ex) * sc * S, c + vy * sc * S
        dl.ellipse([ex_ - er, ey_ - er * 1.05, ex_ + er, ey_ + er * 1.05], fill=CYAN + (255,))
    # antena (con su retraso)
    base = (c, c - 24 * sc * S)
    tip = (base[0] + math.sin(ant) * 11 * sc * S, base[1] - math.cos(ant) * 11 * sc * S)
    dl.line([base, tip], fill=INK + (255,), width=max(1, int(2.4 * sc * S)))
    br = 4 * sc * S
    dl.ellipse([tip[0] - br, tip[1] - br, tip[0] + br, tip[1] + br], fill=GOLD + (255,))
    layer = layer.rotate(-math.degrees(lean), resample=Image.BICUBIC)
    lx2, ly2 = f.L(rx, ry + bob)
    f.im.alpha_composite(layer, (int(lx2 * S - n / 2), int(ly2 * S - n / 2)))


def draw_card(f, kind, obj=None, perfect=False):
    top = f.m['card_top']
    gold = kind == 'ask'
    f.rrect(10, top, 340, 66, 18, (20, 27, 58, 255), outline=GOLD + (178,) if gold else (142, 131, 216, 90), width=2)
    if kind in ('ask', 'store'):
        d = ImageDraw.Draw(f.im, 'RGBA')
        cx, cy = 42, top + 33
        d.ellipse([(cx - 24) * S, (cy - 24) * S, (cx + 24) * S, (cy + 24) * S], fill=(35, 43, 87, 255))
        f.put_logical('bod_obj_%d' % obj, cx, cy, 40, 40)
        f.text('Pedido' if kind == 'ask' else 'Llega a la bodega', 82, top + 19, 14, LAV, bold=False, anchor='lm')
        s = ('¿Dónde está %s?' % OBJ[obj]) if kind == 'ask' else OBJ[obj][0].upper() + OBJ[obj][1:]
        f.text(s, 82, top + 45, 19, (255, 255, 255, 255), anchor='lm')
    else:
        title, sub = {'move': ('El robot cambia una caja de lugar', 'Síguela con la vista'), 'spin': ('¡La bodega gira!', 'Fíjate dónde queda la esclusa'),
                      'watch': ('Mira dónde guarda cada cosa', '2 objetos')}[kind]
        f.text(title, 180, top + 22, 18, TEXT)
        f.text(sub, 180, top + 47, 14, LAV, bold=False)


def draw_tray(f, objs, record=None):
    n = len(objs)
    f.text('Carro de reparto', 14, f.m['tray_label_y'], 14, LAV, bold=False, anchor='lm')
    if record:
        f.text('Récord: %d objetos' % record, 346, f.m['tray_label_y'], 14, LAV, bold=False, anchor='rm')
    d = ImageDraw.Draw(f.im, 'RGBA')
    w = n * 38 + (n - 1) * 8
    for i, st in enumerate(objs):
        x = 180 - w / 2 + i * 46 + 19
        y = f.m['tray_y']
        landed = st is not None
        border = (MINT if st and st[1] else (171, 165, 210, 153)) if landed else (255, 255, 255, 31)
        bw = 3 if landed and st[1] else 2
        d.ellipse([(x - 19) * S, (y - 19) * S, (x + 19) * S, (y + 19) * S], fill=tuple(border[:3]) + ((border[3],) if len(border) > 3 else (255,)))
        d.ellipse([(x - 19 + bw) * S, (y - 19 + bw) * S, (x + 19 - bw) * S, (y + 19 - bw) * S], fill=(20, 27, 58, 255))
        if landed:
            f.put_logical('bod_obj_%d' % st[0], x, y, 26, 26)
            if st[1]:
                f.put_logical('bod_check', x + 11, y - 14, 12, 10, tint=MINT)


def fly_point(places, rot, to_hatch, e):
    a0 = ring_point(0, places, rot)[2]
    a1 = ring_point(to_hatch + 1, places, rot)[2]
    d = (a1 - a0 + math.pi) % (2 * math.pi) - math.pi
    ang = a0 + d * e
    r = RING - 34 * math.sin(math.pi * e)
    return CX + math.cos(ang) * r, CY + math.sin(ang) * r


def scene_guardando(raw):
    f = Frame(raw)
    places, rot = 7, 0.0
    f.text('Bodega de carga', 14, 22, 20, (255, 255, 255, 255), anchor='lm')
    draw_card(f, 'store', obj=0)
    draw_hull(f, places, rot)
    draw_airlock(f, places, rot, flash=0.55)
    contents = {1: 3, 4: 1}
    for i in range(6):
        k = 0.55 if i == 2 else 0.0
        draw_hatch(f, places, rot, i, contents.get(i) if i != 2 else 0, k=k, glow=0.0, show=False)
    px, py = fly_point(places, rot, 2, 0.45)
    rx, ry = CX + (px - CX) * .2, CY + (py - CY) * .2
    f.line((rx, ry), (px, py), 16, CYAN + (56,))
    f.line((rx, ry), (px, py), 3, (191, 233, 255, 217))
    f.glow(px, py, 52, (255, 214, 120), 0.7)
    f.put('bod_obj_0', px, py, 34, 34, rot_ccw=-8)
    draw_robot(f, rx, ry, math.atan2(py - CY, px - CX), lean=0.05, ant=-0.25, bob=1.0, squash=0.0)
    draw_tray(f, [None, None], record=3)
    return f


def scene_caja(raw):
    f = Frame(raw)
    places, rot = 11, 0.0
    draw_card(f, 'move')
    draw_hull(f, places, rot)
    draw_airlock(f, places, rot)
    contents = {0: 5, 2: 8, 5: 11, 7: 2, 8: 6}
    for i in range(10):
        draw_hatch(f, places, rot, i, contents.get(i), glow=0.55 if i == 4 else 0.0)
    ang = -math.pi / 2 + 0.7
    rx, ry = CX + math.cos(ang) * 67, CY + math.sin(ang) * 67
    sw = -0.35
    f.line((rx, ry + 18), (rx + math.sin(sw) * 34, ry + math.cos(sw) * 34 + 4 - 12), 2, (191, 233, 255, 153))
    cx, cy = rx + math.sin(sw) * 34, ry + math.cos(sw) * 34 + 4
    f.glow(cx, cy, 52, (255, 214, 120), 0.55)
    f.put('bod_crate', cx, cy, 38, 32, rot_ccw=-math.degrees(sw * .8))
    draw_robot(f, rx, ry, ang + math.pi / 2, lean=0.16, ant=-0.4, bob=-1.0)
    draw_tray(f, [None] * 5, record=4)
    return f


def scene_error(raw):
    f = Frame(raw)
    places = 11
    rot = 3 * 2 * math.pi / places
    draw_card(f, 'ask', obj=2)
    draw_hull(f, places, rot)
    draw_airlock(f, places, rot)
    contents = {0: 5, 2: 8, 4: 4, 6: 2, 8: 11}
    for i in range(10):
        if i == 4:
            draw_hatch(f, places, rot, i, contents[i], k=1.0, show=True, shake=2.0)       # la equivocada: se abrió y muestra el hongo
        elif i == 6:
            draw_hatch(f, places, rot, i, contents[i], k=0.0, glow=0.9)                   # la correcta (el farol) brilla en dorado
        else:
            draw_hatch(f, places, rot, i, contents.get(i))
    draw_robot(f, CX, CY, math.pi / 2, bob=1.4)
    draw_tray(f, [(0, True), (9, False), None, None, None], record=4)
    return f


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('raw')
    ap.add_argument('--out', default=os.path.join(ROOT, 'docs', 'previews'))
    a = ap.parse_args()
    frames = [scene_guardando(a.raw), scene_caja(a.raw), scene_error(a.raw)]
    gap = 14
    sheet = Image.new('RGBA', (3 * W * S + 4 * gap * S, H * S + 2 * gap * S), (8, 10, 32, 255))
    for i, fr in enumerate(frames):
        sheet.alpha_composite(fr.im, ((gap + i * (W + gap)) * S, gap * S))
    os.makedirs(a.out, exist_ok=True)
    path = os.path.join(a.out, 'bodega.png')
    sheet.convert('RGB').save(path)
    print('OK ->', path)


if __name__ == '__main__':
    main()
