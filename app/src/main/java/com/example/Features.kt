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

  /**
   * Comparar con otras personas (Tarea 159, Ricardo, 22-oct): SOLO al final de una partida en modo Desafío, con la marca del juego en su unidad (nunca los puntos), contra personas reales del mismo juego, modo, reloj y franja de edad, y nunca como percentil. En el resto de la app, solo contigo.
   * Necesita servidor, cuentas, consentimiento y la revisión del abogado (regla permanente 3): mientras tanto queda APAGADA y no se ve nada. Lo único que existe hoy es que cada medida guardada ([MeasurePoint.mode]) anota el modo en que se jugó, para que el día que haya servidor no falten datos.
   */
  const val COMPARE = false
}
