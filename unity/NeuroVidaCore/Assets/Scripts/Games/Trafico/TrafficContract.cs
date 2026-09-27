using System;
using System.Collections.Generic;

namespace NeuroVida.Games.Trafico
{
    /// <summary>
    /// La red de rutas: un árbol que nace en el portal (nodo 0), se abre en desvíos (dos salidas cada uno) y termina en
    /// los puertos (hojas). Coordenadas normalizadas del campo: x 0..1 (izquierda a derecha), y 0..1 (arriba a abajo).
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

        public float EdgeLength(int from, int to)
        {
            float dx = X[to] - X[from], dy = Y[to] - Y[from];
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }
    }

    /// <summary>Una cápsula en viaje: va de <see cref="From"/> a <see cref="To"/> y lleva recorrido <see cref="Progress"/> del tramo.</summary>
    public sealed class Pod
    {
        public int Id, Color, From, To;
        public float Progress, SpawnTime;
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

        public Pod Spawn(int color)
        {
            var p = new Pod { Id = _nextId++, Color = color, From = 0, To = Net.Children[0][0], Progress = 0f, SpawnTime = Time };
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
                float remaining = Speed * dt;
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
        public void Position(Pod p, out float x, out float y)
        {
            x = Net.X[p.From] + (Net.X[p.To] - Net.X[p.From]) * p.Progress;
            y = Net.Y[p.From] + (Net.Y[p.To] - Net.Y[p.From]) * p.Progress;
        }
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
        /// <summary>Cápsulas por oleada (entre oleadas puede cambiar el mapa).</summary>
        public const int WaveSize = 10;

        /// <summary>Anticipación desde la que un desvío cuenta como "preparado con tiempo" (s).</summary>
        public const float ProactiveLead = 1.0f;
        /// <summary>Tope de anticipación que se registra (s).</summary>
        public const float MaxLead = 4f;

        /// <summary>Punto de partida (el portal) y distancia mínima entre nodos, en anchos de campo (para que puertos y
        /// desvíos no se monten y se puedan tocar).</summary>
        public const float SourceX = 0.5f, SourceY = 0.05f, MinNodeGap = 0.14f;

        /// <summary>Distancia mínima de un nodo a una ruta que no es suya (que ningún tramo pase rozando un desvío o
        /// un planeta ajeno: confundiría por dónde va).</summary>
        public const float MinRailGap = 0.08f;

        /// <summary>Proporción alto/ancho típica del campo en un teléfono vertical.</summary>
        public const float DefaultAspect = 1.6f;

        private static readonly int[] PortsByLevel = { 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8 };

        public static int Ports(int level) => PortsByLevel[Clamp(level) - 1];

        /// <summary>Velocidad (alturas de campo por segundo): 0,16 → 0,33.</summary>
        public static float Speed(int level, bool precision) => (0.16f + 0.0155f * (Clamp(level) - 1)) * (precision ? 0.8f : 1f);

        /// <summary>Segundos entre cápsulas: 3,4 → 1,4 (Precisión: 30% más).</summary>
        public static float SpawnInterval(int level, bool precision) => (3.4f - 0.18f * (Clamp(level) - 1)) * (precision ? 1.3f : 1f);

        // ------------------------------------------------------------------ la red

        /// <summary>
        /// Arma una red de <paramref name="ports"/> puertos: los puertos se reparten a lo largo de una "U" (lado
        /// izquierdo, abajo, lado derecho); el árbol se arma partiendo al azar el grupo de puertos en dos, y cada desvío
        /// se ubica entre el portal y el centro de sus puertos. Se reintenta hasta que no haya tramos que se crucen ni
        /// nodos demasiado juntos.
        /// </summary>
        /// <param name="aspect">Alto / ancho del campo en pantalla: las distancias verticales se estiran por eso.</param>
        public static TrafficNetwork BuildNetwork(int ports, Random rng, float aspect = DefaultAspect)
        {
            ports = Math.Max(2, Math.Min(MaxPorts, ports));
            TrafficNetwork best = null;
            for (int attempt = 0; attempt < 200; attempt++)
            {
                var net = TryBuild(ports, rng);
                Relax(net, aspect);
                best = net;
                if (IsPlanar(net) && MinGap(net, aspect) >= MinNodeGap && MinNodeToRail(net, aspect) >= MinRailGap) return net;
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
            }
            var net = new TrafficNetwork();
            int source = net.Add(SourceX, SourceY, -1);
            int top = BuildRange(net, source, 0, ports - 1, px, py, ports, rng);
            net.Children[source] = new[] { top };
            // Colores de los puertos al azar (que el mismo color no quede siempre en el mismo lugar).
            var colors = new List<int>();
            for (int i = 0; i < ports; i++) colors.Add(i);
            for (int i = colors.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (colors[i], colors[j]) = (colors[j], colors[i]);
            }
            int k = 0;
            for (int n = 0; n < net.Count; n++) if (net.IsPort(n)) net.PortColor[n] = colors[k++];
            return net;
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
            int node = net.Add(Clamp01(x, 0.12f, 0.88f), Clamp01(y, 0.12f, 0.8f), parent);
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

        public static bool IsPlanar(TrafficNetwork net)
        {
            var edges = new List<(int, int)>();
            for (int n = 0; n < net.Count; n++) foreach (int c in net.Children[n]) edges.Add((n, c));
            for (int i = 0; i < edges.Count; i++)
                for (int j = i + 1; j < edges.Count; j++)
                {
                    var (a, b) = edges[i];
                    var (c, d) = edges[j];
                    if (a == c || a == d || b == c || b == d) continue; // comparten un nodo
                    if (SegmentsCross(net.X[a], net.Y[a], net.X[b], net.Y[b], net.X[c], net.Y[c], net.X[d], net.Y[d])) return false;
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

        /// <summary>Menor distancia (en anchos de campo) entre un nodo y un tramo que no lo toca.</summary>
        public static float MinNodeToRail(TrafficNetwork net, float aspect = DefaultAspect)
        {
            float g = float.MaxValue;
            for (int a = 0; a < net.Count; a++)
                foreach (int b in net.Children[a])
                    for (int n = 0; n < net.Count; n++)
                    {
                        if (n == a || n == b) continue;
                        g = Math.Min(g, PointSegment(net.X[n], net.Y[n] * aspect, net.X[a], net.Y[a] * aspect, net.X[b], net.Y[b] * aspect));
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

        public static int Points(bool correct, int level, int streak) =>
            correct ? 50 + 10 * (Clamp(level) - 1) + 15 * Math.Min(Math.Max(streak - 1, 0), 10) : 0;

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
