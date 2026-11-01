# -*- coding: utf-8 -*-
"""Lámina «hoy | ahora» de la Tarea 203 (31-oct-2026): la página «Contrarreloj» de Avance rediseñada (dirección D2 de Fable, `auditoria-fable/contrarreloj-rediseno.md`), con DATOS DE MUESTRA y solo en depuración. Capturas REALES de Roborazzi a tamaño real (1 px = 1 dp):
«ahora» sale de `Tarea203CapturasTest` (`app/src/test/screenshots/t203/`); «hoy» es la página de antes, de las capturas de las Tareas 199, 201 y 202 (`t199/`, `t201/`, `t202/`). Orden de cada fila: hoy · ahora · ahora.
Datos: la persona de 45 a 54 años de la lámina de Fable (128 personas esta semana, 43 aciertos supera a 63 de cada 100; con los umbrales calculados sobre los tramos de 5 aciertos: En la Luna desde 50, En órbita desde 39) con las marcas de la Tarea 199 y el ritmo de la Tarea 201.
Uso:  python -I tools/previews/tarea_203.py      → docs/previews/tarea-203.png
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SHOTS = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-203.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
NAVY, SUN, DIM, LINE, HOYC = (20, 18, 63), (255, 203, 69), (200, 196, 235), (90, 86, 160), (170, 166, 210)
GAP, MARGIN = 40, 24

# (carpeta, captura, título, ancho dp, qué se ve)
ROWS = [
    [('t202', 'ahora_section_393_10', 'hoy · Tarea 202 · letra 1,0', 393,
      'la comparación iba DESPUÉS de «Tus marcas»: antes de llegar había que bajar cuatro pantallas; el rótulo «Comparado con personas de tu edad» y la frase larga con el percentil'),
     ('t203', 'ahora_top_393_10', 'ahora · arriba · letra 1,0', 393,
      'primero la comparación: bajo el selector, «Esta semana, comparado con tu edad», el aviso de muestra y la cifra «63 de cada 100» (Rubik 40) con su unidad, su frase, la división y «Percentil 63». Sin lila ni recuadros: una sola losa crema. La cifra y su división caben en la primera pantalla'),
     ('t203', 'ahora_ladder_393_10', 'ahora · la escalera', 393,
      'las cinco divisiones de arriba abajo, cada una con su PRIMER acierto («desde 50 aciertos»); tu fila en lima, con la división en palabras y «Tú · 43 aciertos» (no solo por el color). Al pie, lo que sigue (en la lámina de al lado)')],
    [('t202', 'ahora_place_393_10_bell', 'hoy · Tu lugar y la campana', 393,
      'la frase en el recuadro lila, «Es el percentil 63: la división En órbita.», las dos frases de la campana con la mitad (39) y la cuarta parte más alta (47): números que la escalera ahora dice una vez'),
     ('t203', 'ahora_ladder_foot_393_10', 'ahora · «Te faltan 7 aciertos para En la Luna.»', 393,
      'al pie de la escalera: lo que falta, con su barra (4 de los 11 aciertos del tramo), «La semana cierra el domingo.», «Llevas 3 semanas seguidas En órbita o más arriba.» (solo si hay; cuando se corta, calla) y la clase de la escalera dicha una vez'),
     ('t203', 'ahora_bell_393_10', 'ahora · la campana, en su losa', 393,
      '«Cómo se reparten las marcas»: la campana real con «Tú» y la línea de la mitad, y UNA frase corta que no repite ningún número de la escalera')],
    [('t202', 'ahora_others_393_10', 'hoy · otras edades y semanas', 393,
      'la losa de otras edades y la de las 7 semanas iban separadas, después de la campana'),
     ('t203', 'ahora_others_393_10', 'ahora · otras edades y semana a semana', 393,
      'detrás de la campana: «De cada 100 personas que jugaron Tinta o Palabra esta semana, tu mejor marca deja detrás a:» con las seis franjas, y «Tu percentil, semana a semana» con las semanas con marca, de la más vieja a esta'),
     ('t203', 'ahora_topdivision_393_10', 'ahora · la división más alta', 393,
      'con 70 aciertos: la fila lima va arriba, la escalera pierde su peldaño sin barra y dice «Estás en la división más alta de tu franja esta semana.» sin cifra de «te faltan»')],
    [('t201', 'ahora_page_393_10_top', 'hoy · Tu récord y las 10 marcas', 393,
      'una losa grande con la cifra «44 aciertos», su fecha y su paso, y adentro las 10 marcas; después «Esta semana y la pasada», «Aciertos y errores» y «Pistas anteriores»'),
     ('t203', 'ahora_marks_393_10', 'ahora · «Tus marcas»', 393,
      'al final de la página, bajo su rótulo: tres cifras con su unidad («44 aciertos, tu récord · 43 aciertos esta semana · 3 de 4 semanas arriba de la mitad»), las 10 marcas y el ritmo. «Esta semana y la pasada», «Aciertos y errores» y «Pistas anteriores» se fundieron aquí'),
     ('t203', 'ahora_notes_393_10', 'ahora · el ritmo y las notas', 393,
      'las notas, juntas y sin título en la última losa: cómo se cuenta la marca, el récord de la pista anterior y «No es una medida de capacidad.» (con la comparación, sin «solo contigo»)')],
    [('t202', 'ahora_place_393_20', 'hoy · letra 2,0', 393,
      'la frase en el recuadro lila parte la línea por palabras y la losa crece'),
     ('t203', 'ahora_hero_393_20', 'ahora · la cifra · letra 2,0', 393,
      'la cifra grande no pasa de 1,3 de escala (cabe en una línea); la frase y la división siguen la letra: la división y el percentil se apilan cuando no caben juntos'),
     ('t203', 'ahora_ladder_393_20', 'ahora · la escalera · letra 2,0', 393,
      'cada fila pasa su «desde N aciertos» a su propia línea y la fila lima apila la división y «Tú · 43 aciertos» (4 dp entre las dos); el título de la losa parte por palabras')],
    [('t201', 'ahora_page_393_20_top', 'hoy · letra 2,0', 393, 'la cabecera, el selector y la losa de Tu récord'),
     ('t203', 'ahora_top_393_20', 'ahora · arriba · letra 2,0', 393, 'el selector pasa a una columna; la cifra y el resto bajan, sin choques'),
     ('t203', 'ahora_marks_393_20', 'ahora · «Tus marcas» · letra 2,0', 393,
      'con letra 1,3 o más las tres cifras pasan a una columna, cada una con su rótulo al lado (la cifra, con tope de escala 1,3); las barras son cinco')],
    [('t201', 'ahora_page_393_10_top', 'hoy · sin comparación (la tienda)', 393, 'la página de la Tarea 201: Tu récord y las 10 marcas, sin rótulos de sección'),
     ('t203', 'ahora_nocompare_top_393_10', 'ahora · sin comparación · arriba', 393,
      'la build de la tienda y quien tiene de 13 a 17 años: la página es SOLO «Tus marcas», sin rótulos ni percentil; la tercera cifra no depende de otras personas («partidas esta semana»)'),
     ('t203', 'ahora_nocompare_marks_393_10', 'ahora · sin comparación · las 10 marcas', 393,
      'las 10 marcas con el récord en sol y su moneda; el subtítulo de la cabecera y la nota siguen diciendo «comparadas solo contigo»')],
    [('t202', 'ahora_few_393_10', 'hoy · «Todavía somos pocos»', 393, 'con 23 personas en la franja: la frase en el recuadro lila, la cuenta y «Mientras tanto…»'),
     ('t203', 'ahora_few_393_10', 'ahora · «Todavía somos pocos»', 393,
      'sin cifra, sin escalera y sin «te faltan»: con tan pocas personas no se inventan umbrales. La frase enmarcada con la cuenta real, la barra «23 de 50 personas» y «Mientras tanto…»; abajo, las otras edades y, al final, «Tus marcas»'),
     ('t203', 'ahora_nomark_393_10', 'ahora · sin marca esta semana', 393,
      'la losa dice qué hacer («Juega su Contrarreloj para ver tu lugar…») y no inventa números ni escalera')],
    [('t202', 'ahora_place_360_10', 'hoy · 360 × 780', 360, 'la losa de Tu lugar a 360 dp'),
     ('t203', 'ahora_ladder_360_10', 'ahora · la escalera · 360 × 780', 360, 'la fila lima y las demás caben sin tocarse; todo dentro del margen de 16 dp'),
     ('t203', 'ahora_marks_360_10', 'ahora · «Tus marcas» · 360 × 780', 360, 'las tres columnas con 82 dp útiles cada una: los rótulos parten por palabras')],
    [('t199', 'prop_avance_sinplus', 'hoy · Avance sin Plus', 393, 'la puerta con el récord de hoy y el candado «Con Nubi Plus»'),
     ('t203', 'ahora_avance_sinplus_393_10', 'ahora · la puerta, sin Plus', 393,
      'LO GRATIS: «Esta semana estás En órbita. Te faltan 2 aciertos para En la Luna.» entre el récord y el candado (solo con una comparación disponible y una marca esta semana); el percentil, la escalera y las otras edades siguen siendo Plus'),
     ('t203', 'ahora_sheet_393_10', 'ahora · la hoja del candado', 393,
      'tres líneas (antes seis): el lugar entre personas de tu edad, la escalera con tus semanas seguidas y tus marcas con el ritmo. En la tienda, sin comparación, solo dice lo que hay detrás')],
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
            d.text((x, y), title, font=tf, fill=HOYC if title.startswith('hoy') else SUN)
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
