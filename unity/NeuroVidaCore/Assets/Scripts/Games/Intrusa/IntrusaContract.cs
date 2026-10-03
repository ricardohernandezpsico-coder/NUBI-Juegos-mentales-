using System;
using System.Collections.Generic;

namespace NeuroVida.Games.Intrusa
{
    /// <summary>Un grupo del banco (Resources/Lexico/intrusa_es.json: lo escribe tools/intrusa/grupos.py): 4 palabras que comparten una
    /// regla y una intrusa. <c>t</c> = tipo 1 a 6 (amplia, vecina, uso, material-lugar-parte, trampa, regla + trampa); <c>k</c> = clave de la
    /// regla (una figura por clave); <c>o</c> = 3 opciones de «¿Qué las une?» y <c>c</c> cuál es la correcta; <c>a</c>/<c>r</c> = en las
    /// trampas, con qué palabra del grupo «va» la intrusa y cómo decirlo.</summary>
    [Serializable]
    public sealed class IntrusaGroup
    {
        public string i;
        public int t;
        public string[] p;
        public string x;
        public string n;
        public string[] o;
        public int c;
        public string a;
        public string e;
        public string r;
        public string k;

        public bool IsTrap => t >= 5;
    }

    [Serializable]
    public sealed class IntrusaData
    {
        public int version;
        public string idioma;
        public string fuente;
        public IntrusaGroup[] grupos;
    }

    /// <summary>Un trazo del grabado: contorno suavizado por puntos ([x0,y0,x1,y1…], <c>suave</c>) o una elipse ([cx,cy,rx,ry]).</summary>
    [Serializable]
    public sealed class FigureOutline
    {
        public bool suave;
        public float[] puntos;
        public float[] elipse;
    }

    /// <summary>Un detalle del grabado: <c>tipo</c> = "ojo" ([x,y]), "punto" ([x,y], un aro pequeño) o "linea" ([x1,y1,x2,y2]).</summary>
    [Serializable]
    public sealed class FigureDetail
    {
        public string tipo;
        public float[] v;
    }

    /// <summary>Una figura (Resources/Lexico/intrusa_figuras.json: lo escribe tools/intrusa/figuras.py): el dibujo de estrellas propio de
    /// una regla. Todo en 0..1, en listas planas (JsonUtility no lee listas de listas). Las <c>aristas</c> van en el orden en que las
    /// recorre la chispa; las <c>anclas</c> son las 4 estrellas con palabra; la intrusa cae en uno de los <c>huecos</c>.</summary>
    [Serializable]
    public sealed class FigureShape
    {
        public string regla;
        public string nombre;
        public float[] puntos;
        public int[] aristas;
        public int[] anclas;
        public float[] huecos;
        public FigureOutline[] contorno;
        public FigureDetail[] detalles;

        public int PointCount => puntos == null ? 0 : puntos.Length / 2;
        public int EdgeCount => aristas == null ? 0 : aristas.Length / 2;
        public int HoleCount => huecos == null ? 0 : huecos.Length / 2;
        public Vec2 Point(int i) => new Vec2(puntos[i * 2], puntos[i * 2 + 1]);
        public Vec2 Hole(int i) => new Vec2(huecos[i * 2], huecos[i * 2 + 1]);
    }

    [Serializable]
    public sealed class FigureData
    {
        public int version;
        public FigureShape[] figuras;
    }

    /// <summary>De dónde saca grupos el juego (separado para probarlo con listas chicas).</summary>
    public interface IGroupSource
    {
        IReadOnlyList<IntrusaGroup> OfType(int type);
        IReadOnlyList<IntrusaGroup> OfRule(string key);
        FigureShape FigureOf(string key);
    }

    /// <summary>Punto 2D sin UnityEngine (las reglas puras se prueban con NUnit sin el motor).</summary>
    public struct Vec2
    {
        public float X, Y;
        public Vec2(float x, float y) { X = x; Y = y; }
        public static Vec2 operator +(Vec2 a, Vec2 b) => new Vec2(a.X + b.X, a.Y + b.Y);
        public static Vec2 operator -(Vec2 a, Vec2 b) => new Vec2(a.X - b.X, a.Y - b.Y);
        public static Vec2 operator *(Vec2 a, float k) => new Vec2(a.X * k, a.Y * k);
        public float Length => (float)Math.Sqrt(X * X + Y * Y);
        public static float Distance(Vec2 a, Vec2 b) => (a - b).Length;
    }

    /// <summary>Una ronda armada: qué grupo, qué figura y cómo se muestra (reflejo y giro al azar, hueco de la intrusa, qué palabra va en
    /// cada ancla).</summary>
    public sealed class IntrusaSpec
    {
        public IntrusaGroup G;
        public FigureShape Figure;
        /// <summary>Regla que vuelve a salir porque se falló en otro día (se marca «por repasar»).</summary>
        public bool Review;
        public bool Mirror;
        public float RotationDeg;
        public int Hole;
        /// <summary>Las 4 palabras del grupo, en el orden de las 4 anclas de la figura.</summary>
        public string[] AnchorWords;
    }

    /// <summary>
    /// Reglas puras de "La estrella intrusa" (juego estrella de Lenguaje; ver docs/diseno-estrella-intrusa.md). Cinco estrellas-palabra,
    /// una no pertenece: se toca; cae como estrella fugaz y las otras cuatro, que forman la figura de su regla, se unen cuando una
    /// chispa traza el dibujo y aparece un grabado con su nombre. Mide cómo se organizan los significados (por tipo de cosa, por
    /// uso, por lo que va junto) y cuánto cuesta resistir una asociación engañosa (Mirman, Landrigan y Britt, 2017; Geller, Landrigan y
    /// Mirman, 2019). Las líneas NO se ven antes de responder: la figura no regala la respuesta. Sin UnityEngine: testeable con NUnit.
    /// </summary>
    public static class IntrusaContract
    {
        public const string GameId = "intrusa";
        public const int MaxLevel = 12;

        public const int RetoSeconds = 120;
        public const int PrecisionRounds = 20;
        /// <summary>Desde este nivel, tras acertar, aparece «¿Qué las une?» (opcional, 4 s; mayores 6 s).</summary>
        public const int BonusFromLevel = 3;
        public const float BonusSeconds = 4f;
        public const float SeniorBonusSeconds = 6f;
        /// <summary>Racha desde la que el cielo se despeja (puntos × 1,5).</summary>
        public const int ClearStreak = 5;
        public const float ClearMultiplier = 1.5f;
        public const int RecentGames = 3;
        public const float MinTouchDp = 64f;
        /// <summary>Cada cuántas rondas vuelve una regla por repasar (si hay alguna pendiente de otro día).</summary>
        public const int ReviewEvery = 4;
        /// <summary>Rondas mínimas para dar la medida de una categoría y para hablar de las trampas.</summary>
        public const int MinCategoryRounds = 3;
        public const int MinTrapRounds = 4;
        /// <summary>Segundos que dura la caída de la intrusa (estrella fugaz).</summary>
        public const float FallSeconds = 0.9f;

        public static int ClampLevel(int level) => Math.Max(1, Math.Min(MaxLevel, level));

        /// <summary>Tipo de grupo por nivel: 1-2 amplia, 3-4 vecina, 5-6 uso, 7-8 material/lugar/parte, 9-10 trampa, 11-12 regla + trampa.</summary>
        public static int TypeForLevel(int level) => (ClampLevel(level) - 1) / 2 + 1;

        public static bool HasBonus(int level) => ClampLevel(level) >= BonusFromLevel;
        public static float BonusWindow(bool senior) => senior ? SeniorBonusSeconds : BonusSeconds;

        public static string LevelNews(int level)
        {
            switch (ClampLevel(level))
            {
                case 3: return "Ahora, «¿qué las une?»";
                case 5: return "Ahora, para qué sirven";
                case 7: return "Dónde viven, de qué están hechas";
                case 9: return "Cuidado: hay palabras que van juntas";
                case 11: return "Reglas menos obvias";
                default: return "";
            }
        }

        // ------------------------------------------------------------------ puntaje

        /// <summary>Puntos de un acierto: 100 + 10 por nivel; «¿Qué las une?» acertada = × 2; con el cielo despejado (racha ≥ 5) × 1,5.</summary>
        public static int Points(int level, int streak, bool named)
        {
            float pts = 100f + 10f * (ClampLevel(level) - 1);
            if (named) pts *= 2f;
            if (streak >= ClearStreak) pts *= ClearMultiplier;
            return (int)Math.Round(pts);
        }

        /// <summary>Puntaje 0-100: aciertos / rondas (70%) y nivel más alto alcanzado (30%).</summary>
        public static int Score(float accuracy, int peakLevel)
        {
            float lv = (float)(ClampLevel(peakLevel) - 1) / (MaxLevel - 1);
            float a = Math.Max(0f, Math.Min(1f, accuracy));
            return Math.Max(0, Math.Min(100, (int)Math.Round((0.7f * a + 0.3f * lv) * 100f)));
        }

        // ------------------------------------------------------------------ medidas

        /// <summary>Categoría de medida de un tipo: 0 = tipo de cosa (1-2), 1 = uso (3), 2 = material, lugar o parte (4), 3 = trampas (5-6).</summary>
        public static int CategoryOfType(int type)
        {
            switch (type)
            {
                case 1: case 2: return 0;
                case 3: return 1;
                case 4: return 2;
                default: return 3;
            }
        }

        public static readonly string[] CategoryNames = { "Tipo de cosa", "Para qué sirve", "Dónde está o de qué es", "Trampas" };

        /// <summary>La categoría de menor acierto entre las que tienen al menos <see cref="MinCategoryRounds"/> rondas, si hay dos o más y
        /// no todas empatan; -1 si no se puede decir (la más baja se marca con TEXTO, nunca solo con color).</summary>
        public static int LowestCategory(IReadOnlyList<int> seen, IReadOnlyList<int> hits)
        {
            int best = -1, valid = 0;
            float low = float.MaxValue, high = -1f;
            for (int c = 0; c < seen.Count; c++)
            {
                if (seen[c] < MinCategoryRounds) continue;
                valid++;
                float r = hits[c] / (float)seen[c];
                if (r < low) { low = r; best = c; }
                if (r > high) high = r;
            }
            return valid >= 2 && high - low > 1e-6f ? best : -1;
        }

        /// <summary>Trampas: «resististe N de M» (con al menos <see cref="MinTrapRounds"/> trampas); la misma fuente da «te engañaron M − N de M».</summary>
        public static bool TrapReadable(int trapSeen) => trapSeen >= MinTrapRounds;

        /// <summary>Mediana de una lista de tiempos (ms); -1 si hay menos de [min].</summary>
        public static int MedianMs(IReadOnlyList<int> values, int min = 4)
        {
            if (values == null || values.Count < min) return -1;
            var s = new List<int>(values);
            s.Sort();
            int n = s.Count;
            return n % 2 == 1 ? s[n / 2] : (int)Math.Round((s[n / 2 - 1] + s[n / 2]) / 2.0);
        }

        // ------------------------------------------------------------------ grupos de las últimas partidas

        /// <summary>Ids de los grupos de las últimas partidas: partidas separadas por ';' e ids por ','.</summary>
        public static List<List<string>> ParseRecent(string stored)
        {
            var games = new List<List<string>>();
            if (string.IsNullOrEmpty(stored)) return games;
            foreach (var g in stored.Split(';'))
            {
                var ids = new List<string>();
                foreach (var id in g.Split(','))
                    if (!string.IsNullOrWhiteSpace(id)) ids.Add(id.Trim());
                if (ids.Count > 0) games.Add(ids);
            }
            return games;
        }

        public static string PushRecent(string stored, IEnumerable<string> thisGame)
        {
            var games = ParseRecent(stored);
            var mine = new List<string>(thisGame);
            if (mine.Count > 0) games.Add(mine);
            while (games.Count > RecentGames) games.RemoveAt(0);
            var parts = new List<string>();
            foreach (var g in games) parts.Add(string.Join(",", g));
            return string.Join(";", parts);
        }

        public static HashSet<string> RecentIds(string stored)
        {
            var set = new HashSet<string>();
            foreach (var g in ParseRecent(stored)) foreach (var id in g) set.Add(id);
            return set;
        }

        // ------------------------------------------------------------------ repaso espaciado y atlas

        /// <summary>Reglas falladas pendientes de repasar, con el día (número de día desde 1970) en que se fallaron: «clave:día;clave:día».
        /// Las claves no llevan ni ':' ni ';'.</summary>
        public static Dictionary<string, int> ParseReview(string stored)
        {
            var d = new Dictionary<string, int>();
            if (string.IsNullOrEmpty(stored)) return d;
            foreach (var part in stored.Split(';'))
            {
                int cut = part.LastIndexOf(':');
                if (cut <= 0) continue;
                if (int.TryParse(part.Substring(cut + 1), out int day)) d[part.Substring(0, cut)] = day;
            }
            return d;
        }

        public static string FormatReview(IDictionary<string, int> review)
        {
            var parts = new List<string>();
            foreach (var kv in review) parts.Add(kv.Key + ":" + kv.Value);
            parts.Sort(StringComparer.Ordinal);
            return string.Join(";", parts);
        }

        /// <summary>Las reglas que se pueden repasar hoy: las falladas en un día ANTERIOR (en otra partida de otro día, con palabras distintas).</summary>
        public static List<string> ReviewDue(IDictionary<string, int> review, int today)
        {
            var due = new List<string>();
            foreach (var kv in review) if (kv.Value < today) due.Add(kv.Key);
            due.Sort(StringComparer.Ordinal);
            return due;
        }

        /// <summary>Claves de las láminas ganadas (el atlas), separadas por ';'.</summary>
        public static HashSet<string> ParsePlates(string stored)
        {
            var set = new HashSet<string>();
            if (string.IsNullOrEmpty(stored)) return set;
            foreach (var k in stored.Split(';')) if (!string.IsNullOrWhiteSpace(k)) set.Add(k.Trim());
            return set;
        }

        public static string FormatPlates(IEnumerable<string> plates)
        {
            var list = new List<string>(plates);
            list.Sort(StringComparer.Ordinal);
            return string.Join(";", list);
        }

        /// <summary>Día actual (días desde 1970, hora local) para fechar las reglas por repasar.</summary>
        public static int DayNumber(DateTime local) => (int)Math.Floor((local.Date - new DateTime(1970, 1, 1)).TotalDays);
    }
}
