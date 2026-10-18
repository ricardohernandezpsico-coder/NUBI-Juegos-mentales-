"""La app ACTUAL, para la revisión de pulido (Tarea 121, parte 2; 17-oct): lleva las capturas REALES que graba
`AppActualScreenshotTest` (Roborazzi, con unos 20 días de uso de ejemplo) a `docs/previews/app-actual/` y arma la lámina resumen `lamina.png`.

  ./gradlew.bat :app:recordRoborazziDebug --tests "*AppActualScreenshotTest*"     (graba en app/src/test/screenshots/app-actual/)
  python tools/previews/app_actual.py                                              ->  docs/previews/app-actual/*.png + lamina.png

Las pantallas que se deslizan (Avance, Ajustes, Perfil, Licencias, resultado) se capturan enteras en una imagen alta; aquí se recortan al contenido y,
en la lámina, se parten en tiras del alto de un teléfono. De Ajustes se corta el bloque «[Debug]» (solo existe en builds de depuración).
"""
import os
import shutil
import sys

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SRC = os.path.join(ROOT, "app", "src", "test", "screenshots", "app-actual")
OUT = os.path.join(ROOT, "docs", "previews", "app-actual")
FONT = os.path.join(ROOT, "app", "src", "main", "res", "font", "fredoka.ttf")
NUNITO = os.path.join(ROOT, "app", "src", "main", "res", "font", "nunito_regular.ttf")

BG = (9, 11, 38)
CREAM = (255, 246, 226)
DIM = (186, 190, 230)
SUN = (255, 201, 60)

# Recortes (en píxeles de la captura de 1080 de ancho; los fija mirar las imágenes, ver el README de la carpeta)
CROPS = {
    "avance-completa": (0, 8300),          # hasta «Ver más detalles» (debajo solo la barra de pestañas del marco)
    "perfil-completa": (0, 4660),          # hasta el último nivel de estudios
    "resultado-completa": (0, 4280),       # hasta «Jugar de nuevo»
    "ajustes-completa": (0, 5970),         # hasta el aviso de salud (debajo viene el bloque «[Debug]», que solo existe en builds de depuración)
    "licencias-completa": (0, 5400),       # hasta el aviso de salud
}


def font(path, size, weight=None):
    f = ImageFont.truetype(path, size)
    if weight is not None:
        try:
            f.set_variation_by_axes([weight])
        except Exception:
            pass
    return f


def load(name):
    return Image.open(os.path.join(SRC, name + ".png")).convert("RGB")


def cropped(name):
    im = load(name)
    if name in CROPS:
        a, b = CROPS[name]
        im = im.crop((0, a, im.width, min(im.height, b)))
    return im


def strips(im, scale_w, strip_h):
    """Parte una captura alta en tiras (cada una del ancho [scale_w] y hasta [strip_h] de alto, ya reducidas)."""
    k = scale_w / im.width
    small = im.resize((scale_w, int(im.height * k)), Image.LANCZOS)
    out = []
    y = 0
    while y < small.height:
        out.append(small.crop((0, y, scale_w, min(small.height, y + strip_h))))
        y += strip_h
    return out


def lamina():
    W = 360                                  # ancho de cada teléfono en la lámina
    GAP = 22
    H_PHONE = int(W * 2401 / 1080)           # alto de un teléfono entero
    sections = [
        ("Hoy", [("Hoy: la isla, la ruta y la barra", [load("hoy")]), ("Al tocar un edificio: la lámina del área", [load("hoy-lamina")])]),
        ("Juegos", [("Las cuatro áreas", [load("juegos")]), ("Memoria", [load("juegos-memoria")]), ("Atención", [load("juegos-atencion")]),
                    ("Razonamiento", [load("juegos-razonamiento")]), ("Lenguaje", [load("juegos-lenguaje")]), ("Ficha de un juego", [load("juegos-ficha")])]),
        ("Avance (entera, en tiras)", [("Avance", strips(cropped("avance-completa"), W, H_PHONE))]),
        ("Perfil, Ajustes y Licencias", [("Perfil", strips(cropped("perfil-completa"), W, H_PHONE)),
                                         ("Ajustes", strips(cropped("ajustes-completa"), W, H_PHONE)),
                                         ("Licencias y créditos", strips(cropped("licencias-completa") if os.path.exists(os.path.join(SRC, "licencias-completa.png")) else load("licencias"), W, H_PHONE))]),
        ("Primer vuelo, barra y resultado", [("Primer vuelo 1", [load("primer-vuelo-1")]), ("Primer vuelo 2", [load("primer-vuelo-2")]), ("Primer vuelo 3", [load("primer-vuelo-3")]),
                                              ("Resultado de una partida", strips(cropped("resultado-completa"), W, H_PHONE))]),
    ]
    title_f = font(FONT, 44, 600)
    sec_f = font(FONT, 30, 600)
    lab_f = font(NUNITO, 20)
    margin = 40
    # ancho de la lámina: lo que pida la sección más ancha
    def sec_width(items):
        n = sum(len(v) for _, v in items)
        return margin * 2 + n * W + (n - 1) * GAP + (len(items) - 1) * GAP
    width = max(sec_width(items) for _, items in sections)
    # alto de cada sección: rótulo + tiras
    def tiles(items):
        out = []
        for lab, vs in items:
            for i, v in enumerate(vs):
                if isinstance(v, Image.Image) and v.width != W:
                    v = v.resize((W, int(v.height * W / v.width)), Image.LANCZOS)
                out.append((lab if i == 0 else "", v, i, len(vs)))
        return out
    rows = [(name, tiles(items)) for name, items in sections]
    height = margin + 80
    for name, t in rows:
        height += 56 + max(v.height for _, v, _, _ in t) + 56
    sheet = Image.new("RGB", (width, height + margin), BG)
    d = ImageDraw.Draw(sheet)
    d.text((margin, margin), "Nubi · la app de hoy (17-oct): capturas reales con unos 20 días de uso de ejemplo", font=title_f, fill=CREAM)
    y = margin + 80
    for name, t in rows:
        d.text((margin, y), name, font=sec_f, fill=SUN)
        y += 56
        x = margin
        rowh = max(v.height for _, v, _, _ in t)
        for lab, v, i, n in t:
            sheet.paste(v, (x, y))
            d.rectangle((x, y, x + v.width - 1, y + v.height - 1), outline=(70, 74, 130), width=1)
            if lab:
                d.text((x, y + rowh + 8), lab, font=lab_f, fill=DIM)
            elif n > 1:
                pass
            x += W + GAP
            if i == n - 1:
                x += GAP
        y += rowh + 56
    return sheet


def main():
    if not os.path.isdir(SRC):
        sys.exit("faltan las capturas: " + SRC)
    os.makedirs(OUT, exist_ok=True)
    for f in sorted(os.listdir(SRC)):
        if f.endswith(".png"):
            im = cropped(f[:-4]) if f[:-4] in CROPS else load(f[:-4])
            im.save(os.path.join(OUT, f), optimize=True)
    lamina().save(os.path.join(OUT, "lamina.png"), optimize=True)
    print("ok:", OUT)


if __name__ == "__main__":
    main()
