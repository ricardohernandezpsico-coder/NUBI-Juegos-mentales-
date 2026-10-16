# -*- coding: utf-8 -*-
"""Arma `docs/previews/plasticina/aterrizaje-piezas.png`: la zona de la regla como la arma el juego (la regla de plasticina estirada a 304 × 20 dp con sus marcas de extremo y del medio, los números y el asta de la bandera, TODO en tinta y en su x de siempre; el gallardete de
plasticina; la nave posada con su eje vertical marcado), y las piezas sueltas: la nave (con su espejo), la Tierra, el satélite, las dos cúpulas del marcador, el gallardete y las insignias ✓ y aspa de lo común. Con los sprites REALES de los atlas
(`Assets/Resources/Plasticina/aterrizaje` y `comun`), a 3 píxeles por dp (el teléfono: 1 dp = 3 unidades del lienzo de 1080).
Uso: python -I tools/juegos/hoja_aterrizaje.py [salida.png]"""
import json, math, os, sys
import numpy as np
from PIL import Image, ImageDraw
sys.stdout.reconfigure(encoding='utf-8')

AQUI = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.normpath(os.path.join(AQUI, '..', '..'))
PLAST = os.path.join(REPO, 'unity', 'NeuroVidaCore', 'Assets', 'Resources', 'Plasticina')
SALIDA = sys.argv[1] if len(sys.argv) > 1 else os.path.join(REPO, 'docs', 'previews', 'plasticina', 'aterrizaje-piezas.png')
PX = 3
CIELO = (0x0A, 0x0F, 0x33, 255); LUNA = (0x8E, 0x86, 0xD0, 255); TINTA = (26, 18, 64, 255)

def abre(grupo):
    man = json.load(open(os.path.join(PLAST, grupo, 'manifest.json'), encoding='utf-8')); at = Image.open(os.path.join(PLAST, grupo, 'atlas.png')).convert('RGBA')
    return man, at
MAN, ATLAS = abre('aterrizaje'); MCOM, ACOM = abre('comun')

def sprite(n, grupo='aterrizaje'):
    man, at = (MAN, ATLAS) if grupo == 'aterrizaje' else (MCOM, ACOM)
    e = next(s for s in man['sprites'] if s['n'] == n)
    return at.crop((e['x'], at.height - e['y'] - e['h'], e['x'] + e['w'], at.height - e['y'])), e

def a_px(im, e, ancho_dp=None, alto_dp=None):
    """El sprite a la hoja: a su tamaño natural en dp (px / ppu) por [PX] píxeles por dp, o a [ancho_dp] × [alto_dp] si se dan (lo que hace el controlador)."""
    w = ancho_dp if ancho_dp is not None else im.width / e['ppu']; h = alto_dp if alto_dp is not None else im.height / e['ppu']
    return im.resize((max(1, round(w * PX)), max(1, round(h * PX))), Image.LANCZOS)

def pega(base, im, cx, cy): base.alpha_composite(im, (int(round(cx - im.width / 2)), int(round(cy - im.height / 2))))

def nueve(im, e, ancho_dp, alto_dp):
    """Una losa nine-slice (los bordes izq, abajo, der, arr del manifiesto) con el CUERPO de [ancho_dp] × [alto_dp]; devuelve la imagen con su sombra y el desplazamiento del cuerpo."""
    bl, bb, br, bt = e['borde']; ml, mt, mr, mb = e['margen']
    W = round(ancho_dp * PX) + ml + mr; H = round(alto_dp * PX) + mt + mb; w0, h0 = im.size
    xs = [0, bl, w0 - br, w0]; ys = [0, bt, h0 - bb, h0]; xd = [0, bl, W - br, W]; yd = [0, bt, H - bb, H]
    out = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    for i in range(3):
        for j in range(3):
            parte = im.crop((xs[i], ys[j], xs[i + 1], ys[j + 1])); tam = (max(1, xd[i + 1] - xd[i]), max(1, yd[j + 1] - yd[j]))
            out.alpha_composite(parte.resize(tam, Image.LANCZOS) if parte.size != tam else parte, (xd[i], yd[j]))
    return out, (ml, mt)

# ---- 1. la zona de la regla como la arma el juego (360 × 190 dp)
W_DP, H_DP = 360, 190
zona = Image.new('RGBA', (W_DP * PX, H_DP * PX), CIELO)
d = ImageDraw.Draw(zona)
d.rectangle((0, 112 * PX, W_DP * PX, H_DP * PX), fill=LUNA)                                         # la luna (lavanda) detrás de la regla
RY = 120.0                                                                                          # el arriba de la regla (dp)
rg, er = sprite('regla'); cuerpo, (ox, oy) = nueve(rg, er, 304.0, 20.0)
zona.alpha_composite(cuerpo, (round(180 * PX - 152 * PX) - ox, round(RY * PX) - oy))
y_c = RY + 10.0
def tinta(cx, cy, w, h): d.rectangle((round((cx - w / 2) * PX), round((cy - h / 2) * PX), round((cx + w / 2) * PX), round((cy + h / 2) * PX)), fill=TINTA)
tinta(36.0, y_c, 3.4, 32.0); tinta(324.0, y_c, 3.4, 32.0); tinta(180.0, y_c, 2.6, 14.0)                 # las marcas: procedurales, en tinta, en su x
d.text((round(28 * PX), round((RY + 28) * PX)), '0', fill=(255, 255, 255, 255)); d.text((round(318 * PX), round((RY + 28) * PX)), '100', fill=(255, 255, 255, 255))
tx = 36.0 + 0.37 * 288.0; fh = 80.0
nv, en = sprite('nave'); pega(zona, a_px(nv, en, 90, 90), 158 * PX, (RY - 50) * PX)                   # la nave posada; la bandera se dibuja ENCIMA de ella
tinta(tx, RY - fh / 2, 3.0, fh)                                                                      # el asta: en el lugar justo
gl, eg = sprite('gallardete'); pega(zona, a_px(gl, eg, 36, 24), (tx + 15) * PX, (RY - fh + 9) * PX)
d.line((158 * PX, (RY - 100) * PX, 158 * PX, (RY - 4) * PX), fill=(255, 120, 120, 200), width=1)       # el eje vertical de la nave (su x es la respuesta)

# ---- 2. las piezas sueltas
piezas = Image.new('RGBA', (W_DP * PX, 250 * PX), CIELO); dp = ImageDraw.Draw(piezas)
def etiqueta(x, y, t): dp.text((x, y), t, fill=(220, 220, 240, 255))
pega(piezas, a_px(nv, en), 52 * PX, 50 * PX); espejo = nv.transpose(Image.FLIP_LEFT_RIGHT)
pega(piezas, a_px(espejo, en), 132 * PX, 50 * PX); etiqueta(18 * PX, 92 * PX, 'la nave y su espejo (idénticos)')
tr, et = sprite('tierra'); pega(piezas, a_px(tr, et), 270 * PX, 76 * PX); etiqueta(246 * PX, 142 * PX, 'la Tierra')
sa, es = sprite('satelite'); pega(piezas, a_px(sa, es), 40 * PX, 132 * PX); etiqueta(22 * PX, 146 * PX, 'el satélite')
on, eo = sprite('domo_on'); off, ef = sprite('domo_off'); pega(piezas, a_px(on, eo), 120 * PX, 132 * PX); pega(piezas, a_px(off, ef), 160 * PX, 132 * PX); etiqueta(112 * PX, 146 * PX, 'las cúpulas')
pega(piezas, a_px(gl, eg), 220 * PX, 190 * PX); etiqueta(196 * PX, 206 * PX, 'el gallardete')
ok, eok = sprite('marca_ok', 'comun'); no, eno = sprite('marca_no', 'comun'); pega(piezas, ok.resize((28 * PX, 28 * PX), Image.LANCZOS), 40 * PX, 190 * PX); pega(piezas, no.resize((28 * PX, 28 * PX), Image.LANCZOS), 80 * PX, 190 * PX); etiqueta(20 * PX, 210 * PX, 'las insignias ✓ y aspa')

hoja = Image.new('RGBA', (W_DP * PX + 24, zona.height + piezas.height + 70), (0x08, 0x0A, 0x22, 255)); dh = ImageDraw.Draw(hoja)
dh.text((12, 6), 'la zona de la regla como la arma el juego: regla de plasticina; marcas, números y asta en tinta y en su x; nave posada (línea roja = su eje)', fill=(171, 165, 210, 255)); hoja.alpha_composite(zona, (12, 22))
dh.text((12, 22 + zona.height + 12), 'las piezas', fill=(171, 165, 210, 255)); hoja.alpha_composite(piezas, (12, 22 + zona.height + 30))
os.makedirs(os.path.dirname(SALIDA), exist_ok=True)
hoja.convert('RGB').save(SALIDA, optimize=True); print('hoja', SALIDA, hoja.size)
