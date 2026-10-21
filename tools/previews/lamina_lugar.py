# -*- coding: utf-8 -*-
"""Hoja «hoy | propuesta» del REDISEÑO DE LA LÁMINA DEL LUGAR (Tarea 147, 21-oct-2026; `auditoria-fable/spec-lamina-lugar.md`): la lámina cabe sin deslizar.
Las dos columnas son capturas REALES de Roborazzi (`LaminaLugarCapturesTest`, la misma prueba con el código de HOY —un `git worktree` del commit 630fc96— y con el de ahora): el Observatorio (4 juegos) y la Antena (5),
en 393 × 873 y 360 × 780, con letra 1,0 y 1,3. Debajo de cada par van las medidas (de `medidas.txt`): dónde empieza la lámina, cuánto mide y cuánto había que deslizar para verla entera (0 = cabe).
Las capturas viven en `docs/previews/lamina-lugar/{hoy,propuesta}/` (se regeneran con `tools/previews/lamina_lugar_capturas.sh`).
Uso:  python -I tools/previews/lamina_lugar.py      → docs/previews/lamina-lugar.png
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
PREV = os.path.join(ROOT, 'docs', 'previews'); SRC = os.path.join(PREV, 'lamina-lugar'); OUT = os.path.join(PREV, 'lamina-lugar.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
NAVY, SUN, DIM, CREAM, LIME, CORAL = (20, 18, 63), (255, 203, 69), (200, 196, 235), (245, 235, 219), (155, 229, 100), (255, 140, 120)
W, GAP, PAIR_GAP, MARGIN = 300, 8, 36, 30
SIZES = ((393, 873), (360, 780))
PLACES = (('observatorio', 'Observatorio (4 juegos)'), ('antena', 'Antena (5 juegos)'))
LETTERS = (('1.0', 'letra 1,0'), ('1.3', 'letra 1,3'))


def font(px, bold=True): return ImageFont.truetype(os.path.join(FONTS, 'fredoka.ttf' if bold else 'nunito_regular.ttf'), int(px))


def medidas(which):
    out = {}
    with open(os.path.join(SRC, which, 'medidas.txt'), encoding='utf-8') as f:
        for line in f:
            line = line.strip()
            if not line: continue
            name, top, h, slack = line.split('|')
            out[name] = (float(top), float(h), float(slack))                       # el último de cada nombre gana (si se repitió la corrida)
    return out


def load(which, name):
    return Image.open(os.path.join(SRC, which, name + '.png')).convert('RGB')


def fit(im):
    return im.resize((W, round(im.height * W / im.width)), Image.LANCZOS)


def verdict(m, h_screen):
    top, h, slack = m
    scroll = 'cabe sin deslizar' if slack < 1 else 'hay que deslizar %d dp' % round(slack)
    return 'empieza a %d dp de arriba · mide %d dp · %s' % (round(top), round(h), scroll), slack < 1


hoy, hoy_m = None, medidas('hoy'); nuevo_m = medidas('propuesta')
tiles = []
for place, ptitle in PLACES:
    row = []
    for (w, h) in SIZES:
        for letter, ltitle in LETTERS:
            name = 'lamina-%s-%dx%d-%s' % (place, w, h, letter)
            a, b = fit(load('hoy', name)), fit(load('propuesta', name))
            va, oka = verdict(hoy_m[name], h); vb, okb = verdict(nuevo_m[name], h)
            row.append((('%s · %d × %d · %s' % (ptitle.split(' (')[0], w, h, ltitle)), a, b, (va, oka), (vb, okb)))
    tiles.append(row)

COLS = len(tiles[0]); pair_w = 2 * W + GAP
probe = ImageDraw.Draw(Image.new('RGB', (10, 10))); SUBF = font(19, False)


def wrap(text, fnt, width):
    lines, cur = [], ''
    for word in text.split(' '):
        t = (cur + ' ' + word).strip()
        if probe.textlength(t, font=fnt) <= width // 2: cur = t
        else: lines.append(cur); cur = word
    if cur: lines.append(cur)
    return lines


row_h = [max(max(t[1].height, t[2].height) for t in row) + 34 + 40 + 3 * 26 + 24 for row in tiles]
width = MARGIN * 2 + COLS * pair_w + (COLS - 1) * PAIR_GAP
height = 190 + sum(row_h) + 20
sheet = Image.new('RGB', (width, height), NAVY); d = ImageDraw.Draw(sheet)
d.text((MARGIN, 22), 'La lámina del lugar cabe sin deslizar (Tarea 147): hoy | propuesta', font=font(50), fill=SUN)
d.text((MARGIN, 88), 'Capturas reales de Roborazzi (Observatorio con 4 juegos, Antena con 5; 393 × 873 y 360 × 780; letra 1,0 y 1,3). HOY = el commit 630fc96; PROPUESTA = las de ahora.', font=font(20, False), fill=DIM)
d.text((MARGIN, 114), 'Una sola vista: Nubi a la izquierda del arco con su globo, las 4-5 filas de juego (emblema de 48 dp, nombre, «cuándo» y chip «Nubi sugiere») y el botón «Jugar» con el emblema del juego sugerido.', font=font(20, False), fill=DIM)
d.text((MARGIN, 140), 'Con letra grande en pantalla chica el globo cede (Nubi queda callado junto a la línea de avance) y después los nombres de las etapas; nunca se parte una palabra ni se pierde un dato. Mínimo 56 dp de cielo arriba.', font=font(20, False), fill=DIM)
y = 190
for r, row in enumerate(tiles):
    for c, (title, a, b, va, vb) in enumerate(row):
        x = MARGIN + c * (pair_w + PAIR_GAP)
        top = max(a.height, b.height)
        for k, (im, tag, col) in enumerate(((a, 'HOY', DIM), (b, 'PROPUESTA', LIME))):
            xx = x + k * (W + GAP)
            sheet.paste(im, (xx, y)); d.rectangle((xx, y, xx + im.width - 1, y + im.height - 1), outline=(70, 74, 130), width=1)
            d.text((xx, y + top + 4), tag, font=font(22), fill=col)
        ty = y + top + 34
        d.text((x, ty), title, font=font(26), fill=CREAM)
        ty += 40
        for k, (txt, ok) in enumerate((va, vb)):
            xx = x + k * (W + GAP)
            yy = ty
            for part in wrap(txt, SUBF, 2 * W + GAP):
                d.text((xx, yy), part, font=SUBF, fill=LIME if ok else CORAL); yy += 26
    y += row_h[r]
sheet.save(OUT, optimize=True); print('hoja:', OUT, sheet.size, os.path.getsize(OUT) // 1024, 'KB')
