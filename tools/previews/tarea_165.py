# -*- coding: utf-8 -*-
"""Lámina «hoy | propuesta» de la Tarea 165, «Hoy, una sola tarjeta» (24-oct-2026; `auditoria-fable/hoy-reestructura.md`, opción 3, elegida por Ricardo): `docs/previews/tarea-165.png`.
Ricardo vio los letreros de la isla (Tarea 163, no instalada) y dijo que Hoy «se ve muy sobrecargada, demasiada sobrecarga de estímulos, de botones»: Hoy tenía 12 toques y once focos para una sola tarea. Ahora: el saludo, la semana, UNA tarjeta con la ruta de hoy (Nubi en su esquina, tres filas que no se tocan y el botón sol adentro) y «Todos los juegos».
Todas las tomas son CAPTURAS REALES de Roborazzi a 393 × 873 dp (1080 × 2400): «HOY» = el commit anterior (`app/src/test/screenshots/tarea-165-hoy/`) y «PROPUESTA» = `Tarea165CapturasTest` (`app/src/test/screenshots/tarea-165/`: los cinco estados a letra 1,0 y 1,3 y la letra 2,0);
si existe `app/src/test/screenshots/tarea-165-emulador/` (capturas del emulador `medium_phone` con letra 2,0 y 3 botones, de `tools/emulador_ui.py shot`), se suman.
Uso:  python -I tools/previews/tarea_165.py      → docs/previews/tarea-165.png
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SHOTS = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots')
HOY = os.path.join(SHOTS, 'tarea-165-hoy'); NUEVO = os.path.join(SHOTS, 'tarea-165'); EMU = os.path.join(SHOTS, 'tarea-165-emulador')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-165.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
NAVY, SUN, DIM, CREAM, LIME, CORAL = (20, 18, 63), (255, 203, 69), (200, 196, 235), (245, 235, 219), (155, 229, 100), (255, 140, 107)
CW = 360; GAP = 24; MG = 40; COLS = 6
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


def shot(folder, name):
    p = os.path.join(folder, name + '.png')
    if not os.path.exists(p): return None
    im = Image.open(p).convert('RGB')
    return im.resize((CW, int(round(im.height * CW / im.width))), Image.LANCZOS)


def main():
    probe = ImageDraw.Draw(Image.new('RGB', (10, 10)))
    F_T, F_H, F_P, F_C, F_B = font(60), font(36), font(26, False), font(22, False), font(27)
    intro = ('Ricardo vio los letreros de la isla (Tarea 163, que no se instaló) y dijo que Hoy «se ve muy sobrecargada, demasiada sobrecarga de estímulos, de botones». Hoy tenía 12 toques y once focos para una sola tarea: jugar la ruta. Eligió la opción 3 de Fable, «Una sola tarjeta»: '
             'el saludo, la semana (farolas, llama y la frase de la racha) y UNA tarjeta crema con «Tu ruta de hoy», «1 de 3 · unos 4 minutos», los tres juegos con su estado en palabras (no se tocan) y el botón sol ADENTRO, con Nubi posada en su esquina; debajo, solo «Todos los juegos». '
             'Sin isla: aparece una vez al día, dentro de la tarjeta, cuando la ruta queda completa, con Nubi celebrando. Capturas reales a 393 × 873 dp.')
    cambios = ('Qué se ve ahora: 5 toques (el botón, «Todos los juegos» y las 3 pestañas) y 4 focos; un solo sol (el botón de la ruta: las filas van en tinta y crema, «Sigue» es un punto de tinta); Nubi de frente de día y dormida de las 20 a las 6; '
               'la partida en pausa cambia el botón por «Seguir tu partida»; con la ruta completa, la tarjeta lleva la isla chica con las piezas que ganaste y «Ruta completa». Se fueron la isla grande, los edificios como botones, la lámina del lugar, Nubi volando, las piedritas y el globo de la isla. '
               'La revelación de las piezas nuevas de la isla pasó a Tú.')
    medidas = ('Cabe sin deslizar, también con letra 1,3: a 393 × 873, «Todos los juegos» termina en 553 dp (letra 1,0) y 596 dp (1,3) de los 710 dp que dejan las barras; a 360 × 780, en 591 de 617. Con letra 2,0 se desliza y nada se corta. Las filas miden 52 dp, los nombres 17 sp, todo texto 16 sp o más; '
               'el estado se dice con palabra y marca (visto, punto, aro), no solo con color.')
    decidi = ('Lo que decidí yo: (1) «Sigue» lleva un punto de tinta y no el punto sol de la spec, porque Ricardo pidió que el botón sea el ÚNICO sol; (2) la isla de la ruta completa va en su ventanilla noche dentro de la tarjeta crema (se calibró sobre el cielo oscuro); (3) hay unos 120 dp de cielo bajo «Todos los juegos» con letra 1,0 (el aire que pedía Fable); '
              '(4) los tests de píxeles no corren en Robolectric: el «único sol» se vigila sobre el código; (5) el EMULADOR con letra 2,0 mostró «Despué/s» y «Acoplami/ento» partidos a media palabra (Robolectric no lo veía): ahora, si la palabra más larga no cabe junto a su estado, las tres filas ponen el estado DEBAJO del nombre, y `HoyStonesWordsTest` lo vigila con los 17 nombres hasta letra 2,0.')
    intro_l = wrap(probe, intro, F_P, W - 2 * MG); cam_l = wrap(probe, cambios, F_P, W - 2 * MG); med_l = wrap(probe, medidas, F_P, W - 2 * MG); dec_l = wrap(probe, decidi, F_P, W - 2 * MG)
    hoy = [('HOY · a mitad de ruta', shot(HOY, 'hoy')), ('HOY · letra 1,3 (se desliza)', shot(HOY, 'hoy-letra13')), ('HOY · al tocar un edificio', shot(HOY, 'hoy-lamina')), ('HOY · el globo de la isla', shot(HOY, 'hoy-globo')), ('HOY · la isla con 10 piezas', shot(HOY, 'hoy-isla-10'))]
    estados = [('primera hora', 'primera-hora'), ('a mitad de ruta', 'mitad-de-ruta'), ('ruta completa', 'ruta-completa'), ('partida en pausa', 'partida-en-pausa'), ('de noche', 'de-noche')]
    r1 = [(f'{lab} · letra 1,0', shot(NUEVO, f)) for lab, f in estados]
    r2 = [(f'{lab} · letra 1,3', shot(NUEVO, f + '-letra13')) for lab, f in estados]
    extra = [('a mitad de ruta · letra 2,0 (Roborazzi)', shot(NUEVO, 'mitad-de-ruta-letra20'))]
    for name, lab in (('letra20', 'emulador · letra 2,0'), ('letra20-abajo', 'emulador · letra 2,0, deslizado'), ('letra13', 'emulador · letra 1,3')):
        im = shot(EMU, name)
        if im is not None: extra.append((lab, im))
    rows = [('HOY (antes)', DIM, hoy), ('PROPUESTA · los cinco estados con letra 1,0', LIME, r1), ('PROPUESTA · los mismos con letra 1,3 (todo cabe sin deslizar) y letra 2,0', LIME, r2 + extra[:1]), ]
    if len(extra) > 1: rows.append(('PROPUESTA · en el emulador con letra 2,0', LIME, extra[1:]))
    H = MG + 80 + 34 * (len(intro_l) + len(cam_l) + len(med_l) + len(dec_l)) + 60
    for _, _, row in rows: H += 50 + max((im.height for _, im in row if im is not None), default=0) + 50
    sheet = Image.new('RGB', (W, H), NAVY); d = ImageDraw.Draw(sheet); y = MG
    d.text((MG, y), 'Hoy, una sola tarjeta: hoy | propuesta (Tarea 165)', font=F_T, fill=SUN); y += 80
    for lines, col in ((intro_l, CREAM), (cam_l, CREAM), (med_l, DIM), (dec_l, CORAL)):
        for ln in lines: d.text((MG, y), ln, font=F_P, fill=col); y += 34
        y += 12
    y += 16
    for titulo, color, row in rows:
        d.text((MG, y), titulo, font=F_H, fill=color); y += 50
        h = max((im.height for _, im in row if im is not None), default=0)
        for k, (lab, im) in enumerate(row):
            if im is None: continue
            x = MG + k * (CW + GAP)
            sheet.paste(im, (x, y)); d.rectangle((x, y, x + im.width - 1, y + im.height - 1), outline=(70, 74, 130), width=1)
            d.text((x, y + h + 8), lab, font=F_C, fill=color)
        y += h + 50
    sheet = sheet.crop((0, 0, W, y)); sheet.save(OUT)
    print('ok', OUT, sheet.size)


if __name__ == '__main__':
    main()
