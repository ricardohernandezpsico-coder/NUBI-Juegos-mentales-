# -*- coding: utf-8 -*-
"""Lámina «antes | ahora» de la Tarea 203b (1-nov-2026): los arreglos chicos de la página «Contrarreloj» (revisión de Opus y auditoría de Fable, `auditoria-fable/contrarreloj-t203-auditoria.md`), con DATOS DE MUESTRA y solo en depuración. Capturas REALES de Roborazzi a tamaño real (1 px = 1 dp):
«ahora» sale de `Tarea203bCapturasTest` (`app/src/test/screenshots/t203b/`); «antes» es la página de la Tarea 203 (`t203/`, grabada con `Tarea203CapturasTest`, ya reemplazada). Orden de cada fila: antes · ahora · ahora.
Uso:  python -I tools/previews/tarea_203b.py      → docs/previews/tarea-203b.png
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SHOTS = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-203b.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
NAVY, SUN, DIM, LINE, HOYC = (20, 18, 63), (255, 203, 69), (200, 196, 235), (90, 86, 160), (170, 166, 210)
GAP, MARGIN = 40, 24

# (carpeta, captura, título, ancho dp, qué se ve)
ROWS = [
    [('t203', 'ahora_ladder_393_10', 'antes · T203 · la escalera', 393,
      'el título ocupaba dos líneas («Las divisiones de tu franja esta semana»); «En la plataforma · desde 0 aciertos» era una celda sin sentido; el pie repetía la franja y el juego de la cifra'),
     ('t203b', 'ahora_ladder_393_10', 'ahora · «La escalera de tu franja»', 393,
      'título en UNA línea; «En la plataforma · hasta 30 aciertos» (el primer acierto de Despegando, menos uno): la escalera queda completa de 0 a 60; los nombres de las divisiones no se parten'),
     ('t203b', 'ahora_ladder_foot_393_10', 'ahora · el pie de la escalera', 393,
      '«Te faltan 7 aciertos para En la Luna.» sigue siendo lo único en negrita del texto corrido; el pie dice «Con las 128 personas de tu franja que jugaron esta semana.» (con las 4 semanas, que lo dice)')],
    [('t203', 'ahora_bell_393_10', 'antes · T203 · la campana', 393,
      '«Cómo se reparten las marcas» partía en dos líneas y la segunda frase («…no una curva teórica») hablaba con el diseñador'),
     ('t203b', 'ahora_bell_393_10', 'ahora · «La campana de tu edad»', 393,
      'título en una línea (con «tu franja» partía a 360 dp, así que dice «tu edad») y una sola frase'),
     ('t203b', 'ahora_others_393_10', 'ahora · «Otras edades» (muchos)', 393,
      'título en una línea; las franjas con 50 personas o más (la de 75 años o más tiene 38: sin fila); «Todas las edades»; y UNA línea que cierra. Ya no está «De 13 a 17 años no hay comparación por ahora.»')],
    [('t203', 'ahora_few_393_10', 'antes · T203 · «pocos»', 393,
      'en el mundo de «pocos», cinco de las seis filas decían «todavía somos pocos»: una lista de huecos'),
     ('t203b', 'ahora_few_others_393_10', 'ahora · «Otras edades» (pocos)', 393,
      'solo las clases con datos: «18 a 29 años» (70 personas) y «Todas las edades»; la propia franja (23 personas) no tiene fila y la línea que cierra lo dice. Sin la fila partida de «45 a 54 años (tu franja)»'),
     ('t203b', 'ahora_veryfew_393_10', 'ahora · «muy pocos»', 393,
      'sin ninguna clase con 50 personas, la losa «Otras edades» no existe; «Mientras tanto…» ya dice «deja detrás a», como la cifra y «Otras edades»')],
    [('t203', 'ahora_others_393_10', 'antes · T203 · semana a semana', 393,
      'el título partía en dos líneas («Tu percentil, semana a semana»), la frase de abajo iba en negrita y «arriba de la mitad» no se veía en el dibujo'),
     ('t203b', 'ahora_weeks_393_10', 'ahora · «Tu percentil semanal»', 393,
      'título en una línea; una raya punteada en 50 con «la mitad» (14 sp, como los ejes) en un carril propio: ninguna barra ni cifra se le cruza; la frase pasa a peso normal'),
     ('t203b', 'ahora_weeks7_393_10', 'ahora · siete semanas', 393,
      'con las siete semanas con marca, las cifras siguen sin tocarse y la raya cruza todo el gráfico')],
    [('t203', 'ahora_marks_393_10', 'antes · T203 · «Tus marcas»', 393,
      'la tercera cifra decía «3 de 4 semanas arriba de la mitad»: lo mismo que la frase de semana a semana, dos veces'),
     ('t203b', 'ahora_marks_393_10', 'ahora · «Tus marcas»', 393,
      'la tercera cifra es SIEMPRE «partidas esta semana» (o «partidas en total»): «Tus marcas» es solo de la persona'),
     ('t203b', 'ahora_nocompare_393_10', 'ahora · sin comparación', 393,
      'la tienda y quien tiene de 13 a 17 años: las mismas tres cifras, sin rótulos de sección')],
    [('t203', 'ahora_notes_393_10', 'antes · T203 · el ritmo y las notas', 393,
      'la frase del ritmo iba en negrita, de cuatro líneas y con «en el paso 4 de la pista»'),
     ('t203b', 'ahora_notes_393_10', 'ahora · el ritmo y las notas', 393,
      '«Tu ritmo en 2 minutos» (con «los» no cabía en una línea a 360 dp) y la frase en peso normal: «En tu última partida, tu mejor medio minuto fue el de 30 a 60 segundos, con 14 aciertos. Tus 3 errores llegaron en el último minuto.»'),
     ('t203b', 'ahora_top_360_10', 'ahora · arriba · 360 × 780', 360,
      'a 360 dp con letra 1,0 la cabecera, el selector y el rótulo de la comparación; los seis títulos de losa caben en una línea (lo mide `ContrarrelojPageTest`)')],
    [('t203', 'ahora_top_393_20', 'antes · T203 · letra 2,0', 393,
      'el rótulo «Esta semana, comparado con tu edad» partía y la raya quedaba como un guion suelto'),
     ('t203b', 'ahora_top_393_20', 'ahora · letra 2,0 · el rótulo', 393,
      'cuando el rótulo no cabe en una línea con 24 dp de raya, la raya NO va: el rótulo parte por palabras y queda limpio'),
     ('t203b', 'ahora_weeks7_360_20', 'ahora · siete semanas · 360 × 780 · letra 2,0', 360,
      'con letra 2,0 y siete semanas, «la mitad» pasa a dos líneas («la / mitad») y el carril se angosta: las cifras mantienen 8 dp entre columnas')],
    [('t203', 'ahora_ladder_393_20', 'antes · T203 · la escalera · letra 2,0', 393,
      'a 2,0 se leía «personas de 45 a / 54 años» y, a 360 dp, «para En la / Luna.»'),
     ('t203b', 'ahora_ladder_393_20', 'ahora · la escalera · letra 2,0', 393,
      'espacios duros en los nombres de las divisiones, en la franja entera («45 a 54 años») y en «Te faltan 7 aciertos»: nada se parte por dentro'),
     ('t203b', 'ahora_ladder_360_10', 'ahora · «Te faltan…» · 360 × 780', 360,
      '«Te faltan 7 aciertos para / En la Luna.»: el nombre de la división ya no queda cortado')],
    [('t203', 'ahora_avance_sinplus_393_10', 'antes · T203 · la puerta de Avance', 393,
      'la lámina decía «Te faltan 2» porque sembraba otro juego del día, junto al «7» de la página: parecía una contradicción'),
     ('t203b', 'ahora_avance_sinplus_393_10', 'ahora · la puerta, sin Plus', 393,
      'la marca de esta semana se siembra con la escalera del juego del día: «Te faltan 7 aciertos para En la Luna.», igual que la página de Tinta o Palabra; los nombres no se parten'),
     ('t203b', 'ahora_avance_sinplus_393_20', 'ahora · la puerta · letra 2,0', 393,
      'con letra 2,0 la frase gratis sigue sin partir «En la Luna» ni «Te faltan 7 aciertos»')],
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
    sheet_w = MARGIN * 2 + 3 * 393 + 2 * GAP
    def path(folder, key): return os.path.join(SHOTS, folder, key + '.png')
    def real_h(folder, key, w):
        im = Image.open(path(folder, key))
        return int(round(im.height * w / im.width))
    heights = []
    for row in ROWS:
        shots = max(real_h(f, k, w) for f, k, _, w, _ in row)
        cap = max(len(wrap(d0, t, sf, w)) * 20 for _, _, _, w, t in row)
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
            for i, line in enumerate(wrap(d, text, sf, w)):
                d.text((x, cy + i * 20), line, font=sf, fill=DIM)
            x += 393 + GAP
        y += hrow
    sheet.save(OUT)
    print(OUT, sheet.size)


main()
