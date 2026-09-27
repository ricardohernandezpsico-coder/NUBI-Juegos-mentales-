using System;
using System.Collections.Generic;

namespace NeuroVida.Games.Radar
{
    /// <summary>Un ensayo de Radar: qué se ve en el centro, dónde está el astronauta y dónde los asteroides.</summary>
    public sealed class RadarTrial
    {
        /// <summary>Lo que aparece en la pantalla central (índices de <c>ShapeKind</c> de Parejas y su variante).</summary>
        public int CenterShape, CenterVariant;
        /// <summary>La otra opción que se ofrece al responder (el "parecido").</summary>
        public int DecoyShape, DecoyVariant;
        /// <summary>Dirección del astronauta: 0 = arriba y en sentido horario (1 = arriba a la derecha ... 7).</summary>
        public int Direction;
        /// <summary>Anillo del astronauta: 0 = cerca del centro, 2 = borde del radar.</summary>
        public int Ring;
        /// <summary>Casillas con asteroides (casilla = anillo * 8 + dirección). Nunca incluye la del astronauta.</summary>
        public int[] Distractors;
        public int ExposureMs;
        public int Level;
    }

    /// <summary>
    /// Reglas puras de "Radar": entrenamiento de VELOCIDAD DE PROCESAMIENTO y campo visual útil, con la estructura
    /// de la tarea UFOV (Ball y Owsley) usada en el ensayo ACTIVE (Ball et al., JAMA 2002; Edwards et al., 2017):
    /// en un destello breve aparece algo en el centro y algo en la periferia; luego una "interferencia" (máscara) borra
    /// la imagen y hay que decir qué había en el centro y dónde estaba lo de afuera. La dificultad acorta el destello
    /// (500 → 40 ms), agrega asteroides que distraen, aleja al astronauta del centro y hace más parecidas las dos
    /// opciones del centro. Sin dependencias de UnityEngine: testeable con NUnit.
    /// </summary>
    public static class RadarContract
    {
        public const string GameId = "radar";
        public const int MaxLevel = 12;
        public const int Directions = 8;
        public const int Rings = 3;

        /// <summary>Reto: 90 s de radar.</summary>
        public const int RetoSeconds = 90;
        /// <summary>Precisión (sin reloj): cantidad de ensayos.</summary>
        public const int PrecisionTrials = 20;

        /// <summary>Radio de cada anillo como fracción del radio del radar.</summary>
        public static readonly float[] RingRadius = { 0.42f, 0.65f, 0.87f };

        /// <summary>Radio de la pantalla central (fracción del radio del radar): tocar adentro no elige dirección.</summary>
        public const float CenterWindow = 0.22f;

        // Íconos de ShapeKind (Parejas).
        private const int Planet = 0, Rocket = 1, Comet = 2, Star = 3, Moon = 4, Ufo = 5, Satellite = 6, Sun = 7, Telescope = 10;
        /// <summary>El astronauta a rescatar (casco) y los asteroides que distraen.</summary>
        public const int HelmetShape = 8, AsteroidShape = 9;

        /// <summary>
        /// Pares del centro por dificultad: (forma A, variante A, forma B, variante B). En cada ensayo se muestra uno y
        /// el otro queda como opción al responder.
        /// <list type="bullet">
        /// <item>0 fácil: formas y colores muy distintos.</item>
        /// <item>1 media: formas distintas del mismo color.</item>
        /// <item>2 difícil: siluetas parecidas del mismo color (estrella/sol, planeta/platillo...).</item>
        /// </list>
        /// </summary>
        public static readonly int[][][] CenterPairs =
        {
            new[]
            {
                new[] { Rocket, 0, Ufo, 0 },
                new[] { Planet, 1, Star, 2 },
                new[] { Moon, 0, Satellite, 1 },
                new[] { Comet, 0, Sun, 0 },
            },
            new[]
            {
                new[] { Rocket, 0, Planet, 2 },
                new[] { Star, 1, Comet, 0 },
                new[] { Moon, 1, Ufo, 2 },
                new[] { Sun, 1, Moon, 0 },
            },
            new[]
            {
                new[] { Star, 0, Sun, 1 },
                new[] { Planet, 2, Ufo, 1 },
                new[] { Comet, 0, Rocket, 1 },
                new[] { Satellite, 0, Telescope, 0 },
            },
        };

        // ------------------------------------------------------------------ dificultad

        /// <summary>Duración del destello (ms): 500 en el nivel 1 → 40 en el 12, en pasos proporcionales (~20% menos
        /// por nivel), como las escaleras de duración de la tarea UFOV.</summary>
        public static int ExposureMs(int level)
        {
            double k = (Clamp(level) - 1) / (double)(MaxLevel - 1);
            return (int)Math.Round(500.0 * Math.Pow(40.0 / 500.0, k));
        }

        /// <summary>Asteroides que distraen: ninguno hasta el nivel 3; luego llenan el anillo del astronauta (7), dos
        /// anillos (15) y todo el radar (23).</summary>
        public static int DistractorCount(int level)
        {
            int l = Clamp(level);
            return l <= 3 ? 0 : l <= 5 ? 7 : l <= 8 ? 15 : 23;
        }

        /// <summary>Dificultad de las dos opciones del centro (0 fácil, 1 media, 2 difícil).</summary>
        public static int CenterTier(int level)
        {
            int l = Clamp(level);
            return l <= 4 ? 0 : l <= 8 ? 1 : 2;
        }

        /// <summary>Anillos posibles para el astronauta: cerca al principio, cualquier distancia desde el nivel 6.</summary>
        public static int MaxRing(int level)
        {
            int l = Clamp(level);
            return l <= 2 ? 0 : l <= 5 ? 1 : 2;
        }

        public static RadarTrial NextTrial(int level, Random rng)
        {
            level = Clamp(level);
            var pairs = CenterPairs[CenterTier(level)];
            var pair = pairs[rng.Next(pairs.Length)];
            bool showA = rng.NextDouble() < 0.5;
            var t = new RadarTrial
            {
                CenterShape = showA ? pair[0] : pair[2],
                CenterVariant = showA ? pair[1] : pair[3],
                DecoyShape = showA ? pair[2] : pair[0],
                DecoyVariant = showA ? pair[3] : pair[1],
                Direction = rng.Next(Directions),
                Ring = rng.Next(MaxRing(level) + 1),
                ExposureMs = ExposureMs(level),
                Level = level,
            };
            t.Distractors = PickDistractors(DistractorCount(level), t.Ring, t.Direction, rng);
            return t;
        }

        /// <summary>Asteroides por anillos completos: primero el del astronauta, después uno vecino, después todos.</summary>
        private static int[] PickDistractors(int count, int ring, int direction, Random rng)
        {
            if (count <= 0) return new int[0];
            var rings = new List<int> { ring };
            if (count > 7)
            {
                int neighbor = ring == 0 ? 1 : ring == 2 ? 1 : (rng.NextDouble() < 0.5 ? 0 : 2);
                rings.Add(neighbor);
            }
            if (count > 15)
                for (int r = 0; r < Rings; r++)
                    if (!rings.Contains(r)) rings.Add(r);

            var slots = new List<int>();
            foreach (int r in rings)
                for (int d = 0; d < Directions; d++)
                    if (!(r == ring && d == direction)) slots.Add(r * Directions + d);
            return slots.ToArray();
        }

        // ------------------------------------------------------------------ geometría

        /// <summary>Posición de una casilla en coordenadas del radar (radio 1, y hacia arriba).</summary>
        public static void Position(int direction, int ring, out float x, out float y)
        {
            double a = direction * Math.PI / 4.0;
            float r = RingRadius[Math.Max(0, Math.Min(Rings - 1, ring))];
            x = (float)(r * Math.Sin(a));
            y = (float)(r * Math.Cos(a));
        }

        /// <summary>Dirección (0..7, 0 = arriba, sentido horario) más cercana a un toque en (x, y) respecto del centro.</summary>
        public static int DirectionFromOffset(float x, float y)
        {
            double a = Math.Atan2(x, y); // 0 arriba, positivo hacia la derecha
            int d = (int)Math.Round(a / (Math.PI / 4.0));
            return ((d % Directions) + Directions) % Directions;
        }

        /// <summary>¿El toque (en radios del radar) cuenta como respuesta de dirección? Fuera de la pantalla central y
        /// sin alejarse demasiado del radar.</summary>
        public static bool IsDirectionTap(float x, float y)
        {
            double r = Math.Sqrt(x * x + y * y);
            return r >= CenterWindow && r <= 1.25;
        }

        private static readonly string[] DirectionNames =
            { "arriba", "arriba a la derecha", "a la derecha", "abajo a la derecha", "abajo", "abajo a la izquierda", "a la izquierda", "arriba a la izquierda" };

        public static string DirectionName(int direction) => DirectionNames[((direction % Directions) + Directions) % Directions];

        // ------------------------------------------------------------------ puntaje y medidas

        /// <summary>Puntos del ensayo: completo (centro + dirección) con bono de nivel y racha; a medias, un poco.</summary>
        public static int Points(bool center, bool location, int level, int streak)
        {
            if (center && location) return 100 + 10 * (Clamp(level) - 1) + 20 * Math.Min(Math.Max(streak - 1, 0), 10);
            return center || location ? 25 : 0;
        }

        /// <summary>Ensayos iniciales que no cuentan para <see cref="GlanceMs"/> (la escalera todavía está buscando).</summary>
        public const int GlanceSkip = 4;
        /// <summary>Ensayos finales que se promedian.</summary>
        public const int GlanceWindow = 12;

        /// <summary>
        /// "Tu vistazo" (ms): la duración de destello en la que la escalera se asentó, o sea, la más breve con la que
        /// la persona acierta cerca de 8 de cada 10 (la escalera apunta ahí). Media geométrica de las duraciones reales
        /// de los últimos ensayos, sin los primeros. -1 si hay muy pocos ensayos.
        /// </summary>
        public static int GlanceMs(IReadOnlyList<float> exposuresMs)
        {
            if (exposuresMs == null || exposuresMs.Count < GlanceSkip + 4) return -1;
            int from = Math.Max(GlanceSkip, exposuresMs.Count - GlanceWindow);
            double logSum = 0;
            int n = 0;
            for (int i = from; i < exposuresMs.Count; i++)
            {
                logSum += Math.Log(Math.Max(1.0, exposuresMs[i]));
                n++;
            }
            return (int)Math.Round(Math.Exp(logSum / n));
        }

        /// <summary>Puntaje 0-100: aciertos completos (50%) y rapidez del vistazo (50%, escala logarítmica entre 500 y
        /// 40 ms). Sin vistazo medido, solo los aciertos.</summary>
        public static int Score(float accuracy, int glanceMs)
        {
            float acc = Math.Max(0f, Math.Min(1f, accuracy));
            if (glanceMs <= 0) return (int)Math.Round(acc * 100f);
            double speed = Math.Log(500.0 / Math.Max(1, glanceMs)) / Math.Log(500.0 / 40.0);
            speed = Math.Max(0.0, Math.Min(1.0, speed));
            return Math.Max(0, Math.Min(100, (int)Math.Round((0.5 * acc + 0.5 * speed) * 100.0)));
        }

        private static int Clamp(int level) => Math.Max(1, Math.Min(MaxLevel, level));
    }
}
