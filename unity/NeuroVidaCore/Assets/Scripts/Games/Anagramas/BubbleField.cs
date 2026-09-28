using System;

namespace NeuroVida.Games.Anagramas
{
    /// <summary>Cómo se mueven las burbujas en una palabra: diámetro, separación mínima y velocidad (en dp).</summary>
    public readonly struct BubbleSpec
    {
        public readonly float DiameterDp, GapDp, SpeedDp;

        public BubbleSpec(float diameterDp, float gapDp, float speedDp)
        {
            DiameterDp = diameterDp;
            GapDp = gapDp;
            SpeedDp = speedDp;
        }
    }

    /// <summary>
    /// Burbujas flotantes de Anagramas (28-sep, idea de Ricardo): en los niveles difíciles (5-7, palabras de 7 a 11
    /// letras) cada letra es una burbuja que rebota sin parar contra los bordes y contra las demás (choque elástico,
    /// nunca se tocan: siempre queda la separación mínima). Física pura, sin UnityEngine, con pruebas.
    /// Las coordenadas van en unidades de la UI, con el origen arriba a la izquierda del área de juego.
    /// </summary>
    public sealed class BubbleField
    {
        public const float MinDiameterDp = 48f;
        /// <summary>Por encima de esta ocupación del área las burbujas se achican (nunca bajo 48 dp).</summary>
        private const float MaxPacking = 0.34f;

        /// <summary>
        /// Tamaño y ritmo según la edad (mismo perfil que Parejas, <see cref="DdaUserProfileConfig"/>): en mayores,
        /// burbujas más grandes, más separadas y más lentas. El movimiento entra de a poco: nivel 5 al 60% de la
        /// velocidad, 6 al 80%, 7 completo. Niveles 1-4: null (fichas quietas en fila, como siempre).
        /// </summary>
        public static BubbleSpec? SpecFor(int level, AgeBand age)
        {
            if (level < 5) return null;
            float pace = level == 5 ? 0.6f : level == 6 ? 0.8f : 1f;
            switch (age)
            {
                case AgeBand.Senior: return new BubbleSpec(64f, 12f, 42f * pace);
                case AgeBand.Under18: return new BubbleSpec(56f, 8f, 80f * pace);
                default: return new BubbleSpec(56f, 8f, 70f * pace);
            }
        }

        public readonly float Width, Height, Radius, Gap, Speed;
        public readonly float[] X, Y, Vx, Vy;
        public readonly bool[] Active;
        public int Count => X.Length;

        /// <param name="radius">Radio deseado; se achica si no caben cómodas (hasta <paramref name="minRadius"/>).</param>
        public BubbleField(int n, float width, float height, float radius, float minRadius, float gap, float speed, Random rng)
        {
            Width = width;
            Height = height;
            Gap = gap;
            Speed = speed;
            Radius = FitRadius(n, width, height, radius, minRadius, gap);
            X = new float[n]; Y = new float[n]; Vx = new float[n]; Vy = new float[n];
            Active = new bool[n];
            for (int i = 0; i < n; i++)
            {
                PlaceFree(i, rng);
                Active[i] = true;
                SetRandomVelocity(i, rng);
            }
            Relax();
        }

        /// <summary>El radio más grande (hasta el deseado) con el que n burbujas ocupan como mucho ~1/3 del área.</summary>
        public static float FitRadius(int n, float width, float height, float radius, float minRadius, float gap)
        {
            float r = radius;
            while (r > minRadius && n * Math.PI * (r + gap / 2f) * (r + gap / 2f) > MaxPacking * width * height) r -= 1f;
            return Math.Max(r, minRadius);
        }

        /// <summary>Avanza el tiempo: se mueven, rebotan en los bordes y chocan entre ellas. Cada una mantiene su
        /// rapidez (el choque solo cambia la dirección): nunca se detienen.</summary>
        public void Step(float dt)
        {
            if (dt <= 0f) return;
            int sub = Math.Max(1, Math.Min(12, (int)Math.Ceiling(dt * Speed / (Radius * 0.2f))));
            float h = dt / sub;
            for (int s = 0; s < sub; s++)
            {
                for (int i = 0; i < Count; i++)
                {
                    if (!Active[i]) continue;
                    X[i] += Vx[i] * h;
                    Y[i] += Vy[i] * h;
                }
                for (int pass = 0; pass < 3; pass++)
                {
                    Walls();
                    Collide();
                }
                Walls();
            }
            for (int i = 0; i < Count; i++) if (Active[i]) KeepSpeed(i);
        }

        /// <summary>La burbuja se fue a su casilla: deja de moverse y de chocar.</summary>
        public void Deactivate(int i) => Active[i] = false;

        /// <summary>La letra vuelve al área: aparece en un lugar libre con una dirección al azar.</summary>
        public void Reactivate(int i, Random rng)
        {
            PlaceFree(i, rng);
            Active[i] = true;
            SetRandomVelocity(i, rng);
            Relax();
        }

        /// <summary>La menor distancia libre entre bordes de dos burbujas activas (para las pruebas).</summary>
        public float MinClearance()
        {
            float min = float.MaxValue;
            for (int i = 0; i < Count; i++)
            {
                if (!Active[i]) continue;
                for (int j = i + 1; j < Count; j++)
                {
                    if (!Active[j]) continue;
                    float d = (float)Math.Sqrt((X[i] - X[j]) * (X[i] - X[j]) + (Y[i] - Y[j]) * (Y[i] - Y[j]));
                    min = Math.Min(min, d - 2f * Radius);
                }
            }
            return min;
        }

        // ------------------------------------------------------------------ interno

        private float Lo => Radius + Gap / 2f;
        private float HiX => Width - Radius - Gap / 2f;
        private float HiY => Height - Radius - Gap / 2f;

        private void Walls()
        {
            for (int i = 0; i < Count; i++)
            {
                if (!Active[i]) continue;
                if (X[i] < Lo) { X[i] = Lo; Vx[i] = Math.Abs(Vx[i]); }
                if (X[i] > HiX) { X[i] = HiX; Vx[i] = -Math.Abs(Vx[i]); }
                if (Y[i] < Lo) { Y[i] = Lo; Vy[i] = Math.Abs(Vy[i]); }
                if (Y[i] > HiY) { Y[i] = HiY; Vy[i] = -Math.Abs(Vy[i]); }
            }
        }

        // Choque elástico entre iguales: se intercambian la componente de la velocidad a lo largo de la línea que
        // une los centros, y se separan hasta la distancia mínima (2 radios + separación).
        private void Collide()
        {
            float minD = 2f * Radius + Gap;
            for (int i = 0; i < Count; i++)
            {
                if (!Active[i]) continue;
                for (int j = i + 1; j < Count; j++)
                {
                    if (!Active[j]) continue;
                    float dx = X[j] - X[i], dy = Y[j] - Y[i];
                    float d2 = dx * dx + dy * dy;
                    if (d2 >= minD * minD) continue;
                    float d = (float)Math.Sqrt(d2);
                    float nx, ny;
                    if (d < 1e-4f) { nx = 1f; ny = 0f; d = 0f; }
                    else { nx = dx / d; ny = dy / d; }
                    float push = (minD - d) / 2f;
                    X[i] -= nx * push; Y[i] -= ny * push;
                    X[j] += nx * push; Y[j] += ny * push;
                    float vi = Vx[i] * nx + Vy[i] * ny, vj = Vx[j] * nx + Vy[j] * ny;
                    if (vi - vj > 0f)
                    {
                        Vx[i] += (vj - vi) * nx; Vy[i] += (vj - vi) * ny;
                        Vx[j] += (vi - vj) * nx; Vy[j] += (vi - vj) * ny;
                    }
                }
            }
        }

        // Si al repartirlas quedaron dos muy juntas (área llena), se separan antes de moverse: solo posiciones.
        private void Relax()
        {
            float minD = 2f * Radius + Gap;
            for (int it = 0; it < 300; it++)
            {
                bool moved = false;
                for (int i = 0; i < Count; i++)
                {
                    if (!Active[i]) continue;
                    for (int j = i + 1; j < Count; j++)
                    {
                        if (!Active[j]) continue;
                        float dx = X[j] - X[i], dy = Y[j] - Y[i];
                        float d = (float)Math.Sqrt(dx * dx + dy * dy);
                        if (d >= minD) continue;
                        float nx = d < 1e-4f ? 1f : dx / d, ny = d < 1e-4f ? 0f : dy / d;
                        float push = (minD - d) / 2f + 0.01f;
                        X[i] -= nx * push; Y[i] -= ny * push;
                        X[j] += nx * push; Y[j] += ny * push;
                        moved = true;
                    }
                }
                for (int i = 0; i < Count; i++)
                {
                    if (!Active[i]) continue;
                    X[i] = Math.Max(Lo, Math.Min(HiX, X[i]));
                    Y[i] = Math.Max(Lo, Math.Min(HiY, Y[i]));
                }
                if (!moved) return;
            }
        }

        private void KeepSpeed(int i)
        {
            float v = (float)Math.Sqrt(Vx[i] * Vx[i] + Vy[i] * Vy[i]);
            if (v < 1e-3f) { Vx[i] = Speed; Vy[i] = 0f; return; }
            Vx[i] *= Speed / v;
            Vy[i] *= Speed / v;
        }

        private void SetRandomVelocity(int i, Random rng)
        {
            double a = rng.NextDouble() * Math.PI * 2.0;
            Vx[i] = (float)Math.Cos(a) * Speed;
            Vy[i] = (float)Math.Sin(a) * Speed;
        }

        // Un lugar donde no toque a ninguna activa (al azar; si el área está muy llena, el lugar más holgado).
        private void PlaceFree(int i, Random rng)
        {
            float bestX = Width / 2f, bestY = Height / 2f, bestClear = float.MinValue;
            float minD = 2f * Radius + Gap;
            for (int attempt = 0; attempt < 1500; attempt++)
            {
                float x = Lo + (float)rng.NextDouble() * Math.Max(0f, HiX - Lo);
                float y = Lo + (float)rng.NextDouble() * Math.Max(0f, HiY - Lo);
                float clear = float.MaxValue;
                for (int j = 0; j < Count; j++)
                {
                    if (j == i || !Active[j]) continue;
                    float d = (float)Math.Sqrt((x - X[j]) * (x - X[j]) + (y - Y[j]) * (y - Y[j]));
                    clear = Math.Min(clear, d - minD);
                }
                if (clear >= 0f) { X[i] = x; Y[i] = y; return; }
                if (clear > bestClear) { bestClear = clear; bestX = x; bestY = y; }
            }
            X[i] = bestX; Y[i] = bestY;
        }
    }
}
