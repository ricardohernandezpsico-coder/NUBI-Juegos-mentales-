#!/usr/bin/env python3
"""Imprime en Markdown las tablas de docs/sonido-madera-plan.md que salen del catálogo medido (tools/sonido/catalogo.json):
inventario por juego (con niveles de hoy), la paleta medida, la afinación de las notas con información, la latencia de inicio y la memoria.
Uso: python tools/sonido/tablas_plan.py [inventario|paleta|afinacion|latencia|memoria|parejo]  (sin argumento imprime todo)."""
import io
import json
import math
import os
import sys
import statistics

if hasattr(sys.stdout, 'reconfigure'):
    sys.stdout.reconfigure(encoding='utf-8')
RAIZ = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
CAT = json.load(io.open(os.path.join(RAIZ, 'tools', 'sonido', 'catalogo.json'), encoding='utf-8'))
CLIPS = CAT['clips']
GAIN_WOOD = CAT['playVolume']

GRUPOS = [
    ('comun', 'Lo común'), ('parejas', 'Constelaciones'), ('secuencia', 'Rastro de luz'), ('bodega', 'Bodega de carga'), ('correo', 'La estación de correo'),
    ('stroop', 'Tinta o Palabra'), ('piloto', 'Piloto Estelar'), ('freno', 'Freno de emergencia'), ('satelites', 'Satélites'), ('radar', 'Rescate relámpago (ya en Madera)'),
    ('calculo', 'Carga exacta'), ('engranajes', 'Engranajes'), ('acoplamiento', 'Acoplamiento (ya en Madera)'), ('aterrizaje', 'Aterrizaje Lunar (ya en Madera)'),
    ('anagramas', 'En la punta de la lengua'), ('meteoros', 'Lluvia de meteoros'), ('disparate', '¿Verdad o disparate?'), ('intrusa', 'La estrella intrusa'),
]


def db(g):
    return 20 * math.log10(g)


def f1(v):
    return '—' if v is None else ('%.1f' % v).replace('.', ',')


def f2(v):
    return '—' if v is None else ('%.2f' % v).replace('.', ',')


def lufs_hoy(e):
    c = CLIPS.get(e['hoy']) if e['hoy'] else None
    return (c['peakDb'] + db(e['hoyGain']), c['lufs'] + db(e['hoyGain'])) if c else (None, None)


def inventario():
    out = []
    for gid, titulo in GRUPOS:
        evs = [e for e in CAT['events'] if e['group'] == gid]
        out.append('\n### %s\n' % titulo)
        out.append('| Evento | Hoy suena | Dónde se llama | Pico / LUFS hoy | Mapa a Madera | Info |')
        out.append('|---|---|---|---|---|---|')
        for e in evs:
            pico, lufs = lufs_hoy(e)
            niv = '—' if pico is None else '%s / %s' % (f1(pico), f1(lufs))
            if e['hoy'] and e['hoy'].startswith('fam.'):
                niv = 'ver afinación'
            flag = e['flag'] or ''
            out.append('| %s | %s | %s | %s | %s | %s |' % (e['title'], e['today'], e['sites'], niv, e['map'], flag))
    return '\n'.join(out)


# la paleta: descripción de cada evento (instrumento, altura, envolvente) — lo escrito en WoodSounds.cs
PALETA = [
    ('tap', 'Tap', 'Micro', 'marimba apagada', '392 Hz (sol4)', 'ataque 3 ms, τ 0,06 s'),
    ('unselect', 'Unselect', 'Micro', 'kalimba apagada', '392 Hz', 'ataque 2 ms, τ 0,07 s'),
    ('tick', 'Tick', 'Micro', 'bloque de madera', '1250 Hz + parcial ×2,71', 'ataque 1 ms, τ 35 ms'),
    ('count3', 'Count(3,2,1)', 'Micro', 'marimba', 'do5, re5, mi5 (un escalón por número)', 'ataque 3 ms, τ 0,12 s'),
    ('pausein', 'PauseIn', 'Micro', '2 marimbas, 0,1 s entre una y otra', 'mi5 → do5 (baja)', 'τ 0,15 s'),
    ('pauseout', 'PauseOut', 'Micro', '2 marimbas', 'do5 → mi5 (sube)', 'τ 0,15 s'),
    ('select0', 'Select(k)', 'Respuesta', 'kalimba', 'la de la escala: do5 a la6 (k = 0 a 9)', 'ataque 2 ms, τ 0,45 s'),
    ('correct1', 'Correct(racha)', 'Respuesta', '2 kalimbas, 90 ms entre una y otra', 'P[s] y P[s+2]: la pareja sube con la racha (1 a 7)', 'τ 0,30 s y 0,42 s'),
    ('wrong', 'Wrong', 'Respuesta', '2 marimbas graves', '196 Hz → 164,8 Hz (baja)', 'τ 0,25 s, 0,13 s entre notas'),
    ('knock', 'Knock', 'Respuesta', 'golpe de madera hueco', '120 → 70 Hz y marimba de 185 Hz', 'τ 0,08 s'),
    ('hint', 'Hint', 'Respuesta', 'soplo que sube y kalimba grave', 'ruido 400 → 1600 Hz; kalimba de 392 Hz a los 0,32 s', 'τ 0,2 s / 0,5 s'),
    ('new', 'New', 'Respuesta', '2 kalimbas agudas, 0,12 s entre una y otra', 'do6 → mi6 (sube)', 'τ 0,4 s / 0,5 s'),
    ('notice', 'Notice', 'Respuesta', 'marimba', 'do6', 'τ 0,2 s'),
    ('spark1', 'Spark(n)', 'Respuesta', 'kalimba aguda', 'do6 a mi7 (sube con n = 1 a 8)', 'τ 0,25 s'),
    ('streak', 'Streak', 'Hito', 'arpegio de kalimba sobre marimba grave', 'mi5, la5, do6 sobre do5', 'τ 0,35 s'),
    ('levelup', 'LevelUp', 'Hito', 'arpegio de kalimba', 'do5, mi5, sol5, do6 (65 ms entre notas)', 'τ 0,35 s'),
    ('perfect', 'Perfect', 'Hito', 'marimba y 2 kalimbas (la de Rescate)', 'do-mi-sol-do y do6, mi6', 'τ 0,35 s'),
    ('go', 'Go', 'Hito', '2 marimbas y 1 kalimba aguda', 'do5, sol5 y do6', 'τ 0,25 s / 0,4 s'),
    ('record', 'Record', 'Cierre', 'escalera de kalimba y acorde de marimba', 'do5 a la6, acorde do4-sol4-do5', 'τ 0,35 s / 0,5 s'),
    ('finish', 'Finish', 'Cierre', 'acorde de marimba y kalimba larga (el de Rescate)', '7 notas do4 a do6 y mi6', 'τ 0,4 s / 0,6 s'),
    ('sweepup', 'SweepUp', 'Textura', 'ruido de banda', '500 → 2200 Hz', 'τ 0,15 s'),
    ('sweepdown', 'SweepDown', 'Textura', 'ruido de paso bajo (el de Aterrizaje)', '1400 → 300 Hz', 'τ 0,14 s'),
    ('hum', 'Hum', 'Textura', '2 marimbas graves', '196 y 294 Hz', 'τ 0,5 s'),
    ('clack', 'Clack', 'Textura', 'golpe grave y bloque de madera (el de Aterrizaje)', '110 → 55 Hz y 520 Hz', 'τ 0,09 s'),
]


def paleta():
    out = ['| Evento | Familia | Instrumento | Altura | Envolvente | Pico (dBFS) | LUFS | Inicio (ms) | Dura (ms) | Memoria (KB) |', '|---|---|---|---|---|---|---|---|---|---|']
    g = db(GAIN_WOOD)
    for clave, nombre, familia, instr, altura, env in PALETA:
        c = CLIPS['wood.' + clave]
        out.append('| %s | %s | %s | %s | %s | %s | %s | %s | %.0f | %.0f |' % (nombre, familia, instr, altura, env, f1(c['peakDb'] + g), f1(c['lufs'] + g), f1(c['onsetMs']), c['durMs'], c['bytes44'] / 1024))
    return '\n'.join(out)


def afinacion():
    out = ['| Familia | Notas | Claridad de altura: hoy | Kalimba | Marimba | Caída 20 dB: hoy (ms) | Kalimba | Marimba |', '|---|---|---|---|---|---|---|---|']
    for f in CAT['families']:
        n = f['notes']
        avg = lambda k: statistics.mean(x[k] for x in n) if k in n[0] else None
        r0 = lambda v: '—' if v is None else '%.0f' % v
        if f['cues']:
            out.append('| %s | %d | %s | — | %s | %s | — | %s |' % (f['title'], len(n), f2(avg('hoyF0')), f2(avg('marF0')), r0(avg('hoyT20')), r0(avg('marT20'))))
        else:
            out.append('| %s | %d | %s | %s | %s | %s | %s | %s |' % (f['title'], len(n), f2(avg('hoyF0')), f2(avg('kalF0')), f2(avg('marF0')), r0(avg('hoyT20')), r0(avg('kalT20')), r0(avg('marT20'))))
    return '\n'.join(out)


def latencia():
    hoy = []
    madera = []
    for e in CAT['events']:
        if e['hoy'] and e['hoy'] in CLIPS and not e['hoy'].startswith('fam.'):
            hoy.append((e['group'] + '.' + e['id'], CLIPS[e['hoy']]['onsetMs'], CLIPS[e['hoy']]['attackMs']))
    for clave, nombre, *_ in PALETA:
        c = CLIPS['wood.' + clave]
        madera.append((nombre, c['onsetMs'], c['attackMs']))
    out = ['Inicio = ms hasta que el sonido llega al 10 % de su pico (lo que tarda en oírse).', '',
           '| | Sonidos | Inicio: mediana | Inicio: máximo | Con inicio de 3 ms o menos |', '|---|---|---|---|---|']
    for nombre, lista in (('Hoy', hoy), ('Madera (paleta)', madera)):
        ini = [x[1] for x in lista]
        out.append('| %s | %d | %s | %s | %d de %d |' % (nombre, len(lista), f1(statistics.median(ini)), f1(max(ini)), sum(1 for v in ini if v <= 3.0), len(ini)))
    lentos = sorted(hoy, key=lambda x: -x[1])[:6]
    out += ['', 'Los de hoy con el inicio más lento: ' + ', '.join('%s (%s ms)' % (n, f1(i)) for n, i, a in lentos) + '.']
    return '\n'.join(out)


def memoria():
    out = []
    pal = [(k, v) for k, v in CLIPS.items() if k.startswith('wood.') and not k.startswith('wood.ladder')]
    total = sum(v['bytes44'] for k, v in pal)
    stored = sum(v['bytesStored'] for k, v in pal)
    out.append('- Paleta completa (%d clips: todas las variantes), como se calcula (estéreo, 44,1 kHz, 32 bits, ya recortada a lo audible): **%.1f MB**.' % (len(pal), total / 1048576.0))
    out.append('- Guardada como la guarda el juego (22,05 kHz; los micro en mono): **%.1f MB**.' % (stored / 1048576.0))
    # lo que carga un juego típico: lo común de la Fase B (los 5 de GameFeel + cuenta, ya, pausa, aviso, tap) y las variantes que sube la racha
    core = ['correct1', 'correct2', 'correct3', 'correct4', 'correct5', 'correct6', 'correct7', 'correct8', 'wrong', 'levelup', 'tick', 'finish', 'count1', 'count2', 'count3', 'go', 'pausein', 'pauseout', 'notice', 'tap', 'knock', 'new', 'perfect', 'hint', 'unselect']
    nucleo = sum(CLIPS['wood.' + k]['bytesStored'] for k in core)
    out.append('- El núcleo común que carga TODO juego en la Fase B (%d clips: acierto con sus 8 escalones, error, nivel, tic, fin, cuenta, ya, pausa, aviso, tap, no se puede, nuevo, perfecto, ayuda, quitar): **%.1f MB**.' % (len(core), nucleo / 1048576.0))
    return '\n'.join(out)


def parejo():
    """El desorden de hoy: para cada tipo de evento, el rango de sonoridad entre juegos."""
    grupos = {}
    for e in CAT['events']:
        if not e['hoy'] or e['hoy'] not in CLIPS or e['hoy'].startswith('fam.') or e['alreadyWood']:
            continue
        clave = e['map'].split(':')[0].split(' ')[0].split('(')[0].strip()
        pico, lufs = lufs_hoy(e)
        grupos.setdefault(clave, []).append(lufs)
    out = ['| Evento (paleta) | Sonidos de hoy | LUFS más bajo | LUFS más alto | Diferencia (LU) |', '|---|---|---|---|---|']
    for k, v in sorted(grupos.items()):
        if len(v) < 2:
            continue
        out.append('| %s | %d | %s | %s | %s |' % (k, len(v), f1(min(v)), f1(max(v)), f1(max(v) - min(v))))
    return '\n'.join(out)


if __name__ == '__main__':
    qué = sys.argv[1] if len(sys.argv) > 1 else 'todo'
    for nombre, fn in (('inventario', inventario), ('paleta', paleta), ('afinacion', afinacion), ('latencia', latencia), ('memoria', memoria), ('parejo', parejo)):
        if qué in ('todo', nombre):
            print('## ' + nombre + '\n')
            print(fn())
            print()
