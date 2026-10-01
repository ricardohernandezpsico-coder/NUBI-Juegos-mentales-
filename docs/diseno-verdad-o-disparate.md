# ¿Verdad o disparate? — diseño (1-oct, para aprobar con maqueta)

Juego estrella de **Lenguaje** (id `disparate`), el segundo después de Lluvia de meteoros (orden acordado con Ricardo el
30-sep). Este documento es la especificación: la maqueta, el contenido y después el programa salen de acá.

## 1. La idea en una línea

Llegan transmisiones con frases cortas: **¿es verdad o es un disparate?** ("Los peces nadan" / "Las sillas ríen"). Se
responde con un toque, rápido. Al final te dice a qué velocidad lees entendiendo y qué tipo de frase te frena.

## 2. Ciencia (PubMed, 1-oct)

- **Verificación de frases**: decidir si una frase es verdadera mide la velocidad del procesamiento semántico; es la
  tarea de Collins y Quillian (1969) que se usa en neuropsicología para evaluar la memoria semántica (p. ej. Wilson y
  Baddeley, 1988, *Brain and Cognition* 8:31-46, [doi:10.1016/0278-2626(88)90037-1](https://doi.org/10.1016/0278-2626(88)90037-1);
  Clare et al., 1993, *Neuropsychologia* 31:1225-41, [doi:10.1016/0028-3932(93)90070-g](https://doi.org/10.1016/0028-3932(93)90070-g)).
- **Frases absurdas para medir velocidad de lenguaje**: el principio del test de "frases tontas" de Baddeley (SCOLP,
  1992; usado p. ej. en Gardner, Grantham-McGregor y Baddeley, 1996, [doi:10.1080/00034983.1996.11813026](https://doi.org/10.1080/00034983.1996.11813026)).
  No se copian sus ítems (el test tiene derechos).
- **Generador automático de frases verdadero/falso** para medir lectura con comprensión garantizada: Crossland, Legge y
  Dakin, 2008, *Behavioral and Brain Functions* 4:14, [doi:10.1186/1744-9081-4-14](https://doi.org/10.1186/1744-9081-4-14).
  Es exactamente nuestro enfoque: frases generadas por programa, sin un banco fijo que se memorice.
- **Las negaciones cuestan más**: verificar una frase con "no" toma más tiempo (Clark y Chase, 1972; Carpenter y
  Just, 1975; clásicos de psicolingüística, no indexados en PubMed). Da la medida "qué te frena" y un consejo.

Patentes (revisión del 1-oct): la verificación de frases es pública (1969, Collins y Quillian; Royer, 1979, técnica
de verificación de frases). Lo más cercano encontrado, US 6,409,513 (lectura veloz), está vencido. Riesgo **bajo**.
**Regla que se aplica**: NO calcular un perfil de sesgo "impulsivo / conservador" (tiende a decir verdad o disparate)
— US 11,839,472 (Akili), ver `nombre-marca-y-riesgos.md`. Solo aciertos por tipo de frase. Nombre "¿Verdad o
disparate?": frase común, sin registro conocido; va a la lista del abogado como el resto.

## 3. Cómo se juega

- **Escena**: una sala de radio en el espacio (`GameWorld` nuevo "Radio"): antena arriba que emite ondas, cielo fondo C.
- **La frase** llega como transmisión: una cinta de luz entra desde la derecha y se despliega en una placa crema con la
  frase completa (Atkinson Hyperlegible Bold, la misma letra de los meteoros; 24 sp, 28 sp mayores; hasta 2 líneas).
  Debajo, una barra fina de **señal** que se va apagando = el tiempo para responder.
- **Dos botones grandes** abajo, de lado a lado: **VERDAD** (lima, ícono ✓) y **DISPARATE** (coral, ícono de onda
  rota/zigzag), con texto; mínimo 64 dp de alto; cuentan al PRESIONAR. También se puede deslizar la placa a izquierda
  (disparate) o derecha (verdad), opcional.
- **Acierto**: la placa se ilumina, ✓, y la antena suma una barra de señal (racha). Nota de la pentatónica.
  **Error**: ✗ y la frase muestra brevemente por qué ("Las sillas no ríen"). **Se agotó la señal**: "Se perdió la
  señal" (sin castigo extra).
- Sin trivia ni conocimiento escolar: la verdad tiene que ser de sentido común para cualquier adulto de cualquier país
  ("Los peces nadan", "Una hormiga es más chica que un elefante"). Nada regional, nada de opiniones, nada metafórico
  ("el tiempo vuela" está prohibido: es ambiguo).

## 4. Dificultad (DDA común, 12 niveles; la dificultad viene de la FORMA de la frase, no del conocimiento)

| Nivel | Tipo de frase | Ejemplo verdad / disparate | Señal |
|---|---|---|---|
| 1-2 | sujeto + verbo (3 palabras) | Los peces nadan / Las piedras cantan | 9 s |
| 3-4 | + complemento o adjetivo | El sol calienta la tierra / La nieve quema las manos | 8 s |
| 5-6 | negación | Los gatos no vuelan / La sopa no se puede tomar | 7 s |
| 7-8 | frase con pausa («, que …,») | El pan, que sale del horno, está caliente / El pan, que sale del horno, ladra | 6,5 s |
| 9-10 | cuantificadores (todos/algunos/ningún) | Algunas frutas son rojas / Todos los peces tienen plumas | 6 s |
| 11-12 | comparaciones y orden | Un minuto es más corto que una hora / Una semana es más larga que un año | 5 s |

- 50% verdad / 50% disparate, nunca más de 3 iguales seguidas. Disparates de dos clases: **evidentes** (categoría
  imposible: "las mesas lloran") y **sutiles** (casi verdad: "los pingüinos vuelan", "el hielo es caliente").
- **Precisión**: sin señal que se apaga, 30 frases. **Reto**: 120 s. Mayores: señal × 1,3 y letra mayor.
- **Ráfaga** (enganche): cada 12 frases, 5 frases de 3 palabras muy rápidas, fuera de la escalera y de las medidas.
- **Transmisión perfecta**: 10 seguidas sin error = antena encendida (puntos × 1,5).

## 5. Medidas al final (juego estrella)

1. **Tu lectura con comprensión**: palabras por minuto en las frases bien respondidas (mediana de palabras ÷ tiempo de
   respuesta). Se rotula "leer y decidir" (incluye la decisión). Solo con ≥ 10 aciertos; si no, "juega más para medir".
2. **Qué te frena**: tiempo medio por tipo de frase (cortas, complemento, negaciones, con pausa, todos/algunos,
   comparaciones), mostrando solo las que tuvieron ≥ 4 aciertos. Frase: "Las negaciones te toman 0,9 s más que las
   frases simples. Es normal. Truco: lee la frase sin el 'no' y después dala vuelta". Un consejo por tipo.
3. **Tu precisión**: aciertos sobre el total y por clase de disparate (evidentes / sutiles) — SIN perfil de sesgo.
4. **Tu mejor racha** (transmisión más larga sin error).
Nota común al pie. Agregar a `docs/medidas-juegos-estrella.md`.

## 6. Contenido: el generador de frases (español primero)

- `tools/frases/` (Python, determinista): una **base de conocimiento propia** (no de un test ni de otra app) con
  ~150 entidades comunes y universales (animales, objetos de la casa, comida, naturaleza, cuerpo, tiempo) y sus
  propiedades CATEGÓRICAS: qué es (categoría), qué hace (acciones que siempre/nunca hace), cómo es (atributos sin
  excepciones), partes, tamaño y duración en escala ordinal. Solo propiedades sin excepciones de sentido común: nada de
  "a veces" (ej. "los perros nadan" queda fuera: es dudoso).
- **Plantillas** por tipo de la sección 4 que arman frases correctas en español (género, número, artículo, concordancia
  del verbo, tildes) y su respuesta. Disparates evidentes = cruzar categorías imposibles; sutiles = propiedad de una
  categoría cercana.
- Salida: `unity/NeuroVidaCore/Assets/Resources/Frases/disparate_es.json` con todas las frases ya generadas (tipo,
  respuesta, clase de disparate, palabras, la corrección corta para el error) — miles, para que no se repitan seguido.
- Filtro: nada de muerte, violencia, enfermedad, religión, política, cuerpo íntimo, comida que en algún país sea tabú;
  sin regionalismos. **Ricardo revisa una muestra de 100 frases** (la ambigüedad es el mayor riesgo de este juego:
  una frase dudosa frustra) y en el juego habrá "Esta frase no está clara" (mantener presionada la placa) que se anota
  en el registro local para corregir el banco.

## 7. Arte y sonido

- Antena de arcilla que emite ondas (anillos que se expanden) con luces que se encienden con la racha; cinta de luz de
  la transmisión; placa crema; botones de arcilla grandes. Sonido de radio suave (soplo filtrado al llegar la frase),
  campana al acertar, onda que se quiebra al errar. Pentatónica de `GameFeel`. Nada arcade.
- Respeta "quitar animaciones" (ondas quietas), sonido y vibración apagados.

## 8. Efectos y fluidez (1-oct, libertad de Ricardo: "que se vea muy fluido")

Criterio: que todo se sienta continuo y vivo sin estorbar la lectura; 60 cuadros por segundo; la frase siguiente se prepara
mientras se muestra la actual y la transición entre frases dura menos de 300 ms. Programado en
`Games/Disparate/DisparateGameController.cs`; sonidos en `DisparateSounds.cs` (síntesis propia, nada arcade).

- **Llegada**: una cinta de luz sale de la antena y recorre la pantalla hasta la placa; la frase "sintoniza": las letras pasan
  de símbolos de estática a letras nítidas en ~200 ms (cada letra a su turno; las letras no se mueven). El reloj de respuesta
  arranca recién cuando la frase está nítida. Sonido: barrido de ruido filtrado que sube.
- **Señal**: la barra se vacía; en el último 25% la placa tiembla levemente y aparece estática en los BORDES de la placa
  (cuatro tiras fuera de ella; nunca sobre las letras).
- **Acierto**: la placa late, una onda sale del plato de la antena, chispas viajan hasta la antena y se enciende una barra del
  medidor de señal (5 barras = racha, hasta 5); campana de la pentatónica que sube con la racha. Con racha ≥ 5 las ondas son
  de color y con 10 seguidas ("Transmisión perfecta", × 1,5) la luz de la punta se enciende y una aurora suave cruza el cielo.
- **Error**: chasquido de estática suave, la placa se sacude poco, ✗ y la corrección ("Las sillas no ríen") aparece en su lugar
  con un fundido; se queda ~1,4 s (1,9 s en mayores). Nada agresivo.
- **Señal perdida**: la frase se disuelve en estática y aparece "Se perdió la señal" (cuenta como no acertada, sin castigo extra).
- **Deslizar la placa** (además de los botones): sigue al dedo con una leve inclinación; a la derecha aparece VERDAD (lima, ✓) y
  a la izquierda DISPARATE (coral). Al soltar pasado ~22% del ancho responde; si no, vuelve con resorte.
- **Esta frase no está clara**: mantener presionada la placa ~0,8 s → "Gracias, la revisaremos". No cuenta para nada; Unity manda
  los ids (`sv_unclear`), la app los guarda en `unclear_sentences` (con respaldo) y los suma al "Enviar informe de errores" de
  Ajustes; `python tools/frases/buscar.py <id>` encuentra la frase.
- **Ráfaga**: estrellas del fondo en movimiento rápido (`StarfieldFx.Warp`), llegada más corta (140 ms) y 4 s de señal.
- **Vibración corta** al responder (si está activada); la racha múltiplo de 5 sigue con la de `GameFeel`.
- **Quitar animaciones**: sin estática animada, sin temblor, sin aurora, sin cinta ni inclinación y con fundidos simples.
- **Arte**: antena de arcilla con volumen (plataforma y base lila, mástil crema con remaches y patas, plato con borde celeste, brazo
  y luz sol en la punta; `ClayRaster`, sombra dura abajo) y `GameWorld.Radio` en `WorldBackdrop`. Íconos de los botones dibujados
  (✓ y onda rota). Vista previa del sprite: `tools/art-preview` (`disparate_antenna`).
- **Reglas de tamaño**: frase en Atkinson Hyperlegible Bold, 24 sp (28 en mayores), hasta 2 renglones (3 en mayores);
  el generador no deja pasar frases de más de 50 caracteres. Botones de ~93 dp de alto (mínimo 64 dp), cuentan al PRESIONAR.

## 9. Estado

Programado y verificado el 1-oct (Unity: `DisparateContract`, `DisparateDirector`/`DisparateTally`, `SentenceBank`, controlador,
sprites y sonidos; app: `Reading.kt`, final de partida, marca "tu lectura" en la evolución, ícono de la antena). Medidas y
referencias en `docs/medidas-juegos-estrella.md`. Pendiente: que Ricardo lo pruebe en el teléfono (fluidez, deslizar, mantener).

