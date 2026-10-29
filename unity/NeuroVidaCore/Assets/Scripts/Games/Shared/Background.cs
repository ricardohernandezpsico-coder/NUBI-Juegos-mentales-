using System;
using System.Collections;
using System.Threading;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// Hilos de trabajo para el horneo (Tarea 190, 28-oct): el arte procedural de los juegos (<see cref="ClayRaster"/> y los <c>Bake</c> de cada juego) calcula un pixel a la vez con funciones de distancia, y el trabajo de un solo sprite grande llegaba a 0,3 a 3 s en el hilo principal. Todo lo de acá es
    /// ARITMÉTICA pura: ninguna API de Unity (la <c>Texture2D</c> y el <c>Sprite</c> los crea siempre el hilo principal con lo que los hilos calcularon). Los pixeles salen IDÉNTICOS a los de un solo hilo (cada fila es independiente y se calcula con el mismo código; la prueba <c>SpriteThreadsTests</c> lo mide).
    /// <para><see cref="ForRows"/> reparte las filas de un sprite entre los hilos de trabajo y el que llama (espera a que terminen): sirve a la carga, que la pantalla de la app tapa. <see cref="Run"/> y <see cref="RunEnumerator"/> calculan algo EN SEGUNDO PLANO y el hilo principal solo pregunta <see cref="Handle.IsDone"/> cada cuadro (el módulo siguiente de Acoplamiento, que se hornea mientras se juega).</para>
    /// </summary>
    public static class Background
    {
        /// <summary>false = todo en el hilo que llama, una fila tras otra (las pruebas lo apagan para comparar los pixeles con los del cálculo repartido).</summary>
        public static bool Parallel = true;

        /// <summary>Cuántos hilos reparten un horneo: uno menos que los núcleos (entre 1 y 4). <see cref="Environment.ProcessorCount"/> se puede leer desde cualquier hilo.</summary>
        public static int Workers
        {
            get
            {
                int n = Environment.ProcessorCount - 1;
                return n < 1 ? 1 : n > 4 ? 4 : n;
            }
        }

        /// <summary>Por debajo de estos pixeles un sprite se calcula en un solo hilo: repartirlo costaría más que calcularlo.</summary>
        public const int MinParallelPixels = 16384;

        /// <summary>Lo que se está calculando en segundo plano: el hilo principal pregunta <see cref="IsDone"/> sin esperar.</summary>
        public sealed class Handle
        {
            private volatile bool _done;
            private Exception _error;
            public bool IsDone => _done;
            /// <summary>El error del hilo de trabajo, si lo hubo (null si salió bien).</summary>
            public Exception Error => _error;
            internal void Finish(Exception error) { _error = error; _done = true; }
        }

        /// <summary>Calcula <paramref name="work"/> en un hilo de trabajo; el que llama no espera.</summary>
        public static Handle Run(Action work)
        {
            var handle = new Handle();
            if (!Parallel)
            {
                Exception e0 = null;
                try { work(); } catch (Exception e) { e0 = e; }
                handle.Finish(e0);
                return handle;
            }
            ThreadPool.QueueUserWorkItem(_ =>
            {
                Exception error = null;
                try { work(); } catch (Exception e) { error = e; }
                handle.Finish(error);
            });
            return handle;
        }

        /// <summary>
        /// Una rutina de horneo que cede entre trozos, pero que en cada cuadro avanza VARIOS trozos hasta <paramref name="budgetMs"/> milisegundos en vez de uno solo (Tarea 190). Se usa en lo que corre DETRÁS de la pantalla de carga de la app (nadie ve esos cuadros): con un trozo por cuadro la carga esperaba
        /// cuadros casi vacíos (uno por trozo, 17 ms cada uno). Los pixeles y los clips que salen son los mismos; solo cambia cuántos trozos entran en un cuadro.
        /// </summary>
        public static IEnumerator Budgeted(IEnumerator routine, float budgetMs = 25f)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            while (true)
            {
                watch.Restart();
                bool more;
                do { more = routine.MoveNext(); } while (more && watch.Elapsed.TotalMilliseconds < budgetMs);
                if (!more) yield break;
                yield return null;
            }
        }

        /// <summary>Corre de punta a punta una rutina de horneo (las de <c>DockingArt</c> escriben sus pixeles en un arreglo y ceden cada tanto) en un hilo de trabajo, sin esperar los cuadros: los <c>yield</c> se ignoran.</summary>
        public static Handle RunEnumerator(IEnumerator routine) => Run(() => { while (routine.MoveNext()) { } });

        /// <summary>
        /// Calcula las filas <c>0 .. rows - 1</c> repartidas entre los hilos de trabajo y el que llama, y espera a que terminen todas. Cada fila es independiente: <paramref name="row"/> tiene que escribir SOLO en su fila (y leer datos que nadie cambia), así el resultado es el mismo que fila tras fila.
        /// Con pocas filas, o con <see cref="Parallel"/> apagado, las corre una tras otra en el hilo que llama.
        /// </summary>
        public static void ForRows(int rows, Action<int> row, int pixels = int.MaxValue)
        {
            int workers = Parallel ? Workers : 1;
            if (workers <= 1 || rows < workers * 4 || pixels < MinParallelPixels)
            {
                for (int i = 0; i < rows; i++) row(i);
                return;
            }
            int next = -1;
            Exception error = null;
            void Loop()
            {
                try
                {
                    int i;
                    while ((i = Interlocked.Increment(ref next)) < rows) row(i);
                }
                catch (Exception e)
                {
                    Interlocked.CompareExchange(ref error, e, null);
                    Interlocked.Exchange(ref next, rows);                          // que los demás hilos paren
                }
            }
            using (var done = new CountdownEvent(workers - 1))
            {
                for (int w = 0; w < workers - 1; w++)
                    ThreadPool.QueueUserWorkItem(_ => { try { Loop(); } finally { done.Signal(); } });
                Loop();                                                            // el que llama también trabaja
                done.Wait();
            }
            if (error != null) throw new AggregateException(error);
        }
    }
}
