using System;
using System.Collections.Generic;

namespace NeuroVida.Games.Rumbo
{
    /// <summary>
    /// Un viaje de ida: la base está en (0, 0) y la nave sale mirando hacia arriba (rumbo 0). <see cref="CrystalX"/> /
    /// <see cref="CrystalY"/> son las paradas en orden; la última es donde empieza la vuelta.
    /// </summary>
    public sealed class HomingTrip
    {
        public int Level;
        public float[] CrystalX, CrystalY;
        /// <summary>Hay faro en este viaje: un planeta lejanísimo (en el infinito) que solo cambia de lugar al girar.</summary>
        public bool Beacon;
        /// <summary>Rumbo del faro (grados, sentido horario desde arriba del mapa).</summary>
        public float BeaconBearing;

        public int Legs => CrystalX.Length;
        public float StartX => CrystalX[Legs - 1];
        public float StartY => CrystalY[Legs - 1];

        /// <summary>Rumbo con el que la nave llega a la última parada (el del último tramo).</summary>
        public float ArrivalHeading
        {
            get
            {
                float px = Legs >= 2 ? CrystalX[Legs - 2] : 0f, py = Legs >= 2 ? CrystalY[Legs - 2] : 0f;
                return HomingContract.HeadingOf(StartX - px, StartY - py);
            }
        }

        /// <summary>Distancia en línea recta desde la última parada hasta la base.</summary>
        public float HomeDistance => (float)Math.Sqrt(StartX * StartX + StartY * StartY);

        /// <summary>Largo del recorrido de ida (suma de los tramos).</summary>
        public float PathLength
        {
            get
            {
                float s = 0f, px = 0f, py = 0f;
                for (int i = 0; i < Legs; i++)
                {
                    float dx = CrystalX[i] - px, dy = CrystalY[i] - py;
                    s += (float)Math.Sqrt(dx * dx + dy * dy);
                    px = CrystalX[i];
                    py = CrystalY[i];
                }
                return s;
            }
        }
    }

    /// <summary>
    /// Resultado de una vuelta. <see cref="Along"/> y <see cref="Lateral"/> son dónde quedó la nave medido en el marco
    /// de la vuelta correcta, en fracciones de la distancia a casa: la base está en (Along = 1, Lateral = 0); el punto de
    /// partida de la vuelta, en (0, 0). Lateral positivo = a la derecha de la línea a casa.
    /// </summary>
    public struct HomingOutcome
    {
        /// <summary>Error de rumbo, en grados, con signo (+ = a la derecha de casa).</summary>
        public float AngleError;
        /// <summary>Recorrido de vuelta / distancia real a casa (1 = justo; menos = se quedó corto).</summary>
        public float DistanceRatio;
        /// <summary>Distancia final a la base / distancia a casa (0 = justo en la base).</summary>
        public float ErrorRatio;
        public float Along, Lateral;
        public bool Hit, Perfect;
    }

    /// <summary>
    /// Reglas puras de "Rumbo a Casa": INTEGRACIÓN DE TRAYECTO (volver al punto de partida sin verlo, solo con los giros
    /// y las distancias recorridas), la tarea de completar el triángulo de Klatzky et al. (1990) y Loomis et al. (1993),
    /// que depende de las células de red de la corteza entorrinal (Hafting et al., 2005) y se usa en los estudios de
    /// orientación en realidad virtual (Howett et al., 2019; Sea Hero Quest, Coutrot et al., 2018).
    /// <para>La vista es de cabina: la nave queda fija en el centro mirando hacia arriba y el espacio gira y pasa a su
    /// alrededor. La ida recorre 2 a 5 tramos entre cristales; la base queda en la niebla. En la vuelta se apunta hacia
    /// casa (rumbo) y se avanza hasta donde se cree que está (distancia): así el error se separa en sus dos partes.</para>
    /// Coordenadas: x a la derecha, y hacia arriba del mapa; rumbos en grados en sentido horario desde "arriba".
    /// Sin dependencias de UnityEngine: testeable con NUnit.
    /// </summary>
    public static class HomingContract
    {
        public const string GameId = "rumbo";
        public const int MaxLevel = 10;

        /// <summary>Reto: 150 s (cada viaje dura ~25 s; se termina el viaje en curso).</summary>
        public const int RetoSeconds = 150;
        /// <summary>Precisión (sin reloj): cantidad de viajes (mitad con faro, mitad sin).</summary>
        public const int PrecisionTrips = 8;

        /// <summary>Velocidad de la nave (unidades del mapa por segundo), igual en la ida y en la vuelta y en todos los
        /// niveles: así el paso del polvo de estrellas siempre "mide" lo mismo.</summary>
        public const float Speed = 300f;
        /// <summary>Niebla: se ve todo hasta esta distancia de la nave…</summary>
        public const float FogClear = 380f;
        /// <summary>…y desde esta ya no se ve nada.</summary>
        public const float FogGone = 520f;
        /// <summary>Después de la primera parada, la ruta nunca vuelve a pasar tan cerca de la base (no se la vuelve a ver).</summary>
        public const float HomeClearance = FogGone + 80f;
        /// <summary>La vuelta nunca es más corta que esto (la base siempre queda en la niebla al empezar).</summary>
        public const float MinHomeDistance = 820f;
        public const float MaxHomeDistance = 2600f;
        /// <summary>Si en la vuelta se pasa de largo tanto (× distancia a casa), se acaba el combustible y se detiene.</summary>
        public const float MaxReturnFactor = 2.2f;

        /// <summary>Buena llegada: la nave queda a menos de esta fracción de la distancia que había hasta casa.</summary>
        public const float HitRatio = 0.35f;
        /// <summary>"¡Llegada perfecta!".</summary>
        public const float PerfectRatio = 0.12f;

        /// <summary>Tramos del viaje de ida (incluido el primero, que sale derecho desde la base): 2 → 5.</summary>
        public static int Legs(int level)
        {
            int l = Clamp(level);
            return l <= 2 ? 2 : l <= 5 ? 3 : l <= 8 ? 4 : 5;
        }

        /// <summary>Giro en cada parada (grados, sin signo): primero giros medianos y parecidos; después, abanico amplio.</summary>
        public static void TurnRange(int level, out float min, out float max)
        {
            switch (Clamp(level))
            {
                case 1: min = 60f; max = 110f; break;
                case 2: min = 40f; max = 135f; break;
                case 3: min = 60f; max = 110f; break;
                case 4: min = 45f; max = 135f; break;
                case 5: min = 30f; max = 150f; break;
                case 6: min = 55f; max = 120f; break;
                case 7: min = 40f; max = 140f; break;
                case 8: min = 30f; max = 150f; break;
                case 9: min = 40f; max = 140f; break;
                default: min = 30f; max = 150f; break;
            }
        }

        /// <summary>Largo de cada tramo: parejos al principio, cada vez más distintos entre sí.</summary>
        public static void LegRange(int level, out float min, out float max)
        {
            float k = (Clamp(level) - 1f) / (MaxLevel - 1f);
            min = 800f - 180f * k;
            max = 1000f + 300f * k;
        }

        /// <summary>Velocidad de giro de la nave (grados por segundo): 80 → 150.</summary>
        public static float TurnRate(int level) => 80f + 70f * (Clamp(level) - 1f) / (MaxLevel - 1f);

        /// <summary>Qué trae cada nivel (para el aviso al subir).</summary>
        public static string LevelNews(int level)
        {
            switch (Clamp(level))
            {
                case 2: return "Giros más variados";
                case 3: return "Viajes de 3 tramos";
                case 4: return "Giros más abiertos";
                case 5: return "Giros de todo tipo";
                case 6: return "Viajes de 4 tramos";
                case 7: return "Tramos más desparejos";
                case 8: return "Giros más rápidos";
                case 9: return "Viajes de 5 tramos";
                case 10: return "Ruta de exploración completa";
                default: return "";
            }
        }

        // ------------------------------------------------------------------ geometría

        /// <summary>Rumbo (grados, sentido horario desde arriba) de un desplazamiento (dx, dy).</summary>
        public static float HeadingOf(float dx, float dy) => Wrap((float)(Math.Atan2(dx, dy) * 180.0 / Math.PI));

        /// <summary>Lleva un ángulo a (-180, 180].</summary>
        public static float Wrap(float deg)
        {
            deg %= 360f;
            if (deg > 180f) deg -= 360f;
            if (deg <= -180f) deg += 360f;
            return deg;
        }

        /// <summary>Unidad en la dirección del rumbo <paramref name="headingDeg"/>.</summary>
        public static void Dir(float headingDeg, out float x, out float y)
        {
            double r = headingDeg * Math.PI / 180.0;
            x = (float)Math.Sin(r);
            y = (float)Math.Cos(r);
        }

        /// <summary>Distancia del punto (px, py) al segmento (ax, ay)-(bx, by).</summary>
        public static float SegmentDistance(float px, float py, float ax, float ay, float bx, float by)
        {
            float vx = bx - ax, vy = by - ay;
            float len2 = vx * vx + vy * vy;
            float t = len2 <= 0f ? 0f : Math.Max(0f, Math.Min(1f, ((px - ax) * vx + (py - ay) * vy) / len2));
            float dx = ax + vx * t - px, dy = ay + vy * t - py;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        // ------------------------------------------------------------------ viajes

        /// <summary>
        /// Arma un viaje del nivel. El primer tramo sale derecho desde la base; en cada parada se gira (a la derecha o a
        /// la izquierda, al azar) dentro de <see cref="TurnRange"/>. Se acepta si la vuelta queda entre
        /// <see cref="MinHomeDistance"/> y <see cref="MaxHomeDistance"/> y si, después de la primera parada, la ruta no
        /// vuelve a acercarse a la base (se vería entre la niebla y la vuelta sería regalada).
        /// </summary>
        public static HomingTrip NextTrip(int level, bool beacon, Random rng)
        {
            int l = Clamp(level);
            int legs = Legs(l);
            TurnRange(l, out float tMin, out float tMax);
            LegRange(l, out float lMin, out float lMax);
            var xs = new float[legs];
            var ys = new float[legs];
            for (int attempt = 0; attempt < 2000; attempt++)
            {
                // Pasados muchos intentos, todos los giros para el mismo lado (siempre hay solución así).
                bool sameSide = attempt > 1500;
                int side = rng.NextDouble() < 0.5 ? -1 : 1;
                float heading = 0f, x = 0f, y = 0f;
                for (int i = 0; i < legs; i++)
                {
                    if (i > 0)
                    {
                        float turn = tMin + (float)rng.NextDouble() * (tMax - tMin);
                        int s = sameSide ? side : rng.NextDouble() < 0.5 ? -1 : 1;
                        heading = Wrap(heading + s * turn);
                    }
                    float len = lMin + (float)rng.NextDouble() * (lMax - lMin);
                    Dir(heading, out float dx, out float dy);
                    x += dx * len;
                    y += dy * len;
                    xs[i] = x;
                    ys[i] = y;
                }
                if (!Acceptable(xs, ys)) continue;
                return new HomingTrip
                {
                    Level = l,
                    CrystalX = (float[])xs.Clone(),
                    CrystalY = (float[])ys.Clone(),
                    Beacon = beacon,
                    BeaconBearing = (float)(rng.NextDouble() * 360.0 - 180.0),
                };
            }
            // Último recurso (no debería pasar): un triángulo simple.
            return new HomingTrip
            {
                Level = l, CrystalX = new[] { 0f, 900f }, CrystalY = new[] { 900f, 900f }, Beacon = beacon, BeaconBearing = 60f,
            };
        }

        private static bool Acceptable(float[] xs, float[] ys)
        {
            int n = xs.Length;
            float d = (float)Math.Sqrt(xs[n - 1] * xs[n - 1] + ys[n - 1] * ys[n - 1]);
            if (d < MinHomeDistance || d > MaxHomeDistance) return false;
            // Después de la primera parada, la ruta no vuelve cerca de la base.
            for (int i = 1; i < n; i++)
                if (SegmentDistance(0f, 0f, xs[i - 1], ys[i - 1], xs[i], ys[i]) < HomeClearance) return false;
            // Las paradas no se amontonan (cada cristal, lejos de los otros).
            for (int i = 0; i < n; i++)
                for (int j = i + 1; j < n; j++)
                {
                    float dx = xs[i] - xs[j], dy = ys[i] - ys[j];
                    if (dx * dx + dy * dy < 500f * 500f) return false;
                }
            // Un tramo no cruza otro tramo anterior (un viaje que se cruza a sí mismo confunde el dibujo del final).
            for (int i = 2; i < n; i++)
                for (int j = 0; j < i - 1; j++)
                {
                    float ax = j == 0 ? 0f : xs[j - 1], ay = j == 0 ? 0f : ys[j - 1];
                    if (Cross(ax, ay, xs[j], ys[j], xs[i - 1], ys[i - 1], xs[i], ys[i])) return false;
                }
            return true;
        }

        private static bool Cross(float ax, float ay, float bx, float by, float cx, float cy, float dx, float dy)
        {
            float d1 = Orient(cx, cy, dx, dy, ax, ay), d2 = Orient(cx, cy, dx, dy, bx, by);
            float d3 = Orient(ax, ay, bx, by, cx, cy), d4 = Orient(ax, ay, bx, by, dx, dy);
            return d1 * d2 < 0f && d3 * d4 < 0f;
        }

        private static float Orient(float ax, float ay, float bx, float by, float cx, float cy) =>
            (bx - ax) * (cy - ay) - (by - ay) * (cx - ax);

        /// <summary>Faro sí/no en el viaje <paramref name="index"/>: de a pares, uno con faro y otro sin, en orden al azar
        /// dentro de cada par (misma cantidad de cada uno, sin patrón fijo).</summary>
        public static bool BeaconFor(int index, Random rng, ref bool pairFirst)
        {
            if (index % 2 == 0)
            {
                pairFirst = rng.NextDouble() < 0.5;
                return pairFirst;
            }
            return !pairFirst;
        }

        // ------------------------------------------------------------------ la vuelta

        /// <summary>
        /// Evalúa una vuelta: desde la última parada, con el rumbo de llegada <see cref="HomingTrip.ArrivalHeading"/>, la
        /// persona giró <paramref name="turnDeg"/> (con signo, + = a la derecha) y avanzó <paramref name="traveled"/>.
        /// </summary>
        public static HomingOutcome Evaluate(HomingTrip trip, float turnDeg, float traveled)
        {
            float sx = trip.StartX, sy = trip.StartY;
            float vx = -sx, vy = -sy;                      // hacia la base
            float d = (float)Math.Sqrt(vx * vx + vy * vy);
            float chosen = Wrap(trip.ArrivalHeading + turnDeg);
            Dir(chosen, out float ux, out float uy);
            float ex = sx + ux * traveled, ey = sy + uy * traveled;
            float err = (float)Math.Sqrt(ex * ex + ey * ey);

            // Marco de la vuelta correcta: "adelante" hacia la base, "derecha" girando 90° en sentido horario.
            float fx = vx / d, fy = vy / d, rx = fy, ry = -fx;
            float mx = ex - sx, my = ey - sy;
            var o = new HomingOutcome
            {
                AngleError = Wrap(chosen - HeadingOf(vx, vy)),
                DistanceRatio = traveled / d,
                ErrorRatio = err / d,
                Along = (mx * fx + my * fy) / d,
                Lateral = (mx * rx + my * ry) / d,
            };
            o.Hit = o.ErrorRatio <= HitRatio;
            o.Perfect = o.ErrorRatio <= PerfectRatio;
            return o;
        }

        /// <summary>Giro correcto desde la última parada (con signo, + = a la derecha): hacia dónde estaba la base.</summary>
        public static float CorrectTurn(HomingTrip trip) => Wrap(HeadingOf(-trip.StartX, -trip.StartY) - trip.ArrivalHeading);

        // ------------------------------------------------------------------ puntaje y medidas

        /// <summary>Puntos: más cuanto más cerca de la base (0 desde el 60% de error), bono de llegada perfecta, nivel y racha.</summary>
        public static int Points(HomingOutcome o, int level, int streak)
        {
            float closeness = Math.Max(0f, 1f - o.ErrorRatio / 0.6f);
            int pts = (int)Math.Round(150f * closeness);
            if (o.Perfect) pts += 100;
            if (o.Hit) pts += 15 * (Clamp(level) - 1) + 20 * Math.Min(Math.Max(streak - 1, 0), 10);
            return pts;
        }

        /// <summary>"Tu brújula interna": a qué distancia de casa quedaste, en % de la distancia que había. -1 sin viajes.</summary>
        public static float MeanErrorPct(IReadOnlyList<HomingOutcome> trips)
        {
            if (trips == null || trips.Count == 0) return -1f;
            double s = 0;
            foreach (var o in trips) s += o.ErrorRatio;
            return (float)(s / trips.Count * 100.0);
        }

        /// <summary>Desvío medio del rumbo (grados, sin signo). -1 sin viajes.</summary>
        public static float MeanAbsAngle(IReadOnlyList<HomingOutcome> trips, int beacon = -1, IReadOnlyList<bool> beacons = null)
        {
            if (trips == null) return -1f;
            double s = 0;
            int n = 0;
            for (int i = 0; i < trips.Count; i++)
            {
                if (beacon >= 0 && (beacons == null || beacons[i] != (beacon == 1))) continue;
                s += Math.Abs(trips[i].AngleError);
                n++;
            }
            return n == 0 ? -1f : (float)(s / n);
        }

        /// <summary>Puntaje 0-100: cercanía a casa (70%; 0% de error = 1, 60% = 0) y nivel más alto alcanzado (30%).</summary>
        public static int Score(float meanErrorPct, int peakLevel)
        {
            float acc = meanErrorPct < 0f ? 0f : Math.Max(0f, Math.Min(1f, 1f - meanErrorPct / 60f));
            float lv = (float)(Clamp(peakLevel) - 1) / (MaxLevel - 1);
            return Math.Max(0, Math.Min(100, (int)Math.Round((0.7f * acc + 0.3f * lv) * 100f)));
        }

        private static int Clamp(int level) => Math.Max(1, Math.Min(MaxLevel, level));
    }
}
