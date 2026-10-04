"""Banco de definiciones de «En la punta de la lengua» (reemplaza a Anagramas; ver docs/diseno-punta-de-la-lengua.md §4).

Lee las listas escritas a mano en tools/punta/datos/*.txt (una palabra por línea: «palabra | definición propia»; las
líneas «## categoría» cambian de categoría) y escribe unity/NeuroVidaCore/Assets/Resources/Lexico/punta_banco.json, que
el juego lee con Resources.Load.

Las definiciones son PROPIAS, escritas para Nubi: no se copian ni se parafrasean del diccionario de la RAE ni de otros.
La palabra se acepta solo si:
  - está en SPALEX (la misma fuente de frecuencia de Cosecha y Meteoros: la banda sale de ahí) y mide 4-10 letras;
  - no es un nombre propio (tools/lexico/propios.py), salvo que sea también un sustantivo común del diccionario (mesa, gato…);
  - no cae en la lista de temas excluidos de acá abajo (TEMAS).

Uso:  python tools/punta/banco.py            (necesita las fuentes de tools/lexico/fuentes/, fuera de git)
"""
import glob
import json
import os
import re
import sys
import unicodedata

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(os.path.dirname(HERE))
sys.path.insert(0, os.path.join(ROOT, "tools", "lexico"))
DATOS = os.path.join(HERE, "datos")
OUT_JSON = os.path.join(ROOT, "unity", "NeuroVidaCore", "Assets", "Resources", "Lexico", "punta_banco.json")

MIN_LEN, MAX_LEN = 4, 10
MAX_DEF = 90
NIVELES = (1, 2, 3, 4, 5)

# Temas que NO van (Ricardo, 3-oct): enfermedad, muerte, violencia, groserías, política, religión. Raíces (se busca dentro de la palabra
# y de la definición, sin tildes).
TEMAS = (
    "enferm", "muert", "muer", "morir", "matar", "asesin", "cadaver", "funeral", "tumba", "entierr", "cementerio", "luto",
    "violen", "arma", "pistol", "bomba", "guerra", "herid", "dolor", "cancer", "virus", "epidemi", "veneno", "droga",
    "cigarr", "alcohol", "borrach", "cerveza", "vino", "tabaco",
    "politic", "president", "elecci", "partido", "gobiern", "diputad", "senador", "ministro", "militar", "ejercito", "soldado",
    "relig", "dios", "iglesia", "cristian", "catoli", "biblia", "rezar", "rezan", "oracion", "santo", "diablo", "demonio",
    "infierno",
    "sexo", "sexual", "desnud", "culo", "teta", "puta", "mierda", "joder",
)

LETRAS_FICHA = set("ABCDEFGHIJKLMNOPQRSTUVWXYZÑ")


def quitar_tildes(s):
    """Sin tildes, dejando la ñ y convirtiendo la ü en u."""
    s = s.replace("ñ", "\0").replace("Ñ", "\1")
    s = "".join(c for c in unicodedata.normalize("NFD", s) if unicodedata.category(c) != "Mn")
    return s.replace("\0", "ñ").replace("\1", "Ñ")


def fichas(palabra):
    """Letras de las fichas: mayúsculas, sin tildes, con Ñ (la ü cuenta como U)."""
    return quitar_tildes(palabra).upper()


def tokens(texto):
    return re.findall(r"[a-zñ]+", quitar_tildes(texto.lower()))


def raiz(palabra):
    """Raíz de 4 letras (o la palabra entera si es más corta), sin tildes."""
    return quitar_tildes(palabra.lower())[:4]


def tiene_la_palabra(palabra, definicion):
    """¿La definición lleva la palabra o su raíz de 4 letras al comienzo de alguna palabra suya?"""
    r = raiz(palabra)
    return any(t.startswith(r) for t in tokens(definicion))


def tema_excluido(palabra, definicion):
    """Devuelve la raíz de tema excluido que aparezca, o None. Se busca al comienzo de una palabra (palabra o definición)."""
    for t in tokens(palabra) + tokens(definicion):
        for raiz_tema in TEMAS:
            if t.startswith(raiz_tema):
                return raiz_tema
    return None


# Dificultad de una palabra = qué tan poco común es + qué tan larga es. La banda SPALEX (prevalencia) casi no distingue entre
# palabras de este tipo (todas las conocen más del 93 %), así que el nivel sale de la frecuencia de uso (zipf, SPALEX/EsPal),
# que es la misma fuente, pero más fina. La banda 1-6 se guarda igual, por coherencia con Cosecha y Meteoros.
CORTES_NIVEL = (1.6, 2.8, 3.9, 5.0)


def dificultad(zipf, largo):
    """0 (muy común y corta) … 8 (poco común y larga): 4 puntos por poco común (zipf 4.9 → 0, 2.7 → 4) y 4 por larga (4 → 0, 10 → 4)."""
    f = min(max((4.9 - zipf) / 0.55, 0.0), 4.0)
    l = min(max((largo - 4) * (4.0 / 6.0), 0.0), 4.0)
    return f + l


def nivel_de(zipf, largo):
    """Nivel 1-5. El 1 son palabras muy comunes de 4-5 letras; el 5, las menos comunes y largas (diseno-punta-de-la-lengua.md §4)."""
    d = dificultad(zipf, largo)
    return 1 + sum(d >= c for c in CORTES_NIVEL)


def leer_datos():
    """[(palabra, definicion, categoria, archivo, linea)] en el orden de los archivos."""
    filas = []
    for ruta in sorted(glob.glob(os.path.join(DATOS, "*.txt"))):
        cat = "objeto"
        with open(ruta, encoding="utf8") as f:
            lineas = f.read().splitlines()
        for n, linea in enumerate(lineas, 1):
            linea = linea.strip()
            if not linea or (linea.startswith("#") and not linea.startswith("##")):
                continue
            if linea.startswith("##"):
                cat = linea[2:].strip()
                continue
            if "|" not in linea:
                raise ValueError("%s:%d sin «|»: %s" % (os.path.basename(ruta), n, linea))
            p, d = (x.strip() for x in linea.split("|", 1))
            filas.append((p, d, cat, os.path.basename(ruta), n))
    return filas


def construir(verbose=True):
    import meteoros as M
    from propios import ProperNames

    spalex = M.load_spalex()
    propios = ProperNames.load()
    # Un sustantivo común que también existe como nombre propio (mesa, gato, lago…) NO es un nombre propio: la lista de
    # Hunspell trae también las entradas con mayúscula (la ciudad «Mesa»). Basta con que esté en minúscula en el diccionario.
    lemas = M.load_lemmas()
    palabras, descartes = [], []
    vistas = set()
    for p, d, cat, arch, n in leer_datos():
        quien = "%s:%d «%s»" % (arch, n, p)
        if p in vistas:
            descartes.append((quien, "repetida"))
            continue
        vistas.add(p)
        if not (MIN_LEN <= len(p) <= MAX_LEN):
            descartes.append((quien, "largo %d fuera de %d-%d" % (len(p), MIN_LEN, MAX_LEN)))
            continue
        f = fichas(p)
        if not all(c in LETRAS_FICHA for c in f):
            descartes.append((quien, "letras inválidas"))
            continue
        if p not in spalex:
            descartes.append((quien, "no está en SPALEX"))
            continue
        if propios.is_proper(p) and p not in lemas:
            descartes.append((quien, "nombre propio"))
            continue
        tema = tema_excluido(p, d)
        if tema:
            descartes.append((quien, "tema excluido (%s)" % tema))
            continue
        banda = M.band_of(spalex[p][0]) or 6
        zipf = round(spalex[p][1], 2)
        palabras.append({"p": p, "l": f, "d": d, "n": nivel_de(zipf, len(p)), "b": banda, "z": zipf, "c": cat})
    palabras.sort(key=lambda w: (w["n"], w["p"]))
    if verbose:
        print("palabras:", len(palabras), " descartadas:", len(descartes))
        for q, r in descartes:
            print("  -", q, "→", r)
    return palabras


def escribir(palabras):
    os.makedirs(os.path.dirname(OUT_JSON), exist_ok=True)
    datos = {"version": 1, "palabras": palabras}
    with open(OUT_JSON, "w", encoding="utf8", newline="\n") as f:
        json.dump(datos, f, ensure_ascii=False, separators=(",", ":"))
        f.write("\n")


def resumen(palabras):
    from collections import Counter
    por_nivel = Counter(w["n"] for w in palabras)
    por_cat = Counter(w["c"] for w in palabras)
    print("por nivel:", {n: por_nivel[n] for n in NIVELES})
    print("por categoría:", dict(por_cat))
    cruce = Counter((w["n"], w["c"]) for w in palabras)
    for n in NIVELES:
        print(" nivel", n, {c: cruce[(n, c)] for c in sorted(por_cat) if cruce[(n, c)]})


if __name__ == "__main__":
    ws = construir()
    resumen(ws)
    if "--no-escribir" not in sys.argv:
        escribir(ws)
        print("escrito", OUT_JSON)
