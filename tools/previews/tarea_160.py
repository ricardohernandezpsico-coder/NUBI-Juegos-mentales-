# -*- coding: utf-8 -*-
"""Lámina «antes | después» de la Tarea 160, «sin abreviaturas ni signos» (22-oct-2026): `docs/previews/tarea-160.png`.
Regla de Ricardo, «sin palabras sueltas»: en todo texto visible de la app Kotlin, «segundos», «minutos» y «grados» completos; ni «×», ni «+N» o «−N», ni flechas, ni «pts», «sem.» o «n.º»; y un «%» siempre con su «de qué» al lado.
La tabla lista CADA texto que cambió (antes → después, por pantalla); debajo, capturas REALES de Roborazzi (393 dp) de los finales y la ficha que cambian de forma (`app/src/test/screenshots/`, que no va a git: se regeneran con
`./gradlew.bat :app:recordRoborazziDebug --tests "*Grupo2FinalesScreenshotTest*" --tests "*AcoplamientoResultScreenshotTest*" --tests "*MeteorResultScreenshotTest*" --tests "*GameSheetFitScreenshotTest*"`).
Uso:  python -I tools/previews/tarea_160.py      → docs/previews/tarea-160.png
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
PREV = os.path.join(ROOT, 'docs', 'previews'); SHOTS = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots'); OUT = os.path.join(PREV, 'tarea-160.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
NAVY, SUN, DIM, CREAM, LIME, CORAL = (20, 18, 63), (255, 203, 69), (200, 196, 235), (245, 235, 219), (155, 229, 100), (255, 120, 90)
W = 2000; MG = 40; COLW = (W - 2 * MG - 40) // 2


def font(px, bold=True): return ImageFont.truetype(os.path.join(FONTS, 'fredoka.ttf' if bold else 'nunito_regular.ttf'), int(px))


def wrap(d, text, fnt, width):
    lines, cur = [], ''
    for word in text.split(' '):
        t = (cur + ' ' + word).strip()
        if d.textlength(t, font=fnt) <= width: cur = t
        else: lines.append(cur); cur = word
    if cur: lines.append(cur)
    return lines or ['']


# ------------------------------------------------------------------ cada texto que cambió: (pantalla, [(antes, después)])
TABLA = [
    ('Finales de «Tu avance»: cada medida dice su unidad y su «de qué»', [
        ('Como tu promedio (70 %): vas parejo. Tu mejor: 80 %.  (Correo)', 'Como tu promedio (70 % de los encargos): vas parejo. Tu mejor: 80 % de los encargos.'),
        ('Mejor que tu promedio (70 %). Tu mejor: 80 %.  (Constelaciones)', 'Mejor que tu promedio (70 % de las veces). Tu mejor: 80 % de las veces.'),
        ('(50 %) / (75 %) / (62 %)  en Meteoros, Intrusa, Carga, Engranajes, Punta, Bodega y Piloto', '(50 % de las palabras) · (75 % de aciertos) · (75 % de las cargas) · (… de las máquinas) · (… de los objetos) · (… de las señales)'),
        ('Como tu promedio (213 °/s): vas parejo. Tu mejor: 260 °/s.  (Acoplamiento)', 'Como tu promedio (213 grados por segundo): vas parejo. Tu mejor: 260 grados por segundo.'),
        ('Como tu promedio (140 por minuto): vas parejo. Tu mejor: 150 por minuto.  (Verdad o disparate)', 'Como tu promedio (140 palabras por minuto): vas parejo. Tu mejor: 150 palabras por minuto.'),
        ('Mejor que tu promedio (+0,4 s).  /  Tu mejor: +0,3 s.  (Tinta o Palabra)', 'Mejor que tu promedio (0,4 segundos más).  /  Tu mejor: 0,3 segundos más.'),
    ]),
    ('Finales B: la cifra grande y su unidad', [
        ('+0,4 s  ·  te frenó la palabra  (Tinta o Palabra)', '0,4 segundos  ·  de demora cuando la palabra te frenó'),
        ('+0,1 s  ·  te frenó la palabra', '0,1 segundos  ·  de demora cuando la palabra te frenó   («casi nada» se queda igual)'),
        ('212° por segundo  ·  de giro en tu mente  (Acoplamiento)', '212 grados  ·  por segundo, de giro en tu mente'),
        ('Cambiar de orilla te costó: +0,2 s  (Tinta o Palabra, detalle)', 'Cambiar de orilla te costó: 0,2 segundos más'),
    ]),
    ('Detalles de la partida', [
        ('Tu ritmo: 4,5 s por objeto  (Bodega)', 'Tu ritmo: 4,5 segundos por objeto'),
        ('Tu ritmo: 7,5 s por máquina  (Engranajes)', 'Tu ritmo: 7,5 segundos por máquina'),
        ('Tardaste 14 s por carga, en promedio, en las que lograste sin pista  (Carga exacta)', 'Tardaste 14 segundos por carga, en promedio, en las que lograste sin pista'),
        ('Tardaste 4,2 s en decir «¡La tengo!» en las que salieron solas  (En la punta de la lengua)', 'Tardaste 4,2 segundos en decir «¡La tengo!» en las que salieron solas'),
        ('Mediana al tocar: 1,8 s  (La estrella intrusa)', 'Mediana al tocar: 1,8 segundos'),
        ('Las negaciones te toman 0,9 s más que las frases simples.  (Verdad o disparate)', 'Las negaciones te toman 0,9 segundos más que las frases simples.'),
        ('Las comunes, en 0,7 s. Las raras, en 1,1 s.  (Lluvia de meteoros)', 'Las comunes, en 0,7 segundos. Las raras, en 1,1 segundos.'),
        ('barra del vocabulario «78%»  ·  colección «+3 palabras raras»', '«78 %» bajo «Cada barra es el porcentaje de palabras de ese grupo que reconociste.»  ·  «3 palabras raras»'),
        ('Curva de giro (Acoplamiento): rótulos «0° 45° 90° 135° 180°» y «0,9 s» sobre cada columna', 'Rótulos «0 45 90 135 180» y «0,9»; la frase de abajo dice la unidad: «Abajo, los grados que venía girada la pieza; arriba, los segundos que tardaste en promedio.»'),
        ('Velocidad más alta superada: 1,4×  (Satélites)', 'Velocidad más alta superada: 1,4 veces la inicial'),
        ('¡Despegó tu cohete n.º 3!  (Engranajes)', '¡Despegó tu cohete número 3!'),
        ('Rondas perfectas: 5 de 12 · más seguidos ×3  (Rescate y Satélites)', 'Rondas perfectas: 5 de 12 · más seguidos: 3'),
        ('2 dianas lunares · más seguidos ×4 · 1 cúpula  /  1 diana lunar · más seguidos — · 0 cúpulas  (Aterrizaje)', '2 dianas lunares · más seguidos: 4 · 1 cúpula  /  1 diana lunar · 0 cúpulas  (sin seguidos, no se nombran)'),
        ('Más seguidos ×7  (Piloto)', 'Más seguidos: 7'),
        ('Tus señales a los mandos: 71 %   ·   En la ruta: 82 %  (Piloto)', '71 % de las señales de tu misión, a los mandos   ·   En la ruta: 82 % del tiempo'),
        ('Memoria de lugar: 70 %  (Constelaciones)   ·   Tu memoria para lo pendiente: 63 %  (Correo)', 'Memoria de lugar: 70 % de las veces   ·   Tu memoria para lo pendiente: 63 % de los encargos'),
        ('Tu promedio: 11 %  (Aterrizaje, tus números en la frase)', '11 % de la regla'),
    ]),
    ('La ficha de un juego', [
        ('Con reloj (2 min)', 'Con reloj (2 minutos)'),
        ('Con reloj (1 min 30 s)', 'Con reloj (1 minuto y 30 segundos)'),
        ('Con reloj (45 s)', 'Con reloj (45 segundos)'),
    ]),
    ('Avance y sus medidas (el espacio ante el signo, como en el resto de la app)', [
        ('70% de las veces  ·  a 18% del blanco  ·  62% de las palabras', '70 % de las veces  ·  a 18 % del blanco  ·  62 % de las palabras'),
    ]),
]

# capturas «después»: (rótulo, archivo, recorte opcional (alto máx.))
CAPTURAS = [
    ('Final de Acoplamiento · «212 grados»', 'acoplamiento-final-sentido-1.png'),
    ('… su detalle: la curva y la frase que dice la unidad', 'acoplamiento-final-sentido-detalle-2.png'),
    ('Final de Tinta o Palabra · «0,4 segundos»', 'tinta-final-sentido-1.png'),
    ('Final de Lluvia de meteoros · «51 % de las palabras raras, reconocidas»', 'meteoros-final-sentido-2.png'),
]


def main():
    probe = ImageDraw.Draw(Image.new('RGB', (10, 10)))
    F_T, F_H, F_P, F_B, F_C = font(58), font(34), font(24, False), font(25, False), font(22, False)
    intro = ('Regla de Ricardo, «sin palabras sueltas»: nada de abreviaturas ni signos en lo que se ve. «s» pasa a «segundos», «min» a «minutos», «°» a «grados»; no quedan «×», «+N» ni «−N» y un «%» siempre lleva su «de qué» al lado («9 % de la regla»). '
             'Esta lámina lista cada texto que cambió, por pantalla. Solo la app Kotlin: lo que dibuja Unity queda anotado aparte. Una prueba nueva (VisibleTextsGuardTest) mira todo el código de la app y falla si vuelve alguna de esas abreviaturas o signos.')
    intro_l = wrap(probe, intro, F_P, W - 2 * MG)
    # medir la altura
    H = MG + 70 + 32 * len(intro_l) + 62
    bloques = []
    for titulo, pares in TABLA:
        filas = []
        for a, b in pares:
            la = wrap(probe, a, F_B, COLW - 20); lb = wrap(probe, b, F_B, COLW - 20)
            filas.append((la, lb))
        bloques.append((titulo, filas))
        H += 60 + sum(max(len(la), len(lb)) * 31 + 18 for la, lb in filas) + 30
    caps = []
    for rot, f in CAPTURAS:
        p = os.path.join(SHOTS, f)
        if os.path.exists(p):
            im = Image.open(p).convert('RGB'); w = 440
            im = im.resize((w, int(im.height * w / im.width)), Image.LANCZOS)
            caps.append((rot, im.crop((0, 0, w, min(im.height, 1500)))))
    if caps: H += 60 + max(i.height for _, i in caps) + 60
    H += 40
    sheet = Image.new('RGB', (W, H), NAVY); d = ImageDraw.Draw(sheet); y = MG
    d.text((MG, y), 'Sin abreviaturas ni signos (Tarea 160): antes | después', font=F_T, fill=SUN); y += 70
    for ln in intro_l: d.text((MG, y), ln, font=F_P, fill=CREAM); y += 32
    d.text((MG, y + 4), 'Cada fila: a la izquierda, ANTES (gris); a la derecha, DESPUÉS (verde).', font=F_P, fill=DIM); y += 30 + 32
    for titulo, filas in bloques:
        d.text((MG, y), titulo, font=F_H, fill=SUN); y += 52
        for la, lb in filas:
            h = max(len(la), len(lb)) * 31 + 12
            d.rounded_rectangle((MG, y, MG + COLW, y + h), radius=10, outline=(70, 74, 130), width=1)
            d.rounded_rectangle((MG + COLW + 40, y, MG + 2 * COLW + 40, y + h), radius=10, outline=(70, 110, 80), width=1)
            for i, ln in enumerate(la): d.text((MG + 10, y + 6 + i * 31), ln, font=F_B, fill=DIM)
            for i, ln in enumerate(lb): d.text((MG + COLW + 50, y + 6 + i * 31), ln, font=F_B, fill=LIME)
            y += h + 6
        y += 30
    if caps:
        d.text((MG, y), 'Cómo queda (capturas reales de Roborazzi a 393 dp)', font=F_H, fill=SUN); y += 56
        x = MG; h = max(i.height for _, i in caps)
        for rot, im in caps:
            sheet.paste(im, (x, y)); d.rectangle((x, y, x + im.width - 1, y + im.height - 1), outline=(70, 74, 130), width=1)
            for k, ln in enumerate(wrap(probe, rot, F_C, im.width)): d.text((x, y + im.height + 6 + k * 26), ln, font=F_C, fill=LIME)
            x += im.width + 24
        y += h + 60
    sheet = sheet.crop((0, 0, W, y + 20)); sheet.save(OUT)
    print('ok', OUT, sheet.size, '· capturas:', len(caps))


if __name__ == '__main__':
    main()
