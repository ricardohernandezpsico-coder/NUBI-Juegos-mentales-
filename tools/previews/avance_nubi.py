# -*- coding: utf-8 -*-
"""Lámina «hoy | propuesta» de Nubi comentando «Tu semana» en Avance (Tarea 131, A6; `auditoria-fable/spec-adopcion-A1-A7.md`): el bloque REAL (`AvanceWeek` + `AvanceAreas`, Roborazzi) a 393 × 873 dp, con la letra del sistema normal y al 130 %.
Hoy: sin comentario. Propuesta: Nubi de 56 dp con su globo y UNA frase de `nubiWeekLine` (aquí, «Te falta 1 día para tu meta. Cualquier ruta cuenta.»).

Las capturas salen de `app/src/test/java/com/example/AvanceNubiScreenshotTest.kt` (`AVANCE_NUBI_PREFIX=hoy|prop ./gradlew.bat :app:recordRoborazziDebug --tests "*AvanceNubiScreenshotTest*"`) y quedan en
`app/src/test/screenshots/avance-nubi/`. Uso:  python -I tools/previews/avance_nubi.py   (arma docs/previews/avance-nubi.png y copia las cuatro capturas a docs/previews/avance-nubi/)
"""
import os, shutil
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SRC = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots', 'avance-nubi')
DST = os.path.join(ROOT, 'docs', 'previews', 'avance-nubi')
OUT = os.path.join(ROOT, 'docs', 'previews', 'avance-nubi.png')
FONT = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font', 'fredoka.ttf'); NUNITO = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font', 'nunito_regular.ttf')
BG = (9, 11, 38); SUN = (255, 201, 60); DIM = (186, 190, 230); WHITE = (255, 255, 255)
PW = 380
COLS = [('hoy-393-1_0', 'HOY · letra normal', 'sin comentario: «Tu semana» termina en «Áreas esta semana»', WHITE), ('prop-393-1_0', 'PROPUESTA · letra normal', 'Nubi de 56 dp con UNA frase corta, la primera de las seis que se cumple', SUN),
        ('hoy-393-1_3', 'HOY · letra al 130 %', 'igual: sin comentario', WHITE), ('prop-393-1_3', 'PROPUESTA · letra al 130 %', 'el globo pasa a 3 líneas y nada se corta; la pantalla crece unos 80 dp', SUN)]


def fuente(path, size):
    f = ImageFont.truetype(path, size)
    if path == FONT:
        try: f.set_variation_by_name('Bold')
        except Exception: pass
    return f


def envuelve(d, texto, font, ancho):
    lineas, actual = [], ''
    for palabra in texto.split():
        prueba = (actual + ' ' + palabra).strip()
        if d.textlength(prueba, font=font) <= ancho: actual = prueba
        else: lineas.append(actual); actual = palabra
    if actual: lineas.append(actual)
    return lineas


def telefono(im, w):
    h = int(im.height * w / im.width)
    im = im.convert('RGB').resize((w, h), Image.LANCZOS)
    mask = Image.new('L', (w, h), 0); ImageDraw.Draw(mask).rounded_rectangle((0, 0, w - 1, h - 1), 24, fill=255)
    out = Image.new('RGBA', (w, h), (0, 0, 0, 0)); out.paste(im, (0, 0), mask)
    ImageDraw.Draw(out).rounded_rectangle((0, 0, w - 1, h - 1), 24, outline=(60, 64, 120, 255), width=2)
    return out


def main():
    os.makedirs(DST, exist_ok=True)
    ims = []
    for name, _, _, _ in COLS:
        p = os.path.join(SRC, name + '.png')
        shutil.copy(p, os.path.join(DST, name + '.png'))
        ims.append(Image.open(p))
    f_t = fuente(FONT, 36); f_h = fuente(FONT, 24); f_s = fuente(NUNITO, 20); f_c = fuente(NUNITO, 18)
    gap, mar = 24, 40
    ph = max(int(im.height * PW / im.width) for im in ims)
    Wt = 2 * mar + len(COLS) * PW + (len(COLS) - 1) * gap
    sheet = Image.new('RGB', (Wt, 200 + ph + 100), BG)
    d = ImageDraw.Draw(sheet)
    d.text((mar, 26), 'Avance: Nubi comenta tu semana, hoy | propuesta', font=f_t, fill=SUN)
    y = 76
    for texto in ('Bajo «Tu semana», Nubi (la misma de Juegos, de 56 dp) dice UNA frase en su globo de losa crema. Siempre en positivo: «Te falta 1 día para tu meta. Cualquier ruta cuenta.»; nunca «no jugaste», «perdiste» ni «te atrasaste». '
                  'Seis frases, en este orden de prioridad: sin partidas · un día perdonado · meta cumplida · las 4 áreas · falta 1 día · lleva n de la meta. Capturas reales (Roborazzi) a 393 × 873 dp.',):
        for linea in envuelve(d, texto, f_s, Wt - 2 * mar):
            d.text((mar, y), linea, font=f_s, fill=DIM); y += 26
    y += 18
    for i, (name, titulo, leyenda, color) in enumerate(COLS):
        x = mar + i * (PW + gap)
        d.text((x, y), titulo, font=f_h, fill=color)
        im = telefono(ims[i], PW)
        sheet.paste(im, (x, y + 34), im)
        for j, linea in enumerate(envuelve(d, leyenda, f_c, PW)):
            d.text((x, y + 34 + im.height + 8 + j * 22), linea, font=f_c, fill=color if color == SUN else DIM)
    sheet = sheet.crop((0, 0, Wt, y + 34 + ph + 90))
    sheet.save(OUT, optimize=True)
    print('ok', OUT, sheet.size)


if __name__ == '__main__':
    main()
