# Rumbo a Casa (`rumbo`) — ficha técnica

> Ficha técnica movida TAL CUAL desde `CLAUDE.md` el 2-oct (CLAUDE.md quedó como índice). Lo que manda es el código; esta ficha explica cómo y por qué.

**Rumbo a Casa** (`Games/Rumbo/`, id `rumbo`, dominio MEMORIA; juego estrella de orientación, 28-sep, elegido por
Ricardo entre varias propuestas: "calidad, enganche, teoría y que se diferencie del mercado"): INTEGRACIÓN DE TRAYECTO,
volver al punto de partida sin verlo (tarea de completar el triángulo: Klatzky et al., 1990; Loomis et al., 1993;
células de red, Hafting et al., 2005; realidad virtual: Howett et al., 2019; Sea Hero Quest, Coutrot et al., 2018). Ni
Lumosity, Peak, Elevate ni NeuroNation tienen algo así. En la app NO se nombran enfermedades.
- Vista de CABINA: la nave fija un poco abajo del centro, mirando hacia arriba; el mundo (`_pivot` gira/escala,
  `_content` lleva las coordenadas del mapa) gira y pasa alrededor. Polvo de estrellas propio (150 puntos que se
  envuelven alrededor de la cámara) = flujo óptico; niebla redonda (`HomingSprites.Fog`: se ve hasta 380, nada desde
  520). Fondo `GameWorld.DeepSpace` SIN estrellas (serían una brújula).
- Ida: aparece la SEÑAL del próximo cristal en el borde de la vista (anillo del color del cristal); se toca (o sola a
  los 8 s), la nave gira hacia él (80 → 150°/s) y vuela a velocidad FIJA (300 u/s, igual en ida y vuelta: el paso del
  polvo mide la distancia). 2 → 5 tramos (el primero sale derecho de la base). Vuelta: "¿Hacia dónde está casa?"
  (dial con 12 marcas que gira con la nave + flecha sol; tocar/arrastrar) → FIJAR RUMBO → la nave avanza → ¡AQUÍ!
  (si pasa 2,2 × la distancia: "Sin combustible"). Revelación: la cámara se aleja hasta el mapa con el norte arriba;
  ida lima, vuelta sol, vuelta justa crema, tramo que faltó; ✓/✗ + "Rumbo: 14° a la derecha · distancia justa".
- **Faro** en la mitad de los viajes (de a pares, orden al azar): estrella sol lejanísima que solo cambia de lugar al
  girar (en el infinito); da el rumbo, no la posición. Así el final compara con faro / sin faro.
- Reglas y pruebas: `HomingContract` / `HomingContractTests` (7). `NextTrip` acepta el viaje si la vuelta mide 820-2600,
  la ruta no vuelve cerca de la base tras la primera parada (600), las paradas quedan separadas y no se cruza.
  `Evaluate` separa rumbo y distancia. Llegada = a ≤ 35% de la distancia a casa; perfecta ≤ 12%. DDA `stepUp` 0.5 por
  viaje, sin tiempo de reacción. 10 niveles. Reto 150 s (termina el viaje en curso); Precisión 8 viajes. 60 cuadros/s.
- Medidas: **tu brújula interna** ("a X% de casa") y **tus llegadas** (diana alrededor de la base: encima = te
  pasaste, debajo = corto, a los lados = rumbo). Telemetría `homing_error_pct / homing_along / homing_lateral /
  homing_beacon / homing_perfect` → `GamePlayResult.homing*` → `GameResultScreen` (`HomingTarget`); la lectura
  (`data/Homing.kt`, 5 pruebas): desvío medio del rumbo, distancia (justa / corto / largo / varía, desde 4 viajes y
  70% para el mismo lado), qué aleja más de casa (rumbo o distancia, 1,5 veces y 5 puntos) con un truco, y faro vs
  sin faro (3+ de cada uno, 8° de diferencia).
- Sonido `HomingSounds` (identidad de la app): la ida sube nota a nota (un cristal = una nota) y la llegada "vuelve a
  do": justo en casa, acorde completo con destellos; cerca, do y sol; lejos, acorde en suspenso. Ping de sonar con eco
  para la señal, soplo al girar, madera al fijar rumbo; vuelo con el colchón de `TrafficSounds.EngineLoop`.
- Arte: `HomingSprites` (nave con y sin sombra, base en anillo con casita, 5 cristales, faro, flecha, dial, niebla).
  Vista previa: `python3 tools/art-preview/rumbo.py <raw>` → `docs/previews/rumbo.png`; sonidos →
  `docs/previews/rumbo-sonidos.wav` y `rumbo-sonidos-llegadas.wav` (las tres llegadas). Sin probar en el teléfono:
  revisar si marea el giro, si se entiende tocar la señal y el dial, y el ritmo (~25 s por viaje).
