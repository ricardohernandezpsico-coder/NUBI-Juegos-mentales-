using System;
using System.Collections.Generic;

namespace NeuroVida.Games.Cosecha
{
    public enum PlantKind { Sprout, Flower, Tulip, Bush, Sunflower, Mushroom, GoldenFlower }

    /// <summary>Dónde crece una planta: posición respecto del centro del planeta (y hacia arriba), tamaño relativo al radio e inclinación.</summary>
    public struct PlantSpot
    {
        public float X, Y;
        /// <summary>Alto de la planta como fracción del radio del planeta.</summary>
        public float Size;
        public PlantKind Kind;
        /// <summary>Grados (antihorario): las plantas de los costados se inclinan hacia afuera, siguiendo la curva.</summary>
        public float Tilt;
    }

    /// <summary>
    /// Dónde brota cada planta del huerto (lógica pura, sin UnityEngine). Cada palabra sembrada suma una planta sobre la cara
    /// visible del planeta: el TAMAÑO sigue el largo (3 letras = brote u hongo chico … 6 = girasol o arbusto grande), una palabra
    /// rara da una flor DORADA y la palabra estrella el árbol dorado del centro. Las plantas se reparten sin encimarse (cada
    /// nueva elige, entre varios candidatos, el lugar más lejos de las que ya hay) y de atrás hacia adelante se ordenan por Y.
    /// </summary>
    public sealed class Garden
    {
        public const float TreeSize = 1.05f;
        private readonly Random _rng;
        private readonly List<PlantSpot> _spots = new List<PlantSpot>();

        public float Radius { get; }
        public IReadOnlyList<PlantSpot> Spots => _spots;
        public int Count => _spots.Count;

        public Garden(float radius, int seed)
        {
            Radius = radius;
            _rng = new Random(seed);
        }

        public static PlantKind KindFor(int length, bool rare, int index)
        {
            if (rare) return PlantKind.GoldenFlower;
            PlantKind[] options;
            switch (length)
            {
                case 3: options = new[] { PlantKind.Sprout, PlantKind.Mushroom }; break;
                case 4: options = new[] { PlantKind.Flower, PlantKind.Sprout, PlantKind.Mushroom }; break;
                case 5: options = new[] { PlantKind.Tulip, PlantKind.Bush, PlantKind.Flower }; break;
                default: options = new[] { PlantKind.Sunflower, PlantKind.Bush, PlantKind.Tulip }; break;
            }
            return options[Math.Abs(index) % options.Length];
        }

        /// <summary>Alto de la planta (fracción del radio): 3 letras 0,28 … 6 letras o más 0,62.</summary>
        public static float SizeFor(int length) => length <= 3 ? 0.28f : length == 4 ? 0.38f : length == 5 ? 0.5f : 0.62f;

        /// <summary>Distancia mínima entre dos plantas (fracción del radio); se achica al llenarse el huerto.</summary>
        public static float MinGap(int count) => count > 24 ? 0.13f : count > 16 ? 0.18f : 0.26f;

        /// <summary>Suma la planta de una palabra de [length] letras (rara = flor dorada) y devuelve su lugar. [forced] fija el tipo (los brotes del comienzo).</summary>
        public PlantSpot Add(int length, bool rare, PlantKind? forced = null)
        {
            var best = new PlantSpot();
            float bestScore = float.MinValue;
            for (int k = 0; k < 40; k++)
            {
                float ang = (float)(Math.PI / 180.0 * (14 + _rng.NextDouble() * 152));       // arco de arriba
                float rad = (float)(0.16 + _rng.NextDouble() * 0.7) * Radius;
                float x = rad * (float)Math.Cos(ang);
                float y = rad * (float)Math.Sin(ang) - (float)(_rng.NextDouble() * 0.34) * Radius;   // algo hacia adelante
                if (y < 0.12f * Radius || (float)Math.Sqrt(x * x + y * y) > 0.9f * Radius) continue;
                float near = float.MaxValue;
                foreach (var s in _spots)
                {
                    float dx = x - s.X, dy = (y - s.Y) * 1.4f;
                    near = Math.Min(near, (float)Math.Sqrt(dx * dx + dy * dy));
                }
                if (_spots.Count == 0) near = Radius;
                float score = near + (float)_rng.NextDouble() * 0.02f * Radius;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = new PlantSpot { X = x, Y = y };
                }
            }
            best.Kind = forced ?? KindFor(length, rare, _spots.Count);
            best.Size = SizeFor(length);
            best.Tilt = -best.X / Radius * 24f;
            _spots.Add(best);
            return best;
        }

        /// <summary>Posición del árbol dorado de la palabra estrella: arriba al centro del planeta.</summary>
        public PlantSpot TreeSpot() => new PlantSpot { X = 0f, Y = 0.8f * Radius, Size = TreeSize, Kind = PlantKind.GoldenFlower, Tilt = 0f };

        /// <summary>Distancia mínima entre dos plantas ya puestas (en unidades del radio, con el eje Y estirado como al elegir).</summary>
        public float MinDistance()
        {
            float min = float.MaxValue;
            for (int i = 0; i < _spots.Count; i++)
                for (int j = i + 1; j < _spots.Count; j++)
                {
                    float dx = _spots[i].X - _spots[j].X, dy = (_spots[i].Y - _spots[j].Y) * 1.4f;
                    min = Math.Min(min, (float)Math.Sqrt(dx * dx + dy * dy) / Radius);
                }
            return min;
        }
    }
}
