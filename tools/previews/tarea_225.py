# -*- coding: utf-8 -*-
"""Lámina de la Tarea 225 (4-nov-2026): «Tú» sale de la barra y pasa a un botón redondo arriba a la derecha. Capturas REALES del emulador (`medium_phone`) con `tools/capturar_cabeceras.py`, antes y después: las cuatro cabeceras (Hoy, Juegos, Contrarreloj y Avance)
con el botón, la barra de 4 y Tú abierto con su «‹», a 393 y a 360 dp con letra 1,0 · 1,3 · 2,0; y el paso 3 de la guía.
Las capturas salen de `unity/test-results/t225/` (fuera de git; `antes_<ancho>_<letra>_<pestaña>.png` y `despues_...`). Uso:  python -I tools/previews/tarea_225.py      → docs/previews/tarea-225.png
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SHOTS = os.path.join(ROOT, 'unity', 'test-results', 't225')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-225.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
NAVY, SUN, DIM, LINE, WHITE = (20, 18, 63), (255, 203, 69), (200, 196, 235), (90, 86, 160), (240, 244, 255)
GAP, MARGIN = 28, 24
CONFIGS = [(393, '10', '1,0'), (393, '13', '1,3'), (393, '20', '2,0'), (360, '10', '1,0'), (360, '13', '1,3'), (360, '20', '2,0')]
TABS4 = ['Hoy', 'Juegos', 'Contrarreloj', 'Avance']


def font(px, bold=True): return ImageFont.truetype(os.path.join(FONTS, 'fredoka.ttf' if bold else 'nunito_regular.ttf'), int(px))


def wrap(draw, text, f, width):
    lines = []
    for para in text.split('\n'):
        cur = ''
        for word in para.split(' '):
            t = (cur + ' ' + word).strip()
            if draw.textlength(t, font=f) <= width: cur = t
            else: lines.append(cur); cur = word
        lines.append(cur)
    return lines


def load(name):
    path = os.path.join(SHOTS, name + '.png')
    if not os.path.exists(path):
        im = Image.new('RGB', (393, 200), (40, 36, 100)); ImageDraw.Draw(im).text((10, 80), 'falta ' + name, font=font(18), fill=SUN); return im
    return Image.open(path).convert('RGB')


def dp_crop(name, w, y0, y1, from_bottom=False):
    """Recorta una franja de la captura (`w` dp de ancho; 2,625 px por dp) y la deja a 1 dp = 1 px."""
    im = load(name); k = im.width / float(w)
    if from_bottom: box = (0, im.height - int(y1 * k), im.width, im.height - int(y0 * k))
    else: box = (0, int(y0 * k), im.width, int(y1 * k))
    c = im.crop(box)
    return c.resize((w, int(round(c.height / k))), Image.LANCZOS)


def full(name, w):
    im = load(name); return im.resize((w, int(round(im.height * w / im.width))), Image.LANCZOS)


def main():
    sf = font(15, bold=False)
    d0 = ImageDraw.Draw(Image.new('RGB', (10, 10)))
    sheet_w = MARGIN * 2 + 4 * 393 + 3 * GAP
    items = []          # (kind, ...) en el orden en que se dibujan

    # 1) las cuatro cabeceras, a cada tamaño y letra: antes arriba, después abajo
    for w, sc, lab in CONFIGS:
        antes = [dp_crop('antes_%d_%s_%s' % (w, sc, t), w, 0, 118) for t in TABS4]
        desp = [dp_crop('despues_%d_%s_%s' % (w, sc, t), w, 0, 118) for t in TABS4]
        items.append(('cab', '%d dp · letra %s' % (w, lab), antes, desp))

    # 2) la barra: antes (5) y después (4), de a dos pares por fila
    caps = {
        (393, '10'): 'Cuatro columnas, más aire.', (393, '13'): 'Los rótulos se dibujan a 1,3 (con cinco, a 393 dp según el teléfono, 1,2 o 1,3).', (393, '20'): 'Con letra 2,0 el tope es 1,3: una línea y ninguna palabra partida.',
        (360, '10'): 'Cuatro columnas, más aire.', (360, '13'): 'A 360 dp los rótulos ya se dibujan a 1,3 (con cinco caían a 1,1).', (360, '20'): 'Con letra 2,0 el tope es 1,3 también a 360 dp: una línea y ninguna palabra partida.',
    }
    bars = []
    for w, sc, lab in CONFIGS:
        bars.append(('Barra ahora · %d dp · letra %s' % (w, lab), 'Cinco pestañas: «Tú» es la quinta columna.', dp_crop('antes_%d_%s_Hoy' % (w, sc), w, 0, 132, from_bottom=True)))
        bars.append(('Barra con cuatro · %d dp · letra %s' % (w, lab), caps[(w, sc)], dp_crop('despues_%d_%s_Hoy' % (w, sc), w, 0, 132, from_bottom=True)))

    # 3) Tú: antes (la pestaña) y después (abierto a pantalla completa), y el paso 3 de la guía
    tu = [
        ('Tú ahora · pestaña de la barra · 393 dp', 'Con la barra de abajo; sin «‹».', full('antes_393_10_Tú', 393)),
        ('Tú abierto desde el botón · 393 dp', 'A pantalla completa, SIN la barra, con el «‹» de 48 dp arriba a la izquierda (lector: «Volver»). Atrás y el «‹» vuelven a la pestaña de donde se abrió.', full('despues_393_10_TuAbierto', 393)),
        ('Tú abierto · 360 dp · letra 2,0', 'El título y el «‹» no chocan con letra 2,0.', full('despues_360_20_TuAbierto', 360)),
        ('Guía, paso 3 · 393 dp', '«En Contrarreloj ves tus marcas; en Avance, tu punto de partida. Arriba a la derecha, Tú: tus ajustes.» El hueco ilumina solo Contrarreloj y Avance. OJO: «Saltar la guía» queda sobre el botón «Tú».', full('despues_guia3_393_10', 393)),
    ]
    tu2 = [('Guía, paso 3 · 360 dp · letra 2,0', 'El globo de seis renglones cabe entero con «Listo».', full('despues_guia3_360_20', 360))]

    notes = [
        ('EL BOTÓN «TÚ»', 'Un círculo de plasticina: relleno crema (el de las letras de Hoy), borde de tinta de 2,5 dp y sombra dura de 3 dp; se ve de 44 dp y se toca en 48 dp (el mínimo), con 16 dp de margen derecho. Adentro, la inicial del nombre en mayúscula (Fredoka Bold de 20 sp, con tope de escala 1,3); sin nombre guardado, la persona de línea que tenía la barra. '
         'Lector: «Tú: tu perfil y ajustes», botón; tic háptico del kit al tocar; hundido (presionado) baja 3 dp sobre su sombra.'),
        ('SIEMPRE EN EL MISMO LUGAR (medido en `TuFlowTest`)', 'Es UN solo botón que pone `MainActivity` encima de las cuatro pestañas: mismo margen derecho (16 dp), misma altura (arriba, con su centro a 24 dp del borde de arriba: la línea de los títulos y del logotipo) y 48 × 48 dp en Hoy, Juegos, Contrarreloj y Avance. Queda a la vista aunque la página se deslice. '
         'Ninguna fila de título crece (0 dp): los títulos ya estaban centrados a 24 dp y el logotipo de Hoy sube 2 dp (4 dp de aire arriba y 4 hasta el saludo, antes 6 y 2: el saludo sigue a 48 dp). Los títulos de Juegos, Avance y Contrarreloj dejan libres 64 dp a la derecha: no chocan con el botón a 360 dp con letra 2,0.'),
        ('LA BARRA DE CUATRO', 'Hoy · Juegos · Contrarreloj · Avance. Con cuatro, a 360 dp los rótulos ya se dibujan con letra 1,3 (antes, con cinco, a 360 dp caían a 1,1 y a 393 dp a 1,2 o 1,3 según el teléfono): a 360 y a 393 dp, con letra 1,0 · 1,3 · 2,0, se dibujan con 1,00 · 1,30 · 1,30 y la barra mide 88,8 · 95,2 · 95,2 dp (Pixel 8 simulado, `NeuroNavBarTest`). Ninguna columna baja de 48 dp ni se parte una palabra.'),
        ('LA GUÍA', 'Sigue con tres pasos; el tercero ilumina las DOS columnas de la derecha (Contrarreloj y Avance) y dice lo que pidió Opus (101 letras; la prueba de «máximo ~100» subió a 102 para este texto, y `HoyGuideOverlayTest` mide que el globo lo muestre entero). '
         'Cosa que avisar: «Saltar la guía» (crema, arriba a la derecha) queda justo SOBRE el botón «Tú», que el velo oscurece: el texto dice «arriba a la derecha, Tú» y ahí está el chip. Se puede mover el chip o iluminar también el botón; no lo cambié.'),
    ]

    # ---------- alturas
    head = 74
    cab_h = [(20 + 118 + 12 + 118 + 12 + 14) for _ in items]
    bar_rows = [(26 + max(b[2].height for b in bars[i:i + 4]) + 12 + max(len(wrap(d0, b[1], sf, 393)) for b in bars[i:i + 4]) * 20 + 22) for i in range(0, len(bars), 4)]
    tu_cap = max(len(wrap(d0, c[1], sf, 393)) for c in tu) * 20
    tu_h = 26 + max(c[2].height for c in tu) + 12 + tu_cap + 30
    tu2_h = 26 + tu2[0][2].height + 12 + len(wrap(d0, tu2[0][1], sf, 360)) * 20 + 30
    note_h = sum(24 + len(wrap(d0, b, sf, sheet_w - 2 * MARGIN)) * 20 + 14 for _, b in notes)
    total = MARGIN + head + 30 + sum(cab_h) + 30 + sum(bar_rows) + 30 + tu_h + tu2_h + note_h + MARGIN
    sheet = Image.new('RGB', (sheet_w, total), NAVY)
    d = ImageDraw.Draw(sheet)
    d.text((MARGIN, MARGIN), 'Tarea 225 · «Tú» sale de la barra y pasa a un botón redondo arriba a la derecha', font=font(26), fill=SUN)
    sub = 'Capturas REALES del emulador (antes: el código de la Tarea 224; después: el de la 225), a tamaño real (1 dp = 1 px; las cabeceras, recortadas a 118 dp de alto). Ricardo: «Tú» sale de la barra y pasa a un botón redondo arriba a la derecha; la barra queda en 4 pestañas.'
    for i, line in enumerate(wrap(d, sub, sf, sheet_w - 2 * MARGIN)): d.text((MARGIN, MARGIN + 38 + i * 20), line, font=sf, fill=DIM)
    y = MARGIN + head
    d.text((MARGIN, y), 'LAS CUATRO CABECERAS · arriba, ahora; abajo, con el botón «Tú» (Hoy · Juegos · Contrarreloj · Avance)', font=font(17), fill=SUN); y += 30
    for (kind, label, antes, desp) in items:
        d.text((MARGIN, y), label, font=font(15), fill=WHITE); y += 20
        for row in (antes, desp):
            x = MARGIN
            for im in row:
                sheet.paste(im, (x, y)); d.rectangle((x - 1, y - 1, x + im.width, y + im.height), outline=LINE); x += 393 + GAP
            y += 118 + 12
        y += 14
    y += 0
    d.text((MARGIN, y), 'LA BARRA · cinco pestañas contra cuatro', font=font(17), fill=SUN); y += 30
    for i in range(0, len(bars), 4):
        x = MARGIN
        hrow = bar_rows[i // 4]
        for b in bars[i:i + 4]:
            d.text((x, y), b[0], font=font(15), fill=WHITE)
            sheet.paste(b[2], (x, y + 24)); d.rectangle((x - 1, y + 23, x + b[2].width, y + 24 + b[2].height), outline=LINE)
            for j, line in enumerate(wrap(d, b[1], sf, 393)): d.text((x, y + 24 + b[2].height + 10 + j * 20), line, font=sf, fill=DIM)
            x += 393 + GAP
        # (dos barras por fila de 393 + 393: se reparten en la mitad izquierda; la derecha queda para el texto)
        y += hrow
    d.text((MARGIN, y), 'TÚ ABIERTO Y LA GUÍA', font=font(17), fill=SUN); y += 30
    x = MARGIN
    for title, text, im in tu:
        d.text((x, y), title, font=font(15), fill=WHITE)
        sheet.paste(im, (x, y + 24)); d.rectangle((x - 1, y + 23, x + im.width, y + 24 + im.height), outline=LINE)
        for j, line in enumerate(wrap(d, text, sf, 393)): d.text((x, y + 24 + im.height + 10 + j * 20), line, font=sf, fill=DIM)
        x += 393 + GAP
    y += tu_h
    x = MARGIN
    for title, text, im in tu2:
        d.text((x, y), title, font=font(15), fill=WHITE)
        sheet.paste(im, (x, y + 24)); d.rectangle((x - 1, y + 23, x + im.width, y + 24 + im.height), outline=LINE)
        for j, line in enumerate(wrap(d, text, sf, 360)): d.text((x, y + 24 + im.height + 10 + j * 20), line, font=sf, fill=DIM)
    y += tu2_h
    for t, body in notes:
        d.text((MARGIN, y), t, font=font(16), fill=SUN)
        for i, line in enumerate(wrap(d, body, sf, sheet_w - 2 * MARGIN)): d.text((MARGIN, y + 24 + i * 20), line, font=sf, fill=WHITE)
        y += 24 + len(wrap(d, body, sf, sheet_w - 2 * MARGIN)) * 20 + 14
    sheet.save(OUT)
    print(OUT, sheet.size)


main()
