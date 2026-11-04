# -*- coding: utf-8 -*-
"""Capturas REALES de las pestañas en el EMULADOR (`medium_phone`, API 36) a 393 y 360 dp de ancho con letra 1,0 · 1,3 · 2,0 (Tarea 225, 4-nov-2026): la cabecera de cada pestaña y la barra de abajo, antes y después.

NUNCA toca un teléfono real: el aparato se fija con SERIAL y el programa se niega si no es un emulador (`emulator-XXXX`). Con la app instalada en el emulador, desde la carpeta del repo:
  python tools/capturar_cabeceras.py antes          # deja unity/test-results/t225/<etiqueta>_<ancho>_<letra>_<pestaña>.png
  python tools/capturar_cabeceras.py despues
Cambia el tamaño de la pantalla (`wm size`) y la letra del sistema (`font_scale`) y, al terminar, los deja como estaban (`wm size reset` y letra 1,0). Las pestañas se tocan por las coordenadas de la barra, que se leen de la pantalla (los rótulos «Hoy», «Juegos», «Contrarreloj», «Avance» y «Tú»).
"""
import os
import re
import subprocess
import sys
import time
import xml.etree.ElementTree as ET

SERIAL = os.environ.get('NUBI_EMULADOR', 'emulator-5554')
ADB = os.environ.get('ADB', os.path.expandvars('%LOCALAPPDATA%/Android/Sdk/platform-tools/adb.exe') if os.name == 'nt' else 'adb')
PKG = 'com.nubibraingames.app'
OUT = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), 'unity', 'test-results', 't225')
DENSITY = 2.625                                                         # 420 dpi
CONFIGS = [(393, 2292, '1.0'), (393, 2292, '1.3'), (393, 2292, '2.0'), (360, 2100, '1.0'), (360, 2100, '1.3'), (360, 2100, '2.0')]
TABS = ['Hoy', 'Juegos', 'Contrarreloj', 'Avance', 'Tú']

if not SERIAL.startswith('emulator-'):
    sys.exit('Este programa solo maneja emuladores (emulator-XXXX), nunca un teléfono real: %s' % SERIAL)


def adb(*args, binary=False):
    r = subprocess.run([ADB, '-s', SERIAL] + list(args), capture_output=True)
    return r.stdout if binary else r.stdout.decode('utf-8', 'replace')


def nav_centers():
    adb('shell', 'uiautomator', 'dump', '/sdcard/ui.xml')
    xml = adb('exec-out', 'cat', '/sdcard/ui.xml')
    root = ET.fromstring(xml[xml.index('<'):])
    height = int(re.findall(r'\d+', adb('shell', 'wm', 'size'))[-1])
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
    return found


def ready(retries=10):
    """Cierra el aviso «System UI isn't responding» (pasa con un emulador recién arrancado) y espera a que la barra de abajo se vea: devuelve las coordenadas de sus pestañas."""
    for _ in range(retries):
        adb('shell', 'uiautomator', 'dump', '/sdcard/ui.xml')
        xml = adb('exec-out', 'cat', '/sdcard/ui.xml')
        try: root = ET.fromstring(xml[xml.index('<'):])
        except ValueError: time.sleep(2); continue
        wait = [n for n in root.iter('node') if n.get('text', '') == 'Wait']
        if wait:
            b = re.findall(r'\d+', wait[0].get('bounds', ''))
            adb('shell', 'input', 'tap', str((int(b[0]) + int(b[2])) // 2), str((int(b[1]) + int(b[3])) // 2)); time.sleep(1.5); continue
        c = nav_centers()
        if 'Hoy' in c and 'Avance' in c: return c
        time.sleep(3)
    return nav_centers()


def tu_button():
    """El centro del botón «Tú» (su descripción es «Tú: tu perfil y ajustes»), o None si no está (la app de antes de la Tarea 225)."""
    adb('shell', 'uiautomator', 'dump', '/sdcard/ui.xml')
    xml = adb('exec-out', 'cat', '/sdcard/ui.xml')
    try: root = ET.fromstring(xml[xml.index('<'):])
    except ValueError: return None
    for n in root.iter('node'):
        if n.get('content-desc', '').startswith('Tú: tu perfil'):
            b = re.findall(r'\d+', n.get('bounds', ''))
            return (int(b[0]) + int(b[2])) // 2, (int(b[1]) + int(b[3])) // 2
    return None


def shot(path):
    png = adb('exec-out', 'screencap', '-p', binary=True)
    with open(path, 'wb') as f: f.write(png)


def main():
    label = sys.argv[1] if len(sys.argv) > 1 else 'medida'
    os.makedirs(OUT, exist_ok=True)
    try:
        for w, h, scale in CONFIGS:
            adb('shell', 'wm', 'size', '%dx%d' % (round(w * DENSITY), h))
            adb('shell', 'settings', 'put', 'system', 'font_scale', scale)
            adb('shell', 'am', 'force-stop', PKG); time.sleep(1.0)
            adb('shell', 'monkey', '-p', PKG, '-c', 'android.intent.category.LAUNCHER', '1'); time.sleep(8.0)
            centers = ready()
            print(w, scale, 'barra:', centers)
            for t in TABS:
                if t not in centers: continue
                adb('shell', 'input', 'tap', str(centers[t][0]), str(centers[t][1])); time.sleep(2.2)
                shot(os.path.join(OUT, '%s_%d_%s_%s.png' % (label, w, scale.replace('.', ''), t)))
            # el botón «Tú» de arriba a la derecha (desde la Tarea 225): se toca y se captura Tú abierto; Atrás vuelve
            tu = tu_button()
            if tu:
                adb('shell', 'input', 'tap', str(tu[0]), str(tu[1])); time.sleep(2.5)
                shot(os.path.join(OUT, '%s_%d_%s_TuAbierto.png' % (label, w, scale.replace('.', ''))))
                adb('shell', 'input', 'keyevent', '4'); time.sleep(2.0)
    finally:
        adb('shell', 'wm', 'size', 'reset')
        adb('shell', 'settings', 'put', 'system', 'font_scale', '1.0')
    print('Listo en', OUT)


main()
