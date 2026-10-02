"""Maqueta de "La estrella intrusa" (1-oct; diseño en docs/diseno-estrella-intrusa.md): 4 pantallas de teléfono (412 x 915 dp) sobre
el fondo C, en Pillow, con las mismas piezas y reglas que las maquetas aprobadas (meteoros, disparate, cosecha): arcilla, borde tinta,
sombra dura, textos >= 14 sp, palabras en Atkinson Hyperlegible Bold 24 sp, contraste >= 4,5:1, nunca solo color. Es una maqueta, NO
la app. Los grupos del ejemplo salen de la base de significados (tools/intrusa) y pasan el verificador de unicidad.

  python tools/art-preview/intrusa.py  ->  docs/previews/intrusa.png
"""
import math
import os

from PIL import Image, ImageDraw, ImageFont

import meteoros as M
from meteoros import (CORAL, GRAD, GRAPE, INK, LIME, PLACA, SKY, SOFT, SUN, W, H, WHITE, glow, ov, phone, sky, text)

S = M.S
OUT = os.path.join(M.ROOT, "docs", "previews", "intrusa.png")
ATKINSON = os.path.join(M.ROOT, "tools", "previews", "fuentes-letra", "AtkinsonHyperlegible-Bold.ttf")
MARGIN = 28
WORD_SP = 24
LENGUAJE = (110, 206, 70)            # verde del área Lenguaje, un poco más claro para el halo sobre el cielo oscuro
_atk = {}
WORDS_USED = []


def atkinson(sp):
    if sp not in _atk:
        _atk[sp] = ImageFont.truetype(ATKINSON, int(sp * S))
    return _atk[sp]


def word(d, xy, s, fill=INK, sp=WORD_SP, anchor="mm"):
    WORDS_USED.append((s, sp))
    d.text((xy[0] * S, xy[1] * S), s, font=atkinson(sp), fill=fill, anchor=anchor)


def word_w(s, sp=WORD_SP):
    return atkinson(sp).getlength(s) / S


# ---------------------------------------------------------------- piezas

def hud(img, nivel, racha, puntos, comet=0.6, tiempo="1:12"):
    M.clay_rr(img, (24, 36, 100, 66), 14, (38, 46, 110), INK, 3, 3)
    M.clay_rr(img, (108, 36, 188, 66), 14, (38, 46, 110), INK, 3, 3)
    M.clay_rr(img, (196, 36, 270, 66), 14, (38, 46, 110), INK, 3, 3)
    d = ImageDraw.Draw(img)
    text(d, (62, 51), f"Nivel {nivel}", "f", 15, SKY, anchor="mm")
    text(d, (148, 51), f"Racha {racha}", "f", 15, SUN, anchor="mm")
    text(d, (233, 51), puntos, "f", 15, WHITE, anchor="mm")
    text(d, (W - MARGIN, 51), tiempo, "f", 15, SOFT, anchor="rm")
    comet_bar(img, comet)


def comet_bar(img, frac, y=86):
    """El tiempo de la partida: un cometa que cruza arriba (la estela es lo que ya pasó)."""
    x0, x1 = MARGIN, W - MARGIN
    hx = x0 + (x1 - x0) * frac
    ov(img, lambda dd: dd.line([(x0 * S, y * S), (x1 * S, y * S)], fill=(255, 255, 255, 36), width=int(2 * S)))

    def tail(d):
        for i in range(28):
            t = i / 27
            xx = hx - (hx - x0) * t
            a = int(190 * (1 - t) ** 1.6)
            r = (3.2 - 2.0 * t) * S
            d.ellipse([xx * S - r, y * S - r, xx * S + r, y * S + r], fill=SKY + (a,))
    ov(img, tail, blur=0.5)
    glow(img, hx, y, 16, SUN, 160)
    d = ImageDraw.Draw(img)
    d.ellipse([(hx - 6) * S, (y - 6) * S, (hx + 6) * S, (y + 6) * S], fill=PLACA, outline=INK, width=int(2 * S))


def lines(img, pts, bright=None, dotted=None, faint=True):
    """Líneas tenues entre todas las estrellas (y, si se pide, las que se encienden)."""
    def draw(d):
        if faint:
            for i in range(len(pts)):
                for j in range(i + 1, len(pts)):
                    d.line([(pts[i][0] * S, pts[i][1] * S), (pts[j][0] * S, pts[j][1] * S)], fill=(255, 255, 255, 34), width=int(1.6 * S))
    ov(img, draw)
    if bright:
        def lit(d):
            for a, b in bright:
                d.line([(pts[a][0] * S, pts[a][1] * S), (pts[b][0] * S, pts[b][1] * S)], fill=(255, 244, 200, 255), width=int(3.2 * S))
        ov(img, lit, blur=0.0)
        ov(img, lambda d: [d.line([(pts[a][0] * S, pts[a][1] * S), (pts[b][0] * S, pts[b][1] * S)], fill=SUN + (110,), width=int(9 * S)) for a, b in bright], blur=3)


def dashed(img, a, b, col, w=3.2, dash=9, gap=7):
    d = ImageDraw.Draw(img)
    dist = math.hypot(b[0] - a[0], b[1] - a[1])
    n = int(dist // (dash + gap))
    ux, uy = (b[0] - a[0]) / dist, (b[1] - a[1]) / dist
    for i in range(n + 1):
        s0 = i * (dash + gap)
        s1 = min(dist, s0 + dash)
        d.line([((a[0] + ux * s0) * S, (a[1] + uy * s0) * S), ((a[0] + ux * s1) * S, (a[1] + uy * s1) * S)], fill=col, width=int(w * S))


def star(img, cx, cy, halo=LENGUAJE, r=19, state=None, glow_a=140):
    """Estrella-palabra: núcleo crema de arcilla con borde tinta y halo del color del área. state: None / 'bad' (coral) / 'true' (aro sol)."""
    col = {"bad": CORAL, "true": SUN}.get(state, halo)
    glow(img, cx, cy, r * 2.5, col, glow_a if state is None else 190)
    M.clay_star(img, cx, cy, r, fill=PLACA)
    if state == "bad":
        M.mark(img, cx, cy - 1, False, r=12)
    if state in ("bad", "true"):
        d = ImageDraw.Draw(img)
        d.ellipse([(cx - r - 10) * S, (cy - r - 10) * S, (cx + r + 10) * S, (cy + r + 10) * S], outline=col, width=int(4 * S))


def plate(img, cx, cy, s, state=None):
    """Placa crema con la palabra (Atkinson Bold 24 sp)."""
    ww = max(word_w(s) + 30, 92)
    hh = 44
    if state:
        glow(img, cx, cy, ww * 0.62, CORAL if state == "bad" else SUN, 110)
    M.clay_rr(img, (cx - ww / 2, cy - hh / 2, cx + ww / 2, cy + hh / 2), 15, PLACA, INK, 3.5, 5)
    if state:
        d = ImageDraw.Draw(img)
        d.rounded_rectangle([(cx - ww / 2) * S, (cy - hh / 2) * S, (cx + ww / 2) * S, (cy + hh / 2) * S], 15 * S,
                            outline=CORAL if state == "bad" else SUN, width=int(4.5 * S))
    d = ImageDraw.Draw(img)
    word(d, (cx, cy + 1), s)


# disposición de constelación (pentágono irregular): arriba, derecha-alta, derecha-baja, izquierda-baja, izquierda-alta
PTS = [(206, 232), (330, 322), (286, 482), (122, 474), (82, 318)]


def constellation(img, palabras, states=None, offset=(0, 0)):
    states = states or [None] * 5
    pts = [(x + offset[0], y + offset[1]) for x, y in PTS]
    lines(img, pts)
    for (x, y), w, st in zip(pts, palabras, states):
        star(img, x, y, state=st)
    for (x, y), w, st in zip(pts, palabras, states):
        plate(img, x, y + 50, w, state=st)
    return pts


def caption(img, y, l1, l2=None, col=WHITE):
    d = ImageDraw.Draw(img)
    text(d, (W / 2, y), l1, "f", 20, col, anchor="mm", stroke=2.5)
    if l2:
        text(d, (W / 2, y + 28), l2, "n", 16, SOFT, anchor="mm")


# ---------------------------------------------------------------- las 4 pantallas

def screen_play():
    img = sky(31)
    hud(img, 2, 3, "260")
    constellation(img, ["manzana", "pera", "martillo", "uva", "plátano"])
    caption(img, 640, "Una no pertenece. Tócala.", "Mira las cinco y decide con calma")
    d = ImageDraw.Draw(img)
    text(d, (W / 2, 748), "Cada acierto enciende la constelación", "n", 15, SOFT, anchor="mm")
    text(d, (W / 2, 774), "y la guarda en Tu cielo", "n", 15, SOFT, anchor="mm")
    return img


def shooting_star(img, x0, y0, x1, y1, label=None):
    def tail(d):
        n = 40
        for i in range(n):
            t = i / (n - 1)
            xx, yy = x1 + (x0 - x1) * t, y1 + (y0 - y1) * t
            r = (7 - 5.5 * t) * S
            d.ellipse([xx * S - r, yy * S - r, xx * S + r, yy * S + r], fill=(255, 240, 200, int(210 * (1 - t) ** 1.4)))
    ov(img, tail, blur=0.8)
    glow(img, x1, y1, 30, SUN, 170)
    M.clay_star(img, x1, y1, 15, fill=PLACA)
    if label:
        d = ImageDraw.Draw(img)
        word(d, (x1 - 6, y1 + 34), label, fill=SOFT)


def option(img, y, label, state=None):
    ww = W - 2 * MARGIN
    hh = 72
    M.clay_rr(img, (MARGIN, y, MARGIN + ww, y + hh), 26, PLACA, INK, 4, 6)
    d = ImageDraw.Draw(img)
    text(d, (W / 2, y + hh / 2), label, "f", 20, INK, anchor="mm")


def screen_done():
    img = sky(32)
    hud(img, 4, 4, "420", comet=0.52, tiempo="0:58")
    # las cuatro unidas: pentágono sin la intrusa (índice 2), líneas de luz una a una
    pts = [(x, y - 30) for x, y in PTS]
    union = [(0, 1), (1, 3), (3, 4), (4, 0)]
    lines(img, [p for i, p in enumerate(pts) if i != 2] + [pts[2]], faint=False)
    pts4 = [pts[0], pts[1], pts[3], pts[4]]
    lines(img, pts4, bright=[(0, 1), (1, 2), (2, 3), (3, 0)], faint=False)
    for (x, y), w in zip(pts4, ["serrucho", "tijera", "cuchillo", "cortaúñas"]):
        star(img, x, y, halo=SUN, glow_a=210)
        plate(img, x, y + 50, w)
    shooting_star(img, 360, 150, 396, 112)
    for dx, dy, rr in ((-96, -6, 9), (96, 18, 12), (0, -92, 8), (-30, 80, 7)):
        M.sparkle(img, 206 + dx, 380 + dy, rr, WHITE if rr % 2 else SUN)
    d = ImageDraw.Draw(img)
    glow(img, W / 2, 552, 110, SUN, 60)
    d = ImageDraw.Draw(img)
    text(d, (W / 2, 548), "Sirven para cortar", "f", 24, SUN, anchor="mm", stroke=3.5)
    text(d, (W / 2, 574), "Constelación lograda", "n", 15, WHITE, anchor="mm")
    # bonus
    text(d, (W / 2, 618), "¿Qué las une?", "f", 21, WHITE, anchor="mm", stroke=2.5)
    ov(img, lambda dd: dd.rounded_rectangle([MARGIN * S, 640 * S, (W - MARGIN) * S, 650 * S], 5 * S, fill=(255, 255, 255, 40)))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([MARGIN * S, 640 * S, (MARGIN + (W - 2 * MARGIN) * 0.62) * S, 650 * S], 5 * S, fill=SKY)
    text(d, (W - MARGIN, 622), "4 s", "f", 15, SKY, anchor="rm")
    for y, l in ((662, "Sirven para beber"), (742, "Sirven para abrir"), (822, "Sirven para cortar")):
        option(img, y, l)
    return img


def screen_trap():
    img = sky(33)
    hud(img, 9, 2, "880", comet=0.35, tiempo="0:41")
    # perro (arriba), caballo, hueso (abajo derecha), conejo (tocado), gato
    palabras = ["perro", "caballo", "hueso", "gato", "conejo"]
    states = [None, None, "true", None, "bad"]
    pts = [(x, y - 20) for x, y in PTS]
    lines(img, pts)
    dashed(img, pts[2], pts[0], SUN)
    for (x, y), w, st in zip(pts, palabras, states):
        star(img, x, y, state=st)
    for (x, y), w, st in zip(pts, palabras, states):
        plate(img, x, y + 50, w, state=st)
    d = ImageDraw.Draw(img)
    # rótulo de la línea punteada, sobre un fondo suave para que se lea
    mx, my = 236, 408
    ov(img, lambda dd: dd.rounded_rectangle([(mx - 128) * S, (my - 18) * S, (mx + 128) * S, (my + 18) * S], 16 * S, fill=(5, 8, 35, 215)))
    d = ImageDraw.Draw(img)
    text(d, (mx, my), "va con perro, pero no es un animal", "n", 15, SUN, anchor="mm", weight=700)
    text(d, (MARGIN, 108), "Tocaste «conejo»", "f", 18, CORAL, anchor="lm")
    text(d, (W / 2, 640), "Todos son animales; el hueso no.", "f", 22, WHITE, anchor="mm", stroke=2.5)
    text(d, (W / 2, 676), "Hay cosas que van juntas sin ser del mismo tipo.", "n", 15, SOFT, anchor="mm")
    text(d, (W / 2, 722), "Sin prisa: lee la regla y toca para seguir", "n", 15, SOFT, anchor="mm")
    M.clay_rr(img, (MARGIN, 770, W - MARGIN, 826), 28, SUN, INK, 3.5, 4)
    d = ImageDraw.Draw(img)
    text(d, (W / 2, 798), "Seguir", "f", 21, INK, anchor="mm")
    return img


# ---- Tu cielo: una forma de estrellas propia por categoría
SHAPES = {
    "Frutas": [(0, -26), (25, -8), (15, 22), (-15, 22), (-25, -8)],
    "Animales": [(-30, 14), (-15, -14), (0, 8), (15, -14), (30, 14)],
    "Herramientas": [(-26, -18), (0, -18), (26, -18), (0, 0), (0, 26)],
    "Muebles": [(-24, 24), (-24, -16), (24, -16), (24, 24)],
    "Oficios": [(0, -26), (0, 0), (-24, 10), (24, 10), (0, 26)],
    "Cuerpo": [(-26, 0), (-8, -22), (14, -10), (26, 12), (2, 24)],
    "Instrumentos": [(-28, 22), (-10, -8), (6, 6), (24, -22)],
}


def sky_map(img, x0, y0, w, h):
    cells = [("Frutas", "new"), ("Animales", "new"), ("Herramientas", None), ("Muebles", "review"), ("Oficios", None), ("Instrumentos", None)]
    cw, ch = w / 3, h / 2
    d = ImageDraw.Draw(img)
    for i, (name, st) in enumerate(cells):
        cx = x0 + cw * (i % 3) + cw / 2
        cy = y0 + ch * (i // 3) + ch / 2 - 8
        pts = [(cx + px * 0.95, cy + py * 0.95) for px, py in SHAPES[name]]
        col = {"new": SUN, "review": GRAPE}.get(st, SKY)
        if st == "new":
            glow(img, cx, cy, 44, SUN, 120)
        if st == "review":
            d = ImageDraw.Draw(img)
            d.ellipse([(cx - 34) * S, (cy - 34) * S, (cx + 34) * S, (cy + 34) * S], outline=GRAPE, width=int(3.2 * S))
        ov(img, lambda dd, pts=pts, col=col: [dd.line([(pts[k][0] * S, pts[k][1] * S), (pts[k + 1][0] * S, pts[k + 1][1] * S)], fill=col + (200,), width=int(2.2 * S)) for k in range(len(pts) - 1)])
        for k, (px, py) in enumerate(pts):
            rr = 5 if st != "new" else 6.5
            d = ImageDraw.Draw(img)
            d.ellipse([(px - rr) * S, (py - rr) * S, (px + rr) * S, (py + rr) * S], fill=PLACA if st != "review" else (226, 218, 255), outline=INK, width=int(1.6 * S))
        d = ImageDraw.Draw(img)
        text(d, (cx, cy + 46), name, "f", 14, WHITE, anchor="mm")
        tag = {"new": "nueva", "review": "por repasar"}.get(st)
        if tag:
            text(d, (cx, cy + 62), tag, "n", 14, col, anchor="mm", weight=700)


def bar(img, x0, y, w, frac, col=GRAPE, h=11):
    ov(img, lambda dd: dd.rounded_rectangle([x0 * S, y * S, (x0 + w) * S, (y + h) * S], h * S / 2, fill=(255, 255, 255, 40)))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([x0 * S, y * S, (x0 + max(w * frac, h)) * S, (y + h) * S], h * S / 2, fill=col)


def screen_result():
    img = sky(34)
    d = ImageDraw.Draw(img)
    text(d, (W / 2, 36), "La estrella intrusa", "f", 25, WHITE, anchor="mm", stroke=3)
    d.ellipse([(W / 2 - 62) * S, 58 * S, (W / 2 - 50) * S, 70 * S], fill=M.hexrgb("#10B981"))
    text(d, (W / 2 - 42, 64), "Lenguaje · Nivel 7 · Reto", "n", 15, SOFT, anchor="lm")
    # Tu red de significados
    glow(img, W / 2, 112, 80, LENGUAJE, 70)
    d = ImageDraw.Draw(img)
    text(d, (W / 2 - 6, 112), "17", "f", 56, SUN, anchor="rm", weight=700, stroke=3)
    text(d, (W / 2 + 4, 120), "de 20", "f", 22, WHITE, anchor="lm")
    text(d, (W / 2, 156), "Tu red de significados", "f", 18, WHITE, anchor="mm")
    rows = [("Tipo de cosa", 0.95, "9 de 10", False), ("Uso", 0.85, "4 de 5", False), ("Material / lugar", 0.75, "3 de 4", False), ("Trampas", 0.45, "1 de 4", True)]
    # (la barra más corta es la más baja: se marca con texto)
    y = 184
    for i, (lab, frac, t, low) in enumerate(rows):
        yy = y + i * 28
        text(d, (MARGIN, yy + 6), lab, "f", 15, WHITE, anchor="lm")
        bar(img, 172, yy, 72, frac, CORAL if low else GRAPE)
        d = ImageDraw.Draw(img)
        text(d, (W - MARGIN, yy + 6), t + (" · la más baja" if low else ""), "f", 15, CORAL if low else WHITE, anchor="rm")
    # trampas
    text(d, (W / 2, 322), "Las trampas te engañaron 3 de 8", "f", 18, SUN, anchor="mm")
    text(d, (W / 2, 350), "Es normal: el cerebro une lo que suele ir junto.", "n", 15, WHITE, anchor="mm")
    text(d, (W / 2, 372), "Truco: antes de tocar, pregúntate de qué TIPO es cada una.", "n", 14, SOFT, anchor="mm")
    text(d, (W / 2, 412), "¿Qué las une?  9 de 12", "f", 18, WHITE, anchor="mm")
    # Tu cielo
    text(d, (W / 2, 454), "Tu cielo", "f", 21, SUN, anchor="mm")
    text(d, (W / 2, 478), "14 constelaciones · 2 nuevas hoy · 1 por repasar", "n", 14, SOFT, anchor="mm")
    ov(img, lambda dd: dd.rounded_rectangle([MARGIN * S, 494 * S, (W - MARGIN) * S, 730 * S], 22 * S, fill=(8, 12, 48, 150), outline=(255, 255, 255, 30), width=int(1.5 * S)))
    sky_map(img, MARGIN, 500, W - 2 * MARGIN, 226)
    d = ImageDraw.Draw(img)
    text(d, (W / 2, 748), "Medida de esta partida. No es un diagnóstico.", "n", 14, SOFT, anchor="mm")
    M.clay_rr(img, (MARGIN, 770, W - MARGIN, 826), 28, SUN, INK, 3.5, 4)
    d = ImageDraw.Draw(img)
    text(d, (W / 2, 798), "Siguiente juego (3 de 3)", "f", 20, INK, anchor="mm")
    M.clay_rr(img, (MARGIN, 840, W - MARGIN, 886), 23, WHITE, INK, 3.5, 4)
    d = ImageDraw.Draw(img)
    text(d, (W / 2, 863), "Jugar de nuevo", "f", 18, INK, anchor="mm")
    return img


# ---------------------------------------------------------------- reglas y hoja

def check_rules():
    bad = [(s, sp) for s, sp, word_, _ in M.USED if sp < 14]
    assert not bad, f"texto demasiado chico: {bad}"
    short = [(s, sp) for s, sp in WORDS_USED if sp < WORD_SP]
    assert not short, f"palabra bajo {WORD_SP} sp: {short}"
    bg = M.hexrgb(GRAD[2])
    low = []
    for s, sp, word_, col in M.USED:
        if col == INK:
            continue
        c = M.contrast(col, bg)
        if c < 4.5:
            low.append((s, round(c, 1)))
    assert not low, f"contraste bajo: {low}"
    return (f"palabra en tinta sobre placa crema {M.contrast(INK, PLACA):.1f}:1, tinta sobre sol {M.contrast(INK, SUN):.1f}:1, "
            f"sobre crema (opciones) {M.contrast(INK, PLACA):.1f}:1")


def main():
    shots = [("1 · Así se juega (nivel 2)", "cinco estrellas: una no pertenece", screen_play()),
             ("2 · Constelación lograda", "las cuatro se unen y llega el bonus", screen_done()),
             ("3 · Trampa", "error con la regla y la pareja que engaña", screen_trap()),
             ("4 · Al final y Tu cielo", "medidas y constelaciones ganadas", screen_result())]
    report = check_rules()
    pad, head = 28, 90
    sheet = Image.new("RGB", (len(shots) * (W + pad) + pad, H + head + pad), (236, 234, 244))
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
    sheet.save(OUT)
    print(OUT)
    print("Contrastes:", report)


if __name__ == "__main__":
    main()
