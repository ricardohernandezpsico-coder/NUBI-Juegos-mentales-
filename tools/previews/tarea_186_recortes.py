# -*- coding: utf-8 -*-
"""Tres recortes chicos de la Tarea 186 después de las decisiones de Opus (28-oct), con CAPTURAS REALES:
  `docs/previews/tarea-186-seccion-plegada.png`  «Cómo está hecha Nubi» con cada grupo PLEGADO en «Estudios (N)» (letra 1,0 y 1,3).
  `docs/previews/tarea-186-hoy-racha-cero.png`   la semana de Hoy con la racha en 0 y una mejor de antes: «Tu racha empieza hoy» y «Tu mejor racha: 12 días.» (letra 1,0 y 1,3).
  `docs/previews/tarea-186-pie-rastro.png`       el pie del final de Rastro de luz: «Resultado de esta partida. Puede variar de una a otra. No es un diagnóstico.».
Entradas (fuera de git): `app/src/test/screenshots/tarea-186/propuesta/{mecanismos-plegado,hoy-racha-cero}-*.png` (`Tarea186CapturasTest`) y `app/src/test/screenshots/rastro-final-2.png` (`TrailResultScreenshotTest`), grabadas con `recordRoborazziDebug`.
Uso:  python -I tools/previews/tarea_186_recortes.py
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SHOTS = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots')
PROP = os.path.join(SHOTS, 'tarea-186', 'propuesta')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
OUT = os.path.join(ROOT, 'docs', 'previews')
NAVY, SUN, DIM = (20, 18, 63), (255, 203, 69), (200, 196, 235)
GAP = 24


def font(px): return ImageFont.truetype(os.path.join(FONTS, 'fredoka.ttf'), int(px))


def load(path): return Image.open(path).convert('RGB')


def scaled(im, f): return im.resize((round(im.width * f), round(im.height * f)), Image.LANCZOS)


def sheet(parts, title, scale):
    """parts = [(imagen, rótulo)] una al lado de la otra, con el título arriba."""
    ims = [(scaled(im, scale), label) for im, label in parts]
    w = GAP + sum(im.width + GAP for im, _ in ims)
    h = 70 + max(im.height for im, _ in ims) + 50
    out = Image.new('RGB', (w, h), NAVY)
    d = ImageDraw.Draw(out)
    d.text((GAP, 16), title, font=font(30), fill=SUN)
    x = GAP
    for im, label in ims:
        out.paste(im, (x, 64)); d.text((x, 64 + im.height + 8), label, font=font(24), fill=DIM); x += im.width + GAP
    return out


def save(im, name):
    os.makedirs(OUT, exist_ok=True)
    p = os.path.join(OUT, name); im.save(p, optimize=True); print('ok', p, im.size)


# 1. la sección plegada: del título de la sección y los primeros grupos (se corta bajo la barra de arriba)
a = load(os.path.join(PROP, 'mecanismos-plegado-1_0.png')).crop((0, 230, 1080, 1950))
b = load(os.path.join(PROP, 'mecanismos-plegado-1_3.png')).crop((0, 230, 1080, 1950))
save(sheet([(a, 'Letra 1,0'), (b, 'Letra 1,3')], '«Cómo está hecha Nubi», cada grupo plegado en «Estudios (N)»', 0.5), 'tarea-186-seccion-plegada.png')

# 2. Hoy con la racha en cero
h1 = load(os.path.join(PROP, 'hoy-racha-cero-1_0.png'))
h3 = load(os.path.join(PROP, 'hoy-racha-cero-1_3.png'))
save(sheet([(h1, 'Letra 1,0'), (h3, 'Letra 1,3')], 'Hoy con la racha en 0 y una mejor de antes (12 días)', 0.6), 'tarea-186-hoy-racha-cero.png')

# 3. el pie de Rastro de luz
r = load(os.path.join(SHOTS, 'rastro-final-2.png'))
save(sheet([(r.crop((0, 1640, r.width, r.height)), 'Final de Rastro de luz, al pie')], 'El pie de Rastro de luz', 0.6), 'tarea-186-pie-rastro.png')
