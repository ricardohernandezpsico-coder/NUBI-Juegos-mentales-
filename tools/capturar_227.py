# -*- coding: utf-8 -*-
"""Capturas REALES en el EMULADOR para la Tarea 227 (4-nov-2026): Hoy y Avance con una persona que esta semana solo jugó Contrarreloj. Antes del arreglo, Hoy decía «2 de 4 días de tu meta» y Avance «0 de 4» con las siete llamitas apagadas.

NUNCA toca un teléfono real: el aparato se fija con SERIAL y el programa se niega si no es un emulador (`emulator-XXXX`). Con la app (de depuración) instalada en el emulador, desde la carpeta del repo:
  python tools/capturar_227.py preparar        # guarda una copia de la base del emulador y borra de ELLA las partidas de siempre (quedan las del Contrarreloj)
  python tools/capturar_227.py antes           # Hoy y Avance → unity/test-results/t227/<etiqueta>_<Hoy|Avance>.png (393 dp, letra 1,0)
  python tools/capturar_227.py despues
  python tools/capturar_227.py restaurar       # devuelve la base que había
Las bases pasan por `run-as` (la app es de depuración); la copia queda en unity/test-results/t227/base-original/ (fuera de git).
"""
import os
import re
import shutil
import sqlite3
import subprocess
import sys
import tempfile
import time
import xml.etree.ElementTree as ET

SERIAL = os.environ.get('NUBI_EMULADOR', 'emulator-5554')
ADB = os.environ.get('ADB', os.path.expandvars('%LOCALAPPDATA%/Android/Sdk/platform-tools/adb.exe') if os.name == 'nt' else 'adb')
PKG = 'com.nubibraingames.app'
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, 'unity', 'test-results', 't227')
BACKUP = os.path.join(OUT, 'base-original')
DB = 'neurovida_database'
DENSITY = 2.625

if not SERIAL.startswith('emulator-'):
    sys.exit('Este programa solo maneja emuladores (emulator-XXXX), nunca un teléfono real: %s' % SERIAL)


def adb(*args, binary=False):
    r = subprocess.run([ADB, '-s', SERIAL] + list(args), capture_output=True)
    return r.stdout if binary else r.stdout.decode('utf-8', 'replace')


def pull_db(dest):
    os.makedirs(dest, exist_ok=True)
    for suffix in ('', '-wal', '-shm'):
        data = adb('exec-out', 'run-as', PKG, 'cat', 'databases/' + DB + suffix, binary=True)
        path = os.path.join(dest, DB + suffix)
        if data and not data.startswith(b'run-as:') and b'No such file' not in data[:200]:
            with open(path, 'wb') as f: f.write(data)
        elif os.path.exists(path): os.remove(path)


def push_db(src):
    adb('shell', 'am', 'force-stop', PKG)
    for suffix in ('-wal', '-shm', '-journal'):
        adb('shell', 'run-as', PKG, 'rm', '-f', 'databases/' + DB + suffix)
    adb('push', os.path.join(src, DB), '/data/local/tmp/' + DB)
    adb('shell', 'run-as', PKG, 'cp', '/data/local/tmp/' + DB, 'databases/' + DB)
    for suffix in ('-wal', '-shm'):
        p = os.path.join(src, DB + suffix)
        if os.path.exists(p):
            adb('push', p, '/data/local/tmp/' + DB + suffix)
            adb('shell', 'run-as', PKG, 'cp', '/data/local/tmp/' + DB + suffix, 'databases/' + DB + suffix)


def preparar():
    adb('shell', 'am', 'force-stop', PKG); time.sleep(1.0)
    pull_db(BACKUP)
    work = tempfile.mkdtemp(prefix='nubi227_')
    for suffix in ('', '-wal', '-shm'):
        p = os.path.join(BACKUP, DB + suffix)
        if os.path.exists(p): shutil.copy(p, os.path.join(work, DB + suffix))
    con = sqlite3.connect(os.path.join(work, DB))
    n_games = con.execute('SELECT COUNT(*) FROM game_results').fetchone()[0]
    n_trials = con.execute('SELECT COUNT(*) FROM time_trial_results').fetchone()[0]
    print('partidas de siempre:', n_games, '· Contrarreloj:', n_trials)
    con.execute('DELETE FROM game_results')
    con.commit()
    con.execute('PRAGMA wal_checkpoint(TRUNCATE)')
    con.close()
    for suffix in ('-wal', '-shm'):
        p = os.path.join(work, DB + suffix)
        if os.path.exists(p): os.remove(p)
    push_db(work)
    print('base preparada (sin partidas de siempre); copia de la original en', BACKUP)


def restaurar():
    if not os.path.exists(os.path.join(BACKUP, DB)): sys.exit('No hay copia en ' + BACKUP)
    push_db(BACKUP)
    print('base original restaurada')


def dump():
    adb('shell', 'uiautomator', 'dump', '/sdcard/ui.xml')
    xml = adb('exec-out', 'cat', '/sdcard/ui.xml')
    try: return ET.fromstring(xml[xml.index('<'):])
    except ValueError: return None


def center_of(root, part):
    for n in root.iter('node'):
        label = n.get('text', '') or n.get('content-desc', '')
        b = re.findall(r'\d+', n.get('bounds', ''))
        if len(b) == 4 and part in label:
            x0, y0, x1, y1 = map(int, b); return (x0 + x1) // 2, (y0 + y1) // 2
    return None


def shot(path):
    with open(path, 'wb') as f: f.write(adb('exec-out', 'screencap', '-p', binary=True))


def capturar(label):
    os.makedirs(OUT, exist_ok=True)
    try:
        adb('shell', 'wm', 'size', '%dx%d' % (round(393 * DENSITY), 2292))
        adb('shell', 'settings', 'put', 'system', 'font_scale', '1.0')
        adb('shell', 'am', 'force-stop', PKG); time.sleep(1.0)
        adb('shell', 'monkey', '-p', PKG, '-c', 'android.intent.category.LAUNCHER', '1'); time.sleep(10.0)
        for _ in range(3):                                           # «System UI isn't responding» y la guía de Nubi, si salen
            root = dump()
            if root is None: time.sleep(2); continue
            w = center_of(root, 'Wait') or center_of(root, 'Saltar la guía')
            if w: adb('shell', 'input', 'tap', str(w[0]), str(w[1])); time.sleep(2.0)
            else: break
        root = dump()
        hoy = center_of(root, 'Hoy'); avance = center_of(root, 'Avance')
        # (los rótulos de la barra están abajo; «Hoy» también aparece arriba en el saludo, por eso se buscan por la fila de abajo)
        bar = [(n, tuple(map(int, re.findall(r'\d+', n.get('bounds'))))) for n in root.iter('node') if n.get('bounds') and re.findall(r'\d+', n.get('bounds'))]
        def bar_center(name):
            for n, (x0, y0, x1, y1) in bar:
                lab = n.get('text', '') or n.get('content-desc', '')
                if y0 > 2292 * 0.8 and (lab == name or lab.startswith(name + ',') or lab.startswith(name + ' ')): return (x0 + x1) // 2, (y0 + y1) // 2
            return None
        h = bar_center('Hoy'); a = bar_center('Avance')
        if h: adb('shell', 'input', 'tap', str(h[0]), str(h[1])); time.sleep(2.0)
        shot(os.path.join(OUT, '%s_Hoy.png' % label))
        if a: adb('shell', 'input', 'tap', str(a[0]), str(a[1])); time.sleep(2.5)
        shot(os.path.join(OUT, '%s_Avance.png' % label))
    finally:
        adb('shell', 'wm', 'size', 'reset')
        adb('shell', 'settings', 'put', 'system', 'font_scale', '1.0')
    print('capturas en', OUT)


def main():
    cmd = sys.argv[1] if len(sys.argv) > 1 else ''
    if cmd == 'preparar': preparar()
    elif cmd == 'restaurar': restaurar()
    elif cmd in ('antes', 'despues'): capturar(cmd)
    else: sys.exit(__doc__)


main()
