"""Generador del banco de grupos de "La estrella intrusa" (docs/diseno-estrella-intrusa.md, secciones 4 y 5).

Escribe unity/NeuroVidaCore/Assets/Resources/Lexico/intrusa_es.json (y la muestra con --muestra). Determinista.

Cada grupo: 4 palabras que comparten una REGLA + 1 intrusa, el nombre de la constelación, 3 opciones para "¿Qué las une?" (1 correcta
y 2 plausibles pero FALSAS para ese grupo), la explicación ("Todos son animales; el hueso no.") y, en las trampas, con qué palabra del
grupo "va" la intrusa ("va con perro, pero no es un animal").

6 tipos (tabla de la sección 4): 1 amplia · 2 vecina · 3 uso · 4 material-lugar-parte · 5 trampa · 6 regla + trampa.

VERIFICADOR DE UNICIDAD (obligatorio): para cada grupo se prueba cada uno de los 5 subconjuntos de 4. Si en algún subconjunto "sin
la palabra x" las otras cuatro (con la intrusa) comparten una propiedad de la base que x NO tiene, entonces x también podría ser la
intrusa y el grupo se descarta. Los tags dudosos (DUDOSO) cuentan como "podría tenerla": solo hacen el filtro más estricto.
"""
import argparse
import hashlib
import itertools
import json
import os
import random
import sys
from collections import Counter

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(os.path.dirname(HERE))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.join(ROOT, "tools", "lexico"))
import asociaciones  # noqa: E402
import lexico  # noqa: E402
import meteoros as lex  # noqa: E402  (reutiliza las exclusiones: excluir.txt)

OUT_JSON = os.path.join(ROOT, "unity", "NeuroVidaCore", "Assets", "Resources", "Lexico", "intrusa_es.json")
OUT_MD = os.path.join(ROOT, "docs", "intrusa-muestra.md")
SEED = 20261002
POR_TIPO = 160
MAX_COMO_MIEMBRO = 9
MAX_COMO_INTRUSA = 10

NOMBRES_TIPO = {1: "amplia", 2: "vecina", 3: "uso", 4: "material / lugar / parte", 5: "trampa", 6: "regla + trampa"}

# Palabras que excluir.txt saca (insultos, sexo, violencia, enfermedad) pero que aquí son cotidianas y sin doble sentido en este
# contexto. Todo lo demás de excluir.txt queda FUERA (huevo, regla, burro, zorro, hacha, hospital...).
PERMITIDAS = {"perro", "cerdo", "cuchillo", "camisa", "imán", "canario", "caparazón"}
SUEÑAN_A = {"agua", "águila", "arpa", "ancla"}      # femeninas que llevan "el" (a tónica)

# ------------------------------------------------------------------ reglas nombrables
# clave | clase | nombre de la constelación | predicado plural | predicado singular
REGLAS_TXT = """
animal|tipo|Animales|son animales|es un animal
mamífero|tipo|Mamíferos|son mamíferos|es un mamífero
ave|tipo|Aves|son aves|es un ave
pez|tipo|Peces|son peces|es un pez
insecto|tipo|Insectos|son insectos|es un insecto
reptil|tipo|Reptiles|son reptiles|es un reptil
fruta|tipo|Frutas|son frutas|es una fruta
verdura|tipo|Verduras|son verduras|es una verdura
bebida|tipo|Bebidas|son bebidas|es una bebida
herramienta|tipo|Herramientas|son herramientas|es una herramienta
utensilio de cocina|tipo|Utensilios de cocina|son utensilios de cocina|es un utensilio de cocina
mueble|tipo|Muebles|son muebles|es un mueble
prenda|tipo|Prendas de vestir|son prendas de vestir|es una prenda de vestir
calzado|tipo|Calzado|son calzado|es calzado
vehículo|tipo|Vehículos|son vehículos|es un vehículo
instrumento|tipo|Instrumentos musicales|son instrumentos musicales|es un instrumento musical
parte del cuerpo|tipo|Partes del cuerpo|son partes del cuerpo|es una parte del cuerpo
flor|tipo|Flores|son flores|es una flor
árbol|tipo|Árboles|son árboles|es un árbol
astro|tipo|Astros del cielo|son astros|es un astro
clima|tipo|Cosas del clima|son cosas del clima|es una cosa del clima
paisaje|tipo|Lugares de la naturaleza|son lugares de la naturaleza|es un lugar de la naturaleza
material escolar|tipo|Material escolar|son material escolar|es material escolar
juguete|tipo|Juguetes|son juguetes|es un juguete
aparato|tipo|Aparatos de la casa|son aparatos de la casa|es un aparato de la casa
oficio|tipo|Oficios|son oficios|es un oficio
lugar|tipo|Lugares|son lugares|es un lugar
cuatro patas|rasgo|Tienen cuatro patas|tienen cuatro patas|tiene cuatro patas
seis patas|rasgo|Tienen seis patas|tienen seis patas|tiene seis patas
pelo|rasgo|Tienen pelo|tienen pelo|tiene pelo
mascota|rasgo|Son mascotas|son mascotas|es una mascota
granja|rasgo|Viven en la granja|viven en la granja|vive en la granja
salvaje|rasgo|Son animales salvajes|son salvajes|es salvaje
come carne|rasgo|Comen carne|comen carne|come carne
come hierba|rasgo|Comen hierba|comen hierba|come hierba
vuela|rasgo|Vuelan|vuelan|vuela
crece en árbol|rasgo|Crecen en los árboles|crecen en los árboles|crece en los árboles
crece bajo la tierra|rasgo|Crecen bajo la tierra|crecen bajo la tierra|crece bajo la tierra
lácteo|rasgo|Son lácteos|son lácteos|es lácteo
dulce|rasgo|Son dulces|son dulces|es dulce
alas|parte|Tienen alas|tienen alas|tiene alas
plumas|parte|Tienen plumas|tienen plumas|tiene plumas
aletas|parte|Tienen aletas|tienen aletas|tiene aletas
garras|parte|Tienen garras|tienen garras|tiene garras
pétalos|parte|Tienen pétalos|tienen pétalos|tiene pétalos
cuerdas|parte|Tienen cuerdas|tienen cuerdas|tiene cuerdas
ruedas|parte|Tienen ruedas|tienen ruedas|tiene ruedas
motor|parte|Tienen motor|tienen motor|tiene motor
vive en el mar|lugar|Viven en el mar|viven en el mar|vive en el mar
vive en el agua|lugar|Viven en el agua|viven en el agua|vive en el agua
está en el cielo|lugar|Están en el cielo|están en el cielo|está en el cielo
se usa en la cocina|lugar|Se usan en la cocina|se usan en la cocina|se usa en la cocina
se usa en el jardín|lugar|Se usan en el jardín|se usan en el jardín|se usa en el jardín
va por tierra|lugar|Van por tierra|van por tierra|va por tierra
va por el agua|lugar|Van por el agua|van por el agua|va por el agua
va por el aire|lugar|Van por el aire|van por el aire|va por el aire
va al fuego|lugar|Van al fuego|van al fuego|va al fuego
se pone en los pies|lugar|Se ponen en los pies|se ponen en los pies|se pone en los pies
se pone en la cabeza|lugar|Se ponen en la cabeza|se ponen en la cabeza|se pone en la cabeza
sirve para cortar|uso|Sirven para cortar|sirven para cortar|sirve para cortar
sirve para escribir|uso|Sirven para escribir|sirven para escribir|sirve para escribir
sirve para sentarse|uso|Sirven para sentarse|sirven para sentarse|sirve para sentarse
sirve para dormir|uso|Sirven para dormir|sirven para dormir|sirve para dormir
sirve para beber|uso|Sirven para beber|sirven para beber|sirve para beber
sirve para comer|uso|Sirven para comer|sirven para comer|sirve para comer
sirve para cocinar|uso|Sirven para cocinar|sirven para cocinar|sirve para cocinar
sirve para abrigarse|uso|Sirven para abrigarse|sirven para abrigarse|sirve para abrigarse
sirve para alumbrar|uso|Sirven para alumbrar|sirven para alumbrar|sirve para alumbrar
sirve para limpiar|uso|Sirven para limpiar|sirven para limpiar|sirve para limpiar
sirve para abrir|uso|Sirven para abrir|sirven para abrir|sirve para abrir
sirve para guardar cosas|uso|Sirven para guardar cosas|sirven para guardar cosas|sirve para guardar cosas
sirve para pintar|uso|Sirven para pintar|sirven para pintar|sirve para pintar
sirve para transportar|uso|Sirven para transportar|sirven para transportar|sirve para transportar
sirve para respirar|uso|Sirven para respirar|sirven para respirar|sirve para respirar
sirve para lavar|uso|Sirven para lavar|sirven para lavar|sirve para lavar
se golpea|uso|Se golpean|se golpean|se golpea
se sopla|uso|Se soplan|se soplan|se sopla
"""


# Para los grupos del MISMO mundo (tipos 2 y 4): la intrusa debe tener una propiedad que demuestre que NO cumple la regla (si no,
# el hecho de que la base no le anote la propiedad no prueba nada: "libélula" también es salvaje aunque no lo diga).
COMPLEMENTO = {
    "cuatro patas": {"dos patas", "seis patas", "ocho patas", "aletas", "se arrastra", "brazos"},
    "seis patas": {"cuatro patas", "dos patas", "ocho patas", "aletas", "se arrastra", "brazos"},
    "pelo": {"plumas", "escamas", "aletas"},
    "mascota": {"salvaje", "granja"}, "granja": {"salvaje", "mascota"}, "salvaje": {"doméstico", "mascota", "granja"},
    "come carne": {"come hierba"}, "come hierba": {"come carne"},
    "vuela": {"cuatro patas", "aletas", "se arrastra", "ocho patas", "brazos"},
    "alas": {"cuatro patas", "aletas", "se arrastra", "ocho patas", "brazos", "escamas"},
    "plumas": {"pelo", "escamas", "aletas", "seis patas", "ocho patas"},
    "aletas": {"cuatro patas", "plumas", "pelo", "alas", "seis patas"},
    "escamas": {"pelo", "plumas"},
    "garras": {"aletas", "brazos", "come hierba"},
    "hojas": {"bebida", "lácteo", "dulce"},
    "pétalos": {"tronco", "verde"},
    "cuerdas": {"se sopla", "se golpea"},
    "ruedas": {"flota"},
    "motor": {"sin motor"},
    "vive en el mar": {"cuatro patas", "alas", "seis patas", "ocho patas", "plumas"},
    "vive en el agua": {"cuatro patas", "alas", "seis patas", "ocho patas", "pelo"},
    "está en el cielo": {"paisaje", "duro"},
    "va por tierra": {"va por el agua", "va por el aire"}, "va por el agua": {"va por tierra", "va por el aire"},
    "va por el aire": {"va por tierra", "va por el agua"},
    "va al fuego": {"se usa en la mesa"},
    "crece en árbol": {"crece bajo la tierra", "crece en racimo", "crece en el suelo"},
    "crece bajo la tierra": {"crece en árbol", "crece en racimo", "crece en el suelo"},
    "se golpea": {"se sopla", "cuerdas"}, "se sopla": {"se golpea", "cuerdas"},
    "se pone en los pies": {"se pone en la cabeza", "se pone en las manos", "se pone en el cuerpo", "se pone en el cuello"},
    "se pone en la cabeza": {"se pone en los pies", "se pone en las manos", "se pone en el cuerpo", "se pone en el cuello"},
}
# "Vecina" (tipo 2): la intrusa es de una categoría HERMANA de la regla (4 mamíferos + 1 ave; frutas + 1 verdura), nunca de una lejana.
# Una regla sin hermanas no se usa en este tipo. Prendas y calzado no se mezclan; las bebidas tampoco tienen hermana.
HERMANAS = {
    "mamífero": {"ave", "pez", "insecto", "reptil"}, "ave": {"mamífero", "pez", "insecto", "reptil"},
    "pez": {"mamífero", "ave", "insecto", "reptil"}, "insecto": {"mamífero", "ave", "pez", "reptil"},
    "reptil": {"mamífero", "ave", "pez", "insecto"},
    "fruta": {"verdura"}, "verdura": {"fruta"},
    "flor": {"árbol"}, "árbol": {"flor"},
    "astro": {"clima", "paisaje"}, "clima": {"astro", "paisaje"}, "paisaje": {"astro", "clima"},
    "herramienta": {"utensilio de cocina"}, "utensilio de cocina": {"herramienta"},
    "mueble": {"aparato"}, "aparato": {"mueble"},
}

# De qué mundo puede venir la intrusa según la clase de regla (si la regla no está en COMPLEMENTO).
SUPER_INTRUSA = {"lugar": {"objeto"}, "uso": {"objeto", "cuerpo"}}

TIPOS_POR_NIVEL = {
    1: ("tipo",),
    2: ("tipo", "rasgo"),
    3: ("uso",),
    4: ("lugar", "parte"),
    5: ("tipo",),
    6: ("uso", "lugar", "parte", "rasgo"),
}
CON_TRAMPA = {5, 6}


class Regla:
    def __init__(self, clave, clase, nombre, plural, singular):
        self.clave, self.clase, self.nombre, self.plural, self.singular = clave, clase, nombre, plural, singular


REGLAS = {}
for _l in REGLAS_TXT.strip().splitlines():
    _p = _l.split("|")
    REGLAS[_p[0]] = Regla(*_p)


# ------------------------------------------------------------------ la base de palabras
class Palabra:
    def __init__(self, texto, genero, tags, sec):
        self.texto, self.genero = texto, genero
        self.tags = tags            # propiedades ciertas (con lo que implican)
        self.dudosos = set()        # propiedades que "a veces" tiene
        self.sec = sec              # propiedades de otro sentido de la palabra
        self.super = None

    def vale(self):
        """Propiedades para VERIFICAR: todas las que podría tener (ciertas, dudosas y de otro sentido)."""
        return self.tags | self.dudosos | self.sec

    def cierta(self, t):
        return t in self.tags and t not in self.dudosos

    def puede(self, t):
        return t in self.tags or t in self.dudosos or t in self.sec


def expandir(tags):
    out = set()
    pendientes = list(tags)
    while pendientes:
        t = pendientes.pop()
        if t in out:
            continue
        out.add(t)
        pendientes.extend(lexico.IMPLICA.get(t, []))
    return out


def superclase(tags):
    for t, s in (("animal", "animal"), ("comida", "comida"), ("bebida", "comida"), ("planta", "planta"), ("oficio", "oficio"),
                 ("lugar", "lugar"), ("cuerpo", "cuerpo"), ("natural", "naturaleza"), ("objeto", "objeto")):
        if t in tags:
            return s
    return "otro"


def cargar():
    palabras, descartadas = {}, []
    for linea in lexico.PALABRAS.splitlines():
        linea = linea.strip()
        if not linea or linea.startswith("#"):
            continue
        partes = linea.split("|")
        texto, genero = partes[0], partes[1]
        tags = expandir([t.strip() for t in partes[2].split(",") if t.strip()])
        sec = set()
        for extra in partes[3:]:
            if extra.startswith("sec:"):
                sec = expandir([t.strip() for t in extra[4:].split(",") if t.strip()])
        if texto in lexico.AMBIGUAS or (lex.excluded_word(texto) and texto not in PERMITIDAS):
            descartadas.append(texto)
            continue
        p = Palabra(texto, genero, tags, sec)
        p.super = superclase(tags)
        palabras[texto] = p
    for w, t in lexico.DUDOSO:
        if w in palabras:
            palabras[w].dudosos.add(t)
    return palabras, descartadas


PALABRAS, DESCARTADAS_FILTRO = cargar()
PARES = [(a, b) for a, b in asociaciones.pares() if a in PALABRAS and b in PALABRAS]
ASOCIADOS = {}
for _a, _b in PARES:
    ASOCIADOS.setdefault(_b, set()).add(_a)


# ------------------------------------------------------------------ gramática
def articulo(w):
    p = PALABRAS[w]
    return "el" if p.genero == "m" or w in SUEÑAN_A else "la"


def todos(miembros):
    return "Todas" if all(PALABRAS[m].genero == "f" for m in miembros) else "Todos"


# ------------------------------------------------------------------ el verificador de unicidad
def ambigua(miembros, intrusa):
    """Si otra palabra podría ser la intrusa: devuelve (palabra, propiedad) que lo demuestra, o None."""
    for x in miembros:
        otros = [m for m in miembros if m != x] + [intrusa]
        comunes = set.intersection(*(PALABRAS[w].vale() for w in otros))
        extra = comunes - PALABRAS[x].vale()
        if extra:
            return x, sorted(extra)[0]
    return None


# Reglas casi sinónimas: una opción falsa nunca puede ser parienta de la correcta (si no, las dos podrían ser verdaderas).
CLUSTERS = [
    {"sirve para lavar", "sirve para limpiar"}, {"sirve para guardar cosas", "sirve para llevar cosas"},
    {"se usa en la cocina", "sirve para cocinar", "va al fuego", "utensilio de cocina"},
    {"sirve para comer", "se usa en la mesa", "utensilio de cocina"}, {"vuela", "va por el aire", "alas"},
    {"va por el agua", "vive en el agua", "vive en el mar", "aletas"}, {"va por tierra", "ruedas", "vehículo"},
    {"sirve para transportar", "vehículo", "va por tierra", "va por el agua", "va por el aire", "ruedas"},
    {"sirve para escribir", "material escolar", "se usa en la escuela", "sirve para pintar"},
    {"está en el cielo", "astro", "clima"}, {"sirve para abrigarse", "prenda", "se pone en los pies", "se pone en la cabeza"},
    {"sirve para beber", "bebida", "utensilio de cocina", "sirve para servir líquidos"},
    {"fruta", "dulce", "crece en árbol", "cítrico"}, {"mascota", "granja", "salvaje", "animal"},
    {"calzado", "se pone en los pies"}, {"herramienta", "sirve para cortar", "sirve para pintar"},
    {"instrumento", "se golpea", "se sopla", "cuerdas"}, {"sirve para alumbrar", "aparato"},
]


def _parientes(a, b):
    return any(a in c and b in c for c in CLUSTERS)


def opciones_falsas(miembros, intrusa, regla, rng):
    """Dos nombres plausibles pero FALSOS para las 4 palabras (ninguna regla que las cuatro puedan cumplir)."""
    cand = []
    for clave, r in REGLAS.items():
        if clave == regla.clave or r.nombre == regla.nombre:
            continue
        cuantos = sum(1 for m in miembros if PALABRAS[m].puede(clave))
        if r.clase in ("rasgo", "parte"):
            # una opción de rasgo solo es "falsa" si de cada palabra se SABE si la cumple o no (la base no calla)
            comp = COMPLEMENTO.get(clave)
            if comp is None or any(not (PALABRAS[m].tags & ({clave} | comp)) for m in miembros):
                continue
        if cuantos > 2 or _parientes(clave, regla.clave):
            continue                                   # podría pasar por verdadera (la cumplen casi todas, o es prima de la correcta)
        puntaje = cuantos * 2 + (3 if PALABRAS[intrusa].puede(clave) else 0) + (2 if r.clase == regla.clase else 0)
        cand.append((-puntaje, rng.random(), r))
    cand.sort(key=lambda c: (c[0], c[1]))
    elegidas = []
    for _, _, r in cand:
        if all(r.clave != e.clave for e in elegidas) and not _solapadas(r, elegidas):
            elegidas.append(r)
        if len(elegidas) == 2:
            return elegidas
    return None


def _solapadas(r, elegidas):
    """Dos opciones falsas no pueden ser casi lo mismo (p. ej. "Son mascotas" y "Son animales" nunca juntas con sus parientes)."""
    for e in elegidas:
        if e.clase == r.clase and (e.clave in lexico.IMPLICA.get(r.clave, []) or r.clave in lexico.IMPLICA.get(e.clave, [])):
            return True
    return False


# ------------------------------------------------------------------ armar grupos
def miembros_de(clave):
    return sorted(w for w, p in PALABRAS.items() if p.cierta(clave))


def intrusa_valida(regla, w, super_m, tipo):
    """¿Puede 'w' ser la intrusa de esta regla? (la base no le anota la propiedad Y hay algo que demuestre que de verdad no la tiene)"""
    p = PALABRAS[w]
    if p.puede(regla.clave) or (w, regla.clave) in lexico.DUDOSO:
        return False
    comp = COMPLEMENTO.get(regla.clave)
    if tipo == 2 and regla.clase == "tipo":
        return bool(p.tags & HERMANAS.get(regla.clave, set()))
    if tipo in (2, 3, 4):
        if comp is not None:
            return bool(p.tags & comp)
        if regla.clase in ("rasgo", "parte") and tipo != 3:
            return False
    if regla.clase in SUPER_INTRUSA and comp is None and p.super not in SUPER_INTRUSA[regla.clase]:
        return False
    return True


def candidatos_intrusa(regla, miembros, tipo):
    out = []
    super_m = {PALABRAS[m].super for m in miembros}
    for w, p in PALABRAS.items():
        if w in miembros or not intrusa_valida(regla, w, super_m, tipo):
            continue
        if tipo == 1 and (p.super in super_m or p.super == "otro"):
            continue                                  # amplia: de OTRO mundo
        if tipo in (2, 3, 4) and p.super not in super_m:
            continue                                  # vecina, uso, lugar: del mismo mundo
        out.append(w)
    return sorted(out)


def construir(tipo, miembros, intrusa, regla, rng, pareja=""):
    falsas = opciones_falsas(miembros, intrusa, regla, rng)
    if falsas is None:
        return None
    opciones = [regla.nombre, falsas[0].nombre, falsas[1].nombre]
    rng.shuffle(opciones)
    correcta = opciones.index(regla.nombre)
    a = articulo(intrusa)
    explicacion = f"{todos(miembros)} {regla.plural}; {a} {intrusa} no."
    trampa = f"va con {pareja}, pero no {regla.singular}" if pareja else ""
    ident = hashlib.sha1((",".join(sorted(miembros)) + "|" + intrusa).encode("utf8")).hexdigest()[:8]
    orden = list(miembros)
    rng.shuffle(orden)
    return {"i": ident, "t": tipo, "p": orden, "x": intrusa, "n": regla.nombre, "o": opciones, "c": correcta, "a": pareja,
            "e": explicacion, "r": trampa, "k": regla.clave}


def generar():
    estadisticas = Counter()
    grupos = {t: [] for t in range(1, 7)}
    vistos_global = set()
    conjuntos_usados = Counter()
    for tipo in range(1, 7):
        rng = random.Random(f"{SEED}:{tipo}")
        vistos = set()
        como_miembro, como_intrusa = Counter(), Counter()
        reglas = [r for r in REGLAS.values() if r.clase in TIPOS_POR_NIVEL[tipo] and len(miembros_de(r.clave)) >= 4]
        por_regla = Counter()
        tope_regla = max(14, 3 * POR_TIPO // len(reglas))
        intentos = 0
        while len(grupos[tipo]) < POR_TIPO and intentos < 150000:
            intentos += 1
            regla = rng.choice(reglas)
            if por_regla[regla.clave] >= tope_regla:
                continue
            pool = [w for w in miembros_de(regla.clave)]
            if len(pool) < 4:
                continue
            pareja = ""
            if tipo in CON_TRAMPA:
                a, b = rng.choice(PARES)
                if not PALABRAS[a].cierta(regla.clave):
                    continue
                otros = [w for w in pool if w != a]
                if len(otros) < 3:
                    continue
                miembros = [a] + rng.sample(otros, 3)
                intrusa, pareja = b, a
                if b in miembros or not intrusa_valida(regla, b, {PALABRAS[m].super for m in miembros}, tipo):
                    estadisticas["trampa: la intrusa sí cumple o no es clara"] += 1
                    continue
                if len(ASOCIADOS.get(b, set()) & set(miembros)) > 1:
                    estadisticas["trampa doble"] += 1
                    continue
            else:
                miembros = rng.sample(pool, 4)
                if tipo == 2 and len({PALABRAS[m].super for m in miembros}) > 1:
                    continue                                  # vecina: todo del mismo mundo
                cands = candidatos_intrusa(regla, miembros, tipo)
                if not cands:
                    continue
                intrusa = rng.choice(cands)
            if any(como_miembro[m] >= MAX_COMO_MIEMBRO for m in miembros) or como_intrusa[intrusa] >= MAX_COMO_INTRUSA:
                continue
            clave = (frozenset(miembros), intrusa)
            if clave in vistos or clave in vistos_global or conjuntos_usados[frozenset(miembros)] >= 4:
                continue
            vistos.add(clave)
            if any(lexico_dudoso(w, regla.clave) for w in miembros + [intrusa]):
                estadisticas["dudoso"] += 1
                continue
            malo = ambigua(miembros, intrusa)
            if malo:
                estadisticas["verificador: otra intrusa posible"] += 1
                continue
            g = construir(tipo, miembros, intrusa, regla, rng, pareja)
            if g is None:
                estadisticas["sin opciones falsas"] += 1
                continue
            vistos_global.add(clave)
            conjuntos_usados[frozenset(miembros)] += 1
            por_regla[regla.clave] += 1
            for m in miembros:
                como_miembro[m] += 1
            como_intrusa[intrusa] += 1
            grupos[tipo].append(g)
    return grupos, estadisticas


def lexico_dudoso(w, clave):
    return (w, clave) in lexico.DUDOSO


def resumen(grupos, est):
    print("palabras de la base:", len(PALABRAS), "· fuera por ambiguas / excluir.txt:", sorted(DESCARTADAS_FILTRO))
    print("pares de asociaciones usables:", len(PARES))
    for t in range(1, 7):
        print(f"  tipo {t} ({NOMBRES_TIPO[t]}): {len(grupos[t])} grupos")
    print("descartes del generador:", dict(est))


def escribir(grupos):
    todos_ = [g for t in range(1, 7) for g in grupos[t]]
    ids = [g["i"] for g in todos_]
    assert len(ids) == len(set(ids)), "ids repetidos"
    datos = {
        "version": 1, "idioma": "es",
        "fuente": "Base de significados propia (tools/intrusa/lexico.py) y asociaciones revisadas a mano; sin ítems de pruebas ajenas",
        "grupos": todos_,
    }
    os.makedirs(os.path.dirname(OUT_JSON), exist_ok=True)
    with open(OUT_JSON, "w", encoding="utf8", newline="\n") as f:
        json.dump(datos, f, ensure_ascii=False, separators=(",", ":"))
    print(OUT_JSON, os.path.getsize(OUT_JSON) // 1024, "KB")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--muestra", action="store_true")
    args = ap.parse_args()
    sys.stdout.reconfigure(encoding="utf8")
    grupos, est = generar()
    resumen(grupos, est)
    escribir(grupos)
    if args.muestra:
        import muestra
        muestra.escribir(grupos, OUT_MD)


if __name__ == "__main__":
    main()
