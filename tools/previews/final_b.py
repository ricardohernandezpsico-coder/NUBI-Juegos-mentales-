# -*- coding: utf-8 -*-
"""Hoja «hoy | propuesta» del final de partida «B» (Tarea 146; `auditoria-fable/spec-final-B.md` §6): capturas REALES de Roborazzi, no réplicas.
Las tomas las hace `FinalBCapturesTest` (cada pantalla ENTERA, en una ventana de 393 × 2000 dp a 3 px por dp): PROPUESTA con el código de ahora y HOY con el del commit anterior
(la misma prueba en un `git worktree` de ese commit). Este script recorta cada toma a su contenido (la última fila que no es el fondo + los 32 dp de relleno de abajo), la baja a 1 px por dp y la guarda en `docs/previews/final-b/{hoy,propuesta}/`;
la hoja sale de esas copias (así se rearma sin Gradle).
Uso:
  python -I tools/previews/final_b.py prepare hoy <carpeta con las tomas del commit anterior>
  python -I tools/previews/final_b.py prepare propuesta <carpeta con las tomas de ahora>      (app/src/test/screenshots/final-b)
  python -I tools/previews/final_b.py                                                          → docs/previews/final-B.png
"""
import json, os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
PREV = os.path.join(ROOT, 'docs', 'previews'); STORE = os.path.join(PREV, 'final-b'); OUT = os.path.join(PREV, 'final-B.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
NAVY, SUN, DIM, CREAM, LIME = (20, 18, 63), (255, 203, 69), (200, 196, 235), (245, 235, 219), (155, 229, 100)
BG = (5, 8, 35)                      # el fondo plano de las pruebas, #050823
SRC_PX_PER_DP = 3                    # xxhdpi
KEEP_PX_PER_DP = 1                   # lo que se guarda en docs/ (la hoja usa 0,8: no hace falta más)
SHEET_PX_PER_DP = 0.80               # la escala común de la hoja: TODAS las pantallas a la misma
BOTTOM_DP = 32                       # el relleno de abajo de la pantalla de resultado

# (archivo, título de la columna, lo que muestra)
COLUMNS = [
    ('final-b-rastro-primera-1.0', 'Rastro de luz · primera partida', 'playsBefore 0 y sin historial: la frase dice que la próxima te comparas contigo y «¿Por qué importa?» está a la vista'),
    ('final-b-rastro-historial-1.0', 'Rastro de luz · con historial', '5 partidas comparables: la frase trae tu promedio y tu mejor; «¿Por qué importa?» ya vive en el detalle'),
    ('final-b-freno-historial-1.0', 'Freno de Emergencia · con historial', 'la cifra es una zona; la unidad dice «tu freno, en 5 partidas»'),
    ('final-b-punta-primera-1.0', 'En la punta de la lengua · primera partida', 'un «N de M»: «5 de 8 palabras · por tu cuenta»'),
    ('final-b-rastro-historial-1.3', 'Rastro de luz · con historial, letra 1,3', 'el botón principal sigue a la vista con la letra grande'),
    ('final-b-ficha-rastro', 'La ficha de Rastro de luz', 'ahora trae «Qué pones en juego» con su fuente (antes, lo de «¿Por qué importa?» iba al pie de cada final)'),
]


def font(px, bold=True): return ImageFont.truetype(os.path.join(FONTS, 'fredoka.ttf' if bold else 'nunito_regular.ttf'), int(px))


def crop_to_content(im, is_screen):
    """Recorta una toma ENTERA a su contenido: la última fila que no es el fondo más el relleno de abajo (las fichas ya vienen del alto justo)."""
    im = im.convert('RGB')
    if not is_screen: return im
    px = im.load(); w, h = im.size
    last = 0
    for y in range(h - 1, -1, -1):
        row_has = False
        for x in range(0, w, 3):
            r, g, b = px[x, y]
            if abs(r - BG[0]) > 10 or abs(g - BG[1]) > 10 or abs(b - BG[2]) > 10: row_has = True; break
        if row_has: last = y; break
    return im.crop((0, 0, w, min(h, last + 1 + BOTTOM_DP * SRC_PX_PER_DP)))


def prepare(which, src):
    out = os.path.join(STORE, which); os.makedirs(out, exist_ok=True)
    heights = {}
    # se guardan TODAS las tomas (las 12 de los tres juegos y la ficha), aunque la hoja use solo 5 + 1
    names = sorted(f[:-4] for f in os.listdir(src) if f.startswith('final-b-') and f.endswith('.png'))
    for name in names:
        im = Image.open(os.path.join(src, name + '.png'))
        im = crop_to_content(im, is_screen='ficha' not in name)
        dp = im.height / SRC_PX_PER_DP
        k = KEEP_PX_PER_DP / SRC_PX_PER_DP
        im = im.resize((round(im.width * k), round(im.height * k)), Image.LANCZOS)
        im.save(os.path.join(out, name + '.png'), optimize=True)
        heights[name] = round(dp)
    json.dump(heights, open(os.path.join(out, 'alto-dp.json'), 'w', encoding='utf-8'), indent=1, sort_keys=True)
    print(which, len(names), 'tomas →', out)


def wrap(d, text, fnt, width):
    lines, cur = [], ''
    for word in text.split(' '):
        t = (cur + ' ' + word).strip()
        if d.textlength(t, font=fnt) <= width: cur = t
        else: lines.append(cur); cur = word
    if cur: lines.append(cur)
    return lines


def sheet():
    k = SHEET_PX_PER_DP / KEEP_PX_PER_DP
    tiles = []
    for name, title, sub in COLUMNS:
        pair = []
        for which in ('hoy', 'propuesta'):
            im = Image.open(os.path.join(STORE, which, name + '.png')).convert('RGB')
            dp = json.load(open(os.path.join(STORE, which, 'alto-dp.json'), encoding='utf-8'))[name]
            pair.append((im.resize((round(im.width * k), round(im.height * k)), Image.LANCZOS), dp))
        tiles.append((title, sub, pair))
    GAP, COL_GAP, MARGIN = 8, 40, 30
    w_one = tiles[0][2][0][0].width
    col_w = 2 * w_one + GAP
    probe = ImageDraw.Draw(Image.new('RGB', (10, 10)))
    SUBF = font(17, False)
    sub_h = max(len(wrap(probe, t[1], SUBF, col_w)) for t in tiles) * 23
    tallest = max(im.height for t in tiles for im, _ in t[2])
    head = 150
    width = MARGIN * 2 + len(tiles) * col_w + (len(tiles) - 1) * COL_GAP
    height = head + 62 + tallest + 44 + sub_h + 60
    sheet_im = Image.new('RGB', (width, height), NAVY); d = ImageDraw.Draw(sheet_im)
    d.text((MARGIN, 22), 'El final de partida «B» (Tarea 146): hoy | propuesta', font=font(48), fill=SUN)
    d.text((MARGIN, 86), 'Capturas reales de Roborazzi, cada pantalla ENTERA a 393 dp de ancho y a la misma escala (el alto en dp, bajo cada una). HOY = el commit anterior; PROPUESTA = el de ahora.', font=font(20, False), fill=DIM)
    d.text((MARGIN, 114), 'Primera partida = playsBefore 0; con historial = 5 partidas comparables.', font=font(20, False), fill=DIM)
    y0 = head
    for c, (title, sub, pair) in enumerate(tiles):
        x = MARGIN + c * (col_w + COL_GAP)
        d.text((x, y0), title, font=font(24), fill=CREAM)
        for j, ((im, dp), tag, col) in enumerate(zip(pair, ('HOY', 'PROPUESTA'), (DIM, LIME))):
            xx = x + j * (w_one + GAP)
            sheet_im.paste(im, (xx, y0 + 62)); d.rectangle((xx, y0 + 62, xx + im.width - 1, y0 + 62 + im.height - 1), outline=(70, 74, 130), width=1)
            d.text((xx, y0 + 62 + im.height + 6), tag, font=font(22), fill=col)
            d.text((xx, y0 + 62 + im.height + 32), '%s dp de alto' % format(dp, ',').replace(',', '.'), font=font(18, False), fill=DIM)
        ty = y0 + 62 + tallest + 62
        for line in wrap(d, sub, SUBF, col_w): d.text((x, ty), line, font=SUBF, fill=DIM); ty += 23
    d.text((MARGIN, height - 36), '20-oct-2026 · HOY = commit 39b1260 (T145) · PROPUESTA = el commit de la Tarea 146 · docs/previews/final-b/{hoy,propuesta}/ · tools/previews/final_b.py', font=font(18, False), fill=DIM)
    sheet_im.save(OUT, optimize=True); print('hoja:', OUT, sheet_im.size, os.path.getsize(OUT) // 1024, 'KB')


if __name__ == '__main__':
    if len(sys.argv) >= 4 and sys.argv[1] == 'prepare': prepare(sys.argv[2], sys.argv[3])
    else: sheet()
