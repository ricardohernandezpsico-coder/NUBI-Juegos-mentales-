# -*- coding: utf-8 -*-
"""Lámina de la Tarea 177, «El Contrarreloj de hoy: la app» (`docs/previews/tarea-177.png`), con CAPTURAS REALES; no son maquetas.
  1. Hoy en sus cuatro estados del día (ruta a medias, ruta completa, Contrarreloj jugado, marca nueva), a 393 × 873 dp con letra 1,0 (Roborazzi).
  2. Hoy con la pausa (letra 1,0 y 1,3), con letra 2,0 (el botón y «Todos los juegos» se apilan; se desliza y se alcanza todo) y en el teléfono más chico (360 × 780).
  3. Los cuatro juegos de la primera tanda corriendo como Contrarreloj en Unity (1080 × 2400): el chip «N aciertos» y el tiempo «1:30» del marcador, a los 30 s de partida.
  4. El final del Contrarreloj en sus tres estados (sin récord, marca nueva, primera vez) y con letra 1,3.
  5. La prueba del azar de la Tarea 176 con sus números (el margen justo: Acoplamiento, 59 % de 70 %).
Entradas: `app/src/test/screenshots/tarea-177/` (`HoyTarjetaTest.tarea_177_capturas` y `TimeTrialResultScreenTest.tarea_177_capturas`, solo al grabar), `app/src/test/screenshots/tarea-177-emulador/` (si hay: Hoy con letra 2,0 en el emulador),
`unity/test-results/capturas-contrarreloj/` (`NUBI_SHOTS_TT=1` con `ScreenshotRunner`, fuera de git) y `unity/test-results/contrarreloj-azar.txt` (la prueba del azar, fuera de git).
Uso:  python -I tools/previews/tarea_177.py
"""
import os, sys, textwrap
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
ROB = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots', 'tarea-177')
EMU = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots', 'tarea-177-emulador')
UNI = os.path.join(ROOT, 'unity', 'test-results', 'capturas-contrarreloj')
AZAR = os.path.join(ROOT, 'unity', 'test-results', 'contrarreloj-azar.txt')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-177.png')
NAVY, SUN, DIM, CREAM, OK, WARN = (20, 18, 63), (255, 203, 69), (200, 196, 235), (245, 235, 219), (110, 220, 130), (255, 150, 120)
GAP = 36
W = 2400


def font(px, bold=True, rubik=False):
    return ImageFont.truetype(os.path.join(FONTS, 'rubik_bold.ttf' if rubik else ('fredoka.ttf' if bold else 'nunito_regular.ttf')), int(px))


def load(path): return Image.open(path).convert('RGB') if os.path.exists(path) else None


def top(im, h):
    """Solo la parte de arriba de una captura (el resto es cielo vacío)."""
    return im if im is None or im.height <= h else im.crop((0, 0, im.width, h))


def fit_w(im, w): return im.resize((w, round(im.height * w / im.width)), Image.LANCZOS)


def wrap(d, x, y, text, f, fill, width_chars, step):
    for line in textwrap.wrap(text, width_chars):
        d.text((x, y), line, font=f, fill=fill); y += step
    return y


def row(sheet, d, y, items, w, caption_chars=34):
    """items: [(imagen o None, rótulo, nota)]; las pone en fila con su rótulo debajo. Devuelve el y siguiente."""
    n = len(items); gap = (W - 2 * GAP - n * w) // max(1, n - 1) if n > 1 else 0
    x = GAP; tallest = 0; notes_y = 0
    for im, label, note in items:
        if im is not None:
            fi = fit_w(im, w); sheet.paste(fi, (x, y)); tallest = max(tallest, fi.height)
        else:
            d.rectangle((x, y, x + w, y + 300), outline=DIM); d.text((x + 16, y + 16), 'sin captura', font=font(26, False), fill=DIM); tallest = max(tallest, 300)
        x += w + gap
    x = GAP; ly = y + tallest + 10; end = ly
    for im, label, note in items:
        d.text((x, ly), label, font=font(30), fill=SUN)
        e = wrap(d, x, ly + 40, note, font(24, False), DIM, caption_chars, 31)
        end = max(end, e); x += w + gap
    return end + 36


def build():
    sheet = Image.new('RGB', (W, 9200), NAVY); d = ImageDraw.Draw(sheet)
    d.text((GAP, 24), 'El Contrarreloj de hoy: la app (Tarea 177)', font=font(66, rubik=True), fill=SUN)
    y = 118
    for line in ('Capturas reales (Roborazzi a 393 × 873 dp, Unity en 1080 × 2400 y, si hay, el emulador API 36). El Contrarreloj de hoy REEMPLAZA al Desafío de hoy: mismo lugar, mismas reglas de «un solo sol a la vez», gratis y sin tope de repeticiones.',
                 'Cada día toca UN juego (Tinta o Palabra, ¿Verdad o disparate?, Rescate relámpago o Acoplamiento, por fecha) y la misma pista para todos: 120 s, sin dificultad adaptativa, siempre desde el paso 1. La marca se compara SOLO contigo.'):
        y = wrap(d, GAP, y, line, font(26, False), DIM, 150, 36)
    y += 14
    # ---- 1. Hoy, cuatro estados
    d.text((GAP, y), '1 · Hoy en sus cuatro estados del día (393 × 873 dp, letra 1,0)', font=font(44), fill=SUN); y += 70
    y = row(sheet, d, y, [
        (top(load(os.path.join(ROB, 'hoy-a-media-ruta.png')), 1900), 'La ruta sigue', 'El sol es de la ruta. «Contrarreloj de hoy» en lila, con el reloj, junto a «Todos los juegos»; «de hoy» va unido: si no cabe en una línea, queda «Contrarreloj» arriba y «de hoy» abajo.'),
        (top(load(os.path.join(ROB, 'hoy-ruta-completa.png')), 1900), 'Ruta completa', 'La tarjeta se cierra sola y el sol pasa al Contrarreloj, a todo el ancho, con «Tinta o Palabra · 2 minutos» y, en texto plano, «Tu récord: 41 aciertos.».'),
        (top(load(os.path.join(ROB, 'hoy-jugado.png')), 1900), 'Ya jugado, sin marca nueva', 'Crema, con «Tinta o Palabra · hoy 38 aciertos» y «otra vez»: ningún sol; se puede repetir.'),
        (top(load(os.path.join(ROB, 'hoy-marca-nueva.png')), 1900), 'Marca nueva', 'Recuadro PLANO lila claro (no se toca, sin sol): «Marca nueva: 43 aciertos en Tinta o Palabra.»; debajo «Otra vez» junto a «Todos los juegos».'),
    ], 540, 40)
    # ---- 2. pausa, letra grande, 360
    d.text((GAP, y), '2 · La pausa, la letra grande y el teléfono más chico', font=font(44), fill=SUN); y += 70
    emu13 = load(os.path.join(EMU, 'hoy-1_3-r1-pendiente.png'))
    emu20 = load(os.path.join(EMU, 'hoy-2_0-r1-pendiente-abajo.png'))
    items = [
        (top(load(os.path.join(ROB, 'hoy-pausa-1_0.png')), 1900), 'Con partida en pausa · 1,0', 'El sol es de «Seguir tu partida» (dentro de la tarjeta); el Contrarreloj sigue en lila, a media fila, con su reloj.'),
        (top(load(os.path.join(ROB, 'hoy-pausa-1_3.png')), 1900), 'Con partida en pausa · 1,3', 'Con letra 1,3 «Contrarreloj» ya no cabe junto al reloj: el botón lo suelta y la palabra queda entera en dos renglones. Todo Hoy cabe sin deslizar.'),
        (emu13 if emu13 is not None else load(os.path.join(ROB, 'hoy-a-media-ruta-1_3.png')), 'Letra 1,3 ' + ('(emulador, real)' if emu13 is not None else '(Roborazzi)'), 'La ruta sigue: cabe entero, sin reloj en el botón lila y con la barra de pestañas debajo.'),
        (emu20 if emu20 is not None else load(os.path.join(ROB, 'hoy-a-media-ruta-2_0-abajo.png')), 'Letra 2,0 ' + ('(emulador, deslizado)' if emu20 is not None else '(Roborazzi, deslizado)'), 'Ni sin el reloj cabe la palabra en media fila: los dos botones se apilan a todo el ancho, con el reloj. Se desliza y nada se corta ni se parte.'),
        (top(load(os.path.join(ROB, 'hoy-marca-con-ruta-a-medias-360.png')), 1950), '360 × 780 · el rincón más alto', 'Marca nueva con la ruta a medias: la tarjeta con su sol, el recuadro de la marca (dos renglones) y la fila de abajo; se pasa 45 dp de la barra y se desliza.'),
    ]
    y = row(sheet, d, y, items, 420, 28)
    # ---- 3. Unity
    d.text((GAP, y), '3 · Los cuatro juegos como Contrarreloj, en Unity (1080 × 2400, a los 30 s de partida)', font=font(44), fill=SUN); y += 70
    sel = {'stroop': '06-t30.png', 'disparate': '06-t30.png', 'radar': '04-t30.png', 'acoplamiento': '06-t30.png'}
    notes = {
        'stroop': 'Tinta o Palabra: «10 aciertos» arriba a la derecha y «1:30» en la fila de la consigna; la barra de abajo del marcador baja con el tiempo.',
        'disparate': '¿Verdad o disparate?: «6 aciertos» y «1:30»; el aviso «¡Un paso más!» marca que la pista subió de paso.',
        'radar': 'Rescate relámpago: «5 aciertos» y «1:33» (el reloj sigue durante la ronda); «2 seguidos» aparte, sin tapar nada.',
        'acoplamiento': 'Acoplamiento: «5 aciertos» y «1:30», con la fila de 8 luces y «Tiempo» del puerto, cada uno en lo suyo.',
    }
    names = {'stroop': 'Tinta o Palabra', 'disparate': '¿Verdad o disparate?', 'radar': 'Rescate relámpago', 'acoplamiento': 'Acoplamiento'}
    y = row(sheet, d, y, [(load(os.path.join(UNI, g, sel[g])), names[g], notes[g]) for g in ('stroop', 'disparate', 'radar', 'acoplamiento')], 540, 40)
    # ---- 4. el final
    d.text((GAP, y), '4 · El final del Contrarreloj (393 × 873 dp)', font=font(44), fill=SUN); y += 70
    y = row(sheet, d, y, [
        (load(os.path.join(ROB, 'final-sin-record.png')), 'Sin récord', 'La cifra y su unidad («38 aciertos en 2 minutos de Contrarreloj»), UNA frase solo contigo, el paso de la pista, tus últimas 5 partidas (la de hoy en sol, marcada «hoy») y la mejor de la semana. Sin «¿Por qué importa?».'),
        (load(os.path.join(ROB, 'final-marca-nueva.png')), 'Marca nueva', 'El sello «¡Tu mejor marca hasta hoy!» (y el sonido de récord, si los efectos están encendidos) y «Antes, tu mejor marca era 41 aciertos.».'),
        (load(os.path.join(ROB, 'final-primera-vez.png')), 'La primera vez', 'Sin récord ni sello: «Es tu primera marca en el Contrarreloj. La próxima te comparas contigo.»; no hay barras (nada con qué comparar).'),
        (load(os.path.join(ROB, 'final-marca-nueva-1_3.png')), 'Marca nueva · letra 1,3', 'Entra igual: «Otra vez» se ve sin deslizar y ninguna palabra se parte.'),
    ], 540, 40)
    # ---- 5. el azar
    d.text((GAP, y), '5 · La prueba del azar (Tarea 176): la misma pista, 200 veces con azar y 200 con cuidado', font=font(44), fill=SUN); y += 70
    rows = []
    if os.path.exists(AZAR):
        for line in open(AZAR, encoding='utf-8').read().splitlines():
            parts = line.split('\t')
            if len(parts) == 7 and parts[0] != 'juego': rows.append(parts)
    heads = ('juego', 'cuidado (media)', 'azar (media)', 'azar (p90)', 'azar/cuidado', 'resultado')
    xs = [GAP, GAP + 360, GAP + 760, GAP + 1100, GAP + 1440, GAP + 1800]
    for x, h in zip(xs, heads): d.text((x, y), h, font=font(28), fill=DIM)
    y += 46
    for r in rows:
        tight = r[0] == 'acoplamiento'
        vals = (r[0], r[1], r[2], r[3], r[5], r[6])
        for x, v in zip(xs, vals): d.text((x, y), v, font=font(32), fill=(WARN if tight and v == r[5] else CREAM))
        y += 46
    y += 8
    y = wrap(d, GAP, y, 'Se exige: media del azar < 70 % de la del cuidado y p90 del azar < media del cuidado. Los cuatro pasan con los números del anexo sin tocar. El margen más justo es Acoplamiento (59 % de un máximo de 70 %): conviene volver a mirarlo si se cambia su pista (versión 1).', font(26, False), DIM, 150, 36)
    y += 30
    d.text((GAP, y), 'Lo que cambió o hay que decidir al tocar el código real', font=font(44), fill=SUN); y += 66
    for t in (
        'Hoy: el botón del Contrarreloj tiene tres formas según la letra: con reloj (1,0), sin reloj (1,3) y apilado con «Todos los juegos» (2,0, o 1,3 en 360 dp). Se mide la palabra «Contrarreloj» con la letra del sistema y nunca se parte. «de hoy» lleva un espacio duro para que no quede «hoy» solo.',
        'La prueba de ajuste de Hoy medía con `boundsInRoot`, que RECORTA a lo que se ve: un botón a medio salir daba «termina justo en el borde». Ahora mide la posición sin recortar. A 393 × 873 caben todos los estados con letra 1,0 y 1,3. A 360 × 780 con letra 1,0 la ruta completa y el jugado SE PASABAN 16 dp: por pedido de Opus se apretó el ritmo vertical de Hoy 16 dp (solo espacios) y ya caben; se deslizan la pausa (31 dp) y la marca nueva con la ruta a medias (45 dp), con su tope en la prueba. Las capturas del emulador son anteriores a ese ajuste.',
        'El estado de Hoy sale de la tabla `time_trial_results` (Room 16, con migración y en el respaldo): pendiente, jugado o marca nueva, sin preferencia pasajera. `DailyChallenge`, su prueba y `desafio_hoy` se retiraron. Un Contrarreloj cuenta para la racha y las farolas, pero NO para los puntos, los niveles ni lo medido.',
        'Sin red, sin cuentas y sin otras personas: la marca se compara solo con tus últimas 5 partidas y tu mejor de la semana. `Features.COMPARE` sigue apagada y las siete franjas de edad quedaron anotadas para la fase 2.',
    ):
        y = wrap(d, GAP, y, '• ' + t, font(26, False), CREAM, 150, 36) + 8
    return sheet.crop((0, 0, W, y + 30))


if __name__ == '__main__':
    im = build(); os.makedirs(os.path.dirname(OUT), exist_ok=True); im.save(OUT, optimize=True); print('ok', OUT, im.size)
