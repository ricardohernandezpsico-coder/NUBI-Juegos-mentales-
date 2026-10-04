"""Pruebas del banco de «En la punta de la lengua» (python -m unittest, desde tools/punta).

Revisan el JSON que lee el juego (Resources/Lexico/punta_banco.json) contra las reglas de docs/diseno-punta-de-la-lengua.md §4.
Las que necesitan las fuentes de tools/lexico/fuentes/ (fuera de git) se saltan si no están.
"""
import collections
import json
import os
import sys
import unittest

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.join(os.path.dirname(os.path.dirname(HERE)), "tools", "lexico"))
import banco as B  # noqa: E402

FUENTES = os.path.join(os.path.dirname(HERE), "lexico", "fuentes")
HAY_FUENTES = os.path.exists(os.path.join(FUENTES, "word_info.csv")) and os.path.exists(os.path.join(FUENTES, "es_ES.dic"))


def cargar():
    with open(B.OUT_JSON, encoding="utf8") as f:
        return json.load(f)


class TestBanco(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.datos = cargar()
        cls.palabras = cls.datos["palabras"]

    def test_version_y_tamano(self):
        self.assertEqual(1, self.datos["version"])
        self.assertGreaterEqual(len(self.palabras), 400)

    def test_campos(self):
        for w in self.palabras:
            self.assertEqual({"p", "l", "d", "n", "b", "z", "c"}, set(w), w)
            self.assertIn(w["n"], B.NIVELES, w["p"])
            self.assertIn(w["b"], range(1, 7), w["p"])
            self.assertTrue(w["c"], w["p"])

    def test_sin_repetidos(self):
        palabras = [w["p"] for w in self.palabras]
        self.assertEqual(len(palabras), len(set(palabras)), "palabra repetida")
        defs = [w["d"].lower() for w in self.palabras]
        self.assertEqual(len(defs), len(set(defs)), "definición repetida")

    def test_letras_validas(self):
        for w in self.palabras:
            self.assertEqual(B.fichas(w["p"]), w["l"], w["p"])
            self.assertTrue(set(w["l"]) <= B.LETRAS_FICHA, "%s: %s" % (w["p"], w["l"]))
            self.assertEqual(len(w["p"]), len(w["l"]), w["p"])
            self.assertTrue(B.MIN_LEN <= len(w["l"]) <= B.MAX_LEN, w["p"])
            self.assertEqual(w["p"], w["p"].lower(), "la palabra va en minúsculas: " + w["p"])

    def test_la_definicion_no_lleva_la_palabra_ni_su_raiz(self):
        for w in self.palabras:
            self.assertFalse(B.tiene_la_palabra(w["p"], w["d"]), "%s: %s" % (w["p"], w["d"]))

    def test_definicion_corta_y_ordenada(self):
        for w in self.palabras:
            d = w["d"]
            self.assertLessEqual(len(d), B.MAX_DEF, "%s (%d): %s" % (w["p"], len(d), d))
            self.assertGreaterEqual(len(d), 8, w["p"])
            self.assertEqual(d, d.strip(), w["p"])
            self.assertTrue(d[0].isupper() or d[0] in "¡¿«", "%s: empieza con minúscula" % w["p"])
            self.assertNotIn("  ", d, w["p"])
            self.assertTrue(d[-1] in ".?!»", "%s: sin punto final" % w["p"])

    def test_temas_excluidos(self):
        for w in self.palabras:
            self.assertIsNone(B.tema_excluido(w["p"], w["d"]), "%s: %s" % (w["p"], w["d"]))

    def test_nivel_coherente_con_frecuencia_y_largo(self):
        for w in self.palabras:
            self.assertEqual(B.nivel_de(w["z"], len(w["p"])), w["n"], w["p"])
        # Más corta y más común nunca sube el nivel.
        for largo in range(4, 11):
            niveles = [B.nivel_de(z / 10.0, largo) for z in range(60, 20, -1)]  # de más a menos común
            self.assertEqual(sorted(niveles), niveles, "largo %d" % largo)
        for z in (5.5, 4.5, 3.5, 2.5):
            niveles = [B.nivel_de(z, n) for n in range(4, 11)]
            self.assertEqual(sorted(niveles), niveles, "zipf %s" % z)

    def test_nivel_1_es_facil_y_nivel_5_dificil(self):
        for w in self.palabras:
            if w["n"] == 1:
                self.assertLessEqual(len(w["p"]), 7, w["p"])
                self.assertGreaterEqual(w["z"], 3.7, w["p"])
            if w["n"] == 5:
                self.assertGreaterEqual(len(w["p"]), 6, w["p"])
        medio = {n: sum(len(w["p"]) for w in self.palabras if w["n"] == n) / max(1, sum(1 for w in self.palabras if w["n"] == n)) for n in B.NIVELES}
        for n in range(1, 5):
            self.assertLess(medio[n], medio[n + 1], "el largo medio debe crecer con el nivel: %s" % medio)
        zipf = {n: sum(w["z"] for w in self.palabras if w["n"] == n) / max(1, sum(1 for w in self.palabras if w["n"] == n)) for n in B.NIVELES}
        self.assertGreater(zipf[1], zipf[5], zipf)

    def test_equilibrio_por_nivel(self):
        por_nivel = collections.Counter(w["n"] for w in self.palabras)
        for n in B.NIVELES:
            self.assertGreaterEqual(por_nivel[n], 50, "nivel %d: %s" % (n, dict(por_nivel)))
            self.assertLessEqual(por_nivel[n], 0.35 * len(self.palabras), "nivel %d: %s" % (n, dict(por_nivel)))

    def test_equilibrio_por_categoria(self):
        por_cat = collections.Counter(w["c"] for w in self.palabras)
        self.assertGreaterEqual(len(por_cat), 10, dict(por_cat))
        for c, n in por_cat.items():
            self.assertLessEqual(n, 0.25 * len(self.palabras), "demasiadas de «%s»: %s" % (c, dict(por_cat)))
        # Cada nivel mezcla varias categorías: no hay un nivel «de animales».
        for nivel in B.NIVELES:
            cats = collections.Counter(w["c"] for w in self.palabras if w["n"] == nivel)
            self.assertGreaterEqual(len(cats), 8, "nivel %d: %s" % (nivel, dict(cats)))
            self.assertLessEqual(max(cats.values()), 0.35 * sum(cats.values()), "nivel %d: %s" % (nivel, dict(cats)))
        # Lo concreto pesa más que lo abstracto.
        abstracto = sum(por_cat[c] for c in ("emoción", "idea"))
        self.assertLess(abstracto, 0.15 * len(self.palabras))

    def test_no_hay_dos_palabras_con_las_mismas_letras(self):
        # dos palabras con las mismas fichas (valle/llave) harían que armar la otra, que también es correcta, parezca un error
        por_fichas = collections.defaultdict(list)
        for w in self.palabras:
            por_fichas[tuple(sorted(w["l"]))].append(w["p"])
        for fichas, lista in por_fichas.items():
            if len(lista) > 1:
                self.fail("anagramas entre sí (confundirían las fichas): %s" % lista)


@unittest.skipUnless(HAY_FUENTES, "faltan las fuentes de tools/lexico/fuentes (SPALEX, Hunspell)")
class TestContraLasFuentes(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        import meteoros as M
        from propios import ProperNames
        cls.M = M
        cls.spalex = M.load_spalex()
        cls.lemas = M.load_lemmas()
        cls.propios = ProperNames.load()
        cls.palabras = cargar()["palabras"]

    def test_todas_estan_en_spalex_con_su_frecuencia_y_banda(self):
        for w in self.palabras:
            self.assertIn(w["p"], self.spalex, w["p"])
            self.assertEqual(round(self.spalex[w["p"]][1], 2), w["z"], w["p"])
            self.assertEqual(self.M.band_of(self.spalex[w["p"]][0]) or 6, w["b"], w["p"])

    def test_sin_nombres_propios(self):
        for w in self.palabras:
            if self.propios.is_proper(w["p"]):
                self.assertIn(w["p"], self.lemas, "nombre propio: " + w["p"])

    def test_el_json_esta_al_dia_con_las_listas(self):
        self.assertEqual(B.construir(verbose=False), self.palabras, "regenerar con: python tools/punta/banco.py")

    def test_las_listas_no_pierden_palabras(self):
        palabras = {w["p"] for w in self.palabras}
        perdidas = [p for p, *_ in B.leer_datos() if p not in palabras]
        self.assertEqual([], perdidas, "palabras de las listas que no llegaron al banco (ver banco.py)")


if __name__ == "__main__":
    unittest.main()
