"""Hoja de contacto de las CAPTURAS REALES de un juego (docs/previews/<juego>-estacion.png para Correo).

Las capturas las saca Unity (NO es una composición aparte): `bash tools/verificar-todo.sh --capturas Correo` corre el juego en forma de teléfono (1080x2400), lo lleva por sus momentos clave con un guion y guarda un PNG por momento en
unity/test-results/capturas/<juego>/NN-<momento>.png; este script los junta en UNA lámina con su rótulo debajo.

Uso:  python tools/capturas/hoja.py <juego> [--in unity/test-results/capturas/<juego>] [--out docs/previews/<archivo>.png]
Hoy solo hay guion de capturas de Correo (docs/previews/correo-estacion.png).
"""
import argparse
import os
import sys

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
FB = ROOT + '/unity/NeuroVidaCore/Assets/Resources/Fonts/Fredoka-Bold.ttf'
FS = ROOT + '/unity/NeuroVidaCore/Assets/Resources/Fonts/Fredoka-SemiBold.ttf'
BG = (13, 16, 40)
TEXT = (237, 234, 251)
LAV = (171, 165, 210)

# momento -> rótulo (en el orden en que se guardan)
CAPTIONS = {
    'correo': {
        'nuevo-estacion': 'Tarjeta «NUEVO»: la estación',
        'nuevo-lazo': 'Tarjeta «NUEVO»: el lazo',
        'hoja': 'La hoja de encargos de la mañana',
        'cinta-dorado': 'La cinta: sello dorado delante',
        'pausa': 'La pausa',
        'cinta-lazo': 'La cinta: carta con lazo',
        'faro': 'El faro: la nave del correo llega',
        'resumen': 'El resumen del día',
        'final': 'La pantalla final',
    },
}
OUT_DEFAULT = {'correo': 'docs/previews/correo-estacion.png'}


def font(path, size):
    return ImageFont.truetype(path, size)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('juego')
    ap.add_argument('--in', dest='src')
    ap.add_argument('--out')
    a = ap.parse_args()
    game = a.juego.lower()
    if game not in CAPTIONS:
        sys.exit('Sin guion de capturas para «%s» (hoy solo: %s)' % (a.juego, ', '.join(CAPTIONS)))
    src = a.src or os.path.join(ROOT, 'unity', 'test-results', 'capturas', game)
    out = a.out or os.path.join(ROOT, OUT_DEFAULT[game])
    files = sorted(f for f in os.listdir(src) if f.lower().endswith('.png'))
    if not files:
        sys.exit('No hay capturas en ' + src)
    cols = 5
    w, h = 270, 600                                   # cada captura, de 1080x2400 a 270x600 (un cuarto)
    pad, cap_h, top = 18, 54, 70
    rows = (len(files) + cols - 1) // cols
    W = pad + cols * (w + pad)
    H = top + rows * (h + cap_h + pad)
    sheet = Image.new('RGB', (W, H), BG)
    d = ImageDraw.Draw(sheet)
    d.text((pad, 18), 'Capturas reales de Unity (lienzo de teléfono 1080×2400)', font=font(FB, 24), fill=TEXT)
    d.text((pad, 46), 'Sale de `bash tools/verificar-todo.sh --capturas ' + a.juego + '`: el juego de verdad, no una composición.', font=font(FS, 14), fill=LAV)
    for i, f in enumerate(files):
        im = Image.open(os.path.join(src, f)).convert('RGB').resize((w, h), Image.LANCZOS)
        r, c = divmod(i, cols)
        x = pad + c * (w + pad)
        y = top + r * (h + cap_h + pad)
        sheet.paste(im, (x, y))
        d.rectangle([x - 1, y - 1, x + w, y + h], outline=(60, 64, 110), width=1)
        key = f[3:-4] if len(f) > 7 else f
        d.text((x + w / 2, y + h + 10), CAPTIONS[game].get(key, key), font=font(FB, 15), fill=TEXT, anchor='mt')
    os.makedirs(os.path.dirname(out), exist_ok=True)
    sheet.save(out, optimize=True)
    print('OK', out, sheet.size)


if __name__ == '__main__':
    main()
