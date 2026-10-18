"""Hoja de registro del fondo «Espacio profundo» (Tarea 122; 17-oct): «hoy | A» en capturas reales de Hoy, la lámina de un área, Juegos, Avance y el Primer vuelo, un recorte del texto al 100 % y las medidas
(mediana real de cada render, contraste de los textos claros contra arriba, medio y pie). Es para la revisión de Opus y Fable, no bloquea nada.

  - «hoy»: docs/previews/app-actual/*.png (la app de antes del cambio) y docs/previews/fondo-espacio/hoy-fondo_*.webp (el fondo B de antes).
  - «A»: docs/previews/fondo-espacio/A-*.png (la misma prueba, `AppActualScreenshotTest`, con el fondo nuevo) y A-fondo_*.webp (los que entran a la app).

  python tools/previews/fondo_espacio.py   ->  docs/previews/fondo-espacio.png
"""
import os

import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
HOY = os.path.join(ROOT, "docs", "previews", "app-actual")
DIR = os.path.join(ROOT, "docs", "previews", "fondo-espacio")
OUT = os.path.join(ROOT, "docs", "previews", "fondo-espacio.png")
FREDOKA = os.path.join(ROOT, "app", "src", "main", "res", "font", "fredoka.ttf")
NUNITO = os.path.join(ROOT, "app", "src", "main", "res", "font", "nunito_regular.ttf")

BG = (9, 11, 38)
CREAM = (255, 246, 226)
DIM = (186, 190, 230)
SUN = (255, 201, 60)
OK = (155, 229, 100)
BAD = (255, 107, 74)

# Textos claros que van directo sobre el fondo (los de BoardBackgroundContrastTest)
def lerp(c, t, k): return tuple(c[i] + (t[i] - c[i]) * k for i in range(3))
AREAS = {"Memoria": (0x3B, 0x82, 0xF6), "Atención": (0xF5, 0x9E, 0x0B), "Razonamiento": (0x8B, 0x5C, 0xF6), "Lenguaje": (0x10, 0xB9, 0x81)}
TOKENS = [
    ("crema (saludo)", (0xF6, 0xEB, 0xD3)), ("blanco (títulos)", (255, 255, 255)), ("sol (racha)", (0xFF, 0xC9, 0x3C)), ("subtítulo", (0xD6, 0xD1, 0xF0)),
    ("OnNightDim Avance", (0xB4, 0xBF, 0xEA)), ("OnNightDim Hoy/Juegos", (0xC7, 0xD0, 0xFF)), ("coral", (0xFF, 0x6B, 0x4A)),
] + [(n + " clara", tuple(int(v) for v in lerp(c, (255, 255, 255), 0.45))) for n, c in AREAS.items()]
BANDS = [("arriba", 0.0, 0.12), ("medio", 0.40, 0.60), ("pie", 0.88, 1.0)]


def font(path, size, weight=None):
    f = ImageFont.truetype(path, size)
    if weight is not None:
        try:
            f.set_variation_by_axes([weight])
        except Exception:
            pass
    return f


def lum(c):
    def ch(v):
        v /= 255.0
        return v / 12.92 if v <= 0.03928 else ((v + 0.055) / 1.055) ** 2.4
    return 0.2126 * ch(c[0]) + 0.7152 * ch(c[1]) + 0.0722 * ch(c[2])


def contrast(a, b):
    la, lb = lum(a), lum(b)
    return (max(la, lb) + 0.05) / (min(la, lb) + 0.05)


def hexs(c): return "#%02X%02X%02X" % tuple(int(round(v)) for v in c)


def medidas(webp):
    a = np.asarray(Image.open(webp).convert("RGB")).astype(float)
    h = a.shape[0]
    out = {"mediana": tuple(np.median(a.reshape(-1, 3), axis=0))}
    for name, y0, y1 in BANDS:
        out[name] = tuple(np.median(a[int(h * y0):int(h * y1)].reshape(-1, 3), axis=0))
    return out


def paste_pair(sheet, d, x, y, pw, name, hoy, a, f1, f2):
    h = int(pw * 2401 / 1080)
    sheet.paste(Image.open(hoy).convert("RGB").resize((pw, h), Image.LANCZOS), (x, y))
    sheet.paste(Image.open(a).convert("RGB").resize((pw, h), Image.LANCZOS), (x + pw + 12, y))
    d.text((x, y + h + 6), "hoy", font=f1, fill=DIM)
    d.text((x + pw + 12, y + h + 6), "A · Espacio profundo", font=f1, fill=SUN)
    d.text((x, y + h + 36), name, font=f2, fill=CREAM)
    return h


def hoja():
    PW, MARGIN, GAP = 300, 40, 40
    f1, f2, ft, fs, fn = font(FREDOKA, 22, 600), font(NUNITO, 20), font(FREDOKA, 46, 600), font(FREDOKA, 30, 600), font(NUNITO, 24)
    pairs = [("Hoy", "hoy.png", "A-hoy.png"), ("Hoy · lámina de un área (con el velo nuevo)", "hoy-lamina.png", "A-hoy-lamina.png"), ("Juegos", "juegos.png", "A-juegos.png"),
             ("Avance", "avance.png", "A-avance.png"), ("Primer vuelo (el cielo usa los mismos tres tonos)", "primer-vuelo-2.png", "A-primer-vuelo-2.png")]
    width = MARGIN * 2 + len(pairs) * (PW * 2 + 12) + (len(pairs) - 1) * 24
    sheet = Image.new("RGB", (width, 3200), BG)
    d = ImageDraw.Draw(sheet)
    d.text((MARGIN, MARGIN), "Fondo «Espacio profundo» (A) — hoy | A", font=ft, fill=CREAM)
    d.text((MARGIN, MARGIN + 58), "Decisión de Ricardo del 17-oct: «como si estuviésemos en el espacio». Lámina #0E0D33 con nebulosa apagada y viñeta del 25 %, 55 luceros en tres profundidades. Capturas reales, mismos datos de ejemplo.", font=fn, fill=DIM)
    y = MARGIN + 110
    x = MARGIN
    ph = 0
    for name, hoy, a in pairs:
        ph = paste_pair(sheet, d, x, y, PW, name, os.path.join(HOY, hoy), os.path.join(DIR, a), f1, f2)
        x += PW * 2 + 12 + 24
    y += ph + 90

    # recorte al 100 %: el saludo y la fecha, en píxeles de la captura (1080 de ancho)
    d.text((MARGIN, y), "El texto sobre el fondo, al 100 % (saludo, fecha, farolitos y racha)", font=fs, fill=SUN)
    y += 46
    box = (0, 130, 1080, 420)
    ca = Image.open(os.path.join(HOY, "hoy.png")).convert("RGB").crop(box)
    cb = Image.open(os.path.join(DIR, "A-hoy.png")).convert("RGB").crop(box)
    sheet.paste(ca, (MARGIN, y)); sheet.paste(cb, (MARGIN + 1080 + GAP, y))
    d.text((MARGIN, y + ca.height + 6), "hoy", font=f1, fill=DIM); d.text((MARGIN + 1080 + GAP, y + ca.height + 6), "A", font=f1, fill=SUN)
    y += ca.height + 60

    # medidas
    d.text((MARGIN, y), "Medidas (de los render que entran a la app, fondo_lamina 720 × 1200)", font=fs, fill=SUN)
    y += 46
    mh, ma = medidas(os.path.join(DIR, "hoy-fondo_lamina.webp")), medidas(os.path.join(DIR, "A-fondo_lamina.webp"))
    d.text((MARGIN, y), "Mediana real de la lámina:", font=f1, fill=CREAM)
    y += 34
    for k in ["mediana", "arriba", "medio", "pie"]:
        d.text((MARGIN + 20, y), "%-8s  hoy %s   a   A %s" % (k, hexs(mh[k]), hexs(ma[k])), font=fn, fill=CREAM)
        sw = 26
        d.rectangle((MARGIN + 480, y + 2, MARGIN + 480 + sw, y + 2 + sw), fill=tuple(int(v) for v in mh[k]), outline=DIM)
        d.rectangle((MARGIN + 520, y + 2, MARGIN + 520 + sw, y + 2 + sw), fill=tuple(int(v) for v in ma[k]), outline=DIM)
        y += 34
    d.text((MARGIN + 20, y), "OnBoardHalo.Tone (el contorno de los textos claros) = la mediana nueva: %s (antes #333369). BoardNavy = #0E0D33 (antes #23255A)." % hexs(ma["mediana"]), font=fn, fill=DIM)
    y += 50

    d.text((MARGIN, y), "Contraste de los textos claros contra la mediana de cada banda de la lámina (mínimo exigido 4,5:1; mide BoardBackgroundContrastTest)", font=f1, fill=CREAM)
    y += 38
    cols = [MARGIN, MARGIN + 380, MARGIN + 560, MARGIN + 740, MARGIN + 920, MARGIN + 1100, MARGIN + 1280, MARGIN + 1460]
    heads = ["texto", "hoy arriba", "hoy medio", "hoy pie", "A arriba", "A medio", "A pie"]
    for i, h in enumerate(heads):
        d.text((cols[i], y), h, font=f2, fill=DIM)
    y += 32
    for name, c in TOKENS:
        d.text((cols[0], y), "%s  %s" % (name, hexs(c)), font=f2, fill=tuple(int(v) for v in c))
        for j, (m, off) in enumerate([(mh, 1), (ma, 4)]):
            for k, band in enumerate(["arriba", "medio", "pie"]):
                v = contrast(c, m[band])
                d.text((cols[off + k], y), "%.1f:1" % v, font=f2, fill=OK if v >= 4.5 else BAD)
        y += 30
    y += 24
    d.text((MARGIN, y), "Los luceros: de ~80 visibles a 46 (55 colocados), en tres profundidades (0,9 a 2,2 % del ancho; #EADFC9, #D3CBBA y #B9B3A8); sombra de contacto más corta en los chicos. La isla y la barra no se tocaron.", font=fn, fill=DIM)
    y += 36
    d.text((MARGIN, y), "La barra de pestañas (barra_bandeja.webp) no trae sombra horneada con el navy viejo (sus píxeles semitransparentes son crema): no hizo falta volver a renderizarla.", font=fn, fill=DIM)
    y += 50
    return sheet.crop((0, 0, width, y))


if __name__ == "__main__":
    hoja().save(OUT, optimize=True)
    print("ok:", OUT)
