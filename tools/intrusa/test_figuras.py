"""Pruebas de las figuras de "La estrella intrusa":  python -m unittest test_figuras   (desde tools/intrusa)."""
import json
import math
import os
import sys
import unittest

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import figuras as F  # noqa: E402
import grupos as G  # noqa: E402

with open(F.G_JSON, encoding="utf8") as _f:
    POR_REGLA = {}
    for _g in json.load(_f)["grupos"]:
        POR_REGLA.setdefault(_g["k"], []).append(_g)
FIGS = F.resolver(F.todas(), POR_REGLA)
LARGA = F.palabra_mas_larga()


class Figuras(unittest.TestCase):
    def test_una_figura_por_regla_del_banco(self):
        claves = [f["k"] for f in FIGS]
        self.assertEqual(len(claves), len(set(claves)), "claves repetidas")
        self.assertEqual(set(claves), set(G.REGLAS))
        self.assertEqual(len(FIGS), 77)

    def test_todas_validan(self):
        for f in FIGS:
            self.assertEqual(F.validar(f, LARGA, POR_REGLA.get(f["k"], [])), [], f["n"])

    def test_nombres_unicos(self):
        nombres = [f["n"] for f in FIGS]
        self.assertEqual(len(nombres), len(set(nombres)), "figuras compartidas (hay que listarlas)")

    def test_el_validador_detecta_lo_prohibido(self):
        import math
        # pentágono regular
        pent = dict(k="x", n="P", p=[(.5 + .4 * math.cos(i * 2 * math.pi / 5), .5 + .4 * math.sin(i * 2 * math.pi / 5)) for i in range(5)] + [(.5, .5), (.5, .62)],
                    t=[[0, 1, 2, 3, 4, 0], [5, 6]], a=[0, 1, 2, 3], h=[(.02, .02), (.98, .02)], o=[0, 1, 2, 3, 4])
        self.assertTrue(any("regular" in e for e in F.validar(pent)))
        # estrella de 5 puntas (5 rayos iguales desde el centro)
        est = dict(k="x", n="E", p=[(.5, .5)] + [(.5 + .4 * math.cos(i * 2 * math.pi / 5), .5 + .4 * math.sin(i * 2 * math.pi / 5)) for i in range(5)],
                   t=[[0, i] for i in range(1, 6)], a=[1, 2, 3, 4], h=[(.02, .02), (.98, .02)], o=[1, 2, 3, 4, 5])
        self.assertTrue(any("estrella" in e for e in F.validar(est)))
        # cruz
        cruz = dict(k="x", n="C", p=[(.5, .5), (.5, .1), (.5, .9), (.1, .5), (.9, .5), (.2, .2), (.8, .8)],
                    t=[[0, 1], [0, 2], [0, 3], [0, 4], [3, 5], [4, 6]], a=[1, 2, 3, 4], h=[(.02, .02), (.98, .02)], o=[1, 3, 2, 4])
        self.assertTrue(any("aspa" in e for e in F.validar(cruz)))

    def test_ninguna_figura_tiene_aspa(self):
        self.assertEqual(F.CON_ASPA, set())
        for f in FIGS:
            self.assertFalse(F.hay_aspa(f["p"], F.aristas(f["t"])), f["n"])

    def test_ningun_nodo_con_4_aristas_ni_rayos(self):
        for f in FIGS:
            self.assertLessEqual(max(F.grados(len(f["p"]), F.aristas(f["t"]))), 3, f["n"])

    def test_ninguna_simetria_de_giro_ni_rueda_ni_triangulo_con_linea_ni_estrella_de_4(self):
        for f in FIGS:
            ed = F.aristas(f["t"])
            self.assertEqual(F.simetria_giro(f["p"]), 0, f["n"])
            self.assertFalse(F.rueda(f, ed), f["n"])
            self.assertFalse(F.triangulo_con_linea(f["p"], ed), f["n"])
            self.assertFalse(F.estrella_4(f, f["p"]), f["n"])

    def test_los_rediseños_del_2_oct_estan_y_cambiaron_de_emblema(self):
        nombres = {f["k"]: f["n"] for f in FIGS}
        self.assertEqual(nombres["pétalos"], "La Rosa")
        self.assertEqual(nombres["garras"], "La Pata con Garras")
        self.assertEqual(nombres["sirve para respirar"], "La Nariz")
        self.assertEqual(nombres["sirve para lavar"], "El Balde")
        self.assertNotIn("La Margarita", nombres.values())
        self.assertNotIn("Los Pulmones", nombres.values())
        self.assertNotIn("El Grifo", nombres.values())
        self.assertEqual(F.REDISENADAS, {f["k"] for f in F.FIGURAS_D})

    def test_las_tijeras_se_abren_poco_y_tienen_dos_pivotes(self):
        t = next(f for f in FIGS if f["k"] == "sirve para cortar")
        pts = t["p"]
        # los dos pivotes (nodos 0 y 1) son distintos y cada uno tiene a lo sumo 3 aristas
        self.assertGreater(F.dist(pts[0], pts[1]), 0.02)
        # abertura de las hojas: ángulo entre los dos filos desde el pivote, ~25°
        a, b = pts[2], pts[3]
        ang = abs(math.degrees(math.atan2(a[1] - pts[0][1], a[0] - pts[0][0]) - math.atan2(b[1] - pts[1][1], b[0] - pts[1][0])))
        self.assertLess(ang, 40)
        self.assertGreater(ang, 15)

    def test_el_validador_detecta_los_patrones_de_simbolo(self):
        # rayos: un nodo con 5 aristas
        rayos = dict(k="x", n="R", p=[(.5, .5), (.1, .1), (.9, .1), (.1, .9), (.9, .9), (.5, .05), (.5, .95)],
                     t=[[0, 1], [0, 2], [0, 3], [0, 4], [0, 5], [5, 6]], a=[1, 2, 3, 4], h=[(.02, .5), (.98, .5)], o=[1, 2, 4, 3])
        self.assertTrue(any("aristas" in e for e in F.validar(rayos)))
        # simetría de giro de orden 3
        tri = [(.5 + .4 * math.cos(i * 2 * math.pi / 3), .5 + .4 * math.sin(i * 2 * math.pi / 3)) for i in range(3)]
        sim = dict(k="x", n="S", p=tri + [(.5 + .2 * math.cos(i * 2 * math.pi / 3 + 1), .5 + .2 * math.sin(i * 2 * math.pi / 3 + 1)) for i in range(3)] + [(.5, .5)],
                   t=[[0, 3, 1], [1, 4, 2], [2, 5, 0], [6, 3]], a=[0, 1, 2, 6], h=[(.02, .02), (.98, .02)], o=[0, 1, 2])
        self.assertEqual(F.simetria_giro(sim["p"]), 3)
        self.assertTrue(any("simetría" in e for e in F.validar(sim)))
        # rueda: contorno circular con un diámetro
        pts = [(.5 + .4 * math.cos(i * math.pi / 4), .5 + .4 * math.sin(i * math.pi / 4)) for i in range(8)] + [(.5, .5)]
        rueda = dict(k="x", n="W", p=pts, t=[[0, 1, 2, 3, 4, 5, 6, 7, 0], [2, 6]], a=[0, 2, 4, 6], h=[(.02, .02), (.98, .02)], o=list(range(8)))
        self.assertTrue(F.rueda(rueda, F.aristas(rueda["t"])))
        # triángulo con una línea central
        tr = dict(k="x", n="T", p=[(.5, .04), (.04, .94), (.96, .94), (.5, .94), (.3, .6), (.7, .6), (.5, .5)],
                  t=[[0, 1, 3, 2, 0], [0, 3]], a=[0, 1, 2, 4], h=[(.02, .5), (.98, .5)], o=[0, 1, 2])
        self.assertTrue(F.triangulo_con_linea(tr["p"], F.aristas(tr["t"])))

    def test_json_escrito_coincide(self):
        datos = {"version": 1, "figuras": [F.construir(f) for f in FIGS]}
        unity = {"version": 1, "figuras": [F.plano(d) for d in datos["figuras"]]}
        for ruta, esperado in ((F.OUT_UNITY, unity), (F.OUT_APP, datos)):
            with open(ruta, encoding="utf8") as fh:
                self.assertEqual(json.load(fh), json.loads(json.dumps(esperado, ensure_ascii=False)), ruta)

    def test_formato_json(self):
        with open(F.OUT_UNITY, encoding="utf8") as fh:
            for f in json.load(fh)["figuras"]:
                self.assertEqual(set(f), {"regla", "nombre", "puntos", "aristas", "anclas", "huecos", "contorno", "detalles"})
                self.assertEqual(len(f["anclas"]), 4)
                self.assertIn(len(f["huecos"]), (4, 6))      # planos: x,y por hueco
                self.assertEqual(len(f["puntos"]) % 2, 0)
                self.assertTrue(7 * 2 <= len(f["puntos"]) <= 12 * 2)
                self.assertEqual(len(f["aristas"]) % 2, 0)


if __name__ == "__main__":
    unittest.main()
