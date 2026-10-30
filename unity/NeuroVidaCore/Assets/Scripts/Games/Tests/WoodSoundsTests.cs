using System;
using System.Collections.Generic;
using NUnit.Framework;
using NeuroVida.Games.Radar;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Tests
{
    /// <summary>
    /// La paleta «Madera cálida» de toda la app (Games/Shared/WoodSounds.cs, Tarea 119 Fase A) y el medidor de sonidos (Games/Shared/SoundMeter.cs): cada evento suena finito, sin clics ni nada fuerte, con el nivel de su familia (así todo queda parejo), con el inicio inmediato, con las notas en su altura y recortado al final
    /// audible (la memoria). Ningún juego la usa todavía: esto la deja medida y fija para la Fase B.
    /// </summary>
    public class WoodSoundsTests
    {
        private sealed class Rendered
        {
            public WoodSfx Sfx;
            public int Arg;
            public float[] L, R;
            public double Level;
        }

        private static List<Rendered> _all;

        private static List<Rendered> All()
        {
            if (_all != null) return _all;
            _all = new List<Rendered>();
            foreach (WoodSfx sfx in Enum.GetValues(typeof(WoodSfx)))
                foreach (int arg in WoodSounds.Variants(sfx))
                {
                    var recipe = WoodSounds.MakeRecipe(sfx, arg);
                    WoodSounds.Render(recipe, out var l, out var r);
                    _all.Add(new Rendered { Sfx = sfx, Arg = arg, L = l, R = r, Level = recipe.Level });
                }
            return _all;
        }

        private static double Lufs(Rendered x) => SoundMeter.MomentaryLufs(x.L, x.R) + SoundMeter.ToDb(WoodSounds.PlayVolume);

        // ------------------------------------------------------------------ el medidor

        [Test]
        public void TheMeter_MeasuresASineAsTheRecommendationSays()
        {
            var l = SoundKit.Render(1.0, t => 0.1 * Math.Sin(SoundKit.Tau * 1000.0 * t));
            var r = (float[])l.Clone();
            Assert.AreEqual(-20.0, SoundMeter.ToDb(SoundMeter.Peak(l, r)), 0.01, "pico 0,1 = -20 dBFS");
            Assert.AreEqual(-20.0, SoundMeter.MomentaryLufs(l, r), 0.4, "un seno de 1 kHz de 0,1 en los dos canales mide cerca de -20 LUFS");
            Assert.AreEqual(SoundMeter.MomentaryLufs(l, r), SoundMeter.MomentaryLufs(l), 1e-9, "un clip mono se cuenta en los dos canales");
            var loud = SoundKit.Render(1.0, t => 0.2 * Math.Sin(SoundKit.Tau * 1000.0 * t));
            Assert.AreEqual(6.0, SoundMeter.MomentaryLufs(loud) - SoundMeter.MomentaryLufs(l), 0.1, "el doble de amplitud suena 6 dB más fuerte");
            Assert.Less(SoundMeter.MomentaryLufs(new float[SoundKit.Rate]), -100.0, "el silencio no mide nada");
        }

        [Test]
        public void TheMeter_OnsetDurationAndPitchClarity()
        {
            var x = SoundKit.Render(1.0, t => t < 0.1 ? 0.0 : Math.Sin(SoundKit.Tau * 440.0 * t) * Math.Exp(-(t - 0.1) / 0.1));
            Assert.AreEqual(100.0, SoundMeter.OnsetMs(x), 2.0, "empieza a los 100 ms");
            Assert.Greater(SoundMeter.DurationMs(x), 600.0, "y dura hasta que baja 50 dB");
            Assert.Greater(SoundMeter.PitchClarity(x, null, 440.0, 105, 300), 0.8, "un seno limpio tiene casi toda su energía en su frecuencia");
            Assert.Less(SoundMeter.PitchClarity(x, null, 466.16, 105, 300), 0.2, "y casi nada un semitono más arriba");
            Assert.Greater(SoundMeter.DecayMs(x, null, 20.0), 150.0, "tarda un rato en caer 20 dB");
        }

        // ------------------------------------------------------------------ la paleta

        [Test]
        public void EveryEvent_IsListed_WithItsVariants_AndItsTier()
        {
            Assert.AreEqual(7, WoodSounds.Variants(WoodSfx.Correct).Length, "la racha 7 en adelante suena igual que la 7");
            Assert.AreEqual(10, WoodSounds.Variants(WoodSfx.Select).Length);
            Assert.AreEqual(8, WoodSounds.Variants(WoodSfx.Spark).Length);
            var keys = new HashSet<string>();
            foreach (var x in All()) Assert.IsTrue(keys.Add(WoodSounds.KeyOf(x.Sfx, x.Arg)), "las claves de los sonidos son únicas: " + WoodSounds.KeyOf(x.Sfx, x.Arg));
            foreach (WoodSfx sfx in Enum.GetValues(typeof(WoodSfx))) Assert.IsTrue(Enum.IsDefined(typeof(WoodTier), WoodSounds.TierOf(sfx)));
            Assert.AreEqual(WoodTier.Cierre, WoodSounds.TierOf(WoodSfx.Finish));
            Assert.AreEqual(WoodTier.Micro, WoodSounds.TierOf(WoodSfx.Tap));
        }

        [Test]
        public void EveryEvent_IsFinite_WithThePeakAtItsLevel_NothingLoud_AndEndsInSilence()
        {
            foreach (var x in All())
            {
                string name = WoodSounds.KeyOf(x.Sfx, x.Arg);
                Assert.Greater(x.L.Length, 2000, name);
                Assert.AreEqual(x.L.Length, x.R.Length, name);
                for (int i = 0; i < x.L.Length; i++)
                {
                    Assert.IsFalse(float.IsNaN(x.L[i]) || float.IsInfinity(x.L[i]) || float.IsNaN(x.R[i]) || float.IsInfinity(x.R[i]), name + " tiene un valor no finito");
                }
                double peak = SoundMeter.Peak(x.L, x.R);
                Assert.AreEqual(x.Level, peak, 0.005, name + ": el pico es el nivel de la receta");
                Assert.LessOrEqual(peak, 0.41, name + ": nada pasa de 0,40");
                Assert.AreEqual(0f, x.L[x.L.Length - 1], 1e-6f, name + ": termina en 0 (sin clic de corte)");
                Assert.AreEqual(0f, x.R[x.R.Length - 1], 1e-6f, name);
            }
        }

        [Test]
        public void TheTrim_OnlyCutsWhatIsInaudible()
        {
            int fade = (int)Math.Round(0.03 * SoundKit.Rate);
            foreach (var x in All())
            {
                string name = WoodSounds.KeyOf(x.Sfx, x.Arg);
                double peak = SoundMeter.Peak(x.L, x.R), limit = Math.Max(peak * Math.Pow(10.0, -51.0 / 20.0), Math.Pow(10.0, (WoodSounds.TrimFloorDbfs + 1.0) / 20.0));
                for (int i = Math.Max(0, x.L.Length - fade + 2); i < x.L.Length; i++)
                    Assert.LessOrEqual(Math.Max(Math.Abs(x.L[i]), Math.Abs(x.R[i])), limit * 1.5, name + ": en el fundido final ya no se oye (muestra " + i + ")");
            }
        }

        [Test]
        public void TheLevelsFollowTheTiers_SoEverythingSoundsEven()
        {
            var byTier = new Dictionary<WoodTier, List<double>>();
            foreach (var x in All())
            {
                var tier = WoodSounds.TierOf(x.Sfx);
                if (!byTier.ContainsKey(tier)) byTier[tier] = new List<double>();
                double lufs = Lufs(x);
                byTier[tier].Add(lufs);
                string name = WoodSounds.KeyOf(x.Sfx, x.Arg) + " (" + lufs.ToString("0.0") + " LUFS)";
                switch (tier)
                {
                    case WoodTier.Micro: Assert.LessOrEqual(lufs, -27.0, name + ": los micro son los más suaves"); break;
                    case WoodTier.Respuesta: Assert.That(lufs, Is.InRange(-30.0, -19.0), name); break;
                    case WoodTier.Hito: Assert.That(lufs, Is.InRange(-22.0, -17.0), name); break;
                    case WoodTier.Cierre: Assert.That(lufs, Is.InRange(-20.0, -15.0), name); break;
                }
            }
            double microMax = double.MinValue, closeMin = double.MaxValue;
            foreach (var v in byTier[WoodTier.Micro]) microMax = Math.Max(microMax, v);
            foreach (var v in byTier[WoodTier.Cierre]) closeMin = Math.Min(closeMin, v);
            Assert.Less(microMax, closeMin - 6.0, "el cierre suena al menos 6 LU más fuerte que lo más fuerte de los micro");
            Assert.Less(Median(byTier[WoodTier.Micro]), Median(byTier[WoodTier.Respuesta]));
            Assert.Less(Median(byTier[WoodTier.Respuesta]), Median(byTier[WoodTier.Hito]));
            Assert.Less(Median(byTier[WoodTier.Hito]), Median(byTier[WoodTier.Cierre]));
        }

        private static double Median(List<double> v)
        {
            var c = new List<double>(v);
            c.Sort();
            return c[c.Count / 2];
        }

        [Test]
        public void TheStartOfEverySoundIsImmediate_TheLatencyDoesNotGrow()
        {
            foreach (var x in All())
            {
                string name = WoodSounds.KeyOf(x.Sfx, x.Arg);
                double onset = SoundMeter.OnsetMs(x.L, x.R, 0.1);
                // la ayuda empieza con un soplo suave (el que sube) y la nota entra a los 320 ms: su soplo ya se oye a los pocos ms
                Assert.LessOrEqual(onset, x.Sfx == WoodSfx.Hint ? 30.0 : 3.0, name + ": se oye a los " + onset.ToString("0.0") + " ms");
            }
        }

        [Test]
        public void TheMemory_OfThePalette_IsKnown_AndNoClipIsHuge()
        {
            long full = 0, total = 0, biggest = 0;
            foreach (var x in All())
            {
                full += (long)x.L.Length * 2 * 4;                                      // estéreo, 44,1 kHz, 32 bits (como se calcula)
                WoodSounds.RenderForClip(x.Sfx, x.Arg, out var l, out var r);          // y como se guarda: 22,05 kHz y los micro en mono
                long bytes = (long)l.Length * (r == null ? 1 : 2) * 4;
                total += bytes;
                biggest = Math.Max(biggest, bytes);
            }
            Assert.LessOrEqual(full, 28L * 1024 * 1024, "calculada a 44,1 kHz estéreo, la paleta entera (con todas sus variantes) no pasa de 28 MB");
            Assert.LessOrEqual(total, 14L * 1024 * 1024, "guardada a 22,05 kHz (y los micro en mono), la paleta entera no pasa de 14 MB; un juego carga solo lo que usa");
            Assert.LessOrEqual(biggest, 700L * 1024, "ningún clip guardado pasa de 700 KB");
        }

        [Test]
        public void TheVolume_IsThreeDecibelsHigher_InOnePlace_AndNothingSaturates()
        {
            Assert.AreEqual(3.0, WoodKit.MasterGainDb, "+3 dB sobre lo que Ricardo aprobó el 10-oct");
            Assert.AreEqual(System.Math.Pow(10.0, 3.0 / 20.0), WoodKit.MasterGain, 1e-12);
            double loudest = 0, second = 0;
            foreach (var x in All())
            {
                WoodSounds.RenderForClip(x.Sfx, x.Arg, out var l, out var r);
                string name = WoodSounds.KeyOf(x.Sfx, x.Arg);
                double stored = SoundMeter.Peak(l, r), calculated = SoundMeter.Peak(x.L, x.R);
                Assert.That(stored / calculated, Is.InRange(0.75 * WoodKit.MasterGain, 1.06 * WoodKit.MasterGain), name + ": lo guardado es lo calculado con el volumen maestro (el filtro de 22 kHz le quita al pico hasta un 25 % en las notas agudas con clic y parcial metálico (la kalimba de 2 kHz suena un parcial a 12 kHz) y a veces suma un poco)");
                double played = stored * WoodKit.PlayVolume;                       // el pico ya reproducido
                Assert.LessOrEqual(played, 0.5, name + ": ya reproducido no pasa de −6 dBFS (pico " + SoundMeter.ToDb(played).ToString("0.0") + ")");
                if (played > loudest) { second = loudest; loudest = played; } else if (played > second) second = played;
            }
            Assert.LessOrEqual(loudest + second, 1.0, "dos de los sonidos más fuertes a la vez no saturan");
        }

        [Test]
        public void TheHalfRate_KeepsTheLowAndTheMiddle_AndFiltersWhatWouldFold()
        {
            var tone = SoundKit.Render(0.5, t => Math.Sin(SoundKit.Tau * 1000.0 * t));
            var half = SoundKit.HalveRate(tone);
            Assert.AreEqual(tone.Length / 2, half.Length);
            Assert.AreEqual(1.0, SteadyPeak(half), 0.03, "1 kHz pasa igual");
            var high = SoundKit.Render(0.5, t => Math.Sin(SoundKit.Tau * 14000.0 * t));
            Assert.Less(SteadyPeak(SoundKit.HalveRate(high)), 0.1, "14 kHz (que se doblaría) casi no pasa");
            var kalimbaTop = SoundKit.Render(0.5, t => Math.Sin(SoundKit.Tau * 9000.0 * t));
            Assert.Greater(SteadyPeak(SoundKit.HalveRate(kalimbaTop)), 0.5, "los 9 kHz de la kalimba más aguda todavía pasan");
        }

        /// <summary>El pico del tramo del medio (sin el golpe de arranque del tono de prueba).</summary>
        private static double SteadyPeak(float[] x)
        {
            double peak = 0;
            for (int i = x.Length / 4; i < x.Length / 2; i++) peak = Math.Max(peak, Math.Abs((double)x[i]));
            return peak;
        }

        [Test]
        public void TheStoredClips_KeepTheirLevelAndTheirPitch()
        {
            foreach (var x in All())
            {
                WoodSounds.RenderForClip(x.Sfx, x.Arg, out var l, out var r);
                string name = WoodSounds.KeyOf(x.Sfx, x.Arg);
                Assert.AreEqual(WoodSounds.IsMono(x.Sfx), r == null, name);
                Assert.LessOrEqual(SoundMeter.Peak(l, r), 0.43 * WoodKit.MasterGain, name + ": con el volumen maestro, el filtro no sube el pico más de un par de dB");
                Assert.AreEqual(0f, l[l.Length - 1], 1e-5f, name);
            }
        }

        [Test]
        public void TheRenders_AreDeterministic_AndTheVariantsDiffer()
        {
            WoodSounds.RenderStereo(WoodSfx.Perfect, 0, out var a, out var b);
            WoodSounds.RenderStereo(WoodSfx.Perfect, 0, out var a2, out var b2);
            Assert.AreEqual(a.Length, a2.Length);
            for (int i = 0; i < a.Length; i++) { Assert.AreEqual(a[i], a2[i]); Assert.AreEqual(b[i], b2[i]); }
            WoodSounds.RenderStereo(WoodSfx.Correct, 1, out var c1, out _);
            WoodSounds.RenderStereo(WoodSfx.Correct, 5, out var c5, out _);
            bool differ = c1.Length != c5.Length;
            for (int i = 0; !differ && i < c1.Length; i++) if (c1[i] != c5[i]) differ = true;
            Assert.IsTrue(differ, "la racha 5 no suena igual que la 1");
        }

        [Test]
        public void FinishAndPerfect_AreTheApprovedRescueOnes()
        {
            AssertSameAsRescue(WoodSfx.Finish, RadarSfx.Finale);
            AssertSameAsRescue(WoodSfx.Perfect, RadarSfx.Perfect);
        }

        private static void AssertSameAsRescue(WoodSfx wood, RadarSfx radar)
        {
            WoodSounds.RenderStereo(wood, 0, out var l, out var r);
            RadarSounds.RenderStereo(radar, 0, out var rl, out var rr);
            Assert.LessOrEqual(l.Length, rl.Length, wood + ": la de la paleta es la de Rescate sin el silencio del final");
            int same = l.Length - (int)Math.Round(0.03 * SoundKit.Rate) - 10;
            for (int i = 0; i < same; i++)
            {
                Assert.AreEqual(rl[i], l[i], 1e-6f, wood + " izquierda " + i);
                Assert.AreEqual(rr[i], r[i], 1e-6f, wood + " derecha " + i);
            }
        }

        [Test]
        public void TheNotesWithInformation_KeepTheirPitch_InBothVoices()
        {
            foreach (double hz in new[] { 392.0, 440.0, 523.25, 783.99, 1046.5, 1760.0 })
                foreach (var voice in new[] { WoodVoice.Kalimba, WoodVoice.Marimba })
                {
                    WoodSounds.Render(WoodSounds.MakeNote(hz, voice), out var l, out var r);
                    string name = voice + " " + hz;
                    Assert.Greater(SoundMeter.PitchClarity(l, r, hz), 0.6, name + ": la energía de la nota cae en su altura");
                    Assert.Less(SoundMeter.PitchClarity(l, r, hz * 1.06), 0.3, name + ": y no en el semitono de arriba");
                    Assert.Less(SoundMeter.OnsetMs(l, r), 3.0, name);
                }
        }

        [Test]
        public void TheNoteWithATighterDecay_FallsFasterAndKeepsItsPitch()
        {
            // paso 0 de las Fases C a E: MakeNote(hz, voz, tau). Rastro necesita 0,2 s y la cascada de Engranajes 0,15 s; la de siempre (0,3 s) cae a -20 dB cerca de los 0,7 s (con la sala de Rescate)
            var table = new System.Text.StringBuilder("voz hz: -20 dB con tau 0,3 / 0,2 / 0,15 (ms)");
            var rows = new List<(string Name, double T30, double T20, double T15)>();
            foreach (var voice in new[] { WoodVoice.Kalimba, WoodVoice.Marimba })
                foreach (double hz in new[] { 440.0, 783.99, 1318.51 })
                {
                    double t30 = Decay(WoodSounds.MakeNote(hz, voice), hz), t20 = Decay(WoodSounds.MakeNote(hz, voice, 0.2), hz), t15 = Decay(WoodSounds.MakeNote(hz, voice, 0.15), hz);
                    rows.Add((voice + " " + hz, t30, t20, t15));
                    table.Append("\n  " + voice + " " + hz + ": " + t30.ToString("0") + " / " + t20.ToString("0") + " / " + t15.ToString("0"));
                    foreach (double tau in new[] { 0.2, 0.15 })
                    {
                        WoodSounds.Render(WoodSounds.MakeNote(hz, voice, tau), out var l, out var r);
                        Assert.Greater(SoundMeter.PitchClarity(l, r, hz), 0.6, voice + " " + hz + " tau " + tau + ": la misma claridad de altura");
                        Assert.Less(SoundMeter.OnsetMs(l, r), 3.0, voice + " " + hz + " tau " + tau + ": empieza en menos de 3 ms");
                    }
                }
            foreach (var row in rows)
            {
                string why = row.Name + table;
                Assert.That(row.T30, Is.InRange(550.0, 800.0), why);
                Assert.Less(row.T20, row.T30 - 120.0, why + "\n  tau 0,2 cae bastante antes que tau 0,3");
                Assert.Less(row.T15, row.T20 - 30.0, why + "\n  y tau 0,15 antes que tau 0,2");
                Assert.That(row.T20, Is.InRange(300.0, 500.0), why);
                Assert.That(row.T15, Is.InRange(200.0, 450.0), why);
            }
            var a = WoodSounds.MakeNote(523.25, WoodVoice.Kalimba);
            var b = WoodSounds.MakeNote(523.25, WoodVoice.Kalimba, 0.3);
            Assert.AreEqual(a.X.Length, b.X.Length);
            for (int i = 0; i < a.X.Length; i++) Assert.AreEqual(a.X[i], b.X[i], 0f, "sin tau es la de siempre, muestra por muestra");
            Assert.AreEqual(0.30, a.Level, 1e-9);
        }

        private static double Decay(WoodSounds.Recipe recipe, double hz)
        {
            WoodSounds.Render(recipe, out var l, out var r);
            return SoundMeter.DecayMs(l, r, 20.0);
        }

        [Test]
        public void TheGenericBakeJob_GivesTheSameClip_AsToClip_ForARecipeOfThePalette()
        {
            foreach (var (sfx, arg) in new[] { (WoodSfx.Select, 2), (WoodSfx.Tap, 0), (WoodSfx.Perfect, 0), (WoodSfx.Hum, 0) })
            {
                var recipe = WoodSounds.MakeRecipe(sfx, arg);
                string key = WoodSounds.KeyOf(sfx, arg);
                bool mono = WoodSounds.IsMono(sfx);
                var job = new WoodSounds.BakeJob(recipe, key, mono);
                int steps = 0;
                while (!job.Step(8000)) steps++;
                Assert.Greater(steps, 1, key + ": se hornea de a poco");
                var direct = WoodSounds.ToClip(sfx, arg);
                var viaRecipe = WoodSounds.ToClip(recipe, key, mono);
                foreach (var other in new[] { direct, viaRecipe })
                {
                    Assert.AreEqual(other.channels, job.Clip.channels, key);
                    Assert.AreEqual(other.samples, job.Clip.samples, key);
                    var x = new float[other.samples * other.channels];
                    var y = new float[x.Length];
                    other.GetData(x, 0);
                    job.Clip.GetData(y, 0);
                    for (int i = 0; i < x.Length; i++) Assert.AreEqual(x[i], y[i], 0f, key + " muestra " + i);
                }
                Assert.AreEqual(key, job.Clip.name);
            }
            // y con una receta suelta (no de la paleta) el trabajo con la receta en el primer paso da lo mismo que con la receta ya armada
            var own = new WoodSounds.Recipe(WoodKit.Woodblock(900), 0.1, 0.1, WoodTier.Micro);
            var lazy = new WoodSounds.BakeJob(() => own, "propio", true);
            while (!lazy.Step()) { }
            var eager = WoodSounds.ToClip(own, "propio", true);
            Assert.AreEqual(eager.samples, lazy.Clip.samples);
            Assert.AreEqual(1, lazy.Clip.channels, "mono");
        }

        [Test]
        public void TheCue_IsShortAndStartsRightAway()
        {
            foreach (double hz in new[] { 147.0, 330.0, 784.0, 988.0 })
            {
                WoodSounds.Render(WoodSounds.MakeCue(hz, 0.1), out var l, out var r);
                Assert.LessOrEqual(l.Length * 1000.0 / SoundKit.Rate, 1100.0, "la señal de " + hz + " Hz (con su poca sala) cabe en 1,1 s");
                Assert.LessOrEqual(SoundMeter.DurationMs(l, r, 30.0), 250.0, "y el sonido en sí, que dura lo mismo que el tono de hoy (0,1 s), cae a −30 dB en menos de 0,25 s");
                Assert.LessOrEqual(SoundMeter.OnsetMs(l, r), 2.0, "y empieza de inmediato");
                Assert.Greater(SoundMeter.PitchClarity(l, r, hz, 2, 120), 0.5, "en su altura");
            }
        }
    }
}
