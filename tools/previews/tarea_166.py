# -*- coding: utf-8 -*-
"""Lámina «hoy | propuesta» de la Tarea 166, «Engranajes, últimos retoques» (25-oct-2026): `docs/previews/tarea-166.png`.
Ricardo probó la Tarea 164 en el teléfono (colores, mesa y botones bien) y pidió tres cosas: (1) quitar la SOMBRA dura que aparece detrás del recuadro de «el momento» y del aviso (el oscurecimiento del juego detrás se queda), (2) botones menos grandes («para este juego no requiere que sean tan grandes»; vuelven a 88 dp y el aire va a la mesa) y (3) que el cartel «Motor» no tape media rueda del motor.
Todas las tomas son CAPTURAS REALES de Unity (1080 × 2400, `bash tools/verificar-todo.sh --capturas engranajes,stroop,piloto,bodega`): «HOY» = el commit anterior (`unity/test-results/engranajes-164-madera/` para Engranajes y `engranajes-166-hoy/` para los otros juegos) y «PROPUESTA» = esta tarea (`unity/test-results/engranajes-166-nuevo/`). Los recortes van al 100 % salvo que se diga otra cosa.
Uso:  python -I tools/previews/tarea_166.py      → docs/previews/tarea-166.png
"""
import os, sys, glob
import numpy as np
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
TR = os.path.join(ROOT, 'unity', 'test-results')
HOY_E = os.path.join(TR, 'engranajes-164-madera'); HOY_O = os.path.join(TR, 'engranajes-166-hoy'); NUEVO = os.path.join(TR, 'engranajes-166-nuevo')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-166.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
NAVY, SUN, DIM, CREAM, LIME, CORAL = (20, 18, 63), (255, 203, 69), (200, 196, 235), (245, 235, 219), (155, 229, 100), (255, 140, 107)
W = 2400; MG = 40; GAP = 30; FR = (70, 74, 130)


def font(px, bold=True): return ImageFont.truetype(os.path.join(FONTS, 'fredoka.ttf' if bold else 'nunito_regular.ttf'), int(px))


def wrap(d, text, fnt, width):
    lines, cur = [], ''
    for word in text.split(' '):
        t = (cur + ' ' + word).strip()
        if d.textlength(t, font=fnt) <= width: cur = t
        else: lines.append(cur); cur = word
    if cur: lines.append(cur)
    return lines or ['']


def toma(folder, name):
    """La captura cuyo nombre (sin el número de orden) es [name]: `03-primera.png` → «primera»."""
    for p in sorted(glob.glob(os.path.join(folder, '*-' + name + '.png'))): return Image.open(p).convert('RGB')
    p = os.path.join(folder, name + '.png')
    if os.path.exists(p): return Image.open(p).convert('RGB')
    raise FileNotFoundError(name + ' en ' + folder)


def alto_boton_dp(im):
    """El alto (dp, a 3 px por dp) del botón «Arrancar» (celeste del kit) en una captura completa."""
    a = np.asarray(im).astype(int)[:, 400:1000]
    sky = (abs(a[..., 0] - 76) < 22) & (abs(a[..., 1] - 201) < 22) & (abs(a[..., 2] - 240) < 22)
    filas = np.where(sky.sum(axis=1) > 300)[0]
    return (filas.max() - filas.min() + 1) / 3.0


def pega(sheet, d, im, x, y, label=None, col=DIM, F=None):
    sheet.paste(im, (x, y)); d.rectangle((x, y, x + im.width - 1, y + im.height - 1), outline=FR)
    if label: d.text((x, y + im.height + 6), label, font=F, fill=col)


def main():
    probe = ImageDraw.Draw(Image.new('RGB', (10, 10)))
    F_T, F_H, F_P, F_C, F_B = font(60), font(36), font(26, False), font(23, False), font(27)
    bh = alto_boton_dp(toma(HOY_E, 'primera')); bn = alto_boton_dp(toma(NUEVO, 'engranajes-primera'))
    intro = ('Ricardo probó la Tarea 164 en el teléfono: colores, mesa y botones bien, y el juego funciona. Decidió dejar Engranajes así (se cancelan la parte B, los agujeros, y la C, el cohete lila; mi tinte lila del casco queda como el casco definitivo) y pidió tres retoques: '
             '(1) quitar la sombra dura que aparece detrás del recuadro de «el momento» y del aviso, (2) botones menos grandes y (3) que el cartel «Motor» no tape media rueda. Son capturas REALES de Unity a 1080 × 2400.')
    c1 = ('1 · La sombra del recuadro. Era una sombra negra semitransparente HORNEADA en la imagen de la ficha (la misma de los 17 juegos: el momento y la cuenta regresiva) y en la losa crema del aviso. Se quitó EN EL ORIGEN: `quita_sombra` en `tools/juegos/empaqueta.py` borra lo que hay fuera del cuerpo opaco (el cuerpo, su canto y su bisel quedan exactos; el lienzo no cambia). '
          'La ficha sin sombra se quita en los 17 juegos (atlas `emblemas`; OK de Opus) y la losa del aviso solo en Engranajes (`aviso_crema`): las losas y botones del kit conservan su sombra. El oscurecimiento del juego detrás NO se tocó.')
    c2 = ('2 · Los botones. Vuelven a %d dp (en la T164 eran %d) y su altura ya no crece con el aire. La escena no puede crecer con él: ya llega a 10 dp de cada borde de la pantalla (los carteles de la derecha mandan), así que el aire va a la mesa (44 dp más abajo de «Cohete número 1») y a la separación con los botones (68 dp); '
          'con un teléfono de 800 dp quedan 139 dp de cielo bajo los botones sin aviso (antes de la T164, 196) y 45 dp libres bajo el aviso.' % (88, 114))
    c3 = ('3 · El cartel «Motor». Ya no cuelga 16 dp bajo el centro del motor: se busca el lugar más cercano al motor, FUERA de su círculo, dentro de la ventanilla y sin pisar otra rueda, barra, correa ni eje (debajo mejor que arriba, por la flecha del giro). Prueba nueva: en 4800 máquinas '
          '(400 por cada una de las 12 etapas) el rótulo queda fuera del círculo del motor, dentro de la ventanilla y sin tocar nada: 0 casos que pisen otra pieza.')
    l_in = wrap(probe, intro, F_P, W - 2 * MG); l1 = wrap(probe, c1, F_P, W - 2 * MG); l2 = wrap(probe, c2, F_P, W - 2 * MG); l3 = wrap(probe, c3, F_P, W - 2 * MG)
    H = 400 + MG + 90 + 34 * (len(l_in) + len(l1) + len(l2) + len(l3)) + 60
    sheet = Image.new('RGB', (W, H + 7000), NAVY); d = ImageDraw.Draw(sheet); y = MG
    d.text((MG, y), 'Engranajes, últimos retoques: hoy | propuesta (Tarea 166)', font=F_T, fill=SUN); y += 90
    for lines, col in ((l_in, CREAM),):
        for ln in lines: d.text((MG, y), ln, font=F_P, fill=col); y += 34
    y += 14

    # --- 1 · el momento: Engranajes y tres juegos más (recortes de 600 × 600 al 75 %)
    d.text((MG, y), '1 · EL RECUADRO DEL MOMENTO · sin su sombra dura (el velo oscuro detrás se queda)', font=F_H, fill=SUN); y += 56
    for ln in l1: d.text((MG, y), ln, font=F_P, fill=CREAM); y += 34
    y += 12
    box = (240, 780, 840, 1380); cw = 450
    pares = []
    for lab, fh, fn in (('Engranajes', lambda: toma(HOY_E, 'momento'), lambda: toma(NUEVO, 'engranajes-momento')),
                        ('Tinta o Palabra', lambda: toma(HOY_O, 'stroop-momento'), lambda: toma(NUEVO, 'stroop-momento')),
                        ('Piloto Estelar', lambda: toma(HOY_O, 'piloto-momento'), lambda: toma(NUEVO, 'piloto-momento')),
                        ('Bodega de carga', lambda: toma(HOY_O, 'bodega-momento'), lambda: toma(NUEVO, 'bodega-momento'))):
        pares.append((lab, fh().crop(box).resize((cw, cw), Image.LANCZOS), fn().crop(box).resize((cw, cw), Image.LANCZOS)))
    x = MG; fila = 0
    for k, (lab, a, b) in enumerate(pares):
        if k == 2: y += cw + 70; x = MG
        pega(sheet, d, a, x, y, 'HOY · ' + lab, DIM, F_C); pega(sheet, d, b, x + cw + GAP, y, 'PROPUESTA · ' + lab, LIME, F_C)
        x += 2 * cw + 2 * GAP + 60
    y += cw + 70

    # --- el aviso
    d.text((MG, y), 'EL AVISO · la losa crema sin su sombra (recortes al 100 %)', font=F_H, fill=SUN); y += 56
    a = toma(HOY_E, 'aviso-mal').crop((0, 1975, 1080, 2285)); b = toma(NUEVO, 'engranajes-aviso-mal').crop((0, 2000, 1080, 2310))               # el aviso queda 25 px más abajo: los botones de 88 dp lo bajan menos que los de 114
    pega(sheet, d, a, MG, y, 'HOY · el aviso de un error: sombra dura abajo y a la derecha', DIM, F_C); pega(sheet, d, b, MG + 1080 + GAP, y, 'PROPUESTA · sin sombra (el del acierto es la misma losa)', LIME, F_C)
    y += 310 + 60

    # --- 2 · el reparto
    d.text((MG, y), '2 · LOS BOTONES a %d dp y el aire para la mesa · tomas enteras al 44 %%' % 88, font=F_H, fill=SUN); y += 56
    for ln in l2: d.text((MG, y), ln, font=F_P, fill=CREAM); y += 34
    y += 12
    fw = 470; fh = int(2400 * fw / 1080)
    for k, (im, lab, col) in enumerate(((toma(HOY_E, 'primera-aviso'), 'HOY (T164): botones de %d dp' % 114, DIM), (toma(NUEVO, 'engranajes-primera-aviso'), 'PROPUESTA: botones de %d dp, mesa y separación más aireadas' % 88, LIME))):
        pega(sheet, d, im.resize((fw, fh), Image.LANCZOS), MG + k * (fw + GAP), y, None)
        d.text((MG + k * (fw + GAP), y + fh + 6), lab, font=F_C, fill=col)
    y += fh + 70

    # --- 3 · el cartel Motor
    d.text((MG, y), '3 · EL CARTEL «MOTOR» · fuera del círculo del motor, en tres máquinas distintas (recortes al 70 %)', font=F_H, fill=SUN); y += 56
    for ln in l3: d.text((MG, y), ln, font=F_P, fill=CREAM); y += 34
    y += 12
    cx0, cy0, cx1, cy1 = 30, 400, 690, 1450; sc = 0.70; cwid = int((cx1 - cx0) * sc); chei = int((cy1 - cy0) * sc)
    shots = ((toma(HOY_E, 't20'), 'HOY (T164): el rótulo tapa media rueda', DIM),
             (toma(NUEVO, 'engranajes-primera'), 'PROPUESTA · máquina 1', LIME), (toma(NUEVO, 'engranajes-t08'), 'PROPUESTA · máquina 2', LIME), (toma(NUEVO, 'engranajes-t20'), 'PROPUESTA · máquina 3', LIME))
    for k, (im, lab, col) in enumerate(shots):
        pega(sheet, d, im.crop((cx0, cy0, cx1, cy1)).resize((cwid, chei), Image.LANCZOS), MG + k * (cwid + GAP), y, lab, col, F_C)
    y += chei + 70
    sheet = sheet.crop((0, 0, W, y)); sheet.save(OUT)
    print('ok', OUT, sheet.size, 'botones: hoy %.1f dp, propuesta %.1f dp' % (bh, bn))


if __name__ == '__main__':
    main()
