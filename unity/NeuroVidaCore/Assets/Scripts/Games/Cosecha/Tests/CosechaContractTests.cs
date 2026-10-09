using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace NeuroVida.Games.Cosecha.Tests
{
    public class CosechaContractTests
    {
        private static HarvestWord W(string p, int b = 1, bool star = false, bool hidden = false) =>
            new HarvestWord { p = p, b = b, comun = b <= 3 && !hidden, estrella = star, oculta = hidden };

        /// <summary>Ronda de prueba con las letras I R A O A S C (estrella ASOCIAR) y unas cuantas palabras, una con tilde y una oculta.</summary>
        private static HarvestRound Round(string id = "r1", int level = 3)
        {
            var words = new List<HarvestWord>
            {
                W("asociar", 2, star: true), W("casa"), W("caso"), W("cosa"), W("rosa"), W("risa"), W("río"), W("así"), W("sacar"), W("rosca", 3),
                W("ácaros", 4), W("oír"), W("aro"), W("ora"), W("arco"), W("roca"), W("saciar", 2), W("acosar", 3), W("cara"), W("cría"),
                W("aso", 1, hidden: true)
            };
            return new HarvestRound { id = id, nivel = level, letras = "iraoasc", palabras = words.ToArray() };
        }

        // ------------------------------------------------------------------ letras y puntos

        [Test]
        public void Normalize_QuitaTildesPeroNoLaEnie()
        {
            Assert.AreEqual("rio", CosechaContract.Normalize("río"));
            Assert.AreEqual("acaros", CosechaContract.Normalize("ácaros"));
            Assert.AreEqual("pinguino", CosechaContract.Normalize("pingüino"));
            Assert.AreEqual("año", CosechaContract.Normalize("AÑO"));
            Assert.AreEqual("", CosechaContract.Normalize(null));
        }

        [Test]
        public void Points_PorLargo_RaraYEstrella()
        {
            Assert.AreEqual(10, CosechaContract.Points(W("ora")));
            Assert.AreEqual(20, CosechaContract.Points(W("casa")));
            Assert.AreEqual(35, CosechaContract.Points(W("sacar")));
            Assert.AreEqual(55, CosechaContract.Points(W("saciar", 2)));
            Assert.AreEqual(55 * 3 / 2, CosechaContract.Points(W("ácaros", 4)));          // rara: la mitad más
            Assert.AreEqual(300, CosechaContract.Points(W("asociar", 2, star: true)));    // estrella × 3
        }

        [Test]
        public void Success_ExigeAlMenosElTreintaYCincoPorCiento()
        {
            Assert.IsTrue(CosechaContract.Success(7, 20));
            Assert.IsFalse(CosechaContract.Success(6, 20));
            Assert.IsTrue(CosechaContract.Success(5, 13));    // 13 × 0,35 = 4,55 → 5
            Assert.IsFalse(CosechaContract.Success(4, 13));
            Assert.IsFalse(CosechaContract.Success(0, 0));
        }

        [Test]
        public void Score_CreceConLasComunesYElNivel()
        {
            Assert.AreEqual(0, CosechaContract.Score(0f, 1));
            Assert.AreEqual(100, CosechaContract.Score(0.6f, 10));
            Assert.Greater(CosechaContract.Score(0.4f, 5), CosechaContract.Score(0.2f, 5));
            Assert.Greater(CosechaContract.Score(0.4f, 8), CosechaContract.Score(0.4f, 3));
            Assert.LessOrEqual(CosechaContract.Score(1f, 10), 100);
        }

        [Test]
        public void Constantes_TiempoPistaYTocar()
        {
            Assert.AreEqual(60, CosechaContract.CosechaSeconds(false));
            Assert.AreEqual(90, CosechaContract.CosechaSeconds(true));
            Assert.AreEqual(15f, CosechaContract.HintAfter(false));
            Assert.AreEqual(10f, CosechaContract.HintAfter(true));
            Assert.AreEqual(30, CosechaContract.LetterSizeSp(false));
            Assert.AreEqual(34, CosechaContract.LetterSizeSp(true));
            Assert.AreEqual(3, CosechaContract.Cosechas);
        }

        // ------------------------------------------------------------------ la bandeja y sembrar

        [Test]
        public void Bandeja_TocarSumaYTocarLaUltimaDevuelve()
        {
            var s = new CosechaSession(Round());
            Assert.IsTrue(s.TapTile(6));   // c
            Assert.IsTrue(s.TapTile(2));   // a
            Assert.IsTrue(s.TapTile(0));   // i  (letras: i r a o a s c)
            Assert.AreEqual("cai", s.TrayText);
            Assert.IsFalse(s.TapTile(6), "tocar una del medio de la bandeja no la saca");
            Assert.IsTrue(s.TapTile(0), "tocar la última la devuelve");
            Assert.AreEqual("ca", s.TrayText);
            Assert.IsTrue(s.RemoveLast());
            Assert.AreEqual("c", s.TrayText);
            s.Clear();
            Assert.AreEqual("", s.TrayText);
            Assert.IsFalse(s.RemoveLast());
            Assert.IsFalse(s.TapTile(9));
        }

        private static void Tap(CosechaSession s, string word)
        {
            // toca, para cada letra de la palabra, la primera ficha libre con esa letra
            foreach (char c in word)
            {
                for (int i = 0; i < s.Letters.Length; i++)
                    if (s.Letters[i] == c && !s.InTray(i)) { s.TapTile(i); break; }
            }
        }

        [Test]
        public void Sembrar_ValidaSinTildesYMuestraLaPalabraConSuTilde()
        {
            var s = new CosechaSession(Round());
            Tap(s, "rio");
            var r = s.Submit(4f);
            Assert.AreEqual(SubmitKind.Valid, r.Kind);
            Assert.AreEqual("río", r.Word.p);
            Assert.AreEqual(20 - 10, r.Points);                     // 3 letras = 10
            Assert.AreEqual("", s.TrayText, "la bandeja queda vacía");
            Assert.AreEqual(1, s.Found.Count);
            Assert.AreEqual(4f, s.Found[0].Time);
        }

        [Test]
        public void Sembrar_CadaFichaUnaVezPorPalabra()
        {
            var s = new CosechaSession(Round());
            Tap(s, "caa");   // dos aes: hay dos
            Assert.AreEqual("caa", s.TrayText);
            s.Clear();
            Tap(s, "aaa");   // solo hay dos aes: la tercera no entra
            Assert.AreEqual("aa", s.TrayText);
        }

        [Test]
        public void Sembrar_RepetidaInvalidaYMuyCorta()
        {
            var s = new CosechaSession(Round());
            Tap(s, "casa");
            Assert.AreEqual(SubmitKind.Valid, s.Submit(1f).Kind);
            Tap(s, "casa");
            Assert.AreEqual(SubmitKind.Repeat, s.Submit(2f).Kind);
            Assert.AreEqual(1, s.Found.Count);
            Tap(s, "crsa");
            Assert.AreEqual(SubmitKind.Invalid, s.Submit(3f).Kind);
            Tap(s, "ca");
            var tooShort = s.Submit(4f);
            Assert.AreEqual(SubmitKind.TooShort, tooShort.Kind);
            Assert.AreEqual("ca", s.TrayText, "una palabra muy corta no vacía la bandeja");
        }

        [Test]
        public void Sembrar_LaEstrellaVale_Por3_YSeAcumulanLosPuntos()
        {
            var s = new CosechaSession(Round());
            Tap(s, "asociar");
            var r = s.Submit(10f);
            Assert.IsTrue(r.Star);
            Assert.AreEqual(300, r.Points);
            Tap(s, "ora");
            s.Submit(12f);
            Assert.AreEqual(310, s.Points);
            Assert.AreEqual(2, s.CommonFound);
        }

        [Test]
        public void Ocultas_SeAceptanPeroNoCuentan()
        {
            var s = new CosechaSession(Round());
            Tap(s, "aso");
            var r = s.Submit(5f);
            Assert.AreEqual(SubmitKind.Valid, r.Kind, "una palabra oculta se acepta");
            Assert.Greater(r.Points, 0);
            Assert.AreEqual(0, s.CommonFound);
            Assert.AreEqual(0, s.CountedWords);
            var tally = new CosechaTally();
            tally.AddCosecha(s, 60, 3);
            Assert.AreEqual(0, tally.Words, "no entra en las medidas");
            Assert.IsEmpty(tally.Best);
        }

        [Test]
        public void CommonTotal_NoCuentaOcultas()
        {
            Assert.AreEqual(19, CosechaContract.CommonTotal(Round()));
        }

        // ------------------------------------------------------------------ pista

        [Test]
        public void Pista_EsLaPrimeraLetraDeLaPalabraComunMasCortaQueFalta()
        {
            var round = Round();
            var found = new HashSet<string>();
            var hint = CosechaContract.PickHint(round, found);
            Assert.AreEqual(3, CosechaContract.Normalize(hint.p).Length);
            Assert.AreEqual("aro", CosechaContract.Normalize(hint.p), "empata por uso y por orden alfabético: aro, así, oír, ora, río");
            found.Add("aro");
            found.Add("asi");
            Assert.AreEqual("oir", CosechaContract.Normalize(CosechaContract.PickHint(round, found).p));
        }

        [Test]
        public void Pista_NuncaSeñalaUnaOcultaNiUnaYaEncontrada()
        {
            var round = Round();
            var found = new HashSet<string>(new[] { "aro", "asi", "oir", "ora", "rio" });
            var hint = CosechaContract.PickHint(round, found);
            Assert.AreEqual(4, CosechaContract.Normalize(hint.p).Length);
            Assert.AreNotEqual("aso", CosechaContract.Normalize(hint.p));
            foreach (var w in round.palabras) found.Add(CosechaContract.Normalize(w.p));
            Assert.IsNull(CosechaContract.PickHint(round, found));
        }

        // ------------------------------------------------------------------ medidas

        [Test]
        public void Racimo_ComparteLasDosPrimerasLetrasOLaRaiz()
        {
            Assert.IsTrue(CosechaContract.IsCluster("casa", "casas"));
            Assert.IsTrue(CosechaContract.IsCluster("casa", "caso"));
            Assert.IsTrue(CosechaContract.IsCluster("cosa", "cosas"));
            Assert.IsTrue(CosechaContract.IsCluster("cría", "crío"));
            Assert.IsTrue(CosechaContract.IsCluster("sacar", "saca"));
            Assert.IsFalse(CosechaContract.IsCluster("casa", "rosa"));
            Assert.IsFalse(CosechaContract.IsCluster("acero", "arco"));
            Assert.IsFalse(CosechaContract.IsCluster("oír", "río"));
        }

        [Test]
        public void ManeraDeBuscar_SoloConDiezPalabras()
        {
            Assert.AreEqual(-1, CosechaContract.ClusterPercent(5, 3, 9));
            Assert.AreEqual(60, CosechaContract.ClusterPercent(6, 4, 11));
            Assert.AreEqual(-1, CosechaContract.ClusterPercent(0, 0, 12));
        }

        [Test]
        public void Ritmo_PrimerosYUltimosVeinteSegundos()
        {
            CosechaContract.Window(new List<float> { 2f, 9f, 15f, 19.9f, 20f, 35f, 41f, 50f, 59f }, 60, out int first, out int last);
            Assert.AreEqual(4, first);
            Assert.AreEqual(3, last);                    // 40 s o más: 41, 50 y 59
            CosechaContract.Window(new List<float> { 5f, 80f, 75f, 71f }, 90, out first, out last);
            Assert.AreEqual(1, first);
            Assert.AreEqual(3, last);
        }

        [Test]
        public void Tally_PromediaElRitmoYCuentaRacimosYSaltos()
        {
            var tally = new CosechaTally();
            for (int c = 0; c < 3; c++)
            {
                var s = new CosechaSession(Round("r" + c));
                foreach (var w in new[] { "casa", "caso", "rosa", "risa", "sacar", "cosa" })
                {
                    s.Tray.Clear();
                    Tap(s, w);
                    s.Submit(w == "cosa" ? 55f : w == "sacar" ? 30f : 5f + c);
                }
                tally.AddCosecha(s, 60, 3 + c);
            }
            Assert.AreEqual(18, tally.Words);
            Assert.AreEqual(3, tally.Cosechas);
            Assert.AreEqual(5, tally.PeakLevel);
            Assert.AreEqual(4, tally.First20);          // casa, caso, rosa, risa
            Assert.AreEqual(1, tally.Last20);           // cosa a los 55 s
            Assert.Greater(tally.Clusters, 0);
            Assert.Greater(tally.Jumps, 0);
            Assert.AreEqual(tally.Clusters + tally.Jumps, 15, "la primera palabra de cada cosecha no tiene anterior");
            Assert.GreaterOrEqual(tally.ClusterPercent, 0);
            Assert.AreEqual(18, tally.CommonFound);
        }

        [Test]
        public void Tally_PalabraEstrellaYLaMasLarga()
        {
            var tally = new CosechaTally();
            var s = new CosechaSession(Round());
            Tap(s, "casa"); s.Submit(5f);
            Tap(s, "saciar"); s.Submit(9f);
            tally.AddCosecha(s, 60, 2);
            Assert.AreEqual("", tally.Star);
            Assert.AreEqual("saciar", tally.Best);
            Tap(s, "asociar"); s.Submit(20f);
            var t2 = new CosechaTally();
            t2.AddCosecha(s, 60, 2);
            Assert.AreEqual("asociar", t2.Star);
            Assert.AreEqual("asociar", t2.Best);
        }

        [Test]
        public void TambienPodias_CincoComunesQueFaltan_LasMasUsadasYCortas_SinOcultasNiEstrella()
        {
            var round = Round();
            var found = new Dictionary<string, HashSet<string>> { { round.id, new HashSet<string> { "casa", "caso" } } };
            var missed = CosechaContract.Missed(new[] { round }, found, 5);
            Assert.AreEqual(5, missed.Count);
            Assert.IsFalse(missed.Contains("casa"));
            Assert.IsFalse(missed.Contains("asociar"));
            Assert.IsFalse(missed.Contains("aso"));
            // las de banda 1 (las más usadas) primero, y dentro de cada banda las más cortas
            for (int i = 1; i < missed.Count; i++)
                Assert.LessOrEqual(CosechaContract.Normalize(missed[i - 1]).Length, CosechaContract.Normalize(missed[i]).Length + 1);
            Assert.AreEqual(missed.Count, new HashSet<string>(missed).Count);
        }

        // ------------------------------------------------------------------ rondas recientes

        [Test]
        public void Recientes_SeQuedaConLasUltimasCincoPartidas()
        {
            string stored = "";
            for (int g = 0; g < 7; g++) stored = CosechaContract.PushRecent(stored, new[] { "a" + g, "b" + g, "c" + g });
            var games = CosechaContract.ParseRecent(stored);
            Assert.AreEqual(5, games.Count);
            Assert.AreEqual("a2", games[0][0]);
            Assert.AreEqual("c6", games[4][2]);
            var ids = CosechaContract.RecentIds(stored);
            Assert.IsTrue(ids.Contains("b6"));
            Assert.IsFalse(ids.Contains("a1"));
            Assert.AreEqual(0, CosechaContract.ParseRecent("").Count);
        }

        // ------------------------------------------------------------------ el huerto

        [Test]
        public void Huerto_TreintaPlantasDentroDelPlanetaSinEncimarse()
        {
            var g = new Garden(200f, 7);
            var rnd = new Random(3);
            for (int i = 0; i < 30; i++) g.Add(rnd.Next(3, 7), i % 9 == 4);
            Assert.AreEqual(30, g.Count);
            foreach (var s in g.Spots)
            {
                float d = (float)Math.Sqrt(s.X * s.X + s.Y * s.Y);
                Assert.LessOrEqual(d, 0.9f * 200f + 0.01f, "dentro del planeta");
                Assert.GreaterOrEqual(s.Y, 0.12f * 200f - 0.01f, "sobre la cara de arriba");
            }
            Assert.Greater(g.MinDistance(), 0.08f, "ninguna encima de otra");
        }

        [Test]
        public void Huerto_PrimerasPlantasMasSeparadas()
        {
            var g = new Garden(200f, 11);
            for (int i = 0; i < 6; i++) g.Add(4, false);
            Assert.Greater(g.MinDistance(), 0.2f);
        }

        [Test]
        public void Huerto_EsDeterministaConLaMismaSemilla()
        {
            var a = new Garden(200f, 5);
            var b = new Garden(200f, 5);
            for (int i = 0; i < 12; i++) { a.Add(3 + i % 4, i == 7); b.Add(3 + i % 4, i == 7); }
            for (int i = 0; i < 12; i++)
            {
                Assert.AreEqual(a.Spots[i].X, b.Spots[i].X);
                Assert.AreEqual(a.Spots[i].Y, b.Spots[i].Y);
                Assert.AreEqual(a.Spots[i].Kind, b.Spots[i].Kind);
            }
        }

        [Test]
        public void Huerto_TamanoPorLargoYFlorDoradaSiEsRara()
        {
            Assert.Less(Garden.SizeFor(3), Garden.SizeFor(4));
            Assert.Less(Garden.SizeFor(4), Garden.SizeFor(5));
            Assert.Less(Garden.SizeFor(5), Garden.SizeFor(6));
            Assert.AreEqual(Garden.SizeFor(6), Garden.SizeFor(7));
            Assert.AreEqual(PlantKind.GoldenFlower, Garden.KindFor(5, true, 2));
            Assert.AreNotEqual(PlantKind.GoldenFlower, Garden.KindFor(5, false, 2));
            // de 3 letras: brote u hongo; de 6: girasol, arbusto o tulipán
            for (int i = 0; i < 6; i++)
            {
                var k3 = Garden.KindFor(3, false, i);
                Assert.IsTrue(k3 == PlantKind.Sprout || k3 == PlantKind.Mushroom);
                var k6 = Garden.KindFor(6, false, i);
                Assert.IsTrue(k6 == PlantKind.Sunflower || k6 == PlantKind.Bush || k6 == PlantKind.Tulip);
            }
        }

        // ------------------------------------------------------------------ ronda de práctica del tutorial

        [Test]
        public void Practica_LaPalabraPedidaSeSiembraDeVerdad_YLaDelErrorNo()
        {
            var s = new CosechaSession(CosechaContract.PracticeRound());
            foreach (int tile in CosechaContract.TilesFor(CosechaContract.PracticeLetters, CosechaContract.PracticeWord)) s.TapTile(tile);
            Assert.AreEqual(CosechaContract.PracticeWord, s.TrayText);
            Assert.AreEqual(SubmitKind.Valid, s.Submit(1f).Kind);

            // lo que se arma «por error» para mostrar «Borrar» es corto: no se podría sembrar (hace falta MinLetters)
            foreach (int tile in CosechaContract.TilesFor(CosechaContract.PracticeLetters, CosechaContract.PracticeMistake)) s.TapTile(tile);
            Assert.Less(s.TrayText.Length, CosechaContract.MinLetters);
            Assert.AreEqual(SubmitKind.TooShort, s.Submit(2f).Kind);
        }

        [Test]
        public void Practica_SoloTienePalabrasMuyConocidas_YNingunaEstrella()
        {
            var r = CosechaContract.PracticeRound();
            Assert.AreEqual(CosechaContract.LetterCount, r.letras.Length);
            Assert.GreaterOrEqual(r.palabras.Length, 2, "la práctica necesita al menos dos palabras fáciles");
            foreach (var w in r.palabras)
            {
                Assert.IsTrue(w.IsCommon, w.p);
                Assert.IsFalse(w.estrella, w.p);
                Assert.IsFalse(w.IsRare, w.p);
                Assert.IsNotNull(CosechaContract.TilesFor(r.letras, CosechaContract.Normalize(w.p)), "se puede formar con las 7 letras: " + w.p);
            }
        }

        [Test]
        public void TilesFor_UsaCadaFichaUnaVez_YDaNullSiNoSePuede()
        {
            CollectionAssert.AreEqual(new[] { 6, 2, 5, 4 }, CosechaContract.TilesFor("iraoasc", "casa"));
            CollectionAssert.AreEqual(new[] { 1, 3 }, CosechaContract.TilesFor("iraoasc", "ro"));
            Assert.IsNull(CosechaContract.TilesFor("iraoasc", "casaa"), "solo hay dos A");
            Assert.IsNull(CosechaContract.TilesFor("iraoasc", "luna"));
            Assert.IsNull(CosechaContract.TilesFor("", "casa"));
        }

        [Test]
        public void Huerto_ElArbolDoradoVaArribaAlCentro()
        {
            var g = new Garden(200f, 1);
            var t = g.TreeSpot();
            Assert.AreEqual(0f, t.X);
            Assert.Greater(t.Y, 100f);
            Assert.Greater(t.Size, Garden.SizeFor(6));
        }
    }
}
