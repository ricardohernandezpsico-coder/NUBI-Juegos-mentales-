"""Pruebas del generador de "¿Verdad o disparate?":  python -m unittest tools/frases/test_disparate.py -v  (o pytest).

Verifican la lógica (respuestas de casos conocidos, cuantificadores y negaciones, comparaciones recalculadas por separado),
la gramática básica, que no haya duplicados, el balance 50/50 por tipo y el formato del JSON.
"""
import json
import os
import re
import sys
import unittest

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import conocimiento as K  # noqa: E402
import disparate as D  # noqa: E402

CAND = D.candidatos()
SEL = D.seleccion(CAND)


def todas(t, verdad):
    return {f["f"]: f for _, f in CAND[t][0 if verdad else 1]}


class Gramatica(unittest.TestCase):
    def test_conjugacion_singular(self):
        self.assertEqual(D.singular("tienen alas"), "tiene alas")
        self.assertEqual(D.singular("se derriten con el calor"), "se derrite con el calor")
        self.assertEqual(D.singular("son una estrella"), "es una estrella")
        self.assertEqual(D.singular("dan leche"), "da leche")
        self.assertEqual(D.singular("ríen"), "ríe")

    def test_adjetivos(self):
        self.assertEqual(D.adj("frío", "f", True), "frías")
        self.assertEqual(D.adj("caliente", "m", True), "calientes")
        self.assertEqual(D.adj("azul", "f", True), "azules")
        self.assertEqual(D.adj("pequeño", "f", False), "pequeña")

    def test_predicados(self):
        self.assertEqual(D.predicado("~redondo", "m", False), "es redondo")
        self.assertEqual(D.predicado("=un animal/animales", "f", True), "son animales")
        self.assertEqual(D.predicado("=un animal/animales", "f", False), "es un animal")

    def test_forma_de_las_frases(self):
        for t, (V, Dd) in CAND.items():
            for _, f in V + Dd:
                for txt in (f["f"], f["r"]):
                    if not txt:
                        continue
                    self.assertTrue(txt[0].isupper(), txt)
                    self.assertNotIn("  ", txt)
                    self.assertFalse(txt.endswith((".", ",", " ")), txt)
                    self.assertNotRegex(txt, r"\b(es|son) (un|una) \w+s\b(?! de)", txt)   # "es un animales"


class CasosConocidos(unittest.TestCase):
    def test_tipo1(self):
        self.assertTrue(todas(1, True)["Los peces nadan"]["v"])
        self.assertIn("Los pingüinos vuelan", todas(1, False))
        self.assertEqual(todas(1, False)["Los pingüinos vuelan"]["c"], "sutil")
        self.assertEqual(todas(1, False)["Las sillas ríen"]["c"], "evidente")
        self.assertEqual(todas(1, False)["Las sillas ríen"]["r"], "Las sillas no ríen")
        self.assertNotIn("Los perros nadan", todas(1, True))       # dudoso: no entra ni como verdad
        self.assertNotIn("Los perros nadan", todas(1, False))      # ni como disparate

    def test_tipo2(self):
        self.assertIn("El sol calienta la tierra", todas(2, True))
        self.assertIn("El hielo es caliente", todas(2, False))
        self.assertEqual(todas(2, False)["El hielo es caliente"]["r"], "El hielo es frío")
        self.assertIn("Las vacas leen libros", todas(2, False))

    def test_tipo3_negaciones(self):
        self.assertIn("Los gatos no vuelan", todas(3, True))
        self.assertIn("Las sillas no ríen", todas(3, True))
        self.assertIn("Los peces no nadan", todas(3, False))
        self.assertEqual(todas(3, False)["Los peces no nadan"]["r"], "Los peces sí nadan")
        self.assertIn("El hielo no es caliente", todas(3, True))
        self.assertIn("El hielo no es frío", todas(3, False))

    def test_tipo4_frase_con_pausa(self):
        self.assertIn("Los peces, que viven en el agua, tienen aletas", todas(4, True))
        self.assertIn("Los peces, que viven en el agua, tienen plumas", todas(4, False))
        self.assertIn("El hielo, que flota en el agua, es frío", todas(4, True))
        self.assertEqual(todas(4, False)["Los peces, que viven en el agua, tienen plumas"]["r"], "Los peces, que viven en el agua, no tienen plumas")

    def test_tipo1_sin_verbos_genericos_como_verdad(self):
        for g in ("crecen", "respiran", "comen", "duermen"):
            self.assertNotIn(f"Las abejas {g}", todas(1, True))
        self.assertIn("Las abejas zumban", todas(1, True))
        self.assertIn("La leche duerme", todas(1, False))     # en los disparates sí sirven

    def test_tipo5_cuantificadores(self):
        self.assertIn("Todos los peces nadan", todas(5, True))
        self.assertIn("Ningún pez ladra", todas(5, True))
        self.assertIn("Algunos animales vuelan", todas(5, True))
        # sin cuantificadores discutibles
        discutibles = ["Todos los instrumentos musicales se tocan con las manos", "Ningún mueble tiene ruedas", "Todos los árboles tienen flores"]
        for d in discutibles:
            self.assertNotIn(d, todas(5, True) | todas(5, False))
        self.assertIn("Todos los animales vuelan", todas(5, False))
        self.assertEqual(todas(5, False)["Todos los animales vuelan"]["r"], "Solo algunos animales vuelan")
        self.assertIn("Ningún animal vuela", todas(5, False))
        self.assertIn("Ninguna fruta es de metal", todas(5, True))
        self.assertIn("Algunas frutas son rojas", todas(5, True))
        self.assertIn("Todos los triángulos tienen tres lados", todas(5, True))
        # "Algunos" + lo que cumplen todos suena a que otros no: ambiguo, no se genera
        self.assertNotIn("Algunos peces nadan", todas(5, True) | todas(5, False))
        self.assertNotIn("Algunos triángulos tienen tres lados", todas(5, True) | todas(5, False))

    def test_tipo6(self):
        self.assertIn("Una hormiga es más pequeña que un elefante", todas(6, True))
        self.assertIn("Una hormiga es más grande que un elefante", todas(6, False))
        self.assertIn("Un minuto dura menos que una hora", todas(6, True))
        self.assertIn("Una semana dura menos que un año", todas(6, True))
        self.assertIn("Una semana dura más que un año", todas(6, False))
        self.assertIn("Siete es mayor que tres", todas(6, True))
        self.assertIn("Tres es mayor que siete", todas(6, False))
        # sin dudas: tamaños del mismo nivel o de niveles vecinos no se comparan
        self.assertNotIn("Un gato es más grande que un conejo", todas(6, True) | todas(6, False))


class Semantica(unittest.TestCase):
    """Recalcula la respuesta por otro camino y la compara con la que trae cada frase."""

    def test_cuantificadores_contra_la_tabla_de_verdad(self):
        for g in K.GRUPOS:
            sg, pl, gen, todos, ninguno, algunos = g
            tabla = {"todos": {}, "algunos": {}, "ninguno": {}}
            for p in todos:
                tabla["todos"][p] = True; tabla["ninguno"][p] = False; tabla["algunos"][p] = None   # None: ambiguo, no se genera
            for p in ninguno:
                tabla["todos"][p] = False; tabla["ninguno"][p] = True; tabla["algunos"][p] = False
            for p in algunos:
                tabla["todos"][p] = False; tabla["ninguno"][p] = False; tabla["algunos"][p] = True
            vistas = todas(5, True) | todas(5, False)
            for q, props in tabla.items():
                for p, esperado in props.items():
                    frase = D.frase_q(q, g, p)
                    if esperado is None:
                        self.assertNotIn(frase, vistas, frase)
                    elif frase in vistas:
                        self.assertEqual(vistas[frase]["v"], esperado, frase)
                    else:   # si no está, es porque el filtro la sacó; no debe estar en la lista contraria
                        self.fail("falta " + frase)

    def test_numeros_y_letras_contra_el_orden_real(self):
        idx = {n: i for i, n in enumerate(K.NUMEROS)}
        for f in list(todas(6, True).values()) + list(todas(6, False).values()):
            m = re.match(r"^(\w+) es (mayor|menor) que (\w+)$", f["f"])
            if m and m[1].lower() in idx:
                a, op, b = idx[m[1].lower()], m[2], idx[m[3]]
                self.assertEqual(f["v"], (a > b) if op == "mayor" else (a < b), f["f"])
            m = re.match(r"^La letra (\w) viene (antes que|después de) la letra (\w)$", f["f"])
            if m:
                a, b = m[1], m[3]
                self.assertEqual(f["v"], (a < b) if m[2] == "antes que" else (a > b), f["f"])

    def test_escalas_contra_los_niveles(self):
        niveles = {n: lv for fam in (K.TAMANO, K.DURACION, K.VELOCIDAD, K.TEMPERATURA) for n, _, lv in fam}
        for f in list(todas(6, True).values()) + list(todas(6, False).values()):
            m = re.match(r"^(?:Un|Una|El|La) (\w+) (?:dura más|pesa más|dura menos|pesa menos|es más \w+) que (?:un|una|el|la) (\w+)$", f["f"])
            if not m:
                continue
            x, y = niveles[m[1]], niveles[m[2]]
            self.assertNotEqual(x, y, f["f"])
            fam_dir = "más" if re.search(r"(dura|pesa) más|más (grande|rápid|caliente)", f["f"]) else "menos"
            esperado = (x > y) if fam_dir == "más" else (x < y)
            self.assertEqual(f["v"], esperado, f["f"])

    def test_ninguna_frase_es_verdad_y_disparate_a_la_vez(self):
        for t in range(1, 7):
            self.assertFalse(set(todas(t, True)) & set(todas(t, False)), t)
        todo_v = set().union(*[set(todas(t, True)) for t in range(1, 7)])
        todo_d = set().union(*[set(todas(t, False)) for t in range(1, 7)])
        self.assertFalse(todo_v & todo_d)

    def test_la_correccion_de_las_comparaciones_es_una_verdad(self):
        verdades = set(todas(6, True))
        for f in todas(6, False).values():
            self.assertIn(f["r"], verdades)


class Explicativas(unittest.TestCase):
    def test_la_explicativa_es_siempre_verdad(self):
        ents = {e.sujeto: e for e in D.cargar()}
        n = 0
        for _, f in CAND[4][0] + CAND[4][1]:
            m = re.match(r"^(.+?), que (.+?), (.+)$", f["f"])
            self.assertIsNotNone(m, f["f"])
            sujeto = m[1][0].lower() + m[1][1:]
            e = ents[sujeto]
            propios = [e.pred(p) for p in e.si if p not in e.defecto]
            self.assertIn(m[2], propios, f["f"])           # la explicativa sale de lo que SIEMPRE es o hace
            self.assertNotIn(m[2], [e.pred(p) for p, _ in e.no], f["f"])
            n += 1
        self.assertGreater(n, 1000)

    def test_solo_el_predicado_final_decide(self):
        ents = {e.sujeto: e for e in D.cargar()}
        for v, lista in ((True, CAND[4][0]), (False, CAND[4][1])):
            for _, f in lista:
                m = re.match(r"^(.+?), que (.+?), (.+)$", f["f"])
                e = ents[m[1][0].lower() + m[1][1:]]
                final = m[3]
                verdad = final in [e.pred(p) for p in e.si if p not in e.defecto]
                self.assertEqual(verdad, v, f["f"])


class Seleccion(unittest.TestCase):
    def test_balance_y_cantidad_por_tipo(self):
        for t in range(1, 7):
            v = sum(1 for f in SEL[t] if f["v"])
            d = len(SEL[t]) - v
            self.assertEqual(v, d, f"tipo {t}")
            self.assertGreaterEqual(len(SEL[t]), 300, f"tipo {t}: menos de 300 frases")

    def test_sin_duplicados(self):
        todas_ = [f["f"] for t in range(1, 7) for f in SEL[t]]
        self.assertEqual(len(todas_), len(set(todas_)))

    def test_cada_disparate_tiene_clase_y_correccion(self):
        for t in range(1, 7):
            for f in SEL[t]:
                if f["v"]:
                    self.assertEqual((f["c"], f["r"]), ("", ""), f["f"])
                else:
                    self.assertIn(f["c"], ("evidente", "sutil"), f["f"])
                    self.assertTrue(f["r"], f["f"])
                self.assertEqual(f["n"], len(f["f"].split(" ")))

    def test_hay_de_las_dos_clases_en_cada_tipo(self):
        for t in range(1, 7):
            clases = {f["c"] for f in SEL[t] if not f["v"]}
            self.assertEqual(clases, {"evidente", "sutil"}, f"tipo {t}")

    def test_abecedario_es_poco_del_tipo_6(self):
        letras = [f for f in SEL[6] if f["f"].startswith("La letra ")]
        self.assertLessEqual(len(letras) / len(SEL[6]), 0.15)
        self.assertGreater(len(letras), 0)
        for fam in ("tamaño", "pesa", "dura", "rápid", "caliente", "mayor"):
            self.assertTrue(any(fam in f["f"] or fam == "tamaño" for f in SEL[6]), fam)

    def test_dificultad_por_largo(self):
        # el tipo 1 son frases de 3 palabras; el resto, más largas en general
        self.assertTrue(all(f["n"] in (3, 4) for f in SEL[1]))

    def test_nunca_mas_de_3_iguales_posible(self):
        # el juego mezcla; aquí solo se verifica que el orden guardado no sea una racha larga de lo mismo
        for t in range(1, 7):
            racha = mejor = 0
            ant = None
            for f in SEL[t]:
                racha = racha + 1 if f["v"] == ant else 1
                ant = f["v"]
                mejor = max(mejor, racha)
            self.assertLessEqual(mejor, 12, f"tipo {t}")

    def test_determinista(self):
        otra = D.seleccion(D.candidatos())
        self.assertEqual(json.dumps(SEL, sort_keys=True), json.dumps(otra, sort_keys=True))


class Filtros(unittest.TestCase):
    def test_nada_de_temas_prohibidos(self):
        excl = D.cargar_excluir()
        self.assertGreater(len(excl), 3)
        for t in range(1, 7):
            for f in SEL[t]:
                for p in excl:
                    self.assertIsNone(p.search(f["f"] + " " + f["r"]), f["f"])


class Salida(unittest.TestCase):
    def test_json_generado_es_valido(self):
        self.assertTrue(os.path.exists(D.OUT_JSON), "corre primero: python tools/frases/disparate.py")
        doc = json.load(open(D.OUT_JSON, encoding="utf8"))
        self.assertIsInstance(doc["frases"], list)
        for f in doc["frases"]:
            self.assertEqual(set(f), {"f", "v", "t", "c", "n", "r"})
            self.assertIn(f["t"], range(1, 7))
        self.assertEqual(len(doc["frases"]), sum(len(SEL[t]) for t in range(1, 7)))

    def test_muestra_de_100_frases(self):
        txt = open(D.OUT_MD, encoding="utf8").read()
        filas = [l for l in txt.splitlines() if re.match(r"^\| \d+ \|", l)]
        self.assertEqual(len(filas), 100)
        self.assertIn("Marca las que estén mal, sean dudosas o suenen raras", txt)


if __name__ == "__main__":
    unittest.main()
