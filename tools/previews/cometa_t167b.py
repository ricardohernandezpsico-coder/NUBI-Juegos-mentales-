# -*- coding: utf-8 -*-
"""Lámina chica «cometa-t167b» de la Tarea 167 (25-oct-2026): el cometa del Desafío de hoy con la «cola ancha» que eligió Ricardo (la cola nace del ancho completo de la cabeza y se afina hasta una punta: una gota alargada en diagonal), en dos variantes y al lado el cometa actual.
Cada variante va (1) ampliada (render de Blender a 12 px por dp, mostrado a 8) sobre lila, sol y crema; (2) a su tamaño real de 24 dp (72 px a 3 px por dp) sobre los mismos tres fondos; (3) dentro del botón lila de media fila y del botón sol ancho de Hoy: son las CAPTURAS REALES de `Tarea167CapturasTest`
(`app/src/test/screenshots/tarea-167/`, `primera-hora` y `ruta-completa`) con el ícono cambiado: se borra el cometa actual (relleno fila por fila con el color de la losa) y se pone el nuevo centrado igual que lo hace la app (el centro del cuerpo en el centro de la caja de 24 dp, a 2,75 px por dp).
Los renders van en `tools/isla/out/cometa-t167b/<variante>/kit_i_cometa.png` (gitignored). ESTA LÁMINA ES EL REGISTRO DE LA ELECCIÓN de Ricardo (25-oct: la variante (b)): después de elegir, `isla.py` quedó solo con (b) (`ic_cometa`), así que `actual` (la cabeza con la cuña delgada) y `A` ya no se pueden volver a renderizar del generador;
la (b) sí: `blender -b -P tools/isla/isla.py -- --engine CYCLES --samples 112 --kit --kit-solo cometa --kit-px 12 --out <carpeta>`. Para rehacer la lámina hace falta conservar los renders de esas carpetas.
Uso:  python -I tools/previews/cometa_t167b.py [variantes: actual A B ...]      → docs/previews/cometa-t167b.png
"""
import os, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SHOTS = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots', 'tarea-167')
RENDERS = os.path.join(ROOT, 'tools', 'isla', 'out', 'cometa-t167b')
OUT = os.path.join(ROOT, 'docs', 'previews', 'cometa-t167b.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
NAVY, SUN, DIM, CREAM, LILAC = (20, 18, 63), (255, 203, 69), (200, 196, 235), (245, 235, 219), (184, 164, 255)
FONDOS = (('lila', LILAC), ('sol', SUN), ('crema', CREAM))
PX_RENDER = 12; DENS = 1080 / 393.0              # px por dp del render grande y de las capturas (393 dp de ancho en 1080 px)
TITULOS = {'actual': 'ACTUAL · cabeza y una cuña delgada (la guía de Fable)',
           'A': '(a) gota alargada en diagonal + los dos luceros',
           'B': '(b) la misma gota con dos estelas finas y cortas (en lugar de los luceros)',
           'B2': '(b2) la gota con las dos estelas y los dos luceros'}
# dónde está el ícono dentro de cada captura (px de la imagen de 1080 × 2400): una zona que solo lo contiene a él (nunca el texto) y el recorte del botón
LILA = {'captura': 'primera-hora', 'zona': (86, 1486, 172, 1594), 'recorte': (36, 1440, 540, 1620)}
SOL = {'captura': 'ruta-completa', 'zona': (130, 1290, 240, 1410), 'recorte': (36, 1240, 1048, 1448)}


def font(px, bold=True): return ImageFont.truetype(os.path.join(FONTS, 'fredoka.ttf' if bold else 'nunito_regular.ttf'), int(px))


def ruta(v): return os.path.join(RENDERS, v, 'kit_i_cometa.png')


def limpia(path):
    """El render con la bruma del suelo recortada, igual que `kit.py` (se resta un piso de 12/255 y el alfa llega a 0 en los bordes)."""
    im = Image.open(path).convert('RGBA'); a = np.asarray(im).astype(float).copy()
    al = np.clip((a[..., 3] - 12.0) * 255.0 / (255.0 - 12.0), 0, 255)
    h, w = al.shape; yy, xx = np.mgrid[0:h, 0:w]; d = np.minimum.reduce([xx, yy, w - 1 - xx, h - 1 - yy]).astype(float)
    al *= 0.5 - 0.5 * np.cos(np.pi * np.clip(d / (10.0 * PX_RENDER / 3), 0, 1)); a[..., 3] = al
    return Image.fromarray(a.astype(np.uint8), 'RGBA')


def centro_cuerpo(im):
    """El centro del cuerpo (lo muy opaco, sin la sombra) en px de la imagen: así lo centra la app en su caja de 24 dp."""
    ys, xs = np.where(np.asarray(im)[..., 3] >= 235)
    return (xs.min() + xs.max() + 1) / 2.0, (ys.min() + ys.max() + 1) / 2.0, (xs.max() + 1 - xs.min(), ys.max() + 1 - ys.min())


def icono(im, px_dp, lado, fondo):
    """El ícono a [px_dp] píxeles por dp con el centro de su cuerpo en el centro de un cuadrado de [lado] px de [fondo]."""
    k = px_dp / PX_RENDER; cx, cy, _ = centro_cuerpo(im)
    g = im.resize((max(1, int(round(im.width * k))), max(1, int(round(im.height * k)))), Image.LANCZOS)
    bg = Image.new('RGBA', (lado, lado), fondo + (255,))
    bg.alpha_composite(g, (int(round(lado / 2.0 - cx * k)), int(round(lado / 2.0 - cy * k)))) if True else None
    return bg.convert('RGB')


def boton(cfg, im_nuevo, ref):
    """El recorte del botón de la captura real; con [im_nuevo] se cambia el cometa actual por el nuevo."""
    src = Image.open(os.path.join(SHOTS, cfg['captura'] + '.png')).convert('RGB')
    if im_nuevo is None: return src.crop(cfg['recorte'])
    a = np.asarray(src).astype(float); x0, y0, x1, y1 = cfg['zona']
    luz = a[y0:y1, x0:x1].sum(axis=2) / 3.0
    ys, xs = np.where(luz < 90)                                                                  # la tinta del cometa actual (oscura y opaca; la sombra es más clara y queda fuera)
    bx0, by0, bx1, by1 = xs.min() + x0, ys.min() + y0, xs.max() + x0 + 1, ys.max() + y0 + 1
    cx, cy = (bx0 + bx1) / 2.0, (by0 + by1) / 2.0
    ex0, ey0, ex1, ey1 = bx0 - 12, by0 - 12, bx1 + 20, by1 + 22                                  # el cometa y su sombra suave (cae abajo a la derecha)
    for y in range(ey0, ey1):                                                                    # borrarlo: cada fila se rellena con el degradado del color de la losa de un lado al otro
        izq = np.median(a[y, ex0 - 4:ex0 - 1], axis=0); der = np.median(a[y, ex1 + 1:ex1 + 4], axis=0)
        t = (np.arange(ex0, ex1) - ex0) / max(1, ex1 - ex0 - 1)
        a[y, ex0:ex1] = izq[None, :] * (1 - t)[:, None] + der[None, :] * t[:, None]
    base = Image.fromarray(a.astype(np.uint8), 'RGB').convert('RGBA')
    _, _, (rw, rh) = centro_cuerpo(ref)                                                          # la escala de ESTE botón: lo que mide hoy el cometa actual en la captura (el botón ancho lo dibuja un poco más grande)
    k = ((bx1 - bx0) / float(rw) + (by1 - by0) / float(rh)) / 2.0; ox, oy, _ = centro_cuerpo(im_nuevo)
    g = im_nuevo.resize((int(round(im_nuevo.width * k)), int(round(im_nuevo.height * k))), Image.LANCZOS)
    base.alpha_composite(g, (int(round(cx - ox * k)), int(round(cy - oy * k))))
    return base.convert('RGB').crop(cfg['recorte'])


def main():
    variantes = [v for v in (sys.argv[1:] or ['actual', 'A', 'B']) if os.path.exists(ruta(v))]
    if 'actual' not in variantes: sys.exit('falta el render «actual»')
    if not variantes: sys.exit('faltan los renders en ' + RENDERS)
    TILE, PX_GRANDE, REAL = 176, 8, 72
    lila_w = LILA['recorte'][2] - LILA['recorte'][0]; sol_w = SOL['recorte'][2] - SOL['recorte'][0]
    sol_h = SOL['recorte'][3] - SOL['recorte'][1]; lila_h = LILA['recorte'][3] - LILA['recorte'][1]
    MG, GAP = 40, 20
    linea1 = 3 * TILE + 2 * 8 + GAP + 3 * (REAL + 24) + 2 * 8 + GAP + lila_w
    W = max(linea1, sol_w) + 2 * MG
    F_T, F_H, F_C, F_P = font(46), font(30), font(22, False), font(24, False)
    probe = ImageDraw.Draw(Image.new('RGB', (10, 10)))
    nota = ('Ricardo eligió la «cola ancha»: la cola nace del ancho completo de la cabeza y se afina hasta una punta (gota alargada en diagonal: cabeza abajo a la derecha, punta arriba a la izquierda); la diagonal y los luceros la separan de una gota de agua. '
            'Ampliado = render de Blender a 12 px por dp (aquí a 8). El botón es la CAPTURA REAL de Hoy (393 dp de ancho) con solo el ícono cambiado.')
    def partir(texto, fnt, ancho):
        out, cur = [], ''
        for p in texto.split(' '):
            t = (cur + ' ' + p).strip()
            if probe.textlength(t, font=fnt) <= ancho: cur = t
            else: out.append(cur); cur = p
        return out + ([cur] if cur else [])
    nl = partir(nota, F_P, W - 2 * MG)
    bloque = 50 + TILE + 16 + sol_h + 36
    H = 40 + 64 + len(nl) * 32 + 20 + len(variantes) * bloque + 50
    sheet = Image.new('RGB', (W, H), NAVY); d = ImageDraw.Draw(sheet)
    y = 40; d.text((MG, y), 'El cometa del Desafío de hoy · «cola ancha»', font=F_T, fill=SUN); y += 64
    for ln in nl: d.text((MG, y), ln, font=F_P, fill=DIM); y += 32
    y += 20
    ref = limpia(ruta('actual'))
    for v in variantes:
        im = limpia(ruta(v)); d.text((MG, y), TITULOS.get(v, v), font=F_H, fill=(255, 255, 255)); y += 50
        x = MG
        for (n, c) in FONDOS:                                                                    # ampliado
            sheet.paste(icono(im, PX_GRANDE, TILE, c), (x, y)); d.rectangle((x, y, x + TILE - 1, y + TILE - 1), outline=(70, 74, 130)); x += TILE + 8
        x += GAP - 8
        for (n, c) in FONDOS:                                                                    # 24 dp reales (72 px a 3 px por dp)
            s = icono(im, 3, REAL, c); yy = y + (TILE - REAL) // 2
            sheet.paste(s, (x, yy)); d.rectangle((x, yy, x + REAL - 1, yy + REAL - 1), outline=(70, 74, 130)); x += REAL + 24 + 8
        d.text((x - 3 * (REAL + 32) - 8, y + (TILE - REAL) // 2 + REAL + 8), '24 dp reales (72 px)', font=F_C, fill=DIM)
        x += GAP - 8
        nuevo = None if v == 'actual' else im
        sheet.paste(boton(LILA, nuevo, ref), (x, y + (TILE - lila_h) // 2)); y += TILE + 16
        sheet.paste(boton(SOL, nuevo, ref), (MG, y)); y += sol_h + 36
    sheet.save(OUT, optimize=True); print(OUT, sheet.size, variantes)


if __name__ == '__main__':
    main()
