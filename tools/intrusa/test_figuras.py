"""Pruebas de las figuras de "La estrella intrusa":  python -m unittest test_figuras   (desde tools/intrusa)."""
import json
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

    def test_solo_las_tijeras_tienen_aspa(self):
        for f in FIGS:
            if F.hay_aspa(f["p"], F.aristas(f["t"])):
                self.assertIn(f["k"], F.CON_ASPA)

    def test_json_escrito_coincide(self):
        datos = {"version": 1, "figuras": [F.construir(f) for f in FIGS]}
        for ruta in (F.OUT_UNITY, F.OUT_APP):
            with open(ruta, encoding="utf8") as fh:
                self.assertEqual(json.load(fh), json.loads(json.dumps(datos, ensure_ascii=False)), ruta)

    def test_formato_json(self):
        with open(F.OUT_UNITY, encoding="utf8") as fh:
            for f in json.load(fh)["figuras"]:
                self.assertEqual(set(f), {"regla", "nombre", "puntos", "aristas", "anclas", "huecos", "contorno", "detalles"})
                self.assertEqual(len(f["anclas"]), 4)
                self.assertIn(len(f["huecos"]), (2, 3))


if __name__ == "__main__":
    unittest.main()
