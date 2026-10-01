"""Maqueta de "Lluvia de meteoros" (30-sep, juego estrella de Lenguaje; especificación en
docs/diseno-lluvia-de-meteoros.md). Cuatro pantallas de teléfono (412 x 915 dp) con el fondo C y la arcilla de la app
(borde tinta, sombra dura hacia abajo): Así se juega (nivel 2), Más difícil (nivel 9), Acierto y error y Al final (la
pantalla de resultado con las 4 medidas). Todo se dibuja en Pillow: no necesita los .raw de ArtPreview. Maqueta, NO el
juego.

  python tools/art-preview/meteoros.py  ->  docs/previews/meteoros.png

Reglas que se comprueban al correr: palabras de los meteoros >= 24 sp, el resto >= 14 sp, y contraste >= 4,5:1 del texto
sobre el cielo. Sin emojis; el acierto y el error se dicen con forma (✓, ✗, grietas) y texto, no solo con color.
"""
import math
import os
import random

from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
RES = os.path.join(ROOT, "app", "src", "main", "res", "font")
FONT = os.path.join(RES, "fredoka.ttf")
NUNITO = os.path.join(RES, "nunito_regular.ttf")
OUT = os.path.join(ROOT, "docs", "previews", "meteoros.png")

W, H, S = 412, 915, 2

GRAD = ["#02030F", "#050823", "#0A0F33"]
INK = (26, 18, 64)
CORAL = (255, 107, 74)
SUN = (255, 201, 60)
SKY = (76, 201, 240)
GRAPE = (184, 164, 255)
LIME = (155, 229, 100)
CREAM = (255, 255, 255)
PLACA = (255, 248, 236)
SOFT = (214, 208, 245)
DUST = (150, 150, 168)
WHITE = (255, 255, 255)


def hexrgb(h):
    h = h.lstrip("#")
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))


def lum(c):
    def ch(v):
        v /= 255
        return v / 12.92 if v <= 0.03928 else ((v + 0.055) / 1.055) ** 2.4
    r, g, b = (ch(x) for x in c[:3])
    return 0.2126 * r + 0.7152 * g + 0.0722 * b


def contrast(a, b):
    la, lb = sorted((lum(a), lum(b)), reverse=True)
    return (la + 0.05) / (lb + 0.05)


_f = {}


def fredoka(sp, weight=650):
    k = ("f", sp, weight)
    if k not in _f:
        f = ImageFont.truetype(FONT, int(sp * S))
        try:
            f.set_variation_by_axes([weight])
        except OSError:
            pass
        _f[k] = f
    return _f[k]


def nunito(sp):
    k = ("n", sp)
    if k not in _f:
        _f[k] = ImageFont.truetype(NUNITO, int(sp * S))
    return _f[k]


USED = []  # (texto, sp, es palabra de meteoro, color)


def text(d, xy, s, kind, sp, fill, anchor="la", weight=650, word=False, stroke=0):
    USED.append((s, sp, word, fill))
    f = fredoka(sp, weight) if kind == "f" else nunito(sp)
    kw = {}
    if stroke:
        kw = dict(stroke_width=int(stroke * S), stroke_fill=INK)
    d.text((xy[0] * S, xy[1] * S), s, font=f, fill=fill, anchor=anchor, **kw)


def tw(s, kind, sp, weight=650):
    f = fredoka(sp, weight) if kind == "f" else nunito(sp)
    return f.getlength(s) / S


def ov(img, fn, blur=0):
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    fn(ImageDraw.Draw(layer))
    if blur:
        layer = layer.filter(ImageFilter.GaussianBlur(blur * S))
    img.alpha_composite(layer)


def P(pts):
    return [(x * S, y * S) for x, y in pts]


def gradient(stops, w, h):
    cols = [hexrgb(s) for s in stops]
    g = Image.new("RGB", (1, h))
    px = g.load()
    for y in range(h):
        t = y / (h - 1) * (len(cols) - 1)
        i = min(int(t), len(cols) - 2)
        f = t - i
        px[0, y] = tuple(int(cols[i][k] + (cols[i + 1][k] - cols[i][k]) * f) for k in range(3))
    return g.resize((w, h))


def glow(img, cx, cy, r, color, alpha, blur=0.25):
    def draw(d):
        for i in range(24, 0, -1):
            rr = r * S * i / 24
            d.ellipse([cx * S - rr, cy * S - rr, cx * S + rr, cy * S + rr], fill=tuple(color) + (int(alpha * (1 - i / 24)),))
    ov(img, draw, blur * 4)


def sky(seed=7):
    w, h = W * S, H * S
    img = gradient(GRAD, w, h).convert("RGBA")

    def neb(cx, cy, r, col, a):
        glow(img, cx / S, cy / S, r / S, col, a, 0.4)
    neb(w * 0.9, h * 0.10, w * 0.9, SKY, 0x28)
    neb(w * 0.1, h * 0.55, w * 0.8, GRAPE, 0x1C)
    neb(w * 0.05, h * 0.95, w * 0.85, CORAL, 0x24)
    rnd = random.Random(seed)
    stars = [(rnd.random() * w, rnd.random() * h, (0.6 + rnd.random() * 1.6) * S, rnd.random() < 0.18,
              int(90 + rnd.random() * 140)) for _ in range(80)]

    def draw(d):
        for x, y, r, warm, a in stars:
            d.ellipse([x - r, y - r, x + r, y + r], fill=((255, 195, 138) if warm else WHITE) + (a,))
    ov(img, draw)
    return img


# ------------------------------------------------------------------ arcilla


def clay_rr(img, box, radius, fill, outline=INK, width=3, shadow=4):
    x0, y0, x1, y1 = box
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([x0 * S, (y0 + shadow) * S, x1 * S, (y1 + shadow) * S], radius * S, fill=INK)
    d.rounded_rectangle([x0 * S, y0 * S, x1 * S, y1 * S], radius * S, fill=fill, outline=outline, width=int(width * S))


def star_pts(cx, cy, r, inner=0.45, rot=-90):
    pts = []
    for k in range(10):
        a = math.radians(rot + 36 * k)
        rr = r if k % 2 == 0 else r * inner
        pts.append((cx + rr * math.cos(a), cy + rr * math.sin(a)))
    return pts


def clay_star(img, cx, cy, r, fill=SUN, frac=1.0, outline=INK):
    """Estrella de arcilla; [frac] 0..1 la enciende de izquierda a derecha (media estrella = 0,5). Apagada = contorno."""
    d = ImageDraw.Draw(img)
    pts = P(star_pts(cx, cy, r))
    if frac < 1.0:
        d.polygon(pts, fill=(40, 48, 110, 255))
    if frac > 0:
        layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
        ImageDraw.Draw(layer).polygon(pts, fill=fill + (255,))
        mask = Image.new("L", img.size, 0)
        ImageDraw.Draw(mask).rectangle([(cx - r) * S, 0, (cx - r + 2 * r * frac) * S, H * S], fill=255)
        layer.putalpha(Image.composite(layer.getchannel("A"), Image.new("L", img.size, 0), mask))
        img.alpha_composite(layer)
    ImageDraw.Draw(img).polygon(pts, outline=(255, 240, 190) if frac > 0 else (190, 184, 230), width=int(1.4 * S))


def sparkle(img, cx, cy, r, color):
    def draw(d):
        pts = []
        for k in range(8):
            a = math.radians(-90 + 45 * k)
            rr = r if k % 2 == 0 else r * 0.28
            pts.append((cx + rr * math.cos(a), cy + rr * math.sin(a)))
        d.polygon(P(pts), fill=tuple(color) + (255,))
    ov(img, draw)


def mark(img, cx, cy, ok, r=15):
    """✓ (lima) o ✗ (coral) de arcilla: forma + color."""
    d = ImageDraw.Draw(img)
    d.ellipse([(cx - r) * S, (cy - r + 3) * S, (cx + r) * S, (cy + r + 3) * S], fill=INK)
    d.ellipse([(cx - r) * S, (cy - r) * S, (cx + r) * S, (cy + r) * S], fill=LIME if ok else CORAL, outline=INK, width=int(2.5 * S))
    w = int(3.4 * S)
    if ok:
        d.line(P([(cx - r * 0.45, cy), (cx - r * 0.1, cy + r * 0.38), (cx + r * 0.5, cy - r * 0.34)]), fill=WHITE, width=w, joint="curve")
    else:
        d.line(P([(cx - r * 0.38, cy - r * 0.38), (cx + r * 0.38, cy + r * 0.38)]), fill=WHITE, width=w)
        d.line(P([(cx + r * 0.38, cy - r * 0.38), (cx - r * 0.38, cy + r * 0.38)]), fill=WHITE, width=w)


# ------------------------------------------------------------------ meteoro


def blob(cx, cy, rx, ry, seed, n=30, jit=0.05):
    rnd = random.Random(seed)
    pts = []
    for k in range(n):
        a = 2 * math.pi * k / n
        j = 1 + (rnd.random() - 0.5) * 2 * jit
        pts.append((cx + rx * j * math.cos(a), cy + ry * j * math.sin(a)))
    return pts


def trail(img, cx, cy, ang, length, color, bright=False):
    """Estela: partículas que se achican y se apagan hacia atrás (hacia arriba a la izquierda)."""
    vx, vy = -math.sin(math.radians(ang)), -math.cos(math.radians(ang))  # hacia atrás (arriba a la izquierda)
    n = 26 if bright else 18

    def draw(d):
        for i in range(n):
            t = i / n
            x, y = cx + vx * (30 + length * t), cy + vy * (30 + length * t)
            r = (15 if bright else 12) * (1 - t * 0.85)
            a = int((150 if bright else 110) * (1 - t) ** 1.4)
            d.ellipse([(x - r) * S, (y - r) * S, (x + r) * S, (y + r) * S], fill=tuple(color) + (a,))
    ov(img, draw, blur=2.2)
    if bright:
        def sp(d):
            rnd = random.Random(5)
            for i in range(9):
                t = rnd.random()
                x = cx + vx * (40 + length * t) + (rnd.random() - 0.5) * 30
                y = cy + vy * (40 + length * t) + (rnd.random() - 0.5) * 30
                r = 1.5 + rnd.random() * 2
                d.ellipse([(x - r) * S, (y - r) * S, (x + r) * S, (y + r) * S], fill=WHITE + (230,))
        ov(img, sp)


def meteor(img, cx, cy, word, color, seed, ang=18, golden=False, streak=False, state="fall", trail_len=150):
    """Roca de arcilla con placa crema. La placa va siempre horizontal. [state]: fall | cracked."""
    wsz = 28
    pw = tw(word, "f", wsz) + 26
    rx, ry = pw / 2 + 16, 46
    rock = DUST if state == "cracked" else color
    if state != "cracked":
        trail(img, cx, cy, ang, trail_len + (60 if streak else 0), SUN if streak else color, bright=streak)
    if golden:
        glow(img, cx, cy, rx * 1.5, SUN, 150)
    d = ImageDraw.Draw(img)
    pts = blob(cx, cy, rx, ry, seed)
    d.polygon(P([(x, y + 5) for x, y in pts]), fill=INK)
    d.polygon(P(pts), fill=rock + (255,), outline=INK, width=int(3.5 * S))
    # brillo y cráteres para que no sea un óvalo liso
    ov(img, lambda dd: dd.ellipse([(cx - rx * 0.78) * S, (cy - ry * 0.78) * S, (cx - rx * 0.1) * S, (cy - ry * 0.25) * S],
                                  fill=(255, 255, 255, 70)), blur=3)
    rnd = random.Random(seed + 1)
    d = ImageDraw.Draw(img)
    for _ in range(3):
        a = rnd.random() * 2 * math.pi
        x, y = cx + math.cos(a) * rx * 0.82, cy + math.sin(a) * ry * 0.72
        r = 4 + rnd.random() * 4
        d.ellipse([(x - r) * S, (y - r) * S, (x + r) * S, (y + r) * S], fill=tuple(int(c * 0.72) for c in rock) + (255,))
    if state == "cracked":
        cracks(img, cx, cy, rx, ry)
        d = ImageDraw.Draw(img)
    if golden:
        d.ellipse([(cx - rx - 10) * S, (cy - ry - 10) * S, (cx + rx + 10) * S, (cy + ry + 10) * S], outline=SUN, width=int(3 * S))
    # placa
    box = (cx - pw / 2, cy - 21, cx + pw / 2, cy + 21)
    clay_rr(img, box, 12, PLACA, INK, 3, 3)
    d = ImageDraw.Draw(img)
    text(d, (cx, cy + 1), word, "f", wsz, INK, anchor="mm", word=True)
    return pw


def cracks(img, cx, cy, rx, ry):
    d = ImageDraw.Draw(img)
    for pts in ([(cx - rx * 0.55, cy - ry * 0.7), (cx - rx * 0.3, cy - ry * 0.3), (cx - rx * 0.42, cy - ry * 0.05), (cx - rx * 0.2, cy + ry * 0.35)],
                [(cx + rx * 0.6, cy - ry * 0.55), (cx + rx * 0.38, cy - ry * 0.2), (cx + rx * 0.5, cy + ry * 0.2), (cx + rx * 0.3, cy + ry * 0.7)]):
        d.line(P(pts), fill=INK, width=int(3 * S), joint="curve")


def dust(img, cx, cy, n, spread, color=DUST, seed=3, up=False):
    rnd = random.Random(seed)

    def draw(d):
        for _ in range(n):
            x = cx + (rnd.random() - 0.5) * spread
            y = cy + rnd.random() * spread * (-0.5 if up else 0.7)
            r = 2 + rnd.random() * 4
            d.ellipse([(x - r) * S, (y - r) * S, (x + r) * S, (y + r) * S], fill=tuple(color) + (int(80 + rnd.random() * 120),))
    ov(img, draw, blur=0.6)


# ------------------------------------------------------------------ escena


def planet(img):
    """Borde curvo del planeta (abajo) con la cúpula del observatorio."""
    cx, cy, r = W / 2, 1190, 420
    glow(img, cx, cy - r + 10, 250, SKY, 55, 0.5)

    def atm(d):
        d.ellipse([(cx - r - 16) * S, (cy - r - 16) * S, (cx + r + 16) * S, (cy + r + 16) * S], outline=SKY + (70,), width=int(7 * S))
    ov(img, atm, blur=3)
    d = ImageDraw.Draw(img)
    d.ellipse([(cx - r) * S, (cy - r + 5) * S, (cx + r) * S, (cy + r + 5) * S], fill=INK)
    d.ellipse([(cx - r) * S, (cy - r) * S, (cx + r) * S, (cy + r) * S], fill=(31, 44, 112, 255), outline=INK, width=int(4 * S))
    # luz en el borde y cráteres suaves
    ov(img, lambda dd: dd.ellipse([(cx - r + 8) * S, (cy - r + 8) * S, (cx + r - 8) * S, (cy + r - 8) * S],
                                  outline=(120, 150, 255, 90), width=int(5 * S)), blur=2)
    for (x, y, rr) in ((100, 850, 26), (320, 868, 20), (230, 884, 14), (60, 905, 12), (360, 902, 16)):
        ov(img, lambda dd, x=x, y=y, rr=rr: dd.ellipse([(x - rr) * S, (y - rr * 0.5) * S, (x + rr) * S, (y + rr * 0.5) * S],
                                                        fill=(20, 28, 80, 150)))
    # cúpula
    dx, dy = cx, 772
    d = ImageDraw.Draw(img)
    d.pieslice([(dx - 50) * S, (dy - 50 + 4) * S, (dx + 50) * S, (dy + 50 + 4) * S], 180, 360, fill=INK)
    d.pieslice([(dx - 50) * S, (dy - 50) * S, (dx + 50) * S, (dy + 50) * S], 180, 360, fill=PLACA, outline=INK, width=int(3.5 * S))
    d.rounded_rectangle([(dx - 8) * S, (dy - 46) * S, (dx + 8) * S, (dy) * S], 4 * S, fill=GRAPE, outline=INK, width=int(2.5 * S))
    d.line(P([(dx + 2, dy - 30), (dx + 34, dy - 62)]), fill=INK, width=int(9 * S))
    d.line(P([(dx + 2, dy - 30), (dx + 34, dy - 62)]), fill=SKY, width=int(4.5 * S))
    d.rectangle([(dx - 50) * S, (dy - 2) * S, (dx + 50) * S, (dy + 6) * S], fill=INK)


def constellation(img, lit, x0=262, y0=66, glow_idx=None):
    pts = [(x0, y0 + 12), (x0 + 28, y0 - 6), (x0 + 56, y0 + 10), (x0 + 84, y0 - 8), (x0 + 112, y0 + 8)]
    ov(img, lambda d: d.line(P(pts), fill=(190, 184, 230, 120), width=int(1.6 * S)))
    for i, (x, y) in enumerate(pts):
        if glow_idx == i:
            glow(img, x, y, 30, SUN, 160)
        clay_star(img, x, y, 11, SUN, 1.0 if i < lit else 0.0)


def hud(img, nivel, tiempo, puntos, lit, glow_idx=None, mult=None):
    clay_rr(img, (24, 36, 100, 66), 14, (38, 46, 110), INK, 3, 3)
    clay_rr(img, (108, 36, 170, 66), 14, (38, 46, 110), INK, 3, 3)
    clay_rr(img, (178, 36, 252, 66), 14, (38, 46, 110), INK, 3, 3)
    d = ImageDraw.Draw(img)
    text(d, (62, 51), f"Nivel {nivel}", "f", 15, SKY, anchor="mm")
    text(d, (139, 51), tiempo, "f", 15, SUN, anchor="mm")
    text(d, (215, 51), puntos, "f", 15, WHITE, anchor="mm")
    constellation(img, lit, glow_idx=glow_idx)
    d = ImageDraw.Draw(img)
    text(d, (318, 96), "constelación", "n", 14, SOFT, anchor="mm")
    if mult:
        clay_rr(img, (24, 76, 86, 104), 14, SUN, INK, 3, 3)
        d = ImageDraw.Draw(img)
        text(d, (55, 90), mult, "f", 16, INK, anchor="mm")


def caption(img, y, line1, line2=None, col=WHITE):
    d = ImageDraw.Draw(img)
    text(d, (W / 2, y), line1, "f", 20, col, anchor="mm", stroke=1.5)
    if line2:
        text(d, (W / 2, y + 28), line2, "f", 20, col, anchor="mm", stroke=1.5)


# ------------------------------------------------------------------ pantallas


def screen_intro():
    img = sky(11)
    planet(img)
    hud(img, 2, "1:12", "340", 3)
    caption(img, 150, "Toca las palabras que existen.", "Las inventadas, déjalas pasar.")
    meteor(img, 232, 430, "ventana", SKY, 21, ang=16)
    # mano que toca: aro sol y rótulo
    ov(img, lambda d: d.ellipse([(232 - 92) * S, (430 - 66) * S, (232 + 92) * S, (430 + 66) * S], outline=SUN + (230,), width=int(3 * S)))
    ov(img, lambda d: d.ellipse([(232 - 104) * S, (430 - 76) * S, (232 + 104) * S, (430 + 76) * S], outline=SUN + (100,), width=int(2 * S)))
    d = ImageDraw.Draw(img)
    text(d, (232, 534), "¡Tócala!", "f", 22, SUN, anchor="mm", stroke=2)
    text(d, (W / 2, 668), "Si dudas, déjala pasar: no pierdes nada.", "n", 15, SOFT, anchor="mm")
    return img


def screen_hard():
    img = sky(12)
    planet(img)
    hud(img, 9, "0:48", "2.340", 4, mult="× 1,5")
    meteor(img, 128, 250, "murciélago", GRAPE, 31, ang=16, streak=True)
    meteor(img, 282, 440, "chocloate", SKY, 32, ang=16, streak=True)
    meteor(img, 150, 585, "efímero", SUN, 33, ang=16, golden=True, streak=True)
    d = ImageDraw.Draw(img)
    text(d, (150, 662), "dorada: vale el doble", "n", 15, SUN, anchor="mm", stroke=1.5)
    text(d, (W / 2 + 40, 692), "Racha de 9: la estela se enciende", "n", 15, SOFT, anchor="mm")
    return img


def screen_feedback():
    img = sky(13)
    planet(img)
    hud(img, 6, "0:31", "1.120", 4, glow_idx=3)
    d = ImageDraw.Draw(img)
    # Izquierda: roca que estalla en estrellas camino a la constelación, con ✓
    bx, by = 112, 330
    glow(img, bx, by, 80, SUN, 140)
    for k, (col, a, rr) in enumerate([(SUN, -80, 54), (SKY, -20, 60), (GRAPE, 40, 52), (LIME, 100, 58), (SUN, 150, 50), (CORAL, 205, 56),
                                      (SKY, 255, 48), (GRAPE, 300, 55)]):
        x, y = bx + rr * math.cos(math.radians(a)), by + rr * math.sin(math.radians(a))
        sparkle(img, x, y, 11 + (k % 3) * 3, col)
    # trocitos de roca
    for (dx, dy, r) in ((-30, 22, 9), (26, 30, 7), (6, -34, 8)):
        dd = ImageDraw.Draw(img)
        dd.ellipse([(bx + dx - r) * S, (by + dy - r) * S, (bx + dx + r) * S, (by + dy + r) * S], fill=SKY, outline=INK, width=int(2.5 * S))
    mark(img, bx + 52, by + 42, True)
    # camino punteado de estrellas hacia la constelación (cuarta estrella)
    tx, ty = 262 + 84, 66 - 8
    for i in range(1, 9):
        t = i / 9
        x = bx + (tx - bx) * t
        y = by + (ty - by) * t - math.sin(t * math.pi) * 70
        sparkle(img, x, y, 4 + 4 * (1 - abs(t - 0.5) * 2), SUN)
    d = ImageDraw.Draw(img)
    text(d, (bx, by + 92), "¡Acierto!", "f", 20, LIME, anchor="mm", stroke=2)
    text(d, (bx, by + 118), "la palabra va a tu constelación", "n", 14, SOFT, anchor="mm")
    # Derecha: "brúgala" tocada (inventada): roca agrietada en polvo + ✗ y palabra tachada
    cx, cy = 292, 560
    pw = meteor(img, cx, cy, "brúgala", SKY, 41, ang=16, state="cracked")
    d = ImageDraw.Draw(img)
    d.line(P([(cx - pw / 2 + 8, cy + 4), (cx + pw / 2 - 8, cy + 4)]), fill=CORAL, width=int(3 * S))
    dust(img, cx, cy + 48, 26, 120, seed=9)
    mark(img, cx + pw / 2 + 6, cy - 34, False)
    d = ImageDraw.Draw(img)
    text(d, (cx, cy + 104), "Esa no existía", "f", 20, CORAL, anchor="mm", stroke=2)
    text(d, (cx, cy + 130), "suena a palabra, pero es inventada", "n", 14, SOFT, anchor="mm")
    # Abajo: una inventada que se deshizo al llegar a la atmósfera y una real que se fue
    dust(img, 112, 750, 30, 70, color=SKY, seed=4, up=True)
    d = ImageDraw.Draw(img)
    for (x, y, c) in ((92, 738, SUN), (132, 728, GRAPE), (112, 752, WHITE), (146, 748, SKY)):
        sparkle(img, x, y, 6, c)
    text(d, (112, 672), "se deshizo:", "n", 14, SOFT, anchor="mm")
    text(d, (112, 690), "no la tocaste", "n", 14, SOFT, anchor="mm")
    text(d, (332, 748), "se fue: brújula", "n", 15, SOFT, anchor="mm")
    return img


def screen_result():
    img = sky(14)
    d = ImageDraw.Draw(img)
    M = 28
    text(d, (W / 2, 40), "Lluvia de meteoros", "f", 26, WHITE, anchor="mm")
    d.ellipse([(W / 2 - 62) * S, 62 * S, (W / 2 - 50) * S, 74 * S], fill=hexrgb("#10B981"))
    text(d, (W / 2 - 42, 68), "Lenguaje · Nivel 6 · Reto", "n", 15, SOFT, anchor="lm")
    glow(img, W / 2, 118, 70, SUN, 90)
    d = ImageDraw.Draw(img)
    text(d, (W / 2 - 14, 118), "78", "f", 60, SUN, anchor="rm", weight=700, stroke=3)
    text(d, (W / 2 - 6, 130), "puntos", "n", 15, SOFT, anchor="lm")
    text(d, (W / 2, 168), "¡Muy bien!", "f", 24, WHITE, anchor="mm")

    # 1. Tu vocabulario
    y = 202
    text(d, (M, y), "Tu vocabulario", "f", 19, SUN, anchor="lm")
    pcts = [100, 96, 90, 78, 55, 30]
    colw = (W - 2 * M) / 6
    for i, p in enumerate(pcts):
        cx = M + colw * (i + 0.5)
        lit = round(p / 20 * 2) / 2
        for k in range(5):
            frac = max(0.0, min(1.0, lit - k))
            clay_star(img, cx, y + 118 - k * 21, 9.5, SUN, frac)
        d = ImageDraw.Draw(img)
        text(d, (cx, y + 148), f"{p}%", "f", 14, WHITE, anchor="mm")
    d = ImageDraw.Draw(img)
    text(d, (M, y + 174), "común", "n", 14, SOFT, anchor="lm")
    text(d, (W - M, y + 174), "rara", "n", 14, SOFT, anchor="rm")
    text(d, (M, y + 198), "Reconoces casi todas hasta las poco frecuentes;", "n", 15, WHITE, anchor="lm")
    text(d, (M, y + 218), "las raras, la mitad.", "n", 15, WHITE, anchor="lm")

    # 2. Tu reconocimiento
    y = 442
    text(d, (M, y), "Tu reconocimiento", "f", 19, SUN, anchor="lm")
    text(d, (M, y + 30), "comunes 0,7 s · raras 1,1 s", "f", 17, WHITE, anchor="lm")
    for k, (lab, secs, col) in enumerate((("comunes", 0.7, SKY), ("raras", 1.1, GRAPE))):
        yy = y + 54 + k * 22
        text(d, (M, yy), lab, "n", 14, SOFT, anchor="lm")
        x0 = M + 70
        ov(img, lambda dd, yy=yy, x0=x0: dd.rounded_rectangle([x0 * S, (yy - 6) * S, (W - M - 40) * S, (yy + 6) * S], 6 * S, fill=(255, 255, 255, 36)))
        x1 = x0 + (W - M - 40 - x0) * secs / 1.5
        d = ImageDraw.Draw(img)
        d.rounded_rectangle([x0 * S, (yy - 6) * S, x1 * S, (yy + 6) * S], 6 * S, fill=col)
        text(d, (W - M, yy), f"{secs:.1f} s".replace(".", ","), "n", 14, WHITE, anchor="rm")

    # 3. Tu filtro
    y = 548
    text(d, (M, y), "Tu filtro", "f", 19, SUN, anchor="lm")
    rows = (("obvias", 0, 6), ("una letra cambiada", 1, 8), ("letras cambiadas de lugar", 4, 7))
    for k, (lab, bad, tot) in enumerate(rows):
        yy = y + 30 + k * 28
        text(d, (M, yy), lab, "n", 15, WHITE, anchor="lm")
        text(d, (W - M, yy), f"{bad} de {tot}", "f", 16, CORAL if bad >= 3 else WHITE, anchor="rm")
        # marcas: ✗ = engañó, punto = la dejaste pasar bien (forma, no solo color)
        mx = M + 200
        for j in range(tot):
            x = mx + j * 11
            if j < bad:
                ov(img, lambda dd, x=x, yy=yy: (dd.line(P([(x - 3.5, yy - 3.5), (x + 3.5, yy + 3.5)]), fill=CORAL + (255,), width=int(2 * S)),
                                               dd.line(P([(x + 3.5, yy - 3.5), (x - 3.5, yy + 3.5)]), fill=CORAL + (255,), width=int(2 * S))))
            else:
                ov(img, lambda dd, x=x, yy=yy: dd.ellipse([(x - 2.5) * S, (yy - 2.5) * S, (x + 2.5) * S, (yy + 2.5) * S], fill=(190, 184, 230, 230)))
        d = ImageDraw.Draw(img)
    d.line(P([(M, y + 126), (M, y + 188)]), fill=SUN, width=int(3 * S))
    for k, ln in enumerate(("Las letras cambiadas de lugar engañan a casi todos:", "leemos la palabra entera. Mira el centro",
                            "de la palabra para no caer.")):
        text(d, (M + 12, y + 134 + k * 20), ln, "n", 14, WHITE, anchor="lm")

    # 4. Tu colección
    y = 748
    text(d, (M, y), "Tu colección", "f", 19, SUN, anchor="lm")
    text(d, (W - M, y), "+5 palabras raras", "f", 19, LIME, anchor="rm")
    text(d, (M, y + 28), "efímero · umbral · inefable · cántaro · alba", "n", 15, WHITE, anchor="lm")

    text(d, (W / 2, 810), "Medida de esta partida. No es un diagnóstico.", "n", 14, SOFT, anchor="mm")
    clay_rr(img, (M, 832, W - M, 886), 27, SUN, INK, 3.5, 4)
    d = ImageDraw.Draw(img)
    text(d, (W / 2, 859), "Siguiente juego (3 de 3)", "f", 20, INK, anchor="mm")
    return img


# ------------------------------------------------------------------ lámina


def phone(img):
    out = img.resize((W, H), Image.LANCZOS).convert("RGB")
    mask = Image.new("L", (W, H), 0)
    ImageDraw.Draw(mask).rounded_rectangle([0, 0, W - 1, H - 1], 30, fill=255)
    res = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    res.paste(out, (0, 0), mask)
    return res


def check_rules():
    bad = [(s, sp) for s, sp, word, _ in USED if (word and sp < 24) or (not word and sp < 14)]
    assert not bad, f"texto demasiado chico: {bad}"
    bg = hexrgb(GRAD[2])
    low = []
    for s, sp, word, col in USED:
        if col in (INK,):
            continue  # texto tinta sobre placa crema o botón sol (se mide aparte)
        c = contrast(col, bg)
        if c < 4.5:
            low.append((s, round(c, 1)))
    assert not low, f"contraste bajo: {low}"
    return (f"tinta sobre placa crema {contrast(INK, PLACA):.1f}:1, tinta sobre botón sol {contrast(INK, SUN):.1f}:1, "
            f"texto suave sobre cielo {contrast(SOFT, hexrgb(GRAD[2])):.1f}:1")


def main():
    shots = [("1 · Así se juega (nivel 2)", "toca las palabras que existen", screen_intro()),
             ("2 · Más difícil (nivel 9)", "tres a la vez, racha y dorada", screen_hard()),
             ("3 · Acierto y error", "forma y texto, no solo color", screen_feedback()),
             ("4 · Al final", "las 4 medidas de la partida", screen_result())]
    report = check_rules()
    pad, head = 28, 90
    sheet = Image.new("RGB", (len(shots) * (W + pad) + pad, H + head + pad), (236, 234, 244))
    d = ImageDraw.Draw(sheet)
    fh = ImageFont.truetype(FONT, 26)
    try:
        fh.set_variation_by_axes([650])
    except OSError:
        pass
    fs = ImageFont.truetype(NUNITO, 16)
    for i, (t, s, im) in enumerate(shots):
        x = pad + i * (W + pad)
        d.text((x, 14), t, font=fh, fill=INK)
        d.text((x, 52), s, font=fs, fill=(90, 84, 120))
        ph = phone(im)
        sheet.paste(ph, (x, head), ph)
    sheet.save(OUT)
    print(OUT)
    print("Contrastes:", report)


if __name__ == "__main__":
    main()
