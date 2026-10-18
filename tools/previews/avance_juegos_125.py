# -*- coding: utf-8 -*-
"""Lámina «antes | después» de Avance y Juegos (Tarea 125): las capturas REALES de la app (Roborazzi, `AppActualScreenshotTest`) de antes (las que están en git en `docs/previews/app-actual/`, de las Tareas 121 y 123) junto a las de ahora
(`app/src/test/screenshots/app-actual/`, recién grabadas). Avance, la grilla de Juegos, la ventana de Memoria, la ficha de un juego, Hoy y el Perfil.
Uso (antes de regenerar `docs/previews/app-actual/` con `app_actual.py`, porque «antes» sale de git HEAD):
  ./gradlew.bat :app:recordRoborazziDebug --tests "*AppActualScreenshotTest.hoy_y_juegos" --tests "*AppActualScreenshotTest.avance_y_perfil"
  python -I tools/previews/avance_juegos_125.py                       ->  docs/previews/avance-juegos-125.png
"""
import io, os, subprocess
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
AFTER_DIR = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots', 'app-actual')
OUT = os.path.join(ROOT, 'docs', 'previews', 'avance-juegos-125.png')
FONT = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font', 'fredoka.ttf'); NUNITO = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font', 'nunito_regular.ttf')
BG = (9, 11, 38); SUN = (255, 201, 60); DIM = (186, 190, 230); WHITE = (255, 255, 255)
W = 420                                                         # ancho de cada teléfono en la lámina
GAP = 26


def antes(name):
    """La captura de antes: la que está en git HEAD."""
    data = subprocess.run(['git', 'show', 'HEAD:docs/previews/app-actual/%s.png' % name], cwd=ROOT, capture_output=True, check=True).stdout
    return Image.open(io.BytesIO(data)).convert('RGB')


def despues(name):
    return Image.open(os.path.join(AFTER_DIR, name + '.png')).convert('RGB')


def recorta(im, h):
    return im.crop((0, 0, im.width, min(im.height, h)))


def chico(im):
    return im.resize((W, int(im.height * W / im.width)), Image.LANCZOS)


# (título, captura, recorte antes (px de la captura de 1080), recorte después)
PARES = [
    ('Avance', 'avance-completa', 8250, 5330),
    ('Juegos: las cuatro áreas', 'juegos', None, None),
    ('Juegos: la ventana de Memoria', 'juegos-memoria', None, None),
    ('Juegos: la ficha de un juego', 'juegos-ficha', None, None),
    ('Hoy', 'hoy', None, None),
    ('Perfil', 'perfil-completa', 4900, 3450),
]


def main():
    f_t = ImageFont.truetype(FONT, 34); f_h = ImageFont.truetype(FONT, 26); f_s = ImageFont.truetype(NUNITO, 22)
    bloques = []
    for titulo, name, cb, ca in PARES:
        a = chico(recorta(antes(name), cb) if cb else antes(name)); d = chico(recorta(despues(name), ca) if ca else despues(name))
        bloques.append((titulo, a, d))
    # filas: Avance solo (es alto), luego pares de dos en dos
    filas = [[bloques[0]], [bloques[1], bloques[2]], [bloques[3], bloques[4]], [bloques[5]]]
    ancho_fila = lambda fila: len(fila) * (2 * W + GAP) + (len(fila) + 1) * GAP * 2
    Wt = max(ancho_fila(f) for f in filas)
    y = 110; layout = []
    for fila in filas:
        h = max(max(a.height, d.height) for _, a, d in fila) + 90
        layout.append((fila, y)); y += h + 30
    im = Image.new('RGB', (Wt, y + 20), BG); dr = ImageDraw.Draw(im)
    dr.text((GAP * 2, 28), 'Avance y Juegos · antes | después (Tarea 125)', font=f_t, fill=WHITE)
    dr.text((GAP * 2, 72), 'Capturas reales de la app (teléfono de 393 × 873 dp, letra normal): a la izquierda de cada par, la app de antes; a la derecha, la de ahora.', font=f_s, fill=DIM)
    for fila, y0 in layout:
        x = GAP * 2
        for titulo, a, d in fila:
            dr.text((x, y0), titulo, font=f_h, fill=SUN)
            dr.text((x, y0 + 36), 'antes', font=f_s, fill=DIM); dr.text((x + W + GAP, y0 + 36), 'después', font=f_s, fill=WHITE)
            im.paste(a, (x, y0 + 70)); im.paste(d, (x + W + GAP, y0 + 70))
            x += 2 * W + GAP + GAP * 2
    im.save(OUT, optimize=True); print(OUT, im.size)


if __name__ == '__main__':
    main()
