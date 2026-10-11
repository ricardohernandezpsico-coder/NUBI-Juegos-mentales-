# -*- coding: utf-8 -*-
"""Fondo tablero (el azul marino de la app): del render `fondo_tablero.png` saca el WebP a 2× para `app/src/main/res/drawable-nodpi/fondo_tablero.webp`
   y mide el contraste del texto crema y sol en la zona del saludo (x 4-70 %, y 8-22 % de la imagen, sin luceros por diseño; lo vuelve a medir `BoardBackgroundContrastTest`).
Uso: python -I tablero.py <carpeta_render_tablero> <carpeta_salida>"""
import sys, os
from PIL import Image
src, out = sys.argv[1], sys.argv[2]; os.makedirs(out, exist_ok=True)
def hx(h): h=h.lstrip('#'); return tuple(int(h[i:i+2],16) for i in (0,2,4))
def lum(c):
    def ch(v): v/=255; return v/12.92 if v<=0.03928 else ((v+0.055)/1.055)**2.4
    r,g,b = c; return 0.2126*ch(r)+0.7152*ch(g)+0.0722*ch(b)
def contrast(a,b):
    la, lb = lum(a), lum(b); hi, lo = max(la,lb), min(la,lb); return (hi+0.05)/(lo+0.05)
im = Image.open(os.path.join(src, 'fondo_tablero.png')).convert('RGB'); W, H = im.size
im.save(os.path.join(out, 'fondo_tablero.webp'), quality=86, method=6)
TEXTO = (0.04, 0.08, 0.70, 0.22); band = im.crop((int(W*TEXTO[0]), int(H*TEXTO[1]), int(W*TEXTO[2]), int(H*TEXTO[3]))); px = band.load()
cream, sun = hx('#F6EBD3'), hx('#FFCF5A'); wc = ws = 99
for y in range(0, band.height, 2):
    for x in range(0, band.width, 2):
        c = px[x,y]; wc = min(wc, contrast(cream, c)); ws = min(ws, contrast(sun, c))
print(f'tablero: contraste mínimo crema {wc:.1f}:1, sol {ws:.1f}:1 (zona del saludo)')
