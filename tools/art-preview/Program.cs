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
            // Muestra de sonido de Tráfico Estelar (WAV): el motor de fondo con los efectos encima, como en una partida.
            TrafficSoundDemo(Path.Combine(dir, "trafico-sonidos.wav"));
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
        At(4.5f, NeuroVida.Games.Trafico.TrafficSounds.Launch(), 0.3f);
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
}
