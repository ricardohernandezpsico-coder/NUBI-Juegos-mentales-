"""Ayudante para manejar el EMULADOR de Android (`medium_phone`, API 36) con adb + uiautomator: ver docs/respaldo-y-diagnostico.md, «Barras del sistema».

NUNCA toca un teléfono real: el aparato se fija con SERIAL y el programa se niega si no es un emulador (`emulator-XXXX`). Uso desde Git Bash, en la carpeta del repo:
  python tools/emulador_ui.py texts                 # los textos de la pantalla (accesibilidad)
  python tools/emulador_ui.py tap "Juegos"          # toca el primer nodo que contenga ese texto
  python tools/emulador_ui.py xy 540 2172           # toca una coordenada (px)
  python tools/emulador_ui.py shot ficha.png        # captura (a unity/test-results/ si es un nombre suelto)
"""
import os
import re
import subprocess
import sys
import time
import xml.etree.ElementTree as ET

SERIAL = os.environ.get('NUBI_EMULADOR', 'emulator-5554')
ADB = os.environ.get('ADB', os.path.expandvars('%LOCALAPPDATA%/Android/Sdk/platform-tools/adb.exe') if os.name == 'nt' else 'adb')
OUT = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), 'unity', 'test-results')

if not SERIAL.startswith('emulator-'):
    sys.exit('Este ayudante solo maneja emuladores (emulator-XXXX), nunca un teléfono real: %s' % SERIAL)


def adb(*args, binary=False):
    r = subprocess.run([ADB, '-s', SERIAL] + list(args), capture_output=True)
    return r.stdout if binary else r.stdout.decode('utf-8', 'replace')


def dump():
    adb('shell', 'uiautomator', 'dump', '/sdcard/ui.xml')
    xml = adb('exec-out', 'cat', '/sdcard/ui.xml')
    return ET.fromstring(xml[xml.index('<'):]) if xml else None


def nodes(root):
    for n in root.iter('node'):
        b = re.findall(r'\d+', n.get('bounds', ''))
        if len(b) == 4:
            yield {'text': n.get('text', ''), 'desc': n.get('content-desc', ''), 'bounds': tuple(map(int, b))}


def find(root, needle):
    for n in nodes(root):
        if needle.lower() in n['text'].lower() or needle.lower() in n['desc'].lower():
            return n
    return None


def tap_xy(x, y):
    adb('shell', 'input', 'tap', str(x), str(y))


def tap(needle, wait=1.2):
    n = find(dump(), needle)
    if not n:
        return False
    x1, y1, x2, y2 = n['bounds']
    tap_xy((x1 + x2) // 2, (y1 + y2) // 2)
    time.sleep(wait)
    return True


def shot(name):
    path = name if os.path.dirname(name) else os.path.join(OUT, name)
    open(path, 'wb').write(adb('exec-out', 'screencap', '-p', binary=True))
    return path


def texts():
    return [n['text'] or n['desc'] for n in nodes(dump()) if n['text'] or n['desc']]


if __name__ == '__main__':
    cmd = sys.argv[1] if len(sys.argv) > 1 else ''
    if cmd == 'texts':
        print('\n'.join(texts()))
    elif cmd == 'tap':
        print('tap', sys.argv[2], tap(sys.argv[2]))
    elif cmd == 'xy':
        tap_xy(int(sys.argv[2]), int(sys.argv[3]))
    elif cmd == 'shot':
        print(shot(sys.argv[2]))
    else:
        sys.exit(__doc__)
