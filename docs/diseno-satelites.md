# Satélites: «enciende tu planeta» (renovación, 9-oct-2026)

Boceto aprobado por Ricardo el 9-oct: `docs/previews/satelites-orbitas-boceto.html`
(artifact https://claude.ai/artifact/1jEfQQFgViMb3fqQmP19Cp, versión 2). Mismo id `satelites`, mismo dominio (Atención).

## 1. Por qué se renueva

Ricardo probó la versión anterior el 9-oct:
- **Le gustó:** «la idea me gusta bastante», «se entiende bien, sin explicación».
- **El problema:** «se hace largo y monótono… cuando juego tres rondas, después ya no me dan ganas de seguir».

La auditoría externa del 8-oct lo marcaba como «pulir y probar; candidato a retirar si no engancha».

Las causas, a la vista en el código y en las capturas:
- 5 a 8 s de mirar pasivo por ronda;
- rebotes al azar, siempre iguales;
- ninguna recompensa visible entre rondas;
- un tercio de pantalla vacío en 20:9.

## 2. La idea

Tu planeta está a oscuras. Algunos satélites traen un mensaje.
1. Se encienden (aro dorado y sobre).
2. Se apagan, y todos giran alrededor del planeta.
3. Cuando se detienen, tocas los que traían mensaje.

Cada mensaje entregado vuela al planeta y **enciende una luz**. Al final, el planeta aparece grande e iluminado con todo lo que seguiste.

## 3. Base científica (PubMed, con DOI)

- **Seguimiento de múltiples objetos (MOT):**
  - Pylyshyn y Storm 1988, doi:10.1163/156856888X00122.
  - La capacidad depende de la velocidad: Alvarez y Franconeri 2007, doi:10.1167/7.13.14.
- **Mayores:**
  - El rendimiento de seguimiento cae con la edad, y los distractores parecidos a los objetivos interfieren más: Störmer et al. 2011, doi:10.1167/11.2.1.
  - Los mayores aprenden igual que los jóvenes con práctica de seguimiento: Legault, Allard y Faubert 2013, doi:10.3389/fpsyg.2013.00323.
- **Oclusión:** seguir objetos que se esconden un rato es posible aunque más difícil, y el efecto depende de la carga: Lukavský, Oksama y Děchtěrenko 2022, doi:10.1177/17470218221142463. Es la base de la «nube de polvo».
- **Transferencia a la vida diaria:** baja (revisión crítica de Vater et al. 2021, doi:10.3758/s13423-021-01892-2). No se promete nada.

## 4. Reglas exactas

- **Ronda:** aviso de sorpresa, si hay (1,7 s; 2,6 s la primera vez, con la etiqueta «NUEVO») → listo 0,45 s → **presentación 1,8 s** → **seguimiento** (tabla) → **respuesta** → **revelación** 1,9 s (+0,5 s si hay «¿Aquí se cruzaron?»).
- **Presentación:** k satélites con aro dorado y sobre arriba; la señal se desvanece en los últimos 300 ms.
- **Seguimiento:** sin ninguna respuesta posible (ver §9).
  - Se detienen al cumplir el tiempo **solo si** la distancia mínima entre satélites es ≥ 38 dp y ninguno está bajo la nube.
  - Si no se cumple, siguen hasta 0,9 s más.
- **Respuesta:** tocar marca o desmarca (aro celeste); toque al satélite más cercano dentro de 30 dp.
  - Al marcar el k-ésimo se evalúa 0,55 s después; ese margen permite corregir el último.
- **Acierto de la ronda** = todos los k. El motor común recibe un ensayo por ronda.
- **Partida:** Precisión = 8 rondas; Reto = 120 s (como hoy).

## 5. Movimiento: órbitas planas

- **Anillos:** tres anillos elípticos alrededor del planeta, más altos que anchos.
  - Ancho del más externo = `campo/2 − (radio + paneles) − 8 dp`, para que nunca se salga.
  - Alto = 1,42 × ancho, ajustado a la altura disponible: en 20:9 se usa toda la pantalla.
- **Por satélite:** carril propio (radio × 1 ± 6 %) y velocidad propia (media del nivel × 0,75–1,25), así se adelantan.
- **Sentido:** por anillo en los niveles 1-2; mezclado dentro del anillo desde el 3 (**se cruzan de frente**).
- **Media vuelta** (desde el nivel 6): ~35 % de los satélites invierte el sentido una vez, con frenado suave de 0,5 s.
- **Reparto inicial:** distancia mínima de 46 dp entre satélites.
- **Movimiento puro con semilla**, probado sin Unity (como `SatelliteSwarm` hoy).

## 6. Niveles (12)

| Nivel | Seguir de total | Velocidad media (dp/s) | Seguimiento (s) | Sorpresas posibles | Frente | Media vuelta |
|---|---|---|---|---|---|---|
| 1 | 2 de 6 | 55 | 3,5 | — | — | — |
| 2 | 3 de 6 | 58 | 3,8 | órbita | — | — |
| 3 | 3 de 7 | 64 | 4,0 | órbita | sí | — |
| 4 | 3 de 8 | 70 | 4,2 | órbita, nube | sí | — |
| 5 | 4 de 8 | 74 | 4,4 | órbita, nube | sí | — |
| 6 | 4 de 9 | 80 | 4,6 | órbita, nube, rápida | sí | sí |
| 7 | 4 de 10 | 88 | 4,8 | las tres | sí | sí |
| 8 | 4 de 10 | 96 | 5,0 | las tres | sí | sí |
| 9 | 5 de 11 | 100 | 5,2 | las tres | sí | sí |
| 10 | 5 de 11 | 108 | 5,4 | las tres | sí | sí |
| 11 | 5 de 12 | 116 | 5,7 | las tres | sí | sí |
| 12 | 5 de 13 | 126 | 6,0 | las tres | sí | sí |

Las velocidades están en dp/s sobre un campo de 360 dp de ancho.

## 7. Sorpresas

Se anuncian SIEMPRE antes de la presentación, nunca durante el seguimiento. Una de cada dos rondas, desde la 2.ª, y nunca la misma dos veces seguidas.

- **Cambio de órbita:** 2 a 4 satélites, al menos uno con mensaje, saltan al anillo vecino en 0,9 s, repartidos en el seguimiento.
- **Nube de polvo:** una nube plana y opaca (≈160 × 125 dp) cruza el campo de lado a lado y sale antes del final. Lo que pasa debajo no se ve.
- **Órbitas rápidas:** velocidad × 1,25 y seguimiento × 0,8.

## 8. Pantalla, sonido y accesibilidad

- **Arriba:** título, «Ronda N de 8 · Nivel N», racha (desde ×2) e instrucción de 1-2 líneas que cambia por fase:
  - «Estos 3 traen un mensaje / Síguelos con la vista»;
  - «Síguelos con la vista» (+ la pista de la sorpresa);
  - «Toca los 3 que traían mensaje / 2 de 3 marcados · toca otra vez para quitar».
- **Al centro:** el planeta (radio ≈ 44 dp) con continentes tenues y las **luces** encendidas, con un halo que crece con la cantidad.
- **Abajo:** 8 discos de rondas (lleno = perfecta; medio = parcial; con forma, no solo color) y «Luces encendidas: N».
- **Revelación:**
  - acierto: ✓ verde;
  - marcado sin mensaje: **aspa diagonal** coral (no cruz recta, por `simbolos-neutros.md`);
  - con mensaje no marcado: aro coral punteado;
  - si hubo confusión, «¿Aquí se cruzaron?» en el punto de mayor cercanía (`ClosestApproach`, ya existe);
  - los sobres de los aciertos vuelan al planeta y encienden luces;
  - un aviso «¡3 de 3!» o «2 de 3», y «Racha ×N» desde 3.
- **Final:**
  - planeta grande con sus luces y «Encendiste N luces»;
  - «Tu seguimiento: X de Y a la vez» (medida actual, `TrackedEstimate` corregido por azar);
  - «Rondas perfectas: N de 8 · racha mayor ×N»;
  - la nota común «Medida de esta partida. No es un diagnóstico.»;
  - récord de luces en una partida, en las preferencias nuevas `satelites_record` (respaldadas como `correo_record`).
- **Sonido:**
  - campanas pentatónicas, una por satélite con mensaje, en la presentación;
  - zumbido suave durante el seguimiento;
  - «clac» al detenerse;
  - nota al marcar;
  - arpegio al acertar;
  - destello al encenderse cada luz.
- **«Quitar animaciones»:** los satélites se siguen moviendo (es la tarea), pero sin pulso del aro, sin partículas, sin arco de vuelo de los sobres y con luces fijas.
- **Accesibilidad:**
  - la señal es aro + sobre, y la marca es otro aro: forma, no solo color;
  - textos ≥ 14 dp;
  - toque de 60 dp de diámetro;
  - satélite de 28 dp más paneles.

## 9. Patentes y nombre

Revisión de patentes del 9-oct (informe externo, fuera del repo): **riesgo BAJO**.
- **NeuroTracker/Cognisens:** las vigentes exigen 3D estereoscópico o casco. La 2D (US 10,743,807) figura como caducada.
- **Posit:** la solicitud sobre MOT (2007/0218440) quedó abandonada.
- **Lumos, Akili y CogniFit:** nada sobre MOT.
- **Nombre:** «Satélites» no choca en Play Chile.

Reglas que se mantienen:
- **plano**, sin profundidad simulada;
- **ninguna respuesta durante el seguimiento** (lejos de la multitarea de Akili);
- no calcular una «firma» por series.

## 10. Tutorial con Nubi (no lo tenía)

Ronda de práctica que no cuenta: 2 de 5, lenta y sin sorpresas.
1. «Algunos traen un mensaje», con los objetivos encendidos.
2. Mirar: «Síguelos con la vista».
3. Tocar los que traían mensaje (hueco: cada objetivo).
4. Aviso: «¡Cada mensaje enciende una luz!»
5. «¡Listo! Ahora va en serio».

## 11. Qué se va

- Los rebotes al azar contra los bordes y entre ellos.
- Los 5-8 s de seguimiento fijo por nivel.
- El campo rectangular que dejaba vacío un tercio de la pantalla.
- La revelación inmediata al marcar el último: ahora hay 0,55 s para corregirlo.

## 12. Cómo quedó (implementación del 9-oct, tarea 59)

Código: `Games/Satelites/` — `SatelliteContract` (niveles, sorpresas, medidas, textos), `SatelliteSwarm` (`OrbitLayout`, `OrbitSwarm`, `DustCloud`: movimiento puro con semilla), `SatelliteLayout` (`ScreenPlan`: dónde va cada cosa), `SatelliteRun` (lo que suma la partida),
`SatelliteMetrics` (motor común y telemetría), `SatelliteSprites`, `SatelliteSounds`, `OrbitLineGraphic` (las órbitas punteadas) y el controlador en cuatro partes (`.cs`, `.Build`, `.Scene`, `.Guided`; `.Shots` es solo del Editor, para las capturas).
Pruebas: `Tests/` (`SatelliteContractTests`, `SatelliteSwarmTests`, `SatelliteRunTests`) y, en la app, `SatelitesTest`, `SatelitesRecordTest`, `MigrationTest` (14 → 15) y `BackupRulesTest`.

Lo que se agregó o cambió respecto del diseño, y por qué:

- **Asentamiento al detenerse (agregado).** Medido con la lógica pura (400 a 800 rondas simuladas por nivel): con la regla «solo se detienen si todos están a 38 dp o más (hasta 0,9 s más)», quedaban satélites **tocándose** (< 28 dp) en el 15 % de las rondas del nivel 1 y en el 73 % del 12.
  Causa: los anillos vecinos están a unos 30 dp entre sí a los costados y dos satélites que giran juntos tardan varios segundos en soltarse, más que los 0,9 s. Ahora, si al terminar la espera todavía hay dos a menos de 36 dp, se apartan lo mínimo en 0,35 s (≤ 22 dp cada uno; media 8,5 dp; sin salirse
  del campo ni pisar el planeta). Después del asentamiento nadie queda a menos de 30 dp (peor caso medido: 32,5 dp en 14.400 rondas). Va con un «clac» y no se nota como salto. Lo prueban `SatelliteSwarmTests`.
- **Aire para el sobre.** El anillo de arriba deja 22 dp más que el de abajo (`SatelliteContract.EnvelopeRise`): en pantallas 16:9 el sobre de la señal tapaba la instrucción.
- **Marcar durante los 0,55 s.** Se puede desmarcar y volver a marcar mientras corre ese margen (el boceto lo bloqueaba); con los k marcados otro toque no suma (suena un tono corto) y para cambiar uno primero se quita otro.
- **Tope de espera de la respuesta: 25 s.** Si nadie completa los marcados, se evalúa lo que haya (así una partida abandonada no deja el Reto sin terminar).
- **Motor común.** `stepUp` 1,0 con `useReaction: false`: una ronda perfecta sube un nivel (los seis primeros ensayos pesan 1,5 por la calibración) y un error baja hasta uno (el motor lo limita). Una ronda = un ensayo (acierto = todos los k). El nivel de la ronda es el `PresentedLevel`.
- **Marcador.** Se usa el marcador común (`GameHud`): «Nivel N», «Ronda N de 8» (en el Reto, «Ronda N» y la barra de tiempo) y la píldora de racha de siempre (el número desde 0; «Racha ×N» sale en la revelación desde la tercera perfecta seguida). Ya no hay puntos en el Reto: el puntaje del final es el de siempre (`Score`).
- **Sonido.** El de la revelación es el propio del juego (arpegio al acertar todo, tono suave que baja si faltó alguno); de lo común solo se usan las vibraciones.
- **Tutorial.** Cinco pasos como en §10, salvo que el segundo («Síguelos con la vista») es un aviso con los satélites quietos y no un foco de «Mirar»: las órbitas ocupan todo el campo y Nubi y su globo no tendrían dónde ponerse sin tapar el juego (el smoke lo detectó). La práctica no pasa por el motor, el puntaje ni el guardado,
  y «Cómo se juega» desde la pausa devuelve las luces ganadas.
- **Migración de Room 14 → 15.** El rating guardado del juego viejo se lleva a las tres cuartas partes (`ddaRating × 0,75`): los niveles nuevos traen mecánicas que el viejo no tenía (los de frente desde el 3, las sorpresas desde el 2, la media vuelta desde el 6), así que el mejor jugador del viejo (0,9) parte en el nivel 9 y el del medio (0,5), en el 5. Sin dato no se toca. Prueba en `MigrationTest`.
- **Telemetría.** Siguen `tracking_capacity`, `tracking_targets` y `tracking_speed` (la velocidad es la del nivel más alto superado completo, respecto del 1); se suman `sat_lights`, `sat_perfect`, `sat_best_streak`, `sat_best` y `sat_new`. El récord de luces va en `satelites_record` (respaldado).
- **Herramientas de prueba.** Botones [Debug]: «Satélites con tutorial», «Satélites nivel 3 / 6 / 9» y «Satélites sorpresa: nube» (`sat_stage` y `sat_surprise` de la config).
- **Smoke.** `Satelites` (55 s, juega solo desde el nivel 6 con las tres sorpresas en las tres primeras rondas), `PantallaSatelites` (en forma de teléfono, con la guardia de textos), `TutorialSatelites` (3 formas de pantalla, con toque real) y `HowToSatelites`. Capturas reales: `bash tools/verificar-todo.sh --capturas Satelites` → `docs/previews/capturas/satelites.png`.
