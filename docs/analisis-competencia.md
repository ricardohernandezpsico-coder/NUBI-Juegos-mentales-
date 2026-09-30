# Análisis de la competencia: Lumosity, NeuroNation y Peak (29-sep)

Sale de las capturas de pantalla que Ricardo guardó en `Proyectos/` (fuera del repositorio, por peso):
`pantallazos Lomosity/` (90), `pantallazos Neuronation/` (12) y `pantallazos PEAK/` (14). Versiones vistas: Lumosity
10.20.96, NeuroNation 3.8.96; de Peak no aparece la versión. Cuenta gratuita en los tres (Lumosity en "Miembro, acceso
limitado"), así que casi todo lo de pago se vio solo bloqueado.

**Límite de este análisis.** Es solo lo que se ve en las capturas y lo que las apps dicen de sí mismas. No se miró
retención, reseñas ni descargas: sus cifras son autodeclaradas. Una captura de la carpeta de Lumosity (la de "Rondas de
calentamiento") es en realidad de NeuroNation ("Letras móviles").

## ¿Les ha ido bien? Lo que muestran ellas mismas

| App | Señal visible | Cómo se financia |
|---|---|---|
| Lumosity | "Más de 100 millones de personas han entrenado su memoria"; "8 mil millones de sesiones"; un usuario con racha de 4.500 días; ofrece LumosityRx, "terapia digital aprobada por la FDA" para atención en adultos con TDAH | Prueba gratis de 7 días, luego $66.000/año (anual "más popular"), $13.200/mes o plan familiar de 5 usuarios ($111.000/año) |
| NeuroNation | Respaldo de universidades (Edith Cowan, Freie Universität Berlin) y el sello "Google's Best Apps"; "30 ejercicios en 250 niveles" | $2.800 el primer mes, o pago único $49.000 (-30% con cuenta regresiva de oferta) |
| Peak | "Puntaje mental" propio; socio Universidad de Cambridge (juego de memoria "Wizard"); 30+ juegos en 5 categorías | Prueba de 1 semana de "Peak Pro"; casi todos los juegos aparecen bloqueados |

Lo que comparten: los tres son **freemium con la mayoría de los juegos bloqueados**, tienen **sesión diaria**, **racha**,
**puntaje por área** y **comparación con otras personas**. Ese es el patrón que sostienen los tres.

## Cómo se ve cada una

| | Lumosity | NeuroNation | Peak |
|---|---|---|---|
| Navegación | Hoy · Juegos · Mi cerebro (LPI / Entrenamiento / Análisis) | Sesión · Extras · Premium · Evaluación · Perfil | Hoy · Todos los juegos · Estadísticas · Tests · Yo |
| Puntaje | LPI por área (Memoria 927, Atención 666…) y global | Porcentaje por área (Velocidad 48%, Memoria 80%) y "Nivel 1 – Novato" | "Puntaje mental" 184/1000 y una barra por área |
| Comparación | "Superior al 52% de la gente" tras la prueba inicial; "Cómo te comparas" (premium) | Percentil en logros ("percentil 90 de tus compañeros") | "Tú vs. tu grupo de edad": percentil 11 |
| Plan del día | "Sesión de entrenamiento diaria · 3 juegos" + "Ejercicios de hoy" | Sesión con 5 puntos encadenados (por área) | Tarjeta "Entrenamiento mental" del día |
| Juegos | Tira horizontal por área, con candados y un buscador | Ficha con nivel y "~3 ejercicios para subir de nivel" | Rejilla por área con contador y candados |
| Estilo | Noche azul, naranja para actuar, títulos con serif, cada juego con su textura (papel, madera, fieltro verde) | Azul petróleo, ilustraciones planas de personas | Blanco y celeste, color por área |

## Lo que hacen bien y conviene tomar

Ordenado por lo que más valor da con menos esfuerzo. Para cada uno: qué vimos, qué tiene Nubi hoy y qué proponer.

### 1. Registro diario de sueño y ánimo (Lumosity)
- **Vimos:** antes de entrenar pregunta "¿Cuántas horas dormiste anoche?" (≤5 a 9+) y "¿Cómo te sientes hoy?" (5 caritas),
  con "Omitir" y "Detén estas verificaciones". Después muestra tendencias del estado de ánimo y patrones de sueño.
- **Nubi hoy:** nada parecido.
- **Propuesta:** dos preguntas opcionales de un toque, en Hoy, y una vista simple "cuando duermes 7 horas o más, tu
  juego sube así". Datos locales, apagable en Ajustes. Encaja con el enfoque social y el perfil de Ricardo (psicólogo),
  cuesta poco y da un motivo para volver. Ojo con la regla de las medidas: solo decir lo que la persona registró, sin
  interpretar de más. Nueva preferencia = decidir su respaldo en `BackupRulesTest`.

### 2. Tutorial de primera vez con "Saltar tutorial" y tarjeta "X puso a prueba tu Y" (Lumosity)
- **Vimos:** cada juego abre con una tarjeta (ícono, nombre, una línea) y "Comenzar tutorial"; el tutorial se juega con
  cartel corto y botón "Saltar tutorial" siempre visible; al terminar, una tarjeta explica "Matriz de memoria puso a
  prueba tu memoria espacial", con el respaldo científico en dos párrafos.
- **Nubi hoy:** ya estaba anotado como pendiente ("tutorial de primera vez por juego", `CLAUDE.md`). Las medidas del
  final son lo más cercano a la tarjeta de "puso a prueba".
- **Propuesta:** una plantilla común de tutorial en Unity (cartel + mano de Nubi + "Saltar") y la tarjeta de cierre
  reutilizando el texto de `docs/medidas-juegos-estrella.md`. Empezar por 2 juegos y replicar.

### 3. Pausa con menú completo (Lumosity)
- **Vimos:** Reanudar · Reiniciar · Silenciar · Silenciar música · Salir · Cómo jugar.
- **Nubi hoy:** la pausa existe (`paused_game`); falta comprobar cuáles de esas opciones tiene cada juego.
- **Propuesta:** dejar la misma lista en todos los juegos y que "Cómo jugar" repita el tutorial (junto con el punto 2).

### 4. Comentarios por juego dentro de la app (Lumosity)
- **Vimos:** "Enviar comentarios" con dos pestañas (ajustes generales / acerca de un juego), calificación de 5 estrellas
  opcional y cuadro "¿Qué te gustó? ¿Qué podría mejorar?".
- **Nubi hoy:** solo el informe de errores (Ajustes).
- **Propuesta:** un "¿Cómo te fue este juego?" (👍/👎 y texto opcional) en la pantalla de resultado, que se comparte igual
  que el informe de errores. Para las pruebas con personas reales es lo que faltaba para calibrar cada juego.

### 5. Racha más amable: escudos y calendario (Lumosity)
- **Vimos:** "Escudos de racha" (2 monedas), barra hacia la siguiente meta ("2 días más"), calendario de 4 semanas con
  llama en los días activos y "Últimas 4 semanas: días activos, partidas".
- **Nubi hoy:** racha con hitos 3/7/14/30 y camino de nodos día a día.
- **Propuesta:** un escudo que perdona un día perdido (mucho más humano para adultos mayores que ver la racha en cero) y
  el resumen de 4 semanas en Perfil.

### 6. Atajos "Reforzar" y "Rápido" (Lumosity)
- **Vimos:** bajo "Más entrenamientos": Matemáticas, Favoritos, **Reforzar** ("tus puntuaciones en los juegos que peor
  se te dan") y **Rápido** ("8 minutos o menos").
- **Nubi hoy:** el camino de 3 juegos ya elige por el área que más lo necesita, pero no hay atajos elegibles.
- **Propuesta:** en Hoy, dos botones: "Reforzar tu área más baja" y "Sesión corta (2 juegos)". Favoritos, más adelante.

### 7. Cuánto falta para subir de nivel (NeuroNation)
- **Vimos:** "Nivel 3/9", una barra y "~3 ejercicios para subir de nivel · ¡Estás en camino al éxito!"; además "Modo de
  aprendizaje sin límite de puntuación ni tiempo".
- **Nubi hoy:** ficha de juego con avance y última marca; el modo Suave y Precisión cubren el "sin presión".
- **Propuesta:** en la ficha, una estimación honesta de partidas para subir de etapa (sale del DDA), rotulada como
  estimación. Es un motor de motivación barato.

### 8. Estilo de motivación en el onboarding (Lumosity)
- **Vimos:** "¿Qué forma de darte ánimos te ayuda?": Palmadas en el hombro ("quiero celebrar todos mis logros") o
  Entrenador firme ("necesito disciplina"); también "nivel de dificultad: Estándar o Avanzado" y fijar hora de
  recordatorio dentro del onboarding.
- **Nubi hoy:** pregunta nombre, edad, educación, metas y días por semana.
- **Propuesta:** sumar esa pregunta y que cambie el tono de Nubi (mensajes de resultado y recordatorios). Y proponer la
  hora del recordatorio al final del onboarding.

### 9. Accesibilidad como filtro (Peak y Lumosity)
- **Vimos:** Peak: "¿Eres daltónico? Ocultaremos los juegos que no son adecuados para ti", "¿Tienes dislexia?", "Juegos
  del lenguaje si Peak no está en tu idioma". Lumosity: "Juegos ocultos", tema Oscuro/Claro/Sistema y aviso sobre colores
  y contraste.
- **Nubi hoy:** nunca se depende solo del color, pero no se puede ocultar un juego.
- **Propuesta:** "Juegos ocultos" en Ajustes y revisar Tinta o Palabra (depende de leer el color). Sirve mucho con el
  público mayor.

### 10. Ideas de bienestar (NeuroNation "NeuroActividades")
- **Vimos:** pestañas Cuerpo y Mente con ejercicios de 1-2 minutos: estiramientos, respiración, relajar los ojos, calma
  instantánea, rompecabezas creativo.
- **Nubi hoy:** fuera del núcleo.
- **Propuesta (más adelante):** un cierre de sesión de un minuto de respiración con Nubi. Queda guardado, no urgente.

## Lo que NO conviene copiar

- **Promesas de mejora.** Lumosity predice "mejorarás tu LPI un 30-40% en un mes" en el onboarding. Nubi no debe prometer
  eso: la regla del proyecto es medir lo que mide la partida y nada más. (De memoria, Lumosity pagó una multa a la FTC de
  EE. UU. en 2016 por afirmaciones de este tipo; conviene comprobar el detalle antes de citarlo.)
- **Pruebas de salud.** Peak ofrece pruebas de desgaste, de CI y una de "demencia de inicio precoz" ("Próximamente"). Va
  en contra de "social, no clínico"; un puntaje que parezca diagnóstico es un riesgo legal.
- **Onboarding largo antes de ver nada.** Lumosity hace unas 35 pantallas y recién después muestra el pago. Nubi entra
  en menos y sin cuenta.
- **Presión para crear cuenta y comprar.** Peak pone una franja roja con advertencia ("¡Asegura tu progreso!") y
  NeuroNation una cuenta regresiva de oferta. Nubi guarda todo en el teléfono y respalda solo.
- **Un puntaje "cerebral" único.** Los tres muestran uno (LPI, puntaje mental Peak). Sin datos propios ni validación,
  un número así se lee como diagnóstico. Si se hace, que sea un "nivel de juego" y no una medida cognitiva.

## Dónde Nubi ya está mejor

- 19 juegos abiertos desde el primer día, ninguno detrás de un candado.
- Medidas propias por juego estrella al final (freno, seguimiento, brújula interna), con su respaldo.
- Sin cuenta, sin conexión, con respaldo del progreso y registro de errores.
- Letra y contraste pensados para adultos mayores y sin depender solo del color.
- Un personaje propio (Nubi) y un tema que une todo, en vez de un menú de ejercicios sueltos.

## Diseño que vale la pena mirar

- **Textura por juego.** Lumosity cambia el fondo según el juego (cartón marrón, fieltro verde, madera) y mantiene la
  app en azul noche. Nubi ya tiene su "arcilla"; se podría dar a cada área su fondo propio dentro del juego.
- **Títulos con serif en las tarjetas de juego** ("A todo vapor", "Comparación de colores") frente a texto sans: da
  personalidad. Nubi usa Fredoka + Nunito; no hace falta cambiar, solo saber que el contraste editorial funciona.
- **HUD claro y pequeño:** "PRUEBA 11/15 · PUNTUACIÓN 23400", multiplicador de combo con cuatro puntos y "bonificación de
  puntuación". Coincide con lo que ya se hizo en varios juegos.
- **Color por área** (Peak: naranja memoria, verde razonamiento, morado lenguaje…). Nubi ya lo hace.

## Orden sugerido

1. Registro de sueño y ánimo (1).
2. Tutorial + "puso a prueba tu…" + pausa completa (2 y 3): es lo que ya estaba pendiente.
3. Comentarios por juego (4): ayuda a todo lo demás.
4. Escudo de racha y resumen de 4 semanas (5).
5. Atajos Reforzar y Rápido (6) y "faltan N partidas" (7).
6. Estilo de motivación y hora del recordatorio en el onboarding (8), juegos ocultos (9).
7. Pendiente de decisión de Ricardo: puntaje global, comparación con otras personas (necesita datos reales), precios.

## Para saber si "les ha ido bien" de verdad

Las capturas solo muestran lo que ellas cuentan. Para evidencia externa habría que buscar aparte: calificaciones y número
de reseñas en Google Play, descargas, rankings de la categoría y estudios publicados sobre cada app. Se puede hacer si se
pide.
