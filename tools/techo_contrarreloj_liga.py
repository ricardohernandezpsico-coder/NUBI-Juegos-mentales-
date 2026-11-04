# -*- coding: utf-8 -*-
"""¿Alcanza la pista del Contrarreloj para separar bien ARRIBA? (informe del 4-nov-2026, `docs/techo-dificultad.md` §9). SOLO análisis.

La liga de la semana tendrá 10 divisiones con líneas cada 10 % del «superas al N %» promedio de la semana (0, 10, 20 … 90). Para cada juego del Contrarreloj y cada franja de edad, con las tablas de la referencia estimada
(`app/src/main/assets/referencia/*.json`) y la MISMA regla del percentil que usa la app (`Comparison.percentile` y `firstMarkReaching`: sobre los tramos sin suavizar, hacia abajo, tope 99):
  · la marca con que se llega a cada línea (10 … 90) y a 99;
  · a qué paso de la pista llega esa marca (`TimeTrial.LevelFor`: 1 + aciertos / cada_N, con tope en el último) suponiendo 2 errores (aciertos = marca + 2);
  · en qué segundo llega al último paso (ritmo parejo: 120 · aciertos del último paso / aciertos de la partida; es una cota inferior) y cuántos segundos se queda ahí.
Rescate relámpago cuenta RONDAS logradas (una cada 3) y su marca son cápsulas (2 a 3 por ronda: se usan 2,7); no llega nunca arriba.
Uso:  python -I tools/techo_contrarreloj_liga.py
"""
import json
import os
import sys

sys.stdout.reconfigure(encoding='utf-8')
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
REF = os.path.join(ROOT, 'app', 'src', 'main', 'assets', 'referencia')
# (juego, nombre, niveles, cada cuántos sube, unidad)
TRACKS = [('stroop', 'Tinta o Palabra', 5, 8, 'aciertos'), ('disparate', '¿Verdad o disparate?', 12, 3, 'aciertos'),
          ('acoplamiento', 'Acoplamiento', 12, 2, 'aciertos'), ('radar', 'Rescate relámpago', 12, 3, 'rondas')]
BANDS = {'R18_29': '18-29', 'R30_44': '30-44', 'R45_54': '45-54', 'R55_64': '55-64', 'R65_74': '65-74', 'R75_PLUS': '75+'}
LINES = [10, 20, 30, 40, 50, 60, 70, 80, 90, 99]
ERRORS = 2
CAPSULES_PER_ROUND = 2.7


def percentile(mark, bins, w):
    """`Comparison.percentile`: los tramos de abajo, enteros, más la parte del propio tramo que queda debajo de la marca; hacia abajo, tope 99."""
    people = sum(bins)
    m = max(0, mark)
    last = len(bins) - 1
    i = min(m // w, last)
    below = sum(bins[:i]) * w
    within = 0 if i == last else bins[i] * (m - i * w)
    return max(0, min(99, (100 * (below + within)) // (w * people)))


def first_mark_reaching(p, bins, w):
    """`Comparison.firstMarkReaching`: el primer acierto con que el percentil llega a p (None si ni con el borde de la pista se llega)."""
    for m in range(0, w * len(bins)):
        if percentile(m, bins, w) >= p: return m
    return None


def step_of(game, mark, L, every):
    if game == 'radar':
        rounds = (mark + ERRORS) / CAPSULES_PER_ROUND
        return min(L, 1 + int(rounds // every)), rounds
    hits = mark + ERRORS
    return min(L, 1 + hits // every), hits


def main():
    for game, name, L, every, unit in TRACKS:
        d = None
        for fn in os.listdir(REF):
            if fn.startswith(game + '_'): d = json.load(open(os.path.join(REF, fn), encoding='utf-8'))
        w = d['ancho_tramo']; top_hits = (L - 1) * every
        print('\n### %s — %d pasos, el último a los %d %s (uno cada %d); tramos de a %d, borde de la pista en %d' % (name, L, top_hits, unit, every, w, w * d['tramos']))
        print('| Franja | ' + ' | '.join('%d %%' % p for p in LINES) + ' | paso en 80 % · 90 % · 99 % | último paso (s) en 80 % · 90 % · 99 % |')
        print('|---|' + '---|' * len(LINES) + '---|---|')
        for band, label in BANDS.items():
            bins = d['franjas'][band]['tramos']
            marks = [first_mark_reaching(p, bins, w) for p in LINES]
            steps, secs = [], []
            for p in (80, 90, 99):
                m = marks[LINES.index(p)]
                if m is None: steps.append('—'); secs.append('—'); continue
                s, amount = step_of(game, m, L, every); steps.append('%d de %d' % (s, L))
                if game == 'radar': secs.append('no llega')
                elif s >= L: t = 120.0 * top_hits / amount; secs.append('%.0f (quedan %.0f)' % (t, 120 - t))
                else: secs.append('no llega')
            print('| %s | ' % label + ' | '.join('—' if m is None else str(m) for m in marks) + ' | ' + ' · '.join(steps) + ' | ' + ' · '.join(secs) + ' |')
        # separación: ¿cuánto distan las líneas de 80 y 90, y de 90 y 99, en aciertos?
        out = []
        for band, label in BANDS.items():
            bins = d['franjas'][band]['tramos']
            m80, m90, m99 = (first_mark_reaching(p, bins, w) for p in (80, 90, 99))
            out.append('%s: 80→90 = %s, 90→99 = %s' % (label, '—' if None in (m80, m90) else m90 - m80, '—' if None in (m90, m99) else m99 - m90))
        print('Separación en aciertos entre líneas: ' + ' · '.join(out))
        # ¿las nueve líneas de 10 a 90 son marcas DISTINTAS en todas las franjas? (si dos líneas caen en la misma marca, una de las 10 divisiones queda vacía)
        clash = []
        for band, label in BANDS.items():
            ms = [first_mark_reaching(p, d['franjas'][band]['tramos'], w) for p in range(10, 100, 10)]
            if len(set(ms)) != len(ms) or None in ms: clash.append(label)
        print('Franjas con dos líneas de 10 a 90 en la MISMA marca: ' + (', '.join(clash) if clash else 'ninguna'))


main()
