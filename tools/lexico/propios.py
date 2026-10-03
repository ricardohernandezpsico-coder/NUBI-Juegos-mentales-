"""Filtro de nombres propios para las inventadas de «Lluvia de meteoros» (tarea 21c).

Por qué: una inventada como «Mario» la toca cualquiera (es un nombre), y entonces el juego castiga a quien acierta. Ninguna inventada puede
coincidir, SIN TILDES NI MAYÚSCULAS (y con la ñ como n), con:

  a) nombres de pila frecuentes en español (España y Latinoamérica)      tools/lexico/propios/nombres-varones.txt, nombres-mujeres.txt
  b) apellidos frecuentes                                                  tools/lexico/propios/apellidos.txt
  c) entradas con mayúscula inicial de Hunspell es_ES (los nombres propios del diccionario que ya se usa al construir: nombres, países,
     ciudades españolas, siglas...)                                         tools/lexico/fuentes/es_ES.dic (fuera de git)
  d) países, capitales y ciudades grandes de habla hispana, marcas y personajes muy conocidos (lista propia, corta, revisada a mano)
                                                                            tools/lexico/propios/lugares-y-marcas.txt

Las listas (a), (b) y (d) son PROPIAS: se armaron a mano, sin descargar nada (Ricardo decide si se suma una lista pública con atribución,
como los nombres frecuentes del INE de España). También se descartan las formas con -s final (Marios, Lucías...).

Determinista. Uso:   from propios import ProperNames;  ProperNames.load().is_proper("mario")   # True
Prueba:              python -m unittest test_propios   (desde tools/lexico)
"""
import glob
import os
import unicodedata

HERE = os.path.dirname(os.path.abspath(__file__))
LISTS_DIR = os.path.join(HERE, "propios")
DIC_PATH = os.path.join(HERE, "fuentes", "es_ES.dic")


def norm(s):
    """Minúsculas, sin tildes ni diéresis y con la ñ como n: la comparación con una inventada no distingue nada de eso."""
    s = unicodedata.normalize("NFD", s.lower())
    return "".join(c for c in s if unicodedata.category(c) != "Mn")


def read_list(path):
    """Las palabras de una lista (varias por línea; # = comentario). Sin normalizar."""
    out = []
    with open(path, encoding="utf8") as f:
        for line in f:
            if line.lstrip().startswith("#"):
                continue
            out += line.split()
    return out


def hunspell_proper(dic_path=DIC_PATH):
    """Las entradas de es_ES.dic con mayúscula inicial (nombres propios y siglas). Vacío si no está el diccionario (fuera de git)."""
    out = []
    if not os.path.exists(dic_path):
        return out
    with open(dic_path, encoding="utf8", errors="replace") as f:
        next(f)
        for line in f:
            lem = line.split("/")[0].strip()
            if lem and lem[0].isupper():
                out.append(lem)
    return out


class ProperNames:
    def __init__(self, given, surnames, places_brands, hunspell):
        self.given = {norm(w) for w in given}
        self.surnames = {norm(w) for w in surnames}
        self.places_brands = {norm(w) for w in places_brands}
        self.hunspell = {norm(w) for w in hunspell}
        base = self.given | self.surnames | self.places_brands | self.hunspell
        # plurales y formas con -s: «marios», «lucias»
        self.all = base | {w + "s" for w in base if len(w) >= 3}

    @classmethod
    def load(cls, lists_dir=LISTS_DIR, dic_path=DIC_PATH):
        given = []
        for p in sorted(glob.glob(os.path.join(lists_dir, "nombres-*.txt"))):
            given += read_list(p)
        surnames = read_list(os.path.join(lists_dir, "apellidos.txt"))
        places = read_list(os.path.join(lists_dir, "lugares-y-marcas.txt"))
        return cls(given, surnames, places, hunspell_proper(dic_path))

    def is_proper(self, word):
        return norm(word) in self.all

    def source_of(self, word):
        """De qué lista viene la coincidencia (para el informe): nombre, apellido, hunspell, lugar/marca o None."""
        w = norm(word)
        if w.endswith("s"):
            w2 = w[:-1]
        else:
            w2 = w
        for label, s in (("nombre", self.given), ("apellido", self.surnames), ("hunspell", self.hunspell), ("lugar/marca", self.places_brands)):
            if w in s or w2 in s:
                return label
        return None

    def counts(self):
        return {"nombres": len(self.given), "apellidos": len(self.surnames), "hunspell con mayúscula": len(self.hunspell),
                "lugares/marcas/personajes": len(self.places_brands)}
