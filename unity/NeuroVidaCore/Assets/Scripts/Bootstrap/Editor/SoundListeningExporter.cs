using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using NeuroVida.Games.Shared;

namespace NeuroVida.Bridge.EditorTools
{
    /// <summary>
    /// Exportador de la PÁGINA DE ESCUCHA de sonido (Tarea 119, Fase A; docs/sonido-madera-plan.md): por cada evento de los 17 juegos y de lo común, el sonido de HOY y el de MADERA CÁLIDA, renderizados con el MISMO C# que suena en el juego (las clases <c>XSounds</c> de cada juego, <c>GameFeel</c> por
    /// reflexión y la paleta <see cref="WoodSounds"/>), y sus medidas (pico, sonoridad, inicio, duración, memoria). Escribe un WAV por sonido (mono, 22.050 Hz, 16 bits, recortado al silencio y normalizado a pico 0,9: la ganancia real va en el catálogo) en <c>docs/previews/sonido-escucha/</c> y el catálogo en <c>tools/sonido/catalogo.json</c>;
    /// <c>tools/sonido/armar_escucha.py</c> arma con eso <c>docs/previews/sonido-escucha.html</c>. Solo corre en el Editor (no entra al APK).
    /// Uso: menú «NeuroVida/Sonido/Exportar página de escucha» o <c>Unity -batchmode -executeMethod NeuroVida.Bridge.EditorTools.SoundListeningExporter.ExportBatch [-soundOut carpeta]</c>.
    /// </summary>
    public static partial class SoundListeningExporter
    {
        public const int Rate = 44100;
        public const int OutRate = 22050;
        private const double OutPeak = 0.9;

        // ------------------------------------------------------------------ el modelo

        /// <summary>Un sonido renderizado (44.100 Hz; <see cref="R"/> es null si es mono).</summary>
        internal sealed class Clip
        {
            public string Key;
            public float[] L, R;
            /// <summary>Un sonido de «hoy» CONGELADO (Tarea 133, paso 0): no se renderiza; su WAV y sus medidas salen del catálogo del 17-oct (<c>tools/sonido/catalogo-hoy-17-oct.json</c>). Así «hoy» sigue siendo el sonido real que sonaba aunque se borren las clases <c>XSounds</c> de cada juego.</summary>
            public bool Frozen;
            public int FrozenChannels, FrozenSamples;
            public int Channels => Frozen ? FrozenChannels : (R == null ? 1 : 2);
            public int Samples => Frozen ? FrozenSamples : L.Length;
            // medidas (con ganancia 1)
            public double Peak, Lufs, OnsetMs, AttackMs, DurMs, F0Frac, T20Ms;
            public double F0Hz;
            public string File;
            public int WavBytes;
            /// <summary>Lo que ocupa guardado como lo guarda el juego (paleta: 22.050 Hz y mono en los micro), en bytes; 0 si no es de la paleta.</summary>
            public long StoredBytes;
        }

        /// <summary>Un evento del juego: qué pasa, cómo suena hoy (<see cref="HoyKey"/>), dónde se llama y a qué sonido de la paleta se mapea (<see cref="WoodKey"/>).</summary>
        internal sealed class Ev
        {
            public string Group, Id, Title, Today, Sites, Map, Flag = "", Note = "";
            public string HoyKey, WoodKey, WoodAltKey;
            /// <summary>La fase del plan en que se migra el juego del evento: «B» lo común (ya suena), «C», «D» o «E»; vacío = el juego ya está en Madera (Rescate, Acoplamiento y Aterrizaje).</summary>
            public string Phase = "";
            /// <summary>Si el evento NO tiene sonido de Madera a propósito (se quita, o se queda como está), lo que dice la página en lugar del botón.</summary>
            public string NoWood = "";
            /// <summary>Lo que dice el segundo botón de la página (por defecto «Madera»): en la página de la pista de Constelaciones cada evento es una OPCIÓN («Opción (a)»).</summary>
            public string WoodLabel = "";
            public float HoyGain = 1f, WoodGain = WoodSounds.PlayVolume;
            public bool AlreadyWood;
            public string WoodAltLabel = "Madera (marimba)";
            /// <summary>Solo los eventos con un sonido de Madera (o de la paleta) llevan botones en la página; los demás se miden y se listan, pero no se tocan en esta fase.</summary>
            public bool Audio => !string.IsNullOrEmpty(WoodKey) || !string.IsNullOrEmpty(NoWood);          // un evento que se queda sin Madera a propósito lleva igual su botón «Hoy» y la explicación
        }

        internal static readonly List<Ev> Events = new List<Ev>();
        private static readonly Dictionary<string, Func<Clip>> Factories = new Dictionary<string, Func<Clip>>();
        private static readonly Dictionary<string, Clip> Rendered = new Dictionary<string, Clip>();
        internal static readonly List<string> ClipOrder = new List<string>();

        // ------------------------------------------------------------------ entradas

        [MenuItem("NeuroVida/Sonido/Exportar página de escucha")]
        public static void ExportMenu() => Export(DefaultOut());

        /// <summary>Para <c>-executeMethod</c>: <c>-soundOut carpeta</c> cambia la carpeta de los WAV.</summary>
        public static void ExportBatch()
        {
            var args = Environment.GetCommandLineArgs();
            string dir = DefaultOut();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-soundOut") dir = args[i + 1];
            try
            {
                Export(dir);
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogError("[SonidoEscucha] FALLÓ: " + e);
                EditorApplication.Exit(1);
            }
        }

        private static string RepoRoot() => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", ".."));

        private static string DefaultOut() => Path.Combine(RepoRoot(), "docs", "previews", "sonido-escucha");

        // ------------------------------------------------------------------ exportar

        private static void Export(string outDir)
        {
            Events.Clear();
            Factories.Clear();
            Rendered.Clear();
            ClipOrder.Clear();
            RegisterAll();
            Directory.CreateDirectory(outDir);
            // (Tarea 133) ya NO se borran todos los WAV al empezar: los de «hoy» congelados (del 17-oct) tienen que quedarse; los demás se reescriben con los mismos bytes (el render es determinista)

            // 1) renderizar cada sonido que usa algún evento
            var used = new List<string>();
            foreach (var e in Events)
                foreach (var k in new[] { e.HoyKey, e.WoodKey, e.WoodAltKey })
                    if (!string.IsNullOrEmpty(k) && !used.Contains(k)) used.Add(k);
            foreach (var k in used)
            {
                if (!Factories.ContainsKey(k)) throw new InvalidOperationException("sonido sin receta: " + k);
                Measure(Get(k));
            }
            // la paleta completa (todas las variantes): para medir la memoria aunque ningún evento las nombre
            foreach (WoodSfx sfx in Enum.GetValues(typeof(WoodSfx)))
                foreach (int arg in WoodSounds.Variants(sfx))
                {
                    string k = "wood." + WoodSounds.KeyOf(sfx, arg);
                    if (!Factories.ContainsKey(k)) Factories[k] = () => FromWood(k, sfx, arg);
                    if (!used.Contains(k)) { Measure(Get(k)); used.Add(k); }
                }

            // 2) escribir los WAV de lo que la página toca (los idénticos se comparten)
            var needed = new HashSet<string>();
            foreach (var e in Events)
            {
                if (!e.Audio) continue;
                foreach (var k in new[] { e.HoyKey, e.WoodKey, e.WoodAltKey }) if (!string.IsNullOrEmpty(k)) needed.Add(k);
            }
            var byHash = new Dictionary<string, string>();
            foreach (var k in used)
            {
                if (!needed.Contains(k)) continue;
                var c = Get(k);
                if (c.Frozen)
                {
                    if (string.IsNullOrEmpty(c.File) || !File.Exists(Path.Combine(outDir, c.File))) throw new InvalidOperationException("el WAV congelado de «" + k + "» (" + c.File + ") no está en " + outDir);
                    continue;
                }
                short[] pcm = ToWav22(c);
                string hash = Hash(pcm);
                if (byHash.TryGetValue(hash, out var existing)) { c.File = existing; c.WavBytes = 0; continue; }
                c.File = Safe(k) + ".wav";
                byHash[hash] = c.File;
                File.WriteAllBytes(Path.Combine(outDir, c.File), WavBytes(pcm, OutRate));
                c.WavBytes = 44 + pcm.Length * 2;
            }

            // 3) el catálogo
            string cat = Path.Combine(RepoRoot(), "tools", "sonido", "catalogo.json");
            Directory.CreateDirectory(Path.GetDirectoryName(cat));
            File.WriteAllText(cat, CatalogJson(used), new UTF8Encoding(false));
            long total = 0;
            var seen = new HashSet<string>();
            foreach (var k in used) { var c = Get(k); if (c.File != null && seen.Add(c.File)) total += c.WavBytes; }
            Debug.Log("[SonidoEscucha] OK: " + Events.Count + " eventos, " + seen.Count + " WAV (" + (total / 1024) + " KB) en " + outDir + "; catálogo en " + cat);
        }

        // ------------------------------------------------------------------ registrar sonidos

        internal static Clip Get(string key)
        {
            if (Rendered.TryGetValue(key, out var c)) return c;
            c = Factories[key]();
            c.Key = key;
            Rendered[key] = c;
            return c;
        }

        /// <summary>Un sonido de HOY que ya es un clip del juego.</summary>
        internal static void H(string key, Func<AudioClip> make) => Factories[key] = () => FromClip(make());

        /// <summary>
        /// Un sonido de HOY congelado (Tarea 133): NO se renderiza; toma el WAV ya escrito y las medidas del catálogo del 17-oct (<c>tools/sonido/catalogo-hoy-17-oct.json</c>, solo lectura). Cuando un juego se migra a Madera y se borran sus clases <c>XSounds</c>, sus líneas
        /// <c>H("bodega.hum", () => BodegaSounds.Hum())</c> pasan a <c>Frozen("bodega.hum")</c>: «hoy» sigue siendo el sonido real que sonaba.
        /// </summary>
        internal static void Frozen(string key) => Factories[key] = () => FrozenClip(key);

        private static string _frozenText;

        private static string FrozenText() => _frozenText ?? (_frozenText = File.ReadAllText(Path.Combine(RepoRoot(), "tools", "sonido", "catalogo-hoy-17-oct.json")));

        /// <summary>Las medidas de la nota <paramref name="i"/> de la familia <paramref name="famId"/> con el sonido de HOY (afinación, caída, inicio y duración), del catálogo congelado del 17-oct.</summary>
        private static double[] FrozenHoyNote(string famId, int i)
        {
            string t = FrozenText(); int fams = t.IndexOf("\"families\":[", StringComparison.Ordinal);
            int at = fams < 0 ? -1 : t.IndexOf("{\"id\":\"" + famId + "\"", fams, StringComparison.Ordinal);
            if (at < 0) throw new InvalidOperationException("la familia «" + famId + "» no está en el catálogo congelado del 17-oct");
            var ms = System.Text.RegularExpressions.Regex.Matches(t.Substring(at), "\"hoyF0\":(null|-?[0-9.]+),\"hoyT20\":(null|-?[0-9.]+),\"hoyOnset\":(null|-?[0-9.]+),\"hoyDur\":(null|-?[0-9.]+)");
            if (i >= ms.Count) throw new InvalidOperationException("la nota " + i + " de «" + famId + "» no está en el catálogo congelado");
            var r = new double[4];
            for (int k = 0; k < 4; k++) r[k] = ms[i].Groups[k + 1].Value == "null" ? double.NaN : double.Parse(ms[i].Groups[k + 1].Value, CultureInfo.InvariantCulture);
            return r;
        }

        private static Clip FrozenClip(string key)
        {
            var m = System.Text.RegularExpressions.Regex.Match(FrozenText(), "\"" + System.Text.RegularExpressions.Regex.Escape(key) + "\":\\{([^}]*)\\}");
            if (!m.Success) throw new InvalidOperationException("«" + key + "» no está en el catálogo congelado del 17-oct (tools/sonido/catalogo-hoy-17-oct.json)");
            string body = m.Groups[1].Value;
            double Num(string name)
            {
                var n = System.Text.RegularExpressions.Regex.Match(body, "\"" + name + "\":(null|-?[0-9.]+(?:[eE][-+]?[0-9]+)?)");
                if (!n.Success) throw new InvalidOperationException("«" + key + "» congelado no trae «" + name + "»");
                return n.Groups[1].Value == "null" ? 0.0 : double.Parse(n.Groups[1].Value, CultureInfo.InvariantCulture);
            }
            var f = System.Text.RegularExpressions.Regex.Match(body, "\"file\":\"([^\"]*)\"");
            if (!f.Success) throw new InvalidOperationException("«" + key + "» congelado no tiene WAV");
            return new Clip
            {
                Frozen = true, File = f.Groups[1].Value, FrozenChannels = (int)Num("channels"), FrozenSamples = (int)Num("samples"), WavBytes = (int)Num("wavBytes"), StoredBytes = (long)Num("bytesStored"),
                Peak = Num("peak"), Lufs = Num("lufs"), OnsetMs = Num("onsetMs"), AttackMs = Num("attackMs"), DurMs = Num("durMs"), T20Ms = Num("t20Ms"), F0Hz = Num("f0Hz"), F0Frac = Num("f0Frac"),
            };
        }

        /// <summary>Un sonido de hoy hecho de muestras mono.</summary>
        internal static void HRaw(string key, Func<float[]> make) => Factories[key] = () => new Clip { L = make() };

        /// <summary>Un sonido de la paleta de Madera cálida.</summary>
        internal static void W(string key, WoodSfx sfx, int arg = 0) => Factories[key] = () => FromWood(key, sfx, arg);

        internal static void Raw(string key, Func<Clip> make) => Factories[key] = make;

        private static Clip FromWood(string key, WoodSfx sfx, int arg)
        {
            WoodSounds.RenderStereo(sfx, arg, out var l, out var r);
            WoodSounds.RenderForClip(sfx, arg, out var sl, out var sr);
            return new Clip { L = l, R = r, StoredBytes = (long)sl.Length * (sr == null ? 1 : 2) * 4 };
        }

        internal static Clip FromRecipe(WoodSounds.Recipe recipe)
        {
            WoodSounds.Render(recipe, out var l, out var r);
            return new Clip { L = l, R = r };
        }

        internal static Clip FromClip(AudioClip clip)
        {
            if (clip == null) throw new InvalidOperationException("el juego devolvió un clip vacío");
            int ch = clip.channels, n = clip.samples;
            var data = new float[n * ch];
            clip.GetData(data, 0);
            var l = new float[n];
            float[] r = ch > 1 ? new float[n] : null;
            for (int i = 0; i < n; i++)
            {
                l[i] = data[i * ch];
                if (r != null) r[i] = data[i * ch + 1];
            }
            return new Clip { L = l, R = r };
        }

        /// <summary>Varias notas una tras otra cada <paramref name="spacing"/> s (una escala o una frase): cada nota empieza en su lugar y deja sonar su cola.</summary>
        internal static Clip Sequence(IList<Clip> notes, IList<double> starts)
        {
            int n = 0;
            bool stereo = false;
            for (int i = 0; i < notes.Count; i++)
            {
                n = Math.Max(n, (int)Math.Round(starts[i] * Rate) + notes[i].Samples);
                stereo |= notes[i].R != null;
            }
            var l = new float[n];
            var r = stereo ? new float[n] : null;
            for (int i = 0; i < notes.Count; i++)
            {
                int o = (int)Math.Round(starts[i] * Rate);
                var c = notes[i];
                for (int j = 0; j < c.Samples; j++)
                {
                    l[o + j] += c.L[j];
                    if (r != null) r[o + j] += c.R != null ? c.R[j] : c.L[j];
                }
            }
            return new Clip { L = l, R = r };
        }

        /// <summary>Una escala de notas con el mismo espaciado.</summary>
        internal static Clip Scale(int count, double spacing, Func<int, Clip> note, double firstAt = 0.0)
        {
            var notes = new List<Clip>();
            var starts = new List<double>();
            for (int i = 0; i < count; i++) { notes.Add(note(i)); starts.Add(firstAt + i * spacing); }
            return Sequence(notes, starts);
        }

        /// <summary>Los primeros <paramref name="seconds"/> s del sonido con un fundido final (para las escalas de la página: cada nota deja de sonar cuando entra la siguiente).</summary>
        internal static Clip Trimmed(Clip c, double seconds, double fadeSeconds = 0.04)
        {
            int n = Math.Min(c.Samples, (int)Math.Round(seconds * Rate)), f = Math.Min(n, (int)Math.Round(fadeSeconds * Rate));
            var l = new float[n];
            Array.Copy(c.L, l, n);
            float[] r = null;
            if (c.R != null) { r = new float[n]; Array.Copy(c.R, r, n); }
            for (int i = 0; i < f; i++)
            {
                float k = i / (float)f;
                l[n - 1 - i] *= k;
                if (r != null) r[n - 1 - i] *= k;
            }
            return new Clip { L = l, R = r };
        }

        internal static Clip Scaled(Clip c, double gain)
        {
            var l = new float[c.Samples];
            for (int i = 0; i < l.Length; i++) l[i] = (float)(c.L[i] * gain);
            float[] r = null;
            if (c.R != null)
            {
                r = new float[c.Samples];
                for (int i = 0; i < r.Length; i++) r[i] = (float)(c.R[i] * gain);
            }
            return new Clip { L = l, R = r };
        }

        // ------------------------------------------------------------------ medidas

        private static void Measure(Clip c)
        {
            if (c.Frozen) return;                                              // las medidas del congelado salen del catálogo del 17-oct
            c.Peak = Math.Max(SoundMeter.Peak(c.L, c.R), 1e-12);
            c.Lufs = SoundMeter.MomentaryLufs(c.L, c.R);
            c.DurMs = SoundMeter.DurationMs(c.L, c.R);
            c.OnsetMs = SoundMeter.OnsetMs(c.L, c.R, 0.1);
            c.AttackMs = SoundMeter.OnsetMs(c.L, c.R, 0.9);
            c.T20Ms = SoundMeter.DecayMs(c.L, c.R, 20.0);
        }

        private static int LastAbove(Clip c, double threshold)
        {
            for (int i = c.Samples - 1; i >= 0; i--)
            {
                if (Math.Abs((double)c.L[i]) >= threshold) return i;
                if (c.R != null && Math.Abs((double)c.R[i]) >= threshold) return i;
            }
            return 0;
        }

        // ------------------------------------------------------------------ WAV: 22.050 Hz, mono, 16 bits, recortado y normalizado

        /// <summary>Mono, recortado hasta -38 dB del pico (con un fundido de 25 ms), a 22.050 Hz con el mismo filtro que usa el juego (<see cref="SoundKit.HalveRate"/>) y normalizado a pico 0,9.</summary>
        private static short[] ToWav22(Clip c)
        {
            int end = Math.Min(c.Samples, Math.Max(LastAbove(c, c.Peak * Math.Pow(10.0, -38.0 / 20.0)) + 1, Rate / 8) + Rate / 40);
            var mono = new float[end];
            for (int i = 0; i < end; i++) mono[i] = c.R == null ? c.L[i] : 0.5f * (c.L[i] + c.R[i]);
            int fade = Math.Min(end, Rate / 40);
            for (int i = 0; i < fade; i++) mono[end - 1 - i] *= i / (float)fade;
            var half = SoundKit.HalveRate(mono);
            double peak = 1e-12;
            foreach (var v in half) peak = Math.Max(peak, Math.Abs((double)v));
            var pcm = new short[half.Length];
            double g = OutPeak / peak;
            for (int j = 0; j < pcm.Length; j++) pcm[j] = (short)Math.Round(Math.Max(-1.0, Math.Min(1.0, half[j] * g)) * 32767.0);
            return pcm;
        }

        private static byte[] WavBytes(short[] pcm, int rate)
        {
            int dataBytes = pcm.Length * 2;
            using (var ms = new MemoryStream(44 + dataBytes))
            using (var w = new BinaryWriter(ms))
            {
                w.Write(Encoding.ASCII.GetBytes("RIFF"));
                w.Write(36 + dataBytes);
                w.Write(Encoding.ASCII.GetBytes("WAVE"));
                w.Write(Encoding.ASCII.GetBytes("fmt "));
                w.Write(16);
                w.Write((short)1);
                w.Write((short)1);
                w.Write(rate);
                w.Write(rate * 2);
                w.Write((short)2);
                w.Write((short)16);
                w.Write(Encoding.ASCII.GetBytes("data"));
                w.Write(dataBytes);
                foreach (var s in pcm) w.Write(s);
                return ms.ToArray();
            }
        }

        private static string Hash(short[] pcm)
        {
            ulong h = 1469598103934665603UL;
            foreach (var s in pcm) { h ^= (ushort)s; h *= 1099511628211UL; }
            return h.ToString("x") + "-" + pcm.Length;
        }

        private static string Safe(string key)
        {
            var sb = new StringBuilder();
            foreach (char ch in key) sb.Append(char.IsLetterOrDigit(ch) && ch < 128 ? ch : '-');
            return sb.ToString();
        }

        // ------------------------------------------------------------------ el catálogo (JSON)

        private static string Q(string s)
        {
            if (s == null) return "null";
            var sb = new StringBuilder("\"");
            foreach (char ch in s)
            {
                if (ch == '"') sb.Append("\\\"");
                else if (ch == '\\') sb.Append("\\\\");
                else if (ch == '\n') sb.Append("\\n");
                else if (ch < 32) sb.Append(' ');
                else sb.Append(ch);
            }
            return sb.Append('"').ToString();
        }

        private static string N(double v) => double.IsNaN(v) || double.IsInfinity(v) ? "null" : v.ToString("0.####", CultureInfo.InvariantCulture);

        private static string Db(double linear) => N(20.0 * Math.Log10(Math.Max(linear, 1e-9)));

        /// <summary>La afinación y la caída de cada nota con información: la de hoy, la de kalimba y la de marimba (o, en las señales cortas, la marimba seca).</summary>
        private static string FamiliesJson()
        {
            var sb = new StringBuilder("\"families\":[\n");
            for (int f = 0; f < Families.Count; f++)
            {
                var fam = Families[f];
                bool cues = fam.Id == "senales";
                sb.Append("{\"id\":").Append(Q(fam.Id)).Append(",\"title\":").Append(Q(fam.Title)).Append(",\"cues\":").Append(cues ? "true" : "false").Append(",\"notes\":[");
                for (int i = 0; i < fam.Hz.Length; i++)
                {
                    double hz = fam.Hz[i];
                    var hoy = fam.Hoy == null ? null : fam.Hoy(i);
                    double[] fz = hoy == null ? FrozenHoyNote(fam.Id, i) : null;                // F0, T20, inicio y duración de «hoy»: medidos, o del catálogo congelado
                    if (hoy != null) Measure(hoy);
                    int window = cues ? 100 : 300;                                           // las señales duran 0,1 s: se mide su altura en ese tramo
                    Clip kal = null, mar;
                    if (cues) mar = FromRecipe(WoodSounds.MakeCue(hz, 0.1));
                    else { kal = FromRecipe(WoodSounds.MakeNote(hz, WoodVoice.Kalimba)); mar = FromRecipe(WoodSounds.MakeNote(hz, WoodVoice.Marimba)); }
                    Measure(mar);
                    if (kal != null) Measure(kal);
                    sb.Append("{\"hz\":").Append(N(hz))
                      .Append(",\"hoyF0\":").Append(N(hoy != null ? SoundMeter.PitchClarity(hoy.L, hoy.R, hz, 2, window) : fz[0])).Append(",\"hoyT20\":").Append(N(hoy != null ? hoy.T20Ms : fz[1])).Append(",\"hoyOnset\":").Append(N(hoy != null ? hoy.OnsetMs : fz[2])).Append(",\"hoyDur\":").Append(N(hoy != null ? hoy.DurMs : fz[3]))
                      .Append(",\"marF0\":").Append(N(SoundMeter.PitchClarity(mar.L, mar.R, hz, 2, window))).Append(",\"marT20\":").Append(N(mar.T20Ms)).Append(",\"marOnset\":").Append(N(mar.OnsetMs)).Append(",\"marDur\":").Append(N(mar.DurMs));
                    if (kal != null) sb.Append(",\"kalF0\":").Append(N(SoundMeter.PitchClarity(kal.L, kal.R, hz, 2, window))).Append(",\"kalT20\":").Append(N(kal.T20Ms)).Append(",\"kalOnset\":").Append(N(kal.OnsetMs)).Append(",\"kalDur\":").Append(N(kal.DurMs));
                    sb.Append('}').Append(i + 1 < fam.Hz.Length ? "," : "");
                }
                sb.Append("]}").Append(f + 1 < Families.Count ? ",\n" : "\n");
            }
            return sb.Append("]").ToString();
        }

        private static string CatalogJson(List<string> used)
        {
            var sb = new StringBuilder();
            sb.Append("{\n\"wavRate\":").Append(OutRate).Append(",\n\"wavPeak\":").Append(N(OutPeak)).Append(",\n\"playVolume\":").Append(N(WoodSounds.PlayVolume)).Append(",\n\"clips\":{\n");
            for (int i = 0; i < used.Count; i++)
            {
                var c = Get(used[i]);
                sb.Append(Q(c.Key)).Append(":{\"file\":").Append(Q(c.File))
                  .Append(",\"channels\":").Append(c.Channels)
                  .Append(",\"samples\":").Append(c.Samples)
                  .Append(",\"bytes44\":").Append((long)c.Samples * c.Channels * 4)
                  .Append(",\"bytesStored\":").Append(c.StoredBytes)
                  .Append(",\"wavBytes\":").Append(c.WavBytes)
                  .Append(",\"peak\":").Append(N(c.Peak))
                  .Append(",\"peakDb\":").Append(Db(c.Peak))
                  .Append(",\"lufs\":").Append(N(c.Lufs))
                  .Append(",\"onsetMs\":").Append(N(c.OnsetMs))
                  .Append(",\"attackMs\":").Append(N(c.AttackMs))
                  .Append(",\"durMs\":").Append(N(c.DurMs))
                  .Append(",\"t20Ms\":").Append(N(c.T20Ms))
                  .Append(",\"f0Hz\":").Append(N(c.F0Hz))
                  .Append(",\"f0Frac\":").Append(c.F0Hz > 0 ? N(c.F0Frac) : "null")
                  .Append(",\"scale\":").Append(N(c.Peak / OutPeak))                 // lo que hay que multiplicar el WAV (pico 0,9) para recuperar el nivel real del clip
                  .Append('}').Append(i + 1 < used.Count ? ",\n" : "\n");
            }
            sb.Append("},\n\"events\":[\n");
            for (int i = 0; i < Events.Count; i++)
            {
                var e = Events[i];
                sb.Append("{\"group\":").Append(Q(e.Group)).Append(",\"id\":").Append(Q(e.Id)).Append(",\"title\":").Append(Q(e.Title))
                  .Append(",\"today\":").Append(Q(e.Today)).Append(",\"sites\":").Append(Q(e.Sites)).Append(",\"map\":").Append(Q(e.Map))
                  .Append(",\"flag\":").Append(Q(e.Flag)).Append(",\"note\":").Append(Q(e.Note))
                  .Append(",\"hoy\":").Append(Q(e.HoyKey)).Append(",\"hoyGain\":").Append(N(e.HoyGain))
                  .Append(",\"wood\":").Append(Q(e.WoodKey)).Append(",\"woodGain\":").Append(N(e.WoodGain))
                  .Append(",\"woodAlt\":").Append(Q(e.WoodAltKey)).Append(",\"woodAltLabel\":").Append(Q(e.WoodAltLabel))
                  .Append(",\"alreadyWood\":").Append(e.AlreadyWood ? "true" : "false")
                  .Append(",\"phase\":").Append(Q(e.Phase)).Append(",\"noWood\":").Append(Q(e.NoWood)).Append(",\"woodLabel\":").Append(Q(e.WoodLabel)).Append('}')
                  .Append(i + 1 < Events.Count ? ",\n" : "\n");
            }
            sb.Append("],\n").Append(FamiliesJson()).Append("\n}\n");
            return sb.ToString();
        }
    }
}
