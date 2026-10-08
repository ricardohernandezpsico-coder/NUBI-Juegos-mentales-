"""Lámina de «Constelaciones» (7-oct-2026): los 12 objetos, los 4 gemelos, las luces y tres cielos de muestra, con los sprites REALES horneados (tools/art-preview) y el reparto y las líneas que calcula el código
(ConstelacionLayout / ConstelacionSky).

Uso:  python tools/art-preview/constelaciones.py <raw> [--out docs/previews]
      (<raw> = la carpeta que vuelca ArtPreview: `dotnet run --project tools/art-preview -- <raw>`; con solo el runtime 10 de .NET: DOTNET_ROLL_FORWARD=LatestMajor)
Genera docs/previews/constelaciones.png:
  arriba, los 12 objetos y, abajo de ellos, los 4 gemelos junto a su original (el detalle que los distingue es grande y de forma: anillo, llama, patas, antena);
  abajo, tres cielos (etapas 7, 13 y 18) a mitad de partida: luces dormidas, una pareja o trío abierto, y las ya unidas con su aro y su línea (dorada continua = de memoria; celeste punteada = a la primera vista).
No es una captura del juego: son los sprites reales en las posiciones que calcula el código; el volteo con resorte no se ve en una imagen quieta.
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
OBJ = ['planeta', 'cohete', 'cometa', 'ovni', 'cristal', 'casco', 'luna llena', 'satélite', 'telescopio', 'asteroide', 'antena', 'brújula']
TWINS = [(0, 'sin anillo'), (1, 'con llama grande'), (3, 'con patas'), (5, 'con antena')]
GOLD = (255, 201, 74)
CYAN = (127, 216, 255)
LAV = (171, 165, 210)
TEXT = (237, 234, 251)
BAKE_R, OBJ_BOX, DORMANT_BOX, RING_BOX = 38.0, 56.0, 84.0, 100.0


def load(raw, name):
    b = open(os.path.join(raw, name + '.raw'), 'rb').read()
    n = struct.unpack('<i', b[:4])[0]
    w = -n
    h = struct.unpack('<i', b[4:8])[0]
    return Image.frombytes('RGBA', (w, h), b[8:8 + w * h * 4]).transpose(Image.FLIP_TOP_BOTTOM)


def font(size, bold=True):
    return ImageFont.truetype(FB if bold else FS, int(size * S))


def paste_center(img, sprite, cx, cy, wdp):
    px = max(1, int(round(wdp * S)))
    sp = sprite.resize((px, px), Image.LANCZOS)
    img.alpha_composite(sp, (int(round(cx * S - px / 2)), int(round(cy * S - px / 2))))


def night(w, h):
    img = Image.new('RGBA', (w * S, h * S), (7, 10, 38, 255))
    d = ImageDraw.Draw(img)
    for y in range(h * S):
        t = y / (h * S)
        d.line([(0, y), (w * S, y)], fill=(int(3 + 8 * t), int(4 + 12 * t), int(26 + 22 * t), 255))
    import random
    r = random.Random(7)
    for _ in range(w * h // 700):
        x, y = r.random() * w * S, r.random() * h * S
        rad = 0.5 + r.random() * 1.2
        a = int(60 + r.random() * 120)
        d.ellipse([x - rad, y - rad, x + rad, y + rad], fill=(255, 255, 255, a))
    return img


def qpt(a, b, c, t):
    u = 1 - t
    return (u * u * a[0] + 2 * u * t * c[0] + t * t * b[0], u * u * a[1] + 2 * u * t * c[1] + t * t * b[1])


def polyline(img, pts, color, width, dash=None):
    """Una línea gruesa de puntas redondas; con dash = (trazo, hueco) en dp va punteada."""
    layer = Image.new('RGBA', img.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    w = max(1, int(width * S))
    if dash is None:
        segs = [pts]
    else:
        segs, cur, on, acc = [], [], True, 0.0
        for i in range(len(pts) - 1):
            (x0, y0), (x1, y1) = pts[i], pts[i + 1]
            seg = math.hypot(x1 - x0, y1 - y0)
            pos = 0.0
            while pos < seg:
                lim = (dash[0] if on else dash[1]) - acc
                step = min(lim, seg - pos)
                if on:
                    if not cur:
                        cur = [(x0 + (x1 - x0) * pos / seg, y0 + (y1 - y0) * pos / seg)]
                    cur.append((x0 + (x1 - x0) * (pos + step) / seg, y0 + (y1 - y0) * (pos + step) / seg))
                pos += step
                acc += step
                if acc >= (dash[0] if on else dash[1]) - 1e-6:
                    if on and cur:
                        segs.append(cur)
                        cur = []
                    on = not on
                    acc = 0.0
        if cur:
            segs.append(cur)
    for sgm in segs:
        if len(sgm) < 2:
            continue
        xy = [(x * S, y * S) for x, y in sgm]
        d.line(xy, fill=color, width=w, joint='curve')
        if dash is None:
            for x, y in (xy[0], xy[-1]):
                d.ellipse([x - w / 2, y - w / 2, x + w / 2, y + w / 2], fill=color)
    img.alpha_composite(layer)


def draw_links(img, links, ox, oy):
    for (ax, ay, bx, by, cx, cy, t0, t1, typ) in links:
        a, b, c = (ax + ox, ay + oy), (bx + ox, by + oy), (cx + ox, cy + oy)
        pts = [qpt(a, b, c, t0 + (t1 - t0) * i / 40) for i in range(41)]
        if typ == 1:     # de memoria
            polyline(img, pts, (255, 201, 74, 70), 12)
            polyline(img, pts, (11, 10, 38, 190), 7.5)
            polyline(img, pts, (255, 201, 74, 255), 4)
        else:
            polyline(img, pts, (127, 216, 255, 40), 9)
            polyline(img, pts, (11, 10, 38, 178), 6.5, dash=(7, 7))
            polyline(img, pts, (191, 233, 255, 230), 3, dash=(7, 7))


def sky_panel(raw, stage, title, w=360, h=720):
    lines = open(os.path.join(raw, f'con_sky_{stage}.txt'), encoding='utf-8').read().splitlines()
    head = lines[0].split()
    sw, sh, r = float(head[1]), float(head[2]), float(head[3])
    lights, links = [], []
    for ln in lines[1:]:
        t = ln.split()
        if t[0] == 'light':
            lights.append(dict(id=int(t[1]), x=float(t[2]), y=float(t[3]), kind=int(t[4]), var=int(t[5]), state=int(t[6]), ring=int(t[7]), seen=int(t[8]), size=int(t[9])))
        else:
            links.append(tuple(float(v) for v in t[1:9]) + (int(t[9]),))
    img = night(w, h)
    d = ImageDraw.Draw(img)
    ox, oy = (w - sw) / 2, 130.0
    # arriba: el marcador y la tarjeta
    d.text((14 * S, 24 * S), 'Cielo 3 de 6', font=font(17), fill=TEXT)
    d.rounded_rectangle([10 * S, 58 * S, (w - 10) * S, 118 * S], radius=18 * S, fill=(20, 27, 58, 255), outline=(142, 131, 216, 90), width=2)
    d.text((w / 2 * S, 80 * S), title[0], font=font(18), fill=TEXT, anchor='mm')
    d.text((w / 2 * S, 101 * S), title[1], font=font(14, False), fill=LAV, anchor='mm')
    dormant, opn = load(raw, 'con_dormant'), load(raw, 'con_open')
    ringm, ringn = load(raw, 'con_ringmem'), load(raw, 'con_ringnew')
    k = r / BAKE_R
    draw_ring = lambda l: (ringm if l['ring'] == 1 else ringn)
    for l in lights:
        cx, cy = l['x'] + ox, l['y'] + oy
        if l['state'] == 2:
            paste_center(img, draw_ring(l), cx, cy, RING_BOX * k)
        paste_center(img, opn if l['state'] else dormant, cx, cy, DORMANT_BOX * k)
        if l['state']:
            ob = load(raw, f"con_obj_{l['kind']}_{l['var']}")
            paste_center(img, ob, cx, cy, OBJ_BOX * (r * 1.32 / 40.0))
    draw_links(img, links, ox, oy)
    d = ImageDraw.Draw(img)
    # la fila de abajo
    ry = oy + sh + 16
    d.text((16 * S, ry * S), 'De memoria', font=font(14, False), fill=LAV, anchor='lm')
    d.text(((w - 16) * S, ry * S), '3 de 4', font=font(15), fill=TEXT, anchor='rm')
    for i, hit in enumerate([1, 1, 0, 1]):
        x, y = (16 + 8 + i * 22) * S, (ry + 20) * S
        if hit:
            d.ellipse([x - 7 * S, y - 7 * S, x + 7 * S, y + 7 * S], fill=GOLD, outline=(26, 18, 64, 255), width=3)
        else:
            d.ellipse([x - 6 * S, y - 6 * S, x + 6 * S, y + 6 * S], outline=LAV, width=4)
    return img


def gallery(raw, w):
    pad = 14
    cell = 86
    cols = 4
    rows = 3 + 1
    img = night(w, 800)
    d = ImageDraw.Draw(img)
    d.text((pad * S, 20 * S), 'Los 12 objetos del espacio', font=font(17), fill=TEXT, anchor='lm')
    opn = load(raw, 'con_open')
    r = 33.0
    for i in range(12):
        x = 50 + (i % cols) * 87
        y = 70 + (i // cols) * 112
        paste_center(img, opn, x, y, DORMANT_BOX * r / BAKE_R)
        paste_center(img, load(raw, f'con_obj_{i}_0'), x, y, OBJ_BOX * (r * 1.32 / 40.0))
        d = ImageDraw.Draw(img)
        d.text((x * S, (y + 48) * S), OBJ[i], font=font(13, False), fill=LAV, anchor='mm')
    y0 = 70 + 3 * 112 + 4
    d.text((pad * S, (y0 - 8) * S), 'Gemelos: cambia un detalle grande', font=font(15), fill=TEXT, anchor='lm')
    for j, (k, note) in enumerate(TWINS):
        x = 48 + j * 87
        for v in (0, 1):
            y = y0 + 48 + v * 84
            paste_center(img, opn, x, y, DORMANT_BOX * 26.0 / BAKE_R)
            paste_center(img, load(raw, f'con_obj_{k}_{v}'), x, y, OBJ_BOX * (26.0 * 1.32 / 40.0))
        d = ImageDraw.Draw(img)
        d.text((x * S, (y0 + 48 + 84 + 34) * S), note, font=font(12, False), fill=LAV, anchor='mm')
    return img.crop((0, 0, w * S, int((y0 + 48 + 84 + 52) * S)))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('raw')
    ap.add_argument('--out', default=os.path.join(ROOT, 'docs', 'previews'))
    a = ap.parse_args()
    W = 360
    g = gallery(a.raw, W)
    panels = [
        sky_panel(a.raw, 7, ('Ojo con los gemelos', 'Solo se unen los idénticos · faltan 3 parejas')),
        sky_panel(a.raw, 13, ('Parejas y tríos', 'Faltan 3 parejas y 1 trío')),
        sky_panel(a.raw, 18, ('Gemelos, parejas y tríos', 'Faltan 5 parejas y 1 trío')),
    ]
    gap = 24 * S
    total_w = max(g.width, 3 * panels[0].width + 2 * gap)
    total_h = g.height + gap + panels[0].height
    sheet = Image.new('RGBA', (total_w, total_h), (3, 4, 20, 255))
    sheet.alpha_composite(g, ((total_w - g.width) // 2, 0))
    for i, p in enumerate(panels):
        sheet.alpha_composite(p, (i * (p.width + gap), g.height + gap))
    os.makedirs(a.out, exist_ok=True)
    out = os.path.join(a.out, 'constelaciones.png')
    sheet.convert('RGB').save(out)
    print('OK', out, sheet.size)


if __name__ == '__main__':
    main()
