using System;
using System.Collections.Generic;

namespace NeuroVida.Games.Satelites
{
    /// <summary>Las tres sorpresas de una ronda (se anuncian ANTES de la presentación, nunca durante el seguimiento).</summary>
    [Flags]
    public enum Surprise { None = 0, Orbit = 1, Cloud = 2, Fast = 4 }

    /// <summary>Lo que trae un nivel: cuántos se siguen (K) de cuántos hay (N), la velocidad media (dp/s sobre un campo de 360 dp), los segundos de seguimiento y qué sorpresas, sentidos de frente y media vuelta se suman.</summary>
    public readonly struct LevelSpec
    {
        public readonly int K, N;
        public readonly float Speed, TrackSeconds;
        public readonly Surprise Surprises;
        public readonly bool Head, Turn;

        public LevelSpec(int k, int n, float speed, float trackSeconds, Surprise surprises, bool head, bool turn)
        {
            K = k; N = n; Speed = speed; TrackSeconds = trackSeconds; Surprises = surprises; Head = head; Turn = turn;
        }

        /// <summary>El mismo nivel con el seguimiento más corto o largo (solo para el smoke del Editor, que juega rápido).</summary>
        public LevelSpec Scaled(float trackFactor) => new LevelSpec(K, N, Speed, TrackSeconds * trackFactor, Surprises, Head, Turn);

        /// <summary>El nivel con una sorpresa aplicada: «órbitas rápidas» = velocidad × 1,25 y seguimiento × 0,8.</summary>
        public LevelSpec With(Surprise surprise) =>
            surprise == Surprise.Fast
                ? new LevelSpec(K, N, Speed * SatelliteContract.FastSpeedFactor, TrackSeconds * SatelliteContract.FastTrackFactor, Surprises, Head, Turn)
                : this;
    }

    /// <summary>
    /// Los tres anillos elípticos de las órbitas, medidos sobre el campo. Planos, más altos que anchos: el más externo mide <c>campo/2 − (radio + paneles) − 8 dp</c> de ancho (contando el carril más ancho, +6 %), así que nunca se sale,
    /// y su alto es 1,42 × el ancho, ajustado a la altura que haya (en 20:9 se usa toda la pantalla). Sin dependencias de UnityEngine.
    /// </summary>
    public sealed class OrbitLayout
    {
        /// <summary>Los anillos son proporcionales a 80 / 110 / 140 (el boceto), del interno al externo.</summary>
        private static readonly float[] RingShare = { 80f / 140f, 110f / 140f, 1f };

        public readonly float Cx, Cy, FieldWidth, HalfHeight;
        public readonly float[] Rx = new float[3];
        /// <summary>Alto de un anillo = Rx × RyK (1,42 salvo que la pantalla sea más baja).</summary>
        public readonly float RyK;

        public OrbitLayout(float fieldWidth, float cx, float cy, float halfHeight)
        {
            Cx = cx; Cy = cy; FieldWidth = fieldWidth; HalfHeight = halfHeight;
            float outer = (fieldWidth * 0.5f - (SatelliteContract.SatRadius + SatelliteContract.PanelReach) - SatelliteContract.EdgeMargin) / (1f + SatelliteContract.LaneSpread);
            for (int i = 0; i < 3; i++) Rx[i] = outer * RingShare[i];
            // el alto: 1,42 × el ancho, o lo que quepa (los centros llegan hasta halfHeight del centro del planeta)
            float fit = halfHeight / (outer * (1f + SatelliteContract.LaneSpread));
            RyK = Math.Max(1f, Math.Min(SatelliteContract.RingHeightK, fit));
        }

        /// <summary>Hasta dónde puede llegar el centro de un satélite a la izquierda y a la derecha del centro del campo (deja los paneles y el margen al borde).</summary>
        public float MaxDx => FieldWidth * 0.5f - (SatelliteContract.SatRadius + SatelliteContract.PanelReach) - SatelliteContract.EdgeMargin;

        /// <summary>El radio horizontal en una posición de anillo (0 a 2; con decimales entre dos anillos, durante un salto).</summary>
        public float RadiusAt(float ringPos)
        {
            ringPos = Math.Max(0f, Math.Min(2f, ringPos));
            int lo = (int)Math.Floor(ringPos), hi = Math.Min(2, lo + 1);
            return Rx[lo] + (Rx[hi] - Rx[lo]) * (ringPos - lo);
        }

        /// <summary>El radio medio de un anillo (para pasar de dp/s a vueltas por segundo).</summary>
        public float MeanRadius(int ring) => (Rx[ring] + Rx[ring] * RyK) * 0.5f;
    }

    /// <summary>La nube de polvo: plana, opaca, cruza el campo de lado a lado y sale ANTES de que termine el seguimiento. Lo que pasa debajo no se ve.</summary>
    public sealed class DustCloud
    {
        public float X, Y;
        public readonly float Vx, Rx, Ry;

        public DustCloud(float x, float y, float vx, float rx, float ry) { X = x; Y = y; Vx = vx; Rx = rx; Ry = ry; }

        public bool Covers(float px, float py)
        {
            float dx = (px - X) / Rx, dy = (py - Y) / Ry;
            return dx * dx + dy * dy < 0.8f;
        }
    }

    /// <summary>Un salto de un satélite al anillo vecino durante el seguimiento (cambio de órbita).</summary>
    public sealed class OrbitJump
    {
        public int Sat, To, From = -1;
        public float At, Duration;
    }

    /// <summary>
    /// El movimiento de los satélites (lógica pura, reproducible con semilla): órbitas planas en tres anillos, cada satélite con su carril (radio ± 6 %) y su velocidad (media del nivel × 0,75 a 1,25, así se adelantan). Niveles 1-2: un
    /// sentido por anillo; desde el 3 se mezclan dentro del anillo y se cruzan de frente; desde el 6 cerca de un tercio da media vuelta (con un frenado suave de 0,5 s). Coordenadas en dp del campo, con el origen arriba a la izquierda.
    /// </summary>
    public sealed class OrbitSwarm
    {
        public readonly int Count, K;
        public readonly OrbitLayout Layout;
        public readonly float TrackSeconds;
        public readonly float[] X, Y, Theta, Omega, Lane, RingPos;
        public readonly int[] Ring;
        public readonly bool[] Target;
        public readonly List<OrbitJump> Jumps = new List<OrbitJump>();
        public DustCloud Cloud { get; private set; }
        /// <summary>Segundos de seguimiento que lleva.</summary>
        public float Time { get; private set; }

        private readonly float[] _turnAt, _omega0;      // media vuelta: cuándo empieza (−1 = no da) y la velocidad con que venía
        private readonly Random _rng;

        public OrbitSwarm(LevelSpec level, Surprise surprise, OrbitLayout layout, Random rng)
        {
            _rng = rng;
            Layout = layout;
            K = level.K;
            TrackSeconds = level.TrackSeconds;
            int n = level.N;
            Count = n;
            X = new float[n]; Y = new float[n]; Theta = new float[n]; Omega = new float[n]; Lane = new float[n]; RingPos = new float[n];
            Ring = new int[n];
            Target = new bool[n];
            _turnAt = new float[n]; _omega0 = new float[n];

            // reparto inicial: ningún par a menos de SpawnGap (si en una pantalla muy baja no cupieran, se baja la distancia de a poco, nunca de 30 dp)
            int placed = 0;
            for (float gap = SatelliteContract.SpawnGap; placed < n && gap >= 30f; gap -= 4f)
            {
                placed = 0;
                for (int tries = 0; placed < n && tries < 6000; tries++)
                {
                    int ring = _rng.Next(3);
                    float th = (float)(_rng.NextDouble() * Math.PI * 2.0);
                    float lane = 1f + ((float)_rng.NextDouble() - 0.5f) * 2f * SatelliteContract.LaneSpread;
                    Ring[placed] = ring; RingPos[placed] = ring; Theta[placed] = th; Lane[placed] = lane;
                    Place(placed);
                    bool free = true;
                    for (int j = 0; j < placed && free; j++) free = Dist(placed, j) >= gap;
                    if (free) placed++;
                }
            }
            if (placed < n) throw new InvalidOperationException("no cupieron los satélites en los anillos");

            // sentidos: por anillo (el interno y el medio al revés, el externo al azar) o mezclados si el nivel trae «de frente»
            int d0 = _rng.NextDouble() < 0.5 ? 1 : -1, d2 = _rng.NextDouble() < 0.5 ? 1 : -1;
            int[] ringDir = { d0, -d0, d2 };
            for (int i = 0; i < n; i++)
            {
                float speed = level.Speed * (0.75f + (float)_rng.NextDouble() * 0.5f);
                int dir = level.Head ? (_rng.NextDouble() < 0.5 ? 1 : -1) : ringDir[Ring[i]];
                Omega[i] = dir * speed / layout.MeanRadius(Ring[i]);
                _turnAt[i] = -1f;
                if (level.Turn && _rng.NextDouble() < 0.35)
                {
                    _turnAt[i] = 0.8f + (float)_rng.NextDouble() * Math.Max(0.1f, level.TrackSeconds - 1.8f);
                    _omega0[i] = Omega[i];
                }
            }

            // los que traen mensaje: K al azar
            var order = Shuffled(n);
            for (int i = 0; i < K && i < n; i++) Target[order[i]] = true;

            if (surprise == Surprise.Orbit) PlanJumps(level);
        }

        private int[] Shuffled(int n)
        {
            var a = new int[n];
            for (int i = 0; i < n; i++) a[i] = i;
            for (int i = n - 1; i > 0; i--) { int j = _rng.Next(i + 1); int t = a[i]; a[i] = a[j]; a[j] = t; }
            return a;
        }

        /// <summary>Cambio de órbita: de 2 a 4 satélites (al menos uno con mensaje) saltan al anillo vecino en 0,9 s, repartidos en el seguimiento.</summary>
        private void PlanJumps(LevelSpec level)
        {
            int m = Math.Min(Count, 2 + _rng.Next(3));
            var order = Shuffled(Count);
            var chosen = new List<int>();
            for (int i = 0; i < m; i++) chosen.Add(order[i]);
            int firstTarget = -1;
            for (int i = 0; i < Count && firstTarget < 0; i++) if (Target[i]) firstTarget = i;
            if (firstTarget >= 0 && !chosen.Contains(firstTarget)) chosen[0] = firstTarget;
            for (int j = 0; j < chosen.Count; j++)
            {
                int s = chosen[j];
                int to = Ring[s] == 0 ? 1 : Ring[s] == 2 ? 1 : (_rng.NextDouble() < 0.5 ? 0 : 2);
                float span = level.TrackSeconds - 1.8f;
                Jumps.Add(new OrbitJump { Sat = s, To = to, At = 0.7f + span * (j + (float)_rng.NextDouble() * 0.6f) / chosen.Count, Duration = SatelliteContract.JumpSeconds });
            }
        }

        /// <summary>La nube de polvo (solo en las rondas con esa sorpresa): sale de un lado, cruza el campo y sale por el otro ANTES del final.</summary>
        public void StartCloud(float fieldWidth)
        {
            int dir = _rng.NextDouble() < 0.5 ? 1 : -1;
            float span = fieldWidth + 220f, dur = Math.Max(1.6f, TrackSeconds - 0.9f);
            Cloud = new DustCloud(dir > 0 ? -110f : fieldWidth + 110f, Layout.Cy + ((float)_rng.NextDouble() - 0.5f) * 160f, dir * span / dur, 78f, 62f);
        }

        public void EndCloud() { Cloud = null; }

        private void Place(int i)
        {
            float rx = Layout.RadiusAt(RingPos[i]) * Lane[i];
            X[i] = Layout.Cx + rx * (float)Math.Cos(Theta[i]);
            Y[i] = Layout.Cy + rx * Layout.RyK * (float)Math.Sin(Theta[i]);
        }

        private float Dist(int a, int b)
        {
            float dx = X[a] - X[b], dy = Y[a] - Y[b];
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        private static float EaseInOut(float t) => t < 0.5f ? 2f * t * t : 1f - (float)Math.Pow(-2f * t + 2f, 2) / 2f;

        /// <summary>Avanza <paramref name="dt"/> segundos de seguimiento.</summary>
        public void Step(float dt)
        {
            if (dt <= 0f) return;
            Time += dt;
            for (int i = 0; i < Count; i++)
            {
                if (_turnAt[i] >= 0f)
                {
                    float k = (Time - _turnAt[i]) / SatelliteContract.TurnSeconds;
                    if (k >= 0f && k <= 1f) Omega[i] = _omega0[i] * (float)Math.Cos(Math.PI * k);       // frena suave y se da vuelta
                    else if (k > 1f) Omega[i] = -_omega0[i];
                }
                Theta[i] += Omega[i] * dt;
            }
            foreach (var j in Jumps)
            {
                float k = (Time - j.At) / j.Duration;
                if (k >= 0f && k <= 1f)
                {
                    if (j.From < 0) j.From = Ring[j.Sat];
                    RingPos[j.Sat] = j.From + (j.To - j.From) * EaseInOut(k);
                }
                else if (k > 1f && j.From >= 0 && Ring[j.Sat] != j.To) { Ring[j.Sat] = j.To; RingPos[j.Sat] = j.To; }
            }
            for (int i = 0; i < Count; i++) Place(i);
            if (Cloud != null) Cloud.X += Cloud.Vx * dt;
        }

        /// <summary>La menor distancia entre dos satélites ahora.</summary>
        public float MinGap()
        {
            float m = float.MaxValue;
            for (int i = 0; i < Count; i++)
                for (int j = i + 1; j < Count; j++) m = Math.Min(m, Dist(i, j));
            return m;
        }

        public bool CloudCoversAny()
        {
            if (Cloud == null) return false;
            for (int i = 0; i < Count; i++) if (Cloud.Covers(X[i], Y[i])) return true;
            return false;
        }

        /// <summary>¿Ya pueden detenerse? Cumplido el tiempo, SOLO si nadie está a menos de 38 dp de otro y ninguno está bajo la nube; si no, siguen hasta 0,9 s más.</summary>
        public bool ReadyToStop() =>
            Time >= TrackSeconds && ((MinGap() >= SatelliteContract.StopGap && !CloudCoversAny()) || Time >= TrackSeconds + SatelliteContract.MaxExtraTrackSeconds);

        // ------------------------------------------------------------------ el asentamiento al detenerse

        private float[] _fromX, _fromY, _toX, _toY;
        private float _settleT = -1f;

        /// <summary>true mientras los satélites se deslizan a su lugar final (<see cref="SettleStep"/>).</summary>
        public bool Settling => _settleT >= 0f && _settleT < 1f;

        /// <summary>
        /// Al detenerse, si todavía quedan dos satélites a menos de <see cref="SatelliteContract.SettleGap"/> (los anillos vecinos casi se tocan: 30 dp entre sí a los costados, y dos que giran juntos tardan varios segundos en soltarse, más de los 0,9 s de espera),
        /// calcula dónde quedan después de apartarse lo mínimo (≤ <see cref="SatelliteContract.SettleMax"/> dp cada uno; sin salirse del campo ni pisar el planeta). Devuelve true si alguno se mueve más de 1 dp. Es lo último que pasa antes de la respuesta.
        /// </summary>
        public bool PlanSettle()
        {
            _fromX = (float[])X.Clone(); _fromY = (float[])Y.Clone();
            _toX = (float[])X.Clone(); _toY = (float[])Y.Clone();
            float gap = SatelliteContract.SettleGap, maxMove = SatelliteContract.SettleMax;
            float minPlanet = SatelliteContract.PlanetRadius + SatelliteContract.SatRadius + 6f;
            for (int iter = 0; iter < 80; iter++)
            {
                bool moved = false;
                for (int i = 0; i < Count; i++)
                {
                    for (int j = i + 1; j < Count; j++)
                    {
                        float dx = _toX[i] - _toX[j], dy = _toY[i] - _toY[j];
                        float d = (float)Math.Sqrt(dx * dx + dy * dy);
                        if (d >= gap) continue;
                        if (d < 1e-3f) { float a = (i * 7 + j * 3) * 0.9f; dx = (float)Math.Cos(a); dy = (float)Math.Sin(a); d = 1f; }
                        float push = (gap - d) * 0.5f + 0.05f;
                        float ux = dx / d, uy = dy / d;
                        _toX[i] += ux * push; _toY[i] += uy * push;
                        _toX[j] -= ux * push; _toY[j] -= uy * push;
                        moved = true;
                    }
                    ClampSettle(i, minPlanet, maxMove);
                }
                if (!moved) break;
            }
            bool any = false;
            for (int i = 0; i < Count; i++)
            {
                float dx = _toX[i] - _fromX[i], dy = _toY[i] - _fromY[i];
                if (dx * dx + dy * dy > 1f) any = true;
            }
            _settleT = any ? 0f : 1f;
            return any;
        }

        private void ClampSettle(int i, float minPlanet, float maxMove)
        {
            // no se aleja más de maxMove de donde se detuvo
            float mx = _toX[i] - _fromX[i], my = _toY[i] - _fromY[i];
            float m = (float)Math.Sqrt(mx * mx + my * my);
            if (m > maxMove) { _toX[i] = _fromX[i] + mx / m * maxMove; _toY[i] = _fromY[i] + my / m * maxMove; }
            // dentro del campo
            _toX[i] = Math.Max(Layout.Cx - Layout.MaxDx, Math.Min(Layout.Cx + Layout.MaxDx, _toX[i]));
            _toY[i] = Math.Max(Layout.Cy - Layout.HalfHeight, Math.Min(Layout.Cy + Layout.HalfHeight, _toY[i]));
            // y fuera del planeta
            float px = _toX[i] - Layout.Cx, py = _toY[i] - Layout.Cy;
            float pd = (float)Math.Sqrt(px * px + py * py);
            if (pd < minPlanet && pd > 1e-3f) { _toX[i] = Layout.Cx + px / pd * minPlanet; _toY[i] = Layout.Cy + py / pd * minPlanet; }
        }

        /// <summary>Avanza el deslizamiento final (0,35 s, suave). Cuando termina, <see cref="Settling"/> da false y las posiciones son las finales.</summary>
        public void SettleStep(float dt)
        {
            if (_settleT < 0f || _settleT >= 1f) return;
            _settleT = Math.Min(1f, _settleT + dt / SatelliteContract.SettleSeconds);
            float e = EaseInOut(_settleT);
            for (int i = 0; i < Count; i++)
            {
                X[i] = _fromX[i] + (_toX[i] - _fromX[i]) * e;
                Y[i] = _fromY[i] + (_toY[i] - _fromY[i]) * e;
            }
        }

        /// <summary>Índice del satélite más cercano a (x, y) dentro de <paramref name="reach"/> (por defecto, el radio de toque de 30 dp); -1 si ninguno.</summary>
        public int Nearest(float x, float y, float reach = SatelliteContract.TouchRadius)
        {
            int best = -1;
            float bestD = reach * reach;
            for (int i = 0; i < Count; i++)
            {
                float dx = X[i] - x, dy = Y[i] - y, d2 = dx * dx + dy * dy;
                if (d2 < bestD) { bestD = d2; best = i; }
            }
            return best;
        }
    }
}
