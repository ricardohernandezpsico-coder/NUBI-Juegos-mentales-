"""Pruebas de las rondas de Cosecha de palabras.  python -m unittest test_rondas   (desde tools/cosecha)

Revisan el archivo ya generado (unity/.../Resources/Lexico/cosecha_es.json) y que el generador sea determinista.
Con COSECHA_REGENERAR=1 también reconstruyen TODO desde las fuentes (tarda ~2 min) y exigen el mismo archivo."""
import hashlib
import json
import os
import unittest
from collections import Counter

import rondas

JSON_PATH = rondas.OUT_JSON
BAND_PREV = {1: 99.5, 2: 98.0, 3: 95.5, 4: 91.0, 5: 82.0, 6: 60.0}


def norm(w):
    return w.translate(str.maketrans("áéíóúü", "aeiouu"))


class RondasTest(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        with open(JSON_PATH, encoding="utf8") as f:
            cls.data = json.load(f)
        cls.rondas = cls.data["rondas"]

    def test_hay_600_rondas_repartidas_en_10_niveles(self):
        self.assertEqual(len(self.rondas), 600)
        por_nivel = Counter(r["nivel"] for r in self.rondas)
        self.assertEqual(sorted(por_nivel), list(range(1, 11)))
        self.assertTrue(all(n == 60 for n in por_nivel.values()), por_nivel)

    def test_ids_unicos_y_letras_validas(self):
        self.assertEqual(len({r["id"] for r in self.rondas}), len(self.rondas))
        for r in self.rondas:
            self.assertEqual(len(r["letras"]), 7, r["id"])
            self.assertTrue(all(c in rondas.LETTERS for c in r["letras"]), r["letras"])   # sin k, w ni tildes
            self.assertEqual(r["id"], hashlib.sha1("".join(sorted(r["letras"])).encode()).hexdigest()[:8])

    def test_toda_palabra_se_puede_formar_con_las_letras(self):
        for r in self.rondas:
            letras = Counter(r["letras"])
            for w in r["palabras"]:
                n = norm(w["p"])
                self.assertTrue(3 <= len(n) <= 7, (r["id"], w["p"]))
                self.assertFalse(Counter(n) - letras, f'{w["p"]} no sale de {r["letras"]}')

    def test_sin_palabras_duplicadas_en_una_ronda(self):
        for r in self.rondas:
            claves = [norm(w["p"]) for w in r["palabras"]]
            self.assertEqual(len(claves), len(set(claves)), r["id"])

    def test_cada_ronda_cumple_los_minimos(self):
        for r in self.rondas:
            comunes = [w for w in r["palabras"] if w.get("comun")]
            self.assertGreaterEqual(len(comunes), rondas.MIN_COMMON, r["id"])
            estrellas = [w for w in comunes if w.get("estrella")]
            self.assertGreaterEqual(len(estrellas), 1, r["id"])
            for w in estrellas:
                self.assertEqual(len(norm(w["p"])), 7)

    def test_estrella_es_toda_palabra_de_7_letras(self):
        for r in self.rondas:
            for w in r["palabras"]:
                self.assertEqual(bool(w.get("estrella")), len(norm(w["p"])) == 7, (r["id"], w["p"]))

    def test_comun_es_banda_1_a_3_y_ninguna_oculta_es_comun(self):
        for r in self.rondas:
            for w in r["palabras"]:
                self.assertEqual(bool(w.get("comun")), w["b"] <= 3 and not w.get("oculta"), (r["id"], w))
                if w.get("oculta"):
                    self.assertNotIn("comun", w)

    def test_ninguna_estrella_comun_esta_oculta_ni_en_la_lista_de_exclusion(self):
        for r in self.rondas:
            for w in r["palabras"]:
                if w.get("comun"):
                    self.assertFalse(rondas.lex.excluded_word(w["p"]), w["p"])
                    self.assertNotIn(norm(w["p"]), rondas.EXTRA_OCULTAS)
                    self.assertNotIn(w["p"], rondas.NO_VALIDAS)

    def test_las_palabras_excluidas_van_ocultas(self):
        ocultas = [w for r in self.rondas for w in r["palabras"] if w.get("oculta")]
        self.assertTrue(ocultas, "debería haber alguna oculta")
        for w in ocultas:
            self.assertTrue(rondas.lex.excluded_word(w["p"]) or norm(w["p"]) in rondas.EXTRA_OCULTAS, w["p"])

    def test_no_hay_dos_rondas_con_las_mismas_letras_ni_con_6_en_comun(self):
        sets = [Counter(r["letras"]) for r in self.rondas]
        for i in range(len(sets)):
            for j in range(i + 1, len(sets)):
                self.assertLess(sum((sets[i] & sets[j]).values()), 6, (self.rondas[i]["letras"], self.rondas[j]["letras"]))

    def test_las_letras_no_forman_una_palabra_a_primera_vista(self):
        validas = {norm(w["p"]) for r in self.rondas for w in r["palabras"]}
        for r in self.rondas:
            self.assertNotIn(r["letras"], validas, r["letras"])

    def test_los_niveles_suben_en_dificultad(self):
        """Las comunes disponibles bajan de ~40 (nivel 1) a ~15 (nivel 10)."""
        prom = []
        for lv in range(1, 11):
            sub = [r for r in self.rondas if r["nivel"] == lv]
            prom.append(sum(sum(1 for w in r["palabras"] if w.get("comun")) for r in sub) / len(sub))
        self.assertGreater(prom[0], 33)
        self.assertLess(prom[-1], 17)
        for a, b in zip(prom, prom[1:]):
            self.assertGreaterEqual(a + 0.5, b)       # nunca sube de un nivel al siguiente (salvo ruido)

    def test_sin_tildes_mal_puestas_ni_letras_raras_en_las_palabras(self):
        for r in self.rondas:
            for w in r["palabras"]:
                self.assertRegex(w["p"], r"^[a-záéíóúüñ]+$")
                self.assertNotRegex(norm(w["p"]), "[kw]")

    def test_formatos_para_jsonutility(self):
        self.assertIn("rondas", self.data)
        for r in self.rondas:
            self.assertEqual(set(r), {"id", "nivel", "letras", "palabras"})
            for w in r["palabras"]:
                self.assertLessEqual(set(w), {"p", "b", "comun", "estrella", "oculta"})
                self.assertTrue(w["p"] and 1 <= w["b"] <= 6)


class OcultasTest(unittest.TestCase):
    PALABRAS = ["raja", "rajas", "coca", "cocas", "toreo", "toreos", "torear", "óseo", "óseos", "ósea", "óseas", "caza", "cazar"]

    def test_las_palabras_pedidas_nunca_son_comunes(self):
        with open(JSON_PATH, encoding="utf8") as f:
            data = json.load(f)
        for r in data["rondas"]:
            for w in r["palabras"]:
                if w["p"] in self.PALABRAS:
                    self.assertTrue(w.get("oculta") and not w.get("comun"), w)
        for p in self.PALABRAS:
            self.assertTrue(rondas.lex.excluded_word(p), p)


class DeterminismoTest(unittest.TestCase):
    def test_misma_entrada_mismas_rondas(self):
        """Reconstruye un diccionario con las palabras del archivo y arma las rondas dos veces: tienen que salir iguales."""
        with open(JSON_PATH, encoding="utf8") as f:
            data = json.load(f)
        words = {}
        for r in data["rondas"]:
            for w in r["palabras"]:
                words[norm(w["p"])] = {"p": w["p"], "prev": BAND_PREV[w["b"]], "zipf": 4.0}

        def correr():
            cands, hidden = rondas.make_rounds(words)
            chosen, level_of = rondas.pick_rounds(cands, words)
            return json.dumps(rondas.build_json(chosen, level_of, words, hidden), ensure_ascii=False, sort_keys=True)

        a, b = correr(), correr()
        self.assertEqual(a, b)
        self.assertGreater(len(json.loads(a)["rondas"]), 100)


@unittest.skipUnless(os.environ.get("COSECHA_REGENERAR") == "1", "COSECHA_REGENERAR=1 para reconstruir desde las fuentes")
class RegenerarTest(unittest.TestCase):
    def test_el_archivo_es_el_que_sale_de_las_fuentes(self):
        words = rondas.build_dictionary()
        cands, hidden = rondas.make_rounds(words)
        previous, letters = rondas.load_previous(JSON_PATH)      # el generador conserva las rondas que siguen cumpliendo
        chosen, level_of = rondas.pick_rounds(cands, words, previous)
        nuevo = rondas.build_json(chosen, level_of, words, hidden, letters)
        with open(JSON_PATH, encoding="utf8") as f:
            self.assertEqual(json.load(f), nuevo)


if __name__ == "__main__":
    unittest.main()
