"""Hojas de contacto de las CAPTURAS REALES de los juegos.

Las capturas las saca Unity (NO es una composición aparte): `bash tools/verificar-todo.sh --capturas todos` (o `--capturas <Juego>`) corre cada juego en forma de teléfono (1080x2400) y guarda un PNG por toma en
unity/test-results/capturas/<juego>/NN-<toma>.png (fuera de git); este script junta las tomas de cada juego en UNA lámina con su rótulo debajo:

  - un juego suelto  -> docs/previews/capturas/<juego>.png   (Correo, solo: docs/previews/correo-estacion.png, la lámina de siempre)
  - `todos`          -> una lámina por juego en docs/previews/capturas/<juego>.png y el índice docs/previews/capturas/README.md (fecha, commit, tiempos y avisos de cada juego)

Uso:  python tools/capturas/hoja.py <juego | juego1,juego2 | todos> [--in <carpeta de capturas>] [--out <archivo.png>] [--completa]
`--completa` guarda la lámina con todos los colores (por defecto se reducen a 256 para que pesen menos en git).
"""
import argparse
import datetime
import os
import subprocess
import sys

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
FB = ROOT + '/unity/NeuroVidaCore/Assets/Resources/Fonts/Fredoka-Bold.ttf'
FS = ROOT + '/unity/NeuroVidaCore/Assets/Resources/Fonts/Fredoka-SemiBold.ttf'
BG = (13, 16, 40)
TEXT = (237, 234, 251)
LAV = (171, 165, 210)
SHOTS = os.path.join(ROOT, 'unity', 'test-results', 'capturas')
OUT_DIR = os.path.join(ROOT, 'docs', 'previews', 'capturas')

# id -> nombre que se ve
NAMES = {
    'parejas': 'Constelaciones', 'secuencia': 'Rastro de luz', 'bodega': 'Bodega de carga', 'rumbo': 'Rumbo a Casa', 'correo': 'La estación de correo',
    'stroop': 'Tinta o Palabra', 'piloto': 'Piloto Estelar', 'freno': 'Freno de Emergencia', 'satelites': 'Satélites', 'radar': 'Rescate relámpago',
    'acoplamiento': 'Acoplamiento', 'calculo': 'Carga exacta', 'aterrizaje': 'Aterrizaje Lunar', 'engranajes': 'Engranajes',
    'anagramas': 'En la punta de la lengua', 'meteoros': 'Lluvia de meteoros', 'disparate': '¿Verdad o disparate?', 'cosecha': 'Cosecha de palabras', 'intrusa': 'La estrella intrusa',
}
ORDER = list(NAMES)

# toma -> rótulo (las de cada juego se suman a estas)
CAPTIONS = {
    'primera': 'Primera pantalla jugable',
    't08': 'A los 8 s de juego',
    't20': 'A los 20 s de juego',
    't40': 'A los 40 s de juego',
    'pausa': 'La pausa',
    'aviso': 'Un aviso (de prueba)',
    'listo': 'Cortina «¡Listo!»',
    'final': 'La pantalla final',
    'sin-animaciones': 'Con «quitar animaciones»',
    'tutorial-rescatar': 'Tutorial: rescatar (marcadas y botón encendido)',
    'viaje-salida': 'El viaje: a mitad de la salida',
    'viaje-vuelta': 'El viaje: a mitad de la vuelta',
    'atento-16x9': 'En 16:9: atento',
    'destello-16x9': 'En 16:9: el destello',
    'respuesta-16x9': 'En 16:9: la respuesta',
    'revelacion-16x9': 'En 16:9: la revelación',
    'tutorial-1': 'Tutorial: paso 1',
    'tutorial-3': 'Tutorial: paso 3',
    'tutorial-nueva-mision': 'Tutorial: la misión cambió',
    'tutorial-toca-nueva-mision': 'Tutorial: toca la misión nueva',
}
GAME_CAPTIONS = {
    'acoplamiento': {
        'llegada': 'Llega un módulo',
        'respuesta': 'Hay que decidir (Encaja / Espejo)',
        'vuelta-del-espejo': 'Era un espejo: se da vuelta',
        'acople': 'Encaja en el puerto',
        'vuelo-a-la-estacion': 'Vuela a su casillero',
        'anillo-completo': '¡Anillo completo!',
        'error': 'Un error: el módulo se aleja',
        'sin-tiempo': 'Sin tiempo: se ve la verdad',
        'respuesta-16x9': 'En 16:9: hay que decidir',
        'acople-16x9': 'En 16:9: encaja',
        'vuelo-16x9': 'En 16:9: vuela a la estación',
        'final-16x9': 'En 16:9: la pantalla final',
        'tutorial-se-suma': 'Tutorial: se suma a la estación',
        'tutorial-espejo': 'Tutorial: «Espejo» (toque real)',
    },
    'aterrizaje': {
        'bajada': 'La nave baja con su guía',
        'acierto': 'Aterrizaje justo: bandera y marca',
        'lejos': 'Lejos: tramo y aspa',
        'diana': '¡Diana lunar!',
        'suma': 'Suma: la bandera dice el total',
        'cupula': '¡Nueva cúpula en tu base!',
        'sin-animaciones': 'Con «quitar animaciones» (sin cordilleras)',
        'final': 'El cierre «¡Listo!»',
        'bajada-16x9': 'En 16:9: la nave baja',
        'acierto-16x9': 'En 16:9: aterrizaje justo',
        'cupula-16x9': 'En 16:9: nueva cúpula',
        'final-16x9': 'En 16:9: el cierre',
        'tutorial-suelta': 'Tutorial: suelta para aterrizar',
    },
    'correo': {
        'nuevo-estacion': 'Tarjeta «NUEVO»: la estación',
        'nuevo-lazo': 'Tarjeta «NUEVO»: el lazo',
        'hoja': 'La hoja de encargos de la mañana',
        'cinta-dorado': 'La cinta: sello dorado delante',
        'cinta-sin-animaciones': 'La cinta con «quitar animaciones»',
        'cinta-lazo': 'La cinta: carta con lazo',
        'faro': 'El faro: la nave del correo llega',
        'resumen': 'El resumen del día',
        'saco': '«¡Llega un saco!»',
        'radio': 'La radio cancela un encargo',
    },
}
OUT_DEFAULT = {'correo': 'docs/previews/correo-estacion.png'}      # solo cuando se pide Correo suelto (la lámina de siempre)
COLS = 5
QUANT = True


def font(path, size):
    return ImageFont.truetype(path, size)


def caption(game, key):
    return GAME_CAPTIONS.get(game, {}).get(key) or CAPTIONS.get(key, key)


def sheet_for(game, src, out, title_extra=''):
    files = sorted(f for f in os.listdir(src) if f.lower().endswith('.png'))
    if not files:
        return None
    w, h = 270, 600                                   # cada captura, de 1080x2400 a 270x600 (un cuarto)
    pad, cap_h, top = 18, 54, 70
    rows = (len(files) + COLS - 1) // COLS
    W = pad + COLS * (w + pad)
    H = top + rows * (h + cap_h + pad)
    sheet = Image.new('RGB', (W, H), BG)
    d = ImageDraw.Draw(sheet)
    d.text((pad, 14), '%s — capturas reales de Unity (lienzo de teléfono 1080×2400)' % NAMES.get(game, game), font=font(FB, 24), fill=TEXT)
    d.text((pad, 46), 'Sale de `bash tools/verificar-todo.sh --capturas %s`: el juego de verdad, no una composición.%s' % (game, title_extra), font=font(FS, 14), fill=LAV)
    for i, f in enumerate(files):
        im = Image.open(os.path.join(src, f)).convert('RGB')
        if abs(im.width / im.height - 1080 / 2400) > 0.01:             # otra proporción (16:9, 1080×1920): entra a todo lo ancho de la celda, centrada de alto
            ih = int(round(w * im.height / im.width))
            im = im.resize((w, ih), Image.LANCZOS)
            cell = Image.new('RGB', (w, h), BG)
            cell.paste(im, (0, (h - ih) // 2))
            im = cell
        else:
            im = im.resize((w, h), Image.LANCZOS)
        r, c = divmod(i, COLS)
        x = pad + c * (w + pad)
        y = top + r * (h + cap_h + pad)
        sheet.paste(im, (x, y))
        d.rectangle([x - 1, y - 1, x + w, y + h], outline=(60, 64, 110), width=1)
        key = f[3:-4] if len(f) > 7 else f
        d.text((x + w / 2, y + h + 10), caption(game, key), font=font(FB, 15), fill=TEXT, anchor='mt')
    os.makedirs(os.path.dirname(out), exist_ok=True)
    if QUANT:
        sheet = sheet.quantize(colors=256, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE)
    sheet.save(out, optimize=True)
    return len(files), sheet.size, os.path.getsize(out)


def read_lines(path):
    try:
        with open(path, encoding='utf-8') as f:
            return [l.rstrip('\n') for l in f if l.strip()]
    except OSError:
        return []


def commit():
    try:
        sha = subprocess.check_output(['git', 'rev-parse', '--short', 'HEAD'], cwd=ROOT, text=True).strip()
        dirty = subprocess.check_output(['git', 'status', '--porcelain', '--untracked-files=no'], cwd=ROOT, text=True).strip()
        return sha + (' (con cambios sin comitear)' if dirty else '')
    except Exception:
        return '(sin git)'


def write_index(done, src_root):
    resumen = read_lines(os.path.join(src_root, '_resumen.txt'))
    times = {}
    total = ''
    for l in resumen:
        p = l.split('\t')
        if p[0] == 'total':
            total = ' · '.join(p[1:])
        elif len(p) >= 2:
            times[p[0]] = p[1]
    out = [
        '# Capturas reales de los juegos',
        '',
        'Fecha: %s · commit: %s' % (datetime.date.today().isoformat(), commit()),
        '',
        'Cada lámina muestra UN juego corriendo de verdad en Unity con el lienzo en forma de teléfono (1080×2400): la primera pantalla jugable, a los 8, 20 y 40 s de juego, la pausa, la cortina «¡Listo!» y la pantalla final si la partida llega a su fin, '
        'una toma con «quitar animaciones» y, en los juegos con tutorial, los pasos 1 y 3. Sirven para ver a golpe de ojo errores visuales: textos fuera de su lugar, cosas tapadas, fondos que no cubren, elementos detrás de otros. '
        'No son una prueba de que el juego esté bien: donde no hay piloto automático (solo Engranajes, Bodega, Constelaciones y Correo se juegan solos) la partida se queda en su primera situación. Las tomas sueltas, a tamaño completo, quedan en `unity/test-results/capturas/<juego>/` (fuera de git).',
        '',
        'Se regeneran con `bash tools/verificar-todo.sh --capturas todos` (necesita tarjeta de video; no corre en el CI).',
    ]
    if total:
        out += ['', 'Corrida completa: %s.' % total]
    out += ['', '| Juego | Lámina | Tomas | Tiempo | Errores de consola y avisos del guion |', '|---|---|---|---|---|']
    for g, n, _size in done:
        errs = read_lines(os.path.join(src_root, g, 'errores.txt'))
        errs = [e for e in errs if not e.startswith('(sin errores')]
        nota = '; '.join(e.replace('|', '/')[:140] for e in errs[:4]) + (' …' if len(errs) > 4 else '') if errs else 'sin errores de consola'
        out.append('| %s (`%s`) | [%s.png](%s.png) | %d | %s | %s |' % (NAMES.get(g, g), g, g, g, n, times.get(g, ''), nota))
    out.append('')
    if os.path.exists(os.path.join(OUT_DIR, 'hallazgos.md')):
        out += ['Lo que se ve mal a primera vista, juego por juego (revisión a mano de estas láminas): [hallazgos.md](hallazgos.md).', '']
    with open(os.path.join(OUT_DIR, 'README.md'), 'w', encoding='utf-8', newline='\n') as f:
        f.write('\n'.join(out))


def main():
    global QUANT
    ap = argparse.ArgumentParser()
    ap.add_argument('juego')
    ap.add_argument('--in', dest='src')
    ap.add_argument('--out')
    ap.add_argument('--completa', action='store_true')
    a = ap.parse_args()
    if a.completa:
        QUANT = False
    src_root = a.src or SHOTS
    if a.juego.lower() == 'todos':
        games = [g for g in ORDER if os.path.isdir(os.path.join(src_root, g))]
    else:
        names = {v.lower(): k for k, v in NAMES.items()}
        games = []
        for p in a.juego.replace(';', ',').split(','):
            p = p.strip().lower()
            if not p:
                continue
            g = p if p in NAMES else ('secuencia' if p == 'rastro' else p)
            if g not in NAMES:
                sys.exit('No conozco el juego «%s»' % p)
            games.append(g)
    if not games:
        sys.exit('No hay capturas en ' + src_root)
    done = []
    for g in games:
        src = os.path.join(src_root, g)
        if not os.path.isdir(src):
            sys.exit('No hay capturas de «%s» en %s' % (g, src))
        single_correo = (g == 'correo' and a.juego.lower() != 'todos' and len(games) == 1)
        out = a.out if (a.out and len(games) == 1) else os.path.join(ROOT, OUT_DEFAULT['correo']) if single_correo else os.path.join(OUT_DIR, g + '.png')
        res = sheet_for(g, src, out)
        if res is None:
            sys.exit('No hay capturas en ' + src)
        done.append((g, res[0], res[2]))
        print('  %s: %d tomas -> %s (%d KB)' % (g, res[0], os.path.relpath(out, ROOT), res[2] // 1024))
    if a.juego.lower() == 'todos':
        write_index(done, src_root)
        print('OK %d láminas + %s' % (len(done), os.path.relpath(os.path.join(OUT_DIR, 'README.md'), ROOT)))
    else:
        print('OK', ', '.join(g for g, _n, _s in done))


if __name__ == '__main__':
    main()
