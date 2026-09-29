# Tu galaxia con estrellas de verdad (29-sep). Ricardo aprobó el concepto de la galaxia pero no el estilo de las
# estrellas: "se ve muy artificial y de baja calidad" (eran polígonos de 4 puntas con bordes duros). Aquí la luz se
# calcula como luz: cada estrella es un punto con núcleo gaussiano + halo ancho (+ destellos en cruz solo en las más
# brillantes), colores de estrellas reales (azul-blanco a ámbar) teñidos hacia el color del área, brillo con ley de
# potencias (muchas tenues, pocas brillantes), cúmulos con reparto de Plummer y gas de nebulosa del color del área;
# el disco de la galaxia son miles de estrellas en dos brazos espirales con resplandor difuso, bulbo cálido y bandas
# de polvo. Todo se suma en luz lineal y se lleva a pantalla con mapeo de tonos (sin blancos quemados) y mezcla
# "pantalla" sobre el cielo. En la app: lo mismo en un Canvas de Compose (estrellas horneadas en un ImageBitmap por
# área + BlendMode.Plus; halo con Brush.radialGradient; en Android 13+ un RuntimeShader AGSL para el gas).
# Uso: python3 tools/previews/galaxia_hd.py  ->  docs/previews/galaxia-hd.png
import os, math, random, importlib.util
import numpy as np
from PIL import Image, ImageDraw
HERE = os.path.dirname(__file__)
spec = importlib.util.spec_from_file_location('ga', os.path.join(HERE, 'galaxia.py'))
G = importlib.util.module_from_spec(spec); spec.loader.exec_module(G)
AL, NM, NS, N, P = G.AL, G.NM, G.NS, G.N, G.P
F, dp, W = P.F, P.dp, P.W
SUN, WHITE, DIM = P.SUN, P.WHITE, P.DIM
COL, LIGHT, NAMES, ORDER = G.COL, G.LIGHT, G.NAMES, G.ORDER

TEMPS = [((165, 190, 255), 0.18), ((200, 214, 255), 0.25), ((250, 248, 255), 0.27), ((255, 236, 205), 0.2), ((255, 206, 160), 0.1)]
_KC = {}


def lin(rgb): return (np.array(rgb, np.float32) / 255) ** 2.2


def kernel(sigma):
    key = round(sigma * 4) / 4
    if key not in _KC:
        s = max(key, 0.5); r = int(math.ceil(3.2 * s)); ax = np.arange(-r, r + 1, dtype=np.float32)
        g = np.exp(-ax ** 2 / (2 * s * s)); _KC[key] = (np.outer(g, g), r)
    return _KC[key]


def splat(buf, x, y, flux, col, sigma):
    k, r = kernel(sigma); h, w = buf.shape[:2]
    xi, yi = int(round(x)), int(round(y))
    x0, x1, y0, y1 = xi - r, xi + r + 1, yi - r, yi + r + 1
    if x1 <= 0 or y1 <= 0 or x0 >= w or y0 >= h: return
    kx0, ky0 = max(0, -x0), max(0, -y0); kx1, ky1 = k.shape[1] - max(0, x1 - w), k.shape[0] - max(0, y1 - h)
    buf[max(0, y0):min(h, y1), max(0, x0):min(w, x1)] += (flux * k[ky0:ky1, kx0:kx1])[..., None] * col


def spikes(buf, x, y, flux, col, L):
    h, w = buf.shape[:2]; r = int(L * 3)
    ax = np.arange(-r, r + 1, dtype=np.float32)
    line = np.exp(-np.abs(ax) / L) * flux
    xi, yi = int(round(x)), int(round(y))
    for dx, dy in ((1, 0), (0, 1)):
        for i, v in zip(ax.astype(int), line):
            px, py = xi + i * dx, yi + i * dy
            if 0 <= px < w and 0 <= py < h: buf[py, px] += v * col


def star_color(rnd, tint, mix):
    c = rnd.choices([t for t, _ in TEMPS], [p for _, p in TEMPS])[0]
    return lin(tuple(c[i] * (1 - mix) + tint[i] * mix for i in range(3)))


def star(buf, x, y, f, col, px):
    """Una estrella: núcleo + halo (+ cruz si es muy brillante). px = píxeles por dp."""
    splat(buf, x, y, f, col, 0.55 * px)
    splat(buf, x, y, f * 0.10, col, 2.4 * px)
    if f > 2.2:
        splat(buf, x, y, f * 0.03, col, 7 * px)
        spikes(buf, x, y, f * 0.05, col, 3.2 * px)


def tonemap(buf, exposure=1.0):
    return (1 - np.exp(-buf * exposure)) ** (1 / 2.2)


def feather(buf, soft=0.18):
    """Apaga suave los bordes del parche (sin esto se ven cuadrados)."""
    h, w = buf.shape[:2]
    Y, X = np.mgrid[0:h, 0:w].astype(np.float32)
    d = np.maximum(np.abs(X - w / 2) / (w / 2), np.abs(Y - h / 2) / (h / 2))
    r = np.sqrt(((X - w / 2) / (w / 2)) ** 2 + ((Y - h / 2) / (h / 2)) ** 2)
    m = np.clip((1 - np.maximum(d, r * 0.92)) / soft, 0, 1)
    return buf * (m * m * (3 - 2 * m))[..., None]


def screen_blend(im, x0, y0, rgb01, alpha=None):
    """Pega luz sobre la imagen con mezcla "pantalla" (la luz suma sin quemar)."""
    h, w = rgb01.shape[:2]
    X0, Y0 = int(x0), int(y0)
    sx0, sy0 = max(0, -X0), max(0, -Y0)
    X1, Y1 = min(im.size[0], X0 + w), min(im.size[1], Y0 + h)
    if X1 <= max(0, X0) or Y1 <= max(0, Y0): return
    region = im.crop((max(0, X0), max(0, Y0), X1, Y1)).convert('RGBA')
    bg = np.asarray(region, np.float32) / 255
    s = rgb01[sy0:sy0 + (Y1 - max(0, Y0)), sx0:sx0 + (X1 - max(0, X0))]
    out = bg.copy(); out[..., :3] = 1 - (1 - bg[..., :3]) * (1 - s)
    im.paste(Image.fromarray((np.clip(out, 0, 1) * 255).astype(np.uint8)), (max(0, X0), max(0, Y0)))


def gas(w, h, cx, cy, rad, col, amount, seed):
    n = NM.fbm(w, h, seed, 5, 4)
    Y, X = np.mgrid[0:h, 0:w].astype(np.float32)
    d = np.sqrt(((X - cx) / rad) ** 2 + ((Y - cy) / (rad * 0.85)) ** 2)
    m = np.clip(1 - d, 0, 1) ** 1.6 * np.clip((n - 0.3) * 1.8, 0, 1)
    return m[..., None] * col * amount


def cluster(im, x, y, key, scale, depth, selected=False, bright=None):
    b = G.BRIGHT[key] if bright is None else bright
    stage = G.STAGE[key]
    px = dp(1)
    R = dp(40) * scale; size = int(R * 2.6)
    buf = np.zeros((size, size, 3), np.float32); c = size / 2
    rnd = random.Random(hash(key) % 1009 + int(scale * 100))
    tint = LIGHT[key]
    # gas del color del área (menos gas si está apagada)
    buf += gas(size, size, c, c, R * 1.05, lin(COL[key]), 0.25 + 0.75 * b, hash(key) % 97)
    buf += gas(size, size, c, c, R * 0.6, lin(LIGHT[key]), 0.08 + 0.25 * b, hash(key) % 89 + 5)
    n = 26 + stage * 22
    a = dp(8.5) * scale * (0.8 + 0.1 * stage)
    for i in range(n):
        u = max(1e-3, rnd.random())
        r = min(a / math.sqrt(max(u ** (-2 / 3) - 1, 1e-3)), R)
        th = rnd.random() * 2 * math.pi
        sx, sy = c + math.cos(th) * r, c + math.sin(th) * r * 0.85
        f = min(0.25 * rnd.random() ** -0.55, 3.5) * (0.25 + 0.95 * b) * (0.7 + 0.5 * depth)
        star(buf, sx, sy, f, star_color(rnd, tint, 0.85), px * (0.8 + 0.3 * scale))
    # el corazón del cúmulo
    star(buf, c, c, (2.6 + stage * 0.5) * (0.3 + 0.9 * b), lin(tuple(min(255, t + 25) for t in tint)), px)
    rgb = tonemap(feather(buf), 1.0)
    if b < 0.4:   # apagada: un poco más gris
        g = rgb.mean(axis=2, keepdims=True); rgb = rgb * 0.6 + g * 0.4
    screen_blend(im, x - c, y - c, rgb)
    if selected:
        AL.ov(im, lambda d: d.ellipse([x - R * 0.95, y - R * 0.8, x + R * 0.95, y + R * 0.8], outline=SUN + (200,), width=int(dp(1.6))))


def galaxy(im, cx, cy, rx, ry, rot, sel=None, stars=True, labels=True, only=None, seed=3):
    rnd = random.Random(seed)
    pad = dp(20)
    w, h = int(rx * 2 + pad * 2), int(ry * 2 + pad * 2)
    ox, oy = cx - w / 2, cy - h / 2
    buf = np.zeros((h, w, 3), np.float32)
    lo = np.zeros((h // 4 + 1, w // 4 + 1, 3), np.float32)
    lc, lx, ly = w / 2, w / 2, h / 2
    if stars:
        for k in range(5200):
            if rnd.random() < 0.72:
                t = rnd.random() ** 0.8; arm = rnd.randint(0, 1)
                r = 0.06 + 0.98 * t
                th = math.radians(rot) + arm * math.pi + t * 3.9 + rnd.gauss(0, 0.28 * (1 - 0.4 * t))
                warm = 1 - t
            else:
                r = abs(rnd.gauss(0, 0.45)); th = rnd.random() * 2 * math.pi; warm = 0.8
            sx, sy = lx + math.cos(th) * r * rx, ly + math.sin(th) * r * ry
            col = lin((255, 226, 190)) if rnd.random() < warm * 0.8 else star_color(rnd, (190, 200, 255), 0.3)
            f = min(0.10 * rnd.random() ** -0.6, 1.6)
            splat(buf, sx, sy, f, col, 0.5 * dp(1))
            if f > 0.6: splat(buf, sx, sy, f * 0.08, col, 2 * dp(1))
            if k % 3 == 0: splat(buf, sx, sy, 0.012, col, 7 * dp(1))
        # bulbo cálido
        Y, X = np.mgrid[0:h, 0:w].astype(np.float32)
        bul = np.exp(-(((X - lx) / (rx * 0.2)) ** 2 + ((Y - ly) / (ry * 0.22)) ** 2))
        buf += bul[..., None] * lin((255, 222, 180)) * 0.8
        # bandas de polvo (oscurecen un poco los brazos)
        nx, ny = (X - lx) / rx, (Y - ly) / ry
        rr = np.sqrt(nx ** 2 + ny ** 2) + 1e-4; th = np.arctan2(ny, nx) - math.radians(rot)
        lane = np.cos(2 * (th - (rr - 0.06) * 3.9 / 0.98) - 0.9) * 0.5 + 0.5
        dust = np.clip(lane - 0.55, 0, 1) * 1.8 * np.clip(1 - rr, 0, 1) * NM.fbm(w, h, 13, 4, 6)
        buf *= (1 - np.clip(dust, 0, 0.7))[..., None]
    screen_blend(im, ox, oy, tonemap(feather(buf, 0.12), 0.95))
    pos = {}; items = []
    for i, key in enumerate(ORDER):
        th = math.radians(rot + i * 60 + 90)
        x, y = cx + math.cos(th) * 0.8 * rx, cy + math.sin(th) * 0.8 * ry
        items.append(((math.sin(th) + 1) / 2, key, x, y))
    for depth, key, x, y in sorted(items):
        if only and key not in only: continue
        s = 0.72 + 0.55 * depth
        cluster(im, x, y, key, s, depth, sel == key)
        pos[key] = (x, y, depth)
        if labels:
            d = ImageDraw.Draw(im)
            d.text((x, y + dp(30) * s + dp(4)), NAMES[key], font=F(14 if depth > 0.45 else 13),
                   fill=SUN if sel == key else (WHITE if depth > 0.35 else DIM), anchor='ma')
    return pos


G.cluster = cluster
G.galaxy = galaxy


def compare():
    """Antes (estrellas de polígono) y ahora (luz), el mismo cúmulo."""
    w, h = W, dp(210)
    im = NM.nebula_bg(int(w), int(h), 91, stars=40)
    d = ImageDraw.Draw(im)
    for i, (lab, fn) in enumerate([('antes', 'old'), ('ahora', 'new')]):
        x = w * (0.27 + 0.46 * i)
        if fn == 'old': G_old_cluster(im, x, dp(92), 'memoria', 1.5, 1.0)
        else: cluster(im, x, dp(92), 'memoria', 1.5, 1.0)
        d = ImageDraw.Draw(im); d.text((x, dp(176)), lab, font=F(15), fill=SUN if i else DIM, anchor='mm')
    return im


def sheet():
    scr = G.screen()
    cmp_ = compare()
    fw, fh = (W - dp(52)) / 2, dp(270)
    H = dp(84) + cmp_.size[1] + dp(24) + scr.size[1] + dp(60) + 2 * (fh + dp(12)) + dp(40)
    im = Image.new('RGBA', (W, int(H)), (10, 10, 30, 255)); d = ImageDraw.Draw(im)
    d.text((dp(20), dp(26)), 'Tu galaxia · estrellas de luz', font=F(24), fill=SUN)
    d.text((dp(20), dp(58)), 'Cada estrella con núcleo, halo y su color; gas de nebulosa por área.', font=F(13, False), fill=DIM)
    y = dp(84)
    AL.paste_round(im, cmp_, 0, y, dp(24)); y += cmp_.size[1] + dp(24)
    AL.paste_round(im, scr, 0, y, dp(24)); y += scr.size[1] + dp(24)
    d = ImageDraw.Draw(im); d.text((dp(20), y), 'Cómo se comporta', font=F(18), fill=SUN); y += dp(36)
    for i, (fn, cap, sub) in enumerate(G.FRAMES):
        fr = G.frame(fw, fh, 70 + i, fn, cap, sub)
        AL.paste_round(im, fr, dp(20) + (i % 2) * (fw + dp(12)), y + (i // 2) * (fh + dp(12)))
    out = os.path.join(P.OUT, 'galaxia-hd.png'); im.convert('RGB').save(out); print(out)


# el cúmulo viejo, para la comparación
spec2 = importlib.util.spec_from_file_location('ga_old', os.path.join(HERE, 'galaxia.py'))
_old = importlib.util.module_from_spec(spec2); spec2.loader.exec_module(_old)
G_old_cluster = _old.cluster

if __name__ == '__main__':
    sheet()
