# -*- coding: utf-8 -*-
"""Lámina de la Tarea 179, «Los avisos: la fila habla» (`docs/previews/tarea-179.png`), con CAPTURAS REALES de Unity (1080 × 2400); no son maquetas.
  1. «hoy | propuesta» en seis juegos: el aviso de arcilla de siempre (capturas del 25-oct, antes de la tarea) y la fila de consigna hablando (el mismo recorte del marcador, a tamaño real).
  2. Un ejemplo por tono, con la fila en reposo y de vuelta a su consigna.
  3. Un aviso en dos tiempos (el texto y después el subtítulo, en la misma fila).
  4. El fallo de Engranajes con su «Truco» (la franja de abajo se retiró) y Acoplamiento sin su chip.
  5. El error del Contrarreloj (coral con la equis).
  6. Los textos que cambiaron («antes → después») y las pruebas.
Entradas (todo fuera de git): `unity/test-results/capturas/<juego>/` (hoy), `unity/test-results/capturas-avisos/<juego>/` y `capturas-avisos-tt/<juego>/` (`NUBI_SHOTS_AVISOS=1`, con y sin `NUBI_SHOTS_TT=1`), `tools/previews/tarea_179_textos.txt` (antes → después).
Uso:  python -I tools/previews/tarea_179.py
"""
import os, sys, textwrap
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
HOY = os.path.join(ROOT, 'unity', 'test-results', 'capturas')
NUEVO = os.path.join(ROOT, 'unity', 'test-results', 'capturas-avisos')
NUEVO_TT = os.path.join(ROOT, 'unity', 'test-results', 'capturas-avisos-tt')
TEXTOS = os.path.join(HERE, 'tarea_179_textos.txt')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-179.png')
NAVY, SUN, DIM, CREAM, OK, WARN = (20, 18, 63), (255, 203, 69), (200, 196, 235), (245, 235, 219), (110, 220, 130), (255, 150, 120)
GAP = 36
W = 2400
NOMBRES = {'stroop': 'Tinta o Palabra', 'disparate': '¿Verdad o disparate?', 'freno': 'Freno de Emergencia', 'meteoros': 'Lluvia de meteoros', 'acoplamiento': 'Acoplamiento', 'engranajes': 'Engranajes'}


def font(px, bold=True, rubik=False):
    return ImageFont.truetype(os.path.join(FONTS, 'rubik_bold.ttf' if rubik else ('fredoka.ttf' if bold else 'nunito_regular.ttf')), int(px))


def load(path): return Image.open(path).convert('RGB') if path and os.path.exists(path) else None


def named(folder, name):
    """La toma cuyo nombre acaba en «-<name>.png» (los números de orden cambian de una corrida a otra)."""
    if not os.path.isdir(folder): return None
    for f in sorted(os.listdir(folder)):
        if f.lower().endswith('.png') and f[:-4].endswith('-' + name) and f[:2].isdigit(): return load(os.path.join(folder, f))
    return None


def crop(im, y0, y1):
    if im is None: return None
    return im.crop((0, y0, im.width, min(im.height, y1)))


def fit_w(im, w): return im.resize((w, round(im.height * w / im.width)), Image.LANCZOS)


def wrap(d, x, y, text, f, fill, width_chars, step):
    for line in textwrap.wrap(text, width_chars):
        d.text((x, y), line, font=f, fill=fill); y += step
    return y


def row(sheet, d, y, items, w, caption_chars=40, boxed=True):
    """items = [(imagen, etiqueta, nota)]. Pega a la escala dada y devuelve la y siguiente."""
    n = len(items); gap = (W - 2 * GAP - n * w) // max(1, n - 1) if n > 1 else 0
    x = GAP; tallest = 0
    for im, label, note in items:
        if im is not None:
            fi = fit_w(im, w); sheet.paste(fi, (x, y)); tallest = max(tallest, fi.height)
            if boxed: d.rectangle((x - 1, y - 1, x + fi.width, y + fi.height), outline=(70, 66, 120), width=2)
        else:
            d.rectangle((x, y, x + w, y + 300), outline=DIM); d.text((x + 16, y + 16), 'sin captura', font=font(26, False), fill=DIM); tallest = max(tallest, 300)
        x += w + gap
    x = GAP; ly = y + tallest + 10; end = ly
    for im, label, note in items:
        d.text((x, ly), label, font=font(30), fill=SUN)
        end = max(end, wrap(d, x, ly + 40, note, font(24, False), DIM, caption_chars, 31)); x += w + gap
    return end + 40


def title(d, y, text):
    d.text((GAP, y), text, font=font(46, rubik=True), fill=SUN); return y + 74


def build():
    sheet = Image.new('RGB', (W, 12000), NAVY); d = ImageDraw.Draw(sheet)
    d.text((GAP, 24), 'Los avisos: la fila habla (Tarea 179)', font=font(66, rubik=True), fill=SUN)
    y = 118
    for line in ('Hasta hoy el aviso de un juego era una losa de arcilla sol de 300 por 72 dp con un «!», en el primer lugar libre que encontraba (a veces sobre la zona de juego), y convivía con otros tres sistemas (chip de seguidos, franjas propias de Engranajes y Acoplamiento, textos flotantes). Ahora lo dice la fila de consigna del marcador, que ya está en los 17 juegos: se tiñe con el tono, cambia su ícono por una forma (flecha, visto, reloj, equis, punto), dice el aviso en una línea y vuelve sola a su consigna. No tapa nada, hay un solo lugar donde mirar y es plana (no parece un botón).',
                 'Todo son capturas reales de Unity a 1080 × 2400 (recortes del marcador a tamaño real; los recortes «hoy» son del 25-oct, antes de la tarea, con el aviso de prueba de dos renglones que usa el smoke). Los «+N» de puntos de las partidas de siempre se quedan (Ricardo, 22-oct); en Contrarreloj no sale ninguno.'):
        y = wrap(d, GAP, y, line, font(27, False), DIM, 150, 38)
    y += 16

    # ---- 1. hoy | propuesta (teléfonos enteros)
    y = title(d, y, '1 · Hoy | propuesta en cuatro juegos (el teléfono entero: ya no hay un cartel sobre la zona de juego)')
    pares = [('stroop', 'aviso', 'tono-paso', '«¡Un paso más! · Más difícil»: lima con la flecha, en la fila de siempre.'),
             ('disparate', 'aviso', 'tono-calma', '«Con calma · Un poco más fácil»: lila con el reloj.'),
             ('freno', 'aviso', 'tono-hito', '«10 seguidos · Sigue así»: sol con el visto, solo para un hito.'),
             ('meteoros', 'aviso', 'tono-hecho', '«¡Todo en orden!»: lima con el visto. El cielo queda libre.')]
    for k in (0, 2):
        items = []
        for jid, hoyn, nuevon, nota in pares[k:k + 2]:
            items.append((load(os.path.join(HOY, jid, [f for f in sorted(os.listdir(os.path.join(HOY, jid))) if f[:-4].endswith('-' + hoyn)][0])), 'HOY · ' + NOMBRES[jid], 'Un cartel de arcilla de 300 × 72 dp con su «!», sobre la zona de juego.'))
            items.append((named(os.path.join(NUEVO, jid), nuevon), 'PROPUESTA · ' + NOMBRES[jid], nota))
        y = row(sheet, d, y, items, 520, 34)

    # ---- 2. un ejemplo por tono
    y = title(d, y, '2 · Un ejemplo por tono (Tinta o Palabra: la fila en reposo, cada tono y la consigna que vuelve)')
    base = os.path.join(NUEVO, 'stroop')
    tonos = [('reposo', 'En reposo', 'La fila de consigna de siempre: losa noche, la regla de Tinta con su ícono.'),
             ('tono-paso', 'Paso · lima con la flecha', '«¡Un paso más!» sube la dificultad.'),
             ('tono-hecho', 'Hecho · lima con el visto', 'Lo logrado: «¡Todo en orden!», «¡Encaja!».'),
             ('tono-hito', 'Hito · sol con el visto', 'SOLO un hito: una racha, un límite nuevo (un solo sol a la vez).'),
             ('tono-calma', 'Ritmo · lila con el reloj', '«Con calma»: la dificultad baja un poco.'),
             ('tono-error', 'Error · coral con la equis', 'La nota del error, sin culpa.'),
             ('tono-info', 'Información · crema con un punto', 'Lo que solo informa; la fila en reposo es noche, no se confunde.'),
             ('vuelta', 'La fila vuelve sola', 'A su consigna, con un fundido de 0,2 s.')]
    for i in range(0, len(tonos), 2):
        items = [(crop(named(base, t[0]), 90, 440), t[1], t[2]) for t in tonos[i:i + 2]]
        y = row(sheet, d, y, items, 1060, 60)

    # ---- 3. dos tiempos
    y = title(d, y, '3 · Un aviso largo, en dos tiempos en la misma fila (texto 1,2 s; subtítulo 2 s)')
    base = os.path.join(NUEVO, 'disparate')
    y = row(sheet, d, y, [(crop(named(base, 'dos-tiempos-1'), 90, 440), 'Primer tiempo', '«¡Ráfaga! Cinco frases rápidas»: lo que pasa.'),
                          (crop(named(base, 'dos-tiempos-2'), 90, 440), 'Segundo tiempo', '«No cuentan para tu medida»: lo que importa. Nunca dos renglones, nunca dos avisos a la vez.')], 1060, 60)

    # ---- 4. Engranajes y Acoplamiento
    y = title(d, y, '4 · Engranajes: el fallo con su «Truco» (3,2 s, como la franja) y Acoplamiento sin su chip')
    eh = named(os.path.join(HOY, 'engranajes'), 'aviso-mal'); e1 = named(os.path.join(NUEVO, 'engranajes'), 'dos-tiempos-1'); e2 = named(os.path.join(NUEVO, 'engranajes'), 'dos-tiempos-2')
    y = row(sheet, d, y, [(eh, 'HOY · franja de abajo', 'El fallo y su pista en una losa crema bajo los botones.'), (e1, 'PROPUESTA · 1.er tiempo', 'Coral con la equis: lo que falló.'), (e2, 'PROPUESTA · 2.º tiempo', 'La pista («Truco: …») en la misma fila; la franja de abajo ya no existe.')], 700, 40)
    ah = load(os.path.join(HOY, 'acoplamiento', '07-error.png')); an = named(os.path.join(NUEVO, 'acoplamiento'), 'natural'); an2 = named(os.path.join(NUEVO, 'acoplamiento'), 'tono-hecho')
    y = row(sheet, d, y, [(ah, 'HOY · chip bajo las luces', '«Era su espejo» en un chip que busca su lugar.'), (an or an2, 'PROPUESTA · en la fila', 'El mismo aviso por la fila de consigna: nada tapa la pieza, el puerto ni los botones.')], 700, 40)

    # ---- 5. Contrarreloj
    y = title(d, y, '5 · El error del Contrarreloj (0,8 s): coral con la equis, por la misma fila')
    items = []
    for jid, nota in (('stroop', 'Tinta: «La tinta era azul» (la píldora se aparta).'), ('disparate', '«Era verdad» / «Era un disparate».'), ('acoplamiento', '«Era su espejo» / «Sí encajaba».')):
        items.append((named(os.path.join(NUEVO_TT, jid), 'tt-error'), NOMBRES[jid], nota))
    y = row(sheet, d, y, items, 700, 40)

    # ---- 6. textos
    y = title(d, y, '6 · Textos que cambiaron para caber en una línea a 16 dp (antes y después)')
    if os.path.exists(TEXTOS):
        for line in open(TEXTOS, encoding='utf-8').read().splitlines():
            if not line.strip(): continue
            y = wrap(d, GAP, y, line.replace(' → ', ' pasa a '), font(26, False), CREAM, 170, 36)
    y += 20
    sheet = sheet.crop((0, 0, W, y + 20)); sheet.save(OUT, optimize=True)
    print('lámina:', OUT, sheet.size)


if __name__ == '__main__':
    build()
