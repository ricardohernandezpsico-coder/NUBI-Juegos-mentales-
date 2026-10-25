# -*- coding: utf-8 -*-
"""Lámina de la Tarea 174, «Avance: cuatro cuadros y letra Rubik» (`docs/previews/tarea-174.png`), con CAPTURAS REALES del emulador (API 36, 1080 × 2400) y de Roborazzi (393 × 873 dp); no son maquetas.
  1. La vista principal de Avance «hoy | propuesta» con letra 1,0, 1,3 y 2,0 (emulador; «hoy» es la app de la Tarea 173). La cuenta de la captura tiene un área SIN partidas (Lenguaje): dice «Aún sin medir» con la barra vacía.
  2. Lo que mide a 393 × 873 dp con letra 1,0 y 1,3 (Roborazzi): entran los cuatro cuadros sin deslizar, «Razonamiento» entra entero, y un zoom a los cuatro cuadros.
  3. Lo que se fue de la vista principal vive en la página del área: la etapa, lo que falta, la frase de la semana y «Tu mejor marca sigue en N puntos» (cuando bajó), con los títulos de la página en Rubik.
  4. La cuenta nueva (sin ninguna partida): queda como hoy, con el título «Avance» en Rubik.
Entradas: `app/src/test/screenshots/tarea-174-emulador/*.png` (`tools/emulador_ui.py`, solo el emulador; las siembra `emu_avance.py` del cuaderno de trabajo: puntos por área, mejores marcas) y `app/src/test/screenshots/tarea-174/` (`Tarea174CapturasTest`, solo al grabar).
Uso:  python -I tools/previews/tarea_174.py
"""
import os, re, sys, textwrap
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
EMU = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots', 'tarea-174-emulador')
ROB = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots', 'tarea-174')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-174.png')
NAVY, SUN, DIM, CREAM, OK = (20, 18, 63), (255, 203, 69), (200, 196, 235), (245, 235, 219), (110, 220, 130)
GAP = 36


def font(px, bold=True, rubik=False):
    return ImageFont.truetype(os.path.join(FONTS, 'rubik_bold.ttf' if rubik else ('fredoka.ttf' if bold else 'nunito_regular.ttf')), int(px))


def load(path): return Image.open(path).convert('RGB') if os.path.exists(path) else None


def fit_w(im, w): return im.resize((w, round(im.height * w / im.width)), Image.LANCZOS)


def wrap(d, x, y, text, f, fill, width_chars, step):
    for line in textwrap.wrap(text, width_chars):
        d.text((x, y), line, font=f, fill=fill); y += step
    return y


def build():
    w = 430; pair = 2 * w + 14
    W = 2 * GAP + 3 * pair + 2 * 44
    h_ph = round(2400 * w / 1080)
    medidas = {}
    mp = os.path.join(ROB, 'medidas.txt')
    if os.path.exists(mp):
        for line in open(mp, encoding='utf-8').read().splitlines():
            m = re.match(r'vista-(\d_\d): (.*)', line)
            if m: medidas[m.group(1)] = m.group(2)
    H = 260 + (h_ph + 100) + 160 + (round(873 * w / 393) + 140) + 160 + (round(h_ph * 0.62) + 190) + 160 + (h_ph + 100) + 40
    sheet = Image.new('RGB', (W, H), NAVY); d = ImageDraw.Draw(sheet)
    d.text((GAP, 24), 'Avance: cuatro cuadros y letra Rubik (Tarea 174)', font=font(66, rubik=True), fill=SUN)
    y = 118
    for line in ('Capturas reales (emulador API 36 y Roborazzi). Decisión de Ricardo sobre un boceto de Opus, tras decir 5 personas que la lista de áreas era «monótona»: las cuatro áreas en 2 × 2 y la letra Rubik Bold en los títulos de Avance, a prueba.',
                 'Cada cuadro es una losa de arcilla que se toca y abre la página del área, con tres cosas siempre en el mismo lugar: el nombre (Rubik), los puntos («668» y debajo «de 1000 puntos»; un área sin partidas dice «Aún sin medir») y una barra del color del área sobre su riel.'):
        d.text((GAP, y), line, font=font(26, False), fill=DIM); y += 36
    y = 220
    # ---- 1. la vista principal
    d.text((GAP, y), '1 · La vista principal, hoy | propuesta, con letra 1,0, 1,3 y 2,0', font=font(44), fill=SUN)
    y += 74
    for k, sc in enumerate(('1_0', '1_3', '2_0')):
        x = GAP + 44 + k * (pair + 44)
        for j, (pre, cap) in enumerate((('hoy', 'hoy'), ('nueva-vista', 'propuesta'))):
            im = load(os.path.join(EMU, f'{pre}-{sc}.png'))
            if im: sheet.paste(fit_w(im, w), (x + j * (w + 14), y))
            d.text((x + j * (w + 14), y + h_ph + 8), f'letra {sc.replace("_", ",")} · {cap}', font=font(28), fill=CREAM if j else DIM)
    y += h_ph + 80
    # ---- 2. lo que mide
    d.text((GAP, y), '2 · Lo que mide a 393 × 873 dp (Roborazzi)', font=font(44), fill=SUN)
    y += 64
    ph2 = round(873 * w / 393)
    for k, sc in enumerate(('1_0', '1_3')):
        im = load(os.path.join(ROB, f'vista-{sc}.png'))
        x = GAP + 44 + k * (w + 20)
        if im: sheet.paste(fit_w(im, w), (x, y))
        d.text((x, y + ph2 + 6), f'393 × 873 dp · letra {sc.replace("_", ",")}', font=font(26), fill=CREAM)
    xt = GAP + 44 + 2 * (w + 20) + 20
    yy = y
    for sc in ('1_0', '1_3'):
        if sc in medidas:
            d.text((xt, yy), f'Letra {sc.replace("_", ",")}', font=font(30), fill=SUN); yy += 40
            yy = wrap(d, xt, yy, medidas[sc].replace('vista-' + sc, '').strip(), font(25, False), CREAM, 78, 33) + 14
    yy = wrap(d, xt, yy, 'Con letra 2,0 va un cuadro por fila (cada uno a todo el ancho) y la lista se desliza: el nombre sigue en una línea, sin partirse (ver la fila de arriba, a la derecha).', font(25, False), DIM, 78, 33) + 10
    yy = wrap(d, xt, yy, 'El nombre toma el mayor tamaño de 20 a 16 sp al que el nombre más largo cabe entero: 20 sp con letra 1,0; 16 sp con letra 1,3 (nunca menos de 16 sp). Los cuatro cuadros usan el mismo tamaño. Con letra mayor que 1,3, o si «Razonamiento» no cupiera a 16 sp en media fila (un teléfono de 360 dp con letra 1,3), va un cuadro por fila.', font(25, False), DIM, 78, 33)
    y += ph2 + 70
    # ---- 3. la página del área
    d.text((GAP, y), '3 · Lo que se fue de la vista principal vive en la página del área', font=font(44), fill=SUN)
    y += 64
    notas = [
        ('hoy-pagina-arriba', 'hoy · Memoria'),
        ('nueva-vista-pagina-arriba', 'propuesta · Memoria: título en Rubik, etapa, barra de las 5 etapas, lo que falta y la semana'),
        ('bajada-pagina-arriba', 'propuesta · Razonamiento cuando bajó: «Tu mejor marca sigue en N puntos» vive aquí'),
        ('lenguaje-pagina-arriba', 'propuesta · Lenguaje sin partidas (0 puntos): «Aún sin medir» y su invitación'),
    ]
    cw = (W - 2 * GAP - 3 * 24) // 4
    crop_h = 0
    for k, (name, cap) in enumerate(notas):
        im = load(os.path.join(EMU, f'{name}.png'))
        x = GAP + k * (cw + 24)
        if im:
            p = fit_w(im, cw); ch = round(p.height * 0.62); crop_h = ch
            sheet.paste(p.crop((0, 0, cw, ch)), (x, y))
        wrap(d, x, y + crop_h + 8, cap, font(24, False), CREAM if k else DIM, 36, 30)
    y += crop_h + 150
    # ---- 4. la cuenta nueva
    d.text((GAP, y), '4 · La cuenta nueva (sin ninguna partida): queda como hoy, con el título en Rubik', font=font(44), fill=SUN)
    y += 64
    for j, (name, cap) in enumerate((('hoy-nueva-1_0', 'hoy'), ('nueva-cuenta-1_0', 'propuesta'))):
        im = load(os.path.join(EMU, f'{name}.png'))
        x = GAP + 44 + j * (w + 14)
        if im: sheet.paste(fit_w(im, w), (x, y))
        d.text((x, y + h_ph + 8), f'cuenta nueva · {cap}', font=font(28), fill=CREAM if j else DIM)
    xt = GAP + 44 + 2 * (w + 14) + 40
    d.text((xt, y), 'Dónde vive cada cosa que se fue de la fila', font=font(34), fill=SUN); yy = y + 54
    for linea in ('La etapa («Etapa Experto»): en la página, junto a los puntos («668 puntos de 1000 · Etapa Experto») y en su barra de las 5 etapas con sus nombres.',
                  'Lo que falta («Te faltan 132 puntos para Maestro»): en la página, bajo la barra de las 5 etapas.',
                  'La frase de la semana («Esta semana sumaste 52 puntos» / «te mantuviste» / «bajaste N puntos»): en la página, en la misma frase de lo que falta.',
                  '«Tu mejor marca sigue en N puntos» (cuando bajó): estaba SOLO en la fila; ahora está en esa misma frase de la página (`AreaPoints.toNextSentence`, con prueba).',
                  'Un área sin partidas: antes «Todavía sin puntos: juega un juego de Lenguaje» en la fila; ahora «Aún sin medir» en el cuadro y, en la página, «Todavía sin puntos en Lenguaje: cada juego suma desde su primera partida a tu medida».',
                  'El lector de pantalla lee cada cuadro entero: «Memoria, 668 de 1000 puntos» o «Lenguaje, aún sin medir».'):
        yy = wrap(d, xt, yy, '·  ' + linea, font(25, False), CREAM, 70, 33) + 16
    sheet = sheet.crop((0, 0, W, y + h_ph + 60))
    sheet.save(OUT, optimize=True); print('lámina:', OUT, sheet.size, os.path.getsize(OUT) // 1024, 'KB')


if __name__ == '__main__':
    build()
