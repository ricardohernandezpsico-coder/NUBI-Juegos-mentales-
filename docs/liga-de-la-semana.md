# La liga de la semana (especificación, 4-nov-2026)

Decisiones de Ricardo (4-nov):
- **UNA división por persona**, que sale **solo del Contrarreloj**. Es una liga semanal que se sube de a una, como en Duolingo.
- Los demás juegos siguen con sus puntos en Avance, sin divisiones ni «Etapa».
- La liga es **GRATIS**.
- Los dibujos del artifact «Liga de la semana» (https://claude.ai/artifact/Q6vKP4VjCFAPZy3KV9GoLY) están bien.
- **Diez divisiones** en vez de cinco (propuesta de Opus, aprobada).

Análisis de apoyo: `docs/techo-dificultad.md` §8. Con la referencia estimada, las nueve líneas caen en marcas distintas en los 4 juegos y las 6 franjas.

Tareas:
- **T228:** los 5 emblemas nuevos y una lámina con los 10, para que Ricardo los apruebe.
- **T229:** la lógica de la liga, la pestaña Contrarreloj, Hoy, la hoja del lunes y Tú.
- **T230:** sale «Etapa» de toda la app, salen las divisiones por juego y se corrigen los textos.

T226 sigue reservada para el logotipo.

---

## 1. Las diez divisiones

| # | División (`label`) | Línea (`from`, %) | Emblema | Lugar, en palabras |
|---|---|---|---|---|
| 1 | En la plataforma | 0 | `division_plataforma` (ya existe) | la primera de diez divisiones |
| 2 | Despegando | 10 | `division_despegando` (ya existe) | la segunda de diez divisiones |
| 3 | Sobre las nubes | 20 | `division_nubes` **nuevo** | la tercera de diez divisiones |
| 4 | En órbita | 30 | `division_orbita` (ya existe) | la cuarta de diez divisiones |
| 5 | En la Luna | 40 | `division_luna` (ya existe) | la quinta de diez divisiones |
| 6 | En Marte | 50 | `division_marte` **nuevo** | la sexta de diez divisiones |
| 7 | Entre asteroides | 60 | `division_asteroides` **nuevo** | la séptima de diez divisiones |
| 8 | En Júpiter | 70 | `division_jupiter` **nuevo** | la octava de diez divisiones |
| 9 | Entre estrellas | 80 | `division_estrellas` (ya existe) | la novena de diez divisiones |
| 10 | En otra galaxia | 90 | `division_galaxia` **nuevo** | la décima y más alta de las diez divisiones |

- Los nombres se escriben siempre completos: «En la Luna», no «Luna».
- `Comparison.Division` pasa a ser la **división de la liga**, con estas diez entradas. Su `from` es la línea del % de la semana, ya no el percentil de una partida.
- `divisionPlace` y `divisionPlaceLine` dicen «de diez».

### 1.1 Los emblemas nuevos (T228)

Siguen la dirección A «Los cinco lugares» de la T212:
- un solo objeto grande, el lugar que dice el nombre;
- las mismas piezas, la misma luz y el mismo material de `tools/juegos/plasticina.py`;
- lienzo de 440 px;
- el objeto ocupa el 86 % de la ventanilla noche `#100F3A`, a 52 dp en la cifra y 28 dp en la escalera.

Cada emblema debe distinguirse de los otros nueve por su **forma**, no solo por el color. Nada de puntas, porque los luceros siempre son redondos.

| Emblema | Qué es | Qué lo separa de los parecidos |
|---|---|---|
| Sobre las nubes | Un banco de nubes grande: 4 o 5 bolas de nube (`NUBE #ECE8F3`) en dos filas, que llena el ancho. | Despegando tiene el cohete y tres nubecitas de humo chicas. Aquí no hay cohete y la nube es el objeto. |
| En Marte | Una esfera terracota oscura (p. ej. `#C8553D`), con un casquete polar crema arriba y dos o tres cráteres bajos. | El planeta de En órbita es naranja claro `#FF9F6B` y tiene anillo. Marte es más oscuro, no tiene anillo y lleva casquete. |
| Entre asteroides | Tres o cuatro rocas **irregulares** (bolas abolladas, no esferas), de tamaños distintos y gris cálido o pardo (p. ej. `#A8968A`), con cráteres chicos. | La Luna es UNA esfera lisa gris lavanda `#C9CFE6`. Aquí hay varias piezas deformes. |
| En Júpiter | Una esfera grande con **bandas horizontales** de plasticina (crema, arena y naranja) y la mancha roja ovalada. | No lleva anillo, para no confundirse con En órbita. Las bandas lo identifican. |
| En otra galaxia | Una espiral de dos brazos hecha de bolitas redondas (lila y sol) que achican hacia afuera, alrededor de un centro claro. | Entre estrellas son tres luceros sueltos. Aquí las bolitas forman la espiral. |

**Lámina para Ricardo:** los 10 emblemas en orden, con su nombre, en la ventanilla de 52 dp y en la de 28 dp, a tamaño real de teléfono. Se integran a la app recién cuando Ricardo los apruebe; antes no entran en T229.

---

## 2. Tu semana

- **Qué juegos:** los 4 del Contrarreloj: Tinta o Palabra, ¿Verdad o disparate?, Acoplamiento y Rescate relámpago.
  - Solo cuentan las partidas con la **pista de hoy** (`TimeTrialDay.ofGame`).
- **Qué semana:** desde el lunes a las 00:00, hora local (`Week.weekStart`), hasta el domingo a medianoche.
- **Por juego:**
  - Se toma la **mejor marca de la semana** (`Comparison.bestOfWeek`).
  - Su percentil se calcula con `Comparison.percentile` contra la referencia de la franja de la persona. Es la misma fuente y la misma tabla que la cifra de la página: tramos sin suavizar, hacia abajo, tope 99.
  - Esta definición calza con la referencia, que es la mejor marca de cada robot en la semana (Sonnet, §8, punto 6).
  - Un juego cuenta solo si su `Comparison.gate` es `SHOW`: tiene fuente, la persona tiene franja y el juego ya tiene 3 partidas guardadas. Si no, ese juego no entra en el promedio.
- **Tu semana** = el promedio de los percentiles de los juegos que cuentan, **redondeado hacia abajo**, como el percentil: nunca dice más de lo cierto.
  - Ejemplo del video de Ricardo: (94 + 19 + 42) ÷ 3 = 51,67 → **51 %**. El artifact decía 52 % porque redondeaba.
  - Los juegos no jugados no cuentan y no bajan el promedio.
- **Sin ningún juego que cuente:** la semana queda **sin %**.
- **Sin franja de edad (`NEEDS_AGE`) o sin fuente (`NONE`):** no hay liga. La página muestra lo de hoy y Hoy no muestra la línea.
- **Plus o gratis:** se calcula igual para todos.
  - Quien no tiene Plus juega solo el juego del día, que rota entre los cuatro.
  - Quien tiene Plus puede elegir; queda anotado para la fase 2 (personas reales), sin cambio por ahora.

## 3. Subir, quedarse, bajar (al cerrar la semana)

Con `línea(d) = d.from`:
- Si la semana llega a la línea de la división de **arriba** → **sube una**.
- Si llega a la línea de **la suya** → **se queda**.
- Si no llega a la de la suya → **baja una**. Nunca baja de En la plataforma.
- En la más alta, En otra galaxia, con 90 % o más → se queda.
- **Nunca más de una división por semana**, para arriba o para abajo.
- **Semana sin %** (no jugó, o solo jugó juegos que todavía no cuentan): **baja una**, como cualquier semana que no llega a su línea.
  - Lo decidió Ricardo el 4-nov: «si no juega, queda a la cola de la semana en comparación con los demás, por lo que debería bajar».
  - Cada semana cerrada sin % baja una. Si la persona no juega en 3 semanas, baja 3, de a una por semana.
  - En la plataforma no se baja más.
- **Sin histéresis por ahora.** El ruido arriba (Sonnet §8.1, punto 5: en Acoplamiento y Rescate, de 1 a 2 aciertos entre las líneas del 80 % y el 90 %) se amortigua con tres cosas:
  - la mejor marca de la semana;
  - el promedio de varios juegos;
  - de a una división por semana.

  Se revisa con datos reales.

## 4. Guardar y cerrar semanas

- **Semanas cerradas guardadas:** para cada una, `weekStart`, el % de la semana (o nada), la división con que empezó y la división con que terminó.
  - La división actual es la «terminó» de la última semana cerrada.
  - **Nunca se recalcula hacia atrás.** Si cambia la pista o la referencia, lo cerrado queda como estaba.
  - Sonnet elige dónde guardarlo. Se recomienda SharedPreferences `league_weeks` (JSON) en vez de una tabla nueva de Room.
  - Va en el respaldo: `backup_rules.xml` y `data_extraction_rules.xml`, comprobado por `BackupRulesTest`.
  - «Borrar datos» lo borra.
- **Cuándo se cierran:** al abrir la app y al volver a primer plano.
  - Se cierran, en orden, todas las semanas pendientes desde la última cerrada hasta la anterior a la actual.
  - Si la persona no abrió la app en 3 semanas, se cierran 3 semanas: cada una con sus partidas, o «sin %».
- **Inicio:** la liga empieza la semana en que se abre por primera vez la versión que la trae.
  - Todos parten **En la plataforma**, también quienes ya tienen partidas del Contrarreloj: no se reconstruye el pasado.
  - Esa primera semana ya cuenta.

## 5. Lo que se ve

Reglas de siempre:
- letra de 16 sp o más;
- toques de 48 dp o más;
- contraste de 4,5:1 o más;
- nada se superpone y no hay datos sobre el cielo con estrellas;
- sin palabras sueltas, sin «percentil» ni «ranking»;
- siempre «referencia estimada».

En los textos de la liga sí se usan «subir», «quedarse» y «bajar», en frases completas.

### 5.1 Pestaña Contrarreloj, arriba (GRATIS)

**a) Losa de la liga** (crema, como la losa de la cifra):
- Emblema de 52 dp en su ventanilla, el nombre de la división y debajo «Tu división: la segunda de diez.»
- «Tu semana», con **«51 %»** grande en Rubik 40 y tope de escala 1,3. Es la única excepción a la escala de letra de la página.
- Frase: «En promedio, esta semana superas al 51 % de la referencia estimada para tu edad.»
- Barra de 0 a 100 con la marca de la persona y la línea de la división de arriba, rotulada con su nombre.
- La frase del lunes, según cómo va:

| Cómo va | Frase |
|---|---|
| Para subir | «Si la semana terminara hoy, el lunes subirías a Sobre las nubes.» |
| Para quedarse | «Si la semana terminara hoy, te quedarías en Despegando. Para subir a Sobre las nubes, te falta llegar al 20 %.» |
| Para bajar | «Si la semana terminara hoy, bajarías a En la plataforma. Para quedarte, te falta llegar al 10 %.» |
| Sin % esta semana | «Todavía no juegas esta semana. Con cualquiera de los cuatro juegos puedes quedarte en Despegando o subir.» (En la plataforma: «…puedes subir a Despegando.») |
| En la más alta | «Si la semana terminara hoy, te quedarías En otra galaxia, la división más alta.» |
| Primera semana, sin partidas | «Tu primera semana: todos parten En la plataforma.» |

  - Las preposiciones van pegadas al nombre: «te quedarías en Despegando», pero «te quedarías En la Luna» (no «en En la Luna»). Hay que armarlas con una función que lo pruebe para las diez.
- «La semana cierra el domingo. Quedan N días.» («Queda 1 día.»; el domingo: «La semana cierra hoy a medianoche.»)

**b)** La fila «Con qué te comparas ›» queda donde está, debajo de la losa.

**c) Escalera «Las diez divisiones»**, de arriba abajo:
- Cada fila lleva emblema de 28 dp, nombre y «desde el 80 %»; la de abajo dice «desde el 0 %».
- Filas de 40 dp; la de la persona en lima, de 52 dp, con «Tú», como la escalera de la T203.
- Reemplaza a la escalera en aciertos de cada juego.

**d) «Tus juegos de esta semana»:** cuatro cuadros en 2×2, que **reemplazan al selector de juegos** (`GameSelector`).
- Cada cuadro lleva el nombre del juego y su mejor marca de la semana: «81 aciertos» y «Superas al 94 %».
  - Sin partidas esta semana: «Sin partidas esta semana».
  - Con menos de 3 partidas en total: «Cuenta desde tu tercera partida».
- El juego de hoy lleva «Hoy».
- Tocar un cuadro elige ese juego para el resto de la página (Jugar y lo de Plus).
  - Sin Plus, solo se elige el de hoy. Los otros tres abren la hoja del candado, como el selector de ahora, pero **muestran igual su marca y su %**, porque la liga es gratis.
- Debajo, una línea: «Tu semana es el promedio de los juegos que jugaste.»

**e) `FreeCard`:**
- Queda «Jugar · 2 minutos», «Tu récord: N aciertos.» y lo de las partidas que quedan hoy.
- **Salen** la división del juego con su emblema y su lugar, «Te faltan N aciertos para X», su barra y «La semana cierra el domingo.», que ahora está en la losa de la liga.

**f) Plus, debajo, como hoy:** la cifra del juego elegido, la campana de tu edad, otras edades, semana a semana, tus marcas y tu ritmo.
- Sale todo lo que nombre una división por juego.
- La racha de semanas («Llevas 3 semanas seguidas En órbita o más arriba») pasa a decir: «Llevas 3 semanas seguidas en la mitad de arriba de la referencia estimada.» El umbral sigue en 50 y no usa el nombre de ninguna división.

### 5.2 Hoy

Bajo la losa celeste del Contrarreloj (`home_timetrial_line`): emblema de 24 dp, seguido de una línea.

| Cómo va | Línea |
|---|---|
| Para subir | «Despegando · vas para subir a Sobre las nubes» |
| Para quedarse | «Despegando · vas para quedarte» |
| Para bajar | «Despegando · juega para quedarte» |
| Sin % | «Despegando · juega para quedarte» (En la plataforma: «En la plataforma · juega para subir») |
| En la más alta | «En otra galaxia · vas para quedarte» |

- «Te quedan N partidas» sale de Hoy y queda solo en la pestaña y en la ficha.
- Sin franja o sin fuente, no hay línea.
- El lector de pantalla lee la línea entera, sin el emblema.

### 5.3 La hoja del lunes

Aparece **una vez por semana cerrada**, la primera vez que se abre la app después del cierre. Si se cerraron varias semanas, habla solo de la última.

**Si subió:**
- Título: «¡Subiste a Sobre las nubes!», con el emblema de 52 dp.
- «Tu semana pasada: superaste en promedio al 51 % de la referencia estimada para tu edad.»
- Si el % alcanzaba para más de una: «Alcanzaba para En Marte, pero se sube de a una división por semana.»
- «Esta semana: con el 30 % subes a En órbita; con el 20 %, te quedas Sobre las nubes.»
- Botón «Seguir».

**Si se quedó:** «Sigues en Despegando», sin celebración, con la misma línea de «Esta semana…».

**Si bajó:** «Esta semana estás En la plataforma», sin celebración, sin «bajaste» y con lo que necesita.

**Semana sin %:** es el caso «si bajó» (o «si se quedó», En la plataforma), con «Tu semana pasada: no jugaste el Contrarreloj.» en vez de la frase del %. Sin culpa: nunca «bajaste» ni «perdiste».

### 5.4 Tú

«Etapa general: Hábil» pasa a ser «Tu división: Despegando», con el emblema de 24 dp. Sin liga (sin franja), la fila no se muestra.

## 6. Lo que sale (T230)

- **«Etapa», de toda la app.** Sonnet hace el inventario completo. Lo conocido:
  - `AreaPoints.stageLine`, la tira «Inicio · Aprendiz · Hábil · Experto · Maestro» y «Te faltan N puntos para Hábil» (`StageScale`, `AvanceAreaPage`);
  - la pantalla de carga (`GameLoadingScreen`), la ficha del juego (`GameSheet`, `sheet_stage`), `GameResultScreen`, `FirstFlight` y `ProfileScreen`.
  - Avance **conserva los puntos y la barra**.
  - «Te mantuviste» cambia de palabras, para no confundirse con «te quedas» de la liga.
- **«Etapa más alta: N de M»** pasa a «Nivel más alto: N de M» en Bodega, Constelaciones, Engranajes y Correo.
- **La división por juego,** en todas partes: `FreeCard`, la escalera en aciertos (`thresholds`, `ladder`, `needSentence` y `rungText` en la UI), la frase con «En órbita o más arriba» y el final de partida, si la nombra.
- **Hoja del candado (`PlusAccess`):** se corrige todo texto que diga que el lugar o la división son de Plus. Ahora la liga y el % de cada juego son gratis.
  - Plus sigue siendo: jugar los cuatro cuando quieras y sin tope; la campana; otras edades; semana a semana; tus marcas y tu ritmo.
  - Sonnet lista los textos viejos y nuevos en la lámina para que Opus los revise.
- **Pendientes de la T227** (correcciones, no reglas nuevas):
  - «N partidas» de Tú y las medallas por número de partidas suman también las del Contrarreloj;
  - la medalla de constancia ganada con un Contrarreloj se celebra al final de ESE Contrarreloj, si el final ya celebra medallas. Si no, queda como está y se avisa.
- **Las ligas viejas escondidas** (`RankTier`, `LeaguePromotionOverlay`, `Features.LEAGUES = false`) no se reutilizan. Si se pueden borrar sin tocar nada más, se borran en T230 para que no haya dos «ligas» en el código.

## 7. Pruebas mínimas (T229)

**Lógica pura (`LeagueTest`):**
- el promedio redondeado hacia abajo y el ejemplo 94, 19 y 42 → 51;
- el juego en `EARLY` no cuenta;
- la pista vieja no cuenta;
- la mejor marca de la semana, no la última;
- exactamente en la línea → se queda o sube;
- tope y piso;
- de a una aunque el % alcance para tres;
- cerrar 3 semanas pendientes;
- la semana sin % baja una, y en la plataforma se queda;
- nunca se recalcula lo cerrado;
- el respaldo.

**Frases:** las diez divisiones con cada frase. Ninguna dice «en En», ninguna dice «percentil» y todas las cifras van con «%» pegado (`glueNumbers`).

**Láminas** (360 y 393 dp, letra 1,0, 1,3 y 2,0; ojo con la lámina de `Dialog` en Robolectric, que sale a 1,0):
- la losa de la liga en sus 6 estados;
- la escalera de 10 con la persona abajo, al medio y arriba;
- los cuatro cuadros con y sin Plus;
- la línea de Hoy;
- la hoja del lunes en sus 3 casos;
- Tú.

Hay que revisarlas buscando choques: emblema con texto, «51 %» con el rótulo, la barra con su rótulo.

---

## 8. Cómo quedó (T229, 4-nov)

Hecho en la Tarea 229; el detalle está en [`docs/contrarreloj.md`](contrarreloj.md) §33. Donde la spec no calzaba con el código o dejaba un hueco, se decidió así:

- **Hoy, sin partidas gratis.** La línea de la liga se dibuja en TODOS los estados del bloque del Contrarreloj, también cuando no quedan partidas (bajo el recuadro y la fila gris, que no tienen losa celeste que la preceda).
- **`FreeCard` conserva «Esta semana jugaste N partidas y la mejor fue…»:** la spec no la nombra entre lo que sale.
- **La barra de la losa** rotula la línea con el nombre y el %: «Sobre las nubes · 20 %» (con solo el nombre, una marca en una barra de 0 a 100 no dice dónde está).
- **Semana sin % o primera semana sin partidas:** no hay cifra grande ni barra (no hay nada que medir); quedan la división y la frase. Si jugó pero ningún juego cuenta todavía (les faltan partidas para la tercera) la frase no dice «Todavía no juegas»: dice «Todavía no cuenta ningún juego esta semana: cada uno cuenta desde tu tercera partida.».
- **Las semanas seguidas** viven en «Semana a semana» (antes en la escalera en aciertos, que salió).
- **«Etapa general»** de Tú se reemplazó entera; `overallIndex` y `levelWord` quedan para la T230.
