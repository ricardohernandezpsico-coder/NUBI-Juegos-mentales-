# -*- coding: utf-8 -*-
"""Lámina de la Tarea 184, «Nubi siempre despierta y de Hoy a Tú sin pasar por Avance» (`docs/previews/tarea-184.png`), con CAPTURAS REALES; no son maquetas.
  1. Hoy a las 23:00, antes | después (Roborazzi a 393 × 873 dp, `Tarea184CapturasTest`: «antes» se grabó con el código de antes de la tarea —Nubi dormida de 20 a 6—, «después» con el de ahora) y un acercamiento a la cara de Nubi.
  2. La transición Hoy → Tú en el EMULADOR `medium_phone` (la página de Hoy se desvanece, entra Tú; Avance no aparece nunca).
  3. La transición Hoy → Avance, para comparar: sigue deslizando.
  4. Los números (tiempos, pruebas, lo que no cambia).
Los cuadros del emulador se tomaron con la escala de animación del emulador al 80 × (`settings put global animator_duration_scale 80`; Compose la respeta): una transición de 300 ms dura 24 s y un `screencap` por cuadro alcanza para ~30 capturas
(un `screencap` de este emulador tarda ~0,8 s). El tiempo de cada cuadro es el real de la transición: (hora de la captura − hora del primer cuadro que cambió) ÷ 80, con ± 15 ms de incertidumbre.
Entradas (fuera de git): `app/src/test/screenshots/tarea-184/{antes,despues}/hoy-23h.png`, `unity/test-results/t184-emu/{hoy_tu,hoy_avance}/` (`fNN.png` y `times.txt`), `tools/previews/tarea_184_notas.txt`.
Uso:  python -I tools/previews/tarea_184.py
"""
import os, sys, textwrap
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SHOTS = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots', 'tarea-184')
EMU = os.path.join(ROOT, 'unity', 'test-results', 't184-emu')
NOTAS = os.path.join(HERE, 'tarea_184_notas.txt')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-184.png')
NAVY, SUN, DIM, CREAM = (20, 18, 63), (255, 203, 69), (200, 196, 235), (245, 235, 219)
GAP = 36
W = 2400
SCALE = 80                                  # la escala de animación del emulador al tomar los cuadros
HOY_TU = [0, 3, 6, 8, 10, 11, 13, 16]       # los cuadros que se muestran (índices de fNN.png)
HOY_AVANCE = [0, 2, 3, 4, 6, 8, 10, 14]


def font(px, bold=True, rubik=False):
    return ImageFont.truetype(os.path.join(FONTS, 'rubik_bold.ttf' if rubik else ('fredoka.ttf' if bold else 'nunito_regular.ttf')), int(px))


def load(path): return Image.open(path).convert('RGB') if path and os.path.exists(path) else None


def fit_w(im, w): return im.resize((w, round(im.height * w / im.width)), Image.LANCZOS)


def wrap(d, x, y, text, f, fill, width_chars, step):
    for line in textwrap.wrap(text, width_chars):
        d.text((x, y), line, font=f, fill=fill); y += step
    return y


def title(d, y, text):
    d.text((GAP, y), text, font=font(46, rubik=True), fill=SUN); return y + 74


def times(folder):
    """Las horas (ns) de cada cuadro, de `times.txt`."""
    out = {}
    for line in open(os.path.join(EMU, folder, 'times.txt'), encoding='utf-8').read().splitlines():
        p = line.split()
        if len(p) == 2 and p[0].isdigit(): out[int(p[0])] = int(p[1])
    return out


def changed(a, b, thr=6):
    """¿Cambió el cuadro? (diferencia media por canal, en la zona de arriba de la pantalla: sin la hora de la barra de estado)."""
    box = (0, 160, a.width, 2000)
    from PIL import ImageChops, ImageStat
    return max(ImageStat.Stat(ImageChops.difference(a.crop(box), b.crop(box))).mean) > thr


def strip(sheet, d, y, folder, picks, w=270):
    """Una tira de cuadros del emulador, con el tiempo real de la transición debajo de cada uno."""
    ts = times(folder)
    base = load(os.path.join(EMU, folder, 'f00.png'))
    first = next((i for i in range(1, len(ts)) if changed(base, load(os.path.join(EMU, folder, f'f{i:02d}.png')))), 1)
    t0 = ts[first]                                  # hora del primer cuadro que ya cambió: el principio de la transición (± un cuadro)
    n = len(picks); gap = (W - 2 * GAP - n * w) // (n - 1)
    x = GAP; tall = 0
    for i in picks:
        im = load(os.path.join(EMU, folder, f'f{i:02d}.png'))
        fi = fit_w(im, w); sheet.paste(fi, (x, y)); tall = fi.height
        d.rectangle((x - 1, y - 1, x + fi.width, y + fi.height), outline=(70, 66, 120), width=2)
        ms = max(0, round((ts[i] - t0) / 1e6 / SCALE))
        d.text((x, y + tall + 8), ('~%d ms' % ms) if i >= first else 'antes del toque', font=font(28), fill=SUN)
        x += w + gap
    return y + tall + 60


def build():
    sheet = Image.new('RGB', (W, 9000), NAVY); d = ImageDraw.Draw(sheet)
    d.text((GAP, 24), 'Nubi siempre despierta y de Hoy a Tú sin pasar por Avance (Tarea 184)', font=font(62, rubik=True), fill=SUN)
    y = 118
    for line in ('Dos arreglos chicos que decidió Ricardo el 27-oct. (1) Nubi ya no duerme de 20 a 6 en la tarjeta de Hoy: él prueba de noche y la veía «siempre con los ojos cerrados»; ahora flota y parpadea a cualquier hora. '
                 '(2) Al tocar «Tú» estando en Hoy (o al revés) el pager deslizaba dos páginas y se veía pasar Avance: ahora, entre pestañas que NO son vecinas, hay un «fade through» de Material (la página se desvanece en 90 ms y entra la nueva en 210 ms, '
                 'con una escala de 0,96 a 1); entre vecinas se desliza como siempre, con el dedo no cambia nada y con «quitar animaciones» se salta de golpe. Todo son capturas reales.',):
        y = wrap(d, GAP, y, line, font(27, False), DIM, 150, 38)
    y += 16

    y = title(d, y, '1 · Hoy a las 23:00, antes | después (Roborazzi a 393 × 873)')
    antes = load(os.path.join(SHOTS, 'antes', 'hoy-23h.png')); despues = load(os.path.join(SHOTS, 'despues', 'hoy-23h.png'))
    zoom = (700, 410, 1000, 640)                      # la cara de Nubi, en píxeles de la captura (1032 × 2292)
    x1, x2 = GAP, GAP + 760
    top = y
    for x, im, label, note in ((x1, antes, 'ANTES', 'De 20 a 6 Nubi dormía: ojos cerrados todo el rato.'), (x2, despues, 'DESPUÉS', 'Siempre despierta: ojos abiertos y parpadea como de día.')):
        if im is None: continue
        fi = fit_w(im.crop((0, 0, im.width, 1780)), 700)
        sheet.paste(fi, (x, top)); d.rectangle((x - 1, top - 1, x + fi.width, top + fi.height), outline=(70, 66, 120), width=2)
        d.text((x, top + fi.height + 10), label, font=font(32), fill=SUN); wrap(d, x, top + fi.height + 52, note, font(24, False), DIM, 40, 30)
    for k, (im, label) in enumerate(((antes, 'ANTES · la cara de Nubi'), (despues, 'DESPUÉS · la cara de Nubi'))):
        if im is None: continue
        z = im.crop(zoom).resize((560, round(560 * (zoom[3] - zoom[1]) / (zoom[2] - zoom[0]))), Image.LANCZOS)
        zx = x2 + 760 + k * 0; zy = top + k * (z.height + 70)
        sheet.paste(z, (zx, zy)); d.rectangle((zx - 1, zy - 1, zx + z.width, zy + z.height), outline=(70, 66, 120), width=2)
        d.text((zx, zy + z.height + 8), label, font=font(28), fill=SUN)
    y = top + round(700 * 1780 / 1032) + 110

    y = title(d, y, '2 · De Hoy a Tú (emulador, 80 × más lento): se desvanece Hoy, entra Tú, Avance no aparece')
    y = strip(sheet, d, y, 'hoy_tu', HOY_TU)
    y = wrap(d, GAP, y, 'La barra de abajo marca «Tú» de una vez (la losa sol se funde sola, a su ritmo). El cambio de página se hace en lo más transparente, de golpe: el cuadro vacío del medio es el cielo, que no se mueve.', font(25, False), DIM, 175, 33) + 24

    y = title(d, y, '3 · De Hoy a Avance (lo mismo, para comparar): sigue deslizando')
    y = strip(sheet, d, y, 'hoy_avance', HOY_AVANCE)
    y = wrap(d, GAP, y, 'Entre pestañas vecinas no cambia nada: Hoy sale hacia la izquierda y Avance entra desde la derecha, en 300 ms.', font(25, False), DIM, 175, 33) + 24

    y = title(d, y, '4 · Los números')
    if os.path.exists(NOTAS):
        for line in open(NOTAS, encoding='utf-8').read().splitlines():
            if line.strip(): y = wrap(d, GAP, y, line, font(25, False), CREAM, 175, 34)
    y += 20
    sheet = sheet.crop((0, 0, W, y + 20)); sheet.save(OUT, optimize=True)
    print('lámina:', OUT, sheet.size)


if __name__ == '__main__':
    build()
