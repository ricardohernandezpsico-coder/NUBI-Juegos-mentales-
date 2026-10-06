using System;
using System.Collections.Generic;

namespace NeuroVida.Games.Bodega
{
    /// <summary>Las tarjetas «NUEVO» (una sola vez por instalación cada una): la presentación de la bodega, la caja que cambia de lugar y la bodega que gira.</summary>
    public enum Intro { None = 0, Bodega = 1, Mueve = 2, Gira = 3 }

    /// <summary>Una etapa de la escalera (12 niveles del DDA común): cuántas escotillas, cuántos objetos y qué se suma.</summary>
    public sealed class Stage
    {
        public int Number, Hatches, Objects, Moves;
        public bool Spin;
        /// <summary>El grupo de la pantalla final (1..5).</summary>
        public int Group;
    }

    /// <summary>Una caja que cambia de lugar: sale de <see cref="From"/> con el objeto <see cref="Obj"/> y entra en <see cref="To"/> (siempre una escotilla vacía).</summary>
    public struct Move
    {
        public int From, To, Obj;
    }

    /// <summary>
    /// Un pedido completo: lo que el robot guarda, las cajas que cambia de lugar, cuánto gira la bodega y en qué orden se piden los objetos.
    /// Las escotillas se numeran 0..Hatches-1; la esclusa de carga es la posición 0 del anillo y las escotillas ocupan las posiciones 1..Hatches (<see cref="Hatches"/> + 1 lugares en total).
    /// La esclusa NUNCA es una escotilla elegible: no se guarda en ella, no se mueve una caja a ella ni es respuesta; por ella entra y sale la carga y gira con la bodega.
    /// </summary>
    public sealed class Order
    {
        public int Level;
        public Stage Stage;
        public int Hatches;
        /// <summary>Lo que hay en cada escotilla cuando el robot guarda (el objeto, o -1 si está vacía).</summary>
        public int[] Loaded;
        /// <summary>Las escotillas en el orden en que el robot guarda.</summary>
        public int[] StoreOrder;
        public Move[] Moves;
        /// <summary>Lo que hay en cada escotilla al terminar las cajas (con lo que se pide).</summary>
        public int[] Final;
        /// <summary>Posiciones que gira la bodega: 0 si no gira, si no ±2 o ±3 (positivo = en el sentido del reloj).</summary>
        public int Spin;
        /// <summary>Los objetos que se piden, uno por vez y en orden al azar (son todos los guardados).</summary>
        public int[] Asks;

        public int Objects => Asks.Length;

        /// <summary>La escotilla que de verdad tiene ese objeto ahora (después de las cajas); -1 si no está.</summary>
        public int HatchOf(int obj) => Array.IndexOf(Final, obj);

        /// <summary>Lo que hay en la escotilla antes de las cajas (para dibujar el guardado).</summary>
        public int LoadedOf(int hatch) => Loaded[hatch];

        /// <summary>El lugar del anillo (0 = esclusa, 1..Hatches = escotillas) que ocupa la escotilla DESPUÉS del giro.</summary>
        public int PositionAfterSpin(int hatch) => BodegaContract.Wrap(hatch + 1 + Spin, Hatches + 1);

        /// <summary>La escotilla (0..Hatches-1) que queda en ese lugar del anillo después del giro; -1 si es la esclusa.</summary>
        public int HatchAtPosition(int position)
        {
            int p = BodegaContract.Wrap(position - Spin, Hatches + 1);
            return p == 0 ? -1 : p - 1;
        }
    }

    /// <summary>
    /// Reglas puras de «Bodega de carga» (Memoria; ver docs/diseno-bodega-de-carga.md): el robot guarda objetos en las escotillas de una bodega redonda, a veces cambia una caja de lugar
    /// y a veces gira la bodega; después piden los objetos uno por uno y se toca la escotilla que lo tiene. Es memoria de ubicación (qué quedó dónde), el paradigma de los test de
    /// «asociación objeto-lugar». Sin dependencias de UnityEngine: se prueba con NUnit.
    /// </summary>
    public static class BodegaContract
    {
        public const string GameId = "bodega";
        public const int MaxLevel = 12;
        public const int ObjectCount = 12;
        public const int PrecisionOrders = 6;
        public const float RetoSeconds = 120f;
        /// <summary>Mayores: los tiempos de mirar (guardar, cajas, giro) se multiplican por esto.</summary>
        public const float SeniorSlow = 1.35f;
        /// <summary>Cuánto sube el rating por objeto encontrado al primer intento (con 5 objetos, un pedido perfecto sube una etapa y dos errores o más bajan una).</summary>
        public const float DdaStepUp = 0.2f;

        private static readonly Stage[] Stages =
        {
            new Stage { Number = 1,  Hatches = 6,  Objects = 2, Group = 1 },
            new Stage { Number = 2,  Hatches = 6,  Objects = 3, Group = 1 },
            new Stage { Number = 3,  Hatches = 8,  Objects = 3, Group = 2 },
            new Stage { Number = 4,  Hatches = 8,  Objects = 4, Group = 2 },
            new Stage { Number = 5,  Hatches = 8,  Objects = 4, Moves = 1, Group = 3 },
            new Stage { Number = 6,  Hatches = 8,  Objects = 5, Group = 3 },
            new Stage { Number = 7,  Hatches = 10, Objects = 5, Moves = 1, Group = 3 },
            new Stage { Number = 8,  Hatches = 8,  Objects = 4, Spin = true, Group = 4 },
            new Stage { Number = 9,  Hatches = 10, Objects = 5, Spin = true, Group = 4 },
            new Stage { Number = 10, Hatches = 10, Objects = 5, Moves = 1, Spin = true, Group = 5 },
            new Stage { Number = 11, Hatches = 10, Objects = 6, Moves = 1, Spin = true, Group = 5 },
            new Stage { Number = 12, Hatches = 10, Objects = 7, Moves = 2, Spin = true, Group = 5 },
        };

        public static Stage StageOf(int level) => Stages[Clamp(level) - 1];

        public static int Clamp(int level) => Math.Max(1, Math.Min(MaxLevel, level));

        /// <summary>El grupo de etapa de la pantalla final (1..5): pocos objetos, más escotillas, cajas que se mueven, la bodega gira y todo junto.</summary>
        public static int Group(int level) => StageOf(level).Group;

        public static int Wrap(int value, int modulo) => ((value % modulo) + modulo) % modulo;

        public static float Slow(bool senior) => senior ? SeniorSlow : 1f;

        // ------------------------------------------------------------------ el pedido

        /// <summary>Arma un pedido de esa etapa: objetos distintos nunca dos en una escotilla, las cajas siempre a una escotilla vacía, el giro de 2 o 3 posiciones y todos los objetos pedidos.</summary>
        public static Order Generate(int level, Random rng)
        {
            var st = StageOf(level);
            int n = st.Hatches;
            var objects = Shuffled(ObjectCount, rng);
            var places = Shuffled(n, rng);
            var loaded = new int[n];
            for (int i = 0; i < n; i++) loaded[i] = -1;
            var asks = new int[st.Objects];
            var store = new int[st.Objects];
            for (int i = 0; i < st.Objects; i++)
            {
                loaded[places[i]] = objects[i];
                asks[i] = objects[i];
                store[i] = places[i];
            }
            Shuffle(store, rng);                                  // el robot no guarda en el orden en que se pidió ni en el de las escotillas
            Shuffle(asks, rng);

            var final = (int[])loaded.Clone();
            var moves = new List<Move>();
            var movedObjects = new HashSet<int>();
            for (int m = 0; m < st.Moves; m++)
            {
                var from = new List<int>();
                var empty = new List<int>();
                for (int h = 0; h < n; h++)
                {
                    if (final[h] >= 0 && !movedObjects.Contains(final[h])) from.Add(h);
                    else if (final[h] < 0) empty.Add(h);
                }
                if (from.Count == 0 || empty.Count == 0) break;
                int a = from[rng.Next(from.Count)];
                int b = empty[rng.Next(empty.Count)];
                int obj = final[a];
                final[a] = -1;
                final[b] = obj;
                movedObjects.Add(obj);
                moves.Add(new Move { From = a, To = b, Obj = obj });
            }

            int spin = 0;
            if (st.Spin)
            {
                spin = 2 + rng.Next(2);
                if (rng.Next(2) == 0) spin = -spin;
            }
            return new Order { Level = Clamp(level), Stage = st, Hatches = n, Loaded = loaded, StoreOrder = store, Moves = moves.ToArray(), Final = final, Spin = spin, Asks = asks };
        }

        private static int[] Shuffled(int count, Random rng)
        {
            var a = new int[count];
            for (int i = 0; i < count; i++) a[i] = i;
            Shuffle(a, rng);
            return a;
        }

        private static void Shuffle(int[] a, Random rng)
        {
            for (int i = a.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                int t = a[i];
                a[i] = a[j];
                a[j] = t;
            }
        }

        // ------------------------------------------------------------------ tarjetas «NUEVO»

        /// <summary>Las tarjetas que esta etapa podría mostrar, en orden: la presentación, la caja que cambia de lugar y el giro (cada una solo si la etapa lo trae).</summary>
        public static List<Intro> IntrosFor(int level)
        {
            var st = StageOf(level);
            var list = new List<Intro> { Intro.Bodega };
            if (st.Moves > 0) list.Add(Intro.Mueve);
            if (st.Spin) list.Add(Intro.Gira);
            return list;
        }

        public static string[] IntroText(Intro intro)
        {
            switch (intro)
            {
                case Intro.Bodega: return new[] { "La carga entra por la esclusa dorada.", "El robot la guarda en las escotillas:", "mira bien dónde queda cada cosa." };
                case Intro.Mueve: return new[] { "Ahora el robot cambia una caja de lugar.", "Síguela con la vista:", "el objeto va adentro." };
                case Intro.Gira: return new[] { "¡La bodega gira!", "La esclusa gira con ella:", "úsala para orientarte." };
                default: return new string[0];
            }
        }

        // ------------------------------------------------------------------ objetos y textos

        /// <summary>Las 12 siluetas fáciles de nombrar, con el artículo para la pregunta («¿Dónde está el farol?»).</summary>
        public static readonly string[] ObjectNames = { "llave", "campana", "farol", "manzana", "hongo", "taza", "paraguas", "libro", "gema", "reloj de arena", "pluma", "bellota" };
        public static readonly string[] ObjectWithArticle = { "la llave", "la campana", "el farol", "la manzana", "el hongo", "la taza", "el paraguas", "el libro", "la gema", "el reloj de arena", "la pluma", "la bellota" };

        public static string Capitalized(int obj)
        {
            string s = ObjectWithArticle[obj];
            return char.ToUpperInvariant(s[0]) + s.Substring(1);
        }

        public static string AskText(int obj) => "¿Dónde está " + ObjectWithArticle[obj] + "?";

        public const string WatchTitle = "Mira dónde guarda cada cosa";
        public const string StoreLabel = "Llega a la bodega";
        public const string MoveTitle = "El robot cambia una caja de lugar";
        public const string MoveSub = "Síguela con la vista";
        public const string SpinTitle = "¡La bodega gira!";
        public const string SpinSub = "Fíjate dónde queda la esclusa";
        public const string AskLabel = "Pedido";
        public const string PerfectTitle = "¡Pedido perfecto!";
        public const string DoneTitle = "¡Pedido completo!";
        public const string EmptyLabel = "vacía";
        public const string TrayLabel = "Carro de reparto";

        public static string ObjectsLine(int objects) => objects + " objetos";

        public static string DoneLine(int firstTry, int total, bool record) => firstTry + " de " + total + " al primer intento" + (record ? " · ¡nuevo récord!" : "");

        public static string RecordLine(int best) => "Récord: " + best + " objetos";

        public static string StreakLine(int streak) => "Racha ×" + streak;

        /// <summary>El aviso de abajo al terminar un pedido con errores: dice cuántos fueron en ESE pedido (sin adjetivos que se contradigan con la tarjeta: la tarjeta ya dice «Pedido completo»).</summary>
        public static string ToastTitle(int errors) => errors == 1 ? "1 error en este pedido" : errors + " errores en este pedido";

        private static readonly string[] Tips =
        {
            "Truco: imagina el objeto dentro de su escotilla",
            "Truco: dile su nombre en voz baja al guardarlo",
            "Truco: escucha la nota de cada escotilla"
        };

        public static string ToastTip(int round, int errors) => Tips[Wrap(round + errors, Tips.Length)];

        public static IEnumerable<string> AllTips() => Tips;

        // ------------------------------------------------------------------ puntaje y medidas

        /// <summary>Puntaje 0-100: el porcentaje de objetos encontrados al primer intento.</summary>
        public static int Score(int firstTry, int total) => total <= 0 ? 0 : Math.Max(0, Math.Min(100, (int)Math.Round(100f * firstTry / total)));

        /// <summary>El récord de «tu bodega más grande»: solo sube con un pedido perfecto (sin errores).</summary>
        public static int NewRecord(int previousBest, int biggestPerfectToday) => Math.Max(Math.Max(0, previousBest), Math.Max(0, biggestPerfectToday));

        /// <summary>Nota (índice de la pentatónica) de cada escotilla: de la 1 a la 10 sube por la escala y de la 11 en adelante repite una octava arriba.</summary>
        public static int NoteIndex(int hatch) => hatch % 10;
        public static int NoteOctave(int hatch) => hatch >= 10 ? 2 : 1;
    }

    /// <summary>Las cuentas de una partida: encontrados al primer intento, la racha, el pedido perfecto más grande y la etapa más alta.</summary>
    public sealed class BodegaTally
    {
        public int Total { get; private set; }
        public int FirstTry { get; private set; }
        public int Streak { get; private set; }
        public int BestStreak { get; private set; }
        public int Orders { get; private set; }
        public int PerfectOrders { get; private set; }
        /// <summary>El pedido más grande (en objetos) sin ningún error.</summary>
        public int BiggestPerfect { get; private set; }
        public int PeakLevel { get; private set; } = 1;
        private long _ms;
        private int _timed;

        /// <summary>Un objeto encontrado: <paramref name="firstTry"/> = a la primera (sin tocar antes otra escotilla). Un error baja la racha a 0.</summary>
        public void Found(bool firstTry, int ms)
        {
            Total++;
            if (firstTry)
            {
                FirstTry++;
                Streak++;
                BestStreak = Math.Max(BestStreak, Streak);
            }
            else Streak = 0;
            if (ms >= 0) { _ms += ms; _timed++; }
        }

        /// <summary>Un error: la racha vuelve a 0 (el objeto se cuenta cuando se encuentra, ya sin ser «al primer intento»).</summary>
        public void BreakStreak() => Streak = 0;

        /// <summary>Un pedido terminado (todos sus objetos pedidos). El récord solo sube con los perfectos.</summary>
        public void EndOrder(int objects, int errors, int level)
        {
            Orders++;
            PeakLevel = Math.Max(PeakLevel, level);
            if (errors == 0)
            {
                PerfectOrders++;
                BiggestPerfect = Math.Max(BiggestPerfect, objects);
            }
        }

        public int Percent => Total == 0 ? 0 : (int)Math.Round(100f * FirstTry / Total);
        public int PeakGroup => BodegaContract.Group(PeakLevel);
        /// <summary>Milisegundos medios por objeto encontrado; -1 sin datos.</summary>
        public int MeanMs => _timed == 0 ? -1 : (int)(_ms / _timed);
    }
}
