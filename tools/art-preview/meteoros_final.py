"""Maqueta de la pantalla FINAL de Lluvia de meteoros (1-oct): Ricardo la encontró confusa ("muchas estrellas, no se
entiende"). Tres propuestas para la sección "Tu vocabulario" (A tres grupos con barra, B una escalera, C una cifra grande),
con los mismos datos de ejemplo (bandas 1-6: 100, 96, 90, 78, 55 y 30%) y el resto de la pantalla más corto y limpio:
"Tu reconocimiento", "Tu filtro" y "Tu colección" se leen de un vistazo. Pillow; maqueta, NO la app.

  python tools/art-preview/meteoros_final.py  ->  docs/previews/meteoros-final.png
"""
import os

from PIL import Image, ImageDraw, ImageFont

import meteoros as M
from meteoros import (CORAL, GRAPE, INK, LIME, SKY, SOFT, SUN, W, H, WHITE, ov, phone, sky, text, tw)

OUT = os.path.join(M.ROOT, "docs", "previews", "meteoros-final.png")
S = M.S
MARGIN = 28
BAND_PCT = [100, 96, 90, 78, 55, 30]
LAV = GRAPE


def header(img, d):
    text(d, (W / 2, 40), "Lluvia de meteoros", "f", 26, WHITE, anchor="mm")
    d.ellipse([(W / 2 - 62) * S, 62 * S, (W / 2 - 50) * S, 74 * S], fill=M.hexrgb("#10B981"))
    text(d, (W / 2 - 42, 68), "Lenguaje · Nivel 6 · Reto", "n", 15, SOFT, anchor="lm")
    M.glow(img, W / 2, 118, 70, SUN, 90)
    d = ImageDraw.Draw(img)
    text(d, (W / 2 - 14, 118), "78", "f", 60, SUN, anchor="rm", weight=700, stroke=3)
    text(d, (W / 2 - 6, 130), "puntos", "n", 15, SOFT, anchor="lm")
    text(d, (W / 2, 168), "¡Muy bien!", "f", 24, WHITE, anchor="mm")
    return d


def bar(img, x0, y, w, frac, h=11, col=LAV):
    ov(img, lambda dd: dd.rounded_rectangle([x0 * S, y * S, (x0 + w) * S, (y + h) * S], h * S / 2, fill=(255, 255, 255, 40)))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([x0 * S, y * S, (x0 + max(w * frac, h)) * S, (y + h) * S], h * S / 2, fill=col)


# ---------------------------------------------------------------- las tres versiones de "Tu vocabulario"


def vocab_a(img, d):
    text(d, (MARGIN, 204), "Tu vocabulario", "f", 19, SUN, anchor="lm")
    rows = [("Comunes", 98), ("Intermedias", 84), ("Raras", 43)]
    for i, (lab, p) in enumerate(rows):
        y = 238 + i * 38
        text(d, (MARGIN, y + 6), lab, "f", 17, WHITE, anchor="lm")
        bar(img, 150, y, 168, p / 100)
        d = ImageDraw.Draw(img)
        text(d, (W - MARGIN, y + 6), f"{p}%", "f", 18, WHITE, anchor="rm")
    text(d, (MARGIN, 366), "Reconoces casi todas hasta las intermedias;", "n", 15, WHITE, anchor="lm")
    text(d, (MARGIN, 386), "las raras, la mitad.", "n", 15, WHITE, anchor="lm")
    return d


def vocab_b(img, d):
    text(d, (MARGIN, 204), "Tu vocabulario", "f", 19, SUN, anchor="lm")
    x0, x1, y = MARGIN, W - MARGIN, 262
    seg = (x1 - x0 - 5 * 3) / 6
    for i, p in enumerate(BAND_PCT):
        xs = x0 + i * (seg + 3)
        on = p >= 80
        if on:
            d.rounded_rectangle([xs * S, y * S, (xs + seg) * S, (y + 24) * S], 8 * S, fill=LAV)
        else:
            ov(img, lambda dd, xs=xs: dd.rounded_rectangle([xs * S, y * S, (xs + seg) * S, (y + 24) * S], 8 * S, fill=(255, 255, 255, 30),
                                                           outline=(190, 184, 230, 200), width=int(1.5 * S)))
        d = ImageDraw.Draw(img)
        text(d, (xs + seg / 2, y + 40), f"{p}%", "f" if on else "n", 14, WHITE if on else SOFT, anchor="mm")
    # "llegas hasta aquí": una marca en el borde entre la banda 3 y la 4 (la última que reconoces casi toda)
    mx = x0 + 3 * (seg + 3) - 1.5
    d.polygon([((mx - 8) * S, 238 * S), ((mx + 8) * S, 238 * S), (mx * S, 252 * S)], fill=SUN)
    text(d, (mx, 226), "llegas hasta aquí", "f", 15, SUN, anchor="mm")
    text(d, (x0, 330), "común", "n", 14, SOFT, anchor="lm")
    text(d, (x1, 330), "rara", "n", 14, SOFT, anchor="rm")
    text(d, (MARGIN, 362), "Reconoces casi todas hasta las poco", "n", 15, WHITE, anchor="lm")
    text(d, (MARGIN, 382), "frecuentes.", "n", 15, WHITE, anchor="lm")
    return d


def vocab_c(img, d):
    text(d, (MARGIN, 204), "Tu vocabulario", "f", 19, SUN, anchor="lm")
    text(d, (MARGIN, 258), "8 de cada 10", "f", 54, LAV, anchor="lm", weight=700, stroke=2.5)
    text(d, (MARGIN, 300), "palabras poco frecuentes reconocidas", "f", 16, WHITE, anchor="lm")
    bar(img, MARGIN, 322, W - 2 * MARGIN, 0.84, h=12)
    d = ImageDraw.Draw(img)
    text(d, (MARGIN, 358), "Las raras, 4 de cada 10. Las comunes, casi todas.", "n", 15, WHITE, anchor="lm")
    return d


# ---------------------------------------------------------------- el resto, más corto


def rest(img, d):
    # Tu reconocimiento
    text(d, (MARGIN, 420), "Tu reconocimiento", "f", 19, SUN, anchor="lm")
    text(d, (MARGIN, 448), "Las comunes, en 0,7 s. Las raras, en 1,1 s.", "f", 16, WHITE, anchor="lm")
    text(d, (MARGIN, 470), "Es normal: las raras tardan más.", "n", 14, SOFT, anchor="lm")
    # Tu filtro
    text(d, (MARGIN, 516), "Tu filtro", "f", 19, SUN, anchor="lm")
    text(d, (MARGIN, 544), "Te engañaron 5 de 21 palabras inventadas:", "n", 15, WHITE, anchor="lm")
    for k, (lab, n, tot, hot) in enumerate((("con letras cambiadas de lugar", 4, 7, True), ("con una letra cambiada", 1, 8, False),
                                             ("obvias", 0, 6, False))):
        y = 572 + k * 26
        text(d, (MARGIN + 10, y), lab, "n", 15, WHITE, anchor="lm")
        text(d, (W - MARGIN, y), f"{n} de {tot}", "f", 16, CORAL if hot else WHITE, anchor="rm")
    text(d, (MARGIN, 660), "Las letras cambiadas de lugar engañan a casi todos:", "n", 14, SOFT, anchor="lm")
    text(d, (MARGIN, 679), "mira el centro de la palabra.", "n", 14, SOFT, anchor="lm")
    # Tu colección
    text(d, (MARGIN, 722), "Tu colección", "f", 19, SUN, anchor="lm")
    text(d, (W - MARGIN, 722), "+5 palabras raras", "f", 19, LIME, anchor="rm")
    text(d, (MARGIN, 750), "efímero · umbral · inefable · cántaro · alba", "n", 15, WHITE, anchor="lm")
    text(d, (W / 2, 800), "Medida de esta partida. No es un diagnóstico.", "n", 14, SOFT, anchor="mm")
    M.clay_rr(img, (MARGIN, 826, W - MARGIN, 880), 27, SUN, INK, 3.5, 4)
    d = ImageDraw.Draw(img)
    text(d, (W / 2, 853), "Siguiente juego (3 de 3)", "f", 20, INK, anchor="mm")
    return d


def screen(vocab):
    img = sky(14)
    d = header(img, ImageDraw.Draw(img))
    d = vocab(img, d)
    rest(img, d)
    return img


def main():
    shots = [("A · Tres grupos con barra", "Comunes, intermedias y raras", screen(vocab_a)),
             ("B · Escalera", "hasta dónde reconoces casi todas", screen(vocab_b)),
             ("C · Una cifra grande", "8 de cada 10 poco frecuentes", screen(vocab_c))]
    M.check_rules()
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


if __name__ == "__main__":
    main()
