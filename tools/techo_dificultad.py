# -*- coding: utf-8 -*-
"""Techo de la dificultad (informe de 4-nov-2026; `docs/techo-dificultad.md`). SOLO análisis: no toca la app ni Unity.

Qué hace: (1) porta a Python la regla de rating de `Games/AdaptiveDifficulty.cs` (up-down ponderado de Kaernbach, calibración ×1,5 en los 6 primeros ensayos, bono de +0,20 cada 6 aciertos seguidos, red anti-frustración, tope `MaxLevel + 0,99`) y el paso entre partidas de la app (`Skill.nextRating`:
60 % partida + 40 % lo anterior, `blendDdaRating`); (2) simula a una persona que acierta p = 95 % (y, de sensibilidad, 90 %) en TODOS los niveles y cuenta en qué ensayo y en qué partida de «A tu medida» toca el último nivel y en cuál ya EMPIEZA ahí; (3) lee las tablas de la referencia estimada del Contrarreloj
(`app/src/main/assets/referencia/*.json`) y estima cuándo se llega al último paso de cada pista.
Uso:  python -I tools/techo_dificultad.py [--json]      → imprime las tablas en Markdown (con --json deja `tools/techo_dificultad.json` con los números crudos; tarda unos 25 s)
Supuestos que importan (están también en el documento): modulación por tiempo de reacción = 1 (ningún juego la usa salvo Freno y Tinta con reloj: ignorarla es lo prudente); el ensayo es el que cada juego le da al motor (`_dda.Register`); la partida de «A tu medida» es la SIN reloj (Precisión): el camino diario de Hoy va siempre sin reloj
y los 4 juegos del Contrarreloj ya no ofrecen «¿Con reloj?»; la persona nueva parte del nivel 1 (`getEffectiveLevelForGame` = 1 sin historial) y su primera partida no tiene subida guiada porque ya parte en lo más fácil.
"""
import json
import os
import random
import statistics
import sys

sys.stdout.reconfigure(encoding='utf-8')
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# ------------------------------------------------------------------ el motor (AdaptiveDifficulty.cs)
WARMUP, PROVISIONAL, PROVISIONAL_BOOST = 2, 6, 1.5
STRUGGLE_EXTRA, BOREDOM_STREAK, BOREDOM_BONUS, MAX_DROP_PER_ERROR = 0.15, 6, 0.20, 1.0


class Dda:
    def __init__(self, max_level, senior, start_rating, step_up, target=None):
        self.max_level = max(2, max_level)
        self.target = target if target is not None else (0.85 if senior else 0.80)
        self.step = step_up * (0.85 if senior else 1.0)
        self.min_r, self.max_r = 1.0, self.max_level + 0.99
        self.rating = max(self.min_r, min(self.max_r, start_rating))
        self.trials = 0
        self.ok_run = 0
        self.err_run = 0

    @property
    def level(self):
        return max(1, min(self.max_level, int(self.rating)))

    def register(self, correct):
        self.trials += 1
        prov = PROVISIONAL_BOOST if self.trials <= PROVISIONAL else 1.0
        if correct:
            self.ok_run += 1
            self.err_run = 0
            delta = self.step * prov
            if self.ok_run % BOREDOM_STREAK == 0: delta += BOREDOM_BONUS
        else:
            self.ok_run = 0
            self.err_run += 1
            ratio = self.target / (1.0 - self.target)
            delta = -min(self.step * ratio, MAX_DROP_PER_ERROR) * prov
            if self.err_run >= 2: delta -= STRUGGLE_EXTRA
        self.rating = max(self.min_r, min(self.max_r, self.rating + delta))

    def norm(self):
        return max(0.0, min(1.0, (self.rating - 1.0) / self.max_level))


def blend(previous, end):
    """`blendDdaRating`: 60 % la partida, 40 % lo anterior; sin dato, la partida."""
    return end if previous < 0 else max(0.0, min(1.0, 0.6 * end + 0.4 * previous))


# ------------------------------------------------------------------ los 17 juegos: escalera, paso, ensayos por partida «A tu medida» (sin reloj)
# (id, nombre, área, niveles, stepUp, objetivo propio, ensayos por partida, nota del ensayo)
GAMES = [
    ('parejas', 'Constelaciones', 'Memoria', 18, 0.20, None, 24, 'cada oportunidad de memoria; 6 cielos, de unas 3 a 12 oportunidades cada uno (24 es una cifra media)'),
    ('secuencia', 'Rastro de luz', 'Memoria', 16, 0.25, None, 14, 'cada ronda; 14 rondas (paso 0,25 adultos y 0,17 mayores)'),
    ('bodega', 'Bodega de carga', 'Memoria', 12, 0.20, None, 6, 'cada pedido; 6 pedidos'),
    ('correo', 'Correo Estelar', 'Memoria', 10, 0.30, None, 10, 'cada encargo; 4 días de 1 a 5 encargos (unos 10)'),
    ('stroop', 'Tinta o Palabra', 'Atención', 5, 0.20, None, 16, 'cada palabra; 16 palabras (paso 0,20 en Precisión)'),
    ('piloto', 'Piloto Estelar', 'Atención', 9, 0.25, 0.80, 24, 'MOTOR DE SEÑALES: cada señal; 24 (el pilotaje es otro motor: abajo)'),
    ('freno', 'Freno de Emergencia', 'Atención', 12, 0.15, None, 28, 'solo los lanzamientos de IR: 40 con 30 % de altos = 28'),
    ('satelites', 'Satélites', 'Atención', 12, 1.00, None, 8, 'cada ronda (perfecta = acierto); 8 rondas; paso 1,0'),
    ('radar', 'Rescate relámpago', 'Atención', 12, 0.30, None, 12, 'cada ronda (sin las lluvias); 12 rondas'),
    ('calculo', 'Carga exacta', 'Razonamiento', 5, 0.40, None, 8, 'cada carga; 8 cargas'),
    ('engranajes', 'Engranajes', 'Razonamiento', 12, 0.50, None, 10, 'cada máquina; 10 máquinas; paso 0,5'),
    ('acoplamiento', 'Acoplamiento', 'Razonamiento', 12, 0.30, None, 24, 'cada pieza; 24 piezas'),
    ('aterrizaje', 'Aterrizaje Lunar', 'Razonamiento', 12, 0.30, None, 15, 'cada aterrizaje; 15'),
    ('anagramas', 'En la punta de la lengua', 'Lenguaje', 5, 0.40, None, 8, 'cada palabra; 8 palabras'),
    ('meteoros', 'Lluvia de meteoros', 'Lenguaje', 12, 0.15, None, 40, 'cada meteoro; 40'),
    ('disparate', '¿Verdad o disparate?', 'Lenguaje', 12, 0.15, None, 30, 'cada frase; 30 frases'),
    ('intrusa', 'La estrella intrusa', 'Lenguaje', 12, 0.40, None, 20, 'cada ronda; 20 rondas'),
]
EXTRA = [('piloto_pilotaje', 'Piloto Estelar (pilotaje)', 'Atención', 9, 0.15, 0.85, 40, 'MOTOR DE PILOTAJE: cada ventana de 1,5 s; unas 40 por vuelo (estimado)')]

PEOPLE = 3000
MAX_PARTIDAS = 80


def simulate(game, senior, p, start_norm=None, seed=1):
    """Devuelve (mediana, p90) de: ensayos hasta tocar el último nivel, partida en que lo toca y partida desde la que EMPIEZA ahí."""
    gid, name, area, L, step, target, T, note = game
    rng = random.Random(seed)
    touch_trials, touch_game, start_game = [], [], []
    for _ in range(PEOPLE):
        saved = -1.0 if start_norm is None else start_norm
        total, g_touch, t_touch, g_start = 0, None, None, None
        for g in range(1, MAX_PARTIDAS + 1):
            start = 1.0 if saved < 0 else min(L + 0.99, 1.0 + saved * L)
            d = Dda(L, senior, start, step, target)
            if g_start is None and start >= L: g_start = g
            for _t in range(T):
                d.register(rng.random() < p)
                total += 1
                if g_touch is None and d.rating >= L:
                    g_touch, t_touch = g, total
            end = d.norm()
            saved = end if saved < 0 else blend(saved, end)
            if g_touch is not None and g_start is not None: break
        touch_trials.append(t_touch if t_touch else 10 ** 6)
        touch_game.append(g_touch if g_touch else 10 ** 6)
        start_game.append(g_start if g_start else 10 ** 6)

    def q(xs, f):
        xs = sorted(xs)
        v = xs[min(len(xs) - 1, int(f * (len(xs) - 1)))]
        return None if v >= 10 ** 6 else v
    return {'ensayos_mediana': q(touch_trials, 0.5), 'ensayos_p90': q(touch_trials, 0.9), 'toca_partida_mediana': q(touch_game, 0.5), 'toca_partida_p90': q(touch_game, 0.9),
            'empieza_arriba_partida_mediana': q(start_game, 0.5), 'empieza_arriba_partida_p90': q(start_game, 0.9)}


def at_top(game, senior):
    """Qué pasa arriba: el rating queda en MaxLevel + 0,99 (el bono ya no mueve nada) y el avance guardado tiende a 1 (los puntos del juego, a su máximo)."""
    L = game[3]
    return {'rating_techo': L + 0.99, 'norm_techo': round((L + 0.99 - 1.0) / L, 4)}


def main():
    out = {'supuestos': {'personas': PEOPLE, 'partidas_max': MAX_PARTIDAS}, 'juegos': {}}
    rows = []
    for game in GAMES + EXTRA:
        gid = game[0]
        res = {}
        for label, senior in (('adulto', False), ('65+', True)):
            for p in (0.95, 0.90):
                res['%s_%d' % (label, round(p * 100))] = simulate(game, senior, p)
            res['%s_95_desde_mitad' % label] = simulate(game, senior, 0.95, start_norm=0.5)
        res['arriba'] = at_top(game, False)
        out['juegos'][gid] = {'nombre': game[1], 'area': game[2], 'niveles': game[3], 'paso': game[4], 'ensayos_por_partida': game[6], 'nota': game[7], 'sim': res}
        rows.append(gid)
    if '--json' in sys.argv:                                   # los números crudos, por si se quieren reusar (no se versiona)
        with open(os.path.join(ROOT, 'tools', 'techo_dificultad.json'), 'w', encoding='utf-8') as f:
            json.dump(out, f, ensure_ascii=False, indent=1)

    def cell(r):
        if r['toca_partida_mediana'] is None: return 'nunca (80)'
        s = '%d ens. · partida %d' % (r['ensayos_mediana'], r['toca_partida_mediana'])
        a = r['empieza_arriba_partida_mediana']
        return s + (' · arriba desde la %d' % a if a else '')
    print('| Juego | Niveles | Ens./partida | Adulto 95 % | 65+ 95 % | Adulto 90 % | Adulto 95 % desde la mitad |')
    print('|---|---|---|---|---|---|---|')
    for gid in rows:
        j = out['juegos'][gid]; s = j['sim']
        print('| %s | %d | %d | %s | %s | %s | %s |' % (j['nombre'], j['niveles'], j['ensayos_por_partida'], cell(s['adulto_95']), cell(s['65+_95']), cell(s['adulto_90']), cell(s['adulto_95_desde_mitad'])))

    # ---- el Contrarreloj, con las tablas de la referencia estimada
    tops = {'stroop': (5, 8, 'aciertos'), 'disparate': (12, 3, 'aciertos'), 'acoplamiento': (12, 2, 'aciertos'), 'radar': (12, 3, 'RONDAS logradas')}
    print()
    for gid, (L, every, unit) in tops.items():
        top = (L - 1) * every
        path = os.path.join(ROOT, 'app', 'src', 'main', 'assets', 'referencia')
        for fn in os.listdir(path):
            if fn.startswith(gid + '_'):
                d = json.load(open(os.path.join(path, fn), encoding='utf-8'))
        print('%s: último paso %d con %d %s' % (gid, L, top, unit))
        for fr, v in d['franjas'].items():
            tr = v['tramos']; n = sum(tr); w = d['ancho_tramo']
            def frac(m):
                acc = 0.0
                for i, c in enumerate(tr):
                    lo = i * w; hi = lo + w - 1 if i < len(tr) - 1 else 10 ** 9
                    if lo >= m: acc += c
                    elif hi >= m: acc += c * (hi - m + 1) / w
                return acc / n
            est = lambda mark: 120.0 * top / (mark + 2.0)
            print('  %-8s p25 %3d p50 %3d p75 %3d p90 %3d · marca >= %d: %3.0f %% · segundo del último paso (proporcional) p50 %s p90 %s' % (
                fr, v['p25'], v['p50'], v['p75'], v['p90'], top, 100 * frac(top), ('%.0f' % est(v['p50']) if gid != 'radar' else '—'), ('%.0f' % est(v['p90']) if gid != 'radar' else '—')))


main()
