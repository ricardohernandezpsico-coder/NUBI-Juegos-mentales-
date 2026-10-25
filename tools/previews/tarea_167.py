# -*- coding: utf-8 -*-
"""Lámina «hoy | propuesta» de la Tarea 167, «El Desafío de hoy» (25-oct-2026; `auditoria-fable/hoy-desafio-y-dia-completo.md`, opción 1 «Dos botones, un solo sol a la vez», elegida por Ricardo): `docs/previews/tarea-167.png`.
Ricardo: la isla al completar el día «causa curiosidad pero no cumple una función» y en el inicio quiere «un botón exclusivo para jugar en modo desafío». Ahora Hoy lleva «Desafío de hoy» (lila, con un COMETA nuevo) junto a «Todos los juegos» mientras la ruta sigue; con la ruta completa la tarjeta se cierra sola y el sol pasa al Desafío; con el Desafío superado, ningún sol.
Todas las tomas son CAPTURAS REALES: «HOY» = Roborazzi de la Tarea 165 instalada (`app/src/test/screenshots/tarea-165/`) y «PROPUESTA» = `Tarea167CapturasTest` (`app/src/test/screenshots/tarea-167/`: los estados del día a letra 1,0 y 1,3); las del emulador `medium_phone` con letra 2,0 (`tools/emulador_ui.py shot`) están en `app/src/test/screenshots/tarea-167-emulador/`; el cometa grande es el render de Blender a 12 px/dp (`tools/isla/out/kit-cometa12/`).
Uso:  python -I tools/previews/tarea_167.py      → docs/previews/tarea-167.png
"""
import os, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SHOTS = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots')
HOY = os.path.join(SHOTS, 'tarea-165'); NUEVO = os.path.join(SHOTS, 'tarea-167'); EMU = os.path.join(SHOTS, 'tarea-167-emulador')
COMETA = os.path.join(ROOT, 'tools', 'isla', 'out', 'kit-cometa12', 'kit_i_cometa.png')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-167.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
NAVY, SUN, DIM, CREAM, LIME, CORAL, LILAC = (20, 18, 63), (255, 203, 69), (200, 196, 235), (245, 235, 219), (155, 229, 100), (255, 140, 107), (184, 164, 255)
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


def cometa_sobre(fondo, lado):
    """El cometa del render grande (Blender, 12 px/dp) sobre un fondo liso, con el recorte de la bruma del suelo que hace `kit.py`."""
    im = Image.open(COMETA).convert('RGBA'); a = np.asarray(im).astype(float).copy()
    al = np.clip((a[..., 3] - 12.0) * 255.0 / (255.0 - 12.0), 0, 255)
    h, w = al.shape; yy, xx = np.mgrid[0:h, 0:w]; d = np.minimum.reduce([xx, yy, w - 1 - xx, h - 1 - yy]).astype(float)
    al *= 0.5 - 0.5 * np.cos(np.pi * np.clip(d / 40.0, 0, 1)); a[..., 3] = al
    im = Image.fromarray(a.astype(np.uint8), 'RGBA').resize((lado, lado), Image.LANCZOS)
    bg = Image.new('RGBA', im.size, fondo + (255,)); bg.alpha_composite(im); return bg.convert('RGB')


def main():
    probe = ImageDraw.Draw(Image.new('RGB', (10, 10)))
    F_T, F_H, F_P, F_C, F_B = font(60), font(36), font(26, False), font(22, False), font(27)
    intro = ('Ricardo decidió (25-oct, sobre la propuesta de Fable): (1) el Desafío de hoy es la opción 1, «dos botones, un solo sol a la vez»; (2) es GRATIS y uno al día (el cobro de Plus no se construye; no cuenta para el futuro tope de partidas libres); (3) se puede REPETIR el mismo día; (4) lleva un COMETA nuevo. '
             'Todas las tomas son capturas reales (Roborazzi a 393 × 873 dp y el emulador con letra 2,0).')
    reglas = ('Cómo funciona: un juego al día en modo Desafío, de unos 2 minutos, elegido por Nubi con seis reglas (`DailyChallenge.kt`): solo juegos ya medidos; nunca uno de la ruta; primero el área que la ruta dejó fuera; el que lleva más días sin Desafío; no el de ayer; el reloj, como lo tenga en su ficha. '
              'Mientras la ruta sigue, el sol está en la tarjeta y el Desafío es una losa LILA junto a «Todos los juegos». Con la ruta completa la tarjeta se cierra sola (tres «Hecho», «3 de 3 · ruta completa» en negrita, Nubi saluda, sin botón ni rótulo; se fueron la isla chica y el «Ruta completa» de la T165b) y el SOL pasa al Desafío, a todo el ancho. '
              'Jugado sin superar, queda en crema con «otra vez». Superado, el visto con «Desafío superado: Tinta o Palabra.» y «Mañana hay ruta y Desafío nuevos.»: ningún sol, el día está hecho.')
    medidas = ('Cabe sin deslizar con letra 1,3 a 393 × 873 dp en los cuatro estados más la pausa (lo último termina en 625, 614, 614, 614 y 688 dp de los 710 que deja la barra; con letra 1,0, en 561, 559, 559, 536 y 590) y a 360 × 780 con letra 1,0 (hasta 617 de 617). «Todos los juegos» y «Desafío de hoy» caben en UNA línea con letra 1,0; '
               'con letra 2,0 se desliza, nada se corta y NINGUNA palabra se parte a la mitad (se midió en el emulador, como en la T165, no solo en Robolectric).')
    decidi = ('Lo que decidí yo: (1) en media fila, «Todos los juegos» va SIN su ícono de casillas para que quepa en una línea con letra 1,0 (con ícono se partía en dos); en el botón ancho lo conserva; (2) en la fila de media ancho, jugado sin superar dice «Desafío de hoy» y «otra vez»; (3) con el Desafío superado y la ruta a medias dice solo el visto y la frase, sin «Mañana…»; '
              '(4) sin Desafío (el primer día no hay un juego medido fuera de la ruta) Hoy queda como la T165 con «Mañana te espera una ruta nueva.» bajo la tarjeta cerrada; (5) el cometa: la «cola ancha» que Ricardo eligió entre dos variantes (`docs/previews/cometa-t167b.png`), una gota alargada en diagonal con dos estelas finas y sin luceros; se ve sobre lila, sol y crema (abajo, ampliado).')
    l_in = wrap(probe, intro, F_P, W - 2 * MG); l_re = wrap(probe, reglas, F_P, W - 2 * MG); l_me = wrap(probe, medidas, F_P, W - 2 * MG); l_de = wrap(probe, decidi, F_P, W - 2 * MG)
    estados = [('primera hora', 'primera-hora'), ('a mitad de ruta', 'mitad-de-ruta'), ('ruta completa · sol al Desafío', 'ruta-completa'), ('Desafío jugado · otra vez', 'otra-vez'), ('Desafío superado · sin sol', 'desafio-superado'), ('partida en pausa', 'partida-en-pausa')]
    hoy = [('HOY (T165) · primera hora', shot(HOY, 'primera-hora')), ('HOY · a mitad de ruta', shot(HOY, 'mitad-de-ruta')), ('HOY · ruta completa (la isla)', shot(HOY, 'ruta-completa')), ('HOY · partida en pausa', shot(HOY, 'partida-en-pausa'))]
    r1 = [(lab, shot(NUEVO, f)) for lab, f in estados]
    r2 = [(lab, shot(NUEVO, f + '-letra13')) for lab, f in estados]
    emu = [(lab, shot(EMU, n)) for lab, n in (('emulador · letra 1,3 · primera hora', 'letra13'), ('emulador · letra 2,0 · primera hora', 'letra20'), ('emulador · letra 2,0, deslizado', 'letra20-abajo'), ('emulador · letra 2,0 · ruta completa', 'completa-letra20-abajo'), ('emulador · letra 2,0 · Desafío jugado', 'completa-jugado-letra20'), ('emulador · letra 2,0 · Desafío superado', 'completa-superado-letra20'))]
    sin = [('sin Desafío · ruta completa · 1,0', shot(NUEVO, 'sin-desafio')), ('Roborazzi · letra 2,0 · ruta completa', shot(NUEVO, 'ruta-completa-letra20')), ('Roborazzi · letra 2,0 · a mitad', shot(NUEVO, 'mitad-de-ruta-letra20'))]
    rows = [('HOY (antes)', DIM, hoy), ('PROPUESTA · los estados del día con letra 1,0', LIME, r1), ('PROPUESTA · los mismos con letra 1,3 (todo cabe sin deslizar)', LIME, r2)]
    if any(im is not None for _, im in emu): rows.append(('PROPUESTA · en el emulador (letra 2,0 y 1,3, tomas reales con las barras del sistema)', LIME, emu))
    rows.append(('PROPUESTA · sin Desafío (primer día) y letra 2,0 en Roborazzi', LIME, sin))
    lado = 440
    H = MG + 80 + 34 * (len(l_in) + len(l_re) + len(l_me) + len(l_de)) + 100 + 60 + lado + 120
    for _, _, row in rows: H += 50 + max((im.height for _, im in row if im is not None), default=0) + 50
    sheet = Image.new('RGB', (W, H + 400), NAVY); d = ImageDraw.Draw(sheet); y = MG
    d.text((MG, y), 'El Desafío de hoy: hoy | propuesta (Tarea 167)', font=F_T, fill=SUN); y += 80
    for lines, col in ((l_in, CREAM), (l_re, CREAM), (l_me, DIM), (l_de, CORAL)):
        for ln in lines: d.text((MG, y), ln, font=F_P, fill=col); y += 34
        y += 12
    y += 10
    # el cometa ampliado sobre lila, sol, crema y noche
    if os.path.exists(COMETA):
        d.text((MG, y), 'EL COMETA · kit_i_cometa, ampliado (render de Blender a 12 px por dp) sobre lila, sol, crema y noche, y a su tamaño real (24 dp)', font=F_H, fill=SUN); y += 56
        for k, (n, c) in enumerate((('lila', LILAC), ('sol', SUN), ('crema', CREAM), ('noche', (28, 27, 82)))):
            x = MG + k * (lado + GAP)
            sheet.paste(cometa_sobre(c, lado), (x, y)); d.rectangle((x, y, x + lado - 1, y + lado - 1), outline=(70, 74, 130)); d.text((x, y + lado + 6), n, font=F_C, fill=DIM)
        # a 24 dp (72 px a 3 px por dp) y a 2x, sobre los tres fondos del botón
        x = MG + 4 * (lado + GAP)
        for k, (n, c) in enumerate((('lila', LILAC), ('sol', SUN), ('crema', CREAM))):
            for j, sz in enumerate((72, 144)):
                t = cometa_sobre(c, sz); sheet.paste(t, (x + j * 160, y + k * 150)); d.rectangle((x + j * 160, y + k * 150, x + j * 160 + sz - 1, y + k * 150 + sz - 1), outline=(70, 74, 130))
        d.text((x, y + 3 * 150), '72 px (24 dp a 3 px/dp) y 144 px', font=F_C, fill=DIM)
        y += lado + 60
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
