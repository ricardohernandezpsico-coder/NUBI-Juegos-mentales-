using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NeuroVida.Games.Disparate;
using NeuroVida.Games.Shared;
using NUnit.Framework;
using UnityEngine;

namespace NeuroVida.Games.Tests
{
    /// <summary>
    /// LA PRUEBA OBLIGATORIA del Contrarreloj (Tarea 176; <c>auditoria-fable/contrarreloj-cuarto-juego.md</c> §5): adivinar NO debe ganar. En cada uno de los cuatro juegos se juega la MISMA pista del día 200 veces con un bot de «azar» (responde apenas puede, sin mirar) y 200 con uno de «cuidado» (acierta el 85 % y responde con el
    /// tiempo de una persona atenta; ver <see cref="TimeTrialSimulation"/>). Se exige, en los CUATRO:
    ///   · la media del azar menor que el 70 % de la media del cuidado, y
    ///   · el percentil 90 del azar menor que la media del cuidado (ni el azar más afortunado alcanza a quien juega con cuidado).
    /// Si un juego falla, se sube su pausa del error o su mínimo antes de responder (<c>TimeTrial.ErrorPauseBase</c>, <c>MinThinkSeconds</c>) hasta que pase, y se anota en el contrato del juego.
    /// El informe con los números queda en <c>unity/test-results/contrarreloj-azar.txt</c> y en el registro de la prueba.
    /// </summary>
    public class TimeTrialFairnessTests
    {
        private static SentenceBank _bank;
        private static SentenceBank Bank => _bank ?? (_bank = SentenceBank.Load());

        [Test] public void Stroop_GuessingDoesNotWin() => AssertFair(TimeTrialSimulation.Measure("stroop", Bank));
        [Test] public void Disparate_GuessingDoesNotWin() => AssertFair(TimeTrialSimulation.Measure("disparate", Bank));
        [Test] public void Radar_GuessingDoesNotWin() => AssertFair(TimeTrialSimulation.Measure("radar", Bank));
        [Test] public void Docking_GuessingDoesNotWin() => AssertFair(TimeTrialSimulation.Measure("acoplamiento", Bank));

        [Test]
        public void TheReportIsWrittenWithTheNumbersOfTheFourGames()
        {
            var results = new List<TimeTrialSimulation.Result>();
            foreach (var g in TimeTrial.GameIds) results.Add(TimeTrialSimulation.Measure(g, Bank));
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.AppendLine("Contrarreloj · prueba del azar (Tarea 176): la MISMA pista del día (" + TimeTrialSimulation.Day + ") jugada " + TimeTrialSimulation.Runs + " veces con un bot de azar y " + TimeTrialSimulation.Runs + " con uno de cuidado (acierta el " + (int)(TimeTrialSimulation.Careful * 100) + " %).");
            sb.AppendLine("Pausa del error " + TimeTrial.ErrorPauseBase.ToString("0.0", inv) + " s (×2 al segundo error seguido, ×3 desde el tercero); mínimo antes de responder: Disparate " + TimeTrial.DisparateMinThinkSeconds.ToString("0.0", inv) + " s (o " + TimeTrial.DisparateSecondsPerWord.ToString("0.00", inv) + " s por palabra), Acoplamiento " + TimeTrial.DockingPortOpensSeconds.ToString("0.0", inv) + " s.");
            sb.AppendLine("Se exige: media del azar < 70 % de la media del cuidado Y p90 del azar < media del cuidado.");
            sb.AppendLine();
            sb.AppendLine("juego\tcuidado (media)\tazar (media)\tazar (p90)\tazar (máx.)\tazar/cuidado\tresultado");
            foreach (var r in results)
                sb.AppendLine(string.Format(inv, "{0}\t{1:0.0}\t{2:0.0}\t{3:0.0}\t{4:0.0}\t{5:0}%\t{6}", r.Game, r.CareMean, r.ChanceMean, r.ChanceP90, r.ChanceMax, 100 * r.ChanceMean / r.CareMean, r.Passes ? "pasa" : "FALLA"));
            string text = sb.ToString();
            Debug.Log("[TimeTrialFairness]\n" + text);
            try
            {
                string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "test-results"));
                Directory.CreateDirectory(dir);
                File.WriteAllText(Path.Combine(dir, "contrarreloj-azar.txt"), text, new UTF8Encoding(false));
            }
            catch (Exception e) { Debug.LogWarning("[TimeTrialFairness] no se pudo escribir el informe: " + e.Message); }
            foreach (var r in results) Assert.IsTrue(r.Passes, TimeTrialSimulation.Describe(r));
        }

        private static void AssertFair(TimeTrialSimulation.Result r)
        {
            Assert.Less(r.ChanceMean, 0.7 * r.CareMean, "la media del azar debe ser menor que el 70 % de la del cuidado. " + TimeTrialSimulation.Describe(r));
            Assert.Less(r.ChanceP90, r.CareMean, "el p90 del azar debe ser menor que la media del cuidado. " + TimeTrialSimulation.Describe(r));
        }
    }
}
