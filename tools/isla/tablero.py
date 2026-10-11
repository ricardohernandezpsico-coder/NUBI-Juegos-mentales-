# -*- coding: utf-8 -*-
"""Fondo en DOS capas (Tarea 83): saca de los renders del tablero los dos WebP a 2× de la app y comprueba que apilados dan el B aprobado.
   - `fondo_lamina.webp`: la lámina azul marino sola, sin luceros (render `--sin-luceros`). Va siempre detrás del pager.
   - `fondo_luceros.webp`: SOLO los luceros con su sombra de contacto, transparente (render `--solo-luceros`, con la lámina como «shadow catcher» de Cycles). Solo se ve en Hoy.
   El «shadow catcher» da bien dónde están los luceros y sus sombras (la máscara), pero su sombra sale gris neutra por el canal alfa, y la real es un poco cálida (rebote del lucero) y menos azul. Por eso, DENTRO de esa máscara
   (dilatada 3 px) el color y la opacidad de la capa se calculan para que `lámina · (1-α) + color · α` dé exactamente el píxel del B completo (render sin `--sin-luceros` ni `--solo-luceros`, con `--b-aprobado`):
   la α más baja que deja el color dentro de 0-255; fuera de la máscara, α = 0. Lo que sale se mide: diferencia de píxeles del apilado contra el B y contra otro render del mismo B (el ruido de Cycles).
Uso: python -I tablero.py <render_lamina> <render_luceros> <render_B_completo> <salida>
     (cada <render_*> es la carpeta con su PNG: fondo_lamina.png, fondo_luceros.png, fondo_tablero.png)"""
import sys, os
import numpy as np
sys.stdout.reconfigure(encoding='utf-8')
from PIL import Image

lam_dir, luc_dir, full_dir, out = sys.argv[1:5]
os.makedirs(out, exist_ok=True)
lam = np.asarray(Image.open(os.path.join(lam_dir, 'fondo_lamina.png')).convert('RGB')).astype(np.float64)
catcher = np.asarray(Image.open(os.path.join(luc_dir, 'fondo_luceros.png')).convert('RGBA')).astype(np.float64)
full = np.asarray(Image.open(os.path.join(full_dir, 'fondo_tablero.png')).convert('RGB')).astype(np.float64)
H, W = lam.shape[:2]

# 1. máscara: donde el «shadow catcher» vio un lucero o su sombra, dilatada 3 px
mask = catcher[..., 3] > 255 * 0.02
pad = np.pad(mask, 3, constant_values=False)          # sin dar la vuelta por los bordes (np.roll copiaba la máscara del borde derecho al izquierdo)
dil = np.zeros_like(mask)
for dy in range(7):
    for dx in range(7):
        dil |= pad[dy:dy + H, dx:dx + W]

# 2. α mínima y color por canal para reproducir el B completo (lámina · (1-α) + color · α = B)
need = np.where(full >= lam, (full - lam) / np.maximum(255.0 - lam, 1e-6), (lam - full) / np.maximum(lam, 1e-6))   # α que pide cada canal con color 255 (más claro) o 0 (más oscuro)
alpha = np.clip(need.max(axis=2), 0.0, 1.0)
alpha = np.ceil(alpha * 255.0) / 255.0                     # en pasos de 8 bits, hacia arriba: el color no se sale de 0-255
alpha[~dil] = 0.0
alpha[alpha < 1.5 / 255.0] = 0.0                           # ruido de render, no luceros
a3 = alpha[..., None]
color = np.where(a3 > 0, lam + (full - lam) / np.maximum(a3, 1e-6), 0.0)
color = np.clip(np.rint(color), 0, 255)
layer = np.dstack([color, np.rint(alpha * 255.0)]).astype(np.uint8)

Image.open(os.path.join(lam_dir, 'fondo_lamina.png')).convert('RGB').save(os.path.join(out, 'fondo_lamina.webp'), quality=86, method=6)
Image.fromarray(layer, 'RGBA').save(os.path.join(out, 'fondo_luceros.webp'), quality=90, alpha_quality=100, method=6)

# 3. medidas sobre lo que de verdad entra a la app (los WebP ya guardados)
lam_w = np.asarray(Image.open(os.path.join(out, 'fondo_lamina.webp')).convert('RGB')).astype(np.float64)
luc_w = np.asarray(Image.open(os.path.join(out, 'fondo_luceros.webp')).convert('RGBA')).astype(np.float64)
aw = luc_w[..., 3:4] / 255.0
stack = luc_w[..., :3] * aw + lam_w * (1 - aw)
d = np.abs(stack - full)
print(f'apilado (WebP) contra el B completo: media {d.mean():.2f}/255, p99 {np.percentile(d, 99):.1f}, máximo {int(d.max())}; píxeles con diferencia > 8: {100 * (d.max(axis=2) > 8).mean():.2f} %')
print(f'  dentro de la máscara de luceros ({100 * dil.mean():.1f} % de la imagen): media {d[dil].mean():.2f}, máximo {int(d[dil].max())}')
print(f'  fuera de la máscara (solo la lámina): media {d[~dil].mean():.2f}, máximo {int(d[~dil].max())}')
print(f'luceros: {int((luc_w[..., 3] >= 60).sum())} píxeles opacos (α ≥ 60), {int((luc_w[..., 3] > 0).sum())} con algo de α')

# 4. contraste del saludo sobre la lámina (zona x 4-70 %, y 8-22 %)
def hx(h): h = h.lstrip('#'); return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))
def lum(c):
    def ch(v): v /= 255; return v / 12.92 if v <= 0.03928 else ((v + 0.055) / 1.055) ** 2.4
    r, g, b = c; return 0.2126 * ch(r) + 0.7152 * ch(g) + 0.0722 * ch(b)
def contrast(a, b):
    la, lb = lum(a), lum(b); hi, lo = max(la, lb), min(la, lb); return (hi + 0.05) / (lo + 0.05)
band = lam_w[int(H * 0.08):int(H * 0.22):2, int(W * 0.04):int(W * 0.70):2].reshape(-1, 3)
cream, sun = hx('#F6EBD3'), hx('#FFCF5A')
print(f'lámina: contraste mínimo crema {min(contrast(cream, c) for c in band):.1f}:1, sol {min(contrast(sun, c) for c in band):.1f}:1 (zona del saludo)')
