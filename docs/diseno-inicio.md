# Primer vuelo con Nubi — el inicio nuevo (aprobado por Ricardo el 3-oct)

Reemplaza al onboarding actual (`OnboardingScreen`, 9 pantallas de preguntas) y a la evaluación aparte (`BaselineScreen`,
`data/Baseline.kt`: Secuencia, Tinta o Palabra y Comparación, que medían solo 2 de las 4 áreas). Maqueta jugable aprobada:
https://claude.ai/artifact/H2edrC6krMV9mAqLCxuiwa (copia local de la maqueta en `docs/previews/inicio-maqueta.html`).
Ricardo dio libertad para sumar ideas; las que agregué después de la maqueta están marcadas con **(nuevo)**.

## La idea

Se juega desde el primer minuto. Cada juego enseña mientras se juega (la primera ronda es guiada) y mide el punto de
partida de su área; las pocas preguntas necesarias van entre juego y juego, de un toque y con su porqué. Unos 5 minutos.
Inspirado en lo observable del inicio de Lumosity (capturas en `Proyectos/pantallazos Lomosity/`): jugar pronto, tutorial
dentro del juego con «Saltar tutorial», tarjeta «X puso a prueba tu Y», preguntas intercaladas y barra de avance. Nada
copiado: nombres, textos, arte y juegos propios.

## Recorrido

1. **Nubi se presenta**: «Vamos a encontrar tu punto de partida jugando. Son unos 5 minutos.»
2. **¿Cómo te llamo?** (opcional).
3. **Rango de edad**: ajusta ritmo y tamaño (no la dificultad).
4. **Juego 1 · Memoria**: Secuencia Lumínica renovada (ver «Juegos del inicio»). Tarjeta de entrada con Nubi maestra →
   ronda guiada → rondas que miden → tarjeta «Secuencia Lumínica puso a prueba tu memoria de trabajo» con el primer dato en
   palabras y una línea de para qué sirve esa capacidad en la vida diaria.
5. **¿Qué te gustaría entrenar?** (hasta 3 áreas).
6. **Nubi: un dato** («poco y seguido rinde más»: práctica distribuida; sin promesas de mejora).
7. **Juego 2 · Atención**: Freno de Emergencia.
8. **¿Cómo prefieres que te anime?** Celebrando cada logro / diciéndome las cosas claras: cambia el tono de los mensajes
   de Nubi (resultados y recordatorios). Se cambia en Ajustes.
9. **(nuevo) ¿Te cuesta distinguir algunos colores?** Sí / No / No sé. Con «sí», los juegos que dependen del color usan su
   paleta segura y Tinta o Palabra se marca para revisar (ver «Accesibilidad»). Se cambia en Ajustes.
10. **Juego 3 · Razonamiento**: Aterrizaje Lunar.
11. **¿Cuántos días por semana?** (3 / 4 recomendado / 5 / todos).
12. **Juego 4 · Lenguaje**: Lluvia de meteoros.
13. **Tu punto de partida**: las 4 áreas con su etapa (Inicio…Maestro), «tu fuerte hoy» y «donde más vamos a jugar»;
    «No es un examen: es desde dónde partimos». **(nuevo) Nace tu primera estrella**: Nubi celebra y en el cielo aparece la
    primera estrella (la historia de Nubi: «cada partida hace nacer una estrella»). Se enciende el día 1 de la racha.
14. **¿A qué hora te aviso?** (aquí se pide el permiso de notificaciones de Android 13+).
15. **Tu camino de hoy**: los 3 juegos del día elegidos por las metas y el punto de partida → entra a Hoy.

Salen del inicio: nivel de estudios (pasa a Perfil, opcional: hoy no cambia nada del juego) y «Así funciona» (cada cosa
se explica cuando aparece por primera vez).

## Reglas

- **Jugar en menos de un minuto**: antes del primer juego solo nombre y edad.
- **Barra de avance** arriba en todo el recorrido; en los juegos, además, el indicador de 4 pasos (✓ ✓ ● ○).
  **(nuevo)** Debajo de la barra, «faltan unos N minutos» (estimado, se actualiza).
- **Tutorial dentro del juego** (es EL tutorial de cada juego, el mismo que verá quien abra un juego por primera vez):
  - tarjeta de entrada con Nubi maestra y la meta en una frase;
  - ronda guiada: Nubi muestra, una marca indica dónde tocar, si hay error explica la regla y se repite (sin culpa);
  - «Saltar tutorial» siempre visible durante la ronda guiada;
  - la ronda guiada NO cuenta: no suma puntos, no mueve dificultad, avance ni rachas, no se guarda como partida;
  - después de la primera vez, queda en pausa → «Cómo se juega».
- **Cada juego cierra con «puso a prueba tu…»**: qué entrenó + el primer dato en palabras + una línea de para qué sirve.
  Texto propio, basado en `docs/medidas-juegos-estrella.md`.
- **Una pregunta por pantalla, un toque, con su porqué.**
- **Honesto**: sin promesas de mejora, sin comparar con otras personas mientras no haya datos propios, sin «puntaje
  cerebral». Es «tu punto de partida».
- **Sin cuenta para empezar.** Más adelante (cuando se trabaje Google Play): al final, «Guarda tu avance con Google»,
  opcional.
- **Se puede dejar a medias**: «Terminar después» en cada juego guarda lo jugado y estima el resto (como hoy).
- **(nuevo) Se retoma donde quedó**: Android puede cerrar la app mientras corre Unity (pasa en el Motorola). Cada paso del
  inicio se guarda en disco al completarse (mismo criterio que `bridge/GameSessionStore`); al volver, sigue en el paso
  siguiente, sin repetir juegos ni preguntas.
- **(nuevo) Duración de los juegos del inicio**: versión corta de cada juego (~60 s o un número fijo de rondas), con la
  dificultad inicial según la edad. El resultado siembra el rating guardado de ese juego y la estimación de su área
  (como hace hoy `data/Baseline.kt`), para que la primera partida normal ya empiece a la medida.
- Accesibilidad de siempre: texto ≥ 18 sp en el inicio, contraste ≥ 4,5:1, toque ≥ 48 dp, acierto/error con forma o texto,
  «quitar animaciones» respetado (la barra y los cambios de pantalla con fundido).

## Juegos del inicio (uno por área)

Elegidos por ser los más pulidos y fáciles de entender en segundos:

| Área | Juego | Mide (primer dato) |
|---|---|---|
| Memoria | Secuencia Lumínica **renovada** | cuántas luces recuerdas en orden (amplitud) |
| Atención | Freno de Emergencia | cuántas veces frenaste a tiempo |
| Razonamiento | Aterrizaje Lunar | distancia media al lugar justo de la recta |
| Lenguaje | Lluvia de meteoros | palabras reales reconocidas |

Depende de: (1) Secuencia y Parejas con la dificultad común; (2) Secuencia reestructurada (mundo propio, símbolos
neutros, medida al final). El inicio nuevo se programa después de esas dos tareas.

## Accesibilidad de color (nuevo)

La pregunta 9 guarda una preferencia (`color_vision`: normal / dificultad / no sé). Con «dificultad»: paletas seguras
para daltonismo donde existan y, en el futuro, la opción de ocultar juegos (idea de Peak/Lumosity, `docs/analisis-competencia.md` §9).
Nueva preferencia = decidir su respaldo en `BackupRulesTest` (es progreso de configuración: va al respaldo).
