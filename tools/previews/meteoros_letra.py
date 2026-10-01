"""Maqueta de la LETRA de las palabras de los meteoros (30-sep, tras la prueba de Ricardo): en Fredoka la "a" de un solo
piso se confunde con la "o". La placa crema de un meteoro con las MISMAS palabras en cuatro fuentes, a tamaño real del juego
(26 dp; 30 dp en mayores; el juego dibuja 3 unidades por dp, aquí 3 px por dp).

  python tools/previews/meteoros_letra.py  ->  docs/previews/meteoros-letra.png

Fuentes: Fredoka (la actual del juego: Bold 700), Nunito (la de la app solo trae Regular y Medium: se usa Medium),
Atkinson Hyperlegible Bold y Lexend SemiBold (ambas OFL, de Google Fonts, descargadas a tools/previews/fuentes-letra/,
fuera de git).
"""
import os

from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
RES = os.path.join(ROOT, "app", "src", "main", "res", "font")
EXTRA = os.path.join(ROOT, "tools", "previews", "fuentes-letra")
OUT = os.path.join(ROOT, "docs", "previews", "meteoros-letra.png")

PX_PER_DP = 3
INK = (26, 18, 64)
PLACA = (255, 248, 236)
SOFT = (214, 208, 245)
SUN = (255, 201, 60)
WHITE = (255, 255, 255)
GRAD = [(2, 3, 15), (5, 8, 35), (10, 15, 51)]


def variable(path, weight):
    def make(px):
        f = ImageFont.truetype(path, px)
        f.set_variation_by_axes([weight])
        return f
    return make


def static(path):
    return lambda px: ImageFont.truetype(path, px)


FONTS = [
    ("Fredoka · la actual", variable(os.path.join(RES, "fredoka.ttf"), 700)),
    ("Nunito Medium", static(os.path.join(RES, "nunito_medium.ttf"))),
    ("Atkinson Hyperlegible Bold", static(os.path.join(EXTRA, "AtkinsonHyperlegible-Bold.ttf"))),
    ("Lexend SemiBold", variable(os.path.join(EXTRA, "Lexend.ttf"), 600)),
]

# palabras que muestran el problema: a/o, i/l y las tildes
PLAQUES = [
    (26, ["casa", "cosa", "pala", "palo"]),
    (26, ["ala", "ola", "amargo", "ilusión"]),
    (26, ["cálculo", "murciélago", "chocloate"]),
    (30, ["casa", "cosa", "murciélago"]),
]

NOTES = [
    ["a / o: se confunden (la a es de un solo piso, casi una o con palito).",
     "i / l: bien (i con punto, l lisa).   Tildes: se ven, pero el trazo grueso las llena."],
    ["a / o: se distinguen (a de dos pisos).   i / l: bien.   Tildes: nítidas.",
     "El trazo es más fino: menos peso sobre la placa que las otras tres."],
    ["a / o: se distinguen muy bien (a de dos pisos).   i / l: la mejor (la l lleva una colita).",
     "Tildes: nítidas y grandes. Hecha para baja visión. RECOMENDADA."],
    ["a / o: se parecen (a de un solo piso, como Fredoka).   i / l: bien.   Tildes: nítidas.",
     "Letra ancha: las palabras largas ocupan más (murciélago)."],
]

W, MARGIN = 2600, 40
BLOCK_H = 620


def sky(w, h):
    img = Image.new("RGB", (1, h))
    px = img.load()
    for y in range(h):
        t = y / (h - 1) * 2
        i = min(int(t), 1)
        f = t - i
        px[0, y] = tuple(int(GRAD[i][k] + (GRAD[i + 1][k] - GRAD[i][k]) * f) for k in range(3))
    return img.resize((w, h))


def plaque(d, img, x, y, font, words, px):
    """Placa crema con borde tinta y sombra dura; devuelve su ancho."""
    sep = px // 2
    ws = [d.textlength(w, font=font) for w in words]
    inner = sum(ws) + sep * (len(words) - 1)
    pad_x, pad_y = 22 * PX_PER_DP // 3 * 2, 10 * PX_PER_DP
    w_box = inner + pad_x * 2
    h_box = px * 1.5
    d.rounded_rectangle([x, y + 8, x + w_box, y + h_box + 8], 18, fill=INK)
    d.rounded_rectangle([x, y, x + w_box, y + h_box], 18, fill=PLACA, outline=INK, width=6)
    cx = x + pad_x
    for w, wd in zip(words, ws):
        d.text((cx, y + h_box / 2), w, font=font, fill=INK, anchor="lm")
        cx += wd + sep
        if w is not words[-1]:
            d.ellipse([cx - sep / 2 - 4, y + h_box / 2 - 4, cx - sep / 2 + 4, y + h_box / 2 + 4], fill=(190, 184, 214))
    return w_box


def main():
    H = len(FONTS) * BLOCK_H + MARGIN
    img = sky(W, H)
    d = ImageDraw.Draw(img)
    title = ImageFont.truetype(os.path.join(RES, "fredoka.ttf"), 40)
    title.set_variation_by_axes([650])
    small = ImageFont.truetype(os.path.join(RES, "nunito_regular.ttf"), 26)
    note = ImageFont.truetype(os.path.join(RES, "nunito_medium.ttf"), 30)
    for i, (name, make) in enumerate(FONTS):
        y0 = MARGIN + i * BLOCK_H
        d.text((MARGIN, y0), name, font=title, fill=SUN)
        y = y0 + 70
        x = MARGIN
        for size_dp, words in PLAQUES[:2]:
            px = size_dp * PX_PER_DP
            x += plaque(d, img, x, y, make(px), words, px) + 40
        y += 150
        x = MARGIN
        for size_dp, words in PLAQUES[2:]:
            px = size_dp * PX_PER_DP
            x += plaque(d, img, x, y, make(px), words, px) + 40
        d.text((MARGIN, y + 150), "26 dp arriba y a la izquierda · 30 dp (mayores) abajo a la derecha", font=small, fill=SOFT)
        for k, line in enumerate(NOTES[i]):
            d.text((MARGIN, y + 190 + k * 34), line, font=note, fill=WHITE)
    img.save(OUT)
    print(OUT, img.size)


if __name__ == "__main__":
    main()
