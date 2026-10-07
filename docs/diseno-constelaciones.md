# Constelaciones: renovación de Parejas Ocultas (propuesta A, elegida por Ricardo el 7-oct)

Renovación a fondo de **Parejas Ocultas** (id `parejas`, área Memoria). Se mantiene el id, el historial y el lugar en la
app; cambia el juego, el nombre que se muestra y la medida del final.

- **Boceto:** `docs/previews/constelaciones-boceto.html` (https://claude.ai/artifact/XkUFmW6A13TW92Y2B1b2z9). Con
  `?etapa=N` se entra directo a una etapa. Con `?debug` deja las posiciones en el DOM. `?debug&objetos` muestra los 12
  objetos y los 4 gemelos, y `?debug&fin` la pantalla final.
- **Lo que eligió Ricardo:** «la recomendación A, considerando lo que genera su desarrollo, su base científica».
- **Lo que rechazó:** el álbum «Mi cielo» para coleccionar constelaciones: «no le encuentro sentido». Se reemplazó por
  algo con función: **el color de cada línea dice cómo encontraste la pareja** (sección 1).
- **Pedido de Ricardo:** usar las skills de UI/UX a fondo. Las reglas que se aplicaron están en la sección 6.

## Por qué se renueva

- Es el clásico de dar vuelta cartas que tienen casi todas las apps; era el juego más genérico de Nubi.
- Chocaba con Bodega de carga: los dos eran «dónde quedó cada cosa». Ahora se diferencian:
  - en **Bodega** te muestran dónde va cada cosa y después preguntan;
  - en **Constelaciones** exploras tú y decides qué mirar.
- No tenía medida al final y el puntaje mezclaba memoria con suerte.
- Usaba símbolos fuera de regla: el sol y una constelación con estrellas de puntas (`SymbolBank` tier 1).

## 1. La idea

El tablero es un **cielo nocturno**. Cada luz esconde un objeto del espacio. Das vuelta dos luces:
- **si son iguales**, quedan unidas por una línea de luz;
- **si no**, se cierran solas o al tocar la siguiente.

**La línea dice cómo la encontraste** (esto reemplaza al álbum):

| Línea | Aro de las luces | Cuándo |
|---|---|---|
| **Dorada continua** | dorado continuo | **De memoria**: la pareja ya se había visto y fuiste directo a ella. |
| **Celeste punteada** | celeste punteado | **A la primera vista**: la segunda luz era nueva. Fue suerte, no memoria. |

Además de la línea, cambia la forma (continua o punteada), no solo el color. Al terminar el cielo, las líneas se
iluminan una por una y queda a la vista **qué parte del cielo armaste con tu memoria**.

## 2. Respaldo científico (PubMed, revisado el 7-oct)

- **El juego de parejas como prueba de memoria visual y de lugar.** Schumann-Hengsteler (1996) sacó de la partida
  indicadores distintos: cuánta información se recoge, cuánta se pierde y errores de lugar. Es la base de la medida
  «memoria de lugar», que separa memoria de suerte. https://doi.org/10.1080/00221325.1996.9914847
- **Con la edad cae más la memoria de las uniones que la de los objetos sueltos.** Old y Naveh-Benjamin (2008),
  metaanálisis de 90 estudios con unas 6.400 personas. El efecto incluye la unión objeto-lugar y las parejas de objetos.
  https://doi.org/10.1037/0882-7974.23.1.104
- **Distinguir lo visto de algo muy parecido (gemelos):**
  - La habilidad depende del hipocampo y también cae con la edad (Stark y Stark, 2017,
    https://doi.org/10.1016/j.bbr.2017.06.049; Stark et al., 2015, https://doi.org/10.1037/bne0000055).
  - **Advertencia de diseño:** en mayores, parte de la dificultad es de la vista (Davidson et al., 2019,
    https://doi.org/10.1093/geronb/gby130). Por eso el detalle que distingue a los gemelos es **grande y de forma**:
    anillo, llama, patas, antena. Nunca un tono parecido ni un detalle diminuto.

## 3. Reglas exactas

- **Turno:** dar vuelta `size` luces (2 en parejas, 3 en tríos). Si una no coincide con la primera, el turno termina.
- **Las abiertas se cierran solas a los 850 ms**, o **al instante** si se toca otra luz; el toque nuevo cuenta. Nada
  bloquea al jugador: cada luz se puede tocar apenas aparece (sin candado global).
- **Oportunidad de memoria:** desde la segunda luz del turno en adelante, hay oportunidad si alguna luz compañera de la
  primera **ya se vio antes** y sigue escondida.
  - **Acierto:** tocar una de esas compañeras.
  - **Se te escapó:** tocar otra.
  - Sin compañera vista no hay oportunidad. Elegir la primera luz del turno nunca se juzga.
- **Tras un «se te escapó»:**
  - a los 380 ms, la compañera ya vista **brilla 1,1 s sin darse vuelta** (aro dorado claro);
  - el aviso dice «Se te escapó: brilla dónde estaba / Ya la habías visto: la próxima vez, directo».
  - Es el mismo principio sin error de Bodega: nunca te quedas sin saber dónde estaba.
- **Racha de memoria:** sube con cada acierto y se corta solo con un «se te escapó». Explorar luces nuevas o fallar sin
  oportunidad **no la corta**.
- **Se acabaron las 3 fallas y los corazones.** El cielo siempre se completa; el nivel siguiente lo decide el motor común.
  Se acaba también la vista previa de 3 s, que hacía imposible separar memoria de suerte.
- **Gemelos:** el gemelo es otra pareja (otra clave). Solo se unen los idénticos.
- **Tríos:** se unen con dos líneas, A-B y B-C. El tipo de cada línea es el de su luz de llegada.

## 4. Etapas (12 niveles del DDA común)

| Etapa | Grupos | Tipo | Gemelos | Luces | Novedad (tarjeta NUEVO) |
|---|---|---|---|---|---|
| 1 | 3 | parejas | – | 6 | **CONSTELACIONES**: «Cada luz esconde un objeto. / Da vuelta dos: si son iguales, / quedan unidas con luz.» + «Línea dorada: la recordaste» |
| 2 | 4 | parejas | – | 8 | |
| 3 | 5 | parejas | – | 10 | |
| 4 | 6 | parejas | – | 12 | |
| 5 | 6 | parejas | 1 | 12 | **NUEVO**: «¡Llegan gemelos parecidos! / Mira bien el detalle: / solo se unen los idénticos.» (planeta con anillo ≠ sin anillo) |
| 6 | 7 | parejas | 1 | 14 | |
| 7 | 8 | parejas | 2 | 16 | |
| 8 | 4 | tríos | – | 12 | **NUEVO**: «Ahora son tríos. / Da vuelta tres iguales / para unirlos.» |
| 9 | 5 | tríos | – | 15 | |
| 10 | 9 | parejas | 2 | 18 | |
| 11 | 5 | tríos | 1 | 15 | |
| 12 | 10 | parejas | 3 | 20 | |

- **Gemelos:** cuentan dentro de los grupos. Ejemplo: 1 gemelo = planeta con anillo + planeta sin anillo, es decir, 2 de
  los grupos.
- **Grupo para la pantalla final:** etapas 1-2 → 1, 3-4 → 2, 5-7 → 3, 8-9 → 4, 10-12 → 5.
- **Partida:** 6 cielos. Un cielo con 0-1 «se te escapó» sube una etapa; con 3 o más baja una.
  - En la app esto lo hace el **motor común** (`AdaptiveDifficulty`, `MaxLevel` 12). Cada oportunidad de memoria es un
    ensayo: acierto = correcto, «se te escapó» = error.
  - Los cielos sin oportunidades no mueven el rating.
- **Simulación** (1.500 partidas con un jugador que olvida una parte de lo visto; `scratchpad/constelaciones/sim.js`):
  - Ninguna partida se trabó.
  - La memoria de lugar sigue al jugador: 100 % con memoria perfecta, 84 % si olvida el 30 % y 54 % si olvida el 80 %.
  - La etapa máxima sube con la memoria.
  - Una partida dura unos 47 turnos (unos 3 minutos).

## 5. Pantalla (360×640 dp de referencia)

- **Arriba (y 28):** «Cielo N de 6». A la derecha, la píldora dorada «Racha de memoria ×N» desde 2; si no, «Mejor racha: N».
- **Tarjeta (y 58-118):**
  - en juego: «Busca las parejas / Faltan N parejas», «Busca tres iguales» u «Ojo con los gemelos / Solo se unen los
    idénticos · faltan N»;
  - al terminar el cielo: «¡Constelación completa! / X de Y de memoria» o «Todas a la primera vista: ¡qué suerte!».
- **El cielo (x 14-346, y 130-526).** Las luces se reparten como estrellas, no en grilla:
  - **reparto:** muestreo del mejor candidato (40 intentos por luz) y luego empuje hasta respetar la distancia mínima;
  - **radio:** `R = clamp(√(área/n)·0.34, 28, 38)`; distancia mínima entre centros `2R + 12`, es decir, toque de 56 dp o
    más con 12 de aire;
  - **verificado:** 300 repartos por tamaño, de 6 a 20 luces, sin una sola violación.
  - **Toque:** la luz más cercana dentro de `R + 10`.
- **Luz dormida:** esfera de arcilla oscura con un núcleo tibio. Sin animación continua; todas se ven iguales.
- **Luz abierta:** disco claro con el objeto a `1,32·R`.
- **Líneas:**
  - se curvan un poco (desvío máximo de ±50) para esquivar otras luces y otras líneas cuando pueden;
  - las parejas cercanas (distancia < 4R) van rectas;
  - **se dibujan sobre todas las luces, con borde oscuro, y nacen en el borde de las suyas**. Así, si cruzan otra luz,
    se ve que pasan de largo y nunca parece que terminan en ella.
  - Probado: dibujadas bajo las luces, hasta el 39 % parecían unir una luz ajena.
- **Abajo (y 546-580): «De memoria — X de Y».** Cada oportunidad es un punto dorado lleno (acierto) o un aro vacío (se te
  escapó). Antes de la primera dice «aún nada que recordar».
- **Pantalla final:**
  - **«Memoria de lugar: N %»** en grande, con «X de Y veces fuiste directo / a una pareja que ya habías visto»;
  - filas: parejas de memoria, racha de memoria más larga, etapa más alta y tu mejor racha;
  - un truco;
  - «Lo encontrado por suerte no cuenta.» y «Medida de esta partida. No es un diagnóstico.»

### Objetos (siluetas propias, del espacio, fáciles de nombrar)

- **Los 12:** planeta (con anillo), cohete, cometa, ovni, cristal, casco, luna llena, satélite, telescopio, asteroide,
  antena y brújula.
- **Gemelos** (variante 1):
  - planeta **sin anillo**;
  - cohete **con llama grande**;
  - ovni **con patas**;
  - casco **con antena**.
- **Sin** sol, sin estrellas de puntas, sin media luna y sin cruces (`docs/simbolos-neutros.md`).
- Reemplazan a `SymbolBank` y sus tiers. Los dibujos de `SymbolSprite` que sirvan se pueden reusar si siguen estas
  siluetas.

## 6. Movimiento, sonido y accesibilidad (reglas UI/UX aplicadas)

- **Al tocar:** la luz se achica a 0,94 durante 140 ms (respuesta en menos de 150 ms).
- **Volteo con resorte:**
  - abrir: frecuencia 2,6 y amortiguación 0,72, con un pequeño rebote;
  - cerrar: frecuencia 3,6 y amortiguación 0,95, más rápido y sin rebote. La salida es más corta que la entrada.
- **Línea:** se traza en 380 ms desacelerando, con una chispa en la punta. En tríos, la segunda sale 200 ms después.
- **Cielo nuevo:**
  - las luces aparecen escalonadas, 45 ms entre una y otra, cada una con 380 ms de escala `easeBack`;
  - al terminar el cielo, las luces se encogen en 360 ms.
- **Al encontrar:** una etiqueta flota sobre el punto medio y sube 22 dp en 1 s: «¡De memoria!» en dorado, o
  «¡Encontrada!» / «¡Trío!» en celeste.
- **Quitar animaciones:** sin rebotes, sin partículas y sin estela; las líneas aparecen completas; el brillo de pista es
  fijo (sin latido); la etiqueta no sube.
- **Sonido propio:**
  - cada grupo tiene su nota pentatónica y la luz suena suave al abrirse (las dos de una pareja suenan igual);
  - el encuentro suena como acorde de quinta, o de tercera si fue a la primera vista;
  - un acierto de memoria suma un brillo que sube con la racha;
  - el «no son iguales» es un tono grave y corto, no un castigo;
  - al empezar el cielo suena un zumbido de fondo.
- **Nada depende solo del color:**
  - las líneas son continuas o punteadas;
  - los puntos de abajo, llenos o vacíos;
  - los gemelos se distinguen por forma.
- **Textos:** 14 dp o más. Contraste: texto claro `#EDEAFB` / `#ABA5D2` sobre `#141B3A` (más de 4,5:1).

## 7. Medidas al final (juego estrella)

- **Memoria de lugar** (principal) = aciertos de memoria / oportunidades.
  - Guardar también aciertos y oportunidades por separado.
  - Una partida sin oportunidades muestra «—» y no se guarda como medida.
- **Parejas de memoria:** grupos con al menos una línea dorada / grupos encontrados.
- **Racha de memoria más larga.** El récord es «mejor racha», en preferencias, con respaldo (`BackupRulesTest`).
- **Otros datos para la telemetría:**
  - **vueltas inútiles:** segunda luz ya vista que no era compañera, sin oportunidad;
  - **turnos**;
  - el `end_rating` del motor común.
- Va en `StarMeasures` / `ResultAdvice` como los demás juegos estrella. Truco del consejo: «da vuelta primero una luz
  nueva».

## 8. Tutorial (NubiCoach, se aprende haciendo)

1. **Touch** en una luz: «Toca una luz para ver qué esconde.»
2. **Touch** en otra (forzada nueva y distinta): «Toca otra. Si no son iguales, se cierran.»
3. **Touch** en una luz nueva cuya compañera es la del paso 1 (forzado): «Esta ya la viste. ¿Dónde estaba su pareja?»
   El hueco cubre todas las dormidas y acepta solo la compañera; la red de seguridad de la tarea 42 sigue vigente.
4. **Notice** sobre la línea dorada: «Línea dorada: la encontraste de memoria.»
5. **Notice** sobre «De memoria» abajo: «Aquí se cuenta cuántas veces fuiste directo.»

Luego, un cielo de 3 parejas que no cuenta. Las zonas protegidas (`keep:`) son la tarjeta de arriba y la fila de abajo.

## 9. Originalidad y riesgos

- **El juego de parejas es de dominio público y antiquísimo.**
- **Búsqueda del 7-oct:**
  - no hay un juego de parejas en un cielo, ni de constelaciones, en Lumosity, Peak, Elevate, CogniFit ni NeuroNation;
  - lo más parecido son los «Onet», que unen fichas iguales con un camino, pero con las fichas siempre a la vista: no hay
    memoria y no separan memoria de suerte.
- **Patente de Posit (US 7,540,615):** describe cartas mostradas una por una en secuencia. Aquí el cielo completo está
  siempre a la vista (regla vigente de `docs/nombre-marca-y-riesgos.md`).
- **Nombre:** «Constelaciones» no lo usa ninguna de esas apps. Va a la lista del abogado como nombre nuevo.

## 10. Lo que se va de la versión anterior

- La vista previa de las cartas al empezar.
- Los distractores de fondo.
- El sistema de 3 fallas y los corazones.
- La grilla.
- `SymbolBank` y sus tiers, con el sol y la constelación de puntas.
- El bono de velocidad por pareja. En Reto se mantiene el reloj de la ficha («¿Con reloj?»).
