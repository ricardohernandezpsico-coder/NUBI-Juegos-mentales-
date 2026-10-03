"""Figuras de "La estrella intrusa" (docs/diseno-estrella-intrusa.md, sección 7): CADA REGLA del banco tiene su figura, un dibujo de
estrellas de un objeto o animal emblemático que se traza con una chispa y se graba con su nombre.

  python tools/intrusa/figuras.py            escribe las figuras para Unity y la app y las valida
  python tools/intrusa/figuras.py --hoja     además arma la hoja de revisión docs/previews/intrusa-figuras.png

Formato de salida (un JSON, sirve a Unity y a la app; los puntos van en 0..1):
  La copia de Unity usa listas planas (JsonUtility no lee listas de listas; ver plano()); la de la app, las listas anidadas.
  figuras: [ {regla, nombre, puntos:[[x,y]...], aristas:[[a,b]...] (en orden de trazado), anclas:[4 índices], huecos:[[x,y]...],
              contorno:[{suave:true, puntos:[[x,y]...]} | {elipse:[cx,cy,rx,ry]}], detalles:[{ojo:[x,y]} | {linea:[[x,y],[x,y]]} | {punto:[x,y]}]} ]
Las figuras son dibujos propios: no se calcó ni copió ninguna lámina histórica ni ninguna constelación real.
"""
import argparse
import functools
import itertools
import json
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(os.path.dirname(HERE))
sys.path.insert(0, HERE)
from figuras_a import FIGURAS_A  # noqa: E402
from figuras_b import FIGURAS_B  # noqa: E402
from figuras_c import FIGURAS_C  # noqa: E402
from figuras_d import FIGURAS_D  # noqa: E402

OUT_UNITY = os.path.join(ROOT, "unity", "NeuroVidaCore", "Assets", "Resources", "Lexico", "intrusa_figuras.json")
OUT_APP = os.path.join(ROOT, "app", "src", "main", "assets", "intrusa_figuras.json")
OUT_HOJA = os.path.join(ROOT, "docs", "previews", "intrusa-figuras.png")
G_JSON = os.path.join(ROOT, "unity", "NeuroVidaCore", "Assets", "Resources", "Lexico", "intrusa_es.json")

# Caja de juego (dp) y pantalla lógica del boceto: 360 × 640. La figura se dibuja en BOX; las etiquetas pueden salir un poco de ella.
W, H = 360, 640
BOX = (40, 170, 280, 260)          # x, y, ancho, alto
LABEL_SP = 17                      # tamaño de las etiquetas (sp) para verificar que no se encimen
LABEL_H = 30
HUD_BOTTOM = 84                    # nada de etiquetas por encima (HUD y cometa del tiempo)
TEXT_TOP = 508                     # nada por debajo (aviso y nombre de la figura)
ROT = 12.0

# Figuras compartidas entre dos reglas (solo si son casi iguales) y figuras con aspa a propósito: se listan para Ricardo.
REDISENADAS = {f["k"] for f in FIGURAS_D}   # rediseñadas el 2-oct (anexo de la Tarea 15): se marcan con * en la hoja
COMPARTIDAS = {}
CON_ASPA = set()   # sin excepciones: ni las tijeras llevan aspa (se abren ~25° y los dos pivotes son nodos distintos)
DUDOSAS = {                        # clave de la regla -> por qué conviene mirarla
    "se pone en los pies": "¿se distingue de La Bota?",
    "sirve para guardar cosas": "baúl: ¿se lee?",
    "sirve para respirar": "nariz: ¿se lee?",
}


def todas():
    """A + B + C, con los rediseños del 2-oct (figuras_d.py) en lugar de las figuras de la misma regla."""
    nuevas = {f["k"]: f for f in FIGURAS_D}
    base = [nuevas.pop(f["k"], f) for f in FIGURAS_A + FIGURAS_B + FIGURAS_C]
    return base + list(nuevas.values())


def resolver(figs, por_regla):
    """Rellena los huecos de las figuras que dicen h=None."""
    out = []
    for f in figs:
        if f.get("h") is None:
            f = dict(f, h=auto_huecos(f, por_regla.get(f["k"], [])))
        out.append(f)
    return out


# ------------------------------------------------------------------ construir
def aristas(trazos):
    seen, out = set(), []
    for tr in trazos:
        for a, b in zip(tr, tr[1:]):
            key = (min(a, b), max(a, b))
            if a != b and key not in seen:
                seen.add(key)
                out.append([a, b])
    return out


def contorno(fig):
    """Contorno suave y grabado alrededor de los puntos del contorno (empujados 0,035 hacia afuera) y las elipses extra."""
    idx = fig["o"]
    if len(idx) < 3:
        return [{"elipse": list(e)} for e in fig.get("e", [])]
    pts = [fig["p"][i] for i in idx]
    cx = sum(p[0] for p in pts) / len(pts)
    cy = sum(p[1] for p in pts) / len(pts)
    out = []
    for x, y in pts:
        dx, dy = x - cx, y - cy
        d = math.hypot(dx, dy) or 1
        out.append([round(x + dx / d * 0.035, 3), round(y + dy / d * 0.035, 3)])
    res = [{"suave": True, "puntos": out}]
    for e in fig.get("e", []):
        res.append({"elipse": list(e)})
    return res


def detalles(fig):
    out = []
    for d in fig.get("d", []):
        if d[0] == "ojo":
            out.append({"ojo": [d[1], d[2]]})
        elif d[0] == "punto":
            out.append({"punto": [d[1], d[2]]})
        else:
            out.append({"linea": [[d[1], d[2]], [d[3], d[4]]]})
    return out


def auto_huecos(fig, grupos, n=3):
    """Elige los huecos de la figura (donde puede caer la intrusa): lejos de estrellas y líneas (>= 0,16) y tales que las etiquetas
    de los grupos reales de la regla quepan. Determinista: rejilla de 0,02, primero los mejores, separados >= 0,3 entre sí."""
    pts, ed = fig["p"], aristas(fig["t"])
    cand = []
    for i in range(1, 25):
        for j in range(1, 25):
            h = (i * 0.04, j * 0.04)
            c = min(min(dist(h, p) for p in pts), min(dist_segmento(h, pts[a], pts[b]) for a, b in ed))
            if c >= 0.165:
                cand.append((h, c))
    muestra = (grupos or [])[:5]

    def puntaje(h):
        f2 = dict(fig, h=[h])
        ok = tot = 0
        for g in muestra:
            for espejo, giro in itertools.product((False, True), (-ROT, 0.0, ROT)):
                tot += 1
                ok += hay_reparto(f2, g["p"] + [g["x"]], espejo, giro, 0)
        return ok / tot if tot else 1.0
    pun = sorted(((puntaje(h), c, h) for h, c in cand), key=lambda t: (-round(t[0], 2), -t[1]))
    pun = [t for t in pun if t[0] >= 0.99] or pun
    elegidos = []
    for p_, c, h in pun:
        if all(dist(h, e) >= 0.3 for e in elegidos):
            elegidos.append(h)
        if len(elegidos) == n:
            break
    return [(round(x, 2), round(y, 2)) for x, y in elegidos]


def construir(fig, regla_nombre=None):
    return {
        "regla": fig["k"], "nombre": fig["n"],
        "puntos": [[round(x, 3), round(y, 3)] for x, y in fig["p"]],
        "aristas": aristas(fig["t"]),
        "anclas": list(fig["a"]),
        "huecos": [[round(x, 3), round(y, 3)] for x, y in fig["h"]],
        "contorno": contorno(fig),
        "detalles": detalles(fig),
    }


def plano(d):
    """La misma figura para Unity: JsonUtility no lee listas de listas, así que todo va en listas planas de números
    (puntos [x0,y0,x1,y1...], aristas [a0,b0,a1,b1...], huecos [x,y...]); contorno = [{suave, puntos, elipse}], detalles = [{tipo, v}]."""
    flat = lambda L: [v for p in L for v in p]
    cont = [{"suave": "suave" in c, "puntos": flat(c.get("puntos", [])), "elipse": list(c.get("elipse", []))} for c in d["contorno"]]
    det = []
    for x in d["detalles"]:
        if "ojo" in x:
            det.append({"tipo": "ojo", "v": list(x["ojo"])})
        elif "punto" in x:
            det.append({"tipo": "punto", "v": list(x["punto"])})
        else:
            det.append({"tipo": "linea", "v": flat(x["linea"])})
    return {"regla": d["regla"], "nombre": d["nombre"], "puntos": flat(d["puntos"]), "aristas": flat(d["aristas"]), "anclas": d["anclas"],
            "huecos": flat(d["huecos"]), "contorno": cont, "detalles": det}


# ------------------------------------------------------------------ geometría
def dist(a, b):
    return math.hypot(a[0] - b[0], a[1] - b[1])


def dist_segmento(p, a, b):
    ax, ay, bx, by = a[0], a[1], b[0], b[1]
    dx, dy = bx - ax, by - ay
    L = dx * dx + dy * dy
    t = 0 if L == 0 else max(0, min(1, ((p[0] - ax) * dx + (p[1] - ay) * dy) / L))
    return math.hypot(p[0] - (ax + t * dx), p[1] - (ay + t * dy))


def componentes_conexas(n, edges):
    padre = list(range(n))

    def f(x):
        while padre[x] != x:
            padre[x] = padre[padre[x]]
            x = padre[x]
        return x
    for a, b in edges:
        padre[f(a)] = f(b)
    return len({f(i) for i in range(n)})


def angulo_interior(a, b, c):
    v1 = (a[0] - b[0], a[1] - b[1])
    v2 = (c[0] - b[0], c[1] - b[1])
    d = (v1[0] * v2[0] + v1[1] * v2[1]) / ((math.hypot(*v1) * math.hypot(*v2)) or 1)
    return math.degrees(math.acos(max(-1, min(1, d))))


def ciclos(n, edges, largos=(5, 6)):
    adj = {i: set() for i in range(n)}
    for a, b in edges:
        adj[a].add(b)
        adj[b].add(a)
    vistos, out = set(), []

    def dfs(inicio, actual, camino):
        if len(camino) in largos and inicio in adj[actual]:
            key = frozenset(camino)
            if key not in vistos and len(camino) >= 3:
                vistos.add(key)
                out.append(list(camino))
        if len(camino) >= max(largos):
            return
        for v in adj[actual]:
            if v > inicio and v not in camino:
                dfs(inicio, v, camino + [v])
    for i in range(n):
        dfs(i, i, [i])
    return out


def casi_regular(pts, ciclo, tol=0.15):
    P = [pts[i] for i in ciclo]
    k = len(P)
    lados = [dist(P[i], P[(i + 1) % k]) for i in range(k)]
    angs = [angulo_interior(P[i - 1], P[i], P[(i + 1) % k]) for i in range(k)]
    ml, ma = sum(lados) / k, sum(angs) / k
    return all(abs(l - ml) <= tol * ml for l in lados) and all(abs(a - ma) <= tol * ma for a in angs)


def es_estrella(pts, edges, tol=0.15):
    """Un punto con 5 o 6 rayos de largo y reparto angular casi iguales: una estrella de 5 o 6 puntas."""
    adj = {}
    for a, b in edges:
        adj.setdefault(a, []).append(b)
        adj.setdefault(b, []).append(a)
    for c, vec in adj.items():
        if len(vec) in (5, 6):
            L = [dist(pts[c], pts[v]) for v in vec]
            ang = sorted(math.degrees(math.atan2(pts[v][1] - pts[c][1], pts[v][0] - pts[c][0])) % 360 for v in vec)
            gaps = [(ang[(i + 1) % len(ang)] - ang[i]) % 360 for i in range(len(ang))]
            ml, mg = sum(L) / len(L), 360 / len(vec)
            if all(abs(l - ml) <= tol * ml for l in L) and all(abs(g - mg) <= tol * mg for g in gaps):
                return True
    return False


def hay_aspa(pts, edges):
    """Un punto de 4 rayos formando dos rectas casi perpendiculares (una cruz o aspa)."""
    adj = {}
    for a, b in edges:
        adj.setdefault(a, []).append(b)
        adj.setdefault(b, []).append(a)
    for c, vec in adj.items():
        if len(vec) == 4:
            ang = sorted(math.degrees(math.atan2(pts[v][1] - pts[c][1], pts[v][0] - pts[c][0])) % 360 for v in vec)
            # opuestos casi rectos: ang[0]~ang[2]-180 y ang[1]~ang[3]-180
            if abs((ang[2] - ang[0]) - 180) < 30 and abs((ang[3] - ang[1]) - 180) < 30:
                dif = (ang[1] - ang[0]) % 180
                if 55 <= dif <= 125:
                    return True
    return False


# ------------------------------------------------------------------ transformaciones de la ronda (reflejo y giro)
def a_pantalla(p, espejo=False, giro=0.0):
    x, y = p
    if espejo:
        x = 1 - x
    bx, by, bw, bh = BOX
    px, py = (x - 0.5) * bw, (y - 0.5) * bh
    c, s = math.cos(math.radians(giro)), math.sin(math.radians(giro))
    return (bx + bw / 2 + px * c - py * s, by + bh / 2 + px * s + py * c)


_fuente = {}


@functools.lru_cache(maxsize=None)
def ancho_texto(palabra, sp=LABEL_SP):
    from PIL import ImageFont
    ruta = os.path.join(ROOT, "tools", "previews", "fuentes-letra", "AtkinsonHyperlegible-Bold.ttf")
    if sp not in _fuente:
        _fuente[sp] = ImageFont.truetype(ruta, sp * 4)
    return _fuente[sp].getlength(palabra) / 4


def placa(x, y, ancho, arriba):
    w = ancho + 22
    py = y - 14 - LABEL_H if arriba else y + 14
    px = max(8, min(W - 8 - w, x - w / 2))
    return (px, py, w, LABEL_H)


def se_cruzan(a, b, margen=3):
    return not (a[0] + a[2] + margen <= b[0] or b[0] + b[2] + margen <= a[0] or a[1] + a[3] + margen <= b[1] or b[1] + b[3] + margen <= a[1])


def verificar_etiquetas(fig, palabras, espejo, giro, hueco):
    """Las 4 etiquetas de las anclas y la de la intrusa en [hueco]: sin solaparse, dentro de pantalla y fuera del HUD y del texto de abajo.
    Devuelve el motivo del fallo o None."""
    rects = []
    items = [(fig["p"][i], palabras[j]) for j, i in enumerate(fig["a"])] + [(fig["h"][hueco], palabras[4])]
    for (x, y), pal in items:
        sx, sy = a_pantalla((x, y), espejo, giro)
        arriba = (sy - BOX[1]) / BOX[3] < 0.35
        r = placa(sx, sy, ancho_texto(pal), arriba)
        if r[1] < HUD_BOTTOM or r[1] + r[3] > TEXT_TOP:
            return f"etiqueta «{pal}» fuera de la zona de juego"
        if r[0] < 8 - 0.01 or r[0] + r[2] > W - 8 + 0.01:
            return f"etiqueta «{pal}» fuera de la pantalla"
        rects.append((r, pal))
    for (ra, pa), (rb, pb) in itertools.combinations(rects, 2):
        if se_cruzan(ra, rb):
            return f"«{pa}» y «{pb}» se solapan"
    return None


# ------------------------------------------------------------------ reglas anti-símbolo (anexo del 2-oct)
# Ninguna figura puede leerse como un símbolo: nada de rayos que salen de un centro, ruedas, estrellas, abanicos ni simetría radial.
MAX_GRADO = 3          # a) ningún nodo con 4 aristas o más (esto incluye cualquier abanico de 5 rayos o más: f)


def grados(n, edges):
    g = [0] * n
    for a, b in edges:
        g[a] += 1
        g[b] += 1
    return g


def centroide(pts):
    return (sum(p[0] for p in pts) / len(pts), sum(p[1] for p in pts) / len(pts))


def casco(pts):
    """Casco convexo (cadena monótona) como lista de puntos."""
    P = sorted(set((round(x, 6), round(y, 6)) for x, y in pts))
    if len(P) <= 2:
        return P

    def cross(o, a, b):
        return (a[0] - o[0]) * (b[1] - o[1]) - (a[1] - o[1]) * (b[0] - o[0])
    lo, up = [], []
    for p in P:
        while len(lo) >= 2 and cross(lo[-2], lo[-1], p) <= 0:
            lo.pop()
        lo.append(p)
    for p in reversed(P):
        while len(up) >= 2 and cross(up[-2], up[-1], p) <= 0:
            up.pop()
        up.append(p)
    return lo[:-1] + up[:-1]


def esquinas(hull, tol=165.0):
    """Vértices del casco con ángulo interior menor que [tol] (se descartan los casi colineales)."""
    out = []
    n = len(hull)
    for i in range(n):
        if angulo_interior(hull[i - 1], hull[i], hull[(i + 1) % n]) < tol:
            out.append(hull[i])
    return out


def area(poly):
    return abs(sum(poly[i][0] * poly[(i + 1) % len(poly)][1] - poly[(i + 1) % len(poly)][0] * poly[i][1] for i in range(len(poly)))) / 2


def simetria_giro(pts, tol=0.045):
    """El mayor orden k (3 a 6) tal que girar los puntos 360/k grados alrededor de su centro deja el conjunto igual; 0 si ninguno."""
    cx, cy = centroide(pts)
    for k in (6, 5, 4, 3):
        ang = 2 * math.pi / k
        c, s = math.cos(ang), math.sin(ang)
        ok = True
        for x, y in pts:
            rx, ry = cx + (x - cx) * c - (y - cy) * s, cy + (x - cx) * s + (y - cy) * c
            if min(math.hypot(rx - u, ry - v) for u, v in pts) > tol:
                ok = False
                break
        if ok:
            return k
    return 0


def rueda(fig, ed):
    """b) Contorno circular o elíptico (elipse, o puntos casi equidistantes del centro) con líneas internas que cruzan por el centro."""
    pts = fig["p"]
    formas = []
    if fig.get("o") and len(fig["o"]) >= 6:
        cont = [pts[i] for i in fig["o"]]
        cx, cy = centroide(cont)
        r = [math.hypot(x - cx, y - cy) for x, y in cont]
        m = sum(r) / len(r)
        if m > 0.12 and all(abs(v - m) <= 0.22 * m for v in r):
            formas.append((cx, cy, m))
    for e in fig.get("e", []):
        formas.append((e[0], e[1], (e[2] + e[3]) / 2))
    for cx, cy, r in formas:
        for a, b in ed:
            A, B = pts[a], pts[b]
            L2 = (B[0] - A[0]) ** 2 + (B[1] - A[1]) ** 2
            if L2 == 0:
                continue
            t = ((cx - A[0]) * (B[0] - A[0]) + (cy - A[1]) * (B[1] - A[1])) / L2
            if 0.12 < t < 0.88 and dist_segmento((cx, cy), A, B) < 0.045 and dist(A, B) > r * 1.5:
                return True
    return False


def triangulo_con_linea(pts, ed):
    """d) Casco de solo 3 esquinas (un triángulo) con alguna línea interna larga."""
    hull = casco(pts)
    if len(esquinas(hull)) != 3:
        return False
    return any(dist(pts[a], pts[b]) > 0.3 and a != b for a, b in ed if pts[a] not in hull or pts[b] not in hull or True) and len(ed) > 3


def estrella_4(fig, pts):
    """e) Rombo con picos: 4 esquinas en cruz cuyas diagonales se cortan por el medio, y un contorno muy hundido (menos del 55% del casco)."""
    hull = casco(pts)
    cor = esquinas(hull)
    if len(cor) != 4:
        return False
    a, b, c, d = cor
    m1 = ((a[0] + c[0]) / 2, (a[1] + c[1]) / 2)
    m2 = ((b[0] + d[0]) / 2, (b[1] + d[1]) / 2)
    if dist(m1, m2) > 0.12:
        return False
    contorno = [pts[i] for i in fig["o"]] if fig.get("o") and len(fig["o"]) >= 3 else None
    if contorno is None or sum(1 for c_ in cor if any(dist(c_, q) < 1e-3 for q in contorno)) < 3:
        return False
    return area(contorno) < 0.55 * area(hull)


def validar(fig, palabra_larga="rompecabezas", grupos=None):
    """Lista de problemas de una figura (vacía si está bien)."""
    err = []
    pts, ed = fig["p"], aristas(fig["t"])
    n = len(pts)
    if not 7 <= n <= 12:
        err.append(f"{n} puntos (deben ser 7 a 12)")
    if any(not (0 <= x <= 1 and 0 <= y <= 1) for x, y in pts):
        err.append("puntos fuera de 0..1")
    if componentes_conexas(n, ed) != 1:
        err.append("el grafo no es conexo")
    if any(max(a, b) >= n for a, b in ed):
        err.append("índice de arista inválido")
    an = fig["a"]
    if len(an) != 4 or len(set(an)) != 4:
        err.append("las anclas deben ser 4 distintas")
    else:
        for i, j in itertools.combinations(an, 2):
            if dist(pts[i], pts[j]) < 0.25:
                err.append(f"anclas {i} y {j} a menos de 0,25")
    if not 2 <= len(fig["h"]) <= 3:
        err.append("deben ser 2 o 3 huecos")
    for k, h in enumerate(fig["h"]):
        if min(dist(h, p) for p in pts) < 0.15:
            err.append(f"hueco {k} a menos de 0,15 de una estrella")
        if ed and min(dist_segmento(h, pts[a], pts[b]) for a, b in ed) < 0.15:
            err.append(f"hueco {k} a menos de 0,15 de una línea")
    for c in ciclos(n, ed):
        if casi_regular(pts, c):
            err.append(f"polígono casi regular {c}")
    if es_estrella(pts, ed):
        err.append("estrella de 5 o 6 puntas")
    gr = grados(n, ed)
    if max(gr) > MAX_GRADO:
        err.append(f"nodo {gr.index(max(gr))} con {max(gr)} aristas (máximo {MAX_GRADO}: nada de rayos ni abanicos)")
    if rueda(fig, ed):
        err.append("contorno circular con líneas por el centro (rueda, símbolo)")
    k = simetria_giro(pts)
    if k:
        err.append(f"simetría de giro de orden {k}")
    if triangulo_con_linea(pts, ed):
        err.append("triángulo con una línea interna")
    if estrella_4(fig, pts):
        err.append("estrella de 4 puntas (rombo con picos)")
    if hay_aspa(pts, ed) and fig["k"] not in CON_ASPA:
        err.append("cruz o aspa")
    if len(fig["o"]) < 3 and not fig.get("e"):
        err.append("sin contorno (ni puntos ni elipses)")
    # etiquetas: la palabra más larga del banco, sola en cada estrella, siempre cabe en pantalla y en la zona de juego
    if len(an) == 4:
        for espejo, giro, hueco in itertools.product((False, True), (-ROT, 0.0, ROT), range(len(fig["h"]))):
            for j in range(5):
                x, y = (fig["p"][an[j]] if j < 4 else fig["h"][hueco])
                sx, sy = a_pantalla((x, y), espejo, giro)
                r = placa(sx, sy, ancho_texto(palabra_larga), (sy - BOX[1]) / BOX[3] < 0.35)
                if r[1] < HUD_BOTTOM or r[1] + r[3] > TEXT_TOP or r[0] < 7.99 or r[0] + r[2] > W - 7.99:
                    err.append(f"la etiqueta más larga no cabe en la estrella {j} (espejo={espejo}, giro={giro:+.0f})")
        # y con cada grupo REAL del banco de la regla: el juego busca al armar la ronda un reflejo/giro/hueco y un reparto de las
        # palabras sin choques; exigimos que para cada grupo sirvan al menos MIN_COMBOS de las 12-18 combinaciones
        for g in (grupos or []):
            buenos = sum(1 for espejo, giro, hueco in itertools.product((False, True), (-ROT, 0.0, ROT), range(len(fig["h"])))
                         if hay_reparto(fig, g["p"] + [g["x"]], espejo, giro, hueco))
            if buenos < MIN_COMBOS:
                err.append(f"grupo {g['i']} {g['p']}+{g['x']}: solo {buenos} combinaciones de etiquetas sin choque")
                break
    return err


MIN_COMBOS = 6
PALABRA_TIPICA = "mariposas"


def hay_reparto(fig, palabras, espejo, giro, hueco):
    """True si alguna manera de poner las 4 palabras del grupo en las 4 anclas (la intrusa va en el hueco) deja las etiquetas bien."""
    cuatro, intrusa = palabras[:4], palabras[4]
    for perm in itertools.permutations(cuatro):
        if verificar_etiquetas(fig, list(perm) + [intrusa], espejo, giro, hueco) is None:
            return True
    return False


def palabra_mas_larga():
    sys.path.insert(0, HERE)
    import grupos as G
    return max(G.PALABRAS, key=lambda w: ancho_texto(w))


def reglas_del_banco():
    import grupos as G
    return set(G.REGLAS)


# ------------------------------------------------------------------ hoja de revisión
def hoja(figs):
    from PIL import Image, ImageDraw, ImageFont
    cols = 9
    cw, ch = 214, 218
    filas = (len(figs) + cols - 1) // cols
    head = 90
    img = Image.new("RGB", (cols * cw + 20, head + filas * ch + 20), (6, 8, 30))
    d = ImageDraw.Draw(img)
    fh = ImageFont.truetype(os.path.join(ROOT, "app", "src", "main", "res", "font", "fredoka.ttf"), 30)
    fn = ImageFont.truetype(os.path.join(ROOT, "app", "src", "main", "res", "font", "nunito_regular.ttf"), 13)
    fb = ImageFont.truetype(os.path.join(ROOT, "app", "src", "main", "res", "font", "fredoka.ttf"), 17)
    d.text((14, 14), "La estrella intrusa: figuras de las reglas (hoja de revisión)", font=fh, fill=(255, 227, 163))
    d.text((14, 54), "Cada cuadro: estrellas de la figura, líneas de la chispa, grabado dorado, nombre y regla. Las marcadas con * se rediseñaron el 2-oct o conviene mirarlas.", font=fn, fill=(217, 212, 245))
    import random
    for n, f in enumerate(figs):
        col, fila = n % cols, n // cols
        x0, y0 = 10 + col * cw, head + fila * ch
        bx, by, bw, bh = x0 + 22, y0 + 8, cw - 44, 150
        P = [(bx + p[0] * bw, by + p[1] * bh) for p in f["puntos"]]
        # contorno dorado
        for c in f["contorno"]:
            if "suave" in c:
                Q = [(bx + p[0] * bw, by + p[1] * bh) for p in c["puntos"]]
                curva = suavizar(Q)
                d.line(curva + [curva[0]], fill=(150, 125, 78), width=1)
            else:
                cx, cy, rx, ry = c["elipse"]
                d.ellipse([bx + (cx - rx) * bw, by + (cy - ry) * bh, bx + (cx + rx) * bw, by + (cy + ry) * bh], outline=(150, 125, 78))
        for a, b in f["aristas"]:
            d.line([P[a], P[b]], fill=(255, 217, 138), width=1)
        for i, (x, y) in enumerate(P):
            r = 3.4 if i in f["anclas"] else 1.8
            d.ellipse([x - r, y - r, x + r, y + r], fill=(255, 248, 230))
            if i in f["anclas"]:
                d.ellipse([x - 6, y - 6, x + 6, y + 6], outline=(255, 217, 138))
        for hx, hy in f["huecos"]:
            x, y = bx + hx * bw, by + hy * bh
            d.ellipse([x - 3, y - 3, x + 3, y + 3], outline=(255, 107, 74))
        for det in f["detalles"]:
            if "ojo" in det:
                x, y = bx + det["ojo"][0] * bw, by + det["ojo"][1] * bh
                d.ellipse([x - 2, y - 2, x + 2, y + 2], fill=(233, 199, 123))
            if "punto" in det:
                x, y = bx + det["punto"][0] * bw, by + det["punto"][1] * bh
                d.ellipse([x - 3, y - 3, x + 3, y + 3], outline=(233, 199, 123))
            if "linea" in det:
                (x1, y1), (x2, y2) = det["linea"]
                d.line([(bx + x1 * bw, by + y1 * bh), (bx + x2 * bw, by + y2 * bh)], fill=(233, 199, 123), width=1)
        marca = "* " if f["regla"] in DUDOSAS or f["regla"] in COMPARTIDAS or f["regla"] in CON_ASPA or f["regla"] in REDISENADAS else ""
        d.text((x0 + cw / 2, y0 + 168), marca + f["nombre"], font=fb, fill=(255, 227, 163), anchor="mm")
        d.text((x0 + cw / 2, y0 + 190), f["regla"], font=fn, fill=(217, 212, 245), anchor="mm")
        nota = DUDOSAS.get(f["regla"]) or ("rediseñada el 2-oct" if f["regla"] in REDISENADAS else None)
        if nota:
            d.text((x0 + cw / 2, y0 + 207), nota[:34], font=fn, fill=(255, 107, 74), anchor="mm")
    os.makedirs(os.path.dirname(OUT_HOJA), exist_ok=True)
    img.save(OUT_HOJA)
    print(OUT_HOJA)


def suavizar(P, pasos=10):
    n = len(P)
    out = []
    for i in range(n):
        p0, p1, p2, p3 = P[(i - 1) % n], P[i], P[(i + 1) % n], P[(i + 2) % n]
        for k in range(pasos):
            t = k / pasos
            t2, t3 = t * t, t * t * t
            out.append(tuple(0.5 * ((2 * p1[j]) + (-p0[j] + p2[j]) * t + (2 * p0[j] - 5 * p1[j] + 4 * p2[j] - p3[j]) * t2 + (-p0[j] + 3 * p1[j] - 3 * p2[j] + p3[j]) * t3) for j in range(2)))
    return out


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--hoja", action="store_true")
    ap.add_argument("--check", action="store_true", help="solo valida: no escribe ningún archivo")
    args = ap.parse_args()
    sys.stdout.reconfigure(encoding="utf8")
    figs = todas()
    larga = palabra_mas_larga()
    print(f"{len(figs)} figuras; palabra más larga del banco: «{larga}» ({ancho_texto(larga):.0f} px a {LABEL_SP} sp)")
    malas = 0
    por_regla = {}
    with open(G_JSON, encoding="utf8") as fh:
        for g in json.load(fh)["grupos"]:
            por_regla.setdefault(g["k"], []).append(g)
    figs = resolver(figs, por_regla)
    for f in figs:
        err = validar(f, larga, por_regla.get(f["k"], []))
        if err:
            malas += 1
            print(f"✗ {f['k']} ({f['n']}):")
            for e in err:
                print("    -", e)
    claves = [f["k"] for f in figs]
    faltan = sorted(reglas_del_banco() - set(claves))
    sobran = sorted(set(claves) - reglas_del_banco())
    if faltan:
        print("reglas sin figura:", faltan)
    if sobran:
        print("figuras sin regla:", sobran)
    if len(claves) != len(set(claves)):
        print("claves repetidas:", sorted({c for c in claves if claves.count(c) > 1}))
    nombres = [f["n"] for f in figs]
    rep = sorted({n for n in nombres if nombres.count(n) > 1})
    if rep:
        print("nombres repetidos (compartidos):", rep)
    print(f"{malas} figuras con problemas")
    datos = {"version": 1, "figuras": [construir(f) for f in figs]}
    if args.check:
        if args.hoja:
            hoja(datos["figuras"])
        return
    datos_unity = {"version": 1, "figuras": [plano(d) for d in datos["figuras"]]}
    for ruta, contenido in ((OUT_UNITY, datos_unity), (OUT_APP, datos)):
        os.makedirs(os.path.dirname(ruta), exist_ok=True)
        with open(ruta, "w", encoding="utf8", newline="\n") as fh:
            json.dump(contenido, fh, ensure_ascii=False, separators=(",", ":"))
        print(ruta, os.path.getsize(ruta) // 1024, "KB")
    if args.hoja:
        hoja(datos["figuras"])


if __name__ == "__main__":
    main()
