package com.example

/**
 * Banderas de funciones que están escritas pero NO se ven (Tarea 149, 20-oct).
 */
object Features {
  /**
   * Las ligas (escudos, «Tu liga», la celebración de ascenso, compartir la liga, «trofeos») quedaron FUERA de la v1 (decisión de Ricardo, 19-oct): con `false` no se ve NADA de liga en la app y los avisos no hablan de trofeos.
   * El dato se sigue guardando (`RankTier`, rating por juego, trofeos, `LeagueEvents`) por si las ligas vuelven; solo se oculta lo que se ve. Si algún día se enciende, el escudo del avatar, «Tu liga» en Avance y Perfil, el ascenso
   * y el compartir de liga vuelven tal cual estaban (`LigasFueraDeLaVistaTest` vigila que cada sitio siga detrás de esta bandera).
   */
  const val LEAGUES = false
}
