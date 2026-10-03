"""Pruebas del filtro de nombres propios de «Lluvia de meteoros» (tarea 21c).  Desde tools/lexico:  python -m unittest test_propios"""
import json
import os
import unittest

import propios
from propios import ProperNames, norm, read_list

HERE = os.path.dirname(os.path.abspath(__file__))
JSON_PATH = os.path.join(os.path.dirname(os.path.dirname(HERE)), "unity", "NeuroVidaCore", "Assets", "Resources", "Lexico", "meteoros_es.json")

COMMON_NAMES = ["mario", "maria", "jose", "juan", "carlos", "luis", "pedro", "pablo", "miguel", "antonio", "manuel", "javier", "daniel",
                "lucia", "carmen", "laura", "marta", "sara", "ana", "paula", "sofia", "elena", "isabel", "andrea", "claudia", "nuria", "lara", "aldo"]


class ProperNamesTest(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.names = ProperNames.load()
        with open(JSON_PATH, encoding="utf8") as f:
            cls.lex = json.load(f)

    def test_mario_y_otros_nombres_comunes_son_nombres_propios(self):
        for w in COMMON_NAMES:
            self.assertTrue(self.names.is_proper(w), w)

    def test_la_comparacion_no_distingue_tildes_ni_mayusculas_ni_la_enie(self):
        for w in ("MARIO", "Mario", "María", "MARÍA", "Nuño", "nuno", "Adrián", "adrian", "JOSÉ", "Ángel", "angel"):
            self.assertTrue(self.names.is_proper(w), w)
        self.assertEqual("nuno", norm("Nuño"))
        self.assertEqual("maria", norm("MARÍA"))

    def test_tambien_las_formas_con_s_final(self):
        self.assertTrue(self.names.is_proper("marios"))
        self.assertTrue(self.names.is_proper("lucias"))

    def test_apellidos_lugares_y_marcas(self):
        for w in ("garcia", "fernandez", "rodriguez", "martinez", "gonzalez", "espana", "madrid", "colombia", "bogota", "nintendo", "pikachu", "adidas", "pokemon", "zelda"):
            self.assertTrue(self.names.is_proper(w), w)

    def test_una_palabra_comun_no_es_un_nombre(self):
        for w in ("casa", "perro", "ventana", "silla", "cocina", "libro", "cuchara", "puerta"):
            self.assertFalse(self.names.is_proper(w), w)

    def test_las_listas_propias_tienen_el_tamano_pedido(self):
        given = {norm(w) for p in sorted(os.listdir(propios.LISTS_DIR)) if p.startswith("nombres-") for w in read_list(os.path.join(propios.LISTS_DIR, p))}
        surnames = {norm(w) for w in read_list(os.path.join(propios.LISTS_DIR, "apellidos.txt"))}
        self.assertGreaterEqual(len(given), 1500)
        self.assertGreaterEqual(len(surnames), 500)

    def test_ninguna_inventada_del_lexico_es_un_nombre_propio(self):
        bad = [x["p"] for x in self.lex["inventadas"] if self.names.is_proper(x["p"])]
        self.assertEqual([], bad)

    def test_mario_y_los_nombres_comunes_no_estan_entre_las_inventadas(self):
        inv = {x["p"] for x in self.lex["inventadas"]}
        for w in COMMON_NAMES:
            self.assertNotIn(w, inv, w)

    def test_ninguna_palabra_real_del_banco_es_solo_un_nombre_propio(self):
        # Las reales salen de los lemas en MINÚSCULA de Hunspell (nunca de las entradas con mayúscula) y no pueden ser un nombre que se tome por tal
        # (revision-manual.txt: nacho, puebla, pancho...). Las que son nombre Y palabra común (luna, justo, norma, margarita) siguen: son reales.
        manual = {l.strip().lower() for l in open(os.path.join(HERE, "revision-manual.txt"), encoding="utf8") if l.strip() and not l.startswith("#")}
        words = [x["p"] for x in self.lex["palabras"]]
        for w in ("nacho", "puebla", "pancho", "amador", "aquilino", "escobar", "olmedo", "roque", "rebeca"):
            self.assertIn(w, manual)
            self.assertNotIn(w, words)
        only_names = [w for w in words if self.names.source_of(w) in ("nombre", "apellido") and w not in {"alba", "justo", "luna", "norma", "margarita", "jacinto", "valeriana", "sierra", "socorro", "pinto", "gallardo", "montero", "caballero", "mulero", "alegría", "ambrosía", "máximo", "peña", "costa", "valle", "marfil", "niña", "línea", "colonia", "ciudadela", "ecuatorial", "honda", "ópera", "montaña", "rincón"}]
        self.assertEqual([], only_names, "palabras reales del banco que son un nombre propio y no figuran como palabra común: decidir a mano")

    def test_los_descartes_se_registran(self):
        path = os.path.join(HERE, "descartes-propios.txt")
        self.assertTrue(os.path.exists(path))
        dropped = [l.split("\t")[0] for l in open(path, encoding="utf8") if l.strip() and not l.startswith("#")]
        inv = {x["p"] for x in self.lex["inventadas"]}
        for w in dropped:
            self.assertNotIn(w, inv)

    @unittest.skipUnless(os.path.exists(propios.DIC_PATH), "falta tools/lexico/fuentes/es_ES.dic (fuera de git)")
    def test_hunspell_aporta_las_entradas_con_mayuscula(self):
        caps = {norm(w) for w in propios.hunspell_proper()}
        self.assertIn("mario", caps)
        self.assertGreater(len(caps), 1000)
        self.assertTrue(caps <= self.names.all)


if __name__ == "__main__":
    unittest.main()
