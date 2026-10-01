"""Generador de frases de "¿Verdad o disparate?" (1-oct; diseño en docs/diseno-verdad-o-disparate.md, sección 6).

Determinista (semilla fija). Lee la base de conocimiento propia (`conocimiento.py`), arma frases correctas en español para
los 6 tipos de la tabla de dificultad y su respuesta, y escribe:

  unity/NeuroVidaCore/Assets/Resources/Frases/disparate_es.json   (para el juego; formato de JsonUtility)
  docs/frases-muestra-disparate.md                                (100 frases al azar para que Ricardo las revise)

Tipos: 1 sujeto + verbo · 2 + complemento o adjetivo · 3 negación · 4 con "que" · 5 todos / algunos / ningún ·
6 comparaciones y orden. Cada tipo sale 50% verdad y 50% disparate. Un disparate es EVIDENTE (cruza categorías: "las
sillas ríen") o SUTIL (se parece a algo cierto: "los pingüinos vuelan"). Cada disparate lleva su corrección corta.

  python tools/frases/disparate.py            # escribe el JSON y la muestra
  python tools/frases/disparate.py --stats    # solo cuenta
"""
import hashlib
import itertools
import json
import os
import random
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import conocimiento as K  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT_JSON = os.path.join(ROOT, "unity", "NeuroVidaCore", "Assets", "Resources", "Frases", "disparate_es.json")
OUT_MD = os.path.join(ROOT, "docs", "frases-muestra-disparate.md")
EXCLUIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), "excluir.txt")
SEED = 20261001
MAX_CARACTERES = 50   # tope del largo de una frase: en el teléfono ocupa 2 renglones (3 en mayores)
POR_CLASE_MAX = 200     # frases de verdad (y de disparate) por tipo, como máximo
NOMBRES_TIPO = {1: "corta", 2: "con complemento", 3: "negación", 4: "frase con pausa (, que …,)", 5: "todos / algunos / ningún", 6: "comparación y orden"}


# ---------------------------------------------------------------- gramática

def cap(s):
    return s[0].upper() + s[1:]


def singular(frase):
    """'tienen alas' -> 'tiene alas'; 'se derriten con el calor' -> 'se derrite con el calor'; 'son una estrella' -> 'es una estrella'."""
    w = frase.split(" ")
    i = 1 if w[0] == "se" else 0
    v = w[i]
    if v == "son":
        n = "es"
    elif v.endswith("n"):
        n = v[:-1]
    else:
        raise ValueError("no sé conjugar: " + frase)
    w[i] = n
    return " ".join(w)


def adj(base, genero, plural):
    """Adjetivo con concordancia: 'frío' f pl -> 'frías'; 'caliente' -> 'calientes'; 'azul' pl -> 'azules'."""
    w = base
    if w.endswith("o"):
        w = w[:-1] + ("a" if genero == "f" else "o")
    if plural:
        w += "s" if w[-1] in "aeiouáéíóú" else "es"
    return w


def predicado(prop, genero, plural):
    """Predicado de una propiedad: 'nadan' / 'nada'; '~frío' -> 'es frío' / 'son fríos'; '=un animal/animales' -> 'es un animal' / 'son animales'."""
    if prop.startswith("~"):
        return ("son " if plural else "es ") + adj(prop[1:], genero, plural)
    if prop.startswith("="):
        sg, pl = prop[1:].split("/")
        return "son " + pl if plural else "es " + sg
    return prop if plural else singular(prop)


class Ent:
    def __init__(self, cat, sujeto, si, no):
        self.cat = cat
        self.sujeto = sujeto                       # "los peces"
        art = sujeto.split(" ")[0]
        self.plural = art in ("los", "las")
        self.genero = "f" if art in ("la", "las") else "m"
        self.si = [p.strip() for p in si.split(";") if p.strip()]
        self.no = []
        for p in no.split(";"):
            p = p.strip()
            if p:
                self.no.append((p.lstrip("*"), p.startswith("*")))     # (propiedad, es sutil)
        self.defecto = []
        if cat == "animal":
            self.defecto = [p for p in K.DEFECTO_ANIMAL if p not in self.si]
            self.si += self.defecto

    def pred(self, prop):
        """Predicado de la propiedad concordado con el sujeto: 'nadan' / 'es frío' / 'son fríos'."""
        return predicado(prop, self.genero, self.plural)

    def neg(self, prop):
        return "no " + self.pred(prop)

    def afirma(self, prop):
        """Para la corrección 'sí': 'sí nadan' / 'sí es frío'."""
        return "sí " + self.pred(prop)

    def pool(self):
        return K.POOL_POR_CATEGORIA.get(self.cat, [])


def es_adj(p):
    return p.startswith("~")


def es_corta(p):
    """Una sola palabra, un verbo ('nadan'): da frases de 3 palabras (tipo 1)."""
    return not es_adj(p) and not p.startswith("=") and len(p.split(" ")) == 1


def cargar():
    ents = []
    for linea in K.ENTIDADES.strip().splitlines():
        cat, suj, si, no = linea.split("|")
        ents.append(Ent(cat, suj, si, no))
    return ents


def F(f, v, t, c="", r=""):
    # "i" = id estable de la frase (8 hex del SHA-1 del texto): no cambia si se regenera mientras la frase siga igual
    return {"f": f, "v": v, "t": t, "c": c, "n": len(f.split(" ")), "r": r, "i": hashlib.sha1(f.encode("utf8")).hexdigest()[:8]}


# ---------------------------------------------------------------- tipos 1 a 4

def tipo1(ents):
    V, D = [], []
    for e in ents:
        for p in e.si:
            if es_corta(p) and p not in e.defecto:      # las verdades usan verbos PROPIOS ("ladran"); "comen" o "crecen" suenan raros
                V.append((e.sujeto, F(cap(f"{e.sujeto} {e.pred(p)}"), True, 1)))
        for p, sutil in e.no:
            if es_corta(p):
                D.append((e.sujeto, F(cap(f"{e.sujeto} {e.pred(p)}"), False, 1, "sutil" if sutil else "evidente", cap(f"{e.sujeto} {e.neg(p)}"))))
        for p in e.pool():
            if es_corta(p):
                D.append((e.sujeto, F(cap(f"{e.sujeto} {e.pred(p)}"), False, 1, "evidente", cap(f"{e.sujeto} {e.neg(p)}"))))
    return V, D


def correccion_negada(e, p):
    """'Los gatos no vuelan'; para un adjetivo con opuesto conocido, mejor la verdad: 'El hielo es frío'."""
    if es_adj(p):
        sis = [q for q in e.si if es_adj(q)]
        if sis:
            return cap(f"{e.sujeto} {e.pred(sis[0])}")
    return cap(f"{e.sujeto} {e.neg(p)}")


def tipo2(ents):
    V, D = [], []
    for e in ents:
        for p in e.si:
            if not es_corta(p):
                V.append((e.sujeto, F(cap(f"{e.sujeto} {e.pred(p)}"), True, 2)))
        for p, sutil in e.no:
            if not es_corta(p):
                D.append((e.sujeto, F(cap(f"{e.sujeto} {e.pred(p)}"), False, 2, "sutil" if sutil else "evidente", correccion_negada(e, p))))
        for p in e.pool():
            if not es_corta(p):
                D.append((e.sujeto, F(cap(f"{e.sujeto} {e.pred(p)}"), False, 2, "evidente", cap(f"{e.sujeto} {e.neg(p)}"))))
    return V, D


def tipo3(ents):
    V, D = [], []
    for e in ents:
        for p, _ in e.no:
            V.append((e.sujeto, F(cap(f"{e.sujeto} {e.neg(p)}"), True, 3)))
        for p in e.pool():
            V.append((e.sujeto, F(cap(f"{e.sujeto} {e.neg(p)}"), True, 3)))
        for p in e.si:
            clase = "evidente" if p in e.defecto else "sutil"
            D.append((e.sujeto, F(cap(f"{e.sujeto} {e.neg(p)}"), False, 3, clase, cap(f"{e.sujeto} {e.afirma(p)}"))))
    return V, D


PALABRAS_VACIAS = {"tienen", "sirven", "para", "viven", "hacen", "están", "son", "como", "sobre", "bajo"}


def raices(p):
    """Raíces (3 letras) de las palabras con contenido de una propiedad: sirve para no armar frases que repiten lo mismo."""
    return {w[:3] for w in re.split(r"[ ~=/]", p) if len(w) >= 4 and w not in PALABRAS_VACIAS}


def distintas(a, b):
    return not (raices(a) & raices(b))


def tipo4(ents):
    """Frase con pausa: "Los delfines, que tienen aletas, nadan". La explicativa (entre comas) es SIEMPRE verdad (sale de lo que
    la cosa siempre es o hace), así solo el predicado final decide la respuesta."""
    V, D = [], []
    for e in ents:
        propios = [p for p in e.si if p not in e.defecto]
        for a in propios:
            rel = f"{cap(e.sujeto)}, que {e.pred(a)},"
            for b in propios:
                if b != a and distintas(a, b):
                    V.append((e.sujeto, F(f"{rel} {e.pred(b)}", True, 4)))
            for b, sutil in e.no:
                if distintas(a, b):
                    D.append((e.sujeto, F(f"{rel} {e.pred(b)}", False, 4, "sutil" if sutil else "evidente", f"{rel} {e.neg(b)}")))
            for b in e.pool():
                if distintas(a, b):
                    D.append((e.sujeto, F(f"{rel} {e.pred(b)}", False, 4, "evidente", f"{rel} {e.neg(b)}")))
    return V, D


# ---------------------------------------------------------------- tipo 5: cuantificadores

def frase_q(q, g, p):
    sg, pl, gen, _, _, _ = g
    f = gen == "f"
    if q == "todos":
        sujeto = ("Todas las " if f else "Todos los ") + pl
        pred = predicado(p, gen, True)
    elif q == "algunos":
        sujeto = ("Algunas " if f else "Algunos ") + pl
        pred = predicado(p, gen, True)
    else:
        sujeto = ("Ninguna " if f else "Ningún ") + sg
        pred = predicado(p, gen, False)
    return f"{sujeto} {pred}"


def corr_q(q_real, g, p):
    """La frase verdadera que corrige: 'Solo algunos animales vuelan'."""
    if q_real == "algunos":
        f = frase_q("algunos", g, p)
        return "Solo " + f[0].lower() + f[1:]
    return frase_q(q_real, g, p)


def tipo5(_ents=None):
    V, D = [], []
    for g in K.GRUPOS:
        sg, pl, gen, todos, ninguno, algunos = g
        for p in todos:
            V.append((sg, F(frase_q("todos", g, p), True, 5)))
        for p in ninguno:
            V.append((sg, F(frase_q("ninguno", g, p), True, 5)))
            D.append((sg, F(frase_q("todos", g, p), False, 5, "evidente", corr_q("ninguno", g, p))))
            D.append((sg, F(frase_q("algunos", g, p), False, 5, "evidente", corr_q("ninguno", g, p))))
        for p in algunos:
            V.append((sg, F(frase_q("algunos", g, p), True, 5)))
            D.append((sg, F(frase_q("todos", g, p), False, 5, "sutil", corr_q("algunos", g, p))))
            D.append((sg, F(frase_q("ninguno", g, p), False, 5, "sutil", corr_q("algunos", g, p))))
        for p in todos:
            D.append((sg, F(frase_q("ninguno", g, p), False, 5, "evidente", corr_q("todos", g, p))))
        # 'Algunos' + lo que cumplen todos queda fuera: suena a que otros no (implicatura), es ambiguo
    return V, D


# ---------------------------------------------------------------- tipo 6: comparaciones y orden

def indef(n, g):
    return ("una " if g == "f" else "un ") + n


def defin(n, g):
    return ("la " if g == "f" else "el ") + n


def tipo6(_ents=None):
    V, D = [], []

    # cada familia arma el par (mayor, menor) y saca la verdad y el disparate
    def familia_pares(familia, escala, verdad_fn, falso_fn, dist_min, dist_evidente):
        for (a, ga, na), (b, gb, nb) in itertools.permutations(escala, 2):
            if na - nb < dist_min:
                continue                      # a es el MAYOR (nivel más alto), b el menor
            clase = "evidente" if na - nb >= dist_evidente else "sutil"
            vf = verdad_fn(a, ga, b, gb)
            ff = falso_fn(a, ga, b, gb)
            V.append((familia, F(vf, True, 6)))
            D.append((familia, F(ff, False, 6, clase, vf)))

    # tamaño
    familia_pares("tamaño", K.TAMANO,
                  lambda a, ga, b, gb: f"{cap(indef(a, ga))} es más grande que {indef(b, gb)}",
                  lambda a, ga, b, gb: f"{cap(indef(b, gb))} es más grande que {indef(a, ga)}", 2, 3)
    familia_pares("tamaño", K.TAMANO,
                  lambda a, ga, b, gb: f"{cap(indef(b, gb))} es más {adj('pequeño', gb, False)} que {indef(a, ga)}",
                  lambda a, ga, b, gb: f"{cap(indef(a, ga))} es más {adj('pequeño', ga, False)} que {indef(b, gb)}", 2, 3)
    # peso
    familia_pares("peso", K.TAMANO,
                  lambda a, ga, b, gb: f"{cap(indef(a, ga))} pesa más que {indef(b, gb)}",
                  lambda a, ga, b, gb: f"{cap(indef(b, gb))} pesa más que {indef(a, ga)}", 2, 3)
    familia_pares("peso", K.TAMANO,
                  lambda a, ga, b, gb: f"{cap(indef(b, gb))} pesa menos que {indef(a, ga)}",
                  lambda a, ga, b, gb: f"{cap(indef(a, ga))} pesa menos que {indef(b, gb)}", 2, 3)
    # duración
    familia_pares("duración", K.DURACION,
                  lambda a, ga, b, gb: f"{cap(indef(a, ga))} dura más que {indef(b, gb)}",
                  lambda a, ga, b, gb: f"{cap(indef(b, gb))} dura más que {indef(a, ga)}", 1, 3)
    familia_pares("duración", K.DURACION,
                  lambda a, ga, b, gb: f"{cap(indef(b, gb))} dura menos que {indef(a, ga)}",
                  lambda a, ga, b, gb: f"{cap(indef(a, ga))} dura menos que {indef(b, gb)}", 1, 3)
    # velocidad
    familia_pares("velocidad", K.VELOCIDAD,
                  lambda a, ga, b, gb: f"{cap(defin(a, ga))} es más {adj('rápido', ga, False)} que {defin(b, gb)}",
                  lambda a, ga, b, gb: f"{cap(defin(b, gb))} es más {adj('rápido', gb, False)} que {defin(a, ga)}", 2, 3)
    familia_pares("velocidad", K.VELOCIDAD,
                  lambda a, ga, b, gb: f"{cap(defin(b, gb))} es más {adj('lento', gb, False)} que {defin(a, ga)}",
                  lambda a, ga, b, gb: f"{cap(defin(a, ga))} es más {adj('lento', ga, False)} que {defin(b, gb)}", 2, 3)
    # temperatura
    familia_pares("temperatura", K.TEMPERATURA,
                  lambda a, ga, b, gb: f"{cap(defin(a, ga))} es más caliente que {defin(b, gb)}",
                  lambda a, ga, b, gb: f"{cap(defin(b, gb))} es más caliente que {defin(a, ga)}", 2, 3)
    familia_pares("temperatura", K.TEMPERATURA,
                  lambda a, ga, b, gb: f"{cap(defin(b, gb))} es más {adj('frío', gb, False)} que {defin(a, ga)}",
                  lambda a, ga, b, gb: f"{cap(defin(a, ga))} es más {adj('frío', ga, False)} que {defin(b, gb)}", 2, 3)
    # números
    nums = [(n, "m", i) for i, n in enumerate(K.NUMEROS)]
    familia_pares("números", nums,
                  lambda a, ga, b, gb: f"{cap(a)} es mayor que {b}",
                  lambda a, ga, b, gb: f"{cap(b)} es mayor que {a}", 1, 8)
    familia_pares("números", nums,
                  lambda a, ga, b, gb: f"{cap(b)} es menor que {a}",
                  lambda a, ga, b, gb: f"{cap(a)} es menor que {b}", 1, 8)
    # orden del abecedario
    letras = [(c, "f", i) for i, c in enumerate(K.LETRAS)]
    familia_pares("abecedario", letras,
                  lambda a, ga, b, gb: f"La letra {b} viene antes que la letra {a}",
                  lambda a, ga, b, gb: f"La letra {a} viene antes que la letra {b}", 2, 8)
    familia_pares("abecedario", letras,
                  lambda a, ga, b, gb: f"La letra {a} viene después de la letra {b}",
                  lambda a, ga, b, gb: f"La letra {b} viene después de la letra {a}", 2, 8)
    return V, D


# ---------------------------------------------------------------- selección, filtros y salida

def cargar_excluir():
    pats = []
    if os.path.exists(EXCLUIR):
        for linea in open(EXCLUIR, encoding="utf8"):
            linea = linea.strip()
            if linea and not linea.startswith("#"):
                pats.append(re.compile(linea, re.IGNORECASE))
    return pats


def candidatos():
    """Todas las frases posibles, por tipo: {t: (verdades, disparates)}, cada una como (clave, frase), ya filtradas."""
    ents = cargar()
    excl = cargar_excluir()
    out = {}
    for t, fn in ((1, tipo1), (2, tipo2), (3, tipo3), (4, tipo4), (5, tipo5), (6, tipo6)):
        V, D = fn(ents)
        out[t] = (_limpiar(V, excl), _limpiar(D, excl))
    return out


def _limpiar(lista, excl):
    vistos, res = set(), []
    for k, f in lista:
        txt = f["f"] + " " + f["r"]
        if any(p.search(txt) for p in excl):
            continue
        if f["f"] in vistos or len(f["f"]) > MAX_CARACTERES or len(f["r"]) > MAX_CARACTERES + 12:
            continue
        vistos.add(f["f"])
        res.append((k, f))
    return res


def escoger(lista, n, rng):
    """n frases repartidas parejo entre las claves (entidad, grupo o familia), al azar con semilla."""
    cubos = {}
    for k, f in lista:
        cubos.setdefault(k, []).append(f)
    claves = sorted(cubos)
    for k in claves:
        cubos[k].sort(key=lambda f: f["f"])
        rng.shuffle(cubos[k])
    orden = claves[:]
    rng.shuffle(orden)
    res = []
    while len(res) < n and any(cubos[k] for k in orden):
        for k in orden:
            if cubos[k] and len(res) < n:
                res.append(cubos[k].pop())
    return res


# Reparto del tipo 6 por familia (el orden alfabético se siente escolar: poco)
CUOTAS_T6 = {"tamaño": 0.20, "peso": 0.18, "duración": 0.18, "velocidad": 0.15, "temperatura": 0.06, "números": 0.14, "abecedario": 0.09}


def escoger_cuotas(lista, n, rng, cuotas):
    cubos = {}
    for k, f in lista:
        cubos.setdefault(k, []).append((k, f))
    res = []
    for k, w in cuotas.items():
        q = min(round(n * w), len(cubos.get(k, [])))
        res += escoger(cubos.get(k, []), q, rng)
    ya = {f["f"] for f in res}
    resto = [(k, f) for k, f in lista if f["f"] not in ya and k != "abecedario"]
    if len(res) < n:
        res += escoger(resto, n - len(res), rng)
    return res[:n]


def seleccion(cand=None):
    """{t: [frases]} con el mismo número de verdades y de disparates por tipo (hasta POR_CLASE_MAX de cada una)."""
    cand = cand or candidatos()
    sel = {}
    for t in range(1, 7):
        V, D = cand[t]
        n = min(len(V), len(D), POR_CLASE_MAX)
        rng = random.Random(SEED * 10 + t)
        if t == 6:
            frases = escoger_cuotas(V, n, rng, CUOTAS_T6) + escoger_cuotas(D, n, rng, CUOTAS_T6)
        else:
            frases = escoger(V, n, rng) + escoger(D, n, rng)
        rng.shuffle(frases)
        sel[t] = frases
    return sel


def escribir_json(sel):
    frases = [f for t in range(1, 7) for f in sel[t]]
    doc = {
        "version": 1,
        "idioma": "es",
        "fuente": "tools/frases/disparate.py (generador propio sobre tools/frases/conocimiento.py)",
        "frases": frases,
    }
    os.makedirs(os.path.dirname(OUT_JSON), exist_ok=True)
    cab = {k: v for k, v in doc.items() if k != "frases"}
    nl = chr(10)
    with open(OUT_JSON, "w", encoding="utf8", newline=nl) as fh:
        fh.write(json.dumps(cab, ensure_ascii=False, separators=(",", ":"))[:-1] + ',"frases":[' + nl)
        fh.write(("," + nl).join(json.dumps(f, ensure_ascii=False, separators=(",", ":")) for f in frases))
        fh.write(nl + "]}" + nl)


def escribir_muestra(sel):
    rng = random.Random(SEED + 99)
    cuotas = {1: 17, 2: 17, 3: 17, 4: 17, 5: 16, 6: 16}
    lineas = [
        "# ¿Verdad o disparate? — muestra de 100 frases para revisar (1-oct)",
        "",
        "**Marca las que estén mal, sean dudosas o suenen raras.** La ambigüedad es el mayor riesgo de este juego: una frase",
        "dudosa frustra. Cada frase trae su respuesta (✔ verdad / ✘ disparate) y, en los disparates, su clase y la corrección",
        "que se muestra al errar. Salen al azar con semilla fija de `docs/frases-muestra-disparate.md` (generador:",
        "`python tools/frases/disparate.py`); las frases salen de una base propia en `tools/frases/conocimiento.py`.",
        "",
        "Tipos: 1 corta · 2 con complemento o adjetivo · 3 negación · 4 frase con pausa («, que …,») · 5 todos / algunos / ningún · 6 comparación y orden.",
        "",
    ]
    n = 0
    for t in range(1, 7):
        V = [f for f in sel[t] if f["v"]]
        D = [f for f in sel[t] if not f["v"]]
        rng.shuffle(V)
        rng.shuffle(D)
        q = cuotas[t]
        grupo = V[: q // 2 + q % 2] + D[: q // 2]
        rng.shuffle(grupo)
        lineas += [f"## Tipo {t} · {NOMBRES_TIPO[t]}", "", "| # | Frase | Respuesta | Clase | Corrección |", "|---|---|---|---|---|"]
        for f in grupo:
            n += 1
            ok = "✔ verdad" if f["v"] else "✘ disparate"
            lineas.append(f"| {n} | {f['f']} | {ok} | {f['c'] or '—'} | {f['r'] or '—'} |")
        lineas.append("")
    with open(OUT_MD, "w", encoding="utf8", newline="\n") as fh:
        fh.write("\n".join(lineas))


def main():
    cand = candidatos()
    sel = seleccion(cand)
    for t in range(1, 7):
        V, D = cand[t]
        nv = sum(1 for f in sel[t] if f["v"])
        print(f"tipo {t}: candidatas {len(V)} verdades / {len(D)} disparates -> elegidas {nv} / {len(sel[t]) - nv}")
    if "--stats" in sys.argv:
        return
    escribir_json(sel)
    escribir_muestra(sel)
    print(OUT_JSON)
    print(OUT_MD)


if __name__ == "__main__":
    main()
