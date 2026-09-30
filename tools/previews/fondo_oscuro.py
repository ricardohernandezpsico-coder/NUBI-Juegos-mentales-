"""Fondo más oscuro: cuatro variantes de la pantalla Hoy lado a lado (30-sep, pedido de Ricardo: "el fondo un poco más
oscuro"). Réplica PIL de CosmosBackground (degradado + 3 nebulosas + estrellas) con Nubi al centro y las 6 áreas.

  python tools/previews/fondo_oscuro.py  ->  docs/previews/fondo-oscuro.png
"""
import math
import os
import random

from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
RES = os.path.join(ROOT, "app", "src", "main", "res")
FONT = os.path.join(RES, "font", "fredoka.ttf")
NUNITO = os.path.join(RES, "font", "nunito_regular.ttf")
OUT = os.path.join(ROOT, "docs", "previews", "fondo-oscuro.png")

W, H = 420, 900
S = 2  # supermuestreo

VARIANTS = [
    dict(name="A · Actual", sub="#04061C a #101A58",
         grad=["#04061C", "#080E3A", "#101A58"], neb=(0x44, 0x30, 0x3D), nav="light"),
    dict(name="B · Noche profunda", sub="#02030F a #0A0F33 · nebulosas -40%",
         grad=["#02030F", "#050823", "#0A0F33"], neb=(0x28, 0x1C, 0x24), nav="light"),
    dict(name="C · B + barra oscura", sub="barra de pestañas en tinta",
         grad=["#02030F", "#050823", "#0A0F33"], neb=(0x28, 0x1C, 0x24), nav="dark"),
    dict(name="D · Casi negro", sub="#030308 a #0B0C22 · nebulosas -65%",
         grad=["#030308", "#060716", "#0B0C22"], neb=(0x18, 0x10, 0x15), nav="dark"),
]

LAVENDER = (184, 164, 255)
SUN = (255, 201, 60)
INK = (26, 18, 64)
TEXT = (255, 255, 255)
TEXT_SOFT = (214, 208, 245)


def hex_rgb(h):
    h = h.lstrip("#")
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))


def lum(c):
    def ch(v):
        v /= 255
        return v / 12.92 if v <= 0.03928 else ((v + 0.055) / 1.055) ** 2.4
    r, g, b = (ch(x) for x in c)
    return 0.2126 * r + 0.7152 * g + 0.0722 * b


def contrast(a, b):
    la, lb = sorted((lum(a), lum(b)), reverse=True)
    return (la + 0.05) / (lb + 0.05)


def fredoka(size, weight=600):
    f = ImageFont.truetype(FONT, size)
    try:
        f.set_variation_by_axes([weight])
    except OSError:
        pass
    return f


def gradient(stops, w, h):
    cols = [hex_rgb(s) for s in stops]
    img = Image.new("RGB", (w, h))
    px = img.load()
    for y in range(h):
        t = y / (h - 1) * (len(cols) - 1)
        i = min(int(t), len(cols) - 2)
        f = t - i
        c = tuple(int(cols[i][k] + (cols[i + 1][k] - cols[i][k]) * f) for k in range(3))
        for x in range(w):
            px[x, y] = c
    return img


def nebula(base, center, radius, color, alpha):
    layer = Image.new("RGBA", base.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    steps = 40
    for i in range(steps, 0, -1):
        r = radius * i / steps
        a = int(alpha * (1 - i / steps))
        d.ellipse([center[0] - r, center[1] - r, center[0] + r, center[1] + r], fill=color + (a,))
    return Image.alpha_composite(base, layer.filter(ImageFilter.GaussianBlur(radius * 0.08)))


def panel(v):
    w, h = W * S, H * S
    img = gradient(v["grad"], w, h).convert("RGBA")
    a1, a2, a3 = v["neb"]
    img = nebula(img, (w * 0.9, h * 0.10), w * 0.9, (76, 201, 240), a1)
    img = nebula(img, (w * 0.1, h * 0.55), w * 0.8, (184, 164, 255), a2)
    img = nebula(img, (w * 0.05, h * 0.95), w * 0.85, (255, 107, 74), a3)
    d = ImageDraw.Draw(img, "RGBA")
    rnd = random.Random(7)
    for _ in range(70):
        x, y = rnd.random() * w, rnd.random() * h
        r = (0.6 + rnd.random() * 1.6) * S
        warm = rnd.random() < 0.18
        c = (255, 195, 138) if warm else (255, 255, 255)
        d.ellipse([x - r, y - r, x + r, y + r], fill=c + (int(90 + rnd.random() * 140),))

    f_title = fredoka(22 * S, 650)
    f_body = ImageFont.truetype(NUNITO, 19 * S)
    f_bubble = fredoka(20 * S, 700)
    f_small = ImageFont.truetype(NUNITO, 13 * S)

    # Burbuja de Nubi
    bx0, by0, bx1, by1 = 40 * S, 40 * S, (W - 40) * S, 120 * S
    d.rounded_rectangle([bx0, by0 + 4 * S, bx1, by1 + 4 * S], 22 * S, fill=(70, 50, 150, 255))
    d.rounded_rectangle([bx0, by0, bx1, by1], 22 * S, fill=(250, 248, 255, 255), outline=(70, 50, 150, 255), width=3 * S)
    d.text((bx0 + 20 * S, by0 + 12 * S), "Nubi", font=f_small, fill=(90, 80, 140))
    d.text((bx0 + 20 * S, by0 + 36 * S), "4 áreas avanzaron", font=f_bubble, fill=INK)

    # Halo y Nubi
    cx, cy = w // 2, int(h * 0.48)
    halo = Image.new("RGBA", img.size, (0, 0, 0, 0))
    hd = ImageDraw.Draw(halo)
    for i in range(30, 0, -1):
        r = 150 * S * i / 30
        hd.ellipse([cx - r, cy - r, cx + r, cy + r], fill=(150, 130, 255, int(60 * (1 - i / 30))))
    img = Image.alpha_composite(img, halo.filter(ImageFilter.GaussianBlur(10 * S)))
    d = ImageDraw.Draw(img, "RGBA")
    for rr in (118, 132):
        d.ellipse([cx - rr * S, cy - rr * S, cx + rr * S, cy + rr * S], outline=(190, 180, 255, 60), width=S)
    nubi = Image.open(os.path.join(RES, "drawable-nodpi", "nubi_hola.webp")).convert("RGBA")
    ns = 190 * S
    nubi = nubi.resize((ns, int(nubi.height * ns / nubi.width)), Image.LANCZOS)
    img.alpha_composite(nubi, (cx - nubi.width // 2, cy - nubi.height // 2))
    d = ImageDraw.Draw(img, "RGBA")

    # Áreas
    areas = [("Memoria", "Hábil", 0.45, 0.09), ("Lenguaje", "Inicio", 0.08, 0.0),
             ("Atención", "Hábil", 0.42, 0.04), ("Cálculo", "Aprendiz", 0.30, 0.03),
             ("Razonamiento", "Aprendiz", 0.28, 0.0), ("Velocidad", "Experto", 0.62, 0.05)]
    ys = [200, 470, 700]
    for i, (name, stage, val, week) in enumerate(areas):
        right = i % 2 == 1
        y = ys[i // 2] * S
        bw = 130 * S
        x0 = (W - 20) * S - bw if right else 20 * S
        tw = d.textlength(name, font=f_title)
        d.text(((W - 20) * S - tw if right else x0, y), name, font=f_title, fill=TEXT)
        by = y + 38 * S
        track = Image.new("RGBA", img.size, (0, 0, 0, 0))
        ImageDraw.Draw(track).rounded_rectangle([x0, by, x0 + bw, by + 9 * S], 5 * S, fill=(255, 255, 255, 34))
        img.alpha_composite(track)
        d = ImageDraw.Draw(img, "RGBA")
        d.rounded_rectangle([x0, by, x0 + int(bw * val), by + 9 * S], 5 * S, fill=LAVENDER)
        if week:
            d.rounded_rectangle([x0 + int(bw * (val - week)), by, x0 + int(bw * val), by + 9 * S], 5 * S, fill=SUN)
        sw = d.textlength(stage, font=f_body)
        d.text(((W - 20) * S - sw if right else x0, by + 16 * S), stage, font=f_body, fill=TEXT_SOFT)

    # Barra de pestañas
    ny0 = (H - 86) * S
    if v["nav"] == "light":
        bg, fg, act_bg, act_fg = (255, 255, 255, 255), (110, 103, 144), (255, 225, 215, 255), INK
    else:
        bg, fg, act_bg, act_fg = (20, 17, 46, 255), (160, 152, 200), (60, 48, 120, 255), (255, 255, 255)
    d.rounded_rectangle([16 * S, ny0, (W - 16) * S, (H - 16) * S], 30 * S, fill=bg,
                        outline=(58, 46, 120, 255), width=2 * S)
    labels = ["Hoy", "Juegos", "Avance"]
    for i, lab in enumerate(labels):
        cxl = int((16 + (W - 32) * (i + 0.5) / 3) * S)
        if i == 0:
            d.rounded_rectangle([cxl - 44 * S, ny0 + 10 * S, cxl + 44 * S, ny0 + 60 * S], 20 * S, fill=act_bg)
        lw = d.textlength(lab, font=f_small)
        d.text((cxl - lw / 2, ny0 + 38 * S), lab, font=f_small, fill=act_fg if i == 0 else fg)
        d.ellipse([cxl - 9 * S, ny0 + 14 * S, cxl + 9 * S, ny0 + 32 * S], outline=act_fg if i == 0 else fg, width=2 * S)

    img = img.resize((W, H), Image.LANCZOS)
    bg_mid = hex_rgb(v["grad"][1])
    return img, contrast(TEXT_SOFT, bg_mid), contrast(LAVENDER, hex_rgb(v["grad"][2]))


def main():
    pad, head = 24, 92
    sheet = Image.new("RGB", (len(VARIANTS) * (W + pad) + pad, H + head + pad), (236, 234, 244))
    d = ImageDraw.Draw(sheet)
    f_h = fredoka(24, 650)
    f_s = ImageFont.truetype(NUNITO, 15)
    for i, v in enumerate(VARIANTS):
        img, c_text, c_bar = panel(v)
        x = pad + i * (W + pad)
        d.text((x, 14), v["name"], font=f_h, fill=INK)
        d.text((x, 46), v["sub"], font=f_s, fill=(90, 84, 120))
        d.text((x, 66), f"contraste texto secundario {c_text:.1f}:1 · barra {c_bar:.1f}:1", font=f_s, fill=(90, 84, 120))
        sheet.paste(img, (x, head))
    sheet.save(OUT)
    print(OUT)


if __name__ == "__main__":
    main()
