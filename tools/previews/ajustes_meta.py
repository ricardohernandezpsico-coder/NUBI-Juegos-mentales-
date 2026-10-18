# -*- coding: utf-8 -*-
"""Lámina «hoy | propuesta» de la meta semanal de Ajustes (Tarea 130, `auditoria-fable/spec-kit-juice-ajustes.md` §3): el panel «Meta semanal de juego» REAL (Roborazzi) a 393 × 873 dp, con la letra del sistema normal y al 130 %.
Hoy: el `Slider` de Material (pulgar de 20 dp, pista de 4 dp: el único control de la app que no era del kit). Propuesta: 7 farolas del kit (las de Hoy y Avance), encendidas de 1 a la meta, con su número 1-7 debajo.

Las capturas salen de `app/src/test/java/com/example/AjustesMetaScreenshotTest.kt` (`AJUSTES_META_PREFIX=hoy|prop ./gradlew.bat :app:recordRoborazziDebug --tests "*AjustesMetaScreenshotTest*"`) y quedan en
`app/src/test/screenshots/ajustes-meta/`. Uso:  python -I tools/previews/ajustes_meta.py   (arma docs/previews/ajustes-meta.png y copia las cuatro capturas a docs/previews/ajustes-meta/)
"""
import os, shutil
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SRC = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots', 'ajustes-meta')
DST = os.path.join(ROOT, 'docs', 'previews', 'ajustes-meta')
OUT = os.path.join(ROOT, 'docs', 'previews', 'ajustes-meta.png')
FONT = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font', 'fredoka.ttf'); NUNITO = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font', 'nunito_regular.ttf')
BG = (9, 11, 38); SUN = (255, 201, 60); DIM = (186, 190, 230); WHITE = (255, 255, 255)
PW = 380
COLS = [('hoy-393-1_0', 'HOY · letra normal', 'el Slider de Material: pulgar de 20 dp', WHITE), ('prop-393-1_0', 'PROPUESTA · letra normal', '7 farolas del kit; la elegida, con su número en un chip sol', SUN),
        ('hoy-393-1_3', 'HOY · letra al 130 %', 'igual: el pulgar no crece con la letra', WHITE), ('prop-393-1_3', 'PROPUESTA · letra al 130 %', 'los números pasan a ~21 sp y caben en 46 dp', SUN),
        ('prop-360-2_0', 'PROPUESTA · 360 dp, letra al 200 %', 'la pantalla más angosta con la letra más grande: todo entra', SUN)]


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
    sheet = Image.new('RGB', (Wt, 200 + ph + 80), BG)
    d = ImageDraw.Draw(sheet)
    d.text((mar, 26), 'Ajustes: la meta semanal, hoy | propuesta', font=f_t, fill=SUN)
    y = 76
    for texto in ('El único control de la app que no era del kit (un Slider de Material, de pulgar chico) pasa a 7 farolas del kit —las mismas de Hoy y Avance— con su número 1-7 debajo: se lee como CANTIDAD. Cada una es un botón de radio en una caja de 46 dp con el toque mínimo de Material; la farola mide 32 dp, las encendidas llevan un halo sol y la ELEGIDA se marca además por forma: su número en un chip sol con tinta. '
                  'Capturas reales (Roborazzi) a 393 × 873 dp (la última, a 360 dp).',):
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
    sheet = sheet.crop((0, 0, Wt, y + 34 + ph + 70))
    sheet.save(OUT, optimize=True)
    print('ok', OUT, sheet.size)


if __name__ == '__main__':
    main()
