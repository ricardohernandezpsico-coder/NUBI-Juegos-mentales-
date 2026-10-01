"""Maqueta de "Cosecha de palabras" (1-oct; diseño en docs/diseno-cosecha-de-palabras.md): 4 pantallas de teléfono
(412 x 915 dp) sobre el fondo C, en Pillow, con las mismas piezas y reglas que las maquetas de Lluvia de meteoros y
¿Verdad o disparate? (arcilla, borde tinta, sombra dura, textos >= 14 sp, letras de las fichas en Atkinson Hyperlegible
Bold 30 sp, contraste >= 4,5:1, nunca solo color). Es una maqueta, NO la app. La ronda del ejemplo es real
(letras I R A O A S C, estrella ASOCIAR; sale de cosecha_es.json).

  python tools/art-preview/cosecha.py  ->  docs/previews/cosecha.png
"""
import math
import os

from PIL import Image, ImageDraw, ImageFont

import meteoros as M
from meteoros import (CORAL, GRAD, GRAPE, INK, LIME, PLACA, SKY, SOFT, SUN, W, H, WHITE, glow, ov, phone, sky, text)

S = M.S
OUT = os.path.join(M.ROOT, "docs", "previews", "cosecha.png")
ATKINSON = os.path.join(M.ROOT, "tools", "previews", "fuentes-letra", "AtkinsonHyperlegible-Bold.ttf")
NUBI = os.path.join(M.ROOT, "app", "src", "main", "res", "drawable-nodpi")
MARGIN = 28
LETTER_SP = 30
LEAF = (112, 200, 84)
LEAF_D = (70, 150, 70)
SOIL = (96, 66, 120)
GOLD = (255, 214, 92)
TILES = [PLACA, (206, 232, 255), (222, 210, 255), (214, 240, 190), (255, 226, 170), (255, 214, 204), (200, 238, 244)]
_atk = {}
LETTERS_USED = []


def atkinson(sp):
    if sp not in _atk:
        _atk[sp] = ImageFont.truetype(ATKINSON, int(sp * S))
    return _atk[sp]


def letter(d, xy, s, fill=INK, sp=LETTER_SP, anchor="mm"):
    LETTERS_USED.append((s, sp))
    d.text((xy[0] * S, xy[1] * S), s, font=atkinson(sp), fill=fill, anchor=anchor)


def atk_w(s, sp=LETTER_SP):
    return atkinson(sp).getlength(s) / S


# ---------------------------------------------------------------- piezas


def hud(img, cosecha, tiempo, puntos, mult=None):
    M.clay_rr(img, (24, 36, 206, 66), 14, (38, 46, 110), INK, 3, 3)
    M.clay_rr(img, (214, 36, 290, 66), 14, (38, 46, 110), INK, 3, 3)
    d = ImageDraw.Draw(img)
    text(d, (115, 51), f"Cosecha {cosecha} de 3 · {tiempo}", "f", 15, SKY, anchor="mm")
    text(d, (252, 51), puntos, "f", 15, WHITE, anchor="mm")
    if mult:
        M.clay_rr(img, (298, 36, 362, 66), 14, SUN, INK, 3, 3)
        d = ImageDraw.Draw(img)
        text(d, (330, 51), mult, "f", 16, INK, anchor="mm")


def time_bar(img, frac, y=78):
    ov(img, lambda dd: dd.rounded_rectangle([MARGIN * S, y * S, (W - MARGIN) * S, (y + 8) * S], 4 * S, fill=(255, 255, 255, 40)))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([MARGIN * S, y * S, (MARGIN + (W - 2 * MARGIN) * frac) * S, (y + 8) * S], 4 * S, fill=SKY)


def caption(img, y, line1, line2=None, col=WHITE):
    d = ImageDraw.Draw(img)
    text(d, (W / 2, y), line1, "f", 19, col, anchor="mm", stroke=2.5)
    if line2:
        text(d, (W / 2, y + 28), line2, "n", 16, SOFT, anchor="mm")


def moon_tile(img, cx, cy, size, ch, fill, state="free", order=None):
    """Ficha-luna de arcilla (círculo, borde tinta, sombra dura, cráteres tenues y brillo). state: free / used (con número) / hint."""
    r = size / 2
    if state == "hint":
        glow(img, cx, cy, r * 1.9, SUN, 150)
    d = ImageDraw.Draw(img)
    d.ellipse([(cx - r) * S, (cy - r + 4) * S, (cx + r) * S, (cy + r + 4) * S], fill=INK)
    base = fill if state != "used" else tuple(int(c * 0.55 + 40) for c in fill)
    d.ellipse([(cx - r) * S, (cy - r) * S, (cx + r) * S, (cy + r) * S], fill=base, outline=INK, width=int(3.4 * S))
    # cráteres y brillo
    for dx, dy, rr in ((-0.46, -0.30, 0.13), (0.42, 0.40, 0.10), (0.30, -0.50, 0.07)):
        ov(img, lambda dd, dx=dx, dy=dy, rr=rr: dd.ellipse([(cx + dx * r - rr * r) * S, (cy + dy * r - rr * r) * S, (cx + dx * r + rr * r) * S, (cy + dy * r + rr * r) * S], fill=(0, 0, 0, 26)))
    ov(img, lambda dd: dd.arc([(cx - r + 6) * S, (cy - r + 6) * S, (cx + r - 6) * S, (cy + r - 6) * S], 200, 255, fill=(255, 255, 255, 190), width=int(3.4 * S)), blur=0.4)
    d = ImageDraw.Draw(img)
    letter(d, (cx, cy + 1), ch, fill=INK if state != "used" else (60, 52, 96))
    if state == "used" and order:
        # número de orden: la ficha "usada" se distingue por el número y el aro, no solo por apagarse
        d.ellipse([(cx + r * 0.46 - 11) * S, (cy - r * 0.9 - 11 + 3) * S, (cx + r * 0.46 + 11) * S, (cy - r * 0.9 + 11 + 3) * S], fill=INK)
        d.ellipse([(cx + r * 0.46 - 11) * S, (cy - r * 0.9 - 11) * S, (cx + r * 0.46 + 11) * S, (cy - r * 0.9 + 11) * S], fill=SKY, outline=INK, width=int(2.5 * S))
        text(d, (cx + r * 0.46, cy - r * 0.9), str(order), "f", 14, INK, anchor="mm")
    if state == "hint":
        d.ellipse([(cx - r - 5) * S, (cy - r - 5) * S, (cx + r + 5) * S, (cy + r + 5) * S], outline=SUN, width=int(4 * S))


def orbit(img, cx, cy, rx, ry, letters, used=(), hint=None, phase=0.35, dim=False):
    """Siete fichas en una órbita elíptica alrededor del planeta; las de atrás van detrás del planeta."""
    def ring(d):
        d.ellipse([(cx - rx) * S, (cy - ry) * S, (cx + rx) * S, (cy + ry) * S], outline=SKY + (70,), width=int(2.5 * S))
    ov(img, ring, blur=0.5)
    n = len(letters)
    items = []
    for i, ch in enumerate(letters):
        a = phase + 2 * math.pi * i / n
        items.append((math.sin(a), cx + rx * math.cos(a), cy + ry * math.sin(a), i, ch))
    return items


def draw_tiles(img, items, used, hint, back):
    for depth, x, y, i, ch in sorted(items):
        if (depth < 0) != back:
            continue
        size = 64 + 8 * (depth + 1) / 2          # el de adelante un poco mayor; ninguna baja de 64 dp
        st = "hint" if i == hint else ("used" if i in used else "free")
        order = used.index(i) + 1 if i in used else None
        moon_tile(img, x, y, size, ch, TILES[i % len(TILES)], st, order)


EARTH = (158, 102, 66)        # tierra de arcilla, marrón cálido
EARTH_D = (122, 76, 52)       # vetas
EARTH_L = (190, 134, 90)      # relieve claro
EARTH_SHADE = (70, 40, 44)    # lado en sombra
GRASS = (112, 200, 84)
SHROOM = (236, 96, 88)
SUNFLOWER = (255, 196, 54)
BUSH = (86, 176, 96)


def _earth_texture(r, seed=4):
    """Textura de la tierra (vetas, manchas de relieve y cráteres) en una capa del tamaño del planeta."""
    import random
    rnd = random.Random(seed)
    n = int(2 * r * S) + 8
    layer = Image.new("RGBA", (n, n), EARTH + (255,))
    d = ImageDraw.Draw(layer)
    c = n / 2
    for k in range(7):                                     # vetas onduladas
        y = (0.12 + k * 0.125) * n
        pts = []
        for i in range(0, 41):
            x = i / 40 * n
            pts.append((x, y + math.sin(i / 40 * 6.28 * (1.2 + rnd.random() * 0.8) + k) * n * 0.03))
        d.line(pts, fill=EARTH_D + (255,), width=int((5 + rnd.random() * 4) * S))
    for k in range(9):                                     # manchas claras de relieve
        x, y, rr = rnd.random() * n, rnd.random() * n, (8 + rnd.random() * 16) * S
        d.ellipse([x - rr, y - rr * 0.6, x + rr, y + rr * 0.6], fill=EARTH_L + (255,))
    for k in range(6):                                     # cráteres: borde claro arriba, sombra abajo
        x, y, rr = (0.15 + rnd.random() * 0.7) * n, (0.25 + rnd.random() * 0.65) * n, (6 + rnd.random() * 8) * S
        d.ellipse([x - rr, y - rr * 0.55 + 2 * S, x + rr, y + rr * 0.55 + 2 * S], fill=EARTH_L + (255,))
        d.ellipse([x - rr, y - rr * 0.55, x + rr, y + rr * 0.55], fill=EARTH_D + (255,))
    return layer


def _plant_sprite(kind, h, golden=False):
    """Planta de arcilla en su propia capa, con la base abajo al centro. kind: brote / flor / tulipan / arbusto / girasol / hongo."""
    w = 110
    sh = int(h) + 70
    layer = Image.new("RGBA", (w * S, sh * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    bx, by = w / 2, sh - 4
    top = by - h

    def ell(x0, y0, x1, y1, fill, ow=2.2):
        d.ellipse([x0 * S, (y0 + 2.5) * S, x1 * S, (y1 + 2.5) * S], fill=INK)
        d.ellipse([x0 * S, y0 * S, x1 * S, y1 * S], fill=fill, outline=INK, width=int(ow * S))

    def stem():
        d.line([(bx * S, by * S), (bx * S, top * S)], fill=INK, width=int(8 * S))
        d.line([(bx * S, by * S), (bx * S, top * S)], fill=LEAF_D, width=int(4.4 * S))

    if kind == "arbusto":
        r = 8 + h * 0.34
        for dx, dy, rr in ((-r * 0.8, 0, r * 0.8), (r * 0.8, 0, r * 0.8), (0, -r * 0.55, r * 0.95)):
            ell(bx + dx - rr, by - rr * 0.9 + dy, bx + dx + rr, by + rr * 0.9 + dy, BUSH)
        for dx, dy in ((-r * 0.45, -r * 0.5), (r * 0.5, -r * 0.2), (0, -r * 0.95)):
            d.ellipse([(bx + dx - 2.6) * S, (by + dy - 2.6) * S, (bx + dx + 2.6) * S, (by + dy + 2.6) * S], fill=GOLD if golden else CORAL, outline=INK, width=int(1.4 * S))
        return layer
    if kind == "hongo":
        d.rounded_rectangle([(bx - 5) * S, (by - h * 0.55) * S, (bx + 5) * S, by * S], 4 * S, fill=PLACA, outline=INK, width=int(2.2 * S))
        rw = 11 + h * 0.34
        ell(bx - rw, by - h * 0.55 - rw * 0.75, bx + rw, by - h * 0.55 + rw * 0.35, SHROOM)
        for dx, dy in ((-rw * 0.45, -rw * 0.3), (rw * 0.2, -rw * 0.5), (rw * 0.55, -rw * 0.12)):
            d.ellipse([(bx + dx - 2.6) * S, (by - h * 0.55 + dy - 2.6) * S, (bx + dx + 2.6) * S, (by - h * 0.55 + dy + 2.6) * S], fill=PLACA)
        return layer
    stem()
    for sgn in (-1, 1):                                    # dos hojas
        lx, ly = bx + sgn * 10, by - h * 0.42
        ell(lx - 10, ly - 5.5, lx + 10, ly + 5.5, GRASS)
    if kind == "brote":
        for sgn in (-1, 1):
            ell(bx + sgn * 8 - 8, top - 6, bx + sgn * 8 + 8, top + 6, GRASS)
    elif kind == "flor":
        col = GOLD if golden else CORAL
        for k in range(6):
            a = math.radians(60 * k)
            px, py = bx + 9.5 * math.cos(a), top + 9.5 * math.sin(a)
            ell(px - 6.5, py - 6.5, px + 6.5, py + 6.5, col, 2)
        ell(bx - 6, top - 6, bx + 6, top + 6, SUN if not golden else WHITE, 2.2)
    elif kind == "tulipan":
        d.polygon([((bx - 12) * S, (top - 12) * S), ((bx - 6) * S, (top - 2) * S), (bx * S, (top - 13) * S), ((bx + 6) * S, (top - 2) * S),
                   ((bx + 12) * S, (top - 12) * S), ((bx + 11) * S, (top + 7) * S), ((bx - 11) * S, (top + 7) * S)], fill=SKY, outline=INK, width=int(2.4 * S))
    elif kind == "girasol":
        for k in range(12):
            a = math.radians(30 * k)
            px, py = bx + 13 * math.cos(a), top + 13 * math.sin(a)
            ell(px - 5.5, py - 5.5, px + 5.5, py + 5.5, SUNFLOWER, 1.8)
        ell(bx - 9, top - 9, bx + 9, top + 9, (122, 78, 52), 2.2)
    return layer


def _paste_plant(img, x, y, h, kind, tilt=0.0, golden=False):
    sp = _plant_sprite(kind, h, golden)
    base = (sp.width / 2, sp.height - 4 * S)
    if tilt:
        sp = sp.rotate(-tilt, resample=Image.BICUBIC, center=base)
    img.alpha_composite(sp, (int(x * S - base[0]), int(y * S - base[1])))


def _plants_for(words, r):
    """Posición, tipo y tamaño de cada planta según las palabras sembradas (largo -> tamaño; rara -> flor dorada).
    Se reparten por la cara visible del planeta, de atrás hacia adelante, sin amontonarse."""
    import random
    rnd = random.Random(7)
    out = []
    kinds_by_len = {3: ("brote", "hongo"), 4: ("flor", "brote", "hongo"), 5: ("tulipan", "arbusto", "flor"),
                    6: ("girasol", "arbusto", "tulipan"), 7: ("girasol", "arbusto")}
    spots = []
    tries = 0
    while len(spots) < len(words) and tries < 4000:
        tries += 1
        ang = math.radians(rnd.uniform(-168, -12))
        rad = rnd.uniform(0.18, 0.86) * r
        px, py = rad * math.cos(ang), rad * math.sin(ang) + r * 0.34 * rnd.random()
        if py > -r * 0.12 or math.hypot(px, py) > r * 0.9:
            continue
        mind = 15 if len(words) > 16 else 22
        if all(math.hypot(px - qx, (py - qy) * 1.4) > mind for qx, qy in spots):
            spots.append((px, py))
    spots.sort(key=lambda p: p[1])                       # de atrás (arriba) hacia adelante
    for i, (ln, rare) in enumerate(words):
        if i >= len(spots):
            break
        kind = "flor" if rare else kinds_by_len.get(ln, ("brote",))[i % len(kinds_by_len.get(ln, ("brote",)))]
        h = {3: 15, 4: 21, 5: 27, 6: 33, 7: 38}.get(ln, 20)
        out.append((spots[i][0], spots[i][1], h, kind, rare, spots[i][0] / r * 24))
    return out


def planet(img, cx, cy, r, words=(), tree=False, green=0.15, sprouts=0):
    """Planeta-huerto: esfera de tierra de arcilla (marrón cálido con vetas y relieve, borde tinta, sombra dura abajo, brillo de
    atmósfera). Empieza con un poco de pasto ralo y 2-3 brotes; cada palabra suma una planta (más alta cuanto más larga;
    flor dorada si es rara); la palabra estrella suma el árbol dorado. [words] = lista de (largo, es_rara)."""
    # atmósfera suave
    glow(img, cx, cy, r * 1.55, (150, 226, 190), 70, 0.5)
    ov(img, lambda dd: dd.ellipse([(cx - r - 10) * S, (cy - r - 10) * S, (cx + r + 10) * S, (cy + r + 10) * S], outline=(170, 236, 210, 120), width=int(5 * S)), blur=2.5)
    d = ImageDraw.Draw(img)
    d.ellipse([(cx - r) * S, (cy - r + 6) * S, (cx + r) * S, (cy + r + 6) * S], fill=INK)     # sombra dura abajo
    tex = _earth_texture(r)
    n = tex.width
    mask = Image.new("L", (n, n), 0)
    ImageDraw.Draw(mask).ellipse([4, 4, n - 4, n - 4], fill=255)
    # sombreado del lado oscuro (abajo a la derecha) y brillo arriba a la izquierda
    sh = Image.new("RGBA", (n, n), (0, 0, 0, 0))
    sd = ImageDraw.Draw(sh)
    for i in range(18):
        a = int(6 + i * 3.2)
        off = i * 0.012 * n
        sd.ellipse([n * 0.22 + off, n * 0.18 + off, n * 1.25, n * 1.25], fill=EARTH_SHADE + (a,))
    tex.alpha_composite(sh)
    # pasto: casquete verde arriba, más grande cuanto más huerto
    cap = Image.new("RGBA", (n, n), (0, 0, 0, 0))
    cd = ImageDraw.Draw(cap)
    capy = n * (0.02 + 0.30 * green)
    cd.ellipse([-n * 0.4, -n * 0.7, n * 1.4, capy + n * 0.1], fill=GRASS + (255,)) if green > 0.3 else None
    if green > 0.3:
        for i in range(12):                              # borde del pasto irregular
            x = i / 11 * n
            cd.ellipse([x - 16 * S, capy - 4 * S, x + 16 * S, capy + n * 0.1 + 8 * S * (i % 3)], fill=GRASS + (255,))
    else:                                                # pasto ralo: matitas sueltas
        import random
        rr_ = random.Random(3)
        for i in range(9):
            tx, ty = (0.18 + rr_.random() * 0.64) * n, (0.05 + rr_.random() * 0.2) * n
            for dx in (-5, 0, 5):
                cd.line([(tx + dx * S, ty), (tx + dx * 1.4 * S, ty - (7 + rr_.random() * 4) * S)], fill=GRASS + (255,), width=int(2.6 * S))
    tex.alpha_composite(cap)
    planet_img = Image.new("RGBA", (n, n), (0, 0, 0, 0))
    planet_img.paste(tex, (0, 0), mask)
    img.alpha_composite(planet_img, (int(cx * S - n / 2), int(cy * S - n / 2)))
    d = ImageDraw.Draw(img)
    d.ellipse([(cx - r) * S, (cy - r) * S, (cx + r) * S, (cy + r) * S], outline=INK, width=int(4 * S))
    ov(img, lambda dd: dd.arc([(cx - r + 8) * S, (cy - r + 8) * S, (cx + r - 8) * S, (cy + r - 8) * S], 200, 262, fill=(255, 236, 200, 190), width=int(4 * S)), blur=0.6)
    # brotes iniciales (2-3) y plantas
    spr = [(-r * 0.42, -r * 0.80), (r * 0.12, -r * 0.93), (r * 0.5, -r * 0.7)][:sprouts]
    for (px, py) in spr:
        _paste_plant(img, cx + px, cy + py, 13, "brote", tilt=px / r * 20)
    for (px, py, h, kind, rare, tilt) in _plants_for(list(words), r):
        _paste_plant(img, cx + px, cy + py, h, kind, tilt=tilt, golden=rare)
    if tree:
        golden_tree(img, cx, cy - r * 0.78)


def golden_tree(img, x, y):
    """Árbol dorado de la palabra estrella, con destello."""
    glow(img, x, y - 60, 130, GOLD, 150, 0.5)
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([(x - 12) * S, (y - 74 + 4) * S, (x + 12) * S, (y + 4) * S], 8 * S, fill=INK)
    d.rounded_rectangle([(x - 12) * S, (y - 74) * S, (x + 12) * S, y * S], 8 * S, fill=(176, 120, 76), outline=INK, width=int(3 * S))
    for dx, dy, rr in ((-30, -86, 30), (30, -86, 30), (0, -112, 36), (-14, -64, 24), (14, -64, 24)):
        d.ellipse([(x + dx - rr) * S, (y + dy - rr + 4) * S, (x + dx + rr) * S, (y + dy + rr + 4) * S], fill=INK)
    for dx, dy, rr in ((-30, -86, 30), (30, -86, 30), (0, -112, 36), (-14, -64, 24), (14, -64, 24)):
        d.ellipse([(x + dx - rr) * S, (y + dy - rr) * S, (x + dx + rr) * S, (y + dy + rr) * S], fill=GOLD, outline=INK, width=int(3.2 * S))
    ov(img, lambda dd: dd.arc([(x - 22) * S, (y - 144) * S, (x + 22) * S, (y - 98) * S], 200, 260, fill=(255, 255, 255, 220), width=int(4 * S)), blur=0.4)
    for (dx, dy, rr) in ((-62, -128, 11), (58, -142, 14), (70, -90, 9), (-70, -84, 10), (0, -162, 12)):
        M.sparkle(img, x + dx, y + dy, rr, WHITE)


def tray(img, cy, typed, total=7, state=None, shake=0, glow_col=None):
    """Bandeja de arcilla crema (arriba del planeta) con las letras ya tocadas y los huecos que faltan."""
    ww = total * 46 + 24
    x0, x1 = W / 2 - ww / 2 + shake, W / 2 + ww / 2 + shake
    y0, y1 = cy - 36, cy + 36
    if glow_col:
        glow(img, W / 2 + shake, cy, ww * 0.6, glow_col, 120)
    M.clay_rr(img, (x0, y0, x1, y1), 26, PLACA, INK, 4, 6)
    d = ImageDraw.Draw(img)
    if state:
        d.rounded_rectangle([x0 * S, y0 * S, x1 * S, y1 * S], 26 * S, outline=state, width=int(5 * S))
    for i in range(total):
        cx = x0 + 12 + 23 + i * 46
        if i < len(typed):
            letter(d, (cx, cy + 1), typed[i], fill=INK)
        else:
            d.line([((cx - 12) * S, (cy + 16) * S), ((cx + 12) * S, (cy + 16) * S)], fill=(190, 182, 210), width=int(3.4 * S))
    return x0, y0, x1, y1


def seed_icon(d, cx, cy):
    d.line([(cx * S, (cy + 11) * S), (cx * S, (cy - 4) * S)], fill=INK, width=int(4.4 * S))
    for sgn in (-1, 1):
        d.ellipse([(cx + sgn * 8 - 9) * S, (cy - 10) * S, (cx + sgn * 8 + 9) * S, (cy + 2) * S], fill=INK)
        d.ellipse([(cx + sgn * 8 - 7) * S, (cy - 9) * S, (cx + sgn * 8 + 7) * S, (cy + 1) * S], fill=LEAF)


def action_buttons(img, y, big="Sembrar", small="Borrar", press=False):
    x0, x1 = 24, 286
    M.clay_rr(img, (x0, y, x1, y + 76), 30, LIME, INK, 4, 6)
    d = ImageDraw.Draw(img)
    seed_icon(d, x0 + 54, y + 40)
    text(d, ((x0 + x1) / 2 + 22, y + 39), big, "f", 24, INK, anchor="mm")
    sx0, sx1 = 298, 390
    M.clay_rr(img, (sx0, y + 10, sx1, y + 66), 22, WHITE, INK, 3.5, 4)
    d = ImageDraw.Draw(img)
    # flecha de borrar (forma, no solo texto)
    d.polygon([((sx0 + 10) * S, (y + 38) * S), ((sx0 + 19) * S, (y + 29) * S), ((sx0 + 31) * S, (y + 29) * S), ((sx0 + 31) * S, (y + 47) * S), ((sx0 + 19) * S, (y + 47) * S)], fill=PLACA, outline=INK, width=int(2.4 * S))
    text(d, (sx0 + 61, y + 38), "Borrar", "f", 15, INK, anchor="mm")


def harvest_words(img, y, words, label="Tu cosecha"):
    d = ImageDraw.Draw(img)
    text(d, (MARGIN, y), f"{label} · {len(words)} palabras", "f", 17, SUN, anchor="lm")
    for i, w in enumerate(words):
        col = i % 3
        row = i // 3
        text(d, (MARGIN + col * 120, y + 30 + row * 26), w, "f", 17, WHITE, anchor="lm")


# ---------------------------------------------------------------- las 4 pantallas

LETRAS = "IRAOASC"
WORDS_13 = [(4, False), (5, False), (4, False), (3, False), (6, False), (4, False), (5, True), (3, False), (4, False), (5, False), (4, False), (6, False)]


def screen_play():
    img = sky(21)
    hud(img, 1, "0:42", "140")
    time_bar(img, 0.7)
    caption(img, 118, "Toca letras en orden y siembra palabras", "Cada palabra brota como una planta en tu huerto")
    harvest_words(img, 196, ["casa", "sacar", "risa", "rosa"])
    tray(img, 346, "CAS")
    cx, cy, rx, ry = W / 2, 580, 164, 96
    items = orbit(img, cx, cy, rx, ry, LETRAS, phase=-1.12)
    used = [6, 2, 5]          # C, A, S en el orden en que se tocaron
    draw_tiles(img, items, used, None, back=True)
    planet(img, cx, cy + 4, 76, words=[(4, False), (5, False), (4, False), (4, False)], sprouts=3, green=0.2)
    draw_tiles(img, items, used, None, back=False)
    action_buttons(img, 760)
    d = ImageDraw.Draw(img)
    text(d, (W / 2, 862), "Tocar la última letra de la bandeja la devuelve", "n", 14, SOFT, anchor="mm")
    return img


def screen_star():
    img = sky(22)
    hud(img, 1, "0:18", "420", mult="× 3")
    time_bar(img, 0.3)
    glow(img, W / 2, 250, 200, GOLD, 90)
    d = ImageDraw.Draw(img)
    text(d, (W / 2, 126), "¡Palabra estrella!", "f", 32, GOLD, anchor="mm", stroke=3.5)
    text(d, (W / 2, 166), "Usaste las 7 letras · puntos × 3", "n", 16, WHITE, anchor="mm")
    tray(img, 232, "ASOCIAR", state=GOLD, glow_col=GOLD)
    for k, (dx, dy, rr) in enumerate(((-146, 188, 12), (150, 196, 10), (-120, 276, 9), (128, 282, 13))):
        M.sparkle(img, W / 2 + dx, dy, rr, SUN if k % 2 == 0 else WHITE)
    cx, cy, rx, ry = W / 2, 630, 164, 96
    d = ImageDraw.Draw(img)
    text(d, (W / 2, 350), "Brota un árbol dorado en tu huerto", "f", 18, WHITE, anchor="mm", stroke=2.5)
    items = orbit(img, cx, cy, rx, ry, LETRAS, phase=-2.05)
    draw_tiles(img, items, [], None, back=True)
    planet(img, cx, cy + 4, 80, words=WORDS_13, tree=True, green=0.7)
    draw_tiles(img, items, [], None, back=False)
    d = ImageDraw.Draw(img)
    text(d, (W / 2, 800), "Cosecha lista en cuanto se acabe el tiempo", "n", 16, SOFT, anchor="mm")
    text(d, (W / 2, 832), "13 palabras sembradas · 1 palabra estrella", "f", 17, SUN, anchor="mm")
    return img


def moment(img, y, title, note, tone):
    d = ImageDraw.Draw(img)
    text(d, (MARGIN, y), title, "f", 20, tone, anchor="lm")
    text(d, (MARGIN, y + 28), note, "n", 15, SOFT, anchor="lm")


def screen_moments():
    img = sky(23)
    # 1 · Ya la tienes
    moment(img, 50, "Repetida", "Late suave y avisa. Sin castigo.", SKY)
    tray(img, 150, "CASA", state=SKY, glow_col=SKY)
    d = ImageDraw.Draw(img)
    for i in range(3):
        d.arc([(W / 2 - 92 - i * 10) * S, (150 - 44 - i * 6) * S, (W / 2 + 92 + i * 10) * S, (150 + 44 + i * 6) * S], 200, 340, fill=SKY + (110 - i * 30,), width=int(2.4 * S)) if False else None
    text(d, (W / 2, 218), "Ya la tienes", "f", 22, WHITE, anchor="mm", stroke=2.5)
    # 2 · No válida
    moment(img, 292, "No válida", "Se sacude un poco y lo dice sin “error”.", CORAL)
    tray(img, 392, "CRSA", shake=6, state=CORAL)
    d = ImageDraw.Draw(img)
    for sgn in (-1, 1):                       # rayitas de movimiento a los lados: "se sacude"
        for k in range(2):
            x = W / 2 + sgn * (170 + k * 9) + 6
            d.arc([(x - 8) * S, (392 - 18) * S, (x + 8) * S, (392 + 18) * S], 270 if sgn > 0 else 90, 90 if sgn > 0 else 270, fill=CORAL, width=int(3 * S))
    text(d, (W / 2, 460), "No está en el diccionario de Nubi", "f", 20, WHITE, anchor="mm", stroke=2.5)
    # 3 · Pista
    moment(img, 534, "Pista", "Tras 15 s sin sembrar, Nubi ilumina una letra.", SUN)
    cx, cy, rx, ry = W / 2 + 44, 690, 140, 84
    items = orbit(img, cx, cy, rx, ry, LETRAS, phase=-1.12)
    draw_tiles(img, items, [], 3, back=True)
    draw_tiles(img, items, [], 3, back=False)
    nubi = Image.open(os.path.join(NUBI, "nubi_mira.webp")).convert("RGBA").resize((130 * S, 130 * S), Image.LANCZOS)
    img.alpha_composite(nubi, (int((40 - 65) * S), int((770 - 65) * S)))
    M.clay_rr(img, (96, 824, 392, 868), 22, PLACA, INK, 3.5, 4)
    d = ImageDraw.Draw(img)
    text(d, (244, 846), "Prueba con la letra que brilla", "n", 15, INK, anchor="mm")
    text(d, (W / 2, 892), "La pista se anota en tu medida", "n", 14, SOFT, anchor="mm")
    return img


def bar(img, x0, y, w, frac, col=GRAPE, h=11):
    ov(img, lambda dd: dd.rounded_rectangle([x0 * S, y * S, (x0 + w) * S, (y + h) * S], h * S / 2, fill=(255, 255, 255, 40)))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([x0 * S, y * S, (x0 + max(w * frac, h)) * S, (y + h) * S], h * S / 2, fill=col)


def split_bar(img, x0, y, w, frac, c1, c2, h=14):
    d = ImageDraw.Draw(img)
    cut = x0 + w * frac
    d.rounded_rectangle([x0 * S, y * S, (x0 + w) * S, (y + h) * S], h * S / 2, fill=c2)
    d.rounded_rectangle([x0 * S, y * S, cut * S, (y + h) * S], h * S / 2, fill=c1)
    d.rectangle([(cut - h / 2) * S, y * S, cut * S, (y + h) * S], fill=c1)
    d.line([(cut * S, (y - 2) * S), (cut * S, (y + h + 2) * S)], fill=INK, width=int(2.5 * S))


def screen_result():
    img = sky(24)
    d = ImageDraw.Draw(img)
    text(d, (W / 2, 40), "Cosecha de palabras", "f", 26, WHITE, anchor="mm", stroke=3)
    d.ellipse([(W / 2 - 62) * S, 62 * S, (W / 2 - 50) * S, 74 * S], fill=M.hexrgb("#10B981"))
    text(d, (W / 2 - 42, 68), "Lenguaje · Nivel 5 · Reto", "n", 15, SOFT, anchor="lm")
    glow(img, W / 2, 128, 80, LIME, 80)
    d = ImageDraw.Draw(img)
    text(d, (W / 2 - 4, 128), "34", "f", 64, SUN, anchor="rm", weight=700, stroke=3)
    text(d, (W / 2 + 6, 142), "palabras", "f", 20, WHITE, anchor="lm")
    text(d, (W / 2, 186), "de las comunes, 21 de 48", "f", 18, WHITE, anchor="mm")

    # Tu manera de buscar
    text(d, (W / 2, 232), "Tu manera de buscar", "f", 19, SUN, anchor="mm")
    split_bar(img, MARGIN + 6, 252, W - 2 * (MARGIN + 6), 0.60, GRAPE, SKY)
    d = ImageDraw.Draw(img)
    text(d, (MARGIN + 6, 286), "Racimos 60 %", "f", 16, WHITE, anchor="lm")
    text(d, (W - MARGIN - 6, 286), "Saltos 40 %", "f", 16, WHITE, anchor="rm")
    text(d, (W / 2, 312), "cómo buscaste en esta partida", "n", 14, SOFT, anchor="mm")
    for i, l in enumerate(("Exprimes bien cada idea (casa, casas, caso).", "Prueba también saltar a otra letra inicial.")):
        text(d, (W / 2, 340 + 22 * i), l, "n", 15, WHITE, anchor="mm")

    # Tu ritmo
    text(d, (W / 2, 408), "Tu ritmo", "f", 19, SUN, anchor="mm")
    rows = [("Primeros 20 s", 0.9, "13 palabras"), ("Últimos 20 s", 0.45, "6 palabras")]
    for i, (lab, frac, t) in enumerate(rows):
        y = 436 + i * 32
        text(d, (MARGIN, y + 7), lab, "f", 15, WHITE, anchor="lm")
        bar(img, 150, y, 130, frac, GRAPE if i == 0 else SKY)
        d = ImageDraw.Draw(img)
        text(d, (W - MARGIN, y + 7), t, "f", 15, WHITE, anchor="rm")
    text(d, (W / 2, 516), "Arrancas fuerte y bajas al final: es lo normal.", "n", 15, WHITE, anchor="mm")
    text(d, (W / 2, 538), "Si te atascas, cambia de idea: otra letra o otra terminación.", "n", 14, SOFT, anchor="mm")

    # Tu palabra estrella + también podías
    text(d, (W / 2, 590), "Tu palabra estrella", "f", 19, SUN, anchor="mm")
    glow(img, W / 2, 636, 120, GOLD, 90)
    d = ImageDraw.Draw(img)
    letter(d, (W / 2, 636), "ASOCIAR", fill=GOLD, sp=36)
    M.sparkle(img, W / 2 + atk_w("ASOCIAR", 36) / 2 + 18, 622, 9, WHITE)
    M.sparkle(img, W / 2 - atk_w("ASOCIAR", 36) / 2 - 16, 648, 7, SUN)
    d = ImageDraw.Draw(img)
    text(d, (W / 2, 684), "También podías: saciar, acosar,", "n", 15, WHITE, anchor="mm")
    text(d, (W / 2, 706), "rosca, ácaros, orcas.", "n", 15, WHITE, anchor="mm")
    text(d, (W / 2, 742), "Medida de esta partida. No es un diagnóstico.", "n", 14, SOFT, anchor="mm")
    M.clay_rr(img, (MARGIN, 770, W - MARGIN, 826), 28, SUN, INK, 3.5, 4)
    d = ImageDraw.Draw(img)
    text(d, (W / 2, 798), "Siguiente juego (3 de 3)", "f", 20, INK, anchor="mm")
    M.clay_rr(img, (MARGIN, 840, W - MARGIN, 886), 23, WHITE, INK, 3.5, 4)
    d = ImageDraw.Draw(img)
    text(d, (W / 2, 863), "Jugar de nuevo", "f", 18, INK, anchor="mm")
    return img



PANEL_H = 470


def strip_panel(title, sub, words, tree, green, sprouts, seed):
    """Un cuadro de la tira "El huerto crece": el mismo planeta, solo, en uno de sus tres estados."""
    ph = PANEL_H
    img = sky(seed)
    img = img.crop((0, 0, W * S, ph * S))
    cx, cy, r = W / 2, 300, 100
    planet(img, cx, cy, r, words=words, tree=tree, green=green, sprouts=sprouts)
    d = ImageDraw.Draw(img)
    text(d, (W / 2, PANEL_H - 56), title, "f", 20, WHITE, anchor="mm", stroke=2.5)
    text(d, (W / 2, PANEL_H - 28), sub, "n", 15, SOFT, anchor="mm")
    return img


def growth_strip():
    import random
    rnd = random.Random(11)
    w30 = [(rnd.choice((3, 3, 4, 4, 4, 5, 5, 6)), False) for _ in range(29)]
    w30[5] = (6, True)
    w30[17] = (5, True)
    w12 = [(rnd.choice((3, 4, 4, 5, 5, 6)), False) for _ in range(12)]
    w12[4] = (5, True)
    return [strip_panel("Al empezar", "0 palabras: tierra, pasto ralo y 3 brotes", [], False, 0.15, 3, 31),
            strip_panel("A mitad de la cosecha", "12 palabras: flores, arbustos, girasoles y hongos", w12, False, 0.55, 3, 32),
            strip_panel("Al final", "30 palabras y la palabra estrella: árbol dorado", w30, True, 1.0, 3, 33)]


# ---------------------------------------------------------------- reglas y hoja


def check_rules():
    bad = [(s, sp) for s, sp, word, _ in M.USED if sp < 14]
    assert not bad, f"texto demasiado chico: {bad}"
    short = [(s, sp) for s, sp in LETTERS_USED if sp < LETTER_SP]
    assert not short, f"letra bajo {LETTER_SP} sp: {short}"
    bg = M.hexrgb(GRAD[2])
    low = []
    for s, sp, word, col in M.USED:
        if col == INK:
            continue
        c = M.contrast(col, bg)
        if c < 4.5:
            low.append((s, round(c, 1)))
    assert not low, f"contraste bajo: {low}"
    worst = min(M.contrast(INK, t) for t in TILES)
    edge = min(M.contrast(t, EARTH) for t in TILES)
    assert edge >= 1.5, edge
    assert worst >= 4.5, worst
    return f"letra en tinta sobre las fichas, peor caso {worst:.1f}:1; sobre lima {M.contrast(INK, LIME):.1f}:1, sobre sol {M.contrast(INK, SUN):.1f}:1"


def main():
    shots = [("1 · Así se juega", "7 lunas en órbita, bandeja con “CAS” y el huerto", screen_play()),
             ("2 · Palabra estrella", "usa las 7 letras: árbol dorado y puntos × 3", screen_star()),
             ("3 · Momentos chicos", "repetida, no válida y pista", screen_moments()),
             ("4 · Al final", "cómo buscaste, tu ritmo y también podías", screen_result())]
    strip = growth_strip()
    report = check_rules()
    pad, head, strip_h = 28, 90, PANEL_H + 90
    sheet = Image.new("RGB", (len(shots) * (W + pad) + pad, H + head + strip_h + pad), (236, 234, 244))
    sd = ImageDraw.Draw(sheet)
    fh = ImageFont.truetype(M.FONT, 26)
    try:
        fh.set_variation_by_axes([650])
    except OSError:
        pass
    fs = ImageFont.truetype(M.NUNITO, 16)
    for i, (t, s, im) in enumerate(shots):
        x = pad + i * (W + pad)
        sd.text((x, 14), t, font=fh, fill=INK)
        sd.text((x, 52), s, font=fs, fill=(90, 84, 120))
        ph = phone(im)
        sheet.paste(ph, (x, head), ph)
    y0 = head + H + 28
    sd.text((pad, y0), "5 · El huerto crece", font=fh, fill=INK)
    sd.text((pad, y0 + 38), "el mismo planeta en tres momentos de una cosecha: tierra con pasto ralo, huerto a medias y huerto lleno con su árbol dorado", font=fs, fill=(90, 84, 120))
    for i, im in enumerate(strip):
        ph = im.resize((W, PANEL_H), Image.LANCZOS).convert("RGB")
        mask = Image.new("L", (W, PANEL_H), 0)
        ImageDraw.Draw(mask).rounded_rectangle([0, 0, W - 1, PANEL_H - 1], 30, fill=255)
        sheet.paste(ph, (pad + i * (W + pad), y0 + 80), mask)
    sheet.save(OUT)
    print(OUT)
    print("Contrastes:", report)


if __name__ == "__main__":
    main()
