# -*- coding: utf-8 -*-
"""Lámina «hoy | propuesta» del UN SOLO FINAL (Tarea 126, `auditoria-fable/pulido-juegos.md` §2): tres juegos (Rescate relámpago, Engranajes y La estación de correo) y, de cada uno, los tres cuadros que ve quien termina una partida:
  1. el momento del objeto (hoy: el panel de resultado de Unity con su tabla; propuesta: la escena apagada y el emblema del juego),
  2. la cortina «¡Listo!» (hoy: el cielo viejo y la placa sola; propuesta: el cielo nuevo con el emblema),
  3. el resultado en la app (hoy: puntaje gigante, tres estrellas y «¡Muy bien!»; propuesta: la placa del juego y directo el final con sentido).

Las tomas de «hoy» y de «propuesta» son REALES salvo lo que dice la leyenda de cada una: la cortina de hoy y el panel de Engranajes de hoy son réplicas hechas a mano de lo que dibujaba el código anterior (no hay toma real: la cortina nunca se
sacó sola y Engranajes no llegaba a su final en las capturas de 40 s). Las demás vienen de Unity (`bash tools/verificar-todo.sh --capturas <juego>`) y de Roborazzi (`:app:recordRoborazziDebug`; «hoy» se grabó con el código de HEAD de antes de la Tarea 126).

Uso:
  python -I tools/previews/final_unico.py --tomar     copia (a 540 px de ancho) las tomas desde donde quedaron a docs/previews/final-unico/   (las rutas de abajo)
  python -I tools/previews/final_unico.py             arma docs/previews/final-unico.png con lo que hay en docs/previews/final-unico/
"""
import os, random, shutil, sys
from PIL import Image, ImageDraw, ImageFont, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SRC = os.path.join(ROOT, 'docs', 'previews', 'final-unico')
OUT = os.path.join(ROOT, 'docs', 'previews', 'final-unico.png')
FONT = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font', 'fredoka.ttf'); NUNITO = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font', 'nunito_regular.ttf')
BG = (9, 11, 38); SUN = (255, 201, 60); DIM = (186, 190, 230); WHITE = (255, 255, 255); INK = (31, 22, 64)
PW = 300                                    # ancho de cada teléfono en la lámina
GAMES = [('radar', 'Rescate relámpago'), ('engranajes', 'Engranajes: taller de reparación'), ('correo', 'La estación de correo')]

# de dónde salen las tomas al usar --tomar (rutas del PC de Ricardo; ver la leyenda de arriba)
CAP = os.path.join(ROOT, 'unity', 'test-results', 'capturas')          # tomas sueltas de Unity de esta tarea (--capturas)
HOY_CAP = os.environ.get('NUBI_HOY_CAPTURAS', '')                       # carpeta con las tomas de Unity de ANTES (unity/test-results/capturas/ de antes de la tarea)
APP_NUEVA = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots')    # Roborazzi de esta tarea
APP_HOY = os.environ.get('NUBI_HOY_APP', '')                            # Roborazzi del código de antes (carpeta app/src/test/screenshots de un worktree en HEAD)
APP_SHOT = {'radar': 'rescate-final', 'engranajes': 'engranajes-final-sentido-1', 'correo': 'correo-final-sentido-1'}


def chico(im, w=540):
    return im.convert('RGB').resize((w, int(im.height * w / im.width)), Image.LANCZOS)


def toma_unity(game, suffix):
    """La toma de Unity de un juego cuyo archivo termina en -suffix.png (p. ej. «momento»), o None."""
    folder = os.path.join(CAP, game)
    if not os.path.isdir(folder): return None
    for name in sorted(os.listdir(folder)):
        if name.endswith('-' + suffix + '.png'): return os.path.join(folder, name)
    return None


def tomar():
    os.makedirs(SRC, exist_ok=True)
    faltan = []
    for g, _ in GAMES:
        for nombre, ruta in (('prop-momento', toma_unity(g, 'momento')), ('prop-cortina', toma_unity(g, 'listo')),
                             ('prop-app', os.path.join(APP_NUEVA, APP_SHOT[g] + '.png')),
                             ('hoy-app', os.path.join(APP_HOY, APP_SHOT[g] + '.png') if APP_HOY else None),
                             ('hoy-panel', os.path.join(HOY_CAP, g, '12-final.png') if (HOY_CAP and g != 'engranajes') else None)):
            if ruta and os.path.exists(ruta): chico(Image.open(ruta)).save(os.path.join(SRC, '%s-%s.png' % (nombre, g)), optimize=True)
            elif not (nombre == 'hoy-panel' and g == 'engranajes'): faltan.append('%s-%s' % (nombre, g))
    print('tomadas; faltan:', faltan or 'ninguna')


# ------------------------------------------------------------------ réplicas de lo que dibujaba el código de antes
def fuente(path, size):
    f = ImageFont.truetype(path, size)
    if path == FONT:
        try: f.set_variation_by_name('Bold')               # Fredoka es variable y arranca en Light
        except Exception: pass
    return f


def centrado(d, xy, text, font, fill):
    w = d.textlength(text, font=font)
    d.text((xy[0] - w / 2, xy[1]), text, font=font, fill=fill)


def cielo_viejo(w=540, h=1200):
    """El cielo de los juegos de ANTES (NeuroStyle.Night*: #02030F abajo, #050823 al medio, #0A0F33 arriba)."""
    top, mid, bot = (10, 15, 51), (5, 8, 35), (2, 3, 15)
    im = Image.new('RGB', (w, h))
    px = im.load()
    for y in range(h):
        t = 1 - y / (h - 1)                         # 0 arriba -> 1 abajo, como el degradé de Unity (abajo = NightBottom)
        a, b, k = (bot, mid, t * 2) if t < 0.5 else (mid, top, (t - 0.5) * 2)
        c = tuple(int(a[i] + (b[i] - a[i]) * k) for i in range(3))
        for x in range(w): px[x, y] = c
    return im


def cortina_hoy(w=540, h=1200):
    """La cortina «¡Listo!» de hoy: el cielo viejo, estrellas, la placa sol con «¡Listo!» y «Veamos cómo te fue» (FinishCurtain de HEAD: a 0,55 de la altura, sin emblema)."""
    im = cielo_viejo(w, h)
    d = ImageDraw.Draw(im)
    rnd = random.Random(29)
    for _ in range(70):
        x, y = rnd.randint(0, w), rnd.randint(0, h); r = rnd.choice((1, 1, 2))
        d.ellipse((x - r, y - r, x + r, y + r), fill=(200, 205, 240))
    cy = int(h * 0.45)
    d.rounded_rectangle((w / 2 - 195 + 4, cy - 68 + 8, w / 2 + 195 + 4, cy + 68 + 8), 40, fill=(184, 120, 20))        # sombra dura
    d.rounded_rectangle((w / 2 - 195, cy - 68, w / 2 + 195, cy + 68), 40, fill=SUN)
    centrado(d, (w / 2, cy - 52), '¡Listo!', fuente(FONT, 92), INK)
    centrado(d, (w / 2, cy + 98), 'Veamos cómo te fue', fuente(NUNITO, 27), (255, 244, 220))
    return im


def panel_engranajes_hoy(w=540, h=1200):
    """El panel de resultado de Engranajes de hoy (EngranajesGameController.ShowResult de HEAD): la caja azul oscuro de 880 × 760 con el título, el puntaje gigante, el detalle y lo del cohete."""
    im = cielo_viejo(w, h)
    d = ImageDraw.Draw(im)
    k = w / 1080.0
    cx, cy = w / 2, h / 2
    d.rounded_rectangle((cx - 440 * k, cy - 380 * k, cx + 440 * k, cy + 380 * k), int(64 * k), fill=(20, 31, 56))
    centrado(d, (cx, cy - 250 * k - 44 * k), '¡Buen trabajo!', fuente(FONT, int(76 * k)), WHITE)
    centrado(d, (cx, cy - 60 * k - 130 * k), '74', fuente(FONT, int(260 * k)), WHITE)
    centrado(d, (cx, cy + 150 * k - 26 * k), '7 de 10 máquinas arregladas · etapa 3 de 5', fuente(FONT, int(46 * k * 0.8)), (217, 219, 224))
    centrado(d, (cx, cy + 250 * k - 22 * k), 'Faltan 2 luces: tu cohete espera en el hangar', fuente(FONT, int(42 * k * 0.8)), (166, 170, 180))
    return im


# ------------------------------------------------------------------ la lámina
def abre(nombre):
    p = os.path.join(SRC, nombre + '.png')
    return Image.open(p).convert('RGB') if os.path.exists(p) else None


def telefono(im, w):
    """La toma a w de ancho con las esquinas redondas y un filo fino."""
    if im is None:
        im = Image.new('RGB', (540, 1200), (30, 32, 70))
        ImageDraw.Draw(im).text((20, 20), 'falta la toma', fill=WHITE)
    h = int(im.height * w / im.width)
    im = im.resize((w, h), Image.LANCZOS)
    mask = Image.new('L', (w, h), 0)
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, w - 1, h - 1), 22, fill=255)
    out = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    out.paste(im, (0, 0), mask)
    ImageDraw.Draw(out).rounded_rectangle((0, 0, w - 1, h - 1), 22, outline=(60, 64, 120, 255), width=2)
    return out


def main():
    if '--tomar' in sys.argv:
        tomar(); return
    f_t = fuente(FONT, 38); f_h = fuente(FONT, 27); f_s = fuente(NUNITO, 21); f_c = fuente(NUNITO, 18)
    hoy_cortina = cortina_hoy()
    COLS = [('1 · El momento del objeto', 'hoy-panel', 'prop-momento', 'hoy: el panel de resultado de Unity (lo tapaba la cortina)', 'propuesta: la escena apagada y el emblema'),
            ('2 · La cortina «¡Listo!»', None, 'prop-cortina', 'hoy: cielo viejo, la placa sola (réplica)', 'propuesta: el cielo nuevo con el emblema'),
            ('3 · El resultado en la app', 'hoy-app', 'prop-app', 'hoy: puntaje gigante, estrellas, «¡Muy bien!»', 'propuesta: la placa y directo el final con sentido')]
    gap_in, gap_grp, mar = 14, 44, 50
    ph = int(1200 * PW / 540)
    row_h = 70 + 64 + 34 + ph + 70
    Wt = 2 * mar + 6 * PW + 3 * gap_in + 2 * gap_grp
    Ht = 130 + row_h * len(GAMES) + 30
    sheet = Image.new('RGB', (Wt, Ht), BG)
    d = ImageDraw.Draw(sheet)
    d.text((mar, 28), 'Un solo final: hoy | propuesta', font=f_t, fill=SUN)
    d.text((mar, 82), 'Tres juegos, tres cuadros: lo que se ve al terminar una partida. Las tomas son reales; «réplica» = hecha a mano igual que el código anterior (leyenda de cada una).', font=f_s, fill=DIM)
    y = 130
    for g, nombre in GAMES:
        d.text((mar, y + 14), nombre, font=f_h, fill=WHITE)
        x = mar
        for titulo, hoy_key, prop_key, leg_hoy, leg_prop in COLS:
            d.text((x, y + 52), titulo, font=f_s, fill=SUN)
            if hoy_key is None: hoy = hoy_cortina
            elif hoy_key == 'hoy-panel' and g == 'engranajes': hoy = panel_engranajes_hoy()
            else: hoy = abre('%s-%s' % (hoy_key, g))
            prop = abre('%s-%s' % (prop_key, g))
            leg_h = leg_hoy + (' (réplica)' if (hoy_key == 'hoy-panel' and g == 'engranajes') else '')
            sheet.paste(telefono(hoy, PW), (x, y + 80), telefono(hoy, PW))
            sheet.paste(telefono(prop, PW), (x + PW + gap_in, y + 80), telefono(prop, PW))
            for i, leg in enumerate((leg_h, leg_prop)):
                xx = x + i * (PW + gap_in)
                words = leg.split(' '); line = ''; ly = y + 80 + ph + 8
                for wd in words:
                    t = (line + ' ' + wd).strip()
                    if d.textlength(t, font=f_c) > PW: d.text((xx, ly), line, font=f_c, fill=DIM); ly += 22; line = wd
                    else: line = t
                d.text((xx, ly), line, font=f_c, fill=DIM)
            x += 2 * PW + gap_in + gap_grp
        y += row_h
    sheet.save(OUT, optimize=True)
    print('ok', OUT, sheet.size)


if __name__ == '__main__':
    main()
