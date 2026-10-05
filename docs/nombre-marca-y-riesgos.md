# Nombre, ícono y riesgos legales (28-sep)

> **Decisión (29-sep): Nubi** ("Nubi – Brain Games", mercado global). Hoja del personaje: `docs/previews/nubi-personaje.png`.

Pedido de Ricardo: definir el nombre y el ícono de la app "considerando temas legales, marcas registradas, etc.",
y revisar lo que decía una conversación con Gemini sobre BrainHQ y sus ejercicios.

> **No es asesoría legal.** Es una revisión hecha con búsquedas públicas en internet para decidir con más
> información. Antes de publicar (sobre todo en Estados Unidos) conviene que un abogado de marcas y patentes revise
> el nombre elegido y el juego Radar (ver más abajo).

Maquetas: `python3 tools/previews/nombre_icono.py` → `docs/previews/nombre-icono.png` (y cada ícono suelto en
`docs/previews/icono-<nombre>.png`); segunda ronda de nombres: `tools/previews/nombres_mas.py` →
`docs/previews/nombres-mas.png`; retoque de Radar (descartado): `tools/previews/radar_retoque.py` → `docs/previews/radar-retoque.png`;
rediseño de Radar: `tools/previews/radar_rescate.py` → `docs/previews/radar-rescate.png`.

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

### Tercera ronda (28-sep, Ricardo: "ningún nombre me convence")

Las dos rondas anteriores eran casi todas "algo + mente" o "algo + luna": mismo molde. Esta vez probé otros moldes
(palabra real cálida, palabra inventada suave, heredar "vida"), y la búsqueda rápida tumbó a la mayoría:

- **Pasaron**: **Vivastro** (vivo + astro; hereda la "vida" de NeuroVida; sin resultados), **Avívate** (solo un
  proyecto universitario; es un llamado a la acción, cercano), **Despegue** (sin app; palabra común, marca más débil).
- **Aluna**: sin app, pero es un concepto sagrado del pueblo kogi (Colombia: el mundo del pensamiento). Usarlo como
  marca sería apropiarse de algo ajeno: **no lo recomiendo**.
- **Descartados**: Brío (BRIO es una marca sueca de juguetes), Asombro (agencia asombro.app), Vidastral (tarot),
  Vivaluz (app de belleza), Vívido (varias apps), Nubelia (empresa de software), Tripulantes (choca con el juego de
  mesa espacial "La Tripulación"), Lumbre (varias apps).

**Antes de otra ronda conviene fijar la dirección** (preguntas para Ricardo): ¿palabra real del español o
inventada?; ¿debe sonar a espacio, a vida/energía o a hábito diario?; ¿corta de 2 sílabas o puede ser más larga?;
¿tiene que funcionar también en inglés?; ¿algún nombre de todas las rondas que "casi" le gustó y por qué no?

### Cuarta ronda (29-sep, Ricardo: "algo asociado a lo mental, como Synapp")

Despegue le recordó a despegar.com; Avívate y Vivastro le sonaron mal. Busqué 25 nombres con palabras del cerebro y la
mente. **Ese vocabulario está casi todo tomado, y muchas veces por apps del mismo rubro**:

- **Tomados**: Synapp (mensajería médica, app agrícola, ciberseguridad), Neuronia (app de juegos cerebrales), Axon
  (varias apps de entrenamiento cerebral), Engrama (app de entrenamiento cognitivo), Mielina (cuenta brasileña de
  estimulación cognitiva 50+, agencia en España; "Myelin" en inglés, apps de entrenamiento cerebral), Neurito (un
  juego en tiendas), Sinaptia y Sinapsia (empresas de software), Sinapsa (educación), Synapto (pagos, IA), Synaptik
  (salud), Dendra / Dendrite (productividad, neurología), Glia (varias apps), Mnemo / Mnema (muchas apps de memoria),
  Cortexa (notas médicas, finanzas), Cerebrito (apps de juegos), Neuralia, Neuru / Neuri / Neura (apps de
  entrenamiento cerebral), Sinapi (banca, educación).
- **Sinastro** (sinapsis + astro): libre como app, pero suena igual que **Sinestro**, un villano de DC Comics.
- **Libres, con reparos**: **Neuronita** (neurona pequeña; libre, pero muy cerca de Neuronia y NeuroNation, que son del
  mismo rubro), **Plastia** (de plasticidad; libre, pero "-plastia" es la terminación de cirugías: rinoplastia),
  **Axolito** (ajolote: el animal que regenera partes de su cerebro; solo aparece como personaje de un juego de
  Roblox y un usuario de GitHub; es simpático y daría una mascota, pero se aleja del espacio).

**Conclusión**: con palabras técnicas del cerebro (sinapsis, neurona, axón, córtex, mielina, engrama) ya no queda
nada libre ni distintivo. Lo que sí se puede es inventar una palabra que *suene* a mente sin ser un término técnico,
o mezclar una sílaba del cerebro con el mundo de la app.

### Quinta ronda (29-sep): los nombres de Ricardo

| Nombre | Qué encontré | Veredicto |
|---|---|---|
| **BrainStride** | Marca REGISTRADA en EE. UU. (BRAIN STRIDE, n.º 88609908, Pentara Corporation, 2021) para coaching de salud cerebral (brainstride.com, prevención de demencia) + una app de trivia "Brain Stride". | Descartado: marca registrada del mismo rubro. |
| **NeuroDash** | Ya hay al menos tres apps de entrenamiento con ese nombre (reacción y memoria, "QuickIQ Memory Game - NeuroDash"), una plataforma para TDAH y un proyecto hospitalario. | Descartado: mismo rubro, mismo nombre. |
| **NeuroIgnite** | Suplemento para el cerebro (NeuroIGNITE, Havasu Nutrition; neuroignite.com), con un expediente de Truth in Advertising sobre sus promesas. | Descartado: marca de suplementos y mala asociación. |
| **Synapta** | Empresa italiana de software (Synapta Srl), plataforma inglesa de apps móviles (synapta.co.uk) y una agencia digital. | Riesgo alto: software, mismo tipo de producto (clase 9/42). |
| **MindKinetics** | No hay app con ese nombre; sí "Mental Kinetics LLC", "Kinetic Mind", BRAINKINETIK® (método de "kinética cerebral") y la editorial Human Kinetics. | Posible, con reparos: largo (12 letras), en inglés y difícil de decir y escribir para un adulto mayor hispanohablante; "kinetics" suena a movimiento físico. |

Variantes probadas en el mismo estilo: MindOrbit (ya existe "MindOrbit: Brain Training Kids", ¡espacial y de entrenamiento!),
MindVolt (app de entrenamiento cerebral y un suplemento), Astromind (apps de astrología), Kinetia (desarrollador de
apps y consultora), Mentek (sin app, pero muy cerca de Mentiks, app de juegos mentales). **Kinemind** solo tiene dominios
estacionados (kinemind.com, kinemind.be): es la versión corta de MindKinetics.

**SEO / ASO (tiendas).** En apps lo que pesa es el buscador de Google Play y App Store, no Google web. El título (30
caracteres) y la descripción corta (80) llevan las palabras clave; el nombre solo tiene que ser **único** para que quien
busca la marca la encuentre primera. Los nombres con "Brain", "Neuro" o "Mind" compiten con miles de apps: nunca serán
dueños de su búsqueda y cuesta que el boca a boca funcione ("¿cómo se escribe?"). Mejor: nombre corto, distinto y fácil de
dictar + título con palabras clave en español, por ejemplo "Nombre: juegos para la mente".

### Sexta ronda (29-sep): mercado GLOBAL, "Brain Games / Juegos mentales" al lado

Ricardo: la app es para el mercado global; el nombre no necesita decir "neuro" si en la tienda lleva "juegos mentales"
al lado. Probé palabras inventadas cortas, fáciles en varios idiomas, asociadas al mundo de la app (órbita, planeta,
cometa, estrella, chispa, mente). Casi todas ya existen, varias en el mismo rubro:

- **Mismo rubro (descartados)**: Brainet (app de entrenamiento cerebral con 40 juegos), MindOrbit (entrenamiento
  cerebral espacial para niños), MindVolt y Mindzy (apps de entrenamiento cerebral), Orbini (app de aprendizaje con
  rachas y XP), MindPlanet / MindPal (juegos mentales), Zenova (app para TDAH).
- **Otro rubro, pero apps o software**: Orbiva (salud en el hogar, Hong Kong), Orbly (dictado y chat), Novami (software),
  Stellio (reproductor de música), Glimo (varias apps de salud), Orbiko (oro y blockchain), Kometo (desarrollador de
  apps), Planeko (app de astronomía y marca de herramientas), Astriko / Astrico (tarot), Cosmind (pedidos de comida),
  Menzu (pizzería), Astromind (astrología).
- **Casi libres**: **Thinkoo** (solo el dominio en venta; ojo: "thinkO" es un juego de lógica y una app de noticias),
  **Brainling** ("cerebrito" en inglés; libre como app, pero muy cerca de Brainly, gigante de educación) y **Kinemind**
  (solo dominios estacionados; en Bélgica y Francia "kiné" es el kinesiólogo: puede sonar a fisioterapia).

**Lección de la ronda**: en 2026 casi toda palabra corta y bonita ya la usa alguien. La prueba real no es "cero
resultados en Google" sino **que nadie tenga un nombre parecido en apps, juegos, educación o salud** (clases de Niza 9,
28, 41, 44). Una salida que usan muchas marcas globales: nombrar la app por su **personaje** (la mascota de la app),
que es más fácil de hacer única y de registrar.

### Séptima ronda (29-sep): nombre = personaje (Pulsi, Nubi, Kibo)

A Ricardo le gustan **Pulsi, Nubi y Kibo** y propone construir el personaje y ajustarlo a la app. Bocetos:
`python3 tools/previews/personajes.py` → `docs/previews/personajes.png` (y `personaje-<nombre>.png`).

| Nombre | Qué encontré | A favor | En contra |
|---|---|---|---|
| **Nubi** | Varias apps "Nubi": tarjeta y beneficios (Nubi S.A., Argentina), nutrición infantil (Universidad de Parma), registro de comidas, eventos. Ninguna de juegos mentales. | "Nube" en español, "nubi" = nubes en italiano: se entiende en varios idiomas. Personaje suave (una nebulosa donde nacen estrellas), calza con "Tu planeta". | En inglés puede leerse cerca de "noob/newbie" (novato, burla en los videojuegos). Nombre bastante usado. |
| **Pulsi** | Una app de radio "Pulsi" y una app para escuchar el latido del bebé ("Hear My Baby Heartbeat - Pulsi"); muchas "Puls". | Enérgico; púlsar = estrella que late con luz: calza con destellos, radar y rachas. | "Pulso" suena a corazón y salud (ya hay una app de latidos con ese nombre): puede leerse como app médica. |
| **Kibo** | KIBO Commerce (software empresarial grande), KIBO (robot educativo para niños de 4-7 años, en 70 países), "Kibo: Cozy Self-Care" (app de autocuidado) y "Kibo" (lectura accesible). | El mejor significado: "esperanza" en japonés y el módulo japonés de la Estación Espacial Internacional. | El MÁS tomado, y justo en software, educación y bienestar: el más difícil de registrar como marca. |

**Regla para el personaje** (patentes y tono): nunca se pone triste ni cambia de cara según cómo le va a la persona en un
juego (US 11,507,178 de Akili exige caras que reaccionan al desempeño; además la app no usa culpa). Celebra y acompaña.

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

### Radar — riesgo ALTO → rediseño "Rescate relámpago" (aprobado por Ricardo e implementado el 29-sep) (maqueta: `docs/previews/radar-rescate.png`)

US 8,348,671 (Posit Science, "entrenamiento de atención visual dividida", vence ~nov. 2031). Su reclamo 1 exige,
todo junto: (a) una imagen en el centro y a la vez (b) una ubicación marcada en la periferia, por un tiempo, y se
apagan; (c) mostrar dos o más imágenes candidatas y (d) exigir que se elija la del centro entre ellas; (e) SOLO si
acertó, pedir la ubicación de la periferia; (f) ajustar el tiempo de exposición según las respuestas; (g) repetir.
Radar hoy hace casi todo eso. El primer retoque (sí/no y "primero dónde") **no le convenció a Ricardo** (28-sep):
pidió reestructurar el juego con evidencia, no parchearlo.

**Rediseño propuesto: "Rescate relámpago"** (informe total, o *whole report*: Sperling, 1960; teoría de la atención
visual, TVA: Bundesen, 1990; revisión clínica de Habekost, 2015):

1. **Atento**: el haz del radar gira; el destello llega en un momento imprevisible (1,5-4 s). Esa espera sin aviso
   entrena la alerta propia, que en mayores aumentó la velocidad de procesamiento visual medida con TVA
   (Penning et al., Psychological Science, 2021).
2. **Destello**: VARIOS astronautas a la vez, repartidos por el radar (8 direcciones × 2 anillos = 16 lugares,
   cerca y lejos del centro). No hay nave central que identificar.
3. **Interferencia**, como hoy.
4. **¿Dónde estaban?**: se tocan TODOS los lugares donde se vio un astronauta (baliza celeste; tocar de nuevo la
   saca) y "¡Rescatar!". Nunca se elige entre imágenes: se reporta todo lo que se captó.
5. **Revelación**: rescatados en lima con ✓ y vuelan a la fila; el que se escapó brilla con aro sol; una baliza de
   más, con cruz. Nunca solo por color. "¡Rescate triple!" y rachas.

**Dificultad**: el destello se acorta con la escalera (500 → 40 ms, como hoy: es lo central del entrenamiento de
velocidad); cuántos astronautas sube por etapas (2 → 5); desde el nivel 5 aparecen **robots** parecidos que NO se
rescatan (informe parcial: seleccionar lo importante e ignorar lo demás); los asteroides de hoy siguen como
desorden visual. Una ronda cuenta como acierto si se rescatan todos (hasta 3) o todos menos uno (4 o más) con
máximo una baliza de más. Cada 5 rondas, **"¡Lluvia de astronautas!"**: destello largo con 6, para medir cuántos se
captan cuando el tiempo no es el límite.

**El final** (lo que más le gustó a Ricardo de Radar, se mantiene y crece): **tu vistazo** (cuánto destello
necesitas para rescatar casi todos, como hoy), **tu captura** ("3,4 de un vistazo": promedio de rescatados en las
lluvias de astronautas; en TVA es la capacidad de la memoria visual de corto plazo, que en adultos ronda 3-4),
**tu filtro** (robots tocados de los mostrados) y **tu radar** (por dirección y cerca / lejos del centro).

**Por qué queda fuera de la patente**: no hay imagen central ni candidatas (faltan (a) como la define el reclamo,
(c) y (d)), y la ubicación no depende de acertar nada (falta (e)); además se reportan varias ubicaciones a la vez.
El informe total es un método de laboratorio de 1960, público. En la búsqueda rápida no encontré patentes sobre
entrenar con informe total; igual va a la lista del abogado (Posit tiene más de 90 patentes).

**Diseño** (skill ui-ux-pro-max): casillas de toque de al menos 48 dp y, en mayores, toda la celda del sector
cuenta como toque; 8 dp entre casillas; una sola animación importante por momento (el destello), respeta "quitar
animaciones" (el haz queda quieto) y el sonido apagado; texto de estado corto y fijo arriba.

**Otras dos formas que evalué** (por si Ricardo prefiere otra):
- **Alerta de rescate** (continua): el radar gira sin parar y aparecen SOS muy breves en lugares y momentos
  imprevisibles; se toca su dirección antes de que se apaguen. Evidencia: Penning et al., 2021 (entrenar la alerta
  propia aumentó la velocidad de procesamiento visual en mayores). A favor: movimiento continuo, se entiende al
  instante. En contra: se parece a las señales de Piloto y a los lanzamientos de Freno.
- **Cambio en el cielo** (detección de cambios): dos destellos de una constelación; una estrella cambió; tocarla.
  Evidencia: Truong et al., Scientific Reports 2022 (entrenar detección de cambios mejoró la búsqueda visual). En
  contra: es más memoria que velocidad y se acerca a Parejas.

### Licencias de terceros (1-oct)

- **SPALEX** (palabras de Lluvia de meteoros): CC BY 4.0, uso comercial permitido con atribución (confirmado por el autor,
  Prof. Duñabeitia, 1-oct; datos oficiales en FigShare). Atribución en Ajustes → "Licencias y créditos".
- **Fredoka y Nunito**: SIL Open Font License 1.1 (aviso en la misma pantalla). **Unity**: aviso "Hecho con Unity".

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
| **Tráfico Estelar** (RETIRADO el 4-oct-2026: era prácticamente «Train of Thought», ver `docs/juegos/descartados.md`) | La patente de Lumos Labs encontrada (US 8,821,242) es de un juego tipo pinball; no encontré patente del juego de trenes. | Bajo | Nombre, arte, sonidos y medidas propios (ya los tiene); nada de trenes ni estaciones de tren. |
| Demás juegos | Stroop (1935), señal de alto (Logan, 1984), línea numérica (Siegler, 2003), rotación mental (Shepard, 1971), integración de trayecto, memoria episódica y prospectiva: tareas clásicas y públicas. | Bajo (no revisado a fondo) | Nada por ahora. |

**Nombres que nunca se usan** (ni en la app, ni en la tienda, ni en publicidad): UFOV, Double Decision, Target
Tracker, BrainHQ, NeuroRacer, EndeavorRx, NeuroTracker, Train of Thought, Lumosity, Peak, Elevate. En los documentos
internos, como referencia científica, sí.

**Lista para el abogado** (cuando llegue el momento de publicar): el nombre elegido; Radar rediseñado; el costo
de multitarea de Piloto; el sistema de avance (Desafío → Experto) frente a US 10,559,221; y (3-oct) **el inicio completo («Primer vuelo con Nubi»)
frente al de Lumosity**: orden de pasos, preguntas, tarjetas y barra de avance, antes de publicar (la pregunta del ánimo y la tarjeta «X puso a prueba tu Y» ya se cambiaron
por otras propias: «¿qué te sirve más ver primero?» y «Acabas de usar tu…»).

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
- Penning et al. (2021), entrenar la alerta aumenta la velocidad de procesamiento visual en mayores: https://pubmed.ncbi.nlm.nih.gov/33529541
- Revisión de estudios clínicos con TVA (Habekost, 2015): https://www.ncbi.nlm.nih.gov/pmc/articles/PMC4364300/
- Truong et al. (2022), entrenamiento de detección de cambios: https://www.nature.com/articles/s41598-022-15649-x
- Patentes de NeuroTracker (Faubert): https://image-ppubs.uspto.gov/dirsearch-public/print/downloadPdf/9566029 · https://patents.google.com/patent/US10706730B2/en
- Tetris contra Xio: https://en.wikipedia.org/wiki/Tetris_Holding,_LLC_v._Xio_Interactive,_Inc.
- FTC y Lumosity (2016): https://www.ftc.gov/news-events/news/press-releases/2016/01/lumosity-pay-2-million-settle-ftc-deceptive-advertising-charges-its-brain-training-program
- Synapp: https://apps.apple.com/us/app/synapp-messaging/id1591750660 · Neuronia: https://play.google.com/store/apps/details?id=com.virandigitallabs.neuronia · Engrama: https://apps.apple.com/pk/app/engrama/id6763983777
- Mielina (estimulación cognitiva 50+): https://www.instagram.com/minhamielina/ · MyelinZ: https://myelinz.com/ · Neuri: https://neuri.app/
- BRAIN STRIDE (marca registrada): https://trademarks.justia.com/886/09/brain-88609908.html · NeuroDash: https://play.google.com/store/apps/details?id=com.enpe5v3.neurodash · NeuroIGNITE: https://neuroignite.com/ · Synapta: https://www.synapta.co.uk/ · MindOrbit: https://play.google.com/store/apps/details?id=com.rimors.mindorbit · MindVolt: https://play.google.com/store/apps/details?id=com.nirala.mindvolt
- Nubi: https://play.google.com/store/apps/details?id=com.tunubi.b2bwallet · https://play.google.com/store/apps/details?id=it.unipr.ailab.nubi · Pulsi: https://apps.apple.com/us/app/hear-my-baby-heartbeat-pulsi/id6763934623 · Kibo: https://kibocommerce.com/ · https://kinderlabrobotics.com/kibo/ · https://apps.apple.com/us/app/kibo-cozy-self-care/id6761014109 · JAXA Kibō: https://iss.jaxa.jp/en/kibo/about/
- Podcast Mentenautas: https://podcasters.spotify.com/pod/show/mentenautas-podcast

## Nombre nuevo de Anagramas: «En la punta de la lengua» (3-oct)

Anagramas se reemplazó por **En la punta de la lengua** (id interno `anagramas`, que no cambia). Hay que sumarlo a la búsqueda oficial de marca (clases 9 y 41): es una expresión
común del español («tener algo en la punta de la lengua»), así que conviene mirar si alguna app o juego de palabras ya la usa como nombre. «Anagramas» deja de aparecer en la app.

## Juego nuevo: «Engranajes» (5-oct; rehecho como «Taller de reparación» el mismo día)

Juego nuevo de Razonamiento (id `engranajes`; ocupa el lugar de Tráfico Estelar, con otro id: `trafico` queda reservado). Un motor, una cadena de engranajes y correas, y un cohete con cuatro piezas (antena, compuerta, carga
y turbina), cada una con un cartel de lo que debe hacer: la máquina viene mal armada y se arregla con uno o dos cambios (tocar el motor o una correa). **Nombre NUEVO a revisar:** «Engranajes» es una palabra común (clases 9 y 41 de
la búsqueda oficial de marca: hay que mirar si alguna app o juego de lógica ya lo usa como nombre; por eso la app muestra «Engranajes» solo como título del juego, no como marca). Los acertijos de giro de engranajes son clásicos y
de uso libre (rompecabezas de poleas y engranajes de los libros de física y de los concursos de lógica). Los puzles de engranajes existen como género en las tiendas de apps, pero no encontramos uno en Lumosity, Peak ni
Elevate (búsqueda del 4-oct). Lo propio es arreglar con un número fijo de cambios y la idea de «aguas abajo» de una bifurcación, junto con el cohete, los carteles, el arte y los sonidos. Nombre, arte, escena del cohete y
sonidos propios. Va a la lista del abogado como juego nuevo (comparar el aspecto y la jugada con los juegos de lógica mecánica de otras apps antes de publicar).

## Nombre nuevo de Cálculo Sereno: «Carga exacta» (4-oct)

Cálculo Sereno se reemplazó por **Carga exacta** (id interno `calculo`, que no cambia): el reactor de la nave pide una carga y se juntan celdas de energía de a dos con + − × ÷ hasta llegar. Se retiró
el parecido que tenía con «Raindrops» de Lumosity: el Reto de Cálculo Sereno eran cuentas dentro de gotas que caen a un estanque (mecánica, nombre, burbuja y agua muy cercanos); ahora nada cae ni apura
dentro de una carga y no hay agua. Hay que sumar «Carga exacta» a la búsqueda oficial de marca (clases 9 y 41): es una expresión común. La mecánica de «llegar a un número juntando otros con operaciones» es un
juego clásico y público (la tradición de los acertijos de números); por eso NO se usan los nombres «24 Game», «Countdown» ni «Cifras y letras» (programa de televisión) en ninguna parte. Va a la lista del abogado
junto con lo demás (comparar el aspecto y la jugada con los juegos de números de Lumosity y de Peak antes de publicar).

