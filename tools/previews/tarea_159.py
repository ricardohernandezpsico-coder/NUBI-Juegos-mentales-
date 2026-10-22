# -*- coding: utf-8 -*-
"""Lámina «hoy | propuesta» de la Tarea 159, «Avance con PUNTOS por área» (22-oct-2026; `auditoria-fable/avance-b-puntos.md`): `docs/previews/tarea-159.png`.
Avance pasa de «Tu semana + Tus áreas con etapa + Tus medidas + medallas + Tus últimas partidas» a: título, UNA línea de la semana, cuatro filas de área con sus PUNTOS (de 0 a 1000) y «Tus mejores marcas»; tocar una fila abre la PÁGINA del área (puntos y etapa, lo que falta,
las últimas semanas, una tarjeta por juego con su nivel en palabras y su medida, «Qué pones en juego» y la nota honesta), sin «Jugar». Todo en una sola escala: la lámina del edificio de Hoy y la ficha de un juego también hablan en puntos.
CAPTURAS REALES de Roborazzi a 393 dp (`Tarea159CapturasTest`, datos de ejemplo sembrados con el repositorio real): «HOY» = el commit `8b3d69a` (la app de la Tarea 158, antes de esta) y «PROPUESTA» = `src/test/screenshots/tarea-159/`. Al final, el emulador con letra 2,0 y las 17 frases «Ya …» en texto
(salen de `app/build/frases-ya.tsv`, que escribe `LevelLinesTest`).
Uso:  python -I tools/previews/tarea_159.py      → docs/previews/tarea-159.png    (las tomas del emulador, si existen, son `docs/previews/emulador-avance-*.png`)
"""
import io, os, subprocess, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
PREV = os.path.join(ROOT, 'docs', 'previews'); SHOTS = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots', 'tarea-159'); OUT = os.path.join(PREV, 'tarea-159.png')
FRASES = os.path.join(ROOT, 'app', 'build', 'frases-ya.tsv')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
BEFORE = '8b3d69a'
NAVY, SUN, DIM, CREAM, LIME, CORAL = (20, 18, 63), (255, 203, 69), (200, 196, 235), (245, 235, 219), (155, 229, 100), (255, 120, 90)
CW = 420; GAP = 24; MG = 40; COLS = 5
W = MG * 2 + COLS * CW + (COLS - 1) * GAP


def font(px, bold=True): return ImageFont.truetype(os.path.join(FONTS, 'fredoka.ttf' if bold else 'nunito_regular.ttf'), int(px))


def old(name):
    raw = subprocess.run(['git', '-C', ROOT, 'show', '%s:docs/previews/app-actual/%s.png' % (BEFORE, name)], capture_output=True, check=True).stdout
    return Image.open(io.BytesIO(raw)).convert('RGB')


def new(name): return Image.open(os.path.join(SHOTS, name + '.png')).convert('RGB')


def resumen(): return Image.open(os.path.join(ROOT, 'app', 'src', 'test', 'screenshots', 'resumen-sesion.png')).convert('RGB')


def emu(name): return Image.open(os.path.join(PREV, name + '.png')).convert('RGB')


def wrap(d, text, fnt, width):
    lines, cur = [], ''
    for word in text.split(' '):
        t = (cur + ' ' + word).strip()
        if d.textlength(t, font=fnt) <= width: cur = t
        else: lines.append(cur); cur = word
    if cur: lines.append(cur)
    return lines


def fit(im, w=CW):
    return im.resize((w, int(round(im.height * w / im.width))), Image.LANCZOS)


def top(im, h):
    """Las primeras [h] unidades de una toma alta (a la escala de 1080 px de ancho)."""
    return im.crop((0, 0, im.width, min(im.height, h)))


# ------------------------------------------------------------------ las filas de la lámina: (título, descripción, [(rótulo, imagen, 'a' hoy | 'b' propuesta)])
def filas():
    return [
        ('Avance', 'Antes: «Tu semana» con las farolas, la racha y el globo de Nubi, «Tus áreas» con la etapa y «avanzó de 37 a 47», «Tus medidas», el enlace a las medallas y «Tus últimas partidas». Ahora: el título, UNA línea de la semana, cuatro filas con los puntos grandes («470 de 1000 puntos»), «Etapa Hábil», la barra de las 5 etapas y la frase de la semana; y «Tus mejores marcas». Sin flechas, signos de más o menos ni abreviaturas.',
         [('HOY · letra 1,0', lambda: old('avance'), 'a'), ('PROPUESTA · letra 1,0', lambda: new('avance'), 'b'), ('HOY · letra 1,3', lambda: old('avance-letra13'), 'a'), ('PROPUESTA · letra 1,3', lambda: new('avance-letra13'), 'b')]),
        ('Avance entero (lo que se desliza)', 'La pestaña entera: las cuatro filas, «Tus mejores marcas» (hasta tres, con su unidad y su fecha) y «Tu punto de partida». Con letra 1,3 los puntos bajan bajo el nombre solo si no caben al lado.',
         [('HOY · entero', lambda: top(old('avance-completa'), 5200), 'a'), ('PROPUESTA · entero', lambda: new('avance-completa'), 'b'), ('PROPUESTA · entero · letra 1,3', lambda: new('avance-completa-letra13'), 'b')]),
        ('La página de Memoria (al tocar una fila)', 'Antes se abría la lámina del lugar con «Jugar» y el gráfico de 4 semanas. Ahora una página: «‹ Avance», los puntos y la etapa, la barra con los nombres de las etapas (o una frase con letra grande), lo que falta, los puntos de las últimas 8 semanas con la cifra del primero y del último, una tarjeta por juego («180 de 250 puntos», «Ya repites 6 luces seguidas», su medida y cuándo se jugó), «Qué pones en juego» y la nota honesta. Sin «Jugar».',
         [('HOY · la lámina de Avance', lambda: old('avance-lamina'), 'a'), ('PROPUESTA · letra 1,0', lambda: new('pagina-memoria'), 'b'), ('PROPUESTA · letra 1,3', lambda: new('pagina-memoria-letra13'), 'b')]),
        ('La página de Memoria, entera', 'Se desliza: arriba los puntos y el gráfico, abajo las tarjetas de los cuatro juegos, lo que pones en juego y la nota.',
         [('PROPUESTA · entera · letra 1,0', lambda: new('pagina-memoria-completa'), 'b'), ('PROPUESTA · entera · letra 1,3', lambda: new('pagina-memoria-completa-letra13'), 'b')]),
        ('La página de Atención (5 juegos: hasta 200 puntos cada uno)', 'Atención tiene cinco juegos, así que cada uno aporta hasta 200 puntos («Cada juego aporta hasta 200 puntos»).',
         [('PROPUESTA · entera · letra 1,0', lambda: new('pagina-atencion-completa'), 'b'), ('PROPUESTA · entera · letra 1,3', lambda: new('pagina-atencion-completa-letra13'), 'b')]),
        ('Estados vacíos', 'Un área sin partidas (0 puntos, «Todavía sin partidas · aporta hasta 250 puntos» en cada juego, sin gráfico) y un área con menos de 4 semanas de uso (en lugar del gráfico, «Llevas 2 semanas en Memoria. El gráfico aparece cuando tengas 4.»).',
         [('Un área sin partidas', lambda: new('pagina-lenguaje-vacia'), 'b'), ('… con letra 1,3', lambda: new('pagina-lenguaje-vacia-letra13'), 'b'), ('Menos de 4 semanas', lambda: new('pagina-memoria-empieza'), 'b'), ('… con letra 1,3', lambda: new('pagina-memoria-empieza-letra13'), 'b')]),
        ('Una semana con bajada', 'Se dice sin culpa y con la mejor marca al lado: «Esta semana bajaste 12 puntos. Tu mejor marca sigue en 520 puntos.»; sin rojo, sin flechas, sin «perdiste». La barra no retrocede con animación: muestra el valor. En la página, una línea fija: «Los puntos suben y bajan un poco de una partida a otra. Lo que cuenta es la etapa.»',
         [('Avance con una bajada', lambda: new('avance-bajada'), 'b'), ('La página de Memoria', lambda: new('pagina-memoria-bajada-completa'), 'b')]),
        ('El resumen de la ruta', 'Antes: a cada juego le ponía «puntaje de 100» (lo que se sacó en UNA partida: otra escala) y el avance de cada área decía «Hoy avanzó de 46 a 52». Ahora cada juego dice los puntos que sumó hoy en su área («Sumaste 12 puntos en Atención»), o «Hecho», y el avance de cada área, sus puntos de 0 a 1000. Ya no hay un segundo «puntaje» en ninguna pantalla.',
         [('PROPUESTA · el resumen de la ruta', lambda: resumen(), 'b')]),
        ('Una sola escala en toda la app: la lámina de Hoy y la ficha de un juego', 'La lámina del edificio dice «Etapa Hábil · 470 de 1000 puntos» del MISMO cálculo que Avance (antes «Hábil · 47 de 100», un promedio solo de los juegos jugados) y su globo, «Te faltan 130 puntos para Experto. Esta semana sumaste 12 puntos.». La ficha de un juego dice «Etapa Hábil · 110 de 200 puntos» y «Te faltan 10 puntos para Experto» (los mismos tramos, en proporción).',
         [('HOY · la lámina de Hoy', lambda: old('hoy-lamina'), 'a'), ('PROPUESTA · la lámina de Hoy', lambda: new('hoy-lamina'), 'b'), ('… letra 1,3', lambda: new('hoy-lamina-letra13'), 'b'), ('HOY · la ficha del juego', lambda: old('ficha'), 'a'), ('PROPUESTA · la ficha', lambda: new('ficha'), 'b')]),
    ]


def frases_ya():
    """Las frases por juego: [(id, [(niveles 'a-b', frase)])], juntando los niveles vecinos que dicen lo mismo."""
    out = {}
    order = []
    with open(FRASES, encoding='utf-8') as f:
        for ln in f:
            g, lv, tx = ln.rstrip('\n').split('\t')
            if g not in out: out[g] = []; order.append(g)
            out[g].append((int(lv), tx))
    res = []
    for g in order:
        grupos = []
        for lv, tx in out[g]:
            if grupos and grupos[-1][2] == tx: grupos[-1][1] = lv
            else: grupos.append([lv, lv, tx])
        res.append((g, [('%d' % a if a == b else '%d a %d' % (a, b), t) for a, b, t in grupos], out[g][-1][0]))
    return res


NOMBRES = {'freno_reloj': 'Freno de Emergencia (si juegas con reloj)', 'parejas': 'Constelaciones', 'secuencia': 'Rastro de luz', 'bodega': 'Bodega de carga', 'correo': 'Correo Estelar', 'stroop': 'Tinta o Palabra', 'piloto': 'Piloto Estelar', 'freno': 'Freno de Emergencia',
           'satelites': 'Satélites', 'radar': 'Rescate relámpago', 'calculo': 'Carga exacta', 'engranajes': 'Engranajes', 'acoplamiento': 'Acoplamiento', 'aterrizaje': 'Aterrizaje Lunar',
           'anagramas': 'En la punta de la lengua', 'meteoros': 'Lluvia de meteoros', 'disparate': '¿Verdad o disparate?', 'intrusa': 'La estrella intrusa'}


def main():
    probe = ImageDraw.Draw(Image.new('RGB', (10, 10)))
    F_T, F_H, F_P, F_C, F_B = font(62), font(40), font(27, False), font(23, False), font(25, False)
    hojas = []
    for titulo, desc, celdas in filas():
        imgs = [(lab, fit(f()), tipo) for lab, f, tipo in celdas]
        hojas.append((titulo, wrap(probe, desc, F_P, W - 2 * MG), imgs))
    emul = [(t, emu(f)) for t, f in (('Emulador · Avance · letra 2,0', 'emulador-avance-letra20'), ('Emulador · página del área · letra 2,0', 'emulador-pagina-letra20'), ('Emulador · Avance · 3 botones', 'emulador-avance-3botones'), ('Emulador · Avance · gestos', 'emulador-avance-gestos')) if os.path.exists(os.path.join(PREV, f + '.png'))]
    intro = ('Ricardo aprobó la tarea: «puntos por área». Cada área va de 0 a 1000 y es la SUMA de sus juegos (cada uno aporta hasta 1000 ÷ cuántos juegos tiene el área: 250, o 200 en Atención; un juego sin partidas vale 0, así que probar uno nuevo solo puede sumar); la etapa es el tramo fijo de 200 donde cae. '
             'Los puntos salen del nivel que alcanzaste en cada juego (nunca del porcentaje de aciertos ni del tiempo), no bajan por dejar de jugar y la bajada de una partida está acotada (a lo más 12 o 13 puntos). La página del área no lleva «Jugar» y «Tus últimas partidas» salió de Avance. '
             'Una sola escala: todo lo que decía «Hábil · 47 de 100» o «N de 100» dice ahora lo mismo, de un mismo cálculo. Segundos y no milisegundos en TODA la app («0,16 segundos de destello», «grados por segundo»); «En tu última partida: …» cuando la última no fue hoy. '
             'La comparación con otras personas solo en Desafío queda preparada y apagada (`Features.COMPARE = false`; cada medida guarda el modo en que se jugó). Todas las tomas son capturas reales de Roborazzi a 393 dp.')
    intro_l = wrap(probe, intro, F_P, W - 2 * MG)
    fr = frases_ya()
    H = MG + 80 + 34 * len(intro_l) + 30
    for titulo, dl, imgs in hojas: H += 56 + 32 * len(dl) + 10 + max(i.height for _, i, _ in imgs) + 36 + 24
    H += 120 + 60 * 2 + 60 + 40 * 60 + 40 + (60 + 40 + sum(1 for _ in emul) * 0 + 4000 if emul else 0) + 400
    sheet = Image.new('RGB', (W, H), NAVY); d = ImageDraw.Draw(sheet); y = MG
    d.text((MG, y), 'Avance con puntos por área (Tarea 159): hoy | propuesta', font=F_T, fill=SUN); y += 80
    for ln in intro_l: d.text((MG, y), ln, font=F_P, fill=CREAM); y += 34
    y += 30
    for titulo, dl, imgs in hojas:
        d.text((MG, y), titulo, font=F_H, fill=SUN); y += 56
        for ln in dl: d.text((MG, y), ln, font=F_P, fill=DIM); y += 32
        y += 10; x = MG; h = max(i.height for _, i, _ in imgs)
        for lab, im, tipo in imgs:
            sheet.paste(im, (x, y)); d.rectangle((x, y, x + im.width - 1, y + im.height - 1), outline=(70, 74, 130), width=1)
            d.text((x, y + h + 6), lab, font=F_C, fill=(DIM if tipo == 'a' else LIME)); x += CW + GAP
        y += h + 36 + 24
    if emul:
        d.text((MG, y), 'En el emulador medium_phone (API 36)', font=F_H, fill=SUN); y += 56
        for ln in wrap(probe, 'Capturas reales del emulador a 1080 px de ancho: Avance y la página de un área con la letra del sistema al 200 % (nada se corta ni se pisa), y con 3 botones y con gestos (la barra de pestañas sigue sobre las barras del sistema).', F_P, W - 2 * MG): d.text((MG, y), ln, font=F_P, fill=DIM); y += 32
        y += 10; x = MG; rowh = 0
        for lab, im in emul:
            im2 = fit(im)
            if x + im2.width > W - MG: x = MG; y += rowh + 40; rowh = 0
            sheet.paste(im2, (x, y)); d.rectangle((x, y, x + im2.width - 1, y + im2.height - 1), outline=(70, 74, 130), width=1)
            d.text((x, y + im2.height + 6), lab, font=F_C, fill=DIM); x += CW + GAP; rowh = max(rowh, im2.height)
        y += rowh + 60
    d.text((MG, y), 'Las 17 frases «Ya …» (el nivel de cada juego en palabras)', font=F_H, fill=SUN); y += 56
    for ln in wrap(probe, 'Salen del nivel donde la persona acierta 8 de 10 (nivel = 1 + parte entera de N × avance) y de la tabla de cada contrato de Unity (solo lectura). Reglas de esta vuelta: 12 palabras como máximo, una cifra principal y a lo más UN rasgo extra, sin mecánica interna («4 de cada 10») ni condiciones («con reloj»), a lo más 2 cifras significativas. Cada línea es un tramo de niveles con la misma frase. Rastro de luz, Satélites y Rescate no repiten su cifra en la tarjeta cuando su medida («Hoy: …») ya la dice en la misma unidad. Freno dice su plazo solo si la persona juega con reloj.', F_P, W - 2 * MG): d.text((MG, y), ln, font=F_P, fill=DIM); y += 32
    y += 14
    for g, grupos, n in fr:
        d.text((MG, y), '%s (%d niveles)' % (NOMBRES.get(g, g), n), font=font(30), fill=LIME); y += 40
        for niv, tx in grupos:
            etiqueta = ('nivel ' if '-' not in niv and ' a ' not in niv else 'niveles ') + niv
            lines = wrap(probe, tx, F_B, W - 2 * MG - 210)
            d.text((MG, y), etiqueta, font=F_C, fill=DIM)
            for ln in lines: d.text((MG + 210, y), ln, font=F_B, fill=CREAM); y += 32
            if not lines: y += 32
        y += 14
    sheet = sheet.crop((0, 0, W, y + 30)); sheet.save(OUT)
    print('ok', OUT, sheet.size)


if __name__ == '__main__':
    main()
