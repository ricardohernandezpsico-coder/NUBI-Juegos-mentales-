# -*- coding: utf-8 -*-
"""Pistas del Contrarreloj más largas: ¿hasta qué paso llega una persona buena y en qué segundo? (propuesta del 4-nov-2026, `docs/alargar-dificultad.md` §A). SOLO análisis: no toca la app ni Unity.

Es una PORTA en Python del robot de la referencia estimada (`Games/Tests/AgeReference.cs`: lentitud lognormal por franja, exactitud normal, vaivén lognormal de 0,25 en cada respuesta, mejor marca de 3 partidas de 120 s) para Tinta o Palabra, ¿Verdad o disparate? y Acoplamiento.
Se CALIBRA contra las tablas que ya están en `app/src/main/assets/referencia/*.json` (p25/p50/p75/p90 de cada franja): si la porta no reproduce las marcas de hoy, no sirve para las pistas nuevas. Luego juega pistas ALARGADAS (más pasos, otra regla de avance) y dice,
por franja: la marca del 80, el 90 y el 99 %, el paso al que llega y el segundo en que llega al último paso. El valor exacto de la tabla definitiva sale de `bash tools/verificar-todo.sh --referencia` (Unity), no de aquí.
Uso:  python -I tools/alargar_pistas.py calibrar|stroop|disparate|acoplamiento
"""
import json
import math
import os
import random
import sys

sys.stdout.reconfigure(encoding='utf-8')
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
REF = os.path.join(ROOT, 'app', 'src', 'main', 'assets', 'referencia')
RANGES = ['R18_29', 'R30_44', 'R45_54', 'R55_64', 'R65_74', 'R75_PLUS']
SLOWNESS = [1.00, 1.06, 1.14, 1.24, 1.40, 1.62]
DISPERSION = [0.18, 0.19, 0.20, 0.22, 0.25, 0.28]
ACCURACY = [0.95, 0.95, 0.94, 0.93, 0.91, 0.89]
SENIOR = [False, False, False, False, True, True]
WITHIN = 0.25
ROBOTS = 1500
SECONDS = 120.0
LINES = (10, 20, 30, 40, 50, 60, 70, 80, 90, 99)


def lognorm(rng, median, sigma): return median * math.exp(rng.gauss(0.0, 1.0) * sigma)


def draw(rng, band, scale):
    return {'slow': lognorm(rng, SLOWNESS[band], DISPERSION[band]) * scale, 'acc': min(0.995, max(0.6, rng.gauss(ACCURACY[band], 0.04))), 'senior': SENIOR[band]}


def rt(rng, p, median): return lognorm(rng, median * p['slow'], WITHIN)


def percentile_of(values, q):
    v = sorted(values); return v[min(len(v) - 1, int(q * (len(v) - 1)))]


# ------------------------------------------------------------------ Tinta o Palabra (StroopContract, PlayStroop)
# nivel: (dos reglas, por bloques, prob. de cambio, % que chocan, llegada ms)
STROOP_LEVELS = [(False, False, 0.0, 0.60, 360), (False, False, 0.0, 0.75, 320), (True, True, 0.2, 0.75, 300), (True, False, 0.40, 0.80, 280), (True, False, 0.50, 0.85, 240)]


def play_stroop(rng, p, levels, every, steps):
    t = 0.0; hits = 0; errors = 0; n = 0; reached = None
    while True:
        level = min(steps, 1 + hits // every)
        both, blocks, sw, clash_share, arrive = levels[min(level, len(levels)) - 1]
        if level >= steps and reached is None: reached = t
        n += 1
        switched = both and n > 3 and rng.random() < sw
        clash = rng.random() < clash_share
        median = 1.25 * (1.0 if clash else 0.85) + (0.25 if switched else 0.0)
        ans = t + arrive / 1000.0 * (1.3 if p['senior'] else 1.0) + rt(rng, p, median)
        if ans > SECONDS: break
        acc = p['acc'] + (0.0 if clash else 0.04) - (0.04 if switched else 0.0)
        ok = rng.random() < min(0.995, max(0.5, acc))
        if ok: hits += 1
        else: errors += 1
        t = ans + (0.17 if ok else 0.8 + 0.16)
        if t >= SECONDS: break
    return max(0, hits - errors), hits, reached


# ------------------------------------------------------------------ Acoplamiento (DockingContract.NextTrial, PlayDocking)
# nivel: (celdas, giro máximo, paso del ángulo, plazo ms); los 12 de hoy
def docking_levels(extra=()):
    out = []
    for l in range(1, 13):
        out.append((4 + (l - 1) // 3 if l <= 9 else 7, 90 if l == 1 else 135 if l == 2 else 180, 45 if l < 5 else 15, 6000 - round(3500 * (l - 1) / 11)))
    return out + list(extra)


def play_docking(rng, p, levels, every, steps, cell_s=0.0):
    t = 0.0; hits = 0; errors = 0; reached = None
    while True:
        level = min(steps, 1 + hits // every)
        cells, mx, stp, deadline_ms = levels[min(level, len(levels)) - 1]
        if level >= steps and reached is None: reached = t
        angle = stp * rng.randrange(mx // stp + 1)
        if angle not in (0, 180) and rng.random() < 0.5: angle = -angle
        disp = abs(angle) % 360; disp = 360 - disp if disp > 180 else disp
        mirrored = rng.random() < 0.5
        decide = t + 0.42; deadline = deadline_ms / 1000.0
        resp = max(1.2, rt(rng, p, 1.3 + disp / 150.0 + (0.3 if mirrored else 0.0) + cell_s * max(0, cells - 7)))
        timed_out = resp >= deadline
        ans = decide + (deadline if timed_out else resp)
        if ans > SECONDS: break
        acc = p['acc'] + 0.03 - 0.0004 * disp
        ok = (not timed_out) and rng.random() < min(0.995, max(0.5, acc))
        if ok: hits += 1
        else: errors += 1
        t = ans + ((0.32 + (0.3 if mirrored else 0.0) + 0.38 + 0.22 + 0.52 + 0.38) if ok else (0.32 + (0.3 if mirrored else 0.0) + 0.38 + 0.6 + 0.2))
        if t >= SECONDS: break
    return max(0, hits - errors), hits, reached


# ------------------------------------------------------------------ ¿Verdad o disparate? (DisparateDirector, PlayDisparate): las frases reales del banco, por tipo
def _bank():
    path = os.path.join(ROOT, 'unity', 'NeuroVidaCore', 'Assets', 'Resources', 'Frases', 'disparate_es.json')
    by = {}
    for f in json.load(open(path, encoding='utf-8'))['frases']: by.setdefault(f['t'], []).append(f['n'])
    return by


BANK = _bank()
DISPARATE_HARD = [0.0, 0.01, 0.03, 0.02, 0.02, 0.03]


def play_disparate(rng, p, types, every, steps):
    """[types] = el tipo de frase de cada paso (hoy: 1,1,2,2,3,3,4,4,5,5,6,6); un paso nuevo puede reusar un tipo o mezclar dos con (tipo_a, tipo_b, peso_de_a)."""
    t = 0.0; hits = 0; errors = 0; reached = None
    while True:
        level = min(steps, 1 + hits // every)
        spec = types[min(level, len(types)) - 1]
        if level >= steps and reached is None: reached = t
        ty = spec if isinstance(spec, int) else (spec[0] if rng.random() < spec[2] else spec[1])
        words = rng.choice(BANK[ty])
        read = rt(rng, p, 0.7 + 0.3 * words)
        ans = t + 0.2 + max(max(1.5, 0.25 * words), read)
        if ans > SECONDS: break
        ok = rng.random() < min(0.995, max(0.5, p['acc'] - DISPARATE_HARD[ty - 1]))
        if ok: hits += 1
        else: errors += 1
        t = ans + (0.12 if ok else 0.8 + 0.12)
        if t >= SECONDS: break
    return max(0, hits - errors), hits, reached


GAMES = {'stroop': dict(scale=0.89, play=play_stroop, width=5, nbins=20, base=dict(levels=STROOP_LEVELS, every=8, steps=5)),
         'acoplamiento': dict(scale=0.90, play=play_docking, width=2, nbins=20, base=dict(levels=docking_levels(), every=2, steps=12)),
         'disparate': dict(scale=0.92, play=play_disparate, width=5, nbins=16, base=dict(types=[1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6], every=3, steps=12))}


def simulate(game, band, cfg, seed):
    rng = random.Random(seed)
    marks, hits, tops = [], [], []
    for _ in range(ROBOTS):
        p = draw(rng, band, GAMES[game]['scale'])
        runs = [GAMES[game]['play'](rng, p, **cfg) for _ in range(3)]
        best = max(runs, key=lambda r: r[0])
        marks.append(best[0]); hits.append(best[1]); tops.append(best[2])
    return marks, hits, tops


def calibrar(game):
    ref = json.load(open([os.path.join(REF, f) for f in os.listdir(REF) if f.startswith(game + '_')][0], encoding='utf-8'))
    print('%s: marcas de la porta contra la tabla del juego, p25 · p50 · p75 · p90' % game)
    for b, band in enumerate(RANGES):
        marks, _, _ = simulate(game, b, GAMES[game]['base'], 1000 + b)
        sim = [percentile_of(marks, q) for q in (0.25, 0.5, 0.75, 0.9)]
        tab = [ref['franjas'][band][k] for k in ('p25', 'p50', 'p75', 'p90')]
        print('  %-8s porta %s · tabla %s · diferencia %s' % (band, sim, tab, [s - t for s, t in zip(sim, tab)]))


def bins_of(marks, width, n):
    bins = [0] * n
    for m in marks: bins[min(n - 1, m // width)] += 1
    return bins


def pct(mark, bins, w):
    """`Comparison.percentile` (tramos sin suavizar, hacia abajo, tope 99)."""
    people = sum(bins); m = max(0, mark); last = len(bins) - 1; i = min(m // w, last)
    below = sum(bins[:i]) * w; within = 0 if i == last else bins[i] * (m - i * w)
    return max(0, min(99, (100 * (below + within)) // (w * people)))


def first_reaching(p, bins, w):
    for m in range(0, w * len(bins)):
        if pct(m, bins, w) >= p: return m
    return None


def informe(game, cfg, etiqueta, steps, every, every_label=None):
    """Por franja: la marca de las líneas 80, 90 y 99 %, el paso al que llegan, el segundo en que llegan al último (y el % de esas personas que llegan) y si las líneas 10 a 90 caen en marcas distintas."""
    g = GAMES[game]; w, nb = g['width'], g['nbins']
    print('')
    print('== %s · %s · %d pasos, uno cada %d aciertos (último a los %d)' % (game, etiqueta, steps, every, (steps - 1) * every))
    print('| franja | 80 %: marca · paso · segundo del último | 90 % | 99 % | 9 líneas distintas | 80→90 | 90→99 |')
    print('|---|---|---|---|---|---|---|')
    for b, band in enumerate(RANGES):
        marks, hits, tops = simulate(game, b, cfg, 7000 + b)
        bins = bins_of(marks, w, nb)
        lines = {q: first_reaching(q, bins, w) for q in LINES}
        cells = []
        for q in (80, 90, 99):
            m = lines[q]
            near = [i for i in range(len(marks)) if m is not None and abs(marks[i] - m) <= 1]
            hh = percentile_of([hits[i] for i in near], 0.5) if near else 0
            ts = [tops[i] for i in near if tops[i] is not None]
            reach = len(ts) / max(1, len(near))
            step = min(steps, 1 + hh // every)
            cells.append('%s · %d de %d · %s' % (m, step, steps, ('%.0f s (%.0f %%)' % (percentile_of(ts, 0.5), 100 * reach)) if ts else 'no llega'))
        distinct = len({lines[q] for q in LINES if q < 99}) == 9
        sep = (lines[90] - lines[80], lines[99] - lines[90]) if None not in (lines[80], lines[90], lines[99]) else ('—', '—')
        print('| %s | %s | %s | %s | %s | %s | %s |' % (band, cells[0], cells[1], cells[2], 'sí' if distinct else 'NO', sep[0], sep[1]))


def stroop_cfg(extra, every):
    levels = STROOP_LEVELS + extra
    return dict(levels=levels, every=every, steps=len(levels))


def docking_cfg(n_levels, every, cell_s=0.0):
    """Los 12 niveles de hoy y, del 13 en adelante, 8 a 10 celdas y el mismo plazo de 2,5 s (el robot no ve las celdas: ver el documento)."""
    extra = [(min(10, 8 + (l - 13) // 2), 180, 15, 2500) for l in range(13, n_levels + 1)]
    return dict(levels=docking_levels(extra), every=every, steps=n_levels, cell_s=cell_s)


def disparate_cfg(n_levels, every):
    """Los 12 niveles de hoy (dos por tipo) y, del 13 al 14, un tipo 7 NUEVO: frases largas (las del tipo 4 con 2 palabras más, hasta 12) con la exactitud del tipo 6 (SUPUESTO: aún no hay frases)."""
    BANK[7] = [min(12, w + 2) for w in BANK[4]]
    while len(DISPARATE_HARD) < 7: DISPARATE_HARD.append(0.03)
    types = [1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6] + [7] * max(0, n_levels - 12)
    return dict(types=types, every=every, steps=n_levels)


if __name__ == '__main__':
    cmd = sys.argv[1] if len(sys.argv) > 1 else 'calibrar'
    if cmd == 'calibrar':
        for g in ('stroop', 'acoplamiento', 'disparate'): calibrar(g)
    if cmd == 'stroop':
        informe('stroop', GAMES['stroop']['base'], 'HOY', 5, 8)
        extra = [(True, False, 0.55, 0.88, 210), (True, False, 0.60, 0.90, 180), (True, False, 0.65, 0.90, 150)]
        for every in (10, 12):
            cfg = stroop_cfg(extra, every)
            informe('stroop', cfg, '3 niveles nuevos (cambios 0,55/0,60/0,65; choques 0,88/0,90/0,90; llegada 210/180/150 ms)', cfg['steps'], every)
    if cmd == 'acoplamiento':
        informe('acoplamiento', GAMES['acoplamiento']['base'], 'HOY', 12, 2)
        for n, every in ((12, 3), (15, 2), (17, 2), (18, 2)):
            informe('acoplamiento', docking_cfg(n, every), '%d niveles' % n, n, every)
        informe('acoplamiento', docking_cfg(17, 2, 0.15), '17 niveles, HIPÓTESIS: cada celda sobre 7 suma 0,15 s de respuesta', 17, 2)
    if cmd == 'disparate':
        informe('disparate', GAMES['disparate']['base'], 'HOY', 12, 3)
        for n, every in ((12, 4), (14, 4), (14, 3)):
            informe('disparate', disparate_cfg(n, every), '%d niveles (tipo 7 nuevo desde el 13)' % n, n, every)
