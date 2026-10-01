"""Maqueta de "¿Verdad o disparate?" (1-oct; diseño en docs/diseno-verdad-o-disparate.md): 4 pantallas de teléfono
(412 x 915 dp) sobre el fondo C, en Pillow, con las mismas piezas y reglas que la maqueta de Lluvia de meteoros
(`meteoros.py`: arcilla, borde tinta, sombra dura, textos >= 14 sp, frases >= 24 sp en Atkinson Hyperlegible Bold,
contraste >= 4,5:1, nunca solo color). Es una maqueta, NO la app.

  python tools/art-preview/disparate.py  ->  docs/previews/disparate.png
"""
import os

from PIL import Image, ImageDraw, ImageFont

import meteoros as M
from meteoros import (CORAL, GRAD, GRAPE, INK, LIME, PLACA, SKY, SOFT, SUN, W, H, WHITE, glow, ov, phone, sky, text)

S = M.S
OUT = os.path.join(M.ROOT, "docs", "previews", "disparate.png")
ATKINSON = os.path.join(M.ROOT, "tools", "previews", "fuentes-letra", "AtkinsonHyperlegible-Bold.ttf")
MARGIN = 28
PHRASE_SP = 24
_atk = {}
PHRASES_USED = []


def atkinson(sp):
    if sp not in _atk:
        _atk[sp] = ImageFont.truetype(ATKINSON, int(sp * S))
    return _atk[sp]


def phrase(d, xy, s, fill=INK, anchor="mm", sp=PHRASE_SP):
    PHRASES_USED.append((s, sp))
    d.text((xy[0] * S, xy[1] * S), s, font=atkinson(sp), fill=fill, anchor=anchor)


def phrase_w(s, sp=PHRASE_SP):
    return atkinson(sp).getlength(s) / S


# ---------------------------------------------------------------- piezas

def hud(img, nivel, racha, puntos):
    M.clay_rr(img, (24, 36, 100, 66), 14, (38, 46, 110), INK, 3, 3)
    M.clay_rr(img, (108, 36, 188, 66), 14, (38, 46, 110), INK, 3, 3)
    M.clay_rr(img, (196, 36, 270, 66), 14, (38, 46, 110), INK, 3, 3)
    d = ImageDraw.Draw(img)
    text(d, (62, 51), f"Nivel {nivel}", "f", 15, SKY, anchor="mm")
    text(d, (148, 51), f"Racha {racha}", "f", 15, SUN, anchor="mm")
    text(d, (233, 51), puntos, "f", 15, WHITE, anchor="mm")


def antenna(img, cx, base_y, top_y, lit, burning=False):
    """Antena de arcilla que emite ondas; [lit] de 5 barras de señal encendidas (la racha). [burning] = racha encendida."""
    # ondas
    def waves(d):
        for i, r in enumerate((46, 76, 106)):
            a = (170, 110, 60)[i]
            col = (SUN if burning else SKY) + (a,)
            d.arc([(cx - r) * S, (top_y - r) * S, (cx + r) * S, (top_y + r) * S], 215, 325, fill=col, width=int(5 * S))
    ov(img, waves, blur=0.6)
    if burning:
        glow(img, cx, top_y, 60, SUN, 120)
    d = ImageDraw.Draw(img)
    # base y mástil
    d.rounded_rectangle([(cx - 100) * S, (base_y + 4) * S, (cx + 100) * S, (base_y + 30) * S], 12 * S, fill=INK)
    d.rounded_rectangle([(cx - 100) * S, base_y * S, (cx + 100) * S, (base_y + 26) * S], 12 * S, fill=(38, 46, 110), outline=INK, width=int(3 * S))
    M.clay_rr(img, (cx - 40, base_y - 36, cx + 40, base_y + 2), 12, GRAPE, INK, 3.5, 4)
    M.clay_rr(img, (cx - 8, top_y + 10, cx + 8, base_y - 30), 6, PLACA, INK, 3, 3)
    d = ImageDraw.Draw(img)
    # plato y esfera
    d.ellipse([(cx - 30) * S, (top_y - 20 + 4) * S, (cx + 30) * S, (top_y + 24 + 4) * S], fill=INK)
    d.ellipse([(cx - 30) * S, (top_y - 20) * S, (cx + 30) * S, (top_y + 24) * S], fill=PLACA, outline=INK, width=int(3.5 * S))
    d.ellipse([(cx - 15) * S, (top_y - 8) * S, (cx + 15) * S, (top_y + 14) * S], fill=SKY, outline=INK, width=int(2.5 * S))
    d.ellipse([(cx - 8) * S, (top_y - 34) * S, (cx + 8) * S, (top_y - 18) * S], fill=SUN, outline=INK, width=int(2.5 * S))
    # medidor de señal (5 barras) a la derecha del mástil: la racha
    for i in range(5):
        x0 = cx + 50 + i * 13
        h = 12 + i * 7
        on = i < lit
        d.rounded_rectangle([x0 * S, (base_y - 30 - h) * S, (x0 + 9) * S, (base_y - 30) * S], 3 * S,
                            fill=(SUN if on else (38, 46, 110)), outline=INK, width=int(2.2 * S))


def ribbon(img, x1, y):
    """La cinta de luz de la transmisión: entra desde la derecha y llega a la placa."""
    def band(d):
        for i, a in enumerate((40, 70, 110, 160)):
            hh = 22 - i * 5
            d.rounded_rectangle([x1 * S, (y - hh / 2) * S, (W + 20) * S, (y + hh / 2) * S], hh / 2 * S, fill=SKY + (a,))
    ov(img, band, blur=1.2)


def plate(img, cx, cy, lines, state=None, w=None):
    """Placa crema con la frase (1 o 2 renglones). state: None / 'ok' (borde lima) / 'bad' (borde coral)."""
    ww = w or (max(phrase_w(l) for l in lines) + 56)
    hh = 40 + 30 * len(lines)
    x0, y0, x1, y1 = cx - ww / 2, cy - hh / 2, cx + ww / 2, cy + hh / 2
    if state:
        glow(img, cx, cy, ww * 0.62, LIME if state == "ok" else CORAL, 120)
    M.clay_rr(img, (x0, y0, x1, y1), 24, PLACA, INK, 4, 7)
    if state:
        d = ImageDraw.Draw(img)
        d.rounded_rectangle([x0 * S, y0 * S, x1 * S, y1 * S], 24 * S, outline=LIME if state == "ok" else CORAL, width=int(6 * S))
    d = ImageDraw.Draw(img)
    for i, l in enumerate(lines):
        phrase(d, (cx, y0 + 20 + 30 * i + 15), l)
    return x0, y0, x1, y1


def signal_bar(img, y, frac, col=SKY):
    d = ImageDraw.Draw(img)
    text(d, (MARGIN, y), "señal", "n", 14, SOFT, anchor="lm")
    x0, x1 = 84, W - MARGIN
    ov(img, lambda dd: dd.rounded_rectangle([x0 * S, (y - 5) * S, x1 * S, (y + 5) * S], 5 * S, fill=(255, 255, 255, 40)))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([x0 * S, (y - 5) * S, (x0 + (x1 - x0) * frac) * S, (y + 5) * S], 5 * S, fill=col)


def buttons(img, press=None):
    for (x0, x1, col, label, kind) in ((24, 200, LIME, "VERDAD", "ok"), (212, 388, CORAL, "DISPARATE", "bad")):
        M.clay_rr(img, (x0, 700, x1, 792), 28, col, INK, 4, 6)
        d = ImageDraw.Draw(img)
        cx = (x0 + x1) / 2
        if kind == "ok":
            d.line([(cx - 16) * S, 733 * S, (cx - 5) * S, 745 * S, (cx + 17) * S, 719 * S], fill=INK, width=int(6 * S), joint="curve")
        else:
            pts = [(cx - 22, 730), (cx - 10, 720), (cx - 1, 738), (cx + 6, 724), (cx + 14, 740), (cx + 22, 727)]
            d.line([(x * S, y * S) for x, y in pts], fill=INK, width=int(5.5 * S), joint="curve")
        text(d, (cx, 770), label, "f", 20, INK, anchor="mm")


def prompt(img, y=600, line="¿Es verdad o es un disparate?", line2="Responde rápido."):
    d = ImageDraw.Draw(img)
    text(d, (W / 2, y), line, "f", 19, WHITE, anchor="mm", stroke=2.5)
    if line2:
        text(d, (W / 2, y + 28), line2, "n", 16, SOFT, anchor="mm")


def label(img, y, s, col=SOFT):
    d = ImageDraw.Draw(img)
    text(d, (W / 2, y), s, "f", 17, col, anchor="mm")


# ---------------------------------------------------------------- las 4 pantallas

def screen_play():
    img = sky(11)
    hud(img, 2, 3, "60")
    antenna(img, W / 2 - 24, 330, 190, lit=2)
    ribbon(img, W / 2 + phrase_w("Los peces nadan") / 2 + 20, 440)
    plate(img, W / 2, 440, ["Los peces nadan"])
    signal_bar(img, 508, 0.7)
    prompt(img)
    buttons(img)
    return img


def screen_hard():
    img = sky(12)
    hud(img, 9, 12, "340")
    M.clay_rr(img, (282, 36, 346, 66), 14, SUN, INK, 3, 3)
    d = ImageDraw.Draw(img)
    text(d, (314, 51), "× 1,5", "f", 16, INK, anchor="mm")
    antenna(img, W / 2 - 24, 330, 190, lit=4, burning=True)
    ribbon(img, W / 2 + phrase_w("Todos los peces que viven") / 2 + 20, 440)
    plate(img, W / 2, 440, ["Todos los peces que viven", "en el mar tienen plumas"])
    signal_bar(img, 520, 0.45, SUN)
    prompt(img)
    buttons(img)
    return img


def screen_feedback():
    img = sky(13)
    hud(img, 5, 4, "120")
    # acierto
    d = ImageDraw.Draw(img)
    text(d, (MARGIN, 108), "Acierto", "f", 19, LIME, anchor="lm")
    plate(img, W / 2 - 16, 190, ["Los peces nadan"], state="ok")
    M.mark(img, W / 2 + 170, 152, True, r=20)
    d = ImageDraw.Draw(img)
    text(d, (W / 2, 250), "¡Bien! La antena suma una barra de señal", "n", 15, WHITE, anchor="mm")
    for i in range(5):
        x0 = W / 2 - 52 + i * 22
        h = 16 + i * 8
        d.rounded_rectangle([x0 * S, (326 - h) * S, (x0 + 14) * S, 326 * S], 4 * S, fill=SUN if i < 3 else (38, 46, 110), outline=INK, width=int(2.5 * S))
    # error
    text(d, (MARGIN, 372), "Error", "f", 19, CORAL, anchor="lm")
    plate(img, W / 2 - 16, 470, ["Los pingüinos vuelan"], state="bad")
    M.mark(img, W / 2 + 170, 430, False, r=20)
    d = ImageDraw.Draw(img)
    text(d, (W / 2, 548), "Por qué:", "n", 15, SOFT, anchor="mm")
    phrase(d, (W / 2, 584), "Los pingüinos no vuelan", fill=WHITE)
    # señal perdida
    text(d, (MARGIN, 668), "Si se acaba la señal", "f", 19, SKY, anchor="lm")
    signal_bar(img, 706, 0.0)
    text(d, (W / 2, 750), "Se perdió la señal", "f", 20, WHITE, anchor="mm", stroke=2.5)
    text(d, (W / 2, 780), "Sin castigo extra: sigue la próxima frase", "n", 15, SOFT, anchor="mm")
    return img


def bar(img, x0, y, w, frac, col=GRAPE, h=11):
    ov(img, lambda dd: dd.rounded_rectangle([x0 * S, y * S, (x0 + w) * S, (y + h) * S], h * S / 2, fill=(255, 255, 255, 40)))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([x0 * S, y * S, (x0 + max(w * frac, h)) * S, (y + h) * S], h * S / 2, fill=col)


def screen_result():
    img = sky(14)
    d = ImageDraw.Draw(img)
    text(d, (W / 2, 40), "¿Verdad o disparate?", "f", 26, WHITE, anchor="mm", stroke=3)
    d.ellipse([(W / 2 - 62) * S, 62 * S, (W / 2 - 50) * S, 74 * S], fill=M.hexrgb("#10B981"))
    text(d, (W / 2 - 42, 68), "Lenguaje · Nivel 8 · Reto", "n", 15, SOFT, anchor="lm")
    glow(img, W / 2, 118, 70, SUN, 90)
    d = ImageDraw.Draw(img)
    text(d, (W / 2 - 14, 118), "86", "f", 60, SUN, anchor="rm", weight=700, stroke=3)
    text(d, (W / 2 - 6, 130), "puntos", "n", 15, SOFT, anchor="lm")
    text(d, (W / 2, 168), "¡Muy bien!", "f", 24, WHITE, anchor="mm")

    # Tu lectura con comprensión
    text(d, (W / 2, 210), "Tu lectura con comprensión", "f", 19, SUN, anchor="mm")
    text(d, (W / 2, 258), "142", "f", 48, GRAPE, anchor="mm", weight=700, stroke=2.5)
    text(d, (W / 2, 296), "palabras por minuto", "f", 17, WHITE, anchor="mm")
    text(d, (W / 2, 318), "leyendo y decidiendo, en las frases que acertaste", "n", 14, SOFT, anchor="mm")

    # Qué te frena
    text(d, (W / 2, 358), "Qué te frena", "f", 19, SUN, anchor="mm")
    rows = [("Frases cortas", 0.9, "0,9 s", False), ("Con «que»", 1.6, "1,6 s", False), ("Todos / algunos", 1.4, "1,4 s", False),
            ("Comparaciones", 1.3, "1,3 s", False), ("Negaciones", 1.9, "1,9 s · la más lenta", True)]
    top = 388
    for i, (lab, secs, t, slow) in enumerate(rows):
        y = top + i * 30
        text(d, (MARGIN, y + 6), lab, "f", 15, WHITE, anchor="lm")
        bar(img, 152, y, 110, secs / 2.0, CORAL if slow else GRAPE)
        d = ImageDraw.Draw(img)
        text(d, (W - MARGIN, y + 6), t, "f", 15, CORAL if slow else WHITE, anchor="rm")
    text(d, (MARGIN, 552), "Las negaciones te toman 1 s más que las frases", "n", 14, WHITE, anchor="lm")
    text(d, (MARGIN, 572), "simples. Es normal. Truco: lee la frase sin el «no»", "n", 14, WHITE, anchor="lm")
    text(d, (MARGIN, 592), "y después dala vuelta.", "n", 14, WHITE, anchor="lm")

    # Tu precisión
    text(d, (W / 2, 636), "Tu precisión", "f", 19, SUN, anchor="mm")
    text(d, (W / 2, 664), "27 de 30 · disparates sutiles 6 de 8", "f", 17, WHITE, anchor="mm")
    # Tu mejor racha
    text(d, (MARGIN, 706), "Tu mejor racha", "f", 19, SUN, anchor="lm")
    text(d, (W - MARGIN, 706), "14 seguidas", "f", 19, LIME, anchor="rm")
    text(d, (W / 2, 746), "Medida de esta partida. No es un diagnóstico.", "n", 14, SOFT, anchor="mm")
    M.clay_rr(img, (MARGIN, 770, W - MARGIN, 826), 28, SUN, INK, 3.5, 4)
    d = ImageDraw.Draw(img)
    text(d, (W / 2, 798), "Siguiente juego (3 de 3)", "f", 20, INK, anchor="mm")
    M.clay_rr(img, (MARGIN, 840, W - MARGIN, 886), 23, WHITE, INK, 3.5, 4)
    d = ImageDraw.Draw(img)
    text(d, (W / 2, 863), "Jugar de nuevo", "f", 18, INK, anchor="mm")
    return img


# ---------------------------------------------------------------- reglas y hoja

def check_rules():
    bad = [(s, sp) for s, sp, word, _ in M.USED if sp < 14]
    assert not bad, f"texto demasiado chico: {bad}"
    short = [(s, sp) for s, sp in PHRASES_USED if sp < 24]
    assert not short, f"frase bajo 24 sp: {short}"
    bg = M.hexrgb(GRAD[2])
    low = []
    for s, sp, word, col in M.USED:
        if col == INK:
            continue
        c = M.contrast(col, bg)
        if c < 4.5:
            low.append((s, round(c, 1)))
    assert not low, f"contraste bajo: {low}"
    return (f"frase en tinta sobre placa crema {M.contrast(INK, PLACA):.1f}:1, tinta sobre lima {M.contrast(INK, LIME):.1f}:1, "
            f"sobre coral {M.contrast(INK, CORAL):.1f}:1, sobre sol {M.contrast(INK, SUN):.1f}:1")


def main():
    shots = [("1 · Así se juega (nivel 2)", "frase corta: ¿verdad o disparate?", screen_play()),
             ("2 · Más difícil (nivel 9)", "con cuantificador, 2 renglones, racha encendida", screen_hard()),
             ("3 · Acierto y error", "forma y texto, no solo color; la corrección", screen_feedback()),
             ("4 · Al final", "las medidas de la partida", screen_result())]
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
