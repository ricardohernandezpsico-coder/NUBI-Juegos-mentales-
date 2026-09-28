# Radar: retoque para alejarse de la patente US 8,348,671 (28-sep; ver docs/nombre-marca-y-riesgos.md, sección 5).
# Maqueta PIL, NO el juego: arriba el Radar de hoy (paneles reales de docs/previews/radar.png), abajo la propuesta:
# 1) la nave de la misión se muestra ANTES de la ronda; 2) primero se pregunta DÓNDE estaba el astronauta;
# 3) después "¿Pasó tu nave por el centro?" con SÍ / NO (ya no se elige entre dos naves). La patente exige elegir la
# imagen del centro entre candidatas y, SOLO si se acertó, preguntar la ubicación: la propuesta no hace ninguna de las dos.
# Uso: python3 tools/previews/radar_retoque.py  ->  docs/previews/radar-retoque.png
import os, importlib.util
from PIL import Image, ImageDraw, ImageFilter
HERE = os.path.dirname(__file__)
spec = importlib.util.spec_from_file_location('prop', os.path.join(HERE, 'inicio_propuestas.py'))
P = importlib.util.module_from_spec(spec); spec.loader.exec_module(P)
INK, SUN, CREAM, WHITE, DIM, SOFT, LIME, CORAL, GRAPE = P.INK, P.SUN, P.CREAM, P.WHITE, P.DIM, P.SOFT, P.LIME, P.CORAL, P.GRAPE
FB = P.FB

SRC = Image.open(os.path.join(P.OUT, 'radar.png')).convert('RGBA')
X0 = [23, 587, 1151, 1716]; PW = 541; Y0, PH = 23, 960


def panel(i): return SRC.crop((X0[i], Y0, X0[i] + PW, Y0 + PH))


def font(sz, bold=True):
    from PIL import ImageFont
    return ImageFont.truetype(FB if bold else P.FS, sz)


def cover(im, box, src_y=None):
    """Tapa una franja con el cielo del panel: cada fila toma el color del borde izquierdo (sin estrellas)."""
    x0, y0, x1, y1 = box
    d = ImageDraw.Draw(im)
    for y in range(y0, y1):
        px = sorted(im.getpixel((x, y))[:3] for x in range(4, 16))[6]
        d.line([(x0, y), (x1, y)], fill=px + (255,))


def prompt(im, text, col=WHITE):
    cover(im, (20, 118, PW - 20, 172), 600)
    ImageDraw.Draw(im).text((PW / 2, 143), text, font=font(26), fill=col, anchor='mm')


def clay_button(d, box, fill, text, tcol=INK):
    x0, y0, x1, y1 = box
    d.rounded_rectangle([x0, y0 + 8, x1, y1 + 8], radius=26, fill=INK)
    d.rounded_rectangle(box, radius=26, fill=fill, outline=INK, width=5)
    d.text(((x0 + x1) / 2, (y0 + y1) / 2), text, font=font(34), fill=tcol, anchor='mm')


def ship_card(size):
    """Carta crema con el cohete del juego (sin la marca de acierto de la captura)."""
    k = 4; S2 = size * k
    c = Image.new('RGBA', (S2, S2 + 8 * k), (0, 0, 0, 0)); d = ImageDraw.Draw(c)
    d.rounded_rectangle([0, 8 * k, S2 - 1, S2 + 8 * k - 1], radius=26 * k, fill=INK)
    d.rounded_rectangle([0, 0, S2 - 1, S2 - 1], radius=26 * k, fill=CREAM, outline=INK, width=5 * k)
    rocket = SRC.crop((1285, 742, 1372, 828)).resize((int(S2 * 0.66), int(S2 * 0.66)), Image.LANCZOS)
    c.alpha_composite(rocket, (int(S2 * 0.17), int(S2 * 0.17)))
    return c.resize((size, size + 8), Image.LANCZOS)


def mission_panel():
    im = panel(0)
    dark = Image.new('RGBA', im.size, (6, 9, 36, 215)); im.alpha_composite(dark, (0, 0))
    d = ImageDraw.Draw(im)
    d.text((PW / 2, 190), 'Tu nave de rescate', font=font(34), fill=SUN, anchor='mm')
    card = ship_card(210)
    im.alpha_composite(card, (int(PW / 2 - card.width / 2), 280))
    for k, line in enumerate(['Si pasa por el centro,', 'dilo al final de la ronda.']):
        d.text((PW / 2, 560 + k * 38), line, font=font(26, False), fill=WHITE, anchor='mm')
    d.text((PW / 2, 700), 'Cambia cada 5 rondas', font=font(22, False), fill=DIM, anchor='mm')
    return im


def where_first():
    im = panel(2)
    cover(im, (40, 664, PW - 40, 852))
    return im


def did_it_pass():
    im = panel(2)
    cover(im, (40, 664, PW - 40, 852))
    prompt(im, '¿Pasó tu nave por el centro?')
    veil = Image.new('RGBA', im.size, (0, 0, 0, 0)); ImageDraw.Draw(veil).ellipse([45, 180, PW - 45, 600], fill=(6, 9, 36, 150))
    im.alpha_composite(veil)
    d = ImageDraw.Draw(im)
    card = ship_card(110)
    im.alpha_composite(card, (int(PW / 2 - 55), 330))
    clay_button(d, (60, 700, 255, 800), LIME, 'SÍ, PASÓ')
    clay_button(d, (285, 700, 480, 800), GRAPE, 'NO PASÓ')
    return im


def arrow(d, x, y):
    d.line([(x - 14, y), (x + 12, y)], fill=SUN, width=5)
    d.polygon([(x + 18, y), (x + 6, y - 9), (x + 6, y + 9)], fill=SUN)


def sheet():
    sc = 0.5; pw, ph = int(PW * sc), int(PH * sc); gap = 36
    W = 40 + 4 * pw + 3 * gap + 40; H = 1340
    im = P.sky(9).resize((W, H))
    d = ImageDraw.Draw(im)
    d.text((40, 36), 'Radar: retoque para no chocar con la patente', font=font(40), fill=WHITE)
    d.text((40, 92), 'Arriba, hoy. Abajo, la propuesta: el destello, la interferencia, la escalera del tiempo y la medida "tu vistazo" quedan igual.', font=font(22, False), fill=DIM)
    rows = [('HOY', [panel(0), panel(1), panel(2), panel(3)],
             ['Destello: nave al centro,', 'Interferencia', '¿Qué nave pasó? (2 naves)', 'Revelación'],
             ['astronauta en el borde', '', 'y después ¿dónde estaba?', '']),
            ('PROPUESTA', [mission_panel(), panel(0), where_first(), did_it_pass()],
             ['1. Tu nave, ANTES', '2. Destello + interferencia', '3. PRIMERO: ¿dónde?', '4. ¿Pasó tu nave? SÍ / NO'],
             ['(cada 5 rondas)', '(igual que hoy)', 'siempre, pase lo que pase', 'sin elegir entre naves'])]
    y = 150
    for title, panels, cap1, cap2 in rows:
        d.text((40, y), title, font=font(26), fill=SUN if title == 'PROPUESTA' else SOFT)
        y += 44
        for i, pn in enumerate(panels):
            x = 40 + i * (pw + gap)
            im.alpha_composite(pn.resize((pw, ph), Image.LANCZOS), (x, y))
            d = ImageDraw.Draw(im)
            d.text((x + pw / 2, y + ph + 16), cap1[i], font=font(21), fill=WHITE, anchor='ma')
            d.text((x + pw / 2, y + ph + 44), cap2[i], font=font(19, False), fill=DIM, anchor='ma')
            if i < 3: arrow(d, x + pw + gap / 2, y + ph / 2)
        y += ph + 96
    out = os.path.join(P.OUT, 'radar-retoque.png'); im.convert('RGB').save(out); print(out)


if __name__ == '__main__':
    sheet()
