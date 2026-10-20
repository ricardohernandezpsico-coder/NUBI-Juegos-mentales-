using System;
using UnityEngine;

namespace NeuroVida.Games.Anagramas
{
    /// <summary>
    /// Disposición de «En la punta de la lengua» en dp lógicos (360 de ancho; y desde arriba del área de juego, que incluye el marcador). Es la del boceto
    /// aprobado (docs/previews/punta-de-la-lengua-boceto.html: cielo de luceros, tarjeta, casillas, banco de fichas, ayudas, botones) pero ESTIRADA a la
    /// altura de cada teléfono: lo de arriba queda arriba, los botones abajo y las casillas y las fichas se reparten el aire. Pura, con pruebas.
    /// Las fichas miden ≥ 44 dp y se tocan con un radio de 27 dp (≥ 48 dp de blanco), los botones ≥ 58 dp.
    /// </summary>
    public static class PuntaLayout
    {
        public const float W = 360f;
        public const float SideMargin = 20f;
        /// <summary>Lo que ocupa el marcador común de los juegos (GameHud, 190 unidades = 63 dp) más un respiro.</summary>
        public const float HudDp = 66f;
        public const float CardH = 164f;
        public const float ButtonH = 58f;
        public const float ButtonsBottomMargin = 22f;
        public const float SkySpacing = 34f;
        public const float SkyRadius = 7f;
        public const float TileHitRadius = 27f;

        // ---- la bandeja del banco y la pregunta (Tarea 144, ítem 21 de la revisión del 19-oct: antes la pregunta flotaba a mitad de pantalla y el banco aparecía de golpe tras «¡La tengo!»)
        /// <summary>Aire entre el borde de abajo de la tarjeta y la pregunta «¿Cuál es la palabra?».</summary>
        public const float AskGap = 16f;
        /// <summary>Pozos de una fila de la bandeja: SIEMPRE cinco, para toda palabra. Las fichas del banco ocupan los pozos de izquierda a derecha, fila por fila.</summary>
        public const int BankCols = 5;
        /// <summary>El banco más grande que el juego puede armar: la palabra más larga del banco de palabras (10 letras) más el relleno más grande (4, nivel 5). Una prueba lo compara con los datos reales: si el banco de palabras o el relleno crecen, falla.</summary>
        public const int MaxBankTiles = 14;
        /// <summary>Filas de pozos que se ven antes de «¡La tengo!»: las que hacen falta para el banco más grande (<see cref="MaxBankTiles"/> / <see cref="BankCols"/>, redondeado hacia arriba).</summary>
        public const int TrayMaxRows = 3;
        /// <summary>
        /// Opacidad de la bandeja mientras se PIENSA la palabra (Tarea 144, ajuste de Ricardo: «tenues hasta ¡La tengo!»): un lugar que espera, no algo que llenar ni las letras de la palabra; ~45 % (entre 40 y 50). Con «¡La tengo!» pasa a firme
        /// (1,0) en <see cref="TrayFirmSeconds"/> mientras caen las fichas; con «quitar animaciones», de golpe.
        /// </summary>
        public const float TrayDimAlpha = 0.45f;
        public const float TrayFirmSeconds = 0.2f;
        /// <summary>1 cuando las fichas ya llegaron (con «¡La tengo!»), 0 mientras se piensa.</summary>
        public static float TrayFirmTarget(bool tilesCame) => tilesCame ? 1f : 0f;
        /// <summary>La opacidad de la bandeja según qué tan firme está (0 = tenue, 1 = firme).</summary>
        public static float TrayAlpha(float firm) => TrayDimAlpha + (1f - TrayDimAlpha) * Math.Max(0f, Math.Min(1f, firm));
        /// <summary>
        /// Cuántos pozos tiene la bandeja antes de «¡La tengo!»: LA MISMA cantidad para toda palabra (la cantidad no puede decir cuántas letras tiene: eso es la ayuda 1 de «Una ayuda» y de ella depende lo que mide «sola»)
        /// y suficiente para el banco más grande (<see cref="MaxBankTiles"/>): nunca falta un pozo. Cuando caen las fichas, los pozos sin ficha se desvanecen y la bandeja se achica a las filas que usa.
        /// </summary>
        public const int TrayPockets = BankCols * TrayMaxRows;
        /// <summary>Aire de la bandeja alrededor de sus casillas.</summary>
        public const float TrayPad = 10f;

        // ---- ronda guiada del tutorial: dónde cabe «Saltar tutorial» (docs/tutoriales-con-nubi.md, «Punta»)
        /// <summary>Fichas de la ronda guiada: seis como mucho. Con siete el banco pasa a DOS filas y, en una pantalla corta, el hueco de una ficha de la fila de abajo pisa «Saltar tutorial».</summary>
        public const int GuidedMaxTiles = 6;
        /// <summary>El hueco del foco sobre una ficha mide esto por la ficha (paso «Toca las letras en orden»).</summary>
        public const float GuidedHoleScale = 1.7f;
        /// <summary>Lo que el hueco sobresale de ese cuadrado (el borde de NubiCoach, 12 u = 4 dp) más lo que flota la ficha (3 dp).</summary>
        public const float GuidedHoleSlack = 7f;
        /// <summary>«Saltar tutorial» mide 132 unidades del lienzo = 44 dp (GuidedTutorial).</summary>
        public const float SkipH = 44f;
        /// <summary>Altura del rótulo «Ayudas» (con su letra a 16 dp desde la Tarea 128): la mitad cae sobre LadderY.</summary>
        public const float LadderHalfH = 11f;
        /// <summary>Aire que la ronda guiada deja entre el banco y la escalera de ayudas: «Saltar tutorial» (44 dp) con ~9 dp de cada lado y lo que el hueco sobresale de la ficha.</summary>
        public const float GuidedReserve = 64f;

        /// <summary>Las fichas de la ronda guiada: de 4 letras en adelante y, con las de relleno del nivel 1, sin pasar de <see cref="GuidedMaxTiles"/> (una sola fila de banco).</summary>
        public static bool FitsGuidedBank(string tiles, int extraLetters) =>
            tiles != null && tiles.Length >= 4 && tiles.Length + extraLetters <= GuidedMaxTiles;

        public struct Metrics
        {
            public float SkyY, CardTop, CardBottom, SayY, SlotY, BankFirstY, LadderY, ButtonsTop;
            /// <summary>Dónde va el centro de la pregunta «¿Cuál es la palabra?»: pegada bajo la tarjeta (<see cref="AskGap"/>), salvo en la ronda guiada, donde queda donde estaba (en el lugar de las casillas).</summary>
            public float AskY;
            /// <summary>true = el banco es una cuadrícula fija de <see cref="BankCols"/> columnas dentro de una bandeja (todo menos la ronda guiada, que conserva su fila centrada).</summary>
            public bool FixedGrid;
            public float TileD, RowGap, TileSpacing;
            public int PerRow, Rows;
        }

        /// <param name="extraGap">dp de aire que se suman entre la tarjeta y las casillas (el mensaje de Nubi en el tutorial va ahí y no tapa nada).</param>
        /// <param name="reserveBelowBank">dp que se dejan libres entre el banco de fichas y la escalera de ayudas (la ronda guiada pasa <see cref="GuidedReserve"/>: ahí va «Saltar tutorial»).</param>
        /// <param name="minCardTop">Lo más arriba que puede quedar la tarjeta (dp): en la ronda guiada, bajo la fila fija del tutorial («Práctica» y «Saltar», Tarea 127), donde el cielo ya no se ve. 0 = sin tope.</param>
        public static Metrics Compute(float height, int tileCount, float extraGap = 0f, float reserveBelowBank = 0f, float minCardTop = 0f)
        {
            var m = new Metrics();
            m.SkyY = HudDp + 18f;
            m.CardTop = Math.Max(m.SkyY + 20f, minCardTop);
            m.CardBottom = m.CardTop + CardH;
            m.SayY = m.CardBottom + 17f;
            m.ButtonsTop = height - ButtonsBottomMargin - ButtonH;
            m.LadderY = m.ButtonsTop - 22f;

            int count = Math.Max(1, tileCount);
            m.FixedGrid = reserveBelowBank <= 0f && minCardTop <= 0f;              // la ronda guiada (con su aire reservado y su fila fija) conserva el banco de siempre
            m.Rows = count <= 5 ? 1 : (int)Math.Ceiling(count / 6.0);
            m.PerRow = m.FixedGrid ? BankCols : (int)Math.Ceiling(count / (double)m.Rows);
            m.TileSpacing = Math.Min(56f, (W - 2f * SideMargin) / m.PerRow);
            m.TileD = Math.Min(46f, m.TileSpacing - 6f);
            m.RowGap = m.TileD + 14f;
            m.AskY = m.CardBottom + AskGap + 14f;

            float slotY = m.CardBottom + 66f + extraGap;
            float bankY = slotY + 80f;
            int fitRows = m.FixedGrid ? TrayMaxRows : m.Rows;                        // la bandeja mide siempre sus tres filas de pozos hasta «¡La tengo!»: nada de lo que se ve depende de la palabra
            float bankBottom = bankY + (fitRows - 1) * m.RowGap + m.TileD / 2f;
            float limit = m.LadderY - 16f - reserveBelowBank;
            float trayPad = m.FixedGrid ? TrayPad : 0f;                               // el borde de abajo de la bandeja también tiene que quedar sobre las ayudas
            bool centred = false;
            if (m.FixedGrid && bankBottom + trayPad <= limit)
            {
                // la bandeja (con sus tres filas) va al centro del aire libre entre la pregunta y las ayudas; las casillas de la palabra, 80 dp sobre su primera fila (aparecen con la ayuda 1)
                float top = m.AskY + 14f + 22f + extraGap;
                float trayH = (fitRows - 1) * m.RowGap + m.TileD + 2f * TrayPad;
                float room = (limit - top) - trayH;
                if (room >= 0f)
                {
                    float firstRow = top + room * 0.5f + TrayPad + m.TileD / 2f;
                    float slotMin = m.AskY + 14f + 16f + 23f + extraGap;           // las casillas de la palabra nunca suben sobre la pregunta
                    firstRow = Math.Max(firstRow, slotMin + 80f);
                    if (firstRow + (fitRows - 1) * m.RowGap + m.TileD / 2f + TrayPad <= limit + 0.01f)
                    {
                        slotY = firstRow - 80f;
                        bankY = firstRow;
                        centred = true;
                    }
                }
            }
            if (centred) { }
            else if (bankBottom > limit)
            {
                // pantalla bajita: las filas se acercan un poco (nunca por debajo de la ficha + 6 dp)
                float need = bankBottom - limit;
                float room = (fitRows - 1) * (m.RowGap - (m.TileD + 6f));
                float cut = fitRows > 1 ? Math.Min(need, room) : 0f;
                if (fitRows > 1) m.RowGap -= cut / (fitRows - 1);
                bankBottom -= cut;
                if (bankBottom > limit) { slotY -= (bankBottom - limit); bankY -= (bankBottom - limit); }
            }
            else
            {
                // pantalla alta: el aire que sobra se reparte (las casillas y las fichas bajan un poco; la tarjeta y el cielo no se mueven)
                float free = limit - bankBottom;
                float shift = free * 0.38f;
                slotY += shift * 0.55f;
                bankY += shift;
            }
            m.SlotY = slotY;
            m.BankFirstY = bankY;
            if (!m.FixedGrid) m.AskY = slotY;                                        // la ronda guiada: la pregunta donde estaba
            return m;
        }

        /// <summary>El ancho de la bandeja del banco (seis casillas y su aire).</summary>
        public static float TrayWidth(Metrics m) => (BankCols - 1) * m.TileSpacing + m.TileD + 2f * TrayPad;

        /// <summary>El alto de la bandeja con <paramref name="rows"/> filas de pozos (puede ser fraccionario mientras se achica).</summary>
        public static float TrayHeight(Metrics m, float rows) => (Math.Max(1f, rows) - 1f) * m.RowGap + m.TileD + 2f * TrayPad;

        /// <summary>El centro de la bandeja (y en dp desde arriba): la primera fila de pozos queda arriba y las demás debajo; la bandeja se achica por abajo.</summary>
        public static float TrayCenterY(Metrics m, float rows) => m.BankFirstY - m.TileD / 2f - TrayPad + TrayHeight(m, rows) / 2f;

        /// <summary>Las filas de pozos que usa un banco de <paramref name="tiles"/> fichas (al menos una).</summary>
        public static int TrayRowsFor(int tiles) => Math.Max(1, Math.Min(TrayMaxRows, (int)Math.Ceiling(tiles / (double)BankCols)));

        /// <summary>
        /// Dónde va «Saltar tutorial» en la ronda guiada: el centro de la franja libre entre lo más bajo que ilumina el foco de una ficha del banco y lo más alto del rótulo «Ayudas»,
        /// en dp desde el borde de ABAJO. Sale de la disposición (no de un porcentaje de la altura): así ninguna palabra ni ninguna pantalla lo deja sobre las fichas.
        /// Vale para un banco de una sola fila (hasta <see cref="GuidedMaxTiles"/> fichas, calculado con <c>reserveBelowBank = GuidedReserve</c>) y pantallas desde 640 dp (1080x1920).
        /// </summary>
        public static float GuidedSkipFromBottom(Metrics m)
        {
            float height = m.ButtonsTop + ButtonH + ButtonsBottomMargin;
            float top = m.BankFirstY + (m.Rows - 1) * m.RowGap + GuidedHoleScale * m.TileD / 2f + GuidedHoleSlack;   // lo más bajo que ilumina el foco del banco
            float bottom = m.LadderY - LadderHalfH;                                                                   // lo más alto de «Ayudas»
            return height - (top + bottom) / 2f;
        }

        /// <summary>Separación entre casillas: 46 dp como mucho, y se aprieta con las palabras largas para que quepan en el ancho.</summary>
        public static float SlotSpacing(int n) => Math.Min(46f, (W - 20f) / Math.Max(1, n));

        public static Vector2 SlotPos(Metrics m, int k, int n)
        {
            float sp = SlotSpacing(n);
            return new Vector2(W / 2f + (k - (n - 1) / 2f) * sp, m.SlotY);
        }

        /// <summary>Tamaño al que se achica una ficha colocada para que no pise a la vecina (1 = sin achicar).</summary>
        public static float PlacedScale(Metrics m, int n) => Math.Min(1f, (SlotSpacing(n) + 2f) / m.TileD);

        /// <summary>El lugar de la ficha k del banco (dos o tres filas centradas, como en el boceto).</summary>
        public static Vector2 BankHome(Metrics m, int k, int count)
        {
            if (m.FixedGrid)
            {
                // la bandeja: seis casillas por fila, las fichas las llenan de izquierda a derecha y de arriba abajo
                int r = k / BankCols, c = k % BankCols;
                return new Vector2(W / 2f + (c - (BankCols - 1) / 2f) * m.TileSpacing, m.BankFirstY + r * m.RowGap);
            }
            int perRow = m.PerRow;
            int rowsUsed = Math.Max(1, (int)Math.Ceiling(count / (double)perRow));
            int row = Math.Min(rowsUsed - 1, k / perRow);
            int idx = k - row * perRow;
            int cnt = row == rowsUsed - 1 ? count - row * perRow : perRow;
            float x = W / 2f - (cnt - 1) * m.TileSpacing / 2f + idx * m.TileSpacing;
            return new Vector2(x, m.BankFirstY + row * m.RowGap);
        }

        /// <summary>El lucero k de la fila del cielo: <paramref name="slots"/> en total (8 en Precisión; en el Reto las palabras jugadas más la que sigue).</summary>
        public static Vector2 SkyPos(Metrics m, int k, int slots)
        {
            float sp = Math.Min(SkySpacing, (W - 2f * SideMargin) / Math.Max(1, slots));
            return new Vector2(W / 2f + (k - (slots - 1) / 2f) * sp, m.SkyY);
        }
    }
}
