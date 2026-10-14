# -*- coding: utf-8 -*-
"""Una lámina de contacto para MIRAR los renders de Blender: pega cada PNG (con fondo transparente) sobre un fondo liso, en una cuadrícula, con su nombre.
Uso: python -I lamina.py <salida.png> <fondo #RRGGBB> <columnas> <escala> <archivo.png>...
(El fondo se elige igual al que tendrá la pieza en el juego: el vidrio del radar, el cielo, una casilla.)"""
import sys, os
from PIL import Image, ImageDraw
sys.stdout.reconfigure(encoding='utf-8')

def main():
    out, fondo, cols, escala = sys.argv[1], sys.argv[2], int(sys.argv[3]), float(sys.argv[4])
    files = sys.argv[5:]
    bg = tuple(int(fondo.lstrip('#')[i:i + 2], 16) for i in (0, 2, 4))
    ims = [Image.open(f).convert('RGBA') for f in files]
    ims = [im.resize((max(1, round(im.width * escala)), max(1, round(im.height * escala))), Image.LANCZOS) if escala != 1 else im for im in ims]
    cw = max(im.width for im in ims) + 16; ch = max(im.height for im in ims) + 34
    rows = (len(ims) + cols - 1) // cols
    sheet = Image.new('RGBA', (cw * cols, ch * rows), bg + (255,)); d = ImageDraw.Draw(sheet)
    for i, (f, im) in enumerate(zip(files, ims)):
        x = (i % cols) * cw + 8; y = (i // cols) * ch + 4
        sheet.alpha_composite(im, (x, y)); d.text((x, y + im.height + 6), os.path.splitext(os.path.basename(f))[0], fill=(220, 220, 240, 255))
    sheet.convert('RGB').save(out); print('lámina', out, sheet.size)

main()
