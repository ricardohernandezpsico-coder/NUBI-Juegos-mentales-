# -*- coding: utf-8 -*-
"""Lámina «antes | después» de la Tarea 161, «Unity sin signos» (22-oct-2026): `docs/previews/tarea-161.png`.
Lo que la 160 dejó anotado para los juegos de Unity, con la misma regla de Ricardo, «sin palabras sueltas»: nada de «×» (salvo el signo de multiplicar de la cuenta de Carga exacta), nada de «n.º» y un «%» nunca pegado a la cifra.
La tabla lista CADA texto que cambió, por juego; debajo, capturas REALES de Unity en forma de teléfono (1080 × 2400, `bash tools/verificar-todo.sh --capturas <juego>`, tomas sueltas en `unity/test-results/capturas/<juego>/`, que no van a git).
Uso:  python -I tools/previews/tarea_161.py [captura.png ...]      → docs/previews/tarea-161.png   (las capturas se pasan por ruta, con su rótulo tras «=»: ruta=rótulo)
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
PREV = os.path.join(ROOT, 'docs', 'previews'); OUT = os.path.join(PREV, 'tarea-161.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
NAVY, SUN, DIM, CREAM, LIME = (20, 18, 63), (255, 203, 69), (200, 196, 235), (245, 235, 219), (155, 229, 100)
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


# ------------------------------------------------------------------ cada texto que cambió: (juego, [(antes, después)])
TABLA = [
    ('El marcador común (todos los juegos con aciertos seguidos)', [
        ('×3 seguidos   (el chip aparece desde 2)', '3 seguidos   (sigue apareciendo desde 2)'),
    ]),
    ('Correo Estelar', [
        ('¡Imparable! ×20   ·   ¡Maestro del correo! ×30', '¡Imparable! 20 seguidos   ·   ¡Maestro del correo! 30 seguidos'),
        ('Cartas bien puestas: 8 de 11 · ×5 seguidas', 'Cartas bien puestas: 8 de 11 · 5 seguidas'),
        ('Final del juego: «78 %» con «8 de 11 encargos cumplidos» debajo', 'Sin cambio: ya tenía el espacio ante el «%» y su «de qué» en la línea de abajo'),
    ]),
    ('Piloto Estelar', [
        ('¡Hiperimpulso!  ·  Puntos ×2', '¡Hiperimpulso!  ·  Puntos dobles'),
        ('1.250 puntos ×2   (el chip de puntos durante el hiperimpulso)', '1.250 puntos dobles'),
    ]),
    ('La estrella intrusa', [
        ('La nombraste tú · puntos ×2', 'La nombraste tú · puntos dobles'),
        ('¡Cielo despejado!  ·  Puntos × 1,5', '¡Cielo despejado!  ·  50 % más de puntos'),
    ]),
    ('¿Verdad o disparate?', [
        ('¡Transmisión perfecta!  ·  Antena encendida: puntos × 1,5', '¡Transmisión perfecta!  ·  Antena encendida: 50 % más de puntos'),
    ]),
    ('Engranajes', [
        ('Cohete n.º 3   (cabecera del cohete)', 'Cohete número 3   (el título pasa de 130 a 160 dp y las 10 luces empiezan en x = 184, cada 16: antes en 150, cada 19)'),
        ('Tu cohete n.º 3 · va a la órbita   (el despegue)', 'Tu cohete número 3 · va a la órbita'),
    ]),
    ('Aterrizaje Lunar', [
        ('Aterriza en 40%   (la misión)', 'Aterriza en 40 %'),
        ('0%  …  100%   (las marcas de la regla de porcentajes)', '0 %  …  100 %'),
        ('Arrastra la nave hasta el 40% y suelta   (el tutorial de Nubi)', 'Arrastra la nave hasta el 40 % y suelta'),
        ('N.º 3   (el chip del marcador cuando la partida es con reloj y no tiene total; lo atrapó la guardia nueva)', 'Aterrizaje 3'),
    ]),
    ('Carga exacta', [
        ('Se queda: el «×» de la cuenta es el signo de multiplicar de un operador, no un texto', 'Sin cambio (la guardia de `HudRulesTests` lo exceptúa por nombre)'),
    ]),
    ('Fuera de esta tarea (se piden aparte)', [
        ('+12   (el «+N» que flota al sumar puntos, en seis juegos)', 'Sin cambio: es una cifra flotante, no una frase; si Ricardo lo quiere, es otra tarea'),
    ]),
]


def main():
    caps_args = [a for a in sys.argv[1:] if os.path.exists(a.split('=')[0])]
    probe = ImageDraw.Draw(Image.new('RGB', (10, 10)))
    F_T, F_H, F_P, F_B, F_C = font(58), font(34), font(24, False), font(25, False), font(22, False)
    intro = ('Lo que la Tarea 160 dejó anotado para Unity, con la misma regla de Ricardo, «sin palabras sueltas»: ya no hay «×» (salvo el signo de multiplicar de Carga exacta), ni «n.º», ni un «%» pegado a la cifra. '
             'Una guardia nueva en HudRulesTests (NoGameDrawsSignsOrGluedPercents_InItsSources) mira el código de los juegos y falla si vuelve alguno; el sello de las compilaciones de depuración pasa a «sin signos 22-oct».')
    intro_l = wrap(probe, intro, F_P, W - 2 * MG)
    H = MG + 70 + 32 * len(intro_l) + 62
    bloques = []
    for titulo, pares in TABLA:
        filas = [(wrap(probe, a, F_B, COLW - 20), wrap(probe, b, F_B, COLW - 20)) for a, b in pares]
        bloques.append((titulo, filas))
        H += 60 + sum(max(len(la), len(lb)) * 31 + 18 for la, lb in filas) + 30
    caps = []
    for a in caps_args:
        path, _, rot = a.partition('=')
        im = Image.open(path).convert('RGB'); w = 440
        im = im.resize((w, int(im.height * w / im.width)), Image.LANCZOS)
        caps.append((rot or os.path.basename(path), im.crop((0, 0, w, min(im.height, 960)))))
    if caps: H += 60 + max(i.height for _, i in caps) + 80
    H += 40
    sheet = Image.new('RGB', (W, H), NAVY); d = ImageDraw.Draw(sheet); y = MG
    d.text((MG, y), 'Unity sin signos (Tarea 161): antes | después', font=F_T, fill=SUN); y += 70
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
        d.text((MG, y), 'Cómo queda (capturas reales de Unity en forma de teléfono)', font=F_H, fill=SUN); y += 56
        x = MG; h = max(i.height for _, i in caps)
        for rot, im in caps:
            sheet.paste(im, (x, y)); d.rectangle((x, y, x + im.width - 1, y + im.height - 1), outline=(70, 74, 130), width=1)
            for k, ln in enumerate(wrap(probe, rot, F_C, im.width)): d.text((x, y + im.height + 6 + k * 26), ln, font=F_C, fill=LIME)
            x += im.width + 24
        y += h + 80
    sheet = sheet.crop((0, 0, W, y + 20)); sheet.save(OUT)
    print('ok', OUT, sheet.size, '· capturas:', len(caps))


if __name__ == '__main__':
    main()
