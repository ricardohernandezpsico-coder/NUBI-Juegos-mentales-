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
        using (var f = File.CreateText(Path.Combine(dir, "palette.txt")))
            for (int i = 0; i < 16; i++)
            {
                var t = TilePalette.Get(i);
                f.WriteLine($"{t.NormalColor.r} {t.NormalColor.g} {t.NormalColor.b} {t.LightColor.r} {t.LightColor.g} {t.LightColor.b}");
            }
        Console.WriteLine("OK -> " + dir);
    }
}
