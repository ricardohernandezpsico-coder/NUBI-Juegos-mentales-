# -*- coding: utf-8 -*-
"""Lámina de la Tarea 178, «Contrarreloj: aciertos menos errores» (`docs/previews/tarea-178.png`), con CAPTURAS REALES; no son maquetas.
  1. El chip del marcador en partida, con la MARCA neta («aciertos menos errores»), en los cuatro juegos (Unity, 1080 × 2400, a los 30 s).
  2. El final con la marca neta: sin récord, con marca nueva y sin errores (Roborazzi a 393 × 873 dp): la cifra grande es la marca y debajo, en frase, cuántos aciertos y cuántos errores.
  3. La prueba del azar con la marca neta (los cuatro pasan; antes de la Tarea 178, en gris, entre paréntesis).
  4. El «círculo blanco» de Tinta o Palabra, cuadro a cuadro, ANTES y DESPUÉS: la píldora de mensajes (`PhasePill`) se veía como un disco crema bajo la tarjeta hasta la primera respuesta; ahora nace oculta.
Entradas: `app/src/test/screenshots/tarea-178/` (`TimeTrialResultScreenTest.tarea_178_capturas`, solo al grabar), `unity/test-results/capturas-contrarreloj/` (`NUBI_SHOTS_TT=1`), `unity/test-results/capturas-cuadros-tt/` (antes) y `capturas-cuadros-tt-despues/`
(`NUBI_SHOTS_FRAMES=1 NUBI_SHOTS_TT=1`), `unity/test-results/contrarreloj-azar.txt` (todo fuera de git salvo la primera carpeta de capturas, que es de pruebas y tampoco va).
Uso:  python -I tools/previews/tarea_178.py
"""
import os, sys, textwrap
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
ROB = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots', 'tarea-178')
UNI = os.path.join(ROOT, 'unity', 'test-results', 'capturas-contrarreloj')
ANTES = os.path.join(ROOT, 'unity', 'test-results', 'capturas-cuadros-tt', 'stroop')
DESPUES = os.path.join(ROOT, 'unity', 'test-results', 'capturas-cuadros-tt-despues', 'stroop')
AZAR = os.path.join(ROOT, 'unity', 'test-results', 'contrarreloj-azar.txt')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-178.png')
NAVY, SUN, DIM, CREAM, OK, WARN, GREY = (20, 18, 63), (255, 203, 69), (200, 196, 235), (245, 235, 219), (110, 220, 130), (255, 150, 120), (150, 146, 190)
GAP = 36
W = 2400

# el azar de la Tarea 176 (marca = aciertos, con pausa del error), para la comparación
ANTES_AZAR = {'stroop': ('54,8', '5,5', '10 %'), 'disparate': ('30,4', '13,3', '44 %'), 'radar': ('12,6', '1,2', '10 %'), 'acoplamiento': ('19,2', '11,3', '59 %')}
NOMBRES = {'stroop': 'Tinta o Palabra', 'disparate': '¿Verdad o disparate?', 'radar': 'Rescate relámpago', 'acoplamiento': 'Acoplamiento'}


def font(px, bold=True, rubik=False):
    return ImageFont.truetype(os.path.join(FONTS, 'rubik_bold.ttf' if rubik else ('fredoka.ttf' if bold else 'nunito_regular.ttf')), int(px))


def load(path): return Image.open(path).convert('RGB') if os.path.exists(path) else None


def fit_w(im, w): return im.resize((w, round(im.height * w / im.width)), Image.LANCZOS)


def wrap(d, x, y, text, f, fill, width_chars, step):
    for line in textwrap.wrap(text, width_chars):
        d.text((x, y), line, font=f, fill=fill); y += step
    return y


def frame_named(folder, stem):
    """La toma cuyo nombre contiene «-fNNN» (el cuadro NNN)."""
    if not os.path.isdir(folder): return None
    for f in os.listdir(folder):
        if f.lower().endswith('.png') and ('-' + stem + '.') in f: return load(os.path.join(folder, f))
    return None


def row(sheet, d, y, items, w, caption_chars=34):
    n = len(items); gap = (W - 2 * GAP - n * w) // max(1, n - 1) if n > 1 else 0
    x = GAP; tallest = 0
    for im, label, note in items:
        if im is not None:
            fi = fit_w(im, w); sheet.paste(fi, (x, y)); tallest = max(tallest, fi.height)
        else:
            d.rectangle((x, y, x + w, y + 300), outline=DIM); d.text((x + 16, y + 16), 'sin captura', font=font(26, False), fill=DIM); tallest = max(tallest, 300)
        x += w + gap
    x = GAP; ly = y + tallest + 10; end = ly
    for im, label, note in items:
        d.text((x, ly), label, font=font(30), fill=SUN)
        end = max(end, wrap(d, x, ly + 40, note, font(24, False), DIM, caption_chars, 31)); x += w + gap
    return end + 36


def build():
    sheet = Image.new('RGB', (W, 9000), NAVY); d = ImageDraw.Draw(sheet)
    d.text((GAP, 24), 'Contrarreloj: aciertos menos errores (Tarea 178)', font=font(66, rubik=True), fill=SUN)
    y = 118
    for line in ('Ricardo probó Tinta o Palabra («funciona bien») pero la espera tras cada error (3, 6 y 9 s) alargaba demasiado la partida. Eligió cambiar la forma de medir: ya NO hay pausa tras el error (solo el aviso breve de siempre, 0,8 s) y la MARCA es aciertos menos errores, con piso en 0.',
                 'La pista pasa a la versión 2 en los cuatro juegos (la regla cambió: los récords empiezan de nuevo; los de la versión 1 quedan en el detalle). Se mantiene el mínimo antes de responder en los juegos de dos opciones. En la partida el chip baja con un error (sin signos); en el final la cifra grande es la marca y debajo, en frase, los aciertos y los errores.'):
        y = wrap(d, GAP, y, line, font(26, False), DIM, 150, 36)
    y += 14
    # ---- 1. el chip en partida
    d.text((GAP, y), '1 · El chip del marcador es la marca neta (Unity, a los 30 s de partida)', font=font(44), fill=SUN); y += 70
    sel = {'stroop': '06-t30.png', 'disparate': '06-t30.png', 'radar': '04-t30.png', 'acoplamiento': '06-t30.png'}
    notes = {
        'stroop': 'Tinta o Palabra: el chip de arriba a la derecha dice la marca; un error lo baja uno. El tiempo («1:30») en la fila de la consigna.',
        'disparate': '¿Verdad o disparate?: ya sin la espera de 3, 6 o 9 s tras el error: la frase que sigue llega enseguida.',
        'radar': 'Rescate relámpago: una ronda lograda suma uno, una fallada resta uno (nunca por debajo de 0).',
        'acoplamiento': 'Acoplamiento: la revelación de siempre y se sigue; sin pausa de más.',
    }
    def t30(g):
        folder = os.path.join(UNI, g)
        if os.path.isdir(folder):
            for f in sorted(os.listdir(folder)):
                if f.endswith('-t30.png'): return load(os.path.join(folder, f))
        return None
    y = row(sheet, d, y, [(t30(g), NOMBRES[g], notes[g]) for g in ('stroop', 'disparate', 'radar', 'acoplamiento')], 540, 40)
    # ---- 2. el final
    d.text((GAP, y), '2 · El final con la marca neta (393 × 873 dp)', font=font(44), fill=SUN); y += 70
    y = row(sheet, d, y, [
        (load(os.path.join(ROB, 'final-sin-record.png')), 'Sin récord', '«34 aciertos» (38 aciertos menos 4 errores) no supera el récord de 41: «Tu mejor marca sigue en 41 aciertos…». Las últimas 5 y la semana también son marcas netas.'),
        (load(os.path.join(ROB, 'final-marca-nueva.png')), 'Marca nueva', '47 aciertos menos 4 errores son 43: supera el récord de 41, con su sello. La frase de debajo de la cifra dice de dónde sale.'),
        (load(os.path.join(ROB, 'final-sin-errores.png')), 'Sin errores', 'Sin errores la frase es «44 aciertos sin errores, en 2 minutos de Contrarreloj» (no «menos 0 errores»).'),
    ], 540, 40)
    # ---- 3. el azar
    d.text((GAP, y), '3 · La prueba del azar con la marca neta: adivinar pierde', font=font(44), fill=SUN); y += 70
    rows = []
    if os.path.exists(AZAR):
        for line in open(AZAR, encoding='utf-8').read().splitlines():
            parts = line.split('\t')
            if len(parts) == 7 and parts[0] != 'juego': rows.append(parts)
    heads = ('juego', 'cuidado (media)', 'azar (media)', 'azar (p90)', 'azar/cuidado', 'resultado')
    xs = [GAP, GAP + 400, GAP + 820, GAP + 1240, GAP + 1660, GAP + 2000]
    for x, h in zip(xs, heads): d.text((x, y), h, font=font(28), fill=DIM)
    y += 46
    for r in rows:
        old = ANTES_AZAR.get(r[0])
        vals = (NOMBRES.get(r[0], r[0]), r[1], r[2], r[3], r[5], r[6])
        for x, v in zip(xs, vals): d.text((x, y), v, font=font(32), fill=CREAM)
        if old:
            d.text((xs[4] + 150, y + 6), '(antes %s)' % old[2], font=font(24, False), fill=GREY)
        y += 46
    y += 8
    y = wrap(d, GAP, y, 'Se exige, en los cuatro: media del azar < 70 % de la del cuidado y p90 del azar < media del cuidado. Con la marca neta cada error resta lo que un acierto suma: en los juegos de cuatro opciones (y en Rescate) el azar se queda en 0 por el piso; en los de dos opciones es una caminata sin rumbo (media 0; con el piso, ~2,5) y el mínimo antes de responder limita cuántas veces prueba. El margen más corto de antes (Acoplamiento, 59 %) pasó a 15 %.', font(26, False), DIM, 150, 36)
    y += 24
    # ---- 4. el círculo blanco
    d.text((GAP, y), '4 · El «círculo blanco» de Tinta o Palabra: la píldora de mensajes, antes y después (cuadro a cuadro, 50 ms cada uno)', font=font(44), fill=SUN); y += 70
    def crop(im): return None if im is None else im.crop((0, 380, 1080, 2000))
    pairs = [('f000', 'el primer cuadro de la partida'), ('f001', 'el cuadro siguiente')]
    antes = [frame_named(ANTES, p[0]) for p in pairs]; despues = [frame_named(DESPUES, p[0]) for p in pairs]
    items = []
    for (stem, what), a, b in zip(pairs, antes, despues):
        items.append((crop(a), 'ANTES · cuadro ' + stem[1:], what + ': bajo la tarjeta, un DISCO crema con un punto blanco (la píldora de mensajes sin acomodar). Se queda hasta la primera respuesta.'))
        items.append((crop(b), 'DESPUÉS · cuadro ' + stem[1:], what + ': la píldora nace oculta y aparece con su primer mensaje («¡Bien!», «La tinta era…»), ya con su tamaño y su color.'))
    y = row(sheet, d, y, items, 520, 38)
    for line in ('QUÉ ERA: la píldora de mensajes (`PhasePill`, solo la usa Tinta o Palabra: por eso la Tarea 170, que barrió los juegos con chispas, no la vio). Se activaba al empezar la partida pero recibía su primer texto, color y tamaño recién con la primera respuesta: hasta entonces era un objeto de 100 × 100 sin acomodar, con su losa de plasticina cruda (158 × 169 px, crema) y su punto blanco de 33 px, un disco crema bajo la tarjeta durante todo el primer ensayo (con el Contrarreloj y sin él: se ve en cada partida). Los aros blancos y las chispas que se abren desde el botón en cada respuesta son el diseño del juego y NO se tocaron.',
                 'ARREGLO EN LA CAUSA: `PhasePill` nace oculta (con su tamaño mínimo, no los 100 × 100 de fábrica) y se muestra con su primer `Set`; `Clear()` la oculta al empezar otra partida (antes quedaba el mensaje de la anterior).',
                 'POR QUÉ NO LO VIO EL VIGILANTE AL PRINCIPIO: buscaba imágenes blancas de 100 × 100 en el centro; la píldora estaba posicionada, con su losa estirada sobre un padre de 100 × 100 sin acomodar. La guardia final tiene dos reglas: A (imagen blanca de fábrica sin arte con nombre) y C (imagen ESTIRADA sobre un padre de 100 × 100, en cualquier posición). CONTROL DE PUNTA A PUNTA: con el arreglo quitado, el smoke de Tinta FALLA señalando `PhasePill/Slab` [C]; con él, 0 marcas en 52 corridas dirigidas (los juegos que más marcaban y todas sus Pantalla, Tutorial, Aviso, Seguidos, HowTo, Reto, Mudo y Contrarreloj) y 0 en 108.072 cuadros de una partida de 110 s en Contrarreloj. El punto medio «ella o su padre en estado COMPLETO» no ve la píldora real (ya estaba posicionada) y se descartó.',
                 'GUARDIA MÁS ANCHA: la prueba de lectura de código no se puede ensanchar a todos los métodos (se probó: decenas de falsos «ofensores»: los aros y destellos se arman con la escena y se encienden desde otro método, y la píldora ni siquiera era un efecto). En su lugar el smoke corre `FactoryImageGuard` cada 3 cuadros de CADA corrida de los 17 juegos (sin mirar los primeros 5 s ni la cuenta regresiva) y mira lo que realmente se ve; `FactoryImageGuardTests` prueba que detecta, con la jerarquía real de la píldora, y `PhasePillTests` prueba que nace oculta.'):
        y = wrap(d, GAP, y, '• ' + line, font(26, False), CREAM, 150, 36) + 10
    return sheet.crop((0, 0, W, y + 30))


if __name__ == '__main__':
    im = build(); os.makedirs(os.path.dirname(OUT), exist_ok=True); im.save(OUT, optimize=True); print('ok', OUT, im.size)
