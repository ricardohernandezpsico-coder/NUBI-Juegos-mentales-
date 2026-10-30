# -*- coding: utf-8 -*-
"""Lámina de la Tarea 190d («pulido de la entrada», 29-oct): (a) la hoja de la mañana y el resumen de Correo, «hoy | propuesta», a tamaño real
(1080 de ancho, etapa 10: la peor para el largo del texto); (b) la tabla de la luna y de los primeros ensayos, antes y después.

Las capturas salen de `NUBI_SHOTS_GAMES=correo NUBI_SHOTS_MAIL_STAGE=10 NUBI_SHOTS_DIR=<carpeta>` (ScreenshotRunner, necesita tarjeta de video):
`unity/test-results/capturas-190d/hoy10/` con el código del último commit y `.../propuesta10/` con el de la tarea (fuera de git).
Uso: python tools/previews/tarea_190d.py [salida.png]
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
CAP = os.path.join(ROOT, "unity", "test-results", "capturas-190d")
OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(ROOT, "docs", "previews", "tarea-190d.png")

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


def shot(side, name, box):
    return Image.open(os.path.join(CAP, side, "correo", name + ".png")).convert("RGB").crop(box)


# ---------------------------------------------------------------- las tomas a tamaño real
BRIEF_BOX = (0, 380, 1080, 1950)
RECAP_BOX = (0, 360, 1080, 1560)
bh, rh = BRIEF_BOX[3] - BRIEF_BOX[1], RECAP_BOX[3] - RECAP_BOX[1]
GAP, PAD = 40, 50
colw = 1080
W = PAD * 2 + colw * 2 + GAP

ROWS = [
    ("La luna de la carga: cuadros por segundo", "6 a 7 por segundo (un cuadro cada 150 ms)", "60 por segundo (mediana 8,3 ms, p95 8,4 ms)", "teléfono, T190c: solo se cambió la aceleración"),
    ("La luna: lo que se ve la carga en Tinta o Palabra (Unity vivo)", "1,23 s", "0,55 s", "teléfono, T190c"),
    ("Unity vivo: espera de la luna y su fundido de entrada", "250 ms y 320 ms", "400 ms y 200 ms (en Tinta el «listo» llega a los 0,35 s: la luna ya no destella)", "decisión de la T190d; en frío no cambia"),
    ("Tinta o Palabra: letras reconstruidas tras el «listo»", "2 en el PC; en el teléfono 4, en el cuadro de 83 ms del primer ensayo", "0 en el PC (8 antes del «listo», tapadas por la carga); cuadro visible más largo 17,8 ms", "PC (Editor); en el teléfono se lee tras instalar"),
    ("Engranajes: cuadro más largo del primer ensayo", "36 ms en el PC; 67 ms en el teléfono", "18 a 21 ms en el PC (la primera máquina ya está armada antes del «listo»)", "PC (Editor); en el teléfono se lee tras instalar"),
    ("Engranajes etapa 5: bandas de fábrica (cuadro blanco)", "Belt3_line* se veía 1 cuadro con los valores de fábrica", "ninguna: se dibujan en el mismo cuadro en que la máquina se muestra", "guardia de imágenes de fábrica + prueba nueva"),
    ("Tiempo hasta el «listo» (PC)", "Tinta 0,79 s; Engranajes 1,39 s", "Tinta 0,81 s (30 ms de letras repartidos en la carga); Engranajes 1,50 s", "PC; cambia entre corridas unos 0,1 a 0,3 s"),
]
COLS = [(0, 520), (520, 1000), (1000, 1650), (1650, 2200)]          # x de cada columna de la tabla (dentro de un ancho de 2200)
row_h = 108
table_h = 120 + row_h * len(ROWS) + 30
H = 150 + 70 + bh + 90 + rh + 110 + table_h + 90

im = Image.new("RGB", (W, H), NIGHT)
d = ImageDraw.Draw(im, "RGBA")


def text(xy, s, size=26, fill=INK, bold=False, anchor="la"):
    d.text(xy, s, font=font(size, bold), fill=fill, anchor=anchor)


def wrap(xy, s, size, fill, width, bold=False, gap=4, maxlines=4):
    x, y = xy
    f = font(size, bold)
    line, n = "", 0
    for w in s.split(" "):
        t = (line + " " + w).strip()
        if d.textlength(t, font=f) > width and line:
            d.text((x, y), line, font=f, fill=fill)
            y += size + gap
            n += 1
            line = w
        else:
            line = t
    if line:
        d.text((x, y), line, font=f, fill=fill)
        y += size + gap
    return y


text((PAD, 34), "Tarea 190d · el pulido de la entrada", 44, INK, True)
text((PAD, 92), "Arriba, la hoja de la mañana y el resumen de Correo a tamaño real (etapa 10, pantalla de 1080 × 2400). Abajo, lo medido antes y después.", 24, DIM)

y = 150
text((PAD, y), "La hoja de la mañana", 34, SUN, True)
text((PAD + colw + GAP, y), "", 30, SUN, True)
y += 56
text((PAD + colw // 2, y), "HOY (último commit)", 28, CORAL, True, "ma")
text((PAD + colw + GAP + colw // 2, y), "PROPUESTA", 28, LIME, True, "ma")
y += 40
im.paste(shot("hoy10", "03-hoja", BRIEF_BOX), (PAD, y))
im.paste(shot("propuesta10", "03-hoja", BRIEF_BOX), (PAD + colw + GAP, y))
y += bh + 20
text((PAD, y), "«Si llega una carta con sello dorado,» se salía de la tarjeta; ahora pasa a dos líneas y la fila crece 20 dp. El texto no cambió.", 24, DIM)
y += 60
text((PAD, y), "El resumen del día", 34, SUN, True)
y += 56
im.paste(shot("hoy10", "10-resumen", RECAP_BOX), (PAD, y))
im.paste(shot("propuesta10", "10-resumen", RECAP_BOX), (PAD + colw + GAP, y))
y += rh + 20
text((PAD, y), "«Faro a media mañana · de todos los días» pasaba del borde de la tarjeta; ahora va en dos líneas, partido en el « · » (la fila crece 14 dp).", 24, DIM)
y += 70

# ---------------------------------------------------------------- la tabla
text((PAD, y), "La luna y los primeros ensayos, antes y después", 34, SUN, True)
y += 56
x0 = PAD
heads = ["Qué", "Hoy", "Con la tarea", "Dónde se midió"]
d.rounded_rectangle((x0 - 10, y - 8, W - PAD + 10, y + row_h * len(ROWS) + 58), 18, fill=CARD)
for (a, b), h in zip(COLS, heads):
    text((x0 + a + 10, y + 6), h, 22, DIM, True)
y += 46
d.line((x0, y, W - PAD, y), fill=LINE, width=2)
for r in ROWS:
    for i, ((a, b), s) in enumerate(zip(COLS, r)):
        wrap((x0 + a + 10, y + 10), s, 22, INK if i != 2 else LIME if i == 2 else INK, b - a - 24, bold=(i == 0))
    y += row_h
    d.line((x0, y, W - PAD, y), fill=LINE, width=1)
text((x0 + 10, y + 14), "El teléfono mide con la sonda de entrada (solo depuración). Los números del PC son del Editor sin gráficos: sirven para comparar, no para prometer lo del Motorola.", 20, DIM)

os.makedirs(os.path.dirname(OUT), exist_ok=True)
im.save(OUT, optimize=True)
print(OUT, im.size)
