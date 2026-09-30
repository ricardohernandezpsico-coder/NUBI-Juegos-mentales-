"""Maqueta de Nubi con 4 áreas (30-sep): Ricardo decidió pasar de 6 a 4 áreas y usar el fondo C. Tres pantallas de
teléfono (412 x 915 dp) para aprobar antes de programar: Hoy B final (nombres cortos + subtítulo, Nubi grande)
y dos propuestas para Juegos (1 Lunas en órbita, 2 Nubi te sugiere). Maqueta PIL, NO la app.

  python tools/previews/cuatro_areas.py  ->  docs/previews/cuatro-areas.png

Reglas de diseño que se comprueban aquí: ningún texto menor de 14 sp (13 solo en la barra de pestañas), contraste de
texto >= 4,5:1 sobre el fondo, sin emojis, y el estado nunca se dice solo con color (etapa en palabras).
"""
import os
import random

from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
RES = os.path.join(ROOT, "app", "src", "main", "res")
FONT = os.path.join(RES, "font", "fredoka.ttf")
NUNITO = os.path.join(RES, "font", "nunito_regular.ttf")
OUT = os.path.join(ROOT, "docs", "previews", "cuatro-areas.png")

W, H = 412, 915
S = 2  # supermuestreo

# Fondo C (tools/previews/fondo_oscuro.py): degradado noche profunda, nebulosas -40%, barra oscura
GRAD = ["#02030F", "#050823", "#0A0F33"]
NEB = (0x28, 0x1C, 0x24)
NAV_BG, NAV_EDGE, NAV_ACTIVE = (20, 17, 46), (58, 46, 120), (60, 48, 120)
NAV_FG, NAV_ON = (160, 152, 200), (255, 255, 255)

LAVENDER = (184, 164, 255)
SUN = (255, 201, 60)
INK = (26, 18, 64)
WHITE = (255, 255, 255)
SOFT = (214, 208, 245)  # #D6D0F5
SKY = (76, 201, 240)

STAGES = ["Inicio", "Aprendiz", "Hábil", "Experto", "Maestro"]

# clave, nombre completo, [línea 1, línea 2], nombre corto, subtítulo (B), color, planeta, avance, cambio semana, juegos
AREAS = [
    dict(key="memoria", name="Memoria", lines=["Memoria"], short="Memoria", sub="recordar y ubicar",
         color="#3B82F6", planet="area_memoria.webp", v=51, dv=6, games=6),
    dict(key="atencion", name="Atención y velocidad", lines=["Atención", "y velocidad"], short="Atención",
         sub="foco y velocidad", color="#F59E0B", planet="area_atencion.webp", v=62, dv=4, games=7),
    dict(key="razonamiento", name="Razonamiento y números", lines=["Razonamiento", "y números"],
         short="Razonamiento", sub="lógica y números", color="#8B5CF6", planet="area_razonamiento.webp",
         v=43, dv=0, games=5),
    dict(key="lenguaje", name="Lenguaje", lines=["Lenguaje"], short="Lenguaje", sub="palabras y letras",
         color="#10B981", planet="area_lenguaje.webp", v=12, dv=3, games=1),
]
BY = {a["key"]: a for a in AREAS}


def hex_rgb(h):
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


_fcache = {}


def fredoka(sp, weight=650):
    key = ("f", sp, weight)
    if key not in _fcache:
        f = ImageFont.truetype(FONT, int(sp * S))
        try:
            f.set_variation_by_axes([weight])
        except OSError:
            pass
        _fcache[key] = f
    return _fcache[key]


def nunito(sp):
    key = ("n", sp)
    if key not in _fcache:
        _fcache[key] = ImageFont.truetype(NUNITO, int(sp * S))
    return _fcache[key]


# Regla: nada menor de 14 sp (13 solo en la barra de pestañas). Se anota cada tamaño usado para comprobarlo al final.
USED_SIZES = []


def text(d, xy, s, font_kind, sp, fill, anchor="la", weight=650):
    USED_SIZES.append((s, sp, font_kind))
    f = fredoka(sp, weight) if font_kind == "f" else nunito(sp)
    d.text((xy[0] * S, xy[1] * S), s, font=f, fill=fill, anchor=anchor)


def tw(s, font_kind, sp, weight=650):
    f = fredoka(sp, weight) if font_kind == "f" else nunito(sp)
    return f.getlength(s) / S


def gradient(stops, w, h):
    cols = [hex_rgb(s) for s in stops]
    img = Image.new("RGB", (1, h))
    px = img.load()
    for y in range(h):
        t = y / (h - 1) * (len(cols) - 1)
        i = min(int(t), len(cols) - 2)
        f = t - i
        px[0, y] = tuple(int(cols[i][k] + (cols[i + 1][k] - cols[i][k]) * f) for k in range(3))
    return img.resize((w, h))


def radial(base, center, radius, color, alpha, blur=0.08):
    layer = Image.new("RGBA", base.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    steps = 40
    for i in range(steps, 0, -1):
        r = radius * i / steps
        a = int(alpha * (1 - i / steps))
        d.ellipse([center[0] - r, center[1] - r, center[0] + r, center[1] + r], fill=tuple(color) + (a,))
    return Image.alpha_composite(base, layer.filter(ImageFilter.GaussianBlur(radius * blur)))


def sky():
    w, h = W * S, H * S
    img = gradient(GRAD, w, h).convert("RGBA")
    a1, a2, a3 = NEB
    img = radial(img, (w * 0.9, h * 0.10), w * 0.9, SKY, a1)
    img = radial(img, (w * 0.1, h * 0.55), w * 0.8, LAVENDER, a2)
    img = radial(img, (w * 0.05, h * 0.95), w * 0.85, (255, 107, 74), a3)
    rnd = random.Random(7)
    stars = [(rnd.random() * w, rnd.random() * h, (0.6 + rnd.random() * 1.6) * S, rnd.random() < 0.18,
              int(90 + rnd.random() * 140)) for _ in range(70)]

    def draw(d):
        for x, y, r, warm, a in stars:
            d.ellipse([x - r, y - r, x + r, y + r], fill=((255, 195, 138) if warm else (255, 255, 255)) + (a,))
    ov(img, draw)
    return img


def ov(img, fn):
    """Dibuja con transparencia de verdad (PIL pisa el alfa si se dibuja directo sobre una imagen RGBA)."""
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    fn(ImageDraw.Draw(layer))
    img.alpha_composite(layer)


def planet_img(name, size_dp):
    p = Image.open(os.path.join(RES, "drawable-nodpi", name)).convert("RGBA")
    n = int(size_dp * S)
    return p.resize((n, n), Image.LANCZOS)


def glow_under(img, cx, cy, r, color, alpha):
    return radial(img, (cx * S, cy * S), r * S, hex_rgb(color), alpha, blur=0.2)


def paste_planet(img, area, cx, cy, size):
    img = glow_under(img, cx, cy, size * 0.85, area["color"], 120)
    p = planet_img(area["planet"], size)
    img.alpha_composite(p, (int(cx * S - p.width / 2), int(cy * S - p.height / 2)))
    return img


def stage_of(v):
    return min(4, int(v // 20))


def progress_bar(img, x, y, w, v, dv, h=9):
    """Barra de 0 a 100 con 5 etapas (4 marcas). Lavanda = lo de antes, sol = lo avanzado esta semana."""
    X = lambda val: (x + w * val / 100) * S
    ov(img, lambda dd: dd.rounded_rectangle([x * S, y * S, (x + w) * S, (y + h) * S], h * S / 2, fill=(255, 255, 255, 40)))
    d = ImageDraw.Draw(img, "RGBA")
    before = max(0, v - dv)
    if before > 0:
        d.rounded_rectangle([x * S, y * S, max(X(before), x * S + h * S), (y + h) * S], h * S / 2, fill=LAVENDER)
    if dv > 0:
        d.rounded_rectangle([max(x * S, X(before) - h * S / 2), y * S, X(v), (y + h) * S], h * S / 2, fill=SUN)
        r = h * S * 0.22
        cy = (y + h / 2) * S
        d.ellipse([X(v) - r * 2, cy - r, X(v) - r * 0, cy + r], fill=WHITE)
    for k in range(1, 5):
        xx = x * S + w * S * k / 5
        d.line([(xx, y * S), (xx, (y + h) * S)], fill=(10, 10, 40), width=int(1.4 * S))


def nubi(img, cx, cy, size):
    halo = Image.new("RGBA", img.size, (0, 0, 0, 0))
    hd = ImageDraw.Draw(halo)
    for i in range(30, 0, -1):
        r = size * 0.85 * S * i / 30
        hd.ellipse([cx * S - r, cy * S - r, cx * S + r, cy * S + r], fill=(150, 130, 255, int(60 * (1 - i / 30))))
    img = Image.alpha_composite(img, halo.filter(ImageFilter.GaussianBlur(10 * S)))
    for rr in (size * 0.5, size * 0.56):
        ov(img, lambda dd, rr=rr: dd.ellipse([(cx - rr) * S, (cy - rr) * S, (cx + rr) * S, (cy + rr) * S],
                                             outline=(190, 180, 255, 60), width=S))
    n = Image.open(os.path.join(RES, "drawable-nodpi", "nubi_hola.webp")).convert("RGBA")
    ns = int(size * S)
    n = n.resize((ns, int(n.height * ns / n.width)), Image.LANCZOS)
    img.alpha_composite(n, (int(cx * S - n.width / 2), int(cy * S - n.height / 2)))
    return img


def bubble(img, cx, y, line1, line2):
    d = ImageDraw.Draw(img, "RGBA")
    w = max(tw(line2, "f", 20, 700), tw(line1, "n", 14)) + 44
    x0, x1, y1 = cx - w / 2, cx + w / 2, y + 70
    d.rounded_rectangle([x0 * S, (y + 4) * S, x1 * S, (y1 + 4) * S], 22 * S, fill=(70, 50, 150, 255))
    d.rounded_rectangle([x0 * S, y * S, x1 * S, y1 * S], 22 * S, fill=(250, 248, 255, 255),
                        outline=(70, 50, 150, 255), width=3 * S)
    d.polygon([((cx - 10) * S, y1 * S), ((cx + 10) * S, y1 * S), (cx * S, (y1 + 12) * S)], fill=(250, 248, 255, 255))
    text(d, (x0 + 20, y + 10), line1, "n", 14, (90, 80, 140))
    text(d, (x0 + 20, y + 32), line2, "f", 20, INK, weight=700)


def tab_bar(img, selected):
    d = ImageDraw.Draw(img, "RGBA")
    y0, y1 = H - 86, H - 16
    d.rounded_rectangle([16 * S, y0 * S, (W - 16) * S, y1 * S], 30 * S, fill=NAV_BG + (255,), outline=NAV_EDGE + (255,),
                        width=2 * S)
    labels = ["Hoy", "Juegos", "Avance"]
    for i, lab in enumerate(labels):
        cx = 16 + (W - 32) * (i + 0.5) / 3
        on = i == selected
        col = NAV_ON if on else NAV_FG
        if on:
            d.rounded_rectangle([(cx - 46) * S, (y0 + 8) * S, (cx + 46) * S, (y0 + 60) * S], 22 * S, fill=NAV_ACTIVE + (255,))
        iy = y0 + 22
        lw = 2 * S
        if i == 0:  # casa
            d.polygon([((cx - 10) * S, (iy + 8) * S), (cx * S, (iy - 2) * S), ((cx + 10) * S, (iy + 8) * S)], outline=col, width=lw)
            d.rectangle([(cx - 7) * S, (iy + 8) * S, (cx + 7) * S, (iy + 18) * S], outline=col, width=lw)
        elif i == 1:  # jugar
            d.ellipse([(cx - 10) * S, (iy - 2) * S, (cx + 10) * S, (iy + 18) * S], outline=col, width=lw)
            d.polygon([((cx - 3) * S, (iy + 3) * S), ((cx + 5) * S, (iy + 8) * S), ((cx - 3) * S, (iy + 13) * S)], fill=col)
        else:  # avance: tres barras
            for k, hh in enumerate((8, 14, 20)):
                bx = cx - 10 + k * 8
                d.rounded_rectangle([bx * S, (iy + 18 - hh) * S, (bx + 5) * S, (iy + 18) * S], 2 * S, fill=col)
        text(d, (cx, y0 + 53), lab, "n", 13, col, anchor="ms")
    return img


def top_buttons(img):
    d = ImageDraw.Draw(img, "RGBA")
    # perfil (círculo celeste con R) y opciones (engranaje), como en docs/previews/juegos-areas-real.png
    cx1, cx2, cy, r = W - 92, W - 38, 50, 22
    d.ellipse([(cx1 - r) * S, (cy - r + 3) * S, (cx1 + r) * S, (cy + r + 3) * S], fill=(40, 30, 100, 255))
    d.ellipse([(cx1 - r) * S, (cy - r) * S, (cx1 + r) * S, (cy + r) * S], fill=SKY + (255,), outline=(30, 40, 120), width=2 * S)
    text(d, (cx1, cy + 1), "R", "f", 22, INK, anchor="mm", weight=700)
    d.ellipse([(cx2 - r) * S, (cy - r + 3) * S, (cx2 + r) * S, (cy + r + 3) * S], fill=(40, 30, 100, 255))
    d.ellipse([(cx2 - r) * S, (cy - r) * S, (cx2 + r) * S, (cy + r) * S], fill=WHITE + (255,))
    import math
    for k in range(8):  # dientes del engranaje
        a = k * math.pi / 4
        px, py = cx2 + math.cos(a) * 12, cy + math.sin(a) * 12
        d.ellipse([(px - 3.4) * S, (py - 3.4) * S, (px + 3.4) * S, (py + 3.4) * S], fill=INK)
    d.ellipse([(cx2 - 10) * S, (cy - 10) * S, (cx2 + 10) * S, (cy + 10) * S], fill=INK)
    d.ellipse([(cx2 - 4.5) * S, (cy - 4.5) * S, (cx2 + 4.5) * S, (cy + 4.5) * S], fill=WHITE + (255,))


# ------------------------------------------------------------------ Hoy · B final

BAR_W = 130
MARGIN = 16
PLAYED = {"memoria": 4, "atencion": 3, "razonamiento": 1, "lenguaje": 0}


def footer(img, area, cx, top, align, name_sp=22, sub_sp=15, stage_sp=15, bar_w=BAR_W, caption=None):
    """Pie de un área: nombre, subtítulo, barra y etapa. Devuelve la y donde termina."""
    d = ImageDraw.Draw(img, "RGBA")
    anc = {"l": "l", "r": "r", "c": "m"}[align]
    tx = cx
    text(d, (tx, top), area["short"], "f", name_sp, WHITE, anchor=anc + "t")
    y = top + name_sp + 6
    text(d, (tx, y), area["sub"], "n", sub_sp, SOFT, anchor=anc + "t")
    y += sub_sp + 8
    bx = {"l": tx, "r": tx - bar_w, "c": tx - bar_w / 2}[align]
    progress_bar(img, bx, y, bar_w, area["v"], area["dv"])
    d = ImageDraw.Draw(img, "RGBA")
    y += 9 + 8
    cap = caption or STAGES[stage_of(area["v"])]
    text(d, (tx, y), cap, "n", stage_sp, SOFT, anchor=anc + "t")
    return y + stage_sp + 4


def hoy_block_b(img, area, x, y, align):
    psize = 56
    left = align == "l"
    pcx = x + psize / 2 if left else x + BAR_W - psize / 2
    img = paste_planet(img, area, pcx, y + psize / 2, psize)
    footer(img, area, x if left else x + BAR_W, y + psize + 8, align)
    return img


def screen_hoy_b():
    img = sky()
    avanzaron = sum(1 for a in AREAS if a["dv"] > 0)
    bubble(img, W / 2, 44, "Nubi", f"{avanzaron} áreas avanzaron esta semana")
    block_h, nubi_size, gap = 160, 230, 14
    region_top, region_bot = 150, H - 100
    total = block_h * 2 + nubi_size + gap * 2
    y0 = region_top + (region_bot - region_top - total) / 2
    img = nubi(img, W / 2, y0 + block_h + gap + nubi_size / 2, nubi_size)
    rows = [y0, y0 + block_h + gap * 2 + nubi_size]
    left = [BY["memoria"], BY["razonamiento"]]
    right = [BY["atencion"], BY["lenguaje"]]
    for i in range(2):
        img = hoy_block_b(img, left[i], MARGIN, rows[i], "l")
        img = hoy_block_b(img, right[i], W - MARGIN - BAR_W, rows[i], "r")
    return tab_bar(img, 0)


# ------------------------------------------------------------------ Juegos · 1 Lunas en órbita

import math


def _glow(size, x, y, r, color, alpha):
    layer = Image.new("RGBA", size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    for i in range(20, 0, -1):
        rr = r * S * i / 20
        d.ellipse([x * S - rr, y * S - rr, x * S + rr, y * S + rr], fill=hex_rgb(color) + (int(alpha * (1 - i / 20)),))
    return layer.filter(ImageFilter.GaussianBlur(2 * S))


def orbit_point(cx, cy, a, b, tilt_deg, t):
    t0 = math.radians(tilt_deg)
    x, y = a * math.cos(t), b * math.sin(t)
    return cx + x * math.cos(t0) - y * math.sin(t0), cy + x * math.sin(t0) + y * math.cos(t0)


def moon(img, x, y, r, color, lit):
    if lit:
        img.alpha_composite(_glow(img.size, x, y, r * 2.6, color, 150))
        ImageDraw.Draw(img, "RGBA").ellipse([(x - r) * S, (y - r) * S, (x + r) * S, (y + r) * S],
                                            fill=hex_rgb(color) + (255,), outline=WHITE + (255,), width=int(1.6 * S))
    else:
        ImageDraw.Draw(img, "RGBA").ellipse([(x - r) * S, (y - r) * S, (x + r) * S, (y + r) * S],
                                            fill=(10, 15, 51, 255), outline=(190, 184, 230, 255), width=int(1.6 * S))


def orbit_cell(img, area, cx, cy, psize=132):
    a_ax, b_ax, tilt = psize * 0.62, psize * 0.2, -16
    img = glow_under(img, cx, cy, psize * 0.9, area["color"], 120)
    n = area["games"]
    lit_n = PLAYED[area["key"]]
    moons = []
    for i in range(n):
        t = 2 * math.pi * (i + 0.5) / n + math.pi * 0.1
        mx, my = orbit_point(cx, cy, a_ax, b_ax, tilt, t)
        moons.append((mx, my, math.sin(t), i < lit_n))
    ring_col = hex_rgb(area["color"]) + (200,)
    steps = 120

    def arc(front):
        run = []
        for k in range(steps + 1):
            t = 2 * math.pi * k / steps
            if (math.sin(t) >= 0) == front:
                px, py = orbit_point(cx, cy, a_ax, b_ax, tilt, t)
                run.append((px * S, py * S))
            elif run:
                ov(img, lambda dd, c=run: dd.line(c, fill=ring_col, width=int(1.8 * S)))
                run = []
        if run:
            ov(img, lambda dd, c=run: dd.line(c, fill=ring_col, width=int(1.8 * S)))

    arc(False)
    for mx, my, s, lit in moons:
        if s < 0:
            moon(img, mx, my, 7.5, area["color"], lit)
    p = planet_img(area["planet"], psize)
    img.alpha_composite(p, (int(cx * S - p.width / 2), int(cy * S - p.height / 2)))
    arc(True)
    for mx, my, s, lit in moons:
        if s >= 0:
            moon(img, mx, my, 8.5, area["color"], lit)
    return img


def screen_juegos_1():
    img = sky()
    top_buttons(img)
    gy0, gy1 = 92, H - 98
    cw, ch = W / 2, (gy1 - gy0) / 2
    order = [BY["memoria"], BY["atencion"], BY["razonamiento"], BY["lenguaje"]]
    psize = 140
    foot_h = 24 + 6 + 15 + 8 + 9 + 8 + 14 + 4
    zone = psize + 44
    for i, a in enumerate(order):
        col, row = i % 2, i // 2
        cx = cw * col + cw / 2
        y = gy0 + ch * row + (ch - (zone + foot_h)) / 2
        img = orbit_cell(img, a, cx, y + zone / 2, psize)
        jg = f"{PLAYED[a['key']]} de {a['games']} esta semana"
        footer(img, a, cx, y + zone, "c", name_sp=24, stage_sp=14, bar_w=140, caption=f"{STAGES[stage_of(a['v'])]} · {jg}")
    return tab_bar(img, 1)


# ------------------------------------------------------------------ Juegos · 2 Nubi te sugiere


def screen_juegos_2():
    img = sky()
    top_buttons(img)
    n = Image.open(os.path.join(RES, "drawable-nodpi", "nubi_cientifica.webp")).convert("RGBA")
    ns = 96 * S
    n = n.resize((ns, int(n.height * ns / n.width)), Image.LANCZOS)
    img = radial(img, (66 * S, 130 * S), 70 * S, LAVENDER, 70, blur=0.2)
    img.alpha_composite(n, (int(16 * S), int(84 * S)))
    d = ImageDraw.Draw(img, "RGBA")
    text(d, (122, 94), "¿Qué entrenamos hoy?", "f", 24, WHITE, anchor="lt")
    sug = BY["razonamiento"]
    for k, ln in enumerate(["Te sugiero Razonamiento:", "hace 4 días que no lo juegas"]):
        text(d, (122, 128 + k * 21), ln, "n", 15, SOFT, anchor="lt")
    gy0, gy1 = 204, H - 98
    cw, ch = W / 2, (gy1 - gy0) / 2
    order = [BY["memoria"], BY["atencion"], sug, BY["lenguaje"]]
    foot_h = 22 + 6 + 15 + 8 + 9 + 8 + 15 + 4
    zone = 162
    for i, a in enumerate(order):
        col, row = i % 2, i // 2
        cx = cw * col + cw / 2
        is_sug = a is sug
        psize = 118 if is_sug else 88
        y = gy0 + ch * row + (ch - (zone + foot_h)) / 2
        cy = y + zone / 2
        if is_sug:
            rr = psize / 2 + 13
            img.alpha_composite(_glow(img.size, cx, cy, rr * 1.4, "#FFC93C", 55))
            ov(img, lambda dd, cx=cx, cy=cy, rr=rr: dd.ellipse([(cx - rr) * S, (cy - rr) * S, (cx + rr) * S, (cy + rr) * S],
                                                                outline=SUN + (255,), width=int(3 * S)))
        img = paste_planet(img, a, cx, cy, psize)
        if is_sug:
            d = ImageDraw.Draw(img, "RGBA")
            pw = tw("Sugerida", "f", 15) + 24
            py = cy - rr - 11
            d.rounded_rectangle([(cx - pw / 2) * S, py * S, (cx + pw / 2) * S, (py + 24) * S], 12 * S, fill=SUN + (255,))
            text(d, (cx, py + 12.5), "Sugerida", "f", 15, INK, anchor="mm")
        jg = "juego" if a["games"] == 1 else "juegos"
        footer(img, a, cx, y + zone, "c", bar_w=140, caption=f"{STAGES[stage_of(a['v'])]} · {a['games']} {jg}")
    return tab_bar(img, 1)


# ------------------------------------------------------------------ Lámina


def phone(img):
    out = img.resize((W, H), Image.LANCZOS).convert("RGB")
    mask = Image.new("L", (W, H), 0)
    ImageDraw.Draw(mask).rounded_rectangle([0, 0, W - 1, H - 1], 30, fill=255)
    res = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    res.paste(out, (0, 0), mask)
    return res


def check_rules():
    bad = [(s, sp) for s, sp, _ in USED_SIZES if sp < 14 and sp != 13]
    assert not bad, f"texto menor de 14 sp: {bad}"
    # 13 sp solo en la barra de pestañas
    thirteen = {s for s, sp, _ in USED_SIZES if sp == 13}
    assert thirteen <= {"Hoy", "Juegos", "Avance"}, thirteen
    bg_dark = hex_rgb(GRAD[0])
    bg_light = hex_rgb(GRAD[2])
    report = []
    for name, col, bg in [("texto blanco", WHITE, bg_light), ("texto suave #D6D0F5", SOFT, bg_light),
                          ("pestaña inactiva", NAV_FG, NAV_BG), ("pestaña activa", NAV_ON, NAV_ACTIVE),
                          ("nombre en burbuja", INK, (250, 248, 255)), ("subtítulo burbuja", (90, 80, 140), (250, 248, 255))]:
        c = contrast(col, bg)
        report.append(f"{name}: {c:.1f}:1")
        assert c >= 4.5, f"contraste bajo en {name}: {c:.2f}"
    return report


def main():
    shots = [("Hoy · B final", "Nubi grande, nombres cortos y subtítulo", screen_hoy_b()),
             ("Juegos · 1 Lunas en órbita", "una luna por juego; encendida = jugado esta semana", screen_juegos_1()),
             ("Juegos · 2 Nubi te sugiere", "el área sugerida se destaca", screen_juegos_2())]
    report = check_rules()
    pad, head = 28, 92
    sheet = Image.new("RGB", (len(shots) * (W + pad) + pad, H + head + pad), (236, 234, 244))
    d = ImageDraw.Draw(sheet)
    f_h = ImageFont.truetype(FONT, 26)
    try:
        f_h.set_variation_by_axes([650])
    except OSError:
        pass
    f_s = ImageFont.truetype(NUNITO, 16)
    for i, (title, sub, img) in enumerate(shots):
        x = pad + i * (W + pad)
        d.text((x, 14), title, font=f_h, fill=INK)
        d.text((x, 52), sub, font=f_s, fill=(90, 84, 120))
        sheet.paste(phone(img), (x, head), phone(img))
    sheet.save(OUT)
    print(OUT)
    print("Contrastes:", "; ".join(report))


if __name__ == "__main__":
    main()
