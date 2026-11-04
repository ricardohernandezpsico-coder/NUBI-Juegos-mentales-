# -*- coding: utf-8 -*-
"""Capturas REALES en el EMULADOR (`medium_phone`, API 36) para la Tarea 225b (4-nov-2026): el botón «Tú» dentro de la cabecera de cada pestaña, el aire entre el botón y lo que va debajo, y el paso 3 de la guía con el botón iluminado.

NUNCA toca un teléfono real: el aparato se fija con SERIAL y el programa se niega si no es un emulador (`emulator-XXXX`). Con la app instalada en el emulador, desde la carpeta del repo:
  python tools/capturar_225b.py <etiqueta> tabs      # las 4 pestañas arriba, en 393 y 360 dp con letra 1,0 · 1,3 · 2,0
  python tools/capturar_225b.py <etiqueta> hoy200    # Hoy deslizada unos 200 dp (393 y 360 dp con letra 1,0)
  python tools/capturar_225b.py <etiqueta> guia3     # el paso 3 de la guía (393 dp con 1,0 y 360 dp con 2,0)
Deja unity/test-results/t225b/<etiqueta>_<ancho>_<letra>_<pestaña>.png (y <etiqueta>_hoy200_<ancho>_<letra>.png, <etiqueta>_guia3_<ancho>_<letra>.png). Cambia el tamaño de la pantalla y la letra del sistema y, al terminar, los deja como estaban.
La guía se provoca escribiendo `profile_extra.xml` (vuelo terminado, guía sin ver) con `run-as` (la app es de depuración); eso borra lo que había en esa preferencia EN EL EMULADOR.
"""
import os
import re
import subprocess
import sys
import tempfile
import time
import xml.etree.ElementTree as ET

SERIAL = os.environ.get('NUBI_EMULADOR', 'emulator-5554')
ADB = os.environ.get('ADB', os.path.expandvars('%LOCALAPPDATA%/Android/Sdk/platform-tools/adb.exe') if os.name == 'nt' else 'adb')
PKG = 'com.nubibraingames.app'
OUT = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), 'unity', 'test-results', 't225b')
DENSITY = 2.625                                                         # 420 dpi
CONFIGS = [(393, 2292, '1.0'), (393, 2292, '1.3'), (393, 2292, '2.0'), (360, 2100, '1.0'), (360, 2100, '1.3'), (360, 2100, '2.0')]
TABS = ['Hoy', 'Juegos', 'Contrarreloj', 'Avance']
PROFILE = """<?xml version='1.0' encoding='utf-8' standalone='yes' ?>
<map>
<boolean name="first_flight_done" value="true" />
<string name="age_range">R45_54</string>
</map>
"""

if not SERIAL.startswith('emulator-'):
    sys.exit('Este programa solo maneja emuladores (emulator-XXXX), nunca un teléfono real: %s' % SERIAL)


def adb(*args, binary=False):
    r = subprocess.run([ADB, '-s', SERIAL] + list(args), capture_output=True)
    return r.stdout if binary else r.stdout.decode('utf-8', 'replace')


def dump():
    adb('shell', 'uiautomator', 'dump', '/sdcard/ui.xml')
    xml = adb('exec-out', 'cat', '/sdcard/ui.xml')
    try: return ET.fromstring(xml[xml.index('<'):])
    except ValueError: return None


def nodes(root):
    for n in root.iter('node'):
        b = re.findall(r'\d+', n.get('bounds', ''))
        if len(b) == 4: yield n, tuple(map(int, b))


def nav_centers(root=None):
    root = root if root is not None else dump()
    if root is None: return {}
    height = int(re.findall(r'\d+', adb('shell', 'wm', 'size'))[-1])
    found = {}
    for n, (x0, y0, x1, y1) in nodes(root):
        label = n.get('text', '') or n.get('content-desc', '')
        if y0 > height * 0.80:
            for t in TABS:
                if label == t or label.startswith(t + ',') or label.startswith(t + ' '):
                    found.setdefault(t, ((x0 + x1) // 2, (y0 + y1) // 2))
    return found


def ready(retries=10):
    """Cierra «System UI isn't responding» (emulador recién arrancado) y espera a que la barra de abajo se vea."""
    for _ in range(retries):
        root = dump()
        if root is None: time.sleep(2); continue
        wait = [b for n, b in nodes(root) if n.get('text', '') == 'Wait']
        if wait:
            b = wait[0]
            adb('shell', 'input', 'tap', str((b[0] + b[2]) // 2), str((b[1] + b[3]) // 2)); time.sleep(1.5); continue
        c = nav_centers(root)
        if 'Hoy' in c and 'Avance' in c: return c
        time.sleep(3)
    return nav_centers()


def shot(path):
    png = adb('exec-out', 'screencap', '-p', binary=True)
    with open(path, 'wb') as f: f.write(png)


def configure(w, h, scale):
    adb('shell', 'wm', 'size', '%dx%d' % (round(w * DENSITY), h))
    adb('shell', 'settings', 'put', 'system', 'font_scale', scale)


def launch(wait=8.0):
    adb('shell', 'am', 'force-stop', PKG); time.sleep(1.0)
    adb('shell', 'monkey', '-p', PKG, '-c', 'android.intent.category.LAUNCHER', '1'); time.sleep(wait)


def tag(scale): return scale.replace('.', '')


def do_tabs(label):
    for w, h, scale in CONFIGS:
        configure(w, h, scale); launch()
        centers = ready()
        print(w, scale, 'barra:', centers)
        for t in TABS:
            if t not in centers: continue
            adb('shell', 'input', 'tap', str(centers[t][0]), str(centers[t][1])); time.sleep(2.2)
            shot(os.path.join(OUT, '%s_%d_%s_%s.png' % (label, w, tag(scale), t)))


def dismiss_guide():
    """Si la guía de Nubi está abierta (el emulador quedó con el vuelo terminado y la guía sin ver), la salta: queda marcada como vista y no vuelve."""
    for _ in range(3):
        if tap_text('Saltar la guía'): time.sleep(2.0)
        else: break


def do_hoy200(label):
    for w, h, scale in [(393, 2292, '1.0'), (360, 2100, '1.0')]:
        configure(w, h, scale); launch(12.0)
        dismiss_guide()
        centers = ready()
        if 'Hoy' in centers: adb('shell', 'input', 'tap', str(centers['Hoy'][0]), str(centers['Hoy'][1])); time.sleep(2.0)
        px = round(w * DENSITY)
        # un arrastre lento (sin lanzamiento) de 525 px = 200 dp; empieza dentro de la pantalla (un borde dispararía Atrás)
        y0 = int(h * 0.62)
        adb('shell', 'input', 'swipe', str(px // 2), str(y0), str(px // 2), str(y0 - 525), '1800'); time.sleep(1.5)
        shot(os.path.join(OUT, '%s_hoy200_%d_%s.png' % (label, w, tag(scale))))


def tap_text(part):
    root = dump()
    if root is None: return False
    for n, b in nodes(root):
        if part in (n.get('text', '') or n.get('content-desc', '')):
            adb('shell', 'input', 'tap', str((b[0] + b[2]) // 2), str((b[1] + b[3]) // 2)); return True
    return False


def do_guia3(label):
    local = os.path.join(tempfile.gettempdir(), 'profile_extra_guia.xml')
    with open(local, 'w', encoding='utf-8', newline='\n') as f: f.write(PROFILE)
    for w, h, scale in [(393, 2292, '1.0'), (360, 2100, '2.0')]:
        configure(w, h, scale)
        adb('shell', 'am', 'force-stop', PKG); time.sleep(1.0)
        adb('push', local, '/data/local/tmp/profile_extra_guia.xml')
        adb('shell', 'run-as', PKG, 'cp', '/data/local/tmp/profile_extra_guia.xml', 'shared_prefs/profile_extra.xml')
        adb('shell', 'monkey', '-p', PKG, '-c', 'android.intent.category.LAUNCHER', '1'); time.sleep(12.0)
        ready(3)
        for step in (1, 2):
            ok = tap_text('Seguir'); print('Seguir', step, ok); time.sleep(2.5)
        time.sleep(1.5)
        shot(os.path.join(OUT, '%s_guia3_%d_%s.png' % (label, w, tag(scale))))


def main():
    label = sys.argv[1] if len(sys.argv) > 1 else 'medida'
    what = sys.argv[2:] or ['tabs']
    os.makedirs(OUT, exist_ok=True)
    try:
        if 'tabs' in what: do_tabs(label)
        if 'hoy200' in what: do_hoy200(label)
        if 'guia3' in what: do_guia3(label)
    finally:
        adb('shell', 'wm', 'size', 'reset')
        adb('shell', 'settings', 'put', 'system', 'font_scale', '1.0')
    print('Listo en', OUT)


main()
