# -*- coding: utf-8 -*-
"""Lámina de la Tarea 233 (4-nov-2026): Carga exacta de 5 a 8 niveles con los puntos anclados a los 5 de antes (docs/alargar-dificultad.md §B.1 y §B.2).
Qué muestra: (1) la página de Razonamiento de Avance de la MISMA persona, antes (código de la Tarea 230) y ahora, con los mismos puntos y el mismo nivel, y la de alguien que ya llegó al nivel 7 (sus puntos están en el tope y la frase dice el nivel nuevo), a 393 dp con letra 1,0 y a 360 dp con 2,0;
(2) una tabla con tres personas de ejemplo y dos que ya jugaron en los niveles nuevos; (3) si están, las capturas REALES de Carga exacta en los niveles 6, 7 y 8 (smoke de Unity con `NUBI_EDITOR_RATING`).
Las capturas de Avance son REALES de Robolectric/Roborazzi por la pantalla de verdad (`Tarea233CapturasTest`, xxhdpi: 3 px por dp), grabadas una vez sobre el código de antes y otra sobre el de ahora, y la tabla sale del código (`Tarea233TablaCapturaTest`); todo está en `unity/test-results/t233/`
(fuera de git: `antes_*.png`, `despues_*.png`, `tabla.txt` y, de Unity, `carga_nivel6.png` a `carga_nivel8.png`).
Uso:  python -I tools/previews/tarea_233.py      → docs/previews/tarea-233.png
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SHOTS = os.path.join(ROOT, 'unity', 'test-results', 't233')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-233.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
NAVY, SUN, DIM, LINE, WHITE, LIME = (20, 18, 63), (255, 230, 166), (200, 196, 235), (90, 86, 160), (240, 244, 255), (176, 235, 110)
K = 3.0                                                       # los PNG son xxhdpi: 3 px por dp; en la lámina 1 dp = 1 px
COL, GAP, MARGIN = 393, 20, 24
W = MARGIN * 2 + 3 * COL + 2 * GAP


def font(px, bold=True): return ImageFont.truetype(os.path.join(FONTS, 'fredoka.ttf' if bold else 'nunito_regular.ttf'), int(px))


def crop(name, top, bottom):
    im = Image.open(os.path.join(SHOTS, name + '.png')).convert('RGB')
    c = im.crop((0, int(top * K), im.width, min(int(bottom * K), im.height)))
    return c.resize((int(round(c.width / K)), int(round(c.height / K))), Image.LANCZOS)


def stack(parts):
    h = sum(p.height for p in parts) + 6 * (len(parts) - 1)
    out = Image.new('RGB', (max(p.width for p in parts), h), (255, 255, 255)); y = 0
    for p in parts: out.paste(p, (0, y)); y += p.height + 6
    return out


def wrap(d, text, f, width):
    lines, cur = [], ''
    for word in text.split(' '):
        t = (cur + ' ' + word).strip()
        if d.textlength(t, font=f) <= width: cur = t
        else: lines.append(cur); cur = word
    lines.append(cur)
    return lines


def comma(x, nd=3): return ('%.*f' % (nd, float(x))).replace('.', ',')


def main():
    sf, tf, hf, cf = font(15, bold=False), font(19), font(15), font(14, bold=False)
    heads = ['ANTES · Tarea 230 (Carga con 5 niveles)', 'AHORA · la misma persona (4,6 de 8)', 'AHORA · tras llegar al nivel 7']
    names = ['antes_razonamiento_4_6', 'despues_razonamiento_4_6', 'despues_razonamiento_nivel7']
    big = []
    # 393 dp con letra 1,0: del título al tarjeta de Carga exacta
    c1 = [crop(n + '_393_10', 0, 1000) for n in names]
    # 360 dp con letra 2,0: la cabecera y, aparte, la tarjeta de Carga exacta
    c2 = [stack([crop(n + '_360_20', 0, 330), crop(n + '_360_20', 1380, 1830)]) for n in names]
    rows = []
    try:
        tabla = [l.split('|') for l in open(os.path.join(SHOTS, 'tabla.txt'), encoding='utf-8').read().splitlines() if l.strip()]
    except OSError:
        tabla = []
    carga = [os.path.join(SHOTS, 'carga_nivel%d.png' % n) for n in (6, 7, 8)]
    have_carga = all(os.path.exists(p) for p in carga)
    notes = ('Lo que NO cambia: quien jugaba Carga exacta sigue en el mismo nivel y con los mismos puntos. Su rating pasa de 4,6 de 5 a 4,6 de 8 (`ddaRating × 5/8`, Migration(18, 19) de Room; las medidas de star_measures las traduce la app una vez al arrancar), '
             'los puntos se leen sobre los 5 niveles de antes con tope 1 (`Skill.progress` anclado a `pointsLevels`), así que quien estaba en el tope sigue con los 250 puntos del juego y los niveles 6 a 8 NO suman. '
             'Lo que cambia: la frase «Tu nivel» de Avance sale de la escalera entera (`Skill.ladderProgress`), así que llega a los niveles nuevos; un jugador nuevo parte donde partía (0,45 × 5/8) y el tope de bajada se mide en puntos.')
    # ---- altura
    tmp = Image.new('RGB', (W, 10)); td = ImageDraw.Draw(tmp)
    h = MARGIN + 100
    h += 34 + 24 + c1[0].height + 40 + 24 + c2[0].height + 30
    h += 34 + (len(tabla) + 1) * 34 + 80
    if have_carga: h += 34 + 24 + 900 + 30
    h += len(wrap(td, notes, sf, W - 2 * MARGIN)) * 20 + 60
    sheet = Image.new('RGB', (W, h), NAVY); d = ImageDraw.Draw(sheet)
    d.text((MARGIN, MARGIN), 'Tarea 233 · Carga exacta de 5 a 8 niveles, sin perder nada', font=font(28), fill=SUN)
    y = MARGIN + 42
    sub = 'Base común de «alargar la dificultad» (docs/alargar-dificultad.md §B.1) con su primer caso. Capturas REALES de las pruebas (Roborazzi, la pantalla de Avance de verdad): antes y ahora, la misma persona con los mismos puntos.'
    for line in wrap(d, sub, sf, W - 2 * MARGIN):
        d.text((MARGIN, y), line, font=sf, fill=DIM); y += 20
    y += 14
    # ---- 1. páginas de Razonamiento
    d.text((MARGIN, y), '1 · Página de Razonamiento en Avance', font=tf, fill=WHITE); y += 34
    for label, imgs, ty in (('393 dp · letra 1,0 (del título a la tarjeta de Carga exacta)', c1, 0), ('360 dp · letra 2,0 (la cabecera y la tarjeta de Carga exacta)', c2, 0)):
        d.text((MARGIN, y), label, font=hf, fill=DIM); y += 24
        for i, im in enumerate(imgs):
            x = MARGIN + i * (COL + GAP); ox = x + (COL - im.width) // 2
            sheet.paste(im, (ox, y)); d.rectangle((ox - 1, y - 1, ox + im.width, y + im.height), outline=LINE)
            d.text((x + 4, y + im.height + 4), heads[i], font=hf, fill=LIME if i else DIM)
        y += imgs[0].height + 40
    # ---- 2. la tabla
    d.text((MARGIN, y), '2 · Tres personas de ejemplo y dos que ya jugaron en los niveles nuevos (sale del código)', font=tf, fill=WHITE); y += 34
    cols = [('Persona', MARGIN), ('Rating (antes a ahora)', MARGIN + 230), ('Nivel (antes a ahora)', MARGIN + 430), ('Puntos (antes a ahora)', MARGIN + 620), ('Lo que dice Avance ahora', MARGIN + 800)]
    for name, x in cols: d.text((x, y), name, font=hf, fill=SUN)
    y += 30
    d.line((MARGIN, y - 4, W - MARGIN, y - 4), fill=LINE)
    for r in tabla:
        who, old_r, new_r, old_l, old_n, lvl, n, old_p, pts, phrase = r[:10]
        rating = (comma(old_r) + ' a ' + comma(new_r)) if old_r else 'sin dato a ' + comma(new_r)
        level = ('%s de 5 a %s de 8' % (old_l, lvl)) if old_l else 'nuevo: %s de 8' % lvl
        points = ('%s a %s' % (old_p, pts)) if old_p else 'nuevo: %s' % pts
        for (name, x), text in zip(cols, (who, rating, level, points, phrase)):
            d.text((x, y), text, font=cf if name == 'Lo que dice Avance ahora' else sf, fill=WHITE)
        y += 34
    y += 20
    # ---- 3. Carga en los niveles nuevos (Unity)
    if have_carga:
        d.text((MARGIN, y), '3 · Carga exacta en los niveles 6, 7 y 8 (capturas REALES del juego en el Editor de Unity)', font=tf, fill=WHITE); y += 34
        for i, p in enumerate(carga):
            im = Image.open(p).convert('RGB'); k = 900.0 / im.height; im = im.resize((int(im.width * k), 900), Image.LANCZOS)
            x = MARGIN + i * (COL + GAP); ox = x + (COL - im.width) // 2
            sheet.paste(im, (ox, y)); d.rectangle((ox - 1, y - 1, ox + im.width, y + 900), outline=LINE)
        y += 900 + 14
        for i, t in enumerate(('Nivel 6: cinco celdas, se juntan 4 o las 5', 'Nivel 7: casi siempre las 5 juntas', 'Nivel 8: siempre las 5 juntas, números hasta 14')):
            d.text((MARGIN + i * (COL + GAP) + 4, y - 20), t, font=hf, fill=LIME)
        y += 20
    for line in wrap(d, notes, sf, W - 2 * MARGIN):
        d.text((MARGIN, y), line, font=sf, fill=WHITE); y += 20
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    sheet.crop((0, 0, W, y + MARGIN)).save(OUT, optimize=True)
    print('listo', OUT, sheet.crop((0, 0, W, y + MARGIN)).size)


main()
