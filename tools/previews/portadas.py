# -*- coding: utf-8 -*-
"""Las 17 portadas de la ficha del juego (Tarea 171, 25-oct-2026; `auditoria-fable/ficha-juego.md`, opción 1 «Portada»).

Cada portada es un recorte de una captura REAL del juego corriendo en Unity (`unity/test-results/capturas/<juego>/`, fuera de git; se sacan con
`bash tools/verificar-todo.sh --capturas todos`, 1080 × 2400): un momento de plena partida, SIN el marcador, la consigna ni avisos dentro del recorte (a 150 dp de alto no se leerían).
Salen de 1080 × 480 px en webp, a `app/src/main/res/drawable-nodpi/portada_<id>.webp` (la ficha la recorta a 150 dp de alto por el ancho que tenga el teléfono: la escena va al centro).
Se regeneran con este script cuando cambia el arte de un juego, como las láminas de `docs/previews/`.

Cada entrada de PORTADAS dice qué captura (archivo), dónde empieza el recorte (y, en píxeles de la captura), cuánto se amplía (zoom ≥ 1: 1 = el recorte tiene 1080 de ancho, 1,5 = 720 de ancho y se amplía) y, si no va al centro, desde qué x empieza.
Uso:  python -I tools/previews/portadas.py            → escribe las 17 portadas y la hoja de contacto tools/previews/out/portadas.png
      python -I tools/previews/portadas.py --solo piloto correo   → solo esas
      python -I tools/previews/portadas.py --marcos      → además deja `out/marcos-<id>.png`: la captura entera con el recorte marcado (para elegirlo)
      python -I tools/previews/portadas.py --zonas       → la hoja de contacto marca en rojo lo que la ficha le pone ENCIMA a la portada (la X, el emblema y «Sobre este juego»): la escena importante va fuera de esas zonas
"""
import os, sys
from PIL import Image, ImageDraw

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
CAPT = os.path.join(ROOT, 'unity', 'test-results', 'capturas')
RES = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'drawable-nodpi')
OUT = os.path.join(HERE, 'out')
W, H = 1080, 480                                              # la portada: 2,25 : 1 (la ficha la muestra a 150 dp de alto)
# lo que la ficha dibuja sobre la portada, en px de la imagen (la ventanilla mide 345 × 150 dp: 3,13 px por dp): la X arriba a la derecha, el emblema abajo a la izquierda y «Sobre este juego» abajo a la derecha (con letra 1,0)
ZONAS = [(905, 25, 1055, 175), (25, 382, 200, 480), (498, 388, 1055, 480)]

# juego: (captura, y del recorte, zoom, x del recorte o None = centrado)
PORTADAS = {
    'parejas':      ('09-t40.png', 1330, 1.0, None),
    'secuencia':    ('05-t08.png', 1060, 1.0, None),
    'bodega':       ('05-t08.png', 810, 1.0, None),
    'correo':       ('09-faro.png', 620, 1.0, None),
    'stroop':       ('06-t20.png', 500, 1.0, None),
    'piloto':       ('02-ruta-balizas.png', 1680, 1.4, None),
    'freno':        ('06-t20.png', 1420, 1.0, None),
    'satelites':    ('02-seguimiento.png', 940, 1.0, None),
    'radar':        ('02-destello.png', 560, 1.0, None),
    'calculo':      ('05-t08.png', 600, 1.0, None),
    'engranajes':   ('11-t40.png', 730, 1.0, None),
    'acoplamiento': ('05-luz-encendida.png', 1210, 1.0, None),
    'aterrizaje':   ('02-acierto.png', 1400, 1.0, None),
    'anagramas':    ('05-fichas.png', 1190, 1.0, None),
    'meteoros':     ('05-t08.png', 1100, 1.0, None),
    'disparate':    ('05-t08.png', 250, 1.0, None),
    'intrusa':      ('05-t08.png', 700, 1.0, None),
}


def crop_of(game):
    f, y, zoom, x = PORTADAS[game]
    path = os.path.join(CAPT, game, f)
    if not os.path.exists(path): sys.exit(f'falta la captura {path}: sacarlas con `bash tools/verificar-todo.sh --capturas todos`')
    im = Image.open(path).convert('RGB')
    w, h = round(W / zoom), round(H / zoom)
    x0 = (im.width - w) // 2 if x is None else x
    box = (x0, y, x0 + w, y + h)
    assert 0 <= box[0] and box[2] <= im.width and 0 <= box[1] and box[3] <= im.height, (game, box, im.size)
    return im, box, im.crop(box).resize((W, H), Image.LANCZOS)


def main():
    args = sys.argv[1:]
    only = args[args.index('--solo') + 1:] if '--solo' in args else list(PORTADAS)
    only = [a for a in only if not a.startswith('--')]
    os.makedirs(OUT, exist_ok=True)
    total = 0; made = []
    for g in only:
        im, box, c = crop_of(g)
        dst = os.path.join(RES, f'portada_{g}.webp')
        c.save(dst, 'WEBP', quality=80, method=6)
        total += os.path.getsize(dst); made.append((g, c))
        if '--marcos' in args:
            full = im.copy(); d = ImageDraw.Draw(full); d.rectangle(box, outline=(255, 255, 0), width=6)
            full.resize((full.width // 2, full.height // 2), Image.LANCZOS).save(os.path.join(OUT, f'marcos-{g}.png'))
        print(f'{g:13s} {PORTADAS[g][0]:24s} y={PORTADAS[g][1]:5d} zoom={PORTADAS[g][2]}  {os.path.getsize(dst) // 1024} KB')
    # hoja de contacto: las portadas a tamaño real, de dos en dos
    cols = 2; gap = 24
    rows = (len(made) + cols - 1) // cols
    sheet = Image.new('RGB', (cols * W + (cols + 1) * gap, rows * (H + gap) + gap), (20, 18, 63))
    for i, (g, c) in enumerate(made):
        if '--zonas' in args:
            c = c.convert('RGBA'); ov = Image.new('RGBA', c.size, (0, 0, 0, 0)); od = ImageDraw.Draw(ov)
            for z in ZONAS: od.rectangle(z, fill=(255, 0, 0, 90), outline=(255, 60, 60, 255), width=3)
            c = Image.alpha_composite(c, ov).convert('RGB')
        sheet.paste(c, (gap + (i % cols) * (W + gap), gap + (i // cols) * (H + gap)))
    sheet.save(os.path.join(OUT, 'portadas.png'), optimize=True)
    print(f'{len(made)} portadas, {total // 1024} KB en total; hoja: {os.path.join(OUT, "portadas.png")}')


if __name__ == '__main__':
    main()
