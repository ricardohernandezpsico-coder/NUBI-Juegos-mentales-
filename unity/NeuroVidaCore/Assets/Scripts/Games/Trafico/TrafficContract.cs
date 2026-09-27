using System;
using System.Collections.Generic;

namespace NeuroVida.Games.Trafico
{
    /// <summary>
    /// La red de rutas: un árbol que nace en el portal (nodo 0), se abre en desvíos (dos salidas cada uno) y termina en
    /// los puertos (hojas). Coordenadas normalizadas del campo: x 0..1 (izquierda a derecha), y 0..1 (arriba a abajo).
    /// Cada tramo es un RECORRIDO (curva muestreada), no una recta: <see cref="PathX"/>/<see cref="PathY"/> del nodo n
    /// van desde su padre hasta n.
    /// </summary>
    public sealed class TrafficNetwork
    {
        public readonly List<float> X = new List<float>();
        public readonly List<float> Y = new List<float>();
        /// <summary>Hijos de cada nodo: 0 (puerto), 1 (el portal) o 2 (desvío).</summary>
        public readonly List<int[]> Children = new List<int[]>();
        public readonly List<int> Parent = new List<int>();
        /// <summary>Puerto → color (0..ports-1); -1 si no es puerto.</summary>
        public readonly List<int> PortColor = new List<int>();

        /// <summary>Alto / ancho del campo con el que se armaron las rutas (para medir largos como se ven).</summary>
        public float Aspect = TrafficContract.DefaultAspect;

        /// <summary>Recorrido del tramo que llega a cada nodo (del padre al nodo), en coordenadas normalizadas; null en el portal.</summary>
        public readonly List<float[]> PathX = new List<float[]>();
        public readonly List<float[]> PathY = new List<float[]>();
        /// <summary>Largo acumulado de cada recorrido, en alturas de campo.</summary>
        private readonly List<float[]> _cum = new List<float[]>();

        public int Count => X.Count;
        public bool IsPort(int n) => Children[n].Length == 0;
        public bool IsSwitch(int n) => Children[n].Length == 2;

        public IEnumerable<int> Switches()
        {
            for (int i = 0; i < Count; i++) if (IsSwitch(i)) yield return i;
        }

        public int PortOfColor(int color)
        {
            for (int i = 0; i < Count; i++) if (PortColor[i] == color) return i;
            return -1;
        }

        internal int Add(float x, float y, int parent)
        {
            X.Add(x);
            Y.Add(y);
            Children.Add(new int[0]);
            Parent.Add(parent);
            PortColor.Add(-1);
            return Count - 1;
        }

        /// <summary>Completa los recorridos que falten con rectas (redes armadas a mano, o recién armadas).</summary>
        public void EnsurePaths(bool resetAll = false)
        {
            while (PathX.Count < Count) { PathX.Add(null); PathY.Add(null); _cum.Add(null); }
            for (int n = 0; n < Count; n++)
            {
                int p = Parent[n];
                if (p < 0) { PathX[n] = PathY[n] = null; _cum[n] = null; continue; }
                if (!resetAll && PathX[n] != null) continue;
                SetPath(n, new[] { X[p], X[n] }, new[] { Y[p], Y[n] });
            }
        }

        public void SetPath(int node, float[] xs, float[] ys)
        {
            while (PathX.Count < Count) { PathX.Add(null); PathY.Add(null); _cum.Add(null); }
            PathX[node] = xs;
            PathY[node] = ys;
            var cum = new float[xs.Length];
            for (int i = 1; i < xs.Length; i++)
            {
                float dx = (xs[i] - xs[i - 1]) / Aspect, dy = ys[i] - ys[i - 1];
                cum[i] = cum[i - 1] + (float)Math.Sqrt(dx * dx + dy * dy);
            }
            _cum[node] = cum;
        }

        /// <summary>Largo del recorrido que llega a <paramref name="node"/>, en alturas de campo.</summary>
        public float PathLength(int node)
        {
            var c = _cum[node];
            return c[c.Length - 1];
        }

        /// <summary>Largo medio del viaje completo, de la estación a cada puerto (alturas de campo).</summary>
        public float MeanRouteLength()
        {
            EnsurePaths();
            float sum = 0f;
            int ports = 0;
            for (int n = 0; n < Count; n++)
            {
                if (!IsPort(n)) continue;
                ports++;
                for (int k = n; Parent[k] >= 0; k = Parent[k]) sum += PathLength(k);
            }
            return ports > 0 ? sum / ports : 0f;
        }

        /// <summary>Largo del tramo de <paramref name="from"/> a su hijo <paramref name="to"/> (alturas de campo).</summary>
        public float EdgeLength(int from, int to) => PathLength(to);

        /// <summary>Punto del recorrido que llega a <paramref name="node"/> a una distancia (alturas de campo) desde su inicio.</summary>
        public void PointAt(int node, float distance, out float x, out float y)
        {
            var c = _cum[node];
            var xs = PathX[node];
            var ys = PathY[node];
            float d = Math.Max(0f, Math.Min(c[c.Length - 1], distance));
            int i = 1;
            while (i < c.Length - 1 && c[i] < d) i++;
            float seg = Math.Max(1e-6f, c[i] - c[i - 1]);
            float t = Math.Max(0f, Math.Min(1f, (d - c[i - 1]) / seg));
            x = xs[i - 1] + (xs[i] - xs[i - 1]) * t;
            y = ys[i - 1] + (ys[i] - ys[i - 1]) * t;
        }
    }

    /// <summary>Una cápsula en viaje: va de <see cref="From"/> a <see cref="To"/> y lleva recorrido <see cref="Progress"/> del tramo.</summary>
    public sealed class Pod
    {
        public int Id, Color, From, To;
        /// <summary>SpawnTime: desde cuándo la persona la tiene a la vista (al salir, o antes si se anunció en "próximas").</summary>
        public float Progress, SpawnTime;
        /// <summary>Cápsula urgente: va más rápido y vale el doble.</summary>
        public bool Urgent;
        public float SpeedFactor = 1f;
        public bool Done;
    }

    /// <summary>Algo que pasó en un paso de la simulación (para que el juego lo muestre y lo cuente).</summary>
    public readonly struct TrafficEvent
    {
        public readonly int PodId, Node, Color;
        public readonly bool Arrived, Correct;
        /// <summary>Solo en pasos por un desvío que la persona movió para esta cápsula: cuánto antes (s). -1 si no.</summary>
        public readonly float Lead;

        public TrafficEvent(int podId, int node, int color, bool arrived, bool correct, float lead)
        {
            PodId = podId;
            Node = node;
            Color = color;
            Arrived = arrived;
            Correct = correct;
            Lead = lead;
        }
    }

    /// <summary>
    /// Simulación pura del tráfico: las cápsulas avanzan a velocidad constante por los tramos; al llegar a un desvío
    /// toman la salida que esté activa EN ESE MOMENTO; al llegar a un puerto, llegan bien si el color coincide.
    /// </summary>
    public sealed class TrafficSim
    {
        public readonly TrafficNetwork Net;
        public readonly List<Pod> Pods = new List<Pod>();
        /// <summary>Salida activa de cada desvío (0 o 1).</summary>
        public readonly Dictionary<int, int> SwitchState = new Dictionary<int, int>();
        private readonly Dictionary<int, float> _lastToggle = new Dictionary<int, float>();
        private int _nextId;
        public float Time { get; private set; }
        public float Speed = 0.2f;

        public TrafficSim(TrafficNetwork net)
        {
            Net = net;
            net.EnsurePaths();
            foreach (int s in net.Switches())
            {
                SwitchState[s] = 0;
                _lastToggle[s] = float.NegativeInfinity;
            }
        }

        public void Toggle(int sw)
        {
            if (!SwitchState.ContainsKey(sw)) return;
            SwitchState[sw] = 1 - SwitchState[sw];
            _lastToggle[sw] = Time;
        }

        /// <param name="announcedAt">Desde cuándo se veía en "próximas" (tiempo de esta simulación); si no, desde que sale.
        /// Mover un desvío después de eso cuenta como prepararlo para ella.</param>
        public Pod Spawn(int color, bool urgent = false, float announcedAt = float.NaN)
        {
            var p = new Pod
            {
                Id = _nextId++, Color = color, From = 0, To = Net.Children[0][0], Progress = 0f,
                SpawnTime = float.IsNaN(announcedAt) ? Time : Math.Min(Time, announcedAt),
                Urgent = urgent, SpeedFactor = urgent ? TrafficContract.UrgentSpeedFactor : 1f
            };
            Pods.Add(p);
            return p;
        }

        /// <summary>Cuántas cápsulas están en viaje ahora.</summary>
        public int InFlight
        {
            get
            {
                int n = 0;
                foreach (var p in Pods) if (!p.Done) n++;
                return n;
            }
        }

        public void Step(float dt, List<TrafficEvent> events)
        {
            Time += dt;
            foreach (var p in Pods)
            {
                if (p.Done) continue;
                float remaining = Speed * p.SpeedFactor * dt;
                while (remaining > 0f && !p.Done)
                {
                    float len = Math.Max(1e-4f, Net.EdgeLength(p.From, p.To));
                    float left = (1f - p.Progress) * len;
                    if (remaining < left)
                    {
                        p.Progress += remaining / len;
                        remaining = 0f;
                        break;
                    }
                    remaining -= left;
                    int node = p.To;
                    if (Net.IsPort(node))
                    {
                        p.Done = true;
                        p.Progress = 1f;
                        events?.Add(new TrafficEvent(p.Id, node, p.Color, true, Net.PortColor[node] == p.Color, -1f));
                        break;
                    }
                    int choice = SwitchState.TryGetValue(node, out int st) ? st : 0;
                    int next = Net.Children[node][Net.Children[node].Length == 2 ? choice : 0];
                    if (Net.IsSwitch(node))
                    {
                        bool good = Leads(next, p.Color);
                        float lead = _lastToggle[node] >= p.SpawnTime ? Time - _lastToggle[node] : -1f;
                        events?.Add(new TrafficEvent(p.Id, node, p.Color, false, good, good ? lead : -1f));
                    }
                    p.From = node;
                    p.To = next;
                    p.Progress = 0f;
                }
            }
        }

        /// <summary>¿Desde este nodo se puede llegar al puerto de ese color?</summary>
        public bool Leads(int node, int color)
        {
            if (Net.IsPort(node)) return Net.PortColor[node] == color;
            foreach (int c in Net.Children[node]) if (Leads(c, color)) return true;
            return false;
        }

        /// <summary>Posición actual de una cápsula (normalizada).</summary>
        public void Position(Pod p, out float x, out float y) => Net.PointAt(p.To, p.Progress * Net.PathLength(p.To), out x, out y);
    }

    /// <summary>
    /// Reglas puras de "Tráfico Estelar": ruteo de cápsulas con desvíos, una tarea de atención dividida y
    /// planificación bajo presión de tiempo, en la línea de la tarea de control de tráfico aéreo de Kanfer y Ackerman
    /// (1989). La medida propia distingue control PROACTIVO (preparar el desvío con tiempo) de REACTIVO (a último
    /// momento), según el modelo de mecanismos duales de control de Braver (2012).
    /// Sin dependencias de UnityEngine: testeable con NUnit.
    /// </summary>
    public static class TrafficContract
    {
        public const string GameId = "trafico";
        public const int MaxLevel = 12;
        public const int MaxPorts = 8;

        /// <summary>Reto: 2 minutos.</summary>
        public const int RetoSeconds = 120;
        /// <summary>Precisión (sin reloj): cantidad de cápsulas.</summary>
        public const int PrecisionPods = 30;
        /// <summary>Entregas seguidas sin error que dan el bono de "serie perfecta".</summary>
        public const int SeriesSize = 10;
        /// <summary>Cuántas cápsulas se ven esperando en "próximas", en la estación.</summary>
        public const int QueueSize = 3;
        /// <summary>Cápsula urgente: más rápida (×1,35) y vale el doble; desde el nivel 5.</summary>
        public const float UrgentSpeedFactor = 1.35f;
        public const int UrgentFromLevel = 5;
        /// <summary>Separación mínima entre salidas (s): que dos cápsulas nunca se monten en la ruta de la estación.</summary>
        public const float MinSpawnGap = 0.9f;

        /// <summary>Anticipación desde la que un desvío cuenta como "preparado con tiempo" (s).</summary>
        public const float ProactiveLead = 1.0f;
        /// <summary>Tope de anticipación que se registra (s).</summary>
        public const float MaxLead = 4f;

        /// <summary>Punto de partida (el portal) y distancia mínima entre nodos, en anchos de campo (para que puertos y
        /// desvíos no se monten y se puedan tocar).</summary>
        public const float SourceX = 0.5f, SourceY = 0.08f, MinNodeGap = 0.14f;

        /// <summary>Distancia mínima de un nodo a una ruta que no es suya (que ningún tramo pase rozando un desvío o
        /// un planeta ajeno: confundiría por dónde va).</summary>
        public const float MinRailGap = 0.08f;

        /// <summary>Proporción alto/ancho típica del campo en un teléfono vertical.</summary>
        public const float DefaultAspect = 1.6f;

        private static readonly int[] PortsByLevel = { 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8 };

        public static int Ports(int level) => PortsByLevel[Clamp(level) - 1];

        /// <summary>
        /// Velocidad (alturas de campo por segundo): 0,09 → 0,18 (Precisión: 20% menos). Lento a propósito: lo que se
        /// vuelve difícil es CUÁNTAS cápsulas hay a la vez (pedido de Ricardo: "más seguidas pero más lento"), no el apuro.
        /// </summary>
        public static float Speed(int level, bool precision) => (0.09f + 0.09f * (Clamp(level) - 1) / (MaxLevel - 1)) * (precision ? 0.8f : 1f);

        /// <summary>Cápsulas en viaje a la vez que busca el ritmo: 2,5 (nivel 1) → 7,5 (nivel 12).</summary>
        public static float TargetInFlight(int level) => 2.5f + 5f * (Clamp(level) - 1) / (MaxLevel - 1);

        /// <summary>
        /// Segundos entre salidas para que haya <see cref="TargetInFlight"/> cápsulas en viaje, según lo que tarda en
        /// promedio el viaje en ESTA red (<paramref name="meanTravelSeconds"/>). Nunca menos de <see cref="MinSpawnGap"/>;
        /// en Precisión, 30% más espaciadas.
        /// </summary>
        public static float SpawnInterval(int level, bool precision, float meanTravelSeconds) =>
            Math.Max(MinSpawnGap, Math.Min(4.5f, meanTravelSeconds / TargetInFlight(level))) * (precision ? 1.3f : 1f);

        /// <summary>Probabilidad de que la próxima sea urgente: 0 antes del nivel 5; 8% → 18% hasta el 12.</summary>
        public static float UrgentChance(int level) =>
            Clamp(level) < UrgentFromLevel ? 0f : 0.08f + 0.1f * (Clamp(level) - UrgentFromLevel) / (MaxLevel - UrgentFromLevel);

        /// <summary>¿La próxima es urgente? Nunca dos urgentes seguidas.</summary>
        public static bool NextIsUrgent(int level, bool previousUrgent, Random rng) =>
            !previousUrgent && rng.NextDouble() < UrgentChance(level);

        // ------------------------------------------------------------------ la red

        /// <summary>Giro de cada salida de un desvío respecto de la ruta que llega (como un cambio de vía), en grados.</summary>
        public const float BranchDeg = 38f;
        /// <summary>Distancia mínima entre dos rutas (anchos de campo), salvo junto al desvío del que salen las dos.</summary>
        public const float MinRouteGapWidth = 0.055f;
        /// <summary>Radio alrededor de un nodo compartido donde dos rutas pueden estar juntas (queda bajo el desvío).</summary>
        public const float ShareRadius = 0.1f;

        /// <summary>Cuánto se enroscan las rutas según el nivel: 0 = curvas suaves; 1 = serpentinas cerradas.</summary>
        public static float Twist(int level) => (Clamp(level) - 1f) / (MaxLevel - 1f);

        /// <summary>
        /// Arma una red de <paramref name="ports"/> puertos: los puertos se reparten a lo largo de una "U" (lado
        /// izquierdo, abajo, lado derecho) con un poco de desorden; el árbol se arma partiendo al azar el grupo de puertos
        /// en dos, y cada desvío se ubica entre el portal y el centro de sus puertos. Se reintenta hasta que no haya tramos
        /// que se crucen ni nodos o rutas demasiado juntos. Después las rectas se vuelven curvas y, según
        /// <paramref name="twist"/>, serpentinas (<see cref="CurveRoutes"/>).
        /// </summary>
        /// <param name="aspect">Alto / ancho del campo en pantalla: las distancias verticales se estiran por eso.</param>
        public static TrafficNetwork BuildNetwork(int ports, Random rng, float aspect = DefaultAspect, float twist = 0.5f)
        {
            ports = Math.Max(2, Math.Min(MaxPorts, ports));
            TrafficNetwork best = null;
            for (int attempt = 0; attempt < 300; attempt++)
            {
                var net = TryBuild(ports, rng);
                net.Aspect = aspect;
                Relax(net, aspect);
                net.EnsurePaths(true);
                best = net;
                if (IsPlanar(net) && MinGap(net, aspect) >= MinNodeGap && MinNodeToRail(net, aspect) >= MinRailGap
                    && MinRouteGap(net) >= MinRouteGapWidth)
                {
                    CurveRoutes(net, rng, Math.Max(0f, Math.Min(1f, twist)));
                    return net;
                }
            }
            return best;
        }

        private static TrafficNetwork TryBuild(int ports, Random rng)
        {
            var px = new float[ports];
            var py = new float[ports];
            for (int i = 0; i < ports; i++)
            {
                float t = ports == 1 ? 0.5f : 0.08f + 0.84f * i / (ports - 1);
                UPoint(t, out px[i], out py[i]);
                // Un poco de desorden: los planetas no quedan en fila como en una grilla.
                float inward = (float)rng.NextDouble();
                if (px[i] < 0.2f) px[i] += inward * 0.05f;
                else if (px[i] > 0.8f) px[i] -= inward * 0.05f;
                if (py[i] > 0.85f) py[i] -= (float)rng.NextDouble() * 0.05f;
                else py[i] += ((float)rng.NextDouble() - 0.5f) * 0.05f;
            }
            var net = new TrafficNetwork();
            int source = net.Add(SourceX, SourceY, -1);
            int top = BuildRange(net, source, 0, ports - 1, px, py, ports, rng);
            net.Children[source] = new[] { top };
            // Colores de los puertos al azar (que el mismo color no quede siempre en el mismo lugar).
            var colors = new List<int>();
            for (int i = 0; i < ports; i++) colors.Add(i);
            Shuffle(colors, rng);
            int k = 0;
            for (int n = 0; n < net.Count; n++) if (net.IsPort(n)) net.PortColor[n] = colors[k++];
            return net;
        }

        private static void Shuffle(List<int> list, Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private static int BuildRange(TrafficNetwork net, int parent, int a, int b, float[] px, float[] py, int total, Random rng)
        {
            if (a == b) return net.Add(px[a], py[a], parent);
            float cx = 0f, cy = 0f;
            for (int i = a; i <= b; i++) { cx += px[i]; cy += py[i]; }
            cx /= b - a + 1;
            cy /= b - a + 1;
            // Cuanto más grande el grupo, más cerca del portal queda su desvío.
            float f = 1f - 0.62f * (b - a + 1) / total;
            float jitter = ((float)rng.NextDouble() - 0.5f) * 0.06f;
            float x = SourceX + (cx - SourceX) * f + jitter;
            float y = SourceY + (cy - SourceY) * f + 0.05f;
            int node = net.Add(Clamp01(x, 0.12f, 0.88f), Clamp01(y, 0.14f, 0.8f), parent);
            int span = b - a;
            int m = a + span / 2 + (span > 1 ? rng.Next(-1, 1) : 0);
            m = Math.Max(a, Math.Min(b - 1, m));
            int left = BuildRange(net, node, a, m, px, py, total, rng);
            int right = BuildRange(net, node, m + 1, b, px, py, total, rng);
            net.Children[node] = new[] { left, right };
            return node;
        }

        /// <summary>
        /// Separa nodos demasiado juntos: el desvío (nunca un puerto ni el portal) se corre de a poco hacia el nodo del
        /// que viene, que lo aleja de sus hijos sin cruzar tramos.
        /// </summary>
        private static void Relax(TrafficNetwork net, float aspect)
        {
            for (int pass = 0; pass < 40; pass++)
            {
                bool moved = false;
                for (int i = 1; i < net.Count; i++)
                {
                    if (!net.IsSwitch(i)) continue;
                    for (int j = 0; j < net.Count; j++)
                    {
                        if (j == i) continue;
                        float dx = net.X[i] - net.X[j], dy = (net.Y[i] - net.Y[j]) * aspect;
                        if (dx * dx + dy * dy >= MinNodeGap * MinNodeGap * 1.1f) continue;
                        int p = net.Parent[i];
                        net.X[i] += (net.X[p] - net.X[i]) * 0.15f;
                        net.Y[i] += (net.Y[p] - net.Y[i]) * 0.15f;
                        moved = true;
                        break;
                    }
                }
                if (!moved) return;
            }
        }

        // ------------------------------------------------------------------ curvas

        /// <summary>
        /// Vuelve curvas las rectas. Cada tramo sale de su desvío siguiendo la ruta que llega, girado ±<see cref="BranchDeg"/>
        /// (como un cambio de vía: sin quiebres), y llega a su nodo por una curva de Bézier. Después, según
        /// <paramref name="twist"/>, la ruta del portal y algunos tramos largos se enroscan en serpentinas (cambios de
        /// sentido). Cada curva se acepta solo si no se cruza ni roza otras rutas ni nodos ajenos; si no, se prueba una
        /// más suave, y en el peor caso queda la recta (que ya se comprobó).
        /// </summary>
        private static void CurveRoutes(TrafficNetwork net, Random rng, float twist)
        {
            float a = net.Aspect;
            int count = net.Count;
            // Rumbo con que la ruta llega a cada nodo, en espacio "real" (x, y · proporción): los ángulos como se ven.
            var hx = new float[count];
            var hy = new float[count];
            hy[0] = 1f;
            var order = new List<int>();
            var queue = new Queue<int>();
            queue.Enqueue(0);
            while (queue.Count > 0)
            {
                int n = queue.Dequeue();
                order.Add(n);
                foreach (int c in net.Children[n]) queue.Enqueue(c);
            }

            var ctrl = new float[count][];
            foreach (int n in order)
            {
                if (n == 0) continue;
                int p = net.Parent[n];
                float ax = net.X[p], ay = net.Y[p] * a, bx = net.X[n], by = net.Y[n] * a;
                float ox = hx[p], oy = hy[p];
                if (net.IsSwitch(p))
                {
                    int other = net.Children[p][0] == n ? net.Children[p][1] : net.Children[p][0];
                    float cn = CrossZ(ox, oy, bx - ax, by - ay);
                    float co = CrossZ(ox, oy, net.X[other] - ax, net.Y[other] * a - ay);
                    float side = cn > co ? 1f : cn < co ? -1f : (n == net.Children[p][0] ? 1f : -1f);
                    Rotate(ref ox, ref oy, side * BranchDeg * (float)Math.PI / 180f);
                }
                float lx = bx - ax, ly = by - ay, len = Math.Max(1e-5f, Len(lx, ly));
                lx /= len;
                ly /= len;
                float ix, iy;
                if (net.IsPort(n))
                {
                    // A un planeta se llega "cayendo" un poco: la curva entra desde arriba.
                    ix = 0.6f * lx;
                    iy = 0.6f * ly + 0.4f;
                }
                else
                {
                    // A un desvío se llega apuntando hacia sus salidas (así sus dos ramas se abren parejas).
                    float kx = 0f, ky = 0f;
                    foreach (int c in net.Children[n]) { kx += net.X[c]; ky += net.Y[c] * a; }
                    kx = kx / net.Children[n].Length - bx;
                    ky = ky / net.Children[n].Length - by;
                    float kl = Math.Max(1e-5f, Len(kx, ky));
                    ix = lx + kx / kl;
                    iy = ly + ky / kl;
                }
                Normalize(ref ix, ref iy);

                // La recta (ya comprobada), muestreada como las curvas, por si ninguna curva cabe.
                ctrl[n] = new[] { ax, ay, ax, ay, bx, by, bx, by };
                SampleRoute(ctrl[n], 0f, 0, a, out var sx, out var sy);
                net.SetPath(n, sx, sy);
                foreach (float k in new[] { 0.42f, 0.3f, 0.18f })
                {
                    var c = new[] { ax, ay, ax + ox * k * len, ay + oy * k * len, bx - ix * k * len, by - iy * k * len, bx, by };
                    SampleRoute(c, 0f, 0, a, out var xs, out var ys);
                    if (!RouteFits(net, n, xs, ys)) continue;
                    net.SetPath(n, xs, ys);
                    ctrl[n] = c;
                    break;
                }
                var px = net.PathX[n];
                var py = net.PathY[n];
                int m = px.Length;
                hx[n] = px[m - 1] - px[m - 2];
                hy[n] = (py[m - 1] - py[m - 2]) * a;
                Normalize(ref hx[n], ref hy[n]);
            }

            // La ruta del portal: en niveles bajos una "S" suave; desde el 4, una cornisa que va y vuelve (cambios de
            // sentido con curvas redondas), con más vueltas en niveles altos.
            int trunk = net.Children[0][0];
            bool trunkDone = false;
            if (twist >= 0.25f)
            {
                int lanes = twist >= 0.62f ? 3 : 2;
                float first = rng.Next(2) == 0 ? 1f : -1f;
                for (int attempt = 0; attempt < 6 && !trunkDone; attempt++)
                {
                    int l = attempt < 4 ? lanes : lanes - 1;
                    if (l < 2) break;
                    float side = attempt % 2 == 0 ? first : -first;
                    float w = (0.2f + 0.12f * twist) * (attempt < 2 ? 1f : 0.8f);
                    if (!Switchback(net.X[0], net.Y[0] * a, net.X[trunk], net.Y[trunk] * a, hx[trunk], hy[trunk], l, w, side, a,
                            out var xs, out var ys)) continue;
                    if (!RouteFits(net, trunk, xs, ys)) continue;
                    net.SetPath(trunk, xs, ys);
                    trunkDone = true;
                }
            }

            // Ondulaciones: la ruta del portal (si no hizo cornisa) y, a veces, los tramos largos.
            var edges = new List<int>(order);
            edges.Remove(0);
            Shuffle(edges, rng);
            foreach (int n in edges)
            {
                var c = ctrl[n];
                float len = Len(c[6] - c[0], c[7] - c[1]);
                int waves;
                float amp;
                if (n == trunk)
                {
                    if (trunkDone) continue;
                    waves = 2;
                    amp = 0.07f + 0.1f * twist;
                }
                else
                {
                    if (len < 0.28f || rng.NextDouble() > 0.3 + 0.6 * twist) continue;
                    waves = len > 0.5f && twist > 0.4f ? 2 + rng.Next(2) : 1 + rng.Next(2);
                    amp = len * (0.07f + 0.1f * twist);
                }
                float sign = rng.Next(2) == 0 ? 1f : -1f;
                foreach (float s in new[] { 1f, 0.7f, 0.45f })
                {
                    SampleRoute(c, sign * amp * s, waves, a, out var xs, out var ys);
                    if (!RouteFits(net, n, xs, ys)) continue;
                    net.SetPath(n, xs, ys);
                    break;
                }
            }
        }

        /// <summary>Radio de giro mínimo de una ruta (anchos de campo): nada de quiebres en punta.</summary>
        public const float MinTurnRadius = 0.035f;

        /// <summary>
        /// Cornisa (espacio real, y hacia abajo): sale del portal hacia abajo, gira un cuarto hacia
        /// <paramref name="side"/>, recorre <paramref name="lanes"/> tramos a lo ancho unidos por medias vueltas de radio
        /// R (cada una baja 2R y cambia el sentido), gira hacia abajo y llega al desvío con el rumbo (ex, ey). Falla si
        /// no entra con curvas de radio razonable.
        /// </summary>
        private static bool Switchback(float ax, float ay, float bx, float by, float ex, float ey, int lanes, float w, float side,
            float aspect, out float[] xs, out float[] ys)
        {
            xs = ys = null;
            const float drop = 0.07f;
            float v = by - ay;
            float r = Math.Min(0.065f, (v - drop) / (2f * lanes));
            if (r < 0.04f) return false;
            var px = new List<float> { ax };
            var py = new List<float> { ay };
            float dir = side;
            // Primer cuarto de vuelta: de "hacia abajo" a "hacia dir".
            Arc(px, py, ax + dir * r, ay, r, dir > 0 ? Math.PI : 0.0, -dir * Math.PI / 2);
            float x = ax + dir * r, y = ay + r;
            for (int i = 0; i < lanes; i++)
            {
                bool last = i == lanes - 1;
                float xt = last ? bx - dir * r : ax + dir * (w - r);
                if ((xt - x) * dir < 0f) return false;
                Lane(px, py, x, xt, y);
                if (!last)
                {
                    // Media vuelta: baja 2R y queda yendo al revés.
                    Arc(px, py, xt, y + r, r, -Math.PI / 2, dir * Math.PI);
                    y += 2f * r;
                    x = xt;
                    dir = -dir;
                }
                else
                {
                    // Cuarto de vuelta final: queda hacia abajo.
                    Arc(px, py, xt, y + r, r, -Math.PI / 2, dir * Math.PI / 2);
                    x = xt + dir * r;
                    y += r;
                }
            }
            float h = (by - y) * 0.45f;
            if (by - y < drop * 0.8f) return false;
            var c = new[] { x, y, x, y + h, bx - ex * h, by - ey * h, bx, by };
            for (int i = 1; i <= 14; i++)
            {
                float t = i / 14f, u = 1f - t;
                float b0 = u * u * u, b1 = 3f * u * u * t, b2 = 3f * u * t * t, b3 = t * t * t;
                px.Add(b0 * c[0] + b1 * c[2] + b2 * c[4] + b3 * c[6]);
                py.Add(b0 * c[1] + b1 * c[3] + b2 * c[5] + b3 * c[7]);
            }
            xs = px.ToArray();
            ys = new float[py.Count];
            for (int i = 0; i < py.Count; i++) ys[i] = py[i] / aspect;
            return true;
        }

        /// <summary>Agrega un arco de centro (cx, cy) desde el ángulo <paramref name="from"/> recorriendo <paramref name="sweep"/> (radianes, y hacia abajo).</summary>
        private static void Arc(List<float> px, List<float> py, float cx, float cy, float r, double from, double sweep)
        {
            int steps = Math.Max(6, (int)Math.Ceiling(Math.Abs(sweep) / (Math.PI / 20)));
            for (int k = 1; k <= steps; k++)
            {
                double ang = from + sweep * k / steps;
                px.Add(cx + r * (float)Math.Cos(ang));
                py.Add(cy + r * (float)Math.Sin(ang));
            }
        }

        /// <summary>Tramo a lo ancho de la cornisa, con una comba suave hacia abajo (como un cable colgado), sin quiebres en los extremos.</summary>
        private static void Lane(List<float> px, List<float> py, float x0, float x1, float y)
        {
            float span = Math.Abs(x1 - x0);
            float sag = 0.025f * Math.Min(1f, span / 0.3f);
            int steps = Math.Max(2, (int)Math.Ceiling(span / 0.025f));
            for (int k = 1; k <= steps; k++)
            {
                float t = (float)k / steps, b = (float)Math.Sin(Math.PI * t);
                px.Add(x0 + (x1 - x0) * t);
                py.Add(y + sag * b * b);
            }
        }

        /// <summary>Menor radio de giro de un recorrido (anchos de campo).</summary>
        public static float TurnRadius(float[] xs, float[] ys, float aspect)
        {
            float g = float.MaxValue;
            for (int i = 1; i < xs.Length - 1; i++)
            {
                float ux = xs[i] - xs[i - 1], uy = (ys[i] - ys[i - 1]) * aspect;
                float vx = xs[i + 1] - xs[i], vy = (ys[i + 1] - ys[i]) * aspect;
                float lu = Len(ux, uy), lv = Len(vx, vy);
                if (lu < 1e-6f || lv < 1e-6f) continue;
                float ang = (float)Math.Atan2(Math.Abs(CrossZ(ux, uy, vx, vy)), ux * vx + uy * vy);
                if (ang < 1e-4f) continue;
                g = Math.Min(g, 0.5f * (lu + lv) / ang);
            }
            return g;
        }

        /// <summary>
        /// Muestrea una Bézier cúbica (puntos de control en espacio real) con una ondulación lateral de
        /// <paramref name="waves"/> medias ondas: el desvío lateral se apaga en los extremos, así la ruta sigue saliendo
        /// y llegando con el mismo rumbo. Devuelve coordenadas normalizadas.
        /// </summary>
        private static void SampleRoute(float[] c, float amp, int waves, float aspect, out float[] xs, out float[] ys)
        {
            int count = 28 + 14 * waves;
            xs = new float[count];
            ys = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / (count - 1), u = 1f - t;
                float b0 = u * u * u, b1 = 3f * u * u * t, b2 = 3f * u * t * t, b3 = t * t * t;
                float x = b0 * c[0] + b1 * c[2] + b2 * c[4] + b3 * c[6];
                float y = b0 * c[1] + b1 * c[3] + b2 * c[5] + b3 * c[7];
                if (amp != 0f && waves > 0)
                {
                    float dx = 3f * (u * u * (c[2] - c[0]) + 2f * u * t * (c[4] - c[2]) + t * t * (c[6] - c[4]));
                    float dy = 3f * (u * u * (c[3] - c[1]) + 2f * u * t * (c[5] - c[3]) + t * t * (c[7] - c[5]));
                    if (Len(dx, dy) < 1e-5f) { dx = c[6] - c[0]; dy = c[7] - c[1]; }
                    Normalize(ref dx, ref dy);
                    float envelope = Math.Min(1f, 6.4f * t * u);
                    float off = amp * envelope * (float)Math.Sin(Math.PI * waves * t);
                    x += -dy * off;
                    y += dx * off;
                }
                xs[i] = x;
                ys[i] = y / aspect;
            }
        }

        /// <summary>¿El recorrido candidato para el tramo que llega a <paramref name="n"/> cabe sin cruzar ni rozar nada?</summary>
        private static bool RouteFits(TrafficNetwork net, int n, float[] xs, float[] ys)
        {
            float a = net.Aspect;
            int m = xs.Length;
            float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
            for (int i = 0; i < m; i++)
            {
                if (xs[i] < 0.04f || xs[i] > 0.96f || ys[i] < 0.02f || ys[i] > 0.97f) return false;
                minX = Math.Min(minX, xs[i]); maxX = Math.Max(maxX, xs[i]);
                minY = Math.Min(minY, ys[i] * a); maxY = Math.Max(maxY, ys[i] * a);
            }
            if (TurnRadius(xs, ys, a) < MinTurnRadius) return false;
            // No se cruza a sí mismo.
            for (int i = 0; i < m - 1; i++)
                for (int j = i + 2; j < m - 1; j++)
                    if (SegmentsCross(xs[i], ys[i], xs[i + 1], ys[i + 1], xs[j], ys[j], xs[j + 1], ys[j + 1])) return false;
            int p = net.Parent[n];
            // Ningún nodo ajeno queda rozado.
            for (int o = 0; o < net.Count; o++)
            {
                if (o == n || o == p) continue;
                float ox = net.X[o], oy = net.Y[o] * a;
                if (ox < minX - MinRailGap || ox > maxX + MinRailGap || oy < minY - MinRailGap || oy > maxY + MinRailGap) continue;
                if (PointToRoute(ox, oy, xs, ys, a) < MinRailGap) return false;
            }
            // Ni cruza ni roza otras rutas.
            for (int e = 1; e < net.Count; e++)
            {
                if (e == n || net.PathX[e] == null) continue;
                int shared = e == p ? p : net.Parent[e] == n ? n : net.Parent[e] == p ? p : -1;
                if (!RoutesClear(net, xs, ys, net.PathX[e], net.PathY[e], shared)) return false;
            }
            return true;
        }

        /// <summary>
        /// Distancia (anchos de campo) entre dos recorridos, sin contar los segmentos junto al nodo que comparten
        /// (<paramref name="shared"/>, -1 si ninguno); 0 si se cruzan. Deja de buscar al bajar de <paramref name="stopBelow"/>.
        /// </summary>
        private static float RouteDistance(TrafficNetwork net, float[] ax, float[] ay, float[] bx, float[] by, int shared, float stopBelow)
        {
            float a = net.Aspect, g = float.MaxValue;
            if (!BoxesNear(ax, ay, bx, by, a, MinRouteGapWidth * 2f)) return g;
            float sx = shared >= 0 ? net.X[shared] : 0f, sy = shared >= 0 ? net.Y[shared] * a : 0f;
            for (int i = 0; i < ax.Length - 1; i++)
            {
                float p0x = ax[i], p0y = ay[i] * a, p1x = ax[i + 1], p1y = ay[i + 1] * a;
                bool nearA = shared >= 0 && (Len(p0x - sx, p0y - sy) < ShareRadius || Len(p1x - sx, p1y - sy) < ShareRadius);
                for (int j = 0; j < bx.Length - 1; j++)
                {
                    float q0x = bx[j], q0y = by[j] * a, q1x = bx[j + 1], q1y = by[j + 1] * a;
                    if (SegmentsCross(p0x, p0y, p1x, p1y, q0x, q0y, q1x, q1y)) return 0f;
                    if (nearA || (shared >= 0 && (Len(q0x - sx, q0y - sy) < ShareRadius || Len(q1x - sx, q1y - sy) < ShareRadius))) continue;
                    float d = Math.Min(Math.Min(PointSegment(p0x, p0y, q0x, q0y, q1x, q1y), PointSegment(p1x, p1y, q0x, q0y, q1x, q1y)),
                                       Math.Min(PointSegment(q0x, q0y, p0x, p0y, p1x, p1y), PointSegment(q1x, q1y, p0x, p0y, p1x, p1y)));
                    if (d < g)
                    {
                        g = d;
                        if (g < stopBelow) return g;
                    }
                }
            }
            return g;
        }

        private static bool RoutesClear(TrafficNetwork net, float[] ax, float[] ay, float[] bx, float[] by, int shared) =>
            RouteDistance(net, ax, ay, bx, by, shared, MinRouteGapWidth) >= MinRouteGapWidth;

        private static bool BoxesNear(float[] ax, float[] ay, float[] bx, float[] by, float a, float margin)
        {
            Box(ax, ay, a, out float x0, out float x1, out float y0, out float y1);
            Box(bx, by, a, out float u0, out float u1, out float v0, out float v1);
            return !(x1 + margin < u0 || u1 + margin < x0 || y1 + margin < v0 || v1 + margin < y0);
        }

        private static void Box(float[] xs, float[] ys, float a, out float x0, out float x1, out float y0, out float y1)
        {
            x0 = y0 = float.MaxValue;
            x1 = y1 = float.MinValue;
            for (int i = 0; i < xs.Length; i++)
            {
                x0 = Math.Min(x0, xs[i]); x1 = Math.Max(x1, xs[i]);
                y0 = Math.Min(y0, ys[i] * a); y1 = Math.Max(y1, ys[i] * a);
            }
        }

        private static float PointToRoute(float px, float py, float[] xs, float[] ys, float a)
        {
            float g = float.MaxValue;
            for (int i = 0; i < xs.Length - 1; i++)
                g = Math.Min(g, PointSegment(px, py, xs[i], ys[i] * a, xs[i + 1], ys[i + 1] * a));
            return g;
        }

        private static float CrossZ(float ax, float ay, float bx, float by) => ax * by - ay * bx;

        private static void Rotate(ref float x, ref float y, float radians)
        {
            float c = (float)Math.Cos(radians), s = (float)Math.Sin(radians);
            float nx = x * c - y * s, ny = x * s + y * c;
            x = nx;
            y = ny;
        }

        private static float Len(float x, float y) => (float)Math.Sqrt(x * x + y * y);

        private static void Normalize(ref float x, ref float y)
        {
            float l = Len(x, y);
            if (l < 1e-6f) { x = 0f; y = 1f; return; }
            x /= l;
            y /= l;
        }

        /// <summary>Punto de la "U" de puertos para t en 0..1 (lado izquierdo de arriba a abajo, fondo, lado derecho).</summary>
        public static void UPoint(float t, out float x, out float y)
        {
            const float l = 0.09f, r = 0.91f, top = 0.42f, bottom = 0.9f;
            float side = bottom - top, width = r - l, total = side * 2f + width;
            float d = t * total;
            if (d <= side) { x = l; y = top + d; }
            else if (d <= side + width) { x = l + (d - side); y = bottom; }
            else { x = r; y = bottom - (d - side - width); }
        }

        private static float Clamp01(float v, float lo, float hi) => Math.Max(lo, Math.Min(hi, v));

        /// <summary>¿Ningún recorrido cruza a otro (ni a sí mismo)? Tramos que parten del mismo punto no cuentan como cruce.</summary>
        public static bool IsPlanar(TrafficNetwork net)
        {
            net.EnsurePaths();
            for (int e = 1; e < net.Count; e++)
            {
                var xs = net.PathX[e];
                var ys = net.PathY[e];
                for (int i = 0; i < xs.Length - 1; i++)
                    for (int j = i + 2; j < xs.Length - 1; j++)
                        if (SegmentsCross(xs[i], ys[i], xs[i + 1], ys[i + 1], xs[j], ys[j], xs[j + 1], ys[j + 1])) return false;
                for (int f = e + 1; f < net.Count; f++)
                {
                    var us = net.PathX[f];
                    var vs = net.PathY[f];
                    for (int i = 0; i < xs.Length - 1; i++)
                        for (int j = 0; j < us.Length - 1; j++)
                            if (SegmentsCross(xs[i], ys[i], xs[i + 1], ys[i + 1], us[j], vs[j], us[j + 1], vs[j + 1])) return false;
                }
            }
            return true;
        }

        /// <summary>Menor distancia entre dos nodos, en anchos de campo (con la proporción real de la pantalla).</summary>
        public static float MinGap(TrafficNetwork net, float aspect = DefaultAspect)
        {
            float g = float.MaxValue;
            for (int i = 0; i < net.Count; i++)
                for (int j = i + 1; j < net.Count; j++)
                {
                    float dx = net.X[i] - net.X[j], dy = (net.Y[i] - net.Y[j]) * aspect;
                    g = Math.Min(g, (float)Math.Sqrt(dx * dx + dy * dy));
                }
            return g;
        }

        /// <summary>Menor distancia (en anchos de campo) entre un nodo y un recorrido que no lo toca.</summary>
        public static float MinNodeToRail(TrafficNetwork net, float aspect = DefaultAspect)
        {
            net.EnsurePaths();
            float g = float.MaxValue;
            for (int e = 1; e < net.Count; e++)
                for (int n = 0; n < net.Count; n++)
                {
                    if (n == e || n == net.Parent[e]) continue;
                    g = Math.Min(g, PointToRoute(net.X[n], net.Y[n] * aspect, net.PathX[e], net.PathY[e], aspect));
                }
            return g;
        }

        /// <summary>
        /// Menor distancia (anchos de campo) entre dos recorridos, sin contar el tramo junto al nodo que comparten.
        /// 0 si alguno se cruza.
        /// </summary>
        public static float MinRouteGap(TrafficNetwork net)
        {
            net.EnsurePaths();
            float g = float.MaxValue;
            for (int e = 1; e < net.Count; e++)
                for (int f = e + 1; f < net.Count; f++)
                {
                    int pe = net.Parent[e], pf = net.Parent[f];
                    int shared = pe == pf ? pe : pe == f ? f : pf == e ? e : -1;
                    g = Math.Min(g, RouteDistance(net, net.PathX[e], net.PathY[e], net.PathX[f], net.PathY[f], shared, 0f));
                }
            return g;
        }

        private static float PointSegment(float px, float py, float ax, float ay, float bx, float by)
        {
            float vx = bx - ax, vy = by - ay;
            float t = Math.Max(0f, Math.Min(1f, ((px - ax) * vx + (py - ay) * vy) / Math.Max(1e-6f, vx * vx + vy * vy)));
            float dx = px - (ax + vx * t), dy = py - (ay + vy * t);
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        private static bool SegmentsCross(float ax, float ay, float bx, float by, float cx, float cy, float dx, float dy)
        {
            float d1 = Cross(cx, cy, dx, dy, ax, ay), d2 = Cross(cx, cy, dx, dy, bx, by);
            float d3 = Cross(ax, ay, bx, by, cx, cy), d4 = Cross(ax, ay, bx, by, dx, dy);
            return ((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) && ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0));
        }

        private static float Cross(float ox, float oy, float ax, float ay, float bx, float by) =>
            (ax - ox) * (by - oy) - (ay - oy) * (bx - ox);

        /// <summary>Color de la próxima cápsula: al azar, sin repetir el anterior (para que haya que mover desvíos).</summary>
        public static int NextColor(int ports, int previous, Random rng)
        {
            if (ports <= 1) return 0;
            int c = rng.Next(ports - 1);
            return previous >= 0 && c >= previous ? c + 1 : c;
        }

        // ------------------------------------------------------------------ puntaje y medidas

        public static int Points(bool correct, int level, int streak, bool urgent = false) =>
            correct ? (50 + 10 * (Clamp(level) - 1) + 15 * Math.Min(Math.Max(streak - 1, 0), 10)) * (urgent ? 2 : 1) : 0;

        /// <summary>
        /// "Tu carga": la mayor cantidad de cápsulas en viaje a la vez en un momento LIMPIO, es decir, sin ninguna cápsula
        /// mal entregada en viaje (cada error ensucia el tramo de tiempo desde que esa cápsula se vio hasta que llegó).
        /// Muestras: momento y cantidad en viaje. 0 si no hay momentos limpios.
        /// </summary>
        public static int CleanPeakLoad(IReadOnlyList<float> times, IReadOnlyList<int> inFlight,
            IReadOnlyList<float> errorFrom, IReadOnlyList<float> errorTo)
        {
            int best = 0;
            for (int i = 0; i < times.Count && i < inFlight.Count; i++)
            {
                bool clean = true;
                for (int e = 0; e < errorFrom.Count && e < errorTo.Count; e++)
                    if (times[i] >= errorFrom[e] && times[i] <= errorTo[e]) { clean = false; break; }
                if (clean) best = Math.Max(best, inFlight[i]);
            }
            return best;
        }

        /// <summary>
        /// "Tu anticipación": mediana de cuánto antes se preparan los desvíos (solo los movidos para la cápsula que
        /// pasa, y bien). -1 con menos de 5 datos.
        /// </summary>
        public static float MedianLead(IReadOnlyList<float> leads)
        {
            if (leads == null || leads.Count < 5) return -1f;
            var s = new List<float>();
            foreach (float l in leads) s.Add(Math.Min(MaxLead, Math.Max(0f, l)));
            s.Sort();
            int n = s.Count;
            return n % 2 == 1 ? s[n / 2] : (s[n / 2 - 1] + s[n / 2]) * 0.5f;
        }

        /// <summary>Proporción de desvíos preparados con tiempo (≥ <see cref="ProactiveLead"/>). -1 con menos de 5 datos.</summary>
        public static float ProactiveShare(IReadOnlyList<float> leads)
        {
            if (leads == null || leads.Count < 5) return -1f;
            int p = 0;
            foreach (float l in leads) if (l >= ProactiveLead) p++;
            return (float)p / leads.Count;
        }

        /// <summary>Puntaje 0-100: cápsulas bien entregadas (65%) y nivel más alto alcanzado (35%).</summary>
        public static int Score(float accuracy, int peakLevel)
        {
            float acc = Math.Max(0f, Math.Min(1f, accuracy));
            float lv = (float)(Clamp(peakLevel) - 1) / (MaxLevel - 1);
            return Math.Max(0, Math.Min(100, (int)Math.Round((0.65f * acc + 0.35f * lv) * 100f)));
        }

        private static int Clamp(int level) => Math.Max(1, Math.Min(MaxLevel, level));
    }
}
