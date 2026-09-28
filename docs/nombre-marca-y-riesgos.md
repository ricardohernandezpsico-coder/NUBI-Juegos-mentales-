# Nombre, ícono y riesgos legales (28-sep)

Pedido de Ricardo: definir el nombre y el ícono de la app "considerando temas legales, marcas registradas, etc.",
y revisar lo que decía una conversación con Gemini sobre BrainHQ y sus ejercicios.

> **No es asesoría legal.** Es una revisión hecha con búsquedas públicas en internet para decidir con más
> información. Antes de publicar (sobre todo en Estados Unidos) conviene que un abogado de marcas y patentes revise
> el nombre elegido y el juego Radar (ver más abajo).

Maqueta: `python3 tools/previews/nombre_icono.py` → `docs/previews/nombre-icono.png` (y cada ícono suelto en
`docs/previews/icono-<nombre>.png`).

## 1. "NeuroVida" no sirve como nombre final

- **Ya es marca registrada en EE. UU.**: NEUROVIDA, registro 6563086 (clase 5, suplementos alimenticios). Es otra
  clase, pero el público es parecido (salud del cerebro) y eso aumenta el riesgo de confusión.
- **Ya hay una app "NEUROVIDA PSICOLOGIA" en Google Play.** Misma tienda, mismo rubro (psicología).
- Además, "Neuro-" suena clínico, y la idea de Ricardo es una app masiva y motivadora, no clínica.

Conclusión: cambiar el nombre antes de publicar. El `applicationId` técnico (`com.aistudio.neurovida.cgnv`) se
cambia al final, junto con la firma y la tienda (cambiarlo = app nueva), como ya estaba acordado.

## 2. Cinco nombres propuestos

Criterios: palabra INVENTADA (las marcas inventadas son las más fáciles de registrar y proteger; las palabras
comunes como "Órbita" o "Mente Clara" ya están tomadas o no se pueden registrar), en español, fácil de decir para
un adulto mayor, que calce con el mundo de la app (espacio, planeta, estrellas), sin "neuro" y sin prometer salud.

| Nombre | Idea | Búsqueda rápida (28-sep) | Comentario |
|---|---|---|---|
| **Cosmente** | cosmos + mente; suena a "cósmicamente" | Sin apps ni marcas con ese nombre. Hay "CosMe" / "Cosmee" (cosmética). | **Recomendado.** Corto (8 letras, cabe entero bajo el ícono), se entiende en español y se pronuncia en inglés. Parecido lejano a marcas de cosmética: otra clase, pero un abogado debe mirarlo. |
| **Luminautas** | navegantes de la luz; "¡Hola, luminauta!" | Sin app ni marca de software. "Luminauta" lo usan un estudio de diseño, una lámpara y cuentas de Instagram. | **Segundo.** El más social: le da nombre a la comunidad (la persona ES una luminauta). 10 letras: cabe. |
| **Astromente** | astro + mente | Sin resultados. | Claro, pero "astro" acerca a la astrología. |
| **Mentaluna** | mente + luna | Sin resultados ("Luna Menta" es una joyería en Londres). | Suave y bonito; menos energía que los dos primeros. |
| **Orbimente** | órbita + mente | Sin resultados. | Correcto, pero cuesta un poco decirlo. |

Descartados en la búsqueda: **Mentenautas** (ya existe un podcast de salud mental con ese nombre, mismo rubro),
**Orbitea** (proyecto de software de viajes), **Órbita** / **Nova** / **Aurora** (palabras comunes, muy tomadas).

**Lo que falta antes de decidir en firme** (no se puede hacer bien desde aquí, las bases oficiales no responden a
búsquedas automáticas):

1. Buscar el nombre elegido en las bases oficiales: **INAPI** (Chile), **IMPI** (México), **TMview** (Unión Europea
   y muchos países), **USPTO** (EE. UU.) y la **Base Mundial de Marcas de la OMPI** (WIPO Global Brand Database).
2. Clases de Niza donde registrar: **9** (aplicación descargable), **41** (juegos en línea, entretenimiento,
   educación) y, si más adelante hay suscripción web, **42** (software en línea). Mejor NO la 44 (servicios médicos
   o psicológicos): refuerza la idea de app no clínica.
3. Revisar que estén libres el nombre en Google Play y App Store, el dominio (.com y .cl) e Instagram / TikTok.
4. Registrar primero en el país donde se lance (INAPI si es Chile) y, si va bien, extender por el Protocolo de
   Madrid.

## 3. El ícono

Cinco ideas, una por nombre (se pueden mezclar): ver la lámina. Todas en "noche + arcilla" (cielo nocturno de la
app, piezas de arcilla con borde tinta y sombra dura), **sin cerebros** (los usan casi todas las apps de la
competencia y suena clínico) y sin letras (a 48 dp no se leen).

- **Cosmente → "Tu planeta"**: el planeta de la pestaña Hoy, con sus zonas de colores, un anillo sol y una estrella.
  Une el ícono con lo primero que se ve al abrir la app.
- **Luminautas → el casco**: el casco del astronauta con una estrella en la visera. Personaje, más cálido.
- Astromente → la chispa (destello sol sobre una órbita); Mentaluna → luna creciente con estrella lima;
  Orbimente → sol con dos órbitas (ojo: se parece al átomo de muchos logos de ciencia).

En la maqueta cada ícono se ve como lo muestra Android: solo el centro (72 de 108 dp) y recortado en círculo o en
cuadrado redondeado según el teléfono; abajo, en tamaño real en una pantalla de inicio.

## 4. Lo que decía Gemini sobre BrainHQ: qué es cierto y qué hay que corregir

- **Cierto**: la evidencia de BrainHQ es real. El ensayo ACTIVE (Ball et al., JAMA 2002) usó el entrenamiento de
  velocidad de procesamiento que hoy es "Double Decision", y los seguimientos a 10 y 20 años asociaron ese
  entrenamiento con menos riesgo de demencia (el de 2026 salió en la prensa en febrero). Ojo: esa evidencia es de
  SU ejercicio y SU protocolo; nosotros no podemos usarla para prometer nada de nuestra app.
- **Hay que corregir "ejercicios con copyright"**: el copyright (derecho de autor) protege la EXPRESIÓN (el código,
  el dibujo, los sonidos, los textos), no la mecánica de un juego ni la tarea científica (caso Tetris contra Xio,
  EE. UU., 2012: se puede usar la misma regla, no copiar el aspecto). Los NOMBRES de los ejercicios son marcas:
  no se usan. Lo que sí puede proteger una mecánica es una **PATENTE**, y Posit Science (la empresa de BrainHQ)
  anunció más de 90. Ese es el riesgo real, y está revisado juego por juego en la sección 5.
- De la lista de Gemini (Double Decision, Target Tracker, Hawk Eye, Visual Sweeps, Sound Sweeps, Fine Tuning,
  Memory Grid, Syllable Stacks, To-Do List Training, In the Know), la mayoría son auditivos o de lenguaje que no
  tenemos. Los que se parecen a juegos nuestros: **Double Decision ↔ Radar** y **Target Tracker ↔ Satélites**.

## 5. Juegos y patentes: riesgo de cada uno

Las patentes valen **solo en el país donde se registraron** y por 20 años desde que se pidieron. Las de abajo son de
EE. UU.: si la app se lanza primero en Chile o Latinoamérica, no aplican allá, salvo que la misma empresa haya
registrado la patente también en esos países (hay que revisarlo; es lo que se llama "familia" de la patente).

| Juego | Patente que se parece | Riesgo | Qué hacer |
|---|---|---|---|
| **Radar** | US 8,348,671 (Posit, "entrenamiento de atención visual dividida", vence ~nov. 2031). Su reclamo principal: blanco en el centro y otro en la periferia a la vez → se apagan → elegir el del centro entre opciones → si acertó, marcar dónde estaba el de la periferia → el tiempo de exposición se adapta. | **ALTO** | Es casi la misma secuencia. Antes de publicar en EE. UU.: revisión de un abogado de patentes ("libertad de operación"). Diferencias a mostrarle: en Radar SIEMPRE se pregunta la dirección (no solo si acertó el centro) y se cuentan las dos respuestas juntas; tema de rescate, arte y medida propios. Si el abogado lo ve riesgoso: rediseñar (por ejemplo, preguntar primero la dirección) o dejar Radar fuera en EE. UU. hasta 2031. |
| **Piloto Estelar** | US 9,940,844 (Universidad de California / Akili, el NeuroRacer; ~2032). Todos sus reclamos exigen un sensor de movimiento o de posición (inclinar el teléfono, cámara, equipo de ejercicio). | Bajo-medio | Piloto se maneja con el dedo: queda fuera. **Nunca** agregar manejo inclinando el teléfono. |
| **Parejas** | US 7,540,615 (Posit, ~2026-2028): exige mostrar las cartas UNA POR UNA en secuencia. | Bajo | Parejas muestra el tablero completo a la vez: mantenerlo así. |
| **Radar (nombre)** | "UFOV®" es marca registrada (la patente original del UFOV, US 4,971,434, ya venció). | Bajo | No usar "UFOV" en la app ni en la tienda (en los documentos internos, como referencia científica, está bien). |
| **Satélites** | No encontré patente aplicable; el seguimiento de múltiples objetos es un paradigma de laboratorio público (Pylyshyn y Storm, 1988). | Bajo | Mantener nombre, arte y medida propios; no usar "Target Tracker" ni "NeuroTracker". |
| **Tráfico Estelar** | La patente de Lumos Labs encontrada (US 8,821,242) es de un juego tipo pinball; no encontré patente del juego de trenes. | Bajo | Ya tiene nombre, arte, sonidos y medidas propios. |
| Demás juegos | Stroop (1935), señal de alto (Logan, 1984), línea numérica (Siegler, 2003), rotación mental (Shepard, 1971), integración de trayecto, memoria episódica y prospectiva: tareas clásicas y públicas. | Bajo (no revisado a fondo) | Nada por ahora. |

## 6. Publicidad y textos: la lección de Lumosity

En 2016 Lumosity pagó 2 millones de dólares (con una condena suspendida de 50 millones) a la FTC de EE. UU. por
decir, sin pruebas suficientes, que sus juegos mejoraban el rendimiento en el trabajo y el colegio y retrasaban el
deterioro por la edad o la demencia. Regla para la tienda, la publicidad y los textos de la app (ya la seguimos):

- Nada de "previene", "retrasa", "mejora tu memoria", "demencia", "Alzheimer" ni nombres de enfermedades.
- Sí: "juegos para entrenar la atención, la memoria…", "mide cómo te fue", "no es un diagnóstico".
- No apropiarse de estudios ajenos: "basado en el ejercicio del estudio ACTIVE" sugiere que nuestra app tiene esa
  evidencia, y no la tiene.
- El aviso de Ajustes ("aplicación para el entretenimiento… No constituye diagnóstico") se mantiene.

## 7. Cuando Ricardo elija

Cambio chico y verificable (sin tocar el `applicationId`, que queda para el final):

1. `app_name` en `res/values/strings.xml`.
2. Ícono adaptativo nuevo (`drawable/ic_launcher_foreground.xml` + fondo, y el monocromo de Android 13) hecho a
   partir de la maqueta; revisar también el ícono de notificaciones (`ic_stat_neurovida`).
3. Los 6 textos que dicen "NeuroVida": compartir logros y ligas, tarjeta para compartir, bienvenida del onboarding y
   aviso de Ajustes.
4. En Unity, `productName` (hoy `NeuroVidaCore`: no se ve, se puede dejar).
5. Captura Roborazzi de la bienvenida con el nombre nuevo.

## Fuentes

- Marca NEUROVIDA (EE. UU.): https://trademarks.justia.com/871/21/neurovida-87121356.html
- App NEUROVIDA PSICOLOGIA: https://play.google.com/store/apps/details?id=app.superconsole.android68ba166603fe2
- Double Decision: https://www.brainhq.com/why-brainhq/about-the-brainhq-exercises/attention/double-decision/
- Seguimiento 2026 de ACTIVE (prensa): https://www.npr.org/2026/02/18/nx-s1-5716010/brain-training-exercise-cut-dementia-risk-decades
- Posit, 90 patentes: https://www.brainhq.com/news/press-releases/ninety-brain-fitness-patents-for-posit-science/
- US 8,348,671: https://image-ppubs.uspto.gov/dirsearch-public/print/downloadPdf/8348671
- US 7,540,615: https://image-ppubs.uspto.gov/dirsearch-public/print/downloadPdf/7540615
- US 9,940,844: https://patents.google.com/patent/US9940844B2/en
- US 4,971,434 (UFOV original): https://patents.google.com/patent/US4971434/en · marca UFOV®: https://www.visualawareness.com/what-is-ufov/
- US 8,821,242 (Lumos Labs): https://patents.google.com/patent/US8821242
- Tetris contra Xio: https://en.wikipedia.org/wiki/Tetris_Holding,_LLC_v._Xio_Interactive,_Inc.
- FTC y Lumosity (2016): https://www.ftc.gov/news-events/news/press-releases/2016/01/lumosity-pay-2-million-settle-ftc-deceptive-advertising-charges-its-brain-training-program
- Podcast Mentenautas: https://podcasters.spotify.com/pod/show/mentenautas-podcast
