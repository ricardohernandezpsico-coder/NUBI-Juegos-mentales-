"""Hoja «antes | después» de la Tarea 123 (base técnica del pulido; 18-oct): la app de ayer (capturas de la Tarea 121) frente a la de hoy, con la letra de 16, la paleta única, la barra fija y los luceros detrás.
Capturas reales de `AppActualScreenshotTest` (Roborazzi) con los mismos datos de ejemplo; solo cambia el día (de sábado a domingo).

  - «antes»: docs/previews/pulido-123/antes-*.png (reducidas a 540 de ancho; son las que había en docs/previews/app-actual/ el 17-oct).
  - «después»: docs/previews/app-actual/*.png (regrabadas en la Tarea 123).

  python tools/previews/pulido_123.py   ->  docs/previews/pulido-123.png
"""
import os

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
ANTES = os.path.join(ROOT, "docs", "previews", "pulido-123")
AHORA = os.path.join(ROOT, "docs", "previews", "app-actual")
OUT = os.path.join(ROOT, "docs", "previews", "pulido-123.png")
FREDOKA = os.path.join(ROOT, "app", "src", "main", "res", "font", "fredoka.ttf")
NUNITO = os.path.join(ROOT, "app", "src", "main", "res", "font", "nunito_regular.ttf")

BG = (9, 11, 38)
CREAM = (255, 246, 226)
DIM = (186, 190, 230)
SUN = (255, 201, 60)

PAIRS = [
    ("Hoy", "hoy"),
    ("Hoy · lámina de un área (ahora cubre la barra y sale sin espera)", "hoy-lamina"),
    ("Juegos", "juegos"),
    ("Juegos · ventana de Memoria", "juegos-memoria"),
    ("Juegos · ficha de un juego", "juegos-ficha"),
    ("Avance", "avance"),
    ("Perfil (logros en 3 columnas: ninguna palabra se parte)", "perfil"),
    ("Resultado de una partida", "resultado"),
]
NOTES = [
    "Letra: el texto secundario pasa de 14 a 16 sp (14 queda solo en rótulos de gráficos, como la escala de etapas). La isla de Hoy baja 16 dp (344 dp a 393 × 873).",
    "Barra: ahora es una capa fija encima de las páginas; no se encoge al abrir una lámina o ventana (se desvanece en 150 ms) y las ventanas la cubren. Sin desvanecido de página: el contenido termina nítido sobre la barra.",
    "Lámina de un lugar: velo de 200 ms y lámina de 400 ms sin espera (antes 360 y 460 + 180); ocupa todo el alto, con la lista «Tus juegos aquí» a la vista.",
    "Paleta única (NubiTokens): el texto secundario sobre el cielo es un solo tono (#C7D0FF), un poco más claro donde era #B4BFEA; las áreas salen de una sola fuente. Perfil: los logros van de a 3, con letra de 16 sp.",
    "Movimiento: 80 ms al apretar y 140 al soltar en la plasticina, 300 ms al pasar de pestaña, 150-200 en estados; háptica al apoyar el dedo en el kit (solo con «Vibración y respuesta háptica» encendida; no se ve en una imagen).",
]


def font(path, size, weight=None):
    f = ImageFont.truetype(path, size)
    if weight is not None:
        try:
            f.set_variation_by_axes([weight])
        except Exception:
            pass
    return f


def hoja():
    PW, MARGIN, GAP, COL = 300, 40, 12, 26
    ph = int(PW * 2401 / 1080)
    per_row = 4
    f1, f2, ft, fn = font(FREDOKA, 22, 600), font(NUNITO, 20), font(FREDOKA, 46, 600), font(NUNITO, 24)
    width = MARGIN * 2 + per_row * (PW * 2 + GAP) + (per_row - 1) * COL
    sheet = Image.new("RGB", (width, 4000), BG)
    d = ImageDraw.Draw(sheet)
    d.text((MARGIN, MARGIN), "Tarea 123 · base técnica del pulido: antes | después", font=ft, fill=CREAM)
    y = MARGIN + 70
    for start in range(0, len(PAIRS), per_row):
        x = MARGIN
        for title, name in PAIRS[start:start + per_row]:
            a = Image.open(os.path.join(ANTES, "antes-%s.png" % name)).convert("RGB").resize((PW, ph), Image.LANCZOS)
            b = Image.open(os.path.join(AHORA, name + ".png")).convert("RGB").resize((PW, ph), Image.LANCZOS)
            sheet.paste(a, (x, y)); sheet.paste(b, (x + PW + GAP, y))
            d.text((x, y + ph + 6), "antes", font=f1, fill=DIM)
            d.text((x + PW + GAP, y + ph + 6), "después", font=f1, fill=SUN)
            d.text((x, y + ph + 36), title[:60], font=f2, fill=CREAM)
            x += PW * 2 + GAP + COL
        y += ph + 80
    for n in NOTES:
        d.text((MARGIN, y), "· " + n, font=fn, fill=CREAM)
        y += 36
    return sheet.crop((0, 0, width, y + 20))


if __name__ == "__main__":
    hoja().save(OUT, optimize=True)
    print("ok:", OUT)
