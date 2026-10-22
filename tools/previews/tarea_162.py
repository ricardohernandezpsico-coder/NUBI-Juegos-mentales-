# -*- coding: utf-8 -*-
"""Lámina «hoy | propuesta» de la Tarea 162, «La tarjeta de juego de la página del área, ordenada» (22-oct-2026; `auditoria-fable/spec-tarjeta-juego-avance.md`): `docs/previews/tarea-162.png`.
Ricardo, con 5 personas, vio las tarjetas «desordenadas» (`auditoria-fable/capturas-ricardo-22oct/2-tarjetas-juegos-desordenadas.jpg`): cuatro líneas con tres estilos, cortadas donde caen, con «Hoy:» en una y «En tu última partida:» en otra y «Jugado …» aparte.
Ahora cada tarjeta tiene CUATRO FILAS FIJAS al borde izquierdo (nombre; barra con «147 de 250 puntos»; «Tu nivel: …»; «Tu última partida, hoy: …»), dos pesos de letra y un solo color, y la fecha en el rótulo.
Todas las tomas son CAPTURAS REALES de Roborazzi a 393 dp (`Tarea162CapturasTest`: los seis casos del spec en una página, a letra 1,0, 1,3 y 2,0): «HOY» = la tarjeta del commit anterior (`src/test/screenshots/tarea-162-hoy/`) y «PROPUESTA» = `src/test/screenshots/tarea-162/`.
Uso:  python -I tools/previews/tarea_162.py      → docs/previews/tarea-162.png
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
PREV = os.path.join(ROOT, 'docs', 'previews'); OUT = os.path.join(PREV, 'tarea-162.png')
SHOTS = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots')
RICARDO = os.path.join(ROOT, '..', 'auditoria-fable', 'capturas-ricardo-22oct', '2-tarjetas-juegos-desordenadas.jpg')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
NAVY, SUN, DIM, CREAM, LIME = (20, 18, 63), (255, 203, 69), (200, 196, 235), (245, 235, 219), (155, 229, 100)
CW = 430; GAP = 30; MG = 40; COLS = 4
W = MG * 2 + COLS * CW + (COLS - 1) * GAP


def font(px, bold=True): return ImageFont.truetype(os.path.join(FONTS, 'fredoka.ttf' if bold else 'nunito_regular.ttf'), int(px))


def wrap(d, text, fnt, width):
    lines, cur = [], ''
    for word in text.split(' '):
        t = (cur + ' ' + word).strip()
        if d.textlength(t, font=fnt) <= width: cur = t
        else: lines.append(cur); cur = word
    if cur: lines.append(cur)
    return lines or ['']


def cards_crop(path, margin=28):
    """Recorta la zona de las tarjetas (las filas con mucho crema) de una toma entera de la página."""
    im = Image.open(path).convert('RGB'); px = im.load(); w, h = im.size
    rows = []
    for y in range(0, h, 4):
        cream = sum(1 for x in range(60, w - 60, 12) if abs(px[x, y][0] - 255) < 12 and abs(px[x, y][1] - 236) < 16 and abs(px[x, y][2] - 214) < 22)
        rows.append(cream / ((w - 120) / 12) > 0.5)
    ys = [i * 4 for i, v in enumerate(rows) if v]
    top, bottom = max(0, min(ys) - margin), min(h, max(ys) + margin)
    return im.crop((0, top, w, bottom))


def fit(im, w=CW): return im.resize((w, int(round(im.height * w / im.width))), Image.LANCZOS)


def main():
    probe = ImageDraw.Draw(Image.new('RGB', (10, 10)))
    F_T, F_H, F_P, F_C = font(58), font(34), font(25, False), font(23, False)
    intro = ('La prueba de Ricardo con 5 personas: en Avance, al abrir un área, las tarjetas de juego se veían desordenadas (cuatro líneas con tres estilos y colores, cortadas donde caen, «Hoy:» en una y «En tu última partida:» en otra, «Jugado …» aparte). '
             'Ahora cada tarjeta tiene cuatro filas fijas, siempre en el mismo orden y pegadas al borde izquierdo: (1) el emblema y el nombre; (2) la barra fina del kit con «147 de 250 puntos» a su derecha, o debajo si no cabe; (3) «Tu nivel: ya giras piezas de 6 bloques, en giros finos.»; '
             '(4) «Tu última partida, hoy: 234 grados por segundo. Es tu mejor marca.». Dos pesos de letra (negrita para el nombre, los puntos y los rótulos) y un solo color; la fecha va en el rótulo de la fila 4 («hace 6 semanas», no «hace 45 días»).')
    intro_l = wrap(probe, intro, F_P, W - 2 * MG)
    casos = ('Los seis casos del spec en una misma página: Acoplamiento (normal, con su mejor marca), Aterrizaje Lunar (frase larga; última partida de hace 4 días con su mejor marca), En la punta de la lengua (nombre largo y aún sin medir), '
             'Freno de Emergencia (su medida es un promedio y va en una oración aparte), Engranajes (sin partidas) y Rastro de luz (el nivel y la medida hablan de lo mismo: la última partida va sin «Tu mejor marca»). Con letra 2,0 los puntos pasan debajo de la barra en todas las tarjetas.')
    casos_l = wrap(probe, casos, F_P, W - 2 * MG)
    letras = (('letra 1,0', 'casos-letra10.png'), ('letra 1,3', 'casos-letra13.png'), ('letra 2,0', 'casos-letra20.png'))
    hoy = [(et, fit(cards_crop(os.path.join(SHOTS, 'tarea-162-hoy', f)))) for et, f in letras]
    nuevo = [(et, fit(cards_crop(os.path.join(SHOTS, 'tarea-162', f)))) for et, f in letras]
    ricardo = fit(Image.open(RICARDO).convert('RGB')) if os.path.exists(RICARDO) else None
    row1 = ([('lo que vio Ricardo (22-oct, letra 1,0)', ricardo)] if ricardo is not None else [('', None)]) + hoy
    row2 = [('', None)] + nuevo
    H = MG + 80 + 34 * len(intro_l) + 30 + 34 * len(casos_l) + 40
    for row in (row1, row2): H += 56 + max((im.height for _, im in row if im is not None), default=0) + 70
    sheet = Image.new('RGB', (W, H), NAVY); d = ImageDraw.Draw(sheet); y = MG
    d.text((MG, y), 'La tarjeta de juego de Avance, ordenada: hoy | propuesta (Tarea 162)', font=F_T, fill=SUN); y += 80
    for ln in intro_l: d.text((MG, y), ln, font=F_P, fill=CREAM); y += 34
    y += 12
    for ln in casos_l: d.text((MG, y), ln, font=F_P, fill=DIM); y += 34
    y += 40
    for titulo, color, row in (('HOY', DIM, row1), ('PROPUESTA', LIME, row2)):
        d.text((MG, y), titulo, font=F_H, fill=color); y += 56
        h = max((im.height for _, im in row if im is not None), default=0)
        for k, (et, im) in enumerate(row):
            if im is None: continue
            x = MG + k * (CW + GAP)
            sheet.paste(im, (x, y)); d.rectangle((x, y, x + im.width - 1, y + im.height - 1), outline=(70, 74, 130), width=1)
            d.text((x, y + im.height + 8), et if k == 0 and titulo == 'HOY' else (et if et else ''), font=F_C, fill=color)
        y += h + 70
    sheet = sheet.crop((0, 0, W, y)); sheet.save(OUT)
    print('ok', OUT, sheet.size)


if __name__ == '__main__':
    main()
