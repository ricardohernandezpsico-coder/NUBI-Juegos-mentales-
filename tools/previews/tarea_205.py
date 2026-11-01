# -*- coding: utf-8 -*-
"""Lámina «antes (T203b) | ahora» de la Tarea 205 (1-nov-2026): la app lee la REFERENCIA ESTIMADA (`assets/referencia/*.json`, Tarea 204) y la tienda también compara.
«ahora» sale de `Tarea205CapturasTest` (`app/src/test/screenshots/t205/`, tamaño real); «antes» es la página de la Tarea 203b (`t203b/`, la que comparaba solo con datos de muestra en depuración).
Filas: 1) arriba de la página (393 · 1,0, 393 · 2,0, 360 · 1,0, 360 · 2,0); 2) cifra, escalera, campana; 3) otras edades, semanas y sin comparación; 4) Rescate relámpago (de a 1 acierto) y Acoplamiento (de a 2);
5) la puerta de Avance y la hoja del candado (393 · 1,0 y 2,0, y 360); 6) el final de «[Debug] Contrarreloj de invitado».
Uso:  python -I tools/previews/tarea_205.py      → docs/previews/tarea-205.png
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SHOTS = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-205.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
NAVY, SUN, DIM, LINE, HOYC = (20, 18, 63), (255, 203, 69), (200, 196, 235), (90, 86, 160), (170, 166, 210)
GAP, MARGIN, COLW = 36, 24, 393

# (carpeta, captura, título, ancho dp, qué se ve); las filas tienen hasta 3 columnas de 393 dp (las de 360 dp se muestran a su ancho real)
ROWS = [
    [('t203b', 'ahora_top_393_20', 'antes · T203b · arriba · letra 2,0', 393,
      'solo en la versión de prueba: «Estos son datos de muestra: solo se ven en la versión de prueba.» bajo el rótulo; en la tienda la página no traía comparación'),
     ('t205', 'ahora_top_393_20', 'ahora · arriba · 393 × 873 · letra 2,0', 393,
      'la tienda compara: bajo el rótulo, UNA vez, «Referencia estimada para 45 a 54 años: sale de estudios por edad y de un programa que jugó esta misma pista en un computador, no en tu teléfono. No son personas.»; con letra 2,0 se lee entera y el rótulo parte sin raya'),
     ('t205', 'ahora_top_393_10', 'ahora · arriba · 393 × 873 · letra 1,0', 393,
      'la cifra con «marcas de la referencia estimada para 45 a 54 años»; la división y «Percentil N» como siempre; nada llama «personas» a la referencia')],
    [('t203b', 'ahora_top_360_10', 'antes · T203b · arriba · 360 × 780', 360,
      'la cabecera de la 203b a 360 dp con letra 1,0'),
     ('t205', 'ahora_top_360_10', 'ahora · arriba · 360 × 780 · letra 1,0', 360,
      'la frase larga en 4 líneas a 360 dp; los seis títulos de losa siguen en UNA línea (lo mide `ContrarrelojPageTest`)'),
     ('t205', 'ahora_top_360_20', 'ahora · arriba · 360 × 780 · letra 2,0', 360,
      'letra 2,0 a 360 dp: la frase larga y el rótulo parten por palabras, sin choques (`comparacion_estimada_cero_choques_a_360_con_letra_2_0`)')],
    [('t203b', 'ahora_ladder_393_10', 'antes · T203b · la escalera', 393,
      'el pie decía «Con las 128 personas de tu franja que jugaron esta semana.»'),
     ('t205', 'ahora_ladder_393_10', 'ahora · la escalera de Tinta o Palabra', 393,
      'los peldaños salen de la tabla del juego (de a 5 aciertos, 16 tramos); el pie dice «Peldaños de la referencia estimada para 45 a 54 años; no cambian de semana en semana.» y no cuenta personas'),
     ('t205', 'ahora_ladder_foot_360_10', 'ahora · «Te faltan…» · 360 × 780', 360,
      'el mismo pie a 360 dp; «Te faltan N aciertos para …» sigue sin partirse por dentro')],
    [('t203b', 'ahora_bell_393_10', 'antes · T203b · la campana', 393,
      '«La campana de tu edad» y «La línea marca la mitad de las personas.»'),
     ('t205', 'ahora_bell_393_10', 'ahora · «La campana de tu edad»', 393,
      'el título es el mismo: «La campana estimada de tu edad» partía en dos líneas (56 dp de alto, incluso a 393 dp y letra 1,0); «referencia estimada» va en su frase: «La línea marca la mitad de la referencia estimada.»'),
     ('t205', 'ahora_bell_360_10', 'ahora · la campana · 360 × 780', 360,
      'a 360 dp el título sigue en una línea (lo mide `ContrarrelojPageTest`)')],
    [('t203b', 'ahora_others_393_10', 'antes · T203b · «Otras edades»', 393,
      'con «Todas las edades» y la línea «Las demás edades aparecen cuando lleguen a 50 personas.»'),
     ('t205', 'ahora_others_393_10', 'ahora · «Otras edades»', 393,
      'sin «Todas las edades» y sin la línea de lo que falta: las seis franjas, cada una con su fila, y «De cada 100 marcas de la referencia estimada de cada edad, tu mejor marca deja detrás a:»'),
     ('t205', 'ahora_weeks_393_10', 'ahora · «Tu percentil semanal»', 393,
      'la tabla no cambia de semana en semana; la frase dice «comparadas con la referencia estimada»; «la mitad» en su raya')],
    [('t205', 'ahora_radar_top_393_10', 'ahora · Rescate relámpago · arriba', 393,
      'Rescate cuenta de a 1 acierto: la cifra, la división y el percentil salen de su tabla de 24 tramos'),
     ('t205', 'ahora_radar_ladder_393_10', 'ahora · Rescate · la escalera', 393,
      'los peldaños de Rescate son de a 1 acierto (la mitad de la gente cae entre 12 y 14): «hasta 12 aciertos», «desde 13», «desde 15»; «En la Luna» NO tiene peldaño propio (el percentil 75 y el 90 se alcanzan con el mismo acierto, y una división vacía no se dibuja)'),
     ('t205', 'ahora_radar_bell_393_10', 'ahora · Rescate · la campana', 393,
      'el eje dice 0 · 8 · 15 · 23 o más y la barra «Tú» cae en el centro de SU tramo (se mide en `ContrarrelojPageTest`)')],
    [('t205', 'ahora_acop_bell_393_10', 'ahora · Acoplamiento · la campana', 393,
      'Acoplamiento cuenta de a 2 aciertos: el eje dice 0 · 10 · 20 · 30 o más'),
     ('t203b', 'ahora_nocompare_393_10', 'antes · T203b · sin comparación', 393,
      'sin tabla para la pista de un juego, o de 13 a 17 años: las mismas tres cifras, sin rótulos'),
     ('t205', 'ahora_nocompare_393_10', 'ahora · sin comparación', 393,
      'igual que antes: la página solo con «Tus marcas»')],
    [('t203b', 'ahora_avance_sinplus_393_10', 'antes · T203b · la puerta, sin Plus', 393,
      'la frase gratis solo salía en la versión de prueba: «Esta semana estás En órbita. Te faltan 7 aciertos para En la Luna.»'),
     ('t205', 'ahora_avance_sinplus_393_10', 'ahora · la puerta, sin Plus', 393,
      'también en la tienda, con la referencia dicha: «Esta semana estás En la plataforma en la referencia estimada para tu edad. Te faltan 5 aciertos para Despegando.» (la marca sembrada es la del juego del día; a 360 dp la frase pasa a seis líneas y no parte ningún nombre de división)'),
     ('t205', 'ahora_avance_sinplus_393_20', 'ahora · la puerta · letra 2,0', 393,
      'con letra 2,0 la frase gratis sigue sin partir los nombres de división ni «Te faltan 5 aciertos»')],
    [('t203b', 'ahora_sheet_393_10', 'antes · T203b · la hoja del candado', 393,
      'con la comparación de muestra la hoja hablaba de «personas de tu edad» y cerraba con «Entendido»'),
     ('t205', 'ahora_sheet_393_10', 'ahora · la hoja · 393 × 873 · letra 1,0', 393,
      'sin promesas: «Tu lugar en una referencia estimada para tu edad: cuántas de cada 100 marcas superas y en qué división estás.»; ningún texto dice que luego se compare con personas; el botón es «Cerrar»'),
     ('t205', 'ahora_sheet_393_20', 'ahora · la hoja · letra 2,0', 393,
      'letra 2,0: la hoja cabe entera y «Cerrar» queda a la vista')],
    [('t205', 'ahora_avance_sinplus_360_10', 'ahora · la puerta · 360 × 780', 360,
      'la puerta a 360 dp'),
     ('t205', 'ahora_sheet_360_10', 'ahora · la hoja · 360 × 780', 360,
      'la hoja a 360 dp'),
     ('t205', 'ahora_guest_393_10', 'ahora · «[Debug] Contrarreloj de invitado»', 393,
      'el final de una partida de invitado: solo la marca, cómo se cuenta y «Esta partida no se guardó: ni récord, ni racha, ni la semana. Anota tu marca.»; no guarda nada')],
    [('t205', 'ahora_weeks7_360_20', 'ahora · siete semanas · 360 × 780 · letra 2,0', 360,
      'con la referencia las siete semanas se comparan siempre con la misma tabla; las cifras mantienen 8 dp entre columnas')],
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


def main():
    d0 = ImageDraw.Draw(Image.new('RGB', (10, 10)))
    tf, sf = font(17), font(15, bold=False)
    sheet_w = MARGIN * 2 + 3 * COLW + 2 * GAP
    def path(folder, key): return os.path.join(SHOTS, folder, key + '.png')
    def real_h(folder, key, w):
        im = Image.open(path(folder, key))
        return int(round(im.height * w / im.width))
    heights = []
    for row in ROWS:
        shots = max(real_h(f, k, w) for f, k, _, w, _ in row)
        cap = max(len(wrap(d0, t, sf, COLW)) * 20 for _, _, _, w, t in row)
        heights.append(26 + shots + 12 + cap + 34)
    sheet = Image.new('RGB', (sheet_w, MARGIN + sum(heights)), NAVY)
    d = ImageDraw.Draw(sheet)
    y = MARGIN
    for row, hrow in zip(ROWS, heights):
        x = MARGIN
        for folder, key, title, w, text in row:
            d.text((x, y), title, font=tf, fill=HOYC if title.startswith('antes') else SUN)
            im = Image.open(path(folder, key)).convert('RGB')
            h = int(round(im.height * w / im.width))
            sheet.paste(im.resize((w, h), Image.LANCZOS), (x, y + 26))
            d.rectangle((x - 1, y + 25, x + w, y + 26 + h), outline=LINE)
            cy = y + 26 + h + 12
            for i, line in enumerate(wrap(d, text, sf, COLW)):
                d.text((x, cy + i * 20), line, font=sf, fill=DIM)
            x += COLW + GAP
        y += hrow
    sheet.save(OUT)
    print(OUT, sheet.size)


main()
