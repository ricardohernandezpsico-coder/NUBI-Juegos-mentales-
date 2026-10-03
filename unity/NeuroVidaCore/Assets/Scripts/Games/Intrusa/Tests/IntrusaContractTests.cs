using System;
using System.Collections.Generic;
using NUnit.Framework;
using Random = System.Random;

namespace NeuroVida.Games.Intrusa.Tests
{
    /// <summary>Reglas puras de La estrella intrusa: niveles, puntaje, medidas, repaso espaciado, geometría de la ronda, recorrido de la chispa y director.</summary>
    public class IntrusaContractTests
    {
        // ------------------------------------------------------------------ ayudas

        private static FigureShape Shape(string key = "fruta")
        {
            // una figura chica y simple: 8 puntos; aristas en orden de trazado con un salto (la última no toca a la anterior)
            return new FigureShape
            {
                regla = key, nombre = "La Figura",
                puntos = new[] { .10f, .50f, .30f, .20f, .55f, .15f, .80f, .30f, .90f, .60f, .65f, .85f, .35f, .80f, .50f, .50f },
                aristas = new[] { 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 0, 2, 7 },
                anclas = new[] { 1, 3, 5, 6 },
                huecos = new[] { .95f, .08f, .06f, .95f },
                contorno = new FigureOutline[0], detalles = new FigureDetail[0]
            };
        }

        private static IntrusaGroup Group(string id, int t, string k, params string[] p)
        {
            return new IntrusaGroup
            {
                i = id, t = t, k = k, p = p.Length == 4 ? p : new[] { "uno", "dos", "tres", "cuatro" }, x = "intrusa", n = "Regla",
                o = new[] { "A", "B", "C" }, c = 0, a = "", e = "Todos lo son; la intrusa no.", r = ""
            };
        }

        private sealed class StubSource : IGroupSource
        {
            public readonly List<IntrusaGroup> All = new List<IntrusaGroup>();
            public IReadOnlyList<IntrusaGroup> OfType(int type) => All.FindAll(g => g.t == type);
            public IReadOnlyList<IntrusaGroup> OfRule(string key) => All.FindAll(g => g.k == key);
            public FigureShape FigureOf(string key) => Shape(key);
        }

        private static StubSource Source(int perType = 12, int rulesPerType = 4)
        {
            var s = new StubSource();
            for (int t = 1; t <= 6; t++)
                for (int n = 0; n < perType; n++)
                    s.All.Add(Group($"g{t}_{n}", t, $"r{t}_{n % rulesPerType}"));
            return s;
        }

        // ------------------------------------------------------------------ niveles y puntaje

        [Test]
        public void TypeForLevel_SubeCadaDosNiveles()
        {
            int[] expected = { 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6 };
            for (int l = 1; l <= 12; l++) Assert.AreEqual(expected[l - 1], IntrusaContract.TypeForLevel(l), "nivel " + l);
            Assert.AreEqual(1, IntrusaContract.TypeForLevel(-3));
            Assert.AreEqual(6, IntrusaContract.TypeForLevel(99));
        }

        [Test]
        public void Bonus_DesdeElNivel3_YMayoresTienenMasTiempo()
        {
            Assert.IsFalse(IntrusaContract.HasBonus(2));
            Assert.IsTrue(IntrusaContract.HasBonus(3));
            Assert.AreEqual(4f, IntrusaContract.BonusWindow(false));
            Assert.AreEqual(6f, IntrusaContract.BonusWindow(true));
        }

        [Test]
        public void Points_SumanPorNivel_ElBonusDuplica_ElCieloDespejadoMultiplica()
        {
            Assert.AreEqual(100, IntrusaContract.Points(1, 0, false));
            Assert.AreEqual(210, IntrusaContract.Points(12, 0, false));
            Assert.AreEqual(200, IntrusaContract.Points(1, 0, true));
            Assert.AreEqual(150, IntrusaContract.Points(1, IntrusaContract.ClearStreak, false));
            Assert.AreEqual(300, IntrusaContract.Points(1, IntrusaContract.ClearStreak, true));
            Assert.AreEqual(100, IntrusaContract.Points(1, IntrusaContract.ClearStreak - 1, false));
        }

        [Test]
        public void Score_PesaAciertosYNivel()
        {
            Assert.AreEqual(0, IntrusaContract.Score(0f, 1));
            Assert.AreEqual(100, IntrusaContract.Score(1f, 12));
            Assert.AreEqual(70, IntrusaContract.Score(1f, 1));
            Assert.AreEqual(30, IntrusaContract.Score(0f, 12));
        }

        // ------------------------------------------------------------------ medidas

        [Test]
        public void CategoryOfType_AgrupaLosSeisTipos()
        {
            Assert.AreEqual(0, IntrusaContract.CategoryOfType(1));
            Assert.AreEqual(0, IntrusaContract.CategoryOfType(2));
            Assert.AreEqual(1, IntrusaContract.CategoryOfType(3));
            Assert.AreEqual(2, IntrusaContract.CategoryOfType(4));
            Assert.AreEqual(3, IntrusaContract.CategoryOfType(5));
            Assert.AreEqual(3, IntrusaContract.CategoryOfType(6));
        }

        [Test]
        public void LowestCategory_SoloConAlMenos3Rondas_YSiHayDiferencia()
        {
            // la de uso (1) es la más baja; la de trampas (3) tiene solo 2 rondas y no cuenta
            Assert.AreEqual(1, IntrusaContract.LowestCategory(new[] { 6, 4, 5, 2 }, new[] { 6, 2, 5, 0 }));
            // una sola categoría válida: nada que comparar
            Assert.AreEqual(-1, IntrusaContract.LowestCategory(new[] { 6, 2, 1, 0 }, new[] { 3, 1, 0, 0 }));
            // todas iguales: no se marca ninguna
            Assert.AreEqual(-1, IntrusaContract.LowestCategory(new[] { 4, 4, 4, 4 }, new[] { 3, 3, 3, 3 }));
        }

        [Test]
        public void TrapReadable_ConAlMenos4Trampas()
        {
            Assert.IsFalse(IntrusaContract.TrapReadable(3));
            Assert.IsTrue(IntrusaContract.TrapReadable(4));
        }

        [Test]
        public void MedianMs_ConPocasMuestrasNoHay()
        {
            Assert.AreEqual(-1, IntrusaContract.MedianMs(new List<int> { 1000, 2000, 3000 }));
            Assert.AreEqual(2500, IntrusaContract.MedianMs(new List<int> { 4000, 1000, 2000, 3000 }));
            Assert.AreEqual(3000, IntrusaContract.MedianMs(new List<int> { 5000, 1000, 3000, 2000, 4000 }));
        }

        [Test]
        public void Tally_CuentaPorTipo_Categorias_Rachas()
        {
            var tally = new IntrusaTally();
            tally.Add(Group("a", 1, "r1"), true, 1200);
            tally.Add(Group("b", 2, "r2"), true, 1500);
            tally.Add(Group("c", 5, "r5"), false, -1);
            tally.Add(Group("d", 6, "r6"), true, 2000);
            tally.Add(Group("e", 3, "r3"), true, 1000);
            Assert.AreEqual(5, tally.Total);
            Assert.AreEqual(4, tally.Correct);
            Assert.AreEqual(2, tally.BestStreak);            // la trampa fallada cortó la racha; luego 2 seguidas
            Assert.AreEqual(2, tally.TrapSeen);
            Assert.AreEqual(1, tally.TrapHits);
            tally.Categories(out var seen, out var hits);
            CollectionAssert.AreEqual(new[] { 2, 1, 0, 2 }, seen);
            CollectionAssert.AreEqual(new[] { 2, 1, 0, 1 }, hits);
            tally.AddBonus(true, "r1"); tally.AddBonus(false, "r2");
            Assert.AreEqual(2, tally.BonusSeen);
            Assert.AreEqual(1, tally.BonusHits);
            CollectionAssert.AreEqual(new[] { "r1" }, tally.Named);
        }

        // ------------------------------------------------------------------ repaso espaciado y atlas

        [Test]
        public void Review_SeGuardaYSoloVuelveEnOtroDia()
        {
            var review = new Dictionary<string, int> { ["fruta"] = 100, ["pez"] = 101, ["vuela"] = 100 };
            string stored = IntrusaContract.FormatReview(review);
            Assert.AreEqual("fruta:100;pez:101;vuela:100", stored);
            var back = IntrusaContract.ParseReview(stored);
            Assert.AreEqual(3, back.Count);
            Assert.AreEqual(101, back["pez"]);
            // el mismo día no vuelve; al día siguiente, sí
            CollectionAssert.AreEqual(new string[0], IntrusaContract.ReviewDue(back, 100));
            CollectionAssert.AreEqual(new[] { "fruta", "vuela" }, IntrusaContract.ReviewDue(back, 101));
            CollectionAssert.AreEqual(new[] { "fruta", "pez", "vuela" }, IntrusaContract.ReviewDue(back, 102));
        }

        [Test]
        public void Review_ConClavesConEspacios_Y_TextoRoto()
        {
            var review = new Dictionary<string, int> { ["sirve para cortar"] = 7 };
            var back = IntrusaContract.ParseReview(IntrusaContract.FormatReview(review));
            Assert.AreEqual(7, back["sirve para cortar"]);
            Assert.AreEqual(0, IntrusaContract.ParseReview("sin dos puntos;:3;x:no").Count);
            Assert.AreEqual(0, IntrusaContract.ParseReview(null).Count);
        }

        [Test]
        public void Plates_IdaYVuelta()
        {
            var set = IntrusaContract.ParsePlates("pez;fruta; ;pez");
            Assert.AreEqual(2, set.Count);
            Assert.AreEqual("fruta;pez", IntrusaContract.FormatPlates(set));
        }

        [Test]
        public void DayNumber_CuentaDiasDesde1970()
        {
            Assert.AreEqual(0, IntrusaContract.DayNumber(new DateTime(1970, 1, 1, 23, 59, 0)));
            Assert.AreEqual(1, IntrusaContract.DayNumber(new DateTime(1970, 1, 2, 0, 1, 0)));
        }

        [Test]
        public void PushRecent_ConservaLasUltimas3Partidas()
        {
            string s = "";
            for (int g = 1; g <= 5; g++) s = IntrusaContract.PushRecent(s, new[] { "a" + g, "b" + g });
            Assert.AreEqual(3, IntrusaContract.ParseRecent(s).Count);
            var ids = IntrusaContract.RecentIds(s);
            Assert.IsFalse(ids.Contains("a1"));
            Assert.IsTrue(ids.Contains("b5"));
        }

        // ------------------------------------------------------------------ geometría

        private static float Width(string w) => w.Length * 9.5f;

        [Test]
        public void ToScreen_ElCentroDeLaCajaEsElCentroDeLaCaja_ConReflejoYGiro()
        {
            var c = IntrusaLayout.ToScreen(new Vec2(0.5f, 0.5f), false, 0f);
            Assert.AreEqual(180f, c.X, 1e-3f);
            Assert.AreEqual(300f, c.Y, 1e-3f);
            var c2 = IntrusaLayout.ToScreen(new Vec2(0.5f, 0.5f), true, 12f);
            Assert.AreEqual(180f, c2.X, 1e-3f);
            // reflejo: el borde izquierdo pasa al derecho
            var l = IntrusaLayout.ToScreen(new Vec2(0f, 0.5f), false, 0f);
            var r = IntrusaLayout.ToScreen(new Vec2(0f, 0.5f), true, 0f);
            Assert.AreEqual(40f, l.X, 1e-3f);
            Assert.AreEqual(320f, r.X, 1e-3f);
            // giro de 12°: la distancia al centro se conserva
            var p = IntrusaLayout.ToScreen(new Vec2(1f, 0.5f), false, 12f);
            Assert.AreEqual(140f, Vec2.Distance(p, new Vec2(180f, 300f)), 1e-2f);
        }

        [Test]
        public void Plaque_VaDebajoSalvoArribaEnElTercioSuperior_YDentroDeLaPantalla()
        {
            var low = IntrusaLayout.Plaque(new Vec2(180f, 330f), 80f);
            Assert.AreEqual(344f, low.Y, 1e-3f);
            Assert.AreEqual(102f, low.W, 1e-3f);
            var up = IntrusaLayout.Plaque(new Vec2(180f, 200f), 80f);
            Assert.AreEqual(200f - 14f - 30f, up.Y, 1e-3f);
            // pegada al borde: se corre hacia adentro
            var edge = IntrusaLayout.Plaque(new Vec2(10f, 330f), 80f);
            Assert.AreEqual(8f, edge.X, 1e-3f);
            var edgeR = IntrusaLayout.Plaque(new Vec2(355f, 330f), 80f);
            Assert.AreEqual(360f - 8f, edgeR.X + edgeR.W, 1e-3f);
        }

        [Test]
        public void Violation_DetectaEncimadas_FueraDeZona_Y_DaCeroSiTodoCabe()
        {
            var ok = new List<LRect> { new LRect(10, 200, 100, 30), new LRect(150, 200, 100, 30), new LRect(10, 300, 100, 30) };
            Assert.AreEqual(0f, IntrusaLayout.Violation(ok), 1e-4f);
            var overlap = new List<LRect> { new LRect(10, 200, 100, 30), new LRect(90, 205, 100, 30) };
            Assert.Greater(IntrusaLayout.Violation(overlap), 0f);
            var underHud = new List<LRect> { new LRect(10, 60, 100, 30) };
            Assert.Greater(IntrusaLayout.Violation(underHud), 0f);
            var offRight = new List<LRect> { new LRect(300, 200, 100, 30) };
            Assert.Greater(IntrusaLayout.Violation(offRight), 0f);
            var touching = new List<LRect> { new LRect(10, 200, 100, 30), new LRect(111, 200, 100, 30) };   // 1 dp de aire: menos que el margen de 3
            Assert.Greater(IntrusaLayout.Violation(touching), 0f);
        }

        [Test]
        public void Arrange_EncuentraUnaCombinacionSinChoques_YReflejaYGiraAlAzar()
        {
            var shape = Shape();
            var g = Group("g", 1, "fruta", "manzana", "pera", "uva", "melón");
            var mirrors = new HashSet<bool>();
            var rots = new HashSet<int>();
            var holes = new HashSet<int>();
            var firstWord = new HashSet<string>();
            for (int seed = 0; seed < 60; seed++)
            {
                var spec = IntrusaLayout.Arrange(g, shape, new Random(seed), Width);
                Assert.IsNotNull(spec.AnchorWords);
                CollectionAssert.AreEquivalent(g.p, spec.AnchorWords);
                Assert.LessOrEqual(Math.Abs(spec.RotationDeg), IntrusaLayout.MaxRotationDeg + 1e-3f);
                var plaques = IntrusaLayout.Plaques(shape, spec.AnchorWords, g.x, spec.Mirror, spec.RotationDeg, spec.Hole, Width);
                Assert.AreEqual(0f, IntrusaLayout.Violation(plaques), 1e-4f, "semilla " + seed);
                mirrors.Add(spec.Mirror);
                rots.Add((int)Math.Round(spec.RotationDeg / 4f));
                holes.Add(spec.Hole);
                firstWord.Add(spec.AnchorWords[0]);
            }
            Assert.AreEqual(2, mirrors.Count, "alguna vez refleja y alguna no");
            Assert.GreaterOrEqual(rots.Count, 4, "el giro varía");
            Assert.AreEqual(2, holes.Count, "la intrusa ocupa los dos huecos");
            Assert.GreaterOrEqual(firstWord.Count, 3, "el reparto de palabras varía");
        }

        [Test]
        public void Arrange_ConUnaPalabraEnormeBuscaOtroRepartoOElMenorChoque()
        {
            var shape = Shape();
            var g = Group("g", 1, "fruta", "rompecabezas", "destornillador", "sacacorchos", "cortacésped");
            var spec = IntrusaLayout.Arrange(g, shape, new Random(5), w => w.Length * 9.9f);
            Assert.IsNotNull(spec);
            Assert.AreEqual(4, spec.AnchorWords.Length);
        }

        [Test]
        public void TouchArea_NuncaBajaDe64Dp_YPickEligeLaMasCercana()
        {
            var star = new Vec2(100f, 200f);
            var plaque = IntrusaLayout.Plaque(star, 60f);
            var area = IntrusaLayout.TouchArea(star, plaque);
            Assert.GreaterOrEqual(area.W, 64f - 1e-3f);
            Assert.GreaterOrEqual(area.H, 64f - 1e-3f);
            Assert.IsTrue(area.Contains(star.X, star.Y));
            Assert.IsTrue(area.Contains(plaque.CenterX, plaque.CenterY));
            var areas = new List<LRect> { new LRect(0, 0, 100, 100), new LRect(60, 0, 100, 100) };
            Assert.AreEqual(0, IntrusaLayout.Pick(40f, 50f, areas));
            Assert.AreEqual(1, IntrusaLayout.Pick(120f, 50f, areas));
            Assert.AreEqual(-1, IntrusaLayout.Pick(300f, 300f, areas));
        }

        // ------------------------------------------------------------------ chispa

        [Test]
        public void Spark_RecorreLasAristasEnOrden_ConSaltosYTiempoEntre1_2Y2_2s()
        {
            var shape = Shape();
            var segs = IntrusaSpark.Plan(shape, false, 0f, 1f, out float total);
            Assert.AreEqual(shape.EdgeCount, segs.Count);
            Assert.GreaterOrEqual(total, IntrusaSpark.MinTotalSeconds - 0.05f);
            Assert.LessOrEqual(total, IntrusaSpark.MaxTotalSeconds + 0.05f);
            // las aristas 0-1, 1-2, 2-3, 3-4, 4-5, 5-6, 6-0 se encadenan: sin saltos; la última (2-7) no toca el 0 donde quedó: salta
            for (int i = 0; i < 7; i++) Assert.IsFalse(segs[i].Jump, "tramo " + i);
            Assert.IsTrue(segs[7].Jump);
            for (int i = 1; i < segs.Count; i++)
            {
                Assert.GreaterOrEqual(segs[i].Start, segs[i - 1].End - 1e-4f);
                if (!segs[i].Jump) Assert.AreEqual(segs[i - 1].To, segs[i].From);
            }
        }

        [Test]
        public void Spark_ADobleVelocidadDuraLaMitad()
        {
            var shape = Shape();
            IntrusaSpark.Plan(shape, false, 0f, 1f, out float normal);
            IntrusaSpark.Plan(shape, false, 0f, 2f, out float fast);
            Assert.AreEqual(normal / 2f, fast, 0.06f);
        }

        // ------------------------------------------------------------------ director

        [Test]
        public void Director_ElTipoSigueAlNivel_SinRepetirGrupoNiReglaSeguida()
        {
            var src = Source();
            var dir = new IntrusaDirector(src, new Random(1));
            string lastRule = "";
            var seen = new HashSet<string>();
            for (int level = 1; level <= 12; level++)
            {
                var next = dir.Next(level);
                Assert.IsTrue(next.HasValue, "nivel " + level);
                var g = next.Value.group;
                Assert.AreEqual(IntrusaContract.TypeForLevel(level), g.t);
                Assert.IsTrue(seen.Add(g.i), "grupo repetido");
                Assert.AreNotEqual(lastRule, g.k);
                lastRule = g.k;
            }
            Assert.AreEqual(12, dir.UsedInOrder.Count);
        }

        [Test]
        public void Director_EvitaLosGruposDeLasUltimasPartidas()
        {
            var src = Source();
            var avoid = new List<string>();
            for (int n = 0; n < 9; n++) avoid.Add($"g1_{n}");          // quedan g1_9, g1_10, g1_11
            var dir = new IntrusaDirector(src, new Random(2), avoid);
            var picked = new List<string>();
            for (int k = 0; k < 3; k++) { var nx = dir.Next(1); Assert.IsTrue(nx.HasValue); picked.Add(nx.Value.group.i); }
            foreach (var id in picked) CollectionAssert.DoesNotContain(avoid, id);
        }

        [Test]
        public void Director_CadaCuartaRondaVuelveUnaReglaPorRepasar_ConOtroGrupo()
        {
            var src = Source();
            // la regla por repasar existe en el tipo 3; su grupo "viejo" ya salió en una partida anterior (se evita)
            var dir = new IntrusaDirector(src, new Random(3), new[] { "g3_0" }, new[] { "r3_0" });
            var flags = new List<bool>();
            for (int k = 0; k < 6; k++)
            {
                var nx = dir.Next(1);
                Assert.IsTrue(nx.HasValue);
                flags.Add(nx.Value.review);
                if (nx.Value.review)
                {
                    Assert.AreEqual("r3_0", nx.Value.group.k);
                    Assert.AreNotEqual("g3_0", nx.Value.group.i, "con palabras distintas");
                }
            }
            Assert.AreEqual(1, flags.FindAll(f => f).Count, "una sola vez: ya se repasó");
            Assert.IsFalse(flags[0]);
            Assert.IsTrue(flags[3], "en la cuarta ronda");
        }

        [Test]
        public void Director_ConBancoChicoRelajaYNoSeQuedaSinRonda()
        {
            var src = Source(perType: 2, rulesPerType: 1);
            var dir = new IntrusaDirector(src, new Random(4), new[] { "g1_0", "g1_1" });
            for (int k = 0; k < 6; k++) Assert.IsTrue(dir.Next(1).HasValue, "ronda " + k);
        }
    }
}
