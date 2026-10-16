# -*- coding: utf-8 -*-
"""Arma `docs/previews/plasticina/engranajes-piezas.png`: las piezas de Engranajes «Taller de reparación» en plasticina, con los sprites REALES del atlas (`Assets/Resources/Plasticina/engranajes`) a 3 píxeles por dp (el teléfono: 1 dp = 3 unidades del lienzo de 1080):
  - los 6 engranajes (grande y chico, en las tres paletas) y el estado presionado del motor;
  - el par grande-chico EN REPOSO con las fases de `MeshPhase` (holgura cero), el de antes (el dibujo procedural que deja la prueba en `unity/test-results/engranajes-antes`) y el de después, y mal encajado;
  - el radar, la turbina de 5 aspas, la polea (y la presionada), el portón, el marco, la plataforma y la caja de la carga, la barra dentada y el casco;
  - las flechas de giro (a favor y en contra del reloj, a color y en escala de grises) y los triángulos.
Todo sobre el fondo oscuro de la sala (los huecos entre dientes dejan ver el disco oscuro de atrás, como hoy).
Uso: python -I tools/juegos/hoja_engranajes.py [salida.png]"""
import json, math, os, sys
import numpy as np
from PIL import Image, ImageDraw
sys.stdout.reconfigure(encoding='utf-8')

AQUI = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.normpath(os.path.join(AQUI, '..', '..'))
PLAST = os.path.join(REPO, 'unity', 'NeuroVidaCore', 'Assets', 'Resources', 'Plasticina', 'engranajes')
ANTES = os.path.join(REPO, 'unity', 'test-results', 'engranajes-antes')
SALIDA = sys.argv[1] if len(sys.argv) > 1 else os.path.join(REPO, 'docs', 'previews', 'plasticina', 'engranajes-piezas.png')
PX = 3
SALA = (0x0F, 0x0C, 0x2E, 255); DISCO = (0x08, 0x06, 0x1C, 255); TEXTO = (171, 165, 210, 255)

MAN = json.load(open(os.path.join(PLAST, 'manifest.json'), encoding='utf-8')); ATLAS = Image.open(os.path.join(PLAST, 'atlas.png')).convert('RGBA')

def sprite(n):
    e = next(s for s in MAN['sprites'] if s['n'] == n)
    return ATLAS.crop((e['x'], ATLAS.height - e['y'] - e['h'], e['x'] + e['w'], ATLAS.height - e['y'])), e['ppu']

def antes(n):
    ruta = os.path.join(ANTES, n + '.png')
    if not os.path.exists(ruta): return None
    ppu = float(open(os.path.join(ANTES, n + '.ppu')).read()) if os.path.exists(os.path.join(ANTES, n + '.ppu')) else 3.5
    return Image.open(ruta).convert('RGBA'), ppu

def recorta(im): return im.crop(im.getbbox())                                                       # las flechas ocupan una parte chica de su lienzo: en la hoja, solo el dibujo

def a_px(im, ppu): return im.resize((max(1, round(im.width / ppu * PX)), max(1, round(im.height / ppu * PX))), Image.LANCZOS)
def pega(base, im, cx, cy): base.alpha_composite(im, (int(round(cx - im.width / 2)), int(round(cy - im.height / 2))))

def gris(im):
    a = np.asarray(im).astype(float); l = (0.2126 * a[..., 0] + 0.7152 * a[..., 1] + 0.0722 * a[..., 2]) * a[..., 3] / 255.0
    g = np.stack([l, l, l, np.where(a[..., 3] > 0, 255.0, 0.0)], axis=-1); return Image.fromarray(np.clip(g, 0, 255).astype(np.uint8), 'RGBA')

def muestrea(im, ppu, x, ydown, ang):
    """La imagen [im] (y hacia arriba, centrada) girada [ang] radianes en el sentido del reloj, en los puntos (x, ydown) dp de la tabla: como en la prueba (`FillAtRotated`)."""
    a = np.asarray(im).astype(float); H, W = a.shape[:2]; c, s = math.cos(ang), math.sin(ang)
    xs = x * c + ydown * s; ys = x * s - ydown * c
    fx = W / 2.0 + xs * ppu - 0.5; fy = H / 2.0 - ys * ppu - 0.5
    x0 = np.floor(fx).astype(int); y0 = np.floor(fy).astype(int); tx = (fx - x0)[..., None]; ty = (fy - y0)[..., None]
    def g(xx, yy):
        ok = ((xx >= 0) & (xx < W) & (yy >= 0) & (yy < H))[..., None]
        px = a[np.clip(yy, 0, H - 1), np.clip(xx, 0, W - 1)].copy(); pm = px[..., :3] * px[..., 3:4] / 255.0; px[..., :3] = pm
        return np.where(ok, px, 0.0)
    v = (g(x0, y0) * (1 - tx) + g(x0 + 1, y0) * tx) * (1 - ty) + (g(x0, y0 + 1) * (1 - tx) + g(x0 + 1, y0 + 1) * tx) * ty
    al = v[..., 3:4]; rgb = np.where(al > 1e-3, v[..., :3] / np.maximum(al / 255.0, 1e-3), 0.0)
    return Image.fromarray(np.clip(np.concatenate([rgb, al], axis=-1), 0, 255).astype(np.uint8), 'RGBA')

def par(big, small, ppu_a, ppu_b, ang_extra=0.0, escala=4):
    """El par grande-chico a 50 dp con las fases de MeshPhase, sobre el fondo de la sala con el disco oscuro de atrás de cada uno."""
    A_ = 0.37; pi_ = 2 * math.pi / 15; pj = 2 * math.pi / 10
    fi = (((0.0 - A_) / pi_) % 1.0 + 1.0) % 1.0; fj = ((0.5 - fi) % 1.0 + 1.0) % 1.0; B_ = 0.0 + math.pi - fj * pj
    x0, x1, y0, y1 = -40.0, 80.0, -40.0, 40.0
    gx, gy = np.meshgrid(np.arange(x0, x1, 1.0 / escala) + 0.5 / escala, np.arange(y0, y1, 1.0 / escala) + 0.5 / escala)
    base = Image.new('RGBA', gx.shape[::-1], SALA)
    d = ImageDraw.Draw(base)
    for cx, r in ((0.0, 33.0 - 1.0), (50.0, 23.0 - 1.0)):                                       # el disco oscuro de apoyo detrás de cada engranaje (el hueco entre dientes lo deja ver)
        d.ellipse(((cx - r - x0) * escala, (0 - r - y0) * escala, (cx + r - x0) * escala, (0 + r - y0) * escala), fill=DISCO)
    base.alpha_composite(muestrea(big, ppu_a, gx, gy, A_)); base.alpha_composite(muestrea(small, ppu_b, gx - 50.0, gy, B_ + ang_extra))
    return base

# ---- lo que se pega
filas = []
W_DP = 480
hoja = Image.new('RGBA', (W_DP * PX + 24, 3600), (0x08, 0x0A, 0x22, 255)); dh = ImageDraw.Draw(hoja)
def titulo(y, t): dh.text((12, y), t, fill=TEXTO)
y = 6
titulo(y, 'los engranajes: grande (15 dientes) en motor azul, camino lila y pieza dorada, y el motor PRESIONADO'); y += 16
fila = Image.new('RGBA', (W_DP * PX, 76 * PX), SALA)
for i, n in enumerate(('gear_0_0', 'gear_0_1', 'gear_0_2', 'gear_0_0_p')):
    im, ppu = sprite(n); pega(fila, a_px(im, ppu), (46 + 82 * i) * PX, 38 * PX)
hoja.alpha_composite(fila, (12, y)); y += fila.height + 8
titulo(y, 'y el chico (10 dientes) en las tres paletas y presionado'); y += 16
fila = Image.new('RGBA', (W_DP * PX, 56 * PX), SALA)
for i, n in enumerate(('gear_1_0', 'gear_1_1', 'gear_1_2', 'gear_1_0_p')):
    im, ppu = sprite(n); pega(fila, a_px(im, ppu), (36 + 82 * i) * PX, 28 * PX)
hoja.alpha_composite(fila, (12, y)); y += fila.height + 8

titulo(y, 'el engrane en reposo (holgura cero, fases de MeshPhase): antes (procedural) · después (plasticina) · mal encajado (medio paso)'); y += 16
big, pb = sprite('gear_0_0'); small, ps = sprite('gear_1_2'); ab = antes('gear_0_0'); asm = antes('gear_1_2')
trio = Image.new('RGBA', (W_DP * PX, 80 * 3 + 4), SALA); ancho = 120 * 3
paneles = []
if ab and asm: paneles.append(par(ab[0], asm[0], ab[1], asm[1], escala=3))
paneles.append(par(big, small, pb, ps, escala=3)); paneles.append(par(big, small, pb, ps, math.pi / 10, escala=3))
for i, p in enumerate(paneles): trio.alpha_composite(p, (i * ancho, 2))
hoja.alpha_composite(trio, (12, y)); y += trio.height + 8

titulo(y, 'lo que se mueve y la sala: radar (el punto coral lo hace asimétrico), turbina de 5 aspas, polea y presionada, barra dentada'); y += 16
fila = Image.new('RGBA', (W_DP * PX, 90 * PX), SALA)
xs = 34
for n in ('radar', 'turbina', 'polea', 'polea_p', 'barra'):
    im, ppu = sprite(n); a = a_px(im, ppu); pega(fila, a, xs * PX, 45 * PX); xs += int(a.width / PX) + 22
hoja.alpha_composite(fila, (12, y)); y += fila.height + 8

titulo(y, 'el portón, el marco, la plataforma y la caja de la carga'); y += 16
fila = Image.new('RGBA', (W_DP * PX, 80 * PX), SALA)
xs = 30
for n in ('porton', 'marco_carga', 'plataforma', 'caja_carga'):
    im, ppu = sprite(n); a = a_px(im, ppu); pega(fila, a, xs * PX, 40 * PX); xs += int(a.width / PX) + 26
hoja.alpha_composite(fila, (12, y)); y += fila.height + 8

titulo(y, 'las flechas del motor (grande y chica): a favor y en contra del reloj, a color y en escala de grises: la cabeza decide el sentido'); y += 16
fila = Image.new('RGBA', (W_DP * PX, 60 * PX), SALA)
for i, n in enumerate(('flecha_grande_cw', 'flecha_grande_ccw')):
    im, ppu = sprite(n); a = a_px(recorta(im), ppu); pega(fila, a, (58 + 116 * i) * PX, 30 * PX); pega(fila, gris(a), (58 + 232 + 116 * i) * PX, 30 * PX)
hoja.alpha_composite(fila, (12, y)); y += fila.height + 6
fila = Image.new('RGBA', (W_DP * PX, 52 * PX), SALA)
for i, n in enumerate(('flecha_chico_cw', 'flecha_chico_ccw')):
    im, ppu = sprite(n); a = a_px(recorta(im), ppu); pega(fila, a, (50 + 96 * i) * PX, 22 * PX); pega(fila, gris(a), (50 + 192 + 96 * i) * PX, 22 * PX)
for i, n in enumerate(('miniflecha_cw', 'miniflecha_ccw')):
    im, ppu = sprite(n); a = a_px(recorta(im), ppu); pega(fila, a, (400 + 36 * i) * PX, 14 * PX); pega(fila, gris(a), (400 + 36 * i) * PX, 38 * PX)
hoja.alpha_composite(fila, (12, y)); y += fila.height + 6
titulo(y, 'los triángulos de los carteles y el «play» de «Arrancar» (blancos: se tiñen)'); y += 16
fila = Image.new('RGBA', (W_DP * PX, 64 * PX), SALA)
for i, n in enumerate(('tri_arriba', 'tri_abajo', 'tri_play')):
    im, ppu = sprite(n); a = a_px(im, ppu); pega(fila, a, (40 + 70 * i) * PX, 32 * PX)
hoja.alpha_composite(fila, (12, y)); y += fila.height + 8

titulo(y, 'el casco del cohete'); y += 16
im, ppu = sprite('casco'); a = a_px(im, ppu); fila = Image.new('RGBA', (W_DP * PX, a.height + 8), SALA); pega(fila, a, 90 * PX, fila.height // 2)
hoja.alpha_composite(fila, (12, y)); y += fila.height + 8

hoja = hoja.crop((0, 0, hoja.width, y + 6))
os.makedirs(os.path.dirname(SALIDA), exist_ok=True)
hoja.convert('RGB').save(SALIDA, optimize=True); print('hoja', SALIDA, hoja.size)
