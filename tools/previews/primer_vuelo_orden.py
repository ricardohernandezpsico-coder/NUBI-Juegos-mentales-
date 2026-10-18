# -*- coding: utf-8 -*-
"""Lámina «hoy | propuesta» del Primer vuelo (Tarea 132, A2 + A4; `auditoria-fable/spec-adopcion-A1-A7.md`): las pantallas REALES (Roborazzi) a 412 × 915 dp.
Hoy: la edad se pregunta ANTES del juego 1, y la cabecera es una barra con «Faltan unos 5 minutos». Propuesta: Hola · Nombre · Juego 1 (antes de jugar) · Tarjeta 1 · Edad (después), con seis farolas de etapa y «Paso N de 6 · <etapa>».
El último cuadro de cada fila es el paso más largo a 360 dp con la letra al 130 %.

Las capturas salen de `app/src/test/java/com/example/FirstFlightOrdenScreenshotTest.kt` (`PRIMER_VUELO_PREFIX=hoy|prop ./gradlew.bat :app:recordRoborazziDebug --tests "*FirstFlightOrdenScreenshotTest*"`; «hoy» se graba con el código de antes) y quedan en
`app/src/test/screenshots/primer-vuelo-orden/`. Uso:  python -I tools/previews/primer_vuelo_orden.py   (arma docs/previews/primer-vuelo-orden.png y copia las capturas, a 540 px de ancho, a docs/previews/primer-vuelo-orden/)
"""
import os, shutil
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SRC = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots', 'primer-vuelo-orden')
DST = os.path.join(ROOT, 'docs', 'previews', 'primer-vuelo-orden')
OUT = os.path.join(ROOT, 'docs', 'previews', 'primer-vuelo-orden.png')
FONT = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font', 'fredoka.ttf'); NUNITO = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font', 'nunito_regular.ttf')
BG = (9, 11, 38); SUN = (255, 201, 60); DIM = (186, 190, 230); WHITE = (255, 255, 255)
PW = 270
ROWS = [
    ('hoy', 'HOY', 'la edad antes del juego 1 · la barra y «Faltan unos 5 minutos»', WHITE,
     [('hola', 'Hola'), ('nombre', 'Nombre'), ('edad', 'Edad (antes del juego)'), ('juego1', 'Juego 1'), ('tarjeta1', 'Tarjeta 1'), ('360-1_3', '360 dp · letra al 130 %')]),
    ('prop', 'PROPUESTA', 'el juego primero · seis farolas de etapa y «Paso N de 6»', SUN,
     [('hola', 'Hola'), ('nombre', 'Nombre'), ('juego1', 'Juego 1 (antes de jugar)'), ('tarjeta1', 'Tarjeta 1'), ('edad', 'Edad (después)'), ('360-1_3', '360 dp · letra al 130 %')]),
]


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
    mask = Image.new('L', (w, h), 0); ImageDraw.Draw(mask).rounded_rectangle((0, 0, w - 1, h - 1), 20, fill=255)
    out = Image.new('RGBA', (w, h), (0, 0, 0, 0)); out.paste(im, (0, 0), mask)
    ImageDraw.Draw(out).rounded_rectangle((0, 0, w - 1, h - 1), 20, outline=(60, 64, 120, 255), width=2)
    return out


def main():
    os.makedirs(DST, exist_ok=True)
    for prefix, _, _, _, frames in ROWS:
        for name, _ in frames:
            p = os.path.join(SRC, '%s-%s.png' % (prefix, name))
            im = Image.open(p).convert('RGB'); im = im.resize((540, int(im.height * 540 / im.width)), Image.LANCZOS)      # las copias de docs/ van a 540 px de ancho (las originales pesan 1,3 MB cada una)
            im.save(os.path.join(DST, '%s-%s.png' % (prefix, name)), optimize=True)
    f_t = fuente(FONT, 36); f_h = fuente(FONT, 26); f_s = fuente(NUNITO, 20); f_c = fuente(NUNITO, 19)
    gap, mar = 18, 40
    ncol = max(len(r[4]) for r in ROWS)
    Wt = 2 * mar + ncol * PW + (ncol - 1) * gap
    ph = max(int(Image.open(os.path.join(SRC, '%s-%s.png' % (r[0], n))).height * PW / Image.open(os.path.join(SRC, '%s-%s.png' % (r[0], n))).width) for r in ROWS for n, _ in r[4])
    sheet = Image.new('RGB', (Wt, 220 + len(ROWS) * (ph + 130)), BG)
    d = ImageDraw.Draw(sheet)
    d.text((mar, 26), 'Primer vuelo: el juego primero y farolas de etapa, hoy | propuesta', font=f_t, fill=SUN)
    y = 78
    for texto in ('Tres cambios que Ricardo eligió el 18-oct: (1) el primer juego va ANTES de la edad y de las metas: antes del primer juego solo el nombre (opcional); (2) la edad se pregunta después de la primera tarjeta, solo si no se sabe, y el juego 1 corre como adulto con el arranque suave; '
                  '(3) seis farolas de etapa (Empezar · Memoria · Atención · Razonamiento · Lenguaje · Tu punto de partida; la primera ya encendida, la actual con el aro lima) en lugar de la barra y de los minutos. Capturas reales (Roborazzi); «hoy» es el código de antes.',):
        for linea in envuelve(d, texto, f_s, Wt - 2 * mar):
            d.text((mar, y), linea, font=f_s, fill=DIM); y += 26
    y += 16
    for prefix, titulo, sub, color, frames in ROWS:
        d.text((mar, y), titulo, font=f_h, fill=color)
        d.text((mar + d.textlength(titulo, font=f_h) + 16, y + 5), sub, font=f_c, fill=DIM)
        y += 42
        for i, (name, cap) in enumerate(frames):
            x = mar + i * (PW + gap)
            im = telefono(Image.open(os.path.join(SRC, '%s-%s.png' % (prefix, name))), PW)
            sheet.paste(im, (x, y), im)
            for j, linea in enumerate(envuelve(d, cap, f_c, PW)):
                d.text((x, y + im.height + 6 + j * 22), linea, font=f_c, fill=color if color == SUN else DIM)
        y += ph + 80
    sheet = sheet.crop((0, 0, Wt, y))
    sheet.save(OUT, optimize=True)
    print('ok', OUT, sheet.size)


if __name__ == '__main__':
    main()
