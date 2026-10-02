"""Pruebas del banco de grupos de "La estrella intrusa":  python -m unittest test_grupos   (desde tools/intrusa).

Lo más importante es la UNICIDAD: en ningún grupo puede haber otra palabra que sea "la intrusa" con una regla que se pueda nombrar.
Estas pruebas la vuelven a comprobar con un código distinto del generador, sobre el archivo ya escrito.
"""
import itertools
import json
import os
import sys
import unittest

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import asociaciones  # noqa: E402
import grupos as G  # noqa: E402
import lexico  # noqa: E402

with open(G.OUT_JSON, encoding="utf8") as _f:
    DATOS = json.load(_f)
GRUPOS = DATOS["grupos"]
NOMBRE_A_CLAVE = {r.nombre: r.clave for r in G.REGLAS.values()}


def propiedades(w):
    """Todo lo que la base dice (o duda) de una palabra, escrito aparte del generador."""
    p = G.PALABRAS[w]
    return set(p.tags) | set(p.sec) | {t for (x, t) in lexico.DUDOSO if x == w}


class Banco(unittest.TestCase):
    def test_al_menos_150_grupos_por_tipo(self):
        for t in range(1, 7):
            self.assertGreaterEqual(sum(1 for g in GRUPOS if g["t"] == t), 150, f"tipo {t}")

    def test_formato_de_cada_grupo(self):
        for g in GRUPOS:
            self.assertEqual(set(g), {"i", "t", "p", "x", "n", "o", "c", "a", "e", "r", "k"})
            self.assertEqual(len(g["p"]), 4, g)
            self.assertEqual(len(set(g["p"]) | {g["x"]}), 5, f"palabras repetidas: {g}")
            self.assertEqual(len(g["o"]), 3)
            self.assertIn(g["c"], (0, 1, 2))
            self.assertEqual(g["o"][g["c"]], g["n"])
            self.assertTrue(g["e"].endswith(" no."), g["e"])
            self.assertIn(g["x"], g["e"])
            self.assertEqual(len(g["i"]), 8)

    def test_sin_duplicados(self):
        self.assertEqual(len({g["i"] for g in GRUPOS}), len(GRUPOS), "ids repetidos")
        self.assertEqual(len({(frozenset(g["p"]), g["x"]) for g in GRUPOS}), len(GRUPOS), "mismo grupo dos veces")

    def test_unicidad_ninguna_otra_intrusa_posible(self):
        """Para cada x distinta de la intrusa, las otras 4 (con la intrusa) no comparten ninguna propiedad que x no tenga."""
        for g in GRUPOS:
            cinco = g["p"] + [g["x"]]
            for x in g["p"]:
                otras = [w for w in cinco if w != x]
                comunes = set.intersection(*(propiedades(w) for w in otras))
                self.assertFalse(comunes - propiedades(x), f"{g['p']} + {g['x']}: «{x}» también podría ser la intrusa ({sorted(comunes - propiedades(x))})")

    def test_la_regla_es_de_las_cuatro_y_no_de_la_intrusa(self):
        for g in GRUPOS:
            k = g["k"]
            self.assertEqual(NOMBRE_A_CLAVE[g["n"]], k)
            for w in g["p"]:
                self.assertIn(k, G.PALABRAS[w].tags, f"{w} debe cumplir «{k}»")
                self.assertNotIn((w, k), lexico.DUDOSO, f"{w} es dudosa para «{k}»")
            self.assertFalse(G.PALABRAS[g["x"]].puede(k), f"{g['x']} no debe poder cumplir «{k}»")

    def test_opciones_del_bonus_una_correcta_y_dos_falsas(self):
        for g in GRUPOS:
            falsas = [o for i, o in enumerate(g["o"]) if i != g["c"]]
            self.assertEqual(len(set(g["o"])), 3, g)
            for o in falsas:
                clave = NOMBRE_A_CLAVE[o]
                cuantas = sum(1 for w in g["p"] if G.PALABRAS[w].puede(clave))
                self.assertLessEqual(cuantas, 2, f"«{o}» podría pasar por verdadera en {g['p']}")
                self.assertFalse(G._parientes(clave, g["k"]), f"«{o}» es prima de «{g['n']}»")

    def test_trampas_bien_formadas(self):
        pares = set(asociaciones.pares())
        for g in GRUPOS:
            if g["t"] in G.CON_TRAMPA:
                self.assertIn(g["a"], g["p"], g)
                self.assertIn((g["a"], g["x"]), pares, f"{g['x']} no «va con» {g['a']}")
                self.assertTrue(g["r"].startswith(f"va con {g['a']}, pero no "), g["r"])
                # la intrusa va con UNA sola palabra del grupo (si no, la trampa sería doble)
                con = [w for w in g["p"] if (w, g["x"]) in pares]
                self.assertEqual(len(con), 1, f"{g['x']} va con varias: {con}")
            else:
                self.assertEqual((g["a"], g["r"]), ("", ""))

    def test_explicacion_con_genero_y_articulo(self):
        for g in GRUPOS:
            todas = all(G.PALABRAS[w].genero == "f" for w in g["p"])
            self.assertTrue(g["e"].startswith("Todas " if todas else "Todos "), g["e"])
            self.assertIn(f"; {G.articulo(g['x'])} {g['x']} no.", g["e"])
        self.assertEqual(G.articulo("águila"), "el")
        self.assertEqual(G.articulo("agua"), "el")
        self.assertEqual(G.articulo("hueso"), "el")
        self.assertEqual(G.articulo("cuchara"), "la")

    def test_tipos_1_y_2_y_el_mundo_de_la_intrusa(self):
        for g in GRUPOS:
            supers = {G.PALABRAS[w].super for w in g["p"]}
            si = G.PALABRAS[g["x"]].super
            if g["t"] == 1:
                self.assertNotIn(si, supers, f"amplia: {g}")
            if g["t"] == 2:
                self.assertEqual(len(supers), 1, f"vecina: {g}")

    def test_los_5_subconjuntos_solo_uno_comparte_la_regla(self):
        for g in GRUPOS:
            cinco = g["p"] + [g["x"]]
            comparten = [c for c in itertools.combinations(cinco, 4) if all(g["k"] in G.PALABRAS[w].tags for w in c)]
            self.assertEqual(len(comparten), 1, g)
            self.assertNotIn(g["x"], comparten[0])


class Filtros(unittest.TestCase):
    def test_palabras_ambiguas_y_excluidas_fuera(self):
        usadas = {w for g in GRUPOS for w in g["p"] + [g["x"]]}
        self.assertFalse(usadas & lexico.AMBIGUAS, usadas & lexico.AMBIGUAS)
        for w in usadas:
            self.assertTrue(not G.lex.excluded_word(w) or w in G.PERMITIDAS, f"«{w}» está en excluir.txt")
        for w in ("ratón", "banco", "planta", "sierra", "cola", "mango", "huevo", "regla", "hacha", "burro", "zorro"):
            self.assertNotIn(w, usadas)

    def test_la_base_no_tiene_palabras_ambiguas(self):
        self.assertFalse(set(G.PALABRAS) & lexico.AMBIGUAS)

    def test_el_filtro_deja_afuera_lo_sensible(self):
        for w in G.DESCARTADAS_FILTRO:
            self.assertNotIn(w, G.PALABRAS)
        self.assertIn("huevo", G.DESCARTADAS_FILTRO)
        self.assertIn("hacha", G.DESCARTADAS_FILTRO)

    def test_todas_las_asociaciones_usan_palabras_de_la_base(self):
        for a, b in G.PARES:
            self.assertIn(a, G.PALABRAS)
            self.assertIn(b, G.PALABRAS)
            self.assertNotEqual(a, b)

    def test_las_asociaciones_son_de_tipos_distintos(self):
        """Si A y B comparten una categoría fina no sería una trampa (ver el encabezado de asociaciones.py)."""
        finas = {"animal", "fruta", "verdura", "bebida", "herramienta", "utensilio de cocina", "mueble", "prenda", "vehículo",
                 "instrumento", "parte del cuerpo", "flor", "árbol", "astro", "clima", "paisaje", "material escolar", "oficio", "lugar"}
        for a, b in G.PARES:
            comunes = (G.PALABRAS[a].tags & G.PALABRAS[b].tags) & finas
            self.assertFalse(comunes, f"{a}–{b} comparten {comunes}")


class Verificador(unittest.TestCase):
    def test_detecta_una_segunda_intrusa(self):
        # perro, gato, conejo, loro son mascotas y la oveja no: con "oveja" entre los cuatro, ella sería la intrusa (y el loro, un impostor)
        self.assertIsNotNone(G.ambigua(["perro", "gato", "conejo", "oveja"], "loro"))
        self.assertIsNone(G.ambigua(["perro", "gato", "vaca", "caballo"], "mesa"))

    def test_los_dudosos_cuentan_como_si_las_tuvieran(self):
        # "pingüino" duda de 'alas': no puede ser la intrusa de una regla de alas ni miembro
        self.assertTrue(G.lexico_dudoso("pingüino", "alas"))
        self.assertFalse(G.intrusa_valida(G.REGLAS["alas"], "pingüino", {"animal"}, 4))

    def test_opciones_no_incluyen_la_regla_ni_primas(self):
        import random
        regla = G.REGLAS["sirve para lavar"]
        falsas = G.opciones_falsas(["jabón", "champú", "lavadora", "esponja"], "cómoda", regla, random.Random(1))
        self.assertTrue(falsas)
        for r in falsas:
            self.assertNotEqual(r.clave, "sirve para limpiar")


class Determinismo(unittest.TestCase):
    def test_generar_dos_veces_da_lo_mismo(self):
        a, _ = G.generar()
        b, _ = G.generar()
        self.assertEqual(json.dumps(a, ensure_ascii=False, sort_keys=True), json.dumps(b, ensure_ascii=False, sort_keys=True))

    def test_el_archivo_es_lo_que_genera_el_programa(self):
        g, _ = G.generar()
        esperado = [x for t in range(1, 7) for x in g[t]]
        self.assertEqual(esperado, GRUPOS)


if __name__ == "__main__":
    unittest.main()
