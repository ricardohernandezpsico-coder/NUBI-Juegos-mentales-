# -*- coding: utf-8 -*-
"""Lámina de la Tarea 191 («lo que se ve antes y al empezar a jugar», 30-oct): (a) la tabla de la primera partida de cada juego con tarjetas «NUEVO» (alguien nuevo que no se equivoca y alguien que falla el 3.er ensayo);
(b) la página del Primer vuelo «hoy | propuesta» (H8: ya no repite la línea de cómo se juega que dice el tutorial); (c) la hoja de la mañana de Correo «hoy | propuesta» (espacios duros en «con sello dorado,»).

La tabla sale de `FirstGameTableTests` (EditMode: cada fila es lo que esa prueba imprime y vigila). Las capturas: el Primer vuelo con `FirstFlightScreenshotTest.antes_de_un_juego` (Roborazzi) y Correo con
`NUBI_SHOTS_GAMES=correo NUBI_SHOTS_MAIL_STAGE=10 NUBI_SHOTS_DIR=<carpeta>` (ScreenshotRunner), todas en `unity/test-results/capturas-191/` (fuera de git).
Uso: python tools/previews/tarea_191.py [salida.png]
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
CAP = os.path.join(ROOT, "unity", "test-results", "capturas-191")
OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(ROOT, "docs", "previews", "tarea-191.png")

NIGHT = (14, 13, 51)
INK = (241, 238, 255)
DIM = (176, 170, 214)
SUN = (255, 207, 64)
CORAL = (255, 107, 74)
LIME = (160, 224, 80)
CARD = (28, 27, 82)
LINE = (70, 68, 130)


def font(size, bold=False):
    for p in (r"C:\Windows\Fonts\segoeuib.ttf" if bold else r"C:\Windows\Fonts\segoeui.ttf", r"C:\Windows\Fonts\arialbd.ttf" if bold else r"C:\Windows\Fonts\arial.ttf"):
        try:
            return ImageFont.truetype(p, size)
        except OSError:
            pass
    return ImageFont.load_default()


def shot(path, box):
    return Image.open(os.path.join(CAP, path)).convert("RGB").crop(box)


# ---------------------------------------------------------------- la tabla de la primera partida
HEAD = ["Juego", "Parte en", "Alguien nuevo que no se equivoca", "Alguien que falla el 3.er ensayo"]
ROWS = [
    ("Constelaciones", "etapa 1\n(antes la 8)",
     "Una etapa por ensayo: llega a la 9 sembrada en el ensayo 9.\n1 tarjeta, «Gemelos», en el ensayo 7 (la de «Cielo» ya la dejó vista el tutorial).",
     "Cae a la etapa 1 en el ensayo 4 y se queda en la 2.\nNinguna tarjeta."),
    ("Bodega de carga", "etapa 1\n(antes la 5)",
     "Llega a la 6 sembrada en el pedido 6.\n1 tarjeta, «Mueve», en el pedido 5 («Gira», en el 11 si sigue sin errores).",
     "Cae a la etapa 1 en el pedido 4 y se queda en la 2.\nNinguna tarjeta."),
    ("Engranajes", "etapa 1\n(antes la 5)",
     "Llega a la 6 sembrada en la máquina 6.\n4 tarjetas, de a una y antes de su máquina: Correas (2), Ramas (3), Barras (5) y Bien (6).",
     "Cae a la etapa 1 en la máquina 4 y sube de a poco.\n2 tarjetas: Correas (máquina 2) y Ramas (3)."),
    ("Satélites", "nivel 1\n(antes el 5)",
     "Llega al 6 sembrado en la ronda 6.\nSin tarjetas (sus sorpresas son avisos, no tarjetas).",
     "Cae al nivel 1 en la ronda 4 y sube de a poco.\nSin tarjetas."),
    ("Correo Estelar", "etapa 1\n(antes la 4)",
     "Cuenta por DÍAS: con días perfectos sube una etapa por día (1, 2, 3, 4) y no llega a la 5 sembrada en los 4 días.\n2 tarjetas: «Rutina» (día 3) y «Cancela» (día 4); «Estación» y «Hora» ya las dejó vistas el tutorial.",
     "Un error en un encargo del día 1: el día 2 repite la etapa 1.\nNinguna tarjeta."),
]
COLS = [(0, 330), (330, 600), (600, 1480), (1480, 2140)]
ROW_H = 128
PAD = 50

# ---------------------------------------------------------------- las tomas a tamaño real
FF_BOX = (0, 0, 1078, 1130)
CORREO_BOX = (0, 380, 1080, 1560)
fh, ch = FF_BOX[3] - FF_BOX[1], CORREO_BOX[3] - CORREO_BOX[1]
colw, GAP = 1080, 40
W = PAD * 2 + colw * 2 + GAP
table_h = 120 + ROW_H * len(ROWS) + 90
H = 140 + table_h + 90 + 60 + fh + 100 + 60 + ch + 120

im = Image.new("RGB", (W, H), NIGHT)
d = ImageDraw.Draw(im, "RGBA")


def text(xy, s, size=26, fill=INK, bold=False, anchor="la"):
    d.text(xy, s, font=font(size, bold), fill=fill, anchor=anchor)


def wrap(xy, s, size, fill, width, bold=False, gap=4):
    x, y = xy
    f = font(size, bold)
    for para in s.split("\n"):
        line = ""
        for w in para.split(" "):
            t = (line + " " + w).strip()
            if d.textlength(t, font=f) > width and line:
                d.text((x, y), line, font=f, fill=fill)
                y += size + gap
                line = w
            else:
                line = t
        if line:
            d.text((x, y), line, font=f, fill=fill)
            y += size + gap
    return y


text((PAD, 34), "Tarea 191 · lo que se ve antes y al empezar a jugar", 44, INK, True)
text((PAD, 92), "La primera partida parte en la etapa 1 y sube un paso por ensayo bien hecho hasta el nivel sembrado (0,45); las tarjetas salen de a una, antes del «¡Ya!».", 24, DIM)

y = 140
card_bottom = y + 46 + ROW_H * len(ROWS) + 56
d.rounded_rectangle((PAD - 10, y - 8, W - PAD + 10, card_bottom), 18, fill=CARD)
for (a, b), h in zip(COLS, HEAD):
    text((PAD + a + 10, y + 6), h, 22, DIM, True)
y += 46
d.line((PAD, y, W - PAD, y), fill=LINE, width=2)
for r in ROWS:
    for i, ((a, b), s) in enumerate(zip(COLS, r)):
        wrap((PAD + a + 10, y + 10), s, 21, SUN if i == 0 else INK if i != 1 else LIME, b - a - 24, bold=(i == 0))
    y += ROW_H
    d.line((PAD, y, W - PAD, y), fill=LINE, width=1)
text((PAD + 10, y + 12), "Un «ensayo» es lo que cuenta el motor de dificultad de cada juego (en Constelaciones, una oportunidad de memoria). Si no se equivoca, el tutorial deja vistas las tarjetas que repite (Cielo, Bodega, Estación, Hora, Taller, Ahora: PALABRA).", 19, DIM)
y = card_bottom + 50

text((PAD, y), "La página del Primer vuelo (H8)", 34, SUN, True)
y += 56
text((PAD + colw // 2, y), "HOY", 28, CORAL, True, "ma")
text((PAD + colw + GAP + colw // 2, y), "PROPUESTA", 28, LIME, True, "ma")
y += 40
im.paste(shot("primer-vuelo/hoy.png", FF_BOX), (PAD, y))
im.paste(shot("primer-vuelo/propuesta.png", FF_BOX), (PAD + colw + GAP, y))
y += fh + 20
text((PAD, y), "La línea «Mira qué luces se encienden y tócalas en el mismo orden.» ya la dice el tutorial dentro del juego: queda el título y «Empieza con una ronda de práctica que no cuenta. Dura alrededor de un minuto.».", 22, DIM)
y += 80

text((PAD, y), "La hoja de la mañana de Correo", 34, SUN, True)
y += 56
text((PAD + colw // 2, y), "HOY", 28, CORAL, True, "ma")
text((PAD + colw + GAP + colw // 2, y), "PROPUESTA", 28, LIME, True, "ma")
y += 40
im.paste(shot("correo/hoy.png", CORREO_BOX), (PAD, y))
im.paste(shot("correo/propuesta.png", CORREO_BOX), (PAD + colw + GAP, y))
y += ch + 20
text((PAD, y), "«con sello dorado,» va con espacios duros: si no cabe, la línea corta «Si llega una carta / con sello dorado,» y no deja «dorado,» solo. El texto que se lee es el mismo.", 22, DIM)

os.makedirs(os.path.dirname(OUT), exist_ok=True)
im.save(OUT, optimize=True)
print(OUT, im.size)
