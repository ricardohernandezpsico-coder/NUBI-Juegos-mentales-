using System;
using System.IO;
using NeuroVida.Games.Anagramas;
using NeuroVida.Games.Calculo;
using NeuroVida.Games.CambioChip;
using NeuroVida.Games.Comparacion;
using NeuroVida.Games.Parejas;
using NeuroVida.Games.RutaTesoro;
using NeuroVida.Games.Secuencia;
using NeuroVida.Games.Series;
using NeuroVida.Games.Shared;
using NeuroVida.Games.Stroop;
using UnityEngine;

/// Vuelca cada sprite procedural a <carpeta>/<nombre>.raw (int32 lado + RGBA, fila 0 = abajo).
internal static class Program
{
    private static void Main(string[] args)
    {
        string dir = args.Length > 0 ? args[0] : "art-raw";
        Directory.CreateDirectory(dir);

        void DumpRect(string name, Color32[] px, int w, int h)
        {
            using var file = File.Create(Path.Combine(dir, name + ".raw"));
            var bw = new BinaryWriter(file);
            bw.Write(-w);
            bw.Write(h);
            foreach (var c in px) { bw.Write(c.r); bw.Write(c.g); bw.Write(c.b); bw.Write(c.a); }
        }

        void Dump(string name, Sprite sprite)
        {
            using var file = File.Create(Path.Combine(dir, name + ".raw"));
            var w = new BinaryWriter(file);
            w.Write(sprite.texture.width);
            foreach (var c in sprite.texture.pixels) { w.Write(c.r); w.Write(c.g); w.Write(c.b); w.Write(c.a); }
        }

        foreach (ShapeKind kind in Enum.GetValues(typeof(ShapeKind)))
            for (int v = 0; v < SymbolSprite.VariantCount; v++)
                Dump($"sym_{kind}_{v}", SymbolSprite.Get(kind, v));
        foreach (CardSprites.Face face in Enum.GetValues(typeof(CardSprites.Face)))
            Dump("card_" + face, CardSprites.Get(face));
        for (int i = 0; i < 6; i++) Dump("treasure_" + i, TreasureSprites.ForIndex(i));
        Dump("heart_full", HeartSprite.GetFull());
        Dump("heart_lost", HeartSprite.GetLost());
        for (int i = 0; i < TileGlyphSprite.Count; i++) Dump("glyph_" + i, TileGlyphSprite.Get(i));
        Dump("count_star", CountStarSprite.Get());
        Dump("magnifier", MagnifierSprite.Get());
        Dump("tile", TileSprites.Get());
        DumpRect("surface", LunarSurfaceSprite.Render(ClayRaster.Hex(0x9C94D6)), LunarSurfaceSprite.Width, LunarSurfaceSprite.Height);
        foreach (RuleBadgeSprite.Kind k in Enum.GetValues(typeof(RuleBadgeSprite.Kind))) Dump("badge_" + k, RuleBadgeSprite.Get(k));
        for (int d = 0; d < 4; d++) Dump("arrow_" + d, ClayArrowSprite.Get(d));
        for (int d = 0; d < 4; d++) Dump("ship_" + d, ChipShipSprite.Get((ChipDirection)d));
        for (int v = 0; v < 2; v++) Dump("lily_" + v, PondSprites.LilyPad(v));
        Dump("slot", AnagramSprites.Slot());
        foreach (AnagramSprites.Icon ic in Enum.GetValues(typeof(AnagramSprites.Icon))) Dump("icon_" + ic, AnagramSprites.ActionIcon(ic));
        Dump("screw", NeonSignSprites.Screw());
        Dump("mark_check", AnswerMarkSprite.Check());
        Dump("mark_cross", AnswerMarkSprite.Cross());
        Dump("radar_scope", NeuroVida.Games.Radar.RadarSprites.Scope());
        Dump("radar_sweep", NeuroVida.Games.Radar.RadarSprites.Sweep());
        Dump("radar_pad", NeuroVida.Games.Radar.RadarSprites.Pad());
        for (int i = 0; i < 2; i++) Dump("radar_mask_" + i, NeuroVida.Games.Radar.RadarSprites.Mask(i));
        Dump("brake_stop", NeuroVida.Games.Freno.BrakeSprites.StopSign());
        Dump("brake_pad", NeuroVida.Games.Freno.BrakeSprites.LaunchPad());
        Dump("brake_button", NeuroVida.Games.Freno.BrakeSprites.LaunchButton(false));
        Dump("brake_button_lit", NeuroVida.Games.Freno.BrakeSprites.LaunchButton(true));
        Dump("land_lander", NeuroVida.Games.Aterrizaje.LandingSprites.Lander());
        Dump("land_flag", NeuroVida.Games.Aterrizaje.LandingSprites.Flag());
        Dump("dock_cell", NeuroVida.Games.Acoplamiento.DockingSprites.ModuleCell());
        Dump("dock_socket", NeuroVida.Games.Acoplamiento.DockingSprites.SocketCell());
        Dump("dock_sil", NeuroVida.Games.Acoplamiento.DockingSprites.CellSilhouette());
        Dump("dock_fit", NeuroVida.Games.Acoplamiento.DockingSprites.FitIcon());
        Dump("dock_mirror", NeuroVida.Games.Acoplamiento.DockingSprites.MirrorIcon());
        {
            // Pieza de la maqueta: pentominó F (quiral).
            var f = new[] { new NeuroVida.Games.Acoplamiento.Cell(1, 0), new NeuroVida.Games.Acoplamiento.Cell(0, 1), new NeuroVida.Games.Acoplamiento.Cell(1, 1),
                new NeuroVida.Games.Acoplamiento.Cell(1, 2), new NeuroVida.Games.Acoplamiento.Cell(2, 2) };
            var L = NeuroVida.Games.Acoplamiento.DockingSprites.PieceLayer.Body;
            Dump("dock_piece", NeuroVida.Games.Acoplamiento.DockingSprites.PieceSprite(f, false, L, 288));
            Dump("dock_piece_m", NeuroVida.Games.Acoplamiento.DockingSprites.PieceSprite(f, true, L, 288));
            Dump("dock_piece_sil", NeuroVida.Games.Acoplamiento.DockingSprites.PieceSprite(f, false, NeuroVida.Games.Acoplamiento.DockingSprites.PieceLayer.Silhouette, 288));
            Dump("dock_piece_sil_m", NeuroVida.Games.Acoplamiento.DockingSprites.PieceSprite(f, true, NeuroVida.Games.Acoplamiento.DockingSprites.PieceLayer.Silhouette, 288));
            Dump("dock_piece_socket", NeuroVida.Games.Acoplamiento.DockingSprites.PieceSprite(f, false, NeuroVida.Games.Acoplamiento.DockingSprites.PieceLayer.Socket, 288));
        }
        {
            for (int c = 0; c < 8; c++)
            {
                Dump("traffic_port_" + c, NeuroVida.Games.Trafico.TrafficSprites.Port(c));
                Dump("traffic_pod_" + c, NeuroVida.Games.Trafico.TrafficSprites.Pod(c));
            }
            Dump("traffic_knob", NeuroVida.Games.Trafico.TrafficSprites.SwitchKnob());
            Dump("traffic_station", NeuroVida.Games.Trafico.TrafficSprites.Station());
            for (int f = 0; f < NeuroVida.Games.Bitacora.BitacoraContract.FindCount; f++)
                Dump("bit_find_" + f, NeuroVida.Games.Bitacora.BitacoraSprites.Find(f));
            Dump("bit_probe", NeuroVida.Games.Bitacora.BitacoraSprites.Probe());
            // Una misión real (la genera el contrato del juego) para la maqueta: planetas, ruta, hallazgos y cajón.
            {
                var m = NeuroVida.Games.Bitacora.BitacoraContract.Generate(20260928, 6);
                var ic = System.Globalization.CultureInfo.InvariantCulture;
                var sb = new System.Text.StringBuilder();
                for (int p = 0; p < m.Planets; p++) sb.AppendLine($"P {m.PlanetColors[p]} {m.PlanetX[p].ToString(ic)} {m.PlanetY[p].ToString(ic)}");
                sb.AppendLine("R " + string.Join(" ", m.Route));
                sb.AppendLine("F " + string.Join(" ", m.Finds));
                sb.AppendLine("D " + string.Join(" ", m.Drawer));
                System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "bit_mission.txt"), sb.ToString());
            }
            // Muestra de sonido de Tráfico Estelar (WAV): el motor de fondo con los efectos encima, como en una partida.
            TrafficSoundDemo(Path.Combine(dir, "trafico-sonidos.wav"));
            BitacoraSoundDemo(Path.Combine(dir, "bitacora-sonidos.wav"));
            // Rumbo a Casa: sprites, un viaje real del contrato (con una vuelta de ejemplo) y la muestra de sonido.
            Dump("homing_ship", NeuroVida.Games.Rumbo.HomingSprites.Ship());
            Dump("homing_ship_flat", NeuroVida.Games.Rumbo.HomingSprites.Ship(false));
            Dump("homing_base", NeuroVida.Games.Rumbo.HomingSprites.Base());
            Dump("homing_beacon", NeuroVida.Games.Rumbo.HomingSprites.Beacon());
            Dump("homing_arrow", NeuroVida.Games.Rumbo.HomingSprites.AimArrow());
            Dump("homing_dial", NeuroVida.Games.Rumbo.HomingSprites.Dial());
            Dump("homing_fog", NeuroVida.Games.Rumbo.HomingSprites.Fog());
            for (int c = 0; c < 5; c++) Dump("homing_crystal_" + c, NeuroVida.Games.Rumbo.HomingSprites.Crystal(c));
            {
                var ic = System.Globalization.CultureInfo.InvariantCulture;
                var sb = new System.Text.StringBuilder();
                foreach (var (level, seed, dTurn, dRatio) in new[] { (1, 4, 14f, 0.92f), (4, 9, -22f, 1.18f), (7, 2, 8f, 0.8f) })
                {
                    var trip = NeuroVida.Games.Rumbo.HomingContract.NextTrip(level, true, new System.Random(seed));
                    float turn = NeuroVida.Games.Rumbo.HomingContract.CorrectTurn(trip) + dTurn;
                    float traveled = trip.HomeDistance * dRatio;
                    var o = NeuroVida.Games.Rumbo.HomingContract.Evaluate(trip, turn, traveled);
                    NeuroVida.Games.Rumbo.HomingContract.Dir(trip.ArrivalHeading + turn, out float ux, out float uy);
                    sb.Append("T ").Append(level);
                    for (int i = 0; i < trip.Legs; i++) sb.Append(' ').Append(trip.CrystalX[i].ToString(ic)).Append(' ').Append(trip.CrystalY[i].ToString(ic));
                    sb.AppendLine();
                    sb.AppendLine("E " + (trip.StartX + ux * traveled).ToString(ic) + " " + (trip.StartY + uy * traveled).ToString(ic)
                        + " " + trip.ArrivalHeading.ToString(ic) + " " + turn.ToString(ic) + " " + o.AngleError.ToString(ic)
                        + " " + o.DistanceRatio.ToString(ic) + " " + o.ErrorRatio.ToString(ic) + " " + trip.BeaconBearing.ToString(ic));
                }
                System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "homing_trips.txt"), sb.ToString());
            }
            RumboSoundDemo(Path.Combine(dir, "rumbo-sonidos.wav"));
            // Constelación de Palabras: estrellas, micrófono, una ronda REAL (frases de la prueba de voz de Ricardo, 28-sep)
            // leída por el contrato del juego, y la muestra de sonido.
            for (int c = 0; c < 8; c++) Dump("const_star_" + c, NeuroVida.Games.Constelacion.ConstellationSprites.Star(c));
            Dump("const_star_neutral", NeuroVida.Games.Constelacion.ConstellationSprites.Neutral());
            Dump("const_mic", NeuroVida.Games.Constelacion.ConstellationSprites.Mic());
            {
                var cat = NeuroVida.Games.Constelacion.FluencyContract.Get("animales");
                var said = new System.Collections.Generic.List<NeuroVida.Games.Constelacion.FluencyWord>();
                foreach (var (t, text) in new[] { (12f, "perro gato oso koala León Puma tigre tigre de bengala"), (23f, "León del Atlas serpiente Cascabel boa boa constructor"),
                                                  (27f, "calamar"), (38f, "tiburón tiburón blanco tiburón ballena"), (46f, "ñandú emu"), (52f, "zorzal"), (57f, "loro") })
                    NeuroVida.Games.Constelacion.FluencyContract.Accept(cat, said, text, t);
                var valid = said.FindAll(w => !w.Repeat);
                var clusters = NeuroVida.Games.Constelacion.FluencyContract.Clusters(valid);
                var sb = new System.Text.StringBuilder();
                int ordinal = 0;
                foreach (var cl in clusters)
                {
                    int color = cl.Count >= 2 ? ordinal++ : -1;
                    for (int i = cl.Start; i < cl.Start + cl.Count; i++)
                        sb.AppendLine($"W|{valid[i].Display}|{color}|{i - cl.Start}|{(cl.Count >= 2 ? cl.Group : "")}");
                }
                sb.AppendLine($"S|{valid.Count}|{NeuroVida.Games.Constelacion.FluencyContract.MeanClusterSize(clusters).ToString(System.Globalization.CultureInfo.InvariantCulture)}|{NeuroVida.Games.Constelacion.FluencyContract.Switches(clusters)}|{string.Join(",", NeuroVida.Games.Constelacion.FluencyContract.Quarters(said))}");
                System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "const_round.txt"), sb.ToString());
            }
            ConstelacionSoundDemo(Path.Combine(dir, "constelacion-sonidos.wav"));
            // Redes de ejemplo para la maqueta: nodos ("N x y padre color") y recorridos ("P nodo x y x y ...").
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            foreach (var (ports, seed, twist) in new[] { (4, 3, 0.2f), (6, 11, 0.55f), (8, 5, 1f), (6, 21, 0.55f) })
            {
                var net = NeuroVida.Games.Trafico.TrafficContract.BuildNetwork(ports, new System.Random(seed), 825f / 500f, twist);
                var sb = new System.Text.StringBuilder();
                for (int n = 0; n < net.Count; n++)
                    sb.AppendLine(string.Join(" ", "N", net.X[n].ToString(inv), net.Y[n].ToString(inv), net.Parent[n], net.PortColor[n]));
                for (int n = 1; n < net.Count; n++)
                {
                    sb.Append("P ").Append(n);
                    for (int i = 0; i < net.PathX[n].Length; i++) sb.Append(' ').Append(net.PathX[n][i].ToString(inv)).Append(' ').Append(net.PathY[n][i].ToString(inv));
                    sb.AppendLine();
                }
                System.IO.File.WriteAllText(System.IO.Path.Combine(dir, $"traffic_net_{ports}_{seed}.txt"), sb.ToString());
            }
        }
        using (var f = File.CreateText(Path.Combine(dir, "palette.txt")))
            for (int i = 0; i < 16; i++)
            {
                var t = TilePalette.Get(i);
                f.WriteLine($"{t.NormalColor.r} {t.NormalColor.g} {t.NormalColor.b} {t.LightColor.r} {t.LightColor.g} {t.LightColor.b}");
            }
        Console.WriteLine("OK -> " + dir);
    }

    static void TrafficSoundDemo(string path)
    {
        const int rate = 44100;
        float seconds = 9.5f;
        var mix = new float[(int)(seconds * rate)];
        // Vuelo: entra al salir la primera cápsula, crece con más cápsulas (volumen) y se apaga al final.
        var engine = NeuroVida.Games.Trafico.TrafficSounds.EngineLoop().data;
        double pos = 0;
        for (int i = 0; i < mix.Length; i++)
        {
            float t = i / (float)rate;
            int flying = t < 0.5f ? 0 : t < 2.5f ? 1 : t < 4.5f ? 2 : t < 7f ? 4 : t < 8f ? 1 : 0;
            float vol = flying > 0 ? 0.12f + 0.025f * Math.Min(flying - 1, 6) : 0f;
            float pitch = 1f; // el juego no cambia el tono del vuelo (queda afinado con campanas y marimba)
            pos = (pos + pitch) % engine.Length;
            mix[i] += vol * engine[(int)pos];
        }
        void At(float t, UnityEngine.AudioClip c, float v)
        {
            int start = (int)(t * rate);
            for (int k = 0; k < c.data.Length && start + k < mix.Length; k++) mix[start + k] += v * c.data[k];
        }
        // Réplicas de los sonidos comunes de la app (GameFeel: "pling" de la racha, error suave, subir de nivel), que el
        // juego toca junto con los propios; acá solo para escuchar el conjunto.
        float[] penta = { 523.25f, 587.33f, 659.25f, 783.99f, 880f, 1046.5f, 1174.66f, 1318.51f };
        float Env(float x, float at, float tau) => x < 0 ? 0 : (x < at ? x / at : 1f) * (float)Math.Exp(-Math.Max(0, x - at) / tau);
        float Sn(float hz, float x) => (float)Math.Sin(2 * Math.PI * hz * x);
        void Synth(float t0, float seconds, Func<float, float> f, float v)
        {
            int start = (int)(t0 * rate);
            for (int k = 0; k < seconds * rate && start + k < mix.Length; k++) mix[start + k] += v * f(k / (float)rate);
        }
        void Pling(float t0, int streak) { float hz = penta[Math.Min(streak - 1, 7)]; Synth(t0, 0.26f, x => 0.8f * Sn(hz, x) * Env(x, 0.004f, 0.11f) + 0.22f * Sn(hz * 4, x) * Env(x, 0.002f, 0.025f), 0.55f); }
        void Wrong(float t0) => Synth(t0, 0.34f, x => { float hz = x < 0.11f ? 220f : 185f; float l = x < 0.11f ? x : x - 0.11f; return 0.7f * Env(l, 0.006f, 0.09f) * (x < 0.11f ? 0.75f : 1f) * (Sn(hz, x) + 0.18f * Sn(hz * 3, x)); }, 0.5f);
        void LevelUp(float t0) { float[] n = { 523.25f, 659.25f, 783.99f, 1046.5f }; Synth(t0, 0.5f, x => { float s2 = 0; for (int i = 0; i < 4; i++) { float st = i * 0.065f; if (x < st) break; s2 += 0.42f * Sn(n[i], x) * Env(x - st, 0.004f, 0.14f) + 0.1f * Sn(n[i] * 4, x) * Env(x - st, 0.002f, 0.03f); } return s2; }, 0.5f); }

        At(0.5f, NeuroVida.Games.Trafico.TrafficSounds.Launch(), 0.3f);
        At(1.2f, NeuroVida.Games.Trafico.TrafficSounds.Switch(true), 0.4f);
        At(1.8f, NeuroVida.Games.Trafico.TrafficSounds.PassChime(2), 0.2f);
        At(2.5f, NeuroVida.Games.Trafico.TrafficSounds.Launch(), 0.3f);
        At(2.9f, NeuroVida.Games.Trafico.TrafficSounds.PassChime(4), 0.2f);
        Pling(3.3f, 1); At(3.3f, NeuroVida.Games.Trafico.TrafficSounds.Landing(2), 0.3f);
        At(3.8f, NeuroVida.Games.Trafico.TrafficSounds.Switch(false), 0.4f);
        At(4.2f, NeuroVida.Games.Trafico.TrafficSounds.PassChime(0), 0.2f);
        At(4.5f, NeuroVida.Games.Trafico.TrafficSounds.UrgentLaunch(), 0.4f);
        Pling(4.9f, 2); At(4.9f, NeuroVida.Games.Trafico.TrafficSounds.Landing(4), 0.3f);
        At(5.4f, NeuroVida.Games.Trafico.TrafficSounds.PassChime(5), 0.2f);
        Pling(5.8f, 3); At(5.8f, NeuroVida.Games.Trafico.TrafficSounds.Landing(0), 0.3f);
        Wrong(6.4f);
        LevelUp(7.1f);
        At(8.0f, NeuroVida.Games.Trafico.TrafficSounds.Cascade(), 0.5f);
        using (var w = new BinaryWriter(File.Create(path)))
        {
            int n = mix.Length;
            w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + n * 2); w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
            w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(rate); w.Write(rate * 2); w.Write((short)2); w.Write((short)16);
            w.Write(System.Text.Encoding.ASCII.GetBytes("data")); w.Write(n * 2);
            foreach (float v in mix) w.Write((short)(Math.Max(-1f, Math.Min(1f, v)) * 32000));
        }
    }

    static void RumboSoundDemo(string path)
    {
        const int rate = 44100;
        var mix = new float[(int)(17f * rate)];
        void At(float t, UnityEngine.AudioClip c, float v)
        {
            int start = (int)(t * rate);
            for (int k = 0; k < c.data.Length && start + k < mix.Length; k++) mix[start + k] += v * c.data[k];
        }
        // Vuelo (el colchón de nave de la app) solo mientras la nave avanza.
        var engine = NeuroVida.Games.Trafico.TrafficSounds.EngineLoop().data;
        (float a, float b)[] flying = { (1.0f, 2.8f), (4.3f, 6.0f), (7.5f, 9.2f), (11.2f, 13.2f) };
        for (int i = 0; i < mix.Length; i++)
        {
            float t = i / (float)rate;
            float vol = 0f;
            foreach (var (a, b) in flying)
                vol = Math.Max(vol, 0.16f * Math.Clamp(Math.Min((t - a) / 0.3f, (b - t) / 0.3f), 0f, 1f));
            mix[i] += vol * engine[i % engine.Length];
        }
        // Ida: señal → (giro) → vuelo → cristal, tres veces (cada cristal una nota más alta).
        At(0.2f, NeuroVida.Games.Rumbo.HomingSounds.Ping(), 0.45f);
        At(2.8f, NeuroVida.Games.Rumbo.HomingSounds.Crystal(0), 0.55f);
        At(3.3f, NeuroVida.Games.Rumbo.HomingSounds.Ping(), 0.45f);
        At(3.7f, NeuroVida.Games.Rumbo.HomingSounds.Turn(), 0.35f);
        At(6.0f, NeuroVida.Games.Rumbo.HomingSounds.Crystal(1), 0.55f);
        At(6.5f, NeuroVida.Games.Rumbo.HomingSounds.Ping(), 0.45f);
        At(6.9f, NeuroVida.Games.Rumbo.HomingSounds.Turn(), 0.35f);
        At(9.2f, NeuroVida.Games.Rumbo.HomingSounds.Crystal(2), 0.55f);
        // Vuelta: rumbo fijado, giro, avance, ¡AQUÍ!, vista desde arriba y llegada perfecta ("vuelve a do").
        At(10.3f, NeuroVida.Games.Rumbo.HomingSounds.Lock(), 0.5f);
        At(10.5f, NeuroVida.Games.Rumbo.HomingSounds.Turn(), 0.35f);
        At(11.2f, NeuroVida.Games.Rumbo.HomingSounds.Turn(), 0.25f);
        At(13.4f, NeuroVida.Games.Rumbo.HomingSounds.MapReveal(), 0.5f);
        At(14.9f, NeuroVida.Games.Rumbo.HomingSounds.Arrival(0), 0.6f);
        using (var w = new BinaryWriter(File.Create(path)))
        {
            int n = mix.Length;
            w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + n * 2); w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
            w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(rate); w.Write(rate * 2); w.Write((short)2); w.Write((short)16);
            w.Write(System.Text.Encoding.ASCII.GetBytes("data")); w.Write(n * 2);
            foreach (float v in mix) w.Write((short)(Math.Max(-1f, Math.Min(1f, v)) * 32000));
        }
        // Las tres llegadas por separado, para comparar: justo en casa, cerca y lejos ("en suspenso").
        var arr = new float[(int)(6.6f * rate)];
        void AtA(float t, UnityEngine.AudioClip c, float v)
        {
            int start = (int)(t * rate);
            for (int k = 0; k < c.data.Length && start + k < arr.Length; k++) arr[start + k] += v * c.data[k];
        }
        AtA(0.2f, NeuroVida.Games.Rumbo.HomingSounds.Arrival(0), 0.6f);
        AtA(2.4f, NeuroVida.Games.Rumbo.HomingSounds.Arrival(1), 0.55f);
        AtA(4.6f, NeuroVida.Games.Rumbo.HomingSounds.Arrival(2), 0.5f);
        using (var w = new BinaryWriter(File.Create(path.Replace(".wav", "-llegadas.wav"))))
        {
            int n = arr.Length;
            w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + n * 2); w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
            w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(rate); w.Write(rate * 2); w.Write((short)2); w.Write((short)16);
            w.Write(System.Text.Encoding.ASCII.GetBytes("data")); w.Write(n * 2);
            foreach (float v in arr) w.Write((short)(Math.Max(-1f, Math.Min(1f, v)) * 32000));
        }
    }

    static void ConstelacionSoundDemo(string path)
    {
        const int rate = 44100;
        var mix = new float[(int)(12f * rate)];
        void At(float t, UnityEngine.AudioClip c, float v)
        {
            int start = (int)(t * rate);
            for (int k = 0; k < c.data.Length && start + k < mix.Length; k++) mix[start + k] += v * c.data[k];
        }
        // Micrófono encendido; una constelación de felinos (la melodía sube con cada palabra del grupo), un salto a los
        // reptiles (vuelve a la nota de partida), una palabra repetida, y el cielo de la ronda.
        At(0.2f, NeuroVida.Games.Constelacion.ConstellationSounds.Listen(), 0.45f);
        float t0 = 1.2f;
        for (int i = 0; i < 5; i++)
        {
            At(t0 + 0.55f * i, NeuroVida.Games.Constelacion.ConstellationSounds.Star(i), 0.4f);
            if (i == 1) At(t0 + 0.55f * i, NeuroVida.Games.Constelacion.ConstellationSounds.Formed(), 0.35f);
        }
        float t1 = 4.4f;
        for (int i = 0; i < 4; i++)
        {
            At(t1 + 0.6f * i, NeuroVida.Games.Constelacion.ConstellationSounds.Star(i), 0.4f);
            if (i == 1) At(t1 + 0.6f * i, NeuroVida.Games.Constelacion.ConstellationSounds.Formed(), 0.35f);
        }
        At(7.2f, NeuroVida.Games.Constelacion.ConstellationSounds.Repeat(), 0.35f);
        At(8.0f, NeuroVida.Games.Constelacion.ConstellationSounds.Stop(), 0.4f);
        At(8.9f, NeuroVida.Games.Constelacion.ConstellationSounds.RoundEnd(), 0.5f);
        using (var w = new BinaryWriter(File.Create(path)))
        {
            int n = mix.Length;
            w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + n * 2); w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
            w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(rate); w.Write(rate * 2); w.Write((short)2); w.Write((short)16);
            w.Write(System.Text.Encoding.ASCII.GetBytes("data")); w.Write(n * 2);
            foreach (float v in mix) w.Write((short)(Math.Max(-1f, Math.Min(1f, v)) * 32000));
        }
    }

    static void BitacoraSoundDemo(string path)
    {
        const int rate = 44100;
        var mix = new float[(int)(11f * rate)];
        void At(float t, UnityEngine.AudioClip c, float v)
        {
            int start = (int)(t * rate);
            for (int k = 0; k < c.data.Length && start + k < mix.Length; k++) mix[start + k] += v * c.data[k];
        }
        // Llega la transmisión; la sonda viaja a 4 planetas (cada hallazgo con su nota y destello; se guarda al tocarlo);
        // se abre el informe; lo recordado se archiva.
        At(0.2f, NeuroVida.Games.Bitacora.BitacoraSounds.Incoming(), 0.5f);
        for (int i = 0; i < 4; i++)
        {
            float t = 1.8f + 1.5f * i;
            At(t, NeuroVida.Games.Bitacora.BitacoraSounds.Travel(), 0.3f);
            At(t + 0.5f, NeuroVida.Games.Bitacora.BitacoraSounds.Reveal(i), 0.55f);
            At(t + 1.1f, NeuroVida.Games.Bitacora.BitacoraSounds.Save(), 0.45f);
        }
        At(8.0f, NeuroVida.Games.Bitacora.BitacoraSounds.Report(), 0.5f);
        At(9.4f, NeuroVida.Games.Bitacora.BitacoraSounds.Archive(), 0.55f);
        using (var w = new BinaryWriter(File.Create(path)))
        {
            int n = mix.Length;
            w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + n * 2); w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
            w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(rate); w.Write(rate * 2); w.Write((short)2); w.Write((short)16);
            w.Write(System.Text.Encoding.ASCII.GetBytes("data")); w.Write(n * 2);
            foreach (float v in mix) w.Write((short)(Math.Max(-1f, Math.Min(1f, v)) * 32000));
        }
    }
}
