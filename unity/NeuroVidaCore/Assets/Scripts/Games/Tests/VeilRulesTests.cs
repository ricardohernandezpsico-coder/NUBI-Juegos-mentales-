using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace NeuroVida.Games.Tests
{
    /// <summary>
    /// El velo detrás de una tarjeta («Nuevo», pedido de la hoja, resumen, final) es un rectángulo liso que cubre TODA la pantalla (Tarea 197). Ricardo vio en Bodega («Ahora el robot cambia una caja de lugar») un óvalo oscuro enorme detrás de la tarjeta, con el tablero y el cielo a la vista en las esquinas:
    /// el velo era un DISCO (<c>DiscSprite</c>) estirado a toda la capa, y un disco estirado a una pantalla alta es una elipse. Correo ya lo hacía bien (<c>MakeImage(_introLayer, "Dim", null)</c>, una imagen sin sprite: un color liso). Estas pruebas miran el CÓDIGO (como <c>HudRulesTests</c>): el smoke solo ve lo que sale en
    /// pantalla durante su corrida y una elipse oscura con la tarjeta encima pasa por «todo en su lugar».
    /// </summary>
    public class VeilRulesTests
    {
        /// <summary>Un sprite que no llena su rectángulo: un disco, un rectángulo con esquinas redondas, un brillo radial o un aro. Estirado a una capa entera deja las esquinas de la pantalla sin cubrir.</summary>
        private static readonly Regex RoundedSprite = new Regex(@"DiscSprite|RoundedRectSprite|RadialGlowSprite|RingSprite|\bRr\(");

        /// <summary><c>Stretch(x.rectTransform)</c>: la imagen <c>x</c> se estira a todo su padre (la capa).</summary>
        private static readonly Regex StretchCall = new Regex(@"\bStretch\(\s*(\w+)\.rectTransform");

        private static IEnumerable<(string rel, string[] lines)> GameSources()
        {
            var root = Path.Combine(Application.dataPath, "Scripts", "Games");
            foreach (var file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                string rel = file.Substring(root.Length + 1).Replace('\\', '/');
                if (rel.StartsWith("Tests/") || rel.Contains("/Tests/") || rel.Contains("/Editor/") || rel.StartsWith("Bitacora/") || rel.StartsWith("Rumbo/") || rel.StartsWith("Cosecha/")) continue;     // los retirados conservan su código pero no se juegan
                yield return (rel, File.ReadAllLines(file));
            }
        }

        /// <summary>Cada imagen que se estira a su capa (<c>Stretch(x.rectTransform)</c>) y se creó con un sprite que no llena su rectángulo: archivo, renglón de su creación y el renglón. Debe quedar vacío.</summary>
        private static List<string> StretchedImagesWithARoundedSprite()
        {
            var found = new List<string>();
            foreach (var (rel, lines) in GameSources())
            {
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].TrimStart().StartsWith("//")) continue;
                    var m = StretchCall.Match(lines[i]);
                    if (!m.Success) continue;
                    var assignment = new Regex(@"\b" + Regex.Escape(m.Groups[1].Value) + @"\s*=\s*");
                    for (int j = i - 1; j >= System.Math.Max(0, i - 40); j--)                    // la última asignación a esa variable antes de estirarla
                    {
                        if (lines[j].TrimStart().StartsWith("//") || !assignment.IsMatch(lines[j])) continue;
                        if (RoundedSprite.IsMatch(lines[j])) found.Add(rel + ":" + (j + 1) + " " + lines[j].Trim());
                        break;
                    }
                }
            }
            return found;
        }

        [Test]
        public void NoImageStretchedToItsLayer_UsesADiscOrARoundedSprite_InTheGames()
        {
            var found = StretchedImagesWithARoundedSprite();
            Assert.IsEmpty(found, "una imagen estirada a toda su capa con un sprite redondo se ve como una elipse y deja las esquinas sin cubrir: el velo va SIN sprite (null), un color liso, como el de Correo:\n" + string.Join("\n", found));
        }

        [Test]
        public void TheGuard_ReadsTheSourcesOfTheGames_SoItIsNotEmptyHanded()
        {
            // (control) si la ruta de las fuentes cambiara y el análisis no leyera ningún archivo, la prueba de arriba pasaría sin mirar nada
            int files = 0, stretches = 0;
            foreach (var (rel, lines) in GameSources()) { files++; foreach (var line in lines) if (StretchCall.IsMatch(line)) stretches++; }
            Assert.Greater(files, 100, "el análisis lee los archivos de los juegos");
            Assert.Greater(stretches, 5, "y encuentra las imágenes que se estiran a su capa (hoy son 12: los velos de las tarjetas y los fondos de las capas)");
        }

        [Test]
        public void TheGuard_FlagsTheOldDisc_SoItMeasuresSomething()
        {
            // (control) el mismo análisis, sobre el renglón que tenían Bodega, Engranajes y Constelaciones, SÍ lo marca
            Assert.IsTrue(RoundedSprite.IsMatch("_dim = MakeImage(_introLayer, \"Dim\", DiscSprite.Get());"));
            Assert.IsFalse(RoundedSprite.IsMatch("_dim = MakeImage(_introLayer, \"Dim\", null);"));
            Assert.IsTrue(StretchCall.IsMatch("Stretch(_dim.rectTransform);"));
        }

        [TestCase("Bodega/BodegaGameController.Build.cs")]
        [TestCase("Engranajes/EngranajesGameController.Build.cs")]
        [TestCase("Parejas/ConstelacionGameController.Build.cs")]
        [TestCase("Correo/MailGameController.Build.cs")]
        public void TheIntroVeilOfTheNuevoCard_IsASolidRectangle_WithoutASprite(string file)
        {
            string text = File.ReadAllText(Path.Combine(Application.dataPath, "Scripts", "Games", file));
            StringAssert.Contains("MakeImage(_introLayer, \"Dim\", null)", text, file + ": el velo de la tarjeta «Nuevo» es un rectángulo liso sobre toda la pantalla");
            StringAssert.DoesNotContain("\"Dim\", DiscSprite", text);
        }
    }
}
