# -*- coding: utf-8 -*-
"""Lámina de las piezas de la isla por medallas (Tarea 134, A1 de `auditoria-fable/spec-adopcion-A1-A7.md`): las 10 piezas sueltas, la isla de hoy, la isla con las 3 primeras (C1, V1, Vo1) y la isla completa con las 10.
Lee los renders de Blender (`tools/isla/out/<carpeta>/`: `isla.py --layers --json` y `isla.py --deco --deco-completa --deco-lamina`) y los WebP que deja `componer.py` (`<carpeta>/app/`), copia a
`docs/previews/isla-piezas/` las 10 capas (WebP con alfa) y `isla-deco.json` (ancla y capa de cada una) y arma `docs/previews/isla-piezas.png`.
Los paneles de la isla son los que dibujará la app (capas apiladas: base, piezas de suelo, edificios, piezas de aire, Nubi) a 2,2 px por dp, sobre el fondo; el de las 10 es el render REAL de Blender (con las sombras entre piezas).
Mide además, sobre los WebP: el tamaño de cada pieza en dp (isla de 390 dp de ancho), que no pise edificios, zonas tocables (48 dp en torno de cada lugar), farolas de la ruta ni la caja de Nubi, y el peso de las diez capas (≤ 200 KB).
Uso:  python -I tools/previews/isla_piezas.py [carpeta_de_renders]      (por defecto tools/isla/out/t134)
"""
import os, sys, json
import numpy as np
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SRC = os.path.abspath(sys.argv[1]) if len(sys.argv) > 1 else os.path.join(ROOT, 'tools', 'isla', 'out', 't134')
APP = os.path.join(SRC, 'app')
DST = os.path.join(ROOT, 'docs', 'previews', 'isla-piezas'); OUT = os.path.join(ROOT, 'docs', 'previews', 'isla-piezas.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font'); FONDO = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'drawable-nodpi', 'fondo_lamina.webp')
S = 2.2                                                  # px por dp de los paneles de la isla
DP_ISLA = 390.0                                          # ancho de la isla en una pantalla de 393 dp
INK, SUN, DIM, CREAM, NAVY = (33, 28, 69), (255, 203, 69), (200, 196, 235), (245, 235, 219), (20, 18, 63)
TARJ_AIRE = (38, 34, 98)                                  # fondo de las tarjetas de las piezas de aire (un poco más claro que la hoja)
SLAB = (241, 223, 191)
IDS = ('c1', 'c2', 'c3', 'c4', 'v1', 'v2', 'v3', 'vo1', 'vo2', 'vo3')
INFO = {   # id: (nombre, familia y nivel con el que aparece)
    'c1': ('dos arbustos', 'Constancia · nivel 2 «Una semana»'), 'c2': ('flores en el jardín', 'Constancia · nivel 4 «Dos semanas»'), 'c3': ('un árbol', 'Constancia · nivel 5 «Un mes entero»'),
    'c4': ('frutos en el árbol', 'Constancia · nivel 7 «Imparable»'), 'v1': ('banderines Antena–Biblioteca', 'Variedad · nivel 2 «Mente completa»'), 'v2': ('banderines sobre el Observatorio', 'Variedad · nivel 3 «Explorador»'),
    'v3': ('un banderín en cada techo', 'Variedad · nivel 4 «Explorador total»'), 'vo1': ('un muelle', 'Volver · nivel 1 «Volviste»'), 'vo2': ('una nave amarrada', 'Volver · nivel 2 «Siempre vuelves»'),
    'vo3': ('el farol del muelle', 'Volver · nivel 3 «Vuelves una y otra vez»'),
}
TOPE_AIRE_TOCABLE = 15.0      # las piezas de aire nacen de los edificios (cuerdas de la lámpara de la Antena, astas en los techos): rozan el borde de su zona tocable; se acepta hasta este % del área de una zona (las capas no tienen toque: no tapan el botón)
DEBAJO = {'c2': 'C1', 'c4': 'C3', 'vo2': 'Vo1', 'vo3': 'Vo1'}          # piezas que en el primer plano se ven CON la de abajo (la capa de la app va aparte)
MIN_DP = {'c1': 34, 'c2': 40, 'c3': 34, 'c4': 0, 'v1': 0, 'v2': 0, 'v3': 0, 'vo1': 22, 'vo2': 28, 'vo3': 0}      # lado menor mínimo de la spec (los detalles sobre una pieza ya visible no se miden solos)


def font(px, bold=True): return ImageFont.truetype(os.path.join(FONTS, 'fredoka.ttf' if bold else 'nunito_regular.ttf'), int(round(px)))
def abre(p): return Image.open(p).convert('RGBA')
def escala(im, k): return im.resize((max(1, round(im.width * k)), max(1, round(im.height * k))), Image.LANCZOS)


def limpia(im):
    """Igual que `componer.py`: sin la bruma del «shadow catcher» y con la sombra de contacto teñida y suave (para los primeros planos, que no pasan por componer)."""
    a = np.asarray(im).astype(float).copy(); a[..., 3] = np.clip((a[..., 3] - 12.0) * 255.0 / 243.0, 0, 255)
    sh = (a[..., 3] < 252) & (a[..., :3].max(axis=2) < 48); a[..., 3] = np.where(sh, a[..., 3] * 0.6, a[..., 3]); a[..., :3] = np.where(sh[..., None], np.array((62, 42, 92), float), a[..., :3])
    return Image.fromarray(a.astype(np.uint8), 'RGBA')


def capa(i):
    p = os.path.join(APP, 'isla_deco_' + i + '.webp'); return abre(p) if os.path.exists(p) else None


def compone(ids, nubi=True):
    """Las capas apiladas como las dibuja la app: base, suelo (en el orden de IDS), edificios, aire y Nubi."""
    base = abre(os.path.join(APP, 'isla_base.webp')); lienzo = Image.new('RGBA', base.size, (0, 0, 0, 0)); lienzo.alpha_composite(base)
    for i in IDS:
        if i in ids and DECO[i]['capa'] == 'suelo' and capa(i): lienzo.alpha_composite(capa(i))
    lienzo.alpha_composite(abre(os.path.join(APP, 'isla_edificios.webp')))
    for i in IDS:
        if i in ids and DECO[i]['capa'] == 'aire' and capa(i): lienzo.alpha_composite(capa(i))
    if nubi:
        n = abre(os.path.join(APP, 'nubi_hola.webp')); (x0, y0), (x1, y1) = JSON['capa_nubi']['hola']['caja']; W, H = lienzo.size
        lienzo.alpha_composite(n.resize((round((x1 - x0) * W), round((y1 - y0) * H)), Image.LANCZOS), (round(x0 * W), round(y0 * H)))
    return lienzo


def fondo(w, h):
    f = abre(FONDO) if os.path.exists(FONDO) else Image.new('RGBA', (w, h), NAVY + (255,)); k = max(w / f.width, h / f.height); f = f.resize((round(f.width * k), round(f.height * k)), Image.LANCZOS)
    return f.crop((0, 0, w, h))


def panel(img, titulo, sub):
    pw, ph = round(393 * S), round(520 * S); p = fondo(pw, ph).convert('RGBA'); k = (DP_ISLA * S) / img.width; im = escala(img, k)
    p.alpha_composite(im, ((pw - im.width) // 2, round(70 * S) + 24))
    d = ImageDraw.Draw(p); d.text((18, 14), titulo, font=font(17 * S / 2.2 * 1.0, True), fill=SUN); d.text((18, 14 + 26), sub, font=font(13.5 * S / 2.2, False), fill=DIM)
    return p


def mide(im, umbral=128):
    a = np.asarray(im.getchannel('A')) >= umbral; ys, xs = np.where(a)
    if len(xs) == 0: return None
    return xs.min(), ys.min(), xs.max() + 1, ys.max() + 1


JSON = json.load(open(os.path.join(APP, 'isla.json'), encoding='utf-8')); DECO = JSON.get('decoraciones', {})
W0, H0 = abre(os.path.join(APP, 'isla_base.webp')).size
PXDP = W0 / DP_ISLA                                                    # px del recorte por dp
presentes = [i for i in IDS if capa(i)]
print('capas presentes:', presentes)

# ---------------------------------------------------------------------------------------------- mediciones sobre los WebP
base_alpha = np.asarray(abre(os.path.join(APP, 'isla_edificios.webp')).getchannel('A')) >= 128
yy, xx = np.mgrid[0:H0, 0:W0]
def zona_dp(cx, cy, mitad_dp): return (np.abs(xx - cx * W0) <= mitad_dp * PXDP) & (np.abs(yy - cy * H0) <= mitad_dp * PXDP)
tocables = np.zeros((H0, W0), bool); zonas = {}
for nm, l in JSON['lugares'].items(): zonas[nm] = zona_dp(l['centro'][0], l['centro'][1], 24); tocables |= zonas[nm]
farolas = np.zeros((H0, W0), bool)
for f in JSON['lamparas']: farolas |= (np.hypot(xx - f[0] * W0, yy - f[1] * H0) <= 14 * PXDP)
(nx0, ny0), (nx1, ny1) = JSON['capa_nubi']['hola']['caja']
_n = np.asarray(abre(os.path.join(APP, 'nubi_hola.webp')).resize((round((nx1 - nx0) * W0), round((ny1 - ny0) * H0))))[..., 3] > 16
nubi_caja = np.zeros((H0, W0), bool); nubi_caja[round(ny0 * H0):round(ny0 * H0) + _n.shape[0], round(nx0 * W0):round(nx0 * W0) + _n.shape[1]] = _n
_r = int(3 * PXDP); _d = nubi_caja.copy()                                  # la silueta REAL de Nubi con 3 dp de margen (no su caja: la nube tiene esquinas vacías)
for _dy in range(-_r, _r + 1, 2):
    for _dx in range(-_r, _r + 1, 2):
        if _dy * _dy + _dx * _dx <= _r * _r: _d |= np.roll(np.roll(nubi_caja, _dy, 0), _dx, 1)
nubi_caja = _d
filas = []; problemas = []
for i in presentes:
    im = capa(i); m = mide(im, 128); kb = os.path.getsize(os.path.join(APP, 'isla_deco_' + i + '.webp')) / 1024.0
    a = np.asarray(im.getchannel('A')) >= 128
    sobre_ed = int((a & base_alpha).sum()) if DECO[i]['capa'] == 'suelo' else 0           # el aire va ENCIMA de los edificios: ahí sí se puede cruzar
    sobre_toc = int((a & tocables).sum()); pct_toc = max(((a & z).sum() / z.sum() * 100.0 for z in zonas.values()), default=0.0); sobre_far = int((a & farolas).sum()); sobre_nubi = int((a & nubi_caja).sum())
    w = (m[2] - m[0]) / PXDP; h = (m[3] - m[1]) / PXDP
    filas.append((i, w, h, kb, sobre_ed, sobre_toc, sobre_far, sobre_nubi, pct_toc))
    if sobre_ed: problemas.append(f'{i}: {sobre_ed} px sobre un edificio')
    if sobre_toc and (DECO[i]['capa'] == 'suelo' or pct_toc > TOPE_AIRE_TOCABLE): problemas.append(f'{i}: {pct_toc:.0f} % de una zona tocable cubierto')
    if sobre_far: problemas.append(f'{i}: {sobre_far} px sobre una farola de la ruta')
    if sobre_nubi: problemas.append(f'{i}: {sobre_nubi} px sobre la silueta de Nubi (con 3 dp de margen)')
    if min(w, h) < MIN_DP[i]: problemas.append(f'{i}: lado menor {min(w, h):.0f} dp < {MIN_DP[i]}')
total_kb = sum(f[3] for f in filas)
print('pieza   ancho x alto (dp)   peso   sobre-edif  zona tocable (%)  farola  Nubi')
for f in filas: print(f'{f[0]:4s}   {f[1]:5.0f} x {f[2]:4.0f}        {f[3]:5.1f} KB  {f[4]:6d}  {f[8]:10.1f} %  {f[6]:6d}  {f[7]:6d}')
print(f'peso de las {len(filas)} capas: {total_kb:.0f} KB' + ('' if total_kb <= 200 else '  ¡PASA DE 200 KB!'))
print('PROBLEMAS:' if problemas else 'sin problemas de superposición ni de tamaño', *problemas, sep='\n  ')

# ---------------------------------------------------------------------------------------------- copias al repo
os.makedirs(DST, exist_ok=True)
for i in presentes: capa(i).save(os.path.join(DST, 'isla_deco_' + i + '.webp'), 'WEBP', quality=88, alpha_quality=100, method=6)
json.dump({'nota': 'Piezas de la isla por medallas (Tarea 134): capas WebP con el MISMO recorte que isla_base e isla_edificios (930 x 1042). `capa` = suelo (entre la base y los edificios) o aire (encima de los edificios); `ancla` = donde la pieza toca la isla, en 0-1 del recorte. NO están en la app todavía (tarea T-D2).',
           'recorte_px': [W0, H0], 'decoraciones': DECO}, open(os.path.join(DST, 'isla-deco.json'), 'w', encoding='utf-8'), ensure_ascii=False, indent=1)

# ---------------------------------------------------------------------------------------------- la lámina
CW, CH = 560, 440                                                        # tarjeta de cada pieza suelta
cards = []
for i in IDS:
    f = os.path.join(SRC, 'lamina-deco-' + i + '.png')
    if not os.path.exists(f): continue
    im = limpia(abre(f)); b = mide(im, 30)
    if b: im = im.crop((max(0, b[0] - 14), max(0, b[1] - 14), min(im.width, b[2] + 14), min(im.height, b[3] + 14)))
    aire = DECO.get(i, {}).get('capa') == 'aire'
    c = Image.new('RGBA', (CW, CH), (TARJ_AIRE if aire else SLAB) + (255,))
    k = min((CW - 40) / im.width, (CH - 130) / im.height, 1.0); k = k if k < 1.0 else 1.0
    ime = escala(im, k); c.alpha_composite(ime, ((CW - ime.width) // 2, 16 + (CH - 130 - ime.height) // 2))
    d = ImageDraw.Draw(c); nm, fam = INFO[i]; fg = CREAM if aire else INK
    d.text((16, CH - 108), f'{i.upper()} · {nm}', font=font(25), fill=fg); d.text((16, CH - 72), fam, font=font(19, False), fill=(DIM if aire else (90, 80, 120)))
    fr = next((r for r in filas if r[0] == i), None)
    if fr: d.text((16, CH - 42), f'mide {fr[1]:.0f} × {fr[2]:.0f} dp en la isla · {fr[3]:.0f} KB' + (f' · se ve con {DEBAJO[i]}' if i in DEBAJO else ''), font=font(19, False), fill=(DIM if aire else (90, 80, 120)))
    cards.append(c)

tresp = [i for i in ('c1', 'v1', 'vo1') if i in presentes]
paneles = [panel(compone([]), 'Hoy: sin piezas', 'la isla como está en la app'),
           panel(compone(tresp), 'Al empezar: 3 piezas', 'arbustos · una cuerda de banderines · el muelle')]
real = os.path.join(SRC, 'isla-completa-deco.png')
if os.path.exists(real):
    r = abre(real); r = r.crop((225, 220, 225 + 930, 220 + 1042)); paneles.append(panel(r, 'Con las 10 piezas', 'render real de Blender, con sombras entre piezas'))
    r.save(os.path.join(DST, 'isla-completa-deco.webp'), 'WEBP', quality=88, alpha_quality=100, method=6)       # el render real con las diez, con el recorte común
paneles.append(panel(compone(presentes), 'Con las 10: lo que dibuja la app', 'capas apiladas (comprobación de que coinciden)'))

GAP = 24; cols = 5; rows = (len(cards) + cols - 1) // cols
w_sheet = max(cols * CW + (cols + 1) * GAP, len(paneles) * paneles[0].width + (len(paneles) + 1) * GAP)
h_cards = rows * CH + (rows + 1) * GAP; h_pan = paneles[0].height + GAP; h_head = 120; h_foot = 70 + 34 * (len(problemas) + 3)
sheet = Image.new('RGBA', (w_sheet, h_head + h_cards + h_pan + h_foot), NAVY + (255,)); d = ImageDraw.Draw(sheet)
d.text((GAP, 22), 'La isla crece: las 10 piezas por medallas (Tarea 134)', font=font(46), fill=SUN)
d.text((GAP, 78), 'Constancia (jardín), Variedad (banderines) y Volver (muelle). Piezas sueltas arriba; la isla en tamaño de teléfono (393 dp) abajo. Aún NO están en la app.', font=font(24, False), fill=DIM)
for n, c in enumerate(cards):
    r, k = divmod(n, cols); x = GAP + k * (CW + GAP)
    if len(cards) - r * cols < cols and r == rows - 1: x = GAP + k * (CW + GAP)
    sheet.alpha_composite(c, (x, h_head + GAP + r * (CH + GAP)))
y = h_head + h_cards
for n, p in enumerate(paneles): sheet.alpha_composite(p, (GAP + n * (p.width + GAP), y))
y += h_pan + 8
d.text((GAP, y), f'Comprobaciones sobre las capas: peso de las {len(filas)} capas {total_kb:.0f} KB (tope 200 KB). ' + ('Sin superposiciones con edificios, farolas de la ruta ni la silueta de Nubi; las cuerdas y astas solo rozan el borde de las zonas tocables (48 dp), a lo más %.0f %% de una zona.' % max([f[8] for f in filas] or [0]) if not problemas else 'A REVISAR:'), font=font(24, False), fill=CREAM)
for n, t in enumerate(problemas): d.text((GAP + 24, y + 38 + 34 * n), '· ' + t, font=font(22, False), fill=(255, 140, 120))
sheet.convert('RGB').save(OUT, optimize=True); print('lámina:', OUT, sheet.size)
