# Auditoría de Nubi (30-sep)

Pedido de Ricardo: revisar la app completa y la competencia (capturas de Lumosity, NeuroNation y Peak) para pulirla,
decidir si se simplifican áreas, mejorar la estética (fondo más oscuro), fundamentar mejor los juegos con estudios,
cuidar patentes, marcas y derechos de autor, y preparar el camino a Google Play. Complementa (no repite)
[`analisis-competencia.md`](analisis-competencia.md), [`nombre-marca-y-riesgos.md`](nombre-marca-y-riesgos.md) y
[`hoja-de-ruta.md`](hoja-de-ruta.md).

## En pocas líneas

- **Lo fuerte**: 19 juegos abiertos, 10 "estrella" con una medida propia y su respaldo científico, sin cuenta ni
  servidores, respaldo del progreso, 125 + 182 pruebas automáticas y verificación en GitHub. En ciencia por juego y en
  honestidad de las medidas, Nubi ya está por encima de los tres competidores.
- **Lo débil**: áreas desparejas (Lenguaje con 1 juego, Cálculo y Velocidad con 2), 6 juegos estrella y las pantallas
  nuevas de Hoy y Juegos **sin probar en el teléfono**, no hay tutorial de primera vez (el estándar de la competencia), y
  la lista de requisitos de Google Play todavía no se empezó.
- **Recomendación central**: pasar de 6 a **4 áreas** (sección 2). Baja de 12 a 2-3 los juegos que faltan y deja la app
  lista para publicar mucho antes.

## 1. Qué hay hoy

| Área | Juegos | Estrella (medida propia) |
|---|---|---|
| Memoria | Parejas Ocultas, Secuencia Lumínica, Ruta del Tesoro, Bitácora de Misión, Rumbo a Casa, Correo Estelar | Bitácora, Rumbo, Correo |
| Atención | Tinta o Palabra, Cambio de Chip, Piloto Estelar, Freno de Emergencia, Satélites | Piloto, Freno, Satélites |
| Razonamiento | Detective de Series, Acoplamiento, Tráfico Estelar | Acoplamiento, Tráfico |
| Cálculo | Cálculo Sereno, Aterrizaje Lunar | Aterrizaje |
| Velocidad | Radar (Rescate relámpago), Comparación Instantánea | Rescate relámpago |
| Lenguaje | Anagramas | – |

## 2. Áreas: ¿simplificar?

**Opción A — Mantener 6 y llegar a 5 por área** (plan actual de la hoja de ruta): faltan 12 juegos. Con el ritmo de
calidad que se viene exigiendo (maqueta, programar, probar, ajustar), son meses antes de poder publicar.

**Opción B — 4 áreas (recomendada)**:

| Área nueva | Juegos | Cuántos |
|---|---|---|
| Memoria | igual que hoy | 6 |
| Atención y velocidad | Tinta o Palabra, Cambio de Chip, Piloto, Freno, Satélites, Rescate relámpago, Comparación | 7 |
| Razonamiento y números | Detective de Series, Acoplamiento, Tráfico Estelar, Cálculo Sereno, Aterrizaje Lunar | 5 |
| Lenguaje | Anagramas + 2 nuevos (Lluvia de meteoros: decisión léxica; La palabra intrusa: categorías) | 3 → 5 después |

- Solo falta Lenguaje, y con 3 juegos ya se puede publicar (subir a 5 después, con la app en la tienda).
- Pantallas más limpias: en Hoy quedan 2 áreas por lado (más aire, letra más grande para mayores) y Juegos pasa a una
  rejilla de 2 × 2 que llena la pantalla (hoy queda un cuarto vacío abajo, ver `previews/juegos-areas-real.png`).
- **Honestidad científica**: los modelos de la inteligencia (por ejemplo CHC) separan velocidad de procesamiento y
  conocimiento numérico como capacidades propias. Juntarlas es una decisión de producto para simplificar, no una
  afirmación científica: por eso la app no debe decir "velocidad y atención son lo mismo", sino agrupar los juegos. Lo
  hacen también los competidores (Peak junta "Agilidad mental"; NeuroNation reparte distinto que Lumosity).
- **Costo**: el área está guardada por nombre en la base de datos (`domain_mastery`), en `AreaProgress`, la evaluación
  inicial, colores, íconos, textos para compartir y logros. Es una migración de Room (v11 → v12) que junta el avance de
  las áreas unidas + pantallas de Hoy y Juegos. Estimado: 1-2 sesiones, con pruebas.

**Opción C — 3 áreas, sin Lenguaje**: lo más rápido y lo más barato para un mercado global (Lenguaje depende del idioma:
cada idioma necesita sus listas de palabras). Pero deja la app menos completa que la competencia (las tres tienen
Lenguaje) y a Anagramas sin lugar. No la recomiendo, salvo que se decida publicar primero en varios idiomas a la vez.

**Idioma de lanzamiento**: recomiendo **español primero** (Latinoamérica y España). Hoy casi todos los textos están
escritos directo en español; traducir todo (i18n) es un proyecto aparte, y en Lenguaje significa listas de palabras por
idioma. Inglés, en una segunda etapa.

## 3. Estética

### Fondo más oscuro (lámina: [`previews/fondo-oscuro.png`](previews/fondo-oscuro.png), `tools/previews/fondo_oscuro.py`)

| | Fondo | Barra de pestañas | Contraste del texto secundario |
|---|---|---|---|
| A · Actual | #04061C → #101A58, nebulosas como hoy | blanca | 12,5 : 1 |
| B · Noche profunda | #02030F → #0A0F33, nebulosas −40% | blanca | 13,3 : 1 |
| **C · B + barra oscura** (recomendada) | como B | tinta #14112E, pestaña activa uva | 13,3 : 1 |
| D · Casi negro | #030308 → #0B0C22, nebulosas −65% | tinta | 13,5 : 1 |

Por qué C: al oscurecer el fondo, la barra blanca pasa a ser lo más brillante de la pantalla y le quita protagonismo a
Nubi (en A ya compite). D se ve elegante, pero pierde el color "noche" que une la app con los juegos de Unity y queda
cerca de un tema oscuro genérico. El cambio es chico: `CosmosBackground.kt` (3 colores + transparencia de las 3
nebulosas), `NeuroNavBar` y, para que el paso app → juego no se note, el cielo de `WorldBackdrop` en Unity (y cambiar la
marca `CountdownScreen.StyleStamp`).

### Otras mejoras visuales (para trabajar con la skill `ui-ux-pro-max` y maquetas primero)

1. **Juegos**: la rejilla deja un cuarto de pantalla vacío; con 4 áreas se resuelve sola (tarjetas más grandes).
2. **Identidad por área dentro de los juegos**: los 19 juegos son espaciales; Lumosity le da a cada juego su textura
   (papel, madera, fieltro). Propuesta: el cielo de cada juego toma un tinte del color de su área, así se distinguen sin
   romper el hilo del espacio.
3. **Tutorial de primera vez** con Nubi maestra (ya dibujada en `nubi-guia.png`) y "Saltar": además de estética, es lo
   que más retiene (sección 7).
4. **Ficha de la tienda**: ícono 512 px, imagen destacada 1024 × 500 y 4-8 capturas con una frase cada una. Es lo
   primero que ve la gente: vale la pena una maqueta propia.

## 4. Ciencia: fundamentar más

**Lo que está bien**: los 10 juegos estrella tienen su tarea clásica, referencias y reglas para no sobreinterpretar
(`medidas-juegos-estrella.md`).

**Lo que falta**:

1. **Ficha científica de los 9 juegos base** (Parejas, Secuencia, Ruta del Tesoro, Tinta o Palabra, Cambio de Chip,
   Detective de Series, Cálculo Sereno, Comparación, Anagramas): tarea de origen, qué pone a prueba, referencias
   verificadas (con DOI) y qué NO decir. Propuesta: `docs/ciencia-juegos.md` con los 19 en el mismo formato.
2. **Decir con honestidad qué dice la evidencia del campo**: los metaanálisis muestran que se mejora en lo que se
   entrena y en tareas parecidas, pero que el paso a la vida diaria es limitado o discutido (Simons et al., 2016,
   *Psychological Science in the Public Interest* 17(3):103-186, [doi:10.1177/1529100616661983](https://doi.org/10.1177/1529100616661983);
   Melby-Lervåg, Redick y Hulme, 2016, *Perspectives on Psychological Science* 11(4):512-534,
   [doi:10.1177/1745691616635612](https://doi.org/10.1177/1745691616635612); Sala y Gobet, 2019, *Trends in Cognitive
   Sciences* 23(1):9-20, [doi:10.1016/j.tics.2018.10.004](https://doi.org/10.1016/j.tics.2018.10.004)). La excepción con
   datos a largo plazo es el entrenamiento de velocidad del estudio ACTIVE (Ball et al., 2002, *JAMA* 288(18):2271-2281,
   [doi:10.1001/jama.288.18.2271](https://doi.org/10.1001/jama.288.18.2271); a 2 años mejoró lo entrenado, pero sin
   efecto todavía en la vida diaria), que es de otra empresa y no podemos usar como propio. Referencias verificadas en
   PubMed el 30-sep. Esto no es malo para Nubi: es exactamente la línea que ya sigue la app ("mide cómo te fue,
   no es un diagnóstico").
3. **Página "La ciencia de Nubi"** dentro de la app (Peak tiene "La ciencia de Peak"): qué mide cada juego, de qué
   estudio viene y qué no promete. Con Ricardo como psicólogo detrás, es un diferenciador creíble.
4. **Paso más fuerte (más adelante)**: un piloto de confiabilidad (medir dos veces a 20-30 adultos con una semana de
   diferencia y ver si las medidas propias se repiten). Hecho con una universidad chilena, daría un respaldo real del
   tipo "desarrollado con…" (como NeuroNation con la Freie Universität o Peak con Cambridge). Requiere comité de ética
   y consentimiento; no usar pacientes de la consulta.

## 5. Patentes, marcas y derechos de autor

La revisión de patentes juego por juego ya está hecha (`nombre-marca-y-riesgos.md`, sección 5) y el rediseño de Radar
resolvió el riesgo alto. Lo que agrego:

- **Marca "Nubi"**: además de lo ya anotado, Nubi S.A. (Argentina) tiene una app de billetera y tarjeta en Google Play
  con más de 50 mil descargas. Es otro rubro (finanzas), pero comparte la clase 9 (aplicaciones descargables) y es
  conocida en Latinoamérica, justo el mercado de lanzamiento. No necesariamente bloquea, pero hace **urgente** la
  búsqueda oficial (INAPI, IMPI, TMview, USPTO, OMPI) y la consulta al abogado antes de invertir en la ficha de la
  tienda. "Nubi – Brain Games" como nombre en la tienda ayuda a diferenciar.
- **Arte de Nubi**: se dibujó "del tono de una lámina de referencia de Ricardo". Conviene anotar de dónde salió esa
  lámina: si es una ilustración de otra persona, el derecho de autor protege el dibujo (no la idea de una nube con
  cara), y el nuestro debe quedar claramente distinto. Hoy lo es (redibujado por programa), pero queda por escrito.
- **Imágenes hechas con IA** (si se usan para la tienda): se pueden usar, pero en EE. UU. una imagen hecha solo por IA
  no queda protegida como obra propia (informe de la Oficina de Derechos de Autor de EE. UU., 2025). Para el personaje y
  el ícono, mejor el camino actual (dibujo propio por programa).
- **Licencias de terceros**: las fuentes (Fredoka, Nunito: licencia OFL) y Unity piden incluir sus avisos. **HECHO el 1-oct**:
  pantalla Ajustes → "Licencias y créditos" (SPALEX con su cita completa y CC BY 4.0, Fredoka y Nunito con SIL OFL 1.1, y
  Unity). Si se elige una letra nueva para los meteoros, hay que sumarla ahí.
- **Tráfico Estelar**: es el juego más cercano a uno emblemático de la competencia (el de trenes de Lumosity). La
  mecánica es libre y el arte, sonidos y medidas son propios; mantener la regla "nada de trenes ni estaciones de tren"
  y no mencionarlo nunca en la tienda.
- **Lista para el abogado** (se suma a la existente): marca Nubi frente a Nubi S.A.; textos de la tienda.

## 6. Para publicar en Google Play

**Lo que no se puede deshacer (hacer bien una sola vez)**

1. **`applicationId`**: hoy es `com.aistudio.neurovida.cgnv` (heredado de AI Studio). Una vez subida la app, no se
   cambia nunca. Definirlo antes de la primera subida (por ejemplo `cl.<tu-dominio>.nubi`).
2. **Clave de firma**: usar la firma de apps de Google Play y guardar la clave de subida con respaldo fuera del PC.

**Lo que exige Google Play**

3. **Prueba cerrada de 12 personas durante 14 días seguidos** antes de poder publicar, si la cuenta de desarrollador es
   personal y se creó después de noviembre de 2023. Conviene planificarla ya (familia y amigos, no pacientes).
4. **Formulario de seguridad de datos + política de privacidad** (una página web simple). Como nada sale del teléfono,
   es fácil de declarar; el informe de errores lo comparte la persona a mano.
5. **Público objetivo**: el onboarding tiene la opción "Menos de 18". Si el público incluye menores de 13, aplica la
   política de Familias (mucho más estricta). Recomiendo declarar **13+ o 18+** y cambiar esa opción a "13 a 17" (o
   quitarla).
6. **Categoría**: "Educación" o "Juegos > Educativos / Puzle", no "Salud y bienestar" (esa exige una declaración de app
   de salud y más revisión de lo que se promete).
7. **Páginas de memoria de 16 KB**: desde noviembre de 2025, las apps con código nativo (Unity lo es) que apuntan a
   Android 15+ deben ser compatibles. Unity 6 lo soporta en sus versiones recientes, pero hay que comprobarlo en el
   APK (Analizador de APK de Android Studio).
8. **Tamaño**: `unity/AndroidExport` pesa 2 GB en disco; medir el `.aab` final. Si la descarga pasa de 200 MB, hay que
   usar Play Asset Delivery.
9. **Versión de tienda optimizada**: hoy `isMinifyEnabled = false`. Activar R8 (más liviana) con reglas para Unity y el
   puente, y probar la versión de tienda en el teléfono antes de subirla.
10. **Ficha**: descripción corta y larga sin promesas de salud (sección 6 de `nombre-marca-y-riesgos.md`), ícono,
    imagen destacada, capturas y cuestionario de clasificación por edades.

**Decisiones que conviene tomar antes (aunque se implementen al final)**

11. **Modelo de negocio** (gratis, freemium, pago único): cambia qué se muestra y cómo; los tres competidores bloquean la
    mayoría de los juegos. Nubi hoy lo abre todo, y eso puede ser su bandera.
12. **Errores de personas reales**: la consola de Play trae "Android vitals" (cierres y bloqueos) sin agregar nada a la
    app. Suficiente para empezar, sin servidores.

## 7. Riesgos del producto (lo que más me preocupa)

1. **Juegos sin probar en el teléfono**: Rescate relámpago, Satélites, Bitácora, Rumbo a Casa, Correo Estelar (versión
   con escudo) y Tráfico "lento y lleno", más las pantallas nuevas de Hoy, Juegos y el resumen de sesión. Antes de
   sumar juegos nuevos, probar estos.
2. **Sin tutorial de primera vez**: el mismo Ricardo no captó el momento "¡A LOS MANDOS!" en Piloto. Lección de
   Primer Contacto: "una persona que no lo entienda no lo vuelve a jugar". Es la mejora con más retorno.
3. **Cambios sin guardar en la carpeta del repositorio**: 5 archivos con limpieza de código sin uso (−120 líneas:
   `NunitoFamily`, `ClayLightCard`, sliders de dificultad por área, `defaultRounds`, `getAllSync`). No son de esta
   auditoría; probablemente de una sesión anterior. Hay que decidir si se comitean (compilando antes) o se descartan.
4. **Índice de `Proyectos/CLAUDE.md` desactualizado**: dice "9 juegos, Kotlin + Compose"; hoy son 19, con Unity, y la
   app se llama Nubi.

## 8. Herramientas

**Ya disponibles (sin instalar nada)**

- Skills `ui-ux-pro-max`, `design`, `design-system`, `banner-design` y `dataviz`: decisiones de diseño, ficha de la
  tienda, gráficos de Avance.
- **Stitch** (Google, conectado): genera pantallas y variantes desde una descripción. Sirve para explorar Hoy / Avance /
  Juegos con 4 áreas antes de programar.
- **Nano Banana 2** (conectado): imágenes para la tienda (ojo con lo dicho sobre IA en la sección 5).
- Plugin `android-development-assistant` (agente de producción: pruebas, rendimiento, publicación) y skill
  `android-cli`.
- Navegador integrado / Claude en Chrome: acompañar el llenado de la consola de Play (las contraseñas y pagos los
  ingresa Ricardo).

**Propongo conectar**

- **PubMed** (conector oficial): buscar y verificar referencias con DOI para la ficha científica de los 19 juegos.
- **Consensus** o **Elicit** (opcional): resumen de lo que dicen muchos estudios sobre una pregunta ("¿el entrenamiento
  de inhibición transfiere?"). Útil para la página "La ciencia de Nubi".
- Más adelante, cuando se decidan servidores: Firebase. Marcas y patentes: sin conector; se buscan en las bases
  oficiales (INAPI, OMPI, Google Patents) con el navegador.

## 9. Orden sugerido

1. Ricardo decide: áreas (A / B / C de la sección 2) y fondo (A-D de la lámina).
2. Fondo y barra (sesión corta) + qué hacer con los cambios sin guardar.
3. Probar en el teléfono los 6 juegos y las pantallas pendientes.
4. Tutorial de primera vez: plantilla común en Unity, primero en 2 juegos y luego en todos.
5. Reorganizar áreas (si se elige B) + 2 juegos de Lenguaje.
6. Ficha científica de los 19 + página "La ciencia de Nubi" + pantalla de licencias (esta última ya hecha el 1-oct).
7. Antes de publicar: marca (búsqueda + abogado), `applicationId`, público 13+/18+, política de privacidad, 16 KB,
   tamaño, versión de tienda con R8, prueba cerrada de 12 personas por 14 días.
