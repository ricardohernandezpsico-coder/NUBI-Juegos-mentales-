using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Tests
{
    /// <summary>
    /// UN solo nombre por juego (Tarea 127): el del catálogo de la app (<c>GameRegistry</c>), en el marcador, la cuenta regresiva y el tutorial. Correo decía «Correo Estelar» en la app y «La estación de correo» en Unity.
    /// La lista es <see cref="GameTitles"/>; <c>GameTitlesUnityTest</c> (Kotlin) vigila que sea igual al catálogo de la app, y esta prueba, que los títulos de los contratos y los textos que cada controlador le pasa al marcador, la cuenta regresiva y el tutorial sean esos.
    /// </summary>
    public class GameTitlesTests
    {
        private static readonly string[] AppGames =
        {
            "parejas", "secuencia", "bodega", "correo", "stroop", "piloto", "freno", "satelites", "radar", "calculo", "engranajes", "acoplamiento", "aterrizaje", "anagramas", "meteoros", "disparate", "intrusa"
        };

        [Test]
        public void La_lista_trae_los_17_juegos_de_la_app_con_un_nombre_cada_uno()
        {
            var ids = new List<string>();
            var titles = new HashSet<string>();
            foreach (var entry in GameTitles.All)
            {
                ids.Add(entry.id);
                Assert.IsTrue(titles.Add(entry.title), "dos juegos con el mismo nombre: " + entry.title);
                Assert.IsFalse(string.IsNullOrWhiteSpace(entry.title), entry.id);
            }
            CollectionAssert.AreEquivalent(AppGames, ids);
            Assert.AreEqual("Correo Estelar", GameTitles.Of("correo"), "«La estación de correo» es solo la descripción en los documentos");
            Assert.IsNull(GameTitles.Of("bitacora"), "un juego retirado no tiene nombre en el catálogo");
        }

        [Test]
        public void Los_contratos_dicen_el_nombre_del_catalogo()
        {
            Assert.AreEqual(GameTitles.Of("radar"), Radar.RadarContract.Title);
            Assert.AreEqual(GameTitles.Of("piloto"), Piloto.PilotContract.Title);
            Assert.AreEqual(GameTitles.Of("satelites"), Satelites.SatelliteContract.Title);
            Assert.AreEqual(GameTitles.Of("acoplamiento"), Acoplamiento.DockingContract.Title);
            Assert.AreEqual(GameTitles.Of("aterrizaje"), Aterrizaje.LandingContract.Title);
            Assert.AreEqual(GameTitles.Of("correo"), Correo.MailContract.TitleRun);
        }

        /// <summary>Los textos entre comillas que cada controlador le pasa al marcador (<c>new GameHud(…, "…")</c>), a la cuenta regresiva (<c>_countdown.Play("…")</c>) y al tutorial (<c>BuildTutorial(…, "…")</c>) son nombres del catálogo.</summary>
        [Test]
        public void Los_controladores_usan_solo_nombres_del_catalogo()
        {
            var catalog = new HashSet<string>();
            foreach (var entry in GameTitles.All) catalog.Add(entry.title);
            // los juegos retirados de la app (el código se conserva) conservan sus nombres
            catalog.Add("Bitácora de Misión"); catalog.Add("Rumbo a Casa"); catalog.Add("Cosecha de palabras");
            var patterns = new[]
            {
                new Regex(@"new GameHud\(\s*\w+\s*,\s*""([^""]+)"""),
                new Regex(@"_countdown\.Play\(\s*""([^""]+)"""),
                new Regex(@"BuildTutorial\(\s*\w+\s*,\s*[^,]+,\s*""([^""]+)"""),
            };
            string root = Path.Combine(Application.dataPath, "Scripts", "Games");
            int seen = 0;
            foreach (var file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Replace('\\', '/').Contains("/Tests/")) continue;
                string text = File.ReadAllText(file);
                foreach (var pattern in patterns)
                    foreach (Match m in pattern.Matches(text))
                    {
                        seen++;
                        Assert.IsTrue(catalog.Contains(m.Groups[1].Value), Path.GetFileName(file) + ": «" + m.Groups[1].Value + "» no es un nombre del catálogo (GameTitles)");
                    }
            }
            Assert.Greater(seen, 20, "la prueba no encontró los títulos de los controladores: ¿cambió la forma de llamarlos?");
        }
    }
}
