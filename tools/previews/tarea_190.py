# -*- coding: utf-8 -*-
"""Lámina de la Tarea 190, «La entrada al juego sin cuenta regresiva» (`docs/previews/tarea-190.png`), con CAPTURAS REALES; no son maquetas.
  1. La pantalla de carga: hoy (con «Cómo se juega» y su frase; captura real de Fable) | propuesta (planeta, nombre y etapa; capturas de Roborazzi).
  2. El «¡Ya!» sobre el tablero real, a tamaño real (Tinta o Palabra y Rastro de luz; capturas reales de Unity de 1080 × 2400) y un recorte al 100 %.
  3. La primera partida de un juego con tutorial: carga → tarjeta de Nubi → ronda guiada → «¡Ya!» → juego.
  4. La tabla por juego: tiempo hasta «listo», p95 y máximo de los cuadros visibles, antes y después (PC; `tarea_190_medidas.txt`).
Entradas (fuera de git): `app/src/test/screenshots/tarea-190/propuesta/*.png` (`Tarea190CapturasTest`, solo al grabar) y `unity/test-results/capturas-t190/<juego>/` (`NUBI_SHOTS_DIR` + `NUBI_SHOTS_GAMES="stroop,secuencia"`);
en git: `tarea_190_hoy_carga.png` y `tarea_190_medidas.txt`.  Uso:  python -I tools/previews/tarea_190.py
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
PROP = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots', 'tarea-190', 'propuesta')
SHOTS = os.path.join(ROOT, 'unity', 'test-results', 'capturas-t190')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-190.png')
NAVY, SUN, DIM, CREAM, LILAC, LIME, CORAL = (20, 18, 63), (255, 203, 69), (200, 196, 235), (245, 235, 219), (184, 164, 255), (155, 229, 100), (255, 107, 74)
W = 2300
M = 48


def font(px, kind='fredoka'):
    return ImageFont.truetype(os.path.join(FONTS, {'rubik': 'rubik_bold.ttf', 'fredoka': 'fredoka.ttf', 'nunito': 'nunito_regular.ttf'}[kind]), int(px))


def load(path, h=None, w=None, scale=None):
    im = Image.open(path).convert('RGB')
    if scale: im = im.resize((int(im.width * scale), int(im.height * scale)), Image.LANCZOS)
    elif h: im = im.resize((int(im.width * h / im.height), h), Image.LANCZOS)
    elif w: im = im.resize((w, int(im.height * w / im.width)), Image.LANCZOS)
    return im


def wrap(d, text, f, width):
    lines = []
    for para in text.split('\n'):
        cur = ''
        for word in para.split(' '):
            t = (cur + ' ' + word).strip()
            if d.textlength(t, font=f) <= width: cur = t
            else:
                lines.append(cur); cur = word
        lines.append(cur)
    return lines


medidas = []
for ln in open(os.path.join(HERE, 'tarea_190_medidas.txt'), encoding='utf-8'):
    if ln.startswith('#') or not ln.strip(): continue
    p = [x.strip() for x in ln.split('|')]
    medidas.append((p[0], float(p[1]), float(p[2]), float(p[3]), float(p[4]), float(p[5]), float(p[6]), int(p[7])))

# ---------------------------------------------------------------- piezas
items = []   # (alto, función de dibujo)
H1 = 800


def title(txt, sub=None):
    def draw(d, im, y):
        d.text((M, y), txt, font=font(46, 'rubik'), fill=SUN)
        yy = y + 62
        if sub:
            for ln in wrap(d, sub, font(25, 'nunito'), W - 2 * M):
                d.text((M, yy), ln, font=font(25, 'nunito'), fill=DIM); yy += 34
    n = 0 if not sub else 1
    return (70 + (len(wrap(ImageDraw.Draw(Image.new('RGB', (10, 10))), sub, font(25, 'nunito'), W - 2 * M)) * 34 if sub else 0), draw)


def panels(row, h, gap=34):
    """row = lista de (imagen, rótulo, subrótulo)"""
    def draw(d, im, y):
        x = M
        for img, cap, sub in row:
            im.paste(img, (x, y))
            d.rectangle((x - 1, y - 1, x + img.width, y + img.height), outline=(80, 76, 140), width=2)
            yy = y + img.height + 10
            for ln in wrap(d, cap, font(24, 'fredoka'), img.width):
                d.text((x, yy), ln, font=font(24, 'fredoka'), fill=SUN); yy += 30
            if sub:
                for ln in wrap(d, sub, font(21, 'nunito'), img.width):
                    d.text((x, yy), ln, font=font(21, 'nunito'), fill=DIM); yy += 27
            x += img.width + gap
    return (h + 180, draw)


# 1 · la carga
hoy = load(os.path.join(HERE, 'tarea_190_hoy_carga.png'), h=H1)
p1 = load(os.path.join(PROP, 'carga-secuencia.png'), h=H1)
p2 = load(os.path.join(PROP, 'carga-correo.png'), h=H1)
p3 = load(os.path.join(PROP, 'carga-contrarreloj.png'), h=H1)
items.append(title('1 · La pantalla de carga: hoy | propuesta',
                   'Hoy explica el juego (el rótulo «Cómo se juega» y 2 a 4 líneas) en CADA arranque, y se quita 3 cuadros después de la configuración. Propuesta: solo el planeta, el nombre y la etapa; tapa el juego hasta que el tablero está COMPLETO (también con Unity ya vivo) y se va con un fundido de 250 ms. La explicación queda en la ficha («Sobre este juego») y en la pausa.'))
items.append(panels([(hoy, 'Hoy · Rastro de luz', 'planeta, nombre, etapa y reloj + «Cómo se juega» + la frase + «Preparando el juego…»'),
                     (p1, 'Propuesta · Rastro de luz', 'planeta, nombre y «Etapa Hábil · Con reloj»'),
                     (p2, 'Propuesta · Correo Estelar', 'sin reloj elegido: solo la etapa'),
                     (p3, 'Propuesta · un Contrarreloj', 'el nombre del modo en lugar de la etapa')], H1))

# 2 · el ¡Ya!
def shot(g, n): return os.path.join(SHOTS, g, n)
sc = 0.45
s1, s2 = load(shot('stroop', '01-tablero.png'), scale=sc), load(shot('stroop', '02-ya.png'), scale=sc)
r1, r2 = load(shot('secuencia', '01-tablero.png'), scale=sc), load(shot('secuencia', '02-ya.png'), scale=sc)
items.append(title('2 · El «¡Ya!» sobre el tablero real',
                   'El primer cuadro que se ve ya es el tablero entero; 0,3 s después entra la losa sol de «¡Ya!» (la misma de «¡Listo!» del final, con la marimba de salida), se queda un instante y sale en espejo: 0,8 s en total. No se toca, no tapa el marcador ni la fila de consigna, y el primer ensayo y el reloj empiezan cuando se va. Capturas de 1080 × 2400 (el teléfono de 393 dp a 440 dpi), escala 45 %.'))
items.append(panels([(s1, 'Tinta o Palabra · tablero', 'el primer cuadro visible'), (s2, 'Tinta o Palabra · «¡Ya!»', None), (r1, 'Rastro de luz · tablero', None), (r2, 'Rastro de luz · «¡Ya!»', 'tapa un instante dos luceros; todavía no hay estímulo')], int(2400 * sc)))

crop = Image.open(shot('stroop', '02-ya.png')).convert('RGB').crop((140, 1080, 940, 1380))
items.append(title('El «¡Ya!» al 100 % (los píxeles reales del teléfono)'))
items.append((crop.height + 40, lambda d, im, y: im.paste(crop, (M, y))))

# 3 · la primera partida con tutorial
sc3 = 0.27
q = [(p1.resize((int(1080 * sc3), int(2400 * sc3)), Image.LANCZOS), '1 · La carga', 'sin explicación'),
     (load(shot('secuencia', '15-tutorial-1.png'), scale=sc3), '2 · La tarjeta de Nubi', 'el tutorial: UNA sola vez por juego'),
     (load(shot('secuencia', '16-tutorial-3.png'), scale=sc3), '3 · La ronda guiada', '«Práctica (sin puntos)»'),
     (load(shot('secuencia', '02-ya.png'), scale=sc3), '4 · «¡Ya!»', 'el tablero ya está; empieza el juego'),
     (load(shot('secuencia', '04-t08.png'), scale=sc3), '5 · La partida', 'la consigna vive en la fila del marcador')]
items.append(title('3 · La primera partida de un juego con tutorial (Rastro de luz)',
                   'Portada, carga sin explicación, tarjeta del tutorial, ronda guiada, «¡Ya!» y juego. En las partidas siguientes: Portada, carga, «¡Ya!» y juego. La consigna se lee en un solo lugar (la fila del marcador). Las tarjetas «NUEVO» y el pre-roll de Freno NO se tocaron: van en la Tarea 191.'))
items.append(panels(q, int(2400 * sc3), gap=44))

# 4 · la tabla
cols = ['Juego', 'Antes: trabajo de entrada', 'Antes: cuadros de la cuenta (p95 / máx)', 'Después: «listo»', 'Después: cuadros visibles (p95 / máx)', 'Cuadros > 34 ms']
xs = [M, 520, 920, 1420, 1720, 2170]
ROW = 38


def table(d, im, y):
    d.text((M, y), '4 · La entrada, juego por juego (PC, Editor sin gráficos, 17 de 17 juegos)', font=font(46, 'rubik'), fill=SUN)
    y += 74
    for x, c in zip(xs, cols):
        for k, ln in enumerate(wrap(d, c, font(21, 'fredoka'), (xs[xs.index(x) + 1] - x - 12) if xs.index(x) + 1 < len(xs) else 120)):
            d.text((x, y + 28 * k), ln, font=font(21, 'fredoka'), fill=LILAC)
    y += 86
    for i, (n, aw, ap, am, dl, dp, dm, over) in enumerate(medidas):
        if i % 2 == 0: d.rectangle((M - 10, y - 4, W - M + 10, y + ROW - 6), fill=(28, 26, 78))
        d.text((xs[0], y), n, font=font(24, 'fredoka'), fill=CREAM)
        d.text((xs[1], y), '%.1f s' % aw, font=font(24, 'nunito'), fill=DIM)
        d.text((xs[2], y), '%.0f / %.0f ms' % (ap, am), font=font(24, 'nunito'), fill=CORAL if am > 100 else DIM)
        d.text((xs[3], y), '%.2f s' % dl, font=font(24, 'fredoka'), fill=LIME if dl <= 1.5 else CORAL)
        d.text((xs[4], y), '%.1f / %.1f ms' % (dp, dm), font=font(24, 'fredoka'), fill=LIME if dm <= 34 and dp <= 20 else CORAL)
        d.text((xs[5], y), str(over), font=font(24, 'fredoka'), fill=LIME if over == 0 else CORAL)
        y += ROW
    y += 14
    notes = ['Metas de Opus (PC): «listo» en 1,5 s o menos desde la configuración; de «listo» a los 5 s de partida (con el «¡Ya!») ningún cuadro de más de 34 ms y p95 de 20 ms o menos; ni horneo ni síntesis en el hilo principal en ese tramo (la sonda: «tras “listo”: 0 ms» en los 17).',
             'El único que no llega: Engranajes, UN cuadro de 36 ms a 1,15 s de «listo» (2 ms sobre la meta). Cae justo cuando termina el «¡Ya!» y empieza el primer ensayo: `ShowMachine` arma la primera máquina (los engranajes y las correas) en el hilo principal. Arreglarlo exige armarla antes del «¡Ya!»; se deja anotado para mirarlo en el teléfono.',
             'Antes = lo que corría hasta el primer cuadro con el tablero (primer cuadro + trozos de sprites + cálculo de sonido) y los cuadros que se veían durante la cuenta (Tarea 189, `docs/cuenta-y-tutoriales-diagnostico.md`). Después = la sonda `EntryProbe` (`[sonda-entrada]`, solo con la bandera de depuración): «listo» desde que llega la configuración, y los cuadros VISIBLES de «listo» a +5 s.']
    for t in notes:
        for ln in wrap(d, t, font(22, 'nunito'), W - 2 * M):
            d.text((M, y), ln, font=font(22, 'nunito'), fill=DIM); y += 30
        y += 8


items.append((74 + 86 + ROW * len(medidas) + 14 + 3 * 8 + 30 * 12, table))

H = M + sum(h for h, _ in items) + 70 * 0 + M
img = Image.new('RGB', (W, H), NAVY)
d = ImageDraw.Draw(img)
d.text((M, 24), 'Tarea 190 · La entrada al juego sin cuenta regresiva: hoy | propuesta', font=font(58, 'rubik'), fill=CREAM)
y = M + 60
for h, fn in items:
    fn(d, img, y)
    y += h
img = img.crop((0, 0, W, min(H, y + M)))
os.makedirs(os.path.dirname(OUT), exist_ok=True)
img.save(OUT, optimize=True)
print('ok', OUT, img.size)
