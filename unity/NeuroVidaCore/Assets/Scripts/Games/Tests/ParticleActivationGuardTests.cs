using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace NeuroVida.Games.Tests
{
    /// <summary>
    /// Ninguna chispa, aro o lucero se ACTIVA antes de fijar su posición y su tamaño (Tarea 170, 25-oct). Un objeto de la interfaz (<c>Image</c>) nace con 100 × 100, blanco y en el centro; las chispas de los juegos se animaban en un <c>AnimateSparks</c> que corre en el <c>Update</c>,
    /// así que una chispa pedida DESPUÉS de ese paso (desde una corrutina, o desde la lectura del toque, que va al final del <c>Update</c>) se veía un cuadro como un disco blanco de ~100 px en el centro de la pantalla: «unas pelotitas blanquitas que aparecen y se apagan» de En la punta de la lengua
    /// (Ricardo, 25-oct). Se vigila leyendo el código: en cada método que lanza efectos de una piscina (<c>Emit…</c>, <c>SpawnParticles</c>, <c>FlashRing</c>, <c>AddBloom</c>, <c>LaunchFlyer</c> y <c>LaunchArc</c>: las chispas, los aros, los luceros y los objetos que vuelan y los arcos), antes de cada <c>SetActive(true)</c> tienen que estar <c>anchoredPosition</c> y <c>sizeDelta</c>.
    /// Y En la punta de la lengua ya no tiene chispas: Ricardo las quitó (menos estímulos).
    /// <para>
    /// POR QUÉ NO SE ENSANCHA A TODOS LOS MÉTODOS (Tarea 178): se probó. Mirando todos los métodos de todos los juegos que crean un objeto y lo activan (<c>new GameObject(</c> + <c>SetActive(true)</c>) salían decenas de «ofensores» que no lo son: los objetos se fabrican con ayudas (<c>MakeImage(parent, pos, size)</c>…)
    /// que fijan posición y tamaño por dentro, y casi todos los aros, destellos y marcas (~48 en los 17 juegos) se construyen al armar la escena y se encienden más tarde desde OTRO método: leyendo el texto no se puede saber si estaban acomodados. Lo que vigila en su lugar es <c>FactoryImageGuard</c> (<c>FactoryImageGuardTests</c> prueba que detecta),
    /// que el smoke corre en CADA cuadro de CADA corrida de los 17 juegos y mira lo que realmente se ve: una <c>Image</c> activa de 100 × 100, blanca y en el centro es un error, sea cual sea el objeto y el método que la encendió.
    /// </para>
    /// </summary>
    public class ParticleActivationGuardTests
    {
        private static readonly Regex Spawner = new Regex(@"^[ \t]*(?:\[[^\]]*\][ \t]*)?(?:(?:private|public|protected|internal|static)[ \t]+)+(?:void|IEnumerator)[ \t]+(Emit\w*|SpawnParticles|FlashRing|AddBloom|LaunchFlyer|LaunchArc)[ \t]*\(", RegexOptions.Multiline);

        private static string GamesDir => Path.Combine(Application.dataPath, "Scripts", "Games");

        private static IEnumerable<string> Sources()
        {
            foreach (var file in Directory.GetFiles(GamesDir, "*.cs", SearchOption.AllDirectories))
            {
                string rel = file.Substring(GamesDir.Length).Replace('\\', '/');
                if (rel.Contains("/Tests/")) continue;
                yield return file;
            }
        }

        /// <summary>El cuerpo de un método (de su primera llave a la que la cierra), o null si es de una sola expresión.</summary>
        private static string BodyAfter(string src, int from)
        {
            int open = src.IndexOf('{', from), semi = src.IndexOf(';', from);
            if (open < 0 || (semi >= 0 && semi < open)) return null;
            int depth = 0;
            for (int i = open; i < src.Length; i++)
            {
                if (src[i] == '{') depth++;
                else if (src[i] == '}' && --depth == 0) return src.Substring(open, i - open + 1);
            }
            return null;
        }

        [Test]
        public void Ningun_efecto_de_una_piscina_se_activa_antes_de_fijar_su_posicion_y_su_tamano()
        {
            int spawners = 0, activating = 0;
            var offenders = new List<string>();
            foreach (var file in Sources())
            {
                string src = File.ReadAllText(file).Replace("\r\n", "\n");
                foreach (Match m in Spawner.Matches(src))
                {
                    string body = BodyAfter(src, m.Index + m.Length);
                    if (body == null) continue;
                    spawners++;
                    int at = 0; bool any = false;
                    while ((at = body.IndexOf("SetActive(true)", at, System.StringComparison.Ordinal)) >= 0)
                    {
                        any = true;
                        string before = body.Substring(0, at);
                        if (!before.Contains("anchoredPosition") || !before.Contains("sizeDelta"))
                            offenders.Add(Path.GetFileName(file) + " · " + m.Groups[1].Value + ": SetActive(true) antes de fijar anchoredPosition y sizeDelta");
                        at += "SetActive(true)".Length;
                    }
                    if (any) activating++;
                }
            }
            Assert.GreaterOrEqual(spawners, 10, "no encontré los métodos que lanzan chispas, aros y luceros (¿cambió el código?): " + spawners);
            Assert.GreaterOrEqual(activating, 8, "los métodos que activan un efecto: " + activating);
            Assert.IsEmpty(offenders, "Un efecto que se activa antes de fijar su posición y su tamaño sale un cuadro con los de fábrica (100 × 100, blanco, en el centro):\n" + string.Join("\n", offenders));
        }

        [Test]
        public void En_la_punta_de_la_lengua_ya_no_hay_chispas()
        {
            string src = File.ReadAllText(Path.Combine(GamesDir, "Anagramas", "PuntaGameController.cs"));
            Assert.IsFalse(Regex.IsMatch(src, @"\bEmit\("), "Ricardo quitó las chispas de la cascada y de la llegada del lucero (25-oct): menos estímulos");
            Assert.IsFalse(src.Contains("_particles") || src.Contains("AnimateParticles"), "el sistema de chispas de Punta se retiró entero");
        }
    }
}
