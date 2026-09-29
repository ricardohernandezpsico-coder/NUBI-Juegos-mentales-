# Hoy con el mundo de Nubi (29-sep). Ricardo trajo una lámina de referencia y pidió tomar más de ella: el planeta con
# paisajes (valle, picos, mar), el fondo de nebulosas de colores, los satélites y la gráfica de la semana. Maqueta PIL,
# NO la app: la pantalla Hoy rehecha con eso, conservando lo que ya funciona (cada zona = un dominio y crece con cada
# partida; las 3 partidas del día orbitan; el descubrimiento del día; sin recuadros para informar).
#   - Fondo: nebulosas de colores (uva, turquesa, ámbar) con ruido fractal, sobre la noche de la app.
#   - Planeta: océano con continentes; cada dominio es un PAISAJE con nombre: Valle de la Memoria (cristales), Picos de
#     la Atención (montañas con faro), Mar del Razonamiento (islas con torres), Bosque del Lenguaje (árboles), Domos del
#     Cálculo (arena con domos) y Meseta de la Velocidad (antenas). Atmósfera que brilla, luz de arriba a la izquierda.
#   - Órbita: las 3 partidas del día son SATÉLITES (color del dominio); los jugados llevan ✓.
#   - Nubi flota junto al planeta y dice la zona del día.
#   - Tu semana: gráfica de partidas por día con Nubi; mejoras del planeta que se ganan jugando.
# Uso: python3 tools/previews/nubi_mundo.py  ->  docs/previews/hoy-mundo-nubi.png
import os, math, random, importlib.util
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
HERE = os.path.dirname(__file__)
spec = importlib.util.spec_from_file_location('ns', os.path.join(HERE, 'nubi_suave.py'))
NS = importlib.util.module_from_spec(spec); spec.loader.exec_module(NS)
N, P = NS.N, NS.P
F, dp, W = P.F, P.dp, P.W
INK, SUN, CREAM, WHITE, DIM = P.INK, P.SUN, P.CREAM, P.WHITE, P.DIM
LIME, CORAL, SKY, GRAPE = P.LIME, P.CORAL, P.SKY, P.GRAPE
LINE = NS.LINE
H = dp(1180)


# ---------- ruido fractal ----------
def fbm(w, h, seed, octaves=5, base=4):
    rng = np.random.default_rng(seed); out = np.zeros((h, w), np.float32); amp, tot = 1.0, 0.0
    for o in range(octaves):
        n = base * 2 ** o
        g = rng.random((n + 1, n + 1)).astype(np.float32)
        im = Image.fromarray((g * 255).astype(np.uint8)).resize((w, h), Image.BICUBIC)
        out += np.asarray(im, np.float32) / 255 * amp; tot += amp; amp *= 0.5
    return out / tot


def to_img(alpha, rgb):
    lay = np.zeros(alpha.shape + (4,), np.uint8); lay[..., :3] = rgb
    lay[..., 3] = (np.clip(alpha, 0, 1) * 255).astype(np.uint8); return Image.fromarray(lay)


# ---------- fondo de nebulosas ----------
def nebula_bg(w, h, seed=5):
    t = np.linspace(0, 1, h)[:, None, None]
    top, bot = np.array([14, 16, 60.]), np.array([6, 7, 26.])
    bg = np.repeat(top + (bot - top) * t, w, axis=1)
    Y, X = np.mgrid[0:h, 0:w].astype(np.float32)
    for i, (cx, cy, r, rgb, a) in enumerate([(0.1, 0.1, 0.7, (130, 70, 220), 0.85), (0.95, 0.22, 0.6, (30, 170, 185), 0.75),
                                            (0.05, 0.42, 0.5, (235, 120, 60), 0.5), (0.95, 0.55, 0.55, (120, 70, 215), 0.6),
                                            (0.15, 0.8, 0.6, (30, 160, 175), 0.5), (0.9, 0.92, 0.5, (230, 110, 70), 0.4)]):
        n = fbm(w, h, seed + i, 6, 3)
        d = np.clip(1 - np.sqrt((X - cx * w) ** 2 + (Y - cy * h) ** 2) / (r * w * 1.5), 0, 1)
        m = np.clip((n - 0.33) * 2.2, 0, 1) * d ** 1.2 * a
        bg = bg * (1 - m[..., None]) + np.array(rgb) * m[..., None]
    im = Image.fromarray(bg.clip(0, 255).astype(np.uint8)).convert('RGBA')
    d = ImageDraw.Draw(im); rnd = random.Random(seed)
    for _ in range(220):
        x, y, r = rnd.random() * w, rnd.random() * h, rnd.random() * 1.8 + 0.6
        d.ellipse([x - r, y - r, x + r, y + r], fill=(255, 255, 255, int(70 + rnd.random() * 170)))
    for _ in range(10):
        x, y = rnd.random() * w, rnd.random() * h
        d.polygon(N.sparkle(x, y, dp(3 + rnd.random() * 3)), fill=(255, 255, 255, 200))
    return im


# ---------- el planeta ----------
PS = 1100; PC = PS / 2; PR = 400
ZONES = {   # centro en coordenadas del disco (-1..1), tamaño, color de tierra, nombre
    'memoria':      (-0.46, -0.30, 0.34, (120, 205, 130), 'Valle de la\nMemoria'),
    'atencion':     (0.30, -0.52, 0.32, (150, 138, 170), 'Picos de la\nAtención'),
    'lenguaje':     (0.56, 0.08, 0.28, (70, 165, 105), 'Bosque del\nLenguaje'),
    'calculo':      (-0.52, 0.34, 0.28, (228, 196, 128), 'Domos del\nCálculo'),
    'velocidad':    (0.02, -0.02, 0.22, (226, 128, 96), 'Meseta de la\nVelocidad'),
    'razonamiento': (0.18, 0.56, 0.16, (120, 200, 140), 'Mar del\nRazonamiento'),
}


def planet_img(grow):
    """Planeta con paisajes. grow: crecimiento 0..1 por dominio (más cristales, árboles, domos...)."""
    Y, X = np.mgrid[0:PS, 0:PS].astype(np.float32)
    nx, ny = (X - PC) / PR, (Y - PC) / PR
    r = np.sqrt(nx ** 2 + ny ** 2)
    disc = np.clip((1 - r) * PR / 1.5, 0, 1)
    im = Image.new('RGBA', (PS, PS), (0, 0, 0, 0))
    # atmósfera
    im.alpha_composite(to_img(np.asarray(Image.fromarray((np.clip((1.06 - r) * 30, 0, 1) * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(26)), np.float32) / 255 * 0.8, (110, 200, 255)))
    # océano
    t = np.clip((nx * 0.5 + ny * 0.7 + 1) / 2, 0, 1)[..., None]
    ocean = np.array([60, 150, 225.]) + (np.array([26, 64, 160.]) - np.array([60, 150, 225.])) * t
    lay = np.zeros((PS, PS, 4), np.float32); lay[..., :3] = ocean; lay[..., 3] = disc * 255
    im.alpha_composite(Image.fromarray(lay.astype(np.uint8)))
    # tierras: una mancha con ruido por zona
    noise = fbm(PS, PS, 21, 5, 5)
    land = np.zeros((PS, PS), np.float32); col = np.zeros((PS, PS, 3), np.float32)
    for key, (zx, zy, zr, rgb, _) in ZONES.items():
        dz = np.sqrt((nx - zx) ** 2 + (ny - zy) ** 2) / zr
        m = np.clip((1 - dz + (noise - 0.5) * 0.9) * 8, 0, 1)
        if key == 'razonamiento':   # el mar: sin tierra (islas dibujadas aparte)
            continue
        new = m * (1 - land)
        col += new[..., None] * np.array(rgb, np.float32); land += new
    land *= disc
    # costa: agua clara alrededor de las tierras
    shore = np.asarray(Image.fromarray((land * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(12)), np.float32) / 255
    im.alpha_composite(to_img(np.clip(shore * 1.8, 0, 1) * (1 - land) * disc * 0.55, (120, 215, 240)))
    lay = np.zeros((PS, PS, 4), np.float32); lay[..., :3] = col / np.maximum(land, 1e-3)[..., None]; lay[..., 3] = land * 255
    im.alpha_composite(Image.fromarray(np.clip(lay, 0, 255).astype(np.uint8)))
    # textura de la tierra (claros y oscuros suaves)
    tex = fbm(PS, PS, 33, 4, 12)
    im.alpha_composite(to_img(np.clip(tex - 0.55, 0, 1) * 1.6 * land * 0.5, (255, 255, 230)))
    im.alpha_composite(to_img(np.clip(0.45 - tex, 0, 1) * 1.6 * land * 0.35, (30, 60, 40)))
    features(im, grow)
    cl = fbm(PS, PS, 57, 5, 6)
    wisp = np.clip((cl - 0.6) * 4, 0, 1) * disc * np.clip(np.abs(np.sin(ny * 7 + nx * 2)) * 1.4, 0, 1)
    im.alpha_composite(to_img(wisp * 0.55, (250, 252, 255)))
    # sombra (terminador) y luz
    shade = np.clip((nx * 0.55 + ny * 0.65 + 0.15) * 1.1, 0, 1) ** 1.3 * disc
    im.alpha_composite(to_img(shade * 0.55, (10, 12, 50)))
    rim = np.clip(1 - np.abs(r - 0.97) * 25, 0, 1) * np.clip(-(nx + ny) * 0.8, 0, 1)
    im.alpha_composite(to_img(rim * 0.7, (200, 240, 255)))
    hl = np.clip(1 - np.sqrt((nx + 0.45) ** 2 + (ny + 0.5) ** 2) / 0.5, 0, 1) ** 2 * disc
    im.alpha_composite(to_img(hl * 0.25, (255, 255, 255)))
    d = ImageDraw.Draw(im)
    d.ellipse([PC - PR, PC - PR, PC + PR, PC + PR], outline=LINE, width=6)
    return im


def P2(zx, zy): return PC + zx * PR, PC + zy * PR


def outline_poly(d, pts, fill, w=4):
    d.polygon(pts, fill=fill, outline=LINE, width=w)


def crystal(d, x, y, s, col):
    for dx, h, ww, c in [(-0.55, 0.8, 0.32, col), (0.5, 0.7, 0.3, (170, 150, 255)), (0, 1.15, 0.38, col)]:
        cx = x + dx * s; top = y - h * s * 1.6
        pts = [(cx - ww * s, y), (cx - ww * s, top + ww * s), (cx, top), (cx + ww * s, top + ww * s), (cx + ww * s, y)]
        outline_poly(d, pts, c)
        d.line([(cx - ww * s * 0.4, top + ww * s + 6), (cx - ww * s * 0.4, y - 8)], fill=(235, 250, 255), width=6)


def mountain(d, x, y, s, snow=True):
    pts = [(x - s, y), (x, y - s * 1.3), (x + s, y)]
    outline_poly(d, pts, (132, 120, 158))
    d.polygon([(x, y - s * 1.3), (x + s * 0.55, y - s * 0.6), (x + s, y)], fill=(104, 94, 136))
    if snow:
        d.polygon([(x - s * 0.34, y - s * 0.86), (x, y - s * 1.3), (x + s * 0.34, y - s * 0.86), (x + s * 0.12, y - s * 0.95), (x - s * 0.1, y - s * 0.84)], fill=(245, 248, 255))
    d.line(pts + [pts[0]], fill=LINE, width=6)


def lighthouse(d, x, y, s):
    outline_poly(d, [(x - s * 0.25, y), (x - s * 0.16, y - s), (x + s * 0.16, y - s), (x + s * 0.25, y)], CREAM)
    d.rectangle([x - s * 0.2, y - s * 0.62, x + s * 0.2, y - s * 0.46], fill=CORAL)
    d.ellipse([x - s * 0.22, y - s * 1.3, x + s * 0.22, y - s * 0.94], fill=SUN, outline=LINE, width=5)


def tree(d, x, y, s, col):
    d.rectangle([x - s * 0.1, y - s * 0.5, x + s * 0.1, y], fill=(120, 80, 60))
    d.ellipse([x - s * 0.5, y - s * 1.4, x + s * 0.5, y - s * 0.4], fill=col, outline=LINE, width=5)
    d.ellipse([x - s * 0.28, y - s * 1.25, x - s * 0.02, y - s * 1.05], fill=(255, 255, 255, 90))


def dome(d, x, y, s):
    d.chord([x - s, y - s, x + s, y + s], 180, 360, fill=(250, 245, 230), outline=LINE, width=6)
    d.chord([x - s * 0.55, y - s * 0.7, x + s * 0.05, y - s * 0.1], 180, 360, fill=(150, 220, 250))
    d.line([(x - s, y), (x + s, y)], fill=LINE, width=6)


def antenna(d, x, y, s):
    d.line([(x, y), (x, y - s)], fill=LINE, width=5)
    d.chord([x - s * 0.45, y - s * 1.35, x + s * 0.45, y - s * 0.55], 200, 20, fill=(235, 235, 250), outline=LINE, width=5)
    d.ellipse([x - 10, y - s - 10, x + 10, y - s + 10], fill=CORAL)


def island_tower(d, x, y, s):
    d.ellipse([x - s, y - s * 0.35, x + s, y + s * 0.35], fill=(120, 200, 140), outline=LINE, width=5)
    d.rectangle([x - s * 0.22, y - s * 1.1, x + s * 0.22, y - s * 0.05], fill=(190, 170, 255), outline=LINE, width=5)
    d.polygon([(x - s * 0.3, y - s * 1.1), (x, y - s * 1.5), (x + s * 0.3, y - s * 1.1)], fill=(150, 120, 240), outline=LINE)


def features(im, grow):
    d = ImageDraw.Draw(im); rnd = random.Random(2)
    def spots(key, n, spread=0.55):
        zx, zy, zr = ZONES[key][:3]
        pts = []
        for i in range(n):
            a = rnd.random() * 2 * math.pi; rr = zr * spread * math.sqrt(rnd.random())
            pts.append(P2(zx + math.cos(a) * rr, zy + math.sin(a) * rr * 0.8))
        return sorted(pts, key=lambda p: p[1])
    for x, y in spots('memoria', 1 + int(grow['memoria'] * 4)):
        crystal(d, x, y, 46, (120, 220, 250))
    pk = spots('atencion', 2 + int(grow['atencion'] * 2))
    for i, (x, y) in enumerate(pk): mountain(d, x, y, 90 - i * 10)
    x, y = P2(0.5, -0.36); lighthouse(d, x, y, 80)
    for x, y in spots('lenguaje', 3 + int(grow['lenguaje'] * 5)):
        tree(d, x, y, 54, (60, 180 + rnd.randint(-20, 20), 100))
    for x, y in spots('calculo', 1 + int(grow['calculo'] * 3)):
        dome(d, x, y, 50)
    for x, y in spots('velocidad', 1 + int(grow['velocidad'] * 2), 0.4):
        antenna(d, x, y, 84)
    for zx, zy in [(0.08, 0.52), (0.32, 0.62)][: 1 + int(grow['razonamiento'] * 2)]:
        x, y = P2(zx, zy); island_tower(d, x, y, 52)


def satellite(im, cx, cy, s, col, done):
    d = ImageDraw.Draw(im)
    for sx in (-1, 1):
        x0 = cx + sx * s * 0.62
        box = [min(x0, x0 + sx * s * 0.9), cy - s * 0.3, max(x0, x0 + sx * s * 0.9), cy + s * 0.3]
        d.rectangle(box, fill=(70, 120, 220), outline=LINE, width=int(dp(1.5)))
        for k in range(1, 3):
            xx = box[0] + (box[2] - box[0]) * k / 3; d.line([(xx, box[1]), (xx, box[3])], fill=(150, 200, 255), width=int(dp(1)))
    d.line([(cx - s * 0.62, cy), (cx + s * 0.62, cy)], fill=LINE, width=int(dp(2)))
    d.rounded_rectangle([cx - s * 0.42, cy - s * 0.5, cx + s * 0.42, cy + s * 0.5], radius=s * 0.18, fill=col, outline=LINE, width=int(dp(2)))
    d.ellipse([cx - s * 0.2, cy - s * 0.25, cx + s * 0.2, cy + s * 0.15], fill=(230, 245, 255), outline=LINE, width=int(dp(1.2)))
    if done:
        r = s * 0.36; x, y = cx + s * 0.45, cy - s * 0.55
        d.ellipse([x - r, y - r, x + r, y + r], fill=LIME, outline=LINE, width=int(dp(1.5)))
        d.line([(x - r * 0.45, y), (x - r * 0.1, y + r * 0.4), (x + r * 0.5, y - r * 0.4)], fill=LINE, width=int(dp(2)), joint='curve')


def nubi_sprite(fn, size, scale=1.0):
    c = Image.new('RGBA', (NS.S, NS.S), (0, 0, 0, 0)); fn(c, NS.S / 2, NS.S / 2, NS.u(0.95) * scale)
    return c.resize((int(size), int(size)), Image.LANCZOS)


def bubble(im, x, y, lines, tail='left'):
    d = ImageDraw.Draw(im); f1, f2 = F(12, False), F(15)
    w = max(d.textlength(lines[0], font=f1), d.textlength(lines[1], font=f2)) + dp(24); h = dp(52)
    x0 = x - w if tail == 'right' else x
    d.rounded_rectangle([x0, y + dp(3), x0 + w, y + h + dp(3)], radius=dp(16), fill=LINE)
    d.rounded_rectangle([x0, y, x0 + w, y + h], radius=dp(16), fill=(252, 250, 255), outline=LINE, width=int(dp(2)))
    tx = x0 + dp(18) if tail == 'left' else x0 + w - dp(18)
    d.polygon([(tx - dp(7), y + h - dp(2)), (tx + dp(7), y + h - dp(2)), (tx - dp(10) if tail == 'left' else tx + dp(10), y + h + dp(12))], fill=(252, 250, 255))
    d.line([(tx - dp(7), y + h), (tx - dp(10) if tail == 'left' else tx + dp(10), y + h + dp(12)), (tx + dp(7), y + h)], fill=LINE, width=int(dp(2)))
    d.text((x0 + dp(12), y + dp(8)), lines[0], font=f1, fill=(90, 84, 130))
    d.text((x0 + dp(12), y + dp(25)), lines[1], font=f2, fill=INK)


def label(d, x, y, text):
    f = F(14)
    for i, ln in enumerate(text.split('\n')):
        yy = y + i * dp(17)
        d.text((x + dp(1.2), yy + dp(1.6)), ln, font=f, fill=(10, 10, 40, 230), anchor='ma')
        d.text((x, yy), ln, font=f, fill=WHITE, anchor='ma')


def ov(im, fn):
    lay = Image.new('RGBA', im.size, (0, 0, 0, 0)); fn(ImageDraw.Draw(lay)); im.alpha_composite(lay)


def screen():
    im = nebula_bg(W, int(H))
    P.header(im)
    d = ImageDraw.Draw(im)
    d.text((dp(20), dp(66)), 'Tu planeta', font=F(28), fill=WHITE, anchor='lm')
    grow = {'memoria': 0.9, 'atencion': 0.7, 'lenguaje': 0.3, 'calculo': 0.5, 'velocidad': 0.8, 'razonamiento': 0.5}
    pcx, pcy, prad = W / 2, dp(300), dp(138)
    # órbita, mitad de atrás
    orb = [pcx - dp(185), pcy - dp(46), pcx + dp(185), pcy + dp(46)]
    lay = Image.new('RGBA', im.size, (0, 0, 0, 0)); ld = ImageDraw.Draw(lay)
    ld.arc(orb, 180, 360, fill=(200, 230, 255, 110), width=int(dp(2)))
    im.alpha_composite(lay)
    pl = planet_img(grow).resize((int(prad * 2 * PS / (2 * PR)), int(prad * 2 * PS / (2 * PR))), Image.LANCZOS)
    im.alpha_composite(pl, (int(pcx - pl.size[0] / 2), int(pcy - pl.size[1] / 2)))
    d = ImageDraw.Draw(im); k = prad / PR
    zx, zy, zr = ZONES['atencion'][:3]
    lay = Image.new('RGBA', im.size, (0, 0, 0, 0)); ld = ImageDraw.Draw(lay)
    rr = zr * prad * 1.05
    ld.ellipse([pcx + zx * prad - rr, pcy + zy * prad - rr * 0.8, pcx + zx * prad + rr, pcy + zy * prad + rr * 0.8], outline=SUN + (230,), width=int(dp(3)))
    im.alpha_composite(lay.filter(ImageFilter.GaussianBlur(dp(1.5))))
    im.alpha_composite(lay)
    d = ImageDraw.Draw(im)
    lay = Image.new('RGBA', im.size, (0, 0, 0, 0)); ld = ImageDraw.Draw(lay)
    ld.arc(orb, 0, 180, fill=(200, 230, 255, 150), width=int(dp(2)))
    im.alpha_composite(lay)
    # los 3 juegos del día como satélites en la órbita
    for ang, col, done in [(200, P.DOM['atencion'], True), (330, P.DOM['memoria'], True), (100, P.DOM['velocidad'], False)]:
        a = math.radians(ang)
        satellite(im, pcx + math.cos(a) * dp(185), pcy + math.sin(a) * dp(46), dp(17), col, done)
    # Nubi y la zona del día
    sp = nubi_sprite(lambda c, x, y, kk: NS.nubi(c, x, y, kk, 'hola'), dp(96))
    im.alpha_composite(sp, (int(W - dp(104)), int(dp(92))))
    bubble(im, W - dp(96), dp(86), ['Zona del día', 'Picos de la Atención'], tail='right')
    # frase de crecimiento
    d = ImageDraw.Draw(im)
    d.text((W / 2, dp(470)), '¡Creció el Valle de la Memoria!', font=F(18), fill=SUN, anchor='ma')
    d.text((W / 2, dp(496)), 'Hoy: 2 de 3 satélites en órbita', font=F(14, False), fill=DIM, anchor='ma')
    # botón seguir
    y = dp(532)
    x0, x1, h = dp(20), W - dp(20), dp(56)
    d.rounded_rectangle([x0, y + dp(5), x1, y + h + dp(5)], radius=dp(20), fill=INK)
    d.rounded_rectangle([x0, y, x1, y + h], radius=dp(20), fill=SUN, outline=INK, width=int(dp(3)))
    satellite(im, x0 + dp(36), y + h / 2, dp(15), P.DOM['velocidad'], False)
    d = ImageDraw.Draw(im); d.text((x0 + dp(66), y + h / 2), 'Radar', font=F(20), fill=INK, anchor='lm')
    P.play_tri(d, x1 - dp(32), y + h / 2, dp(16))
    # Tu semana (gráfica) con Nubi
    y = dp(624)
    ov(im, lambda dd: dd.line([(dp(20), y), (W - dp(20), y)], fill=(255, 255, 255, 50), width=int(dp(1))))
    d = ImageDraw.Draw(im)
    d.text((dp(20), y + dp(18)), 'TU SEMANA', font=F(13), fill=SUN)
    sp = nubi_sprite(lambda c, x, y_, kk: NS.pose_coach(c, x, y_, kk), dp(118), 0.9)
    im.alpha_composite(sp, (int(dp(4)), int(y + dp(40))))
    d = ImageDraw.Draw(im)
    gx0, gx1, gy0, gy1 = dp(140), W - dp(28), y + dp(56), y + dp(150)
    vals = [2, 3, 0, 3, 4, 3, 5]
    for i in range(4):
        yy = gy1 - (gy1 - gy0) * i / 3; ov(im, lambda dd: dd.line([(gx0, yy), (gx1, yy)], fill=(255, 255, 255, 30), width=int(dp(1))))
    d = ImageDraw.Draw(im)
    pts = [(gx0 + (gx1 - gx0) * i / 6, gy1 - (gy1 - gy0) * v / 5) for i, v in enumerate(vals)]
    lay = Image.new('RGBA', im.size, (0, 0, 0, 0)); ld = ImageDraw.Draw(lay)
    ld.polygon(pts + [(gx1, gy1), (gx0, gy1)], fill=SKY + (45,)); im.alpha_composite(lay); d = ImageDraw.Draw(im)
    d.line(pts, fill=SKY, width=int(dp(3)), joint='curve')
    for i, (x, yy) in enumerate(pts):
        r = dp(4.5); d.ellipse([x - r, yy - r, x + r, yy + r], fill=SKY if i < 6 else SUN, outline=INK, width=int(dp(1.5)))
        d.text((x, gy1 + dp(8)), 'LMMJVSD'[i], font=F(13, False), fill=DIM if i < 6 else SUN, anchor='ma')
    d.polygon(N.sparkle(pts[-1][0], pts[-1][1] - dp(14), dp(7)), fill=SUN)
    d.text((dp(20), y + dp(186)), '20 partidas esta semana', font=F(18), fill=WHITE)
    d.text((dp(20), y + dp(212)), '4 más que la semana pasada · tu mejor semana', font=F(14, False), fill=DIM)
    # mejoras del planeta
    y = dp(870)
    ov(im, lambda dd: dd.line([(dp(20), y), (W - dp(20), y)], fill=(255, 255, 255, 50), width=int(dp(1))))
    d = ImageDraw.Draw(im)
    d.text((dp(20), y + dp(18)), 'MEJORAS DE TU PLANETA', font=F(13), fill=SUN)
    items = [('Anillo', True), ('Aurora', False), ('Luna', False), ('Cometa', False)]
    for i, (name, got) in enumerate(items):
        cx = dp(56) + i * dp(100); cy = y + dp(84)
        cosmetic(im, name, cx, cy, got)
        d = ImageDraw.Draw(im)
        d.text((cx, cy + dp(40)), name, font=F(14), fill=WHITE if got else DIM, anchor='ma')
    d.text((dp(20), y + dp(160)), 'Aurora: juega 3 días más esta semana', font=F(14, False), fill=WHITE)
    bx0, bx1, by = dp(20), W - dp(20), y + dp(190)
    ov(im, lambda dd: dd.rounded_rectangle([bx0, by, bx1, by + dp(12)], radius=dp(6), fill=(255, 255, 255, 45)))
    d = ImageDraw.Draw(im)
    d.rounded_rectangle([bx0, by, bx0 + (bx1 - bx0) * 0.4, by + dp(12)], radius=dp(6), fill=SUN)
    for i in range(5):
        x = bx0 + (bx1 - bx0) * (i + 1) / 5 - dp(6)
        d.polygon(N.star(x, by + dp(6), dp(9)), fill=SUN if i < 2 else (90, 90, 140), outline=INK)
    nb = Image.new('RGBA', (W, P.H), (0, 0, 0, 0)); P.navbar(nb, 0)
    im.alpha_composite(nb.crop((0, int(P.H - dp(92)), W, P.H)), (0, int(H - dp(92))))
    return im


def cosmetic(im, name, cx, cy, got):
    """Mejora del planeta: el planetita con su adorno; las que faltan, apagadas."""
    d = ImageDraw.Draw(im); r = dp(20)
    a = 255 if got else 110
    lay = Image.new('RGBA', im.size, (0, 0, 0, 0)); ld = ImageDraw.Draw(lay)
    if name == 'Anillo':
        ld.arc([cx - r * 1.8, cy - r * 0.55, cx + r * 1.8, cy + r * 0.55], 180, 360, fill=(230, 200, 150, a), width=int(dp(4)))
    ld.ellipse([cx - r, cy - r, cx + r, cy + r], fill=(60, 150, 225, a), outline=LINE + (a,), width=int(dp(2)))
    ld.ellipse([cx - r * 0.6, cy - r * 0.5, cx + r * 0.1, cy + r * 0.2], fill=(120, 205, 130, a))
    if name == 'Anillo':
        ld.arc([cx - r * 1.8, cy - r * 0.55, cx + r * 1.8, cy + r * 0.55], 0, 180, fill=(230, 200, 150, a), width=int(dp(4)))
        for i in range(7):
            aa = math.radians(i * 26 + 10); ld.ellipse([cx + math.cos(aa) * r * 1.8 - 4, cy + math.sin(aa) * r * 0.55 - 4, cx + math.cos(aa) * r * 1.8 + 4, cy + math.sin(aa) * r * 0.55 + 4], fill=(170, 150, 130, a))
    if name == 'Aurora':
        for i, c in enumerate([(120, 240, 170), (90, 200, 255)]):
            ld.arc([cx - r * 1.1, cy - r * 1.35 + i * 6, cx + r * 1.1, cy + r * 0.6 + i * 6], 200, 340, fill=c + (a,), width=int(dp(3)))
    if name == 'Luna':
        ld.ellipse([cx + r * 0.9, cy - r * 1.3, cx + r * 1.5, cy - r * 0.7], fill=(235, 232, 250, a), outline=LINE + (a,), width=int(dp(1.5)))
    if name == 'Cometa':
        ld.line([(cx - r * 1.8, cy - r * 1.4), (cx - r * 0.9, cy - r * 0.9)], fill=(150, 220, 255, a), width=int(dp(3)))
        ld.ellipse([cx - r * 1.0, cy - r * 1.05, cx - r * 0.7, cy - r * 0.75], fill=(255, 255, 255, a))
    im.alpha_composite(lay)
    if not got:
        d = ImageDraw.Draw(im)
        d.rounded_rectangle([cx - dp(7), cy - dp(4), cx + dp(7), cy + dp(8)], radius=dp(2), fill=(200, 196, 235))
        d.arc([cx - dp(5), cy - dp(11), cx + dp(5), cy + dp(1)], 180, 360, fill=(200, 196, 235), width=int(dp(2.5)))


def sheet():
    scr = screen()
    out = os.path.join(P.OUT, 'hoy-mundo-nubi.png'); scr.convert('RGB').save(out); print(out)


if __name__ == '__main__':
    sheet()
