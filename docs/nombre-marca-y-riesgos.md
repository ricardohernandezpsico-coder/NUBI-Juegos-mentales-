# Nombre, ícono y riesgos legales (28-sep)

Pedido de Ricardo: definir el nombre y el ícono de la app "considerando temas legales, marcas registradas, etc.",
y revisar lo que decía una conversación con Gemini sobre BrainHQ y sus ejercicios.

> **No es asesoría legal.** Es una revisión hecha con búsquedas públicas en internet para decidir con más
> información. Antes de publicar (sobre todo en Estados Unidos) conviene que un abogado de marcas y patentes revise
> el nombre elegido y el juego Radar (ver más abajo).

Maquetas: `python3 tools/previews/nombre_icono.py` → `docs/previews/nombre-icono.png` (y cada ícono suelto en
`docs/previews/icono-<nombre>.png`); segunda ronda de nombres: `tools/previews/nombres_mas.py` →
`docs/previews/nombres-mas.png`; retoque de Radar: `tools/previews/radar_retoque.py` → `docs/previews/radar-retoque.png`.

## 1. "NeuroVida" no sirve como nombre final

- **Ya es marca registrada en EE. UU.**: NEUROVIDA, registro 6563086 (clase 5, suplementos alimenticios). Es otra
  clase, pero el público es parecido (salud del cerebro) y eso aumenta el riesgo de confusión.
- **Ya hay una app "NEUROVIDA PSICOLOGIA" en Google Play.** Misma tienda, mismo rubro (psicología).
- Además, "Neuro-" suena clínico, y la idea de Ricardo es una app masiva y motivadora, no clínica.

Conclusión: cambiar el nombre antes de publicar. El `applicationId` técnico (`com.aistudio.neurovida.cgnv`) se
cambia al final, junto con la firma y la tienda (cambiarlo = app nueva), como ya estaba acordado.

## 2. Nombres propuestos

Criterios: palabra INVENTADA (las marcas inventadas son las más fáciles de registrar y proteger; las palabras
comunes como "Órbita" o "Mente Clara" ya están tomadas o no se pueden registrar), en español, fácil de decir para
un adulto mayor, que calce con el mundo de la app (espacio, planeta, estrellas), sin "neuro" y sin prometer salud.

| Nombre | Idea | Búsqueda rápida (28-sep) | Comentario |
|---|---|---|---|
| **Cosmente** | cosmos + mente; suena a "cósmicamente" | Sin apps ni marcas con ese nombre. Hay "CosMe" / "Cosmee" (cosmética). | **Recomendado.** Corto (8 letras, cabe entero bajo el ícono), se entiende en español y se pronuncia en inglés. Parecido lejano a marcas de cosmética: otra clase, pero un abogado debe mirarlo. |
| **Luminautas** | navegantes de la luz; "¡Hola, luminauta!" | Sin app ni marca de software. "Luminauta" lo usan un estudio de diseño, una lámpara y cuentas de Instagram. | El más social: le da nombre a la comunidad (la persona ES una luminauta). **Baja al 2.º grupo** (revisión del 28-sep): "Lumi-" suena a LUMOSITY / Lumos Labs, la marca más conocida del mismo rubro (juegos para la mente); un parecido así es lo primero que mira una oficina de marcas. |
| **Astromente** | astro + mente | Sin resultados. | Claro, pero "astro" acerca a la astrología. |
| **Mentaluna** | mente + luna | Sin resultados ("Luna Menta" es una joyería en Londres). | Suave y bonito; menos energía que los dos primeros. |
| **Orbimente** | órbita + mente | Sin resultados. | Correcto, pero cuesta un poco decirlo. "Orbi" es una marca de Netgear (routers, clase 9): parecido parcial. |

### Segunda ronda (pedido de Ricardo: "dame más opciones")

Lámina: `python3 tools/previews/nombres_mas.py` → `docs/previews/nombres-mas.png`.

| Nombre | Idea | Búsqueda rápida (28-sep) | Comentario |
|---|---|---|---|
| **Planetea** | "planetear": hacer crecer tu planeta | Sin apps ni marcas con ese nombre. | **Muy bueno**: el nombre ES lo que se hace en la app (la pestaña Hoy es "Tu planeta"); corto y alegre. Ojo: se parece a "Planeta" (Grupo Planeta, editorial grande con marcas en libros y medios): que el abogado lo mire. |
| **Lunamente** | luna + mente; suena a adverbio ("vivir lunamente") | Solo una cuenta de Instagram en portugués; ninguna app. | Suena mejor que Mentaluna (fluye como palabra del idioma). |
| **Estrellamente** | estrella + mente ("brillar estrellamente") | Sin resultados. | Bonito pero largo (13 letras): en algunos teléfonos se corta bajo el ícono. |
| **Pensastro** | pensar + astro ("¡piensa, astro!") | Sin resultados. | Juguetón, con humor; menos serio. |
| **Pensaluna** | pensar + luna | Sin resultados. | Suave y fácil de decir. |

**Orden sugerido con las dos rondas**: 1) **Cosmente**, 2) **Planetea**, 3) Lunamente, 4) Astromente,
5) Estrellamente; después Pensastro, Pensaluna, Mentaluna, Orbimente y Luminautas.

Descartados en la búsqueda (ya existen o chocan con el rubro): **Mentenautas** (podcast de salud mental),
**Mentaluz** (empresa de salud mental en Chile), **Mentelar** (organización de educación y salud), **Mentaria**
(proyecto "próximamente"), **Kosmi** (app de juegos y fiestas virtuales), **Galaxio** (juego de puzzle espacial),
**Constela** (app de viajes y agencia en México), **Astrelia** (app de astrología), **Estelaria** (perfumes y
autoayuda), **Orbelia** (sitio de bienestar), **Lunio** (varias apps), **Tripulia** (jerga en portugués),
**Orbitea** (software de viajes), **Órbita** / **Nova** / **Aurora** (palabras comunes, muy tomadas).

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

## 5. Juegos y patentes: riesgo y retoques

Las patentes valen **solo en el país donde se registraron** y por 20 años desde que se pidieron. Las de abajo son de
EE. UU. (algunas de Akili también de Japón): si la app se lanza primero en Chile o Latinoamérica, no aplican allá,
salvo que la empresa haya registrado la misma patente en esos países (su "familia": hay que revisarlo).

**Cómo se "roza el límite" sin pasarlo.** Una patente se infringe solo si el juego hace TODOS los pasos de un
reclamo. Si un paso falta de verdad, el juego queda fuera. Ojo: en EE. UU. un cambio que es solo de apariencia (otro
dibujo, otro nombre) puede contar igual ("doctrina de equivalentes"); el retoque tiene que cambiar lo que la persona
HACE. Por eso los retoques de abajo cambian pasos, no dibujos.

### Radar — riesgo ALTO hoy → retoque propuesto (maqueta: `docs/previews/radar-retoque.png`)

US 8,348,671 (Posit Science, "entrenamiento de atención visual dividida", vence ~nov. 2031). Su reclamo 1 exige,
todo junto: (a) una imagen en el centro y a la vez (b) una ubicación marcada en la periferia, por un tiempo, y se
apagan; (c) mostrar dos o más imágenes candidatas y (d) exigir que se elija la del centro entre ellas; (e) SOLO si
acertó, pedir la ubicación de la periferia; (f) ajustar el tiempo de exposición según las respuestas; (g) repetir.
Radar hoy hace (a), (b), (c), (d), (f) y (g); lo único distinto es que pregunta la dirección siempre, no solo si
acertó.

**Retoque (3 cambios chicos, uno por paso del reclamo):**

1. **Tu nave de rescate, antes.** Al empezar (y cada 5 rondas) se muestra la nave de la misión: "Si pasa por el
   centro, dilo al final de la ronda".
2. **Primero ¿dónde?**: después de la interferencia se pregunta SIEMPRE primero dónde estaba el astronauta.
3. **Después "¿Pasó tu nave por el centro?" SÍ / NO.** Ya no se muestran naves para elegir: se responde con dos
   botones de texto. En la mitad de las rondas pasa tu nave; en la otra mitad, otra (desde el nivel 5 del mismo
   color, desde el 9 de silueta parecida: la misma dificultad de hoy).

Con esto faltan los pasos (c) y (d) (no hay candidatas ni se elige entre imágenes) y el (e) (la ubicación no depende
de acertar el centro, y va primero). **Se mantiene todo lo que importa**: el destello que se acorta con la escalera
(lo central del entrenamiento de velocidad), las 8 direcciones y 3 anillos, los asteroides, la interferencia,
"rescatado" cuando las dos respuestas están bien, **tu vistazo** y **tu radar**. Ciencia: sigue siendo atención
dividida centro + periferia bajo un destello cada vez más breve; la tarea del centro pasa de "identificar" a
"reconocer" (sí/no), con el mismo 50% de azar que hoy. Trabajo: `RadarContract` (ronda con o sin tu nave), el orden
de preguntas en `RadarGameController`, pruebas, y cambiar la marca de verificación de Unity. Riesgo después:
**bajo-medio**. Aun así, que un abogado lo mire antes de publicar en EE. UU. (Posit tiene más patentes de esa familia
que no revisé).

### Piloto Estelar — riesgo bajo; sin retoque, con reglas

- US 9,940,844 (Universidad de California / Akili, el NeuroRacer, ~2032): todos sus reclamos exigen un sensor de
  movimiento o de posición. Piloto se maneja con el dedo. **Regla: nunca manejar inclinando el teléfono.**
- Revisé también otras patentes de Akili (hay decenas): **US 11,839,472** exige un "clasificador" que calcula un
  perfil de respuesta impulsivo o conservador variando el plazo para responder; **US 11,507,178** exige caras que
  expresan emociones y que cambian en tiempo real según el desempeño; **US 11,304,657** exige un sensor fisiológico
  (pulso, EEG). Piloto no hace nada de eso. **Reglas para todos los juegos**: no calcular un "perfil impulsivo /
  conservador"; no usar caras con emociones que reaccionen a cómo le va a la persona (por ejemplo, un astronauta que
  sonríe o se entristece según los aciertos); no usar sensores del cuerpo.
- El **costo de multitarea** (caída entre piloto automático y a los mandos) es una medida clásica de doble tarea
  (décadas de laboratorio), pero Akili la describe en sus patentes: va a la lista del abogado.

### Sistema de avance (no es un juego) — riesgo bajo-medio; con reglas

US 10,559,221 (Akili, vence ~2036): evaluación inicial → "máximo rendimiento" de la persona → un rango personal →
el rango se divide en "puertas" de avance → tareas de esa puerta → si la supera, la siguiente, siempre dentro del
rango personal. Lo nuestro es distinto en lo esencial: el punto de partida solo fija el nivel de inicio; las etapas
Inicio…Maestro son quintos de la escala COMÚN de cada juego (iguales para todos), no un rango personal partido; y la
dificultad se mueve ensayo a ensayo (la escalera), no por puertas. **Reglas**: (1) las etapas quedan en la escala
común; no calcular un "máximo personal" para partirlo en escalones; (2) el re-chequeo mensual del punto de partida
(pendiente) va por calendario y no rearma rangos; (3) Desafío y Experto son modos que se eligen, no puertas
obligatorias para avanzar. Como Desafío → Experto se parece un poco a una "puerta", va a la lista del abogado.

### Los demás

| Juego | Qué encontré | Riesgo | Regla |
|---|---|---|---|
| **Parejas** | US 7,540,615 (Posit, ~2026-2028): exige mostrar las cartas UNA POR UNA en secuencia. | Bajo | Mantener el tablero completo a la vez. |
| **Satélites** | Las patentes de NeuroTracker (Faubert / CogniSens: US 9,566,029 y US 10,706,730) describen un ambiente 3D estereoscópico e inmersivo; el seguimiento de varios objetos en sí es un paradigma público (Pylyshyn y Storm, 1988). | Bajo | Satélites es plano: nada de 3D estereoscópico ni realidad virtual. |
| **Tráfico Estelar** | La patente de Lumos Labs encontrada (US 8,821,242) es de un juego tipo pinball; no encontré patente del juego de trenes. | Bajo | Nombre, arte, sonidos y medidas propios (ya los tiene); nada de trenes ni estaciones de tren. |
| Demás juegos | Stroop (1935), señal de alto (Logan, 1984), línea numérica (Siegler, 2003), rotación mental (Shepard, 1971), integración de trayecto, memoria episódica y prospectiva: tareas clásicas y públicas. | Bajo (no revisado a fondo) | Nada por ahora. |

**Nombres que nunca se usan** (ni en la app, ni en la tienda, ni en publicidad): UFOV, Double Decision, Target
Tracker, BrainHQ, NeuroRacer, EndeavorRx, NeuroTracker, Train of Thought, Lumosity, Peak, Elevate. En los documentos
internos, como referencia científica, sí.

**Lista para el abogado** (cuando llegue el momento de publicar): el nombre elegido; Radar con el retoque; el costo
de multitarea de Piloto; el sistema de avance (Desafío → Experto) frente a US 10,559,221.

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
- US 11,839,472 (Akili): https://patents.google.com/patent/US11839472
- US 11,507,178 (Akili): https://image-ppubs.uspto.gov/dirsearch-public/print/downloadPdf/11507178
- US 11,304,657 (Akili): https://image-ppubs.uspto.gov/dirsearch-public/print/downloadPdf/11304657
- US 10,559,221 (Akili): https://patents.google.com/patent/US10559221B2/en
- Patentes de NeuroTracker (Faubert): https://image-ppubs.uspto.gov/dirsearch-public/print/downloadPdf/9566029 · https://patents.google.com/patent/US10706730B2/en
- Tetris contra Xio: https://en.wikipedia.org/wiki/Tetris_Holding,_LLC_v._Xio_Interactive,_Inc.
- FTC y Lumosity (2016): https://www.ftc.gov/news-events/news/press-releases/2016/01/lumosity-pay-2-million-settle-ftc-deceptive-advertising-charges-its-brain-training-program
- Podcast Mentenautas: https://podcasters.spotify.com/pod/show/mentenautas-podcast
