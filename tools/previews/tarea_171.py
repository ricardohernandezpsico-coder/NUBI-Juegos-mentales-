# -*- coding: utf-8 -*-
"""Lámina de la Tarea 171, «La ficha del juego: Portada» (`docs/previews/tarea-171.png`), con CAPTURAS REALES (no maquetas):
  1. las 17 portadas a tamaño real (1080 × 480 px: las que lleva la app, recortadas de capturas reales de Unity con `tools/previews/portadas.py`);
  2. la ficha «hoy | propuesta» en Piloto Estelar (con Reto) y en Correo Estelar (sin Reto), con letra 1,0 y 1,3 (Roborazzi a 393 × 873 dp menos las barras del sistema);
  3. «Más opciones» desplegado y «Sobre este juego»;
  4. las 17 fichas plegadas, para ver cada portada dentro de su ventanilla con el emblema asomando;
  5. el EMULADOR (API 36) con la letra del sistema al 200 %.
Entradas: `app/src/test/screenshots/tarea-171/*.png` (`./gradlew.bat :app:recordRoborazziDebug --tests "*Tarea171CapturasTest*"`; las `hoy-*.png` salen de una copia temporal de la ficha de ANTES, commit 37bac38),
`app/src/main/res/drawable-nodpi/portada_*.webp` y `app/src/test/screenshots/tarea-171-emulador/*.png` (`tools/emulador_ui.py`, solo el emulador).
Uso:  python -I tools/previews/tarea_171.py
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SHOTS = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots', 'tarea-171')
EMU = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots', 'tarea-171-emulador')
RES = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'drawable-nodpi')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-171.png')
NAVY, SUN, DIM, CREAM = (20, 18, 63), (255, 203, 69), (200, 196, 235), (245, 235, 219)
GAP = 36
W = 3 * 1080 + 4 * GAP                                               # tres portadas a tamaño real por fila

GAMES = [  # (id, nombre) en el orden del catálogo
    ('parejas', 'Constelaciones'), ('secuencia', 'Rastro de luz'), ('bodega', 'Bodega de carga'), ('correo', 'Correo Estelar'), ('stroop', 'Tinta o Palabra'), ('piloto', 'Piloto Estelar'),
    ('freno', 'Freno de Emergencia'), ('satelites', 'Satélites'), ('radar', 'Rescate relámpago'), ('calculo', 'Carga exacta'), ('engranajes', 'Engranajes'), ('acoplamiento', 'Acoplamiento'),
    ('aterrizaje', 'Aterrizaje Lunar'), ('anagramas', 'En la punta de la lengua'), ('meteoros', 'Lluvia de meteoros'), ('disparate', '¿Verdad o disparate?'), ('intrusa', 'La estrella intrusa'),
]


def font(px, bold=True): return ImageFont.truetype(os.path.join(FONTS, 'fredoka.ttf' if bold else 'nunito_regular.ttf'), int(px))


def load(path):
    return Image.open(path).convert('RGB') if os.path.exists(path) else None


def fit_w(im, w):
    return im.resize((w, round(im.height * w / im.width)), Image.LANCZOS)


class Sheet:
    def __init__(self): self.parts = []; self.y = 0
    def add(self, draw_fn, h): self.parts.append((self.y, draw_fn, h)); self.y += h


def build():
    # 1. las 17 portadas, tres por fila, a tamaño real
    covers = [(g, n, load(os.path.join(RES, f'portada_{g}.webp'))) for g, n in GAMES]
    rows1 = (len(covers) + 2) // 3
    h1 = 150 + rows1 * (480 + 70)
    # 2. hoy | propuesta
    pw = 800
    pair_h = round(2202 * pw / 1080) + 90
    h2 = 150 + 2 * pair_h
    # 3. más opciones y sobre este juego
    mw = 640
    h3 = 150 + round(2202 * mw / 1080) + 90
    # 4. las 17 fichas plegadas (la parte de arriba)
    fw = 400; per = 8
    rows4 = (17 + per - 1) // per
    top_h = 400
    h4 = 150 + rows4 * (top_h + 60)
    # 5. emulador
    ew = 640
    h5 = 150 + round(2400 * ew / 1080) + 90
    H = 170 + h1 + h2 + h3 + h4 + h5 + 80
    sheet = Image.new('RGB', (W, H), NAVY); d = ImageDraw.Draw(sheet)
    d.text((GAP, 24), 'La ficha del juego: Portada (Tarea 171)', font=font(66), fill=SUN)
    d.text((GAP, 108), 'Capturas reales: las portadas son recortes de capturas de Unity; las fichas, Roborazzi a 393 × 873 dp; el emulador, API 36 con la letra del sistema al 200 %. Nada de esto es una maqueta.', font=font(30, False), fill=DIM)
    y = 170

    def title(txt, sub):
        nonlocal y
        d.text((GAP, y), txt, font=font(46), fill=SUN); d.text((GAP, y + 62), sub, font=font(26, False), fill=DIM); y += 150

    kb = sum(os.path.getsize(os.path.join(RES, f'portada_{g}.webp')) for g, _ in GAMES) // 1024
    title('1 · Las 17 portadas, a tamaño real (1080 × 480 px)', f'Cada una es un recorte de una captura real del juego en plena partida, sin el marcador ni avisos dentro. {kb} KB en total en webp. La ficha las muestra a 150 dp de alto.')
    for i, (g, n, im) in enumerate(covers):
        x = GAP + (i % 3) * (1080 + GAP); yy = y + (i // 3) * (480 + 70)
        if im: sheet.paste(im, (x, yy))
        d.text((x, yy + 486), n, font=font(30), fill=CREAM)
    y += rows1 * (480 + 70)

    title('2 · La ficha, hoy | propuesta', 'Izquierda de cada pareja: la ficha de ANTES (commit 37bac38). Derecha: la ficha nueva. Piloto Estelar (con Reto) y Correo Estelar (sin Reto), con letra 1,0 y 1,3.')
    for r, (gid, label) in enumerate([('piloto', 'Piloto Estelar · con Reto'), ('correo', 'Correo Estelar · sin Reto')]):
        x = GAP
        for scale in ('1_0', '1_3'):
            for kind, cap in (('hoy-', 'hoy'), ('', 'propuesta')):
                im = load(os.path.join(SHOTS, f'{kind}{gid}-{scale}.png'))
                if im:
                    p = fit_w(im, pw); sheet.paste(p, (x, y))
                    d.text((x, y + p.height + 8), f'{label} · letra {scale.replace("_", ",")} · {cap}', font=font(28), fill=CREAM)
                x += pw + 30
        y += pair_h
    title('3 · «Más opciones» desplegado y «Sobre este juego»', 'Más opciones despliega las casillas de siempre; «Sobre este juego» abre la lámina con «Qué pones en juego», su fuente y «Cómo se juega» (abre el tutorial otra vez).')
    x = GAP
    for name, cap in (('piloto-mas-1_0', 'Piloto · Más opciones · 1,0'), ('piloto-mas-1_3', 'Piloto · Más opciones · 1,3'), ('correo-mas-1_0', 'Correo · Más opciones · 1,0 · sin «¿Con reloj?»'),
                      ('piloto-sobre-1_0', 'Sobre este juego · 1,0'), ('piloto-sobre-1_3', 'Sobre este juego · 1,3')):
        im = load(os.path.join(SHOTS, f'{name}.png'))
        if im:
            p = fit_w(im, mw); sheet.paste(p, (x, y)); d.text((x, y + p.height + 8), cap, font=font(26), fill=CREAM)
        x += mw + 30
    y += h3 - 150
    title('4 · Las 17 portadas dentro de su ficha (la parte de arriba, letra 1,0)', 'Para ver que cada escena se lee sobre la ventanilla con el emblema asomando y los botones sobre ella.')
    for i, (g, n) in enumerate(GAMES):
        im = load(os.path.join(SHOTS, f'juego-{g}.png'))
        x = GAP + (i % per) * (fw + 12); yy = y + (i // per) * (top_h + 60)
        if im:
            p = fit_w(im, fw).crop((0, 185, fw, 185 + 330)); sheet.paste(p, (x, yy))
        d.text((x, yy + 336), n, font=font(22), fill=CREAM)
    y += rows4 * (top_h + 60)
    title('5 · En el emulador (API 36) con la letra del sistema al 130 % y al 200 %', 'Nada se sale ni se parte. Con letra 2,0 «Desafío» y «Más opciones» se apilan, «Sobre este juego» pasa a dos líneas y la ficha plegada sigue cabiendo entera; desplegada, se desliza sola.')
    x = GAP
    for name, cap in (('ficha-radar-130', 'Rescate · plegada · letra 1,3'), ('ficha-radar-200', 'Rescate · plegada · letra 2,0'), ('ficha-radar-200-mas', 'Rescate · Más opciones · 2,0'), ('ficha-correo-200', 'Correo (sin Reto) · plegada · 2,0'), ('ficha-sobre-200', 'Sobre este juego · 2,0')):
        im = load(os.path.join(EMU, f'{name}.png'))
        if im:
            p = fit_w(im, ew); sheet.paste(p, (x, y)); d.text((x, y + p.height + 8), cap, font=font(26), fill=CREAM)
        x += ew + 30
    sheet = sheet.crop((0, 0, W, y + h5 - 40 + 60))
    sheet.save(OUT, optimize=True); print('lámina:', OUT, sheet.size, os.path.getsize(OUT) // 1024, 'KB')


if __name__ == '__main__':
    build()
