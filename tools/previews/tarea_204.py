# -*- coding: utf-8 -*-
"""Tabla de la Tarea 204 (1-nov-2026): la referencia estimada del Contrarreloj, producida por los robots por edad de Unity (`AgeReference`), contra la simulación ilustrativa de Fable (`auditoria-fable/referencia-estimada.md` §2.4, hecha en Python con una pista generada por paso).
Por juego y franja: la MITAD que dio Fable con sus dos escalas (la del laboratorio, 1,0, y la de un teléfono 30 % más lento, 1,3; la mejor de 3 partidas), y la de los robots reales con la escala 1,0: con 3 partidas por semana en días distintos (lo que se guarda en la app: las personas de Ricardo
juegan 3 Contrarrelojes por juego) y con 7 (todos los días de la semana). Cada robot juega la pista REAL del día; de cada uno se toma su mejor marca de la semana.
Datos: `app/src/main/assets/referencia/*.json` (3 partidas, el que lee la app) y `unity/test-results/referencia-7dias/*.json` (7 partidas; se genera con  NUBI_REF_PARTIDAS=7 NUBI_REF_OUT=unity/test-results/referencia-7dias bash tools/verificar-todo.sh --referencia  y no va a git).
Uso:  python -I tools/previews/tarea_204.py      → docs/previews/tarea-204.png
"""
import json, os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
THREE = os.path.join(ROOT, 'app', 'src', 'main', 'assets', 'referencia')
SEVEN = os.path.join(ROOT, 'unity', 'test-results', 'referencia-7dias')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-204.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
NAVY, SUN, DIM, CREAM, LINE = (20, 18, 63), (255, 203, 69), (200, 196, 235), (245, 235, 219), (90, 86, 160)

KEYS = ['R18_29', 'R30_44', 'R45_54', 'R55_64', 'R65_74', 'R75_PLUS']
LABELS = ['18 a 29', '30 a 44', '45 a 54', '55 a 64', '65 a 74', '75 o más']
# la mitad (p50) de la §2.4 de Fable, con la escala 1,0 y con la 1,3 (400 personas por franja, mejor de 3 partidas)
FABLE = {
    'stroop': ('Tinta o Palabra', 'respuesta joven 1,25 s · 1,62 s', [61, 59, 55, 51, 43, 38], [51, 48, 45, 42, 35, 31]),
    'disparate': ('¿Verdad o disparate?', '0,30 s por palabra · 0,39 s', [37, 37, 34, 31, 29, 24], [32, 30, 28, 26, 22, 19]),
    'radar': ('Rescate relámpago', 'respuesta 2,2 s · 2,9 s', [14, 14, 13, 12, 10, 9], [13, 13, 12, 11, 9, 7]),
    'acoplamiento': ('Acoplamiento', 'decidir 1,3 s · 1,7 s', [23, 22, 20, 19, 16, 13], [18, 17, 16, 13, 10, 6]),
}


def font(px, bold=True): return ImageFont.truetype(os.path.join(FONTS, 'fredoka.ttf' if bold else 'nunito_regular.ttf'), int(px))


def load(folder, game):
    p = os.path.join(folder, f'{game}_v2.json')
    return json.load(open(p, encoding='utf-8')) if os.path.exists(p) else None


def main():
    W = 1500
    ft, fh, fb, fs = font(24), font(17), font(17, False), font(15, False)
    cols = [('franja', 110), ('Fable 1,0', 100), ('Fable 1,3', 100), ('robots · 3 partidas (lo que se guarda)', 420), ('robots · 7 partidas', 330), ('3 partidas menos Fable 1,0', 240)]
    rows_h = 34
    blocks = [(game, load(THREE, game), load(SEVEN, game)) for game in FABLE]
    H = 120 + len(blocks) * (70 + 34 + 6 * rows_h + 60) + 100
    im = Image.new('RGB', (W, H), NAVY)
    d = ImageDraw.Draw(im)
    d.text((30, 24), 'Referencia estimada del Contrarreloj · robots por edad (Tarea 204) contra la simulación de Fable', font=ft, fill=SUN)
    d.text((30, 62), 'Cada robot es una persona simulada que juega la pista REAL con sus números de franja; la marca es la mejor de su semana. Escala 1,0 en los cuatro juegos (medianas de laboratorio). NO son personas.', font=fs, fill=DIM)
    d.text((30, 84), 'Cuartiles (p25 · mitad · p75 · p90) de 2.000 robots por franja; «Fable» = la mitad de su simulación ilustrativa con dos escalas (400 personas, la mejor de 3 partidas).', font=fs, fill=DIM)
    y = 120
    for game, t3, t7 in blocks:
        name, scales, f10, f13 = FABLE[game]
        width = (t3 or {}).get('ancho_tramo', '?')
        d.text((30, y), name, font=ft, fill=CREAM)
        d.text((330, y + 6), f'tramos de {width} · Fable: {scales}', font=fs, fill=DIM)
        y += 40
        x = 30
        for title, w in cols:
            d.text((x, y), title, font=fh, fill=SUN); x += w
        y += 30
        d.line((30, y - 4, W - 30, y - 4), fill=LINE)
        for i, key in enumerate(KEYS):
            x = 30
            r3 = t3['franjas'][key] if t3 else None
            r7 = t7['franjas'][key] if t7 else None
            cells = [
                LABELS[i], str(f10[i]), str(f13[i]),
                f"{r3['p25']} · {r3['p50']} · {r3['p75']} · {r3['p90']}" if r3 else '—',
                f"{r7['p25']} · {r7['p50']} · {r7['p75']} · {r7['p90']}" if r7 else '—',
                (f"{r3['p50'] - f10[i]:+d}" if r3 else '—'),
            ]
            for (title, w), cell in zip(cols, cells):
                d.text((x, y), cell, font=fb, fill=CREAM if title != 'franja' else DIM); x += w
            y += rows_h
        y += 24
    d.text((30, y), 'Con 3 partidas por semana y la pista real los robots caen a ±2 aciertos de la mitad de Fable en los cuatro juegos; con 7 la mitad sube 1 a 3 aciertos más en Tinta y en ¿Verdad o disparate?.', font=fs, fill=DIM)
    d.text((30, y + 22), 'El orden entre franjas se conserva. Rescate relámpago (24 tramos de a 1): los robots llegan como mucho a 16 —una ronda dura más de 7 s—, así que entre los más jóvenes el 47 % cae en 14 y el 20 % en 15: su techo real, no un efecto de los tramos.', font=fs, fill=DIM)
    im.crop((0, 0, W, y + 70)).save(OUT)
    print(OUT)


main()
