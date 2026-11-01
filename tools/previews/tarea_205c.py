# -*- coding: utf-8 -*-
"""Lámina «antes | ahora» de la Tarea 205c (1-nov-2026): letra 2,0 sin palabras partidas por dentro (la puerta de Avance y la escalera), la hoja del candado a 2,0 (la de la Tarea 205 era idéntica a la de 1,0: un `Dialog` trae su propia densidad) y la pregunta de edad del «[Debug] Contrarreloj de invitado».
Las capturas con el nombre `emu_*` son del EMULADOR `medium_phone` (API 36, 393 y 360 dp, letra 2,0 con `settings put system font_scale 2.0`; ver `tools/emulador_ui.py`), con la app de la T205b («antes») y la de esta tarea («ahora»); las demás salen de `Tarea205cCapturasTest` y `Tarea205CapturasTest` (Roborazzi).
Todo en `app/src/test/screenshots/t205c/` (y `t205/` para la hoja vieja). Uso:  python -I tools/previews/tarea_205c.py      → docs/previews/tarea-205c.png
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SHOTS = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-205c.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
NAVY, SUN, DIM, LINE, HOYC = (20, 18, 63), (255, 203, 69), (200, 196, 235), (90, 86, 160), (170, 166, 210)
GAP, MARGIN, COLW = 36, 24, 393

# (carpeta, captura, ancho dp, alto recortado dp o None, título, qué se ve); cada fila trae hasta 3 columnas
ROWS = [
    [('t205c', 'emu_antes_door_360_20_n12', 360, 800, 'antes · T205b · la puerta · 360 dp · letra 2,0',
      'en el emulador: «Te faltan 12 / acier / tos para Despegando.» mide ~300 dp pegado y el renglón tiene ~246: Android lo corta por letras'),
     ('t205c', 'emu_ahora_door_360_20_n12', 360, 800, 'ahora · T205c · la puerta · 360 dp · letra 2,0',
      'con letra mayor a 1,3 los nombres de división y «Te faltan» parten entre palabras; «12 aciertos» (la cifra con su unidad) sigue junto'),
     ('t205c', 'emu_antes_door_393_20_n12', 393, 800, 'antes · T205b · la puerta · 393 dp · letra 2,0',
      'a 393 dp «Te faltan 12 aciertos» cabe justo (~276 dp en ~279): el corte aparece a 360 dp y con dos cifras. Con 1,0 y 1,3 sigue todo pegado (cabe)')],
    [('t205c', 'emu_antes_ladder_360_20_n15', 360, 800, 'antes · T205b · la escalera · 360 dp · letra 2,0',
      'la escalera YA cabía (su texto va con tope de letra 1,3): «Te faltan 15 aciertos» queda en un renglón; Fable midió ~361 dp con una cuenta que no es la real'),
     ('t205c', 'emu_ahora_ladder_360_20_n15', 360, 800, 'ahora · T205c · la escalera · 360 dp · letra 2,0',
      'igual que antes: el arreglo no la toca (dentro del tope de letra se sigue pegando) y la prueba `ContrarrelojLetraTest` mira que ningún renglón de la página termine dentro de una palabra con 1,0, 1,3 y 2,0')],
    [('t205c', 'emu_antes_sheet_393_20_top', 393, 873, 'antes · T205b · la hoja a 2,0 en el emulador (la de verdad)',
      'un Dialog a 2,0 llena la pantalla: el título y el encabezado ocupan ~60 % y «Cerrar» queda más abajo'),
     ('t205c', 'emu_antes_sheet_393_20_end', 393, 873, 'antes · T205b · la hoja, deslizada hasta «Cerrar»',
      'la hoja se desliza: «Cerrar» se alcanza deslizando (en el emulador, con un gesto)'),
     ('t205', 'ahora_sheet_393_20', 393, 873, 'antes · T205 · captura de Roborazzi «a 2,0»',
      'era IDÉNTICA a la de 1,0 píxel a píxel: el Dialog ignoraba el cambio de densidad de la prueba; no probaba nada')],
    [('t205c', 'ahora_sheet_393_20_top', 393, 873, 'ahora · T205c · el contenido sin Dialog a 2,0',
      '`PlusLockSheetContent` se compone suelto: la prueba sí cambia la letra. Con 2,0 se lee lo mismo que en el emulador'),
     ('t205c', 'ahora_sheet_393_20_end', 393, 873, 'ahora · T205c · deslizada hasta «Cerrar»',
      '«Cerrar» se alcanza con `performScrollTo` (la prueba lo exige) y cierra la hoja'),
     ('t205c', 'ahora_sheet_393_10', 393, 873, 'ahora · T205c · a 1,0',
      'con 1,0 y 1,3 «Cerrar» queda a la vista sin deslizar; con 2,0 hay que deslizar (`ContrarrelojLetraTest`)')],
    [('t205c', 'emu_ahora_guest_age_393_10', 393, 873, 'ahora · T205c · invitado: la pregunta de edad',
      'en el emulador: al tocar el botón de un juego pregunta la EDAD de la persona (las seis franjas de 18 en adelante; nunca 13 a 17): juega con la banda de esa edad y no se guarda nada'),
     ('t205c', 'ahora_guest_393_10', 393, 873, 'ahora · T205c · el final de invitado',
      'dice la marca, cómo se cuenta, «Edad elegida: 65 a 74 años.» para anotarla, y que no se guardó; «Otra vez» repite con la misma edad')],
]


def font(px, bold=True): return ImageFont.truetype(os.path.join(FONTS, 'fredoka.ttf' if bold else 'nunito_regular.ttf'), int(px))


def wrap(draw, text, f, width):
    lines, cur = [], ''
    for word in text.split(' '):
        t = (cur + ' ' + word).strip()
        if draw.textlength(t, font=f) <= width: cur = t
        else: lines.append(cur); cur = word
    if cur: lines.append(cur)
    return lines


def load(folder, key, w, crop_h):
    im = Image.open(os.path.join(SHOTS, folder, key + '.png')).convert('RGB')
    h = int(round(im.height * w / im.width))
    im = im.resize((w, h), Image.LANCZOS)
    return im.crop((0, 0, w, min(h, crop_h))) if crop_h else im


def main():
    d0 = ImageDraw.Draw(Image.new('RGB', (10, 10)))
    tf, sf = font(17), font(15, bold=False)
    sheet_w = MARGIN * 2 + 3 * COLW + 2 * GAP
    heights = []
    for row in ROWS:
        shots = max(load(f, k, w, c).height for f, k, w, c, _, _ in row)
        cap = max(len(wrap(d0, t, sf, COLW)) * 20 for _, _, _, _, _, t in row)
        heights.append(26 + shots + 12 + cap + 34)
    sheet = Image.new('RGB', (sheet_w, MARGIN + sum(heights)), NAVY)
    d = ImageDraw.Draw(sheet)
    y = MARGIN
    for row, hrow in zip(ROWS, heights):
        x = MARGIN
        for folder, key, w, crop, title, text in row:
            d.text((x, y), title, font=tf, fill=HOYC if title.startswith('antes') else SUN)
            im = load(folder, key, w, crop)
            sheet.paste(im, (x, y + 26))
            d.rectangle((x - 1, y + 25, x + w, y + 26 + im.height), outline=LINE)
            cy = y + 26 + im.height + 12
            for i, line in enumerate(wrap(d, text, sf, COLW)):
                d.text((x, cy + i * 20), line, font=sf, fill=DIM)
            x += COLW + GAP
        y += hrow
    sheet.save(OUT)
    print(OUT, sheet.size)


main()
