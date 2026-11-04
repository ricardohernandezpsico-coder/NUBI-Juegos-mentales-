# -*- coding: utf-8 -*-
"""Mide los cuadros lentos al cambiar de pestaña en el EMULADOR (`medium_phone`, API 36) con `dumpsys gfxinfo` (Tarea 224, 4-nov-2026).

NUNCA toca un teléfono real: el aparato se fija con SERIAL y el programa se niega si no es un emulador (`emulator-XXXX`). Con la app abierta en Hoy, desde la carpeta del repo:
  python tools/medir_cambio_de_pestana.py antes            # 20 cambios (cada par, en los dos sentidos) x 3 veces, ya con todas las pestañas visitadas; y 4 «primeras visitas» desde Hoy
  python tools/medir_cambio_de_pestana.py despues 5        # lo mismo con 5 repeticiones
Deja `unity/test-results/cambio-de-pestana-<etiqueta>.json` y imprime una tabla: por cambio, el peor cuadro del HILO PRINCIPAL (ms) y cuántos pasan de 16,7 y de 32 ms; entre paréntesis, el cuadro entero (con el dibujo).

Cómo se mide: se toca la pestaña de origen (un segundo de calma), se pone en cero `dumpsys gfxinfo <paquete> reset`, se toca la de destino, se esperan 1,2 s y se leen los cuadros (`framestats`, solo los cuadros con banderas en cero). El número que importa es el
del HILO PRINCIPAL (`FrameStartTime` → `SyncQueued`: lo que tardan la recomposición, la medida y el registro del dibujo, es decir, lo que hace que algo «se pegue»). El cuadro entero (`IntendedVsync` → `FrameCompleted`) NO sirve en el emulador: dibuja con swiftshader (por
software) y cada cuadro tarda 150 ms o más aunque no haya nada que hacer; se imprime solo como referencia. Los milisegundos valen para COMPARAR antes y después, no como los de un teléfono. Las pestañas se tocan por coordenadas de la barra (se leen una vez de la pantalla).
"""
import json
import os
import re
import subprocess
import sys
import time
import xml.etree.ElementTree as ET

SERIAL = os.environ.get('NUBI_EMULADOR', 'emulator-5554')
ADB = os.environ.get('ADB', os.path.expandvars('%LOCALAPPDATA%/Android/Sdk/platform-tools/adb.exe') if os.name == 'nt' else 'adb')
PKG = 'com.nubibraingames.app'
OUT = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), 'unity', 'test-results')
TABS = ['Hoy', 'Juegos', 'Contrarreloj', 'Avance', 'Tú']

if not SERIAL.startswith('emulator-'):
    sys.exit('Este programa solo maneja emuladores (emulator-XXXX), nunca un teléfono real: %s' % SERIAL)


def adb(*args, binary=False):
    r = subprocess.run([ADB, '-s', SERIAL] + list(args), capture_output=True)
    return r.stdout if binary else r.stdout.decode('utf-8', 'replace')


def nav_centers():
    """El centro (px) de cada pestaña de la barra de abajo: los rótulos exactos en el cuarto de abajo de la pantalla."""
    adb('shell', 'uiautomator', 'dump', '/sdcard/ui.xml')
    xml = adb('exec-out', 'cat', '/sdcard/ui.xml')
    root = ET.fromstring(xml[xml.index('<'):])
    size = re.findall(r'\d+', adb('shell', 'wm', 'size'))
    height = int(size[1])
    found = {}
    for n in root.iter('node'):
        label = n.get('text', '') or n.get('content-desc', '')
        b = re.findall(r'\d+', n.get('bounds', ''))
        if len(b) != 4: continue
        x0, y0, x1, y1 = map(int, b)
        if y0 > height * 0.80:
            for t in TABS:
                if label == t or label.startswith(t + ',') or label.startswith(t + ' '):
                    found.setdefault(t, ((x0 + x1) // 2, (y0 + y1) // 2))
    missing = [t for t in TABS if t not in found]
    if missing: sys.exit('No encuentro en la barra: %s (¿la app está abierta en una pestaña, sin ventanas encima?). Vi: %s' % (missing, found))
    return found


def tap(xy): adb('shell', 'input', 'tap', str(xy[0]), str(xy[1]))


def frames():
    """Los cuadros que dibujó la app desde el último `reset`: duración en ms de cada uno (IntendedVsync → FrameCompleted)."""
    out = adb('shell', 'dumpsys', 'gfxinfo', PKG, 'framestats')
    rows, cols = [], None
    on = False
    for line in out.splitlines():
        line = line.strip()
        if line == '---PROFILEDATA---':
            on = not on; cols = None; continue
        if not on or not line: continue
        parts = line.rstrip(',').split(',')
        if cols is None:
            cols = parts; continue
        try: v = list(map(int, parts))
        except ValueError: continue
        d = dict(zip(cols, v))
        if d.get('Flags', 0) != 0: continue
        rows.append(((d['SyncQueued'] - d['FrameStartTime']) / 1e6, (d['FrameCompleted'] - d['IntendedVsync']) / 1e6))
    return rows


def stats(f):
    ui = [a for a, _ in f]; full = [b for _, b in f]
    return {'n': len(f), 'max': round(max(ui), 1) if ui else 0.0, 'over16': sum(1 for x in ui if x > 16.7), 'over32': sum(1 for x in ui if x > 32.0), 'maxFull': round(max(full), 1) if full else 0.0, 'ui': [round(x, 1) for x in ui]}


def measure(src, dst, centers, wait=1.2):
    tap(centers[src]); time.sleep(1.3)
    adb('shell', 'dumpsys', 'gfxinfo', PKG, 'reset'); time.sleep(0.2)
    tap(centers[dst]); time.sleep(wait)
    return stats(frames())


def main():
    label = sys.argv[1] if len(sys.argv) > 1 else 'medida'
    reps = int(sys.argv[2]) if len(sys.argv) > 2 else 3
    centers = nav_centers()
    print('Barra:', centers)
    result = {'label': label, 'reps': reps, 'warm': {}, 'cold': {}}
    # primera visita: la app recién abierta en Hoy y de ahí a cada pestaña
    for dst in TABS[1:]:
        adb('shell', 'am', 'force-stop', PKG); time.sleep(1.0)
        adb('shell', 'monkey', '-p', PKG, '-c', 'android.intent.category.LAUNCHER', '1'); time.sleep(9.0)
        adb('shell', 'dumpsys', 'gfxinfo', PKG, 'reset'); time.sleep(0.2)
        tap(centers[dst]); time.sleep(1.5)
        result['cold']['Hoy→' + dst] = stats(frames())
    # ya visitadas: una vuelta para visitar todas y después cada par en los dos sentidos
    for t in TABS: tap(centers[t]); time.sleep(1.5)
    for src in TABS:
        for dst in TABS:
            if src == dst: continue
            runs = [measure(src, dst, centers) for _ in range(reps)]
            result['warm']['%s→%s' % (src, dst)] = {'max': max(r['max'] for r in runs), 'over16': max(r['over16'] for r in runs), 'over32': max(r['over32'] for r in runs), 'maxFull': max(r['maxFull'] for r in runs), 'n': runs[0]['n'], 'runs': runs}
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, 'cambio-de-pestana-%s.json' % label)
    json.dump(result, open(path, 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
    print('\nPrimera visita (desde Hoy, app recién abierta):')
    for k, v in result['cold'].items(): print('  %-22s cuadros %3d · peor hilo principal %6.1f ms (cuadro entero %6.1f) · >16,7 ms: %2d · >32 ms: %2d' % (k, v['n'], v['max'], v['maxFull'], v['over16'], v['over32']))
    print('\nPestañas ya visitadas (el peor de %d repeticiones):' % reps)
    for k, v in result['warm'].items(): print('  %-22s cuadros %3d · peor hilo principal %6.1f ms (cuadro entero %6.1f) · >16,7 ms: %2d · >32 ms: %2d' % (k, v['n'], v['max'], v['maxFull'], v['over16'], v['over32']))
    worst = max(result['warm'].values(), key=lambda v: v['max'])
    print('\nEl peor cuadro del hilo principal de todos los cambios: %.1f ms; cambios con algún cuadro >32 ms: %d de %d' % (worst['max'], sum(1 for v in result['warm'].values() if v['over32']), len(result['warm'])))
    print('Cuadros >32 ms del hilo principal sumando los 20 cambios: %d (los >16,7 ms: %d)' % (sum(v['over32'] for v in result['warm'].values()), sum(v['over16'] for v in result['warm'].values())))
    print('Guardado en', path)


main()
