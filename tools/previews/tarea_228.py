# -*- coding: utf-8 -*-
"""Lámina de la Tarea 228 (4-nov-2026): los 10 emblemas de las divisiones de la liga de la semana (`docs/liga-de-la-semana.md` §1 y §1.1), de abajo arriba, cada uno en su ventanilla noche `#100F3A` de 52 dp (la cifra) y de 28 dp (la escalera), a 3 px por dp (el tamaño de las imágenes
de la app), y los cinco pares que se podrían confundir a 28 dp.
Los emblemas salen de `app/src/main/res/drawable-nodpi/division_*.webp` o, los cinco nuevos mientras la Tarea 229 no los cablee, de `tools/juegos/emblemas_liga/` (las maestras de Blender: `tools/juegos/emblemas_divisiones.py` y `emblemas_divisiones_app.py`). La ventanilla se dibuja como la de la app (`ui/components/DivisionEmblem.kt`): esquina de lado × 0,23 y la imagen al 86 %.
Uso:  python -I tools/previews/tarea_228.py      → docs/previews/tarea-228.png
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
RES = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'drawable-nodpi')
LIGA = os.path.join(ROOT, 'tools', 'juegos', 'emblemas_liga')    # los cinco nuevos esperan acá, fuera del APK, hasta que la Tarea 229 los cablee (Tarea 232b)
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-228.png')
NAVY, SUN, DIM, WHITE, LINE, NIGHT = (20, 18, 63), (255, 203, 69), (200, 196, 235), (240, 244, 255), (90, 86, 160), (16, 15, 58)
K = 3                                                       # px por dp
MARGIN = 28
# (línea %, nombre, archivo): de la división más baja a la más alta
DIVS = [(0, 'En la plataforma', 'plataforma'), (10, 'Despegando', 'despegando'), (20, 'Sobre las nubes', 'nubes'), (30, 'En órbita', 'orbita'), (40, 'En la Luna', 'luna'),
        (50, 'En Marte', 'marte'), (60, 'Entre asteroides', 'asteroides'), (70, 'En Júpiter', 'jupiter'), (80, 'Entre estrellas', 'estrellas'), (90, 'En otra galaxia', 'galaxia')]
PARES = [('En Marte', 'marte', 'En órbita', 'orbita', 'terracota oscura con casquete crema y sin anillo / naranja claro con anillo'),
         ('Entre asteroides', 'asteroides', 'En la Luna', 'luna', 'cuatro rocas pardas abolladas / UNA esfera lisa gris lavanda'),
         ('En Júpiter', 'jupiter', 'En órbita', 'orbita', 'bandas horizontales y mancha roja, sin anillo / un solo color con anillo'),
         ('En otra galaxia', 'galaxia', 'Entre estrellas', 'estrellas', 'espiral de dos brazos con centro claro / tres luceros sueltos'),
         ('Sobre las nubes', 'nubes', 'Despegando', 'despegando', 'un banco de nubes grande, sin cohete / el cohete con tres nubecitas')]


def font(px, bold=True): return ImageFont.truetype(os.path.join(FONTS, 'fredoka.ttf' if bold else 'nunito_regular.ttf'), int(px))


def emblema(archivo):
    for carpeta in (RES, LIGA):
        ruta = os.path.join(carpeta, 'division_%s.webp' % archivo)
        if os.path.exists(ruta): return Image.open(ruta).convert('RGBA')
    raise FileNotFoundError('division_%s.webp no está en res/drawable-nodpi ni en tools/juegos/emblemas_liga' % archivo)


def ventanilla(archivo, dp):
    """La ventanilla noche de [dp] dp de lado con el emblema al 86 %, a K px por dp (como `DivisionEmblem`)."""
    side = dp * K
    w = Image.new('RGBA', (side, side), (0, 0, 0, 0)); d = ImageDraw.Draw(w)
    d.rounded_rectangle((0, 0, side - 1, side - 1), radius=int(side * 0.23), fill=NIGHT + (255,), outline=(70, 66, 140, 255), width=1)
    inner = int(round(side * 0.86)); im = emblema(archivo).resize((inner, inner), Image.LANCZOS)
    w.alpha_composite(im, ((side - inner) // 2, (side - inner) // 2))
    return w


def main():
    sf = font(15, bold=False)
    W = 1260
    row_h = 176; ladder_h = len(DIVS) * row_h
    pair_h = 250
    H = MARGIN + 86 + 34 + max(ladder_h, 5 * 250) + 40 + MARGIN
    sheet = Image.new('RGB', (W, H), NAVY); d = ImageDraw.Draw(sheet)
    d.text((MARGIN, MARGIN), 'Tarea 228 · Los 10 emblemas de las divisiones de la liga de la semana', font=font(28), fill=SUN)
    sub = 'De abajo arriba (la décima, arriba). Cada emblema en su ventanilla noche de 52 dp (la cifra) y de 28 dp (la escalera), a 3 px por dp. Los cinco NUEVOS: Sobre las nubes, En Marte, Entre asteroides, En Júpiter y En otra galaxia.'
    cur = ''; yl = MARGIN + 42
    for w in sub.split(' '):
        t = (cur + ' ' + w).strip()
        if d.textlength(t, font=sf) <= W - 2 * MARGIN: cur = t
        else: d.text((MARGIN, yl), cur, font=sf, fill=DIM); yl += 20; cur = w
    d.text((MARGIN, yl), cur, font=sf, fill=DIM)
    y0 = MARGIN + 86
    d.text((MARGIN, y0), 'LAS DIEZ DIVISIONES', font=font(17), fill=SUN)
    nuevos = {'nubes', 'marte', 'asteroides', 'jupiter', 'galaxia'}
    y = y0 + 34
    for k, (linea, nombre, arch) in reversed(list(enumerate(DIVS))):
        n = k + 1
        d.rounded_rectangle((MARGIN, y, 590, y + row_h - 10), radius=14, outline=LINE)
        d.text((MARGIN + 18, y + 22), '%d · %s' % (n, nombre), font=font(24), fill=WHITE)
        d.text((MARGIN + 18, y + 60), 'desde el %d %% de la referencia' % linea if linea else 'la primera, desde el 0 %', font=sf, fill=DIM)
        if arch in nuevos: d.text((MARGIN + 18, y + 90), 'NUEVO', font=font(17), fill=SUN)
        sheet.paste(ventanilla(arch, 52), (360, y + 4), ventanilla(arch, 52))
        sheet.paste(ventanilla(arch, 28), (360 + 52 * K + 22, y + 4 + (52 - 28) * K // 2), ventanilla(arch, 28))
        y += row_h
    # a la derecha: los pares que se podrían confundir, a 28 dp y a 52 dp
    x0 = 640
    d.text((x0, y0), 'LOS CINCO PARES QUE SE PODRÍAN CONFUNDIR (a 28 dp y a 52 dp)', font=font(17), fill=SUN)
    yy = y0 + 34
    for (na, aa, nb, ab, nota) in PARES:
        d.rounded_rectangle((x0, yy, W - MARGIN, yy + pair_h - 12), radius=14, outline=LINE)
        for i, (nom, arch) in enumerate(((na, aa), (nb, ab))):
            x = x0 + 16 + i * 290
            d.text((x, yy + 10), nom, font=font(18), fill=WHITE)
            v52 = ventanilla(arch, 52); v28 = ventanilla(arch, 28)
            sheet.paste(v52, (x, yy + 40), v52); sheet.paste(v28, (x + 52 * K + 12, yy + 40 + (52 - 28) * K // 2), v28)
        # la frase, partida a mano en dos líneas
        words = nota.split(' '); cur = ''; lines = []
        for w in words:
            t = (cur + ' ' + w).strip()
            if d.textlength(t, font=sf) <= W - MARGIN - x0 - 32: cur = t
            else: lines.append(cur); cur = w
        lines.append(cur)
        for j, line in enumerate(lines): d.text((x0 + 16, yy + 40 + 52 * K + 6 + j * 20), line, font=sf, fill=DIM)
        yy += pair_h
    # la fila de los diez a 28 dp, como la escalera de la app (de la más alta a la más baja)
    yfila = max(y, yy) + 8
    sheet2 = Image.new('RGB', (W, yfila + 28 * K + 70), NAVY); sheet2.paste(sheet.crop((0, 0, W, yfila)), (0, 0)); d2 = ImageDraw.Draw(sheet2)
    d2.text((MARGIN, yfila), 'LOS DIEZ A 28 dp, DE LA MÁS ALTA A LA MÁS BAJA (así se verán en la escalera de la app)', font=font(17), fill=SUN)
    x = MARGIN
    for (linea, nombre, arch) in reversed(DIVS):
        v = ventanilla(arch, 28); sheet2.paste(v, (x, yfila + 30), v); x += 28 * K + 24
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    sheet2.save(OUT, optimize=True)
    print('listo', OUT, sheet2.size)


main()
