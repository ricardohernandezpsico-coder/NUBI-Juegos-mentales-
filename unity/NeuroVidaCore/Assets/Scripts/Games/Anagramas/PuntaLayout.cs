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

        public struct Metrics
        {
            public float SkyY, CardTop, CardBottom, SayY, SlotY, BankFirstY, LadderY, ButtonsTop;
            public float TileD, RowGap, TileSpacing;
            public int PerRow, Rows;
        }

        /// <param name="extraGap">dp de aire que se suman entre la tarjeta y las casillas (el mensaje de Nubi en el tutorial va ahí y no tapa nada).</param>
        public static Metrics Compute(float height, int tileCount, float extraGap = 0f)
        {
            var m = new Metrics();
            m.SkyY = HudDp + 18f;
            m.CardTop = m.SkyY + 20f;
            m.CardBottom = m.CardTop + CardH;
            m.SayY = m.CardBottom + 17f;
            m.ButtonsTop = height - ButtonsBottomMargin - ButtonH;
            m.LadderY = m.ButtonsTop - 22f;

            int count = Math.Max(1, tileCount);
            m.Rows = count <= 5 ? 1 : (int)Math.Ceiling(count / 6.0);
            m.PerRow = (int)Math.Ceiling(count / (double)m.Rows);
            m.TileSpacing = Math.Min(56f, (W - 2f * SideMargin) / m.PerRow);
            m.TileD = Math.Min(46f, m.TileSpacing - 6f);
            m.RowGap = m.TileD + 14f;

            float slotY = m.CardBottom + 66f + extraGap;
            float bankY = slotY + 80f;
            float bankBottom = bankY + (m.Rows - 1) * m.RowGap + m.TileD / 2f;
            float limit = m.LadderY - 16f;
            if (bankBottom > limit)
            {
                // pantalla bajita: las filas se acercan un poco (nunca por debajo de la ficha + 6 dp)
                float need = bankBottom - limit;
                float room = (m.Rows - 1) * (m.RowGap - (m.TileD + 6f));
                float cut = m.Rows > 1 ? Math.Min(need, room) : 0f;
                if (m.Rows > 1) m.RowGap -= cut / (m.Rows - 1);
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
            return m;
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
