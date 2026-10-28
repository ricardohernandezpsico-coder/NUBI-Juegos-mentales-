# -*- coding: utf-8 -*-
"""Lámina de la Tarea 185, «La pestaña Tú» (`docs/previews/tarea-185.png`), con CAPTURAS REALES; no son maquetas.
  1. Tú, hoy | propuesta: arriba SOLO Nubi (la isla salió de la app), los cuatro cuadros en sus DOS variantes (V1 «Centrado» y V2 «Ícono a la izquierda»), letra 1,0 y 1,3.
  2. Tú entera hasta «Contacto» (con «Cómo juegas», «Más ajustes», «En qué se basa Nubi» y «Contacto») y deslizada hasta Contacto en el emulador.
  3. Ajustes, hoy | cuatro losas; el recordatorio encendido y apagado (las horas apagadas con «Enciéndelo en Tú»).
  4. «En qué se basa Nubi»: al principio, en el medio y al final (emulador), letra 1,0 y 1,3.
  5. La celebración de una medalla, antes (con «Y en tu isla aparece: …») y ahora; el cierre del Primer vuelo, antes (la isla) y ahora (Nubi).
  6. Letra 2,0 en el emulador `medium_phone`, y las medidas de cabida.
Entradas (fuera de git): `app/src/test/screenshots/tarea-185/{hoy,propuesta}/` (`Tarea185CapturasTest`; «hoy» se grabó con el código de antes de la tarea), `unity/test-results/t185-emu-*.png`, `tools/previews/tarea_185_medidas.txt`.
Uso:  python -I tools/previews/tarea_185.py
"""
import os, sys, textwrap
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SHOTS = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots')
HOY = os.path.join(SHOTS, 'tarea-185', 'hoy'); PROP = os.path.join(SHOTS, 'tarea-185', 'propuesta')
EMU = os.path.join(ROOT, 'unity', 'test-results')
MEDIDAS = os.path.join(HERE, 'tarea_185_medidas.txt')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-185.png')
NAVY, SUN, DIM, CREAM = (20, 18, 63), (255, 203, 69), (200, 196, 235), (245, 235, 219)
GAP = 36
W = 2400
DENSITY = 2.75                                  # px por dp de las capturas de Roborazzi (440 dpi)


def font(px, bold=True, rubik=False):
    return ImageFont.truetype(os.path.join(FONTS, 'rubik_bold.ttf' if rubik else ('fredoka.ttf' if bold else 'nunito_regular.ttf')), int(px))


def load(path): return Image.open(path).convert('RGB') if path and os.path.exists(path) else None


def fit_w(im, w): return im.resize((w, round(im.height * w / im.width)), Image.LANCZOS)


def crop_dp(im, top_dp, bottom_dp):
    """Recorta una franja de una captura alta de Roborazzi, en dp (la captura es de 393 dp de ancho)."""
    if im is None: return None
    return im.crop((0, round(top_dp * DENSITY), im.width, min(im.height, round(bottom_dp * DENSITY))))


def wrap(d, x, y, text, f, fill, width_chars, step):
    for line in textwrap.wrap(text, width_chars):
        d.text((x, y), line, font=f, fill=fill); y += step
    return y


def title(d, y, text):
    d.text((GAP, y), text, font=font(46, rubik=True), fill=SUN); return y + 74


def row(sheet, d, y, items, w, caption_chars=34, crop_h=None):
    """items = [(imagen, rótulo, nota)]; todas a la misma anchura, alineadas por arriba."""
    n = len(items); gap = (W - 2 * GAP - n * w) // max(1, n - 1) if n > 1 else 0
    x = GAP; tallest = 0
    for im, label, note in items:
        if im is not None:
            fi = fit_w(im, w)
            if crop_h: fi = fi.crop((0, 0, fi.width, min(fi.height, crop_h)))
            sheet.paste(fi, (x, y)); tallest = max(tallest, fi.height)
            d.rectangle((x - 1, y - 1, x + fi.width, y + fi.height), outline=(70, 66, 120), width=2)
        else:
            d.rectangle((x, y, x + w, y + 300), outline=DIM); d.text((x + 16, y + 16), 'sin captura', font=font(26, False), fill=DIM); tallest = max(tallest, 300)
        x += w + gap
    x = GAP; ly = y + tallest + 10; end = ly
    for im, label, note in items:
        d.text((x, ly), label, font=font(28), fill=SUN)
        end = max(end, wrap(d, x, ly + 38, note, font(23, False), DIM, caption_chars, 29)); x += w + gap
    return end + 40


def hoy(name): return load(os.path.join(HOY, name + '.png'))
def prop(name): return load(os.path.join(PROP, name + '.png'))
def emu(name): return load(os.path.join(EMU, 't185-emu-' + name + '.png'))


def build():
    sheet = Image.new('RGB', (W, 20000), NAVY); d = ImageDraw.Draw(sheet)
    d.text((GAP, 24), 'La pestaña Tú (Tarea 185)', font=font(66, rubik=True), fill=SUN)
    y = 118
    for line in ('Tú sigue la propuesta B «Lo tuyo en cuadros» de Fable con las decisiones de Ricardo del 27-oct: arriba SOLO Nubi (la isla sale de la app, y con ella la línea «Y en tu isla aparece: …» de la medalla), los cuatro cuadros con datos reales y su unidad en DOS variantes '
                 'para elegir, «Cómo juegas» con el sonido, la vibración y el recordatorio a mano, «Más ajustes», «En qué se basa Nubi» y «Contacto» al final. Una sola puerta por ajuste: esos tres interruptores viven SOLO en Tú; Ajustes queda en cuatro losas. '
                 'Los cuadros no se tocan, por eso van PLANOS (crema lisa, sin borde ni sombra de arcilla: lo tocable es arcilla y lo informativo es plano). Todo son capturas reales (Roborazzi a 393 × 873 dp y el emulador).',):
        y = wrap(d, GAP, y, line, font(27, False), DIM, 150, 38)
    y += 16

    y = title(d, y, '1 · Tú arriba: hoy | V1 «Centrado» | V2 «Ícono a la izquierda» (letra 1,0)')
    y = row(sheet, d, y, [
        (hoy('tu'), 'HOY', 'La isla con sus piezas, el nombre y tres cifras sin unidad: «21 Racha · 41 Partidas · 9 Medallas».'),
        (prop('tu-v1-1_0'), 'V1 · CENTRADO', 'Ícono del kit arriba al centro (40 dp), la cifra grande, su unidad en una frase y la línea de apoyo, todo centrado.'),
        (prop('tu-v2-1_0'), 'V2 · ÍCONO A LA IZQUIERDA', 'El ícono grande (44 dp) a la izquierda; a su derecha la cifra con su unidad debajo; la línea de apoyo abajo, a todo el ancho.'),
    ], 700, 40)
    y = title(d, y, '2 · Con letra 1,3: ninguna palabra partida (V2 pasa a una columna desde 1,1)')
    y = row(sheet, d, y, [
        (hoy('tu-letra13'), 'HOY · 1,3', ''),
        (prop('tu-v1-1_3'), 'V1 · 1,3', 'Sigue en dos columnas, los cuatro de la misma altura.'),
        (prop('tu-v2-1_3'), 'V2 · 1,3', 'Una columna: con dos, «seguidos» se partiría.'),
    ], 700, 40)

    y = title(d, y, '3 · Tú entera hasta Contacto (V1)')
    full1, full2, fullh = prop('tu-v1-completa'), prop('tu-v2-completa'), hoy('tu-completa')
    y = row(sheet, d, y, [
        (crop_dp(fullh, 0, 790), 'HOY · entera', 'Una pantalla y media: la isla, las cifras, las medallas y la losa «Ajustes».'),
        (crop_dp(full1, 0, 790), 'V1 · arriba', ''),
        (crop_dp(full1, 760, 1500), 'V1 · en medio', '«Cómo juegas» trae los tres interruptores; abajo, los dos botones que abren paneles.'),
        (crop_dp(full1, 1380, 1990), 'V1 · hasta Contacto', '«Escribir a Nubi» dice «Correo por definir» y no hace nada hasta que Ricardo cree el correo.'),
    ], 520, 34)
    cont = emu('contacto')
    if cont is not None:
        y = row(sheet, d, y, [(cont, 'EMULADOR · Tú deslizada hasta Contacto', 'Con la barra de pestañas encima, en un teléfono de verdad (Android 16).')], 520, 34)

    y = title(d, y, '4 · Ajustes: hoy (nueve bloques) | cuatro losas')
    y = row(sheet, d, y, [
        (crop_dp(hoy('ajustes-completa'), 0, 1500), 'HOY · arriba', 'Meta, «Cómo juegas» (edad, estudios, sonido, vibración), recordatorio, privacidad, aviso de salud, informe, licencias y restablecer: unos 1 900 dp.'),
        (crop_dp(prop('ajustes-completa-encendido'), 0, 1500), 'AHORA · arriba', 'JUEGO (meta, edad, estudios en una línea con «Cambiar», animaciones) y RECORDATORIO (solo la hora y «Probar el aviso»).'),
        (crop_dp(prop('ajustes-completa-encendido'), 1250, 1860), 'AHORA · abajo', 'TUS DATOS (tu progreso es tuyo y restablecer) y ACERCA DE NUBI (licencias y aviso de salud). Sin política de privacidad todavía.'),
    ], 700, 40)
    y = row(sheet, d, y, [
        (crop_dp(prop('ajustes-completa-encendido'), 735, 1200), 'RECORDATORIO ENCENDIDO', 'Se elige la hora y se puede probar el aviso («Aviso de prueba enviado» sale 2 s al lado, sin Toast).'),
        (crop_dp(prop('ajustes-completa-apagado'), 735, 1200), 'RECORDATORIO APAGADO', 'Las horas se ven a media luz y no se tocan; una línea dice dónde encenderlo: «Enciéndelo en Tú, en «Cómo juegas»».'),
        (crop_dp(prop('ajustes-1_3'), 0, 873), 'AJUSTES · 1,3', 'Con letra 1,3 cada losa crece sin cortar nada.'),
    ], 700, 40)

    y = title(d, y, '5 · «En qué se basa Nubi»: al principio y al final (letra 1,0 y 1,3)')
    y = row(sheet, d, y, [
        (prop('fuentes-inicio-1_0'), 'AL PRINCIPIO · 1,0', 'Dos frases de encuadre y, por área, una losa por juego: emblema, nombre, qué tomamos (una línea) y «Autor, año · Ver el estudio ›».'),
        (emu('fuentes-inicio'), 'AL PRINCIPIO · emulador', 'Tocar una losa abre el estudio en el navegador (https://doi.org/…): se probó en el emulador y abre Chrome. Sin permiso de internet.'),
        (emu('fuentes-final'), 'AL FINAL · emulador', 'Cómo se ajusta la dificultad (tres estudios), las palabras, y «Lo que no decimos» con el aviso de salud.'),
        (prop('fuentes-inicio-1_3'), 'AL PRINCIPIO · 1,3', ''),
    ], 520, 34)

    y = title(d, y, '6 · Sin la isla: la celebración de una medalla y el cierre del Primer vuelo (antes | ahora)')
    y = row(sheet, d, y, [
        (hoy('logro-con-pieza'), 'MEDALLA · ANTES', 'Una medalla de racha sumaba «Y en tu isla aparece: dos arbustos», con la pieza chica y un sonido.'),
        (load(os.path.join(SHOTS, 'logro-sin-pieza-racha.png')), 'MEDALLA · AHORA', 'La misma medalla, sin la línea ni el sonido: la medalla, su nombre, «6 de 15 medallas» y los botones.'),
        (hoy('primer-vuelo-3'), 'CIERRE DEL VUELO · ANTES', 'La última página mostraba «Tu isla, la que verás en Tú».'),
        (prop('cierre'), 'CIERRE DEL VUELO · AHORA', 'Nubi despierta y parpadeando, grande y centrada; las mismas dos líneas, sin decir «isla».'),
    ], 520, 34, crop_h=1100)

    y = title(d, y, '7 · Letra 2,0 (emulador)')
    e2 = [emu('2_0-tu'), emu('2_0-contacto'), emu('2_0-ajustes'), emu('2_0-fuentes')]
    y = row(sheet, d, y, [
        (e2[0], 'TÚ · 2,0', 'Los cuadros pasan a una columna: ninguna palabra partida.'),
        (e2[1], 'CONTACTO · 2,0', ''),
        (e2[2], 'AJUSTES · 2,0', ''),
        (e2[3], 'EN QUÉ SE BASA · 2,0', ''),
    ], 520, 34)

    y = title(d, y, '8 · Medidas de cabida (dp; la barra de pestañas empieza a 785 con 393 × 873)')
    if os.path.exists(MEDIDAS):
        for line in open(MEDIDAS, encoding='utf-8').read().splitlines():
            if line.strip(): y = wrap(d, GAP, y, line, font(25, False), CREAM, 175, 34)
    y += 20
    sheet = sheet.crop((0, 0, W, y + 20)); sheet.save(OUT, optimize=True)
    print('lámina:', OUT, sheet.size)


if __name__ == '__main__':
    build()
