# -*- coding: utf-8 -*-
"""Hoja «hoy | propuesta» de la Tarea 148 (21-oct-2026; `auditoria-fable/propuestas-diseno-visual.md`: P1 Nubi viva, P3 una sola sombra y el tamaño de Nubi en los tutoriales, P4 lo que quedaba de otra época y P9 la cuenta regresiva con el mismo kit).
CAPTURAS REALES: las de la app salen de Roborazzi (`AppActualScreenshotTest`, `AchievementOverlayScreenshotTest` y `NubiIdleScreenshotTest`; «HOY» = el commit `4097255`, la Tarea 153, por `git show`) y las de Unity de
`bash tools/verificar-todo.sh --capturas Stroop,Bodega,Meteoros` (el «HOY» de Unity son las tomas del último recorrido, guardadas aparte en la carpeta que se pasa como `--hoy-unity`).
La gota-llama de la racha es una COMPOSICIÓN: el render real de Blender (`kit_avance.py`, `llama_encendida` y `llama_apagada`) pegado sobre las capturas de hoy; todavía NO está en la app (se integra con el OK de la hoja).
Uso:  python -I tools/previews/tarea_148.py [--hoy-unity CARPETA]      → docs/previews/tarea-148.png
"""
import glob, io, os, subprocess, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
PREV = os.path.join(ROOT, 'docs', 'previews'); OUT = os.path.join(PREV, 'tarea-148.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
SHOTS = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots')
UNITY_SHOTS = os.path.join(ROOT, 'unity', 'test-results', 'capturas')
LLAMAS = os.path.join(ROOT, 'tools', 'isla', 'out', 'kit-avance-llama')
BEFORE_COMMIT = '4097255'
args = sys.argv[1:]
HOY_UNITY = args[args.index('--hoy-unity') + 1] if '--hoy-unity' in args else os.path.join(os.environ.get('TEMP', '.'), 'capturas-hoy')
NAVY, SUN, DIM, CREAM, LIME = (20, 18, 63), (255, 203, 69), (200, 196, 235), (245, 235, 219), (155, 229, 100)
MARGIN, GAP = 40, 14


def font(px, bold=True): return ImageFont.truetype(os.path.join(FONTS, 'fredoka.ttf' if bold else 'nunito_regular.ttf'), int(px))


def hoy_app(name):
    raw = subprocess.run(['git', '-C', ROOT, 'show', '%s:docs/previews/app-actual/%s.png' % (BEFORE_COMMIT, name)], capture_output=True, check=True).stdout
    return Image.open(io.BytesIO(raw)).convert('RGB')


def nuevo_app(name): return Image.open(os.path.join(PREV, 'app-actual', name + '.png')).convert('RGB')


def unity(base, game, suffix):
    """La toma `NN-<suffix>.png` MÁS NUEVA de la carpeta de ese juego (las numeradas siguen el orden del recorrido)."""
    files = sorted(glob.glob(os.path.join(base, game, '*-%s.png' % suffix)))
    return Image.open(files[-1]).convert('RGB') if files else None


def unity_first(base, game, suffix):
    files = sorted(glob.glob(os.path.join(base, game, '*-%s.png' % suffix)))
    return Image.open(files[0]).convert('RGB') if files else None


def fit(im, w, crop=None):
    if crop: im = im.crop(crop)
    return im.resize((w, round(im.height * w / im.width)), Image.LANCZOS)


def wrap(d, text, fnt, width):
    lines, cur = [], ''
    for word in text.split(' '):
        t = (cur + ' ' + word).strip()
        if d.textlength(t, font=fnt) <= width: cur = t
        else: lines.append(cur); cur = word
    if cur: lines.append(cur)
    return lines


class Sheet:
    def __init__(self, width):
        self.width = width; self.items = []; self.probe = ImageDraw.Draw(Image.new('RGB', (10, 10)))

    def section(self, title, sub):
        self.items.append(('section', title, sub))

    def row(self, tiles, caption=None):
        """tiles = [(imagen, rótulo, color del rótulo)], todas del mismo ancho (el que traen)."""
        self.items.append(('row', tiles, caption))

    def render(self):
        SUBF, TAGF, TITF = font(21, False), font(22), font(36)
        heights = []
        for it in self.items:
            if it[0] == 'section': heights.append(70 + 28 * len(wrap(self.probe, it[2], SUBF, self.width - 2 * MARGIN)) + 10)
            else:
                tiles, cap = it[1], it[2]
                heights.append(max(t[0].height for t in tiles) + 34 + (28 * len(wrap(self.probe, cap, SUBF, self.width - 2 * MARGIN)) + 8 if cap else 0) + 26)
        H = 130 + sum(heights) + 20
        sheet = Image.new('RGB', (self.width, H), NAVY); d = ImageDraw.Draw(sheet)
        d.text((MARGIN, 24), 'Nubi viva y lo que quedaba de otra época (Tarea 148): hoy | propuesta', font=font(52), fill=SUN)
        d.text((MARGIN, 92), 'Capturas reales de Roborazzi (app, 393 dp) y de Unity (juegos, 1080 × 2400). HOY = el commit %s (Tarea 153); PROPUESTA = lo de ahora.' % BEFORE_COMMIT, font=font(21, False), fill=DIM)
        y = 130
        for it, h in zip(self.items, heights):
            if it[0] == 'section':
                d.text((MARGIN, y + 12), it[1], font=TITF, fill=CREAM)
                for i, line in enumerate(wrap(d, it[2], SUBF, self.width - 2 * MARGIN)): d.text((MARGIN, y + 62 + 28 * i), line, font=SUBF, fill=DIM)
            else:
                tiles, cap = it[1], it[2]; x = MARGIN
                for im, tag, col in tiles:
                    sheet.paste(im, (x, y)); d.rectangle((x, y, x + im.width - 1, y + im.height - 1), outline=(70, 74, 130), width=1)
                    d.text((x, y + im.height + 4), tag, font=TAGF, fill=col); x += im.width + GAP
                if cap:
                    top = y + max(t[0].height for t in tiles) + 34
                    for i, line in enumerate(wrap(d, cap, SUBF, self.width - 2 * MARGIN)): d.text((MARGIN, top + 28 * i), line, font=SUBF, fill=DIM)
            y += h
        return sheet


def pega_llama(base, caja, centro_dp, tam_dp, encendida, px_dp=3.0, muestra=None):
    """Borra el ícono de Material (un parche liso del color del fondo de `caja`) y pega la gota de plasticina (el render real) centrada en `centro` (px de la captura)."""
    im = base.copy(); x0, y0, x1, y1 = caja
    fondo = np.asarray(im.crop(muestra or (x0 - 6, y0, x0 - 2, y1))).reshape(-1, 3).mean(axis=0).astype(int)      # el fondo justo a la izquierda del ícono (o de la zona de muestra)
    ImageDraw.Draw(im).rectangle(caja, fill=tuple(int(v) for v in fondo))
    llama = Image.open(os.path.join(LLAMAS, 'kit_pz_llama_%s.png' % ('encendida' if encendida else 'apagada'))).convert('RGBA')
    lado = round(tam_dp * px_dp * (32.0 / 26.0))                         # la pieza es de 32 dp con la gota de 19 × 26 dp: «tam_dp» es el ALTO de la gota, su lienzo mide 32/26 de eso
    llama = llama.resize((lado, lado), Image.LANCZOS)
    im.paste(llama, (round(centro_dp[0] - lado / 2), round(centro_dp[1] - lado / 2)), llama)
    return im


def pastilla_crema(im, caja):
    """Aproximación para la hoja: la pastilla sol de Avance pasa a crema (se baja la saturación de los píxeles amarillos de la caja; las sombras y el brillo de la plasticina quedan)."""
    im = im.copy(); parte = im.crop(caja).convert('HSV'); a = np.asarray(parte).astype(float)
    amarillo = (a[..., 0] > 20) & (a[..., 0] < 50) & (a[..., 1] > 90)
    a[..., 1] = np.where(amarillo, a[..., 1] * 0.22, a[..., 1]); a[..., 2] = np.where(amarillo, np.minimum(255, a[..., 2] * 1.03), a[..., 2])
    im.paste(Image.fromarray(a.astype('uint8'), 'HSV').convert('RGB'), caja[:2]); return im


def main():
    W = 6 * 300 + 5 * GAP                                      # ancho de la hoja (seis tomas de teléfono de 300 px)
    S = Sheet(W + 2 * MARGIN)
    TW = 300

    # ---------------------------------------------------------------- P1: Nubi viva
    frames = [Image.open(os.path.join(SHOTS, 'nubi-viva-%s.png' % n)).convert('RGB') for n in ('1-reposo', '2-arriba', '3-ojos-cerrados', '4-de-vuelta')]
    crop = (210, 330, 970, 900)
    tiles = [(fit(f, 440, crop), tag, col) for f, (tag, col) in zip(frames, (('1 · reposo', DIM), ('2 · flota +6 dp: la sombra se achica', LIME), ('3 · parpadea (ojos cerrados)', LIME), ('4 · y los abre otra vez', DIM)))]
    S.section('P1 · Nubi viva (renders reales de Blender, la misma cámara y luz de siempre)',
              'Nubi flota (±6 dp en 4,5 s) y parpadea (120 ms, cada 4 a 7 s, doble cada cinco). Es decorativo: no depende de cómo juegas. Mira un lugar o salta = quieta; con «quitar animaciones» = quieta y con los ojos abiertos. '
              'Cuatro cuadros de la misma Nubi (la grande, 200 dp) con el reloj de la prueba a mano. En Unity, los tutoriales hacen lo mismo con la imagen «descansa» del atlas.')
    S.row(tiles)

    # ---------------------------------------------------------------- P3: una sola sombra, el tamaño en los tutoriales
    S.section('P3 · Una sola sombra para Nubi, la de la isla',
              'Antes `NubiClay` tenía una sombra propia, más chica y más oscura que la de la isla. Ahora es la MISMA (tinta, forma y opacidad); en Hoy, la de la isla se achica y se aclara al subir. Nubi de frente a 1080 px (antes 720).')
    pares = [('app-actual/hoy', 'hoy', (420, 500, 880, 900), 'Hoy · la isla'), ('app-actual/primer-vuelo-1', 'primer-vuelo-1', (180, 560, 960, 1080), 'Primer vuelo · L'), ('app-actual/juegos', 'juegos', (0, 240, 560, 620), 'Juegos · M')]
    tiles = []
    for _, name, crop, title in pares:
        tiles.append((fit(hoy_app(name), 440, crop), 'HOY · ' + title, DIM)); tiles.append((fit(nuevo_app(name), 440, crop), 'PROPUESTA', LIME))
    S.row(tiles[:4])
    S.row(tiles[4:])

    # ---------------------------------------------------------------- P4: lo que quedaba de otra época
    S.section('P4 · Lo que quedaba de otra época (sin el escudo de liga ni los discos de la sesión: no van en esta tarea)',
              'La celebración de la medalla sin el abanico de rayos vectoriales que giraba ni los destellos con puntas: un halo sol suave y seis luceros redondos que suben (en la captura, con «quitar animaciones», solo el halo); el título en sol con el contorno de los títulos de las pestañas. '
              'Los tildes de Material pasan al tilde del kit (aquí, «Hecho» de la ruta). Las tres cifras del Perfil van en crema.')
    cel = ('app-actual/medalla-con-pieza', 'medalla-con-pieza', (60, 250, 1020, 1330))
    hecho = ('app-actual/hoy-isla-10', 'hoy-isla-10', (30, 1490, 400, 1780))
    cifras = ('app-actual/perfil', 'perfil', (60, 1090, 1020, 1260))
    tiles = [(fit(hoy_app(cel[1]), 360, cel[2]), 'HOY · celebración', DIM), (fit(nuevo_app(cel[1]), 360, cel[2]), 'PROPUESTA', LIME),
             (fit(hoy_app(hecho[1]), 360, hecho[2]), 'HOY · «Hecho»', DIM), (fit(nuevo_app(hecho[1]), 360, hecho[2]), 'PROPUESTA · tilde del kit', LIME)]
    S.row(tiles)
    S.row([(fit(hoy_app(cifras[1]), 560, cifras[2]), 'HOY · Perfil: sol, cielo y lima', DIM), (fit(nuevo_app(cifras[1]), 560, cifras[2]), 'PROPUESTA · las tres en crema', LIME)])

    # ---------------------------------------------------------------- la gota-llama (propuesta: composición con el render real)
    S.section('La llama de la racha: una GOTA de plasticina en vez del ícono de Material (propuesta, sin integrar)',
              'Hoy el ícono `Whatshot` de Material (una llama con varias puntas: la regla de símbolos pide gotas y luceros, nada con puntas) aparece en Hoy, Avance, el Perfil y el resumen de la ruta. La gota es un símbolo aprobado (`docs/simbolos-neutros.md`): '
              'encendida = sol con una gota clara por dentro; apagada = crema lisa (racha en 0). Es el render real de Blender (`kit_avance.py`) pegado sobre las capturas de hoy; si la apruebas, se integra con `KitGlyph`-like (`kit_llama_on` y `kit_llama_off`) y una prueba que impide volver a un `Icons.` de Material en `ui/`.')
    hoyimg = hoy_app('hoy-isla-10'); perfimg = hoy_app('perfil'); perfnuevo = nuevo_app('perfil')
    fila = (300, 355, 1080, 455)                                       # la fila de las farolas y «7 días»
    a = pega_llama(hoyimg, (782, 388, 836, 436), (803, 412), 16, True)
    a2 = pega_llama(hoyimg, (782, 388, 836, 436), (803, 412), 16, False)
    b = pega_llama(perfnuevo, (166, 1100, 226, 1164), (190, 1131), 21, True)
    S.row([(fit(hoyimg, 920, fila), 'HOY · Hoy: el ícono Whatshot de Material', DIM), (fit(a, 920, fila), 'PROPUESTA · gota encendida (sol)', LIME)])
    S.row([(fit(a2, 920, fila), 'ALTERNATIVA · gota crema también en Hoy (un solo sol, P7)', LIME)])
    S.row([(fit(perfimg, 920, (60, 1090, 1020, 1200)), 'HOY · Perfil (Whatshot en sol)', DIM), (fit(b, 920, (60, 1090, 1020, 1200)), 'PROPUESTA · cifras en crema y gota encendida', LIME)])
    avimg = hoy_app('avance')
    pastilla = (60, 735, 480, 845)
    c = pega_llama(pastilla_crema(avimg, pastilla), (92, 760, 156, 824), (123, 790), 20, True, muestra=(108, 743, 140, 753))
    fila_av = (40, 700, 560, 880)
    S.row([(fit(avimg, 920, fila_av), 'HOY · Avance: pastilla sol con el ícono de Material', DIM), (fit(c, 920, fila_av), 'PROPUESTA · pastilla crema + gota encendida (sobre sol no se vería; pastilla aproximada)', LIME)])

    # ---------------------------------------------------------------- P9 y P3 en Unity: la cuenta regresiva y Nubi de los tutoriales (capturas reales)
    S.section('P9 · La cuenta regresiva con el mismo kit (Unity, capturas reales a 1080 × 2400)',
              'Los numerales 3-2-1 son de plasticina (churros gordos hechos en Blender, teñidos con el color de cada paso), el título es una losa mediana crema y, bajo el anillo, va el EMBLEMA del juego: el mismo objeto que sale en el momento del final. Los tiempos no cambian. '
              'Tres juegos de tres áreas: Tinta o Palabra, Bodega de carga y Lluvia de meteoros.')
    for game, nombre in (('stroop', 'Tinta o Palabra'), ('bodega', 'Bodega de carga'), ('meteoros', 'Lluvia de meteoros')):
        hoy_c = unity(HOY_UNITY, game, 'cuenta'); n3 = unity_first(UNITY_SHOTS, game, 'cuenta-3'); n2 = unity_first(UNITY_SHOTS, game, 'cuenta'); n1 = unity_first(UNITY_SHOTS, game, 'cuenta-1')
        tiles = []
        for im, tag, col in ((hoy_c, 'HOY · ' + nombre, DIM), (n3, 'PROPUESTA · 3', LIME), (n2, '2', LIME), (n1, '1', LIME)):
            if im is not None: tiles.append((fit(im, 290, (0, 100, 1080, 2250)), tag, col))
        if tiles: S.row(tiles)
    S.section('P3 · Nubi en los tutoriales de Unity: 120 dp preferido, 60 dp mínimo (antes 100 a 50), con su sombra fija',
              'La Nubi de los tutoriales mide hasta 120 dp (antes 100) y nunca baja de 60 dp (antes 50: la cara se lee hasta unos 56 dp); flota y parpadea como la de la app, con UNA sombra en el suelo que no flota con ella.')
    for game, nombre, crop in (('stroop', 'Tinta o Palabra', (0, 330, 1080, 620)), ('bodega', 'Bodega de carga', (0, 1990, 1080, 2290))):
        h = unity(HOY_UNITY, game, 'tutorial-1'); n = unity(UNITY_SHOTS, game, 'tutorial-1')
        if h is not None and n is not None: S.row([(fit(h, 920, crop), 'HOY · ' + nombre + ' · tutorial, paso 1', DIM), (fit(n, 920, crop), 'PROPUESTA · Nubi de 120 dp con su sombra', LIME)])

    return S, TW


if __name__ == '__main__':
    S, TW = main()
    S.render().save(OUT)
    print('ok', OUT)
