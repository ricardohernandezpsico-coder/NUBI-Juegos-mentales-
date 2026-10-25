# -*- coding: utf-8 -*-
"""Lámina chica «hoy | propuesta» de la Tarea 169, «Desafío superado en un recuadro» (25-oct-2026): `docs/previews/tarea-169.png`.
Ricardo: «el desafío superado sale en letras como en el fondo. Quizá podría ser bueno que también quede en un recuadro pequeñito, quizás de otro color». Opus decidió cómo: un recuadro PLANO (no de arcilla: sin borde tinta grueso ni sombra dura, para que no parezca botón) en el lugar y al ancho del botón del Desafío,
con el radio del kit (22 dp), en el LILA del Desafío en un tono claro y suave (#D9CEFF), con el visto y «Desafío superado: <juego>.» en tinta y «Mañana hay ruta y Desafío nuevos.» debajo; sin cometa, sin sol y sin tocar.
Todas las tomas son CAPTURAS REALES: «HOY» = la Tarea 167 instalada (`app/src/test/screenshots/tarea-167/`, Roborazzi, y `tarea-167-emulador/`, el emulador `medium_phone` con letra 2,0) y «PROPUESTA» = `Tarea169CapturasTest` (`app/src/test/screenshots/tarea-169/`) y, con letra 2,0, el emulador
(`app/src/test/screenshots/tarea-169-emulador/`; si falta, la de Roborazzi).
Uso:  python -I tools/previews/tarea_169.py      → docs/previews/tarea-169.png
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SHOTS = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots')
HOY = os.path.join(SHOTS, 'tarea-167'); HOY_EMU = os.path.join(SHOTS, 'tarea-167-emulador'); NUEVO = os.path.join(SHOTS, 'tarea-169'); NUEVO_EMU = os.path.join(SHOTS, 'tarea-169-emulador')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-169.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
NAVY, SUN, DIM, WHITE, FILL = (20, 18, 63), (255, 203, 69), (200, 196, 235), (255, 255, 255), (217, 206, 255)
CW = 360; GAP = 24; MG = 40; COLS = 3
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


def shot(*paths):
    for p in paths:
        if os.path.exists(p):
            im = Image.open(p).convert('RGB')
            return im.resize((CW, int(round(im.height * CW / im.width))), Image.LANCZOS)
    return None


def main():
    probe = ImageDraw.Draw(Image.new('RGB', (10, 10)))
    F_T, F_H, F_P, F_C = font(52), font(34), font(25, False), font(22, False)
    intro = ('Ricardo (25-oct): «el desafío superado sale en letras como en el fondo. Quizá podría ser bueno que también quede en un recuadro pequeñito, quizás de otro color». '
             'Opus lo decidió así: un recuadro PLANO (sin borde tinta grueso ni sombra dura: no parece botón, como la losa lima de «Ruta completa» que se quitó en la T165b), en el LILA del Desafío en un tono claro y suave (#D9CEFF), '
             'en el lugar y al ancho que tenía el botón, con el radio del kit (22 dp) y el alto que le da el contenido. Adentro: el visto y «Desafío superado: <juego>.» en tinta y, debajo, «Mañana hay ruta y Desafío nuevos.» cuando el día está hecho. Sin cometa, sin sol y sin tocar.')
    medidas = ('Medido en la prueba: tinta sobre el lila claro 11,8:1 y tinta suave 5,4:1 (mínimo 4,5:1). Cabe sin deslizar con letra 1,3 (HoyTarjetaTest, en los siete estados del día); con letra 2,0 se desliza y ninguna palabra se parte (emulador). '
               'Con la ruta a medias el recuadro dice solo el visto y la frase, sin «Mañana…».')
    l_in = wrap(probe, intro, F_P, W - 2 * MG); l_me = wrap(probe, medidas, F_P, W - 2 * MG)
    filas = [
        ('HOY (T167) · ruta completa: solo letras sobre el fondo',
         [('letra 1,0', shot(os.path.join(HOY, 'desafio-superado.png'))), ('letra 1,3', shot(os.path.join(HOY, 'desafio-superado-letra13.png'))), ('letra 2,0 · emulador', shot(os.path.join(HOY_EMU, 'completa-superado-letra20.png')))]),
        ('PROPUESTA · ruta completa: recuadro plano en lila claro',
         [('letra 1,0', shot(os.path.join(NUEVO, 'desafio-superado.png'))), ('letra 1,3', shot(os.path.join(NUEVO, 'desafio-superado-letra13.png'))),
          ('letra 2,0 · emulador' if os.path.exists(os.path.join(NUEVO_EMU, 'completa-superado-letra20.png')) else 'letra 2,0 · Roborazzi', shot(os.path.join(NUEVO_EMU, 'completa-superado-letra20.png'), os.path.join(NUEVO, 'desafio-superado-letra20.png')))]),
        ('PROPUESTA · ruta a medias: solo el visto y la frase',
         [('letra 1,0', shot(os.path.join(NUEVO, 'superado-ruta-a-medias.png'))), ('letra 1,3', shot(os.path.join(NUEVO, 'superado-ruta-a-medias-letra13.png'))),
          ('letra 2,0 · emulador', shot(os.path.join(NUEVO_EMU, 'mitad-superado-letra20.png')))]),
    ]
    alto = max(im.height for _, row in filas for _, im in row if im is not None)
    H = 40 + 70 + len(l_in) * 34 + 14 + len(l_me) * 34 + 24 + len(filas) * (52 + alto + 40) + 30
    sheet = Image.new('RGB', (W, H), NAVY); d = ImageDraw.Draw(sheet)
    y = 40; d.text((MG, y), 'Desafío superado en un recuadro · Tarea 169', font=F_T, fill=SUN); y += 70
    for ln in l_in: d.text((MG, y), ln, font=F_P, fill=DIM); y += 34
    y += 14
    for ln in l_me: d.text((MG, y), ln, font=F_P, fill=WHITE); y += 34
    y += 24
    for titulo, row in filas:
        d.text((MG, y), titulo, font=F_H, fill=FILL if titulo.startswith('PROPUESTA') else SUN); y += 52
        for k, (rot, im) in enumerate(row):
            x = MG + k * (CW + GAP)
            if im is None:
                d.rectangle((x, y, x + CW - 1, y + alto - 1), outline=(70, 74, 130)); d.text((x + 14, y + 14), rot + ' (sin toma)', font=F_C, fill=DIM); continue
            sheet.paste(im, (x, y)); d.rectangle((x, y, x + CW - 1, y + im.height - 1), outline=(70, 74, 130)); d.text((x + 8, y + im.height + 6), rot, font=F_C, fill=DIM)
        y += alto + 40
    sheet.save(OUT, optimize=True); print('ok', OUT, sheet.size)


if __name__ == '__main__':
    main()
