using System;
using System.IO;
using NeuroVida.Games.Piloto;
using NeuroVida.Games.Parejas;
using NeuroVida.Games.Secuencia;
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

        // Engranajes «Taller de reparación»: todos los sprites (rectangulares: -ancho, alto) y las máquinas de muestra (etapas 3, 7 y 10) para componer la lámina (engranajes.py)
        {
            var E = typeof(NeuroVida.Games.Engranajes.EngranajesContract);
            void DumpTex(string name, Sprite sp)
            {
                int tw = sp.texture.width, th = sp.texture.pixels.Length / tw;
                DumpRect(name, sp.texture.pixels, tw, th);
            }
            foreach (NeuroVida.Games.Engranajes.EngranajesSprites.GearSize sz in Enum.GetValues(typeof(NeuroVida.Games.Engranajes.EngranajesSprites.GearSize)))
            {
                DumpTex("eng_sil_" + sz, NeuroVida.Games.Engranajes.EngranajesSprites.Silhouette(sz));
                foreach (NeuroVida.Games.Engranajes.EngranajesSprites.Palette pal in Enum.GetValues(typeof(NeuroVida.Games.Engranajes.EngranajesSprites.Palette)))
                    DumpTex("eng_gear_" + sz + "_" + pal, NeuroVida.Games.Engranajes.EngranajesSprites.Gear(sz, pal));
            }
            DumpTex("eng_room", NeuroVida.Games.Engranajes.EngranajesSprites.Room());
            DumpTex("eng_hull", NeuroVida.Games.Engranajes.EngranajesSprites.Hull());
            DumpTex("eng_dish", NeuroVida.Games.Engranajes.EngranajesSprites.Dish());
            DumpTex("eng_fan", NeuroVida.Games.Engranajes.EngranajesSprites.Fan());
            DumpTex("eng_door", NeuroVida.Games.Engranajes.EngranajesSprites.Door());
            DumpTex("eng_cframe", NeuroVida.Games.Engranajes.EngranajesSprites.CargoFrame());
            DumpTex("eng_cplate", NeuroVida.Games.Engranajes.EngranajesSprites.CargoPlate());
            DumpTex("eng_cbox", NeuroVida.Games.Engranajes.EngranajesSprites.CargoBox());
            DumpTex("eng_hub", NeuroVida.Games.Engranajes.EngranajesSprites.Hub());
            DumpTex("eng_pulley", NeuroVida.Games.Engranajes.EngranajesSprites.Pulley());
            DumpTex("eng_rack", NeuroVida.Games.Engranajes.EngranajesSprites.Rack());
            DumpTex("eng_wrench", NeuroVida.Games.Engranajes.EngranajesSprites.Wrench());
            DumpTex("eng_check", NeuroVida.Games.Engranajes.EngranajesSprites.Check());
            DumpTex("eng_mini_cw", NeuroVida.Games.Engranajes.EngranajesSprites.MiniArrow(true));
            DumpTex("eng_mini_ccw", NeuroVida.Games.Engranajes.EngranajesSprites.MiniArrow(false));
            foreach (var w in NeuroVida.Games.Engranajes.EngranajesSprites.BeltWidths)
                DumpTex("eng_cap_" + w.ToString(System.Globalization.CultureInfo.InvariantCulture), NeuroVida.Games.Engranajes.EngranajesSprites.BeltCap(w));
            foreach (NeuroVida.Games.Engranajes.EngranajesSprites.Glyph g in Enum.GetValues(typeof(NeuroVida.Games.Engranajes.EngranajesSprites.Glyph)))
                DumpTex("eng_glyph_" + g, NeuroVida.Games.Engranajes.EngranajesSprites.GlyphSprite(g));
            foreach (var r in new[] { NeuroVida.Games.Engranajes.EngranajesContract.BigTip + 8f, NeuroVida.Games.Engranajes.EngranajesContract.SmallTip + 8f })
            {
                DumpTex("eng_arrow_" + (int)r + "_cw", NeuroVida.Games.Engranajes.EngranajesSprites.Arrow(r, 4f, true));
                DumpTex("eng_arrow_" + (int)r + "_ccw", NeuroVida.Games.Engranajes.EngranajesSprites.Arrow(r, 4f, false));
            }
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            // (nombre, nivel, semilla, qué se hace: 0 = nada, 1 = un cambio equivocado, 2 = la solución guardada)
            foreach (var (name, level, seed, action) in new[] { ("l3", 3, 11, 0), ("l7", 7, 6, 1), ("l10", 10, 3, 2) })
            {
                var rng = new Random(seed);
                var m = NeuroVida.Games.Engranajes.EngranajesContract.Generate(level, rng);
                var changes = new System.Collections.Generic.List<int>();
                if (action == 2) changes.AddRange(m.Solution);
                else if (action == 1)
                {
                    foreach (var sw in m.Switches)
                    {
                        if (Array.IndexOf(m.Solution, sw.Id) >= 0) continue;
                        changes.Add(sw.Id);
                        break;
                    }
                }
                var sol = NeuroVida.Games.Engranajes.EngranajesContract.Solve(m, changes);
                var res = NeuroVida.Games.Engranajes.EngranajesContract.Results(m, changes);
                using var f = File.CreateText(Path.Combine(dir, "eng_machine_" + name + ".txt"));
                f.WriteLine("meta " + level + " " + m.MotorDir + " " + m.W + " " + m.MinK + " " + m.Stage.Keys + " " + (int)m.Stage.Hint);
                f.WriteLine("targets " + string.Join(" ", Array.ConvertAll(m.Targets, t => ((int)t).ToString())));
                foreach (var t in m.Targets) f.WriteLine("mission " + (int)t + " " + (int)m.Mission[t]);
                foreach (var r in res) f.WriteLine("result " + (int)r.Station + " " + (int)r.Got + " " + (r.Ok ? 1 : 0));
                f.WriteLine("changes " + string.Join(" ", changes));
                f.WriteLine("solution " + string.Join(" ", m.Solution));
                for (int i = 0; i < m.Gears.Count; i++)
                {
                    var g = m.Gears[i];
                    f.WriteLine(string.Join(" ", "gear", i, g.X.ToString(inv), g.Y.ToString(inv), g.Tip.ToString(inv), g.N, g.A.ToString(inv), (int)g.Role, g.HasStation ? (int)g.Station : -1, g.Pitch.ToString(inv), sol.Depth[i], sol.Speed[i].ToString(inv)));
                }
                for (int i = 0; i < m.Links.Count; i++) f.WriteLine("link " + i + " " + m.Links[i].A + " " + m.Links[i].B + " " + m.Links[i].Type + " " + (m.Links[i].Crossed ? 1 : 0));
                var hot = new System.Collections.Generic.HashSet<int>();
                foreach (int id in changes) hot.UnionWith(NeuroVida.Games.Engranajes.EngranajesContract.Downstream(m, id));
                f.WriteLine("downstream " + string.Join(" ", hot));
                f.WriteLine("fail " + NeuroVida.Games.Engranajes.EngranajesContract.FailText(res));
                f.WriteLine("hint " + NeuroVida.Games.Engranajes.EngranajesContract.HintText(m, res, changes));
                f.WriteLine("ok " + NeuroVida.Games.Engranajes.EngranajesContract.SuccessText(m, changes, 1));
                var cons = NeuroVida.Games.Engranajes.EngranajesContract.Consigna(m.Stage);
                f.WriteLine("consigna " + cons[0] + "|" + cons[1]);
            }
        }
        foreach (ShapeKind kind in Enum.GetValues(typeof(ShapeKind)))
            for (int v = 0; v < SymbolSprite.VariantCount; v++)
                Dump($"sym_{kind}_{v}", SymbolSprite.Get(kind, v));
        foreach (CardSprites.Face face in Enum.GetValues(typeof(CardSprites.Face)))
            Dump("card_" + face, CardSprites.Get(face));
        Dump("heart_full", HeartSprite.GetFull());
        Dump("heart_lost", HeartSprite.GetLost());
        // Rastro de luz: los 9 luceros de cristal, el disco y el aro punteados, la ✗, los 4 íconos de modo, Nubi maestra del tutorial y el tablero
        for (int i = 0; i < RastroBoard.Orbs; i++) Dump("rastro_orb_" + i, RastroSprites.Orb(i));
        Dump("rastro_disc", RastroSprites.DottedDisc());
        Dump("rastro_dashed", RastroSprites.DashedRing());
        Dump("rastro_vignette", RastroSprites.Vignette());
        Dump("rastro_x", RastroSprites.XMark());
        foreach (RastroMode m in RastroModes.All) Dump("rastro_icon_" + (int)m, RastroSprites.Icon(m));
        Dump("nubi_maestra", NubiTeacherSprite.Get());
        using (var f = File.CreateText(Path.Combine(dir, "rastro_board.txt")))
            for (int i = 0; i < RastroBoard.Orbs; i++)
            {
                var p = RastroBoard.Base(i);
                f.WriteLine(string.Join(" ", new[] { p.x.ToString(System.Globalization.CultureInfo.InvariantCulture), p.y.ToString(System.Globalization.CultureInfo.InvariantCulture), RastroBoard.Colors[i].ToString() }));
            }
        Dump("tile", TileSprites.Get());
        DumpRect("surface", LunarSurfaceSprite.Render(ClayRaster.Hex(0x9C94D6)), LunarSurfaceSprite.Width, LunarSurfaceSprite.Height);
        Dump("ship_0", PilotShipSprite.Get());
        Dump("screw", NeonSignSprites.Screw());
        Dump("mark_check", AnswerMarkSprite.Check());
        Dump("mark_cross", AnswerMarkSprite.Cross());
        Dump("radar_scope", NeuroVida.Games.Radar.RadarSprites.Scope());
        Dump("radar_sweep", NeuroVida.Games.Radar.RadarSprites.Sweep());
        Dump("radar_robot", NeuroVida.Games.Radar.RadarSprites.Robot());
        Dump("radar_beacon", NeuroVida.Games.Radar.RadarSprites.Beacon());
        Dump("radar_slot", NeuroVida.Games.Radar.RadarSprites.Slot());
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
                Dump("port_" + c, NeuroVida.Games.Shared.PortSprites.Port(c));
            }
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
            // Correo Estelar: sobre, paquete, radio y reloj (tapado y destapado).
            Dump("mail_envelope", NeuroVida.Games.Correo.MailSprites.Envelope());
            Dump("mail_package", NeuroVida.Games.Correo.MailSprites.Package());
            Dump("mail_radio", NeuroVida.Games.Correo.MailSprites.Radio());
            Dump("mail_clock", NeuroVida.Games.Correo.MailSprites.ClockFace());
            Dump("mail_clock_cover", NeuroVida.Games.Correo.MailSprites.ClockCover());
            Dump("mail_shield_full", NeuroVida.Games.Correo.MailSprites.ShieldPip(true));
            Dump("mail_shield_empty", NeuroVida.Games.Correo.MailSprites.ShieldPip(false));
            Dump("mail_damage1", NeuroVida.Games.Correo.MailSprites.ShipDamage(1));
            Dump("mail_damage2", NeuroVida.Games.Correo.MailSprites.ShipDamage(2));
            CorreoSoundDemo(Path.Combine(dir, "correo-sonidos.wav"));
        }
        using (var f = File.CreateText(Path.Combine(dir, "palette.txt")))
            for (int i = 0; i < 16; i++)
            {
                var t = TilePalette.Get(i);
                f.WriteLine($"{t.NormalColor.r} {t.NormalColor.g} {t.NormalColor.b} {t.LightColor.r} {t.LightColor.g} {t.LightColor.b}");
            }
        Console.WriteLine("OK -> " + dir);

        // Meteoros (opción B, 1-oct): la roca de arcilla con brasa, 4 formas x 4 colores (lila, celeste, coral, sol) + la de polvo.
        void DumpPx(string name, Color32[] px, int side)
        {
            using var file = File.Create(Path.Combine(dir, name + ".raw"));
            var bw = new BinaryWriter(file);
            bw.Write(side);
            foreach (var c in px) { bw.Write(c.r); bw.Write(c.g); bw.Write(c.b); bw.Write(c.a); }
        }
        for (int shape = 0; shape < 4; shape++)
        {
            for (int tint = 0; tint < 4; tint++)
                DumpPx($"rock_{shape}_{tint}", NeuroVida.Games.Meteoros.MeteorSprites.RenderRock(176, shape, (NeuroVida.Games.Meteoros.RockTint)tint), 176);
            DumpPx($"rock_{shape}_dust", NeuroVida.Games.Meteoros.MeteorSprites.RenderRock(176, shape, NeuroVida.Games.Meteoros.RockTint.Dust), 176);
        }
        DumpRect("heat_trail", NeuroVida.Games.Meteoros.MeteorSprites.RenderHeatTrail(96, 288, true), 96, 288);
        DumpRect("heat_trail_plain", NeuroVida.Games.Meteoros.MeteorSprites.RenderHeatTrail(96, 288, false), 96, 288);

        // ¿Verdad o disparate?: la antena y los íconos de los botones
        // Cosecha de palabras: el planeta-huerto, su pasto, las plantas, el árbol dorado y la ficha-luna
        Dump("cosecha_planet", NeuroVida.Games.Cosecha.CosechaSprites.Planet());
        Dump("cosecha_grass", NeuroVida.Games.Cosecha.CosechaSprites.GrassCap());
        Dump("cosecha_tufts", NeuroVida.Games.Cosecha.CosechaSprites.Tufts());
        Dump("cosecha_moon", NeuroVida.Games.Cosecha.CosechaSprites.Moon());
        Dump("cosecha_seed", NeuroVida.Games.Cosecha.CosechaSprites.Seed());
        Dump("cosecha_tree", NeuroVida.Games.Cosecha.CosechaSprites.GoldenTree());
        Dump("cosecha_seedicon", NeuroVida.Games.Cosecha.CosechaSprites.SeedIcon());
        Dump("cosecha_backicon", NeuroVida.Games.Cosecha.CosechaSprites.BackIcon());
        foreach (NeuroVida.Games.Cosecha.PlantKind pk in Enum.GetValues(typeof(NeuroVida.Games.Cosecha.PlantKind)))
            Dump("cosecha_plant_" + pk, NeuroVida.Games.Cosecha.CosechaSprites.Plant(pk));

        DumpPx("disparate_antenna", NeuroVida.Games.Disparate.DisparateSprites.RenderAntenna(288), 288);
        DumpPx("disparate_check", NeuroVida.Games.Disparate.DisparateSprites.RenderFlat(96, (x, y) => ClayRaster.Union(ClayRaster.Capsule(x, y, -0.55f, 0f, -0.18f, -0.4f, 0.17f), ClayRaster.Capsule(x, y, -0.18f, -0.4f, 0.6f, 0.46f, 0.17f)), new Color(0.1f, 0.07f, 0.25f, 1f)), 96);
    }

    static void CorreoSoundDemo(string path)
    {
        const int rate = 44100;
        var mix = new float[(int)(12.5f * rate)];
        void At(float t, UnityEngine.AudioClip c, float v)
        {
            int start = (int)(t * rate);
            for (int k = 0; k < c.data.Length && start + k < mix.Length; k++) mix[start + k] += v * c.data[k];
        }
        for (int i = 0; i < 4; i++) At(0.2f + 0.45f * i, NeuroVida.Games.Correo.MailSounds.Pickup(), 0.3f);
        At(1.95f, NeuroVida.Games.Correo.MailSounds.Bump(), 0.55f);
        At(2.2f, NeuroVida.Games.Correo.MailSounds.Deliver(), 0.55f);
        At(3.6f, NeuroVida.Games.Correo.MailSounds.WrongPlanet(), 0.5f);
        At(4.5f, NeuroVida.Games.Correo.MailSounds.Peek(), 0.45f);
        At(5.3f, NeuroVida.Games.Correo.MailSounds.RadioOk(), 0.55f);
        At(6.4f, NeuroVida.Games.Correo.MailSounds.RadioOff(), 0.45f);
        At(7.2f, NeuroVida.Games.Correo.MailSounds.Missed(), 0.45f);
        At(8.4f, NeuroVida.Games.Correo.MailSounds.Bump(), 0.55f);
        At(8.4f, NeuroVida.Games.Correo.MailSounds.ShieldCrack(), 0.4f);
        At(8.6f, NeuroVida.Games.Correo.MailSounds.Emergency(), 0.5f);
        At(10.6f, NeuroVida.Games.Correo.MailSounds.Repair(), 0.4f);
        using var file = File.Create(path);
        var w = new BinaryWriter(file);
        int n = mix.Length;
        w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + n * 2); w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
        w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(rate); w.Write(rate * 2); w.Write((short)2); w.Write((short)16);
        w.Write(System.Text.Encoding.ASCII.GetBytes("data")); w.Write(n * 2);
        for (int i = 0; i < n; i++) w.Write((short)(Math.Max(-1f, Math.Min(1f, mix[i])) * 32000));
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
        var engine = NeuroVida.Games.Shared.ShipSounds.EngineLoop().data;
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
