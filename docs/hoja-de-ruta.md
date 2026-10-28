# Hoja de ruta (aprobada por Ricardo el 8-oct-2026)

Reemplaza a la del 30-sep. Lo que manda es `CLAUDE.md` y el código; esto ordena lo que viene y en qué orden. Cada etapa termina cuando sus tareas están hechas, probadas y verificadas; no se empieza la siguiente por entusiasmo.

## Origen y roles

- **Origen**: la auditoría externa del 8-oct (Fable; el informe está fuera del repositorio y no se cita aquí). De ahí salen el orden de las etapas y la lista de «Lo que NO hacemos ahora».
- **Roles**: Opus coordina y diseña; Sonnet es el único que toca código; Fable es segunda opinión crítica, en solo lectura. Ricardo decide y prueba en el teléfono.

## Dónde estamos (8-oct)

17 juegos en 4 áreas (Cosecha de palabras se retiró el 10-oct, Tarea 80). TODOS tienen tutorial guiado (`UnityGameLauncher.TUTORIAL_GAMES`): Acoplamiento lo recibió en la Tarea 68 y Rumbo a Casa, el único que faltaba, se retiró en la 69 (10-oct). Los retirados están en `docs/juegos/descartados.md`.

| Área | Juegos |
|---|---|
| Memoria | Constelaciones, Rastro de luz, Bodega de carga, Correo Estelar |
| Atención | Tinta o Palabra, Piloto Estelar, Freno de Emergencia, Satélites, Rescate relámpago (antes Radar) |
| Razonamiento | Acoplamiento, Carga exacta, Aterrizaje Lunar, Engranajes («Taller de reparación») |
| Lenguaje | En la punta de la lengua, Lluvia de meteoros, ¿Verdad o disparate?, La estrella intrusa |

## Etapa 0 — Limpiar y destrabar

- **Hecho (Tarea 51)**: textos sin promesas de salud (guardia `VisibleTextsGuardTest`), sin percentiles ni varios perfiles, licencia OFL completa y documentos al día.
- **Hecho (Tarea 52)**: el camino diario se elige por avance real y con variedad (`data/DailyPath.kt`: 3 juegos de 3 áreas, el área que quedó fuera ayer entra hoy, sin repetir los de ayer y anteayer, sin juegos que la persona nunca jugó y no tienen tutorial, determinístico por fecha); el día en que se completa el Primer vuelo, ese día queda cumplido con el vuelo (cuenta para la racha, la meta semanal y los desafíos de días, sin duplicar puntos ni partidas), y «dominio» pasó a «área» en lo que se ve.
- **Hecho (Tareas 53 y 54)**: capturas reales de los 19 juegos (`--capturas todos`, láminas en `docs/previews/capturas/`) y arreglo de lo que mostraron: los avisos van debajo del marcador, la letra de los juegos de Unity no baja de 14 dp (con guardia en el smoke), el foco del tutorial ya no deja cuadrados oscuros y varios detalles por juego (`docs/previews/capturas/hallazgos.md`).
- **Hecho (Tarea 55)**: los avisos ya no tapan el juego (se acomodan en una franja libre; el smoke lo prueba). Queda exento Rumbo, que se rehace en la etapa 2: al rehacerlo, quitarlo de `ToastCoverExceptions` (Piloto ya salió en la Tarea 61 y tiene su propia guardia). Rumbo se retiró el 10-oct (Tarea 69): su exención sigue mientras se conserven su código de Unity y su arranque de prueba.
- **Hecho (Tarea 56)**: tutoriales guiados con Nubi en Cosecha de palabras, ¿Verdad o disparate? y La estrella intrusa (cada uno con su ronda de práctica que no cuenta, «Saltar tutorial», «Cómo se juega» en la pausa y su smoke en las tres formas de pantalla).
- **En espera, no ahora**: los pilotos automáticos que faltan en 15 juegos (las capturas de 8, 20 y 40 s repiten la primera situación donde no los hay) y, al tocar cada juego en las etapas 1 y 2, agrandar su área de juego: en pantalla 20:9 sobra casi un tercio de abajo en Rumbo, La estrella intrusa y Bodega (y, en menor medida, en Rescate relámpago, que escala la pantalla de 360 × 640 dp sin estirarla) (Satélites ya usa toda la altura desde la tarea 59).

## Etapa 1 — Atención

Cada juego sale con su tutorial (Freno ya lo tenía; los otros tres lo reciben aquí). **Etapa 1 completa (Tarea 62, 9-oct).** Orden:

1. **Freno de Emergencia** — **Hecho (Tarea 57)**: el SSRT (lo que tarda la persona en frenar) salió del puntaje (`100 × (0,75 × lanzamientos correctos + 0,25 × min(1, altos frenados ÷ altos ÷ 0,5))`) y queda solo como medida; «Tu freno» se muestra como el promedio de las últimas ≤ 5 estimaciones válidas, en un velocímetro de tres zonas con nombre (ágil < 230 ms, firme 230-300, pausado > 300; cortes de diseño, no normas), sin milisegundos; Juegos y Hoy dicen la zona.
2. **Satélites** — **Hecho (Tarea 59)**: renovado como «enciende tu planeta» (mismo id): órbitas planas en tres anillos que usan toda la pantalla, sorpresas anunciadas antes (cambio de órbita, nube de polvo, órbitas rápidas), cada mensaje entregado enciende una luz en el planeta, tutorial con Nubi y récord de luces (`docs/diseno-satelites.md`). Falta que Ricardo lo pruebe con personas: «se hace largo y monótono» era el problema que lo motivó.
3. **Piloto Estelar** — **Hecho (Tarea 61)**: renovado como «la ruta de las balizas» (mismo id): ruta de balizas, señales por forma y detalle (nunca solo por color), sectores con misión nueva, hiperimpulso, sonido de motor, tutorial con Nubi, ningún aviso tapa una señal y las dos tareas siempre juntas, sin «costo de multitarea» (`docs/diseno-piloto.md`). **Ricardo lo probó en el teléfono y lo aprobó (10-oct, con el motor «Cohete»).**
4. **Rescate relámpago** — **Hecho (Tarea 62)**: renovado como «qué cápsulas viste» (mismo id `radar`): seis cápsulas con forma y color fijos que se eligen en un tablero de orden fijo (se responde QUÉ y no DÓNDE: regla permanente de Ricardo, `docs/nombre-marca-y-riesgos.md` §9), rocas grises, nave que se llena con el rayo tractor, lluvia de cápsulas cada 4 rondas, «Tu vistazo» y «Tu captura: X de 4», récord de cápsulas, tutorial con Nubi y sin ninguna medida por lugar (`docs/diseno-rescate.md`). **Versión 4 hecha (Tarea 65, 9-oct)**: toda la pantalla en 20:9, mundo con estación accidentada, radar luminoso, nave protagonista con viaje a la estación cada 10 cápsulas, totales «En total: …» y sonido «Madera cálida» (sección 16 del documento del juego). **Ricardo lo probó en el teléfono, con sonido, y lo aprobó (10-oct, con «Madera cálida»).**

## Etapa 2 — Memoria y razonamiento

- **Rumbo a Casa** — **RETIRADO (Tarea 69, 10-oct)**: Ricardo lo probó con una persona de 60 años o más (le costó entender la mecánica y le generó desorientación) y eligió retirarlo en vez de rediseñarlo con un mapa fijo con el norte arriba. Ver `docs/juegos/descartados.md`.
- **Acoplamiento** — **Hecho (Tarea 68, 10-oct)**: renovado como «muelle de acoplamiento» (mismo id): piezas de hasta 4 × 4 bloques, módulo y hueco grandes y a la misma escala, estación de anillos que crece (8 casilleros por anillo), luz y sombra fijas, revelación en seis tiempos, sonido «Madera cálida», récord y totales, tutorial con Nubi (`docs/diseno-acoplamiento.md`).
- **Aterrizaje Lunar** — **Hecho (Tarea 70, 10-oct)**: renovado como «la misma tarea, más grande, con más vida y un final con sentido» (mismo id): nave y regla grandes, Tierra, estrellas y cordilleras que se desplazan (nada fijo junto a la regla), bandera y cúpulas de una base lunar, sonido «Madera cálida», tutorial con el aspecto nuevo y un final con sentido (`ui/components/MeaningfulResult.kt`, la plantilla para llevar a los demás juegos si Ricardo lo confirma) (`docs/diseno-aterrizaje.md`). **Con este juego se cierra la Etapa 2.** Ricardo lo probó en el teléfono, con sonido, y lo aprobó el 10-oct (con la unidad en el final, Tareas 71 y 72). Ese día también aprobó Rescate (con «Madera cálida»), Piloto (con el motor «Cohete») y Acoplamiento; falta probar con personas (Etapa 2b).
- **Sonido «Madera cálida»:** aprobado por Ricardo en Rescate, Acoplamiento y Aterrizaje (y el motor «Cohete» en Piloto). **Llevarlo al resto de los juegos queda para DESPUÉS**, cuando terminemos lo estético (no se hace ahora).
- Con la 68 y la 69, TODOS los juegos de la app tienen tutorial, así que la regla del camino diario «nunca un juego sin tutorial que nunca se jugó» queda inerte (sigue en el código, con su prueba, por si se suma un juego nuevo sin tutorial).

## Etapa 2b — Probar con personas

- Una versión firmada y optimizada (no la de depuración).
- **8 a 12 personas de 60 años o más, durante 14 días**; que no sean pacientes de Ricardo.
- Qué se mira: qué entienden, qué abandonan y si vuelven. Con lo aprendido se ajusta el DDA (calibrarlo con datos reales) y se decide qué juegos siguen.

## Etapa 3 — Avance de fondo

- **Finales con sentido en los juegos de la app (18 entonces; 17 desde el retiro de Cosecha) — Hecho (Tarea 77, 10-oct)**: lo que hiciste con UN dato tuyo, tu avance solo contigo (hoy, tu promedio y tu mejor, con su frase), el truco y «¿Por qué importa?» con su fuente de PubMed; el detalle de siempre va detrás de «Ver el detalle de tu partida» (`docs/finales-con-sentido.md`).
- **Cosecha de palabras retirada y textos de Piloto — Hecho (Tarea 80, 10-oct)**: Ricardo decidió «quitemos cosecha; dejemos piloto». Cosecha repetía a En la punta de la lengua y tenía la medida más débil de Lenguaje (`docs/juegos/descartados.md`); la app queda en 17 juegos y Lenguaje en 4. Piloto Estelar se queda y sus textos visibles se corrigieron (ya no dicen «multitarea», «atención dividida» ni «piloto automático»: las dos tareas van juntas desde el primer segundo, regla permanente 1). Más adelante podría hacerse otra modificación de Piloto para no tener conflictos con patentes (idea de Ricardo, sin fecha).
- **Avance — Hecho (Tareas 124 y 125, 18-oct)**: tu semana como único número grande («3 de 4 días»), racha que perdona (un día de descanso por semana no la corta), tus áreas con el canal y el churro del kit, lo que descubriste (tus marcas en palabras), medallas por conducta (Constancia, Variedad y Volver), tu liga en una fila y tus últimas partidas sin puntos; Juegos con el edificio de cada área sobre su pedestal, los juegos en grupos («En tu ruta de hoy», «Hace días que no juegas», «Nuevo para ti») y el emblema de cada juego. Detalle en `docs/arquitectura-app.md`.
- Revisar los juegos del Primer vuelo (y de la evaluación inicial) y el re-chequeo mensual del punto de partida.
- Marcas ✓/✗ de arcilla sobre las respuestas: el ✗ en aspa diagonal, no en cruz (`docs/simbolos-neutros.md`).

## Etapa 4 — Publicar

Solo cuando la app esté casi lista.

- **Abogado y especialista en lanzamiento.**
- **EsPal, pendiente de consulta (decisión de Ricardo, 10-oct):** la frecuencia de uso que trae SPALEX y que En la punta de la lengua usa para el nivel de cada palabra viene de EsPal, y sus términos de uso no están confirmados. Ya se cita por cortesía en «Licencias y créditos» (sin ninguna licencia escrita); antes de publicar hay que consultar a los autores o al abogado, o sacar esa dependencia (`docs/nombre-marca-y-riesgos.md`, «Licencias de terceros»).
- **Marca**: búsqueda oficial (clases 9 y 41) — pendiente. ~~`applicationId` propio~~: **hecho el 15-oct** (`com.nubibraingames.app`; cambiarlo después de publicar = app nueva).
- **Mínimo Android 8 (API 26)** — **hecho el 27-oct (Tarea 183)**, decisión de Ricardo: la app usaba `java.time` (`ZoneId`, `Instant`, `LocalDate`…), que no existe en Android 7, y se habría cerrado ahí (Lint `NewApi`: 14 errores con `minSdk 24`, ahora 0). `app/build.gradle.kts`: `minSdk = 26`. El export de Unity (`unity.minSdkVersion=24` en `gradle.properties`) NO se tocó: manda el más alto. Google Play ofrecerá Nubi desde Android 8.
- **Pendientes de «La pestaña Tú» (Tarea 185, 28-oct):** (1) **El correo de Nubi**: un correo NUEVO solo para Nubi, que Ricardo todavía no crea; Google Play pide un correo del desarrollador y sale público en la ficha. Cuando exista, se escribe UNA vez en `NubiContact.EMAIL` (hoy `null`: la losa «Escribir a Nubi» de Tú dice «Correo por definir» y no hace nada). (2) **La política de privacidad**: Google Play la pide para TODAS las apps (enlace en la consola y enlace o texto dentro de la app); no hay página ni enlace todavía, así que la losa «Política de privacidad» NO está en «Acerca de Nubi». Hace falta una página corta (no se recoge nada, sin cuenta ni internet, el informe de errores lo envía la persona a mano) alojada donde Ricardo decida. (3) Las 17 líneas de «qué tomamos» de «En qué se basa Nubi» y los cuadros de Tú (V1 «Centrado») YA los aprobó Ricardo el 28-oct (`docs/en-que-se-basa-nubi.md`; los 24 DOI se comprobaron). (4) La **declaración de salud** de Google Play («Mental Acuity») y el aviso en la descripción de la tienda.
- **Revisión con REA del APK (16-oct)** — informe fuera del repo: `C:\Users\RURAL7\Desktop\Proyectos\REA\analisis\nubi-prepublicacion-2026-10-16\informe.md`. ~~Páginas de memoria de 16 KB~~: **pasa** (las 10 librerías `.so`, también `libil2cpp.so` y `libunity.so`). ~~Filtro de procesador~~: **hecho el 16-oct** (`ndk { abiFilters += "arm64-v8a" }` solo en `release`; sin él Play habría ofrecido Nubi a teléfonos de 32 bits y Chromebooks x86, donde los juegos se caen; debug no cambia, así el emulador `medium_phone` sigue sirviendo). **Pendiente:** repetir la revisión REA con el AAB o APK *release* cuando exista, para confirmar que salió igual.
- **Ficha de la tienda sin promesas de salud.** Declaración de salud de Google Play («Mental Acuity»); privacidad y seguridad de datos. Para el formulario «Seguridad de los datos»: REA no encontró `android.permission.INTERNET`, ni anuncios, ni analítica, ni reporte de errores (informe de arriba, §3 y §4); lo esperable es declarar que la app no recopila ni comparte datos, confirmándolo al llenarlo junto con quien revise la política de privacidad. Si las ligas llegan a usar un servidor, aparecerá el permiso de internet y habrá que actualizar el formulario y la política.
- Página «La ciencia de Nubi» (qué mide cada medida y qué no).
- Firma de lanzamiento, i18n completo (hoy `ui/i18n/AppStrings` cubre pocos textos) y **prueba cerrada**.
- **Países**: evaluar empezar por Chile y Latinoamérica.

## Etapa 5 — Solo si la gente vuelve (retención a 30 días)

- Un juego nuevo: Torre de Lunas, o razonamiento con patrones a la vista.
- Cuentas y nube (servidores).

## Lo que NO hacemos ahora

- Juegos nuevos antes de la prueba con personas.
- Percentiles ni comparaciones con una referencia supuesta.
- «Entrenamiento cognitivo» (ni «cerebro», ni promesas de salud) en ningún texto.

Las ideas en espera (rangos de tripulación, «Tu astronauta») están en `docs/ideas-guardadas.md`.
El modelo de negocio y el enganche a la Duolingo (planes, liga, protector de racha) están en estudio, sin implementar: [`docs/modelo-negocio.md`](modelo-negocio.md).

## Candidatos de juegos, para después de la prueba

Tareas clásicas con décadas de literatura pública; cada una pasa por la revisión de patentes de `docs/nombre-marca-y-riesgos.md` ANTES de programarse; nombre, arte y sonidos propios, táctiles y sin voz.

- **Razonamiento**: Torre de Lunas (planificar; Torre de Londres) · Matriz Perdida (patrones generados por programa, sin copiar ítems de ningún test).
- **Cálculo**: ¿Cuántas estrellas? (comparar cantidades sin contar) · Balanza de carga.
- **Velocidad** (el área más patentada por la competencia, sobre todo Posit): Decodificador (símbolos propios con su clave) · Unir la constelación (tarea de trazar caminos, 1944) · La nave distinta (búsqueda visual).
- **Lenguaje**: Frase rota (ordenar una frase) · Sinónimos en órbita. Dependen del idioma (listas de palabras por idioma).

## Repositorios externos

Regla de Ricardo (30-sep): si aparece un repositorio de GitHub que pueda potenciar la app, se le comenta y ÉL decide si
se agrega. Descartados: Zenject / Extenject (inyección de dependencias: el original sin cambios desde 2021; obligaría a
reescribir los 20 juegos, trae riesgos con IL2CPP en Android y los juegos ya están separados en reglas + pantalla) y
awesome-unity (es una lista de enlaces, archivada en enero de 2025; sirve solo para consultar).
